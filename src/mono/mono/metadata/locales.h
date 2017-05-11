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

extern MonoBoolean ves_icall_System_Globalization_CalendarData_fill_calendar_data (MonoCalendarData *this_obj, MonoString *name, gint32 calendar_index);
extern void ves_icall_System_Globalization_CultureData_fill_culture_data (MonoCultureData *this_obj, gint32 datetime_index);
extern void ves_icall_System_Globalization_CultureData_fill_number_data (MonoNumberFormatInfo* number, gint32 number_index);
extern void ves_icall_System_Globalization_CultureInfo_construct_internal_locale (MonoCultureInfo *this_obj, MonoString *locale);
extern MonoStringHandle ves_icall_System_Globalization_CultureInfo_get_current_locale_name (MonoError *error);
extern MonoBoolean ves_icall_System_Globalization_CultureInfo_construct_internal_locale_from_lcid (MonoCultureInfo *this_obj, gint lcid);
extern MonoBoolean ves_icall_System_Globalization_CultureInfo_construct_internal_locale_from_name (MonoCultureInfo *this_obj, MonoString *name);
extern MonoArray *ves_icall_System_Globalization_CultureInfo_internal_get_cultures (MonoBoolean neutral, MonoBoolean specific, MonoBoolean installed);
extern void ves_icall_System_Globalization_CompareInfo_construct_compareinfo (MonoCompareInfo *comp, MonoString *locale);
extern int ves_icall_System_Globalization_CompareInfo_internal_compare (MonoCompareInfo *this_obj, MonoString *str1, gint32 off1, gint32 len1, MonoString *str2, gint32 off2, gint32 len2, gint32 options);
extern void ves_icall_System_Globalization_CompareInfo_free_internal_collator (MonoCompareInfo *this_obj);
extern MonoBoolean
ves_icall_System_Globalization_RegionInfo_construct_internal_region_from_lcid (MonoRegionInfo *this_obj, gint lcid);
extern MonoBoolean
ves_icall_System_Globalization_RegionInfo_construct_internal_region_from_name (MonoRegionInfo *this_obj,
 MonoString *name);
extern void ves_icall_System_Globalization_CompareInfo_assign_sortkey (MonoCompareInfo *this_obj, MonoSortKey *key, MonoString *source, gint32 options);
extern int ves_icall_System_Globalization_CompareInfo_internal_index (MonoCompareInfo *this_obj, MonoString *source, gint32 sindex, gint32 count, MonoString *value, gint32 options, MonoBoolean first);
extern int ves_icall_System_Globalization_CompareInfo_internal_index_char (MonoCompareInfo *this_obj, MonoString *source, gint32 sindex, gint32 count, gunichar2 value, gint32 options, MonoBoolean first);
extern int ves_icall_System_Threading_Thread_current_lcid (void);
extern MonoString *ves_icall_System_String_InternalToLower_Comp (MonoString *this_obj, MonoCultureInfo *cult);
extern MonoString *ves_icall_System_String_InternalToUpper_Comp (MonoString *this_obj, MonoCultureInfo *cult);
extern gunichar2 ves_icall_System_Char_InternalToUpper_Comp (gunichar2 c, MonoCultureInfo *cult);
extern gunichar2 ves_icall_System_Char_InternalToLower_Comp (gunichar2 c, MonoCultureInfo *cult);
extern void ves_icall_System_Text_Normalization_load_normalization_resource (guint8 **argProps, guint8** argMappedChars, guint8** argCharMapIndex, guint8** argHelperIndex, guint8** argMapIdxToComposite, guint8** argCombiningClass, MonoError *error);

#endif /* _MONO_METADATA_FILEIO_H_ */
