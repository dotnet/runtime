// SPDX-License-Identifier: 0BSD

///////////////////////////////////////////////////////////////////////////////
//
/// \file       tuklib_mbstr_wrap.h
/// \brief      Word wrapping for multibyte strings
///
/// The word wrapping functions are intended to be usable, for example,
/// for printing --help text in command line tools. While manually-wrapped
/// --help text allows precise formatting, such freedom requires translators
/// to count spaces and determine where line breaks should occur. It's
/// tedious and error prone, and experience has shown that only some
/// translators do it well. Automatic word wrapping is less flexible but
/// results in polished-enough look with less effort from everyone.
/// Right-to-left languages and languages that don't use spaces between
/// words will still need extra effort though.
//
//  Author:     Lasse Collin
//
///////////////////////////////////////////////////////////////////////////////

#ifndef TUKLIB_MBSTR_WRAP_H
#define TUKLIB_MBSTR_WRAP_H

#include "tuklib_common.h"
#include <stdio.h>

TUKLIB_DECLS_BEGIN

/// One or more output lines exceeded right_margin.
/// This only a warning; everything was still printed successfully.
#define TUKLIB_WRAP_WARN_OVERLONG   0x01

/// Error writing to to the output FILE. The error flag in the FILE
/// should have been set as well.
#define TUKLIB_WRAP_ERR_IO          0x02

/// Invalid options in struct tuklib_wrap_opt.
/// Nothing was printed.
#define TUKLIB_WRAP_ERR_OPT         0x04

/// Invalid or unsupported multibyte character in the input string:
/// either mbrtowc() failed or wcwidth() returned a negative value.
#define TUKLIB_WRAP_ERR_STR         0x08

/// Only tuklib_wrapf(): Error in converting the format string.
/// It's either a memory allocation failure or something bad with the
/// format string or arguments.
#define TUKLIB_WRAP_ERR_FORMAT      0x10

/// Options for tuklib_wraps() and tuklib_wrapf()
struct tuklib_wrap_opt {
	/// Indentation of the first output line after `\n` or `\r`.
	/// This can be anything less than right_margin.
	unsigned short left_margin;

	/// Column where word-wrapped continuation lines start.
	/// This can be anything less than right_margin.
	unsigned short left_cont;

	/// Column where the text after `\v` will start, either on the current
	/// line (when there is room to add at least one space) or on a new
	/// empty line.
	unsigned short left2_margin;

	/// Like left_cont but for text after a `\v`. However, this must
	/// be greater than or equal to left2_margin in addition to being
	/// less than right_margin.
	unsigned short left2_cont;

	/// For 80-column terminals, it is recommended to use 79 here for
	/// maximum portability. 80 will work most of the time but it will
	/// result in unwanted empty lines in the rare case where a terminal
	/// moves the cursor to the beginning of the next line immediately
	/// when the last column has been used.
	unsigned short right_margin;
};

#define tuklib_wraps TUKLIB_SYMBOL(tuklib_wraps)
extern int tuklib_wraps(FILE *stream, const struct tuklib_wrap_opt *opt,
		const char *str);
