// noveltyhistogram.cs created with MonoDevelop
// User: joel at 2:22 AM 7/23/2009
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using System.Collections.Generic;
using SharpNeatLib;
using SharpNeatLib.NeatGenome;
using SharpNeatLib.Evolution;
using SharpNeatLib.NeatGenome.Xml;
using SharpNeatLib.NeuralNetwork;
using SharpNeatLib.NeuralNetwork.Xml;

namespace SharpNeatLib.Novelty
{
	public class Pair<T,U>:IComparable where T:System.IComparable<T> {
	 public Pair() { }
	 public Pair(T first, U second) {
	  this.First=first;
	  this.Second=second;
	 }
	 public T First { get; set; }
	 public U Second { get; set; }
	 int IComparable.CompareTo(object obj) {
	  Pair<T,U> c=(Pair<T,U>)obj;
	  return this.First.CompareTo(c.First);
	 }
	};
	
	public class noveltyfixed
	{	
	    public int nearest_neighbors;
	    public bool initialized; 
	    public double archive_threshold;
	    public GenomeList measure_against;
	    public GenomeList archive;
	    public GenomeList pending_addition;
	    
	    public void addPending()
	    {
	        int length = pending_addition.Count;
	        
	        if(length == 0)
	        {
	            archive_threshold *= 0.95;
	        }
	        if(length > 5)
	        {
	            archive_threshold *= 1.3;
	        }
	        
	        for(int i=0;i<length;i++)
	        {	    
	            if(measureAgainstArchive((NeatGenome.NeatGenome)pending_addition[i],false))
	                archive.Add(pending_addition[i]); 
	        }
	        pending_addition.Clear();
	    }

        double maxDistSeen = double.MinValue;
	    public bool measureAgainstArchive(NeatGenome.NeatGenome neatgenome,bool addToPending) 
	    {
			
	        foreach (IGenome genome in archive)
	        {
                double dist = BehaviorDistance.Distance(neatgenome.Behavior,((NeatGenome.NeatGenome)genome).Behavior);
               
                if (dist > maxDistSeen)
                {
                    maxDistSeen = dist;
                    Console.WriteLine("Most novel distance: " + maxDistSeen);
                }

                if (dist < archive_threshold)
	                return false;
	        }
	        
	        if(addToPending)
	        {
				
	            pending_addition.Add(neatgenome);
	        }
	        
	        return true;
	    }
	    
	    //measure the novelty of an organism against the fixed population
	    public double measureNovelty(NeatGenome.NeatGenome neatgenome)
	    {
		   double sum = 0.0;

            if(!initialized)
               return Double.MinValue;
               
	        List< Pair<double,NeatGenome.NeatGenome> > noveltyList = new List<Pair<double,NeatGenome.NeatGenome>>();
	        
	        foreach(IGenome genome in measure_against)
	        {
	            noveltyList.Add(new Pair<double,NeatGenome.NeatGenome>(BehaviorDistance.Distance(((NeatGenome.NeatGenome)genome).Behavior,neatgenome.Behavior),((NeatGenome.NeatGenome)genome)));
	        }
	        foreach(IGenome genome in archive)
	        {
	            noveltyList.Add(new Pair<double,NeatGenome.NeatGenome>(BehaviorDistance.Distance(((NeatGenome.NeatGenome)genome).Behavior,neatgenome.Behavior),((NeatGenome.NeatGenome)genome)));
//				noveltyList.Add(BehaviorDistance.Distance(((NeatGenome.NeatGenome)genome).Behavior,neatgenome.Behavior));
	        }
            
            //see if we should add this genome to the archive
            measureAgainstArchive(neatgenome,true);
                
	        noveltyList.Sort();
			int nn = nearest_neighbors;
			if(noveltyList.Count<nearest_neighbors) {
				nn=noveltyList.Count;
			}
            neatgenome.nearestNeighbors = nn;

            //Paul - reset local competition and local genome novelty -- might have been incrementing over time
            neatgenome.competition = 0;
            neatgenome.localGenomeNovelty = 0;

            for (int x = 0; x < nn; x++)
            {
                sum += noveltyList[x].First;

                if (neatgenome.RealFitness > noveltyList[x].Second.RealFitness)
                    neatgenome.competition += 1;

                if (neatgenome.objectives[neatgenome.objectives.Length - 1] > noveltyList[x].Second.objectives[neatgenome.objectives.Length - 1])
                    neatgenome.localGenomeNovelty += 1;

                noveltyList[x].Second.locality += 1;
                // sum+=10000.0; //was 100
            }
            //neatgenome.locality = 0;
            //for(int x=0;x<nn;x++)
            //{
            //    sum+=noveltyList[x].First;

            //    if(neatgenome.RealFitness>noveltyList[x].Second.RealFitness)
            //        neatgenome.competition+=1;
				
            //    noveltyList[x].Second.locality+=1;
            //    //Paul: This might not be the correct meaning of locality, but I am hijacking it instead
            //    //count how many genomes we are neighbored to
            //    //then, if we take neatgenome.competition/neatgenome.locality - we get percentage of genomes that were beaten locally!
            //    neatgenome.locality += 1;
            //    // sum+=10000.0; //was 100
            //}
	        return Math.Max(sum,EvolutionAlgorithm.MIN_GENOME_FITNESS);
	    }
	    
