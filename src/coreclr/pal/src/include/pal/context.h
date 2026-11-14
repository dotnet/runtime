// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++
Module Name:
    include/pal/context.h

Abstract:
    Header file for thread context utility functions.
--*/

#ifndef _PAL_CONTEXT_H_
#define _PAL_CONTEXT_H_

#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

#include <signal.h>
#include <pthread.h>

#include <minipal/cpuid.h>

/* A type to wrap the native context type, which is ucontext_t on some
 * platforms and another type elsewhere. */
#if HAVE_UCONTEXT_T
#if HAVE_UCONTEXT_H
#include <ucontext.h>
#endif // HAVE_UCONTEXT_H

typedef ucontext_t native_context_t;
#elif defined(TARGET_WASI)
// WASI (wasm32-wasip2 in wasi-sdk 33) has no signal/ucontext support: <ucontext.h>
// is absent and <signal.h> hides siginfo_t/FPE_*/ILL_* behind
// __wasilibc_unmodified_upstream. PAL declarations still reference these types
// at the header level even though the call sites are excluded from compilation
// on WASI (exception/signal.cpp, exception/seh-unwind.cpp). Provide opaque
// native_context_t here; siginfo_t lives in pal/wasi/pal_wasi_missing.h
// which is pulled in below.
//
// TODO: delete when wasi-libc exposes siginfo_t and a ucontext_t (no upstream
// plan as of WASI 0.2.8).
typedef struct { int _placeholder; } native_context_t;
#include "pal/wasi/pal_wasi_missing.h"
#else   // HAVE_UCONTEXT_T
#error Native context type is not known on this platform!
#endif  // HAVE_UCONTEXT_T

#include <unix/NativeContextRegs.h>

#if HAVE_MACH_EXCEPTIONS
#include <mach/kern_return.h>
#include <mach/mach_port.h>
#endif // HAVE_MACH_EXCEPTIONS

#if defined(XSTATE_SUPPORTED) || (defined(HOST_AMD64) && defined(HAVE_MACH_EXCEPTIONS))
bool Xstate_IsAvx512Supported();
#endif // XSTATE_SUPPORTED || (HOST_AMD64 && HAVE_MACH_EXCEPTIONS)

#if defined(HOST_64BIT) && defined(HOST_ARM64) && !defined(TARGET_FREEBSD) && !defined(__APPLE__)
#if !defined(SVE_MAGIC)

// Add the missing SVE defines

#define EXTRA_MAGIC 0x45585401

struct extra_context {
    struct _aarch64_ctx head;
    __u64 datap; /* 16-byte aligned pointer to extra space cast to __u64 */
    __u32 size; /* size in bytes of the extra space */
    __u32 __reserved[3];
};

#define SVE_MAGIC   0x53564501

struct sve_context {
    struct _aarch64_ctx head;
    __u16 vl;
    __u16 flags;
    __u16 __reserved[2];
};

#define __SVE_VQ_BYTES      16  /* number of bytes per quadword */

#define __SVE_NUM_ZREGS     32
#define __SVE_NUM_PREGS     16

#define sve_vq_from_vl(vl)    ((vl) / __SVE_VQ_BYTES)
#define sve_vl_from_vq(vq)    ((vq) * __SVE_VQ_BYTES)

#define __SVE_ZREG_SIZE(vq) ((__u32)(vq) * __SVE_VQ_BYTES)
#define __SVE_PREG_SIZE(vq) ((__u32)(vq) * (__SVE_VQ_BYTES / 8))
#define __SVE_FFR_SIZE(vq)  __SVE_PREG_SIZE(vq)

#define __SVE_ZREGS_OFFSET  0
#define __SVE_ZREG_OFFSET(vq, n) \
    (__SVE_ZREGS_OFFSET + __SVE_ZREG_SIZE(vq) * (n))
#define __SVE_ZREGS_SIZE(vq) \
    (__SVE_ZREG_OFFSET(vq, __SVE_NUM_ZREGS) - __SVE_ZREGS_OFFSET)

#define __SVE_PREGS_OFFSET(vq) \
    (__SVE_ZREGS_OFFSET + __SVE_ZREGS_SIZE(vq))
#define __SVE_PREG_OFFSET(vq, n) \
    (__SVE_PREGS_OFFSET(vq) + __SVE_PREG_SIZE(vq) * (n))
#define __SVE_PREGS_SIZE(vq) \
    (__SVE_PREG_OFFSET(vq, __SVE_NUM_PREGS) - __SVE_PREGS_OFFSET(vq))

#define __SVE_FFR_OFFSET(vq) \
    (__SVE_PREGS_OFFSET(vq) + __SVE_PREGS_SIZE(vq))

#define SVE_SIG_ZREG_SIZE(vq)   __SVE_ZREG_SIZE(vq)
#define SVE_SIG_PREG_SIZE(vq)   __SVE_PREG_SIZE(vq)
#define SVE_SIG_FFR_SIZE(vq)    __SVE_FFR_SIZE(vq)

#define SVE_SIG_REGS_OFFSET                 \
    ((sizeof(struct sve_context) + (__SVE_VQ_BYTES - 1))    \
        / __SVE_VQ_BYTES * __SVE_VQ_BYTES)

#define SVE_SIG_ZREGS_OFFSET \
        (SVE_SIG_REGS_OFFSET + __SVE_ZREGS_OFFSET)
#define SVE_SIG_ZREG_OFFSET(vq, n) \
        (SVE_SIG_REGS_OFFSET + __SVE_ZREG_OFFSET(vq, n))
#define SVE_SIG_ZREGS_SIZE(vq) __SVE_ZREGS_SIZE(vq)

#define SVE_SIG_PREGS_OFFSET(vq) \
        (SVE_SIG_REGS_OFFSET + __SVE_PREGS_OFFSET(vq))
#define SVE_SIG_PREG_OFFSET(vq, n) \
        (SVE_SIG_REGS_OFFSET + __SVE_PREG_OFFSET(vq, n))
#define SVE_SIG_PREGS_SIZE(vq) __SVE_PREGS_SIZE(vq)

#define SVE_SIG_FFR_OFFSET(vq) \
        (SVE_SIG_REGS_OFFSET + __SVE_FFR_OFFSET(vq))

#define SVE_SIG_REGS_SIZE(vq) \
        (__SVE_FFR_OFFSET(vq) + __SVE_FFR_SIZE(vq))

