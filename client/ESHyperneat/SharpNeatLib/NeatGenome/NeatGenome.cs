using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;
using SharpNeatLib.Evolution;
using SharpNeatLib.Maths;
using SharpNeatLib.NeatGenome.Xml;
using SharpNeatLib.NeuralNetwork;
using SharpNeatLib.Masters;

namespace SharpNeatLib.NeatGenome
{
	public class NeatGenome : AbstractGenome
	{	
		#region NeuronConnectionLookup Class [Pruning]

		class NeuronConnectionLookup
		{
			public NeuronGene neuronGene;
			public ConnectionGeneList incomingList = new ConnectionGeneList();
			public ConnectionGeneList outgoingList = new ConnectionGeneList();
		}

		#endregion

		#region Class Variables [General]

		// Ensure that the connectionGenes are sorted by innovation ID at all times.
		NeuronGeneList neuronGeneList;
        List<ModuleGene> moduleGeneList;
		ConnectionGeneList connectionGeneList;

		// For efficiency we store the number of input and output neurons. These two quantities do not change
		// throughout the life of a genome. Note that inputNeuronCount does NOT include the bias neuron! use inputAndBiasNeuronCount.
		// We also keep all input(including bias) neurons at the start of the neuronGeneList followed by 
		// the output neurons.
		int inputNeuronCount;
		int inputAndBiasNeuronCount;
		int outputNeuronCount;
		int inputBiasOutputNeuronCount;
		int inputBiasOutputNeuronCountMinus2;

        public bool networkAdaptable;
        public bool networkModulatory;

		// Build on-demand to represnt all of the ConnectionGene that do not have the FixedWeight bit set, 
		// so that MutateConnectionWeights can operate more efficiently.
		ConnectionGeneList mutableConnectionGeneList = null;

		#endregion

		#region Class Variables [Pruning]
		// Temp tables.
		Hashtable neuronConnectionLookupTable = null;
		Hashtable neuronGeneTable = null;

        public NeatGenome parent;

		#endregion

		#region Constructors

        public NeatGenome(long genomeId,
                        NeuronGeneList neuronGeneList,
                        ConnectionGeneList connectionGeneList,
                        int inputNeuronCount,
                        int outputNeuronCount)
            : this(genomeId, neuronGeneList, new List<ModuleGene>(), connectionGeneList, inputNeuronCount, outputNeuronCount) { }

        public NeatGenome(long genomeId,
                NeuronGeneList neuronGeneList,
                List<ModuleGene> moduleGeneList,
                ConnectionGeneList connectionGeneList,
                int inputNeuronCount,
                int outputNeuronCount)
        {
			this.genomeId = genomeId;

			this.neuronGeneList = neuronGeneList;
            this.moduleGeneList = moduleGeneList;
			this.connectionGeneList = connectionGeneList;

			this.inputNeuronCount = inputNeuronCount;
			this.inputAndBiasNeuronCount = inputNeuronCount+1;
			this.outputNeuronCount = outputNeuronCount;
			this.inputBiasOutputNeuronCount = inputAndBiasNeuronCount + outputNeuronCount;
			this.inputBiasOutputNeuronCountMinus2 = inputBiasOutputNeuronCount-2;

            //all genomes must be catalogued by the evolution manager
            EvolutionManager.SharedEvolutionManager.GenomeCreated(this);

            if (!connectionGeneList.IsSorted())
            {
                connectionGeneList.IsSorted();
            }

			Debug.Assert(connectionGeneList.IsSorted(), "ConnectionGeneList is not sorted by innovation ID");
		}

		/// <summary>
		/// Copy constructor.
		/// </summary>
		/// <param name="copyFrom"></param>
        public NeatGenome(NeatGenome copyFrom, long genomeId)
		{
			this.genomeId = genomeId;
            this.parent = copyFrom;

			// No need to loop the arrays to clone each element because NeuronGene and ConnectionGene are 
			// value data types (structs).
			neuronGeneList = new NeuronGeneList(copyFrom.neuronGeneList);
            moduleGeneList = new List<ModuleGene>(copyFrom.moduleGeneList);
			connectionGeneList = new ConnectionGeneList(copyFrom.connectionGeneList);

            //joel
            if(copyFrom.Behavior!=null)
            Behavior = new SharpNeatLib.BehaviorType(copyFrom.Behavior);
            
			inputNeuronCount = copyFrom.inputNeuronCount;
			inputAndBiasNeuronCount = copyFrom.inputNeuronCount+1;
			outputNeuronCount = copyFrom.outputNeuronCount;
			inputBiasOutputNeuronCount = copyFrom.inputBiasOutputNeuronCount;
			inputBiasOutputNeuronCountMinus2 = copyFrom.inputBiasOutputNeuronCountMinus2;

            //all genomes must be catalogued by the evolution manager
            EvolutionManager.SharedEvolutionManager.GenomeCreated(this);

            if (!connectionGeneList.IsSorted())
            {
                connectionGeneList.IsSorted();
            }

			Debug.Assert(connectionGeneList.IsSorted(), "ConnectionGeneList is not sorted by innovation ID");
		}

		#endregion

		#region Properties

		public NeuronGeneList NeuronGeneList
		{
			get
			{
				return neuronGeneList;
			}
		}

        public List<ModuleGene> ModuleGeneList
        {
            get
            {
                return moduleGeneList;
            }
        }

		public ConnectionGeneList ConnectionGeneList
		{
			get
			{
				return connectionGeneList;
			}
		}

		public int InputNeuronCount
		{
			get
			{
				return inputNeuronCount;
			}
		}

		public int OutputNeuronCount
		{
			get
			{
				return outputNeuronCount;
			}
		}

		#endregion

		#region IGenome

		/// <summary>
		/// Some(most) types of network have fixed numbers of input and output nodes and will not work correctly or
		/// throw an exception if we try and use inputs/outputs that do not exist. This method allows us to check
		/// compatibility before we begin.
		/// </summary>
		/// <param name="inputCount"></param>
		/// <param name="outputCount"></param>
		/// <returns></returns>
		public override bool IsCompatibleWithNetwork(int inputCount, int outputCount)
		{
			return (inputCount==inputNeuronCount) && (outputCount==outputNeuronCount);
		}

		/// <summary>
		/// Asexual reproduction with built in mutation.
		/// </summary>
		/// <returns></returns>
		public override IGenome CreateOffspring_Asexual(EvolutionAlgorithm ea)
		{
			// Make an exact copy this Genome.
			NeatGenome offspring = new NeatGenome(this, ea.NextGenomeId);

			// Mutate the new genome.
			offspring.Mutate(ea.NeatParameters, ea.Population.IdGenerator, ea.NewNeuronGeneStructTable, ea.NewConnectionGeneTable);
			return offspring;
		}

        public IGenome CreateOffspring_Asexual(NeatParameters neatParameters, IdGenerator idGen, Hashtable newNeuronTable, Hashtable newConnectionTable)
        {
            // Make an exact copy this Genome.
            NeatGenome offspring = new NeatGenome(this, idGen.NextGenomeId);

            // Mutate the new genome.
            //WIN acknowledges any connection or node mutations, but doesn't save any other mutations
            offspring.Mutate(neatParameters, idGen, newNeuronTable, newConnectionTable);

            return offspring;
        }

        /// <summary>
        /// Adds a connection to the list that will eventually be copied into a child of this genome during sexual reproduction.
        /// A helper function that is only called by CreateOffspring_Sexual_ProcessCorrelationItem().
        /// </summary>
        /// <param name="connectionGene">Specifies the connection to add to this genome.</param>
        /// <param name="overwriteExisting">If there is already a connection from the same source to the same target,
        /// that connection is replaced when overwriteExisting is true and remains (no change is made) when overwriteExisting is false.</param>
		private void CreateOffspring_Sexual_AddGene(ConnectionGene connectionGene, bool overwriteExisting)
		{
			ConnectionEndpointsStruct connectionKey = new ConnectionEndpointsStruct(
																connectionGene.SourceNeuronId, 
																connectionGene.TargetNeuronId);

			// Check if a matching gene has already been added.
			object oIdx = newConnectionGeneTable[connectionKey];
			if(oIdx==null)
			{	// No matching gene has been added.
				// Register this new gene with the newConnectionGeneTable - store its index within newConnectionGeneList.
				newConnectionGeneTable[connectionKey] = newConnectionGeneList.Count;

				// Add the gene to the list.
				newConnectionGeneList.Add(connectionGene);
			}
			else if(overwriteExisting)
			{
				// Overwrite the existing matching gene with this one. In fact only the weight value differs between two
				// matching connection genes, so just overwrite the existing genes weight value.
				
				// Remember that we stored the gene's index in newConnectionGeneTable. So use it here.
				newConnectionGeneList[(int)oIdx].Weight = connectionGene.Weight;
			}
		}


        /// <summary>
        /// Given a description of a connection in two parents, decide how to copy it into their child.
        /// A helper function that is only called by CreateOffspring_Sexual().
        /// </summary>
        /// <param name="correlationItem">Describes a connection and whether it exists on one parent, the other, or both.</param>
        /// <param name="fitSwitch">If this is 1, then the first parent is more fit; if 2 then the second parent. Other values are not defined.</param>
        /// <param name="combineDisjointExcessFlag">If this is true, add disjoint and excess genes to the child; otherwise, leave them out.</param>
        /// <param name="np">Not used.</param>
		private void CreateOffspring_Sexual_ProcessCorrelationItem(CorrelationItem correlationItem,
                                                                   byte fitSwitch,
                                                                   bool combineDisjointExcessFlag,
                                                                   NeatParameters np)
		{
			switch(correlationItem.CorrelationItemType)
			{	
				// Disjoint and excess genes.
				case CorrelationItemType.DisjointConnectionGene:
				case CorrelationItemType.ExcessConnectionGene:
				{
					// If the gene is in the fittest parent then override any existing entry in the connectionGeneTable.
					if(fitSwitch==1 && correlationItem.ConnectionGene1!=null)
					{
						CreateOffspring_Sexual_AddGene(correlationItem.ConnectionGene1, true);
						return;
					}

					if(fitSwitch==2 && correlationItem.ConnectionGene2!=null)
					{
						CreateOffspring_Sexual_AddGene(correlationItem.ConnectionGene2, true);
						return;
					}

					// The disjoint/excess gene is on the less fit parent.
                    //if(Utilities.NextDouble() < np.pDisjointExcessGenesRecombined)	// Include the gene n% of the time from whichever parent contains it.
					if(combineDisjointExcessFlag)
					{
						if(correlationItem.ConnectionGene1!=null)
						{
							CreateOffspring_Sexual_AddGene(correlationItem.ConnectionGene1, false);
							return;
						}
						if(correlationItem.ConnectionGene2!=null)
						{
							CreateOffspring_Sexual_AddGene(correlationItem.ConnectionGene2, false);
							return;
						}
					}
					break;
				}

                case CorrelationItemType.MatchedConnectionGenes:
				{
					if(RouletteWheel.SingleThrow(0.5))
					{
						// Override any existing entries in the table.
						CreateOffspring_Sexual_AddGene(correlationItem.ConnectionGene1, true);
					}
					else
					{
						// Override any existing entries in the table.
						CreateOffspring_Sexual_AddGene(correlationItem.ConnectionGene2, true);
					}
					break;
				}
			} 
		}

		/// <summary>
		/// A table that keeps a track of which connections have added to the sexually reproduced child genome.
		/// This is cleared on each call to CreateOffspring_Sexual() and is only declared at class level to
		/// prevent having to re-allocate the table and it's associated memory on each invokation.
		/// </summary>
		Hashtable newConnectionGeneTable;
		Hashtable newNeuronGeneTable;
		ConnectionGeneList newConnectionGeneList;
        public int nearestNeighbors;

