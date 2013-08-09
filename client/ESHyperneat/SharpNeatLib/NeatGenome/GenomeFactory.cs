using System;
using System.Collections;

using SharpNeatLib;
using SharpNeatLib.Evolution;
using SharpNeatLib.NeuralNetwork;
using SharpNeatLib.Maths;
using SharpNeatLib.Masters;

namespace SharpNeatLib.NeatGenome
{
        public delegate bool AssessGenotypeFunction(IGenome genome);
	public class GenomeFactory
	{



		/// <summary>
		/// Create a default minimal genome that describes a NN with the given number of inputs and outputs.
		/// </summary>
		/// <returns></returns>
		public static IGenome CreateGenome(NeatParameters neatParameters, IdGenerator idGenerator, int inputNeuronCount, int outputNeuronCount, float connectionProportion)
		{
            IActivationFunction actFunct;
			NeuronGene neuronGene; // temp variable.
			NeuronGeneList inputNeuronGeneList = new NeuronGeneList(); // includes bias neuron.
			NeuronGeneList outputNeuronGeneList = new NeuronGeneList();
			NeuronGeneList neuronGeneList = new NeuronGeneList();
			ConnectionGeneList connectionGeneList = new ConnectionGeneList();

			// IMPORTANT NOTE: The neurons must all be created prior to any connections. That way all of the genomes
			// will obtain the same innovation ID's for the bias,input and output nodes in the initial population.
			// Create a single bias neuron.
            //TODO: DAVID proper activation function change to NULL?
            actFunct = ActivationFunctionFactory.GetActivationFunction("NullFn");
            //neuronGene = new NeuronGene(idGenerator.NextInnovationId, NeuronType.Bias, actFunct);
            neuronGene = new NeuronGene(null, idGenerator.NextInnovationId, NeuronGene.INPUT_LAYER, NeuronType.Bias, actFunct);
			inputNeuronGeneList.Add(neuronGene);
			neuronGeneList.Add(neuronGene);

			// Create input neuron genes.
            actFunct = ActivationFunctionFactory.GetActivationFunction("NullFn");
			for(int i=0; i<inputNeuronCount; i++)
			{
                //TODO: DAVID proper activation function change to NULL?
                //neuronGene = new NeuronGene(idGenerator.NextInnovationId, NeuronType.Input, actFunct);
                neuronGene = new NeuronGene(null, idGenerator.NextInnovationId, NeuronGene.INPUT_LAYER, NeuronType.Input, actFunct);
				inputNeuronGeneList.Add(neuronGene);
				neuronGeneList.Add(neuronGene);
			}

			// Create output neuron genes. 
            //actFunct = ActivationFunctionFactory.GetActivationFunction("NullFn");
			for(int i=0; i<outputNeuronCount; i++)
			{
                actFunct = ActivationFunctionFactory.GetActivationFunction("BipolarSigmoid");
                //actFunct = ActivationFunctionFactory.GetRandomActivationFunction(neatParameters);
                //TODO: DAVID proper activation function
                //neuronGene = new NeuronGene(idGenerator.NextInnovationId, NeuronType.Output, actFunct);
                neuronGene = new NeuronGene(null, idGenerator.NextInnovationId, NeuronGene.OUTPUT_LAYER, NeuronType.Output, actFunct);
				outputNeuronGeneList.Add(neuronGene);
				neuronGeneList.Add(neuronGene);
			}

			// Loop over all possible connections from input to output nodes and create a number of connections based upon
			// connectionProportion.
			foreach(NeuronGene targetNeuronGene in outputNeuronGeneList)
			{
				foreach(NeuronGene sourceNeuronGene in inputNeuronGeneList)
				{
					// Always generate an ID even if we aren't going to use it. This is necessary to ensure connections
					// between the same neurons always have the same ID throughout the generated population.
                    long connectionInnovationId = idGenerator.NextInnovationId;

					if(Utilities.NextDouble() < connectionProportion)
					{	// Ok lets create a connection.
						connectionGeneList.Add(	new ConnectionGene(connectionInnovationId, 
							sourceNeuronGene.InnovationId,
							targetNeuronGene.InnovationId,
							(Utilities.NextDouble() * neatParameters.connectionWeightRange ) - neatParameters.connectionWeightRange/2.0));  // Weight 0 +-5
					}
				}
			}

			// Don't create any hidden nodes at this point. Fundamental to the NEAT way is to start minimally!
			return new NeatGenome(idGenerator.NextGenomeId, neuronGeneList, connectionGeneList, inputNeuronCount, outputNeuronCount);
		}

