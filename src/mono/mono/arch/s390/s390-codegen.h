/*
   Copyright (C)  2001 Radek Doulik
*/

#ifndef S390_H
#define S390_H
#include <glib.h>
#include <assert.h>

#define FLOAT_REGS 	2	/* No. float registers for parms    */
#define GENERAL_REGS 	5	/* No. general registers for parms  */

#define ARG_BASE s390_r10	/* Register for addressing arguments*/
#define STKARG \
	(i*(sizeof(stackval)))	/* Displacement of ith argument     */

#define MINV_POS  	96 	/* MonoInvocation stack offset      */
#define STACK_POS 	(MINV_POS - sizeof (stackval) * sig->param_count)
#define OBJ_POS   	8
#define TYPE_OFFSET 	(G_STRUCT_OFFSET (stackval, type))

#define MIN_CACHE_LINE 256

/*------------------------------------------------------------------*/
/* Sequence to add an int/long long to parameters to stack_from_data*/
/*------------------------------------------------------------------*/
#define ADD_ISTACK_PARM(r, i) \
	if (reg_param < GENERAL_REGS-(r)) { \
		s390_la (p, s390_r4, 0, STK_BASE, \
		         local_start + (reg_param - this_flag) * sizeof(long)); \
		reg_param += (i); \
	} else { \
		s390_la (p, s390_r4, 0, STK_BASE, \
			 sz.stack_size + MINV_POS + stack_param * sizeof(long)); \
		stack_param += (i); \
	}

/*------------------------------------------------------------------*/
/* Sequence to add a float/double to parameters to stack_from_data  */
/*------------------------------------------------------------------*/
#define ADD_RSTACK_PARM(i) \
	if (fpr_param < FLOAT_REGS) { \
		s390_la (p, s390_r4, 0, STK_BASE, \
		         float_pos + (fpr_param * sizeof(float) * (i))); \
		fpr_param++; \
	} else { \
		stack_param += (stack_param % (i)); \
		s390_la (p, s390_r4, 0, STK_BASE, \
		         sz.stack_size + MINV_POS + stack_param * sizeof(float) * (i)); \
		stack_param += (i); \
	}

/*------------------------------------------------------------------*/
/* Sequence to add a structure ptr to parameters to stack_from_data */
/*------------------------------------------------------------------*/
#define ADD_TSTACK_PARM \
	if (reg_param < GENERAL_REGS) { \
		s390_l (p, s390_r4, 0, STK_BASE, \
			local_start + (reg_param - this_flag) * sizeof(long)); \
		reg_param++; \
	} else { \
		s390_l (p, s390_r4, 0, STK_BASE, \
			sz.stack_size + MINV_POS + stack_param * sizeof(long)); \
		stack_param++; \
	}

#define ADD_PSTACK_PARM(r, i) \
	if (reg_param < GENERAL_REGS-(r)) { \
		s390_la (p, s390_r4, 0, STK_BASE, \
			 local_start + (reg_param - this_flag) * sizeof(long)); \
		reg_param += (i); \
	} else { \
		s390_l (p, s390_r4, 0, STK_BASE, \
			sz.stack_size + MINV_POS + stack_param * sizeof(long)); \
		stack_param++; \
	}
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
	s390_a0 = 0,
	s390_a1,
	s390_a2,
	s390_a3,
	s390_a4,
	s390_a5,
	s390_a6,
	s390_a7,
	s390_a8,
	s390_a9,
	s390_a10,
	s390_a11,
	s390_a12,
	s390_a13,
	s390_a14,
	s390_a15,
} S390AccRegister;

typedef enum {
	s390_fpc = 256,
} S390SpecialRegister;

#define s390_is_imm16(val) 		((gint)val >= (gint)-(1<<15) && \
					 (gint)val <= (gint)((1<<15)-1))
#define s390_is_uimm16(val) 		((gint)val >= 0 && (gint)val <= 65535)
#define s390_is_imm12(val)		((gint)val >= (gint)-(1<<11) && \
					 (gint)val <= (gint)((1<<15)-1))
#define s390_is_uimm12(val)		((gint)val >= 0 && (gint)val <= 4095)