        public IGenome CreateOffspring_Sexual(NeatParameters np, IdGenerator idGen, IGenome parent)
        {
            NeatGenome otherParent = parent as NeatGenome;
            if (otherParent == null)
                return null;

            // Build a list of connections in either this genome or the other parent.
            CorrelationResults correlationResults = CorrelateConnectionGeneLists(connectionGeneList, otherParent.connectionGeneList);
            Debug.Assert(correlationResults.PerformIntegrityCheck(), "CorrelationResults failed integrity check.");

            //----- Connection Genes.
            // We will temporarily store the offspring's genes in newConnectionGeneList and keeping track of which genes
            // exist with newConnectionGeneTable. Here we ensure these objects are created, and if they already existed
            // then ensure they are cleared. Clearing existing objects is more efficient that creating new ones because
            // allocated memory can be re-used.

            // Key = connection key, value = index in newConnectionGeneList.
            if (newConnectionGeneTable == null)
            {	// Provide a capacity figure to the new Hashtable. The offspring will be the same length (or thereabouts).
                newConnectionGeneTable = new Hashtable(connectionGeneList.Count);
            }
            else
            {
                newConnectionGeneTable.Clear();
            }
            //TODO: No 'capacity' constructor on CollectionBase. Create modified/custom CollectionBase.
            // newConnectionGeneList must be constructed on each call because it is passed to a new NeatGenome 
            // at construction time and a permanent reference to the list is kept.
            newConnectionGeneList = new ConnectionGeneList(ConnectionGeneList.Count);

            // A switch that stores which parent is fittest 1 or 2. Chooses randomly if both are equal. More efficient to calculate this just once.
            byte fitSwitch;
            if (Fitness > otherParent.Fitness)
                fitSwitch = 1;
            else if (Fitness < otherParent.Fitness)
                fitSwitch = 2;
            else
            {	// Select one of the parents at random to be the 'master' genome during crossover.
                if (Utilities.NextDouble() < 0.5)
                    fitSwitch = 1;
                else
                    fitSwitch = 2;
            }

            bool combineDisjointExcessFlag = Utilities.NextDouble() < np.pDisjointExcessGenesRecombined;

            // Loop through the correlationResults, building a table of ConnectionGenes from the parents that will make it into our 
            // new [single] offspring. We use a table keyed on connection end points to prevent passing connections to the offspring 
            // that may have the same end points but a different innovation number - effectively we filter out duplicate connections.
            int idxBound = correlationResults.CorrelationItemList.Count;
            for (int i = 0; i < idxBound; i++)
            {
                CreateOffspring_Sexual_ProcessCorrelationItem((CorrelationItem)correlationResults.CorrelationItemList[i], fitSwitch, combineDisjointExcessFlag, np);
            }

            //----- Neuron Genes.
            // Build a neuronGeneList by analysing each connection's neuron end-point IDs.
            // This strategy has the benefit of eliminating neurons that are no longer connected too.
            // Remember to always keep all input, output and bias neurons though!
            NeuronGeneList newNeuronGeneList = new NeuronGeneList(neuronGeneList.Count);

            // Keep a table of the NeuronGene ID's keyed by ID so that we can keep track of which ones have been added.
            // Key = innovation ID, value = null for some reason.
            if (newNeuronGeneTable == null)
                newNeuronGeneTable = new Hashtable(neuronGeneList.Count);
            else
                newNeuronGeneTable.Clear();

            // Get the input/output neurons from this parent. All Genomes share these neurons, they do not change during a run.
            idxBound = neuronGeneList.Count;
            for (int i = 0; i < idxBound; i++)
            {
                if (neuronGeneList[i].NeuronType != NeuronType.Hidden)
                {
                    newNeuronGeneList.Add(new NeuronGene(neuronGeneList[i]));
                    newNeuronGeneTable.Add(neuronGeneList[i].InnovationId, null);
                }
                else
                {	// No more bias, input or output nodes. break the loop.
                    break;
                }
            }

            // Now analyse the connections to determine which NeuronGenes are required in the offspring.
            // Loop through every connection in the child, and add to the child those hidden neurons that are sources or targets of the connection.
            idxBound = newConnectionGeneList.Count;
            for (int i = 0; i < idxBound; i++)
            {
                NeuronGene neuronGene;
                ConnectionGene connectionGene = newConnectionGeneList[i];
                if (!newNeuronGeneTable.ContainsKey(connectionGene.SourceNeuronId))
                {
                    //TODO: DAVID proper activation function
                    // We can safely assume that any missing NeuronGenes at this point are hidden heurons.
                    neuronGene = this.neuronGeneList.GetNeuronById(connectionGene.SourceNeuronId);
                    if (neuronGene != null)
                        newNeuronGeneList.Add(new NeuronGene(neuronGene));
                    else
                        newNeuronGeneList.Add(new NeuronGene(otherParent.NeuronGeneList.GetNeuronById(connectionGene.SourceNeuronId)));
                    //newNeuronGeneList.Add(new NeuronGene(connectionGene.SourceNeuronId, NeuronType.Hidden, ActivationFunctionFactory.GetActivationFunction("SteepenedSigmoid")));
                    newNeuronGeneTable.Add(connectionGene.SourceNeuronId, null);
                }

                if (!newNeuronGeneTable.ContainsKey(connectionGene.TargetNeuronId))
                {
                    //TODO: DAVID proper activation function
                    // We can safely assume that any missing NeuronGenes at this point are hidden heurons.
                    neuronGene = this.neuronGeneList.GetNeuronById(connectionGene.TargetNeuronId);
                    if (neuronGene != null)
                        newNeuronGeneList.Add(new NeuronGene(neuronGene));
                    else
                        newNeuronGeneList.Add(new NeuronGene(otherParent.NeuronGeneList.GetNeuronById(connectionGene.TargetNeuronId)));
                    //newNeuronGeneList.Add(new NeuronGene(connectionGene.TargetNeuronId, NeuronType.Hidden, ActivationFunctionFactory.GetActivationFunction("SteepenedSigmoid")));
                    newNeuronGeneTable.Add(connectionGene.TargetNeuronId, null);
                }
            }

            // Determine which modules to pass on to the child in the same way.
            // For each module in this genome or in the other parent, if it was referenced by even one connection add it and all its dummy neurons to the child.
            List<ModuleGene> newModuleGeneList = new List<ModuleGene>();

            // Build a list of modules the child might have, which is a union of the parents' module lists, but they are all copies so we can't just do a union.
            List<ModuleGene> unionParentModules = new List<ModuleGene>(moduleGeneList);
            foreach (ModuleGene moduleGene in otherParent.moduleGeneList)
            {
                bool alreadySeen = false;
                foreach (ModuleGene match in unionParentModules)
                {
                    if (moduleGene.InnovationId == match.InnovationId)
                    {
                        alreadySeen = true;
                        break;
                    }
                }
                if (!alreadySeen)
                {
                    unionParentModules.Add(moduleGene);
                }
            }

            foreach (ModuleGene moduleGene in unionParentModules)
            {
                // Examine each neuron in the child to determine whether it is part of a module.
                foreach (List<long> dummyNeuronList in new List<long>[] { moduleGene.InputIds, moduleGene.OutputIds })
                {
                    foreach (long dummyNeuronId in dummyNeuronList)
                    {
                        if (newNeuronGeneTable.ContainsKey(dummyNeuronId))
                        {
                            goto childHasModule;
                        }
                    }
                }

                continue; // the child does not contain this module, so continue the loop and check for the next module.
            childHasModule: // the child does contain this module, so make sure the child gets all the nodes the module requires to work.

                // Make sure the child has all the neurons in the given module.
                newModuleGeneList.Add(new ModuleGene(moduleGene));
            foreach (List<long> dummyNeuronList in new List<long>[] { moduleGene.InputIds, moduleGene.OutputIds })
                {
                    foreach (long dummyNeuronId in dummyNeuronList)
                    {
                        if (!newNeuronGeneTable.ContainsKey(dummyNeuronId))
                        {
                            newNeuronGeneTable.Add(dummyNeuronId, null);
                            NeuronGene neuronGene = this.neuronGeneList.GetNeuronById(dummyNeuronId);
                            if (neuronGene != null)
                            {
                                newNeuronGeneList.Add(new NeuronGene(neuronGene));
                            }
                            else
                            {
                                newNeuronGeneList.Add(new NeuronGene(otherParent.NeuronGeneList.GetNeuronById(dummyNeuronId)));
                            }
                        }
                    }
                }
            }

            // TODO: Inefficient code?
            newNeuronGeneList.SortByInnovationId();

            //PAUL: WIN should go here some more!
            //in the future, we're going to call out to WIN to create our NeatGenome for us, where it will save it
            //TODO: WIN calls for sexual reproduction

            // newConnectionGeneList is already sorted because it was generated by passing over the list returned by
            // CorrelateConnectionGeneLists() - which is always in order.
            return new NeatGenome(idGen.NextGenomeId, newNeuronGeneList, newModuleGeneList, newConnectionGeneList, inputNeuronCount, outputNeuronCount);
        }
		public override IGenome CreateOffspring_Sexual(EvolutionAlgorithm ea, IGenome parent)
		{
            NeatGenome otherParent = parent as NeatGenome;
            if (otherParent == null)
                return null;
            
            // Build a list of connections in either this genome or the other parent.
			CorrelationResults correlationResults = CorrelateConnectionGeneLists(connectionGeneList, otherParent.connectionGeneList);
            if (!correlationResults.PerformIntegrityCheck())
            {
                CorrelationResults cr = CorrelateConnectionGeneLists(connectionGeneList, otherParent.connectionGeneList);
                cr.PerformIntegrityCheck();

            }
            //Debug.Assert(correlationResults.PerformIntegrityCheck(), "CorrelationResults failed integrity check.");

			//----- Connection Genes.
			// We will temporarily store the offspring's genes in newConnectionGeneList and keeping track of which genes
			// exist with newConnectionGeneTable. Here we ensure these objects are created, and if they already existed
			// then ensure they are cleared. Clearing existing objects is more efficient that creating new ones because
			// allocated memory can be re-used.

            // Key = connection key, value = index in newConnectionGeneList.
			if(newConnectionGeneTable==null)
			{	// Provide a capacity figure to the new Hashtable. The offspring will be the same length (or thereabouts).
				newConnectionGeneTable = new Hashtable(connectionGeneList.Count);
			}
			else
			{
				newConnectionGeneTable.Clear();
			}
			//TODO: No 'capacity' constructor on CollectionBase. Create modified/custom CollectionBase.
			// newConnectionGeneList must be constructed on each call because it is passed to a new NeatGenome 
			// at construction time and a permanent reference to the list is kept.
            newConnectionGeneList = new ConnectionGeneList(ConnectionGeneList.Count);

			// A switch that stores which parent is fittest 1 or 2. Chooses randomly if both are equal. More efficient to calculate this just once.
			byte fitSwitch;
			if(Fitness > otherParent.Fitness)
				fitSwitch = 1;
			else if(Fitness < otherParent.Fitness)
				fitSwitch = 2;
			else
			{	// Select one of the parents at random to be the 'master' genome during crossover.
				if(Utilities.NextDouble() < 0.5)
					fitSwitch = 1;
				else
					fitSwitch = 2;
			}

			bool combineDisjointExcessFlag = Utilities.NextDouble() < ea.NeatParameters.pDisjointExcessGenesRecombined;

			// Loop through the correlationResults, building a table of ConnectionGenes from the parents that will make it into our 
			// new [single] offspring. We use a table keyed on connection end points to prevent passing connections to the offspring 
			// that may have the same end points but a different innovation number - effectively we filter out duplicate connections.
			int idxBound = correlationResults.CorrelationItemList.Count;
			for(int i=0; i<idxBound; i++)
			{
				CreateOffspring_Sexual_ProcessCorrelationItem((CorrelationItem)correlationResults.CorrelationItemList[i], fitSwitch, combineDisjointExcessFlag, ea.NeatParameters);
			}

			//----- Neuron Genes.
			// Build a neuronGeneList by analysing each connection's neuron end-point IDs.
			// This strategy has the benefit of eliminating neurons that are no longer connected too.
			// Remember to always keep all input, output and bias neurons though!
            NeuronGeneList newNeuronGeneList = new NeuronGeneList(neuronGeneList.Count);

			// Keep a table of the NeuronGene ID's keyed by ID so that we can keep track of which ones have been added.
            // Key = innovation ID, value = null for some reason.
			if(newNeuronGeneTable==null)
				newNeuronGeneTable = new Hashtable(neuronGeneList.Count);
			else
				newNeuronGeneTable.Clear();

			// Get the input/output neurons from this parent. All Genomes share these neurons, they do not change during a run.
			idxBound = neuronGeneList.Count;
			for(int i=0; i<idxBound; i++)
			{
				if(neuronGeneList[i].NeuronType != NeuronType.Hidden)
				{
					newNeuronGeneList.Add(new NeuronGene(neuronGeneList[i]));
					newNeuronGeneTable.Add(neuronGeneList[i].InnovationId, null);
				}
				else
				{	// No more bias, input or output nodes. break the loop.
					break;
				}
			}

			// Now analyse the connections to determine which NeuronGenes are required in the offspring.
            // Loop through every connection in the child, and add to the child those hidden neurons that are sources or targets of the connection.
			idxBound = newConnectionGeneList.Count;
			for(int i=0; i<idxBound; i++)
			{
                NeuronGene neuronGene;
				ConnectionGene connectionGene = newConnectionGeneList[i];
				if(!newNeuronGeneTable.ContainsKey(connectionGene.SourceNeuronId))
				{	
                    //TODO: DAVID proper activation function
					// We can safely assume that any missing NeuronGenes at this point are hidden heurons.
                   neuronGene = this.neuronGeneList.GetNeuronById(connectionGene.SourceNeuronId);
                    if (neuronGene != null)
                        newNeuronGeneList.Add(new NeuronGene(neuronGene));
                    else
                        newNeuronGeneList.Add(new NeuronGene(otherParent.NeuronGeneList.GetNeuronById(connectionGene.SourceNeuronId)));
                    //newNeuronGeneList.Add(new NeuronGene(connectionGene.SourceNeuronId, NeuronType.Hidden, ActivationFunctionFactory.GetActivationFunction("SteepenedSigmoid")));
					newNeuronGeneTable.Add(connectionGene.SourceNeuronId, null);
				}

				if(!newNeuronGeneTable.ContainsKey(connectionGene.TargetNeuronId))
				{	
                    //TODO: DAVID proper activation function
					// We can safely assume that any missing NeuronGenes at this point are hidden heurons.
                    neuronGene = this.neuronGeneList.GetNeuronById(connectionGene.TargetNeuronId);
                    if (neuronGene != null)
                        newNeuronGeneList.Add(new NeuronGene(neuronGene));
                    else
                        newNeuronGeneList.Add(new NeuronGene(otherParent.NeuronGeneList.GetNeuronById(connectionGene.TargetNeuronId)));
                    //newNeuronGeneList.Add(new NeuronGene(connectionGene.TargetNeuronId, NeuronType.Hidden, ActivationFunctionFactory.GetActivationFunction("SteepenedSigmoid")));
					newNeuronGeneTable.Add(connectionGene.TargetNeuronId, null);
				}
			}

            // Determine which modules to pass on to the child in the same way.
            // For each module in this genome or in the other parent, if it was referenced by even one connection add it and all its dummy neurons to the child.
            List<ModuleGene> newModuleGeneList = new List<ModuleGene>();

            // Build a list of modules the child might have, which is a union of the parents' module lists, but they are all copies so we can't just do a union.
            List<ModuleGene> unionParentModules = new List<ModuleGene>(moduleGeneList);
            foreach (ModuleGene moduleGene in otherParent.moduleGeneList) {
                bool alreadySeen = false;
                foreach (ModuleGene match in unionParentModules) {
                    if (moduleGene.InnovationId == match.InnovationId) {
                        alreadySeen = true;
                        break;
                    }
                }
                if (!alreadySeen) {
                    unionParentModules.Add(moduleGene);
                }
            }

            foreach (ModuleGene moduleGene in unionParentModules) {
                // Examine each neuron in the child to determine whether it is part of a module.
                foreach (List<long> dummyNeuronList in new List<long>[] { moduleGene.InputIds, moduleGene.OutputIds })
                {
                    foreach (long dummyNeuronId in dummyNeuronList)
                    {
                        if (newNeuronGeneTable.ContainsKey(dummyNeuronId)) {
                            goto childHasModule;
                        }
                    }
                }

                continue; // the child does not contain this module, so continue the loop and check for the next module.
            childHasModule: // the child does contain this module, so make sure the child gets all the nodes the module requires to work.

                // Make sure the child has all the neurons in the given module.
                newModuleGeneList.Add(new ModuleGene(moduleGene));
            foreach (List<long> dummyNeuronList in new List<long>[] { moduleGene.InputIds, moduleGene.OutputIds })
            {
                foreach (long dummyNeuronId in dummyNeuronList)
                {
                        if (!newNeuronGeneTable.ContainsKey(dummyNeuronId)) {
                            newNeuronGeneTable.Add(dummyNeuronId, null);
                            NeuronGene neuronGene = this.neuronGeneList.GetNeuronById(dummyNeuronId);
                            if (neuronGene != null) {
                                newNeuronGeneList.Add(new NeuronGene(neuronGene));
                            } else {
                                newNeuronGeneList.Add(new NeuronGene(otherParent.NeuronGeneList.GetNeuronById(dummyNeuronId)));
                            }
                        }
                    }
                }
            }

			// TODO: Inefficient code?
			newNeuronGeneList.SortByInnovationId();

			// newConnectionGeneList is already sorted because it was generated by passing over the list returned by
			// CorrelateConnectionGeneLists() - which is always in order.
			return new NeatGenome(ea.NextGenomeId, newNeuronGeneList, newModuleGeneList, newConnectionGeneList, inputNeuronCount, outputNeuronCount);
		}



