using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using SocketIOClient;
using NodeCommunicator.Evolution;
using Newtonsoft.Json;
using SharpNeatLib.Masters;
using Newtonsoft.Json.Linq;
using Accord.Statistics.Analysis;
using Accord.Statistics.Kernels;
using System.Windows;
using SharpNeatLib.Novelty;
using SharpNeatLib.NeatGenome;
using SharpNeatLib.Evolution;
using System.Diagnostics;
using Awesomium.sockets;
using System.IO;
using System.Reflection;
using SharpNeatLib.NeatGenome.Xml;
using System.Xml;
using SharpNeatLib.Xml;

namespace NodeCommunicator
{

    public struct PCAData2D
    {
        public long uid;
        public double absoluteFitness;

        public double x;
        public double y;

        public int xBin;
        public int yBin;

    }

    public delegate void JSONDynamicEventHandler(dynamic jsonVar);
    /// <summary>
    /// Example usage class for SocketIO4Net
    /// </summary>
    public class SimpleCommunicator
    {
        //EventHandler onOpen, EventHandler onClose, 
        public SimpleCommunicator(SimplePrinter printer)
        {
            //openSocket += onOpen;
            //closeSocket += onClose;
            print = printer;
            simpleExperiment = new SimpleExperiment();
            simpleExperiment.setCommunicator(this);

            //create directories to save our objects inside
            //createExperimentalDirectories();
        }

        #region Initialize Storage Directories

        static string experimentFolder;
        static string noveltyFolder;
        public void createExperimentalDirectories()
        {
            //don't do this more than once
            if (experimentFolder != null)
                return;

            string currentAssemblyDirectoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var now = DateTime.Now;
            int cnt = 0;
            string expName = "exp_" + now.Month + "_" + now.Day + "_" + now.Hour + "_" + now.Minute + "_" + cnt++;

            experimentFolder = currentAssemblyDirectoryName + "/" + expName;

            while (Directory.Exists(experimentFolder))
            {
                experimentFolder = experimentFolder.Substring(0, experimentFolder.LastIndexOf("_")) + "_" + cnt++;
            }

            //add the final touch
            experimentFolder += "/";
            noveltyFolder = experimentFolder + "novelty/";
            Directory.CreateDirectory(experimentFolder);

            //create novelty folder inside our experiment folder -- to store our archive
            Directory.CreateDirectory(noveltyFolder);
        }

        #endregion

        SimplePrinter print;

        //EventHandler openSocket;
        //EventHandler closeSocket;

        public SimpleExperiment simpleExperiment;
        public JObject loadedExperimentData;
        //Client socket;

        //string formattedSocketMessage(int? ackID, string jsonMessage)
        //{
        //    return string.Format("{0}:::{1}+[{2}]", (int)SocketIOMessageTypes.ACK, ackID, jsonMessage);
        //}

        static int saveCount = 0;
        static HashSet<long> savedArchiveGenomes = new HashSet<long>();
        static HashSet<long> savedGenomes = new HashSet<long>();


        XmlElement GenomeListToXML(string elementName, XmlDocument doc, List<NeatGenome> genomesToSave, bool append)
        {
            XmlElement networkList = doc.CreateElement(elementName);

            //loop through, see who we need to save all together
            for (var i = 0; i < genomesToSave.Count; i++)
            {
                var g = genomesToSave[i];

                //element to hold our genome
                XmlNode ngElement = XmlUtilities.AddElement(networkList, "network");

                //write this to the element
                XmlGenomeWriterStatic.Write(ngElement, g);

                //then add the element to the network list
                networkList.AppendChild(ngElement);
            }

            if(append)
                doc.AppendChild(networkList);

            return networkList;
        }
        object saveLock = new object();


