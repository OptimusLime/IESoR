/***************************************************************************
 *  Adapted from Lutz Roeder's Mapack for .NET, September 2000             *
 *  Adapted from Mapack for COM and Jama routines.                         *
 *  http://www.aisto.com/roeder/dotnet                                     *
 ***************************************************************************/

using System;
using AForge.Math;

namespace Accord.Math.Decompositions
{

	/// <summary>
	///	  QR decomposition for a rectangular matrix.
	/// </summary>
	/// <remarks>
	///   For an m-by-n matrix <c>A</c> with <c>m &gt;= n</c>, the QR decomposition is an m-by-n
	///   orthogonal matrix <c>Q</c> and an n-by-n upper triangular 
	///   matrix <c>R</c> so that <c>A = Q * R</c>.
	///   The QR decompostion always exists, even if the matrix does not have
	///   full rank, so the constructor will never fail.  The primary use of the
	///   QR decomposition is in the least squares solution of nonsquare systems
	///   of simultaneous linear equations.
	///   This will fail if <see cref="FullRank"/> returns <see langword="false"/>.
	/// </remarks>
	public sealed class QrDecomposition
	{
        private double[,] qr;
		private double[] Rdiag;

		/// <summary>Construct a QR decomposition.</summary>	
        public QrDecomposition(double[,] value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");	
			}

            this.qr = (double[,])value.Clone();
			double[,] qr = this.qr;
			int m = value.GetLength(0);
			int n = value.GetLength(1);
			this.Rdiag = new double[n];
	
			for (int k = 0; k < n; k++) 
			{
				// Compute 2-norm of k-th column without under/overflow.
				double nrm = 0;
				for (int i = k; i < m; i++)
				{
					nrm = Tools.Hypotenuse(nrm,qr[i,k]);
				}
				 
				if (nrm != 0.0) 
				{
					// Form k-th Householder vector.
					if (qr[k,k] < 0)
					{
						nrm = -nrm;
					}
					
					for (int i = k; i < m; i++)
					{
						qr[i,k] /= nrm;
					}

					qr[k,k] += 1.0;
	
					// Apply transformation to remaining columns.
					for (int j = k+1; j < n; j++) 
					{
						double s = 0.0;

						for (int i = k; i < m; i++)
						{
							s += qr[i,k]*qr[i,j];
						}

						s = -s/qr[k,k];

						for (int i = k; i < m; i++)
						{
							qr[i,j] += s*qr[i,k];
						}
					}
				}

				this.Rdiag[k] = -nrm;
			}
		}

		/// <summary>Least squares solution of <c>A * X = B</c></summary>
		/// <param name="value">Right-hand-side matrix with as many rows as <c>A</c> and any number of columns.</param>
		/// <returns>A matrix that minimized the two norm of <c>Q * R * X - B</c>.</returns>
		/// <exception cref="T:System.ArgumentException">Matrix row dimensions must be the same.</exception>
		/// <exception cref="T:System.InvalidOperationException">Matrix is rank deficient.</exception>
        public double[,] Solve(double[,] value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");	
			}

			if (value.GetLength(0) != qr.GetLength(0))
			{
				throw new ArgumentException("Matrix row dimensions must agree.");
			}
			
			if (!this.FullRank) 
			{
				throw new InvalidOperationException("Matrix is rank deficient.");
			}
				
			// Copy right hand side
			int count = value.GetLength(1);
            double[,] X = (double[,])value.Clone();
			int m = qr.GetLength(0);
			int n = qr.GetLength(1);
			
			// Compute Y = transpose(Q)*B
			for (int k = 0; k < n; k++) 
			{
				for (int j = 0; j < count; j++) 
				{
					double s = 0.0; 
					
					for (int i = k; i < m; i++)
					{
						s += qr[i,k] * X[i,j];
					}

					s = -s / qr[k,k];
					
					for (int i = k; i < m; i++)
					{
						X[i,j] += s * qr[i,k];
					}
				}
			}
				
			// Solve R*X = Y;
			for (int k = n-1; k >= 0; k--) 
			{
				for (int j = 0; j < count; j++) 
				{
					X[k,j] /= Rdiag[k];
				}
	
				for (int i = 0; i < k; i++) 
				{
					for (int j = 0; j < count; j++) 
					{
						X[i,j] -= X[k,j] * qr[i,k];
					}
				}
			}

            double[,] r = new double[n, count];
            for (int i = 0; i < r.GetLength(0); i++)
                for (int j = 0; j < r.GetLength(1); j++)
                    r[i, j] = X[i, j];
            
            return r;
		}

		/// <summary>Shows if the matrix <c>A</c> is of full rank.</summary>
		/// <value>The value is <see langword="true"/> if <c>R</c>, and hence <c>A</c>, has full rank.</value>
		public bool FullRank
		{
			get
			{
				int columns = qr.GetLength(1);

				for (int i = 0; i < columns; i++)
				{
					if (this.Rdiag[i] == 0)
					{
						return false;
					}
				}

				return true;
			}			
		}
	
		/// <summary>Returns the upper triangular factor <c>R</c>.</summary>
        public double[,] UpperTriangularFactor
		{
			get
			{
				int n = this.qr.GetLength(1);
				double[,] x = new double[n, n];

				for (int i = 0; i < n; i++) 
				{
					for (int j = 0; j < n; j++) 
					{
						if (i < j)
						{
							x[i,j] = qr[i,j];
						}
						else if (i == j) 
						{
							x[i,j] = Rdiag[i];
						}
						else
						{
							x[i,j] = 0.0;
						}
					}
				}
	
				return x;
			}
		}

		/// <summary>Returns the orthogonal factor <c>Q</c>.</summary>
        public double[,] OrthogonalFactor
		{
			get
			{
                int rows = qr.GetLength(0);
                int cols = qr.GetLength(1);
				double[,] x = new double[rows, cols];
                
				for (int k = qr.GetLength(1) - 1; k >= 0; k--) 
				{
					for (int i = 0; i < rows; i++)
					{
						x[i,k] = 0.0;
					}

					x[k,k] = 1.0;
					for (int j = k; j < cols; j++) 
					{
						if (qr[k,k] != 0) 
						{
							double s = 0.0;
				
							for (int i = k; i < rows; i++)
							{
								s += qr[i,k] * x[i,j];
							}

							s = -s / qr[k,k];
				
							for (int i = k; i < rows; i++)
							{
								x[i,j] += s * qr[i,k];
							}
						}
					}
				}

				return x;
			}
		}

	}
}
