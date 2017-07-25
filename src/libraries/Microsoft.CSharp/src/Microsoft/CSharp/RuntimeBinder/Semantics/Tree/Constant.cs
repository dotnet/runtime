// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        public bool IsZero => Val.IsZero(Type.constValKind());

        public ConstVal Val { get; }

        public ulong UInt64Value => Val.UInt64Val;

        public long Int64Value
        {
            get
            {
                switch (Type.fundType())
                {
                    case FUNDTYPE.FT_I8:
                    case FUNDTYPE.FT_U8:
                        return Val.Int64Val;
                    case FUNDTYPE.FT_U4:
                        return Val.UInt32Val;
                    case FUNDTYPE.FT_I1:
                    case FUNDTYPE.FT_I2:
                    case FUNDTYPE.FT_I4:
                    case FUNDTYPE.FT_U1:
                    case FUNDTYPE.FT_U2:
                        return Val.Int32Val;
                    default:
                        Debug.Assert(false, "Bad fundType in getI64Value");
                        return 0;
                }
            }
        }
    }
}
