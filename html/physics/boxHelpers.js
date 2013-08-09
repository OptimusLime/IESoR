
var bHelpNSS = "BoxHelpers";
var bHelpNS = namespace(bHelpNSS);



bHelpNS.ContainedWorld = function(intervalRate, adaptive, width, height, scale, yGravity, sleep, callbacks ) {


        var b2Vec2 = Box2D.Common.Math.b2Vec2;
        var b2BodyDef = Box2D.Dynamics.b2BodyDef;
        var b2Body = Box2D.Dynamics.b2Body;
        var b2FixtureDef = Box2D.Dynamics.b2FixtureDef;
        var b2Fixture = Box2D.Dynamics.b2Fixture;
        var b2World = Box2D.Dynamics.b2World;
        var b2MassData = Box2D.Collision.Shapes.b2MassData;
        var b2PolygonShape = Box2D.Collision.Shapes.b2PolygonShape;
        var b2CircleShape = Box2D.Collision.Shapes.b2CircleShape;
        var b2FilterData = Box2D.Dynamics.b2FilterData;
        var b2DebugDraw = Box2D.Dynamics.b2DebugDraw;
        var b2RevoluteJointDef = Box2D.Dynamics.Joints.b2RevoluteJointDef;
        var b2DistanceJointDef = Box2D.Dynamics.Joints.b2DistanceJointDef;
        var b2MouseJointDef =  Box2D.Dynamics.Joints.b2MouseJointDef;


    this.callObject = callbacks.object;
    this.addBodyCallback = callbacks.addBody || function(){};
    this.removeBodyCallback = callbacks.removeBody || function(){};
    this.addJointCallback = callbacks.addJoint || function(){};
    this.removeJointCallback = callbacks.removeJoint || function(){};





    this.canvasWidth = width;
    this.canvasHeight = height;

    this.scaleUp = Math.sqrt(this.canvasWidth*this.canvasWidth+ this.canvasHeight*this.canvasHeight)/Math.sqrt(2*230*230);

    this.intervalRate = parseInt(intervalRate);

    this.simulationRate = 1/parseInt(intervalRate);

    this.adaptive = adaptive;
    this.scale = scale;

    this.bodiesMap = {};
    this.bodiesList = [];
    this.jointsList = [];
    this.muscles = [];

    this.worldObjects = {};

    this.world = new b2World(
        new b2Vec2(0, (yGravity ===undefined ? 10 : yGravity))    //gravity
        ,  (sleep === undefined  ? true : sleep)                //allow sleep
    );

    this.fixDef = new b2FixtureDef;
    this.fixDef.density = 25.0;
    this.fixDef.friction = 1.0;
    this.fixDef.restitution = 0.1;
    this.fixDef.fixedRotation = true;
//    this.fixDef.linearDamping = 1.1;
    var rad = Math.PI;

    this.interpolation = 0;
    this.lastTime = Date.now();
    this.realTime = true;

    var accumulator = 0;


//based on principles in here:
//http://gafferongames.com/game-physics/fix-your-timestep/

    this.update = function(msSimRate, props) {

        props = props || {};

        var logEvents = '';
        var stepCount =0;

        if(msSimRate)
            this.realTime = false;

        var currentTime = (msSimRate) ? this.lastTime + msSimRate : Date.now();

        if(!this.realTime && !msSimRate)
        {
            //we aren't in real time, but now we are! quickly, reset our lastTime variable
            this.lastTime = Date.now();

            //and make sure we're in real time now
            this.realTime = true;
        }

        //# of seconds since last time
        var frameTime = (currentTime - this.lastTime)/1000;

        //maximum frame time, to prevent what is called the spiral of death
        if(frameTime > .35)
            frameTime = .35;


        //we don't need last time anymore, set it to the current time
        this.lastTime = currentTime;

        //we accumulate all the time we haven't rendered things in
        accumulator += frameTime;

//        console.log('Frame time: ' + frameTime + ' accumulator: ' + accumulator);

//        console.log('Pre acc');
        while(accumulator >= this.simulationRate)
        {
            stepCount++;
            //push the muscles outward a bit
            var speedup = 3;
//            if(!props.visual)
//                console.log('-- pre  muscles');
            logEvents += '--Pre muscles';
            for(var i=0; i < this.muscles.length; i++){
                var muscle = this.muscles[i];
                muscle.SetLength(muscle.m_length + muscle.amplitude/this.scale*Math.cos(rad + muscle.phase*2*Math.PI));
            }

            //step the physics world
//            if(!props.visual)
//                console.log('Post muscle, pre setep');
            logEvents += 'Post muscle, Pre step';
            this.world.Step(
                this.simulationRate   //frame-rate
                ,  10       //velocity iterations
                ,  10       //position iterations
            );

//            if(!props.visual)
//                console.log('Post sime');

            logEvents += 'Post sime, Pre force';
            this.world.ClearForces();

//            console.log('Post forces');
//            if(!props.visual)
//                console.log('Post forces');

            logEvents += 'Post forces--';
            //increment the radians for the muscles
            rad += speedup*this.simulationRate;

            //decrement the accumulator - we ran a chunk just now!
            accumulator -= this.simulationRate;

//            console.log(logEvents);
        }
//        if(!props.visual)
//            console.log('Post acc');
        logEvents += 'Post acc';
        this.interpolation = accumulator/this.simulationRate;
        //console.log("Partial: " +  this.interpolation);
        //console.log(rad*180/Math.PI);
//        console.log('Step count: ' + stepCount + ' dTime: ' + (Date.now() - currentTime));
//        console.log(logEvents);

        return {stepCount: stepCount, deltaChange: (Date.now() - currentTime), log:logEvents};
    };

    //always returns SOMETHING, even if nothing is calculated
    this.nodesCenterOfMass = function(print)
    {
        //grab all of the bodies that aren't static, and tell me the center of mass
        //right now, we assume 1 object per world, which is okay if it's a "small" world.

        var x = 0;
        var y = 0;
        var nodeBodyCount = 0;
        var nodeLocations = [];

        //grab body and shape first
        for(var i=0; i < this.bodiesList.length;i++)
        {
            var body = this.bodiesList[i];
            if(print)
            {
//                console.log('body: ' );
//                console.log(body);
            }
//            console.log('Body: ');
//            console.log(body);

            var shape = body.GetFixtureList().GetShape();
            if(print)
            {
//                console.log('Shape: ' );
//                console.log(shape);
            }
            var centerInfo = this.callObject.shapeInfo(body, shape);
            if(print)
            {
                console.log('Centerinfo: ' );
                console.log(centerInfo);
                console.log(centerInfo.center);
            }
            if(centerInfo.center)
            {
                nodeLocations.push(centerInfo.center);
                x += centerInfo.center.x;
                y += centerInfo.center.y;

                if(print)
                {
                    console.log('X: ' + x + 'Y: ' + y);
                }
                nodeBodyCount++;
            }


        }
        //if nothign was calculated, then x still = 0, and y = 0 => 0/1 = 0, 0/1 = 0, and we avoid an undefined
        if(print)
        {
            console.log('Nodebodycount: ' + nodeBodyCount);
        }
        if(nodeBodyCount == 0)
            nodeBodyCount =1;

        return {x: x/nodeBodyCount, y: y/nodeBodyCount, nodeLocations:nodeLocations};
    }


    this.jsonParseMINS = function(jsonDoc, documentType)
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
                entities[nodeObj.id] = (Entity.build({id:nodeObj.id, x: parseFloat(nodeObj.x)/this.scale - this.canvasWidth/2, y: this.canvasHeight -parseFloat(nodeObj.y)/this.scale,
                    radius: .5 }));
            }
        }
        //push our bodies into the system so that our joints have bodies to connect to
        this.setBodies(entities);

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
                        var dJoint = this.addDistanceJoint(linkObj.a, linkObj.b, {frequencyHz: 3, dampingRatio:.3});
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
                       var mJoint = this.addMuscleJoint(musObj.a, musObj.b, {amplitude: parseFloat(musObj.amplitude)});//, phase: parseFloat(musObj.phase)});//{frequencyHz: 5, dampingRatio:.3 });
                       mJoint.SetLength(parseFloat(musObj.restlength)/this.scale);
                    }

                    break;
            }

        }




    };

    this.polarToCartesian= function(r, theta, rScale, shift)
    {
        return {x:  (r+1)/2*Math.cos(theta*Math.PI)*rScale + shift.x, y: (r+1)/2*Math.sin(theta*Math.PI)*rScale + shift.y };
    };

    this.jsonParseNodeApp = function(jsonData)
    {
        //All the bodies are inside of nodes
        //should all be of type "mass" or "node"

        var inLocations = jsonData.fromJS ? 'inputLocations' : 'InputLocations';
        var hidLocations = jsonData.fromJS ? 'hiddenLocations' : 'HiddenLocations';
        var connLocation = jsonData.fromJS ? 'connections' : 'Connections';
        var x = jsonData.fromJS ? 'x' : 'X';
        var y = jsonData.fromJS ? 'y' : 'Y';
        var src = jsonData.fromJS ? 'sourceID' : 'SourceNeuronId';
        var tgt = jsonData.fromJS ? 'targetID' : 'TargetNeuronId';

        //behave differently if you can from javascript or c#, duh!
        var oNodes = jsonData[inLocations].concat(jsonData[hidLocations])

        //different name if you come from c# or js
        var connections =  jsonData[connLocation];

        var oBodyCount = this.bodiesList.length;
        var bodyID = this.bodiesList.length;

        console.log('Conns: ' + connections.length +
            ' Nodes: ' + oNodes.length);


        var useLEO = jsonData.useLEO;

//        console.log('JSON Data: ');
//        console.log(jsonData);

        var entities = {};
        var xScaled,yScaled;
        var divideForMax = 2.2;
        var divideForMaxHeight = 2.5;
        var maxAllowedWidth = this.canvasWidth/divideForMax;
        var maxAllowedHeight = this.canvasHeight/divideForMaxHeight;

        var minX = this.canvasWidth; var maxX = 0;
        var minY = this.canvasHeight; var maxY = 0;


//        console.log('No prob');
        for(var nodeKey in oNodes)
        {
            if (Object.prototype.hasOwnProperty.call(oNodes, nodeKey)) {
                // prop is not inherited

            //node type doesn't matter as much as our documentType

           // var aBodies = oNodes[nodeType];

            //for(var b=0; b < aBodies.length; b++)
           // {
             //   var nodeObj = aBodies[b];
            var nodeLocation = oNodes[nodeKey];

                var nodeX = nodeLocation[x];
                var nodeY= nodeLocation[y];

                //characterization of novelty, novlety + local search
                //look at polar, why did i do it????
                //bias local connectrions with leo

//                var maxR = Math.sqrt(this.canvasWidth*this.canvasWidth + this.canvasHeight*this.canvasHeight)/4;

//                var polarScaled = this.polarToCartesian(parseFloat(nodeLocation.X), parseFloat(nodeLocation.Y), maxR, {x: this.canvasWidth/2, y: this.canvasHeight/2});
//            console.log('Polar scaled: ');
//                console.log( {x: parseFloat(nodeLocation.X), y: parseFloat(nodeLocation.Y)});
//            xScaled = polarScaled.x;// (parseFloat(nodeLocation.X) +1)*300;
                xScaled = (parseFloat(nodeX) +1)*maxAllowedWidth;
//            yScaled = polarScaled.y;//(parseFloat(nodeLocation.Y) +1)*200;
                yScaled = (parseFloat(nodeY) +1)*maxAllowedHeight;
              //FOR each node, we make a body with certain properties, then increment count
                entities[bodyID] = (Entity.build({id:bodyID, x: xScaled, y: yScaled, radius: .5 }));


                minX = Math.min(minX, xScaled);
                maxX = Math.max(maxX, xScaled);

                minY = Math.min(minY, yScaled);
                maxY = Math.max(maxY, yScaled);

            //need to increment the body id so we don't overwrite previous object
            bodyID++;
//                console.log('No prob1');
            }
            //}
        }

        var moveX = (maxX- minX)/2;
        var moveY = (maxY- minY)/2;

        //need to rescale and move x,y coordinates for entities
        //have to divide by scale, and also center the object, no one should get an unfair advantage
        for(var bid in entities)
        {
            var entity = entities[bid];

//            console.log('Xbefore: ' + entity.x + ' ybefore: ' + entity.y + ' Min/Max X: (' + minX + ', ' + maxX + ') '  + ' Min/Max Y: (' + minY+ ', ' + maxY+ ') ');
            entity.x = (entity.x - minX + this.canvasWidth/2 - moveX);
            entity.y = (entity.y - minY + this.canvasHeight/2 - moveY);

//            console.log('Xscale: ' + entity.x + ' Yscal: ' + entity.y);


            entity.x /= this.scale;
            entity.y /= this.scale;
//            console.log('Xafter: ' + entity.x + ' yafter: ' + entity.y);
        }

        //push our bodies into the system so that our joints have bodies to connect to
        this.setBodies(entities);

        var amplitudeCutoff =.2;

        var connectionDistanceSum = 0;

//        var count =0;
        for(var connectionID in connections)
        {

            if (Object.prototype.hasOwnProperty.call(connections, connectionID)) {
                // prop is not inherited
//                console.log('No prob3');
                var connectionObject = connections[connectionID];

                var sourceID = oBodyCount + parseInt(connectionObject[src]);
                var targetID = oBodyCount + parseInt(connectionObject[tgt]);


                if(sourceID == targetID){
//                    console.log('We ignore self connections, since that is physically silly');
                    continue;
                }
                try
                {
                    var phaseIx = (useLEO ? 2 : 1);
                    var ampIx =  (useLEO ? 3 : 2);
                    var amp = (connectionObject.cppnOutputs[ampIx] +1 )/2;
//                    console.log('Phaseix: ' + phaseIx + ' AmpIx: ' + ampIx + ' useleo: ' + useLEO);

                    var connectionDistance = Math.sqrt(Math.pow(entities[sourceID].x - entities[targetID].x, 2) +  Math.pow(entities[sourceID].y - entities[targetID].y, 2));
                    connectionDistanceSum += connectionDistance;
//                    console.log('Amp dist: ' +.6*connectionDistance*amp + ' before: '+ this.scaleUp*amp);
//                    console.log('Amplitudes: ' + amp);
                    if(amp < amplitudeCutoff)
                        var dJoint = this.addDistanceJoint(sourceID, targetID);//, {frequencyHz: 3, dampingRatio:.3});
                    else//frequencyHz: 3, dampingRatio:.3,
                    //need to scale joints based on size of the screen - this is a bit odd, but should help multiple sizes behave the same!
                        var dJoint = this.addMuscleJoint(sourceID, targetID, {frequencyHz: 3, dampingRatio:.3, phase: connectionObject.cppnOutputs[phaseIx], amplitude:.3*connectionDistance*amp});

//                    var info = this.callObject.jointInfo(dJoint);

//                    console.log(info);

                }
                catch(e)
                {
                    console.error('HOLY POOP ERROR:');
                    console.log(e);
                    throw e;
                }

            }
        }


//        var startMorphology =
        return {width: maxX - minX, height: maxY - minY, startX: minX, startY: minY, mass: oNodes.length + connectionDistanceSum/2 };

    };
    this.addDistanceJoint = function(body1Id, body2Id, params) {
        var body1 = this.bodiesMap[body1Id];
        var body2 = this.bodiesMap[body2Id];
        var joint = new b2DistanceJointDef();
        joint.Initialize(body1, body2, body1.GetWorldCenter(), body2.GetWorldCenter());
        if (params && params['frequencyHz']) joint.frequencyHz = params['frequencyHz'];
        if (params && params['dampingRatio']) joint.dampingRatio = params['dampingRatio'];
        var wJoint =  this.world.CreateJoint(joint);
        //we push our joint into a list of joints created
        this.jointsList.push(wJoint);
        this.addJointCallback.call(this.callObject, wJoint);

        return wJoint;
    };
    this.addMuscleJoint = function(body1Id, body2Id, params) {
        params = params || {};
        var addedJoint = this.addDistanceJoint(body1Id, body2Id, params);
//        console.log('Phase: ' + params['phase'] + ' amp: ' + params['amplitude']);
        addedJoint.phase =  params['phase']|| 0;
        addedJoint.amplitude = params['amplitude'] || 1;
        //we push our muscles onto our muscle list
        this.muscles.push(addedJoint);
        return addedJoint;
    };

    this.setBody = function(entity)
    {
        var bodyDef = new b2BodyDef;

//        console.log("Adding entity:");
//        console.log(entity);

        if (entity.id == 'ground') {
            bodyDef.type = b2Body.b2_staticBody;
        } else {
            bodyDef.type = b2Body.b2_dynamicBody;
        }

        bodyDef.position.x = entity.x;
        bodyDef.position.y = entity.y;

//        bodyDef.linearVelocity.x = entity.vx;
//        bodyDef.linearVelocity.y = entity.vy;

//        bodyDef.SetUserData(entity.id);
//        bodyDef.friction = 1.0;
//        bodyDef.restitution = 0.1;
//        bodyDef.linearDamping = 1.1;

        bodyDef.linearDamping = 1.1;
        bodyDef.fixedRotation = true;

        bodyDef.userData = entity.id;
        bodyDef.angle = entity.angle;

        var body = this.world.CreateBody(bodyDef);
        body.SetUserData(entity.id);
//        console.log('User ID yo: ' + entity.id);

        var filter = new b2FilterData();
        filter.categoryBits = 0x0002;
        filter.maskBits = 0x0001 ;//0xFFFF;
        filter.groupIndex = 0;

        if (entity.radius) {
            this.fixDef.shape = new b2CircleShape(entity.radius);
           var fixture = body.CreateFixture(this.fixDef);
            fixture.fixedRotation = true;
            fixture.SetFilterData(filter);
        } else if (entity.polys) {
            for (var j = 0; j < entity.polys.length; j++) {
                var points = entity.polys[j];
                var vecs = [];
                for (var i = 0; i < points.length; i++) {
                    var vec = new b2Vec2();
                    vec.Set(points[i].x, points[i].y);
                    vecs[i] = vec;
                }
                this.fixDef.shape = new b2PolygonShape;
                this.fixDef.shape.SetAsArray(vecs, vecs.length);
                body.CreateFixture(this.fixDef);
            }
        } else {
            this.fixDef.shape = new b2PolygonShape;
            this.fixDef.shape.SetAsBox(entity.halfWidth, entity.halfHeight);
            body.CreateFixture(this.fixDef);
        }

        //we have to register our body AFTER we create the fixture to go with it!
        this.registerBody(body);
    };

    this.setBodies = function(bodyEntities) {
        var bodyDef = new b2BodyDef;
        for(var id in bodyEntities) {
            var entity = bodyEntities[id];
            this.setBody(entity);
        }
        this.ready = true;
    };

    this.registerBody = function(body) {
//        console.log(body.GetUserData());
        this.bodiesMap[body.GetUserData()] = body;
        this.bodiesList.push(body);
        this.addBodyCallback.call(this.callObject, body);
        return body;
    }
    var sentMessage = false;

    //dealing with the mouse clicks
    this.mouseDownAt = function(x, y) {
        if (!this.mouseJoint) {
            if(!sentMessage){
                console.log("Ignoring mouse click - single warning");
                sentMessage = true;
            }

            var body = null;//this.getBodyAt(x, y);
            if (body) {
                var md = new b2MouseJointDef();
                md.bodyA = this.world.GetGroundBody();
                md.bodyB = body;
                md.target.Set(x, y);
                md.collideConnected = true;
                md.maxForce = 300.0 * body.GetMass();
                this.mouseJoint = this.world.CreateJoint(md);
                body.SetAwake(true);
            }
        } else {
            this.mouseJoint.SetTarget(new b2Vec2(x, y));
        }
    }

    this.isMouseDown = function() {
        return (this.mouseJoint != null);
    }

    this.mouseUp = function() {
        this.world.DestroyJoint(this.mouseJoint);
        this.mouseJoint = null;
    }

    //we need to draw things with d3



};

