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

/* A type to wrap the native context type, which is ucontext_t on some
 * platforms and another type elsewhere. */
#if HAVE_UCONTEXT_T
#include <ucontext.h>

typedef ucontext_t native_context_t;
#else   // HAVE_UCONTEXT_T
#error Native context type is not known on this platform!
#endif  // HAVE_UCONTEXT_T

#if !HAVE_MACH_EXCEPTIONS

#if defined(XSTATE_SUPPORTED) && !HAVE_PUBLIC_XSTATE_STRUCT
namespace asm_sigcontext
{
#include <asm/sigcontext.h>
};
using asm_sigcontext::_fpx_sw_bytes;
using asm_sigcontext::_xstate;
#endif // defined(XSTATE_SUPPORTED) && !HAVE_PUBLIC_XSTATE_STRUCT

#else // !HAVE_MACH_EXCEPTIONS
#include <mach/kern_return.h>
#include <mach/mach_port.h>
#endif // !HAVE_MACH_EXCEPTIONS else

#if defined(XSTATE_SUPPORTED) || (defined(HOST_AMD64) && defined(HAVE_MACH_EXCEPTIONS))
bool Xstate_IsAvx512Supported();
#endif // XSTATE_SUPPORTED || (HOST_AMD64 && HAVE_MACH_EXCEPTIONS)

#ifdef HOST_S390X

#define MCREG_PSWMask(mc)   ((mc).psw.mask)
#define MCREG_PSWAddr(mc)   ((mc).psw.addr)
#define MCREG_R0(mc)        ((mc).gregs[0])
#define MCREG_R1(mc)        ((mc).gregs[1])
#define MCREG_R2(mc)        ((mc).gregs[2])
#define MCREG_R3(mc)        ((mc).gregs[3])
#define MCREG_R4(mc)        ((mc).gregs[4])
#define MCREG_R5(mc)        ((mc).gregs[5])
#define MCREG_R6(mc)        ((mc).gregs[6])
#define MCREG_R7(mc)        ((mc).gregs[7])
#define MCREG_R8(mc)        ((mc).gregs[8])
#define MCREG_R9(mc)        ((mc).gregs[9])
#define MCREG_R10(mc)       ((mc).gregs[10])
#define MCREG_R11(mc)       ((mc).gregs[11])
#define MCREG_R12(mc)       ((mc).gregs[12])
#define MCREG_R13(mc)       ((mc).gregs[13])
#define MCREG_R14(mc)       ((mc).gregs[14])
#define MCREG_R15(mc)       ((mc).gregs[15])

#elif HOST_POWERPC64

#define MCREG_R0(mc)        ((mc).gp_regs[0])
#define MCREG_R1(mc)        ((mc).gp_regs[1])
#define MCREG_R2(mc)        ((mc).gp_regs[2])
#define MCREG_R3(mc)        ((mc).gp_regs[3])
#define MCREG_R4(mc)        ((mc).gp_regs[4])
#define MCREG_R5(mc)        ((mc).gp_regs[5])
#define MCREG_R6(mc)        ((mc).gp_regs[6])
#define MCREG_R7(mc)        ((mc).gp_regs[7])
#define MCREG_R8(mc)        ((mc).gp_regs[8])
#define MCREG_R9(mc)        ((mc).gp_regs[9])
#define MCREG_R10(mc)       ((mc).gp_regs[10])
#define MCREG_R11(mc)       ((mc).gp_regs[11])
#define MCREG_R12(mc)       ((mc).gp_regs[12])
#define MCREG_R13(mc)       ((mc).gp_regs[13])
#define MCREG_R14(mc)       ((mc).gp_regs[14])
#define MCREG_R15(mc)       ((mc).gp_regs[15])
#define MCREG_R16(mc)       ((mc).gp_regs[16])
#define MCREG_R17(mc)       ((mc).gp_regs[17])
#define MCREG_R18(mc)       ((mc).gp_regs[18])
#define MCREG_R19(mc)       ((mc).gp_regs[19])
#define MCREG_R20(mc)       ((mc).gp_regs[20])
#define MCREG_R21(mc)       ((mc).gp_regs[21])
#define MCREG_R22(mc)       ((mc).gp_regs[22])
#define MCREG_R23(mc)       ((mc).gp_regs[23])
#define MCREG_R24(mc)       ((mc).gp_regs[24])
#define MCREG_R25(mc)       ((mc).gp_regs[25])
#define MCREG_R26(mc)       ((mc).gp_regs[26])
#define MCREG_R27(mc)       ((mc).gp_regs[27])
#define MCREG_R28(mc)       ((mc).gp_regs[28])
#define MCREG_R29(mc)       ((mc).gp_regs[29])
#define MCREG_R30(mc)       ((mc).gp_regs[30])
#define MCREG_R31(mc)       ((mc).gp_regs[31])
#define MCREG_Nip(mc)       ((mc).gp_regs[32])
#define MCREG_Msr(mc)       ((mc).gp_regs[33])
#define MCREG_Ctr(mc)       ((mc).gp_regs[35])
#define MCREG_Link(mc)      ((mc).gp_regs[36])
#define MCREG_Xer(mc)       ((mc).gp_regs[37])
#define MCREG_Ccr(mc)       ((mc).gp_regs[38])

#elif HAVE___GREGSET_T

