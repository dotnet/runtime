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

        public bool IsZero(ConstValKind kind)
        {
            switch (kind)
            {
                case ConstValKind.Decimal:
                    return DecimalVal == 0;
                case ConstValKind.String:
                    return false;
                default:
                    return IsDefault(ObjectVal);
            }
        }

        private static T SpecialUnbox<T>(object o)
        {
            if (IsDefault(o))
            {
                return default(T);
            }

            return (T)Convert.ChangeType(o, typeof(T), CultureInfo.InvariantCulture);
        }

        private static bool IsDefault(object o)
        {
            if (o == null)
                return true;

            switch (Type.GetTypeCode(o.GetType()))
            {
                case TypeCode.Boolean:
                    return default(bool).Equals(o);
                case TypeCode.SByte:
                    return default(sbyte).Equals(o);
                case TypeCode.Byte:
                    return default(byte).Equals(o);
                case TypeCode.Int16:
                    return default(short).Equals(o);
                case TypeCode.UInt16:
                    return default(ushort).Equals(o);
                case TypeCode.Int32:
                    return default(int).Equals(o);
                case TypeCode.UInt32:
                    return default(uint).Equals(o);
                case TypeCode.Int64:
                    return default(long).Equals(o);
                case TypeCode.UInt64:
                    return default(ulong).Equals(o);
                case TypeCode.Single:
                    return default(float).Equals(o);
                case TypeCode.Double:
                    return default(double).Equals(o);
                case TypeCode.Decimal:
                    return default(decimal).Equals(o);
                case TypeCode.Char:
                    return default(char).Equals(o);
            }

            return false;
        }

        public static ConstVal GetDefaultValue(ConstValKind kind)
        {
            switch (kind)
            {
                case ConstValKind.Int:
                    return new ConstVal(s_zeroInt32);

                case ConstValKind.Double:
                    return new ConstVal(0.0);

                case ConstValKind.Long:
                    return new ConstVal(0L);

                case ConstValKind.Decimal:
                    return new ConstVal(0M);

                case ConstValKind.Float:
                    return new ConstVal(0F);

                case ConstValKind.Boolean:
                    return new ConstVal(s_false);
            }

            return default(ConstVal);
        }

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
