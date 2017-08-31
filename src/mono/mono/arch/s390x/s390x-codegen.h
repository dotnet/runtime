/*
   Copyright (C)  2001 Radek Doulik
   * Licensed under the MIT license. See LICENSE file in the project root for full license information.
*/

#ifndef S390X_H
#define S390X_H
#include <glib.h>
#include <assert.h>
#include <limits.h>

#define FLOAT_REGS 	2	/* No. float registers for parms    */
#define GENERAL_REGS 	5	/* No. general registers for parms  */

#define ARG_BASE s390_r10	/* Register for addressing arguments*/
#define STKARG \
	(i*(sizeof(stackval)))	/* Displacement of ith argument     */

#define MINV_POS  	160	/* MonoInvocation stack offset      */
#define STACK_POS 	(MINV_POS - sizeof (stackval) * sig->param_count)
#define OBJ_POS   	8
#define TYPE_OFFSET 	(G_STRUCT_OFFSET (stackval, type))

#define MIN_CACHE_LINE 256

/*------------------------------------------------------------------*/
/* Sequence to add an int/long long to parameters to stack_from_data*/
/*------------------------------------------------------------------*/
#define ADD_ISTACK_PARM(r, i) \
	if (reg_param < GENERAL_REGS-(r)) { \
		s390_lay (p, s390_r4, 0, STK_BASE, \
		          local_start + (reg_param - this_flag) * sizeof(long)); \
		reg_param += (i); \
	} else { \
		s390_lay (p, s390_r4, 0, STK_BASE, \
			  sz.stack_size + MINV_POS + stack_param * sizeof(long)); \
		stack_param += (i); \
	}

/*------------------------------------------------------------------*/
/* Sequence to add a float/double to parameters to stack_from_data  */
/*------------------------------------------------------------------*/
#define ADD_RSTACK_PARM(i) \
	if (fpr_param < FLOAT_REGS) { \
		s390_lay (p, s390_r4, 0, STK_BASE, \
		          float_pos + (fpr_param * sizeof(float) * (i))); \
		fpr_param++; \
	} else { \
		stack_param += (stack_param % (i)); \
		s390_lay (p, s390_r4, 0, STK_BASE, \
		          sz.stack_size + MINV_POS + stack_param * sizeof(float) * (i)); \
		stack_param += (i); \
	}

/*------------------------------------------------------------------*/
/* Sequence to add a structure ptr to parameters to stack_from_data */
/*------------------------------------------------------------------*/
#define ADD_TSTACK_PARM \
	if (reg_param < GENERAL_REGS) { \
		s390_ly (p, s390_r4, 0, STK_BASE, \
			local_start + (reg_param - this_flag) * sizeof(long)); \
		reg_param++; \
	} else { \
		s390_ly (p, s390_r4, 0, STK_BASE, \
		 	 sz.stack_size + MINV_POS + stack_param * sizeof(long)); \
		stack_param++; \
	}

#define ADD_PSTACK_PARM(r, i) \
	if (reg_param < GENERAL_REGS-(r)) { \
		s390_lay (p, s390_r4, 0, STK_BASE, \
			  local_start + (reg_param - this_flag) * sizeof(long)); \
		reg_param += (i); \
	} else { \
		s390_ly (p, s390_r4, 0, STK_BASE, \
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

typedef enum {
	s390_VR0 = 0,
	s390_VR1 = 1,
	s390_VR2 = 2,
	s390_VR3 = 3,
	s390_VR4 = 4,
	s390_VR5 = 5,
	s390_VR6 = 6,
	s390_VR7 = 7,
	s390_VR8 = 8,
	s390_VR9 = 9,
	s390_VR10 = 10,
	s390_VR11 = 11,
	s390_VR12 = 12,
	s390_VR13 = 13,
	s390_VR14 = 14,
	s390_VR15 = 15,
	s390_VR16 = 16,
	s390_VR17 = 17,
	s390_VR18 = 18,
	s390_VR19 = 19,
	s390_VR20 = 20,
	s390_VR21 = 21,
	s390_VR22 = 22,
	s390_VR23 = 23,
	s390_VR24 = 24,
	s390_VR25 = 25,
	s390_VR26 = 26,
	s390_VR27 = 27,
	s390_VR28 = 28,
	s390_VR29 = 29,
	s390_VR30 = 30,
	s390_VR31 = 31,
	s390_VR_NREG = 32,
} s390_VR_Reg_No;

#define s390_is_imm16(val) 		((glong)val >= (glong) SHRT_MIN && \
					 (glong)val <= (glong) SHRT_MAX)
#define s390_is_imm32(val) 		((glong)val >= (glong) INT_MIN && \
					 (glong)val <= (glong) INT_MAX)
#define s390_is_uimm16(val) 		((glong)val >= 0 && (glong)val <= (glong) USHRT_MAX)
#define s390_is_uimm32(val) 		((glong)val >= 0 && (glong)val <= (glong) UINT_MAX)
#define s390_is_uimm20(val) 		((glong)val >= 0 && (glong)val <= 1048575)
#define s390_is_imm20(val) 		((glong)val >= -524288 && (glong)val <= 524287)
#define s390_is_imm12(val)		((glong)val >= (glong)-4096 && \
					 (glong)val <= (glong)4095)
#define s390_is_uimm12(val)		((glong)val >= 0 && (glong)val <= 4095)

#define STK_BASE			s390_r15
#define S390_SP				s390_r15
#define S390_FP				s390_r11
#define S390_MINIMAL_STACK_SIZE		160
#define S390_REG_SAVE_OFFSET 		48
#define S390_PARM_SAVE_OFFSET 		16
#define S390_RET_ADDR_OFFSET		112
#define S390_FLOAT_SAVE_OFFSET 		128

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
	short	op;
	char	xx;
	char	ri1 : 4;
	char	ri2 : 4;
} IE_Format;

typedef struct {
	short	op;
	short	m1 : 4;
	short	ri2 : 12;
	short	i3;	
} MII_Format;

typedef struct {
	char 	op;
	char	r1 : 4;
	char 	r2 : 4;
} RR_Format;

typedef struct {
	short	op;
	char	r1 : 4;
	char	xx : 4;
	char	r3 : 4;
	char	r4 : 4;
} __attribute__ ((__packed__)) RRD_Format;

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
	short	op;
	char	m3 : 4;
	char	m4 : 4;
	char	r1 : 4;
	char	r2 : 4;
} RRF_Format_4;

typedef struct {
	char	op1;
	char	r1 : 4;
	char	r2 : 4;
	short	b4 : 4;
	short	d4 : 12;
	char	m3 : 4;
	char	xx : 4;
	char	op2;
} RRS_Format;

typedef struct {
	char	op;
	char	r1 : 4;
	char	x2 : 4;
	short	b2 : 4;
	short	d2 : 12;
} RX_Format;

typedef struct {
	char 	op1;
	char	r3 : 4;
	char	x2 : 4;
	short	b2 : 4;
	short	d2 : 12;
	char	r1 : 4;
	char	xx : 4;
	char	op2;
} RXF_Format;

typedef struct {
	char 	op1;
	char	r1 : 4;
	char	x2 : 4;
	int 	b2 : 4;
	int	d2 : 20;
	char	op2;
} __attribute__ ((__packed__)) RXY_Format;

typedef struct {
	char 	op;
	char	r1 : 4;
	char	r3 : 4;
	short	b2 : 4;
	short	d2 : 12;
} RS_Format_1;

typedef struct {
	char 	op;
	char	r1 : 4;
	char	m3 : 4;
	short	b2 : 4;
	short	d2 : 12;
} RS_Format_2;

typedef struct {
	char 	op;
	char	r1 : 4;
	char	xx : 4;
	short	b2 : 4;
	short	dl2 : 12;
	char	dh2;
} RS_Format_3;

typedef struct {
	char 	op1;
	char	r1 : 4;
	char	r3 : 4;
	short	b2 : 4;
	short	dl2 : 12;
	char	dh2;
	char 	op2;
} __attribute__ ((__packed__)) RSY_Format_1;

typedef struct {
	char 	op1;
	char	r1 : 4;
	char	m3 : 4;
	short	b2 : 4;
	short	dl2 : 12;
	char	dh2;
	char 	op2;
} __attribute__ ((__packed__)) RSY_Format_2;

typedef struct {
	char 	op1;
	char	l1 : 4;
	char	xx : 4;
	short	b1 : 4;
	short	d1 : 12;
	char	yy;
	char 	op2;
} __attribute__ ((__packed__)) RSL_Format;

typedef struct {
	char 	op;
	char	r1 : 4;
	char	r3 : 4;
	short	i2;
} __attribute__ ((__packed__)) RSI_Format;

typedef struct {
	char 	op1;
	char	m1 : 4;
	char	op2 : 4;
	short	i2;
} __attribute__ ((__packed__)) RI_Format;

