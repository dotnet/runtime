// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

namespace Microsoft.VisualBasic.CompilerServices.Tests
{
    public partial class OperatorsTests
    {
        public static IEnumerable<object[]> Compare_Primitives_TestData()
        {
            // byte.
            yield return new object[] { (byte)8, (byte)7, true, false, false };
            yield return new object[] { (byte)8, (byte)8, false, true, false };
            yield return new object[] { (byte)8, (byte)9, false, false, true };
            yield return new object[] { (byte)8, (ByteEnum)7, true, false, false };
            yield return new object[] { (byte)8, (ByteEnum)8, false, true, false };
            yield return new object[] { (byte)8, (ByteEnum)9, false, false, true };
            yield return new object[] { (byte)8, (sbyte)7, true, false, false };
            yield return new object[] { (byte)8, (sbyte)8, false, true, false };
            yield return new object[] { (byte)8, (sbyte)9, false, false, true };
            yield return new object[] { (byte)8, (SByteEnum)7, true, false, false };
            yield return new object[] { (byte)8, (SByteEnum)8, false, true, false };
            yield return new object[] { (byte)8, (SByteEnum)9, false, false, true };
            yield return new object[] { (byte)8, (ushort)7, true, false, false };
            yield return new object[] { (byte)8, (ushort)8, false, true, false };
            yield return new object[] { (byte)8, (ushort)9, false, false, true };
            yield return new object[] { (byte)8, (UShortEnum)7, true, false, false };
            yield return new object[] { (byte)8, (UShortEnum)8, false, true, false };
            yield return new object[] { (byte)8, (UShortEnum)9, false, false, true };
            yield return new object[] { (byte)8, (short)7, true, false, false };
            yield return new object[] { (byte)8, (short)8, false, true, false };
            yield return new object[] { (byte)8, (short)9, false, false, true };
            yield return new object[] { (byte)8, (ShortEnum)7, true, false, false };
            yield return new object[] { (byte)8, (ShortEnum)8, false, true, false };
            yield return new object[] { (byte)8, (ShortEnum)9, false, false, true };
            yield return new object[] { (byte)8, (uint)7, true, false, false };
            yield return new object[] { (byte)8, (uint)8, false, true, false };
            yield return new object[] { (byte)8, (uint)9, false, false, true };
            yield return new object[] { (byte)8, (UIntEnum)7, true, false, false };
            yield return new object[] { (byte)8, (UIntEnum)8, false, true, false };
            yield return new object[] { (byte)8, (UIntEnum)9, false, false, true };
            yield return new object[] { (byte)8, 7, true, false, false };
            yield return new object[] { (byte)8, 8, false, true, false };
            yield return new object[] { (byte)8, 9, false, false, true };
            yield return new object[] { (byte)8, (IntEnum)7, true, false, false };
            yield return new object[] { (byte)8, (IntEnum)8, false, true, false };
            yield return new object[] { (byte)8, (IntEnum)9, false, false, true };
            yield return new object[] { (byte)8, (ulong)7, true, false, false };
            yield return new object[] { (byte)8, (ulong)8, false, true, false };
            yield return new object[] { (byte)8, (ulong)9, false, false, true };
            yield return new object[] { (byte)8, (ULongEnum)7, true, false, false };
            yield return new object[] { (byte)8, (ULongEnum)8, false, true, false };
            yield return new object[] { (byte)8, (ULongEnum)9, false, false, true };
            yield return new object[] { (byte)8, (long)7, true, false, false };
            yield return new object[] { (byte)8, (long)8, false, true, false };
            yield return new object[] { (byte)8, (long)9, false, false, true };
            yield return new object[] { (byte)8, (LongEnum)7, true, false, false };
            yield return new object[] { (byte)8, (LongEnum)8, false, true, false };
            yield return new object[] { (byte)8, (LongEnum)9, false, false, true };
            yield return new object[] { (byte)8, (float)7, true, false, false };
            yield return new object[] { (byte)8, (float)8, false, true, false };
            yield return new object[] { (byte)8, (float)9, false, false, true };
            yield return new object[] { (byte)8, float.PositiveInfinity, false, false, true };
            yield return new object[] { (byte)8, float.NegativeInfinity, true, false, false };
            yield return new object[] { (byte)8, float.NaN, false, false, false };
            yield return new object[] { (byte)8, (double)7, true, false, false };
            yield return new object[] { (byte)8, (double)8, false, true, false };
            yield return new object[] { (byte)8, (double)9, false, false, true };
            yield return new object[] { (byte)8, double.PositiveInfinity, false, false, true };
            yield return new object[] { (byte)8, double.NegativeInfinity, true, false, false };
            yield return new object[] { (byte)8, double.NaN, false, false, false };
            yield return new object[] { (byte)8, (decimal)7, true, false, false };
            yield return new object[] { (byte)8, (decimal)8, false, true, false };
            yield return new object[] { (byte)8, (decimal)9, false, false, true };
            yield return new object[] { (byte)8, "7", true, false, false };
            yield return new object[] { (byte)8, "8", false, true, false };
            yield return new object[] { (byte)8, "9", false, false, true };
            yield return new object[] { (byte)8, true, true, false, false };
            yield return new object[] { (byte)8, false, true, false, false };
            yield return new object[] { (byte)8, null, true, false, false };

            // sbyte.
            yield return new object[] { (sbyte)8, (byte)7, true, false, false };
            yield return new object[] { (sbyte)8, (byte)8, false, true, false };
            yield return new object[] { (sbyte)8, (byte)9, false, false, true };
            yield return new object[] { (sbyte)8, (ByteEnum)7, true, false, false };
            yield return new object[] { (sbyte)8, (ByteEnum)8, false, true, false };
            yield return new object[] { (sbyte)8, (ByteEnum)9, false, false, true };
            yield return new object[] { (sbyte)8, (sbyte)7, true, false, false };
            yield return new object[] { (sbyte)8, (sbyte)8, false, true, false };
            yield return new object[] { (sbyte)8, (sbyte)9, false, false, true };
            yield return new object[] { (sbyte)8, (SByteEnum)7, true, false, false };
            yield return new object[] { (sbyte)8, (SByteEnum)8, false, true, false };
            yield return new object[] { (sbyte)8, (SByteEnum)9, false, false, true };
            yield return new object[] { (sbyte)8, (ushort)7, true, false, false };
            yield return new object[] { (sbyte)8, (ushort)8, false, true, false };
            yield return new object[] { (sbyte)8, (ushort)9, false, false, true };
            yield return new object[] { (sbyte)8, (UShortEnum)7, true, false, false };
            yield return new object[] { (sbyte)8, (UShortEnum)8, false, true, false };
            yield return new object[] { (sbyte)8, (UShortEnum)9, false, false, true };
            yield return new object[] { (sbyte)8, (short)7, true, false, false };
            yield return new object[] { (sbyte)8, (short)8, false, true, false };
            yield return new object[] { (sbyte)8, (short)9, false, false, true };
            yield return new object[] { (sbyte)8, (ShortEnum)7, true, false, false };
            yield return new object[] { (sbyte)8, (ShortEnum)8, false, true, false };
            yield return new object[] { (sbyte)8, (ShortEnum)9, false, false, true };
            yield return new object[] { (sbyte)8, (uint)7, true, false, false };
            yield return new object[] { (sbyte)8, (uint)8, false, true, false };
            yield return new object[] { (sbyte)8, (uint)9, false, false, true };
            yield return new object[] { (sbyte)8, (UIntEnum)7, true, false, false };
            yield return new object[] { (sbyte)8, (UIntEnum)8, false, true, false };
            yield return new object[] { (sbyte)8, (UIntEnum)9, false, false, true };
            yield return new object[] { (sbyte)8, 7, true, false, false };
            yield return new object[] { (sbyte)8, 8, false, true, false };
            yield return new object[] { (sbyte)8, 9, false, false, true };
            yield return new object[] { (sbyte)8, (IntEnum)7, true, false, false };
            yield return new object[] { (sbyte)8, (IntEnum)8, false, true, false };
            yield return new object[] { (sbyte)8, (IntEnum)9, false, false, true };
            yield return new object[] { (sbyte)8, (ulong)7, true, false, false };
            yield return new object[] { (sbyte)8, (ulong)8, false, true, false };
            yield return new object[] { (sbyte)8, (ulong)9, false, false, true };
            yield return new object[] { (sbyte)8, (ULongEnum)7, true, false, false };
            yield return new object[] { (sbyte)8, (ULongEnum)8, false, true, false };
            yield return new object[] { (sbyte)8, (ULongEnum)9, false, false, true };
            yield return new object[] { (sbyte)8, (long)7, true, false, false };
            yield return new object[] { (sbyte)8, (long)8, false, true, false };
            yield return new object[] { (sbyte)8, (long)9, false, false, true };
            yield return new object[] { (sbyte)8, (LongEnum)7, true, false, false };
            yield return new object[] { (sbyte)8, (LongEnum)8, false, true, false };
            yield return new object[] { (sbyte)8, (LongEnum)9, false, false, true };
            yield return new object[] { (sbyte)8, (float)7, true, false, false };
            yield return new object[] { (sbyte)8, (float)8, false, true, false };
            yield return new object[] { (sbyte)8, (float)9, false, false, true };
            yield return new object[] { (sbyte)8, float.PositiveInfinity, false, false, true };
            yield return new object[] { (sbyte)8, float.NegativeInfinity, true, false, false };
            yield return new object[] { (sbyte)8, float.NaN, false, false, false };
            yield return new object[] { (sbyte)8, (double)7, true, false, false };
            yield return new object[] { (sbyte)8, (double)8, false, true, false };
            yield return new object[] { (sbyte)8, (double)9, false, false, true };
            yield return new object[] { (sbyte)8, double.PositiveInfinity, false, false, true };
            yield return new object[] { (sbyte)8, double.NegativeInfinity, true, false, false };
            yield return new object[] { (sbyte)8, double.NaN, false, false, false };
            yield return new object[] { (sbyte)8, (decimal)7, true, false, false };
            yield return new object[] { (sbyte)8, (decimal)8, false, true, false };
            yield return new object[] { (sbyte)8, (decimal)9, false, false, true };
            yield return new object[] { (sbyte)8, "7", true, false, false };
            yield return new object[] { (sbyte)8, "8", false, true, false };
            yield return new object[] { (sbyte)8, "9", false, false, true };
            yield return new object[] { (sbyte)8, true, true, false, false };
            yield return new object[] { (sbyte)8, false, true, false, false };
            yield return new object[] { (sbyte)8, null, true, false, false };

            // ushort.
            yield return new object[] { (ushort)8, (byte)7, true, false, false };
            yield return new object[] { (ushort)8, (byte)8, false, true, false };
            yield return new object[] { (ushort)8, (byte)9, false, false, true };
            yield return new object[] { (ushort)8, (ByteEnum)7, true, false, false };
            yield return new object[] { (ushort)8, (ByteEnum)8, false, true, false };
            yield return new object[] { (ushort)8, (ByteEnum)9, false, false, true };
            yield return new object[] { (ushort)8, (sbyte)7, true, false, false };
            yield return new object[] { (ushort)8, (sbyte)8, false, true, false };
            yield return new object[] { (ushort)8, (sbyte)9, false, false, true };
            yield return new object[] { (ushort)8, (SByteEnum)7, true, false, false };
            yield return new object[] { (ushort)8, (SByteEnum)8, false, true, false };
            yield return new object[] { (ushort)8, (SByteEnum)9, false, false, true };
            yield return new object[] { (ushort)8, (ushort)7, true, false, false };
            yield return new object[] { (ushort)8, (ushort)8, false, true, false };
            yield return new object[] { (ushort)8, (ushort)9, false, false, true };
            yield return new object[] { (ushort)8, (UShortEnum)7, true, false, false };
            yield return new object[] { (ushort)8, (UShortEnum)8, false, true, false };
            yield return new object[] { (ushort)8, (UShortEnum)9, false, false, true };
            yield return new object[] { (ushort)8, (short)7, true, false, false };
            yield return new object[] { (ushort)8, (short)8, false, true, false };
            yield return new object[] { (ushort)8, (short)9, false, false, true };
            yield return new object[] { (ushort)8, (ShortEnum)7, true, false, false };
            yield return new object[] { (ushort)8, (ShortEnum)8, false, true, false };
            yield return new object[] { (ushort)8, (ShortEnum)9, false, false, true };
            yield return new object[] { (ushort)8, (uint)7, true, false, false };
            yield return new object[] { (ushort)8, (uint)8, false, true, false };
            yield return new object[] { (ushort)8, (uint)9, false, false, true };
            yield return new object[] { (ushort)8, (UIntEnum)7, true, false, false };
            yield return new object[] { (ushort)8, (UIntEnum)8, false, true, false };
            yield return new object[] { (ushort)8, (UIntEnum)9, false, false, true };
            yield return new object[] { (ushort)8, 7, true, false, false };
            yield return new object[] { (ushort)8, 8, false, true, false };
            yield return new object[] { (ushort)8, 9, false, false, true };
            yield return new object[] { (ushort)8, (IntEnum)7, true, false, false };
            yield return new object[] { (ushort)8, (IntEnum)8, false, true, false };
            yield return new object[] { (ushort)8, (IntEnum)9, false, false, true };
            yield return new object[] { (ushort)8, (ulong)7, true, false, false };
            yield return new object[] { (ushort)8, (ulong)8, false, true, false };
            yield return new object[] { (ushort)8, (ulong)9, false, false, true };
            yield return new object[] { (ushort)8, (ULongEnum)7, true, false, false };
            yield return new object[] { (ushort)8, (ULongEnum)8, false, true, false };
            yield return new object[] { (ushort)8, (ULongEnum)9, false, false, true };
            yield return new object[] { (ushort)8, (long)7, true, false, false };
            yield return new object[] { (ushort)8, (long)8, false, true, false };
            yield return new object[] { (ushort)8, (long)9, false, false, true };
            yield return new object[] { (ushort)8, (LongEnum)7, true, false, false };
            yield return new object[] { (ushort)8, (LongEnum)8, false, true, false };
            yield return new object[] { (ushort)8, (LongEnum)9, false, false, true };
            yield return new object[] { (ushort)8, (float)7, true, false, false };
            yield return new object[] { (ushort)8, (float)8, false, true, false };
            yield return new object[] { (ushort)8, (float)9, false, false, true };
            yield return new object[] { (ushort)8, float.PositiveInfinity, false, false, true };
            yield return new object[] { (ushort)8, float.NegativeInfinity, true, false, false };
            yield return new object[] { (ushort)8, float.NaN, false, false, false };
            yield return new object[] { (ushort)8, (double)7, true, false, false };
            yield return new object[] { (ushort)8, (double)8, false, true, false };
            yield return new object[] { (ushort)8, (double)9, false, false, true };
            yield return new object[] { (ushort)8, double.PositiveInfinity, false, false, true };
            yield return new object[] { (ushort)8, double.NegativeInfinity, true, false, false };
            yield return new object[] { (ushort)8, double.NaN, false, false, false };
            yield return new object[] { (ushort)8, (decimal)7, true, false, false };
            yield return new object[] { (ushort)8, (decimal)8, false, true, false };
            yield return new object[] { (ushort)8, (decimal)9, false, false, true };
            yield return new object[] { (ushort)8, "7", true, false, false };
            yield return new object[] { (ushort)8, "8", false, true, false };
            yield return new object[] { (ushort)8, "9", false, false, true };
            yield return new object[] { (ushort)8, true, true, false, false };
            yield return new object[] { (ushort)8, false, true, false, false };
            yield return new object[] { (ushort)8, null, true, false, false };

            // short.
            yield return new object[] { (short)8, (byte)7, true, false, false };
            yield return new object[] { (short)8, (byte)8, false, true, false };
            yield return new object[] { (short)8, (byte)9, false, false, true };
            yield return new object[] { (short)8, (ByteEnum)7, true, false, false };
            yield return new object[] { (short)8, (ByteEnum)8, false, true, false };
            yield return new object[] { (short)8, (ByteEnum)9, false, false, true };
            yield return new object[] { (short)8, (sbyte)7, true, false, false };
            yield return new object[] { (short)8, (sbyte)8, false, true, false };
            yield return new object[] { (short)8, (sbyte)9, false, false, true };
            yield return new object[] { (short)8, (SByteEnum)7, true, false, false };
            yield return new object[] { (short)8, (SByteEnum)8, false, true, false };
            yield return new object[] { (short)8, (SByteEnum)9, false, false, true };
            yield return new object[] { (short)8, (ushort)7, true, false, false };
            yield return new object[] { (short)8, (ushort)8, false, true, false };
            yield return new object[] { (short)8, (ushort)9, false, false, true };
            yield return new object[] { (short)8, (UShortEnum)7, true, false, false };
            yield return new object[] { (short)8, (UShortEnum)8, false, true, false };
            yield return new object[] { (short)8, (UShortEnum)9, false, false, true };
            yield return new object[] { (short)8, (short)7, true, false, false };
            yield return new object[] { (short)8, (short)8, false, true, false };
            yield return new object[] { (short)8, (short)9, false, false, true };
            yield return new object[] { (short)8, (ShortEnum)7, true, false, false };
            yield return new object[] { (short)8, (ShortEnum)8, false, true, false };
            yield return new object[] { (short)8, (ShortEnum)9, false, false, true };
            yield return new object[] { (short)8, (uint)7, true, false, false };
            yield return new object[] { (short)8, (uint)8, false, true, false };
            yield return new object[] { (short)8, (uint)9, false, false, true };
            yield return new object[] { (short)8, (UIntEnum)7, true, false, false };
            yield return new object[] { (short)8, (UIntEnum)8, false, true, false };
            yield return new object[] { (short)8, (UIntEnum)9, false, false, true };
            yield return new object[] { (short)8, 7, true, false, false };
            yield return new object[] { (short)8, 8, false, true, false };
            yield return new object[] { (short)8, 9, false, false, true };
            yield return new object[] { (short)8, (IntEnum)7, true, false, false };
            yield return new object[] { (short)8, (IntEnum)8, false, true, false };
            yield return new object[] { (short)8, (IntEnum)9, false, false, true };
            yield return new object[] { (short)8, (ulong)7, true, false, false };
            yield return new object[] { (short)8, (ulong)8, false, true, false };
            yield return new object[] { (short)8, (ulong)9, false, false, true };
            yield return new object[] { (short)8, (ULongEnum)7, true, false, false };
            yield return new object[] { (short)8, (ULongEnum)8, false, true, false };
            yield return new object[] { (short)8, (ULongEnum)9, false, false, true };
            yield return new object[] { (short)8, (long)7, true, false, false };
            yield return new object[] { (short)8, (long)8, false, true, false };
            yield return new object[] { (short)8, (long)9, false, false, true };
            yield return new object[] { (short)8, (LongEnum)7, true, false, false };
            yield return new object[] { (short)8, (LongEnum)8, false, true, false };
            yield return new object[] { (short)8, (LongEnum)9, false, false, true };
            yield return new object[] { (short)8, (float)7, true, false, false };
            yield return new object[] { (short)8, (float)8, false, true, false };
            yield return new object[] { (short)8, (float)9, false, false, true };
            yield return new object[] { (short)8, float.PositiveInfinity, false, false, true };
            yield return new object[] { (short)8, float.NegativeInfinity, true, false, false };
            yield return new object[] { (short)8, float.NaN, false, false, false };
            yield return new object[] { (short)8, (double)7, true, false, false };
            yield return new object[] { (short)8, (double)8, false, true, false };
            yield return new object[] { (short)8, (double)9, false, false, true };
            yield return new object[] { (short)8, double.PositiveInfinity, false, false, true };
            yield return new object[] { (short)8, double.NegativeInfinity, true, false, false };
            yield return new object[] { (short)8, double.NaN, false, false, false };
            yield return new object[] { (short)8, (decimal)7, true, false, false };
            yield return new object[] { (short)8, (decimal)8, false, true, false };
            yield return new object[] { (short)8, (decimal)9, false, false, true };
            yield return new object[] { (short)8, "7", true, false, false };
            yield return new object[] { (short)8, "8", false, true, false };
            yield return new object[] { (short)8, "9", false, false, true };
            yield return new object[] { (short)8, true, true, false, false };
            yield return new object[] { (short)8, false, true, false, false };
            yield return new object[] { (short)8, null, true, false, false };

            // uint.
            yield return new object[] { (uint)8, (byte)7, true, false, false };
            yield return new object[] { (uint)8, (byte)8, false, true, false };
            yield return new object[] { (uint)8, (byte)9, false, false, true };
            yield return new object[] { (uint)8, (ByteEnum)7, true, false, false };
            yield return new object[] { (uint)8, (ByteEnum)8, false, true, false };
            yield return new object[] { (uint)8, (ByteEnum)9, false, false, true };
            yield return new object[] { (uint)8, (sbyte)7, true, false, false };
            yield return new object[] { (uint)8, (sbyte)8, false, true, false };
            yield return new object[] { (uint)8, (sbyte)9, false, false, true };
            yield return new object[] { (uint)8, (SByteEnum)7, true, false, false };
            yield return new object[] { (uint)8, (SByteEnum)8, false, true, false };
            yield return new object[] { (uint)8, (SByteEnum)9, false, false, true };
            yield return new object[] { (uint)8, (ushort)7, true, false, false };
            yield return new object[] { (uint)8, (ushort)8, false, true, false };
            yield return new object[] { (uint)8, (ushort)9, false, false, true };
            yield return new object[] { (uint)8, (UShortEnum)7, true, false, false };
            yield return new object[] { (uint)8, (UShortEnum)8, false, true, false };
            yield return new object[] { (uint)8, (UShortEnum)9, false, false, true };
            yield return new object[] { (uint)8, (short)7, true, false, false };
            yield return new object[] { (uint)8, (short)8, false, true, false };
            yield return new object[] { (uint)8, (short)9, false, false, true };
            yield return new object[] { (uint)8, (ShortEnum)7, true, false, false };
            yield return new object[] { (uint)8, (ShortEnum)8, false, true, false };
            yield return new object[] { (uint)8, (ShortEnum)9, false, false, true };
            yield return new object[] { (uint)8, (uint)7, true, false, false };
            yield return new object[] { (uint)8, (uint)8, false, true, false };
            yield return new object[] { (uint)8, (uint)9, false, false, true };
            yield return new object[] { (uint)8, (UIntEnum)7, true, false, false };
            yield return new object[] { (uint)8, (UIntEnum)8, false, true, false };
            yield return new object[] { (uint)8, (UIntEnum)9, false, false, true };
            yield return new object[] { (uint)8, 7, true, false, false };
            yield return new object[] { (uint)8, 8, false, true, false };
            yield return new object[] { (uint)8, 9, false, false, true };
            yield return new object[] { (uint)8, (IntEnum)7, true, false, false };
            yield return new object[] { (uint)8, (IntEnum)8, false, true, false };
            yield return new object[] { (uint)8, (IntEnum)9, false, false, true };
            yield return new object[] { (uint)8, (ulong)7, true, false, false };
            yield return new object[] { (uint)8, (ulong)8, false, true, false };
            yield return new object[] { (uint)8, (ulong)9, false, false, true };
            yield return new object[] { (uint)8, (ULongEnum)7, true, false, false };
            yield return new object[] { (uint)8, (ULongEnum)8, false, true, false };
            yield return new object[] { (uint)8, (ULongEnum)9, false, false, true };
            yield return new object[] { (uint)8, (long)7, true, false, false };
            yield return new object[] { (uint)8, (long)8, false, true, false };
            yield return new object[] { (uint)8, (long)9, false, false, true };
            yield return new object[] { (uint)8, (LongEnum)7, true, false, false };
            yield return new object[] { (uint)8, (LongEnum)8, false, true, false };
            yield return new object[] { (uint)8, (LongEnum)9, false, false, true };
            yield return new object[] { (uint)8, (float)7, true, false, false };
            yield return new object[] { (uint)8, (float)8, false, true, false };
            yield return new object[] { (uint)8, (float)9, false, false, true };
            yield return new object[] { (uint)8, float.PositiveInfinity, false, false, true };
            yield return new object[] { (uint)8, float.NegativeInfinity, true, false, false };
            yield return new object[] { (uint)8, float.NaN, false, false, false };
            yield return new object[] { (uint)8, (double)7, true, false, false };
            yield return new object[] { (uint)8, (double)8, false, true, false };
            yield return new object[] { (uint)8, (double)9, false, false, true };
            yield return new object[] { (uint)8, double.PositiveInfinity, false, false, true };
            yield return new object[] { (uint)8, double.NegativeInfinity, true, false, false };
            yield return new object[] { (uint)8, double.NaN, false, false, false };
            yield return new object[] { (uint)8, (decimal)7, true, false, false };
            yield return new object[] { (uint)8, (decimal)8, false, true, false };
            yield return new object[] { (uint)8, (decimal)9, false, false, true };
            yield return new object[] { (uint)8, "7", true, false, false };
            yield return new object[] { (uint)8, "8", false, true, false };
            yield return new object[] { (uint)8, "9", false, false, true };
            yield return new object[] { (uint)8, true, true, false, false };
            yield return new object[] { (uint)8, false, true, false, false };
            yield return new object[] { (uint)8, null, true, false, false };

            // int.
            yield return new object[] { 8, (byte)7, true, false, false };
            yield return new object[] { 8, (byte)8, false, true, false };
            yield return new object[] { 8, (byte)9, false, false, true };
            yield return new object[] { 8, (ByteEnum)7, true, false, false };
            yield return new object[] { 8, (ByteEnum)8, false, true, false };
            yield return new object[] { 8, (ByteEnum)9, false, false, true };
            yield return new object[] { 8, (sbyte)7, true, false, false };
            yield return new object[] { 8, (sbyte)8, false, true, false };
            yield return new object[] { 8, (sbyte)9, false, false, true };
            yield return new object[] { 8, (SByteEnum)7, true, false, false };
            yield return new object[] { 8, (SByteEnum)8, false, true, false };
            yield return new object[] { 8, (SByteEnum)9, false, false, true };
            yield return new object[] { 8, (ushort)7, true, false, false };
            yield return new object[] { 8, (ushort)8, false, true, false };
            yield return new object[] { 8, (ushort)9, false, false, true };
            yield return new object[] { 8, (UShortEnum)7, true, false, false };
            yield return new object[] { 8, (UShortEnum)8, false, true, false };
            yield return new object[] { 8, (UShortEnum)9, false, false, true };
            yield return new object[] { 8, (short)7, true, false, false };
            yield return new object[] { 8, (short)8, false, true, false };
            yield return new object[] { 8, (short)9, false, false, true };
            yield return new object[] { 8, (ShortEnum)7, true, false, false };
            yield return new object[] { 8, (ShortEnum)8, false, true, false };
            yield return new object[] { 8, (ShortEnum)9, false, false, true };
            yield return new object[] { 8, (uint)7, true, false, false };
            yield return new object[] { 8, (uint)8, false, true, false };
            yield return new object[] { 8, (uint)9, false, false, true };
            yield return new object[] { 8, (UIntEnum)7, true, false, false };
            yield return new object[] { 8, (UIntEnum)8, false, true, false };
            yield return new object[] { 8, (UIntEnum)9, false, false, true };
            yield return new object[] { 8, 7, true, false, false };
            yield return new object[] { 8, 8, false, true, false };
            yield return new object[] { 8, 9, false, false, true };
            yield return new object[] { 8, (IntEnum)7, true, false, false };
            yield return new object[] { 8, (IntEnum)8, false, true, false };
            yield return new object[] { 8, (IntEnum)9, false, false, true };
            yield return new object[] { 8, (ulong)7, true, false, false };
            yield return new object[] { 8, (ulong)8, false, true, false };
            yield return new object[] { 8, (ulong)9, false, false, true };
            yield return new object[] { 8, (ULongEnum)7, true, false, false };
            yield return new object[] { 8, (ULongEnum)8, false, true, false };
            yield return new object[] { 8, (ULongEnum)9, false, false, true };
            yield return new object[] { 8, (long)7, true, false, false };
            yield return new object[] { 8, (long)8, false, true, false };
            yield return new object[] { 8, (long)9, false, false, true };
            yield return new object[] { 8, (LongEnum)7, true, false, false };
            yield return new object[] { 8, (LongEnum)8, false, true, false };
            yield return new object[] { 8, (LongEnum)9, false, false, true };
            yield return new object[] { 8, (float)7, true, false, false };
            yield return new object[] { 8, (float)8, false, true, false };
            yield return new object[] { 8, (float)9, false, false, true };
            yield return new object[] { 8, float.PositiveInfinity, false, false, true };
            yield return new object[] { 8, float.NegativeInfinity, true, false, false };
            yield return new object[] { 8, float.NaN, false, false, false };
            yield return new object[] { 8, (double)7, true, false, false };
            yield return new object[] { 8, (double)8, false, true, false };
            yield return new object[] { 8, (double)9, false, false, true };
            yield return new object[] { 8, double.PositiveInfinity, false, false, true };
            yield return new object[] { 8, double.NegativeInfinity, true, false, false };
            yield return new object[] { 8, double.NaN, false, false, false };
            yield return new object[] { 8, (decimal)7, true, false, false };
            yield return new object[] { 8, (decimal)8, false, true, false };
            yield return new object[] { 8, (decimal)9, false, false, true };
            yield return new object[] { 8, "7", true, false, false };
            yield return new object[] { 8, "8", false, true, false };
            yield return new object[] { 8, "9", false, false, true };
            yield return new object[] { 8, true, true, false, false };
            yield return new object[] { 8, false, true, false, false };
            yield return new object[] { 8, null, true, false, false };

            // ulong.
            yield return new object[] { (ulong)8, (byte)7, true, false, false };
            yield return new object[] { (ulong)8, (byte)8, false, true, false };
            yield return new object[] { (ulong)8, (byte)9, false, false, true };
            yield return new object[] { (ulong)8, (ByteEnum)7, true, false, false };
            yield return new object[] { (ulong)8, (ByteEnum)8, false, true, false };
            yield return new object[] { (ulong)8, (ByteEnum)9, false, false, true };
            yield return new object[] { (ulong)8, (sbyte)7, true, false, false };
            yield return new object[] { (ulong)8, (sbyte)8, false, true, false };
            yield return new object[] { (ulong)8, (sbyte)9, false, false, true };
            yield return new object[] { (ulong)8, (SByteEnum)7, true, false, false };
            yield return new object[] { (ulong)8, (SByteEnum)8, false, true, false };
            yield return new object[] { (ulong)8, (SByteEnum)9, false, false, true };
            yield return new object[] { (ulong)8, (ushort)7, true, false, false };
            yield return new object[] { (ulong)8, (ushort)8, false, true, false };
            yield return new object[] { (ulong)8, (ushort)9, false, false, true };
            yield return new object[] { (ulong)8, (UShortEnum)7, true, false, false };
            yield return new object[] { (ulong)8, (UShortEnum)8, false, true, false };
            yield return new object[] { (ulong)8, (UShortEnum)9, false, false, true };
            yield return new object[] { (ulong)8, (short)7, true, false, false };
            yield return new object[] { (ulong)8, (short)8, false, true, false };
            yield return new object[] { (ulong)8, (short)9, false, false, true };
            yield return new object[] { (ulong)8, (ShortEnum)7, true, false, false };
            yield return new object[] { (ulong)8, (ShortEnum)8, false, true, false };
            yield return new object[] { (ulong)8, (ShortEnum)9, false, false, true };
            yield return new object[] { (ulong)8, (uint)7, true, false, false };
            yield return new object[] { (ulong)8, (uint)8, false, true, false };
            yield return new object[] { (ulong)8, (uint)9, false, false, true };
            yield return new object[] { (ulong)8, (UIntEnum)7, true, false, false };
            yield return new object[] { (ulong)8, (UIntEnum)8, false, true, false };
            yield return new object[] { (ulong)8, (UIntEnum)9, false, false, true };
            yield return new object[] { (ulong)8, 7, true, false, false };
            yield return new object[] { (ulong)8, 8, false, true, false };
            yield return new object[] { (ulong)8, 9, false, false, true };
            yield return new object[] { (ulong)8, (IntEnum)7, true, false, false };
            yield return new object[] { (ulong)8, (IntEnum)8, false, true, false };
            yield return new object[] { (ulong)8, (IntEnum)9, false, false, true };
            yield return new object[] { (ulong)8, (ulong)7, true, false, false };
            yield return new object[] { (ulong)8, (ulong)8, false, true, false };
            yield return new object[] { (ulong)8, (ulong)9, false, false, true };
            yield return new object[] { (ulong)8, (ULongEnum)7, true, false, false };
            yield return new object[] { (ulong)8, (ULongEnum)8, false, true, false };
            yield return new object[] { (ulong)8, (ULongEnum)9, false, false, true };
            yield return new object[] { (ulong)8, (long)7, true, false, false };
            yield return new object[] { (ulong)8, (long)8, false, true, false };
            yield return new object[] { (ulong)8, (long)9, false, false, true };
            yield return new object[] { (ulong)8, (LongEnum)7, true, false, false };
            yield return new object[] { (ulong)8, (LongEnum)8, false, true, false };
            yield return new object[] { (ulong)8, (LongEnum)9, false, false, true };
            yield return new object[] { (ulong)8, (float)7, true, false, false };
            yield return new object[] { (ulong)8, (float)8, false, true, false };
            yield return new object[] { (ulong)8, (float)9, false, false, true };
            yield return new object[] { (ulong)8, float.PositiveInfinity, false, false, true };
            yield return new object[] { (ulong)8, float.NegativeInfinity, true, false, false };
            yield return new object[] { (ulong)8, float.NaN, false, false, false };
            yield return new object[] { (ulong)8, (double)7, true, false, false };
            yield return new object[] { (ulong)8, (double)8, false, true, false };
            yield return new object[] { (ulong)8, (double)9, false, false, true };
            yield return new object[] { (ulong)8, double.PositiveInfinity, false, false, true };
            yield return new object[] { (ulong)8, double.NegativeInfinity, true, false, false };
            yield return new object[] { (ulong)8, double.NaN, false, false, false };
            yield return new object[] { (ulong)8, (decimal)7, true, false, false };
            yield return new object[] { (ulong)8, (decimal)8, false, true, false };
            yield return new object[] { (ulong)8, (decimal)9, false, false, true };
            yield return new object[] { (ulong)8, "7", true, false, false };
            yield return new object[] { (ulong)8, "8", false, true, false };
            yield return new object[] { (ulong)8, "9", false, false, true };
            yield return new object[] { (ulong)8, true, true, false, false };
            yield return new object[] { (ulong)8, false, true, false, false };
            yield return new object[] { (ulong)8, null, true, false, false };

            // long.
            yield return new object[] { (long)8, (byte)7, true, false, false };
            yield return new object[] { (long)8, (byte)8, false, true, false };
            yield return new object[] { (long)8, (byte)9, false, false, true };
            yield return new object[] { (long)8, (ByteEnum)7, true, false, false };
            yield return new object[] { (long)8, (ByteEnum)8, false, true, false };
            yield return new object[] { (long)8, (ByteEnum)9, false, false, true };
            yield return new object[] { (long)8, (sbyte)7, true, false, false };
            yield return new object[] { (long)8, (sbyte)8, false, true, false };
            yield return new object[] { (long)8, (sbyte)9, false, false, true };
            yield return new object[] { (long)8, (SByteEnum)7, true, false, false };
            yield return new object[] { (long)8, (SByteEnum)8, false, true, false };
            yield return new object[] { (long)8, (SByteEnum)9, false, false, true };
            yield return new object[] { (long)8, (ushort)7, true, false, false };
            yield return new object[] { (long)8, (ushort)8, false, true, false };
            yield return new object[] { (long)8, (ushort)9, false, false, true };
            yield return new object[] { (long)8, (UShortEnum)7, true, false, false };
            yield return new object[] { (long)8, (UShortEnum)8, false, true, false };
            yield return new object[] { (long)8, (UShortEnum)9, false, false, true };
            yield return new object[] { (long)8, (short)7, true, false, false };
            yield return new object[] { (long)8, (short)8, false, true, false };
            yield return new object[] { (long)8, (short)9, false, false, true };
            yield return new object[] { (long)8, (ShortEnum)7, true, false, false };
            yield return new object[] { (long)8, (ShortEnum)8, false, true, false };
            yield return new object[] { (long)8, (ShortEnum)9, false, false, true };
            yield return new object[] { (long)8, (uint)7, true, false, false };
            yield return new object[] { (long)8, (uint)8, false, true, false };
            yield return new object[] { (long)8, (uint)9, false, false, true };
            yield return new object[] { (long)8, (UIntEnum)7, true, false, false };
            yield return new object[] { (long)8, (UIntEnum)8, false, true, false };
            yield return new object[] { (long)8, (UIntEnum)9, false, false, true };
            yield return new object[] { (long)8, 7, true, false, false };
            yield return new object[] { (long)8, 8, false, true, false };
            yield return new object[] { (long)8, 9, false, false, true };
            yield return new object[] { (long)8, (IntEnum)7, true, false, false };
            yield return new object[] { (long)8, (IntEnum)8, false, true, false };
            yield return new object[] { (long)8, (IntEnum)9, false, false, true };
            yield return new object[] { (long)8, (ulong)7, true, false, false };
            yield return new object[] { (long)8, (ulong)8, false, true, false };
            yield return new object[] { (long)8, (ulong)9, false, false, true };
            yield return new object[] { (long)8, (ULongEnum)7, true, false, false };
            yield return new object[] { (long)8, (ULongEnum)8, false, true, false };
            yield return new object[] { (long)8, (ULongEnum)9, false, false, true };
            yield return new object[] { (long)8, (long)7, true, false, false };
            yield return new object[] { (long)8, (long)8, false, true, false };
            yield return new object[] { (long)8, (long)9, false, false, true };
            yield return new object[] { (long)8, (LongEnum)7, true, false, false };
            yield return new object[] { (long)8, (LongEnum)8, false, true, false };
            yield return new object[] { (long)8, (LongEnum)9, false, false, true };
            yield return new object[] { (long)8, (float)7, true, false, false };
            yield return new object[] { (long)8, (float)8, false, true, false };
            yield return new object[] { (long)8, (float)9, false, false, true };
            yield return new object[] { (long)8, float.PositiveInfinity, false, false, true };
            yield return new object[] { (long)8, float.NegativeInfinity, true, false, false };
            yield return new object[] { (long)8, float.NaN, false, false, false };
            yield return new object[] { (long)8, (double)7, true, false, false };
            yield return new object[] { (long)8, (double)8, false, true, false };
            yield return new object[] { (long)8, (double)9, false, false, true };
            yield return new object[] { (long)8, double.PositiveInfinity, false, false, true };
            yield return new object[] { (long)8, double.NegativeInfinity, true, false, false };
            yield return new object[] { (long)8, double.NaN, false, false, false };
            yield return new object[] { (long)8, (decimal)7, true, false, false };
            yield return new object[] { (long)8, (decimal)8, false, true, false };
            yield return new object[] { (long)8, (decimal)9, false, false, true };
            yield return new object[] { (long)8, "7", true, false, false };
            yield return new object[] { (long)8, "8", false, true, false };
            yield return new object[] { (long)8, "9", false, false, true };
            yield return new object[] { (long)8, true, true, false, false };
            yield return new object[] { (long)8, false, true, false, false };
            yield return new object[] { (long)8, null, true, false, false };

            // float.
            yield return new object[] { (float)8, (byte)7, true, false, false };
            yield return new object[] { (float)8, (byte)8, false, true, false };
            yield return new object[] { (float)8, (byte)9, false, false, true };
            yield return new object[] { (float)8, (ByteEnum)7, true, false, false };
            yield return new object[] { (float)8, (ByteEnum)8, false, true, false };
            yield return new object[] { (float)8, (ByteEnum)9, false, false, true };
            yield return new object[] { (float)8, (sbyte)7, true, false, false };
            yield return new object[] { (float)8, (sbyte)8, false, true, false };
            yield return new object[] { (float)8, (sbyte)9, false, false, true };
            yield return new object[] { (float)8, (SByteEnum)7, true, false, false };
            yield return new object[] { (float)8, (SByteEnum)8, false, true, false };
            yield return new object[] { (float)8, (SByteEnum)9, false, false, true };
            yield return new object[] { (float)8, (ushort)7, true, false, false };
            yield return new object[] { (float)8, (ushort)8, false, true, false };
            yield return new object[] { (float)8, (ushort)9, false, false, true };
            yield return new object[] { (float)8, (UShortEnum)7, true, false, false };
            yield return new object[] { (float)8, (UShortEnum)8, false, true, false };
            yield return new object[] { (float)8, (UShortEnum)9, false, false, true };
            yield return new object[] { (float)8, (short)7, true, false, false };
            yield return new object[] { (float)8, (short)8, false, true, false };
            yield return new object[] { (float)8, (short)9, false, false, true };
            yield return new object[] { (float)8, (ShortEnum)7, true, false, false };
            yield return new object[] { (float)8, (ShortEnum)8, false, true, false };
            yield return new object[] { (float)8, (ShortEnum)9, false, false, true };
            yield return new object[] { (float)8, (uint)7, true, false, false };
            yield return new object[] { (float)8, (uint)8, false, true, false };
            yield return new object[] { (float)8, (uint)9, false, false, true };
            yield return new object[] { (float)8, (UIntEnum)7, true, false, false };
            yield return new object[] { (float)8, (UIntEnum)8, false, true, false };
            yield return new object[] { (float)8, (UIntEnum)9, false, false, true };
            yield return new object[] { (float)8, 7, true, false, false };
            yield return new object[] { (float)8, 8, false, true, false };
            yield return new object[] { (float)8, 9, false, false, true };
            yield return new object[] { (float)8, (IntEnum)7, true, false, false };
            yield return new object[] { (float)8, (IntEnum)8, false, true, false };
            yield return new object[] { (float)8, (IntEnum)9, false, false, true };
            yield return new object[] { (float)8, (ulong)7, true, false, false };
            yield return new object[] { (float)8, (ulong)8, false, true, false };
            yield return new object[] { (float)8, (ulong)9, false, false, true };
            yield return new object[] { (float)8, (ULongEnum)7, true, false, false };
            yield return new object[] { (float)8, (ULongEnum)8, false, true, false };
            yield return new object[] { (float)8, (ULongEnum)9, false, false, true };
            yield return new object[] { (float)8, (long)7, true, false, false };
            yield return new object[] { (float)8, (long)8, false, true, false };
            yield return new object[] { (float)8, (long)9, false, false, true };
            yield return new object[] { (float)8, (LongEnum)7, true, false, false };
            yield return new object[] { (float)8, (LongEnum)8, false, true, false };
            yield return new object[] { (float)8, (LongEnum)9, false, false, true };
            yield return new object[] { (float)8, (float)7, true, false, false };
            yield return new object[] { (float)8, (float)8, false, true, false };
            yield return new object[] { (float)8, (float)9, false, false, true };
            yield return new object[] { (float)8, float.PositiveInfinity, false, false, true };
            yield return new object[] { (float)8, float.NegativeInfinity, true, false, false };
            yield return new object[] { (float)8, float.NaN, false, false, false };
            yield return new object[] { (float)8, (double)7, true, false, false };
            yield return new object[] { (float)8, (double)8, false, true, false };
            yield return new object[] { (float)8, (double)9, false, false, true };
            yield return new object[] { (float)8, double.PositiveInfinity, false, false, true };
            yield return new object[] { (float)8, double.NegativeInfinity, true, false, false };
            yield return new object[] { (float)8, double.NaN, false, false, false };
            yield return new object[] { (float)8, (decimal)7, true, false, false };
            yield return new object[] { (float)8, (decimal)8, false, true, false };
            yield return new object[] { (float)8, (decimal)9, false, false, true };
            yield return new object[] { (float)8, "7", true, false, false };
            yield return new object[] { (float)8, "8", false, true, false };
            yield return new object[] { (float)8, "9", false, false, true };
            yield return new object[] { (float)8, true, true, false, false };
            yield return new object[] { (float)8, false, true, false, false };
            yield return new object[] { (float)8, null, true, false, false };
            yield return new object[] { (float)8, float.NaN, false, false, false };

            // float.
            yield return new object[] { float.NaN, (float)8, false, false, false };
            yield return new object[] { float.NaN, float.NaN, false, false, false };

            // double.
            yield return new object[] { (double)8, (byte)7, true, false, false };
            yield return new object[] { (double)8, (byte)8, false, true, false };
            yield return new object[] { (double)8, (byte)9, false, false, true };
            yield return new object[] { (double)8, (ByteEnum)7, true, false, false };
            yield return new object[] { (double)8, (ByteEnum)8, false, true, false };
            yield return new object[] { (double)8, (ByteEnum)9, false, false, true };
            yield return new object[] { (double)8, (sbyte)7, true, false, false };
            yield return new object[] { (double)8, (sbyte)8, false, true, false };
            yield return new object[] { (double)8, (sbyte)9, false, false, true };
            yield return new object[] { (double)8, (SByteEnum)7, true, false, false };
            yield return new object[] { (double)8, (SByteEnum)8, false, true, false };
            yield return new object[] { (double)8, (SByteEnum)9, false, false, true };
            yield return new object[] { (double)8, (ushort)7, true, false, false };
            yield return new object[] { (double)8, (ushort)8, false, true, false };
            yield return new object[] { (double)8, (ushort)9, false, false, true };
            yield return new object[] { (double)8, (UShortEnum)7, true, false, false };
            yield return new object[] { (double)8, (UShortEnum)8, false, true, false };
            yield return new object[] { (double)8, (UShortEnum)9, false, false, true };
            yield return new object[] { (double)8, (short)7, true, false, false };
            yield return new object[] { (double)8, (short)8, false, true, false };
            yield return new object[] { (double)8, (short)9, false, false, true };
            yield return new object[] { (double)8, (ShortEnum)7, true, false, false };
            yield return new object[] { (double)8, (ShortEnum)8, false, true, false };
            yield return new object[] { (double)8, (ShortEnum)9, false, false, true };
            yield return new object[] { (double)8, (uint)7, true, false, false };
            yield return new object[] { (double)8, (uint)8, false, true, false };
            yield return new object[] { (double)8, (uint)9, false, false, true };
            yield return new object[] { (double)8, (UIntEnum)7, true, false, false };
            yield return new object[] { (double)8, (UIntEnum)8, false, true, false };
            yield return new object[] { (double)8, (UIntEnum)9, false, false, true };
            yield return new object[] { (double)8, 7, true, false, false };
            yield return new object[] { (double)8, 8, false, true, false };
            yield return new object[] { (double)8, 9, false, false, true };
            yield return new object[] { (double)8, (IntEnum)7, true, false, false };
            yield return new object[] { (double)8, (IntEnum)8, false, true, false };
            yield return new object[] { (double)8, (IntEnum)9, false, false, true };
            yield return new object[] { (double)8, (ulong)7, true, false, false };
            yield return new object[] { (double)8, (ulong)8, false, true, false };
            yield return new object[] { (double)8, (ulong)9, false, false, true };
            yield return new object[] { (double)8, (ULongEnum)7, true, false, false };
            yield return new object[] { (double)8, (ULongEnum)8, false, true, false };
            yield return new object[] { (double)8, (ULongEnum)9, false, false, true };
            yield return new object[] { (double)8, (long)7, true, false, false };
            yield return new object[] { (double)8, (long)8, false, true, false };
            yield return new object[] { (double)8, (long)9, false, false, true };
            yield return new object[] { (double)8, (LongEnum)7, true, false, false };
            yield return new object[] { (double)8, (LongEnum)8, false, true, false };
            yield return new object[] { (double)8, (LongEnum)9, false, false, true };
            yield return new object[] { (double)8, (float)7, true, false, false };
            yield return new object[] { (double)8, (float)8, false, true, false };
            yield return new object[] { (double)8, (float)9, false, false, true };
            yield return new object[] { (double)8, float.PositiveInfinity, false, false, true };
            yield return new object[] { (double)8, float.NegativeInfinity, true, false, false };
            yield return new object[] { (double)8, float.NaN, false, false, false };
            yield return new object[] { (double)8, (double)7, true, false, false };
            yield return new object[] { (double)8, (double)8, false, true, false };
            yield return new object[] { (double)8, (double)9, false, false, true };
            yield return new object[] { (double)8, double.PositiveInfinity, false, false, true };
            yield return new object[] { (double)8, double.NegativeInfinity, true, false, false };
            yield return new object[] { (double)8, double.NaN, false, false, false };
            yield return new object[] { (double)8, (decimal)7, true, false, false };
            yield return new object[] { (double)8, (decimal)8, false, true, false };
            yield return new object[] { (double)8, (decimal)9, false, false, true };
            yield return new object[] { (double)8, "7", true, false, false };
            yield return new object[] { (double)8, "8", false, true, false };
            yield return new object[] { (double)8, "9", false, false, true };
            yield return new object[] { (double)8, true, true, false, false };
            yield return new object[] { (double)8, false, true, false, false };
            yield return new object[] { (double)8, null, true, false, false };
            yield return new object[] { (double)8, double.NaN, false, false, false };

            // double.
            yield return new object[] { double.NaN, (double)8, false, false, false };
            yield return new object[] { double.NaN, double.NaN, false, false, false };

            // decimal.
            yield return new object[] { (decimal)8, (byte)7, true, false, false };
            yield return new object[] { (decimal)8, (byte)8, false, true, false };
            yield return new object[] { (decimal)8, (byte)9, false, false, true };
            yield return new object[] { (decimal)8, (ByteEnum)7, true, false, false };
            yield return new object[] { (decimal)8, (ByteEnum)8, false, true, false };
            yield return new object[] { (decimal)8, (ByteEnum)9, false, false, true };
            yield return new object[] { (decimal)8, (sbyte)7, true, false, false };
            yield return new object[] { (decimal)8, (sbyte)8, false, true, false };
            yield return new object[] { (decimal)8, (sbyte)9, false, false, true };
            yield return new object[] { (decimal)8, (SByteEnum)7, true, false, false };
            yield return new object[] { (decimal)8, (SByteEnum)8, false, true, false };
            yield return new object[] { (decimal)8, (SByteEnum)9, false, false, true };
            yield return new object[] { (decimal)8, (ushort)7, true, false, false };
            yield return new object[] { (decimal)8, (ushort)8, false, true, false };
            yield return new object[] { (decimal)8, (ushort)9, false, false, true };
            yield return new object[] { (decimal)8, (UShortEnum)7, true, false, false };
            yield return new object[] { (decimal)8, (UShortEnum)8, false, true, false };
            yield return new object[] { (decimal)8, (UShortEnum)9, false, false, true };
            yield return new object[] { (decimal)8, (short)7, true, false, false };
            yield return new object[] { (decimal)8, (short)8, false, true, false };
            yield return new object[] { (decimal)8, (short)9, false, false, true };
            yield return new object[] { (decimal)8, (ShortEnum)7, true, false, false };
            yield return new object[] { (decimal)8, (ShortEnum)8, false, true, false };
            yield return new object[] { (decimal)8, (ShortEnum)9, false, false, true };
            yield return new object[] { (decimal)8, (uint)7, true, false, false };
            yield return new object[] { (decimal)8, (uint)8, false, true, false };
            yield return new object[] { (decimal)8, (uint)9, false, false, true };
            yield return new object[] { (decimal)8, (UIntEnum)7, true, false, false };
            yield return new object[] { (decimal)8, (UIntEnum)8, false, true, false };
            yield return new object[] { (decimal)8, (UIntEnum)9, false, false, true };
            yield return new object[] { (decimal)8, 7, true, false, false };
            yield return new object[] { (decimal)8, 8, false, true, false };
            yield return new object[] { (decimal)8, 9, false, false, true };
            yield return new object[] { (decimal)8, (IntEnum)7, true, false, false };
            yield return new object[] { (decimal)8, (IntEnum)8, false, true, false };
            yield return new object[] { (decimal)8, (IntEnum)9, false, false, true };
            yield return new object[] { (decimal)8, (ulong)7, true, false, false };
            yield return new object[] { (decimal)8, (ulong)8, false, true, false };
            yield return new object[] { (decimal)8, (ulong)9, false, false, true };
            yield return new object[] { (decimal)8, (ULongEnum)7, true, false, false };
            yield return new object[] { (decimal)8, (ULongEnum)8, false, true, false };
            yield return new object[] { (decimal)8, (ULongEnum)9, false, false, true };
            yield return new object[] { (decimal)8, (long)7, true, false, false };
            yield return new object[] { (decimal)8, (long)8, false, true, false };
            yield return new object[] { (decimal)8, (long)9, false, false, true };
            yield return new object[] { (decimal)8, (LongEnum)7, true, false, false };
            yield return new object[] { (decimal)8, (LongEnum)8, false, true, false };
            yield return new object[] { (decimal)8, (LongEnum)9, false, false, true };
            yield return new object[] { (decimal)8, (float)7, true, false, false };
            yield return new object[] { (decimal)8, (float)8, false, true, false };
            yield return new object[] { (decimal)8, (float)9, false, false, true };
            yield return new object[] { (decimal)8, float.PositiveInfinity, false, false, true };
            yield return new object[] { (decimal)8, float.NegativeInfinity, true, false, false };
            yield return new object[] { (decimal)8, float.NaN, false, false, false };
            yield return new object[] { (decimal)8, (double)7, true, false, false };
            yield return new object[] { (decimal)8, (double)8, false, true, false };
            yield return new object[] { (decimal)8, (double)9, false, false, true };
            yield return new object[] { (decimal)8, double.PositiveInfinity, false, false, true };
            yield return new object[] { (decimal)8, double.NegativeInfinity, true, false, false };
            yield return new object[] { (decimal)8, double.NaN, false, false, false };
            yield return new object[] { (decimal)8, (decimal)7, true, false, false };
            yield return new object[] { (decimal)8, (decimal)8, false, true, false };
            yield return new object[] { (decimal)8, (decimal)9, false, false, true };
            yield return new object[] { (decimal)8, "7", true, false, false };
            yield return new object[] { (decimal)8, "8", false, true, false };
            yield return new object[] { (decimal)8, "9", false, false, true };
            yield return new object[] { (decimal)8, true, true, false, false };
            yield return new object[] { (decimal)8, false, true, false, false };
            yield return new object[] { (decimal)8, null, true, false, false };

            // string.
            yield return new object[] { "8", (byte)7, true, false, false };
            yield return new object[] { "8", (byte)8, false, true, false };
            yield return new object[] { "8", (byte)9, false, false, true };
            yield return new object[] { "8", (ByteEnum)7, true, false, false };
            yield return new object[] { "8", (ByteEnum)8, false, true, false };
            yield return new object[] { "8", (ByteEnum)9, false, false, true };
            yield return new object[] { "8", (sbyte)7, true, false, false };
            yield return new object[] { "8", (sbyte)8, false, true, false };
            yield return new object[] { "8", (sbyte)9, false, false, true };
            yield return new object[] { "8", (SByteEnum)7, true, false, false };
            yield return new object[] { "8", (SByteEnum)8, false, true, false };
            yield return new object[] { "8", (SByteEnum)9, false, false, true };
            yield return new object[] { "8", (ushort)7, true, false, false };
            yield return new object[] { "8", (ushort)8, false, true, false };
            yield return new object[] { "8", (ushort)9, false, false, true };
            yield return new object[] { "8", (UShortEnum)7, true, false, false };
            yield return new object[] { "8", (UShortEnum)8, false, true, false };
            yield return new object[] { "8", (UShortEnum)9, false, false, true };
            yield return new object[] { "8", (short)7, true, false, false };
            yield return new object[] { "8", (short)8, false, true, false };
            yield return new object[] { "8", (short)9, false, false, true };
            yield return new object[] { "8", (ShortEnum)7, true, false, false };
            yield return new object[] { "8", (ShortEnum)8, false, true, false };
            yield return new object[] { "8", (ShortEnum)9, false, false, true };
            yield return new object[] { "8", (uint)7, true, false, false };
            yield return new object[] { "8", (uint)8, false, true, false };
            yield return new object[] { "8", (uint)9, false, false, true };
            yield return new object[] { "8", (UIntEnum)7, true, false, false };
            yield return new object[] { "8", (UIntEnum)8, false, true, false };
            yield return new object[] { "8", (UIntEnum)9, false, false, true };
            yield return new object[] { "8", 7, true, false, false };
            yield return new object[] { "8", 8, false, true, false };
            yield return new object[] { "8", 9, false, false, true };
            yield return new object[] { "8", (IntEnum)7, true, false, false };
            yield return new object[] { "8", (IntEnum)8, false, true, false };
            yield return new object[] { "8", (IntEnum)9, false, false, true };
            yield return new object[] { "8", (ulong)7, true, false, false };
            yield return new object[] { "8", (ulong)8, false, true, false };
            yield return new object[] { "8", (ulong)9, false, false, true };
            yield return new object[] { "8", (ULongEnum)7, true, false, false };
            yield return new object[] { "8", (ULongEnum)8, false, true, false };
            yield return new object[] { "8", (ULongEnum)9, false, false, true };
            yield return new object[] { "8", (long)7, true, false, false };
            yield return new object[] { "8", (long)8, false, true, false };
            yield return new object[] { "8", (long)9, false, false, true };
            yield return new object[] { "8", (LongEnum)7, true, false, false };
            yield return new object[] { "8", (LongEnum)8, false, true, false };
            yield return new object[] { "8", (LongEnum)9, false, false, true };
            yield return new object[] { "8", (float)7, true, false, false };
            yield return new object[] { "8", (float)8, false, true, false };
            yield return new object[] { "8", (float)9, false, false, true };
            yield return new object[] { "8", float.PositiveInfinity, false, false, true };
            yield return new object[] { "8", float.NegativeInfinity, true, false, false };
            yield return new object[] { "8", float.NaN, false, false, false };
            yield return new object[] { "8", (double)7, true, false, false };
            yield return new object[] { "8", (double)8, false, true, false };
            yield return new object[] { "8", (double)9, false, false, true };
            yield return new object[] { "8", double.PositiveInfinity, false, false, true };
            yield return new object[] { "8", double.NegativeInfinity, true, false, false };
            yield return new object[] { "8", double.NaN, false, false, false };
            yield return new object[] { "8", (decimal)7, true, false, false };
            yield return new object[] { "8", (decimal)8, false, true, false };
            yield return new object[] { "8", (decimal)9, false, false, true };
            yield return new object[] { "8", "7", true, false, false };
            yield return new object[] { "8", "8", false, true, false };
            yield return new object[] { "8", "9", false, false, true };
            yield return new object[] { "8", "", true, false, false };
            yield return new object[] { "8", new char[] { '7' }, true, false, false };
            yield return new object[] { "8", new char[] { '8' }, false, true, false };
            yield return new object[] { "8", new char[] { '9' }, false, false, true };
            yield return new object[] { "8", true, false, true, false };
            yield return new object[] { "8", false, false, false, true };
            yield return new object[] { "8", '7', true, false, false };
            yield return new object[] { "8", '8', false, true, false };
            yield return new object[] { "8", '9', false, false, true };
            yield return new object[] { "8", null, true, false, false };

            // string.
            yield return new object[] { "", "7", false, false, true };
            yield return new object[] { "", "8", false, false, true };
            yield return new object[] { "", "9", false, false, true };
            yield return new object[] { "", "", false, true, false };
            yield return new object[] { "", new char[] { '7' }, false, false, true };
            yield return new object[] { "", new char[] { '8' }, false, false, true };
            yield return new object[] { "", new char[] { '9' }, false, false, true };
            yield return new object[] { "", '7', false, false, true };
            yield return new object[] { "", '8', false, false, true };
            yield return new object[] { "", '9', false, false, true };
            yield return new object[] { "", null, false, true, false };

            // chars.
            yield return new object[] { new char[] { '8' }, "7", true, false, false };
            yield return new object[] { new char[] { '8' }, "8", false, true, false };
            yield return new object[] { new char[] { '8' }, "9", false, false, true };
            yield return new object[] { new char[] { '8' }, "", true, false, false };
            yield return new object[] { new char[] { '8' }, new char[] { '7' }, true, false, false };
            yield return new object[] { new char[] { '8' }, new char[] { '8' }, false, true, false };
            yield return new object[] { new char[] { '8' }, new char[] { '9' }, false, false, true };
            yield return new object[] { new char[] { '8' }, null, true, false, false };

            // chars.
            yield return new object[] { new char[0], "7", false, false, true };
            yield return new object[] { new char[0], "8", false, false, true };
            yield return new object[] { new char[0], "9", false, false, true };
            yield return new object[] { new char[0], "", false, true, false };
            yield return new object[] { new char[0], new char[] { '7' }, false, false, true };
            yield return new object[] { new char[0], new char[] { '8' }, false, false, true };
            yield return new object[] { new char[0], new char[] { '9' }, false, false, true };
            yield return new object[] { new char[0], null, false, true, false };

            // bool.
            yield return new object[] { true, (byte)7, false, false, true };
            yield return new object[] { true, (byte)8, false, false, true };
            yield return new object[] { true, (byte)9, false, false, true };
            yield return new object[] { true, (ByteEnum)7, false, false, true };
            yield return new object[] { true, (ByteEnum)8, false, false, true };
            yield return new object[] { true, (ByteEnum)9, false, false, true };
            yield return new object[] { true, (sbyte)7, false, false, true };
            yield return new object[] { true, (sbyte)8, false, false, true };
            yield return new object[] { true, (sbyte)9, false, false, true };
            yield return new object[] { true, (SByteEnum)7, false, false, true };
            yield return new object[] { true, (SByteEnum)8, false, false, true };
            yield return new object[] { true, (SByteEnum)9, false, false, true };
            yield return new object[] { true, (ushort)7, false, false, true };
            yield return new object[] { true, (ushort)8, false, false, true };
            yield return new object[] { true, (ushort)9, false, false, true };
            yield return new object[] { true, (UShortEnum)7, false, false, true };
            yield return new object[] { true, (UShortEnum)8, false, false, true };
            yield return new object[] { true, (UShortEnum)9, false, false, true };
            yield return new object[] { true, (short)7, false, false, true };
            yield return new object[] { true, (short)8, false, false, true };
            yield return new object[] { true, (short)9, false, false, true };
            yield return new object[] { true, (ShortEnum)7, false, false, true };
            yield return new object[] { true, (ShortEnum)8, false, false, true };
            yield return new object[] { true, (ShortEnum)9, false, false, true };
            yield return new object[] { true, (uint)7, false, false, true };
            yield return new object[] { true, (uint)8, false, false, true };
            yield return new object[] { true, (uint)9, false, false, true };
            yield return new object[] { true, (UIntEnum)7, false, false, true };
            yield return new object[] { true, (UIntEnum)8, false, false, true };
            yield return new object[] { true, (UIntEnum)9, false, false, true };
            yield return new object[] { true, 7, false, false, true };
            yield return new object[] { true, 8, false, false, true };
            yield return new object[] { true, 9, false, false, true };
            yield return new object[] { true, (IntEnum)7, false, false, true };
            yield return new object[] { true, (IntEnum)8, false, false, true };
            yield return new object[] { true, (IntEnum)9, false, false, true };
            yield return new object[] { true, (ulong)7, false, false, true };
            yield return new object[] { true, (ulong)8, false, false, true };
            yield return new object[] { true, (ulong)9, false, false, true };
            yield return new object[] { true, (ULongEnum)7, false, false, true };
            yield return new object[] { true, (ULongEnum)8, false, false, true };
            yield return new object[] { true, (ULongEnum)9, false, false, true };
            yield return new object[] { true, (long)7, false, false, true };
            yield return new object[] { true, (long)8, false, false, true };
            yield return new object[] { true, (long)9, false, false, true };
            yield return new object[] { true, (LongEnum)7, false, false, true };
            yield return new object[] { true, (LongEnum)8, false, false, true };
            yield return new object[] { true, (LongEnum)9, false, false, true };
            yield return new object[] { true, (float)7, false, false, true };
            yield return new object[] { true, (float)8, false, false, true };
            yield return new object[] { true, (float)9, false, false, true };
            yield return new object[] { true, float.PositiveInfinity, false, false, true };
            yield return new object[] { true, float.NegativeInfinity, true, false, false };
            yield return new object[] { true, float.NaN, false, false, false };
            yield return new object[] { true, (double)7, false, false, true };
            yield return new object[] { true, (double)8, false, false, true };
            yield return new object[] { true, (double)9, false, false, true };
            yield return new object[] { true, double.PositiveInfinity, false, false, true };
            yield return new object[] { true, double.NegativeInfinity, true, false, false };
            yield return new object[] { true, double.NaN, false, false, false };
            yield return new object[] { true, (decimal)7, false, false, true };
            yield return new object[] { true, (decimal)8, false, false, true };
            yield return new object[] { true, (decimal)9, false, false, true };
            yield return new object[] { true, "7", false, true, false };
            yield return new object[] { true, "8", false, true, false };
            yield return new object[] { true, "9", false, true, false };
            yield return new object[] { true, true, false, true, false };
            yield return new object[] { true, false, false, false, true };
            yield return new object[] { true, null, false, false, true };

            // char.
            yield return new object[] { '8', "7", true, false, false };
            yield return new object[] { '8', "8", false, true, false };
            yield return new object[] { '8', "9", false, false, true };
            yield return new object[] { '8', "", true, false, false };
            yield return new object[] { '8', '7', true, false, false };
            yield return new object[] { '8', '8', false, true, false };
            yield return new object[] { '8', '9', false, false, true };
            yield return new object[] { '8', null, true, false, false };

            // DateTime.
            yield return new object[] { new DateTime(2018, 7, 20), new DateTime(2018, 7, 19), true, false, false };
            yield return new object[] { new DateTime(2018, 7, 20), new DateTime(2018, 7, 20), false, true, false };
            yield return new object[] { new DateTime(2018, 7, 20), new DateTime(2018, 7, 11), true, false, false };
            yield return new object[] { new DateTime(2018, 7, 20), new DateTime(2018, 7, 19).ToString(), true, false, false };
            yield return new object[] { new DateTime(2018, 7, 20), new DateTime(2018, 7, 20).ToString(), false, true, false };
            yield return new object[] { new DateTime(2018, 7, 20), new DateTime(2018, 7, 11).ToString(), true, false, false };
            yield return new object[] { new DateTime(2018, 7, 20), null, true, false, false };

            // string.
            yield return new object[] { new DateTime(2018, 7, 20).ToString(), new DateTime(2018, 7, 19), true, false, false };
            yield return new object[] { new DateTime(2018, 7, 20).ToString(), new DateTime(2018, 7, 20), false, true, false };
            yield return new object[] { new DateTime(2018, 7, 20).ToString(), new DateTime(2018, 7, 21), false, false, true };

            // null.
            yield return new object[] { null, (byte)7, false, false, true };
            yield return new object[] { null, (byte)8, false, false, true };
            yield return new object[] { null, (byte)9, false, false, true };
            yield return new object[] { null, (ByteEnum)7, false, false, true };
            yield return new object[] { null, (ByteEnum)8, false, false, true };
            yield return new object[] { null, (ByteEnum)9, false, false, true };
            yield return new object[] { null, (sbyte)7, false, false, true };
            yield return new object[] { null, (sbyte)8, false, false, true };
            yield return new object[] { null, (sbyte)9, false, false, true };
            yield return new object[] { null, (SByteEnum)7, false, false, true };
            yield return new object[] { null, (SByteEnum)8, false, false, true };
            yield return new object[] { null, (SByteEnum)9, false, false, true };
            yield return new object[] { null, (ushort)7, false, false, true };
            yield return new object[] { null, (ushort)8, false, false, true };
            yield return new object[] { null, (ushort)9, false, false, true };
            yield return new object[] { null, (UShortEnum)7, false, false, true };
            yield return new object[] { null, (UShortEnum)8, false, false, true };
            yield return new object[] { null, (UShortEnum)9, false, false, true };
            yield return new object[] { null, (short)7, false, false, true };
            yield return new object[] { null, (short)8, false, false, true };
            yield return new object[] { null, (short)9, false, false, true };
            yield return new object[] { null, (ShortEnum)7, false, false, true };
            yield return new object[] { null, (ShortEnum)8, false, false, true };
            yield return new object[] { null, (ShortEnum)9, false, false, true };
            yield return new object[] { null, (uint)7, false, false, true };
            yield return new object[] { null, (uint)8, false, false, true };
            yield return new object[] { null, (uint)9, false, false, true };
            yield return new object[] { null, (UIntEnum)7, false, false, true };
            yield return new object[] { null, (UIntEnum)8, false, false, true };
            yield return new object[] { null, (UIntEnum)9, false, false, true };
            yield return new object[] { null, 7, false, false, true };
            yield return new object[] { null, 8, false, false, true };
            yield return new object[] { null, 9, false, false, true };
            yield return new object[] { null, (IntEnum)7, false, false, true };
            yield return new object[] { null, (IntEnum)8, false, false, true };
            yield return new object[] { null, (IntEnum)9, false, false, true };
            yield return new object[] { null, (ulong)7, false, false, true };
            yield return new object[] { null, (ulong)8, false, false, true };
            yield return new object[] { null, (ulong)9, false, false, true };
            yield return new object[] { null, (ULongEnum)7, false, false, true };
            yield return new object[] { null, (ULongEnum)8, false, false, true };
            yield return new object[] { null, (ULongEnum)9, false, false, true };
            yield return new object[] { null, (long)7, false, false, true };
            yield return new object[] { null, (long)8, false, false, true };
            yield return new object[] { null, (long)9, false, false, true };
            yield return new object[] { null, (LongEnum)7, false, false, true };
            yield return new object[] { null, (LongEnum)8, false, false, true };
            yield return new object[] { null, (LongEnum)9, false, false, true };
            yield return new object[] { null, (float)7, false, false, true };
            yield return new object[] { null, (float)8, false, false, true };
            yield return new object[] { null, (float)9, false, false, true };
            yield return new object[] { null, float.PositiveInfinity, false, false, true };
            yield return new object[] { null, float.NegativeInfinity, true, false, false };
            yield return new object[] { null, float.NaN, false, false, false };
            yield return new object[] { null, (double)7, false, false, true };
            yield return new object[] { null, (double)8, false, false, true };
            yield return new object[] { null, (double)9, false, false, true };
            yield return new object[] { null, double.PositiveInfinity, false, false, true };
            yield return new object[] { null, double.NegativeInfinity, true, false, false };
            yield return new object[] { null, double.NaN, false, false, false };
            yield return new object[] { null, (decimal)7, false, false, true };
            yield return new object[] { null, (decimal)8, false, false, true };
            yield return new object[] { null, (decimal)9, false, false, true };
            yield return new object[] { null, "7", false, false, true };
            yield return new object[] { null, "8", false, false, true };
            yield return new object[] { null, "9", false, false, true };
            yield return new object[] { null, "", false, true, false };
            yield return new object[] { null, new char[] { '7' }, false, false, true };
            yield return new object[] { null, new char[] { '8' }, false, false, true };
            yield return new object[] { null, new char[] { '9' }, false, false, true };
            yield return new object[] { null, true, true, false, false };
            yield return new object[] { null, false, false, true, false };
            yield return new object[] { null, '7', false, false, true };
            yield return new object[] { null, '8', false, false, true };
            yield return new object[] { null, '9', false, false, true };
            yield return new object[] { null, new DateTime(7), false, false, true };
            yield return new object[] { null, new DateTime(8), false, false, true };
            yield return new object[] { null, new DateTime(9), false, false, true };
            yield return new object[] { null, null, false, true, false };
        }

