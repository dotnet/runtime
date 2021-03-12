
/*
 * List of LLVM intrinsics
 *
 * INTRINS(id, llvm_id, llvm_argument_type)
 *   To define a simple intrinsic
 * INTRINS_OVR(id, llvm_id, llvm_argument_type)
 *   To define an overloaded intrinsic with a single argument
 * INTRINS_OVR_2_ARG(id, llvm_id, llvm_argument_type1, llvm_argument_type2)
 *   To define an overloaded intrinsic with two arguments
 * INTRINS_OVR_3_ARG(id, llvm_id, llvm_argument_type1, llvm_argument_type2, llvm_argument_type3)
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

INTRINS_OVR_2_ARG(MEMSET, memset, LLVMPointerType (LLVMInt8Type (), 0), LLVMInt32Type ())
INTRINS_OVR_3_ARG(MEMCPY, memcpy, LLVMPointerType (LLVMInt8Type (), 0), LLVMPointerType (LLVMInt8Type (), 0), LLVMInt32Type () )
INTRINS_OVR_3_ARG(MEMMOVE, memmove, LLVMPointerType (LLVMInt8Type (), 0), LLVMPointerType (LLVMInt8Type (), 0), LLVMInt64Type ())
INTRINS_OVR(SADD_OVF_I32, sadd_with_overflow, LLVMInt32Type ())
INTRINS_OVR(UADD_OVF_I32, uadd_with_overflow, LLVMInt32Type ())
INTRINS_OVR(SSUB_OVF_I32, ssub_with_overflow, LLVMInt32Type ())
INTRINS_OVR(USUB_OVF_I32, usub_with_overflow, LLVMInt32Type ())
INTRINS_OVR(SMUL_OVF_I32, smul_with_overflow, LLVMInt32Type ())
INTRINS_OVR(UMUL_OVF_I32, umul_with_overflow, LLVMInt32Type ())
INTRINS_OVR(SADD_OVF_I64, sadd_with_overflow, LLVMInt64Type ())
INTRINS_OVR(UADD_OVF_I64, uadd_with_overflow, LLVMInt64Type ())
INTRINS_OVR(SSUB_OVF_I64, ssub_with_overflow, LLVMInt64Type ())
INTRINS_OVR(USUB_OVF_I64, usub_with_overflow, LLVMInt64Type ())
INTRINS_OVR(SMUL_OVF_I64, smul_with_overflow, LLVMInt64Type ())
INTRINS_OVR(UMUL_OVF_I64, umul_with_overflow, LLVMInt64Type ())
INTRINS_OVR(SIN, sin, LLVMDoubleType ())
INTRINS_OVR(COS, cos, LLVMDoubleType ())
INTRINS_OVR(SQRT, sqrt, LLVMDoubleType ())
INTRINS_OVR(FLOOR, floor, LLVMDoubleType ())
INTRINS_OVR(FLOORF, floor, LLVMFloatType ())
INTRINS_OVR(CEIL, ceil, LLVMDoubleType ())
INTRINS_OVR(CEILF, ceil, LLVMFloatType ())
INTRINS_OVR(FMA, fma, LLVMDoubleType ())
INTRINS_OVR(FMAF, fma, LLVMFloatType ())
	/* This isn't an intrinsic, instead llvm seems to special case it by name */
