var socket = require('socket.io')
var express = require('express')
    , http = require('http');

var assert = require("assert");
var Browser = require("zombie");
var browser = new Browser({debug:true, runScripts:true});

var pauseEvaluation = false;

var lastRequestObject = {};// = 0;

var app = express();

var backADirectory = function(directory)
{
    if(!directory.lastIndexOf("\\"))
        return directory;

    return directory.substring(0,directory.lastIndexOf("\\"));
}
var minsDirectory = backADirectory(backADirectory(__dirname)) + '/html';
//
//var util = require('util'),
//    connect = require('connect'),
//    port = 1337;
//
//connect.createServer(connect.static(minsDirectory)).listen(port);
//util.puts('Listening for MINS on ' + port + '...');
//util.puts('Press Ctrl + C to stop.');
//
//app.configure(function(){
    app.set('port', process.env.PORT || 3000);
//    app.use(express.cookieParser());
//    app.use(express.bodyParser());
//});

//app.configure('development', function(){

//app.use(express.static(backADirectory(__dirname) + '/MINS.js'));
    app.use(express.errorHandler());
    app.use(express.cookieParser());

    app.use(express.bodyParser());

var server = http.createServer(app);

var io = socket.listen(server, {origins: '*:*', 'log level':2});

server.listen(app.get('port'), function(){
    console.log("Express server listening on port " + app.get('port'));
});

//});

var addGlobalContext = function(res)
{
    res.header("Access-Control-Allow-Origin", "*");
    res.header("Access-Control-Allow-Headers", "X-Requested-With");
}
var getAvailableSocket = function()
{
    var availableSocket;
    for(var key in availableSockets){
        availableSocket = availableSockets[key];
        break;
    }
    return availableSocket;
}
app.get('/getEvaluationPage', function (req, res) {
//    console.log(__dirname);
//    console.log(minsDirectory + '/Evolution/SingleEvaluation.html');
    res.sendfile(minsDirectory + '/evolution/evaluate/SingleEvaluation.html');
});
var browserVisited = false;
var loadedBrowser;
//this is the funciton that does the actual zombie evaluation
//you don't need this to be hooked into the request directly (zombieEvaluation page is just an http way of accessing this function
//we also want to be able to access it from sockets as well.
var zombieEvaluationProcess = function(requiredGenomeEvaluations, successFunction, failureFunction)
{
//    console.log('Attempting zombie eval');
    var availableSocket = getAvailableSocket();
    if(availableSocket)
    {
//        console.log('Socket acquired for zombie eval');
        availableSocket.emit('getGenomes', requiredGenomeEvaluations , function(genomeObjects)
        {

//            console.log('Genomes acquired time for  zombie eval');

            //Now, we must take the genome objects, and bada bing get our evaluations

            //if we haven't gone and fetched our page into memory, go ahead and do so
            //otherwise, we're back for more, load up our already enabled browser, and badabing badaboom!
            if(!browserVisited)
            {
                //so let's call the zombie browser
                browser.visit("http://localhost:3000/getEvaluationPage", function (err, browser) {

    //                console.log('Zombie started');
                    if(err){
                        failureFunction(err);
                        return;
                    }
                    loadedBrowser = browser;
                    browserVisited = true;
                    runZombieBrowserEval(genomeObjects, loadedBrowser, successFunction, failureFunction);

                });
            }
            else
            {
                runZombieBrowserEval(genomeObjects, loadedBrowser, successFunction, failureFunction);
            }


        });
    }
}
var runZombieBrowserEval = function(genomeObjects, browser, successFunction, failureFunction)
{
    if(pauseEvaluation)
    {
        setTimeout(function()
        {
            console.log('Delaying zombie run');
            runZombieBrowserEval(genomeObjects, browser, successFunction, failureFunction);

        }, 100);

        return;
    }
    //check for errors?
    assert.ok(browser.success);

    //this will return a javascript object/dictionary,
    //indexed by genome, and their behaviors
    browser.document.window.headlessEvaluateGenomeBehaviors(genomeObjects, function(err, behaviorDictionary)
    {
        console.log('Zombie eval completed!');
        //catch some errors yo!
        if(err){

            console.log('failed zombie evaluation');
            return failureFunction(err);
        }

        console.log('Evaluation is a success');
        //send back our behaviors yo!
        successFunction(behaviorDictionary);

    });
}

app.get('/zombieEvaluation', function (req, res) {
//    console.log(__dirname);
//    console.log(minsDirectory + '/Evolution/SingleEvaluation.html');

//   console.log('Query: ');
//    console.log(req.query);

    if(req.query['genomeIDArray'])
    {
        var requiredGenomeEvaluations = req.query['genomeIDArray'];
        zombieEvaluationProcess(requiredGenomeEvaluations, function(behaviorDictionary)
        {

            console.log('Success evaluation');
            //our success function
            res.send(behaviorDictionary);
            res.end();
        }, function(err)
        {
            console.log('Failed evaluation');
           //our error function
            returnError(res, 'Fail evaluation: ' + err);
        });
    }
    else {
        console.log('Body: ');
        console.log(req.body);

        returnError(res, 'No genomeIDArray specified, fail evaluation.');
    }
});

