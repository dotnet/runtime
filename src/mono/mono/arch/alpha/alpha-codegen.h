#ifndef __ALPHA_CODEGEN_H__
#define __ALPHA_CODEGEN_H__

/*
    http://ftp.digital.com/pub/Digital/info/semiconductor/literature/alphaahb.pdf
*/

typedef enum {
	alpha_r0 = 0,
	alpha_r1 = 1,
	alpha_r2 = 2,
	alpha_r3 = 3,
	alpha_r4 = 4,
	alpha_r5 = 5,
	alpha_r6 = 6,
	alpha_r7 = 7,
	alpha_r8 = 8,
	alpha_r9 = 9,
	alpha_r10 = 10,
	alpha_r11 = 11,
	alpha_r12 = 12,
	alpha_r13 = 13,
	alpha_r14 = 14,
	alpha_r15 = 15,
	alpha_r16 = 16,
	alpha_r17 = 17,
	alpha_r18 = 18,
	alpha_r19 = 19,
	alpha_r20 = 20,
	alpha_r21 = 21,
	alpha_r22 = 22,
	alpha_r23 = 23,
	alpha_r24 = 24,
	alpha_r25 = 25,
	alpha_r26 = 26,
	alpha_r27 = 27,
	alpha_r28 = 28,
	alpha_r29 = 29,
	alpha_r30 = 30,
	alpha_r31 = 31, alpha_zero = 31,
	/* aliases */
	alpha_v0 = 0,  /* return value */
	
	alpha_t0 = 1,  /* temporaries */
	alpha_t1 = 2,
	alpha_t2 = 3,
	alpha_t3 = 4,
	alpha_t4 = 5,
	alpha_t5 = 6,
	alpha_t6 = 7,
	alpha_t7 = 8,

	alpha_s0 = 9,  /* saved registers */
	alpha_s1 = 10,
	alpha_s2 = 11,
	alpha_s3 = 12,
	alpha_s4 = 13,
	alpha_s5 = 14,
	alpha_s6 = 15,
	
	alpha_fp = 15, /* frame pointer */

	alpha_a0 = 16, /* argument registers */
	alpha_a1 = 17,
	alpha_a2 = 18,
	alpha_a3 = 19,
	alpha_a4 = 20,
	alpha_a5 = 21,

	alpha_t8  = 22, /* temporaries */
	alpha_t9  = 23,
	alpha_t10  = 24,
	alpha_t11  = 25,

	alpha_ra   = 26,   /* Return Address */

	alpha_pv   = 27,  /* pv  current procedure */
	alpha_t12  = 27,  /* temp 12 */

	alpha_altreg = 28,
	alpha_at     = 28,

	alpha_gp     = 29,  /* Global Pointer */
	alpha_sp     = 30,  /* Stack Pointer */
} AlphaRegister;

typedef enum {
	/* floating point registers */
	alpha_f0 = 0,
	alpha_f1 = 1,
	alpha_f2 = 2,
	alpha_f3 = 3,
	alpha_f4 = 4,
	alpha_f5 = 5,
	alpha_f6 = 6,
	alpha_f7 = 7,
	alpha_f8 = 8,
	alpha_f9 = 9,
	alpha_f10 = 10,
	alpha_f11 = 11,
	alpha_f12 = 12,
	alpha_f13 = 13,
	alpha_f14 = 14,
	alpha_f15 = 15,
	alpha_f16 = 16,
	alpha_f17 = 17,
	alpha_f18 = 18,
	alpha_f19 = 19,
	alpha_f20 = 20,
	alpha_f21 = 21,
	alpha_f22 = 22,
	alpha_f23 = 23,
	alpha_f24 = 24,
	alpha_f25 = 25,
	alpha_f26 = 26,
	alpha_f27 = 27,
	alpha_f28 = 28,
	alpha_f29 = 29,
	alpha_f30 = 30,
	alpha_f31 = 31, alpha_fzero = 31,
	/* aliases */
	alpha_fv0 = 0, /* return value */
	alpha_fv1 = 1,

	alpha_fs0 = 2, /* saved registers */
	alpha_fs1 = 3,
	alpha_fs2 = 4,
	alpha_fs3 = 5,
	alpha_fs4 = 6,
	alpha_fs5 = 7,
	alpha_fs6 = 8,
	alpha_fs7 = 9,

	alpha_ft0 = 10, /* temporary */
	alpha_ft1 = 11,
	alpha_ft2 = 12,
	alpha_ft3 = 13,
	alpha_ft4 = 14,
	alpha_ft5 = 15,

	alpha_fa0 = 16, /* args */
	alpha_fa1 = 17,
	alpha_fa2 = 18,
	alpha_fa3 = 19,
	alpha_fa4 = 20,
	alpha_fa5 = 21,

	alpha_ft6 = 22,
	alpha_ft7 = 23,
	alpha_ft8 = 24,
	alpha_ft9 = 25,
	alpha_ft10 = 26,
	alpha_ft11 = 27,
	alpha_ft12 = 28,
	alpha_ft13 = 29,
	alpha_ft14 = 30
} AlphaFPRegister;

/***************************************/

#define __alpha_int_32 unsigned int

/***************************************/
#define AXP_OFF26_MASK 0x03ffffff
#define AXP_OFF21_MASK 0x01fffff
#define AXP_OFF16_MASK 0x0ffff
#define AXP_OFF14_MASK 0x03fff
#define AXP_OFF13_MASK 0x01fff
#define AXP_OFF11_MASK 0x07ff
#define AXP_OFF8_MASK  0x0ff
#define AXP_OFF7_MASK  0x07f
#define AXP_OFF6_MASK  0x03f
#define AXP_OFF5_MASK  0x01f
#define AXP_OFF4_MASK  0x0f
#define AXP_OFF2_MASK  0x03
#define AXP_OFF1_MASK  0x01


#define AXP_REG_MASK  AXP_OFF5_MASK
#define AXP_REGSIZE      5

#define AXP_OP_SHIFT     26
#define AXP_REG1_SHIFT   21
#define AXP_REG2_SHIFT   16
#define AXP_MEM_BR_SHIFT 14
#define AXP_LIT_SHIFT    13

/* encode registers */
#define alpha_opcode( op ) \
	((op&AXP_OFF6_MASK) << AXP_OP_SHIFT)

/* encode registers */
#define alpha_reg_a( reg ) \
	((reg & AXP_REG_MASK) << AXP_REG1_SHIFT)

#define alpha_reg_b( reg ) \
	((reg & AXP_REG_MASK) << AXP_REG2_SHIFT)

#define alpha_reg_c( reg ) \
	(reg & AXP_REG_MASK)



