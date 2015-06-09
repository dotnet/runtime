// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Test
{
    using System;

    struct AA
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
        static int Main()
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
