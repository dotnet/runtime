/*
 * dis-cil.c: Disassembles CIL byte codes
 *
 * Author:
 *   Miguel de Icaza (miguel@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */
#include <config.h>
#include <glib.h>
#include <stdio.h>
#include <wchar.h>
#include "meta.h"
#include "get.h"
#include "dump.h"
#include "dis-cil.h"

enum {
	InlineBrTarget,
	InlineField,
	InlineI,
	InlineI8,
	InlineMethod,
	InlineNone,
	InlineR,
	InlineSig,
	InlineString,
	InlineSwitch,
	InlineTok,
	InlineType,
	InlineVar,
	ShortInlineBrTarget,
	ShortInlineI,
	ShortInlineR,
	ShortInlineVar
};

#define OPDEF(a,b,c,d,e,f,g,h,i,j) \
	{ b, e, g, h, i },

typedef struct {
	char *name;
	int   argument;

	/*
	 * we are not really using any of the following:
	 */
	int   bytes;
	unsigned char  o1, o2;
} opcode_t;

static opcode_t opcodes [300] = {
#include "mono/cil/opcode.def"
};

/*
 * Strings on the US heap are encoded using UTF-16.  Poor man's
 * UTF-16 to UTF-8.  I know its broken, use libunicode later.
 */
static char *
get_encoded_user_string (const char *ptr)
{
	char *res;
	int len, i, j;

	ptr = get_blob_encoded_size (ptr, &len);
	res = g_malloc (len + 1);

	/*
	 * I should really use some kind of libunicode here
	 */
	for (i = 0, j = 0; i < len; j++, i += 2)
		res [j] = ptr [i];

	res [j] = 0;
		
	return res;
}

void
dissasemble_cil (metadata_t *m, const unsigned char *start, int size) 
{
	const unsigned char *end = start + size;
	const unsigned char *ptr = start;
	opcode_t *entry;

	while (ptr < end){
		if (*ptr == 0xfe){
			ptr++;
			entry = &opcodes [*ptr + 256];
		} else 
			entry = &opcodes [*ptr];

		fprintf (output, "\tIL_%04x: %s ", (int) (ptr - start), entry->name);
		ptr++;
		switch (entry->argument){
		case InlineBrTarget: {
			gint target = *(gint32 *) ptr;
			fprintf (output, "IL_%04x", ((int) (ptr - start)) + 4 + target);
			ptr += 4;
			break;
		}
			
		case InlineField: {
			guint32 token = *(guint32 *) ptr;
			char *s;
			
			s = get_field (m, token);
			fprintf (output, "%s", s);
			g_free (s);
			ptr += 4;
			break;
		}
		
		case InlineI: {
			int value = *(int *) ptr;

			fprintf (output, "%d", value);
			ptr += 4;
			break;
		}
		
		case InlineI8: {
			gint64 top = *(guint64 *) ptr;

			fprintf (output, "%lld", (long long) top);
			ptr += 8;
			break;
		}
		
		case InlineMethod: {
			guint32 token = *(guint32 *) ptr;
			char *s;

			s = get_method (m, token);
			fprintf (output, "%s", s);
			g_free (s);
			ptr += 4;
			break;
		}
		
		case InlineNone:
			break;
			
		case InlineR: {
			double r = *(double *) ptr;
			fprintf (output, "%g", r);
			ptr += 8;
			break;
		}
		
		case InlineSig: {
			guint32 token = *(guint32 *) ptr;
			fprintf (output, "signature-0x%08x", token);
			ptr += 4;
			break;
		}
		
		case InlineString: {
			guint32 token = *(guint32 *) ptr;
			
			char *s = get_encoded_user_string (
				mono_metadata_user_string (m, token & 0xffffff));
			
			/*
			 * See section 23.1.4 on the encoding of the #US heap
			 */
			fprintf (output, "\"%s\"", s);
			g_free (s);
			ptr += 4;
			break;
		}

		case InlineSwitch: {
			guint32 count = *(guint32 *) ptr;
			guint32 i;
			
			ptr += 4;
			fprintf (output, "(\n\t\t\t");
			for (i = 0; i < count; i++){
				fprintf (output, "IL_%x", *(guint32 *) ptr);
				ptr += 4;
			}
			fprintf (output, "\t\t\t)");
			break;
		}

		case InlineTok: {
			guint32 token = *(guint32 *) ptr;
			char *s;
			
			s = get_token (m, token);
			fprintf (output, "%s", s);
			g_free (s);
			
			ptr += 4;
			break;
		}
		
		case InlineType: {
			guint32 token = *(guint32 *) ptr;
			char *s = get_token_type (m, token);
			fprintf (output, "%s", s);
			g_free (s);
			ptr += 4;
			break;
		}

		case InlineVar: {
			gint16 var_idx = *(gint16 *) ptr;

			fprintf (output, "variable-%d\n", var_idx);
			ptr += 2;
			break;
		}

		case ShortInlineBrTarget: {
			signed char x = *ptr;
			
			fprintf (output, "IL_%04x", ptr - start + 1 + x);
			ptr++;
			break;
		}

		case ShortInlineI: {
			char x = *ptr;

			fprintf (output, "0x%02x", x);
			ptr++;
			break;
		}

		case ShortInlineR: {
			float f = *(float *) ptr;

			fprintf (output, "%g", (double) f);
			ptr += 4;
			break;
		}

		case ShortInlineVar: {
			signed char x = *ptr;

			fprintf (output, "V_%d", (int) x);
			ptr++;
			break;
		}

		}

		fprintf (output, "\n");
	}
}
