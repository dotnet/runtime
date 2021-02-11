/**
 * \file
 * Contains inline functions to explicitly mark data races that should not be changed.
 * This way, instruments like Clang's ThreadSanitizer can be told to ignore very specific instructions.
 *
 * Please keep this file and its methods organised:
 *  * Increment, Decrement, Add, Subtract, Write, Read
 *  * gint32 (""), guint32 ("Unsigned"),
 *      gint64 ("64"), guint64 ("Unsigned64"),
 *      gsize ("Size"), gboolean ("Bool"),
 *      gpointer ("Pointer")
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#ifndef _UNLOCKED_H_
#define _UNLOCKED_H_

#include <glib.h>
#include <mono/utils/mono-compiler.h>

#if MONO_HAS_CLANG_THREAD_SANITIZER
#define MONO_UNLOCKED_ATTRS MONO_NO_SANITIZE_THREAD MONO_NEVER_INLINE static
#elif defined(_MSC_VER)
#define MONO_UNLOCKED_ATTRS MONO_ALWAYS_INLINE static
#else
#define MONO_UNLOCKED_ATTRS MONO_ALWAYS_INLINE static inline
#endif

MONO_UNLOCKED_ATTRS
gint32
UnlockedIncrement (volatile gint32 *val)
{
	return ++*val;
}

MONO_UNLOCKED_ATTRS
gint64
UnlockedIncrement64 (volatile gint64 *val)
{
	return ++*val;
}

MONO_UNLOCKED_ATTRS
gint64
UnlockedDecrement64 (volatile gint64 *val)
{
	return --*val;
}

MONO_UNLOCKED_ATTRS
gint32
UnlockedDecrement (volatile gint32 *val)
{
	return --*val;
}

MONO_UNLOCKED_ATTRS
gint32
UnlockedAdd (volatile gint32 *dest, gint32 add)
{
	return *dest += add;
}

MONO_UNLOCKED_ATTRS
gint64
UnlockedAdd64 (volatile gint64 *dest, gint64 add)
{
	return *dest += add;
}

MONO_UNLOCKED_ATTRS
gdouble
UnlockedAddDouble (volatile gdouble *dest, gdouble add)
{
	return *dest += add;
}

MONO_UNLOCKED_ATTRS
gint64
UnlockedSubtract64 (volatile gint64 *dest, gint64 sub)
{
	return *dest -= sub;
}

MONO_UNLOCKED_ATTRS
void
UnlockedWrite (volatile gint32 *dest, gint32 val)
{
	*dest = val;
}

MONO_UNLOCKED_ATTRS
void
UnlockedWrite64 (volatile gint64 *dest, gint64 val)
{
	*dest = val;
}

MONO_UNLOCKED_ATTRS
void
UnlockedWriteBool (volatile gboolean *dest, gboolean val)
{
	*dest = val;
}

MONO_UNLOCKED_ATTRS
void
UnlockedWritePointer (volatile gpointer *dest, gpointer val)
{
	*dest = val;
}

MONO_UNLOCKED_ATTRS
gint32
UnlockedRead (volatile gint32 *src)
{
	return *src;
}

MONO_UNLOCKED_ATTRS
gint64
UnlockedRead64 (volatile gint64 *src)
{
	return *src;
}

MONO_UNLOCKED_ATTRS
gboolean
UnlockedReadBool (volatile gboolean *src)
{
	return *src;
}

MONO_UNLOCKED_ATTRS
gpointer
UnlockedReadPointer (volatile gpointer *src)
{
	return *src;
}

#endif /* _UNLOCKED_H_ */
