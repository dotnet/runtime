// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//This is a positive stress test.  All the rngchk should be removed

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace SimpleArray_01
{
    public delegate void RngTest();
    public class Class1
    {
        [Fact]
        public static int TestEntryPoint()
        {
            int retVal = 100;

            int[] array = new int[3];
            for (int index1 = 0; index1 < array.Length; index1++)
            {
                for (int index2 = 0; index2 < array.Length; index2++)
                {
                    for (int index3 = 0; index3 < array.Length; index3++)
                    {
                        for (int index4 = 0; index4 < array.Length; index4++)
                        {
                            for (int index5 = 0; index5 < array.Length; index5++)
                            {
                                for (int index6 = 0; index6 < array.Length; index6++)
                                {
                                    for (int index7 = 0; index7 < array.Length; index7++)
                                    {
                                        for (int index8 = 0; index8 < array.Length; index8++)
                                        {
                                            for (int index9 = 0; index9 < array.Length; index9++)
                                            {
                                                for (int index10 = 0; index10 < array.Length; index10++)
                                                {
                                                    for (int index11 = 0; index11 < array.Length; index11++)
                                                    {
                                                        for (int index12 = 0; index12 < array.Length; index12++)
                                                        {
                                                            for (int index13 = 0; index13 < array.Length; index13++)
                                                            {
                                                                for (int index14 = 0; index14 < array.Length; index14++)
                                                                {
                                                                    for (int index15 = 0; index15 < array.Length; index15++)
                                                                    {
                                                                        for (int index16 = 0; index16 < array.Length; index16++)
                                                                        {
                                                                            for (int index17 = 0; index17 < array.Length; index17++)
                                                                            {
                                                                                for (int index18 = 0; index18 < array.Length; index18++)
                                                                                {
                                                                                    for (int index19 = 0; index19 < array.Length; index19++)
                                                                                    {
                                                                                        for (int index20 = 0; index20 < array.Length; index20++)
                                                                                        {
                                                                                            array[index1] = index1;
                                                                                            array[index2] = index2;
                                                                                            array[index3] = index3;
                                                                                            array[index4] = index4;
                                                                                            array[index5] = index5;
                                                                                            array[index6] = index6;
                                                                                            array[index7] = index7;
                                                                                            array[index8] = index8;
                                                                                            array[index9] = index9;
                                                                                            array[index10] = index10;
                                                                                            array[index11] = index11;
                                                                                            array[index12] = index12;
                                                                                            array[index13] = index13;
                                                                                            array[index14] = index14;
                                                                                            array[index15] = index15;
                                                                                            array[index16] = index16;
                                                                                            array[index17] = index17;
                                                                                            array[index18] = index18;
                                                                                            array[index19] = index19;
                                                                                            array[index20] = index20;
                                                                                        }
                                                                                    }
                                                                                }
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return retVal;
        }
    }
}