#define SVE_SIG_CONTEXT_SIZE(vq) \
        (SVE_SIG_REGS_OFFSET + SVE_SIG_REGS_SIZE(vq))

#endif // SVE_MAGIC
#endif // HOST_64BIT && HOST_ARM64 && !TARGET_FREEBSD && !__APPLE__

#if defined(HOST_S390X) || defined(HOST_POWERPC64) || defined(HOST_WASM)

#elif HAVE___GREGSET_T

#if defined(HOST_64BIT) && !defined(HOST_LOONGARCH64) && !defined(HOST_RISCV64)

#define FPREG_Xmm(uc, index) *(M128A*)&(((struct fxsave*)(&(uc)->uc_mcontext.__fpregs))->fx_xmm[index])

#define FPREG_St(uc, index) *(M128A*)&(((struct fxsave*)(&(uc)->uc_mcontext.__fpregs))->fx_87_ac[index])

#define FPREG_ControlWord(uc) (((struct fxsave*)(&(uc)->uc_mcontext.__fpregs))->fx_cw)
#define FPREG_StatusWord(uc) (((struct fxsave*)(&(uc)->uc_mcontext.__fpregs))->fx_sw)
#define FPREG_TagWord(uc) (((struct fxsave*)(&(uc)->uc_mcontext.__fpregs))->fx_tw)
#define FPREG_ErrorOffset(uc) (*(DWORD*) &(((struct fxsave*)(&(uc)->uc_mcontext.__fpregs))->fx_ip))
#define FPREG_ErrorSelector(uc) *((WORD*) &(((struct fxsave*)(&(uc)->uc_mcontext.__fpregs))->fx_ip) + 2)
#define FPREG_DataOffset(uc) (*(DWORD*) &(((struct fxsave*)(&(uc)->uc_mcontext.__fpregs))->fx_dp))
#define FPREG_DataSelector(uc) *((WORD*) &(((struct fxsave*)(&(uc)->uc_mcontext.__fpregs))->fx_dp) + 2)
#define FPREG_MxCsr(uc) (((struct fxsave*)(&(uc)->uc_mcontext.__fpregs))->fx_mxcsr)
#define FPREG_MxCsr_Mask(uc) (((struct fxsave*)(&(uc)->uc_mcontext.__fpregs))->fx_mxcsr_mask)

#endif // HOST_64BIT && !HOST_LOONGARCH64 && !HOST_RISCV64

#elif HAVE_GREGSET_T

#ifdef HOST_64BIT

#if HAVE_FPREGS_WITH_CW
#define FPREG_Fpstate(uc) (&((uc)->uc_mcontext.fpregs.fp_reg_set.fpchip_state))

#define FPREG_Xmm(uc, index) *(M128A*)&(FPREG_Fpstate(uc)->xmm[index])
#define FPREG_St(uc, index) *(M128A*)&(FPREG_Fpstate(uc)->st[index])
#define FPREG_ControlWord(uc) (FPREG_Fpstate(uc)->cw)
#define FPREG_StatusWord(uc) (FPREG_Fpstate(uc)->sw)
#define FPREG_MxCsr_Mask(uc) (FPREG_Fpstate(uc)->mxcsr_mask)

// on SunOS, fctw and __fx_rsvd are uint8_t, whereas on linux ftw is uint16_t,
// so we use split and join technique for these two uint8_t members at call sites.
#define FPREG_TagWord1(uc) (FPREG_Fpstate(uc)->fctw)
#define FPREG_TagWord2(uc) (FPREG_Fpstate(uc)->__fx_rsvd)
#else
#define FPREG_Fpstate(uc) ((uc)->uc_mcontext.fpregs)

#define FPREG_Xmm(uc, index) *(M128A*)&(FPREG_Fpstate(uc)->_xmm[index])
#define FPREG_St(uc, index) *(M128A*)&(FPREG_Fpstate(uc)->_st[index])
#define FPREG_ControlWord(uc) (FPREG_Fpstate(uc)->cwd)
#define FPREG_StatusWord(uc) (FPREG_Fpstate(uc)->swd)
#define FPREG_TagWord(uc) (FPREG_Fpstate(uc)->ftw)
#define FPREG_MxCsr_Mask(uc) (FPREG_Fpstate(uc)->mxcr_mask)
#endif

#define FPREG_ErrorOffset(uc) *(DWORD*)&(FPREG_Fpstate(uc)->rip)
#define FPREG_ErrorSelector(uc) *(((WORD*)&(FPREG_Fpstate(uc)->rip)) + 2)
#define FPREG_DataOffset(uc) *(DWORD*)&(FPREG_Fpstate(uc)->rdp)
#define FPREG_DataSelector(uc) *(((WORD*)&(FPREG_Fpstate(uc)->rdp)) + 2)
#define FPREG_MxCsr(uc) (FPREG_Fpstate(uc)->mxcsr)

/////////////////////
// Extended state

#include <unix/NativeContextXstate.h>

#if defined(XSTATE_SUPPORTED) && defined(HOST_AMD64)

#ifndef XFEATURE_MASK_YMM
#define XFEATURE_MASK_YMM (1 << XSTATE_AVX)
#endif // XFEATURE_MASK_YMM

#ifndef XFEATURE_MASK_OPMASK
#define XFEATURE_MASK_OPMASK (1 << XSTATE_AVX512_KMASK)
#endif // XFEATURE_MASK_OPMASK

#ifndef XFEATURE_MASK_ZMM_Hi256
#define XFEATURE_MASK_ZMM_Hi256 (1 << XSTATE_AVX512_ZMM_H)
#endif // XFEATURE_MASK_ZMM_Hi256

#ifndef XFEATURE_MASK_Hi16_ZMM
#define XFEATURE_MASK_Hi16_ZMM (1 << XSTATE_AVX512_ZMM)
#endif // XFEATURE_MASK_Hi16_ZMM

#ifndef XFEATURE_MASK_AVX512
#define XFEATURE_MASK_AVX512 (XFEATURE_MASK_OPMASK | XFEATURE_MASK_ZMM_Hi256 | XFEATURE_MASK_Hi16_ZMM)
#endif // XFEATURE_MASK_AVX512

inline bool FPREG_HasYmmRegisters(const ucontext_t *uc)
{
    if (!FPREG_HasExtendedState(uc))
    {
        return false;
    }

    return (FPREG_FpxSwBytes_xfeatures(uc) & XFEATURE_MASK_YMM) == XFEATURE_MASK_YMM;
}

