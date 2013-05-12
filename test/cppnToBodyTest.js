var assert = require('assert');
var should = require('should');
var fs = require('fs');

var utilities = require('../evolution/neatjs/cppnjs/utility/utilities.js');

var neatNode = require('../evolution/neatjs/genome/neatNode.js');
var neatConnection = require('../evolution/neatjs/genome/neatConnection.js');
var neatGenome = require('../evolution/neatjs/genome/neatGenome.js');

var cppnToBody = require('../evolution/interpret/cppnToBody.js');


var cppns = require('../evolution/neatjs/cppnjs/cppns/cppn.js');
var cppnConnection = require('../evolution/neatjs/cppnjs/components/cppnConnection.js');
var cppnNode = require('../evolution/neatjs/cppnjs/components/cppnNode.js');
var cppnActivationFactory = require('../evolution/neatjs/cppnjs/activationFunctions/cppnActivationFactory.js');

describe('Testing cppnToBody functions against the known working C# version',function(){

    it('cppnToBody.CPPNToBody(): testing conversion from genome to body', function(done){

        fs.readFile(__dirname  + '/testgenomebodies.json', 'utf8', function (err,data) {
            if (err) {
                console.log(err);
                throw err;
            }
            //we need to parse the data, and create some cppns!
            var ensureDoubleArray = function(obj, x, y)
            {
                if (obj[x] === undefined){
                    obj[x] = {};
                }

                if(obj[x][y] === undefined){
                    obj[x][y] = obj.count;
                    obj.count++;
//                console.log('x: ' + x + ' y: ' + y + ' obj: ' + obj[x][y]);
                }
            };

            var dataObject = JSON.parse(data);

            var weightRange = dataObject['meta'].weightRange;

            var testGenomes  = dataObject['genomes'];
            for(var i=0; i < testGenomes.length; i++)
            {
                //grab our network, we'll need to parse
                var genomeJSON = testGenomes[i];

                var genomeObject = genomeJSON['genome'];

                var ins = genomeObject.InputNeuronCount;
                var outs = genomeObject.OutputNeuronCount;

//
//                console.log("Nglist: ");
//                console.log(genomeObject.NeuronGeneList);

                var nodes = [];
                for(var n =0; n < genomeObject.NeuronGeneList.length; n++)
                {

                    var gObj = genomeObject.NeuronGeneList[n];
                    var type;
                    switch(gObj.NeuronType)
                    {
                        case 0: type = cppnNode.NodeType.input; break;
                        case 1: type = cppnNode.NodeType.bias; break;
                        case 2: type = cppnNode.NodeType.hidden; break;
                        case 3: type = cppnNode.NodeType.output; break;
                        case 4: type = cppnNode.NodeType.other; break;
                    }

                    var node = new neatNode.NeatNode(gObj.InnovationId, gObj.ActivationFunction.FunctionId, gObj.Layer, {type: type});
                    nodes.push(node);

                    node.gid.should.equal(gObj.InnovationId);
                    node.activationFunction.should.equal(gObj.ActivationFunction.FunctionId);
                    node.layer.should.equal(gObj.Layer);
                    node.type.should.equal(type);
                }

//                console.log("NodeList: ");
//                console.log(nodes);

                var connections = [];
                //turn our connections into actual connections!
                for(var c=0; c< genomeObject.ConnectionGeneList.length; c++)
                {
                    var gObj = genomeObject.ConnectionGeneList[c];
                    var connection = new neatConnection.NeatConnection(gObj.InnovationId, gObj.Weight, {sourceID: gObj.SourceNeuronId, targetID: gObj.TargetNeuronId});
                    connections.push(connection);

                    connection.gid.should.equal(gObj.InnovationId);
                    connection.weight.should.equal(gObj.Weight);
                    connection.sourceID.should.equal(gObj.SourceNeuronId);
                    connection.targetID.should.equal(gObj.TargetNeuronId);
                }

//                console.log("connectionsList: ");
//                console.log(connections);

                //now we're ready to make a genome
                var genome = new neatGenome.NeatGenome(genomeObject.GenomeId, nodes, connections, ins , outs , false);

                genome.localID.should.equal(genomeObject.GenomeId);
                genome.inputNodeCount.should.equal(genomeObject.InputNeuronCount);
                genome.outputNodeCount.should.equal(genomeObject.OutputNeuronCount);
                (typeof genome.localID).should.equal('number');
                (typeof genome.inputNodeCount).should.equal('number');
                (typeof genome.outputNodeCount).should.equal('number');

                //now we take our genome and decode into cppn, and check against the decoding process
                var cppn = genome.networkDecode();

                //let us examine our cppn, versus the given cppn
                var networkObject = genomeJSON['network'];

                cppn.inputNeuronCount.should.equal(networkObject.InputNeuronCount);
                cppn.outputNeuronCount.should.equal(networkObject.OutputNeuronCount);
                cppn.totalNeuronCount.should.equal(networkObject.TotalNeuronCount);
                cppn.connections.length.should.equal(networkObject.connections.length);
                cppn.activationFunctions.length.should.equal(networkObject.activationFunctions.length);
                //same counts should be true, lets test that the same information is present as well

                for(var c=0; c< cppn.connections.length; c++)
                {
                    //check our connections yo
                    var cConn = cppn.connections[c];
                    var noConn = networkObject.connections[c];

                    parseFloat(cConn.weight.toFixed(3)).should.equal(parseFloat(noConn.weight.toFixed(3)));
                    cConn.sourceIdx.should.equal(noConn.sourceNeuronIdx);
                    cConn.targetIdx.should.equal(noConn.targetNeuronIdx);
                    cConn.a.should.equal(noConn.A);
                    cConn.b.should.equal(noConn.B);
                    cConn.c.should.equal(noConn.C);
                    cConn.d.should.equal(noConn.D);
                }

                for(var af=0; af< cppn.activationFunctions.length; af++)
                {
                    cppn.activationFunctions[af].functionID.should.equal(networkObject.activationFunctions[af].FunctionId);
                }

                //now that we've ensured identical networks, lets convert from a CPPN to a body, and test that conversion
                //we also are testing, so we grab more info (and pass in boolean)
                var bodyObject = genomeJSON['body'];
                var bodyIsEmpty = genomeJSON['isEmpty'] == 'True';

                var creature = cppnToBody.CPPNToBody(cppn, bodyObject.useLEO, weightRange, true);

                creature.beforeConnection.should.equal(bodyObject.BeforeConnection);
                creature.beforeNeuron.should.equal(bodyObject.BeforeNeuron);
//                console.log(' before conn: ' + creature.beforeConnection + ' before neur: ' + creature.beforeNeuron);
//                console.log(' want after conn: ' + creature.connections.length + ' want after neur: ' + creature.hiddenLocations.count);
//                console.log(' actual after conn: ' + bodyObject.Connections.length + ' actual after neur: ' + bodyObject.HiddenLocations.length);
                creature.connections.length.should.equal(bodyObject.Connections.length);

                //if we get no errors, we proceed to check bodies against bodies!
//                Things to check
//                connections : connections,
//                hiddenLocations : hiddenNeurons,
//                inputLocations : inputs,
//                useLEO : useLeo,
//                isEmpty: isEmpty


                //check our connections against known connections
                for(var b=0; b< creature.connections.length; b++)
                {
                    var cConn  = creature.connections[b];
                    var objConn  = bodyObject.Connections[b];

                    parseFloat(cConn.weight.toFixed(3)).should.equal(parseFloat(objConn.Weight.toFixed(3)));
                    if(cConn.sourceID != objConn.SourceNeuronId){
                        console.log(cConn);
                        console.log(objConn);
                    }
                    cConn.sourceID.should.equal(objConn.SourceNeuronId);
                    cConn.targetID.should.equal(objConn.TargetNeuronId);
                }

                creature.hiddenLocations.count.should.equal(bodyObject.HiddenLocations.length);
                creature.inputLocations.length.should.equal(bodyObject.InputLocations.length);
                creature.useLEO.should.equal(bodyObject.useLEO);
                creature.isEmpty.should.equal(bodyIsEmpty);

                //check our hidden locations against known hiddens
                //we need to invert and check!
                var invertedHidden = {};
                for(var key in creature.hiddenLocations)
                {
                    if(key != 'count')
                    {
                        for(var innerKey in creature.hiddenLocations[key])
                        {
                            invertedHidden[creature.hiddenLocations[key][innerKey]] = {x: parseFloat(key), y:parseFloat(innerKey)};
                        }
                    }
                }

                for(var h=0; h< creature.hiddenLocations.count; h++)
                {
                    invertedHidden[h].x.should.equal(bodyObject.HiddenLocations[h].X);
                    invertedHidden[h].y.should.equal(bodyObject.HiddenLocations[h].Y);
                }


//                creature.allBodyOutputs.length.should.equal(bodyObject.AllBodyOutputs.length);
//                for(var b=0; b< creature.allBodyOutputs.length; b++)
//                {
//                    var outs = creature.allBodyOutputs[b];
//
//                    //leo outputs should be identical, without rounding
//                    outs[1].should.equal(bodyObject.AllBodyOutputs[b][1]);
//
////                    console.log('outs: ' + outs.length);
//                    for(var o=0; o < outs.length; o++)
//                    {
//                        parseFloat(outs[o].toFixed(3)).should.equal(parseFloat(bodyObject.AllBodyOutputs[b][o].toFixed(3)));
//                    }
//                }


//                var buildNeurons = {count:0};
//                for(var bn =0; bn < bodyObject.PreHiddenLocations.length; bn++)
//                {
//                    ensureDoubleArray(buildNeurons, bodyObject.PreHiddenLocations[bn].X,bodyObject.PreHiddenLocations[bn].Y);
//                }
//
////                console.log(creature.preHiddenLocations);
//                for(var nKey in creature.preHiddenLocations)
//                {
//                    if(nKey != 'count')
//                    {
//                        for(var innerKey in creature.preHiddenLocations[nKey])
//                        {
//                            console.log('Keys: ' + nKey + ' inner: ' + innerKey);
//                            console.log('Creature: ' + creature.preHiddenLocations[nKey][innerKey] + ' build: '+ buildNeurons[nKey][innerKey]);
//                            creature.preHiddenLocations[nKey][innerKey].should.equal(buildNeurons[nKey][innerKey]);
//                        }
//                    }
//                }
                //AllBodyOutputs

            }
            done();

        });

    });

});