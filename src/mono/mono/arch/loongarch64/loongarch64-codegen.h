#ifndef __LOONGARCH64_CODEGEN_H__
#define __LOONGARCH64_CODEGEN_H__
/*
 * Copyright (c) 2021 Loongson Technology, Inc
 * Author: Qiao Pengcheng (qiaopengcheng@loongson.cn), Liu An (liuan@loongson.cn)
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

/* registers */
enum {
	loongarch_zero,/* contonst zero */
	loongarch_ra,  /* return values */
	loongarch_tp,  /* TLS */
	loongarch_sp,  /* stack pointer */
	loongarch_a0,  /* 8 - func arguments */
	loongarch_a1,  /* a0,a1 also return registers */
	loongarch_a2,
	loongarch_a3,
	loongarch_a4,
	loongarch_a5,
	loongarch_a6,
	loongarch_a7,
	loongarch_t0,  /* 9 temp registers */
	loongarch_t1,
	loongarch_t2,
	loongarch_t3,
	loongarch_t4,
	loongarch_t5,
	loongarch_t6,
	loongarch_t7,
	loongarch_t8,
	loongarch_r21, /* scratch registers for mono. */
	loongarch_fp,  /* frame pointer */
	loongarch_s0,  /* 9 callee saved registers */
	loongarch_s1,
	loongarch_s2,
	loongarch_s3,
	loongarch_s4,
	loongarch_s5,
	loongarch_s6,
	loongarch_s7,
	loongarch_s8,
};

/* we treat the register file as containing just doubles... */
enum {
	loongarch_f0 = 0, /* 8 float arguments */
	loongarch_f1 = 1, /* f0,f1 also return regs */
	loongarch_f2 = 2,
	loongarch_f3 = 3,
	loongarch_f4 = 4,
	loongarch_f5 = 5,
	loongarch_f6 = 6,
	loongarch_f7 = 7,
	loongarch_ft0 = 8, /* 16 temp registers */
	loongarch_ft1 = 9,
	loongarch_ft2 = 10,
	loongarch_ft3 = 11,
	loongarch_ft4 = 12,
	loongarch_ft5 = 13,
	loongarch_ft6 = 14,
	loongarch_ft7 = 15,
	loongarch_ft8 = 16,
	loongarch_ft9 = 17,
	loongarch_ft10 = 18,
	loongarch_ft11 = 19,
	loongarch_ft12 = 20,
	loongarch_ft13 = 21,
	loongarch_ft14 = 22,
	loongarch_ft15 = 23,
	loongarch_fs0 = 24,  /* 8 callee saved registers */
	loongarch_fs1 = 25,
	loongarch_fs2 = 26,
	loongarch_fs3 = 27,
	loongarch_fs4 = 28,
	loongarch_fs5 = 29,
	loongarch_fs6 = 30,
	loongarch_fs7 = 31,
};

/* fpu enable/cause flags, cc */
enum {
	LOONGARCH_INEXACT = 1,
	LOONGARCH_UNDERFLOW = 2,
	LOONGARCH_OVERFLOW = 4,
	LOONGARCH_DIVZERO = 8,
	LOONGARCH_INVALID = 16,

	LOONGARCH_FPU_ENABLES_OFFSET = 0,
	LOONGARCH_FPU_FLAGS_OFFSET = 20,
	LOONGARCH_FPU_CAUSES_OFFSET = 28
};

/* fpu compare condition encode - see manual entry for FCMP.cond.{S/D} instructions */
#define  LOONGARCH_FPU_CAF      0x0
#define  LOONGARCH_FPU_CUN      0x8
#define  LOONGARCH_FPU_CEQ      0x4
#define  LOONGARCH_FPU_CUEQ     0xc
#define  LOONGARCH_FPU_CLT      0x2
#define  LOONGARCH_FPU_CULT     0xe
#define  LOONGARCH_FPU_CLE      0x6
#define  LOONGARCH_FPU_CULE     0xe
#define  LOONGARCH_FPU_CNE      0x10
#define  LOONGARCH_FPU_COR      0x14
#define  LOONGARCH_FPU_CUNE     0x18
#define  LOONGARCH_FPU_SAF      0x1
#define  LOONGARCH_FPU_SUN      0x9
#define  LOONGARCH_FPU_SEQ      0x5
#define  LOONGARCH_FPU_SUEQ     0xd
#define  LOONGARCH_FPU_SLT      0x3
#define  LOONGARCH_FPU_SULT     0xb
#define  LOONGARCH_FPU_SLE      0x7
#define  LOONGARCH_FPU_SULE     0xf
#define  LOONGARCH_FPU_SNE      0x11
#define  LOONGARCH_FPU_SOR      0x15
#define  LOONGARCH_FPU_SUNE     0x19


#if SIZEOF_REGISTER != 8
#error Unknown SIZEOF_REGISTER
#endif

#define IS_B_SI28_LOONGARCH(dist)  (((dist) >= -(1 << 27)) && ((dist) < (1 << 27)))
#define IS_B_SI23_LOONGARCH(dist)  (((dist) >= -(1 << 22)) && ((dist) < (1 << 22)))
#define IS_B_SI18_LOONGARCH(dist)  (((dist) >= -(1 << 17)) && ((dist) < (1 << 17)))

#define loongarch_is_imm12(val)  (((val) >= -0x800) && ((val) <= 0x7ff))
#define loongarch_is_imm14(val)  (((val) >= -0x2000) && ((val) <= 0x1fff))
#define loongarch_is_imm16(val) ((gint)(gshort)(gint)(val) == (gint)(val))

#define GASSERT_IS_INT32(val) g_assert((gint)(val) == (long)(val))

#define loongarch_load_const(c,D,v) do {	\
		if (!loongarch_is_imm12 ((v))) {	\
			loongarch_lu12iw ((c), (D), ((v) >> 12) & 0xfffff); \
			loongarch_ori ((c), (D), (D), ((guint32)(v)) & 0xfff); \
		}							\
		else							\
			loongarch_addiw ((c), (D), loongarch_zero, (v) & 0xfff); \
	} while (0)

#define loongarch_format_bsi21(pointer,encoding,si21,cc)      do {             \
        if ((si21)>>16) {   \
        *((guint32 *)(pointer)) = (guint32)((encoding) | (((si21)&0xffff)<<10) | ((cc)<<5) | ((si21)>>16));    \
        } else    \
        *((guint32 *)(pointer)) = (guint32)((encoding) | ((si21)<<10) | ((cc)<<5));    \
        (pointer) = (typeof(pointer))(((guint32 *)(pointer)) + 1);             \
	} while (0)
