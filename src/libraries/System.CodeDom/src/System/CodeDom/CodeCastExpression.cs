// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.CodeDom
{
    public class CodeCastExpression : CodeExpression
    {
        public CodeCastExpression() { }

        public CodeCastExpression(CodeTypeReference targetType, CodeExpression expression)
        {
            TargetType = targetType;
            Expression = expression;
        }

        public CodeCastExpression(string targetType, CodeExpression expression)
        {
            TargetType = new CodeTypeReference(targetType);
            Expression = expression;
        }

        public CodeCastExpression(Type targetType, CodeExpression expression)
        {
            TargetType = new CodeTypeReference(targetType);
            Expression = expression;
        }

        public CodeTypeReference TargetType
        {
            get => field ??= new CodeTypeReference("");
            set => field = value;
        }

        public CodeExpression Expression { get; set; }
    }
}
