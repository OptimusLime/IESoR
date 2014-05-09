using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpNeatLib.Evolution;
using SharpNeatLib.NeuralNetwork;
using SharpNeatLib;
using SharpNeatLib.NeatGenome;
using Newtonsoft.Json;
using SharpNeatLib.CPPNs;
using System.Drawing;
using SharpNeatLib.Experiments;
using SharpNeatLib.Masters;
using System.Diagnostics;
using System.Threading;
using SharpNeatLib.Novelty;
using PointPair = System.Collections.Generic.KeyValuePair<System.Drawing.PointF, System.Drawing.PointF>;
using Awesomium.sockets;

namespace NodeCommunicator.Evolution
{
    public class NoveltyThread 
    {
        bool waitNextTime = false;
        AutoResetEvent autoEvent;
        Thread novelThread;
        EvolutionAlgorithm noveltyRun;
        int populationSize;
        IPopulationEvaluator popEval;

        public NoveltyThread(JSPopulationEvaluator jsPop, AssessGenotypeFunction assess, int popSize)
        {
            //save our objects for executing later!
            popEval = jsPop;
            populationSize = popSize;

            autoEvent = new AutoResetEvent(false);

            
            waitNextTime = true;

            novelThread = new Thread(delegate()
                {
                    autoEvent.WaitOne();
                    
                    //we'll start by testing with 0 parents, and popsize of 15 yay!
                    noveltyRun = EvolutionManager.SharedEvolutionManager.initializeEvolutionAlgorithm(popEval, populationSize, assess);

                    //let our algoirhtm know we want to do novelty gosh darnit
                    if(noveltyRun.multiobjective != null)
                         noveltyRun.multiobjective.doNovelty = true;

                    //we make sure we don't wait in this loop, since we just got started!
                    waitNextTime = false;

                    while (true)
                    {
                        //this will cause us to pause!
                        if (waitNextTime)
                        {
                            waitNextTime = false;       
                            autoEvent.WaitOne();                     
                        }
                        // Start the stopwatch we'll use to measure eval performance
                        Stopwatch sw = Stopwatch.StartNew();

                        //run the generation
                        noveltyRun.PerformOneGeneration();

                        //start on the second generation please!
                        if ((noveltyRun.Generation +1) % 4 == 0)
                            MasterSocketManager.callDisplayJS("startPCA", null, "window");

                        noveltyRun.multiobjective.nov.addPendingTournament();

                        // Stop the stopwatch
                        sw.Stop();

                        // Report the results
                        Console.WriteLine("Time used per gen (float): {0} ms", sw.Elapsed.TotalMilliseconds);
                        Console.WriteLine("Time used per gen (rounded): {0} ms", sw.ElapsedMilliseconds);

                    }
                });

            novelThread.Start();

        }
        public EvolutionAlgorithm EA
        {
            get { return noveltyRun; }
        }
        public void StartNovelty()
        {
            autoEvent.Set();
        }
        public void PauseNovelty()
        {
            waitNextTime = true;       
        }

    }
    public class ESBodyInformation 
    {
        public List<PointF> InputLocations;
        public List<PointF> HiddenLocations;
        //public List<PointF> PreHiddenLocations;
        public long GenomeID;
        public bool useLEO;

        public double[] Objectives;
        public double Fitness;
        public double Locality;
        public int BeforeNeuron;
        public int BeforeConnection;

        //public List<PointPair> AllBodyInputs = new List<PointPair>();
        //public List<List<float>> AllBodyOutputs = new List<List<float>>();
        public Dictionary<int, int> indexToConnection = new Dictionary<int, int>();

        //eventually this will be replaced by body information -- this is just temporarily here so we don't
        //have to heavily modify ES infrastructure
        public ConnectionGeneList Connections;

    }

    public class SimpleExperiment
    {
        SimpleCommunicator simpleCom;
        //EvoManager evMan;
        //NeatParameters np;
        JSPopulationEvaluator jsPop;
        EvolvableSubstrate esSubstrate = new EvolvableSubstrate() {};
        EvolutionManager evolutionManager = EvolutionManager.SharedEvolutionManager;

