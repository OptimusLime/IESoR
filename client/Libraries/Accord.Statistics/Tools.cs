// Accord Statistics Library
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

using Accord.Math;

using AForge;

namespace Accord.Statistics
{
    /// <summary>
    ///     Set of statistics functions
    /// </summary>
    /// 
    /// <remarks>
    ///     This class represents collection of functions used
    ///     in statistics. Every Matrix function assumes data is organized
    ///     in a table-like model, where Columns represents variables and
    ///     Rows represents a observation of each variable.
    /// </remarks>
    /// 
    public static class Tools
    {

        #region Vector

        /// <summary>Computes the Mean of the given values.</summary>
        /// <param name="vector">A double array containing the vector members.</param>
        /// <returns>The mean of the given data.</returns>
        public static double Mean(double[] values)
        {
            double sum = 0.0;
            double n = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                sum += values[i];
            }
            return sum / n;
        }

        /// <summary>Computes the Standard Deviation of the given values.</summary>
        /// <param name="vector">A double array containing the vector members.</param>
        /// <returns>The standard deviation of the given data.</returns>
        public static double StandardDeviation(double[] values)
        {
            return StandardDeviation(values, Mean(values));
        }

        /// <summary>Computes the Standard Deviation of the given values.</summary>
        /// <param name="vector">A double array containing the vector members.</param>
        /// <param name="mean">The mean of the vector, if already known.</param>
        /// <returns>The standard deviation of the given data.</returns>
        public static double StandardDeviation(double[] values, double mean)
        {
            return System.Math.Sqrt(Variance(values, mean));
        }

        /// <summary>
        ///   Computes the Standard Error for a sample size, which estimates the
        ///   standard deviation of the sample mean based on the population mean.
        /// </summary>
        /// <param name="samples">The sample size.</param>
        /// <param name="standardDeviation">The sample standard deviation.</param>
        /// <returns>The standard error for the sample.</returns>
        public static double StandardError(int samples, double standardDeviation)
        {
            return standardDeviation / System.Math.Sqrt(samples);
        }

        /// <summary>
        ///   Computes the Standard Error for a sample size, which estimates the
        ///   standard deviation of the sample mean based on the population mean.
        /// </summary>
        /// <param name="vector">A double array containing the samples.</param>
        /// <returns>The standard error for the sample.</returns>
        public static double StandardError(double[] values)
        {
            return StandardError(values.Length, StandardDeviation(values));
        }

        /// <summary>Computes the Median of the given values.</summary>
        /// <param name="vector">A double array containing the vector members.</param>
        /// <returns>The median of the given data.</returns>
        public static double Median(double[] values)
        {
            return Median(values, false);
        }

        /// <summary>Computes the Median of the given values.</summary>
        /// <param name="values">An integer array containing the vector members.</param>
        /// <param name="alreadySorted">A boolean parameter informing if the given values have already been sorted.</param>
        /// <returns>The median of the given data.</returns>
        public static double Median(double[] values, bool alreadySorted)
        {
            double[] data = new double[values.Length];
            values.CopyTo(data, 0); // Creates a copy of the given values,

            if (!alreadySorted) // So we can sort it without modifying the original array.
                Array.Sort(data);

            int N = data.Length;

            if ((N % 2) == 0)
                return (data[N / 2] + data[(N / 2) + 1]) * 0.5; // N is even 
            else return data[(N + 1) / 2];                      // N is odd
        }


        /// <summary>Computes the Variance of the given values.</summary>
        /// <param name="vector">A double precision number array containing the vector members.</param>
        /// <returns>The variance of the given data.</returns>
        public static double Variance(double[] values)
        {
            return Variance(values, Mean(values));
        }

