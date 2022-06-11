/*
   Authors:
     Radek Doulik
     Christopher Taylor <ct_AT_clemson_DOT_edu>
     Andreas Faerber <andreas.faerber@web.de>

   Copyright (C)  2001 Radek Doulik
   Copyright (C)  2007-2008 Andreas Faerber

   for testing do the following: ./test | as -o test.o
   Licensed under the MIT license. See LICENSE file in the project root for full license information.
*/

#ifndef __MONO_PPC_CODEGEN_H__
#define __MONO_PPC_CODEGEN_H__
#include <glib.h>
#include <assert.h>

typedef enum {
	ppc_r0 = 0,
	ppc_r1,
	ppc_sp = ppc_r1,
	ppc_r2,
	ppc_r3,
	ppc_r4,
	ppc_r5,
	ppc_r6,
	ppc_r7,
	ppc_r8,
	ppc_r9,
	ppc_r10,
	ppc_r11,
	ppc_r12,
	ppc_r13,
	ppc_r14,
	ppc_r15,
	ppc_r16,
	ppc_r17,
	ppc_r18,
	ppc_r19,
	ppc_r20,
	ppc_r21,
	ppc_r22,
	ppc_r23,
	ppc_r24,
	ppc_r25,
	ppc_r26,
	ppc_r27,
	ppc_r28,
	ppc_r29,
	ppc_r30,
	ppc_r31
} PPCIntRegister;

typedef enum {
	ppc_f0 = 0,
	ppc_f1,
	ppc_f2,
	ppc_f3,
	ppc_f4,
	ppc_f5,
	ppc_f6,
	ppc_f7,
	ppc_f8,
	ppc_f9,
	ppc_f10,
	ppc_f11,
	ppc_f12,
	ppc_f13,
	ppc_f14,
	ppc_f15,
	ppc_f16,
	ppc_f17,
	ppc_f18,
	ppc_f19,
	ppc_f20,
	ppc_f21,
	ppc_f22,
	ppc_f23,
	ppc_f24,
	ppc_f25,
	ppc_f26,
	ppc_f27,
	ppc_f28,
	ppc_f29,
	ppc_f30,
	ppc_f31
} PPCFloatRegister;

typedef enum {
	ppc_lr = 256,
	ppc_ctr = 256 + 32,
	ppc_xer = 32
} PPCSpecialRegister;

enum {
	/* B0 operand for branches */
	PPC_BR_DEC_CTR_NONZERO_FALSE = 0,
	PPC_BR_LIKELY = 1, /* can be or'ed with the conditional variants */
	PPC_BR_DEC_CTR_ZERO_FALSE = 2,
	PPC_BR_FALSE  = 4,
	PPC_BR_DEC_CTR_NONZERO_TRUE = 8,
	PPC_BR_DEC_CTR_ZERO_TRUE = 10,
	PPC_BR_TRUE   = 12,
	PPC_BR_DEC_CTR_NONZERO = 16,
	PPC_BR_DEC_CTR_ZERO = 18,
	PPC_BR_ALWAYS = 20,
	/* B1 operand for branches */
	PPC_BR_LT     = 0,
	PPC_BR_GT     = 1,
	PPC_BR_EQ     = 2,
	PPC_BR_SO     = 3
};

enum {
	PPC_TRAP_LT = 1,
	PPC_TRAP_GT = 2,
	PPC_TRAP_EQ = 4,
	PPC_TRAP_LT_UN = 8,
	PPC_TRAP_GT_UN = 16,
	PPC_TRAP_LE = 1 + PPC_TRAP_EQ,
	PPC_TRAP_GE = 2 + PPC_TRAP_EQ,
	PPC_TRAP_LE_UN = 8 + PPC_TRAP_EQ,
	PPC_TRAP_GE_UN = 16 + PPC_TRAP_EQ
};

#define ppc_emit32(c,x) do { *((guint32 *) (c)) = (guint32) (x); (c) = ((guint8 *)(c) + sizeof (guint32));} while (0)

#define ppc_is_imm16(val) ((((val)>> 15) == 0) || (((val)>> 15) == -1))
#define ppc_is_uimm16(val) ((glong)(val) >= 0L && (glong)(val) <= 65535L)
#define ppc_ha(val) (((val >> 16) + ((val & 0x8000) ? 1 : 0)) & 0xffff)
#define ppc_is_dsoffset_valid(offset) (((offset)& 3) == 0)

#define ppc_load32(c,D,v) G_STMT_START {	\
		ppc_lis ((c), (D),      (guint32)(v) >> 16);	\
		ppc_ori ((c), (D), (D), (guint32)(v) & 0xffff);	\
	} G_STMT_END

/* Macros to load/store pointer sized quantities */

#if defined(TARGET_POWERPC64) && !defined(MONO_ARCH_ILP32)

#define ppc_ldptr(c,D,d,A)         ppc_ld   ((c), (D), (d), (A))
#define ppc_ldptr_update(c,D,d,A)  ppc_ldu  ((c), (D), (d), (A))
#define ppc_ldptr_indexed(c,D,A,B)        ppc_ldx  ((c), (D), (A), (B))
#define ppc_ldptr_update_indexed(c,D,A,B) ppc_ldux ((c), (D), (A), (B))

#define ppc_stptr(c,S,d,A)        ppc_std  ((c), (S), (d), (A))
#define ppc_stptr_update(c,S,d,A) ppc_stdu ((c), (S), (d), (A))
#define ppc_stptr_indexed(c,S,A,B)        ppc_stdx  ((c), (S), (A), (B))
#define ppc_stptr_update_indexed(c,S,A,B) ppc_stdux ((c), (S), (A), (B))

#else

/* Same as ppc32 */
#define ppc_ldptr(c,D,d,A)         ppc_lwz  ((c), (D), (d), (A))
#define ppc_ldptr_update(c,D,d,A)  ppc_lwzu ((c), (D), (d), (A))
#define ppc_ldptr_indexed(c,D,A,B)        ppc_lwzx ((c), (D), (A), (B))
#define ppc_ldptr_update_indexed(c,D,A,B) ppc_lwzux ((c), (D), (A), (B))

#define ppc_stptr(c,S,d,A)        ppc_stw  ((c), (S), (d), (A))
#define ppc_stptr_update(c,S,d,A) ppc_stwu ((c), (S), (d), (A))
#define ppc_stptr_indexed(c,S,A,B)        ppc_stwx  ((c), (S), (A), (B))
#define ppc_stptr_update_indexed(c,S,A,B) ppc_stwux ((c), (S), (A), (B))

#endif

/* Macros to load pointer sized immediates */
#define ppc_load_ptr(c,D,v) ppc_load ((c),(D),(gsize)(v))
#define ppc_load_ptr_sequence(c,D,v) ppc_load_sequence ((c),(D),(gsize)(v))

/* Macros to load/store regsize quantities */

#ifdef TARGET_POWERPC64
#define ppc_ldr(c,D,d,A)         ppc_ld  ((c), (D), (d), (A))
#define ppc_ldr_indexed(c,D,A,B) ppc_ldx  ((c), (D), (A), (B))
#define ppc_str(c,S,d,A)         ppc_std ((c), (S), (d), (A))
#define ppc_str_update(c,S,d,A)  ppc_stdu ((c), (S), (d), (A))
#define ppc_str_indexed(c,S,A,B) ppc_stdx ((c), (S), (A), (B))
#define ppc_str_update_indexed(c,S,A,B) ppc_stdux ((c), (S), (A), (B))
#else
#define ppc_ldr(c,D,d,A)         ppc_lwz  ((c), (D), (d), (A))
#define ppc_ldr_indexed(c,D,A,B) ppc_lwzx ((c), (D), (A), (B))
#define ppc_str(c,S,d,A)         ppc_stw ((c), (S), (d), (A))
#define ppc_str_update(c,S,d,A)  ppc_stwu ((c), (S), (d), (A))
#define ppc_str_indexed(c,S,A,B) ppc_stwx ((c), (S), (A), (B))
#define ppc_str_update_indexed(c,S,A,B) ppc_stwux ((c), (S), (A), (B))
#endif

#define ppc_str_multiple(c,S,d,A) ppc_store_multiple_regs((c),(S),(d),(A))
#define ppc_ldr_multiple(c,D,d,A) ppc_load_multiple_regs((c),(D),(d),(A))

/* PPC32 macros */

#ifndef TARGET_POWERPC64

#define ppc_load_sequence(c,D,v) ppc_load32 ((c), (D), (guint32)(v))

#define PPC_LOAD_SEQUENCE_LENGTH	8

#define ppc_load(c,D,v) G_STMT_START {	\
		if (ppc_is_imm16 ((guint32)(v)))	{	\
			ppc_li ((c), (D), (guint16)(guint32)(v));	\
		} else {	\
			ppc_load32 ((c), (D), (guint32)(v));	\
		}	\
	} G_STMT_END

#define ppc_load_func(c,D,V)	      ppc_load_sequence ((c), (D), (V))

#define ppc_load_multiple_regs(c,D,d,A)      ppc_lmw   ((c), (D), (d), (A))

#define ppc_store_multiple_regs(c,S,d,A)      ppc_stmw  ((c), (S), (d), (A))

#define ppc_compare(c,cfrD,A,B)		      ppc_cmp((c), (cfrD), 0, (A), (B))
#define ppc_compare_reg_imm(c,cfrD,A,B)	      ppc_cmpi((c), (cfrD), 0, (A), (B))
#define ppc_compare_log(c,cfrD,A,B)	      ppc_cmpl((c), (cfrD), 0, (A), (B))

#define ppc_shift_left(c,A,S,B)		      ppc_slw((c), (S), (A), (B))
#define ppc_shift_left_imm(c,A,S,n)	      ppc_slwi((c), (A), (S), (n))

#define ppc_shift_right_imm(c,A,S,B)	      ppc_srwi((c), (A), (S), (B))
#define ppc_shift_right_arith_imm(c,A,S,B)    ppc_srawi((c), (A), (S), (B))

#define ppc_multiply(c,D,A,B)		      ppc_mullw((c), (D), (A), (B))

#define ppc_clear_right_imm(c,A,S,n)	      ppc_clrrwi((c), (A), (S), (n))

#endif

#define ppc_opcode(c) ((c) >> 26)
#define ppc_split_5_1_1(x) (((x) >> 5) & 0x1)
#define ppc_split_5_1_5(x) ((x) & 0x1F)
#define ppc_split_5_1(x) ((ppc_split_5_1_5(x) << 1) | ppc_split_5_1_1(x))

