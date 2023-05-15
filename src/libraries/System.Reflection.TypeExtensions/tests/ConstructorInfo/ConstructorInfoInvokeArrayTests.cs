// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Reflection.Tests
{
    public class ConstructorInfoInvokeArrayTests
    {
        [Fact]
        public void Invoke_SZArrayConstructor()
        {
            Type type = Type.GetType("System.Object[]");
            ConstructorInfo[] constructors = TypeExtensions.GetConstructors(type);
            Assert.Equal(1, constructors.Length);

            ConstructorInfo constructor = constructors[0];
            int[] blength = new int[] { -100, -9, -1 };
            for (int j = 0; j < blength.Length; j++)
            {
                Exception ex = Assert.Throws<TargetInvocationException>(() => constructor.Invoke(
                    new object[] { blength[j] }));

                Assert.IsType<OverflowException>(ex.InnerException);
            }

            int[] glength = new int[] { 0, 1, 2, 3, 5, 10, 99, 65535 };
            for (int j = 0; j < glength.Length; j++)
            {
                object[] arr = (object[])constructor.Invoke(new object[] { glength[j] });
                Assert.Equal(0, arr.GetLowerBound(0));
                Assert.Equal(glength[j] - 1, arr.GetUpperBound(0));
                Assert.Equal(glength[j], arr.Length);
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/mono/mono/issues/15316", TestRuntimes.Mono)]
        public void Invoke_1DArrayConstructor()
        {
            Type type = Type.GetType("System.Char[*]");
            MethodInfo getLowerBound = TypeExtensions.GetMethod(type, "GetLowerBound");
            MethodInfo getUpperBound = TypeExtensions.GetMethod(type, "GetUpperBound");
            PropertyInfo getLength = TypeExtensions.GetProperty(type, "Length");

            ConstructorInfo[] constructors = TypeExtensions.GetConstructors(type);
            Assert.Equal(2, constructors.Length);

            for (int i = 0; i < constructors.Length; i++)
            {
                switch (constructors[i].GetParameters().Length)
                {
                    case 1:
                        {
                            int[] invalidLengths = new int[] { -100, -9, -1 };
                            for (int j = 0; j < invalidLengths.Length; j++)
                            {
                                Exception ex = Assert.Throws<TargetInvocationException>(() => constructors[i].Invoke(
                                    new object[] { invalidLengths[j] }));

                                Assert.IsType<OverflowException>(ex.InnerException);
                            }

                            int[] validLengths = new int[] { 0, 1, 2, 3, 5, 10, 99 };
                            for (int j = 0; j < validLengths.Length; j++)
                            {
                                char[] arr = (char[])constructors[i].Invoke(new object[] { validLengths[j] });
                                Assert.Equal(0, arr.GetLowerBound(0));
                                Assert.Equal(validLengths[j] - 1, arr.GetUpperBound(0));
                                Assert.Equal(validLengths[j], arr.Length);
                            }
                        }
                        break;
                    case 2:
                        {
                            int[] invalidLowerBounds = new int[] { -20, 0, 20 };
                            if (!PlatformDetection.IsNonZeroLowerBoundArraySupported)
                            {
                                Array.Clear(invalidLowerBounds);
                            }
                            int[] invalidLengths = new int[] { -100, -9, -1 };
                            for (int j = 0; j < invalidLengths.Length; j++)
                            {
                                Exception ex = Assert.Throws<TargetInvocationException>(() => constructors[i].Invoke(
                                    new object[] { invalidLowerBounds[j], invalidLengths[j] }));

                                Assert.IsType<OverflowException>(ex.InnerException);
                            }

                            int[] validLowerBounds = new int[] { 0, 1, -1, 2, -3, 5, -10, 99, 100 };
                            if (!PlatformDetection.IsNonZeroLowerBoundArraySupported)
                            {
                                Array.Clear(validLowerBounds);
                            }
                            int[] validLengths = new int[] { 0, 1, 3, 2, 3, 5, 10, 99, 0 };
                            for (int j = 0; j < validLengths.Length; j++)
                            {
                                object o = constructors[i].Invoke(new object[] { validLowerBounds[j], validLengths[j] });

                                Assert.Equal(validLowerBounds[j], (int)getLowerBound.Invoke(o, new object[] { 0 }));
                                Assert.Equal(validLowerBounds[j] + validLengths[j] - 1, (int)getUpperBound.Invoke(o, new object[] { 0 }));
                                Assert.Equal(validLengths[j], (int)getLength.GetValue(o, null));
                            }
                        }
                        break;
                }
            }
        }

        [Fact]
        public void Invoke_2DArrayConstructor()
        {
            Type type = Type.GetType("System.Int32[,]", false);

            ConstructorInfo[] constructors = TypeExtensions.GetConstructors(type);
            Assert.Equal(2, constructors.Length);

            for (int i = 0; i < constructors.Length; i++)
            {
                switch (constructors[i].GetParameters().Length)
                {
                    case 2:
                        {
                            int[] invalidLengths1 = new int[] { -11, -10, 0, 10 };
                            int[] invalidLengths2 = new int[] { -33, 0, -20, -33 };

                            for (int j = 0; j < invalidLengths1.Length; j++)
                            {
                                Exception ex = Assert.Throws<TargetInvocationException>(() => constructors[i].Invoke(
                                    new object[] { invalidLengths1[j], invalidLengths2[j] }));

                                Assert.IsType<OverflowException>(ex.InnerException);
                            }

                            int[] validLengths1 = new int[] { 0, 0, 1, 1, 2, 1, 2, 10, 17, 99 };
                            int[] validLengths2 = new int[] { 0, 1, 0, 1, 1, 2, 2, 110, 5, 900 };

                            for (int j = 0; j < validLengths1.Length; j++)
                            {
                                int[,] arr = (int[,])constructors[i].Invoke(new object[] { validLengths1[j], validLengths2[j] });
                                Assert.Equal(0, arr.GetLowerBound(0));
                                Assert.Equal(validLengths1[j] - 1, arr.GetUpperBound(0));
                                Assert.Equal(0, arr.GetLowerBound(1));
                                Assert.Equal(validLengths2[j] - 1, arr.GetUpperBound(1));
                                Assert.Equal(validLengths1[j] * validLengths2[j], arr.Length);
                            }
                        }

                        break;
                    case 4:
                        {
                            int[] invalidLowerBounds1 = new int[] { 10, -10, 20 };
                            int[] invalidLowerBounds2 = new int[] { -10, 10, 0 };
                            int[] invalidLengths3 = new int[] { -11, -10, 0 };
                            int[] invalidLengths4 = new int[] { -33, 0, -20 };

                            if (!PlatformDetection.IsNonZeroLowerBoundArraySupported)
                            {
                                Array.Clear(invalidLowerBounds1);
                                Array.Clear(invalidLowerBounds2);
                            }

                            for (int j = 0; j < invalidLengths3.Length; j++)
                            {
                                Exception ex = Assert.Throws<TargetInvocationException>(() => constructors[i].Invoke(
                                    new object[] { invalidLowerBounds1[j], invalidLengths3[j], invalidLowerBounds2[j], invalidLengths4[j] }));

                                Assert.IsType<OverflowException>(ex.InnerException);
                            }

                            int baseNum = 3;
                            int baseNum4 = baseNum * baseNum * baseNum * baseNum;
                            int[] validLowerBounds1 = new int[baseNum4];
                            int[] validLowerBounds2 = new int[baseNum4];
                            int[] validLengths1 = new int[baseNum4];
                            int[] validLengths2 = new int[baseNum4];

                            int cnt = 0;
                            for (int pos1 = 0; pos1 < baseNum; pos1++)
                                for (int pos2 = 0; pos2 < baseNum; pos2++)
                                    for (int pos3 = 0; pos3 < baseNum; pos3++)
                                        for (int pos4 = 0; pos4 < baseNum; pos4++)
                                        {
                                            int saved = cnt;
                                            validLowerBounds1[cnt] = saved % baseNum;
                                            saved = saved / baseNum;
                                            validLengths1[cnt] = saved % baseNum;
                                            saved = saved / baseNum;
                                            validLowerBounds2[cnt] = saved % baseNum;
                                            saved = saved / baseNum;
                                            validLengths2[cnt] = saved % baseNum;
                                            cnt++;
                                        }

                            if (!PlatformDetection.IsNonZeroLowerBoundArraySupported)
                            {
                                Array.Clear(validLowerBounds1);
                                Array.Clear(validLowerBounds2);
                            }

                            for (int j = 0; j < validLengths1.Length; j++)
                            {
                                int[,] arr = (int[,])constructors[i].Invoke(new object[] { validLowerBounds1[j], validLengths1[j], validLowerBounds2[j], validLengths2[j] });
                                Assert.Equal(validLowerBounds1[j], arr.GetLowerBound(0));
                                Assert.Equal(validLowerBounds1[j] + validLengths1[j] - 1, arr.GetUpperBound(0));
                                Assert.Equal(validLowerBounds2[j], arr.GetLowerBound(1));
                                Assert.Equal(validLowerBounds2[j] + validLengths2[j] - 1, arr.GetUpperBound(1));
                                Assert.Equal(validLengths1[j] * validLengths2[j], arr.Length);
                            }

                            // Lower can be < 0
                            validLowerBounds1 = new int[] { 10, 10, 65535, 40, 0, -10, -10, -20, -40, 0 };
                            validLowerBounds2 = new int[] { 5, 99, -100, 30, 4, -5, 99, 100, -30, 0 };
                            validLengths1 = new int[] { 1, 200, 2, 40, 0, 1, 200, 2, 40, 65535 };
                            validLengths2 = new int[] { 5, 10, 1, 0, 4, 5, 65535, 1, 0, 4 };

                            if (!PlatformDetection.IsNonZeroLowerBoundArraySupported)
                            {
                                Array.Clear(validLowerBounds1);
                                Array.Clear(validLowerBounds2);
                            }

                            for (int j = 0; j < validLengths1.Length; j++)
                            {
                                int[,] arr = (int[,])constructors[i].Invoke(new object[] { validLowerBounds1[j], validLengths1[j], validLowerBounds2[j], validLengths2[j] });
                                Assert.Equal(validLowerBounds1[j], arr.GetLowerBound(0));
                                Assert.Equal(validLowerBounds1[j] + validLengths1[j] - 1, arr.GetUpperBound(0));
                                Assert.Equal(validLowerBounds2[j], arr.GetLowerBound(1));
                                Assert.Equal(validLowerBounds2[j] + validLengths2[j] - 1, arr.GetUpperBound(1));
                                Assert.Equal(validLengths1[j] * validLengths2[j], arr.Length);
                            }
                        }
                        break;
                }
            }
        }
        [Fact]
        [ActiveIssue("https://github.com/mono/mono/issues/15318", TestRuntimes.Mono)]
        public void Invoke_LargeDimensionalArrayConstructor()
        {
            Type type = Type.GetType($"System.Type[{new string(',', 31)}]");
            ConstructorInfo[] cia = TypeExtensions.GetConstructors(type);
            Assert.Equal(2, cia.Length);
            Assert.Throws<TypeLoadException>(() => Type.GetType($"System.Type[{new string(',', 42)}]"));
        }

        [Fact]
        public void Invoke_JaggedArrayConstructor()
        {
            Type type = Type.GetType("System.String[][]");
            ConstructorInfo[] constructors = TypeExtensions.GetConstructors(type);
            Assert.Equal(2, constructors.Length);

            for (int i = 0; i < constructors.Length; i++)
            {
                switch (constructors[i].GetParameters().Length)
                {
                    case 1:
                        {
                            int[] invalidLengths = new int[] { -11, -10, -99 };
                            for (int j = 0; j < invalidLengths.Length; j++)
                            {
                                Exception ex = Assert.Throws<TargetInvocationException>(() => constructors[i].Invoke(
                                    new object[] { invalidLengths[j] }));

                                Assert.IsType<OverflowException>(ex.InnerException);
                            }

                            int[] validLengths = new int[] { 0, 1, 2, 10, 17, 99 };
                            for (int j = 0; j < validLengths.Length; j++)
                            {
                                string[][] arr = (string[][])constructors[i].Invoke(new object[] { validLengths[j] });
                                Assert.Equal(0, arr.GetLowerBound(0));
                                Assert.Equal(validLengths[j] - 1, arr.GetUpperBound(0));
                                Assert.Equal(validLengths[j], arr.Length);
                            }
                        }
                        break;
                    case 2:
                        {
                            int[] invalidLengths1 = new int[] { -11, -10, 10, 1 };
                            int[] invalidLengths2 = new int[] { -33, 0, -33, -1 };
                            for (int j = 0; j < invalidLengths1.Length; j++)
                            {
                                Exception ex = Assert.Throws<TargetInvocationException>(() => constructors[i].Invoke(
                                     new object[] { invalidLengths1[j], invalidLengths2[j] }));

                                Assert.IsType<OverflowException>(ex.InnerException);
                            }

                            int[] validLengths1 = new int[] { 0, 0, 0, 1, 1, 2, 1, 2, 10, 17, 500 };
                            int[] validLengths2 = new int[] { -33, 0, 1, 0, 1, 1, 2, 2, 110, 5, 100 };
                            for (int j = 0; j < validLengths1.Length; j++)
                            {
                                string[][] arr = (string[][])constructors[i].Invoke(new object[] { validLengths1[j], validLengths2[j] });
                                Assert.Equal(0, arr.GetLowerBound(0));
                                Assert.Equal(validLengths1[j] - 1, arr.GetUpperBound(0));
                                Assert.Equal(validLengths1[j], arr.Length);

                                if (validLengths1[j] == 0)
                                {
                                    Assert.Equal(0, arr.Length);
                                }
                                else
                                {
                                    Assert.Equal(0, arr[0].GetLowerBound(0));
                                    Assert.Equal(validLengths2[j] - 1, arr[0].GetUpperBound(0));
                                    Assert.Equal(validLengths2[j], arr[0].Length);
                                }
                            }
                        }
                        break;
                }
            }
        }
    }
}
