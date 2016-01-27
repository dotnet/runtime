// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**
** Represents a variable within a symbol store. This could be a
** parameter, local variable, or some other non-local variable.
**
** 
===========================================================*/
namespace System.Diagnostics.SymbolStore {
    // Interface does not need to be marked with the serializable attribute
    using System;

[System.Runtime.InteropServices.ComVisible(true)]
    public interface ISymbolVariable
    {
        // Get the name of this variable.
        String Name { get; }
    
        // Get the attributes of this variable.
        Object Attributes { get; }
    
        // Get the signature of this variable.
        byte[] GetSignature();
    
        SymAddressKind AddressKind { get; }
        int AddressField1 { get; }
        int AddressField2 { get; }
        int AddressField3 { get; }
    
        // Get the start/end offsets of this variable within its
        // parent. If this is a local variable within a scope, these will
        // fall within the offsets defined for the scope.
        int StartOffset { get; }
        int EndOffset { get; }
    }
}
