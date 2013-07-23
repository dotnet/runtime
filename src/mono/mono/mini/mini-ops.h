/*
 * Copyright 2003 Ximian, Inc
 * Copyright 2003-2011 Novell Inc
 * Copyright 2011 Xamarin Inc
 */
MINI_OP(OP_LOAD,	"load", NONE, NONE, NONE)
MINI_OP(OP_LDADDR,	"ldaddr", IREG, NONE, NONE)
MINI_OP(OP_STORE,	"store", NONE, NONE, NONE)
MINI_OP(OP_NOP,     "nop", NONE, NONE, NONE)
MINI_OP(OP_HARD_NOP,    "hard_nop", NONE, NONE, NONE)
MINI_OP(OP_RELAXED_NOP,     "relaxed_nop", NONE, NONE, NONE)
MINI_OP(OP_PHI,		"phi", IREG, NONE, NONE)
MINI_OP(OP_FPHI,	"fphi", FREG, NONE, NONE)
MINI_OP(OP_VPHI,	"vphi", VREG, NONE, NONE)
MINI_OP(OP_COMPARE,	"compare", NONE, IREG, IREG)
MINI_OP(OP_COMPARE_IMM,	"compare_imm", NONE, IREG, NONE)
MINI_OP(OP_FCOMPARE,	"fcompare", NONE, FREG, FREG)
MINI_OP(OP_LCOMPARE,	"lcompare", NONE, LREG, LREG)
MINI_OP(OP_ICOMPARE,	"icompare", NONE, IREG, IREG)
MINI_OP(OP_ICOMPARE_IMM,	"icompare_imm", NONE, IREG, NONE)
MINI_OP(OP_LCOMPARE_IMM,	"lcompare_imm", NONE, LREG, NONE)
MINI_OP(OP_LOCAL,	"local", NONE, NONE, NONE)
MINI_OP(OP_ARG,		"arg", NONE, NONE, NONE)
/* inst_imm contains the local index */
MINI_OP(OP_GSHAREDVT_LOCAL, "gsharedvt_local", NONE, NONE, NONE)
MINI_OP(OP_GSHAREDVT_ARG_REGOFFSET, "gsharedvt_arg_regoffset", NONE, NONE, NONE)
/*
 * Represents passing a valuetype argument which has not been decomposed yet.
 * inst_p0 points to the call.
 */
MINI_OP(OP_OUTARG_VT,	"outarg_vt", NONE, VREG, NONE)
MINI_OP(OP_OUTARG_VTRETADDR, "outarg_vtretaddr", IREG, NONE, NONE)
MINI_OP(OP_SETRET,	"setret", NONE, IREG, NONE)
MINI_OP(OP_SETFRET,	"setfret", FREG, FREG, NONE)
MINI_OP(OP_SETLRET,	"setlret", NONE, IREG, IREG)
MINI_OP(OP_LOCALLOC, "localloc", IREG, IREG, NONE)
MINI_OP(OP_LOCALLOC_IMM, "localloc_imm", IREG, NONE, NONE)
MINI_OP(OP_CHECK_THIS,	"checkthis", NONE, IREG, NONE)
MINI_OP(OP_SEQ_POINT, "seq_point", NONE, NONE, NONE)
MINI_OP(OP_IMPLICIT_EXCEPTION, "implicit_exception", NONE, NONE, NONE)

MINI_OP(OP_VOIDCALL,	"voidcall", NONE, NONE, NONE)
MINI_OP(OP_VOIDCALLVIRT,	"voidcallvirt", NONE, NONE, NONE)
MINI_OP(OP_VOIDCALL_REG,	"voidcall_reg", NONE, IREG, NONE)
MINI_OP(OP_VOIDCALL_MEMBASE,	"voidcall_membase", NONE, IREG, NONE)
MINI_OP(OP_CALL,        "call", IREG, NONE, NONE)
MINI_OP(OP_CALL_REG,	"call_reg", IREG, IREG, NONE)
MINI_OP(OP_CALL_MEMBASE,	"call_membase", IREG, IREG, NONE)
MINI_OP(OP_CALLVIRT, "callvirt", IREG, NONE, NONE)
MINI_OP(OP_FCALL,	"fcall", FREG, NONE, NONE)
MINI_OP(OP_FCALLVIRT,	"fcallvirt", FREG, NONE, NONE)
MINI_OP(OP_FCALL_REG,	"fcall_reg", FREG, IREG, NONE)
MINI_OP(OP_FCALL_MEMBASE,	"fcall_membase", FREG, IREG, NONE)
MINI_OP(OP_LCALL,	"lcall", LREG, NONE, NONE)
MINI_OP(OP_LCALLVIRT,	"lcallvirt", LREG, NONE, NONE)
MINI_OP(OP_LCALL_REG,	"lcall_reg", LREG, IREG, NONE)
MINI_OP(OP_LCALL_MEMBASE,	"lcall_membase", LREG, IREG, NONE)
MINI_OP(OP_VCALL, 	"vcall", VREG, NONE, NONE)
MINI_OP(OP_VCALLVIRT, 	"vcallvirt", VREG, NONE, NONE)
MINI_OP(OP_VCALL_REG,	"vcall_reg", VREG, IREG, NONE)
MINI_OP(OP_VCALL_MEMBASE,	"vcall_membase", VREG, IREG, NONE)
/* Represents the decomposed vcall which doesn't return a vtype no more */
MINI_OP(OP_VCALL2, 	"vcall2", NONE, NONE, NONE)
MINI_OP(OP_VCALL2_REG,	"vcall2_reg", NONE, IREG, NONE)
MINI_OP(OP_VCALL2_MEMBASE,	"vcall2_membase", NONE, IREG, NONE)
MINI_OP(OP_DYN_CALL, "dyn_call", NONE, IREG, IREG)

MINI_OP(OP_ICONST,	"iconst", IREG, NONE, NONE)
MINI_OP(OP_I8CONST,	"i8const", LREG, NONE, NONE)
MINI_OP(OP_R4CONST,	"r4const", FREG, NONE, NONE)
MINI_OP(OP_R8CONST,	"r8const", FREG, NONE, NONE)
MINI_OP(OP_REGVAR,	"regvar", NONE, NONE, NONE)
MINI_OP(OP_REGOFFSET,	"regoffset", NONE, NONE, NONE)
MINI_OP(OP_VTARG_ADDR,	"vtarg_addr", NONE, NONE, NONE)
MINI_OP(OP_LABEL,	"label", NONE, NONE, NONE)
MINI_OP(OP_SWITCH,  "switch", NONE, IREG, NONE)
MINI_OP(OP_THROW, "throw", NONE, IREG, NONE)
MINI_OP(OP_RETHROW,	"rethrow", NONE, IREG, NONE)

/*
 * Vararg calls are implemented as follows:
 * - the caller emits a hidden argument just before the varargs argument. this
 *   'signature cookie' argument contains the signature describing the the call.
 * - all implicit arguments are passed in memory right after the signature cookie, i.e.
 *   the stack will look like this:
 *   <argn>
 *   ..
 *   <arg1>
 *   <sig cookie>
 * - the OP_ARGLIST opcode in the callee computes the address of the sig cookie argument
 *   on the stack and saves it into its sreg1.
 * - mono_ArgIterator_Setup receives this value and uses it to find the signature and
 *   the arguments.
 */
MINI_OP(OP_ARGLIST,	"oparglist", NONE, IREG, NONE)

/* MONO_IS_STORE_MEMBASE depends on the order here */
MINI_OP(OP_STORE_MEMBASE_REG,"store_membase_reg", IREG, IREG, NONE)
MINI_OP(OP_STOREI1_MEMBASE_REG, "storei1_membase_reg", IREG, IREG, NONE)
MINI_OP(OP_STOREI2_MEMBASE_REG, "storei2_membase_reg", IREG, IREG, NONE)
MINI_OP(OP_STOREI4_MEMBASE_REG, "storei4_membase_reg", IREG, IREG, NONE)
MINI_OP(OP_STOREI8_MEMBASE_REG, "storei8_membase_reg", IREG, LREG, NONE)
MINI_OP(OP_STORER4_MEMBASE_REG, "storer4_membase_reg", IREG, FREG, NONE)
MINI_OP(OP_STORER8_MEMBASE_REG, "storer8_membase_reg", IREG, FREG, NONE)

#if defined(TARGET_X86) || defined(TARGET_AMD64)
MINI_OP(OP_STOREX_MEMBASE_REG, "storex_membase_reg", IREG, XREG, NONE)
MINI_OP(OP_STOREX_ALIGNED_MEMBASE_REG,     "storex_aligned_membase_reg", IREG, XREG, NONE)
MINI_OP(OP_STOREX_NTA_MEMBASE_REG,     "storex_nta_membase_reg", IREG, XREG, NONE)
#endif

MINI_OP(OP_STORE_MEMBASE_IMM,"store_membase_imm", IREG, NONE, NONE)
MINI_OP(OP_STOREI1_MEMBASE_IMM, "storei1_membase_imm", IREG, NONE, NONE)
MINI_OP(OP_STOREI2_MEMBASE_IMM, "storei2_membase_imm", IREG, NONE, NONE)
MINI_OP(OP_STOREI4_MEMBASE_IMM, "storei4_membase_imm", IREG, NONE, NONE)
MINI_OP(OP_STOREI8_MEMBASE_IMM, "storei8_membase_imm", IREG, NONE, NONE)
MINI_OP(OP_STOREX_MEMBASE,      	"storex_membase", IREG, XREG, NONE)
MINI_OP(OP_STOREV_MEMBASE,      "storev_membase", IREG, VREG, NONE)

/* MONO_IS_LOAD_MEMBASE depends on the order here */
MINI_OP(OP_LOAD_MEMBASE,	"load_membase", IREG, IREG, NONE)
MINI_OP(OP_LOADI1_MEMBASE,"loadi1_membase", IREG, IREG, NONE)
MINI_OP(OP_LOADU1_MEMBASE,"loadu1_membase", IREG, IREG, NONE)
MINI_OP(OP_LOADI2_MEMBASE,"loadi2_membase", IREG, IREG, NONE)
MINI_OP(OP_LOADU2_MEMBASE,"loadu2_membase", IREG, IREG, NONE)
MINI_OP(OP_LOADI4_MEMBASE,"loadi4_membase", IREG, IREG, NONE)
MINI_OP(OP_LOADU4_MEMBASE,"loadu4_membase", IREG, IREG, NONE)
MINI_OP(OP_LOADI8_MEMBASE,"loadi8_membase", LREG, IREG, NONE)
MINI_OP(OP_LOADR4_MEMBASE,"loadr4_membase", FREG, IREG, NONE)
MINI_OP(OP_LOADR8_MEMBASE,"loadr8_membase", FREG, IREG, NONE)

MINI_OP(OP_LOADX_MEMBASE, 			"loadx_membase", XREG, IREG, NONE)

#if defined(TARGET_X86) || defined(TARGET_AMD64)
MINI_OP(OP_LOADX_ALIGNED_MEMBASE,  "loadx_aligned_membase", XREG, IREG, NONE)
#endif

MINI_OP(OP_LOADV_MEMBASE,   "loadv_membase", VREG, IREG, NONE)

/* indexed loads: dreg = load at (sreg1 + sreg2)*/
MINI_OP(OP_LOAD_MEMINDEX,  "load_memindex", IREG, IREG, IREG)
MINI_OP(OP_LOADI1_MEMINDEX,"loadi1_memindex", IREG, IREG, IREG)
MINI_OP(OP_LOADU1_MEMINDEX,"loadu1_memindex", IREG, IREG, IREG)
MINI_OP(OP_LOADI2_MEMINDEX,"loadi2_memindex", IREG, IREG, IREG)
MINI_OP(OP_LOADU2_MEMINDEX,"loadu2_memindex", IREG, IREG, IREG)
MINI_OP(OP_LOADI4_MEMINDEX,"loadi4_memindex", IREG, IREG, IREG)
MINI_OP(OP_LOADU4_MEMINDEX,"loadu4_memindex", IREG, IREG, IREG)
MINI_OP(OP_LOADI8_MEMINDEX,"loadi8_memindex", IREG, IREG, IREG)
MINI_OP(OP_LOADR4_MEMINDEX,"loadr4_memindex", FREG, IREG, IREG)
MINI_OP(OP_LOADR8_MEMINDEX,"loadr8_memindex", FREG, IREG, IREG)
/* indexed stores: store sreg1 at (destbasereg + sreg2) */
/* MONO_IS_STORE_MEMINDEX depends on the order here */
MINI_OP(OP_STORE_MEMINDEX,"store_memindex", IREG, IREG, IREG)
MINI_OP(OP_STOREI1_MEMINDEX,"storei1_memindex", IREG, IREG, IREG)
MINI_OP(OP_STOREI2_MEMINDEX,"storei2_memindex", IREG, IREG, IREG)
MINI_OP(OP_STOREI4_MEMINDEX,"storei4_memindex", IREG, IREG, IREG)
MINI_OP(OP_STOREI8_MEMINDEX,"storei8_memindex", IREG, IREG, IREG)
MINI_OP(OP_STORER4_MEMINDEX,"storer4_memindex", IREG, FREG, IREG)
MINI_OP(OP_STORER8_MEMINDEX,"storer8_memindex", IREG, FREG, IREG)