#define loongarch_format_bsi26(pointer,encoding,si26)      do {                  \
        if ((si26)>>16) {   \
        *((guint32 *)(pointer)) = (guint32)((encoding) | (((si26)&0xffff)<<10) | (((si26)>>16)&0x3ff));    \
        } else    \
        *((guint32 *)(pointer)) = (guint32)((encoding) | ((si26)<<10));    \
        (pointer) = (typeof(pointer))(((guint32 *)(pointer)) + 1);             \
	} while (0)
#define loongarch_format_1rsi(pointer,encoding,si,rd)      do {                  \
        *((guint32 *)(pointer)) = (guint32)((encoding) | ((si)<<5) | (rd));    \
        (pointer) = (typeof(pointer))(((guint32 *)(pointer)) + 1);             \
	} while (0)
#define loongarch_format_2r(pointer,encoding,rj,rd)      do {                  \
        *((guint32 *)(pointer)) = (guint32)((encoding) | ((rj)<<5) | (rd));    \
        (pointer) = (typeof(pointer))(((guint32 *)(pointer)) + 1);             \
	} while (0)
#define loongarch_format_2rui(pointer,encoding,ui,rj,rd)      do {                  \
        *((guint32 *)(pointer)) = (guint32)((encoding) | ((ui)<<10) | ((rj)<<5) | (rd));    \
        (pointer) = (typeof(pointer))(((guint32 *)(pointer)) + 1);             \
	} while (0)
#define loongarch_format_2r2ui(pointer,encoding,msbd,lsbd,rj,rd)      do {     \
        *((guint32 *)(pointer)) = (guint32)((encoding) | ((msbd)<<16) | ((lsbd)<<10) | ((rj)<<5) | (rd));\
        (pointer) = (typeof(pointer))(((guint32 *)(pointer)) + 1);             \
	} while (0)
#define loongarch_format_3r(pointer,encoding,rk,rj,rd)      do {               \
        *((guint32 *)(pointer)) = (guint32)((encoding) | ((rk)<<10) | ((rj)<<5) | (rd)); \
        (pointer) = (typeof(pointer))(((guint32 *)(pointer)) + 1);             \
	} while (0)
#define loongarch_format_3rsa(pointer,encoding,sa,rk,rj,rd)      do {          \
        *((guint32 *)(pointer)) = (guint32)((encoding) | ((sa)<<15) | ((rk)<<10) | ((rj)<<5) | (rd)); \
        (pointer) = (typeof(pointer))(((guint32 *)(pointer)) + 1);             \
	} while (0)
#define loongarch_format_code(pointer,encoding,opcode)      do {        \
        *((guint32 *)(pointer)) = (guint32)((encoding) | (opcode));     \
        (pointer) = (typeof(pointer))(((guint32 *)(pointer)) + 1);      \
	} while (0)

/* arithmetric ops */
#define loongarch_addw(c,dest,srcj,srck) loongarch_format_3r(c,0x00100000,srck,srcj,dest)
#define loongarch_addd(c,dest,srcj,srck) loongarch_format_3r(c,0x00108000,srck,srcj,dest)
#define loongarch_subw(c,dest,srcj,srck) loongarch_format_3r(c,0x00110000,srck,srcj,dest)
#define loongarch_subd(c,dest,srcj,srck) loongarch_format_3r(c,0x00118000,srck,srcj,dest)
#define loongarch_alslw(c,dest,srcj,srck, sa2)  loongarch_format_3rsa(c,0x00040000,srck,srcj,dest,sa2)
#define loongarch_alslwu(c,dest,srcj,srck, sa2) loongarch_format_3rsa(c,0x00060000,srck,srcj,dest,sa2)
#define loongarch_alsld(c,dest,srcj,srck, sa2)  loongarch_format_3rsa(c,0x002c0000,srck,srcj,dest,sa2)
#define loongarch_addiw(c,dest,src1,si12)    loongarch_format_2rui(c,0x02800000,(si12 & 0xfff),src1,dest)
#define loongarch_addid(c,dest,src1,si12)    loongarch_format_2rui(c,0x02c00000,(si12 & 0xfff),src1,dest)
#define loongarch_addu16id(c,dest,src1,si16) loongarch_format_2rui(c,0x10000000,si16,src1,dest)

#define loongarch_lu12iw(c,dest,si20) loongarch_format_1rsi(c,0x14000000,si20,dest)
#define loongarch_lu32id(c,dest,si20) loongarch_format_1rsi(c,0x16000000,si20,dest)
#define loongarch_lu52id(c,dest,src1,si12) loongarch_format_2rui(c,0x03000000,si12,src1,dest)

/* relative pc ops */
#define loongarch_pcaddi(c,dest,si20)    loongarch_format_1rsi(c,0x18000000,si20,dest)
#define loongarch_pcaddu12i(c,dest,si20) loongarch_format_1rsi(c,0x1c000000,si20,dest)
#define loongarch_pcaddu18i(c,dest,si20) loongarch_format_1rsi(c,0x1e000000,si20,dest)
#define loongarch_pcalau12i(c,dest,si20) loongarch_format_1rsi(c,0x1a000000,si20,dest)

/* mul ops */
#define loongarch_mulw(c,dest,src1,src2)    loongarch_format_3r(c,0x001c0000,src2,src1,dest)
#define loongarch_mulhw(c,dest,src1,src2)   loongarch_format_3r(c,0x001c8000,src2,src1,dest)
#define loongarch_mulhwu(c,dest,src1,src2)  loongarch_format_3r(c,0x001d0000,src2,src1,dest)
#define loongarch_muld(c,dest,src1,src2)    loongarch_format_3r(c,0x001d8000,src2,src1,dest)
#define loongarch_mulhd(c,dest,src1,src2)   loongarch_format_3r(c,0x001e0000,src2,src1,dest)
#define loongarch_mulhdu(c,dest,src1,src2)  loongarch_format_3r(c,0x001e8000,src2,src1,dest)
#define loongarch_mulwdw(c,dest,src1,src2)  loongarch_format_3r(c,0x001f0000,src2,src1,dest)
#define loongarch_mulwdwu(c,dest,src1,src2) loongarch_format_3r(c,0x001f8000,src2,src1,dest)
/* div and mod ops */
#define loongarch_divw(c,dest,src1,src2)  loongarch_format_3r(c,0x00200000,src2,src1,dest)
#define loongarch_divwu(c,dest,src1,src2) loongarch_format_3r(c,0x00210000,src2,src1,dest)
#define loongarch_modw(c,dest,src1,src2)  loongarch_format_3r(c,0x00208000,src2,src1,dest)
#define loongarch_modwu(c,dest,src1,src2) loongarch_format_3r(c,0x00218000,src2,src1,dest)
#define loongarch_divd(c,dest,src1,src2)  loongarch_format_3r(c,0x00220000,src2,src1,dest)
#define loongarch_divdu(c,dest,src1,src2) loongarch_format_3r(c,0x00230000,src2,src1,dest)
#define loongarch_modd(c,dest,src1,src2)  loongarch_format_3r(c,0x00228000,src2,src1,dest)
#define loongarch_moddu(c,dest,src1,src2) loongarch_format_3r(c,0x00238000,src2,src1,dest)

