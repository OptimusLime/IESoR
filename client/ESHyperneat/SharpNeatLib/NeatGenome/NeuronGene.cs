using System;
using SharpNeatLib.NeuralNetwork;

namespace SharpNeatLib.NeatGenome
{
    public class NeuronGene
    {
        // Although this id is allocated from the global innovation ID pool, neurons do not participate 
        // in compatibility measurements and so it is not used as an innovation ID. It is used as a unique
        // ID to distinguish between neurons.

        public static int OUTPUT_LAYER = 10;
        public static int INPUT_LAYER = 0;

        long innovationId;
        NeuronType neuronType;
        IActivationFunction activationFunction;
        public double Layer;

        public float Bias { get; set; }

        #region Constructor

        /// <summary>
        /// Copy constructor.
        /// </summary>
        /// <param name="copyFrom"></param>
        public NeuronGene(NeuronGene copyFrom)
        {
            this.innovationId = copyFrom.innovationId;
            this.neuronType = copyFrom.neuronType;
            this.activationFunction = copyFrom.activationFunction;
            this.Layer = copyFrom.Layer;
        }

        public NeuronGene(long innovationId, NeuronType neuronType, IActivationFunction activationFunction)
        {
            this.innovationId = innovationId;
            this.neuronType = neuronType;
            this.activationFunction = activationFunction;
        }


        public NeuronGene(NeuronGene copyFrom, long _innovationId, double _layer, NeuronType _neuronType, IActivationFunction _activationFunction)
        {
            if (copyFrom != null)
            {
                this.innovationId = copyFrom.InnovationId;
                this.neuronType = copyFrom.NeuronType;
                this.activationFunction = copyFrom.ActivationFunction;
                this.Layer = copyFrom.Layer;
            }
            else
            {
                this.innovationId = _innovationId;
                this.neuronType = _neuronType;
                this.activationFunction = _activationFunction;
                this.Layer = _layer;
            }
        }
        #endregion

        #region Properties

        public long InnovationId
        {
            get
            {
                return innovationId;
            }
            set
            {
                innovationId = value;
            }
        }

        public NeuronType NeuronType
        {
            get
            {
                return neuronType;
            }
            set
            {
                neuronType = value;
            }
        }

        public IActivationFunction ActivationFunction
        {
            get
            {
                return activationFunction;
            }
            set
            {
                activationFunction = value;
            }
        }

        #endregion
    }
}
