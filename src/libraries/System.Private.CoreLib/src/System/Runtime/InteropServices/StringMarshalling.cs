// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Types in this file are used for generated p/invokes
//
namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Specifies how strings should be marshalled for generated p/invokes
    /// </summary>
#if SYSTEM_PRIVATE_CORELIB
    public
#else
    internal
#endif
    enum StringMarshalling
    {
        Custom = 0,
        Utf8,   // UTF-8
        Utf16,  // UTF-16, machine-endian
    }
}
