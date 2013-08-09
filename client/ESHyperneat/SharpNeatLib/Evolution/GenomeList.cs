using System;
using System.Collections.Generic;

namespace SharpNeatLib.Evolution
{

	public class GenomeList : List<IGenome>
	{
		static GenomeComparer genomeComparer = new GenomeComparer();
		static PruningModeGenomeComparer pruningModeGenomeComparer = new PruningModeGenomeComparer();

        Dictionary<long, IGenome> genomesByID = new Dictionary<long, IGenome>();

        #region Access GenomeID Dictionary from Outside

        //safe access to genomes by ID, will return null if not found
        public IGenome GenomeByID(long genomeID)
        {
            IGenome value;
            genomesByID.TryGetValue(genomeID, out value);
            return value;

        }

        public Dictionary<long, IGenome> GenomeDictionary
        {
            get { return genomesByID; }
        }

        public ICollection<long> GenomeIDList
        {
            get { return genomesByID.Keys; }
        }
        #endregion

        #region Make dictionary accessible

        private void safeAddDict(IGenome item)
        {
            if (!genomesByID.ContainsKey(item.GenomeId))
                genomesByID.Add(item.GenomeId, item);
            //else
            //    throw new Exception("Cannot allow genomelists to have genome objects with duplicate IDs, all must be unique");
        }
        private void safeRemoveDict(IGenome item)
        {
            //no error necessary, remove if you have it, otherwise dont throw an error
            if (genomesByID.ContainsKey(item.GenomeId))
                genomesByID.Remove(item.GenomeId);  
        }
        //add functions make sure object is in the dictionary
        new public void Add(IGenome item)
        {
            this.safeAddDict(item);
            base.Add(item);
        }
        new public void AddRange(IEnumerable<IGenome> collection)
        {
            foreach(var item in collection)
                this.safeAddDict(item);
            base.AddRange(collection);
        }

        new public void Insert(int index, IGenome item)
        {
            this.safeAddDict(item);
            base.Insert(index, item);
        }
        new public void InsertRange(int index, IEnumerable<IGenome> collection)
        {
            foreach (var item in collection)
                this.safeAddDict(item);
            base.InsertRange(index, collection);
        }
        new public void Clear()
        {
            //clear out our dictionary, since we're clearing the list
            genomesByID.Clear();
            base.Clear();
        }
        new public bool Remove(IGenome item)
        {
            this.safeRemoveDict(item);
           return base.Remove(item);
        }
        new public int RemoveAll(Predicate<IGenome> match)
        {
            IList<long> removeAll = new List<long>();
            foreach (var pair in genomesByID)
            {
                if (match(pair.Value))
                    removeAll.Add(pair.Key);
            }
            foreach (var item in removeAll)
                genomesByID.Remove(item);

            return base.RemoveAll(match);
        }
        new public void RemoveAt(int index)
        {
            this.safeRemoveDict(this[index]);
            base.RemoveAt(index);
        }
        new public void RemoveRange(int index, int count)
        {
            for (int i = 0; i < count; i++)
            {
                this.safeRemoveDict(this[index + i]);
            }

            base.RemoveRange(index, count);
        }
        

        #endregion
       
        new public void Sort()
		{
			Sort(genomeComparer);
		}

		/// <summary>
		/// This perfroms a secondary sort on genome size (ascending order), so that small genomes
		/// are more likely to be selected thus aiding a pruning phase.
		/// </summary>
		public void Sort_PruningMode()
		{
			Sort(pruningModeGenomeComparer);
		}

	}
}
