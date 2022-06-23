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
    internal sealed class CodeParameterDeclarationExpression : CodeExpression
#else
    public class CodeParameterDeclarationExpression : CodeExpression
#endif
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
            get => _customAttributes ??= new CodeAttributeDeclarationCollection();
            set => _customAttributes = value;
        }

        public FieldDirection Direction { get; set; } = FieldDirection.In;

        public CodeTypeReference Type
        {
            get => _type ??= new CodeTypeReference("");
            set => _type = value;
        }

        public string Name
        {
            get => _name ?? string.Empty;
            set => _name = value;
        }
    }
}