inline void *FPREG_Xstate_Ymmh(const ucontext_t *uc, uint32_t *featureSize)
{
    _ASSERTE(FPREG_HasYmmRegisters(uc));
    return FPREG_Xstate_ExtendedFeature(uc, featureSize, XSTATE_AVX);
}

inline bool FPREG_HasAvx512Registers(const ucontext_t *uc)
{
    if (!FPREG_HasExtendedState(uc))
    {
        return false;
    }

    if ((FPREG_FpxSwBytes_xfeatures(uc) & XFEATURE_MASK_AVX512) != XFEATURE_MASK_AVX512)
    {
        return false;
    }

    _ASSERTE(FPREG_HasYmmRegisters(uc));
    return Xstate_IsAvx512Supported();
}

inline void *FPREG_Xstate_Opmask(const ucontext_t *uc, uint32_t *featureSize)
{
    _ASSERTE(FPREG_HasAvx512Registers(uc));
    return FPREG_Xstate_ExtendedFeature(uc, featureSize, XSTATE_AVX512_KMASK);
}

inline void *FPREG_Xstate_ZmmHi256(const ucontext_t *uc, uint32_t *featureSize)
{
    _ASSERTE(FPREG_HasAvx512Registers(uc));
    return FPREG_Xstate_ExtendedFeature(uc, featureSize, XSTATE_AVX512_ZMM_H);
}

inline void *FPREG_Xstate_Hi16Zmm(const ucontext_t *uc, uint32_t *featureSize)
{
    _ASSERTE(FPREG_HasAvx512Registers(uc));
    return FPREG_Xstate_ExtendedFeature(uc, featureSize, XSTATE_AVX512_ZMM);
}
#endif // XSTATE_SUPPORTED && HOST_AMD64

/////////////////////

#endif // HOST_64BIT

#else // HAVE_GREGSET_T

#ifdef HOST_64BIT

#if defined(HOST_ARM64)

#if defined(TARGET_FREEBSD)

inline
struct fpregs* GetNativeSigSimdContext(native_context_t *mc)
{
    return &(mc->uc_mcontext.mc_fpregs);
}

inline
const struct fpregs* GetConstNativeSigSimdContext(const native_context_t *mc)
{
    return GetNativeSigSimdContext(const_cast<native_context_t*>(mc));
}

#elif !defined(__APPLE__) // TARGET_FREEBSD

void _GetNativeSigSimdContext(uint8_t *data, uint32_t size, fpsimd_context **fp_ptr, sve_context **sve_ptr);

inline
void GetNativeSigSimdContext(native_context_t *mc, fpsimd_context **fp_ptr, sve_context **sve_ptr)
{
    _GetNativeSigSimdContext((uint8_t *)&mc->uc_mcontext.__reserved[0], sizeof(mc->uc_mcontext.__reserved), fp_ptr, sve_ptr);
}

inline
void GetConstNativeSigSimdContext(const native_context_t *mc, fpsimd_context const **fp_ptr, sve_context const **sve_ptr)
{
    GetNativeSigSimdContext(const_cast<native_context_t*>(mc), const_cast<fpsimd_context **>(fp_ptr), const_cast<sve_context **>(sve_ptr));
}

#else // __APPLE__

inline
_STRUCT_ARM_NEON_STATE64* GetNativeSigSimdContext(native_context_t *mc)
{
    return &(mc)->uc_mcontext->__ns;
}

inline
const _STRUCT_ARM_NEON_STATE64* GetConstNativeSigSimdContext(const native_context_t *mc)
{
    return GetNativeSigSimdContext(const_cast<native_context_t*>(mc));
}

#endif // __APPLE__

#elif defined(HOST_LOONGARCH64)

#else // HOST_ARM64

#ifdef __APPLE__

#define FPSTATE(uc)             ((uc)->uc_mcontext->__fs)
#define FPREG_ControlWord(uc)   *((WORD*)&FPSTATE(uc).__fpu_fcw)
#define FPREG_StatusWord(uc)    *((WORD*)&FPSTATE(uc).__fpu_fsw)
#define FPREG_TagWord(uc)       FPSTATE(uc).__fpu_ftw
#define FPREG_MxCsr(uc)         FPSTATE(uc).__fpu_mxcsr
#define FPREG_MxCsr_Mask(uc)    FPSTATE(uc).__fpu_mxcsrmask
#define FPREG_ErrorOffset(uc)   *(DWORD*) &(FPSTATE(uc).__fpu_ip)
#define FPREG_ErrorSelector(uc) *((WORD*) &(FPSTATE(uc).__fpu_ip) + 2)
#define FPREG_DataOffset(uc)    *(DWORD*) &(FPSTATE(uc).__fpu_dp)
#define FPREG_DataSelector(uc)  *((WORD*) &(FPSTATE(uc).__fpu_dp) + 2)

#define FPREG_Xmm(uc, index)    *(M128A*) &((&FPSTATE(uc).__fpu_xmm0)[index])
#define FPREG_St(uc, index)     *(M128A*) &((&FPSTATE(uc).__fpu_stmm0)[index]) //.fp_acc)

inline bool FPREG_HasYmmRegisters(const ucontext_t *uc)
{
    _ASSERTE((uc->uc_mcsize == sizeof(_STRUCT_MCONTEXT_AVX64)) || (uc->uc_mcsize == sizeof(_STRUCT_MCONTEXT_AVX512_64)));
    return (uc->uc_mcsize == sizeof(_STRUCT_MCONTEXT_AVX64)) || (uc->uc_mcsize == sizeof(_STRUCT_MCONTEXT_AVX512_64));
}

static_assert(offsetof(_STRUCT_X86_AVX_STATE64, __fpu_ymmh0) == offsetof(_STRUCT_X86_AVX512_STATE64, __fpu_ymmh0));

inline void *FPREG_Xstate_Ymmh(const ucontext_t *uc, uint32_t *featureSize)
{
    _ASSERTE(FPREG_HasYmmRegisters(uc));
    _ASSERTE(featureSize != nullptr);

    *featureSize = sizeof(_STRUCT_XMM_REG) * 16;
    return reinterpret_cast<void *>(&((_STRUCT_X86_AVX_STATE64&)FPSTATE(uc)).__fpu_ymmh0);
}

