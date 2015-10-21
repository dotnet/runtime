#ifndef UTILS_H
#define UTILS_H

#include "pal.h"

pal::string_t change_extension(const pal::string_t& filename, const pal::char_t* new_extension);
pal::string_t get_directory(const pal::string_t& path);
pal::string_t get_filename(const pal::string_t& path);
void append_path(pal::string_t& path1, const pal::char_t* path2);

#endif
