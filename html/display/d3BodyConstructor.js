//Handles constructing objects on a grid
//optional genometype construction as well
(function(exports, selfBrowser, isBrowser){

    //if we need a node object, this is how we would do it
//    var cantorPair = isBrowser ? selfBrowser['cantorPair'] : require('../utility/cantorPair.js');
    var d3BodyConstructor = exports;


    var neatjs = isBrowser ? selfBrowser['common'] : require('neatjs');

    var neatGenome = neatjs.loadLibraryFile('neatjs', 'neatGenome');

    d3BodyConstructor.CheckDependencies = function()
    {
        //double check our neatjs objects now
        neatGenome = neatjs.loadLibraryFile('neatjs', 'neatGenome');
    };

    d3BodyConstructor.d3Grid = function(constructPath, d3ID, width, height, readyCallback)
    {

        if(!isBrowser)
         throw new Error("Not setup for nodejs loading!");

        var self = this;

        //set up everything inside d3
        self.radius = 5;
        self.padding = 1.5*self.radius;

        if(d3ID.indexOf('#') === -1)
            d3ID = '#' + d3ID;

        self.svg = d3.select(d3ID).append("svg")
            .attr("width", width)
            .attr("height", height)
            .style("margin", 20);



        self.selectedPoints = [];
        self.connections = [];

        self.lineDuration = 125;
        self.animationSpeed = 125;
        self.circleSpeed = 125;


        //go get our stuff!
        $.getJSON(constructPath, function(bodyConstructs)
        {

            self.savedBodyConstructs = bodyConstructs;
            //set up our object, then we get going
            var gridPoints = [];
            for(var ix in bodyConstructs.grid){
                var gp = bodyConstructs.grid[ix];
                gridPoints.push({x:gp.X, y:gp.Y});
            }

            var xMin = d3.min(gridPoints, function(d){return d.x;})
            var xMax = d3.max(gridPoints, function(d){return d.x;})

            self.xScale = d3.scale.linear()
                .domain([xMin, xMax])
                .range([self.padding, width-self.padding]);

            var yMin = d3.min(gridPoints, function(d){return d.y;})
            var yMax = d3.max(gridPoints, function(d){return d.y;})

            self.yScale = d3.scale.linear()
                .domain([yMin, yMax])
                .range([self.padding, height-self.padding]);

            //save our grid points
            self.gridPoints = gridPoints;

            //default circle colors
            self.circles = self.svg.selectAll("circles")
                .data(gridPoints)
                .enter()
                .append("circle")
                .style("fill", "steelblue")
                .attr("cx", function(d){return self.xScale(d.x);})
                .attr("cy", function(d){return self.yScale(d.y);})
                .attr("r", self.radius);

            self.d3Line = d3.svg.line()
                .x(function(d) { return self.xScale(d.x); })
                .y(function(d) { return self.yScale(d.y); })
                .interpolate("basis");



            readyCallback();
        });

    };

    d3BodyConstructor.d3Grid.prototype.pointsEqual = function(p1,p2)
    {
        return p1.x == p2.x && p1.y == p2.y;
    };

    d3BodyConstructor.d3Grid.prototype.isSelected = function(d, selectedPoints)
    {
        for(var i=0; i < selectedPoints.length; i++)
            if(this.pointsEqual(d, selectedPoints[i]))
                return true;

        return false;
    };

    d3BodyConstructor.d3Grid.prototype.pauseAnimation = function()
    {
        this.pause = true;
    };
    d3BodyConstructor.d3Grid.prototype.resetAnimation = function(startIx)
    {
        this.inputIx = startIx || 0;
        this.connections = [];

        //update yo! We want a reset!
        this.updateGridDrawing();
        this.redrawLines(this.connections);
    };

    d3BodyConstructor.d3Grid.prototype.fullBodyAnimation = function(gid, options)
    {
        var self = this;

        options = options || {};

        self.setBodyToConstruct(gid, options.startIx);

        self.pause = false;

        //call in timeout mode -- don't block "thread"
        setTimeout(self.animateTillComplete.call(self, options), 0);
    };

    d3BodyConstructor.d3Grid.prototype.animateTillComplete = function(options)
    {
        var self = this;

        //stop the animation here
        if(self.pause){
            //undo pause action
            self.pause = false;
            //but we don't call anything after pause
            return;
        }

        //do a single step in the animation
        self.singleStepConstruction();

        //process the animation speed changes
        if(options.increaseSpeed)
        {
            self.animationSpeed*=.995;
            self.circleSpeed*=.995;
        }

        if(self.inputIx <= self.inputs.length)
            setTimeout(function(){self.animateTillComplete(options);}, self.animationSpeed);
    };

    d3BodyConstructor.d3Grid.prototype.processBodyConstructs = function(bodyConstructs)
    {
        var self = this;
        self.constructObjects = {};
        for(var gid in bodyConstructs.genomes)
        {
            var builtBody = bodyConstructs.genomes[gid];

            var constructObject= {};

            var allBodyInputs = [];

            for(var ix in builtBody.AllBodyInputs)
            {
                var ip = builtBody.AllBodyInputs[ix];
                allBodyInputs.push({p1: {x: ip.Key.X, y: ip.Key.Y}, p2: {x: ip.Value.X, y: ip.Value.Y}});
            }

            //not sure if this needs to be processed at all
            var allBodyOuputs = [];
            for(var ix =0; ix < builtBody.AllBodyOuputs; ix++)
            {
                var outArray = builtBody.AllBodyOuputs[ix];
                allBodyOuputs.push(outArray);
            }

            //inputs.outputs.ixToConnection setting here
            constructObject.outputs = allBodyOuputs;
            constructObject.inputs = allBodyInputs;
            constructObject.ixToConnection = builtBody.indexToConnection;

            constructObject.creatureConnections = [];
            for(var c=0; c < builtBody.Connections.length; c++)
            {
                builtBody.Connections[c].gid = builtBody.Connections[c].InnovationId;
                constructObject.creatureConnections.push(builtBody.Connections[c]);
            }

            //save the object
            self.constructObjects[gid] = constructObject;
        }
    };
    d3BodyConstructor.d3Grid.prototype.setBodyToConstruct = function(gid, startIndex)
    {
        var self = this;
        if(!self.constructObjects)
        {
            //process the construct objects
            self.processBodyConstructs(self.savedBodyConstructs);
        }

        self.inputs = self.constructObjects[gid].inputs;
        self.outputs = self.constructObjects[gid].outputs;
        self.ixToConnection = self.constructObjects[gid].ixToConnection;
        self.creatureConnections =  self.constructObjects[gid].creatureConnections;

        //prep connection mapping
        self.connectionLookup = neatGenome.Help.CreateGIDLookup(self.creatureConnections);

        //set start point
        self.inputIx = startIndex || 0;

        self.connections = [];
        //do something else? Like prep the graph object

    };

    d3BodyConstructor.d3Grid.prototype.updateGridDrawing = function()
    {
        var self = this;
        self.svg.selectAll("circle")
            .transition()
            .duration(self.circleSpeed)
            .style("fill", function(d){
                return self.isSelected(d, self.selectedPoints) ? "red" : "steelblue";
            })
            .attr("cx", function(d){return self.xScale(d.x);})
            .attr("cy", function(d){return self.yScale(d.y);})
            .attr("r", function(d){
                return self.isSelected(d, self.selectedPoints) ? self.radius*1.5 : self.radius;
            });
    };


    d3BodyConstructor.d3Grid.prototype.singleStepConstruction = function()
    {
        var self = this;

        //can't operate if we're greater than the length of our inputs
        if(self.inputIx > self.inputs.length)
            return;

        //end with nuffin
        if(self.inputIx == self.inputs.length)
            self.selectedPoints = [];
        else
            self.selectedPoints = [self.inputs[self.inputIx].p1, self.inputs[self.inputIx].p2];

        //now that selected points are set, update the grid
        self.updateGridDrawing();


        //we need to check if we have a connection at these locations
        if(self.ixToConnection[self.inputIx] !== undefined)
        {
            var cID = self.ixToConnection[self.inputIx];

            //check for gid of connection
            var finalConnection = self.connectionLookup[cID];

            //we have a connection!
            if(finalConnection !== undefined){
                self.connections.push({p1: self.inputs[self.inputIx].p1, p2: self.inputs[self.inputIx].p2, color: finalConnection ? "purple" : "black", opacity: finalConnection ? 1 : .1});

                //connection added, need to redraw lines
                self.redrawLines(self.connections);
            }
        }

        //step it. up.
        self.inputIx++;
    };


    d3BodyConstructor.d3Grid.prototype.redrawLines = function(conns)
    {
        var self = this;

            var lines = self.svg.selectAll("lines")
                .data(conns);

            lines
                .enter()
                .append("path")
//                                    .data(function(d,i){return [connections[i].p1, connections[i].p2];})
                .attr("d", function(d){ return self.d3Line([d.p1, d.p2]);})
                .style("stroke", function(d){ return d.color;})
//                                    .style("opacity", function(d){ return d.opacity;})
                .style("stroke-width", 3);
//                                    .style("stroke-linecap", "round")
//                                    .attr("x1", function(d) {return xScale((d.p1.x + d.p2.x)/2);})
//                                    .attr("y1", function(d) {return yScale((d.p1.y + d.p2.y)/2);})
//                                    .attr("x2", function(d) {return xScale((d.p1.x + d.p2.x)/2);})
//                                    .attr("y2", function(d) {return yScale((d.p1.y + d.p2.y)/2);})
//                                    .transition()
//                                    .duration(lineDuration)
////                                    .style("opacity", function(d){ return d.opacity;})
//                                    .style("stroke", function(d){ return d.color;})
//                                    .attr("x1", function(d) {return xScale(d.p1.x);})
//                                    .attr("y1", function(d) {return yScale(d.p1.y);})
//                                    .attr("x2", function(d) {return xScale(d.p2.x);})
//                                    .attr("y2", function(d) {return yScale(d.p2.y);});


    };




//send in the object, and also whetehr or not this is nodejs
})(typeof exports === 'undefined'? this['d3BodyConstructor']={}: exports, this, typeof exports === 'undefined'? true : false);