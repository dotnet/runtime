// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if smolloy_codedom_full_internalish
namespace System.Runtime.Serialization.CodeDom
#nullable disable
#else
namespace System.CodeDom
#endif
{
    public class CodeTypeMember : CodeObject
    {
        private string _name;
        private CodeAttributeDeclarationCollection _customAttributes;
        private CodeDirectiveCollection _startDirectives;
        private CodeDirectiveCollection _endDirectives;

        public string Name
        {
            get => _name ?? string.Empty;
            set => _name = value;
        }

        public MemberAttributes Attributes { get; set; } = MemberAttributes.Private | MemberAttributes.Final;

        public CodeAttributeDeclarationCollection CustomAttributes
        {
            get => _customAttributes ??= new CodeAttributeDeclarationCollection();
            set => _customAttributes = value;
        }

        public CodeLinePragma LinePragma { get; set; }

        public CodeCommentStatementCollection Comments { get; } = new CodeCommentStatementCollection();

        public CodeDirectiveCollection StartDirectives => _startDirectives ??= new CodeDirectiveCollection();

        public CodeDirectiveCollection EndDirectives => _endDirectives ??= new CodeDirectiveCollection();
    }
}
