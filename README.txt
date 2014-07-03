Indirectly Encoded SodaRace, IESoR v 1.0
By Paul Szerlip
EMail: pszerlip@cs.ucf.edu
http://eplex.cs.ucf.edu

Faculty Supervisor: Kenneth Stanley
kstanley@eecs.ucf.edu

Example walking individuals can be found here: http://eplex.cs.ucf.edu/iesor/live/walk/

Example jumping individuals can be found here: http://eplex.cs.ucf.edu/iesor/live/jump/

IESoR is built in part on the HyperSharpNEAT code
By David D'Ambrosio, Joel Lehman, and Sebastian Risi (itself based on SharpNEAT)

Current Documentation for this package is included in this README file (future documentation on github)

//The documentation of this package as well as the code itself is currently undergoing many changes
//If you are interested in the library, it's best to follow the repository on Github, and file ALL bugs there
//I (Paul) will be actively maintaining the repo, and should have bugs fixed quickly.

There are 2 big sections in the code.
1. HTML
2. Client

1. Html represents the javascript side of the experiments. This includes the domain (the Sodarace-like environment), and the viewing of progress inside evolution.

2. Client represents the C# side of things.

Since there was no default NEAT library written in Javascript at the time of the experiments (now there is neatjs: https://github.com/OptimusLime/neatjs), all the evolution was run using the Eplex C# HyperNEAT implementation.

(The C# code is based on Colin Green's SharpNEAT).

Therefore, the C# code is in control of the evolution loop, and it talks to the browser through a nodejs app using sockets/websockets.

##IMPORTANT INFORMATION:
//How to install and run and experiment (a little rough around the edges in this version)
//Since it's currently a bit rough, feel free to contact me at pszerlip@cs.ucf.edu for detailed help.

#Get the software:

Open up git bash -- navigate to where you want to load the software
1. Clone the whole repository onto your computer (or download the archive and unzip)
git clone https://github.com/OptimusLime/IESoR.git

#Installing Nodejs dependencies:

2. Inside the folder there are 2 folders (HTML and Client)

Navigate to /html so we can install dependencies:
Type into bash (ignore $)

$ npm install -d

This will install dependencies for IESoR UI

Navigate to /client/NodejsCommunicator
Type into bash (ignore $)

$ npm install -d

This will install dependencies for the nodejs app that connections the javascript code to the C# code

#Running the Experiment:

//We need to start up the nodejs app, and then load the C# client.

When finished installing dependencies, navigate to /client/NodejsCommunicator
We'll run the app by typing the following into git bash:

$ node simpleapp.js

Now our communicator is running, we need to start the client code in C#.

The C# code is inside /client/ExperimentRunnner

1. Navigate to /client/ExperimentRunner and open NodeCommunicator.sln in visual studio
2. Build and run the project
3. Click "Connect and Run with Novelty" while the nodejs app is running, and it should connect and start the experiment

#To view results of the running experiment:

Inside of /html/display folder, there are 2 very useful HTML pages.

IECCurrent.html shows you individuals in the archive during evolution. Open IECCurrent.html inside of Firefox (Chrome will complain about cross-domain requests unless you disable that)

PCA.html shows you the PCA display for all individuals during the entire evolution.
Note: Loading the PCA html page will automatically save all objects inside of the C# evolutionary code. The loading process might take 1-3 minutes depending on CPU speed.

You can explore the current best individuals in the pca code! Refreshing will refresh the PCA display.

Sometimes the values estimated for distance aren't correct -- in order to fix the PCA display, simply click the "Sanitize PCA" button. Warning: that might take a while later in the runs.
