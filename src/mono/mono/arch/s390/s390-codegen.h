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
#define s390_is_uimm16(val) 		((gint)val >= 0 && (gint)val <= 32767)
#define s390_is_imm12(val)		((gint)val >= (gint)-(1<<11) && \
					 (gint)val <= (gint)((1<<11)-1))
#define s390_is_uimm12(val)		((gint)val >= 0 && (gint)val <= 4095)

#define STK_BASE			s390_r15
#define S390_MINIMAL_STACK_SIZE		96
#define S390_PARM_SAVE_OFFSET 		8
#define S390_REG_SAVE_OFFSET 		24
#define S390_RET_ADDR_OFFSET		56
#define S390_FLOAT_SAVE_OFFSET		64

#define S390_CC_ZR			8
#define S390_CC_NE			7
#define S390_CC_NZ			7
#define S390_CC_LT			4
#define S390_CC_GT			2
#define S390_CC_GE			11
#define S390_CC_NM			11
#define S390_CC_LE			13
#define S390_CC_OV			1
#define S390_CC_NO			14
#define S390_CC_CY			3
#define S390_CC_NC			12
#define S390_CC_UN			15

#define s390_word(addr, value) do	 	\
{						\
	* (guint32 *) addr = (guint32) value;	\
	addr += sizeof(guint32);		\
} while (0)

#define s390_float(addr, value)	do 		\
{						\
	* (gfloat *) addr = (gfloat) value;	\
	addr += sizeof(gfloat);			\
} while (0)

#define s390_llong(addr, value)	do 		\
{						\
	* (guint64 *) addr = (guint64) value;	\
	addr += sizeof(guint64);		\
} while (0)

#define s390_double(addr, value) do 		\
{						\
	* (gdouble *) addr = (gdouble) value;	\
	addr += sizeof(gdouble);		\
} while (0)

typedef struct {
	short 	op;
} E_Format;

typedef struct {
	char	op;
	int	im;
} I_Format;

typedef struct {
	char 	op;
	char	r1 : 4;
	char 	r2 : 4;
} RR_Format;

typedef struct {
	short	op;
	char	xx;
	char	r1 : 4;
	char	r2 : 4;
} RRE_Format;

typedef struct {
	short	op;
	char	r1 : 4;
	char	xx : 4;
	char	r3 : 4;
	char	r2 : 4;
} RRF_Format_1;

typedef struct {
	short	op;
	char	m3 : 4;
	char	xx : 4;
	char	r1 : 4;
	char	r2 : 4;
} RRF_Format_2;

typedef struct {
	short	op;
	char	r3 : 4;
	char	m4 : 4;
	char	r1 : 4;
	char	r2 : 4;
} RRF_Format_3;

typedef struct {
	char	op;
	char	r1 : 4;
	char	x2 : 4;
	char	b2 : 4;
	short	d2 : 12;
} RX_Format;

typedef struct {
	char 	op1;
	char	r1 : 4;
	char	x2 : 4;
	char	b2 : 4;
	int	d2 : 12;
	char	xx;
	char	op2;
} RXE_Format;

typedef struct {
	char 	op1;
	char	r3 : 4;
	char	x2 : 4;
	char	b2 : 4;
	int	d2 : 12;
	char	r1 : 4;
	char	xx : 4;
	char	op2;
} RXF_Format;

typedef struct __attribute__ ((packed)) {
	char 	op1;
	char	r1 : 4;
	char	x2 : 4;
	char	b2 : 4;
	int	d2 : 20;
	char	op2;
} RXY_Format;

typedef struct {
	char 	op;
	char	r1 : 4;
	char	r3 : 4;
	char	b2 : 4;
	int	d2 : 12;
} RS_Format_1;

typedef struct {
	char 	op;
	char	r1 : 4;
	char	m3 : 4;
	char	b2 : 4;
	int	d2 : 12;
} RS_Format_2;

