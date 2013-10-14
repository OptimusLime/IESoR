var webSocketClient = function (socketType) {
    var self = this;

    var wsUri = "ws://localhost:4000";
    var websocket = new WebSocket(wsUri);

    var isOpen = false;

    var storedCallObjects = {};

    var SocketEventTypes =
    {
        socketInfo : "socketInfo",
        functionCall : "functionCall",
        functionReturn : "functionReturn"
    };

    var FunctionVariables =
    {
        functionName : "functionName",
        fArguments : "fArguments",
        objectID : "objectID"
    };

    var sendSocketType = function()
    {
        websocket.send(JSON.stringify({event: SocketEventTypes.socketInfo, data: {socketType: socketType}}));
    };

    websocket.onopen = function (evt) {
        isOpen = true;

        sendSocketType();

        if (self.onOpen) {
            self.onOpen(evt);
        }


    };

    websocket.onclose = function (evt) {
        isOpen = false;
        if (self.onClose) {
            self.onClose(evt);
        }
    };

    var nextMessageID = 0;
    var messageCallbacks = {};
    var nextCallbackID = 0;


    self.registerStoredObject = function(id, obj)
    {
        storedCallObjects[id] = obj;
    };

    //just a nice default object to have
    self.registerStoredObject("window", window);

    //args is supposed to be an array type
    self.callNativeFunction = function(fName, args, callback)
    {
        if(!isOpen)
        {
            console.log('Unanswered native function call');
            return;
        }

        //don't have to send in args, could just be callback
        if(typeof args === 'function')
        {
            callback = args;
            args = [];
        }

        var message = {event: SocketEventTypes.functionCall, data: {}};

        if(callback)
        {
            var messageID = nextMessageID++;
            message.messageID = messageID;
            messageCallbacks[messageID] = callback;
        }

        message.data[FunctionVariables.functionName] = fName;

        //only args if we've got args -- otherwise empty please!
        message.data[FunctionVariables.fArguments] = args || [];

        //send our message over for native processing -- with or without callback
        websocket.send(JSON.stringify(message));
    };

    //we do a little wiggle to send the request to the correct function
    //in a timely manner of course
    var handleFunctionRequest = function(jsonMessage, callbackReturn)
    {
        var data = jsonMessage.data;

        var args = data[FunctionVariables.fArguments];
        var fn = data[FunctionVariables.functionName];
        var callID = data[FunctionVariables.objectID];

        var callObject = (callID === undefined ? window : storedCallObjects[callID]);

        if(!callObject)
        {
            console.log('Error processing C# function request: invalid callobject id');
            return;
        }
        else if(callObject[fn] === undefined)
        {
            console.log('Error processing C# function request: function does not exist');
            return;
        }

        //we have a call object, let's make the call
        //async callbacks
        setTimeout(function()
        {
            //easily call a global function -- need help calling objects
            var results = callObject[fn].apply(self, args);

            callbackReturn(results);

        },0);
    };

    //pretty simple, if we got something returned,
    //check the message id, that's where we goin' yo
    var handleFunctionReturned = function(jsonReturn)
    {
        if(jsonReturn.messageID != undefined)
        {
            //send our callback with the data plz if it exists
            if(messageCallbacks[jsonReturn.messageID])
                messageCallbacks[jsonReturn.messageID](jsonReturn);
        }
    };

    websocket.onmessage = function (evt) {


        if (self.onMessage) {
            self.onMessage(evt.data);
        }

        try
        {
            var jsonMessage;
            if(typeof evt.data === 'object')
            {
                jsonMessage = evt.data;
            }
            else if(typeof evt.data === 'string')
            {
                jsonMessage = JSON.parse(evt.data);
            }

            var socketType = jsonMessage.event;

            switch(socketType)
            {
                //C# is requesting a function be called on the JS side of things
                case SocketEventTypes.functionCall:

                    var messageID = jsonMessage.messageID;

                    handleFunctionRequest(jsonMessage, function(finishedResults)
                    {
                        //we know what to send back
                        var jsonReturn = {messageID: messageID, event: SocketEventTypes.functionReturn, data: finishedResults};

                        //send information back with appropriate information, and message identification
                        websocket.send(JSON.stringify(jsonReturn));
                    });
                    break;
                //C# has returned our information to us
                case SocketEventTypes.functionReturn:

                    //we simply use the messageID to pass it on to the relevant target object
                    handleFunctionReturned(jsonMessage);

                    break;
                default:
                    console.log('Unknown socket type received: ' + JSON.stringify(jsonMessage));
                    break;
            }

        }
        catch(e)
        {
            console.log('Error parsing message, invalid json format!');
        }


        //                var reader = new FileReader();
        //
        //                reader.onloadend = function() {
        //
        //                    console.log('finished load!');
        //                    if(self.onMessage)
        //                    {
        //                        console.log('Received message!');
        //                        self.onMessage(JSON.parse(reader.result));
        //                    }
        //                };
        //
        //                console.log(evt.data);
        //
        //
        //                reader.readAsText(evt.data);


    };

    websocket.onerror = function (evt) {
        if (self.onError) {
            self.onError(evt);
        }
    };






};