        public SimpleExperiment()
        {
            //We need to create some parameters, a population evaluator
            //And then we can create an EvoManager

            jsPop = new JSPopulationEvaluator();

            //we want to use leo
            esSubstrate.useLEO = true;
            evolutionManager.loadSeed("leoEmptySeed.xml");//NoConnections.xml");


            //set up default ins and outs (it's already 4,3 by default, this is just for customization later maybe)
            evolutionManager.setDefaultCPPNs(4, 3);
        }
        #region Select/Deselect
        public bool selectNetwork(string id)
        {
            long uid;
            if (!long.TryParse(id, out uid))
                throw new Exception("Can't read networkID, can't select it!");

            //we have the uid, now lets attempt to select it inside of our object
           return evolutionManager.selectGenome(uid);

        }
        public bool deselectNetwork(string id)
        {
            long uid;
            if (!long.TryParse(id, out uid))
                throw new Exception("Can't read networkID, can't select it!");

            //we have the uid, now lets attempt to select it inside of our object
            return evolutionManager.deselectGenome(uid);

        }
        public bool toggleNetwork(string id)
        {  
            long uid;
            if (!long.TryParse(id, out uid))
                throw new Exception("Can't read networkID, can't select it!");

           return  evolutionManager.toggleSelect(uid);
        }
        public bool toggleNetwork(long uid)
        {
            return evolutionManager.toggleSelect(uid);
        }
        #endregion

        #region Test Evaluate Genomes
        
        NoveltyThread novel;

        public bool isRunning
        {
            get { return (novel != null && novel.EA != null); }
        }

        public void StartNoveltyEvaluations()
        {
            try
            {
                novel = new NoveltyThread(jsPop, this.assessGenome,  120);

                //then we run a single generation by doing this
                novel.StartNovelty();
                
                //we can stop at any time using PauseNovelty(), but we can just leave it running for now
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to create ea:");
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);

            }

        }

        /// <summary>
        /// Get the most up to date list of the archive
        /// </summary>
        /// <returns></returns>
        public List<long> GetNoveltyArchive()
        {
            if (novel == null)
                return null;

            //send back the current archive of IDs, we'll fetch bodies later friend
            return novel.EA.noveltyFixed.archive.Select(genome => (long)genome.GenomeId).ToList();
        }

        public List<IGenome> GetNoveltyGenomes()
        {
            return novel.EA.multiobjective.nov.archive;
        }

        public List<IGenome> CurrentPopulation()
        {
            return novel.EA.multiobjective.population;
        }



        public List<long> GetMultiCurrentGeneration()
        {
            if (novel == null)
                return null;

            //send back the current archive of IDs, we'll fetch bodies later friend
            return novel.EA.multiobjective.population.Select(genome => (long)genome.GenomeId).ToList();
        }

        public Pair<long, Dictionary<long, string>> fetchBestBodies(long lastRequest)
        {
            //get the latest and greatest!
            var pairGenomes = novel.EA.genomeFilter.requestBestAndLatestGenomes(.05, lastRequest);
            
            //turn each genome into a body, return the request time and the dictionary of objects. Communicator will know what to do!
            return new Pair<long, Dictionary<long, string>>(pairGenomes.First, fetchBodiesFromIDs(pairGenomes.Second.Select(x => x.GenomeId)));
        }
        //public List<NeatGenome> getGenomeByID(long id)
        //{
        //    evolutionManager.getGenomeFromID(id);
        //}
        public List<NeatGenome> fetchAllGenomes( long lastRequest = 0)
        {
            return novel.EA.genomeFilter.requestLatestGenomes(lastRequest).Second;
        }
        public List<NeatGenome> fetchBestGenomes(double topPercent, long lastRequest = 0)
        {
            return novel.EA.genomeFilter.requestBestAndLatestGenomes(topPercent, lastRequest).Second;
        }
        public void PauseNoveltyEvaluations()
        {
            //we pause novelty yay? nay? hay? hey? Bay? PEW PEW PEW?!?!?
            novel.PauseNovelty();
        }
        #endregion