typedef struct {
	char 	op1;
	char	r1 : 4;
	char	r3 : 4;
	short	i2;
	char	xx;
	char	op2;
} __attribute__ ((__packed__)) RIE_Format_1;

typedef struct {
	char 	op1;
	char	r1 : 4;
	char	r3 : 4;
	short	i2;
	char	m2 : 4;
	char    xx : 4;
	char	op2;
} __attribute__ ((__packed__)) RIE_Format_2;

typedef struct {
	char 	op1;
	char	r1 : 4;
	char	r3 : 4;
	short	d;
	char	i;
	char	op2;
} __attribute__ ((__packed__)) RIE_Format_3;

typedef struct {
	char 	op1;
	char	r1 : 4;
	char	yy : 4;
	short	i2;
	char	m3 : 4;
	char	xx : 4;
	char	op2;
} __attribute__ ((__packed__)) RIE_Format_4;

typedef struct {
	char 	op1;
	char	r1 : 4;
	char	r3 : 4;
	short	ri2;
	char	xx;
	char	op2;
} __attribute__ ((__packed__)) RIE_Format_5;

typedef struct {
	char 	op1;
	char	r1 : 4;
	char	r2 : 4;
	char	i3;
	char	i4;
	char	i5;
	char	op2;
} __attribute__ ((__packed__)) RIE_Format_6;

typedef struct {
	char 	op1;
	char	r1 : 4;
	char	m3 : 4;
	short	i2;
	char	xx;
	char	op2;
} __attribute__ ((__packed__)) RIE_Format_7;

typedef struct {
	char	op1;
	char	r1 : 4;
	char	m3 : 4;
	int	b4 : 4;
	int	d4 : 12;
	char	i2;
	char	op2;
} __attribute__ ((__packed__)) RIS_Format;

typedef struct {
	char 	op1;
	char	r1 : 4;
	char	op2 : 4;
	int	i2;
} __attribute__ ((__packed__)) RIL_Format_1;

typedef struct {
	char 	op1;
	char	m1 : 4;
	char	op2 : 4;
	int	i2;
} __attribute__ ((__packed__)) RIL_Format_2;

typedef struct {
	short	op1;
	char	r1 : 4;
	char	x2 : 4;
	short	b2 : 4;
	short	d1 : 12;
	char	m3 : 4;
	char	xx : 4;
	char	op2;
} __attribute__ ((__packed__)) RXE_Format;

typedef struct {
	char	op;
	char	i2;
	short	b1 : 4;
	short	d1 : 12;
} __attribute__ ((__packed__)) SI_Format;

typedef struct {
	short	op;
	char	b1 : 4;
	short	d1 : 12;
	short	i2;
} __attribute__ ((__packed__)) SIL_Format;

typedef struct {
	char	op1;
	char	i2;
	char	b1 : 4;
	int	d1 : 20;
	char	op2;
} __attribute__ ((__packed__)) SIY_Format;

typedef struct {
	char	op1;
	char	m1 : 4;
	char	xx : 4;
	short	b3 : 4;
	short	d3 : 12;
	short	ri2;
} __attribute__ ((__packed__)) SMI_Format;

typedef struct {
	short	op;
	short	b2 : 4;
	short	d2 : 12;
} __attribute__ ((__packed__)) S_Format;

typedef struct {
	char	op;
	char	ll;
	short	b1 : 4;
	short	d1 : 12;
	short	b2 : 4;
	short	d2 : 12;
} __attribute__ ((__packed__)) SS_Format_1;

typedef struct {
	char	op;
	char	l1 : 4;
	char	l2 : 4;
	short	b1 : 4;	
	short	d1 : 12;
	short	b2 : 4;
	short	d2 : 12;
} __attribute__ ((__packed__)) SS_Format_2;

typedef struct {
	char	op;
	char	r1 : 4;
	char	r3 : 4;
	short	b1 : 4;	
	short	d1 : 12;
	short	b2 : 4;
	short	d2 : 12;
} __attribute__ ((__packed__)) SS_Format_3;	

typedef struct {
	char	op;
	char	r1 : 4;
	char	r3 : 4;
	short	b2 : 4;	
	short	d2 : 12;
	short	b4 : 4;
	short	d4 : 12;
} __attribute__ ((__packed__)) SS_Format_4;	

typedef struct {
	short	op;
	short	tb1 : 4;
	short	d1 : 12;
	short	b2 : 4;
	short	d2 : 12;
} __attribute__ ((__packed__)) SSE_Format;

typedef struct {
	short	op;
	char	r3 : 4;
	char	o2 : 4;
	short	b1 : 4;
	short	d1 : 12;
	short	b2 : 4;
	short	d2 : 12;
} __attribute__ ((__packed__)) SSF_Format;

typedef struct {
	short	op1;
	char	v1 : 4;
	char	xx : 4;
	short	i2;
	char	m3 : 4;
	char	rxb : 4;
	char	op2;
} __attribute__ ((__packed__)) VRIa_Format;

typedef struct {
	short	op1;
	char	v1 : 4;
	char	xx : 4;
	char	i2;
	char	i3;
	char	m4 : 4;
	char	rxb : 4;
	char	op2;
} __attribute__ ((__packed__)) VRIb_Format;

typedef struct {
	short	op1;
	char	v1 : 4;
	char	v3 : 4;
	short	i2;
	char	m4 : 4;
	char	rxb : 4;
	char	op2;
} __attribute__ ((__packed__)) VRIc_Format;

typedef struct {
	short	op1;
	char	v1 : 4;
	char	v2 : 4;
	char	v3 : 4;
	char	xx : 4;
	char	i4;
	char	m5 : 4;
	char	rxb : 4;
	char	op2;
} __attribute__ ((__packed__)) VRId_Format;

typedef struct {
	short	op1;
	char	v1 : 4;
	char	v2 : 4;
	short	i3 : 12;
	char	m5 : 4;
	char	m4 : 4;
	char	rxb : 4;
	char	op2;
} __attribute__ ((__packed__)) VRIe_Format;

typedef struct {
	short	op1;
	char	v1 : 4;
	char	v2 : 4;
	char	xx;
	char	m5 : 4;
	char	m4 : 4;
	char	m3 : 4;
	char	rxb : 4;
	char	op2;
} __attribute__ ((__packed__)) VRRa_Format;

typedef struct {
	short	op1;
	char	v1 : 4;
	char	v2 : 4;
	char	v3 : 4;
	char	xx : 4;
	char	m5 : 4;
	char	yy : 4;
	char	m4 : 4;
	char	rxb : 4;
	char	op2;
} __attribute__ ((__packed__)) VRRb_Format;

typedef struct {
	short	op1;
	char	v1 : 4;
	char	v2 : 4;
	char	v3 : 4;
	char	xx : 4;
	char	m5 : 4;
	char	m4 : 4;
	char	m3 : 4;
	char	rxb : 4;
	char	op2;
} __attribute__ ((__packed__)) VRRc_Format;

typedef struct {
	short	op1;
	char	v1 : 4;
	char	v2 : 4;
	char	v3 : 4;
	char	m5 : 4;
	char	m6 : 4;
	char	xx : 4;
	char	v4 : 4;
	char	rxb : 4;
	char	op2;
} __attribute__ ((__packed__)) VRRd_Format;

typedef struct {
	short	op1;
	char	v1 : 4;
	char	v2 : 4;
	char	v3 : 4;
	char	m6 : 4;
	char	xx : 4;
	char	m5 : 4;
	char	v4 : 4;
	char	rxb : 4;
	char	op2;
} __attribute__ ((__packed__)) VRRe_Format;

typedef struct {
	short	op1;
	char	v1 : 4;
	char	r2 : 4;
	char	r3 : 4;
	short	xx;
	char	rxb : 4;
	char	op2;
} __attribute__ ((__packed__)) VRRf_Format;

typedef struct {
	short	op1;
	char	v1 : 4;
	char	v3 : 4;
	char	b2 : 4;
	short	d2 : 12;
	char	m4 : 4;
	char	rxb : 4;
	char	op2;
} __attribute__ ((__packed__)) VRSa_Format;

typedef struct {
	short	op1;
	char	v1 : 4;
	char	r3 : 4;
	char	b2 : 4;
	short	d2 : 12;
	char	m4 : 4;
	char	rxb : 4;
	char	op2;
} __attribute__ ((__packed__)) VRSb_Format;

typedef struct {
	short	op1;
	char	r1 : 4;
	char	v3 : 4;
	char	b2 : 4;
	short	d2 : 12;
	char	m4 : 4;
	char	rxb : 4;
	char	op2;
} __attribute__ ((__packed__)) VRSc_Format;

typedef struct {
	short	op1;
	char	v1 : 4;
	char	v2 : 4;
	char	b2 : 4;
	short	d2 : 12;
	char	m3 : 4;
	char	rxb : 4;
	char	op2;
} __attribute__ ((__packed__)) VRV_Format;