INTRINS_OVR(FABS, fabs, LLVMDoubleType ())
INTRINS_OVR(ABSF, fabs,LLVMFloatType ())
INTRINS_OVR(SINF, sin, LLVMFloatType ())
INTRINS_OVR(COSF, cos, LLVMFloatType ())
INTRINS_OVR(SQRTF, sqrt, LLVMFloatType ())
INTRINS_OVR(POWF, pow, LLVMFloatType ())
INTRINS_OVR(POW, pow, LLVMDoubleType ())
INTRINS_OVR(EXP, exp, LLVMDoubleType ())
INTRINS_OVR(EXPF, exp, LLVMFloatType ())
INTRINS_OVR(LOG, log, LLVMDoubleType ())
INTRINS_OVR(LOG2, log2, LLVMDoubleType ())
INTRINS_OVR(LOG2F, log2, LLVMFloatType ())
INTRINS_OVR(LOG10, log10, LLVMDoubleType ())
INTRINS_OVR(LOG10F, log10, LLVMFloatType ())
INTRINS_OVR(TRUNC, trunc, LLVMDoubleType ())
INTRINS_OVR(TRUNCF, trunc, LLVMFloatType ())
INTRINS_OVR(COPYSIGN, copysign, LLVMDoubleType ())
INTRINS_OVR(COPYSIGNF, copysign, LLVMFloatType ())
INTRINS_OVR(EXPECT_I8, expect, LLVMInt8Type ())
INTRINS_OVR(EXPECT_I1, expect, LLVMInt1Type ())
INTRINS_OVR(CTPOP_I32, ctpop, LLVMInt32Type ())
INTRINS_OVR(CTPOP_I64, ctpop, LLVMInt64Type ())
INTRINS_OVR(CTLZ_I32, ctlz, LLVMInt32Type ())
INTRINS_OVR(CTLZ_I64, ctlz, LLVMInt64Type ())
INTRINS_OVR(CTTZ_I32, cttz, LLVMInt32Type ())
INTRINS_OVR(CTTZ_I64, cttz, LLVMInt64Type ())
INTRINS(PREFETCH, prefetch)
INTRINS(BZHI_I32, x86_bmi_bzhi_32)
INTRINS(BZHI_I64, x86_bmi_bzhi_64)
INTRINS(BEXTR_I32, x86_bmi_bextr_32)
INTRINS(BEXTR_I64, x86_bmi_bextr_64)
INTRINS(PEXT_I32, x86_bmi_pext_32)
INTRINS(PEXT_I64, x86_bmi_pext_64)
INTRINS(PDEP_I32, x86_bmi_pdep_32)
INTRINS(PDEP_I64, x86_bmi_pdep_64)
#if defined(TARGET_AMD64) || defined(TARGET_X86)
INTRINS(SSE_PMOVMSKB, x86_sse2_pmovmskb_128)
INTRINS(SSE_MOVMSK_PS, x86_sse_movmsk_ps)
INTRINS(SSE_MOVMSK_PD, x86_sse2_movmsk_pd)
INTRINS(SSE_PSRLI_W, x86_sse2_psrli_w)
INTRINS(SSE_PSRAI_W, x86_sse2_psrai_w)
INTRINS(SSE_PSLLI_W, x86_sse2_pslli_w)
INTRINS(SSE_PSRLI_D, x86_sse2_psrli_d)
INTRINS(SSE_PSRAI_D, x86_sse2_psrai_d)
INTRINS(SSE_PSLLI_D, x86_sse2_pslli_d)
INTRINS(SSE_PSRLI_Q, x86_sse2_psrli_q)
INTRINS(SSE_PSLLI_Q, x86_sse2_pslli_q)
INTRINS(SSE_PSRL_W, x86_sse2_psrl_w)
INTRINS(SSE_PSRA_W, x86_sse2_psra_w)
INTRINS(SSE_PSRL_D, x86_sse2_psrl_d)
INTRINS(SSE_PSRA_D, x86_sse2_psra_d)
INTRINS(SSE_PSRL_Q, x86_sse2_psrl_q)
INTRINS(SSE_PSLL_W, x86_sse2_psll_w)
INTRINS(SSE_PSLL_D, x86_sse2_psll_d)
INTRINS(SSE_PSLL_Q, x86_sse2_psll_q)
#if LLVM_API_VERSION < 700
// These intrinsics were removed in LLVM 7 (bcaab53d479e7005ee69e06321bbb493f9b7f5e6).
INTRINS(SSE_SQRT_PS, x86_sse_sqrt_ps)
INTRINS(SSE_SQRT_SS, x86_sse_sqrt_ss)
INTRINS(SSE_SQRT_PD, x86_sse2_sqrt_pd)
INTRINS(SSE_SQRT_SD, x86_sse2_sqrt_sd)
INTRINS(SSE_PMULUDQ, x86_sse2_pmulu_dq)
#else
INTRINS_OVR(SSE_SQRT_PD, sqrt, sse_r8_t)
INTRINS_OVR(SSE_SQRT_PS, sqrt, sse_r4_t)
INTRINS_OVR(SSE_SQRT_SD, sqrt, LLVMDoubleType ())
INTRINS_OVR(SSE_SQRT_SS, sqrt, LLVMFloatType ())
#endif
INTRINS(SSE_RCP_PS, x86_sse_rcp_ps)
INTRINS(SSE_RSQRT_PS, x86_sse_rsqrt_ps)
INTRINS(SSE_RCP_SS, x86_sse_rcp_ss)
INTRINS(SSE_RSQRT_SS, x86_sse_rsqrt_ss)
INTRINS(SSE_CVTTPD2DQ, x86_sse2_cvttpd2dq)
INTRINS(SSE_CVTTPS2DQ, x86_sse2_cvttps2dq)
INTRINS(SSE_CVTPD2DQ, x86_sse2_cvtpd2dq)
INTRINS(SSE_CVTPS2DQ, x86_sse2_cvtps2dq)
INTRINS(SSE_CVTPD2PS, x86_sse2_cvtpd2ps)
INTRINS(SSE_CVTSS2SI, x86_sse_cvtss2si)
INTRINS(SSE_CVTSS2SI64, x86_sse_cvtss2si64)
INTRINS(SSE_CVTTSS2SI, x86_sse_cvttss2si)
INTRINS(SSE_CVTTSS2SI64, x86_sse_cvttss2si64)
INTRINS(SSE_CVTSD2SI, x86_sse2_cvtsd2si)
INTRINS(SSE_CVTTSD2SI, x86_sse2_cvttsd2si)
INTRINS(SSE_CVTSD2SI64, x86_sse2_cvtsd2si64)
INTRINS(SSE_CVTTSD2SI64, x86_sse2_cvttsd2si64)
INTRINS(SSE_CVTSD2SS, x86_sse2_cvtsd2ss)
INTRINS(SSE_CMPPD, x86_sse2_cmp_pd)
INTRINS(SSE_CMPPS, x86_sse_cmp_ps)
INTRINS(SSE_CMPSS, x86_sse_cmp_ss)
INTRINS(SSE_CMPSD, x86_sse2_cmp_sd)
INTRINS(SSE_COMIEQ_SS, x86_sse_comieq_ss)
INTRINS(SSE_COMIGT_SS, x86_sse_comigt_ss)
INTRINS(SSE_COMIGE_SS, x86_sse_comige_ss)
INTRINS(SSE_COMILT_SS, x86_sse_comilt_ss)
INTRINS(SSE_COMILE_SS, x86_sse_comile_ss)
INTRINS(SSE_COMINEQ_SS, x86_sse_comineq_ss)
INTRINS(SSE_UCOMIEQ_SS, x86_sse_ucomieq_ss)
INTRINS(SSE_UCOMIGT_SS, x86_sse_ucomigt_ss)
INTRINS(SSE_UCOMIGE_SS, x86_sse_ucomige_ss)
INTRINS(SSE_UCOMILT_SS, x86_sse_ucomilt_ss)
INTRINS(SSE_UCOMILE_SS, x86_sse_ucomile_ss)
INTRINS(SSE_UCOMINEQ_SS, x86_sse_ucomineq_ss)
INTRINS(SSE_COMIEQ_SD, x86_sse2_comieq_sd)
INTRINS(SSE_COMIGT_SD, x86_sse2_comigt_sd)
INTRINS(SSE_COMIGE_SD, x86_sse2_comige_sd)
INTRINS(SSE_COMILT_SD, x86_sse2_comilt_sd)
INTRINS(SSE_COMILE_SD, x86_sse2_comile_sd)
INTRINS(SSE_COMINEQ_SD, x86_sse2_comineq_sd)
INTRINS(SSE_UCOMIEQ_SD, x86_sse2_ucomieq_sd)
INTRINS(SSE_UCOMIGT_SD, x86_sse2_ucomigt_sd)
INTRINS(SSE_UCOMIGE_SD, x86_sse2_ucomige_sd)
INTRINS(SSE_UCOMILT_SD, x86_sse2_ucomilt_sd)
INTRINS(SSE_UCOMILE_SD, x86_sse2_ucomile_sd)
INTRINS(SSE_UCOMINEQ_SD, x86_sse2_ucomineq_sd)
INTRINS(SSE_PACKSSWB, x86_sse2_packsswb_128)
INTRINS(SSE_PACKUSWB, x86_sse2_packuswb_128)
INTRINS(SSE_PACKSSDW, x86_sse2_packssdw_128)
INTRINS(SSE_PACKUSDW, x86_sse41_packusdw)
INTRINS(SSE_MINPS, x86_sse_min_ps)
INTRINS(SSE_MAXPS, x86_sse_max_ps)
INTRINS(SSE_MINSS, x86_sse_min_ss)
INTRINS(SSE_MAXSS, x86_sse_max_ss)
INTRINS(SSE_HADDPS, x86_sse3_hadd_ps)
INTRINS(SSE_HSUBPS, x86_sse3_hsub_ps)
INTRINS(SSE_ADDSUBPS, x86_sse3_addsub_ps)
INTRINS(SSE_MINPD, x86_sse2_min_pd)
INTRINS(SSE_MAXPD, x86_sse2_max_pd)
INTRINS(SSE_MAXSD, x86_sse2_max_sd)
INTRINS(SSE_MINSD, x86_sse2_min_sd)
INTRINS(SSE_HADDPD, x86_sse3_hadd_pd)
INTRINS(SSE_HSUBPD, x86_sse3_hsub_pd)
INTRINS(SSE_ADDSUBPD, x86_sse3_addsub_pd)
INTRINS(SSE_PMULHW, x86_sse2_pmulh_w)
INTRINS(SSE_PMULHU, x86_sse2_pmulhu_w)
INTRINS(SSE_PMULHUW, x86_sse2_pmulhu_w)
INTRINS(SSE_PMADDWD, x86_sse2_pmadd_wd)
INTRINS(SSE_PSADBW, x86_sse2_psad_bw)
INTRINS(SSE_PAUSE, x86_sse2_pause)
INTRINS(SSE_MASKMOVDQU, x86_sse2_maskmov_dqu)
INTRINS(SSE_PSHUFB, x86_ssse3_pshuf_b_128)
INTRINS(SSE_DPPS, x86_sse41_dpps)
INTRINS(SSE_DPPD, x86_sse41_dppd)
INTRINS(SSE_ROUNDSS, x86_sse41_round_ss)
INTRINS(SSE_ROUNDSD, x86_sse41_round_sd)
INTRINS(SSE_ROUNDPS, x86_sse41_round_ps)
INTRINS(SSE_ROUNDPD, x86_sse41_round_pd)
INTRINS(SSE_PTESTZ, x86_sse41_ptestz)
INTRINS(SSE_INSERTPS, x86_sse41_insertps)
INTRINS(SSE_SFENCE, x86_sse_sfence)
INTRINS(SSE_MFENCE, x86_sse2_mfence)
INTRINS(SSE_LFENCE, x86_sse2_lfence)
INTRINS(SSE_LDU_DQ, x86_sse3_ldu_dq)
INTRINS(SSE_PHADDW, x86_ssse3_phadd_w_128)
INTRINS(SSE_PHADDD, x86_ssse3_phadd_d_128)
INTRINS(SSE_PHADDSW, x86_ssse3_phadd_sw_128)
INTRINS(SSE_PHSUBW, x86_ssse3_phsub_w_128)
INTRINS(SSE_PHSUBD, x86_ssse3_phsub_d_128)
INTRINS(SSE_PHSUBSW, x86_ssse3_phsub_sw_128)
INTRINS(SSE_PMADDUBSW, x86_ssse3_pmadd_ub_sw_128)
INTRINS(SSE_PMULHRSW, x86_ssse3_pmul_hr_sw_128)
INTRINS(SSE_PSIGNB, x86_ssse3_psign_b_128)
INTRINS(SSE_PSIGNW, x86_ssse3_psign_w_128)
INTRINS(SSE_PSIGND, x86_ssse3_psign_d_128)
INTRINS(SSE_CRC32_32_8, x86_sse42_crc32_32_8)
INTRINS(SSE_CRC32_32_16, x86_sse42_crc32_32_16)
INTRINS(SSE_CRC32_32_32, x86_sse42_crc32_32_32)
INTRINS(SSE_CRC32_64_64, x86_sse42_crc32_64_64)
INTRINS(SSE_TESTC, x86_sse41_ptestc)
INTRINS(SSE_TESTNZ, x86_sse41_ptestnzc)
INTRINS(SSE_TESTZ, x86_sse41_ptestz)
INTRINS(SSE_PBLENDVB, x86_sse41_pblendvb)
INTRINS(SSE_BLENDVPS, x86_sse41_blendvps)
INTRINS(SSE_BLENDVPD, x86_sse41_blendvpd)
#if LLVM_API_VERSION < 700
// Clang 7 and above use a sequence of IR operations to represent pmuldq.
INTRINS(SSE_PMULDQ, x86_sse41_pmuldq)
#endif
INTRINS(SSE_PHMINPOSUW, x86_sse41_phminposuw)
INTRINS(SSE_MPSADBW, x86_sse41_mpsadbw)
INTRINS(PCLMULQDQ, x86_pclmulqdq)
INTRINS(AESNI_AESKEYGENASSIST, x86_aesni_aeskeygenassist)
INTRINS(AESNI_AESDEC, x86_aesni_aesdec)
INTRINS(AESNI_AESDECLAST, x86_aesni_aesdeclast)
INTRINS(AESNI_AESENC, x86_aesni_aesenc)
INTRINS(AESNI_AESENCLAST, x86_aesni_aesenclast)
INTRINS(AESNI_AESIMC, x86_aesni_aesimc)
#if LLVM_API_VERSION >= 800
	// these intrinsics were renamed in LLVM 8