		/// <summary>
		/// Construct a GenomeList. This can be used to construct a new Population object.
		/// </summary>
		/// <param name="evolutionAlgorithm"></param>
		/// <param name="inputNeuronCount"></param>
		/// <param name="outputNeuronCount"></param>
		/// <param name="length"></param>
		/// <returns></returns>
		public static GenomeList CreateGenomeList(NeatParameters neatParameters, IdGenerator idGenerator, int inputNeuronCount, int outputNeuronCount, float connectionProportion, int length)
		{
			GenomeList genomeList = new GenomeList();
			
			for(int i=0; i<length; i++)
			{
				idGenerator.ResetNextInnovationNumber();
				genomeList.Add(CreateGenome(neatParameters, idGenerator, inputNeuronCount, outputNeuronCount, connectionProportion));
			}

			return genomeList;
		}

        public static GenomeList CreateGenomeListPreserveIDs(NeatParameters neatParameters, IdGenerator idGenerator, 
            int inputNeuronCount, int outputNeuronCount, float connectionProportion, int length, AssessGenotypeFunction assess)
        {
            GenomeList genomeList = new GenomeList();

            int testCount = 0; int maxTests = 5;

            //for (int i = 0; i < length; i++)
            while(genomeList.Count < length)
            {                    
                    IGenome genome = CreateGenomePreserveID(neatParameters, idGenerator, inputNeuronCount, outputNeuronCount, connectionProportion);

                    if (assess != null && assess(genome) && testCount++ < maxTests)
                    {
                        //after adding the genome, reset test count
                        genomeList.Add(genome);
                        testCount = 0;
                    }
                    else if (assess == null)
                        genomeList.Add(genome);
                    else if (testCount >= maxTests)
                    {
                        genomeList.Add(genome);
                        testCount = 0;
                    }
            }

            return genomeList;
        }
        public static GenomeList CreateGenomeListPreserveIDs(GenomeList seedGenomes, int length, NeatParameters neatParameters, IdGenerator idGenerator, AssessGenotypeFunction assess)
        {
            //Eventually, WIN will be brought in to maintain the genomes, for now, no need

            //Build the list.
            GenomeList genomeList = new GenomeList();

            if (length < seedGenomes.Count)
                throw new Exception("Attempting to generate a population that is smaller than the number of seeds (i.e. some seeds will be lost). Please change pop size to accomodate for all seeds.");
            NeatGenome newGenome;
            
            for (int i = 0; i < seedGenomes.Count; i++)
            {
                // Use each seed directly just once.
                newGenome = new NeatGenome((NeatGenome)seedGenomes[i], idGenerator.NextGenomeId);
                genomeList.Add(newGenome);
            }

            int testCount = 0; int maxTests = 5;
            
            // For the remainder we alter the weights.
            //for (int i = 1; i < length; i++)
            //{
            while (genomeList.Count < length)
            {
                newGenome = new NeatGenome((NeatGenome)seedGenomes[Utilities.Next(seedGenomes.Count)], idGenerator.NextGenomeId);

                // Reset the connection weights

                //in this particular instance, we would take a snapshot of the genome AFTER mutation for WIN purposes. But we don't track genomes yet
                foreach (ConnectionGene connectionGene in newGenome.ConnectionGeneList)
                    connectionGene.Weight += (0.1 - Utilities.NextDouble() * 0.2);

                //!connectionGene.Weight = (Utilities.NextDouble() * neatParameters.connectionWeightRange) - neatParameters.connectionWeightRange/2.0;
                //Console.WriteLine((0.1 - Utilities.NextDouble() * 0.2));
                //newGenome.ConnectionGeneList.Add(new ConnectionGene(idGenerator.NextInnovationId,5,newGenome.NeuronGeneList[Utilities.Next(newGenome.NeuronGeneList.Count-7)+7].InnovationId ,(Utilities.NextDouble() * neatParameters.connectionWeightRange) - neatParameters.connectionWeightRange/2.0));
                //newGenome.ConnectionGeneList.Add(new ConnectionGene(idGenerator.NextInnovationId, 6, newGenome.NeuronGeneList[Utilities.Next(newGenome.NeuronGeneList.Count - 7) + 7].InnovationId, (Utilities.NextDouble() * neatParameters.connectionWeightRange) - neatParameters.connectionWeightRange / 2.0));
                //if we have an assess function, it should be used for generating this individual!


                if (assess != null && assess(newGenome) && testCount++ < maxTests)
                {
                    //after adding the genome, reset test count
                    genomeList.Add(newGenome);
                    testCount = 0;
                }
                else if (assess == null)
                    genomeList.Add(newGenome);
                else if (testCount >= maxTests)
                {
                    genomeList.Add(newGenome);
                    testCount = 0;
                }
            }

            //

            return genomeList;
        }

