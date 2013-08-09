/***************************************************************************
 *  Adapted from Lutz Roeder's Mapack for .NET, September 2000             *
 *  Adapted from Mapack for COM and Jama routines.                         *
 *  http://www.aisto.com/roeder/dotnet                                     *
 ***************************************************************************/

using System;
using AForge.Math;

using Accord.Math;

namespace Accord.Math.Decompositions
{

    /// <summary>
    ///   LU decomposition of a rectangular matrix.
    /// </summary>
    /// <remarks>
    ///   For an m-by-n matrix <c>A</c> with m >= n, the LU decomposition is an m-by-n
    ///   unit lower triangular matrix <c>L</c>, an n-by-n upper triangular matrix <c>U</c>,
    ///   and a permutation vector <c>piv</c> of length m so that <c>A(piv)=L*U</c>.
    ///   If m &lt; n, then <c>L</c> is m-by-m and <c>U</c> is m-by-n.
    ///   The LU decompostion with pivoting always exists, even if the matrix is
    ///   singular, so the constructor will never fail.  The primary use of the
    ///   LU decomposition is in the solution of square systems of simultaneous
    ///   linear equations. This will fail if <see cref="NonSingular"/> returns <see langword="false"/>.
    /// </remarks>
    public class LuDecomposition
    {
        private double[,] LU;
        private int pivotSign;
        private int[] pivotVector;

        /// <summary>Construct a LU decomposition.</summary>	
        public LuDecomposition(double[,] value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            this.LU = (double[,])value.Clone();
            double[,] lu = LU;
            int rows = value.GetLength(0);
            int columns = value.GetLength(1);
            pivotVector = new int[rows];
            for (int i = 0; i < rows; i++)
            {
                pivotVector[i] = i;
            }

            pivotSign = 1;
            //double[] LUrowi;
            double[] LUcolj = new double[rows];

            // Outer loop.
            for (int j = 0; j < columns; j++)
            {
                // Make a copy of the j-th column to localize references.
                for (int i = 0; i < rows; i++)
                {
                    LUcolj[i] = lu[i, j];
                }

                // Apply previous transformations.
                for (int i = 0; i < rows; i++)
                {
                    //LUrowi = lu[i];

                    // Most of the time is spent in the following dot product.
                    int kmax = System.Math.Min(i, j);
                    double s = 0.0;
                    for (int k = 0; k < kmax; k++)
                    {
                        s += lu[i, k] * LUcolj[k];
                    }
                    lu[i, j] = LUcolj[i] -= s;
                }

                // Find pivot and exchange if necessary.
                int p = j;
                for (int i = j + 1; i < rows; i++)
                {
                    if (System.Math.Abs(LUcolj[i]) > System.Math.Abs(LUcolj[p]))
                    {
                        p = i;
                    }
                }

                if (p != j)
                {
                    for (int k = 0; k < columns; k++)
                    {
                        double t = lu[p, k];
                        lu[p, k] = lu[j, k];
                        lu[j, k] = t;
                    }

                    int v = pivotVector[p];
                    pivotVector[p] = pivotVector[j];
                    pivotVector[j] = v;

                    pivotSign = -pivotSign;
                }

                // Compute multipliers.

                if (j < rows & lu[j, j] != 0.0)
                {
                    for (int i = j + 1; i < rows; i++)
                    {
                        lu[i, j] /= lu[j, j];
                    }
                }
            }
        }

        /// <summary>Returns if the matrix is non-singular.</summary>
        public bool NonSingular
        {
            get
            {
                for (int j = 0; j < LU.GetLength(1); j++)
                    if (LU[j, j] == 0)
                        return false;
                return true;
            }
        }

        /// <summary>Returns the determinant of the matrix.</summary>
        public double Determinant
        {
            get
            {
                if (LU.GetLength(0) != LU.GetLength(1))
                    throw new ArgumentException("Matrix must be square.");
                double determinant = (double)pivotSign;
                for (int j = 0; j < LU.GetLength(1); j++)
                    determinant *= LU[j, j];
                return determinant;
            }
        }

        /// <summary>Returns the lower triangular factor <c>L</c> with <c>A=LU</c>.</summary>
        public double[,] LowerTriangularFactor
        {
            get
            {
                int rows = LU.GetLength(0);
                int columns = LU.GetLength(1);
                double[,] X = new double[rows, columns];
                for (int i = 0; i < rows; i++)
                    for (int j = 0; j < columns; j++)
                        if (i > j)
                            X[i, j] = LU[i, j];
                        else if (i == j)
                            X[i, j] = 1.0;
                        else
                            X[i, j] = 0.0;
                return X;
            }
        }

        /// <summary>Returns the lower triangular factor <c>L</c> with <c>A=LU</c>.</summary>
        public double[,] UpperTriangularFactor
        {
            get
            {
                int rows = LU.GetLength(0);
                int columns = LU.GetLength(1);
                double[,] X = new double[rows, columns];
                for (int i = 0; i < rows; i++)
                    for (int j = 0; j < columns; j++)
                        if (i <= j)
                            X[i, j] = LU[i, j];
                        else
                            X[i, j] = 0.0;
                return X;
            }
        }