        void writePCAToXML(XmlElement e, PCAData2D data)
        {
            XmlUtilities.AddAttribute(e, "uid", data.uid.ToString());
            XmlUtilities.AddAttribute(e, "x", data.x.ToString());
            XmlUtilities.AddAttribute(e, "y", data.y.ToString());
            XmlUtilities.AddAttribute(e, "xBin", data.xBin.ToString());
            XmlUtilities.AddAttribute(e, "yBin", data.yBin.ToString());
        }
        void saveArchiveAndPCAGenomes(List<PCAData2D> rawPCAToSave)
        {
            //lets lock this up so no issues with multiple threads saving at the same time
            lock (saveLock)
            {

                EvolutionManager evoManager = EvolutionManager.SharedEvolutionManager;
                List<NeatGenome> archiveToSave = new List<NeatGenome>();
                List<NeatGenome> genomesToSave = new List<NeatGenome>();

                //grab our list of potentials from the pca info
                List<long> potentialIdentifiers = new List<long>();

                for (var i = 0; i < rawPCAToSave.Count; i++)
                {
                    var pcaObject = JObject.FromObject(rawPCAToSave[i]);

                    //this is our unique identifier
                    var uuid = (long)pcaObject["uid"];

                    if (!savedGenomes.Contains(uuid))
                    {
                        //add the genome for saving
                        genomesToSave.Add((NeatGenome)evoManager.getGenomeFromID(uuid));
                    }
                }

                var fullArchive = simpleExperiment.GetNoveltyGenomes();

                for (int i = 0; i < fullArchive.Count; i++)
                {
                    IGenome g = fullArchive[i];
                    if (!savedArchiveGenomes.Contains(g.GenomeId))
                    {
                        //add this guy for saving
                        archiveToSave.Add((NeatGenome)g);
                    }
                }

                //now we have all the genomes we want to save
                //and all the archive objects we need to save


                if (genomesToSave.Count > 0)
                {
                    XmlDocument doc = new XmlDocument();
                    //create list and append to doc
                    GenomeListToXML("Networks", doc, genomesToSave, true);

                    //doc has all the genomes we need to save
                    doc.Save(experimentFolder + "genomes_" + saveCount + "_count_" + genomesToSave.Count + ".xml");
                }

                if (archiveToSave.Count > 0)
                {
                    //now we do the archive
                    XmlDocument noveltyDoc = new XmlDocument();

                    //then add our archive objects
                    //create list and append to doc
                    GenomeListToXML("Archive", noveltyDoc, archiveToSave, true);

                    //j = JObject.FromObject(noveltyDoc);

                    //now save the novelty document
                    noveltyDoc.Save(noveltyFolder + "archive_" + saveCount + "_count_" + archiveToSave.Count + ".xml");
                }


                //and finally, our current population
                var pop = simpleExperiment.CurrentPopulation().Select(x => (NeatGenome)x).ToList<NeatGenome>();
                if (pop.Count > 0)
                {
                    XmlDocument popDoc = new XmlDocument();
                    //save the pop inside the popdoc
                    GenomeListToXML("Networks", popDoc, pop, true);

                    //doc has all the genomes we need to save -- just keep saving in the same place, we can't reconstruct anything but the last one anyways
                    popDoc.Save(experimentFolder + "_multipopulation"  + "_" + saveCount + ".xml");
                }

                XmlDocument pcaDoc = new XmlDocument();
                XmlElement pcaElements = pcaDoc.CreateElement("Points");
                foreach (var pca in rawPCAToSave)
                {
                   //element to hold our genome
                    XmlElement pEl = XmlUtilities.AddElement(pcaElements, "point");

                    //convert pca to xml
                    writePCAToXML(pEl, pca);

                    //append!
                    pcaElements.AppendChild(pEl);
                }
                //append to pca doc
                pcaDoc.AppendChild(pcaElements);

                //then save -- don't overwrite, just keep saving pca displays over time
                pcaDoc.Save(experimentFolder + "pcaData_" + saveCount + ".xml");


                //up the save count, we jsut saved a bunch
                saveCount++;

                //mark these objects as having been saved
                foreach (var ng in genomesToSave)
                    savedGenomes.Add(ng.GenomeId);

                //mark the archive objects as having been saved
                foreach (var ng in archiveToSave)
                    savedArchiveGenomes.Add(ng.GenomeId);
            }

        }