        public IGenome fetchNextGenome()
        {
            //we get our next network 
            return evolutionManager.loadNextNetworkFromSelected();

            //we've gotten back a genome, random or not
            //convert into a network, which will be converted to a body
        }

        public Dictionary<long, string> fetchBodiesFromIDs(IEnumerable<long> genomeIDs)
        {
            //this will get me the actual genome
            //I need to convert the genome to a body, and send it back
            //this is annoying
            Dictionary<long, IGenome> genomeDict = evolutionManager.getGenomesFromIDs(genomeIDs);
            return fetchBodiesFromGenomes(genomeDict);
        }
        public Dictionary<long, string> fetchBodiesFromGenomes(Dictionary<long, IGenome> genomeDict)
        {
            Dictionary<long, string> genomeBodies = new Dictionary<long, string>();

            bool isEmpty = false;

            foreach (var idGenomePair in genomeDict)
            {
                genomeBodies.Add(idGenomePair.Key, genomeIntoBodyJSON(idGenomePair.Value, out isEmpty));
            }

            return genomeBodies;
        }

        public string[] fetchBodies(int count)
        {
            string[] bodies = new string[count];

            for (int i = 0; i < count; i++)
            {
                bodies[i] = this.fetchNextBodyJSON();
            }

            return bodies;
        }
        public string fetchNextBodyJSON()
        {
            //we have a genome and a network from the genome
            IGenome genome = fetchNextGenome();
            bool isEmpty;
            var body = genomeIntoBodyJSON(genome, out isEmpty);

            while(isEmpty)
            {
                body = genomeIntoBodyJSON(genome, out isEmpty);
            }

            return body;
        }
        public bool assessGenome(IGenome genome)
        {
            bool isEmpty = true;
            
            //check if the body turns out to be empty
            genomeIntoBodyJSON(genome, out isEmpty);
            
            //if you're not empty, you are good to go!
            return !isEmpty;
        }

        public string genomeIntoBodyJSON(IGenome genome, out bool isEmpty)
        {
            //grab body object then convert the body into JSON
            return JsonConvert.SerializeObject(genomeIntoBodyObject(genome, out isEmpty));
        }

        float dXDistance(int resolution, float mult)
        {
            return mult*2 / ((float)resolution - 1);
        }
        float dYDistance(int resolution, float mult)
        {
            return mult * 2 / ((float)resolution - 1);
        }
        public List<PointF> gridQueryPoints(int resolution)
        {
            float dx = 2 / ((float)resolution - 1);
            float dy = 2 / ((float)resolution - 1);
            float fX = -1; float fY = -1;

            //float threeNodeDistance = (float)Math.Sqrt(9.0f * dx * dx + 9.0f * dy * dy);
            float xDistanceThree = 3 * dx;
            float yDistanceThree = 3 * dy;

            List<PointF> queryPoints = new List<PointF>();

            for (int x = 0; x < resolution; x++)
            {
                for (int y = 0; y < resolution; y++)
                {
                    queryPoints.Add(new PointF(fX, fY));
                    //now increment fy and go again
                    fY += dy;
                }
                //increment dx, run through again
                //reset fy to -1
                fX += dx;
                fY = -1;
            }

            return queryPoints;
        }


