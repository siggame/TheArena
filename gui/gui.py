"""
A gui for creating and monitoring Arena servers
Tanner May Arena Lead FS2019
"""

import tkinter as tk
from tkinter import ttk
from tkinter import *
import tkinter.scrolledtext as tkst
import googleapiclient.discovery
import time
import subprocess
import os

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

IMAGE_NAME = "host-client-starter"
GAMES_FILE = "games.txt"
USERNAME = "Arena" # the username that the google cloud servers will make in your /home file

#read games into list, allows permanently adding games
GAMES = [line.rstrip('\n') for line in open(GAMES_FILE, 'r')] # fancy one line file read into a list
START = 'Start'
STOP = 'Stop'
DEL = 'Delete'

class Cloud:
    def __init__(self):
        self.compute = googleapiclient.discovery.build('compute', 'v1')
        self.operation = None
        self.machines = [] #change this to use a dictionary to keep track if host is started/created!!!!
       #Possibly populate with machine objects to keep track of IPs, names, and other stuff

    def create(self, project, zone, region, host):
        """
        host is boolean, true means launch host server, false: launch client server
        """
        name = ("host-" if host else "client-") + str(time.time()).replace('.', '') # give server unique name using time
        self.machines.append(name)
        self.operation = self.gen_config(project, zone, region, name) # use the api to start the server

        return self.wait_for_operation(project, zone, self.operation['name'])

    def delete(self, project, zone, serverName):
        """
        delete a specified server
        serverName will almost always come from self.machines
        """
        # DELETE https://www.googleapis.com/compute/v1/projects/<project>/zones/<zone>/instances/<serverName>
        self.operation = self.compute.instances().delete(
            project=project,
            zone=zone,
            instance=serverName).execute()

        return self.wait_for_operation(project, zone, self.operation['name'])

    def stop(self, project, zone, serverName):
        """
        stop a specified server
        """
        # POST https://www.googleapis.com/compute/v1/projects/<project>/zones/<zone>/instances/<serverName>/stop
        self.operation = self.compute.instances().stop(
            project=project,
            zone=zone,
            instance=serverName).execute()

        return self.wait_for_operation(project, zone, self.operation['name'])

    def start(self, project, zone, serverName):
        """
        start a specified server
        """
        # POST https://www.googleapis.com/compute/v1/projects/<project>/zones/<zone>/instances/<serverName>/start
        self.operation = self.compute.instances().start(
            project=project,
            zone=zone,
            instance=serverName).execute()

        return self.wait_for_operation(project, zone, self.operation['name'])

    def wait_for_operation(self, project, zone, operation):
        """ 
        Taken from google's api guides. It checks if the server has completed the operation yet and if there were errors
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

    def gen_config(self, project, zone, region, name):
        """
        don't mess with this function unless you know exactly what you're doing
        modeled off of the REST generator on the cloud console
        
        Not needed if using a custom disk image

        # Get the latest Debian Jessie image.
        image_response = self.compute.images().getFromFamily(
            project='debian-cloud', family='debian-9').execute()
        source_disk_image = image_response['selfLink']"""

        # Configure the machine
        # "custom-1-6656" -> 1CPU, 6.5 GB of memory
        machine_type = "projects/%s/zones/%s/machineTypes/custom-2-6656" % (project, zone)

        config = {
            'name': name,
            'machineType': machine_type,

            'displayDevice': { 'enableDisplay': False },

            'metadata': {
                'kind': 'compute#metadata',
                'items': [
                    {
                        "key": "startup-script",
                        #update apt and packages then pull latest from git
                        "value": "apt update && apt -y upgrade\n"
                                 "cd /home/%s/TheArena\n"
                                 "git pull\n"
                                 "cd /home/%s/Cerveau\n"
                                 "git pull\n"
                                 "cd /home/%s" % (USERNAME, USERNAME, USERNAME)
                    }
                ]
            },

            'tags': {
                'items': [
                    'http-server',
                    'https-server'
                ]
            },

            'disks': [
                {
                    'kind': 'compute#attachedDisk',
                    'type': 'PERSISTENT',
                    'boot': True,
                    'mode': 'READ_WRITE',
                    'autoDelete': True,
                    'deviceName': name,
                    'initializeParams': {
                        #'sourceImage': source_disk_image, #original
                        'sourceImage': ('projects/%s/global/images/%s' % (project, IMAGE_NAME)),
                        'diskType': 'projects/%s/zones/%s/diskTypes/pd-standard' % (project, zone),
                        'diskSizeGb': '50'
                    },
                    'diskEncryptionKey': {}
                }
            ],

            'canIpForward': True,

            'networkInterfaces': [
                {
                    'kind': 'compute#networkInterface',
                    'subnetwork': 'projects/%s/regions/%s/subnetworks/default' %(project, region),
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
        self.window = self.make_window() #build tk interface
        self.configTabs = None #must be here so new tabs can be added in  other functions
        self.make_tabs()   #build the tabs

        self.project = ""
        self.region = ""
        self.zone = ""
        self.numClients = 0
        self.game = "Chess" #default value, will change when user clicks game in drop down

    # Tab/Window Creators#################################################
    @staticmethod
    def make_window():
        window = tk.Tk()  # create window instance
        window.title("Arena Manager")  # name it
        window.geometry("1000x600+150+150")

        return window

    def make_tabs(self):
        tabControl = ttk.Notebook(self.window)  # create tab control

        # setup tab setup
        setupTab = ttk.Frame(tabControl)  # Create the tab for host setup
        tabControl.add(setupTab, text='Server Setup')  # add tab with name "Server Setup"
        self.setup_setup_tab(setupTab)  # run setup_setup_tab function to populate with buttons and stuff

        # monitoring tab setup
        monitorTab = ttk.Frame(tabControl)  # Create the tab for host setup
        tabControl.add(monitorTab, text='Monitoring')  # add tab with name "Host Setup"
        self.setup_monitor_tab(monitorTab)

        ######################################-After tab setup
        tabControl.pack(expand=1, fill='both')  # Makes it visible

    def setup_setup_tab(self, parent):
        split = ttk.PanedWindow(parent, orient=HORIZONTAL)
        split.pack(fill=BOTH, expand=1)

        # Left side##############################################################
        left = ttk.LabelFrame(split, text="Config: press enter key to input values!")

        # Project box
        projectBox = ttk.Entry(left)
        projectBox.bind("<Return>", self.entry_project) # triggers function when user press <Return>
        # with the input_vals function these are redundant but handy to have

        # Region box
        regionBox = ttk.Entry(left)
        regionBox.bind("<Return>", self.entry_region)

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

        # Add game section#####
        addGameBox = ttk.Entry(left)
        newGame = ttk.Button(left, command=lambda: self.add_game(addGameBox, selection, popupMenu), text='Add game')

        # Placements
        # Project box
        ttk.Label(left, text='Project Name').grid(row=0, column=0, sticky=W, pady=5)
        projectBox.grid(row=0, column=1, sticky=E, pady=5)

        # region box
        ttk.Label(left, text='Region').grid(row=1, column=0, sticky=W, pady=5)
        regionBox.grid(row=1, column=1, sticky=E, pady=5)

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

        # Add game
        ttk.Label(left, text='Add game').grid(row=7, column=0, sticky=W, padx=(0, 20), pady=5)
        addGameBox.grid(row=7, column=1, sticky=E, pady=5)
        newGame.grid(row=8, column=1, sticky=E, pady=5)

        # Always at bottom
        split.add(left)
        ##################################################################

        # Right side######################################################
        right = ttk.LabelFrame(split, text="Monitor")

        # status box
        statusBox = tkst.ScrolledText(right) # adds scroll bar, was previously just a Text widget
        statusBox.configure(state='disabled') # makes it uneditable
        statusBox.pack(fill='both', expand=True)

        split.add(right)
        ##################################################################

        # left side extras################################################
        # input values button
        # has to be down here so that all buttons are already created
        inputVals = ttk.Button(left, command=lambda: self.input_vals(projectBox, regionBox,
                                                                     zoneBox, numClientsBox, statusBox, left),
                               text='Input Values')
        inputVals.grid(row=4, column=1, sticky=E, pady=5)

    def setup_monitor_tab(self, parent):
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
            self.setup_monitor_client_tab(serverTabs[-1], m) #m is the index of the client in self.compute.machines
                                                             #needed for doing actions of that server

        self.configTabs.pack(expand=1, fill='both')

    #/ setup tabs in monitor tab//////////////////////////////////////////

    def setup_monitor_all_tab(self, parent):
        split = ttk.PanedWindow(parent, orient=VERTICAL)
        split.pack(fill=BOTH, expand=1)

        # Top#################################
        top = ttk.LabelFrame(split, text="Monitor")

        messageBox = tkst.ScrolledText(top) # adds scroll bar, was previously just a Text widget
        messageBox.configure(state='disabled')  # make the box uneditable
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
        NAME = self.compute.machines[clientIndex] # never change this. It is the name of this client server in self.compute.machines
        # without it the tab cannot do actions on that client server

        split = ttk.PanedWindow(parent, orient=VERTICAL)
        split.pack(fill=BOTH, expand=True)

        # Top#################################
        top = ttk.LabelFrame(split, text="Monitor")

        messageBox = tkst.ScrolledText(top)  # adds scroll bar, was previously just a Text widget
        messageBox.configure(state='disabled')  # make the box uneditable
        messageBox.pack(fill='both', expand=True)

        self.copy_name(NAME, messageBox, top)

        split.add(top)
        ######################################

        # Bottom##############################
        bottom = ttk.LabelFrame(parent, text="Control")

        howSSH = Label(bottom, text="To open ssh connection type in terminal: "
                                    "'gcloud compute ssh <serverName> --zone <zone>'")

        # SSH Terminal
        sshInputLabel = ttk.Label(bottom, text="SSH Input")
        sshInput = ttk.Entry(bottom)


        # Button Creation ###
        #sshStart = ttk.Button(bottom, command=lambda: self.start_ssh(), text="Open SSH")

        # Start server button
        start = ttk.Button(bottom, command=lambda: self.action_single(START, NAME, messageBox, top), text="Start server")

        # Stop server button
        stop = ttk.Button(bottom, command=lambda: self.action_single(STOP, NAME, messageBox, top), text="Stop server")

        # Kill server button
        kill = ttk.Button(bottom, command=lambda: self.action_single(DEL, NAME, messageBox, top), text="Kill server")

        # Get server name
        getName = ttk.Button(bottom, command=lambda: self.copy_name(NAME, messageBox, top), text="Copy name")

        #####################

        # Positioning #######

        start.grid(row=0, column=0, padx=5,  pady=5)
        stop.grid(row=0, column=1, padx=5, pady=5)
        kill.grid(row=0, column=2, padx=5, pady=5)
        getName.grid(row=0, column=3, padx=5, pady=5)

        howSSH.grid(row=1, column=0, padx=5, pady=5, columnspan=8)
        #####################

        split.add(bottom)

    # adds or removes tabs depending on contents of self.compute.machines
    # just reruns the setup for the monitor tabs
    # it does delete all entries in the monitor text box...
    # improvement: find a way so it doesn't delete all of those then restore it after refresh
    def reconfigure_monitor_tabs(self, box, frame):
        self.text_edit("Reconfiguring GUI...", box, frame)
        self.setup_monitor_tab(self.configTabs)
    #end setup for monitor tabs///////////////////////////////////////////

    # end tab/window creators#############################################

    # user input utils####################################################
    def entry_project(self, event):
        self.project = event.widget.get()

    def entry_region(self, event):
        self.region = event.widget.get()

    def entry_zone(self, event):
        self.zone = event.widget.get()

    def entry_num_clients(self, event):
        self.numClients = int(event.widget.get())

    def entry_game(self, event):
        self.game = event

    #ability to add a game to the drop down menu
    @staticmethod
    def add_game(box, var, menu):
        game = box.get()

        GAMES.append(game)
        menu['menu'].add_command(label=game, command=tk._setit(var, game))

        # saves game for next time the software is used
        with open(GAMES_FILE, 'a') as file:
            file.write(game + '\n')

    #Now you don't have to press enter every time!
    def input_vals(self, projectBox, regionBox, zoneBox, numClientsBox, box, frame):
        try:
            self.project = projectBox.get()
            self.region = regionBox.get()
            self.zone = zoneBox.get()
            self.numClients = int(numClientsBox.get())
        except Exception as e:
            self.text_edit("There was an error:\n" + str(e), box, frame)

    @staticmethod
    def text_edit(s, box, frame):
        #make box contents editable
        box.configure(state='normal')
        #change contents
        box.insert(INSERT, "->%s\n" % s)
        #make uneditable again
        box.configure(state='disabled')
        #push new changes
        frame.update()

    def copy_name(self, name, box, frame):
        #copy NAME to clipboard
        self.window.clipboard_clear()
        self.window.clipboard_append(name)
        self.window.update()

        self.text_edit("%s has been copied." % name, box, frame)
    # end user input utils################################################

    # server manipulators#################################################
    # These aren't in the cloud class because they require the text edit function and stuff
    
    def start_ssh(self, serverName):

    def action_all(self, action, box, frame):
        """
        start, stop, or delete all servers.
        action can be START, STOP, or DEL
        """
        if action is not START and action is not STOP and action is not DEL:
            raise ValueError("Incorrect action.")

        #nothing to do
        if len(self.compute.machines) <= 0:
            self.text_edit("No servers to %s." % action.lower(), box, frame)
            return

        #status report to the box
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
            self.compute.machines = [] # remove all machines since they no longer exist
            self.text_edit("All servers deleted.\n", box, frame)
            self.reconfigure_monitor_tabs(box, frame) #remove tabs of machines that no longer exist

    def action_single(self, action, serverName, box, frame):
        if action is not START and action is not STOP and action is not DEL:
            raise ValueError("Incorrect action.")

        #nothing to do
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
        """
            creates the servers
        """
        #delete existing servers
        self.action_all(DEL, box, frame)

        self.text_edit("Starting host server and " + str(self.numClients) + " client servers...", box, frame)
        self.text_edit("This window will be unresponsive until task completes.", box, frame)
        self.text_edit("Starting host server...", box, frame)

        #create servers
        try:
            #create host
            self.compute.create(self.project, self.zone, self.region, True)
            self.text_edit("Host server started.", box,  frame)

            #create client servers
            for i in range(self.numClients):
                self.text_edit("Starting client server " + str(i) + "...", box, frame)
                self.compute.create(self.project, self.zone, self.region, False)
                self.text_edit("Client " + str(i) + " started.", box, frame)

            self.reconfigure_monitor_tabs(box, frame)  # add tabs of new servers
        except Exception as e:
            self.compute.machines = self.compute.machines[:-1] # remove newly created machine name since it didn't pan out
            self.text_edit("There was an error:\n" + str(e), box, frame)

        self.text_edit("Done.\n", box, frame)

    #starts the arena on the host server and the game server on the clients
    def arena_servers(self):

        return

    # end server manipulators#############################################

    def mainloop(self):
        self.window.mainloop()

ui = GUI()

#always goes at the bottom
ui.mainloop()