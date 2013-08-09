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
using Accord.Statistics.Kernels;
using Accord.Math.Decompositions;

namespace Accord.Statistics.Analysis
{

    /// <summary>
    ///   Kernel Principal Component Analysis
    /// </summary>
    /// <remarks>
    ///   Kernel principal component analysis (kernel PCA) is an extension of principal
    ///   component analysis (PCA) using techniques of kernel methods. Using a kernel,
    ///   the originally linear operations of PCA are done in a reproducing kernel Hilbert
    ///   space with a non-linear mapping.
    ///   
    ///   References:
    ///    - http://www.heikohoffmann.de/htmlthesis/node37.html
    ///    - http://www.heikohoffmann.de/htmlthesis/node137.html#sec_speedup
    ///    - http://www.hpl.hp.com/conferences/icml2003/papers/345.pdf
    ///    - http://www.cse.ust.hk/~jamesk/papers/icml03_slides.pdf
    /// </remarks>
    public class KernelPrincipalComponentAnalysis : PrincipalComponentAnalysis
    {

        private IKernel kernel;
        private double[,] sourceCentered;
        private bool centerFeatureSpace;


        //---------------------------------------------


        #region Constructor
        /// <summary>Constructs the Kernel Principal Component Analysis.</summary>
        /// <param name="data">The source data to perform analysis.</param>
        /// <param name="kernel">The kernel to be used in the analysis.</param>
        /// <param name="method">The analysis method to perform.</param>
        public KernelPrincipalComponentAnalysis(double[,] data, IKernel kernel, AnalysisMethod method)
            : base(data, method)
        {
            this.kernel = kernel;
            this.centerFeatureSpace = true;
        }

        /// <summary>Constructs the Kernel Principal Component Analysis.</summary>
        /// <param name="data">The source data to perform analysis.</param>
        /// <param name="kernel">The kernel to be used in the analysis.</param>
        public KernelPrincipalComponentAnalysis(double[,] data, IKernel kernel)
            : this(data, kernel, AnalysisMethod.Covariance)
        {
        }
        #endregion


        //---------------------------------------------


        #region Public Properties
        /// <summary>
        ///   Gets the Kernel used in the analysis.
        /// </summary>
        public IKernel Kernel
        {
            get { return kernel; }
        }

        /// <summary>
        ///   Gets or sets whether the points should be centured in feature space.
        /// </summary>
        public bool Center
        {
            get { return centerFeatureSpace; }
            set { centerFeatureSpace = value; }
        }
        #endregion


        //---------------------------------------------


        #region Public Methods
        /// <summary>Computes the Kernel Principal Component Analysis algorithm.</summary>
        public override void Compute()
        {
            int rows = Source.GetLength(0);
            int cols = Source.GetLength(1);

            // Center (adjust) the source matrix
            sourceCentered = adjust(Source);


            // Create the Gram (Kernel) Matrix
            double[,] K = new double[rows, rows];
            for (int i = 0; i < rows; i++)
            {
                for (int j = i; j < rows; j++)
                {
                    double k = kernel.Kernel(sourceCentered.GetRow(i), sourceCentered.GetRow(j));
                    K[i, j] = k; // Kernel matrix is symmetric
                    K[j, i] = k;
                }
            }


            // Center the Gram (Kernel) Matrix
            if (centerFeatureSpace)
                K = centerKernel(K);


            // Perform the Eigen Value Decomposition (EVD) of the Kernel matrix
            EigenValueDecomposition evd = new EigenValueDecomposition(K);

            // Gets the eigenvalues and corresponding eigenvectors
            double[] evals = evd.RealEigenValues;
            double[,] eigs = evd.EigenVectors;

            // Sort eigen values and vectors in ascending order
            int[] indices = new int[rows];
            for (int i = 0; i < rows; i++) indices[i] = i;
            Array.Sort(evals, indices, new AbsoluteComparer(true));
            eigs = eigs.Submatrix(0, rows - 1, indices);


            // Normalize eigenvectors
            if (centerFeatureSpace)
            {
                for (int j = 0; j < rows; j++)
                {
                    double eig = System.Math.Sqrt(System.Math.Abs(evals[j]));
                    for (int i = 0; i < rows; i++)
                        eigs[i, j] = eigs[i, j] / eig;
                }
            }


            // Set analysis properties
            this.SingularValues = new double[rows];
            this.EigenValues = evals;
            this.ComponentMatrix = eigs;


            // Project the original data into principal component space
            double[,] result = new double[rows, rows];
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < rows; j++)
                    for (int k = 0; k < rows; k++)
                        result[i, j] += K[i, k] * eigs[k, j];

