/**
 * \file
 */

#ifndef __INTERPRETER_MINTOPS_H
#define __INTERPRETER_MINTOPS_H

#include <config.h>
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
	MintOpTwoInts,
	MintOpShortAndInt,
	MintOpShortAndShortBranch,
	MintOpPair2,
	MintOpPair3,
	MintOpPair4
} MintOpArgType;

#define OPDEF(a,b,c,d,e,f) a,
typedef enum {
#include "mintops.def"
	MINT_LASTOP
} MintOpcode;
#undef OPDEF

/* SIMD opcodes, grouped by signature */

#define INTERP_SIMD_INTRINSIC_P_P(a,b,c)
#define INTERP_SIMD_INTRINSIC_P_PP(a,b,c)
#define INTERP_SIMD_INTRINSIC_P_PPP(a,b,c)

#undef INTERP_SIMD_INTRINSIC_P_P
#define INTERP_SIMD_INTRINSIC_P_P(a,b,c) a,
typedef enum {
#include "interp-simd-intrins.def"
} MintSIMDOpsPP;
#undef INTERP_SIMD_INTRINSIC_P_P
#define INTERP_SIMD_INTRINSIC_P_P(a,b,c)

#undef INTERP_SIMD_INTRINSIC_P_PP
#define INTERP_SIMD_INTRINSIC_P_PP(a,b,c) a,
typedef enum {
#include "interp-simd-intrins.def"
	INTERP_SIMD_INTRINSIC_P_PP_LAST
} MintSIMDOpsPPP;
#undef INTERP_SIMD_INTRINSIC_P_PP
#define INTERP_SIMD_INTRINSIC_P_PP(a,b,c)

#undef INTERP_SIMD_INTRINSIC_P_PPP
#define INTERP_SIMD_INTRINSIC_P_PPP(a,b,c) a,
typedef enum {
#include "interp-simd-intrins.def"
	INTERP_SIMD_INTRINSIC_P_PPP_LAST
} MintSIMDOpsPPPP;
#undef INTERP_SIMD_INTRINSIC_P_PPP
#define INTERP_SIMD_INTRINSIC_P_PPP(a,b,c)

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

#if SIZEOF_VOID_P == 8
#define MINT_NEG_P MINT_NEG_I8
#define MINT_NOT_P MINT_NOT_I8

#define MINT_NEG_FP MINT_NEG_R8

#define MINT_ADD_P MINT_ADD_I8
#define MINT_ADD_P_IMM MINT_ADD_I8_IMM
#define MINT_SUB_P MINT_SUB_I8
#define MINT_MUL_P MINT_MUL_I8
#define MINT_DIV_P MINT_DIV_I8
#define MINT_DIV_UN_P MINT_DIV_UN_I8
#define MINT_REM_P MINT_REM_I8
#define MINT_REM_UN_P MINT_REM_UN_I8
#define MINT_AND_P MINT_AND_I8
#define MINT_OR_P MINT_OR_I8
#define MINT_XOR_P MINT_XOR_I8
#define MINT_SHL_P MINT_SHL_I8
#define MINT_SHR_P MINT_SHR_I8
#define MINT_SHR_UN_P MINT_SHR_UN_I8

#define MINT_CEQ_P MINT_CEQ_I8
#define MINT_CNE_P MINT_CNE_I8
#define MINT_CLT_P MINT_CLT_I8
#define MINT_CLT_UN_P MINT_CLT_UN_I8
#define MINT_CGT_P MINT_CGT_I8
#define MINT_CGT_UN_P MINT_CGT_UN_I8
#define MINT_CLE_P MINT_CLE_I8
#define MINT_CLE_UN_P MINT_CLE_UN_I8
#define MINT_CGE_P MINT_CGE_I8
#define MINT_CGE_UN_P MINT_CGE_UN_I8

#define MINT_ADD_FP MINT_ADD_R8
#define MINT_SUB_FP MINT_SUB_R8
#define MINT_MUL_FP MINT_MUL_R8
#define MINT_DIV_FP MINT_DIV_R8
#define MINT_REM_FP MINT_REM_R8

#define MINT_CNE_FP MINT_CNE_R8
#define MINT_CEQ_FP MINT_CEQ_R8
#define MINT_CGT_FP MINT_CGT_R8
#define MINT_CGE_FP MINT_CGE_R8
#define MINT_CLT_FP MINT_CLT_R8
#define MINT_CLE_FP MINT_CLE_R8

#define MINT_CONV_OVF_U4_P MINT_CONV_OVF_U4_I8
#else

#define MINT_NEG_P MINT_NEG_I4
#define MINT_NOT_P MINT_NOT_I4

#define MINT_NEG_FP MINT_NEG_R4

#define MINT_ADD_P MINT_ADD_I4
#define MINT_ADD_P_IMM MINT_ADD_I4_IMM
#define MINT_SUB_P MINT_SUB_I4
#define MINT_MUL_P MINT_MUL_I4
#define MINT_DIV_P MINT_DIV_I4
#define MINT_DIV_UN_P MINT_DIV_UN_I4
#define MINT_REM_P MINT_REM_I4
#define MINT_REM_UN_P MINT_REM_UN_I4
#define MINT_AND_P MINT_AND_I4
#define MINT_OR_P MINT_OR_I4
#define MINT_XOR_P MINT_XOR_I4
#define MINT_SHL_P MINT_SHL_I4
#define MINT_SHR_P MINT_SHR_I4
#define MINT_SHR_UN_P MINT_SHR_UN_I4