#define STK_BASE			s390_r15
#define S390_MINIMAL_STACK_SIZE		96
#define S390_REG_SAVE_OFFSET 		24
#define S390_RET_ADDR_OFFSET		56

#define S390_CC_ZR			8
#define S390_CC_NE			7
#define S390_CC_NZ			7
#define S390_CC_LT			4
#define S390_CC_GT			2
#define S390_CC_GE			11
#define S390_CC_LE			13
#define S390_CC_OV			1
#define S390_CC_NO			14
#define S390_CC_CY			3
#define S390_CC_NC			12
#define S390_CC_UN			15

#define s390_word(addr, value)		do {*((guint32 *) addr) = (guint32) (value); \
					    ((guint32 *) addr)++;} while (0)
#define s390_float(addr, value)		do {*((guint32 *) addr) = (guint32) (value); \
					    ((guint32 *) addr)++;} while (0)
#define s390_llong(addr, value)		do {*((guint64 *) addr) = (guint64) (value); \
					    ((guint64 *) addr)++;} while (0)
#define s390_double(addr, value)	do {*((guint64 *) addr) = (guint64) (value); \
					    ((guint64 *) addr)++;} while (0)
#define s390_emit16(c, x)		do {*((guint16 *) c) = x; ((guint16 *) c)++;} while(0)
#define s390_emit32(c, x)		do {*((guint32 *) c) = x; ((guint32 *) c)++;} while(0)
#define s390_basr(code, r1, r2)		s390_emit16 (code, (13 << 8 | (r1) << 4 | (r2)))
#define s390_bras(code, r, o)		s390_emit32 (code, (167 << 24 | (r) << 20 | 5 << 16 | (o)))
#define s390_brasl(code, r, o)		do {s390_emit16 (code, (192 << 8 | (r) << 4 | 5)); \
					    s390_emit32 (code, (o));} while(0)
