using System;
using System.Collections.Generic;
using System.Text;
using SharpNeatLib.NeuralNetwork;
using SharpNeatLib.Xml;
using System.Xml;
using System.Drawing;
using SharpNeatLib.Experiments;
using SharpNeatLib.NeatGenome;
using System.Runtime.CompilerServices;

namespace SharpNeatLib.CPPNs
{
    public class SubstrateDescription
    {
        private List<NeuronGroup> neuronGroups;
        private List<PointF> hiddenNeurons;
        private List<PointF> inputNeurons;
        private List<PointF> outputNeurons;

        //ANN input, output and hidden count
        public uint InputCount { get; set; }
        public uint OutputCount { get; set; }
        public uint HiddenCount { get; set; }

        public bool useLeo;
        public SubstrateDescription(String filename)
        {
            useLeo = false;
            HiddenCount = 0;
            InputCount = 0;
            OutputCount = 0;

            hiddenNeurons = new List<PointF>();
            inputNeurons = new List<PointF>();
            outputNeurons = new List<PointF>();

            neuronGroups = new List<NeuronGroup>();

            XmlDocument document = new XmlDocument();
            document.Load(filename);
            XmlElement xmlSubstrate = (XmlElement)document.SelectSingleNode("substrate");

            if (xmlSubstrate == null)
                throw new Exception("The genome XML is missing the root 'substrate' element.");
            this.useLeo = bool.Parse(XmlUtilities.GetAttribute(xmlSubstrate, "leo").Value);
            //--- Read neuron genes into a list.
            //NeuronGeneList neuronGeneList = new NeuronGeneList();

            XmlElement xmlGroups = (XmlElement)xmlSubstrate.SelectSingleNode("neuronGroups");
            XmlNodeList listNeuronGroups = xmlGroups.SelectNodes("group");
            foreach (XmlElement xmlNeuronGroup in listNeuronGroups)
            {
                int id = int.Parse(XmlUtilities.GetAttribute(xmlNeuronGroup, "id").Value);
                String tmp = XmlUtilities.GetAttribute(xmlNeuronGroup, "type").Value;

                int neuronGroupType = -1;

                float startX = float.Parse(XmlUtilities.GetAttribute(xmlNeuronGroup, "startx").Value);
                float startY = float.Parse(XmlUtilities.GetAttribute(xmlNeuronGroup, "starty").Value);
                float endX = float.Parse(XmlUtilities.GetAttribute(xmlNeuronGroup, "endx").Value);
                float endY = float.Parse(XmlUtilities.GetAttribute(xmlNeuronGroup, "endy").Value);
                uint dx = uint.Parse(XmlUtilities.GetAttribute(xmlNeuronGroup, "dx").Value);
                uint dy = uint.Parse(XmlUtilities.GetAttribute(xmlNeuronGroup, "dy").Value);
                NeuronGroup ng = null;

                if (tmp.Equals("Hidden"))
                {
                    neuronGroupType = 2;
                    ng = new NeuronGroup(startX, startY, endX, endY, dx, dy, id, neuronGroupType, HiddenCount);
                    HiddenCount += dx * dy;
                }
                else if (tmp.Equals("Output"))
                {
                    neuronGroupType = 1;
                    ng = new NeuronGroup(startX, startY, endX, endY, dx, dy, id, neuronGroupType, OutputCount);
                    OutputCount += dx * dy;
                }
                else if (tmp.Equals("Input"))
                {
                    neuronGroupType = 0;
                    ng = new NeuronGroup(startX, startY, endX, endY, dx, dy, id, neuronGroupType, InputCount);
                    InputCount += dx * dy;
                }

                //  Console.WriteLine(id + " " + neuronGroupType + " " + startX + " " + startY + " " + endX + " " + endY + " " + dx + " " + dy);

                neuronGroups.Add(ng);
            }
            //Load Connections
            XmlElement xmlConnections = (XmlElement)xmlSubstrate.SelectSingleNode("connections");
            XmlNodeList listConnections = xmlConnections.SelectNodes("connection");
            foreach (XmlElement xmlConnection in listConnections)
            {
                int srcID = int.Parse(XmlUtilities.GetAttribute(xmlConnection, "src-id").Value);
                int tgID = int.Parse(XmlUtilities.GetAttribute(xmlConnection, "tg-id").Value);
                foreach (NeuronGroup n in neuronGroups)
                {
                    if (n.GroupID == srcID)
                    {
                        n.ConnectedTo.Add(tgID);
                    }
                }
            }
        }