        public static IEnumerable<object[]> Compare_InvalidObjects_TestData()
        {
            yield return new object[] { 1, '2' };
            yield return new object[] { '2', 1 };
            yield return new object[] { 2, DBNull.Value };
            yield return new object[] { DBNull.Value, 2 };
            yield return new object[] { '3', new object() };
            yield return new object[] { new object(), '3' };

            yield return new object[] { new char[] { '8' }, 10 };
            yield return new object[] { 10, new char[] { '8' } };
            yield return new object[] { new char[] { '8' }, new object() };
            yield return new object[] { new object(), new char[] { '8' } };
        }

        [Theory]
        [MemberData(nameof(Compare_Primitives_TestData))]
        public void CompareObjectEqual_Invoke_ReturnsExpected(object left, object right, bool greater, bool equal, bool less)
        {
             Assert.Equal(equal, Operators.CompareObjectEqual(left, right, true));
             Assert.Equal(equal, Operators.CompareObjectEqual(left, right, false));
        }

        public static IEnumerable<object[]> CompareObjectEqual_OverloadOperator_TestData()
        {
            yield return new object[] { new CompareObjectEqual(), 2, "custom" };
            yield return new object[] { 2, new CompareObjectEqual(), "motsuc" };
            yield return new object[] { new CompareObjectEqual(), new OperatorsTests(), "customobject" };
            yield return new object[] { new OperatorsTests(), new CompareObjectEqual(), "tcejbomotsuc" };
        }

