
/*
 * List of LLVM intrinsics
 *
 * INTRINS(id, llvm_id, arch, llvm_argument_type)
 *   To define a simple intrinsic
 * INTRINS_OVR(id, llvm_id, arch, llvm_argument_type)
 *   To define an overloaded intrinsic with a single argument
 * INTRINS_OVR_2_ARG(id, llvm_id, arch, llvm_argument_type1, llvm_argument_type2)
 *   To define an overloaded intrinsic with two arguments
 * INTRINS_OVR_3_ARG(id, llvm_id, arch, llvm_argument_type1, llvm_argument_type2, llvm_argument_type3)
 * 	 To define an overloaded intrinsic with three arguments
 */

#define Scalar INTRIN_scalar
#define V64 INTRIN_vector64
#define V128 INTRIN_vector128
#define I1 INTRIN_int8
#define I2 INTRIN_int16
#define I4 INTRIN_int32
#define I8 INTRIN_int64
#define R4 INTRIN_float32
#define R8 INTRIN_float64
#define Ftoi INTRIN_kind_ftoi
#define Widen INTRIN_kind_widen
#define WidenAcross INTRIN_kind_widen_across
#define Across INTRIN_kind_across
#define Arm64DotProd INTRIN_kind_arm64_dot_prod
#define AddPointer INTRIN_kind_add_pointer
#if !defined(Generic)
#define Generic
#endif
#if !defined(X86)
#define X86
#endif
#if !defined(Arm64)
#define Arm64
#endif
#if !defined(Wasm)
#define Wasm
#endif

INTRINS_OVR_2_ARG(MEMSET, memset, Generic, LLVMPointerType (LLVMInt8Type (), 0), LLVMInt32Type ())
INTRINS_OVR_3_ARG(MEMCPY, memcpy, Generic, LLVMPointerType (LLVMInt8Type (), 0), LLVMPointerType (LLVMInt8Type (), 0), LLVMInt32Type () )
INTRINS_OVR_3_ARG(MEMMOVE, memmove, Generic, LLVMPointerType (LLVMInt8Type (), 0), LLVMPointerType (LLVMInt8Type (), 0), LLVMInt64Type ())
INTRINS_OVR(SADD_OVF_I32, sadd_with_overflow, Generic, LLVMInt32Type ())
INTRINS_OVR(UADD_OVF_I32, uadd_with_overflow, Generic, LLVMInt32Type ())
INTRINS_OVR(SSUB_OVF_I32, ssub_with_overflow, Generic, LLVMInt32Type ())
INTRINS_OVR(USUB_OVF_I32, usub_with_overflow, Generic, LLVMInt32Type ())
INTRINS_OVR(SMUL_OVF_I32, smul_with_overflow, Generic, LLVMInt32Type ())
INTRINS_OVR(UMUL_OVF_I32, umul_with_overflow, Generic, LLVMInt32Type ())
INTRINS_OVR(SADD_OVF_I64, sadd_with_overflow, Generic, LLVMInt64Type ())
INTRINS_OVR(UADD_OVF_I64, uadd_with_overflow, Generic, LLVMInt64Type ())
INTRINS_OVR(SSUB_OVF_I64, ssub_with_overflow, Generic, LLVMInt64Type ())
INTRINS_OVR(USUB_OVF_I64, usub_with_overflow, Generic, LLVMInt64Type ())
INTRINS_OVR(SMUL_OVF_I64, smul_with_overflow, Generic, LLVMInt64Type ())
INTRINS_OVR(UMUL_OVF_I64, umul_with_overflow, Generic, LLVMInt64Type ())
INTRINS_OVR(SIN, sin, Generic, LLVMDoubleType ())
INTRINS_OVR(COS, cos, Generic, LLVMDoubleType ())
INTRINS_OVR(SQRT, sqrt, Generic, LLVMDoubleType ())
INTRINS_OVR(FLOOR, floor, Generic, LLVMDoubleType ())
INTRINS_OVR(FLOORF, floor, Generic, LLVMFloatType ())
INTRINS_OVR(CEIL, ceil, Generic, LLVMDoubleType ())
INTRINS_OVR(CEILF, ceil, Generic, LLVMFloatType ())
INTRINS_OVR(FMA, fma, Generic, LLVMDoubleType ())
INTRINS_OVR(FMAF, fma, Generic, LLVMFloatType ())
	/* This isn't an intrinsic, instead llvm seems to special case it by name */
INTRINS_OVR(FABS, fabs, Generic, LLVMDoubleType ())
INTRINS_OVR(ABSF, fabs, Generic, LLVMFloatType ())
INTRINS_OVR(SINF, sin, Generic, LLVMFloatType ())
INTRINS_OVR(COSF, cos, Generic, LLVMFloatType ())
INTRINS_OVR(SQRTF, sqrt, Generic, LLVMFloatType ())
INTRINS_OVR(POWF, pow, Generic, LLVMFloatType ())
INTRINS_OVR(POW, pow, Generic, LLVMDoubleType ())
INTRINS_OVR(EXP, exp, Generic, LLVMDoubleType ())
INTRINS_OVR(EXPF, exp, Generic, LLVMFloatType ())
INTRINS_OVR(LOG, log, Generic, LLVMDoubleType ())
INTRINS_OVR(LOG2, log2, Generic, LLVMDoubleType ())
INTRINS_OVR(LOG2F, log2, Generic, LLVMFloatType ())
INTRINS_OVR(LOG10, log10, Generic, LLVMDoubleType ())
INTRINS_OVR(LOG10F, log10, Generic, LLVMFloatType ())
INTRINS_OVR(TRUNC, trunc, Generic, LLVMDoubleType ())
INTRINS_OVR(TRUNCF, trunc, Generic, LLVMFloatType ())
INTRINS_OVR(COPYSIGN, copysign, Generic, LLVMDoubleType ())
INTRINS_OVR(COPYSIGNF, copysign, Generic, LLVMFloatType ())
INTRINS_OVR(EXPECT_I8, expect, Generic, LLVMInt8Type ())
INTRINS_OVR(EXPECT_I1, expect, Generic, LLVMInt1Type ())
INTRINS_OVR(CTPOP_I32, ctpop, Generic, LLVMInt32Type ())
INTRINS_OVR(CTPOP_I64, ctpop, Generic, LLVMInt64Type ())
INTRINS_OVR(CTLZ_I32, ctlz, Generic, LLVMInt32Type ())
INTRINS_OVR(CTLZ_I64, ctlz, Generic, LLVMInt64Type ())
INTRINS_OVR(CTTZ_I32, cttz, Generic, LLVMInt32Type ())
INTRINS_OVR(CTTZ_I64, cttz, Generic, LLVMInt64Type ())
INTRINS_OVR(PREFETCH, prefetch, Generic, LLVMPointerType (i1_t, 0))
INTRINS(BZHI_I32, x86_bmi_bzhi_32, X86)
INTRINS(BZHI_I64, x86_bmi_bzhi_64, X86)
INTRINS(BEXTR_I32, x86_bmi_bextr_32, X86)
INTRINS(BEXTR_I64, x86_bmi_bextr_64, X86)
INTRINS(PEXT_I32, x86_bmi_pext_32, X86)
INTRINS(PEXT_I64, x86_bmi_pext_64, X86)
INTRINS(PDEP_I32, x86_bmi_pdep_32, X86)
INTRINS(PDEP_I64, x86_bmi_pdep_64, X86)

