// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.CodeDom
{
    public class CodeDirectionExpression : CodeExpression
    {
        public CodeDirectionExpression() { }

        public CodeDirectionExpression(FieldDirection direction, CodeExpression expression)
        {
            Expression = expression;
            Direction = direction;
        }

        public CodeExpression Expression { get; set; }

        public FieldDirection Direction { get; set; }
    }
}
