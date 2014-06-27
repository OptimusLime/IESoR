//This is where we define our simple experiment, and do all our simulation logic

var smallNSS = "SmallWorld";
var smallNS = namespace(smallNSS);



smallNS.BehaviorTypes = {
    xyCenterOfMass : 0,
    xCenterOfMass : 1,
    yCenterOfMass : 2,
    heatMap10x10 : 3,
    nodeMovements : 4,
    avgXYCenterOfMass: 5,
    widthHeightMass: 6,
    widthHeightEfficiencyMass: 7,
    widthHeightMassSaveHeighestJump: 8
}

var desiredSmallRenderSpeed = 30;
var desiredSmallSimulationSpeed = 30;
var lastFPS = 1000/desiredSmallRenderSpeed;

// http://paulirish.com/2011/requestanimationframe-for-smart-animating/
window.requestAnimFrame = (function(callback){
    return  window.requestAnimationFrame       ||
        window.webkitRequestAnimationFrame ||
        window.mozRequestAnimationFrame    ||
        window.oRequestAnimationFrame      ||
        window.msRequestAnimationFrame     ||
        function(/* function */ callback, /* DOMElement */ element){
            window.setTimeout(callback, 1000/desiredSmallRenderSpeed);
        };
})();



//initialization of our world. Clears everything pretty much
smallNS.SmallWorld = function(sCanvasID, canvasWidth, canvasHeight, scale, zombieMode) {

    this.worldID = worldId + 0;

    worldId++;

    this.scale = scale;
    //make sure to save our canvasID for generating html string
    this.canvasID = sCanvasID;
    this.canvasWidth = canvasWidth;
    this.canvasHeight = canvasHeight;

    //we say what kind of behavior we want, then create that object -- should be a function in the future
    this.behaviorType = smallNS.BehaviorTypes.widthHeightMassSaveHeighestJump;
    this.behavior = {};
    this.behavior.frameCount = 0;

//    this.secondBehaviorType = smallNS.BehaviorTypes.heatMap10x10;
//    this.secondBehavior = {};

    this.initializeBehavior = function(behavior, behaviorType)
    {

        behavior.totalBehaviorFrames = 0;

        switch(behaviorType)
        {
            case smallNS.BehaviorTypes.widthHeightMass:
            case smallNS.BehaviorTypes.widthHeightEfficiencyMass:
            case smallNS.BehaviorTypes.xCenterOfMass:
            case smallNS.BehaviorTypes.yCenterOfMass:
            case smallNS.BehaviorTypes.xyCenterOfMass:
            case smallNS.BehaviorTypes.widthHeightMassSaveHeighestJump:
            case smallNS.BehaviorTypes.avgXYCenterOfMass:
                behavior.points = [];

                break;
            case smallNS.BehaviorTypes.nodeMovements:

                console.log('Making node movement map');
                behavior.heatMap = {};


                var xSides = 9;
                var ySides = 9;

                var moveDirections = 9;

                for(var x = 0; x < xSides; x++)
                {
                    behavior.heatMap[x] = {};
                    for(var y=0; y < ySides;y++)
                    {
                        behavior.heatMap[x][y] = {};
                        behavior.heatMap[x][y].bCount = 0;
                        for(var w =0; w < moveDirections; w ++)
                        {
                            behavior.heatMap[x][y][w] = 0;
                        }
                    }
                }

//            this.behavior.heatMap.fabCount = 0;

                break;

            case smallNS.BehaviorTypes.heatMap10x10:
                console.log('Making heatmap');
                behavior.heatMap = {};

                var xSides = 10;
                var ySides = 10;
                for(var x = 0; x < xSides; x++)
                {
                    behavior.heatMap[x] = {};
                    for(var y=0; y < ySides;y++)
                    {
                        behavior.heatMap[x][y]=0;
                    }
                }

                behavior.heatMap.fabCount = 0;
                break;
        }
    };

    this.initializeBehavior(this.behavior, this.behaviorType);
    if(this.secondBehavior)
    {
        this.initializeBehavior(this.secondBehavior, this.secondBehaviorType);
    }


    //measure behavior every three frames
    this.behaviorSkipFrames = 5;
    this.beginEvaluation = true;
    //go for about a second?
    this.waitToStartFrames = 60;

    //30 frames/sec, skip 3 frames, = 10 frames a second
    //50 behaviors = 5 seconds
    this.behaviorTotalCount = 75;
    this.frameCount  = 0;

    this.initialSmallState = [
        {id: "ground", x: canvasWidth / 2 / scale, y: canvasHeight / scale, halfHeight: 0.5, halfWidth: 12*canvasWidth / scale, color: 'black'}
        //,
        // {id: "ball1", x: 9, y: 2, radius: 0.5},
        // {id: "ball2", x: 11, y: 4, radius: 0.5}
    ];

    //
    this.interruptLoop = false;

    //temp hack
    this.lastObjectID = -1;
    //Set up some mouse variables
    this.canvas = document.getElementById(sCanvasID);

    /* ---- INPUT ----------------------------- */
    this.mouseX;    this.mouseY;    this.isMouseDown;


    //we grab our canvas object really
    //if we start in zombie mode, we won't process any added objects for drawing, nor do we initialize canvas
    this.drawObject = new boxNS.DrawingObject(sCanvasID, canvasWidth, canvasHeight, scale, zombieMode);

    this.drawObject.addBehavior(this.behavior, this.behaviorType);

    this.theWorld = new bHelpNS.ContainedWorld(desiredSmallSimulationSpeed, false, canvasWidth, canvasHeight, scale, 15, false,
        {object: this.drawObject, addBody: this.drawObject.addBody, removeBody: this.drawObject.removeBody,
            addJoint: this.drawObject.addJoint, removeJoint: this.drawObject.removeJoint });

    var world = {};
    for (var i = 0; i < this.initialSmallState.length; i++) {
        world[this.initialSmallState[i].id] = Entity.build(this.initialSmallState[i]);
    }
    //this will populate the bodies map -- thereby causing d3 to draw the data on screen
    this.theWorld.setBodies(world);




}