        [Theory]
        [MemberData(nameof(CompareObjectEqual_OverloadOperator_TestData))]
        public void CompareObjectEqual_InvokeOverloadedOperator_ReturnsExpected(object left, object right, object expected)
        {
             Assert.Equal(expected, Operators.CompareObjectEqual(left, right, true));
             Assert.Equal(expected, Operators.CompareObjectEqual(left, right, false));
        }

        [Theory]
        [MemberData(nameof(Compare_InvalidObjects_TestData))]
        public void CompareObjectEqual_InvalidObjects_ThrowsInvalidCastException(object left, object right)
        {
            Assert.Throws<InvalidCastException>(() => Operators.CompareObjectEqual(left, right, true));
            Assert.Throws<InvalidCastException>(() => Operators.CompareObjectEqual(right, left, true));
        }

        public static IEnumerable<object[]> CompareObjectEqual_MismatchingObjects_TestData()
        {
            yield return new object[] { new CompareObjectEqual(), new object() };
            yield return new object[] { new object(), new CompareObjectEqual() };

            yield return new object[] { new CompareObjectEqual(), new CompareObjectEqual() };
        }

        [Theory]
        [MemberData(nameof(CompareObjectEqual_MismatchingObjects_TestData))]
        public void CompareObjectEqual_MismatchingObjects_ThrowsAmibguousMatchException(object left, object right)
        {
            Assert.Throws<AmbiguousMatchException>(() => Operators.CompareObjectEqual(left, right, true));
        }

