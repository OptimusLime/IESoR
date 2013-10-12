//simply contains a function that outputs a d3 window object
//pass in container name, then pass in window object, and it draws it!

var d3WindowGraph = function(svgContainer, width, height)
{
    //derrrr

    //width/height spec'ed let's make a baby

    var self = this;

    //we need to make some scaling attributes

    self.padding = 10;

    if(svgContainer.indexOf('#') === -1)
        svgContainer = '#' + svgContainer;

    self.svg = d3.select(svgContainer).append("svg")
        .attr("width", width)
        .attr("height", height)
        .style("margin", 20);



    var setScaleAndLineDrawing = function(data)
    {
        //here we create scales for drawing to our little window
        self.xScale = d3.scale.linear()
            .domain([0, data.length])
            .range([self.padding, width-self.padding]);

        var yMin = d3.min(data, function(d){return d;})
        var yMax = d3.max(data, function(d){return d;})

        self.yScale = d3.scale.linear()
            .domain([yMin, yMax])
            .range([self.padding, height-self.padding]);

        self.d3Line = d3.svg.line()
            .x(function(d, i) { return self.xScale(i); })
            .y(function(d, i) { return self.yScale(d); })
            .interpolate("basis");

    };


    var drawData = function(data)
    {
        var lines = self.svg.selectAll("lines")
            .data([data]);

        lines
            .enter()
            .append("path")
//                                    .data(function(d,i){return [connections[i].p1, connections[i].p2];})
            .attr("d", function(d){ return self.d3Line(d);})
            .style("stroke", function(d){ return "#812";})
            .style("fill", "#FFF")
//                                    .style("opacity", function(d){ return d.opacity;})
            .style("stroke-width", 3);

        var points = self.svg.selectAll("points")
            .data(data)
            .enter()
            .append("circle")
            .attr("r", 5)
            .attr("cx", function(d,i){ return self.xScale(i);})
            .attr("cy", function(d,i){ return self.yScale(d);})
            .style("fill", "#128")

    };

    self.updateWindowData = function(data)
    {
        //this sets our drawing configuration for this data
        setScaleAndLineDrawing(data);

        //then this sets our actual data to be draw
        drawData(data);
    };




};
