/*
 * unicode.h: Unicode support
 *
 * Author:
 *	Dietmar Maurer (dietmar@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */

#ifndef _MONO_METADATA_UNICODE_H_
#define _MONO_METADATA_UNICODE_H_

#include <config.h>
#include <glib.h>

#include <mono/metadata/object.h>

typedef enum {
	UppercaseLetter         =  0,
	LowercaseLetter         =  1,
	TitlecaseLetter         =  2,
	ModifierLetter          =  3,
	OtherLetter             =  4,
	NonSpacingMark          =  5,
	SpaceCombiningMark      =  6,
	EnclosingMark           =  7,
	DecimalDigitNumber      =  8,
	LetterNumber            =  9,
	OtherNumber             = 10,
	SpaceSeperator          = 11,
	LineSeperator           = 12,
	ParagraphSeperator      = 13,
	Control                 = 14,
	Format                  = 15,
	Surrogate               = 16,
	PrivateUse              = 17,
	ConnectorPunctuation    = 18,
	DashPunctuation         = 19,
	OpenPunctuation         = 20,
	ClosePunctuation        = 21,
	InitialQuotePunctuation = 22,
	FinalQuotePunctuation   = 23,
	OtherPunctuation        = 24,
	MathSymbol              = 25,
	CurrencySymbol          = 26,
	ModifierSymbol          = 27,
	OtherSymbol             = 28,
	OtherNotAssigned        = 29,
} MonoUnicodeCategory;

double 
ves_icall_System_Char_GetNumericValue    (gunichar2 c);

MonoUnicodeCategory 
ves_icall_System_Char_GetUnicodeCategory (gunichar2 c);

gboolean 
ves_icall_System_Char_IsControl          (gunichar2 c);

gboolean 
ves_icall_System_Char_IsDigit            (gunichar2 c);

gboolean 
ves_icall_System_Char_IsLetter           (gunichar2 c);

gboolean 
ves_icall_System_Char_IsLower            (gunichar2 c);

gboolean 
ves_icall_System_Char_IsUpper            (gunichar2 c);

gboolean 
ves_icall_System_Char_IsNumber           (gunichar2 c);

gboolean 
ves_icall_System_Char_IsPunctuation      (gunichar2 c);

gboolean 
ves_icall_System_Char_IsSeparator        (gunichar2 c);

gboolean 
ves_icall_System_Char_IsSurrogate        (gunichar2 c);

gboolean 
ves_icall_System_Char_IsSymbol           (gunichar2 c);

gboolean 
ves_icall_System_Char_IsWhiteSpace       (gunichar2 c);

gunichar2
ves_icall_System_Char_ToLower            (gunichar2 c);

gunichar2
ves_icall_System_Char_ToUpper            (gunichar2 c);

#endif
