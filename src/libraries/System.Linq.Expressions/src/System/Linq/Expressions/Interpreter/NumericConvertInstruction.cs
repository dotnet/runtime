// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Dynamic.Utils;

namespace System.Linq.Expressions.Interpreter
{
    internal abstract class NumericConvertInstruction : Instruction
    {
        internal readonly TypeCode _from, _to;
        private readonly bool _isLiftedToNull;

        protected NumericConvertInstruction(TypeCode from, TypeCode to, bool isLiftedToNull)
        {
            _from = from;
            _to = to;
            _isLiftedToNull = isLiftedToNull;
        }

        public sealed override int Run(InterpretedFrame frame)
        {
            object obj = frame.Pop();
            object converted;
            if (obj == null)
            {
                if (_isLiftedToNull)
                {
                    converted = null;
                }
                else
                {
                    // We cannot have null in a non-lifted numeric context. Throw the exception
                    // about not Nullable object requiring a value.
                    return (int)(int?)obj;
                }
            }
            else
            {
                converted = Convert(obj);
            }

            frame.Push(converted);
            return 1;
        }

        protected abstract object Convert(object obj);

        public override string InstructionName => "NumericConvert";
        public override int ConsumedStack => 1;
        public override int ProducedStack => 1;

        public override string ToString() => InstructionName + "(" + _from + "->" + _to + ")";

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        internal sealed class Unchecked : NumericConvertInstruction
        {
            public override string InstructionName => "UncheckedConvert";

            public Unchecked(TypeCode from, TypeCode to, bool isLiftedToNull)
                : base(from, to, isLiftedToNull)
            {
            }

            protected override object Convert(object obj)
            {
                return _from switch
                {
                    TypeCode.Boolean => ConvertInt32((bool)obj ? 1 : 0),
                    TypeCode.Byte => ConvertInt32((byte)obj),
                    TypeCode.SByte => ConvertInt32((sbyte)obj),
                    TypeCode.Int16 => ConvertInt32((short)obj),
                    TypeCode.Char => ConvertInt32((char)obj),
                    TypeCode.Int32 => ConvertInt32((int)obj),
                    TypeCode.Int64 => ConvertInt64((long)obj),
                    TypeCode.UInt16 => ConvertInt32((ushort)obj),
                    TypeCode.UInt32 => ConvertInt64((uint)obj),
                    TypeCode.UInt64 => ConvertUInt64((ulong)obj),
                    TypeCode.Single => ConvertDouble((float)obj),
                    TypeCode.Double => ConvertDouble((double)obj),
                    _ => throw ContractUtils.Unreachable,
                };
            }

            private object ConvertInt32(int obj)
            {
                unchecked
                {
                    return _to switch
                    {
                        TypeCode.Byte => (byte)obj,
                        TypeCode.SByte => (sbyte)obj,
                        TypeCode.Int16 => (short)obj,
                        TypeCode.Char => (char)obj,
                        TypeCode.Int32 => (int)obj,
                        TypeCode.Int64 => (long)obj,
                        TypeCode.UInt16 => (ushort)obj,
                        TypeCode.UInt32 => (uint)obj,
                        TypeCode.UInt64 => (ulong)obj,
                        TypeCode.Single => (float)obj,
                        TypeCode.Double => (double)obj,
                        TypeCode.Decimal => (decimal)obj,
                        TypeCode.Boolean => obj != 0,
                        _ => throw ContractUtils.Unreachable,
                    };
                }
            }

            private object ConvertInt64(long obj)
            {
                unchecked
                {
                    return _to switch
                    {
                        TypeCode.Byte => (byte)obj,
                        TypeCode.SByte => (sbyte)obj,
                        TypeCode.Int16 => (short)obj,
                        TypeCode.Char => (char)obj,
                        TypeCode.Int32 => (int)obj,
                        TypeCode.Int64 => (long)obj,
                        TypeCode.UInt16 => (ushort)obj,
                        TypeCode.UInt32 => (uint)obj,
                        TypeCode.UInt64 => (ulong)obj,
                        TypeCode.Single => (float)obj,
                        TypeCode.Double => (double)obj,
                        TypeCode.Decimal => (decimal)obj,
                        _ => throw ContractUtils.Unreachable,
                    };
                }
            }

            private object ConvertUInt64(ulong obj)
            {
                unchecked
                {
                    return _to switch
                    {
                        TypeCode.Byte => (byte)obj,
                        TypeCode.SByte => (sbyte)obj,
                        TypeCode.Int16 => (short)obj,
                        TypeCode.Char => (char)obj,
                        TypeCode.Int32 => (int)obj,
                        TypeCode.Int64 => (long)obj,
                        TypeCode.UInt16 => (ushort)obj,
                        TypeCode.UInt32 => (uint)obj,
                        TypeCode.UInt64 => (ulong)obj,
                        TypeCode.Single => (float)obj,
                        TypeCode.Double => (double)obj,
                        TypeCode.Decimal => (decimal)obj,
                        _ => throw ContractUtils.Unreachable,
                    };
                }
            }

            private object ConvertDouble(double obj)
            {
                unchecked
                {
                    return _to switch
                    {
                        TypeCode.Byte => (byte)obj,
                        TypeCode.SByte => (sbyte)obj,
                        TypeCode.Int16 => (short)obj,
                        TypeCode.Char => (char)obj,
                        TypeCode.Int32 => (int)obj,
                        TypeCode.Int64 => (long)obj,
                        TypeCode.UInt16 => (ushort)obj,
                        TypeCode.UInt32 => (uint)obj,
                        TypeCode.UInt64 => (ulong)obj,
                        TypeCode.Single => (float)obj,
                        TypeCode.Double => (double)obj,
                        TypeCode.Decimal => (decimal)obj,
                        _ => throw ContractUtils.Unreachable,
                    };
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        internal sealed class Checked : NumericConvertInstruction
        {
            public override string InstructionName => "CheckedConvert";

            public Checked(TypeCode from, TypeCode to, bool isLiftedToNull)
                : base(from, to, isLiftedToNull)
            {
            }

            protected override object Convert(object obj)
            {
                return _from switch
                {
                    TypeCode.Boolean => ConvertInt32((bool)obj ? 1 : 0),
                    TypeCode.Byte => ConvertInt32((byte)obj),
                    TypeCode.SByte => ConvertInt32((sbyte)obj),
                    TypeCode.Int16 => ConvertInt32((short)obj),
                    TypeCode.Char => ConvertInt32((char)obj),
                    TypeCode.Int32 => ConvertInt32((int)obj),
                    TypeCode.Int64 => ConvertInt64((long)obj),
                    TypeCode.UInt16 => ConvertInt32((ushort)obj),
                    TypeCode.UInt32 => ConvertInt64((uint)obj),
                    TypeCode.UInt64 => ConvertUInt64((ulong)obj),
                    TypeCode.Single => ConvertDouble((float)obj),
                    TypeCode.Double => ConvertDouble((double)obj),
                    _ => throw ContractUtils.Unreachable,
                };
            }

            private object ConvertInt32(int obj)
            {
                checked
                {
                    return _to switch
                    {
                        TypeCode.Byte => (byte)obj,
                        TypeCode.SByte => (sbyte)obj,
                        TypeCode.Int16 => (short)obj,
                        TypeCode.Char => (char)obj,
                        TypeCode.Int32 => (int)obj,
                        TypeCode.Int64 => (long)obj,
                        TypeCode.UInt16 => (ushort)obj,
                        TypeCode.UInt32 => (uint)obj,
                        TypeCode.UInt64 => (ulong)obj,
                        TypeCode.Single => (float)obj,
                        TypeCode.Double => (double)obj,
                        TypeCode.Decimal => (decimal)obj,
                        TypeCode.Boolean => obj != 0,
                        _ => throw ContractUtils.Unreachable,
                    };
                }
            }

            private object ConvertInt64(long obj)
            {
                checked
                {
                    return _to switch
                    {
                        TypeCode.Byte => (byte)obj,
                        TypeCode.SByte => (sbyte)obj,
                        TypeCode.Int16 => (short)obj,
                        TypeCode.Char => (char)obj,
                        TypeCode.Int32 => (int)obj,
                        TypeCode.Int64 => (long)obj,
                        TypeCode.UInt16 => (ushort)obj,
                        TypeCode.UInt32 => (uint)obj,
                        TypeCode.UInt64 => (ulong)obj,
                        TypeCode.Single => (float)obj,
                        TypeCode.Double => (double)obj,
                        TypeCode.Decimal => (decimal)obj,
                        _ => throw ContractUtils.Unreachable,
                    };
                }
            }

            private object ConvertUInt64(ulong obj)
            {
                checked
                {
                    return _to switch
                    {
                        TypeCode.Byte => (byte)obj,
                        TypeCode.SByte => (sbyte)obj,
                        TypeCode.Int16 => (short)obj,
                        TypeCode.Char => (char)obj,
                        TypeCode.Int32 => (int)obj,
                        TypeCode.Int64 => (long)obj,
                        TypeCode.UInt16 => (ushort)obj,
                        TypeCode.UInt32 => (uint)obj,
                        TypeCode.UInt64 => (ulong)obj,
                        TypeCode.Single => (float)obj,
                        TypeCode.Double => (double)obj,
                        TypeCode.Decimal => (decimal)obj,
                        _ => throw ContractUtils.Unreachable,
                    };
                }
            }

            private object ConvertDouble(double obj)
            {
                checked
                {
                    return _to switch
                    {
                        TypeCode.Byte => (byte)obj,
                        TypeCode.SByte => (sbyte)obj,
                        TypeCode.Int16 => (short)obj,
                        TypeCode.Char => (char)obj,
                        TypeCode.Int32 => (int)obj,
                        TypeCode.Int64 => (long)obj,
                        TypeCode.UInt16 => (ushort)obj,
                        TypeCode.UInt32 => (uint)obj,
                        TypeCode.UInt64 => (ulong)obj,
                        TypeCode.Single => (float)obj,
                        TypeCode.Double => (double)obj,
                        TypeCode.Decimal => (decimal)obj,
                        _ => throw ContractUtils.Unreachable,
                    };
                }
            }
        }

        internal sealed class ToUnderlying : NumericConvertInstruction
        {
            public override string InstructionName => "ConvertToUnderlying";

            public ToUnderlying(TypeCode to, bool isLiftedToNull)
                : base(to, to, isLiftedToNull)
            {
            }

            protected override object Convert(object obj)
            {
                unchecked
                {
                    return _to switch
                    {
                        TypeCode.Boolean => (bool)obj,
                        TypeCode.Byte => (byte)obj,
                        TypeCode.SByte => (sbyte)obj,
                        TypeCode.Int16 => (short)obj,
                        TypeCode.Char => (char)obj,
                        TypeCode.Int32 => (int)obj,
                        TypeCode.Int64 => (long)obj,
                        TypeCode.UInt16 => (ushort)obj,
                        TypeCode.UInt32 => (uint)obj,
                        TypeCode.UInt64 => (ulong)obj,
                        _ => throw ContractUtils.Unreachable,
                    };
                }
            }
        }
    }
}
