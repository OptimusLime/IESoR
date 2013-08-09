// Accord Statistics Library
// Accord.NET framework
// http://www.crsouza.com
//
// Copyright © César Souza, 2009
// cesarsouza@gmail.com
//

using System;
using System.Collections.Generic;
using System.Text;
using System.Data;

using AForge;
using Accord.Math;

namespace Accord.Statistics.Analysis
{

    /// <summary>
    ///   Descriptive statistics are used to describe the basic features of the data
    ///   in a study. They provide simple summaries about the sample and the measures.
    ///   Together with simple graphics analysis, they form the basis of virtually
    ///   every quantitative analysis of data.
    /// </summary>
    public class DescriptiveAnalysis
    {

        private int samples;
        private int variables;
        private double[] sums;
        private double[] means;
        private double[] standardDeviations;
        private double[] variances;
        private double[] medians;
        private double[] modes;
        private string[] columnNames;
        private DoubleRange[] ranges;

        private double[] kurtosis;
        private Double[] skewness;
        private double[] standardErrors;


        private double[,] covarianceMatrix;
        private double[,] correlationMatrix;

        private double[,] zScores;
        private double[,] dScores;
        private double[,] sourceMatrix;

        private DescriptiveMeasureCollection measuresCollection;



        /// <summary>Constructs the Descriptive Analysis.</summary>
        /// <param name="data">The source data to perform analysis.</param>
        public DescriptiveAnalysis(double[,] data, string[] columnNames)
        {
            this.sourceMatrix = data;
            this.columnNames = columnNames;

            this.samples = data.GetLength(0);
            this.variables = data.GetLength(1);

            // Run analysis
            this.sums = Statistics.Tools.Sum(data);
            this.means = Statistics.Tools.Mean(data, sums);
            this.standardDeviations = Statistics.Tools.StandardDeviation(data, means);
            this.ranges = Statistics.Tools.Range(data);
            this.kurtosis = Statistics.Tools.Kurtosis(data, means, standardDeviations);
            this.skewness = Statistics.Tools.Skewness(data, means, standardDeviations);
            this.medians = Statistics.Tools.Median(data);
            this.modes = Statistics.Tools.Mode(data);
            this.variances = Statistics.Tools.Variance(data, means);
            this.standardErrors = Statistics.Tools.StandardError(samples, standardDeviations);

            // Mean centered data
            this.dScores = (double[,])data.Clone();
            Statistics.Tools.Center(dScores, means);

            // Mean centered and standardized data
            this.zScores = Statistics.Tools.ZScores(data, means, standardDeviations);

            // Covariance and correlation
            this.covarianceMatrix = Statistics.Tools.Covariance(data, means);
            this.correlationMatrix = Statistics.Tools.Covariance(zScores);


            // Create object-oriented structure to access data
            DescriptiveMeasures[] measures = new DescriptiveMeasures[sourceMatrix.GetLength(1)];
            for (int i = 0; i < measures.Length; i++)
            {
                measures[i] = new DescriptiveMeasures(this, i);
            }
            this.measuresCollection = new DescriptiveMeasureCollection(measures);
        }


        #region Properties
        /// <summary>
        ///   Gets the source matrix from which the analysis was run.
        /// </summary>
        public double[,] Source
        {
            get { return this.sourceMatrix; }
        }

        /// <summary>
        ///   Gets the column names from the variables in the data.
        /// </summary>
        public String[] ColumnNames
        {
            get { return this.columnNames; }
        }

        /// <summary>
        /// Gets the mean subtracted data.
        /// </summary>
        public double[,] DeviationScores
        {
            get { return this.dScores; }
        }

        /// <summary>
        /// Gets the mean subtracted and deviation divided data. Also known as Z-Scores.
        /// </summary>
        public double[,] StandardScores
        {
            get { return this.zScores; }
        }

        /// <summary>
        /// Gets the Covariance Matrix
        /// </summary>
        /// <seealso cref="AForge.Statistics.Tools.CovarianceMatrix"/>
        public double[,] CovarianceMatrix
        {
            get { return covarianceMatrix; }
        }

        /// <summary>
        /// Gets the Correlation Matrix
        /// </summary>
        /// <seealso cref="AForge.Statistics.Tools.CorrelationMatrix"/>
        public double[,] CorrelationMatrix
        {
            get { return correlationMatrix; }
        }

        /// <summary>
        /// Gets a vector containing the Mean of each column of data.
        /// </summary>
        public double[] Means
        {
            get { return means; }
        }

        /// <summary>
        /// Gets a vector containing the Standard Deviation of each column of data.
        /// </summary>
        public double[] StandardDeviations
        {
            get { return standardDeviations; }
        }

