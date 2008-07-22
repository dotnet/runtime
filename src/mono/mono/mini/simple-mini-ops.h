
#if 0

MINI_OP(OP_OBJADDR,	"objaddr")
MINI_OP(OP_VTADDR,	"vtaddr")
MINI_OP(OP_RENAME,	"rename")

MINI_OP(OP_GROUP, "group")

MINI_OP(OP_CISINST, "cisinst")
MINI_OP(OP_CCASTCLASS, "ccastclass")

MINI_OP(OP_GETCHR, "getchar")
MINI_OP(OP_STRLEN, "strlen")

#endif

#if (ANALYZE_DEV_USE_SPECIFIC_OPS)
MINI_OP(OP_LOCAL, "local")
MINI_OP(OP_ARG, "arg")
#endif


MINI_OP(OP_LOAD_MEMBASE,"load_membase")
MINI_OP(OP_LOADI1_MEMBASE,"loadi1_membase")
MINI_OP(OP_LOADU1_MEMBASE,"loadu1_membase")
MINI_OP(OP_LOADI2_MEMBASE,"loadi2_membase")
MINI_OP(OP_LOADU2_MEMBASE,"loadu2_membase")
MINI_OP(OP_LOADI4_MEMBASE,"loadi4_membase")
MINI_OP(OP_LOADU4_MEMBASE,"loadu4_membase")
MINI_OP(OP_LOADI8_MEMBASE,"loadi8_membase")
MINI_OP(OP_LOADR4_MEMBASE,"loadr4_membase")
MINI_OP(OP_LOADR8_MEMBASE,"loadr8_membase")
MINI_OP(OP_LOADR8_SPILL_MEMBASE,"loadr8_spill_membase")
MINI_OP(OP_LOADU4_MEM,"loadu4_mem")

#if (TREEMOVE_SPECIFIC_OPS)
MINI_OP(OP_LDELEMA2D, "getldelema2")
#endif

MINI_OP(OP_GETTYPE, "gettype")
MINI_OP(OP_GETHASHCODE, "gethashcode")



MINI_OP(OP_ADD_IMM,    "add_imm")
MINI_OP(OP_SUB_IMM,    "sub_imm")
MINI_OP(OP_MUL_IMM,    "mul_imm")
MINI_OP(OP_DIV_IMM,    "div_imm")
MINI_OP(OP_DIV_UN_IMM, "div_un_imm")
MINI_OP(OP_REM_IMM,    "rem_imm")
MINI_OP(OP_REM_UN_IMM, "rem_un_imm")
MINI_OP(OP_AND_IMM,    "and_imm")
MINI_OP(OP_OR_IMM,     "or_imm")
MINI_OP(OP_XOR_IMM,    "xor_imm")
MINI_OP(OP_SHL_IMM,    "shl_imm")
MINI_OP(OP_SHR_IMM,    "shr_imm")
MINI_OP(OP_SHR_UN_IMM, "shr_un_imm")

MINI_OP(OP_LADD,    "long_add")
MINI_OP(OP_LSUB,    "long_sub")
MINI_OP(OP_LMUL,    "long_mul")
MINI_OP(OP_LDIV,    "long_div")
MINI_OP(OP_LDIV_UN, "long_div_un")
MINI_OP(OP_LREM,    "long_rem")
MINI_OP(OP_LREM_UN, "long_rem_un")
MINI_OP(OP_LAND,    "long_and")
MINI_OP(OP_LOR,     "long_or")
MINI_OP(OP_LXOR,    "long_xor")
MINI_OP(OP_LSHL,    "long_shl")
MINI_OP(OP_LSHR,    "long_shr")
MINI_OP(OP_LSHR_UN, "long_shr_un")