        public static IGenome CreateGenomePreserveID(IGenome seedGenome, IdGenerator idGenerator)
        {
            NeatGenome newGenome = new NeatGenome((NeatGenome)seedGenome, idGenerator.NextGenomeId);

            // Reset the connection weights

            //in this particular instance, we would take a snapshot of the genome AFTER mutation for WIN purposes. But we don't track genomes yet
            foreach (ConnectionGene connectionGene in newGenome.ConnectionGeneList)
                connectionGene.Weight += (0.1 - Utilities.NextDouble() * 0.2);

            return newGenome;
        }

        public static IGenome CreateGenomePreserveID(NeatParameters neatParameters, IdGenerator idGenerator, int inputNeuronCount, int outputNeuronCount, float connectionProportion)
        {
            IActivationFunction actFunct;
            NeuronGene neuronGene; // temp variable.
            NeuronGeneList inputNeuronGeneList = new NeuronGeneList(); // includes bias neuron.
            NeuronGeneList outputNeuronGeneList = new NeuronGeneList();
            NeuronGeneList neuronGeneList = new NeuronGeneList();
            ConnectionGeneList connectionGeneList = new ConnectionGeneList();

            int nodeCount = 0;

            WINManager win = WINManager.SharedWIN;
            
            // IMPORTANT NOTE: The neurons must all be created prior to any connections. That way all of the genomes
            // will obtain the same innovation ID's for the bias,input and output nodes in the initial population.
            // Create a single bias neuron.
            //TODO: DAVID proper activation function change to NULL?
            actFunct = ActivationFunctionFactory.GetActivationFunction("NullFn");
            //neuronGene = new NeuronGene(idGenerator.NextInnovationId, NeuronType.Bias, actFunct);
            WINNode neuronNode = win.findOrInsertNodeWithProperties(idGenerator,
                WINNode.NodeWithProperties(nodeCount++, NeuronType.Bias)
                );

            neuronGene = new NeuronGene(null, neuronNode.UniqueID, NeuronGene.INPUT_LAYER, NeuronType.Bias, actFunct);
            inputNeuronGeneList.Add(neuronGene);
            neuronGeneList.Add(neuronGene);

            // Create input neuron genes.
            actFunct = ActivationFunctionFactory.GetActivationFunction("NullFn");
            for (int i = 0; i < inputNeuronCount; i++)
            {
                //TODO: DAVID proper activation function change to NULL?
                //neuronGene = new NeuronGene(idGenerator.NextInnovationId, NeuronType.Input, actFunct);
                neuronNode = win.findOrInsertNodeWithProperties(idGenerator, WINNode.NodeWithProperties(nodeCount++, NeuronType.Input));

                neuronGene = new NeuronGene(null, neuronNode.UniqueID, NeuronGene.INPUT_LAYER, NeuronType.Input, actFunct);
                inputNeuronGeneList.Add(neuronGene);
                neuronGeneList.Add(neuronGene);
            }

            // Create output neuron genes. 
            //actFunct = ActivationFunctionFactory.GetActivationFunction("NullFn");
            for (int i = 0; i < outputNeuronCount; i++)
            {
                actFunct = ActivationFunctionFactory.GetActivationFunction("BipolarSigmoid");
                //actFunct = ActivationFunctionFactory.GetRandomActivationFunction(neatParameters);
                //TODO: DAVID proper activation function
                //neuronGene = new NeuronGene(idGenerator.NextInnovationId, NeuronType.Output, actFunct);
                neuronNode = win.findOrInsertNodeWithProperties(idGenerator, WINNode.NodeWithProperties(nodeCount++, NeuronType.Output));

                neuronGene = new NeuronGene(null, neuronNode.UniqueID, NeuronGene.OUTPUT_LAYER, NeuronType.Output, actFunct);
                outputNeuronGeneList.Add(neuronGene);
                neuronGeneList.Add(neuronGene);
            }


            int currentConnCount = 0;
            WINConnection winConn;
            // Loop over all possible connections from input to output nodes and create a number of connections based upon
            // connectionProportion.
            foreach (NeuronGene targetNeuronGene in outputNeuronGeneList)
            {
                foreach (NeuronGene sourceNeuronGene in inputNeuronGeneList)
                {
                    // Always generate an ID even if we aren't going to use it. This is necessary to ensure connections
                    // between the same neurons always have the same ID throughout the generated population.
                    //PAUL NOTE:
                    //instead of generating and not using and id, we use the target and connection properties to uniquely identify a connection in WIN
                    //uint connectionInnovationId = idGenerator.NextInnovationId;

                    if (Utilities.NextDouble() < connectionProportion)
                    {	
                        // Ok lets create a connection.
                        //first we search or create the winconnection object
                        winConn = win.findOrInsertConnectionWithProperties(idGenerator,
                            WINConnection.ConnectionWithProperties(currentConnCount, sourceNeuronGene.InnovationId, targetNeuronGene.InnovationId));

                        //our winconn will have our innovationID, and our weight like normal
                        //this will also respect the idgenerator, since it gets sent in as well, for legacy purposes
                        connectionGeneList.Add(new ConnectionGene(winConn.UniqueID,
                            sourceNeuronGene.InnovationId,
                            targetNeuronGene.InnovationId,
                            (Utilities.NextDouble() * neatParameters.connectionWeightRange) - neatParameters.connectionWeightRange / 2.0)
                            );
                            //(Utilities.NextDouble() * neatParameters.connectionWeightRange) - neatParameters.connectionWeightRange / 2.0));  // Weight 0 +-5
                    }

                    currentConnCount++;

                }
            }
            //WIN will eventually be in control of all the genomes that are created as well, but not quite yet!
            //TODO: WIN should be generating genomeIDs explicitly

            // Don't create any hidden nodes at this point. Fundamental to the NEAT way is to start minimally!
            return new NeatGenome(idGenerator.NextGenomeId, neuronGeneList, connectionGeneList, inputNeuronCount, outputNeuronCount);
        }