        //Get the start ID for a specific group
        public uint getStartID(uint groupID)
        {
            return getNeuronGroup(groupID).GlobalID;
        }

        public void getNeuronDensity(uint groupID, out uint dx, out uint dy)
        {
            dx = getNeuronGroup(groupID).DX;
            dy = getNeuronGroup(groupID).DY;
        }
        public List<PointF> getNeuronGroupByType(uint groupType)
        {
            List<PointF> ngl = new List<PointF>();

            foreach (NeuronGroup ng in neuronGroups)
            {
                if (ng.GroupType == groupType)
                {
                    ngl.AddRange(ng.NeuronPositions);
                }
            }
            return ngl;
        }

        //Changes the neuron density for a given group. Call generateGenome afterwards to get the new network
        public void setNeuronDensity(uint groupID, uint dx, uint dy)
        {
            //Console.WriteLine("Changing neuron density. id:" + groupID + " dx:" + dx + " dy:" + dy);
            getNeuronGroup(groupID).DX = dx;
            getNeuronGroup(groupID).DY = dy;
            updateNeuronCounts();
            foreach (NeuronGroup ng in neuronGroups)
            {
                ng.generateNodePositions();
            }
        }

        //Call updateNeuronCount after changing the density of a node group
        private void updateNeuronCounts()
        {
            HiddenCount = 0;
            InputCount = 0;
            OutputCount = 0;

            hiddenNeurons = new List<PointF>();
            inputNeurons = new List<PointF>();
            outputNeurons = new List<PointF>();

            foreach (NeuronGroup ng in neuronGroups)
            {
                switch (ng.GroupType)
                {
                    case 2: HiddenCount += ng.DX * ng.DY; break;
                    case 1: OutputCount += ng.DX * ng.DY; break;
                    case 0: InputCount += ng.DX * ng.DY; break;
                }
            }
        }

        public void normalizeWeightConnections(ref ConnectionGeneList connections, int neuronCount)
        {
            double[] weightSumPos = new double[neuronCount];
            double[] weightSumNeg = new double[neuronCount];
            ////Normalize Connection Weights
            ////ONLY NORMALIZE WEIGHTS BETWEEN HIDDEN NEURONS
            for (int i = 0; i < connections.Count; i++)
            {

                if (connections[i].Weight >= 0.0f)
                {
                    weightSumPos[connections[i].TargetNeuronId] += Math.Abs(connections[i].Weight); //connections[i].weight; //Abs value?
                }
                else
                {
                    weightSumNeg[connections[i].TargetNeuronId] += Math.Abs(connections[i].Weight); //connections[i].weight; //Abs value?

                }

            }
            for (int i = 0; i < connections.Count; i++)
            {

                if (connections[i].Weight >= 0.0f)
                {
                    if (weightSumPos[connections[i].TargetNeuronId] != 0.0f)
                        connections[i].Weight /= weightSumPos[connections[i].TargetNeuronId];
                }
                else
                {
                    if (weightSumNeg[connections[i].TargetNeuronId] != 0.0f)
                        connections[i].Weight /= weightSumNeg[connections[i].TargetNeuronId];
                }
                connections[i].Weight *= 3.0;
            }
        }

        public NeuronGroup getNeuronGroup(uint groupID)
        {
            foreach (NeuronGroup ng in neuronGroups)
            {
                if (ng.GroupID == groupID)
                {
                    return ng;
                }
            }
            return null;
        }

        public NeatGenome.NeatGenome generateGenome(INetwork network)
        {
            return null;    //Not supported right now
        }

        public NeatGenome.NeatGenome generateMultiGenomeModulus(INetwork network, uint numberOfAgents)
        {
            return null; //Not supported right now
        }

