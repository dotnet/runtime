// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef UTILS_H
#define UTILS_H

#include "pal.h"
#include "trace.h"
#include <type_traits>
#include <runtime_version.h>
#include <minipal/utils.h>

#if defined(_WIN32)
#define DOTNET_CORE_INSTALL_PREREQUISITES_URL _X("https://go.microsoft.com/fwlink/?linkid=798306")
#elif defined(TARGET_OSX)
#define DOTNET_CORE_INSTALL_PREREQUISITES_URL _X("https://go.microsoft.com/fwlink/?linkid=2063366")
#else
#define DOTNET_CORE_INSTALL_PREREQUISITES_URL _X("https://go.microsoft.com/fwlink/?linkid=2063370")
#endif
#define DOTNET_CORE_DOWNLOAD_URL _X("https://aka.ms/dotnet/download")
#define DOTNET_CORE_APPLAUNCH_URL _X("https://aka.ms/dotnet-core-applaunch")

#define DOTNET_INFO_URL _X("https://aka.ms/dotnet/info")
#define DOTNET_APP_LAUNCH_FAILED_URL _X("https://aka.ms/dotnet/app-launch-failed")
#define DOTNET_SDK_NOT_FOUND_URL _X("https://aka.ms/dotnet/sdk-not-found")

// This message is defined here for consistency between errors on the command line and GUI (Windows apphost).
#define INSTALL_OR_UPDATE_NET_ERROR_MESSAGE _X("You must install or update .NET to run this application.")

#define INSTALL_NET_ERROR_MESSAGE _X("You must install .NET to run this application.")
#define INSTALL_NET_DESKTOP_ERROR_MESSAGE _X("You must install .NET Desktop Runtime to run this application.")

#define DOC_LINK_INTRO _X("Learn more:")

#define MISSING_RUNTIME_ERROR_FORMAT \
    _X("%s\n\n")                                \
    _X("App: %s\n")                             \
    _X("Architecture: %s\n")                    \
    _X("App host version: %s\n")                \
    _X(".NET location: %s\n")                   \
    _X("\n")                                    \
    DOC_LINK_INTRO _X("\n")                     \
    DOTNET_APP_LAUNCH_FAILED_URL                \
    _X("\n\n")                                  \
    _X("Download the .NET runtime:\n")          \
    _X("%s&apphost_version=%s")

#define DOTNET_ROOT_ENV_VAR _X("DOTNET_ROOT")

#define SDK_DOTNET_DLL _X("dotnet.dll")

#define _TEXT(x) #x
#define _QUOTE(x) _TEXT(x)

#define HOST_VERSION _QUOTE(RuntimeProductVersion)

bool ends_with(const pal::string_t& value, const pal::string_t& suffix, bool match_case);
bool starts_with(const pal::string_t& value, const pal::string_t& prefix, bool match_case);

namespace utils
{
    template<size_t L>
    inline constexpr size_t strlen(const pal::char_t(&)[L])
    {
        return L - 1;
    }

    bool ends_with(const pal::string_t& value, const pal::char_t *suffix, size_t suffix_len, bool match_case);
    bool starts_with(const pal::string_t& value, const pal::char_t* prefix, size_t prefix_len, bool match_case);

    template<size_t L>
    bool ends_with(const pal::string_t& value, const pal::char_t (&suffix)[L], bool match_case)
    {
        return ends_with(value, suffix, L - 1, match_case);
    }

    template<size_t L>
    bool starts_with(const pal::string_t& value, const pal::char_t (&prefix)[L], bool match_case)
    {
        return starts_with(value, prefix, L - 1, match_case);
    }
}

pal::string_t strip_executable_ext(const pal::string_t& filename);
pal::string_t get_directory(const pal::string_t& path);
pal::string_t strip_file_ext(const pal::string_t& path);
pal::string_t get_filename(const pal::string_t& path);
pal::string_t get_filename_without_ext(const pal::string_t& path);
void append_path(pal::string_t* path1, const pal::char_t* path2);
bool library_exists_in_dir(const pal::string_t& lib_dir, const pal::string_t& lib_name, pal::string_t* p_lib_path);
bool coreclr_exists_in_dir(const pal::string_t& candidate);
void remove_trailing_dir_separator(pal::string_t* dir);
void replace_char(pal::string_t* path, pal::char_t match, pal::char_t repl);
pal::string_t get_replaced_char(const pal::string_t& path, pal::char_t match, pal::char_t repl);

