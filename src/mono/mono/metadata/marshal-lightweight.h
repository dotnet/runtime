/**
 * \file
 * Copyright 2018 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#ifndef __MONO_MARSHAL_LIGHTWEIGHT_H__
#define __MONO_MARSHAL_LIGHTWEIGHT_H__
#include <glib.h>
#include <mono/utils/mono-publib.h>
MONO_API void
mono_marshal_lightweight_init (void);

MONO_API MONO_RT_EXTERNAL_ONLY void
mono_marshal_ilgen_init (void);

gboolean
mono_marshal_is_ilgen_requested (void);

#endif // __MONO_MARSHAL_LIGHTWEIGHT_H__