            this.Result = result;


            // Computes additional information about the analysis and creates the
            //  object-oriented structure to hold the principal components found.
            createComponents();
        }



        /// <summary>Projects a given matrix into the principal component space.</summary>
        /// <param name="data">The matrix to be projected. The matrix should contain
        /// variables as columns and observations of each variable as rows.</param>
        public override double[,] Transform(double[,] data, int components)
        {
            int rows = data.GetLength(0);
            int cols = data.GetLength(1);
            int N = sourceCentered.GetLength(0);

            // Center the data
            data = adjust(data);

            // Create the Kernel matrix
            double[,] K = new double[rows, N];
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < N; j++)
                    K[i, j] = kernel.Kernel(data.GetRow(i), sourceCentered.GetRow(j));

            // Center the Gram (Kernel) Matrix
            if (centerFeatureSpace)
                K = centerKernel(K);

            // Project into the kernel principal components
            double[,] result = new double[rows, components];
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < components; j++)
                    for (int k = 0; k < rows; k++)
                        result[i, j] += K[i, k] * ComponentMatrix[k, j];

            return result;
        }

        /// <summary>
        ///   Reverts a set of projected data into it's original form. Complete reverse
        ///   transformation is not always possible and is not even guaranteed to exist.
        /// </summary>
        /// <remarks>
        ///   This method works using a closed-form MDS approach as suggested by
        ///   Kwok and Tsang. It is currently a direct implementation of the algorithm
        ///   without any kind of optimization.
        ///   
        ///   Reference:
        ///   - http://cmp.felk.cvut.cz/cmp/software/stprtool/manual/kernels/preimage/list/rbfpreimg3.html
        /// </remarks>
        /// <param name="data">The kpca-transformed data.</param>
        public override double[,] Revert(double[,] data)
        {
            return Revert(data, 10);
        }

        /// <summary>
        ///   Reverts a set of projected data into it's original form. Complete reverse
        ///   transformation is not always possible and is not even guaranteed to exist.
        /// </summary>
        /// <remarks>
        ///   This method works using a closed-form MDS approach as suggested by
        ///   Kwok and Tsang. It is currently a direct implementation of the algorithm
        ///   without any kind of optimization.
        ///   
        ///   Reference:
        ///   - http://cmp.felk.cvut.cz/cmp/software/stprtool/manual/kernels/preimage/list/rbfpreimg3.html
        /// </remarks>
        /// <param name="data">The kpca-transformed data.</param>
        /// <param name="neighbors">The number of nearest neighbors to use while constructing the pre-image.</param>
        public double[,] Revert(double[,] data, int neighbors)
        {
            int rows = data.GetLength(0);
            int cols = data.GetLength(1);

            double[,] reversion = new double[rows, sourceCentered.GetLength(1)];

            // number of neighbors cannot exceed the number of training vectors.
            int nn = System.Math.Min(neighbors, sourceCentered.GetLength(0));


            // For each point to be reversed
            for (int p = 0; p < rows; p++)
            {
                // 1. Get the point in feature space
                double[] y = data.GetRow(p);

                // 2. Select nn nearest neighbors of the feature space
                double[,] X = sourceCentered;
                double[] d2 = new double[Result.GetLength(0)];
                int[] inx = new int[Result.GetLength(0)];

                // 2.1 Calculate distances
                for (int i = 0; i < X.GetLength(0); i++)
                {
                    inx[i] = i;
                    d2[i] = kernel.Distance(y, Result.GetRow(i).Submatrix(y.Length));
                }

                // 2.2 Order them
                Array.Sort(d2, inx);

                // 2.3 Select nn neighbors
                inx = inx.Submatrix(nn);
                X = X.Submatrix(inx).Transpose(); // X is in input space
                d2 = d2.Submatrix(nn);       // distances in input space


                // 3. Create centering matrix
                // H = eye(nn, nn) - 1 / nn * ones(nn, nn);
                double[,] H = Matrix.Identity(nn).Subtract(Matrix.Create(nn, 1.0 / nn));


                // 4. Perform SVD
                //    [U,L,V] = svd(X*H);
                SingularValueDecomposition svd = new SingularValueDecomposition(X.Multiply(H));
                double[,] U = svd.LeftSingularVectors;
                double[,] L = Matrix.Diagonal(nn, svd.Diagonal);
                double[,] V = svd.RightSingularVectors;


                // 5. Compute projections
                //    Z = L*V';
                double[,] Z = L.Multiply(V.Transpose());


                // 6. Calculate distances
                //    d02 = sum(Z.^2)';
                double[] d02 = Matrix.Sum(Matrix.DotPow(Z,2));


                // 7. Get the pre-image using z = -0.5*inv(Z')*(d2-d02)
                double[,] inv = Matrix.PseudoInverse(Z.Transpose());
                double[] z = (-0.5).Multiply(inv).Multiply(d2.Subtract(d02));


                // 8. Project the pre-image on the original basis
                //    using x = U*z + sum(X,2)/nn;
                double[] x = (U.Multiply(z)).Add(Matrix.Sum(X.Transpose()).Multiply(1.0 / nn));


                // 9. Store the computed pre-image.
                for (int i = 0; i < reversion.GetLength(1); i++)
                    reversion[p, i] = x[i];
            }



            // if the data has been standardized or centered,
            //  we need to revert those operations as well
            if (this.Method == AnalysisMethod.Correlation)
            {
                // multiply by standard deviation and add the mean
                for (int i = 0; i < reversion.GetLength(0); i++)
                    for (int j = 0; j < reversion.GetLength(1); j++)
                        reversion[i, j] = (reversion[i, j] * StandardDeviations[j]) + Means[j];
            }
            else
            {
                // only add the mean
                for (int i = 0; i < reversion.GetLength(0); i++)
                    for (int j = 0; j < reversion.GetLength(1); j++)
                        reversion[i, j] = reversion[i, j] + Means[j];
            }


            return reversion;
        }

        #endregion


        //---------------------------------------------


        #region Private Methods
        private double[,] centerKernel(double[,] K)
        {
            int rows = K.GetLength(0);

            // All methods yelds similar results despite some sign changes.

            // K = K - 1K - K1 + 1K1
            //  double[,] U = Matrix.Create(rows, 1.0 / rows);
            //  double[,] A = U.Multiply(K);
            //  double[,] B = K.Multiply(U);
            //  double[,] C = U.Multiply(K).Multiply(U);
            //  K = K.Subtract(A).Subtract(B).Add(C);


            // K = (I - (1/N))*K*(I - (1/N)) 
            // double[,] Z = Matrix.Identity(rows).Subtract(Matrix.Create(rows, 1.0 / rows));
            // K = Z.Multiply(K.Multiply(Z));


            // K = K - mean(K) - mean(K') + mean(mean(K))
            double[] M = Tools.Mean(K);
            double MM = Tools.Mean(M);
            for (int i = 0; i < rows; i++)
            {
                for (int j = i; j < rows; j++)
                {
                    double k = K[i, j] - M[i] - M[j] + MM;
                    K[i, j] = k; // K is symmetric
                    K[j, i] = k;
                }
            }


            return K;
        }
        #endregion


    }

}