        /// <summary>Computes the Variance of the given values.</summary>
        /// <param name="vector">A number array containing the vector members.</param>
        /// <param name="mean">The mean of the array, if already known.</param>
        /// <returns>The variance of the given data.</returns>
        public static double Variance(double[] values, double mean)
        {
            double sum1 = 0.0;
            double sum2 = 0.0;
            double N = values.Length;
            double x = 0.0;

            for (int i = 0; i < values.Length; i++)
            {
                x = values[i] - mean;
                sum1 += x;
                sum2 += x * x;
            }

            // Sample variance
            return (sum2 - ((sum1 * sum1) / N)) / (N - 1);
        }


        /// <summary>Computes the Mode of the given values.</summary>
        /// <param name="values">A number array containing the vector values.</param>
        /// <returns>The variance of the given data.</returns>
        public static double Mode(double[] values)
        {
            int[] itemCount = new int[values.Length];
            double[] itemArray = new double[values.Length];
            int count = 0;

            for (int i = 0; i < values.Length; i++)
            {
                int index = Array.IndexOf<double>(itemArray, values[i], 0, count);

                if (index >= 0)
                {
                    itemCount[index]++;
                }
                else
                {
                    itemArray[count] = values[i];
                    itemCount[count] = 1;
                    count++;
                }
            }

            int maxValue = 0;
            int maxIndex = 0;

            for (int i = 0; i < count; i++)
            {
                if (itemCount[i] > maxValue)
                {
                    maxValue = itemCount[i];
                    maxIndex = i;
                }
            }

            return itemArray[maxIndex];
        }

        /// <summary>Computes the Covariance between two values arrays.</summary>
        /// <param name="u">A number array containing the first vector members.</param>
        /// <param name="v">A number array containing the second vector members.</param>
        /// <returns>The variance of the given data.</returns>
        public static double Covariance(double[] u, double[] v)
        {
            if (u.Length != v.Length)
            {
                throw new ArgumentException("Vector sizes must be equal.", "u");
            }

            double uSum = 0.0;
            double vSum = 0.0;
            double N = u.Length;

            // Calculate Sums for each vector
            for (int i = 0; i < u.Length; i++)
            {
                uSum += u[i];
                vSum += v[i];
            }

            double uMean = uSum / N;
            double vMean = vSum / N;

            double covariance = 0.0;
            for (int i = 0; i < u.Length; i++)
            {
                covariance += (u[i] - uMean) * (v[i] - vMean);
            }

            return covariance / (N - 1); // sample variance
        }

        /// <summary>
        ///   Computes the Skewness for the given values.
        /// </summary>
        /// <remarks>
        ///   Skewness characterizes the degree of asymmetry of a distribution
        ///   around its mean. Positive skewness indicates a distribution with
        ///   an asymmetric tail extending towards more positive values. Negative
        ///   skewness indicates a distribution with an asymmetric tail extending
        ///   towards more negative values.
        /// </remarks>
        /// <param name="values">A number array containing the vector values.</param>
        /// <returns>The skewness of the given data.</returns>
        public static double Skewness(double[] values)
        {
            double mean = Mean(values);
            return Skewness(values, mean, StandardDeviation(values, mean));
        }

        /// <summary>
        ///   Computes the Skewness for the given values.
        /// </summary>
        /// <remarks>
        ///   Skewness characterizes the degree of asymmetry of a distribution
        ///   around its mean. Positive skewness indicates a distribution with
        ///   an asymmetric tail extending towards more positive values. Negative
        ///   skewness indicates a distribution with an asymmetric tail extending
        ///   towards more negative values.
        /// </remarks>
        /// <param name="values">A number array containing the vector values.</param>
        /// <param name="mean">The values' mean, if already known.</param>
        /// <param name="standardDeviation">The values' standard deviations, if already known.</param>
        /// <returns>The skewness of the given data.</returns>
        public static double Skewness(double[] values, double mean, double standardDeviation)
        {
            int n = values.Length;
            double sum = 0.0;
            for (int i = 0; i < n; i++)
            {
                // Sum of third moment deviations
                sum += System.Math.Pow(values[i] - mean, 3);
            }

            return sum / ((n - 1) * System.Math.Pow(standardDeviation, 3));
        }