///<
/// \brief      Word wrap a multibyte string and write it to a FILE
///
/// Word wrapping is done only at spaces and at the special control characters
/// described below. Multiple consecutive spaces are handled properly: strings
/// that have two (or more) spaces after a full sentence will look good even
/// when the spaces occur at a word wrapping boundary. Trailing spaces are
/// ignored at the end of a line or at the end of a string.
///
/// The following control characters have been repurposed:
///
///   - `\t` = Zero-width space allows a line break without producing any
///            output by itself. This can be useful after hard hyphens as
///            hyphens aren't otherwise used for line breaking. This can also
///            be useful in languages that don't use spaces between words.
///            (The Unicode character U+200B isn't supported.)
///   - `\b` = Text between a pair of `\b` characters is treated as an
///            unbreakable block (not wrapped even if there are spaces).
///            For example, a non-breaking space can be done like
///            in `"123\b \bMiB"`. Control characters (like `\n` or `\t`)
///            aren't allowed before the closing `\b`. If closing `\b` is
///            missing, the block extends to the end of the string. Empty
///            blocks are treated as zero-width characters. If line breaks
///            are possible around an empty block (like in `"foo \b\b bar"`
///            or `"foo \b"`), it can result in weird output.
///   - `\v` = Change to alternative indentation (left2_margin).
///   - `\r` = Reset back to the initial indentation and add a newline.
///            The next line will be indented by left_margin.
///   - `\n` = Add a newline without resetting the effect of `\v`. The
///            next line will be indented by left_margin or left2_margin
///            (not left_cont or left2_cont).
///
/// Only `\n` should appear in translatable strings. `\t` works too but
/// even that might confuse some translators even if there is a TRANSLATORS
/// comment explaining its meaning.
///
/// To use the other control characters in messages, one should use
/// tuklib_wrapf() with appropriate printf format string to combine
/// translatable strings with non-translatable portions. For example:
///
/// \code{.c}
/// static const struct tuklib_wrap_opt wrap2 = { 2,  2, 22, 22, 79 };
/// int e = 0;
/// ...
/// e |= tuklib_wrapf(stdout, &wrap2,
///                   "-h, --help\v%s\r"
///                   "    --version\v%s",
///                   W_("display this help and exit"),
///                   W_("display version information and exit"));
/// ...
/// if (e != 0) {
///     // Handle warning or error.
///     ...
/// }
/// \endcode
///
/// Control characters other than `\n` and `\t` are unusable in
/// translatable strings:
///
///   - Gettext tools show annoying warnings if C escape sequences other
///     than `\n` or `\t` are seen. (Otherwise they still work perfectly
///     fine though.)
///
///   - While at least Poedit and Lokalize support all escapes, some
///     editors only support `\n` and `\t`.
///
///   - They could confuse some translators, resulting in broken
///     translations.
///
/// Using non-control characters would solve some issues but it wouldn't
/// help with the unfortunate real-world issue that some translators would
/// likely have trouble understanding a new syntax. The Gettext manual
/// specifically warns about this, see the subheading "No unusual markup"
/// in `info (gettext)Preparing Strings`. (While using `\t` for zero-width
/// space is such custom markup, most translators will never need it.)
///
/// Translators can use the Unicode character U+00A0 (or U+202F) if they
/// need a non-breaking space. For example, in French a non-breaking space
/// may be needed before colons and question marks (U+00A0 is common in
/// real-world French PO files).
///
/// Using a non-ASCII char in a string in the C code (like `"123\u00A0MiB"`)
/// can work if one tells xgettext that input encoding is UTF-8, one
/// ensures that the C compiler uses UTF-8 as the input charset, and one
/// is certain that the program is *always* run under an UTF-8 locale.
/// Unfortunately a portable program cannot make this kind of assumptions,
/// which means that there is no pretty way to have a non-breaking space in
/// a translatable string.
///
/// Optional: To tell translators which strings are automatically word
/// wrapped, see the macro `W_` in tuklib_gettext.h.
///
/// \param      stream      Output FILE stream. For decent performance, it
///                         should be in buffered mode because this function
///                         writes the output one byte at a time with fputc().
/// \param      opt         Word wrapping options.
/// \param      str         Null-terminated multibyte string that is in
///                         the encoding used by the current locale.
///
/// \return     Returns 0 on success. If an error or warning occurs, one of
///             TUKLIB_WRAP_* codes is returned. Those codes are powers
///             of two. When warning/error detection can be delayed, the
///             return values can be accumulated from multiple calls using
///             bitwise-or into a single variable which can be checked after
///             all strings have (hopefully) been printed.

#define tuklib_wrapf TUKLIB_SYMBOL(tuklib_wrapf)
tuklib_attr_format_printf(3, 4)
extern int tuklib_wrapf(FILE *stream, const struct tuklib_wrap_opt *opt,
		const char *fmt, ...);
///<
/// \brief      Format and word-wrap a multibyte string and write it to a FILE
///
/// This is like tuklib_wraps() except that this takes a printf
/// format string.
///
/// \note       On platforms that lack vasprintf(), the intermediate
///             result from vsnprintf() must fit into a 128 KiB buffer.
///             TUKLIB_WRAP_ERR_FORMAT is returned if it doesn't but
///             only on platforms that lack vasprintf().

TUKLIB_DECLS_END
#endif
