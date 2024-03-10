// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

namespace MathFloorSingleTest
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

            if (MathF.Floor(constantValue) != 0.0f)
            {
                Console.WriteLine("MathF.Floor of a constant value failed");
                return Fail;
            }

            if (MathF.Floor(staticValue) != 1.0f)
            {
                Console.WriteLine("MathF.Floor of a static value failed");
                return Fail;
            }

            fixed (float* pStaticValue = &staticValue)
            {
                if (MathF.Floor(*pStaticValue) != 1.0f)
                {
                    Console.WriteLine("MathF.Floor of an addressed static value failed");
                    return Fail;
                }
            }

            if (MathF.Floor(staticValueArray[0]) != 2.0f)
            {
                Console.WriteLine("MathF.Floor of a static value array (index 0) failed");
                return Fail;
            }

            if (MathF.Floor(staticValueArray[1]) != 3.0f)
            {
                Console.WriteLine("MathF.Floor of a static value array (index 1) failed");
                return Fail;
            }

            if (MathF.Floor(staticValueArray[2]) != 4.0f)
            {
                Console.WriteLine("MathF.Floor of a static value array (index 2) failed");
                return Fail;
            }

            fixed (float* pStaticValueArray = &staticValueArray[0])
            {
                if (MathF.Floor(pStaticValueArray[0]) != 2.0f)
                {
                    Console.WriteLine("MathF.Floor of a addressed static value array (index 0) failed");
                    return Fail;
                }

                if (MathF.Floor(pStaticValueArray[1]) != 3.0f)
                {
                    Console.WriteLine("MathF.Floor of a addressed static value array (index 1) failed");
                    return Fail;
                }

                if (MathF.Floor(pStaticValueArray[2]) != 4.0f)
                {
                    Console.WriteLine("MathF.Floor of a addressed static value array (index 2) failed");
                    return Fail;
                }
            }

            if (MathF.Floor(program.instanceValue) != 5.0f)
            {
                Console.WriteLine("MathF.Floor of an instance value failed");
                return Fail;
            }

            fixed (float* pInstanceValue = &program.instanceValue)
            {
                if (MathF.Floor(*pInstanceValue) != 5.0f)
                {
                    Console.WriteLine("MathF.Floor of an addressed instance value failed");
                    return Fail;
                }
            }

            if (MathF.Floor(program.instanceValueArray[0]) != 6.0f)
            {
                Console.WriteLine("MathF.Floor of an instance value array (index 0) failed");
                return Fail;
            }

            if (MathF.Floor(program.instanceValueArray[1]) != 7.0f)
            {
                Console.WriteLine("MathF.Floor of an instance value array (index 1) failed");
                return Fail;
            }

            if (MathF.Floor(program.instanceValueArray[2]) != 8.0f)
            {
                Console.WriteLine("MathF.Floor of an instance value array (index 2) failed");
                return Fail;
            }

            fixed (float* pInstanceValueArray = &program.instanceValueArray[0])
            {
                if (MathF.Floor(pInstanceValueArray[0]) != 6.0f)
                {
                    Console.WriteLine("MathF.Floor of a addressed instance value array (index 0) failed");
                    return Fail;
                }

                if (MathF.Floor(pInstanceValueArray[1]) != 7.0f)
                {
                    Console.WriteLine("MathF.Floor of a addressed instance value array (index 1) failed");
                    return Fail;
                }

                if (MathF.Floor(pInstanceValueArray[2]) != 8.0f)
                {
                    Console.WriteLine("MathF.Floor of a addressed instance value array (index 2) failed");
                    return Fail;
                }
            }

            if (MathF.Floor(localValue) != 9.0f)
            {
                Console.WriteLine("MathF.Floor of a local value failed");
                return Fail;
            }

            float* pLocalValue = &localValue;

            if (MathF.Floor(*pLocalValue) != 9.0f)
            {
                Console.WriteLine("MathF.Floor of an addressed local value failed");
                return Fail;
            }

            return Pass;
        }
    }
}
