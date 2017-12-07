// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal sealed partial class ExpressionBinder
    {
        private sealed class BinOpArgInfo
        {
            public BinOpArgInfo(Expr op1, Expr op2)
            {
                Debug.Assert(op1 != null);
                Debug.Assert(op2 != null);
                arg1 = op1;
                arg2 = op2;
                type1 = arg1.Type;
                type2 = arg2.Type;
                typeRaw1 = type1.StripNubs();
                typeRaw2 = type2.StripNubs();
                pt1 = type1.isPredefined() ? type1.getPredefType() : PredefinedType.PT_COUNT;
                pt2 = type2.isPredefined() ? type2.getPredefType() : PredefinedType.PT_COUNT;
                ptRaw1 = typeRaw1.isPredefined() ? typeRaw1.getPredefType() : PredefinedType.PT_COUNT;
                ptRaw2 = typeRaw2.isPredefined() ? typeRaw2.getPredefType() : PredefinedType.PT_COUNT;
            }

            public Expr arg1;
            public Expr arg2;
            public PredefinedType pt1;
            public PredefinedType pt2;
            public PredefinedType ptRaw1;
            public PredefinedType ptRaw2;
            public CType type1;
            public CType type2;
            public CType typeRaw1;
            public CType typeRaw2;
            public BinOpKind binopKind;
            public BinOpMask mask;

            public bool ValidForDelegate()
            {
                return (mask & BinOpMask.Delegate) != 0;
            }

            public bool ValidForEnumAndUnderlyingType()
            {
                return (mask & BinOpMask.EnumUnder) != 0;
            }

            public bool ValidForUnderlyingTypeAndEnum()
            {
                return (mask & BinOpMask.UnderEnum) != 0;
            }

            public bool ValidForEnum()
            {
                return (mask & BinOpMask.Enum) != 0;
            }
        }
    }
}
