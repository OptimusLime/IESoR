using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpNeatLib.Evolution;

//using Microsoft.JScript;
//using Microsoft.JScript.Vsa;
using Newtonsoft.Json.Linq;
using System.Dynamic;
using Awesomium.sockets;
using System.Threading;

public class JSEval
{
    //private static VsaEngine _Engine = VsaEngine.CreateEngine();
    //public static string concatenateScripts(string[] filenames)
    //{
    //    string concat = "";
        
    //    foreach (string file in filenames)
    //    {
    //        concat += loadScriptString(file);
    //    }

    //    return concat;
    //}
    //public static string loadScriptString(string filename)
    //{
    //    //need to open up the js file
    //    //then push the file all into a single string
    //    //and then we'll go and evaluate the function
    //}

    //public static object Eval(string javaScript)
    //{
    //    return Microsoft.JScript.Eval.JScriptEvaluate(javaScript, _Engine);
    //}

    //public static ArrayObject EvalToArray(string javaScript)
    //{
    //    return Eval(javaScript) as ArrayObject;
    //}
}

namespace NodeCommunicator.Evolution
{
    /// <summary>
    /// Evaluates populations using Javascript code. Converts INetwork to bodies, then runs inside javascript environment for automated evolution.
    /// </summary>
    public class JSPopulationEvaluator : IPopulationEvaluator
    {
        SimpleCommunicator simpleCom;

        static List<AutoResetEvent> openQueries = new List<AutoResetEvent>();

        public static void forceThreadClose()
        {
            foreach (var are in openQueries)
            {
                are.Set();
            }
        }

        bool waitingOnEvaluation = false;
        //Dictionary<long, KeyValuePair<double[], List<double>>> genomeBehaviors;
        //Dictionary<long, List<double>> genomeSecondaryBehaviors;
        //Dictionary<long, double> fitnessDictionary;
        //not entirely concerned with this yet. 

