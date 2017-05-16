// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            get { return _type ?? (_type = new CodeTypeReference("")); }
            set { _type = value; }
        }
    }
}