        public class CompareObjectEqual
        {
             [SpecialName]
             public static string op_Equality(CompareObjectEqual left, int right) => "custom";

             [SpecialName]
             public static string op_Equality(int left, CompareObjectEqual right) => "motsuc";

             [SpecialName]
             public static string op_Equality(CompareObjectEqual left, OperatorsTests right) => "customobject";

             [SpecialName]
             public static string op_Equality(OperatorsTests left, CompareObjectEqual right) => "tcejbomotsuc";
        }

        [Theory]
        [MemberData(nameof(Compare_Primitives_TestData))]
        public void CompareObjectGreater_Invoke_ReturnsExpected(object left, object right, bool greater, bool equal, bool less)
        {
             Assert.Equal(greater, Operators.CompareObjectGreater(left, right, true));
             Assert.Equal(greater, Operators.CompareObjectGreater(left, right, false));
        }

        public static IEnumerable<object[]> CompareObjectGreater_OverloadOperator_TestData()
        {
            yield return new object[] { new CompareObjectGreater(), 2, "custom" };
            yield return new object[] { 2, new CompareObjectGreater(), "motsuc" };
            yield return new object[] { new CompareObjectGreater(), new OperatorsTests(), "customobject" };
            yield return new object[] { new OperatorsTests(), new CompareObjectGreater(), "tcejbomotsuc" };
        }

