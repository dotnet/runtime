/*
   Copyright (C)  2001 Radek Doulik
*/

#ifndef PPC_H
#define PPC_H
#include <glib.h>
#include <assert.h>

typedef enum {
	r0 = 0,
	r1,
	r2,
	r3,
	r4,
	r5,
	r6,
	r7,
	r8,
	r9,
	r10,
	r11,
	r12,
	r13,
	r14,
	r15,
	r16,
	r17,
	r18,
	r19,
	r20,
	r21,
	r22,
	r23,
	r24,
	r25,
	r26,
	r27,
	r28,
	r29,
	r30,
	r31
} PPCIntRegister;

typedef enum {
	lr = 256,
} PPCSpecialRegister;

#define emit32(c,x) *((guint32 *) c) = x; ((guint32 *)c)++
#define emit32_bad(c,val) { \
guint32 x = val; \
c[0] = x & 0xff; x >>= 8; \
c[1] = x & 0xff; x >>= 8; \
c[2] = x & 0xff; x >>= 8; \
c[3] = x; c += 4; }

#define  addi(c,D,A,d) emit32 (c, (14 << 26) | ((D) << 21) | ((A) << 16) | (guint16)(d))
#define   lwz(c,D,d,a) emit32 (c, (32 << 26) | ((D) << 21) | ((a) << 16) | (guint16)(d))
#define   stw(c,S,d,a) emit32 (c, (36 << 26) | ((S) << 21) | ((a) << 16) | (guint16)(d))
#define  stwu(c,s,d,a) emit32 (c, (37 << 26) | ((s) << 21) | ((a) << 16) | (guint16)(d))
#define    or(c,a,s,b) emit32 (c, (31 << 26) | ((s) << 21) | ((a) << 16) | ((b) << 11) | 888)
#define    mr(c,a,s)   or     (c, a, s, s)
#define mfspr(c,D,spr) emit32 (c, (31 << 26) | ((D) << 21) | ((spr) << 11) | (339 << 1))
#define  mflr(c,D)     mfspr  (c, D, lr)
#define mtspr(c,spr,S) emit32 (c, (31 << 26) | ((S) << 21) | ((spr) << 11) | (467 << 1))
#define  mtlr(c,S)     mtspr  (c, lr, S)

#define  blrl(c)       emit32(c, 0x4e800021)
#define   blr(c)       emit32(c, 0x4e800020)

#endif
