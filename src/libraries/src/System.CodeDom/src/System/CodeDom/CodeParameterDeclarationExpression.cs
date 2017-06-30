// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.CodeDom
{
    public class CodeParameterDeclarationExpression : CodeExpression
    {
        private CodeTypeReference _type;
        private string _name;
        private CodeAttributeDeclarationCollection _customAttributes;

        public CodeParameterDeclarationExpression() { }

        public CodeParameterDeclarationExpression(CodeTypeReference type, string name)
        {
            Type = type;
            Name = name;
        }

        public CodeParameterDeclarationExpression(string type, string name)
        {
            Type = new CodeTypeReference(type);
            Name = name;
        }

        public CodeParameterDeclarationExpression(Type type, string name)
        {
            Type = new CodeTypeReference(type);
            Name = name;
        }

        public CodeAttributeDeclarationCollection CustomAttributes
        {
            get { return _customAttributes ?? (_customAttributes = new CodeAttributeDeclarationCollection()); }
            set { _customAttributes = value; }
        }

        public FieldDirection Direction { get; set; } = FieldDirection.In;

        public CodeTypeReference Type
        {
            get { return _type ?? (_type = new CodeTypeReference("")); }
            set { _type = value; }
        }

        public string Name
        {
            get { return _name ?? string.Empty; }
            set { _name = value; }
        }
    }
}
