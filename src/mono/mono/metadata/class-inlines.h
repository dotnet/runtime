/*
 * Copyright 2016 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#ifndef __MONO_METADATA_CLASS_INLINES_H__
#define __MONO_METADATA_CLASS_INLINES_H__

#include <mono/metadata/class-internals.h>

static inline gboolean
mono_class_is_def (MonoClass *class)
{
	return class->class_kind == MONO_CLASS_DEF;
}

static inline gboolean
mono_class_is_gtd (MonoClass *class)
{
	return class->class_kind == MONO_CLASS_GTD;
}

static inline gboolean
mono_class_is_ginst (MonoClass *class)
{
	return class->class_kind == MONO_CLASS_GINST;
}

static inline gboolean
mono_class_is_gparam (MonoClass *class)
{
	return class->class_kind == MONO_CLASS_GPARAM;
}

static inline gboolean
mono_class_is_array (MonoClass *class)
{
	return class->class_kind == MONO_CLASS_ARRAY;
}

static inline gboolean
mono_class_is_pointer (MonoClass *class)
{
	return class->class_kind == MONO_CLASS_POINTER;
}


#endif