INTRINS_OVR(SIMD_SQRT_R8, sqrt, Generic, sse_r8_t)
INTRINS_OVR(SIMD_SQRT_R4, sqrt, Generic, sse_r4_t)
INTRINS_OVR_TAG(SIMD_FLOOR, floor, Generic, Scalar | V64 | V128 | R4 | R8)
INTRINS_OVR_TAG(SIMD_CEIL, ceil, Generic, Scalar | V64 | V128 | R4 | R8)
INTRINS_OVR_TAG(SIMD_TRUNC, trunc, Generic, Scalar | V64 | V128 | R4 | R8)
INTRINS_OVR_TAG(SIMD_ROUND, round, Generic, Scalar | V64 | V128 | R4 | R8)
INTRINS_OVR_TAG(SIMD_NEAREST, nearbyint, Generic, V64 | V128 | R4 | R8)
INTRINS(EH_TYPEID_FOR, eh_typeid_for, Generic)
INTRINS_OVR_TAG(ROUNDEVEN, roundeven, Generic, Scalar | V64 | V128 | R4 | R8)

#if defined(TARGET_AMD64) || defined(TARGET_X86)
INTRINS(SSE_PMOVMSKB, x86_sse2_pmovmskb_128, X86)
INTRINS(SSE_MOVMSK_PS, x86_sse_movmsk_ps, X86)
INTRINS(SSE_MOVMSK_PD, x86_sse2_movmsk_pd, X86)
INTRINS(SSE_PSRLI_W, x86_sse2_psrli_w, X86)
INTRINS(SSE_PSRAI_W, x86_sse2_psrai_w, X86)
INTRINS(SSE_PSLLI_W, x86_sse2_pslli_w, X86)
INTRINS(SSE_PSRLI_D, x86_sse2_psrli_d, X86)
INTRINS(SSE_PSRAI_D, x86_sse2_psrai_d, X86)
INTRINS(SSE_PSLLI_D, x86_sse2_pslli_d, X86)
INTRINS(SSE_PSRLI_Q, x86_sse2_psrli_q, X86)
INTRINS(SSE_PSLLI_Q, x86_sse2_pslli_q, X86)
INTRINS(SSE_PSRL_W, x86_sse2_psrl_w, X86)
INTRINS(SSE_PSRA_W, x86_sse2_psra_w, X86)
INTRINS(SSE_PSRL_D, x86_sse2_psrl_d, X86)
INTRINS(SSE_PSRA_D, x86_sse2_psra_d, X86)
INTRINS(SSE_PSRL_Q, x86_sse2_psrl_q, X86)
INTRINS(SSE_PSLL_W, x86_sse2_psll_w, X86)
INTRINS(SSE_PSLL_D, x86_sse2_psll_d, X86)
INTRINS(SSE_PSLL_Q, x86_sse2_psll_q, X86)
INTRINS_OVR(SSE_SQRT_SD, sqrt, Generic, LLVMDoubleType ())
INTRINS_OVR(SSE_SQRT_SS, sqrt, Generic, LLVMFloatType ())
INTRINS(SSE_RCP_PS, x86_sse_rcp_ps, X86)
INTRINS(SSE_RSQRT_PS, x86_sse_rsqrt_ps, X86)
INTRINS(SSE_RCP_SS, x86_sse_rcp_ss, X86)
INTRINS(SSE_RSQRT_SS, x86_sse_rsqrt_ss, X86)
INTRINS(SSE_CVTTPD2DQ, x86_sse2_cvttpd2dq, X86)
INTRINS(SSE_CVTTPS2DQ, x86_sse2_cvttps2dq, X86)
INTRINS(SSE_CVTPD2DQ, x86_sse2_cvtpd2dq, X86)
INTRINS(SSE_CVTPS2DQ, x86_sse2_cvtps2dq, X86)
INTRINS(SSE_CVTPD2PS, x86_sse2_cvtpd2ps, X86)
INTRINS(SSE_CVTSS2SI, x86_sse_cvtss2si, X86)
INTRINS(SSE_CVTSS2SI64, x86_sse_cvtss2si64, X86)
INTRINS(SSE_CVTTSS2SI, x86_sse_cvttss2si, X86)
INTRINS(SSE_CVTTSS2SI64, x86_sse_cvttss2si64, X86)
INTRINS(SSE_CVTSD2SI, x86_sse2_cvtsd2si, X86)
INTRINS(SSE_CVTTSD2SI, x86_sse2_cvttsd2si, X86)
INTRINS(SSE_CVTSD2SI64, x86_sse2_cvtsd2si64, X86)
INTRINS(SSE_CVTTSD2SI64, x86_sse2_cvttsd2si64, X86)
INTRINS(SSE_CVTSD2SS, x86_sse2_cvtsd2ss, X86)
INTRINS(SSE_CMPPD, x86_sse2_cmp_pd, X86)
INTRINS(SSE_CMPPS, x86_sse_cmp_ps, X86)
INTRINS(SSE_CMPSS, x86_sse_cmp_ss, X86)
INTRINS(SSE_CMPSD, x86_sse2_cmp_sd, X86)
INTRINS(SSE_COMIEQ_SS, x86_sse_comieq_ss, X86)
INTRINS(SSE_COMIGT_SS, x86_sse_comigt_ss, X86)
INTRINS(SSE_COMIGE_SS, x86_sse_comige_ss, X86)
INTRINS(SSE_COMILT_SS, x86_sse_comilt_ss, X86)
INTRINS(SSE_COMILE_SS, x86_sse_comile_ss, X86)
INTRINS(SSE_COMINEQ_SS, x86_sse_comineq_ss, X86)
INTRINS(SSE_UCOMIEQ_SS, x86_sse_ucomieq_ss, X86)
INTRINS(SSE_UCOMIGT_SS, x86_sse_ucomigt_ss, X86)
INTRINS(SSE_UCOMIGE_SS, x86_sse_ucomige_ss, X86)
INTRINS(SSE_UCOMILT_SS, x86_sse_ucomilt_ss, X86)
INTRINS(SSE_UCOMILE_SS, x86_sse_ucomile_ss, X86)
INTRINS(SSE_UCOMINEQ_SS, x86_sse_ucomineq_ss, X86)
INTRINS(SSE_COMIEQ_SD, x86_sse2_comieq_sd, X86)
INTRINS(SSE_COMIGT_SD, x86_sse2_comigt_sd, X86)
INTRINS(SSE_COMIGE_SD, x86_sse2_comige_sd, X86)
INTRINS(SSE_COMILT_SD, x86_sse2_comilt_sd, X86)
INTRINS(SSE_COMILE_SD, x86_sse2_comile_sd, X86)
INTRINS(SSE_COMINEQ_SD, x86_sse2_comineq_sd, X86)
INTRINS(SSE_UCOMIEQ_SD, x86_sse2_ucomieq_sd, X86)
INTRINS(SSE_UCOMIGT_SD, x86_sse2_ucomigt_sd, X86)
INTRINS(SSE_UCOMIGE_SD, x86_sse2_ucomige_sd, X86)
INTRINS(SSE_UCOMILT_SD, x86_sse2_ucomilt_sd, X86)
INTRINS(SSE_UCOMILE_SD, x86_sse2_ucomile_sd, X86)
INTRINS(SSE_UCOMINEQ_SD, x86_sse2_ucomineq_sd, X86)
INTRINS(SSE_PACKSSWB, x86_sse2_packsswb_128, X86)
INTRINS(SSE_PACKUSWB, x86_sse2_packuswb_128, X86)
INTRINS(SSE_PACKSSDW, x86_sse2_packssdw_128, X86)
INTRINS(SSE_PACKUSDW, x86_sse41_packusdw, X86)
INTRINS(SSE_MINPS, x86_sse_min_ps, X86)
INTRINS(SSE_MAXPS, x86_sse_max_ps, X86)
INTRINS(SSE_MINSS, x86_sse_min_ss, X86)
INTRINS(SSE_MAXSS, x86_sse_max_ss, X86)
INTRINS(SSE_HADDPS, x86_sse3_hadd_ps, X86)
INTRINS(SSE_HSUBPS, x86_sse3_hsub_ps, X86)
INTRINS(SSE_ADDSUBPS, x86_sse3_addsub_ps, X86)
INTRINS(SSE_MINPD, x86_sse2_min_pd, X86)
INTRINS(SSE_MAXPD, x86_sse2_max_pd, X86)
INTRINS(SSE_MAXSD, x86_sse2_max_sd, X86)
INTRINS(SSE_MINSD, x86_sse2_min_sd, X86)
INTRINS(SSE_HADDPD, x86_sse3_hadd_pd, X86)
INTRINS(SSE_HSUBPD, x86_sse3_hsub_pd, X86)
INTRINS(SSE_ADDSUBPD, x86_sse3_addsub_pd, X86)
INTRINS(SSE_PMULHW, x86_sse2_pmulh_w, X86)
INTRINS(SSE_PMULHU, x86_sse2_pmulhu_w, X86)
INTRINS(SSE_PMULHUW, x86_sse2_pmulhu_w, X86)
INTRINS(SSE_PMADDWD, x86_sse2_pmadd_wd, X86)
INTRINS(SSE_PSADBW, x86_sse2_psad_bw, X86)
INTRINS(SSE_PAUSE, x86_sse2_pause, X86)
INTRINS(SSE_MASKMOVDQU, x86_sse2_maskmov_dqu, X86)
INTRINS(SSE_PSHUFB, x86_ssse3_pshuf_b_128, X86)
INTRINS(SSE_DPPS, x86_sse41_dpps, X86)
INTRINS(SSE_DPPD, x86_sse41_dppd, X86)
INTRINS(SSE_ROUNDSS, x86_sse41_round_ss, X86)
INTRINS(SSE_ROUNDSD, x86_sse41_round_sd, X86)
INTRINS(SSE_ROUNDPS, x86_sse41_round_ps, X86)
INTRINS(SSE_ROUNDPD, x86_sse41_round_pd, X86)
INTRINS(SSE_PTESTZ, x86_sse41_ptestz, X86)
INTRINS(SSE_INSERTPS, x86_sse41_insertps, X86)
INTRINS(SSE_SFENCE, x86_sse_sfence, X86)
INTRINS(SSE_MFENCE, x86_sse2_mfence, X86)
INTRINS(SSE_LFENCE, x86_sse2_lfence, X86)
INTRINS(SSE_LDU_DQ, x86_sse3_ldu_dq, X86)
INTRINS(SSE_PHADDW, x86_ssse3_phadd_w_128, X86)
INTRINS(SSE_PHADDD, x86_ssse3_phadd_d_128, X86)
INTRINS(SSE_PHADDSW, x86_ssse3_phadd_sw_128, X86)
INTRINS(SSE_PHSUBW, x86_ssse3_phsub_w_128, X86)
INTRINS(SSE_PHSUBD, x86_ssse3_phsub_d_128, X86)
INTRINS(SSE_PHSUBSW, x86_ssse3_phsub_sw_128, X86)
INTRINS(SSE_PMADDUBSW, x86_ssse3_pmadd_ub_sw_128, X86)
INTRINS(SSE_PMULHRSW, x86_ssse3_pmul_hr_sw_128, X86)
INTRINS(SSE_PSIGNB, x86_ssse3_psign_b_128, X86)
INTRINS(SSE_PSIGNW, x86_ssse3_psign_w_128, X86)
INTRINS(SSE_PSIGND, x86_ssse3_psign_d_128, X86)
INTRINS(SSE_CRC32_32_8, x86_sse42_crc32_32_8, X86)
INTRINS(SSE_CRC32_32_16, x86_sse42_crc32_32_16, X86)
INTRINS(SSE_CRC32_32_32, x86_sse42_crc32_32_32, X86)
INTRINS(SSE_CRC32_64_64, x86_sse42_crc32_64_64, X86)
INTRINS(SSE_TESTC, x86_sse41_ptestc, X86)
INTRINS(SSE_TESTNZ, x86_sse41_ptestnzc, X86)
INTRINS(SSE_TESTZ, x86_sse41_ptestz, X86)
INTRINS(SSE_PBLENDVB, x86_sse41_pblendvb, X86)
INTRINS(SSE_BLENDVPS, x86_sse41_blendvps, X86)
INTRINS(SSE_BLENDVPD, x86_sse41_blendvpd, X86)
INTRINS(SSE_PHMINPOSUW, x86_sse41_phminposuw, X86)
INTRINS(SSE_MPSADBW, x86_sse41_mpsadbw, X86)
INTRINS(PCLMULQDQ, x86_pclmulqdq, X86)
INTRINS(AESNI_AESKEYGENASSIST, x86_aesni_aeskeygenassist, X86)
INTRINS(AESNI_AESDEC, x86_aesni_aesdec, X86)
INTRINS(AESNI_AESDECLAST, x86_aesni_aesdeclast, X86)
INTRINS(AESNI_AESENC, x86_aesni_aesenc, X86)
INTRINS(AESNI_AESENCLAST, x86_aesni_aesenclast, X86)
INTRINS(AESNI_AESIMC, x86_aesni_aesimc, X86)