MINI_OP(OP_LNEG,       "long_neg")
MINI_OP(OP_LNOT,       "long_not")
MINI_OP(OP_LCONV_TO_I1,"long_conv_to_i1")
MINI_OP(OP_LCONV_TO_I2,"long_conv_to_i2")
MINI_OP(OP_LCONV_TO_I4,"long_conv_to_i4")
MINI_OP(OP_LCONV_TO_I8,"long_conv_to_i8")
MINI_OP(OP_LCONV_TO_R4,"long_conv_to_r4")
MINI_OP(OP_LCONV_TO_R8,"long_conv_to_r8")
MINI_OP(OP_LCONV_TO_U4,"long_conv_to_u4")
MINI_OP(OP_LCONV_TO_U8,"long_conv_to_u8")

MINI_OP(OP_LCONV_TO_U2,   "long_conv_to_u2")
MINI_OP(OP_LCONV_TO_U1,   "long_conv_to_u1")
MINI_OP(OP_LCONV_TO_I,    "long_conv_to_i")

#if 0
MINI_OP(OP_LCONV_TO_OVF_I,"long_conv_to_ovf_i")
MINI_OP(OP_LCONV_TO_OVF_U,"long_conv_to_ovf_u")
MINI_OP(OP_LADD_OVF,      "long_add_ovf")
MINI_OP(OP_LADD_OVF_UN,   "long_add_ovf_un")
MINI_OP(OP_LMUL_OVF,      "long_mul_ovf")
MINI_OP(OP_LMUL_OVF_UN,   "long_mul_ovf_un")
MINI_OP(OP_LSUB_OVF,      "long_sub_ovf")
MINI_OP(OP_LSUB_OVF_UN,   "long_sub_ovf_un")

MINI_OP(OP_LCONV_TO_OVF_I1_UN,"long_conv_to_ovf_i1_un")
MINI_OP(OP_LCONV_TO_OVF_I2_UN,"long_conv_to_ovf_i2_un")
MINI_OP(OP_LCONV_TO_OVF_I4_UN,"long_conv_to_ovf_i4_un")
MINI_OP(OP_LCONV_TO_OVF_I8_UN,"long_conv_to_ovf_i8_un")
MINI_OP(OP_LCONV_TO_OVF_U1_UN,"long_conv_to_ovf_u1_un")
MINI_OP(OP_LCONV_TO_OVF_U2_UN,"long_conv_to_ovf_u2_un")
MINI_OP(OP_LCONV_TO_OVF_U4_UN,"long_conv_to_ovf_u4_un")
MINI_OP(OP_LCONV_TO_OVF_U8_UN,"long_conv_to_ovf_u8_un")
MINI_OP(OP_LCONV_TO_OVF_I_UN, "long_conv_to_ovf_i_un")
MINI_OP(OP_LCONV_TO_OVF_U_UN, "long_conv_to_ovf_u_un")

MINI_OP(OP_LCONV_TO_OVF_I1,"long_conv_to_ovf_i1")
MINI_OP(OP_LCONV_TO_OVF_U1,"long_conv_to_ovf_u1")
MINI_OP(OP_LCONV_TO_OVF_I2,"long_conv_to_ovf_i2")
MINI_OP(OP_LCONV_TO_OVF_U2,"long_conv_to_ovf_u2")
MINI_OP(OP_LCONV_TO_OVF_I4,"long_conv_to_ovf_i4")
MINI_OP(OP_LCONV_TO_OVF_U4,"long_conv_to_ovf_u4")
MINI_OP(OP_LCONV_TO_OVF_I8,"long_conv_to_ovf_i8")
MINI_OP(OP_LCONV_TO_OVF_U8,"long_conv_to_ovf_u8")
#endif

MINI_OP(OP_LCONV_TO_R_UN,"long_conv_to_r_un")
MINI_OP(OP_LCONV_TO_U,   "long_conv_to_u")
MINI_OP(OP_LSHR_IMM,	 "long_shr_imm")
MINI_OP(OP_LSHR_UN_IMM,  "long_shr_un_imm")
MINI_OP(OP_LSHL_IMM,     "long_shl_imm")
MINI_OP(OP_LADD_IMM,     "long_add_imm")
MINI_OP(OP_LSUB_IMM,     "long_sub_imm")

#if 0
MINI_OP(OP_IMUL_OVF,    "int_mul_ovf")
MINI_OP(OP_IMUL_OVF_UN, "int_mul_ovf_un")
#endif

