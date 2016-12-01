// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#ifndef UTILS_H
#define UTILS_H

#include "pal.h"

#define _STRINGIFY(s) _X(s)
#define DOTNET_CORE_URL _X("http://go.microsoft.com/fwlink/?LinkID=798306&clcid=0x409")

template <typename T>
class at_scope_exit
{
public:
    const T& l;
    at_scope_exit(const T& l) : l(l) { }
    ~at_scope_exit() { l(); }
}; 

bool ends_with(const pal::string_t& value, const pal::string_t& suffix, bool match_case);
bool starts_with(const pal::string_t& value, const pal::string_t& prefix, bool match_case);
pal::string_t get_executable(const pal::string_t& filename);
pal::string_t get_directory(const pal::string_t& path);
pal::string_t strip_file_ext(const pal::string_t& path);
pal::string_t get_filename(const pal::string_t& path);
pal::string_t get_filename_without_ext(const pal::string_t& path);
void append_path(pal::string_t* path1, const pal::char_t* path2);
bool library_exists_in_dir(const pal::string_t& lib_dir, const pal::string_t& lib_name, pal::string_t* p_lib_path);
bool coreclr_exists_in_dir(const pal::string_t& candidate);
void replace_char(pal::string_t* path, pal::char_t match, pal::char_t repl);
const pal::char_t* get_arch();
pal::string_t get_last_known_arg(
    const std::unordered_map<pal::string_t, std::vector<pal::string_t>>& opts,
    const pal::string_t& opt_key,
    const pal::string_t& de_fault);
bool parse_known_args(
    const int argc,
    const pal::char_t* argv[],
    const std::vector<pal::string_t>& known_opts,
    std::unordered_map<pal::string_t, std::vector<pal::string_t>>* opts,
    int* num_args);
bool skip_utf8_bom(pal::ifstream_t* stream);
#endif
