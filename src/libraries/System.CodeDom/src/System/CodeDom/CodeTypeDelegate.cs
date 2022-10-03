// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace System.CodeDom
{
    public class CodeTypeDelegate : CodeTypeDeclaration
    {
        private CodeTypeReference _returnType;

        public CodeTypeDelegate()
        {
            TypeAttributes &= ~TypeAttributes.ClassSemanticsMask;
            TypeAttributes |= TypeAttributes.Class;
            BaseTypes.Clear();
            BaseTypes.Add(new CodeTypeReference("System.Delegate"));
        }

        public CodeTypeDelegate(string name) : this()
        {
            Name = name;
        }

        public CodeTypeReference ReturnType
        {
            get => _returnType ??= new CodeTypeReference("");
            set => _returnType = value;
        }

        public CodeParameterDeclarationExpressionCollection Parameters { get; } = new CodeParameterDeclarationExpressionCollection();
    }
}
