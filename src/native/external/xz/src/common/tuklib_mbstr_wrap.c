// SPDX-License-Identifier: 0BSD

///////////////////////////////////////////////////////////////////////////////
//
/// \file       tuklib_mbstr_wrap.c
/// \brief      Word wraps a string and prints it to a FILE stream
///
/// This depends on tuklib_mbstr_width.c.
//
//  Author:     Lasse Collin
//
///////////////////////////////////////////////////////////////////////////////

#include "tuklib_mbstr.h"
#include "tuklib_mbstr_wrap.h"
#include <stdarg.h>
#include <stdlib.h>
#include <stdio.h>
#include <string.h>


extern int
tuklib_wraps(FILE *outfile, const struct tuklib_wrap_opt *opt, const char *str)
{
	// left_cont may be less than left_margin. In that case, if the first
	// word is extremely long, it will stay on the first line even if
	// the line then gets overlong.
	//
	// On the other hand, left2_cont < left2_margin isn't allowed because
	// it could result in inconsistent behavior when a very long word
	// comes right after a \v.
	//
	// It is fine to have left2_margin < left_margin although it would be
	// an odd use case.
	if (!(opt->left_margin < opt->right_margin
			&& opt->left_cont < opt->right_margin
			&& opt->left2_margin <= opt->left2_cont
			&& opt->left2_cont < opt->right_margin))
		return TUKLIB_WRAP_ERR_OPT;

	// This is set to TUKLIB_WRAP_WARN_OVERLONG if one or more
	// output lines extend past opt->right_margin columns.
	int warn_overlong = 0;

	// Indentation of the first output line after \n or \r.
	// \v sets this to opt->left2_margin.
	// \r resets this back to the original value.
	size_t first_indent = opt->left_margin;

	// Indentation of the output lines that occur due to word wrapping.
	// \v sets this to opt->left2_cont and \r back to the original value.
	size_t cont_indent = opt->left_cont;

	// If word wrapping occurs, the newline isn't printed unless more
	// text would be put on the continuation line. This is also used
	// when \v needs to start on a new line.
	bool pending_newline = false;

	// Spaces are printed only when there is something else to put
	// after the spaces on the line. This avoids unwanted empty lines
	// in the output and makes it possible to ignore possible spaces
	// before a \v character.
	size_t pending_spaces = first_indent;

	// Current output column. When cur_col == pending_spaces, nothing
	// has been actually printed to the current output line.
	size_t cur_col = pending_spaces;

	while (true) {
		// Number of bytes until the *next* line-break opportunity.
		size_t len = 0;

		// Number of columns until the *next* line-break opportunity.
		size_t width = 0;

		// Text between a pair of \b characters is treated as
		// an unbreakable block even if it contains spaces.
		// It must not contain any control characters before
		// the closing \b.
		bool unbreakable = false;

		while (true) {
			// Find the next character that we handle specially.
			// In an unbreakable block, search only for the
			// closing \b; if missing, the unbreakable block
			// extends to the end of the string.
			const size_t n = strcspn(str + len,
					unbreakable ? "\b" : " \t\n\r\v\b");

			// Calculate how many columns the characters need.
			const size_t w = tuklib_mbstr_width_mem(str + len, n);
			if (w == (size_t)-1)
				return TUKLIB_WRAP_ERR_STR;

			width += w;
			len += n;

			// \b isn't a line-break opportunity so it has to
			// be handled here. For simplicity, empty blocks
			// are treated as zero-width characters.
			if (str[len] == '\b') {
				++len;
				unbreakable = !unbreakable;
				continue;
			}

			break;
		}

		// Determine if adding this chunk of text would make the
		// current output line exceed opt->right_margin columns.
		const bool too_long = cur_col + width > opt->right_margin;

		// Wrap the line if needed. However:
		//
		//   - Don't wrap if the current column is less than where
		//     the continuation line would begin. In that case
		//     the chunk wouldn't fit on the next line either so
		//     we just have to produce an overlong line.
		//
		//   - Don't wrap if so far the line only contains spaces.
		//     Wrapping in that case would leave a weird empty line.
		//     NOTE: This "only contains spaces" condition is the
		//     reason why left2_margin > left2_cont isn't allowed.
		if (too_long && cur_col > cont_indent
				&& cur_col > pending_spaces) {
			// There might be trailing spaces or zero-width spaces
			// which need to be ignored to keep the output pretty.
			//
			// Spaces need to be ignored because in some
			// writing styles there are two spaces after
			// a full stop. Example string:
			//
			//     "Foo bar.  Abc def."
			//              ^
			// If the first space after the first full stop
			// triggers word wrapping, both spaces must be
			// ignored. Otherwise the next line would be
			// indented too much.
			//
			// Zero-width spaces are ignored the same way
			// because they are meaningless if an adjacent
			// character is a space.
			while (*str == ' ' || *str == '\t')
				++str;

			// Don't print the newline here; only mark it as
			// pending. This avoids an unwanted empty line if
			// there is a \n or \r or \0 after the spaces have
			// been ignored.
			pending_newline = true;
			pending_spaces = cont_indent;
			cur_col = pending_spaces;

			// Since str may have been incremented due to the
			// ignored spaces, the loop needs to be restarted.
			continue;
		}

		// Print the current chunk of text before the next
		// line-break opportunity. If the chunk was empty,
		// don't print anything so that the pending newline
		// and pending spaces aren't printed on their own.
		if (len > 0) {
			if (pending_newline) {
				pending_newline = false;
				if (putc('\n', outfile) == EOF)
					return TUKLIB_WRAP_ERR_IO;
			}

			while (pending_spaces > 0) {
				if (putc(' ', outfile) == EOF)
					return TUKLIB_WRAP_ERR_IO;

				--pending_spaces;
			}

			for (size_t i = 0; i < len; ++i) {
				// Ignore unbreakable block characters (\b).
				const int c = (unsigned char)str[i];
				if (c != '\b' && putc(c, outfile) == EOF)
					return TUKLIB_WRAP_ERR_IO;
			}

			str += len;
			cur_col += width;

			// Remember if the line got overlong. If no other
			// errors occur, we return warn_overlong. It might
			// help in catching problematic strings.
			if (too_long)
				warn_overlong = TUKLIB_WRAP_WARN_OVERLONG;
		}

		// Handle the special character after the chunk of text.
		switch (*str) {
		case ' ':
			// Regular space.
			++cur_col;
			++pending_spaces;
			break;

		case '\v':
			// Set the alternative indentation settings.
			first_indent = opt->left2_margin;
			cont_indent = opt->left2_cont;

			if (first_indent > cur_col) {
				// Add one or more spaces to reach
				// the column specified in first_indent.
				pending_spaces += first_indent - cur_col;
			} else {
				// There is no room to add even one space
				// before reaching the column first_indent.
				pending_newline = true;
				pending_spaces = first_indent;
			}

			cur_col = first_indent;
			break;

		case '\0': // Implicit newline at the end of the string.
		case '\r': // Newline that also resets the effect of \v.
		case '\n': // Newline without resetting the indentation mode.
			if (putc('\n', outfile) == EOF)
				return TUKLIB_WRAP_ERR_IO;

			if (*str == '\0')
				return warn_overlong;

			if (*str == '\r') {
				first_indent = opt->left_margin;
				cont_indent = opt->left_cont;
			}

			pending_newline = false;
			pending_spaces = first_indent;
			cur_col = first_indent;
			break;
		}

		// Skip the specially-handled character.
		++str;
	}
}


