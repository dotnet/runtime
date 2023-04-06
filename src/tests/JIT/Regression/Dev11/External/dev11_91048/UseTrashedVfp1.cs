// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace UseTrashedVfp1
{
    public static class App
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static float SubtractStandardValueFrom(object untypedValue)
        {
            return ((Single)untypedValue - Helpers.TrashVFPAndGetStandardFloat32());
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool RunRepro()
        {
            float result;

            result = SubtractStandardValueFrom(Helpers.StandardFloatValue_x_3);

            return Helpers.IsWithin_0Point1(result, Helpers.StandardFloatValue_x_2);
        }


        [Fact]
        public static int TestEntryPoint()
        {
            bool fTestPassed;

            fTestPassed = App.RunRepro();

            if (fTestPassed)
            {
                Console.WriteLine("Test passed.");
                return 100;
            }
            else
            {
                Console.WriteLine("Test failed.");
            }

            return 101;
        }
    }


    public static class Helpers
    {
        private const float BaseFloatValue = 123.456f;

        public const float StandardFloatValue_x_1 = BaseFloatValue;
        public const float StandardFloatValue_x_2 = (2.0f * BaseFloatValue);
        public const float StandardFloatValue_x_3 = (3.0f * BaseFloatValue);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool IsWithin_0Point1(float valueToTest, float baselineValue)
        {
            float difference;

            difference = (valueToTest - baselineValue);

            if ((difference <= -0.1f) || (difference >= 0.1f))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float TrashVFPAndGetStandardFloat32()
        {
            TrashVolatileVFPRegisters();
            return StandardFloatValue_x_1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void TrashVolatileVFPRegistersHelper(double d0, double d1, double d2, double d3, double d4, double d5, double d6, double d7)
        {
            return;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void TrashVolatileVFPRegisters()
        {
            TrashVolatileVFPRegistersHelper(0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0);
            return;
        }
    }
}
