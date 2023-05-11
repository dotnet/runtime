#include <config.h>
#include <mono/utils/mono-compiler.h>
#include <mono/eglib/glib.h>

#if defined (HOST_WASM)

#include "mono/utils/mono-dl.h"
#include "mono/utils/mono-embed.h"
#include "mono/utils/mono-path.h"

#include <stdlib.h>
#include <stdio.h>
#include <ctype.h>
#include <string.h>
#include <glib.h>

#ifndef HOST_WASI
#include <dlfcn.h>
#endif

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

const char*
mono_dl_get_system_dir (void)
{
	return NULL;
}


void*
mono_dl_lookup_symbol (MonoDl *module, const char *name)
{
	return NULL;
}

char*
mono_dl_current_error_string (void)
{
	return g_strdup ("");
}

// Copied from mono-dl-posix.c
int
mono_dl_convert_flags (int mono_flags, int native_flags)
{
	int lflags = native_flags;

#ifndef HOST_WASI // On WASI, these flags are undefined and not required

	// Specifying both will default to LOCAL
	if (mono_flags & MONO_DL_GLOBAL && !(mono_flags & MONO_DL_LOCAL))
		lflags |= RTLD_GLOBAL;
	else
		lflags |= RTLD_LOCAL;

	if (mono_flags & MONO_DL_LAZY)
		lflags |= RTLD_LAZY;
	else
		lflags |= RTLD_NOW;

#endif

	return lflags;
}

void *
mono_dl_open_file (const char *file, int flags, MonoError *error)
{
	// Actual dlopen is done in driver.c:wasm_dl_load()
	return NULL;
}

void
mono_dl_close_handle (MonoDl *module, MonoError *error)
{
}

#else

MONO_EMPTY_SOURCE_FILE (mono_dl_wasm);

#endif

#if defined (HOST_WASM)

static GHashTable *name_to_blob = NULL;

typedef struct {
	const unsigned char *data;
	unsigned int size;
} FileBlob;

int
mono_wasm_add_bundled_file (const char *name, const unsigned char *data, unsigned int size)
{
	// printf("mono_wasm_add_bundled_file: %s %p %d\n", name, data, size);
	if(name_to_blob == NULL)
	{
		name_to_blob = g_hash_table_new (g_str_hash, g_str_equal);
	}
	FileBlob *blob = g_new0 (FileBlob, 1);
	blob->data = data;
	blob->size = size;
	g_hash_table_insert (name_to_blob, (gpointer) name, blob);
	return 0;
}

const unsigned char*
mono_wasm_get_bundled_file (const char *name, int* out_length)
{
	FileBlob *blob = (FileBlob *)g_hash_table_lookup (name_to_blob, name);
	if (blob != NULL)
	{
		// printf("mono_wasm_get_bundled_file: %s %p %d \n", name, blob->data, blob->size);
		*out_length = blob->size;
		return blob->data;
	}

	// printf("mono_wasm_get_bundled_file: %s not found \n", name);
	*out_length = 0;
	return NULL;
}

#endif /* HOST_WASM */