        public void EvaluatePopulation(Population pop, EvolutionAlgorithm ea)
        {

            Dictionary<long, string> evaluateObjects = new Dictionary<long,string>();
            int objCount = 3;
            int behaviorCount = 5;
            int secondBehaviorCount = 9;
            List<double> emptyBehavior = new List<double>();
            List<double> emptySecondBehavior = new List<double>();
            for (var i = 0; i < behaviorCount; i++) emptyBehavior.Add(0);
            for (var i = 0; i < secondBehaviorCount; i++) emptySecondBehavior.Add(0);

            List<long> seenGenomes = new List<long>();
            
            Dictionary<long, IGenome> genomeDict = new Dictionary<long, IGenome>();
            pop.GenomeList.ForEach(x =>
                {
                    if(!genomeDict.ContainsKey(x.GenomeId))
                        genomeDict.Add(x.GenomeId, x);
                });
            


            int maxGroupCount = 10;
            List<Dictionary<long, string>> evaluateGroups = new List<Dictionary<long, string>>();
            
            foreach (var genome in pop.GenomeList)
            {
                //if (maxCount-- <= 0)
                //    break;
                //each genome needs evaluation

                //first, convert each body

                bool isEmpty = false;
                
                
                //do something really quick if empty!
                string body = simpleCom.simpleExperiment.genomeIntoBodyJSON(genome, out isEmpty);

                if (isEmpty)
                {
                    //assign generic stuff here
                    genome.Fitness = EvolutionAlgorithm.MIN_GENOME_FITNESS;
                    genome.RealFitness = EvolutionAlgorithm.MIN_GENOME_FITNESS;
                    //empty behavior list -- 
                    genome.Behavior = new SharpNeatLib.BehaviorType() { objectives = new double[objCount], behaviorList = new List<double>(emptyBehavior) };

                    if (secondBehaviorCount > 0)
                        genome.SecondBehavior = new SharpNeatLib.BehaviorType() { objectives = new double[objCount], behaviorList = new List<double>(emptySecondBehavior) };

                }
                else
                {
                    if (!seenGenomes.Contains(genome.GenomeId))
                    {
                        evaluateObjects.Add(genome.GenomeId, body);
                        seenGenomes.Add(genome.GenomeId);
                    }
                }

                if (evaluateObjects.Count == maxGroupCount)
                {
                    evaluateGroups.Add(evaluateObjects);
                    evaluateObjects = new Dictionary<long, string>();
                }
            }

            if (evaluateObjects.Count > 0)
            {
                evaluateGroups.Add(evaluateObjects);
            }

            var executedCallBack = new AutoResetEvent(false);
            openQueries.Add(executedCallBack);
            int calculatedCount = pop.GenomeList.Count;

            foreach (var group in evaluateGroups)
            {

                JArray args = new JArray();
                args.Add(JObject.FromObject(group));


                if (MasterSocketManager.serverClosed)
                {
                    //hack to shut down other threads doing processing
                    Thread.CurrentThread.Abort();
                    return;
                }


                MasterSocketManager.callEvaluatorJS("headlessEvaluateGenomeBehaviors", args, "window",
                    (JObject finishedResults) =>
                    {
                        //Evaluated all genome objects here
                        //ready to proceed with evolution evaluation

                      

                        Dictionary<long, double> fitnessResults;
                        Dictionary<long, List<double>> secondBehavior;
                        var processedBehavior = processEvaluationResults(finishedResults, out fitnessResults, out secondBehavior);

                  

                      
                        //assign genome behaviors to population objects!
                        foreach (var idKeyVal in finishedResults)
                        {
                            long gID;

                            //any key that's not a long isn't of interest to us -- socket info, messageid ,etc
                            if (!long.TryParse(idKeyVal.Key.ToString(), out gID))
                                continue;

                            //get our genome from the id to genome mapping
                            var genome = genomeDict[gID];
                            //calculate our progress in obj

                            double[] accumObjectives = new double[objCount];
                            for (int i = 0; i < objCount; i++) accumObjectives[i] = 0.0;

                            //our real fitness is measured by distance traveled
                            genome.RealFitness = fitnessResults[genome.GenomeId];
                            genome.Fitness = EvolutionAlgorithm.MIN_GENOME_FITNESS;

                            //set the behavior yo!
                            //objectives should be [ fitness, 0, 0 ] -- to be updated with novelty stuff
                            genome.Behavior = new SharpNeatLib.BehaviorType() { objectives = processedBehavior[genome.GenomeId].Key, behaviorList = processedBehavior[genome.GenomeId].Value };

                            if (secondBehavior.Count > 0)
                                genome.SecondBehavior = new SharpNeatLib.BehaviorType() { objectives = processedBehavior[genome.GenomeId].Key, behaviorList = secondBehavior[genome.GenomeId] };

                            calculatedCount--;
                        }


                        //continue onwards and upwards with newly acquired evaluation information
                        executedCallBack.Set();

                        return null;
                    });

               
                executedCallBack.WaitOne();
                //continue execution synchronously

                Console.WriteLine("Pop Chunk eval!");
            }
            //all done, this thread be done thanks
            openQueries.Remove(executedCallBack);
            Console.WriteLine("Finished pop eval 1 gen.");


        }

