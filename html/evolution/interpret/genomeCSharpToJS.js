(function(exports, selfBrowser, isBrowser){

    //if we need a node object, this is how we would do it
//    var cantorPair = isBrowser ? selfBrowser['cantorPair'] : require('../utility/cantorPair.js');
    var genomeSharpToJS = exports;

    var neatjs = isBrowser ? selfBrowser['common'] : require('neatjs');
    var cppnjs = isBrowser ? selfBrowser['common'] : require('cppn');

    var neatConnection = neatjs.loadLibraryFile('neatjs', 'neatConnection');//isBrowser ? selfBrowser['neatConnection'] : require('../neatjs/genome/neatConnection.js');

    var cppnNode = cppnjs.loadLibraryFile('cppnjs', 'cppnNode');//isBrowser ? selfBrowser['cppnNode'] : require('../neatjs/cppnjs/components/cppnNode.js');
    var neatNode = neatjs.loadLibraryFile('neatjs', 'neatNode');//isBrowser ? selfBrowser['neatNode'] : require('../neatjs/genome/neatNode.js');
    var neatGenome = neatjs.loadLibraryFile('neatjs', 'neatGenome');//isBrowser ? selfBrowser['neatGenome'] : require('../neatjs/genome/neatGenome.js');

    genomeSharpToJS.CheckDependencies = function()
    {
        //load cppnjs objects
        cppnNode = cppnjs.loadLibraryFile('cppnjs', 'cppnNode');

        //laod our neatjs objects now
        neatConnection = neatjs.loadLibraryFile('neatjs', 'neatConnection');
        neatNode = neatjs.loadLibraryFile('neatjs', 'neatNode');
        neatGenome = neatjs.loadLibraryFile('neatjs', 'neatGenome');//isBrowser ? selfBrowser['neatGenome'] : require('../neatjs/genome/neatGenome.js');

    };

    genomeSharpToJS.NeuronTypeToNodeType = function(type)
    {
        switch(type)
        {
            case "bias":
                return cppnNode.NodeType.bias;
            case "in":
                return cppnNode.NodeType.input;
            case "out":
                return cppnNode.NodeType.output;
            case "hid":
                return cppnNode.NodeType.hidden;
        }
    };

    genomeSharpToJS.ConvertCSharpToJS = function(xmlGenome)
    {

        //we need to parse through a c# version of genome, and make a js genome from it

        var aNeurons = xmlGenome['neurons']['neuron'] || xmlGenome['neurons'];
        var aConnections = xmlGenome['connections']['connection'] || xmlGenome['connections'];


        //we will use nodes and connections to make our genome
        var nodes = [], connections = [];
        var inCount = 0, outCount = 0;

        for(var i=0; i < aNeurons.length; i++)
        {
            var csNeuron = aNeurons[i];
            var jsNode = new neatNode.NeatNode(csNeuron.id, csNeuron.activationFunction, csNeuron.layer, {type: genomeSharpToJS.NeuronTypeToNodeType(csNeuron.type)});
            nodes.push(jsNode);

            if(csNeuron.type == 'in') inCount++;
            else if(csNeuron.type == 'out') outCount++;
        }

        for(var i=0; i < aConnections.length; i++)
        {
            var csConnection = aConnections[i];
            var jsConnection = new neatConnection.NeatConnection(csConnection['innov-id'], csConnection.weight, {sourceID:csConnection['src-id'], targetID: csConnection['tgt-id']});
            connections.push(jsConnection);
        }

        var ng = new neatGenome.NeatGenome(xmlGenome['id'], nodes, connections, inCount, outCount);
        ng.adaptable = (xmlGenome['adaptable'] == 'True');
        ng.modulated = (xmlGenome['modulated'] == 'True');
        ng.fitness = xmlGenome['fitness'];
        ng.realFitness = xmlGenome['realfitness'];
        ng.age = xmlGenome['age'];

        return ng;
    };

    //send in the object, and also whetehr or not this is nodejs
})(typeof exports === 'undefined'? this['genomeSharpToJS']={}: exports, this, typeof exports === 'undefined'? true : false);