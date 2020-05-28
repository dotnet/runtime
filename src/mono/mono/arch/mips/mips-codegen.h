#ifndef __MIPS_CODEGEN_H__
#define __MIPS_CODEGEN_H__
/*
 * Copyright (c) 2004 Novell, Inc
 * Author: Paolo Molaro (lupus@ximian.com)
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

/* registers */
enum {
	mips_zero,
	mips_at, /* assembler temp */
	mips_v0, /* return values */
	mips_v1,
	mips_a0, /* 4 - func arguments */
	mips_a1,
	mips_a2,
	mips_a3,
#if _MIPS_SIM == _ABIO32
	mips_t0, /* 8 temporaries */
	mips_t1,
	mips_t2,
	mips_t3,
	mips_t4,
	mips_t5,
	mips_t6,
	mips_t7,
#elif _MIPS_SIM == _ABIN32
	mips_a4, /* 4 more argument registers */
	mips_a5,
	mips_a6,
	mips_a7,
	mips_t0, /* 4 temporaries */
	mips_t1,
	mips_t2,
	mips_t3,
#endif
	mips_s0, /* 16 calle saved */
	mips_s1,
	mips_s2,
	mips_s3,
	mips_s4,
	mips_s5,
	mips_s6,
	mips_s7,
	mips_t8, /* 24 temps */
	mips_t9, /* 25 temp / pic call-through register */
	mips_k0, /* 26 kernel-reserved */
	mips_k1,
	mips_gp, /* 28 */
	mips_sp, /* stack pointer */
	mips_fp, /* frame pointer */
	mips_ra /* return address */
};

/* we treat the register file as containing just doubles... */
enum {
	mips_f0, /* return regs */
	mips_f1,
	mips_f2,
	mips_f3,
	mips_f4, /* temps */
	mips_f5,
	mips_f6,
	mips_f7,
	mips_f8,
	mips_f9,
	mips_f10,
	mips_f11,
	mips_f12, /* first arg */
	mips_f13,
	mips_f14, /* second arg */
	mips_f15,
	mips_f16, /* temps */
	mips_f17,
	mips_f18,
	mips_f19,
	mips_f20, /* callee saved */
	mips_f21,
	mips_f22,
	mips_f23,
	mips_f24,
	mips_f25,
	mips_f26,
	mips_f27,
	mips_f28,
	mips_f29,
	mips_f30,
	mips_f31
};

/* prefetch hints */
enum {
	MIPS_FOR_LOAD,
	MIPS_FOR_STORE,
	MIPS_FOR_LOAD_STREAMED = 4,
	MIPS_FOR_STORE_STREAMED,
	MIPS_FOR_LOAD_RETAINED,
	MIPS_FOR_STORE_RETAINED
};

/* coprocessors */
enum {
	MIPS_COP0,
	MIPS_COP1,
	MIPS_COP2,
	MIPS_COP3
};

enum {
	MIPS_FMT_SINGLE = 16,
	MIPS_FMT_DOUBLE = 17,
	MIPS_FMT_WORD = 20,
	MIPS_FMT_LONG = 21,
	MIPS_FMT3_SINGLE = 0,
	MIPS_FMT3_DOUBLE = 1
};

/* fpu rounding mode */
enum {
	MIPS_ROUND_TO_NEAREST,
	MIPS_ROUND_TO_ZERO,
	MIPS_ROUND_TO_POSINF,
	MIPS_ROUND_TO_NEGINF,
	MIPS_ROUND_MASK = 3
};

/* fpu enable/cause flags, cc */
enum {
	MIPS_FPU_C_MASK = 1 << 23,
	MIPS_INEXACT = 1,
	MIPS_UNDERFLOW = 2,
	MIPS_OVERFLOW = 4,
	MIPS_DIVZERO = 8,
	MIPS_INVALID = 16,
	MIPS_NOTIMPL = 32,
	MIPS_FPU_FLAGS_OFFSET = 2,
	MIPS_FPU_ENABLES_OFFSET = 7,
	MIPS_FPU_CAUSES_OFFSET = 12
};