typedef struct {
	char 	op;
	char	r1 : 4;
	char	xx : 4;
	char	b2 : 4;
	int	d2 : 12;
} RS_Format_3;

typedef struct __attribute__ ((packed)) {
	char 	op1;
	char	r1 : 4;
	char	r3 : 4;
	char	b2 : 4;
	int	d2 : 20;
	char 	op2;
} RSY_Format_1;

typedef struct __attribute__ ((packed)) {
	char 	op1;
	char	r1 : 4;
	char	m3 : 4;
	char	b2 : 4;
	int	d2 : 20;
	char 	op2;
} RSY_Format_2;

typedef struct {
	char 	op1;
	char	l1 : 4;
	char	xx : 4;
	char	b1 : 4;
	int 	d1 : 12;
	char	yy;
	char 	op2;
} RSL_Format;

typedef struct {
	char 	op;
	char	r1 : 4;
	char	r3 : 4;
	short	i2;
} RSI_Format;

typedef struct {
	char 	op1;
	char	r1 : 4;
	char	op2 : 4;
	short	i2;
} RI_Format;

typedef struct {
	char 	op1;
	char	r1 : 4;
	char	r3 : 4;
	short	i2;
	char	xx;
	char	op2;
} RIE_Format;

typedef struct __attribute__ ((packed)) {
	char 	op1;
	char	r1 : 4;
	char	op2 : 4;
	int	i2;
} RIL_Format_1;

typedef struct __attribute__ ((packed)) {
	char 	op1;
	char	m1 : 4;
	char	op2 : 4;
	int	i2;
} RIL_Format_2;

typedef struct {
	char	op;
	char	i2;
	char	b1 : 4;
	short	d1 : 12;
} SI_Format;

typedef struct __attribute__ ((packed)) {
	char	op1;
	char	i2;
	char	b1 : 4;
	int	d1 : 20;
	char	op2;
} SIY_Format;

typedef struct {
	short	op;
	char	b2 : 4;
	short	d2 : 12;
} S_Format;

typedef struct {
	char	op;
	char	ll;
	char	b1 : 4;
	short	d1 : 12;
	char	b2 : 4;
	short	d2 : 12;
} SS_Format_1;

typedef struct {
	char	op;
	char	l1 : 4;
	char	l2 : 4;
	char	b1 : 4;	
	short	d1 : 12;
	char	b2 : 4;
	short	d2 : 12;
} SS_Format_2;

typedef struct {
	char	op;
	char	r1 : 4;
	char	r3 : 4;
	char	b1 : 4;	
	short	d1 : 12;
	char	b2 : 4;
	short	d2 : 12;
} SS_Format_3;	

typedef struct {
	char	op;
	char	r1 : 4;
	char	r3 : 4;
	char	b2 : 4;	
	short	d2 : 12;
	char	b4 : 4;
	short	d4 : 12;
} SS_Format_4;	

typedef struct __attribute__ ((packed)) {
	short	op;
	char	b1 : 4;
	short	d1 : 12;
	char	b2 : 4;
	short	d2 : 12;
} SSE_Format;

#define s390_emit16(c, x) do 			\
{						\
	*((guint16 *) c) = x; 			\
	c += sizeof(guint16);			\
} while(0)

#define s390_emit32(c, x) do 			\
{						\
	*((guint32 *) c) = x; 			\
	c += sizeof(guint32);			\
} while(0)

#define S390_E(c,opc) 			s390_emit16(c,opc)

#define S390_I(c,opc,imm) 		s390_emit16(c, (opc << 8 | imm))

#define S390_RR(c,opc,g1,g2)		s390_emit16(c, (opc << 8 | (g1) << 4 | g2))

#define S390_RRE(c,opc,g1,g2)		s390_emit32(c, (opc << 16 | (g1) << 4 | g2)) 

