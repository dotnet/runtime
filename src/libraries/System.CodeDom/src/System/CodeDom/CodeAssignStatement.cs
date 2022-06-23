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
    internal sealed class CodeAssignStatement : CodeStatement
#else
    public class CodeAssignStatement : CodeStatement
#endif
    {
        public CodeAssignStatement() { }

        public CodeAssignStatement(CodeExpression left, CodeExpression right)
        {
            Left = left;
            Right = right;
        }

        public CodeExpression Left { get; set; }

        public CodeExpression Right { get; set; }
    }
}
