// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Thin CoreCLR-WASI corehost, built as a static archive (libWasiHost.a) and linked per-app by the
// CoreCLR-WASI app builder against the statically-linked runtime. Mirrors browserhost, but with a
// real wasi:cli/run main() instead of a JS driver.
// See https://github.com/dotnet/runtime/issues/130129.

#include <cstdint>
#include <cstdio>
#include <cstring>
#include <set>
#include <sstream>
#include <string>
#include <vector>

// Shared pal (path handling, CORE_ROOT/TPA helpers); header-only, so no corerun object is linked.
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

// Statically linked at the per-app relink, so declared extern here (as browserhost does).
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

// App-generated P/Invoke resolver (per-app, replaces libcoreclr_gen_static.a at the relink), passed
// to the runtime via the host contract below so app callhelpers and reverse thunks resolve. Plain
// C++ linkage to match the generated definition (not extern "C").
const void* callhelpers_pinvoke_override(const char* library_name, const char* entry_point_name);

// Weak: only linked (from libSystem.Globalization.Native.a) in a non-invariant relink; null and
// skipped otherwise.
extern "C" __attribute__((weak)) int32_t GlobalizationNative_LoadICUData(const char* path);

// Init properties, kept alive for the process so the runtime contract callback can serve them.
static std::vector<std::string> s_property_keys;
static std::vector<std::string> s_property_values;

static void log_error_info(const char* line)
{
    std::fprintf(stderr, "%s\n", line);
}

// Include only the first instance of each simple assembly name (CoreCLR may otherwise prefer a
// later ni over an earlier il).
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

    // argv[1] is the managed entry assembly; argv[2..] are passed to it. Copy the slice into a
    // const char* vector rather than casting char** to const char**.
    string_t entry_assembly = pal::get_absolute_path(argv[1]);
    std::vector<const char*> entry_argv;
    for (int i = 2; i < argc; ++i)
        entry_argv.push_back(argv[i]);

    string_t app_path;
    {
        string_t file;
        pal::split_path_to_dir_filename(entry_assembly, app_path, file);
        pal::ensure_trailing_delimiter(app_path);
    }

    string_t core_libs = pal::getenv(envvar::coreLibraries);
    if (!core_libs.empty())
        pal::ensure_trailing_delimiter(core_libs);

    // CORE_ROOT locates the framework assemblies for the TPA list (the runtime itself is static on
    // wasi). On wasi the bundle co-locates the framework with the entry assembly, so default to the
    // entry assembly's directory; CORE_ROOT remains an optional override.
    string_t core_root = pal::getenv(envvar::coreRoot);
    if (core_root.empty())
        core_root = app_path;
    pal::ensure_trailing_delimiter(core_root);

    string_t exe_path = pal::get_exe_path();

    string_t tpa_list = build_tpa(core_root, core_libs);

    s_property_keys.push_back("TRUSTED_PLATFORM_ASSEMBLIES");
    s_property_values.push_back(tpa_list);

    s_property_keys.push_back("APP_PATHS");
    s_property_values.push_back(app_path);

    // NATIVE_DLL_SEARCH_DIRECTORIES is intentionally not set: on wasi native libraries are
    // statically linked and every P/Invoke is resolved by the pinvoke_override (callhelpers) before
    // the runtime's native-library search runs, and wasm has no shared-library/dlopen support, so
    // the search directories can never contribute a load.

    // Static: the contract must outlive coreclr_initialize (the runtime keeps its address). The
    // pinvoke_override field is forwarded to PInvokeOverride::SetPInvokeOverride by the runtime.
    static host_runtime_contract host_contract = {
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

    // The static ICU shim needs icudt.dat preloaded before managed globalization inits, otherwise it
    // falls back to invariant (mirrors the browser JS host's wasm_load_icu_data). Skipped for
    // invariant relinks (weak symbol null) and tolerant of a missing file.
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
        (int)entry_argv.size(),
        entry_argv.data(),
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

    // wasi:cli/exit's exit() only signals ok/err, so wasmtime collapses a non-zero result to host
    // exit 1. Under DOTNET_WASI_PRINT_EXIT_CODE=1, emit a "WASM EXIT <n>" marker the WASI launcher
    // parses (matching Mono). exit-with-code is stable in WASI 0.3 but still @unstable in the wasip2
    // world this targets; see corerun.cpp.
    if (pal::getenv(envvar::printExitCode) == W("1"))
        std::fprintf(stderr, "WASM EXIT %d\n", latched_exit_code);

    return latched_exit_code;
}
