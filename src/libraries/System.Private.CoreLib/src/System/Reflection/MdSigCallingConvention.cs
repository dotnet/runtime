// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection
{
    // ECMA-335 II.23.2.1-3: the first byte of a signature blob.
    [Flags]
    internal enum MdSigCallingConvention : byte
    {
        CallConvMask = 0x0f,  // Calling convention is bottom 4 bits

        Default = 0x00,
        C = 0x01,
        StdCall = 0x02,
        ThisCall = 0x03,
        FastCall = 0x04,
        Vararg = 0x05,
        Field = 0x06,
        LocalSig = 0x07,
        Property = 0x08,
        Unmanaged = 0x09,
        GenericInst = 0x0a,  // generic method instantiation

        Generic = 0x10,  // Generic method sig with explicit number of type arguments (precedes ordinary parameter count)
        HasThis = 0x20,  // Top bit indicates a 'this' parameter
        ExplicitThis = 0x40,  // This parameter is explicitly in the signature
    }
}