#define ppc_break(c) ppc_tw((c),31,0,0)
#define  ppc_addi(c,D,A,i) ppc_emit32 (c, (14 << 26) | ((D) << 21) | ((A) << 16) | (guint16)(i))
#define ppc_addis(c,D,A,i) ppc_emit32 (c, (15 << 26) | ((D) << 21) | ((A) << 16) | (guint16)(i))
#define    ppc_li(c,D,v)   ppc_addi   (c, D, 0, (guint16)(v))
#define   ppc_lis(c,D,v)   ppc_addis  (c, D, 0, (guint16)(v))
#define   ppc_lwz(c,D,d,A) ppc_emit32 (c, (32 << 26) | ((D) << 21) | ((A) << 16) | (guint16)(d))
#define   ppc_lhz(c,D,d,A) ppc_emit32 (c, (40 << 26) | ((D) << 21) | ((A) << 16) | (guint16)(d))
#define   ppc_lbz(c,D,d,A) ppc_emit32 (c, (34 << 26) | ((D) << 21) | ((A) << 16) | (guint16)(d))
#define   ppc_stw(c,S,d,A) ppc_emit32 (c, (36 << 26) | ((S) << 21) | ((A) << 16) | (guint16)(d))
#define   ppc_sth(c,S,d,A) ppc_emit32 (c, (44 << 26) | ((S) << 21) | ((A) << 16) | (guint16)(d))
#define   ppc_stb(c,S,d,A) ppc_emit32 (c, (38 << 26) | ((S) << 21) | ((A) << 16) | (guint16)(d))
#define  ppc_stwu(c,s,d,A) ppc_emit32 (c, (37 << 26) | ((s) << 21) | ((A) << 16) | (guint16)(d))
#define    ppc_or(c,a,s,b) ppc_emit32 (c, (31 << 26) | ((s) << 21) | ((a) << 16) | ((b) << 11) | 888)
#define    ppc_mr(c,a,s)   ppc_or     (c, a, s, s)
#define   ppc_ori(c,S,A,ui) ppc_emit32 (c, (24 << 26) | ((S) << 21) | ((A) << 16) | (guint16)(ui))
#define	  ppc_nop(c)       ppc_ori    (c, 0, 0, 0)
#define ppc_mfspr(c,D,spr) ppc_emit32 (c, (31 << 26) | ((D) << 21) | ((spr) << 11) | (339 << 1))
#define  ppc_mflr(c,D)     ppc_mfspr  (c, D, ppc_lr)
#define ppc_mtspr(c,spr,S) ppc_emit32 (c, (31 << 26) | ((S) << 21) | ((spr) << 11) | (467 << 1))
#define  ppc_mtlr(c,S)     ppc_mtspr  (c, ppc_lr, S)
#define  ppc_mtctr(c,S)     ppc_mtspr  (c, ppc_ctr, S)
#define  ppc_mtxer(c,S)     ppc_mtspr  (c, ppc_xer, S)

#define  ppc_b(c,li)       ppc_emit32 (c, (18 << 26) | ((li) << 2))
#define  ppc_bl(c,li)       ppc_emit32 (c, (18 << 26) | ((li) << 2) | 1)
#define  ppc_ba(c,li)       ppc_emit32 (c, (18 << 26) | ((li) << 2) | 2)
#define  ppc_bla(c,li)       ppc_emit32 (c, (18 << 26) | ((li) << 2) | 3)
#define  ppc_blrl(c)       ppc_emit32 (c, 0x4e800021)
#define   ppc_blr(c)       ppc_emit32 (c, 0x4e800020)

#define   ppc_lfs(c,D,d,A) ppc_emit32 (c, (48 << 26) | ((D) << 21) | ((A) << 16) | (guint16)(d))
#define   ppc_lfd(c,D,d,A) ppc_emit32 (c, (50 << 26) | ((D) << 21) | ((A) << 16) | (guint16)(d))
#define  ppc_stfs(c,S,d,a) ppc_emit32 (c, (52 << 26) | ((S) << 21) | ((a) << 16) | (guint16)(d))
#define  ppc_stfd(c,S,d,a) ppc_emit32 (c, (54 << 26) | ((S) << 21) | ((a) << 16) | (guint16)(d))

/***********************************************************************
The macros below were tapped out by Christopher Taylor <ct_AT_clemson_DOT_edu>
from 18 November 2002 to 19 December 2002.

Special thanks to rodo, lupus, dietmar, miguel, and duncan for patience,
and motivation.

The macros found in this file are based on the assembler instructions found
in Motorola and Digital DNA's:

"Programming Enviornments Manual For 32-bit Implementations of the PowerPC Architecture"

MPCFPE32B/AD
12/2001
REV2

see pages 326 - 524 for detailed information regarding each instruction

Also see the "Ximian Copyright Agreement, 2002" for more information regarding
my and Ximian's copyright to this code. ;)
*************************************************************************/

#define ppc_addx(c,D,A,B,OE,Rc) ppc_emit32(c, (31 << 26) | ((D) << 21) | ((A) << 16) | ((B) << 11) | (OE << 10) | (266 << 1) | Rc)
#define ppc_add(c,D,A,B) ppc_addx(c,D,A,B,0,0)
#define ppc_addd(c,D,A,B) ppc_addx(c,D,A,B,0,1)
#define ppc_addo(c,D,A,B) ppc_addx(c,D,A,B,1,0)
#define ppc_addod(c,D,A,B) ppc_addx(c,D,A,B,1,1)

#define ppc_addcx(c,D,A,B,OE,Rc) ppc_emit32(c, (31 << 26) | ((D) << 21) | ((A) << 16) | ((B) << 11) | (OE << 10) | (10 << 1) | Rc)
#define ppc_addc(c,D,A,B) ppc_addcx(c,D,A,B,0,0)
#define ppc_addcd(c,D,A,B) ppc_addcx(c,D,A,B,0,1)
#define ppc_addco(c,D,A,B) ppc_addcx(c,D,A,B,1,0)
#define ppc_addcod(c,D,A,B) ppc_addcx(c,D,A,B,1,1)

#define ppc_addex(c,D,A,B,OE,Rc) ppc_emit32(c, (31 << 26) | ((D) << 21) | ((A) << 16) | ((B) << 11) | (OE << 10) | (138 << 1) | Rc)
#define ppc_adde(c,D,A,B) ppc_addex(c,D,A,B,0,0)
#define ppc_added(c,D,A,B) ppc_addex(c,D,A,B,0,1)
#define ppc_addeo(c,D,A,B) ppc_addex(c,D,A,B,1,0)
#define ppc_addeod(c,D,A,B) ppc_addex(c,D,A,B,1,1)

#define ppc_addic(c,D,A,i) ppc_emit32(c, (12 << 26) | ((D) << 21) | ((A) << 16) | (guint16)(i))
#define ppc_addicd(c,D,A,i) ppc_emit32(c, (13 << 26) | ((D) << 21) | ((A) << 16) | (guint16)(i))

#define ppc_addmex(c,D,A,OE,RC) ppc_emit32(c, (31 << 26) | ((D) << 21 ) | ((A) << 16) | (0 << 11) | ((OE) << 10) | (234 << 1) | RC)
#define ppc_addme(c,D,A) ppc_addmex(c,D,A,0,0)
#define ppc_addmed(c,D,A) ppc_addmex(c,D,A,0,1)
#define ppc_addmeo(c,D,A) ppc_addmex(c,D,A,1,0)
#define ppc_addmeod(c,D,A) ppc_addmex(c,D,A,1,1)

#define ppc_addzex(c,D,A,OE,RC) ppc_emit32(c, (31 << 26) | ((D) << 21 ) | ((A) << 16) | (0 << 11) | ((OE) << 10) | (202 << 1) | RC)
#define ppc_addze(c,D,A) ppc_addzex(c,D,A,0,0)
#define ppc_addzed(c,D,A) ppc_addzex(c,D,A,0,1)
#define ppc_addzeo(c,D,A) ppc_addzex(c,D,A,1,0)
#define ppc_addzeod(c,D,A) ppc_addzex(c,D,A,1,1)

#define ppc_andx(c,S,A,B,RC) ppc_emit32(c, (31 << 26) | ((S) << 21 ) | ((A) << 16) | ((B) << 11) | (28 << 1) | RC)
#define ppc_and(c,S,A,B) ppc_andx(c,S,A,B,0)
#define ppc_andd(c,S,A,B) ppc_andx(c,S,A,B,1)

#define ppc_andcx(c,S,A,B,RC) ppc_emit32(c, (31 << 26) | ((S) << 21 ) | ((A) << 16) | ((B) << 11) | (60 << 1) | RC)
#define ppc_andc(c,S,A,B) ppc_andcx(c,S,A,B,0)
#define ppc_andcd(c,S,A,B) ppc_andcx(c,S,A,B,1)

#define ppc_andid(c,S,A,ui) ppc_emit32(c, (28 << 26) | ((S) << 21 ) | ((A) << 16) | ((guint16)(ui)))
#define ppc_andisd(c,S,A,ui) ppc_emit32(c, (29 << 26) | ((S) << 21 ) | ((A) << 16) | ((guint16)(ui)))

#define ppc_bcx(c,BO,BI,BD,AA,LK) ppc_emit32(c, (16 << 26) | ((BO) << 21 )| ((BI) << 16) | (BD << 2) | ((AA) << 1) | LK)
#define ppc_bc(c,BO,BI,BD) ppc_bcx(c,BO,BI,BD,0,0)
#define ppc_bca(c,BO,BI,BD) ppc_bcx(c,BO,BI,BD,1,0)
#define ppc_bcl(c,BO,BI,BD) ppc_bcx(c,BO,BI,BD,0,1)
#define ppc_bcla(c,BO,BI,BD) ppc_bcx(c,BO,BI,BD,1,1)

#define ppc_bcctrx(c,BO,BI,LK) ppc_emit32(c, (19 << 26) | (BO << 21 )| (BI << 16) | (0 << 11) | (528 << 1) | LK)
#define ppc_bcctr(c,BO,BI) ppc_bcctrx(c,BO,BI,0)
#define ppc_bcctrl(c,BO,BI) ppc_bcctrx(c,BO,BI,1)

#define ppc_bnectrp(c,BO,BI) ppc_bcctr(c,BO,BI)
#define ppc_bnectrlp(c,BO,BI) ppc_bcctr(c,BO,BI)

#define ppc_bclrx(c,BO,BI,BH,LK) ppc_emit32(c, (19 << 26) | ((BO) << 21 )| ((BI) << 16) | (0 << 13) | ((BH) << 11) | (16 << 1) | (LK))
#define ppc_bclr(c,BO,BI,BH) ppc_bclrx(c,BO,BI,BH,0)
#define ppc_bclrl(c,BO,BI,BH) ppc_bclrx(c,BO,BI,BH,1)

