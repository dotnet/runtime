// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*****************************************************************************/
#if !defined(HARDWARE_INTRINSIC) && !defined(HARDWARE_INTRINSIC_CLASS)
#error Define HARDWARE_INTRINSIC and/or HARDWARE_INTRINSIC_CLASS before including this file
#endif
/*****************************************************************************/

// clang-format off

#if defined(HARDWARE_INTRINSIC_CLASS)
HARDWARE_INTRINSIC_CLASS(JIT_FLAG_HAS_ARM64_AES       , EnableArm64Aes      , Aes      )
HARDWARE_INTRINSIC_CLASS(JIT_FLAG_HAS_ARM64_ATOMICS   , EnableArm64Atomics  , Atomics  )
HARDWARE_INTRINSIC_CLASS(JIT_FLAG_HAS_ARM64_CRC32     , EnableArm64Crc32    , Crc32    )
HARDWARE_INTRINSIC_CLASS(JIT_FLAG_HAS_ARM64_DCPOP     , EnableArm64Dcpop    , Dcpop    )
HARDWARE_INTRINSIC_CLASS(JIT_FLAG_HAS_ARM64_DP        , EnableArm64Dp       , Dp       )
HARDWARE_INTRINSIC_CLASS(JIT_FLAG_HAS_ARM64_FCMA      , EnableArm64Fcma     , Fcma     )
HARDWARE_INTRINSIC_CLASS(JIT_FLAG_HAS_ARM64_FP        , EnableArm64Fp       , Fp       )
HARDWARE_INTRINSIC_CLASS(JIT_FLAG_HAS_ARM64_FP16      , EnableArm64Fp16     , Fp16     )
HARDWARE_INTRINSIC_CLASS(JIT_FLAG_HAS_ARM64_JSCVT     , EnableArm64Jscvt    , Jscvt    )
HARDWARE_INTRINSIC_CLASS(JIT_FLAG_HAS_ARM64_LRCPC     , EnableArm64Lrcpc    , Lrcpc    )
HARDWARE_INTRINSIC_CLASS(JIT_FLAG_HAS_ARM64_PMULL     , EnableArm64Pmull    , Pmull    )
HARDWARE_INTRINSIC_CLASS(JIT_FLAG_HAS_ARM64_SHA1      , EnableArm64Sha1     , Sha1     )
HARDWARE_INTRINSIC_CLASS(JIT_FLAG_HAS_ARM64_SHA256    , EnableArm64Sha256   , Sha256   )
HARDWARE_INTRINSIC_CLASS(JIT_FLAG_HAS_ARM64_SHA512    , EnableArm64Sha512   , Sha512   )
HARDWARE_INTRINSIC_CLASS(JIT_FLAG_HAS_ARM64_SHA3      , EnableArm64Sha3     , Sha3     )
HARDWARE_INTRINSIC_CLASS(JIT_FLAG_HAS_ARM64_SIMD      , EnableArm64Simd     , Simd     )
HARDWARE_INTRINSIC_CLASS(JIT_FLAG_HAS_ARM64_SIMD_V81  , EnableArm64Simd_v81 , Simd_v81 )
HARDWARE_INTRINSIC_CLASS(JIT_FLAG_HAS_ARM64_SIMD_FP16 , EnableArm64Simd_fp16, Simd_fp16)
HARDWARE_INTRINSIC_CLASS(JIT_FLAG_HAS_ARM64_SM3       , EnableArm64Sm3      , Sm3      )
HARDWARE_INTRINSIC_CLASS(JIT_FLAG_HAS_ARM64_SM4       , EnableArm64Sm4      , Sm4      )
HARDWARE_INTRINSIC_CLASS(JIT_FLAG_HAS_ARM64_SVE       , EnableArm64Sve      , Sve      )
#endif // defined(HARDWARE_INTRINSIC_CLASS)

#if defined(HARDWARE_INTRINSIC)
//                (ID                             Class       Function name                   Form           Floating,    Signed,      Unsigned,     Flags)
//  None (For internal use only)
HARDWARE_INTRINSIC(NI_ARM64_NONE_MOV,             None,          None,                           UnaryOp,       INS_mov,     INS_mov,     INS_mov,      None )

//  Base
HARDWARE_INTRINSIC(NI_ARM64_BASE_CLS,             Base,          LeadingSignCount,               UnaryOp,       INS_invalid, INS_cls,     INS_cls,      None )
HARDWARE_INTRINSIC(NI_ARM64_BASE_CLZ,             Base,          LeadingZeroCount,               UnaryOp,       INS_invalid, INS_clz,     INS_clz,      None )

