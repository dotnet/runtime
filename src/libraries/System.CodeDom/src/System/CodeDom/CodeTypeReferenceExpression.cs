// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.CodeDom
{
    public class CodeTypeReferenceExpression : CodeExpression
    {
        private CodeTypeReference _type;

        public CodeTypeReferenceExpression() { }

        public CodeTypeReferenceExpression(CodeTypeReference type)
        {
            Type = type;
        }

        public CodeTypeReferenceExpression(string type)
        {
            Type = new CodeTypeReference(type);
        }

        public CodeTypeReferenceExpression(Type type)
        {
            Type = new CodeTypeReference(type);
        }

        public CodeTypeReference Type
        {
            get => _type ??= new CodeTypeReference("");
            set => _type = value;
        }
    }
}
