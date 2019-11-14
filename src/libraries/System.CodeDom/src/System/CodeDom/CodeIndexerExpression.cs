// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.CodeDom
{
    public class CodeIndexerExpression : CodeExpression
    {
        private CodeExpressionCollection _indices;

        public CodeIndexerExpression() { }

        public CodeIndexerExpression(CodeExpression targetObject, params CodeExpression[] indices)
        {
            TargetObject = targetObject;
            Indices.AddRange(indices);
        }

        public CodeExpression TargetObject { get; set; }

        public CodeExpressionCollection Indices => _indices ?? (_indices = new CodeExpressionCollection());
    }
}
