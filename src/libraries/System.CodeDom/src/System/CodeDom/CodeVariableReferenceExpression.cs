// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.CodeDom
{
    public class CodeVariableReferenceExpression : CodeExpression
    {
        private string? _variableName;

        public CodeVariableReferenceExpression() { }

        public CodeVariableReferenceExpression(string? variableName)
        {
            _variableName = variableName;
        }

        [AllowNull]
        public string VariableName
        {
            get => _variableName ?? string.Empty;
            set => _variableName = value;
        }
    }
}
