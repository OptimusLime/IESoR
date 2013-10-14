//Class for drawing box2D objects - using fabric and cavnas/SVG -- should get us some benefits where possible

var boxNSS = "BoxDrawing";
var boxNS = namespace(boxNSS);
var b2Math = Box2D.Common.Math.b2Math,
    b2Joint = Box2D.Dynamics.Joints.b2Joint,
    b2PulleyJoint = Box2D.Dynamics.Joints.b2PulleyJoint,
    b2Vec2 = Box2D.Common.Math.b2Vec2,
    b2CircleShape = Box2D.Collision.Shapes.b2CircleShape,
    b2Shape = Box2D.Collision.Shapes.b2Shape,
    b2PolygonShape = Box2D.Collision.Shapes.b2PolygonShape,
    b2EdgeShape = Box2D.Collision.Shapes.b2EdgeShape;

boxNS.DrawingObject = function(sDrawElementName, canvasWidth, canvasHeight, scale, zombieMode)
{
    //for creating IDs for our drawing objects
    this.idCount = 0;

    this.canvasWidth = canvasWidth;
    this.canvasHeight = canvasHeight;

    this.lineColor = '#FFC704';// '#B2B212';
    this.lineWidth = 3;

    this.nodeColor = '#FE0000'; //'#B20C09';
    this.nodeOutlineColor = '#222';

    this.groundColor = '#008188';// '#199EFF';

    //do we actually want to draw using the fabricJS library - can toggle
    //create fabric object, render only when we call
    //and disable global selection (i.e. drag rectangle)
    //if you are a zombie, YOU DO NOT SUPPORT CANVAS, DO NOT INITIALIZE THIS OBJECT
    if(!zombieMode)
    {
        this.fabricCanvas = new fabric.Canvas(sDrawElementName, { renderOnAddition: false});
        this.fabricCanvas.selection = false;
        this.backRect = new fabric.Rect({width: 8*this.canvasWidth, height: 2*this.canvasHeight, x: -4*this.canvasWidth, y:-this.canvasHeight/2}, {fill: "#000"});


        var lowerColor = '#555555';
        var higherColor = '#AAAAAA';

        this.backRect.setGradientFill(this.fabricCanvas.getContext(), {
            x1: 0,
            y1:  this.backRect.height / 2,
            x2:  this.backRect.width,
            y2:  this.backRect.height / 2,
            colorStops: {
                0: lowerColor,
                0.05: higherColor,
                0.1: lowerColor,
                0.15: higherColor,
                0.2: lowerColor,
                0.25: higherColor,
                0.3:lowerColor,
                0.35:higherColor,
                0.4: lowerColor,
                0.45:higherColor,
                0.5: lowerColor,
                0.55:higherColor,
                0.6:lowerColor,
                0.65: higherColor,
                0.7: lowerColor,
                0.75: higherColor,
                0.8: lowerColor,
                0.85: higherColor,
                0.9: lowerColor,
                0.95: higherColor,
                1: lowerColor
            }
        });

        this.fabricCanvas.add(this.backRect);
    }
    //we set the scale, and then initialize the drawObjects
    this.drawScale = scale || 1;
    this.drawObjects = {bodies:{}, joints:{}};
    this.zombieMode = zombieMode;

};

boxNS.DrawingObject.prototype.turnOffDrawing = false;

boxNS.DrawingObject.prototype.drawBehavior = false;

boxNS.DrawingObject.prototype.showRawBehavior = true;

boxNS.DrawingObject.prototype.zombieMode = false;


boxNS.DrawingObject.prototype.addBehavior = function(behaviorObject, behaviorType)
{
    this.behaviorType = behaviorType;
    //keep track of the behavior of an object!
    this.behavior = behaviorObject;
}

//some helpers for the class

boxNS.DrawingObject.prototype.peekNextID = function()
{
    return this.idCount;
};
boxNS.DrawingObject.prototype.getNextID = function()
{
    return this.idCount++;
};

//for scaling a vector
boxNS.DrawingObject.scalePoint = function(p, drawScale, modifyInPlace)
{
    if(!modifyInPlace)
        return new b2Vec2(p.x*drawScale, p.y*drawScale);
    else
    {
        p.x *= drawScale; p.y*=drawScale;
        return p;
    }

};

