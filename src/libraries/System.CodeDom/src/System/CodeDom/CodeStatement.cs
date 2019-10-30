// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.CodeDom
{
    public class CodeStatement : CodeObject
    {
        private CodeDirectiveCollection _startDirectives = null;
        private CodeDirectiveCollection _endDirectives = null;

        public CodeLinePragma LinePragma { get; set; }

        public CodeDirectiveCollection StartDirectives => _startDirectives ?? (_startDirectives = new CodeDirectiveCollection());

        public CodeDirectiveCollection EndDirectives => _endDirectives ?? (_endDirectives = new CodeDirectiveCollection());
    }
}
