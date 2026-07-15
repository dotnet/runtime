// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// CoreCLR-WASI host.
//
// This is the shipping corehost for the CoreCLR-WASI library-test leg. It is built as a
// static archive (libWasiHost.a) and linked per-app by the WASI app builder
// (src/mono/wasi/build/WasiApp.CoreCLR.targets), whole-archive so main()/_start are pulled,
// together with the runtime-pack static libraries (libcoreclr_static.a, libSystem.Native.a,
// ...) and the app-generated P/Invoke callhelpers. This mirrors the browser host
// (src/native/corehost/browserhost) and the static apphost (src/native/corehost/apphost/static);
// the coreclr-internal corerun (src/coreclr/hosts/corerun) keeps its role for CoreCLR runtime
// tests.
//
// The host is a thin one: it constructs the CoreCLR initialization properties from CORE_ROOT
// and calls coreclr_initialize / coreclr_execute_assembly directly (declared extern against the
// statically linked runtime), the same shape as browserhost.cpp but with a real wasi:cli/run
// main() (there is no JS driver). The path/TPA logic is shared with corerun via its pal header so
// the CORE_ROOT resolution, directory enumeration and absolute-path handling stay identical to the
// validated corerun-based host.
//
// See https://github.com/dotnet/runtime/issues/130129.
//

#include <cstdint>
#include <cstdlib>
#include <set>
#include <sstream>

// Shared pal (path handling, directory enumeration, CORE_ROOT/TPA helpers). The header is
// self-contained and TARGET_WASM-guarded; only header-only helpers are used here, so no corerun
// runtime object is required in this archive.
#include "corerun.hpp"

#include <host_runtime_contract.h>

using pal::char_t;
using pal::string_t;

namespace envvar
{
    const char_t* const coreRoot = W("CORE_ROOT");
    const char_t* const coreLibraries = W("CORE_LIBRARIES");
    const char_t* const printExitCode = W("DOTNET_WASI_PRINT_EXIT_CODE");
}

// CoreCLR entry points. On wasi the runtime is statically linked, so these are resolved at the
// per-app relink from libcoreclr_static.a (declared extern, as browserhost.cpp does) rather than
// looked up dynamically.
extern "C"
{
    int coreclr_initialize(
        const char* exePath,
        const char* appDomainFriendlyName,
        int propertyCount,
        const char** propertyKeys,
        const char** propertyValues,
        void** hostHandle,
        unsigned int* domainId);

    int coreclr_execute_assembly(
        void* hostHandle,
        unsigned int domainId,
        int argc,
        const char** argv,
        const char* managedAssemblyPath,
        unsigned int* exitCode);

    int coreclr_shutdown_2(
        void* hostHandle,
        unsigned int domainId,
        int* latchedExitCode);

    int coreclr_set_error_writer(void (*errorWriter)(const char* line));
}

// The app-generated P/Invoke resolver, produced per-app by ManagedToNativeGenerator and linked at
// the relink (replacing libcoreclr_gen_static.a). Passed to the runtime via the host contract
// below (pinvoke_override); coreclr_initialize forwards it to PInvokeOverride::SetPInvokeOverride
// with Source::RuntimeConfiguration - the same registration corerun performs via
// add_pinvoke_override() - so the app callhelpers are hooked and reverse thunks for the app/test
// [UnmanagedCallersOnly] methods resolve (else precode_portable.cpp asserts). See
// coreclr_initialize in src/coreclr/dlls/mscoree/exports.cpp. Declared as a plain (C++-mangled)
// function to match the generated definition, as corerun/browserhost do - not extern "C".
const void* callhelpers_pinvoke_override(const char* library_name, const char* entry_point_name);

// Fake implementations to satisfy the linker without pulling
// libSystem.Runtime.InteropServices.JavaScript.Native (a browser-only library) into the wasi
// relink; these JS interop QCall targets are referenced by libcoreclr_static.a but never called on
// wasi. Ported from src/coreclr/hosts/corerun/wasm/pinvoke_override.cpp.
extern "C"
{
    void* SystemInteropJS_BindJSImportST(void*) { std::abort(); }
    void SystemInteropJS_CancelPromise(void*) { std::abort(); }
    void SystemInteropJS_InvokeJSFunction(void*, void*) { std::abort(); }
    void SystemInteropJS_InvokeJSImportST(int32_t, void*) { std::abort(); }
    void SystemInteropJS_ReleaseCSOwnedObject(void*) { std::abort(); }
    void SystemInteropJS_ResolveOrRejectPromise(void*) { std::abort(); }
}

