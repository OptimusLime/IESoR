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
    ///   Linear Kernel
    /// </summary>
    public class Linear : Polynomial
    {

        /// <summary>
        ///   Constructs a new Linear kernel.
        /// </summary>
        /// <param name="constant"></param>
        public Linear(double constant) : base(1,constant)
        {
        }

        /// <summary>
        ///   Constructs a new Linear Kernel.
        /// </summary>
        public Linear()
            : base(1, 0)
        {
        }

    }
}
