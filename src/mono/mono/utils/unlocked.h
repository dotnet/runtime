/**
 * \file
 * Contains inline functions to explicitly mark data races that should not be changed.
 * This way, instruments like Clang's ThreadSanitizer can be told to ignore very specific instructions.
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#ifndef _UNLOCKED_H_
#define _UNLOCKED_H_

#include <glib.h>
#include <mono/utils/mono-compiler.h>

#if MONO_HAS_CLANG_THREAD_SANITIZER
#define MONO_UNLOCKED_ATTRS MONO_NO_SANITIZE_THREAD MONO_NEVER_INLINE static
#else
#define MONO_UNLOCKED_ATTRS MONO_ALWAYS_INLINE static inline
#endif

MONO_UNLOCKED_ATTRS
gint32
UnlockedIncrement (gint32 *val)
{
	return ++*val;
}

MONO_UNLOCKED_ATTRS
gint64
UnlockedIncrement64 (gint64 *val)
{
	return ++*val;
}

MONO_UNLOCKED_ATTRS
gsize
UnlockedIncrementSize (gsize *val)
{
	return ++*val;
}

#endif /* _UNLOCKED_H_ */