/* encode function codes */
#define alpha_fp_func( func ) \
	((func & AXP_OFF11_MASK) << AXP_REGSIZE)

#define alpha_op_func( func ) \
	((func & AXP_OFF7_MASK) << AXP_REGSIZE)

#define alpha_op_literal( lit ) \
	((lit &  AXP_OFF8_MASK) << AXP_LIT_SHIFT)

#define alpha_mem_br_func( func, hint ) \
    (((func & AXP_OFF2_MASK ) << AXP_MEM_BR_SHIFT ) | (hint&AXP_OFF14_MASK))

#define alpha_mem_fc_func( func ) \
	(func && AXP_OFF16_MASK)


#define alpha_encode_hw4_mem( op, func ) \
		(alpha_opcode( op ) | (( func & 0x0f ) << 12))

#define alpha_encode_hw5_mem( op, func ) \
		(alpha_opcode( op ) | (( func & 0x3f ) << 10))

#define alpha_encode_hw6mem( op, func ) \
		(alpha_opcode( op ) | (( func & 0x0f ) << 12))

#define alpha_encode_hw6mem_br( op, func ) \
		(alpha_opcode( op ) | (( func & 0x07 ) << 13))


/*****************************************/


#define alpha_encode_palcall( ins, op, func ) \
	*((__alpha_int_32*)(ins)) = ( 0 |\
		alpha_opcode( op ) | ( func & AXP_OFF26_MASK )),\
	((__alpha_int_32*)(ins))++

#define alpha_encode_mem( ins, op, Rdest, Rsrc, offset ) \
	*((__alpha_int_32*)(ins)) = ( 0 |\
		alpha_opcode( op ) | alpha_reg_a( Rdest ) | \
		alpha_reg_b( Rsrc ) | (offset & AXP_OFF16_MASK )),\
	((__alpha_int_32*)(ins))++

#define alpha_encode_mem_fc( ins, op, func, Rdest, Rsrc, offset ) \
	*((__alpha_int_32*)(ins)) = ( 0 |\
		alpha_opcode( op ) | alpha_reg_a( Rdest ) | \
		alpha_reg_b( Rsrc ) | alpha_mem_fc_func( func )),\
	*((__alpha_int_32*)(ins))++

#define alpha_encode_mem_br( ins, op, func,  Rdest, Rsrc, hint ) \
	*((__alpha_int_32*)(ins)) = ( 0 |\
		alpha_opcode( op ) | alpha_reg_a( Rdest ) | \
		alpha_reg_b( Rsrc ) | alpha_mem_br_func( func, hint ) ),\
	((__alpha_int_32*)(ins))++

#define alpha_encode_branch( ins, op, Reg, offset ) \
	*((__alpha_int_32*)(ins)) = ( 0 |\
		alpha_opcode( op ) | alpha_reg_a( Reg ) | \
		(offset & AXP_OFF21_MASK )),\
	((__alpha_int_32*)(ins))++

#define alpha_encode_op( ins, op, func, Rsrc1, Rsrc2, Rdest ) \
	*((__alpha_int_32*)(ins)) = ( 0 |\
		alpha_opcode( op ) | alpha_reg_a( Rsrc1 ) | \
		alpha_reg_b( Rsrc2 ) | alpha_op_func( func ) | \
		alpha_reg_c( Rdest )),\
	((__alpha_int_32*)(ins))++


#define alpha_encode_opl( ins, op, func, Rsrc, lit, Rdest ) \
	*((__alpha_int_32*)(ins)) = ( 0 |\
		alpha_opcode( op ) | alpha_reg_a( Rsrc ) | \
		alpha_op_literal(lit) | ( 1 << 12 ) | \
		alpha_op_func( func ) | alpha_reg_c( Rdest ) ),\
	((__alpha_int_32*)(ins))++


#define alpha_encode_fpop( ins, op, func, Rsrc1, Rsrc2, Rdest ) \
	*((__alpha_int_32*)(ins)) = ( 0 |\
		alpha_opcode( op ) | alpha_reg_a( Rsrc1 ) | \
		alpha_reg_b( Rsrc2 ) | alpha_fp_func( func ) | \
		alpha_reg_c( Rdest )),\
	((__alpha_int_32*)(ins))++


/***************************************/

/* pal calls */
/* #define alpha_halt( ins )            alpha_encode_palcall( ins, 0, 0 ) */

#define alpha_call_pal( ins, func )  alpha_encode_palcall( ins, 0, func )

/*memory*/
#define alpha_lda( ins, Rdest, Rsrc, offset )	alpha_encode_mem( ins, 0x08, Rdest, Rsrc, offset )
#define alpha_ldah( ins, Rdest, Rsrc, offset )	alpha_encode_mem( ins, 0x09, Rdest, Rsrc, offset )
#define alpha_ldbu( ins, Rdest, Rsrc, offset )	alpha_encode_mem( ins, 0x0a, Rdest, Rsrc, offset )
#define alpha_ldq_u( ins, Rdest, Rsrc, offset )	alpha_encode_mem( ins, 0x0b, Rdest, Rsrc, offset )
#define alpha_ldwu( ins, Rdest, Rsrc, offset )	alpha_encode_mem( ins, 0x0c, Rdest, Rsrc, offset )
#define alpha_stw( ins, Rdest, Rsrc, offset )	alpha_encode_mem( ins, 0x0d, Rdest, Rsrc, offset )
#define alpha_stb( ins, Rdest, Rsrc, offset )	alpha_encode_mem( ins, 0x0e, Rdest, Rsrc, offset )
#define alpha_stq_u( ins, Rdest, Rsrc, offset )	alpha_encode_mem( ins, 0x0f, Rdest, Rsrc, offset )

#ifdef __VAX__
#define alpha_ldf( ins, Rdest, Rsrc, offset )	alpha_encode_mem( ins, 0x20, Rdest, Rsrc, offset )
#define alpha_ldg( ins, Rdest, Rsrc, offset )	alpha_encode_mem( ins, 0x21, Rdest, Rsrc, offset )
#define alpha_stf( ins, Rdest, Rsrc, offset )	alpha_encode_mem( ins, 0x24, Rdest, Rsrc, offset )
#define alpha_stg( ins, Rdest, Rsrc, offset )	alpha_encode_mem( ins, 0x25, Rdest, Rsrc, offset )
#endif

#define alpha_lds( ins, Rdest, Rsrc, offset )	alpha_encode_mem( ins, 0x22, Rdest, Rsrc, offset )
#define alpha_ldt( ins, Rdest, Rsrc, offset )	alpha_encode_mem( ins, 0x23, Rdest, Rsrc, offset )
#define alpha_ldqf( ins, Rdest, Rsrc, offset )	alpha_encode_mem( ins, 0x23, Rdest, Rsrc, offset )