MINI_OP(OP_LOAD_MEM,"load_mem", IREG, NONE, NONE)
MINI_OP(OP_LOADU1_MEM,"loadu1_mem", IREG, NONE, NONE)
MINI_OP(OP_LOADU2_MEM,"loadu2_mem", IREG, NONE, NONE)
MINI_OP(OP_LOADI4_MEM,"loadi4_mem", IREG, NONE, NONE)
MINI_OP(OP_LOADU4_MEM,"loadu4_mem", IREG, NONE, NONE)
MINI_OP(OP_LOADI8_MEM,"loadi8_mem", IREG, NONE, NONE)
MINI_OP(OP_STORE_MEM_IMM, "store_mem_imm", NONE, NONE, NONE)

MINI_OP(OP_MOVE,	"move", IREG, IREG, NONE)
MINI_OP(OP_LMOVE,	"lmove", IREG, IREG, NONE)
MINI_OP(OP_FMOVE,	"fmove", FREG, FREG, NONE)
MINI_OP(OP_VMOVE,   "vmove", VREG, VREG, NONE)

MINI_OP(OP_VZERO,   "vzero", VREG, NONE, NONE)

MINI_OP(OP_ADD_IMM,    "add_imm", IREG, IREG, NONE)
MINI_OP(OP_SUB_IMM,    "sub_imm", IREG, IREG, NONE)
MINI_OP(OP_MUL_IMM,    "mul_imm", IREG, IREG, NONE)
MINI_OP(OP_DIV_IMM,    "div_imm", IREG, IREG, NONE)
MINI_OP(OP_DIV_UN_IMM, "div_un_imm", IREG, IREG, NONE)
MINI_OP(OP_REM_IMM,    "rem_imm", IREG, IREG, NONE)
MINI_OP(OP_REM_UN_IMM, "rem_un_imm", IREG, IREG, NONE)
MINI_OP(OP_AND_IMM,    "and_imm", IREG, IREG, NONE)
MINI_OP(OP_OR_IMM,     "or_imm", IREG, IREG, NONE)
MINI_OP(OP_XOR_IMM,    "xor_imm", IREG, IREG, NONE)
MINI_OP(OP_SHL_IMM,    "shl_imm", IREG, IREG, NONE)
MINI_OP(OP_SHR_IMM,    "shr_imm", IREG, IREG, NONE)
MINI_OP(OP_SHR_UN_IMM, "shr_un_imm", IREG, IREG, NONE)

MINI_OP(OP_BR,         "br", NONE, NONE, NONE)
MINI_OP(OP_JMP,        "jmp", NONE, NONE, NONE)
/* Same as OP_JMP, but the passing of arguments is done similarly to calls */
MINI_OP(OP_TAILCALL,   "tailcall", NONE, NONE, NONE)
MINI_OP(OP_BREAK,      "break", NONE, NONE, NONE)

MINI_OP(OP_CEQ,   "ceq", IREG, NONE, NONE)
MINI_OP(OP_CGT,   "cgt", IREG, NONE, NONE)
MINI_OP(OP_CGT_UN,"cgt.un", IREG, NONE, NONE)
MINI_OP(OP_CLT,   "clt", IREG, NONE, NONE)
MINI_OP(OP_CLT_UN,"clt.un", IREG, NONE, NONE)

/* exceptions: must be in the same order as the matching CEE_ branch opcodes */
MINI_OP(OP_COND_EXC_EQ, "cond_exc_eq", NONE, NONE, NONE)
MINI_OP(OP_COND_EXC_GE, "cond_exc_ge", NONE, NONE, NONE)
MINI_OP(OP_COND_EXC_GT, "cond_exc_gt", NONE, NONE, NONE)
MINI_OP(OP_COND_EXC_LE, "cond_exc_le", NONE, NONE, NONE)
MINI_OP(OP_COND_EXC_LT, "cond_exc_lt", NONE, NONE, NONE)
MINI_OP(OP_COND_EXC_NE_UN, "cond_exc_ne_un", NONE, NONE, NONE)
MINI_OP(OP_COND_EXC_GE_UN, "cond_exc_ge_un", NONE, NONE, NONE)
MINI_OP(OP_COND_EXC_GT_UN, "cond_exc_gt_un", NONE, NONE, NONE)
MINI_OP(OP_COND_EXC_LE_UN, "cond_exc_le_un", NONE, NONE, NONE)
MINI_OP(OP_COND_EXC_LT_UN, "cond_exc_lt_un", NONE, NONE, NONE)

MINI_OP(OP_COND_EXC_OV, "cond_exc_ov", NONE, NONE, NONE)
MINI_OP(OP_COND_EXC_NO, "cond_exc_no", NONE, NONE, NONE)
MINI_OP(OP_COND_EXC_C, "cond_exc_c", NONE, NONE, NONE)
MINI_OP(OP_COND_EXC_NC, "cond_exc_nc", NONE, NONE, NONE)

MINI_OP(OP_COND_EXC_IEQ, "cond_exc_ieq", NONE, NONE, NONE)
MINI_OP(OP_COND_EXC_IGE, "cond_exc_ige", NONE, NONE, NONE)
MINI_OP(OP_COND_EXC_IGT, "cond_exc_igt", NONE, NONE, NONE)
MINI_OP(OP_COND_EXC_ILE, "cond_exc_ile", NONE, NONE, NONE)
MINI_OP(OP_COND_EXC_ILT, "cond_exc_ilt", NONE, NONE, NONE)
MINI_OP(OP_COND_EXC_INE_UN, "cond_exc_ine_un", NONE, NONE, NONE)
MINI_OP(OP_COND_EXC_IGE_UN, "cond_exc_ige_un", NONE, NONE, NONE)
MINI_OP(OP_COND_EXC_IGT_UN, "cond_exc_igt_un", NONE, NONE, NONE)
MINI_OP(OP_COND_EXC_ILE_UN, "cond_exc_ile_un", NONE, NONE, NONE)
MINI_OP(OP_COND_EXC_ILT_UN, "cond_exc_ilt_un", NONE, NONE, NONE)

MINI_OP(OP_COND_EXC_IOV, "cond_exc_iov", NONE, NONE, NONE)
MINI_OP(OP_COND_EXC_INO, "cond_exc_ino", NONE, NONE, NONE)
MINI_OP(OP_COND_EXC_IC, "cond_exc_ic", NONE, NONE, NONE)
MINI_OP(OP_COND_EXC_INC, "cond_exc_inc", NONE, NONE, NONE)

/* 64 bit opcodes: must be in the same order as the matching CEE_ opcodes: binops_op_map */
MINI_OP(OP_LADD,    "long_add", LREG, LREG, LREG)
MINI_OP(OP_LSUB,    "long_sub", LREG, LREG, LREG)
MINI_OP(OP_LMUL,    "long_mul", LREG, LREG, LREG)
MINI_OP(OP_LDIV,    "long_div", LREG, LREG, LREG)
MINI_OP(OP_LDIV_UN, "long_div_un", LREG, LREG, LREG)
MINI_OP(OP_LREM,    "long_rem", LREG, LREG, LREG)
MINI_OP(OP_LREM_UN, "long_rem_un", LREG, LREG, LREG)
MINI_OP(OP_LAND,    "long_and", LREG, LREG, LREG)
MINI_OP(OP_LOR,     "long_or", LREG, LREG, LREG)
MINI_OP(OP_LXOR,    "long_xor", LREG, LREG, LREG)
MINI_OP(OP_LSHL,    "long_shl", LREG, LREG, IREG)
MINI_OP(OP_LSHR,    "long_shr", LREG, LREG, IREG)
MINI_OP(OP_LSHR_UN, "long_shr_un", LREG, LREG, IREG)

/* 64 bit opcodes: must be in the same order as the matching CEE_ opcodes: unops_op_map */
MINI_OP(OP_LNEG,       "long_neg", LREG, LREG, NONE)
MINI_OP(OP_LNOT,       "long_not", LREG, LREG, NONE)
MINI_OP(OP_LCONV_TO_I1,"long_conv_to_i1", IREG, LREG, NONE)
MINI_OP(OP_LCONV_TO_I2,"long_conv_to_i2", IREG, LREG, NONE)
MINI_OP(OP_LCONV_TO_I4,"long_conv_to_i4", IREG, LREG, NONE)
MINI_OP(OP_LCONV_TO_I8,"long_conv_to_i8", LREG, LREG, NONE)
MINI_OP(OP_LCONV_TO_R4,"long_conv_to_r4", FREG, LREG, NONE)
MINI_OP(OP_LCONV_TO_R8,"long_conv_to_r8", FREG, LREG, NONE)
MINI_OP(OP_LCONV_TO_U4,"long_conv_to_u4", IREG, LREG, NONE)
MINI_OP(OP_LCONV_TO_U8,"long_conv_to_u8", LREG, LREG, NONE)

MINI_OP(OP_LCONV_TO_U2,   "long_conv_to_u2", IREG, LREG, NONE)
MINI_OP(OP_LCONV_TO_U1,   "long_conv_to_u1", IREG, LREG, NONE)
MINI_OP(OP_LCONV_TO_I,    "long_conv_to_i", LREG, LREG, NONE)
MINI_OP(OP_LCONV_TO_OVF_I,"long_conv_to_ovf_i", LREG, LREG, NONE)
MINI_OP(OP_LCONV_TO_OVF_U,"long_conv_to_ovf_u", LREG, LREG, NONE)

MINI_OP(OP_LADD_OVF,      "long_add_ovf", LREG, LREG, LREG)
MINI_OP(OP_LADD_OVF_UN,   "long_add_ovf_un", LREG, LREG, LREG)
MINI_OP(OP_LMUL_OVF,      "long_mul_ovf", LREG, LREG, LREG)
MINI_OP(OP_LMUL_OVF_UN,   "long_mul_ovf_un", LREG, LREG, LREG)
MINI_OP(OP_LSUB_OVF,      "long_sub_ovf", LREG, LREG, LREG)
MINI_OP(OP_LSUB_OVF_UN,   "long_sub_ovf_un", LREG, LREG, LREG)

MINI_OP(OP_LCONV_TO_OVF_I1_UN,"long_conv_to_ovf_i1_un", LREG, LREG, NONE)
MINI_OP(OP_LCONV_TO_OVF_I2_UN,"long_conv_to_ovf_i2_un", LREG, LREG, NONE)
MINI_OP(OP_LCONV_TO_OVF_I4_UN,"long_conv_to_ovf_i4_un", LREG, LREG, NONE)
MINI_OP(OP_LCONV_TO_OVF_I8_UN,"long_conv_to_ovf_i8_un", LREG, LREG, NONE)
MINI_OP(OP_LCONV_TO_OVF_U1_UN,"long_conv_to_ovf_u1_un", LREG, LREG, NONE)
MINI_OP(OP_LCONV_TO_OVF_U2_UN,"long_conv_to_ovf_u2_un", LREG, LREG, NONE)
MINI_OP(OP_LCONV_TO_OVF_U4_UN,"long_conv_to_ovf_u4_un", LREG, LREG, NONE)
MINI_OP(OP_LCONV_TO_OVF_U8_UN,"long_conv_to_ovf_u8_un", LREG, LREG, NONE)
MINI_OP(OP_LCONV_TO_OVF_I_UN, "long_conv_to_ovf_i_un", LREG, LREG, NONE)
MINI_OP(OP_LCONV_TO_OVF_U_UN, "long_conv_to_ovf_u_un", LREG, LREG, NONE)

MINI_OP(OP_LCONV_TO_OVF_I1,"long_conv_to_ovf_i1", LREG, LREG, NONE)
MINI_OP(OP_LCONV_TO_OVF_U1,"long_conv_to_ovf_u1", LREG, LREG, NONE)
MINI_OP(OP_LCONV_TO_OVF_I2,"long_conv_to_ovf_i2", LREG, LREG, NONE)
MINI_OP(OP_LCONV_TO_OVF_U2,"long_conv_to_ovf_u2", LREG, LREG, NONE)
MINI_OP(OP_LCONV_TO_OVF_I4,"long_conv_to_ovf_i4", LREG, LREG, NONE)
MINI_OP(OP_LCONV_TO_OVF_U4,"long_conv_to_ovf_u4", LREG, LREG, NONE)
MINI_OP(OP_LCONV_TO_OVF_I8,"long_conv_to_ovf_i8", LREG, LREG, NONE)
MINI_OP(OP_LCONV_TO_OVF_U8,"long_conv_to_ovf_u8", LREG, LREG, NONE)

/* mono_decompose_long_opts () depends on the order here */
MINI_OP(OP_LCEQ,   "long_ceq", LREG, NONE, NONE)
MINI_OP(OP_LCGT,   "long_cgt", LREG, NONE, NONE)
MINI_OP(OP_LCGT_UN,"long_cgt_un", LREG, NONE, NONE)
MINI_OP(OP_LCLT,   "long_clt", LREG, NONE, NONE)
MINI_OP(OP_LCLT_UN,"long_clt_un", LREG, NONE, NONE)

MINI_OP(OP_LCONV_TO_R_UN,"long_conv_to_r_un", FREG, LREG, NONE)
MINI_OP(OP_LCONV_TO_U,   "long_conv_to_u", IREG, LREG, NONE)