		/// <summary>
		/// Decode the genome's 'DNA' into a working network.
		/// </summary>
		/// <returns></returns>
		public override INetwork Decode(IActivationFunction activationFn)
		{
	//		if(network==null) //commented this out because otherwise all the homogenous agents have the same network
	//		{
				//network = GenomeDecoder.DecodeToConcurrentNetwork(this, activationFn);
				//network = GenomeDecoder.DecodeToFloatFastConcurrentNetwork(this, activationFn);
				//network = GenomeDecoder.DecodeToIntegerFastConcurrentNetwork(this);
				//network = GenomeDecoder.DecodeToFastConcurrentMultiplicativeNetwork(this, activationFn);
                network = GenomeDecoder.DecodeToModularNetwork(this);
    //        }

			return network;
		}

        public INetwork Decode(IActivationFunction activationFn, bool forceFloat)
        {
          //  if (network == null)
           // {
                //network = GenomeDecoder.DecodeToConcurrentNetwork(this, activationFn);
                network = GenomeDecoder.DecodeToFloatFastConcurrentNetwork(this, activationFn);
                //network = GenomeDecoder.DecodeToIntegerFastConcurrentNetwork(this);
                //network = GenomeDecoder.DecodeToFastConcurrentMultiplicativeNetwork(this, activationFn);
                //network = GenomeDecoder.DecodeToModularNetwork(this);
           // }

            return network;
        }

		/// <summary>
		/// Clone this genome.
		/// </summary>
		/// <returns></returns>
		public override IGenome Clone(EvolutionAlgorithm ea)
		{
			// Utilise the copy constructor for cloning.
			return new NeatGenome(this, ea.NextGenomeId);
		}

		public double compat(IGenome comparisonGenome, NeatParameters neatParameters) {
						/* A very simple way of implementing this routine is to call CorrelateConnectionGeneLists and to then loop 
			 * through the correlation items, calculating a compatibility score as we go. However, this routine
			 * is heavily used and in performance tests was shown consume 40% of the CPU time for the core NEAT code.
			 * Therefore this new routine has been rewritten with it's own version of the logic within  
			 * CorrelateConnectionGeneLists. This allows us to only keep comparing genes up to the point where the
			 * threshold is passed. This also eliminates the need to build the correlation results list, this difference
			 * alone is responsible for a 200x performance improvement when testing with a 1664 length genome!!
			 * 
			 * A further optimisation is achieved by comparing the genes starting at the end of the genomes which is
			 * where most disparities are located - new novel genes are always attached to the end of genomes. This
			 * has the result of complicating the routine because we must now invoke additional logic to determine
			 * which genes are excess and when the first disjoint gene is found. This is done with an extra integer:
			 * 
			 * int excessGenesSwitch=0; // indicates to the loop that it is handling the first gene.
			 *						=1;	// Indicates that the first gene was excess and on genome 1.
			 *						=2;	// Indicates that the first gene was excess and on genome 2.
			 *						=3;	// Indicates that there are no more excess genes.
			 * 
			 * This extra logic has a slight performance hit, but this is minor especially in comparison to the savings that
			 * are expected to be achieved overall during a NEAT search.
			 * 
			 * If you have trouble understanding this logic then it might be best to work through the previous version of
			 * this routine (below) that scans through the genomes from start to end, and which is a lot simpler.
			 * 
			 */
			ConnectionGeneList list1 = this.connectionGeneList;
			ConnectionGeneList list2 = ((NeatGenome)comparisonGenome).connectionGeneList;
			int excessGenesSwitch=0;

			// Store these heavily used values locally.
			int list1Count = list1.Count;
			int list2Count = list2.Count;

			//----- Test for special cases.
			if(list1Count==0 && list2Count==0)
			{	// Both lists are empty! No disparities, therefore the genomes are compatible!
				return 0.0;
			}

			if(list1Count==0)
			{	// All list2 genes are excess.
				return ((list2.Count * neatParameters.compatibilityExcessCoeff));
			}

			if(list2Count==0)
			{	
				// All list1 genes are excess.
				return ((list1Count * neatParameters.compatibilityExcessCoeff));
			}

		//----- Both ConnectionGeneLists contain genes - compare the contents.
			double compatibility=0;
			int list1Idx=list1Count-1;
			int list2Idx=list2Count-1;
			ConnectionGene connectionGene1 = list1[list1Idx];
			ConnectionGene connectionGene2 = list2[list2Idx];
			for(;;)
			{
				if(connectionGene2.InnovationId > connectionGene1.InnovationId)
				{	
					// Most common test case(s) at top for efficiency.
					if(excessGenesSwitch==3)
					{	// No more excess genes. Therefore this mismatch is disjoint.
						compatibility += neatParameters.compatibilityDisjointCoeff;
					}
					else if(excessGenesSwitch==2)
					{	// Another excess gene on genome 2.
						compatibility += neatParameters.compatibilityExcessCoeff;
					}
					else if(excessGenesSwitch==1)
					{	// We have found the first non-excess gene.
						excessGenesSwitch=3;
						compatibility += neatParameters.compatibilityDisjointCoeff;
					}
					else //if(excessGenesSwitch==0)
					{	// First gene is excess, and is on genome 2.
						excessGenesSwitch = 2;
						compatibility += neatParameters.compatibilityExcessCoeff;
					}

					// Move to the next gene in list2.
					list2Idx--;
				}
				else if(connectionGene1.InnovationId == connectionGene2.InnovationId)
				{
					// No more excess genes. It's quicker to set this every time than to test if is not yet 3.
					excessGenesSwitch=3;

					// Matching genes. Increase compatibility by weight difference * coeff.
					compatibility += Math.Abs(connectionGene1.Weight-connectionGene2.Weight) * neatParameters.compatibilityWeightDeltaCoeff;

					// Move to the next gene in both lists.
					list1Idx--;
					list2Idx--;
				}
				else // (connectionGene2.InnovationId < connectionGene1.InnovationId)
				{	
					// Most common test case(s) at top for efficiency.
					if(excessGenesSwitch==3)
					{	// No more excess genes. Therefore this mismatch is disjoint.
						compatibility += neatParameters.compatibilityDisjointCoeff;
					}
					else if(excessGenesSwitch==1)
					{	// Another excess gene on genome 1.
						compatibility += neatParameters.compatibilityExcessCoeff;
					}
					else if(excessGenesSwitch==2)
					{	// We have found the first non-excess gene.
						excessGenesSwitch=3;
						compatibility += neatParameters.compatibilityDisjointCoeff;
					}
					else //if(excessGenesSwitch==0)
					{	// First gene is excess, and is on genome 1.
						excessGenesSwitch = 1;
						compatibility += neatParameters.compatibilityExcessCoeff;
					}

					// Move to the next gene in list1.
					list1Idx--;
				}
				

				// Check if we have reached the end of one (or both) of the lists. If we have reached the end of both then 
				// we execute the first 'if' block - but it doesn't matter since the loop is not entered if both lists have 
				// been exhausted.
				if(list1Idx < 0)
				{	
					// All remaining list2 genes are disjoint.
					compatibility +=  (list2Idx+1) * neatParameters.compatibilityDisjointCoeff;
					return (compatibility); //< neatParameters.compatibilityThreshold);
				}

				if(list2Idx < 0)
				{
					// All remaining list1 genes are disjoint.
					compatibility += (list1Idx+1) * neatParameters.compatibilityDisjointCoeff;
					return (compatibility); //< neatParameters.compatibilityThreshold);
				}

				connectionGene1 = list1[list1Idx];
				connectionGene2 = list2[list2Idx];
			}

		}

