var assert = require('assert');
var should = require('should');
var fs = require('fs');

var utilities = require('../evolution/neatjs/cppnjs/utility/utilities.js');
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

            var dataObject = JSON.parse(data);

            var testGenomes  = dataObject['genomes'];
            for(var i=0; i < testGenomes.length; i++)
            {
                //grab our network, we'll need to parse
                var genomeJSON = testGenomes[i];

                var nodesAndConnections = genomeJSON['genome'];

                var nodes = [];
                for(var n =0; n < nodesAndConnections.nodes.length; n++)
                {
                    nodes.push();
                }

                var connections = [];
                //turn our connections into actual connections!
                for(var c=0; c< nodesAndConnections.connections.length; c++)
                {
                    var loadedConn = nodesAndConnections.connections[c];
                    connections.push();
                }



            }
            done();

        });

    });

});