MINI_OP(OP_LADD_IMM,    "long_add_imm", LREG, LREG, NONE)
MINI_OP(OP_LSUB_IMM,    "long_sub_imm", LREG, LREG, NONE)
MINI_OP(OP_LMUL_IMM,    "long_mul_imm", LREG, LREG, NONE)
MINI_OP(OP_LAND_IMM,    "long_and_imm", LREG, LREG, NONE)
MINI_OP(OP_LOR_IMM,     "long_or_imm", LREG, LREG, NONE)
MINI_OP(OP_LXOR_IMM,    "long_xor_imm", LREG, LREG, NONE)
MINI_OP(OP_LSHL_IMM,    "long_shl_imm", LREG, LREG, NONE)
MINI_OP(OP_LSHR_IMM,    "long_shr_imm", LREG, LREG, NONE)
MINI_OP(OP_LSHR_UN_IMM, "long_shr_un_imm", LREG, LREG, NONE)
MINI_OP(OP_LDIV_IMM,    "long_div_imm", LREG, LREG, NONE)
MINI_OP(OP_LDIV_UN_IMM, "long_div_un_imm", LREG, LREG, NONE)
MINI_OP(OP_LREM_IMM,    "long_rem_imm", LREG, LREG, NONE)
MINI_OP(OP_LREM_UN_IMM, "long_rem_un_imm", LREG, LREG, NONE)

/* mono_decompose_long_opts () depends on the order here */
MINI_OP(OP_LBEQ,    "long_beq", NONE, NONE, NONE)
MINI_OP(OP_LBGE,    "long_bge", NONE, NONE, NONE)
MINI_OP(OP_LBGT,    "long_bgt", NONE, NONE, NONE)
MINI_OP(OP_LBLE,    "long_ble", NONE, NONE, NONE)
MINI_OP(OP_LBLT,    "long_blt", NONE, NONE, NONE)
MINI_OP(OP_LBNE_UN, "long_bne_un", NONE, NONE, NONE)
MINI_OP(OP_LBGE_UN, "long_bge_un", NONE, NONE, NONE)
MINI_OP(OP_LBGT_UN, "long_bgt_un", NONE, NONE, NONE)
MINI_OP(OP_LBLE_UN, "long_ble_un", NONE, NONE, NONE)
MINI_OP(OP_LBLT_UN, "long_blt_un", NONE, NONE, NONE)

/* Variants of the original opcodes which take the two parts of the long as two arguments */
MINI_OP(OP_LCONV_TO_R8_2,"long_conv_to_r8_2", FREG, IREG, IREG)
MINI_OP(OP_LCONV_TO_R4_2,"long_conv_to_r4_2", FREG, IREG, IREG)
MINI_OP(OP_LCONV_TO_R_UN_2,"long_conv_to_r_un_2", FREG, IREG, IREG)
MINI_OP(OP_LCONV_TO_OVF_I4_2,"long_conv_to_ovf_i4_2", IREG, IREG, IREG)

/* 32 bit opcodes: must be in the same order as the matching CEE_ opcodes: binops_op_map */
MINI_OP(OP_IADD,    "int_add", IREG, IREG, IREG)
MINI_OP(OP_ISUB,    "int_sub", IREG, IREG, IREG)
MINI_OP(OP_IMUL,    "int_mul", IREG, IREG, IREG)
MINI_OP(OP_IDIV,    "int_div", IREG, IREG, IREG)
MINI_OP(OP_IDIV_UN, "int_div_un", IREG, IREG, IREG)
MINI_OP(OP_IREM,    "int_rem", IREG, IREG, IREG)
MINI_OP(OP_IREM_UN, "int_rem_un", IREG, IREG, IREG)
MINI_OP(OP_IAND,    "int_and", IREG, IREG, IREG)
MINI_OP(OP_IOR,     "int_or", IREG, IREG, IREG)
MINI_OP(OP_IXOR,    "int_xor", IREG, IREG, IREG)
MINI_OP(OP_ISHL,    "int_shl", IREG, IREG, IREG)
MINI_OP(OP_ISHR,    "int_shr", IREG, IREG, IREG)
MINI_OP(OP_ISHR_UN, "int_shr_un", IREG, IREG, IREG)

/* 32 bit opcodes: must be in the same order as the matching CEE_ opcodes: unops_op_map */
MINI_OP(OP_INEG,       "int_neg", IREG, IREG, NONE)
MINI_OP(OP_INOT,       "int_not", IREG, IREG, NONE)
MINI_OP(OP_ICONV_TO_I1,"int_conv_to_i1", IREG, IREG, NONE)
MINI_OP(OP_ICONV_TO_I2,"int_conv_to_i2", IREG, IREG, NONE)
MINI_OP(OP_ICONV_TO_I4,"int_conv_to_i4", IREG, IREG, NONE)
MINI_OP(OP_ICONV_TO_I8,"int_conv_to_i8", IREG, IREG, NONE)
MINI_OP(OP_ICONV_TO_R4,"int_conv_to_r4", FREG, IREG, NONE)
MINI_OP(OP_ICONV_TO_R8,"int_conv_to_r8", FREG, IREG, NONE)
MINI_OP(OP_ICONV_TO_U4,"int_conv_to_u4", IREG, IREG, NONE)
MINI_OP(OP_ICONV_TO_U8,"int_conv_to_u8", LREG, IREG, NONE)

MINI_OP(OP_ICONV_TO_R_UN, "int_conv_to_r_un", FREG, IREG, NONE)
MINI_OP(OP_ICONV_TO_U,   "int_conv_to_u", IREG, IREG, NONE)

/* 32 bit opcodes: must be in the same order as the matching CEE_ opcodes: ovfops_op_map */
MINI_OP(OP_ICONV_TO_U2,   "int_conv_to_u2", IREG, IREG, NONE)
MINI_OP(OP_ICONV_TO_U1,   "int_conv_to_u1", IREG, IREG, NONE)
MINI_OP(OP_ICONV_TO_I,    "int_conv_to_i", IREG, IREG, NONE)
MINI_OP(OP_ICONV_TO_OVF_I,"int_conv_to_ovf_i", IREG, IREG, NONE)
MINI_OP(OP_ICONV_TO_OVF_U,"int_conv_to_ovf_u", IREG, IREG, NONE)
MINI_OP(OP_IADD_OVF,      "int_add_ovf", IREG, IREG, IREG)
MINI_OP(OP_IADD_OVF_UN,   "int_add_ovf_un", IREG, IREG, IREG)
MINI_OP(OP_IMUL_OVF,      "int_mul_ovf", IREG, IREG, IREG)
MINI_OP(OP_IMUL_OVF_UN,   "int_mul_ovf_un", IREG, IREG, IREG)
MINI_OP(OP_ISUB_OVF,      "int_sub_ovf", IREG, IREG, IREG)
MINI_OP(OP_ISUB_OVF_UN,   "int_sub_ovf_un", IREG, IREG, IREG)

/* 32 bit opcodes: must be in the same order as the matching CEE_ opcodes: ovf2ops_op_map */
MINI_OP(OP_ICONV_TO_OVF_I1_UN,"int_conv_to_ovf_i1_un", IREG, IREG, NONE)
MINI_OP(OP_ICONV_TO_OVF_I2_UN,"int_conv_to_ovf_i2_un", IREG, IREG, NONE)
MINI_OP(OP_ICONV_TO_OVF_I4_UN,"int_conv_to_ovf_i4_un", IREG, IREG, NONE)
MINI_OP(OP_ICONV_TO_OVF_I8_UN,"int_conv_to_ovf_i8_un", IREG, IREG, NONE)
MINI_OP(OP_ICONV_TO_OVF_U1_UN,"int_conv_to_ovf_u1_un", IREG, IREG, NONE)
MINI_OP(OP_ICONV_TO_OVF_U2_UN,"int_conv_to_ovf_u2_un", IREG, IREG, NONE)
MINI_OP(OP_ICONV_TO_OVF_U4_UN,"int_conv_to_ovf_u4_un", IREG, IREG, NONE)
MINI_OP(OP_ICONV_TO_OVF_U8_UN,"int_conv_to_ovf_u8_un", IREG, IREG, NONE)
MINI_OP(OP_ICONV_TO_OVF_I_UN, "int_conv_to_ovf_i_un", IREG, IREG, NONE)
MINI_OP(OP_ICONV_TO_OVF_U_UN, "int_conv_to_ovf_u_un", IREG, IREG, NONE)

/* 32 bit opcodes: must be in the same order as the matching CEE_ opcodes: ovf3ops_op_map */
MINI_OP(OP_ICONV_TO_OVF_I1,"int_conv_to_ovf_i1", IREG, IREG, NONE)
MINI_OP(OP_ICONV_TO_OVF_U1,"int_conv_to_ovf_u1", IREG, IREG, NONE)
MINI_OP(OP_ICONV_TO_OVF_I2,"int_conv_to_ovf_i2", IREG, IREG, NONE)
MINI_OP(OP_ICONV_TO_OVF_U2,"int_conv_to_ovf_u2", IREG, IREG, NONE)
MINI_OP(OP_ICONV_TO_OVF_I4,"int_conv_to_ovf_i4", IREG, IREG, NONE)
MINI_OP(OP_ICONV_TO_OVF_U4,"int_conv_to_ovf_u4", IREG, IREG, NONE)
MINI_OP(OP_ICONV_TO_OVF_I8,"int_conv_to_ovf_i8", IREG, IREG, NONE)
MINI_OP(OP_ICONV_TO_OVF_U8,"int_conv_to_ovf_u8", IREG, IREG, NONE)

MINI_OP(OP_IADC,     "int_adc", IREG, IREG, IREG)
MINI_OP(OP_IADC_IMM, "int_adc_imm", IREG, IREG, NONE)
MINI_OP(OP_ISBB,     "int_sbb", IREG, IREG, IREG)
MINI_OP(OP_ISBB_IMM, "int_sbb_imm", IREG, IREG, NONE)
MINI_OP(OP_IADDCC,   "int_addcc", IREG, IREG, IREG)
MINI_OP(OP_ISUBCC,   "int_subcc", IREG, IREG, IREG)

MINI_OP(OP_IADD_IMM,    "int_add_imm", IREG, IREG, NONE)
MINI_OP(OP_ISUB_IMM,    "int_sub_imm", IREG, IREG, NONE)
MINI_OP(OP_IMUL_IMM,    "int_mul_imm", IREG, IREG, NONE)
MINI_OP(OP_IDIV_IMM,    "int_div_imm", IREG, IREG, NONE)
MINI_OP(OP_IDIV_UN_IMM, "int_div_un_imm", IREG, IREG, NONE)
MINI_OP(OP_IREM_IMM,    "int_rem_imm", IREG, IREG, NONE)
MINI_OP(OP_IREM_UN_IMM, "int_rem_un_imm", IREG, IREG, NONE)
MINI_OP(OP_IAND_IMM,    "int_and_imm", IREG, IREG, NONE)
MINI_OP(OP_IOR_IMM,     "int_or_imm", IREG, IREG, NONE)
MINI_OP(OP_IXOR_IMM,    "int_xor_imm", IREG, IREG, NONE)
MINI_OP(OP_ISHL_IMM,    "int_shl_imm", IREG, IREG, NONE)
MINI_OP(OP_ISHR_IMM,    "int_shr_imm", IREG, IREG, NONE)
MINI_OP(OP_ISHR_UN_IMM, "int_shr_un_imm", IREG, IREG, NONE)

MINI_OP(OP_ICEQ,   "int_ceq", IREG, NONE, NONE)
MINI_OP(OP_ICGT,   "int_cgt", IREG, NONE, NONE)
MINI_OP(OP_ICGT_UN,"int_cgt_un", IREG, NONE, NONE)
MINI_OP(OP_ICLT,   "int_clt", IREG, NONE, NONE)
MINI_OP(OP_ICLT_UN,"int_clt_un", IREG, NONE, NONE)

MINI_OP(OP_IBEQ,    "int_beq", NONE, NONE, NONE)
MINI_OP(OP_IBGE,    "int_bge", NONE, NONE, NONE)
MINI_OP(OP_IBGT,    "int_bgt", NONE, NONE, NONE)
MINI_OP(OP_IBLE,    "int_ble", NONE, NONE, NONE)
MINI_OP(OP_IBLT,    "int_blt", NONE, NONE, NONE)
MINI_OP(OP_IBNE_UN, "int_bne_un", NONE, NONE, NONE)
MINI_OP(OP_IBGE_UN, "int_bge_un", NONE, NONE, NONE)
MINI_OP(OP_IBGT_UN, "int_bgt_un", NONE, NONE, NONE)
MINI_OP(OP_IBLE_UN, "int_ble_un", NONE, NONE, NONE)
MINI_OP(OP_IBLT_UN, "int_blt_un", NONE, NONE, NONE)

MINI_OP(OP_FBEQ,    "float_beq", NONE, NONE, NONE)
MINI_OP(OP_FBGE,    "float_bge", NONE, NONE, NONE)
MINI_OP(OP_FBGT,    "float_bgt", NONE, NONE, NONE)
MINI_OP(OP_FBLE,    "float_ble", NONE, NONE, NONE)
MINI_OP(OP_FBLT,    "float_blt", NONE, NONE, NONE)
MINI_OP(OP_FBNE_UN, "float_bne_un", NONE, NONE, NONE)
MINI_OP(OP_FBGE_UN, "float_bge_un", NONE, NONE, NONE)
MINI_OP(OP_FBGT_UN, "float_bgt_un", NONE, NONE, NONE)
MINI_OP(OP_FBLE_UN, "float_ble_un", NONE, NONE, NONE)
MINI_OP(OP_FBLT_UN, "float_blt_un", NONE, NONE, NONE)

/* float opcodes: must be in the same order as the matching CEE_ opcodes: binops_op_map */
MINI_OP(OP_FADD,   "float_add", FREG, FREG, FREG)
MINI_OP(OP_FSUB,   "float_sub", FREG, FREG, FREG)
MINI_OP(OP_FMUL,   "float_mul", FREG, FREG, FREG)
MINI_OP(OP_FDIV,   "float_div", FREG, FREG, FREG)
MINI_OP(OP_FDIV_UN,"float_div_un", FREG, FREG, FREG)
MINI_OP(OP_FREM,   "float_rem", FREG, FREG, FREG)
MINI_OP(OP_FREM_UN,"float_rem_un", FREG, FREG, FREG)

