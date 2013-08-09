using System;
using System.Collections.Generic;
using System.Text;
using SharpNeatLib.Evolution;
using System.IO;
using System.Xml;
using SharpNeatLib.NeuralNetwork;
using SharpNeatLib.NeatGenome;

namespace SharpNeatLib.Masters
{
    #region WIN Objects
    #region WINObject
    public class WINObject 
    {
        public static string SUniqueString = "uniqueString";

        public virtual long UniqueID
        {
            get;
            set;
        }

        Dictionary<string, string> dict = new Dictionary<string, string>();
        //An extra parameter to specify this particular node
        public Dictionary<string, string> Parameters
        {
            get { return dict; }
            set { dict = value; }
        }
    }
    #endregion
    #region WINNode

    //containe all node information
    public class WINNode : WINObject
    {
        #region Static Helper Strings/Function
        public static string SNodeString = "nodeType";

        public static PropertyObject NodeWithProperties(long uniqueID, NeuronType nt)
        {
            return new PropertyObject() { { WINNode.SUniqueString, uniqueID.ToString() }, { WINNode.SNodeString, nt.ToString() } };
        }
        #endregion

        #region Getters/Setters

        long nodeID;
        public override long UniqueID
        {
            get
            {
                return nodeID;
            }
            set
            {
                nodeID = value;
            }
        }
        public NeuronType NodeType
        {
            get;
            set;
        }
        #endregion
        //need other node information for convenience?       
    }
    #endregion
    #region WINConnection

    public class WINConnection : WINObject
    {
        #region Static Strings

        public static string SSourceID = "sourceID";
        public static string STargetID = "targetID";
        //public static string SWeight = "weight";

        public static PropertyObject ConnectionWithProperties(long uniqueID, long sourceID, long targetID)//, double weight = double.MaxValue)
        {
            return new PropertyObject() { 
            { WINObject.SUniqueString, uniqueID.ToString() }, 
            { WINConnection.SSourceID, sourceID.ToString() },
            { WINConnection.STargetID, targetID.ToString() }
            //{ WINConnection.SWeight, weight.ToString()} 
            };
        }
        public static PropertyObject ConnectionWithProperties(long sourceID, long targetID)//, double weight = double.MaxValue)
        {
            return new PropertyObject() { 
            { WINConnection.SSourceID, sourceID.ToString() },
            { WINConnection.STargetID, targetID.ToString() }
            //{ WINConnection.SWeight, weight.ToString()} 
            };
        }
        #endregion

        #region Placeholder

        #endregion

        #region Source/Target

        //double weight;
        //public double Weight
        //{
        //    get
        //    {
        //        return weight;
        //    }
        //    set
        //    {
        //        weight = value;
        //    }
        //}

        long sourceID;
        public long SourceID
        {
            get
            {
                return sourceID;
            }
            set
            {
                sourceID = value;
            }
        }

        long targetID;
        public long TargetID
        {
            get
            {
                return targetID;
            }
            set
            {
                targetID = value;
            }
        }
        #endregion
    }
    #endregion

    #region WINGenome
    public class WINGenome : WINObject
    {
        //we don't want WINGenome to be a clone of NeatGenome, so we will need to simplify the objects inside
        //it's mostly a collection of nodes and connections, which is really just a list of uniqueIDs, and some other information, 
        //like marking where it came from (singlesteps)

    }
    #endregion

    #region WINProcess
    public class WINProcess : WINObject
    {
    }
    #endregion
    #region SingleStep
    public class WINSingleStep : WINObject
    {
    }
    #endregion

    #region WINSession
    public class WINSession : WINObject
    {

    }
    #endregion
    #endregion

    #region WIN Helpers
    public class PropertyObject : Dictionary<string, string>
    {
        public PropertyObject()
        {

        }
        public PropertyObject(params KeyValuePair<string,string>[] properties)
        {
            foreach (var prop in properties)
            {
                this.Add(prop.Key, prop.Value);
            }
        }

    }
    #endregion


    /// <summary>
    /// Rudimentary WIN class. This is before the switch to a database model, it's just dictionaries for now. 
    /// The calls will be structured to be more like they will be in the future, but need a basic implementation for now. 
    /// </summary>
    public sealed class WINManager
    {
        #region Variables



        #endregion

        //this is for dictionaries that may or may not be in use
        //Some dictionaries will be set up with future progress in mind, and might need to be adjusted over time
        #region Experimental Variables

