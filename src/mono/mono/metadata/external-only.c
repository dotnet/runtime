/**
 * Functions that are in the (historical) embedding API
 * but must not be used by the runtime. Often
 * just a thin wrapper mono_foo => mono_foo_internal.
 *
 * Copyright 2018 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include "config.h"
#include "class-internals.h"
#include "object-internals.h"

/**
 * mono_array_length:
 * \param array a \c MonoArray*
 * \returns the total number of elements in the array. This works for
 * both vectors and multidimensional arrays.
 */
uintptr_t
mono_array_length (MonoArray *array)
{
	return mono_array_length_internal (array);
}