/* float opcodes: must be in the same order as the matching CEE_ opcodes: unops_op_map */
MINI_OP(OP_FNEG,       "float_neg", FREG, FREG, NONE)
MINI_OP(OP_FNOT,       "float_not", FREG, FREG, NONE)
MINI_OP(OP_FCONV_TO_I1,"float_conv_to_i1", IREG, FREG, NONE)
MINI_OP(OP_FCONV_TO_I2,"float_conv_to_i2", IREG, FREG, NONE)
MINI_OP(OP_FCONV_TO_I4,"float_conv_to_i4", IREG, FREG, NONE)
MINI_OP(OP_FCONV_TO_I8,"float_conv_to_i8", LREG, FREG, NONE)
MINI_OP(OP_FCONV_TO_R4,"float_conv_to_r4", FREG, FREG, NONE)
MINI_OP(OP_FCONV_TO_R8,"float_conv_to_r8", FREG, FREG, NONE)
MINI_OP(OP_FCONV_TO_U4,"float_conv_to_u4", IREG, FREG, NONE)
MINI_OP(OP_FCONV_TO_U8,"float_conv_to_u8", LREG, FREG, NONE)

MINI_OP(OP_FCONV_TO_U2,   "float_conv_to_u2", IREG, FREG, NONE)
MINI_OP(OP_FCONV_TO_U1,   "float_conv_to_u1", IREG, FREG, NONE)
MINI_OP(OP_FCONV_TO_I,    "float_conv_to_i", IREG, FREG, NONE)
MINI_OP(OP_FCONV_TO_OVF_I,"float_conv_to_ovf_i", IREG, FREG, NONE)
MINI_OP(OP_FCONV_TO_OVF_U,"float_conv_to_ovd_u", IREG, FREG, NONE)

MINI_OP(OP_FADD_OVF,      "float_add_ovf", FREG, FREG, FREG)
MINI_OP(OP_FADD_OVF_UN,   "float_add_ovf_un", FREG, FREG, FREG)
MINI_OP(OP_FMUL_OVF,      "float_mul_ovf", FREG, FREG, FREG)
MINI_OP(OP_FMUL_OVF_UN,   "float_mul_ovf_un", FREG, FREG, FREG)
MINI_OP(OP_FSUB_OVF,      "float_sub_ovf", FREG, FREG, FREG)
MINI_OP(OP_FSUB_OVF_UN,   "float_sub_ovf_un", FREG, FREG, FREG)

MINI_OP(OP_FCONV_TO_OVF_I1_UN,"float_conv_to_ovf_i1_un", IREG, FREG, NONE)
MINI_OP(OP_FCONV_TO_OVF_I2_UN,"float_conv_to_ovf_i2_un", IREG, FREG, NONE)
MINI_OP(OP_FCONV_TO_OVF_I4_UN,"float_conv_to_ovf_i4_un", IREG, FREG, NONE)
MINI_OP(OP_FCONV_TO_OVF_I8_UN,"float_conv_to_ovf_i8_un", IREG, FREG, NONE)
MINI_OP(OP_FCONV_TO_OVF_U1_UN,"float_conv_to_ovf_u1_un", IREG, FREG, NONE)
MINI_OP(OP_FCONV_TO_OVF_U2_UN,"float_conv_to_ovf_u2_un", IREG, FREG, NONE)
MINI_OP(OP_FCONV_TO_OVF_U4_UN,"float_conv_to_ovf_u4_un", IREG, FREG, NONE)
MINI_OP(OP_FCONV_TO_OVF_U8_UN,"float_conv_to_ovf_u8_un", IREG, FREG, NONE)
MINI_OP(OP_FCONV_TO_OVF_I_UN, "float_conv_to_ovf_i_un", IREG, FREG, NONE)
MINI_OP(OP_FCONV_TO_OVF_U_UN, "float_conv_to_ovf_u_un", IREG, FREG, NONE)

MINI_OP(OP_FCONV_TO_OVF_I1,"float_conv_to_ovf_i1", IREG, FREG, NONE)
MINI_OP(OP_FCONV_TO_OVF_U1,"float_conv_to_ovf_u1", IREG, FREG, NONE)
MINI_OP(OP_FCONV_TO_OVF_I2,"float_conv_to_ovf_i2", IREG, FREG, NONE)
MINI_OP(OP_FCONV_TO_OVF_U2,"float_conv_to_ovf_u2", IREG, FREG, NONE)
MINI_OP(OP_FCONV_TO_OVF_I4,"float_conv_to_ovf_i4", IREG, FREG, NONE)
MINI_OP(OP_FCONV_TO_OVF_U4,"float_conv_to_ovf_u4", IREG, FREG, NONE)
MINI_OP(OP_FCONV_TO_OVF_I8,"float_conv_to_ovf_i8", IREG, FREG, NONE)
MINI_OP(OP_FCONV_TO_OVF_U8,"float_conv_to_ovf_u8", IREG, FREG, NONE)

/* These do the comparison too */
MINI_OP(OP_FCEQ,   "float_ceq", IREG, FREG, FREG)
MINI_OP(OP_FCGT,   "float_cgt", IREG, FREG, FREG)
MINI_OP(OP_FCGT_UN,"float_cgt_un", IREG, FREG, FREG)
MINI_OP(OP_FCLT,   "float_clt", IREG, FREG, FREG)
MINI_OP(OP_FCLT_UN,"float_clt_un", IREG, FREG, FREG)

MINI_OP(OP_FCEQ_MEMBASE,   "float_ceq_membase", IREG, FREG, IREG)
MINI_OP(OP_FCGT_MEMBASE,   "float_cgt_membase", IREG, FREG, IREG)
MINI_OP(OP_FCGT_UN_MEMBASE,"float_cgt_un_membase", IREG, FREG, IREG)
MINI_OP(OP_FCLT_MEMBASE,   "float_clt_membase", IREG, FREG, IREG)
MINI_OP(OP_FCLT_UN_MEMBASE,"float_clt_un_membase", IREG, FREG, IREG)

MINI_OP(OP_FCONV_TO_U,	"float_conv_to_u", IREG, FREG, NONE)
MINI_OP(OP_CKFINITE, "ckfinite", FREG, FREG, NONE)

/* Return the low 32 bits of a double vreg */
MINI_OP(OP_FGETLOW32, "float_getlow32", IREG, FREG, NONE)
/* Return the high 32 bits of a double vreg */
MINI_OP(OP_FGETHIGH32, "float_gethigh32", IREG, FREG, NONE)

MINI_OP(OP_JUMP_TABLE, "jump_table", IREG, NONE, NONE)

/* aot compiler */
MINI_OP(OP_AOTCONST, "aot_const", IREG, NONE, NONE)
MINI_OP(OP_PATCH_INFO, "patch_info", NONE, NONE, NONE)
MINI_OP(OP_GOT_ENTRY, "got_entry", IREG, IREG, NONE)

/* exception related opcodes */
MINI_OP(OP_CALL_HANDLER  , "call_handler", NONE, NONE, NONE)
MINI_OP(OP_START_HANDLER  , "start_handler", NONE, NONE, NONE)
MINI_OP(OP_ENDFILTER,  "endfilter", NONE, IREG, NONE)
MINI_OP(OP_ENDFINALLY,  "endfinally", NONE, NONE, NONE)

/* inline (long)int * (long)int */
MINI_OP(OP_BIGMUL, "bigmul", LREG, IREG, IREG)
MINI_OP(OP_BIGMUL_UN, "bigmul_un", LREG, IREG, IREG)
MINI_OP(OP_IMIN_UN, "int_min_un", IREG, IREG, IREG)
MINI_OP(OP_IMAX_UN, "int_max_un", IREG, IREG, IREG)
MINI_OP(OP_LMIN_UN, "long_min_un", LREG, LREG, LREG)
MINI_OP(OP_LMAX_UN, "long_max_un", LREG, LREG, LREG)

MINI_OP(OP_MIN, "min", IREG, IREG, IREG)
MINI_OP(OP_MAX, "max", IREG, IREG, IREG)

MINI_OP(OP_IMIN, "int_min", IREG, IREG, IREG)
MINI_OP(OP_IMAX, "int_max", IREG, IREG, IREG)
MINI_OP(OP_LMIN, "long_min", LREG, LREG, LREG)
MINI_OP(OP_LMAX, "long_max", LREG, LREG, LREG)

/* opcodes most architecture have */
MINI_OP(OP_ADC,     "adc", IREG, IREG, IREG)
MINI_OP(OP_ADC_IMM, "adc_imm", IREG, IREG, NONE)
MINI_OP(OP_SBB,     "sbb", IREG, IREG, IREG)
MINI_OP(OP_SBB_IMM, "sbb_imm", IREG, IREG, NONE)
MINI_OP(OP_ADDCC,   "addcc", IREG, IREG, IREG)
MINI_OP(OP_ADDCC_IMM,   "addcc_imm", IREG, IREG, NONE)
MINI_OP(OP_SUBCC,   "subcc", IREG, IREG, IREG)
MINI_OP(OP_SUBCC_IMM,   "subcc_imm", IREG, IREG, NONE)
MINI_OP(OP_BR_REG,  "br_reg", NONE, IREG, NONE)
MINI_OP(OP_SEXT_I1,  "sext_i1", IREG, IREG, NONE)
MINI_OP(OP_SEXT_I2,  "sext_i2", IREG, IREG, NONE)
MINI_OP(OP_SEXT_I4,  "sext_i4", LREG, IREG, NONE)
MINI_OP(OP_ZEXT_I1,  "zext_i1", IREG, IREG, NONE)
MINI_OP(OP_ZEXT_I2,  "zext_i2", IREG, IREG, NONE)
MINI_OP(OP_ZEXT_I4,  "zext_i4", LREG, IREG, NONE)
MINI_OP(OP_CNE,      "cne", NONE, NONE, NONE)
MINI_OP(OP_TRUNC_I4, "trunc_i4", IREG, LREG, NONE)
/* to implement the upper half of long32 add and sub */
MINI_OP(OP_ADD_OVF_CARRY,   "add_ovf_carry", IREG, IREG, IREG)
MINI_OP(OP_SUB_OVF_CARRY,   "sub_ovf_carry", IREG, IREG, IREG)
MINI_OP(OP_ADD_OVF_UN_CARRY,   "add_ovf_un_carry", IREG, IREG, IREG)
MINI_OP(OP_SUB_OVF_UN_CARRY,   "sub_ovf_un_carry", IREG, IREG, IREG)

/* instructions with explicit long arguments to deal with 64-bit ilp32 machines */
MINI_OP(OP_LADDCC,   "laddcc", LREG, LREG, LREG)
MINI_OP(OP_LSUBCC,   "lsubcc", LREG, LREG, LREG)


/* FP functions usually done by the CPU */
MINI_OP(OP_SIN,     "sin", FREG, FREG, NONE)
MINI_OP(OP_COS,     "cos", FREG, FREG, NONE)
MINI_OP(OP_ABS,     "abs", FREG, FREG, NONE)
MINI_OP(OP_TAN,     "tan", FREG, FREG, NONE)
MINI_OP(OP_ATAN,    "atan", FREG, FREG, NONE)
MINI_OP(OP_SQRT,    "sqrt", FREG, FREG, NONE)
MINI_OP(OP_ROUND,   "round", FREG, FREG, NONE)
/* to optimize strings */
MINI_OP(OP_STRLEN, "strlen", IREG, IREG, NONE)
MINI_OP(OP_NEWARR, "newarr", IREG, IREG, NONE)
MINI_OP(OP_LDLEN, "ldlen", IREG, IREG, NONE)
MINI_OP(OP_BOUNDS_CHECK, "bounds_check", NONE, IREG, IREG)
/* get adress of element in a 2D array */
MINI_OP(OP_LDELEMA2D, "getldelema2", NONE, NONE, NONE)
/* inlined small memcpy with constant length */
MINI_OP(OP_MEMCPY, "memcpy", NONE, NONE, NONE)
/* inlined small memset with constant length */
MINI_OP(OP_MEMSET, "memset", NONE, NONE, NONE)
MINI_OP(OP_SAVE_LMF, "save_lmf", NONE, NONE, NONE)
MINI_OP(OP_RESTORE_LMF, "restore_lmf", NONE, NONE, NONE)

/* write barrier */
MINI_OP(OP_CARD_TABLE_WBARRIER, "card_table_wbarrier", NONE, IREG, IREG)

/* arch-dep tls access */
MINI_OP(OP_TLS_GET,            "tls_get", IREG, NONE, NONE)
MINI_OP(OP_TLS_GET_REG,            "tls_get_reg", IREG, IREG, NONE)

MINI_OP(OP_LOAD_GOTADDR, "load_gotaddr", IREG, NONE, NONE)
MINI_OP(OP_DUMMY_USE, "dummy_use", NONE, IREG, NONE)
MINI_OP(OP_DUMMY_STORE, "dummy_store", NONE, NONE, NONE)
MINI_OP(OP_NOT_REACHED, "not_reached", NONE, NONE, NONE)
MINI_OP(OP_NOT_NULL, "not_null", NONE, IREG, NONE)

/* SIMD opcodes. */

#if defined(TARGET_X86) || defined(TARGET_AMD64)