#define s390_ahi(code, r, v)		s390_emit32 (code, (167 << 24 | (r) << 20 | 10 << 16 | ((v) & 0xffff)))
#define s390_alcr(code, r1, r2)		s390_emit32 (code, (185 << 24 | 152 << 16 | (r1) << 4 | (r2)))
#define s390_ar(code, r1, r2)		s390_emit16 (code, (26 << 8 | (r1) << 4 | (r2)))
#define s390_alr(code, r1, r2)		s390_emit16 (code, (30 << 8 | (r1) << 4 | (r2)))
#define s390_a(code, r, x, b, d)	s390_emit32 (code, (90 << 24 | (r) << 20 | (x) << 16 | (b) << 12 | ((d) & 0xfff)))
#define s390_al(code, r, x, b, d)	s390_emit32 (code, (94 << 24 | (r) << 20 | (x) << 16 | (b) << 12 | ((d) & 0xfff)))
#define s390_slbr(code, r1, r2)		s390_emit32 (code, (185 << 24 | 153 << 16 | (r1) << 4 | (r2)))
#define s390_sr(code, r1, r2)		s390_emit16 (code, (27 << 8 | (r1) << 4 | (r2)))
#define s390_slr(code, r1, r2)		s390_emit16 (code, (31 << 8 | (r1) << 4 | (r2)))
#define s390_s(code, r, x, b, d)	s390_emit32 (code, (91 << 24 | (r) << 20 | (x) << 16 | (b) << 12 | ((d) & 0xfff)))
#define s390_sl(code, r, x, b, d)	s390_emit32 (code, (95 << 24 | (r) << 20 | (x) << 16 | (b) << 12 | ((d) & 0xfff)))
#define s390_mr(code, r1, r2)		s390_emit16 (code, (28 << 8 | (r1) << 4 | (r2)))
#define s390_m(code, r, x, b, d)	s390_emit32 (code, (92 << 24 | (r) << 20 | (x) << 16 | (b) << 12 | ((d) & 0xfff)))
#define s390_msr(code, r1, r2)		s390_emit32 (code, (178 << 24 | 82 << 16 | (r1) << 4| (r2)))
#define s390_ms(code, r, x, b, d)	s390_emit32 (code, (113 << 24 | (r) << 20 | (x) << 16 | (b) << 12 | ((d) & 0xfff)))
#define s390_mlr(code, r1, r2)		s390_emit32 (code, (185 << 24 | 150 << 16 | (r1) << 4| (r2)))
#define s390_dr(code, r1, r2)		s390_emit16 (code, (29 << 8 | (r1) << 4 | (r2)))
#define s390_d(code, r, x, b, d)	s390_emit32 (code, (93 << 24 | (r) << 20 | (x) << 16 | (b) << 12 | ((d) & 0xfff)))
#define s390_dlr(code, r1, r2)		s390_emit32 (code, (185 << 24 | 151 << 16 | (r1) << 4| (r2)))
#define s390_br(code, r)		s390_emit16 (code, (7 << 8 | 15 << 4 | (r)))
#define s390_nr(code, r1, r2)		s390_emit16 (code, (20 << 8 | (r1) << 4 | (r2)))
#define s390_n(code, r, x, b, d)	s390_emit32 (code, (84 << 24 | (r) << 20 | (x) << 16 | (b) << 12 | ((d) & 0xfff)))
#define s390_or(code, r1, r2)		s390_emit16 (code, (22 << 8 | (r1) << 4 | (r2)))
#define s390_o(code, r, x, b, d)	s390_emit32 (code, (86 << 24 | (r) << 20 | (x) << 16 | (b) << 12 | ((d) & 0xfff)))
#define s390_xr(code, r1, r2)		s390_emit16 (code, (23 << 8 | (r1) << 4 | (r2)))
#define s390_x(code, r, x, b, d)	s390_emit32 (code, (87 << 24 | (r) << 20 | (x) << 16 | (b) << 12 | ((d) & 0xfff)))
#define s390_lr(code, r1, r2)		s390_emit16 (code, (24 << 8 | (r1) << 4 | (r2)))
#define s390_ltr(code, r1, r2)		s390_emit16 (code, (18 << 8 | (r1) << 4 | (r2)))
#define s390_l(code, r, x, b, d)	s390_emit32 (code, (88 << 24 | (r) << 20 | (x) << 16 | (b) << 12 | ((d) & 0xfff)))
#define s390_lcr(code, r1, r2)		s390_emit16 (code, (19 << 8 | (r1) << 4 | (r2)))
#define s390_lnr(code, r1, r2)		s390_emit16 (code, (17 << 8 | (r1) << 4 | (r2)))
#define s390_lpr(code, r1, r2)		s390_emit16 (code, (16 << 8 | (r1) << 4 | (r2)))
#define s390_lm(code, r1, r2, b, d)	s390_emit32 (code, (152 << 24 | (r1) << 20 | (r2) << 16 \
						    | (b) << 12 | ((d) & 0xfff)))
#define s390_lh(code, r, x, b, d)	s390_emit32 (code, (72 << 24 | (r) << 20 | (x) << 16 | (b) << 12 | ((d) & 0xfff)))
#define s390_lhi(code, r, v)		s390_emit32 (code, (167 << 24 | (r) << 20 | 8 << 16 | ((v) & 0xffff)))
#define s390_ic(code, r, x, b, d)	s390_emit32 (code, (67 << 24 | (r) << 20 | (x) << 16 | (b) << 12 | ((d) & 0xfff)))
#define s390_icm(code, r, m, b, d)	s390_emit32 (code, (191 << 24 | (r) << 20 | (m) << 16 | (b) << 12 | ((d) & 0xfff)))
#define s390_st(code, r, x, b, d)	s390_emit32 (code, (80 << 24 | (r) << 20 | (x) << 16 | (b) << 12 | ((d) & 0xfff)))
#define s390_stm(code, r1, r2, b, d)	s390_emit32 (code, (144 << 24 | (r1) << 20 | (r2) << 16 \
						    | (b) << 12 | ((d) & 0xfff)))
#define s390_stam(c, r1, r2, b, d)	s390_emit32 (code, (155 << 24 | (r1) << 20 | (r2) << 16 \
						    | (b) << 12 | ((d) & 0xfff)))