        #region Generate homogenous genome
        public NeatGenome.NeatGenome generateHomogeneousGenome(INetwork network, bool normalizeWeights, bool adaptiveNetwork, bool modulatoryNet, bool evolveSubstrate)
        {
            if (evolveSubstrate)
            {
                return generateHomogeneousGenomeES(network, normalizeWeights, adaptiveNetwork, modulatoryNet);
            }
            else
                return generateHomogeneousGenome(network, normalizeWeights, adaptiveNetwork, modulatoryNet);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private NeatGenome.NeatGenome generateHomogeneousGenomeES(INetwork network, bool normalizeWeights, bool adaptiveNetwork, bool modulatoryNet)
        {
            List<PointF> hiddenNeuronPositions = new List<PointF>();

            IActivationFunction activationFunction = HyperNEATParameters.substrateActivationFunction;
            ConnectionGeneList connections = new ConnectionGeneList();//(int)((InputCount * HiddenCount) + (HiddenCount * OutputCount)));

            List<PointF> outputNeuronPositions = getNeuronGroupByType(1);
            List<PointF> inputNeuronPositions = getNeuronGroupByType(0);


            EvolvableSubstrate se = new EvolvableSubstrate();

            se.generateSubstrate(inputNeuronPositions, outputNeuronPositions, network,
                HyperNEATParameters.initialDepth,
                (float)HyperNEATParameters.varianceThreshold,
                (float)HyperNEATParameters.bandingThreshold,
                (int)HyperNEATParameters.ESIterations,
                (float)HyperNEATParameters.divisionThreshold,
                HyperNEATParameters.maximumDepth, 
                InputCount, OutputCount, ref connections, ref hiddenNeuronPositions);

            HiddenCount = (uint)hiddenNeuronPositions.Count;

            float[] coordinates = new float[5];
            long connectionCounter = connections.Count;

            NeuronGeneList neurons;
            // SharpNEAT requires that the neuron list be in this order: bias|input|output|hidden
            neurons = new NeuronGeneList((int)(InputCount + OutputCount + HiddenCount));

            // set up the input nodes
            for (uint a = 0; a < InputCount; a++)
            {
                neurons.Add(new NeuronGene(a, NeuronType.Input, ActivationFunctionFactory.GetActivationFunction("NullFn")));
            }

            // set up the output nodes
            for (uint a = 0; a < OutputCount; a++)
            {

                neurons.Add(new NeuronGene(a + InputCount, NeuronType.Output, activationFunction));

            }
            // set up the hidden nodes
            for (uint a = 0; a < HiddenCount; a++)
            {
                neurons.Add(new NeuronGene(a + InputCount + OutputCount, NeuronType.Hidden, activationFunction));
            }
            
            bool[] visited = new bool[neurons.Count];
            List<long> nodeList = new List<long>();
            bool[] connectedToInput = new bool[neurons.Count];

            bool[] isOutput = new bool[neurons.Count];
            
            bool danglingConnection = true;

            while (danglingConnection)
            {
                bool[] hasIncomming = new bool[neurons.Count];

                foreach (ConnectionGene co in connections)
                {
                    hasIncomming[co.TargetNeuronId] = true;
                }
                for (int i = 0; i < InputCount; i++)
                    hasIncomming[i] = true;

                bool[] hasOutgoing = new bool[neurons.Count];
                foreach (ConnectionGene co in connections)
                {
                    if (co.TargetNeuronId != co.SourceNeuronId)  //neurons that only connect to themselfs don't count
                    {
                        hasOutgoing[co.SourceNeuronId] = true;
                    }
                }

                //Keep  output neurons
                for (int i = 0; i < OutputCount; i++)
                    hasOutgoing[i + InputCount] = true;


                danglingConnection = false;
                //Check if there are still dangling connections
                foreach (ConnectionGene co in connections)
                {
                    if (!hasOutgoing[co.TargetNeuronId] || !hasIncomming[co.SourceNeuronId])
                    {
                        danglingConnection = true;
                        break;
                    }
                }

                connections.RemoveAll(delegate(ConnectionGene m) { return (!hasIncomming[m.SourceNeuronId]); });
                connections.RemoveAll(delegate(ConnectionGene m) { return (!hasOutgoing[m.TargetNeuronId]); });
            }

            if (normalizeWeights)
            {
                normalizeWeightConnections(ref connections, neurons.Count);
            }

            SharpNeatLib.NeatGenome.NeatGenome gn = new SharpNeatLib.NeatGenome.NeatGenome(0, neurons, connections, (int)(InputCount), (int)(OutputCount));
            
            gn.networkAdaptable = adaptiveNetwork;
            gn.networkModulatory = modulatoryNet;

            return gn;
        }

        private NeatGenome.NeatGenome generateHomogeneousGenome(INetwork network, bool normalizeWeights, bool adaptiveNetwork, bool modulatoryNet)
        {
            IActivationFunction activationFunction = HyperNEATParameters.substrateActivationFunction;
            ConnectionGeneList connections = new ConnectionGeneList((int)((InputCount * HiddenCount) + (HiddenCount * OutputCount)));
            float[] coordinates = new float[4];
            float output;
            uint connectionCounter = 0;
            int iterations = 2 * (network.TotalNeuronCount - (network.InputNeuronCount + network.OutputNeuronCount)) + 1;

            uint totalOutputCount = OutputCount;
            uint totalInputCount = InputCount;
            uint totalHiddenCount = HiddenCount;

            uint sourceCount, targetCout;
            double weightRange = HyperNEATParameters.weightRange;
            double threshold = HyperNEATParameters.threshold;

            NeuronGeneList neurons;
            // SharpNEAT requires that the neuron list be in this order: bias|input|output|hidden
            neurons = new NeuronGeneList((int)(InputCount + OutputCount + HiddenCount));

            // set up the input nodes
            for (uint a = 0; a < totalInputCount; a++)
            {
                neurons.Add(new NeuronGene(a, NeuronType.Input, ActivationFunctionFactory.GetActivationFunction("NullFn")));
            }
            // set up the output nodes
            for (uint a = 0; a < totalOutputCount; a++)
            {

                neurons.Add(new NeuronGene(a + InputCount, NeuronType.Output, activationFunction));
            }
            // set up the hidden nodes
            for (uint a = 0; a < totalHiddenCount; a++)
            {
                neurons.Add(new NeuronGene(a + InputCount + OutputCount, NeuronType.Hidden, activationFunction));
            }

            bool[] biasCalculated = new bool[totalHiddenCount + totalOutputCount + totalInputCount];
          

            uint sourceID = uint.MaxValue, targetID = uint.MaxValue;
            NeuronGroup connectedNG;

            foreach (NeuronGroup ng in neuronGroups)
            {
                foreach (uint connectedTo in ng.ConnectedTo)
                {
                    connectedNG = getNeuronGroup(connectedTo);

                    sourceCount = 0;
                    foreach (PointF source in ng.NeuronPositions)
                    {

                        targetCout = 0;
                        foreach (PointF target in connectedNG.NeuronPositions)
                        {
                            switch (ng.GroupType)
                            {
                                case 0: sourceID = ng.GlobalID + sourceCount; break;                             //Input
                                case 1: sourceID = totalInputCount + ng.GlobalID + sourceCount; break;                //Output
                                case 2: sourceID = totalInputCount + totalOutputCount + ng.GlobalID + sourceCount; break;  //Hidden
                            }

                            switch (connectedNG.GroupType)
                            {
                                case 0: targetID = connectedNG.GlobalID + targetCout; break;
                                case 1: targetID = totalInputCount + connectedNG.GlobalID + targetCout; break;
                                case 2: targetID = totalInputCount + totalOutputCount + connectedNG.GlobalID + targetCout; break;
                            }
                            
                            //calculate bias of target node
                            if (!biasCalculated[targetID])
                            {
                                coordinates[0] = 0.0f; coordinates[1] = 0.0f; coordinates[2] = target.X; coordinates[3] = target.Y;

                                network.ClearSignals();
                                network.SetInputSignals(coordinates);
                                ((ModularNetwork)network).RecursiveActivation();
                                neurons[(int)targetID].Bias = (float)(network.GetOutputSignal(1) * weightRange);
                                biasCalculated[targetID] = true;
                            }

                            coordinates[0] = source.X;
                            coordinates[1] = source.Y;
                            coordinates[2] = target.X;
                            coordinates[3] = target.Y;

                            network.ClearSignals();
                            network.SetInputSignals(coordinates);
                            ((ModularNetwork)network).RecursiveActivation();
                            //network.MultipleSteps(iterations);
                            output = network.GetOutputSignal(0);

                            if (Math.Abs(output) > threshold)
                            {
                                float weight = (float)(((Math.Abs(output) - (threshold)) / (1 - threshold)) * weightRange * Math.Sign(output));
                                connections.Add(new ConnectionGene(connectionCounter++, sourceID, targetID, weight, ref coordinates, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f));
                            }
                            //else
                            //{
                            //    Console.WriteLine("Not connected");
                            //}
                            targetCout++;
                        }
                        sourceCount++;
                    }
                }
            }
            if (normalizeWeights)
            {
                normalizeWeightConnections(ref connections, neurons.Count);
            }
            NeatGenome.NeatGenome gn = new SharpNeatLib.NeatGenome.NeatGenome(0, neurons, connections, (int)(totalInputCount), (int)(totalOutputCount));

            gn.networkAdaptable = adaptiveNetwork;
            gn.networkModulatory = modulatoryNet;
            return gn;
        }
        #endregion

        #region Generate heterogenous genomes with z-stack
        public NeatGenome.NeatGenome generateMultiGenomeStack(INetwork network, List<float> stackCoordinates, bool normalizeWeights, bool adaptiveNetwork, bool modulatoryNet)
        {
            uint numberOfAgents = (uint)stackCoordinates.Count;
            IActivationFunction activationFunction = HyperNEATParameters.substrateActivationFunction;
            ConnectionGeneList connections = new ConnectionGeneList((int)(numberOfAgents * (InputCount * HiddenCount) + numberOfAgents * (HiddenCount * OutputCount)));
            float[] coordinates = new float[5];
            float output;
            uint connectionCounter = 0;
            float agentDelta = 2.0f / (numberOfAgents - 1);
            int iterations = 2 * (network.TotalNeuronCount - (network.InputNeuronCount + network.OutputNeuronCount)) + 1;

            uint totalOutputCount = OutputCount * numberOfAgents;
            uint totalInputCount = InputCount * numberOfAgents;
            uint totalHiddenCount = HiddenCount * numberOfAgents;

            uint sourceCount, targetCout;
            double weightRange = HyperNEATParameters.weightRange;
            double threshold = HyperNEATParameters.threshold;

            NeuronGeneList neurons;
            // SharpNEAT requires that the neuron list be in this order: bias|input|output|hidden
            neurons = new NeuronGeneList((int)(InputCount * numberOfAgents + OutputCount * numberOfAgents + HiddenCount * numberOfAgents));

            // set up the input nodes
            for (uint a = 0; a < totalInputCount; a++)
            {
                neurons.Add(new NeuronGene(a, NeuronType.Input, ActivationFunctionFactory.GetActivationFunction("NullFn")));
            }
            // set up the output nodes
            for (uint a = 0; a < totalOutputCount; a++)
            {

                neurons.Add(new NeuronGene(a + InputCount * numberOfAgents, NeuronType.Output, activationFunction));
            }
            // set up the hidden nodes
            for (uint a = 0; a < totalHiddenCount; a++)
            {
                neurons.Add(new NeuronGene(a + InputCount * numberOfAgents + OutputCount * numberOfAgents, NeuronType.Hidden, activationFunction));
            }

            bool[] biasCalculated = new bool[totalHiddenCount + totalOutputCount + totalInputCount];
          
            uint agent = 0;
            float A = 0.0f, B = 0.0f, C = 0.0f, D = 0.0f, learningRate = 0.0f, modConnection;

            foreach (float stackCoordinate in stackCoordinates)
            {
                coordinates[4] = stackCoordinate;
                uint sourceID = uint.MaxValue, targetID = uint.MaxValue;
                NeuronGroup connectedNG;

                foreach (NeuronGroup ng in neuronGroups)
                {
                    foreach (uint connectedTo in ng.ConnectedTo)
                    {
                        connectedNG = getNeuronGroup(connectedTo);

                        sourceCount = 0;
                        foreach (PointF source in ng.NeuronPositions)
                        {

                            targetCout = 0;
                            foreach (PointF target in connectedNG.NeuronPositions)
                            {
                                switch (ng.GroupType)
                                {
                                    case 0: sourceID = (agent * InputCount) + ng.GlobalID + sourceCount; break;                             //Input
                                    case 1: sourceID = totalInputCount + (agent * OutputCount) + ng.GlobalID + sourceCount; break;                //Output
                                    case 2: sourceID = totalInputCount + totalOutputCount + (agent * HiddenCount) + ng.GlobalID + sourceCount; break;  //Hidden
                                }

                                switch (connectedNG.GroupType)
                                {
                                    case 0: targetID = (agent * InputCount) + connectedNG.GlobalID + targetCout; break;
                                    case 1: targetID = totalInputCount + (agent * OutputCount) + connectedNG.GlobalID + targetCout; break;
                                    case 2: targetID = totalInputCount + totalOutputCount + (agent * HiddenCount) + connectedNG.GlobalID + targetCout; break;
                                }
                                
                                //target node bias
                                if (!biasCalculated[targetID])
                                {
                                    coordinates[0] = 0.0f; coordinates[1] = 0.0f; coordinates[2] = target.X; coordinates[3] = target.Y;

                                    network.ClearSignals();
                                    network.SetInputSignals(coordinates);
                                    ((ModularNetwork)network).RecursiveActivation();
                                    neurons[(int)targetID].Bias = (float)(network.GetOutputSignal(1) * weightRange);
                                    biasCalculated[targetID] = true;
                                }

                                coordinates[0] = source.X;
                                coordinates[1] = source.Y;
                                coordinates[2] = target.X;
                                coordinates[3] = target.Y;

                                network.ClearSignals();
                                network.SetInputSignals(coordinates);
                                ((ModularNetwork)network).RecursiveActivation();
                                //network.MultipleSteps(iterations);
                                output = network.GetOutputSignal(0);

                                double leo = 0.0;

                                if (adaptiveNetwork)
                                {
                                    A = network.GetOutputSignal(2);
                                    B = network.GetOutputSignal(3);
                                    C = network.GetOutputSignal(4);
                                    D = network.GetOutputSignal(5);
                                    learningRate = network.GetOutputSignal(6);
                                }

                                if (modulatoryNet)
                                {
                                    modConnection = network.GetOutputSignal(7);
                                }
                                else
                                {
                                    modConnection = 0.0f;
                                }

                                if (useLeo)
                                {
                                    threshold = 0.0;
                                    leo = network.GetOutputSignal(2);
                                }

                                if (!useLeo || leo > 0.0)
                                    if (Math.Abs(output) > threshold)
                                    {
                                        float weight = (float)(((Math.Abs(output) - (threshold)) / (1 - threshold)) * weightRange * Math.Sign(output));
                                        //if (adaptiveNetwork)
                                        //{
                                        //    //If adaptive network set weight to small value
                                        //    weight = 0.1f;
                                        //}
                                        connections.Add(new ConnectionGene(connectionCounter++, sourceID, targetID, weight, ref coordinates, A, B, C, D, modConnection, learningRate));
                                    }
                                //else
                                //{
                                //    Console.WriteLine("Not connected");
                                //}
                                targetCout++;
                            }
                            sourceCount++;
                        }
                    }
                }
                agent++;
            }
            if (normalizeWeights)
            {
                normalizeWeightConnections(ref connections, neurons.Count);
            }
            SharpNeatLib.NeatGenome.NeatGenome sng = new SharpNeatLib.NeatGenome.NeatGenome(0, neurons, connections, (int)(totalInputCount), (int)(totalOutputCount));
            sng.networkAdaptable = adaptiveNetwork;
            sng.networkModulatory = modulatoryNet;
            return sng;
        }

        public NeatGenome.NeatGenome generateMultiGenomeStack(INetwork network, uint numberOfAgents, bool normalizeWeights, bool adaptiveNetwork, bool modNetwork, out List<float> coords, bool evolvedSubstrate)
        {
            //List<float>
            coords = new List<float>();

            float coord = -1.0f;
            float delta = 2.0f / (numberOfAgents - 1);
            for (uint x = 0; x < numberOfAgents; x++)
            {
                coords.Add(coord);
                coord += delta;
            }

            if (evolvedSubstrate)
            {
                Console.WriteLine("Evolvable-Substrate not supported for heterogenous agents right now");
                return null;
            }
            else
                return this.generateMultiGenomeStack(network, coords, normalizeWeights, adaptiveNetwork, modNetwork);
        }

        #endregion

        #region Generate heterogenous genomes with situational policy
        public List<NeatGenome.NeatGenome> generateGenomeStackSituationalPolicy(INetwork network, uint numberOfAgents, bool normalizeWeights, bool adaptiveNetwork, bool modNetwork, int numSig, out List<float> coords)
        {
            float signal = 0;
            List<NeatGenome.NeatGenome> genomes = new
 List<NeatGenome.NeatGenome>(numSig);
            coords = new List<float>((int)numberOfAgents);
            for (int j = 0; j < numSig; j++)
            {
                if (numSig <= 1)
                    signal = 0;
                else
                    signal = ((2.0f / (numSig - 1)) * j) + -1.0f;
                genomes.Add(generateGenomeStackSituationalPolicy(network,
 numberOfAgents, normalizeWeights, adaptiveNetwork, modNetwork, out
coords, signal));
            }

            return genomes;
        }

        public NeatGenome.NeatGenome generateGenomeStackSituationalPolicy(INetwork network, uint numberOfAgents, bool normalizeWeights, bool adaptiveNetwork, bool modNetwork, out List<float> coords, float signal)
        {
            coords = new List<float>();

            float coord = -1.0f;
            float delta = 2.0f / (numberOfAgents - 1);
            for (uint x = 0; x < numberOfAgents; x++)
            {
                coords.Add(coord);
                coord += delta;
            }

            return this.generateGenomeStackSituationalPolicy(network, coords,
 normalizeWeights, adaptiveNetwork, modNetwork, signal);
        }

        public NeatGenome.NeatGenome generateGenomeStackSituationalPolicy(INetwork network, List<float> stackCoordinates, bool normalizeWeights, bool adaptiveNetwork, bool modulatoryNet, float signal)
        {
            uint numberOfAgents = (uint)stackCoordinates.Count;
            IActivationFunction activationFunction =
 HyperNEATParameters.substrateActivationFunction;
            ConnectionGeneList connections = new
 ConnectionGeneList((int)(numberOfAgents * (InputCount * HiddenCount) +
 numberOfAgents * (HiddenCount * OutputCount)));
            float[] coordinates = new float[5 + 1];
            float output;
            uint connectionCounter = 0;
            float agentDelta = 2.0f / (numberOfAgents - 1);
            int iterations = 2 * (network.TotalNeuronCount -
 (network.InputNeuronCount + network.OutputNeuronCount)) + 1;

            uint totalOutputCount = OutputCount * numberOfAgents;
            uint totalInputCount = InputCount * numberOfAgents;
            uint totalHiddenCount = HiddenCount * numberOfAgents;

            uint sourceCount, targetCout;
            double weightRange = HyperNEATParameters.weightRange;
            double threshold = HyperNEATParameters.threshold;

            bool[] biasCalculated = new bool[totalHiddenCount + totalOutputCount+totalInputCount];
            coordinates[5] = signal;

            NeuronGeneList neurons;
            // SharpNEAT requires that the neuron list be in thisorder: bias|input|output|hidden
            neurons = new NeuronGeneList((int)(InputCount *
 numberOfAgents + OutputCount * numberOfAgents + HiddenCount *
 numberOfAgents));

            // set up the input nodes
            for (uint a = 0; a < totalInputCount; a++)
            {
                neurons.Add(new NeuronGene(a, NeuronType.Input,
 ActivationFunctionFactory.GetActivationFunction("NullFn")));
            }
            // set up the output nodes
            for (uint a = 0; a < totalOutputCount; a++)
            {

                neurons.Add(new NeuronGene(a + InputCount *
 numberOfAgents, NeuronType.Output, activationFunction));
            }
            // set up the hidden nodes
            for (uint a = 0; a < totalHiddenCount; a++)
            {
                neurons.Add(new NeuronGene(a + InputCount *
 numberOfAgents + OutputCount * numberOfAgents, NeuronType.Hidden,
 activationFunction));
            }

            uint agent = 0;
            float A = 0.0f, B = 0.0f, C = 0.0f, D = 0.0f, learningRate
 = 0.0f, modConnection;

            foreach (float stackCoordinate in stackCoordinates)
            {
                coordinates[4] = stackCoordinate;
                uint sourceID = uint.MaxValue, targetID = uint.MaxValue;
                NeuronGroup connectedNG;

                foreach (NeuronGroup ng in neuronGroups)
                {
                    foreach (uint connectedTo in ng.ConnectedTo)
                    {
                        connectedNG = getNeuronGroup(connectedTo);

                        sourceCount = 0;


                        foreach (PointF source in ng.NeuronPositions)
                        {

                            //----------------------------

                            targetCout = 0;
                            foreach (PointF target in connectedNG.NeuronPositions)
                            {
                                switch (ng.GroupType)
                                {
                                    case 0: sourceID = (agent *  InputCount) + ng.GlobalID + sourceCount; break;
                                    //Input
                                    case 1: sourceID = totalInputCount + (agent * OutputCount) + ng.GlobalID + sourceCount; break;
                                    //Output
                                    case 2: sourceID = totalInputCount + totalOutputCount + (agent * HiddenCount) + ng.GlobalID + sourceCount; break;  //Hidden
                                }

                                switch (connectedNG.GroupType)
                                {
                                    case 0: targetID = (agent * InputCount) + connectedNG.GlobalID + targetCout; break;
                                    case 1: targetID = totalInputCount + (agent * OutputCount) + connectedNG.GlobalID + targetCout; break;
                                    case 2: targetID = totalInputCount + totalOutputCount + (agent * HiddenCount) + connectedNG.GlobalID + targetCout; break;
                                }
                                                                
                                //--- bias
                                //-----------------Get the bias of the target node
                                if (!biasCalculated[targetID])
                                {
                                    coordinates[0] = 0.0f; coordinates[1] = 0.0f; coordinates[2] = target.X; coordinates[3] = target.Y;

                                    network.ClearSignals();
                                    network.SetInputSignals(coordinates);
                                    ((ModularNetwork)network).RecursiveActivation();
                                    neurons[(int)targetID].Bias = (float)(network.GetOutputSignal(1) * weightRange);
                                    biasCalculated[targetID] = true;
                                }
                                //--bias



                                coordinates[0] = source.X;
                                coordinates[1] = source.Y;
                                coordinates[2] = target.X;
                                coordinates[3] = target.Y;

                                network.ClearSignals();
                                network.SetInputSignals(coordinates);
                                ((ModularNetwork)network).RecursiveActivation();
                                //network.MultipleSteps(iterations);
                                output = network.GetOutputSignal(0);

                                double leo = 0.0;

                                if (adaptiveNetwork)
                                {
                                    A = network.GetOutputSignal(2);
                                    B = network.GetOutputSignal(3);
                                    C = network.GetOutputSignal(4);
                                    D = network.GetOutputSignal(5);
                                    learningRate = network.GetOutputSignal(6);
                                }

                                if (modulatoryNet)
                                {
                                    modConnection = network.GetOutputSignal(7);
                                }
                                else
                                {
                                    modConnection = 0.0f;
                                }

                                if (useLeo)
                                {
                                    threshold = 0.0;
                                    leo = network.GetOutputSignal(2);
                                }

                                if (!useLeo || leo > 0.0)
                                    if (Math.Abs(output) > threshold)
                                    {
                                        float weight =
 (float)(((Math.Abs(output) - (threshold)) / (1 - threshold)) *
 weightRange * Math.Sign(output));
                                        //if (adaptiveNetwork)
                                        //{
                                        //    //If adaptive networkset weight to small value
                                        //    weight = 0.1f;
                                        //}
                                        connections.Add(new
 ConnectionGene(connectionCounter++, sourceID, targetID, weight, ref
coordinates, A, B, C, D, modConnection, learningRate));
                                    }
                                //else
                                //{
                                //    Console.WriteLine("Not connected");
                                //}
                                targetCout++;
                            }
                            sourceCount++;
                        }
                    }
                }
                agent++;
            }
            if (normalizeWeights)
            {
                normalizeWeightConnections(ref connections, neurons.Count);
            }
            SharpNeatLib.NeatGenome.NeatGenome sng = new
 SharpNeatLib.NeatGenome.NeatGenome(0, neurons, connections,
 (int)(totalInputCount), (int)(totalOutputCount));
            sng.networkAdaptable = adaptiveNetwork;
            sng.networkModulatory = modulatoryNet;
            return sng;
        }

        #endregion
    }
}
