/*
 * opcodes.c: CIL instruction information
 *
 * Author:
 *   Paolo Molaro (lupus@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */
#include <mono/metadata/opcodes.h>
#include <malloc.h> /* for NULL */

#define OPDEF(a,b,c,d,e,f,g,h,i,j) \
	{ Mono ## e, MONO_FLOW_ ## j, ((g-1)<<8) | i },

const MonoOpcode
mono_opcodes [MONO_N_OPCODES] = {
#include "mono/cil/opcode.def"
	{0}
};

#undef OPDEF

#define OPDEF(a,b,c,d,e,f,g,h,i,j) b,

const char* const
mono_opcode_names [MONO_N_OPCODES] = {
#include "mono/cil/opcode.def"
	NULL
};

