#ifndef __SPARC_CODEGEN_H__
#define __SPARC_CODEGEN_H__

#if SIZEOF_VOID_P == 8
#define SPARCV9 1
#else
#endif

typedef enum {
	sparc_r0 = 0,
	sparc_r1 = 1,
	sparc_r2 = 2,
	sparc_r3 = 3,
	sparc_r4 = 4,
	sparc_r5 = 5,
	sparc_r6 = 6,
	sparc_r7 = 7,
	sparc_r8 = 8,
	sparc_r9 = 9,
	sparc_r10 = 10,
	sparc_r11 = 11,
	sparc_r12 = 12,
	sparc_r13 = 13,
	sparc_r14 = 14,
	sparc_r15 = 15,
	sparc_r16 = 16,
	sparc_r17 = 17,
	sparc_r18 = 18,
	sparc_r19 = 19,
	sparc_r20 = 20,
	sparc_r21 = 21,
	sparc_r22 = 22,
	sparc_r23 = 23,
	sparc_r24 = 24,
	sparc_r25 = 25,
	sparc_r26 = 26,
	sparc_r27 = 27,
	sparc_r28 = 28,
	sparc_r29 = 29,
	sparc_r30 = 30,
	sparc_r31 = 31,
	/* aliases */
	/* global registers */
	sparc_g0 = 0, sparc_zero = 0,
	sparc_g1 = 1,
	sparc_g2 = 2,
	sparc_g3 = 3,
	sparc_g4 = 4,
	sparc_g5 = 5,
	sparc_g6 = 6,
	sparc_g7 = 7,
	/* out registers */
	sparc_o0 = 8,
	sparc_o1 = 9,
	sparc_o2 = 10,
	sparc_o3 = 11,
	sparc_o4 = 12,
	sparc_o5 = 13,
	sparc_o6 = 14, sparc_sp = 14,
	sparc_o7 = 15, sparc_callsite = 15,
	/* local registers */
	sparc_l0 = 16,
	sparc_l1 = 17,
	sparc_l2 = 18,
	sparc_l3 = 19,
	sparc_l4 = 20,
	sparc_l5 = 21,
	sparc_l6 = 22,
	sparc_l7 = 23,
	/* in registers */
	sparc_i0 = 24,
	sparc_i1 = 25,
	sparc_i2 = 26,
	sparc_i3 = 27,
	sparc_i4 = 28,
	sparc_i5 = 29,
	sparc_i6 = 30, sparc_fp = 30,
	sparc_i7 = 31,
	sparc_nreg = 32,
	/* floating point registers */
	sparc_f0 = 0,
	sparc_f1 = 1,
	sparc_f2 = 2,
	sparc_f3 = 3,
	sparc_f4 = 4,
	sparc_f5 = 5,
	sparc_f6 = 6,
	sparc_f7 = 7,
	sparc_f8 = 8,
	sparc_f9 = 9,
	sparc_f10 = 10,
	sparc_f11 = 11,
	sparc_f12 = 12,
	sparc_f13 = 13,
	sparc_f14 = 14,
	sparc_f15 = 15,
	sparc_f16 = 16,
	sparc_f17 = 17,
	sparc_f18 = 18,
	sparc_f19 = 19,
	sparc_f20 = 20,
	sparc_f21 = 21,
	sparc_f22 = 22,
	sparc_f23 = 23,
	sparc_f24 = 24,
	sparc_f25 = 25,
	sparc_f26 = 26,
	sparc_f27 = 27,
	sparc_f28 = 28,
	sparc_f29 = 29,
	sparc_f30 = 30,
	sparc_f31 = 31,
} SparcRegister;

typedef enum {
	sparc_bn   = 0, sparc_bnever = 0,
	sparc_be   = 1,
	sparc_ble  = 2,
	sparc_bl   = 3,
	sparc_bleu = 4,
	sparc_bcs  = 5, sparc_blu = 5,
	sparc_bneg = 6,
	sparc_bvs  = 7, sparc_boverflow = 7,
	sparc_ba   = 8, sparc_balways = 8,
	sparc_bne  = 9,
	sparc_bg   = 10,
	sparc_bge  = 11,
	sparc_bgu  = 12,
	sparc_bcc  = 13, sparc_beu = 13,
	sparc_bpos = 14,
	sparc_bvc  = 15
} SparcCond;

typedef enum {
	/* with fcmp */
	sparc_feq = 0,
	sparc_fl  = 1,
	sparc_fg  = 2,
	sparc_unordered = 3,
	/* branch ops */
	sparc_fba   = 8,
	sparc_fbn   = 0,
	sparc_fbu   = 7,
	sparc_fbg   = 6,
	sparc_fbug  = 5,
	sparc_fbl   = 4,
	sparc_fbul  = 3,
	sparc_fblg  = 2,
	sparc_fbne  = 1,
	sparc_fbe   = 9,
	sparc_fbue  = 10,
	sparc_fbge  = 11,
	sparc_fbuge = 12,
	sparc_fble  = 13,
	sparc_fbule = 14,
	sparc_fbo   = 15
} SparcFCond;

typedef enum {
	sparc_icc = 4,
    sparc_xcc = 6,
    sparc_fcc0 = 0,
	sparc_fcc1 = 1,
	sparc_fcc2 = 2,
	sparc_fcc3 = 3
} SparcCC;

typedef enum {
	sparc_icc_short = 0,
    sparc_xcc_short = 2
} SparcCCShort;