	    //initialize fixed novelty measure
		public noveltyfixed(double threshold)
		{
		    initialized = false;
		    nearest_neighbors = 20;
		    archive_threshold = threshold;
		    archive = new GenomeList();
		    pending_addition = new GenomeList();
		}
		
		
		//Todo REFINE... adding highest fitness might
		//not correspond with most novel?
		public void add_most_novel(Population p)
		{
		    double max_novelty =0;
		    IGenome best= null;
		    for(int i=0;i<p.GenomeList.Count;i++)
		    {
		        if(p.GenomeList[i].Fitness > max_novelty)
		        {
		            best = p.GenomeList[i];
		            max_novelty = p.GenomeList[i].Fitness;
		        }
		    }
		    archive.Add(best);
		}
		public void initialize(GenomeList p)
		{
			initialized = true;
		  
			measure_against = new GenomeList();
		    
		    if(p!=null)
		    for(int i=0;i<p.Count;i++)
		    {
		        //we might not need to make copies
                //Paul: removed copies to make it easier to read the realfitness from the indiviudals, without making a million update calls
		        measure_against.Add(p[i]);//new NeatGenome.NeatGenome((NeatGenome.NeatGenome)p[i],i));    
		    }
		}
		
		public void initialize(Population p)
		{
		    initialize(p.GenomeList);
		}
		
		//update the measure population by intelligently sampling
		//the current population + archive + fixed population
		public void update_measure(Population p)
		{
			update_measure(p.GenomeList);
		}
		
		public void update_measure(GenomeList p)
		{
			GenomeList total = new GenomeList();
		
		    total.AddRange(p);
		    total.AddRange(measure_against);
		    total.AddRange(archive);
		    
		    merge_together(total, p.Count);
		    Console.WriteLine("size: " + Convert.ToString(measure_against.Count));
		}
		
		public void merge_together(GenomeList list,int size)
		{
		    Console.WriteLine("total count: "+ Convert.ToString(list.Count));
		    
		    Random r = new Random();
		    GenomeList newList = new GenomeList();
		    
		    List<bool> dirty = new List<bool>();
		    List<double> closest = new List<double>();
		    //set default values
		    for(int x=0;x<list.Count;x++)
		    {
		        dirty.Add(false);
		        closest.Add(Double.MaxValue);
		    }
		    //now add the first individual randomly to the new population
		    int last_added = r.Next() % list.Count;
		    dirty[last_added] = true;
		    newList.Add(list[last_added]);
		    
		    while(newList.Count < size)
		    {
		        double mostNovel = 0.0;
		        int mostNovelIndex = 0;
		        for(int x=0;x<list.Count;x++)
		        {
		            if (dirty[x])
		                continue;
		            double dist_to_last = BehaviorDistance.Distance(((NeatGenome.NeatGenome)list[x]).Behavior,
		                                                            ((NeatGenome.NeatGenome)list[last_added]).Behavior);
		            if (dist_to_last < closest[x])
		                closest[x] = dist_to_last;
		            
		            if (closest[x] > mostNovel)
		            {
		                mostNovel = closest[x];
		                mostNovelIndex = x;
		            }
		        }
		        
		        dirty[mostNovelIndex] = true;
		        newList.Add(new NeatGenome.NeatGenome((NeatGenome.NeatGenome)list[mostNovelIndex],0));
		        last_added = mostNovelIndex;
		    }
		    
		    measure_against = newList;
		}

        public void updatePopulationFitness(GenomeList genomePop)
        {
            for (int i = 0; i < genomePop.Count; i++)
            {
                //we might not need to make copies
                measure_against[i].RealFitness = genomePop[i].RealFitness;
            }
        }
    }
}
