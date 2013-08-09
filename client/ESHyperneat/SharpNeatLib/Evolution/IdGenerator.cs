using System;

namespace SharpNeatLib.Evolution
{
	public class IdGenerator
	{
        long nextGenomeId;
        long nextInnovationId;
        long savedInnovationId;
	
		#region Constructors

		public IdGenerator()
		{
			this.nextGenomeId = 0;
			this.nextInnovationId = 0;
		}

        public IdGenerator(long nextGenomeId, long nextInnovationId)
		{
			this.nextGenomeId = nextGenomeId;
			this.nextInnovationId = nextInnovationId;
		}

		#endregion

		#region Properties

        public long NextGenomeId
		{
			get
			{
                if (nextGenomeId == long.MaxValue)
					nextGenomeId=0;
				return nextGenomeId++;
			}
		}

        public long NextInnovationId
		{
			get
			{
                if (nextInnovationId == long.MaxValue)
					nextInnovationId=0;
				return nextInnovationId++;
			}
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Used primarilty by the GenomeFactory so that the same innovation ID's are used for input & output nodes
		/// for all of the initial population.
		/// </summary>
		public void ResetNextInnovationNumber()
		{
			nextInnovationId=0;
		}
        public void mostRecentInnovationID(long innovateID)
        {
            //choose the maximum of the two numbers
            nextInnovationId = Math.Max(nextInnovationId, innovateID+1);
        }
        public void mostRecentGenomeID(long gID)
        {
            //choose the maximum of the two numbers
            nextGenomeId = Math.Max(nextGenomeId, gID +1);
        }
        public void TemporaryResetInnovationNumber()
        {
            savedInnovationId = nextInnovationId;
            nextInnovationId = 0;
        }
        public void TemporaryReturnInnovationNumber()
        {
            nextInnovationId = savedInnovationId;
            savedInnovationId = 0;
        }

		#endregion
	}
}