INTRINS_OVR(SSE_SSUB_SATI8, ssub_sat, Generic, v128_i1_t)
INTRINS_OVR(SSE_USUB_SATI8, usub_sat, Generic, v128_i1_t)
INTRINS_OVR(SSE_SSUB_SATI16, ssub_sat, Generic, v128_i2_t)
INTRINS_OVR(SSE_USUB_SATI16, usub_sat, Generic, v128_i2_t)
#endif
#if defined(TARGET_AMD64) || defined(TARGET_X86) || defined(TARGET_WASM)
INTRINS_OVR(SSE_SADD_SATI8, sadd_sat, Generic, v128_i1_t)
INTRINS_OVR(SSE_UADD_SATI8, uadd_sat, Generic, v128_i1_t)
INTRINS_OVR(SSE_SADD_SATI16, sadd_sat, Generic, v128_i2_t)
INTRINS_OVR(SSE_UADD_SATI16, uadd_sat, Generic, v128_i2_t)
#endif
#if defined(TARGET_ARM64) || defined(TARGET_WASM)
INTRINS_OVR_TAG(SIMD_POPCNT, ctpop, Generic, V64 | V128 | I1)
#endif
#if defined(TARGET_WASM)
INTRINS_OVR(WASM_EXTADD_PAIRWISE_SIGNED_V16, wasm_extadd_pairwise_signed, Wasm, sse_i2_t)
INTRINS_OVR(WASM_EXTADD_PAIRWISE_SIGNED_V8, wasm_extadd_pairwise_signed, Wasm, sse_i4_t)
INTRINS_OVR(WASM_EXTADD_PAIRWISE_UNSIGNED_V16, wasm_extadd_pairwise_unsigned, Wasm, sse_i2_t)
INTRINS_OVR(WASM_EXTADD_PAIRWISE_UNSIGNED_V8, wasm_extadd_pairwise_unsigned, Wasm, sse_i4_t)
INTRINS_OVR(WASM_ALLTRUE_V16, wasm_alltrue, Wasm, sse_i1_t)
INTRINS_OVR(WASM_ALLTRUE_V8, wasm_alltrue, Wasm, sse_i2_t)
INTRINS_OVR(WASM_ALLTRUE_V4, wasm_alltrue, Wasm, sse_i4_t)
INTRINS_OVR(WASM_ALLTRUE_V2, wasm_alltrue, Wasm, sse_i8_t)
INTRINS_OVR(WASM_ANYTRUE_V16, wasm_anytrue, Wasm, sse_i1_t)
INTRINS_OVR(WASM_ANYTRUE_V8, wasm_anytrue, Wasm, sse_i2_t)
INTRINS_OVR(WASM_ANYTRUE_V4, wasm_anytrue, Wasm, sse_i4_t)
INTRINS_OVR(WASM_ANYTRUE_V2, wasm_anytrue, Wasm, sse_i8_t)
INTRINS_OVR(WASM_AVERAGE_ROUNDED_V16, wasm_avgr_unsigned, Wasm, sse_i1_t)
INTRINS_OVR(WASM_AVERAGE_ROUNDED_V8, wasm_avgr_unsigned, Wasm, sse_i2_t)
INTRINS_OVR(WASM_BITMASK_V16, wasm_bitmask, Wasm, sse_i1_t)
INTRINS_OVR(WASM_BITMASK_V8, wasm_bitmask, Wasm, sse_i2_t)
INTRINS_OVR(WASM_BITMASK_V4, wasm_bitmask, Wasm, sse_i4_t)
INTRINS_OVR(WASM_BITMASK_V2, wasm_bitmask, Wasm, sse_i8_t)
INTRINS(WASM_DOT, wasm_dot, Wasm)
INTRINS_OVR(WASM_FABS_V4, fabs, Generic, sse_r4_t)
INTRINS_OVR(WASM_FABS_V2, fabs, Generic, sse_r8_t)
INTRINS_OVR_2_ARG(WASM_NARROW_SIGNED_V16, wasm_narrow_signed, Wasm, sse_i1_t, sse_i2_t)
INTRINS_OVR_2_ARG(WASM_NARROW_SIGNED_V8, wasm_narrow_signed, Wasm, sse_i2_t, sse_i4_t)
INTRINS_OVR_2_ARG(WASM_NARROW_UNSIGNED_V16, wasm_narrow_unsigned, Wasm, sse_i1_t, sse_i2_t)
INTRINS_OVR_2_ARG(WASM_NARROW_UNSIGNED_V8, wasm_narrow_unsigned, Wasm, sse_i2_t, sse_i4_t)
INTRINS_OVR_2_ARG(WASM_CONV_R8_TO_I4, fptosi_sat, Generic, v64_i4_t, v128_r8_t)
INTRINS_OVR_2_ARG(WASM_CONV_R8_TO_U4, fptoui_sat, Generic, v64_i4_t, v128_r8_t)
INTRINS_OVR_TAG(WASM_FMAX, maximum, Generic, V128 | R4 | R8)
INTRINS_OVR_TAG(WASM_FMIN, minimum, Generic, V128 | R4 | R8)
INTRINS_OVR_TAG(WASM_PMAX, wasm_pmax, Wasm, V128 | R4 | R8)
INTRINS_OVR_TAG(WASM_PMIN, wasm_pmin, Wasm, V128 | R4 | R8)
INTRINS_OVR(WASM_PMAX_V4, fabs, Generic, sse_r4_t)
INTRINS_OVR(WASM_PMAX_V2, fabs, Generic, sse_r8_t)
INTRINS(WASM_Q15MULR_SAT_SIGNED, wasm_q15mulr_sat_signed, Wasm)
INTRINS(WASM_SHUFFLE, wasm_shuffle, Wasm)
INTRINS_OVR(WASM_SUB_SAT_SIGNED_V16, wasm_sub_sat_signed, Wasm, sse_i1_t)
INTRINS_OVR(WASM_SUB_SAT_SIGNED_V8, wasm_sub_sat_signed, Wasm, sse_i2_t)
INTRINS_OVR(WASM_SUB_SAT_UNSIGNED_V16, wasm_sub_sat_unsigned, Wasm, sse_i1_t)
INTRINS_OVR(WASM_SUB_SAT_UNSIGNED_V8, wasm_sub_sat_unsigned, Wasm, sse_i2_t)
INTRINS(WASM_SWIZZLE, wasm_swizzle, Wasm)
INTRINS(WASM_GET_EXCEPTION, wasm_get_exception, Wasm)
INTRINS(WASM_GET_EHSELECTOR, wasm_get_ehselector, Wasm)
INTRINS(WASM_RETHROW, wasm_rethrow, Wasm)
#endif
#if defined(TARGET_ARM64)
INTRINS_OVR(BITREVERSE_I32, bitreverse, Generic, LLVMInt32Type ())
INTRINS_OVR(BITREVERSE_I64, bitreverse, Generic, LLVMInt64Type ())
INTRINS_OVR_TAG(BITREVERSE, bitreverse, Generic, V64 | V128 | I1 | I2 | I4 | I8)
INTRINS(AARCH64_CRC32B, aarch64_crc32b, Arm64)
INTRINS(AARCH64_CRC32H, aarch64_crc32h, Arm64)
INTRINS(AARCH64_CRC32W, aarch64_crc32w, Arm64)
INTRINS(AARCH64_CRC32X, aarch64_crc32x, Arm64)
INTRINS(AARCH64_CRC32CB, aarch64_crc32cb, Arm64)
INTRINS(AARCH64_CRC32CH, aarch64_crc32ch, Arm64)
INTRINS(AARCH64_CRC32CW, aarch64_crc32cw, Arm64)
INTRINS(AARCH64_CRC32CX, aarch64_crc32cx, Arm64)
INTRINS(AARCH64_AESD, aarch64_crypto_aesd, Arm64)
INTRINS(AARCH64_AESE, aarch64_crypto_aese, Arm64)
INTRINS(AARCH64_AESIMC, aarch64_crypto_aesimc, Arm64)
INTRINS(AARCH64_AESMC, aarch64_crypto_aesmc, Arm64)
INTRINS(AARCH64_SHA1C, aarch64_crypto_sha1c, Arm64)
INTRINS(AARCH64_SHA1H, aarch64_crypto_sha1h, Arm64)
INTRINS(AARCH64_SHA1M, aarch64_crypto_sha1m, Arm64)
INTRINS(AARCH64_SHA1P, aarch64_crypto_sha1p, Arm64)
INTRINS(AARCH64_SHA1SU0, aarch64_crypto_sha1su0, Arm64)
INTRINS(AARCH64_SHA1SU1, aarch64_crypto_sha1su1, Arm64)
INTRINS(AARCH64_SHA256SU0, aarch64_crypto_sha256su0, Arm64)
INTRINS(AARCH64_SHA256SU1, aarch64_crypto_sha256su1, Arm64)
INTRINS(AARCH64_SHA256H, aarch64_crypto_sha256h, Arm64)
INTRINS(AARCH64_SHA256H2, aarch64_crypto_sha256h2, Arm64)
INTRINS(AARCH64_PMULL64, aarch64_neon_pmull64, Arm64)
INTRINS(AARCH64_HINT, aarch64_hint, Arm64)

