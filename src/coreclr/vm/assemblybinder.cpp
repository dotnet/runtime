// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "assemblybinder.h"
#include "nativeimage.h"
#include "../binder/inc/assemblyname.hpp"

#ifndef DACCESS_COMPILE

HRESULT AssemblyBinder::BindAssemblyByName(AssemblyNameData* pAssemblyNameData,
    BINDER_SPACE::Assembly** ppAssembly)
{
    _ASSERTE(pAssemblyNameData != nullptr && ppAssembly != nullptr);

    HRESULT hr = S_OK;
    *ppAssembly = nullptr;

    ReleaseHolder<BINDER_SPACE::AssemblyName> pAssemblyName;
    SAFE_NEW(pAssemblyName, BINDER_SPACE::AssemblyName);
    IF_FAIL_GO(pAssemblyName->Init(*pAssemblyNameData));

    hr = BindUsingAssemblyName(pAssemblyName, ppAssembly);

Exit:
    return hr;
}


NativeImage* AssemblyBinder::LoadNativeImage(Module* componentModule, LPCUTF8 nativeImageName)
{
    STANDARD_VM_CONTRACT;

    BaseDomain::LoadLockHolder lock(AppDomain::GetCurrentDomain());
    AssemblyBinder* binder = componentModule->GetPEAssembly()->GetAssemblyBinder();
    PTR_LoaderAllocator moduleLoaderAllocator = componentModule->GetLoaderAllocator();

    bool isNewNativeImage;
    NativeImage* nativeImage = NativeImage::Open(componentModule, nativeImageName, binder, moduleLoaderAllocator, &isNewNativeImage);

    return nativeImage;
}

#ifdef FEATURE_READYTORUN
static void MvidMismatchFatalError(GUID mvidActual, GUID mvidExpected, LPCUTF8 simpleName, bool compositeComponent, LPCUTF8 assemblyRequirementName)
{
    CHAR assemblyMvidText[GUID_STR_BUFFER_LEN];
    GuidToLPSTR(mvidActual, assemblyMvidText);

    CHAR componentMvidText[GUID_STR_BUFFER_LEN];
    GuidToLPSTR(mvidExpected, componentMvidText);

    SString message;
    if (compositeComponent)
    {
        message.Printf("MVID mismatch between loaded assembly '%s' (MVID = %s) and an assembly with the same simple name embedded in the native image '%s' (MVID = %s)",
            simpleName,
            assemblyMvidText,
            assemblyRequirementName,
            componentMvidText);
    }
    else
    {
        message.Printf("MVID mismatch between loaded assembly '%s' (MVID = %s) and version of assembly '%s' expected by assembly '%s' (MVID = %s)",
            simpleName,
            assemblyMvidText,
            simpleName,
            assemblyRequirementName,
            componentMvidText);
    }

    EEPOLICY_HANDLE_FATAL_ERROR_WITH_MESSAGE(COR_E_FAILFAST, message.GetUnicode());
}

void AssemblyBinder::DeclareDependencyOnMvid(LPCUTF8 simpleName, GUID mvid, bool compositeComponent, LPCUTF8 imageName)
{
    _ASSERTE(imageName != NULL);

    // If the table is empty, then we didn't fill it with all the loaded assemblies as they were loaded. Record this detail, and fix after adding the dependency
    bool addAllLoadedModules = false;
    if (m_assemblySimpleNameMvidCheckHash.GetCount() == 0)
    {
        addAllLoadedModules = true;
    }

    SimpleNameToExpectedMVIDAndRequiringAssembly* foundElem = (SimpleNameToExpectedMVIDAndRequiringAssembly*)m_assemblySimpleNameMvidCheckHash.LookupPtr(simpleName);
    if (foundElem == NULL)
    {
        SimpleNameToExpectedMVIDAndRequiringAssembly newElem(simpleName, mvid, compositeComponent, imageName);
        m_assemblySimpleNameMvidCheckHash.Add(newElem);
    }
    else
    {
        // Elem already exists. Determine if the existing elem is another one with the same mvid, in which case just record that a dependency is in play.
        // If the existing elem has a different mvid, fail.
        if (IsEqualGUID(mvid, foundElem->Mvid))
        {
            // Mvid matches exactly.
            if (foundElem->AssemblyRequirementName == NULL)
            {
                foundElem->AssemblyRequirementName = imageName;
                foundElem->CompositeComponent = compositeComponent;
            }
        }
        else
        {
            MvidMismatchFatalError(foundElem->Mvid, mvid, simpleName, compositeComponent, imageName);
        }
    }

    if (addAllLoadedModules)
    {
        for (COUNT_T assemblyIndex = 0; assemblyIndex < m_loadedAssemblies.GetCount(); assemblyIndex++)
        {
           DeclareLoadedAssembly(m_loadedAssemblies[assemblyIndex]);
        }
    }
}

