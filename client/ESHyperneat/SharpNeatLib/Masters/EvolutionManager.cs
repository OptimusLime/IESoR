using System;
using System.Collections.Generic;
using System.Text;
using SharpNeatLib.Evolution;
using System.IO;
using System.Xml;

using SharpNeatLib.NeuralNetwork;
using SharpNeatLib.NeatGenome;

using System.Collections;
using SharpNeatLib.NeatGenome.Xml;
using System.Xml.Serialization;

namespace SharpNeatLib.Masters
{


    public sealed class EvolutionManager
    {
        #region Variables

        static string CurrentGenomePool = "chosenGenomes";
        static double ChanceOfAsexual = .5;
        static int maxRandomAttempts = 20;
        //our random number generator
        Random r = new Random();
        NeatGenome.NeatGenome officialSeedGenome;


        //for evolutionary algorithm juice
        //IPopulationEvaluator populationEval = null;

        //we store a reference to the parameters, but they only dictate a run of an automated algorithm
        NeatParameters neatParams = null;
        
        //this is a default, but in time, input and output should be irrelevant to WIN -- so you shouldn't need to create 
        //a  new evolutionary algorithm just to change the number of inputs or outputs, just add a node. 
        int cppnInputs, cppnOutputs;

        //the generator for all time
        IdGenerator idgen;

        EvolutionAlgorithm evoAlgorithm;

        double maxFitness = 0;
        StreamWriter logOutput;
        string outputFolder = "";
        EvolutionAlgorithm ea = null;
        XmlDocument doc;
        FileInfo oFileInfo;

        #endregion

        //this is for dictionaries that may or may not be in use
        //Some dictionaries will be set up with future progress in mind, and might need to be adjusted over time
        #region Experimental Variables

        Dictionary<string, GenomeList> genomeListDictionary = new Dictionary<string, GenomeList>();
        GenomeList allGeneratedGenomes = new GenomeList();

        #endregion

        #region Singleton Code

        static readonly EvolutionManager instance = new EvolutionManager();

        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static EvolutionManager()
        {

        }

        EvolutionManager()
        {
            //generic neat params, if need to change, just create a function that swaps them in
            neatParams = new NeatParameters();
            //neatParams.noveltySearch = true;
            //neatParams.noveltyFloat = true;
            neatParams.multiobjective = true;
            neatParams.archiveThreshold = 0.5;
            //neatParams.archiveThreshold = 300000.0;

            //maybe we load this idgenerator from server or local database, just keep that in mind
            idgen = new IdGenerator();

            //we just default to this, but it doesn't have to be so
            this.setDefaultCPPNs(4, 3);

            genomeListDictionary.Add(CurrentGenomePool, new GenomeList());
        }

        public static EvolutionManager SharedEvolutionManager
        {
            get
            {
                return instance;
            }
        }
        #endregion

        #region Set Default Inputs/Outputs

        public void setDefaultCPPNs(int inputCount, int outputCount)
        {
            this.cppnInputs = inputCount;
            this.cppnOutputs = outputCount;            

            //what more you ask? Well, we need to keep a record of what the default was at creation
            //this way, when you add another, 
            Console.WriteLine(" Do we need to do more when resetting input, output count? For now, we ignore this!");
        }

        #endregion



        #region Generating Individuals (Sexual, asexual)

        /// <summary>
        /// Random genome from a genomelist, simple function helper to call
        /// </summary>
        /// <param name="potentials"></param>
        /// <returns></returns>
        private IGenome randomGenome(GenomeList potentials)
        {
            return potentials[r.Next(potentials.Count)];
        }

