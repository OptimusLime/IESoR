/***************************************************************************
 *  Adapted from Lutz Roeder's Mapack for .NET, September 2000             *
 *  Adapted from Mapack for COM and Jama routines.                         *
 *  http://www.aisto.com/roeder/dotnet                                     *
 ***************************************************************************/


using System;

namespace Accord.Math.Decompositions
{

    /// <summary>
    ///		Cholesky Decomposition of a symmetric, positive definite matrix.
    ///	</summary>
    /// <remarks>
    ///		For a symmetric, positive definite matrix <c>A</c>, the Cholesky decomposition is a
    ///		lower triangular matrix <c>L</c> so that <c>A = L * L'</c>.
    ///		If the matrix is not symmetric or positive definite, the constructor returns a partial 
    ///		decomposition and sets two internal variables that can be queried using the
    ///		<see cref="Symmetric"/> and <see cref="PositiveDefinite"/> properties.
    /// 
    ///     Any square matrix A with non-zero pivots can be written as the product of a
    ///     lower triangular matrix L and an upper triangular matrix U; this is called
    ///     the LU decomposition. However, if A is symmetric and positive definite, we
    ///     can choose the factors such that U is the transpose of L, and this is called
    ///     the Cholesky decomposition. Both the LU and the Cholesky decomposition are
    ///     used to solve systems of linear equations.
    /// 
    ///     When it is applicable, the Cholesky decomposition is twice as efficient
    ///     as the LU decomposition.
    ///	</remarks>
    public sealed class CholeskyDecomposition
    {

        private double[,] L;
        private bool symmetric;
        private bool positiveDefinite;

        /// <summary>Constructs a Cholesky Decomposition.</summary>
        public CholeskyDecomposition(double[,] value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            if (value.GetLength(0) != value.GetLength(1))
            {
                throw new ArgumentException("Matrix is not square.", "value");
            }

            int dimension = value.GetLength(0);
            L = new double[dimension, dimension];

            double[,] a = value;
            double[,] l = L;

            this.positiveDefinite = true;
            this.symmetric = true;

            for (int j = 0; j < dimension; j++)
            {
                //double[] Lrowj = l.GetRow(j);
                double d = 0.0;
                for (int k = 0; k < j; k++)
                {
                    //double[] Lrowk = l.GetRow(k);
                    double s = 0.0;
                    for (int i = 0; i < k; i++)
                    {
                        s += l[k,i] * l[j,i];
                    }
                    l[j,k] = s = (a[j,k] - s) / l[k,k];
                    d = d + s * s;

                    this.symmetric = this.symmetric & (a[k,j] == a[j,k]);
                }

                d = a[j,j] - d;

                this.positiveDefinite = this.positiveDefinite & (d > 0.0);
                l[j,j] = System.Math.Sqrt(System.Math.Max(d, 0.0));
                for (int k = j + 1; k < dimension; k++)
                {
                    l[j,k] = 0.0;
                }
            }
        }

        /// <summary>Returns <see langword="true"/> if the matrix is symmetric.</summary>
        public bool Symmetric
        {
            get
            {
                return this.symmetric;
            }
        }

        /// <summary>Returns <see langword="true"/> if the matrix is positive definite.</summary>
        public bool PositiveDefinite
        {
            get
            {
                return this.positiveDefinite;
            }
        }

        /// <summary>Returns the left triangular factor <c>L</c> so that <c>A = L * L'</c>.</summary>
        public double[,] LeftTriangularFactor
        {
            get
            {
                return this.L;
            }
        }

        /// <summary>Solves a set of equation systems of type <c>A * X = B</c>.</summary>
        /// <param name="value">Right hand side matrix with as many rows as <c>A</c> and any number of columns.</param>
        /// <returns>Matrix <c>X</c> so that <c>L * L' * X = B</c>.</returns>
        /// <exception cref="T:System.ArgumentException">Matrix dimensions do not match.</exception>
        /// <exception cref="T:System.InvalidOperationException">Matrix is not symmetrix and positive definite.</exception>
        public double[,] Solve(double[,] value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            if (value.GetLength(0) != L.GetLength(0))
            {
                throw new ArgumentException("Matrix dimensions do not match.");
            }

            if (!this.symmetric)
            {
                throw new InvalidOperationException("Matrix is not symmetric.");
            }

            if (!this.positiveDefinite)
            {
                throw new InvalidOperationException("Matrix is not positive definite.");
            }

            int dimension = L.GetLength(0);
            int count = value.GetLength(1);

            double[,] B = (double[,])value.Clone();
            double[,] l = L;

            // Solve L*Y = B;
            for (int k = 0; k < L.GetLength(0); k++)
            {
                for (int i = k + 1; i < dimension; i++)
                {
                    for (int j = 0; j < count; j++)
                    {
                        B[i, j] -= B[k, j] * l[i,k];
                    }
                }

                for (int j = 0; j < count; j++)
                {
                    B[k, j] /= l[k,k];
                }
            }

            // Solve L'*X = Y;
            for (int k = dimension - 1; k >= 0; k--)
            {
                for (int j = 0; j < count; j++)
                {
                    B[k, j] /= l[k,k];
                }

                for (int i = 0; i < k; i++)
                {
                    for (int j = 0; j < count; j++)
                    {
                        B[i, j] -= B[k, j] * l[k,i];
                    }
                }
            }

            return B;
        }
    }
}
