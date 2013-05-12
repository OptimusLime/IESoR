var dcNSS = "D3CPPN";
var dcNS = namespace(dcNSS);

//initialization of our world. Clears everything pretty much
dcNS.CPPNGraph= function(elementID,  w, h, nodes, links, weights) {
//    var w = 960,
//        h = 500;

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
            foci[n.name] = {x: 0, y: quickMap(n.layer, 10)};
            layers[n.layer] = maxYLayer++;
        }
        else{
            seenLayers[n.layer]++;
            foci[n.name] = {x: seenLayers[n.layer]*w, y: quickMap(n.layer, 10)};
        }
        maxLayer =  Math.max(seenLayers[n.layer], maxLayer);
    }

    maxYLayer = Math.max(maxYLayer-1, 1);

    var maxWeight = d3.max(links, function(d){ return d.weight;});

    var force = d3.layout.force()
        .nodes(d3.values(nodes))
        .links(links)
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
        .style("fill", function(d){
            switch(d.type)
            {
                case "Hidden":
                    return "#0C6";
                case "Input":
                    return "#25F";
                case "Output":
                    return "#F41";
                case "Bias":
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
        .text(function(d) { return d.name; });

    text.append("svg:text")
        .attr("x", 8)
        .attr("y", ".31em")
        .text(function(d) { return d.name; });

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
            var focus = (foci[o.name].y === undefined ? maxYLayer : foci[o.name].y);
//            console.log('Focus:' + focus);
            o.y += ((maxYLayer - focus)/maxYLayer*h *.8 +.1*h - o.y) * k;
//            console.log('FOcus: ' + foci[o.name].y + ' calc: ' + ((maxYLayer - foci[o.name].y)/maxYLayer*h) +  ' max: ' + maxYLayer)
            o.x += (foci[o.name].x/maxLayer *.8 +.1*w - o.x) * k;
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