INTRINS_OVR_TAG_KIND(AARCH64_ADV_SIMD_FACGE, aarch64_neon_facge, Arm64, Ftoi, Scalar | V64 | V128 | I4 | I8)
INTRINS_OVR_TAG_KIND(AARCH64_ADV_SIMD_FACGT, aarch64_neon_facgt, Arm64, Ftoi, Scalar | V64 | V128 | I4 | I8)

INTRINS_OVR_TAG(AARCH64_ADV_SIMD_FABD_SCALAR, aarch64_sisd_fabd, Arm64, Scalar | R4 | R8)

INTRINS_OVR_TAG(AARCH64_ADV_SIMD_FABD, aarch64_neon_fabd, Arm64, V64 | V128 | R4 | R8)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_UABD, aarch64_neon_uabd, Arm64, V64 | V128 | I1 | I2 | I4)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_SABD, aarch64_neon_sabd, Arm64, V64 | V128 | I1 | I2 | I4)

INTRINS_OVR_TAG(AARCH64_ADV_SIMD_SQABS, aarch64_neon_sqabs, Arm64, Scalar | V64 | V128 | I1 | I2 | I4 | I8)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_FABS, fabs, Generic, Scalar | V64 | V128 | R4 | R8)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_ABS, aarch64_neon_abs, Arm64, Scalar | V64 | V128 | I1 | I2 | I4 | I8)