        public GenomeList loadMoreGenomesFromSelected(int count)
        {
            GenomeList gl = new GenomeList();

            for (int i = 0; i < count; i++)
                gl.Add(loadNextNetworkFromSelected());

            return gl;

        }
        public IGenome loadNextNetworkFromSelected()
        {

            GenomeList gl = genomeListDictionary[CurrentGenomePool];
            //there is nobody currently selected, send in a rando, or you could always look for historically selected objects, or something like that
            if (gl.Count == 0)
                return noSelectedNetworks();
            
            //now we need to choose the parents, we can do sexual or asexual reproduction as well. 
            //We can also merge more than 2 parents, but we won't do that for now

            if (gl.Count == 1)
                return asexualNetwork(gl);
            else
            {
                if (r.NextDouble() < ChanceOfAsexual)
                    return asexualNetwork(gl);
                else
                    return sexualNetwork(gl);
            }



        }
        /// <summary>
        /// What we do when we want to choose at least 2 parents to mate
        /// </summary>
        /// <param name="potentials"></param>
        /// <returns></returns>
        private IGenome sexualNetwork(GenomeList potentials)
        {
            IGenome parent1 = randomGenome(potentials);
            IGenome parent2 = randomGenome(potentials);

            int attemptCount = 0;
            //make sure you aren't combining the same individual
            while (parent1 == parent2 && attemptCount++ < maxRandomAttempts)
            {
                parent2 = randomGenome(potentials);
            }

            //make babies directly with parent1 and parent2
            //We'll need to make note of these changes as well in WIN -- not sure where to store this location
            IGenome genome =  ((NeatGenome.NeatGenome)parent1).CreateOffspring_Sexual(neatParams, idgen, parent2);

            //allGeneratedGenomes.Add(genome);

            return genome;
        }

        /// <summary>
        /// What we do when we want asexual reproduction from a collection of genomes
        /// </summary>
        /// <param name="potentials"></param>
        /// <returns></returns>
        private IGenome asexualNetwork(GenomeList potentials)
        {
            //select a random genome for asexual reprodcution
            IGenome genome = randomGenome(potentials);

            //make note of any new additions, we'll have to verify this with the WIN server in some fashion
            //in the future, we will need to do something with this information, for now, that's not necessarily an issue
            Hashtable newConnectionTable = new Hashtable();
            Hashtable newNeuronGeneTable = new Hashtable();

            //shazam, create an asexual genome
            IGenome offspring = ((NeatGenome.NeatGenome)genome).CreateOffspring_Asexual(neatParams, idgen, newNeuronGeneTable, newConnectionTable);

            //make sure to note this in our evolution manager (that we created this object)
            //allGeneratedGenomes.Add(offspring);

            //after noting its creation, we return
            return offspring;
        }
        /// <summary>
        /// What we do in the case where we have no networks we have selected for reproduction
        /// </summary>
        /// <returns></returns>
        private IGenome noSelectedNetworks()
        {
            //grab a randomized network (perhaps we can be more sophisticated about how to get an individual when there are no selected networks)
            IGenome genome = fetchRandomNetwork();

            //take note of the individual that's been created
            //allGeneratedGenomes.Add(genome);

            //then return our genome
            return genome;
        }

        public IGenome fetchRandomNetwork()
        {
            //we create our objects, making sure any unique node or connection is saved in WIN 
            //(in reality, after the first few individuals, there shouldn't be any new nodes or connections created from random networks)

            //if we have a seed, EVERYTHIGN MUST COME FROM TEH SEED
            if (officialSeedGenome != null)
                return GenomeFactory.CreateGenomePreserveID(officialSeedGenome, idgen);

            //we need to verify that we aren't duplicating nodes or connections while generating this object, and that at the end of this call
            //the idgenerator hasn't been destroyed! In the future, we'll phase out use of the idgenerator, in favor of WIN, but not yet. 
            return GenomeFactory.CreateGenomePreserveID(neatParams, idgen, cppnInputs, cppnOutputs, neatParams.pInitialPopulationInterconnections);

            //big bada boom
        }

        public void GenomeCreated(IGenome genome)
        {
            //only add it if you haven't seen this genome before (might be making copies for the sake of novelty search/archive)
            if(allGeneratedGenomes.GenomeByID(genome.GenomeId) == null)
                allGeneratedGenomes.Add(genome);
        }

        #endregion



        //This is all really inefficient, but I don't really care. This isn't forever, and I think I'll make a better object for tracking selection/deselection

        #region Select/Deselect Objects

        /// <summary>
        /// Returns genomes from list of IDs, NO DUPLICATES
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public Dictionary<long, IGenome> getGenomesFromIDs(IEnumerable<long> ids)
        {
            Dictionary<long, IGenome> genomeByID = new Dictionary<long,IGenome>();
          
            foreach(long id in ids){
                if(!genomeByID.ContainsKey(id))
                    genomeByID.Add(id, allGeneratedGenomes.GenomeByID(id));
            }

            return genomeByID;
        }
        public IGenome getGenomeFromID(long id)
        {
            return allGeneratedGenomes.GenomeByID(id);
        }

