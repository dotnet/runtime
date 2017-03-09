/**
 * \file
 * CIL instruction information
 *
 * Author:
 *   Paolo Molaro (lupus@ximian.com)
 *
 * Copyright 2002-2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include <mono/metadata/opcodes.h>
#include <stddef.h> /* for NULL */
#include <config.h>

#define MONO_PREFIX1_OFFSET MONO_CEE_ARGLIST
#define MONO_CUSTOM_PREFIX_OFFSET MONO_CEE_MONO_ICALL

#define OPDEF(a,b,c,d,e,f,g,h,i,j) \
	{ Mono ## e, MONO_FLOW_ ## j, MONO_ ## a },

const MonoOpcode
mono_opcodes [MONO_CEE_LAST + 1] = {
#include "mono/cil/opcode.def"
	{0}
};

#undef OPDEF

#ifdef HAVE_ARRAY_ELEM_INIT
#define MSGSTRFIELD(line) MSGSTRFIELD1(line)
#define MSGSTRFIELD1(line) str##line
static const struct msgstr_t {
#define OPDEF(a,b,c,d,e,f,g,h,i,j) char MSGSTRFIELD(__LINE__) [sizeof (b)];
#include "mono/cil/opcode.def"
#undef OPDEF
} opstr = {
#define OPDEF(a,b,c,d,e,f,g,h,i,j) b,
#include "mono/cil/opcode.def"
#undef OPDEF
};
static const int16_t opidx [] = {
#define OPDEF(a,b,c,d,e,f,g,h,i,j) [MONO_ ## a] = offsetof (struct msgstr_t, MSGSTRFIELD(__LINE__)),
#include "mono/cil/opcode.def"
#undef OPDEF
};

/**
 * mono_opcode_name:
 */
const char*
mono_opcode_name (int opcode)
{
	return (const char*)&opstr + opidx [opcode];
}

#else
#define OPDEF(a,b,c,d,e,f,g,h,i,j) b,
static const char* const
mono_opcode_names [MONO_CEE_LAST + 1] = {
#include "mono/cil/opcode.def"
	NULL
};

const char*
mono_opcode_name (int opcode)
{
	return mono_opcode_names [opcode];
}

#endif

MonoOpcodeEnum
mono_opcode_value (const mono_byte **ip, const mono_byte *end)
{
	MonoOpcodeEnum res;
	const mono_byte *p = *ip;

	if (p >= end)
		return (MonoOpcodeEnum)-1;
	if (*p == 0xfe) {
		++p;
		if (p >= end)
			return (MonoOpcodeEnum)-1;
		res = (MonoOpcodeEnum)(*p + MONO_PREFIX1_OFFSET);
	} else if (*p == MONO_CUSTOM_PREFIX) {
		++p;
		if (p >= end)
			return (MonoOpcodeEnum)-1;
		res = (MonoOpcodeEnum)(*p + MONO_CUSTOM_PREFIX_OFFSET);
	} else {
		res = (MonoOpcodeEnum)*p;
	}
	*ip = p;
	return res;
}

