// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Internal.Runtime.Binder
{
    internal enum CorPEKind
    {
        peNot = 0x00000000,   // not a PE file
        peILonly = 0x00000001,   // flag IL_ONLY is set in COR header
        pe32BitRequired = 0x00000002,  // flag 32BITREQUIRED is set and 32BITPREFERRED is clear in COR header
        pe32Plus = 0x00000004,   // PE32+ file (64 bit)
        pe32Unmanaged = 0x00000008,    // PE32 without COR header
        pe32BitPreferred = 0x00000010  // flags 32BITREQUIRED and 32BITPREFERRED are set in COR header
    }

    internal static class AssemblyBinderCommon
    {
        // defined in System.Reflection.PortableExecutable.Machine, but it's in System.Reflection.Metadata
        // also defined in System.Reflection.ImageFileMachine
        private const int IMAGE_FILE_MACHINE_I386 = 0x014c;  // Intel 386.
        private const int IMAGE_FILE_MACHINE_ARMNT = 0x01c4;  // ARM Thumb-2 Little-Endian
        private const int IMAGE_FILE_MACHINE_AMD64 = 0x8664;  // AMD64 (K8)
        private const int IMAGE_FILE_MACHINE_ARM64 = 0xAA64;  // ARM64 Little-Endian

        public static unsafe PEKind TranslatePEToArchitectureType(int* pdwPAFlags)
        {
            CorPEKind CLRPeKind = (CorPEKind)pdwPAFlags[0];
            int dwImageType = pdwPAFlags[1];

            if (CLRPeKind == CorPEKind.peNot)
            {
                // Not a PE. Shouldn't ever get here.
                throw new BadImageFormatException();
            }

            if ((CLRPeKind & CorPEKind.peILonly) != 0 && (CLRPeKind & CorPEKind.pe32Plus) == 0 &&
                (CLRPeKind & CorPEKind.pe32BitRequired) == 0 && dwImageType == IMAGE_FILE_MACHINE_I386)
            {
                // Processor-agnostic (MSIL)
                return PEKind.MSIL;
            }
            else if ((CLRPeKind & CorPEKind.pe32Plus) != 0)
            {
                // 64-bit
                if ((CLRPeKind & CorPEKind.pe32BitRequired) != 0)
                {
                    // Invalid
                    throw new BadImageFormatException();
                }

                // Regardless of whether ILONLY is set or not, the architecture
                // is the machine type.
                if (dwImageType == IMAGE_FILE_MACHINE_ARM64)
                    return PEKind.ARM64;
                else if (dwImageType == IMAGE_FILE_MACHINE_AMD64)
                    return PEKind.AMD64;
                else
                {
                    // We don't support other architectures
                    throw new BadImageFormatException();
                }
            }
            else
            {
                // 32-bit, non-agnostic
                if (dwImageType == IMAGE_FILE_MACHINE_I386)
                    return PEKind.I386;
                else if (dwImageType == IMAGE_FILE_MACHINE_ARMNT)
                    return PEKind.ARM;
                else
                {
                    // Not supported
                    throw new BadImageFormatException();
                }
            }
        }

        public static bool IsValidArchitecture(PEKind architecture)
        {
            if (architecture is PEKind.MSIL or PEKind.None)
                return true;

            PEKind processArchitecture =
#if TARGET_X86
                PEKind.I386;
#elif TARGET_AMD64
                PEKind.AMD64;
#elif TARGET_ARM
                PEKind.ARM;
#elif TARGET_ARM64
                PEKind.ARM64;
#else
                PEKind.MSIL;
#endif

            return architecture == processArchitecture;
        }
    }
}
