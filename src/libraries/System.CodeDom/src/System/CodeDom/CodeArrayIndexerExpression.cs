// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.CodeDom
{
    public class CodeArrayIndexerExpression : CodeExpression
    {
        private CodeExpressionCollection _indices;

        public CodeArrayIndexerExpression() { }

        public CodeArrayIndexerExpression(CodeExpression targetObject, params CodeExpression[] indices)
        {
            TargetObject = targetObject;
            Indices.AddRange(indices);
        }

        public CodeExpression TargetObject { get; set; }

        public CodeExpressionCollection Indices => _indices ??= new CodeExpressionCollection();
    }
}
