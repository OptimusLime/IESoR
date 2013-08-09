// Accord Math Library
// Accord.NET framework
// http://www.crsouza.com
//
// Copyright © César Souza, 2009
// cesarsouza@gmail.com
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Accord.Math.Decompositions;
using AForge.Math;
using System.Data;


namespace Accord.Math
{
    /// <summary>
    ///   Static class Matrix. Defines a set of extension methods that operate
    ///   mainly on multidimensional arrays and vectors.
    /// </summary>
    public static class Matrix
    {

        #region Comparison and Rounding
        public static bool Equals(this double[,] a, double[,] b, double threshold)
        {
            for (int i = 0; i < a.GetLength(0); i++)
            {
                for (int j = 0; j < b.GetLength(1); j++)
                {
                    if (System.Math.Abs(a[i, j] - b[i, j]) > threshold)
                        return false;
                }
            }
            return true;
        }

        public static bool Equals(this double[,] a, double[,] b)
        {
            for (int i = 0; i < a.GetLength(0); i++)
            {
                for (int j = 0; j < b.GetLength(1); j++)
                {
                    if (a[i, j] != b[i, j])
                        return false;
                }
            }
            return true;
        }

        public static double[,] Round(this double[,] a, int decimals)
        {
            double[,] r = new double[a.GetLength(0), a.GetLength(1)];

            for (int i = 0; i < a.GetLength(0); i++)
            {
                for (int j = 0; j < a.GetLength(1); j++)
                {
                    r[i, j] = System.Math.Round(a[i, j], decimals);
                }
            }

            return r;
        }
        #endregion


        #region Algebraic Operations

        /// <summary>
        ///   Multiplies two matrices.
        /// </summary>
        /// <param name="a">The left matrix.</param>
        /// <param name="b">The right matrix.</param>
        /// <returns>The product of the multiplication of the two matrices.</returns>
        public static double[,] Multiply(this double[,] a, double[,] b)
        {
            int m = a.GetLength(0);
            int n = b.GetLength(1);
            int p = a.GetLength(1);

            double[,] r = new double[m, n];

            for (int i = 0; i < m; i++)
                for (int j = 0; j < n; j++)
                    for (int k = 0; k < p; k++)
                        r[i, j] += a[i, k] * b[k, j];

            return r;
        }

        /// <summary>
        ///   Multiplies a vector and a matrix.
        /// </summary>
        /// <param name="a">A row vector.</param>
        /// <param name="b">A matrix.</param>
        /// <returns>The product of the multiplication of the row vector and the matrix.</returns>
        public static double[,] Multiply(this double[] a, double[,] b)
        {
            if (a.Length != b.GetLength(0))
                throw new Exception("Matrix dimensions must match");

            double[,] r = new double[a.Length, b.GetLength(1)];

            for (int i = 0; i < a.Length; i++)
                for (int j = 0; j < b.GetLength(1); j++)
                    for (int k = 0; k < b.GetLength(0); k++)
                        r[i, j] += a[i] * b[k, j];

            return r;
        }

        /// <summary>
        ///   Gets the inner product between two vectors (aT*b).
        /// </summary>
        /// <param name="a">A vector.</param>
        /// <param name="b">A vector.</param>
        /// <returns>The inner product of the multiplication of the vectors.</returns>
        public static double Multiply(this double[] a, double[] b)
        {
            double r = 0.0;

            for (int i = 0; i < a.GetLength(0); i++)
                r += a[i] * b[i];

            return r;
        }

        /// <summary>
        ///   Multiplies a matrix and a vector (a*bT).
        /// </summary>
        /// <param name="a">A matrix.</param>
        /// <param name="b">A column vector.</param>
        /// <returns>The product of the multiplication of matrix a and column vector b.</returns>
        public static double[] Multiply(this double[,] a, double[] b)
        {
            double[] r = new double[a.GetLength(0)];

            for (int i = 0; i < a.GetLength(0); i++)
                for (int j = 0; j < b.GetLength(0); j++)
                    for (int k = 0; k < a.GetLength(1); k++)
                        r[i] += a[i, k] * b[j];

            return r;
        }

        /// <summary>
        ///   Multiplies a matrix by a scalar.
        /// </summary>
        /// <param name="a">A matrix.</param>
        /// <param name="b">A scalar.</param>
        /// <returns>The product of the multiplication of matrix a and scalar x.</returns>
        public static double[,] Multiply(this double[,] a, double x)
        {
            double[,] r = new double[a.GetLength(0), a.GetLength(1)];

            for (int i = 0; i < a.GetLength(0); i++)
                for (int j = 0; j < a.GetLength(1); j++)
                    r[i, j] = a[i, j] * x;

            return r;
        }

