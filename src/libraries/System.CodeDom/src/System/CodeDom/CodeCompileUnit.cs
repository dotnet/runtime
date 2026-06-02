// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Specialized;

namespace System.CodeDom
{
    public class CodeCompileUnit : CodeObject
    {
        public CodeCompileUnit() { }

        public CodeNamespaceCollection Namespaces { get; } = new CodeNamespaceCollection();

        public StringCollection ReferencedAssemblies => field ??= new StringCollection();

        public CodeAttributeDeclarationCollection AssemblyCustomAttributes => field ??= new CodeAttributeDeclarationCollection();

        public CodeDirectiveCollection StartDirectives => field ??= new CodeDirectiveCollection();

        public CodeDirectiveCollection EndDirectives => field ??= new CodeDirectiveCollection();
    }
}
