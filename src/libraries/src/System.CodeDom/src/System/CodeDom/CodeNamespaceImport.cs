// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.CodeDom
{
    public class CodeNamespaceImport : CodeObject
    {
        private string _nameSpace;

        public CodeNamespaceImport() { }

        public CodeNamespaceImport(string nameSpace)
        {
            Namespace = nameSpace;
        }

        public CodeLinePragma LinePragma { get; set; }

        public string Namespace
        {
            get => _nameSpace ?? string.Empty;
            set => _nameSpace = value;
        }
    }
}