/* fpu condition values - see manual entry for C.cond.fmt instructions */
enum {
	MIPS_FPU_F,
	MIPS_FPU_UN,
	MIPS_FPU_EQ,
	MIPS_FPU_UEQ,
	MIPS_FPU_OLT,
	MIPS_FPU_ULT,
	MIPS_FPU_OLE,
	MIPS_FPU_ULE,
	MIPS_FPU_SF,
	MIPS_FPU_NGLE,
	MIPS_FPU_SEQ,
	MIPS_FPU_NGL,
	MIPS_FPU_LT,
	MIPS_FPU_NGE,
	MIPS_FPU_LE,
	MIPS_FPU_NGT
};

#if SIZEOF_REGISTER == 4

#define MIPS_SW		mips_sw
#define MIPS_LW		mips_lw
#define MIPS_ADDU	mips_addu
#define MIPS_ADDIU	mips_addiu
#define MIPS_SWC1	mips_swc1
#define MIPS_LWC1	mips_lwc1
#define MIPS_MOVE	mips_move

#elif SIZEOF_REGISTER == 8

#define MIPS_SW		mips_sd
#define MIPS_LW		mips_ld
#define MIPS_ADDU	mips_daddu
#define MIPS_ADDIU	mips_daddiu
#define MIPS_SWC1	mips_sdc1
#define MIPS_LWC1	mips_ldc1
#define MIPS_MOVE	mips_dmove

#else
#error Unknown SIZEOF_REGISTER
#endif

#define mips_emit32(c,x) do {				\
		*((guint32 *) (void *)(c)) = x;				\
		(c) = (typeof(c))(((guint32 *)(void *)(c)) + 1);	\
	} while (0)

#define mips_format_i(code,op,rs,rt,imm) mips_emit32 ((code), (((op)<<26)|((rs)<<21)|((rt)<<16)|((imm)&0xffff)))
#define mips_format_j(code,op,imm) mips_emit32 ((code), (((op)<<26)|((imm)&0x03ffffff)))
#define mips_format_r(code,op,rs,rt,rd,sa,func) mips_emit32 ((code), (((op)<<26)|((rs)<<21)|((rt)<<16)|((rd)<<11)|((sa)<<6)|(func)))
#define mips_format_divmul(code,op,src1,src2,fun) mips_emit32 ((code), (((op)<<26)|((src1)<<21)|((src2)<<16)|(fun)))

#define mips_is_imm16(val) ((gint)(gshort)(gint)(val) == (gint)(val))

/* Load always using lui/addiu pair (for later patching) */
#define mips_load(c,D,v) do {	\
		if (((guint32)(v)) & (1 << 15)) {								\
			mips_lui ((c), (D), mips_zero, (((guint32)(v))>>16)+1);		\
		}																\
		else {															\
			mips_lui ((c), (D), mips_zero, (((guint32)(v))>>16));		\
		}																\
		mips_addiu ((c), (D), (D), ((guint32)(v)) & 0xffff);			\
	} while (0)

/* load constant - no patch-up */
#define mips_load_const(c,D,v) do {	\
		if (!mips_is_imm16 ((v)))	{	\
			if (((guint32)(v)) & (1 << 15)) {		\
				mips_lui ((c), (D), mips_zero, (((guint32)(v))>>16)+1); \
			} \
			else {			\
				mips_lui ((c), (D), mips_zero, (((guint32)(v))>>16)); \
			}						\
			if (((guint32)(v)) & 0xffff) \
				mips_addiu ((c), (D), (D), ((guint32)(v)) & 0xffff); \
		}							\
		else							\
			mips_addiu ((c), (D), mips_zero, ((guint32)(v)) & 0xffff); \
	} while (0)

