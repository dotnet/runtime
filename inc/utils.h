#ifndef UTILS_H
#define UTILS_H

#include "pal.h"

bool ends_with(const pal::string_t& value, const pal::string_t& suffix);
pal::string_t get_executable(const pal::string_t& filename);
pal::string_t get_directory(const pal::string_t& path);
pal::string_t get_filename(const pal::string_t& path);
bool find_coreclr(const pal::string_t& appbase, pal::string_t& recv);
void append_path(pal::string_t& path1, const pal::char_t* path2);

#endif
