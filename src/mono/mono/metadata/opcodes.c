/*
 * opcodes.c: CIL instruction information
 *
 * Author:
 *   Paolo Molaro (lupus@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */
#include <mono/metadata/opcodes.h>

#define OPDEF(a,b,c,d,e,f,g,h,i,j) \
	{ Mono ## e, MONO_FLOW_ ## j, h, i },

const MonoOpcode
mono_opcodes [MONO_N_OPCODES] = {
#include "mono/cil/opcode.def"
};

#undef OPDEF

#define OPDEF(a,b,c,d,e,f,g,h,i,j) b,

const char* const
mono_opcode_names [MONO_N_OPCODES] = {
#include "mono/cil/opcode.def"
};

