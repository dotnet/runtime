// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace System.CodeDom
{
    public class CodeAttributeDeclaration
    {
        private string _name;
        private readonly CodeAttributeArgumentCollection _arguments = new CodeAttributeArgumentCollection();
        private CodeTypeReference _attributeType;

        public CodeAttributeDeclaration() { }

        public CodeAttributeDeclaration(string name)
        {
            Name = name;
        }

        public CodeAttributeDeclaration(string name, params CodeAttributeArgument[] arguments)
        {
            Name = name;
            Arguments.AddRange(arguments);
        }

        public CodeAttributeDeclaration(CodeTypeReference attributeType) : this(attributeType, null) { }

        public CodeAttributeDeclaration(CodeTypeReference attributeType, params CodeAttributeArgument[] arguments)
        {
            _attributeType = attributeType;
            if (attributeType != null)
            {
                _name = attributeType.BaseType;
            }

            if (arguments != null)
            {
                Arguments.AddRange(arguments);
            }
        }

        public string Name
        {
            get { return _name ?? string.Empty; }
            set
            {
                _name = value;
                _attributeType = new CodeTypeReference(_name);
            }
        }

        public CodeAttributeArgumentCollection Arguments => _arguments;

        public CodeTypeReference AttributeType => _attributeType;
    }
}