MINI_OP(OP_IADD,    "int_add")
MINI_OP(OP_ISUB,    "int_sub")
MINI_OP(OP_IMUL,    "int_mul")
MINI_OP(OP_IDIV,    "int_div")
MINI_OP(OP_IDIV_UN, "int_div_un")
MINI_OP(OP_IREM,    "int_rem")
MINI_OP(OP_IREM_UN, "int_rem_un")
MINI_OP(OP_IAND,    "int_and")
MINI_OP(OP_IOR,     "int_or")
MINI_OP(OP_IXOR,    "int_xor")
MINI_OP(OP_ISHL,    "int_shl")
MINI_OP(OP_ISHR,    "int_shr")
MINI_OP(OP_ISHR_UN, "int_shr_un")
MINI_OP(OP_IADC,     "int_adc")
MINI_OP(OP_IADC_IMM, "int_adc_imm")
MINI_OP(OP_ISBB,     "int_sbb")
MINI_OP(OP_ISBB_IMM, "int_sbb_imm")
MINI_OP(OP_IADDCC,   "int_addcc")
MINI_OP(OP_ISUBCC,   "int_subcc")

MINI_OP(OP_IADD_IMM,    "int_add_imm")
MINI_OP(OP_ISUB_IMM,    "int_sub_imm")
MINI_OP(OP_IMUL_IMM,    "int_mul_imm")
MINI_OP(OP_IDIV_IMM,    "int_div_imm")
MINI_OP(OP_IDIV_UN_IMM, "int_div_un_imm")
MINI_OP(OP_IREM_IMM,    "int_rem_imm")
MINI_OP(OP_IREM_UN_IMM, "int_rem_un_imm")
MINI_OP(OP_IAND_IMM,    "int_and_imm")
MINI_OP(OP_IOR_IMM,     "int_or_imm")
MINI_OP(OP_IXOR_IMM,    "int_xor_imm")
MINI_OP(OP_ISHL_IMM,    "int_shl_imm")
MINI_OP(OP_ISHR_IMM,    "int_shr_imm")
MINI_OP(OP_ISHR_UN_IMM, "int_shr_un_imm")

MINI_OP(OP_INEG,       "int_neg")
MINI_OP(OP_INOT,       "int_not")

MINI_OP(OP_LSHR_UN_32, "long_shr_un_32")

MINI_OP(OP_FADD,   "float_add")
MINI_OP(OP_FSUB,   "float_sub")
MINI_OP(OP_FMUL,   "float_mul")
MINI_OP(OP_FDIV,   "float_div")
MINI_OP(OP_FDIV_UN,"float_div_un")
MINI_OP(OP_FREM,   "float_rem")
MINI_OP(OP_FREM_UN,"float_rem_un")

MINI_OP(OP_FNEG,       "float_neg")
MINI_OP(OP_FNOT,       "float_not")
MINI_OP(OP_FCONV_TO_I1,"float_conv_to_i1")
MINI_OP(OP_FCONV_TO_I2,"float_conv_to_i2")
MINI_OP(OP_FCONV_TO_I4,"float_conv_to_i4")
MINI_OP(OP_FCONV_TO_I8,"float_conv_to_i8")
MINI_OP(OP_FCONV_TO_R4,"float_conv_to_r4")
MINI_OP(OP_FCONV_TO_R8,"float_conv_to_r8")
MINI_OP(OP_FCONV_TO_U4,"float_conv_to_u4")
MINI_OP(OP_FCONV_TO_U8,"float_conv_to_u8")

MINI_OP(OP_FCONV_TO_U2,   "float_conv_to_u2")
MINI_OP(OP_FCONV_TO_U1,   "float_conv_to_u1")
MINI_OP(OP_FCONV_TO_I,    "float_conv_to_i")


