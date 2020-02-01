/**
 * \file
 * Utilities for handling interpreter VM instructions
 *
 * Authors:
 *   Bernie Solomon (bernard@ugsolutions.com)
 *
 */
#include <glib.h>
#include <stdio.h>
#include "mintops.h"

// This, instead of an array of pointers, to optimize away a pointer and a relocation per string.
struct MonoInterpOpnameCharacters {
#define OPDEF(a,b,c,d,e,f) char a [sizeof (b)];
#include "mintops.def"
};
#undef OPDEF

const MonoInterpOpnameCharacters mono_interp_opname_characters = {
#define OPDEF(a,b,c,d,e,f) b,
#include "mintops.def"
};
#undef OPDEF

const guint16 mono_interp_opname_offsets [] = {
#define OPDEF(a,b,c,d,e,f) offsetof (MonoInterpOpnameCharacters, a),
#include "mintops.def"
#undef OPDEF
};

#define OPDEF(a,b,c,d,e,f) c,
unsigned char const mono_interp_oplen [] = {
#include "mintops.def"
};
#undef OPDEF

#define Push0 0
#define Push1 1
#define Push2 2
#define Pop0 0
#define Pop1 1
#define Pop2 2
#define Pop3 3
#define Pop4 4
#define Pop5 5
#define Pop6 6
#define PopAll MINT_POP_ALL
#define VarPush MINT_VAR_PUSH
#define VarPop MINT_VAR_POP

#define OPDEF(a,b,c,d,e,f) d,
int const mono_interp_oppop[] = {
#include "mintops.def"
};
#undef OPDEF

#define OPDEF(a,b,c,d,e,f) e,
int const mono_interp_oppush[] = {
#include "mintops.def"
};
#undef OPDEF

#define OPDEF(a,b,c,d,e,f) f,
MintOpArgType const mono_interp_opargtype [] = {
#include "mintops.def"
};
#undef OPDEF

const guint16*
mono_interp_dis_mintop_len (const guint16 *ip)
{
	int len = mono_interp_oplen [*ip];

	if (len < 0 || len > 10) {
		g_print ("op %d len %d\n", *ip, len);
		g_assert_not_reached ();
	} else if (len == 0) { /* SWITCH */
		int n = READ32 (ip + 1);
		len = MINT_SWITCH_LEN (n);
	}

	return ip + len;
}

/*
 * ins_offset is the associated offset of this instruction
 * native_offset indicates whether this instruction is part of the compacted
 * instruction stream or is part of an InterpInst
 * ip is the address where the arguments of the instruction are located
 */
char *
mono_interp_dis_mintop (gint32 ins_offset, gboolean native_offset, const guint16 *ip, guint16 opcode)
{
	GString *str = g_string_new ("");
	guint32 token;
	int target;

	if (native_offset)
		g_string_append_printf (str, "IR_%04x: %-10s", ins_offset, mono_interp_opname (opcode));
	else
		g_string_append_printf (str, "IL_%04x: %-10s", ins_offset, mono_interp_opname (opcode));

	switch (mono_interp_opargtype [opcode]) {
	case MintOpNoArgs:
		break;
	case MintOpUShortInt:
		g_string_append_printf (str, " %u", *(guint16*)ip);
		break;
	case MintOpTwoShorts:
		g_string_append_printf (str, " %u,%u", *(guint16*)ip, *(guint16 *)(ip + 1));
		break;
	case MintOpShortAndInt:
		g_string_append_printf (str, " %u,%u", *(guint16*)ip, (guint32)READ32(ip + 1));
		break;
	case MintOpShortInt:
		g_string_append_printf (str, " %d", *(gint16*)ip);
		break;
	case MintOpClassToken:
	case MintOpMethodToken:
	case MintOpFieldToken:
		token = * (guint16 *) ip;
		g_string_append_printf (str, " %u", token);
		break;
	case MintOpInt:
		g_string_append_printf (str, " %d", (gint32)READ32 (ip));
		break;
	case MintOpLongInt:
		g_string_append_printf (str, " %" PRId64, (gint64)READ64 (ip));
		break;
	case MintOpFloat: {
		gint32 tmp = READ32 (ip);
		g_string_append_printf (str, " %g", * (float *)&tmp);
		break;
	}
	case MintOpDouble: {
		gint64 tmp = READ64 (ip);
		g_string_append_printf (str, " %g", * (double *)&tmp);
		break;
	}
	case MintOpShortBranch:
		if (native_offset) {
			target = ins_offset + *(gint16*)ip;
			g_string_append_printf (str, " IR_%04x", target);
		} else {
			/* the target IL is already embedded in the instruction */
			g_string_append_printf (str, " IL_%04x", *(gint16*)ip);
		}
		break;
	case MintOpBranch:
		if (native_offset) {
			target = ins_offset + (gint32)READ32 (ip);
			g_string_append_printf (str, " IR_%04x", target);
		} else {
			g_string_append_printf (str, " IL_%04x", (gint32)READ32 (ip));
		}
		break;
	case MintOpSwitch: {
		int sval = (gint32)READ32 (ip);
		int i;
		g_string_append_printf (str, "(");
		gint32 p = 2;
		for (i = 0; i < sval; ++i) {
			if (i > 0)
				g_string_append_printf (str, ", ");
			if (native_offset) {
				int offset = (gint32)READ32 (ip + p);
				g_string_append_printf (str, "IR_%04x", ins_offset + 1 + p + offset);
			} else {
				g_string_append_printf (str, "IL_%04x", (gint32)READ32 (ip + p));
			}
			p += 2;
		}
		g_string_append_printf (str, ")");
		break;
	}
	default:
		g_string_append_printf (str, "unknown arg type\n");
	}

	return g_string_free (str, FALSE);
}

const char*
mono_interp_opname (int op)
{
	return ((const char*)&mono_interp_opname_characters) + mono_interp_opname_offsets [op];
}
