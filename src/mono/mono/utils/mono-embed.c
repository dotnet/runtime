/**
 * \file
 * Example code APIs to register a libraries using
 * mono_dl_fallback_register.  Real implementations should instead
 * use a binary search for implementing the dl_mapping_open and
 * dl_mapping_symbol methods here.
 *
 * Author:
 *    Mono Team (http://www.mono-project.com)
 *
 * Copyright 2001-2004 Ximian, Inc.
 * Copyright 2004-2010 Novell, Inc.
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include "config.h"
#include "mono/utils/mono-dl.h"
#include "mono/utils/mono-embed.h"

#include <stdlib.h>
#include <stdio.h>
#include <ctype.h>
#include <string.h>
#include <glib.h>

static GHashTable *mono_dls;

static void *
dl_mapping_open (const char *file, int flags, char **err, void *user_data)
{
	MonoDlMapping *mappings;
	
	if (mono_dls == NULL){
		*err = g_strdup ("Library not registered");
		return NULL;
	}
		
	mappings = (MonoDlMapping *) g_hash_table_lookup (mono_dls, file);
	*err = g_strdup (mappings == NULL ? "File not registered" : "");
	return mappings;
}

static void *
dl_mapping_symbol (void *handle, const char *symbol, char **err, void *user_data)
{
	MonoDlMapping *mappings = (MonoDlMapping *) handle;
	
	for (;mappings->name; mappings++){
		if (strcmp (symbol, mappings->name) == 0){
			*err = g_strdup ("");
			return mappings->addr;
		}
	}
	*err = g_strdup ("Symbol not found");
	return NULL;
}

/**
 * mono_dl_register_library:
 * \param name Library name, this is the name used by the DllImport as the external library name
 * \param mappings the mappings to register for P/Invoke.
 *
 * The mappings registered using this function are used as fallbacks if the dynamic linker 
 * fails, or if the platform doesn't have a dynamic linker.
 *
 * \p mappings is a pointer to the first element of an array of
 * \c MonoDlMapping values.  The list must be terminated with both 
 * the \c name and \c addr fields set to NULL.
 *
 * This is typically used like this:
 * MonoDlMapping sample_library_mappings [] = {
 *   { "CallMe", CallMe },
 *   { NULL, NULL }
 * };
 *
 * ...
 * main ()
 * {
 *    ...
 *    mono_dl_register_library ("sample", sample_library_mappings);
 *    ...
 * }
 *
 * Then the C# code can use this P/Invoke signature:
 *
 * 	[DllImport ("sample")]
 *	extern static int CallMe (int f);
 */
void
mono_dl_register_library (const char *name, MonoDlMapping *mappings)
{
	if (mono_dls == NULL){
		mono_dls = g_hash_table_new (g_str_hash, g_str_equal);
		mono_dl_fallback_register (dl_mapping_open, dl_mapping_symbol, NULL, NULL);
	}
	
	g_hash_table_insert (mono_dls, g_strdup (name), mappings);
}