/* compares */
#define loongarch_slt(c,dest,src1,src2)   loongarch_format_3r(c,0x00120000,src2,src1,dest)
#define loongarch_sltu(c,dest,src1,src2)  loongarch_format_3r(c,0x00128000,src2,src1,dest)
#define loongarch_slti(c,dest,src1,si12)  loongarch_format_2rui(c,0x02000000,si12,src1,dest)
#define loongarch_sltui(c,dest,src1,si12) loongarch_format_2rui(c,0x02400000,si12,src1,dest)

/* shift ops */
#define loongarch_sllw(c,dest,src1,src2)  loongarch_format_3r(c,0x00170000,src2,src1,dest)
#define loongarch_srlw(c,dest,src1,src2)  loongarch_format_3r(c,0x00178000,src2,src1,dest)
#define loongarch_sraw(c,dest,src1,src2)  loongarch_format_3r(c,0x00180000,src2,src1,dest)
#define loongarch_rotrw(c,dest,src1,src2) loongarch_format_3r(c,0x001b0000,src2,src1,dest)
#define loongarch_slld(c,dest,src1,src2)  loongarch_format_3r(c,0x00188000,src2,src1,dest)
#define loongarch_srld(c,dest,src1,src2)  loongarch_format_3r(c,0x00190000,src2,src1,dest)
#define loongarch_srad(c,dest,src1,src2)  loongarch_format_3r(c,0x00198000,src2,src1,dest)
#define loongarch_rotrd(c,dest,src1,src2) loongarch_format_3r(c,0x001b8000,src2,src1,dest)

#define loongarch_slliw(c,dest,src1,ui5)  loongarch_format_2rui(c,0x00408000,ui5,src1,dest)
#define loongarch_srliw(c,dest,src1,ui5)  loongarch_format_2rui(c,0x00448000,ui5,src1,dest)
#define loongarch_sraiw(c,dest,src1,ui5)  loongarch_format_2rui(c,0x00488000,ui5,src1,dest)
#define loongarch_rotriw(c,dest,src1,ui5) loongarch_format_2rui(c,0x004c8000,ui5,src1,dest)
#define loongarch_sllid(c,dest,src1,ui6)  loongarch_format_2rui(c,0x00410000,ui6,src1,dest)
#define loongarch_srlid(c,dest,src1,ui6)  loongarch_format_2rui(c,0x00450000,ui6,src1,dest)
#define loongarch_sraid(c,dest,src1,ui6)  loongarch_format_2rui(c,0x00490000,ui6,src1,dest)
#define loongarch_rotrid(c,dest,src1,ui6) loongarch_format_2rui(c,0x004d0000,ui6,src1,dest)

/* logical ops */
#define loongarch_and(c,dest,src1,src2)  loongarch_format_3r(c,0x00148000,src2,src1,dest)
#define loongarch_andn(c,dest,src1,src2) loongarch_format_3r(c,0x00168000,src2,src1,dest)
#define loongarch_or(c,dest,src1,src2)   loongarch_format_3r(c,0x00150000,src2,src1,dest)
#define loongarch_orn(c,dest,src1,src2)  loongarch_format_3r(c,0x00160000,src2,src1,dest)
#define loongarch_nor(c,dest,src1,src2)  loongarch_format_3r(c,0x00140000,src2,src1,dest)
#define loongarch_xor(c,dest,src1,src2)  loongarch_format_3r(c,0x00158000,src2,src1,dest)

#define loongarch_andi(c,dest,src1,ui12) loongarch_format_2rui(c,0x03400000,ui12,src1,dest)
#define loongarch_ori(c,dest,src1,ui12)  loongarch_format_2rui(c,0x03800000,ui12,src1,dest)
#define loongarch_xori(c,dest,src1,ui12) loongarch_format_2rui(c,0x03c00000,ui12,src1,dest)
#define loongarch_move(c,dest,src1)   loongarch_ori(c,dest,src1,0)

/* bits ops */
#define loongarch_extwb(c,dest,src1) loongarch_format_2r(c,0x00005c00,src1,dest)
#define loongarch_extwh(c,dest,src1) loongarch_format_2r(c,0x00005800,src1,dest)
#define loongarch_clow(c,dest,src1)  loongarch_format_2r(c,0x00001000,src1,dest)
#define loongarch_clzw(c,dest,src1)  loongarch_format_2r(c,0x00001400,src1,dest)
#define loongarch_ctow(c,dest,src1)  loongarch_format_2r(c,0x00001800,src1,dest)
#define loongarch_ctzw(c,dest,src1)  loongarch_format_2r(c,0x00001c00,src1,dest)
#define loongarch_clod(c,dest,src1)  loongarch_format_2r(c,0x00002000,src1,dest)
#define loongarch_clzd(c,dest,src1)  loongarch_format_2r(c,0x00002400,src1,dest)
#define loongarch_ctod(c,dest,src1)  loongarch_format_2r(c,0x00002800,src1,dest)
#define loongarch_ctzd(c,dest,src1)  loongarch_format_2r(c,0x00002c00,src1,dest)

