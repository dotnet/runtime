// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Xunit;

namespace System.Reflection.Compatibility.UnitTests.ConstructorTests
{
    public class TestMultiDimensionalArray
    {
        [Fact]
        public void TestSZArrayConstructorInvoke()
        {
            Type type = Type.GetType("System.Object[]");
            ConstructorInfo[] cia = type.GetConstructors();
            Assert.Equal(1, cia.Length);

            ConstructorInfo ci = cia[0];
            object[] arr = null;
            int[] blength = new int[] { -100, -9, -1 };
            for (int j = 0; j < blength.Length; j++)
            {
                Assert.Throws<OverflowException>(() =>
               {
                   arr = (object[])ci.Invoke(new Object[] { blength[j] });
               });
            }

            int[] glength = new int[] { 0, 1, 2, 3, 5, 10, 99, 65535 };
            for (int j = 0; j < glength.Length; j++)
            {
                arr = (object[])ci.Invoke(new Object[] { glength[j] });
                Assert.Equal(0, arr.GetLowerBound(0));
                Assert.Equal(glength[j] - 1, arr.GetUpperBound(0));
                Assert.Equal(glength[j], arr.Length);
            }
        }
        [Fact]
        public void Test1DArrayConstructorInvoke()
        {
            Type type = Type.GetType("System.Char[*]");
            MethodInfo milb = type.GetMethod("GetLowerBound");
            MethodInfo miub = type.GetMethod("GetUpperBound");
            PropertyInfo pil = type.GetProperty("Length");

            ConstructorInfo[] cia = type.GetConstructors();
            Assert.Equal(2, cia.Length);

            for (int i = 0; i < cia.Length; i++)
            {
                char[] arr = null;
                switch (cia[i].GetParameters().Length)
                {
                    case 1:
                        {
                            int[] blength = new int[] { -100, -9, -1 };
                            for (int j = 0; j < blength.Length; j++)
                            {
                                Assert.Throws<OverflowException>(() =>
                                {
                                    arr = (char[])cia[i].Invoke(new Object[] { blength[j] });
                                });
                            }

                            int[] glength = new int[] { 0, 1, 2, 3, 5, 10, 99 };
                            for (int j = 0; j < glength.Length; j++)
                            {
                                arr = (char[])cia[i].Invoke(new Object[] { glength[j] });
                                Assert.Equal(0, arr.GetLowerBound(0));
                                Assert.Equal(glength[j] - 1, arr.GetUpperBound(0));
                                Assert.Equal(glength[j], arr.Length);
                            }
                        }
                        break;
                    case 2:
                        {
                            int[] b_lower = new int[] { -20, 0, 20 };
                            int[] blength = new int[] { -100, -9, -1 };
                            for (int j = 0; j < blength.Length; j++)
                            {
                                Assert.Throws<OverflowException>(() =>
                                {
                                    arr = (char[])cia[i].Invoke(new Object[] { b_lower[j], blength[j] });
                                });
                            }

                            int[] glower = new int[] { 0, 1, -1, 2, -3, 5, -10, 99, 100 };
                            int[] glength = new int[] { 0, 1, 3, 2, 3, 5, 10, 99, 0 };
                            for (int j = 0; j < glength.Length; j++)
                            {
                                object o = cia[i].Invoke(new Object[] { glower[j], glength[j] });

                                Assert.Equal(glower[j], (int)milb.Invoke(o, new object[] { 0 }));
                                Assert.Equal(glower[j] + glength[j] - 1, (int)miub.Invoke(o, new object[] { 0 }));
                                Assert.Equal(glength[j], (int)pil.GetValue(o, null));
                            }
                        }
                        break;
                }
            }
        }

