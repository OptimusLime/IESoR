using System;
using System.Collections.Generic;
using System.Text;
using Wintellect.PowerCollections;
using System.Diagnostics;
using System.Xml.Serialization;
using System.Xml.Schema;
using System.Xml;

namespace IntervalTreeLib {
  /// <summary>
  /// An Interval Tree is essentially a map from stubedIntervals to objects, which
  /// can be queried for all data associated with a particular interval of time
  /// </summary>
  /// <typeparam name="T">Type of data store on each interval.</typeparam>
  /// <typeparam name="D">Type define the interval. Must be struct that implement IComperable</typeparam>
  /// <remarks>
  /// This code was translated from Java to C# by ido.ran@gmail.com from the web site http://www.thekevindolan.com/2010/02/interval-tree/index.html.
  /// </remarks>
  [Serializable]
  public class IntervalTree<T, D> : IXmlSerializable where D : struct, IComparable<D> 
      //where T : IXmlSerializable 
  {

    
    private IntervalNode<T,D> head;
     
      
    private List<Interval<T,D>> intervalList;
    private bool inSync;
    private int size;

       public XmlSchema GetSchema() { return null; }

       public virtual void ReadXml(XmlReader reader)
       {
           reader.MoveToAttribute("Size");
           size = reader.ReadContentAsInt();
           if (size == 0)
           {
               intervalList = new List<Interval<T, D>>();
               return;
           }
           intervalList = null;
           bool preceded = false;
           while (reader.Read())
           {

               if (reader.IsStartElement())
               {
                   if(reader.Name != "IntervalList" && reader.Name != "Interval")                   
                   Console.WriteLine("IStart: " + reader.Name);
                   switch (reader.Name)
                   {
                       case "IntervalList":
                           if(intervalList == null)
                           preceded = true;
                           intervalList = new List<Interval<T, D>>();
                           break;
                       case "Interval":
                           Interval<T, D> interval = new Interval<T, D>(default(D), default(D), default(T));
                           interval.ReadXml(reader);
                           intervalList.Add(interval);
                           //reader.MoveToElement();
                           //reader.ReadEndElement();
                           break;
                   }
               }
               //else
               //{
                   
               //    Console.WriteLine("INotStart: " + reader.Name);
               //}

               if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "IntervalList")
                   break;
           }
           inSync = false;
        }

