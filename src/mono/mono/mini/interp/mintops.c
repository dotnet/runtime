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

char *
mono_interp_dis_mintop(const guint16 *base, const guint16 *ip)
{
	GString *str = g_string_new ("");
	guint32 token;
	int target;

	g_string_append_printf (str, "IR_%04x: %-10s", (int)(ip - base), mono_interp_opname (*ip));
	switch (mono_interp_opargtype [*ip]) {
	case MintOpNoArgs:
		break;
	case MintOpUShortInt:
		g_string_append_printf (str, " %u", * (guint16 *)(ip + 1));
		break;
	case MintOpTwoShorts:
		g_string_append_printf (str, " %u,%u", * (guint16 *)(ip + 1), * (guint16 *)(ip + 2));
		break;
	case MintOpShortAndInt:
		g_string_append_printf (str, " %u,%u", * (guint16 *)(ip + 1), (guint32)READ32(ip + 2));
		break;
	case MintOpShortInt:
		g_string_append_printf (str, " %d", * (short *)(ip + 1));
		break;
	case MintOpClassToken:
	case MintOpMethodToken:
	case MintOpFieldToken:
		token = * (guint16 *)(ip + 1);
		g_string_append_printf (str, " %u", token);
		break;
	case MintOpInt:
		g_string_append_printf (str, " %d", (gint32)READ32 (ip + 1));
		break;
	case MintOpLongInt:
		g_string_append_printf (str, " %lld", (long long)READ64 (ip + 1));
		break;
	case MintOpFloat: {
		gint32 tmp = READ32 (ip + 1);
		g_string_append_printf (str, " %g", * (float *)&tmp);
		break;
	}
	case MintOpDouble: {
		gint64 tmp = READ64 (ip + 1);
		g_string_append_printf (str, " %g", * (double *)&tmp);
		break;
	}
	case MintOpShortBranch:
		target = ip + * (short *)(ip + 1) - base;
		g_string_append_printf (str, " IR_%04x", target);
		break;
	case MintOpBranch:
		target = ip + (gint32)READ32 (ip + 1) - base;
		g_string_append_printf (str, " IR_%04x", target);
		break;
	case MintOpSwitch: {
		const guint16 *p = ip + 1;
		int sval = (gint32)READ32 (p);
		int i;
		p += 2;
		g_string_append_printf (str, "(");
		for (i = 0; i < sval; ++i) {
			int offset;
			if (i > 0)
				g_string_append_printf (str, ", ");
			offset = (gint32)READ32 (p);
			g_string_append_printf (str, "IR_%04x", (int)(p + offset - base));
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