        public void Execute(bool startNovelty = false)
        {

            //create directories to save our objects inside
            createExperimentalDirectories();

            Console.WriteLine("Starting TestSocketIOClient Example...");

            //socket = new Client("http://localhost:3000/"); // url to the nodejs / socket.io instance

            //socket.Opened += SocketOpened;


            //socket.Message += SocketMessage;
            //socket.SocketConnectionClosed += SocketConnectionClosed;


            //socket.Error += SocketError;

            MasterSocketManager.registerCallback("savePCANatively", new SocketFunctionCall(
                (JObject functionParams) => 
                {
                     var arguments = JArray.FromObject(functionParams["fArguments"]);



                     return null;
                }));


            



            //An example of how to register a callback to our socket manager
            MasterSocketManager.registerCallback("empty", new SocketFunctionCall(
              (JObject functionParams) =>
              {
                  var arguments = JArray.FromObject(functionParams["fArguments"]);
                  if (arguments.Count > 0)
                  {
                  }
                  return null;
              }));

            //for making a call into the objectID obj, and functionName
            //MasterSocketManager.callDisplayJS("objectID", null, "functionName",
                   //new SocketFunctionCall(
                   //(JObject fp) =>
                   //{
                   //    Console.WriteLine("JS Function return in C#!");
                   //    return null;
                   //}));

            MasterSocketManager.registerCallback("connect", new SocketFunctionCall(
                (JObject functionParams) =>
                {
                    Console.WriteLine("\r\nConnected event...\r\n");
                    Console.WriteLine("Connected To Socket Object");

                    //begin our threaded novelty evaluations on evaluation
                    if (startNovelty && functionParams["socketType"].ToString() == MasterSocketManager.EvaluatorSocket)
                        simpleExperiment.StartNoveltyEvaluations();

                    return null;
                }));

            MasterSocketManager.registerCallback("requestPCAData", new SocketFunctionCall(
            (JObject functionParams) =>
            {
                return loadedExperimentData;
            }));




            MasterSocketManager.registerCallback("start", new SocketFunctionCall(
               (JObject functionParams) =>
               {
                   Console.WriteLine("Request start threaded novelty!");
                   simpleExperiment.StartNoveltyEvaluations();
                   return null;
               }));





            MasterSocketManager.registerCallback("getBodies", new SocketFunctionCall(
            (JObject functionParams) =>
            {
                var arguments = JArray.FromObject(functionParams["fArguments"]);

                if (arguments.Count > 0)
                {
                    //we accept a parameter describing the number of desired bodies
                    int numberOfBodies = (int)arguments[0];

                    //We must generate our collection of bodies and send them back in a JSON object
                    string[] bodies = simpleExperiment.fetchBodies(numberOfBodies);

                    //we have all of our bodies, we must send them back now!
                    //return information to our socket
                    JObject rjo = new JObject();
                    rjo.Add("bodies", JArray.FromObject(bodies));
                    return rjo;
                }

                return null;
            }));

            MasterSocketManager.registerCallback("getGenomes", new SocketFunctionCall(
            (JObject functionParams) =>
            {
                var arguments = JArray.FromObject(functionParams["fArguments"]);
                if (arguments.Count > 0)
                {
                    //we accept a parameter describing the number of desired bodies
                    JArray con = (JArray)arguments[0];

                    List<long> genomeIDs = new List<long>();

                    foreach (var idString in con)
                    {
                        long genomeID;

                        if (long.TryParse(idString.ToString(), out genomeID))
                        {
                            //Console.WriteLine("Genomeid: " + genomeID);
                            genomeIDs.Add(genomeID);
                        }
                        else
                        {
                            throw new Exception("Failure to send appropriate genomeID");
                        }
                    }

                    try
                    {
                        //We must find our collection of bodies and send them back in a JSON object
                        Dictionary<long, string> bodies = simpleExperiment.fetchBodiesFromIDs(genomeIDs);

                        print.WriteLine("Sending bodies: " + JsonConvert.SerializeObject(bodies.Keys));
                        //we have all of our bodies, we must send them back now!
                        //return information to our socket
                        return JObject.FromObject(bodies);
                    }
                    catch (Exception e)
                    {
                        //we failed to fetch the bodies, throw an error, this is some serious stuff!
                        Console.WriteLine("Failed to fetch bodies and send response");
                        Console.WriteLine(e.Message);
                        Console.WriteLine(e.StackTrace);
                        throw e;
                    }

                }
                //no args? we dun her
                return null;
            }));


            MasterSocketManager.registerCallback("getBest", new SocketFunctionCall(
              (JObject functionParams) =>
              {
                  var arguments = JArray.FromObject(functionParams["fArguments"]);
                  if (arguments.Count > 0)
                  {
                      long lastQueryTime = (long)arguments[0];
                      //long.TryParse(fn.Json.Args[0], out lastQueryTime);

                      //get our best bodies
                      var bodyAndTime = simpleExperiment.fetchBestBodies(lastQueryTime);

                      //we have all of our bodies, we must send them back now!
                      print.WriteLine("Sending best: " + JsonConvert.SerializeObject(bodyAndTime.Second.Keys));

                      //we have all of our bodies, we must send them back now!
                      //return information to our socket
                      return JObject.FromObject(bodyAndTime);
                  }
                  return null;

              }));

            MasterSocketManager.registerCallback("runFullPCA", new SocketFunctionCall(
                      (JObject functionParams) =>
                      {
                          if (!simpleExperiment.isRunning)
                          {
                              return null;
                          }

                          var arguments = JArray.FromObject(functionParams["fArguments"]);
                          if (arguments.Count > 0)
                          {
                              int xBins, yBins, genomeSelector;
                              double topPercent;
                              bool firstBehavior;
                              bool.TryParse(arguments[0].ToString(), out firstBehavior);
                              int.TryParse(arguments[1].ToString(), out xBins);
                              int.TryParse(arguments[2].ToString(), out yBins);
                              int.TryParse(arguments[3].ToString(), out genomeSelector);
                              double.TryParse(arguments[4].ToString(), out topPercent);

                              //want to send in custom params for selecting what method to grab genomes (or to send a genome array)

                              //if (fn.Json.Args.Length > 0)
                              //{
                              //    //we accept a parameter describing the number of desired bodies
                              //    JArray con = (JArray)fn.Json.Args[3];


                              // Create new stopwatch
                              Stopwatch stopwatch = new Stopwatch();

                              // Begin timing
                              stopwatch.Start();

                              //grab ALL of our saved genomes, and make sure they are unique individuals
                              //we group them by a genome ID, then we select the first object in the group, voila, distinct!
                              List<NeatGenome> allGenomes;

                              switch (genomeSelector)
                              {
                                  case 0:
                                      allGenomes = simpleExperiment.fetchAllGenomes().GroupBy(g => g.GenomeId).Select(group => group.First()).ToList();
                                      break;
                                  case 1:
                                      allGenomes = simpleExperiment.fetchBestGenomes(topPercent).GroupBy(g => g.GenomeId).Select(group => group.First()).ToList();
                                      break;

                                  default:
                                      allGenomes = simpleExperiment.fetchAllGenomes().GroupBy(g => g.GenomeId).Select(group => group.First()).ToList();
                                      break;

                              }


                              //EvolutionManager.SharedEvolutionManager.saveGenomes(allGenomes);


                              // Begin timing
                              stopwatch.Stop();

                              Console.WriteLine("Fetch Genomes: " + stopwatch.ElapsedMilliseconds);

                              var uidAndPoints = runPCA(allGenomes, firstBehavior, xBins, yBins);

                              //save all genomes associated with the results of this pca, as well as the archive!
                              saveArchiveAndPCAGenomes(uidAndPoints);


                              try
                              {
                                  //we have all of our bodies, we must send them back now!
                                  print.WriteLine("Sending pca analysis for IDs: " + JsonConvert.SerializeObject(uidAndPoints));

                                  if (uidAndPoints == null)
                                      return null;

                                  //we have all of our bodies, we must send them back now!
                                  //return information to our socket
                                  JObject rjo = new JObject();
                                  rjo.Add("pcaData", JArray.FromObject(uidAndPoints));
                                  return rjo;
                              }
                              catch (Exception e)
                              {
                                  //we failed to fetch the bodies, throw an error, this is some serious stuff!
                                  Console.WriteLine("Failed to fetch bodies and send response");
                                  Console.WriteLine(e.Message);
                                  Console.WriteLine(e.StackTrace);
                                  throw e;
                              }
                          }

                          return null;
                      }));


            MasterSocketManager.registerCallback("runPCA", new SocketFunctionCall(
              (JObject functionParams) =>
              {

                  //grab the best individuals (already having been evaluated, we grab their behavior and PCA

                  //get our best bodies
                  List<NeatGenome> topBodyAndTime = simpleExperiment.fetchBestGenomes(.1);

                  var uidAndPoints = runPCA(topBodyAndTime);

                  try
                  {
                      //we have all of our bodies, we must send them back now!
                      print.WriteLine("Sending pca analysis for IDs: " + JsonConvert.SerializeObject(uidAndPoints));

                      if (uidAndPoints == null)
                          return null;

                      //now return information to our socket
                      JObject rjo = new JObject();
                      rjo.Add("pcaData", JArray.FromObject(uidAndPoints));
                      return rjo;
                  }
                  catch (Exception e)
                  {
                      //we failed to fetch the bodies, throw an error, this is some serious stuff!
                      Console.WriteLine("Failed to fetch bodies and send response");
                      Console.WriteLine(e.Message);
                      Console.WriteLine(e.StackTrace);
                      throw e;
                  }

              }));



            //get archive ids
            MasterSocketManager.registerCallback("getArchiveIDs", new SocketFunctionCall(
              (JObject functionParams) =>
              {
                  //let's go fetch our novelty ids!
                  List<long> archiveIDs = simpleExperiment.GetNoveltyArchive();

                  if (archiveIDs != null)
                  {
                      //we have all of our ids, we must send them back now!
                      JObject rjo = new JObject();
                      rjo.Add("archiveIDs", JArray.FromObject(archiveIDs));
                      return rjo;
                  }

                  return null;
              }));


            //An example of how to register a callback to our socket manager
            MasterSocketManager.registerCallback("getCurrentIDs", new SocketFunctionCall(
              (JObject functionParams) =>
              {
                  //let's go fetch our novelty ids!
                  List<long> currentIDs = simpleExperiment.GetMultiCurrentGeneration();

                  if (currentIDs != null)
                  {
                      //we have all of our ids, we must send them back now!
                      JObject rjo = new JObject();
                      rjo.Add("archiveIDs", JArray.FromObject(currentIDs));
                      return rjo;
                  }

                  //if we don't make it above...
                  return null;

              }));



            //// register for 'connect' event with io server
            //socket.On("connect", (fn) =>
            //{
            //    Console.WriteLine("\r\nConnected event...\r\n");
            //    Console.WriteLine("Connected To Socket Object");

            //    //begin our threaded novelty evaluations on connect
            //    if (startNovelty)
            //        simpleExperiment.StartNoveltyEvaluations();
            //});

            //socket.On("toggleNetwork", (fn) =>
            //    {
            //        print.WriteLine("Incoming select: " + fn.Json.ToJsonString());

            //        //we see an incoming request for selection
            //        long selectionID = (long)fn.Json.Args[0];

            //        //we have the ID, now we must do some toggling
            //        //we could call the evolution manager directly, but perhaps it's best to separate these objects? In case there are many experiements or someting?
            //        //or maybe the experiment wants to not the selection information
            //        bool toggle = simpleExperiment.toggleNetwork(selectionID);

            //        if (toggle)
            //            print.WriteLine("Successful toggle, as suspected");

            //        EvolutionManager eManager = EvolutionManager.SharedEvolutionManager;


            //        string isSelected = string.Format("{0}:::{1}+[{2}]", (int)SocketIOMessageTypes.ACK, fn.AckId, JsonConvert.SerializeObject(eManager.isSelected(selectionID)));

            //        print.WriteLine("Responding to toggleNetwork with: " + JsonConvert.SerializeObject(eManager.isSelected(selectionID)));

            //        socket.Send(isSelected);

            //    });
            ////you simply make a request for the next body, no 
            //socket.On("getBodies", (fn) =>
            //{
            //    if (fn.Json.Args.Length > 0)
            //    {
            //        //we accept a parameter describing the number of desired bodies
            //        int numberOfBodies = (int)fn.Json.Args[0];

            //        //We must generate our collection of bodies and send them back in a JSON object
            //        string[] bodies = simpleExperiment.fetchBodies(numberOfBodies);

            //        //we have all of our bodies, we must send them back now!
            //        string bodiesMessage = formattedSocketMessage(fn.AckId, JsonConvert.SerializeObject(bodies));

            //        //now return information to our socket
            //        socket.Send(bodiesMessage);

            //    }
            //});
            //socket.On("getGenomes", (fn) =>
            //{
            //    if (fn.Json.Args.Length > 0)
            //    {
            //        //we accept a parameter describing the number of desired bodies
            //        JArray con = (JArray)fn.Json.Args[0];

            //        List<long> genomeIDs = new List<long>();

            //        foreach (var idString in con)
            //        {
            //            long genomeID;

            //            if (long.TryParse(idString.ToString(), out genomeID))
            //            {
            //                //Console.WriteLine("Genomeid: " + genomeID);
            //                genomeIDs.Add(genomeID);
            //            }
            //            else
            //            {
            //                throw new Exception("Failure to send appropriate genomeID");
            //            }
            //        }

            //        try
            //        {
            //            //We must find our collection of bodies and send them back in a JSON object
            //            Dictionary<long, string> bodies = simpleExperiment.fetchBodiesFromIDs(genomeIDs);

            //            //we have all of our bodies, we must send them back now!
            //            string bodiesMessage = formattedSocketMessage(fn.AckId, JsonConvert.SerializeObject(bodies));

            //            print.WriteLine("Sending bodies: " + JsonConvert.SerializeObject(bodies.Keys));

            //            //now return information to our socket
            //            socket.Send(bodiesMessage);
            //        }
            //        catch (Exception e)
            //        {
            //            //we failed to fetch the bodies, throw an error, this is some serious stuff!
            //            Console.WriteLine("Failed to fetch bodies and send response");
            //            Console.WriteLine(e.Message);
            //            Console.WriteLine(e.StackTrace);
            //            throw e;
            //        }

            //    }
            //});
            //socket.On("getBest", (fn) =>
            //{
            //    if (fn.Json.Args.Length > 0)
            //    {
            //        long lastQueryTime = (long)fn.Json.Args[0];
            //        //long.TryParse(fn.Json.Args[0], out lastQueryTime);

            //        //get our best bodies
            //        var bodyAndTime = simpleExperiment.fetchBestBodies(lastQueryTime);

            //        //we have all of our bodies, we must send them back now!
            //        string bodiesMessage = formattedSocketMessage(fn.AckId, JsonConvert.SerializeObject(bodyAndTime));

            //        print.WriteLine("Sending best: " + JsonConvert.SerializeObject(bodyAndTime.Second.Keys));

            //        //now return information to our socket
            //        socket.Send(bodiesMessage);
            //    }

            //});
            //socket.On("ping", (fn) =>
            //{
            //    print.WriteLine("Incoming Ping: " + fn.Json.ToJsonString());

            //    int value = (int)fn.Json.Args[0];

            //    value = value + 1;

            //    //string jSon = SimpleJson.SimpleJson.SerializeObject(net);

            //    string jSon = simpleExperiment.fetchNextBodyJSON();

            //    //Dictionary<string, object> stuff =new Dictionary<string,object>();
            //    //stuff.Add("c#says", "pong");
            //    //stuff.Add("valuePlusOne", value);
            //    //string data = SimpleJson.SimpleJson.SerializeObject(stuff);

            //    //string.Format("{\"{0}\": \"{1}\", \"{2}\": {3} }", "c#Says", "pong", "value", value);

            //    string tosend = string.Format("{0}:::{1}+[{2}]", (int)SocketIOMessageTypes.ACK, fn.AckId, jSon);

            //    print.WriteLine("Responding to ping with: " + jSon);
            //    socket.Send(tosend);//new AckMessage() { AckId = fn.AckId, MessageText = "pong" });

            //});
            //socket.On("runFullPCA", (fn) =>
            //{

            //    int xBins, yBins, genomeSelector;
            //    double topPercent;
            //    bool firstBehavior;
            //    bool.TryParse(fn.Json.Args[0], out firstBehavior);
            //    int.TryParse(fn.Json.Args[1], out xBins);
            //    int.TryParse(fn.Json.Args[2], out yBins);
            //    int.TryParse(fn.Json.Args[3], out genomeSelector);
            //    double.TryParse(fn.Json.Args[4], out topPercent);

            //    //want to send in custom params for selecting what method to grab genomes (or to send a genome array)

            //    //if (fn.Json.Args.Length > 0)
            //    //{
            //    //    //we accept a parameter describing the number of desired bodies
            //    //    JArray con = (JArray)fn.Json.Args[3];


            //    // Create new stopwatch
            //    Stopwatch stopwatch = new Stopwatch();

            //    // Begin timing
            //    stopwatch.Start();

            //    //grab ALL of our saved genomes, and make sure they are unique individuals
            //    //we group them by a genome ID, then we select the first object in the group, voila, distinct!
            //    List<NeatGenome> allGenomes;

            //    switch (genomeSelector)
            //    {
            //        case 0:
            //            allGenomes = simpleExperiment.fetchAllGenomes().GroupBy(g => g.GenomeId).Select(group => group.First()).ToList();
            //            break;
            //        case 1:
            //            allGenomes = simpleExperiment.fetchBestGenomes(topPercent).GroupBy(g => g.GenomeId).Select(group => group.First()).ToList();
            //            break;

            //        default:
            //            allGenomes = simpleExperiment.fetchAllGenomes().GroupBy(g => g.GenomeId).Select(group => group.First()).ToList();
            //            break;

            //    }


            //    EvolutionManager.SharedEvolutionManager.saveGenomes(allGenomes);


            //    // Begin timing
            //    stopwatch.Stop();

            //    Console.WriteLine("Fetch Genomes: " + stopwatch.ElapsedMilliseconds);



            //    var uidAndPoints = runPCA(allGenomes, firstBehavior, xBins, yBins);

            //    try
            //    {
            //        //we have all of our bodies, we must send them back now!
            //        string bodiesMessage = formattedSocketMessage(fn.AckId, JsonConvert.SerializeObject(uidAndPoints));

            //        print.WriteLine("Sending pca analysis for IDs: " + JsonConvert.SerializeObject(uidAndPoints));

            //        //now return information to our socket
            //        socket.Send(bodiesMessage);
            //    }
            //    catch (Exception e)
            //    {
            //        //we failed to fetch the bodies, throw an error, this is some serious stuff!
            //        Console.WriteLine("Failed to fetch bodies and send response");
            //        Console.WriteLine(e.Message);
            //        Console.WriteLine(e.StackTrace);
            //        throw e;
            //    }


            //});
            //socket.On("runPCA", (empty) =>
            //{
            //    //we accept a parameter describing the number of desired bodies
            //    //JArray con = (JArray)fn.Json.Args[0];


            //    //grab the best individuals (already having been evaluated, we grab their behavior and PCA

            //    //get our best bodies
            //    List<NeatGenome> topBodyAndTime = simpleExperiment.fetchBestGenomes(.1);

            //    var uidAndPoints = runPCA(topBodyAndTime);

            //    try
            //    {
            //        //we have all of our bodies, we must send them back now!
            //        string bodiesMessage = formattedSocketMessage(empty.AckId, JsonConvert.SerializeObject(uidAndPoints));

            //        print.WriteLine("Sending pca analysis for IDs: " + JsonConvert.SerializeObject(uidAndPoints));

            //        //now return information to our socket
            //        socket.Send(bodiesMessage);
            //    }
            //    catch (Exception e)
            //    {
            //        //we failed to fetch the bodies, throw an error, this is some serious stuff!
            //        Console.WriteLine("Failed to fetch bodies and send response");
            //        Console.WriteLine(e.Message);
            //        Console.WriteLine(e.StackTrace);
            //        throw e;
            //    }

            //});

            ////socket.On("evaluateGenomes", (data) =>
            ////{
            ////    print.WriteLine(data);
            ////    //Console.WriteLine("  raw message:      {0}", data.RawMessage);
            ////    //Console.WriteLine("  string message:   {0}", data.MessageText);
            ////    //Console.WriteLine("  json data string: {0}", data.Json.ToJsonString());
            ////    //Console.WriteLine("  json raw:         {0}", data.Json.Args[0]);

            ////});


            //// register for 'update' events - message is a json 'Part' object
            //socket.On("update", (data) =>
            //{
            //    print.WriteLine("recv [socket].[update] event");
            //    //Console.WriteLine("  raw message:      {0}", data.RawMessage);
            //    //Console.WriteLine("  string message:   {0}", data.MessageText);
            //    //Console.WriteLine("  json data string: {0}", data.Json.ToJsonString());
            //    //Console.WriteLine("  json raw:         {0}", data.Json.Args[0]);
            //});

            //// register for 'update' events - message is a json 'Part' object
            //socket.On("getArchiveIDs", (data) =>
            //{
            //    //let's go fetch our novelty ids!
            //    List<long> archiveIDs = simpleExperiment.GetNoveltyArchive();

            //    if (archiveIDs != null)
            //    {
            //        //we have all of our ids, we must send them back now!
            //        string bodiesMessage = formattedSocketMessage(data.AckId, JsonConvert.SerializeObject(archiveIDs));

            //        socket.Send(bodiesMessage);
            //    }


            //});

            //// Grabs the current generation ids for viewing purposes!
            //socket.On("getCurrentIDs", (data) =>
            //{
            //    //let's go fetch our novelty ids!
            //    List<long> currentIDs = simpleExperiment.GetMultiCurrentGeneration();

            //    if (currentIDs != null)
            //    {
            //        //we have all of our ids, we must send them back now!
            //        string bodiesMessage = formattedSocketMessage(data.AckId, JsonConvert.SerializeObject(currentIDs));

            //        socket.Send(bodiesMessage);
            //    }


            //});


            // make the socket.io connection
            //socket.Connect();
        }