void AssemblyBinder::DeclareLoadedAssembly(Assembly* loadedAssembly)
{
    // If table is empty, then no mvid dependencies have been declared, so we don't need to record this information
    if (m_assemblySimpleNameMvidCheckHash.GetCount() == 0)
        return;

    GUID mvid;
    loadedAssembly->GetMDImport()->GetScopeProps(NULL, &mvid);

    LPCUTF8 simpleName = loadedAssembly->GetSimpleName();

    SimpleNameToExpectedMVIDAndRequiringAssembly* foundElem = (SimpleNameToExpectedMVIDAndRequiringAssembly*)m_assemblySimpleNameMvidCheckHash.LookupPtr(simpleName);
    if (foundElem == NULL)
    {
        SimpleNameToExpectedMVIDAndRequiringAssembly newElem(simpleName, mvid, false, NULL);
        m_assemblySimpleNameMvidCheckHash.Add(newElem);
    }
    else
    {
        // Elem already exists. Determine if the existing elem is another one with the same mvid, in which case do nothing. Everything is fine here.
        // If the existing elem has a different mvid, but isn't a dependency on exact mvid elem, then set the mvid to all 0.
        // If the existing elem has a different mvid, and is a dependency on exact mvid elem, then we've hit a fatal error.
        if (IsEqualGUID(mvid, foundElem->Mvid))
        {
            // Mvid matches exactly.
        }
        else if (foundElem->AssemblyRequirementName == NULL)
        {
            // Another loaded assembly, set the stored Mvid to all zeroes to indicate that it isn't a unique mvid
            memset(&foundElem->Mvid, 0, sizeof(GUID));
        }
        else
        {
            MvidMismatchFatalError(mvid, foundElem->Mvid, simpleName, foundElem->CompositeComponent, foundElem->AssemblyRequirementName);
        }
    }
}
#endif // FEATURE_READYTORUN

void AssemblyBinder::AddLoadedAssembly(Assembly* loadedAssembly)
{
    BaseDomain::LoadLockHolder lock(AppDomain::GetCurrentDomain());
    m_loadedAssemblies.Append(loadedAssembly);

#ifdef FEATURE_READYTORUN
    DeclareLoadedAssembly(loadedAssembly);
#endif // FEATURE_READYTORUN
}

void AssemblyBinder::GetNameForDiagnosticsFromManagedALC(INT_PTR managedALC, /* out */ SString& alcName)
{
    if (managedALC == GetAppDomain()->GetDefaultBinder()->GetManagedAssemblyLoadContext())
    {
        alcName.Set(W("Default"));
        return;
    }

    OBJECTREF* alc = reinterpret_cast<OBJECTREF*>(managedALC);

    GCX_COOP();
    struct {
        STRINGREF alcName;
    } gc;
    gc.alcName = NULL;

    GCPROTECT_BEGIN(gc);

    PREPARE_VIRTUAL_CALLSITE(METHOD__OBJECT__TO_STRING, *alc);
    DECLARE_ARGHOLDER_ARRAY(args, 1);
    args[ARGNUM_0] = OBJECTREF_TO_ARGHOLDER(*alc);
    CALL_MANAGED_METHOD_RETREF(gc.alcName, STRINGREF, args);
    gc.alcName->GetSString(alcName);

    GCPROTECT_END();
}

void AssemblyBinder::GetNameForDiagnostics(/*out*/ SString& alcName)
{
    _ASSERTE(this != nullptr);

    if (IsDefault())
    {
        alcName.Set(W("Default"));
    }
    else
    {
        GetNameForDiagnosticsFromManagedALC(GetManagedAssemblyLoadContext(), alcName);
    }
}

void AssemblyBinder::GetNameForDiagnosticsFromSpec(AssemblySpec* spec, /*out*/ SString& alcName)
{
    _ASSERTE(spec != nullptr);

    AppDomain* domain = spec->GetAppDomain();
    AssemblyBinder* binder = spec->GetBinder();
    if (binder == nullptr)
        binder = spec->GetBinderFromParentAssembly(domain);

    binder->GetNameForDiagnostics(alcName);
}

#endif  //DACCESS_COMPILE