        Dictionary<long, KeyValuePair<double[], List<double>>> processEvaluationResults(JObject jsonResults, 
            out Dictionary<long, double> fitnessEvaluation, out Dictionary<long, List<double>> genomeSecondBehaviors)
        {
            //to return
            Dictionary<long, KeyValuePair<double[], List<double>>> genomeBehaviorDict = new Dictionary<long, KeyValuePair<double[], List<double>>>();
            
            //to create and send out
            genomeSecondBehaviors = new Dictionary<long, List<double>>();
            fitnessEvaluation = new Dictionary<long, double>();

            try
            {

                int objCount = 3;

                foreach(var jsonKeyVal in jsonResults)
                {
                    long gID;

                    //any key that's not a long isn't of interest to us -- socket info, messageid ,etc
                    if (!long.TryParse(jsonKeyVal.Key.ToString(), out gID))
                        continue;

                //for each genome, we need to build our double list
                //foreach (var genome in pop)
                //{
                    //var gID = genome.GenomeId;

                    if (genomeBehaviorDict.ContainsKey(gID))
                        continue;

                    List<double> doubleBehavior = new List<double>();

                    double[] accumObjectives = new double[objCount];
                    for (int i = 0; i < objCount; i++) accumObjectives[i] = 0.0;

                    //LINQ way to do it
                    //    parsedJson[gID.ToString()].SelectMany(
                    //    xyBehavior => new List<double>(){ xyBehavior["x"].Value<double>(), xyBehavior["y"].Value<double>()}
                    //).ToList<double>();

                    var genomeEntry = jsonKeyVal.Value;

                    double val;
                    if (!double.TryParse(genomeEntry["fitness"].ToString(), out val))
                        val = EvolutionAlgorithm.MIN_GENOME_FITNESS;

                    fitnessEvaluation.Add(gID, Math.Max(EvolutionAlgorithm.MIN_GENOME_FITNESS, val));

                    int ix = 0;

                    foreach (var objective in genomeEntry["objectives"])
                    {
                        if (double.TryParse(objective.ToString(), out val))
                        {
                            accumObjectives[ix] = val;
                        }
                        ix++;
                    }



                    foreach (var behavior in genomeEntry["behavior"])
                    {

                        if (double.TryParse(behavior.ToString(), out val))
                        {
                            doubleBehavior.Add(val);
                        }
                        else
                        {

                            if (double.TryParse(behavior["x"].ToString(), out val))
                                doubleBehavior.Add(val);

                            if (double.TryParse(behavior["y"].ToString(), out val))
                                doubleBehavior.Add(val);

                        }
                    }

                    if (genomeEntry["secondBehavior"] != null)
                    {

                        List<double> secondBehavior = new List<double>();

                        foreach (var behavior in genomeEntry["secondBehavior"])
                        {


                            if (double.TryParse(behavior.ToString(), out val))
                            {
                                secondBehavior.Add(val);
                            }
                            else
                            {

                                if (double.TryParse(behavior["x"].ToString(), out val))
                                    secondBehavior.Add(val);

                                if (double.TryParse(behavior["y"].ToString(), out val))
                                    secondBehavior.Add(val);

                            }
                        }

                        genomeSecondBehaviors.Add(gID, secondBehavior);

                    }



                    //now we have our double behavior, add to our behavior dictionary
                    //we assume no duplicated for simplicity
                    genomeBehaviorDict.Add(gID, new KeyValuePair<double[], List<double>>(accumObjectives, doubleBehavior));

                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                throw e;
            }


            return genomeBehaviorDict;

        }



        //public void OldEvaluatePopulation(Population pop, EvolutionAlgorithm ea)
        //{
        //    List<long> genomeIDs = pop.GenomeList.Select(x => (long)x.GenomeId).ToList();

        //    //mostly for memory cleanup stuff -- don't really need to do this
        //    if(genomeBehaviors!=null)
        //        genomeBehaviors.Clear();

        //    if (fitnessDictionary != null)
        //        fitnessDictionary.Clear();

        //    Dictionary<long, KeyValuePair<double[], List<double>>> genomeBs = new Dictionary<long,KeyValuePair<double[],List<double>>>();

        
        //    fitnessDictionary = new Dictionary<long, double>();
        //    genomeSecondaryBehaviors = new Dictionary<long, List<double>>();

        //    //break our communication up into 5 almost equal chunks (maybe a better number to select here)
        //   var genomesChunks = genomeIDs.GroupBy(x => genomeIDs.IndexOf(x) % 7);
                
        //    foreach(var chunk in genomesChunks)
        //    {
        //        var genomes = serialCallCommunicatorWithIDs(chunk.ToList());
        //        foreach (var gReturn in genomes)
        //        {
        //            genomeBs.Add(gReturn.Key, gReturn.Value);
        //        }
        //    }

        //    while (genomeBs.Count == 0)
        //    {
        //        //send them back, we want the right ones no matter what!
        //        genomeBs = serialCallCommunicatorWithIDs(genomeIDs);
        //    }

        //    try
        //    {
        //        int objCount = 3;
        //        //assign genome behaviors to population objects!
        //        foreach (IGenome genome in pop.GenomeList)
        //        {
        //            //calculate our progress in obj

        //            double[] accumObjectives = new double[objCount];
        //            for (int i = 0; i < objCount; i++) accumObjectives[i] = 0.0;

        //            //our real fitness is measured by distance traveled
        //            genome.RealFitness = fitnessDictionary[genome.GenomeId];
        //            genome.Fitness = EvolutionAlgorithm.MIN_GENOME_FITNESS;

        //            //set the behavior yo!
        //            //objectives should be [ fitness, 0, 0 ] -- to be updated with novelty stuff
        //            genome.Behavior = new SharpNeatLib.BehaviorType() { objectives = genomeBs[genome.GenomeId].Key, behaviorList = genomeBs[genome.GenomeId].Value };

        //            if (genomeSecondaryBehaviors.Count > 0)
        //                genome.SecondBehavior = new SharpNeatLib.BehaviorType() { objectives = genomeBs[genome.GenomeId].Key, behaviorList = genomeSecondaryBehaviors[genome.GenomeId] };
        //        }

        //        //if (ea.NeatParameters.noveltySearch)
        //        //{
        //        //    if (ea.NeatParameters.noveltySearch && ea.noveltyInitialized)
        //        //    {
        //        //        ea.CalculateNovelty();
        //        //    }
        //        //}

        //    }
        //    catch (Exception e)
        //    {
        //        //check our last object
        //        var parsedJson = JObject.Parse((string)lastReturnedObject.Args[0]);
        //        //Console.WriteLine(parsedJson);
        //        Console.WriteLine("Error: " + e.Message);
        //        Console.WriteLine(e.StackTrace);

        //        throw e;

        //    }
        //}

        //Dictionary<long, KeyValuePair<double[], List<double>>> SerialEvaluateGenomes(List<long> genomeIds)
        //{
        //    return serialCallCommunicatorWithIDs(genomeIds);
        //}
        //dynamic lastReturnedObject;

        //Dictionary<long, KeyValuePair<double[], List<double>>> serialCallCommunicatorWithIDs(List<long> genomeIDs)
        //{
            
        //    waitingOnEvaluation = true;

        //    callCommunicatorWithIDs(genomeIDs, (jsonString) =>
        //    {
        //        lastReturnedObject = jsonString;

        //        //get our genome behaviors!
        //        //genomeBehaviors = new Dictionary<long, KeyValuePair<double[], List<double>>>();//new Dictionary<long, List<double>>();

        //        //fitnessDictionary = new Dictionary<long, double>();
        //        genomeBehaviors = new Dictionary<long, KeyValuePair<double[], List<double>>>();//new Dictionary<long, List<double>>();


        //        if (jsonString.Args.Length > 0)
        //        {
        //            //we try parsing our object into a dictionary like object
        //            try
        //            {
        //                var parsedJson = JObject.Parse((string)jsonString.Args[0]);
        //                int objCount = 3;
                        
        //                //for each genome, we need to build our double list
        //                foreach (var gID in genomeIDs)
        //                {
        //                    if (genomeBehaviors.ContainsKey(gID))
        //                        continue;

        //                    List<double> doubleBehavior = new List<double>();
                            
        //                    double[] accumObjectives = new double[objCount];
        //                    for (int i = 0; i < objCount; i++) accumObjectives[i] = 0.0;

        //                        //LINQ way to do it
        //                    //    parsedJson[gID.ToString()].SelectMany(
        //                    //    xyBehavior => new List<double>(){ xyBehavior["x"].Value<double>(), xyBehavior["y"].Value<double>()}
        //                    //).ToList<double>();

        //                    var genomeEntry = parsedJson[gID.ToString()];

        //                    double val;
        //                    if (!double.TryParse(genomeEntry["fitness"].ToString(), out val))
        //                        val = EvolutionAlgorithm.MIN_GENOME_FITNESS;

        //                    fitnessDictionary.Add(gID, Math.Max(EvolutionAlgorithm.MIN_GENOME_FITNESS, val));

        //                  int ix=0;

        //                    foreach (var objective in genomeEntry["objectives"])
        //                    {
        //                        if (double.TryParse(objective.ToString(), out val))
        //                        {
        //                            accumObjectives[ix] = val;
        //                        }
        //                        ix++;
        //                    }

                            

        //                        foreach (var behavior in genomeEntry["behavior"])
        //                        {

        //                            if (double.TryParse(behavior.ToString(), out val))
        //                            {
        //                                doubleBehavior.Add(val);
        //                            }
        //                            else
        //                            {

        //                                if (double.TryParse(behavior["x"].ToString(), out val))
        //                                    doubleBehavior.Add(val);

        //                                if (double.TryParse(behavior["y"].ToString(), out val))
        //                                    doubleBehavior.Add(val);

        //                            }
        //                        }

        //                        if (genomeEntry["secondBehavior"] != null)
        //                        {

        //                            List<double> secondBehavior = new List<double>();

        //                            foreach (var behavior in genomeEntry["secondBehavior"])
        //                            {


        //                                if (double.TryParse(behavior.ToString(), out val))
        //                                {
        //                                    secondBehavior.Add(val);
        //                                }
        //                                else
        //                                {

        //                                    if (double.TryParse(behavior["x"].ToString(), out val))
        //                                        secondBehavior.Add(val);

        //                                    if (double.TryParse(behavior["y"].ToString(), out val))
        //                                        secondBehavior.Add(val);

        //                                }
        //                            }

        //                            genomeSecondaryBehaviors.Add(gID, secondBehavior);                            

        //                        }

                            
                            
        //                    //now we have our double behavior, add to our behavior dictionary
        //                    //we assume no duplicated for simplicity
        //                    genomeBehaviors.Add(gID, new KeyValuePair<double[], List<double>>(accumObjectives, doubleBehavior));

        //                }

        //            }
        //            catch (Exception e)
        //            {
        //                Console.WriteLine(e.Message);
        //                Console.WriteLine(e.StackTrace);
        //                throw e;
        //            }

        //        }
                
        //        //when finished, stop the eval procedure, error or not (which we don't yet check for -- or timeout!)
        //        waitingOnEvaluation = false;

        //    });

        //    int timeout = 400000;
        //    bool timedOut = false;
        //    DateTime now = DateTime.Now;
        //    //then we wait for a return
        //    //this is very very very dangerous without error checking
        //    while (!timedOut && waitingOnEvaluation){

        //        if ((DateTime.Now - now).TotalMilliseconds > timeout)
        //        {
        //            timedOut = true;
        //          simpleCom.printString("TIMED OUT EVALUATION, RETRYING");
        //        }
            
        //    }

        //    //skip over the rest, just send an empty object
        //    if (timedOut)
        //        return new Dictionary<long, KeyValuePair<double[], List<double>>>();

        //    try
        //    {
        //        //we have some returned evaluation, go ahead and print that poop.
        //        simpleCom.printString("Finished serial evaluation of: " + SimplePrinter.listToString<long>(genomeIDs));

        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine(e);
        //    }

        //    return genomeBehaviors;
        //}


        //void callCommunicatorWithIDs(List<long> genomeIDs, JSONDynamicEventHandler jsonEvent)
        //{
        //    simpleCom.callEventWithJSON("evaluateGenomes",  JArray.FromObject(genomeIDs).ToString() ,jsonEvent);
        //}

        ulong evalCount = 0;
        public ulong EvaluationCount
        {
            get { return evalCount; }
        }

        string state = "sameoldsameold";
        public string EvaluatorStateMessage
        {
            get { return state; }
        }

        public bool bestInter = false;
        public bool BestIsIntermediateChampion
        {
            get { return bestInter; }
        }

       /// <summary>
       /// For now, just returns false. Later will check more thoroughly. 
       /// </summary>
        public bool SearchCompleted
        {
            get { return false; }
        }



        internal void setCommunicator(SimpleCommunicator simpleCom)
        {
            this.simpleCom = simpleCom;
        }
    }
}
