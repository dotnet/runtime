// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.CodeDom
{
    public class CodeConstructor : CodeMemberMethod
    {
        public CodeConstructor()
        {
            Name = ".ctor";
        }

        public CodeExpressionCollection BaseConstructorArgs { get; } = new CodeExpressionCollection();

        public CodeExpressionCollection ChainedConstructorArgs { get; } = new CodeExpressionCollection();
    }
}
