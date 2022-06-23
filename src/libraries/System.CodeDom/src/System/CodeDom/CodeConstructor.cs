// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if smolloy_codedom_full_internalish
namespace System.Runtime.Serialization.CodeDom
#else
namespace System.CodeDom
#endif
{
#if smolloy_codedom_full_internalish
    internal sealed class CodeConstructor : CodeMemberMethod
#else
    public class CodeConstructor : CodeMemberMethod
#endif
    {
        public CodeConstructor()
        {
            Name = ".ctor";
        }

        public CodeExpressionCollection BaseConstructorArgs { get; } = new CodeExpressionCollection();

        public CodeExpressionCollection ChainedConstructorArgs { get; } = new CodeExpressionCollection();
    }
}
