// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection
{
    // This Enum matchs the CorMethodImpl defined in CorHdr.h
    public enum MethodImplAttributes
    {
        // code impl mask
        CodeTypeMask = 0x0003,   // Flags about code type.   
        IL = 0x0000,   // Method impl is IL.
        Native = 0x0001,   // Method impl is native.     
        OPTIL = 0x0002,   // Method impl is OPTIL 
        Runtime = 0x0003,   // Method impl is provided by the runtime.
                            // end code impl mask

        // managed mask
        ManagedMask = 0x0004,   // Flags specifying whether the code is managed or unmanaged.
        Unmanaged = 0x0004,   // Method impl is unmanaged, otherwise managed.
        Managed = 0x0000,   // Method impl is managed.
                            // end managed mask

        // implementation info and interop
        ForwardRef = 0x0010,   // Indicates method is not defined; used primarily in merge scenarios.
        PreserveSig = 0x0080,   // Indicates method sig is exported exactly as declared.

        InternalCall = 0x1000,   // Internal Call...

        Synchronized = 0x0020,   // Method is single threaded through the body.
        NoInlining = 0x0008,   // Method may not be inlined.
        AggressiveInlining = 0x0100,   // Method should be inlined if possible.
        NoOptimization = 0x0040,   // Method may not be optimized.
        AggressiveOptimization = 0x0200, // Method may contain hot code and should be aggressively optimized.

        MaxMethodImplVal = 0xffff,
    }
}