		public override bool IsCompatibleWithGenome(IGenome comparisonGenome, NeatParameters neatParameters)
		{
			/* A very simple way of implementing this routine is to call CorrelateConnectionGeneLists and to then loop 
			 * through the correlation items, calculating a compatibility score as we go. However, this routine
			 * is heavily used and in performance tests was shown consume 40% of the CPU time for the core NEAT code.
			 * Therefore this new routine has been rewritten with it's own version of the logic within  
			 * CorrelateConnectionGeneLists. This allows us to only keep comparing genes up to the point where the
			 * threshold is passed. This also eliminates the need to build the correlation results list, this difference
			 * alone is responsible for a 200x performance improvement when testing with a 1664 length genome!!
			 * 
			 * A further optimisation is achieved by comparing the genes starting at the end of the genomes which is
			 * where most disparities are located - new novel genes are always attached to the end of genomes. This
			 * has the result of complicating the routine because we must now invoke additional logic to determine
			 * which genes are excess and when the first disjoint gene is found. This is done with an extra integer:
			 * 
			 * int excessGenesSwitch=0; // indicates to the loop that it is handling the first gene.
			 *						=1;	// Indicates that the first gene was excess and on genome 1.
			 *						=2;	// Indicates that the first gene was excess and on genome 2.
			 *						=3;	// Indicates that there are no more excess genes.
			 * 
			 * This extra logic has a slight performance hit, but this is minor especially in comparison to the savings that
			 * are expected to be achieved overall during a NEAT search.
			 * 
			 * If you have trouble understanding this logic then it might be best to work through the previous version of
			 * this routine (below) that scans through the genomes from start to end, and which is a lot simpler.
			 * 
			 */
			ConnectionGeneList list1 = this.connectionGeneList;
			ConnectionGeneList list2 = ((NeatGenome)comparisonGenome).connectionGeneList;
			int excessGenesSwitch=0;

			// Store these heavily used values locally.
			int list1Count = list1.Count;
			int list2Count = list2.Count;

			//----- Test for special cases.
			if(list1Count==0 && list2Count==0)
			{	// Both lists are empty! No disparities, therefore the genomes are compatible!
				return true;
			}

			if(list1Count==0)
			{	// All list2 genes are excess.
				return ((list2.Count * neatParameters.compatibilityExcessCoeff) < neatParameters.compatibilityThreshold);
			}

			if(list2Count==0)
			{	
				// All list1 genes are excess.
				return ((list1Count * neatParameters.compatibilityExcessCoeff) < neatParameters.compatibilityThreshold);
			}

		//----- Both ConnectionGeneLists contain genes - compare the contents.
			double compatibility=0;
			int list1Idx=list1Count-1;
			int list2Idx=list2Count-1;
			ConnectionGene connectionGene1 = list1[list1Idx];
			ConnectionGene connectionGene2 = list2[list2Idx];
			for(;;)
			{
				if(connectionGene2.InnovationId > connectionGene1.InnovationId)
				{	
					// Most common test case(s) at top for efficiency.
					if(excessGenesSwitch==3)
					{	// No more excess genes. Therefore this mismatch is disjoint.
						compatibility += neatParameters.compatibilityDisjointCoeff;
					}
					else if(excessGenesSwitch==2)
					{	// Another excess gene on genome 2.
						compatibility += neatParameters.compatibilityExcessCoeff;
					}
					else if(excessGenesSwitch==1)
					{	// We have found the first non-excess gene.
						excessGenesSwitch=3;
						compatibility += neatParameters.compatibilityDisjointCoeff;
					}
					else //if(excessGenesSwitch==0)
					{	// First gene is excess, and is on genome 2.
						excessGenesSwitch = 2;
						compatibility += neatParameters.compatibilityExcessCoeff;
					}

					// Move to the next gene in list2.
					list2Idx--;
				}
				else if(connectionGene1.InnovationId == connectionGene2.InnovationId)
				{
					// No more excess genes. It's quicker to set this every time than to test if is not yet 3.
					excessGenesSwitch=3;

					// Matching genes. Increase compatibility by weight difference * coeff.
					compatibility += Math.Abs(connectionGene1.Weight-connectionGene2.Weight) * neatParameters.compatibilityWeightDeltaCoeff;

					// Move to the next gene in both lists.
					list1Idx--;
					list2Idx--;
				}
				else // (connectionGene2.InnovationId < connectionGene1.InnovationId)
				{	
					// Most common test case(s) at top for efficiency.
					if(excessGenesSwitch==3)
					{	// No more excess genes. Therefore this mismatch is disjoint.
						compatibility += neatParameters.compatibilityDisjointCoeff;
					}
					else if(excessGenesSwitch==1)
					{	// Another excess gene on genome 1.
						compatibility += neatParameters.compatibilityExcessCoeff;
					}
					else if(excessGenesSwitch==2)
					{	// We have found the first non-excess gene.
						excessGenesSwitch=3;
						compatibility += neatParameters.compatibilityDisjointCoeff;
					}
					else //if(excessGenesSwitch==0)
					{	// First gene is excess, and is on genome 1.
						excessGenesSwitch = 1;
						compatibility += neatParameters.compatibilityExcessCoeff;
					}

					// Move to the next gene in list1.
					list1Idx--;
				}
				
				if(compatibility >= neatParameters.compatibilityThreshold)
					return false;

				// Check if we have reached the end of one (or both) of the lists. If we have reached the end of both then 
				// we execute the first 'if' block - but it doesn't matter since the loop is not entered if both lists have 
				// been exhausted.
				if(list1Idx < 0)
				{	
					// All remaining list2 genes are disjoint.
					compatibility +=  (list2Idx+1) * neatParameters.compatibilityDisjointCoeff;
					return (compatibility < neatParameters.compatibilityThreshold);
				}

				if(list2Idx < 0)
				{
					// All remaining list1 genes are disjoint.
					compatibility += (list1Idx+1) * neatParameters.compatibilityDisjointCoeff;
					return (compatibility < neatParameters.compatibilityThreshold);
				}

				connectionGene1 = list1[list1Idx];
				connectionGene2 = list2[list2Idx];
			}
		}

/* The first version of the optimised IsCompatibleWithGenome(). This version scans forward through the genomes,
 * keeping a running total of the compatibility figure as it goes. This version has been superceded by the one above!
 */
//		public override bool IsCompatibleWithGenome(IGenome comparisonGenome, NeatParameters neatParameters)
//		{
//		/* A very simple way of implementing this routine is to call CorrelateConnectionGeneLists and to then loop 
//			* through the correlation items, calculating a compatibility score as we go. However, this routine
//			* is heavily used and in performance tests was shown consume 40% of the CPU time for the core NEAT code.
//			* Therefore this new routine has been rewritten with it's own version of the logic within  
//			* CorrelateConnectionGeneLists. This allows us to only keep comparing genes up to the point where the
//			* threshold is passed.
//			*/
//			ConnectionGeneList list1 = this.connectionGeneList;
//			ConnectionGeneList list2 = ((NeatGenome)comparisonGenome).connectionGeneList;
//		
//			// Store these heavily used values locally.
//			int list1Count = list1.Count;
//			int list2Count = list2.Count;
//		
//			//----- Test for special cases.
//			if(list1Count==0 && list2Count==0)
//			{	// Both lists are empty! No disparities, therefore the genomes are compatible!
//				return true;
//			}
//		
//			if(list1Count==0)
//			{	// All list2 genes are excess.
//				return ((list2Count * neatParameters.compatibilityExcessCoeff) < neatParameters.compatibilityThreshold);
//			}
//		
//			if(list2Count==0)
//			{	
//				// All list1 genes are excess.
//				return ((list1Count * neatParameters.compatibilityExcessCoeff) < neatParameters.compatibilityThreshold);
//			}
//		
//			//----- Both ConnectionGeneLists contain genes - compare the contents.
//			double compatibility=0;
//			int list1Idx=0;
//			int list2Idx=0;
//			ConnectionGene connectionGene1 = list1[list1Idx];
//			ConnectionGene connectionGene2 = list2[list2Idx];
//			for(;;)
//			{
//				if(connectionGene2.InnovationId < connectionGene1.InnovationId)
//				{	
//					// connectionGene2 is disjoint.
//					compatibility += neatParameters.compatibilityDisjointCoeff;
//		
//					// Move to the next gene in list2.
//					list2Idx++;
//				}
//				else if(connectionGene1.InnovationId == connectionGene2.InnovationId)
//				{
//					// Matching genes. Increase compatibility by weight difference * coeff.
//					compatibility += Math.Abs(connectionGene1.Weight-connectionGene2.Weight) * neatParameters.compatibilityWeightDeltaCoeff;
//		
//					// Move to the next gene in both lists.
//					list1Idx++;
//					list2Idx++;
//				}
//				else // (connectionGene2.InnovationId > connectionGene1.InnovationId)
//				{	
//					// connectionGene1 is disjoint.
//					compatibility += neatParameters.compatibilityDisjointCoeff;
//					
//					// Move to the next gene in list1.
//					list1Idx++;
//				}
//				
//				if(compatibility >= neatParameters.compatibilityThreshold)
//					return false;
//		
//				// Check if we have reached the end of one (or both) of the lists. If we have reached the end of both then 
//				// we execute the first 'if' block - but it doesn't matter since the loop is not entered if both lists have 
//				// been exhausted.
//				if(list1Idx >= list1Count)
//				{	
//					// All remaining list2 genes are excess.
//					compatibility += (list2Count - list2Idx) * neatParameters.compatibilityExcessCoeff;
//					return (compatibility < neatParameters.compatibilityThreshold);
//				}
//		
//				if(list2Idx >= list2Count)
//				{
//					// All remaining list1 genes are excess.
//					compatibility += (list1Count - list1Idx) * neatParameters.compatibilityExcessCoeff;
//					return (compatibility < neatParameters.compatibilityThreshold);
//				}
//		
//				connectionGene1 = list1[list1Idx];
//				connectionGene2 = list2[list2Idx];
//			}
//		}



/* The original CalculateCompatibility function coverted to IsCompatibleWithGenome(). This calls CorrelateConnectionGeneLists() and then calculates
 * a compatibility score from the results. If the score is over the threshold then the genomes are incompatible.
 * This routine is superceded by the far more efficient IsCompatibleWithGenome() method.
 */
//		/// <summary>
//		/// Compare this IGenome with the provided one. This routine is utilized by the speciation logic.
//		/// </summary>
//		/// <param name="comparisonGenome"></param>
//		/// <returns></returns>
//		public override bool IsCompatibleWithGenome(IGenome comparisonGenome, NeatParameters neatParameters)
//		{
//			CorrelationResults correlationResults = CorrelateConnectionGeneLists(connectionGeneList, ((NeatGenome)comparisonGenome).connectionGeneList);
//				
//			double compatibilityVal =	neatParameters.compatibilityDisjointCoeff * correlationResults.CorrelationStatistics.DisjointConnectionGeneCount +
//										neatParameters.compatibilityExcessCoeff * correlationResults.CorrelationStatistics.ExcessConnectionGeneCount;
//				
//			if(correlationResults.CorrelationStatistics.MatchingGeneCount > 0)
//			{
//				compatibilityVal +=	neatParameters.compatibilityWeightDeltaCoeff * correlationResults.CorrelationStatistics.ConnectionWeightDelta;
//			}
//		
//			return compatibilityVal < neatParameters.compatibilityThreshold;		
//		}