// Provided by libSystem.Globalization.Native.a when globalization is linked (the non-invariant
// per-app relink). Declared weak so an invariant relink (which omits that archive) leaves the
// reference null and the host ICU preload below is skipped.
extern "C" __attribute__((weak)) int32_t GlobalizationNative_LoadICUData(const char* path);

// Initialization properties, kept alive for the lifetime of the process so the host runtime
// contract callback can serve them.
static std::vector<std::string> s_property_keys;
static std::vector<std::string> s_property_values;

static void log_error_info(const char* line)
{
    std::fprintf(stderr, "%s\n", line);
}

// N.B. CoreCLR doesn't always use the first instance of an assembly on the TPA list (ni's may be
// preferred over il even if they appear later). Include only the first instance of a simple name.
static string_t build_tpa(const string_t& core_root, const string_t& core_libraries)
{
    static const char_t* const tpa_extensions[] =
    {
        W(".dll"),
        W(".exe"),
        nullptr
    };

    std::set<string_t> name_set;
    pal::stringstream_t tpa_list;

    for (const char_t* const* curr_ext = tpa_extensions; *curr_ext != nullptr; ++curr_ext)
    {
        const char_t* ext = *curr_ext;
        const size_t ext_len = pal::strlen(ext);

        for (const string_t& dir : { core_libraries, core_root })
        {
            if (dir.empty())
                continue;

            string_t tmp = pal::build_file_list(dir, ext, [&](const char_t* file)
                {
                    string_t file_local{ file };

                    if (pal::string_ends_with(file_local, ext_len, ext))
                        file_local = file_local.substr(0, file_local.length() - ext_len);

                    return name_set.insert(file_local).second;
                });

            tpa_list << tmp;
        }
    }

    return tpa_list.str();
}

static size_t HOST_CONTRACT_CALLTYPE get_runtime_property(
    const char* key,
    /*out*/ char* value_buffer,
    size_t value_buffer_size,
    void* /*contract_context*/)
{
    for (size_t i = 0; i < s_property_keys.size(); ++i)
    {
        if (s_property_keys[i] == key)
        {
            const std::string& value = s_property_values[i];
            size_t len = value.length();
            if (value_buffer != nullptr && value_buffer_size > len)
                ::memcpy(value_buffer, value.c_str(), len + 1);

            return len + 1;
        }
    }

    return (size_t)-1;
}