inline bool FPREG_HasAvx512Registers(const ucontext_t *uc)
{
    _ASSERTE((uc->uc_mcsize == sizeof(_STRUCT_MCONTEXT_AVX64)) || (uc->uc_mcsize == sizeof(_STRUCT_MCONTEXT_AVX512_64)));
    return (uc->uc_mcsize == sizeof(_STRUCT_MCONTEXT_AVX512_64));
}

inline void *FPREG_Xstate_Opmask(const ucontext_t *uc, uint32_t *featureSize)
{
    _ASSERTE(FPREG_HasAvx512Registers(uc));
    _ASSERTE(featureSize != nullptr);

    *featureSize = sizeof(_STRUCT_OPMASK_REG) * 8;
    return reinterpret_cast<void *>(&((_STRUCT_X86_AVX512_STATE64&)FPSTATE(uc)).__fpu_k0);
}

inline void *FPREG_Xstate_ZmmHi256(const ucontext_t *uc, uint32_t *featureSize)
{
    _ASSERTE(FPREG_HasAvx512Registers(uc));
    _ASSERTE(featureSize != nullptr);

    *featureSize = sizeof(_STRUCT_YMM_REG) * 16;
    return reinterpret_cast<void *>(&((_STRUCT_X86_AVX512_STATE64&)FPSTATE(uc)).__fpu_zmmh0);
}

inline void *FPREG_Xstate_Hi16Zmm(const ucontext_t *uc, uint32_t *featureSize)
{
    _ASSERTE(FPREG_HasAvx512Registers(uc));
    _ASSERTE(featureSize != nullptr);

    *featureSize = sizeof(_STRUCT_ZMM_REG) * 16;
    return reinterpret_cast<void *>(&((_STRUCT_X86_AVX512_STATE64&)FPSTATE(uc)).__fpu_zmm16);
}
#elif defined(TARGET_HAIKU)

// Haiku: missing SegCs

#define FPSTATE(uc)             ((uc)->uc_mcontext.fpu)
#define FPREG_ControlWord(uc)   FPSTATE(uc).fp_fxsave.control
#define FPREG_StatusWord(uc)    FPSTATE(uc).fp_fxsave.status
#define FPREG_TagWord(uc)       FPSTATE(uc).fp_fxsave.tag
#define FPREG_MxCsr(uc)         FPSTATE(uc).fp_fxsave.mxcsr
#define FPREG_MxCsr_Mask(uc)    FPSTATE(uc).fp_fxsave.mxcsr_mask
#define FPREG_ErrorOffset(uc)   *(DWORD*) &(FPSTATE(uc).fp_fxsave.rip)
#define FPREG_ErrorSelector(uc) *((WORD*) &(FPSTATE(uc).fp_fxsave.rip) + 2)
#define FPREG_DataOffset(uc)    *(DWORD*) &(FPSTATE(uc).fp_fxsave.rdp)
#define FPREG_DataSelector(uc)  *((WORD*) &(FPSTATE(uc).fp_fxsave.rdp) + 2)

#define FPREG_Xmm(uc, index)    *(M128A*) &(FPSTATE(uc).fp_fxsave.xmm[index])
#define FPREG_St(uc, index)     *(M128A*) &(FPSTATE(uc).fp_fxsave.fp[index].value)
#elif defined(TARGET_OPENBSD)

  // from machine/fpu.h: struct fxsave64, referenced via sigcontext::sc_fpstate
#define FPSTATE(uc)             ((struct fxsave64*)((uc)->sc_fpstate))
#define FPREG_ControlWord(uc)   FPSTATE(uc)->fx_fcw
#define FPREG_StatusWord(uc)    FPSTATE(uc)->fx_fsw
#define FPREG_TagWord(uc)       FPSTATE(uc)->fx_ftw
#define FPREG_MxCsr(uc)         FPSTATE(uc)->fx_mxcsr
#define FPREG_MxCsr_Mask(uc)    FPSTATE(uc)->fx_mxcsr_mask
#define FPREG_ErrorOffset(uc)   *(DWORD*) &(FPSTATE(uc)->fx_rip)
#define FPREG_ErrorSelector(uc) *((WORD*) &(FPSTATE(uc)->fx_rip) + 2)
#define FPREG_DataOffset(uc)    *(DWORD*) &(FPSTATE(uc)->fx_rdp)
#define FPREG_DataSelector(uc)  *((WORD*) &(FPSTATE(uc)->fx_rdp) + 2)

#define FPREG_Xmm(uc, index)    *(M128A*) &(FPSTATE(uc)->fx_xmm[index])
#define FPREG_St(uc, index)     *(M128A*) &(FPSTATE(uc)->fx_st[index])
#else //__APPLE__

  // from x86/fpu.h: struct __envxmm64
#define FPSTATE(uc)             ((savefpu*)((uc)->uc_mcontext.mc_fpstate))
#define FPREG_ControlWord(uc)   FPSTATE(uc)->sv_env.en_cw
#define FPREG_StatusWord(uc)    FPSTATE(uc)->sv_env.en_sw
#define FPREG_TagWord(uc)       FPSTATE(uc)->sv_env.en_tw
#define FPREG_MxCsr(uc)         FPSTATE(uc)->sv_env.en_mxcsr
#define FPREG_MxCsr_Mask(uc)    FPSTATE(uc)->sv_env.en_mxcsr_mask
#define FPREG_ErrorOffset(uc)   *(DWORD*) &(FPSTATE(uc)->sv_env.en_rip)
#define FPREG_ErrorSelector(uc) *((WORD*) &(FPSTATE(uc)->sv_env.en_rip) + 2)
#define FPREG_DataOffset(uc)    *(DWORD*) &(FPSTATE(uc)->sv_env.en_rdp)
#define FPREG_DataSelector(uc)  *((WORD*) &(FPSTATE(uc)->sv_env.en_rdp) + 2)

#define FPREG_Xmm(uc, index)    *(M128A*) &(FPSTATE(uc)->sv_xmm[index])
#define FPREG_St(uc, index)     *(M128A*) &(FPSTATE(uc)->sv_fp[index].fp_acc)
#endif // __APPLE__
#endif // HOST_ARM64

#else // HOST_64BIT

#if defined(HOST_ARM)

