using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using SharpNeatLib.NeuralNetwork;
using SharpNeatLib.NeatGenome;
using SharpNeatLib.Experiments;

namespace SharpNeatLib.CPPNs
{
    /**
     * Author: Sebastian Risi
     * 
     * Evolvable-Substrate HyperNEAT algorithm 
     **/
    public class EvolvableSubstrate
    {
        public ModularNetwork genome;
        public float initialDepth;
        public float maxDepth;
        public float divisionThreshold;
        public float varianceThreshold;
        public float bandThrehold;
        public bool useLEO = false;
        public float[] coordinates = new float[4];

        public class QuadPoint
        {
            float[] _outputs;
            public float[] Outputs
            {
                get { return _outputs; }
                set {
                    //create an array of the appropriate size 
                    _outputs = new float[value.Length];
                    //copy the array that's set
                    Array.Copy(value, _outputs, value.Length);
                }
            }
            public float x, y;
            public float w; //stores the CPPN values
            public float width; //width of this quadtree square
            public List<QuadPoint> childs;
            public int level; //the level in the quadtree

            public QuadPoint(float _x, float _y, float _w, int _level)
            {
                level = _level;
                w = 0.0f;
                x = _x;
                y = _y;
                width = _w;
                childs = new List<QuadPoint>();
            }
        }
        public class TempConnection
        {
            public float x1, y1, x2, y2;
            //public PointF start, end;
            public float weight;
            //we want to pass along any other information queried from the CPPN
            float[] _outputs;
            public float[] Outputs
            {
                get { return _outputs; }
                set
                {
                    //create an array of the appropriate size 
                    _outputs = new float[value.Length];
                    //copy the array that's set
                    Array.Copy(value, _outputs, value.Length);
                }
            }
            public TempConnection(float x1, float y1, float x2, float y2, float weight, float[] outs)
            {
            //    start = new PointF(x1, y1);
                this.x1 = x1;
                this.y1 = y1;
                this.x2 = x2;
                this.y2 = y2;
                this.weight = weight;
                this.Outputs = outs;
            }
        }

        /*
         * Input: Coordinates of source (outgoing = true) or target node (outgoing = false) at (a,b)
         * Output: Quadtree, in which each quadnode at (x,y) stores CPPN activation level for its
         *         position. The initialized quadtree is used in the PruningAndExtraction phase to
         *         generate the actual ANN connections.
         */
        public QuadPoint QuadTreeInitialisation(float a, float b, bool outgoing, int initialDepth, int maxDepth)
        {
            QuadPoint root = new QuadPoint(0.0f, 0.0f, 1.0f, 1); //x, y, width, level
            List<QuadPoint> queue = new List<QuadPoint>();
            queue.Add(root);

            while (queue.Count > 0)
            {
                QuadPoint p = queue[0];//dequeue
                queue.RemoveAt(0);

                // Divide into sub-regions and assign children to parent
                p.childs.Add(new QuadPoint(p.x - p.width / 2, p.y - p.width / 2, p.width / 2, p.level + 1));
                p.childs.Add(new QuadPoint(p.x - p.width / 2, p.y + p.width / 2, p.width / 2, p.level + 1));
                p.childs.Add(new QuadPoint(p.x + p.width / 2, p.y - p.width / 2, p.width / 2, p.level + 1));
                p.childs.Add(new QuadPoint(p.x + p.width / 2, p.y + p.width / 2, p.width / 2, p.level + 1));

                foreach (QuadPoint c in p.childs)
                {
                    if (outgoing) // Querying connection from input or hidden node
                    {
                        c.Outputs = queryCPPNOutputs(a, b, c.x, c.y); // Outgoing connectivity pattern
                        c.w = c.Outputs[0];
                    }
                    else // Querying connection to output node
                    {
                        c.Outputs = queryCPPNOutputs(c.x, c.y, a, b); // Incoming connectivity pattern
                        c.w = c.Outputs[0];
                    }
                }

                // Divide until initial resolution or if variance is still high
                if (p.level < initialDepth || (p.level < maxDepth && variance(p) > divisionThreshold))
                {
                    foreach (QuadPoint c in p.childs)
                    {
                        queue.Add(c);
                    }
                }
            }
            return root;
        }

        /*
         * Input : Coordinates of source (outgoing = true) or target node (outgoing = false) at (a,b) and initialized quadtree p. 
         * Output: Adds the connections that are in bands of the two-dimensional cross-section of the 
         *         hypercube containing the source or target node to the connections list.
         * 
         */