INTRINS_OVR(SSE_SADD_SATI8, sadd_sat, sse_i1_t)
INTRINS_OVR(SSE_UADD_SATI8, uadd_sat, sse_i1_t)
INTRINS_OVR(SSE_SADD_SATI16, sadd_sat, sse_i1_t)
INTRINS_OVR(SSE_UADD_SATI16, uadd_sat, sse_i1_t)

INTRINS_OVR(SSE_SSUB_SATI8, ssub_sat, sse_i2_t)
INTRINS_OVR(SSE_USUB_SATI8, usub_sat, sse_i2_t)
INTRINS_OVR(SSE_SSUB_SATI16, ssub_sat, sse_i2_t)
INTRINS_OVR(SSE_USUB_SATI16, usub_sat, sse_i2_t)
#else
INTRINS(SSE_SADD_SATI8, x86_sse2_padds_b)
INTRINS(SSE_UADD_SATI8, x86_sse2_paddus_b)
INTRINS(SSE_SADD_SATI16, x86_sse2_padds_w)
INTRINS(SSE_UADD_SATI16, x86_sse2_paddus_w)

INTRINS(SSE_SSUB_SATI8, x86_sse2_psubs_b)
INTRINS(SSE_USUB_SATI8, x86_sse2_psubus_b)
INTRINS(SSE_SSUB_SATI16, x86_sse2_psubs_w)
INTRINS(SSE_USUB_SATI16, x86_sse2_psubus_w)
#endif
#endif
#if defined(TARGET_WASM) && LLVM_API_VERSION >= 800
INTRINS_OVR(WASM_ANYTRUE_V16, wasm_anytrue,  sse_i1_t)
INTRINS_OVR(WASM_ANYTRUE_V8, wasm_anytrue, sse_i2_t)
INTRINS_OVR(WASM_ANYTRUE_V4, wasm_anytrue, sse_i4_t)
INTRINS_OVR(WASM_ANYTRUE_V2, wasm_anytrue, sse_i8_t)
#endif
#if defined(TARGET_ARM64)
INTRINS_OVR(BITREVERSE_I32, bitreverse, LLVMInt32Type ())
INTRINS_OVR(BITREVERSE_I64, bitreverse, LLVMInt64Type ())
INTRINS(AARCH64_CRC32B, aarch64_crc32b)
INTRINS(AARCH64_CRC32H, aarch64_crc32h)
INTRINS(AARCH64_CRC32W, aarch64_crc32w)
INTRINS(AARCH64_CRC32X, aarch64_crc32x)
INTRINS(AARCH64_CRC32CB, aarch64_crc32cb)
INTRINS(AARCH64_CRC32CH, aarch64_crc32ch)
INTRINS(AARCH64_CRC32CW, aarch64_crc32cw)
INTRINS(AARCH64_CRC32CX, aarch64_crc32cx)
INTRINS(AARCH64_AESD, aarch64_crypto_aesd)
INTRINS(AARCH64_AESE, aarch64_crypto_aese)
INTRINS(AARCH64_AESIMC, aarch64_crypto_aesimc)
INTRINS(AARCH64_AESMC, aarch64_crypto_aesmc)
INTRINS(AARCH64_SHA1C, aarch64_crypto_sha1c)
INTRINS(AARCH64_SHA1H, aarch64_crypto_sha1h)
INTRINS(AARCH64_SHA1M, aarch64_crypto_sha1m)
INTRINS(AARCH64_SHA1P, aarch64_crypto_sha1p)
INTRINS(AARCH64_SHA1SU0, aarch64_crypto_sha1su0)
INTRINS(AARCH64_SHA1SU1, aarch64_crypto_sha1su1)
INTRINS(AARCH64_SHA256SU0, aarch64_crypto_sha256su0)
INTRINS(AARCH64_SHA256SU1, aarch64_crypto_sha256su1)
INTRINS(AARCH64_SHA256H, aarch64_crypto_sha256h)
INTRINS(AARCH64_SHA256H2, aarch64_crypto_sha256h2)
INTRINS(AARCH64_PMULL64, aarch64_neon_pmull64)