// Flatterned layout of the arm kernel struct vfp_sigframe
struct VfpSigFrame
{
    DWORD   magic;
    DWORD   size;
    DWORD64 D[32]; // Some arm cpus have 16 D registers.  The kernel will ignore the extra.
    DWORD   Fpscr;
    DWORD   Padding;
    DWORD   Fpexc;
    DWORD   Fpinst;
    DWORD   Fpinst2;
    DWORD   Padding2;
};

inline
VfpSigFrame* GetNativeSigSimdContext(native_context_t *mc)
{
    size_t size = 0;

    const DWORD VfpMagic = 0x56465001; // VFP_MAGIC from arm kernel

    do
    {
        VfpSigFrame* fp = reinterpret_cast<VfpSigFrame *>(&mc->uc_regspace[size]);

        if (fp->magic == VfpMagic)
        {
            _ASSERTE(fp->size == sizeof(VfpSigFrame));
            _ASSERTE(size + fp->size <= sizeof(mc->uc_regspace));

            return fp;
        }

        if (fp->size == 0)
        {
            break;
        }

        size += fp->size;
    } while (size + sizeof(VfpSigFrame) <= sizeof(mc->uc_regspace));

    // VFP is not required on all armv7 processors, this structure may not be present

    return nullptr;
}

inline
const VfpSigFrame* GetConstNativeSigSimdContext(const native_context_t *mc)
{
    return GetNativeSigSimdContext(const_cast<native_context_t*>(mc));
}

#else
#error "Unsupported arch"
#endif

#endif // HOST_64BIT

#endif // HAVE_GREGSET_T

#if HAVE_PT_REGS

#ifdef HOST_64BIT

#if defined(HOST_LOONGARCH64)

#define PTREG_R0(ptreg)      ((ptreg).regs[0])
#define PTREG_Ra(ptreg)      ((ptreg).regs[1])
#define PTREG_Tp(ptreg)      ((ptreg).regs[2])
#define PTREG_Sp(ptreg)      ((ptreg).regs[3])
#define PTREG_A0(ptreg)      ((ptreg).regs[4])
#define PTREG_A1(ptreg)      ((ptreg).regs[5])
#define PTREG_A2(ptreg)      ((ptreg).regs[6])
#define PTREG_A3(ptreg)      ((ptreg).regs[7])
#define PTREG_A4(ptreg)      ((ptreg).regs[8])
#define PTREG_A5(ptreg)      ((ptreg).regs[9])
#define PTREG_A6(ptreg)      ((ptreg).regs[10])
#define PTREG_A7(ptreg)      ((ptreg).regs[11])
#define PTREG_T0(ptreg)      ((ptreg).regs[12])
#define PTREG_T1(ptreg)      ((ptreg).regs[13])
#define PTREG_T2(ptreg)      ((ptreg).regs[14])
#define PTREG_T3(ptreg)      ((ptreg).regs[15])
#define PTREG_T4(ptreg)      ((ptreg).regs[16])
#define PTREG_T5(ptreg)      ((ptreg).regs[17])
#define PTREG_T6(ptreg)      ((ptreg).regs[18])
#define PTREG_T7(ptreg)      ((ptreg).regs[19])
#define PTREG_T8(ptreg)      ((ptreg).regs[20])
#define PTREG_X0(ptreg)      ((ptreg).regs[21])
#define PTREG_Fp(ptreg)      ((ptreg).regs[22])
#define PTREG_S0(ptreg)      ((ptreg).regs[23])
#define PTREG_S1(ptreg)      ((ptreg).regs[24])
#define PTREG_S2(ptreg)      ((ptreg).regs[25])
#define PTREG_S3(ptreg)      ((ptreg).regs[26])
#define PTREG_S4(ptreg)      ((ptreg).regs[27])
#define PTREG_S5(ptreg)      ((ptreg).regs[28])
#define PTREG_S6(ptreg)      ((ptreg).regs[29])
#define PTREG_S7(ptreg)      ((ptreg).regs[30])
#define PTREG_S8(ptreg)      ((ptreg).regs[31])
#define PTREG_Pc(ptreg)      ((ptreg).csr_epc)

#endif // HOST_LOONGARCH64

#if defined(HOST_POWERPC64)
#define PTREG_R0(ptreg)        ((ptreg).gpr[0])
#define PTREG_R1(ptreg)        ((ptreg).gpr[1])
#define PTREG_R2(ptreg)        ((ptreg).gpr[2])
#define PTREG_R3(ptreg)        ((ptreg).gpr[3])
#define PTREG_R4(ptreg)        ((ptreg).gpr[4])
#define PTREG_R5(ptreg)        ((ptreg).gpr[5])
#define PTREG_R6(ptreg)        ((ptreg).gpr[6])
#define PTREG_R7(ptreg)        ((ptreg).gpr[7])
#define PTREG_R8(ptreg)        ((ptreg).gpr[8])
#define PTREG_R9(ptreg)        ((ptreg).gpr[9])
#define PTREG_R10(ptreg)       ((ptreg).gpr[10])
#define PTREG_R11(ptreg)       ((ptreg).gpr[11])
#define PTREG_R12(ptreg)       ((ptreg).gpr[12])
#define PTREG_R13(ptreg)       ((ptreg).gpr[13])
#define PTREG_R14(ptreg)       ((ptreg).gpr[14])
#define PTREG_R15(ptreg)       ((ptreg).gpr[15])
#define PTREG_R16(ptreg)       ((ptreg).gpr[16])
#define PTREG_R17(ptreg)       ((ptreg).gpr[17])
#define PTREG_R18(ptreg)       ((ptreg).gpr[18])
#define PTREG_R19(ptreg)       ((ptreg).gpr[19])
#define PTREG_R20(ptreg)       ((ptreg).gpr[20])
#define PTREG_R21(ptreg)       ((ptreg).gpr[21])
#define PTREG_R22(ptreg)       ((ptreg).gpr[22])
#define PTREG_R23(ptreg)       ((ptreg).gpr[23])
#define PTREG_R24(ptreg)       ((ptreg).gpr[24])
#define PTREG_R25(ptreg)       ((ptreg).gpr[25])
#define PTREG_R26(ptreg)       ((ptreg).gpr[26])
#define PTREG_R27(ptreg)       ((ptreg).gpr[27])
#define PTREG_R28(ptreg)       ((ptreg).gpr[28])
#define PTREG_R29(ptreg)       ((ptreg).gpr[29])
#define PTREG_R30(ptreg)       ((ptreg).gpr[30])
#define PTREG_R31(ptreg)       ((ptreg).gpr[31])
#define PTREG_Nip(ptreg)       ((ptreg).nip)
#define PTREG_Msr(ptreg)       ((ptreg).msr)
#define PTREG_Ctr(ptreg)       ((ptreg).ctr)
#define PTREG_Link(ptreg)      ((ptreg).link)
#define PTREG_Xer(ptreg)       ((ptreg).xer)
#define PTREG_Ccr(ptreg)       ((ptreg).ccr)
#else //HOST_POWERPC64