INTRINS_OVR_TAG_KIND(AARCH64_ADV_SIMD_UADDLV, aarch64_neon_uaddlv, Arm64, WidenAcross, V64 | V128 | I1 | I2 | I4)
INTRINS_OVR_TAG_KIND(AARCH64_ADV_SIMD_SADDLV, aarch64_neon_saddlv, Arm64, WidenAcross, V64 | V128 | I1 | I2 | I4)

INTRINS_OVR_TAG(AARCH64_ADV_SIMD_ADDP, aarch64_neon_addp, Arm64, V64 | V128 | I1 | I2 | I4 | I8)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_FADDP, aarch64_neon_faddp, Arm64, V64 | V128 | R4 | R8)

INTRINS_OVR_TAG_KIND(AARCH64_ADV_SIMD_FMAXNMV, aarch64_neon_fmaxnmv, Arm64, Across, V64 | V128 | R4 | R8)
INTRINS_OVR_TAG_KIND(AARCH64_ADV_SIMD_FMINNMV, aarch64_neon_fminnmv, Arm64, Across, V64 | V128 | R4 | R8)

INTRINS_OVR_TAG_KIND(AARCH64_ADV_SIMD_SADDV, aarch64_neon_saddv, Arm64, Across, V64 | V128 | I1 | I2 | I4 | I8)
INTRINS_OVR_TAG_KIND(AARCH64_ADV_SIMD_UADDV, aarch64_neon_uaddv, Arm64, Across, V64 | V128 | I1 | I2 | I4 | I8)
INTRINS_OVR_TAG_KIND(AARCH64_ADV_SIMD_FADDV, aarch64_neon_faddv, Arm64, Across, V64 | V128 | R4 | R8)