typedef enum {
	/* fop1 format */
	sparc_fitos_val = 196,
	sparc_fitod_val = 200,
	sparc_fitoq_val = 204,
	sparc_fxtos_val = 132,
	sparc_fxtod_val = 136,
	sparc_fxtoq_val = 140,
	sparc_fstoi_val = 209,
	sparc_fdtoi_val = 210,
	sparc_fqtoi_val = 211,
	sparc_fstod_val = 201,
	sparc_fstoq_val = 205,
	sparc_fdtos_val = 198,
	sparc_fdtoq_val = 206,
	sparc_fqtos_val = 199,
	sparc_fqtod_val = 203,
	sparc_fmovs_val  = 1,
	sparc_fmovd_val  = 2,
	sparc_fnegs_val  = 5,
	sparc_fnegd_val  = 6,
	sparc_fabss_val  = 9,
	sparc_fabsd_val  = 10,
	sparc_fsqrts_val = 41,
	sparc_fsqrtd_val = 42,
	sparc_fsqrtq_val = 43,
	sparc_fadds_val  = 65,
	sparc_faddd_val  = 66,
	sparc_faddq_val  = 67,
	sparc_fsubs_val  = 69,
	sparc_fsubd_val  = 70,
	sparc_fsubq_val  = 71,
	sparc_fmuls_val  = 73,
	sparc_fmuld_val  = 74,
	sparc_fmulq_val  = 75,
	sparc_fsmuld_val = 105,
	sparc_fdmulq_val = 111,
	sparc_fdivs_val  = 77,
	sparc_fdivd_val  = 78,
	sparc_fdivq_val  = 79,
	/* fop2 format */
	sparc_fcmps_val  = 81,
	sparc_fcmpd_val  = 82,
	sparc_fcmpq_val  = 83,
	sparc_fcmpes_val = 85,
	sparc_fcmped_val = 86,
	sparc_fcmpeq_val = 87
} SparcFOp;

typedef enum {
	sparc_membar_load_load = 0x1,
	sparc_membar_store_load = 0x2,
	sparc_membar_load_store = 0x4,
	sparc_membar_store_store = 0x8,
   
	sparc_membar_lookaside = 0x10,
	sparc_membar_memissue = 0x20,
	sparc_membar_sync = 0x40,

    sparc_membar_all = 0x4f
} SparcMembarFlags;

typedef struct {
	unsigned int op   : 2; /* always 1 */
	unsigned int disp : 30;
} sparc_format1;

typedef struct {
	unsigned int op   : 2; /* always 0 */
	unsigned int rd   : 5;
	unsigned int op2  : 3;
	unsigned int disp : 22;
} sparc_format2a;

typedef struct {
	unsigned int op   : 2; /* always 0 */
	unsigned int a    : 1;
	unsigned int cond : 4;
	unsigned int op2  : 3;
	unsigned int disp : 22;
} sparc_format2b;

typedef struct {
	unsigned int op   : 2; /* always 0 */
	unsigned int a    : 1;
	unsigned int cond : 4;
	unsigned int op2  : 3;
	unsigned int cc01 : 2;
	unsigned int p    : 1;
	unsigned int d19  : 19;
} sparc_format2c;

typedef struct {
	unsigned int op   : 2; /* always 0 */
	unsigned int a    : 1;
	unsigned int res  : 1;
	unsigned int rcond: 3;
	unsigned int op2  : 3;
	unsigned int d16hi: 2;
	unsigned int p    : 1;
	unsigned int rs1  : 5;
	unsigned int d16lo: 14;
} sparc_format2d;

typedef struct {
	unsigned int op   : 2; /* 2 or 3 */
	unsigned int rd   : 5;
	unsigned int op3  : 6;
	unsigned int rs1  : 5;
	unsigned int i    : 1;
	unsigned int asi  : 8;
	unsigned int rs2  : 5;
} sparc_format3a;

typedef struct {
	unsigned int op   : 2; /* 2 or 3 */
	unsigned int rd   : 5;
	unsigned int op3  : 6;
	unsigned int rs1  : 5;
	unsigned int i    : 1;
	unsigned int x    : 1;
	unsigned int asi  : 7;
	unsigned int rs2  : 5;
} sparc_format3ax;

typedef struct {
	unsigned int op   : 2; /* 2 or 3 */
	unsigned int rd   : 5;
	unsigned int op3  : 6;
	unsigned int rs1  : 5;
	unsigned int i    : 1;
	unsigned int imm  : 13;
} sparc_format3b;

typedef struct {
	unsigned int op   : 2; /* 2 or 3 */
	unsigned int rd   : 5;
	unsigned int op3  : 6;
	unsigned int rs1  : 5;
	unsigned int i    : 1;
	unsigned int x    : 1;
	unsigned int imm  : 12;
} sparc_format3bx;

typedef struct {
	unsigned int op   : 2; /* 2 or 3 */
	unsigned int rd   : 5;
	unsigned int op3  : 6;
	unsigned int rs1  : 5;
	unsigned int opf  : 9;
	unsigned int rs2  : 5;
} sparc_format3c;

typedef struct {
	unsigned int op   : 2;
	unsigned int rd   : 5;
	unsigned int op3  : 6;
	unsigned int rs1  : 5;
	unsigned int i    : 1;
	unsigned int cc01 : 2;
	unsigned int res  : 6;
	unsigned int rs2  : 5;
} sparc_format4a;

typedef struct {
	unsigned int op   : 2;
	unsigned int rd   : 5;
	unsigned int op3  : 6;
	unsigned int rs1  : 5;
	unsigned int i    : 1;
	unsigned int cc01 : 2;
	unsigned int simm : 11;
} sparc_format4b;

typedef struct {
	unsigned int op   : 2;
	unsigned int rd   : 5;
	unsigned int op3  : 6;
	unsigned int cc2  : 1;
	unsigned int cond : 4;
	unsigned int i    : 1;
	unsigned int cc01 : 2;
	unsigned int res  : 6;
	unsigned int rs2  : 5;
} sparc_format4c;

typedef struct {
	unsigned int op   : 2;
	unsigned int rd   : 5;
	unsigned int op3  : 6;
	unsigned int cc2  : 1;
	unsigned int cond : 4;
	unsigned int i    : 1;
	unsigned int cc01 : 2;
	unsigned int simm : 11;
} sparc_format4d;

/* for use in logical ops, use 0 to not set flags */
#define sparc_cc 16

#define sparc_is_imm13(val) ((glong)val >= (glong)-(1<<12) && (glong)val <= (glong)((1<<12)-1))
#define sparc_is_imm22(val) ((glong)val >= (glong)-(1<<21) && (glong)val <= (glong)((1<<21)-1))
#define sparc_is_imm16(val) ((glong)val >= (glong)-(1<<15) && (glong)val <= (glong)((1<<15)-1))
#define sparc_is_imm19(val) ((glong)val >= (glong)-(1<<18) && (glong)val <= (glong)((1<<18)-1))
#define sparc_is_imm30(val) ((glong)val >= (glong)-(1<<29) && (glong)val <= (glong)((1<<29)-1))