// Vector64
HARDWARE_INTRINSIC(NI_Vector64_AsByte,            Vector64,      AsByte,                         UnaryOp,       INS_invalid, INS_invalid, INS_invalid,  None )
HARDWARE_INTRINSIC(NI_Vector64_AsInt16,           Vector64,      AsInt16,                        UnaryOp,       INS_invalid, INS_invalid, INS_invalid,  None )
HARDWARE_INTRINSIC(NI_Vector64_AsInt32,           Vector64,      AsInt32,                        UnaryOp,       INS_invalid, INS_invalid, INS_invalid,  None )
HARDWARE_INTRINSIC(NI_Vector64_AsSByte,           Vector64,      AsSByte,                        UnaryOp,       INS_invalid, INS_invalid, INS_invalid,  None )
HARDWARE_INTRINSIC(NI_Vector64_AsSingle,          Vector64,      AsSingle,                       UnaryOp,       INS_invalid, INS_invalid, INS_invalid,  None )
HARDWARE_INTRINSIC(NI_Vector64_AsUInt16,          Vector64,      AsUInt16,                       UnaryOp,       INS_invalid, INS_invalid, INS_invalid,  None )
HARDWARE_INTRINSIC(NI_Vector64_AsUInt32,          Vector64,      AsUInt32,                       UnaryOp,       INS_invalid, INS_invalid, INS_invalid,  None )