        public void PruneAndExpress(float a, float b, ref List<TempConnection> connections, QuadPoint node, bool outgoing, float maxDepth)
        {
            float left = 0.0f, right = 0.0f, top = 0.0f, bottom = 0.0f;

            if (node.childs[0] == null) return;

            // Traverse quadtree depth-first
            foreach (QuadPoint c in node.childs)
            {
                float childVariance = variance(c);

                if (childVariance >= varianceThreshold)
                {
                    PruneAndExpress(a, b, ref connections, c, outgoing, maxDepth);
                }
                else //this should always happen for at least the leaf nodes because their variance is zero  
                {
                    // Determine if point is in a band by checking neighbor CPPN values
                    if (outgoing)
                    {
                        left = Math.Abs(c.w - queryCPPNWeight(a, b, c.x - node.width, c.y));
                        right = Math.Abs(c.w - queryCPPNWeight(a, b, c.x + node.width, c.y));
                        top = Math.Abs(c.w - queryCPPNWeight(a, b, c.x, c.y - node.width));
                        bottom = Math.Abs(c.w - queryCPPNWeight(a, b, c.x, c.y + node.width));
                    }
                    else
                    {
                        left = Math.Abs(c.w - queryCPPNWeight(c.x - node.width, c.y, a, b));
                        right = Math.Abs(c.w - queryCPPNWeight(c.x + node.width, c.y, a, b));
                        top = Math.Abs(c.w - queryCPPNWeight(c.x, c.y - node.width, a, b));
                        bottom = Math.Abs(c.w - queryCPPNWeight(c.x, c.y + node.width, a, b));
                    }

                    if (Math.Max(Math.Min(top, bottom), Math.Min(left, right)) > bandThrehold)
                    {
                        TempConnection tc = null;
                        if (outgoing)
                        {
                            //check for LEO
                            if(useLEO && c.Outputs[1] > 0)
                                tc = new TempConnection(a, b, c.x, c.y, c.w, c.Outputs);
                            else if(!useLEO)
                                tc = new TempConnection(a, b, c.x, c.y, c.w, c.Outputs);
                            
                        }
                        else
                        {
                            //check against leo!
                            if (useLEO && c.Outputs[1] > 0)
                                tc = new TempConnection(c.x, c.y, a, b, c.w, c.Outputs);
                            else if(!useLEO)
                                tc = new TempConnection(c.x, c.y, a, b, c.w, c.Outputs);

                        }

                        if(tc!= null)
                            connections.Add(tc);
                    }

                }
            }
        }

        //Collect the CPPN values stored in a given quadtree p
        //Used to estimate the variance in a certain region in space
        private void getCPPNValues(ref List<float> l, QuadPoint p)
        {
            if (p != null && p.childs.Count > 0)
            {
                for (int i = 0; i < 4; i++)
                {
                    getCPPNValues(ref l, p.childs[i]);
                }
            }
            else
            {
                l.Add(p.w);
            }
        }

        //determine the variance of a certain region
        public float variance(QuadPoint p)
        {
            if (p.childs.Count == 0)
            {
                return 0.0f;
            }

            List<float> l = new List<float>();
            getCPPNValues(ref l, p);

            float m = 0.0f, v = 0.0f;
            foreach (float f in l)
            {
                m += f;
            }
            m /= l.Count;
            foreach (float f in l)
            {
                v += (float)Math.Pow(f - m, 2);
            }
            v /= l.Count;
            return v;
        }


        public float queryCPPNWeight(float x1, float y1, float x2, float y2)
        {
            coordinates[0] = x1;
            coordinates[1] = y1;
            coordinates[2] = x2;
            coordinates[3] = y2;

            genome.ClearSignals();
            genome.SetInputSignals(coordinates);
            genome.RecursiveActivation();
            
            return genome.GetOutputSignal(0);
        }

        public float[] queryCPPNOutputs(float x1, float y1, float x2, float y2)
        {
            coordinates[0] = x1;
            coordinates[1] = y1;
            coordinates[2] = x2;
            coordinates[3] = y2;

            //Console.WriteLine("Coordinates: ({0}, {1} : {2}, {3})", x1, y1, x2, y2);
            genome.ClearSignals();
            genome.SetInputSignals(coordinates);
            genome.RecursiveActivation();

            float[] outs = new float[genome.OutputNeuronCount];
            for(int i=0; i < genome.OutputNeuronCount; i++)
                outs[i] = genome.GetOutputSignal(i);

            return outs;
        }

        public float queryCPPN(float x1, float y1, float x2, float y2, int ix)
        {
            coordinates[0] = x1;
            coordinates[1] = y1;
            coordinates[2] = x2;
            coordinates[3] = y2;

            genome.ClearSignals();
            genome.SetInputSignals(coordinates);
            genome.RecursiveActivation();

            return genome.GetOutputSignal(ix);
        }

