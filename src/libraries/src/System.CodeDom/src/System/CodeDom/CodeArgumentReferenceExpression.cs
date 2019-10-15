// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.CodeDom
{
    public class CodeArgumentReferenceExpression : CodeExpression
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