/* disassembly */
#define sparc_inst_op(inst) ((inst) >> 30)
#define sparc_inst_op2(inst) (((inst) >> 22) & 0x7)
#define sparc_inst_rd(inst) (((inst) >> 25) & 0x1f)
#define sparc_inst_op3(inst) (((inst) >> 19) & 0x3f)
#define sparc_inst_i(inst) (((inst) >> 13) & 0x1)
#define sparc_inst_rs1(inst) (((inst) >> 14) & 0x1f)
#define sparc_inst_rs2(inst) (((inst) >> 0) & 0x1f)
#define sparc_inst_imm(inst) (((inst) >> 13) & 0x1)
#define sparc_inst_imm13(inst) (((inst) >> 0) & 0x1fff)

#define sparc_encode_call(ins,addr) \
	do {	\
		sparc_format1 *__f = (sparc_format1*)(ins);	\
		__f->op = 1;	\
		__f->disp = ((unsigned int)(addr) >> 2);	\
		(ins) = (unsigned int*)__f + 1;	\
	} while (0)

#define sparc_encode_format2a(ins,val,oper,dest) \
	do {	\
		sparc_format2a *__f = (sparc_format2a*)(ins);	\
		__f->op = 0;	\
		__f->rd = (dest);	\
		__f->op2 = (oper);	\
		__f->disp = (val) & 0x3fffff;	\
		(ins) = (unsigned int*)__f + 1;	\
	} while (0)

#define sparc_encode_format2b(ins,aval,bcond,oper,disp22) \
	do {	\
		sparc_format2b *__f = (sparc_format2b*)(ins);	\
		__f->op = 0;	\
		__f->a = (aval);	\
		__f->cond = (bcond);	\
		__f->op2 = (oper);	\
		__f->disp = (disp22);	\
		(ins) = (unsigned int*)__f + 1;	\
	} while (0)

#define sparc_encode_format2c(ins,aval,bcond,oper,xcc,predict,disp19) \
	do {	\
		sparc_format2c *__f = (sparc_format2c*)(ins);	\
		__f->op = 0;	\
		__f->a = (aval);	\
		__f->cond = (bcond);	\
		__f->op2 = (oper);	\
        __f->cc01 = (xcc); \
        __f->p = (predict); \
        __f->d19 = (disp19); \
		(ins) = (unsigned int*)__f + 1;	\
	} while (0)

#define sparc_encode_format2d(ins,aval,bcond,oper,predict,r1,disp16) \
	do {	\
		sparc_format2d *__f = (sparc_format2d*)(ins);	\
		__f->op = 0;	\
		__f->a = (aval);	\
        __f->res = 0;       \
		__f->rcond = (bcond);	\
		__f->op2 = (oper);	\
        __f->d16hi = ((disp16) >> 14); \
        __f->p = (predict); \
        __f->rs1 = (r1);    \
		__f->d16lo = ((disp16) & 0x3fff);	\
		(ins) = (unsigned int*)__f + 1;	\
	} while (0)

#define sparc_encode_format3a(ins,opval,asival,r1,r2,oper,dest) \
	do {	\
		sparc_format3a *__f = (sparc_format3a*)(ins);	\
		__f->op = (opval);	\
		__f->asi = (asival);	\
		__f->i = 0;	\
		__f->rd = (dest);	\
		__f->rs1 = (r1);	\
		__f->rs2 = (r2);	\
		__f->op3 = (oper);	\
		(ins) = (unsigned int*)__f + 1;	\
	} while (0)

#define sparc_encode_format3ax(ins,opval,asival,r1,r2,oper,dest) \
	do {	\
		sparc_format3ax *__f = (sparc_format3ax*)(ins);	\
		__f->op = (opval);	\
		__f->asi = (asival);	\
		__f->i = 0;	\
		__f->x = 1;	\
		__f->rd = (dest);	\
		__f->rs1 = (r1);	\
		__f->rs2 = (r2);	\
		__f->op3 = (oper);	\
		(ins) = (unsigned int*)__f + 1;	\
	} while (0)

#define sparc_encode_format3b(ins,opval,r1,val,oper,dest) \
	do {	\
		sparc_format3b *__f = (sparc_format3b*)(ins);	\
		__f->op = (opval);	\
		__f->imm = (val);	\
		__f->i = 1;	\
		__f->rd = (dest);	\
		__f->rs1 = (r1);	\
		__f->op3 = (oper);	\
		(ins) = (unsigned int*)__f + 1;	\
	} while (0)

#define sparc_encode_format3bx(ins,opval,r1,val,oper,dest) \
	do {	\
		sparc_format3bx *__f = (sparc_format3bx*)(ins);	\
		__f->op = (opval);	\
		__f->imm = (val);	\
		__f->i = 1;	\
		__f->x = 1;	\
		__f->rd = (dest);	\
		__f->rs1 = (r1);	\
		__f->op3 = (oper);	\
		(ins) = (unsigned int*)__f + 1;	\
	} while (0)

#define sparc_encode_format3c(ins,opval,opfval,r1,oper,r2,dest) \
	do {	\
		sparc_format3c *__f = (sparc_format3c*)(ins);	\
		__f->op = (opval);	\
		__f->opf = (opfval);	\
		__f->rd = (dest);	\
		__f->rs1 = (r1);	\
		__f->rs2 = (r2);	\
		__f->op3 = (oper);	\
		(ins) = (unsigned int*)__f + 1;	\
	} while (0)

#define sparc_encode_format4a(ins,opval,oper,cc,r1,r2,dest) \
	do {	\
		sparc_format4a *__f = (sparc_format4a*)(ins);	\
		__f->op = (opval);	\
		__f->rd = (dest);	\
		__f->op3 = (oper);	\
		__f->rs1 = (r1);	\
        __f->i   = 0;       \
        __f->cc01= (cc) & 0x3; \
        __f->res = 0;       \
		__f->rs2 = (r2);	\
		(ins) = (unsigned int*)__f + 1;	\
	} while (0)

#define sparc_encode_format4b(ins,opval,oper,cc,r1,imm,dest) \
	do {	\
		sparc_format4b *__f = (sparc_format4b*)(ins);	\
		__f->op = (opval);	\
		__f->rd = (dest);	\
		__f->op3 = (oper);	\
		__f->rs1 = (r1);	\
        __f->i   = 1;       \
        __f->cc01= (cc) & 0x3; \
		__f->simm = (imm);	\
		(ins) = (unsigned int*)__f + 1;	\
	} while (0)

