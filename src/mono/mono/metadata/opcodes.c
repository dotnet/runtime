/*
 * opcodes.c: CIL instruction information
 *
 * Author:
 *   Paolo Molaro (lupus@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */
#include <mono/metadata/opcodes.h>
#include <stddef.h> /* for NULL */

#define MONO_PREFIX1_OFFSET MONO_CEE_ARGLIST
#define MONO_CUSTOM_PREFIX_OFFSET MONO_CEE_MONO_FUNC1

#define OPDEF(a,b,c,d,e,f,g,h,i,j) \
	{ Mono ## e, MONO_FLOW_ ## j, MONO_ ## a },

const MonoOpcode
mono_opcodes [MONO_CEE_LAST + 1] = {
#include "mono/cil/opcode.def"
	{0}
};

#undef OPDEF

#define OPDEF(a,b,c,d,e,f,g,h,i,j) b,

const char* const
mono_opcode_names [MONO_CEE_LAST + 1] = {
#include "mono/cil/opcode.def"
	NULL
};

MonoOpcodeEnum
mono_opcode_value (const guint8 **ip)
{
	MonoOpcodeEnum res;

	if (**ip == 0xfe) {
		++*ip;
		res = **ip + MONO_PREFIX1_OFFSET;
	} else if (**ip == MONO_CUSTOM_PREFIX) {
		++*ip;
		res = **ip + MONO_CUSTOM_PREFIX_OFFSET;
	} else {
		res = **ip;
	}
	
	return res;
}