        Dictionary<long, ConnectionGeneList> outwardNodeConnections(ConnectionGeneList cgl)
        {
            Dictionary<long, ConnectionGeneList> nodesToOut = new Dictionary<long, ConnectionGeneList>();

            for (var i = 0; i < cgl.Count; i++)
            {
                //grab the connection
                ConnectionGene cg = cgl[i];

                //we actually need to make sure any node that receives a connection must have a connection gene list -- even if it contains nothing
                //i.e. if I go to check how many outgoing connections to a node that only has incoming connections -- if we don't do this step
                //that query will throw a key not found exception in C#
                if (!nodesToOut.ContainsKey(cg.TargetNeuronId))
                    nodesToOut.Add(cg.TargetNeuronId, new ConnectionGeneList());

                if(!nodesToOut.ContainsKey(cg.SourceNeuronId))
                {
                    //add source node, and list of possible connections
                    nodesToOut.Add(cg.SourceNeuronId, new ConnectionGeneList());
                }

                //add this gene to the object holding souce nodes and their outgoing connections
                nodesToOut[cg.SourceNeuronId].Add(cg);
            }

            return nodesToOut;
        }

        Dictionary<long, ConnectionGeneList> inwardNodeConnections(ConnectionGeneList cgl)
        {
            Dictionary<long, ConnectionGeneList> nodesInward = new Dictionary<long, ConnectionGeneList>();

            for (var i = 0; i < cgl.Count; i++)
            {
                //grab the connection
                ConnectionGene cg = cgl[i];

                //we actually need to make sure any node that receives a connection must have a connection gene list -- even if it contains nothing
                //i.e. if I go to check how many outgoing connections to a node that only has incoming connections -- if we don't do this step
                //that query will throw a key not found exception in C#
                if (!nodesInward.ContainsKey(cg.SourceNeuronId))
                    nodesInward.Add(cg.SourceNeuronId, new ConnectionGeneList());

                if (!nodesInward.ContainsKey(cg.TargetNeuronId))
                {
                    //add source node, and list of possible connections
                    nodesInward.Add(cg.TargetNeuronId, new ConnectionGeneList());
                }

                //add this gene to the object holding souce nodes and their outgoing connections
                nodesInward[cg.TargetNeuronId].Add(cg);
            }

            return nodesInward;
        }