#define S390_RRF_1(c,opc,g1,g2,g3)	s390_emit32(c, (opc << 16 | (g1) << 12 | (g3) << 4 | g2))

#define S390_RRF_2(c,opc,g1,k3,g2)	s390_emit32(c, (opc << 16 | (k3) << 12 | (g1) << 4 | g2))

#define S390_RRF_3(c,opc,g1,g2,k4,g3)	s390_emit32(c, (opc << 16 | (g3) << 12 | (k4) << 8 | (g1) << 4 | g2))

#define S390_RX(c,opc,g1,n2,s2,p2)	s390_emit32(c, (opc << 24 | (g1) << 20 | (n2) << 16 | (s2) << 12 | ((p2) & 0xfff)))

#define S390_RXE(c,opc,g1,n2,s2,p2) do  			\
{								\
	s390_emit16(c, ((opc & 0xff00) | (g1) << 4 | n2));	\
	s390_emit32(c, ((s2) << 28 | (((p2) & 0xfff) << 16) | 	\
			(opc & 0xff)));				\
} while (0)

#define S390_RXY(c,opc,g1,n2,s2,p2) do				\
{								\
	s390_emit16(c, ((opc & 0xff00) | (g1) << 4 | n2));	\
	s390_emit32(c, ((s2) << 28 | (((p2) & 0xfffff) << 8) | 	\
			(opc & 0xff)));				\
} while (0)

#define S390_RS_1(c,opc,g1,g3,s2,p2) 	s390_emit32(c, (opc << 24 | (g1) << 20 | (g3) << 16 | (s2) << 12 | ((p2) & 0xfff))) 

#define S390_RS_2(c,opc,g1,k3,s2,p2)	s390_emit32(c, (opc << 24 | (g1) << 20 | (k3) << 16 | (s2) << 12 | ((p2) & 0xfff)))

#define S390_RS_3(c,opc,g1,s2,p2)	s390_emit32(c, (opc << 24 | (g1) << 20 | (s2) << 12 | ((p2) & 0xfff)))

#define S390_RSY_1(c,opc,g1,g3,s2,p2) do			\
{								\
	s390_emit16(c, ((opc & 0xff00) | (g1) << 4 | g3));	\
	s390_emit32(c, ((s2) << 28 | (((p2) & 0xfffff) << 8) | 	\
			(opc & 0xff)));				\
} while (0)

#define S390_RSY_2(c,opc,g1,k3,s2,p2) do			\
{								\
	s390_emit16(c, ((opc & 0xff00) | (g1) << 4 | k3));	\
	s390_emit32(c, ((s2) << 28 | (((p2) & 0xfffff) << 8) | 	\
			(opc & 0xff)));				\
} while (0)

#define S390_RSL(c,opc,ln,s1,p1) do 				\
{								\
	s390_emit16(c, ((opc & 0xff00) | (ln) << 4));		\
	s390_emit32(c, ((s1) << 28 | ((s1 & 0xfff) << 16) | 	\
			(opc & 0xff)));				\
} while (0)

#define S390_RSI(c,opc,g1,g3,m2) 	s390_emit32(c, (opc << 24 | (g1) << 20 | (g3) << 16 | (m2 & 0xffff)))

#define S390_RI(c,opc,g1,m2)		s390_emit32(c, ((opc >> 4) << 24 | (g1) << 20 | (opc & 0x0f) << 16 | (m2 & 0xffff)))

#define S390_RIE(c,opc,g1,g3,m2) do				\
{								\
	s390_emit16(c, ((opc & 0xff00) | (g1) << 4 | g3));	\
	s390_emit32(c, ((m2) << 16 | (opc & 0xff)));		\
} while (0)

#define S390_RIL_1(c,opc,g1,m2) do					\
{									\
	s390_emit16(c, ((opc >> 4) << 8 | (g1) << 4 | (opc & 0xf)));	\
	s390_emit32(c, m2);						\
} while (0)

