// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    // ----------------------------------------------------------------------------
    //
    // ErrorType
    //
    // ErrorType - a symbol representing an error that has been reported.
    // ----------------------------------------------------------------------------

    internal sealed class ErrorType : CType
    {
        public Name nameText;
        public TypeArray typeArgs;

        public bool HasParent() { return _pParentType != null || _pParentNS != null; }

        public bool HasTypeParent() { return _pParentType != null; }
        public CType GetTypeParent() { return _pParentType; }
        public void SetTypeParent(CType pType) { _pParentType = pType; }

        public bool HasNSParent() { return _pParentNS != null; }
        public AssemblyQualifiedNamespaceSymbol GetNSParent() { return _pParentNS; }
        public void SetNSParent(AssemblyQualifiedNamespaceSymbol pNS) { _pParentNS = pNS; }

        private CType _pParentType;
        private AssemblyQualifiedNamespaceSymbol _pParentNS;
    }
}
