/**
 * \file
 * Copyright 2016 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#ifndef __MONO_METADATA_MARSHAL_INTERNALS_H__
#define __MONO_METADATA_MARSHAL_INTERNALS_H__

#include <config.h>
#include <glib.h>
#include <mono/metadata/object-internals.h>
#include <mono/metadata/marshal.h>

typedef enum {
	TYPECHECK_OBJECT_ARG_POS = 0,
	TYPECHECK_CLASS_ARG_POS = 1,
	TYPECHECK_CACHE_ARG_POS = 2
} MarshalTypeCheckPositions;

void
mono_marshal_noilgen_init (void);

void
mono_marshal_noilgen_init_blittable (MonoMarshalCallbacks *cb);

#endif /* __MONO_METADATA_MARSHAL_INTERNALS_H__ */