        [Theory]
        [MemberData(nameof(CompareObjectGreater_OverloadOperator_TestData))]
        public void CompareObjectGreater_InvokeOverloadedOperator_ReturnsExpected(object left, object right, object expected)
        {
             Assert.Equal(expected, Operators.CompareObjectGreater(left, right, true));
             Assert.Equal(expected, Operators.CompareObjectGreater(left, right, false));
        }

        [Theory]
        [MemberData(nameof(Compare_InvalidObjects_TestData))]
        public void CompareObjectGreater_InvalidObjects_ThrowsInvalidCastException(object left, object right)
        {
            Assert.Throws<InvalidCastException>(() => Operators.CompareObjectGreater(left, right, true));
            Assert.Throws<InvalidCastException>(() => Operators.CompareObjectGreater(right, left, true));
        }

        public static IEnumerable<object[]> CompareObjectGreater_MismatchingObjects_TestData()
        {
            yield return new object[] { new CompareObjectGreater(), new object() };
            yield return new object[] { new object(), new CompareObjectGreater() };

            yield return new object[] { new CompareObjectGreater(), new CompareObjectGreater() };
        }

        [Theory]
        [MemberData(nameof(CompareObjectGreater_MismatchingObjects_TestData))]
        public void CompareObjectGreater_MismatchingObjects_ThrowsAmibguousMatchException(object left, object right)
        {
            Assert.Throws<AmbiguousMatchException>(() => Operators.CompareObjectGreater(left, right, true));
        }

