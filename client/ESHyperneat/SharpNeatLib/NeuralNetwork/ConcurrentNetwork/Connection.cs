using System;

namespace SharpNeatLib.NeuralNetwork
{

	public class Connection
	{
        long sourceNeuronId; // These are redundant in normal operation (we have a reference to the neurons)
        long targetNeuronId;	// but is useful when creating/loading a network.

		Neuron sourceNeuron;								
		double weight;

		#region Constructor

        public Connection(long sourceNeuronId, long targetNeuronId, double weight)
		{
			this.sourceNeuronId = sourceNeuronId;
			this.targetNeuronId = targetNeuronId;
			this.weight = weight;
		}

		#endregion

		#region Public methods

		public void SetSourceNeuron(Neuron neuron)
		{
			sourceNeuron = neuron;
		}

		#endregion

		#region Properties

        public long SourceNeuronId
		{
			get	
			{
				return sourceNeuronId;	
			}
			set
			{
				sourceNeuronId = value;	
			}
		}

        public long TargetNeuronId
		{
			get	
			{
				return targetNeuronId;	
			}
			set
			{
				targetNeuronId = value;	
			}
		}

		public double Weight
		{
			get
			{
				return weight;	
			}
		}

		public Neuron SourceNeuron
		{
			get
			{
				return sourceNeuron;	
			}
		}

		#endregion
	}
}
