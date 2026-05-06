// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

namespace MathRoundSingleTest
{
    public class Program
    {
        public const int Pass = 100;
        public const int Fail = 0;

        public const float constantValue = 0.0f;

        public static float staticValue = 1.1f;

        public static float[] staticValueArray = new float[]
        {
            2.2f,
            3.3f,
            4.4f
        };

        public float instanceValue = 5.5f;

        public float[] instanceValueArray = new float[]
        {
            6.6f,
            7.7f,
            8.8f
        };

        [Fact]
        public unsafe static int TestEntryPoint()
        {
            float localValue = 9.9f;

            var program = new Program();

            if (MathF.Round(constantValue) != 0.0f)
            {
                Console.WriteLine("MathF.Round of a constant value failed");
                return Fail;
            }

            if (MathF.Round(staticValue) != 1.0f)
            {
                Console.WriteLine("MathF.Round of a static value failed");
                return Fail;
            }

            fixed (float* pStaticValue = &staticValue)
            {
                if (MathF.Round(*pStaticValue) != 1.0f)
                {
                    Console.WriteLine("MathF.Round of an addressed static value failed");
                    return Fail;
                }
            }

            if (MathF.Round(staticValueArray[0]) != 2.0f)
            {
                Console.WriteLine("MathF.Round of a static value array (index 0) failed");
                return Fail;
            }

            if (MathF.Round(staticValueArray[1]) != 3.0f)
            {
                Console.WriteLine("MathF.Round of a static value array (index 1) failed");
                return Fail;
            }

            if (MathF.Round(staticValueArray[2]) != 4.0f)
            {
                Console.WriteLine("MathF.Round of a static value array (index 2) failed");
                return Fail;
            }

            fixed (float* pStaticValueArray = &staticValueArray[0])
            {
                if (MathF.Round(pStaticValueArray[0]) != 2.0f)
                {
                    Console.WriteLine("MathF.Round of a addressed static value array (index 0) failed");
                    return Fail;
                }

                if (MathF.Round(pStaticValueArray[1]) != 3.0f)
                {
                    Console.WriteLine("MathF.Round of a addressed static value array (index 1) failed");
                    return Fail;
                }

                if (MathF.Round(pStaticValueArray[2]) != 4.0f)
                {
                    Console.WriteLine("MathF.Round of a addressed static value array (index 2) failed");
                    return Fail;
                }
            }

            if (MathF.Round(program.instanceValue) != 6.0f)
            {
                Console.WriteLine("MathF.Round of an instance value failed");
                return Fail;
            }

            fixed (float* pInstanceValue = &program.instanceValue)
            {
                if (MathF.Round(*pInstanceValue) != 6.0f)
                {
                    Console.WriteLine("MathF.Round of an addressed instance value failed");
                    return Fail;
                }
            }

            if (MathF.Round(program.instanceValueArray[0]) != 7.0f)
            {
                Console.WriteLine("MathF.Round of an instance value array (index 0) failed");
                return Fail;
            }

            if (MathF.Round(program.instanceValueArray[1]) != 8.0f)
            {
                Console.WriteLine("MathF.Round of an instance value array (index 1) failed");
                return Fail;
            }

            if (MathF.Round(program.instanceValueArray[2]) != 9.0f)
            {
                Console.WriteLine("MathF.Round of an instance value array (index 2) failed");
                return Fail;
            }

            fixed (float* pInstanceValueArray = &program.instanceValueArray[0])
            {
                if (MathF.Round(pInstanceValueArray[0]) != 7.0f)
                {
                    Console.WriteLine("MathF.Round of a addressed instance value array (index 0) failed");
                    return Fail;
                }

                if (MathF.Round(pInstanceValueArray[1]) != 8.0f)
                {
                    Console.WriteLine("MathF.Round of a addressed instance value array (index 1) failed");
                    return Fail;
                }

                if (MathF.Round(pInstanceValueArray[2]) != 9.0f)
                {
                    Console.WriteLine("MathF.Round of a addressed instance value array (index 2) failed");
                    return Fail;
                }
            }

            if (MathF.Round(localValue) != 10.0f)
            {
                Console.WriteLine("MathF.Round of a local value failed");
                return Fail;
            }

            float* pLocalValue = &localValue;

            if (MathF.Round(*pLocalValue) != 10.0f)
            {
                Console.WriteLine("MathF.Round of an addressed local value failed");
                return Fail;
            }

            return Pass;
        }
    }
}
