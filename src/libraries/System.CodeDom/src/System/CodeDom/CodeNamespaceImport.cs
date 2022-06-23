// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if smolloy_codedom_full_internalish
namespace System.Runtime.Serialization.CodeDom
#nullable disable
#else
namespace System.CodeDom
#endif
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
