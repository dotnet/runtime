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
    internal sealed class CodeFieldReferenceExpression : CodeExpression
#else
    public class CodeFieldReferenceExpression : CodeExpression
#endif
    {
        private string _fieldName;

        public CodeFieldReferenceExpression() { }

        public CodeFieldReferenceExpression(CodeExpression targetObject, string fieldName)
        {
            TargetObject = targetObject;
            FieldName = fieldName;
        }

        public CodeExpression TargetObject { get; set; }

        public string FieldName
        {
            get => _fieldName ?? string.Empty;
            set => _fieldName = value;
        }
    }
}
