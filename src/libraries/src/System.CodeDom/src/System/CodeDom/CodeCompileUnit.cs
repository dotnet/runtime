// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Specialized;

namespace System.CodeDom
{
    public class CodeCompileUnit : CodeObject
    {
        private StringCollection _assemblies = null;
        private CodeAttributeDeclarationCollection _attributes = null;

        private CodeDirectiveCollection _startDirectives = null;
        private CodeDirectiveCollection _endDirectives = null;

        public CodeCompileUnit() { }

        public CodeNamespaceCollection Namespaces { get; } = new CodeNamespaceCollection();

        public StringCollection ReferencedAssemblies => _assemblies ?? (_assemblies = new StringCollection());

        public CodeAttributeDeclarationCollection AssemblyCustomAttributes => _attributes ?? (_attributes = new CodeAttributeDeclarationCollection());

        public CodeDirectiveCollection StartDirectives => _startDirectives ?? (_startDirectives = new CodeDirectiveCollection());

        public CodeDirectiveCollection EndDirectives => _endDirectives ?? (_endDirectives = new CodeDirectiveCollection());
    }
}
