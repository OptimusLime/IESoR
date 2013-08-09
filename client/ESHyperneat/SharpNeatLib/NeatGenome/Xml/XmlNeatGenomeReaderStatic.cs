using System;
using System.Collections.Generic;
using System.Xml;
using SharpNeatLib.NeuralNetwork;
using SharpNeatLib.Xml;

namespace SharpNeatLib.NeatGenome.Xml
{
	public class XmlNeatGenomeReaderStatic
	{
		public static NeatGenome Read(XmlDocument doc)
		{
			XmlElement xmlGenome = (XmlElement)doc.SelectSingleNode("genome");
			if(xmlGenome==null)
				throw new Exception("The genome XML is missing the root 'genome' element.");

			return Read(xmlGenome);
		}

		public static NeatGenome Read(XmlElement xmlGenome)
		{
			int inputNeuronCount=0;
			int outputNeuronCount=0;

            long id = long.Parse(XmlUtilities.GetAttributeValue(xmlGenome, "id"));

			//--- Read neuron genes into a list.
			NeuronGeneList neuronGeneList = new NeuronGeneList();
			XmlNodeList listNeuronGenes = xmlGenome.SelectNodes("neurons/neuron");
			foreach(XmlElement xmlNeuronGene in listNeuronGenes)
			{
				NeuronGene neuronGene = ReadNeuronGene(xmlNeuronGene);

				// Count the input and output neurons as we go.
				switch(neuronGene.NeuronType)
				{
					case NeuronType.Input:
						inputNeuronCount++;
						break;
					case NeuronType.Output:
						outputNeuronCount++;
						break;
				}

				neuronGeneList.Add(neuronGene);
			}

            //--- Read module genes into a list.
            List<ModuleGene> moduleGeneList = new List<ModuleGene>();
            XmlNodeList listModuleGenes = xmlGenome.SelectNodes("modules/module");
            foreach (XmlElement xmlModuleGene in listModuleGenes) {
                moduleGeneList.Add(ReadModuleGene(xmlModuleGene));
            }

			//--- Read connection genes into a list.
			ConnectionGeneList connectionGeneList = new ConnectionGeneList();
			XmlNodeList listConnectionGenes = xmlGenome.SelectNodes("connections/connection");
			foreach(XmlElement xmlConnectionGene in listConnectionGenes)
				connectionGeneList.Add(ReadConnectionGene(xmlConnectionGene));
			
			//return new NeatGenome(id, neuronGeneList, connectionGeneList, inputNeuronCount, outputNeuronCount);
            return new NeatGenome(id, neuronGeneList, moduleGeneList, connectionGeneList, inputNeuronCount, outputNeuronCount);
		}

		private static NeuronGene ReadNeuronGene(XmlElement xmlNeuronGene)
		{
            long id = long.Parse(XmlUtilities.GetAttributeValue(xmlNeuronGene, "id"));
			NeuronType neuronType = XmlUtilities.GetNeuronType(XmlUtilities.GetAttributeValue(xmlNeuronGene, "type"));
            string activationFn = XmlUtilities.GetAttributeValue(xmlNeuronGene, "activationFunction");
            double layer = double.Parse(XmlUtilities.GetAttributeValue(xmlNeuronGene, "layer"));

			return new NeuronGene(null, id, layer, neuronType, ActivationFunctionFactory.GetActivationFunction(activationFn));	
		}

        private static ModuleGene ReadModuleGene(XmlElement xmlModuleGene)
        {
            long id = long.Parse(XmlUtilities.GetAttributeValue(xmlModuleGene, "id"));
            string function = XmlUtilities.GetAttributeValue(xmlModuleGene, "function");

            XmlNodeList inputNodes = xmlModuleGene.GetElementsByTagName("input");
            long[] inputs = new long[inputNodes.Count];
            foreach (XmlNode inp in inputNodes) {
                inputs[int.Parse(XmlUtilities.GetAttributeValue(inp, "order"))] = long.Parse(XmlUtilities.GetAttributeValue(inp, "id"));
            }

            XmlNodeList outputNodes = xmlModuleGene.GetElementsByTagName("output");
            long[] outputs = new long[outputNodes.Count];
            foreach (XmlNode outp in outputNodes) {
                outputs[int.Parse(XmlUtilities.GetAttributeValue(outp, "order"))] = long.Parse(XmlUtilities.GetAttributeValue(outp, "id"));
            }

            return new ModuleGene(id, ModuleFactory.GetByName(function), new List<long>(inputs), new List<long>(outputs));
        }

        private static ConnectionGene ReadConnectionGene(XmlElement xmlConnectionGene)
		{
            long innovationId = long.Parse(XmlUtilities.GetAttributeValue(xmlConnectionGene, "innov-id"));
            long sourceNeuronId = long.Parse(XmlUtilities.GetAttributeValue(xmlConnectionGene, "src-id"));
            long targetNeuronId = long.Parse(XmlUtilities.GetAttributeValue(xmlConnectionGene, "tgt-id"));
			double weight = double.Parse(XmlUtilities.GetAttributeValue(xmlConnectionGene, "weight"));
	
			return new ConnectionGene(innovationId, sourceNeuronId, targetNeuronId, weight);
		}
	}
}
