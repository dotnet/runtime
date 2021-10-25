// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef UTILS_H
#define UTILS_H

#include "pal.h"
#include "trace.h"
#include <type_traits>

#define _STRINGIFY(s) _X(s)
#if defined(_WIN32)
#define DOTNET_CORE_INSTALL_PREREQUISITES_URL _X("https://go.microsoft.com/fwlink/?linkid=798306")
#elif defined(TARGET_OSX)
#define DOTNET_CORE_INSTALL_PREREQUISITES_URL _X("https://go.microsoft.com/fwlink/?linkid=2063366")
#else
#define DOTNET_CORE_INSTALL_PREREQUISITES_URL _X("https://go.microsoft.com/fwlink/?linkid=2063370")
#endif
#define DOTNET_CORE_DOWNLOAD_URL _X("https://aka.ms/dotnet-download")
#define DOTNET_CORE_APPLAUNCH_URL _X("https://aka.ms/dotnet-core-applaunch")

#define RUNTIME_STORE_DIRECTORY_NAME _X("store")

bool ends_with(const pal::string_t& value, const pal::string_t& suffix, bool match_case);
bool starts_with(const pal::string_t& value, const pal::string_t& prefix, bool match_case);
pal::string_t strip_executable_ext(const pal::string_t& filename);
pal::string_t get_directory(const pal::string_t& path);
pal::string_t strip_file_ext(const pal::string_t& path);
pal::string_t get_filename(const pal::string_t& path);
pal::string_t get_filename_without_ext(const pal::string_t& path);
void append_path(pal::string_t* path1, const pal::char_t* path2);
bool library_exists_in_dir(const pal::string_t& lib_dir, const pal::string_t& lib_name, pal::string_t* p_lib_path);
bool coreclr_exists_in_dir(const pal::string_t& candidate);
void remove_trailing_dir_seperator(pal::string_t* dir);
void replace_char(pal::string_t* path, pal::char_t match, pal::char_t repl);
pal::string_t get_replaced_char(const pal::string_t& path, pal::char_t match, pal::char_t repl);
const pal::char_t* get_arch();
pal::string_t get_current_runtime_id(bool use_fallback);
bool get_env_shared_store_dirs(std::vector<pal::string_t>* dirs, const pal::string_t& arch, const pal::string_t& tfm);
bool get_global_shared_store_dirs(std::vector<pal::string_t>* dirs, const pal::string_t& arch, const pal::string_t& tfm);
bool multilevel_lookup_enabled();
void get_framework_and_sdk_locations(const pal::string_t& dotnet_dir, std::vector<pal::string_t>* locations);
bool get_file_path_from_env(const pal::char_t* env_key, pal::string_t* recv);
size_t index_of_non_numeric(const pal::string_t& str, size_t i);
bool try_stou(const pal::string_t& str, unsigned* num);
bool get_dotnet_root_from_env(pal::string_t* used_dotnet_root_env_var_name, pal::string_t* recv);
pal::string_t get_deps_from_app_binary(const pal::string_t& app_base, const pal::string_t& app);
pal::string_t get_runtime_config_path(const pal::string_t& path, const pal::string_t& name);
pal::string_t get_runtime_config_dev_path(const pal::string_t& path, const pal::string_t& name);
void get_runtime_config_paths(const pal::string_t& path, const pal::string_t& name, pal::string_t* cfg, pal::string_t* dev_cfg);
pal::string_t get_dotnet_root_from_fxr_path(const pal::string_t& fxr_path);

// Get a download URL for a specific framework and version
// If no framework is specified, a download URL for the runtime is returned
pal::string_t get_download_url(const pal::char_t* framework_name = nullptr, const pal::char_t* framework_version = nullptr);

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
