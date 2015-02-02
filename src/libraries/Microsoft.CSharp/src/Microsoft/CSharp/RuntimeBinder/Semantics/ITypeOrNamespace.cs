// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    /////////////////////////////////////////////////////////////////////////////////
    // This is the base interface that Type and Namespace symbol inherit.

    internal interface ITypeOrNamespace
    {
        bool IsType();
        bool IsNamespace();

        AssemblyQualifiedNamespaceSymbol AsNamespace();
        CType AsType();
    }
}
