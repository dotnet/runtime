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
    internal sealed class CodeMethodReferenceExpression : CodeExpression
#else
    public class CodeMethodReferenceExpression : CodeExpression
#endif
    {
        private string _methodName;
        private CodeTypeReferenceCollection _typeArguments;

        public CodeMethodReferenceExpression() { }

        public CodeMethodReferenceExpression(CodeExpression targetObject, string methodName)
        {
            TargetObject = targetObject;
            MethodName = methodName;
        }

        public CodeMethodReferenceExpression(CodeExpression targetObject, string methodName, params CodeTypeReference[] typeParameters)
        {
            TargetObject = targetObject;
            MethodName = methodName;
            if (typeParameters != null && typeParameters.Length > 0)
            {
                TypeArguments.AddRange(typeParameters);
            }
        }

        public CodeExpression TargetObject { get; set; }

        public string MethodName
        {
            get => _methodName ?? string.Empty;
            set => _methodName = value;
        }

        public CodeTypeReferenceCollection TypeArguments => _typeArguments ??= new CodeTypeReferenceCollection();
    }
}
