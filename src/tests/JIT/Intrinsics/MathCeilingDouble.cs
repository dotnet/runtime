// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

namespace MathCeilingDoubleTest
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

            if (Math.Ceiling(constantValue) != 0.0)
            {
                Console.WriteLine("Math.Ceiling of a constant value failed");
                return Fail;
            }

            if (Math.Ceiling(staticValue) != 2.0)
            {
                Console.WriteLine("Math.Ceiling of a static value failed");
                return Fail;
            }

            fixed (double* pStaticValue = &staticValue)
            {
                if (Math.Ceiling(*pStaticValue) != 2.0)
                {
                    Console.WriteLine("Math.Ceiling of an addressed static value failed");
                    return Fail;
                }
            }

            if (Math.Ceiling(staticValueArray[0]) != 3.0)
            {
                Console.WriteLine("Math.Ceiling of a static value array (index 0) failed");
                return Fail;
            }

            if (Math.Ceiling(staticValueArray[1]) != 4.0)
            {
                Console.WriteLine("Math.Ceiling of a static value array (index 1) failed");
                return Fail;
            }

            if (Math.Ceiling(staticValueArray[2]) != 5.0)
            {
                Console.WriteLine("Math.Ceiling of a static value array (index 2) failed");
                return Fail;
            }

            fixed (double* pStaticValueArray = &staticValueArray[0])
            {
                if (Math.Ceiling(pStaticValueArray[0]) != 3.0)
                {
                    Console.WriteLine("Math.Ceiling of a addressed static value array (index 0) failed");
                    return Fail;
                }

                if (Math.Ceiling(pStaticValueArray[1]) != 4.0)
                {
                    Console.WriteLine("Math.Ceiling of a addressed static value array (index 1) failed");
                    return Fail;
                }

                if (Math.Ceiling(pStaticValueArray[2]) != 5.0)
                {
                    Console.WriteLine("Math.Ceiling of a addressed static value array (index 2) failed");
                    return Fail;
                }
            }

            if (Math.Ceiling(program.instanceValue) != 6.0)
            {
                Console.WriteLine("Math.Ceiling of an instance value failed");
                return Fail;
            }

            fixed (double* pInstanceValue = &program.instanceValue)
            {
                if (Math.Ceiling(*pInstanceValue) != 6.0)
                {
                    Console.WriteLine("Math.Ceiling of an addressed instance value failed");
                    return Fail;
                }
            }

            if (Math.Ceiling(program.instanceValueArray[0]) != 7.0)
            {
                Console.WriteLine("Math.Ceiling of an instance value array (index 0) failed");
                return Fail;
            }

            if (Math.Ceiling(program.instanceValueArray[1]) != 8.0)
            {
                Console.WriteLine("Math.Ceiling of an instance value array (index 1) failed");
                return Fail;
            }

            if (Math.Ceiling(program.instanceValueArray[2]) != 9.0)
            {
                Console.WriteLine("Math.Ceiling of an instance value array (index 2) failed");
                return Fail;
            }

            fixed (double* pInstanceValueArray = &program.instanceValueArray[0])
            {
                if (Math.Ceiling(pInstanceValueArray[0]) != 7.0)
                {
                    Console.WriteLine("Math.Ceiling of a addressed instance value array (index 0) failed");
                    return Fail;
                }

                if (Math.Ceiling(pInstanceValueArray[1]) != 8.0)
                {
                    Console.WriteLine("Math.Ceiling of a addressed instance value array (index 1) failed");
                    return Fail;
                }

                if (Math.Ceiling(pInstanceValueArray[2]) != 9.0)
                {
                    Console.WriteLine("Math.Ceiling of a addressed instance value array (index 2) failed");
                    return Fail;
                }
            }

            if (Math.Ceiling(localValue) != 10.0)
            {
                Console.WriteLine("Math.Ceiling of a local value failed");
                return Fail;
            }

            double* pLocalValue = &localValue;

            if (Math.Ceiling(*pLocalValue) != 10.0)
            {
                Console.WriteLine("Math.Ceiling of an addressed local value failed");
                return Fail;
            }

            return Pass;
        }
    }
}