/* arithmetric ops */
#define mips_add(c,dest,src1,src2) mips_format_r(c,0,src1,src2,dest,0,32)
#define mips_addi(c,dest,src1,imm) mips_format_i(c,8,src1,dest,imm)
#define mips_addu(c,dest,src1,src2) mips_format_r(c,0,src1,src2,dest,0,33)
#define mips_addiu(c,dest,src1,imm) mips_format_i(c,9,src1,dest,imm)
#define mips_dadd(c,dest,src1,src2) mips_format_r(c,0,src1,src2,dest,0,44)
#define mips_daddi(c,dest,src1,imm) mips_format_i(c,24,src1,dest,imm)
#define mips_daddu(c,dest,src1,src2) mips_format_r(c,0,src1,src2,dest,0,45)
#define mips_daddiu(c,dest,src1,imm) mips_format_i(c,25,src1,dest,imm)
#define mips_dsub(c,dest,src1,src2) mips_format_r(c,0,src1,src2,dest,0,46)
#define mips_dsubu(c,dest,src1,src2) mips_format_r(c,0,src1,src2,dest,0,47)
#define mips_mul(c,dest,src1,src2) mips_format_r(c,28,src1,src2,dest,0,2)
#define mips_sub(c,dest,src1,src2) mips_format_r(c,0,src1,src2,dest,0,34)
#define mips_subu(c,dest,src1,src2) mips_format_r(c,0,src1,src2,dest,0,35)

/* div and mul ops */
#define mips_ddiv(c,src1,src2) mips_format_divmul(c,0,src1,src2,30)
#define mips_ddivu(c,src1,src2) mips_format_divmul(c,0,src1,src2,31)
#define mips_div(c,src1,src2) mips_format_divmul(c,0,src1,src2,26)
#define mips_divu(c,src1,src2) mips_format_divmul(c,0,src1,src2,27)
#define mips_dmult(c,src1,src2) mips_format_divmul(c,0,src1,src2,28)
#define mips_dmultu(c,src1,src2) mips_format_divmul(c,0,src1,src2,29)
#define mips_mult(c,src1,src2) mips_format_divmul(c,0,src1,src2,24)
#define mips_multu(c,src1,src2) mips_format_divmul(c,0,src1,src2,25)

/* shift ops */
#define mips_dsll(c,dest,src1,imm) mips_format_r(c,0,0,src1,dest,imm,56)
#define mips_dsll32(c,dest,src1,imm) mips_format_r(c,0,0,src1,dest,imm,60)
#define mips_dsllv(c,dest,src1,src2) mips_format_r(c,0,src2,src1,dest,0,20)
#define mips_dsra(c,dest,src1,imm) mips_format_r(c,0,0,src1,dest,imm,59)
#define mips_dsra32(c,dest,src1,imm) mips_format_r(c,0,0,src1,dest,imm,63)
#define mips_dsrav(c,dest,src1,src2) mips_format_r(c,0,src2,src1,dest,0,23)
#define mips_dsrl(c,dest,src1,imm) mips_format_r(c,0,0,src1,dest,imm,58)
#define mips_dsrl32(c,dest,src1,imm) mips_format_r(c,0,0,src1,dest,imm,62)
#define mips_dsrlv(c,dest,src1,src2) mips_format_r(c,0,src2,src1,dest,0,22)
#define mips_sll(c,dest,src1,imm) mips_format_r(c,0,0,src1,dest,imm,0)
#define mips_sllv(c,dest,src1,src2) mips_format_r(c,0,src2,src1,dest,0,4)
#define mips_sra(c,dest,src1,imm) mips_format_r(c,0,0,src1,dest,imm,3)
#define mips_srav(c,dest,src1,src2) mips_format_r(c,0,src2,src1,dest,0,7)
#define mips_srl(c,dest,src1,imm) mips_format_r(c,0,0,src1,dest,imm,2)
#define mips_srlv(c,dest,src1,src2) mips_format_r(c,0,src2,src1,dest,0,6)

/* logical ops */
#define mips_and(c,dest,src1,src2) mips_format_r(c,0,src1,src2,dest,0,36)
#define mips_andi(c,dest,src1,imm) mips_format_i(c,12,src1,dest,imm)
#define mips_nor(c,dest,src1,src2) mips_format_r(c,0,src1,src2,dest,0,39)
#define mips_or(c,dest,src1,src2) mips_format_r(c,0,src1,src2,dest,0,37)
#define mips_ori(c,dest,src1,uimm) mips_format_i(c,13,src1,dest,uimm)
#define mips_xor(c,dest,src1,src2) mips_format_r(c,0,src1,src2,dest,0,38)
#define mips_xori(c,dest,src1,uimm) mips_format_i(c,14,src1,dest,uimm)