        /// <summary>
        ///   Multiplies a vector by a scalar.
        /// </summary>
        /// <param name="a">A vector.</param>
        /// <param name="b">A scalar.</param>
        /// <returns>The product of the multiplication of vector a and scalar x.</returns>
        public static double[] Multiply(this double[] a, double x)
        {
            double[] r = new double[a.Length];

            for (int i = 0; i < a.GetLength(0); i++)
                r[i] = a[i] * x;

            return r;
        }

        /// <summary>
        ///   Multiplies a matrix by a scalar.
        /// </summary>
        /// <param name="a">A scalar.</param>
        /// <param name="b">A matrix.</param>
        /// <returns>The product of the multiplication of vector a and scalar x.</returns>
        public static double[,] Multiply(this double x, double[,] a)
        {
            return a.Multiply(x);
        }

        /// <summary>
        ///   Adds two matrices.
        /// </summary>
        /// <param name="a">A matrix.</param>
        /// <param name="b">A matrix.</param>
        /// <returns>The sum of the two matrices a and b.</returns>
        public static double[,] Add(this double[,] a, double[,] b)
        {
            if (a.GetLength(0) != b.GetLength(0) || a.GetLength(1) != b.GetLength(1))
                throw new Exception("Matrix dimensions must match");

            double[,] r = new double[a.GetLength(0), a.GetLength(1)];

            for (int i = 0; i < a.GetLength(0); i++)
                for (int j = 0; j < a.GetLength(1); j++)
                    r[i, j] = a[i, j] + b[i, j];

            return r;
        }

        /// <summary>
        ///   Subtracts two matrices.
        /// </summary>
        /// <param name="a">A matrix.</param>
        /// <param name="b">A matrix.</param>
        /// <returns>The subtraction of matrix b from matrix a.</returns>
        public static double[,] Subtract(this double[,] a, double[,] b)
        {
            double[,] r = new double[a.GetLength(0), a.GetLength(1)];

            for (int i = 0; i < a.GetLength(0); i++)
                for (int j = 0; j < a.GetLength(1); j++)
                    r[i, j] = a[i, j] - b[i, j];

            return r;
        }

        /// <summary>
        ///   Subtracts two vectors.
        /// </summary>
        /// <param name="a">A vector.</param>
        /// <param name="b">A vector.</param>
        /// <returns>The subtraction of vector b from vector a.</returns>
        public static double[] Subtract(this double[] a, double[] b)
        {
            double[] r = new double[a.Length];

            for (int i = 0; i < a.Length; i++)
                r[i] = a[i] - b[i];

            return r;
        }

        /// <summary>
        ///   Subtracts a scalar from a vector.
        /// </summary>
        /// <param name="a">A vector.</param>
        /// <param name="b">A scalar.</param>
        /// <returns>The subtraction of b from all elements in a.</returns>
        public static double[] Subtract(this double[] a, double b)
        {
            double[] r = new double[a.Length];

            for (int i = 0; i < a.Length; i++)
                r[i] = a[i] - b;

            return r;
        }

        /// <summary>
        ///   Adds two vectors.
        /// </summary>
        /// <param name="a">A vector.</param>
        /// <param name="b">A vector.</param>
        /// <returns>The addition of vector a to vector b.</returns>
        public static double[] Add(this double[] a, double[] b)
        {
            double[] r = new double[a.Length];

            for (int i = 0; i < a.Length; i++)
                r[i] = a[i] + b[i];

            return r;
        }
        #endregion


        #region Matrix Construction
        /// <summary>
        ///   Gets the diagonal vector from a matrix.
        /// </summary>
        /// <param name="m">A matrix.</param>
        /// <returns>The diagonal vector from matrix m.</returns>
        public static double[] Diagonal(this double[,] m)
        {
            double[] r = new double[m.GetLength(0)];

            for (int i = 0; i < r.Length; i++)
                r[i] = m[i, i];

            return r;
        }

        /// <summary>
        ///   Returns a square diagonal matrix of the given size.
        /// </summary>
        public static double[,] Diagonal(int size, double value)
        {
            double[,] m = new double[size, size];

            for (int i = 0; i < size; i++)
                m[i, i] = value;

            return m;
        }

