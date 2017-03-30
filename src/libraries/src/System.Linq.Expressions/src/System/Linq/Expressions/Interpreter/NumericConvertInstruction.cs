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
                switch (_from)
                {
                    case TypeCode.Boolean: return ConvertInt32((bool)obj ? 1 : 0);
                    case TypeCode.Byte: return ConvertInt32((byte)obj);
                    case TypeCode.SByte: return ConvertInt32((sbyte)obj);
                    case TypeCode.Int16: return ConvertInt32((short)obj);
                    case TypeCode.Char: return ConvertInt32((char)obj);
                    case TypeCode.Int32: return ConvertInt32((int)obj);
                    case TypeCode.Int64: return ConvertInt64((long)obj);
                    case TypeCode.UInt16: return ConvertInt32((ushort)obj);
                    case TypeCode.UInt32: return ConvertInt64((uint)obj);
                    case TypeCode.UInt64: return ConvertUInt64((ulong)obj);
                    case TypeCode.Single: return ConvertDouble((float)obj);
                    case TypeCode.Double: return ConvertDouble((double)obj);
                    default: throw ContractUtils.Unreachable;
                }
            }

            private object ConvertInt32(int obj)
            {
                unchecked
                {
                    switch (_to)
                    {
                        case TypeCode.Byte: return (byte)obj;
                        case TypeCode.SByte: return (sbyte)obj;
                        case TypeCode.Int16: return (short)obj;
                        case TypeCode.Char: return (char)obj;
                        case TypeCode.Int32: return (int)obj;
                        case TypeCode.Int64: return (long)obj;
                        case TypeCode.UInt16: return (ushort)obj;
                        case TypeCode.UInt32: return (uint)obj;
                        case TypeCode.UInt64: return (ulong)obj;
                        case TypeCode.Single: return (float)obj;
                        case TypeCode.Double: return (double)obj;
                        case TypeCode.Decimal: return (decimal)obj;
                        case TypeCode.Boolean: return obj != 0;
                        default: throw ContractUtils.Unreachable;
                    }
                }
            }

            private object ConvertInt64(long obj)
            {
                unchecked
                {
                    switch (_to)
                    {
                        case TypeCode.Byte: return (byte)obj;
                        case TypeCode.SByte: return (sbyte)obj;
                        case TypeCode.Int16: return (short)obj;
                        case TypeCode.Char: return (char)obj;
                        case TypeCode.Int32: return (int)obj;
                        case TypeCode.Int64: return (long)obj;
                        case TypeCode.UInt16: return (ushort)obj;
                        case TypeCode.UInt32: return (uint)obj;
                        case TypeCode.UInt64: return (ulong)obj;
                        case TypeCode.Single: return (float)obj;
                        case TypeCode.Double: return (double)obj;
                        case TypeCode.Decimal: return (decimal)obj;
                        default: throw ContractUtils.Unreachable;
                    }
                }
            }

            private object ConvertUInt64(ulong obj)
            {
                unchecked
                {
                    switch (_to)
                    {
                        case TypeCode.Byte: return (byte)obj;
                        case TypeCode.SByte: return (sbyte)obj;
                        case TypeCode.Int16: return (short)obj;
                        case TypeCode.Char: return (char)obj;
                        case TypeCode.Int32: return (int)obj;
                        case TypeCode.Int64: return (long)obj;
                        case TypeCode.UInt16: return (ushort)obj;
                        case TypeCode.UInt32: return (uint)obj;
                        case TypeCode.UInt64: return (ulong)obj;
                        case TypeCode.Single: return (float)obj;
                        case TypeCode.Double: return (double)obj;
                        case TypeCode.Decimal: return (decimal)obj;
                        default: throw ContractUtils.Unreachable;
                    }
                }
            }

            private object ConvertDouble(double obj)
            {
                unchecked
                {
                    switch (_to)
                    {
                        case TypeCode.Byte: return (byte)obj;
                        case TypeCode.SByte: return (sbyte)obj;
                        case TypeCode.Int16: return (short)obj;
                        case TypeCode.Char: return (char)obj;
                        case TypeCode.Int32: return (int)obj;
                        case TypeCode.Int64: return (long)obj;
                        case TypeCode.UInt16: return (ushort)obj;
                        case TypeCode.UInt32: return (uint)obj;
                        case TypeCode.UInt64: return (ulong)obj;
                        case TypeCode.Single: return (float)obj;
                        case TypeCode.Double: return (double)obj;
                        case TypeCode.Decimal: return (decimal)obj;
                        default: throw ContractUtils.Unreachable;
                    }
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
                switch (_from)
                {
                    case TypeCode.Boolean: return ConvertInt32((bool)obj ? 1 : 0);
                    case TypeCode.Byte: return ConvertInt32((byte)obj);
                    case TypeCode.SByte: return ConvertInt32((sbyte)obj);
                    case TypeCode.Int16: return ConvertInt32((short)obj);
                    case TypeCode.Char: return ConvertInt32((char)obj);
                    case TypeCode.Int32: return ConvertInt32((int)obj);
                    case TypeCode.Int64: return ConvertInt64((long)obj);
                    case TypeCode.UInt16: return ConvertInt32((ushort)obj);
                    case TypeCode.UInt32: return ConvertInt64((uint)obj);
                    case TypeCode.UInt64: return ConvertUInt64((ulong)obj);
                    case TypeCode.Single: return ConvertDouble((float)obj);
                    case TypeCode.Double: return ConvertDouble((double)obj);
                    default: throw ContractUtils.Unreachable;
                }
            }

            private object ConvertInt32(int obj)
            {
                checked
                {
                    switch (_to)
                    {
                        case TypeCode.Byte: return (byte)obj;
                        case TypeCode.SByte: return (sbyte)obj;
                        case TypeCode.Int16: return (short)obj;
                        case TypeCode.Char: return (char)obj;
                        case TypeCode.Int32: return (int)obj;
                        case TypeCode.Int64: return (long)obj;
                        case TypeCode.UInt16: return (ushort)obj;
                        case TypeCode.UInt32: return (uint)obj;
                        case TypeCode.UInt64: return (ulong)obj;
                        case TypeCode.Single: return (float)obj;
                        case TypeCode.Double: return (double)obj;
                        case TypeCode.Decimal: return (decimal)obj;
                        case TypeCode.Boolean: return obj != 0;
                        default: throw ContractUtils.Unreachable;
                    }
                }
            }

            private object ConvertInt64(long obj)
            {
                checked
                {
                    switch (_to)
                    {
                        case TypeCode.Byte: return (byte)obj;
                        case TypeCode.SByte: return (sbyte)obj;
                        case TypeCode.Int16: return (short)obj;
                        case TypeCode.Char: return (char)obj;
                        case TypeCode.Int32: return (int)obj;
                        case TypeCode.Int64: return (long)obj;
                        case TypeCode.UInt16: return (ushort)obj;
                        case TypeCode.UInt32: return (uint)obj;
                        case TypeCode.UInt64: return (ulong)obj;
                        case TypeCode.Single: return (float)obj;
                        case TypeCode.Double: return (double)obj;
                        case TypeCode.Decimal: return (decimal)obj;
                        default: throw ContractUtils.Unreachable;
                    }
                }
            }

            private object ConvertUInt64(ulong obj)
            {
                checked
                {
                    switch (_to)
                    {
                        case TypeCode.Byte: return (byte)obj;
                        case TypeCode.SByte: return (sbyte)obj;
                        case TypeCode.Int16: return (short)obj;
                        case TypeCode.Char: return (char)obj;
                        case TypeCode.Int32: return (int)obj;
                        case TypeCode.Int64: return (long)obj;
                        case TypeCode.UInt16: return (ushort)obj;
                        case TypeCode.UInt32: return (uint)obj;
                        case TypeCode.UInt64: return (ulong)obj;
                        case TypeCode.Single: return (float)obj;
                        case TypeCode.Double: return (double)obj;
                        case TypeCode.Decimal: return (decimal)obj;
                        default: throw ContractUtils.Unreachable;
                    }
                }
            }

            private object ConvertDouble(double obj)
            {
                checked
                {
                    switch (_to)
                    {
                        case TypeCode.Byte: return (byte)obj;
                        case TypeCode.SByte: return (sbyte)obj;
                        case TypeCode.Int16: return (short)obj;
                        case TypeCode.Char: return (char)obj;
                        case TypeCode.Int32: return (int)obj;
                        case TypeCode.Int64: return (long)obj;
                        case TypeCode.UInt16: return (ushort)obj;
                        case TypeCode.UInt32: return (uint)obj;
                        case TypeCode.UInt64: return (ulong)obj;
                        case TypeCode.Single: return (float)obj;
                        case TypeCode.Double: return (double)obj;
                        case TypeCode.Decimal: return (decimal)obj;
                        default: throw ContractUtils.Unreachable;
                    }
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
                    switch (_to)
                    {
                        case TypeCode.Boolean: return (bool)obj;
                        case TypeCode.Byte: return (byte)obj;
                        case TypeCode.SByte: return (sbyte)obj;
                        case TypeCode.Int16: return (short)obj;
                        case TypeCode.Char: return (char)obj;
                        case TypeCode.Int32: return (int)obj;
                        case TypeCode.Int64: return (long)obj;
                        case TypeCode.UInt16: return (ushort)obj;
                        case TypeCode.UInt32: return (uint)obj;
                        case TypeCode.UInt64: return (ulong)obj;
                        default: throw ContractUtils.Unreachable;
                    }
                }
            }
        }
    }
}