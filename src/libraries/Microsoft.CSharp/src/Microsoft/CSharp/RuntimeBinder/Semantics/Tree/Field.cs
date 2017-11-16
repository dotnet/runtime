// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal sealed class ExprField : ExprWithType, IExprWithObject
    {
        public ExprField(CType type, Expr optionalObject, FieldWithType field, bool isLValue)
            : base(ExpressionKind.Field, type)
        {
            Flags = isLValue ? EXPRFLAG.EXF_LVALUE : 0;
            OptionalObject = optionalObject;
            FieldWithType = field;
        }

        public Expr OptionalObject { get; set; }

        public FieldWithType FieldWithType { get; }
    }
}
