// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.CodeDom
{
    public class CodeTypeMember : CodeObject
    {
        public string Name
        {
            get => field ?? string.Empty;
            set
            {
                // Explicit implementations are excluded from the overload index. The VB generator
                // temporarily renames them while emitting their Implements clause.
                bool changed = this is CodeMemberMethod { IsMethodIdentityTracked: true, PrivateImplementationType: null } &&
                    !string.Equals(field, value, StringComparison.Ordinal);
                field = value;
                if (changed)
                    CodeMemberMethod.InvalidateMethodIdentity();
            }
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