        /// <summary>
        ///   Returns a matrix of the given size with value on its diagonal.
        /// </summary>
        public static double[,] Diagonal(int rows, int cols, double value)
        {
            double[,] m = new double[rows, cols];

            for (int i = 0; i < rows; i++)
                m[i, i] = value;

            return m;
        }

        /// <summary>
        ///   Return a square matrix with a vector of values on its diagonal.
        /// </summary>
        public static double[,] Diagonal(double[] values)
        {
            double[,] m = new double[values.Length, values.Length];

            for (int i = 0; i < values.Length; i++)
                m[i, i] = values[i];

            return m;
        }

        /// <summary>
        ///   Return a square matrix with a vector of values on its diagonal.
        /// </summary>
        public static double[,] Diagonal(int size, double[] values)
        {
            return Diagonal(size, size, values);
        }

        /// <summary>
        ///   Returns a matrix with a vector of values on its diagonal.
        /// </summary>
        public static double[,] Diagonal(int rows, int cols, double[] values)
        {
            double[,] m = new double[rows, cols];

            for (int i = 0; i < values.Length; i++)
                m[i, i] = values[i];

            return m;
        }

        /// <summary>
        ///   Returns a matrix with all elements set to a given value.
        /// </summary>
        public static double[,] Create(int rows, int cols, double value)
        {
            double[,] m = new double[rows, cols];

            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                    m[i, j] = value;

            return m;
        }

        /// <summary>
        ///   Returns a matrix with all elements set to a given value.
        /// </summary>
        public static double[,] Create(int size, double value)
        {
            return Create(size, size, value);
        }

        /// <summary>
        ///   Returns the Identity matrix of the given size.
        /// </summary>
        public static double[,] Identity(int size)
        {
            return Diagonal(size, 1.0);
        }

        /// <summary>
        ///   Creates a centering matrix of size n x n in the form (I - 1n)
        ///   where 1n is a matrix with all entries 1/n.
        /// </summary>
        public static double[,] Centering(int size)
        {
            return Matrix.Identity(size).Subtract(Matrix.Create(size, 1.0 / size));
        }

        /// <summary>
        ///   Creates a matrix with a single row vector.
        /// </summary>
        public static double[,] RowVector(double[] values)
        {
            double[,] r = new double[values.Length, 1];

            for (int i = 0; i < values.Length; i++)
            {
                r[i, 0] = values[i];
            }

            return r;
        }

        /// <summary>
        ///   Creates a matrix with a single column vector.
        /// </summary>
        public static double[,] ColumnVector(double[] values)
        {
            double[,] r = new double[1, values.Length];

            for (int i = 0; i < values.Length; i++)
            {
                r[0, i] = values[i];
            }

            return r;
        }
        #endregion


        #region Element Selection

        /// <summary>Returns a sub matrix extracted from the current matrix.</summary>
        /// <param name="startRow">Start row index</param>
        /// <param name="endRow">End row index</param>
        /// <param name="startColumn">Start column index</param>
        /// <param name="endColumn">End column index</param>
        public static double[,] Submatrix(this double[,] data, int startRow, int endRow, int startColumn, int endColumn)
        {
            int rows = data.GetLength(0);
            int cols = data.GetLength(1);

            if ((startRow > endRow) || (startColumn > endColumn) || (startRow < 0) ||
                (startRow >= rows) || (endRow < 0) || (endRow >= rows) ||
                (startColumn < 0) || (startColumn >= cols) || (endColumn < 0) ||
                (endColumn >= cols))
            {
                throw new ArgumentException("Argument out of range.");
            }

            double[,] X = new double[endRow - startRow + 1, endColumn - startColumn + 1];
            for (int i = startRow; i <= endRow; i++)
            {
                for (int j = startColumn; j <= endColumn; j++)
                {
                    X[i - startRow, j - startColumn] = data[i, j];
                }
            }

            return X;
        }

        /// <summary>Returns a sub matrix extracted from the current matrix.</summary>
        /// <param name="rowIndexes">Array of row indices</param>
        /// <param name="columnIndexes">Array of column indices</param>
        public static double[,] Submatrix(this double[,] data, int[] rowIndexes, int[] columnIndexes)
        {
            double[,] X = new double[rowIndexes.Length, columnIndexes.Length];

            for (int i = 0; i < rowIndexes.Length; i++)
            {
                for (int j = 0; j < columnIndexes.Length; j++)
                {
                    if ((rowIndexes[i] < 0) || (rowIndexes[i] >= data.GetLength(0)) ||
                        (columnIndexes[j] < 0) || (columnIndexes[j] >= data.GetLength(1)))
                    {
                        throw new ArgumentException("Argument out of range.");
                    }

                    X[i, j] = data[rowIndexes[i], columnIndexes[j]];
                }
            }

            return X;
        }