INTRINS_OVR_TAG_KIND(AARCH64_ADV_SIMD_FACGE, aarch64_neon_facge, Ftoi, Scalar | V64 | V128 | I4 | I8)
INTRINS_OVR_TAG_KIND(AARCH64_ADV_SIMD_FACGT, aarch64_neon_facgt, Ftoi, Scalar | V64 | V128 | I4 | I8)

INTRINS_OVR_TAG(AARCH64_ADV_SIMD_FABD_SCALAR, aarch64_sisd_fabd, Scalar | R4 | R8)

INTRINS_OVR_TAG(AARCH64_ADV_SIMD_FABD, aarch64_neon_fabd, V64 | V128 | R4 | R8)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_UABD, aarch64_neon_uabd, V64 | V128 | I1 | I2 | I4)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_SABD, aarch64_neon_sabd, V64 | V128 | I1 | I2 | I4)

INTRINS_OVR_TAG(AARCH64_ADV_SIMD_SQABS, aarch64_neon_sqabs, Scalar | V64 | V128 | I1 | I2 | I4 | I8)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_FABS, fabs, Scalar | V64 | V128 | R4 | R8)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_ABS, aarch64_neon_abs, Scalar | V64 | V128 | I1 | I2 | I4 | I8)

INTRINS_OVR_TAG_KIND(AARCH64_ADV_SIMD_UADDLV, aarch64_neon_uaddlv, WidenAcross, V64 | V128 | I1 | I2 | I4)
INTRINS_OVR_TAG_KIND(AARCH64_ADV_SIMD_SADDLV, aarch64_neon_saddlv, WidenAcross, V64 | V128 | I1 | I2 | I4)