#ifdef HOST_64BIT

#if defined(HOST_LOONGARCH64)

#define MCREG_R0(mc)      ((mc).__gregs[0])
#define MCREG_Ra(mc)      ((mc).__gregs[1])
#define MCREG_Tp(mc)      ((mc).__gregs[2])
#define MCREG_Sp(mc)      ((mc).__gregs[3])
#define MCREG_A0(mc)      ((mc).__gregs[4])
#define MCREG_A1(mc)      ((mc).__gregs[5])
#define MCREG_A2(mc)      ((mc).__gregs[6])
#define MCREG_A3(mc)      ((mc).__gregs[7])
#define MCREG_A4(mc)      ((mc).__gregs[8])
#define MCREG_A5(mc)      ((mc).__gregs[9])
#define MCREG_A6(mc)      ((mc).__gregs[10])
#define MCREG_A7(mc)      ((mc).__gregs[11])
#define MCREG_T0(mc)      ((mc).__gregs[12])
#define MCREG_T1(mc)      ((mc).__gregs[13])
#define MCREG_T2(mc)      ((mc).__gregs[14])
#define MCREG_T3(mc)      ((mc).__gregs[15])
#define MCREG_T4(mc)      ((mc).__gregs[16])
#define MCREG_T5(mc)      ((mc).__gregs[17])
#define MCREG_T6(mc)      ((mc).__gregs[18])
#define MCREG_T7(mc)      ((mc).__gregs[19])
#define MCREG_T8(mc)      ((mc).__gregs[20])
#define MCREG_X0(mc)      ((mc).__gregs[21])
#define MCREG_Fp(mc)      ((mc).__gregs[22])
#define MCREG_S0(mc)      ((mc).__gregs[23])
#define MCREG_S1(mc)      ((mc).__gregs[24])
#define MCREG_S2(mc)      ((mc).__gregs[25])
#define MCREG_S3(mc)      ((mc).__gregs[26])
#define MCREG_S4(mc)      ((mc).__gregs[27])
#define MCREG_S5(mc)      ((mc).__gregs[28])
#define MCREG_S6(mc)      ((mc).__gregs[29])
#define MCREG_S7(mc)      ((mc).__gregs[30])
#define MCREG_S8(mc)      ((mc).__gregs[31])
#define MCREG_Pc(mc)      ((mc).__pc)

#elif defined(HOST_RISCV64)

#define MCREG_Ra(mc)      ((mc).__gregs[1])
#define MCREG_Sp(mc)      ((mc).__gregs[2])
#define MCREG_Gp(mc)      ((mc).__gregs[3])
#define MCREG_Tp(mc)      ((mc).__gregs[4])
#define MCREG_T0(mc)      ((mc).__gregs[5])
#define MCREG_T1(mc)      ((mc).__gregs[6])
#define MCREG_T2(mc)      ((mc).__gregs[7])
#define MCREG_Fp(mc)      ((mc).__gregs[8])
#define MCREG_S1(mc)      ((mc).__gregs[9])
#define MCREG_A0(mc)      ((mc).__gregs[10])
#define MCREG_A1(mc)      ((mc).__gregs[11])
#define MCREG_A2(mc)      ((mc).__gregs[12])
#define MCREG_A3(mc)      ((mc).__gregs[13])
#define MCREG_A4(mc)      ((mc).__gregs[14])
#define MCREG_A5(mc)      ((mc).__gregs[15])
#define MCREG_A6(mc)      ((mc).__gregs[16])
#define MCREG_A7(mc)      ((mc).__gregs[17])
#define MCREG_S2(mc)      ((mc).__gregs[18])
#define MCREG_S3(mc)      ((mc).__gregs[19])
#define MCREG_S4(mc)      ((mc).__gregs[20])
#define MCREG_S5(mc)      ((mc).__gregs[21])
#define MCREG_S6(mc)      ((mc).__gregs[22])
#define MCREG_S7(mc)      ((mc).__gregs[23])
#define MCREG_S8(mc)      ((mc).__gregs[24])
#define MCREG_S9(mc)      ((mc).__gregs[25])
#define MCREG_S10(mc)     ((mc).__gregs[26])
#define MCREG_S11(mc)     ((mc).__gregs[27])
#define MCREG_T3(mc)      ((mc).__gregs[28])
#define MCREG_T4(mc)      ((mc).__gregs[29])
#define MCREG_T5(mc)      ((mc).__gregs[30])
#define MCREG_T6(mc)      ((mc).__gregs[31])
#define MCREG_Pc(mc)      ((mc).__gregs[0])

#else // !HOST_LOONGARCH64 && !HOST_RISCV64