//determines the effects of structural differences in the behavior metric.
smallNS.SmallWorld.noNodeMultiplier = .2;

smallNS.smallWorldHtmlString = function(divID, canvasID, width, height)
{
    return '<div id=' + divID + ' class="element"' + 'style="width: ' + width + 'px;' + ' height: ' + height + 'px;"' + '>'
        +
        '<div><canvas id=' + canvasID + ' width=' + width + ' height=' + height + ' class="canvas"></canvas></div>' +

        '</div>';
};

smallNS.SmallWorld.prototype.draw = function() {
    //console.log("d");
    this.drawObject.drawWorld(this.theWorld.interpolation, this.theWorld.nodesCenterOfMass());
}

smallNS.SmallWorld.prototype.runSimulationForBehavior = function(props)
{
    var start = (new Date).getTime();

    var updateDeltaMS = 33.34;
    this.simulating  = true;

    var updateCount = 0;
    console.log('Eval '+ props.genomeID +' takes:');
//    console.log('Eval Start');
//    console.log('behavior begining: ' + this.behavior.frameCount);

    while(this.behavior.frameCount < this.behaviorTotalCount)
    {
        updateCount++;
       this.update(updateDeltaMS, props);
        if(updateCount > 1000){
            console.log('Making 5 hundo updates');
            updateCount = 0;
        }
    }

//    console.log('behavior complete: ' + this.behaviorTotalCount);
    this.simulating = false;

    /* Run a test. */
    var diff = (new Date).getTime() - start;

//    console.log('Eval takes: ' + diff);
    console.log(diff);

    if(this.secondBehavior)
        return {behavior: smallNS.SmallWorld.AdjustBehavior(this.behavior, this.behaviorType),
            secondBehavior: smallNS.SmallWorld.AdjustBehavior(this.secondBehavior, this.secondBehaviorType)};
    else
        return {behavior: smallNS.SmallWorld.AdjustBehavior(this.behavior, this.behaviorType)};
};

smallNS.SmallWorld.BehaviorAvgCOMDivisor = 5;

