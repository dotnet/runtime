/**
 * \file
 * Copyright 2018 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#ifndef __MONO_METHOD_BUILDER_ILGEN_INTERNALS_H__
#define __MONO_METHOD_BUILDER_ILGEN_INTERNALS_H__

#include "config.h"
#include <mono/metadata/class.h>
#include <mono/metadata/object-internals.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/opcodes.h>
#include <mono/metadata/reflection.h>
#include <mono/metadata/method-builder.h>

/* ilgen version */
struct _MonoMethodBuilder {
	MonoMethod *method;
	gchar *name;
	gboolean no_dup_name;
	MonoMemoryManager *mem_manager;
	GList *locals_list;
	gint locals;
	gboolean dynamic;
	gboolean skip_visibility;
	gboolean init_locals;
	guint32 code_size;
	guint32 pos;
	guchar *code;
	gint num_clauses;
	MonoExceptionClause *clauses;
	const gchar **param_names;
	MonoBitSet *volatile_args;
	MonoBitSet *volatile_locals;
	gboolean inflate_wrapper_data;
	GList *wrapper_data_inflate_info;
};

typedef struct MonoMethodBuilderInflateWrapperData
{
    uint16_t idx;
    uint16_t kind; // MonoILGenWrapperDataKind
} MonoMethodBuilderInflateWrapperData;

enum MonoILGenWrapperDataKind
{
	MONO_MB_ILGEN_WRAPPER_DATA_NONE = 0,
	MONO_MB_ILGEN_WRAPPER_DATA_FIELD,
	MONO_MB_ILGEN_WRAPPER_DATA_METHOD,
};

// index in MonoMethodWrapper:wrapper_data where the inflate info will be stored.
// note: idx 1 is the wrapper info (see mono_marshal_get_wrapper_info)
#define MONO_MB_ILGEN_INFLATE_WRAPPER_INFO_IDX 2

#endif
