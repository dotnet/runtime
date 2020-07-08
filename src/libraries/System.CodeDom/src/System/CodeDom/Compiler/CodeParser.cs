// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace System.CodeDom.Compiler
{
    public abstract class CodeParser : ICodeParser
    {
        public abstract CodeCompileUnit Parse(TextReader codeStream);
    }
}
