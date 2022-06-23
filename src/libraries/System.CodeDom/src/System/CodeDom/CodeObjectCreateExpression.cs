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
    internal sealed class CodeObjectCreateExpression : CodeExpression
#else
    public class CodeObjectCreateExpression : CodeExpression
#endif
    {
        private CodeTypeReference _createType;

        public CodeObjectCreateExpression() { }

        public CodeObjectCreateExpression(CodeTypeReference createType, params CodeExpression[] parameters)
        {
            CreateType = createType;
            Parameters.AddRange(parameters);
        }

        public CodeObjectCreateExpression(string createType, params CodeExpression[] parameters)
        {
            CreateType = new CodeTypeReference(createType);
            Parameters.AddRange(parameters);
        }

        public CodeObjectCreateExpression(Type createType, params CodeExpression[] parameters)
        {
            CreateType = new CodeTypeReference(createType);
            Parameters.AddRange(parameters);
        }

        public CodeTypeReference CreateType
        {
            get => _createType ??= new CodeTypeReference("");
            set => _createType = value;
        }

        public CodeExpressionCollection Parameters { get; } = new CodeExpressionCollection();
    }
}
