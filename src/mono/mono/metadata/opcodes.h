/**
 * \file
 */

#ifndef __MONO_METADATA_OPCODES_H__
#define __MONO_METADATA_OPCODES_H__

/*
 * opcodes.h: CIL instruction information
 *
 * Author:
 *   Paolo Molaro (lupus@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#include <mono/utils/mono-publib.h>

MONO_BEGIN_DECLS

#define MONO_CUSTOM_PREFIX 0xf0

#define OPDEF(a,b,c,d,e,f,g,h,i,j) \
	MONO_ ## a,

#ifndef __MONO_METADATA_BASIC_BLOCK_H__
typedef enum _MonoOpcodeEnum MonoOpcodeEnum;
#endif

enum _MonoOpcodeEnum {
	MonoOpcodeEnum_Invalid = -1,
#include "mono/cil/opcode.def"
	MONO_CEE_LAST
};

#undef OPDEF

enum {
	MONO_FLOW_NEXT,
	MONO_FLOW_BRANCH,
	MONO_FLOW_COND_BRANCH,
	MONO_FLOW_ERROR,
	MONO_FLOW_CALL,
	MONO_FLOW_RETURN,
	MONO_FLOW_META
};

enum {
	MonoInlineNone          = 0,
	MonoInlineType          = 1,
	MonoInlineField         = 2,
	MonoInlineMethod        = 3,
	MonoInlineTok           = 4,
	MonoInlineString        = 5,
	MonoInlineSig           = 6,
	MonoInlineVar           = 7,
	MonoShortInlineVar      = 8,
	MonoInlineBrTarget      = 9,
	MonoShortInlineBrTarget = 10,
	MonoInlineSwitch        = 11,
	MonoInlineR             = 12,
	MonoShortInlineR        = 13,
	MonoInlineI             = 14,
	MonoShortInlineI        = 15,
	MonoInlineI8            = 16,
};

typedef struct {
	unsigned char argument;
	unsigned char flow_type;
	unsigned short opval;
} MonoOpcode;

MONO_API extern const MonoOpcode mono_opcodes [];

MONO_API const char*
mono_opcode_name (int opcode);

MONO_API MonoOpcodeEnum
mono_opcode_value (const mono_byte **ip, const mono_byte *end);

MONO_END_DECLS

#endif /* __MONO_METADATA_OPCODES_H__ */

