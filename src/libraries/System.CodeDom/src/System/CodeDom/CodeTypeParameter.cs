// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.CodeDom
{
    public class CodeTypeParameter : CodeObject
    {
        private string _name;

        public CodeTypeParameter() { }

        public CodeTypeParameter(string name)
        {
            _name = name;
        }

        public string Name
        {
            get => _name ?? string.Empty;
            set => _name = value;
        }

        public CodeTypeReferenceCollection Constraints => field ??= new CodeTypeReferenceCollection();

        public CodeAttributeDeclarationCollection CustomAttributes => field ??= new CodeAttributeDeclarationCollection();

        public bool HasConstructorConstraint { get; set; }
    }
}