        public static double Kurtosis(double[] values)
        {
            double mean = Mean(values);
            return Kurtosis(values, mean, StandardDeviation(values, mean));
        }

        public static double Kurtosis(double[] values, double mean, double standardDeviation)
        {
            int n = values.Length;
            double sum = 0.0;
            for (int i = 0; i < n; i++)
            {
                // Sum of fourth moment deviations
                sum += System.Math.Pow(values[i] - mean, 4);
            }

            return sum / (n * System.Math.Pow(standardDeviation, 4)) - 3.0;
        }
        #endregion


        // ------------------------------------------------------------


        #region Matrix


        /// <summary>Calculates the matrix Sum vector.</summary>
        /// <param name="m">A matrix whose sums will be calculated.</param>
        /// <returns>Returns a vector containing the sums of each variable in the given matrix.</returns>
        public static double[] Sum(double[,] value)
        {
            double[] sum = new double[value.GetLength(1)];

            // for each row
            for (int i = 0; i < value.GetLength(0); i++)
            {
                // for each column
                for (int j = 0; j < value.GetLength(1); j++)
                {
                    sum[j] += value[i, j];
                }
            }
            return sum;
        }

        /// <summary>Calculates the matrix Mean vector.</summary>
        /// <param name="m">A matrix whose means will be calculated.</param>
        /// <returns>Returns a vector containing the means of the given matrix.</returns>
        public static double[] Mean(double[,] value)
        {
            double[] mean = new double[value.GetLength(1)];
            double rows = value.GetLength(0);

            // for each column
            for (int j = 0; j < value.GetLength(1); j++)
            {
                // for each row
                for (int i = 0; i < value.GetLength(0); i++)
                {
                    mean[j] += value[i, j];
                }

                mean[j] = mean[j] / rows;
            }

            return mean;
        }

        /// <summary>Calculates the matrix Mean vector.</summary>
        /// <param name="m">A matrix whose means will be calculated.</param>
        /// <returns>Returns a vector containing the means of the given matrix.</returns>
        public static double[] Mean(double[,] value, double[] sumVector)
        {
            double[] mean = new double[value.GetLength(1)];
            double rows = value.GetLength(0);

            // for each column
            for (int j = 0; j < value.GetLength(1); j++)
            {
                mean[j] = sumVector[j] / rows;
            }

            return mean;
        }

        /// <summary>Calculates the matrix Standard Deviations vector.</summary>
        /// <param name="m">A matrix whose deviations will be calculated.</param>
        /// <returns>Returns a vector containing the standard deviations of the given matrix.</returns>
        public static double[] StandardDeviation(double[,] value)
        {
            return StandardDeviation(value, Mean(value));
        }

        /// <summary>Calculates the matrix Standard Deviations vector.</summary>
        /// <param name="m">A matrix whose deviations will be calculated.</param>
        /// <param name="meanVector">The mean vector containing already calculated means for each column of the matix.</param>
        /// <returns>Returns a vector containing the standard deviations of the given matrix.</returns>
        public static double[] StandardDeviation(this double[,] value, double[] meanVector)
        {
            return Accord.Math.Tools.Sqrt(Variance(value, meanVector));
        }

        /// <summary>Calculates the matrix Medians vector.</summary>
        /// <param name="m">A matrix whose deviations will be calculated.</param>
        /// <returns>Returns a vector containing the means of the given matrix.</returns>
        public static double[] Variance(this double[,] value)
        {
            return Variance(value, Mean(value));
        }