        //objects indexed by creation time
        Dictionary<string, WINNode> winNodes = new Dictionary<string, WINNode>();
        Dictionary<string, WINConnection> winConnections = new Dictionary<string, WINConnection>();
        Dictionary<string, WINProcess> winProcesses = new Dictionary<string, WINProcess>();
        Dictionary<string, WINSingleStep> winSingleSteps = new Dictionary<string, WINSingleStep>();
        Dictionary<string, WINSession> winSessions = new Dictionary<string, WINSession>();

        #endregion

        #region Singleton Code

        static readonly WINManager instance = new WINManager();

        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static WINManager()
        {

        }

        WINManager()
        {
            //this is somehwere we might attempt connection to our socket server, or start a socket object that responds to our requests

        }

        public static WINManager SharedWIN
        {
            get
            {
                return instance;
            }
        }

        #endregion


        #region Creating Property Dictionaries

        #region WINNode Insert/Find logic
        //uniqueID and neuronType are assumed here!
        /// <summary>
        /// We are trying to find a node here. For now, we just check our local cache. In real WIN, we'll check against the database of entries.
        /// </summary>
        /// <param name="prop"></param>
        /// <returns></returns>
        public WINNode findWINNode(PropertyObject prop)
        {
            long uniqueID = tryGetUnique(prop);
            NeuronType neuronType = tryGetNeuronType(prop);
            
            WINNode node;

            if (winNodes.TryGetValue(nodeKey(uniqueID, neuronType), out node))
                return node;

            return null;

        }
        public WINNode createWINNode(PropertyObject prop, IdGenerator idGen = null)
        {
            WINNode node;

            lock (winNodes)
            {
                //here we would head out to the server, and get a new created node, since we couldn't find one
                //call the server and get back our new unique id AND object, instead, we'll generate it ourselves for now
                long uniqueID = tryGetUnique(prop);
                if (uniqueID == -1)
                    uniqueID = nextNodeUniqueID();

                //if we have sent in an idgenerator, we should update the object with our nextID object
                if (idGen != null)
                    idGen.mostRecentInnovationID(uniqueID);

                //Get our neurontype, which is assumed to have been sent in, it would be silly otherwise
                NeuronType neuronType = tryGetNeuronType(prop);


                //need to return a WINNode object
                 node = new WINNode()
                {
                    //Parsed the neuron type before entering function, required
                    NodeType = neuronType,
                    //set the parameters for posterity, this might be unncessary 
                    Parameters = prop,
                    //set the uniqueID, which should be passed in
                    UniqueID = uniqueID
                };

                //temporarily cache this information, duh!
                //also, we use a combination of our uniqueID and nodetype to create a unique string
                //when we move to databases, this won't matter as much since we will query all properties at the same time
                winNodes.Add(nodeKey(node.UniqueID, node.NodeType), node);

            }
            //then send it back
            return node;
        }
      
        private long nextNodeUniqueID()
        {
            //do we have a definitive uniqueID? No? Then create one
            //note uniqueID and creationtime are the same thing right now
            //also note this time utility will make sure that anyrequests to time are atomic, so that two nearly simultaneous requests wont return identical IDs
            return TimeUtility.GetNowTicks();
        }
        private string nodeKey(long uniqueID, NeuronType nodeType)
        {
            return uniqueID.ToString() + nodeType.ToString();
        
        }
        #endregion

        #region WINConnection Insert/Find logic

        public WINConnection findWINConnection(PropertyObject prop)
        {
            //we have three uniquely defining properties, the unique id, the source and the target
            //really, any connection that connects a unique source and target should be enough to define it. The unique ID should shortcut 
            //the search process

            //long uniqueID = tryGetUnique(prop);
            long sourceID = tryGetSourceID(prop);
            long targetID = tryGetTargetID(prop);

            WINConnection conn;

            if (winConnections.TryGetValue(connectionKey(sourceID, targetID), out conn))
                return conn;

            return null;

        }
        public WINConnection createWINConnection(PropertyObject prop, IdGenerator idGen = null )
        {
            WINConnection conn;

            lock (winConnections)
            {
                //here we would head out to the server, and get a newly created connection, since we couldn't find one
                //call the server and get back our new unique id AND object, instead, we'll generate it ourselves for now
                long uniqueID = tryGetUnique(prop);
                if (uniqueID == -1)
                    uniqueID = nextConnectionUniqueID();

                //if we have sent in an idgenerator, we should update the object with our nextID object
                if (idGen != null)
                    idGen.mostRecentInnovationID(uniqueID);

                //parse out our source and target, which are obviously required for creating a connection
                long sourceID = tryGetSourceID(prop);
                long targetID = tryGetTargetID(prop);

                //double weight = tryGetDouble(WINConnection.SWeight, prop);

                //need to return a WINNode object
                conn = new WINConnection()
                {
                    //Parsed the source and target, which are required
                    SourceID = sourceID,
                    TargetID = targetID,
                    //Weight = weight,
                    //set the parameters for posterity, this might be unncessary 
                    Parameters = prop,
                    //set the uniqueID, which should be passed in
                    UniqueID = uniqueID
                };

                //temporarily cache this information, duh!
                //also, we use a combination of our uniqueID and nodetype to create a unique string
                //when we move to databases, this won't matter as much since we will query all properties at the same time
                winConnections.Add(connectionKey(conn.SourceID, conn.TargetID), conn);

            }

            //then send it back
            return conn;
        }

