using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SuperWebSocket;
using SuperSocket.SocketBase;
using Newtonsoft.Json.Linq;
using System.Runtime.Remoting.Messaging;

namespace Awesomium.sockets
{
    public delegate JObject SocketFunctionCall(JObject information);

    public enum SocketEventTypes
    {
        socketInfo,
        functionCall,
        functionReturn
    }
    public enum FunctionVariables
    {
        functionName,
        fArguments,
        objectID
    }

    class MasterSocketManager
    {
        static WebSocketServer appServer;

        static string EvaluatorSocket = "evaluator";
        static string DisplaySocket = "display";

        static Dictionary<string, List<int>> socketSessions = new Dictionary<string, List<int>>();
        //static Dictionary<int, string> socketSessionTypes = new Dictionary<int,string>();

        static Dictionary<WebSocketSession, int> sessionIdentifiers = new Dictionary<WebSocketSession, int>();
        static Dictionary<int, WebSocketSession> idToSessionDict = new Dictionary<int, WebSocketSession>();

        static Dictionary<string, SocketFunctionCall> socketFunctions = new Dictionary<string, SocketFunctionCall>();

        static int nextSocketID = 0;
        static int nextMessageID = 0;

        public static void LaunchWebsocketServer(int port)
        {
            appServer = new WebSocketServer();

            //Setup the appServer
            if (!appServer.Setup(port)) //Setup with listening port
            {
                Console.WriteLine("Failed to setup!");
                throw new Exception("Failed to setup websocket server");
            }


            appServer.NewMessageReceived += new SessionHandler<WebSocketSession, string>(appServer_NewMessageReceived);
            appServer.NewSessionConnected += new SessionHandler<WebSocketSession>(appServer_NewSessionConnected);
            //appServer.SessionClosed += new SessionHandler<WebSocketSession,CloseReason>


            //Try to start the appServer
            if (!appServer.Start())
            {
                Console.WriteLine("Failed to start!");
                throw new Exception("Failed to start websocket server");
            }
        }

        static void  appServer_NewSessionConnected(WebSocketSession session)
        {
            int sid = nextSocketID++;
            sessionIdentifiers.Add(session, sid);
            idToSessionDict.Add(sid, session);
        }

        public static void CloseServer()
        {
            //Stop the appServer
            appServer.Stop();
        }

        static void appServer_NewMessageReceived(WebSocketSession session, string message)
        {
            processMessage(sessionIdentifiers[session], message);
        }

        //there is a reason for mkaing this a separate function from the one called from the library -- keep logic separate from library insides
        static void processMessage(int socketID, string message)
        {

            //need to parse the message sent in
            //some custom message api info i guess
            try
            {

                JObject incoming = JObject.Parse(message);

                //handle socket info
                var socketEvent = incoming["event"].ToString();

                SocketEventTypes socketEventType;

                //there are only a few reasons we're being contacted
                //To announce what type of socket they are (display, evaluator, etc)
                //To request that a C# function be called
                //To return a request when a JS function was called

                //notice all the handlers take an identifier and the object
                //I'm trying to limit the amount of socket specific code in this class in the event of swapping the library

                if (Enum.TryParse<SocketEventTypes>(socketEvent, out socketEventType))
                {
                    switch (socketEventType)
                    {
                        case SocketEventTypes.socketInfo:
                            handleSocketInfo(socketID, incoming);
                            break;
                        //JS wants to make a C# function call, and return
                        case SocketEventTypes.functionCall:
                            handleSocketFunctionCall(socketID, incoming);
                            break;
                        //you made a call into a javascript function, and it has returned -- oh how delightful
                        case SocketEventTypes.functionReturn:
                            handleSocketReturnCall(socketID, incoming);
                            break;
                        default:
                            Console.WriteLine("Socket event type not implemented yet!");
                            Console.WriteLine(socketEventType.ToString());
                            break;

                    }
                }
                else
                {
                    Console.WriteLine("Invalid event type formatting");
                    Console.WriteLine(message);
                }


            }
            catch (Exception e)
            {
                Console.WriteLine("Invalid message formatting");
                Console.WriteLine(message);
            }
        }