extern int
tuklib_wrapf(FILE *stream, const struct tuklib_wrap_opt *opt,
		const char *fmt, ...)
{
	va_list ap;
	char *buf;

#ifdef HAVE_VASPRINTF
	va_start(ap, fmt);

#ifdef __clang__
#	pragma GCC diagnostic push
#	pragma GCC diagnostic ignored "-Wformat-nonliteral"
#endif
	const int n = vasprintf(&buf, fmt, ap);
#ifdef __clang__
#	pragma GCC diagnostic pop
#endif

	va_end(ap);
	if (n == -1)
		return TUKLIB_WRAP_ERR_FORMAT;
#else
	// Fixed buffer size is dumb but in practice one shouldn't need
	// huge strings for *formatted* output. This simple method is safe
	// with pre-C99 vsnprintf() implementations too which don't return
	// the required buffer size (they return -1 or buf_size - 1) or
	// which might not null-terminate the buffer in case it's too small.
	const size_t buf_size = 128 * 1024;
	buf = malloc(buf_size);
	if (buf == NULL)
		return TUKLIB_WRAP_ERR_FORMAT;

	va_start(ap, fmt);
	const int n = vsnprintf(buf, buf_size, fmt, ap);
	va_end(ap);

	if (n <= 0 || n >= (int)(buf_size - 1)) {
		free(buf);
		return TUKLIB_WRAP_ERR_FORMAT;
	}
#endif

	const int ret = tuklib_wraps(stream, opt, buf);
	free(buf);
	return ret;
}
