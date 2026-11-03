// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.CodeDom
{
    public class CodeSnippetTypeMember : CodeTypeMember
    {
        private string? _text;

        public CodeSnippetTypeMember() { }

        public CodeSnippetTypeMember(string? text)
        {
            Text = text;
        }

        [AllowNull]
        public string Text
        {
            get => _text ?? string.Empty;
            set => _text = value;
        }
    }
}
