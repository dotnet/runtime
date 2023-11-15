// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "assemblybinder.h"
#include "nativeimage.h"
#include "../binder/inc/assemblyname.hpp"

#ifndef DACCESS_COMPILE

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