/* compares */
#define mips_slt(c,dest,src1,src2) mips_format_r(c,0,src1,src2,dest,0,42)
#define mips_slti(c,dest,src1,imm) mips_format_i(c,10,src1,dest,imm)
#define mips_sltiu(c,dest,src1,imm) mips_format_i(c,11,src1,dest,imm)
#define mips_sltu(c,dest,src1,src2) mips_format_r(c,0,src1,src2,dest,0,43)
/* missing traps: teq, teqi, tge, tgei, tgeiu, tgeu, tlt, tlti, tltiu, tltu, tne, tnei, */

/* conditional branches */
#define mips_beq(c,src1,src2,offset) mips_format_i(c,4,src1,src2,offset)
#define mips_beql(c,src1,src2,offset) mips_format_i(c,20,src1,src2,offset)
#define mips_bgez(c,src1,offset) mips_format_i(c,1,src1,1,offset)
#define mips_bgezal(c,src1,offset) mips_format_i(c,1,src1,17,offset)
#define mips_bgezall(c,src1,offset) mips_format_i(c,1,src1,19,offset)
#define mips_bgezl(c,src1,offset) mips_format_i(c,1,src1,3,offset)
#define mips_bgtz(c,src1,offset) mips_format_i(c,7,src1,0,offset)
#define mips_bgtzl(c,src1,offset) mips_format_i(c,23,src1,0,offset)
#define mips_blez(c,src1,offset) mips_format_i(c,6,src1,0,offset)
#define mips_blezl(c,src1,offset) mips_format_i(c,22,src1,0,offset)
#define mips_bltz(c,src1,offset) mips_format_i(c,1,src1,0,offset)
#define mips_bltzal(c,src1,offset) mips_format_i(c,1,src1,16,offset)
#define mips_bltzall(c,src1,offset) mips_format_i(c,1,src1,18,offset)
#define mips_bltzl(c,src1,offset) mips_format_i(c,1,src1,2,offset)
#define mips_bne(c,src1,src2,offset) mips_format_i(c,5,src1,src2,offset)
#define mips_bnel(c,src1,src2,offset) mips_format_i(c,21,src1,src2,offset)

/* uncond branches and calls */
#define mips_jump(c,target) mips_format_j(c,2,target)
#define mips_jumpl(c,target) mips_format_j(c,3,target)
#define mips_jalr(c,src1,retreg) mips_format_r(c,0,src1,0,retreg,0,9)
#define mips_jr(c,src1) mips_emit32(c,((src1)<<21)|8)

/* loads and stores */
#define mips_lb(c,dest,base,offset) mips_format_i(c,32,base,dest,offset)
#define mips_lbu(c,dest,base,offset) mips_format_i(c,36,base,dest,offset)
#define mips_ld(c,dest,base,offset) mips_format_i(c,55,base,dest,offset)
#define mips_ldl(c,dest,base,offset) mips_format_i(c,26,base,dest,offset)
#define mips_ldr(c,dest,base,offset) mips_format_i(c,27,base,dest,offset)
#define mips_lh(c,dest,base,offset) mips_format_i(c,33,base,dest,offset)
#define mips_lhu(c,dest,base,offset) mips_format_i(c,37,base,dest,offset)
#define mips_ll(c,dest,base,offset) mips_format_i(c,48,base,dest,offset)
#define mips_lld(c,dest,base,offset) mips_format_i(c,52,base,dest,offset)
#define mips_lui(c,dest,base,uimm) mips_format_i(c,15,base,dest,uimm)
#define mips_lw(c,dest,base,offset) mips_format_i(c,35,base,dest,offset)
#define mips_lwl(c,dest,base,offset) mips_format_i(c,34,base,dest,offset)
#define mips_lwr(c,dest,base,offset) mips_format_i(c,38,base,dest,offset)
#define mips_lwu(c,dest,base,offset) mips_format_i(c,39,base,dest,offset)