        public ESBodyInformation genomeIntoBodyObject(IGenome genome, out bool isEmpty)
        {
            INetwork net = GenomeDecoder.DecodeToModularNetwork((NeatGenome)genome);
            isEmpty = false;

            //we want the genome, so we can acknowledge the genomeID!

            //now convert a network to a set of hidden neurons and connections
            
            //we'll make body specific function calls later
            var allBodyOutputs = new List<List<float>>();
            var allBodyInputs = new List<PointPair>();
            var indexToConnectionMap = new Dictionary<int, int>();

            List<PointF> inputs, outputs, hiddenNeurons;
            inputs = new List<PointF>();
            outputs = new List<PointF>();
            hiddenNeurons = new List<PointF>();

            //inputs.Add(new PointF(0,0));

            //int initialDepth, ESIterations;
            //uint inputCount, outputCount;
            //float varianceThreshold, bandThreshold;

            ConnectionGeneList connections = new ConnectionGeneList();


            //loop through a grid, defined by some resolution, and test every connection against another using leo


            int resolution = 9;
            //int resolutionHalf = resolution / 2;

            List<PointF> queryPoints = gridQueryPoints(resolution);
            float xDistanceThree = dXDistance(resolution, 3.0f);
            float yDistanceThree = dYDistance(resolution, 3.0f);


            bool useLeo = true;

            int counter = 0;
            Dictionary<long, PointF> conSourcePoints = new Dictionary<long, PointF>();
            Dictionary<long, PointF> conTargetPoints = new Dictionary<long, PointF>();


            //Dictionary<string, List<PointF>> pointsChecked = new Dictionary<string, List<PointF>>();
            //List<PointF> pList;
            int src, tgt;
            //for each points we have
            for(int p1=0; p1 < queryPoints.Count; p1++)
            {
                PointF xyPoint = queryPoints[p1];
            
                //query against all other points (possibly limiting certain connection lengths
                for(int p2 = p1; p2 < queryPoints.Count; p2++)
                {
                    PointF otherPoint = queryPoints[p2];

                    if (p1 != p2 && (Math.Abs(xyPoint.X - otherPoint.X) < xDistanceThree && Math.Abs(xyPoint.Y - otherPoint.Y) < yDistanceThree))
                    {
                        //if(!pointsChecked.TryGetValue(xyPoint.ToString(), out pList))
                        //{
                        //    pList = new List<PointF>();
                        //    pointsChecked.Add(xyPoint.ToString(), pList); 
                        //}
                        //pList.Add(otherPoint);
                        
                        //if (!pointsChecked.TryGetValue(otherPoint.ToString(), out pList))
                        //{
                        //    pList = new List<PointF>();
                        //    pointsChecked.Add(otherPoint.ToString(), pList);
                        //}
                        //pList.Add(xyPoint);

                        //Console.WriteLine("Checking: ({0}, {1}) => ({2}, {3}) ", xyPoint.X, xyPoint.Y, otherPoint.X, otherPoint.Y);

                        float[] outs = queryCPPNOutputs((ModularNetwork)net, xyPoint.X, xyPoint.Y, otherPoint.X, otherPoint.Y, maxXDistanceCenter(xyPoint, otherPoint),  minYDistanceGround(xyPoint, otherPoint));
                        float weight = outs[0];

                        allBodyInputs.Add(new PointPair(xyPoint, otherPoint));
                        allBodyOutputs.Add(new List<float>(outs));                        


                        if (useLeo )
                        {

                            if (outs[1] > 0)
                            {
                                //Console.WriteLine("XY: " + xyPoint + " Other: " + otherPoint + " LEO : " + outs[1]) ;
                                
                                //Console.WriteLine(" XDist: " + sqrt(xDistanceSq(xyPoint, otherPoint)) 
                                //    + " yDist : " + sqrt(yDistanceSq(xyPoint, otherPoint)) 
                                //    + " MaxDist: " + maxXDistanceCenter(xyPoint, otherPoint))
                                   //+ " MinY: " + minYDistanceGround(xyPoint, otherPoint));
                                //Console.WriteLine();

                                //add to hidden neurons
                                if (!hiddenNeurons.Contains(xyPoint))
                                    hiddenNeurons.Add(xyPoint);

                                src = hiddenNeurons.IndexOf(xyPoint);

                                if (!hiddenNeurons.Contains(otherPoint))
                                    hiddenNeurons.Add(otherPoint);

                                tgt = hiddenNeurons.IndexOf(otherPoint);

                                conSourcePoints.Add(counter, xyPoint);
                                conTargetPoints.Add(counter, otherPoint);

                                indexToConnectionMap.Add(allBodyOutputs.Count-1, counter);
                                connections.Add(new ConnectionGene(counter++, (src), (tgt), weight * HyperNEATParameters.weightRange, new float[] { xyPoint.X, xyPoint.Y, otherPoint.X, otherPoint.Y }, outs));


                              
                            }
                        }
                        else
                        {   
                            //add to hidden neurons
                            if (!hiddenNeurons.Contains(xyPoint))
                                hiddenNeurons.Add(xyPoint);

                            src = hiddenNeurons.IndexOf(xyPoint);

                            if (!hiddenNeurons.Contains(otherPoint))
                                hiddenNeurons.Add(otherPoint);

                            tgt = hiddenNeurons.IndexOf(otherPoint);

                            conSourcePoints.Add(counter, xyPoint);
                            conTargetPoints.Add(counter, otherPoint);

                            indexToConnectionMap.Add(allBodyOutputs.Count - 1, counter);
                            connections.Add(new ConnectionGene(counter++, (src), (tgt), weight * HyperNEATParameters.weightRange, new float[] { xyPoint.X, xyPoint.Y, otherPoint.X, otherPoint.Y }, outs));
                         
                        }


                        //PointF newp = new PointF(p.x2, p.y2);

                        //targetIndex = hiddenNeurons.IndexOf(newp);
                        //if (targetIndex == -1)
                        //{
                        //    targetIndex = hiddenNeurons.Count;
                        //    hiddenNeurons.Add(newp);
                        //}
                        //connections.Add(new ConnectionGene(counter++, (sourceIndex), (targetIndex + inputCount + outputCount), p.weight * HyperNEATParameters.weightRange, new float[] { p.x1, p.y1, p.x2, p.y2 }, p.Outputs));




                    }
                }

            }

         



            //esSubstrate.generateSubstrate(inputs, outputs, net,
            //    HyperNEATParameters.initialDepth,
            //    (float)HyperNEATParameters.varianceThreshold,
            //     (float)HyperNEATParameters.bandingThreshold,
            //    HyperNEATParameters.ESIterations,
            //     (float)HyperNEATParameters.divisionThreshold,
            //    HyperNEATParameters.maximumDepth,
            //    (uint)inputs.Count, (uint)outputs.Count,
            //    ref connections, ref hiddenNeurons, true);
            

            //generateSubstrate(List<System.Drawing.PointF> inputNeuronPositions, List<PointF> outputNeuronPositions,
            //INetwork genome, int initialDepth, float varianceThreshold, float bandThreshold, int ESIterations,
            //                                    float divsionThreshold, int maxDepth,
            //                                    uint inputCount, uint outputCount,
            //                                    ref  ConnectionGeneList connections, ref List<PointF> hiddenNeurons)

            //blow out the object, we don't care about testing it

            //foreach (var pPair in pointsChecked)
            //{
            //    Console.WriteLine("Checking: " + pPair.Key + " processed: ");

            //    foreach (var xyPoint in pPair.Value)
            //    {
            //        Console.WriteLine("({0}, {1}) ", xyPoint.X, xyPoint.Y);
            //    }
            //}

            var beforeConn = connections.Count;
            var beforeNeuron = hiddenNeurons.Count;
            //var hiddenCopy = new List<PointF>(hiddenNeurons);


            Dictionary<long, ConnectionGeneList> connectionsGoingOut = outwardNodeConnections(connections);
            Dictionary<long, ConnectionGeneList> connectionsGoingInward = inwardNodeConnections(connections);

            ensureSingleConnectedStructure(connections, connectionsGoingInward,  connectionsGoingOut, hiddenNeurons, conSourcePoints, conTargetPoints);

            if (hiddenNeurons.Count > 20 || connections.Count > 100)
            {
                hiddenNeurons = new List<PointF>();
                connections = new ConnectionGeneList();
            }


            if (hiddenNeurons.Count == 0 || connections.Count == 0)
                isEmpty = true;

            NeatGenome ng = (NeatGenome)genome;

            bool behaviorExists = (ng.Behavior != null);

            ESBodyInformation esbody = new ESBodyInformation() {
                //AllBodyOutputs = allBodyOutputs,
                //AllBodyInputs = allBodyInputs,
                indexToConnection = indexToConnectionMap,
                //PreHiddenLocations = hiddenCopy,
                BeforeNeuron = beforeNeuron,
                BeforeConnection = beforeConn,
                GenomeID = genome.GenomeId,
                Connections = connections, 
                HiddenLocations = hiddenNeurons, 
                InputLocations = inputs,
                Objectives = ng.objectives,
                Fitness =  ng.Fitness,
                Locality = ng.locality,
                useLEO = useLeo
            };
            //Console.WriteLine(" Nodes: " + hiddenNeurons.Count + " Connections: " + connections.Count);

            return esbody;

        }
        
