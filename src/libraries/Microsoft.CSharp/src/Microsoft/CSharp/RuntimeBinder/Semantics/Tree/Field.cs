// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal sealed class ExprField : ExprWithType
    {
        public ExprField(CType type, Expr optionalObject, FieldWithType field)
            : base(ExpressionKind.Field, type)
        {
            Flags = field.Field().isReadOnly ? 0 : EXPRFLAG.EXF_LVALUE;
            OptionalObject = optionalObject;
            FieldWithType = field;
        }

        public Expr OptionalObject { get; set; }

        public FieldWithType FieldWithType { get; }
    }
}