smallNS.SmallWorld.EmptyBehavior = function(behavior, behaviorType, desiredBehaviors)
{
    behavior.fitness = 0.000001;

    //we create our objects, the first is fitness, but we'll also have
    //novelty, and genomic diversity
    behavior.objectives = [];
    behavior.objectives.push(behavior.fitness);

    switch(behaviorType)
    {

        case smallNS.BehaviorTypes.xCenterOfMass:
            behavior.points = [];
            for(var i=0; i < desiredBehaviors; i++)
            {
                behavior.points.push(0);
            }
            return behavior;
        case smallNS.BehaviorTypes.yCenterOfMass:
            behavior.points = [];
            for(var i=0; i < desiredBehaviors; i++)
            {
                behavior.points.push(0);
            }
            return behavior;
        case smallNS.BehaviorTypes.xyCenterOfMass:

            behavior.points = [];
             for(var i=0; i < desiredBehaviors; i++)
            {
                behavior.points.push({x: 0, y:0});
            }

            //return empty behavior
            return behavior;

        case smallNS.BehaviorTypes.avgXYCenterOfMass:

            behavior.points = [];
            for(var i=0; i < desiredBehaviors; i++)
            {
                behavior.points.push({x: 0, y:0});
            }
            behavior.points = smallNS.SmallWorld.condenseAvgCOM(behavior.points);

            //return empty behavior
            return behavior;

        case smallNS.BehaviorTypes.widthHeightMassSaveHeighestJump:
        case smallNS.BehaviorTypes.widthHeightEfficiencyMass:
        case smallNS.BehaviorTypes.widthHeightMass:

            behavior.points =  [];
            //done!
          //just push 0 for the 5 variables
                behavior.points.push(0);
                behavior.points.push(0);
                behavior.points.push(0);
                behavior.points.push(0);
                behavior.points.push(0);


            return behavior;

        case smallNS.BehaviorTypes.heatMap10x10:

            behavior.points = [];
            for(var i=0; i < 10*10; i++)
            {
                behavior.points.push(0);
            }

            return behavior;

        case smallNS.BehaviorTypes.nodeMovements:

            behavior.points = [];

            for(var i=0; i < 9*9*9; i++)
            {
                behavior.points.push(-1*smallNS.SmallWorld.noNodeMultiplier);
            }

            return behavior;

    }
}
smallNS.SmallWorld.AdjustBehavior = function(behavior, behaviorType)
{

    behavior.fitness = behavior.largestCOMDistance;

    //we create our objects, the first is fitness, but we'll also have
    //novelty, and genomic diversity
    behavior.objectives = [];
    behavior.objectives.push(behavior.fitness);

    switch(behaviorType)
    {
        case smallNS.BehaviorTypes.xCenterOfMass:
        case smallNS.BehaviorTypes.yCenterOfMass:
        case smallNS.BehaviorTypes.xyCenterOfMass:
        case smallNS.BehaviorTypes.widthHeightMass:
            //no adjustments to make, all data should be in behavior.points
           return behavior;
        case smallNS.BehaviorTypes.widthHeightMassSaveHeighestJump:

            //if we never went up after falling down, zero out the fitness
            if(behavior.maximumHeight == Number.MIN_VALUE || behavior.minimumHeight == Number.MAX_VALUE)
                behavior.fitness = 0.0000001;
            else {
                behavior.fitness = Math.abs(behavior.maximumHeight - behavior.minimumHeight);

                //a certain amount isn't counted
                if(behavior.fitness < 0.4)
                    behavior.fitness = 0.0000001;

                console.log("Final Height Distance: " + behavior.maximumHeight + " , min: " + behavior.minimumHeight + ", dif: " + behavior.fitness);
            }
            
            return behavior;
        case smallNS.BehaviorTypes.widthHeightEfficiencyMass:
           
           //fitness = distance traveled/mass == fitness/mass behavior == fitness/behavior.points[4] -- the last behavior
           //add 1 just in case mass = 0
           behavior.fitness = behavior.fitness/(behavior.points[4] + 1.0);

           return behavior;

        case smallNS.BehaviorTypes.avgXYCenterOfMass:

            behavior.points = smallNS.SmallWorld.condenseAvgCOM(behavior.points);

            //return empty behavior
            return behavior;

        case smallNS.BehaviorTypes.heatMap10x10:
            //number of sides sent in for adjustment
            //along with our heat map
            behavior.heatMap = smallNS.SmallWorld.heatMapAdjustments(behavior.heatMap, 10,10);

            //now flatten the behavior into a list of points
            behavior.points = smallNS.SmallWorld.flattenHeatMap(behavior.heatMap,10,10);
            return behavior;

        case smallNS.BehaviorTypes.nodeMovements:

            behavior.points = smallNS.SmallWorld.flattenNodeMovements(behavior.heatMap,9,9,9);

            return behavior;

    }
}