#define loongarch_bytepickw(c,dest,src1,src2,sa2) loongarch_format_3rsa(c,0x00080000,sa2,src2,src1,dest)
#define loongarch_bytepickd(c,dest,src1,src2,sa3) loongarch_format_3rsa(c,0x000c0000,sa3,src2,src1,dest)
#define loongarch_revb2h(c,dest,src1)   loongarch_format_2r(c,0x00003000,src1,dest)
#define loongarch_revb4h(c,dest,src1)   loongarch_format_2r(c,0x00003400,src1,dest)
#define loongarch_revb2w(c,dest,src1)   loongarch_format_2r(c,0x00003800,src1,dest)
#define loongarch_revbd(c,dest,src1)    loongarch_format_2r(c,0x00003c00,src1,dest)
#define loongarch_revh2w(c,dest,src1)   loongarch_format_2r(c,0x00004000,src1,dest)
#define loongarch_revhd(c,dest,src1)    loongarch_format_2r(c,0x00004400,src1,dest)
#define loongarch_bitrev4b(c,dest,src1) loongarch_format_2r(c,0x00004800,src1,dest)
#define loongarch_bitrev8b(c,dest,src1) loongarch_format_2r(c,0x00004c00,src1,dest)
#define loongarch_bitrevw(c,dest,src1)  loongarch_format_2r(c,0x00005000,src1,dest)
#define loongarch_bitrevd(c,dest,src1)  loongarch_format_2r(c,0x00005400,src1,dest)
#define loongarch_bstrinsw(c,dest,src1,msbw,lsbw)  loongarch_format_2r2ui(c,0x00600000,msbw,lsbw,src1,dest)
#define loongarch_bstrinsd(c,dest,src1,msbd,lsbd)  loongarch_format_2r2ui(c,0x00800000,msbd,lsbd,src1,dest)
#define loongarch_bstrpickw(c,dest,src1,msbw,lsbw) loongarch_format_2r2ui(c,0x00608000,msbw,lsbw,src1,dest)
#define loongarch_bstrpickd(c,dest,src1,msbd,lsbd) loongarch_format_2r2ui(c,0x00c00000,msbd,lsbd,src1,dest)
#define loongarch_maskeqz(c,dest,src1,src2) loongarch_format_3r(c,0x00130000,src2,src1,dest)
#define loongarch_masknez(c,dest,src1,src2) loongarch_format_3r(c,0x00138000,src2,src1,dest)

/* conditional branches */
#define loongarch_beq(c,src1,src2,offs16)  loongarch_format_2rui(c,0x58000000,offs16,src1,src2)
#define loongarch_bne(c,src1,src2,offs16)  loongarch_format_2rui(c,0x5c000000,offs16,src1,src2)
#define loongarch_blt(c,src1,src2,offs16)  loongarch_format_2rui(c,0x60000000,offs16,src1,src2)
#define loongarch_bge(c,src1,src2,offs16)  loongarch_format_2rui(c,0x64000000,offs16,src1,src2)
#define loongarch_bltu(c,src1,src2,offs16) loongarch_format_2rui(c,0x68000000,offs16,src1,src2)
#define loongarch_bgeu(c,src1,src2,offs16) loongarch_format_2rui(c,0x6c000000,offs16,src1,src2)
#define loongarch_beqz(c,src1,offs21) loongarch_format_bsi21(c,0x40000000,offs21,src1)
#define loongarch_bnez(c,src1,offs21) loongarch_format_bsi21(c,0x44000000,offs21,src1)

/* uncond branches and calls */
#define loongarch_b(c,offs26)  loongarch_format_bsi26(c,0x50000000,offs26)
#define loongarch_bl(c,offs26) loongarch_format_bsi26(c,0x54000000,offs26)
#define loongarch_jirl(c,retreg,src1,offs16) loongarch_format_2rui(c,0x4c000000,offs16,src1,retreg)