app.post('/toggle', function(req,res)
{
//    console.log('Toggle post received');
//    console.log(req.body);
    addGlobalContext(res);

    if(req.body){

        console.log('Toggle post with information. Processing...');
        var availableSocket = getAvailableSocket();
        if(availableSocket)
        {
            console.log('Post to toggle with ID ' + req.body.genomeID);
            var bodyID = req.body.genomeID;
            //we have a body id, lets pass it to our toggle function, and see what we get back
            availableSocket.emit('toggleNetwork', bodyID, "alternativeArgument" , function(returnData)
            {
                console.log("Passing on toggle message");
                //console.log(stuff);
                res.send(returnData);
                res.end();
            });
        }
        else returnError(res, 'No sockets available! Process fail.');
    }
    else returnError(res, 'No genomeID specified, fail toggle.');
});

app.get('/get', function (req, res) {

    addGlobalContext(res);
    //first thing we do is fix up our response to allow for cross-domain requests
//    res.setHeader("Access-Control-Allow-Origin", "*");
//    res.setHeader("Access-Control-Allow-Headers", "X-Requested-With");
//    res.end();

    console.log('Making my way to the get function!');
    var availableSocket;
    for(var key in availableSockets){
        availableSocket = availableSockets[key];
        break;
    }
    if(availableSocket)
    {
        console.log("Sending ping message to socket");
        //console.log(availableSocket);

        availableSocket.emit('getBodies', 1, function(returnData)
        {
            console.log("RECEIVING MESSAGE: ");
            //console.log(stuff);

            res.send(returnData);
            res.end();

        });

    }
    else
        returnError(res,'No available sockets! Process failed!');

});
app.get('/getBodies', function (req, res) {

    addGlobalContext(res);
    console.log('Some bodies yo');
    if(req.query){

        console.log('Getting multiple bodies. Processing...');
        var availableSocket;
        for(var key in availableSockets){
            availableSocket = availableSockets[key];
            break;
        }
        if(availableSocket)
        {
            console.log('Post to get body with count ');console.log( req.query.numberOfBodies);

            //we have a body id, lets pass it to our toggle function, and see what we get back
            availableSocket.emit('getBodies', parseInt(req.query.numberOfBodies), function(returnData)
            {
                console.log("Passing on getBodies message");
                //console.log(stuff);
                try{
                    res.send(returnData);
                    res.end();
                }
                catch(e)
                {
                    console.log('Attempted to send, but caught some stupid ass error, just end the response and error handle client side. Fuck you socketio.');
                    res.end();
                }


            });
        }
        else
            returnError(res,'No available sockets! Process failed!');
    }
    else
        returnError(res,'Number of bodies not specified!');

});

app.get('/getGenomes', function (req, res) {

    addGlobalContext(res);
    console.log('Some genomes requested yo');

    if(req.query['genomeIDArray'])
    {
        var requiredGenomeEvaluations = req.query['genomeIDArray'];

        console.log('Getting multiple bodies by GenomeID. Processing...');
        var availableSocket = getAvailableSocket();

        if(availableSocket)
        {
//            console.log('Post to get body with count ');console.log( req.body.genomeArray);

            //we have a body id, lets pass it to our toggle function, and see what we get back
            availableSocket.emit('getGenomes', requiredGenomeEvaluations, function(returnData)
            {
//                console.log("Passing on getGenomes message");
                //console.log(stuff);
                try{
                    res.send(returnData);
                    res.end();
                }
                catch(e)
                {
                    res.end();
                }


            });
        }
        else
            returnError(res,'No available sockets! Process failed!');
    }
    else
        returnError(res,'GenomeID Array not specified!');

});
app.get('/getArchive', function (req, res) {

    addGlobalContext(res);
    console.log('Checking the archive...');
    var availableSocket = getAvailableSocket();
    if(availableSocket)
    {
        //we have a socket, let's call for archive, and see what we get back
        availableSocket.emit('getArchiveIDs',"", function(returnData)
        {

            console.log('Returning archive IDs: ');
            console.log(returnData);

            try{

                res.send(returnData);
                res.end();
            }
            catch(e)
            {
                res.end();
            }


        });
    }
    else
        returnError(res,'No available sockets! Process failed!');


});
app.get('/getBestBodies', function (req, res) {

    addGlobalContext(res);
    console.log('Checking the best genomes...');
    var availableSocket = getAvailableSocket();
    if(availableSocket)
    {

        var uid = req.query.uid;

        if(!lastRequestObject[uid])
            lastRequestObject[uid] = 0;

        //we have a socket, let's call for archive, and see what we get back
        availableSocket.emit('getBest', lastRequestObject[uid], function(returnData)
        {

            console.log('Returning best gen bodies: ');
            console.log(returnData);

//            var parse = JSON.parse(returnData);
//            console.log('Parsed');
//            console.log(returnData);

            lastRequestObject[uid]  = returnData["First"];

//            console.log('First');
//            console.log(returnData["First"]);
//            console.log('Second');
//            console.log(returnData["Second"]);

            try{
                res.send(returnData["Second"]);
                res.end();
            }
            catch(e)
            {
                res.end();
            }


        });
    }
    else
        returnError(res,'No available sockets! Process failed!');
});