smallNS.SmallWorld.condenseAvgCOM = function(points)
{
    var condensedPoints = [];

    var modOut = smallNS.SmallWorld.BehaviorAvgCOMDivisor;
    var lastPoint = points[0];
    var sumAdd = {x:0, y:0};
    var currentIx = 0;
    condensedPoints.push(lastPoint);

    for(var i=1; i < points.length; i++)
    {
        if(i && i%modOut === 0)
        {
            lastPoint = {x: lastPoint.x + sumAdd.x/currentIx, y: lastPoint.y + sumAdd.y/currentIx};
            condensedPoints.push(lastPoint);
            sumAdd = {x:0, y:0};
            currentIx = 0;
        }
        else
        {
            var point = points[i];
            sumAdd.x += point.x;
            sumAdd.y += point.y;
            currentIx++;
        }
    }

    if(currentIx != 0)
    {
        lastPoint = {x: lastPoint.x + sumAdd.x/currentIx, y: lastPoint.y + sumAdd.y/currentIx};
        condensedPoints.push(lastPoint);
    }

    return condensedPoints;


}
smallNS.SmallWorld.heatMapAdjustments = function(heatMapBehavior, xSides, ySides)
{
    var totalCount = heatMapBehavior.fabCount;
    var adjustedBehavior = {};
    adjustedBehavior.fabCount = totalCount;

    if(totalCount == 0)
        return heatMapBehavior;

    //first, let's check the bottom row summation
    var bottomSum = 0;

    for(var x=0; x< xSides; x++)
    {
        bottomSum += heatMapBehavior[x][ySides-1]/totalCount;
        bottomSum += heatMapBehavior[x][ySides-2]/totalCount;
    }

    var flatten = false;
    if(bottomSum > .75)
    {
        flatten = true;
    }

    //lets flatten our behavior
    for(var x=0; x < xSides;x++)
    {
        adjustedBehavior[x] = {};

        for(var y=0; y < ySides; y++)
        {
            //if you're an asshole, and spend your time on the bottom, we're going to flatten you!
            //that is, you'll appear like nothing happens on the bottom most layer
            if(flatten && y == ySides -1){
                adjustedBehavior[x][y] = 0;
            }
            //if you're in the second row and you're flattened, we take away your juice too (close to 0)
            else if(flatten && y == ySides-2)
                adjustedBehavior[x][y] = .25*heatMapBehavior[x][y];
            else
            {
               adjustedBehavior[x][y] = heatMapBehavior[x][y];
            }
        }
    }

    return adjustedBehavior;

}
smallNS.SmallWorld.flattenHeatMap = function(heatMap, xSides, ySides)
{
    var flatten = [];
    var totalCount = heatMap.fabCount;
    if(totalCount == 0)
    {
        for(var i=0; i < xSides*ySides; i++)
            flatten.push(0);

        return flatten;
    }

    for(var x=0; x < xSides;x++)
    {
        for(var y=0; y < ySides; y++)
        {
            flatten.push(heatMap[x][y]/totalCount);
        }
    }
    return flatten;
}
smallNS.SmallWorld.flattenNodeMovements = function(heatMap, xSides, ySides, moveDirections)
{
    var flatten = [];

//    var totalCount = heatMap.fabCount;
//    if(totalCount == 0)
//    {
//        for(var i=0; i < xSides*ySides*moveDirections; i++)
//            flatten.push(0);
//
//        return flatten;
//    }

    for(var x=0; x < xSides;x++)
    {
        for(var y=0; y < ySides; y++)
        {
            var totalCount = heatMap[x][y].bCount;

            for(var w =0; w < moveDirections; w++)
            {
                var pushValue = (totalCount == 0 ? heatMap[x][y][w] : heatMap[x][y][w]/totalCount);

                if(!pushValue)
                    pushValue = 0;

//                console.log('x: ' + x + ' y: ' + y + ' TotalCount: ' + totalCount + ' heat ' + heatMap[x][y][w]);


               flatten.push(pushValue);
            }
        }
    }
    return flatten;
}