#define mips_sb(c,src,base,offset) mips_format_i(c,40,base,src,offset)
#define mips_sc(c,src,base,offset) mips_format_i(c,56,base,src,offset)
#define mips_scd(c,src,base,offset) mips_format_i(c,60,base,src,offset)
#define mips_sd(c,src,base,offset) mips_format_i(c,63,base,src,offset)
#define mips_sdl(c,src,base,offset) mips_format_i(c,44,base,src,offset)
#define mips_sdr(c,src,base,offset) mips_format_i(c,45,base,src,offset)
#define mips_sh(c,src,base,offset) mips_format_i(c,41,base,src,offset)
#define mips_sw(c,src,base,offset) mips_format_i(c,43,base,src,offset)
#define mips_swl(c,src,base,offset) mips_format_i(c,50,base,src,offset)
#define mips_swr(c,src,base,offset) mips_format_i(c,54,base,src,offset)

/* misc and coprocessor ops */
#define mips_move(c,dest,src) mips_addu(c,dest,src,mips_zero)
#define mips_dmove(c,dest,src) mips_daddu(c,dest,src,mips_zero)
#define mips_nop(c) mips_or(c,mips_at,mips_at,0)
#define mips_break(c,code) mips_emit32(c, ((code)<<6)|13)
#define mips_mfhi(c,dest) mips_format_r(c,0,0,0,dest,0,16)
#define mips_mflo(c,dest) mips_format_r(c,0,0,0,dest,0,18)
#define mips_mthi(c,src) mips_format_r(c,0,src,0,0,0,17)
#define mips_mtlo(c,src) mips_format_r(c,0,src,0,0,0,19)
#define mips_movn(c,dest,src,test) mips_format_r(c,0,src,test,dest,0,11)
#define mips_movz(c,dest,src,test) mips_format_r(c,0,src,test,dest,0,10)
#define mips_pref(c,hint,base,offset) mips_format_i(c,51,base,hint,offset)
#define mips_prefidx(c,hint,base,idx) mips_format_r(c,19,base,idx,hint,0,15)
#define mips_sync(c,stype) mips_emit32(c, ((stype)<<6)|15)
#define mips_syscall(c,code) mips_emit32(c, ((code)<<6)|12)

#define mips_cop(c,cop,fun) mips_emit32(c, ((16|(cop))<<26)|(fun))
#define mips_ldc(c,cop,dest,base,offset) mips_format_i(c,(52|(cop)),base,dest,offset)
#define mips_lwc(c,cop,dest,base,offset) mips_format_i(c,(48|(cop)),base,dest,offset)
#define mips_sdc(c,cop,src,base,offset) mips_format_i(c,(60|(cop)),base,src,offset)
#define mips_swc(c,cop,src,base,offset) mips_format_i(c,(56|(cop)),base,src,offset)
#define mips_cfc1(c,dest,src) mips_format_r(c,17,2,dest,src,0,0)
#define mips_ctc1(c,dest,src) mips_format_r(c,17,6,dest,src,0,0)