/* loads */
#define loongarch_ldb(c,dest,base,si12)    loongarch_format_2rui(c,0x28000000,si12,base,dest)
#define loongarch_ldh(c,dest,base,si12)    loongarch_format_2rui(c,0x28400000,si12,base,dest)
#define loongarch_ldw(c,dest,base,si12)    loongarch_format_2rui(c,0x28800000,si12,base,dest)
#define loongarch_ldd(c,dest,base,si12)    loongarch_format_2rui(c,0x28c00000,si12,base,dest)
#define loongarch_ldbu(c,dest,base,si12)   loongarch_format_2rui(c,0x2a000000,si12,base,dest)
#define loongarch_ldhu(c,dest,base,si12)   loongarch_format_2rui(c,0x2a400000,si12,base,dest)
#define loongarch_ldwu(c,dest,base,si12)   loongarch_format_2rui(c,0x2a800000,si12,base,dest)
#define loongarch_ldptrw(c,dest,base,si14) loongarch_format_2rui(c,0x24000000,si14,base,dest)
#define loongarch_ldptrd(c,dest,base,si14) loongarch_format_2rui(c,0x26000000,si14,base,dest)
#define loongarch_ldxb(c,dest,base,indx)   loongarch_format_2rui(c,0x38000000,indx,base,dest)
#define loongarch_ldxh(c,dest,base,indx)   loongarch_format_2rui(c,0x38040000,indx,base,dest)
#define loongarch_ldxw(c,dest,base,indx)   loongarch_format_2rui(c,0x38080000,indx,base,dest)
#define loongarch_ldxd(c,dest,base,indx)   loongarch_format_2rui(c,0x380c0000,indx,base,dest)
#define loongarch_ldxbu(c,dest,base,indx)  loongarch_format_2rui(c,0x38200000,indx,base,dest)
#define loongarch_ldxhu(c,dest,base,indx)  loongarch_format_2rui(c,0x38240000,indx,base,dest)
#define loongarch_ldxwu(c,dest,base,indx)  loongarch_format_2rui(c,0x38280000,indx,base,dest)
/* stores */
#define loongarch_stb(c,src,base,si12)    loongarch_format_2rui(c,0x29000000,si12,base,src)
#define loongarch_sth(c,src,base,si12)    loongarch_format_2rui(c,0x29400000,si12,base,src)
#define loongarch_stw(c,src,base,si12)    loongarch_format_2rui(c,0x29800000,si12,base,src)
#define loongarch_std(c,src,base,si12)    loongarch_format_2rui(c,0x29c00000,si12,base,src)
#define loongarch_stptrw(c,src,base,si14) loongarch_format_2rui(c,0x25000000,si14,base,src)
#define loongarch_stptrd(c,src,base,si14) loongarch_format_2rui(c,0x27000000,si14,base,src)
#define loongarch_stxb(c,src,base,indx)   loongarch_format_3r(c,0x38100000,indx,base,src)
#define loongarch_stxh(c,src,base,indx)   loongarch_format_3r(c,0x38140000,indx,base,src)
#define loongarch_stxw(c,src,base,indx)   loongarch_format_3r(c,0x38180000,indx,base,src)
#define loongarch_stxd(c,src,base,indx)   loongarch_format_3r(c,0x381c0000,indx,base,src)
/* atomic ops */
#define loongarch_llw(c,dest,base,si14) loongarch_format_2rui(c,0x20000000,si14,base,dest)
#define loongarch_lld(c,dest,base,si14) loongarch_format_2rui(c,0x22000000,si14,base,dest)
#define loongarch_scw(c,src,base,si14)  loongarch_format_2rui(c,0x21000000,si14,base,src)
#define loongarch_scd(c,src,base,si14)  loongarch_format_2rui(c,0x23000000,si14,base,src)
#define loongarch_amswapw(c,dest,src1,addr)    loongarch_format_3r(c,0x38600000,addr,src1,dest)
#define loongarch_amswapd(c,dest,src1,addr)    loongarch_format_3r(c,0x38608000,addr,src1,dest)
#define loongarch_amaddw(c,dest,src1,addr)     loongarch_format_3r(c,0x38610000,addr,src1,dest)
#define loongarch_amaddd(c,dest,src1,addr)     loongarch_format_3r(c,0x38618000,addr,src1,dest)
#define loongarch_amandw(c,dest,src1,addr)     loongarch_format_3r(c,0x38620000,addr,src1,dest)
#define loongarch_amandd(c,dest,src1,addr)     loongarch_format_3r(c,0x38628000,addr,src1,dest)
#define loongarch_amorw(c,dest,src1,addr)      loongarch_format_3r(c,0x38630000,addr,src1,dest)
#define loongarch_amord(c,dest,src1,addr)      loongarch_format_3r(c,0x38638000,addr,src1,dest)
#define loongarch_amxorw(c,dest,src1,addr)     loongarch_format_3r(c,0x38640000,addr,src1,dest)
#define loongarch_amxord(c,dest,src1,addr)     loongarch_format_3r(c,0x38648000,addr,src1,dest)
#define loongarch_ammaxw(c,dest,src1,addr)     loongarch_format_3r(c,0x38650000,addr,src1,dest)
#define loongarch_ammaxd(c,dest,src1,addr)     loongarch_format_3r(c,0x38658000,addr,src1,dest)
#define loongarch_amminw(c,dest,src1,addr)     loongarch_format_3r(c,0x38660000,addr,src1,dest)
#define loongarch_ammind(c,dest,src1,addr)     loongarch_format_3r(c,0x38668000,addr,src1,dest)
#define loongarch_ammaxwu(c,dest,src1,addr)    loongarch_format_3r(c,0x38670000,addr,src1,dest)
#define loongarch_ammaxdu(c,dest,src1,addr)    loongarch_format_3r(c,0x38678000,addr,src1,dest)
#define loongarch_amminwu(c,dest,src1,addr)    loongarch_format_3r(c,0x38680000,addr,src1,dest)
#define loongarch_ammindu(c,dest,src1,addr)    loongarch_format_3r(c,0x38688000,addr,src1,dest)

#define loongarch_amswapw_db(c,dest,src1,addr) loongarch_format_3r(c,0x38690000,addr,src1,dest)
#define loongarch_amswapd_db(c,dest,src1,addr) loongarch_format_3r(c,0x38698000,addr,src1,dest)
#define loongarch_amaddw_db(c,dest,src1,addr)  loongarch_format_3r(c,0x386a0000,addr,src1,dest)
#define loongarch_amaddd_db(c,dest,src1,addr)  loongarch_format_3r(c,0x386a8000,addr,src1,dest)
#define loongarch_amandw_db(c,dest,src1,addr)  loongarch_format_3r(c,0x386b0000,addr,src1,dest)
#define loongarch_amandd_db(c,dest,src1,addr)  loongarch_format_3r(c,0x386b8000,addr,src1,dest)
#define loongarch_amorw_db(c,dest,src1,addr)   loongarch_format_3r(c,0x386c0000,addr,src1,dest)
#define loongarch_amord_db(c,dest,src1,addr)   loongarch_format_3r(c,0x386c8000,addr,src1,dest)
#define loongarch_amxorw_db(c,dest,src1,addr)  loongarch_format_3r(c,0x386d0000,addr,src1,dest)
#define loongarch_amxord_db(c,dest,src1,addr)  loongarch_format_3r(c,0x386d8000,addr,src1,dest)
#define loongarch_ammaxw_db(c,dest,src1,addr)  loongarch_format_3r(c,0x386e0000,addr,src1,dest)
#define loongarch_ammaxd_db(c,dest,src1,addr)  loongarch_format_3r(c,0x386e8000,addr,src1,dest)
#define loongarch_amminw_db(c,dest,src1,addr)  loongarch_format_3r(c,0x386f0000,addr,src1,dest)
#define loongarch_ammind_db(c,dest,src1,addr)  loongarch_format_3r(c,0x386f8000,addr,src1,dest)
#define loongarch_ammaxwu_db(c,dest,src1,addr) loongarch_format_3r(c,0x38700000,addr,src1,dest)
#define loongarch_ammaxdu_db(c,dest,src1,addr) loongarch_format_3r(c,0x38708000,addr,src1,dest)
#define loongarch_amminwu_db(c,dest,src1,addr) loongarch_format_3r(c,0x38710000,addr,src1,dest)
#define loongarch_ammindu_db(c,dest,src1,addr) loongarch_format_3r(c,0x38718000,addr,src1,dest)

/* misc ops */
////nop = andi 0,0,0
#define loongarch_nop(pointer)       do {                                \
		*((guint32 *)(pointer)) = 0x03400000;                      \
		(pointer) = (typeof(pointer))(((guint32 *)(pointer)) + 1); \
	} while (0)
