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
};

#endif
