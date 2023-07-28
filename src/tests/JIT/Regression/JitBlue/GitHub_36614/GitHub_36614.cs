// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

namespace projs
{
    public class Program
    {
        [Fact]
        public static int TestEntryPoint()
        {
            Program p = new Program();
            p.NarrowDouble();
            Console.WriteLine("Passed.");
            return 100;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal void NarrowDouble()
        {
            // GenerateSource1 and GenerateSource2 methods are needed to exercise the bug code path.
            double[] source1 = GenerateSource1();
            double[] source2 = GenerateSource2();
            Vector<double> sourceVec1 = new Vector<double>(source1);
            Vector<double> sourceVec2 = new Vector<double>(source2);
            Vector<float> dest = Vector.Narrow(sourceVec1, sourceVec2);

            for (int i = 0; i < Vector<double>.Count; i++)
            {
                Assert.Equal(unchecked((float)source1[i]), dest[i]);
            }
            for (int i = 0; i < Vector<double>.Count; i++)
            {
                // The test represents a problem on x86 when double-alignment causes access to stack local.
                // At that time, the stack level was not included in the offset while trying to fetch the element
                // of dest present on stack.
                Assert.Equal(unchecked((float)source2[i]), dest[i + Vector<double>.Count]);
            }
        }

        internal static double[] GenerateSource1()
        {
            return new double[4] { 5.1, 5.2, 5.3, 5.4 };
        }
        internal static double[] GenerateSource2()
        {
            return new double[4] { 6.1, 6.2, 6.3, 6.4 };
        }
    }
}