        /// <summary>
        /// Gets a vector containing the Standard Error of the Mean of each column of data.
        /// </summary>
        public double[] StandardErrors
        {
            get { return standardErrors; }
        }

        /// <summary>
        /// Gets a vector containing the Mode of each column of data.
        /// </summary>
        public double[] Modes
        {
            get { return modes; }
        }

        /// <summary>
        /// Gets a vector containing the Median of each column of data.
        /// </summary>
        public double[] Medians
        {
            get { return medians; }
        }

        /// <summary>
        /// Gets a vector containing the Variance of each column of data.
        /// </summary>
        public double[] Variances
        {
            get { return variances; }
        }

        /// <summary>
        /// Gets an array containing the Ranges of each column of data.
        /// </summary>
        public DoubleRange[] Ranges
        {
            get { return ranges; }
        }

        /// <summary>
        /// Gets an array containing the sum of each column of data.
        /// </summary>
        public double[] Sums
        {
            get { return sums; }
        }

        /// <summary>
        /// Gets an array containing the skewness for of each column of data.
        /// </summary>
        public double[] Skewness
        {
            get { return skewness; }
        }

        /// <summary>
        /// Gets an array containing the kurtosis for of each column of data.
        /// </summary>
        public double[] Kurtosis
        {
            get { return kurtosis; }
        }

        /// <summary>
        ///   Gets the number of samples (or observations) in the data.
        /// </summary>
        public int Samples
        {
            get { return samples; }
        }

        /// <summary>
        ///   Gets the number of variables (or features) in the data.
        /// </summary>
        public int Variables
        {
            get { return variables; }
        }

        /// <summary>
        /// Gets a collection of DescriptiveMeasures objects that can be bound to a DataGridView.
        /// </summary>
        public DescriptiveMeasureCollection Measures
        {
            get { return measuresCollection; }
        }
        #endregion


    }

    public class DescriptiveMeasures
    {

        private DescriptiveAnalysis analysis;
        private int index;

        internal DescriptiveMeasures(DescriptiveAnalysis analysis, int index)
        {
            this.analysis = analysis;
            this.index = index;
        }

        /// <summary>
        ///   Gets the variable's index.
        /// </summary>
        public int Index
        {
            get { return index; }
        }

        /// <summary>
        ///   Gets the variable's name
        /// </summary>
        public string Name
        {
            get { return analysis.ColumnNames[index]; }
        }

        /// <summary>
        ///   Gets the variable's mean.
        /// </summary>
        public double Mean
        {
            get { return analysis.Means[index]; }
        }

        /// <summary>
        ///   Gets the variable's standard deviation.
        /// </summary>
        public double StandardDeviation
        {
            get { return analysis.StandardDeviations[index]; }
        }

        /// <summary>
        ///   Gets the variable's median.
        /// </summary>
        public double Median
        {
            get { return analysis.Medians[index]; }
        }

        /// <summary>
        ///   Gets the variable's mode.
        /// </summary>
        public double Mode
        {
            get { return analysis.Modes[index]; }
        }

        /// <summary>
        ///   Gets the variable's variance.
        /// </summary>
        public double Variance
        {
            get { return analysis.Variances[index]; }
        }

        /// <summary>
        ///   Gets the variable's skewness.
        /// </summary>
        public double Skewness
        {
            get { return analysis.Skewness[index]; }
        }

        /// <summary>
        ///   Gets the variable's kurtosis.
        /// </summary>
        public double Kurtosis
        {
            get { return analysis.Kurtosis[index]; }
        }

        /// <summary>
        ///   Gets the variable's standard error of the mean.
        /// </summary>
        public double StandardError
        {
            get { return analysis.StandardErrors[index]; }
        }

        /// <summary>
        ///   Gets the variable's maximum value.
        /// </summary>
        public double Max
        {
            get { return analysis.Ranges[index].Max; }
        }

        /// <summary>
        ///   Gets the variable's minimum value.
        /// </summary>
        public double Min
        {
            get { return analysis.Ranges[index].Min; }
        }

        /// <summary>
        ///   Gets the variable's length.
        /// </summary>
        public double Length
        {
            get { return analysis.Ranges[index].Length; }
        }

        /// <summary>
        ///   Gets the variable's observations.
        /// </summary>
        public double[] Samples
        {
            get
            {
                return analysis.Source.GetColumn(index);
            }
        }

    }


    public class DescriptiveMeasureCollection : System.Collections.ObjectModel.ReadOnlyCollection<DescriptiveMeasures>
    {
        internal DescriptiveMeasureCollection(DescriptiveMeasures[] components)
            : base(components)
        {

        }
    }

}
