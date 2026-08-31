/**
 * \file
 * Parsing for GC options.
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#ifndef __MONO_UTILS_PARSE_H__
#define __MONO_UTILS_PARSE_H__

#include <glib.h>
#include <stdlib.h>

gboolean mono_gc_parse_environment_string_extract_number (const char *str, size_t *out);

#endif