		public override void Write(XmlNode parentNode)
		{
			XmlGenomeWriterStatic.Write(parentNode, this);
		}

		/// <summary>
		/// For debug purposes only.
		/// </summary>
		/// <returns>Returns true if genome integrity checks out OK.</returns>
		public override bool PerformIntegrityCheck()
		{
			return connectionGeneList.IsSorted();
		}

		#endregion

		#region Public Methods

		public void FixConnectionWeights()
		{
			int bound = connectionGeneList.Count;
			for(int i=0; i<bound; i++)
				connectionGeneList[i].FixedWeight = true;
				
			// This will now be out of date. Although it should not need to be used after calling FixConnectionWeights.
			mutableConnectionGeneList=null;
		}

		#endregion

		#region Private Methods

        /// <summary>
        /// Paul: Modified the mutation function to take in the required objects, instead of Evolution Algorithm.
        /// This way, mutation isn't tied to a large EA object, but can still function without issue. Useful for interactive evolution framework.
        /// </summary>
        /// <param name="neatParameters"></param>
        /// <param name="idGen"></param>
        /// <param name="genomeHashtable"></param>
        /// <param name="connectionHashTable"></param>
		public void Mutate(NeatParameters neatParameters, IdGenerator idGen, Hashtable genomeHashtable, Hashtable connectionHashTable)
		{
			// Determine the type of mutation to perform.
			double[] probabilities = new double[] 
			{
				neatParameters.pMutateAddNode,
				neatParameters.pMutateAddModule,
				neatParameters.pMutateAddConnection,
				neatParameters.pMutateDeleteConnection,
				neatParameters.pMutateDeleteSimpleNeuron,
				neatParameters.pMutateConnectionWeights
			};

			int outcome = RouletteWheel.SingleThrow(probabilities);

			switch(outcome)
			{
				case 0:
                    //basic support by WIN, but doesn't acknowledge weight changes
					Mutate_AddNode(neatParameters, idGen, genomeHashtable);
					break;
                case 1:
                    //currently not totally supported by WIN
                    Mutate_AddModule(neatParameters, idGen, connectionHashTable);
                    break;
                case 2:
                    //WIN basic support, doesn't acknowledge weight changes
                    Mutate_AddConnection(neatParameters, idGen, connectionHashTable);
                    break;
                case 3:
                    //Doesn't acknowledge weight changes yet
					Mutate_DeleteConnection();
					break;
				case 4:
                    //WIN doesn't acknowledge deletion of neurons yet
					Mutate_DeleteSimpleNeuronStructure(neatParameters, idGen, connectionHashTable);
					break;
				case 5:
                    //Win doesn't yet acknowledge the weight changes
					Mutate_ConnectionWeights(neatParameters);
					break;
			}
		}

		/// <summary>
		/// Add a new node to the Genome. We do this by removing a connection at random and inserting 
		/// a new node and two new connections that make the same circuit as the original connection.
		/// 
		/// This way the new node is properly integrated into the network from the outset.
		/// </summary>
		/// <param name="ea"></param>
        private void Mutate_AddNode(NeatParameters neatParameters, IdGenerator idGen, Hashtable NewNeuronGeneStructTable)
		{
			if(connectionGeneList.Count==0)
				return;

			// Select a connection at random.
			int connectionToReplaceIdx = (int)Math.Floor(Utilities.NextDouble() * connectionGeneList.Count);
			ConnectionGene connectionToReplace = connectionGeneList[connectionToReplaceIdx];
				
			// Delete the existing connection. JOEL: Why delete old connection?
			//connectionGeneList.RemoveAt(connectionToReplaceIdx);

			// Check if this connection has already been split on another genome. If so then we should re-use the
			// neuron ID and two connection ID's so that matching structures within the population maintain the same ID.
			object existingNeuronGeneStruct = NewNeuronGeneStructTable[connectionToReplace.InnovationId];

			NeuronGene newNeuronGene;
			ConnectionGene newConnectionGene1;
			ConnectionGene newConnectionGene2;
            IActivationFunction actFunct;

            WINManager win = WINManager.SharedWIN;
            WINNode node;
            WINConnection connection;

			if(existingNeuronGeneStruct==null)
			{	// No existing matching structure, so generate some new ID's.

                //TODO: DAVID proper random activation function
				// Replace connectionToReplace with two new connections and a neuron.
                actFunct=ActivationFunctionFactory.GetRandomActivationFunction(neatParameters);
                //newNeuronGene = new NeuronGene(ea.NextInnovationId, NeuronType.Hidden, actFunct);

                //we should be making a call to WIN anytime we call for a new innovationID

                //here we create a new node, and two new connections
                //we don't send in a genomeID here, so we get it set for us!
                node = win.createWINNode(new PropertyObject() { { WINNode.SNodeString, NeuronType.Hidden.ToString()}});
                newNeuronGene = new NeuronGene(null, node.UniqueID, (neuronGeneList.GetNeuronById(connectionToReplace.SourceNeuronId).Layer + neuronGeneList.GetNeuronById(connectionToReplace.TargetNeuronId).Layer) / 2.0, NeuronType.Hidden, actFunct);

                //we don't send in any uniqueIDs so they are generated for us, and we update our idGen object
                connection = win.createWINConnection(WINConnection.ConnectionWithProperties(connectionToReplace.SourceNeuronId, newNeuronGene.InnovationId), idGen);
                newConnectionGene1 = new ConnectionGene(connection.UniqueID, connection.SourceID,  connection.TargetID , 1.0);

                //we don't send in any uniqueIDs so they are generated for us, and we update our idGen object
                connection = win.createWINConnection(WINConnection.ConnectionWithProperties(newNeuronGene.InnovationId, connectionToReplace.TargetNeuronId), idGen);
                newConnectionGene2 = new ConnectionGene(connection.UniqueID, connection.SourceID, connection.TargetID, connectionToReplace.Weight);

				// Register the new ID's with NewNeuronGeneStructTable.
				NewNeuronGeneStructTable.Add(connectionToReplace.InnovationId,
												new NewNeuronGeneStruct(newNeuronGene, newConnectionGene1, newConnectionGene2));
			}
			else
			{	// An existing matching structure has been found. Re-use its ID's

                //Since we don't call idGen.nextinnovationID,  we don't have to create anything new
                //however, we need to note the change in weights override previous weight values
                //this should be documented in WIN - either explicitly (calling a function to note changes) or implicitly (by scanning the changes in a saved genome)
                //Probably explicitly, calling a mutate function for a SingleStep (since we are in the SingleStep process of creating new individuals)

                //TODO: DAVID proper random activation function
				// Replace connectionToReplace with two new connections and a neuron.
                actFunct = ActivationFunctionFactory.GetRandomActivationFunction(neatParameters);
				NewNeuronGeneStruct tmpStruct = (NewNeuronGeneStruct)existingNeuronGeneStruct;
                //newNeuronGene = new NeuronGene(tmpStruct.NewNeuronGene.InnovationId, NeuronType.Hidden, actFunct);
                newNeuronGene = new NeuronGene(null, tmpStruct.NewNeuronGene.InnovationId, tmpStruct.NewNeuronGene.Layer, NeuronType.Hidden, actFunct);
				
				newConnectionGene1 = new ConnectionGene(tmpStruct.NewConnectionGene_Input.InnovationId, connectionToReplace.SourceNeuronId, newNeuronGene.InnovationId, 1.0);
				newConnectionGene2 = new ConnectionGene(tmpStruct.NewConnectionGene_Output.InnovationId, newNeuronGene.InnovationId, connectionToReplace.TargetNeuronId, connectionToReplace.Weight);
			}

			// Add the new genes to the genome.
			neuronGeneList.Add(newNeuronGene);
			connectionGeneList.InsertIntoPosition(newConnectionGene1);
			connectionGeneList.InsertIntoPosition(newConnectionGene2);
		}

        private void Mutate_AddModule(NeatParameters neatParameters, IdGenerator idGen, Hashtable NewConnectionGeneTable)
        {
            // Find all potential inputs, or quit if there are not enough. 
            // Neurons cannot be inputs if they are dummy input nodes created for another module.
            NeuronGeneList potentialInputs = new NeuronGeneList();
            foreach (NeuronGene n in neuronGeneList) {
                if (!(n.ActivationFunction is ModuleInputNeuron)) {
                    potentialInputs.Add(n);
                }
            }
            if (potentialInputs.Count < 1)
                return;

            // Find all potential outputs, or quit if there are not enough.
            // Neurons cannot be outputs if they are dummy input or output nodes created for another module, or network input or bias nodes.
            NeuronGeneList potentialOutputs = new NeuronGeneList();
            foreach (NeuronGene n in neuronGeneList) {
                if (n.NeuronType != NeuronType.Bias && n.NeuronType != NeuronType.Input
                        && !(n.ActivationFunction is ModuleInputNeuron)
                        && !(n.ActivationFunction is ModuleOutputNeuron)) {
                    potentialOutputs.Add(n);
                }
            }
            if (potentialOutputs.Count < 1)
                return;

            // Pick a new function for the new module.
            IModule func = ModuleFactory.GetRandom();

            WINManager win = WINManager.SharedWIN;
            WINNode node;
            WINConnection connection;


            // Choose inputs uniformly at random, with replacement.
            // Create dummy neurons to represent the module's inputs.
            // Create connections between the input nodes and the dummy neurons.
            IActivationFunction inputFunction = ActivationFunctionFactory.GetActivationFunction("ModuleInputNeuron");
            List<long> inputDummies = new List<long>(func.InputCount);
            for (int i = 0; i < func.InputCount; i++) {

                //we are calling nextinnovationID, this is the place for WIN!
                //in reality, win should know the activation function as well, but that's not currently implemented 

                //here we create a new node, and two new connections
                //we don't send in a genomeID here, so we get it set for us!
                //do we need to inform it of the activation function? I think so?
                node = win.createWINNode(new PropertyObject() { { WINNode.SNodeString, NeuronType.Hidden.ToString()}});
              
                NeuronGene newNeuronGene = new NeuronGene(node.UniqueID, NeuronType.Hidden, inputFunction);
                neuronGeneList.Add(newNeuronGene);

                long sourceId = potentialInputs[Utilities.Next(potentialInputs.Count)].InnovationId;
                long targetId = newNeuronGene.InnovationId;

                inputDummies.Add(targetId);

                //aha! we must call the innovationID again, we check against win
               
                //we don't send in any uniqueIDs so they are generated for us, and we update our idGen object
                connection = win.createWINConnection(
                    WINConnection.ConnectionWithProperties(sourceId, targetId), idGen);
               

                // Create a new connection with a new ID and add it to the Genome.
                ConnectionGene newConnectionGene = new ConnectionGene(connection.UniqueID, connection.SourceID, connection.TargetID, 
                    (Utilities.NextDouble() * neatParameters.connectionWeightRange) - neatParameters.connectionWeightRange / 2.0
                    );

                // Register the new connection with NewConnectionGeneTable.
                ConnectionEndpointsStruct connectionKey = new ConnectionEndpointsStruct(sourceId, targetId);
                NewConnectionGeneTable.Add(connectionKey, newConnectionGene);

                // Add the new gene to this genome. We have a new ID so we can safely append the gene to the end 
                // of the list without risk of breaking the innovation ID order.
                connectionGeneList.Add(newConnectionGene);
            }

            // Choose outputs uniformly at random, with replacement.
            // Create dummy neurons to represent the module's outputs.
            // Create connections between the output nodes and the dummy neurons.
            IActivationFunction outputFunction = ActivationFunctionFactory.GetActivationFunction("ModuleOutputNeuron");
            List<long> outputDummies = new List<long>(func.OutputCount);
            for (int i = 0; i < func.OutputCount; i++) {

                node = win.createWINNode(new PropertyObject() { { WINNode.SNodeString, NeuronType.Hidden.ToString() } });

                NeuronGene newNeuronGene = new NeuronGene(node.UniqueID, NeuronType.Hidden, outputFunction);
                neuronGeneList.Add(newNeuronGene);

                long sourceId = newNeuronGene.InnovationId;
                long targetId = potentialOutputs[Utilities.Next(potentialOutputs.Count)].InnovationId;

                outputDummies.Add(sourceId);

                connection = win.createWINConnection(
                   WINConnection.ConnectionWithProperties(sourceId, targetId), idGen);
               

                // Create a new connection with a new ID and add it to the Genome.
                ConnectionGene newConnectionGene = new ConnectionGene(connection.UniqueID, connection.SourceID, connection.TargetID,
                   (Utilities.NextDouble() * neatParameters.connectionWeightRange) - neatParameters.connectionWeightRange / 2.0                    
                    );

                    //new ConnectionGene(idGen.NextInnovationId, sourceId, targetId,
                    //(Utilities.NextDouble() * neatParameters.connectionWeightRange) - neatParameters.connectionWeightRange / 2.0);

                // Register the new connection with NewConnectionGeneTable.
                ConnectionEndpointsStruct connectionKey = new ConnectionEndpointsStruct(sourceId, targetId);
                NewConnectionGeneTable.Add(connectionKey, newConnectionGene);

                // Add the new gene to this genome. We have a new ID so we can safely append the gene to the end 
                // of the list without risk of breaking the innovation ID order.
                connectionGeneList.Add(newConnectionGene);
            }

            // Pick a new ID for the new module and create it.
            // Modules do not participate in history comparisons, so we will always create a new innovation ID.
            // We can change this here if it becomes a problem.

            //TODO: Paul check win conditions here
            //this is confusing from a WIN perspective, I don't know if I'm going to support modules
            //I can create a generic NextInnovationID object, but don't know if it's worth it

            ModuleGene newModule = new ModuleGene(idGen.NextInnovationId, func, inputDummies, outputDummies);
            moduleGeneList.Add(newModule);
        }