        //			testHold.WaitOne(5000);
        //IEndPointClient logger;

        List<PCAData2D> runPCA(List<NeatGenome> bestGenome, bool firstBehavior = true, int xBins = 0, int yBins = 0)
        {
            var totalStopWatch = System.Diagnostics.Stopwatch.StartNew();

            // Create new stopwatch
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            List<long> uIDs = bestGenome.Select(x => x.GenomeId).ToList();
            //make sure we have the right fitness!
            //TODO: Check multi-objective code to see what value has absolute fitness
            List<double> absoluteFitness = bestGenome.Select(x => x.RealFitness).ToList();

            if (bestGenome.Count == 0)
                return null;

            //we know topBody > 0 by above check 
            int componentCount = Math.Min(80, (firstBehavior ? bestGenome[0].Behavior.behaviorList.Count : bestGenome[0].SecondBehavior.behaviorList.Count));
            //double componentCount = (double)fn.Json.Args[1];

            //create our double array that's going to be condensed
            double[,] collectedData = new double[bestGenome.Count, componentCount];

            int xyIndex = 0;
            foreach (IGenome genome in bestGenome)
            {
                //need to grab the behavior objects from the genome, and enter them as data
                var behaviorList = (firstBehavior ? genome.Behavior.behaviorList : genome.SecondBehavior.behaviorList);
                for (var ix = 0; ix < componentCount; ix++)
                {
                    collectedData[xyIndex, ix] = (double)behaviorList[ix];
                }

                xyIndex++;
            }

            try
            {

                stopwatch.Stop();
                Console.WriteLine("Time before kernel: " + stopwatch.ElapsedMilliseconds);
                stopwatch = System.Diagnostics.Stopwatch.StartNew();

                //higher gaussian seemed better at spreading out behavior
                //might try polynomial of 3rd or 4th degree, constant = 0 by default

                //        IKernel kernel = new Polynomial(3, 0);//new Gaussian(1.9);//new Polynomial((int)numDegree.Value, (double)numConstant.Value);

                //        KernelPrincipalComponentAnalysis kpca = new KernelPrincipalComponentAnalysis(collectedData, kernel,
                //(PrincipalComponentAnalysis.AnalysisMethod.Correlation));

                PrincipalComponentAnalysis kpca = new PrincipalComponentAnalysis(collectedData,
        (PrincipalComponentAnalysis.AnalysisMethod.Correlation));
                try
                {
                    kpca.Compute();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return null;
                }

                stopwatch.Stop();
                Console.WriteLine("Time During PCA: " + stopwatch.ElapsedMilliseconds);
                stopwatch = System.Diagnostics.Stopwatch.StartNew();

                double[,] transform = kpca.Transform(collectedData, 2);

                stopwatch.Stop();
                Console.WriteLine("Time During Transform: " + stopwatch.ElapsedMilliseconds);
                stopwatch = System.Diagnostics.Stopwatch.StartNew();

                List<PCAData2D> uidAndPoints = binAllPoints(transform, uIDs, absoluteFitness, xBins, yBins);

                stopwatch.Stop();
                Console.WriteLine("Time During Binning: " + stopwatch.ElapsedMilliseconds);


                //List<PCAData2D> uidAndPoints = new List<PCAData2D>();
                //for (int ix = 0; ix < bestGenome.Count; ix++)
                //{
                //    uidAndPoints.Add(new PCAData2D() { uid = uIDs[ix], x = mappedResults[ix, 0], y = mappedResults[ix, 1] });
                //}

                totalStopWatch.Stop();
                Console.WriteLine("Total Time For PCA: " + totalStopWatch.ElapsedMilliseconds);

                return uidAndPoints;
            }
            catch (Exception e)
            {
                totalStopWatch.Stop();
                Console.WriteLine("Total Time For (failed) PCA: " + totalStopWatch.ElapsedMilliseconds);

                Console.WriteLine("Failed to run PCA");
                return null;
            }

        }
        List<PCAData2D> binAllPoints(double[,] pcaData, List<long> uIDs, List<double> absoluteFitness, int xBins, int yBins)
        {
            List<PCAData2D> binnedData = new List<PCAData2D>();
            double minX = double.MaxValue;
            double minY = double.MaxValue;
            double maxX = double.MinValue;
            double maxY = double.MinValue;
            double xRange, yRange;

            int numGenomes = pcaData.GetLength(0);
            //check max and min range
            for (var c = 0; c < numGenomes; c++)
            {
                minX = Math.Min(minX, pcaData[c, 0]);
                maxX = Math.Max(maxX, pcaData[c, 0]);


                minY = Math.Min(minY, pcaData[c, 1]);
                maxY = Math.Max(maxY, pcaData[c, 1]);

                binnedData.Add(new PCAData2D() { absoluteFitness = absoluteFitness[c], uid = uIDs[c], x = pcaData[c, 0], y = pcaData[c, 1] });
            }

            if (xBins == 0 || yBins == 0)
                return binnedData;

            xRange = maxX - minX;
            yRange = maxY - minY;

            Dictionary<string, List<PCAData2D>> dictBinObjects = new Dictionary<string, List<PCAData2D>>();

            int xBin, yBin;
            PCAData2D bData; string binString;
            List<PCAData2D> binList;
            //loop through again, this time doing the binning
            for (var c = 0; c < numGenomes; c++)
            {
                bData = binnedData[c];

                double normX = (bData.x - minX) / xRange;
                double normY = (bData.y - minY) / yRange;

                xBin = (int)Math.Min(Math.Floor(normX * xBins), xBins - 1);
                yBin = (int)Math.Min(Math.Floor(normY * yBins), yBins - 1);

                bData.xBin = xBin;
                bData.yBin = yBin;
                binString = "" + xBin + yBin;

                if (!dictBinObjects.TryGetValue(binString, out binList))
                {
                    binList = new List<PCAData2D>();
                    dictBinObjects.Add(binString, binList);
                }

                binList.Add(bData);

            }

            //now all binned inside of dictBinObjects, need to choose highest absolute fitness
            //use some ballllller LINQ 

            return dictBinObjects.Select(binObject =>
            {
                //find the max value of each bin
                double maxValue = binObject.Value.Max(data => data.absoluteFitness);
                //whoever is the first genome to have max value, return that individual for this bin
                return binObject.Value.First(data => data.absoluteFitness == maxValue);
                //finally, we just turn this into a list of objects, and return!
            }).ToList();

        }
        //public void callNode()
        //{
        //    if (socket != null)
        //    {
        //        print.WriteLine("Pinging node server");

