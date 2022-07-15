// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.CodeDom
{
    public class CodeArrayCreateExpression : CodeExpression
    {
        private readonly CodeExpressionCollection _initializers = new CodeExpressionCollection();
        private CodeTypeReference _createType;

        public CodeArrayCreateExpression()
        {
        }

        public CodeArrayCreateExpression(CodeTypeReference createType, params CodeExpression[] initializers)
        {
            _createType = createType;
            _initializers.AddRange(initializers);
        }

        public CodeArrayCreateExpression(string createType, params CodeExpression[] initializers)
        {
            _createType = new CodeTypeReference(createType);
            _initializers.AddRange(initializers);
        }

        public CodeArrayCreateExpression(Type createType, params CodeExpression[] initializers)
        {
            _createType = new CodeTypeReference(createType);
            _initializers.AddRange(initializers);
        }

        public CodeArrayCreateExpression(CodeTypeReference createType, int size)
        {
            _createType = createType;
            Size = size;
        }

        public CodeArrayCreateExpression(string createType, int size)
        {
            _createType = new CodeTypeReference(createType);
            Size = size;
        }

        public CodeArrayCreateExpression(Type createType, int size)
        {
            _createType = new CodeTypeReference(createType);
            Size = size;
        }

        public CodeArrayCreateExpression(CodeTypeReference createType, CodeExpression size)
        {
            _createType = createType;
            SizeExpression = size;
        }

        public CodeArrayCreateExpression(string createType, CodeExpression size)
        {
            _createType = new CodeTypeReference(createType);
            SizeExpression = size;
        }

        public CodeArrayCreateExpression(Type createType, CodeExpression size)
        {
            _createType = new CodeTypeReference(createType);
            SizeExpression = size;
        }

        public CodeTypeReference CreateType
        {
            get => _createType ??= new CodeTypeReference("");
            set => _createType = value;
        }

        public CodeExpressionCollection Initializers => _initializers;

        public int Size { get; set; }

        public CodeExpression SizeExpression { get; set; }
    }
}