/* fpu ops */
#define mips_fabss(c,dest,src) mips_format_r(c,17,MIPS_FMT_SINGLE,0,src,dest,5)
#define mips_fabsd(c,dest,src) mips_format_r(c,17,MIPS_FMT_DOUBLE,0,src,dest,5)
#define mips_fadds(c,dest,src1,src2) mips_format_r(c,17,MIPS_FMT_SINGLE,src2,src1,dest,0)
#define mips_faddd(c,dest,src1,src2) mips_format_r(c,17,MIPS_FMT_DOUBLE,src2,src1,dest,0)
#define mips_fdivs(c,dest,src1,src2) mips_format_r(c,17,MIPS_FMT_SINGLE,src2,src1,dest,3)
#define mips_fdivd(c,dest,src1,src2) mips_format_r(c,17,MIPS_FMT_DOUBLE,src2,src1,dest,3)
#define mips_fmuls(c,dest,src1,src2) mips_format_r(c,17,MIPS_FMT_SINGLE,src2,src1,dest,2)
#define mips_fmuld(c,dest,src1,src2) mips_format_r(c,17,MIPS_FMT_DOUBLE,src2,src1,dest,2)
#define mips_fnegs(c,dest,src) mips_format_r(c,17,MIPS_FMT_SINGLE,0,src,dest,7)
#define mips_fnegd(c,dest,src) mips_format_r(c,17,MIPS_FMT_DOUBLE,0,src,dest,7)
#define mips_fsqrts(c,dest,src) mips_format_r(c,17,MIPS_FMT_SINGLE,0,src,dest,4)
#define mips_fsqrtd(c,dest,src) mips_format_r(c,17,MIPS_FMT_DOUBLE,0,src,dest,4)
#define mips_fsubs(c,dest,src1,src2) mips_format_r(c,17,MIPS_FMT_SINGLE,src2,src1,dest,1)
#define mips_fsubd(c,dest,src1,src2) mips_format_r(c,17,MIPS_FMT_DOUBLE,src2,src1,dest,1)
#define mips_madds(c,dest,src1,src2,srcadd) mips_format_r(c,19,srcadd,src2,src1,dest,32|MIPS_FMT_SINGLE)
#define mips_maddd(c,dest,src1,src2,srcadd) mips_format_r(c,19,srcadd,src2,src1,dest,32|MIPS_FMT_DOUBLE)
#define mips_nmadds(c,dest,src1,src2,srcadd) mips_format_r(c,19,srcadd,src2,src1,dest,48|MIPS_FMT_SINGLE)
#define mips_nmaddd(c,dest,src1,src2,srcadd) mips_format_r(c,19,srcadd,src2,src1,dest,48|MIPS_FMT_DOUBLE)
#define mips_msubs(c,dest,src1,src2,srcsub) mips_format_r(c,19,srcsub,src2,src1,dest,40|MIPS_FMT_SINGLE)
#define mips_msubd(c,dest,src1,src2,srcsub) mips_format_r(c,19,srcsub,src2,src1,dest,40|MIPS_FMT_DOUBLE)
#define mips_nmsubs(c,dest,src1,src2,srcsub) mips_format_r(c,19,srcsub,src2,src1,dest,56|MIPS_FMT_SINGLE)
#define mips_nmsubd(c,dest,src1,src2,srcsub) mips_format_r(c,19,srcsub,src2,src1,dest,56|MIPS_FMT_DOUBLE)

/* fp compare and branch */
#define mips_fcmps(c,cond,src1,src2) mips_format_r(c,17,MIPS_FMT_SINGLE,src2,src1,0,(3<<4)|(cond))
#define mips_fcmpd(c,cond,src1,src2) mips_format_r(c,17,MIPS_FMT_DOUBLE,src2,src1,0,(3<<4)|(cond))
#define mips_fbfalse(c,offset) mips_format_i(c,17,8,0,offset)
#define mips_fbfalsel(c,offset) mips_format_i(c,17,8,2,offset)
#define mips_fbtrue(c,offset) mips_format_i(c,17,8,1,offset)
#define mips_fbtruel(c,offset) mips_format_i(c,17,8,3,offset)