//various entities for easy world creation
function Entity(id, x, y, angle, center, color, strength) {

    this.id = id;
    this.x = x;
    this.y = y;
    this.angle = angle || 0;
    this.center = center;
    this.color = color || "red";
    this.isHit = false;
    this.strength = strength;
    this.dead = false;
}

var NULL_CENTER = {x:null, y:null};

//builds the various entity types
Entity.build = function(def) {
    if (def.radius) {
        return new CircleEntity(def.id, def.x, def.y, def.angle, NULL_CENTER, def.color, def.strength, def.radius);
    } else if (def.polys) {
        return new PolygonEntity(def.id, def.x, def.y, def.angle, NULL_CENTER, def.color, def.strength, def.polys);
    } else {
        return new RectangleEntity(def.id, def.x, def.y, def.angle, NULL_CENTER, def.color, def.strength, def.halfWidth, def.halfHeight);
    }
};


function CircleEntity(id, x, y, angle, center, color, strength, radius) {
    color = color || 'aqua';

    Entity.call(this, id, x, y, angle, center, color, strength);
    this.id = id;
    this.x = x;
    this.y = y;
    this.angle = angle || 0;
    this.center = center;
    this.color = color || "red";
    this.isHit = false;
    this.strength = strength;
    this.dead = false;
    this.radius = radius;
}
CircleEntity.prototype = new Entity();
CircleEntity.prototype.constructor = CircleEntity;

function RectangleEntity(id, x, y, angle, center, color, strength, halfWidth, halfHeight) {
    Entity.call(this, id, x, y, angle, center, color, strength);
    this.id = id;
    this.x = x;
    this.y = y;
    this.angle = angle || 0;
    this.center = center;
    this.color = color || "red";
    this.isHit = false;
    this.strength = strength;
    this.dead = false;
    this.halfWidth = halfWidth;
    this.halfHeight = halfHeight;
}
RectangleEntity.prototype = new Entity();
RectangleEntity.prototype.constructor = RectangleEntity;

function PolygonEntity(id, x, y, angle, center, color, strength, polys) {
    Entity.call(this, id, x, y, angle, center, color, strength);
    this.id = id;
    this.x = x;
    this.y = y;
    this.angle = angle || 0;
    this.center = center;
    this.color = color || "red";
    this.isHit = false;
    this.strength = strength;
    this.dead = false;
    this.polys = polys;
}
PolygonEntity.prototype = new Entity();
PolygonEntity.prototype.constructor = PolygonEntity;
