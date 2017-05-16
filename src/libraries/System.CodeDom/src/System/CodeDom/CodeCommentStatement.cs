// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.CodeDom
{
    public class CodeCommentStatement : CodeStatement
    {
        public CodeCommentStatement() { }

        public CodeCommentStatement(CodeComment comment)
        {
            Comment = comment;
        }

        public CodeCommentStatement(string text)
        {
            Comment = new CodeComment(text);
        }

        public CodeCommentStatement(string text, bool docComment)
        {
            Comment = new CodeComment(text, docComment);
        }

        public CodeComment Comment { get; set; }
    }
}
