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

#endif
