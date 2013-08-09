using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IntervalTreeLib;
using SharpNeatLib.Novelty;
using SharpNeatLib.Evolution;

namespace SharpNeatLib.Masters
{

    public class TimeGenomeTree : TimeTree<NeatGenome.NeatGenome> { }

    public class TimeTree<T> : IntervalTreeLib.IntervalTree<T, long>
    {
        public TimeTree() : base() { }
        public long maxEndValue = long.MinValue;

        /// <summary>
        /// Overrided add interval function, measures the largest time value
        /// </summary>
        /// <param name="interval"></param>
        public override void AddInterval(Interval<T, long> interval)
        {
            //record our highest interval
            maxEndValue = Math.Max(maxEndValue, interval.End);
            base.AddInterval(interval);
        }

        /// <summary>
        /// overrided add interval measures the largest time value, inserts interval
        /// </summary>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        /// <param name="data"></param>
        public override void AddInterval(long begin, long end, T data)
        {
            //record our highest
            maxEndValue = Math.Max(maxEndValue, end);
            base.AddInterval(begin, end, data);
        }
        public List<Interval<T, long>> getIntervalsSince(long start)
        {
            return this.GetIntervals(start, maxEndValue);
        }
        public List<T> getSince(long start)
        {
            return this.Get(start, maxEndValue);
        }

    }
    //takes in all the genomes, filters them out
    public class GenomeFilter
    {
        TimeGenomeTree intervalGenomes = new TimeGenomeTree();
        Dictionary<NeatGenome.NeatGenome, Interval<NeatGenome.NeatGenome, long>> quickReferenceIntervals = new Dictionary<NeatGenome.NeatGenome, Interval<NeatGenome.NeatGenome, long>>();
        SortedDictionary<double, List<NeatGenome.NeatGenome>> sortedGenomes = new SortedDictionary<double, List<NeatGenome.NeatGenome>>();
        //doesn't take in anything, just instantiates with an interval tree object
        public GenomeFilter()
        {

        }

        public void addPopulation(GenomeList pop)
        {
            pop.ForEach(x => addGenome((NeatGenome.NeatGenome)x));
        }
        public void addGenome(NeatGenome.NeatGenome genome)
        {
            if (quickReferenceIntervals.ContainsKey(genome))
                return;

            try
            {
                long date = DateTime.Now.Ticks;
                //we'll note when we added this genome object
                Interval<NeatGenome.NeatGenome, long> genomeInterval = new Interval<NeatGenome.NeatGenome, long>(date, date, genome);

                //genomes for this particular fitness
                List<NeatGenome.NeatGenome> genomesForFitness;

                if (!sortedGenomes.TryGetValue(genome.Fitness, out genomesForFitness))
                {
                    genomesForFitness = new List<NeatGenome.NeatGenome>();
                    sortedGenomes.Add(genome.Fitness, genomesForFitness);
                }
                if (!genomesForFitness.Contains(genome))
                    genomesForFitness.Add(genome);

                quickReferenceIntervals.Add(genome, genomeInterval);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }

        }
        public void removeGenome(NeatGenome.NeatGenome genome)
        {
            //got to go through our intervalgenomes and delete that genomes information
            var gInterval = quickReferenceIntervals[genome];

            //will handle deleting the interval
            intervalGenomes.RemoveInterval(gInterval);

            //will remove from our quick refernce list
            quickReferenceIntervals.Remove(genome);
        }

        public Pair<long, List<NeatGenome.NeatGenome>> requestLatestGenomes(long lastRequestTime = 0)
        {
            //we haven't requested before query everything and send it back
            return new Pair<long, List<NeatGenome.NeatGenome>>(DateTime.Now.Ticks, quickReferenceIntervals.Keys.ToList().FindAll(x => quickReferenceIntervals[x].Start >= lastRequestTime).ToList());//intervalGenomes.getSince(lastRequestTime));
        }
        public Pair<long, List<NeatGenome.NeatGenome>> requestBestAndLatestGenomes(double topPercent = .05, long lastRequestTime = 0)
        {
            //first we filter our genomes, then we return them
            //grab the top five percent of all genomes we've seen
            //then filter all the individuals who occur AFTER the last requested time
            List<NeatGenome.NeatGenome> returnGenomes = grabTopGenomes(topPercent).FindAll(x => quickReferenceIntervals[x].Start >= lastRequestTime);

            //return these genomes to us!
            return new Pair<long, List<NeatGenome.NeatGenome>>(DateTime.Now.Ticks, returnGenomes);


            //List<NeatGenome.NeatGenome> returnGenomes = intervalGenomes.getSince(lastRequestTime);

            //if(returnGenomes.Count > 0)
            //{
            //    returnGenomes = returnGenomes.OrderBy(x => x.Fitness).ToList<NeatGenome.NeatGenome>();

            //    //calculate top 5%
            //    int top5 = Math.Min(returnGenomes.Count returnGenomes.Count
            //}
            //we haven't requested before query everything and send it back
        }
        private List<NeatGenome.NeatGenome> grabTopGenomes(double percent)
        {
            int totalCount = (int)Math.Round(percent * quickReferenceIntervals.Count);

            List<NeatGenome.NeatGenome> returnGenomes =  new List<NeatGenome.NeatGenome>();
            //we know how many we want
            if (totalCount == 0)
                return returnGenomes;

            //var sGenomes = sortedGenomes.Values.OrderBy(g => g.

            var quickSorted = quickReferenceIntervals.Keys.OrderByDescending(g => (g.objectives == null) ? 0 : g.objectives[0]).ThenByDescending(g => (g.objectives == null) ? 0 : g.objectives[1]).ToArray();

            for (int i = 0; i < totalCount; i++)
            {
                returnGenomes.Add(quickSorted[i]);
            }
            return returnGenomes;

            //var reverseKeys = sortedGenomes.Keys.Reverse();

            //foreach (var key in reverseKeys)
            //{
            //    var sortedPair = sortedGenomes[key];

            //    foreach (var genome in sortedPair)
            //    {
            //        if (returnGenomes.Count >= totalCount)
            //            break;

            //        returnGenomes.Add(genome);
            //    }

            //    if (returnGenomes.Count >= totalCount)
            //        break;
            //}

            //return returnGenomes;
        }
    }
}
