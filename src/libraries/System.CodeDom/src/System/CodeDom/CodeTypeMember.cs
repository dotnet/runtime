// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.CodeDom
{
    public class CodeTypeMember : CodeObject
    {
        private string _name;
        private CodeAttributeDeclarationCollection _customAttributes = null;
        private CodeDirectiveCollection _startDirectives = null;
        private CodeDirectiveCollection _endDirectives = null;

        public string Name
        {
            get => _name ?? string.Empty;
            set => _name = value;
        }

        public MemberAttributes Attributes { get; set; } = MemberAttributes.Private | MemberAttributes.Final;

        public CodeAttributeDeclarationCollection CustomAttributes
        {
            get => _customAttributes ?? (_customAttributes = new CodeAttributeDeclarationCollection());
            set => _customAttributes = value;
        }

        public CodeLinePragma LinePragma { get; set; }

        public CodeCommentStatementCollection Comments { get; } = new CodeCommentStatementCollection();

        public CodeDirectiveCollection StartDirectives => _startDirectives ?? (_startDirectives = new CodeDirectiveCollection());

        public CodeDirectiveCollection EndDirectives => _endDirectives ?? (_endDirectives = new CodeDirectiveCollection());
    }
}