        private void Mutate_AddConnection(NeatParameters neatParameters, IdGenerator idGen, Hashtable NewConnectionGeneTable)
		{
			// We are always guaranteed to have enough neurons to form connections - because the input/output neurons are
			// fixed. Any domain that doesn't require input/outputs is a bit nonsensical!

			// Make a fixed number of attempts at finding a suitable connection to add. 
			
			if(neuronGeneList.Count>1)
			{	// At least 2 neurons, so we have a chance at creating a connection.

                WINManager win = WINManager.SharedWIN;
                WINConnection connection;

				for(int attempts=0; attempts<5; attempts++)
				{
					// Select candidate source and target neurons. Any neuron can be used as the source. Input neurons 
					// should not be used as a target
					int srcNeuronIdx; 
					int tgtNeuronIdx;
				
					/* Here's some code for adding connections that attempts to avoid any recursive conenctions
					 * within a network by only linking to neurons with innovation id's greater than the source neuron.
					 * Unfortunately this doesn't work because new neurons with large innovations ID's are inserted 
					 * randomly through a network's topology! Hence this code remains here in readyness to be resurrected
					 * as part of some future work to support feedforward nets.
//					if(ea.NeatParameters.feedForwardOnly)
//					{
//						/* We can ensure that all networks are feedforward only by only adding feedforward connections here.
//						 * Feed forward connections fall into one of the following categories.  All references to indexes 
//						 * are indexes within this genome's neuronGeneList:
//						 * 1) Source neuron is an input or hidden node, target is an output node.
//						 * 2) Source is an input or hidden node, target is a hidden node with an index greater than the source node's index.
//						 * 3) Source is an output node, target is an output node with an index greater than the source node's index.
//						 * 
//						 * These rules are easier to understand if you understand how the different types if neuron are arranged within
//						 * the neuronGeneList array. Neurons are arranged in the following order:
//						 * 
//						 * 1) A single bias neuron is always first.
//						 * 2) Experiment specific input neurons.
//						 * 3) Output neurons.
//						 * 4) Hidden neurons.
//						 * 
//						 * The quantity and innovationID of all neurons within the first 3 categories remains fixed throughout the life
//						 * of an experiment, hence we always know where to find the bias, input and output nodes. The number of hidden nodes
//						 * can vary as ne nodes are created, pruned away or perhaps dropped during crossover, however they are always arranged
//						 * newest to oldest, or in other words sorted by innovation idea, lowest ID first. 
//						 * 
//						 * If output neurons were at the end of the list with hidden nodes in the middle then generating feedforward 
//						 * connections would be as easy as selecting a target neuron with a higher index than the source neuron. However, that
//						 * type of arrangement is not conducive to the operation of other routines, hence this routine is a little bit more
//						 * complicated as a result.
//						 */
//					
//						// Ok, for a source neuron we can pick any neuron except the last output neuron.
//						int neuronIdxCount = neuronGeneList.Count;
//						int neuronIdxBound = neuronIdxCount-1;
//
//						// Generate count-1 possibilities and avoid the last output neuron's idx.
//						srcNeuronIdx = (int)Math.Floor(Utilities.NextDouble() * neuronIdxBound);
//						if(srcNeuronIdx>inputBiasOutputNeuronCountMinus2) srcNeuronIdx++;
//						
//
//						// Now generate a target idx depending on what type of neuron srcNeuronIdx is pointing to.
//						if(srcNeuronIdx<inputAndBiasNeuronCount)
//						{	// Source is a bias or input neuron. Target can be any output or hidden neuron.
//							tgtNeuronIdx = inputAndBiasNeuronCount + (int)Math.Floor(Utilities.NextDouble() * (neuronIdxCount-inputAndBiasNeuronCount));
//						}
//						else if(srcNeuronIdx<inputBiasOutputNeuronCount)
//						{	// Source is an output neuron, but not the last output neuron. Target can be any output neuron with an index
//							// greater than srcNeuronIdx.
//							tgtNeuronIdx = (inputAndBiasNeuronCount+1) + (int)Math.Floor(Utilities.NextDouble() * ((inputBiasOutputNeuronCount-1)-srcNeuronIdx));
//						}
//						else 
//						{	// Source is a hidden neuron. Target can be any hidden neuron after srcNeuronIdx or any output neuron.
//							tgtNeuronIdx = (int)Math.Floor(Utilities.NextDouble() * ((neuronIdxBound-srcNeuronIdx)+outputNeuronCount));
//
//							if(tgtNeuronIdx<outputNeuronCount)
//							{	// Map to an output neuron idx.
//								tgtNeuronIdx += inputAndBiasNeuronCount;
//							}
//							else
//							{
//								// Map to one of the hidden neurons after srcNeuronIdx.
//								tgtNeuronIdx = (tgtNeuronIdx-outputNeuronCount)+srcNeuronIdx+1;
//							}
//						}
//					}

//					// Source neuron can by any neuron. Target neuron is any neuron except input neurons.
//					srcNeuronIdx = (int)Math.Floor(Utilities.NextDouble() * neuronGeneList.Count);
//					tgtNeuronIdx = inputAndBiasNeuronCount + (int)Math.Floor(Utilities.NextDouble() * (neuronGeneList.Count-inputAndBiasNeuronCount));
//
//                  NeuronGene sourceNeuron = neuronGeneList[srcNeuronIdx];
//                  NeuronGene targetNeuron = neuronGeneList[tgtNeuronIdx];

                    // Find all potential inputs, or quit if there are not enough. 
                    // Neurons cannot be inputs if they are dummy input nodes of a module.
                    NeuronGeneList potentialInputs = new NeuronGeneList();
                    foreach (NeuronGene n in neuronGeneList) {
                        if (!(n.ActivationFunction is ModuleInputNeuron)) {
                            potentialInputs.Add(n);
                        }
                    }
                    if (potentialInputs.Count < 1)
                        return;

                    // Find all potential outputs, or quit if there are not enough.
                    // Neurons cannot be outputs if they are dummy input or output nodes of a module, or network input or bias nodes.
                    NeuronGeneList potentialOutputs = new NeuronGeneList();
                    foreach (NeuronGene n in neuronGeneList) {
                        if (n.NeuronType != NeuronType.Bias && n.NeuronType != NeuronType.Input
                                && !(n.ActivationFunction is ModuleInputNeuron)
                                && !(n.ActivationFunction is ModuleOutputNeuron)) {
                            potentialOutputs.Add(n);
                        }
                    }
                    if (potentialOutputs.Count < 1)
                        return;

                    NeuronGene sourceNeuron = potentialInputs[Utilities.Next(potentialInputs.Count)];
                    NeuronGene targetNeuron = potentialOutputs[Utilities.Next(potentialOutputs.Count)];

					// Check if a connection already exists between these two neurons.
                    long sourceId = sourceNeuron.InnovationId;
                    long targetId = targetNeuron.InnovationId;

                  

					if(!TestForExistingConnection(sourceId, targetId))
					{
						// Check if a matching mutation has already occured on another genome. 
						// If so then re-use the connection ID.
						ConnectionEndpointsStruct connectionKey = new ConnectionEndpointsStruct(sourceId, targetId);
						ConnectionGene existingConnection = (ConnectionGene)NewConnectionGeneTable[connectionKey];
						ConnectionGene newConnectionGene;
						if(existingConnection==null)
						{	
                            //WIN HAS ARRIVED, BITCH
                            //create our newest conncetion, noting the source,target and weight
                            connection = win.createWINConnection(
                   WINConnection.ConnectionWithProperties(sourceId, targetId), idGen);
               

                            // Create a new connection with a new ID and add it to the Genome.
                            newConnectionGene = new ConnectionGene(connection.UniqueID, connection.SourceID, connection.TargetID, 
                                 (Utilities.NextDouble() * neatParameters.connectionWeightRange / 4.0) - neatParameters.connectionWeightRange / 8.0);

                            //newConnectionGene = 
                                //new ConnectionGene(idGen.NextInnovationId, sourceId, targetId,
                                //(Utilities.NextDouble() * neatParameters.connectionWeightRange/4.0) - neatParameters.connectionWeightRange/8.0);

							// Register the new connection with NewConnectionGeneTable.
							NewConnectionGeneTable.Add(connectionKey, newConnectionGene);

							// Add the new gene to this genome. We have a new ID so we can safely append the gene to the end 
							// of the list without risk of breaking the innovation ID order.
							connectionGeneList.Add(newConnectionGene);
						}
						else
						{	
                            //TODO: WIN should note the mutation in weight from previous connection
                            //WIN AIN'T HERE YET, DAWG. Nothing new being created, just adjusted weights -- which we'll need to note, but not yet

                            // Create a new connection, re-using the ID from existingConnection, and add it to the Genome.
							newConnectionGene = new ConnectionGene(existingConnection.InnovationId, sourceId, targetId,
								(Utilities.NextDouble() * neatParameters.connectionWeightRange/4.0) - neatParameters.connectionWeightRange/8.0);

							// Add the new gene to this genome. We are re-using an ID so we must ensure the connection gene is
							// inserted into the correct position (sorted by innovation ID).
							connectionGeneList.InsertIntoPosition(newConnectionGene);
						}
					
						return;
					}
				}
			}

			// We couldn't find a valid connection to create. Instead of doing nothing lets perform connection
			// weight mutation.
			Mutate_ConnectionWeights(neatParameters);
		}

