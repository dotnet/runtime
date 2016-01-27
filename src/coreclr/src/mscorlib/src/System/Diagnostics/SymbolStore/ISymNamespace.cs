// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**
** Represents a namespace within a symbol reader.
**
** 
===========================================================*/
namespace System.Diagnostics.SymbolStore {
    
    using System;
    
    // Interface does not need to be marked with the serializable attribute
[System.Runtime.InteropServices.ComVisible(true)]
    public interface ISymbolNamespace
    {
        // Get the name of this namespace
        String Name { get; }
    
        // Get the children of this namespace
        ISymbolNamespace[] GetNamespaces();
    
        // Get the variables in this namespace
        ISymbolVariable[] GetVariables();
    }
}