#define alpha_sts( ins, Rdest, Rsrc, offset )	alpha_encode_mem( ins, 0x26, Rdest, Rsrc, offset )
#define alpha_stt( ins, Rdest, Rsrc, offset )	alpha_encode_mem( ins, 0x27, Rdest, Rsrc, offset )
#define alpha_stqf( ins, Rdest, Rsrc, offset )	alpha_encode_mem( ins, 0x27, Rdest, Rsrc, offset )


#define alpha_ldl( ins, Rdest, Rsrc, offset )	alpha_encode_mem( ins, 0x28, Rdest, Rsrc, offset )
#define alpha_ldq( ins, Rdest, Rsrc, offset )	alpha_encode_mem( ins, 0x29, Rdest, Rsrc, offset )
#define alpha_ldl_l( ins, Rdest, Rsrc, offset )	alpha_encode_mem( ins, 0x2A, Rdest, Rsrc, offset )
#define alpha_ldq_l( ins, Rdest, Rsrc, offset )	alpha_encode_mem( ins, 0x2B, Rdest, Rsrc, offset )
#define alpha_stl( ins, Rdest, Rsrc, offset )	alpha_encode_mem( ins, 0x2C, Rdest, Rsrc, offset )
#define alpha_stq( ins, Rdest, Rsrc, offset )	alpha_encode_mem( ins, 0x2D, Rdest, Rsrc, offset )
#define alpha_stl_c( ins, Rdest, Rsrc, offset )	alpha_encode_mem( ins, 0x2E, Rdest, Rsrc, offset )
#define alpha_stq_c( ins, Rdest, Rsrc, offset )	alpha_encode_mem( ins, 0x2F, Rdest, Rsrc, offset )


/* branch*/
#define alpha_jmp( ins, Rdest, Rsrc, hint ) alpha_encode_mem_br( ins, 0x1A, 0x0, Rdest, Rsrc, hint )
#define alpha_jsr( ins, Rdest, Rsrc, hint ) alpha_encode_mem_br( ins, 0x1A, 0x1, Rdest, Rsrc, hint )
#define alpha_ret( ins, Rsrc, hint ) alpha_encode_mem_br( ins, 0x1A, 0x2, alpha_zero, Rsrc, hint )
#define alpha_jsrco( ins, Rdest, Rsrc, hint ) alpha_encode_mem_br( ins, 0x1A, 0x3, Rdest, Rsrc, hint )

#define alpha_br( ins, Reg, offset )    alpha_encode_branch( ins, 0x30, Reg, offset )
#define alpha_fbeq( ins, Reg, offset )  alpha_encode_branch( ins, 0x31, Reg, offset )
#define alpha_fblt( ins, Reg, offset )  alpha_encode_branch( ins, 0x32, Reg, offset )
#define alpha_fble( ins, Reg, offset )  alpha_encode_branch( ins, 0x33, Reg, offset )
#define alpha_bsr( ins, Reg, offset )   alpha_encode_branch( ins, 0x34, Reg, offset )
#define alpha_fbne( ins, Reg, offset )  alpha_encode_branch( ins, 0x35, Reg, offset )
#define alpha_fbge( ins, Reg, offset )  alpha_encode_branch( ins, 0x36, Reg, offset )
#define alpha_fbgt( ins, Reg, offset )  alpha_encode_branch( ins, 0x37, Reg, offset )
#define alpha_blbc( ins, Reg, offset )  alpha_encode_branch( ins, 0x38, Reg, offset )
#define alpha_beq( ins, Reg, offset )   alpha_encode_branch( ins, 0x39, Reg, offset )
#define alpha_blt( ins, Reg, offset )   alpha_encode_branch( ins, 0x3A, Reg, offset )
#define alpha_ble( ins, Reg, offset )   alpha_encode_branch( ins, 0x3B, Reg, offset )
#define alpha_blbs( ins, Reg, offset )  alpha_encode_branch( ins, 0x3C, Reg, offset )
#define alpha_bne( ins, Reg, offset )   alpha_encode_branch( ins, 0x3D, Reg, offset )
#define alpha_bge( ins, Reg, offset )   alpha_encode_branch( ins, 0x3E, Reg, offset )
#define alpha_bgt( ins, Reg, offset )   alpha_encode_branch( ins, 0x3F, Reg, offset )