		private void Mutate_DeleteConnection()
		{
            //TODO: Inform WIN of the removal
			if(connectionGeneList.Count==0)
				return;

			// Select a connection at random.
			int connectionToDeleteIdx = (int)Math.Floor(Utilities.NextDouble() * connectionGeneList.Count);
			ConnectionGene connectionToDelete = connectionGeneList[connectionToDeleteIdx];

			// Delete the connection.
			connectionGeneList.RemoveAt(connectionToDeleteIdx);

			// Remove any neurons that may have been left floating.
			if(IsNeuronRedundant(connectionToDelete.SourceNeuronId))
				neuronGeneList.Remove(connectionToDelete.SourceNeuronId);

			// Recurrent connection has both end points at the same neuron!
			if(connectionToDelete.SourceNeuronId !=connectionToDelete.TargetNeuronId)
				if(IsNeuronRedundant(connectionToDelete.TargetNeuronId))
					neuronGeneList.Remove(connectionToDelete.TargetNeuronId);
		}


		/// <summary>
		/// We define a simple neuron structure as a neuron that has a single outgoing or single incoming connection.
		/// With such a structure we can easily eliminate the neuron and shift it's connections to an adjacent neuron.
		/// If the neuron's non-linearity was not being used then such a mutation is a simplification of the network
		/// structure that shouldn't adversly affect its functionality.
		/// </summary>
        private void Mutate_DeleteSimpleNeuronStructure(NeatParameters neatParameters, IdGenerator idGen, Hashtable NewConnectionGeneTable)
		{
            //TODO: WIN acknowledge deletion of neurongene
			// We will use the NeuronConnectionLookupTable to find the simple structures.
			EnsureNeuronConnectionLookupTable();

			// Build a list of candidate simple neurons to choose from.
			ArrayList simpleNeuronIdList = new ArrayList();

			foreach(NeuronConnectionLookup lookup in neuronConnectionLookupTable.Values)
			{
				// If we test the connection count with <=1 then we also pick up neurons that are in dead-end circuits, 
				// RemoveSimpleNeuron is then able to delete these neurons from the network structure along with any 
				// associated connections.
                // All neurons that are part of a module would appear to be dead-ended, but skip removing them anyway.
                if (lookup.neuronGene.NeuronType == NeuronType.Hidden
                            && !(lookup.neuronGene.ActivationFunction is ModuleInputNeuron)
                            && !(lookup.neuronGene.ActivationFunction is ModuleOutputNeuron) ) {
					if((lookup.incomingList.Count<=1) || (lookup.outgoingList.Count<=1))
						simpleNeuronIdList.Add(lookup.neuronGene.InnovationId);
				}
			}

			// Are there any candiate simple neurons?
			if(simpleNeuronIdList.Count==0)
			{	// No candidate neurons. As a fallback lets delete a connection.
				Mutate_DeleteConnection();
				return;
			}

			// Pick a simple neuron at random.
			int idx = (int)Math.Floor(Utilities.NextDouble() * simpleNeuronIdList.Count);
            long neuronId = (long)simpleNeuronIdList[idx];
            RemoveSimpleNeuron(neuronId, neatParameters, idGen, NewConnectionGeneTable);
		}


        private void RemoveSimpleNeuron(long neuronId, NeatParameters neatParameters, IdGenerator idGen, Hashtable NewConnectionGeneTable)
		{
            WINManager win = WINManager.SharedWIN;
            //WINNode node;
            WINConnection connection;
			// Create new connections that connect all of the incoming and outgoing neurons
			// that currently exist for the simple neuron. 
			NeuronConnectionLookup lookup = (NeuronConnectionLookup)neuronConnectionLookupTable[neuronId];
			foreach(ConnectionGene incomingConnection in lookup.incomingList)
			{
				foreach(ConnectionGene outgoingConnection in lookup.outgoingList)
				{
					if(TestForExistingConnection(incomingConnection.SourceNeuronId, outgoingConnection.TargetNeuronId))
					{	// Connection already exists.
						continue;
					}

					// Test for matching connection within NewConnectionGeneTable.
					ConnectionEndpointsStruct connectionKey = new ConnectionEndpointsStruct(incomingConnection.SourceNeuronId, 
																							outgoingConnection.TargetNeuronId);
					ConnectionGene existingConnection = (ConnectionGene)NewConnectionGeneTable[connectionKey];
					ConnectionGene newConnectionGene;
					if(existingConnection==null)
					{	
                       
                        // No matching connection found. Create a connection with a new ID.

                        //create our newest conncetion, noting the source,target and weight
                        connection = win.createWINConnection(
               WINConnection.ConnectionWithProperties(incomingConnection.SourceNeuronId, outgoingConnection.TargetNeuronId), idGen);

                        // Create a new connection with a new ID and add it to the Genome.
                        newConnectionGene = new ConnectionGene(connection.UniqueID, connection.SourceID, connection.TargetID,
                             (Utilities.NextDouble() * neatParameters.connectionWeightRange) - neatParameters.connectionWeightRange / 2.0
                             );


                        //newConnectionGene = new ConnectionGene(idGen.NextInnovationId,
                        //    incomingConnection.SourceNeuronId,
                        //    outgoingConnection.TargetNeuronId,
                        //    (Utilities.NextDouble() * neatParameters.connectionWeightRange) - neatParameters.connectionWeightRange/2.0);

						// Register the new ID with NewConnectionGeneTable.
						NewConnectionGeneTable.Add(connectionKey, newConnectionGene);
	
						// Add the new gene to the genome.
						connectionGeneList.Add(newConnectionGene);
					}
					else
					{	
                        //WIN should acknowledge change in individual here

                        // Matching connection found. Re-use its ID.
						newConnectionGene = new ConnectionGene(existingConnection.InnovationId,
							incomingConnection.SourceNeuronId,
							outgoingConnection.TargetNeuronId,
							(Utilities.NextDouble() * neatParameters.connectionWeightRange) - neatParameters.connectionWeightRange/2.0);

						// Add the new gene to the genome. Use InsertIntoPosition() to ensure we don't break the sort 
						// order of the connection genes.
						connectionGeneList.InsertIntoPosition(newConnectionGene);
					}

					
				}
			}

			// Delete the old connections.
			foreach(ConnectionGene incomingConnection in lookup.incomingList)
				connectionGeneList.Remove(incomingConnection);

			foreach(ConnectionGene outgoingConnection in lookup.outgoingList)
			{	
				// Filter out recurrent connections - they will have already been 
				// deleted in the loop through 'lookup.incomingList'.
				if(outgoingConnection.TargetNeuronId != neuronId)
					connectionGeneList.Remove(outgoingConnection);
			}

			// Delete the simple neuron - it no longer has any connections to or from it.
			neuronGeneList.Remove(neuronId);
		}


		private void MutateConnectionWeight(ConnectionGene connectionGene, NeatParameters np, ConnectionMutationParameterGroup paramGroup)
		{
            //TODO: Inform WIN of the mutation changes
			switch(paramGroup.PerturbationType)
			{
				case ConnectionPerturbationType.JiggleEven:
				{
					connectionGene.Weight += (Utilities.NextDouble()*2-1.0) * paramGroup.PerturbationFactor;

					// Cap the connection weight. Large connections weights reduce the effectiveness of the search.
					connectionGene.Weight = Math.Max(connectionGene.Weight, -np.connectionWeightRange/2.0);
					connectionGene.Weight = Math.Min(connectionGene.Weight, np.connectionWeightRange/2.0);
					break;
				}
				case ConnectionPerturbationType.JiggleND:
				{
					connectionGene.Weight += RandLib.gennor(0, paramGroup.Sigma);

					// Cap the connection weight. Large connections weights reduce the effectiveness of the search.
					connectionGene.Weight = Math.Max(connectionGene.Weight, -np.connectionWeightRange/2.0);
					connectionGene.Weight = Math.Min(connectionGene.Weight, np.connectionWeightRange/2.0);
					break;
				}
				case ConnectionPerturbationType.Reset:
				{
					// TODO: Precalculate connectionWeightRange / 2.
					connectionGene.Weight = (Utilities.NextDouble()*np.connectionWeightRange) - np.connectionWeightRange/2.0;
					break;
				}
				default:
				{
					throw new Exception("Unexpected ConnectionPerturbationType");
				}
			}
		}

        private void Mutate_ConnectionWeights(NeatParameters neatParameters)
		{
			// Determine the type of weight mutation to perform.
			int groupCount = neatParameters.ConnectionMutationParameterGroupList.Count;
			double[] probabilties = new double[groupCount];
			for(int i=0; i<groupCount; i++)
			{
				probabilties[i] = ((ConnectionMutationParameterGroup)neatParameters.ConnectionMutationParameterGroupList[i]).ActivationProportion;
			}

			// Get a reference to the group we will be using.			
			ConnectionMutationParameterGroup paramGroup = (ConnectionMutationParameterGroup)neatParameters.ConnectionMutationParameterGroupList[RouletteWheel.SingleThrow(probabilties)];

			// Perform mutations of the required type.
			if(paramGroup.SelectionType==ConnectionSelectionType.Proportional)
			{
				bool mutationOccured=false;
				int connectionCount = connectionGeneList.Count;
				for(int i=0; i<connectionCount; i++)
				{
					if(Utilities.NextDouble() < paramGroup.Proportion)
					{
						MutateConnectionWeight(connectionGeneList[i], neatParameters, paramGroup);
						mutationOccured = true;
					}
				}
				if(!mutationOccured && connectionCount>0)
				{	// Perform at least one mutation. Pick a gene at random.
					MutateConnectionWeight(	connectionGeneList[(int)(Utilities.NextDouble() * connectionCount)],
											neatParameters,
											paramGroup);
				}
			}
			else // if(paramGroup.SelectionType==ConnectionSelectionType.FixedQuantity)
			{
				// Determine how many mutations to perform. At least one - if there are any genes.
				int connectionCount = connectionGeneList.Count;
				int mutations = Math.Min(connectionCount, Math.Max(1, paramGroup.Quantity));
				if(mutations==0) return;

				// The mutation loop. Here we pick an index at random and scan forward from that point
				// for the first non-mutated gene. This prevents any gene from being mutated more than once without
				// too much overhead. In fact it's optimal for small numbers of mutations where clashes are unlikely 
				// to occur.
				for(int i=0; i<mutations; i++)
				{
					// Pick an index at random.
					int index = (int)(Utilities.NextDouble()*connectionCount);
					ConnectionGene connectionGene = connectionGeneList[index];

					// Scan forward and find the first non-mutated gene.
					while(connectionGeneList[index].IsMutated)
					{	// Increment index. Wrap around back to the start if we go off the end.
						if(++index==connectionCount)
							index=0; 
					}
					
					// Mutate the gene at 'index'.
					MutateConnectionWeight(connectionGeneList[index], neatParameters, paramGroup);
					connectionGeneList[index].IsMutated = true;
				}
			}
		}

//		private void Mutate_ConnectionWeights(EvolutionAlgorithm ea)
//		{
//			float pColdGaussian, pMutation;
//			bool bMutateAllMutableConnections=false;
//			ConnectionGeneList tmpConnectionGeneList=null;
//
//			if(connectionGeneList.Count==0)
//				return;
//
//			// n% of the time perform more severe connection weight mutation (cold gaussian).
//			if(RouletteWheel.SingleThrow(0.5))
//			{
//				// TODO: Migrate mutation proportion values to the NeatParameters structure?
//				pMutation = 0.1F;
//				pColdGaussian = 1.0F;
//			}
//			else
//			{
//				pMutation = 0.8F;		// mutate 80% of weights.	
//				pColdGaussian = 0.0F;	// 0% of those are cold resets.
//			}
//
//
//			// Determine what type of mutation scheme to use.
//			if(ea.IsConnectionWeightFixingEnabled)
//			{
//				EnsureMutableConnectionGeneList();
//				if(mutableConnectionGeneList.Count==0)
//					return;
//
//				// Only mutate pMutation connections at most. If mutable connections make up a lesser proportion
//				// of total connections then just mutate all of the mutable connections.
//				float pMutableConnections = (float)mutableConnectionGeneList.Count / (float)connectionGeneList.Count;
//				if(pMutableConnections <= pMutation)
//					bMutateAllMutableConnections=true;
//				else
//					tmpConnectionGeneList = mutableConnectionGeneList;
//			}
//			else
//			{
//				tmpConnectionGeneList = connectionGeneList;
//			}
//
//			NeatParameters np = ea.NeatParameters;
//			if(bMutateAllMutableConnections)
//			{
//				// Mutate all connections in mutableConnectionGeneList.
//				int bound = mutableConnectionGeneList.Count;
//				for(int i=0; i<bound; i++)
//				{
//					ConnectionGene connectionGene = mutableConnectionGeneList[i];
//
//					if(Utilities.NextDouble() < pColdGaussian)
//					{	// Cold Normal dist.
//						connectionGene.Weight = (Utilities.NextDouble()* np.connectionWeightRange) - np.connectionWeightRange/2.0; 
//					}
//					else
//					{	// Normal distribution..
//						connectionGene.Weight = ValueMutation.Mutate(connectionGene.Weight, np.connectionMutationSigma);
//					}
//					// Cap the connection weight. Large connections weights reduce the effectiveness of the search.
//					connectionGene.Weight = Math.Max(connectionGene.Weight, -np.connectionWeightRange/2.0);
//					connectionGene.Weight = Math.Min(connectionGene.Weight, np.connectionWeightRange/2.0);
//				}
//			}
//			else
//			{
//				// Determine how many connections to mutate (minimum of 1)
//				int mutateCount = (int)Math.Ceiling(connectionGeneList.Count * pMutation);
//				for(int i=0; i<mutateCount; i++)
//				{
//					// Pick a connection at random.
//					ConnectionGene connectionGene = tmpConnectionGeneList[(int)(Utilities.NextDouble() * tmpConnectionGeneList.Count)];
//
//					if(Utilities.NextDouble() < pColdGaussian)
//					{	// Cold Normal dist.
//						connectionGene.Weight = (Utilities.NextDouble()*np.connectionWeightRange) - np.connectionWeightRange/2.0; 
//					}
//					else
//					{	// Normal distribution..
//						connectionGene.Weight = ValueMutation.Mutate(connectionGene.Weight, np.connectionMutationSigma);
//					}
//					// Cap the connection weight. Large connections weights reduce the effectiveness of the search.
//					connectionGene.Weight = Math.Max(connectionGene.Weight, - np.connectionWeightRange/2.0);
//					connectionGene.Weight = Math.Min(connectionGene.Weight, np.connectionWeightRange/2.0);
//				}
//			}
//		}

//		private void MutateWeight(ConnectionGene connectionGene, NeatParameters np)
//		{
//			if(Utilities.NextDouble() < 0.2)
//			{
//				connectionGene.Weight = (Utilities.NextDouble()*np.connectionWeightRange) - np.connectionWeightRange/2.0; 
//			}
//			else
//			{
//				connectionGene.Weight += (Utilities.NextDouble()*2-1.0) * 0.1;
//
//				// Cap the connection weight. Large connections weights reduce the effectiveness of the search.
//				connectionGene.Weight = Math.Max(connectionGene.Weight, - np.connectionWeightRange/2.0);
//				connectionGene.Weight = Math.Min(connectionGene.Weight, np.connectionWeightRange/2.0);
//			}
//		}