        #region Handle Socket Identification
     
        static void handleSocketInfo(int socketID, JObject jsonIncoming)
        {
            var session = idToSessionDict[socketID];
            var socketType = jsonIncoming["data"]["socketType"].ToString();
            if (socketType == DisplaySocket)
            {
                addSessionObject(DisplaySocket, session);
            }
            else if (socketType == EvaluatorSocket)
            {
                addSessionObject(EvaluatorSocket, session);
            }
            else
            {
                throw new NotImplementedException("Don't know this socket type!");
            }
        }

        #endregion

        #region Handle Calling a C# Function for Information

        static void handleSocketFunctionCall(int socketID, JObject jsonIncoming)
        {
            var session = idToSessionDict[socketID];
            //we want to make a function call, and pass back information

            //we need an id to keep track of
            try
            {

                JToken mid;
                 AsyncCallback messageCallback;
                int messageID;

                if(!jsonIncoming.TryGetValue("messageID", out mid))
                {
                    //we aren't sending nuffin back ya hear?
                    messageID = -1;
                    messageCallback = new AsyncCallback(doNothing);   
                }
                else
                {
                    messageID = int.Parse(mid.ToString());
                    messageCallback = new AsyncCallback(returnMessage);
                }
                 

                var functionCallData = JObject.FromObject(jsonIncoming["data"]);

                string fName = functionCallData["functionName"].ToString();

                var stateObject = new JObject();

                stateObject.Add("socketID", sessionIdentifiers[session]);
                stateObject.Add("messageID", messageID);

                //add the socket identifier (we don't send function callbacks all willy nilly)
                if (socketFunctions.ContainsKey(fName))
                {
                    //asynchronously call the function, and return it's data -- if required -- we send on socket
                    socketFunctions[fName].BeginInvoke(functionCallData,
                      messageCallback, stateObject);
                }
                else
                {
                    Console.WriteLine("Function call going to no known event: " + fName);
                    Console.WriteLine(jsonIncoming.ToString());
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Invalid functionCall message formatting");
                Console.WriteLine(jsonIncoming.ToString());
            }
        }
        static void doNothing(IAsyncResult ar)
        {

        }
        static void returnMessage(IAsyncResult ar)
        {
            // Retrieve the delegate.
            AsyncResult result = (AsyncResult)ar;
            SocketFunctionCall caller = (SocketFunctionCall)result.AsyncDelegate;

            // Retrieve the format string that was passed as state  
            // information. 
            JObject messageAndSocketID = (JObject)ar.AsyncState;

            // Call EndInvoke to retrieve the results. 
            JObject returnValue = caller.EndInvoke(ar);

            if (returnValue == null)
                returnValue = new JObject();

            int messageID = (int)messageAndSocketID["messageID"];

            //now that we've returned this object
            //attach a messageID

            JObject socketMessage = new JObject();

            socketMessage.Add("messageID", messageID);
            socketMessage.Add("event", "functionReturn");
            socketMessage.Add("data", returnValue);
            int socketID = (int)messageAndSocketID["socketID"];

            Console.WriteLine("Sending: " + socketMessage.ToString());

            idToSessionDict[socketID].Send(socketMessage.ToString());
        }

        #endregion

        #region Handle Calling/Returning JS Function

        static void handleSocketReturnCall(int socketID, JObject jsonIncoming)
        {
            //we got a callback, can it be? how exciting
            Console.WriteLine("Incomgin return call on: " + socketID);
            Console.WriteLine(jsonIncoming.ToString());

            //who is it for?
            int messageID = int.Parse(jsonIncoming["messageID"].ToString());

            //let's grab the appropriate callback
            var cb = storedCallbacks[messageID];

            //now we make our callback with the data object

            JToken data;
            JObject passedData = null;

            if (jsonIncoming.TryGetValue("data", out data))
                passedData = JObject.FromObject(data);

            cb(passedData);
        }

        //make a call into JS with a potential callback in C#
        public static void callEvaluatorJS(string functionName, JArray args = null, string objectID = null, SocketFunctionCall callback = null)
        {
            prepareJSFunction(EvaluatorSocket, functionName, args, objectID, callback);
        }
        //make a call into JS with a callback in C# -- calling display object
        public static void callDisplayJS(string functionName, JArray args = null, string objectID = null, SocketFunctionCall callback = null)
        {
            prepareJSFunction(DisplaySocket, functionName, args, objectID, callback);
        }

        static void prepareJSFunction(string socketType, string functionName, JArray args = null, string objectID = null, SocketFunctionCall callback = null)
       {
            //let's build our object
            JObject formattedJSON = new JObject();
            
            formattedJSON.Add("event", SocketEventTypes.functionCall.ToString());

            JObject data = new JObject();
            
            //add our function name, and the function arguments
            data.Add(FunctionVariables.functionName.ToString(), functionName);

            if(args != null)
                data.Add(FunctionVariables.fArguments.ToString(), args);
            
            //finally, the objectID we're looking to call
            //if none provided, we actually just call the global function name
            if(objectID != null)
                data.Add(FunctionVariables.objectID.ToString(), objectID);

            //attach data to json object
            formattedJSON.Add("data", data);

            callJSWithReturn(socketType, formattedJSON, callback);

        }

        #endregion


        #region Register Available Callbacks

        public static void registerCallback(string functionName, SocketFunctionCall callback)
        {
            if (socketFunctions.ContainsKey(functionName))
                socketFunctions[functionName] = callback;
            else
                socketFunctions.Add(functionName, callback);
        }
        #endregion

        #region Register Websocket types

        static void addSessionObject(string type, WebSocketSession socket)
        {
            List<int> sessionObjects;

            if(!socketSessions.TryGetValue(type, out sessionObjects))
            {
                sessionObjects = new List<int>();
                socketSessions.Add(type, sessionObjects);
            }

            int socketID = sessionIdentifiers[socket];

            if (!sessionObjects.Contains(socketID))
                sessionObjects.Add(socketID);
                
        }
        #endregion


        //public void callJavascriptDisplayFunction(string functionName, string arguments = null)
        //{
        //    callJS(DisplaySocket, functionName, arguments);
            
        //}
        //public void callJavascriptEvaluatorFunction(string functionName, string arguments = null)
        //{
        //    callJS(EvaluatorSocket, functionName, arguments);
        //}
        static Dictionary<int, SocketFunctionCall> storedCallbacks = new Dictionary<int,SocketFunctionCall>();

        static void callJSWithReturn(string socketType, JObject message, SocketFunctionCall callback = null)
        {           
            List<int> socketsToCall;
            
            var messageID = nextMessageID++;
            message.Add("messageID", messageID);

            if (socketSessions.TryGetValue(socketType, out socketsToCall))
            {
                for (int i = 0; i < socketsToCall.Count; i++)
                {
                    var sid = socketsToCall[i];

                    //if we have a callback, it needs to be stored for each message sent
                    if (callback != null)
                    {
                        if (i > 0)
                        {
                            messageID = nextMessageID++;
                            message["messageID"] = messageID;
                        }
                       
                        storedCallbacks.Add(messageID, callback);
                    }

                    //message id notes, off we go!
                    idToSessionDict[sid].Send(message.ToString());
                }
            }
        }

        //static string functionCallMessage(string functionName, string arguments = null)
        //{
        //      JObject fCall = new JObject();
        //    fCall.Add("type", "function");
        //    fCall.Add("functionName", functionName);

        //    if(arguments != null)
        //        fCall.Add("arguments", arguments);


        //    return fCall.ToString();
        //}


    }
}