        /// <summary>Calculates the matrix Medians vector.</summary>
        /// <param name="m">A matrix whose deviations will be calculated.</param>
        /// /// <param name="meanVector">The mean vector containing already calculated means for each column of the matix.</param>
        /// <returns>Returns a vector containing the mean of the given matrix.</returns>
        public static double[] Variance(this double[,] value, double[] means)
        {
            double[] variance = new double[value.GetLength(1)];

            // for each column (for each variable)
            for (int j = 0; j < value.GetLength(1); j++)
            {
                double sum1 = 0.0;
                double sum2 = 0.0;
                double x = 0.0;
                double N = value.GetLength(0);

                // for each row (observation of the variable)
                for (int i = 0; i < value.GetLength(0); i++)
                {
                    x = value[i, j] - means[j];
                    sum1 += x;
                    sum2 += x * x;
                }

                // calculate the variance
                variance[j] = (sum2 - ((sum1 * sum1) / N)) / (N - 1);
            }

            return variance;
        }

        /// <summary>Calculates the matrix Medians vector.</summary>
        /// <param name="m">A matrix whose deviations will be calculated.</param>
        /// <returns>Returns a vector containing the medians of the given matrix.</returns>
        public static double[] Median(double[,] value)
        {
            int rows = value.GetLength(0);
            int cols = value.GetLength(1);
            double[] medians = new double[cols];

            for (int i = 0; i < cols; i++)
            {
                double[] data = new double[rows];

                // Creates a copy of the given values
                for (int j = 0; j < rows; j++)
                    data[j] = value[j, i];

                Array.Sort(data); // Sort it

                int N = data.Length;

                if ((N % 2) == 0)
                    medians[i] = (data[N / 2] + data[(N / 2) + 1]) * 0.5; // N is even 
                else medians[i] = data[(N + 1) / 2];                      // N is odd
            }

            return medians;
        }


        /// <summary>Calculates the matrix Modes vector.</summary>
        /// <param name="m">A matrix whose modes will be calculated.</param>
        /// <returns>Returns a vector containing the modes of the given matrix.</returns>
        public static double[] Mode(this double[,] matrix)
        {
            double[] mode = new double[matrix.GetLength(1)];

            for (int i = 0; i < mode.Length; i++)
            {
                int[] itemCount = new int[matrix.GetLength(0)];
                double[] itemArray = new double[matrix.GetLength(0)];
                int count = 0;

                // for each row
                for (int j = 0; j < matrix.GetLength(0); j++)
                {
                    int index = Array.IndexOf<double>(itemArray, matrix[j, i], 0, count);

                    if (index >= 0)
                    {
                        itemCount[index]++;
                    }
                    else
                    {
                        itemArray[count] = matrix[j, i];
                        itemCount[count] = 1;
                        count++;
                    }
                }

                int maxValue = 0;
                int maxIndex = 0;

                for (int j = 0; j < count; j++)
                {
                    if (itemCount[i] > maxValue)
                    {
                        maxValue = itemCount[j];
                        maxIndex = j;
                    }
                }

                mode[i] = itemArray[maxIndex];
            }

            return mode;
        }

        /// <summary>
        ///   Computes the Skewness for the given values.
        /// </summary>
        /// <remarks>
        ///   Skewness characterizes the degree of asymmetry of a distribution
        ///   around its mean. Positive skewness indicates a distribution with
        ///   an asymmetric tail extending towards more positive values. Negative
        ///   skewness indicates a distribution with an asymmetric tail extending
        ///   towards more negative values.
        /// </remarks>
        /// <param name="values">A number array containing the vector values.</param>
        /// <returns>The skewness of the given data.</returns>
        public static double[] Skewness(double[,] matrix)
        {
            double[] means = Mean(matrix);
            return Skewness(matrix, means, StandardDeviation(matrix, means));
        }

