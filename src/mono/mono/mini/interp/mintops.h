/**
 * \file
 */

#ifndef __INTERPRETER_MINTOPS_H
#define __INTERPRETER_MINTOPS_H

#include <glib.h>

typedef enum
{
	MintOpNoArgs,
	MintOpShortInt,
	MintOpUShortInt,
	MintOpInt,
	MintOpLongInt,
	MintOpFloat,
	MintOpDouble,
	MintOpBranch,
	MintOpShortBranch,
	MintOpSwitch,
	MintOpMethodToken,
	MintOpFieldToken,
	MintOpClassToken,
	MintOpTwoShorts,
	MintOpShortAndInt,
	MintOpShortAndShortBranch
} MintOpArgType;

#define OPDEF(a,b,c,d,e,f) a,
typedef enum {
#include "mintops.def"
	MINT_LASTOP
} MintOpcode;
#undef OPDEF

#if NO_UNALIGNED_ACCESS
#  if G_BYTE_ORDER == G_LITTLE_ENDIAN
#define READ32(x) (((guint16 *)(x)) [0] | ((guint16 *)(x)) [1] << 16)
#define READ64(x) ((guint64)((guint16 *)(x)) [0] | \
                   (guint64)((guint16 *)(x)) [1] << 16 | \
                   (guint64)((guint16 *)(x)) [2] << 32 | \
                   (guint64)((guint16 *)(x)) [3] << 48)
#  else
#define READ32(x) (((guint16 *)(x)) [0] << 16 | ((guint16 *)(x)) [1])
#define READ64(x) ((guint64)((guint16 *)(x)) [0] << 48 | \
                   (guint64)((guint16 *)(x)) [1] << 32 | \
                   (guint64)((guint16 *)(x)) [2] << 16 | \
                   (guint64)((guint16 *)(x)) [3])
#  endif
#else /* unaligned access OK */
#define READ32(x) (*(guint32 *)(x))
#define READ64(x) (*(guint64 *)(x))
#endif

#define MINT_SWITCH_LEN(n) (4 + (n) * 2)

#define MINT_IS_MOV(op) ((op) >= MINT_MOV_I1 && (op) <= MINT_MOV_VT)
#define MINT_IS_CONDITIONAL_BRANCH(op) ((op) >= MINT_BRFALSE_I4 && (op) <= MINT_BLT_UN_R8_S)
#define MINT_IS_UNOP_CONDITIONAL_BRANCH(op) ((op) >= MINT_BRFALSE_I4 && (op) <= MINT_BRTRUE_R8_S)
#define MINT_IS_BINOP_CONDITIONAL_BRANCH(op) ((op) >= MINT_BEQ_I4 && (op) <= MINT_BLT_UN_R8_S)
#define MINT_IS_CALL(op) ((op) >= MINT_CALL && (op) <= MINT_JIT_CALL)
#define MINT_IS_PATCHABLE_CALL(op) ((op) >= MINT_CALL && (op) <= MINT_VCALL)
#define MINT_IS_LDC_I4(op) ((op) >= MINT_LDC_I4_M1 && (op) <= MINT_LDC_I4)
#define MINT_IS_LDC_I8(op) ((op) >= MINT_LDC_I8_0 && (op) <= MINT_LDC_I8)
#define MINT_IS_UNOP(op) ((op) >= MINT_ADD1_I4 && (op) <= MINT_CEQ0_I4)
#define MINT_IS_BINOP(op) ((op) >= MINT_ADD_I4 && (op) <= MINT_CLT_UN_R8)
#define MINT_IS_BINOP_SHIFT(op) ((op) >= MINT_SHR_UN_I4 && (op) <= MINT_SHR_I8)
#define MINT_IS_LDFLD(op) ((op) >= MINT_LDFLD_I1 && (op) <= MINT_LDFLD_O)
#define MINT_IS_STFLD(op) ((op) >= MINT_STFLD_I1 && (op) <= MINT_STFLD_O)
#define MINT_IS_LDIND_INT(op) ((op) >= MINT_LDIND_I1 && (op) <= MINT_LDIND_I8)
#define MINT_IS_STIND_INT(op) ((op) >= MINT_STIND_I1 && (op) <= MINT_STIND_I8)

#define MINT_CALL_ARGS 2
#define MINT_CALL_ARGS_SREG -2

extern unsigned char const mono_interp_oplen[];
extern int const mono_interp_op_dregs [];
extern int const mono_interp_op_sregs [];
extern MintOpArgType const mono_interp_opargtype[];
extern const guint16* mono_interp_dis_mintop_len (const guint16 *ip);

// This, instead of an array of pointers, to optimize away a pointer and a relocation per string.
extern const guint16 mono_interp_opname_offsets [ ];
typedef struct MonoInterpOpnameCharacters MonoInterpOpnameCharacters;
extern const MonoInterpOpnameCharacters mono_interp_opname_characters;

const char*
mono_interp_opname (int op);

#endif