int main(int argc, char* argv[])
{
    if (argc < 2)
    {
        std::fprintf(stderr, "USAGE: %s <assembly> [arguments]\n", argc > 0 ? argv[0] : "wasihost");
        return -1;
    }

    string_t exe_path = pal::get_exe_path();

    // The first argument is the managed entry assembly; the rest are passed to it.
    string_t entry_assembly = pal::get_absolute_path(argv[1]);
    int entry_argc = argc - 2;
    const char** entry_argv = entry_argc > 0 ? (const char**)&argv[2] : nullptr;

    // The application directory is where the entry assembly lives.
    string_t app_path;
    {
        string_t file;
        pal::split_path_to_dir_filename(entry_assembly, app_path, file);
        pal::ensure_trailing_delimiter(app_path);
    }

    pal::stringstream_t native_search_dirs;
    native_search_dirs << app_path << pal::env_path_delim;

    string_t core_libs = pal::getenv(envvar::coreLibraries);
    if (!core_libs.empty() && core_libs != app_path)
    {
        pal::ensure_trailing_delimiter(core_libs);
        native_search_dirs << core_libs << pal::env_path_delim;
    }

    // CORE_ROOT locates the framework assemblies (and, on non-wasm, the runtime). On wasi the
    // runtime is statically linked, so CORE_ROOT is only used to build the TPA list. Fall back to
    // the host's own directory when unset.
    string_t core_root = pal::getenv(envvar::coreRoot);
    if (core_root.empty())
    {
        string_t file;
        pal::split_path_to_dir_filename(exe_path, core_root, file);
    }
    pal::ensure_trailing_delimiter(core_root);
    native_search_dirs << core_root << pal::env_path_delim;

    string_t tpa_list = build_tpa(core_root, core_libs);

    s_property_keys.push_back("TRUSTED_PLATFORM_ASSEMBLIES");
    s_property_values.push_back(tpa_list);

    s_property_keys.push_back("APP_PATHS");
    s_property_values.push_back(app_path);

    s_property_keys.push_back("NATIVE_DLL_SEARCH_DIRECTORIES");
    s_property_values.push_back(native_search_dirs.str());

    host_runtime_contract host_contract = {
        sizeof(host_runtime_contract),
        nullptr,
        &get_runtime_property,
        nullptr,
        &callhelpers_pinvoke_override };
    {
        std::stringstream ss;
        ss << "0x" << std::hex << (size_t)(&host_contract);
        s_property_keys.push_back(HOST_PROPERTY_RUNTIME_CONTRACT);
        s_property_values.push_back(ss.str());
    }

    std::vector<const char*> property_keys;
    std::vector<const char*> property_values;
    for (const std::string& key : s_property_keys)
        property_keys.push_back(key.c_str());
    for (const std::string& value : s_property_values)
        property_values.push_back(value.c_str());

    coreclr_set_error_writer(log_error_info);

    void* host_handle = nullptr;
    unsigned int domain_id = 0;
    int result = coreclr_initialize(
        exe_path.c_str(),
        "wasihost",
        (int)property_keys.size(),
        property_keys.data(),
        property_values.data(),
        &host_handle,
        &domain_id);
    if (result < 0)
    {
        std::fprintf(stderr, "coreclr_initialize failed - Error: 0x%08x\n", result);
        return -1;
    }

    coreclr_set_error_writer(nullptr);

    // The static ICU shim requires the host to preload icudt.dat before managed globalization
    // initializes: GlobalizationNative_LoadICU() (the no-path entry the managed side calls on wasi)
    // returns 0 unless the data was already set, and falls back to invariant. This mirrors the
    // browser JS host calling wasm_load_icu_data. GlobalizationNative_LoadICUData is only linked
    // when globalization is enabled (the non-invariant per-app relink), so the reference is weak -
    // when globalization is not linked it is null and this is skipped (invariant). A missing
    // icudt.dat also falls back to invariant (the call just fails).
    if (GlobalizationNative_LoadICUData != nullptr)
    {
        string_t icu_data_path = app_path;
        icu_data_path.append(W("icudt.dat"));
        GlobalizationNative_LoadICUData(icu_data_path.c_str());
    }

    int exit_code = 0;
    result = coreclr_execute_assembly(
        host_handle,
        domain_id,
        entry_argc,
        entry_argv,
        entry_assembly.c_str(),
        (unsigned int*)&exit_code);
    if (result < 0)
    {
        std::fprintf(stderr, "coreclr_execute_assembly failed - Error: 0x%08x\n", result);
        return -1;
    }

    int latched_exit_code = exit_code;
    int shutdown_result = coreclr_shutdown_2(host_handle, domain_id, &latched_exit_code);
    if (shutdown_result < 0)
    {
        std::fprintf(stderr, "coreclr_shutdown_2 failed - Error: 0x%08x\n", shutdown_result);
        latched_exit_code = -1;
    }

    // wasi:cli/exit's exit(status: result) only signals ok/err, so wasmtime collapses any non-zero
    // Main return to host exit 1. When DOTNET_WASI_PRINT_EXIT_CODE=1, emit a "WASM EXIT <n>" marker
    // on stderr matching Mono (src/mono/wasi/runtime/main.c); the WASI launcher recovers the value
    // from it. wasi:cli/exit.exit-with-code(status-code: u8) is stable as of WASI 0.3.0/Preview 3
    // (@since(0.3.0)), but this host targets wasip2, where that world still gates it behind
    // @unstable(feature = cli-exit-with-code); adopting it (and dropping this marker + the launcher
    // parser) is gated on moving to a wasip3 target and toolchain support. See corerun.cpp.
    if (pal::getenv(envvar::printExitCode) == W("1"))
        std::fprintf(stderr, "WASM EXIT %d\n", latched_exit_code);

    return latched_exit_code;
}