/* fp convert */
#define mips_ceills(c,dest,src) mips_format_r(c,17,MIPS_FMT_SINGLE,0,src,dest,10)
#define mips_ceilld(c,dest,src) mips_format_r(c,17,MIPS_FMT_DOUBLE,0,src,dest,10)
#define mips_ceilws(c,dest,src) mips_format_r(c,17,MIPS_FMT_SINGLE,0,src,dest,14)
#define mips_ceilwd(c,dest,src) mips_format_r(c,17,MIPS_FMT_DOUBLE,0,src,dest,14)
#define mips_cvtds(c,dest,src) mips_format_r(c,17,MIPS_FMT_SINGLE,0,src,dest,33)
#define mips_cvtdw(c,dest,src) mips_format_r(c,17,MIPS_FMT_WORD,0,src,dest,33)
#define mips_cvtdl(c,dest,src) mips_format_r(c,17,MIPS_FMT_LONG,0,src,dest,33)
#define mips_cvtls(c,dest,src) mips_format_r(c,17,MIPS_FMT_SINGLE,0,src,dest,37)
#define mips_cvtld(c,dest,src) mips_format_r(c,17,MIPS_FMT_DOUBLE,0,src,dest,37)
#define mips_cvtsd(c,dest,src) mips_format_r(c,17,MIPS_FMT_DOUBLE,0,src,dest,32)
#define mips_cvtsw(c,dest,src) mips_format_r(c,17,MIPS_FMT_WORD,0,src,dest,32)
#define mips_cvtsl(c,dest,src) mips_format_r(c,17,MIPS_FMT_LONG,0,src,dest,32)
#define mips_cvtws(c,dest,src) mips_format_r(c,17,MIPS_FMT_SINGLE,0,src,dest,36)
#define mips_cvtwd(c,dest,src) mips_format_r(c,17,MIPS_FMT_DOUBLE,0,src,dest,36)
#define mips_floorls(c,dest,src) mips_format_r(c,17,MIPS_FMT_SINGLE,0,src,dest,11)
#define mips_floorld(c,dest,src) mips_format_r(c,17,MIPS_FMT_DOUBLE,0,src,dest,11)
#define mips_floorws(c,dest,src) mips_format_r(c,17,MIPS_FMT_SINGLE,0,src,dest,15)
#define mips_floorwd(c,dest,src) mips_format_r(c,17,MIPS_FMT_DOUBLE,0,src,dest,15)
#define mips_roundls(c,dest,src) mips_format_r(c,17,MIPS_FMT_SINGLE,0,src,dest,8)
#define mips_roundld(c,dest,src) mips_format_r(c,17,MIPS_FMT_DOUBLE,0,src,dest,8)
#define mips_roundws(c,dest,src) mips_format_r(c,17,MIPS_FMT_SINGLE,0,src,dest,12)
#define mips_roundwd(c,dest,src) mips_format_r(c,17,MIPS_FMT_DOUBLE,0,src,dest,12)
#define mips_truncls(c,dest,src) mips_format_r(c,17,MIPS_FMT_SINGLE,0,src,dest,9)
#define mips_truncld(c,dest,src) mips_format_r(c,17,MIPS_FMT_DOUBLE,0,src,dest,9)
#define mips_truncws(c,dest,src) mips_format_r(c,17,MIPS_FMT_SINGLE,0,src,dest,13)
#define mips_truncwd(c,dest,src) mips_format_r(c,17,MIPS_FMT_DOUBLE,0,src,dest,13)

/* fp moves, loads */
#define mips_fmovs(c,dest,src) mips_format_r(c,17,MIPS_FMT_SINGLE,0,src,dest,6)
#define mips_fmovd(c,dest,src) mips_format_r(c,17,MIPS_FMT_DOUBLE,0,src,dest,6)
#define mips_mfc1(c,dest,src) mips_format_r(c,17,0,dest,src,0,0)
#define mips_mtc1(c,dest,src) mips_format_r(c,17,4,src,dest,0,0)
#define mips_dmfc1(c,dest,src) mips_format_r(c,17,1,0,dest,src,0)
#define mips_dmtc1(c,dest,src) mips_format_r(c,17,1,0,src,dest,0)
#define mips_ldc1(c,dest,base,offset) mips_ldc(c,1,dest,base,offset)
#define mips_ldxc1(c,dest,base,idx) mips_format_r(c,19,base,idx,0,dest,1)
#define mips_lwc1(c,dest,base,offset) mips_lwc(c,1,dest,base,offset)
#define mips_lwxc1(c,dest,base,idx) mips_format_r(c,19,base,idx,0,dest,0)
#define mips_sdc1(c,src,base,offset) mips_sdc(c,1,src,base,offset)
#define mips_sdxc1(c,src,base,idx) mips_format_r(c,19,base,idx,src,0,9)
#define mips_swc1(c,src,base,offset) mips_swc(c,1,src,base,offset)
#define mips_swxc1(c,src,base,idx) mips_format_r(c,19,base,idx,src,0,8)

#endif /* __MIPS_CODEGEN_H__ */