typedef struct {
	short	op1;
	char	v1 : 4;
	char	x2 : 4;
	char	b2 : 4;
	short	d2 : 12;
	char	m3 : 4;
	char	rxb : 4;
	char	op2;
} __attribute__ ((__packed__)) VRX_Format;

#define s390_emit16(c, x) do 			\
{						\
	*((guint16 *) c) = (guint16) x;		\
	c += sizeof(guint16);			\
} while(0)

#define s390_emit32(c, x) do 			\
{						\
	*((guint32 *) c) = (guint32) x;		\
	c += sizeof(guint32);			\
} while(0)

#define S390_E(c,opc) 			s390_emit16(c,opc)

#define S390_I(c,opc,imm) 		s390_emit16(c, (opc << 8 | imm))

#define S390_RR(c,opc,g1,g2)		s390_emit16(c, (opc << 8 | (g1) << 4 | g2))

#define S390_RRE(c,opc,g1,g2)		s390_emit32(c, (opc << 16 | (g1) << 4 | g2)) 

#define S390_RRF_1(c,opc,g1,g2,g3)	s390_emit32(c, (opc << 16 | (g3) << 12 | (g1) << 4 | g2))

#define S390_RRF_2(c,opc,g1,k3,g2)	s390_emit32(c, (opc << 16 | (k3) << 12 | (g1) << 4 | g2))

#define S390_RRF_3(c,opc,g1,g2,k4,g3)	s390_emit32(c, (opc << 16 | (g3) << 12 | (k4) << 8 | (g1) << 4 | g2))

#define S390_RRF_4(c,opc,g1,m3,g2,m4)	s390_emit32(c, (opc << 16 | (m3) << 12 | (m4) << 8 | (g1) << 4 | g2))

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
	s390_emit32(c, ((s2) << 28 | (((p2) & 0xfff) << 16) | 	\
			((((p2) & 0xff000) >> 12) << 8) |	\
			(opc & 0xff)));				\
} while (0)

#define S390_RS_1(c,opc,g1,g3,s2,p2) 	s390_emit32(c, (opc << 24 | (g1) << 20 | (g3) << 16 | (s2) << 12 | ((p2) & 0xfff))) 

#define S390_RS_2(c,opc,g1,k3,s2,p2)	s390_emit32(c, (opc << 24 | (g1) << 20 | (k3) << 16 | (s2) << 12 | ((p2) & 0xfff)))

#define S390_RS_3(c,opc,g1,s2,p2)	s390_emit32(c, (opc << 24 | (g1) << 20 | (s2) << 12 | ((p2) & 0xfff)))

#define S390_RSY_1(c,opc,g1,g3,s2,p2) do			\
{								\
	s390_emit16(c, ((opc & 0xff00) | (g1) << 4 | g3));	\
	s390_emit32(c, ((s2) << 28 | (((p2) & 0xfff) << 16) | 	\
			((((p2) & 0xff000) >> 12) << 8) |	\
			(opc & 0xff)));				\
} while (0)

