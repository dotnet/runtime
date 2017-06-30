// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.CodeDom
{
    public class CodeDelegateInvokeExpression : CodeExpression
    {
        public CodeDelegateInvokeExpression() { }

        public CodeDelegateInvokeExpression(CodeExpression targetObject)
        {
            TargetObject = targetObject;
        }

        public CodeDelegateInvokeExpression(CodeExpression targetObject, params CodeExpression[] parameters)
        {
            TargetObject = targetObject;
            Parameters.AddRange(parameters);
        }

        public CodeExpression TargetObject { get; set; }

        public CodeExpressionCollection Parameters { get; } = new CodeExpressionCollection();
    }
}
