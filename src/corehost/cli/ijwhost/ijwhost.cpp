// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "ijwhost.h"
#include "hostfxr.h"
#include "fxr_resolver.h"
#include "pal.h"
#include "trace.h"
#include "error_codes.h"
#include "utils.h"
#include "bootstrap_thunk.h"


#if defined(_WIN32)
// IJW entry points are defined without the __declspec(dllexport) attribute.
// The issue here is that the MSVC compiler links to the exact name _CorDllMain instead of their stdcall-managled names.
// So we need to export the exact name, which __declspec(dllexport) doesn't do. The solution here is to the use a .def file on Windows.
#define IJW_API extern "C"
#else
#define IJW_API SHARED_API
#endif // _WIN32

pal::hresult_t get_load_in_memory_assembly_delegate(pal::dll_t handle, load_in_memory_assembly_fn* delegate)
{
    pal::dll_t fxr;

    pal::string_t host_path;
    if (!pal::get_own_module_path(&host_path) || !pal::realpath(&host_path))
    {
        trace::error(_X("Failed to resolve full path of the current host module [%s]"), host_path.c_str());
        return StatusCode::CoreHostCurHostFindFailure;
    }

    pal::string_t dotnet_root;
    pal::string_t fxr_path;
    if (!fxr_resolver::try_get_path(get_directory(host_path), &dotnet_root, &fxr_path))
    {
        return StatusCode::CoreHostLibMissingFailure;
    }

    // Load library
    if (!pal::load_library(&fxr_path, &fxr))
    {
        trace::error(_X("The library %s was found, but loading it from %s failed"), LIBFXR_NAME, fxr_path.c_str());
        trace::error(_X("  - Installing .NET Core prerequisites might help resolve this problem."));
        trace::error(_X("     %s"), DOTNET_CORE_INSTALL_PREREQUISITES_URL);
        return StatusCode::CoreHostLibLoadFailure;
    }

    // Leak fxr

    auto get_delegate_from_hostfxr = (hostfxr_get_delegate_fn)pal::get_symbol(fxr, "hostfxr_get_runtime_delegate");
    if (get_delegate_from_hostfxr == nullptr)
        return StatusCode::CoreHostEntryPointFailure;

    pal::string_t app_path;

    if (!pal::get_module_path(handle, &app_path))
    {
        trace::error(_X("Failed to resolve full path of the current mixed-mode module [%s]"), host_path.c_str());
        return StatusCode::LibHostCurExeFindFailure;
    }

    return get_delegate_from_hostfxr(host_path.c_str(), dotnet_root.c_str(), app_path.c_str(), hostfxr_delegate_type::load_in_memory_assembly, (void**)delegate);
}

IJW_API BOOL STDMETHODCALLTYPE _CorDllMain(HINSTANCE hInst,
    DWORD  dwReason,
    LPVOID lpReserved
)
{
    BOOL res = TRUE;

    PEDecoder pe(hInst);

    // In the following code, want to make sure that we do our own initialization before
    // we call into managed or unmanaged initialization code, and that we perform
    // uninitialization after we call into managed or unmanaged uninitialization code.
    // Thus, we do DLL_PROCESS_ATTACH work first, and DLL_PROCESS_DETACH work last.
    if (dwReason == DLL_PROCESS_ATTACH)
    {
        // If this is not a .NET module (has a CorHeader), shouldn't be calling _CorDllMain
        if (!pe.HasCorHeader())
        {
            return FALSE;
        }

        if (!pe.HasManagedEntryPoint() && !pe.HasNativeEntryPoint())
        {
            // If there is no user entry point, then we don't want the
            // thread start/stop events going through because it slows down
            // thread creation operations
            DisableThreadLibraryCalls(hInst);
        }

        // Install the bootstrap thunks
        if (!patch_vtable_entries(pe))
        {
            return FALSE;
        }
    }

    // Now call the unmanaged entrypoint if it exists
    if (pe.HasNativeEntryPoint())
    {
        DllMain_t pUnmanagedDllMain = (DllMain_t)pe.GetNativeEntryPoint();
        assert(pUnmanagedDllMain != nullptr);
        res = pUnmanagedDllMain(hInst, dwReason, lpReserved);
    }

    if (dwReason == DLL_PROCESS_DETACH)
    {
        release_bootstrap_thunks(pe);
    }

    return res;
}

BOOL STDMETHODCALLTYPE DllMain(HINSTANCE hInst,
    DWORD  dwReason,
    LPVOID lpReserved
)
{
    switch (dwReason)
    {
    case DLL_PROCESS_ATTACH:
        g_heapHandle = HeapCreate(HEAP_CREATE_ENABLE_EXECUTE, 0, 0);
        return g_heapHandle != NULL ? TRUE : FALSE;
    case DLL_PROCESS_DETACH:
        HeapDestroy(g_heapHandle);
        break;
    }
    return TRUE;
}

SHARED_API mdToken STDMETHODCALLTYPE GetTokenForVTableEntry(HMODULE hMod, void** ppVTEntry)
{
    mdToken tok;
    if (are_thunks_installed_for_module(hMod))
    {
        bootstrap_thunk* pThunk =
            bootstrap_thunk::get_thunk_from_entrypoint((std::uintptr_t) *ppVTEntry);
        tok = (mdToken) pThunk->get_token();
    }
    else
    {
        tok = (mdToken)(std::uintptr_t) *ppVTEntry;
    }

    return tok;
}
