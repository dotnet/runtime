/* Poping */
/* 1 bit */
#define Pop0    1
/* 2 bits */
#define Pop1    2
/* 3 bits */
#define PopI    8
/* 1 bit */
#define PopI8   64
/* 1 bit */
#define Pop8    128
/* 1 bit */
#define PopR4   256
/* 1 bit */
#define PopR8   512
/* 1 bit */
#define PopRef  1024
/* 1 bit */
#define VarPop  2048

/* Pushing */
#define Push0   1
#define PushI   2
#define PushI8  4
#define PushR4  8
#define PushR8  16
#define PushRef 32
#define VarPush 64
#define Push1   128

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
#include "meta.h"
#include "dump.h"
#include "dis-cil.h"

/* Poping */
/* 1 bit */
#define Pop0    1
/* 2 bits */
#define Pop1    2
/* 3 bits */
#define PopI    8
/* 1 bit */
#define PopI8   64
/* 1 bit */
#define Pop8    128
/* 1 bit */
#define PopR4   256
/* 1 bit */
#define PopR8   512
/* 1 bit */
#define PopRef  1024
/* 1 bit */
#define VarPop  2048

/* Pushing */
#define Push0   1
#define PushI   2
#define PushI8  4
#define PushR4  8
#define PushR8  16
#define PushRef 32
#define VarPush 64
#define Push1   128

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
	{ b, c, d, e, g, h, i },

typedef struct {
	char *name;
	int   pop, push;
	int   argument;
	int   bytes;
	unsigned char  o1, o2;
} opcode_t;

static opcode_t opcodes [300] = {
#include "mono/cil/opcode.def"
};

void
disassemble_cil (MonoMetadata *m, const unsigned char *start, int size) 
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

		ptr++;

		fprintf (output, "\tIL_%04x: %s ", ptr - start, entry->name);
		switch (entry->argument){
		case InlineBrTarget: {
			gint target = *(gint32 *) ptr;
			fprintf (output, "IL_%04x", ptr + 4 + target);
			ptr += 4;
			break;
		}
			
		case InlineField: {
			token = *(guint32 *) ptr;
			fprintf (output, "fieldref-0x%08x", token);
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
			gint64 top = *(guint64 *) value;

			fprintf (output, "%ld", top);
			ptr += 8;
			break;
		}
		
		case InlineMethod: {
			token = *(guint32 *) ptr;
			fprintf (output, "method-0x%08x", token);
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

			fprintf (output, "string-%0x08x", token);
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

			fprintf (output, "TOKEN_%08x", token);
			ptr += 4;
			break;
		}
		
		case InlineType: {
			guint32 token = *(guint32 *) ptr;

			fprintf (output, "Type-%08x", token);
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
			ptr++:
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

			fprintf (output, "Varidx-%d", (int) x);
			ptr++;
			break;
		}m

		}

		fprintf (output, "\n");
	}
}
