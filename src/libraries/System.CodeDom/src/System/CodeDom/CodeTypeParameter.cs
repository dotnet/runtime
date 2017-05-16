// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.CodeDom
{
    public class CodeTypeParameter : CodeObject
    {
        private string _name;
        private CodeAttributeDeclarationCollection _customAttributes;
        private CodeTypeReferenceCollection _constraints;

        public CodeTypeParameter() { }

        public CodeTypeParameter(string name)
        {
            _name = name;
        }

        public string Name
        {
            get { return _name ?? string.Empty; }
            set { _name = value; }
        }

        public CodeTypeReferenceCollection Constraints => _constraints ?? (_constraints = new CodeTypeReferenceCollection());

        public CodeAttributeDeclarationCollection CustomAttributes => _customAttributes ?? (_customAttributes = new CodeAttributeDeclarationCollection());

        public bool HasConstructorConstraint { get; set; }
    }
}