smallNS.SmallWorld.prototype.update = function(updateDeltaMS, props) {

//    if(typeof updateDeltaMS != 'number' && props != undefined)
//    {
//        props = updateDeltaMS;
//        updateDeltaMS = undefined;
//    }
    try{

    if(! this.simulating)
    {
        if (this.isMouseDown) {
            this.theWorld.mouseDownAt(mouseX, mouseY);
        } else if (this.theWorld.isMouseDown()) {
            this.theWorld.mouseUp();
        }
    }

//    console.log('Update?');

//        console.log('Update the world');
     var updateInfo = this.theWorld.update(updateDeltaMS, props);

//    console.log('Steps in update: ' + updateInfo.stepCount);

//        if(!props.visual)
//            console.log('Update the behavior');


//        if(this.drawObject.drawBehavior)
            this.calculateBehavior(updateInfo.stepCount);

//        if(!props.visual)
//             console.log('Done the behavior');
    }

    catch(e)
    {
        console.log('Major error: ');
        console.log(e);
        console.log(e.message);
//        console.log(e.getStackTrace());
        throw e;
    }

}
//takes in a bheavior object, and updates according to behavior type
smallNS.SmallWorld.prototype.applyBehavior = function(behavior, behaviorType, com, canvasWidth, canvasHeight)
{

    switch(behaviorType)
    {

        case smallNS.BehaviorTypes.xyCenterOfMass:
            behavior.points.push({x:com.x, y: com.y});

            behavior.totalBehaviorFrames = behavior.points.length;

            break;
        case smallNS.BehaviorTypes.xCenterOfMass:
            behavior.points.push(com.x);
            behavior.totalBehaviorFrames = behavior.points.length;

            break;
        case smallNS.BehaviorTypes.yCenterOfMass:
            behavior.points.push(com.y);
            behavior.totalBehaviorFrames = behavior.points.length;
            break;

        case smallNS.BehaviorTypes.avgXYCenterOfMass:

            if(!behavior.lastCom)
            {
                behavior.points.push({x: com.x, y: com.y});
            }
            else
            {
                behavior.points.push({x: com.x - behavior.lastCom.x, y: com.y - behavior.lastCom.y});
            }

            //set last center of mass
            behavior.lastCom = com;

            behavior.totalBehaviorFrames = behavior.points.length;

            break;

            //same for both
        case smallNS.BehaviorTypes.widthHeightEfficiencyMass:
        case smallNS.BehaviorTypes.widthHeightMass:
        case smallNS.BehaviorTypes.widthHeightMassSaveHeighestJump:

            //done!
            if(behavior.initialMorphology)
            {
                if(!behavior.allSet){
                    behavior.points.push(behavior.initialMorphology.width);
                    behavior.points.push(behavior.initialMorphology.height);
                    behavior.points.push(behavior.initialMorphology.startX);
                    behavior.points.push(behavior.initialMorphology.startY);
                    behavior.points.push(behavior.initialMorphology.mass);

                    //done setting the behavior, we'll be on our way now
                    behavior.allSet = true;
                }
            }

            behavior.totalBehaviorFrames++;


            break;

        case smallNS.BehaviorTypes.nodeMovements:

            //we don't have a difference in node locations, skip it!
            if(!behavior.lastNodeLocations)
                break;

            var killEverything = 0;

            var xSides =9;
            var ySides = 9;
            var moveDirections = 9;
            //this is a bit more complicated, we have to break down where every node is
            //and increment the locations where nodes exist (relative to the center of gravity)
            for(var i=0; i < xSides*ySides; i++)
            {
                if(i >= com.nodeLocations.length)
                {
                    var xIx = Math.floor(i%xSides);//Math.floor((centeredLoc.x/(this.canvasWidth/2) + 1)/2*xSides);
                    var yIx = Math.floor(i/ySides);//Math.floor(centeredLoc.y/this.canvasHeight*ySides);


//                        console.log('Node locs ' + i);

                    for(var w =0; w < moveDirections; w++)
                        behavior.heatMap[xIx][yIx][w] = -1*smallNS.SmallWorld.noNodeMultiplier;

                    //i don't think we mess with fabCounts
//                        this.behavior.heatMap.fabCount++;

                    //on to the next please!
                    continue;
                }

                behavior.heatMap.fabCount = 1;

//                    var prevCentered = {x: com.lastNodeLocations[i].x - com.x, y: com.lastNodeLocations[i].y - com.y};
//                    var centeredLoc = {x: com.nodeLocations[i].x - com.x, y: com.nodeLocations[i].y-com.y};
//                    console.log('Node dif');

                var difference = {x: com.nodeLocations[i].x - behavior.lastNodeLocations[i].x, y: com.nodeLocations[i].y - behavior.lastNodeLocations[i].y};

                if(isNaN(difference.x) || isNaN(difference.y)) //|| isNaN(prevCentered.x) || isNaN(prevCentered.y))
                {
                    continue;
                }

                var xDim = Math.floor(i%xSides);//Math.floor((centeredLoc.x/(this.canvasWidth/2) + 1)/2*xSides);
                var yDim = Math.floor(i/ySides);//Math.floor(centeredLoc.y/this.canvasHeight*ySides);


                if(difference.x == 0 && difference.y == 0)
                {
                    behavior.heatMap[xDim][yDim][moveDirections-1]++;
                    behavior.heatMap[xDim][yDim].bCount++;
                    behavior.totalBehaviorFrames++;

                    continue;
                }

                var angle = Math.atan2(difference.y, difference.x);

                //angle between -pi and pi,

                //find out where we are in fractional terms -- i.e. 45 degrees = pi/4 = 1/8 of 2PI -- implies first index in array of 8
                angle = (angle + Math.PI)/(2*Math.PI);
                //you shouldn't select the last index ever, but you can get up to
                //i.e. if i have 8 divisions, angle*8 goes from 0 to 8 -- but in reality, we have indexes from 0 to 7.
                //it only can be eight if we equal exactly PI, so we just make sure not to do that by accident.
                var ix = Math.max(0, Math.min(moveDirections-2,  Math.floor(angle*(moveDirections-1))));

                try
                {

                    behavior.heatMap[xDim][yDim][ix]++;
                    behavior.heatMap[xDim][yDim].bCount++;
                    behavior.totalBehaviorFrames++;

//                        console.log('Bmap : ' + this.behavior.heatMap[xDim][yDim][ix]);
//                        console.log(' bcount: ' + this.behavior.heatMap[xDim][yDim].bCount);

                }
                catch(e)
                {
                    console.log('Printing com error: ');
                    console.log(e.message);
                    throw e;
                }

            }

            break;

        case smallNS.BehaviorTypes.heatMap10x10:

            var killEverything = 0;

            var xSides =10;
            var ySides = 10;
            //this is a bit more complicated, we have to break down where every node is
            //and increment the locations where nodes exist (relative to the center of gravity)
            for(var i=0; i < com.nodeLocations.length; i++)
            {
                var centeredLoc = {x: com.nodeLocations[i].x - com.x, y: com.nodeLocations[i].y};

                if(isNaN(centeredLoc.x) || isNaN(centeredLoc.y))
                {
//                        console.log('skip a lot ' + i);
                    //don't process
                    continue;
//                        killEverything = true;
//
//                        for(var x = 0; x <xSides; x++ )
//                            for(var y=0; y < ySides; y++)
//                               this.behavior.heatMap[x][y] = 0;
//
//                        console.log('Everything died');
//
//                        break;
                }

                var xDim = Math.floor((centeredLoc.x/(canvasWidth/2) + 1)/2*xSides);
                var yDim = Math.floor(centeredLoc.y/canvasHeight*ySides);

                //outside of our heatmap, doesn't count!
                if(xDim < 0 || xDim > xSides-1 || yDim < 0 || yDim > ySides -1)
                    continue;

//                    xDim = Math.max(0,Math.min(xDim, xSides-1));
//                    yDim = Math.max(0,Math.min(yDim, ySides-1));

//                    console.log('Dim found- x: ' + xDim + " y: " + yDim);
//                    console.log('X location: ' + centeredLoc.x + ' halfwidth: ' +
//                        this.canvasWidth/2 + ' div: ' +(centeredLoc.x/(this.canvasWidth/2) + 1)/2 + ' xDim: ' +xDim);

//                    console.log('Y location: ' + centeredLoc.y + ' yHeight: ' +
//                        this.canvasHeight + ' yDim: ' +yDim);
                try
                {
                    behavior.heatMap[xDim][yDim]++;
                    behavior.heatMap.fabCount++;
                    behavior.totalBehaviorFrames++;
                }
                catch(e)
                {
                    console.log('Error in heat mapping, Printing com: ');
                    console.log(com);
                    console.log(e.message);
                    throw e;
                }

            }

            break;
    }

    behavior.lastNodeLocations = com.nodeLocations;
};

