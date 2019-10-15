// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    /*
     * The kind of allocation used in a constant value.
     * Since CONSTVALs don't store a discriminant for the union
     * this must be managed by clients.
     */
    internal enum ConstValKind
    {
        Int,
        Double,
        Long,
        String,
        Decimal,
        IntPtr,
        Float,
        Boolean
    }

    internal readonly struct ConstVal
    {
        // Pre-boxed common values.
        private static readonly object s_false = false;
        private static readonly object s_true = true;
        private static readonly object s_zeroInt32 = 0;

        private ConstVal(object value)
        {
            ObjectVal = value;
        }

        public object ObjectVal { get; }

        public bool BooleanVal => SpecialUnbox<bool>(ObjectVal);

        public sbyte SByteVal => SpecialUnbox<sbyte>(ObjectVal);

        public byte ByteVal => SpecialUnbox<byte>(ObjectVal);

        public short Int16Val => SpecialUnbox<short>(ObjectVal);

        public ushort UInt16Val => SpecialUnbox<ushort>(ObjectVal);

        public int Int32Val => SpecialUnbox<int>(ObjectVal);

        public uint UInt32Val => SpecialUnbox<uint>(ObjectVal);

        public long Int64Val => SpecialUnbox<long>(ObjectVal);

        public ulong UInt64Val => SpecialUnbox<ulong>(ObjectVal);

        public float SingleVal => SpecialUnbox<float>(ObjectVal);

        public double DoubleVal => SpecialUnbox<double>(ObjectVal);

        public decimal DecimalVal => SpecialUnbox<decimal>(ObjectVal);

        public char CharVal => SpecialUnbox<char>(ObjectVal);

        public string StringVal => SpecialUnbox<string>(ObjectVal);

        public bool IsNullRef => ObjectVal == null;

        public bool IsZero(ConstValKind kind) =>
            kind switch
            {
                ConstValKind.Decimal => DecimalVal == 0,
                ConstValKind.String => false,
                _ => IsDefault(ObjectVal),
            };

        private static T SpecialUnbox<T>(object o)
        {
            if (IsDefault(o))
            {
                return default(T);
            }

            return (T)Convert.ChangeType(o, typeof(T), CultureInfo.InvariantCulture);
        }

        private static bool IsDefault(object o) =>
            o is null ||
            Type.GetTypeCode(o.GetType()) switch
            {
                TypeCode.Boolean => default(bool).Equals(o),
                TypeCode.SByte => default(sbyte).Equals(o),
                TypeCode.Byte => default(byte).Equals(o),
                TypeCode.Int16 => default(short).Equals(o),
                TypeCode.UInt16 => default(ushort).Equals(o),
                TypeCode.Int32 => default(int).Equals(o),
                TypeCode.UInt32 => default(uint).Equals(o),
                TypeCode.Int64 => default(long).Equals(o),
                TypeCode.UInt64 => default(ulong).Equals(o),
                TypeCode.Single => default(float).Equals(o),
                TypeCode.Double => default(double).Equals(o),
                TypeCode.Decimal => default(decimal).Equals(o),
                TypeCode.Char => default(char).Equals(o),

                _ => false,
            };

        public static ConstVal GetDefaultValue(ConstValKind kind) =>
            kind switch
            {
                ConstValKind.Int => new ConstVal(s_zeroInt32),
                ConstValKind.Double => new ConstVal(0.0),
                ConstValKind.Long => new ConstVal(0L),
                ConstValKind.Decimal => new ConstVal(0M),
                ConstValKind.Float => new ConstVal(0F),
                ConstValKind.Boolean => new ConstVal(s_false),
                _ => default,
            };

        public static ConstVal Get(bool value) => new ConstVal(value ? s_true : s_false);

        public static ConstVal Get(int value) => new ConstVal(value == 0 ? s_zeroInt32 : value);

        public static ConstVal Get(uint value) => new ConstVal(value);

        public static ConstVal Get(decimal value) => new ConstVal(value);

        public static ConstVal Get(string value) => new ConstVal(value);

        public static ConstVal Get(float value) => new ConstVal(value);

        public static ConstVal Get(double value) => new ConstVal(value);

        public static ConstVal Get(long value) => new ConstVal(value);

        public static ConstVal Get(ulong value) => new ConstVal(value);

        public static ConstVal Get(object p) => new ConstVal(p);
    }
}
