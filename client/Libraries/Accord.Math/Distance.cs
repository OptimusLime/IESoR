using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Accord.Math
{
    /// <summary>
    ///   Static class Distance. Defines a set of extension methods defining distance measures.
    /// </summary>
    public static class Distance
    {
        /// <summary>
        ///   Gets the Square Mahalanobis distance between two points.
        /// </summary>
        /// <param name="x">A point in space.</param>
        /// <param name="y">A point in space.</param>
        /// <param name="precision">
        ///   The inverse of the covariance matrix of the distribution for the two points x and y.
        /// </param>
        /// <returns>The Square Mahalanobis distance between x and y.</returns>
        public static double SquareMahalanobis(this double[] x, double[] y, double[,] precision)
        {
            double[] d = new double[x.Length];
            for (int i = 0; i < x.Length; i++)
            {
                d[i] = x[i] - y[i];
            }

            return d.Multiply(precision.Multiply(d));
        }

        /// <summary>
        ///   Gets the Mahalanobis distance between two points.
        /// </summary>
        /// <param name="x">A point in space.</param>
        /// <param name="y">A point in space.</param>
        /// <param name="precision">
        ///   The inverse of the covariance matrix of the distribution for the two points x and y.
        /// </param>
        /// <returns>The Mahalanobis distance between x and y.</returns>
        public static double Mahalanobis(this double[] x, double[] y, double[,] precision)
        {
            return System.Math.Sqrt(SquareMahalanobis(x, y, precision));
        }

        /// <summary>
        ///   Gets the Manhattan distance between two points.
        /// </summary>
        /// <param name="x">A point in space.</param>
        /// <param name="y">A point in space.</param>
        /// <returns>The manhattan distance between x and y.</returns>
        public static double Manhattan(this double[] x, double[] y)
        {
            double sum = 0.0;
            for (int i = 0; i < x.Length; i++)
            {
                sum += System.Math.Abs(x[i] - y[i]);
            }
            return sum;
        }

        /// <summary>
        ///   Gets the Square Euclidean distance between two points.
        /// </summary>
        /// <param name="x">A point in space.</param>
        /// <param name="y">A point in space.</param>
        /// <returns>The Square Euclidean distance between x and y.</returns>
        public static double SquareEuclidean(this double[] a, double[] b)
        {
            double d = 0.0;

            for (int i = 0; i < a.Length; i++)
            {
                double u = a[i] - b[i];
                d += u * u;
            }

            return d;
        }

        /// <summary>
        ///   Gets the Euclidean distance between two points.
        /// </summary>
        /// <param name="x">A point in space.</param>
        /// <param name="y">A point in space.</param>
        /// <returns>The Euclidean distance between x and y.</returns>
        public static double Euclidean(this double[] a, double[] b)
        {
            return System.Math.Sqrt(SquareEuclidean(a, b));
        }
    }
}