smallNS.SmallWorld.prototype.calculateBehavior = function(stepsTaken)
{
//    console.log('Behaqvioring!');
    //we're done with our behavior!
    if(this.behavior.frameCount >= this.behaviorTotalCount)
        return;

    //only grab it when you wants it (depending on frames to skip)
    //so we add our frame count.
    //this tells us how many frames we've seen
    this.frameCount += stepsTaken;


    //need to make sure we don't start evaluating until a certain number of frames occurs (i.e. the object is falling from the skies!)
    if(this.beginEvaluation && this.frameCount < this.waitToStartFrames)
        return;
    else if(this.beginEvaluation && this.frameCount >= this.waitToStartFrames)
    {
        this.frameCount -= this.waitToStartFrames;
        this.beginEvaluation = false;
    }

    //we want to take a snapshot every 3 frames for instance
    //if we've only gone two simulation steps, ignore this!
    if(this.frameCount < this.behaviorSkipFrames)
        return;

    //every update, we should calculate behavior, but we keep these separate calls, since it may be expensive in some scenarios
    var com = this.theWorld.nodesCenterOfMass();

    if(!this.behavior.startingCOM)
    {
        this.behavior.startingCOM = com;
        this.behavior.minY = com.y;
        this.behavior.maxY = com.y;
    }
    var startCom = this.behavior.startingCOM;
    var dist = {x: (startCom.x - com.x)*(startCom.x - com.x), y: (startCom.y - com.y)*(startCom.y - com.y)};

    //we fetch the current y of the lowest node to the ground -- 
    //previously we measured centerof mass, but that meant really tall individuals could cheat!
    var lowestNodeY = this.lowestNodeHeight(com);
    var heighestNodeY = this.heighestNodeHeight(com);
    // console.log("First node check lowest: " + lowestNodeY + " , heighest: " + heighestNodeY);
    
   

    if(this.behavior.largestCOMDistance == undefined){
        this.behavior.largestCOMDistance = 0.000001;
        this.behavior.minimumHeight = Number.MAX_VALUE;
        this.behavior.maximumHeight = Number.MIN_VALUE;
        this.firstUp = false;
        this.lastLowestHeight = heighestNodeY;
        // console.log("Body Count: " + com.bodyCount);
        // console.log("First node check lowest: " + lowestNodeY + " , heighest: " + heighestNodeY);
    }

     if(!this.firstUp)
        console.log("First up node check lowest: " + this.lastLowestHeight + " , heighest: " + heighestNodeY);

    //we check to see the largest distance accumulated so far from the start
    //we can use this in fitness or local competition calculates
    this.behavior.largestCOMDistance =  Math.sqrt(dist.x);//Math.max(this.behavior.largestCOMDistance, Math.sqrt(dist.x));

    //if you start going up, then we start recording
    this.firstUp = this.firstUp || (heighestNodeY - this.lastLowestHeight < 0);

    if(this.firstUp)
    {
        this.behavior.minimumHeight =  Math.min(this.behavior.minimumHeight, heighestNodeY);//Math.max(this.behavior.largestCOMDistance, Math.sqrt(dist.x));
        this.behavior.maximumHeight =  Math.max(this.behavior.maximumHeight, heighestNodeY);//Math.max(this.behavior.largestCOMDistance, Math.sqrt(dist.x));
    }

    this.lastLowestHeight = heighestNodeY; //lowestNodeY

  

//    console.log('Rec dist: ');
//    console.log(this.behavior.largestCOMDistance);

    //we actually will assume this body position for multiple frames if there is an accidental skip or something
    while(this.frameCount >= this.behaviorSkipFrames && (this.behavior.totalBehaviorFrames < this.behaviorTotalCount))//(!this.behavior.points || this.behavior.points.length < this.behaviorTotalCount))
    {
        //update framecount on behavior for all behavior types!
        this.behavior.frameCount++;

        this.applyBehavior(this.behavior, this.behaviorType, com, this.canvasWidth, this.canvasHeight);

        if(this.secondBehavior)
            this.applyBehavior(this.secondBehavior, this.secondBehaviorType, com, this.canvasWidth, this.canvasHeight);

        this.frameCount -= this.behaviorSkipFrames;
    }
};


