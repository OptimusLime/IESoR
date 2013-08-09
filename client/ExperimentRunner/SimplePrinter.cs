using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace NodeCommunicator
{
    public class SimplePrinter
    {
        #region Static Helpers
        public static string listToString<T>(List<T> list)
        {
            string arrayString = "{";
            list.ForEach(x => arrayString += (x.ToString() + ","));
            arrayString = arrayString.Substring(0, arrayString.Length - 1);
            arrayString += "}";
            return arrayString;
        }
        public static string dictToString<L, D>(Dictionary<L, List<D>> dict)
        {
            string dictString = "[";
            foreach (var keyPair in dict)
            {
                dictString += "{" + keyPair.Key.ToString() + ",";
                dictString += listToString<D>(keyPair.Value);
                dictString += "},";
            }
            if(dict.Count > 0)
                dictString = dictString.Substring(0, dictString.Length - 1);
            
            dictString += "]";
            return dictString;
        }
        #endregion

        public SimplePrinter(TextBox tb)
        {
            thingToCall = tb;
        }
        TextBox thingToCall;

        public void WriteLine(object toWrite)
        {
            callThingWithString(toWrite.ToString() + "\n");
        }
        public void Write(string toWrite)
        {
            callThingWithString(toWrite.ToString());
        }
        void callThingWithString(string print)
        {

            thingToCall.Dispatcher.Invoke((Action)delegate()
            {
                if (thingToCall.Text.Length > 6000)
                    thingToCall.Text = thingToCall.Text.Substring(6000);

                thingToCall.Text += print;
            });

        }

    }
}
