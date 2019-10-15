// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.CodeDom
{
    public class CodeSnippetTypeMember : CodeTypeMember
    {
        private string _text;

        public CodeSnippetTypeMember() { }

        public CodeSnippetTypeMember(string text)
        {
            Text = text;
        }

        public string Text
        {
            get => _text ?? string.Empty;
            set => _text = value;
        }
    }
}
