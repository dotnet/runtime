// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class NewArrayListTests
    {
        #region Tests

        [Fact]
        public static void CheckBoolArrayListTest()
        {
            bool[][] array = new bool[][]
                {
                    new bool[] {  },
                    new bool[] { true },
                    new bool[] { true, false }
                };
            Expression[][] exprs = new Expression[array.Length][];
            for (int i = 0; i < array.Length; i++)
            {
                exprs[i] = new Expression[array[i].Length];
                for (int j = 0; j < array[i].Length; j++)
                {
                    bool val = array[i][j];
                    exprs[i][j] = Expression.Constant(val, typeof(bool));
                }
            }

            for (int i = 0; i < array.Length; i++)
            {
                VerifyBoolArrayList(array[i], exprs[i]);
            }
        }

        [Fact]
        public static void CheckByteArrayListTest()
        {
            byte[][] array = new byte[][]
                {
                    new byte[] {  },
                    new byte[] { 0 },
                    new byte[] { 0, 1, byte.MaxValue }
                };
            Expression[][] exprs = new Expression[array.Length][];
            for (int i = 0; i < array.Length; i++)
            {
                exprs[i] = new Expression[array[i].Length];
                for (int j = 0; j < array[i].Length; j++)
                {
                    byte val = array[i][j];
                    exprs[i][j] = Expression.Constant(val, typeof(byte));
                }
            }

            for (int i = 0; i < array.Length; i++)
            {
                VerifyByteArrayList(array[i], exprs[i]);
            }
        }

        [Fact]
        public static void CheckCustomArrayListTest()
        {
            C[][] array = new C[][]
                {
                    new C[] {  },
                    new C[] { null },
                    new C[] { new C(), new D(), new D(0), new D(5) }
                };
            Expression[][] exprs = new Expression[array.Length][];
            for (int i = 0; i < array.Length; i++)
            {
                exprs[i] = new Expression[array[i].Length];
                for (int j = 0; j < array[i].Length; j++)
                {
                    C val = array[i][j];
                    exprs[i][j] = Expression.Constant(val, typeof(C));
                }
            }

            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustomArrayList(array[i], exprs[i]);
            }
        }

        [Fact]
        public static void CheckCharArrayListTest()
        {
            char[][] array = new char[][]
                {
                    new char[] {  },
                    new char[] { '\0' },
                    new char[] { '\0', '\b', 'A', '\uffff' }
                };
            Expression[][] exprs = new Expression[array.Length][];
            for (int i = 0; i < array.Length; i++)
            {
                exprs[i] = new Expression[array[i].Length];
                for (int j = 0; j < array[i].Length; j++)
                {
                    char val = array[i][j];
                    exprs[i][j] = Expression.Constant(val, typeof(char));
                }
            }

            for (int i = 0; i < array.Length; i++)
            {
                VerifyCharArrayList(array[i], exprs[i]);
            }
        }

        [Fact]
        public static void CheckCustom2ArrayListTest()
        {
            D[][] array = new D[][]
                {
                    new D[] {  },
                    new D[] { null },
                    new D[] { null, new D(), new D(0), new D(5) }
                };
            Expression[][] exprs = new Expression[array.Length][];
            for (int i = 0; i < array.Length; i++)
            {
                exprs[i] = new Expression[array[i].Length];
                for (int j = 0; j < array[i].Length; j++)
                {
                    D val = array[i][j];
                    exprs[i][j] = Expression.Constant(val, typeof(D));
                }
            }

            for (int i = 0; i < array.Length; i++)
            {
                VerifyCustom2ArrayList(array[i], exprs[i]);
            }
        }

        [Fact]
        public static void CheckDecimalArrayListTest()
        {
            decimal[][] array = new decimal[][]
                {
                    new decimal[] {  },
                    new decimal[] { decimal.Zero },
                    new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue }
                };
            Expression[][] exprs = new Expression[array.Length][];
            for (int i = 0; i < array.Length; i++)
            {
                exprs[i] = new Expression[array[i].Length];
                for (int j = 0; j < array[i].Length; j++)
                {
                    decimal val = array[i][j];
                    exprs[i][j] = Expression.Constant(val, typeof(decimal));
                }
            }

            for (int i = 0; i < array.Length; i++)
            {
                VerifyDecimalArrayList(array[i], exprs[i]);
            }
        }

        [Fact]
        public static void CheckDelegateArrayListTest()
        {
            Delegate[][] array = new Delegate[][]
                {
                    new Delegate[] {  },
                    new Delegate[] { null },
                    new Delegate[] { null, (Func<object>) delegate() { return null; }, (Func<int, int>) delegate(int i) { return i+1; }, (Action<object>) delegate { } }
                };
            Expression[][] exprs = new Expression[array.Length][];
            for (int i = 0; i < array.Length; i++)
            {
                exprs[i] = new Expression[array[i].Length];
                for (int j = 0; j < array[i].Length; j++)
                {
                    Delegate val = array[i][j];
                    exprs[i][j] = Expression.Constant(val, typeof(Delegate));
                }
            }

            for (int i = 0; i < array.Length; i++)
            {
                VerifyDelegateArrayList(array[i], exprs[i]);
            }
        }

        [Fact]
        public static void CheckDoubleArrayListTest()
        {
            double[][] array = new double[][]
                {
                    new double[] {  },
                    new double[] { 0 },
                    new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN }
                };
            Expression[][] exprs = new Expression[array.Length][];
            for (int i = 0; i < array.Length; i++)
            {
                exprs[i] = new Expression[array[i].Length];
                for (int j = 0; j < array[i].Length; j++)
                {
                    double val = array[i][j];
                    exprs[i][j] = Expression.Constant(val, typeof(double));
                }
            }

            for (int i = 0; i < array.Length; i++)
            {
                VerifyDoubleArrayList(array[i], exprs[i]);
            }
        }

        [Fact]
        public static void CheckEnumArrayListTest()
        {
            E[][] array = new E[][]
                {
                    new E[] {  },
                    new E[] { (E) 0 },
                    new E[] { (E) 0, E.A, E.B, (E) int.MaxValue, (E) int.MinValue }
                };
            Expression[][] exprs = new Expression[array.Length][];
            for (int i = 0; i < array.Length; i++)
            {
                exprs[i] = new Expression[array[i].Length];
                for (int j = 0; j < array[i].Length; j++)
                {
                    E val = array[i][j];
                    exprs[i][j] = Expression.Constant(val, typeof(E));
                }
            }

            for (int i = 0; i < array.Length; i++)
            {
                VerifyEnumArrayList(array[i], exprs[i]);
            }
        }

        [Fact]
        public static void CheckEnumLongArrayListTest()
        {
            El[][] array = new El[][]
                {
                    new El[] {  },
                    new El[] { (El) 0 },
                    new El[] { (El) 0, El.A, El.B, (El) long.MaxValue, (El) long.MinValue }
                };
            Expression[][] exprs = new Expression[array.Length][];
            for (int i = 0; i < array.Length; i++)
            {
                exprs[i] = new Expression[array[i].Length];
                for (int j = 0; j < array[i].Length; j++)
                {
                    El val = array[i][j];
                    exprs[i][j] = Expression.Constant(val, typeof(El));
                }
            }

            for (int i = 0; i < array.Length; i++)
            {
                VerifyEnumLongArrayList(array[i], exprs[i]);
            }
        }

        [Fact]
        public static void CheckFloatArrayListTest()
        {
            float[][] array = new float[][]
                {
                    new float[] {  },
                    new float[] { 0 },
                    new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN }
                };
            Expression[][] exprs = new Expression[array.Length][];
            for (int i = 0; i < array.Length; i++)
            {
                exprs[i] = new Expression[array[i].Length];
                for (int j = 0; j < array[i].Length; j++)
                {
                    float val = array[i][j];
                    exprs[i][j] = Expression.Constant(val, typeof(float));
                }
            }

            for (int i = 0; i < array.Length; i++)
            {
                VerifyFloatArrayList(array[i], exprs[i]);
            }
        }

        [Fact]
        public static void CheckFuncArrayListTest()
        {
            Func<object>[][] array = new Func<object>[][]
                {
                    new Func<object>[] {  },
                    new Func<object>[] { null },
                    new Func<object>[] { null, (Func<object>) delegate() { return null; } }
                };
            Expression[][] exprs = new Expression[array.Length][];
            for (int i = 0; i < array.Length; i++)
            {
                exprs[i] = new Expression[array[i].Length];
                for (int j = 0; j < array[i].Length; j++)
                {
                    Func<object> val = array[i][j];
                    exprs[i][j] = Expression.Constant(val, typeof(Func<object>));
                }
            }

            for (int i = 0; i < array.Length; i++)
            {
                VerifyFuncArrayList(array[i], exprs[i]);
            }
        }

        [Fact]
        public static void CheckInterfaceArrayListTest()
        {
            I[][] array = new I[][]
                {
                    new I[] {  },
                    new I[] { null },
                    new I[] { null, new C(), new D(), new D(0), new D(5) }
                };
            Expression[][] exprs = new Expression[array.Length][];
            for (int i = 0; i < array.Length; i++)
            {
                exprs[i] = new Expression[array[i].Length];
                for (int j = 0; j < array[i].Length; j++)
                {
                    I val = array[i][j];
                    exprs[i][j] = Expression.Constant(val, typeof(I));
                }
            }

            for (int i = 0; i < array.Length; i++)
            {
                VerifyInterfaceArrayList(array[i], exprs[i]);
            }
        }

        [Fact]
        public static void CheckIEquatableCustomArrayListTest()
        {
            IEquatable<C>[][] array = new IEquatable<C>[][]
                {
                    new IEquatable<C>[] {  },
                    new IEquatable<C>[] { null },
                    new IEquatable<C>[] { null, new C(), new D(), new D(0), new D(5) }
                };
            Expression[][] exprs = new Expression[array.Length][];
            for (int i = 0; i < array.Length; i++)
            {
                exprs[i] = new Expression[array[i].Length];
                for (int j = 0; j < array[i].Length; j++)
                {
                    IEquatable<C> val = array[i][j];
                    exprs[i][j] = Expression.Constant(val, typeof(IEquatable<C>));
                }
            }

            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEquatableCustomArrayList(array[i], exprs[i]);
            }
        }

        [Fact]
        public static void CheckIEquatableCustom2ArrayListTest()
        {
            IEquatable<D>[][] array = new IEquatable<D>[][]
                {
                    new IEquatable<D>[] {  },
                    new IEquatable<D>[] { null },
                    new IEquatable<D>[] { null, new D(), new D(0), new D(5) }
                };
            Expression[][] exprs = new Expression[array.Length][];
            for (int i = 0; i < array.Length; i++)
            {
                exprs[i] = new Expression[array[i].Length];
                for (int j = 0; j < array[i].Length; j++)
                {
                    IEquatable<D> val = array[i][j];
                    exprs[i][j] = Expression.Constant(val, typeof(IEquatable<D>));
                }
            }

            for (int i = 0; i < array.Length; i++)
            {
                VerifyIEquatableCustom2ArrayList(array[i], exprs[i]);
            }
        }

        [Fact]
        public static void CheckIntArrayListTest()
        {
            int[][] array = new int[][]
                {
                    new int[] {  },
                    new int[] { 0 },
                    new int[] { 0, 1, -1, int.MinValue, int.MaxValue }
                };
            Expression[][] exprs = new Expression[array.Length][];
            for (int i = 0; i < array.Length; i++)
            {
                exprs[i] = new Expression[array[i].Length];
                for (int j = 0; j < array[i].Length; j++)
                {
                    int val = array[i][j];
                    exprs[i][j] = Expression.Constant(val, typeof(int));
                }
            }

            for (int i = 0; i < array.Length; i++)
            {
                VerifyIntArrayList(array[i], exprs[i]);
            }
        }

        [Fact]
        public static void CheckLongArrayListTest()
        {
            long[][] array = new long[][]
                {
                    new long[] {  },
                    new long[] { 0 },
                    new long[] { 0, 1, -1, long.MinValue, long.MaxValue }
                };
            Expression[][] exprs = new Expression[array.Length][];
            for (int i = 0; i < array.Length; i++)
            {
                exprs[i] = new Expression[array[i].Length];
                for (int j = 0; j < array[i].Length; j++)
                {
                    long val = array[i][j];
                    exprs[i][j] = Expression.Constant(val, typeof(long));
                }
            }

            for (int i = 0; i < array.Length; i++)
            {
                VerifyLongArrayList(array[i], exprs[i]);
            }
        }

        [Fact]
        public static void CheckObjectArrayListTest()
        {
            object[][] array = new object[][]
                {
                    new object[] {  },
                    new object[] { null },
                    new object[] { null, new object(), new C(), new D(3) }
                };
            Expression[][] exprs = new Expression[array.Length][];
            for (int i = 0; i < array.Length; i++)
            {
                exprs[i] = new Expression[array[i].Length];
                for (int j = 0; j < array[i].Length; j++)
                {
                    object val = array[i][j];
                    exprs[i][j] = Expression.Constant(val, typeof(object));
                }
            }

            for (int i = 0; i < array.Length; i++)
            {
                VerifyObjectArrayList(array[i], exprs[i]);
            }
        }

        [Fact]
        public static void CheckStructArrayListTest()
        {
            S[][] array = new S[][]
                {
                    new S[] {  },
                    new S[] { default(S) },
                    new S[] { default(S), new S() }
                };
            Expression[][] exprs = new Expression[array.Length][];
            for (int i = 0; i < array.Length; i++)
            {
                exprs[i] = new Expression[array[i].Length];
                for (int j = 0; j < array[i].Length; j++)
                {
                    S val = array[i][j];
                    exprs[i][j] = Expression.Constant(val, typeof(S));
                }
            }

            for (int i = 0; i < array.Length; i++)
            {
                VerifyStructArrayList(array[i], exprs[i]);
            }
        }

        [Fact]
        public static void CheckSByteArrayListTest()
        {
            sbyte[][] array = new sbyte[][]
                {
                    new sbyte[] {  },
                    new sbyte[] { 0 },
                    new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue }
                };
            Expression[][] exprs = new Expression[array.Length][];
            for (int i = 0; i < array.Length; i++)
            {
                exprs[i] = new Expression[array[i].Length];
                for (int j = 0; j < array[i].Length; j++)
                {
                    sbyte val = array[i][j];
                    exprs[i][j] = Expression.Constant(val, typeof(sbyte));
                }
            }

            for (int i = 0; i < array.Length; i++)
            {
                VerifySByteArrayList(array[i], exprs[i]);
            }
        }

        [Fact]
        public static void CheckStructWithStringArrayListTest()
        {
            Sc[][] array = new Sc[][]
                {
                    new Sc[] {  },
                    new Sc[] { default(Sc) },
                    new Sc[] { default(Sc), new Sc(), new Sc(null) }
                };
            Expression[][] exprs = new Expression[array.Length][];
            for (int i = 0; i < array.Length; i++)
            {
                exprs[i] = new Expression[array[i].Length];
                for (int j = 0; j < array[i].Length; j++)
                {
                    Sc val = array[i][j];
                    exprs[i][j] = Expression.Constant(val, typeof(Sc));
                }
            }

            for (int i = 0; i < array.Length; i++)
            {
                VerifyStructWithStringArrayList(array[i], exprs[i]);
            }
        }

        [Fact]
        public static void CheckStructWithStringAndFieldArrayListTest()
        {
            Scs[][] array = new Scs[][]
                {
                    new Scs[] {  },
                    new Scs[] { default(Scs) },
                    new Scs[] { default(Scs), new Scs(), new Scs(null,new S()) }
                };
            Expression[][] exprs = new Expression[array.Length][];
            for (int i = 0; i < array.Length; i++)
            {
                exprs[i] = new Expression[array[i].Length];
                for (int j = 0; j < array[i].Length; j++)
                {
                    Scs val = array[i][j];
                    exprs[i][j] = Expression.Constant(val, typeof(Scs));
                }
            }

            for (int i = 0; i < array.Length; i++)
            {
                VerifyStructWithStringAndFieldArrayList(array[i], exprs[i]);
            }
        }

        [Fact]
        public static void CheckShortArrayListTest()
        {
            short[][] array = new short[][]
                {
                    new short[] {  },
                    new short[] { 0 },
                    new short[] { 0, 1, -1, short.MinValue, short.MaxValue }
                };
            Expression[][] exprs = new Expression[array.Length][];
            for (int i = 0; i < array.Length; i++)
            {
                exprs[i] = new Expression[array[i].Length];
                for (int j = 0; j < array[i].Length; j++)
                {
                    short val = array[i][j];
                    exprs[i][j] = Expression.Constant(val, typeof(short));
                }
            }

            for (int i = 0; i < array.Length; i++)
            {
                VerifyShortArrayList(array[i], exprs[i]);
            }
        }

        [Fact]
        public static void CheckStructWithTwoValuesArrayListTest()
        {
            Sp[][] array = new Sp[][]
                {
                    new Sp[] {  },
                    new Sp[] { default(Sp) },
                    new Sp[] { default(Sp), new Sp(), new Sp(5,5.0) }
                };
            Expression[][] exprs = new Expression[array.Length][];
            for (int i = 0; i < array.Length; i++)
            {
                exprs[i] = new Expression[array[i].Length];
                for (int j = 0; j < array[i].Length; j++)
                {
                    Sp val = array[i][j];
                    exprs[i][j] = Expression.Constant(val, typeof(Sp));
                }
            }

            for (int i = 0; i < array.Length; i++)
            {
                VerifyStructWithTwoValuesArrayList(array[i], exprs[i]);
            }
        }

        [Fact]
        public static void CheckStructWithValueArrayListTest()
        {
            Ss[][] array = new Ss[][]
                {
                    new Ss[] {  },
                    new Ss[] { default(Ss) },
                    new Ss[] { default(Ss), new Ss(), new Ss(new S()) }
                };
            Expression[][] exprs = new Expression[array.Length][];
            for (int i = 0; i < array.Length; i++)
            {
                exprs[i] = new Expression[array[i].Length];
                for (int j = 0; j < array[i].Length; j++)
                {
                    Ss val = array[i][j];
                    exprs[i][j] = Expression.Constant(val, typeof(Ss));
                }
            }

            for (int i = 0; i < array.Length; i++)
            {
                VerifyStructWithValueArrayList(array[i], exprs[i]);
            }
        }

        [Fact]
        public static void CheckStringArrayListTest()
        {
            string[][] array = new string[][]
                {
                    new string[] {  },
                    new string[] { null },
                    new string[] { null, "", "a", "foo" }
                };
            Expression[][] exprs = new Expression[array.Length][];
            for (int i = 0; i < array.Length; i++)
            {
                exprs[i] = new Expression[array[i].Length];
                for (int j = 0; j < array[i].Length; j++)
                {
                    string val = array[i][j];
                    exprs[i][j] = Expression.Constant(val, typeof(string));
                }
            }

            for (int i = 0; i < array.Length; i++)
            {
                VerifyStringArrayList(array[i], exprs[i]);
            }
        }

        [Fact]
        public static void CheckUIntArrayListTest()
        {
            uint[][] array = new uint[][]
                {
                    new uint[] {  },
                    new uint[] { 0 },
                    new uint[] { 0, 1, uint.MaxValue }
                };
            Expression[][] exprs = new Expression[array.Length][];
            for (int i = 0; i < array.Length; i++)
            {
                exprs[i] = new Expression[array[i].Length];
                for (int j = 0; j < array[i].Length; j++)
                {
                    uint val = array[i][j];
                    exprs[i][j] = Expression.Constant(val, typeof(uint));
                }
            }

            for (int i = 0; i < array.Length; i++)
            {
                VerifyUIntArrayList(array[i], exprs[i]);
            }
        }

        [Fact]
        public static void CheckULongArrayListTest()
        {
            ulong[][] array = new ulong[][]
                {
                    new ulong[] {  },
                    new ulong[] { 0 },
                    new ulong[] { 0, 1, ulong.MaxValue }
                };
            Expression[][] exprs = new Expression[array.Length][];
            for (int i = 0; i < array.Length; i++)
            {
                exprs[i] = new Expression[array[i].Length];
                for (int j = 0; j < array[i].Length; j++)
                {
                    ulong val = array[i][j];
                    exprs[i][j] = Expression.Constant(val, typeof(ulong));
                }
            }

            for (int i = 0; i < array.Length; i++)
            {
                VerifyULongArrayList(array[i], exprs[i]);
            }
        }

        [Fact]
        public static void CheckUShortArrayListTest()
        {
            ushort[][] array = new ushort[][]
                {
                    new ushort[] {  },
                    new ushort[] { 0 },
                    new ushort[] { 0, 1, ushort.MaxValue }
                };
            Expression[][] exprs = new Expression[array.Length][];
            for (int i = 0; i < array.Length; i++)
            {
                exprs[i] = new Expression[array[i].Length];
                for (int j = 0; j < array[i].Length; j++)
                {
                    ushort val = array[i][j];
                    exprs[i][j] = Expression.Constant(val, typeof(ushort));
                }
            }

            for (int i = 0; i < array.Length; i++)
            {
                VerifyUShortArrayList(array[i], exprs[i]);
            }
        }

        [Fact]
        public static void CheckGenericCustomArrayListTest()
        {
            CheckGenericArrayListHelper<C>();
        }

        [Fact]
        public static void CheckGenericEnumArrayListTest()
        {
            CheckGenericArrayListHelper<E>();
        }

        [Fact]
        public static void CheckGenericObjectArrayListTest()
        {
            CheckGenericArrayListHelper<object>();
        }

        [Fact]
        public static void CheckGenericStructArrayListTest()
        {
            CheckGenericArrayListHelper<S>();
        }

        [Fact]
        public static void CheckGenericStructWithStringAndFieldArrayListTest()
        {
            CheckGenericArrayListHelper<Scs>();
        }

        [Fact]
        public static void CheckGenericCustomWithClassRestrictionArrayListTest()
        {
            CheckGenericWithClassRestrictionArrayListHelper<C>();
        }

        [Fact]
        public static void CheckGenericObjectWithClassRestrictionArrayListTest()
        {
            CheckGenericWithClassRestrictionArrayListHelper<object>();
        }

        [Fact]
        public static void CheckGenericCustomWithSubClassRestrictionArrayListTest()
        {
            CheckGenericWithSubClassRestrictionArrayListHelper<C>();
        }

        [Fact]
        public static void CheckGenericCustomWithClassAndNewRestrictionArrayListTest()
        {
            CheckGenericWithClassAndNewRestrictionArrayListHelper<C>();
        }

        [Fact]
        public static void CheckGenericObjectWithClassAndNewRestrictionArrayListTest()
        {
            CheckGenericWithClassAndNewRestrictionArrayListHelper<object>();
        }

        [Fact]
        public static void CheckGenericCustomWithSubClassAndNewRestrictionArrayListTest()
        {
            CheckGenericWithSubClassAndNewRestrictionArrayListHelper<C>();
        }

        [Fact]
        public static void CheckGenericEnumWithStructRestrictionArrayListTest()
        {
            CheckGenericWithStructRestrictionArrayListHelper<E>();
        }

        [Fact]
        public static void CheckGenericStructWithStructRestrictionArrayListTest()
        {
            CheckGenericWithStructRestrictionArrayListHelper<S>();
        }

        [Fact]
        public static void CheckGenericStructWithStringAndFieldWithStructRestrictionArrayListTest()
        {
            CheckGenericWithStructRestrictionArrayListHelper<Scs>();
        }

        [Fact]
        public static void ThrowOnNegativeSizedCollection()
        {
            // This is an obscure case, and it doesn't much matter what is thrown, as long as is thrown before such
            // an edge case could cause more obscure damage. A class derived from ReadOnlyCollection is used to catch
            // assumptions that such a type is safe.
            Assert.ThrowsAny<Exception>(() => Expression.NewArrayInit(typeof(int), new BogusReadOnlyCollection<Expression>()));
        }

        #endregion

        #region Helper methods

        private class BogusCollection<T> : IList<T>
        {
            public T this[int index]
            {
                get { return default(T); }

                set { throw new NotSupportedException(); }
            }

            public int Count
            {
                get { return -1; }
            }

            public bool IsReadOnly
            {
                get { return true; }
            }

            public void Add(T item)
            {
                throw new NotSupportedException();
            }

            public void Clear()
            {
                throw new NotSupportedException();
            }

            public bool Contains(T item)
            {
                return false;
            }

            public void CopyTo(T[] array, int arrayIndex)
            {
            }

            public IEnumerator<T> GetEnumerator()
            {
                return Enumerable.Empty<T>().GetEnumerator();
            }

            public int IndexOf(T item)
            {
                return -1;
            }

            public void Insert(int index, T item)
            {
                throw new NotSupportedException();
            }

            public bool Remove(T item)
            {
                throw new NotSupportedException();
            }

            public void RemoveAt(int index)
            {
                throw new NotSupportedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private class BogusReadOnlyCollection<T> : ReadOnlyCollection<T>
        {
            public BogusReadOnlyCollection()
                :base(new BogusCollection<T>())
            {

            }
        }

        private static void CheckGenericArrayListHelper<T>()
        {
            T[][] array = new T[][]
                {
                    new T[] { },
                    new T[] { default(T) },
                    new T[] { default(T) }
                };
            Expression[][] exprs = new Expression[array.Length][];
            for (int i = 0; i < array.Length; i++)
            {
                exprs[i] = new Expression[array[i].Length];
                for (int j = 0; j < array[i].Length; j++)
                {
                    T val = array[i][j];
                    exprs[i][j] = Expression.Constant(val, typeof(T));
                }
            }

            for (int i = 0; i < array.Length; i++)
            {
                VerifyGenericArrayList<T>(array[i], exprs[i]);
            }
        }

        private static void CheckGenericWithClassRestrictionArrayListHelper<Tc>() where Tc : class
        {
            Tc[][] array = new Tc[][]
                {
                    new Tc[] { },
                    new Tc[] { default(Tc) },
                    new Tc[] { default(Tc) }
                };
            Expression[][] exprs = new Expression[array.Length][];
            for (int i = 0; i < array.Length; i++)
            {
                exprs[i] = new Expression[array[i].Length];
                for (int j = 0; j < array[i].Length; j++)
                {
                    Tc val = array[i][j];
                    exprs[i][j] = Expression.Constant(val, typeof(Tc));
                }
            }

            for (int i = 0; i < array.Length; i++)
            {
                VerifyGenericWithClassRestrictionArrayList<Tc>(array[i], exprs[i]);
            }
        }

        private static void CheckGenericWithSubClassRestrictionArrayListHelper<TC>() where TC : C
        {
            TC[][] array = new TC[][]
                {
                    new TC[] { },
                    new TC[] { default(TC) },
                    new TC[] { default(TC) }
                };
            Expression[][] exprs = new Expression[array.Length][];
            for (int i = 0; i < array.Length; i++)
            {
                exprs[i] = new Expression[array[i].Length];
                for (int j = 0; j < array[i].Length; j++)
                {
                    TC val = array[i][j];
                    exprs[i][j] = Expression.Constant(val, typeof(TC));
                }
            }

            for (int i = 0; i < array.Length; i++)
            {
                VerifyGenericWithSubClassRestrictionArrayList<TC>(array[i], exprs[i]);
            }
        }

        private static void CheckGenericWithClassAndNewRestrictionArrayListHelper<Tcn>() where Tcn : class, new()
        {
            Tcn[][] array = new Tcn[][]
                {
                    new Tcn[] { },
                    new Tcn[] { default(Tcn) },
                    new Tcn[] { default(Tcn) }
                };
            Expression[][] exprs = new Expression[array.Length][];
            for (int i = 0; i < array.Length; i++)
            {
                exprs[i] = new Expression[array[i].Length];
                for (int j = 0; j < array[i].Length; j++)
                {
                    Tcn val = array[i][j];
                    exprs[i][j] = Expression.Constant(val, typeof(Tcn));
                }
            }

            for (int i = 0; i < array.Length; i++)
            {
                VerifyGenericWithClassAndNewRestrictionArrayList<Tcn>(array[i], exprs[i]);
            }
        }

        private static void CheckGenericWithSubClassAndNewRestrictionArrayListHelper<TCn>() where TCn : C, new()
        {
            TCn[][] array = new TCn[][]
                {
                    new TCn[] { },
                    new TCn[] { default(TCn) },
                    new TCn[] { default(TCn) }
                };
            Expression[][] exprs = new Expression[array.Length][];
            for (int i = 0; i < array.Length; i++)
            {
                exprs[i] = new Expression[array[i].Length];
                for (int j = 0; j < array[i].Length; j++)
                {
                    TCn val = array[i][j];
                    exprs[i][j] = Expression.Constant(val, typeof(TCn));
                }
            }

            for (int i = 0; i < array.Length; i++)
            {
                VerifyGenericWithSubClassAndNewRestrictionArrayList<TCn>(array[i], exprs[i]);
            }
        }

        private static void CheckGenericWithStructRestrictionArrayListHelper<Ts>() where Ts : struct
        {
            Ts[][] array = new Ts[][]
                {
                    new Ts[] { },
                    new Ts[] { default(Ts) },
                    new Ts[] { default(Ts) }
                };
            Expression[][] exprs = new Expression[array.Length][];
            for (int i = 0; i < array.Length; i++)
            {
                exprs[i] = new Expression[array[i].Length];
                for (int j = 0; j < array[i].Length; j++)
                {
                    Ts val = array[i][j];
                    exprs[i][j] = Expression.Constant(val, typeof(Ts));
                }
            }

            for (int i = 0; i < array.Length; i++)
            {
                VerifyGenericWithStructRestrictionArrayList<Ts>(array[i], exprs[i]);
            }
        }

        #endregion

        #region  verifiers

        private static void VerifyBoolArrayList(bool[] val, Expression[] exprs)
        {
            Expression<Func<bool[]>> e =
                Expression.Lambda<Func<bool[]>>(
                    Expression.NewArrayInit(typeof(bool), exprs),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool[]> f = e.Compile();
            bool[] result = f();
            Assert.Equal(val.Length, result.Length);
            for (int i = 0; i < result.Length; i++)
            {
                Assert.Equal(val[i], result[i]);
            }
        }

        private static void VerifyByteArrayList(byte[] val, Expression[] exprs)
        {
            Expression<Func<byte[]>> e =
                Expression.Lambda<Func<byte[]>>(
                    Expression.NewArrayInit(typeof(byte), exprs),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte[]> f = e.Compile();
            byte[] result = f();
            Assert.Equal(val.Length, result.Length);
            for (int i = 0; i < result.Length; i++)
            {
                Assert.Equal(val[i], result[i]);
            }
        }

        private static void VerifyCustomArrayList(C[] val, Expression[] exprs)
        {
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.NewArrayInit(typeof(C), exprs),
                    Enumerable.Empty<ParameterExpression>());
            Func<C[]> f = e.Compile();
            C[] result = f();
            Assert.Equal(val.Length, result.Length);
            for (int i = 0; i < result.Length; i++)
            {
                Assert.Equal(val[i], result[i]);
            }
        }

        private static void VerifyCharArrayList(char[] val, Expression[] exprs)
        {
            Expression<Func<char[]>> e =
                Expression.Lambda<Func<char[]>>(
                    Expression.NewArrayInit(typeof(char), exprs),
                    Enumerable.Empty<ParameterExpression>());
            Func<char[]> f = e.Compile();
            char[] result = f();
            Assert.Equal(val.Length, result.Length);
            for (int i = 0; i < result.Length; i++)
            {
                Assert.Equal(val[i], result[i]);
            }
        }

        private static void VerifyCustom2ArrayList(D[] val, Expression[] exprs)
        {
            Expression<Func<D[]>> e =
                Expression.Lambda<Func<D[]>>(
                    Expression.NewArrayInit(typeof(D), exprs),
                    Enumerable.Empty<ParameterExpression>());
            Func<D[]> f = e.Compile();
            D[] result = f();
            Assert.Equal(val.Length, result.Length);
            for (int i = 0; i < result.Length; i++)
            {
                Assert.Equal(val[i], result[i]);
            }
        }

        private static void VerifyDecimalArrayList(decimal[] val, Expression[] exprs)
        {
            Expression<Func<decimal[]>> e =
                Expression.Lambda<Func<decimal[]>>(
                    Expression.NewArrayInit(typeof(decimal), exprs),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal[]> f = e.Compile();
            decimal[] result = f();
            Assert.Equal(val.Length, result.Length);
            for (int i = 0; i < result.Length; i++)
            {
                Assert.Equal(val[i], result[i]);
            }
        }

        private static void VerifyDelegateArrayList(Delegate[] val, Expression[] exprs)
        {
            Expression<Func<Delegate[]>> e =
                Expression.Lambda<Func<Delegate[]>>(
                    Expression.NewArrayInit(typeof(Delegate), exprs),
                    Enumerable.Empty<ParameterExpression>());
            Func<Delegate[]> f = e.Compile();
            Delegate[] result = f();
            Assert.Equal(val.Length, result.Length);
            for (int i = 0; i < result.Length; i++)
            {
                Assert.Equal(val[i], result[i]);
            }
        }

        private static void VerifyDoubleArrayList(double[] val, Expression[] exprs)
        {
            Expression<Func<double[]>> e =
                Expression.Lambda<Func<double[]>>(
                    Expression.NewArrayInit(typeof(double), exprs),
                    Enumerable.Empty<ParameterExpression>());
            Func<double[]> f = e.Compile();
            double[] result = f();
            Assert.Equal(val.Length, result.Length);
            for (int i = 0; i < result.Length; i++)
            {
                Assert.Equal(val[i], result[i]);
            }
        }

        private static void VerifyEnumArrayList(E[] val, Expression[] exprs)
        {
            Expression<Func<E[]>> e =
                Expression.Lambda<Func<E[]>>(
                    Expression.NewArrayInit(typeof(E), exprs),
                    Enumerable.Empty<ParameterExpression>());
            Func<E[]> f = e.Compile();
            E[] result = f();
            Assert.Equal(val.Length, result.Length);
            for (int i = 0; i < result.Length; i++)
            {
                Assert.Equal(val[i], result[i]);
            }
        }

        private static void VerifyEnumLongArrayList(El[] val, Expression[] exprs)
        {
            Expression<Func<El[]>> e =
                Expression.Lambda<Func<El[]>>(
                    Expression.NewArrayInit(typeof(El), exprs),
                    Enumerable.Empty<ParameterExpression>());
            Func<El[]> f = e.Compile();
            El[] result = f();
            Assert.Equal(val.Length, result.Length);
            for (int i = 0; i < result.Length; i++)
            {
                Assert.Equal(val[i], result[i]);
            }
        }

        private static void VerifyFloatArrayList(float[] val, Expression[] exprs)
        {
            Expression<Func<float[]>> e =
                Expression.Lambda<Func<float[]>>(
                    Expression.NewArrayInit(typeof(float), exprs),
                    Enumerable.Empty<ParameterExpression>());
            Func<float[]> f = e.Compile();
            float[] result = f();
            Assert.Equal(val.Length, result.Length);
            for (int i = 0; i < result.Length; i++)
            {
                Assert.Equal(val[i], result[i]);
            }
        }

        private static void VerifyFuncArrayList(Func<object>[] val, Expression[] exprs)
        {
            Expression<Func<Func<object>[]>> e =
                Expression.Lambda<Func<Func<object>[]>>(
                    Expression.NewArrayInit(typeof(Func<object>), exprs),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<object>[]> f = e.Compile();
            Func<object>[] result = f();
            Assert.Equal(val.Length, result.Length);
            for (int i = 0; i < result.Length; i++)
            {
                Assert.Equal(val[i], result[i]);
            }
        }

        private static void VerifyInterfaceArrayList(I[] val, Expression[] exprs)
        {
            Expression<Func<I[]>> e =
                Expression.Lambda<Func<I[]>>(
                    Expression.NewArrayInit(typeof(I), exprs),
                    Enumerable.Empty<ParameterExpression>());
            Func<I[]> f = e.Compile();
            I[] result = f();
            Assert.Equal(val.Length, result.Length);
            for (int i = 0; i < result.Length; i++)
            {
                Assert.Equal(val[i], result[i]);
            }
        }

        private static void VerifyIEquatableCustomArrayList(IEquatable<C>[] val, Expression[] exprs)
        {
            Expression<Func<IEquatable<C>[]>> e =
                Expression.Lambda<Func<IEquatable<C>[]>>(
                    Expression.NewArrayInit(typeof(IEquatable<C>), exprs),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<C>[]> f = e.Compile();
            IEquatable<C>[] result = f();
            Assert.Equal(val.Length, result.Length);
            for (int i = 0; i < result.Length; i++)
            {
                Assert.Equal(val[i], result[i]);
            }
        }

        private static void VerifyIEquatableCustom2ArrayList(IEquatable<D>[] val, Expression[] exprs)
        {
            Expression<Func<IEquatable<D>[]>> e =
                Expression.Lambda<Func<IEquatable<D>[]>>(
                    Expression.NewArrayInit(typeof(IEquatable<D>), exprs),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<D>[]> f = e.Compile();
            IEquatable<D>[] result = f();
            Assert.Equal(val.Length, result.Length);
            for (int i = 0; i < result.Length; i++)
            {
                Assert.Equal(val[i], result[i]);
            }
        }

        private static void VerifyIntArrayList(int[] val, Expression[] exprs)
        {
            Expression<Func<int[]>> e =
                Expression.Lambda<Func<int[]>>(
                    Expression.NewArrayInit(typeof(int), exprs),
                    Enumerable.Empty<ParameterExpression>());
            Func<int[]> f = e.Compile();
            int[] result = f();
            Assert.Equal(val.Length, result.Length);
            for (int i = 0; i < result.Length; i++)
            {
                Assert.Equal(val[i], result[i]);
            }
        }

        private static void VerifyLongArrayList(long[] val, Expression[] exprs)
        {
            Expression<Func<long[]>> e =
                Expression.Lambda<Func<long[]>>(
                    Expression.NewArrayInit(typeof(long), exprs),
                    Enumerable.Empty<ParameterExpression>());
            Func<long[]> f = e.Compile();
            long[] result = f();
            Assert.Equal(val.Length, result.Length);
            for (int i = 0; i < result.Length; i++)
            {
                Assert.Equal(val[i], result[i]);
            }
        }

        private static void VerifyObjectArrayList(object[] val, Expression[] exprs)
        {
            Expression<Func<object[]>> e =
                Expression.Lambda<Func<object[]>>(
                    Expression.NewArrayInit(typeof(object), exprs),
                    Enumerable.Empty<ParameterExpression>());
            Func<object[]> f = e.Compile();
            object[] result = f();
            Assert.Equal(val.Length, result.Length);
            for (int i = 0; i < result.Length; i++)
            {
                Assert.Equal(val[i], result[i]);
            }
        }

        private static void VerifyStructArrayList(S[] val, Expression[] exprs)
        {
            Expression<Func<S[]>> e =
                Expression.Lambda<Func<S[]>>(
                    Expression.NewArrayInit(typeof(S), exprs),
                    Enumerable.Empty<ParameterExpression>());
            Func<S[]> f = e.Compile();
            S[] result = f();
            Assert.Equal(val.Length, result.Length);
            for (int i = 0; i < result.Length; i++)
            {
                Assert.Equal(val[i], result[i]);
            }
        }

        private static void VerifySByteArrayList(sbyte[] val, Expression[] exprs)
        {
            Expression<Func<sbyte[]>> e =
                Expression.Lambda<Func<sbyte[]>>(
                    Expression.NewArrayInit(typeof(sbyte), exprs),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte[]> f = e.Compile();
            sbyte[] result = f();
            Assert.Equal(val.Length, result.Length);
            for (int i = 0; i < result.Length; i++)
            {
                Assert.Equal(val[i], result[i]);
            }
        }

        private static void VerifyStructWithStringArrayList(Sc[] val, Expression[] exprs)
        {
            Expression<Func<Sc[]>> e =
                Expression.Lambda<Func<Sc[]>>(
                    Expression.NewArrayInit(typeof(Sc), exprs),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sc[]> f = e.Compile();
            Sc[] result = f();
            Assert.Equal(val.Length, result.Length);
            for (int i = 0; i < result.Length; i++)
            {
                Assert.Equal(val[i], result[i]);
            }
        }

        private static void VerifyStructWithStringAndFieldArrayList(Scs[] val, Expression[] exprs)
        {
            Expression<Func<Scs[]>> e =
                Expression.Lambda<Func<Scs[]>>(
                    Expression.NewArrayInit(typeof(Scs), exprs),
                    Enumerable.Empty<ParameterExpression>());
            Func<Scs[]> f = e.Compile();
            Scs[] result = f();
            Assert.Equal(val.Length, result.Length);
            for (int i = 0; i < result.Length; i++)
            {
                Assert.Equal(val[i], result[i]);
            }
        }

        private static void VerifyShortArrayList(short[] val, Expression[] exprs)
        {
            Expression<Func<short[]>> e =
                Expression.Lambda<Func<short[]>>(
                    Expression.NewArrayInit(typeof(short), exprs),
                    Enumerable.Empty<ParameterExpression>());
            Func<short[]> f = e.Compile();
            short[] result = f();
            Assert.Equal(val.Length, result.Length);
            for (int i = 0; i < result.Length; i++)
            {
                Assert.Equal(val[i], result[i]);
            }
        }

        private static void VerifyStructWithTwoValuesArrayList(Sp[] val, Expression[] exprs)
        {
            Expression<Func<Sp[]>> e =
                Expression.Lambda<Func<Sp[]>>(
                    Expression.NewArrayInit(typeof(Sp), exprs),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sp[]> f = e.Compile();
            Sp[] result = f();
            Assert.Equal(val.Length, result.Length);
            for (int i = 0; i < result.Length; i++)
            {
                Assert.Equal(val[i], result[i]);
            }
        }

        private static void VerifyStructWithValueArrayList(Ss[] val, Expression[] exprs)
        {
            Expression<Func<Ss[]>> e =
                Expression.Lambda<Func<Ss[]>>(
                    Expression.NewArrayInit(typeof(Ss), exprs),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ss[]> f = e.Compile();
            Ss[] result = f();
            Assert.Equal(val.Length, result.Length);
            for (int i = 0; i < result.Length; i++)
            {
                Assert.Equal(val[i], result[i]);
            }
        }

        private static void VerifyStringArrayList(string[] val, Expression[] exprs)
        {
            Expression<Func<string[]>> e =
                Expression.Lambda<Func<string[]>>(
                    Expression.NewArrayInit(typeof(string), exprs),
                    Enumerable.Empty<ParameterExpression>());
            Func<string[]> f = e.Compile();
            string[] result = f();
            Assert.Equal(val.Length, result.Length);
            for (int i = 0; i < result.Length; i++)
            {
                Assert.Equal(val[i], result[i]);
            }
        }

        private static void VerifyUIntArrayList(uint[] val, Expression[] exprs)
        {
            Expression<Func<uint[]>> e =
                Expression.Lambda<Func<uint[]>>(
                    Expression.NewArrayInit(typeof(uint), exprs),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint[]> f = e.Compile();
            uint[] result = f();
            Assert.Equal(val.Length, result.Length);
            for (int i = 0; i < result.Length; i++)
            {
                Assert.Equal(val[i], result[i]);
            }
        }

        private static void VerifyULongArrayList(ulong[] val, Expression[] exprs)
        {
            Expression<Func<ulong[]>> e =
                Expression.Lambda<Func<ulong[]>>(
                    Expression.NewArrayInit(typeof(ulong), exprs),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong[]> f = e.Compile();
            ulong[] result = f();
            Assert.Equal(val.Length, result.Length);
            for (int i = 0; i < result.Length; i++)
            {
                Assert.Equal(val[i], result[i]);
            }
        }

        private static void VerifyUShortArrayList(ushort[] val, Expression[] exprs)
        {
            Expression<Func<ushort[]>> e =
                Expression.Lambda<Func<ushort[]>>(
                    Expression.NewArrayInit(typeof(ushort), exprs),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort[]> f = e.Compile();
            ushort[] result = f();
            Assert.Equal(val.Length, result.Length);
            for (int i = 0; i < result.Length; i++)
            {
                Assert.Equal(val[i], result[i]);
            }
        }

        private static void VerifyGenericArrayList<T>(T[] val, Expression[] exprs)
        {
            Expression<Func<T[]>> e =
                Expression.Lambda<Func<T[]>>(
                    Expression.NewArrayInit(typeof(T), exprs),
                    Enumerable.Empty<ParameterExpression>());
            Func<T[]> f = e.Compile();
            T[] result = f();
            Assert.Equal(val.Length, result.Length);
            for (int i = 0; i < result.Length; i++)
            {
                Assert.Equal(val[i], result[i]);
            }
        }

        private static void VerifyGenericWithClassRestrictionArrayList<Tc>(Tc[] val, Expression[] exprs) where Tc : class
        {
            Expression<Func<Tc[]>> e =
                Expression.Lambda<Func<Tc[]>>(
                    Expression.NewArrayInit(typeof(Tc), exprs),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tc[]> f = e.Compile();
            Tc[] result = f();
            Assert.Equal(val.Length, result.Length);
            for (int i = 0; i < result.Length; i++)
            {
                Assert.Equal(val[i], result[i]);
            }
        }

        private static void VerifyGenericWithSubClassRestrictionArrayList<TC>(TC[] val, Expression[] exprs) where TC : C
        {
            Expression<Func<TC[]>> e =
                Expression.Lambda<Func<TC[]>>(
                    Expression.NewArrayInit(typeof(TC), exprs),
                    Enumerable.Empty<ParameterExpression>());
            Func<TC[]> f = e.Compile();
            TC[] result = f();
            Assert.Equal(val.Length, result.Length);
            for (int i = 0; i < result.Length; i++)
            {
                Assert.Equal(val[i], result[i]);
            }
        }

        private static void VerifyGenericWithClassAndNewRestrictionArrayList<Tcn>(Tcn[] val, Expression[] exprs) where Tcn : class, new()
        {
            Expression<Func<Tcn[]>> e =
                Expression.Lambda<Func<Tcn[]>>(
                    Expression.NewArrayInit(typeof(Tcn), exprs),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tcn[]> f = e.Compile();
            Tcn[] result = f();
            Assert.Equal(val.Length, result.Length);
            for (int i = 0; i < result.Length; i++)
            {
                Assert.Equal(val[i], result[i]);
            }
        }

        private static void VerifyGenericWithSubClassAndNewRestrictionArrayList<TCn>(TCn[] val, Expression[] exprs) where TCn : C, new()
        {
            Expression<Func<TCn[]>> e =
                Expression.Lambda<Func<TCn[]>>(
                    Expression.NewArrayInit(typeof(TCn), exprs),
                    Enumerable.Empty<ParameterExpression>());
            Func<TCn[]> f = e.Compile();
            TCn[] result = f();
            Assert.Equal(val.Length, result.Length);
            for (int i = 0; i < result.Length; i++)
            {
                Assert.Equal(val[i], result[i]);
            }
        }

        private static void VerifyGenericWithStructRestrictionArrayList<Ts>(Ts[] val, Expression[] exprs) where Ts : struct
        {
            Expression<Func<Ts[]>> e =
                Expression.Lambda<Func<Ts[]>>(
                    Expression.NewArrayInit(typeof(Ts), exprs),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ts[]> f = e.Compile();
            Ts[] result = f();
            Assert.Equal(val.Length, result.Length);
            for (int i = 0; i < result.Length; i++)
            {
                Assert.Equal(val[i], result[i]);
            }
        }

        #endregion
    }
}
