/**
 * \file
 * Copyright 2018 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#ifndef __MONO_METHOD_BUILDER_INTERNALS_H__
#define __MONO_METHOD_BUILDER_INTERNALS_H__

#include "config.h"
#include <mono/metadata/class.h>
#include <mono/metadata/object-internals.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/opcodes.h>
#include <mono/metadata/reflection.h>
#include <mono/metadata/method-builder.h>

/* noilgen version */
struct _MonoMethodBuilder {
	MonoMethod *method;
	gchar *name;
	gboolean no_dup_name;
	MonoMemoryManager *mem_manager;
};

#endif
