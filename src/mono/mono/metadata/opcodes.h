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
	MonoInlineNone,
	MonoInlineType,
	MonoInlineField,
	MonoInlineMethod,
	MonoInlineTok,
	MonoInlineString,
	MonoInlineSig,
	MonoInlineVar,
	MonoShortInlineVar,
	MonoInlineBrTarget,
	MonoShortInlineBrTarget,
	MonoInlineSwitch,
	MonoInlineR,
	MonoShortInlineR,
	MonoInlineI,
	MonoShortInlineI,
	MonoInlineI8
};

typedef struct {
	unsigned char argument;
	unsigned char flow_type;
	unsigned char byte0, byte1;
} MonoOpcode;

#define MONO_N_OPCODES 300

extern const MonoOpcode mono_opcodes [MONO_N_OPCODES];
extern const char* const mono_opcode_names [MONO_N_OPCODES];

#endif /* __MONO_METADATA_OPCODES_H__ */
