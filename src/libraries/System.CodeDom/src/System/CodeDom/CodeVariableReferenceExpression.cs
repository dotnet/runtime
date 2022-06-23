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
    internal sealed class CodeVariableReferenceExpression : CodeExpression
#else
    public class CodeVariableReferenceExpression : CodeExpression
#endif
    {
        private string _variableName;

        public CodeVariableReferenceExpression() { }

        public CodeVariableReferenceExpression(string variableName)
        {
            _variableName = variableName;
        }

        public string VariableName
        {
            get => _variableName ?? string.Empty;
            set => _variableName = value;
        }
    }
}
