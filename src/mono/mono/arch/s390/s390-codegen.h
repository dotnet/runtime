/*
   Copyright (C)  2001 Radek Doulik
*/

#ifndef S390_H
#define S390_H
#include <glib.h>
#include <assert.h>

typedef enum {
	s390_r0 = 0,
	s390_r1,
	s390_r2,
	s390_r3,
	s390_r4,
	s390_r5,
	s390_r6,
	s390_r7,
	s390_r8,
	s390_r9,
	s390_r10,
	s390_r11,
	s390_r12,
	s390_r13,
	s390_r14,
	s390_r15,
} S390IntRegister;

typedef enum {
	s390_f0 = 0,
	s390_f1,
	s390_f2,
	s390_f3,
	s390_f4,
	s390_f5,
	s390_f6,
	s390_f7,
	s390_f8,
	s390_f9,
	s390_f10,
	s390_f11,
	s390_f12,
	s390_f13,
	s390_f14,
	s390_f15,
} S390FloatRegister;

typedef enum {
	s390_fpc = 256,
} S390SpecialRegister;

#define s390_word(addr, value)		*((guint32 *) addr) = (guint32) (value); ((guint32 *) addr)++
#define s390_emit16(c, x)		*((guint16 *) c) = x; ((guint16 *) c)++
#define s390_emit32(c, x)		*((guint32 *) c) = x; ((guint32 *) c)++
#define s390_basr(code, r1, r2)		s390_emit16 (code, (13 << 8 | (r1) << 4 | (r2)))
#define s390_bras(code, r, o)		s390_emit32 (code, (167 << 24 | (r) << 20 | 5 << 16 | (o)))
#define s390_ahi(code, r, v)		s390_emit32 (code, (167 << 24 | (r) << 20 | 10 << 16 | ((v) & 0xffff)))
#define s390_br(code, r)		s390_emit16 (code, (7 << 8 | 15 << 4 | (r)))
#define s390_nr(code, r1, r2)		s390_emit16 (code, (20 << 8 | (r1) << 4 | (r2)))
#define s390_lr(code, r1, r2)		s390_emit16 (code, (24 << 8 | (r1) << 4 | (r2)))
#define s390_l(code, r, b, d)		s390_emit32 (code, (88 << 24 | (r) << 20 | (b) << 12 | ((d) & 0xfff)))
#define s390_lm(code, r1, r2, b, d)	s390_emit32 (code, (152 << 24 | (r1) << 20 | (r2) << 16 \
						    | (b) << 12 | ((d) & 0xfff)))
#define s390_lh(code, r, b, d)		s390_emit32 (code, (72 << 24 | (r) << 20 | (b) << 12 | ((d) & 0xfff)))
#define s390_lhi(code, r, v)		s390_emit32 (code, (167 << 24 | (r) << 20 | 8 << 16 | ((v) & 0xffff)))
#define s390_ic(code, r, b, d)		s390_emit32 (code, (67 << 24 | (r) << 20 | (b) << 12 | ((d) & 0xfff)))
#define s390_st(code, r, b, d)		s390_emit32 (code, (80 << 24 | (r) << 20 | (b) << 12 | ((d) & 0xfff)))
#define s390_stm(code, r1, r2, b, d)	s390_emit32 (code, (144 << 24 | (r1) << 20 | (r2) << 16 \
						    | (b) << 12 | ((d) & 0xfff)))
#define s390_sth(code, r, b, d)		s390_emit32 (code, (64 << 24 | (r) << 20 | (b) << 12 | ((d) & 0xfff)))
#define s390_stc(code, r, b, d)		s390_emit32 (code, (66 << 24 | (r) << 20 | (b) << 12 | ((d) & 0xfff)))
#define s390_la(code, r, b, d)		s390_emit32 (code, (65 << 24 | (r) << 20 | (b) << 12 | ((d) & 0xfff)))
#define s390_ld(code, f, b, d)		s390_emit32 (code, (104 << 24 | (f) << 20 | (b) << 12 | ((d) & 0xfff)))
#define s390_le(code, f, b, d)		s390_emit32 (code, (120 << 24 | (f) << 20 | (b) << 12 | ((d) & 0xfff)))
#define s390_std(code, f, b, d)		s390_emit32 (code, (96 << 24 | (f) << 20 | (b) << 12 | ((d) & 0xfff)))
#define s390_ste(code, f, b, d)		s390_emit32 (code, (112 << 24 | (f) << 20 | (b) << 12 | ((d) & 0xfff)))
#define s390_mvc(c, l, b1, d1, b2, d2)	s390_emit32 (c, (210 << 24 | ((((l)-1)  << 16) & 0x00ff0000) | \
							(b1) << 12 | ((d1) & 0xfff))); 		  \
					s390_emit16 (c, ((b2) << 12 | ((d2) & 0xfff)))
#define s390_mvcl(c, r1, r2)		s390_emit16 (c, (14 << 8 | (r1) << 4 | (r2)));

#endif
