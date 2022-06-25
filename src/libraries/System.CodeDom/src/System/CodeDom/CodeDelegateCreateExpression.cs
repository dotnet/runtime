// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.CodeDom
{
    public class CodeDelegateCreateExpression : CodeExpression
    {
        private CodeTypeReference _delegateType;
        private string _methodName;

        public CodeDelegateCreateExpression() { }

        public CodeDelegateCreateExpression(CodeTypeReference delegateType, CodeExpression targetObject, string methodName)
        {
            _delegateType = delegateType;
            TargetObject = targetObject;
            _methodName = methodName;
        }

        public CodeTypeReference DelegateType
        {
            get => _delegateType ??= new CodeTypeReference("");
            set => _delegateType = value;
        }

        public CodeExpression TargetObject { get; set; }

        public string MethodName
        {
            get => _methodName ?? string.Empty;
            set => _methodName = value;
        }
    }
}
