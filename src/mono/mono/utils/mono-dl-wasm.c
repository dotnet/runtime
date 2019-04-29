#include <config.h>

#if defined (HOST_WASM)

#include "mono/utils/mono-dl.h"
#include "mono/utils/mono-embed.h"
#include "mono/utils/mono-path.h"

#include <stdlib.h>
#include <stdio.h>
#include <ctype.h>
#include <string.h>
#include <glib.h>
#include <dlfcn.h>

const char *
mono_dl_get_so_prefix (void)
{
	return "";
}

const char **
mono_dl_get_so_suffixes (void)
{
	static const char *suffixes[] = {
		".wasm", //we only recognize .wasm files for DSOs.
		"",
	};
	return suffixes;
}

int
mono_dl_get_executable_path (char *buf, int buflen)
{
	strncpy (buf, "/managed", buflen); //This is a packaging convertion that our tooling should enforce
	return 0;
}

const char*
mono_dl_get_system_dir (void)
{
	return NULL;
}


void*
mono_dl_lookup_symbol (MonoDl *module, const char *name)
{
	return dlsym(module->handle, name);
}

char*
mono_dl_current_error_string (void)
{
	return g_strdup ("");
}


int
mono_dl_convert_flags (int flags)
{
	int lflags = flags & MONO_DL_LOCAL ? 0 : RTLD_GLOBAL;

	if (flags & MONO_DL_LAZY)
		lflags |= RTLD_LAZY;
	else
		lflags |= RTLD_NOW;
	return lflags;
}

void *
mono_dl_open_file (const char *file, int flags)
{
	// issue https://github.com/emscripten-core/emscripten/issues/8511
	if (strstr(file, "System.Native")) {
		return NULL;
	}

	return dlopen(file, flags);
}

void
mono_dl_close_handle (MonoDl *module)
{
	dlclose(module->handle);
}

#endif