#define MINT_CEQ_P MINT_CEQ_I4
#define MINT_CNE_P MINT_CNE_I4
#define MINT_CLT_P MINT_CLT_I4
#define MINT_CLT_UN_P MINT_CLT_UN_I4
#define MINT_CGT_P MINT_CGT_I4
#define MINT_CGT_UN_P MINT_CGT_UN_I4
#define MINT_CLE_P MINT_CLE_I4
#define MINT_CLE_UN_P MINT_CLE_UN_I4
#define MINT_CGE_P MINT_CGE_I4
#define MINT_CGE_UN_P MINT_CGE_UN_I4

#define MINT_ADD_FP MINT_ADD_R4
#define MINT_SUB_FP MINT_SUB_R4
#define MINT_MUL_FP MINT_MUL_R4
#define MINT_DIV_FP MINT_DIV_R4
#define MINT_REM_FP MINT_REM_R4

#define MINT_CNE_FP MINT_CNE_R4
#define MINT_CEQ_FP MINT_CEQ_R4
#define MINT_CGT_FP MINT_CGT_R4
#define MINT_CGE_FP MINT_CGE_R4
#define MINT_CLT_FP MINT_CLT_R4
#define MINT_CLE_FP MINT_CLE_R4

#define MINT_CONV_OVF_U4_P MINT_CONV_OVF_U4_I4
#endif

#if SIZEOF_VOID_P == 8
#define MINT_MOV_P MINT_MOV_8
#define MINT_LDNULL MINT_LDC_I8_0
#define MINT_LDIND_I MINT_LDIND_I8
#define MINT_STIND_I MINT_STIND_I8
#define MINT_LDELEM_I MINT_LDELEM_I8
#define MINT_STELEM_I MINT_STELEM_I8
#define MINT_MUL_P_IMM MINT_MUL_I8_IMM
#define MINT_ADD_MUL_P_IMM MINT_ADD_MUL_I8_IMM
#else
#define MINT_MOV_P MINT_MOV_4
#define MINT_LDNULL MINT_LDC_I4_0
#define MINT_LDIND_I MINT_LDIND_I4
#define MINT_STIND_I MINT_STIND_I4
#define MINT_LDELEM_I MINT_LDELEM_I4
#define MINT_STELEM_I MINT_STELEM_I4
#define MINT_MUL_P_IMM MINT_MUL_I4_IMM
#define MINT_ADD_MUL_P_IMM MINT_ADD_MUL_I4_IMM
#endif

#define MINT_SWITCH_LEN(n) (4 + (n) * 2)

#define MINT_IS_NOP(op) ((op) == MINT_NOP || (op) == MINT_DEF || (op) == MINT_DUMMY_USE || (op) == MINT_IL_SEQ_POINT)
#define MINT_IS_MOV(op) ((op) >= MINT_MOV_I4_I1 && (op) <= MINT_MOV_VT)
#define MINT_IS_UNCONDITIONAL_BRANCH(op) ((op) >= MINT_BR && (op) <= MINT_CALL_HANDLER_S)
#define MINT_IS_CONDITIONAL_BRANCH(op) ((op) >= MINT_BRFALSE_I4 && (op) <= MINT_BLT_UN_R8_S)
#define MINT_IS_UNOP_CONDITIONAL_BRANCH(op) ((op) >= MINT_BRFALSE_I4 && (op) <= MINT_BRTRUE_I8_S)
#define MINT_IS_BINOP_CONDITIONAL_BRANCH(op) ((op) >= MINT_BEQ_I4 && (op) <= MINT_BLT_UN_R8_S)
#define MINT_IS_COMPARE(op) ((op) >= MINT_CEQ_I4 && (op) <= MINT_CLT_UN_R8)
#define MINT_IS_SUPER_BRANCH(op) ((op) >= MINT_BRFALSE_I4_SP && (op) <= MINT_BLT_UN_I8_IMM_SP)
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
#define MINT_IS_LDIND(op) ((op) >= MINT_LDIND_I1 && (op) <= MINT_LDIND_R8)
#define MINT_IS_STIND_INT(op) ((op) >= MINT_STIND_I1 && (op) <= MINT_STIND_I8)
#define MINT_IS_STIND(op) ((op) >= MINT_STIND_I1 && (op) <= MINT_STIND_REF)
#define MINT_IS_LDIND_OFFSET(op) ((op) >= MINT_LDIND_OFFSET_I1 && (op) <= MINT_LDIND_OFFSET_I8)
#define MINT_IS_SIMD_CREATE(op) ((op) >= MINT_SIMD_V128_I1_CREATE && (op) <= MINT_SIMD_V128_I8_CREATE)

// TODO Add more
#define MINT_NO_SIDE_EFFECTS(op) (MINT_IS_MOV (op) || MINT_IS_LDC_I4 (op) || MINT_IS_LDC_I8 (op) || op == MINT_LDPTR)

#define MINT_CALL_ARGS 2
#define MINT_CALL_ARGS_SREG -2

#define MINT_MOV_PAIRS_MAX 4

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