        [Fact]
        public void Test2DArrayConstructorInvoke()
        {
            Type type = Type.GetType("System.Int32[,]", false);

            ConstructorInfo[] cia = type.GetConstructors();
            Assert.Equal(2, cia.Length);

            for (int i = 0; i < cia.Length; i++)
            {
                int[,] arr = null;
                switch (cia[i].GetParameters().Length)
                {
                    case 2:
                        {
                            int[] blength1 = new int[] { -11, -10, 0, 10 };
                            int[] blength2 = new int[] { -33, 0, -20, -33 };

                            for (int j = 0; j < blength1.Length; j++)
                            {
                                Assert.Throws<OverflowException>(() =>
                                {
                                    arr = (int[,])cia[i].Invoke(new Object[] { blength1[j], blength2[j] });
                                });
                            }


                            int[] glength1 = new int[] { 0, 0, 1, 1, 2, 1, 2, 10, 17, 99 };
                            int[] glength2 = new int[] { 0, 1, 0, 1, 1, 2, 2, 110, 5, 900 };

                            for (int j = 0; j < glength1.Length; j++)
                            {
                                arr = (int[,])cia[i].Invoke(new Object[] { glength1[j], glength2[j] });
                                Assert.Equal(0, arr.GetLowerBound(0));
                                Assert.Equal(glength1[j] - 1, arr.GetUpperBound(0));
                                Assert.Equal(0, arr.GetLowerBound(1));
                                Assert.Equal(glength2[j] - 1, arr.GetUpperBound(1));
                                Assert.Equal(glength1[j] * glength2[j], arr.Length);
                            }
                        }

                        break;

                    case 4:
                        {
                            int[] b_lower1 = new int[] { 10, -10, 20 };
                            int[] b_lower2 = new int[] { -10, 10, 0 };
                            int[] blength1 = new int[] { -11, -10, 0 };
                            int[] blength2 = new int[] { -33, 0, -20 };

                            for (int j = 0; j < blength1.Length; j++)
                            {
                                Assert.Throws<OverflowException>(() =>
                                {
                                    arr = (int[,])cia[i].Invoke(new Object[] { b_lower1[j], blength1[j], b_lower2[j], blength2[j] });
                                });
                            }

                            int baseNum = 3;
                            int baseNum4 = baseNum * baseNum * baseNum * baseNum;
                            int[] glower1 = new int[baseNum4];
                            int[] glower2 = new int[baseNum4];
                            int[] glength1 = new int[baseNum4];
                            int[] glength2 = new int[baseNum4];

                            int cnt = 0;
                            for (int pos1 = 0; pos1 < baseNum; pos1++)
                                for (int pos2 = 0; pos2 < baseNum; pos2++)
                                    for (int pos3 = 0; pos3 < baseNum; pos3++)
                                        for (int pos4 = 0; pos4 < baseNum; pos4++)
                                        {
                                            int saved = cnt;
                                            glower1[cnt] = saved % baseNum;
                                            saved = saved / baseNum;
                                            glength1[cnt] = saved % baseNum;
                                            saved = saved / baseNum;
                                            glower2[cnt] = saved % baseNum;
                                            saved = saved / baseNum;
                                            glength2[cnt] = saved % baseNum;
                                            cnt++;
                                        }

                            for (int j = 0; j < glength1.Length; j++)
                            {
                                arr = (int[,])cia[i].Invoke(new Object[] { glower1[j], glength1[j], glower2[j], glength2[j] });
                                Assert.Equal(glower1[j], arr.GetLowerBound(0));
                                Assert.Equal(glower1[j] + glength1[j] - 1, arr.GetUpperBound(0));
                                Assert.Equal(glower2[j], arr.GetLowerBound(1));
                                Assert.Equal(glower2[j] + glength2[j] - 1, arr.GetUpperBound(1));
                                Assert.Equal(glength1[j] * glength2[j], arr.Length);
                            }

                            // lower can be < 0
                            glower1 = new int[] { 10, 10, 65535, 40, 0, -10, -10, -20, -40, 0 };
                            glower2 = new int[] { 5, 99, -100, 30, 4, -5, 99, 100, -30, 0 };
                            glength1 = new int[] { 1, 200, 2, 40, 0, 1, 200, 2, 40, 65535 };
                            glength2 = new int[] { 5, 10, 1, 0, 4, 5, 65535, 1, 0, 4 };

                            for (int j = 0; j < glength1.Length; j++)
                            {
                                arr = (int[,])cia[i].Invoke(new Object[] { glower1[j], glength1[j], glower2[j], glength2[j] });
                                Assert.Equal(glower1[j], arr.GetLowerBound(0));
                                Assert.Equal(glower1[j] + glength1[j] - 1, arr.GetUpperBound(0));
                                Assert.Equal(glower2[j], arr.GetLowerBound(1));
                                Assert.Equal(glower2[j] + glength2[j] - 1, arr.GetUpperBound(1));
                                Assert.Equal(glength1[j] * glength2[j], arr.Length);
                            }
                        }
                        break;
                }
            }
        }
        [Fact]
        public void Test4DArrayConstructorInvoke()
        {
            Type type = Type.GetType("System.Type[,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,]");
            ConstructorInfo[] cia = type.GetConstructors();
            Assert.Equal(2, cia.Length);
            Assert.Throws<TypeLoadException>(() =>
            {
                type = Type.GetType("System.Type[,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,]");
            });
        }