INTRINS_OVR_TAG_KIND(AARCH64_ADV_SIMD_LD2_V64, aarch64_neon_ld2, Arm64, AddPointer, V64 | I1 | I2 | I4 | R4)
INTRINS_OVR_TAG_KIND(AARCH64_ADV_SIMD_LD2_V128, aarch64_neon_ld2, Arm64, AddPointer, V128 | I1 | I2 | I4 | I8 | R4 | R8)
INTRINS_OVR_TAG_KIND(AARCH64_ADV_SIMD_SMAXV, aarch64_neon_smaxv, Arm64, Across, V64 | V128 | I1 | I2 | I4)
INTRINS_OVR_TAG_KIND(AARCH64_ADV_SIMD_UMAXV, aarch64_neon_umaxv, Arm64, Across, V64 | V128 | I1 | I2 | I4)
INTRINS_OVR_TAG_KIND(AARCH64_ADV_SIMD_SMINV, aarch64_neon_sminv, Arm64, Across, V64 | V128 | I1 | I2 | I4)
INTRINS_OVR_TAG_KIND(AARCH64_ADV_SIMD_UMINV, aarch64_neon_uminv, Arm64, Across, V64 | V128 | I1 | I2 | I4)
INTRINS_OVR_TAG_KIND(AARCH64_ADV_SIMD_FMAXV, aarch64_neon_fmaxv, Arm64, Across, V64 | V128 | R4 | R8)
INTRINS_OVR_TAG_KIND(AARCH64_ADV_SIMD_FMINV, aarch64_neon_fminv, Arm64, Across, V64 | V128 | R4 | R8)

INTRINS_OVR_TAG_KIND(AARCH64_ADV_SIMD_SADDLP, aarch64_neon_saddlp, Arm64, Widen, V64 | V128 | I1 | I2 | I4 | I8)
INTRINS_OVR_TAG_KIND(AARCH64_ADV_SIMD_UADDLP, aarch64_neon_uaddlp, Arm64, Widen, V64 | V128 | I1 | I2 | I4 | I8)

INTRINS_OVR_2_ARG(AARCH64_ADV_SIMD_FCVTXN, aarch64_neon_fcvtxn, Arm64, v64_r4_t, v128_r8_t)

INTRINS_OVR_TAG_KIND(AARCH64_ADV_SIMD_FCVTAS, aarch64_neon_fcvtas, Arm64, Ftoi, Scalar | V64 | V128 | I4 | I8)
INTRINS_OVR_TAG_KIND(AARCH64_ADV_SIMD_FCVTNS, aarch64_neon_fcvtns, Arm64, Ftoi, Scalar | V64 | V128 | I4 | I8)
INTRINS_OVR_TAG_KIND(AARCH64_ADV_SIMD_FCVTMS, aarch64_neon_fcvtms, Arm64, Ftoi, Scalar | V64 | V128 | I4 | I8)
INTRINS_OVR_TAG_KIND(AARCH64_ADV_SIMD_FCVTPS, aarch64_neon_fcvtps, Arm64, Ftoi, Scalar | V64 | V128 | I4 | I8)

INTRINS_OVR_TAG_KIND(AARCH64_ADV_SIMD_FCVTAU, aarch64_neon_fcvtau, Arm64, Ftoi, Scalar | V64 | V128 | I4 | I8)
INTRINS_OVR_TAG_KIND(AARCH64_ADV_SIMD_FCVTNU, aarch64_neon_fcvtnu, Arm64, Ftoi, Scalar | V64 | V128 | I4 | I8)
INTRINS_OVR_TAG_KIND(AARCH64_ADV_SIMD_FCVTMU, aarch64_neon_fcvtmu, Arm64, Ftoi, Scalar | V64 | V128 | I4 | I8)
INTRINS_OVR_TAG_KIND(AARCH64_ADV_SIMD_FCVTPU, aarch64_neon_fcvtpu, Arm64, Ftoi, Scalar | V64 | V128 | I4 | I8)

