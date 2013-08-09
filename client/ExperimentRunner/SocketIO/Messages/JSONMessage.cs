﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;


namespace SocketIOClient.Messages
{
    public class JSONMessage : Message
    {
        public void SetMessage(object value)
        {
            this.MessageText = JsonConvert.SerializeObject(value, Formatting.None);
        }

        public virtual T Message<T>()
        {
            try { return JsonConvert.DeserializeObject<T>(this.MessageText); }
            catch (Exception ex)
            {
                // add error logging here
                throw;
            }
        }

        public JSONMessage()
        {
            this.MessageType = SocketIOMessageTypes.JSONMessage;
        }

        public JSONMessage(object jsonObject, int? ackId = null, string endpoint = null):this()
        {
            this.AckId = ackId;
            this.Endpoint = endpoint;
            this.MessageText = JsonConvert.SerializeObject(jsonObject, Formatting.None);
        }

        public static JSONMessage Deserialize(string rawMessage)
        {
			JSONMessage jsonMsg = new JSONMessage();
            //  '4:' [message id ('+')] ':' [message endpoint] ':' [json]
            //   4:1::{"a":"b"}
			jsonMsg.RawMessage = rawMessage;

            string[] args = rawMessage.Split(SPLITCHARS, 4); // limit the number of '
            if (args.Length == 4)
            {
                int id;
                if (int.TryParse(args[1], out id))
					jsonMsg.AckId = id;
				jsonMsg.Endpoint = args[2];
				jsonMsg.MessageText = args[3];
            }
			return jsonMsg;
        }
    }
}