//for adding in world objects all at once (you can do this one at a time if you prefer
//in the future, I think it would be best to do this individually
boxNS.DrawingObject.prototype.setWorldObjects = function(aWorldObjects)
{
    //don't do ANYTHING in zombie mode
    if(this.zombieMode)
        return;

    //for now, we use this as an opportunity to add an object
    //in reality, we're going to make a hook where we get a callback when a physics object is inserted or removed
    for(var j=0; j < aWorldObjects.joints.length; j++)
    {
        this.addJoint(aWorldObjects.joints[j]);
    }
    for(var b=0; b < aWorldObjects.bodies.length; b++)
    {
        this.addBody(aWorldObjects.bodies[b]);
    }


    if(!this.turnOffDrawing)
        this.fabricCanvas.renderAll();


};

boxNS.DrawingObject.prototype.drawWorld = function(alphaInterpolate, centerOfGravity)
{
    //don't draw or update anything in zombie mode
    if(this.zombieMode)
        return;

    //if center of gravity is passed in, center around ... gravity duh!
    centerOfGravity = (centerOfGravity ? {x: centerOfGravity.x - this.canvasWidth/2, y: centerOfGravity.y - 3*this.canvasHeight/4} : {x:0, y:0});
    centerOfGravity.y = 0;
//    centerOfGravity = {x:0 , y:0};

    if(!this.lastCenterOfGravity)
        this.lastCenterOfGravity = centerOfGravity;
    else{
        this.backRect.left += ( this.lastCenterOfGravity.x-centerOfGravity.x);
        this.lastCenterOfGravity = centerOfGravity;
    }
//    this.backRect.y -= centerOfGravity.y;
//    console.log('Center of gravity: '); console.log(centerOfGravity);


    for(var jID in this.drawObjects.joints)
    {
        this.drawFabricJoint(this.drawObjects.joints[jID], alphaInterpolate, centerOfGravity);
    }
    for(var bID in this.drawObjects.bodies)
    {
        this.drawFabricBody(this.drawObjects.bodies[bID],alphaInterpolate, centerOfGravity);
    }
    if(this.drawBehavior)
    {
//        console.log('Drawing behavior ');
        this.updateBehaviorJoints(this.behaviorDrawObj, this.behavior, this.behaviorType, alphaInterpolate, {x:0, y: 0});
    }


    if(!this.turnOffDrawing)
        this.fabricCanvas.renderAll();

};
boxNS.DrawingObject.interpolatePoint = function(pNew, pOld, alpha, centerOfGravity)
{
    pNew.x = alpha*(pNew.x - centerOfGravity.x) + (1-alpha)*pOld.x;
    pNew.y = alpha*(pNew.y - centerOfGravity.y) + (1-alpha)*pOld.y;

    return pNew;
}
boxNS.DrawingObject.interpolatePoints = function(aCurrent, aOld, fAlpha, centerOfGravity)
{
    for(var i=0; i < aCurrent.length; i++)
    {
        aCurrent[i] =  boxNS.DrawingObject.interpolatePoint(aCurrent[i], aOld[i], fAlpha, centerOfGravity);
    }
    return aCurrent;
}
//For updating/drawing bodies or joints
boxNS.DrawingObject.prototype.drawFabricBody = function(drawObj,alphaInterpolate, centerOfGravity)
{
    alphaInterpolate = alphaInterpolate || 1;

    var info = this.shapeInfo(drawObj.body, drawObj.shape);
    var fabObj = drawObj.fabric;

    switch (drawObj.shape.m_type) {
        case b2Shape.e_circleShape:
        {
            fabObj.left = (info.center.x - centerOfGravity.x )*alphaInterpolate + fabObj.left*(1-alphaInterpolate);
            fabObj.top = (info.center.y -centerOfGravity.y)*alphaInterpolate + fabObj.top*(1-alphaInterpolate);
        }
            break;
        case b2Shape.e_polygonShape:
        {
            fabObj.points = boxNS.DrawingObject.interpolatePoints(info.vertices, fabObj.points, alphaInterpolate, centerOfGravity);  //info.vertices;
            fabObj._calcDimensions();
        }
            break;
        case b2Shape.e_edgeShape:
        default:
            console.log("don't know how to draw this object type");
            break;
    }

};

boxNS.DrawingObject.prototype.drawFabricJoint = function(drawObj, alphaInterpolate, centerOfGravity)
{

    var info = this.jointInfo(drawObj.joint);

    var fabObj = drawObj.fabric;
    fabObj.points = boxNS.DrawingObject.interpolatePoints(info.points, fabObj.points, alphaInterpolate, centerOfGravity);//info.points;
    fabObj._calcDimensions();

};

