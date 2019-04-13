// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
namespace System.Reflection
{
    [Flags]
    public enum AssemblyNameFlags
    {
        None = 0x0000,
        // Flag used to indicate that an assembly ref contains the full public key, not the compressed token.
        // Must match afPublicKey in CorHdr.h.
        PublicKey = 0x0001,
        //ProcArchMask              = 0x00F0,     // Bits describing the processor architecture
        // Accessible via AssemblyName.ProcessorArchitecture
        EnableJITcompileOptimizer = 0x4000,
        EnableJITcompileTracking = 0x8000,
        Retargetable = 0x0100,
        //ContentType             = 0x0E00, // Bits describing the ContentType are accessible via AssemblyName.ContentType
    }
}