        //        socket.Emit("pingServer", "hello", "", (fn) =>
        //        {
        //            print.WriteLine("Server says - actually I don't know" + fn.ToString());
        //        });
        //    }
        //}
        //public void callEventWithJSON(string socketEventName, string jsonString, JSONDynamicEventHandler eventHandler)
        //{
        //    if (socket != null)
        //    {
        //        print.WriteLine("Calling Event: " + socketEventName);

        //        socket.Emit(socketEventName, jsonString, "", (fn) =>
        //        {
        //            eventHandler(fn);
        //        });
        //    }
        //}
        public void printString(string line)
        {
            print.WriteLine(line);
        }
        //void SocketError(object sender, ErrorEventArgs e)
        //{

        //    print.WriteLine("socket client error:");
        //    Console.WriteLine(e.Message);
        //}

        //void SocketConnectionClosed(object sender, EventArgs e)
        //{
        //    print.WriteLine("WebSocketConnection was terminated!");

        //    //if (closeSocket != null)
        //    //    closeSocket(this, e);
        //}

        //void SocketMessage(object sender, MessageEventArgs e)
        //{
        //    print.WriteLine("socket message:" + e.Message.MessageText);
        //    // uncomment to show any non-registered messages
        //    //if (string.IsNullOrEmpty(e.Message.Event))
        //    //    Console.WriteLine("Generic SocketMessage: {0}", e.Message.MessageText);
        //    //else
        //    //    Console.WriteLine("Generic SocketMessage: {0} : {1}", e.Message.Event, e.Message.JsonEncodedMessage.ToJsonString());
        //}

        //void SocketOpened(object sender, EventArgs e)
        //{
        //    print.WriteLine("socket opened");
        //    if (openSocket != null)
        //        openSocket(this, e);

        //}

        //public void Close()
        //{
        //    if (this.socket != null)
        //    {
        //        socket.Opened -= SocketOpened;
        //        socket.Message -= SocketMessage;
        //        socket.SocketConnectionClosed -= SocketConnectionClosed;
        //        socket.Error -= SocketError;
        //        if (closeSocket != null)
        //            closeSocket(this, null);
        //        this.socket.Dispose(); // close & dispose of socket client
        //    }
        //}

        internal void testGenome()
        {
            simpleExperiment.StartNoveltyEvaluations();
        }
    }
}