/* integer */
/*//#define alpha_sextl( ins,  Rsrc1, Rsrc2, Rdest ) alpha_encode_op( ins, 0x10, 0x00, Rsrc1, Rsrc2, Rdest )
//#define alpha_sextl_( ins,  Rsrc1, lit, Rdest )  alpha_encode_opl( ins, 0x10, 0x00, Rsrc1, lit, Rdest )
*/
#define alpha_addl( ins,  Rsrc1, Rsrc2, Rdest ) alpha_encode_op( ins, 0x10, 0x00, Rsrc1, Rsrc2, Rdest )
#define alpha_addl_( ins,  Rsrc1, lit, Rdest )  alpha_encode_opl( ins, 0x10, 0x00, Rsrc1, lit, Rdest )
#define alpha_s4addl( ins,  Rsrc1, Rsrc2, Rdest ) alpha_encode_op( ins, 0x10, 0x02, Rsrc1, Rsrc2, Rdest )
#define alpha_s4addl_( ins,  Rsrc1, lit, Rdest )  alpha_encode_opl( ins, 0x10, 0x02, Rsrc1, lit, Rdest )
//#define alpha_negl( ins,  Rsrc1, Rsrc2, Rdest ) alpha_encode_op( ins, 0x10, 0x09, Rsrc1, Rsrc2, Rdest )
//#define alpha_negl_( ins,  Rsrc1, lit, Rdest )  alpha_encode_opl( ins, 0x10, 0x09, Rsrc1, lit, Rdest )
#define alpha_subl( ins,  Rsrc1, Rsrc2, Rdest ) alpha_encode_op( ins, 0x10, 0x09, Rsrc1, Rsrc2, Rdest )
#define alpha_subl_( ins,  Rsrc1, lit, Rdest )  alpha_encode_opl( ins, 0x10, 0x09, Rsrc1, lit, Rdest )
#define alpha_s4subl( ins,  Rsrc1, Rsrc2, Rdest ) alpha_encode_op( ins, 0x10, 0x0B, Rsrc1, Rsrc2, Rdest )
#define alpha_s4subl_( ins,  Rsrc1, lit, Rdest )  alpha_encode_opl( ins, 0x10, 0x0B, Rsrc1, lit, Rdest )
#define alpha_cmpbge( ins,  Rsrc1, Rsrc2, Rdest ) alpha_encode_op( ins, 0x10, 0x0F, Rsrc1, Rsrc2, Rdest )
#define alpha_cmpbge_( ins,  Rsrc1, lit, Rdest )  alpha_encode_opl( ins, 0x10, 0x0F, Rsrc1, lit, Rdest )
#define alpha_s8addl( ins,  Rsrc1, Rsrc2, Rdest ) alpha_encode_op( ins, 0x10, 0x12, Rsrc1, Rsrc2, Rdest )
#define alpha_s8addl_( ins,  Rsrc1, lit, Rdest )  alpha_encode_opl( ins, 0x10, 0x12, Rsrc1, lit, Rdest )
#define alpha_s8subl( ins,  Rsrc1, Rsrc2, Rdest ) alpha_encode_op( ins, 0x10, 0x1B, Rsrc1, Rsrc2, Rdest )
#define alpha_s8subl_( ins,  Rsrc1, lit, Rdest )  alpha_encode_opl( ins, 0x10, 0x1B, Rsrc1, lit, Rdest )
#define alpha_cmpult( ins,  Rsrc1, Rsrc2, Rdest ) alpha_encode_op( ins, 0x10, 0x1d, Rsrc1, Rsrc2, Rdest )
#define alpha_cmpult_( ins,  Rsrc1, lit, Rdest )  alpha_encode_opl( ins, 0x10, 0x1d, Rsrc1, lit, Rdest )
#define alpha_addq( ins,  Rsrc1, Rsrc2, Rdest ) alpha_encode_op( ins, 0x10, 0x20, Rsrc1, Rsrc2, Rdest )
#define alpha_addq_( ins,  Rsrc1, lit, Rdest )  alpha_encode_opl( ins, 0x10, 0x20, Rsrc1, lit, Rdest )
#define alpha_s4addq( ins,  Rsrc1, Rsrc2, Rdest ) alpha_encode_op( ins, 0x10, 0x22, Rsrc1, Rsrc2, Rdest )
#define alpha_s4addq_( ins,  Rsrc1, lit, Rdest )  alpha_encode_opl( ins, 0x10, 0x22, Rsrc1, lit, Rdest )
//#define alpha_negq( ins,  Rsrc1, Rsrc2, Rdest ) alpha_encode_op( ins, 0x10, 0x29, Rsrc1, Rsrc2, Rdest )
//#define alpha_negq_( ins,  Rsrc1, lit, Rdest )  alpha_encode_opl( ins, 0x10, 0x29, Rsrc1, lit, Rdest )
#define alpha_subq( ins,  Rsrc1, Rsrc2, Rdest ) alpha_encode_op( ins, 0x10, 0x29, Rsrc1, Rsrc2, Rdest )
#define alpha_subq_( ins,  Rsrc1, lit, Rdest )  alpha_encode_opl( ins, 0x10, 0x29, Rsrc1, lit, Rdest )
#define alpha_s4subq( ins,  Rsrc1, Rsrc2, Rdest ) alpha_encode_op( ins, 0x10, 0x2B, Rsrc1, Rsrc2, Rdest )
#define alpha_s4subq_( ins,  Rsrc1, lit, Rdest )  alpha_encode_opl( ins, 0x10, 0x2B, Rsrc1, lit, Rdest )
#define alpha_cmpeq( ins,  Rsrc1, Rsrc2, Rdest ) alpha_encode_op( ins, 0x10, 0x2D, Rsrc1, Rsrc2, Rdest )
#define alpha_cmpeq_( ins,  Rsrc1, lit, Rdest )  alpha_encode_opl( ins, 0x10, 0x2D, Rsrc1, lit, Rdest )
#define alpha_s8addq( ins,  Rsrc1, Rsrc2, Rdest ) alpha_encode_op( ins, 0x10, 0x32, Rsrc1, Rsrc2, Rdest )
#define alpha_s8addq_( ins,  Rsrc1, lit, Rdest )  alpha_encode_opl( ins, 0x10, 0x32, Rsrc1, lit, Rdest )
#define alpha_s8subq( ins,  Rsrc1, Rsrc2, Rdest ) alpha_encode_op( ins, 0x10, 0x3B, Rsrc1, Rsrc2, Rdest )
#define alpha_s8subq_( ins,  Rsrc1, lit, Rdest )  alpha_encode_opl( ins, 0x10, 0x3B, Rsrc1, lit, Rdest )
#define alpha_cmpule( ins,  Rsrc1, Rsrc2, Rdest ) alpha_encode_op( ins, 0x10, 0x3D, Rsrc1, Rsrc2, Rdest )
#define alpha_cmpule_( ins,  Rsrc1, lit, Rdest )  alpha_encode_opl( ins, 0x10, 0x3D, Rsrc1, lit, Rdest )
#define alpha_addlv( ins,  Rsrc1, Rsrc2, Rdest ) alpha_encode_op( ins, 0x10, 0x40, Rsrc1, Rsrc2, Rdest )
#define alpha_addlv_( ins,  Rsrc1, lit, Rdest )  alpha_encode_opl( ins, 0x10, 0x40, Rsrc1, lit, Rdest )
//#define alpha_neglv( ins,  Rsrc1, Rsrc2, Rdest ) alpha_encode_op( ins, 0x10, 0x49, Rsrc1, Rsrc2, Rdest )
//#define alpha_neglv_( ins,  Rsrc1, lit, Rdest )  alpha_encode_opl( ins, 0x10, 0x49, Rsrc1, lit, Rdest )
#define alpha_sublv( ins,  Rsrc1, Rsrc2, Rdest ) alpha_encode_op( ins, 0x10, 0x49, Rsrc1, Rsrc2, Rdest )
#define alpha_sublv_( ins,  Rsrc1, lit, Rdest )  alpha_encode_opl( ins, 0x10, 0x49, Rsrc1, lit, Rdest )
#define alpha_cmplt( ins,  Rsrc1, Rsrc2, Rdest ) alpha_encode_op( ins, 0x10, 0x4D, Rsrc1, Rsrc2, Rdest )
#define alpha_cmplt_( ins,  Rsrc1, lit, Rdest )  alpha_encode_opl( ins, 0x10, 0x4D, Rsrc1, lit, Rdest )
#define alpha_addqv( ins,  Rsrc1, Rsrc2, Rdest ) alpha_encode_op( ins, 0x10, 0x60, Rsrc1, Rsrc2, Rdest )
#define alpha_addqv_( ins,  Rsrc1, lit, Rdest )  alpha_encode_opl( ins, 0x10, 0x60, Rsrc1, lit, Rdest )
//#define alpha_negqv( ins,  Rsrc1, Rsrc2, Rdest ) alpha_encode_op( ins, 0x10, 0x69, Rsrc1, Rsrc2, Rdest )
//#define alpha_negqv_( ins,  Rsrc1, lit, Rdest )  alpha_encode_opl( ins, 0x10, 0x69, Rsrc1, lit, Rdest )
#define alpha_subqv( ins,  Rsrc1, Rsrc2, Rdest ) alpha_encode_op( ins, 0x10, 0x69, Rsrc1, Rsrc2, Rdest )
#define alpha_subqv_( ins,  Rsrc1, lit, Rdest )  alpha_encode_opl( ins, 0x10, 0x69, Rsrc1, lit, Rdest )
#define alpha_cmple( ins,  Rsrc1, Rsrc2, Rdest ) alpha_encode_op( ins, 0x10, 0x6D, Rsrc1, Rsrc2, Rdest )
#define alpha_cmple_( ins,  Rsrc1, lit, Rdest )  alpha_encode_opl( ins, 0x10, 0x6D, Rsrc1, lit, Rdest )