        public class CompareObjectGreater
        {
             [SpecialName]
             public static string op_GreaterThan(CompareObjectGreater left, int right) => "custom";

             [SpecialName]
             public static string op_GreaterThan(int left, CompareObjectGreater right) => "motsuc";

             [SpecialName]
             public static string op_GreaterThan(CompareObjectGreater left, OperatorsTests right) => "customobject";

             [SpecialName]
             public static string op_GreaterThan(OperatorsTests left, CompareObjectGreater right) => "tcejbomotsuc";
        }

        [Theory]
        [MemberData(nameof(Compare_Primitives_TestData))]
        public void CompareObjectGreaterEqual_Invoke_ReturnsExpected(object left, object right, bool greater, bool equal, bool less)
        {
             Assert.Equal(greater || equal, Operators.CompareObjectGreaterEqual(left, right, true));
             Assert.Equal(greater || equal, Operators.CompareObjectGreaterEqual(left, right, false));
        }

        public static IEnumerable<object[]> CompareObjectGreaterEqual_OverloadOperator_TestData()
        {
            yield return new object[] { new CompareObjectGreaterEqual(), 2, "custom" };
            yield return new object[] { 2, new CompareObjectGreaterEqual(), "motsuc" };
            yield return new object[] { new CompareObjectGreaterEqual(), new OperatorsTests(), "customobject" };
            yield return new object[] { new OperatorsTests(), new CompareObjectGreaterEqual(), "tcejbomotsuc" };
        }

        [Theory]
        [MemberData(nameof(CompareObjectGreaterEqual_OverloadOperator_TestData))]
        public void CompareObjectGreaterEqual_InvokeOverloadedOperator_ReturnsExpected(object left, object right, object expected)
        {
             Assert.Equal(expected, Operators.CompareObjectGreaterEqual(left, right, true));
             Assert.Equal(expected, Operators.CompareObjectGreaterEqual(left, right, false));
        }

        [Theory]
        [MemberData(nameof(Compare_InvalidObjects_TestData))]
        public void CompareObjectGreaterEqual_InvalidObjects_ThrowsInvalidCastException(object left, object right)
        {
            Assert.Throws<InvalidCastException>(() => Operators.CompareObjectGreaterEqual(left, right, true));
            Assert.Throws<InvalidCastException>(() => Operators.CompareObjectGreaterEqual(right, left, true));
        }

        public static IEnumerable<object[]> CompareObjectGreaterEqual_MismatchingObjects_TestData()
        {
            yield return new object[] { new CompareObjectGreaterEqual(), new object() };
            yield return new object[] { new object(), new CompareObjectGreaterEqual() };

            yield return new object[] { new CompareObjectGreaterEqual(), new CompareObjectGreaterEqual() };
        }

        [Theory]
        [MemberData(nameof(CompareObjectGreaterEqual_MismatchingObjects_TestData))]
        public void CompareObjectGreaterEqual_MismatchingObjects_ThrowsAmibguousMatchException(object left, object right)
        {
            Assert.Throws<AmbiguousMatchException>(() => Operators.CompareObjectGreaterEqual(left, right, true));
        }

        public class CompareObjectGreaterEqual
        {
             [SpecialName]
             public static string op_GreaterThanOrEqual(CompareObjectGreaterEqual left, int right) => "custom";

             [SpecialName]
             public static string op_GreaterThanOrEqual(int left, CompareObjectGreaterEqual right) => "motsuc";

             [SpecialName]
             public static string op_GreaterThanOrEqual(CompareObjectGreaterEqual left, OperatorsTests right) => "customobject";

             [SpecialName]
             public static string op_GreaterThanOrEqual(OperatorsTests left, CompareObjectGreaterEqual right) => "tcejbomotsuc";
        }

        [Theory]
        [MemberData(nameof(Compare_Primitives_TestData))]
        public void CompareObjectLess_Invoke_ReturnsExpected(object left, object right, bool greater, bool equal, bool less)
        {
             Assert.Equal(less, Operators.CompareObjectLess(left, right, true));
             Assert.Equal(less, Operators.CompareObjectLess(left, right, false));
        }