		/// <summary>
		/// Correlate the ConnectionGenes within the two ConnectionGeneLists - based upon innovation number.
		/// Return an ArrayList of ConnectionGene[2] structures - pairs of matching ConnectionGenes.
		/// </summary>
		/// <param name="list1"></param>
		/// <param name="list2"></param>
		/// <returns></returns>
		private CorrelationResults CorrelateConnectionGeneLists(ConnectionGeneList list1, ConnectionGeneList list2)
		{
			CorrelationResults correlationResults = new CorrelationResults();

		//----- Test for special cases.
			if(list1.Count==0 && list2.Count==0)
			{	// Both lists are empty!
				return correlationResults;
			}

			if(list1.Count==0)
			{	// All list2 genes are excess.
				correlationResults.CorrelationStatistics.ExcessConnectionGeneCount = list2.Count;
				foreach(ConnectionGene connectionGene in list2)
					correlationResults.CorrelationItemList.Add(new CorrelationItem(CorrelationItemType.ExcessConnectionGene, null, connectionGene));

				return correlationResults;
			}

			if(list2.Count==0)
			{	// All list1 genes are excess.
				correlationResults.CorrelationStatistics.ExcessConnectionGeneCount = list1.Count;
				foreach(ConnectionGene connectionGene in list1)
					correlationResults.CorrelationItemList.Add(new CorrelationItem(CorrelationItemType.ExcessConnectionGene, null, connectionGene));

				return correlationResults;
			}

		//----- Both ConnectionGeneLists contain genes - compare the contents.
			int list1Idx=0;
			int list2Idx=0;
			ConnectionGene connectionGene1 = list1[list1Idx];
			ConnectionGene connectionGene2 = list2[list2Idx];
			for(;;)
			{
				if(connectionGene2.InnovationId < connectionGene1.InnovationId)
				{	
					// connectionGene2 is disjoint.
					correlationResults.CorrelationItemList.Add(new CorrelationItem(CorrelationItemType.DisjointConnectionGene, null, connectionGene2));
					correlationResults.CorrelationStatistics.DisjointConnectionGeneCount++;

					// Move to the next gene in list2.
					list2Idx++;
				}
				else if(connectionGene1.InnovationId == connectionGene2.InnovationId)
				{
					correlationResults.CorrelationItemList.Add(new CorrelationItem(CorrelationItemType.MatchedConnectionGenes, connectionGene1, connectionGene2));
					correlationResults.CorrelationStatistics.ConnectionWeightDelta += Math.Abs(connectionGene1.Weight-connectionGene2.Weight);
					correlationResults.CorrelationStatistics.MatchingGeneCount++;

					// Move to the next gene in both lists.
					list1Idx++;
					list2Idx++;
				}
				else // (connectionGene2.InnovationId > connectionGene1.InnovationId)
				{	
					// connectionGene1 is disjoint.
					correlationResults.CorrelationItemList.Add(new CorrelationItem(CorrelationItemType.DisjointConnectionGene, connectionGene1, null));
					correlationResults.CorrelationStatistics.DisjointConnectionGeneCount++;

					// Move to the next gene in list1.
					list1Idx++;
				}
				
				// Check if we have reached the end of one (or both) of the lists. If we have reached the end of both then 
				// we execute the first if block - but it doesn't matter since the loop is not entered if both lists have 
				// been exhausted.
				if(list1Idx >= list1.Count)
				{	
					// All remaining list2 genes are excess.
					for(; list2Idx<list2.Count; list2Idx++)
					{
						correlationResults.CorrelationItemList.Add(new CorrelationItem(CorrelationItemType.ExcessConnectionGene, null, list2[list2Idx]));
						correlationResults.CorrelationStatistics.ExcessConnectionGeneCount++;
					}
					return correlationResults;
				}

				if(list2Idx >= list2.Count)
				{
					// All remaining list1 genes are excess.
					for(; list1Idx<list1.Count; list1Idx++)
					{
						correlationResults.CorrelationItemList.Add(new CorrelationItem(CorrelationItemType.ExcessConnectionGene, list1[list1Idx], null));
						correlationResults.CorrelationStatistics.ExcessConnectionGeneCount++;
					}
					return correlationResults;
				}

				connectionGene1 = list1[list1Idx];
				connectionGene2 = list2[list2Idx];
			}
		}


		/// <summary>
		/// If the neuron is a hidden neuron and no connections connect to it then it is redundant.
        /// No neuron is redundant that is part of a module (although the module itself might be found redundant separately).
		/// </summary>
        private bool IsNeuronRedundant(long neuronId)
		{
			NeuronGene neuronGene = neuronGeneList.GetNeuronById(neuronId);
            if (neuronGene.NeuronType != NeuronType.Hidden
                        || neuronGene.ActivationFunction is ModuleInputNeuron
                        || neuronGene.ActivationFunction is ModuleOutputNeuron)
                return false;

			return !IsNeuronConnected(neuronId);
		}


        private bool IsNeuronConnected(long neuronId)
		{
			int bound = connectionGeneList.Count;
			for(int i=0; i<bound; i++)
			{
				ConnectionGene connectionGene = connectionGeneList[i];
				if(connectionGene.SourceNeuronId==neuronId)
					return true;
					
				if(connectionGene.TargetNeuronId==neuronId)
					return true;
			}

			return false;
		}
		
		private void EnsureMutableConnectionGeneList()
		{
			if(mutableConnectionGeneList!=null)
				return;

			mutableConnectionGeneList = new ConnectionGeneList();

			int bound = connectionGeneList.Count;
			for(int i=0; i<bound; i++)
			{
				ConnectionGene connectionGene = connectionGeneList[i];
				if(!connectionGene.FixedWeight)
					mutableConnectionGeneList.Add(connectionGene);
			}
		}

		#endregion

		#region Private Methods [Pruning Support]

		private void EnsureNeuronTable()
		{
			if(neuronGeneTable==null)
				BuildNeuronTable();
		}

		private void BuildNeuronTable()
		{
			neuronGeneTable = new Hashtable();

			foreach(NeuronGene neuronGene in neuronGeneList)
				neuronGeneTable.Add(neuronGene.InnovationId, neuronGene);
		}

		private void EnsureNeuronConnectionLookupTable()
		{
			if(neuronConnectionLookupTable==null)
				BuildNeuronConnectionLookupTable();
		}

		private void BuildNeuronConnectionLookupTable()
		{
			EnsureNeuronTable();

			neuronConnectionLookupTable = new Hashtable();
			foreach(ConnectionGene connectionGene in connectionGeneList)
			{
				BuildNeuronConnectionLookupTable_NewIncomingConnection(connectionGene.TargetNeuronId, connectionGene);
				BuildNeuronConnectionLookupTable_NewOutgoingConnection(connectionGene.SourceNeuronId, connectionGene);
			}
		}

        private void BuildNeuronConnectionLookupTable_NewIncomingConnection(long neuronId, ConnectionGene connectionGene)
		{
			// Is this neuron already known to the lookup table?
			NeuronConnectionLookup lookup = (NeuronConnectionLookup)neuronConnectionLookupTable[neuronId];
			if(lookup==null)
			{	// Creae a new lookup entry for this neuron Id.
				lookup = new NeuronConnectionLookup();
				lookup.neuronGene = (NeuronGene)neuronGeneTable[neuronId];
				neuronConnectionLookupTable.Add(neuronId, lookup);
			}
	
			// Register the connection with the NeuronConnectionLookup object.
			lookup.incomingList.Add(connectionGene);
		}

        private void BuildNeuronConnectionLookupTable_NewOutgoingConnection(long neuronId, ConnectionGene connectionGene)
		{
			// Is this neuron already known to the lookup table?
			NeuronConnectionLookup lookup = (NeuronConnectionLookup)neuronConnectionLookupTable[neuronId];
			if(lookup==null)
			{	// Creae a new lookup entry for this neuron Id.
				lookup = new NeuronConnectionLookup();
				lookup.neuronGene = (NeuronGene)neuronGeneTable[neuronId];
				neuronConnectionLookupTable.Add(neuronId, lookup);
			}
	
			// Register the connection with the NeuronConnectionLookup object.
			lookup.outgoingList.Add(connectionGene);
		}

        private bool TestForExistingConnection(long sourceId, long targetId)
		{
            //if we find it locally, it exists already
            return WINManager.SharedWIN.findWINConnection(WINConnection.ConnectionWithProperties(sourceId, targetId)) != null;

            //for(int connectionIdx=0; connectionIdx<connectionGeneList.Count; connectionIdx++)
            //{
            //    ConnectionGene connectionGene = connectionGeneList[connectionIdx];
            //    if(connectionGene.SourceNeuronId == sourceId && connectionGene.TargetNeuronId == targetId)
            //        return true;
            //}
            //return false;
		}

		#endregion

        //public void mutate(NeatParameters np, IdGenerator idgen)
        //{
        //    Population pop = new Population(idgen, new GenomeList());
        //    EvolutionAlgorithm ea = new EvolutionAlgorithm(pop, null, np, false);

        //    this.Mutate(ea);
        //    //perform a node addition afterwards too, mwahaha!
        //    this.Mutate_AddNode(ea);
        //}
    }
}
