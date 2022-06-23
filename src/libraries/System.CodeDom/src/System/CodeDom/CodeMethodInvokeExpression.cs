// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if smolloy_codedom_full_internalish
namespace System.Runtime.Serialization.CodeDom
#nullable disable
#else
namespace System.CodeDom
#endif
{
#if smolloy_codedom_full_internalish
    internal sealed class CodeMethodInvokeExpression : CodeExpression
#else
    public class CodeMethodInvokeExpression : CodeExpression
#endif
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
