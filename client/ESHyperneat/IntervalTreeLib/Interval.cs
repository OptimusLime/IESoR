using System;
using System.Xml.Serialization;
using System.Xml.Schema;
using System.Xml;
using System.Reflection;

namespace IntervalTreeLib {
  /// <summary>
  /// The Interval class maintains an interval with some associated data
  /// </summary>
  /// <typeparam name="T">The type of data being stored</typeparam>
  
    [Serializable]
  [XmlRoot("Interval")]
    public class Interval<T, D> : IXmlSerializable, IComparable<Interval<T, D>> where D : IComparable<D>
    {

      
    private D start;
      
    private D end;
      
    private T data;

    public Interval(D start, D end, T data) {

        //if (typeof(T) != typeof(double) && typeof(T) != typeof(int))
        //{
        //    ConstructorInfo ci = typeof(T).GetConstructor(new Type[0]);
        //    if (ci == null)
        //        Console.WriteLine("Class {0} must have public parameterless constructor", typeof(T).Name);
        //}
        //if (typeof(D) != typeof(double) && typeof(D) != typeof(int))
        //{
        //    ConstructorInfo di = typeof(D).GetConstructor(new Type[0]);
        //    if (di == null)
        //        Console.WriteLine("Class {0} must have public parameterless constructor", typeof(D).Name);
        //}

      this.start = start;
      this.end = end;
      this.data = data;
    }

    public D Start {
      get { return start; }
      set { start = value; }
    }

    public D End {
      get { return end; }
      set { end = value; }
    }

    public T Data {
      get { return data; }
      set { data = value; }
    }
    public Interval<T, D> Clone()
    {
        return new Interval<T,D>(this.start, this.end, this.data);
    }

    public XmlSchema GetSchema() { return null; }

    public void ReadXml(XmlReader reader)
    {
        MethodInfo mi;

        object[] args = new object[] { reader };

        lock (reader)
        {

            reader.MoveToAttribute("Start");
            Type t = typeof(D); // might be int, float, double, etc

            mi = t.GetMethod("Parse", new Type[] { typeof(string) });

            if (mi != null)
            {
                start = (D)mi.Invoke(null, new object[] { reader.ReadContentAsString() });

            }
            else if ((mi = t.GetMethod("ReadXml")) != null)
            {
                try
                {
                    ConstructorInfo ci = typeof(D).GetConstructor(new Type[0]);
                    start = (D)ci.Invoke(null);
                    mi.Invoke(start, new object[] { reader });

                }
                catch (Exception e)
                {
                    Console.WriteLine("The object of type " + typeof(D).ToString() + " MUST have a parameterless constructor to be used in an Interval Tree");
                    throw (e);
                }


            }



            reader.MoveToAttribute("End");

            t = typeof(D); // might be int, float, double, etc
            mi = t.GetMethod("Parse", new Type[] { typeof(string) });
            if (mi != null)
            {
                end = (D)mi.Invoke(null, new object[] { reader.ReadContentAsString() });

            }
            else if ((mi = t.GetMethod("ReadXml")) != null)
            {

                try
                {

                    ConstructorInfo ci = typeof(D).GetConstructor(new Type[0]);
                    end = (D)ci.Invoke(null);
                    mi.Invoke(end, new object[] { reader });


                }
                catch (Exception e)
                {
                    Console.WriteLine("The object of type " + typeof(D).ToString() + " MUST have a PUBLIC parameterless constructor to be used in an Interval Tree");
                    throw (e);
                }


            }




            t = typeof(T); // might be int, float, double, etc
            mi = t.GetMethod("Parse", new Type[] { typeof(string) });
            if (mi != null)
            {
                reader.Read();
                reader.IsStartElement();
                reader.MoveToAttribute("Value");
                data = (T)mi.Invoke(null, new object[] { reader.ReadContentAsString() });
            }
            else if ((mi = t.GetMethod("ReadXml")) != null)
            {

                try
                {
                    ConstructorInfo ci = typeof(T).GetConstructor(new Type[0]);//BindingFlags.Public, null, new Type[0], null);

                    data = (T)ci.Invoke(null);
                    mi.Invoke(data, new object[] { reader });
                    //t.InvokeMember("ReadXml", BindingFlags.InvokeMethod, null, );

                }
                catch (Exception e)
                {
                    Console.WriteLine("The object of type " + typeof(T).ToString() + " MUST have a PUBLIC parameterless constructor to be used in an Interval Tree");
                    throw (e);
                }


            }
        }
        //reader.MoveToElement();

    }

