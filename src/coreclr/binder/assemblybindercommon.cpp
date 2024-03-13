// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ============================================================
//
// AssemblyBinder.cpp
//


//
// Implements the AssemblyBinder class
//
// ============================================================

#include "common.h"
#include "assemblybindercommon.hpp"
#include "assemblyname.hpp"
#include "assembly.hpp"
#include "bindertracing.h"
#include "utils.hpp"
#include "stringarraylist.h"
#include "configuration.h"

#if !defined(DACCESS_COMPILE)
#include "defaultassemblybinder.h"
#endif // !defined(DACCESS_COMPILE)

STDAPI BinderAcquirePEImage(LPCTSTR            szAssemblyPath,
    PEImage** ppPEImage,
    BundleFileLocation bundleFileLocation);

namespace BINDER_SPACE
{
    HRESULT AssemblyBinderCommon::TranslatePEToArchitectureType(DWORD  *pdwPAFlags, PEKIND *PeKind)
    {
        HRESULT hr = S_OK;

        _ASSERTE(pdwPAFlags != NULL);
        _ASSERTE(PeKind != NULL);

        CorPEKind CLRPeKind = (CorPEKind) pdwPAFlags[0];
        DWORD dwImageType = pdwPAFlags[1];

        *PeKind = peNone;

        if(CLRPeKind == peNot)
        {
            // Not a PE. Shouldn't ever get here.
            IF_FAIL_GO(HRESULT_FROM_WIN32(ERROR_BAD_FORMAT));
        }
        else
        {
            if ((CLRPeKind & peILonly) && !(CLRPeKind & pe32Plus) &&
                !(CLRPeKind & pe32BitRequired) && dwImageType == IMAGE_FILE_MACHINE_I386)
            {
                // Processor-agnostic (MSIL)
                *PeKind = peMSIL;
            }
            else if (CLRPeKind & pe32Plus)
            {
                // 64-bit
                if (CLRPeKind & pe32BitRequired)
                {
                    // Invalid
                    IF_FAIL_GO(HRESULT_FROM_WIN32(ERROR_BAD_FORMAT));
                }

                // Regardless of whether ILONLY is set or not, the architecture
                // is the machine type.
                if(dwImageType == IMAGE_FILE_MACHINE_ARM64)
                    *PeKind = peARM64;
                else if (dwImageType == IMAGE_FILE_MACHINE_AMD64)
                    *PeKind = peAMD64;
                else
                {
                    // We don't support other architectures
                    IF_FAIL_GO(HRESULT_FROM_WIN32(ERROR_BAD_FORMAT));
                }
            }
            else
            {
                // 32-bit, non-agnostic
                if(dwImageType == IMAGE_FILE_MACHINE_I386)
                    *PeKind = peI386;
                else if(dwImageType == IMAGE_FILE_MACHINE_ARMNT)
                    *PeKind = peARM;
                else
                {
                    // Not supported
                    IF_FAIL_GO(HRESULT_FROM_WIN32(ERROR_BAD_FORMAT));
                }
            }
        }

    Exit:
        return hr;
    }
    
    /* static */
    HRESULT AssemblyBinderCommon::BindToSystem(SString   &systemDirectory,
                                               PEImage   **ppPEImage)
    {
        HRESULT hr = S_OK;

        _ASSERTE(ppPEImage != NULL);

        ReleaseHolder<PEImage> pSystemAssembly;

        // System.Private.CoreLib.dll is expected to be found at one of the following locations:
        //   * Non-single-file app: In systemDirectory, beside coreclr.dll
        //   * Framework-dependent single-file app: In systemDirectory, beside coreclr.dll
        //   * Self-contained single-file app: Within the single-file bundle.
        //
        //   CoreLib path (sCoreLib):
        //   * Absolute path when looking for a file on disk
        //   * Bundle-relative path when looking within the single-file bundle.

        StackSString sCoreLibName(CoreLibName_IL_W);
        StackSString sCoreLib;
        BinderTracing::PathSource pathSource = BinderTracing::PathSource::Bundle;
        BundleFileLocation bundleFileLocation = Bundle::ProbeAppBundle(sCoreLibName, /*pathIsBundleRelative */ true);
        if (!bundleFileLocation.IsValid())
        {
            pathSource = BinderTracing::PathSource::ApplicationAssemblies;
        }
        sCoreLib.Set(systemDirectory);
        CombinePath(sCoreLib, sCoreLibName, sCoreLib);

        hr = BinderAcquirePEImage(sCoreLib.GetUnicode(), &pSystemAssembly, bundleFileLocation);

        BinderTracing::PathProbed(sCoreLib, pathSource, hr);

        if ((FAILED(hr)) && IsFileNotFound(hr))
        {
            // Try to find corelib in the TPA
            StackSString sCoreLibSimpleName(CoreLibName_W);
            StackSString sTrustedPlatformAssemblies = Configuration::GetKnobStringValue(W("TRUSTED_PLATFORM_ASSEMBLIES"));
            sTrustedPlatformAssemblies.Normalize();

            bool found = false;
            for (SString::Iterator i = sTrustedPlatformAssemblies.Begin(); i != sTrustedPlatformAssemblies.End(); )
            {
                SString fileName;
                SString simpleName;
                bool isNativeImage = false;
                HRESULT pathResult = S_OK;
                IF_FAIL_GO(pathResult = GetNextTPAPath(sTrustedPlatformAssemblies, i, /*dllOnly*/ true, fileName, simpleName, isNativeImage));
                if (pathResult == S_FALSE)
                {
                    break;
                }

                if (simpleName.EqualsCaseInsensitive(sCoreLibSimpleName))
                {
                    sCoreLib = fileName;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                GO_WITH_HRESULT(HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND));
            }
            
            hr = BinderAcquirePEImage(sCoreLib.GetUnicode(), &pSystemAssembly, bundleFileLocation);

            BinderTracing::PathProbed(sCoreLib, BinderTracing::PathSource::ApplicationAssemblies, hr);
        }

        IF_FAIL_GO(hr);

        *ppPEImage = pSystemAssembly.Extract();

    Exit:
        return hr;
    }
    
#if !defined(DACCESS_COMPILE)
HRESULT AssemblyBinderCommon::CreateDefaultBinder(DefaultAssemblyBinder** ppDefaultBinder)
{
    HRESULT hr = S_OK;
    EX_TRY
    {
        if (ppDefaultBinder != NULL)
        {
            NewHolder<DefaultAssemblyBinder> pBinder;
            SAFE_NEW(pBinder, DefaultAssemblyBinder);

            if (SUCCEEDED(hr))
            {
                pBinder->SetManagedAssemblyLoadContext(NULL);
                *ppDefaultBinder = pBinder.Extract();
            }
        }
    }
    EX_CATCH_HRESULT(hr);

Exit:
    return hr;
}

BOOL AssemblyBinderCommon::IsValidArchitecture(PEKIND kArchitecture)
{
    if ((kArchitecture == peMSIL) || (kArchitecture == peNone))
        return TRUE;

    PEKIND processArchitecture =
#if defined(TARGET_X86)
        peI386;
#elif defined(TARGET_AMD64)
        peAMD64;
#elif defined(TARGET_ARM)
        peARM;
#elif defined(TARGET_ARM64)
        peARM64;
#else
        peMSIL;
#endif

    return (kArchitecture == processArchitecture);
}

#endif // !defined(DACCESS_COMPILE)
};


