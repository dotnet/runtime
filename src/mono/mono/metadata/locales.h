/**
 * \file
 * Culture-sensitive handling
 *
 * Authors:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2003 Ximian, Inc.
 */

#ifndef _MONO_METADATA_LOCALES_H_
#define _MONO_METADATA_LOCALES_H_

#include <glib.h>

#include <mono/metadata/object-internals.h>
#include <mono/metadata/icalls.h>

#if !ENABLE_NETCORE

/* This is a copy of System.Globalization.CompareOptions */
typedef enum {
	CompareOptions_None=0x00,
	CompareOptions_IgnoreCase=0x01,
	CompareOptions_IgnoreNonSpace=0x02,
	CompareOptions_IgnoreSymbols=0x04,
	CompareOptions_IgnoreKanaType=0x08,
	CompareOptions_IgnoreWidth=0x10,
	CompareOptions_StringSort=0x20000000,
	CompareOptions_Ordinal=0x40000000
} MonoCompareOptions;

typedef struct NumberFormatEntryManaged NumberFormatEntryManaged;

ICALL_EXPORT
gconstpointer
ves_icall_System_Globalization_CultureData_fill_number_data (gint32 number_index, NumberFormatEntryManaged *managed);

ICALL_EXPORT
void ves_icall_System_Globalization_CultureInfo_construct_internal_locale (MonoCultureInfo *this_obj, MonoString *locale);

ICALL_EXPORT
void ves_icall_System_Globalization_CompareInfo_construct_compareinfo (MonoCompareInfo *comp, MonoString *locale);

ICALL_EXPORT gint32
ves_icall_System_Globalization_CompareInfo_internal_compare (const gunichar2 *str1, gint32 len1,
	const gunichar2 *str2, gint32 len2, gint32 options);

ICALL_EXPORT
void ves_icall_System_Globalization_CompareInfo_free_internal_collator (MonoCompareInfo *this_obj);

ICALL_EXPORT gint32
ves_icall_System_Globalization_CompareInfo_internal_index (const gunichar2 *source, gint32 sindex,
	gint32 count, const gunichar2 *value, gint32 value_length, MonoBoolean first);

#endif /* !ENABLE_NETCORE */

#define MONO_LOCALE_INVARIANT (0x007F)

#endif /* _MONO_METADATA_FILEIO_H_ */
