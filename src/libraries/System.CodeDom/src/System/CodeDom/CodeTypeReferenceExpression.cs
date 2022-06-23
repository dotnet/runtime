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
    internal sealed class CodeTypeReferenceExpression : CodeExpression
#else
    public class CodeTypeReferenceExpression : CodeExpression
#endif
    {
        private CodeTypeReference _type;

        public CodeTypeReferenceExpression() { }

        public CodeTypeReferenceExpression(CodeTypeReference type)
        {
            Type = type;
        }

        public CodeTypeReferenceExpression(string type)
        {
            Type = new CodeTypeReference(type);
        }

        public CodeTypeReferenceExpression(Type type)
        {
            Type = new CodeTypeReference(type);
        }

        public CodeTypeReference Type
        {
            get => _type ??= new CodeTypeReference("");
            set => _type = value;
        }
    }
}