boxNS.DrawingObject.prototype.createAndAddBehaviorDrawObject = function(behaviorObject, behaviorType)
{
    if(this.zombieMode)
        return;


        var behaviorDrawObject;

        switch(behaviorType)
        {
            case smallNS.BehaviorTypes.widthHeightMass:

                //don't yet have a way to draw this guy
//                width: maxX - minX, height: maxY - minY, startX: minX, startY: minY

                //create our object without any points, and add it!
                var fabPoly  = new fabric.Rect({left: behaviorObject.startX, top: behaviorObject.startY, width: behaviorObject.width, height: behaviorObject.height},{opacity:.3, fill: '#55f', stroke: '#f55'});

                behaviorDrawObject = {index: this.fabricCanvas.getObjects().length, fabric: fabPoly};

                //add it to our canvas
                this.fabricCanvas.add(fabPoly);

                break;
            case smallNS.BehaviorTypes.xyCenterOfMass:
            case smallNS.BehaviorTypes.xCenterOfMass:
            case smallNS.BehaviorTypes.yCenterOfMass:
            case smallNS.BehaviorTypes.avgXYCenterOfMass:

                //create our object without any points, and add it!
                var fabPoly  = new fabric.Polygon(behaviorObject.points, {fill: '#55f', stroke: '#f55'});

                behaviorDrawObject = {index: this.fabricCanvas.getObjects().length, fabric: fabPoly};

                //add it to our canvas
                this.fabricCanvas.add(fabPoly);

                break;

            case smallNS.BehaviorTypes.nodeMovements:

                console.log('Fabbing lines');
                var fabLines = {};

                var xSides =9;
                var ySides = 9;
                var startX= 0, startY = 0;
                var deltaX = this.canvasWidth/xSides;
                var deltaY = this.canvasHeight/ySides;


                var moveDirections = 9;
                var angleDx = 2*Math.PI/(moveDirections-1);

                var startAngle = 0;

                var startIndex = this.fabricCanvas.getObjects().length;
                var endIndex;
                for(var x =0; x < xSides; x++)
                {

                    startY = 0;
                    if(fabLines[x] === undefined)
                        fabLines[x] = {};

                    for(var y=0; y < ySides; y++)
                    {
                        startAngle =0;

                        if(fabLines[x][y] === undefined)
                            fabLines[x][y] = {};

                        if(!behaviorObject.heatMap[x][y].bCount)
                            continue;

                        //for each move direction, we add angleDx, calc vector and add it to current startx.starty
                        for(var w =0; w < moveDirections; w++)
                        {
                            var dist = 15;
                            var addVector = {x: dist*Math.cos(startAngle), y:dist*Math.sin(startAngle)};

                            //create our object without any points, and add it!
                            var fabRect  = new fabric.Line( [startX, startY, startX+addVector.x, startY + addVector.y], {fill: '#000', stroke: '#000', strokeWidth: 2});

//                    console.log('Starx: ' + startX + ' Starty: ' + startY);
//                    console.log('Deltas x: ' + deltaX + ' y:' + deltaY);
//                    console.log(fabRect);



//                        console.log('Creating line: ' + startX + ' y: ' + startY + ' eX: ' + (startX + addVector.x) + ' ey: ' + (startY + addVector.y));

                            //add it to our canvas
                            this.fabricCanvas.add(fabRect);
                            fabLines[x][y][w] = fabRect;

                            startAngle += angleDx;
                        }

                        startY += deltaY;


                    }
                    startX += deltaX;

                }

                endIndex = this.fabricCanvas.getObjects().length;
                behaviorDrawObject = {index: startIndex, endIndex: endIndex, fabCount:0, fabric: fabLines};

                break;
            case smallNS.BehaviorTypes.heatMap10x10:

                var fabRects = {};
                var xSides =10;
                var ySides = 10;
                var startX= 0, startY = 0;
                var deltaX = this.canvasWidth/xSides;
                var deltaY = this.canvasHeight/ySides;

                var startIndex = this.fabricCanvas.getObjects().length;
                var endIndex;

                for(var x =0; x < xSides; x++)
                {
                    startY = 0;

                    if(fabRects[x] === undefined)
                        fabRects[x] = {};

                    for(var y=0; y < ySides; y++)
                    {
                        //create our object without any points, and add it!
                        var fabRect  = new fabric.Rect({left:startX + deltaX/2, top:startY + deltaY/2, width:deltaX, height:deltaY,  fill: '#000', stroke: '#000'});

//                    console.log('Starx: ' + startX + ' Starty: ' + startY);
//                    console.log('Deltas x: ' + deltaX + ' y:' + deltaY);
//                    console.log(fabRect);

                        //add it to our canvas
                        this.fabricCanvas.add(fabRect);
                        fabRects[x][y] = fabRect;

                        startY += deltaY;

                    }
                    startX += deltaX;
                }

                endIndex = this.fabricCanvas.getObjects().length;
                behaviorDrawObject = {index: startIndex, endIndex: endIndex, fabCount:0, fabric: fabRects};

                break;
        }

    return behaviorDrawObject;
}

