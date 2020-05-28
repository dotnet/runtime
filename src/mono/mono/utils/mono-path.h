/**
 * \file
 */

#ifndef __MONO_PATH_H
#define __MONO_PATH_H

#include <glib.h>
#include <mono/utils/mono-publib.h>

MONO_API gchar *mono_path_resolve_symlinks (const char *path);
MONO_API gchar *mono_path_canonicalize (const char *path);
gboolean mono_path_filename_in_basedir (const char *filename, const char *basedir);

#endif /* __MONO_PATH_H */