#define loongarch_preld(c,hint,base,si12)  loongarch_format_2rui(c,0x2ac00000,si12,base,hint)
#define loongarch_preldx(c,hint,base,indx) loongarch_format_3r(c,0x382c0000,indx,base,hint)
#define loongarch_break(c,code)   loongarch_format_code(c,0x002a0005,code)
#define loongarch_dbar(c,hint)    loongarch_format_code(c,0x38720000,hint)
#define loongarch_ibar(c,hint)    loongarch_format_code(c,0x38728000,hint)
#define loongarch_syscall(c,code) loongarch_format_code(c,0x002b0000,code)
#define loongarch_asrtled(c,src1,src2) loongarch_format_3r(c,0x00010000,src2,src1,0)
#define loongarch_asrtgtd(c,src1,src2) loongarch_format_3r(c,0x00018000,src2,src1,0)
#define loongarch_rdtimed(c,dest,src1) loongarch_format_2r(c,0x00006800,src1,dest)
#define loongarch_cpucfg(c,dest,src1)  loongarch_format_2r(c,0x00006c00,src1,dest)
////TODO: for crc* instructions.
//#define loongarch_crc*(c,dest,src1) loongarch_emit32(c, ((code)<<6)|12)

/* fpu ops */
#define loongarch_fabss(c,dest,src) loongarch_format_2r(c,0x01140400,src,dest)
#define loongarch_fabsd(c,dest,src) loongarch_format_2r(c,0x01140800,src,dest)
#define loongarch_fnegs(c,dest,src) loongarch_format_2r(c,0x01141400,src,dest)
#define loongarch_fnegd(c,dest,src) loongarch_format_2r(c,0x01141800,src,dest)

#define loongarch_fadds(c,dest,src1,src2) loongarch_format_3r(c,0x01008000,src2,src1,dest)
#define loongarch_faddd(c,dest,src1,src2) loongarch_format_3r(c,0x01010000,src2,src1,dest)
#define loongarch_fsubs(c,dest,src1,src2) loongarch_format_3r(c,0x01028000,src2,src1,dest)
#define loongarch_fsubd(c,dest,src1,src2) loongarch_format_3r(c,0x01030000,src2,src1,dest)
#define loongarch_fdivs(c,dest,src1,src2) loongarch_format_3r(c,0x01068000,src2,src1,dest)
#define loongarch_fdivd(c,dest,src1,src2) loongarch_format_3r(c,0x01070000,src2,src1,dest)
#define loongarch_fmuls(c,dest,src1,src2) loongarch_format_3r(c,0x01048000,src2,src1,dest)
#define loongarch_fmuld(c,dest,src1,src2) loongarch_format_3r(c,0x01050000,src2,src1,dest)

#define loongarch_fmadds(c,dest,src1,src2,srcadd)  loongarch_format_3rsa(c,,srcadd,src2,src1,dest)
#define loongarch_fmaddd(c,dest,src1,src2,srcadd)  loongarch_format_3rsa(c,,srcadd,src2,src1,dest)
#define loongarch_fmsubs(c,dest,src1,src2,srcsub)  loongarch_format_3rsa(c,,srcsub,src2,src1,dest)
#define loongarch_fmsubd(c,dest,src1,src2,srcsub)  loongarch_format_3rsa(c,,srcsub,src2,src1,dest)
#define loongarch_fnmadds(c,dest,src1,src2,srcadd) loongarch_format_3rsa(c,,srcadd,src2,src1,dest)
#define loongarch_fnmaddd(c,dest,src1,src2,srcadd) loongarch_format_3rsa(c,,srcadd,src2,src1,dest)
#define loongarch_fnmsubs(c,dest,src1,src2,srcsub) loongarch_format_3rsa(c,,srcsub,src2,src1,dest)
#define loongarch_fnmsubd(c,dest,src1,src2,srcsub) loongarch_format_3rsa(c,,srcsub,src2,src1,dest)

#define loongarch_fsqrts(c,dest,src)  loongarch_format_2r(c,0x01144400,src,dest)
#define loongarch_fsqrtd(c,dest,src)  loongarch_format_2r(c,0x01144800,src,dest)
#define loongarch_frecips(c,dest,src) loongarch_format_2r(c,0x01145400,src,dest)
#define loongarch_frecipd(c,dest,src) loongarch_format_2r(c,0x01145800,src,dest)
#define loongarch_frsqrts(c,dest,src) loongarch_format_2r(c,0x01146400,src,dest)
#define loongarch_frsqrtd(c,dest,src) loongarch_format_2r(c,0x01146800,src,dest)

#define loongarch_fmaxs(c,dest,src1,src2)  loongarch_format_3r(c,0x01088000,src2,src1,dest)
#define loongarch_fmaxd(c,dest,src1,src2)  loongarch_format_3r(c,0x01090000,src2,src1,dest)
#define loongarch_fmins(c,dest,src1,src2)  loongarch_format_3r(c,0x010a8000,src2,src1,dest)
#define loongarch_fmind(c,dest,src1,src2)  loongarch_format_3r(c,0x010b0000,src2,src1,dest)
#define loongarch_fmaxas(c,dest,src1,src2) loongarch_format_3r(c,0x010c8000,src2,src1,dest)
#define loongarch_fmaxad(c,dest,src1,src2) loongarch_format_3r(c,0x010d0000,src2,src1,dest)
#define loongarch_fminas(c,dest,src1,src2) loongarch_format_3r(c,0x010e8000,src2,src1,dest)
#define loongarch_fminad(c,dest,src1,src2) loongarch_format_3r(c,0x010f0000,src2,src1,dest)

#define loongarch_flogbs(c,dest,src)  loongarch_format_2r(c,0x01142400,src,dest)
#define loongarch_flogbd(c,dest,src)  loongarch_format_2r(c,0x01142800,src,dest)
#define loongarch_fclasss(c,dest,src) loongarch_format_2r(c,0x01143400,src,dest)
#define loongarch_fclassd(c,dest,src) loongarch_format_2r(c,0x01143800,src,dest)
#define loongarch_fcopysigns(c,dest,src1,src2) loongarch_format_3r(c,0x01128000,src2,src1,dest)
#define loongarch_fcopysignd(c,dest,src1,src2) loongarch_format_3r(c,0x01130000,src2,src1,dest)
#define loongarch_fscalebs(c,dest,src1,src2)   loongarch_format_3r(c,0x01108000,src2,src1,dest)
#define loongarch_fscalebd(c,dest,src1,src2)   loongarch_format_3r(c,0x01110000,src2,src1,dest)