#define ppc_bnelrp(c,BO,BI) ppc_bclr(c,BO,BI,0)
#define ppc_bnelrlp(c,BO,BI) ppc_bclr(c,BO,BI,0)

#define ppc_cmp(c,cfrD,L,A,B) ppc_emit32(c, (31 << 26) | ((cfrD) << 23) | (0 << 22) | ((L) << 21) | ((A) << 16) | ((B) << 11) | (0 << 1) | 0)
#define ppc_cmpi(c,cfrD,L,A,B) ppc_emit32(c, (11 << 26) | (cfrD << 23) | (0 << 22) | (L << 21) | (A << 16) | (guint16)(B))
#define ppc_cmpl(c,cfrD,L,A,B) ppc_emit32(c, (31 << 26) | ((cfrD) << 23) | (0 << 22) | ((L) << 21) | ((A) << 16) | ((B) << 11) | (32 << 1) | 0)
#define ppc_cmpli(c,cfrD,L,A,B) ppc_emit32(c, (10 << 26) | (cfrD << 23) | (0 << 22) | (L << 21) | (A << 16) | (guint16)(B))
#define ppc_cmpw(c,cfrD,A,B) ppc_cmp(c, (cfrD), 0, (A), (B))

#define ppc_cntlzwx(c,S,A,Rc) ppc_emit32(c, (31 << 26) | (S << 21) | (A << 16) | (0 << 11) | (26 << 1) | Rc)
#define ppc_cntlzw(c,S,A) ppc_cntlzwx(c,S,A,0)
#define ppc_cntlzwd(c,S,A) ppc_cntlzwx(c,S,A,1)

#define ppc_crand(c,D,A,B) ppc_emit32(c, (19 << 26) | (D << 21) | (A << 16) | (B << 11) | (257 << 1) | 0)
#define ppc_crandc(c,D,A,B) ppc_emit32(c, (19 << 26) | (D << 21) | (A << 16) | (B << 11) | (129 << 1) | 0)
#define ppc_creqv(c,D,A,B) ppc_emit32(c, (19 << 26) | (D << 21) | (A << 16) | (B << 11) | (289 << 1) | 0)
#define ppc_crnand(c,D,A,B) ppc_emit32(c, (19 << 26) | (D << 21) | (A << 16) | (B << 11) | (225 << 1) | 0)
#define ppc_crnor(c,D,A,B) ppc_emit32(c, (19 << 26) | (D << 21) | (A << 16) | (B << 11) | (33 << 1) | 0)
#define ppc_cror(c,D,A,B) ppc_emit32(c, (19 << 26) | (D << 21) | (A << 16) | (B << 11) | (449 << 1) | 0)
#define ppc_crorc(c,D,A,B) ppc_emit32(c, (19 << 26) | (D << 21) | (A << 16) | (B << 11) | (417 << 1) | 0)
#define ppc_crxor(c,D,A,B) ppc_emit32(c, (19 << 26) | (D << 21) | (A << 16) | (B << 11) | (193 << 1) | 0)

#define ppc_dcba(c,A,B) ppc_emit32(c, (31 << 26) | (0 << 21) | (A << 16) | (B << 11) | (758 << 1) | 0)
#define ppc_dcbf(c,A,B) ppc_emit32(c, (31 << 26) | (0 << 21) | (A << 16) | (B << 11) | (86 << 1) | 0)
#define ppc_dcbi(c,A,B) ppc_emit32(c, (31 << 26) | (0 << 21) | (A << 16) | (B << 11) | (470 << 1) | 0)
#define ppc_dcbst(c,A,B) ppc_emit32(c, (31 << 26) | (0 << 21) | (A << 16) | (B << 11) | (54 << 1) | 0)
#define ppc_dcbt(c,A,B) ppc_emit32(c, (31 << 26) | (0 << 21) | (A << 16) | (B << 11) | (278 << 1) | 0)
#define ppc_dcbtst(c,A,B) ppc_emit32(c, (31 << 26) | (0 << 21) | (A << 16) | (B << 11) | (246 << 1) | 0)
#define ppc_dcbz(c,A,B) ppc_emit32(c, (31 << 26) | (0 << 21) | (A << 16) | (B << 11) | (1014 << 1) | 0)

#define ppc_divwx(c,D,A,B,OE,Rc) ppc_emit32(c, (31 << 26) | (D << 21) | (A << 16) | (B << 11) | (OE << 10) | (491 << 1) | Rc)
#define ppc_divw(c,D,A,B) ppc_divwx(c,D,A,B,0,0)
#define ppc_divwd(c,D,A,B) ppc_divwx(c,D,A,B,0,1)
#define ppc_divwo(c,D,A,B) ppc_divwx(c,D,A,B,1,0)
#define ppc_divwod(c,D,A,B) ppc_divwx(c,D,A,B,1,1)

#define ppc_divwux(c,D,A,B,OE,Rc) ppc_emit32(c, (31 << 26) | (D << 21) | (A << 16) | (B << 11) | (OE << 10) | (459 << 1) | Rc)
#define ppc_divwu(c,D,A,B) ppc_divwux(c,D,A,B,0,0)
#define ppc_divwud(c,D,A,B) ppc_divwux(c,D,A,B,0,1)
#define ppc_divwuo(c,D,A,B) ppc_divwux(c,D,A,B,1,0)
#define ppc_divwuod(c,D,A,B) ppc_divwux(c,D,A,B,1,1)

#define ppc_eciwx(c,D,A,B) ppc_emit32(c, (31 << 26) | (D << 21) | (A << 16) | (B << 11) | (310 << 1) | 0)
#define ppc_ecowx(c,S,A,B) ppc_emit32(c, (31 << 26) | (S << 21) | (A << 16) | (B << 11) | (438 << 1) | 0)
#define ppc_eieio(c) ppc_emit32(c, (31 << 26) | (0 << 21) | (0 << 16) | (0 << 11) | (854 << 1) | 0)

#define ppc_eqvx(c,A,S,B,Rc) ppc_emit32(c, (31 << 26) | (S << 21) | (A << 16) | (B << 11) | (284 << 1) | Rc)
#define ppc_eqv(c,A,S,B) ppc_eqvx(c,A,S,B,0)
#define ppc_eqvd(c,A,S,B) ppc_eqvx(c,A,S,B,1)

#define ppc_extsbx(c,A,S,Rc) ppc_emit32(c, (31 << 26) | (S << 21) | (A << 16) | (0 << 11) | (954 << 1) | Rc)
#define ppc_extsb(c,A,S) ppc_extsbx(c,A,S,0)
#define ppc_extsbd(c,A,S) ppc_extsbx(c,A,S,1)

#define ppc_extshx(c,A,S,Rc) ppc_emit32(c, (31 << 26) | (S << 21) | (A << 16) | (0 << 11) | (922 << 1) | Rc)
#define ppc_extsh(c,A,S) ppc_extshx(c,A,S,0)
#define ppc_extshd(c,A,S) ppc_extshx(c,A,S,1)

#define ppc_fabsx(c,D,B,Rc) ppc_emit32(c, (63 << 26) | (D << 21) | (0 << 16) | (B << 11) | (264 << 1) | Rc)
#define ppc_fabs(c,D,B) ppc_fabsx(c,D,B,0)
#define ppc_fabsd(c,D,B) ppc_fabsx(c,D,B,1)

#define ppc_faddx(c,D,A,B,Rc) ppc_emit32(c, (63 << 26) | (D << 21) | (A << 16) | (B << 11) | (0 << 6) | (21 << 1) | Rc)
#define ppc_fadd(c,D,A,B) ppc_faddx(c,D,A,B,0)
#define ppc_faddd(c,D,A,B) ppc_faddx(c,D,A,B,1)

#define ppc_faddsx(c,D,A,B,Rc) ppc_emit32(c, (59 << 26) | (D << 21) | (A << 16) | (B << 11) | (0 << 6) | (21 << 1) | Rc)
#define ppc_fadds(c,D,A,B) ppc_faddsx(c,D,A,B,0)
#define ppc_faddsd(c,D,A,B) ppc_faddsx(c,D,A,B,1)

#define ppc_fcmpo(c,crfD,A,B) ppc_emit32(c, (63 << 26) | (crfD << 23) | (0 << 21) | (A << 16) | (B << 11) | (32 << 1) | 0)
#define ppc_fcmpu(c,crfD,A,B) ppc_emit32(c, (63 << 26) | (crfD << 23) | (0 << 21) | (A << 16) | (B << 11) | (0 << 1) | 0)

#define ppc_fctiwx(c,D,B,Rc) ppc_emit32(c, (63 << 26) | (D << 21) | (0 << 16) | (B << 11) | (14 << 1) | Rc)
#define ppc_fctiw(c,D,B) ppc_fctiwx(c,D,B,0)
#define ppc_fctiwd(c,D,B) ppc_fctiwx(c,D,B,1)

#define ppc_fctiwzx(c,D,B,Rc) ppc_emit32(c, (63 << 26) | (D << 21) | (0 << 16) | (B << 11) | (15 << 1) | Rc)
#define ppc_fctiwz(c,D,B) ppc_fctiwzx(c,D,B,0)
#define ppc_fctiwzd(c,D,B) ppc_fctiwzx(c,D,B,1)

#define ppc_fdivx(c,D,A,B,Rc) ppc_emit32(c, (63 << 26) | (D << 21) | (A << 16) | (B << 11) | (0 << 6) | (18 << 1) | Rc)
#define ppc_fdiv(c,D,A,B) ppc_fdivx(c,D,A,B,0)
#define ppc_fdivd(c,D,A,B) ppc_fdivx(c,D,A,B,1)

#define ppc_fdivsx(c,D,A,B,Rc) ppc_emit32(c, (59 << 26) | (D << 21) | (A << 16) | (B << 11) | (0 << 6) | (18 << 1) | Rc)
#define ppc_fdivs(c,D,A,B) ppc_fdivsx(c,D,A,B,0)
#define ppc_fdivsd(c,D,A,B) ppc_fdivsx(c,D,A,B,1)

#define ppc_fmaddx(c,D,A,B,C,Rc) ppc_emit32(c, (63 << 26) | (D << 21) | (A << 16) | (B << 11) | (C << 6) | (29 << 1) | Rc)
#define ppc_fmadd(c,D,A,B,C) ppc_fmaddx(c,D,A,B,C,0)
#define ppc_fmaddd(c,D,A,B,C) ppc_fmaddx(c,D,A,B,C,1)