        float maxXDistanceCenter(PointF p1, PointF p2)
        {
            return (float)Math.Max(Math.Sqrt(Math.Pow(p1.X, 2)), Math.Sqrt(Math.Pow(p2.X, 2)));
        }
        float minYDistanceGround(PointF p1, PointF p2)
        {
            return (float)Math.Min(Math.Sqrt(Math.Pow((p1.Y + 1) / 2, 2)), Math.Sqrt(Math.Pow((p2.Y + 1) / 2, 2)));
        }
        float xDistanceSq(PointF p1, PointF p2)
        {
            return (float)(Math.Pow(p1.X - p2.X, 2));
        }
        float yDistanceSq(PointF p1, PointF p2)
        {
            return (float)(Math.Pow(p1.Y - p2.Y, 2));
        }
        float sqrt(float val)
        {
            return (float)Math.Sqrt(val);
        }
        float distance(PointF p1, PointF p2)
        {
            return (float)Math.Sqrt(xDistanceSq(p1,p2) + yDistanceSq(p1,p2));
        }


        ConnectionGene firstConnectionNotInvestigated(List<ConnectionGene> connections, List<ConnectionGene> allConnectionsSeen)
        {
            for(int c=0; c < connections.Count; c++)
            {
                if(!allConnectionsSeen.Contains(connections[c]))
                    return connections[c];
            }

            return null;
        }