#define s390_lam(c, r1, r2, b, d)	s390_emit32 (code, (154 << 24 | (r1) << 20 | (r2) << 16 \
						    | (b) << 12 | ((d) & 0xfff)))
#define s390_sth(code, r, x, b, d)	s390_emit32 (code, (64 << 24 | (r) << 20 | (x) << 16 | (b) << 12 | ((d) & 0xfff)))
#define s390_stc(code, r, x, b, d)	s390_emit32 (code, (66 << 24 | (r) << 20 | (x) << 16 | (b) << 12 | ((d) & 0xfff)))
#define s390_stcm(code, r, m, b, d)	s390_emit32 (code, (190 << 24 | (r) << 20 | (m) << 16 | (b) << 12 | ((d) & 0xfff)))
#define s390_la(code, r, x, b, d)	s390_emit32 (code, (65 << 24 | (r) << 20 | (x) << 16 | (b) << 12 | ((d) & 0xfff)))
#define s390_larl(code, r, o)		do {						\
					    s390_emit16 (code, (192 << 8 | (r) << 4));	\
					    s390_emit32 (code, (o));			\
					} while (0)
#define s390_ld(code, f, x, b, d)	s390_emit32 (code, (104 << 24 | (f) << 20 | (x) << 16 | (b) << 12 | ((d) & 0xfff)))
#define s390_le(code, f, x, b, d)	s390_emit32 (code, (120 << 24 | (f) << 20 | (x) << 16 | (b) << 12 | ((d) & 0xfff)))
#define s390_std(code, f, x, b, d)	s390_emit32 (code, (96 << 24 | (f) << 20 | (x) << 16 | (b) << 12 | ((d) & 0xfff)))
#define s390_ste(code, f, x, b, d)	s390_emit32 (code, (112 << 24 | (f) << 20 | (x) << 16 | (b) << 12 | ((d) & 0xfff)))
#define s390_mvc(c, l, b1, d1, b2, d2)	do {s390_emit32 (c, (210 << 24 | ((((l)-1)  << 16) & 0x00ff0000) | \
							(b1) << 12 | ((d1) & 0xfff))); 		  \
					    s390_emit16 (c, ((b2) << 12 | ((d2) & 0xfff)));} while (0)
#define s390_mvcle(c, r1, r3, d2, b2)	s390_emit32 (c, (168 << 24 | (r1) << 20 |	\
						     (r3) << 16 | (b2) << 12 | 		\
						     ((d2) & 0xfff)))
#define s390_break(c)			s390_emit16 (c, 0)
#define s390_nill(c, r1, v)		s390_emit32 (c, (165 << 24 | (r1) << 20 | 7 << 16 | ((v) & 0xffff)))
#define s390_nilh(c, r1, v)		s390_emit32 (c, (165 << 24 | (r1) << 20 | 6 << 16 | ((v) & 0xffff)))
#define s390_brc(c, m, d)		s390_emit32 (c, (167 << 24 | ((m) & 0xff) << 20 | 4 << 16 | ((d) & 0xffff)))
#define s390_cr(c, r1, r2)		s390_emit16 (c, (25 << 8 | (r1) << 4 | (r2)))
#define s390_clr(c, r1, r2)		s390_emit16 (c, (21 << 8 | (r1) << 4 | (r2)))
#define s390_c(c, r, x, b, d)		s390_emit32 (c, (89 << 24 | (r) << 20 | (x) << 16 | (b) << 12 | ((d) & 0xfff)))
#define s390_cl(c, r, x, b, d)		s390_emit32 (c, (85 << 24 | (r) << 20 | (x) << 16 | (b) << 12 | ((d) & 0xfff)))
#define s390_chi(c, r, i)		s390_emit32 (c, (167 << 24 | (r) << 20 | 15 << 16 | ((i) & 0xffff)))
#define s390_j(c,d)			s390_brc(c, S390_CC_UN, d)
#define s390_je(c, d)			s390_brc(c, S390_CC_EQ, d)
#define s390_jeo(c, d)			s390_brc(c, S390_CC_ZR|S390_CC_OV, d)
#define s390_jz(c, d)			s390_brc(c, S390_CC_ZR, d)
#define s390_jnz(c, d)			s390_brc(c, S390_CC_NZ, d)
#define s390_jne(c, d)			s390_brc(c, S390_CC_NZ, d)
#define s390_jp(c, d)			s390_brc(c, S390_CC_GT, d)
#define s390_jm(c, d)			s390_brc(c, S390_CC_LT, d)
#define s390_jh(c, d)			s390_brc(c, S390_CC_GT, d)
#define s390_jl(c, d)			s390_brc(c, S390_CC_LT, d)
#define s390_jnh(c, d)			s390_brc(c, S390_CC_LE, d)
#define s390_jo(c, d)			s390_brc(c, S390_CC_OV, d)
#define s390_jnl(c, d)			s390_brc(c, S390_CC_GE, d)			
#define s390_jlo(c, d)			s390_brc(c, S390_CC_LT|S390_CC_OV, d)
#define s390_jho(c, d)			s390_brc(c, S390_CC_GT|S390_CC_OV, d)		
#define s390_jc(c, m, d)		s390_brc(c, m, d)
#define s390_jcl(c, m, d)		do {s390_emit16 (c, (192 << 8 | (m) << 4 | 4)); \
					    s390_emit32 (c, (d));} while(0)
