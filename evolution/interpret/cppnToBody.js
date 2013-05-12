(function(exports, selfBrowser, isBrowser){

    //if we need a node object, this is how we would do it
//    var cantorPair = isBrowser ? selfBrowser['cantorPair'] : require('../utility/cantorPair.js');
    var cppnToBody = exports;

    var neatConnection = isBrowser ? selfBrowser['neatConnection'] : require('../neatjs/genome/neatConnection.js');

    //convert genome to body using decoder! In case you forget
    //   INetwork net = GenomeDecoder.DecodeToModularNetwork((NeatGenome)genome);
    cppnToBody.CPPNToBody = function(cppn, useLeo, weightRange, testing)
    {

        var isEmpty = false;

        //we want the genome, so we can acknowledge the genomeID!

        //now convert a network to a set of hidden neurons and connections

        //we'll make body specific function calls later

        var inputs =[], outputs = [], hiddenNeurons = {};

        //zero out our count object :)
        hiddenNeurons.count = 0;

        var connections = [];

        //loop through a grid, defined by some resolution, and test every connection against another using leo

        var resolution = 9;
        //int resolutionHalf = resolution / 2;

        var dx = 2 / (resolution-1);
        var dy = 2 / (resolution -1);
        var fX = -1, fY = -1;

        //var threeNodeDistance = Math.sqrt(9.0 * dx * dx + 9.0 * dy * dy);
        var xDistanceThree = 3 * dx;
        var yDistanceThree = 3 * dy;

        var queryPoints = [];

        for (var x = 0; x < resolution; x++)
        {
            for (var y = 0; y < resolution; y++)
            {
                queryPoints.push({x: fX, y: fY});
                //now increment fy and go again
                fY += dy;
            }
            //increment dx, run through again
            //reset fy to -1
            fX += dx;
            fY = -1;
        }

//        console.log(queryPoints);
        var counter = 0;
        var conSourcePoints = {};//new Dictionary<long, PointF>();
        var conTargetPoints = {};//new Dictionary<long, PointF>();

        var accessDoubleArray = function(obj, xyPoint)
        {
            return obj[xyPoint.x][xyPoint.y];
        };
        var ensureDoubleArray = function(obj, x, y)
        {
            //don't use !obj[x] since 0 gets grouped into that. poop.
            if (obj[x] === undefined){
                obj[x] = {};
            }

            if(obj[x][y] === undefined){
                obj[x][y] = obj.count;
                obj.count++;
//                console.log('x: ' + x + ' y: ' + y + ' obj: ' + obj[x][y]);
            }
        };

        //Dictionary<string, List<PointF>> pointsChecked = new Dictionary<string, List<PointF>>();
        //List<PointF> pList;
        var src, tgt;
        var cnt =0;
        var allBodyOutputs = [];
        //for each points we have
        for(var p1=0; p1 < queryPoints.length; p1++)
        {
            var xyPoint = queryPoints[p1];

            //query against all other points (possibly limiting certain connection lengths
            for(var p2 = p1; p2 < queryPoints.length; p2++)
            {
                var otherPoint = queryPoints[p2];

                if (p1 != p2 && (Math.abs(xyPoint.x - otherPoint.x) < xDistanceThree && Math.abs(xyPoint.y - otherPoint.y) < yDistanceThree))
                {
                    var outs = cppnToBody.queryCPPNOutputs(cppn, xyPoint.x, xyPoint.y, otherPoint.x, otherPoint.y);//, maxXDistanceCenter(xyPoint, otherPoint),  minYDistanceGround(xyPoint, otherPoint));
                    var weight = outs[0];

                    if(testing)
                        allBodyOutputs.push(outs);

                    if (useLeo)
                    {
                        if (outs[1] > 0)
                        {
//                            console.log(outs);
//                            console.log('XYPoint: ');console.log( xyPoint);
//                            console.log('otherPoint: ');console.log( otherPoint);
                            //add to hidden neurons

                            ensureDoubleArray(hiddenNeurons, xyPoint.x, xyPoint.y);
                            src = accessDoubleArray(hiddenNeurons, xyPoint);

                            ensureDoubleArray(hiddenNeurons, otherPoint.x, otherPoint.y);
                            tgt =  accessDoubleArray(hiddenNeurons, otherPoint);

                            conSourcePoints[counter] = xyPoint;
                            conTargetPoints[counter] = otherPoint;

                            var connection = new neatConnection.NeatConnection(counter++, weight*weightRange, {sourceID:src, targetID:tgt});
                            connection.coordinates = [xyPoint.x, xyPoint.y, otherPoint.x, otherPoint.y];
                            connection.cppnOutputs = outs;

                            connections.push(connection);

                        }
                    }
                    else
                    {
                        //add to hidden neurons
                        ensureDoubleArray(hiddenNeurons, xyPoint.x, xyPoint.y);
                        src = accessDoubleArray(hiddenNeurons, xyPoint);

                        ensureDoubleArray(hiddenNeurons, otherPoint.x, otherPoint.y);
                        tgt =  accessDoubleArray(hiddenNeurons, otherPoint);

                        conSourcePoints[counter] = xyPoint;
                        conTargetPoints[counter] = otherPoint;

                        var connection = new neatConnection.NeatConnection(counter++, weight*weightRange, {sourceID:src, targetID:tgt});
                        connection.coordinates = [xyPoint.x, xyPoint.y, otherPoint.x, otherPoint.y];
                        connection.cppnOutputs = outs;

                        connections.push(connection);
                    }

                }
            }
        }

//        console.log('Counter: ' + counter);

        var connBefore = connections.length;
        var neuronBefore = hiddenNeurons.count;

//        PreHiddenLocations
//        var preNeurons = {count: 0};

//        if(testing){
//            var inverted = {};
//
//            for(var key in hiddenNeurons)
//            {
//                if(key != 'count')
//                {
//                    for(var innerKey in hiddenNeurons[key])
//                    {
//                        inverted[(hiddenNeurons[key][innerKey])] = {x: key, y: innerKey};
//                    }
//                }
//            }
//            console.log(inverted);
//
//            for(var ix =0; ix < hiddenNeurons.count; ix++)
//            {
//                console.log('ix: ' + ix + ' inv: ' + inverted[ix]);
//                var point = inverted[ix];
//                ensureDoubleArray(preNeurons, point.x, point.y);
//            }
//        }

        var rep = cppnToBody.ensureSingleConnectedStructure(connections, hiddenNeurons, conSourcePoints, conTargetPoints);

        connections = rep.connections;
        hiddenNeurons = rep.hiddenNeurons;

//        console.log('Looking at body with: ' + hiddenNeurons.count + ' conns: ' + connections.length);
        if (hiddenNeurons.count > 20 || connections.length > 100)
        {
            hiddenNeurons = {count:0};//new List<PointF>();
            connections = [];//new ConnectionGeneList();
        }


        if (hiddenNeurons.count == 0 || connections.length == 0)
            isEmpty = true;

        var esBody = {
            allBodyOutputs : allBodyOutputs,
            beforeNeuron: neuronBefore,
            beforeConnection: connBefore,
            connections : connections,
            hiddenLocations : hiddenNeurons,
            inputLocations : inputs,
            useLEO : useLeo,
            isEmpty: isEmpty
        };

        //then convert the body into JSON
//        console.log(" Nodes: " + hiddenNeurons.count + " Connections: " + connections.length);

        return esBody;
    };


    cppnToBody.queryCPPNOutputs = function(cppn, x1, y1, x2, y2)//, maxXDist, minYDist)
    {
        var coordinates = [];

        coordinates.push(x1);
        coordinates.push(y1);
        coordinates.push(x2);
        coordinates.push(y2);

//      coordinates.push(maxXDist);
//      coordinates.push(minYDist);

        cppn.clearSignals();
        cppn.setInputSignals(coordinates);
        cppn.recursiveActivation();

        var outs = [];
        for (var i = 0; i < cppn.outputNeuronCount; i++)
            outs.push(cppn.getOutputSignal(i));

        return outs;

    };




    cppnToBody.ensureSingleConnectedStructure = function(connections, hiddenNeurons, conSourcePoints, conTargetPoints)
    {
        //a list of lists
        var allChains = [];
        var maxChain = 0;

        for(var i=0; i < connections.length; i++)
        {
            var connection = connections[i];

            var isInChain = false;
            var nChain;
            //track connected structures through all the other chains
            for(var c=0; c< allChains.length; c++)
            {

                //check our chain out, does it have this neuron?
                var chain = allChains[c];

                //what's the largest chain we've seen
                maxChain = Math.max(chain.count, maxChain);

                if (chain[connection.sourceID] || chain[connection.targetID])
                {
                    nChain = chain;
                    isInChain = true;
                    break;
                }
            }

            if (!isInChain)
            {
                //chains are just objects, and we can quickly lookup if we've seen anything before
                //we do lick to keep count though!
                nChain = {count:0};
                allChains.push(nChain);
            }

            if (!nChain[connection.sourceID]){
                nChain[connection.sourceID] = true;
                nChain.count++;
            }
            if (!nChain[connection.targetID]){
                nChain[connection.targetID] = true;
                nChain.count++;
            }
        }


        //gotta find the max chain, here is our check
        //allChains.Find(chain => chain.length == maxChain);
        var finalChain;
        for(var c =0; c < allChains.length; c++)
        {
            if(allChains[c].count == maxChain)
            {
                finalChain = allChains[c];
                break;
            }
        }

        var inverseHidden = {};
        //we might adjust hidden neuron count later, make sure we keep original value
        var originalHiddenCount = hiddenNeurons.count;
        for(var key in hiddenNeurons)
        {
            if(key != 'count')
            {
                for(var innerKey in hiddenNeurons[key])
                {
                    inverseHidden[hiddenNeurons[key][innerKey]] = {x:key, y:innerKey};
                }
            }
        }



        if (finalChain && finalChain.count != 0)
        {
            var markDelete = [];
            var point;
            for(var c =0; c < connections.length; c++)
            {
                var connection = connections[c];

                var del = false;
                //if we don't have you in our chain, get rid of the object
                if (!finalChain[connection.sourceID])
                {
                    point = conSourcePoints[connection.gid];

                    //remove hidden node, friend!
                    if(hiddenNeurons[point.x][point.y] !== undefined){
                        delete hiddenNeurons[point.x][point.y];
                        hiddenNeurons.count--;
                    }

                    //hiddenNeurons.Remove(conSourcePoints[conn.InnovationId]);
                    del = true;

                }

                if (!finalChain[connection.targetID])
                {
                    point = conTargetPoints[connection.gid];

                    //remove hidden node, friend!
                    if(hiddenNeurons[point.x][point.y] !== undefined){
                        delete hiddenNeurons[point.x][point.y];
                        hiddenNeurons.count--;
                    }

                    //hiddenNeurons.Remove(conTargetPoints[conn.InnovationId]);

                    del = true;
                }

                if (del){
                    markDelete[connection.gid] = true;
                }
            }

            //let's rebuild connections, and remove any deleted objects
            var des = connections.length;
            var repConns = [];
            for(var c=0; c< connections.length; c++)
            {
                if(!markDelete[connections[c].gid])
                    repConns.push(connections[c]);
            }
            connections = repConns;
//            console.log('actual Size: ' + des + ' Desired: ' + repConns.length);
//            console.log('Connections: ');
//            console.log(repConns);

//                markDelete.ForEach(x => connections.Remove(x));
        }

        //now that we've deleted the hidden neuron objects, lets recalculate the current indices
        var nCount = 0, p;

        //we access the inverse object in order, and map to our hidden node array with deleted object
        for(var hid = 0; hid < originalHiddenCount; hid++)
        {
            if(inverseHidden[hid]){
                p = inverseHidden[hid];
                if(hiddenNeurons[p.x][p.y] != undefined)
                    hiddenNeurons[p.x][p.y] = nCount++;
            }
        }


        for(var c=0; c< connections.length; c++)
        {
            var connection = connections[c];

            //readjust connection source/target depending on hiddenNeuron array
            var point = conSourcePoints[connection.gid];
            connection.sourceID = hiddenNeurons[point.x][point.y];

            //now adjust the target!
            point = conTargetPoints[connection.gid];
            connection.targetID = hiddenNeurons[point.x][point.y];
        }

        return {connections: connections, hiddenNeurons: hiddenNeurons};
    };
    //send in the object, and also whetehr or not this is nodejs
})(typeof exports === 'undefined'? this['cppnToBody']={}: exports, this, typeof exports === 'undefined'? true : false);