        /// <summary>Returns a sub matrix extracted from the current matrix.</summary>
        /// <param name="rowIndexes">Array of row indices</param>
        /// <param name="columnIndexes">Array of column indices</param>
        public static double[,] Submatrix(this double[,] data, int[] rowIndexes)
        {
            double[,] X = new double[rowIndexes.Length, data.GetLength(1)];

            for (int i = 0; i < rowIndexes.Length; i++)
            {
                for (int j = 0; j < data.GetLength(1); j++)
                {
                    if ((rowIndexes[i] < 0) || (rowIndexes[i] >= data.GetLength(0)))
                    {
                        throw new ArgumentException("Argument out of range.");
                    }

                    X[i, j] = data[rowIndexes[i], j];
                }
            }

            return X;
        }

        /// <summary>Returns a sub matrix extracted from the current matrix.</summary>
        /// <param name="rowIndexes">Array of row indices</param>
        /// <param name="columnIndexes">Array of column indices</param>
        public static double[] Submatrix(this double[] data, int[] indexes)
        {
            double[] X = new double[indexes.Length];

            for (int i = 0; i < indexes.Length; i++)
            {
                X[i] = data[indexes[i]];
            }

            return X;
        }

        /// <summary>Returns a sub matrix extracted from the current matrix.</summary>
        /// <param name="rowIndexes">Array of row indices</param>
        /// <param name="columnIndexes">Array of column indices</param>
        public static T[] Submatrix<T>(this T[] data, int i0, int i1)
        {
            T[] X = new T[i1 - i0 + 1];

            for (int i = i0; i < i1; i++)
            {
                X[i] = data[i];
            }

            return X;
        }

        /// <summary>Returns a sub matrix extracted from the current matrix.</summary>
        /// <param name="rowIndexes">Array of row indices</param>
        /// <param name="columnIndexes">Array of column indices</param>
        public static T[] Submatrix<T>(this T[] data, int first)
        {
            return Submatrix<T>(data, 0, first - 1);
        }

        /// <summary>Returns a sub matrix extracted from the current matrix.</summary>
        /// <param name="i0">Starttial row index</param>
        /// <param name="i1">End row index</param>
        /// <param name="c">Array of row indices</param>
        public static double[,] Submatrix(this double[,] data, int i0, int i1, int[] c)
        {
            if ((i0 > i1) || (i0 < 0) || (i0 >= data.GetLength(0))
                || (i1 < 0) || (i1 >= data.GetLength(0)))
            {
                throw new ArgumentException("Argument out of range.");
            }

            double[,] X = new double[i1 - i0 + 1, c.Length];

            for (int i = i0; i <= i1; i++)
            {
                for (int j = 0; j < c.Length; j++)
                {
                    if ((c[j] < 0) || (c[j] >= data.GetLength(1)))
                    {
                        throw new ArgumentException("Argument out of range.");
                    }

                    X[i - i0, j] = data[i, c[j]];
                }
            }

            return X;
        }

        /// <summary>Returns a sub matrix extracted from the current matrix.</summary>
        /// <param name="r">Array of row indices</param>
        /// <param name="j0">Start column index</param>
        /// <param name="j1">End column index</param>
        public static double[,] Submatrix(this double[,] data, int[] r, int j0, int j1)
        {
            if ((j0 > j1) || (j0 < 0) || (j0 >= data.GetLength(1)) || (j1 < 0)
                || (j1 >= data.GetLength(1)))
            {
                throw new ArgumentException("Argument out of range.");
            }

            double[,] X = new double[r.Length, j1 - j0 + 1];

            for (int i = 0; i < r.Length; i++)
            {
                for (int j = j0; j <= j1; j++)
                {
                    if ((r[i] < 0) || (r[i] >= data.GetLength(0)))
                    {
                        throw new ArgumentException("Argument out of range.");
                    }

                    X[i, j - j0] = data[r[i], j];
                }
            }

            return X;
        }

