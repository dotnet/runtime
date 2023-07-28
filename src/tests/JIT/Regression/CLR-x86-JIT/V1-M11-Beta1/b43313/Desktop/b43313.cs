// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
namespace Test
{
    using System;

    class OtherException : Exception
    {
    }

    public struct AA
    {
        private static float[] s_afStatic1;

        private static void Main1()
        {
            ulong[] param2 = new ulong[10];
            uint[] local5 = new uint[7];
            try
            {
                try
                {
                    int[] local8 = new int[7];
                    try
                    {
                        //.......
                    }
                    catch (IndexOutOfRangeException)
                    {
                        do
                        {
                            //.......
                        } while (s_afStatic1[233] > 0.0);
                    }
                    while (0 != local5[205])
                        return;
                }
                catch (Exception)
                {
                    float[] local10 = new float[7];
                    while ((int)param2[168] != 1)
                    {
                        float[] local11 = new float[7];
                    }
                }
            }
            catch (OtherException) { }
        }
        [Fact]
        public static int TestEntryPoint()
        {
            try
            {
                Main1();
                return -1;
            }
            catch (IndexOutOfRangeException)
            {
                return 100;
            }
        }
    }
}
