// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.CodeDom
{
    public class CodeTypeMember : CodeObject
    {
        public string Name
        {
            get => field ?? string.Empty;
            set => field = value;
        }

        public MemberAttributes Attributes { get; set; } = MemberAttributes.Private | MemberAttributes.Final;

        public CodeAttributeDeclarationCollection CustomAttributes
        {
            get => field ??= new CodeAttributeDeclarationCollection();
            set => field = value;
        }

        public CodeLinePragma LinePragma { get; set; }

        public CodeCommentStatementCollection Comments { get; } = new CodeCommentStatementCollection();

        public CodeDirectiveCollection StartDirectives => field ??= new CodeDirectiveCollection();

        public CodeDirectiveCollection EndDirectives => field ??= new CodeDirectiveCollection();
    }
}
