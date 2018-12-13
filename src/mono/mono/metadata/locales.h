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

ICALL_EXPORT
MonoBoolean ves_icall_System_Globalization_CalendarData_fill_calendar_data (MonoCalendarData *this_obj, MonoString *name, gint32 calendar_index);

ICALL_EXPORT
void ves_icall_System_Globalization_CultureData_fill_culture_data (MonoCultureData *this_obj, gint32 datetime_index);

ICALL_EXPORT
void ves_icall_System_Globalization_CultureData_fill_number_data (MonoNumberFormatInfo* number, gint32 number_index);

ICALL_EXPORT
void ves_icall_System_Globalization_CultureInfo_construct_internal_locale (MonoCultureInfo *this_obj, MonoString *locale);

ICALL_EXPORT
MonoStringHandle ves_icall_System_Globalization_CultureInfo_get_current_locale_name (MonoError *error);

ICALL_EXPORT
MonoBoolean ves_icall_System_Globalization_CultureInfo_construct_internal_locale_from_lcid (MonoCultureInfo *this_obj, gint lcid);

ICALL_EXPORT
MonoBoolean ves_icall_System_Globalization_CultureInfo_construct_internal_locale_from_name (MonoCultureInfo *this_obj, MonoString *name);

ICALL_EXPORT
MonoArray *ves_icall_System_Globalization_CultureInfo_internal_get_cultures (MonoBoolean neutral, MonoBoolean specific, MonoBoolean installed);

ICALL_EXPORT
void ves_icall_System_Globalization_CompareInfo_construct_compareinfo (MonoCompareInfo *comp, MonoString *locale);

ICALL_EXPORT
int ves_icall_System_Globalization_CompareInfo_internal_compare (MonoCompareInfo *this_obj, MonoString *str1, gint32 off1, gint32 len1, MonoString *str2, gint32 off2, gint32 len2, gint32 options);

ICALL_EXPORT
void ves_icall_System_Globalization_CompareInfo_free_internal_collator (MonoCompareInfo *this_obj);

ICALL_EXPORT
MonoBoolean
ves_icall_System_Globalization_RegionInfo_construct_internal_region_from_name (MonoRegionInfo *this_obj,
 MonoString *name);

ICALL_EXPORT
int ves_icall_System_Globalization_CompareInfo_internal_index (MonoCompareInfo *this_obj, MonoString *source, gint32 sindex, gint32 count, MonoString *value, gint32 options, MonoBoolean first);

ICALL_EXPORT
int
ves_icall_System_Threading_Thread_current_lcid (void);

ICALL_EXPORT
MonoString *ves_icall_System_String_InternalToLower_Comp (MonoString *this_obj, MonoCultureInfo *cult);

ICALL_EXPORT
MonoString *ves_icall_System_String_InternalToUpper_Comp (MonoString *this_obj, MonoCultureInfo *cult);

ICALL_EXPORT
gunichar2 ves_icall_System_Char_InternalToUpper_Comp (gunichar2 c, MonoCultureInfo *cult);

ICALL_EXPORT
gunichar2 ves_icall_System_Char_InternalToLower_Comp (gunichar2 c, MonoCultureInfo *cult);

ICALL_EXPORT
void ves_icall_System_Text_Normalization_load_normalization_resource (guint8 **argProps, guint8** argMappedChars, guint8** argCharMapIndex, guint8** argHelperIndex, guint8** argMapIdxToComposite, guint8** argCombiningClass, MonoError *error);

#endif /* _MONO_METADATA_FILEIO_H_ */