boxNS.DrawingObject.prototype.updateBehaviorJoints = function(behaviorDrawObj, behaviorObject, behaviorType, alpha, centerOfGravity)
{

    //no behavior in zombie mode!
    if(this.zombieMode)
        return;

//    console.log(//'bobj len: ' + behaviorObject.points.length +
//        ' bObj heat: ' + behaviorObject.heatMap.fabCount);

    //if we have some valid behavior, but no object yet, add it!
    if(behaviorObject.totalBehaviorFrames && !behaviorDrawObj){//((behaviorObject.points && behaviorObject.points.length) || (behaviorObject.heatMap && behaviorObject.heatMap.fabCount)) && !behaviorDrawObj){

//        console.log('creating draw obj');
        this.behaviorDrawObj = this.createAndAddBehaviorDrawObject(behaviorObject, behaviorType);
        behaviorDrawObj = this.behaviorDrawObj;

//        console.log(behaviorDrawObj);
    }
    else if(!behaviorDrawObj)
        return;


    switch(behaviorType)
    {
        case smallNS.BehaviorTypes.widthHeightMass:
            //fabobj doesn't change yay!
            break;
        case smallNS.BehaviorTypes.xyCenterOfMass:
        case smallNS.BehaviorTypes.xCenterOfMass:
        case smallNS.BehaviorTypes.yCenterOfMass:

            var fabObj = behaviorDrawObj.fabric;
            fabObj.points = behaviorObject.points;// boxNS.DrawingObject.interpolatePoints(behaviorJoints, behaviorJoints, alpha, centerOfGravity);
            fabObj._calcDimensions();

            break;
        case smallNS.BehaviorTypes.avgXYCenterOfMass:

            var fabObj = behaviorDrawObj.fabric;
            fabObj.points = smallNS.SmallWorld.condenseAvgCOM(behaviorObject.points);
            fabObj._calcDimensions();

            break;
        case smallNS.BehaviorTypes.nodeMovements:
            var xSides = 9;
            var ySides = 9;
            var moveDirections = 9;
            var fabObj = behaviorDrawObj.fabric;
            var heatMap = behaviorObject.heatMap;
            var fabCount = heatMap.fabCount;

//            var fixedBehavior = (this.showRawBehavior) ? behaviorJoints : smallNS.SmallWorld.heatMapAdjustments(heatMap, 10,10);

            //don't correct joints here, just use them directly
            var fixedBehavior = heatMap;

            //behavior joints contains heat map info we update our current heat map!
            //skip the heat map if you are already done!
            if(fabCount === 0 || fabCount === undefined)
                break;

            for(var x =0; x < xSides; x++)
            {
                for(var y=0; y < ySides; y++)
                {
                    var bCount = fixedBehavior[x][y].bCount;
//                    console.log('bc: ' + bCount);
                    if(!bCount)
                        continue;

                    for(var w =0; w < moveDirections; w++)
                    {
                            var fabRect = fabObj[x][y][w];
                            var heat =  fixedBehavior[x][y][w]/bCount;//behaviorJoints[x][y]/fabCount;

                            //either we're not a 0 color, or we are a zero color, but the current color is nonzero!
//                            console.log('Heat: ' + heat);
                            if(heat >0 || (heat ==0 && fabRect.get('fill') != "#000000"))
                            {
                                //then we use the heat to calculate the color!
                                //multiply heat by 255!

                                var rgb = decimalToHex(Math.floor(heat*255));

                                var color = '#' + rgb + rgb + rgb;


                                fabRect.set({
                                    fill:     color
                                });

                            }

                    }





                }
            }



            break;

        case smallNS.BehaviorTypes.heatMap10x10:

            var xSides =10;
            var ySides = 10;
            var fabObj = behaviorDrawObj.fabric;
            var heatMap = behaviorObject.heatMap;
            var fabCount = heatMap.fabCount;

            var fixedBehavior = (this.showRawBehavior) ? heatMap : smallNS.SmallWorld.heatMapAdjustments(heatMap, 10,10);

            //behavior joints contains heat map info we update our current heat map!
            //skip the heat map if you are already done!
            if(fabCount === 0 || fabCount === undefined)
             break;

            for(var x =0; x < xSides; x++)
            {
                for(var y=0; y < ySides; y++)
                {
                    var fabRect = fabObj[x][y];
                    var heat =  fixedBehavior[x][y]/fabCount;//behaviorJoints[x][y]/fabCount;

                    //either we're not a 0 color, or we are a zero color, but the current color is nonzero!
                    if(heat >0 || (heat ==0 && fabRect.get('fill') != "#000000"))
                    {
                        //then we use the heat to calculate the color!
                        //multiply heat by 255!

//                      console.log('Heat: ' + heat);
                        var rgb = decimalToHex(Math.floor(heat*255));

                        var color = '#' + rgb + rgb + rgb;


                        fabRect.set({
                            fill:     color
                        });

                    }

                }
            }

            break;
    }


}
function decimalToHex(d, padding) {
    var hex = Number(d).toString(16);
    padding = typeof (padding) === "undefined" || padding === null ? padding = 2 : padding;

    while (hex.length < padding) {
        hex = "0" + hex;
    }

    return hex;
}