#define S390_RIL_2(c,opc,k1,m2) do					\
{									\
	s390_emit16(c, ((opc >> 4) << 8 | (k1) << 4 | (opc & 0xf)));	\
	s390_emit32(c, m2);						\
} while (0)

#define S390_SI(c,opc,s1,p1,m2)		s390_emit32(c, (opc << 24 | (m2) << 16 | (s1) << 12 | ((p1) & 0xfff)));

#define S390_SIY(c,opc,s1,p1,m2) do				\
{								\
	s390_emit16(c, ((opc & 0xff00) | m2));			\
	s390_emit32(c, ((s1) << 24 | (((p2) & 0xfffff) << 8) | 	\
			(opc & 0xff)));				\
} while (0)

#define S390_S(c,opc,s2,p2)	s390_emit32(c, (opc << 16 | (s2) << 12 | ((p2) & 0xfff)))

#define S390_SS_1(c,opc,ln,s1,p1,s2,p2) do			\
{								\
	s390_emit32(c, (opc << 24 | ((ln-1) & 0xff) << 16 |	\
			(s1) << 12 | ((p1) & 0xfff)));		\
	s390_emit16(c, ((s2) << 12 | ((p2) & 0xfff)));		\
} while (0)

#define S390_SS_2(c,opc,n1,n2,s1,p1,s2,p2) do			\
{								\
	s390_emit32(c, (opc << 24 | (n1) << 16 | (n2) << 12 |	\
			(s1) << 12 | ((p1) & 0xfff)));		\
	s390_emit16(c, ((s2) << 12 | ((p2) & 0xfff)));		\
} while (0)

#define S390_SS_3(c,opc,g1,g3,s1,p1,s2,p2) do			\
{								\
	s390_emit32(c, (opc << 24 | (g1) << 16 | (g3) << 12 |	\
			(s1) << 12 | ((p1) & 0xfff)));		\
	s390_emit16(c, ((s2) << 12 | ((p2) & 0xfff)));		\
} while (0)

#define S390_SS_4(c,opc,g1,g3,s2,p2,s4,p4) do			\
{								\
	s390_emit32(c, (opc << 24 | (g1) << 16 | (g3) << 12 |	\
			(s2) << 12 | ((p2) & 0xfff)));		\
	s390_emit16(c, ((s4) << 12 | ((p4) & 0xfff)));		\
} while (0)

#define S390_SSE(c,opc,s1,p1,s2,p2) do			\
{							\
	s390_emit16(c, opc);				\
	s390_emit16(c, ((s1) << 12 | ((p1) & 0xfff)));	\
	s390_emit16(c, ((s2) << 12 | ((p2) & 0xfff)));	\
} while (0)

