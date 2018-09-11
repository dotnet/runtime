// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        // [TODO] Support UNICODE app path
        char wd[MAX_PATH];
        (void)::GetCurrentDirectoryA(ARRAYSIZE(wd), wd);

        const char *values[] =
        {
            wd,
            tpaList.c_str(),
        };

        static_assert(ARRAYSIZE(keys) == ARRAYSIZE(values), "key/values pairs should match in length");

        return inst->Initialize(ARRAYSIZE(keys), keys, values, "COMAct");
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
        "System.Runtime.InteropServices.ComActivator",
        "GetClassFactoryForTypeInternal", (void**)&GetClassFactoryForTypeInternal));

    // Get assembly and type for activation
    std::string assemblyName;
    RETURN_IF_FAILED(Utility::TryGetEnvVar(COMACT_ASSEMBLYNAME_ENVVAR, assemblyName));

    std::string typeName;
    RETURN_IF_FAILED(Utility::TryGetEnvVar(COMACT_TYPENAME_ENVVAR, typeName));

    IUnknown *ccw = nullptr;

    struct ComActivationContext
    {
        GUID ClassId;
        GUID InterfaceId;
        const void *AssemblyName;
        const void *TypeName;
        void **ClassFactoryDest;
    } comCxt{ rclsid, riid, assemblyName.data(), typeName.data(), (void**)&ccw };

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