        public static IEnumerable<object[]> CompareObjectLess_OverloadOperator_TestData()
        {
            yield return new object[] { new CompareObjectLess(), 2, "custom" };
            yield return new object[] { 2, new CompareObjectLess(), "motsuc" };
            yield return new object[] { new CompareObjectLess(), new OperatorsTests(), "customobject" };
            yield return new object[] { new OperatorsTests(), new CompareObjectLess(), "tcejbomotsuc" };
        }

        [Theory]
        [MemberData(nameof(CompareObjectLess_OverloadOperator_TestData))]
        public void CompareObjectLess_InvokeOverloadedOperator_ReturnsExpected(object left, object right, object expected)
        {
             Assert.Equal(expected, Operators.CompareObjectLess(left, right, true));
             Assert.Equal(expected, Operators.CompareObjectLess(left, right, false));
        }

        [Theory]
        [MemberData(nameof(Compare_InvalidObjects_TestData))]
        public void CompareObjectLess_InvalidObjects_ThrowsInvalidCastException(object left, object right)
        {
            Assert.Throws<InvalidCastException>(() => Operators.CompareObjectLess(left, right, true));
            Assert.Throws<InvalidCastException>(() => Operators.CompareObjectLess(right, left, true));
        }

        public static IEnumerable<object[]> CompareObjectLess_MismatchingObjects_TestData()
        {
            yield return new object[] { new CompareObjectLess(), new object() };
            yield return new object[] { new object(), new CompareObjectLess() };

            yield return new object[] { new CompareObjectLess(), new CompareObjectLess() };
        }

        [Theory]
        [MemberData(nameof(CompareObjectLess_MismatchingObjects_TestData))]
        public void CompareObjectLess_MismatchingObjects_ThrowsAmibguousMatchException(object left, object right)
        {
            Assert.Throws<AmbiguousMatchException>(() => Operators.CompareObjectLess(left, right, true));
        }

        public class CompareObjectLess
        {
             [SpecialName]
             public static string op_LessThan(CompareObjectLess left, int right) => "custom";

             [SpecialName]
             public static string op_LessThan(int left, CompareObjectLess right) => "motsuc";

             [SpecialName]
             public static string op_LessThan(CompareObjectLess left, OperatorsTests right) => "customobject";

             [SpecialName]
             public static string op_LessThan(OperatorsTests left, CompareObjectLess right) => "tcejbomotsuc";
        }

        [Theory]
        [MemberData(nameof(Compare_Primitives_TestData))]
        public void CompareObjectLessEqual_Invoke_ReturnsExpected(object left, object right, bool greater, bool equal, bool less)
        {
             Assert.Equal(less || equal, Operators.CompareObjectLessEqual(left, right, true));
             Assert.Equal(less || equal, Operators.CompareObjectLessEqual(left, right, false));
        }

        public static IEnumerable<object[]> CompareObjectLessEqual_OverloadOperator_TestData()
        {
            yield return new object[] { new CompareObjectLessEqual(), 2, "custom" };
            yield return new object[] { 2, new CompareObjectLessEqual(), "motsuc" };
            yield return new object[] { new CompareObjectLessEqual(), new OperatorsTests(), "customobject" };
            yield return new object[] { new OperatorsTests(), new CompareObjectLessEqual(), "tcejbomotsuc" };
        }

        [Theory]
        [MemberData(nameof(CompareObjectLessEqual_OverloadOperator_TestData))]
        public void CompareObjectLessEqual_InvokeOverloadedOperator_ReturnsExpected(object left, object right, object expected)
        {
             Assert.Equal(expected, Operators.CompareObjectLessEqual(left, right, true));
             Assert.Equal(expected, Operators.CompareObjectLessEqual(left, right, false));
        }

        [Theory]
        [MemberData(nameof(Compare_InvalidObjects_TestData))]
        public void CompareObjectLessEqual_InvalidObjects_ThrowsInvalidCastException(object left, object right)
        {
            Assert.Throws<InvalidCastException>(() => Operators.CompareObjectLessEqual(left, right, true));
            Assert.Throws<InvalidCastException>(() => Operators.CompareObjectLessEqual(right, left, true));
        }

        public static IEnumerable<object[]> CompareObjectLessEqual_MismatchingObjects_TestData()
        {
            yield return new object[] { new CompareObjectLessEqual(), new object() };
            yield return new object[] { new object(), new CompareObjectLessEqual() };

            yield return new object[] { new CompareObjectLessEqual(), new CompareObjectLessEqual() };
        }

        [Theory]
        [MemberData(nameof(CompareObjectLessEqual_MismatchingObjects_TestData))]
        public void CompareObjectLessEqual_MismatchingObjects_ThrowsAmibguousMatchException(object left, object right)
        {
            Assert.Throws<AmbiguousMatchException>(() => Operators.CompareObjectLessEqual(left, right, true));
        }

        public class CompareObjectLessEqual
        {
             [SpecialName]
             public static string op_LessThanOrEqual(CompareObjectLessEqual left, int right) => "custom";

             [SpecialName]
             public static string op_LessThanOrEqual(int left, CompareObjectLessEqual right) => "motsuc";

             [SpecialName]
             public static string op_LessThanOrEqual(CompareObjectLessEqual left, OperatorsTests right) => "customobject";

             [SpecialName]
             public static string op_LessThanOrEqual(OperatorsTests left, CompareObjectLessEqual right) => "tcejbomotsuc";
        }

        [Theory]
        [MemberData(nameof(Compare_Primitives_TestData))]
        public void CompareObjectNotEqual_Invoke_ReturnsExpected(object left, object right, bool greater, bool equal, bool less)
        {
             Assert.Equal(!equal, Operators.CompareObjectNotEqual(left, right, true));
             Assert.Equal(!equal, Operators.CompareObjectNotEqual(left, right, false));
        }

        public static IEnumerable<object[]> CompareObjectNotEqual_OverloadOperator_TestData()
        {
            yield return new object[] { new CompareObjectNotEqual(), 2, "custom" };
            yield return new object[] { 2, new CompareObjectNotEqual(), "motsuc" };
            yield return new object[] { new CompareObjectNotEqual(), new OperatorsTests(), "customobject" };
            yield return new object[] { new OperatorsTests(), new CompareObjectNotEqual(), "tcejbomotsuc" };
        }

        [Theory]
        [MemberData(nameof(CompareObjectNotEqual_OverloadOperator_TestData))]
        public void CompareObjectNotEqual_InvokeOverloadedOperator_ReturnsExpected(object left, object right, object expected)
        {
             Assert.Equal(expected, Operators.CompareObjectNotEqual(left, right, true));
             Assert.Equal(expected, Operators.CompareObjectNotEqual(left, right, false));
        }

        [Theory]
        [MemberData(nameof(Compare_InvalidObjects_TestData))]
        public void CompareObjectNotEqual_InvalidObjects_ThrowsInvalidCastException(object left, object right)
        {
            Assert.Throws<InvalidCastException>(() => Operators.CompareObjectNotEqual(left, right, true));
            Assert.Throws<InvalidCastException>(() => Operators.CompareObjectNotEqual(right, left, true));
        }

        public static IEnumerable<object[]> CompareObjectNotEqual_MismatchingObjects_TestData()
        {
            yield return new object[] { new CompareObjectNotEqual(), new object() };
            yield return new object[] { new object(), new CompareObjectNotEqual() };

            yield return new object[] { new CompareObjectNotEqual(), new CompareObjectNotEqual() };
        }

        [Theory]
        [MemberData(nameof(CompareObjectNotEqual_MismatchingObjects_TestData))]
        public void CompareObjectNotEqual_MismatchingObjects_ThrowsAmibguousMatchException(object left, object right)
        {
            Assert.Throws<AmbiguousMatchException>(() => Operators.CompareObjectNotEqual(left, right, true));
        }

        public class CompareObjectNotEqual
        {
             [SpecialName]
             public static string op_Inequality(CompareObjectNotEqual left, int right) => "custom";

             [SpecialName]
             public static string op_Inequality(int left, CompareObjectNotEqual right) => "motsuc";

             [SpecialName]
             public static string op_Inequality(CompareObjectNotEqual left, OperatorsTests right) => "customobject";

             [SpecialName]
             public static string op_Inequality(OperatorsTests left, CompareObjectNotEqual right) => "tcejbomotsuc";
        }


        [Theory]
        [MemberData(nameof(Compare_Primitives_TestData))]
        public void ConditionalCompareObjectEqual_Invoke_ReturnsExpected(object left, object right, bool greater, bool equal, bool less)
        {
             Assert.Equal(equal, Operators.ConditionalCompareObjectEqual(left, right, true));
             Assert.Equal(equal, Operators.ConditionalCompareObjectEqual(left, right, false));
        }

        public static IEnumerable<object[]> ConditionalCompareObjectEqual_OverloadOperator_TestData()
        {
            yield return new object[] { new ConditionalCompareObjectEqual(), 2, true };
            yield return new object[] { 2, new ConditionalCompareObjectEqual(), false };
            yield return new object[] { new ConditionalCompareObjectEqual(), new OperatorsTests(), false };
            yield return new object[] { new OperatorsTests(), new ConditionalCompareObjectEqual(), true };
        }

        [Theory]
        [MemberData(nameof(ConditionalCompareObjectEqual_OverloadOperator_TestData))]
        public void ConditionalCompareObjectEqual_InvokeOverloadedOperator_ReturnsExpected(object left, object right, object expected)
        {
             Assert.Equal(expected, Operators.ConditionalCompareObjectEqual(left, right, true));
             Assert.Equal(expected, Operators.ConditionalCompareObjectEqual(left, right, false));
        }

        [Theory]
        [MemberData(nameof(Compare_InvalidObjects_TestData))]
        public void ConditionalCompareObjectEqual_InvalidObjects_ThrowsInvalidCastException(object left, object right)
        {
            Assert.Throws<InvalidCastException>(() => Operators.ConditionalCompareObjectEqual(left, right, true));
            Assert.Throws<InvalidCastException>(() => Operators.ConditionalCompareObjectEqual(right, left, true));
        }

        public static IEnumerable<object[]> ConditionalCompareObjectEqual_MismatchingObjects_TestData()
        {
            yield return new object[] { new ConditionalCompareObjectEqual(), new object() };
            yield return new object[] { new object(), new ConditionalCompareObjectEqual() };

            yield return new object[] { new ConditionalCompareObjectEqual(), new ConditionalCompareObjectEqual() };
        }

        [Theory]
        [MemberData(nameof(ConditionalCompareObjectEqual_MismatchingObjects_TestData))]
        public void ConditionalCompareObjectEqual_MismatchingObjects_ThrowsAmibguousMatchException(object left, object right)
        {
            Assert.Throws<AmbiguousMatchException>(() => Operators.ConditionalCompareObjectEqual(left, right, true));
        }

        public class ConditionalCompareObjectEqual
        {
             [SpecialName]
             public static bool op_Equality(ConditionalCompareObjectEqual left, int right) => true;

             [SpecialName]
             public static bool op_Equality(int left, ConditionalCompareObjectEqual right) => false;

             [SpecialName]
             public static bool op_Equality(ConditionalCompareObjectEqual left, OperatorsTests right) => false;

             [SpecialName]
             public static bool op_Equality(OperatorsTests left, ConditionalCompareObjectEqual right) => true;
        }

        [Theory]
        [MemberData(nameof(Compare_Primitives_TestData))]
        public void ConditionalCompareObjectGreater_Invoke_ReturnsExpected(object left, object right, bool greater, bool equal, bool less)
        {
             Assert.Equal(greater, Operators.ConditionalCompareObjectGreater(left, right, true));
             Assert.Equal(greater, Operators.ConditionalCompareObjectGreater(left, right, false));
        }

        public static IEnumerable<object[]> ConditionalCompareObjectGreater_OverloadOperator_TestData()
        {
            yield return new object[] { new ConditionalCompareObjectGreater(), 2, true };
            yield return new object[] { 2, new ConditionalCompareObjectGreater(), false };
            yield return new object[] { new ConditionalCompareObjectGreater(), new OperatorsTests(), false };
            yield return new object[] { new OperatorsTests(), new ConditionalCompareObjectGreater(), true };
        }

        [Theory]
        [MemberData(nameof(ConditionalCompareObjectGreater_OverloadOperator_TestData))]
        public void ConditionalCompareObjectGreater_InvokeOverloadedOperator_ReturnsExpected(object left, object right, object expected)
        {
             Assert.Equal(expected, Operators.ConditionalCompareObjectGreater(left, right, true));
             Assert.Equal(expected, Operators.ConditionalCompareObjectGreater(left, right, false));
        }

        [Theory]
        [MemberData(nameof(Compare_InvalidObjects_TestData))]
        public void ConditionalCompareObjectGreater_InvalidObjects_ThrowsInvalidCastException(object left, object right)
        {
            Assert.Throws<InvalidCastException>(() => Operators.ConditionalCompareObjectGreater(left, right, true));
            Assert.Throws<InvalidCastException>(() => Operators.ConditionalCompareObjectGreater(right, left, true));
        }

        public static IEnumerable<object[]> ConditionalCompareObjectGreater_MismatchingObjects_TestData()
        {
            yield return new object[] { new ConditionalCompareObjectGreater(), new object() };
            yield return new object[] { new object(), new ConditionalCompareObjectGreater() };

            yield return new object[] { new ConditionalCompareObjectGreater(), new ConditionalCompareObjectGreater() };
        }