        /// <summary>
        ///   Gets a column vector from a matrix.
        /// </summary>
        public static double[] GetColumn(this double[,] m, int index)
        {
            double[] column = new double[m.GetLength(0)];

            for (int i = 0; i < column.Length; i++)
                column[i] = m[i, index];
            
            return column;
        }

        /// <summary>
        ///   Gets a row vector from a matrix.
        /// </summary>
        public static double[] GetRow(this double[,] m, int index)
        {
            double[] row = new double[m.GetLength(1)];

            for (int i = 0; i < row.Length; i++)
                row[i] = m[index, i];
            
            return row;
        }

        /// <summary>
        ///   Gets the indices of all elements matching a certain criteria.
        /// </summary>
        /// <typeparam name="T">The type of the array.</typeparam>
        /// <param name="data">The array to search inside.</param>
        /// <param name="func">The search criteria.</param>
        public static int[] Find<T>(T[] data, Func<T, bool> func)
        {
            List<int> idx = new List<int>();
            for (int i = 0; i < data.Length; i++)
            {
                if (func(data[i]))
                    idx.Add(i);
            }
            return idx.ToArray();
        }
        #endregion


        #region Matrix Characteristics
        /// <summary>
        ///   Returns true if a matrix is square.
        /// </summary>
        public static bool IsSquare<T>(this T[,] matrix)
        {
            return matrix.GetLength(0) == matrix.GetLength(1);
        }

