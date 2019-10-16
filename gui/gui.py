"""
A gui for creating and monitoring Arena servers
ACM Game - Arena FS2019
"""

import tkinter as tk
from tkinter import ttk
from tkinter import *
import tkinter.scrolledtext as tkst
import googleapiclient.discovery
import time
import pexpect # Using this instead of subprocess at times because it makes continuous communication with child process easy
import subprocess

"""
Some important documentation:

To make a new image for the disk:
1. Create a server with the old image using the "Create similar" option.
2a. (If you are simply adding to the old image) Do the additions to the server created in step one. Shut off machine.
2b. (If you are doing something completely different) In the create similar tab scroll to the disk section and change
    it to the Debian GNU/Linux 9 (stretch). Then do the needed operations. Shut off machine.
3. Create a new image based off of the newly modified server.
4. (Not needed but useful in case the image is deleted) Create a similar server to that created in step 2 but select
   the new image to use.
5. Change the IMAGE_NAME value below.

Regarding imports:
If the googleapiclient is giving an error use "pip3" instead of "pip" in the install command.

Startup script info:
"nano /var/log/syslog" to see if startup script ran
"""

"""
Google Cloud Service Project Attributes:

PROJECT: the id (not name) of your project in Google Cloud Console, found in upper left of window, to the right of
        "Google Cloud Platform"
ZONE: the zone (not region!) you selected when installing gcloud, if you forget:
        https://cloud.google.com/compute/docs/gcloud-compute/#set_default_zone_and_region_in_your_local_client
REGION: same process as ZONE

Note: zone and region can also be changed when creating new servers. If you have done this then that is what you
want to use when inputting values into the gui.
"""

"""
ISSUES:
S -> Needs to be done by megaminer
A -> It would be nice to have done by megaminer but not required
B -> It would make things easier
C -> Cosmetic/form
E -> Efficiency

1. S - Change Arena code from stdin input to cmd line parameters
2. S - Finish SSH code for individual servers, requires 1
3. S - Make Bash script to start arena server and game server on different cores, requires 2
4. A - Change cloud.machines to a dictionary so we can keep track of host
5. B - Change Arena code to use environment variables for paths
6. B - Split gui.py into different files
7. C - Move tkinter placement code to bottom of section
8. E - Change shh connection to start on button press instead of constantly running
9. C - Change input process to be like the ssh command input code -> lambda event: func(p1, p2)
        Allows use of key binds as well as functions with parameters
10. B - Figure out how to stop gui from freezing. Move server functions to other cores? Maybe async?
11. C - Figure out why text box outputs weird after ssh stuff
12. A - Cloud class creates service account if one is not found, if one is found use that one
13. C - Make all boxes copyable
"""

IMAGE_NAME = "official-image"
GAMES_FILE = "games.txt"
USERNAME = "siggame"
REGEX_STRING = "The programs included with the Debian GNU/Linux system are free software;.*" \
               "the exact distribution terms for each program are described in the.*" \
               "individual files in /usr/share/doc/\*/copyright\..*" \
               "Debian GNU/Linux comes with ABSOLUTELY NO WARRANTY, to the extent.*" \
               "permitted by applicable law\."

# read games into list, allows permanently adding games
GAMES = [line.rstrip('\n') for line in open(GAMES_FILE, 'r')]  # fancy one line file read into a list

# For string manipulation
START = 'Start'
STOP = 'Stop'
DEL = 'Delete'