/* fp compare */
#define loongarch_fcmpcafs(c,cc,src1,src2)  loongarch_format_3rsa(c,0x0c100000,0,src2,src1,cc)
#define loongarch_fcmpcuns(c,cc,src1,src2)  loongarch_format_3rsa(c,0x0c140000,0,src2,src1,cc)
#define loongarch_fcmpceqs(c,cc,src1,src2)  loongarch_format_3rsa(c,0x0c120000,0,src2,src1,cc)
#define loongarch_fcmpcueqs(c,cc,src1,src2) loongarch_format_3rsa(c,0x0c160000,0,src2,src1,cc)
#define loongarch_fcmpclts(c,cc,src1,src2)  loongarch_format_3rsa(c,0x0c110000,0,src2,src1,cc)
#define loongarch_fcmpcults(c,cc,src1,src2) loongarch_format_3rsa(c,0x0c150000,0,src2,src1,cc)
#define loongarch_fcmpcles(c,cc,src1,src2)  loongarch_format_3rsa(c,0x0c130000,0,src2,src1,cc)
#define loongarch_fcmpcules(c,cc,src1,src2) loongarch_format_3rsa(c,0x0c170000,0,src2,src1,cc)
#define loongarch_fcmpcnes(c,cc,src1,src2)  loongarch_format_3rsa(c,0x0c180000,0,src2,src1,cc)
#define loongarch_fcmpcors(c,cc,src1,src2)  loongarch_format_3rsa(c,0x0c1a0000,0,src2,src1,cc)
#define loongarch_fcmpcunes(c,cc,src1,src2) loongarch_format_3rsa(c,0x0c1c0000,0,src2,src1,cc)

#define loongarch_fcmpsafs(c,cc,src1,src2)  loongarch_format_3rsa(c,0x0c108000,0,src2,src1,cc)
#define loongarch_fcmpsuns(c,cc,src1,src2)  loongarch_format_3rsa(c,0x0c148000,0,src2,src1,cc)
#define loongarch_fcmpseqs(c,cc,src1,src2)  loongarch_format_3rsa(c,0x0c128000,0,src2,src1,cc)
#define loongarch_fcmpsueqs(c,cc,src1,src2) loongarch_format_3rsa(c,0x0c168000,0,src2,src1,cc)
#define loongarch_fcmpslts(c,cc,src1,src2)  loongarch_format_3rsa(c,0x0c118000,0,src2,src1,cc)
#define loongarch_fcmpsults(c,cc,src1,src2) loongarch_format_3rsa(c,0x0c158000,0,src2,src1,cc)
#define loongarch_fcmpsles(c,cc,src1,src2)  loongarch_format_3rsa(c,0x0c138000,0,src2,src1,cc)
#define loongarch_fcmpsules(c,cc,src1,src2) loongarch_format_3rsa(c,0x0c178000,0,src2,src1,cc)
#define loongarch_fcmpsnes(c,cc,src1,src2)  loongarch_format_3rsa(c,0x0c188000,0,src2,src1,cc)
#define loongarch_fcmpsors(c,cc,src1,src2)  loongarch_format_3rsa(c,0x0c1a8000,0,src2,src1,cc)
#define loongarch_fcmpsunes(c,cc,src1,src2) loongarch_format_3rsa(c,0x0c1c8000,0,src2,src1,cc)
#define loongarch_fcmpcafd(c,cc,src1,src2)  loongarch_format_3rsa(c,0x0c200000,0,src2,src1,cc)
#define loongarch_fcmpcund(c,cc,src1,src2)  loongarch_format_3rsa(c,0x0c240000,0,src2,src1,cc)
#define loongarch_fcmpceqd(c,cc,src1,src2)  loongarch_format_3rsa(c,0x0c220000,0,src2,src1,cc)
#define loongarch_fcmpcueqd(c,cc,src1,src2) loongarch_format_3rsa(c,0x0c260000,0,src2,src1,cc)
#define loongarch_fcmpcltd(c,cc,src1,src2)  loongarch_format_3rsa(c,0x0c210000,0,src2,src1,cc)
#define loongarch_fcmpcultd(c,cc,src1,src2) loongarch_format_3rsa(c,0x0c250000,0,src2,src1,cc)
#define loongarch_fcmpcled(c,cc,src1,src2)  loongarch_format_3rsa(c,0x0c230000,0,src2,src1,cc)
#define loongarch_fcmpculed(c,cc,src1,src2) loongarch_format_3rsa(c,0x0c270000,0,src2,src1,cc)
#define loongarch_fcmpcned(c,cc,src1,src2)  loongarch_format_3rsa(c,0x0c280000,0,src2,src1,cc)
#define loongarch_fcmpcord(c,cc,src1,src2)  loongarch_format_3rsa(c,0x0c2a0000,0,src2,src1,cc)
#define loongarch_fcmpcuned(c,cc,src1,src2) loongarch_format_3rsa(c,0x0c2c0000,0,src2,src1,cc)
#define loongarch_fcmpsafd(c,cc,src1,src2)  loongarch_format_3rsa(c,0x0c208000,0,src2,src1,cc)
#define loongarch_fcmpsund(c,cc,src1,src2)  loongarch_format_3rsa(c,0x0c248000,0,src2,src1,cc)
#define loongarch_fcmpseqd(c,cc,src1,src2)  loongarch_format_3rsa(c,0x0c228000,0,src2,src1,cc)
#define loongarch_fcmpsueqd(c,cc,src1,src2) loongarch_format_3rsa(c,0x0c268000,0,src2,src1,cc)
#define loongarch_fcmpsltd(c,cc,src1,src2)  loongarch_format_3rsa(c,0x0c218000,0,src2,src1,cc)
#define loongarch_fcmpsultd(c,cc,src1,src2) loongarch_format_3rsa(c,0x0c258000,0,src2,src1,cc)
#define loongarch_fcmpsled(c,cc,src1,src2)  loongarch_format_3rsa(c,0x0c238000,0,src2,src1,cc)
#define loongarch_fcmpsuled(c,cc,src1,src2) loongarch_format_3rsa(c,0x0c278000,0,src2,src1,cc)
#define loongarch_fcmpsned(c,cc,src1,src2)  loongarch_format_3rsa(c,0x0c288000,0,src2,src1,cc)
#define loongarch_fcmpsord(c,cc,src1,src2)  loongarch_format_3rsa(c,0x0c2a8000,0,src2,src1,cc)
#define loongarch_fcmpsuned(c,cc,src1,src2) loongarch_format_3rsa(c,0x0c2c8000,0,src2,src1,cc)
/* fp branch */
#define loongarch_bceqz(c,cj,offs21) loongarch_format_bsi21(c,0x48000000,offs21,cj)
#define loongarch_bcnez(c,cj,offs21) loongarch_format_bsi21(c,0x48000100,offs21,cj)

