// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// --------------------------------------------------------------------------------
// PEInformation.h
//

// --------------------------------------------------------------------------------

#ifndef PEINFORMATION_H
#define PEINFORMATION_H

#ifndef PEKIND_ENUM_DEFINED
#define PEKIND_ENUM_DEFINED
// This must match the definition of pekind in fusion.idl
typedef enum _tagPEKIND 
{
    peNone      = 0x00000000,
    peMSIL      = 0x00000001,
    peI386      = 0x00000002,
    peIA64      = 0x00000003,
    peAMD64     = 0x00000004,
    peARM       = 0x00000005,
    peARM64     = 0x00000006,
    peInvalid = 0xffffffff
} PEKIND;

#endif


inline bool IsPE64(PEKIND x)
{
    return ( (x == peIA64) || (x == peAMD64) || (x == peARM64) );
}

inline bool IsPE32(PEKIND x)
{
    return ( (x == peI386) || (x == peARM) );
}

inline bool IsPEMSIL(PEKIND x)
{
    return ( (x == peMSIL) );
}

#ifdef _WIN64
inline bool IsProcess32() { return false; }
#else
inline bool IsProcess32() { return true; }
#endif

#if defined(_TARGET_X86_)
inline PEKIND TargetNativePEKIND() { return peI386; }
#elif defined(_TARGET_AMD64_)
inline PEKIND TargetNativePEKIND() { return peAMD64; }
#elif defined(_TARGET_ARM_)
inline PEKIND TargetNativePEKIND() { return peARM; }
#elif defined(_TARGET_ARM64_)
inline PEKIND TargetNativePEKIND() { return peARM64; }
#else
#error Need to define valid TargetNativePEKIND()
#endif

STDAPI RuntimeIsValidAssemblyOnThisPlatform_CheckProcessorArchitecture(PEKIND processorArchitecture, BOOL bForInstall);

//*****************************************************************************
//  Intreprets CLRPeKind and dwImageType to get PeKind as per the CLRBitness
//  API, CLRPeKind and dwImageType can be recoved from GetPEKind() if you
//  have the metadata, or retrieved directly from the headers as per the
//  implementation in shim.cpp:_CorValidateImage.
//*****************************************************************************
HRESULT TranslatePEToArchitectureType(CorPEKind CLRPeKind, DWORD dwImageType, PEKIND * PeKind);
HRESULT TranslatePEToArchitectureType(CorPEKind CLRPeKind, DWORD dwImageType, DWORD dwAssemblyFlags, PEKIND * PeKind);

#endif // PEINFORMATION_H
