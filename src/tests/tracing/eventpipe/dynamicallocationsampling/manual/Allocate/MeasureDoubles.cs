using System;
using System.Collections.Generic;
using System.Linq;

namespace Allocate
{
    public class MeasureDoubles
    {
        const int Iterations = 200_000;

        public void Allocate()
        {
            List<double[]> arrays = new List<double[]>(Iterations);

            for (int i = 0; i < Iterations; i++)
            {
                arrays.Add(new double[1] { i });
            }

            Console.WriteLine($"Sum {arrays.Count} arrays of one double = {arrays.Sum(doubles => doubles[0])}");
        }
    }
}