#define alpha_and( ins,  Rsrc1, Rsrc2, Rdest ) alpha_encode_op( ins, 0x11, 0x00, Rsrc1, Rsrc2, Rdest )
#define alpha_and_( ins,  Rsrc1, lit, Rdest )  alpha_encode_opl( ins, 0x11, 0x00, Rsrc1, lit, Rdest )
//#define alpha_andnot( ins,  Rsrc1, Rsrc2, Rdest ) alpha_encode_op( ins, 0x11, 0x08, Rsrc1, Rsrc2, Rdest )
//#define alpha_andnot_( ins,  Rsrc1, lit, Rdest )  alpha_encode_opl( ins, 0x11, 0x08, Rsrc1, lit, Rdest )
#define alpha_bic( ins,  Rsrc1, Rsrc2, Rdest ) alpha_encode_op( ins, 0x11, 0x08, Rsrc1, Rsrc2, Rdest )
#define alpha_bic_( ins,  Rsrc1, lit, Rdest )  alpha_encode_opl( ins, 0x11, 0x08, Rsrc1, lit, Rdest )
#define alpha_cmovlbs( ins,  Rsrc1, Rsrc2, Rdest ) alpha_encode_op( ins, 0x11, 0x14, Rsrc1, Rsrc2, Rdest )
#define alpha_cmovlbs_( ins,  Rsrc1, lit, Rdest )  alpha_encode_opl( ins, 0x11, 0x14, Rsrc1, lit, Rdest )
#define alpha_cmovlbc( ins,  Rsrc1, Rsrc2, Rdest ) alpha_encode_op( ins, 0x11, 0x16, Rsrc1, Rsrc2, Rdest )
#define alpha_cmovlbc_( ins,  Rsrc1, lit, Rdest )  alpha_encode_opl( ins, 0x11, 0x16, Rsrc1, lit, Rdest )
#define alpha_nop( ins )			   alpha_encode_op( ins, 0x11, 0x20, alpha_zero, alpha_zero, alpha_zero )
#define alpha_clr( ins, Rdest )			   alpha_encode_op( ins, 0x11, 0x20, alpha_zero, alpha_zero, Rdest )
#define alpha_mov1( ins, Rsrc, Rdest )		   alpha_encode_op( ins, 0x11, 0x20, alpha_zero, Rsrc, Rdest )
#define alpha_mov2( ins, Rsrc1, Rsrc2, Rdest )    alpha_encode_op( ins, 0x11, 0x20,  Rsrc1, Rsrc2, Rdest )
#define alpha_mov_( ins, lit, Rdest )              alpha_encode_op( ins, 0x11, 0x20, alpha_zero, lit, Rdest )
//#define alpha_or( ins,  Rsrc1, Rsrc2, Rdest ) alpha_encode_op( ins, 0x11, 0x20, Rsrc1, Rsrc2, Rdest )
//#define alpha_or_( ins,  Rsrc1, lit, Rdest )  alpha_encode_opl( ins, 0x11, 0x20, Rsrc1, lit, Rdest )
#define alpha_bis( ins,  Rsrc1, Rsrc2, Rdest ) alpha_encode_op( ins, 0x11, 0x20, Rsrc1, Rsrc2, Rdest )
#define alpha_bis_( ins,  Rsrc1, lit, Rdest )  alpha_encode_opl( ins, 0x11, 0x20, Rsrc1, lit, Rdest )
#define alpha_cmoveq( ins,  Rsrc1, Rsrc2, Rdest ) alpha_encode_op( ins, 0x11, 0x24, Rsrc1, Rsrc2, Rdest )
#define alpha_cmoveq_( ins,  Rsrc1, lit, Rdest )  alpha_encode_opl( ins, 0x11, 0x24, Rsrc1, lit, Rdest )
#define alpha_cmovne( ins,  Rsrc1, Rsrc2, Rdest ) alpha_encode_op( ins, 0x11, 0x26, Rsrc1, Rsrc2, Rdest )
#define alpha_cmovne_( ins,  Rsrc1, lit, Rdest )  alpha_encode_opl( ins, 0x11, 0x26, Rsrc1, lit, Rdest )
#define alpha_not( ins,  Rsrc2, Rdest )        alpha_encode_op( ins, 0x11, 0x28, alpha_zero, Rsrc2, Rdest )
#define alpha_not_( ins, lit, Rdest )          alpha_encode_opl( ins, 0x11, 0x28, alpha_zero, lit, Rdest )
#define alpha_ornot( ins,  Rsrc1, Rsrc2, Rdest ) alpha_encode_op( ins, 0x11, 0x28, Rsrc1, Rsrc2, Rdest )
#define alpha_ornot_( ins,  Rsrc1, lit, Rdest )  alpha_encode_opl( ins, 0x11, 0x28, Rsrc1, lit, Rdest )
#define alpha_xor( ins,  Rsrc1, Rsrc2, Rdest ) alpha_encode_op( ins, 0x11, 0x40, Rsrc1, Rsrc2, Rdest )
#define alpha_xor_( ins,  Rsrc1, lit, Rdest )  alpha_encode_opl( ins, 0x11, 0x40, Rsrc1, lit, Rdest )
#define alpha_cmovlt( ins,  Rsrc1, Rsrc2, Rdest ) alpha_encode_op( ins, 0x11, 0x44, Rsrc1, Rsrc2, Rdest )
#define alpha_cmovlt_( ins,  Rsrc1, lit, Rdest )  alpha_encode_opl( ins, 0x11, 0x44, Rsrc1, lit, Rdest )
#define alpha_cmovge( ins,  Rsrc1, Rsrc2, Rdest ) alpha_encode_op( ins, 0x11, 0x46, Rsrc1, Rsrc2, Rdest )
#define alpha_cmovge_( ins,  Rsrc1, lit, Rdest )  alpha_encode_opl( ins, 0x11, 0x46, Rsrc1, lit, Rdest )
#define alpha_eqv( ins,  Rsrc1, Rsrc2, Rdest ) alpha_encode_op( ins, 0x11, 0x48, Rsrc1, Rsrc2, Rdest )
#define alpha_eqv_( ins,  Rsrc1, lit, Rdest )  alpha_encode_opl( ins, 0x11, 0x48, Rsrc1, lit, Rdest )
//#define alpha_xornot( ins,  Rsrc1, Rsrc2, Rdest ) alpha_encode_op( ins, 0x11, 0x48, Rsrc1, Rsrc2, Rdest )
//#define alpha_xornot_( ins,  Rsrc1, lit, Rdest )  alpha_encode_opl( ins, 0x11, 0x48, Rsrc1, lit, Rdest )
#define alpha_ev56b_amask( ins,  Rsrc2, Rdest )     alpha_encode_op( ins, 0x11, 0x61, alpha_zero, Rsrc2, Rdest )
#define alpha_ev56b_amask_( ins,  lit, Rdest )      alpha_encode_opl( ins, 0x11, 0x61, alpha_zero, lit, Rdest )
#define alpha_cmovle( ins,  Rsrc1, Rsrc2, Rdest )   alpha_encode_op( ins, 0x11, 0x64, Rsrc1, Rsrc2, Rdest )
#define alpha_cmovle_( ins,  Rsrc1, lit, Rdest )    alpha_encode_opl( ins, 0x11, 0x64, Rsrc1, lit, Rdest )
#define alpha_cmovgt( ins,  Rsrc1, Rsrc2, Rdest )   alpha_encode_op( ins, 0x11, 0x66, Rsrc1, Rsrc2, Rdest )
#define alpha_cmovgt_( ins,  Rsrc1, lit, Rdest )    alpha_encode_opl( ins, 0x11, 0x66, Rsrc1, lit, Rdest )
//#define alpha_implver_( ins,  Rsrc1, lit, Rdest )    alpha_encode_opl( ins, 0x11, 0x6C, Rsrc1, lit, Rdest )
#define alpha_cmovgt( ins,  Rsrc1, Rsrc2, Rdest )   alpha_encode_op( ins, 0x11, 0x66, Rsrc1, Rsrc2, Rdest )
#define alpha_cmovgt_( ins,  Rsrc1, lit, Rdest )    alpha_encode_opl( ins, 0x11, 0x66, Rsrc1, lit, Rdest )