#define MCREG_Rbx(mc)       ((mc).__gregs[_REG_RBX])
#define MCREG_Rcx(mc)       ((mc).__gregs[_REG_RCX])
#define MCREG_Rdx(mc)       ((mc).__gregs[_REG_RDX])
#define MCREG_Rsi(mc)       ((mc).__gregs[_REG_RSI])
#define MCREG_Rdi(mc)       ((mc).__gregs[_REG_RDI])
#define MCREG_Rbp(mc)       ((mc).__gregs[_REG_RBP])
#define MCREG_Rax(mc)       ((mc).__gregs[_REG_RAX])
#define MCREG_Rip(mc)       ((mc).__gregs[_REG_RIP])
#define MCREG_Rsp(mc)       ((mc).__gregs[_REG_RSP])
#define MCREG_SegCs(mc)     ((mc).__gregs[_REG_CS])
#define MCREG_SegSs(mc)     ((mc).__gregs[_REG_SS])
#define MCREG_R8(mc)        ((mc).__gregs[_REG_R8])
#define MCREG_R9(mc)        ((mc).__gregs[_REG_R9])
#define MCREG_R10(mc)       ((mc).__gregs[_REG_R10])
#define MCREG_R11(mc)       ((mc).__gregs[_REG_R11])
#define MCREG_R12(mc)       ((mc).__gregs[_REG_R12])
#define MCREG_R13(mc)       ((mc).__gregs[_REG_R13])
#define MCREG_R14(mc)       ((mc).__gregs[_REG_R14])
#define MCREG_R15(mc)       ((mc).__gregs[_REG_R15])
#define MCREG_EFlags(mc)    ((mc).__gregs[_REG_RFLAGS])

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

#endif // HOST_LOONGARCH64

#else // HOST_64BIT

#define MCREG_Ebx(mc)       ((mc).__gregs[_REG_EBX])
#define MCREG_Ecx(mc)       ((mc).__gregs[_REG_ECX])
#define MCREG_Edx(mc)       ((mc).__gregs[_REG_EDX])
#define MCREG_Esi(mc)       ((mc).__gregs[_REG_ESI])
#define MCREG_Edi(mc)       ((mc).__gregs[_REG_EDI])
#define MCREG_Ebp(mc)       ((mc).__gregs[_REG_EBP])
#define MCREG_Eax(mc)       ((mc).__gregs[_REG_EAX])
#define MCREG_Eip(mc)       ((mc).__gregs[_REG_EIP])
#define MCREG_Esp(mc)       ((mc).__gregs[_REG_ESP])
#define MCREG_SegCs(mc)     ((mc).__gregs[_REG_CS])
#define MCREG_SegSs(mc)     ((mc).__gregs[_REG_SS])
#define MCREG_EFlags(mc)    ((mc).__gregs[_REG_RFLAGS])

#endif // HOST_64BIT

#elif HAVE_GREGSET_T

#ifdef HOST_64BIT

#if defined(HOST_LOONGARCH64)

#define MCREG_R0(mc)      ((mc).gregs[0])
#define MCREG_Ra(mc)      ((mc).gregs[1])
#define MCREG_Tp(mc)      ((mc).gregs[2])
#define MCREG_Sp(mc)      ((mc).gregs[3])
#define MCREG_A0(mc)      ((mc).gregs[4])
#define MCREG_A1(mc)      ((mc).gregs[5])
#define MCREG_A2(mc)      ((mc).gregs[6])
#define MCREG_A3(mc)      ((mc).gregs[7])
#define MCREG_A4(mc)      ((mc).gregs[8])
#define MCREG_A5(mc)      ((mc).gregs[9])
#define MCREG_A6(mc)      ((mc).gregs[10])
#define MCREG_A7(mc)      ((mc).gregs[11])
#define MCREG_T0(mc)      ((mc).gregs[12])
#define MCREG_T1(mc)      ((mc).gregs[13])
#define MCREG_T2(mc)      ((mc).gregs[14])
#define MCREG_T3(mc)      ((mc).gregs[15])
#define MCREG_T4(mc)      ((mc).gregs[16])
#define MCREG_T5(mc)      ((mc).gregs[17])
#define MCREG_T6(mc)      ((mc).gregs[18])
#define MCREG_T7(mc)      ((mc).gregs[19])
#define MCREG_T8(mc)      ((mc).gregs[20])
#define MCREG_X0(mc)      ((mc).gregs[21])
#define MCREG_Fp(mc)      ((mc).gregs[22])
#define MCREG_S0(mc)      ((mc).gregs[23])
#define MCREG_S1(mc)      ((mc).gregs[24])
#define MCREG_S2(mc)      ((mc).gregs[25])
#define MCREG_S3(mc)      ((mc).gregs[26])
#define MCREG_S4(mc)      ((mc).gregs[27])
#define MCREG_S5(mc)      ((mc).gregs[28])
#define MCREG_S6(mc)      ((mc).gregs[29])
#define MCREG_S7(mc)      ((mc).gregs[30])
#define MCREG_S8(mc)      ((mc).gregs[31])
#define MCREG_Pc(mc)      ((mc).pc)

#endif // HOST_LOONGARCH64