INTRINS_OVR_TAG(AARCH64_ADV_SIMD_ADDP, aarch64_neon_addp, V64 | V128 | I1 | I2 | I4 | I8)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_FADDP, aarch64_neon_faddp, V64 | V128 | R4 | R8)

INTRINS_OVR_TAG_KIND(AARCH64_ADV_SIMD_FMAXNMV, aarch64_neon_fmaxnmv, Across, V64 | V128 | R4 | R8)
INTRINS_OVR_TAG_KIND(AARCH64_ADV_SIMD_FMINNMV, aarch64_neon_fminnmv, Across, V64 | V128 | R4 | R8)

INTRINS_OVR_TAG_KIND(AARCH64_ADV_SIMD_SADDV, aarch64_neon_saddv, Across, V64 | V128 | I1 | I2 | I4)
INTRINS_OVR_TAG_KIND(AARCH64_ADV_SIMD_UADDV, aarch64_neon_uaddv, Across, V64 | V128 | I1 | I2 | I4 | I8)
INTRINS_OVR_TAG_KIND(AARCH64_ADV_SIMD_SMAXV, aarch64_neon_smaxv, Across, V64 | V128 | I1 | I2 | I4)
INTRINS_OVR_TAG_KIND(AARCH64_ADV_SIMD_UMAXV, aarch64_neon_umaxv, Across, V64 | V128 | I1 | I2 | I4)
INTRINS_OVR_TAG_KIND(AARCH64_ADV_SIMD_SMINV, aarch64_neon_sminv, Across, V64 | V128 | I1 | I2 | I4)
INTRINS_OVR_TAG_KIND(AARCH64_ADV_SIMD_UMINV, aarch64_neon_uminv, Across, V64 | V128 | I1 | I2 | I4)
INTRINS_OVR_TAG_KIND(AARCH64_ADV_SIMD_FMAXV, aarch64_neon_fmaxv, Across, V64 | V128 | R4 | R8)
INTRINS_OVR_TAG_KIND(AARCH64_ADV_SIMD_FMINV, aarch64_neon_fminv, Across, V64 | V128 | R4 | R8)