#define s390_slda(c, r, b, d)		s390_emit32 (c, (143 << 24 | (r) << 20 | (b) << 12 | ((d) & 0xfff)))
#define s390_sldl(c, r, b, d)		s390_emit32 (c, (141 << 24 | (r) << 20 | (b) << 12 | ((d) & 0xfff)))
#define s390_srda(c, r, b, d)		s390_emit32 (c, (142 << 24 | (r) << 20 | (b) << 12 | ((d) & 0xfff)))
#define s390_srdl(c, r, b, d)		s390_emit32 (c, (140 << 24 | (r) << 20 | (b) << 12 | ((d) & 0xfff)))
#define s390_sla(c, r, b, d)		s390_emit32 (c, (139 << 24 | (r) << 20 | (b) << 12 | ((d) & 0xfff)))
#define s390_sll(c, r, b, d)		s390_emit32 (c, (137 << 24 | (r) << 20 | (b) << 12 | ((d) & 0xfff)))
#define s390_sra(c, r, b, d)		s390_emit32 (c, (138 << 24 | (r) << 20 | (b) << 12 | ((d) & 0xfff)))
#define s390_srl(c, r, b, d)		s390_emit32 (c, (136 << 24 | (r) << 20 | (b) << 12 | ((d) & 0xfff)))
#define s390_sqdbr(c, r1, r2)		s390_emit32 (c, (179 << 24 | 21 << 16 | ((r1) << 4) | (r2)))
#define s390_sqebr(c, r1, r2)		s390_emit32 (c, (179 << 24 | 20 << 16 | ((r1) << 4) | (r2)))
#define s390_adbr(c, r1, r2)		s390_emit32 (c, (179 << 24 | 26 << 16 | ((r1) << 4) | (r2)))
#define s390_aebr(c, r1, r2)		s390_emit32 (c, (179 << 24 | 10 << 16 | ((r1) << 4) | (r2)))
#define s390_adb(c, r, x, b, d)		do {s390_emit32 (c, (237 << 24 | (r) << 20 | 	\
					     (x) << 16 | (b) << 12 | ((d) & 0xfff))); 	\
					    s390_emit16 (c, (26)); 			\
					} while (0)
#define s390_sdbr(c, r1, r2)		s390_emit32 (c, (179 << 24 | 27 << 16 | ((r1) << 4) | (r2)))
#define s390_sdb(c, r, x, b, d)		do {s390_emit32 (c, (237 << 24 | (r) << 20 | 	\
					     (x) << 16 | (b) << 12 | ((d) & 0xfff))); 	\
					    s390_emit16 (c, (27)); 			\
					} while (0)