#if 0
MINI_OP(OP_FCONV_TO_OVF_I,"float_conv_to_ovf_i")
MINI_OP(OP_FCONV_TO_OVF_U,"float_conv_to_ovd_u")
MINI_OP(OP_FADD_OVF,      "float_add_ovf")
MINI_OP(OP_FADD_OVF_UN,   "float_add_ovf_un")
MINI_OP(OP_FMUL_OVF,      "float_mul_ovf")
MINI_OP(OP_FMUL_OVF_UN,   "float_mul_ovf_un")
MINI_OP(OP_FSUB_OVF,      "float_sub_ovf")
MINI_OP(OP_FSUB_OVF_UN,   "float_sub_ovf_un")

MINI_OP(OP_FCONV_TO_OVF_I1_UN,"float_conv_to_ovf_i1_un")
MINI_OP(OP_FCONV_TO_OVF_I2_UN,"float_conv_to_ovf_i2_un")
MINI_OP(OP_FCONV_TO_OVF_I4_UN,"float_conv_to_ovf_i4_un")
MINI_OP(OP_FCONV_TO_OVF_I8_UN,"float_conv_to_ovf_i8_un")
MINI_OP(OP_FCONV_TO_OVF_U1_UN,"float_conv_to_ovf_u1_un")
MINI_OP(OP_FCONV_TO_OVF_U2_UN,"float_conv_to_ovf_u2_un")
MINI_OP(OP_FCONV_TO_OVF_U4_UN,"float_conv_to_ovf_u4_un")
MINI_OP(OP_FCONV_TO_OVF_U8_UN,"float_conv_to_ovf_u8_un")
MINI_OP(OP_FCONV_TO_OVF_I_UN, "float_conv_to_ovf_i_un")
MINI_OP(OP_FCONV_TO_OVF_U_UN, "float_conv_to_ovf_u_un")

MINI_OP(OP_FCONV_TO_OVF_I1,"float_conv_to_ovf_i1")
MINI_OP(OP_FCONV_TO_OVF_U1,"float_conv_to_ovf_u1")
MINI_OP(OP_FCONV_TO_OVF_I2,"float_conv_to_ovf_i2")
MINI_OP(OP_FCONV_TO_OVF_U2,"float_conv_to_ovf_u2")
MINI_OP(OP_FCONV_TO_OVF_I4,"float_conv_to_ovf_i4")
MINI_OP(OP_FCONV_TO_OVF_U4,"float_conv_to_ovf_u4")
MINI_OP(OP_FCONV_TO_OVF_I8,"float_conv_to_ovf_i8")
MINI_OP(OP_FCONV_TO_OVF_U8,"float_conv_to_ovf_u8")
#endif


MINI_OP(OP_FCONV_TO_U,	"float_conv_to_u")

MINI_OP(OP_BIGMUL, "op_bigmul")
MINI_OP(OP_BIGMUL_UN, "op_bigmul_un")

MINI_OP(OP_ADC,     "adc")
MINI_OP(OP_ADC_IMM, "adc_imm")
MINI_OP(OP_SBB,     "sbb")
MINI_OP(OP_SBB_IMM, "sbb_imm")
MINI_OP(OP_ADDCC,   "addcc")
MINI_OP(OP_ADDCC_IMM,   "addcc_imm")
MINI_OP(OP_SUBCC,   "subcc")
MINI_OP(OP_SUBCC_IMM,   "subcc_imm")
MINI_OP(OP_BR_REG,  "br_reg")
MINI_OP(OP_SEXT_I1,  "sext_i1")
MINI_OP(OP_SEXT_I2,  "sext_i2")
MINI_OP(OP_CNE,      "cne")


#if 0
MINI_OP(OP_ADD_OVF_CARRY,   "add_ovf_carry")
MINI_OP(OP_SUB_OVF_CARRY,   "sub_ovf_carry")
MINI_OP(OP_ADD_OVF_UN_CARRY,   "add_ovf_un_carry")
MINI_OP(OP_SUB_OVF_UN_CARRY,   "sub_ovf_un_carry")
#endif

MINI_OP(OP_SIN,     "sin")
MINI_OP(OP_COS,     "cos")
MINI_OP(OP_ABS,     "abs")
MINI_OP(OP_TAN,     "tan")
MINI_OP(OP_ATAN,    "atan")
MINI_OP(OP_SQRT,    "sqrt")

