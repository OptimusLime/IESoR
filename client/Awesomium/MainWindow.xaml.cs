using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Newtonsoft.Json.Linq;
using SharpNeatLib.Evolution;
using SharpNeatLib.NeatGenome;
using System.Collections;
using SharpNeatLib.NeuralNetwork;
using SharpNeatLib.Masters;
using Newtonsoft.Json;
using SharpNeatLib.Experiments;
using System.IO;
using SharpNeatLib.NeatGenome.Xml;
using System.Xml;
using Awesomium.Core;
using Awesomium.sockets;
using Awesomium.Windows.Controls;
using Awesomium.Windows.Data;

namespace NodeCommunicator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        SimpleCommunicator simpleCom;

        WebView headlessBrowser;

        public MainWindow()
        {
            InitializeComponent();
            //EventText.Text = "Your move, friend... \n";
            //PingButton.IsEnabled = false;


            simpleCom = new SimpleCommunicator(new SimplePrinter(null));

            webControl.Loaded += new RoutedEventHandler(delegate(object sender, RoutedEventArgs e)
                {
                    try
                    {

                        //set up our headless browser
                        headlessBrowser = WebCore.CreateWebView(1024, 768, webControl.WebSession, WebViewType.Offscreen);
                        headlessBrowser.ConsoleMessage += headlessWebView_ConsoleMessage;
                        headlessBrowser.Source = new Uri("asset://local/html/evolution/evaluate/SingleEvaluation.html");
                        headlessBrowser.DocumentReady += new UrlEventHandler(delegate(object s, UrlEventArgs urlE)
                        {
                            //start the websocket server on port 4000 
                            MasterSocketManager.LaunchWebsocketServer(4000);
                            //MasterSocketManager.registerCallback("goofy", socketCall);
                            simpleCom.Execute(true);
                        });

                    }
                    catch (Exception exc
                        )
                    {
                        Console.WriteLine("Failed to launch socket server");
                    }

                });


           

            //simpleCom = new SimpleCommunicator(socketOpened, socketClose, new SimplePrinter(EventText));
            
            //loadSelectedGenomes("experiment");
            //buildBodyExamples();
            
        }

        void webControl_Loaded(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        void headlessBrowser_DocumentReady(object sender, UrlEventArgs e)
        {
            throw new NotImplementedException();
        }

        JObject socketCall(JObject data)
        {
            var jReturn =  new JObject();
            jReturn.Add("butt", "pooper");
            return jReturn;
        }



        #region Awesomium Setup/Event Handling
        private void headlessWebView_ConsoleMessage(object sender, ConsoleMessageEventArgs e)
        {
            Console.WriteLine(String.Format("eval>{0}", e.Message));
            //.AppendText(String.Format(">{0}\n", e.Message));
            //consoleBox.ScrollToEnd();
        }



        private void webControl_ConsoleMessage(object sender, ConsoleMessageEventArgs e)
        {
            Console.WriteLine(String.Format(">{0}", e.Message));
            //.AppendText(String.Format(">{0}\n", e.Message));
            //consoleBox.ScrollToEnd();
        }
        private void webControl_NativeViewInitialized(object sender, WebViewEventArgs e)
        {


            // We demonstrate the creation of a child global object.
            // Acquire the parent first.
            JSObject external = webControl.CreateGlobalJavascriptObject("external");

            if (external == null)
                return;


            //using (external)
            //{
            //    // Create a child using fully qualified name. This only succeeds if
            //    // the parent is created first.
            //    JSObject app = webControl.CreateGlobalJavascriptObject("external.app");

            //    if (app == null)
            //        return;

            //    using (app)
            //    {
            //        // Create and bind to an asynchronous custom method that will be called
            //        // by JavaScript, when the page is ready to provide us with a response
            //        // after performing some time consuming operation.
            //        // (See: /web/index.html)
            //        app.Bind("sendResponse", false, OnResponse);
            //        // Create and bind to an asynchronous custom method that is called
            //        // by JavaScript to have our native app perform some heavy, time consuming 
            //        // work and provide a response asynchronously.
            //        // (See: /web/index.html)
            //        app.Bind("performHeavyWork", false, OnWork);
            //    }
            //}
        }
        
        #endregion



        //open a directory
        //then we'll 
        void loadSelectedGenomes(string dirOfGenomes)
        {
            //first we need to figure out what collection of genome IDs we want to actually add

            //Dictionary<long, double> approvedGenomeIDs = new Dictionary<long, double>();

            //string path = "selectedGenomeIDs.json";
            //JObject parsedGenomeObject;
            //using (StreamReader sr = new StreamReader(path))
            //{
            //    string json = sr.ReadToEnd();
            //    parsedGenomeObject = JObject.Parse(json);
            //}

            //List<string> genomeIDs = new List<string>();
            //foreach (var jToken in parsedGenomeObject)
            //{
            //    genomeIDs.Add(long.Parse(jToken.Key).ToString());
            //    approvedGenomeIDs.Add(long.Parse(jToken.Key), (double)jToken.Value);
            //}

            //Grab all the genome files
            string[] genomes = Directory.GetFiles(dirOfGenomes);


            //loop through all the genomes, loading as we go along
            //This will trigger saving in the Neuron

            //Eventually, the saving will happen at the NeatGenome level, not the individual neuron and connection level!
          


         
            //Dictionary<long, bool> responseGenomes = new Dictionary<long, bool>();


            JObject root = new JObject();
            JObject meta = new JObject();

            //grab query points from body constructor
            JArray queryPoints = JArray.FromObject(simpleCom.simpleExperiment.gridQueryPoints(9));
            root.Add("grid", queryPoints);

            JObject genomeObjects = new JObject();


            foreach (var genomeFile in genomes)
            {

                if (genomeFile.Contains("genome"))
                {
                    //for each genome in this directory, we need to compile into a single file 
                    //full of body information


                    //load the file
                    NeatGenome ng = loadGenome(genomeFile);

                    bool isEmptyBody = false;
                    //make the object
                    var bodyObject = simpleCom.simpleExperiment.genomeIntoBodyObject(ng, out isEmptyBody);

                    //yahoo, we have all our body stuff, let's pack it up and ship it out
                    genomeObjects.Add(ng.GenomeId.ToString(), JObject.FromObject(bodyObject));

                    //save the necessary info!


                    //string genomeName = genomeFile.Substring(genomeFile.IndexOf("_"), genomeFile.IndexOf("."));
                    
                    //bool selectedGenome = false;
                    //foreach (string id in genomeIDs)
                    //{
                    //    if (genomeName.Contains(id))
                    //    {
                    //        selectedGenome = true;
                    //        break;
                    //    }
                    //}

                    //if (selectedGenome)
                    //    Console.WriteLine("Found genome: " + genomeFile);


                }
            }
            root.Add("genomes", genomeObjects);

            //and away we go! Let's save to file!
            using (System.IO.StreamWriter file = new System.IO.StreamWriter("selectedBodyConstruction.json"))
            {
                file.WriteLine(root.ToString());
            }

            return;

        }

        NeatGenome loadGenome(string genomeFileName)
        {

            //XmlDocument xDoc = 
            XmlDocument doc = new XmlDocument();
            doc.Load(genomeFileName);

            //this will read the neatgenome, then we can immediately enter these neurons into the database
            return XmlNeatGenomeReaderStatic.Read(doc);

        }

        void buildBodyExamples()
        {

            //we need to create random genomes, then save their generated bodies!
            NeatParameters np = new NeatParameters();
            IdGenerator idGen = new IdGenerator();

            idGen.ResetNextInnovationNumber();

            Random r = new Random();

            JObject root = new JObject();
            JObject meta = new JObject();

            JArray genomeArray = new JArray();

            //how many random input tests?
            int genomeCount = 20;

            meta.Add("genomeCount", genomeCount);
            meta.Add("weightRange", HyperNEATParameters.weightRange);

            NeatGenome seed = EvolutionManager.SharedEvolutionManager.getSeed();

            int tEmpty = 0;
            int emptyCount = genomeCount/4;

            for (int n = genomeArray.Count; n < genomeCount; n = genomeArray.Count)
            {

                //create our genome
                var inputCount = r.Next(4) + 3;
                var outputCount = r.Next(3) + 1;
                //radnom inputs 3-6, random outputs 1-3
                var genome = GenomeFactory.CreateGenomePreserveID(seed, idGen);//np, idGen, inputCount, outputCount, 1);


                Hashtable nodeHT = new Hashtable();
                Hashtable connHT = new Hashtable();

                //mutate our genome
                for (int m = 0; m < 20; m++)
                {
                    ((NeatGenome)genome).Mutate(np, idGen, nodeHT, connHT);
                }

                //now turn genome into a network
                var network = genome.Decode(null);

                //turn network into JSON, and save as the network object
                //genomeJSON.Add("network", JObject.FromObject(network));

                //now we need a body
                bool isEmptyBody;
                //convert to body object
                var bodyObject = simpleCom.simpleExperiment.genomeIntoBodyObject(genome, out isEmptyBody);

                if ((isEmptyBody && tEmpty++ < emptyCount) || (!isEmptyBody))
                {
                    //create object and add body info to it, then save it in our array
                    JObject genomeJSON = new JObject();

                    genomeJSON.Add("genome", JObject.FromObject(genome, new JsonSerializer() { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }));
                    genomeJSON.Add("network", JObject.FromObject(network, new JsonSerializer() { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }));
                    //save our body object from test
                    genomeJSON.Add("body", JObject.FromObject(bodyObject));
                    genomeJSON.Add("isEmpty", isEmptyBody.ToString());

                    //finally, we add our network json to the body array
                    genomeArray.Add(genomeJSON);

                }

                
            }

            //add our networks, and add our meta information
            root.Add("genomeCount", genomeArray.Count);
            root.Add("genomes", genomeArray);
            root.Add("meta", meta);

            //and away we go! Let's save to file!
            using (System.IO.StreamWriter file = new System.IO.StreamWriter("testgenomebodies.json"))
            {
                file.WriteLine(root.ToString());
            }
        }
        void generateSampleCPPNs()
        {

            NeatParameters np = new NeatParameters();
            IdGenerator idGen = new IdGenerator();

            idGen.ResetNextInnovationNumber();

            Random r = new Random();

            JObject root = new JObject();
            JObject meta = new JObject();

            JArray networkArray = new JArray();

            //how many random input tests?
            int testCount = 100;
            int networkCount = 20;

            meta.Add("networkCount", networkCount);
            meta.Add("sampleCount", testCount);

            Console.WriteLine("All Networks start will run:" + networkCount * testCount);


            for (int n = 0; n < networkCount; n++)
            {
                Console.WriteLine("Network start:" + n);

                JObject networkJSON = new JObject();
                //create our genome
                var inputCount = r.Next(4) + 3;
                var outputCount = r.Next(3) + 1;
                //radnom inputs 3-6, random outputs 1-3
                var genome = GenomeFactory.CreateGenome(np, idGen, inputCount, outputCount, 1);

                Console.WriteLine("Genoem created:" + n);

                Hashtable nodeHT = new Hashtable();
                Hashtable connHT = new Hashtable();

                //mutate our genome
                for (int m = 0; m < 20; m++)
                {
                    ((NeatGenome)genome).Mutate(np, idGen, nodeHT, connHT);
                    Console.WriteLine("Mutation done: " + m);
                }

                Console.WriteLine("Mutations done:" + n);

                //now turn genome into a network
                var network = genome.Decode(null);

                Console.WriteLine("genome decoded:" + n);


                //turn network into JSON, and save as the network object
                networkJSON.Add("network", JObject.FromObject(network));

                JArray inputsAndOutputs = new JArray();

                Console.WriteLine("starting tests:" + n);

                for (var t = 0; t < testCount; t++)
                {
                    Console.WriteLine("Test " + t + " :" + "for" + n);
                    JArray inputSamples = new JArray();
                    JArray outputSamples = new JArray();

                    network.ClearSignals();
                    Console.WriteLine("Testclear " + t + " :" + "for" + n);

                    //var saveInputs = new float[inputCount];

                    for (int ins = 0; ins < inputCount; ins++)
                    {
                        //inputs from -1,1
                        var inF = (float)(2 * r.NextDouble() - 1);
                        //saveInputs[ins] = inF;
                        network.SetInputSignal(ins, inF);
                        //add our random input
                        inputSamples.Add(JToken.FromObject(inF));
                    }

                    Console.WriteLine("Testrecursive next" + t + " :" + "for" + n);

                    //run our network, and save the response
                    ((ModularNetwork)network).RecursiveActivation();
                    //network.MultipleSteps(30);

                    Console.WriteLine("recursive done " + t + " :" + "for" + n);

                    //var saveOuts = new float[outputCount];

                    for (var outs = 0; outs < outputCount; outs++)
                    {
                        //saveOuts[outs] = network.GetOutputSignal(outs);
                        //keep our outputs in an output array
                        outputSamples.Add(JToken.FromObject(network.GetOutputSignal(outs)));
                    }

                    //network.ClearSignals();
                    //network.SetInputSignals(saveInputs);
                    //network.MultipleSteps(30);
                    ////((ModularNetwork)network).RecursiveActivation();
                    //for (var outs = 0; outs < outputCount; outs++)
                    //{
                    //    Console.WriteLine("Difference in activation: " + Math.Abs(network.GetOutputSignal(outs) - saveOuts[outs]));
                    //}


                    Console.WriteLine("test reached past outputs " + t + " :" + "for" + n);

                    JObject test = new JObject();
                    test.Add("inputs", inputSamples);
                    test.Add("outputs", outputSamples);
                    inputsAndOutputs.Add(test);
                    Console.WriteLine("Ins/outs done " + t + " :" + "for" + n);

                }

                Console.WriteLine("tests ended:" + n);

                //we add our inputs/outs for this json network
                networkJSON.Add("tests", inputsAndOutputs);

                //finally, we add our network json to the network array
                networkArray.Add(networkJSON);

                Console.WriteLine("Network finished:" + n);

            }

            Console.WriteLine("All newtorks finished, cleaning up");

            //add our networks, and add our meta information
            root.Add("networks", networkArray);
            root.Add("meta", meta);

            //and away we go! Let's save to file!


            using (System.IO.StreamWriter file = new System.IO.StreamWriter("testgenomes.json"))
            {
                file.WriteLine(root.ToString());
            }


        }


        bool connected = false;

        //private void ConnectButton_Click(object sender, RoutedEventArgs e)
        //{
        //    if (!connected)
        //        simpleCom.Execute();
        //    else
        //        simpleCom.Close();
        //}

        //void socketOpened(object sender, EventArgs e)
        //{
        //    connected = true;

        //    this.Dispatcher.Invoke((Action)delegate() {
        //        PingButton.IsEnabled = true;
        //        //this.ConnectButton.Content = "Disconnect";
        //        this.ConnectNoveltyButton.Content = "Disconnect Novelty";
        //    });

          
        //}
        //void socketClose(object sender, EventArgs e)
        //{
        //    this.Dispatcher.Invoke((Action)delegate()
        //    {
        //        connected = false;
        //        PingButton.IsEnabled = false;
        //        //this.ConnectButton.Content = "Connect";
        //        this.ConnectNoveltyButton.Content = "Connect With Novelty";
        //    });

           

        //}

        //private void PingButton_Click(object sender, RoutedEventArgs e)
        //{
        //    //simpleCom.callNode();
        //    simpleCom.testGenome();
        //}

        //private void ConnectNoveltyButton_Click(object sender, RoutedEventArgs e)
        //{
        //    if (!connected)
        //        simpleCom.Execute(true);
        //    else
        //        simpleCom.Close();
        //}

    }
}