        private long nextConnectionUniqueID()
        {
            //do we have a definitive uniqueID? No? Then create one
            //note uniqueID and creationtime are the same thing right now
            return DateTime.Now.Ticks;
        }
        private string connectionKey(long sourceID, long targetID)
        {
            return sourceID.ToString() + targetID.ToString();
        }

        #endregion

        #region Try/Get Functions

        private NeuronType tryGetNeuronType(PropertyObject prop)
        {
            string val = tryGetValue(WINObject.SUniqueString, prop);
            if (val != null)
                return (NeuronType)Enum.Parse(typeof(NeuronType), val);
            else
                return NeuronType.Undefined;
        }

        #region Source, Target, Unique long parsing
        private long tryGetSourceID(PropertyObject prop)
        {
            return tryGetLong(WINConnection.SSourceID, prop);
        }

        private long tryGetTargetID(PropertyObject prop)
        {
            return tryGetLong(WINConnection.STargetID, prop);
        }

        private long tryGetUnique(PropertyObject prop)
        {
            return tryGetLong(WINObject.SUniqueString, prop);
        }
        #endregion

        #region Try Get Long/String

        private double tryGetDouble(string valString, PropertyObject prop)
        {
            string val = tryGetValue(valString, prop);
            if (val != null)
                return double.Parse(val);
            else
                return -1;
        }
        private long tryGetLong(string valString, PropertyObject prop)
        {
            string val = tryGetValue(valString, prop);
            if(val != null)
                return long.Parse(val);
            else
                return -1;
        }
        private string tryGetValue(string key, PropertyObject prop)
        {
            string val;
            prop.TryGetValue(key, out val);
            return val;
        }
        #endregion

        #endregion

        #endregion

        #region Fetching objects with properties

        /// <summary>
        /// Required: UniqueID property, NeuronType
        /// </summary>
        /// <param name="prop"></param>
        /// <returns></returns>
        public WINNode findOrInsertNodeWithProperties(IdGenerator idGen, PropertyObject prop)
        {
            //the very first thing we must do, is attempt to find our object
            //in order to do this, we need a uniqueID to find, and a neuronType
            //we assume both of these are provided

            WINNode node = findWINNode(prop);

            //we found it, then return it if you don't mind 
            if (node != null)
                return node;

            //we weren't able to find the node, now we must create it and return it (since we are find or insert!)
            //this should create the appropriate ID for this object
            node = createWINNode(prop);

            //now we should take into account that our idGenerator needs to know about new innovations
            idGen.mostRecentInnovationID(node.UniqueID);

            //might want this to be async in the future, but for now we make it synchronous
            //property objects should have everything we need to identify the WINNode
            return node;
        }
        public WINConnection findOrInsertConnectionWithProperties(IdGenerator idGen, PropertyObject prop)
        {
            //the very first thing we must do, is attempt to find our object
            //in order to do this, we need a uniqueID to find, and a neuronType
            //we assume both of these are provided

            WINConnection conn = findWINConnection(prop);

            //we found it, then return it if you don't mind 
            if (conn != null)
                return conn;

            //we weren't able to find the node, now we must create it and return it (since we are find or insert!)
            //this should create the appropriate ID for this object
            conn = createWINConnection(prop);

            //now we should take into account that our idGenerator needs to know about new innovations
            idGen.mostRecentInnovationID(conn.UniqueID);

            //might want this to be async in the future, but for now we make it synchronous
            //property objects should have everything we need to identify the WINNode
            return conn;
        }



        #endregion





    }
}