		public static GenomeList CreateGenomeList(NeatGenome seedGenome, int length, NeatParameters neatParameters, IdGenerator idGenerator)
		{
			//Build the list.
			GenomeList genomeList = new GenomeList();
			
			// Use the seed directly just once.
			NeatGenome newGenome = new NeatGenome(seedGenome, idGenerator.NextGenomeId);
			genomeList.Add(newGenome);

			// For the remainder we alter the weights.
			for(int i=1; i<length; i++)
			{
				newGenome = new NeatGenome(seedGenome, idGenerator.NextGenomeId);
				
				// Reset the connection weights
				foreach(ConnectionGene connectionGene in newGenome.ConnectionGeneList)
                    connectionGene.Weight += (0.1 - Utilities.NextDouble() * 0.2);
                    //!connectionGene.Weight = (Utilities.NextDouble() * neatParameters.connectionWeightRange) - neatParameters.connectionWeightRange/2.0;
                //Console.WriteLine((0.1 - Utilities.NextDouble() * 0.2));
                //newGenome.ConnectionGeneList.Add(new ConnectionGene(idGenerator.NextInnovationId,5,newGenome.NeuronGeneList[Utilities.Next(newGenome.NeuronGeneList.Count-7)+7].InnovationId ,(Utilities.NextDouble() * neatParameters.connectionWeightRange) - neatParameters.connectionWeightRange/2.0));
                //newGenome.ConnectionGeneList.Add(new ConnectionGene(idGenerator.NextInnovationId, 6, newGenome.NeuronGeneList[Utilities.Next(newGenome.NeuronGeneList.Count - 7) + 7].InnovationId, (Utilities.NextDouble() * neatParameters.connectionWeightRange) - neatParameters.connectionWeightRange / 2.0));
				genomeList.Add(newGenome);
			}

            //

			return genomeList;
		}

