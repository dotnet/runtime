// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if smolloy_codedom_full_internalish
namespace System.Runtime.Serialization.CodeDom
#nullable disable
#else
namespace System.CodeDom
#endif
{
#if smolloy_codedom_full_internalish
    internal sealed class CodeSnippetExpression : CodeExpression
#else
    public class CodeSnippetExpression : CodeExpression
#endif
    {
        private string _value;

        public CodeSnippetExpression() { }

        public CodeSnippetExpression(string value)
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