class Cloud:
    def __init__(self):
        self.compute = googleapiclient.discovery.build('compute', 'v1')
        self.operation = None  # Stores the start/stop/delete/etc. operation for the wait_for_operation() function
        self.machines = []  # change this to use a dictionary to keep track if host is started/created!!!!

    def create(self, project, zone, region, host, image):
        """Create a server and return server response after waiting for server to be created.

        :param project: (str) parent project
        :param zone: (str) which zone the server lives in
        :param region: (str) which region the server lives in
        :param host: (bool) if server is the host or not; True: launch host server, False: launch client server
        :param image: (bool) create server for image creation
        :return: operation status, pass/fail -> wait_for_operation()
        """

        # give server unique name using time
        name = ("host-" if host else "client-") + str(time.time()).replace('.', '')

        if image:
            name = IMAGE_NAME # change name of server that image is based on so it isn't confusing in server list

        self.machines.append(name) # add new server to machines list

        self.operation = self.gen_config(project, zone, region, name, image)  # use the api to start the server

        return self.wait_for_operation(project, zone, self.operation['name'])  # wait for operation to finish

    def delete(self, project, zone, serverName):
        """Delete a specified server and return server response after waiting for server to be deleted.

        DELETE https://www.googleapis.com/compute/v1/projects/<project>/zones/<zone>/instances/<serverName>

        :param project: (str) parent project
        :param zone: (str) zone the server lives in
        :param serverName: (str) unique name of server
        :return: operation status, pass/fail -> wait_for_operation()
        """

        self.operation = self.compute.instances().delete(
            project=project,
            zone=zone,
            instance=serverName).execute()

        return self.wait_for_operation(project, zone, self.operation['name'])

    def stop(self, project, zone, serverName):
        """Stop a specified server and return server response after waiting for server to be stopped.

        POST https://www.googleapis.com/compute/v1/projects/<project>/zones/<zone>/instances/<serverName>/stop

        :param project: (str) parent project
        :param zone: (str) zone the server lives in
        :param serverName: (str) unique name of server
        :return: operation status, pass/fail -> wait_for_operation()
        """

        self.operation = self.compute.instances().stop(
            project=project,
            zone=zone,
            instance=serverName).execute()

        return self.wait_for_operation(project, zone, self.operation['name'])

    def start(self, project, zone, serverName):
        """Start a specified server and return server response after waiting for server to be started.

        POST https://www.googleapis.com/compute/v1/projects/<project>/zones/<zone>/instances/<serverName>/start

        :param project: (str) parent project
        :param zone: (str) zone the server lives in
        :param serverName: (str) unique name of server
        :return: operation status, pass/fail -> wait_for_operation()
        """

        self.operation = self.compute.instances().start(
            project=project,
            zone=zone,
            instance=serverName).execute()

        return self.wait_for_operation(project, zone, self.operation['name'])

    def wait_for_operation(self, project, zone, operation):
        """Check if the server has completed the operation yet and return status or errors.

        Taken from google's api guides: https://cloud.google.com/compute/docs/tutorials/python-guide

        :param project: (str) parent project
        :param zone: (str) zone the server lives in
        :param operation: (googleapiclient) operation to wait for
        :return: response of server
        """

        print('Waiting for operation to finish...')

        while True:
            result = self.compute.zoneOperations().get(
                project=project,
                zone=zone,
                operation=operation).execute()

            if result['status'] == 'DONE':
                print("done.")
                if 'error' in result:
                    raise Exception(result['error'])
                return result

            print('...')  # print to console so that user knows it hasn't frozen. This will NOT update GUI
            time.sleep(1)

    def gen_config(self, project, zone, region, name, image):
        """Configure new servers and return insert operation.

        Modeled off of the REST generator on the cloud console.

        :param project: (str) parent project of server
        :param zone: (str) zone server will live in
        :param region: (str) region server will live in
        :param name: (str) name of server
        :param image: (bool) will this be used to create an image or not
        :return: wait for server to be created
        """

        config = {
            'name': name,

            # "custom-2-6656" -> 2CPU, 6.5 GB of memory
            'machineType': "projects/%s/zones/%s/machineTypes/custom-2-6656" % (project, zone),

            # We don't need displays
            'displayDevice': {'enableDisplay': False},

            # Data for the instance for self configuration
            'metadata': {
                'kind': 'compute#metadata',
                'items': [
                    {
                        "key": "startup-script",
                        # Check if git is installed:
                        #    if so: update apt and packages then pull latest
                        #    if not: run through manual install from README and create new image
                        "value": ("#!/bin/bash\n\n"
                                  "apt update && apt -y upgrade\n"  # update apt/packages
                                  "if git --version 2>&1 >/dev/null\n"  # check if git installed
                                  "then\n"  # git installed
                                  "\techo \"GIT FOUND, UPDATING\"\n"  # for easy grepping of log files
                                  "\tcd /home/TheArena\n"
                                  "\tgit pull\n"
                                  "\tcd /home/Cerveau\n"
                                  "\tgit pull\n"
                                  "\techo \"DONE UPDATING\"\n"
                                  "else\n"  # git not installed
                                  "\techo \"NO GIT, INSTALLING\"\n"
                                  "\tapt -y install git\n"
                                  "\tmkdir /home/TheArena\n"
                                  "\tcd /home/TheArena/\n"
                                  "\tgit clone https://github.com/siggame/TheArena.git\n"
                                  "\t./TheArena/scripts/setup-new-machine.sh\n"  # run machine setup
                                  "\tmkdir /home/Cerveau\n"
                                  "\tcd /home/Cerveau\n"
                                  "\tgit clone https://github.com/siggame/Cerveau.git\n"
                                  "\techo \"DONE INSTALLING\"\n"
                                  "fi\n"
                                  "echo \"STARTUP DONE\"\n")
                    }
                ]
            },

            # Open connections
            'tags': {
                'items': [
                    'http-server',
                    'https-server'
                ]
            },

            # Specify the boot disk and the image to use as a source.
            'disks': [
                {
                    'kind': 'compute#attachedDisk',
                    'type': 'PERSISTENT',
                    'boot': True,
                    'mode': 'READ_WRITE',
                    'autoDelete': True,
                    'deviceName': name,
                    'initializeParams': {
                        # 'sourceImage': source_disk_image, #original
                        'sourceImage': 'projects/%s/global/images/%s' % (project, IMAGE_NAME) if not image
                        else "projects/debian-cloud/global/images/debian-9-stretch-v20190916",
                        'diskType': 'projects/%s/zones/%s/diskTypes/pd-standard' % (project, zone),
                        'diskSizeGb': '50'
                    },
                    'diskEncryptionKey': {}
                }
            ],

            # Forward IP
            'canIpForward': True,

            # Specify a network interface with NAT to access the public internet.
            'networkInterfaces': [
                {
                    'kind': 'compute#networkInterface',
                    'subnetwork': 'projects/%s/regions/%s/subnetworks/default' % (project, region),
                    'accessConfigs': [
                        {
                            'kind': 'compute#accessConfig',
                            'name': 'External NAT',
                            'type': 'ONE_TO_ONE_NAT',
                            'networkTier': 'PREMIUM'
                        }
                    ],
                    'aliasIpRanges': []
                }
            ],

            'description': '',

            'labels': {},

            'scheduling': {
                'preemptible': False,
                'onHostMaintenance': 'MIGRATE',
                'automaticRestart': True,
                'nodeAffinities': []
            },

            'deletionProtection': False,

            # Allow the instance to access cloud storage, logging, and apis
            'serviceAccounts': [
                {
                    'email': 'default',
                    'scopes': [
                        'https://www.googleapis.com/auth/devstorage.read_only',
                        'https://www.googleapis.com/auth/logging.write',
                        'https://www.googleapis.com/auth/monitoring.write',
                        'https://www.googleapis.com/auth/servicecontrol',
                        'https://www.googleapis.com/auth/service.management.readonly',
                        'https://www.googleapis.com/auth/trace.append'
                    ]
                }
            ]
        }

        return self.compute.instances().insert(
            project=project,
            zone=zone,
            body=config).execute()