MINI_OP(OP_ADDPS, "addps", XREG, XREG, XREG)
MINI_OP(OP_DIVPS, "divps", XREG, XREG, XREG)
MINI_OP(OP_MULPS, "mulps", XREG, XREG, XREG)
MINI_OP(OP_SUBPS, "subps", XREG, XREG, XREG)
MINI_OP(OP_MAXPS, "maxps", XREG, XREG, XREG)
MINI_OP(OP_MINPS, "minps", XREG, XREG, XREG)
MINI_OP(OP_COMPPS, "compps", XREG, XREG, XREG)
MINI_OP(OP_ANDPS, "andps", XREG, XREG, XREG)
MINI_OP(OP_ANDNPS, "andnps", XREG, XREG, XREG)
MINI_OP(OP_ORPS, "orps", XREG, XREG, XREG)
MINI_OP(OP_XORPS, "xorps", XREG, XREG, XREG)
MINI_OP(OP_HADDPS, "haddps", XREG, XREG, XREG)
MINI_OP(OP_HSUBPS, "hsubps", XREG, XREG, XREG)
MINI_OP(OP_ADDSUBPS, "addsubps", XREG, XREG, XREG)
MINI_OP(OP_DUPPS_LOW, "dupps_low", XREG, XREG, NONE)
MINI_OP(OP_DUPPS_HIGH, "dupps_high", XREG, XREG, NONE)

MINI_OP(OP_RSQRTPS, "rsqrtps", XREG, XREG, NONE)
MINI_OP(OP_SQRTPS, "sqrtps", XREG, XREG, NONE)
MINI_OP(OP_RCPPS, "rcpps", XREG, XREG, NONE)

MINI_OP(OP_PSHUFLEW_HIGH, "pshufflew_high", XREG, XREG, NONE)
MINI_OP(OP_PSHUFLEW_LOW, "pshufflew_low", XREG, XREG, NONE)
MINI_OP(OP_PSHUFLED, "pshuffled", XREG, XREG, NONE)
MINI_OP(OP_SHUFPS, "shufps", XREG, XREG, XREG)
MINI_OP(OP_SHUFPD, "shufpd", XREG, XREG, XREG)

MINI_OP(OP_ADDPD, "addpd", XREG, XREG, XREG)
MINI_OP(OP_DIVPD, "divpd", XREG, XREG, XREG)
MINI_OP(OP_MULPD, "mulpd", XREG, XREG, XREG)
MINI_OP(OP_SUBPD, "subpd", XREG, XREG, XREG)
MINI_OP(OP_MAXPD, "maxpd", XREG, XREG, XREG)
MINI_OP(OP_MINPD, "minpd", XREG, XREG, XREG)
MINI_OP(OP_COMPPD, "comppd", XREG, XREG, XREG)
MINI_OP(OP_ANDPD, "andpd", XREG, XREG, XREG)
MINI_OP(OP_ANDNPD, "andnpd", XREG, XREG, XREG)
MINI_OP(OP_ORPD, "orpd", XREG, XREG, XREG)
MINI_OP(OP_XORPD, "xorpd", XREG, XREG, XREG)
MINI_OP(OP_HADDPD, "haddpd", XREG, XREG, XREG)
MINI_OP(OP_HSUBPD, "hsubpd", XREG, XREG, XREG)
MINI_OP(OP_ADDSUBPD, "addsubpd", XREG, XREG, XREG)
MINI_OP(OP_DUPPD, "duppd", XREG, XREG, NONE)

MINI_OP(OP_SQRTPD, "sqrtpd", XREG, XREG, NONE)

MINI_OP(OP_EXTRACT_MASK, "extract_mask", IREG, XREG, NONE)

MINI_OP(OP_PAND, "pand", XREG, XREG, XREG)
MINI_OP(OP_POR, "por", XREG, XREG, XREG)
MINI_OP(OP_PXOR, "pxor", XREG, XREG, XREG)

MINI_OP(OP_PADDB, "paddb", XREG, XREG, XREG)
MINI_OP(OP_PADDW, "paddw", XREG, XREG, XREG)
MINI_OP(OP_PADDD, "paddd", XREG, XREG, XREG)
MINI_OP(OP_PADDQ, "paddq", XREG, XREG, XREG)

MINI_OP(OP_PSUBB, "psubb", XREG, XREG, XREG)
MINI_OP(OP_PSUBW, "psubw", XREG, XREG, XREG)
MINI_OP(OP_PSUBD, "psubd", XREG, XREG, XREG)
MINI_OP(OP_PSUBQ, "psubq", XREG, XREG, XREG)

MINI_OP(OP_PMAXB_UN, "pmaxb_un", XREG, XREG, XREG)
MINI_OP(OP_PMAXW_UN, "pmaxw_un", XREG, XREG, XREG)
MINI_OP(OP_PMAXD_UN, "pmaxd_un", XREG, XREG, XREG)

MINI_OP(OP_PMAXB, "pmaxb", XREG, XREG, XREG)
MINI_OP(OP_PMAXW, "pmaxw", XREG, XREG, XREG)
MINI_OP(OP_PMAXD, "pmaxd", XREG, XREG, XREG)

MINI_OP(OP_PAVGB_UN, "pavgb_un", XREG, XREG, XREG)
MINI_OP(OP_PAVGW_UN, "pavgw_un", XREG, XREG, XREG)

MINI_OP(OP_PMINB_UN, "pminb_un", XREG, XREG, XREG)
MINI_OP(OP_PMINW_UN, "pminw_un", XREG, XREG, XREG)
MINI_OP(OP_PMIND_UN, "pmind_un", XREG, XREG, XREG)

MINI_OP(OP_PMINB, "pminb", XREG, XREG, XREG)
MINI_OP(OP_PMINW, "pminw", XREG, XREG, XREG)
MINI_OP(OP_PMIND, "pmind", XREG, XREG, XREG)

MINI_OP(OP_PCMPEQB, "pcmpeqb", XREG, XREG, XREG)
MINI_OP(OP_PCMPEQW, "pcmpeqw", XREG, XREG, XREG)
MINI_OP(OP_PCMPEQD, "pcmpeqd", XREG, XREG, XREG)
MINI_OP(OP_PCMPEQQ, "pcmpeqq", XREG, XREG, XREG)

MINI_OP(OP_PCMPGTB, "pcmpgtb", XREG, XREG, XREG)
MINI_OP(OP_PCMPGTW, "pcmpgtw", XREG, XREG, XREG)
MINI_OP(OP_PCMPGTD, "pcmpgtd", XREG, XREG, XREG)
MINI_OP(OP_PCMPGTQ, "pcmpgtq", XREG, XREG, XREG)

MINI_OP(OP_PSUM_ABS_DIFF, "psumabsdiff", XREG, XREG, XREG)

MINI_OP(OP_UNPACK_LOWB, "unpack_lowb", XREG, XREG, XREG)
MINI_OP(OP_UNPACK_LOWW, "unpack_loww", XREG, XREG, XREG)
MINI_OP(OP_UNPACK_LOWD, "unpack_lowd", XREG, XREG, XREG)
MINI_OP(OP_UNPACK_LOWQ, "unpack_lowq", XREG, XREG, XREG)
MINI_OP(OP_UNPACK_LOWPS, "unpack_lowps", XREG, XREG, XREG)
MINI_OP(OP_UNPACK_LOWPD, "unpack_lowpd", XREG, XREG, XREG)

MINI_OP(OP_UNPACK_HIGHB, "unpack_highb", XREG, XREG, XREG)
MINI_OP(OP_UNPACK_HIGHW, "unpack_highw", XREG, XREG, XREG)
MINI_OP(OP_UNPACK_HIGHD, "unpack_highd", XREG, XREG, XREG)
MINI_OP(OP_UNPACK_HIGHQ, "unpack_highq", XREG, XREG, XREG)
MINI_OP(OP_UNPACK_HIGHPS, "unpack_highps", XREG, XREG, XREG)
MINI_OP(OP_UNPACK_HIGHPD, "unpack_highpd", XREG, XREG, XREG)

MINI_OP(OP_PACKW, "packw", XREG, XREG, XREG)
MINI_OP(OP_PACKD, "packd", XREG, XREG, XREG)

MINI_OP(OP_PACKW_UN, "packw_un", XREG, XREG, XREG)
MINI_OP(OP_PACKD_UN, "packd_un", XREG, XREG, XREG)

MINI_OP(OP_PADDB_SAT, "paddb_sat", XREG, XREG, XREG)
MINI_OP(OP_PADDB_SAT_UN, "paddb_sat_un", XREG, XREG, XREG)

MINI_OP(OP_PADDW_SAT, "paddw_sat", XREG, XREG, XREG)
MINI_OP(OP_PADDW_SAT_UN, "paddw_sat_un", XREG, XREG, XREG)

MINI_OP(OP_PSUBB_SAT, "psubb_sat", XREG, XREG, XREG)
MINI_OP(OP_PSUBB_SAT_UN, "psubb_sat_un", XREG, XREG, XREG)

MINI_OP(OP_PSUBW_SAT, "psubw_sat", XREG, XREG, XREG)
MINI_OP(OP_PSUBW_SAT_UN, "psubw_sat_un", XREG, XREG, XREG)

MINI_OP(OP_PMULW, "pmulw", XREG, XREG, XREG)
MINI_OP(OP_PMULD, "pmuld", XREG, XREG, XREG)
MINI_OP(OP_PMULQ, "pmulq", XREG, XREG, XREG)

MINI_OP(OP_PMULW_HIGH_UN, "pmul_high_un", XREG, XREG, XREG)
MINI_OP(OP_PMULW_HIGH, "pmul_high", XREG, XREG, XREG)

/*SSE2 Shift ops must have the _reg version right after as code depends on this ordering.*/ 
MINI_OP(OP_PSHRW, "pshrw", XREG, XREG, NONE)
MINI_OP(OP_PSHRW_REG, "pshrw_reg", XREG, XREG, XREG)

MINI_OP(OP_PSARW, "psarw", XREG, XREG, NONE)
MINI_OP(OP_PSARW_REG, "psarw_reg", XREG, XREG, XREG)

MINI_OP(OP_PSHLW, "pshlw", XREG, XREG, NONE)
MINI_OP(OP_PSHLW_REG, "pshlw_reg", XREG, XREG, XREG)

MINI_OP(OP_PSHRD, "pshrd", XREG, XREG, NONE)
MINI_OP(OP_PSHRD_REG, "pshrd_reg", XREG, XREG, XREG)

MINI_OP(OP_PSHRQ, "pshrq", XREG, XREG, NONE)
MINI_OP(OP_PSHRQ_REG, "pshrq_reg", XREG, XREG, XREG)

MINI_OP(OP_PSARD, "psard", XREG, XREG, NONE)
MINI_OP(OP_PSARD_REG, "psard_reg", XREG, XREG, XREG)

MINI_OP(OP_PSHLD, "pshld", XREG, XREG, NONE)
MINI_OP(OP_PSHLD_REG, "pshld_reg", XREG, XREG, XREG)

MINI_OP(OP_PSHLQ, "pshlq", XREG, XREG, NONE)
MINI_OP(OP_PSHLQ_REG, "pshlq_reg", XREG, XREG, XREG)

MINI_OP(OP_EXTRACT_I4, "extract_i4", IREG, XREG, NONE)
MINI_OP(OP_ICONV_TO_R8_RAW, "iconv_to_r8_raw", FREG, IREG, NONE)

MINI_OP(OP_EXTRACT_I2, "extract_i2", IREG, XREG, NONE)
MINI_OP(OP_EXTRACT_U2, "extract_u2", IREG, XREG, NONE)
MINI_OP(OP_EXTRACT_I1, "extract_i1", IREG, XREG, NONE)
MINI_OP(OP_EXTRACT_U1, "extract_u1", IREG, XREG, NONE)
MINI_OP(OP_EXTRACT_R8, "extract_r8", FREG, XREG, NONE)
MINI_OP(OP_EXTRACT_I8, "extract_i8", LREG, XREG, NONE)

/* Used by LLVM */
MINI_OP(OP_INSERT_I1, "insert_i1", XREG, XREG, IREG)
MINI_OP(OP_INSERT_I4, "insert_i4", XREG, XREG, IREG)
MINI_OP(OP_INSERT_I8, "insert_i8", XREG, XREG, LREG)
MINI_OP(OP_INSERT_R4, "insert_r4", XREG, XREG, FREG)
MINI_OP(OP_INSERT_R8, "insert_r8", XREG, XREG, FREG)

MINI_OP(OP_INSERT_I2, "insert_i2", XREG, XREG, IREG)

MINI_OP(OP_EXTRACTX_U2, "extractx_u2", IREG, XREG, NONE)

/*these slow ops are modeled around the availability of a fast 2 bytes insert op*/
/*insertx_u1_slow takes old value and new value as source regs */
MINI_OP(OP_INSERTX_U1_SLOW, "insertx_u1_slow", XREG, IREG, IREG)
/*insertx_i4_slow takes target xreg and new value as source regs */
MINI_OP(OP_INSERTX_I4_SLOW, "insertx_i4_slow", XREG, XREG, IREG)

MINI_OP(OP_INSERTX_R4_SLOW, "insertx_r4_slow", XREG, XREG, FREG)
MINI_OP(OP_INSERTX_R8_SLOW, "insertx_r8_slow", XREG, XREG, FREG)
MINI_OP(OP_INSERTX_I8_SLOW, "insertx_i8_slow", XREG, XREG, LREG)

MINI_OP(OP_FCONV_TO_R8_X, "fconv_to_r8_x", XREG, FREG, NONE)
MINI_OP(OP_XCONV_R8_TO_I4, "xconv_r8_to_i4", IREG, XREG, NONE)
MINI_OP(OP_ICONV_TO_X, "iconv_to_x", XREG, IREG, NONE)