#define sparc_encode_format4c(ins,opval,oper,cc,bcond,r2,dest) \
	do {	\
		sparc_format4c *__f = (sparc_format4c*)(ins);	\
		__f->op = (opval);	\
		__f->rd = (dest);	\
		__f->op3 = (oper);	\
        __f->cc2 = ((xcc) >> 2) & 0x1; \
        __f->cond = bcond;  \
        __f->i   = 0;       \
        __f->cc01= (xcc) & 0x3; \
        __f->res = 0;       \
		__f->rs2 = (r2);	\
		(ins) = (unsigned int*)__f + 1;	\
	} while (0)

#define sparc_encode_format4d(ins,opval,oper,xcc,bcond,imm,dest) \
	do {	\
		sparc_format4d *__f = (sparc_format4d*)(ins);	\
		__f->op = (opval);	\
		__f->rd = (dest);	\
		__f->op3 = (oper);	\
        __f->cc2 = ((xcc) >> 2) & 0x1; \
        __f->cond = bcond;  \
        __f->i   = 1;       \
        __f->cc01= (xcc) & 0x3; \
		__f->simm = (imm);	\
		(ins) = (unsigned int*)__f + 1;	\
	} while (0)

/* is it useful to provide a non-default value? */
#define sparc_asi 0x0

/* load */
#define sparc_ldsb(ins,base,disp,dest) sparc_encode_format3a((ins),3,sparc_asi,(base),(disp),9,(dest))
#define sparc_ldsb_imm(ins,base,disp,dest) sparc_encode_format3b((ins),3,(base),(disp),9,(dest))

#define sparc_ldsh(ins,base,disp,dest) sparc_encode_format3a((ins),3,sparc_asi,(base),(disp),10,(dest))
#define sparc_ldsh_imm(ins,base,disp,dest) sparc_encode_format3b((ins),3,(base),(disp),10,(dest))

#define sparc_ldub(ins,base,disp,dest) sparc_encode_format3a((ins),3,sparc_asi,(base),(disp),1,(dest))
#define sparc_ldub_imm(ins,base,disp,dest) sparc_encode_format3b((ins),3,(base),(disp),1,(dest))

#define sparc_lduh(ins,base,disp,dest) sparc_encode_format3a((ins),3,sparc_asi,(base),(disp),2,(dest))
#define sparc_lduh_imm(ins,base,disp,dest) sparc_encode_format3b((ins),3,(base),(disp),2,(dest))

#define sparc_ld(ins,base,disp,dest) sparc_encode_format3a((ins),3,sparc_asi,(base),(disp),0,(dest))
#define sparc_ld_imm(ins,base,disp,dest) sparc_encode_format3b((ins),3,(base),(disp),0,(dest))

/* Sparc V9 */
#define sparc_ldx(ins,base,disp,dest) sparc_encode_format3a((ins),3,sparc_asi,(base),(disp),11,(dest))
#define sparc_ldx_imm(ins,base,disp,dest) sparc_encode_format3b((ins),3,(base),(disp),11,(dest))

#define sparc_ldsw(ins,base,disp,dest) sparc_encode_format3a((ins),3,sparc_asi,(base),(disp),8,(dest))
#define sparc_ldsw_imm(ins,base,disp,dest) sparc_encode_format3b((ins),3,(base),(disp),8,(dest))

#define sparc_ldd(ins,base,disp,dest) sparc_encode_format3a((ins),3,sparc_asi,(base),(disp),3,(dest))
#define sparc_ldd_imm(ins,base,disp,dest) sparc_encode_format3b((ins),3,(base),(disp),3,(dest))

#define sparc_ldf(ins,base,disp,dest) sparc_encode_format3a((ins),3,0,(base),(disp),32,(dest))
#define sparc_ldf_imm(ins,base,disp,dest) sparc_encode_format3b((ins),3,(base),(disp),32,(dest))

#define sparc_lddf(ins,base,disp,dest) sparc_encode_format3a((ins),3,0,(base),(disp),35,(dest))
#define sparc_lddf_imm(ins,base,disp,dest) sparc_encode_format3b((ins),3,(base),(disp),35,(dest))

/* store */
#define sparc_stb(ins,src,base,disp) sparc_encode_format3a((ins),3,sparc_asi,(base),(disp),5,(src))
#define sparc_stb_imm(ins,src,base,disp) sparc_encode_format3b((ins),3,(base),(disp),5,(src))

#define sparc_sth(ins,src,base,disp) sparc_encode_format3a((ins),3,sparc_asi,(base),(disp),6,(src))
#define sparc_sth_imm(ins,src,base,disp) sparc_encode_format3b((ins),3,(base),(disp),6,(src))

#define sparc_st(ins,src,base,disp) sparc_encode_format3a((ins),3,sparc_asi,(base),(disp),4,(src))
#define sparc_st_imm(ins,src,base,disp) sparc_encode_format3b((ins),3,(base),(disp),4,(src))

/* Sparc V9 */
#define sparc_stx(ins,src,base,disp) sparc_encode_format3a((ins),3,sparc_asi,(base),(disp),14,(src))
#define sparc_stx_imm(ins,src,base,disp) sparc_encode_format3b((ins),3,(base),(disp),14,(src))

#define sparc_std(ins,src,base,disp) sparc_encode_format3a((ins),3,sparc_asi,(base),(disp),7,(src))
#define sparc_std_imm(ins,src,base,disp) sparc_encode_format3b((ins),3,(base),(disp),7,(src))

#define sparc_stf(ins,src,base,disp) sparc_encode_format3a((ins),3,sparc_asi,(base),(disp),36,(src))
#define sparc_stf_imm(ins,src,base,disp) sparc_encode_format3b((ins),3,(base),(disp),36,(src))

#define sparc_stdf(ins,src,base,disp) sparc_encode_format3a((ins),3,sparc_asi,(base),(disp),39,(src))
#define sparc_stdf_imm(ins,src,base,disp) sparc_encode_format3b((ins),3,(base),(disp),39,(src))