#define S390_RSY_2(c,opc,g1,k3,s2,p2) do			\
{								\
	s390_emit16(c, ((opc & 0xff00) | (g1) << 4 | k3));	\
	s390_emit32(c, ((s2) << 28 | (((p2) & 0xfff) << 16) | 	\
			((((p2) & 0xff000) >> 12) << 8) |	\
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

#define S390_RIE_1(c,opc,g1,g3,m2) do				\
{								\
	s390_emit16(c, ((opc & 0xff00) | (g1) << 4 | g3));	\
	s390_emit32(c, ((m2) << 16 | (opc & 0xff)));		\
} while (0)

#define S390_RIE_2(c,opc,g1,g2,m3,v) do				\
{								\
	s390_emit16(c, ((opc & 0xff00) | (g1) << 4 | g2));	\
	s390_emit16(c, (v));					\
	s390_emit16(c, ((m3) << 12 | (opc & 0xff)));		\
} while (0)

#define S390_RIE_3(c,opc,g1,i,m3,d) do				\
{								\
	s390_emit16(c, ((opc & 0xff00) | (g1) << 4 | m3));	\
	s390_emit16(c, (d));					\
	s390_emit16(c, ((i) << 8 | (opc & 0xff)));		\
} while (0)

#define S390_RIE_4(c,opc,g1,i2,m3) do				\
{								\
	s390_emit16(c, ((opc & 0xff00) | (g1) << 4);		\
	s390_emit16(c, (i2));					\
	s390_emit16(c, ((m3) << 12 | (opc & 0xff)));		\
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

#define S390_RIS(c,opc,r,i,m3,b,d) do				\
{								\
	s390_emit16(c, ((opc, & 0xff00) | (r1) << 4) | (r2));	\
	s390_emit16(c, ((b) << 12) | (d));			\
	s390_emit16(c, ((i) << 4) | ((opc) & 0xff));		\
}

#define S390_RRS(c,opc,r1,r2,m3,b,d) do				\
{								\
	s390_emit16(c, ((opc, & 0xff00) | (r1) << 4) | (r2));	\
	s390_emit16(c, ((b) << 12) | (d));			\
	s390_emit16(c, ((m3) << 12) | ((opc) & 0xff));		\
}

#define S390_SI(c,opc,s1,p1,m2)		s390_emit32(c, (opc << 24 | (m2) << 16 | (s1) << 12 | ((p1) & 0xfff)));

#define S390_SIY(c,opc,s1,p1,m2) do				\
{								\
	s390_emit16(c, ((opc & 0xff00) | m2));			\
	s390_emit32(c, ((s1) << 24 | (((p2) & 0xfffff) << 8) | 	\
			(opc & 0xff)));				\
} while (0)

#define S390_SIY_1(c,opc,d1,b1,i2) do				\
{								\
	s390_emit16(c, ((opc & 0xff00) | i2));			\
	s390_emit32(c, ((b1) << 28 | (((d1) & 0xfff) << 16) | 	\
			((((d1) & 0xff000) >> 12) << 8) |	\
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

#define S390_SSF(c,opc,r3,s1,p1,s2,p2) do				\
{									\
	s390_emit16(c, (((opc) & 0xff00) << 8) | ((r3) << 4) | 		\
			((opc) & 0xf));					\
	s390_emit16(c, ((s1) << 12 | ((p1) & 0xfff)));			\
	s390_emit16(c, ((s2) << 12 | ((p2) & 0xfff)));			\
} while (0)

#define S390_VRIa(c,opc,v1,i2,m3) do				\
{									\
	char rxb = (((v1) > 15) << 7);					\
	s390_emit16(c, (((opc) & 0xff00) << 8) | ((v1) << 4));		\
	s390_emit16(c, (i2));						\
	s390_emit16(c, (((m3) << 12) | ((rxb) << 8) | ((opc) & 0xff)));	\
} while (0)

#define S390_VRIb(c,opc,v1,i2,i3,m4) do				\
{									\
	char rxb = (((v1) > 15) << 7);					\
	s390_emit16(c, (((opc) & 0xff00) << 8) | ((v1) << 4));		\
	s390_emit16(c, (((i2) << 8) | (i3)));				\
	s390_emit16(c, (((m4) << 12) | ((rxb) << 8) | ((opc) & 0xff)));	\
} while (0)

#define S390_VRIc(c,opc,v1,v3,i2,m4) do				\
{									\
	char rxb = (((v1) > 15) << 7) | (((v2) > 15) << 6) |		\
		   (((v3) > 15) << 5);					\
	s390_emit16(c, (((opc) & 0xff00) << 8) | ((v1) << 4) | ((v2)));	\
	s390_emit16(c, (v4));						\
	s390_emit16(c, (((m4) << 12) | ((rxb) << 8) | ((opc) & 0xff)));	\
} while (0)

#define S390_VRId(c,opc,v1,v2,v3,i4,m5) do				\
{									\
	char rxb = (((v1) > 15) << 7) | (((v2) > 15) << 6) |		\
		   (((v3) > 15) << 5);					\
	s390_emit16(c, (((opc) & 0xff00) << 8) | ((v1) << 4) | ((v2)));	\
	s390_emit16(c, ((v3) << 12) | (i2));				\
	s390_emit16(c, (((m5) << 12) | ((rxb) << 8) | ((opc) & 0xff)));	\
} while (0)

#define S390_VRIe(c,opc,v1,v2,i3,m4,m5) do				\
{									\
	char rxb = (((v1) > 15) << 7) | (((v2) > 15) << 6);		\
	s390_emit16(c, (((opc) & 0xff00) << 8) | ((v1) << 4) | ((v2)));	\
	s390_emit16(c, ((i2) << 8) | (m5));				\
	s390_emit16(c, (((m4) << 12) | ((rxb) << 8) | ((opc) & 0xff)));	\
} while (0)

#define S390_VRRa(c,opc,v1,v2,m3,m4,m5) do				\
{									\
	char rxb = (((v1) > 15) << 7) | (((v2) > 15) << 6);		\
	s390_emit16(c, (((opc) & 0xff00) << 8) | ((v1) << 4) | ((v2)));	\
	s390_emit16(c, ((m5) << 4) | (m4));				\
	s390_emit16(c, (((m3) << 12) | ((rxb) << 8) | ((opc) & 0xff)));	\
} while (0)

#define S390_VRRb(c,opc,v1,v2,v3,m4,m5) do				\
{									\
	char rxb = (((v1) > 15) << 7) | (((v2) > 15) << 6) |		\
		   (((v3) > 15) << 5);					\
	s390_emit16(c, (((opc) & 0xff00) << 8) | ((v1) << 4) | ((v2)));	\
	s390_emit16(c, ((v3) << 12) | ((m5) << 4) | (m4));		\
	s390_emit16(c, (((m3) << 12) | ((rxb) << 8) | ((opc) & 0xff)));	\
} while (0)

#define S390_VRRc(c,opc,v1,v2,m3,m4,m5) do				\
{									\
	char rxb = (((v1) > 15) << 7) | (((v2) > 15) << 6) |		\
		   (((v3) > 15) << 5);					\
	s390_emit16(c, (((opc) & 0xff00) << 8) | ((v1) << 4) | ((v2)));	\
	s390_emit16(c, (((v3) << 12)| (m5) << 4));			\
	s390_emit16(c, (((m4) << 12) | ((rxb) << 8) | ((opc) & 0xff)));	\
} while (0)

#define S390_VRRd(c,opc,v1,v2,v3,v4,m5,m6) do				\
{									\
	char rxb = (((v1) > 15) << 7) | (((v2) > 15) << 6) |		\
		   (((v3) > 15) << 5) | (((v4) > 15) << 4);		\
	s390_emit16(c, (((opc) & 0xff00) << 8) | ((v1) << 4) | ((v2)));	\
	s390_emit16(c, (((v3) << 12)| ((m6) << 8)) | ((m5) << 4));	\
	s390_emit16(c, (((v4) << 12) | ((rxb) << 8) | ((opc) & 0xff)));	\
} while (0)

#define S390_VRRe(c,opc,v1,v2,v3,m4,m5,m6) do				\
{									\
	char rxb = (((v1) > 15) << 7) | (((v2) > 15) << 6) |		\
		   (((v3) > 15) << 5);					\
	s390_emit16(c, (((opc) & 0xff00) << 8) | ((v1) << 4) | ((v2)));	\
	s390_emit16(c, (((v3) << 12)| ((m6) << 8)) | (m5));		\
	s390_emit16(c, (((m4) << 12) | ((rxb) << 8) | ((opc) & 0xff)));	\
} while (0)

#define S390_VRRf(c,opc,v1,r2) do					\
{									\
	char rxb = (((v1) > 15) << 7) | (((v2) > 15) << 6);		\
	s390_emit16(c, (((opc) & 0xff00) << 8) | ((v1) << 4) | ((v2)));	\
	s390_emit16(c, ((r2) << 12)| ((r3) << r8) | (m5));		\
	s390_emit16(c, (((rxb) << 8) | ((opc) & 0xff)));		\
} while (0)

#define S390_VRSa(c,opc,v1,v3,b2,d2,m4) do				\
{									\
	char rxb = (((v1) > 15) << 7) | (((v2) > 15) << 6);		\
	s390_emit16(c, (((opc) & 0xff00) << 8) | ((v1) << 4) | ((v3)));	\
	s390_emit16(c, ((b2) << 12)| (d2));				\
	s390_emit16(c, (((m4) << 12) | ((rxb) << 8) | ((opc) & 0xff)));	\
} while (0)

#define S390_VRSb(c,opc,v1,r3,b2,d2,m4) do				\
{									\
	char rxb = (((v1) > 15) << 7);					\
	s390_emit16(c, (((opc) & 0xff00) << 8) | ((v1) << 4) | ((r3)));	\
	s390_emit16(c, ((b2) << 12)| (d2));				\
	s390_emit16(c, (((m4) << 12) | ((rxb) << 8) | ((opc) & 0xff)));	\
} while (0)

#define S390_VRSc(c,opc,r1,v3,b2,d2,m4) do				\
{									\
	char rxb = (((v1) > 15) << 7);					\
	s390_emit16(c, (((opc) & 0xff00) << 8) | ((r1) << 4) | ((v3)));	\
	s390_emit16(c, ((b2) << 12)| (d2));				\
	s390_emit16(c, (((m4) << 12) | ((rxb) << 8) | ((opc) & 0xff)));	\
} while (0)

#define S390_VRV(c,opc,v1,v2,b2,d2,m3) do				\
{									\
	char rxb = (((v1) > 15) << 7) | (((v2) > 15) << 6);		\
	s390_emit16(c, (((opc) & 0xff00) << 8) | ((v1) << 4) | ((v2)));	\
	s390_emit16(c, ((b2) << 12)| (d2));				\
	s390_emit16(c, (((m3) << 12) | ((rxb) << 8) | ((opc) & 0xff)));	\
} while (0)

#define S390_VRX(c,opc,v1,x2,b2,d2,m3) do				\
{									\
	char rxb = ((v1) > 15) << 7;					\
	s390_emit16(c, (((opc) & 0xff00) << 8) | ((v1) << 4) | ((x2)));	\
	s390_emit16(c, ((b2) << 12)| (d2));				\
	s390_emit16(c, (((m3) << 12) | ((rxb) << 8) | ((opc) & 0xff)));	\
} while (0)

#define s390_a(c, r, x, b, d)		S390_RX(c, 0x5a, r, x, b, d)
#define s390_adb(c, r, x, b, d)		S390_RXE(c, 0xed1a, r, x, b, d)
#define s390_adbr(c, r1, r2)		S390_RRE(c, 0xb31a, r1, r2)
#define s390_aebr(c, r1, r2)		S390_RRE(c, 0xb30a, r1, r2)
#define s390_afi(c, r, v)		S390_RIL_1(c, 0xc29, r, v);
#define s390_ag(c, r, x, b, d)		S390_RXY(c, 0xe308, r, x, b, d)
#define s390_agf(c, r, x, b, d)		S390_RXY(c, 0xe318, r, x, b, d)
#define s390_agfi(c, r, v)		S390_RIL_1(c, 0xc28, r, v)
#define s390_afgr(c, r1, r2)		S390_RRE(c, 0xb918, r1, r2)
#define s390_agh(c, r, x, b, d)		S390_RXY(c, 0xe338, r, b, d)
#define s390_aghi(c, r, v)		S390_RI(c, 0xa7b, r, v)
#define s390_aghik(c, r1, r3, v)	S390_RIE_1(c, 0xecd9, r1, r3, v)
#define s390_agr(c, r1, r2)		S390_RRE(c, 0xb908, r1, r2)
#define s390_agrk(c, r1, r2, r3)	S390_RRF_1(c, 0xb9e8, r1, r2, r3)
#define s390_agsi(c, r, v)		S390_SIY(c, 0xeb7a, r v)
#define s390_ahhhr(c, r1, r2, r3)	S390_RRF_1(c, 0xb9c8, r1, r2, r3)
#define s390_ahhlr(c, r1, r2, r3)	S390_RRF_1(c, 0xb9d8, r1, r2, r3)
#define s390_ahi(c, r, v)		S390_RI(c, 0xa7a, r, v)
#define s390_ahik(c, r1, r3, v)		S390_RIE_1(c, 0xecd8, r1, r3, v)
#define s390_ahy(c, r, x, b, d)		S390_RXY(c, 0xe37a, r, b, d)
#define s390_aih(c, r, v)		S390_RIL_1(c, 0xcc8, r, v)
#define s390_al(c, r, x, b, d)		S390_RX(c, 0x5e, r, x, b, d)
#define s390_alc(c, r, x, b, d)		S390_RXY(c, 0xe398, r, x, b, d)
#define s390_alcg(c, r, x, b, d)	S390_RXY(c, 0xe388, r, x, b, d)
#define s390_alcgr(c, r1, r2)		S390_RRE(c, 0xb988, r1, r2)
#define s390_alcr(c, r1, r2)		S390_RRE(c, 0xb998, r1, r2)
#define s390_alfi(c, r, v)		S390_RIL_1(c, 0xc2b, r, v)
#define s390_alg(c, r, x, b, d)		S390_RXY(c, 0xe30a, r, x, b, d)
#define s390_algf(c, r, x, b, d)	S390_RXY(c, 0xe31a, r, x, b, d)
#define s390_algfi(c, r, v)		S390_RIL_1(c, 0xc2a, r, v)
#define s390_algfr(c, r1, r2)		S390_RRE(c, 0xb91a, r1, r2)
#define s390_alghsik(c, r1, r3, v)	S390_RIE_1(c, 0xecdb, r1, r3, v)
#define s390_algr(c, r1, r2)		S390_RRE(c, 0xb90a, r1, r2)
#define s390_algrk(c, r1, r2, r3)	S390_RRF_1(c, 0xb9ea, r1, r2, r3)
#define s390_algsi(c, d1, b1, i2)	S390_SIY_1(c, 0xeb7e, d1, b1, i2)
#define s390_alhhhr(c, r1, r2, r3)	S390_RRF_1(c, 0xb9ca, r1, r2, r3)
#define s390_alhhlr(c, r1, r2, r3)	S390_RRF_1(c, 0xb9da, r1, r2, r3)
#define s390_alhsik(c, r1, r3, v)	S390_RIE_1(c, 0xecda, r1, r3, v)
#define s390_alr(c, r1, r2)		S390_RR(c, 0x1e, r1, r2)
#define s390_alrk(c, r1, r2)		S390_RRF(c, 0xb9fa, r1, r2)
#define s390_alsi(c, d1, b1, i2)	S390_SIY_1(c, 0xeb6e, d1, b1, i2)
#define s390_alsih(c, r, v)		S390_RIL_1(c, 0xcca, r, v)
#define s390_alsihn(c, r, v)		S390_RIL_1(c, 0xccb, r, v)
#define s390_aly(c, r, x, b, d)		S390_RXY(c, 0xe35e, r, x, b, d)
#define s390_ar(c, r1, r2)		S390_RR(c, 0x1a, r1, r2)
#define s390_ark(c, r1, r2, r3)		S390_RRF_1(c, 0xb9f8, r1, r2, r3)
#define s390_asi(c, r, v)		S390_SIY(c, 0xeb6a, r, v)
#define s390_axbr(c, r1, r2)		S390_RRE(c, 0xb34a, r1, r2)
#define s390_ay(c, r, x, b, d)		S390_RXY(c, 0xe35a, r, x, b, d)
#define s390_basr(c, r1, r2)		S390_RR(c, 0x0d, r1, r2)
#define s390_bctr(c, r1, r2)		S390_RR(c, 0x06, r1, r2)
#define s390_bctrg(c, r1, r2)		S390_RRE(c, 0xb946, r1, r2)
#define s390_bi(c, x, b, d)		S390_RXY(c, 0xe347, S390_CC_UN, x, b, d)
#define s390_bic(c, m, x, b, d)		S390_RXY(c, 0xe347, m, x, b, d)
#define s390_bnzr(c, r)			S390_RR(c, 0x07, 0x07, r)
#define s390_bras(c, r, o)		S390_RI(c, 0xa75, r, o)
#define s390_brasl(c, r, o)		S390_RIL_1(c, 0xc05, r, o)
#define s390_brc(c, m, d)		S390_RI(c, 0xa74, m, d)
#define s390_brcl(c, m, d)		S390_RIL_2(c, 0xc04, m, d)
#define s390_br(c, r)			S390_RR(c, 0x07, 0xf, r)
#define s390_break(c)			S390_RR(c, 0, 0, 0)
#define s390_bzr(c, r)			S390_RR(c, 0x07, 0x08, r)
#define s390_c(c, r, x, b, d)		S390_RX(c, 0x59, r, x, b, d)
#define s390_cdb(c, r, x, b, d)		S390_RXE(c, 0xed19, r, x, b, d)
#define s390_cdbr(c, r1, r2)		S390_RRE(c, 0xb319, r1, r2)
#define s390_cdfbr(c, r1, r2)		S390_RRE(c, 0xb395, r1, r2)
#define s390_cdgbr(c, r1, r2)		S390_RRE(c, 0xb3a5, r1, r2)
#define s390_cdlfbr(c, r1, m3, r2, m4)	S390_RRF_4(c, 0xb391, r1, m3, r2, m4)
#define s390_cdlgbr(c, r1, m3, r2, m4)	S390_RRF_4(c, 0xb3a1, r1, m3, r2, m4)
#define s390_cds(c, r1, r2, b, d)	S390_RX(c, 0xbb, r1, r2, b, d)
#define s390_cdsg(c, r1, r2, b, d)	S390_RSY_1(c, 0xeb3e, r1, r2, b, d)
#define s390_cdsy(c, r1, r2, b, d)	S390_RSY_1(c, 0xeb31, r1, r2, b, d)
#define s390_cebr(c, r1, r2)		S390_RRE(c, 0xb309, r1, r2)
#define s390_cegbr(c, r1, r2)		S390_RRE(c, 0xb3a4, r1, r2)
#define s390_cfdbr(c, r1, m, r2)	S390_RRF_2(c, 0xb399, r1, m, r2)
#define s390_cfi(c, r, v)		S390_RIL_1(c, 0xc2d, r, v)
#define s390_cgdbr(c, r1, m, r2)	S390_RRF_2(c, 0xb3a9, r1, m, r2)
#define s390_cg(c, r, x, b, d)		S390_RXY(c, 0xe320, r, x, b, d)
#define s390_cgfi(c, r, v)		S390_RIL_1(c, 0xc2c, r, v)
#define s390_cgfrl(c, r, v)		S390_RIL_1(c, 0xc6c, r, v)
#define s390_cghi(c, r, i)		S390_RI(c, 0xa7f, r, i)
#define s390_cgib(c, r, i, m, b, d)	S390_RIS(c, 0xecfc, r, i, m, b, d)
#define s390_cgij(c, r, i, m, d)	S390_RIE_3(c, 0xec7c, r, i, m, d)
#define s390_cgit(c, r, i, m)		S390_RIE_4(c, 0xec70, r, i m);
#define s390_cgr(c, r1, r2)		S390_RRE(c, 0xb920, r1, r2)
#define s390_cgrb(c, r1, r2, m3, b, d)	S390_RRS(c, 0xece4, r1, r2, m3, b, d)
#define s390_cgrj(c, r1, r2, m3, v)	S390_RIE_2(c, 0xec64, r1, r2, m3, v)
#define s390_cgrl(c, r, v)		S390_RIL_1(c, 0xc68, r, v)
#define s390_chi(c, r, i)		S390_RI(c, 0xa7e, r, i)
#define s390_cib(c, r, i, m, b, d)	S390_RIS(c, 0xecfe, r, i, m, b, d)
#define s390_cij(c, r, i, m, d)		S390_RIE_3(c, 0xec7e, r, i, m, d)
#define s390_cit(c, r, i, m)		S390_RIE_4(c, 0xec72, r, i m);
#define s390_cl(c, r, x, b, d)		S390_RX(c, 0x55, r, x, b, d)
#define s390_clfdbr(c, r1, m3, r2, m4)	S390_RRF_4(c, 0xb39d, r1, m3, r2, m4)
#define s390_clg(c, r, x, b, d)		S390_RXY(c, 0xe321, r, x, b, d)
#define s390_clgib(c, r, i, m, b, d)	S390_RIS(c, 0xecfd, r, i, m, b, d)
#define s390_clgij(c, r, i, b)		S390_RIE_3(c, 0xec7d, r, i, m, d)
#define s390_clgr(c, r1, r2)		S390_RRE(c, 0xb921, r1, r2)
#define s390_clgdbr(c, r1, m3, r2, m4)	S390_RRF_4(c, 0xb3ad, r1, m3, r2, m4)
#define s390_clgrj(c, r1, r2, m, v)	S390_RIE_2(c, 0xec65, r1, r2, m, v)
#define s390_clgrb(c, r1, r2, m3, b, d)	S390_RRS(c, 0xece5, r1, r2, m3, b, d)
#define s390_cli(c, b, d, v)		S390_SI(c, 0x95, b, d, v)
#define s390_clib(c, r, i, m, b, d)	S390_RIS(c, 0xecff, r, i, m, b, d)
#define s390_clij(c, r, i, b)		S390_RIE_3(c, 0xec7f, r, i, m, d)
#define s390_clr(c, r1, r2)		S390_RR(c, 0x15, r1, r2)
#define s390_clrb(c, r1, r2, m3, b, d)	S390_RRS(c, 0xecf7, r1, r2, m3, b, d)
#define s390_clrj(c, r1, r2, m, v)	S390_RIE_2(c, 0xec77, r1, r2, m, v)
#define s390_cr(c, r1, r2)		S390_RR(c, 0x19, r1, r2)
#define s390_crb(c, r1, r2, m3, b, d)	S390_RRS(c, 0xecf6, r1, r2, m3, b, d)
#define s390_crj(c, r1, r2, m3, v)	S390_RIE_2(c, 0xec76, r1, r2, m3, v)
#define s390_crl(c, r, v)		S390_RIL_1(c, 0xc6d, r, v)
#define s390_crt(c, r1, r2, m3)		S390_RRF_2(c, 0xb972, r1, r2, m3);
#define s390_cgrt(c, r1, r2, m3)	S390_RRF_2(c, 0xb960, r1, r2, m3);
#define s390_cs(c, r1, r2, b, d)	S390_RX(c, 0xba, r1, r2, b, d)
#define s390_csg(c, r1, r2, b, d)	S390_RSY_1(c, 0xeb30, r1, r2, b, d)
#define s390_csst(c, d1, b1, d2, b2, r)	S390_SSF(c, 0xc82, b1, d1, b2, d2, r)
#define s390_csy(c, r1, r2, b, d)	S390_RSY_1(c, 0xeb14, r1, r2, b, d)
#define s390_cxgbr(c, r1, r2)		S390_RRE(c, 0xb3a6, r1, r2)
#define s390_ddbr(c, r1, r2)		S390_RRE(c, 0xb31d, r1, r2)
#define s390_debr(c, r1, r2)		S390_RRE(c, 0xb30d, r1, r2)
#define s390_didbr(c, r1, r2, m, r3)    S390_RRF_3(c, 0xb35b, r1, r2, m, r3)
#define s390_dlgr(c, r1, r2)		S390_RRE(c, 0xb987, r1, r2)
#define s390_dlr(c, r1, r2)		S390_RRE(c, 0xb997, r1, r2)
#define s390_dr(c, r1, r2)		S390_RR(c, 0x1d, r1, r2)
#define s390_dsgfr(c, r1, r2)		S390_RRE(c, 0xb91d, r1, r2)
#define s390_dsgr(c, r1, r2)		S390_RRE(c, 0xb90d, r1, r2)
#define s390_ear(c, r1, r2)		S390_RRE(c, 0xb24f, r1, r2)
#define s390_ic(c, r, x, b, d)		S390_RX(c, 0x43, r, x, b, d)
#define s390_icm(c, r, m, b, d)		S390_RX(c, 0xbf, r, m, b, d)
#define s390_icmy(c, r, x, b, d)	S390_RXY(c, 0xeb81, r, x, b, d)
#define s390_icy(c, r, x, b, d)		S390_RXY(c, 0xe373, r, x, b, d)
#define s390_iihf(c, r, v)		S390_RIL_1(c, 0xc08, r, v)
#define s390_iihh(c, r, v)		S390_RI(c, 0xa50, r, v)
#define s390_iihl(c, r, v)		S390_RI(c, 0xa51, r, v)
#define s390_iilf(c, r, v)		S390_RIL_1(c, 0xc09, r, v)
#define s390_iilh(c, r, v)		S390_RI(c, 0xa52, r, v)
#define s390_iill(c, r, v)		S390_RI(c, 0xa53, r, v)
#define s390_j(c,d)			s390_brc(c, S390_CC_UN, d)
#define s390_jc(c, m, d)		s390_brc(c, m, d)
#define s390_jcl(c, m, d)		s390_brcl(c, m, d)
#define s390_jcy(c, d)			s390_brc(c, S390_CC_CY, d)
#define s390_je(c, d)			s390_brc(c, S390_CC_EQ, d)
#define s390_jeo(c, d)			s390_brc(c, S390_CC_ZR|S390_CC_OV, d)
#define s390_jh(c, d)			s390_brc(c, S390_CC_GT, d)
#define s390_jhe(c, d)			s390_brc(c, S390_CC_GT|S390_CC_EQ, d)
#define s390_jho(c, d)			s390_brc(c, S390_CC_GT|S390_CC_OV, d)
#define s390_jl(c, d)			s390_brc(c, S390_CC_LT, d)
#define s390_jle(c, d)			s390_brc(c, S390_CC_LT|S390_CC_EQ, d)
#define s390_jlo(c, d)			s390_brc(c, S390_CC_LT|S390_CC_OV, d)
#define s390_jm(c, d)			s390_brc(c, S390_CC_LT, d)
#define s390_jnc(c, d)			s390_brc(c, S390_CC_NC, d)
#define s390_jne(c, d)			s390_brc(c, S390_CC_NZ, d)
#define s390_jnh(c, d)			s390_brc(c, S390_CC_LE, d)
#define s390_jnl(c, d)			s390_brc(c, S390_CC_GE, d)
#define s390_jnz(c, d)			s390_brc(c, S390_CC_NZ, d)
#define s390_jo(c, d)			s390_brc(c, S390_CC_OV, d)
#define s390_jno(c, d)			s390_brc(c, S390_CC_NO, d)
#define s390_jp(c, d)			s390_brc(c, S390_CC_GT, d)
#define s390_jz(c, d)			s390_brc(c, S390_CC_ZR, d)
#define s390_jg(c,d)			s390_brcl(c, S390_CC_UN, d)
#define s390_jgcy(c, d)			s390_brcl(c, S390_CC_CY, d)
#define s390_jge(c, d)			s390_brcl(c, S390_CC_EQ, d)
#define s390_jgeo(c, d)			s390_brcl(c, S390_CC_ZR|S390_CC_OV, d)
#define s390_jgh(c, d)			s390_brcl(c, S390_CC_GT, d)
#define s390_jgho(c, d)			s390_brcl(c, S390_CC_GT|S390_CC_OV, d)
#define s390_jgl(c, d)			s390_brcl(c, S390_CC_LT, d)
#define s390_jglo(c, d)			s390_brcl(c, S390_CC_LT|S390_CC_OV, d)
#define s390_jgm(c, d)			s390_brcl(c, S390_CC_LT, d)
#define s390_jgnc(c, d)			s390_brcl(c, S390_CC_NC, d)
#define s390_jgne(c, d)			s390_brcl(c, S390_CC_NZ, d)
#define s390_jgnh(c, d)			s390_brcl(c, S390_CC_LE, d)
#define s390_jgnl(c, d)			s390_brcl(c, S390_CC_GE, d)
#define s390_jgnz(c, d)			s390_brcl(c, S390_CC_NZ, d)
#define s390_jgo(c, d)			s390_brcl(c, S390_CC_OV, d)
#define s390_jgno(c, d)			s390_brcl(c, S390_CC_NO, d)
#define s390_jgp(c, d)			s390_brcl(c, S390_CC_GT, d)
#define s390_jgz(c, d)			s390_brcl(c, S390_CC_ZR, d)
#define s390_l(c, r, x, b, d)		S390_RX(c, 0x58, r, x, b, d)
#define s390_ly(c, r, x, b, d)		S390_RXY(c, 0xe358, r, x, b, d)
#define s390_la(c, r, x, b, d)		S390_RX(c, 0x41, r, x, b, d)
#define s390_laa(c, r1, r3, b, d)	S390_RSY_1(c, 0xebf8, r1, r3, b, d)
#define s390_laag(c, r1, r3, b, d)	S390_RSY_1(c, 0xebe8, r1, r3, b, d)
#define s390_lay(c, r, x, b, d)		S390_RXY(c, 0xe371, r, x, b, d)
#define s390_lam(c, r1, r2, b, d)	S390_RS_1(c, 0x9a, r1, r2, b, d)
#define s390_larl(c, r, o)		S390_RIL_1(c, 0xc00, r, o)
#define s390_lb(c, r, x, b, d)		S390_RXY(c, 0xe376, r, x, b, d)
#define s390_lbr(c, r1, r2)		S390_RRE(c, 0xb926, r1, r2)
#define s390_lcdbr(c, r1, r2)		S390_RRE(c, 0xb313, r1, r2)
#define s390_lcgr(c, r1, r2)		S390_RRE(c, 0xb903, r1, r2)
#define s390_lcr(c, r1, r2)		S390_RR(c, 0x13, r1, r2)
#define s390_ld(c, f, x, b, d)		S390_RX(c, 0x68, f, x, b, d)
#define s390_ldy(c, r, x, b, d)		S390_RXY(c, 0xed65, r, x, b, d)
#define s390_ldeb(c, r, x, b, d)	S390_RXE(c, 0xed04, r, x, b, d)
#define s390_ldebr(c, r1, r2)		S390_RRE(c, 0xb304, r1, r2)
#define s390_ldgr(c, r1, r2)		S390_RRE(c, 0xb3c1, r1, r2)
#define s390_ldxbr(c, r1, r2)		S390_RRE(c, 0xb345, r1, r2)
#define s390_ldr(c, r1, r2)		S390_RR(c, 0x28, r1, r2)
#define s390_le(c, f, x, b, d)		S390_RX(c, 0x78, f, x, b, d)
#define s390_ledbr(c, r1, r2)		S390_RRE(c, 0xb344, r1, r2)
#define s390_ler(c, r1, r2)		S390_RR(c, 0x38, r1, r2)
#define s390_ley(c, r, x, b, d)		S390_RXY(c, 0xed64, r, x, b, d)
#define s390_lg(c, r, x, b, d)		S390_RXY(c, 0xe304, r, x, b, d)
#define s390_lgb(c, r, x, b, d)		S390_RXY(c, 0xe377, r, x, b, d)
#define s390_lgbr(c, r1, r2)		S390_RRE(c, 0xb906, r1, r2)
#define s390_lgdr(c, r1, r2)		S390_RRE(c, 0xb3cd, r1, r2)
#define s390_lgf(c, r, x, b, d)		S390_RXY(c, 0xe314, r, x, b, d)
#define s390_lgfi(c, r, v)		S390_RIL_1(c, 0xc01, r, v)
#define s390_lgfrl(c, r1, d)		S390_RIL_1(c, 0xc4c, r1, d)
#define s390_lgfr(c, r1, r2)		S390_RRE(c, 0xb914, r1, r2)
#define s390_lgg(c, r, x, b, d)		S390_RXY(c, 0xe34c, r, x, b, d)
#define s390_lgh(c, r, x, b, d)		S390_RXY(c, 0xe315, r, x, b, d)
#define s390_lghi(c, r, v)		S390_RI(c, 0xa79, r, v)
#define s390_lghr(c, r1, r2)		S390_RRE(c, 0xb907, r1, r2)
#define s390_lgr(c, r1, r2)		S390_RRE(c, 0xb904, r1, r2)
#define s390_lgrl(c, r1, d)		S390_RIL_1(c, 0xc48, r1, d)
#define s390_lgsc(c, r, x, b, d)	S390_RXY(c, 0xe34d, r, x, b, d)
#define s390_lh(c, r, x, b, d)		S390_RX(c, 0x48, r, x, b, d)
#define s390_lhr(c, r1, r2)		S390_RRE(c, 0xb927, r1, r2)
#define s390_lhg(c, r, x, b, d)		S390_RXY(c, 0xe315, r, x, b, d)
#define s390_lhi(c, r, v)		S390_RI(c, 0xa78, r, v)
#define s390_lhy(c, r, x, b, d)		S390_RXY(c, 0xe378, r, x, b, d)
#define s390_llcr(c, r1, r2)		S390_RRE(c, 0xb994, r1, r2)
#define s390_llgc(c, r, x, b, d)	S390_RXY(c, 0xe390, r, x, b, d)
#define s390_llgcr(c, r1, r2)		S390_RRE(c, 0xb984, r1, r2)
#define s390_llgf(c, r, x, b, d)	S390_RXY(c, 0xe316, r, x, b, d)
#define s390_llgfr(c, r1, r2)		S390_RRE(c, 0xb916, r1, r2)
#define s390_llgh(c, r, x, b, d)	S390_RXY(c, 0xe391, r, x, b, d)
#define s390_llghr(c, r1, r2)		S390_RRE(c, 0xb985, r1, r2)
#define s390_llgfsg(c, r, x, b, d)	S390_RXY(c, 0xe348, r, x, b, d)
#define s390_llhr(c, r1, r2)		S390_RRE(c, 0xb995, r1, r2)
#define s390_llihf(c, r, v)		S390_RIL_1(c, 0xc0e, r, v)
#define s390_llihh(c, r, v)		S390_RI(c, 0xa5c, r, v)
#define s390_llihl(c, r, v)		S390_RI(c, 0xa5d, r, v)
#define s390_llilf(c, r, v)		S390_RIL_1(c, 0xc0f, r, v)
#define s390_llilh(c, r, v)		S390_RI(c, 0xa5e, r, v)
#define s390_llill(c, r, v)		S390_RI(c, 0xa5f, r, v)
#define s390_lm(c, r1, r2, b, d)	S390_RS_1(c, 0x98, r1, r2, b, d)
#define s390_lmg(c, r1, r2, b, d)	S390_RSY_1(c, 0xeb04, r1, r2, b, d)
#define s390_lndbr(c, r1, r2)		S390_RRE(c, 0xb311, r1, r2)
#define s390_lngr(c, r1, r2)		S390_RRE(c, 0xb901, r1, r2)
#define s390_lnr(c, r1, r2)		S390_RR(c, 0x11, r1, r2)
#define s390_lpdbr(c, r1, r2)		S390_RRE(c, 0xb310, r1, r2)
#define s390_lpgr(c, r1, r2)		S390_RRE(c, 0xb900, r1, r2)
#define s390_lpr(c, r1, r2)		S390_RR(c, 0x10, r1, r2)
#define s390_lr(c, r1, r2)		S390_RR(c, 0x18, r1, r2)
#define s390_lrl(c, r1, d)		S390_RIL_1(c, 0xc4d, r1, d)
#define s390_lt(c, r, x, b, d)		S390_RXY(c, 0xe312, r, x, b, d)
#define s390_ltg(c, r, x, b, d)		S390_RXY(c, 0xe302, r, x, b, d)
#define s390_ltgfr(c, r1, r2)		S390_RRE(c, 0xb912, r1, r2)
#define s390_ltgr(c, r1, r2)		S390_RRE(c, 0xb902, r1, r2)
#define s390_ltr(c, r1, r2)		S390_RR(c, 0x12, r1, r2)
#define s390_lzdr(c, r)    		S390_RRE(c, 0xb375, r, 0)
#define s390_lzer(c, r)    		S390_RRE(c, 0xb374, r, 0)
#define s390_m(c, r, x, b, d)		S390_RX(c, 0x5c, r, x, b, d)
#define s390_mdbr(c, r1, r2)		S390_RRE(c, 0xb31c, r1, r2)
#define s390_meebr(c, r1, r2)		S390_RRE(c, 0xb317, r1, r2)
#define s390_mg(c, r, x, b, d)		S390_RXY(c, 0xe384, r, x, b, d)
#define s390_mgh(c, r, x, b, d)		S390_RXY(c, 0xe33c, r, x, b, d)
#define s390_mgrk(c, r1, r2, r3)	S390_RRF_1(c, 0xb9ec, r1, r2, r3)
#define s390_mfy(c, r, x, b, d)		S390_RXY(c, 0xe35c, r, x, b, d)
#define s390_mlgr(c, r1, r2)		S390_RRE(c, 0xb986, r1, r2)
#define s390_mlr(c, r1, r2)		S390_RRE(c, 0xb996, r1, r2)
#define s390_mr(c, r1, r2)		S390_RR(c, 0x1c, r1, r2)
#define s390_ms(c, r, x, b, d)		S390_RX(c, 0x71, r, x, b, d)
#define s390_msfi(c, r, v)		S390_RIL_1(c, 0xc20, r, v)
#define s390_msgfi(c, r, v)		S390_RIL_1(c, 0xc21, r, v)
#define s390_msgfr(c, r1, r2)		S390_RRE(c, 0xb91c, r1, r2)
#define s390_msgr(c, r1, r2)		S390_RRE(c, 0xb90c, r1, r2)
#define s390_msgrkc(c, r1, r2, r3)	S390_RRF_1(c, 0xb9ed, r1, r2, r3)
#define s390_msr(c, r1, r2)		S390_RRE(c, 0xb252, r1, r2)
#define s390_mvc(c, l, b1, d1, b2, d2)	S390_SS_1(c, 0xd2, l, b1, d1, b2, d2)
#define s390_mvcl(c, r1, r2)		S390_RR(c, 0x0e, r1, r2)
#define s390_mvcle(c, r1, r3, d2, b2)	S390_RS_1(c, 0xa8, r1, r3, d2, b2)
#define s390_mvi(c, b, d, v)		S390_SI(c, 0x92, b, d, v)
#define s390_n(c, r, x, b, d)		S390_RX(c, 0x54, r, x, b, d)
#define s390_nc(c, l, b1, d1, b2, d2)	S390_SS_1(c, 0xd4, l, b1, d1, b2, d2)
#define s390_ng(c, r, x, b, d)		S390_RXY(c, 0xe380, r, x, b, d)
#define s390_ngr(c, r1, r2)		S390_RRE(c, 0xb980, r1, r2)
#define s390_ngrk(c, r1, r2, r3)	S390_RRF_1(c, 0xb9e4, r1, r2, r3)
#define s390_ni(c, b, d, v)		S390_SI(c, 0x94, b, d, v) 
#define s390_nihf(c, r, v)		S390_RIL_1(c, 0xc0a, r, v)
#define s390_nihh(c, r, v)		S390_RI(c, 0xa54, r, v)
#define s390_nihl(c, r, v)		S390_RI(c, 0xa55, r, v)
#define s390_nilf(c, r, v)		S390_RIL_1(c, 0xc0b, r, v)
#define s390_nilh(c, r, v)		S390_RI(c, 0xa56, r, v)
#define s390_nill(c, r, v)		S390_RI(c, 0xa57, r, v)
#define s390_niy(c, b, d, v)		S390_SIY(c, 0xeb54, b, d, v) 
#define s390_nop(c)  			S390_RR(c, 0x07, 0x0, 0)
#define s390_mem(c)  			S390_RR(c, 0x07, 0xe, 0)
#define s390_nr(c, r1, r2)		S390_RR(c, 0x14, r1, r2)
#define s390_nrk(c, r1, r2)		S390_RRF_1(c, 0xb9f4, r1, r2)
#define s390_ny(c, r, x, b, d)		S390_RRY(c, 0xe354, r1, r2)
#define s390_o(c, r, x, b, d)		S390_RX(c, 0x56, r, x, b, d)
#define s390_oihf(c, r, v)		S390_RIL_1(c, 0xc0c, r, v)
#define s390_oihh(c, r, v)		S390_RI(c, 0xa58, r, v)
#define s390_oihl(c, r, v)		S390_RI(c, 0xa59, r, v)
#define s390_oilf(c, r, v)		S390_RIL_1(c, 0xc0d, r, v)
#define s390_oilh(c, r, v)		S390_RI(c, 0xa5a, r, v)
#define s390_oill(c, r, v)		S390_RI(c, 0xa5b, r, v)
#define s390_oiy(c, b, d, v)		S390_SIY(c, 0xeb56 b, d, v) 
#define s390_og(c, r, x, b, d)		S390_RXY(c, 0xe381, r, x, b, d)
#define s390_ogr(c, r1, r2)		S390_RRE(c, 0xb981, r1, r2)
#define s390_ogrk(c, r1, r2, r3)	S390_RRF_1(c, 0xb9e6, r1, r2, r3)
#define s390_or(c, r1, r2)		S390_RR(c, 0x16, r1, r2)
#define s390_s(c, r, x, b, d)		S390_RX(c, 0x5b, r, x, b, d)
#define s390_sdb(c, r, x, b, d)		S390_RXE(c, 0xed1b, r, x, b, d)
#define s390_sdbr(c, r1, r2)		S390_RRE(c, 0xb31b, r1, r2)
#define s390_sebr(c, r1, r2)		S390_RRE(c, 0xb30b, r1, r2)
#define s390_sg(c, r, x, b, d)		S390_RXY(c, 0xe309, r, x, b, d)
#define s390_sgf(c, r, x, b, d)		S390_RXY(c, 0xe319, r, x, b, d)
#define s390_sgh(c, r, x, b, d)		S390_RXY(c, 0xe339, r, x, b, d)
#define s390_sgr(c, r1, r2)		S390_RRE(c, 0xb909, r1, r2)
#define s390_sgrk(c, r1, r2, r3)	S390_RRF_1(c, 0xb9e9, r1, r2, r3)
#define s390_sl(c, r, x, b, d)		S390_RX(c, 0x5f, r, x, b, d)
#define s390_sla(c, r, b, d)		S390_RS_3(c, 0x8b, r, b, d) 
#define s390_slag(c, r1, r2, b, d)	S390_RSY_1(c, 0xeb0b, r1, r2, b, d) 
#define s390_slbg(c, r, x, b, d)	S390_RXY(c, 0xe389, r, x, b, d)
#define s390_slbgr(c, r1, r2)		S390_RRE(c, 0xb989, r1, r2)
#define s390_slbr(c, r1, r2)		S390_RRE(c, 0xb999, r1, r2)
#define s390_slda(c, r, b, d)		S390_RS_3(c, 0x8f, r, b, d) 
#define s390_sldl(c, r, b, d)		S390_RS_3(c, 0x8d, r, b, d) 
#define s390_slfi(c, r, v)		S390_RIL_1(c, 0xc25, r, v)
#define s390_slg(c, r, x, b, d)		S390_RXY(c, 0xe30b, r, x, b, d)
#define s390_slgf(c, r, x, b, d)	S390_RXY(c, 0xe31b, r, x, b, d)
#define s390_slgfr(c, r1, r2)		S390_RRE(c, 0xb91b, r1, r2)
#define s390_slgfi(c, r, v)		S390_RIL_1(c, 0xc24, r, v)
#define s390_slgr(c, r1, r2)		S390_RRE(c, 0xb90b, r1, r2)
#define s390_slgrk(c, r1, r2, r3)	S390_RRF_1(c, 0xb9eb, r1, r2, r3)
#define s390_sll(c, r, b, d)		S390_RS_3(c, 0x89, r, b, d) 
#define s390_sllg(c, r1, r2, b, d)	S390_RSY_1(c, 0xeb0d, r1, r2, b, d) 
#define s390_slr(c, r1, r2)		S390_RR(c, 0x1f, r1, r2)
#define s390_slrk(c, r1, r2, r3)	S390_RRF_1(c, 0xb9fb, r1, r2, r3)
#define s390_sqdbr(c, r1, r2)		S390_RRE(c, 0xb315, r1, r2)
#define s390_sqebr(c, r1, r2)		S390_RRE(c, 0xb314, r1, r2)
#define s390_sra(c, r, b, d)		S390_RS_3(c, 0x8a, r, b, d) 
#define s390_srag(c, r1, r2, b, d)	S390_RSY_1(c, 0xeb0a, r1, r2, b, d) 
#define s390_sr(c, r1, r2)		S390_RR(c, 0x1b, r1, r2)
#define s390_srda(c, r, b, d)		S390_RS_3(c, 0x8e, r, b, d) 
#define s390_srdl(c, r, b, d)		S390_RS_3(c, 0x8c, r, b, d) 
#define s390_srk(c, r1, r2, r3)		S390_RRF_1(c, 0xb9f9, r1, r2, r3)
#define s390_srl(c, r, b, d)		S390_RS_3(c, 0x88, r, b, d) 
#define s390_srlg(c, r1, r2, b, d)	S390_RSY_1(c, 0xeb0c, r1, r2, b, d) 
#define s390_st(c, r, x, b, d)		S390_RX(c, 0x50, r, x, b, d)
#define s390_stam(c, r1, r2, b, d)	S390_RS_1(c, 0x9b, r1, r2, b, d)
#define s390_stc(c, r, x, b, d)		S390_RX(c, 0x42, r, x, b, d)
#define s390_stcm(c, r, m, b, d)	S390_RX(c, 0xbe, r, m, b, d)
#define s390_stcmy(c, r, x, b, d)	S390_RXY(c, 0xeb2d, r, x, b, d)
#define s390_stcy(c, r, x, b, d)	S390_RXY(c, 0xe372, r, x, b, d)
#define s390_std(c, f, x, b, d)		S390_RX(c, 0x60, f, x, b, d)
#define s390_stdy(c, r, x, b, d)	S390_RXY(c, 0xed67, r, x, b, d)
#define s390_ste(c, f, x, b, d)		S390_RX(c, 0x70, f, x, b, d)
#define s390_stey(c, r, x, b, d)	S390_RXY(c, 0xed66, r, x, b, d)
#define s390_stfpc(c, b, d)		S390_S(c, 0xb29c, b, d)
#define s390_stg(c, r, x, b, d)		S390_RXY(c, 0xe324, r, x, b, d)
#define s390_stgsc(c, r, x, b, d)	S390_RXY(c, 0xe349, r, x, b, d)
#define s390_sth(c, r, x, b, d)		S390_RX(c, 0x40, r, x, b, d)
#define s390_sthy(c, r, x, b, d)	S390_RXY(c, 0xe370, r, x, b, d)
#define s390_stm(c, r1, r2, b, d)	S390_RS_1(c, 0x90, r1, r2, b, d)
#define s390_stmg(c, r1, r2, b, d)	S390_RSY_1(c, 0xeb24, r1, r2, b, d)
#define s390_sty(c, r, x, b, d)		S390_RXY(c, 0xe350, r, x, b, d)
#define s390_tcdb(c, r, x, b, d)	S390_RXE(c, 0xed11, r, x, b, d)
#define s390_tceb(c, r, x, b, d)	S390_RXE(c, 0xed10, r, x, b, d)
#define s390_tmhh(c, r, m)		S390_RI(c, 0xa73, r, m)
#define s390_tmhl(c, r, m)		S390_RI(c, 0xa72, r, m)
#define s390_tmlh(c, r, m)		S390_RI(c, 0xa70, r, m)
#define s390_tmll(c, r, m)		S390_RI(c, 0xa71, r, m)
#define s390_tm(c, b, d, v)		S390_SI(c, 0x91, b, d, v)
#define s390_x(c, r, x, b, d)		S390_RX(c, 0x57, r, x, b, d)
#define s390_xihf(c, r, v)		S390_RIL_1(c, 0xc06, r, v)
#define s390_xilf(c, r, v)		S390_RIL_1(c, 0xc07, r, v)
#define s390_xg(c, r, x, b, d)		S390_RXY(c, 0xe382, r, x, b, d)
#define s390_xgr(c, r1, r2)		S390_RRE(c, 0xb982, r1, r2)
#define s390_xgrk(c, r1, r2, r3)	S390_RRF_1(c, 0xb9e7, r1, r2, r3)
#define s390_xr(c, r1, r2)		S390_RR(c, 0x17, r1, r2)
#define s390_xy(c, r, x, b, d)		S390_RXY(c, 0xe357, r, x, b, d)
#endif