class GUI:
    def __init__(self):
        self.compute = Cloud()
        self.window = self.make_window()  # build tk interface
        self.configTabs = None  # must be here so new tabs can be added in  other functions
        self.make_tabs()  # build the tabs

        self.project = ""
        self.region = ""
        self.zone = ""
        self.numClients = 0
        self.game = "Chess"  # default value, will change when user clicks game in drop down

    # Tab/Window Creators#################################################
    @staticmethod
    def make_window():
        """Create Tk object window, configure the geometry, and return window object.

        :return: (tk) Window object
        """

        window = tk.Tk()  # create window instance
        window.title("Arena Manager")  # name it
        window.geometry("1000x600+150+150")

        return window

    def make_tabs(self):
        """Create tabs at top of window.

        :return: None
        """

        tabControl = ttk.Notebook(self.window)  # create tab control

        # setup tab setup
        setupTab = ttk.Frame(tabControl)  # Create the tab for host setup
        tabControl.add(setupTab, text='Server Setup')  # add tab with name "Server Setup"
        self.setup_setup_tab(setupTab)  # run setup_setup_tab function to populate with buttons and stuff

        # monitoring tab setup
        monitorTab = ttk.Frame(tabControl)  # Create the tab for host setup
        tabControl.add(monitorTab, text='Monitoring')  # add tab with name "Host Setup"
        self.setup_monitor_tab(monitorTab)

        tabControl.pack(expand=1, fill='both')  # Makes it visible

    def setup_setup_tab(self, parent):
        """Add all buttons, labels, and text boxes in setup tab.

        :param parent: (tk) Master tab object in make_tabs()
        :return: None
        """

        split = ttk.PanedWindow(parent, orient=HORIZONTAL)
        split.pack(fill=BOTH, expand=1)

        # Left side##############################################################
        left = ttk.LabelFrame(split, text="Config: press enter key to input values!")

        # Project box
        projectBox = ttk.Entry(left)
        projectBox.bind("<Return>", self.entry_project)  # triggers function when user press <Return>
        # with the input_vals function these are redundant but handy to have

        # Zone box
        zoneBox = ttk.Entry(left)
        zoneBox.bind("<Return>", self.entry_zone)

        # number of clients box
        numClientsBox = ttk.Entry(left)
        numClientsBox.bind('<Return>', self.entry_num_clients)

        # game drop down menu
        selection = tk.StringVar(self.window)
        selection.set("Chess")
        popupMenu = ttk.OptionMenu(left, selection, GAMES[0], *GAMES, command=self.entry_game)

        # start servers button
        startWarning = ttk.Label(left, text="Kills all previously created servers.")
        start = ttk.Button(left, command=lambda: self.button_start(statusBox, left), text='Create servers')

        # Create Image Button
        imageLabel = ttk.Label(left, text="Use this if you need to create image")
        imageButton = ttk.Button(left, command=lambda: self.button_create_image(statusBox, left), text='Create image')

        # Add game section#####
        addGameBox = ttk.Entry(left)
        newGame = ttk.Button(left, command=lambda: self.add_game(addGameBox, selection, popupMenu), text='Add game')

        # Placements
        # Project box
        ttk.Label(left, text='Project ID').grid(row=0, column=0, sticky=W, pady=5)
        projectBox.grid(row=0, column=1, sticky=E, pady=5)

        # zone box
        ttk.Label(left, text='Zone').grid(row=2, column=0, sticky=W, pady=5)
        zoneBox.grid(row=2, column=1, sticky=E, pady=5)

        # numClients box
        ttk.Label(left, text='Number of Clients').grid(row=3, column=0, sticky=W, padx=(0, 20), pady=5)
        numClientsBox.grid(row=3, column=1, sticky=E, pady=5)

        # Games menu
        ttk.Label(left, text="Game").grid(row=5, column=0, sticky=W, pady=5)
        popupMenu.grid(row=5, column=1, sticky=E, pady=5)

        # Start
        startWarning.grid(row=6, column=0)
        start.grid(row=6, column=1, sticky=E, pady=50)

        # image button
        imageLabel.grid(row=7, column=0)
        imageButton.grid(row=7, column=1, sticky=E, pady=5)

        # Add game
        ttk.Label(left, text='Add game').grid(row=8, column=0, sticky=W, padx=(0, 20), pady=5)
        addGameBox.grid(row=8, column=1, sticky=E, pady=5)
        newGame.grid(row=9, column=1, sticky=E, pady=5)

        # Always at bottom
        split.add(left)
        ##################################################################

        # Right side######################################################
        right = ttk.LabelFrame(split, text="Monitor")

        # status box
        statusBox = tkst.ScrolledText(right)  # adds scroll bar, was previously just a Text widget
        statusBox.configure(state='disabled')  # makes it uneditable
        statusBox.bind("<Control-c>", self.general_copy)  # make selction copyable
        statusBox.pack(fill='both', expand=True)

        split.add(right)
        ##################################################################

        # left side extras################################################
        # input values button
        # has to be down here so that all buttons are already created
        inputVals = ttk.Button(left, command=lambda: self.input_vals(projectBox,
                                                                     zoneBox, numClientsBox, statusBox, left),
                               text='Input Values')
        inputVals.grid(row=4, column=1, sticky=E, pady=5)

    def setup_monitor_tab(self, parent):
        """Create tabs for every created server in the monitor tab.

        :param parent: (tk) Master tab object in make_tabs()
        :return: None
        """

        self.configTabs = ttk.Notebook(parent)

        # All server actions tab
        allTab = ttk.Frame(self.configTabs)
        self.configTabs.add(allTab, text='All')
        self.setup_monitor_all_tab(allTab)

        # tab for each server
        serverTabs = []
        for m in range(len(self.compute.machines)):
            serverTabs.append(ttk.Frame(self.configTabs))
            self.configTabs.add(serverTabs[-1], text=('Host' if m == 0 else ('Client ' + str(m - 1))))
            self.setup_monitor_client_tab(serverTabs[-1], m)  # m is the index of the client in self.compute.machines
            # needed for doing actions of that server

        self.configTabs.pack(expand=1, fill='both')

    # / setup tabs in monitor tab//////////////////////////////////////////

    def setup_monitor_all_tab(self, parent):
        """Add buttons, frames, and text boxes to all tab in monitor tab.

        :param parent: (tk) Master tab object in setup_monitor_tab()
        :return: None
        """

        split = ttk.PanedWindow(parent, orient=VERTICAL)
        split.pack(fill=BOTH, expand=1)

        # Top#################################
        top = ttk.LabelFrame(split, text="Monitor")

        messageBox = tkst.ScrolledText(top)  # adds scroll bar, was previously just a Text widget
        messageBox.configure(state='disabled')  # make the box uneditable
        messageBox.bind("<Control-c>", self.general_copy)  # Make selection copyable
        messageBox.pack(fill='both', expand=True)

        split.add(top)
        ######################################

        # Bottom##############################
        bottom = ttk.LabelFrame(parent, text="Control")

        # Warning Message
        ttk.Label(bottom, text='Warning: these act on ALL servers.').grid(row=0, column=0, columnspan=10,
                                                                          sticky=W, padx=(0, 20), pady=5)

        # Start servers button
        start = ttk.Button(bottom, command=lambda: self.action_all(START, messageBox, bottom), text='Start servers')
        start.grid(row=1, column=0, sticky=E, padx=5, pady=5)

        # Stop Servers button
        stop = ttk.Button(bottom, command=lambda: self.action_all(STOP, messageBox, bottom), text='Stop servers')
        stop.grid(row=1, column=1, sticky=E, padx=5, pady=5)

        # Kill Servers button
        kill = ttk.Button(bottom, command=lambda: self.action_all(DEL, messageBox, bottom), text='Kill servers')
        kill.grid(row=1, column=2, sticky=E, padx=5, pady=5)

        # Start Arena on all servers button
        arena = ttk.Button(bottom, command=lambda: self.arena_servers(), text='OPEN THE ARENA')
        arena.grid(row=1, column=3, sticky=E, padx=5, pady=5)

        split.add(bottom)

    def setup_monitor_client_tab(self, parent, clientIndex):
        """Add buttons, labels, frames, and text boxes in corresponding server tab in monitoring tab.

        :param parent: (tk) Master tab object in setup_monitor_tab()
        :param clientIndex: (int) Index of server in self.compute.machines
        :return: None
        """

        # Treat as constant. It is the name of this client server in self.compute.machines
        # without it the tab cannot do actions on that client server
        NAME = self.compute.machines[clientIndex]

        split = ttk.PanedWindow(parent, orient=VERTICAL)
        split.pack(fill=BOTH, expand=True)

        # Top#################################
        top = ttk.LabelFrame(split, text="Monitor")

        messageBox = tkst.ScrolledText(top)  # adds scroll bar, was previously just a Text widget
        messageBox.configure(state='disabled')  # make the box uneditable
        messageBox.bind("<Control-c>", self.general_copy)

        # SSH Terminal
        sshInputLabel = ttk.Label(top, text="SSH Input:")
        sshInput = ttk.Entry(top)
        # Need to come up with a way to start SSH on button press or something instead of it always being on
        sshTunnel = pexpect.spawn("gcloud compute ssh " + USERNAME + "@" + NAME + " --zone " + self.zone)  # open ssh
        sshTunnel.delaybeforesend = None
        print(str(sshTunnel))
        #sshTunnel.expect(REGEX_STRING, timeout=None)
        #sshTunnel.expect("%s@%s:~$" % (USERNAME, NAME), timeout=None)
        sshTunnel.expect("[A-Za-z0-9]+@(([a-z]{4})|([a-z]{6}))-[0-9]+:", timeout=None)
        print(str(sshTunnel))
        print("\n\n\n------------\n%s\n------------\n\n\n" % sshTunnel.before.decode("utf-8"))
        sshInput.bind("<Return>",
                      lambda event: self.entry_ssh_command(NAME, sshInput, sshTunnel, messageBox, top))

        # Placement
        messageBox.pack(fill='both', expand=True)
        sshInputLabel.pack(fill='both', expand=True)
        sshInput.pack(fill='both', expand=True)

        # print server name for copy/pasting
        self.text_edit("%s has been created." % NAME, messageBox, top)

        split.add(top)
        ######################################

        # Bottom##############################
        bottom = ttk.LabelFrame(parent, text="Control")

        howSSH = Label(bottom, text="To open ssh connection type in terminal: "
                                    "'gcloud compute ssh <serverName> --zone <zone>'")

        # Button Creation ###

        # Start server button
        start = ttk.Button(bottom, command=lambda: self.action_single(START, NAME, messageBox, top),
                           text="Start server")

        # Stop server button
        stop = ttk.Button(bottom, command=lambda: self.action_single(STOP, NAME, messageBox, top), text="Stop server")

        # Kill server button
        kill = ttk.Button(bottom, command=lambda: self.action_single(DEL, NAME, messageBox, top), text="Kill server")

        # Get server name
        getName = ttk.Button(bottom, command=lambda: self.copy_name(NAME, messageBox, top), text="Copy name")

        #####################

        # Positioning #######
        start.grid(row=2, column=0, padx=5, pady=5)
        stop.grid(row=2, column=1, padx=5, pady=5)
        kill.grid(row=2, column=2, padx=5, pady=5)
        getName.grid(row=2, column=3, padx=5, pady=5)

        howSSH.grid(row=3, column=0, padx=5, pady=5, columnspan=8)
        #####################

        split.add(bottom)

    def reconfigure_monitor_tabs(self, box, frame):
        """Add or remove tab depending on contents of self.compute.machines.

        Rerun setup_monitor_tab(). This process deletes then restores all tabs in monitor tab.
        Improvements: Don't delete all tabs then restore them.

        :param box: (tk) Text box to send updates to
        :param frame: (tk) Frame the text box lives in
        :return: None
        """

        self.text_edit("Running tab setup...", box, frame)
        self.setup_monitor_tab(self.configTabs)

    # end setup for monitor tabs///////////////////////////////////////////

    # end tab/window creators#############################################

    # user input utils####################################################
    def entry_project(self, event):
        """Get project text from project box.

        Set self.project to contents of project box in setup tab.

        :param event: (tk) Key bind event
        :return: None
        """

        self.project = event.widget.get()

    def entry_region(self, event):
        """Get region text from region box.

        Set self.region to contents of region box in setup tab.

        :param event: (tk) Key bind event
        :return: None
        """

        self.region = event.widget.get().lower()

    def entry_zone(self, event):
        """Get zone text from zone box.

        Set self.zone to contents of zone box in setup tab.

        :param event: (tk) Key bind event
        :return: None
        """

        self.zone = event.widget.get().lower()

    def entry_num_clients(self, event):
        """Get number of clients from numClients box.

        Set self.numClients to contents in numClients box.

        :param event: (tk) Key bind event
        :return: None
        """

        self.numClients = int(event.widget.get())

    def entry_game(self, event):
        """Get game name from game drop down.

        Set self.game to selected game name from game drop down.

        :param event: (tk) Selection change event
        :return: None
        """

        self.game = event

    def entry_ssh_command(self, name, sshInputBox, sshTunnel, box, frame):
        """Send command from entry box to the server.

        UNFINISHED

        :param name: (str) Name of server
        :param sshInputBox: (tk) Entry box where command is
        :param sshTunnel: (pexpect) The ssh connection
        :param box: (tk) Text box to put updates in
        :param frame: (tk) Frame that box lives in
        :return: None
        """

        sshCommand = sshInputBox.get()  # get command from entry box
        self.text_edit("ssh-send: %s" % sshCommand, box, frame)  # put command into message box
        self.text_edit("\tWaiting for response...", box, frame)
        sshTunnel.sendline(sshCommand)  # send it to the server
        sshTunnel.expect("[A-Za-z0-9][A-Za-z0-9]*@(([a-z]{4})|([a-z]{6}))-[0-9]+:", timeout=30)
        sshOutput = sshTunnel.before.decode("utf-8").strip()  # get result of sent command
        self.text_edit("ssh-receive: %s" % sshOutput, box, frame)  # put result into output box
        sshInputBox.delete(0, 'end')  # clear entry box

    # ability to add a game to the drop down menu
    @staticmethod
    def add_game(box, var, menu):
        """Add game name to game drop down and save to file.

        :param box: (tk) Text box to get name from
        :param var: (tk) Current selected game
        :param menu: (tk) Game drop down menu
        :return: None
        """

        game = box.get()  # retrieve new game name from entry box

        GAMES.append(game)  # add new game to GAMES list
        menu['menu'].add_command(label=game, command=tk._setit(var, game))  # select it

        # saves game to file for next time the software is used
        with open(GAMES_FILE, 'a') as file:
            file.write(game + '\n')

    # Now you don't have to press enter every time!
    def input_vals(self, projectBox, zoneBox, numClientsBox, box, frame):
        """Save values to corresponding variables.

        If there are errors send error text to text box.

        :param projectBox: (tk) Entry box that project text is typed in
        :param zoneBox: (tk) Entry box that zone text is typed in
        :param numClientsBox: (tk) Entry box that the amount of clients is typed in
        :param box: (tk) Text box to send updates to
        :param frame: (tk) Frame text box lives in
        :return: None
        """

        try:
            self.project = projectBox.get()
            self.zone = zoneBox.get()
            self.region = '-'.join(self.zone.split('-')[:-1])
            self.numClients = int(numClientsBox.get())
        except Exception as e:
            self.text_edit("There was an error:\n" + str(e), box, frame)

        self.text_edit("Values have been saved.", box, frame)

    @staticmethod
    def text_edit(s, box, frame):
        """Add s to box.

        Allow editing of box, add "->" to s, insert s at end of box, disable editing of text in box, update gui to show
        changes.

        :param s: (str) String to be inserted
        :param box: (tk) Text box to send s to
        :param frame: (tk) Frame to update to show new text
        :return: None
        """
        # make box contents editable
        box.configure(state='normal')
        # change contents
        box.insert(INSERT, "->%s\n" % s)
        # make uneditable again
        box.configure(state='disabled')
        # push new changes
        frame.update()

    def copy_name(self, name, box, frame):
        """Copy name to clipboard and update box

        :param name: (str) Name to be copied to clipboard
        :param box: (tk) Text box to send updates to
        :param frame: (tk) Frame text box lives in
        :return: None
        """

        # copy NAME to clipboard
        self.window.clipboard_clear()
        self.window.clipboard_append(name)
        self.window.update()

        self.text_edit("%s has been copied." % name, box, frame)

    def general_copy(self, event, textBox):
        copied = event.widget.selection_get()
        self.window.clipboard_clear()
        self.window.clipboard_append(copied)

    # end user input utils################################################

    # server manipulators#################################################
    # These aren't in the cloud class because they require the text edit function and stuff\

    def action_all(self, action, box, frame):
        """Start, stop, or delete all servers.

        Complete action on all servers, use START, STOP, or DEL to modify string to send to box.

        :param action: (str) Signify what action to do; domain: START, STOP, DEL
        :param box: (tk) Text box to send updates to
        :param frame: (tk) Frame text box lives in
        :return: None
        """

        if action is not START and action is not STOP and action is not DEL:
            raise ValueError("Incorrect action.")

        # nothing to do
        if len(self.compute.machines) <= 0:
            self.text_edit("No servers to %s." % action.lower(), box, frame)
            return

        # status report to the box
        self.text_edit(("%s %d servers..." % (action, len(self.compute.machines))), box, frame)

        # string manipulation is UGLY
        # do action on all servers
        for m in range(len(self.compute.machines)):
            try:
                if action == START:
                    self.text_edit("Starting " + ("host" if m == 0 else ("client " + str(m - 1))) + " server...",
                                   box, frame)

                    self.compute.start(self.project, self.zone, self.compute.machines[m])

                    self.text_edit(("Host" if m == 0 else ("Client " + str(m - 1))) + " started.", box, frame)
                elif action == STOP:
                    self.text_edit("Stopping " + ("host" if m == 0 else ("client " + str(m - 1))) + " server...",
                                   box, frame)

                    self.compute.stop(self.project, self.zone, self.compute.machines[m])

                    self.text_edit(("Host" if m == 0 else ("Client " + str(m - 1))) + " stopped.", box, frame)
                else:
                    self.text_edit("Deleting " + ("host" if m == 0 else ("client " + str(m - 1))) + " server...",
                                   box, frame)

                    self.compute.delete(self.project, self.zone, self.compute.machines[m])

                    self.text_edit(("Host" if m == 0 else ("Client " + str(m - 1))) + " deleted.", box, frame)
            except Exception as e:
                self.text_edit("There was an error:\n" + str(e), box, frame)

        # update that the action has been completed
        if action == START:
            self.text_edit("All servers started.\n", box, frame)
        elif action == STOP:
            self.text_edit("All servers stopped.\n", box, frame)
        else:
            self.compute.machines = []  # remove all machines since they no longer exist
            self.text_edit("All servers deleted.\n", box, frame)
            self.reconfigure_monitor_tabs(box, frame)  # remove tabs of machines that no longer exist

    def action_single(self, action, serverName, box, frame):
        """Start, stop, or delete single server.

        Complete action on single server, use START, STOP, or DEL to modify string to send to box.

        :param action: (str) Signify what action to do; domain: START, STOP, DEL
        :param serverName: (str) Name of server to do action on
        :param box: (tk) Text box to send updates to
        :param frame: (tk) Frame text box lives in
        :return: None
        """
        if action is not START and action is not STOP and action is not DEL:
            raise ValueError("Incorrect action.")

        # nothing to do
        if len(self.compute.machines) <= 0:
            self.text_edit("No servers to %s." % action.lower(), box, frame)
            return

        # check if serverName is in self.compute.machines
        if serverName not in self.compute.machines:
            self.text_edit("%s does not exist." % serverName, box, frame)
            return

        try:
            if action == START:
                self.text_edit("Starting server...", box, frame)

                self.compute.start(self.project, self.zone, serverName)

                self.text_edit("Server started.", box, frame)
            elif action == STOP:
                self.text_edit("Stopping server...", box, frame)

                self.compute.stop(self.project, self.zone, serverName)

                self.text_edit("Server stopped.", box, frame)
            else:
                self.text_edit("Deleting server...", box, frame)

                self.compute.delete(self.project, self.zone, serverName)
                self.compute.machines.remove(serverName)

                self.text_edit("Server deleted.", box, frame)
                self.configTabs.forget(self.configTabs.select())  # remove tabs of machines that no longer exist
                # This ^ line only removes tab from window but not memory!!!!!!
        except Exception as e:
            self.text_edit("There was an error:\n" + str(e), box, frame)

    def button_start(self, box, frame):
        """Create numClients + 1 servers and update box with status.

        Delete all existing servers, update box with how many servers to make, create self.numClients + 1 servers.

        :param box: (tk) Text box to send updates to
        :param frame: (tk) Frame text box lives in
        :return: None
        """

        # delete existing servers
        self.action_all(DEL, box, frame)

        self.text_edit("Starting host server and " + str(self.numClients) + " client servers...", box, frame)
        self.text_edit("This window will be unresponsive until task completes.", box, frame)
        self.text_edit("Starting host server...", box, frame)

        # create servers
        try:
            # create host
            self.compute.create(self.project, self.zone, self.region, True, False)
            self.text_edit("Host server started.", box, frame)

            # create client servers
            for i in range(self.numClients):
                self.text_edit("Starting client server " + str(i) + "...", box, frame)
                self.compute.create(self.project, self.zone, self.region, False, False)
                self.text_edit("Client " + str(i) + " started.", box, frame)

            self.reconfigure_monitor_tabs(box, frame)  # add tabs of new servers
        except Exception as e:
            # remove newly created machine name since it didn't pan out
            self.compute.machines = self.compute.machines[:-1]
            self.text_edit("There was an error:\n" + str(e), box, frame)

        self.text_edit("Done.\n", box, frame)

    def button_create_image(self, box, frame):
        """Create a new image with everything correctly installed according to README.md.

        WARNING: Images do not get deleted when server is deleted.

        :param box: (tk) Text box to put status updates in
        :param frame: (tk) Parent frame of text box
        :return: nothing
        """

        self.text_edit("Creating new image, this may take a while...", box, frame)

        try:
            self.compute.create(self.project, self.zone, self.region, host=False, image=True)

            # wait until startup script is done:
            self.text_edit("Server started. Waiting for startup script to finish...", box, frame)
            cmd = '\'cat /var/log/syslog | grep "STARTUP DONE"\''
            scriptStatus = ''
            while scriptStatus == '':
                # repeatedly ssh and read log file for "STARTUP DONE"
                try:
                    scriptStatus = subprocess.check_output('gcloud compute ssh %s --command=%s --zone %s'
                                                       % (self.compute.machines[-1], cmd, self.zone), shell=True)
                except subprocess.CalledProcessError:
                    # except because grep throws exit error code 1 if it doesn't find string
                    print("...")
                    time.sleep(5)

            self.text_edit("Startup script is done. Stopping server...", box, frame)

            # Google recommends stopping the server before making an image
            self.compute.stop(self.project, self.zone, self.compute.machines[-1])

            """
            doesn't use API because I am pretty sure it isn't implemented but if we want to try in the future:
            POST https://www.googleapis.com/compute/beta/projects/[PROJECT_ID]/global/images
            https://cloud.google.com/compute/docs/images/create-delete-deprecate-private-images
            """
            self.text_edit("Server stopped. Creating image...", box, frame)
            subprocess.call("gcloud beta compute images create %s --source-disk %s --source-disk-zone %s --force"
                            % (IMAGE_NAME, IMAGE_NAME, self.zone), shell=True)
        except Exception as e:
            # remove newly created machine name since it didn't pan out
            self.compute.machines = self.compute.machines[:-1]
            self.text_edit("There was an error:\n" + str(e), box, frame)

        self.text_edit("Done.\n", box, frame)

    # starts the arena on the host server and the game server on the clients
    def arena_servers(self):

        return

    # end server manipulators#############################################

    def mainloop(self):
        """Run Tkinter mainloop function.

        :return: None
        """
        self.window.mainloop()


class Machine:
    """ Object representation of machines. """
    def __init__(self, name, ip=None):
        self.name = name
        self.ip = ip


ui = GUI()

# always goes at the bottom
ui.mainloop()
