// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** 
** 
** 
**
**
** Purpose: Flags controlling how an AssemblyName is used
**          during binding
**
**
===========================================================*/
namespace System.Reflection {
    
    using System;
    [Serializable]
    [FlagsAttribute()]
    [System.Runtime.InteropServices.ComVisible(true)]
    public enum AssemblyNameFlags
    {
        None                      = 0x0000,
        // Flag used to indicate that an assembly ref contains the full public key, not the compressed token.
        // Must match afPublicKey in CorHdr.h.
        PublicKey                 = 0x0001,        
        //ProcArchMask              = 0x00F0,     // Bits describing the processor architecture
                            // Accessible via AssemblyName.ProcessorArchitecture
        EnableJITcompileOptimizer = 0x4000, 
        EnableJITcompileTracking  = 0x8000, 
        Retargetable              = 0x0100, 
        //ContentType             = 0x0E00, // Bits describing the ContentType are accessible via AssemblyName.ContentType
    }

    [Serializable]
    [System.Runtime.InteropServices.ComVisible(false)]
    public enum AssemblyContentType
    {
        Default                 = 0x0000, 
        WindowsRuntime          = 0x0001
    }

    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public enum ProcessorArchitecture
    {
        None = 0x0000,
        MSIL = 0x0001,
        X86 = 0x0002,
        IA64 = 0x0003,
        Amd64 = 0x0004,
        Arm = 0x0005
    }
}
