using System;
using SharpNeatLib.Experiments;
using System.Collections.Generic;

namespace SharpNeatLib.NeuralNetwork
{
    
    public class ModularNetwork : INetwork
    {

        public delegate void UpdateNetworkDelegate(ModularNetwork network);
        public event UpdateNetworkDelegate UpdateNetworkEvent;

        //Store connection list for visualization purposes
        public NeatGenome.NeatGenome genome;

        #region Class Variables

        float A, B, C, D, learningRate, pre, post;

        public bool adaptable, modulatory;

        // For the following array, neurons are ordered with bias nodes at the head of the list,
        // then input nodes, then output nodes, and then hidden nodes in the array's tail.
        public float[] neuronSignals;

        public float[] modSignals;

        // This array is a parallel of neuronSignals, and only has values during SingleStepInternal().
        // It is declared here to avoid having to reallocate it for every network activation.
        public float[] neuronSignalsBeingProcessed;

        // must be in the same order as neuronSignals. Has null entries for neurons that are inputs or outputs of a module.
        public IActivationFunction[] activationFunctions;

        // The modules and connections are in no particular order; only the order of the neuronSignals is used for input and output methods.
        private ModulePacket[] modules;
        public FloatFastConnection[] connections;

        /// <summary>
        /// The number of input neurons.
        /// </summary>
        private int inputNeuronCount;

        /// <summary>
        /// The number of input neurons including any bias neurons. This is also the index of the first output neuron in the neuron signals.
        /// </summary>
        private int totalInputNeuronCount;

        /// <summary>
        /// The number of output neurons.
        /// </summary>
        private int outputNeuronCount;

        /// <summary>
        /// The number of bias neurons, usually one but sometimes zero. This is also the index of the first input neuron in the neuron signals.
        /// </summary>
        private int biasNeuronCount;

        private float[] biasList;

        // For recursive activation, marks whether we have finished this node yet
        public bool[] activated;

        // For recursive activation, makes whether a node is currently being calculated. For recurrant connections
        public bool[] inActivation;

        // For recursive activation, the previous activation for recurrent connections
        public float[] lastActivation;

        public List<int>[] adjacentList;
        public List<int>[] reverseAdjacentList;
        public float[,] adjacentMatrix;
        #endregion


        #region Constructor

        public ModularNetwork(int biasNeuronCount,
                                int inputNeuronCount,
                                int outputNeuronCount,
                                int totalNeuronCount,
                                FloatFastConnection[] connections,
                                float[] biasList,
                                IActivationFunction[] activationFunctions,
                                ModulePacket[] modules)
        {
            this.biasNeuronCount = biasNeuronCount;
            this.inputNeuronCount = inputNeuronCount;
            this.totalInputNeuronCount = biasNeuronCount + inputNeuronCount;
            this.outputNeuronCount = outputNeuronCount;
            this.connections = connections;
            this.activationFunctions = activationFunctions;
            this.modules = modules;
            this.biasList = biasList;

            adaptable = false;
            modulatory = false;

            // Allocate the arrays that store the states at different points in the neural network.
            // The neuron signals are initialised to 0 by default. Only bias nodes need setting to 1.
            neuronSignals = new float[totalNeuronCount];
            modSignals = new float[totalNeuronCount];

            neuronSignalsBeingProcessed = new float[totalNeuronCount];
            for (int i = 0; i < biasNeuronCount; i++) {
                neuronSignals[i] = 1.0F;
            }
            this.activated = new bool[TotalNeuronCount];
            this.inActivation = new bool[TotalNeuronCount];
            this.lastActivation = new float[TotalNeuronCount];

            adjacentList = new List<int>[TotalNeuronCount];
            reverseAdjacentList = new List<int>[TotalNeuronCount];
            adjacentMatrix = new float[TotalNeuronCount, TotalNeuronCount];

            for (int i = 0; i < TotalNeuronCount; i++)
            {
                this.activated[i] = this.inActivation[i] = false;
                this.lastActivation[i] = 0;
                this.adjacentList[i] = new List<int>(TotalNeuronCount);
                this.reverseAdjacentList[i] = new List<int>(TotalNeuronCount);
                // this.adjacentMatrix[i] = new List<float>(TotalNeuronCount);


            }

            // Set up adjacency list and matrix
            for (int i = 0; i < this.connections.Length; i++)
            {
                int crs = connections[i].sourceNeuronIdx;
                int crt = connections[i].targetNeuronIdx;

                // Holds outgoing nodes
                this.adjacentList[crs].Add(crt);

                // Holds incoming nodes
                this.reverseAdjacentList[crt].Add(crs);

                this.adjacentMatrix[crs, crt] = connections[i].weight;
            }
        }

