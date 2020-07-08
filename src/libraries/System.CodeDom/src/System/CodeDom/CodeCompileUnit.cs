// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Specialized;

namespace System.CodeDom
{
    public class CodeCompileUnit : CodeObject
    {
        private StringCollection _assemblies;
        private CodeAttributeDeclarationCollection _attributes;

        private CodeDirectiveCollection _startDirectives;
        private CodeDirectiveCollection _endDirectives;

        public CodeCompileUnit() { }

        public CodeNamespaceCollection Namespaces { get; } = new CodeNamespaceCollection();

        public StringCollection ReferencedAssemblies => _assemblies ?? (_assemblies = new StringCollection());

        public CodeAttributeDeclarationCollection AssemblyCustomAttributes => _attributes ?? (_attributes = new CodeAttributeDeclarationCollection());

        public CodeDirectiveCollection StartDirectives => _startDirectives ?? (_startDirectives = new CodeDirectiveCollection());

        public CodeDirectiveCollection EndDirectives => _endDirectives ?? (_endDirectives = new CodeDirectiveCollection());
    }
}
