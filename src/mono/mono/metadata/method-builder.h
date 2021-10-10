/**
 * \file
 * Functions for creating IL methods at runtime.
 * 
 * Author:
 *   Paolo Molaro (lupus@ximian.com)
 *
 * (C) 2002 Ximian, Inc.  http://www.ximian.com
 *
 */

#ifndef __MONO_METHOD_BUILDER_H__
#define __MONO_METHOD_BUILDER_H__

#include "config.h"
#include <mono/metadata/class.h>
#include <mono/metadata/object-internals.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/opcodes.h>
#include <mono/metadata/reflection.h>
#include <mono/utils/mono-compiler.h>

typedef struct _MonoMethodBuilder MonoMethodBuilder;

#define MONO_METHOD_BUILDER_CALLBACKS_VERSION 1

typedef struct {
	int version;
	MonoMethodBuilder* (*new_base) (MonoClass *klass, MonoWrapperType type);
	void (*free) (MonoMethodBuilder *mb);
	MonoMethod* (*create_method) (MonoMethodBuilder *mb, MonoMethodSignature *signature, int max_stack);
} MonoMethodBuilderCallbacks;

MONO_COMPONENT_API MonoMethodBuilder *
mono_mb_new (MonoClass *klass, const char *name, MonoWrapperType type);

MonoMethodBuilder *
mono_mb_new_no_dup_name (MonoClass *klass, const char *name, MonoWrapperType type);

MONO_COMPONENT_API void
mono_mb_free (MonoMethodBuilder *mb);

MONO_COMPONENT_API MonoMethod *
mono_mb_create_method (MonoMethodBuilder *mb, MonoMethodSignature *signature, int max_stack);

guint32
mono_mb_add_data (MonoMethodBuilder *mb, gpointer data);

char*
mono_mb_strdup (MonoMethodBuilder *mb, const char *s);

void
mono_install_method_builder_callbacks (MonoMethodBuilderCallbacks *cb);

#endif /* __MONO_METHOD_BUILDER_H__ */