        public bool isSelected(long genomeID)
        {
            return genomeListDictionary[CurrentGenomePool].GenomeByID(genomeID) != null;
        }
        
        public bool toggleSelect(long uid)
        {
            if (isSelected(uid))
               return deselectGenome(uid);
            else
               return selectGenome(uid);

        }
        public bool toggleSelect(IGenome genome)
        {
            if (isSelected(genome.GenomeId))
                deselectGenome(genome);
            else
                selectGenome(genome);

            return true;
        }
        public bool selectGenome(long uid)
        {
            //if we have it selected, then go ahead and select
            if (genomeListDictionary[CurrentGenomePool].GenomeByID(uid) == null)
            {
                genomeListDictionary[CurrentGenomePool].Add(allGeneratedGenomes.GenomeByID(uid));
                return true;
            }
            //already selected, this does nothing for us
            return false;
        }
        public bool selectGenome(IGenome genome)
        {
            if (genomeListDictionary[CurrentGenomePool].GenomeByID(genome.GenomeId) == null)
            {
                genomeListDictionary[CurrentGenomePool].Add(genome);
                return true;
            }
            return false;
        }

        public bool deselectGenome(long uid)
        {
            return genomeListDictionary[CurrentGenomePool].Remove(allGeneratedGenomes.GenomeByID(uid));
        }
        public bool deselectGenome(IGenome genome)
        {
            return genomeListDictionary[CurrentGenomePool].Remove(allGeneratedGenomes.GenomeByID(genome.GenomeId));
        }

        #endregion

        #region Genetic Algorithm Management

        public EvolutionAlgorithm initializeEvolutionAlgorithm(IPopulationEvaluator popEval, int popSize, AssessGenotypeFunction assess, List<long> parentGenomeIDs = null)
        {
            //have to add our seed to the parents!
            if (officialSeedGenome != null)
            {
                //if we aren't given any parents, make a new list, and add the seed
                if (parentGenomeIDs == null)
                    parentGenomeIDs = new List<long>();

                parentGenomeIDs.Add(officialSeedGenome.GenomeId);
            }

            //create our initial population, using seeds or not, making sure it is at least "popsize" long
            GenomeList gl = createGenomeList(popSize, assess, parentGenomeIDs);           
            
            //now we have a genomelist full of our parents, if they didn't die in a car crash when we were young, yay!
            //also, these parents, their connections, and neurons are safely catalogued by WIN (eventually...)
            Population pop = new Population(idgen, gl);

            //create our algorithm
            evoAlgorithm = new EvolutionAlgorithm(pop, popEval, neatParams, assess);

            createExperimentDirectory();

            //send it away!
            return evoAlgorithm;
        }
        /// <summary>
        /// Creates a genomeList from a random start, or using a list of seed genome IDs
        /// </summary>
        /// <param name="popSize"></param>
        /// <param name="parentGenomeIDs"></param>
        /// <returns></returns>
        public GenomeList createGenomeList(int popSize, AssessGenotypeFunction genoAssess, List<long> parentGenomeIDs = null)
        {
            //must return a genome list!
            GenomeList gl;

            //if we have parents, add their genomes to our starting genomelist
            if (parentGenomeIDs != null)
            {
                GenomeList seeds = new GenomeList(); seeds.AddRange(getGenomesFromIDs(parentGenomeIDs).Values);
                gl = new GenomeList();
                gl.AddRange(GenomeFactory.CreateGenomeListPreserveIDs(seeds, popSize, neatParams, idgen, genoAssess));
            }
            else
            {
                //we don't have any seeds, we need to form our own initial population
                gl = GenomeFactory.CreateGenomeListPreserveIDs(neatParams, idgen, 
                    cppnInputs, cppnOutputs, neatParams.pInitialPopulationInterconnections, popSize, 
                    genoAssess);
            }

            //add each genome to our list of generated genomes, yo. 
            //gl.ForEach(genome => allGeneratedGenomes.Add(genome));

            //now we are free to return the genomes
            return gl;
        }