        /// <summary>
        ///   Computes the Skewness for the given values.
        /// </summary>
        /// <remarks>
        ///   Skewness characterizes the degree of asymmetry of a distribution
        ///   around its mean. Positive skewness indicates a distribution with
        ///   an asymmetric tail extending towards more positive values. Negative
        ///   skewness indicates a distribution with an asymmetric tail extending
        ///   towards more negative values.
        /// </remarks>
        /// <param name="values">A number array containing the vector values.</param>
        /// <param name="mean">The values' mean, if already known.</param>
        /// <param name="standardDeviation">The values' standard deviations, if already known.</param>
        /// <returns>The skewness of the given data.</returns>
        public static double[] Skewness(double[,] matrix, double[] means, double[] standardDeviations)
        {
            int n = matrix.GetLength(0);
            double[] skewness = new double[matrix.GetLength(1)];
            for (int j = 0; j < skewness.Length; j++)
            {
                double sum = 0.0;
                for (int i = 0; i < n; i++)
                {
                    // Sum of third moment deviations
                    sum += System.Math.Pow(matrix[i, j] - means[j], 3);
                }

                skewness[j] = sum / ((n - 1) * System.Math.Pow(standardDeviations[j], 3));
            }

            return skewness;
        }

        public static double[] Kurtosis(double[,] matrix)
        {
            double[] means = Mean(matrix);
            return Kurtosis(matrix, means, StandardDeviation(matrix, means));
        }

        public static double[] Kurtosis(double[,] matrix, double[] means, double[] standardDeviations)
        {
            int n = matrix.GetLength(0);
            double[] kurtosis = new double[matrix.GetLength(1)];
            for (int j = 0; j < kurtosis.Length; j++)
            {
                double sum = 0.0;
                for (int i = 0; i < n; i++)
                {
                    // Sum of fourth moment deviations
                    sum += System.Math.Pow(matrix[i, j] - means[j], 4);
                }

                kurtosis[j] = sum / (n * System.Math.Pow(standardDeviations[j], 4)) - 3.0;
            }

            return kurtosis;
        }

        public static double[] StandardError(double[,] matrix)
        {
            return StandardError(matrix.GetLength(0), StandardDeviation(matrix));
        }

        public static double[] StandardError(int samples, double[] standardDeviations)
        {
            double[] standardErrors = new double[standardDeviations.Length];
            double sqrt = System.Math.Sqrt(samples);
            for (int i = 0; i < standardDeviations.Length; i++)
            {
                standardErrors[i] = standardDeviations[i] / sqrt;
            }
            return standardErrors;
        }


        /// <summary>Calculates the covariance matrix of a sample matrix, returning a new matrix object</summary>
        /// <remarks>
        ///   In statistics and probability theory, the covariance matrix is a matrix of
        ///   covariances between elements of a vector. It is the natural generalization
        ///   to higher dimensions of the concept of the variance of a scalar-valued
        ///   random variable.
        /// </remarks>
        /// <returns>The covariance matrix.</returns>
        public static double[,] Covariance(this double[,] matrix)
        {
            return Covariance(matrix, Mean(matrix));
        }

