// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.CodeDom
{
    public class CodePropertyReferenceExpression : CodeExpression
    {
        private string _propertyName;

        public CodePropertyReferenceExpression() { }

        public CodePropertyReferenceExpression(CodeExpression targetObject, string propertyName)
        {
            TargetObject = targetObject;
            PropertyName = propertyName;
        }

        public CodeExpression TargetObject { get; set; }

        public string PropertyName
        {
            get { return _propertyName ?? string.Empty; }
            set { _propertyName = value; }
        }
    }
}
