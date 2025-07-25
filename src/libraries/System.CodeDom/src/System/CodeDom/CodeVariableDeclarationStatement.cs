// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.CodeDom
{
    public class CodeVariableDeclarationStatement : CodeStatement
    {
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
            get => field ?? string.Empty;
            set => field = value;
        }

        public CodeTypeReference Type
        {
            get => field ??= new CodeTypeReference("");
            set => field = value;
        }
    }
}