        public virtual void WriteXml(XmlWriter writer)
        {

            writer.WriteAttributeString("Size", intervalList.Count.ToString());

            writer.WriteStartElement("IntervalList");
            foreach (Interval<T,D> interval in intervalList)
            {
                writer.WriteStartElement("Interval");
                interval.WriteXml(writer);
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }




    /// <summary>
    /// Instantiate a new interval tree with no stubedIntervals
    /// </summary>
    public IntervalTree() {
      this.head = new IntervalNode<T,D>();
      this.intervalList = new List<Interval<T,D>>();
      this.inSync = true;
      this.size = 0;
    }

    /// <summary>
    /// Instantiate and Build an interval tree with a preset list of stubedIntervals
    /// </summary>
    /// <param name="intervalList">The list of stubedIntervals to use</param>
    public IntervalTree(List<Interval<T,D>> intervalList) {
      this.head = new IntervalNode<T,D>(intervalList);
      this.intervalList = new List<Interval<T,D>>();
      this.intervalList.AddRange(intervalList);
      this.inSync = true;
      this.size = intervalList.Count;
    }

    /// <summary>
    /// Perform a stabbing Query, returning the associated data.
    /// Will rebuild the tree if out of sync
    /// </summary>
    /// <param name="time">The time to Stab</param>
    /// <returns>The data associated with all stubedIntervals that contain time</returns>
    public List<T> Get(D time) {
      return Get(time, StubMode.Contains);
    }

      /// <summary>
      /// Will change two borders at the same time (i.e. to ensure there is no overlap at any point)
      /// Time 1 is inside the left interval, time 2 in inside the right interval
      /// </summary>
      /// <param name="time1"></param>
      /// <param name="time2"></param>
      /// <param name="start1"></param>
      /// <param name="end1"></param>
      /// <param name="start2"></param>
      /// <param name="end2"></param>
    public void SimultaneousChangeInterval(D time1, D time2, D start1, D end1, D start2, D end2)
    {
        List<Interval<T, D>> intervals = GetIntervals(time1, time2);
        Debug.Assert(intervals.Count != 2, "2 Item Interval Overlap Should happen");

        intervalList[0].Start = start1;
        intervalList[0].End = end1;

        intervalList[1].Start = start2;
        intervalList[1].End = end2;

        //Mark it out of sync, so that we rebuild the tree later
        inSync = false;

    }
      /// <summary>
      /// Will change a single border in a given time (note: you should only use this for the start or end border (separately)
      /// If you are modifying two borders at a time, use SimultaneousChangeInterval
      /// </summary>
      /// <param name="centerTime"></param>
      /// <param name="start"></param>
      /// <param name="end"></param>
    public void SingleChangeInterval(D centerTime, D start, D end)
    {
        List<Interval<T, D>> intervals = GetIntervals(centerTime);
        Debug.Assert(intervals.Count != 1, "1 Item Interval Overlap Should happen");

        intervalList[0].Start = start;
        intervalList[0].End = end;

        //Mark it out of sync, so that we rebuild the tree later
        inSync = false;
    }
    public void SingleChangeInterval(Interval<T,D> intChange, D start, D end)
    {
        intChange.Start = start;
        intChange.End = end;

        //Mark it out of sync, so that we rebuild the tree later
        inSync = false;
    }
      /// <summary>
      /// Remove an interval that was found using GetIntervals. If not, this method will not work!
      /// </summary>
      /// <param name="remChange"></param>
    public void RemoveInterval(Interval<T, D> remChange)
    {
        //we just remove it based on it's memory reference, not on any check
        this.intervalList.Remove(remChange);
        inSync = false;
    }
    public List<T> Get(D time, StubMode mode) {
      List<Interval<T,D>> intervals = GetIntervals(time, mode);
      List<T> result = new List<T>();
      foreach (Interval<T,D> interval in intervals)
        result.Add(interval.Data);
      return result;
    }

    /// <summary>
    /// Perform a stabbing Query, returning the interval objects.
    /// Will rebuild the tree if out of sync.
    /// </summary>
    /// <param name="time">The time to Stab</param>
    /// <returns>all stubedIntervals that contain time</returns>
    public List<Interval<T,D>> GetIntervals(D time) {
      return GetIntervals(time, StubMode.Contains);
    }

    public List<Interval<T,D>> GetIntervals(D time, StubMode mode) {
      Build();

      List<Interval<T, D>> stubedIntervals;

      switch (mode) {
        case StubMode.Contains:
          stubedIntervals = head.Stab(time, ContainConstrains.None);
          break;
        case StubMode.ContainsStart:
          stubedIntervals = head.Stab(time, ContainConstrains.IncludeStart);
          break;
        case StubMode.ContainsStartThenEnd:
          stubedIntervals = head.Stab(time, ContainConstrains.IncludeStart);
          if (stubedIntervals.Count == 0) {
            stubedIntervals = head.Stab(time, ContainConstrains.IncludeEnd);
          }
          break;
        default:
          throw new ArgumentException("Invalid StubMode " + mode, "mode");
      }

      return stubedIntervals;
    }

    /// <summary>
    /// Perform an interval Query, returning the associated data.
    /// Will rebuild the tree if out of sync.
    /// </summary>
    /// <param name="start">the start of the interval to check</param>
    /// <param name="end">end of the interval to check</param>
    /// <returns>the data associated with all stubedIntervals that intersect target</returns>
    public List<T> Get(D start, D end) {
      List<Interval<T,D>> intervals = GetIntervals(start, end);
      List<T> result = new List<T>();
      foreach (Interval<T,D> interval in intervals)
        result.Add(interval.Data);
      return result;
    }

    /// <summary>
    /// Perform an interval Query, returning the interval objects.
    /// Will rebuild the tree if out of sync
    /// </summary>
    /// <param name="start">the start of the interval to check</param>
    /// <param name="end">the end of the interval to check</param>
    /// <returns>all stubedIntervals that intersect target</returns>
    public virtual List<Interval<T,D>> GetIntervals(D start, D end) {
      Build();
      return head.Query(new Interval<T,D>(start, end, default(T)));
    }
      /// <summary>
      /// Returns a list of objects that are not included inside of start and end
      /// That is to say if you have a series of intervals, this will find out exactly which intervals you are missing between start and end
      /// </summary>
      /// <param name="start"></param>
      /// <param name="end"></param>
      /// <param name="emptyData"></param>
      /// <returns></returns>
    public List<Interval<T, D>> GetMissingIntervals(D start, D end, T emptyData)
    {
        List<Interval<T, D>> mIntervals = new List<Interval<T, D>>();
        D currentStart = start, D = end;
        List<Interval<T, D>> sortedIntervals = this.GetIntervals(start, end);
        sortedIntervals.Sort();

        foreach (Interval<T, D> genPiece in sortedIntervals)
        {
            //if it's less then, it'll return -1
            if (currentStart.CompareTo(genPiece.Start) < 0)
            {
                mIntervals.Add(new Interval<T, D>(currentStart, genPiece.Start, emptyData));
            }
            //Now set our next start to be the end of the current piece
            currentStart = genPiece.End;

        }
        if (currentStart.CompareTo(end) < 0)
        {
            mIntervals.Add(new Interval<T, D>(currentStart, end, emptyData));
        }
        return mIntervals;
    }


    /// <summary>
    /// Add an interval object to the interval tree's list.
    /// Will not rebuild the tree until the next Query or call to Build
    /// </summary>
    /// <param name="interval">interval the interval object to add</param>
    public virtual void AddInterval(Interval<T,D> interval) {
      intervalList.Add(interval);
      inSync = false;
    }

    /// <summary>
    /// Add an interval object to the interval tree's list.
    /// Will not rebuild the tree until the next Query or call to Build.
    /// 
    /// </summary>
    /// <param name="begin">the beginning of the interval</param>
    /// <param name="end">the end of the interval</param>
    /// <param name="data">the data to associate</param>
    public virtual void AddInterval(D begin, D end, T data) {
      intervalList.Add(new Interval<T,D>(begin, end, data));
      inSync = false;
    }

    /// <summary>
    /// Determine whether this interval tree is currently a reflection of all stubedIntervals in the interval list
    /// </summary>
    /// <returns>true if no changes have been made since the last Build</returns>
    public bool IsInSync() {
      return inSync;
    }

    /// <summary>
    /// Build the interval tree to reflect the list of stubedIntervals.
    /// Will not run if this is currently in sync
    /// </summary>
    public void Build() {
      if (!inSync) {
        head = new IntervalNode<T,D>(intervalList);
        inSync = true;
        size = intervalList.Count;
      }
    }

    /// <summary>
    /// Get the number of entries in the currently built interval tree
    /// </summary>
    public int CurrentSize {
      get {
        return size;
      }
    }

    /// <summary>
    /// Get the number of entries in the interval list, equal to .size() if inSync()
    /// </summary>
    public int ListSize {
      get {
        return intervalList.Count;
      }
    }

    /// <summary>
    /// Get list of all intersection stubedIntervals.
    /// </summary>
    /// <returns>Enumerable contain lists of intersecting stubedIntervals.</returns>
    public IEnumerable<ICollection<Interval<T, D>>> GetIntersections() {
      Build();

      Queue<IntervalNode<T, D>> toVisit = new Queue<IntervalNode<T, D>>();
      toVisit.Enqueue(head);

      do {
        var node = toVisit.Dequeue();
        foreach (var intersection in node.Intersections) {
          yield return intersection;
        }

        if (node.Left != null) toVisit.Enqueue(node.Left);
        if (node.Right != null) toVisit.Enqueue(node.Right);

      } while (toVisit.Count > 0);
    }

    /// <summary>
    /// Get all the stubedIntervals in this tree.
    /// This method does not build the tree.
    /// </summary>
    public IList<Interval<T, D>> Intervals {
      get {
        return Algorithms.ReadOnly(intervalList);
      }
    }

    public override String ToString() {
      return NodeString(head, 0);
    }

    private String NodeString(IntervalNode<T,D> node, int level) {
      if (node == null)
        return "";

      var sb = new StringBuilder();
      for (int i = 0; i < level; i++)
        sb.Append("\t");
      sb.Append(node + "\n");
      sb.Append(NodeString(node.Left, level + 1));
      sb.Append(NodeString(node.Right, level + 1));
      return sb.ToString();
    }
  }

  public enum StubMode {
    Contains,
    ContainsStart,
    ContainsStartThenEnd
  }
}
