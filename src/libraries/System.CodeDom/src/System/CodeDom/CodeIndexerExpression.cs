// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.CodeDom
{
    public class CodeIndexerExpression : CodeExpression
    {
        public CodeIndexerExpression() { }

        public CodeIndexerExpression(CodeExpression targetObject, params CodeExpression[] indices)
        {
            TargetObject = targetObject;
            Indices.AddRange(indices);
        }

        public CodeExpression TargetObject { get; set; }

        public CodeExpressionCollection Indices => field ??= new CodeExpressionCollection();
    }
}