INTRINS_OVR_TAG_KIND(AARCH64_ADV_SIMD_SADDLP, aarch64_neon_saddlp, Widen, V64 | V128 | I1 | I2 | I4 | I8)
INTRINS_OVR_TAG_KIND(AARCH64_ADV_SIMD_UADDLP, aarch64_neon_uaddlp, Widen, V64 | V128 | I1 | I2 | I4 | I8)

INTRINS_OVR_2_ARG(AARCH64_ADV_SIMD_FCVTXN, aarch64_neon_fcvtxn, v64_r4_t, v128_r8_t)

INTRINS_OVR_TAG_KIND(AARCH64_ADV_SIMD_FCVTAS, aarch64_neon_fcvtas, Ftoi, Scalar | V64 | V128 | I4 | I8)
INTRINS_OVR_TAG_KIND(AARCH64_ADV_SIMD_FCVTNS, aarch64_neon_fcvtns, Ftoi, Scalar | V64 | V128 | I4 | I8)
INTRINS_OVR_TAG_KIND(AARCH64_ADV_SIMD_FCVTMS, aarch64_neon_fcvtms, Ftoi, Scalar | V64 | V128 | I4 | I8)
INTRINS_OVR_TAG_KIND(AARCH64_ADV_SIMD_FCVTPS, aarch64_neon_fcvtps, Ftoi, Scalar | V64 | V128 | I4 | I8)

INTRINS_OVR_TAG_KIND(AARCH64_ADV_SIMD_FCVTAU, aarch64_neon_fcvtau, Ftoi, Scalar | V64 | V128 | I4 | I8)
INTRINS_OVR_TAG_KIND(AARCH64_ADV_SIMD_FCVTNU, aarch64_neon_fcvtnu, Ftoi, Scalar | V64 | V128 | I4 | I8)
INTRINS_OVR_TAG_KIND(AARCH64_ADV_SIMD_FCVTMU, aarch64_neon_fcvtmu, Ftoi, Scalar | V64 | V128 | I4 | I8)
INTRINS_OVR_TAG_KIND(AARCH64_ADV_SIMD_FCVTPU, aarch64_neon_fcvtpu, Ftoi, Scalar | V64 | V128 | I4 | I8)

INTRINS_OVR_2_ARG(AARCH64_ADV_SIMD_SCALAR_SQXTUN, aarch64_neon_scalar_sqxtun, i4_t, i8_t)
INTRINS_OVR_2_ARG(AARCH64_ADV_SIMD_SCALAR_SQXTN, aarch64_neon_scalar_sqxtn, i4_t, i8_t)
INTRINS_OVR_2_ARG(AARCH64_ADV_SIMD_SCALAR_UQXTN, aarch64_neon_scalar_uqxtn, i4_t, i8_t)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_SQXTUN, aarch64_neon_sqxtun, V64 | I1 | I2 | I4)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_SQXTN, aarch64_neon_sqxtn, V64 | I1 | I2 | I4)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_UQXTN, aarch64_neon_uqxtn, V64 | I1 | I2 | I4)

INTRINS_OVR_TAG(AARCH64_ADV_SIMD_SRHADD, aarch64_neon_srhadd, V64 | V128 | I1 | I2 | I4)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_URHADD, aarch64_neon_urhadd, V64 | V128 | I1 | I2 | I4)