pal::architecture get_current_arch();
const pal::char_t* get_arch_name(pal::architecture arch);
const pal::char_t* get_current_arch_name();

pal::string_t get_runtime_id();
bool try_get_runtime_id_from_env(pal::string_t& out_rid);

bool multilevel_lookup_enabled();
void get_framework_and_sdk_locations(const pal::string_t& dotnet_dir, const bool disable_multilevel_lookup, std::vector<pal::string_t>* locations);
bool get_file_path_from_env(const pal::char_t* env_key, pal::string_t* recv);
size_t index_of_non_numeric(const pal::string_t& str, size_t i);
bool try_stou(const pal::string_t& str, unsigned* num);

pal::string_t get_dotnet_root_env_var_for_arch(pal::architecture arch);
bool get_dotnet_root_from_env(pal::string_t* used_dotnet_root_env_var_name, pal::string_t* recv);

pal::string_t get_deps_from_app_binary(const pal::string_t& app_base, const pal::string_t& app);
pal::string_t get_runtime_config_path(const pal::string_t& path, const pal::string_t& name);
pal::string_t get_runtime_config_dev_path(const pal::string_t& path, const pal::string_t& name);
void get_runtime_config_paths(const pal::string_t& path, const pal::string_t& name, pal::string_t* cfg, pal::string_t* dev_cfg);
pal::string_t get_dotnet_root_from_fxr_path(const pal::string_t& fxr_path);

// Get a download URL for a specific framework and version
// If no framework is specified, a download URL for the runtime is returned
pal::string_t get_download_url(const pal::char_t* framework_name = nullptr, const pal::char_t* framework_version = nullptr);

pal::string_t get_host_version_description();

pal::string_t to_lower(const pal::char_t* in);
pal::string_t to_upper(const pal::char_t* in);

// Retrieves environment variable which is only used for testing.
// This will return the value of the variable only if the product binary is stamped
// with test-only marker.
bool test_only_getenv(const pal::char_t* name, pal::string_t* recv);

// Helper class to make it easy to propagate error writer to the hostpolicy
class propagate_error_writer_t
{
public:
    typedef trace::error_writer_fn(__cdecl* set_error_writer_fn)(trace::error_writer_fn error_writer);

private:
    set_error_writer_fn m_set_error_writer;
    bool m_error_writer_set;

public:
    propagate_error_writer_t(set_error_writer_fn set_error_writer)
    {
        // Previous trace messages from the caller module must be printed before calling trace::setup in callee module
        // The two modules have different trace util instances and thus don't share file IO buffers
        // Not flushing may lead to traces from before the call being written after the call due to module mismatch.
        trace::flush();

        m_set_error_writer = set_error_writer;
        m_error_writer_set = false;

        trace::error_writer_fn error_writer = trace::get_error_writer();
        if (error_writer != nullptr && m_set_error_writer != nullptr)
        {
            m_set_error_writer(error_writer);
            m_error_writer_set = true;
        }
    }

    ~propagate_error_writer_t()
    {
        if (m_error_writer_set && m_set_error_writer != nullptr)
        {
            m_set_error_writer(nullptr);
            m_error_writer_set = false;
        }
    }
};

// Helper class to make it easy to change the error writer within a specific scope only.
class error_writer_scope_t
{
private:
    trace::error_writer_fn m_old_error_writer;

public:
    error_writer_scope_t(trace::error_writer_fn new_error_writer)
    {
        m_old_error_writer = trace::set_error_writer(new_error_writer);
    }

    ~error_writer_scope_t()
    {
        trace::set_error_writer(m_old_error_writer);
    }
};

template<typename T>
size_t to_size_t_dbgchecked(T value)
{
    assert(value >= 0);
    size_t result = static_cast<size_t>(value);
    assert(static_cast<T>(result) == value);
    return result;
}

#endif