var emitToAvailableSocket = function(eventName, param1, param2, param3, param4, param5, req, res)
{
//    pauseEvaluation = true;
    addGlobalContext(res);
    console.log('Running the PCA...');
    var availableSocket = getAvailableSocket();
    if(availableSocket)
    {
        //we have a socket, let's call for archive, and see what we get back
        availableSocket.emit(eventName, param1, param2, param3, param4, param5, function(returnData)
        {
            try{
                res.send(returnData);
                res.end();
            }
            catch(e)
            {
                res.end();
            }

            //stop any evaluation delays
//            pauseEvaluation = false;
        });
    }
    else
        returnError(res,'No available sockets! Process failed!');
}

app.get('/runFullPCA',function (req, res) {

    var behaviorToUse = req.query["firstBehavior"];
    var xBins = req.query["xBins"];
    var yBins = req.query["yBins"];
    var selector = req.query["selector"];
    var percent = req.query["percent"];

    emitToAvailableSocket('runFullPCA', behaviorToUse, xBins, yBins, selector, percent, req, res);

});
app.get('/runPCA', function (req, res) {
    emitToAvailableSocket('runPCA', 0 , 0 ,0,0,0 , req, res);
});


app.get('/getCurrentGeneration', function (req, res) {

    addGlobalContext(res);
    console.log('Checking the archive...');
    var availableSocket = getAvailableSocket();
    if(availableSocket)
    {
        //we have a socket, let's call for archive, and see what we get back
        availableSocket.emit('getCurrentIDs',"", function(returnData)
        {

            console.log('Returning generation IDs: ');
            console.log(returnData);

            try{

                res.send(returnData);
                res.end();
            }
            catch(e)
            {
                res.end();
            }


        });
    }
    else
        returnError(res,'No available sockets! Process failed!');
});


app.get("*", function(req, res){
    console.log('loading: ' + minsDirectory +  req.url);
    res.sendfile(minsDirectory  +  req.url)
//    var from = req.params[0];
//    var to = req.params[1] || 'HEAD';
//    res.send('commit range ' + from + '..' + to);
});

function returnError(response, message)
{
    console.log('Returning error: ' +  message);
    response.writeHead(500, {'Content-Type': 'text/plain'});
    response.end("Server error: "+ message);
    pauseEvaluation = false;
};



var availableSockets = {};
var socketCount = 0;



io.sockets.on('connection', function (socket) {

    var id = socketCount++;
    socket.set('id', id, function(){});

    availableSockets[id] = socket;

    socket.emit('news', { hello: 'world' });

    socket.on('my other event', function (data) {
        console.log(data);
    });

    socket.on('evaluateGenomes', function(data, retFunction)
    {
//        console.log('Evaluating Genomes: ' );
//        console.log(JSON.parse(data));

        //we should make a call to our own self, ummmmm
        zombieEvaluationProcess(JSON.parse(data), function(behavior)
        {

            console.log('Success evaluation');
            //success! Return our genomes!
            //socket.emit('evaluateGenomes', behavior);
            retFunction(behavior);

        }, function(err)
        {
            //fail, doh!
            console.log('Error(!): ');
            console.log(err);

            retFunction(err);
//            socket.emit('evaluateGenomes', err);

        });
    });



    socket.on('pingServer', function (data) {
        socket.emit('pongServer', "pong from node server");
    });

    socket.on('disconnect', function (data) {
        socket.get('id', function(err, dID)
        {
            console.log("Deleting socket: " + dID);
            delete availableSockets[dID];
        });
    });
});


/*
*     app.set('views', __dirname + '/views');
 app.set('view engine', 'jade');
 app.use(express.favicon());
 app.use(express.logger('dev'));
 app.use(express.bodyParser());
 app.use(express.methodOverride());
 app.use(express.cookieParser('your secret here'));
 app.use(express.session());
 app.use(app.router);
 app.use(require('less-middleware')({ src: __dirname + '/public' }));
 app.use(express.static(__dirname + '/public'));
*
* */