/* swap */
#define sparc_ldstub(ins,base,disp,dest) sparc_encode_format3a((ins),3,sparc_asi,(base),(disp),13,(dest))
#define sparc_ldstub_imm(ins,base,disp,dest) sparc_encode_format3b((ins),3,(base),(disp),13,(dest))

#define sparc_swap(ins,base,disp,dest) sparc_encode_format3a((ins),3,sparc_asi,(base),(disp),15,(dest))
#define sparc_swap_imm(ins,base,disp,dest) sparc_encode_format3b((ins),3,(base),(disp),15,(dest))

/* misc */
/* note: with sethi val is the full 32 bit value (think of it as %hi(val)) */
#define sparc_sethi(ins,val,dest) sparc_encode_format2a((ins),((val)>>10),4,(dest))

#define sparc_nop(ins) sparc_sethi((ins),0,sparc_zero)

#define sparc_save(ins,src,disp,dest) sparc_encode_format3a((ins),2,0,(src),(disp),60,(dest))
#define sparc_save_imm(ins,src,disp,dest) sparc_encode_format3b((ins),2,(src),(disp),60,(dest))

#define sparc_restore(ins,src,disp,dest) sparc_encode_format3a((ins),2,0,(src),(disp),61,(dest))
#define sparc_restore_imm(ins,src,disp,dest) sparc_encode_format3b((ins),2,(src),(disp),61,(dest))

#define sparc_rett(ins,src,disp) sparc_encode_format3a((ins),2,0,(src),(disp),0x39,0)
#define sparc_rett_imm(ins,src,disp) sparc_encode_format3b((ins),2,(src),(disp),0x39,0)

#define sparc_jmpl(ins,base,disp,dest) sparc_encode_format3a((ins),2,0,(base),(disp),56,(dest))
#define sparc_jmpl_imm(ins,base,disp,dest) sparc_encode_format3b((ins),2,(base),(disp),56,(dest))

#define sparc_call_simple(ins,disp) sparc_encode_call((ins),((unsigned int)(disp)))

#define sparc_rdy(ins,dest) sparc_encode_format3a((ins),2,0,0,0,40,(dest))

#define sparc_wry(ins,base,disp) sparc_encode_format3a((ins),2,0,(base),(disp),48,0)
#define sparc_wry_imm(ins,base,disp) sparc_encode_format3b((ins),2,(base),(disp),48,0)

/* stbar, unimp, flush */
#define sparc_stbar(ins) sparc_encode_format3a((ins),2,0,15,0,40,0)
#define sparc_unimp(ins,val) sparc_encode_format2b((ins),0,0,0,(val))

#define sparc_flush(ins,base,disp) sparc_encode_format3a((ins),2,0,(base),(disp),59,0)
#define sparc_flush_imm(ins,base,disp) sparc_encode_format3b((ins),2,(base),(disp),59,0)

#define sparc_flushw(ins) sparc_encode_format3a((ins),2,0,0,0,43,0)

#define sparc_membar(ins,flags) sparc_encode_format3b ((ins), 2, 0xf, (flags), 0x28, 0)

/* trap */

#define sparc_ta(ins,tt) sparc_encode_format3b((ins),2,0,(tt),58,0x8)

/* alu fop */
/* provide wrappers for: fitos, fitod, fstoi, fdtoi, fstod, fdtos, fmov, fneg, fabs */

#define sparc_fop(ins,r1,op,r2,dest) sparc_encode_format3c((ins),2,(op),(r1),52,(r2),(dest))
#define sparc_fcmp(ins,r1,op,r2) sparc_encode_format3c((ins),2,(op),(r1),53,(r2),0)

/* format 1 fops */
#define sparc_fadds(ins, r1, r2, dest) sparc_fop( ins, r1, sparc_fadds_val, r2, dest )
#define sparc_faddd(ins, r1, r2, dest) sparc_fop( ins, r1, sparc_faddd_val, r2, dest )
#define sparc_faddq(ins, r1, r2, dest) sparc_fop( ins, r1, sparc_faddq_val, r2, dest )

#define sparc_fsubs(ins, r1, r2, dest) sparc_fop( ins, r1, sparc_fsubs_val, r2, dest ) 
#define sparc_fsubd(ins, r1, r2, dest) sparc_fop( ins, r1, sparc_fsubd_val, r2, dest ) 
#define sparc_fsubq(ins, r1, r2, dest) sparc_fop( ins, r1, sparc_fsubq_val, r2, dest ) 

#define sparc_fmuls( ins, r1, r2, dest ) sparc_fop( ins, r1, sparc_fmuls_val, r2, dest )
#define sparc_fmuld( ins, r1, r2, dest ) sparc_fop( ins, r1, sparc_fmuld_val, r2, dest )
#define sparc_fmulq( ins, r1, r2, dest ) sparc_fop( ins, r1, sparc_fmulq_val, r2, dest )

#define sparc_fsmuld( ins, r1, r2, dest ) sparc_fop( ins, r1, sparc_fsmuld_val, r2, dest )
#define sparc_fdmulq( ins, r1, r2, dest ) sparc_fop( ins, r1, sparc_fdmulq_val, r2, dest )

#define sparc_fdivs( ins, r1, r2, dest ) sparc_fop( ins, r1, sparc_fdivs_val, r2, dest )
#define sparc_fdivd( ins, r1, r2, dest ) sparc_fop( ins, r1, sparc_fdivd_val, r2, dest )
#define sparc_fdivq( ins, r1, r2, dest ) sparc_fop( ins, r1, sparc_fdivq_val, r2, dest )

#define sparc_fitos( ins, r2, dest ) sparc_fop( ins, 0, sparc_fitos_val, r2, dest )
#define sparc_fitod( ins, r2, dest ) sparc_fop( ins, 0, sparc_fitod_val, r2, dest )
#define sparc_fitoq( ins, r2, dest ) sparc_fop( ins, 0, sparc_fitoq_val, r2, dest )

#define sparc_fxtos( ins, r2, dest) sparc_fop( ins, 0, sparc_fxtos_val, r2, dest )
#define sparc_fxtod( ins, r2, dest) sparc_fop( ins, 0, sparc_fxtod_val, r2, dest )
#define sparc_fxtoq( ins, r2, dest) sparc_fop( ins, 0, sparc_fxtoq_val, r2, dest )

