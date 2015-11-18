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
#include <math.h>
#ifdef	HAVE_WCHAR_H
#include <wchar.h>
#endif
#include "meta.h"
#include "get.h"
#include "dump.h"
#include "dis-cil.h"
#include "util.h"
#include "mono/metadata/opcodes.h"
#include "mono/metadata/class-internals.h"
#include "mono/utils/mono-compiler.h"

#define CODE_INDENT g_assert (indent_level < 512); \
	indent[indent_level*2] = ' ';	\
	indent[indent_level*2+1] = ' ';	\
	++indent_level;	\
	indent[indent_level*2] = 0;
#define CODE_UNINDENT g_assert (indent_level);	\
	--indent_level;	\
	indent[indent_level*2] = 0;

void
disassemble_cil (MonoImage *m, MonoMethodHeader *mh, MonoGenericContainer *container)
{
	const unsigned char *start = mh->code;
	int size = mh->code_size;
	const unsigned char *end = start + size;
	const unsigned char *ptr = start;
	const MonoOpcode *entry;
	char indent[1024];
	int i, j, indent_level = 0;
	gboolean in_fault = 0;
	const char *clause_names[] = {"catch", "filter", "finally", "", "fault"};
	gboolean *trys = NULL;
	indent [0] = 0;

#ifdef DEBUG
	for (i = 0; i < mh->num_clauses; ++i) {
#define clause mh->clauses [i]
	       g_print ("/* out clause %d: from %d len=%d, handler at %d, %d */\n",
			clause.flags, clause.try_offset, clause.try_len, clause.handler_offset, clause.handler_len);
#undef clause
	}
#endif

       if (mh->num_clauses) {
	       trys = (gboolean *)g_malloc0 (sizeof (gboolean) * mh->num_clauses);
	       trys [0] = 1;
	       for (i=1; i < mh->num_clauses; ++i) {
#define jcl mh->clauses [j]	
#define cl mh->clauses [i]	
		       trys [i] = 1;
		       for (j = 0; j < i; j++) {
			       if (cl.try_offset == jcl.try_offset && cl.try_len == jcl.try_len) {
				       trys [i] = 0;
				       break;
			       }
		       }
#undef jcl
#undef cl
	       }
       }

	while (ptr < end){
		for (i = mh->num_clauses - 1; i >= 0 ; --i) {
			if (ptr == start + mh->clauses[i].try_offset && trys [i]) {
				fprintf (output, "\t%s.try { // %d\n", indent, i);
				CODE_INDENT;
			}
                        
			if (ptr == start + mh->clauses[i].handler_offset) {
                                if (mh->clauses[i].flags == MONO_EXCEPTION_CLAUSE_FILTER) {
                                        CODE_UNINDENT;
                                        fprintf (output, "\t%s} { // %d\n", indent, i);
                                } else {
                                        char * klass = mh->clauses[i].flags ? g_strdup ("") :
						dis_stringify_object_with_class (m, mh->clauses[i].data.catch_class,
										 TRUE, FALSE);
                                        fprintf (output, "\t%s%s %s { // %d\n", indent,
                                                        clause_names [mh->clauses[i].flags], klass, i);
                                        g_free (klass);
                                }
				CODE_INDENT;
                                if (mh->clauses[i].flags == MONO_EXCEPTION_CLAUSE_FAULT)
                                        in_fault = 1;
			} 
                        if (mh->clauses[i].flags == MONO_EXCEPTION_CLAUSE_FILTER && ptr == start + mh->clauses[i].data.filter_offset) {
                                fprintf (output, "\t%s%s {\n", indent, clause_names[1]);
                                CODE_INDENT;
                        }
		}
		fprintf (output, "\t%sIL_%04x: ", indent, (int) (ptr - start));
		i = *ptr;
		if (*ptr == 0xfe){
			ptr++;
			i = *ptr + 256;
		} 
		entry = &mono_opcodes [i];

                if (in_fault && entry->opval == 0xDC)
                        fprintf (output, " %s", "endfault");
                else
                        fprintf (output, " %s ", mono_opcode_name (i));
		ptr++;
		switch (entry->argument){
		case MonoInlineBrTarget: {
			gint target = read32 (ptr);
			fprintf (output, "IL_%04x\n", ((int) (ptr - start)) + 4 + target);
			ptr += 4;
			break;
		}
			
		case MonoInlineField: {
			guint32 token = read32 (ptr);
			char *s;
			
			s = get_field (m, token, container);
			fprintf (output, "%s", s);
			g_free (s);
			ptr += 4;
			break;
		}
		
		case MonoInlineI: {
			int value = read32 (ptr);

			fprintf (output, "%d", value);
			ptr += 4;
			break;
		}
		
		case MonoInlineI8: {
			gint64 top = read64 (ptr);

			fprintf (output, "0x%llx", (long long) top);
			ptr += 8;
			break;
		}
		
		case MonoInlineMethod: {
			guint32 token = read32 (ptr);
			char *s;

			s = get_method (m, token, container);
			fprintf (output, "%s", s);
			g_free (s);
			ptr += 4;
			break;
		}
		
		case MonoInlineNone:
			break;
			
		case MonoInlineR: {
			double r;
			int inf;
			readr8 (ptr, &r);
			inf = dis_isinf (r);
			if (inf == -1) 
				fprintf (output, "(00 00 00 00 00 00 f0 ff)"); /* negative infinity */
			else if (inf == 1)
				fprintf (output, "(00 00 00 00 00 00 f0 7f)"); /* positive infinity */
			else if (dis_isnan (r))
				fprintf (output, "(00 00 00 00 00 00 f8 ff)"); /* NaN */
			else {
				char *str = stringify_double (r);
				fprintf (output, "%s", str);
				g_free (str);
			}
			ptr += 8;
			break;
		}
		
		case MonoInlineSig: {
			guint32 token = read32 (ptr);
			fprintf (output, "signature-0x%08x", token);
			ptr += 4;
			break;
		}
		
		case MonoInlineString: {
			guint32 token = read32 (ptr);
			const char *us_ptr = mono_metadata_user_string (m, token & 0xffffff);
			int len = mono_metadata_decode_blob_size (us_ptr, (const char**)&us_ptr);

			char *s = get_encoded_user_string_or_bytearray ((const guchar*)us_ptr, len);
			
			/*
			 * See section 23.1.4 on the encoding of the #US heap
			 */
			fprintf (output, "%s", s);
			g_free (s);
			ptr += 4;
			break;
		}

		case MonoInlineSwitch: {
			guint32 count = read32 (ptr);
			const unsigned char *endswitch;
			guint32 n;
			
			ptr += 4;
			endswitch = ptr + sizeof (guint32) * count;
			fprintf (output, count > 0 ? "(\n" : "( )");
			CODE_INDENT;
			for (n = 0; n < count; n++){
				fprintf (output, "\t%sIL_%04x%s", indent, 
						 (int)(endswitch-start+read32 (ptr)), 
						 n == count - 1 ? ")" : ",\n");
				ptr += 4;
			}
			CODE_UNINDENT;
			break;
		}

		case MonoInlineTok: {
			guint32 token = read32 (ptr);
			char *s;
			
			s = get_token (m, token, container);
			fprintf (output, "%s", s);
			g_free (s);
			
			ptr += 4;
			break;
		}
		
		case MonoInlineType: {
			guint32 token = read32 (ptr);
			char *s = get_token_type (m, token, container);
			fprintf (output, "%s", s);
			g_free (s);
			ptr += 4;
			break;
		}

		case MonoInlineVar: {
			guint16 var_idx = read16 (ptr);

			fprintf (output, "%d\n", var_idx);
			ptr += 2;
			break;
		}

		case MonoShortInlineBrTarget: {
			signed char x = *ptr;
			
			fprintf (output, "IL_%04x\n", (int)(ptr - start + 1 + x));
			ptr++;
			break;
		}

		case MonoShortInlineI: {
			char x = *ptr;

			fprintf (output, "0x%02x", x);
			ptr++;
			break;
		}

		case MonoShortInlineR: {
			float f;
			int inf;
			
			readr4 (ptr, &f);

			inf = dis_isinf (f);
			if (inf == -1) 
				fprintf (output, "(00 00 80 ff)"); /* negative infinity */
			else if (inf == 1)
				fprintf (output, "(00 00 80 7f)"); /* positive infinity */
			else if (dis_isnan (f))
				fprintf (output, "(00 00 c0 ff)"); /* NaN */
			else {
				char *str = stringify_double ((double) f);
				fprintf (output, "%s", str);
				g_free (str);
			}
			ptr += 4;
			break;
		}

		case MonoShortInlineVar: {
			unsigned char x = *ptr;

			fprintf (output, "%d", (int) x);
			ptr++;
			break;
		}
		default:
			break;
		}

		fprintf (output, "\n");
		for (i = 0; i < mh->num_clauses; ++i) {
			if (ptr == start + mh->clauses[i].try_offset + mh->clauses[i].try_len && trys [i]) {
				CODE_UNINDENT;
				fprintf (output, "\t%s} // end .try %d\n", indent, i);
			}
			if (ptr == start + mh->clauses[i].handler_offset + mh->clauses[i].handler_len) {
				CODE_UNINDENT;
				fprintf (output, "\t%s} // end handler %d\n", indent, i);
                                if (mh->clauses[i].flags == MONO_EXCEPTION_CLAUSE_FAULT)
                                        in_fault = 0;
			}
		}
	}
	if (trys)
		g_free (trys);
}