#define MCREG_Rbx(mc)       ((mc).gregs[REG_RBX])
#define MCREG_Rcx(mc)       ((mc).gregs[REG_RCX])
#define MCREG_Rdx(mc)       ((mc).gregs[REG_RDX])
#define MCREG_Rsi(mc)       ((mc).gregs[REG_RSI])
#define MCREG_Rdi(mc)       ((mc).gregs[REG_RDI])
#define MCREG_Rbp(mc)       ((mc).gregs[REG_RBP])
#define MCREG_Rax(mc)       ((mc).gregs[REG_RAX])
#define MCREG_Rip(mc)       ((mc).gregs[REG_RIP])
#define MCREG_Rsp(mc)       ((mc).gregs[REG_RSP])
#ifdef REG_CSGSFS
#define MCREG_SegCs(mc)     (*(WORD*)&((mc).gregs[REG_CSGSFS]))
#else
#define MCREG_SegCs(mc)     (*(WORD*)&((mc).gregs[REG_CS]))
#endif
#define MCREG_R8(mc)        ((mc).gregs[REG_R8])
#define MCREG_R9(mc)        ((mc).gregs[REG_R9])
#define MCREG_R10(mc)       ((mc).gregs[REG_R10])
#define MCREG_R11(mc)       ((mc).gregs[REG_R11])
#define MCREG_R12(mc)       ((mc).gregs[REG_R12])
#define MCREG_R13(mc)       ((mc).gregs[REG_R13])
#define MCREG_R14(mc)       ((mc).gregs[REG_R14])
#define MCREG_R15(mc)       ((mc).gregs[REG_R15])

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

#ifdef XSTATE_SUPPORTED

#if HAVE_FPSTATE_GLIBC_RESERVED1
#define FPSTATE_RESERVED __glibc_reserved1
#else
#define FPSTATE_RESERVED padding
#endif

// Presence for various extended state registers is detected via the xfeatures (formerly xstate_bv) field. On some
// Linux distros, this definition is only in internal headers, so we define it here. The masks are extracted from
// the processor xstate bit vector register, so the value is OS independent.

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

#if HAVE__FPX_SW_BYTES_WITH_XSTATE_BV
#define FPREG_FpxSwBytes_xfeatures(uc) FPREG_FpxSwBytes(uc)->xstate_bv
#else
#define FPREG_FpxSwBytes_xfeatures(uc) FPREG_FpxSwBytes(uc)->xfeatures
#endif

// The internal _xstate struct is exposed as fpstate, xstate_hdr, ymmh. However, in reality this is
// fpstate, xstate_hdr, extended_state_area and "technically" we are supposed to be determining the
// offset and size of each XFEATURE_MASK_* via CPUID. The extended region always starts at offset
// 576 which is the same as the address of ymmh

#define FPREG_Xstate_ExtendedStateArea_Offset offsetof(_xstate, ymmh)
#define FPREG_Xstate_ExtendedStateArea(uc) (reinterpret_cast<uint8_t *>(FPREG_Fpstate(uc)) + \
                                            FPREG_Xstate_ExtendedStateArea_Offset)

struct Xstate_ExtendedFeature
{
    bool     initialized;
    uint32_t offset;
    uint32_t size;
};

#define Xstate_ExtendedFeatures_Count (XSTATE_AVX512_ZMM + 1)
extern Xstate_ExtendedFeature Xstate_ExtendedFeatures[Xstate_ExtendedFeatures_Count];

inline _fpx_sw_bytes *FPREG_FpxSwBytes(const ucontext_t *uc)
{
    // Bytes 464..511 in the FXSAVE format are available for software to use for any purpose. In this case, they are used to
    // indicate information about extended state.
    _ASSERTE(reinterpret_cast<UINT8 *>(&FPREG_Fpstate(uc)->FPSTATE_RESERVED[12]) - reinterpret_cast<UINT8 *>(FPREG_Fpstate(uc)) == 464);

    _ASSERTE(FPREG_Fpstate(uc) != nullptr);

    return reinterpret_cast<_fpx_sw_bytes *>(&FPREG_Fpstate(uc)->FPSTATE_RESERVED[12]);
}

inline UINT32 FPREG_ExtendedSize(const ucontext_t *uc)
{
    _ASSERTE(FPREG_FpxSwBytes(uc)->magic1 == FP_XSTATE_MAGIC1);
    return FPREG_FpxSwBytes(uc)->extended_size;
}

inline bool FPREG_HasExtendedState(const ucontext_t *uc)
{
    // See comments in /usr/include/x86_64-linux-gnu/asm/sigcontext.h for info on how to detect if extended state is present
    static_assert_no_msg(FP_XSTATE_MAGIC2_SIZE == sizeof(UINT32));

    if (FPREG_FpxSwBytes(uc)->magic1 != FP_XSTATE_MAGIC1)
    {
        return false;
    }

    UINT32 extendedSize = FPREG_ExtendedSize(uc);
    if (extendedSize < sizeof(_xstate))
    {
        return false;
    }

    _ASSERTE(extendedSize >= FP_XSTATE_MAGIC2_SIZE);
    if (*reinterpret_cast<UINT32 *>(reinterpret_cast<UINT8 *>(FPREG_Fpstate(uc)) + (extendedSize - FP_XSTATE_MAGIC2_SIZE))
        != FP_XSTATE_MAGIC2)
    {
        return false;
    }

    return true;
}

inline bool FPREG_HasYmmRegisters(const ucontext_t *uc)
{
    if (!FPREG_HasExtendedState(uc))
    {
        return false;
    }

    return (FPREG_FpxSwBytes_xfeatures(uc) & XFEATURE_MASK_YMM) == XFEATURE_MASK_YMM;
}