#define alpha_mskbl( ins,  Rsrc1, Rsrc2, Rdest )   alpha_encode_op( ins, 0x12, 0x02, Rsrc1, Rsrc2, Rdest )
#define alpha_mskbl_( ins,  Rsrc1, lit, Rdest )    alpha_encode_opl( ins, 0x12, 0x02, Rsrc1, lit, Rdest )
#define alpha_extbl( ins,  Rsrc1, Rsrc2, Rdest )   alpha_encode_op( ins, 0x12, 0x06, Rsrc1, Rsrc2, Rdest )
#define alpha_extbl_( ins,  Rsrc1, lit, Rdest )    alpha_encode_opl( ins, 0x12, 0x06, Rsrc1, lit, Rdest )
#define alpha_insbl( ins,  Rsrc1, Rsrc2, Rdest )   alpha_encode_op( ins, 0x12, 0x0B, Rsrc1, Rsrc2, Rdest )
#define alpha_insbl_( ins,  Rsrc1, lit, Rdest )    alpha_encode_opl( ins, 0x12, 0x0B, Rsrc1, lit, Rdest )
#define alpha_mskwl( ins,  Rsrc1, Rsrc2, Rdest )   alpha_encode_op( ins, 0x12, 0x12, Rsrc1, Rsrc2, Rdest )
#define alpha_mskwl_( ins,  Rsrc1, lit, Rdest )    alpha_encode_opl( ins, 0x12, 0x12, Rsrc1, lit, Rdest )
#define alpha_extwl( ins,  Rsrc1, Rsrc2, Rdest )   alpha_encode_op( ins, 0x12, 0x16, Rsrc1, Rsrc2, Rdest )
#define alpha_extwl_( ins,  Rsrc1, lit, Rdest )    alpha_encode_opl( ins, 0x12, 0x16, Rsrc1, lit, Rdest )
#define alpha_inswl( ins,  Rsrc1, Rsrc2, Rdest )   alpha_encode_op( ins, 0x12, 0x1b, Rsrc1, Rsrc2, Rdest )
#define alpha_inswl_( ins,  Rsrc1, lit, Rdest )    alpha_encode_opl( ins, 0x12, 0x1b, Rsrc1, lit, Rdest )
#define alpha_mskll( ins,  Rsrc1, Rsrc2, Rdest )   alpha_encode_op( ins, 0x12, 0x22, Rsrc1, Rsrc2, Rdest )
#define alpha_mskll_( ins,  Rsrc1, lit, Rdest )    alpha_encode_opl( ins, 0x12, 0x22, Rsrc1, lit, Rdest )
#define alpha_extll( ins,  Rsrc1, Rsrc2, Rdest )   alpha_encode_op( ins, 0x12, 0x26, Rsrc1, Rsrc2, Rdest )
#define alpha_extll_( ins,  Rsrc1, lit, Rdest )    alpha_encode_opl( ins, 0x12, 0x26, Rsrc1, lit, Rdest )
#define alpha_insll( ins,  Rsrc1, Rsrc2, Rdest )   alpha_encode_op( ins, 0x12, 0x2b, Rsrc1, Rsrc2, Rdest )
#define alpha_insll_( ins,  Rsrc1, lit, Rdest )    alpha_encode_opl( ins, 0x12, 0x2b, Rsrc1, lit, Rdest )
#define alpha_zap( ins,  Rsrc1, Rsrc2, Rdest )   alpha_encode_op( ins, 0x12, 0x30, Rsrc1, Rsrc2, Rdest )
#define alpha_zap_( ins,  Rsrc1, lit, Rdest )    alpha_encode_opl( ins, 0x12, 0x30, Rsrc1, lit, Rdest )
#define alpha_zapnot( ins,  Rsrc1, Rsrc2, Rdest )   alpha_encode_op( ins, 0x12, 0x31, Rsrc1, Rsrc2, Rdest )
#define alpha_zapnot_( ins,  Rsrc1, lit, Rdest )    alpha_encode_opl( ins, 0x12, 0x31, Rsrc1, lit, Rdest )
#define alpha_mskql( ins,  Rsrc1, Rsrc2, Rdest )   alpha_encode_op( ins, 0x12, 0x32, Rsrc1, Rsrc2, Rdest )
#define alpha_mskql_( ins,  Rsrc1, lit, Rdest )    alpha_encode_opl( ins, 0x12, 0x32, Rsrc1, lit, Rdest )
#define alpha_srl( ins,  Rsrc1, Rsrc2, Rdest )   alpha_encode_op( ins, 0x12, 0x34, Rsrc1, Rsrc2, Rdest )
#define alpha_srl_( ins,  Rsrc1, lit, Rdest )    alpha_encode_opl( ins, 0x12, 0x34, Rsrc1, lit, Rdest )
#define alpha_extql( ins,  Rsrc1, Rsrc2, Rdest )   alpha_encode_op( ins, 0x12, 0x36, Rsrc1, Rsrc2, Rdest )
#define alpha_extql_( ins,  Rsrc1, lit, Rdest )    alpha_encode_opl( ins, 0x12, 0x36, Rsrc1, lit, Rdest )
#define alpha_sll( ins,  Rsrc1, Rsrc2, Rdest )   alpha_encode_op( ins, 0x12, 0x39, Rsrc1, Rsrc2, Rdest )
#define alpha_sll_( ins,  Rsrc1, lit, Rdest )    alpha_encode_opl( ins, 0x12, 0x39, Rsrc1, lit, Rdest )
#define alpha_insql( ins,  Rsrc1, Rsrc2, Rdest )   alpha_encode_op( ins, 0x12, 0x3b, Rsrc1, Rsrc2, Rdest )
#define alpha_insql_( ins,  Rsrc1, lit, Rdest )    alpha_encode_opl( ins, 0x12, 0x3b, Rsrc1, lit, Rdest )
#define alpha_sra( ins,  Rsrc1, Rsrc2, Rdest )   alpha_encode_op( ins, 0x12, 0x3c, Rsrc1, Rsrc2, Rdest )
#define alpha_sra_( ins,  Rsrc1, lit, Rdest )    alpha_encode_opl( ins, 0x12, 0x3c, Rsrc1, lit, Rdest )
#define alpha_mskwh( ins,  Rsrc1, Rsrc2, Rdest )   alpha_encode_op( ins, 0x12, 0x52, Rsrc1, Rsrc2, Rdest )
#define alpha_mskwh_( ins,  Rsrc1, lit, Rdest )    alpha_encode_opl( ins, 0x12, 0x52, Rsrc1, lit, Rdest )
#define alpha_inswh( ins,  Rsrc1, Rsrc2, Rdest )   alpha_encode_op( ins, 0x12, 0x57, Rsrc1, Rsrc2, Rdest )
#define alpha_inswh_( ins,  Rsrc1, lit, Rdest )    alpha_encode_opl( ins, 0x12, 0x57, Rsrc1, lit, Rdest )
#define alpha_extwh( ins,  Rsrc1, Rsrc2, Rdest )   alpha_encode_op( ins, 0x12, 0x5a, Rsrc1, Rsrc2, Rdest )
#define alpha_extwh_( ins,  Rsrc1, lit, Rdest )    alpha_encode_opl( ins, 0x12, 0x5a, Rsrc1, lit, Rdest )
#define alpha_msklh( ins,  Rsrc1, Rsrc2, Rdest )   alpha_encode_op( ins, 0x12, 0x62, Rsrc1, Rsrc2, Rdest )
#define alpha_msklh_( ins,  Rsrc1, lit, Rdest )    alpha_encode_opl( ins, 0x12, 0x62, Rsrc1, lit, Rdest )
#define alpha_inslh( ins,  Rsrc1, Rsrc2, Rdest )   alpha_encode_op( ins, 0x12, 0x67, Rsrc1, Rsrc2, Rdest )
#define alpha_inslh_( ins,  Rsrc1, lit, Rdest )    alpha_encode_opl( ins, 0x12, 0x67, Rsrc1, lit, Rdest )
#define alpha_extlh( ins,  Rsrc1, Rsrc2, Rdest )   alpha_encode_op( ins, 0x12, 0x6a, Rsrc1, Rsrc2, Rdest )
#define alpha_extlh_( ins,  Rsrc1, lit, Rdest )    alpha_encode_opl( ins, 0x12, 0x6a, Rsrc1, lit, Rdest )
#define alpha_mskqh( ins,  Rsrc1, Rsrc2, Rdest )   alpha_encode_op( ins, 0x12, 0x72, Rsrc1, Rsrc2, Rdest )
#define alpha_mskqh_( ins,  Rsrc1, lit, Rdest )    alpha_encode_opl( ins, 0x12, 0x72, Rsrc1, lit, Rdest )
#define alpha_insqh( ins,  Rsrc1, Rsrc2, Rdest )   alpha_encode_op( ins, 0x12, 0x77, Rsrc1, Rsrc2, Rdest )
#define alpha_insqh_( ins,  Rsrc1, lit, Rdest )    alpha_encode_opl( ins, 0x12, 0x77, Rsrc1, lit, Rdest )
#define alpha_extqh( ins,  Rsrc1, Rsrc2, Rdest )   alpha_encode_op( ins, 0x12, 0x7a, Rsrc1, Rsrc2, Rdest )
#define alpha_extqh_( ins,  Rsrc1, lit, Rdest )    alpha_encode_opl( ins, 0x12, 0x7a, Rsrc1, lit, Rdest )