INTRINS_OVR_TAG(AARCH64_ADV_SIMD_FMA, fma, Scalar | V64 | V128 | R4 | R8)

INTRINS_OVR_TAG(AARCH64_ADV_SIMD_SHADD, aarch64_neon_shadd, V64 | V128 | I1 | I2 | I4)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_UHADD, aarch64_neon_uhadd, V64 | V128 | I1 | I2 | I4)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_SHSUB, aarch64_neon_shsub, V64 | V128 | I1 | I2 | I4)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_UHSUB, aarch64_neon_uhsub, V64 | V128 | I1 | I2 | I4)

INTRINS_OVR_TAG(AARCH64_ADV_SIMD_CLS, aarch64_neon_cls, V64 | V128 | I1 | I2 | I4 | I8)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_CLZ, ctlz, V64 | V128 | I1 | I2 | I4 | I8)

INTRINS_OVR_TAG(AARCH64_ADV_SIMD_SMAX, aarch64_neon_smax, V64 | V128 | I1 | I2 | I4 | I8)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_UMAX, aarch64_neon_umax, V64 | V128 | I1 | I2 | I4 | I8)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_FMAX, aarch64_neon_fmax, Scalar | V64 | V128 | R4 | R8)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_SMIN, aarch64_neon_smin, V64 | V128 | I1 | I2 | I4 | I8)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_UMIN, aarch64_neon_umin, V64 | V128 | I1 | I2 | I4 | I8)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_FMIN, aarch64_neon_fmin, Scalar | V64 | V128 | R4 | R8)

INTRINS_OVR_TAG(AARCH64_ADV_SIMD_FMAXP, aarch64_neon_fmaxp, V64 | V128 | R4 | R8)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_SMAXP, aarch64_neon_smaxp, V64 | V128 | I1 | I2 | I4 | I8)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_UMAXP, aarch64_neon_umaxp, V64 | V128 | I1 | I2 | I4 | I8)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_FMINP, aarch64_neon_fminp, V64 | V128 | R4 | R8)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_SMINP, aarch64_neon_sminp, V64 | V128 | I1 | I2 | I4 | I8)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_UMINP, aarch64_neon_uminp, V64 | V128 | I1 | I2 | I4 | I8)

INTRINS_OVR_TAG(AARCH64_ADV_SIMD_FMAXNM, aarch64_neon_fmaxnm, Scalar | V64 | V128 | R4 | R8)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_FMINNM, aarch64_neon_fminnm, Scalar | V64 | V128 | R4 | R8)

INTRINS_OVR_TAG(AARCH64_ADV_SIMD_FMAXNMP, aarch64_neon_fmaxnmp, V64 | V128 | R4 | R8)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_FMINNMP, aarch64_neon_fminnmp, V64 | V128 | R4 | R8)

INTRINS_OVR_TAG(AARCH64_ADV_SIMD_SQDMULH, aarch64_neon_sqdmulh, Scalar | V64 | V128 | I2 | I4)

INTRINS(AARCH64_ADV_SIMD_SQDMULL_SCALAR, aarch64_neon_sqdmulls_scalar)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_SQDMULL, aarch64_neon_sqdmull, V64 | V128 | I2 | I4 | I8)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_SQRDMULH, aarch64_neon_sqrdmulh, Scalar | V64 | V128 | I2 | I4)

INTRINS_OVR_TAG(AARCH64_ADV_SIMD_SMULL, aarch64_neon_smull, V128 | I2 | I4 | I8)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_UMULL, aarch64_neon_umull, V128 | I2 | I4 | I8)

INTRINS_OVR_TAG(AARCH64_ADV_SIMD_SQNEG, aarch64_neon_sqneg, Scalar | V64 | V128 | I1 | I2 | I4 | I8)

INTRINS_OVR_TAG(AARCH64_ADV_SIMD_PMUL, aarch64_neon_pmul, V64 | V128 | I1)
INTRINS_OVR(AARCH64_ADV_SIMD_PMULL, aarch64_neon_pmull, v128_i2_t)

INTRINS_OVR_TAG(AARCH64_ADV_SIMD_FMULX, aarch64_neon_fmulx, Scalar | V64 | V128 | R4 | R8)

INTRINS_OVR_TAG(AARCH64_ADV_SIMD_CNT, ctpop, V64 | V128 | I1)

INTRINS_OVR_TAG(AARCH64_ADV_SIMD_URECPE, aarch64_neon_urecpe, V64 | V128 | I4)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_FRECPE, aarch64_neon_frecpe, Scalar | V64 | V128 | R4 | R8)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_FRECPX, aarch64_neon_frecpx, Scalar | R4 | R8)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_URSQRTE, aarch64_neon_ursqrte, V64 | V128 | I4)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_FRSQRTE, aarch64_neon_frsqrte, Scalar | V64 | V128| R4 | R8)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_FRSQRTS, aarch64_neon_frsqrts, Scalar | V64 | V128 | R4 | R8)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_FRECPS, aarch64_neon_frecps, Scalar | V64 | V128 | R4 | R8)