#define sparc_fstoi( ins, r2, dest ) sparc_fop( ins, 0, sparc_fstoi_val, r2, dest )
#define sparc_fdtoi( ins, r2, dest ) sparc_fop( ins, 0, sparc_fdtoi_val, r2, dest )
#define sparc_fqtoi( ins, r2, dest ) sparc_fop( ins, 0, sparc_fqtoi_val, r2, dest )

#define sparc_fstod( ins, r2, dest ) sparc_fop( ins, 0, sparc_fstod_val, r2, dest )
#define sparc_fstoq( ins, r2, dest ) sparc_fop( ins, 0, sparc_fstoq_val, r2, dest )

#define sparc_fdtos( ins, r2, dest ) sparc_fop( ins, 0, sparc_fdtos_val, r2, dest )
#define sparc_fdtoq( ins, r2, dest ) sparc_fop( ins, 0, sparc_fdtoq_val, r2, dest )

#define sparc_fqtos( ins, r2, dest ) sparc_fop( ins, 0, sparc_fqtos_val, r2, dest )
#define sparc_fqtod( ins, r2, dest ) sparc_fop( ins, 0, sparc_fqtod_val, r2, dest )

#define sparc_fmovs( ins, r2, dest ) sparc_fop( ins, 0, sparc_fmovs_val, r2, dest )
#define sparc_fnegs( ins, r2, dest ) sparc_fop( ins, 0, sparc_fnegs_val, r2, dest )
#define sparc_fabss( ins, r2, dest ) sparc_fop( ins, 0, sparc_fabss_val, r2, dest )

#define sparc_fmovd( ins, r2, dest) sparc_fop (ins, 0, sparc_fmovd_val, r2, dest);
#define sparc_fnegd( ins, r2, dest) sparc_fop (ins, 0, sparc_fnegd_val, r2, dest);
#define sparc_fabsd( ins, r2, dest) sparc_fop (ins, 0, sparc_fabsd_val, r2, dest);

#define sparc_fsqrts( ins, r2, dest ) sparc_fop( ins, 0, sparc_fsqrts_val, r2, dest )
#define sparc_fsqrtd( ins, r2, dest ) sparc_fop( ins, 0, sparc_fsqrtd_val, r2, dest )
#define sparc_fsqrtq( ins, r2, dest ) sparc_fop( ins, 0, sparc_fsqrtq_val, r2, dest )

/* format 2 fops */

#define sparc_fcmps( ins, r1, r2 ) sparc_fcmp( ins, r1, sparc_fcmps_val, r2 )
#define sparc_fcmpd( ins, r1, r2 ) sparc_fcmp( ins, r1, sparc_fcmpd_val, r2 )
#define sparc_fcmpq( ins, r1, r2 ) sparc_fcmp( ins, r1, sparc_fcmpq_val, r2 )
#define sparc_fcmpes( ins, r1, r2 ) sparc_fcmpes( ins, r1, sparc_fcmpes_val, r2 )
#define sparc_fcmped( ins, r1, r2 ) sparc_fcmped( ins, r1, sparc_fcmped_val, r2 )
#define sparc_fcmpeq( ins, r1, r2 ) sparc_fcmpeq( ins, r1, sparc_fcmpeq_val, r2 ) 

/* logical */

/* FIXME: condense this using macros */
/* FIXME: the setcc stuff is wrong in lots of places */

#define sparc_logic(ins,op,setcc,r1,r2,dest) sparc_encode_format3a((ins),2,0,(r1),(r2),((setcc) ? 0x10 : 0) | (op), (dest))
#define sparc_logic_imm(ins,op,setcc,r1,imm,dest) sparc_encode_format3b((ins),2,(r1),(imm),((setcc) ? 0x10 : 0) | (op), (dest))

#define sparc_and(ins,setcc,r1,r2,dest) sparc_logic(ins,1,setcc,r1,r2,dest)
#define sparc_and_imm(ins,setcc,r1,imm,dest) sparc_logic_imm(ins,1,setcc,r1,imm,dest)

#define sparc_andn(ins,setcc,r1,r2,dest) sparc_encode_format3a((ins),2,0,(r1),(r2),(setcc)|5,(dest))
#define sparc_andn_imm(ins,setcc,r1,imm,dest) sparc_encode_format3b((ins),2,(r1),(imm),(setcc)|5,(dest))

#define sparc_or(ins,setcc,r1,r2,dest) sparc_encode_format3a((ins),2,0,(r1),(r2),(setcc)|2,(dest))
#define sparc_or_imm(ins,setcc,r1,imm,dest) sparc_encode_format3b((ins),2,(r1),(imm),(setcc)|2,(dest))

#define sparc_orn(ins,setcc,r1,r2,dest) sparc_encode_format3a((ins),2,0,(r1),(r2),(setcc)|6,(dest))
#define sparc_orn_imm(ins,setcc,r1,imm,dest) sparc_encode_format3b((ins),2,(r1),(imm),(setcc)|6,(dest))

#define sparc_xor(ins,setcc,r1,r2,dest) sparc_encode_format3a((ins),2,0,(r1),(r2),(setcc)|3,(dest))
#define sparc_xor_imm(ins,setcc,r1,imm,dest) sparc_encode_format3b((ins),2,(r1),(imm), (setcc)|3,(dest))

#define sparc_xnor(ins,setcc,r1,r2,dest) sparc_encode_format3a((ins),2,0,(r1),(r2),(setcc)|7,(dest))
#define sparc_xnor_imm(ins,setcc,r1,imm,dest) sparc_encode_format3b((ins),2,(r1),(imm),(setcc)|7,(dest))

/* shift */
#define sparc_sll(ins,src,disp,dest) sparc_encode_format3a((ins),2,0,(src),(disp),37,(dest))
#define sparc_sll_imm(ins,src,disp,dest) sparc_encode_format3b((ins),2,(src),(disp),37,(dest))

/* Sparc V9 */
#define sparc_sllx(ins,src,disp,dest) sparc_encode_format3ax((ins),2,0,(src),(disp),37,(dest))
#define sparc_sllx_imm(ins,src,disp,dest) sparc_encode_format3bx((ins),2,(src),(disp),37,(dest))

