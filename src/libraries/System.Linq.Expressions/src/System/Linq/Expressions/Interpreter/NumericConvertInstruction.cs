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
                    converted = (int)(int?)obj;
                    throw ContractUtils.Unreachable;
                }
            }
            else
            {
                converted = Convert(obj);
            }

            frame.Push(converted);
            return +1;
        }

        protected abstract object Convert(object obj);

        public override string InstructionName
        {
            get { return "NumericConvert"; }
        }
        public override int ConsumedStack { get { return 1; } }
        public override int ProducedStack { get { return 1; } }

        public override string ToString()
        {
            return InstructionName + "(" + _from + "->" + _to + ")";
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        internal sealed class Unchecked : NumericConvertInstruction
        {
            public override string InstructionName { get { return "UncheckedConvert"; } }

            public Unchecked(TypeCode from, TypeCode to, bool isLiftedToNull)
                : base(from, to, isLiftedToNull)
            {
            }

            protected override object Convert(object obj)
            {
                switch (_from)
                {
                    case TypeCode.Boolean: return ConvertInt32((Boolean)obj ? 1 : 0);
                    case TypeCode.Byte: return ConvertInt32((Byte)obj);
                    case TypeCode.SByte: return ConvertInt32((SByte)obj);
                    case TypeCode.Int16: return ConvertInt32((Int16)obj);
                    case TypeCode.Char: return ConvertInt32((Char)obj);
                    case TypeCode.Int32: return ConvertInt32((Int32)obj);
                    case TypeCode.Int64: return ConvertInt64((Int64)obj);
                    case TypeCode.UInt16: return ConvertInt32((UInt16)obj);
                    case TypeCode.UInt32: return ConvertInt64((UInt32)obj);
                    case TypeCode.UInt64: return ConvertUInt64((UInt64)obj);
                    case TypeCode.Single: return ConvertDouble((Single)obj);
                    case TypeCode.Double: return ConvertDouble((Double)obj);
                    default: throw ContractUtils.Unreachable;
                }
            }

            private object ConvertInt32(int obj)
            {
                unchecked
                {
                    switch (_to)
                    {
                        case TypeCode.Byte: return (Byte)obj;
                        case TypeCode.SByte: return (SByte)obj;
                        case TypeCode.Int16: return (Int16)obj;
                        case TypeCode.Char: return (Char)obj;
                        case TypeCode.Int32: return (Int32)obj;
                        case TypeCode.Int64: return (Int64)obj;
                        case TypeCode.UInt16: return (UInt16)obj;
                        case TypeCode.UInt32: return (UInt32)obj;
                        case TypeCode.UInt64: return (UInt64)obj;
                        case TypeCode.Single: return (Single)obj;
                        case TypeCode.Double: return (Double)obj;
                        case TypeCode.Decimal: return (Decimal)obj;
                        default: throw ContractUtils.Unreachable;
                    }
                }
            }

            private object ConvertInt64(Int64 obj)
            {
                unchecked
                {
                    switch (_to)
                    {
                        case TypeCode.Byte: return (Byte)obj;
                        case TypeCode.SByte: return (SByte)obj;
                        case TypeCode.Int16: return (Int16)obj;
                        case TypeCode.Char: return (Char)obj;
                        case TypeCode.Int32: return (Int32)obj;
                        case TypeCode.Int64: return (Int64)obj;
                        case TypeCode.UInt16: return (UInt16)obj;
                        case TypeCode.UInt32: return (UInt32)obj;
                        case TypeCode.UInt64: return (UInt64)obj;
                        case TypeCode.Single: return (Single)obj;
                        case TypeCode.Double: return (Double)obj;
                        case TypeCode.Decimal: return (Decimal)obj;
                        default: throw ContractUtils.Unreachable;
                    }
                }
            }

            private object ConvertUInt64(UInt64 obj)
            {
                unchecked
                {
                    switch (_to)
                    {
                        case TypeCode.Byte: return (Byte)obj;
                        case TypeCode.SByte: return (SByte)obj;
                        case TypeCode.Int16: return (Int16)obj;
                        case TypeCode.Char: return (Char)obj;
                        case TypeCode.Int32: return (Int32)obj;
                        case TypeCode.Int64: return (Int64)obj;
                        case TypeCode.UInt16: return (UInt16)obj;
                        case TypeCode.UInt32: return (UInt32)obj;
                        case TypeCode.UInt64: return (UInt64)obj;
                        case TypeCode.Single: return (Single)obj;
                        case TypeCode.Double: return (Double)obj;
                        case TypeCode.Decimal: return (Decimal)obj;
                        default: throw ContractUtils.Unreachable;
                    }
                }
            }

            private object ConvertDouble(Double obj)
            {
                unchecked
                {
                    switch (_to)
                    {
                        case TypeCode.Byte: return (Byte)obj;
                        case TypeCode.SByte: return (SByte)obj;
                        case TypeCode.Int16: return (Int16)obj;
                        case TypeCode.Char: return (Char)obj;
                        case TypeCode.Int32: return (Int32)obj;
                        case TypeCode.Int64: return (Int64)obj;
                        case TypeCode.UInt16: return (UInt16)obj;
                        case TypeCode.UInt32: return (UInt32)obj;
                        case TypeCode.UInt64: return (UInt64)obj;
                        case TypeCode.Single: return (Single)obj;
                        case TypeCode.Double: return (Double)obj;
                        case TypeCode.Decimal: return (Decimal)obj;
                        default: throw ContractUtils.Unreachable;
                    }
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        internal sealed class Checked : NumericConvertInstruction
        {
            public override string InstructionName { get { return "CheckedConvert"; } }

            public Checked(TypeCode from, TypeCode to, bool isLiftedToNull)
                : base(from, to, isLiftedToNull)
            {
            }

            protected override object Convert(object obj)
            {
                switch (_from)
                {
                    case TypeCode.Boolean: return ConvertInt32((Boolean)obj ? 1 : 0);
                    case TypeCode.Byte: return ConvertInt32((Byte)obj);
                    case TypeCode.SByte: return ConvertInt32((SByte)obj);
                    case TypeCode.Int16: return ConvertInt32((Int16)obj);
                    case TypeCode.Char: return ConvertInt32((Char)obj);
                    case TypeCode.Int32: return ConvertInt32((Int32)obj);
                    case TypeCode.Int64: return ConvertInt64((Int64)obj);
                    case TypeCode.UInt16: return ConvertInt32((UInt16)obj);
                    case TypeCode.UInt32: return ConvertInt64((UInt32)obj);
                    case TypeCode.UInt64: return ConvertUInt64((UInt64)obj);
                    case TypeCode.Single: return ConvertDouble((Single)obj);
                    case TypeCode.Double: return ConvertDouble((Double)obj);
                    default: throw ContractUtils.Unreachable;
                }
            }

            private object ConvertInt32(int obj)
            {
                checked
                {
                    switch (_to)
                    {
                        case TypeCode.Byte: return (Byte)obj;
                        case TypeCode.SByte: return (SByte)obj;
                        case TypeCode.Int16: return (Int16)obj;
                        case TypeCode.Char: return (Char)obj;
                        case TypeCode.Int32: return (Int32)obj;
                        case TypeCode.Int64: return (Int64)obj;
                        case TypeCode.UInt16: return (UInt16)obj;
                        case TypeCode.UInt32: return (UInt32)obj;
                        case TypeCode.UInt64: return (UInt64)obj;
                        case TypeCode.Single: return (Single)obj;
                        case TypeCode.Double: return (Double)obj;
                        case TypeCode.Decimal: return (Decimal)obj;
                        default: throw ContractUtils.Unreachable;
                    }
                }
            }

            private object ConvertInt64(Int64 obj)
            {
                checked
                {
                    switch (_to)
                    {
                        case TypeCode.Byte: return (Byte)obj;
                        case TypeCode.SByte: return (SByte)obj;
                        case TypeCode.Int16: return (Int16)obj;
                        case TypeCode.Char: return (Char)obj;
                        case TypeCode.Int32: return (Int32)obj;
                        case TypeCode.Int64: return (Int64)obj;
                        case TypeCode.UInt16: return (UInt16)obj;
                        case TypeCode.UInt32: return (UInt32)obj;
                        case TypeCode.UInt64: return (UInt64)obj;
                        case TypeCode.Single: return (Single)obj;
                        case TypeCode.Double: return (Double)obj;
                        case TypeCode.Decimal: return (Decimal)obj;
                        default: throw ContractUtils.Unreachable;
                    }
                }
            }

            private object ConvertUInt64(UInt64 obj)
            {
                checked
                {
                    switch (_to)
                    {
                        case TypeCode.Byte: return (Byte)obj;
                        case TypeCode.SByte: return (SByte)obj;
                        case TypeCode.Int16: return (Int16)obj;
                        case TypeCode.Char: return (Char)obj;
                        case TypeCode.Int32: return (Int32)obj;
                        case TypeCode.Int64: return (Int64)obj;
                        case TypeCode.UInt16: return (UInt16)obj;
                        case TypeCode.UInt32: return (UInt32)obj;
                        case TypeCode.UInt64: return (UInt64)obj;
                        case TypeCode.Single: return (Single)obj;
                        case TypeCode.Double: return (Double)obj;
                        case TypeCode.Decimal: return (Decimal)obj;
                        default: throw ContractUtils.Unreachable;
                    }
                }
            }

            private object ConvertDouble(Double obj)
            {
                checked
                {
                    switch (_to)
                    {
                        case TypeCode.Byte: return (Byte)obj;
                        case TypeCode.SByte: return (SByte)obj;
                        case TypeCode.Int16: return (Int16)obj;
                        case TypeCode.Char: return (Char)obj;
                        case TypeCode.Int32: return (Int32)obj;
                        case TypeCode.Int64: return (Int64)obj;
                        case TypeCode.UInt16: return (UInt16)obj;
                        case TypeCode.UInt32: return (UInt32)obj;
                        case TypeCode.UInt64: return (UInt64)obj;
                        case TypeCode.Single: return (Single)obj;
                        case TypeCode.Double: return (Double)obj;
                        case TypeCode.Decimal: return (Decimal)obj;
                        default: throw ContractUtils.Unreachable;
                    }
                }
            }
        }
    }
}
