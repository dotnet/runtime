/*
 * unicode.h: Unicode support
 *
 * Author:
 *	Dietmar Maurer (dietmar@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>
#include <errno.h>

#include <mono/metadata/object.h>
#include <mono/metadata/unicode.h>
#include <mono/metadata/exception.h>

static const MonoUnicodeCategory catmap[] = {
	/* G_UNICODE_CONTROL = */              Control,
	/* G_UNICODE_FORMAT = */               Format,
	/* G_UNICODE_UNASSIGNED = */           OtherNotAssigned,
	/* G_UNICODE_PRIVATE_USE = */          PrivateUse,
	/* G_UNICODE_SURROGATE = */            Surrogate,
	/* G_UNICODE_LOWERCASE_LETTER = */     LowercaseLetter,
	/* G_UNICODE_MODIFIER_LETTER = */      ModifierLetter,
	/* G_UNICODE_OTHER_LETTER = */         OtherLetter,
	/* G_UNICODE_TITLECASE_LETTER = */     TitlecaseLetter,
	/* G_UNICODE_UPPERCASE_LETTER = */     UppercaseLetter,
	/* G_UNICODE_COMBINING_MARK = */       SpaceCombiningMark,
	/* G_UNICODE_ENCLOSING_MARK = */       EnclosingMark,
	/* G_UNICODE_NON_SPACING_MARK = */     NonSpacingMark,
	/* G_UNICODE_DECIMAL_NUMBER = */       DecimalDigitNumber,
	/* G_UNICODE_LETTER_NUMBER = */        LetterNumber,
	/* G_UNICODE_OTHER_NUMBER = */         OtherNumber,
	/* G_UNICODE_CONNECT_PUNCTUATION = */  ConnectorPunctuation,
	/* G_UNICODE_DASH_PUNCTUATION = */     DashPunctuation,
	/* G_UNICODE_CLOSE_PUNCTUATION = */    ClosePunctuation,
	/* G_UNICODE_FINAL_PUNCTUATION = */    FinalQuotePunctuation,
	/* G_UNICODE_INITIAL_PUNCTUATION = */  InitialQuotePunctuation,
	/* G_UNICODE_OTHER_PUNCTUATION = */    OtherPunctuation,
	/* G_UNICODE_OPEN_PUNCTUATION = */     OpenPunctuation,
	/* G_UNICODE_CURRENCY_SYMBOL = */      CurrencySymbol,
	/* G_UNICODE_MODIFIER_SYMBOL = */      ModifierSymbol,
	/* G_UNICODE_MATH_SYMBOL = */          MathSymbol,
	/* G_UNICODE_OTHER_SYMBOL = */         OtherSymbol,
	/* G_UNICODE_LINE_SEPARATOR = */       LineSeperator,
	/* G_UNICODE_PARAGRAPH_SEPARATOR = */  ParagraphSeperator,
	/* G_UNICODE_SPACE_SEPARATOR = */      SpaceSeperator,
};

