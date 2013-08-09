var graphNSS = "D3GraphSpace";
var graphNS = namespace(graphNSS);


//initialization of our world. Clears everything pretty much
graphNS.WINGraph= function(elementID,  w, h, setGenomeBodyInViewer, marg) {


    var self = this;
    var elName = elementID;
    var setupCount = 0;

    var mainSVG = "mainSVG-" + elName;

    var sWidth = w;
    var sHeight = h;

    var margin = marg || {top: 20, right: 20, bottom: 30, left: 40};

    var width, height, x, fitness, y, color, xAxis, yAxis, svg;

    var legend;

    self.dataObjects = {};
    self.cachedObjects = {};
    self.lastSelectedIx = 0;

    var addGenomeToView = setGenomeBodyInViewer;

    var setupSVG = function()
    {
            width = sWidth - margin.left - margin.right,
            height = sHeight - margin.top - margin.bottom;

        x = d3.scale.linear()
            .range([0, width]);

        fitness = d3.scale.linear()
            .range([3, 10]);

        y = d3.scale.linear()
            .range([height, 0]);

        if(self.dataObjects){
            x.domain(d3.extent(self.dataObjects, function(d) { return d.x; })).nice();
            y.domain(d3.extent(self.dataObjects, function(d) { return d.y; })).nice();
        }

        color = d3.scale.category10();

        xAxis = d3.svg.axis()
            .scale(x)
            .orient("bottom")
            .ticks(25);

        yAxis = d3.svg.axis()
            .scale(y)
            .orient("left")
            .ticks(25);

        svg = d3.select(elName).append("svg")//insert("svg", ":first-child")
            .attr("width", width + margin.left + margin.right)
            .attr("height", height + margin.top + margin.bottom)
            .attr("class", "svgClass")
            .attr("id", mainSVG)
            .append("g")
            .attr("transform", "translate(" + margin.left + "," + margin.top + ")");

    };

    //setup on class creation
    setupSVG();

    self.rescaleData = function(data)
    {
        x.domain(d3.extent(data, function(d) { return d.x; })).nice();
        y.domain(d3.extent(data, function(d) { return d.y; })).nice();
        fitness.domain(d3.extent(data, function(d) { return d.absoluteFitness; })).nice();

        xAxis = d3.svg.axis()
            .scale(x)
            .orient("bottom")
            .ticks(25);

        yAxis = d3.svg.axis()
            .scale(y)
            .orient("left")
            .ticks(25);
    };

    self.enterDataD3 = function(data, finished)
    {

        //should already be converted into dictionary/points

        //data has 3 params
        //uid, x, y
//        var $container = $(htmlID);
        console.log('Setting up d3');

        svg.append("g")
            .attr("class", "x axis")
            .attr("transform", "translate(0," + height + ")")
            .call(xAxis)
            .append("text")
            .attr("class", "label")
            .attr("x", width)
            .attr("y", -6)
            .style("text-anchor", "end")
            .style("fill", "#DDD");
//                .text("Sepal Width (cm)");


        svg.append("g")
            .attr("class", "y axis")
            .call(yAxis)
            .append("text")
            .attr("class", "label")
            .attr("transform", "rotate(-90)")
            .attr("y", 6)
            .attr("dy", ".71em")
            .style("text-anchor", "end")
            .style("fill", "#DDD");
//                .text("Sepal Length (cm)")


        svg.selectAll(".dot")
            .data(data)
            .enter().append("circle")
            .attr("class", "dot")
            .attr("id", function(d,i){ return "dot-" + i;})
            .attr("r", function(d){
                //scale based on the fitness ranges!
                return fitness(d.absoluteFitness);
            })
            .attr("cx", function(d) {
//                    console.log('Dealing with d'); console.log(d);
                return x(d.x); })
            .attr("cy", function(d) { return y(d.y); })
            .style("fill", "#DDD")
            .on('click',function(d, i){
                //check if we have the object cached so far


                d3.select("#dot-" + self.lastSelectedIx).style("fill", "#DDD");

                if(self.cachedObjects[d.uid])
                {

                    d3.select("#dot-" + i).style("fill", "#02D");
                    self.lastSelectedIx = i;

                    addGenomeToView(d.uid, d.absoluteFitness);

                }

            })
            .on('mouseover', function(d,i) {

                if(i != self.lastSelectedIx)
                    d3.select("#dot-" + i).style("fill", "#D20");

            })
            .on('mouseout', function(d,i) {
                if(i != self.lastSelectedIx)
                    d3.select("#dot-" + i).style("fill", "#DDD");
            });
//                        .style("fill", function(d) { return color(d.uid % 10); });

        legend = svg.selectAll(".legend")
            .data(color.domain())
            .enter().append("g")
            .attr("class", "legend")
            .attr("transform", function(d, i) { return "translate(0," + i * 20 + ")"; })


        legend.append("rect")
            .attr("x", width - 18)
            .attr("width", 18)
            .attr("height", 18);

        legend.append("text")
            .attr("x", width - 24)
            .attr("y", 9)
            .attr("dy", ".35em")
            .style("text-anchor", "end")
            .text(function(d) { return d; });


        svg.selectAll('.x.axis text').attr("class", "axisLabel");
        svg.selectAll('.y.axis text').attr("class", "axisLabel");


        if(finished)
            finished();

//        $('<div id="pcaViewer"></div>').insertAfter(('.svgClass' + (setupCount-1)));


//                    var divID = divIDFromGenome(genomeObject.GenomeID);
//                    var id =  canvasIDFromGenome(genomeObject.GenomeID);


//                    $container.append(smallNS.smallWorldHtmlString(divID, id, 230,230));

//                    var smallWorld = new smallNS.SmallWorld(id, 230, 230, 14, false);

//                    var smallWorld = addGenomeObjectDiv(genomeObject, '#container');

//                    smallWorld.addJSONBody(genomeObject);

    };



};

graphNS.WINGraph.prototype.loadCachedBodies = function(bodies)
{
    var self = this;
    for(var gid in bodies){
        self.cachedObjects[gid] = (typeof bodies[gid] === 'string' ? JSON.parse(bodies[gid]) : bodies[gid]);
    }
};

graphNS.WINGraph.prototype.fetchCache = function(data, fetchGenomeFunction)
{
    var self = this;
    self.dataObjects = data;

    var bodyRequest = [];
    for(var i=0; i < data.length; i++)
    {
        bodyRequest.push(data[i].uid);
    }
    fetchGenomeFunction(bodyRequest, function(bodies){

            for(var gid in bodies){
                self.cachedObjects[gid] = JSON.parse(bodies[gid]);
            }

            console.log('Cached returned bodies');

        },
        function(error)
        {
            console.log('Could not get bodies from PCA request');
            console.log(error);
        }
    );
};


//graphNS.WINGraph.prototype

