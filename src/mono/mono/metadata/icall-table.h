/**
 * \file
 * Copyright 2016 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#ifndef __MONO_METADATA_ICALL_TABLE_H__
#define __MONO_METADATA_ICALL_TABLE_H__

#include <config.h>
#include <glib.h>
#include <mono/utils/mono-publib.h>

#define MONO_ICALL_TABLE_CALLBACKS_VERSION 1

typedef struct {
	int version;
	gpointer (*lookup) (char *classname, char *methodname, char *sigstart, gboolean *uses_handles);
	const char* (*lookup_icall_symbol) (gpointer func);
} MonoIcallTableCallbacks;

void
mono_install_icall_table_callbacks (MonoIcallTableCallbacks *cb);

MONO_API void
mono_icall_table_init (void);

#endif
