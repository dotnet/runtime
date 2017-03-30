// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    /////////////////////////////////////////////////////////////////////////////////
    // This is the base interface that Type and Namespace symbol inherit.

    internal interface ITypeOrNamespace
    {
        bool IsType { get; }

        bool IsNamespace { get; }

        AssemblyQualifiedNamespaceSymbol AsNamespace();
        CType AsType();
    }
}