        #endregion


        public static string ExperimentDirectory = "";

        void createExperimentDirectory()
        {
            DateTime dt = DateTime.Now;

            int attempts = 0;

            string desiredDirectory = "ExperimentData" + dt.Month + "_" + dt.Day + "_" + dt.Year + "_" + dt.Hour + "_" + dt.Minute;

            while (Directory.Exists("ExperimentData" + dt.Month + "_" + dt.Day + "_" + dt.Year + "_" + dt.Hour + "_" + dt.Minute))
                desiredDirectory = (attempts ==0) ? desiredDirectory + attempts++ :  desiredDirectory.Substring(0, desiredDirectory.Length-1) + attempts++;

            Directory.CreateDirectory(desiredDirectory);

            ExperimentDirectory = desiredDirectory;

        }

        string genomeFileName(IGenome genome)
        {
            return "genome" + genome.GenomeId + "_age" + genome.GenomeAge + ".xml";
        }

        public void saveGenomes(IEnumerable<long> genomes)
        {
            Dictionary<long, IGenome> genomeList = this.getGenomesFromIDs(genomes);
            saveGenomes(genomeList.Values);            
        }

        public void saveGenomes(IEnumerable<NeatGenome.NeatGenome> genomes)
        {
            updateEvolutionFile();
            foreach (IGenome genome in genomes)
                saveGenome(genome);
        }
        public void saveGenomes(IEnumerable<IGenome> genomes)
        {
            updateEvolutionFile();
            foreach (IGenome genome in genomes)
                saveGenome(genome);
        }

        public void saveGenome(long genomeID)
        {
            saveGenome(this.getGenomeFromID(genomeID));
        }

        public void saveGenome(IGenome genomeToSave)
        {
            try{

            string genomeAttempt = genomeFileName(genomeToSave);

            string genomePath = ExperimentDirectory + "/" + genomeAttempt;

            if (!File.Exists(genomePath))
            {
                XmlDocument doc = new XmlDocument();
                XmlGenomeWriterStatic.Write(doc, (NeatGenome.NeatGenome)genomeToSave);
                doc.Save(genomePath);
            }
                 }
            catch(Exception e)
            {
                Console.WriteLine("Failed to write genome file: " + e.Message + " Genome: " + genomeToSave.GenomeId);
            }

        }
        public void updateEvolutionFile()
        {
            string evoInfoPathBase = ExperimentDirectory + "/evoInformation";
            string evoInfoPath = evoInfoPathBase + ".txt";

            try{

                //we need to create the evoInformation object
                //we'll serialize neat parameters here as well
            if(!File.Exists(evoInfoPath)){
                
                //create the neat params file!
                XmlSerializer serialize = new XmlSerializer(typeof(NeatParameters));
                XmlWriter xml = XmlWriter.Create(File.Create(evoInfoPathBase + "_" + neatParams + ".xml"));
                serialize.Serialize(xml, evoAlgorithm.NeatParameters);
                xml.Close();
            }

            using (StreamWriter s = File.AppendText(evoInfoPath))
            {
                //new StreamWriter(evoInfoPath + ".txt", true);// File.OpenText(evoInfoPath);//File.Open(evoInfoPath, FileMode.OpenOrCreate);
                s.WriteLine("Last Update: " + DateTime.Now.ToString() + " Evo Info: (Generation: " + evoAlgorithm.Generation + ", Pop Size: "
                 + evoAlgorithm.Population.GenomeList.Count + ", Best Individual (id): " + evoAlgorithm.BestGenome.GenomeId + ")");
            }

            }
            catch(Exception e)
            {
                Console.WriteLine("Failed to write experiment file: " + e.Message);
            }

        }


        public void loadSeed(string seedGenomeFile)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(seedGenomeFile);
            officialSeedGenome = XmlNeatGenomeReaderStatic.Read(doc);
            //since idgen is still in control of genomeIDs, we have to increment it
            //for now... soon win will be in charge. You'll see, things will be different.
            idgen.mostRecentGenomeID(officialSeedGenome.GenomeId);
        }
        public NeatGenome.NeatGenome getSeed()
        {
            return officialSeedGenome;
        }
    }
}
