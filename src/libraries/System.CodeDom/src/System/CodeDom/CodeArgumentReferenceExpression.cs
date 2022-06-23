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
    internal sealed class CodeArgumentReferenceExpression : CodeExpression
#else
    public class CodeArgumentReferenceExpression : CodeExpression
#endif
    {
        private string _parameterName;

        public CodeArgumentReferenceExpression() { }

        public CodeArgumentReferenceExpression(string parameterName)
        {
            _parameterName = parameterName;
        }

        public string ParameterName
        {
            get => _parameterName ?? string.Empty;
            set => _parameterName = value;
        }
    }
}
