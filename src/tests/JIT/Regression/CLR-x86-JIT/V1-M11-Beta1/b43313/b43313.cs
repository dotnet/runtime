// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace Test
{
    using System;

    public struct AA
    {
        static float[] m_afStatic1;

        static void Main1()
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
                    catch (Exception)
                    {
                        do
                        {
                            //.......
                        } while (m_afStatic1[233] > 0.0);
                    }
                    while (0 != local5[205])
                        return;
                }
                catch (IndexOutOfRangeException)
                {
                    float[] local10 = new float[7];
                    while ((int)param2[168] != 1)
                    {
                        float[] local11 = new float[7];
                    }
                }
            }
            catch (NullReferenceException) { }
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