INTRINS_OVR_2_ARG(AARCH64_ADV_SIMD_SCALAR_SQXTUN, aarch64_neon_scalar_sqxtun, Arm64, i4_t, i8_t)
INTRINS_OVR_2_ARG(AARCH64_ADV_SIMD_SCALAR_SQXTN, aarch64_neon_scalar_sqxtn, Arm64, i4_t, i8_t)
INTRINS_OVR_2_ARG(AARCH64_ADV_SIMD_SCALAR_UQXTN, aarch64_neon_scalar_uqxtn, Arm64, i4_t, i8_t)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_SQXTUN, aarch64_neon_sqxtun, Arm64, V64 | I1 | I2 | I4)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_SQXTN, aarch64_neon_sqxtn, Arm64, V64 | I1 | I2 | I4)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_UQXTN, aarch64_neon_uqxtn, Arm64, V64 | I1 | I2 | I4)

INTRINS_OVR_TAG(AARCH64_ADV_SIMD_SRHADD, aarch64_neon_srhadd, Arm64, V64 | V128 | I1 | I2 | I4)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_URHADD, aarch64_neon_urhadd, Arm64, V64 | V128 | I1 | I2 | I4)

INTRINS_OVR_TAG(AARCH64_ADV_SIMD_FMA, fma, Generic, Scalar | V64 | V128 | R4 | R8)

INTRINS_OVR_TAG(AARCH64_ADV_SIMD_SHADD, aarch64_neon_shadd, Arm64, V64 | V128 | I1 | I2 | I4)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_UHADD, aarch64_neon_uhadd, Arm64, V64 | V128 | I1 | I2 | I4)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_SHSUB, aarch64_neon_shsub, Arm64, V64 | V128 | I1 | I2 | I4)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_UHSUB, aarch64_neon_uhsub, Arm64, V64 | V128 | I1 | I2 | I4)

INTRINS_OVR_TAG(AARCH64_ADV_SIMD_CLS, aarch64_neon_cls, Arm64, V64 | V128 | I1 | I2 | I4 | I8)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_CLZ, ctlz, Generic, V64 | V128 | I1 | I2 | I4 | I8)

INTRINS_OVR_TAG(AARCH64_ADV_SIMD_SMAX, aarch64_neon_smax, Arm64, V64 | V128 | I1 | I2 | I4 | I8)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_UMAX, aarch64_neon_umax, Arm64, V64 | V128 | I1 | I2 | I4 | I8)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_FMAX, aarch64_neon_fmax, Arm64, Scalar | V64 | V128 | R4 | R8)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_SMIN, aarch64_neon_smin, Arm64, V64 | V128 | I1 | I2 | I4 | I8)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_UMIN, aarch64_neon_umin, Arm64, V64 | V128 | I1 | I2 | I4 | I8)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_FMIN, aarch64_neon_fmin, Arm64, Scalar | V64 | V128 | R4 | R8)

INTRINS_OVR_TAG(AARCH64_ADV_SIMD_FMAXP, aarch64_neon_fmaxp, Arm64, V64 | V128 | R4 | R8)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_SMAXP, aarch64_neon_smaxp, Arm64, V64 | V128 | I1 | I2 | I4 | I8)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_UMAXP, aarch64_neon_umaxp, Arm64, V64 | V128 | I1 | I2 | I4 | I8)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_FMINP, aarch64_neon_fminp, Arm64, V64 | V128 | R4 | R8)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_SMINP, aarch64_neon_sminp, Arm64, V64 | V128 | I1 | I2 | I4 | I8)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_UMINP, aarch64_neon_uminp, Arm64, V64 | V128 | I1 | I2 | I4 | I8)

INTRINS_OVR_TAG(AARCH64_ADV_SIMD_FMAXNM, aarch64_neon_fmaxnm, Arm64, Scalar | V64 | V128 | R4 | R8)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_FMINNM, aarch64_neon_fminnm, Arm64, Scalar | V64 | V128 | R4 | R8)

INTRINS_OVR_TAG(AARCH64_ADV_SIMD_FMAXNMP, aarch64_neon_fmaxnmp, Arm64, V64 | V128 | R4 | R8)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_FMINNMP, aarch64_neon_fminnmp, Arm64, V64 | V128 | R4 | R8)

INTRINS_OVR_TAG(AARCH64_ADV_SIMD_SQDMULH, aarch64_neon_sqdmulh, Arm64, Scalar | V64 | V128 | I2 | I4)

INTRINS(AARCH64_ADV_SIMD_SQDMULL_SCALAR, aarch64_neon_sqdmulls_scalar, Arm64)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_SQDMULL, aarch64_neon_sqdmull, Arm64, V64 | V128 | I2 | I4 | I8)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_SQRDMULH, aarch64_neon_sqrdmulh, Arm64, Scalar | V64 | V128 | I2 | I4)

INTRINS_OVR_TAG(AARCH64_ADV_SIMD_SMULL, aarch64_neon_smull, Arm64, V128 | I2 | I4 | I8)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_UMULL, aarch64_neon_umull, Arm64, V128 | I2 | I4 | I8)

INTRINS_OVR_TAG(AARCH64_ADV_SIMD_SQNEG, aarch64_neon_sqneg, Arm64, Scalar | V64 | V128 | I1 | I2 | I4 | I8)

INTRINS_OVR_TAG(AARCH64_ADV_SIMD_PMUL, aarch64_neon_pmul, Arm64, V64 | V128 | I1)
INTRINS_OVR(AARCH64_ADV_SIMD_PMULL, aarch64_neon_pmull, Arm64, v128_i2_t)

INTRINS_OVR_TAG(AARCH64_ADV_SIMD_FMULX, aarch64_neon_fmulx, Arm64, Scalar | V64 | V128 | R4 | R8)

INTRINS_OVR_TAG(AARCH64_ADV_SIMD_URECPE, aarch64_neon_urecpe, Arm64, V64 | V128 | I4)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_FRECPE, aarch64_neon_frecpe, Arm64, Scalar | V64 | V128 | R4 | R8)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_FRECPX, aarch64_neon_frecpx, Arm64, Scalar | R4 | R8)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_URSQRTE, aarch64_neon_ursqrte, Arm64, V64 | V128 | I4)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_FRSQRTE, aarch64_neon_frsqrte, Arm64, Scalar | V64 | V128| R4 | R8)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_FRSQRTS, aarch64_neon_frsqrts, Arm64, Scalar | V64 | V128 | R4 | R8)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_FRECPS, aarch64_neon_frecps, Arm64, Scalar | V64 | V128 | R4 | R8)

