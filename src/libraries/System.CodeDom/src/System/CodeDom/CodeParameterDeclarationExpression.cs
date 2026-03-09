// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.CodeDom
{
    public class CodeParameterDeclarationExpression : CodeExpression
    {
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
            get => field ??= new CodeAttributeDeclarationCollection();
            set => field = value;
        }

        public FieldDirection Direction { get; set; } = FieldDirection.In;

        public CodeTypeReference Type
        {
            get => field ??= new CodeTypeReference("");
            set => field = value;
        }

        public string Name
        {
            get => field ?? string.Empty;
            set => field = value;
        }
    }
}