MINI_OP(OP_EXPAND_I1, "expand_i1", XREG, IREG, NONE)
MINI_OP(OP_EXPAND_I2, "expand_i2", XREG, IREG, NONE)
MINI_OP(OP_EXPAND_I4, "expand_i4", XREG, IREG, NONE)
MINI_OP(OP_EXPAND_R4, "expand_r4", XREG, FREG, NONE)
MINI_OP(OP_EXPAND_I8, "expand_i8", XREG, IREG, NONE)
MINI_OP(OP_EXPAND_R8, "expand_r8", XREG, FREG, NONE)

MINI_OP(OP_PREFETCH_MEMBASE, "prefetch_membase", NONE, IREG, NONE)

MINI_OP(OP_CVTDQ2PD, "cvtdq2pd", XREG, XREG, NONE)
MINI_OP(OP_CVTDQ2PS, "cvtdq2ps", XREG, XREG, NONE)
MINI_OP(OP_CVTPD2DQ, "cvtpd2dq", XREG, XREG, NONE)
MINI_OP(OP_CVTPD2PS, "cvtpd2ps", XREG, XREG, NONE)
MINI_OP(OP_CVTPS2DQ, "cvtps2dq", XREG, XREG, NONE)
MINI_OP(OP_CVTPS2PD, "cvtps2pd", XREG, XREG, NONE)
MINI_OP(OP_CVTTPD2DQ, "cvttpd2dq", XREG, XREG, NONE)
MINI_OP(OP_CVTTPS2DQ, "cvttps2dq", XREG, XREG, NONE)

#endif

MINI_OP(OP_XMOVE,   "xmove", XREG, XREG, NONE)
MINI_OP(OP_XZERO,   "xzero", XREG, NONE, NONE)
MINI_OP(OP_XPHI,	"xphi", XREG, NONE, NONE)

/* Atomic specific

	Note, OP_ATOMIC_ADD_IMM_NEW_I4 and
	OP_ATOMIC_ADD_NEW_I4 returns the new
	value compared to OP_ATOMIC_ADD_I4 that
	returns the old value.

	OP_ATOMIC_ADD_NEW_I4 is used by
	Interlocked::Increment and Interlocked:Decrement
	and atomic_add_i4 by Interlocked::Add
*/
MINI_OP(OP_ATOMIC_ADD_I4, "atomic_add_i4", IREG, IREG, IREG)
MINI_OP(OP_ATOMIC_ADD_NEW_I4, "atomic_add_new_i4", IREG, IREG, IREG)
MINI_OP(OP_ATOMIC_ADD_IMM_I4, "atomic_add_imm_i4", IREG, IREG, NONE)
MINI_OP(OP_ATOMIC_ADD_IMM_NEW_I4, "atomic_add_imm_new_i4", IREG, IREG, NONE)
MINI_OP(OP_ATOMIC_EXCHANGE_I4, "atomic_exchange_i4", IREG, IREG, IREG)

MINI_OP(OP_ATOMIC_ADD_I8, "atomic_add_i8", IREG, IREG, IREG)
MINI_OP(OP_ATOMIC_ADD_NEW_I8, "atomic_add_new_i8", IREG, IREG, IREG)
MINI_OP(OP_ATOMIC_ADD_IMM_I8, "atomic_add_imm_i8", IREG, IREG, NONE)
MINI_OP(OP_ATOMIC_ADD_IMM_NEW_I8, "atomic_add_imm_new_i8", IREG, IREG, NONE)
MINI_OP(OP_ATOMIC_EXCHANGE_I8, "atomic_exchange_i8", IREG, IREG, IREG)
MINI_OP(OP_MEMORY_BARRIER, "memory_barrier", NONE, NONE, NONE)

MINI_OP3(OP_ATOMIC_CAS_I4, "atomic_cas_i4", IREG, IREG, IREG, IREG)
MINI_OP3(OP_ATOMIC_CAS_I8, "atomic_cas_i8", IREG, IREG, IREG, IREG)

/* Conditional move opcodes.
 * Must be in the same order as the matching CEE_B... opcodes
 * sreg2 will be assigned to dreg if the condition is true.
 * sreg1 should be equal to dreg and models the fact the instruction doesn't necessary
 * modify dreg. The sreg1==dreg condition could be violated by SSA, so the local
 * register allocator or the code generator should generate a mov dreg, sreg1 before
 * the cmov in those cases.
 * These opcodes operate on pointer sized values.
 */
MINI_OP(OP_CMOV_IEQ,    "cmov_ieq", IREG, IREG, IREG)
MINI_OP(OP_CMOV_IGE,    "cmov_ige", IREG, IREG, IREG)
MINI_OP(OP_CMOV_IGT,    "cmov_igt", IREG, IREG, IREG)
MINI_OP(OP_CMOV_ILE,    "cmov_ile", IREG, IREG, IREG)
MINI_OP(OP_CMOV_ILT,    "cmov_ilt", IREG, IREG, IREG)
MINI_OP(OP_CMOV_INE_UN, "cmov_ine_un", IREG, IREG, IREG)
MINI_OP(OP_CMOV_IGE_UN, "cmov_ige_un", IREG, IREG, IREG)
MINI_OP(OP_CMOV_IGT_UN, "cmov_igt_un", IREG, IREG, IREG)
MINI_OP(OP_CMOV_ILE_UN, "cmov_ile_un", IREG, IREG, IREG)
MINI_OP(OP_CMOV_ILT_UN, "cmov_ilt_un", IREG, IREG, IREG)

MINI_OP(OP_CMOV_LEQ,    "cmov_leq", IREG, IREG, IREG)
MINI_OP(OP_CMOV_LGE,    "cmov_lge", IREG, IREG, IREG)
MINI_OP(OP_CMOV_LGT,    "cmov_lgt", IREG, IREG, IREG)
MINI_OP(OP_CMOV_LLE,    "cmov_lle", IREG, IREG, IREG)
MINI_OP(OP_CMOV_LLT,    "cmov_llt", IREG, IREG, IREG)
MINI_OP(OP_CMOV_LNE_UN, "cmov_lne_un", IREG, IREG, IREG)
MINI_OP(OP_CMOV_LGE_UN, "cmov_lge_un", IREG, IREG, IREG)
MINI_OP(OP_CMOV_LGT_UN, "cmov_lgt_un", IREG, IREG, IREG)
MINI_OP(OP_CMOV_LLE_UN, "cmov_lle_un", IREG, IREG, IREG)
MINI_OP(OP_CMOV_LLT_UN, "cmov_llt_un", IREG, IREG, IREG)

/* Debugging support */
/* 
 * Marks the start of the live range of the variable in inst_c0, that is the
 * first instruction where the variable has a value.
 */
MINI_OP(OP_LIVERANGE_START, "liverange_start", NONE, NONE, NONE)
/* 
 * Marks the end of the live range of the variable in inst_c0, that is the
 * first instruction where the variable no longer has a value.
 */
MINI_OP(OP_LIVERANGE_END, "liverange_end", NONE, NONE, NONE)

/* GC support */
/*
 * mono_arch_output_basic_block () will set the backend.pc_offset field to the current pc
 * offset.
 */
MINI_OP(OP_GC_LIVENESS_DEF, "gc_liveness_def", NONE, NONE, NONE)
MINI_OP(OP_GC_LIVENESS_USE, "gc_liveness_use", NONE, NONE, NONE)

/*
 * This marks the location inside a basic block where a GC tracked spill slot has been
 * defined. The spill slot is assumed to be alive until the end of the bblock.
 */
MINI_OP(OP_GC_SPILL_SLOT_LIVENESS_DEF, "gc_spill_slot_liveness_def", NONE, NONE, NONE)

/*
 * This marks the location inside a basic block where a GC tracked param area slot has
 * been defined. The slot is assumed to be alive until the next call.
 */
MINI_OP(OP_GC_PARAM_SLOT_LIVENESS_DEF, "gc_param_slot_liveness_def", NONE, NONE, NONE)

/* Arch specific opcodes */
/* #if defined(__native_client_codegen__) || defined(__native_client__) */
/* We have to define these in terms of the TARGET defines, not NaCl defines */
/* because genmdesc.pl doesn't have multiple defines per platform.          */
#if defined(TARGET_AMD64) || defined(TARGET_X86) || defined(TARGET_ARM)
MINI_OP(OP_NACL_GC_SAFE_POINT,     "nacl_gc_safe_point", IREG, NONE, NONE)
#endif

#if defined(TARGET_X86) || defined(TARGET_AMD64)
MINI_OP(OP_X86_TEST_NULL,          "x86_test_null", NONE, IREG, NONE)
MINI_OP(OP_X86_COMPARE_MEMBASE_REG,"x86_compare_membase_reg", NONE, IREG, IREG)
MINI_OP(OP_X86_COMPARE_MEMBASE_IMM,"x86_compare_membase_imm", NONE, IREG, NONE)
MINI_OP(OP_X86_COMPARE_MEM_IMM,    "x86_compare_mem_imm", NONE, NONE, NONE)
MINI_OP(OP_X86_COMPARE_MEMBASE8_IMM,"x86_compare_membase8_imm", NONE, IREG, NONE)
MINI_OP(OP_X86_COMPARE_REG_MEMBASE,"x86_compare_reg_membase", NONE, IREG, IREG)
MINI_OP(OP_X86_INC_REG,            "x86_inc_reg", IREG, IREG, NONE)
MINI_OP(OP_X86_INC_MEMBASE,        "x86_inc_membase", NONE, IREG, NONE)
MINI_OP(OP_X86_DEC_REG,            "x86_dec_reg", IREG, IREG, NONE)
MINI_OP(OP_X86_DEC_MEMBASE,        "x86_dec_membase", NONE, IREG, NONE)
MINI_OP(OP_X86_ADD_MEMBASE_IMM,    "x86_add_membase_imm", NONE, IREG, NONE)
MINI_OP(OP_X86_SUB_MEMBASE_IMM,    "x86_sub_membase_imm", NONE, IREG, NONE)
MINI_OP(OP_X86_AND_MEMBASE_IMM,    "x86_and_membase_imm", NONE, IREG, NONE)
MINI_OP(OP_X86_OR_MEMBASE_IMM,     "x86_or_membase_imm", NONE, IREG, NONE)
MINI_OP(OP_X86_XOR_MEMBASE_IMM,    "x86_xor_membase_imm", NONE, IREG, NONE)
MINI_OP(OP_X86_ADD_MEMBASE_REG,    "x86_add_membase_reg", NONE, IREG, IREG)
MINI_OP(OP_X86_SUB_MEMBASE_REG,    "x86_sub_membase_reg", NONE, IREG, IREG)
MINI_OP(OP_X86_AND_MEMBASE_REG,    "x86_and_membase_reg", NONE, IREG, IREG)
MINI_OP(OP_X86_OR_MEMBASE_REG,     "x86_or_membase_reg", NONE, IREG, IREG)
MINI_OP(OP_X86_XOR_MEMBASE_REG,    "x86_xor_membase_reg", NONE, IREG, IREG)
MINI_OP(OP_X86_MUL_MEMBASE_REG,    "x86_mul_membase_reg", NONE, IREG, IREG)

MINI_OP(OP_X86_ADD_REG_MEMBASE,    "x86_add_reg_membase", IREG, IREG, IREG)
MINI_OP(OP_X86_SUB_REG_MEMBASE,    "x86_sub_reg_membase", IREG, IREG, IREG)
MINI_OP(OP_X86_MUL_REG_MEMBASE,    "x86_mul_reg_membase", IREG, IREG, IREG)
MINI_OP(OP_X86_AND_REG_MEMBASE,    "x86_and_reg_membase", IREG, IREG, IREG)
MINI_OP(OP_X86_OR_REG_MEMBASE,     "x86_or_reg_membase", IREG, IREG, IREG)
MINI_OP(OP_X86_XOR_REG_MEMBASE,    "x86_xor_reg_membase", IREG, IREG, IREG)

MINI_OP(OP_X86_PUSH_MEMBASE,       "x86_push_membase", NONE, IREG, NONE)
MINI_OP(OP_X86_PUSH_IMM,           "x86_push_imm", NONE, NONE, NONE)
MINI_OP(OP_X86_PUSH,               "x86_push", NONE, IREG, NONE)
MINI_OP(OP_X86_PUSH_OBJ,           "x86_push_obj", NONE, IREG, NONE)
MINI_OP(OP_X86_PUSH_GOT_ENTRY,     "x86_push_got_entry", NONE, IREG, NONE)
MINI_OP(OP_X86_LEA,                "x86_lea", IREG, IREG, IREG)
MINI_OP(OP_X86_LEA_MEMBASE,        "x86_lea_membase", IREG, IREG, NONE)
MINI_OP(OP_X86_XCHG,               "x86_xchg", NONE, IREG, IREG)
MINI_OP(OP_X86_FPOP,               "x86_fpop", NONE, FREG, NONE)
MINI_OP(OP_X86_FP_LOAD_I8,         "x86_fp_load_i8", FREG, IREG, NONE)
MINI_OP(OP_X86_FP_LOAD_I4,         "x86_fp_load_i4", FREG, IREG, NONE)
MINI_OP(OP_X86_SETEQ_MEMBASE,      "x86_seteq_membase", NONE, IREG, NONE)
MINI_OP(OP_X86_SETNE_MEMBASE,      "x86_setne_membase", NONE, IREG, NONE)
MINI_OP(OP_X86_FXCH,               "x86_fxch", NONE, NONE, NONE)
#endif