smallNS.SmallWorld.prototype.addEventListeners = function()
{
    this.canvas.addEventListener("mousedown", function(e) {
        this.isMouseDown = true;
        this.handleMouseMove(e);
        document.addEventListener("mousemove", this.handleMouseMove, true);
    }, true);

    this.canvas.addEventListener("mouseup", function() {
        if (!this.isMouseDown) return;
        document.removeEventListener("mousemove", this.handleMouseMove, true);
        this.isMouseDown = false;
        this.mouseX = undefined;
        this.mouseY = undefined;
    }, true);
};


smallNS.SmallWorld.prototype.handleMouseMove = function(e) {
    this.mouseX = (e.clientX - canvas.getBoundingClientRect().left) / this.scale;
    this.mouseY = (e.clientY - canvas.getBoundingClientRect().top) / this.scale;
}

smallNS.SmallWorld.prototype.shouldDraw = function(boolValue)
{
    this.drawObject.turnOffDrawing = !boolValue;

}
smallNS.SmallWorld.prototype.shouldDrawBehavior = function(boolValue, showRawBehavior)
{

    this.drawObject.drawBehavior = boolValue;
    this.drawObject.showRawBehavior = (showRawBehavior) ? true : false;

}

smallNS.SmallWorld.prototype.zombieMode = function(boolValue)
{
    this.drawObject.zombieMode = boolValue;
}

smallNS.SmallWorld.prototype.freezeLoop = function(boolValue)
{
    this.refuseStartLoop = boolValue;
    if(boolValue)
        this.stopLoop();

}
smallNS.SmallWorld.prototype.startLoop = function(statObject)
{
    if(this.refuseStartLoop)
        return;

    var props = {visual: true};

    //for a smooth transition, just make the start time be now!
    this.theWorld.lastTime = Date.now();
    this.interruptLoop = false;
    //closure should allow for this variable to be called from the loop. Hoo-ray?
    var smallWorld = this;
    var frameCount = 0;
    var printCount = 90;

//    this.init();
    (function loop(animStart) {
        if(!smallWorld)
            return;

        smallWorld.update(undefined, {visual:true});
        smallWorld.draw();

        if(statObject && (++frameCount % printCount == 0))
            statObject.text(smallWorld.behavior.largestCOMDistance);

        if(!smallWorld.interruptLoop)
            requestAnimFrame(loop);
    })();
};

smallNS.SmallWorld.prototype.stopLoop = function()
{
    //this will cause a break in the loop
    this.interruptLoop = true;
}

smallNS.SmallWorld.prototype.toggleTest = function()
{
    console.log('Attmpting toggle');
    //we need our body ID, where do I get that from?

    toggleSelectedBody(lastObjectID, function(responseData)
    {
        //we've received a server response, lets just print it
        console.log('Toggle success');
        console.log(responseData);
        console.log('toggle over and out');

    });
}


smallNS.SmallWorld.prototype.centerBody = function()
{
    //get the center of mass
    var com = this.theWorld.nodesCenterOfMass();


    var difference = {x: this.canvasWidth/2 - com.x, y:this.canvasHeight/2 - com.y};


};

