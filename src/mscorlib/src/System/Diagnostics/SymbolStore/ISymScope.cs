// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**
** Represents a lexical scope within a ISymbolMethod. Provides access to
** the start and end offsets of the scope, as well as its child and
** parent scopes. Also provides access to all the locals defined
** within this scope.
**
** 
===========================================================*/
namespace System.Diagnostics.SymbolStore {
    // Interface does not need to be marked with the serializable attribute
    using System;
    using System.Text;
    using System.Runtime.InteropServices;

[System.Runtime.InteropServices.ComVisible(true)]
    public interface ISymbolScope
    {
        // Get the method that contains this scope.
        ISymbolMethod Method { get; }

        // Get the parent scope of this scope.
        ISymbolScope Parent { get; }

        // Get any child scopes of this scope.
        ISymbolScope[] GetChildren();

        // Get the start and end offsets for this scope.
        int StartOffset { get; }
        int EndOffset { get; }

        // Get the locals within this scope. They are returned in no
        // particular order. Note: if a local variable changes its address
        // within this scope then that variable will be returned multiple
        // times, each with a different offset range.
        ISymbolVariable[] GetLocals();

        // Get the namespaces that are being "used" within this scope.
        ISymbolNamespace[] GetNamespaces();
    }
}
