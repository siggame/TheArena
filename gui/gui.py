"""
A gui for creating and monitoring Arena servers
Tanner May Arena Lead FS2019
"""

import tkinter as tk
from tkinter import ttk
from tkinter import *
import googleapiclient.discovery
import time

#THESE NEED TO BE BOXES IN THE GUI!!!
"""
Google Cloud Service Project Attributes:
PROJECT: the id (not name) of your project in Google Cloud Console, found in upper left of window, to the right of
        "Google Cloud Platform"
ZONE: the zone (not region!) you selected when installing gcloud, if you forget:
        https://cloud.google.com/compute/docs/gcloud-compute/#set_default_zone_and_region_in_your_local_client
REGION: same process as ZONE
"""
#defaults for now
PROJECT = "sig-game"
REGION = "us-central1"
ZONE = "us-central1-c"

GAMES = [
    "Chess",
    "Checkers",
    #Add new games below this line
    "Stardash",
    "Newtonian",
    "Pirates",
    "Catastrophe",
    "Stumped",
    "Saloon",
    "Spiders",
    "Anarchy"
]

class Cloud:
    def __init__(self):
        self.compute = googleapiclient.discovery.build('compute', 'v1')
        self.operation = None

    #host is boolean, true means launch host server, false: launch client server
    def create(self, host):
        name = ("host-" if host else "client-") + str(time.time()).replace('.', '')
        self.operation = self.gen_config(PROJECT, ZONE, REGION, name) #use the api to start the server

        return self.wait_for_operation(PROJECT, ZONE, self.operation['name'])

    #dont mess with this function unless you know exactly what you're doing
    def gen_config(self, project, zone, region, name):
        # Get the latest Debian Jessie image.
        image_response = self.compute.images().getFromFamily(
            project='debian-cloud', family='debian-9').execute()
        source_disk_image = image_response['selfLink']

        # Configure the machine
        machine_type = "projects/%s/zones/%s/machineTypes/custom-1-6656" % (project, zone)

        config = {
            'name': name,
            'machineType': machine_type,

            'displayDevice': { 'enableDisplay': False },

            'metadata': {
                'kind': 'compute#metadata',
                'items': []
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
                        'sourceImage': source_disk_image,
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

    def wait_for_operation(self, project, zone, operation):
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

            print('...')
            time.sleep(1)

class GUI:
    def __init__(self):
        self.compute = Cloud()
        self.window = self.make_window() #build tk interface
        self.make_tabs()   #build the tabs

        self.project = PROJECT
        self.region = REGION
        self.zone = ZONE
        self.numClients = 0
        self.game = "Chess"

    def make_window(self):
        window = tk.Tk()  # create window instance
        window.title("Arena Manager")  # name it
        window.geometry("1000x600+150+150")

        return window

    def make_tabs(self):
        tabControl = ttk.Notebook(self.window)  # create tab control

        #setup tab setup
        setupTab = ttk.Frame(tabControl)  # Create the tab for host setup
        tabControl.add(setupTab, text='Server Setup')  # add tab with name "Server Setup"
        self.setup_setup_tab(setupTab) # run setup_setup_tab function to populate with buttons and stuff

        # host tab setup
        hostTab = ttk.Frame(tabControl)  # Create the tab for host setup
        tabControl.add(hostTab, text='Host Monitor')  # add tab with name "Host Setup"
        self.setup_host_tab(hostTab)

        # client tab setup
        clientTab = ttk.Frame(tabControl)
        tabControl.add(clientTab, text='Client Monitor')
        self.setup_client_tab(clientTab)

        ######################################-After tab setup
        tabControl.pack(expand=1, fill='both')  # Makes it visible

    ##########################################-Functions to set up the setup tab
    def setup_setup_tab(self, s):
        split = ttk.PanedWindow(s, orient=HORIZONTAL)
        split.pack(fill=BOTH, expand=1)

        #Left side##############################################################
        left = ttk.LabelFrame(split, text="Config: press enter key to input values!")

        #Project box
        ttk.Label(left, text='Project Name').grid(row=0, column=0, sticky=W, pady=5)
        projectBox = ttk.Entry(left)
        projectBox.grid(row=0, column=1, sticky=E, pady=5)
        projectBox.bind("<Return>", self.entry_project)

        #Region box
        ttk.Label(left, text='Region').grid(row=1, column=0, sticky=W, pady=5)
        regionBox = ttk.Entry(left)
        regionBox.grid(row=1, column=1, sticky=E, pady=5)
        regionBox.bind("<Return>", self.entry_region)

        #Zone box
        ttk.Label(left, text='Zone').grid(row=2, column=0, sticky=W, pady=5)
        zoneBox = ttk.Entry(left)
        zoneBox.grid(row=2, column=1, sticky=E, pady=5)
        zoneBox.bind("<Return>", self.entry_zone)

        # number of clients box
        ttk.Label(left, text='Number of Clients').grid(row=3, column=0, sticky=W, padx=(0, 20), pady=5)
        numClientsBox = ttk.Entry(left)
        numClientsBox.grid(row=3, column=1, sticky=E, pady=5)
        numClientsBox.bind('<Return>', self.entry_num_clients)

        # game drop down menu
        ttk.Label(left, text="Game").grid(row=4, column=0, sticky=W, pady=5)
        selection = tk.StringVar(self.window)
        selection.set("Chess")
        popupMenu = ttk.OptionMenu(left, selection, GAMES[0], *GAMES, command=self.entry_game)
        popupMenu.grid(row=4, column=1, sticky=E, pady=5)

        split.add(left)
        ###################################################################

        # Right side########################################################
        right = ttk.LabelFrame(split, text="Monitor")

        # status box
        statusBox = tk.Text(right)
        statusBox.configure(state='disabled')
        statusBox.pack(fill='both', expand=True)

        split.add(right)
        ###################################################################

        # Left Side Extra###################################################
        """# accept information
        accept = ttk.Button(left, command=lambda: self.button_start(statusBox, left), text='Start servers')
        accept.grid(column=1, sticky=E)"""
        # start servers button
        start = ttk.Button(left, command=lambda: self.button_start(statusBox, left), text='Start servers')
        start.grid(column=1, sticky=E, pady=50)
        ###################################################################

    #<return> events
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

    def text_edit(self, s, box, frame):
        #make box contents editable
        box.configure(state='normal')
        #change contents
        box.insert(INSERT, s + '\n')
        #make uneditable again
        box.configure(state='disabled')
        #push new changes
        frame.update()

    #starts the servers
    def button_start(self, box, frame):
        #print out important info
        self.text_edit("Project: " + self.project
                       + "; Region: " + self.region
                       + "; Zone: " + self.zone
                       + "; Game: " + self.game, box, frame)
        self.text_edit("Starting host server and " + str(self.numClients) + " client servers...", box, frame)
        self.text_edit("Starting host server...", box, frame)

        #start host server
        result = None

        try:
            result = self.compute.create(True)

            #update statusBox with result
            self.confirm_start(box, frame, True, result)

            #start client servers
            for i in range(self.numClients):
                self.text_edit("Starting client server " + str(i), box, frame)
                result = self.compute.create(False)
                self.confirm_start(box, frame, False, result)
        except Exception:
            self.text_edit("There was an error:\n" + result['error'], box, frame)

    #generates output for statusBox
    def confirm_start(self, box, frame, host, result):
        if result['status'] == 'DONE':
            self.text_edit(("Host" if host else "Client") + " server started.", box, frame)

    ##########################################

    def setup_host_tab(self, h):

        return

    def setup_client_tab(self, c):

        return

    def mainloop(self):
        self.window.mainloop()

ui = GUI()

#always goes at the bottom
ui.mainloop()