inline void *FPREG_Xstate_ExtendedFeature(const ucontext_t *uc, uint32_t *featureSize, uint32_t featureIndex)
{
    _ASSERTE(featureSize != nullptr);
    _ASSERTE(featureIndex < (sizeof(Xstate_ExtendedFeatures) / sizeof(Xstate_ExtendedFeature)));
    _ASSERT(FPREG_Xstate_ExtendedStateArea_Offset == 576);

    Xstate_ExtendedFeature *extendedFeature = &Xstate_ExtendedFeatures[featureIndex];

    if (!extendedFeature->initialized)
    {
        int cpuidInfo[4];

        const int CPUID_EAX = 0;
        const int CPUID_EBX = 1;
        const int CPUID_ECX = 2;
        const int CPUID_EDX = 3;

#ifdef _DEBUG
        // We should only be calling this function if we know the extended feature exists

        __cpuid(cpuidInfo, 0x00000000);
        _ASSERTE(static_cast<uint32_t>(cpuidInfo[CPUID_EAX]) >= 0x0D);

        __cpuidex(cpuidInfo, 0x0000000D, 0x00000000);
        _ASSERTE((cpuidInfo[CPUID_EAX] & (1 << featureIndex)) != 0);
#endif // _DEBUG

        __cpuidex(cpuidInfo, 0x0000000D, static_cast<int32_t>(featureIndex));

        _ASSERTE(static_cast<uint32_t>(cpuidInfo[CPUID_EAX]) > 0);
        _ASSERTE(static_cast<uint32_t>(cpuidInfo[CPUID_EBX]) >= FPREG_Xstate_ExtendedStateArea_Offset);

        extendedFeature->size   = static_cast<uint32_t>(cpuidInfo[CPUID_EAX]);
        extendedFeature->offset = static_cast<uint32_t>(cpuidInfo[CPUID_EBX] - FPREG_Xstate_ExtendedStateArea_Offset);

        extendedFeature->initialized = true;
    }

    *featureSize = extendedFeature->size;
    return (FPREG_Xstate_ExtendedStateArea(uc) + extendedFeature->offset);
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
#endif // XSTATE_SUPPORTED

/////////////////////

#else // HOST_64BIT

#define MCREG_Ebx(mc)       ((mc).gregs[REG_EBX])
#define MCREG_Ecx(mc)       ((mc).gregs[REG_ECX])
#define MCREG_Edx(mc)       ((mc).gregs[REG_EDX])
#define MCREG_Esi(mc)       ((mc).gregs[REG_ESI])
#define MCREG_Edi(mc)       ((mc).gregs[REG_EDI])
#define MCREG_Ebp(mc)       ((mc).gregs[REG_EBP])
#define MCREG_Eax(mc)       ((mc).gregs[REG_EAX])
#define MCREG_Eip(mc)       ((mc).gregs[REG_EIP])
#define MCREG_Esp(mc)       ((mc).gregs[REG_ESP])
#define MCREG_SegCs(mc)     ((mc).gregs[REG_CS])
#define MCREG_SegSs(mc)     ((mc).gregs[REG_SS])

#endif // HOST_64BIT

#ifdef REG_EFL
#define MCREG_EFlags(mc)    ((mc).gregs[REG_EFL])
#else
#define MCREG_EFlags(mc)    ((mc).gregs[EFL])
#endif

#else // HAVE_GREGSET_T

#ifdef HOST_64BIT

#if defined(HOST_ARM64)

#if defined(TARGET_FREEBSD)

#define MCREG_X0(mc)  (mc.mc_gpregs.gp_x[0])
#define MCREG_X1(mc)  (mc.mc_gpregs.gp_x[1])
#define MCREG_X2(mc)  (mc.mc_gpregs.gp_x[2])
#define MCREG_X3(mc)  (mc.mc_gpregs.gp_x[3])
#define MCREG_X4(mc)  (mc.mc_gpregs.gp_x[4])
#define MCREG_X5(mc)  (mc.mc_gpregs.gp_x[5])
#define MCREG_X6(mc)  (mc.mc_gpregs.gp_x[6])
#define MCREG_X7(mc)  (mc.mc_gpregs.gp_x[7])
#define MCREG_X8(mc)  (mc.mc_gpregs.gp_x[8])
#define MCREG_X9(mc)  (mc.mc_gpregs.gp_x[9])
#define MCREG_X10(mc) (mc.mc_gpregs.gp_x[10])
#define MCREG_X11(mc) (mc.mc_gpregs.gp_x[11])
#define MCREG_X12(mc) (mc.mc_gpregs.gp_x[12])
#define MCREG_X13(mc) (mc.mc_gpregs.gp_x[13])
#define MCREG_X14(mc) (mc.mc_gpregs.gp_x[14])
#define MCREG_X15(mc) (mc.mc_gpregs.gp_x[15])
#define MCREG_X16(mc) (mc.mc_gpregs.gp_x[16])
#define MCREG_X17(mc) (mc.mc_gpregs.gp_x[17])
#define MCREG_X18(mc) (mc.mc_gpregs.gp_x[18])
#define MCREG_X19(mc) (mc.mc_gpregs.gp_x[19])
#define MCREG_X20(mc) (mc.mc_gpregs.gp_x[20])
#define MCREG_X21(mc) (mc.mc_gpregs.gp_x[21])
#define MCREG_X22(mc) (mc.mc_gpregs.gp_x[22])
#define MCREG_X23(mc) (mc.mc_gpregs.gp_x[23])
#define MCREG_X24(mc) (mc.mc_gpregs.gp_x[24])
#define MCREG_X25(mc) (mc.mc_gpregs.gp_x[25])
#define MCREG_X26(mc) (mc.mc_gpregs.gp_x[26])
#define MCREG_X27(mc) (mc.mc_gpregs.gp_x[27])
#define MCREG_X28(mc) (mc.mc_gpregs.gp_x[28])

#define MCREG_Cpsr(mc) (mc.mc_gpregs.gp_spsr)
#define MCREG_Lr(mc)   (mc.mc_gpregs.gp_lr)
#define MCREG_Sp(mc)   (mc.mc_gpregs.gp_sp)
#define MCREG_Pc(mc)   (mc.mc_gpregs.gp_elr)
#define MCREG_Fp(mc)   (mc.mc_gpregs.gp_x[29])

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

#elif !defined(TARGET_OSX) // TARGET_FREEBSD

#define MCREG_X0(mc)      ((mc).regs[0])
#define MCREG_X1(mc)      ((mc).regs[1])
#define MCREG_X2(mc)      ((mc).regs[2])
#define MCREG_X3(mc)      ((mc).regs[3])
#define MCREG_X4(mc)      ((mc).regs[4])
#define MCREG_X5(mc)      ((mc).regs[5])
#define MCREG_X6(mc)      ((mc).regs[6])
#define MCREG_X7(mc)      ((mc).regs[7])
#define MCREG_X8(mc)      ((mc).regs[8])
#define MCREG_X9(mc)      ((mc).regs[9])
#define MCREG_X10(mc)     ((mc).regs[10])
#define MCREG_X11(mc)     ((mc).regs[11])
#define MCREG_X12(mc)     ((mc).regs[12])
#define MCREG_X13(mc)     ((mc).regs[13])
#define MCREG_X14(mc)     ((mc).regs[14])
#define MCREG_X15(mc)     ((mc).regs[15])
#define MCREG_X16(mc)     ((mc).regs[16])
#define MCREG_X17(mc)     ((mc).regs[17])
#define MCREG_X18(mc)     ((mc).regs[18])
#define MCREG_X19(mc)     ((mc).regs[19])
#define MCREG_X20(mc)     ((mc).regs[20])
#define MCREG_X21(mc)     ((mc).regs[21])
#define MCREG_X22(mc)     ((mc).regs[22])
#define MCREG_X23(mc)     ((mc).regs[23])
#define MCREG_X24(mc)     ((mc).regs[24])
#define MCREG_X25(mc)     ((mc).regs[25])
#define MCREG_X26(mc)     ((mc).regs[26])
#define MCREG_X27(mc)     ((mc).regs[27])
#define MCREG_X28(mc)     ((mc).regs[28])
#define MCREG_Fp(mc)      ((mc).regs[29])
#define MCREG_Lr(mc)      ((mc).regs[30])

#define MCREG_Sp(mc)      ((mc).sp)
#define MCREG_Pc(mc)      ((mc).pc)
#define MCREG_Cpsr(mc)    ((mc).pstate)


inline
fpsimd_context* GetNativeSigSimdContext(native_context_t *mc)
{
    size_t size = 0;

    do
    {
        fpsimd_context* fp = reinterpret_cast<fpsimd_context *>(&mc->uc_mcontext.__reserved[size]);

        if(fp->head.magic == FPSIMD_MAGIC)
        {
            _ASSERTE(fp->head.size >= sizeof(fpsimd_context));
            _ASSERTE(size + fp->head.size <= sizeof(mc->uc_mcontext.__reserved));

            return fp;
        }

        if (fp->head.size == 0)
        {
            break;
        }

        size += fp->head.size;
    } while (size + sizeof(fpsimd_context) <= sizeof(mc->uc_mcontext.__reserved));

    _ASSERTE(false);

    return nullptr;
}

inline
const fpsimd_context* GetConstNativeSigSimdContext(const native_context_t *mc)
{
    return GetNativeSigSimdContext(const_cast<native_context_t*>(mc));
}

#else // TARGET_OSX

#define MCREG_X0(mc)      ((mc)->__ss.__x[0])
#define MCREG_X1(mc)      ((mc)->__ss.__x[1])
#define MCREG_X2(mc)      ((mc)->__ss.__x[2])
#define MCREG_X3(mc)      ((mc)->__ss.__x[3])
#define MCREG_X4(mc)      ((mc)->__ss.__x[4])
#define MCREG_X5(mc)      ((mc)->__ss.__x[5])
#define MCREG_X6(mc)      ((mc)->__ss.__x[6])
#define MCREG_X7(mc)      ((mc)->__ss.__x[7])
#define MCREG_X8(mc)      ((mc)->__ss.__x[8])
#define MCREG_X9(mc)      ((mc)->__ss.__x[9])
#define MCREG_X10(mc)     ((mc)->__ss.__x[10])
#define MCREG_X11(mc)     ((mc)->__ss.__x[11])
#define MCREG_X12(mc)     ((mc)->__ss.__x[12])
#define MCREG_X13(mc)     ((mc)->__ss.__x[13])
#define MCREG_X14(mc)     ((mc)->__ss.__x[14])
#define MCREG_X15(mc)     ((mc)->__ss.__x[15])
#define MCREG_X16(mc)     ((mc)->__ss.__x[16])
#define MCREG_X17(mc)     ((mc)->__ss.__x[17])
#define MCREG_X18(mc)     ((mc)->__ss.__x[18])
#define MCREG_X19(mc)     ((mc)->__ss.__x[19])
#define MCREG_X20(mc)     ((mc)->__ss.__x[20])
#define MCREG_X21(mc)     ((mc)->__ss.__x[21])
#define MCREG_X22(mc)     ((mc)->__ss.__x[22])
#define MCREG_X23(mc)     ((mc)->__ss.__x[23])
#define MCREG_X24(mc)     ((mc)->__ss.__x[24])
#define MCREG_X25(mc)     ((mc)->__ss.__x[25])
#define MCREG_X26(mc)     ((mc)->__ss.__x[26])
#define MCREG_X27(mc)     ((mc)->__ss.__x[27])
#define MCREG_X28(mc)     ((mc)->__ss.__x[28])
#define MCREG_Fp(mc)      ((mc)->__ss.__fp)
#define MCREG_Lr(mc)      ((mc)->__ss.__lr)

#define MCREG_Sp(mc)      ((mc)->__ss.__sp)
#define MCREG_Pc(mc)      ((mc)->__ss.__pc)
#define MCREG_Cpsr(mc)    ((mc)->__ss.__cpsr)

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

#endif // TARGET_OSX

#elif defined(HOST_LOONGARCH64)

#define MCREG_R0(mc)      ((mc).regs[0])
#define MCREG_Ra(mc)      ((mc).regs[1])
#define MCREG_Tp(mc)      ((mc).regs[2])
#define MCREG_Sp(mc)      ((mc).regs[3])
#define MCREG_A0(mc)      ((mc).regs[4])
#define MCREG_A1(mc)      ((mc).regs[5])
#define MCREG_A2(mc)      ((mc).regs[6])
#define MCREG_A3(mc)      ((mc).regs[7])
#define MCREG_A4(mc)      ((mc).regs[8])
#define MCREG_A5(mc)      ((mc).regs[9])
#define MCREG_A6(mc)      ((mc).regs[10])
#define MCREG_A7(mc)      ((mc).regs[11])
#define MCREG_T0(mc)      ((mc).regs[12])
#define MCREG_T1(mc)      ((mc).regs[13])
#define MCREG_T2(mc)      ((mc).regs[14])
#define MCREG_T3(mc)      ((mc).regs[15])
#define MCREG_T4(mc)      ((mc).regs[16])
#define MCREG_T5(mc)      ((mc).regs[17])
#define MCREG_T6(mc)      ((mc).regs[18])
#define MCREG_T7(mc)      ((mc).regs[19])
#define MCREG_T8(mc)      ((mc).regs[20])
#define MCREG_X0(mc)      ((mc).regs[21])
#define MCREG_Fp(mc)      ((mc).regs[22])
#define MCREG_S0(mc)      ((mc).regs[23])
#define MCREG_S1(mc)      ((mc).regs[24])
#define MCREG_S2(mc)      ((mc).regs[25])
#define MCREG_S3(mc)      ((mc).regs[26])
#define MCREG_S4(mc)      ((mc).regs[27])
#define MCREG_S5(mc)      ((mc).regs[28])
#define MCREG_S6(mc)      ((mc).regs[29])
#define MCREG_S7(mc)      ((mc).regs[30])
#define MCREG_S8(mc)      ((mc).regs[31])
#define MCREG_Pc(mc)      ((mc).pc)

#else // HOST_ARM64

#ifdef TARGET_OSX

#define MCREG_Rbp(mc)      ((mc)->__ss.__rbp)
#define MCREG_Rip(mc)      ((mc)->__ss.__rip)
#define MCREG_Rsp(mc)      ((mc)->__ss.__rsp)
#define MCREG_Rsi(mc)      ((mc)->__ss.__rsi)
#define MCREG_Rdi(mc)      ((mc)->__ss.__rdi)
#define MCREG_Rbx(mc)      ((mc)->__ss.__rbx)
#define MCREG_Rdx(mc)      ((mc)->__ss.__rdx)
#define MCREG_Rcx(mc)      ((mc)->__ss.__rcx)
#define MCREG_Rax(mc)      ((mc)->__ss.__rax)
#define MCREG_R8(mc)       ((mc)->__ss.__r8)
#define MCREG_R9(mc)       ((mc)->__ss.__r9)
#define MCREG_R10(mc)      ((mc)->__ss.__r10)
#define MCREG_R11(mc)      ((mc)->__ss.__r11)
#define MCREG_R12(mc)      ((mc)->__ss.__r12)
#define MCREG_R13(mc)      ((mc)->__ss.__r13)
#define MCREG_R14(mc)      ((mc)->__ss.__r14)
#define MCREG_R15(mc)      ((mc)->__ss.__r15)
#define MCREG_EFlags(mc)   ((mc)->__ss.__rflags)
#define MCREG_SegCs(mc)    ((mc)->__ss.__cs)

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

static_assert_no_msg(offsetof(_STRUCT_X86_AVX_STATE64, __fpu_ymmh0) == offsetof(_STRUCT_X86_AVX512_STATE64, __fpu_ymmh0));

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
#else //TARGET_OSX

    // For FreeBSD, as found in x86/ucontext.h
#define MCREG_Rbp(mc)	    ((mc).mc_rbp)
#define MCREG_Rip(mc)	    ((mc).mc_rip)
#define MCREG_Rsp(mc)	    ((mc).mc_rsp)
#define MCREG_Rsi(mc)       ((mc).mc_rsi)
#define MCREG_Rdi(mc)	    ((mc).mc_rdi)
#define MCREG_Rbx(mc)	    ((mc).mc_rbx)
#define MCREG_Rdx(mc)	    ((mc).mc_rdx)
#define MCREG_Rcx(mc)	    ((mc).mc_rcx)
#define MCREG_Rax(mc)	    ((mc).mc_rax)
#define MCREG_R8(mc)	    ((mc).mc_r8)
#define MCREG_R9(mc)	    ((mc).mc_r9)
#define MCREG_R10(mc)	    ((mc).mc_r10)
#define MCREG_R11(mc)	    ((mc).mc_r11)
#define MCREG_R12(mc)	    ((mc).mc_r12)
#define MCREG_R13(mc)	    ((mc).mc_r13)
#define MCREG_R14(mc)	    ((mc).mc_r14)
#define MCREG_R15(mc)	    ((mc).mc_r15)
#define MCREG_EFlags(mc)    ((mc).mc_rflags)
#define MCREG_SegCs(mc)     ((mc).mc_cs)

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
#endif // TARGET_OSX
#endif // HOST_ARM64

#else // HOST_64BIT

#if defined(HOST_ARM)

#define MCREG_R0(mc)        ((mc).arm_r0)
#define MCREG_R1(mc)        ((mc).arm_r1)
#define MCREG_R2(mc)        ((mc).arm_r2)
#define MCREG_R3(mc)        ((mc).arm_r3)
#define MCREG_R4(mc)        ((mc).arm_r4)
#define MCREG_R5(mc)        ((mc).arm_r5)
#define MCREG_R6(mc)        ((mc).arm_r6)
#define MCREG_R7(mc)        ((mc).arm_r7)
#define MCREG_R8(mc)        ((mc).arm_r8)
#define MCREG_R9(mc)        ((mc).arm_r9)
#define MCREG_R10(mc)       ((mc).arm_r10)
#define MCREG_R11(mc)       ((mc).arm_fp)
#define MCREG_R12(mc)       ((mc).arm_ip)
#define MCREG_Sp(mc)        ((mc).arm_sp)
#define MCREG_Lr(mc)        ((mc).arm_lr)
#define MCREG_Pc(mc)        ((mc).arm_pc)
#define MCREG_Cpsr(mc)      ((mc).arm_cpsr)


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

#elif defined(HOST_X86)

#define MCREG_Ebx(mc)       ((mc).mc_ebx)
#define MCREG_Ecx(mc)       ((mc).mc_ecx)
#define MCREG_Edx(mc)       ((mc).mc_edx)
#define MCREG_Esi(mc)       ((mc).mc_esi)
#define MCREG_Edi(mc)       ((mc).mc_edi)
#define MCREG_Ebp(mc)       ((mc).mc_ebp)
#define MCREG_Eax(mc)       ((mc).mc_eax)
#define MCREG_Eip(mc)       ((mc).mc_eip)
#define MCREG_SegCs(mc)     ((mc).mc_cs)
#define MCREG_EFlags(mc)    ((mc).mc_eflags)
#define MCREG_Esp(mc)       ((mc).mc_esp)
#define MCREG_SegSs(mc)     ((mc).mc_ss)

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
#elif defined(HOST_X86)
#define PTREG_Ebx(ptreg)    ((ptreg).ebx)
#define PTREG_Ecx(ptreg)    ((ptreg).ecx)
#define PTREG_Edx(ptreg)    ((ptreg).edx)
#define PTREG_Esi(ptreg)    ((ptreg).esi)
#define PTREG_Edi(ptreg)    ((ptreg).edi)
#define PTREG_Ebp(ptreg)    ((ptreg).ebp)
#define PTREG_Eax(ptreg)    ((ptreg).eax)
#define PTREG_Eip(ptreg)    ((ptreg).eip)
#define PTREG_SegCs(ptreg)  ((ptreg).xcs)
#define PTREG_SegSs(ptreg)  ((ptreg).xss)
#define PTREG_Esp(ptreg)    ((ptreg).esp)
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
#elif defined(HOST_X86)
    return pContext->Eip;
#elif defined(HOST_S390X)
    return pContext->PSWAddr;
#elif defined(HOST_POWERPC64)
    return pContext->Nip;
#else
    return pContext->Pc;
#endif
}

inline static void CONTEXTSetPC(LPCONTEXT pContext, DWORD64 pc)
{
#if defined(HOST_AMD64)
    pContext->Rip = pc;
#elif defined(HOST_X86)
    pContext->Eip = pc;
#elif defined(HOST_S390X)
    pContext->PSWAddr = pc;
#elif defined(HOST_POWERPC64)
    pContext->Nip = pc;
#else
    pContext->Pc = pc;
#endif
}

inline static DWORD64 CONTEXTGetFP(LPCONTEXT pContext)
{
#if defined(HOST_AMD64)
    return pContext->Rbp;
#elif defined(HOST_X86)
    return pContext->Ebp;
#elif defined(HOST_ARM)
    return pContext->R7;
#elif defined(HOST_S390X)
    return pContext->R11;
#elif defined(HOST_POWERPC64)
    return pContext->R31;
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
