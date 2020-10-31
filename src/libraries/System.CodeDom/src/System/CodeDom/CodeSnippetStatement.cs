// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.CodeDom
{
    public class CodeSnippetStatement : CodeStatement
    {
        private string _value;

        public CodeSnippetStatement() { }

        public CodeSnippetStatement(string value)
        {
            Value = value;
        }

        public string Value
        {
            get => _value ?? string.Empty;
            set => _value = value;
        }
    }
}