smallNS.SmallWorld.prototype.lowestNodeHeight = function(com)
{
    //use the center of mass node location array
    var nl = com.nodeLocations;

    var lowestY = Number.MAX_VALUE;

    for(var i=0; i < nl.length; i++)
    {

        lowestY = Math.min(nl[i].y, lowestY);
    }

    return lowestY;
};

smallNS.SmallWorld.prototype.heighestNodeHeight = function(com)
{
    //use the center of mass node location array
    var nl = com.nodeLocations;

    var heighestY = Number.MIN_VALUE;

    for(var i=0; i < nl.length; i++)
    {

        heighestY = Math.max(nl[i].y, heighestY);
    }

    return heighestY;
};


smallNS.SmallWorld.prototype.addJSONBody = function(jsonData)
{
        if(!jsonData){
            console.log("No JSON fetched, aborting body add");
            return;
        }
//        console.log("JSON fetched: ");
//        console.log( jsonData);//.InputLocations.concat(jsonData.HiddenLocations));

        //right now, this is a bodyObject with InputLocations, HiddenLocations, and Connections

    this.behavior.initialMorphology = this.theWorld.jsonParseNodeApp(jsonData);

//        this.behavior.startingCOM = this.theWorld.nodesCenterOfMass();

};

smallNS.SmallWorld.prototype.insertBody =  function()
{
    getBody(this.addJSONBody);
}

smallNS.SmallWorld.prototype.pad = function(scale, k) {
    return scale;
//        var range = scale.range();
//        if (range[0] > range[1]) k *= -1;
//        return scale.domain([range[0] - k, range[1] + k].map(scale.invert)).nice();
};
smallNS.SmallWorld.prototype.jsonParseMINS = function(jsonDoc, documentType)
{
    //The structure of the json is as follows

    //All the bodies are inside of nodes
    //should all be of type "mass" or "node"

    //so let's create our bodies
    var oNodes = jsonDoc.model.nodes;

    var entities = {};
    for(var nodeType in oNodes)
    {
        //node type doesn't matter as much as our documentType

        var aBodies = oNodes[nodeType];

        for(var b=0; b < aBodies.length; b++)
        {
            var nodeObj = aBodies[b];
            entities[nodeObj.id] = (Entity.build({id:nodeObj.id,
                x: (parseFloat(nodeObj.x)- this.canvasWidth/1.3)/this.scale,
                y: (this.canvasHeight -parseFloat(nodeObj.y))/this.scale - this.canvasHeight/(2*this.scale),
                vx: parseFloat(nodeObj.vx)/this.scale,
                vy: parseFloat(nodeObj.vy)/this.scale,
                radius: .5

            }));
        }
    }
    //push our bodies into the system so that our joints have bodies to connect to
    this.theWorld.setBodies(entities);

    var oLinks = jsonDoc.model.links;
    for(var linkType in oLinks)
    {
        //link type matters for generating muscles or distance joints

        var aLinks = oLinks[linkType];

        switch(linkType)
        {
            case "spring":
                for(var l=0; l < aLinks.length; l++)
                {
                    var linkObj = aLinks[l];
                    //need to add the spring object info -- so springyness and what have you
                    //maybe also the rest length? Does that matter?
                    var dJoint = this.theWorld.addDistanceJoint(linkObj.a, linkObj.b, {frequencyHz: 15, dampingRatio:.1});

                    dJoint.SetLength(parseFloat(linkObj.restlength)/this.scale);
                }

                break;
            case "muscle":
                for(var s=0; s < aLinks.length; s++)
                {
                    var musObj = aLinks[s];
                    //need to add into the muscle object-- concept of rest location?
                    //addMuscleJoint
                    //phase: parseFloat(musObj.phase), amplitude: parseFloat(musObj.amplitude)
                    var mJoint = this.theWorld.addMuscleJoint(musObj.a, musObj.b, {amplitude: 1.6*parseFloat(musObj.amplitude), phase: parseFloat(musObj.phase)});//, frequencyHz: 15, dampingRatio:.1 });

                    var aCenter = this.theWorld.bodiesMap[musObj.a].GetWorldCenter();
                    var bCenter =  this.theWorld.bodiesMap[musObj.b].GetWorldCenter();
                    console.log('A center: ' + aCenter);
                    console.log('B center: ' + bCenter);

                    console.log('Dist dif: ' + 14*Math.sqrt(Math.pow(aCenter.x- bCenter.x,2) + Math.pow(aCenter.y - bCenter.y, 2) ));
                    console.log('Amp: ' + parseFloat(musObj.amplitude) + ' Rest: ' +  parseFloat(musObj.restlength));

                    mJoint.SetLength(parseFloat(musObj.restlength)/this.scale);
                }

                break;
        }

    }
    //DO NOT set this immediately -- but we can use it to center our object


//    this.behavior.startingCOM = this.theWorld.nodesCenterOfMass();
};