        public static double[,] Covariance(this double[,] matrix, double[] mean)
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);

            if (rows == 1)
            {
                throw new ArgumentException("Sample has only one observation.", "matrix");
            }

            double N = rows;
            double[,] cov = new double[cols, cols];
            for (int i = 0; i < cols; i++)
            {
                for (int j = i; j < cols; j++)
                {
                    double c = 0.0;
                    for (int k = 0; k < rows; k++)
                    {
                        c += (matrix[k, j] - mean[j]) * (matrix[k, i] - mean[i]);
                    }
                    c /= N - 1.0;
                    cov[i, j] = c;
                    cov[j, i] = c;
                }
            }

            return cov;
        }

        public static double[,] Scatter(double[,] matrix, double[] mean)
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);

            if (rows == 1)
            {
                throw new ArgumentException("Sample has only one observation.", "matrix");
            }

            double N = rows;
            double[,] cov = new double[cols, cols];
            for (int i = 0; i < cols; i++)
            {
                for (int j = i; j < cols; j++)
                {
                    double s = 0.0;
                    for (int k = 0; k < rows; k++)
                    {
                        s += (matrix[k, j] - mean[j]) * (matrix[k, i] - mean[i]);
                    }
                    s /= N;
                    cov[i, j] = s;
                    cov[j, i] = s;
                }
            }

            return cov;
        }

        /// <summary>Calculates the correlation matrix of this samples, returning a new matrix object</summary>
        /// <remarks>
        /// In statistics and probability theory, the correlation matrix is the same
        /// as the covariance matrix of the standardized random variables.
        /// </remarks>
        /// <returns>The correlation matrix</returns>
        public static double[,] Correlation(double[,] matrix)
        {
            double[] means = Mean(matrix);
            return Correlation(matrix, means, StandardDeviation(matrix, means));
        }

        /// <summary>
        ///   Calculates the correlation matrix of this samples, returning a new matrix object
        /// </summary>
        /// <remarks>
        ///   In statistics and probability theory, the correlation matrix is the same
        ///   as the covariance matrix of the standardized random variables.
        /// </remarks>
        /// <returns>The correlation matrix</returns>
        public static double[,] Correlation(double[,] matrix, double[] mean, double[] stdDev)
        {
            double[,] scores = ZScores(matrix, mean, stdDev);

            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);

            double N = rows;
            double[,] cor = new double[cols, cols];
            for (int i = 0; i < cols; i++)
            {
                for (int j = i; j < cols; j++)
                {
                    double c = 0.0;
                    for (int k = 0; k < rows; k++)
                    {
                        c += scores[k, j] * scores[k, i];
                    }
                    c /= N - 1.0;
                    cor[i, j] = c;
                    cor[j, i] = c;
                }
            }

            return cor;
        }


        /// <summary>Generates the Standard Scores, also known as Z-Scores, the core from the given data.</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static double[,] ZScores(double[,] value)
        {
            double[] mean = Mean(value);
            return ZScores(value, mean, StandardDeviation(value, mean));
        }


        public static double[,] ZScores(double[,] value, double[] means, double[] deviations)
        {
            double[,] m = (double[,])value.Clone();

            Center(m, means);
            Standardize(m, deviations);

            return m;
        }



        /// <summary>Centers column data, subtracting the empirical mean from each variable.</summary>
        /// <param name="m">A matrix where each column represent a variable and each row represent a observation.</param>
        public static void Center(double[,] value)
        {
            Center(value, Mean(value));
        }

        /// <summary>Centers column data, subtracting the empirical mean from each variable.</summary>
        /// <param name="m">A matrix where each column represent a variable and each row represent a observation.</param>
        public static void Center(double[,] value, double[] means)
        {
            for (int i = 0; i < value.GetLength(0); i++)
            {
                for (int j = 0; j < value.GetLength(1); j++)
                {
                    value[i, j] -= means[j];
                }
            }
        }


        /// <summary>Standardizes column data, removing the empirical standard deviation from each variable.</summary>
        /// <param name="m">A matrix where each column represent a variable and each row represent a observation.</param>
        /// <remarks>This method does not remove the empirical mean prior to execution.</remarks>
        public static void Standardize(double[,] value)
        {
            Standardize(value, StandardDeviation(value));
        }

        public static void Standardize(this double[,] value, double[] deviations)
        {
            for (int i = 0; i < value.GetLength(0); i++)
            {
                for (int j = 0; j < value.GetLength(1); j++)
                {
                    value[i, j] /= deviations[j];
                }
            }
        }


        public static DoubleRange[] Range(double[,] value)
        {
            DoubleRange[] ranges = new DoubleRange[value.GetLength(1)];

            for (int j = 0; j < ranges.Length; j++)
            {
                double max = value[0, j];
                double min = value[0, j];

                for (int i = 0; i < value.GetLength(0); i++)
                {
                    if (value[i, j] > max)
                        max = value[i, j];

                    if (value[i, j] < min)
                        min = value[i, j];
                }

                ranges[j] = new DoubleRange(min, max);
            }

            return ranges;
        }
        #endregion


        // ------------------------------------------------------------
    }
}

