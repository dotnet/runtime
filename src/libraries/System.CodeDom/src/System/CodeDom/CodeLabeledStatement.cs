// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.CodeDom
{
    public class CodeLabeledStatement : CodeStatement
    {
        private string _label;

        public CodeLabeledStatement() { }

        public CodeLabeledStatement(string label)
        {
            _label = label;
        }

        public CodeLabeledStatement(string label, CodeStatement statement)
        {
            _label = label;
            Statement = statement;
        }

        public string Label
        {
            get => _label ?? string.Empty;
            set => _label = value;
        }

        public CodeStatement Statement { get; set; }
    }
}
