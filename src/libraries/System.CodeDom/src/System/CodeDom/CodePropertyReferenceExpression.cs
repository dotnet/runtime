// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.CodeDom
{
    public class CodePropertyReferenceExpression : CodeExpression
    {
        private string? _propertyName;

        public CodePropertyReferenceExpression() { }

        public CodePropertyReferenceExpression(CodeExpression? targetObject, string? propertyName)
        {
            TargetObject = targetObject;
            PropertyName = propertyName;
        }

        public CodeExpression? TargetObject { get; set; }

        [AllowNull]
        public string PropertyName
        {
            get => _propertyName ?? string.Empty;
            set => _propertyName = value;
        }
    }
}