        #endregion


        #region INetwork Members

        /// <summary>
        /// This function carries out a single network activation.
        /// It is called by all those methods that require network activations.
        /// </summary>
        /// <param name="maxAllowedSignalDelta">
        /// The network is not relaxed as long as the absolute value of the change in signals at any given point is greater than this value.
        /// Only positive values are used. If the value is less than or equal to 0, the method will return true without checking for relaxation.
        /// </param>
        /// <returns>True if the network is relaxed, or false if not.</returns>
        private bool SingleStepInternal(double maxAllowedSignalDelta)
        {
            bool isRelaxed = true;	// Assume true.

            // Calculate each connection's output signal, and add the signals to the target neurons.
            for (int i = 0; i < connections.Length; i++) {
                //if (connectionArray[i].modConnection == 0.0f)       //normal connection
                //    connectionArray[i].signal = neuronSignalArray[connectionArray[i].sourceNeuronIdx] * connectionArray[i].weight;
                //else
                //    connectionArray[i].modSignal = neuronSignalArray[connectionArray[i].sourceNeuronIdx] * connectionArray[i].weight;


                if (adaptable)
                {
                    if (connections[i].modConnection <= 0.0f)   //Normal connection
                    {
                        neuronSignalsBeingProcessed[connections[i].targetNeuronIdx] += neuronSignals[connections[i].sourceNeuronIdx] * connections[i].weight;
                    }
                    else //modulatory connection
                    {
                        modSignals[connections[i].targetNeuronIdx] += neuronSignals[connections[i].sourceNeuronIdx] * connections[i].weight;

                    }
                }
                else
                {
                    neuronSignalsBeingProcessed[connections[i].targetNeuronIdx] += neuronSignals[connections[i].sourceNeuronIdx] * connections[i].weight;
                  
                }
             }

            // Pass the signals through the single-valued activation functions. 
            // Do not change the values of input neurons or neurons that have no activation function because they are part of a module.
            for (int i = totalInputNeuronCount; i < neuronSignalsBeingProcessed.Length; i++) {
                neuronSignalsBeingProcessed[i] = activationFunctions[i].Calculate(neuronSignalsBeingProcessed[i]+biasList[i]);
                if (modulatory)
                {
                    //Make sure it's between 0 and 1
                    modSignals[i] += 1.0f;
                    if (modSignals[i]!=0.0f)
                        modSignals[i] = (float)Math.Tanh(modSignals[i]);//(Math.Exp(2 * modSignals[i]) - 1) / (Math.Exp(2 * modSignals[i]) + 1));
                }
            }
            //TODO Sebastian CHECK IF BIAS NEURON IS WORKING CORRECTLY

            // Pass the signals through each module (activation function with more than one input or output).
            foreach (ModulePacket module in modules) {
                float[] inputs = new float[module.inputLocations.Length];
                for (int i = inputs.Length - 1; i >= 0; i--) {
                    inputs[i] = neuronSignalsBeingProcessed[module.inputLocations[i]];
                }

                float[] outputs = module.function.Calculate(inputs);
                for (int i = outputs.Length - 1; i >= 0; i--) {
                    neuronSignalsBeingProcessed[module.outputLocations[i]] = outputs[i];
                }
            }

            /*foreach (float f in neuronSignals)
                HyperNEATParameters.distOutput.Write(f.ToString("R") + " ");
            HyperNEATParameters.distOutput.WriteLine();
            HyperNEATParameters.distOutput.Flush();*/

            // Move all the neuron signals we changed while processing this network activation into storage.
            if (maxAllowedSignalDelta > 0) {
                for (int i = totalInputNeuronCount; i < neuronSignalsBeingProcessed.Length; i++) {

                    // First check whether any location in the network has changed by more than a small amount.
                    isRelaxed &= (Math.Abs(neuronSignals[i] - neuronSignalsBeingProcessed[i]) > maxAllowedSignalDelta);

                    neuronSignals[i] = neuronSignalsBeingProcessed[i];
                    neuronSignalsBeingProcessed[i] = 0.0F;
                }
            } else {
                for (int i = totalInputNeuronCount; i < neuronSignalsBeingProcessed.Length; i++) {
                    neuronSignals[i] = neuronSignalsBeingProcessed[i];
                    neuronSignalsBeingProcessed[i] = 0.0F;
                }
            }

           // Console.WriteLine(inputNeuronCount);

            if (adaptable)//CPPN != null)
            {
                float[] coordinates = new float[4];
                float modValue;
                float weightDelta;
                for (int i = 0; i < connections.Length; i++)
                {
                    if (modulatory)
                    {
                        pre = neuronSignals[connections[i].sourceNeuronIdx];
                        post = neuronSignals[connections[i].targetNeuronIdx];
                        modValue = modSignals[connections[i].targetNeuronIdx];
                       
                        A = connections[i].A;
                        B = connections[i].B;
                        C = connections[i].C;
                        D = connections[i].D;

                        learningRate = connections[i].learningRate;
                        if (modValue != 0.0f && (connections[i].modConnection <= 0.0f))        //modulate target neuron if its a normal connection
                        {
                            connections[i].weight += modValue*learningRate * (A * pre * post + B * pre + C * post + D);
                        }

                        if (Math.Abs(connections[i].weight) > 5.0f)
                        {
                            connections[i].weight = 5.0f * Math.Sign(connections[i].weight);
                        }
                    }
                    else
                    {
                        pre = neuronSignals[connections[i].sourceNeuronIdx];
                        post = neuronSignals[connections[i].targetNeuronIdx];
                        A = connections[i].A;
                        B = connections[i].B;
                        C = connections[i].C;
                       
                        learningRate = connections[i].learningRate;

                        weightDelta = learningRate * (A * pre * post + B * pre + C * post);
                        connections[i].weight += weightDelta;

                     //   Console.WriteLine(pre + " " + post + " " + learningRate + " " + A + " " + B + " " + C + " " + weightDelta);

                        if (Math.Abs(connections[i].weight) > 5.0f)
                        {
                            connections[i].weight = 5.0f * Math.Sign(connections[i].weight);
                        }
                    }

                    if (false)
                    {

                    }
                }
            }
            
            for (int i = totalInputNeuronCount; i < neuronSignalsBeingProcessed.Length; i++) 
            {
                modSignals[i] = 0.0F;
            }
 
            return isRelaxed;
        }