        [Fact]
        public void TestJaggedArrayConstructorInvoke()
        {
            Type type = Type.GetType("System.String[][]");
            ConstructorInfo[] cia = type.GetConstructors();
            Assert.Equal(2, cia.Length);

            for (int i = 0; i < cia.Length; i++)
            {
                string[][] arr = null;
                ParameterInfo[] pia = cia[i].GetParameters();
                switch (pia.Length)
                {
                    case 1:
                        {
                            int[] blength1 = new int[] { -11, -10, -99 };
                            for (int j = 0; j < blength1.Length; j++)
                            {
                                Assert.Throws<OverflowException>(() =>
                                {
                                    arr = (string[][])cia[i].Invoke(new Object[] { blength1[j] });
                                });
                            }

                            int[] glength1 = new int[] { 0, 1, 2, 10, 17, 99 }; // 
                            for (int j = 0; j < glength1.Length; j++)
                            {
                                arr = (string[][])cia[i].Invoke(new Object[] { glength1[j] });
                                Assert.Equal(0, arr.GetLowerBound(0));
                                Assert.Equal(glength1[j] - 1, arr.GetUpperBound(0));
                                Assert.Equal(glength1[j], arr.Length);
                            }
                        }
                        break;
                    case 2:
                        {
                            int[] blength1 = new int[] { -11, -10, 10, 1 };
                            int[] blength2 = new int[] { -33, 0, -33, -1 };
                            for (int j = 0; j < blength1.Length; j++)
                            {
                                Assert.Throws<OverflowException>(() =>
                                {
                                    arr = (string[][])cia[i].Invoke(new Object[] { blength1[j], blength2[j] });
                                });
                            }

                            int[] glength1 = new int[] { 0, 0, 0, 1, 1, 2, 1, 2, 10, 17, 500 };
                            int[] glength2 = new int[] { -33, 0, 1, 0, 1, 1, 2, 2, 110, 5, 100 };
                            for (int j = 0; j < glength1.Length; j++)
                            {
                                arr = (string[][])cia[i].Invoke(new Object[] { glength1[j], glength2[j] });
                                Assert.Equal(0, arr.GetLowerBound(0));
                                Assert.Equal(glength1[j] - 1, arr.GetUpperBound(0));
                                Assert.Equal(glength1[j], arr.Length);

                                if (glength1[j] == 0)
                                {
                                    Assert.Equal(arr.Length, 0);
                                }
                                else
                                {
                                    Assert.Equal(0, arr[0].GetLowerBound(0));
                                    Assert.Equal(glength2[j] - 1, arr[0].GetUpperBound(0));
                                    Assert.Equal(glength2[j], arr[0].Length);
                                }
                            }
                        }
                        break;
                }
            }
        }
    }
}