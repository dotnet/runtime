// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// --------------------------------------------------------------------------------
// PEInformation.cpp
//

// --------------------------------------------------------------------------------

#include "stdafx.h"
#include "utilcode.h"
#include "peinformation.h"


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
