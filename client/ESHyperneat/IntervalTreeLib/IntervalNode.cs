using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Wintellect.PowerCollections;

namespace IntervalTreeLib {
  /// <summary>
  /// The Node class contains the interval tree information for one single node
  /// </summary>
  public class IntervalNode<T,D> where D : struct, IComparable<D> {
    private OrderedDictionary<Interval<T, D>, List<Interval<T, D>>> intervals;
    private D center;
    private IntervalNode<T,D> leftNode;
    private IntervalNode<T,D> rightNode;

    public IntervalNode() {
      intervals = new OrderedDictionary<Interval<T, D>, List<Interval<T, D>>>();
      center = default(D);
      leftNode = null;
      rightNode = null;
    }

    private string debug {
      get {
        StringBuilder sb = new StringBuilder();
        foreach (var key in intervals.Keys) {
          sb.AppendLine(key.ToString());
          sb.AppendLine("==");
          foreach (var val in intervals[key]) {
            sb.AppendLine(val.ToString());
            sb.AppendLine("--");
          }

          sb.AppendLine("***");
        }

        return sb.ToString();
      }
    }

    public IntervalNode(List<Interval<T, D>> intervalList) {

      intervals = new OrderedDictionary<Interval<T, D>, List<Interval<T, D>>>();

      var endpoints = new OrderedSet<D>();

      foreach (var interval in intervalList) {
        endpoints.Add(interval.Start);
        endpoints.Add(interval.End);
      }

      Nullable<D> median = GetMedian(endpoints);
      center = median.GetValueOrDefault();

      List<Interval<T, D>> left = new List<Interval<T, D>>();
      List<Interval<T, D>> right = new List<Interval<T, D>>();

      foreach (Interval<T, D> interval in intervalList) {
        if (interval.End.CompareTo(center) < 0)
          left.Add(interval);
        else if (interval.Start.CompareTo(center) > 0)
          right.Add(interval);
        else {
          List<Interval<T, D>> posting;
          if (!intervals.TryGetValue(interval, out posting)) {
            posting = new List<Interval<T, D>>();
            intervals.Add(interval, posting);
          }
          posting.Add(interval);
        }
      }

      if (left.Count > 0)
        leftNode = new IntervalNode<T,D>(left);
      if (right.Count > 0)
        rightNode = new IntervalNode<T,D>(right);
    }

    public IEnumerable<IList<Interval<T, D>>> Intersections {
      get {
        if (intervals.Count == 0) yield break;
        else if (intervals.Count == 1) {
          if (intervals.First().Value.Count > 1) {
            yield return intervals.First().Value;
          }
        }
        else {
          var keys = intervals.Keys.ToArray();

          int lastIntervalIndex = 0;
          List<Interval<T, D>> intersectionsKeys = new List<Interval<T, D>>();
          for (int index = 1; index < intervals.Count; index++) {
            var intervalKey = keys[index];
            if (intervalKey.Intersects(keys[lastIntervalIndex])) {
              if (intersectionsKeys.Count == 0) {
                intersectionsKeys.Add(keys[lastIntervalIndex]);
              }
              intersectionsKeys.Add(intervalKey);
            }
            else {
              if (intersectionsKeys.Count > 0) {
                yield return GetIntervalsOfKeys(intersectionsKeys);
                intersectionsKeys = new List<Interval<T, D>>();
                index--;
              }
              else {
                if (intervals[intervalKey].Count > 1) {
                  yield return intervals[intervalKey];
                }
              }

              lastIntervalIndex = index;
            }
          }

          if (intersectionsKeys.Count > 0) yield return GetIntervalsOfKeys(intersectionsKeys);
        }
      }
    }

    private List<Interval<T, D>> GetIntervalsOfKeys(List<Interval<T, D>> intervalKeys) {
      var allIntervals =
        from k in intervalKeys
        select intervals[k];

      return allIntervals.SelectMany(x => x).ToList();
    }

    /// <summary>
    /// Perform a stabbing Query on the node
    /// </summary>
    /// <param name="time">the time to Query at</param>
    /// <returns>all stubedIntervals containing time</returns>
    public List<Interval<T, D>> Stab(D time, ContainConstrains constraint) {
      List<Interval<T, D>> result = new List<Interval<T, D>>();

      foreach (var entry in intervals) {
        if (entry.Key.Contains(time, constraint))
          foreach (var interval in entry.Value)
            result.Add(interval);
        else if (entry.Key.Start.CompareTo(time) > 0)
          break;
      }

      if (time.CompareTo(center) < 0 && leftNode != null)
        result.AddRange(leftNode.Stab(time, constraint));
      else if (time.CompareTo(center) > 0 && rightNode != null)
        result.AddRange(rightNode.Stab(time, constraint));
      return result;
    }

    /// <summary>
    /// Perform an interval intersection Query on the node
    /// </summary>
    /// <param name="target">the interval to intersect</param>
    /// <returns>all stubedIntervals containing time</returns>
    public List<Interval<T, D>> Query(Interval<T, D> target) {
      List<Interval<T, D>> result = new List<Interval<T, D>>();

      foreach (var entry in intervals) {
        if (entry.Key.Intersects(target))
          foreach (Interval<T, D> interval in entry.Value)
            result.Add(interval);
        else if (entry.Key.Start.CompareTo(target.End) > 0)
          break;
      }

      if (target.Start.CompareTo(center) < 0 && leftNode != null)
        result.AddRange(leftNode.Query(target));
      if (target.End.CompareTo(center) > 0 && rightNode != null)
        result.AddRange(rightNode.Query(target));
      return result;
    }

    public D Center {
      get { return center; }
      set { center = value; }
    }

    public IntervalNode<T,D> Left {
      get { return leftNode; }
      set { leftNode = value; }
    }

    public IntervalNode<T,D> Right {
      get { return rightNode; }
      set { rightNode = value; }
    }

    /// <summary>
    /// the median of the set, not interpolated
    /// </summary>
    /// <param name="set"></param>
    /// <returns></returns>
    private Nullable<D> GetMedian(OrderedSet<D> set) {
      int i = 0;
      int middle = set.Count / 2;
      foreach (D point in set) {
        if (i == middle)
          return point;
        i++;
      }
      return null;
    }

    public override string ToString() {
      var sb = new StringBuilder();
      sb.Append(center + ": ");
      foreach (var entry in intervals) {
        sb.Append("[" + entry.Key.Start + "," + entry.Key.End + "]:{");
        foreach (Interval<T, D> interval in entry.Value) {
          sb.Append("(" + interval.Start + "," + interval.End + "," + interval.Data + ")");
        }
        sb.Append("} ");
      }
      return sb.ToString();
    }

  }
}