        [Theory]
        [MemberData(nameof(ConditionalCompareObjectGreater_MismatchingObjects_TestData))]
        public void ConditionalCompareObjectGreater_MismatchingObjects_ThrowsAmibguousMatchException(object left, object right)
        {
            Assert.Throws<AmbiguousMatchException>(() => Operators.ConditionalCompareObjectGreater(left, right, true));
        }

        public class ConditionalCompareObjectGreater
        {
             [SpecialName]
             public static bool op_GreaterThan(ConditionalCompareObjectGreater left, int right) => true;

             [SpecialName]
             public static bool op_GreaterThan(int left, ConditionalCompareObjectGreater right) => false;

             [SpecialName]
             public static bool op_GreaterThan(ConditionalCompareObjectGreater left, OperatorsTests right) => false;

             [SpecialName]
             public static bool op_GreaterThan(OperatorsTests left, ConditionalCompareObjectGreater right) => true;
        }

        [Theory]
        [MemberData(nameof(Compare_Primitives_TestData))]
        public void ConditionalCompareObjectGreaterEqual_Invoke_ReturnsExpected(object left, object right, bool greater, bool equal, bool less)
        {
             Assert.Equal(greater || equal, Operators.ConditionalCompareObjectGreaterEqual(left, right, true));
             Assert.Equal(greater || equal, Operators.ConditionalCompareObjectGreaterEqual(left, right, false));
        }

        public static IEnumerable<object[]> ConditionalCompareObjectGreaterEqual_OverloadOperator_TestData()
        {
            yield return new object[] { new ConditionalCompareObjectGreaterEqual(), 2, true };
            yield return new object[] { 2, new ConditionalCompareObjectGreaterEqual(), false };
            yield return new object[] { new ConditionalCompareObjectGreaterEqual(), new OperatorsTests(), false };
            yield return new object[] { new OperatorsTests(), new ConditionalCompareObjectGreaterEqual(), true };
        }

        [Theory]
        [MemberData(nameof(ConditionalCompareObjectGreaterEqual_OverloadOperator_TestData))]
        public void ConditionalCompareObjectGreaterEqual_InvokeOverloadedOperator_ReturnsExpected(object left, object right, object expected)
        {
             Assert.Equal(expected, Operators.ConditionalCompareObjectGreaterEqual(left, right, true));
             Assert.Equal(expected, Operators.ConditionalCompareObjectGreaterEqual(left, right, false));
        }

        [Theory]
        [MemberData(nameof(Compare_InvalidObjects_TestData))]
        public void ConditionalCompareObjectGreaterEqual_InvalidObjects_ThrowsInvalidCastException(object left, object right)
        {
            Assert.Throws<InvalidCastException>(() => Operators.ConditionalCompareObjectGreaterEqual(left, right, true));
            Assert.Throws<InvalidCastException>(() => Operators.ConditionalCompareObjectGreaterEqual(right, left, true));
        }

        public static IEnumerable<object[]> ConditionalCompareObjectGreaterEqual_MismatchingObjects_TestData()
        {
            yield return new object[] { new ConditionalCompareObjectGreaterEqual(), new object() };
            yield return new object[] { new object(), new ConditionalCompareObjectGreaterEqual() };

            yield return new object[] { new ConditionalCompareObjectGreaterEqual(), new ConditionalCompareObjectGreaterEqual() };
        }

        [Theory]
        [MemberData(nameof(ConditionalCompareObjectGreaterEqual_MismatchingObjects_TestData))]
        public void ConditionalCompareObjectGreaterEqual_MismatchingObjects_ThrowsAmibguousMatchException(object left, object right)
        {
            Assert.Throws<AmbiguousMatchException>(() => Operators.ConditionalCompareObjectGreaterEqual(left, right, true));
        }

        public class ConditionalCompareObjectGreaterEqual
        {
             [SpecialName]
             public static bool op_GreaterThanOrEqual(ConditionalCompareObjectGreaterEqual left, int right) => true;

             [SpecialName]
             public static bool op_GreaterThanOrEqual(int left, ConditionalCompareObjectGreaterEqual right) => false;

             [SpecialName]
             public static bool op_GreaterThanOrEqual(ConditionalCompareObjectGreaterEqual left, OperatorsTests right) => false;

             [SpecialName]
             public static bool op_GreaterThanOrEqual(OperatorsTests left, ConditionalCompareObjectGreaterEqual right) => true;
        }

        [Theory]
        [MemberData(nameof(Compare_Primitives_TestData))]
        public void ConditionalCompareObjectLess_Invoke_ReturnsExpected(object left, object right, bool greater, bool equal, bool less)
        {
             Assert.Equal(less, Operators.ConditionalCompareObjectLess(left, right, true));
             Assert.Equal(less, Operators.ConditionalCompareObjectLess(left, right, false));
        }

        public static IEnumerable<object[]> ConditionalCompareObjectLess_OverloadOperator_TestData()
        {
            yield return new object[] { new ConditionalCompareObjectLess(), 2, true };
            yield return new object[] { 2, new ConditionalCompareObjectLess(), false };
            yield return new object[] { new ConditionalCompareObjectLess(), new OperatorsTests(), false };
            yield return new object[] { new OperatorsTests(), new ConditionalCompareObjectLess(), true };
        }

        [Theory]
        [MemberData(nameof(ConditionalCompareObjectLess_OverloadOperator_TestData))]
        public void ConditionalCompareObjectLess_InvokeOverloadedOperator_ReturnsExpected(object left, object right, object expected)
        {
             Assert.Equal(expected, Operators.ConditionalCompareObjectLess(left, right, true));
             Assert.Equal(expected, Operators.ConditionalCompareObjectLess(left, right, false));
        }

        [Theory]
        [MemberData(nameof(Compare_InvalidObjects_TestData))]
        public void ConditionalCompareObjectLess_InvalidObjects_ThrowsInvalidCastException(object left, object right)
        {
            Assert.Throws<InvalidCastException>(() => Operators.ConditionalCompareObjectLess(left, right, true));
            Assert.Throws<InvalidCastException>(() => Operators.ConditionalCompareObjectLess(right, left, true));
        }

        public static IEnumerable<object[]> ConditionalCompareObjectLess_MismatchingObjects_TestData()
        {
            yield return new object[] { new ConditionalCompareObjectLess(), new object() };
            yield return new object[] { new object(), new ConditionalCompareObjectLess() };

            yield return new object[] { new ConditionalCompareObjectLess(), new ConditionalCompareObjectLess() };
        }

        [Theory]
        [MemberData(nameof(ConditionalCompareObjectLess_MismatchingObjects_TestData))]
        public void ConditionalCompareObjectLess_MismatchingObjects_ThrowsAmibguousMatchException(object left, object right)
        {
            Assert.Throws<AmbiguousMatchException>(() => Operators.ConditionalCompareObjectLess(left, right, true));
        }

        public class ConditionalCompareObjectLess
        {
             [SpecialName]
             public static bool op_LessThan(ConditionalCompareObjectLess left, int right) => true;

             [SpecialName]
             public static bool op_LessThan(int left, ConditionalCompareObjectLess right) => false;

             [SpecialName]
             public static bool op_LessThan(ConditionalCompareObjectLess left, OperatorsTests right) => false;

             [SpecialName]
             public static bool op_LessThan(OperatorsTests left, ConditionalCompareObjectLess right) => true;
        }

        [Theory]
        [MemberData(nameof(Compare_Primitives_TestData))]
        public void ConditionalCompareObjectLessEqual_Invoke_ReturnsExpected(object left, object right, bool greater, bool equal, bool less)
        {
             Assert.Equal(less || equal, Operators.ConditionalCompareObjectLessEqual(left, right, true));
             Assert.Equal(less || equal, Operators.ConditionalCompareObjectLessEqual(left, right, false));
        }

        public static IEnumerable<object[]> ConditionalCompareObjectLessEqual_OverloadOperator_TestData()
        {
            yield return new object[] { new ConditionalCompareObjectLessEqual(), 2, true };
            yield return new object[] { 2, new ConditionalCompareObjectLessEqual(), false };
            yield return new object[] { new ConditionalCompareObjectLessEqual(), new OperatorsTests(), false };
            yield return new object[] { new OperatorsTests(), new ConditionalCompareObjectLessEqual(), true };
        }

        [Theory]
        [MemberData(nameof(ConditionalCompareObjectLessEqual_OverloadOperator_TestData))]
        public void ConditionalCompareObjectLessEqual_InvokeOverloadedOperator_ReturnsExpected(object left, object right, object expected)
        {
             Assert.Equal(expected, Operators.ConditionalCompareObjectLessEqual(left, right, true));
             Assert.Equal(expected, Operators.ConditionalCompareObjectLessEqual(left, right, false));
        }

        [Theory]
        [MemberData(nameof(Compare_InvalidObjects_TestData))]
        public void ConditionalCompareObjectLessEqual_InvalidObjects_ThrowsInvalidCastException(object left, object right)
        {
            Assert.Throws<InvalidCastException>(() => Operators.ConditionalCompareObjectLessEqual(left, right, true));
            Assert.Throws<InvalidCastException>(() => Operators.ConditionalCompareObjectLessEqual(right, left, true));
        }

        public static IEnumerable<object[]> ConditionalCompareObjectLessEqual_MismatchingObjects_TestData()
        {
            yield return new object[] { new ConditionalCompareObjectLessEqual(), new object() };
            yield return new object[] { new object(), new ConditionalCompareObjectLessEqual() };

            yield return new object[] { new ConditionalCompareObjectLessEqual(), new ConditionalCompareObjectLessEqual() };
        }

        [Theory]
        [MemberData(nameof(ConditionalCompareObjectLessEqual_MismatchingObjects_TestData))]
        public void ConditionalCompareObjectLessEqual_MismatchingObjects_ThrowsAmibguousMatchException(object left, object right)
        {
            Assert.Throws<AmbiguousMatchException>(() => Operators.ConditionalCompareObjectLessEqual(left, right, true));
        }

        public class ConditionalCompareObjectLessEqual
        {
             [SpecialName]
             public static bool op_LessThanOrEqual(ConditionalCompareObjectLessEqual left, int right) => true;

             [SpecialName]
             public static bool op_LessThanOrEqual(int left, ConditionalCompareObjectLessEqual right) => false;

             [SpecialName]
             public static bool op_LessThanOrEqual(ConditionalCompareObjectLessEqual left, OperatorsTests right) => false;

             [SpecialName]
             public static bool op_LessThanOrEqual(OperatorsTests left, ConditionalCompareObjectLessEqual right) => true;
        }

        [Theory]
        [MemberData(nameof(Compare_Primitives_TestData))]
        public void ConditionalCompareObjectNotEqual_Invoke_ReturnsExpected(object left, object right, bool greater, bool equal, bool less)
        {
             Assert.Equal(!equal, Operators.ConditionalCompareObjectNotEqual(left, right, true));
             Assert.Equal(!equal, Operators.ConditionalCompareObjectNotEqual(left, right, false));
        }

        public static IEnumerable<object[]> ConditionalCompareObjectNotEqual_OverloadOperator_TestData()
        {
            yield return new object[] { new ConditionalCompareObjectNotEqual(), 2, true };
            yield return new object[] { 2, new ConditionalCompareObjectNotEqual(), false };
            yield return new object[] { new ConditionalCompareObjectNotEqual(), new OperatorsTests(), false };
            yield return new object[] { new OperatorsTests(), new ConditionalCompareObjectNotEqual(), true };
        }

        [Theory]
        [MemberData(nameof(ConditionalCompareObjectNotEqual_OverloadOperator_TestData))]
        public void ConditionalCompareObjectNotEqual_InvokeOverloadedOperator_ReturnsExpected(object left, object right, object expected)
        {
             Assert.Equal(expected, Operators.ConditionalCompareObjectNotEqual(left, right, true));
             Assert.Equal(expected, Operators.ConditionalCompareObjectNotEqual(left, right, false));
        }

        [Theory]
        [MemberData(nameof(Compare_InvalidObjects_TestData))]
        public void ConditionalCompareObjectNotEqual_InvalidObjects_ThrowsInvalidCastException(object left, object right)
        {
            Assert.Throws<InvalidCastException>(() => Operators.ConditionalCompareObjectNotEqual(left, right, true));
            Assert.Throws<InvalidCastException>(() => Operators.ConditionalCompareObjectNotEqual(right, left, true));
        }

        public static IEnumerable<object[]> ConditionalCompareObjectNotEqual_MismatchingObjects_TestData()
        {
            yield return new object[] { new ConditionalCompareObjectNotEqual(), new object() };
            yield return new object[] { new object(), new ConditionalCompareObjectNotEqual() };

            yield return new object[] { new ConditionalCompareObjectNotEqual(), new ConditionalCompareObjectNotEqual() };
        }

        [Theory]
        [MemberData(nameof(ConditionalCompareObjectNotEqual_MismatchingObjects_TestData))]
        public void ConditionalCompareObjectNotEqual_MismatchingObjects_ThrowsAmibguousMatchException(object left, object right)
        {
            Assert.Throws<AmbiguousMatchException>(() => Operators.ConditionalCompareObjectNotEqual(left, right, true));
        }

        public class ConditionalCompareObjectNotEqual
        {
             [SpecialName]
             public static bool op_Inequality(ConditionalCompareObjectNotEqual left, int right) => true;

             [SpecialName]
             public static bool op_Inequality(int left, ConditionalCompareObjectNotEqual right) => false;

             [SpecialName]
             public static bool op_Inequality(ConditionalCompareObjectNotEqual left, OperatorsTests right) => false;

             [SpecialName]
             public static bool op_Inequality(OperatorsTests left, ConditionalCompareObjectNotEqual right) => true;
        }
    }
}