#define PTREG_Rbx(ptreg)    ((ptreg).rbx)
#define PTREG_Rcx(ptreg)    ((ptreg).rcx)
#define PTREG_Rdx(ptreg)    ((ptreg).rdx)
#define PTREG_Rsi(ptreg)    ((ptreg).rsi)
#define PTREG_Rdi(ptreg)    ((ptreg).rdi)
#define PTREG_Rbp(ptreg)    ((ptreg).rbp)
#define PTREG_Rax(ptreg)    ((ptreg).rax)
#define PTREG_Rip(ptreg)    ((ptreg).rip)
#define PTREG_SegCs(ptreg)  ((ptreg).cs)
#define PTREG_SegSs(ptreg)  ((ptreg).ss)
#define PTREG_Rsp(ptreg)    ((ptreg).rsp)
#define PTREG_R8(ptreg)     ((ptreg).r8)
#define PTREG_R9(ptreg)     ((ptreg).r9)
#define PTREG_R10(ptreg)    ((ptreg).r10)
#define PTREG_R11(ptreg)    ((ptreg).r11)
#define PTREG_R12(ptreg)    ((ptreg).r12)
#define PTREG_R13(ptreg)    ((ptreg).r13)
#define PTREG_R14(ptreg)    ((ptreg).r14)
#define PTREG_R15(ptreg)    ((ptreg).r15)

#endif //HOST_POWERPC64
#else // HOST_64BIT

#if defined(HOST_ARM)
#define PTREG_R0(ptreg)        ((ptreg).uregs[0])
#define PTREG_R1(ptreg)        ((ptreg).uregs[1])
#define PTREG_R2(ptreg)        ((ptreg).uregs[2])
#define PTREG_R3(ptreg)        ((ptreg).uregs[3])
#define PTREG_R4(ptreg)        ((ptreg).uregs[4])
#define PTREG_R5(ptreg)        ((ptreg).uregs[5])
#define PTREG_R6(ptreg)        ((ptreg).uregs[6])
#define PTREG_R7(ptreg)        ((ptreg).uregs[7])
#define PTREG_R8(ptreg)        ((ptreg).uregs[8])
#define PTREG_R9(ptreg)        ((ptreg).uregs[9])
#define PTREG_R10(ptreg)       ((ptreg).uregs[10])
#define PTREG_R11(ptreg)       ((ptreg).uregs[11])
#define PTREG_R12(ptreg)       ((ptreg).uregs[12])
#define PTREG_Sp(ptreg)        ((ptreg).uregs[13])
#define PTREG_Lr(ptreg)        ((ptreg).uregs[14])
#define PTREG_Pc(ptreg)        ((ptreg).uregs[15])
#define PTREG_Cpsr(ptreg)      ((ptreg).uregs[16])
#else
#error "Unsupported arch"
#endif

#endif // HOST_64BIT

#define PTREG_EFlags(ptreg) ((ptreg).eflags)

#endif // HAVE_PT_REGS

#if HAVE_BSD_REGS_T

#ifndef BSD_REGS_STYLE
#error "struct reg" has unrecognized format
#endif

#ifdef HOST_64BIT

#ifdef HOST_AMD64

#define BSDREG_Rbx(reg)     BSD_REGS_STYLE(reg,RBX,rbx)
#define BSDREG_Rcx(reg)     BSD_REGS_STYLE(reg,RCX,rcx)
#define BSDREG_Rdx(reg)     BSD_REGS_STYLE(reg,RDX,rdx)
#define BSDREG_Rsi(reg)     BSD_REGS_STYLE(reg,RSI,rsi)
#define BSDREG_Rdi(reg)     BSD_REGS_STYLE(reg,RDI,rdi)
#define BSDREG_Rbp(reg)     BSD_REGS_STYLE(reg,RBP,rbp)
#define BSDREG_Rax(reg)     BSD_REGS_STYLE(reg,RAX,rax)
#define BSDREG_Rip(reg)     BSD_REGS_STYLE(reg,RIP,rip)
#define BSDREG_SegCs(reg)   BSD_REGS_STYLE(reg,CS,cs)
#define BSDREG_SegSs(reg)   BSD_REGS_STYLE(reg,SS,ss)
#define BSDREG_Rsp(reg)     BSD_REGS_STYLE(reg,RSP,rsp)
#define BSDREG_R8(reg)      BSD_REGS_STYLE(reg,R8,r8)
#define BSDREG_R9(reg)      BSD_REGS_STYLE(reg,R9,r9)
#define BSDREG_R10(reg)     BSD_REGS_STYLE(reg,R10,r10)
#define BSDREG_R11(reg)     BSD_REGS_STYLE(reg,R11,r11)
#define BSDREG_R12(reg)     BSD_REGS_STYLE(reg,R12,r12)
#define BSDREG_R13(reg)     BSD_REGS_STYLE(reg,R13,r13)
#define BSDREG_R14(reg)     BSD_REGS_STYLE(reg,R14,r14)
#define BSDREG_R15(reg)     BSD_REGS_STYLE(reg,R15,r15)
#define BSDREG_EFlags(reg)  BSD_REGS_STYLE(reg,RFLAGS,rflags)

#elif defined(HOST_ARM64)