#define ppc_fmaddsx(c,D,A,B,C,Rc) ppc_emit32(c, (59 << 26) | (D << 21) | (A << 16) | (B << 11) | (C << 6) | (29 << 1) | Rc)
#define ppc_fmadds(c,D,A,B,C) ppc_fmaddsx(c,D,A,B,C,0)
#define ppc_fmaddsd(c,D,A,B,C) ppc_fmaddsx(c,D,A,B,C,1)

#define ppc_fmrx(c,D,B,Rc) ppc_emit32(c, (63 << 26) | (D << 21) | (0 << 16) | (B << 11) | (72 << 1) | Rc)
#define ppc_fmr(c,D,B) ppc_fmrx(c,D,B,0)
#define ppc_fmrd(c,D,B) ppc_fmrx(c,D,B,1)

#define ppc_fmsubx(c,D,A,C,B,Rc) ppc_emit32(c, (63 << 26) | (D << 21) | (A << 16) | (B << 11) | (C << 6) | (28 << 1) | Rc)
#define ppc_fmsub(c,D,A,C,B) ppc_fmsubx(c,D,A,C,B,0)
#define ppc_fmsubd(c,D,A,C,B) ppc_fmsubx(c,D,A,C,B,1)

#define ppc_fmsubsx(c,D,A,C,B,Rc) ppc_emit32(c, (59 << 26) | (D << 21) | (A << 16) | (B << 11) | (C << 6) | (28 << 1) | Rc)
#define ppc_fmsubs(c,D,A,C,B) ppc_fmsubsx(c,D,A,C,B,0)
#define ppc_fmsubsd(c,D,A,C,B) ppc_fmsubsx(c,D,A,C,B,1)

#define ppc_fmulx(c,D,A,C,Rc) ppc_emit32(c, (63 << 26) | (D << 21) | (A << 16) | (0 << 11) | (C << 6) | (25 << 1) | Rc)
#define ppc_fmul(c,D,A,C) ppc_fmulx(c,D,A,C,0)
#define ppc_fmuld(c,D,A,C) ppc_fmulx(c,D,A,C,1)

#define ppc_fmulsx(c,D,A,C,Rc) ppc_emit32(c, (59 << 26) | (D << 21) | (A << 16) | (0 << 11) | (C << 6) | (25 << 1) | Rc)
#define ppc_fmuls(c,D,A,C) ppc_fmulsx(c,D,A,C,0)
#define ppc_fmulsd(c,D,A,C) ppc_fmulsx(c,D,A,C,1)

#define ppc_fnabsx(c,D,B,Rc) ppc_emit32(c, (63 << 26) | (D << 21) | (0 << 16) | (B << 11) | (136 << 1) | Rc)
#define ppc_fnabs(c,D,B) ppc_fnabsx(c,D,B,0)
#define ppc_fnabsd(c,D,B) ppc_fnabsx(c,D,B,1)

#define ppc_fnegx(c,D,B,Rc) ppc_emit32(c, (63 << 26) | (D << 21) | (0 << 16) | (B << 11) | (40 << 1) | Rc)
#define ppc_fneg(c,D,B) ppc_fnegx(c,D,B,0)
#define ppc_fnegd(c,D,B) ppc_fnegx(c,D,B,1)

#define ppc_fnmaddx(c,D,A,C,B,Rc) ppc_emit32(c, (63 << 26) | (D << 21) | (A << 16) | (B << 11) | (C << 6) | (31 << 1) | Rc)
#define ppc_fnmadd(c,D,A,C,B) ppc_fnmaddx(c,D,A,C,B,0)
#define ppc_fnmaddd(c,D,A,C,B) ppc_fnmaddx(c,D,A,C,B,1)

#define ppc_fnmaddsx(c,D,A,C,B,Rc) ppc_emit32(c, (59 << 26) | (D << 21) | (A << 16) | (B << 11) | (C << 6) | (31 << 1) | Rc)
#define ppc_fnmadds(c,D,A,C,B) ppc_fnmaddsx(c,D,A,C,B,0)
#define ppc_fnmaddsd(c,D,A,C,B) ppc_fnmaddsx(c,D,A,C,B,1)

#define ppc_fnmsubx(c,D,A,C,B,Rc) ppc_emit32(c, (63 << 26) | (D << 21) | (A << 16) | (B << 11) | (C << 6) | (30 << 1) | Rc)
#define ppc_fnmsub(c,D,A,C,B) ppc_fnmsubx(c,D,A,C,B,0)
#define ppc_fnmsubd(c,D,A,C,B) ppc_fnmsubx(c,D,A,C,B,1)

#define ppc_fnmsubsx(c,D,A,C,B,Rc) ppc_emit32(c, (59 << 26) | (D << 21) | (A << 16) | (B << 11) | (C << 6) | (30 << 1) | Rc)
#define ppc_fnmsubs(c,D,A,C,B) ppc_fnmsubsx(c,D,A,C,B,0)
#define ppc_fnmsubsd(c,D,A,C,B) ppc_fnmsubsx(c,D,A,C,B,1)

#define ppc_fresx(c,D,B,Rc) ppc_emit32(c, (59 << 26) | (D << 21) | (0 << 16) | (B << 11) | (0 << 6) | (24 << 1) | Rc)
#define ppc_fres(c,D,B) ppc_fresx(c,D,B,0)
#define ppc_fresd(c,D,B) ppc_fresx(c,D,B,1)

#define ppc_frspx(c,D,B,Rc) ppc_emit32(c, (63 << 26) | (D << 21) | (0 << 16) | (B << 11) | (12 << 1) | Rc)
#define ppc_frsp(c,D,B) ppc_frspx(c,D,B,0)
#define ppc_frspd(c,D,B) ppc_frspx(c,D,B,1)

#define ppc_frsqrtex(c,D,B,Rc) ppc_emit32(c, (63 << 26) | (D << 21) | (0 << 16) | (B << 11) | (0 << 6) | (26 << 1) | Rc)
#define ppc_frsqrte(c,D,B) ppc_frsqrtex(c,D,B,0)
#define ppc_frsqrted(c,D,B) ppc_frsqrtex(c,D,B,1)

#define ppc_fselx(c,D,A,C,B,Rc) ppc_emit32(c, (63 << 26) | (D << 21) | (A << 16) | (B << 11) | (C << 6) | (23 << 1) | Rc)
#define ppc_fsel(c,D,A,C,B) ppc_fselx(c,D,A,C,B,0)
#define ppc_fseld(c,D,A,C,B) ppc_fselx(c,D,A,C,B,1)

#define ppc_fsqrtx(c,D,B,Rc) ppc_emit32(c, (63 << 26) | (D << 21) | (0 << 16) | (B << 11) | (0 << 6) | (22 << 1) | Rc)
#define ppc_fsqrt(c,D,B) ppc_fsqrtx(c,D,B,0)
#define ppc_fsqrtd(c,D,B) ppc_fsqrtx(c,D,B,1)

#define ppc_fsqrtsx(c,D,B,Rc) ppc_emit32(c, (59 << 26) | (D << 21) | (0 << 16) | (B << 11) | (0 << 6) | (22 << 1) | Rc)
#define ppc_fsqrts(c,D,B) ppc_fsqrtsx(c,D,B,0)
#define ppc_fsqrtsd(c,D,B) ppc_fsqrtsx(c,D,B,1)

#define ppc_fsubx(c,D,A,B,Rc) ppc_emit32(c, (63 << 26) | (D << 21) | (A << 16) | (B << 11) | (0 << 6) | (20 << 1) | Rc)
#define ppc_fsub(c,D,A,B) ppc_fsubx(c,D,A,B,0)
#define ppc_fsubd(c,D,A,B) ppc_fsubx(c,D,A,B,1)

#define ppc_fsubsx(c,D,A,B,Rc) ppc_emit32(c, (59 << 26) | (D << 21) | (A << 16) | (B << 11) | (0 << 6) | (20 << 1) | Rc)
#define ppc_fsubs(c,D,A,B) ppc_fsubsx(c,D,A,B,0)
#define ppc_fsubsd(c,D,A,B) ppc_fsubsx(c,D,A,B,1)

#define ppc_icbi(c,A,B) ppc_emit32(c, (31 << 26) | (0 << 21) | (A << 16) | (B << 11) | (982 << 1) | 0)

#define ppc_isync(c) ppc_emit32(c, (19 << 26) | (0 << 11) | (150 << 1) | 0)

#define ppc_lbzu(c,D,d,A) ppc_emit32(c, (35 << 26) | (D << 21) | (A << 16) | (guint16)d)
#define ppc_lbzux(c,D,A,B) ppc_emit32(c, (31 << 26) | (D << 21) | (A << 16) | (B << 11) | (119 << 1) | 0)
#define ppc_lbzx(c,D,A,B) ppc_emit32(c, (31 << 26) | (D << 21) | (A << 16) | (B << 11) | (87 << 1) | 0)

#define ppc_lfdu(c,D,d,A) ppc_emit32(c, (51 << 26) | (D << 21) | (A << 16) | (guint16)d)
#define ppc_lfdux(c,D,A,B) ppc_emit32(c, (31 << 26) | (D << 21) | (A << 16) | (B << 11) | (631 << 1) | 0)
#define ppc_lfdx(c,D,A,B) ppc_emit32(c, (31 << 26) | (D << 21) | (A << 16) | (B << 11) | (599 << 1) | 0)

#define ppc_lfsu(c,D,d,A) ppc_emit32(c, (49 << 26) | (D << 21) | (A << 16) | (guint16)d)
#define ppc_lfsux(c,D,A,B) ppc_emit32(c, (31 << 26) | (D << 21) | (A << 16) | (B << 11) | (567 << 1) | 0)
#define ppc_lfsx(c,D,A,B) ppc_emit32(c, (31 << 26) | (D << 21) | (A << 16) | (B << 11) | (535 << 1) | 0)

#define ppc_lha(c,D,d,A) ppc_emit32(c, (42 << 26) | (D << 21) | (A << 16) | (guint16)d)
#define ppc_lhau(c,D,d,A) ppc_emit32(c, (43 << 26) | (D << 21) | (A << 16) | (guint16)d)
#define ppc_lhaux(c,D,A,B) ppc_emit32(c, (31 << 26) | (D << 21) | (A << 16) | (B << 11) | (375 << 1) | 0)
#define ppc_lhax(c,D,A,B) ppc_emit32(c, (31 << 26) | (D << 21) | (A << 16) | (B << 11) | (343 << 1) | 0)
#define ppc_lhbrx(c,D,A,B) ppc_emit32(c, (31 << 26) | (D << 21) | (A << 16) | (B << 11) | (790 << 1) | 0)
#define ppc_lhzu(c,D,d,A) ppc_emit32(c, (41 << 26) | (D << 21) | (A << 16) | (guint16)d)