/* fp convert */
#define loongarch_fcvtds(c,dest,src)  loongarch_format_2r(c,0x01192400,src,dest)
#define loongarch_fcvtsd(c,dest,src)  loongarch_format_2r(c,0x01191800,src,dest)
#define loongarch_frints(c,dest,src)  loongarch_format_2r(c,0x011e4400,src,dest)
#define loongarch_frintd(c,dest,src)  loongarch_format_2r(c,0x011e4800,src,dest)
#define loongarch_ffintsw(c,dest,src) loongarch_format_2r(c,0x011d1000,src,dest)
#define loongarch_ffintsl(c,dest,src) loongarch_format_2r(c,0x011d1800,src,dest)
#define loongarch_ffintdw(c,dest,src) loongarch_format_2r(c,0x011d2000,src,dest)
#define loongarch_ffintdl(c,dest,src) loongarch_format_2r(c,0x011d2800,src,dest)
//round mode by fcsr.
#define loongarch_ftintws(c,dest,src)   loongarch_format_2r(c,0x011b0400,src,dest)
#define loongarch_ftintwd(c,dest,src)   loongarch_format_2r(c,0x011b0800,src,dest)
#define loongarch_ftintls(c,dest,src)   loongarch_format_2r(c,0x011b2400,src,dest)
#define loongarch_ftintld(c,dest,src)   loongarch_format_2r(c,0x011b2800,src,dest)
//round minus.
#define loongarch_ftintrmws(c,dest,src) loongarch_format_2r(c,0x011a0400,src,dest)
#define loongarch_ftintrmwd(c,dest,src) loongarch_format_2r(c,0x011a0800,src,dest)
#define loongarch_ftintrmls(c,dest,src) loongarch_format_2r(c,0x011a2400,src,dest)
#define loongarch_ftintrmld(c,dest,src) loongarch_format_2r(c,0x011a2800,src,dest)
//round positive.
#define loongarch_ftintrpws(c,dest,src) loongarch_format_2r(c,0x011a4400,src,dest)
#define loongarch_ftintrpwd(c,dest,src) loongarch_format_2r(c,0x011a4800,src,dest)
#define loongarch_ftintrpls(c,dest,src) loongarch_format_2r(c,0x011a6400,src,dest)
#define loongarch_ftintrpld(c,dest,src) loongarch_format_2r(c,0x011a6800,src,dest)
//round zero.
#define loongarch_ftintrzws(c,dest,src) loongarch_format_2r(c,0x011a8400,src,dest)
#define loongarch_ftintrzwd(c,dest,src) loongarch_format_2r(c,0x011a8800,src,dest)
#define loongarch_ftintrzls(c,dest,src) loongarch_format_2r(c,0x011aa400,src,dest)
#define loongarch_ftintrzld(c,dest,src) loongarch_format_2r(c,0x011aa800,src,dest)
//round near-even.
#define loongarch_ftintrnews(c,dest,src) loongarch_format_2r(c,0x011ac400,src,dest)
#define loongarch_ftintrnewd(c,dest,src) loongarch_format_2r(c,0x011ac800,src,dest)
#define loongarch_ftintrnels(c,dest,src) loongarch_format_2r(c,0x011ae400,src,dest)
#define loongarch_ftintrneld(c,dest,src) loongarch_format_2r(c,0x011ae800,src,dest)

/* fp moves, loads */
#define loongarch_fmovs(c,dest,src) loongarch_format_2r(c,0x01149400,src,dest)
#define loongarch_fmovd(c,dest,src) loongarch_format_2r(c,0x01149800,src,dest)
#define loongarch_fsel(c,dest,src1,src2,ca) loongarch_format_3rsa(c,0x0d000000,ca,src2,src1,dest)
//gpr --> fpr.
#define loongarch_movgr2frw(c,dest,src)  loongarch_format_2r(c,0x0114a400,src,dest)
#define loongarch_movgr2frhw(c,dest,src) loongarch_format_2r(c,0x0114ac00,src,dest)
#define loongarch_movgr2frd(c,dest,src)  loongarch_format_2r(c,0x0114a800,src,dest)
//fpr --> gpr.
#define loongarch_movfr2grs(c,dest,src)  loongarch_format_2r(c,0x0114b400,src,dest)
#define loongarch_movfrh2grs(c,dest,src) loongarch_format_2r(c,0x0114bc00,src,dest)
#define loongarch_movfr2grd(c,dest,src)  loongarch_format_2r(c,0x0114b800,src,dest)
//gpr --> fcsr.
#define loongarch_movgr2fcsr(c,dest,src) loongarch_format_2r(c,0x0114c000,src,dest)
//fcsr --> gpr.
#define loongarch_movfcsr2gr(c,dest,src) loongarch_format_2r(c,0x0114c800,src,dest)
//float cond flag <--> fpr.
#define loongarch_movfr2cf(c,dest,src)   loongarch_format_2r(c,0x0114d000,src,dest)
#define loongarch_movcf2fr(c,dest,src)   loongarch_format_2r(c,0x0114d400,src,dest)
//float cond flag <--> gpr.
#define loongarch_movgr2cf(c,dest,src)   loongarch_format_2r(c,0x0114d800,src,dest)
#define loongarch_movcf2gr(c,dest,src)   loongarch_format_2r(c,0x0114dc00,src,dest)

/* fp load/store ops */
#define loongarch_flds(c,dest,base,si12)  loongarch_format_2rui(c,0x2b000000,si12,base,dest)
#define loongarch_fldd(c,dest,base,si12)  loongarch_format_2rui(c,0x2b800000,si12,base,dest)
#define loongarch_fsts(c,src,base,si12)   loongarch_format_2rui(c,0x2b400000,si12,base,src)
#define loongarch_fstd(c,src,base,si12)   loongarch_format_2rui(c,0x2bc00000,si12,base,src)

#define loongarch_fldxs(c,dest,base,indx) loongarch_format_3r(c,0x38300000,indx,base,dest)
#define loongarch_fldxd(c,dest,base,indx) loongarch_format_3r(c,0x38340000,indx,base,dest)
#define loongarch_fstxs(c,src,base,indx)  loongarch_format_3r(c,0x38380000,base,indx,src)
#define loongarch_fstxd(c,src,base,indx)  loongarch_format_3r(c,0x383c0000,base,indx,src)

////TODO:no float boundary load/store instructions.

#endif /* __LOONGARCH64_CODEGEN_H__ */
