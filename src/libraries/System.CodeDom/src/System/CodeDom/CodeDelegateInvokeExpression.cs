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
    internal sealed class CodeDelegateInvokeExpression : CodeExpression
#else
    public class CodeDelegateInvokeExpression : CodeExpression
#endif
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