#define ppc_lhzux(c,D,A,B) ppc_emit32(c, (31 << 26) | (D << 21) | (A << 16) | (B << 11) | (311 << 1) | 0)
#define ppc_lhzx(c,D,A,B) ppc_emit32(c, (31 << 26) | (D << 21) | (A << 16) | (B << 11) | (279 << 1) | 0)

#define ppc_lmw(c,D,d,A) ppc_emit32(c, (46 << 26) | (D << 21) | (A << 16) | (guint16)d)

#define ppc_lswi(c,D,A,NB) ppc_emit32(c, (31 << 26) | (D << 21) | (A << 16) | (NB << 11) | (597 << 1) | 0)
#define ppc_lswx(c,D,A,B) ppc_emit32(c, (31 << 26) | (D << 21) | (A << 16) | (B << 11) | (533 << 1) | 0)
#define ppc_lwarx(c,D,A,B) ppc_emit32(c, (31 << 26) | (D << 21) | (A << 16) | (B << 11) | (20 << 1) | 0)
#define ppc_lwbrx(c,D,A,B) ppc_emit32(c, (31 << 26) | (D << 21) | (A << 16) | (B << 11) | (534 << 1) | 0)

#define ppc_lwzu(c,D,d,A) ppc_emit32(c, (33 << 26) | (D << 21) | (A << 16) | (guint16)d)
#define ppc_lwzux(c,D,A,B) ppc_emit32(c, (31 << 26) | (D << 21) | (A << 16) | (B << 11) | (55 << 1) | 0)
#define ppc_lwzx(c,D,A,B) ppc_emit32(c, (31 << 26) | (D << 21) | (A << 16) | (B << 11) | (23 << 1) | 0)

#define ppc_mcrf(c,crfD,crfS) ppc_emit32(c, (19 << 26) | (crfD << 23) | (0 << 21) | (crfS << 18) | 0)
#define ppc_mcrfs(c,crfD,crfS) ppc_emit32(c, (63 << 26) | (crfD << 23) | (0 << 21) | (crfS << 18) | (0 << 16) | (64 << 1) | 0)
#define ppc_mcrxr(c,crfD) ppc_emit32(c, (31 << 26) | (crfD << 23) | (0 << 16) | (512 << 1) | 0)

#define ppc_mfcr(c,D) ppc_emit32(c, (31 << 26) | (D << 21) | (0 << 16) | (19 << 1) | 0)
#define ppc_mffsx(c,D,Rc) ppc_emit32(c, (63 << 26) | (D << 21) | (0 << 16) | (583 << 1) | Rc)
#define ppc_mffs(c,D) ppc_mffsx(c,D,0)
#define ppc_mffsd(c,D) ppc_mffsx(c,D,1)
#define ppc_mfmsr(c,D) ppc_emit32(c, (31 << 26) | (D << 21) | (0 << 16) | (83 << 1) | 0)
#define ppc_mfsr(c,D,SR) ppc_emit32(c, (31 << 26) | (D << 21) | (0 << 20) | (SR << 16) | (0 << 11) | (595 << 1) | 0)
#define ppc_mfsrin(c,D,B) ppc_emit32(c, (31 << 26) | (D << 21) | (0 << 16) | (B << 11) | (659 << 1) | 0)
#define ppc_mftb(c,D,TBR) ppc_emit32(c, (31 << 26) | (D << 21) | (TBR << 11) | (371 << 1) | 0)

#define ppc_mtcrf(c,CRM,S) ppc_emit32(c, (31 << 26) | (S << 21) | (0 << 20) | (CRM << 12) | (0 << 11) | (144 << 1) | 0)

#define ppc_mtfsb0x(c,CRB,Rc) ppc_emit32(c, (63 << 26) | (CRB << 21) | (0 << 11) | (70 << 1) | Rc)
#define ppc_mtfsb0(c,CRB) ppc_mtfsb0x(c,CRB,0)
#define ppc_mtfsb0d(c,CRB) ppc_mtfsb0x(c,CRB,1)

#define ppc_mtfsb1x(c,CRB,Rc) ppc_emit32(c, (63 << 26) | (CRB << 21) | (0 << 11) | (38 << 1) | Rc)
#define ppc_mtfsb1(c,CRB) ppc_mtfsb1x(c,CRB,0)
#define ppc_mtfsb1d(c,CRB) ppc_mtfsb1x(c,CRB,1)

#define ppc_mtfsfx(c,FM,B,Rc) ppc_emit32(c, (63 << 26) | (0 << 25) | (FM << 22) | (0 << 21) | (B << 11) | (711 << 1) | Rc)
#define ppc_mtfsf(c,FM,B) ppc_mtfsfx(c,FM,B,0)
#define ppc_mtfsfd(c,FM,B) ppc_mtfsfx(c,FM,B,1)

#define ppc_mtfsfix(c,crfD,IMM,Rc) ppc_emit32(c, (63 << 26) | (crfD << 23) | (0 << 16) | (IMM << 12) | (0 << 11) | (134 << 1) | Rc)
#define ppc_mtfsfi(c,crfD,IMM) ppc_mtfsfix(c,crfD,IMM,0)
#define ppc_mtfsfid(c,crfD,IMM) ppc_mtfsfix(c,crfD,IMM,1)

#define ppc_mtmsr(c, S) ppc_emit32(c, (31 << 26) | (S << 21) | (0 << 11) | (146 << 1) | 0)

#define ppc_mtsr(c,SR,S) ppc_emit32(c, (31 << 26) | (S << 21) | (0 << 20) | (SR << 16) | (0 << 11) | (210 << 1) | 0)
#define ppc_mtsrin(c,S,B) ppc_emit32(c, (31 << 26) | (S << 21) | (0 << 16) | (B << 11) | (242 << 1) | 0)

#define ppc_mulhwx(c,D,A,B,Rc) ppc_emit32(c, (31 << 26) | (D << 21) | (A << 16) | (B << 11) | (0 << 10) | (75 << 1) | Rc)
#define ppc_mulhw(c,D,A,B) ppc_mulhwx(c,D,A,B,0)
#define ppc_mulhwd(c,D,A,B) ppc_mulhwx(c,D,A,B,1)

#define ppc_mulhwux(c,D,A,B,Rc) ppc_emit32(c, (31 << 26) | (D << 21) | (A << 16) | (B << 11) | (0 << 10) | (11 << 1) | Rc)
#define ppc_mulhwu(c,D,A,B) ppc_mulhwux(c,D,A,B,0)
#define ppc_mulhwud(c,D,A,B) ppc_mulhwux(c,D,A,B,1)

#define ppc_mulli(c,D,A,SIMM) ppc_emit32(c, ((07) << 26) | (D << 21) | (A << 16) | (guint16)(SIMM))

#define ppc_mullwx(c,D,A,B,OE,Rc) ppc_emit32(c, (31 << 26) | (D << 21) | (A << 16) | (B << 11) | (OE << 10) | (235 << 1) | Rc)
#define ppc_mullw(c,D,A,B) ppc_mullwx(c,D,A,B,0,0)
#define ppc_mullwd(c,D,A,B) ppc_mullwx(c,D,A,B,0,1)
#define ppc_mullwo(c,D,A,B) ppc_mullwx(c,D,A,B,1,0)
#define ppc_mullwod(c,D,A,B) ppc_mullwx(c,D,A,B,1,1)

#define ppc_nandx(c,A,S,B,Rc) ppc_emit32(c, (31 << 26) | (S << 21) | (A << 16) | (B << 11) | (476 << 1) | Rc)
#define ppc_nand(c,A,S,B) ppc_nandx(c,A,S,B,0)
#define ppc_nandd(c,A,S,B) ppc_nandx(c,A,S,B,1)

#define ppc_negx(c,D,A,OE,Rc) ppc_emit32(c, (31 << 26) | (D << 21) | (A << 16) | (0 << 11) | (OE << 10) | (104 << 1) | Rc)
#define ppc_neg(c,D,A) ppc_negx(c,D,A,0,0)
#define ppc_negd(c,D,A) ppc_negx(c,D,A,0,1)
#define ppc_nego(c,D,A) ppc_negx(c,D,A,1,0)
#define ppc_negod(c,D,A) ppc_negx(c,D,A,1,1)

#define ppc_norx(c,A,S,B,Rc) ppc_emit32(c, (31 << 26) | (S << 21) | (A << 16) | (B << 11) | (124 << 1) | Rc)
#define ppc_nor(c,A,S,B) ppc_norx(c,A,S,B,0)
#define ppc_nord(c,A,S,B) ppc_norx(c,A,S,B,1)

#define ppc_not(c,A,S) ppc_norx(c,A,S,S,0)

#define ppc_orx(c,A,S,B,Rc) ppc_emit32(c, (31 << 26) | (S << 21) | (A << 16) | (B << 11) | (444 << 1) | Rc)
#define ppc_ord(c,A,S,B) ppc_orx(c,A,S,B,1)

#define ppc_orcx(c,A,S,B,Rc) ppc_emit32(c, (31 << 26) | (S << 21) | (A << 16) | (B << 11) | (412 << 1) | Rc)
#define ppc_orc(c,A,S,B) ppc_orcx(c,A,S,B,0)
#define ppc_orcd(c,A,S,B) ppc_orcx(c,A,S,B,1)

#define ppc_oris(c,A,S,UIMM) ppc_emit32(c, (25 << 26) | (S << 21) | (A << 16) | (guint16)(UIMM))

#define ppc_rfi(c) ppc_emit32(c, (19 << 26) | (0 << 11) | (50 << 1) | 0)

#define ppc_rlwimix(c,A,S,SH,MB,ME,Rc) ppc_emit32(c, (20 << 26) | (S << 21) | (A << 16) | (SH << 11) | (MB << 6) | (ME << 1) | Rc)
#define ppc_rlwimi(c,A,S,SH,MB,ME) ppc_rlwimix(c,A,S,SH,MB,ME,0)
#define ppc_rlwimid(c,A,S,SH,MB,ME) ppc_rlwimix(c,A,S,SH,MB,ME,1)