        void ensureSingleConnectedStructure(ConnectionGeneList connections,
            Dictionary<long, ConnectionGeneList> inwardConnection, Dictionary<long, ConnectionGeneList> outwardConnections, 
            List<PointF> hiddenNeurons, Dictionary<long, PointF> conSourcePoints, Dictionary<long, PointF> conTargetPoints)
        {
            int maxChain = 0;

            //no changes for 0 or 1 connections -- nothing could be disconnected
            if (connections.Count <= 1)
                return;

            List<ConnectionGeneList> connectionChains = new List<ConnectionGeneList>();

            List<ConnectionGene> seenConnections = new List<ConnectionGene>();
            List<long> seenNodes = new List<long>();

            while (seenConnections.Count < connections.Count)
            {

                List<long> investigateNodes = new List<long>();
                ConnectionGene firstInvestigate = firstConnectionNotInvestigated(connections, seenConnections);

                if (firstInvestigate == null)
                {
                    throw new Exception("Shouldn't be here, the while loop should have failed!");
                }

                //this will mark our chain of connections
                ConnectionGeneList conChain = new ConnectionGeneList();

                //get ready to investigate the node of a connection we haven't seen
                investigateNodes.Add(firstInvestigate.SourceNeuronId);

                //still have nodes left to investigate, we should continue
                while (investigateNodes.Count > 0)
                {

                    //this will be the next nodes we take a look at
                    List<long> nextInvestigate = new List<long>();

                    //for all the nodes we need to look at
                    for (int i = 0; i < investigateNodes.Count; i++)
                    {
                        //grab the node id (this is the starting node, and we want to see all outward connections for that node)
                        //i.e. who does this node connect to!
                        long sourceNode = investigateNodes[i];

                        //don't examine nodes you've already seen -- but we shouodln't get here anyways
                        if (seenNodes.Contains(sourceNode))
                            continue;

                        //we mark it as seen, even before looking, in case of recurring connections
                        seenNodes.Add(sourceNode);

                        //grab all of our connections coming out of this node
                        //make a copy, we're going to modify it
                        ConnectionGeneList cgOut = outwardConnections[sourceNode];

                        //for each connection
                        for (var c = 0; c < cgOut.Count; c++)
                        {
                            //grab our connection gene
                            var cg = cgOut[c];

                            //add it to our chain, since it's connected!
                            //recursive chain connections are actually not allowed
                            if (cg.SourceNeuronId != cg.TargetNeuronId)
                                conChain.Add(cg);

                            //also note that we've seen this connection -- no duplicates please -- although I don't know if
                            //this will ever be triggered -- i don't think so
                            if(!seenConnections.Contains(cg))
                                seenConnections.Add(cg);

                            //we need to investigate another node -- no duplicates please!
                            if (!seenNodes.Contains(cg.TargetNeuronId) && !nextInvestigate.Contains(cg.TargetNeuronId))
                                nextInvestigate.Add(cg.TargetNeuronId);

                        }


                        //and grab all of the connections going inward too -- we need to go both direction
                        cgOut = inwardConnection[sourceNode];

                        //for each connection
                        for (var c = 0; c < cgOut.Count; c++)
                        {
                            //grab our connection gene
                            var cg = cgOut[c];

                            //add it to our chain, since it's connected!
                            //recursive chain connections are actually not allowed
                            if (cg.SourceNeuronId != cg.TargetNeuronId)
                                conChain.Add(cg);

                            //also note that we've seen this connection -- no duplicates please -- although I don't know if
                            //this will ever be triggered -- i don't think so
                            if (!seenConnections.Contains(cg))
                                seenConnections.Add(cg);

                            //we need to investigate another node -- no duplicates please!
                            //going for inward goofers
                            if (!seenNodes.Contains(cg.SourceNeuronId) && !nextInvestigate.Contains(cg.SourceNeuronId))
                                nextInvestigate.Add(cg.SourceNeuronId);
                        }

                    }

                    //we have now investigated all of our previous nodes
                    //time to move to the next batch!
                    investigateNodes = nextInvestigate;
                }

                //now we have exhausted the chain of possible nodes to investigate

                //that means we have the final chain here
                //so we add it for later investigation
                connectionChains.Add(conChain);

                maxChain = Math.Max(conChain.Count, maxChain);
            }


            //now we simply choose the maximum connection chain -- or the first one with the max found
            ConnectionGeneList maxCGL = connectionChains.First(x => x.Count == maxChain);

            //dump the previous connections
            connections.Clear();

            //absorb the correct connections
            connections.AddRange(maxCGL);


            //now we need to say which hidden neurons actually make sense still
            hiddenNeurons.Clear();            
            connections.ForEach(con =>
                {
                    //no duplicates please!
                    PointF potential = conSourcePoints[con.InnovationId];
                    hiddenNeurons.Add(potential);
                    potential = conTargetPoints[con.InnovationId];
                    hiddenNeurons.Add(potential);
                });

            //make sure points are distinct, and no dupes -- we only need to do this once
            var tempDistinct = hiddenNeurons.Distinct().ToList();
            hiddenNeurons.Clear();
            hiddenNeurons.AddRange(tempDistinct);

            //otherwise, in other languages, we would simply check to make sure no duplicates on entry
            //kind like this
            //if (!hiddenNeurons.Any(p => p.X == potential.X && p.Y == potential.Y))
            //    hiddenNeurons.Add(potential);


            //now we have to correct the indices of the max-chain of objects
            connections.ForEach(con =>
            {
                //readjust connection source/target depending on hiddenNeuron array
                PointF point = conSourcePoints[con.InnovationId];
                con.SourceNeuronId = hiddenNeurons.FindIndex(hp => hp.X == point.X && hp.Y == point.Y);

                if (con.SourceNeuronId == -1)
                    Console.WriteLine("Adjusted con src- " + con.SourceNeuronId + " tgt- " + con.TargetNeuronId);

                point = conTargetPoints[con.InnovationId];
                con.TargetNeuronId = hiddenNeurons.FindIndex(hp => hp.X == point.X && hp.Y == point.Y);

                if (con.TargetNeuronId == -1)
                    Console.WriteLine("Adjusted con src- " + con.SourceNeuronId + " tgt- " + con.TargetNeuronId);

            });
        }



        float[] queryCPPNOutputs(ModularNetwork genome, float x1, float y1, float x2, float y2, float maxXDist, float minYDist)
        {
            float[] coordinates = new float[genome.InputNeuronCount];

            coordinates[0] = x1;
            coordinates[1] = y1;
            coordinates[2] = x2;
            coordinates[3] = y2;
            //coordinates[4] = maxXDist;
            //coordinates[5] = minYDist;

            //Console.WriteLine("Coordinates: ({0}, {1} : {2}, {3})", x1, y1, x2, y2);
            genome.ClearSignals();
            genome.SetInputSignals(coordinates);
            genome.RecursiveActivation();

            float[] outs = new float[genome.OutputNeuronCount];
            for (int i = 0; i < genome.OutputNeuronCount; i++)
                outs[i] = genome.GetOutputSignal(i);

            return outs;
        }


        public void setCommunicator(SimpleCommunicator simpleCommunicator)
        {
            this.simpleCom = simpleCommunicator;
            this.jsPop.setCommunicator(simpleCom);
        }
    }
}
