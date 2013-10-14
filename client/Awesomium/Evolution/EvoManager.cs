using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpNeatLib.Evolution;
using System.Xml;
using System.IO;
using SharpNeatLib.NeatGenome;
using SharpNeatLib.NeatGenome.Xml;
using SharpNeatLib.Evolution.Xml;
using SharpNeatLib.NeuralNetwork;

namespace NodeCommunicator.Evolution
{
    public sealed class EvoManager
    {
        IPopulationEvaluator populationEval = null;
        NeatParameters neatParams = null;
        int cppnInputs, cppnOutputs;

        double maxFitness = 0;
        StreamWriter logOutput;
        string outputFolder = "";
        EvolutionAlgorithm ea = null;
        XmlDocument doc;
        FileInfo oFileInfo;

        public bool logging = true;
        //public bool timeEachGeneration = true;

         static readonly EvoManager instance = new EvoManager();

        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static EvoManager()
        {
        }

        EvoManager()
        {
        }

        public static EvoManager SharedEvolutionManager
        {
            get
            {
                return instance;
            }
        }

        public void initalizeEvoManager(int cInputs, int cOuputs, NeatParameters np, IPopulationEvaluator popEval)
        {
            cppnInputs = cInputs;
            cppnOutputs = cOuputs;
            neatParams = np;
            populationEval = popEval;
        }
        public Population EvoPop
        {
            get
            {
                if (ea != null)
                    return ea.Population;
                return null; 
            }
        }
        public void enableNoveltySearch(bool enable)
        {
            if (enable)
            {
                neatParams.noveltySearch = true;
                neatParams.noveltyFloat = true;
            }
            else
            {
                neatParams.noveltySearch = false;
                neatParams.noveltyFloat = false;
            }
        }
        public void initializeEvolution(int populationSize)
        {
            if (logOutput != null)
                logOutput.Close();

            logOutput = new StreamWriter(outputFolder + "logfile.txt");
            IdGenerator idgen = new IdGenerator();
            ea = new EvolutionAlgorithm(new Population(idgen, GenomeFactory.CreateGenomeList(neatParams, idgen, cppnInputs, cppnOutputs, neatParams.pInitialPopulationInterconnections, populationSize)), populationEval, neatParams);
        }

        public void initializeEvolution(int populationSize, NeatGenome seedGenome)
        {
            if (seedGenome == null)
            {
                initializeEvolution(populationSize);
                return;
            }
            if (logOutput != null)
                logOutput.Close();
            logOutput = new StreamWriter(outputFolder + "logfile.txt");
            IdGenerator idgen = new IdGeneratorFactory().CreateIdGenerator(seedGenome);
            ea = new EvolutionAlgorithm(new Population(idgen, GenomeFactory.CreateGenomeList(seedGenome, populationSize, neatParams, idgen)), populationEval, neatParams);
        }
        public void initializeEvolutionFromPopFile(string fname)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(fname);
            Population pop = XmlPopulationReader.Read(doc, new XmlNeatGenomeReader(), new SharpNeatLib.NeatGenome.IdGeneratorFactory());
            initalizeEvolution(pop);
        }

        public void initalizeEvolution(Population pop)
        {
            if (logOutput != null)
                logOutput.Close();
            logOutput = new StreamWriter(outputFolder + "logfile.txt");
            //IdGenerator idgen = new IdGeneratorFactory().CreateIdGenerator(pop.GenomeList);
            ea = new EvolutionAlgorithm(pop, populationEval, neatParams);
        }

        public void setOutputFolder(string folder)
        {
            outputFolder = folder;
        }

        public void oneGeneration(int currentGeneration)
        {
            DateTime dt = DateTime.Now;
            ea.PerformOneGeneration();
            if (ea.BestGenome.RealFitness > maxFitness)
            {
                //simExperiment.bestGenomeSoFar = (NeatGenome)ea.BestGenome;
                maxFitness = ea.BestGenome.RealFitness;
                doc = new XmlDocument();
                XmlGenomeWriterStatic.Write(doc, (NeatGenome)ea.BestGenome);
                oFileInfo = new FileInfo(outputFolder + "bestGenome" + currentGeneration.ToString() + ".xml");
                doc.Save(oFileInfo.FullName);
            }
            Console.WriteLine(ea.Generation.ToString() + " " + ea.BestGenome.RealFitness + " " + ea.Population.GenomeList.Count + " " + (DateTime.Now.Subtract(dt)));
            int gen_mult = 200;
            if (logging)
            {

                if (neatParams.noveltySearch && currentGeneration % gen_mult == 0)
                {
                    XmlDocument archiveout = new XmlDocument();

                    XmlPopulationWriter.WriteGenomeList(archiveout, ea.noveltyFixed.archive);
                    oFileInfo = new FileInfo(outputFolder + "archive.xml");
                    archiveout.Save(oFileInfo.FullName);
                }

                if ((neatParams.noveltySearch || neatParams.multiobjective) && currentGeneration % gen_mult == 0)
                {
                    XmlDocument popout = new XmlDocument();
                    if (!neatParams.multiobjective)
                        XmlPopulationWriter.Write(popout, ea.Population, ActivationFunctionFactory.GetActivationFunction("NullFn"));
                    else
                        XmlPopulationWriter.WriteGenomeList(popout, ea.multiobjective.population);

                    oFileInfo = new FileInfo(outputFolder + "population" + currentGeneration.ToString() + ".xml");
                    popout.Save(oFileInfo.FullName);
                }

                logOutput.WriteLine(ea.Generation.ToString() + " " + (maxFitness).ToString());
            }
        }

        public void evolve(int generations)
        {
            for (int j = 0; j < generations; j++)
            {
                oneGeneration(j);
            }
            logOutput.Close();

            doc = new XmlDocument();
            XmlGenomeWriterStatic.Write(doc, (NeatGenome)ea.BestGenome, ActivationFunctionFactory.GetActivationFunction("NullFn"));
            oFileInfo = new FileInfo(outputFolder + "bestGenome.xml");
            doc.Save(oFileInfo.FullName);

            //if doing novelty search, write out archive
            if (neatParams.noveltySearch)
            {
                XmlDocument archiveout = new XmlDocument();

                XmlPopulationWriter.WriteGenomeList(archiveout, ea.noveltyFixed.archive);
                oFileInfo = new FileInfo(outputFolder + "archive.xml");
                archiveout.Save(oFileInfo.FullName);
            }
        }

        //Do any cleanup here
        public void end()
        {
            if (logging)
                logOutput.Close();
        }

    }
}