#define BSDREG_X0(reg) BSD_REGS_STYLE(reg,X[0],x[0])
#define BSDREG_X1(reg) BSD_REGS_STYLE(reg,X[1],x[1])
#define BSDREG_X2(reg) BSD_REGS_STYLE(reg,X[2],x[2])
#define BSDREG_X3(reg) BSD_REGS_STYLE(reg,X[3],x[3])
#define BSDREG_X4(reg) BSD_REGS_STYLE(reg,X[4],x[4])
#define BSDREG_X5(reg) BSD_REGS_STYLE(reg,X[5],x[5])
#define BSDREG_X6(reg) BSD_REGS_STYLE(reg,X[6],x[6])
#define BSDREG_X7(reg) BSD_REGS_STYLE(reg,X[7],x[7])
#define BSDREG_X8(reg) BSD_REGS_STYLE(reg,X[8],x[8])
#define BSDREG_X9(reg) BSD_REGS_STYLE(reg,X[9],x[9])
#define BSDREG_X10(reg) BSD_REGS_STYLE(reg,X[10],x[10])
#define BSDREG_X11(reg) BSD_REGS_STYLE(reg,X[11],x[11])
#define BSDREG_X12(reg) BSD_REGS_STYLE(reg,X[12],x[12])
#define BSDREG_X13(reg) BSD_REGS_STYLE(reg,X[13],x[13])
#define BSDREG_X14(reg) BSD_REGS_STYLE(reg,X[14],x[14])
#define BSDREG_X15(reg) BSD_REGS_STYLE(reg,X[15],x[15])
#define BSDREG_X16(reg) BSD_REGS_STYLE(reg,X[16],x[16])
#define BSDREG_X17(reg) BSD_REGS_STYLE(reg,X[17],x[17])
#define BSDREG_X18(reg) BSD_REGS_STYLE(reg,X[18],x[18])
#define BSDREG_X19(reg) BSD_REGS_STYLE(reg,X[19],x[19])
#define BSDREG_X20(reg) BSD_REGS_STYLE(reg,X[20],x[20])
#define BSDREG_X21(reg) BSD_REGS_STYLE(reg,X[21],x[21])
#define BSDREG_X22(reg) BSD_REGS_STYLE(reg,X[22],x[22])
#define BSDREG_X23(reg) BSD_REGS_STYLE(reg,X[23],x[23])
#define BSDREG_X24(reg) BSD_REGS_STYLE(reg,X[24],x[24])
#define BSDREG_X25(reg) BSD_REGS_STYLE(reg,X[25],x[25])
#define BSDREG_X26(reg) BSD_REGS_STYLE(reg,X[26],x[26])
#define BSDREG_X27(reg) BSD_REGS_STYLE(reg,X[27],x[27])
#define BSDREG_X28(reg) BSD_REGS_STYLE(reg,X[28],x[28])
#define BSDREG_Pc(reg) BSD_REGS_STYLE(reg,Elr,elr)
#define BSDREG_Fp(reg) BSD_REGS_STYLE(reg,X[29],x[29])
#define BSDREG_Sp(reg) BSD_REGS_STYLE(reg,Sp,sp)
#define BSDREG_Lr(reg) BSD_REGS_STYLE(reg,Lr,lr)
#define BSDREG_Cpsr(reg) BSD_REGS_STYLE(reg,Spsr,spsr)

#elif defined(HOST_POWERPC64)

#define BSDREG_R0(reg)      ((reg).fixreg[0])
#define BSDREG_R1(reg)      ((reg).fixreg[1])
#define BSDREG_R2(reg)      ((reg).fixreg[2])
#define BSDREG_R3(reg)      ((reg).fixreg[3])
#define BSDREG_R4(reg)      ((reg).fixreg[4])
#define BSDREG_R5(reg)      ((reg).fixreg[5])
#define BSDREG_R6(reg)      ((reg).fixreg[6])
#define BSDREG_R7(reg)      ((reg).fixreg[7])
#define BSDREG_R8(reg)      ((reg).fixreg[8])
#define BSDREG_R9(reg)      ((reg).fixreg[9])
#define BSDREG_R10(reg)     ((reg).fixreg[10])
#define BSDREG_R11(reg)     ((reg).fixreg[11])
#define BSDREG_R12(reg)     ((reg).fixreg[12])
#define BSDREG_R13(reg)     ((reg).fixreg[13])
#define BSDREG_R14(reg)     ((reg).fixreg[14])
#define BSDREG_R15(reg)     ((reg).fixreg[15])
#define BSDREG_R16(reg)     ((reg).fixreg[16])
#define BSDREG_R17(reg)     ((reg).fixreg[17])
#define BSDREG_R18(reg)     ((reg).fixreg[18])
#define BSDREG_R19(reg)     ((reg).fixreg[19])
#define BSDREG_R20(reg)     ((reg).fixreg[20])
#define BSDREG_R21(reg)     ((reg).fixreg[21])
#define BSDREG_R22(reg)     ((reg).fixreg[22])
#define BSDREG_R23(reg)     ((reg).fixreg[23])
#define BSDREG_R24(reg)     ((reg).fixreg[24])
#define BSDREG_R25(reg)     ((reg).fixreg[25])
#define BSDREG_R26(reg)     ((reg).fixreg[26])
#define BSDREG_R27(reg)     ((reg).fixreg[27])
#define BSDREG_R28(reg)     ((reg).fixreg[28])
#define BSDREG_R29(reg)     ((reg).fixreg[29])
#define BSDREG_R30(reg)     ((reg).fixreg[30])
#define BSDREG_R31(reg)     ((reg).fixreg[31])
#define BSDREG_Nip(reg)     ((reg).pc)
/* FreeBSD's ptrace `struct reg` has no MSR slot, and MSR can be neither read
   nor set through ptrace. ASSIGN_CONTROL_REGS copies Msr in both directions,
   so route it through a sink that discards writes and reads back 0: MSR is not
   consumed on this path, and this keeps no per-TU storage or stale state. */
struct PAL_BSDppcMsrSink
{
    void operator=(unsigned long) const { }
    operator unsigned long() const { return 0; }
};
#define BSDREG_Msr(reg)     (PAL_BSDppcMsrSink{})
#define BSDREG_Ctr(reg)     ((reg).ctr)
#define BSDREG_Link(reg)    ((reg).lr)
#define BSDREG_Xer(reg)     ((reg).xer)
#define BSDREG_Ccr(reg)     ((reg).cr)

#endif // HOST_ARM64

#else // HOST_64BIT

