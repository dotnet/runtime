/*
 * util.c: Assorted utilities for the dissasembler
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
#include "util.h"

/**
 * map:
 * @code: code to lookup in table
 * @table: table to decode code
 *
 * Warning: returns static buffer.
 */
const char *
map (guint32 code, map_t *table)
{
	int i;

	for (i = 0; table [i].str != NULL; i++)
		if (table [i].code == code)
			return table [i].str;
	g_assert_not_reached ();
	return "";
}

/**
 * flags:
 * @code: bitfield
 * @table: table to decode bitfield
 *
 * Warning: returns static buffer.
 */
const char *
flags (guint32 code, map_t *table)
{
	static char buffer [1024];
	int i;
	
	buffer [0] = 0;
	
	for (i = 0; table [i].str != NULL; i++)
		if (table [i].code & code)
			strcat (buffer, table [i].str);

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
				printf ("\n0x%08x: ", (unsigned char) base + i);

		printf ("%02x ", (unsigned char) (buffer [i]));
	}
	fflush (stdout);
}