#define alpha_mull(ins, Rsrc1, Rsrc2, Rdest) alpha_encode_op( ins, 0x13, 0x00, Rsrc1, Rsrc2, Rdest )
#define alpha_mull_(ins,  Rsrc1, lit, Rdest) alpha_encode_op( ins, 0x13, 0x00, Rsrc1, lit, Rdest )
#define alpha_mulq(ins, Rsrc1, Rsrc2, Rdest) alpha_encode_op( ins, 0x13, 0x20, Rsrc1, Rsrc2, Rdest )
#define alpha_mulq_(ins,  Rsrc1, lit, Rdest) alpha_encode_op( ins, 0x13, 0x20, Rsrc1, lit, Rdest )

#define	alpha_sextb(ins, Rsrc2, Rdest) alpha_encode_op( ins, 0x1c, 0x00, alpha_zero, Rsrc2, Rdest )
#define alpha_sextw(ins, Rsrc2, Rdest) alpha_encode_op( ins, 0x1c, 0x01, alpha_zero, Rsrc2, Rdest )

// For 264 
#define alpha_ftois( ins, RFsrc, Rdest ) alpha_encode_fpop( ins, 0x1c, 0x078, RFsrc, alpha_zero, Rdest ) 
#define alpha_ftoit( ins, RFsrc, Rdest ) alpha_encode_fpop( ins, 0x1c, 0x070, RFsrc, alpha_zero, Rdest )
#define alpha_ftoi_qf( ins, RFsrc, Rdest ) alpha_encode_fpop( ins, 0x1c, 0x070, RFsrc, alpha_zero, Rdest )
// For 264
#define alpha_itofs( ins, Rsrc, RFdest ) alpha_encode_fpop( ins, 0x14, 0x004, Rsrc, alpha_zero, RFdest ) 
#define alpha_itoff( ins, Rsrc, RFdest ) alpha_encode_fpop( ins, 0x14, 0x014, Rsrc, alpha_zero, RFdest )
#define alpha_itoft( ins, Rsrc, RFdest ) alpha_encode_fpop( ins, 0x14, 0x024, Rsrc, alpha_zero, RFdest ) 
#define alpha_itof_qf( ins, Rsrc, RFdest ) alpha_encode_fpop( ins, 0x14, 0x024, Rsrc, alpha_zero, RFdest ) 

