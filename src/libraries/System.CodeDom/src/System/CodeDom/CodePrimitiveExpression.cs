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
    internal sealed class CodePrimitiveExpression : CodeExpression
#else
    public class CodePrimitiveExpression : CodeExpression
#endif
    {
        public CodePrimitiveExpression() { }

        public CodePrimitiveExpression(object value)
        {
            Value = value;
        }

        public object Value { get; set; }
    }
}