#if defined(TARGET_AMD64)
MINI_OP(OP_AMD64_TEST_NULL,              "amd64_test_null", NONE, IREG, NONE)
MINI_OP(OP_AMD64_SET_XMMREG_R4,          "amd64_set_xmmreg_r4", FREG, FREG, NONE)
MINI_OP(OP_AMD64_SET_XMMREG_R8,          "amd64_set_xmmreg_r8", FREG, FREG, NONE)
MINI_OP(OP_AMD64_ICOMPARE_MEMBASE_REG,   "amd64_icompare_membase_reg", NONE, IREG, IREG)
MINI_OP(OP_AMD64_ICOMPARE_MEMBASE_IMM,   "amd64_icompare_membase_imm", NONE, IREG, NONE)
MINI_OP(OP_AMD64_ICOMPARE_REG_MEMBASE,   "amd64_icompare_reg_membase", NONE, IREG, IREG)
MINI_OP(OP_AMD64_COMPARE_MEMBASE_REG,    "amd64_compare_membase_reg", NONE, IREG, IREG)
MINI_OP(OP_AMD64_COMPARE_MEMBASE_IMM,    "amd64_compare_membase_imm", NONE, IREG, NONE)
MINI_OP(OP_AMD64_COMPARE_REG_MEMBASE,    "amd64_compare_reg_membase", NONE, IREG, IREG)

MINI_OP(OP_AMD64_ADD_MEMBASE_REG,        "amd64_add_membase_reg", NONE, IREG, IREG)
MINI_OP(OP_AMD64_SUB_MEMBASE_REG,        "amd64_sub_membase_reg", NONE, IREG, IREG)
MINI_OP(OP_AMD64_AND_MEMBASE_REG,        "amd64_and_membase_reg", NONE, IREG, IREG)
MINI_OP(OP_AMD64_OR_MEMBASE_REG,         "amd64_or_membase_reg", NONE, IREG, IREG)
MINI_OP(OP_AMD64_XOR_MEMBASE_REG,        "amd64_xor_membase_reg", NONE, IREG, IREG)
MINI_OP(OP_AMD64_MUL_MEMBASE_REG,        "amd64_mul_membase_reg", NONE, IREG, IREG)

MINI_OP(OP_AMD64_ADD_MEMBASE_IMM,        "amd64_add_membase_imm", NONE, IREG, NONE)
MINI_OP(OP_AMD64_SUB_MEMBASE_IMM,        "amd64_sub_membase_imm", NONE, IREG, NONE)
MINI_OP(OP_AMD64_AND_MEMBASE_IMM,        "amd64_and_membase_imm", NONE, IREG, NONE)
MINI_OP(OP_AMD64_OR_MEMBASE_IMM,         "amd64_or_membase_imm", NONE, IREG, NONE)
MINI_OP(OP_AMD64_XOR_MEMBASE_IMM,        "amd64_xor_membase_imm", NONE, IREG, NONE)
MINI_OP(OP_AMD64_MUL_MEMBASE_IMM,        "amd64_mul_membase_imm", NONE, IREG, NONE)

MINI_OP(OP_AMD64_ADD_REG_MEMBASE,        "amd64_add_reg_membase", IREG, IREG, IREG)
MINI_OP(OP_AMD64_SUB_REG_MEMBASE,        "amd64_sub_reg_membase", IREG, IREG, IREG)
MINI_OP(OP_AMD64_AND_REG_MEMBASE,        "amd64_and_reg_membase", IREG, IREG, IREG)
MINI_OP(OP_AMD64_OR_REG_MEMBASE,         "amd64_or_reg_membase", IREG, IREG, IREG)
MINI_OP(OP_AMD64_XOR_REG_MEMBASE,        "amd64_xor_reg_membase", IREG, IREG, IREG)
MINI_OP(OP_AMD64_MUL_REG_MEMBASE,        "amd64_mul_reg_membase", IREG, IREG, IREG)

MINI_OP(OP_AMD64_LOADI8_MEMINDEX,        "amd64_loadi8_memindex", IREG, IREG, IREG)
MINI_OP(OP_AMD64_SAVE_SP_TO_LMF,         "amd64_save_sp_to_lmf", NONE, NONE, NONE)
#endif

#if  defined(__ppc__) || defined(__powerpc__) || defined(__ppc64__) || defined(TARGET_POWERPC)
MINI_OP(OP_PPC_SUBFIC,             "ppc_subfic", IREG, IREG, NONE)
MINI_OP(OP_PPC_SUBFZE,             "ppc_subfze", IREG, IREG, NONE)
MINI_OP(OP_CHECK_FINITE,           "ppc_check_finite", NONE, IREG, NONE)
#endif

#if defined(TARGET_ARM)
MINI_OP(OP_ARM_RSBS_IMM,            "arm_rsbs_imm", IREG, IREG, NONE)
MINI_OP(OP_ARM_RSC_IMM,             "arm_rsc_imm", IREG, IREG, NONE)
#endif

#if defined(__sparc__) || defined(sparc)
MINI_OP(OP_SPARC_BRZ,              "sparc_brz", NONE, NONE, NONE)
MINI_OP(OP_SPARC_BRLEZ,            "sparc_brlez", NONE, NONE, NONE)
MINI_OP(OP_SPARC_BRLZ,             "sparc_brlz", NONE, NONE, NONE)
MINI_OP(OP_SPARC_BRNZ,             "sparc_brnz", NONE, NONE, NONE)
MINI_OP(OP_SPARC_BRGZ,             "sparc_brgz", NONE, NONE, NONE)
MINI_OP(OP_SPARC_BRGEZ,            "sparc_brgez", NONE, NONE, NONE)
MINI_OP(OP_SPARC_COND_EXC_EQZ,     "sparc_cond_exc_eqz", NONE, NONE, NONE)
MINI_OP(OP_SPARC_COND_EXC_GEZ,     "sparc_cond_exc_gez", NONE, NONE, NONE)
MINI_OP(OP_SPARC_COND_EXC_GTZ,     "sparc_cond_exc_gtz", NONE, NONE, NONE)
MINI_OP(OP_SPARC_COND_EXC_LEZ,     "sparc_cond_exc_lez", NONE, NONE, NONE)
MINI_OP(OP_SPARC_COND_EXC_LTZ,     "sparc_cond_exc_ltz", NONE, NONE, NONE)
MINI_OP(OP_SPARC_COND_EXC_NEZ,     "sparc_cond_exc_nez", NONE, NONE, NONE)
#endif

#if defined(__s390__) || defined(s390)
MINI_OP(OP_S390_LOADARG,	   "s390_loadarg", NONE, NONE, NONE)
MINI_OP(OP_S390_ARGREG, 	   "s390_argreg", NONE, NONE, NONE)
MINI_OP(OP_S390_ARGPTR, 	   "s390_argptr", NONE, NONE, NONE)
MINI_OP(OP_S390_STKARG, 	   "s390_stkarg", NONE, NONE, NONE)
MINI_OP(OP_S390_MOVE,	 	   "s390_move", IREG, IREG, NONE)
MINI_OP(OP_S390_SETF4RET,	   "s390_setf4ret", FREG, FREG, NONE)
MINI_OP(OP_S390_BKCHAIN, 	   "s390_bkchain", NONE, NONE, NONE)
MINI_OP(OP_S390_LADD,          "s390_long_add", LREG, IREG, IREG)
MINI_OP(OP_S390_LADD_OVF,      "s390_long_add_ovf", LREG, IREG, IREG)
MINI_OP(OP_S390_LADD_OVF_UN,   "s390_long_add_ovf_un", LREG, IREG, IREG)
MINI_OP(OP_S390_LSUB,          "s390_long_sub", LREG, IREG, IREG)
MINI_OP(OP_S390_LSUB_OVF,      "s390_long_sub_ovf", LREG, IREG, IREG)
MINI_OP(OP_S390_LSUB_OVF_UN,   "s390_long_sub_ovf_un", LREG, IREG, IREG)
MINI_OP(OP_S390_LNEG,          "s390_long_neg", LREG, IREG, IREG)
MINI_OP(OP_S390_IADD_OVF,       "s390_int_add_ovf", IREG, IREG, IREG)
MINI_OP(OP_S390_IADD_OVF_UN,    "s390_int_add_ovf_un", IREG, IREG, IREG)
MINI_OP(OP_S390_ISUB_OVF,       "s390_int_sub_ovf", IREG, IREG, IREG)
MINI_OP(OP_S390_ISUB_OVF_UN,    "s390_int_sub_ovf_un", IREG, IREG, IREG)
#endif

#if defined(__ia64__)
MINI_OP(OP_IA64_LOAD,          "ia64_load", NONE, NONE, NONE)
MINI_OP(OP_IA64_LOADI1,        "ia64_loadi1", NONE, NONE, NONE)
MINI_OP(OP_IA64_LOADU1,        "ia64_loadu1", NONE, NONE, NONE)
MINI_OP(OP_IA64_LOADI2,        "ia64_loadi2", NONE, NONE, NONE)
MINI_OP(OP_IA64_LOADU2,        "ia64_loadu2", NONE, NONE, NONE)
MINI_OP(OP_IA64_LOADI4,        "ia64_loadi4", NONE, NONE, NONE)
MINI_OP(OP_IA64_LOADU4,        "ia64_loadu4", NONE, NONE, NONE)
MINI_OP(OP_IA64_LOADI8,        "ia64_loadi8", NONE, NONE, NONE)
MINI_OP(OP_IA64_LOADU8,        "ia64_loadu8", NONE, NONE, NONE)
MINI_OP(OP_IA64_LOADR4,        "ia64_loadr4", NONE, NONE, NONE)
MINI_OP(OP_IA64_LOADR8,        "ia64_loadr8", NONE, NONE, NONE)
MINI_OP(OP_IA64_STORE,          "ia64_store", NONE, NONE, NONE)
MINI_OP(OP_IA64_STOREI1,        "ia64_storei1", NONE, NONE, NONE)
MINI_OP(OP_IA64_STOREU1,        "ia64_storeu1", NONE, NONE, NONE)
MINI_OP(OP_IA64_STOREI2,        "ia64_storei2", NONE, NONE, NONE)
MINI_OP(OP_IA64_STOREU2,        "ia64_storeu2", NONE, NONE, NONE)
MINI_OP(OP_IA64_STOREI4,        "ia64_storei4", NONE, NONE, NONE)
MINI_OP(OP_IA64_STOREU4,        "ia64_storeu4", NONE, NONE, NONE)
MINI_OP(OP_IA64_STOREI8,        "ia64_storei8", NONE, NONE, NONE)
MINI_OP(OP_IA64_STOREU8,        "ia64_storeu8", NONE, NONE, NONE)
MINI_OP(OP_IA64_STORER4,        "ia64_storer4", NONE, NONE, NONE)
MINI_OP(OP_IA64_STORER8,        "ia64_storer8", NONE, NONE, NONE)

MINI_OP(OP_IA64_CMP4_EQ,        "ia64_cmp4_eq", NONE, IREG, IREG)
MINI_OP(OP_IA64_CMP4_NE,        "ia64_cmp4_ne", NONE, IREG, IREG)
MINI_OP(OP_IA64_CMP4_LE,        "ia64_cmp4_le", NONE, IREG, IREG)
MINI_OP(OP_IA64_CMP4_LT,        "ia64_cmp4_lt", NONE, IREG, IREG)
MINI_OP(OP_IA64_CMP4_GE,        "ia64_cmp4_ge", NONE, IREG, IREG)
MINI_OP(OP_IA64_CMP4_GT,        "ia64_cmp4_gt", NONE, IREG, IREG)
MINI_OP(OP_IA64_CMP4_LE_UN,     "ia64_cmp4_le_un", NONE, IREG, IREG)
MINI_OP(OP_IA64_CMP4_LT_UN,     "ia64_cmp4_lt_un", NONE, IREG, IREG)
MINI_OP(OP_IA64_CMP4_GE_UN,     "ia64_cmp4_ge_un", NONE, IREG, IREG)
MINI_OP(OP_IA64_CMP4_GT_UN,     "ia64_cmp4_gt_un", NONE, IREG, IREG)
MINI_OP(OP_IA64_CMP_EQ,         "ia64_cmp_eq", NONE, IREG, IREG)
MINI_OP(OP_IA64_CMP_NE,         "ia64_cmp_ne", NONE, IREG, IREG)
MINI_OP(OP_IA64_CMP_LE,         "ia64_cmp_le", NONE, IREG, IREG)
MINI_OP(OP_IA64_CMP_LT,         "ia64_cmp_lt", NONE, IREG, IREG)
MINI_OP(OP_IA64_CMP_GE,         "ia64_cmp_ge", NONE, IREG, IREG)
MINI_OP(OP_IA64_CMP_GT,         "ia64_cmp_gt", NONE, IREG, IREG)
MINI_OP(OP_IA64_CMP_LT_UN,      "ia64_cmp_lt_un", NONE, IREG, IREG)
MINI_OP(OP_IA64_CMP_GT_UN,      "ia64_cmp_gt_un", NONE, IREG, IREG)
MINI_OP(OP_IA64_CMP_GE_UN,      "ia64_cmp_ge_un", NONE, IREG, IREG)
MINI_OP(OP_IA64_CMP_LE_UN,      "ia64_cmp_le_un", NONE, IREG, IREG)

