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
    internal sealed class CodePropertyReferenceExpression : CodeExpression
#else
    public class CodePropertyReferenceExpression : CodeExpression
#endif
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
            get => _propertyName ?? string.Empty;
            set => _propertyName = value;
        }
    }
}
