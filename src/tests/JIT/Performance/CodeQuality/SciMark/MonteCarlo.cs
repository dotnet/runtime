// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
/// <license>
/// This is a port of the SciMark2a Java Benchmark to C# by
/// Chris Re (cmr28@cornell.edu) and Werner Vogels (vogels@cs.cornell.edu)
/// 
/// For details on the original authors see http://math.nist.gov/scimark2
/// 
/// This software is likely to burn your processor, bitflip your memory chips
/// anihilate your screen and corrupt all your disks, so you it at your
/// own risk.
/// </license>


using System;

namespace SciMark2
{
    /// <summary>Estimate Pi by approximating the area of a circle.
    /// How: generate N random numbers in the unit square, (0,0) to (1,1)
    /// and see how are within a radius of 1 or less, i.e.
    /// <pre>  
    /// sqrt(x^2 + y^2) < r
    /// </pre>
    /// since the radius is 1.0, we can square both sides
    /// and avoid a sqrt() computation:
    /// <pre>
    /// x^2 + y^2 <= 1.0
    /// </pre>
    /// this area under the curve is (Pi * r^2)/ 4.0,
    /// and the area of the unit of square is 1.0,
    /// so Pi can be approximated by 
    /// <pre>
    /// # points with x^2+y^2 < 1
    /// Pi =~ 		--------------------------  * 4.0
    /// total # points
    /// </pre>
    /// </summary>

    public class MonteCarlo
    {
        internal const int SEED = 113;

        public static double num_flops(int Num_samples)
        {
            // 3 flops in x^2+y^2 and 1 flop in random routine

            return ((double)Num_samples) * 4.0;
        }



        public static double integrate(int Num_samples)
        {
            SciMark2.Random R = new SciMark2.Random(SEED);


            int under_curve = 0;
            for (int count = 0; count < Num_samples; count++)
            {
                double x = R.nextDouble();
                double y = R.nextDouble();

                if (x * x + y * y <= 1.0)
                    under_curve++;
            }

            return ((double)under_curve / Num_samples) * 4.0;
        }
    }
}
