/**
 * \file
 * Assorted utilities for the disassembler
 *
 * Author:
 *   Miguel de Icaza (miguel@ximian.com)
 *
 * (C) 2001 Ximian, Inc (http://www.ximian.com)
 */
#include <config.h>
#include <glib.h>
#include <string.h>
#include <stdio.h>
#include <math.h>
#include "util.h"
#include "mono/utils/mono-compiler.h"

#ifdef HAVE_IEEEFP_H
#include <ieeefp.h>
#endif

/**
 * \param code code to lookup in table
 * \param table table to decode code
 *
 * Warning: returns static buffer.
 */
const char *
map (guint32 code, dis_map_t *table)
{
	int i;

	for (i = 0; table [i].str != NULL; i++)
		if (table [i].code == code)
			return table [i].str;
	return "invalid-flags";
}

/**
 * \param code bitfield
 * \param table table to decode bitfield
 *
 * Warning: returns static buffer.
 */
const char *
flags (guint32 code, dis_map_t *table)
{
	static char buffer [1024];
	int i;
	
	buffer [0] = 0;
	
	for (i = 0; code && table [i].str != NULL; i++)
		if (table [i].code & code) {
			code &= ~table [i].code;
			strcat (buffer, table [i].str);
		}

	if (code)
		sprintf (buffer + strlen (buffer), "unknown-flag-%2x ", code);

	return buffer;
}

/**
 * hex_dump:
 * @buffer: pointer to buffer to dump
 * @base: numbering base to use
 * @count: number of bytes to dump
 */
void
hex_dump (const char *buffer, int base, int count)
{
	int show_header = 1;
	int i;

	if (count < 0){
		count = -count;
		show_header = 0;
	}
	
	for (i = 0; i < count; i++){
		if (show_header)
			if ((i % 16) == 0)
				printf ("\n0x%08X: ", (unsigned char) base + i);

		printf ("%02X ", (unsigned char) (buffer [i]));
	}
	fflush (stdout);
}

char*
data_dump (const char *data, int len, const char* prefix) {
	int i, j;
	GString *str;
	if (!len)
		return g_strdup (" ()\n");
	str = g_string_new (" (");
	for (i = 0; i + 15 < len; i += 16) {
		if (i == 0)
			g_string_append_printf (str, "\n");
		g_string_append_printf (str, "%s", prefix);
		for (j = 0; j < 16; ++j)
			g_string_append_printf (str, "%02X ", (unsigned char) (data [i + j]));
		g_string_append_printf (str, i == len - 16? ") // ": "  // ");
		for (j = 0; j < 16; ++j)
			g_string_append_printf (str, "%c", data [i + j] >= 32 && data [i + j] <= 126? data [i + j]: '.');
		g_string_append_printf (str, "\n");
	}
	if (i == len)
		return g_string_free (str, FALSE);
	if (len > 16)
		g_string_append_printf (str, "%s", prefix);
	j = i;
	for (; i < len; ++i)
		g_string_append_printf (str, "%02X ", (unsigned char) (data [i]));
	if (len > 16) {
		/* align */
		int count = 16 - (len % 16);
		for (i = 0; i < count; ++i)
			g_string_append_printf (str, "   ");
	}
	g_string_append_printf (str, ") // ");
	for (i = j; i < len; ++i)
		g_string_append_printf (str, "%c", data [i] >= 32 && data [i] <= 126? data [i]: '.');
	g_string_append_printf (str, "\n");
	return g_string_free (str, FALSE);
}

int
dis_isinf (double num)
{
#ifdef HAVE_ISINF
	return isinf (num);
#elif defined(HAVE_IEEEFP_H)
	fpclass_t klass;

	klass = fpclass (num);
	if (klass == FP_NINF)
		return -1;

	if (klass == FP_PINF)
		return 1;

	return 0;
#elif defined(HAVE__FINITE)
	return _finite (num) ? 0 : 1;
#else
#error "Don't know how to implement isinf for this platform."
#endif
}

int
dis_isnan (double num)
{
#ifdef __MINGW32_VERSION
return _isnan (num);
#else
return isnan (num);
#endif
}

