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
    ///   Polynomial Kernel
    /// </summary>
    public class Polynomial : IKernel
    {
        private int degree;
        private double constant;

        /// <summary>
        ///   Constructs a new Polynomial kernel of a given degree.
        /// </summary>
        /// <param name="degree">The polynomial degree for this kernel.</param>
        /// <param name="constant">The polynomial constant for this kernel.</param>
        public Polynomial(int degree, double constant)
        {
            this.degree = degree;
            this.constant = constant;
        }

        /// <summary>
        ///   Constructs a new Polynomial kernel of a given degree.
        /// </summary>
        /// <param name="degree">The polynomial degree for this kernel.</param>
        public Polynomial(int degree)
            : this(degree, 0.0)
        {
        }

        /// <summary>
        ///   Gets the kernel's polynomial degree.
        /// </summary>
        public int Degree
        {
            get { return degree; }
        }

        /// <summary>
        ///   Gets the kernel's polynomial constant term.
        /// </summary>
        public double Constant
        {
            get { return constant; }
        }


        /// <summary>
        ///   Polynomial kernel function.
        /// </summary>
        /// <param name="x">Vector x in input space.</param>
        /// <param name="y">Vector y in input space.</param>
        /// <returns>Dot product in feature (kernel) space.</returns>
        public double Kernel(double[] x, double[] y)
        {
            double product = 0.0;
            for (int i = 0; i < x.Length; i++)
                product += x[i] * y[i];
            
            return System.Math.Pow(product+constant, degree);
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
            double q = 1.0 / degree;

            return System.Math.Pow(Kernel(x, x), q) + System.Math.Pow(Kernel(y, y), q)
                - 2.0 * System.Math.Pow(Kernel(x, y), q);
        }
    }
}