        /*
         * The main method that generations a list of ANN connections based on the information in the 
         * underlying hypercube. 
         * Input : CPPN, InputPositions, OutputPositions, ES-HyperNEAT parameters
         * Output: Connections, HiddenNodes
         */
        public void generateSubstrate(List<PointF> inputNeuronPositions, List<PointF> outputNeuronPositions,
            INetwork genome, int initialDepth, float varianceThreshold, float bandThreshold, int ESIterations,
                                                float divsionThreshold, int maxDepth,
                                                uint inputCount, uint outputCount,
                                                ref  ConnectionGeneList connections, ref List<PointF> hiddenNeurons, bool useLeo = false)
        {

            List<TempConnection> tempConnections = new List<TempConnection>();
            int sourceIndex, targetIndex = 0;
            uint counter = 0;

            this.genome = (ModularNetwork)genome;
            this.initialDepth = initialDepth;
            this.maxDepth = maxDepth;
            this.varianceThreshold = varianceThreshold;
            this.bandThrehold = bandThreshold;
            this.divisionThreshold = divsionThreshold;

            //CONNECTIONS DIRECTLY FROM INPUT NODES
            sourceIndex = 0;
            foreach (PointF input in inputNeuronPositions)
            {
                // Analyze outgoing connectivity pattern from this input
                QuadPoint root = QuadTreeInitialisation(input.X, input.Y, true, (int)initialDepth, (int)maxDepth);
                tempConnections.Clear();
                // Traverse quadtree and add connections to list
                PruneAndExpress(input.X, input.Y, ref tempConnections, root, true, maxDepth);
               
                foreach (TempConnection p in tempConnections)
                {
                    PointF newp = new PointF(p.x2, p.y2);

                    targetIndex = hiddenNeurons.IndexOf(newp);
                    if (targetIndex == -1) 
                    {
                        targetIndex = hiddenNeurons.Count;
                        hiddenNeurons.Add(newp);
                    }
                    connections.Add(new ConnectionGene(counter++, (sourceIndex), (targetIndex + inputCount + outputCount), p.weight * HyperNEATParameters.weightRange, new float[] {p.x1,p.y1,p.x2,p.y2}, p.Outputs));

                }
                sourceIndex++;
            }

            tempConnections.Clear();

            List<PointF> unexploredHiddenNodes = new List<PointF>();
            unexploredHiddenNodes.AddRange(hiddenNeurons);

            for (int step = 0; step < ESIterations; step++)
            {
                foreach (PointF hiddenP in unexploredHiddenNodes)
                {
                    tempConnections.Clear();
                    QuadPoint root = QuadTreeInitialisation(hiddenP.X, hiddenP.Y, true, (int)initialDepth, (int)maxDepth);
                    PruneAndExpress(hiddenP.X, hiddenP.Y, ref tempConnections, root, true, maxDepth);

                    sourceIndex = hiddenNeurons.IndexOf(hiddenP);   //TODO there might a computationally less expensive way

                    foreach (TempConnection p in tempConnections)
                    {

                        PointF newp = new PointF(p.x2, p.y2);

                        targetIndex = hiddenNeurons.IndexOf(newp);
                        if (targetIndex == -1)
                        {
                            targetIndex = hiddenNeurons.Count;
                            hiddenNeurons.Add(newp);

                        }
                        connections.Add(new ConnectionGene(counter++, (sourceIndex + inputCount + outputCount), (targetIndex + inputCount + outputCount), p.weight * HyperNEATParameters.weightRange, new float[] { p.x1, p.y1, p.x2, p.y2 }, p.Outputs));
                    }
                }
                // Remove the just explored nodes
                List<PointF> temp = new List<PointF>();
                temp.AddRange(hiddenNeurons);
                foreach (PointF f in unexploredHiddenNodes)
                    temp.Remove(f);

                unexploredHiddenNodes = temp;

            }

            tempConnections.Clear();

            //CONNECT TO OUTPUT
            targetIndex = 0;
            foreach (PointF outputPos in outputNeuronPositions)
            {
                // Analyze incoming connectivity pattern to this output
                QuadPoint root = QuadTreeInitialisation(outputPos.X, outputPos.Y, false, (int)initialDepth, (int)maxDepth);
                tempConnections.Clear();
                PruneAndExpress(outputPos.X, outputPos.Y, ref tempConnections, root, false, maxDepth);


                PointF target = new PointF(outputPos.X, outputPos.Y);

                foreach (TempConnection t in tempConnections)
                {
                    PointF source = new PointF(t.x1, t.y1);
                    sourceIndex = hiddenNeurons.IndexOf(source);

                    /* New nodes not created here because all the hidden nodes that are
                        connected to an input/hidden node are already expressed. */
                    if (sourceIndex != -1)  //only connect if hidden neuron already exists
                        connections.Add(new ConnectionGene(counter++, (sourceIndex + inputCount + outputCount), (targetIndex + inputCount), t.weight * HyperNEATParameters.weightRange, new float[] { t.x1, t.y1, t.x2, t.y2 }, t.Outputs));
                }
                targetIndex++;
            }
        }
    }
}
