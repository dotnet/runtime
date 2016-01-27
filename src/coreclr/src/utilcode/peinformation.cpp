// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// --------------------------------------------------------------------------------
// PEInformation.cpp
//

// --------------------------------------------------------------------------------

#include "stdafx.h"
#include "utilcode.h"
#include "peinformation.h"

#if defined(FEATURE_FUSION) && !defined(DACCESS_COMPILE)

extern BOOL g_fWow64Process;    // Wow64 Process

PEKIND GetCurrentRealProcessorPEKIND()
{
    PEKIND curProcessorPEKind = TargetNativePEKIND();

#ifdef _TARGET_X86_
    if (g_fWow64Process)
    {
        SYSTEM_INFO si = {0};

        GetNativeSystemInfo(&si);
        switch (si.wProcessorArchitecture)
        {
        case PROCESSOR_ARCHITECTURE_AMD64:
            curProcessorPEKind = peAMD64;
            break;
        default:
            _ASSERTE(FALSE);
            curProcessorPEKind = peInvalid;
            break;
        }
    }
#endif // _TARGET_X86_

    return curProcessorPEKind;
}

HRESULT RuntimeIsValidAssemblyOnThisPlatform_CheckProcessorArchitecture(PEKIND processorArchitecture, BOOL bForInstall)
{
    LIMITED_METHOD_CONTRACT;

    HRESULT hr = S_OK;
    
    // MSIL / legacy images always allowed
    if (IsPEMSIL(processorArchitecture) || (processorArchitecture == peNone))
    {
        goto Exit;
    }
    else if (IsPE32(processorArchitecture))
    {
#ifdef _TARGET_ARM_
        // ARM can use only native ones
        if (processorArchitecture != TargetNativePEKIND())
        {
            hr = HRESULT_FROM_WIN32(ERROR_BAD_FORMAT);
            goto Exit;
        }

#else //!_TARGET_ARM_
        //ARM assemblies can be installed only on ARM
        if (processorArchitecture == peARM)
        {
            hr = HRESULT_FROM_WIN32(ERROR_BAD_FORMAT);
            goto Exit;
        }
#endif //!_TARGET_ARM_
        
        if (bForInstall)
        {
            goto Exit;
        }
        else
        {
            // won't allow bind to x86 while in 64 bit process.
            if (!IsProcess32())
            {
                hr = HRESULT_FROM_WIN32(ERROR_BAD_FORMAT);
            }
            goto Exit;
        }
    }
    // 64 bit images must match processor type
    else if(IsPE64(processorArchitecture))
    {
        if (!IsProcess32() && (processorArchitecture == TargetNativePEKIND()))
        {
            goto Exit;
        }
        else if (bForInstall && (GetCurrentRealProcessorPEKIND() == processorArchitecture))
        {
            goto Exit;
        }
    }

    // Everything else, fails match
    hr = HRESULT_FROM_WIN32(ERROR_BAD_FORMAT);

Exit:
    return hr;
}
#endif // FEATURE_FUSION && !DACCESS_COMPILE

HRESULT TranslatePEToArchitectureType(CorPEKind CLRPeKind, DWORD dwImageType, PEKIND * pPeKind)
{
    return TranslatePEToArchitectureType(CLRPeKind, dwImageType, 0, pPeKind);
}

HRESULT TranslatePEToArchitectureType(CorPEKind CLRPeKind, DWORD dwImageType, DWORD dwAssemblyFlags, PEKIND * pPeKind)
{
    HRESULT hr = S_OK;
    
    _ASSERTE(pPeKind != NULL);
    
    if (CLRPeKind == peNot)
    {   // Not a PE. Shouldn't ever get here.
        *pPeKind = peInvalid;
        hr = HRESULT_FROM_WIN32(ERROR_BAD_FORMAT);
        goto Exit;
    }
    else if (IsAfPA_NoPlatform(dwAssemblyFlags))
    {
        *pPeKind = peNone;
        goto Exit;
    }
    else
    {
        if ((CLRPeKind & peILonly) && 
            !(CLRPeKind & pe32Plus) && 
            !(CLRPeKind & pe32BitRequired) && 
            (dwImageType == IMAGE_FILE_MACHINE_I386))
        {
            // Processor-agnostic (MSIL)
            *pPeKind = peMSIL;
        }
        else if (CLRPeKind & pe32Plus)
        {
            // 64-bit

            if (CLRPeKind & pe32BitRequired)
            {
                *pPeKind = peInvalid;
                hr = HRESULT_FROM_WIN32(ERROR_BAD_FORMAT);
                goto Exit;
            }

            // Regardless of whether ILONLY is set or not, the architecture
            // is the machine type.

            if (dwImageType == IMAGE_FILE_MACHINE_IA64)
            {
                *pPeKind = peIA64;
            }
            else if (dwImageType == IMAGE_FILE_MACHINE_AMD64)
            {
                *pPeKind = peAMD64;
            }
            else
            {   // We don't support other architectures
                *pPeKind = peInvalid;
                hr = HRESULT_FROM_WIN32(ERROR_BAD_FORMAT);
                goto Exit;
            }
        }
        else
        {
            // 32-bit, non-agnostic

            if (dwImageType == IMAGE_FILE_MACHINE_I386)
            {
                *pPeKind = peI386;
            }
            else if (dwImageType == IMAGE_FILE_MACHINE_ARMNT)
            {
                *pPeKind = peARM;
            }
            else
            {   // Not supported
                *pPeKind = peInvalid;
                hr = HRESULT_FROM_WIN32(ERROR_BAD_FORMAT);
                goto Exit;
            }
        }
    }

Exit:
    return hr;
}