boxNS.DrawingObject.prototype.defaultRadius = 0;
//ADD BODY/ ADD JOINT
boxNS.DrawingObject.prototype.addBody = function(dBody)
{


    if(dBody.drawID != undefined)
    {
        console.log("Already tagged with ID: " + dBody.drawID);
        return;
    }

    dBody.drawID = this.getNextID();

    if(this.zombieMode)
        return;

    var shape = dBody.GetFixtureList().GetShape();
    var info = this.shapeInfo(dBody, shape);

    switch (shape.m_type) {
        case b2Shape.e_circleShape:
        {

            var fabCircle  = new fabric.Circle({ radius: info.radius, fill: this.nodeColor, stroke: this.nodeOutlineColor,  left: info.center.x, top:  info.center.y });

            //we've created a drawID already, link the two
            fabCircle.drawID = dBody.drawID;

            this.drawObjects.bodies[dBody.drawID] = {index: this.fabricCanvas.getObjects().length, fabric: fabCircle, body: dBody, shape: shape, bodyType: shape.m_type};

            //add it to our canvas
            this.fabricCanvas.add(fabCircle);

        }
            break;
        case b2Shape.e_polygonShape:
        {
            //the info object has the vertices in it already - scaled up by drawscale

            //this.m_debugDraw.DrawSolidPolygon(vertices, vertexCount, color);
            var fabPoly  = new fabric.Polygon(info.vertices, {fill: this.groundColor});
            //we've created a drawID already, link the two
            fabPoly.drawID = dBody.drawID;

            this.drawObjects.bodies[dBody.drawID] = {index: this.fabricCanvas.getObjects().length, fabric: fabPoly, body: dBody, shape: shape, bodyType: shape.m_type};

            //add it to our canvas
            this.fabricCanvas.add(fabPoly);

        }
            break;
        case b2Shape.e_edgeShape:
        {
            console.log("Don't know how to add edge shapes. What does that mean?");
            //var edge = (shape instanceof b2EdgeShape ? shape : null);
            //this.m_debugDraw.DrawSegment(b2Math.MulX(xf, edge.GetVertex1()), b2Math.MulX(xf, edge.GetVertex2()), color);
        }
            break;
    }
};
boxNS.DrawingObject.prototype.addJoint = function(joint)
{

    if(joint.drawID != undefined)
    {
        console.log("Already tagged joint with ID: " + joint.drawID);
        return;
    }

    joint.drawID = this.getNextID();

    if(this.zombieMode)
        return;


    var info = this.jointInfo(joint);



    //create our poly line for this joint (normally just 2 points really)
    var fabPolyLine =  new fabric.Polyline(info.points, {fill: this.lineColor, stroke:this.lineColor, strokeWidth:this.lineWidth, opacity: .7});
    fabPolyLine.drawID = joint.drawID;

//    fabPolyLine.setGradientFill(this.fabricCanvas.getContext(),{
//        x1: info.points[0].x,
//        y1: info.points[0].y,
//        x2: info.points[info.points.length-1].x,
//        y2: info.points[info.points.length-1].y,
//        colorStops: {
//            0: '#000',
//            1: '#fff'
//        }
//    });



    this.drawObjects.joints[joint.drawID] = {index: this.fabricCanvas.getObjects().length, fabric: fabPolyLine, joint: joint, jointType: joint.m_type};

    //add it to our canvas

    //but joints get inserted before bodies (if any exist)
    this.fabricCanvas.insertAt(fabPolyLine,1);

};


