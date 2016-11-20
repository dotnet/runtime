// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class BinaryShiftTests
    {
        #region Test methods

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckByteShiftTest(bool useInterpreter)
        {
            byte[] array = new byte[] { 0, 1, byte.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyByteShift(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableByteShiftTest(bool useInterpreter)
        {
            byte?[] array = { 0, 1, byte.MaxValue, null };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyNullableByteShift(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckSByteShiftTest(bool useInterpreter)
        {
            sbyte[] array = new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifySByteShift(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableSByteShiftTest(bool useInterpreter)
        {
            sbyte?[] array = { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue, null };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyNullableSByteShift(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUShortShiftTest(bool useInterpreter)
        {
            ushort[] array = new ushort[] { 0, 1, ushort.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyUShortShift(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableUShortShiftTest(bool useInterpreter)
        {
            ushort?[] array = { 0, 1, ushort.MaxValue, null };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyNullableUShortShift(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckShortShiftTest(bool useInterpreter)
        {
            short[] array = new short[] { 0, 1, -1, short.MinValue, short.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyShortShift(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableShortShiftTest(bool useInterpreter)
        {
            short?[] array = { 0, 1, -1, short.MinValue, short.MaxValue, null };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyNullableShortShift(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUIntShiftTest(bool useInterpreter)
        {
            uint[] array = new uint[] { 0, 1, uint.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyUIntShift(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableUIntShiftTest(bool useInterpreter)
        {
            uint?[] array = { 0, 1, uint.MaxValue, null };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyNullableUIntShift(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIntShiftTest(bool useInterpreter)
        {
            int[] array = new int[] { 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyIntShift(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableIntShiftTest(bool useInterpreter)
        {
            int?[] array = { 0, 1, -1, int.MinValue, int.MaxValue, null };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyNullableIntShift(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckULongShiftTest(bool useInterpreter)
        {
            ulong[] array = new ulong[] { 0, 1, ulong.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyULongShift(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableULongShiftTest(bool useInterpreter)
        {
            ulong?[] array = { 0, 1, ulong.MaxValue, null };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyNullableULongShift(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLongShiftTest(bool useInterpreter)
        {
            long[] array = new long[] { 0, 1, -1, long.MinValue, long.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyLongShift(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableLongShiftTest(bool useInterpreter)
        {
            long?[] array = { 0, 1, -1, long.MinValue, long.MaxValue, null };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyNullableLongShift(array[i], useInterpreter);
            }
        }

        #endregion

        #region Test verifiers

        private static int[] s_shifts = new[] { int.MinValue, -1, 0, 1, 2, 31, 32, 63, 64, int.MaxValue };

        private static void VerifyByteShift(byte a, bool useInterpreter)
        {
            foreach (var b in s_shifts)
            {
                VerifyByteShift(a, b, true, useInterpreter);
                VerifyByteShift(a, b, false, useInterpreter);
            }

            VerifyNullShift(a, true, useInterpreter);
            VerifyNullShift(a, false, useInterpreter);
        }

        private static void VerifyByteShift(byte a, int b, bool left, bool useInterpreter)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    left
                    ?
                    Expression.LeftShift(
                        Expression.Constant(a, typeof(byte)),
                        Expression.Constant(b, typeof(int)))
                    :
                    Expression.RightShift(
                        Expression.Constant(a, typeof(byte)),
                        Expression.Constant(b, typeof(int)))
                    );

            Func<byte> f = e.Compile(useInterpreter);

            Assert.Equal((byte)(left ? a << b : a >> b), f());

            Expression<Func<byte?>> en =
                Expression.Lambda<Func<byte?>>(
                    left
                    ?
                    Expression.LeftShift(
                        Expression.Constant(a, typeof(byte)),
                        Expression.Constant(b, typeof(int?)))
                    :
                    Expression.RightShift(
                        Expression.Constant(a, typeof(byte)),
                        Expression.Constant(b, typeof(int?)))
                    );

            Func<byte?> fn = en.Compile(useInterpreter);

            Assert.Equal((byte)(left ? a << b : a >> b), fn());
        }

        private static void VerifyNullableByteShift(byte? a, bool useInterpreter)
        {
            foreach (var b in s_shifts)
            {
                VerifyNullableByteShift(a, b, true, useInterpreter);
                VerifyNullableByteShift(a, b, false, useInterpreter);
            }

            VerifyNullableNullShift(a, true, useInterpreter);
            VerifyNullableNullShift(a, false, useInterpreter);
        }

        private static void VerifyNullableByteShift(byte? a, int b, bool left, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    left
                    ?
                    Expression.LeftShift(
                        Expression.Constant(a, typeof(byte?)),
                        Expression.Constant(b, typeof(int)))
                    :
                    Expression.RightShift(
                        Expression.Constant(a, typeof(byte?)),
                        Expression.Constant(b, typeof(int)))
                    );

            Func<byte?> f = e.Compile(useInterpreter);

            Assert.Equal((byte?)(left ? a << b : a >> b), f());

            e =
                Expression.Lambda<Func<byte?>>(
                    left
                    ?
                    Expression.LeftShift(
                        Expression.Constant(a, typeof(byte?)),
                        Expression.Constant(b, typeof(int?)))
                    :
                    Expression.RightShift(
                        Expression.Constant(a, typeof(byte?)),
                        Expression.Constant(b, typeof(int?)))
                    );

            f = e.Compile(useInterpreter);

            Assert.Equal((byte?)(left ? a << b : a >> b), f());
        }

        private static void VerifySByteShift(sbyte a, bool useInterpreter)
        {
            foreach (var b in s_shifts)
            {
                VerifySByteShift(a, b, true, useInterpreter);
                VerifySByteShift(a, b, false, useInterpreter);
            }

            VerifyNullShift(a, true, useInterpreter);
            VerifyNullShift(a, false, useInterpreter);
        }

        private static void VerifySByteShift(sbyte a, int b, bool left, bool useInterpreter)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    left
                    ?
                    Expression.LeftShift(
                        Expression.Constant(a, typeof(sbyte)),
                        Expression.Constant(b, typeof(int)))
                    :
                    Expression.RightShift(
                        Expression.Constant(a, typeof(sbyte)),
                        Expression.Constant(b, typeof(int)))
                    );

            Func<sbyte> f = e.Compile(useInterpreter);

            Assert.Equal((sbyte)(left ? a << b : a >> b), f());

            Expression<Func<sbyte?>> en =
                Expression.Lambda<Func<sbyte?>>(
                    left
                    ?
                    Expression.LeftShift(
                        Expression.Constant(a, typeof(sbyte)),
                        Expression.Constant(b, typeof(int?)))
                    :
                    Expression.RightShift(
                        Expression.Constant(a, typeof(sbyte)),
                        Expression.Constant(b, typeof(int?)))
                    );

            Func<sbyte?> fn = en.Compile(useInterpreter);

            Assert.Equal((sbyte)(left ? a << b : a >> b), fn());
        }

        private static void VerifyNullableSByteShift(sbyte? a, bool useInterpreter)
        {
            foreach (var b in s_shifts)
            {
                VerifyNullableSByteShift(a, b, true, useInterpreter);
                VerifyNullableSByteShift(a, b, false, useInterpreter);
            }

            VerifyNullableNullShift(a, true, useInterpreter);
            VerifyNullableNullShift(a, false, useInterpreter);
        }

        private static void VerifyNullableSByteShift(sbyte? a, int b, bool left, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    left
                    ?
                    Expression.LeftShift(
                        Expression.Constant(a, typeof(sbyte?)),
                        Expression.Constant(b, typeof(int)))
                    :
                    Expression.RightShift(
                        Expression.Constant(a, typeof(sbyte?)),
                        Expression.Constant(b, typeof(int)))
                    );

            Func<sbyte?> f = e.Compile(useInterpreter);

            Assert.Equal((sbyte?)(left ? a << b : a >> b), f());

            e =
                Expression.Lambda<Func<sbyte?>>(
                    left
                    ?
                    Expression.LeftShift(
                        Expression.Constant(a, typeof(sbyte?)),
                        Expression.Constant(b, typeof(int?)))
                    :
                    Expression.RightShift(
                        Expression.Constant(a, typeof(sbyte?)),
                        Expression.Constant(b, typeof(int?)))
                    );

            f = e.Compile(useInterpreter);

            Assert.Equal((sbyte?)(left ? a << b : a >> b), f());
        }

        private static void VerifyUShortShift(ushort a, bool useInterpreter)
        {
            foreach (var b in s_shifts)
            {
                VerifyUShortShift(a, b, true, useInterpreter);
                VerifyUShortShift(a, b, false, useInterpreter);
            }

            VerifyNullShift(a, true, useInterpreter);
            VerifyNullShift(a, false, useInterpreter);
        }

        private static void VerifyUShortShift(ushort a, int b, bool left, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    left
                    ?
                    Expression.LeftShift(
                        Expression.Constant(a, typeof(ushort)),
                        Expression.Constant(b, typeof(int)))
                    :
                    Expression.RightShift(
                        Expression.Constant(a, typeof(ushort)),
                        Expression.Constant(b, typeof(int)))
                    );

            Func<ushort> f = e.Compile(useInterpreter);

            Assert.Equal((ushort)(left ? a << b : a >> b), f());

            Expression<Func<ushort?>> en =
                Expression.Lambda<Func<ushort?>>(
                    left
                    ?
                    Expression.LeftShift(
                        Expression.Constant(a, typeof(ushort)),
                        Expression.Constant(b, typeof(int?)))
                    :
                    Expression.RightShift(
                        Expression.Constant(a, typeof(ushort)),
                        Expression.Constant(b, typeof(int?)))
                    );

            Func<ushort?> fn = en.Compile(useInterpreter);

            Assert.Equal((ushort)(left ? a << b : a >> b), fn());
        }

        private static void VerifyNullableUShortShift(ushort? a, bool useInterpreter)
        {
            foreach (var b in s_shifts)
            {
                VerifyNullableUShortShift(a, b, true, useInterpreter);
                VerifyNullableUShortShift(a, b, false, useInterpreter);
            }

            VerifyNullableNullShift(a, true, useInterpreter);
            VerifyNullableNullShift(a, false, useInterpreter);
        }

        private static void VerifyNullableUShortShift(ushort? a, int b, bool left, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    left
                    ?
                    Expression.LeftShift(
                        Expression.Constant(a, typeof(ushort?)),
                        Expression.Constant(b, typeof(int)))
                    :
                    Expression.RightShift(
                        Expression.Constant(a, typeof(ushort?)),
                        Expression.Constant(b, typeof(int)))
                    );

            Func<ushort?> f = e.Compile(useInterpreter);

            Assert.Equal((ushort?)(left ? a << b : a >> b), f());

            e =
                Expression.Lambda<Func<ushort?>>(
                    left
                    ?
                    Expression.LeftShift(
                        Expression.Constant(a, typeof(ushort?)),
                        Expression.Constant(b, typeof(int?)))
                    :
                    Expression.RightShift(
                        Expression.Constant(a, typeof(ushort?)),
                        Expression.Constant(b, typeof(int?)))
                    );

            f = e.Compile(useInterpreter);

            Assert.Equal((ushort?)(left ? a << b : a >> b), f());
        }

        private static void VerifyShortShift(short a, bool useInterpreter)
        {
            foreach (var b in s_shifts)
            {
                VerifyShortShift(a, b, true, useInterpreter);
                VerifyShortShift(a, b, false, useInterpreter);
            }

            VerifyNullShift(a, true, useInterpreter);
            VerifyNullShift(a, false, useInterpreter);
        }

        private static void VerifyShortShift(short a, int b, bool left, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    left
                    ?
                    Expression.LeftShift(
                        Expression.Constant(a, typeof(short)),
                        Expression.Constant(b, typeof(int)))
                    :
                    Expression.RightShift(
                        Expression.Constant(a, typeof(short)),
                        Expression.Constant(b, typeof(int)))
                    );

            Func<short> f = e.Compile(useInterpreter);

            Assert.Equal((short)(left ? a << b : a >> b), f());

            Expression<Func<short?>> en =
                Expression.Lambda<Func<short?>>(
                    left
                    ?
                    Expression.LeftShift(
                        Expression.Constant(a, typeof(short)),
                        Expression.Constant(b, typeof(int?)))
                    :
                    Expression.RightShift(
                        Expression.Constant(a, typeof(short)),
                        Expression.Constant(b, typeof(int?)))
                    );

            Func<short?> fn = en.Compile(useInterpreter);

            Assert.Equal((short)(left ? a << b : a >> b), fn());
        }

        private static void VerifyNullableShortShift(short? a, bool useInterpreter)
        {
            foreach (var b in s_shifts)
            {
                VerifyNullableShortShift(a, b, true, useInterpreter);
                VerifyNullableShortShift(a, b, false, useInterpreter);
            }

            VerifyNullableNullShift(a, true, useInterpreter);
            VerifyNullableNullShift(a, false, useInterpreter);
        }

        private static void VerifyNullableShortShift(short? a, int b, bool left, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    left
                    ?
                    Expression.LeftShift(
                        Expression.Constant(a, typeof(short?)),
                        Expression.Constant(b, typeof(int)))
                    :
                    Expression.RightShift(
                        Expression.Constant(a, typeof(short?)),
                        Expression.Constant(b, typeof(int)))
                    );

            Func<short?> f = e.Compile(useInterpreter);

            Assert.Equal((short?)(left ? a << b : a >> b), f());

            e =
                Expression.Lambda<Func<short?>>(
                    left
                    ?
                    Expression.LeftShift(
                        Expression.Constant(a, typeof(short?)),
                        Expression.Constant(b, typeof(int?)))
                    :
                    Expression.RightShift(
                        Expression.Constant(a, typeof(short?)),
                        Expression.Constant(b, typeof(int?)))
                    );

            f = e.Compile(useInterpreter);

            Assert.Equal((short?)(left ? a << b : a >> b), f());
        }

        private static void VerifyUIntShift(uint a, bool useInterpreter)
        {
            foreach (var b in s_shifts)
            {
                VerifyUIntShift(a, b, true, useInterpreter);
                VerifyUIntShift(a, b, false, useInterpreter);
            }

            VerifyNullShift(a, true, useInterpreter);
            VerifyNullShift(a, false, useInterpreter);
        }

        private static void VerifyUIntShift(uint a, int b, bool left, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    left
                    ?
                    Expression.LeftShift(
                        Expression.Constant(a, typeof(uint)),
                        Expression.Constant(b, typeof(int)))
                    :
                    Expression.RightShift(
                        Expression.Constant(a, typeof(uint)),
                        Expression.Constant(b, typeof(int)))
                    );

            Func<uint> f = e.Compile(useInterpreter);

            Assert.Equal(left ? a << b : a >> b, f());

            Expression<Func<uint?>> en =
                Expression.Lambda<Func<uint?>>(
                    left
                    ?
                    Expression.LeftShift(
                        Expression.Constant(a, typeof(uint)),
                        Expression.Constant(b, typeof(int?)))
                    :
                    Expression.RightShift(
                        Expression.Constant(a, typeof(uint)),
                        Expression.Constant(b, typeof(int?)))
                    );

            Func<uint?> fn = en.Compile(useInterpreter);

            Assert.Equal(left ? a << b : a >> b, fn());
        }

        private static void VerifyNullableUIntShift(uint? a, bool useInterpreter)
        {
            foreach (var b in s_shifts)
            {
                VerifyNullableUIntShift(a, b, true, useInterpreter);
                VerifyNullableUIntShift(a, b, false, useInterpreter);
            }

            VerifyNullableNullShift(a, true, useInterpreter);
            VerifyNullableNullShift(a, false, useInterpreter);
        }

        private static void VerifyNullableUIntShift(uint? a, int b, bool left, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    left
                    ?
                    Expression.LeftShift(
                        Expression.Constant(a, typeof(uint?)),
                        Expression.Constant(b, typeof(int)))
                    :
                    Expression.RightShift(
                        Expression.Constant(a, typeof(uint?)),
                        Expression.Constant(b, typeof(int)))
                    );

            Func<uint?> f = e.Compile(useInterpreter);

            Assert.Equal(left ? a << b : a >> b, f());

            e =
                Expression.Lambda<Func<uint?>>(
                    left
                    ?
                    Expression.LeftShift(
                        Expression.Constant(a, typeof(uint?)),
                        Expression.Constant(b, typeof(int?)))
                    :
                    Expression.RightShift(
                        Expression.Constant(a, typeof(uint?)),
                        Expression.Constant(b, typeof(int?)))
                    );

            f = e.Compile(useInterpreter);

            Assert.Equal(left ? a << b : a >> b, f());
        }

        private static void VerifyIntShift(int a, bool useInterpreter)
        {
            foreach (var b in s_shifts)
            {
                VerifyIntShift(a, b, true, useInterpreter);
                VerifyIntShift(a, b, false, useInterpreter);
            }

            VerifyNullShift(a, true, useInterpreter);
            VerifyNullShift(a, false, useInterpreter);
        }

        private static void VerifyIntShift(int a, int b, bool left, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    left
                    ?
                    Expression.LeftShift(
                        Expression.Constant(a, typeof(int)),
                        Expression.Constant(b, typeof(int)))
                    :
                    Expression.RightShift(
                        Expression.Constant(a, typeof(int)),
                        Expression.Constant(b, typeof(int)))
                    );

            Func<int> f = e.Compile(useInterpreter);

            Assert.Equal(left ? a << b : a >> b, f());

            Expression<Func<int?>> en =
                Expression.Lambda<Func<int?>>(
                    left
                    ?
                    Expression.LeftShift(
                        Expression.Constant(a, typeof(int)),
                        Expression.Constant(b, typeof(int?)))
                    :
                    Expression.RightShift(
                        Expression.Constant(a, typeof(int)),
                        Expression.Constant(b, typeof(int?)))
                    );

            Func<int?> fn = en.Compile(useInterpreter);

            Assert.Equal(left ? a << b : a >> b, fn());
        }

        private static void VerifyNullableIntShift(int? a, bool useInterpreter)
        {
            foreach (var b in s_shifts)
            {
                VerifyNullableIntShift(a, b, true, useInterpreter);
                VerifyNullableIntShift(a, b, false, useInterpreter);
            }

            VerifyNullableNullShift(a, true, useInterpreter);
            VerifyNullableNullShift(a, false, useInterpreter);
        }

        private static void VerifyNullableIntShift(int? a, int b, bool left, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    left
                    ?
                    Expression.LeftShift(
                        Expression.Constant(a, typeof(int?)),
                        Expression.Constant(b, typeof(int)))
                    :
                    Expression.RightShift(
                        Expression.Constant(a, typeof(int?)),
                        Expression.Constant(b, typeof(int)))
                    );

            Func<int?> f = e.Compile(useInterpreter);

            Assert.Equal(left ? a << b : a >> b, f());

            e =
                Expression.Lambda<Func<int?>>(
                    left
                    ?
                    Expression.LeftShift(
                        Expression.Constant(a, typeof(int?)),
                        Expression.Constant(b, typeof(int?)))
                    :
                    Expression.RightShift(
                        Expression.Constant(a, typeof(int?)),
                        Expression.Constant(b, typeof(int?)))
                    );

            f = e.Compile(useInterpreter);

            Assert.Equal(left ? a << b : a >> b, f());
        }

        private static void VerifyULongShift(ulong a, bool useInterpreter)
        {
            foreach (var b in s_shifts)
            {
                VerifyULongShift(a, b, true, useInterpreter);
                VerifyULongShift(a, b, false, useInterpreter);
            }

            VerifyNullShift(a, true, useInterpreter);
            VerifyNullShift(a, false, useInterpreter);
        }

        private static void VerifyULongShift(ulong a, int b, bool left, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    left
                    ?
                    Expression.LeftShift(
                        Expression.Constant(a, typeof(ulong)),
                        Expression.Constant(b, typeof(int)))
                    :
                    Expression.RightShift(
                        Expression.Constant(a, typeof(ulong)),
                        Expression.Constant(b, typeof(int)))
                    );

            Func<ulong> f = e.Compile(useInterpreter);

            Assert.Equal(left ? a << b : a >> b, f());

            Expression<Func<ulong?>> en =
                Expression.Lambda<Func<ulong?>>(
                    left
                    ?
                    Expression.LeftShift(
                        Expression.Constant(a, typeof(ulong)),
                        Expression.Constant(b, typeof(int?)))
                    :
                    Expression.RightShift(
                        Expression.Constant(a, typeof(ulong)),
                        Expression.Constant(b, typeof(int?)))
                    );

            Func<ulong?> fn = en.Compile(useInterpreter);

            Assert.Equal(left ? a << b : a >> b, fn());
        }

        private static void VerifyNullableULongShift(ulong? a, bool useInterpreter)
        {
            foreach (var b in s_shifts)
            {
                VerifyNullableULongShift(a, b, true, useInterpreter);
                VerifyNullableULongShift(a, b, false, useInterpreter);
            }

            VerifyNullableNullShift(a, true, useInterpreter);
            VerifyNullableNullShift(a, false, useInterpreter);
        }

        private static void VerifyNullableULongShift(ulong? a, int b, bool left, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    left
                    ?
                    Expression.LeftShift(
                        Expression.Constant(a, typeof(ulong?)),
                        Expression.Constant(b, typeof(int)))
                    :
                    Expression.RightShift(
                        Expression.Constant(a, typeof(ulong?)),
                        Expression.Constant(b, typeof(int)))
                    );

            Func<ulong?> f = e.Compile(useInterpreter);

            Assert.Equal(left ? a << b : a >> b, f());

            e =
                Expression.Lambda<Func<ulong?>>(
                    left
                    ?
                    Expression.LeftShift(
                        Expression.Constant(a, typeof(ulong?)),
                        Expression.Constant(b, typeof(int?)))
                    :
                    Expression.RightShift(
                        Expression.Constant(a, typeof(ulong?)),
                        Expression.Constant(b, typeof(int?)))
                    );

            f = e.Compile(useInterpreter);

            Assert.Equal(left ? a << b : a >> b, f());
        }

        private static void VerifyLongShift(long a, bool useInterpreter)
        {
            foreach (var b in s_shifts)
            {
                VerifyLongShift(a, b, true, useInterpreter);
                VerifyLongShift(a, b, false, useInterpreter);
            }

            VerifyNullShift(a, true, useInterpreter);
            VerifyNullShift(a, false, useInterpreter);
        }

        private static void VerifyLongShift(long a, int b, bool left, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    left
                    ?
                    Expression.LeftShift(
                        Expression.Constant(a, typeof(long)),
                        Expression.Constant(b, typeof(int)))
                    :
                    Expression.RightShift(
                        Expression.Constant(a, typeof(long)),
                        Expression.Constant(b, typeof(int)))
                    );

            Func<long> f = e.Compile(useInterpreter);

            Assert.Equal(left ? a << b : a >> b, f());

            Expression<Func<long?>> en =
                Expression.Lambda<Func<long?>>(
                    left
                    ?
                    Expression.LeftShift(
                        Expression.Constant(a, typeof(long)),
                        Expression.Constant(b, typeof(int?)))
                    :
                    Expression.RightShift(
                        Expression.Constant(a, typeof(long)),
                        Expression.Constant(b, typeof(int?)))
                    );

            Func<long?> fn = en.Compile(useInterpreter);

            Assert.Equal(left ? a << b : a >> b, fn());
        }

        private static void VerifyNullableLongShift(long? a, bool useInterpreter)
        {
            foreach (var b in s_shifts)
            {
                VerifyNullableLongShift(a, b, true, useInterpreter);
                VerifyNullableLongShift(a, b, false, useInterpreter);
            }

            VerifyNullableNullShift(a, true, useInterpreter);
            VerifyNullableNullShift(a, false, useInterpreter);
        }

        private static void VerifyNullableLongShift(long? a, int b, bool left, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    left
                    ?
                    Expression.LeftShift(
                        Expression.Constant(a, typeof(long?)),
                        Expression.Constant(b, typeof(int)))
                    :
                    Expression.RightShift(
                        Expression.Constant(a, typeof(long?)),
                        Expression.Constant(b, typeof(int)))
                    );

            Func<long?> f = e.Compile(useInterpreter);

            Assert.Equal(left ? a << b : a >> b, f());

            e =
                Expression.Lambda<Func<long?>>(
                    left
                    ?
                    Expression.LeftShift(
                        Expression.Constant(a, typeof(long?)),
                        Expression.Constant(b, typeof(int?)))
                    :
                    Expression.RightShift(
                        Expression.Constant(a, typeof(long?)),
                        Expression.Constant(b, typeof(int?)))
                    );

            f = e.Compile(useInterpreter);

            Assert.Equal(left ? a << b : a >> b, f());
        }

        private static void VerifyNullShift<T>(T a, bool left, bool useInterpreter) where T : struct
        {
            Expression<Func<T?>> e =
                Expression.Lambda<Func<T?>>(
                    left
                    ?
                    Expression.LeftShift(
                        Expression.Constant(a, typeof(T)),
                        Expression.Default(typeof(int?)))
                    :
                    Expression.RightShift(
                        Expression.Constant(a, typeof(T)),
                        Expression.Default(typeof(int?)))
                    );

            Func<T?> f = e.Compile(useInterpreter);

            Assert.Null(f());
        }

        private static void VerifyNullableNullShift<T>(T a, bool left, bool useInterpreter)
        {
            Expression<Func<T>> e =
                Expression.Lambda<Func<T>>(
                    left
                    ?
                    Expression.LeftShift(
                        Expression.Constant(a, typeof(T)),
                        Expression.Default(typeof(int?)))
                    :
                    Expression.RightShift(
                        Expression.Constant(a, typeof(T)),
                        Expression.Default(typeof(int?)))
                    );

            Func<T> f = e.Compile(useInterpreter);

            Assert.Null(f());
        }

        #endregion

        [Fact]
        public static void CannotReduceLeft()
        {
            Expression exp = Expression.LeftShift(Expression.Constant(0), Expression.Constant(0));
            Assert.False(exp.CanReduce);
            Assert.Same(exp, exp.Reduce());
            Assert.Throws<ArgumentException>(null, () => exp.ReduceAndCheck());
        }

        [Fact]
        public static void CannotReduceRight()
        {
            Expression exp = Expression.RightShift(Expression.Constant(0), Expression.Constant(0));
            Assert.False(exp.CanReduce);
            Assert.Same(exp, exp.Reduce());
            Assert.Throws<ArgumentException>(null, () => exp.ReduceAndCheck());
        }

        [Fact]
        public static void LeftThrowsOnLeftNull()
        {
            Assert.Throws<ArgumentNullException>("left", () => Expression.LeftShift(null, Expression.Constant("")));
        }

        [Fact]
        public static void LeftThrowsOnRightNull()
        {
            Assert.Throws<ArgumentNullException>("right", () => Expression.LeftShift(Expression.Constant(""), null));
        }

        [Fact]
        public static void RightThrowsOnLeftNull()
        {
            Assert.Throws<ArgumentNullException>("left", () => Expression.RightShift(null, Expression.Constant("")));
        }

        [Fact]
        public static void RightThrowsOnRightNull()
        {
            Assert.Throws<ArgumentNullException>("right", () => Expression.RightShift(Expression.Constant(""), null));
        }

        private static class Unreadable<T>
        {
            public static T WriteOnly
            {
                set { }
            }
        }

        [Fact]
        public static void LeftThrowsOnLeftUnreadable()
        {
            Expression value = Expression.Property(null, typeof(Unreadable<int>), "WriteOnly");
            Assert.Throws<ArgumentException>("left", () => Expression.LeftShift(value, Expression.Constant(1)));
        }

        [Fact]
        public static void LeftThrowsOnRightUnreadable()
        {
            Expression value = Expression.Property(null, typeof(Unreadable<int>), "WriteOnly");
            Assert.Throws<ArgumentException>("right", () => Expression.LeftShift(Expression.Constant(1), value));
        }

        [Fact]
        public static void RightThrowsOnLeftUnreadable()
        {
            Expression value = Expression.Property(null, typeof(Unreadable<int>), "WriteOnly");
            Assert.Throws<ArgumentException>("left", () => Expression.RightShift(value, Expression.Constant(1)));
        }

        [Fact]
        public static void RightThrowsOnRightUnreadable()
        {
            Expression value = Expression.Property(null, typeof(Unreadable<int>), "WriteOnly");
            Assert.Throws<ArgumentException>("right", () => Expression.RightShift(Expression.Constant(1), value));
        }

        [Fact]
        public static void ToStringTest()
        {
            var e1 = Expression.LeftShift(Expression.Parameter(typeof(int), "a"), Expression.Parameter(typeof(int), "b"));
            Assert.Equal("(a << b)", e1.ToString());

            var e2 = Expression.RightShift(Expression.Parameter(typeof(int), "a"), Expression.Parameter(typeof(int), "b"));
            Assert.Equal("(a >> b)", e2.ToString());
        }

        [Theory, InlineData(typeof(E)), InlineData(typeof(El)), InlineData(typeof(string))]
        public static void IncorrectLHSTypes(Type type)
        {
            var lhs = Expression.Default(type);
            var rhs = Expression.Constant(0);
            Assert.Throws<InvalidOperationException>(() => Expression.LeftShift(lhs, rhs));
            Assert.Throws<InvalidOperationException>(() => Expression.RightShift(lhs, rhs));
        }

        [Theory, InlineData(typeof(E)), InlineData(typeof(El)), InlineData(typeof(string)), InlineData(typeof(long)),
         InlineData(typeof(short)), InlineData(typeof(uint))]
        public static void IncorrectRHSTypes(Type type)
        {
            var lhs = Expression.Constant(0);
            var rhs = Expression.Default(type);
            Assert.Throws<InvalidOperationException>(() => Expression.LeftShift(lhs, rhs));
            Assert.Throws<InvalidOperationException>(() => Expression.RightShift(lhs, rhs));
        }
    }
}
