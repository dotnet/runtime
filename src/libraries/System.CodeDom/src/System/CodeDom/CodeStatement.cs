// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.CodeDom
{
    public class CodeStatement : CodeObject
    {
        public CodeLinePragma LinePragma { get; set; }

        public CodeDirectiveCollection StartDirectives => field ??= new CodeDirectiveCollection();

        public CodeDirectiveCollection EndDirectives => field ??= new CodeDirectiveCollection();
    }
}