        /// <summary>Returns the pivot permuation vector.</summary>
        public double[] PivotPermutationVector
        {
            get
            {
                int rows = LU.GetLength(0);

                double[] p = new double[rows];
                for (int i = 0; i < rows; i++)
                {
                    p[i] = (double)this.pivotVector[i];
                }

                return p;
            }
        }

        /// <summary>Solves a set of equation systems of type <c>A * X = B</c>.</summary>
        /// <param name="value">Right hand side matrix with as many rows as <c>A</c> and any number of columns.</param>
        /// <returns>Matrix <c>X</c> so that <c>L * U * X = B</c>.</returns>
        public double[,] Inverse()
        {
            if (!this.NonSingular)
            {
                throw new InvalidOperationException("Matrix is singular");
            }

            // Copy right hand side with pivoting
            //value.Submatrix(pivotVector, 0, count - 1);

            int rows = LU.GetLength(1);
            int columns = LU.GetLength(1);
            int count = rows;
            double[,] lu = LU;

            double[,] X = new double[rows, columns];
            for (int i = 0; i < rows; i++)
            {
                int k = pivotVector[i];
                X[i, k] = 1.0; 
            }

            // Solve L*Y = B(piv,:)
            for (int k = 0; k < columns; k++)
            {
                for (int i = k + 1; i < columns; i++)
                {
                    for (int j = 0; j < count; j++)
                    {
                        X[i, j] -= X[k, j] * lu[i, k];
                    }
                }
            }

            // Solve U*X = Y;
            for (int k = columns - 1; k >= 0; k--)
            {
                for (int j = 0; j < count; j++)
                {
                    X[k, j] /= lu[k, k];
                }

                for (int i = 0; i < k; i++)
                {
                    for (int j = 0; j < count; j++)
                    {
                        X[i, j] -= X[k, j] * lu[i, k];
                    }
                }
            }

            return X;
        }

        /// <summary>Solves a set of equation systems of type <c>A * X = B</c>.</summary>
        /// <param name="value">Right hand side matrix with as many rows as <c>A</c> and any number of columns.</param>
        /// <returns>Matrix <c>X</c> so that <c>L * U * X = B</c>.</returns>
        public double[,] Solve(double[,] value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            if (value.GetLength(0) != this.LU.GetLength(0))
            {
                throw new ArgumentException("Invalid matrix dimensions.", "value");
            }

            if (!this.NonSingular)
            {
                throw new InvalidOperationException("Matrix is singular");
            }

            // Copy right hand side with pivoting
            int count = value.GetLength(1);
            double[,] X = value.Submatrix(pivotVector, 0, count - 1);

            int rows = LU.GetLength(1);
            int columns = LU.GetLength(1);
            double[,] lu = LU;

            // Solve L*Y = B(piv,:)
            for (int k = 0; k < columns; k++)
            {
                for (int i = k + 1; i < columns; i++)
                {
                    for (int j = 0; j < count; j++)
                    {
                        X[i, j] -= X[k, j] * lu[i, k];
                    }
                }
            }

            // Solve U*X = Y;
            for (int k = columns - 1; k >= 0; k--)
            {
                for (int j = 0; j < count; j++)
                {
                    X[k, j] /= lu[k, k];
                }

                for (int i = 0; i < k; i++)
                {
                    for (int j = 0; j < count; j++)
                    {
                        X[i, j] -= X[k, j] * lu[i, k];
                    }
                }
            }

            return X;
        }



        /// <summary>Solves a set of equation systems of type <c>A * X = B</c>.</summary>
        /// <param name="value">Right hand side matrix with as many rows as <c>A</c> and any number of columns.</param>
        /// <returns>Matrix <c>X</c> so that <c>L * U * X = B</c>.</returns>
        public double[] Solve(double[] value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            if (value.Length != this.LU.GetLength(0))
            {
                throw new ArgumentException("Invalid matrix dimensions.", "value");
            }

            if (!this.NonSingular)
            {
                throw new InvalidOperationException("Matrix is singular");
            }

            // Copy right hand side with pivoting
            int count = value.Length;
            double[] b = new double[count];
            for (int i = 0; i < b.Length; i++)
            {
                b[i] = value[pivotVector[i]];
            }

            int rows = LU.GetLength(1);
            int columns = LU.GetLength(1);
            double[,] lu = LU;

            // http://en.wikipedia.org/wiki/Backsubstitution#Forward_and_Back_Substitution

            // Solve L*Y = B
            double[] X = new double[count];
            for (int i = 0; i < rows; i++)
            {
                X[i] = b[i];
                for (int j = 0; j < i; j++)
                {
                    X[i] -= lu[i,j]*X[j];
                }
            }

            // Solve U*X = Y;
            for (int i = rows - 1; i >= 0; i--)
            {
                //double sum = 0.0;
                for (int j = columns - 1; j > i; j--)
                {
                    X[i] -= lu[i, j] * X[j];
                }
                X[i] /= lu[i, i];
            }
            return X;
        }
    }
}
