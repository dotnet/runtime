// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Types in this file are used for generated p/invokes
//
#if MICROSOFT_INTEROP_SOURCEGENERATION
namespace Microsoft.Interop
#else
namespace System.Runtime.InteropServices
#endif
{
    /// <summary>
    /// Specifies how strings should be marshalled for generated p/invokes
    /// </summary>
#if SYSTEM_PRIVATE_CORELIB || MICROSOFT_INTEROP_SOURCEGENERATION
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
