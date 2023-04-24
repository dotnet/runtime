/* Remarks:
 * - This table is used to drive code generation on operations that are defined by the tuple (ins->opcode, ins->inst_c0).
 * - Operand config specifies the order of operands that are to be supplied to the function or macro. These are
 *   variations of: W (width of the vector register), T (element type), D (dest reg number), S (source reg number),
 *   I (immediate value). If _REV is specifed, the order of source registers is reversed. Note that not all 
 *   options are supported. To specify more options, add the respective macros to the files that include this
 *   (e.g. mini-arm64.c).
 * - To specify that a particular operation is not supported for some data type, use  _UNDEF.
 */

/* 64-bit vectors */
/*        Width   Opcode          Function              Operand config      I8                I16               I32               I64               F32               F64         */
/*--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/
SIMD_OP  (64,  OP_XCOMPARE,    CMP_EQ,               WTDSS,              arm_neon_cmeq,    arm_neon_cmeq,    arm_neon_cmeq,    _UNDEF,           _UNDEF,           _UNDEF)
SIMD_OP  (64,  OP_XCOMPARE,    CMP_GT,               WTDSS,              arm_neon_cmgt,    arm_neon_cmgt,    arm_neon_cmgt,    _UNDEF,           _UNDEF,           _UNDEF)
SIMD_OP  (64,  OP_XCOMPARE,    CMP_GT_UN,            WTDSS,              arm_neon_cmhi,    arm_neon_cmhi,    arm_neon_cmhi,    _UNDEF,           _UNDEF,           _UNDEF)
SIMD_OP  (64,  OP_XCOMPARE,    CMP_GE,               WTDSS,              arm_neon_cmge,    arm_neon_cmge,    arm_neon_cmge,    _UNDEF,           _UNDEF,           _UNDEF)
SIMD_OP  (64,  OP_XCOMPARE,    CMP_GE_UN,            WTDSS,              arm_neon_cmhs,    arm_neon_cmhs,    arm_neon_cmhs,    _UNDEF,           _UNDEF,           _UNDEF)
SIMD_OP  (64,  OP_XCOMPARE,    CMP_LT,               WTDSS_REV,          arm_neon_cmgt,    arm_neon_cmgt,    arm_neon_cmgt,    _UNDEF,           _UNDEF,           _UNDEF)
SIMD_OP  (64,  OP_XCOMPARE,    CMP_LT_UN,            WTDSS_REV,          arm_neon_cmhi,    arm_neon_cmhi,    arm_neon_cmhi,    _UNDEF,           _UNDEF,           _UNDEF)
SIMD_OP  (64,  OP_XCOMPARE,    CMP_LE,               WTDSS_REV,          arm_neon_cmge,    arm_neon_cmge,    arm_neon_cmge,    _UNDEF,           _UNDEF,           _UNDEF)
SIMD_OP  (64,  OP_XCOMPARE,    CMP_LE_UN,            WTDSS_REV,          arm_neon_cmhs,    arm_neon_cmhs,    arm_neon_cmhs,    _UNDEF,           _UNDEF,           _UNDEF)

SIMD_OP  (64,  OP_XCOMPARE_FP, CMP_EQ,               WTDSS,               _UNDEF,          _UNDEF,           _UNDEF,           _UNDEF,           arm_neon_fcmeq,   _UNDEF)
SIMD_OP  (64,  OP_XCOMPARE_FP, CMP_GT,               WTDSS,               _UNDEF,          _UNDEF,           _UNDEF,           _UNDEF,           arm_neon_fcmgt,   _UNDEF)
SIMD_OP  (64,  OP_XCOMPARE_FP, CMP_GE,               WTDSS,               _UNDEF,          _UNDEF,           _UNDEF,           _UNDEF,           arm_neon_fcmge,   _UNDEF)
SIMD_OP  (64,  OP_XCOMPARE_FP, CMP_LT,               WTDSS_REV,           _UNDEF,          _UNDEF,           _UNDEF,           _UNDEF,           arm_neon_fcmgt,   _UNDEF)
SIMD_OP  (64,  OP_XCOMPARE_FP, CMP_LE,               WTDSS_REV,           _UNDEF,          _UNDEF,           _UNDEF,           _UNDEF,           arm_neon_fcmge,   _UNDEF)

SIMD_OP  (64,  OP_XBINOP,      OP_IADD,              WTDSS,              arm_neon_add,     arm_neon_add,     arm_neon_add,     _UNDEF,           _UNDEF,           _UNDEF)  
SIMD_OP  (64,  OP_XBINOP,      OP_FADD,              WTDSS,              _UNDEF,           _UNDEF,           _UNDEF,           _UNDEF,           arm_neon_fadd,    _UNDEF)

/* 128-bit vectors */
/*         Width  Opcode          Function              Operand config      I8                I16               I32               I64               F32               F64         */
/*--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/
SIMD_OP  (128, OP_XCOMPARE,    CMP_EQ,               WTDSS,              arm_neon_cmeq,    arm_neon_cmeq,    arm_neon_cmeq,    arm_neon_cmeq,    _UNDEF,           _UNDEF)
SIMD_OP  (128, OP_XCOMPARE,    CMP_GT,               WTDSS,              arm_neon_cmgt,    arm_neon_cmgt,    arm_neon_cmgt,    arm_neon_cmgt,    _UNDEF,           _UNDEF)
SIMD_OP  (128, OP_XCOMPARE,    CMP_GT_UN,            WTDSS,              arm_neon_cmhi,    arm_neon_cmhi,    arm_neon_cmhi,    arm_neon_cmhi,    _UNDEF,           _UNDEF)
SIMD_OP  (128, OP_XCOMPARE,    CMP_GE,               WTDSS,              arm_neon_cmge,    arm_neon_cmge,    arm_neon_cmge,    arm_neon_cmge,    _UNDEF,           _UNDEF)
SIMD_OP  (128, OP_XCOMPARE,    CMP_GE_UN,            WTDSS,              arm_neon_cmhs,    arm_neon_cmhs,    arm_neon_cmhs,    arm_neon_cmhs,    _UNDEF,           _UNDEF)
SIMD_OP  (128, OP_XCOMPARE,    CMP_LT,               WTDSS_REV,          arm_neon_cmgt,    arm_neon_cmgt,    arm_neon_cmgt,    arm_neon_cmgt,    _UNDEF,           _UNDEF)
SIMD_OP  (128, OP_XCOMPARE,    CMP_LT_UN,            WTDSS_REV,          arm_neon_cmhi,    arm_neon_cmhi,    arm_neon_cmhi,    arm_neon_cmhi,    _UNDEF,           _UNDEF)
SIMD_OP  (128, OP_XCOMPARE,    CMP_LE,               WTDSS_REV,          arm_neon_cmge,    arm_neon_cmge,    arm_neon_cmge,    arm_neon_cmge,    _UNDEF,           _UNDEF)
SIMD_OP  (128, OP_XCOMPARE,    CMP_LE_UN,            WTDSS_REV,          arm_neon_cmhs,    arm_neon_cmhs,    arm_neon_cmhs,    arm_neon_cmhs,    _UNDEF,           _UNDEF)

SIMD_OP  (128, OP_XCOMPARE_FP, CMP_EQ,               WTDSS,              _UNDEF,           _UNDEF,           _UNDEF,           _UNDEF,           arm_neon_fcmeq,   arm_neon_fcmeq)
SIMD_OP  (128, OP_XCOMPARE_FP, CMP_GT,               WTDSS,              _UNDEF,           _UNDEF,           _UNDEF,           _UNDEF,           arm_neon_fcmgt,   arm_neon_fcmgt)
SIMD_OP  (128, OP_XCOMPARE_FP, CMP_GE,               WTDSS,              _UNDEF,           _UNDEF,           _UNDEF,           _UNDEF,           arm_neon_fcmge,   arm_neon_fcmge)
SIMD_OP  (128, OP_XCOMPARE_FP, CMP_LT,               WTDSS_REV,          _UNDEF,           _UNDEF,           _UNDEF,           _UNDEF,           arm_neon_fcmgt,   arm_neon_fcmgt)
SIMD_OP  (128, OP_XCOMPARE_FP, CMP_LE,               WTDSS_REV,          _UNDEF,           _UNDEF,           _UNDEF,           _UNDEF,           arm_neon_fcmge,   arm_neon_fcmge)

SIMD_OP  (128, OP_XUNOP,       OP_SIMD_FCVTL,        DS,                 _UNDEF,           _UNDEF,           _UNDEF,          _UNDEF,            arm_neon_fcvtl,   _UNDEF)
SIMD_OP  (128, OP_XUNOP,       OP_SIMD_FCVTL2,       DS,                 _UNDEF,           _UNDEF,           _UNDEF,          _UNDEF,            arm_neon_fcvtl2,  _UNDEF) 
SIMD_OP  (128, OP_XUNOP,       OP_ARM64_SXTL,        TDS,                arm_neon_sxtl,    arm_neon_sxtl,    arm_neon_sxtl,   _UNDEF,            _UNDEF,           _UNDEF)
SIMD_OP  (128, OP_XUNOP,       OP_ARM64_SXTL2,       TDS,                arm_neon_sxtl2,   arm_neon_sxtl2,   arm_neon_sxtl2,  _UNDEF,            _UNDEF,           _UNDEF)
SIMD_OP  (128, OP_XUNOP,       OP_ARM64_UXTL,        TDS,                arm_neon_uxtl,    arm_neon_uxtl,    arm_neon_uxtl,   _UNDEF,            _UNDEF,           _UNDEF)
SIMD_OP  (128, OP_XUNOP,       OP_ARM64_UXTL2,       TDS,                arm_neon_uxtl2,   arm_neon_uxtl2,   arm_neon_uxtl2,  _UNDEF,            _UNDEF,           _UNDEF)
SIMD_OP  (128, OP_XUNOP,       OP_CVT_FP_SI,         WTDS,               _UNDEF,           _UNDEF,           arm_neon_fcvtzs, arm_neon_fcvtzs,   _UNDEF,           _UNDEF)
SIMD_OP  (128, OP_XUNOP,       OP_CVT_FP_UI,         WTDS,               _UNDEF,           _UNDEF,           arm_neon_fcvtzu, arm_neon_fcvtzu,   _UNDEF,           _UNDEF)
SIMD_OP  (128, OP_XUNOP,       OP_CVT_SI_FP,         WTDS,               _UNDEF,           _UNDEF,           _UNDEF,          _UNDEF,            arm_neon_scvtf,   arm_neon_scvtf)
SIMD_OP  (128, OP_XUNOP,       OP_CVT_UI_FP,         WTDS,               _UNDEF,           _UNDEF,           _UNDEF,          _UNDEF,            arm_neon_ucvtf,   arm_neon_ucvtf)
SIMD_OP  (128, OP_XBINOP,      OP_IADD,              WTDSS,              arm_neon_add,     arm_neon_add,     arm_neon_add,    arm_neon_add,      _UNDEF,           _UNDEF)
SIMD_OP  (128, OP_XBINOP,      OP_FADD,              WTDSS,              _UNDEF,           _UNDEF,           _UNDEF,          _UNDEF,            arm_neon_fadd,    arm_neon_fadd)
SIMD_OP  (128, OP_XBINOP,      OP_ISUB,              WTDSS,              arm_neon_sub,     arm_neon_sub,     arm_neon_sub,    arm_neon_sub,      _UNDEF,           _UNDEF)
SIMD_OP  (128, OP_XBINOP,      OP_FSUB,              WTDSS,              _UNDEF,           _UNDEF,           _UNDEF,          _UNDEF,            arm_neon_fsub,    arm_neon_fsub)
SIMD_OP  (128, OP_XBINOP,      OP_IMAX,              WTDSS,              arm_neon_smax,    arm_neon_smax,    arm_neon_smax,   _SKIP,             _UNDEF,           _UNDEF)
SIMD_OP  (128, OP_XBINOP,      OP_IMAX_UN,           WTDSS,              arm_neon_umax,    arm_neon_umax,    arm_neon_umax,   _SKIP,             _UNDEF,           _UNDEF)
SIMD_OP  (128, OP_XBINOP,      OP_FMAX,              WTDSS,              _UNDEF,           _UNDEF,           _UNDEF,          _UNDEF,            arm_neon_fmax,    arm_neon_fmax)
SIMD_OP  (128, OP_XBINOP,      OP_IMIN,              WTDSS,              arm_neon_smin,    arm_neon_smin,    arm_neon_smin,   _SKIP,             _UNDEF,           _UNDEF)
SIMD_OP  (128, OP_XBINOP,      OP_IMIN_UN,           WTDSS,              arm_neon_umin,    arm_neon_umin,    arm_neon_umin,   _SKIP,             _UNDEF,           _UNDEF)
SIMD_OP  (128, OP_XBINOP,      OP_FMIN,              WTDSS,              _UNDEF,           _UNDEF,           _UNDEF,          _UNDEF,            arm_neon_fmin,    arm_neon_fmin)
SIMD_OP  (128, OP_XBINOP,      OP_IMUL,              WTDSS,              arm_neon_mul,     arm_neon_mul,     arm_neon_mul,    _UNDEF,            _UNDEF,           _UNDEF)
SIMD_OP  (128, OP_XBINOP,      OP_FMUL,              WTDSS,              _UNDEF,           _UNDEF,           _UNDEF,          _UNDEF,            arm_neon_fmul,    arm_neon_fmul)
SIMD_OP  (128, OP_XBINOP,      OP_FDIV,              WTDSS,              _UNDEF,           _UNDEF,           _UNDEF,          _UNDEF,            arm_neon_fdiv,    arm_neon_fdiv)
SIMD_OP  (128, OP_XBINOP_FORCEINT,    XBINOP_FORCEINT_AND,    WDSS,      arm_neon_and,     arm_neon_and,     arm_neon_and,    arm_neon_and,      arm_neon_and,     arm_neon_and)
SIMD_OP  (128, OP_XBINOP_FORCEINT,    XBINOP_FORCEINT_OR,     WDSS,      arm_neon_orr,     arm_neon_orr,     arm_neon_orr,    arm_neon_orr,      arm_neon_orr,     arm_neon_orr)
SIMD_OP  (128, OP_XBINOP_FORCEINT,    XBINOP_FORCEINT_XOR,    WDSS,      arm_neon_eor,     arm_neon_eor,     arm_neon_eor,    arm_neon_eor,      arm_neon_eor,     arm_neon_eor)
SIMD_OP  (128, OP_ARM64_XADDV, INTRINS_AARCH64_ADV_SIMD_UADDV, WTDS,     arm_neon_addv,    arm_neon_addv,    arm_neon_addv,   _SKIP,             _UNDEF,           _UNDEF)
SIMD_OP  (128, OP_ARM64_XADDV, INTRINS_AARCH64_ADV_SIMD_SADDV, WTDS,     arm_neon_addv,    arm_neon_addv,    arm_neon_addv,   _SKIP,             _UNDEF,           _UNDEF)
SIMD_OP  (128, OP_ARM64_XADDV, INTRINS_AARCH64_ADV_SIMD_FADDV, WTDS,     _UNDEF,           _UNDEF,           _UNDEF,          _UNDEF,            _SKIP,            _SKIP)
SIMD_OP  (128, OP_XOP_OVR_X_X, INTRINS_AARCH64_ADV_SIMD_FRINTP, WTDS,    _UNDEF,           _UNDEF,           _UNDEF,          _UNDEF,            arm_neon_frintp,  arm_neon_frintp)
SIMD_OP  (128, OP_XOP_OVR_X_X, INTRINS_AARCH64_ADV_SIMD_FRINTM, WTDS,    _UNDEF,           _UNDEF,           _UNDEF,          _UNDEF,            arm_neon_frintm,  arm_neon_frintm)
SIMD_OP  (128, OP_XOP_OVR_X_X, INTRINS_AARCH64_ADV_SIMD_FSQRT,  WTDS,    _UNDEF,           _UNDEF,           _UNDEF,          _UNDEF,            arm_neon_fsqrt,   arm_neon_fsqrt)
SIMD_OP  (128, OP_XOP_OVR_X_X, INTRINS_AARCH64_ADV_SIMD_ABS,    WTDS,    arm_neon_abs,     arm_neon_abs,     arm_neon_abs,    arm_neon_abs,      _UNDEF,           _UNDEF)
SIMD_OP  (128, OP_XOP_OVR_X_X, INTRINS_AARCH64_ADV_SIMD_FABS,   WTDS,    _UNDEF,           _UNDEF,           _UNDEF,          _UNDEF,            arm_neon_fabs,    arm_neon_fabs)