#define ppc_rlwinmx(c,A,S,SH,MB,ME,Rc) ppc_emit32(c, (21 << 26) | ((S) << 21) | ((A) << 16) | ((SH) << 11) | ((MB) << 6) | ((ME) << 1) | (Rc))
#define ppc_rlwinm(c,A,S,SH,MB,ME) ppc_rlwinmx(c,A,S,SH,MB,ME,0)
#define ppc_rlwinmd(c,A,S,SH,MB,ME) ppc_rlwinmx(c,A,S,SH,MB,ME,1)
#define ppc_extlwi(c,A,S,n,b) ppc_rlwinm(c,A,S, b, 0, (n) - 1)
#define ppc_extrwi(c,A,S,n,b) ppc_rlwinm(c,A,S, (b) + (n), 32 - (n), 31)
#define ppc_rotlwi(c,A,S,n) ppc_rlwinm(c,A,S, n, 0, 31)
#define ppc_rotrwi(c,A,S,n) ppc_rlwinm(c,A,S, 32 - (n), 0, 31)
#define ppc_slwi(c,A,S,n) ppc_rlwinm(c,A,S, n, 0, 31 - (n))
#define ppc_srwi(c,A,S,n) ppc_rlwinm(c,A,S, 32 - (n), n, 31)
#define ppc_clrlwi(c,A,S,n) ppc_rlwinm(c,A,S, 0, n, 31)
#define ppc_clrrwi(c,A,S,n) ppc_rlwinm(c,A,S, 0, 0, 31 - (n))
#define ppc_clrlslwi(c,A,S,b,n) ppc_rlwinm(c,A,S, n, (b) - (n), 31 - (n))

#define ppc_rlwnmx(c,A,S,SH,MB,ME,Rc) ppc_emit32(c, (23 << 26) | (S << 21) | (A << 16) | (SH << 11) | (MB << 6) | (ME << 1) | Rc)
#define ppc_rlwnm(c,A,S,SH,MB,ME) ppc_rlwnmx(c,A,S,SH,MB,ME,0)
#define ppc_rlwnmd(c,A,S,SH,MB,ME) ppc_rlwnmx(c,A,S,SH,MB,ME,1)

#define ppc_sc(c) ppc_emit32(c, (17 << 26) | (0 << 2) | (1 << 1) | 0)

#define ppc_slwx(c,S,A,B,Rc) ppc_emit32(c, (31 << 26) | (S << 21) | (A << 16) | (B << 11) | (24 << 1) | Rc)
#define ppc_slw(c,S,A,B) ppc_slwx(c,S,A,B,0)
#define ppc_slwd(c,S,A,B) ppc_slwx(c,S,A,B,1)

#define ppc_srawx(c,A,S,B,Rc) ppc_emit32(c, (31 << 26) | (S << 21) | (A << 16) | (B << 11) | (792 << 1) | Rc)
#define ppc_sraw(c,A,S,B) ppc_srawx(c,A,S,B,0)
#define ppc_srawd(c,A,S,B) ppc_srawx(c,A,S,B,1)

#define ppc_srawix(c,A,S,SH,Rc) ppc_emit32(c, (31 << 26) | (S << 21) | (A << 16) | (SH << 11) | (824 << 1) | Rc)
#define ppc_srawi(c,A,S,B) ppc_srawix(c,A,S,B,0)
#define ppc_srawid(c,A,S,B) ppc_srawix(c,A,S,B,1)

#define ppc_srwx(c,A,S,SH,Rc) ppc_emit32(c, (31 << 26) | (S << 21) | (A << 16) | (SH << 11) | (536 << 1) | Rc)
#define ppc_srw(c,A,S,B) ppc_srwx(c,A,S,B,0)
#define ppc_srwd(c,A,S,B) ppc_srwx(c,A,S,B,1)

#define ppc_stbu(c,S,d,A) ppc_emit32(c, (39 << 26) | (S << 21) | (A << 16) | (guint16)(d))

#define ppc_stbux(c,S,A,B) ppc_emit32(c, (31 << 26) | (S << 21) | (A << 16) | (B << 11) | (247 << 1) | 0)
#define ppc_stbx(c,S,A,B) ppc_emit32(c, (31 << 26) | (S << 21) | (A << 16) | (B << 11) | (215 << 1) | 0)

#define ppc_stfdu(c,S,d,A) ppc_emit32(c, (55 << 26) | (S << 21) | (A << 16) | (guint16)(d))

#define ppc_stfdx(c,S,A,B) ppc_emit32(c, (31 << 26) | (S << 21) | (A << 16) | (B << 11) | (727 << 1) | 0)
#define ppc_stfiwx(c,S,A,B) ppc_emit32(c, (31 << 26) | (S << 21) | (A << 16) | (B << 11) | (983 << 1) | 0)

#define ppc_stfsu(c,S,d,A) ppc_emit32(c, (53 << 26) | (S << 21) | (A << 16) | (guint16)(d))
#define ppc_stfsux(c,S,A,B) ppc_emit32(c, (31 << 26) | (S << 21) | (A << 16) | (B << 11) | (695 << 1) | 0)
#define ppc_stfsx(c,S,A,B) ppc_emit32(c, (31 << 26) | (S << 21) | (A << 16) | (B << 11) | (663 << 1) | 0)
#define ppc_sthbrx(c,S,A,B) ppc_emit32(c, (31 << 26) | (S << 21) | (A << 16) | (B << 11) | (918 << 1) | 0)
#define ppc_sthu(c,S,d,A) ppc_emit32(c, (45 << 26) | (S << 21) | (A << 16) | (guint16)(d))
#define ppc_sthux(c,S,A,B) ppc_emit32(c, (31 << 26) | (S << 21) | (A << 16) | (B << 11) | (439 << 1) | 0)
#define ppc_sthx(c,S,A,B) ppc_emit32(c, (31 << 26) | (S << 21) | (A << 16) | (B << 11) | (407 << 1) | 0)
#define ppc_stmw(c,S,d,A) ppc_emit32(c, (47 << 26) | (S << 21) | (A << 16) | (guint16)d)
#define ppc_stswi(c,S,A,NB) ppc_emit32(c, (31 << 26) | (S << 21) | (A << 16) | (NB << 11) | (725 << 1) | 0)
#define ppc_stswx(c,S,A,NB) ppc_emit32(c, (31 << 26) | (S << 21) | (A << 16) | (NB << 11) | (661 << 1) | 0)
#define ppc_stwbrx(c,S,A,B) ppc_emit32(c, (31 << 26) | (S << 21) | (A << 16) | (B << 11) | (662 << 1) | 0)
#define ppc_stwcxd(c,S,A,B) ppc_emit32(c, (31 << 26) | (S << 21) | (A << 16) | (B << 11) | (150 << 1) | 1)
#define ppc_stwux(c,S,A,B) ppc_emit32(c, (31 << 26) | (S << 21) | (A << 16) | (B << 11) | (183 << 1) | 0)
#define ppc_stwx(c,S,A,B) ppc_emit32(c, (31 << 26) | (S << 21) | (A << 16) | (B << 11) | (151 << 1) | 0)

#define ppc_subfx(c,D,A,B,OE,Rc) ppc_emit32(c, (31 << 26) | (D << 21) | (A << 16) | (B << 11) | (OE << 10) | (40 << 1) | Rc)
#define ppc_subf(c,D,A,B) ppc_subfx(c,D,A,B,0,0)
#define ppc_subfd(c,D,A,B) ppc_subfx(c,D,A,B,0,1)
#define ppc_subfo(c,D,A,B) ppc_subfx(c,D,A,B,1,0)
#define ppc_subfod(c,D,A,B) ppc_subfx(c,D,A,B,1,1)

#define ppc_sub(c,D,A,B) ppc_subf(c,D,B,A)

#define ppc_subfcx(c,D,A,B,OE,Rc) ppc_emit32(c, (31 << 26) | (D << 21) | (A << 16) | (B << 11) | (OE << 10) | (8 << 1) | Rc)
#define ppc_subfc(c,D,A,B) ppc_subfcx(c,D,A,B,0,0)
#define ppc_subfcd(c,D,A,B) ppc_subfcx(c,D,A,B,0,1)
#define ppc_subfco(c,D,A,B) ppc_subfcx(c,D,A,B,1,0)
#define ppc_subfcod(c,D,A,B) ppc_subfcx(c,D,A,B,1,1)

#define ppc_subfex(c,D,A,B,OE,Rc) ppc_emit32(c, (31 << 26) | (D << 21) | (A << 16) | (B << 11) | (OE << 10) | (136 << 1) | Rc)
#define ppc_subfe(c,D,A,B) ppc_subfex(c,D,A,B,0,0)
#define ppc_subfed(c,D,A,B) ppc_subfex(c,D,A,B,0,1)
#define ppc_subfeo(c,D,A,B) ppc_subfex(c,D,A,B,1,0)
#define ppc_subfeod(c,D,A,B) ppc_subfex(c,D,A,B,1,1)

#define ppc_subfic(c,D,A,SIMM) ppc_emit32(c, (8 << 26) | (D << 21) | (A << 16) | (guint16)(SIMM))

#define ppc_subfmex(c,D,A,OE,Rc) ppc_emit32(c, (31 << 26) | (D << 21) | (A << 16) | (0 << 11) | (OE << 10) | (232 << 1) | Rc)
#define ppc_subfme(c,D,A) ppc_subfmex(c,D,A,0,0)
#define ppc_subfmed(c,D,A) ppc_subfmex(c,D,A,0,1)
#define ppc_subfmeo(c,D,A) ppc_subfmex(c,D,A,1,0)
#define ppc_subfmeod(c,D,A) ppc_subfmex(c,D,A,1,1)

#define ppc_subfzex(c,D,A,OE,Rc) ppc_emit32(c, (31 << 26) | (D << 21) | (A << 16) | (0 << 11) | (OE << 10) | (200 << 1) | Rc)
#define ppc_subfze(c,D,A) ppc_subfzex(c,D,A,0,0)
#define ppc_subfzed(c,D,A) ppc_subfzex(c,D,A,0,1)
#define ppc_subfzeo(c,D,A) ppc_subfzex(c,D,A,1,0)
#define ppc_subfzeod(c,D,A) ppc_subfzex(c,D,A,1,1)

#define ppc_sync(c) ppc_emit32(c, (31 << 26) | (0 << 11) | (598 << 1) | 0)
#define ppc_tlbia(c) ppc_emit32(c, (31 << 26) | (0 << 11) | (370 << 1) | 0)
#define ppc_tlbie(c,B) ppc_emit32(c, (31 << 26) | (0 << 16) | (B << 11) | (306 << 1) | 0)
#define ppc_tlbsync(c) ppc_emit32(c, (31 << 26) | (0 << 11) | (566 << 1) | 0)

