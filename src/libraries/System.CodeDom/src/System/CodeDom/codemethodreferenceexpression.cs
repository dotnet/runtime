// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.CodeDom
{
    public class CodeMethodReferenceExpression : CodeExpression
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
            get { return _methodName ?? string.Empty; }
            set { _methodName = value; }
        }

        public CodeTypeReferenceCollection TypeArguments => _typeArguments ?? (_typeArguments = new CodeTypeReferenceCollection());
    }
}
