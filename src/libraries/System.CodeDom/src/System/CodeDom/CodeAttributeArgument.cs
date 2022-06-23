// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if smolloy_codedom_full_internalish
namespace System.Runtime.Serialization.CodeDom
#nullable disable
#else
namespace System.CodeDom
#endif
{
    public class CodeAttributeArgument
    {
        private string _name;

        public CodeAttributeArgument() { }

        public CodeAttributeArgument(CodeExpression value)
        {
            Value = value;
        }

        public CodeAttributeArgument(string name, CodeExpression value)
        {
            Name = name;
            Value = value;
        }

        public string Name
        {
            get => _name ?? string.Empty;
            set => _name = value;
        }

        public CodeExpression Value { get; set; }
    }
}
