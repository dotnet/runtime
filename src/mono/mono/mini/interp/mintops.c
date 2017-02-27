/*
 * Utilities for handling interpreter VM instructions
 *
 * Authors:
 *   Bernie Solomon (bernard@ugsolutions.com)
 *
 */
#include <glib.h>
#include <stdio.h>
#include "mintops.h"

#define OPDEF(a,b,c,d) \
	b,
const char *mono_interp_opname[] = {
#include "mintops.def"
	""
};
#undef OPDEF

#define OPDEF(a,b,c,d) \
	c,
unsigned char mono_interp_oplen[] = {
#include "mintops.def"
	0
};
#undef OPDEF


#define OPDEF(a,b,c,d) \
	d,
MintOpArgType mono_interp_opargtype[] = {
#include "mintops.def"
	0
};
#undef OPDEF

const guint16 *
mono_interp_dis_mintop(const guint16 *base, const guint16 *ip)
{
	int len = mono_interp_oplen [*ip];
	guint32 token;
	int target;
	if (len < 0 || len > 10) {
		g_print ("op %d len %d\n", *ip, len);
		g_assert_not_reached ();
	} else if (len == 0) { /* SWITCH */
		int n = READ32 (ip + 1);
		len = 3 + n * 2;
	}

	g_print ("IL_%04x: %-10s", ip - base, mono_interp_opname [*ip]);
	switch (mono_interp_opargtype [*ip]) {
	case MintOpNoArgs:
		break;
	case MintOpUShortInt:
		g_print (" %u", * (guint16 *)(ip + 1));
		break;
	case MintOpTwoShorts:
		g_print (" %u,%u", * (guint16 *)(ip + 1), * (guint16 *)(ip + 2));
		break;
	case MintOpShortAndInt:
		g_print (" %u,%u", * (guint16 *)(ip + 1), (guint32)READ32(ip + 2));
		break;
	case MintOpShortInt:
		g_print (" %d", * (short *)(ip + 1));
		break;
	case MintOpClassToken:
	case MintOpMethodToken:
	case MintOpFieldToken:
		token = * (guint16 *)(ip + 1);
		g_print (" %u", token);
		break;
	case MintOpInt:
		g_print (" %d", (gint32)READ32 (ip + 1));
		break;
	case MintOpLongInt:
		g_print (" %lld", (gint64)READ64 (ip + 1));
		break;
	case MintOpFloat: {
		gint32 tmp = READ32 (ip + 1);
		g_print (" %g", * (float *)&tmp);
		break;
	}
	case MintOpDouble: {
		gint64 tmp = READ64 (ip + 1);
		g_print (" %g", * (double *)&tmp);
		break;
	}
	case MintOpShortBranch:
		target = ip + * (short *)(ip + 1) - base;
		g_print (" IL_%04x", target);
		break;
	case MintOpBranch:
		target = ip + (gint32)READ32 (ip + 1) - base;
		g_print (" IL_%04x", target);
		break;
	case MintOpSwitch: {
		const guint16 *p = ip + 1;
		int sval = (gint32)READ32 (p);
		int i;
		p += 2;
		g_print ("(");
		for (i = 0; i < sval; ++i) {
			int offset;
			if (i > 0)
				g_print (", ");
			offset = (gint32)READ32 (p);
			g_print ("IL_%04x", ip - base + 3 + 2 * sval + offset);
			p += 2;
		}
		g_print (")");
		break;
	}
	default:
		g_print("unknown arg type\n");
	}

	return ip + len;
}