        //Apply reward globally and change synaptic weights
   
        public void SingleStep()
        {
            SingleStepInternal(0.0); // we will ignore the value of this function, so the "allowedDelta" argument doesn't matter.
            if (UpdateNetworkEvent != null)
            {
                UpdateNetworkEvent(null);
            }
        }


        public void MultipleSteps(int numberOfSteps)
        {
            for (int i = 0; i < numberOfSteps; i++) {
                SingleStep();
            }
        }


        /// <summary>
        /// Using RelaxNetwork erodes some of the perofrmance gain of FastConcurrentNetwork because of the slightly 
        /// more complex implemementation of the third loop - whe compared to SingleStep().
        /// </summary>
        /// <param name="maxSteps"></param>
        /// <param name="maxAllowedSignalDelta"></param>
        /// <returns></returns>
        public bool RelaxNetwork(int maxSteps, double maxAllowedSignalDelta)
        {
            bool isRelaxed = false;
            for (int j = 0; j < maxSteps && !isRelaxed; j++) {
                isRelaxed = SingleStepInternal(maxAllowedSignalDelta);
            }
            return isRelaxed;
        }

        public void SetInputSignal(int index, float signalValue)
        {
            // For speed we don't bother with bounds checks.
            neuronSignals[biasNeuronCount + index] = signalValue;
        }


