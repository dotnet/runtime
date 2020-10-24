// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.CodeDom
{
    public class CodeGotoStatement : CodeStatement
    {
        private string _label;

        public CodeGotoStatement() { }

        public CodeGotoStatement(string label)
        {
            Label = label;
        }

        public string Label
        {
            get => _label;
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _label = value;
            }
        }
    }
}
