#ifndef __MONO_PATH_H
#define __MONO_PATH_H

#include <glib.h>

gchar *mono_path_resolve_symlinks (const char *path);
gchar *mono_path_canonicalize (const char *path);

#endif /* __MONO_PATH_H */