INTRINS_OVR_TAG(AARCH64_ADV_SIMD_RBIT, aarch64_neon_rbit, V64 | V128 | I1)

INTRINS_OVR_TAG(AARCH64_ADV_SIMD_FRINTA, round, Scalar | V64 | V128 | R4 | R8)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_FRINTN, aarch64_neon_frintn, Scalar | V64 | V128 | R4 | R8)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_FRINTM, floor, Scalar | V64 | V128 | R4 | R8)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_FRINTP, ceil, Scalar | V64 | V128 | R4 | R8)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_FRINTZ, trunc, Scalar | V64 | V128 | R4 | R8)

INTRINS_OVR_TAG(AARCH64_ADV_SIMD_SUQADD, aarch64_neon_suqadd, Scalar | V64 | V128 | I1 | I2 | I4 | I8)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_USQADD, aarch64_neon_usqadd, Scalar | V64 | V128 | I1 | I2 | I4 | I8)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_UQADD, aarch64_neon_uqadd, Scalar | V64 | V128 | I1 | I2 | I4 | I8)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_SQADD, aarch64_neon_sqadd, Scalar | V64 | V128 | I1 | I2 | I4 | I8)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_UQSUB, aarch64_neon_uqsub, Scalar | V64 | V128 | I1 | I2 | I4 | I8)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_SQSUB, aarch64_neon_sqsub, Scalar | V64 | V128 | I1 | I2 | I4 | I8)

INTRINS_OVR_TAG(AARCH64_ADV_SIMD_RADDHN, aarch64_neon_raddhn, V64 | I1 | I2 | I4)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_RSUBHN, aarch64_neon_rsubhn, V64 | I1 | I2 | I4)

INTRINS_OVR_TAG(AARCH64_ADV_SIMD_FSQRT, sqrt, Scalar | V64 | V128 | R4 | R8)

INTRINS_OVR_TAG(AARCH64_ADV_SIMD_UQSHRN, aarch64_neon_uqshrn, V64 | I1 | I2 | I4) // Constant shift

INTRINS_OVR_TAG(AARCH64_ADV_SIMD_RSHRN, aarch64_neon_rshrn, V64 | I1 | I2 | I4) // Constant shift
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_SQRSHRN, aarch64_neon_sqrshrn, V64 | I1 | I2 | I4) // Constant shift
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_SQRSHRUN, aarch64_neon_sqrshrun, V64 | I1 | I2 | I4) // Constant shift
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_SQSHRN, aarch64_neon_sqshrn, V64 | I1 | I2 | I4) // Constant shift
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_SQSHRUN, aarch64_neon_sqshrun, V64 | I1 | I2 | I4) // Constant shift
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_UQRSHRN, aarch64_neon_uqrshrn, Scalar | V64 | I1 | I2 | I4) // Constant shift

INTRINS_OVR_TAG(AARCH64_ADV_SIMD_SQRSHL, aarch64_neon_sqrshl, Scalar | V64 | V128 | I1 | I2 | I4 | I8) // Variable shift
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_SQSHL, aarch64_neon_sqshl, Scalar | V64 | V128 | I1 | I2 | I4 | I8) // Variable shift
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_SRSHL, aarch64_neon_srshl, V64 | V128 | I1 | I2 | I4 | I8) // Variable shift
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_SSHL, aarch64_neon_sshl, V64 | V128 | I1 | I2 | I4 | I8) // Variable shift
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_UQRSHL, aarch64_neon_uqrshl, Scalar | V64 | V128 | I1 | I2 | I4 | I8) // Variable shift
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_UQSHL, aarch64_neon_uqshl, Scalar | V64 | V128 | I1 | I2 | I4 | I8) // Variable shift
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_URSHL, aarch64_neon_urshl, V64 | V128 | I1 | I2 | I4 | I8) // Variable shift
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_USHL, aarch64_neon_ushl, V64 | V128 | I1 | I2 | I4 | I8) // Variable shift

INTRINS_OVR_TAG(AARCH64_ADV_SIMD_SQSHLU, aarch64_neon_sqshlu, Scalar | V64 | V128 | I1 | I2 | I4 | I8) // Constant shift

INTRINS_OVR_TAG(AARCH64_ADV_SIMD_SLI, aarch64_neon_vsli, V64 | V128 | I1 | I2 | I4 | I8) // Constant shift
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_SRI, aarch64_neon_vsri, V64 | V128 | I1 | I2 | I4 | I8) // Constant shift

INTRINS_OVR_TAG(AARCH64_ADV_SIMD_TBX1, aarch64_neon_tbx1, V64 | V128 | I1)
INTRINS_OVR_TAG(AARCH64_ADV_SIMD_TBL1, aarch64_neon_tbl1, V64 | V128 | I1)
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