        public void SetInputSignals(float[] signalArray)
        {
            // For speed we don't bother with bounds checks.
            for (int i = 0; i < signalArray.Length; i++)
                neuronSignals[i + biasNeuronCount] = signalArray[i];
        }

        
        public float GetOutputSignal(int index)
        {
            // For speed we don't bother with bounds checks.
            return neuronSignals[totalInputNeuronCount + index];
        }

        
        public void ClearSignals()
        {
            // Clear signals for input, hidden and output nodes. Only the bias node is untouched.
            for (int i = biasNeuronCount; i < neuronSignals.Length; i++)
                neuronSignals[i] = 0.0F;
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

        
        public int TotalNeuronCount
        {
            get
            {
                return neuronSignals.Length;
            }
        }

        #endregion

        #region Recursive Activation
        public void RecursiveActivation()
        {
            // Initialize boolean arrays and set the last activation signal, but only if it isn't an input (these have already been set when the input is activated)
            for (int i = 0; i < this.TotalNeuronCount; i++)
            {
                // Set as activated if i is an input node, otherwise ensure it is unactivated (false)
                this.activated[i] = (i < this.totalInputNeuronCount) ? true : false;
                this.inActivation[i] = false;
                if (i >= this.totalInputNeuronCount)
                    this.lastActivation[i] = this.neuronSignals[i];
            }

            // Get each output node activation recursively
            // NOTE: This is an assumption that genomes have started minimally, and the output nodes lie sequentially after the input nodes
            for (int i = 0; i < this.outputNeuronCount; i++)
                this.RecursiveActivateNode(this.totalInputNeuronCount + i);
        }

        private void RecursiveActivateNode(int currentNode)
        {
            // If we've reached an input node we return since the signal is already set
            if (this.activated[currentNode])
            {
                this.inActivation[currentNode] = false;
                return;
            }

            // Mark that the node is currently being calculated
            this.inActivation[currentNode] = true;

            // Set the presignal to 0
            this.neuronSignalsBeingProcessed[currentNode] = 0;

            // Adjacency list in reverse holds incoming connections, go through each one and activate it
            for (int i = 0; i < this.reverseAdjacentList[currentNode].Count; i++)
            {
                int crntAdjNode = this.reverseAdjacentList[currentNode][i];

                //{ Region recurrant connection handling - not applicable in our implementation
                // If this node is currently being activated then we have reached a cycle, or recurrant connection. Use the previous activation in this case
                if (this.inActivation[crntAdjNode])
                {
                    this.neuronSignalsBeingProcessed[currentNode] += this.lastActivation[crntAdjNode] * this.adjacentMatrix[crntAdjNode, currentNode];
                }

                // Otherwise proceed as normal
                else
                {
                    // Recurse if this neuron has not been activated yet
                    if (!this.activated[crntAdjNode])
                        RecursiveActivateNode(crntAdjNode);

                    // Add it to the new activation
                    this.neuronSignalsBeingProcessed[currentNode] += this.neuronSignals[crntAdjNode] * this.adjacentMatrix[crntAdjNode, currentNode];
                }
                //} endregion
            }

            // Mark this neuron as completed
            this.activated[currentNode] = true;

            // This is no longer being calculated (for cycle detection)
            this.inActivation[currentNode] = false;

            // Set this signal after running it through the activation function
            this.neuronSignals[currentNode] = this.activationFunctions[currentNode].Calculate(this.neuronSignalsBeingProcessed[currentNode]);
        }
        #endregion

    }
}
