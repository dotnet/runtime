/*
   Copyright (C)  2001 Radek Doulik
*/

#ifndef PPC_H
#define PPC_H
#include <glib.h>
#include <assert.h>

typedef enum {
	ppc_r0 = 0,
	ppc_r1,
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
} PPCSpecialRegister;

#define ppc_emit32(c,x) *((guint32 *) c) = x; ((guint32 *)c)++

#define  ppc_addi(c,D,A,d) ppc_emit32 (c, (14 << 26) | ((D) << 21) | ((A) << 16) | (guint16)(d))
#define ppc_addis(c,D,A,d) ppc_emit32 (c, (15 << 26) | ((D) << 21) | ((A) << 16) | (guint16)(d))
#define    ppc_li(c,D,v)   ppc_addi   (c, D, 0, v);
#define   ppc_lis(c,D,v)   ppc_addis  (c, D, 0, v);
#define   ppc_lwz(c,D,d,a) ppc_emit32 (c, (32 << 26) | ((D) << 21) | ((a) << 16) | (guint16)(d))
#define   ppc_lhz(c,D,d,a) ppc_emit32 (c, (40 << 26) | ((D) << 21) | ((a) << 16) | (guint16)(d))
#define   ppc_lbz(c,D,d,a) ppc_emit32 (c, (34 << 26) | ((D) << 21) | ((a) << 16) | (guint16)(d))
#define   ppc_stw(c,S,d,a) ppc_emit32 (c, (36 << 26) | ((S) << 21) | ((a) << 16) | (guint16)(d))
#define   ppc_sth(c,S,d,a) ppc_emit32 (c, (44 << 26) | ((S) << 21) | ((a) << 16) | (guint16)(d))
#define   ppc_stb(c,S,d,a) ppc_emit32 (c, (38 << 26) | ((S) << 21) | ((a) << 16) | (guint16)(d))
#define  ppc_stwu(c,s,d,a) ppc_emit32 (c, (37 << 26) | ((s) << 21) | ((a) << 16) | (guint16)(d))
#define    ppc_or(c,a,s,b) ppc_emit32 (c, (31 << 26) | ((s) << 21) | ((a) << 16) | ((b) << 11) | 888)
#define   ppc_ori(c,S,A,u) ppc_emit32 (c, (24 << 26) | ((S) << 21) | ((A) << 16) | (guint16)(u))
#define    ppc_mr(c,a,s)   ppc_or     (c, a, s, s)
#define ppc_mfspr(c,D,spr) ppc_emit32 (c, (31 << 26) | ((D) << 21) | ((spr) << 11) | (339 << 1))
#define  ppc_mflr(c,D)     ppc_mfspr  (c, D, ppc_lr)
#define ppc_mtspr(c,spr,S) ppc_emit32 (c, (31 << 26) | ((S) << 21) | ((spr) << 11) | (467 << 1))
#define  ppc_mtlr(c,S)     ppc_mtspr  (c, ppc_lr, S)

#define  ppc_blrl(c)       ppc_emit32 (c, 0x4e800021)
#define   ppc_blr(c)       ppc_emit32 (c, 0x4e800020)

#define   ppc_lfs(c,D,d,A) ppc_emit32 (c, (48 << 26) | ((D) << 21) | ((A) << 16) | (guint16)(d))
#define   ppc_lfd(c,D,d,A) ppc_emit32 (c, (50 << 26) | ((D) << 21) | ((A) << 16) | (guint16)(d))
#define  ppc_stfs(c,S,d,a) ppc_emit32 (c, (52 << 26) | ((S) << 21) | ((a) << 16) | (guint16)(d))
#define  ppc_stfd(c,S,d,a) ppc_emit32 (c, (54 << 26) | ((S) << 21) | ((a) << 16) | (guint16)(d))


#endif
