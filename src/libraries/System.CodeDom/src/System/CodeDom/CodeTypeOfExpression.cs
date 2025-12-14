// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.CodeDom
{
    public class CodeTypeOfExpression : CodeExpression
    {
        public CodeTypeOfExpression() { }

        public CodeTypeOfExpression(CodeTypeReference type)
        {
            Type = type;
        }

        public CodeTypeOfExpression(string type)
        {
            Type = new CodeTypeReference(type);
        }

        public CodeTypeOfExpression(Type type)
        {
            Type = new CodeTypeReference(type);
        }

        public CodeTypeReference Type
        {
            get => field ??= new CodeTypeReference("");
            set => field = value;
        }
    }
}