    public void WriteXml(XmlWriter writer)
    {

        writer.WriteAttributeString("Start", start.ToString());
        writer.WriteAttributeString("End", end.ToString());

        //We have to do something fancier for T
        //What if T also needs to be serialized?
        //Check if it responds to serialization, if so, serialize.
        //Otherwise, boom, send to string, slut!
      
        MethodInfo mi;
        object result = null;
        object[] args = new object[] { writer };
        Type t = typeof(T); // might be int, float, double, etc
        mi = t.GetMethod("WriteXml");
        if (mi != null)
        {
            writer.WriteStartElement("Data");
            //this will write data to xml if it exists 

            try
            {
                t.InvokeMember("WriteXml", BindingFlags.InvokeMethod , null, data, args);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            writer.WriteEndElement();
        }
        else
        {
            writer.WriteStartElement("Data");
            writer.WriteAttributeString("Value", data.ToString());
            writer.WriteEndElement();
        }
    }



    public bool Contains(D time, ContainConstrains constraint) {
      bool isContained;

      switch (constraint) {
        case ContainConstrains.None:
          isContained = Contains(time);
          break;
        case ContainConstrains.IncludeStart:
          isContained = ContainsWithStart(time);
          break;
        case ContainConstrains.IncludeEnd:
          isContained = ContainsWithEnd(time);
          break;
        case ContainConstrains.IncludeStartAndEnd:
          isContained = ContainsWithStartEnd(time);
          break;
        default:
          throw new ArgumentException("Ivnalid constraint " + constraint);
      }

      return isContained;
    }

    /// <summary>
    /// true if this interval contains time (inclusive)
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    public bool Contains(D time) {
      //return time < end && time > start;
      return time.CompareTo(end) < 0 && time.CompareTo(start) > 0;
    }

    /// <summary>
    /// true if this interval contains time (including start).
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    public bool ContainsWithStart(D time) {
      return time.CompareTo(end) < 0 && time.CompareTo(start) >= 0;
    }

    /// <summary>
    /// true if this interval contains time (including end).
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    public bool ContainsWithEnd(D time) {
      return time.CompareTo(end) <= 0 && time.CompareTo(start) > 0;
    }

    /// <summary>
    /// true if this interval contains time (include start and end).
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    public bool ContainsWithStartEnd(D time) {
      return time.CompareTo(end) <= 0 && time.CompareTo(start) >= 0;
    }

    /// <summary>
    /// return true if this interval intersects other
    /// </summary>
    /// <param name="?"></param>
    /// <returns></returns>
    public bool Intersects(Interval<T,D> other) {
      //return other.End > start && other.Start < end;
      return other.End.CompareTo(start) >= 0 && other.Start.CompareTo(end) <= 0;
    }


    /// <summary>
    /// Return -1 if this interval's start time is less than the other, 1 if greater
    /// In the event of a tie, -1 if this interval's end time is less than the other, 1 if greater, 0 if same
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public int CompareTo(Interval<T,D> other) {
      if (start.CompareTo(other.Start) < 0)
        return -1;
      else if (start.CompareTo(other.Start) > 0)
        return 1;
      else if (end.CompareTo(other.End) < 0)
        return -1;
      else if (end.CompareTo(other.End) > 0)
        return 1;
      else
        return 0;
      //if (start < other.Start)
      //  return -1;
      //else if (start > other.Start)
      //  return 1;
      //else if (end < other.End)
      //  return -1;
      //else if (end > other.End)
      //  return 1;
      //else
      //  return 0;
    }

    public override string ToString() {
      return string.Format("{0}-{1}", start, end);
    }
  }

  public enum ContainConstrains {
    None,
    IncludeStart,
    IncludeEnd,
    IncludeStartAndEnd
  }
}
