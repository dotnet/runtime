// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.CodeDom
{
    public class CodeStatement : CodeObject
    {
        private CodeDirectiveCollection _startDirectives;
        private CodeDirectiveCollection _endDirectives;

        public CodeLinePragma LinePragma { get; set; }

        public CodeDirectiveCollection StartDirectives => _startDirectives ??= new CodeDirectiveCollection();

        public CodeDirectiveCollection EndDirectives => _endDirectives ??= new CodeDirectiveCollection();
    }
}
