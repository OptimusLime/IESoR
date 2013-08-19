(function(exports, selfBrowser, isBrowser){

    var d3Genome = exports;

    var cppnjs = isBrowser ? selfBrowser['common'] : require('cppn');
    var neatjs = isBrowser ? selfBrowser['common'] : require('neatjs');


    var cppnNode = cppnjs.loadLibraryFile('cppnjs', 'cppnNode');
    var neatGenome =  neatjs.loadLibraryFile('neatjs', 'neatGenome');
    var neatNode = neatjs.loadLibraryFile('neatjs', 'neatNode');
    var neatConnection = neatjs.loadLibraryFile('neatjs', 'neatConnection');

    d3Genome.CheckDependencies = function()
    {
        cppnNode = cppnjs.loadLibraryFile('cppnjs', 'cppnNode');
        neatGenome =  neatjs.loadLibraryFile('neatjs', 'neatGenome');
        neatNode = neatjs.loadLibraryFile('neatjs', 'neatNode');
        neatConnection = neatjs.loadLibraryFile('neatjs', 'neatConnection');
    };

    //initialization of our world. Clears everything pretty much
    d3Genome.GenomeGraph= function(elementID,  w, h, oNodes, oConnections) {

        //we'll cloned nodes and connections, and won't destroy or modify the original genome!

        var nodes = [];
        for(var i=0; i < oNodes.length; i++)
        {
            //replace our current nodes with the clones
            nodes.push(neatNode.NeatNode.Copy(oNodes[i]));
        }
//        console.log(nodes);

        var nodeLookup = neatGenome.Help.CreateGIDLookup(nodes);

        var connections = [];
        for(var i=0; i< oConnections.length; i++)
        {
            var conn = neatConnection.NeatConnection.Copy(oConnections[i]);
            conn.source = nodeLookup[conn.sourceID];
            conn.target = nodeLookup[conn.targetID];
            connections.push(conn);
        }


        var seenLayers = {};
    //    var foci = [];
        var foci = {};

        var minWeight = 1;
        var maxWeightRange = 3;
        var maxLayer = -1;
        var maxYLayer = 0;
        var layers = {};

        var quickMap = function(y, div)
        {
            if(y == 0)
                return undefined;

            return Math.log(div/y)/Math.log(2);
         };
        for(var key in nodes)
        {
            var n = nodes[key];
    //    nodes.forEach(function(n,i)s
            if(seenLayers[n.layer] === undefined)
            {
                seenLayers[n.layer] = 1;
                foci[n.gid] = {x: 0, y: quickMap(n.layer, 10)};
                layers[n.layer] = maxYLayer++;
            }
            else{
                seenLayers[n.layer]++;
                foci[n.gid] = {x: seenLayers[n.layer]*w, y: quickMap(n.layer, 10)};
            }
            maxLayer =  Math.max(seenLayers[n.layer], maxLayer);
        }

        maxYLayer = Math.max(maxYLayer-1, 1);

        var maxWeight = d3.max(connections, function(d){ return d.weight;});

        var force = d3.layout.force()
            .nodes(nodes)
            .links(connections)
            .size([w, h])
            .linkDistance(100)
            .charge(-300)
            .gravity(0)
            .on("tick", tick)
            .start();

        var svg = d3.select(elementID).append("svg:svg")
            .attr("width", w)
            .attr("height", h);

    // Per-type markers, as they don't inherit styles.
        svg.append("svg:defs").selectAll("marker")
            .data(["Input", "Bias", "Hidden", "Output"])
            .enter().append("svg:marker")
            .attr("id", String)
            .attr("viewBox", "0 -5 10 10")
            .attr("refX", 15)
            .attr("refY", -1.5)
            .attr("markerWidth", 6)
            .attr("markerHeight", 6)
            .attr("orient", "auto")
            .append("svg:path")
            .attr("d", "M0,-5L10,0L0,5");

        var path = svg.append("svg:g").selectAll("path")
            .data(force.links())
            .enter().append("svg:path")
            .attr("class", function(d) { return "link " + "conn" })//+ d.type; })
            .attr("marker-end", function(d) { return "url(#" + "conn" + ")";})
                .style("stroke", function(d) {
                if(d.weight < 0)
                {
                    return rgbToHex(Math.floor(255* -d.weight/maxWeight), 0,0);
                }
                else
                    return rgbToHex( 0, 50, Math.floor(255* d.weight/maxWeight));
            })
            .style("stroke-width", function(d) {
                if(d.weight < 0)
                {
                    return minWeight + (maxWeightRange*-d.weight/maxWeight);
                }
                else
                    return minWeight + (maxWeightRange*d.weight/maxWeight);
            });
        // d.type + ")"; });


        var circle = svg.append("svg:g").selectAll("circle")
            .data(force.nodes())
            .enter().append("svg:circle")
            .attr("r", 6)
            .style("fill", function(d,i){
//                console.log(i);
                switch(d.nodeType)
                {
                    case cppnNode.NodeType.hidden:
                        return "#0C6";
                    case cppnNode.NodeType.input:
                        return "#25F";
                    case cppnNode.NodeType.output:
                        return "#F41";
                    case cppnNode.NodeType.bias:
                        return "#FFF";
                }
            })
            .call(force.drag);

        var text = svg.append("svg:g").selectAll("g")
            .data(force.nodes())
            .enter().append("svg:g");

    // A copy of the text with a thick white stroke for legibility.
        text.append("svg:text")
            .attr("x", 8)
            .attr("y", ".31em")
            .attr("class", "shadow")
            .text(function(d) { return d.gid; });

        text.append("svg:text")
            .attr("x", 8)
            .attr("y", ".31em")
            .text(function(d) { return d.gid; });

        var singleRestart = false;

    // Use elliptical arc path segments to doubly-encode directionality.
        function tick(e) {

            if(!singleRestart && e.alpha < .003){
                force.alpha(.2);
                singleRestart = true;
            }
            // Push nodes toward their designated focus.
            var k = .3;// * e.alpha;
    //        nodes.forEach(function(o, i) {
            for(var key in nodes)
            {
                var o = nodes[key];
                var focus = (foci[o.gid].y === undefined ? maxYLayer : foci[o.gid].y);
    //            console.log('Focus:' + focus);
                o.y += ((maxYLayer - focus)/maxYLayer*h *.8 +.1*h - o.y) * k;
    //            console.log('FOcus: ' + foci[o.gid].y + ' calc: ' + ((maxYLayer - foci[o.gid].y)/maxYLayer*h) +  ' max: ' + maxYLayer)
                o.x += (foci[o.gid].x/maxLayer *.8 +.1*w - o.x) * k;
            }
    //    );

            circle.attr("transform", function(d) {
                return "translate(" + d.x + "," + d.y + ")";
            });
    //

    //        svg.selectAll("circle.node")
    //            .attr("cx", function(d) { return d.x; })
    //            .attr("cy", function(d) { return d.y; });

            path.attr("d", function(d) {
                var dx = d.target.x - d.source.x,
                    dy = d.target.y - d.source.y,
                    dr = Math.sqrt(dx * dx + dy * dy);
                return "M" + d.source.x + "," + d.source.y + "A" + dr + "," + dr + " 0 0,1 " + d.target.x + "," + d.target.y;
            });
    //
    //        circle.attr("transform", function(d) {
    //            return "translate(" + d.x + "," + d.y + ")";
    //        });
    //
            text.attr("transform", function(d) {
                return "translate(" + d.x + "," + d.y + ")";
            });
        }

    };




    //send in the object, and also whetehr or not this is nodejs
})(typeof exports === 'undefined'? this['d3Genome']={}: exports, this, typeof exports === 'undefined'? true : false);