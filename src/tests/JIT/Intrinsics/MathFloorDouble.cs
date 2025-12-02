// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

namespace MathFloorDoubleTest
{
    public class Program
    {
        public const int Pass = 100;
        public const int Fail = 0;

        public const double constantValue = 0.0;

        public static double staticValue = 1.1;

        public static double[] staticValueArray = new double[]
        {
            2.2,
            3.3,
            4.4
        };

        public double instanceValue = 5.5;

        public double[] instanceValueArray = new double[]
        {
            6.6,
            7.7,
            8.8
        };

        [Fact]
        public unsafe static int TestEntryPoint()
        {
            double localValue = 9.9;

            var program = new Program();

            if (Math.Floor(constantValue) != 0.0)
            {
                Console.WriteLine("Math.Floor of a constant value failed");
                return Fail;
            }

            if (Math.Floor(staticValue) != 1.0)
            {
                Console.WriteLine("Math.Floor of a static value failed");
                return Fail;
            }

            fixed (double* pStaticValue = &staticValue)
            {
                if (Math.Floor(*pStaticValue) != 1.0)
                {
                    Console.WriteLine("Math.Floor of an addressed static value failed");
                    return Fail;
                }
            }

            if (Math.Floor(staticValueArray[0]) != 2.0)
            {
                Console.WriteLine("Math.Floor of a static value array (index 0) failed");
                return Fail;
            }

            if (Math.Floor(staticValueArray[1]) != 3.0)
            {
                Console.WriteLine("Math.Floor of a static value array (index 1) failed");
                return Fail;
            }

            if (Math.Floor(staticValueArray[2]) != 4.0)
            {
                Console.WriteLine("Math.Floor of a static value array (index 2) failed");
                return Fail;
            }

            fixed (double* pStaticValueArray = &staticValueArray[0])
            {
                if (Math.Floor(pStaticValueArray[0]) != 2.0)
                {
                    Console.WriteLine("Math.Floor of a addressed static value array (index 0) failed");
                    return Fail;
                }

                if (Math.Floor(pStaticValueArray[1]) != 3.0)
                {
                    Console.WriteLine("Math.Floor of a addressed static value array (index 1) failed");
                    return Fail;
                }

                if (Math.Floor(pStaticValueArray[2]) != 4.0)
                {
                    Console.WriteLine("Math.Floor of a addressed static value array (index 2) failed");
                    return Fail;
                }
            }

            if (Math.Floor(program.instanceValue) != 5.0)
            {
                Console.WriteLine("Math.Floor of an instance value failed");
                return Fail;
            }

            fixed (double* pInstanceValue = &program.instanceValue)
            {
                if (Math.Floor(*pInstanceValue) != 5.0)
                {
                    Console.WriteLine("Math.Floor of an addressed instance value failed");
                    return Fail;
                }
            }

            if (Math.Floor(program.instanceValueArray[0]) != 6.0)
            {
                Console.WriteLine("Math.Floor of an instance value array (index 0) failed");
                return Fail;
            }

            if (Math.Floor(program.instanceValueArray[1]) != 7.0)
            {
                Console.WriteLine("Math.Floor of an instance value array (index 1) failed");
                return Fail;
            }

            if (Math.Floor(program.instanceValueArray[2]) != 8.0)
            {
                Console.WriteLine("Math.Floor of an instance value array (index 2) failed");
                return Fail;
            }

            fixed (double* pInstanceValueArray = &program.instanceValueArray[0])
            {
                if (Math.Floor(pInstanceValueArray[0]) != 6.0)
                {
                    Console.WriteLine("Math.Floor of a addressed instance value array (index 0) failed");
                    return Fail;
                }

                if (Math.Floor(pInstanceValueArray[1]) != 7.0)
                {
                    Console.WriteLine("Math.Floor of a addressed instance value array (index 1) failed");
                    return Fail;
                }

                if (Math.Floor(pInstanceValueArray[2]) != 8.0)
                {
                    Console.WriteLine("Math.Floor of a addressed instance value array (index 2) failed");
                    return Fail;
                }
            }

            if (Math.Floor(localValue) != 9.0)
            {
                Console.WriteLine("Math.Floor of a local value failed");
                return Fail;
            }

            double* pLocalValue = &localValue;

            if (Math.Floor(*pLocalValue) != 9.0)
            {
                Console.WriteLine("Math.Floor of an addressed local value failed");
                return Fail;
            }

            return Pass;
        }
    }
}