INTRINS_OVR_TAG(AARCH64_ADV_SIMD_SUQADD, aarch64_neon_suqadd, Arm64, Scalar | V64 | V128 | I1 | I2 | I4 | I8)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_USQADD, aarch64_neon_usqadd, Arm64, Scalar | V64 | V128 | I1 | I2 | I4 | I8)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_UQADD, aarch64_neon_uqadd, Arm64, Scalar | V64 | V128 | I1 | I2 | I4 | I8)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_SQADD, aarch64_neon_sqadd, Arm64, Scalar | V64 | V128 | I1 | I2 | I4 | I8)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_UQSUB, aarch64_neon_uqsub, Arm64, Scalar | V64 | V128 | I1 | I2 | I4 | I8)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_SQSUB, aarch64_neon_sqsub, Arm64, Scalar | V64 | V128 | I1 | I2 | I4 | I8)

INTRINS_OVR_TAG(AARCH64_ADV_SIMD_RADDHN, aarch64_neon_raddhn, Arm64, V64 | I1 | I2 | I4)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_RSUBHN, aarch64_neon_rsubhn, Arm64, V64 | I1 | I2 | I4)

INTRINS_OVR_TAG(AARCH64_ADV_SIMD_FSQRT, sqrt, Generic, Scalar | V64 | V128 | R4 | R8)

INTRINS_OVR_TAG(AARCH64_ADV_SIMD_UQSHRN, aarch64_neon_uqshrn, Arm64, V64 | I1 | I2 | I4) // Constant shift

INTRINS_OVR_TAG(AARCH64_ADV_SIMD_RSHRN, aarch64_neon_rshrn, Arm64, V64 | I1 | I2 | I4) // Constant shift
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_SQRSHRN, aarch64_neon_sqrshrn, Arm64, V64 | I1 | I2 | I4) // Constant shift
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_SQRSHRUN, aarch64_neon_sqrshrun, Arm64, V64 | I1 | I2 | I4) // Constant shift
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_SQSHRN, aarch64_neon_sqshrn, Arm64, V64 | I1 | I2 | I4) // Constant shift
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_SQSHRUN, aarch64_neon_sqshrun, Arm64, V64 | I1 | I2 | I4) // Constant shift
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_UQRSHRN, aarch64_neon_uqrshrn, Arm64, Scalar | V64 | I1 | I2 | I4) // Constant shift

INTRINS_OVR_TAG(AARCH64_ADV_SIMD_SQRSHL, aarch64_neon_sqrshl, Arm64, Scalar | V64 | V128 | I1 | I2 | I4 | I8) // Variable shift
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_SQSHL, aarch64_neon_sqshl, Arm64, Scalar | V64 | V128 | I1 | I2 | I4 | I8) // Variable shift
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_SRSHL, aarch64_neon_srshl, Arm64, V64 | V128 | I1 | I2 | I4 | I8) // Variable shift
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_SSHL, aarch64_neon_sshl, Arm64, V64 | V128 | I1 | I2 | I4 | I8) // Variable shift
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_UQRSHL, aarch64_neon_uqrshl, Arm64, Scalar | V64 | V128 | I1 | I2 | I4 | I8) // Variable shift
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_UQSHL, aarch64_neon_uqshl, Arm64, Scalar | V64 | V128 | I1 | I2 | I4 | I8) // Variable shift
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_URSHL, aarch64_neon_urshl, Arm64, V64 | V128 | I1 | I2 | I4 | I8) // Variable shift
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_USHL, aarch64_neon_ushl, Arm64, V64 | V128 | I1 | I2 | I4 | I8) // Variable shift

INTRINS_OVR_TAG(AARCH64_ADV_SIMD_SQSHLU, aarch64_neon_sqshlu, Arm64, Scalar | V64 | V128 | I1 | I2 | I4 | I8) // Constant shift

INTRINS_OVR_TAG(AARCH64_ADV_SIMD_SLI, aarch64_neon_vsli, Arm64, V64 | V128 | I1 | I2 | I4 | I8) // Constant shift
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_SRI, aarch64_neon_vsri, Arm64, V64 | V128 | I1 | I2 | I4 | I8) // Constant shift

INTRINS_OVR_TAG(AARCH64_ADV_SIMD_TBX1, aarch64_neon_tbx1, Arm64, V64 | V128 | I1)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_TBL1, aarch64_neon_tbl1, Arm64, V64 | V128 | I1)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_TBX2, aarch64_neon_tbx2, Arm64, V64 | V128 | I1)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_TBL2, aarch64_neon_tbl2, Arm64, V64 | V128 | I1)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_TBX3, aarch64_neon_tbx3, Arm64, V64 | V128 | I1)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_TBL3, aarch64_neon_tbl3, Arm64, V64 | V128 | I1)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_TBX4, aarch64_neon_tbx4, Arm64, V64 | V128 | I1)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_TBL4, aarch64_neon_tbl4, Arm64, V64 | V128 | I1)

INTRINS_OVR_TAG_KIND(AARCH64_ADV_SIMD_SDOT, aarch64_neon_sdot, Arm64, Arm64DotProd, V64 | V128 | I4)
INTRINS_OVR_TAG_KIND(AARCH64_ADV_SIMD_UDOT, aarch64_neon_udot, Arm64, Arm64DotProd, V64 | V128 | I4)

#endif

#undef INTRINS
#undef INTRINS_OVR
#undef INTRINS_OVR_2_ARG
#undef INTRINS_OVR_3_ARG
#undef INTRINS_OVR_TAG
#undef INTRINS_OVR_TAG_KIND
#undef Scalar
#undef V64
#undef V128
#undef I1
#undef I2
#undef I4
#undef I8
#undef R4
#undef R8
#undef Ftoi
#undef WidenAcross
#undef Across
#undef Arm64DotProd
#undef Generic
#undef X86
#undef Arm64
#undef Wasm