        public static GenomeList CreateGenomeListAddedInputs(NeatGenome seedGenome, int length, NeatParameters neatParameters, IdGenerator idGenerator)
        {
            //Build the list.
            GenomeList genomeList = new GenomeList();

            // Use the seed directly just once.
            NeatGenome newGenome = new NeatGenome(seedGenome, idGenerator.NextGenomeId);
            //genomeList.Add(newGenome);

            // For the remainder we alter the weights.
            for (int i = 0; i < length; i++)
            {
                newGenome = new NeatGenome(seedGenome, idGenerator.NextGenomeId);

                // Reset the connection weights
                foreach (ConnectionGene connectionGene in newGenome.ConnectionGeneList)
                    connectionGene.Weight = (Utilities.NextDouble() * neatParameters.connectionWeightRange) - neatParameters.connectionWeightRange / 2.0;
                newGenome.ConnectionGeneList.Add(new ConnectionGene(idGenerator.NextInnovationId, 5, newGenome.NeuronGeneList[Utilities.Next(newGenome.NeuronGeneList.Count - 7) + 7].InnovationId, (Utilities.NextDouble() * neatParameters.connectionWeightRange) - neatParameters.connectionWeightRange / 2.0));
                newGenome.ConnectionGeneList.Add(new ConnectionGene(idGenerator.NextInnovationId, 6, newGenome.NeuronGeneList[Utilities.Next(newGenome.NeuronGeneList.Count - 7) + 7].InnovationId, (Utilities.NextDouble() * neatParameters.connectionWeightRange) - neatParameters.connectionWeightRange / 2.0));
                genomeList.Add(newGenome);
            }

            //

            return genomeList;
        }


		public static GenomeList CreateGenomeList(Population seedPopulation, int length, NeatParameters neatParameters, IdGenerator idGenerator)
		{
			//Build the list.
			GenomeList genomeList = new GenomeList();
			int seedIdx=0;
			
			for(int i=0; i<length; i++)
			{
				NeatGenome newGenome = new NeatGenome((NeatGenome)seedPopulation.GenomeList[seedIdx], idGenerator.NextGenomeId);

				// Reset the connection weights
				foreach(ConnectionGene connectionGene in newGenome.ConnectionGeneList)
					connectionGene.Weight = (Utilities.NextDouble() * neatParameters.connectionWeightRange) - neatParameters.connectionWeightRange/2.0;

				genomeList.Add(newGenome);

				if(++seedIdx >= seedPopulation.GenomeList.Count)
				{	// Back to first genome.
					seedIdx=0;
				}
			}
			return genomeList;
		}
	}
}
