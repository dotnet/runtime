// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Diagnostics.SymbolStore
{
    public interface ISymbolVariable
    {
        // Get the name of this variable.
        string Name { get; }
    
        // Get the attributes of this variable.
        object Attributes { get; }
    
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