#define ppc_tw(c,TO,A,B) ppc_emit32(c, (31 << 26) | (TO << 21) | (A << 16) | (B << 11) | (4 << 1) | 0)
#define ppc_twi(c,TO,A,SIMM) ppc_emit32(c, (3 << 26) | (TO << 21) | (A << 16) | (guint16)(SIMM))

#define ppc_xorx(c,A,S,B,RC) ppc_emit32(c, (31 << 26) | (S << 21) | (A << 16) | (B << 11) | (316 << 1) | RC)
#define ppc_xor(c,A,S,B) ppc_xorx(c,A,S,B,0)
#define ppc_xord(c,A,S,B) ppc_xorx(c,A,S,B,1)

#define ppc_xori(c,S,A,UIMM) ppc_emit32(c, (26 << 26) | (S << 21) | (A << 16) | (guint16)(UIMM))
#define ppc_xoris(c,S,A,UIMM) ppc_emit32(c, (27 << 26) | (S << 21) | (A << 16) | (guint16)(UIMM))

/* this marks the end of my work, ct */

/* Introduced in Power ISA 2.02 (P4?) */
#define ppc_frinx(c,D,B,Rc) ppc_emit32(c, (63 << 26) | (D << 21) | (0 << 16) | (B << 11) | (392 << 1) | Rc)
#define ppc_frin(c,D,B) ppc_frinx(c,D,B,0)
#define ppc_frind(c,D,B) ppc_frinx(c,D,B,1)

#define ppc_fripx(c,D,B,Rc) ppc_emit32(c, (63 << 26) | (D << 21) | (0 << 16) | (B << 11) | (456 << 1) | Rc)
#define ppc_frip(c,D,B) ppc_fripx(c,D,B,0)
#define ppc_fripd(c,D,B) ppc_fripx(c,D,B,1)

#define ppc_frizx(c,D,B,Rc) ppc_emit32(c, (63 << 26) | (D << 21) | (0 << 16) | (B << 11) | (424 << 1) | Rc)
#define ppc_friz(c,D,B) ppc_frizx(c,D,B,0)
#define ppc_frizd(c,D,B) ppc_frizx(c,D,B,1)

#define ppc_frimx(c,D,B,Rc) ppc_emit32(c, (63 << 26) | (D << 21) | (0 << 16) | (B << 11) | (488 << 1) | Rc)
#define ppc_frim(c,D,B) ppc_frimx(c,D,B,0)
#define ppc_frimd(c,D,B) ppc_frimx(c,D,B,1)

/*
 * Introduced in Power ISA 2.03 (P5)
 * This is an A-form instruction like many of the FP arith ops,
 * but arranged slightly differently (swap record and reserved area)
 */
#define ppc_isel(c,D,A,B,C) ppc_emit32(c, (31 << 26) | (D << 21) | (A << 16) | (B << 11) | (C << 6) | (15 << 1) | 0)
#define ppc_isellt(c,D,A,B) ppc_isel(c,D,A,B,0)
#define ppc_iselgt(c,D,A,B) ppc_isel(c,D,A,B,1)
#define ppc_iseleq(c,D,A,B) ppc_isel(c,D,A,B,2)

/* PPC64 */

/* The following FP instructions are not are available to 32-bit
   implementations (prior to PowerISA-V2.01 but are available to
   32-bit mode programs on 64-bit PowerPC implementations and all
   processors compliant with PowerISA-2.01 or later.  */

#define ppc_fcfidx(c,D,B,Rc) ppc_emit32(c, (63 << 26) | ((D) << 21) | (0 << 16) | ((B) << 11) | (846 << 1) | (Rc))
#define ppc_fcfid(c,D,B)  ppc_fcfidx(c,D,B,0)
#define ppc_fcfidd(c,D,B) ppc_fcfidx(c,D,B,1)

#define ppc_fctidx(c,D,B,Rc) ppc_emit32(c, (63 << 26) | ((D) << 21) | (0 << 16) | ((B) << 11) | (814 << 1) | (Rc))
#define ppc_fctid(c,D,B)  ppc_fctidx(c,D,B,0)
#define ppc_fctidd(c,D,B) ppc_fctidx(c,D,B,1)

#define ppc_fctidzx(c,D,B,Rc) ppc_emit32(c, (63 << 26) | ((D) << 21) | (0 << 16) | ((B) << 11) | (815 << 1) | (Rc))
#define ppc_fctidz(c,D,B)  ppc_fctidzx(c,D,B,0)
#define ppc_fctidzd(c,D,B) ppc_fctidzx(c,D,B,1)

#ifdef TARGET_POWERPC64

#define ppc_load_sequence(c,D,v) G_STMT_START {	\
		ppc_lis  ((c), (D),      ((guint64)(v) >> 48) & 0xffff);	\
		ppc_ori  ((c), (D), (D), ((guint64)(v) >> 32) & 0xffff);	\
		ppc_sldi ((c), (D), (D), 32); \
		ppc_oris ((c), (D), (D), ((guint64)(v) >> 16) & 0xffff);	\
		ppc_ori  ((c), (D), (D),  (guint64)(v)        & 0xffff);	\
	} G_STMT_END

#define PPC_LOAD_SEQUENCE_LENGTH	20

#define ppc_is_imm32(val) (((((gint64)val)>> 31) == 0) || ((((gint64)val)>> 31) == -1))
#define ppc_is_imm48(val) (((((gint64)val)>> 47) == 0) || ((((gint64)val)>> 47) == -1))

#define ppc_load48(c,D,v) G_STMT_START {	\
		ppc_li   ((c), (D), ((gint64)(v) >> 32) & 0xffff);	\
		ppc_sldi ((c), (D), (D), 32); \
		ppc_oris ((c), (D), (D), ((guint64)(v) >> 16) & 0xffff);	\
		ppc_ori  ((c), (D), (D),  (guint64)(v)        & 0xffff);	\
	} G_STMT_END

#define ppc_load(c,D,v) G_STMT_START {	\
		if (ppc_is_imm16 ((guint64)(v)))	{	\
			ppc_li ((c), (D), (guint16)(guint64)(v));	\
		} else if (ppc_is_imm32 ((guint64)(v))) {	\
			ppc_load32 ((c), (D), (guint32)(guint64)(v)); \
		} else if (ppc_is_imm48 ((guint64)(v))) {	\
			ppc_load48 ((c), (D), (guint64)(v)); \
		} else {	\
			ppc_load_sequence ((c), (D), (guint64)(v)); \
		}	\
	} G_STMT_END

#if _CALL_ELF == 2
#define ppc_load_func(c,D,V)	      ppc_load_sequence ((c), (D), (V))
#else
#define ppc_load_func(c,D,v) G_STMT_START { \
		ppc_load_sequence ((c), ppc_r12, (guint64)(gsize)(v));	\
		ppc_ldptr ((c), ppc_r2, sizeof (gpointer), ppc_r12);	\
		ppc_ldptr ((c), (D), 0, ppc_r12);	\
	} G_STMT_END
#endif

#define ppc_load_multiple_regs(c,D,d,A) G_STMT_START { \
		int __i, __o = (d);			\
		for (__i = (D); __i <= 31; ++__i) {	\
			ppc_ldr ((c), __i, __o, (A));		\
			__o += sizeof (guint64);				\
		} \
	} G_STMT_END

#define ppc_store_multiple_regs(c,S,d,A) G_STMT_START { \
		int __i, __o = (d);			\
		for (__i = (S); __i <= 31; ++__i) {	\
			ppc_str ((c), __i, __o, (A));		\
			__o += sizeof (guint64);				\
		} \
	} G_STMT_END

#define ppc_compare(c,cfrD,A,B)		      ppc_cmp((c), (cfrD), 1, (A), (B))
#define ppc_compare_reg_imm(c,cfrD,A,B)	      ppc_cmpi((c), (cfrD), 1, (A), (B))
#define ppc_compare_log(c,cfrD,A,B)	      ppc_cmpl((c), (cfrD), 1, (A), (B))

#define ppc_shift_left(c,A,S,B)		      ppc_sld((c), (A), (S), (B))
#define ppc_shift_left_imm(c,A,S,n)	      ppc_sldi((c), (A), (S), (n))

#define ppc_shift_right_imm(c,A,S,B)	      ppc_srdi((c), (A), (S), (B))
#define ppc_shift_right_arith_imm(c,A,S,B)    ppc_sradi((c), (A), (S), (B))

#define ppc_multiply(c,D,A,B)		      ppc_mulld((c), (D), (A), (B))

#define ppc_clear_right_imm(c,A,S,n)	      ppc_clrrdi((c), (A), (S), (n))

#define ppc_divdx(c,D,A,B,OE,Rc) ppc_emit32(c, (31 << 26) | ((D) << 21) | ((A) << 16) | ((B) << 11) | ((OE) << 10) | (489 << 1) | (Rc))
#define ppc_divd(c,D,A,B)   ppc_divdx(c,D,A,B,0,0)
#define ppc_divdd(c,D,A,B)  ppc_divdx(c,D,A,B,0,1)
#define ppc_divdo(c,D,A,B)  ppc_divdx(c,D,A,B,1,0)
#define ppc_divdod(c,D,A,B) ppc_divdx(c,D,A,B,1,1)

#define ppc_divdux(c,D,A,B,OE,Rc) ppc_emit32(c, (31 << 26) | ((D) << 21) | ((A) << 16) | ((B) << 11) | ((OE) << 10) | (457 << 1) | (Rc))
#define ppc_divdu(c,D,A,B)   ppc_divdux(c,D,A,B,0,0)
#define ppc_divdud(c,D,A,B)  ppc_divdux(c,D,A,B,0,1)
#define ppc_divduo(c,D,A,B)  ppc_divdux(c,D,A,B,1,0)
#define ppc_divduod(c,D,A,B) ppc_divdux(c,D,A,B,1,1)

#define ppc_extswx(c,S,A,Rc) ppc_emit32(c, (31 << 26) | ((S) << 21) | ((A) << 16) | (0 << 11) | (986 << 1) | (Rc))
#define ppc_extsw(c,A,S)  ppc_extswx(c,S,A,0)
#define ppc_extswd(c,A,S) ppc_extswx(c,S,A,1)

/* These move float to/from instuctions are only available on POWER6 in
   native mode.  These instruction are faster then the equivalent
   store/load because they avoid the store queue and associated delays.
   These instructions should only be used in 64-bit mode unless the
   kernel preserves the 64-bit GPR on signals and dispatch in 32-bit
   mode.  The Linux kernel does not.  */
