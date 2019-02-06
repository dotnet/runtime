// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#ifndef UTILS_H
#define UTILS_H

#include "pal.h"
#include "trace.h"
struct host_option
{
    pal::string_t option;
    pal::string_t argument;
    pal::string_t description;
    bool framework_dependent;
};

#define _STRINGIFY(s) _X(s)
#if defined(_WIN32)
#define DOTNET_CORE_INSTALL_PREREQUISITES_URL _X("https://go.microsoft.com/fwlink/?linkid=798306")
#elif defined(__APPLE__)
#define DOTNET_CORE_INSTALL_PREREQUISITES_URL _X("https://go.microsoft.com/fwlink/?linkid=2063366")
#else
#define DOTNET_CORE_INSTALL_PREREQUISITES_URL _X("https://go.microsoft.com/fwlink/?linkid=2063370")
#endif
#define DOTNET_CORE_DOWNLOAD_RUNTIME_URL _X("https://aka.ms/dotnet-download-runtime")
#define DOTNET_CORE_DOWNLOAD_URL _X("https://aka.ms/dotnet-download")

#define RUNTIME_STORE_DIRECTORY_NAME _X("store")

typedef std::unordered_map<pal::string_t, std::vector<pal::string_t>> opt_map_t;

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
pal::string_t get_last_known_arg(
    const opt_map_t& opts,
    const pal::string_t& opt_key,
    const pal::string_t& de_fault);
bool parse_known_args(
    const int argc,
    const pal::char_t* argv[],
    const std::vector<host_option>& known_opts,
    opt_map_t* opts,
    int* num_args);
bool skip_utf8_bom(pal::ifstream_t* stream);
bool get_env_shared_store_dirs(std::vector<pal::string_t>* dirs, const pal::string_t& arch, const pal::string_t& tfm);
bool get_global_shared_store_dirs(std::vector<pal::string_t>* dirs, const pal::string_t& arch, const pal::string_t& tfm);
bool multilevel_lookup_enabled();
void get_framework_and_sdk_locations(const pal::string_t& dotnet_dir, std::vector<pal::string_t>* locations);
bool get_file_path_from_env(const pal::char_t* env_key, pal::string_t* recv);
size_t index_of_non_numeric(const pal::string_t& str, unsigned i);
bool try_stou(const pal::string_t& str, unsigned* num);
pal::string_t get_dotnet_root_env_var_name();
pal::string_t get_deps_from_app_binary(const pal::string_t& app_base, const pal::string_t& app);

// Helper class to make it easy to propagate error writer to the hostpolicy
class propagate_error_writer_t
{
public:
	typedef trace::error_writer_fn(*set_error_writer_fn)(trace::error_writer_fn error_writer);

private:
	set_error_writer_fn m_set_error_writer;
	bool m_error_writer_set;

public:
	propagate_error_writer_t(set_error_writer_fn set_error_writer)
	{
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
#endif