MINI_OP(OP_IA64_CMP4_EQ_IMM,        "ia64_cmp4_eq_imm", NONE, NONE, IREG)
MINI_OP(OP_IA64_CMP4_NE_IMM,        "ia64_cmp4_ne_imm", NONE, NONE, IREG)
MINI_OP(OP_IA64_CMP4_LE_IMM,        "ia64_cmp4_le_imm", NONE, NONE, IREG)
MINI_OP(OP_IA64_CMP4_LT_IMM,        "ia64_cmp4_lt_imm", NONE, NONE, IREG)
MINI_OP(OP_IA64_CMP4_GE_IMM,        "ia64_cmp4_ge_imm", NONE, NONE, IREG)
MINI_OP(OP_IA64_CMP4_GT_IMM,        "ia64_cmp4_gt_imm", NONE, NONE, IREG)
MINI_OP(OP_IA64_CMP4_LE_UN_IMM,     "ia64_cmp4_le_un_imm", NONE, NONE, IREG)
MINI_OP(OP_IA64_CMP4_LT_UN_IMM,     "ia64_cmp4_lt_un_imm", NONE, NONE, IREG)
MINI_OP(OP_IA64_CMP4_GE_UN_IMM,     "ia64_cmp4_ge_un_imm", NONE, NONE, IREG)
MINI_OP(OP_IA64_CMP4_GT_UN_IMM,     "ia64_cmp4_gt_un_imm", NONE, NONE, IREG)
MINI_OP(OP_IA64_CMP_EQ_IMM,         "ia64_cmp_eq_imm", NONE, NONE, IREG)
MINI_OP(OP_IA64_CMP_NE_IMM,         "ia64_cmp_ne_imm", NONE, NONE, IREG)
MINI_OP(OP_IA64_CMP_LE_IMM,         "ia64_cmp_le_imm", NONE, NONE, IREG)
MINI_OP(OP_IA64_CMP_LT_IMM,         "ia64_cmp_lt_imm", NONE, NONE, IREG)
MINI_OP(OP_IA64_CMP_GE_IMM,         "ia64_cmp_ge_imm", NONE, NONE, IREG)
MINI_OP(OP_IA64_CMP_GT_IMM,         "ia64_cmp_gt_imm", NONE, NONE, IREG)
MINI_OP(OP_IA64_CMP_LT_UN_IMM,      "ia64_cmp_lt_un_imm", NONE, NONE, IREG)
MINI_OP(OP_IA64_CMP_GT_UN_IMM,      "ia64_cmp_gt_un_imm", NONE, NONE, IREG)
MINI_OP(OP_IA64_CMP_GE_UN_IMM,      "ia64_cmp_ge_un_imm", NONE, NONE, IREG)
MINI_OP(OP_IA64_CMP_LE_UN_IMM,      "ia64_cmp_le_un_imm", NONE, NONE, IREG)

MINI_OP(OP_IA64_FCMP_EQ,         "ia64_fcmp_eq", NONE, IREG, IREG)
MINI_OP(OP_IA64_FCMP_NE,         "ia64_fcmp_ne", NONE, IREG, IREG)
MINI_OP(OP_IA64_FCMP_LE,         "ia64_fcmp_le", NONE, IREG, IREG)
MINI_OP(OP_IA64_FCMP_LT,         "ia64_fcmp_lt", NONE, IREG, IREG)
MINI_OP(OP_IA64_FCMP_GE,         "ia64_fcmp_ge", NONE, IREG, IREG)
MINI_OP(OP_IA64_FCMP_GT,         "ia64_fcmp_gt", NONE, IREG, IREG)
MINI_OP(OP_IA64_FCMP_LT_UN,      "ia64_fcmp_lt_un", NONE, IREG, IREG)
MINI_OP(OP_IA64_FCMP_GT_UN,      "ia64_fcmp_gt_un", NONE, IREG, IREG)
MINI_OP(OP_IA64_FCMP_GE_UN,      "ia64_fcmp_ge_un", NONE, IREG, IREG)
MINI_OP(OP_IA64_FCMP_LE_UN,      "ia64_fcmp_le_un", NONE, IREG, IREG)

MINI_OP(OP_IA64_BR_COND,        "ia64_br_cond", NONE, NONE, NONE)
MINI_OP(OP_IA64_COND_EXC,       "ia64_cond_exc", NONE, NONE, NONE)
MINI_OP(OP_IA64_CSET,           "ia64_cset", IREG, NONE, NONE)

MINI_OP(OP_IA64_STOREI1_MEMBASE_INC_REG, "ia64_storei1_membase_inc_reg", IREG, IREG, NONE)
MINI_OP(OP_IA64_STOREI2_MEMBASE_INC_REG, "ia64_storei2_membase_inc_reg", IREG, IREG, NONE)
MINI_OP(OP_IA64_STOREI4_MEMBASE_INC_REG, "ia64_storei4_membase_inc_reg", IREG, IREG, NONE)
MINI_OP(OP_IA64_STOREI8_MEMBASE_INC_REG, "ia64_storei8_membase_inc_reg", IREG, IREG, NONE)
MINI_OP(OP_IA64_STORER4_MEMBASE_INC_REG, "ia64_storer4_membase_inc_reg", IREG, IREG, NONE)
MINI_OP(OP_IA64_STORER8_MEMBASE_INC_REG, "ia64_storer8_membase_inc_reg", IREG, IREG, NONE)
MINI_OP(OP_IA64_LOADI1_MEMBASE_INC,"ia64_loadi1_membase_inc", IREG, IREG, NONE)
MINI_OP(OP_IA64_LOADU1_MEMBASE_INC,"ia64_loadu1_membase_inc", IREG, IREG, NONE)
MINI_OP(OP_IA64_LOADI2_MEMBASE_INC,"ia64_loadi2_membase_inc", IREG, IREG, NONE)
MINI_OP(OP_IA64_LOADU2_MEMBASE_INC,"ia64_loadu2_membase_inc", IREG, IREG, NONE)
MINI_OP(OP_IA64_LOADI4_MEMBASE_INC,"ia64_loadi4_membase_inc", IREG, IREG, NONE)
MINI_OP(OP_IA64_LOADU4_MEMBASE_INC,"ia64_loadu4_membase_inc", IREG, IREG, NONE)
MINI_OP(OP_IA64_LOADI8_MEMBASE_INC,"ia64_loadi8_membase_inc", IREG, IREG, NONE)
MINI_OP(OP_IA64_LOADR4_MEMBASE_INC,"ia64_loadr4_membase_inc", IREG, IREG, NONE)
MINI_OP(OP_IA64_LOADR8_MEMBASE_INC,"ia64_loadr8_membase_inc", IREG, IREG, NONE)
#endif

#if defined(__mips__)
MINI_OP(OP_MIPS_BEQ,   "mips_beq", NONE, IREG, IREG)
MINI_OP(OP_MIPS_BGEZ,  "mips_bgez", NONE, IREG, NONE)
MINI_OP(OP_MIPS_BGTZ,  "mips_bgtz", NONE, IREG, NONE)
MINI_OP(OP_MIPS_BLEZ,  "mips_blez", NONE, IREG, NONE)
MINI_OP(OP_MIPS_BLTZ,  "mips_bltz", NONE, IREG, NONE)
MINI_OP(OP_MIPS_BNE,   "mips_bne", NONE, IREG, IREG)
MINI_OP(OP_MIPS_CVTSD, "mips_cvtsd", FREG, FREG, NONE)
MINI_OP(OP_MIPS_FBEQ,  "mips_fbeq", NONE, FREG, FREG)
MINI_OP(OP_MIPS_FBGE,  "mips_fbge", NONE, FREG, FREG)
MINI_OP(OP_MIPS_FBGE_UN,  "mips_fbge_un", NONE, FREG, FREG)
MINI_OP(OP_MIPS_FBGT,  "mips_fbgt", NONE, FREG, FREG)
MINI_OP(OP_MIPS_FBGT_UN,  "mips_fbgt_un", NONE, FREG, FREG)
MINI_OP(OP_MIPS_FBLE,  "mips_fble", NONE, FREG, FREG)
MINI_OP(OP_MIPS_FBLE_UN,  "mips_fble_un", NONE, FREG, FREG)
MINI_OP(OP_MIPS_FBLT,  "mips_fblt", NONE, FREG, FREG)
MINI_OP(OP_MIPS_FBLT_UN,  "mips_fblt_un", NONE, FREG, FREG)
MINI_OP(OP_MIPS_FBNE,  "mips_fbne", NONE, FREG, FREG)
MINI_OP(OP_MIPS_FBFALSE, "mips_fbfalse", NONE, NONE, NONE)
MINI_OP(OP_MIPS_FBTRUE, "mips_fbtrue", NONE, NONE, NONE)
MINI_OP(OP_MIPS_LWC1,  "mips_lwc1", NONE, NONE, NONE)
MINI_OP(OP_MIPS_MTC1S, "mips_mtc1_s", FREG, IREG, NONE)
MINI_OP(OP_MIPS_MTC1S_2, "mips_mtc1_s2", FREG, IREG, IREG)
MINI_OP(OP_MIPS_MFC1S, "mips_mfc1_s", IREG, FREG, NONE)
MINI_OP(OP_MIPS_MTC1D, "mips_mtc1_d", FREG, IREG, NONE)
MINI_OP(OP_MIPS_MFC1D, "mips_mfc1_d", IREG, FREG, NONE)
MINI_OP(OP_MIPS_NOP,   "mips_nop", NONE, NONE, NONE)
MINI_OP(OP_MIPS_SLTI,  "mips_slti", IREG, IREG, NONE)
MINI_OP(OP_MIPS_SLT,   "mips_slt", IREG, IREG, IREG)
MINI_OP(OP_MIPS_SLTIU, "mips_sltiu", IREG, IREG, NONE)
MINI_OP(OP_MIPS_SLTU,  "mips_sltu", IREG, IREG, IREG)

MINI_OP(OP_MIPS_COND_EXC_EQ, "mips_cond_exc_eq", NONE, IREG, IREG)
MINI_OP(OP_MIPS_COND_EXC_GE, "mips_cond_exc_ge", NONE, IREG, IREG)
MINI_OP(OP_MIPS_COND_EXC_GT, "mips_cond_exc_gt", NONE, IREG, IREG)
MINI_OP(OP_MIPS_COND_EXC_LE, "mips_cond_exc_le", NONE, IREG, IREG)
MINI_OP(OP_MIPS_COND_EXC_LT, "mips_cond_exc_lt", NONE, IREG, IREG)
MINI_OP(OP_MIPS_COND_EXC_NE_UN, "mips_cond_exc_ne_un", NONE, IREG, IREG)
MINI_OP(OP_MIPS_COND_EXC_GE_UN, "mips_cond_exc_ge_un", NONE, IREG, IREG)
MINI_OP(OP_MIPS_COND_EXC_GT_UN, "mips_cond_exc_gt_un", NONE, IREG, IREG)
MINI_OP(OP_MIPS_COND_EXC_LE_UN, "mips_cond_exc_le_un", NONE, IREG, IREG)
MINI_OP(OP_MIPS_COND_EXC_LT_UN, "mips_cond_exc_lt_un", NONE, IREG, IREG)

MINI_OP(OP_MIPS_COND_EXC_OV, "mips_cond_exc_ov", NONE, IREG, IREG)
MINI_OP(OP_MIPS_COND_EXC_NO, "mips_cond_exc_no", NONE, IREG, IREG)
MINI_OP(OP_MIPS_COND_EXC_C, "mips_cond_exc_c", NONE, IREG, IREG)
MINI_OP(OP_MIPS_COND_EXC_NC, "mips_cond_exc_nc", NONE, IREG, IREG)

MINI_OP(OP_MIPS_COND_EXC_IEQ, "mips_cond_exc_ieq", NONE, IREG, IREG)
MINI_OP(OP_MIPS_COND_EXC_IGE, "mips_cond_exc_ige", NONE, IREG, IREG)
MINI_OP(OP_MIPS_COND_EXC_IGT, "mips_cond_exc_igt", NONE, IREG, IREG)
MINI_OP(OP_MIPS_COND_EXC_ILE, "mips_cond_exc_ile", NONE, IREG, IREG)
MINI_OP(OP_MIPS_COND_EXC_ILT, "mips_cond_exc_ilt", NONE, IREG, IREG)
MINI_OP(OP_MIPS_COND_EXC_INE_UN, "mips_cond_exc_ine_un", NONE, IREG, IREG)
MINI_OP(OP_MIPS_COND_EXC_IGE_UN, "mips_cond_exc_ige_un", NONE, IREG, IREG)
MINI_OP(OP_MIPS_COND_EXC_IGT_UN, "mips_cond_exc_igt_un", NONE, IREG, IREG)
MINI_OP(OP_MIPS_COND_EXC_ILE_UN, "mips_cond_exc_ile_un", NONE, IREG, IREG)
MINI_OP(OP_MIPS_COND_EXC_ILT_UN, "mips_cond_exc_ilt_un", NONE, IREG, IREG)

MINI_OP(OP_MIPS_COND_EXC_IOV, "mips_cond_exc_iov", NONE, IREG, IREG)
MINI_OP(OP_MIPS_COND_EXC_INO, "mips_cond_exc_ino", NONE, IREG, IREG)
MINI_OP(OP_MIPS_COND_EXC_IC, "mips_cond_exc_ic", NONE, IREG, IREG)
MINI_OP(OP_MIPS_COND_EXC_INC, "mips_cond_exc_inc", NONE, IREG, IREG)

#endif

/* Same as OUTARG_VT, but has a dreg */
#ifdef ENABLE_LLVM
MINI_OP(OP_LLVM_OUTARG_VT,	"llvm_outarg_vt", IREG, VREG, NONE)
#endif

