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
    internal sealed class CodeVariableDeclarationStatement : CodeStatement
#else
    public class CodeVariableDeclarationStatement : CodeStatement
#endif
    {
        private CodeTypeReference _type;
        private string _name;

        public CodeVariableDeclarationStatement() { }

        public CodeVariableDeclarationStatement(CodeTypeReference type, string name)
        {
            Type = type;
            Name = name;
        }

        public CodeVariableDeclarationStatement(string type, string name)
        {
            Type = new CodeTypeReference(type);
            Name = name;
        }

        public CodeVariableDeclarationStatement(Type type, string name)
        {
            Type = new CodeTypeReference(type);
            Name = name;
        }

        public CodeVariableDeclarationStatement(CodeTypeReference type, string name, CodeExpression initExpression)
        {
            Type = type;
            Name = name;
            InitExpression = initExpression;
        }

        public CodeVariableDeclarationStatement(string type, string name, CodeExpression initExpression)
        {
            Type = new CodeTypeReference(type);
            Name = name;
            InitExpression = initExpression;
        }

        public CodeVariableDeclarationStatement(Type type, string name, CodeExpression initExpression)
        {
            Type = new CodeTypeReference(type);
            Name = name;
            InitExpression = initExpression;
        }

        public CodeExpression InitExpression { get; set; }

        public string Name
        {
            get => _name ?? string.Empty;
            set => _name = value;
        }

        public CodeTypeReference Type
        {
            get => _type ??= new CodeTypeReference("");
            set => _type = value;
        }
    }
}