#define sparc_srl(ins,src,disp,dest) sparc_encode_format3a((ins),2,0,(src),(disp),38,(dest))
#define sparc_srl_imm(ins,src,disp,dest) sparc_encode_format3b((ins),2,(src),(disp),38,(dest))

/* Sparc V9 */
#define sparc_srlx(ins,src,disp,dest) sparc_encode_format3ax((ins),2,0,(src),(disp),38,(dest))
#define sparc_srlx_imm(ins,src,disp,dest) sparc_encode_format3bx((ins),2,(src),(disp),38,(dest))

#define sparc_sra(ins,src,disp,dest) sparc_encode_format3a((ins),2,0,(src),(disp),39,(dest))
#define sparc_sra_imm(ins,src,disp,dest) sparc_encode_format3b((ins),2,(src),(disp),39,(dest))

/* Sparc V9 */
#define sparc_srax(ins,src,disp,dest) sparc_encode_format3ax((ins),2,0,(src),(disp),39,(dest))
#define sparc_srax_imm(ins,src,disp,dest) sparc_encode_format3bx((ins),2,(src),(disp),39,(dest))

/* alu */

#define sparc_alu_reg(ins,op,setcc,r1,r2,dest) sparc_encode_format3a((ins),2,0,(r1),(r2),op|((setcc) ? 0x10 : 0),(dest))
#define sparc_alu_imm(ins,op,setcc,r1,imm,dest) sparc_encode_format3b((ins),2,(r1),(imm),op|((setcc) ? 0x10 : 0),(dest))

#define sparc_add(ins,setcc,r1,r2,dest) sparc_alu_reg((ins),0,(setcc),(r1),(r2),(dest))
#define sparc_add_imm(ins,setcc,r1,imm,dest) sparc_alu_imm((ins),0,(setcc),(r1),(imm),(dest))

#define sparc_addx(ins,setcc,r1,r2,dest) sparc_alu_reg((ins),0x8,(setcc),(r1),(r2),(dest))
#define sparc_addx_imm(ins,setcc,r1,imm,dest) sparc_alu_imm((ins),0x8,(setcc),(r1),(imm),(dest))

#define sparc_sub(ins,setcc,r1,r2,dest) sparc_alu_reg((ins),0x4,(setcc),(r1),(r2),(dest))
#define sparc_sub_imm(ins,setcc,r1,imm,dest) sparc_alu_imm((ins),0x4,(setcc),(r1),(imm),(dest))

#define sparc_subx(ins,setcc,r1,r2,dest) sparc_alu_reg((ins),0xc,(setcc),(r1),(r2),(dest))
#define sparc_subx_imm(ins,setcc,r1,imm,dest) sparc_alu_imm((ins),0xc,(setcc),(r1),(imm),(dest))

#define sparc_muls(ins,r1,r2,dest) sparc_encode_format3a((ins),2,0,(r1),(r2),36,(dest))
#define sparc_muls_imm(ins,r1,imm,dest) sparc_encode_format3b((ins),2,(r1),(imm),36,(dest))

#define sparc_umul(ins,setcc,r1,r2,dest) sparc_alu_reg((ins),0xa,(setcc),(r1),(r2),(dest))
#define sparc_umul_imm(ins,setcc,r1,imm,dest) sparc_alu_imm((ins),0xa,(setcc),(r1),(imm),(dest))

#define sparc_smul(ins,setcc,r1,r2,dest) sparc_alu_reg((ins),0xb,(setcc),(r1),(r2),(dest))
#define sparc_smul_imm(ins,setcc,r1,imm,dest) sparc_alu_imm((ins),0xb,(setcc),(r1),(imm),(dest))

#define sparc_udiv(ins,setcc,r1,r2,dest) sparc_alu_reg((ins),0xe,(setcc),(r1),(r2),(dest))
#define sparc_udiv_imm(ins,setcc,r1,imm,dest) sparc_alu_imm((ins),0xe,(setcc),(r1),(imm),(dest))

#define sparc_sdiv(ins,setcc,r1,r2,dest) sparc_alu_reg((ins),0xf,(setcc),(r1),(r2),(dest))
#define sparc_sdiv_imm(ins,setcc,r1,imm,dest) sparc_alu_imm((ins),0xf,(setcc),(r1),(imm),(dest))


/* branch */
#define sparc_branch(ins,aval,condval,displ) sparc_encode_format2b((ins),(aval),(condval),2,(displ))
/* FIXME: float condition codes are different: unify. */
#define sparc_fbranch(ins,aval,condval,displ) sparc_encode_format2b((ins),(aval),(condval),6,(displ))
#define sparc_branchp(ins,aval,condval,xcc,predict,displ) sparc_encode_format2c((ins),(aval),(condval),0x1,(xcc),(predict),(displ))

#define sparc_brz(ins,aval,predict,rs1,disp) sparc_encode_format2d((ins), (aval),0x1,0x3,(predict),(rs1),(disp))
#define sparc_brlez(ins,aval,predict,rs1,disp) sparc_encode_format2d((ins), (aval),0x2,0x3,(predict),(rs1),(disp))
#define sparc_brlz(ins,aval,predict,rs1,disp) sparc_encode_format2d((ins), (aval),0x3,0x3,(predict),(rs1),(disp))
#define sparc_brnz(ins,aval,predict,rs1,disp) sparc_encode_format2d((ins), (aval),0x5,0x3,(predict),(rs1),(disp))
#define sparc_brgz(ins,aval,predict,rs1,disp) sparc_encode_format2d((ins), (aval),0x6,0x3,(predict),(rs1),(disp))
#define sparc_brgez(ins,aval,predict,rs1,disp) sparc_encode_format2d((ins), (aval),0x7,0x3,(predict),(rs1),(disp))

/* conditional moves */
#define sparc_movcc(ins,cc,condval,r1,dest) sparc_encode_format4c((ins), 0x2, 0x2c, cc, condval, r1, dest)

#define sparc_movcc_imm(ins,cc,condval,imm,dest) sparc_encode_format4d((ins), 0x2, 0x2c, cc, condval, imm, dest)