#define BSDREG_Ebx(reg)     BSD_REGS_STYLE(reg,EBX,ebx)
#define BSDREG_Ecx(reg)     BSD_REGS_STYLE(reg,ECX,ecx)
#define BSDREG_Edx(reg)     BSD_REGS_STYLE(reg,EDX,edx)
#define BSDREG_Esi(reg)     BSD_REGS_STYLE(reg,ESI,esi)
#define BSDREG_Edi(reg)     BSD_REGS_STYLE(reg,EDI,edi)
#define BSDREG_Ebp(reg)     BSD_REGS_STYLE(reg,EDP,ebp)
#define BSDREG_Eax(reg)     BSD_REGS_STYLE(reg,EAX,eax)
#define BSDREG_Eip(reg)     BSD_REGS_STYLE(reg,EIP,eip)
#define BSDREG_SegCs(reg)   BSD_REGS_STYLE(reg,CS,cs)
#define BSDREG_EFlags(reg)  BSD_REGS_STYLE(reg,EFLAGS,eflags)
#define BSDREG_Esp(reg)     BSD_REGS_STYLE(reg,ESP,esp)
#define BSDREG_SegSs(reg)   BSD_REGS_STYLE(reg,SS,ss)

#endif // HOST_64BIT

#endif // HAVE_BSD_REGS_T

inline static DWORD64 CONTEXTGetPC(LPCONTEXT pContext)
{
#if defined(HOST_AMD64)
    return pContext->Rip;
#elif defined(HOST_S390X)
    return pContext->PSWAddr;
#elif defined(HOST_POWERPC64)
    return pContext->Nip;
#elif defined(HOST_WASM)
    return pContext->InterpreterIP;
#else
    return pContext->Pc;
#endif
}

inline static void CONTEXTSetPC(LPCONTEXT pContext, DWORD64 pc)
{
#if defined(HOST_AMD64)
    pContext->Rip = pc;
#elif defined(HOST_S390X)
    pContext->PSWAddr = pc;
#elif defined(HOST_POWERPC64)
    pContext->Nip = pc;
#elif defined(HOST_WASM)
    pContext->InterpreterIP = pc;
#else
    pContext->Pc = pc;
#endif
}

inline static DWORD64 CONTEXTGetFP(LPCONTEXT pContext)
{
#if defined(HOST_AMD64)
    return pContext->Rbp;
#elif defined(HOST_ARM)
    return pContext->R7;
#elif defined(HOST_S390X)
    return pContext->R11;
#elif defined(HOST_POWERPC64)
    return pContext->R31;
#elif defined(HOST_WASM)
    return pContext->InterpreterFP;
#else
    return pContext->Fp;
#endif
}

/*++
Function :
    CONTEXT_CaptureContext

    Captures the context of the caller.
    The returned context is suitable for performing
    a virtual unwind.

Parameters :
    LPCONTEXT lpContext : new context

--*/
void
PALAPI
CONTEXT_CaptureContext(
    LPCONTEXT lpContext
    );

/*++
Function :
    CONTEXT_SetThreadContext

    Processor-dependent implementation of SetThreadContext

Parameters :
    HANDLE hThread : thread whose context is to be set
    CONTEXT *lpContext : new context

Return value :
    TRUE on success, FALSE on failure

--*/
BOOL
CONTEXT_SetThreadContext(
    DWORD dwProcessId,
    pthread_t self,
    CONST CONTEXT *lpContext
    );

/*++
Function :
    CONTEXT_GetThreadContext

    Processor-dependent implementation of GetThreadContext

Parameters :
    HANDLE hThread : thread whose context is to retrieved
    LPCONTEXT lpContext  : destination for thread's context

Return value :
    TRUE on success, FALSE on failure

--*/
BOOL
CONTEXT_GetThreadContext(
         DWORD dwProcessId,
         pthread_t self,
         LPCONTEXT lpContext);

#if HAVE_MACH_EXCEPTIONS
/*++
Function:
  CONTEXT_GetThreadContextFromPort

  Helper for GetThreadContext that uses a mach_port
--*/
kern_return_t
CONTEXT_GetThreadContextFromPort(
    mach_port_t Port,
    LPCONTEXT lpContext);

/*++
Function:
  SetThreadContextOnPort

  Helper for CONTEXT_SetThreadContext
--*/
kern_return_t
CONTEXT_SetThreadContextOnPort(
   mach_port_t Port,
   IN CONST CONTEXT *lpContext);

/*++
Function:
  GetThreadContextFromThreadState

  Helper for mach exception support
--*/
void
CONTEXT_GetThreadContextFromThreadState(
    thread_state_flavor_t stateFlavor,
    thread_state_t threadState,
    LPCONTEXT lpContext);

#endif // HAVE_MACH_EXCEPTIONS

/*++
Function :
    CONTEXTToNativeContext

    Converts a CONTEXT record to a native context.

Parameters :
    CONST CONTEXT *lpContext : CONTEXT to convert, including
                               flags that determine which registers are valid in
                               lpContext and which ones to set in native
    native_context_t *native : native context to fill in

Return value :
    None

--*/
void CONTEXTToNativeContext(CONST CONTEXT *lpContext, native_context_t *native);

/*++
Function :
    CONTEXTFromNativeContext

    Converts a native context to a CONTEXT record.

Parameters :
    const native_context_t *native : native context to convert
    LPCONTEXT lpContext : CONTEXT to fill in
    ULONG contextFlags : flags that determine which registers are valid in
                         native and which ones to set in lpContext

Return value :
    None

--*/
void CONTEXTFromNativeContext(const native_context_t *native, LPCONTEXT lpContext,
                              ULONG contextFlags);

#if !HAVE_MACH_EXCEPTIONS

/*++
Function :
    GetNativeContextPC

    Returns the program counter from the native context.

Parameters :
    const native_context_t *context : native context

Return value :
    The program counter from the native context.

--*/
LPVOID GetNativeContextPC(const native_context_t *context);

/*++
Function :
    GetNativeContextSP

    Returns the stack pointer from the native context.

Parameters :
    const native_context_t *native : native context

Return value :
    The stack pointer from the native context.

--*/
LPVOID GetNativeContextSP(const native_context_t *context);

/*++
Function :
    CONTEXTGetExceptionCodeForSignal

    Translates signal and context information to a Win32 exception code.

Parameters :
    const siginfo_t *siginfo : signal information from a signal handler
    const native_context_t *context : context information

Return value :
    The Win32 exception code that corresponds to the signal and context
    information.

--*/
DWORD CONTEXTGetExceptionCodeForSignal(const siginfo_t *siginfo,
                                       const native_context_t *context);

#endif  // HAVE_MACH_EXCEPTIONS else

#ifdef __cplusplus
}
#endif // __cplusplus

#endif  // _PAL_CONTEXT_H_
