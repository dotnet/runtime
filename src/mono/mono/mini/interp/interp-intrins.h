#ifndef __MONO_MINI_INTERP_INTRINSICS_H__
#define __MONO_MINI_INTERP_INTRINSICS_H__

#include <glib.h>
#include <mono/metadata/object.h>

#include "interp-internals.h"

void
interp_intrins_marvin_block (guint32 *pp0, guint32 *pp1);

guint32
interp_intrins_ascii_chars_to_uppercase (guint32 val);

int
interp_intrins_ordinal_ignore_case_ascii (guint32 valueA, guint32 valueB);

int
interp_intrins_64ordinal_ignore_case_ascii (guint64 valueA, guint64 valueB);

MonoString*
interp_intrins_u32_to_decstr (guint32 value, MonoArray *cache, MonoVTable *vtable);

mono_u
interp_intrins_widen_ascii_to_utf16 (guint8 *pAsciiBuffer, mono_unichar2 *pUtf16Buffer, mono_u elementCount);

#endif /* __MONO_MINI_INTERP_INTRINSICS_H__ */