        /// <summary>
        ///   Returns true if a matrix is symmetric.
        /// </summary>
        /// <param name="matrix"></param>
        /// <returns></returns>
        public static bool IsSymmetric(this double[,] matrix)
        {
            if (matrix.GetLength(0) == matrix.GetLength(1))
            {
                for (int i = 0; i < matrix.GetLength(0); i++)
                {
                    for (int j = 0; j <= i; j++)
                    {
                        if (matrix[i, j] != matrix[j, i])
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
            return false;
        }

        /// <summary>
        ///   Gets the trace of a matrix.
        /// </summary>
        /// <remarks>
        ///   The trace of an n-by-n square matrix A is defined to be the sum of the
        ///   elements on the main diagonal (the diagonal from the upper left to the
        ///   lower right) of A.
        /// </remarks>
        public static double Trace(this double[,] m)
        {
            double trace = 0.0;
            for (int i = 0; i < m.GetLength(0); i++)
            {
                trace += m[i, i];
            }
            return trace;
        }

        /// <summary>
        ///   Gets the Squared Euclidean norm for a matrix.
        /// </summary>
        public static double SquareNorm(this double[] a)
        {
            double sum = 0.0;
            for (int i = 0; i < a.Length; i++)
            {
                sum += a[i] * a[i];
            }
            return sum;
        }

        /// <summary>
        ///   Gets a n-dimensional vector containing the sum of all
        ///   elements in each column of a [m x n] matrix.
        /// </summary>
        public static double[] Sum(this double[,] m)
        {
            double[] sum = new double[m.GetLength(1)];

            for (int i = 0; i < m.GetLength(1); i++)
                for (int j = 0; j < m.GetLength(0); j++)
                    sum[i] += m[j, i];

            return sum;
        }

        /// <summary>
        ///   Gets the sum of all elements in a vector.
        /// </summary>
        public static double Sum(this double[] m)
        {
            double sum = 0.0;

            for (int i = 0; i < m.GetLength(1); i++)
                sum += m[i];

            return sum;
        }
        #endregion


        #region Matrix Operations
        /// <summary>
        ///   Elementwise power operation.
        /// </summary>
        /// <param name="a">A matrix.</param>
        /// <param name="p">A power.</param>
        public static double[,] DotPow(this double[,] a, double p)
        {
            double[,] r = new double[a.GetLength(0), a.GetLength(1)];

            for (int i = 0; i < a.GetLength(0); i++)
                for (int j = 0; j < a.GetLength(1); j++)
                    r[i, j] = System.Math.Pow(a[i, j], p);

            return r;
        }

        /// <summary>
        ///   Gets the transpose of a matrix.
        /// </summary>
        /// <param name="m">A matrix.</param>
        /// <returns>The transpose of matrix m.</returns>
        public static double[,] Transpose(this double[,] m)
        {
            double[,] t = new double[m.GetLength(1), m.GetLength(0)];
            for (int i = 0; i < m.GetLength(0); i++)
                for (int j = 0; j < m.GetLength(1); j++)
                    t[j, i] = m[i, j];

            return t;
        }

        /// <summary>
        ///   Gets the transpose of a vector.
        /// </summary>
        /// <param name="m">A vector.</param>
        /// <returns>The transpose of vector m.</returns>
        public static double[,] Transpose(this double[] m)
        {
            double[,] t = new double[1, m.GetLength(0)];
            for (int i = 0; i < m.Length; i++)
            {
                t[0, i] = m[i];
            }

            return t;
        }
        #endregion


        #region Conversions
        /// <summary>
        ///   Converts a DataTable to a double[,] array.
        /// </summary>
        public static double[,] ToMatrix(this DataTable table, out string[] columnNames)
        {
            double[,] m = new double[table.Rows.Count, table.Columns.Count];
            columnNames = new string[table.Columns.Count];

            for (int j = 0; j < table.Columns.Count; j++)
            {
                for (int i = 0; i < table.Rows.Count; i++)
                {
                    if (table.Columns[j].DataType == typeof(System.String))
                    {
                        m[i, j] = Double.Parse((String)table.Rows[i][j]);
                    }
                    else if (table.Columns[j].DataType == typeof(System.Boolean))
                    {
                        m[i, j] = (Boolean)table.Rows[i][j] ? 1.0 : 0.0;
                    }
                    else
                    {
                        m[i, j] = (Double)table.Rows[i][j];
                    }
                }

                columnNames[j] = table.Columns[j].Caption;
            }
            return m;
        }

        /// <summary>
        ///   Converts a DataTable to a double[,] array.
        /// </summary>
        public static double[,] ToMatrix(this DataTable table)
        {
            String[] names;
            return ToMatrix(table, out names);
        }

        /// <summary>
        ///   Converts a DataTable to a double[][] array.
        /// </summary>
        public static double[][] ToArray(this DataTable table)
        {
            double[][] m = new double[table.Rows.Count][];

            for (int i = 0; i < table.Rows.Count; i++)
            {
                m[i] = new double[table.Columns.Count];

                for (int j = 0; j < table.Columns.Count; j++)
                {
                    if (table.Columns[j].DataType == typeof(System.String))
                    {
                        m[i][j] = Double.Parse((String)table.Rows[i][j]);
                    }
                    else if (table.Columns[j].DataType == typeof(System.Boolean))
                    {
                        m[i][j] = (Boolean)table.Rows[i][j] ? 1.0 : 0.0;
                    }
                    else
                    {
                        m[i][j] = (Double)table.Rows[i][j];
                    }
                }
            }
            return m;
        }

        /// <summary>
        ///   Converts a DataColumn to a double[] array.
        /// </summary>
        public static double[] ToArray(this DataColumn column)
        {
            double[] m = new double[column.Table.Rows.Count];

            for (int i = 0; i < m.Length; i++)
            {
                object b = column.Table.Rows[i][column];

                if (column.DataType == typeof(System.String))
                {
                    m[i] = Double.Parse((String)b);
                }
                else if (column.DataType == typeof(System.Boolean))
                {
                    m[i] = (Boolean)b ? 1.0 : 0.0;
                }
                else
                {
                    m[i] = (Double)b;
                }
            }

            return m;
        }
        #endregion


        #region Inverse and Linear System Solving
        /// <summary>
        ///   Returns the LHS solution vector if the matrix is square or the least squares solution otherwise.
        /// </summary>
        /// <remarks>
        ///   Please note that this does not check if the matrix is non-singular before attempting to solve.
        /// </remarks>
        public static double[,] Solve(this double[,] m, double[,] rightSide)
        {
            if (m.GetLength(0) == m.GetLength(1))
            {
                // Solve by LU Decomposition if matrix is square.
                return new LuDecomposition(m).Solve(rightSide);
            }
            else
            {
                // Solve by QR Decomposition if not.
                return new QrDecomposition(m).Solve(rightSide);
            }
        }

        /// <summary>
        ///   Inverse of the matrix if matrix is square, pseudoinverse otherwise.
        /// </summary>
        public static double[,] Inverse(this double[,] m)
        {
            return m.Solve(Matrix.Diagonal(m.GetLength(0), 1.0));
        }

        public static double[,] PseudoInverse(this double[,] m)
        {
            SingularValueDecomposition svd = new SingularValueDecomposition(m);
            return svd.Solve(Matrix.Diagonal(m.GetLength(0), m.GetLength(1), 1.0));
        }
        #endregion


    }
}