#define s390_sebr(c, r1, r2)		s390_emit32 (c, (179 << 24 | 11 << 16 | ((r1) << 4) | (r2)))
#define s390_mdbr(c, r1, r2)		s390_emit32 (c, (179 << 24 | 28 << 16 | ((r1) << 4) | (r2)))
#define s390_meebr(c, r1, r2)		s390_emit32 (c, (179 << 24 | 23 << 16 | ((r1) << 4) | (r2)))
#define s390_ldr(c, r1, r2)		s390_emit16 (c, (40 << 8 | (r1) << 4 | (r2)))
#define s390_ler(c, r1, r2)		s390_emit16 (c, (56 << 8 | (r1) << 4 | (r2)))
#define s390_lzdr(c, r1)		s390_emit32 (c, (179 << 24 | 117 << 16 | (r1) << 4))
#define s390_lzer(c, r1)		s390_emit32 (c, (179 << 24 | 116 << 16 | (r1) << 4))
#define s390_ddbr(c, r1, r2)		s390_emit32 (c, (179 << 24 | 29 << 16 | ((r1) << 4) | (r2)))
#define s390_debr(c, r1, r2)		s390_emit32 (c, (179 << 24 | 13 << 16 | ((r1) << 4) | (r2)))
#define s390_didbr(c, r1, r2, m, r3)	s390_emit32 (c, (179 << 24 | 91 << 16 | ((r3) << 12) | ((m) << 8) | ((r1) << 4) | (r2)))
#define s390_lcdbr(c, r1, r2)		s390_emit32 (c, (179 << 24 | 19 << 16 | ((r1) << 4) | (r2)))
#define s390_lndbr(c, r1, r2)		s390_emit32 (c, (179 << 24 | 17 << 16 | ((r1) << 4) | (r2)))
#define s390_ldebr(c, r1, r2)		s390_emit32 (c, (179 << 24 | 4 << 16 | ((r1) << 4) | (r2)))
#define s390_lnebr(c, r1, r2)		s390_emit32 (c, (179 << 24 | 1 << 16 | ((r1) << 4) | (r2)))
#define s390_ledbr(c, r1, r2)		s390_emit32 (c, (179 << 24 | 68 << 16 | ((r1) << 4) | (r2)))
#define s390_ldeb(c, r, x, b, d)	do {s390_emit32 (c, (237 << 24 | (r) << 20 | 	\
					     (x) << 16 | (b) << 12 | ((d) & 0xfff))); 	\
					    s390_emit16 (c, (4)); 			\
					} while (0)
#define s390_cfdbr(c, r1, m, f2)	s390_emit32 (c, (179 << 24 | 153 << 16 | (m) << 12 | (r1) << 4 | (f2)))
#define s390_cdfbr(c, r1, r2)		s390_emit32 (c, (179 << 24 | 149 << 16 | (r1) << 4 | (r2)))
#define s390_cefbr(c, r1, r2)		s390_emit32 (c, (179 << 24 | 148 << 16 | (r1) << 4 | (r2)))
#define s390_cdbr(c, r1, r2)		s390_emit32 (c, (179 << 24 | 25 << 16 | (r1) << 4 | (r2)))
#define s390_cebr(c, r1, r2)		s390_emit32 (c, (179 << 24 | 9 << 16 | (r1) << 4 | (r2)))
#define s390_cdb(c, r, x, b, d)		do {s390_emit32 (c, (237 << 24 | (r) << 20 | 	\
					     (x) << 16 | (b) << 12 | ((d) & 0xfff))); 	\
					    s390_emit16 (c, (25)); 			\
					} while (0)
#define s390_tcdb(c, r, x, b, d)	do {s390_emit32 (c, (237 << 24 | (r) << 20 | 	\
					     (x) << 16 | (b) << 12 | ((d) & 0xfff))); 	\
					    s390_emit16 (c, (17)); 			\
					} while (0)
#define s390_tedb(c, r, x, b, d)	do {s390_emit32 (c, (237 << 24 | (r) << 20 | 	\
					     (x) << 16 | (b) << 12 | ((d) & 0xfff))); 	\
					    s390_emit16 (c, (16)); 			\
					} while (0)
#define s390_stfpc(c, b, d)		s390_emit32 (c, (178 << 24 | 156 << 16 | \
						    (b) << 12 | ((d) & 0xfff)))

#endif
