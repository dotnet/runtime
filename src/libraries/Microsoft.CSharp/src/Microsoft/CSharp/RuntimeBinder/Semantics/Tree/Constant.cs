// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal sealed class ExprConstant : ExprWithType
    {
        public ExprConstant(CType type, ConstVal value)
            : base(ExpressionKind.Constant, type)
        {
            Val = value;
        }

        public Expr OptionalConstructorCall { get; set; }

        public bool IsZero => Val.IsZero(Type.ConstValKind);

        public ConstVal Val { get; }

        public ulong UInt64Value => Val.UInt64Val;

        public long Int64Value
        {
            get
            {
                switch (Type.FundamentalType)
                {
                    case FUNDTYPE.FT_I8:
                    case FUNDTYPE.FT_U8:
                        return Val.Int64Val;

                    case FUNDTYPE.FT_U4:
                        return Val.UInt32Val;

                    default:
                        Debug.Assert(
                            Type.FundamentalType == FUNDTYPE.FT_I1 || Type.FundamentalType == FUNDTYPE.FT_I2
                            || Type.FundamentalType == FUNDTYPE.FT_I4 || Type.FundamentalType == FUNDTYPE.FT_U1
                            || Type.FundamentalType == FUNDTYPE.FT_U2, "Bad fundType in getI64Value");
                        return Val.Int32Val;
                }
            }
        }

        public override object Object
        {
            [RequiresUnreferencedCode(Binder.TrimmerWarning)]
            get
            {
                if (Type is NullType)
                {
                    return null;
                }

                object objval = System.Type.GetTypeCode(Type.AssociatedSystemType) switch
                {
                    TypeCode.Boolean => Val.BooleanVal,
                    TypeCode.SByte => Val.SByteVal,
                    TypeCode.Byte => Val.ByteVal,
                    TypeCode.Int16 => Val.Int16Val,
                    TypeCode.UInt16 => Val.UInt16Val,
                    TypeCode.Int32 => Val.Int32Val,
                    TypeCode.UInt32 => Val.UInt32Val,
                    TypeCode.Int64 => Val.Int64Val,
                    TypeCode.UInt64 => Val.UInt64Val,
                    TypeCode.Single => Val.SingleVal,
                    TypeCode.Double => Val.DoubleVal,
                    TypeCode.Decimal => Val.DecimalVal,
                    TypeCode.Char => Val.CharVal,
                    TypeCode.String => Val.StringVal,
                    _ => Val.ObjectVal,
                };

                return Type.IsEnumType ? Enum.ToObject(Type.AssociatedSystemType, objval) : objval;
            }
        }
    }
}