//DELTEING BODIES AND JOINTS
boxNS.DrawingObject.prototype.removeBody = function(joint)
{
    if(this.zombieMode)
        return;
    console.log("unhandled delete body");
};

boxNS.DrawingObject.prototype.removeJoint = function(joint)
{
    if(this.zombieMode)
        return;
    console.log("unhandled delete joint");

};

//INFO for joints,bodies, etc

boxNS.DrawingObject.prototype.jointInfo = function(joint)
{
    var b1 = joint.GetBodyA();
    var b2 = joint.GetBodyB();
    var xf1 = b1.m_xf;
    var xf2 = b2.m_xf;
    var x1 =  boxNS.DrawingObject.scalePoint(xf1.position, this.drawScale);
    var x2 =  boxNS.DrawingObject.scalePoint(xf2.position, this.drawScale);

    var p1 = boxNS.DrawingObject.scalePoint(joint.GetAnchorA(), this.drawScale);
    var p2 = boxNS.DrawingObject.scalePoint(joint.GetAnchorB(), this.drawScale);
//    var color = b2World.s_jointColor;
    var info = {points:[]};
    switch (joint.m_type) {
        case b2Joint.e_distanceJoint:
            info.points.push(p1, p2);
            break;
        case b2Joint.e_pulleyJoint:
        {
            var pulley = ((joint instanceof b2PulleyJoint ? joint : null));
            var s1 = boxNS.DrawingObject.scalePoint(pulley.GetGroundAnchorA(), this.drawScale);
            var s2 = boxNS.DrawingObject.scalePoint(pulley.GetGroundAnchorB(), this.drawScale);
            info.points.push(p1, s1,s2,p2);
        }
            break;
        case b2Joint.e_mouseJoint:
            info.points.push(p1,p2);
            break;
        default:
            if (b1 != this.m_groundBody) info.points.push(x1);
            info.points.push(p1,p2);
            if (b2 != this.m_groundBody) info.points.push(x2);
    }

    return info;
};

//Shape info for a body, routes to the various circles/polygons
boxNS.DrawingObject.prototype.shapeInfo = function(dBody, shape)
{
    switch (shape.m_type) {
        case b2Shape.e_circleShape:
            return this.circleInfo(dBody, ((shape instanceof b2CircleShape ? shape : null)) );
        case b2Shape.e_polygonShape:
            return this.polygonInfo(dBody, ((shape instanceof b2PolygonShape ? shape : null)));
        case b2Shape.e_edgeShape:
            console.log("No shape info for this type");
            return null;
        default:
            console.log("Don't know this shape type, returning null info");
            return null;
    }
};
//circle shape
boxNS.DrawingObject.prototype.circleInfo = function(dBody, circle)
{
    if(!circle)
        return null;

    var cInfo = {center:b2Math.MulX(dBody.m_xf, circle.m_p) ,
        radius:this.drawScale*circle.m_radius };
    //need to scale the x and y
    cInfo.center.x *= this.drawScale;
    cInfo.center.y *= this.drawScale;

    return cInfo;
};
//polygon shape
boxNS.DrawingObject.prototype.polygonInfo = function(dBody, poly)
{
    var vertexCount = parseInt(poly.GetVertexCount());
    var localVertices = poly.GetVertices();
    var vertices = new Vector(vertexCount);
    for (var i = 0;
         i < vertexCount; ++i) {
        var b2Temp = b2Math.MulX(dBody.m_xf, localVertices[i]);
        vertices[i] = new b2Vec2(b2Temp.x*this.drawScale, b2Temp.y*this.drawScale);
    }

    return {vertices:vertices};
};



