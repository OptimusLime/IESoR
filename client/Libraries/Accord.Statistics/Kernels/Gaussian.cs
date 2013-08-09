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

namespace Accord.Statistics.Kernels
{
    /// <summary>
    ///   Gaussian Kernel
    /// </summary>
    /// <remarks>
    ///    This kernel requires tuning for the proper value of σ. Manual tuning or brute
    ///    force search are alternative approaches. An brute force technique could involve
    ///    stepping through a range of values for σ, perhaps in a gradient ascent optimization,
    ///    seeking optimal performance of a model with training data. Regardless of
    ///    the method utilized to find a proper value for σ, this type of model validation is
    ///    common and necessary when using the gaussian kernel. Although this approach
    ///    is feasible with supervised learning, it is much more difficult to tune σ for unsupervised
    ///    learning methods.
    ///    
    ///    References:
    ///     - http://people.revoledu.com/kardi/tutorial/Regression/KernelRegression/Kernel.htm
    /// </remarks>
    public class Gaussian : IKernel
    {
        private double sigma;

        /// <summary>
        ///   Constructs a new Gaussian Kernel
        /// </summary>
        /// <param name="sigma">The standard deviation for the Gaussian distribution.</param>
        public Gaussian(double sigma)
        {
            this.sigma = sigma;
        }

        /// <summary>
        ///   Gets or sets the sigma value for the kernel.
        /// </summary>
        public double Sigma
        {
            get { return sigma; }
            set { sigma = value; }
        }

        /// <summary>
        ///   Gaussian Kernel function.
        /// </summary>
        /// <param name="x">Vector x in input space.</param>
        /// <param name="y">Vector y in input space.</param>
        /// <returns>Dot product in feature (kernel) space.</returns>
        public double Kernel(double[] x, double[] y)
        {
            double norm = 0.0;
            for (int i = 0; i < x.Length; i++)
            {
                double d = x[i] - y[i];
                norm += d * d;
            }

            double beta = 2.0 * sigma * sigma;
            return System.Math.Exp(-norm / beta);
        }
        
        /// <summary>
        ///   Computes the distance in input space
        ///   between two points given in feature space.
        /// </summary>
        /// <param name="x">Vector x in feature (kernel) space.</param>
        /// <param name="y">Vector y in feature (kernel) space.</param>
        /// <returns>Distance between x and y in input space.</returns>
        public double Distance(double[] x, double[] y)
        {
            // Reference: Bakir Gokhan, pp 51
            // df = K(x,x) + K(y,y) - 2*K(x,y) = 1 + 1 - 2*K(x,y) [for rbf kernels]

            double df = 2.0 - 2.0 * Kernel(x, y); 

            double beta = 2.0 * sigma * sigma;
            double dz = -beta * System.Math.Log(1.0 - 0.5*df);
            
            return dz;
        }

    }
}
