// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.CodeDom
{
    public class CodeMethodInvokeExpression : CodeExpression
    {
        private CodeMethodReferenceExpression _method;

        public CodeMethodInvokeExpression() { }

        public CodeMethodInvokeExpression(CodeMethodReferenceExpression method, params CodeExpression[] parameters)
        {
            _method = method;
            Parameters.AddRange(parameters);
        }

        public CodeMethodInvokeExpression(CodeExpression targetObject, string methodName, params CodeExpression[] parameters)
        {
            _method = new CodeMethodReferenceExpression(targetObject, methodName);
            Parameters.AddRange(parameters);
        }

        public CodeMethodReferenceExpression Method
        {
            get => _method ??= new CodeMethodReferenceExpression();
            set => _method = value;
        }

        public CodeExpressionCollection Parameters { get; } = new CodeExpressionCollection();
    }
}
