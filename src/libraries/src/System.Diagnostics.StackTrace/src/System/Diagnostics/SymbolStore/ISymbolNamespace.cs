// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Diagnostics.SymbolStore
{
    public interface ISymbolNamespace
    {
        // Get the name of this namespace
        string Name { get; }
    
        // Get the children of this namespace
        ISymbolNamespace[] GetNamespaces();
    
        // Get the variables in this namespace
        ISymbolVariable[] GetVariables();
    }
}
