// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.CodeDom
{
    public class CodeSnippetCompileUnit : CodeCompileUnit
    {
        private string _value;

        public CodeSnippetCompileUnit() { }

        public CodeSnippetCompileUnit(string value)
        {
            Value = value;
        }

        public string Value
        {
            get { return _value ?? string.Empty; }
            set { _value = value; }
        }

        public CodeLinePragma LinePragma { get; set; }
    }
}
