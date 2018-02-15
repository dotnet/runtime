// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

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
            get
            {
                if (Type is NullType)
                {
                    return null;
                }

                object objval;
                switch (System.Type.GetTypeCode(Type.AssociatedSystemType))
                {
                    case TypeCode.Boolean:
                        objval = Val.BooleanVal;
                        break;
                    case TypeCode.SByte:
                        objval = Val.SByteVal;
                        break;
                    case TypeCode.Byte:
                        objval = Val.ByteVal;
                        break;
                    case TypeCode.Int16:
                        objval = Val.Int16Val;
                        break;
                    case TypeCode.UInt16:
                        objval = Val.UInt16Val;
                        break;
                    case TypeCode.Int32:
                        objval = Val.Int32Val;
                        break;
                    case TypeCode.UInt32:
                        objval = Val.UInt32Val;
                        break;
                    case TypeCode.Int64:
                        objval = Val.Int64Val;
                        break;
                    case TypeCode.UInt64:
                        objval = Val.UInt64Val;
                        break;
                    case TypeCode.Single:
                        objval = Val.SingleVal;
                        break;
                    case TypeCode.Double:
                        objval = Val.DoubleVal;
                        break;
                    case TypeCode.Decimal:
                        objval = Val.DecimalVal;
                        break;
                    case TypeCode.Char:
                        objval = Val.CharVal;
                        break;
                    case TypeCode.String:
                        objval = Val.StringVal;
                        break;
                    default:
                        objval = Val.ObjectVal;
                        break;
                }

                return Type.IsEnumType ? Enum.ToObject(Type.AssociatedSystemType, objval) : objval;
            }
        }
    }
}