#define alpha_cvtts_c(ins, Rsrc2, Rdest) alpha_encode_fpop( ins, 0x16, 0x2C, alpha_fzero, Rsrc2, Rdest )
#define alpha_cvttq_c(ins, Rsrc2, Rdest) alpha_encode_fpop( ins, 0x16, 0x2F, alpha_fzero, Rsrc2, Rdest )
#define alpha_cvtqs_c(ins, Rsrc2, Rdest) alpha_encode_fpop( ins, 0x16, 0x3C, alpha_fzero, Rsrc2, Rdest )
#define alpha_cvtqt_c(ins, Rsrc2, Rdest) alpha_encode_fpop( ins, 0x16, 0x3E, alpha_fzero, Rsrc2, Rdest )


#define alpha_adds(ins, Rsrc1, Rsrc2, Rdest) alpha_encode_fpop( ins, 0x16, 0x080, Rsrc1, Rsrc2, Rdest )
#define alpha_subs(ins, Rsrc1, Rsrc2, Rdest) alpha_encode_fpop( ins, 0x16, 0x081, Rsrc1, Rsrc2, Rdest )
#define alpha_addt(ins, Rsrc1, Rsrc2, Rdest) alpha_encode_fpop( ins, 0x16, 0x0A0, Rsrc1, Rsrc2, Rdest )
#define alpha_subt(ins, Rsrc1, Rsrc2, Rdest) alpha_encode_fpop( ins, 0x16, 0x0A1, Rsrc1, Rsrc2, Rdest )
#define alpha_mult(ins, Rsrc1, Rsrc2, Rdest) alpha_encode_fpop( ins, 0x16, 0x0A2, Rsrc1, Rsrc2, Rdest )
#define alpha_divt(ins, Rsrc1, Rsrc2, Rdest) alpha_encode_fpop( ins, 0x16, 0x0A3, Rsrc1, Rsrc2, Rdest )

#define alpha_cmptun(ins, Rsrc1, Rsrc2, Rdest) alpha_encode_fpop( ins, 0x16, 0x0A4, Rsrc1, Rsrc2, Rdest )
#define alpha_cmpteq(ins, Rsrc1, Rsrc2, Rdest) alpha_encode_fpop( ins, 0x16, 0x0A5, Rsrc1, Rsrc2, Rdest )
#define alpha_cmptlt(ins, Rsrc1, Rsrc2, Rdest) alpha_encode_fpop( ins, 0x16, 0x0A6, Rsrc1, Rsrc2, Rdest )
#define alpha_cmptle(ins, Rsrc1, Rsrc2, Rdest) alpha_encode_fpop( ins, 0x16, 0x0A7, Rsrc1, Rsrc2, Rdest )

#define alpha_addt_su(ins, Rsrc1, Rsrc2, Rdest) alpha_encode_fpop( ins, 0x16, 0x5A0, Rsrc1, Rsrc2, Rdest )
#define alpha_subt_su(ins, Rsrc1, Rsrc2, Rdest) alpha_encode_fpop( ins, 0x16, 0x5A1, Rsrc1, Rsrc2, Rdest )


#define alpha_cmptun_su(ins, Rsrc1, Rsrc2, Rdest) alpha_encode_fpop( ins, 0x16, 0x5A4, Rsrc1, Rsrc2, Rdest )
#define alpha_cmpteq_su(ins, Rsrc1, Rsrc2, Rdest) alpha_encode_fpop( ins, 0x16, 0x5A5, Rsrc1, Rsrc2, Rdest )
#define alpha_cmptlt_su(ins, Rsrc1, Rsrc2, Rdest) alpha_encode_fpop( ins, 0x16, 0x5A6, Rsrc1, Rsrc2, Rdest )
#define alpha_cmptle_su(ins, Rsrc1, Rsrc2, Rdest) alpha_encode_fpop( ins, 0x16, 0x5A7, Rsrc1, Rsrc2, Rdest )                                                                           

#define alpha_cvtts(ins, Rsrc2, Rdest) alpha_encode_fpop( ins, 0x16, 0x0AC, alpha_fzero, Rsrc2, Rdest )
#define alpha_cvttq(ins, Rsrc2, Rdest) alpha_encode_fpop( ins, 0x16, 0x0AF, alpha_fzero, Rsrc2, Rdest )
#define alpha_cvtqs(ins, Rsrc2, Rdest) alpha_encode_fpop( ins, 0x16, 0x0BC, alpha_fzero, Rsrc2, Rdest )
#define alpha_cvtqt(ins, Rsrc2, Rdest) alpha_encode_fpop( ins, 0x16, 0x0BE, alpha_fzero, Rsrc2, Rdest )

#define alpha_divt_su(ins, Rsrc1, Rsrc2, Rdest) alpha_encode_fpop( ins, 0x16, 0x5A3, Rsrc1, Rsrc2, Rdest )

#define alpha_cvtts_su(ins, Rsrc2, Rdest) alpha_encode_fpop( ins, 0x16, 0x5AC, alpha_fzero, Rsrc2, Rdest )

#define alpha_cpys(ins,  Rsrc1, Rsrc2, Rdest) alpha_encode_fpop( ins, 0x17, 0x020, Rsrc1, Rsrc2, Rdest )
#define alpha_cpysn(ins,  Rsrc1, Rsrc2, Rdest) alpha_encode_fpop( ins, 0x17, 0x021, Rsrc1, Rsrc2, Rdest )
#define alpha_cpyse(ins,  Rsrc1, Rsrc2, Rdest) alpha_encode_fpop( ins, 0x17, 0x022, Rsrc1, Rsrc2, Rdest )

#define	alpha_trapb(ins)	alpha_encode_mem_fc( ins, 0x18, 0x0000, 0, 0, 0 )
#define	alpha_mb(ins)		alpha_encode_mem_fc( ins, 0x18, 0x4000, 0, 0, 0 )

#endif

