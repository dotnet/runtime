/**
 * \file
 * toggleref support for sgen
 *
 * Copyright 2011 Xamarin, Inc.
 *
 * Author:
 *  Rodrigo Kumpera (kumpera@gmail.com)
 * 
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#ifndef _MONO_SGEN_TOGGLEREF_H_
#define _MONO_SGEN_TOGGLEREF_H_

#include <mono/utils/mono-publib.h>

/* GC toggle ref support */

typedef enum {
	MONO_TOGGLE_REF_DROP,
	MONO_TOGGLE_REF_STRONG,
	MONO_TOGGLE_REF_WEAK
} MonoToggleRefStatus;

MONO_API void mono_gc_toggleref_register_callback (MonoToggleRefStatus (*proccess_toggleref) (MonoObject *obj));
MONO_API MONO_RT_EXTERNAL_ONLY void mono_gc_toggleref_add (MonoObject *object, mono_bool strong_ref);

#endif
