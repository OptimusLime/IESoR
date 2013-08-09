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

namespace Accord.Statistics.Kernels
{

    /// <summary>
    ///   Sigmoid kernel.
    /// </summary>
    /// <remarks>
    ///   Sigmoid kernels are not positive definite and therefore do not induce
    ///   a reproducing kernel Hilbert space. However, they have been successfully
    ///   used in practice (Scholkopf & Smola, 2002).
    /// </remarks>
    public class Sigmoid : IKernel
    {
        private double gamma;
        private double constant;

        /// <summary>
        ///   Constructs a Sigmoid kernel.
        /// </summary>
        /// <param name="alpha">Alpha parameter.</param>
        /// <param name="constant">Constant parameter.</param>
        public Sigmoid(double alpha, double constant)
        {
            this.gamma = alpha;
            this.constant = constant;
        }

        /// <summary>
        ///   Gets the kernel's gamma parameter.
        /// </summary>
        /// <remarks>
        ///   In a sigmoid kernel, gamma is a inner product
        ///   coefficient for the hyperbolic tangent function.
        /// </remarks>
        public double Gamma
        {
            get { return gamma; }
        }

        /// <summary>
        ///   Gets the kernel's constant term.
        /// </summary>
        public double Constant
        {
            get { return constant; }
        }

        /// <summary>
        ///   Sigmoid kernel function.
        /// </summary>
        /// <param name="x">Vector x in input space.</param>
        /// <param name="y">Vector y in input space.</param>
        /// <returns>Dot product in feature (kernel) space.</returns>
        public double Kernel(double[] x, double[] y)
        {
            double product = 0.0;
            for (int i = 0; i < x.Length; i++)
                product += x[i] * y[i];
            
            return System.Math.Tanh(gamma * product + constant);
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
            throw new NotImplementedException();
        }

    }
}