#define s390_a(c, r, x, b, d)		S390_RX(c, 0x5a, r, x, b, d)
#define s390_adb(c, r, x, b, d)		S390_RXE(c, 0xed1a, r, x, b, d)
#define s390_adbr(c, r1, r2)		S390_RRE(c, 0xb31a, r1, r2)
#define s390_aebr(c, r1, r2)		S390_RRE(c, 0xb30a, r1, r2)
#define s390_ahi(c, r, v)		S390_RI(c, 0xa7a, r, v)
#define s390_alc(c, r, x, b, d)		S390_RXY(c, 0xe398, r, x, b, d)
#define s390_alcr(c, r1, r2)		S390_RRE(c, 0xb998, r1, r2)
#define s390_al(c, r, x, b, d)		S390_RX(c, 0x5e, r, x, b, d)
#define s390_alr(c, r1, r2)		S390_RR(c, 0x1e, r1, r2)
#define s390_ar(c, r1, r2)		S390_RR(c, 0x1a, r1, r2)
#define s390_basr(c, r1, r2)		S390_RR(c, 0x0d, r1, r2)
#define s390_bctr(c, r1, r2)		S390_RR(c, 0x06, r1, r2)
#define s390_bras(c, r, o)		S390_RI(c, 0xa75, r, o)
#define s390_brasl(c, r, o)		S390_RIL_1(c, 0xc05, r, o)
#define s390_brc(c, m, d)		S390_RI(c, 0xa74, m, d)
#define s390_br(c, r)			S390_RR(c, 0x07, 0xf, r)
#define s390_break(c)			S390_RR(c, 0, 0, 0)
#define s390_c(c, r, x, b, d)		S390_RX(c, 0x59, r, x, b, d)
#define s390_cdb(c, r, x, b, d)		S390_RXE(c, 0xed19, r, x, b, d)
#define s390_cdbr(c, r1, r2)		S390_RRE(c, 0xb319, r1, r2)
#define s390_cdfbr(c, r1, r2)		S390_RRE(c, 0xb395, r1, r2)
#define s390_cds(c, r1, r2, b, d)	S390_RX(c, 0xbb, r1, r2, b, d)
#define s390_cebr(c, r1, r2)		S390_RRE(c, 0xb309, r1, r2)
#define s390_cfdbr(c, r1, m, r2)	S390_RRF_2(c, 0xb399, r1, m, r2)
#define s390_chi(c, r, i)		S390_RI(c, 0xa7e, r, i)
#define s390_cl(c, r, x, b, d)		S390_RX(c, 0x55, r, x, b, d)
#define s390_clr(c, r1, r2)		S390_RR(c, 0x15, r1, r2)
#define s390_cr(c, r1, r2)		S390_RR(c, 0x19, r1, r2)
#define s390_cs(c, r1, r2, b, d)	S390_RX(c, 0xba, r1, r2, b, d)
#define s390_ddbr(c, r1, r2)		S390_RRE(c, 0xb31d, r1, r2)
#define s390_debr(c, r1, r2)		S390_RRE(c, 0xb30d, r1, r2)
#define s390_didbr(c, r1, r2, m, r3)    S390_RRF_3(c, 0xb35b, r1, r2, m, r3)
#define s390_dlr(c, r1, r2)		S390_RRE(c, 0xb997, r1, r2)
#define s390_dr(c, r1, r2)		S390_RR(c, 0x1d, r1, r2)
#define s390_ear(c, r1, r2)		S390_RRE(c, 0xb24f, r1, r2)
#define s390_ic(c, r, x, b, d)		S390_RX(c, 0x43, r, x, b, d)
#define s390_icm(c, r, m, b, d)		S390_RX(c, 0xbf, r, m, b, d)
#define s390_jc(c, m, d)		s390_brc(c, m, d)
#define s390_j(c,d)			s390_brc(c, S390_CC_UN, d)
#define s390_jcl(c, m, d)		S390_RIL_2(c, 0xc04, m, d)
#define s390_je(c, d)			s390_brc(c, S390_CC_EQ, d)
#define s390_jeo(c, d)			s390_brc(c, S390_CC_ZR|S390_CC_OV, d)
#define s390_jh(c, d)			s390_brc(c, S390_CC_GT, d)
#define s390_jho(c, d)			s390_brc(c, S390_CC_GT|S390_CC_OV, d)
#define s390_jl(c, d)			s390_brc(c, S390_CC_LT, d)
#define s390_jlo(c, d)			s390_brc(c, S390_CC_LT|S390_CC_OV, d)
#define s390_jm(c, d)			s390_brc(c, S390_CC_LT, d)
#define s390_jne(c, d)			s390_brc(c, S390_CC_NZ, d)
#define s390_jnh(c, d)			s390_brc(c, S390_CC_LE, d)
#define s390_jnl(c, d)			s390_brc(c, S390_CC_GE, d)
#define s390_jnz(c, d)			s390_brc(c, S390_CC_NZ, d)
#define s390_jo(c, d)			s390_brc(c, S390_CC_OV, d)
#define s390_jno(c, d)			s390_brc(c, S390_CC_NO, d)
#define s390_jp(c, d)			s390_brc(c, S390_CC_GT, d)
#define s390_jz(c, d)			s390_brc(c, S390_CC_ZR, d)
#define s390_jcy(c, d)			s390_brc(c, S390_CC_CY, d)
#define s390_jnc(c, d)			s390_brc(c, S390_CC_NC, d)
#define s390_la(c, r, x, b, d)		S390_RX(c, 0x41, r, x, b, d)
#define s390_lam(c, r1, r2, b, d)	S390_RS_1(c, 0x9a, r1, r2, b, d)
#define s390_larl(c, r, o)		S390_RIL_1(c, 0xc00, r, o)
#define s390_lcdbr(c, r1, r2)		S390_RRE(c, 0xb313, r1, r2)
#define s390_lcr(c, r1, r2)		S390_RR(c, 0x13, r1, r2)
#define s390_l(c, r, x, b, d)		S390_RX(c, 0x58, r, x, b, d)
#define s390_ld(c, f, x, b, d)		S390_RX(c, 0x68, f, x, b, d)
#define s390_ldeb(c, r, x, b, d)	S390_RXE(c, 0xed04, r, x, b, d)
#define s390_ldebr(c, r1, r2)		S390_RRE(c, 0xb304, r1, r2)
#define s390_ldr(c, r1, r2)		S390_RR(c, 0x28, r1, r2)
#define s390_le(c, f, x, b, d)		S390_RX(c, 0x78, f, x, b, d)
#define s390_ledbr(c, r1, r2)		S390_RRE(c, 0xb344, r1, r2)
#define s390_ler(c, r1, r2)		S390_RR(c, 0x38, r1, r2)
#define s390_lh(c, r, x, b, d)		S390_RX(c, 0x48, r, x, b, d)
#define s390_lhi(c, r, v)		S390_RI(c, 0xa78, r, v)
#define s390_lm(c, r1, r2, b, d)	S390_RS_1(c, 0x98, r1, r2, b, d)
#define s390_lndbr(c, r1, r2)		S390_RRE(c, 0xb311, r1, r2)
#define s390_lnr(c, r1, r2)		S390_RR(c, 0x11, r1, r2)
#define s390_lpr(c, r1, r2)		S390_RR(c, 0x10, r1, r2)
#define s390_lr(c, r1, r2)		S390_RR(c, 0x18, r1, r2)
#define s390_ltr(c, r1, r2)		S390_RR(c, 0x12, r1, r2)
#define s390_lzdr(c, r)    		S390_RRE(c, 0xb375, r, 0)
#define s390_lzer(c, r)    		S390_RRE(c, 0xb374, r, 0)
#define s390_m(c, r, x, b, d)		S390_RX(c, 0x5c, r, x, b, d)
#define s390_mdbr(c, r1, r2)		S390_RRE(c, 0xb31c, r1, r2)
#define s390_meebr(c, r1, r2)		S390_RRE(c, 0xb317, r1, r2)
#define s390_mlr(c, r1, r2)		S390_RRE(c, 0xb996, r1, r2)
#define s390_mr(c, r1, r2)		S390_RR(c, 0x1c, r1, r2)
#define s390_ms(c, r, x, b, d)		S390_RX(c, 0x71, r, x, b, d)
#define s390_msr(c, r1, r2)		S390_RRE(c, 0xb252, r1, r2)
#define s390_mvc(c, l, b1, d1, b2, d2)	S390_SS_1(c, 0xd2, l, b1, d1, b2, d2)
#define s390_mvcl(c, r1, r2)		S390_RR(c, 0x0e, r1, r2)
#define s390_mvcle(c, r1, r3, d2, b2)	S390_RS_1(c, 0xa8, r1, r3, d2, b2)
#define s390_n(c, r, x, b, d)		S390_RX(c, 0x54, r, x, b, d)
#define s390_nilh(c, r, v)		S390_RI(c, 0xa56, r, v)
#define s390_nill(c, r, v)		S390_RI(c, 0xa57, r, v)
#define s390_nr(c, r1, r2)		S390_RR(c, 0x14, r1, r2)
#define s390_o(c, r, x, b, d)		S390_RX(c, 0x56, r, x, b, d)
#define s390_or(c, r1, r2)		S390_RR(c, 0x16, r1, r2)
#define s390_s(c, r, x, b, d)		S390_RX(c, 0x5b, r, x, b, d)
#define s390_sdb(c, r, x, b, d)		S390_RXE(c, 0xed1b, r, x, b, d)
#define s390_sdbr(c, r1, r2)		S390_RRE(c, 0xb31b, r1, r2)
#define s390_sebr(c, r1, r2)		S390_RRE(c, 0xb30b, r1, r2)
#define s390_sla(c, r, b, d)		S390_RS_3(c, 0x8b, r, b, d) 
#define s390_slb(c, r, x, b, d)		S390_RXY(c, 0xe399, r, x, b, d)
#define s390_slbr(c, r1, r2)		S390_RRE(c, 0xb999, r1, r2)
#define s390_sl(c, r, x, b, d)		S390_RX(c, 0x5f, r, x, b, d)
#define s390_slda(c, r, b, d)		S390_RS_3(c, 0x8f, r, b, d) 
#define s390_sldl(c, r, b, d)		S390_RS_3(c, 0x8d, r, b, d) 
#define s390_sll(c, r, b, d)		S390_RS_3(c, 0x89, r, b, d) 
#define s390_slr(c, r1, r2)		S390_RR(c, 0x1f, r1, r2)
#define s390_sqdbr(c, r1, r2)		S390_RRE(c, 0xb315, r1, r2)
#define s390_sqebr(c, r1, r2)		S390_RRE(c, 0xb314, r1, r2)
#define s390_sra(c, r, b, d)		S390_RS_3(c, 0x8a, r, b, d) 
#define s390_sr(c, r1, r2)		S390_RR(c, 0x1b, r1, r2)
#define s390_srda(c, r, b, d)		S390_RS_3(c, 0x8e, r, b, d) 
#define s390_srdl(c, r, b, d)		S390_RS_3(c, 0x8c, r, b, d) 
#define s390_srl(c, r, b, d)		S390_RS_3(c, 0x88, r, b, d) 
#define s390_stam(c, r1, r2, b, d)	S390_RS_1(c, 0x9b, r1, r2, b, d)
#define s390_stc(c, r, x, b, d)		S390_RX(c, 0x42, r, x, b, d)
#define s390_stcm(c, r, m, b, d)	S390_RX(c, 0xbe, r, m, b, d)
#define s390_st(c, r, x, b, d)		S390_RX(c, 0x50, r, x, b, d)
#define s390_std(c, f, x, b, d)		S390_RX(c, 0x60, f, x, b, d)
#define s390_ste(c, f, x, b, d)		S390_RX(c, 0x70, f, x, b, d)
#define s390_stfpc(c, b, d)		S390_S(c, 0xb29c, b, d)
#define s390_sth(c, r, x, b, d)		S390_RX(c, 0x40, r, x, b, d)
#define s390_stm(c, r1, r2, b, d)	S390_RS_1(c, 0x90, r1, r2, b, d)
#define s390_tcdb(c, r, x, b, d)	S390_RXE(c, 0xed11, r, x, b, d)
#define s390_tceb(c, r, x, b, d)	S390_RXE(c, 0xed10, r, x, b, d)
#define s390_x(c, r, x, b, d)		S390_RX(c, 0x57, r, x, b, d)
#define s390_xr(c, r1, r2)		S390_RR(c, 0x17, r1, r2)
#endif