/* synthetic instructions */
#define sparc_cmp(ins,r1,r2) sparc_sub((ins),sparc_cc,(r1),(r2),sparc_g0)
#define sparc_cmp_imm(ins,r1,imm) sparc_sub_imm((ins),sparc_cc,(r1),(imm),sparc_g0)
#define sparc_jmp(ins,base,disp) sparc_jmpl((ins),(base),(disp),sparc_g0)
#define sparc_jmp_imm(ins,base,disp) sparc_jmpl_imm((ins),(base),(disp),sparc_g0)
#define sparc_call(ins,base,disp) sparc_jmpl((ins),(base),(disp),sparc_o7)
#define sparc_call_imm(ins,base,disp) sparc_jmpl_imm((ins),(base),(disp),sparc_o7)

#define sparc_test(ins,reg) sparc_or ((ins),sparc_cc,sparc_g0,(reg),sparc_g0)

#define sparc_ret(ins) sparc_jmpl_imm((ins),sparc_i7,8,sparc_g0)
#define sparc_retl(ins) sparc_jmpl_imm((ins),sparc_o7,8,sparc_g0)
#define sparc_restore_simple(ins) sparc_restore((ins),sparc_g0,sparc_g0,sparc_g0)
#define sparc_rett_simple(ins) sparc_rett_imm((ins),sparc_i7,8)

#define sparc_set32(ins,val,reg)	\
	do {	\
        if ((val) == 0) \
            sparc_clr_reg((ins),(reg)); \
               else if (((guint32)(val) & 0x3ff) == 0) \
			sparc_sethi((ins),(guint32)(val),(reg));	\
		else if (((gint32)(val) >= -4096) && ((gint32)(val) <= 4095))	\
			sparc_or_imm((ins),FALSE,sparc_g0,(gint32)(val),(reg));	\
		else {	\
			sparc_sethi((ins),(guint32)(val),(reg));	\
			sparc_or_imm((ins),FALSE,(reg),(guint32)(val)&0x3ff,(reg));	\
		}	\
	} while (0)

#ifdef SPARCV9
#define SPARC_SET_MAX_SIZE (6 * 4)
#else
#define SPARC_SET_MAX_SIZE (2 * 4)
#endif

#if SPARCV9
#define sparc_set(ins,ptr,reg) \
	do {	\
        g_assert ((reg) != sparc_g1); \
        gint64 val = (gint64)ptr; \
		guint32 top_word = (val) >> 32; \
		guint32 bottom_word = (val) & 0xffffffff; \
        if (val == 0) \
           sparc_clr_reg ((ins), reg); \
		else if ((val >= -4096) && ((val) <= 4095))	\
			sparc_or_imm((ins),FALSE,sparc_g0,bottom_word,(reg));	\
        else if ((val >= 0) && (val <= 4294967295L)) {   \
               sparc_sethi((ins),bottom_word,(reg));   \
               if (bottom_word & 0x3ff) \
			sparc_or_imm((ins),FALSE,(reg),bottom_word&0x3ff,(reg));	\
        } \
        else if ((val >= 0) && (val <= (1L << 44) - 1)) {  \
            sparc_sethi ((ins), (val >> 12), (reg)); \
            sparc_or_imm ((ins), FALSE, (reg), (val >> 12) & 0x3ff, (reg)); \
            sparc_sllx_imm ((ins),(reg), 12, (reg)); \
            sparc_or_imm ((ins), FALSE, (reg), (val) & 0xfff, (reg)); \
        } \
        else if (top_word == 0xffffffff) { \
            sparc_xnor ((ins), FALSE, sparc_g0, sparc_g0, sparc_g1);    \
			sparc_sethi((ins),bottom_word,(reg));	\
		    sparc_sllx_imm((ins),sparc_g1,32,sparc_g1);	\
			sparc_or_imm((ins),FALSE,(reg),bottom_word&0x3ff,(reg));	\
		    sparc_or((ins),FALSE,(reg),sparc_g1,(reg));	\
        } \
        else { \
			sparc_sethi((ins),top_word,sparc_g1);	\
			sparc_sethi((ins),bottom_word,(reg));	\
			sparc_or_imm((ins),FALSE,sparc_g1,top_word&0x3ff,sparc_g1);	\
			sparc_or_imm((ins),FALSE,(reg),bottom_word&0x3ff,(reg));	\
		    sparc_sllx_imm((ins),sparc_g1,32,sparc_g1);	\
		    sparc_or((ins),FALSE,(reg),sparc_g1,(reg));	\
        } \
	} while (0)
#else
#define sparc_set(ins,val,reg)	\
	do {	\
        if ((val) == 0) \
            sparc_clr_reg((ins),(reg)); \
               else if (((guint32)(val) & 0x3ff) == 0) \
			sparc_sethi((ins),(guint32)(val),(reg));	\
		else if (((gint32)(val) >= -4096) && ((gint32)(val) <= 4095))	\
			sparc_or_imm((ins),FALSE,sparc_g0,(gint32)(val),(reg));	\
		else {	\
			sparc_sethi((ins),(guint32)(val),(reg));	\
			sparc_or_imm((ins),FALSE,(reg),(guint32)(val)&0x3ff,(reg));	\
		}	\
	} while (0)
#endif

#define sparc_set_ptr(ins,val,reg) sparc_set(ins,val,reg)

#ifdef SPARCV9
#define sparc_set_template(ins,reg) sparc_set (ins,0x7fffffff7fffffff, reg)
#else
#define sparc_set_template(ins,reg) sparc_set (ins,0x7fffffff, reg)
#endif

#define sparc_not(ins,reg) sparc_xnor((ins),FALSE,(reg),sparc_g0,(reg))
#define sparc_neg(ins,reg) sparc_sub((ins),FALSE,sparc_g0,(reg),(reg))
#define sparc_clr_reg(ins,reg) sparc_or((ins),FALSE,sparc_g0,sparc_g0,(reg))

#define sparc_mov_reg_reg(ins,src,dest) sparc_or((ins),FALSE,sparc_g0,(src),(dest))

#ifdef SPARCV9
#define sparc_sti_imm sparc_stx_imm
#define sparc_ldi_imm sparc_ldx_imm
#define sparc_sti sparc_stx
#define sparc_ldi sparc_ldx
#else
#define sparc_sti_imm sparc_st_imm
#define sparc_ldi_imm sparc_ld_imm
#define sparc_sti sparc_st
#define sparc_ldi sparc_ld
#endif

#endif /* __SPARC_CODEGEN_H__ */