#define ppc_mftgpr(c,T,B) ppc_emit32(c, (31 << 26) | ((T) << 21) | (0 << 16) | ((B) << 11) | (735 << 1) | 0)
#define ppc_mffgpr(c,T,B) ppc_emit32(c, (31 << 26) | ((T) << 21) | (0 << 16) | ((B) << 11) | (607 << 1) | 0)

#define ppc_ld(c,D,ds,A) ppc_emit32(c, (58 << 26) | ((D) << 21) | ((A) << 16) | ((guint32)(ds) & 0xfffc) | 0)
#define ppc_lwa(c,D,ds,A) ppc_emit32(c, (58 << 26) | ((D) << 21) | ((A) << 16) | ((ds) & 0xfffc) | 2)
#define ppc_ldarx(c,D,A,B) ppc_emit32(c, (31 << 26) | ((D) << 21) | ((A) << 16) | ((B) << 11) | (84 << 1) | 0)
#define ppc_ldu(c,D,ds,A) ppc_emit32(c, (58 <<	26) | ((D) << 21) | ((A) << 16) | ((guint32)(ds) & 0xfffc) | 1)
#define ppc_ldux(c,D,A,B) ppc_emit32(c, (31 << 26) | ((D) << 21) | ((A) << 16) | ((B) << 11) | (53 << 1) | 0)
#define ppc_lwaux(c,D,A,B) ppc_emit32(c, (31 << 26) | ((D) << 21) | ((A) << 16) | ((B) << 11) | (373 << 1) | 0)
#define ppc_ldx(c,D,A,B) ppc_emit32(c, (31 << 26) | ((D) << 21) | ((A) << 16) | ((B) << 11) | (21 << 1) | 0)
#define ppc_lwax(c,D,A,B) ppc_emit32(c, (31 << 26) | ((D) << 21) | ((A) << 16) | ((B) << 11) | (341 << 1) | 0)

#define ppc_mulhdx(c,D,A,B,Rc) ppc_emit32(c, (31 << 26) | ((D) << 21) | ((A) << 16) | ((B) << 11) | (0 << 10) | (73 << 1) | (Rc))
#define ppc_mulhd(c,D,A,B)  ppc_mulhdx(c,D,A,B,0)
#define ppc_mulhdd(c,D,A,B) ppc_mulhdx(c,D,A,B,1)
#define ppc_mulhdux(c,D,A,B,Rc) ppc_emit32(c, (31 << 26) | ((D) << 21) | ((A) << 16) | ((B) << 11) | (0 << 10) | (9 << 1) | (Rc))
#define ppc_mulhdu(c,D,A,B)  ppc_mulhdux(c,D,A,B,0)
#define ppc_mulhdud(c,D,A,B) ppc_mulhdux(c,D,A,B,1)

#define ppc_mulldx(c,D,A,B,OE,Rc) ppc_emit32(c, (31 << 26) | ((D) << 21) | ((A) << 16) | ((B) << 11) | ((OE) << 10) | (233 << 1) | (Rc))
#define ppc_mulld(c,D,A,B)   ppc_mulldx(c,D,A,B,0,0)
#define ppc_mulldd(c,D,A,B)  ppc_mulldx(c,D,A,B,0,1)
#define ppc_mulldo(c,D,A,B)  ppc_mulldx(c,D,A,B,1,0)
#define ppc_mulldod(c,D,A,B) ppc_mulldx(c,D,A,B,1,1)

#define ppc_rldclx(c,A,S,B,MB,Rc) ppc_emit32(c, (30 << 26) | ((S) << 21) | ((A) << 16) | ((B) << 11) | (ppc_split_5_1(MB) << 5) | (8 << 1) | (Rc))
#define ppc_rldcl(c,A,S,B,MB)  ppc_rldclx(c,A,S,B,MB,0)
#define ppc_rldcld(c,A,S,B,MB) ppc_rldclx(c,A,S,B,MB,1)
#define ppc_rotld(c,A,S,B) ppc_rldcl(c, A, S, B, 0)

#define ppc_rldcrx(c,A,S,B,ME,Rc) ppc_emit32(c, (30 << 26) | ((S) << 21) | ((A) << 16) | ((B) << 11) | (ppc_split_5_1(ME) << 5) | (9 << 1) | (Rc))
#define ppc_rldcr(c,A,S,B,ME)  ppc_rldcrx(c,A,S,B,ME,0)
#define ppc_rldcrd(c,A,S,B,ME) ppc_rldcrx(c,A,S,B,ME,1)

#define ppc_rldicx(c,S,A,SH,MB,Rc) ppc_emit32(c, (30 << 26) | ((S) << 21) | ((A) << 16) | (ppc_split_5_1_5(SH) << 11) | (ppc_split_5_1(MB) << 5) | (2 << 2) | (ppc_split_5_1_1(SH) << 1) | (Rc))
#define ppc_rldic(c,A,S,SH,MB)  ppc_rldicx(c,S,A,SH,MB,0)
#define ppc_rldicd(c,A,S,SH,MB) ppc_rldicx(c,S,A,SH,MB,1)

#define ppc_rldiclx(c,S,A,SH,MB,Rc) ppc_emit32(c, (30 << 26) | ((S) << 21) | ((A) << 16) | (ppc_split_5_1_5(SH) << 11) | (ppc_split_5_1(MB) << 5) | (0 << 2) | (ppc_split_5_1_1(SH) << 1) | (Rc))
#define ppc_rldicl(c,A,S,SH,MB)  ppc_rldiclx(c,S,A,SH,MB,0)
#define ppc_rldicld(c,A,S,SH,MB) ppc_rldiclx(c,S,A,SH,MB,1)
#define ppc_extrdi(c,A,S,n,b) ppc_rldicl(c,A,S, (b) + (n), 64 - (n))
#define ppc_rotldi(c,A,S,n)   ppc_rldicl(c,A,S, n, 0)
#define ppc_rotrdi(c,A,S,n)   ppc_rldicl(c,A,S, 64 - (n), 0)
#define ppc_srdi(c,A,S,n)     ppc_rldicl(c,A,S, 64 - (n), n)
#define ppc_clrldi(c,A,S,n)   ppc_rldicl(c,A,S, 0, n)

#define ppc_rldicrx(c,A,S,SH,ME,Rc) ppc_emit32(c, (30 << 26) | ((S) << 21) | ((A) << 16) | (ppc_split_5_1_5(SH) << 11) | (ppc_split_5_1(ME) << 5) | (1 << 2) | (ppc_split_5_1_1(SH) << 1) | (Rc))
#define ppc_rldicr(c,A,S,SH,ME)  ppc_rldicrx(c,A,S,SH,ME,0)
#define ppc_rldicrd(c,A,S,SH,ME) ppc_rldicrx(c,A,S,SH,ME,1)
#define ppc_extldi(c,A,S,n,b) ppc_rldicr(c, A, S, b, (n) - 1)
#define ppc_sldi(c,A,S,n)     ppc_rldicr(c, A, S, n, 63 - (n))
#define ppc_clrrdi(c,A,S,n)   ppc_rldicr(c, A, S, 0, 63 - (n))

#define ppc_rldimix(c,S,A,SH,MB,Rc) ppc_emit32(c, (30 << 26) | ((S) << 21) | ((A) << 16) | (ppc_split_5_1_5(SH) << 11) | (ppc_split_5_1(MB) << 5) | (3 << 2) | (ppc_split_5_1_1(SH) << 1) | (Rc))
#define ppc_rldimi(c,A,S,SH,MB)  ppc_rldimix(c,S,A,SH,MB,0)
#define ppc_rldimid(c,A,S,SH,MB) ppc_rldimix(c,S,A,SH,MB,1)

#define ppc_slbia(c)  ppc_emit32(c, (31 << 26) | (0 << 21) | (0 << 16) | (0 << 11) | (498 << 1) | 0)
#define ppc_slbie(c,B) ppc_emit32(c, (31 << 26) | (0 << 21) | (0 << 16) | ((B) << 11) | (434 << 1) | 0)
#define ppc_sldx(c,S,A,B,Rc) ppc_emit32(c, (31 << 26) | ((S) << 21) | ((A) << 16) | ((B) << 11) | (27 << 1) | (Rc))
#define ppc_sld(c,A,S,B)  ppc_sldx(c,S,A,B,0)
#define ppc_sldd(c,A,S,B) ppc_sldx(c,S,A,B,1)

#define ppc_sradx(c,S,A,B,Rc) ppc_emit32(c, (31 << 26) | ((S) << 21) | ((A) << 16) | ((B) << 11) | (794 << 1) | (Rc))
#define ppc_srad(c,A,S,B)  ppc_sradx(c,S,A,B,0)
#define ppc_sradd(c,A,S,B) ppc_sradx(c,S,A,B,1)
#define ppc_sradix(c,S,A,SH,Rc) ppc_emit32(c, (31 << 26) | ((S) << 21) | ((A) << 16) | (((SH) & 31) << 11) | (413 << 2) | (((SH) >> 5) << 1) | (Rc))
#define ppc_sradi(c,A,S,SH)  ppc_sradix(c,S,A,SH,0)
#define ppc_sradid(c,A,S,SH) ppc_sradix(c,S,A,SH,1)

#define ppc_srdx(c,S,A,B,Rc) ppc_emit32(c, (31 << 26) | ((S) << 21) | ((A) << 16) | ((B) << 11) | (539 << 1) | (Rc))
#define ppc_srd(c,A,S,B)  ppc_srdx(c,S,A,B,0)
#define ppc_srdd(c,A,S,B) ppc_srdx(c,S,A,B,1)

#define ppc_std(c,S,ds,A)   ppc_emit32(c, (62 << 26) | ((S) << 21) | ((A) << 16) | ((guint32)(ds) & 0xfffc) | 0)
#define ppc_stdcxd(c,S,A,B) ppc_emit32(c, (31 << 26) | ((S) << 21) | ((A) << 16) | ((B) << 11) | (214 << 1) | 1)
#define ppc_stdu(c,S,ds,A)  ppc_emit32(c, (62 << 26) | ((S) << 21) | ((A) << 16) | ((guint32)(ds) & 0xfffc) | 1)
#define ppc_stdux(c,S,A,B)  ppc_emit32(c, (31 << 26) | ((S) << 21) | ((A) << 16) | ((B) << 11) | (181 << 1) | 0)
#define ppc_stdx(c,S,A,B)   ppc_emit32(c, (31 << 26) | ((S) << 21) | ((A) << 16) | ((B) << 11) | (149 << 1) | 0)

#else
/* Always true for 32-bit */
#define ppc_is_imm32(val) (1)
#endif

#endif
