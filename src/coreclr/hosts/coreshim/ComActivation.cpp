// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "CoreShim.h"

#include <vector>

namespace
{
    HRESULT InitializeCoreClr(_In_ coreclr* inst)
    {
        assert(inst != nullptr);

        HRESULT hr;

        std::string tpaList;
        RETURN_IF_FAILED(coreclr::CreateTpaList(tpaList));

        const char *keys[] =
        {
            "APP_PATHS",
            "TRUSTED_PLATFORM_ASSEMBLIES",
        };

        std::string assemblyPath;
        RETURN_IF_FAILED(Utility::GetCoreShimDirectory(assemblyPath));

        const char *values[] =
        {
            assemblyPath.c_str(),
            tpaList.c_str(),
        };

        static_assert(ARRAY_SIZE(keys) == ARRAY_SIZE(values), "key/values pairs should match in length");

        return inst->Initialize(ARRAY_SIZE(keys), keys, values, "COMAct");
    }
}

STDAPI DllGetClassObject(
    _In_ REFCLSID rclsid,
    _In_ REFIID riid,
    _Outptr_ LPVOID FAR* ppv)
{
    HRESULT hr;

    coreclr *inst;
    RETURN_IF_FAILED(coreclr::GetCoreClrInstance(&inst));

    if (hr == S_OK)
        RETURN_IF_FAILED(InitializeCoreClr(inst));

    using GetClassFactoryForTypeInternal_ptr = HRESULT(*)(void *);
    GetClassFactoryForTypeInternal_ptr GetClassFactoryForTypeInternal;
    RETURN_IF_FAILED(inst->CreateDelegate(
        "System.Private.CoreLib",
        "Internal.Runtime.InteropServices.ComActivator",
        "GetClassFactoryForTypeInternal", (void**)&GetClassFactoryForTypeInternal));

    // Get assembly and type for activation
    std::wstring assemblyName;
    RETURN_IF_FAILED(Utility::TryGetEnvVar(COMACT_ASSEMBLYNAME_ENVVAR, assemblyName));

    std::wstring typeName;
    RETURN_IF_FAILED(Utility::TryGetEnvVar(COMACT_TYPENAME_ENVVAR, typeName));

    // Compute the path to the assembly. This should be adjacent to CoreShim (i.e. this library).
    std::wstring assemblyPath;
    RETURN_IF_FAILED(Utility::GetCoreShimDirectory(assemblyPath));
    assemblyPath.append(assemblyName);
    assemblyPath.append(W(".dll"));

    IUnknown *ccw = nullptr;

    struct ComActivationContext
    {
        GUID ClassId;
        GUID InterfaceId;
        const WCHAR *AssemblyPath;
        const WCHAR *AssemblyName;
        const WCHAR *TypeName;
        void **ClassFactoryDest;
    } comCxt{ rclsid, riid, assemblyPath.data(), assemblyName.data(), typeName.data(), (void**)&ccw };

    RETURN_IF_FAILED(GetClassFactoryForTypeInternal(&comCxt));
    assert(ccw != nullptr);

    hr = ccw->QueryInterface(riid, ppv);
    ccw->Release();
    return hr;
}

STDAPI DllCanUnloadNow(void)
{
    return S_FALSE;
}