// Vector128
HARDWARE_INTRINSIC(NI_Vector128_As,               Vector128,     As,                             UnaryOp,       INS_invalid, INS_invalid, INS_invalid,  None )
HARDWARE_INTRINSIC(NI_Vector128_AsByte,           Vector128,     AsByte,                         UnaryOp,       INS_invalid, INS_invalid, INS_invalid,  None )
HARDWARE_INTRINSIC(NI_Vector128_AsDouble,         Vector128,     AsDouble,                       UnaryOp,       INS_invalid, INS_invalid, INS_invalid,  None )
HARDWARE_INTRINSIC(NI_Vector128_AsInt16,          Vector128,     AsInt16,                        UnaryOp,       INS_invalid, INS_invalid, INS_invalid,  None )
HARDWARE_INTRINSIC(NI_Vector128_AsInt32,          Vector128,     AsInt32,                        UnaryOp,       INS_invalid, INS_invalid, INS_invalid,  None )
HARDWARE_INTRINSIC(NI_Vector128_AsInt64,          Vector128,     AsInt64,                        UnaryOp,       INS_invalid, INS_invalid, INS_invalid,  None )
HARDWARE_INTRINSIC(NI_Vector128_AsSByte,          Vector128,     AsSByte,                        UnaryOp,       INS_invalid, INS_invalid, INS_invalid,  None )
HARDWARE_INTRINSIC(NI_Vector128_AsSingle,         Vector128,     AsSingle,                       UnaryOp,       INS_invalid, INS_invalid, INS_invalid,  None )
HARDWARE_INTRINSIC(NI_Vector128_AsUInt16,         Vector128,     AsUInt16,                       UnaryOp,       INS_invalid, INS_invalid, INS_invalid,  None )
HARDWARE_INTRINSIC(NI_Vector128_AsUInt32,         Vector128,     AsUInt32,                       UnaryOp,       INS_invalid, INS_invalid, INS_invalid,  None )
HARDWARE_INTRINSIC(NI_Vector128_AsUInt64,         Vector128,     AsUInt64,                       UnaryOp,       INS_invalid, INS_invalid, INS_invalid,  None )
#if NYI
// Crc32
HARDWARE_INTRINSIC(NI_ARM64_CRC32_CRC32,          Crc32,    Crc32,                          CrcOp,         INS_invalid, INS_invalid, INS_crc32,    None )
HARDWARE_INTRINSIC(NI_ARM64_CRC32_CRC32C,         Crc32,    Crc32C,                         CrcOp,         INS_invalid, INS_invalid, INS_crc32c,   None )
#endif
//  Simd
HARDWARE_INTRINSIC(NI_ARM64_SIMD_Abs,             Simd,     Abs,                            SimdUnaryOp,   INS_fabs,    INS_invalid, INS_abs,      None )
HARDWARE_INTRINSIC(NI_ARM64_SIMD_Add,             Simd,     Add,                            SimdBinaryOp,  INS_fadd,    INS_add,     INS_add,      None )
HARDWARE_INTRINSIC(NI_ARM64_SIMD_BitwiseAnd,      Simd,     And,                            SimdBinaryOp,  INS_and,     INS_and,     INS_and,      None )
HARDWARE_INTRINSIC(NI_ARM64_SIMD_BitwiseAndNot,   Simd,     AndNot,                         SimdBinaryOp,  INS_bic,     INS_bic,     INS_bic,      None )
HARDWARE_INTRINSIC(NI_ARM64_SIMD_BitwiseOr,       Simd,     Or,                             SimdBinaryOp,  INS_orr,     INS_orr,     INS_orr,      None )
HARDWARE_INTRINSIC(NI_ARM64_SIMD_BitwiseOrNot,    Simd,     OrNot,                          SimdBinaryOp,  INS_orn,     INS_orn,     INS_orn,      None )
HARDWARE_INTRINSIC(NI_ARM64_SIMD_BitwiseNot,      Simd,     Not,                            SimdUnaryOp,   INS_not,     INS_not,     INS_not,      None )
HARDWARE_INTRINSIC(NI_ARM64_SIMD_BitwiseSelect,   Simd,     BitwiseSelect,                  SimdSelectOp,  INS_bsl,     INS_bsl,     INS_bsl,      None )
HARDWARE_INTRINSIC(NI_ARM64_SIMD_BitwiseXor,      Simd,     Xor,                            SimdBinaryOp,  INS_eor,     INS_eor,     INS_eor,      None )
HARDWARE_INTRINSIC(NI_ARM64_SIMD_CLS,             Simd,     LeadingSignCount,               SimdUnaryOp,   INS_invalid, INS_cls,     INS_cls,      None )
HARDWARE_INTRINSIC(NI_ARM64_SIMD_CLZ,             Simd,     LeadingZeroCount,               SimdUnaryOp,   INS_invalid, INS_clz,     INS_clz,      None )
HARDWARE_INTRINSIC(NI_ARM64_SIMD_CNT,             Simd,     PopCount,                       SimdUnaryOp,   INS_invalid, INS_cnt,     INS_cnt,      None )
HARDWARE_INTRINSIC(NI_ARM64_SIMD_EQ,              Simd,     CompareEqual,                   SimdBinaryOp,  INS_fcmeq,   INS_cmeq,    INS_cmeq,     None )
HARDWARE_INTRINSIC(NI_ARM64_SIMD_EQ_ZERO,         Simd,     CompareEqualZero,               SimdUnaryOp,   INS_fcmeq,   INS_cmeq,    INS_cmeq,     None )
HARDWARE_INTRINSIC(NI_ARM64_SIMD_GE,              Simd,     CompareGreaterThanOrEqual,      SimdBinaryOp,  INS_fcmge,   INS_cmge,    INS_cmhs,     None )
HARDWARE_INTRINSIC(NI_ARM64_SIMD_GE_ZERO,         Simd,     CompareGreaterThanOrEqualZero,  SimdUnaryOp,   INS_fcmge,   INS_cmge,    INS_invalid,  LowerCmpUZero )
HARDWARE_INTRINSIC(NI_ARM64_SIMD_GT,              Simd,     CompareGreaterThan,             SimdBinaryOp,  INS_fcmgt,   INS_cmgt,    INS_cmhi,     None )
HARDWARE_INTRINSIC(NI_ARM64_SIMD_GT_ZERO,         Simd,     CompareGreaterThanZero,         SimdUnaryOp,   INS_fcmgt,   INS_cmgt,    INS_invalid,  LowerCmpUZero )
HARDWARE_INTRINSIC(NI_ARM64_SIMD_LE_ZERO,         Simd,     CompareLessThanOrEqualZero,     SimdUnaryOp,   INS_fcmle,   INS_cmle,    INS_cmeq,     LowerCmpUZero )
HARDWARE_INTRINSIC(NI_ARM64_SIMD_LT_ZERO,         Simd,     CompareLessThanZero,            SimdUnaryOp,   INS_fcmlt,   INS_cmlt,    INS_invalid,  LowerCmpUZero )
HARDWARE_INTRINSIC(NI_ARM64_SIMD_TST,             Simd,     CompareTest,                    SimdBinaryOp,  INS_ctst,    INS_ctst,    INS_ctst,     None )
HARDWARE_INTRINSIC(NI_ARM64_SIMD_Div,             Simd,     Divide,                         SimdBinaryOp,  INS_fdiv,    INS_invalid, INS_invalid,  None )
HARDWARE_INTRINSIC(NI_ARM64_SIMD_Negate,          Simd,     Negate,                         SimdUnaryOp,   INS_fneg,    INS_neg,     INS_invalid,  None )
HARDWARE_INTRINSIC(NI_ARM64_SIMD_Max,             Simd,     Max,                            SimdBinaryOp,  INS_fmax,    INS_smax,    INS_umax,     None )
HARDWARE_INTRINSIC(NI_ARM64_SIMD_Min,             Simd,     Min,                            SimdBinaryOp,  INS_fmin,    INS_smin,    INS_umin,     None )
HARDWARE_INTRINSIC(NI_ARM64_SIMD_Mul,             Simd,     Multiply,                       SimdBinaryOp,  INS_fmul,    INS_mul,     INS_mul,      None )
HARDWARE_INTRINSIC(NI_ARM64_SIMD_Sqrt,            Simd,     Sqrt,                           SimdUnaryOp,   INS_fsqrt,   INS_invalid, INS_invalid,  None )
HARDWARE_INTRINSIC(NI_ARM64_SIMD_Sub,             Simd,     Subtract,                       SimdBinaryOp,  INS_fsub,    INS_sub,     INS_sub,      None )
HARDWARE_INTRINSIC(NI_ARM64_SIMD_GetItem,         Simd,     Extract,                        SimdExtractOp, INS_mov,     INS_mov,     INS_mov,      None )
HARDWARE_INTRINSIC(NI_ARM64_SIMD_SetItem,         Simd,     Insert,                         SimdInsertOp,  INS_mov,     INS_mov,     INS_mov,      None )
HARDWARE_INTRINSIC(NI_ARM64_SIMD_SetAllVector64,  Simd,     SetAllVector64,                 SimdSetAllOp,  INS_dup,     INS_dup,     INS_dup,      None )
HARDWARE_INTRINSIC(NI_ARM64_SIMD_SetAllVector128, Simd,     SetAllVector128,                SimdSetAllOp,  INS_dup,     INS_dup,     INS_dup,      None )
//Aes
HARDWARE_INTRINSIC(NI_ARM64_AesEncrypt,           Aes,      Encrypt,                        SimdBinaryRMWOp,   INS_invalid,    INS_invalid, INS_aese,      None )
HARDWARE_INTRINSIC(NI_ARM64_AesDecrypt,           Aes,      Decrypt,                        SimdBinaryRMWOp,   INS_invalid,    INS_invalid, INS_aesd,      None )
HARDWARE_INTRINSIC(NI_ARM64_AesMixColumns,        Aes,      MixColumns,                     SimdUnaryOp,           INS_invalid,    INS_invalid, INS_aesmc,     None )
HARDWARE_INTRINSIC(NI_ARM64_AesInvMixColumns,     Aes,      InverseMixColumns,              SimdUnaryOp,           INS_invalid,    INS_invalid, INS_aesimc,    None )

