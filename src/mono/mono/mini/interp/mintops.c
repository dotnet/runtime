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

#define CallArgs MINT_CALL_ARGS

#define OPDEF(a,b,c,d,e,f) e,
int const mono_interp_op_sregs[] = {
#include "mintops.def"
};
#undef OPDEF

#define OPDEF(a,b,c,d,e,f) d,
int const mono_interp_op_dregs[] = {
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
		int n = READ32 (ip + 2);
		len = MINT_SWITCH_LEN (n);
	}

	return ip + len;
}

const char*
mono_interp_opname (int op)
{
	return ((const char*)&mono_interp_opname_characters) + mono_interp_opname_offsets [op];
}