//Sha1
HARDWARE_INTRINSIC(NI_ARM64_Sha1Choose,          Sha1,      HashChoose,                    Sha1HashOp,             INS_invalid,    INS_invalid, INS_sha1c,      None )
HARDWARE_INTRINSIC(NI_ARM64_Sha1Parity,          Sha1,      HashParity,                    Sha1HashOp,             INS_invalid,    INS_invalid, INS_sha1p,      None )
HARDWARE_INTRINSIC(NI_ARM64_Sha1Majority,        Sha1,      HashMajority,                  Sha1HashOp,             INS_invalid,    INS_invalid, INS_sha1m,      None )
HARDWARE_INTRINSIC(NI_ARM64_Sha1FixedRotate,     Sha1,      FixedRotate,                   Sha1RotateOp,           INS_invalid,    INS_invalid, INS_sha1h,      None )
HARDWARE_INTRINSIC(NI_ARM64_Sha1SchedulePart1,   Sha1,      SchedulePart1,                 SimdTernaryRMWOp,       INS_invalid,    INS_invalid, INS_sha1su0,    None )
HARDWARE_INTRINSIC(NI_ARM64_Sha1SchedulePart2,   Sha1,      SchedulePart2,                 SimdBinaryRMWOp,        INS_invalid,    INS_invalid, INS_sha1su1,    None )

//Sha256
HARDWARE_INTRINSIC(NI_ARM64_Sha256HashLower,       Sha256,      HashLower,                 SimdTernaryRMWOp,   INS_invalid,    INS_invalid, INS_sha256h,    None )
HARDWARE_INTRINSIC(NI_ARM64_Sha256HashUpper,       Sha256,      HashUpper,                 SimdTernaryRMWOp,   INS_invalid,    INS_invalid, INS_sha256h2,   None )
HARDWARE_INTRINSIC(NI_ARM64_Sha256SchedulePart1,   Sha256,      SchedulePart1,             SimdBinaryRMWOp,    INS_invalid,    INS_invalid, INS_sha256su0,  None )
HARDWARE_INTRINSIC(NI_ARM64_Sha256SchedulePart2,   Sha256,      SchedulePart2,             SimdTernaryRMWOp,   INS_invalid,    INS_invalid, INS_sha256su1,  None )
#endif


#undef HARDWARE_INTRINSIC_CLASS
#undef HARDWARE_INTRINSIC

// clang-format on
