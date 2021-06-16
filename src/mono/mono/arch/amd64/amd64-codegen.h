/*
 * amd64-codegen.h: Macros for generating amd64 code
 *
 * Authors:
 *   Paolo Molaro (lupus@ximian.com)
 *   Intel Corporation (ORP Project)
 *   Sergey Chaban (serge@wildwestsoftware.com)
 *   Dietmar Maurer (dietmar@ximian.com)
 *   Patrik Torstensson
 *   Zalman Stern
 * 
 * Copyright (C)  2000 Intel Corporation.  All rights reserved.
 * Copyright (C)  2001, 2002 Ximian, Inc.
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#ifndef AMD64_H
#define AMD64_H

// Conventions in this file:

// body: implementation. other macros call this one
// disp: displacement
// inst: instruction
// is_half: short if true, byte if false (then why is it named is_half...?)
// imm: immediate
// mem: read from (immediate-supplied address?)
// membase: read from address in a base register plus a displacement
// memindex: SIP addressing: (address in base register) + (displacement in index register)<<(shift)
// reg: register, encode modR/M bits 00
// regp: register, encode modR/M bits 11
// size: Expected 1,2,4 or 8
// widen: extends from 1 or 2 bytes

#include <glib.h>

typedef enum {
	AMD64_RAX = 0,
	AMD64_RCX = 1,
	AMD64_RDX = 2,
	AMD64_RBX = 3,
	AMD64_RSP = 4,
	AMD64_RBP = 5,
	AMD64_RSI = 6,
	AMD64_RDI = 7,
	AMD64_R8 = 8,
	AMD64_R9 = 9,
	AMD64_R10 = 10,
	AMD64_R11 = 11,
	AMD64_R12 = 12,
	AMD64_R13 = 13,
	AMD64_R14 = 14,
	AMD64_R15 = 15,
	AMD64_RIP = 16,
	AMD64_NREG
} AMD64_Reg_No;

typedef enum {
	AMD64_XMM0 = 0,
	AMD64_XMM1 = 1,
	AMD64_XMM2 = 2,
	AMD64_XMM3 = 3,
	AMD64_XMM4 = 4,
	AMD64_XMM5 = 5,
	AMD64_XMM6 = 6,
	AMD64_XMM7 = 7,
	AMD64_XMM8 = 8,
	AMD64_XMM9 = 9,
	AMD64_XMM10 = 10,
	AMD64_XMM11 = 11,
	AMD64_XMM12 = 12,
	AMD64_XMM13 = 13,
	AMD64_XMM14 = 14,
	AMD64_XMM15 = 15,
	AMD64_XMM_NREG = 16,
} AMD64_XMM_Reg_No;

typedef enum
{
  AMD64_REX_B = 1, /* The register in r/m field, base register in SIB byte, or reg in opcode is 8-15 rather than 0-7 */
  AMD64_REX_X = 2, /* The index register in SIB byte is 8-15 rather than 0-7 */
  AMD64_REX_R = 4, /* The reg field of ModRM byte is 8-15 rather than 0-7 */
  AMD64_REX_W = 8  /* Opeartion is 64-bits instead of 32 (default) or 16 (with 0x66 prefix) */
} AMD64_REX_Bits;

#define amd64_codegen_pre(inst)
#define amd64_codegen_post(inst)

#ifdef TARGET_WIN32
#define AMD64_ARG_REG1 AMD64_RCX
#define AMD64_ARG_REG2 AMD64_RDX
#define AMD64_ARG_REG3 AMD64_R8
#define AMD64_ARG_REG4 AMD64_R9
#else
#define AMD64_ARG_REG1 AMD64_RDI
#define AMD64_ARG_REG2 AMD64_RSI
#define AMD64_ARG_REG3 AMD64_RDX
#define AMD64_ARG_REG4 AMD64_RCX
#endif

#ifdef TARGET_WIN32
#define AMD64_CALLEE_REGS ((1<<AMD64_RAX) | (1<<AMD64_RCX) | (1<<AMD64_RDX) | (1<<AMD64_R8) | (1<<AMD64_R9) | (1<<AMD64_R10))
#define AMD64_IS_CALLEE_REG(reg)  (AMD64_CALLEE_REGS & (1 << (reg)))

#define AMD64_ARGUMENT_REGS ((1<<AMD64_RDX) | (1<<AMD64_RCX) | (1<<AMD64_R8) | (1<<AMD64_R9))
#define AMD64_IS_ARGUMENT_REG(reg) (AMD64_ARGUMENT_REGS & (1 << (reg)))

/* xmm0-xmm3 for standard calling convention, additionally xmm4-xmm5 for __vectorcall (not currently used) */
#define AMD64_ARGUMENT_XREGS ((1<<AMD64_XMM0) | (1<<AMD64_XMM1) | (1<<AMD64_XMM2) | (1<<AMD64_XMM3) | (1<<AMD64_XMM4) | (1<<AMD64_XMM5))
#define AMD64_IS_ARGUMENT_XREG(reg) (AMD64_ARGUMENT_XREGS & (1 << (reg)))

#define AMD64_CALLEE_SAVED_REGS ((1<<AMD64_RDI) | (1<<AMD64_RSI) | (1<<AMD64_RBX) | (1<<AMD64_R12) | (1<<AMD64_R13) | (1<<AMD64_R14) | (1<<AMD64_R15) | (1<<AMD64_RBP))
#define AMD64_IS_CALLEE_SAVED_REG(reg) (AMD64_CALLEE_SAVED_REGS & (1 << (reg)))
#else
#define AMD64_CALLEE_REGS ((1<<AMD64_RAX) | (1<<AMD64_RCX) | (1<<AMD64_RDX) | (1<<AMD64_RSI) | (1<<AMD64_RDI) | (1<<AMD64_R8) | (1<<AMD64_R9) | (1<<AMD64_R10))
#define AMD64_IS_CALLEE_REG(reg)  (AMD64_CALLEE_REGS & (1 << (reg)))

#define AMD64_ARGUMENT_REGS ((1<<AMD64_RDI) | (1<<AMD64_RSI) | (1<<AMD64_RDX) | (1<<AMD64_RCX) | (1<<AMD64_R8) | (1<<AMD64_R9))
#define AMD64_IS_ARGUMENT_REG(reg) (AMD64_ARGUMENT_REGS & (1 << (reg)))

#define AMD64_ARGUMENT_XREGS ((1<<AMD64_XMM0) | (1<<AMD64_XMM1) | (1<<AMD64_XMM2) | (1<<AMD64_XMM3) | (1<<AMD64_XMM4) | (1<<AMD64_XMM5) | (1<<AMD64_XMM6) | (1<<AMD64_XMM7))
#define AMD64_IS_ARGUMENT_XREG(reg) (AMD64_ARGUMENT_XREGS & (1 << (reg)))

#define AMD64_CALLEE_SAVED_REGS ((1<<AMD64_RBX) | (1<<AMD64_R12) | (1<<AMD64_R13) | (1<<AMD64_R14) | (1<<AMD64_R15) | (1<<AMD64_RBP))
#define AMD64_IS_CALLEE_SAVED_REG(reg) (AMD64_CALLEE_SAVED_REGS & (1 << (reg)))
#endif

#define AMD64_REX(bits) ((unsigned char)(0x40 | (bits)))
#define amd64_emit_rex(inst, width, reg_modrm, reg_index, reg_rm_base_opcode) do \
	{ \
		unsigned char _amd64_rex_bits = \
			(((width) > 4) ? AMD64_REX_W : 0) | \
			(((reg_modrm) > 7) ? AMD64_REX_R : 0) | \
			(((reg_index) > 7) ? AMD64_REX_X : 0) | \
			(((reg_rm_base_opcode) > 7) ? AMD64_REX_B : 0); \
		if ((_amd64_rex_bits != 0) || (((width) == 1))) *(inst)++ = AMD64_REX(_amd64_rex_bits); \
	} while (0)

typedef union {
	guint64 val;
	unsigned char b [8];
} amd64_imm_buf;

#include "../x86/x86-codegen.h"

/* In 64 bit mode, all registers have a low byte subregister */
#undef X86_IS_BYTE_REG
#define X86_IS_BYTE_REG(reg) 1

#define amd64_modrm_mod(modrm) ((modrm) >> 6)
#define amd64_modrm_reg(modrm) (((modrm) >> 3) & 0x7)
#define amd64_modrm_rm(modrm) ((modrm) & 0x7)

#define amd64_rex_r(rex) ((((rex) >> 2) & 0x1) << 3)
#define amd64_rex_x(rex) ((((rex) >> 1) & 0x1) << 3)
#define amd64_rex_b(rex) ((((rex) >> 0) & 0x1) << 3)

#define amd64_sib_scale(sib) ((sib) >> 6)
#define amd64_sib_index(sib) (((sib) >> 3) & 0x7)
#define amd64_sib_base(sib) ((sib) & 0x7)

#define amd64_is_imm32(val) ((gint64)val >= -((gint64)1<<31) && (gint64)val <= (((gint64)1<<31)-1))

#define x86_imm_emit64(inst,imm)     \
	do {	\
			amd64_imm_buf imb; 	\
			imb.val = (guint64) (imm);	\
			*(inst)++ = imb.b [0];	\
			*(inst)++ = imb.b [1];	\
			*(inst)++ = imb.b [2];	\
			*(inst)++ = imb.b [3];	\
			*(inst)++ = imb.b [4];	\
			*(inst)++ = imb.b [5];	\
			*(inst)++ = imb.b [6];	\
			*(inst)++ = imb.b [7];	\
	} while (0)

#define amd64_membase_emit(inst,reg,basereg,disp) do { \
	if ((basereg) == AMD64_RIP) { \
        x86_address_byte ((inst), 0, (reg)&0x7, 5); \
        x86_imm_emit32 ((inst), (disp)); \
    } \
	else \
		x86_membase_emit ((inst),(reg)&0x7, (basereg)&0x7, (disp)); \
} while (0)

#define amd64_alu_reg_imm_size_body(inst,opc,reg,imm,size) \
	do {	\
		if (x86_is_imm8((imm))) {	\
			amd64_emit_rex(inst, size, 0, 0, (reg)); \
			*(inst)++ = (unsigned char)0x83;	\
			x86_reg_emit ((inst), (opc), (reg));	\
			x86_imm_emit8 ((inst), (imm));	\
		} else if ((reg) == AMD64_RAX) {	\
			amd64_emit_rex(inst, size, 0, 0, 0); \
			*(inst)++ = (((unsigned char)(opc)) << 3) + 5;	\
			x86_imm_emit32 ((inst), (imm));	\
		} else {	\
			amd64_emit_rex(inst, size, 0, 0, (reg)); \
			*(inst)++ = (unsigned char)0x81;	\
			x86_reg_emit ((inst), (opc), (reg));	\
			x86_imm_emit32 ((inst), (imm));	\
		}	\
	} while (0)

#define amd64_alu_reg_reg_size_body(inst,opc,dreg,reg,size)	\
	do {	\
		amd64_emit_rex(inst, size, (dreg), 0, (reg)); \
		*(inst)++ = (((unsigned char)(opc)) << 3) + 3;	\
		x86_reg_emit ((inst), (dreg), (reg));	\
	} while (0)

#define amd64_test_reg_imm_size_body(inst,reg,imm,size) \
	do { \
		amd64_codegen_pre(inst); \
		amd64_emit_rex ((inst),(size),0,0,(reg)); \
		if ((reg) == AMD64_RAX) { \
			*(inst)++ = (unsigned char)0xa9; \
		} \
		else { \
			*(inst)++ = (unsigned char)0xf7;	\
			x86_reg_emit((inst), 0, (reg));	\
		} \
		x86_imm_emit32((inst), (imm));	\
		amd64_codegen_post(inst); \
	} while (0)

#define amd64_alu_reg_imm_size(inst,opc,reg,imm,size) \
	amd64_alu_reg_imm_size_body((inst), (opc), (reg), (imm), (size))

#define amd64_alu_reg_reg_size(inst,opc,dreg,reg,size) \
		amd64_alu_reg_reg_size_body((inst), (opc), (dreg), (reg), (size))

#define amd64_test_reg_imm_size(inst, reg, imm, size) \
		amd64_test_reg_imm_size_body(inst, reg, imm, size)

#define amd64_alu_reg_imm(inst,opc,reg,imm) amd64_alu_reg_imm_size((inst),(opc),(reg),(imm),8)

#define amd64_alu_reg_reg(inst,opc,dreg,reg) amd64_alu_reg_reg_size ((inst),(opc),(dreg),(reg),8)

#define amd64_test_reg_imm(inst,reg,imm) amd64_test_reg_imm_size(inst,reg,imm,8)

#define amd64_alu_reg_membase_size(inst,opc,reg,basereg,disp,size) \
	do { \
		amd64_codegen_pre(inst);						  \
		amd64_emit_rex ((inst),(size),(reg),0,(basereg)); \
		*(inst)++ = (((unsigned char)(opc)) << 3) + 3;	\
		amd64_membase_emit (inst, reg, basereg, disp); \
		amd64_codegen_post(inst);					   \
} while (0)

#define amd64_mov_regp_reg(inst,regp,reg,size)	\
	do {	\
		amd64_codegen_pre(inst); \
		if ((size) == 2) \
			x86_prefix((inst), X86_OPERAND_PREFIX); \
		amd64_emit_rex(inst, (size), (reg), 0, (regp)); \
		switch ((size)) {	\
		case 1: *(inst)++ = (unsigned char)0x88; break;	\
		case 2: case 4: case 8: *(inst)++ = (unsigned char)0x89; break;	\
		default: assert (0);	\
		}	\
		x86_regp_emit ((inst), (reg), (regp));	\
		amd64_codegen_post(inst); \
	} while (0)

#define amd64_mov_membase_reg(inst,basereg,disp,reg,size)	\
	do {	\
		amd64_codegen_pre(inst); \
		if ((size) == 2) \
			x86_prefix((inst), X86_OPERAND_PREFIX); \
		amd64_emit_rex(inst, (size), (reg), 0, (basereg)); \
		switch ((size)) {	\
		case 1: *(inst)++ = (unsigned char)0x88; break;	\
		case 2: case 4: case 8: *(inst)++ = (unsigned char)0x89; break;	\
		default: assert (0);	\
		}	\
		x86_membase_emit ((inst), ((reg)&0x7), ((basereg)&0x7), (disp));	\
		amd64_codegen_post(inst); \
	} while (0)

#define amd64_mov_mem_reg(inst,mem,reg,size)	\
	do {	\
		amd64_codegen_pre(inst); \
		if ((size) == 2) \
			x86_prefix((inst), X86_OPERAND_PREFIX); \
		amd64_emit_rex(inst, (size), (reg), 0, 0); \
		switch ((size)) {	\
		case 1: *(inst)++ = (unsigned char)0x88; break;	\
		case 2: case 4: case 8: *(inst)++ = (unsigned char)0x89; break;	\
		default: assert (0);	\
		}	\
		x86_address_byte ((inst), 0, (reg), 4); \
		x86_address_byte ((inst), 0, 4, 5); \
		x86_imm_emit32 ((inst), (mem)); \
		amd64_codegen_post(inst); \
	} while (0)

#define amd64_mov_reg_reg(inst,dreg,reg,size)	\
	do {	\
		amd64_codegen_pre(inst); \
		if ((size) == 2) \
			x86_prefix((inst), X86_OPERAND_PREFIX); \
		amd64_emit_rex(inst, (size), (dreg), 0, (reg)); \
		switch ((size)) {	\
		case 1: *(inst)++ = (unsigned char)0x8a; break;	\
		case 2: case 4: case 8: *(inst)++ = (unsigned char)0x8b; break;	\
		default: assert (0);	\
		}	\
		x86_reg_emit ((inst), (dreg), (reg));	\
		amd64_codegen_post(inst); \
	} while (0)

#define amd64_mov_reg_mem_body(inst,reg,mem,size)	\
	do {	\
		amd64_codegen_pre(inst); \
		if ((size) == 2) \
			x86_prefix((inst), X86_OPERAND_PREFIX); \
		amd64_emit_rex(inst, (size), (reg), 0, 0); \
		switch ((size)) {	\
		case 1: *(inst)++ = (unsigned char)0x8a; break;	\
		case 2: case 4: case 8: *(inst)++ = (unsigned char)0x8b; break;	\
		default: assert (0);	\
		}	\
		x86_address_byte ((inst), 0, (reg), 4); \
		x86_address_byte ((inst), 0, 4, 5); \
		x86_imm_emit32 ((inst), (mem)); \
		amd64_codegen_post(inst); \
	} while (0)

#define amd64_mov_reg_mem(inst,reg,mem,size)	\
	do {    \
		amd64_mov_reg_mem_body((inst),(reg),(mem),(size)); \
	} while (0)

#define amd64_mov_reg_membase_body(inst,reg,basereg,disp,size)	\
	do {	\
		if ((size) == 2) \
			x86_prefix((inst), X86_OPERAND_PREFIX); \
		amd64_emit_rex(inst, (size), (reg), 0, (basereg)); \
		switch ((size)) {	\
		case 1: *(inst)++ = (unsigned char)0x8a; break;	\
		case 2: case 4: case 8: *(inst)++ = (unsigned char)0x8b; break;	\
		default: assert (0);	\
		}	\
		amd64_membase_emit ((inst), (reg), (basereg), (disp));	\
	} while (0)

#define amd64_mov_reg_memindex_size_body(inst,reg,basereg,disp,indexreg,shift,size) \
	do { \
		amd64_emit_rex ((inst),(size),(reg),(indexreg),(basereg)); \
		x86_mov_reg_memindex((inst),((reg)&0x7),((basereg)&0x7),(disp),((indexreg)&0x7),(shift),(size) == 8 ? 4 : (size)); \
	} while (0)

#define amd64_mov_reg_memindex_size(inst,reg,basereg,disp,indexreg,shift,size) \
	amd64_mov_reg_memindex_size_body((inst),(reg),(basereg),(disp),(indexreg),(shift),(size))
#define amd64_mov_reg_membase(inst,reg,basereg,disp,size)	\
	do {	\
		amd64_mov_reg_membase_body((inst), (reg), (basereg), (disp), (size)); \
	} while (0)

#define amd64_movzx_reg_membase(inst,reg,basereg,disp,size)	\
	do {	\
		amd64_codegen_pre(inst); \
		amd64_emit_rex(inst, (size), (reg), 0, (basereg)); \
		switch ((size)) {	\
		case 1: *(inst)++ = (unsigned char)0x0f; *(inst)++ = (unsigned char)0xb6; break;	\
		case 2: *(inst)++ = (unsigned char)0x0f; *(inst)++ = (unsigned char)0xb7; break;	\
		case 4: case 8: *(inst)++ = (unsigned char)0x8b; break;	\
		default: assert (0);	\
		}	\
		x86_membase_emit ((inst), ((reg)&0x7), ((basereg)&0x7), (disp));	\
		amd64_codegen_post(inst); \
	} while (0)

#define amd64_movsxd_reg_mem(inst,reg,mem) \
    do {     \
	amd64_codegen_pre(inst); \
	amd64_emit_rex(inst,8,(reg),0,0); \
	*(inst)++ = (unsigned char)0x63; \
	x86_mem_emit ((inst), ((reg)&0x7), (mem)); \
	amd64_codegen_post(inst); \
    } while (0)

#define amd64_movsxd_reg_membase(inst,reg,basereg,disp) \
    do {     \
	amd64_codegen_pre(inst); \
	amd64_emit_rex(inst,8,(reg),0,(basereg)); \
	*(inst)++ = (unsigned char)0x63; \
	x86_membase_emit ((inst), ((reg)&0x7), ((basereg)&0x7), (disp)); \
	amd64_codegen_post(inst); \
    } while (0)

#define amd64_movsxd_reg_reg(inst,dreg,reg) \
    do {     \
	amd64_codegen_pre(inst); \
	amd64_emit_rex(inst,8,(dreg),0,(reg)); \
	*(inst)++ = (unsigned char)0x63; \
	x86_reg_emit ((inst), (dreg), (reg));	\
	amd64_codegen_post(inst); \
    } while (0)

/* Pretty much the only instruction that supports a 64-bit immediate. Optimize for common case of
 * 32-bit immediate. Pepper with casts to avoid warnings.
 */
#define amd64_mov_reg_imm_size(inst,reg,imm,size)	\
	do {	\
		amd64_codegen_pre(inst); \
		amd64_emit_rex(inst, (size), 0, 0, (reg)); \
		*(inst)++ = (unsigned char)0xb8 + ((reg) & 0x7);	\
		if ((size) == 8) \
			x86_imm_emit64 ((inst), (guint64)(imm));	\
		else \
			x86_imm_emit32 ((inst), (int)(guint64)(imm));	\
		amd64_codegen_post(inst); \
	} while (0)

#define amd64_mov_reg_imm(inst,reg,imm)	\
	do {	\
		amd64_codegen_pre(inst); \
		amd64_mov_reg_imm_size ((inst), (reg), (imm), (amd64_is_imm32 (((gint64)imm)) ? 4 : 8)); \
		amd64_codegen_post(inst); \
	} while (0)

#define amd64_set_reg_template(inst,reg) amd64_mov_reg_imm_size ((inst),(reg), 0, 8)

#define amd64_set_template(inst,reg) amd64_set_reg_template((inst),(reg))

#define amd64_mov_membase_imm(inst,basereg,disp,imm,size)	\
	do {	\
		amd64_codegen_pre(inst); \
		if ((size) == 2) \
			x86_prefix((inst), X86_OPERAND_PREFIX); \
		amd64_emit_rex(inst, (size) == 1 ? 0 : (size), 0, 0, (basereg)); \
		if ((size) == 1) {	\
			*(inst)++ = (unsigned char)0xc6;	\
			x86_membase_emit ((inst), 0, (basereg) & 0x7, (disp));	\
			x86_imm_emit8 ((inst), (imm));	\
		} else if ((size) == 2) {	\
			*(inst)++ = (unsigned char)0xc7;	\
			x86_membase_emit ((inst), 0, (basereg) & 0x7, (disp));	\
			x86_imm_emit16 ((inst), (imm));	\
		} else {	\
			*(inst)++ = (unsigned char)0xc7;	\
			x86_membase_emit ((inst), 0, (basereg) & 0x7, (disp));	\
			x86_imm_emit32 ((inst), (imm));	\
		}	\
		amd64_codegen_post(inst); \
	} while (0)


#define amd64_lea_membase_body(inst,reg,basereg,disp,width)	\
	do {	\
		amd64_emit_rex(inst, width, (reg), 0, (basereg)); \
		*(inst)++ = (unsigned char)0x8d;	\
		amd64_membase_emit ((inst), (reg), (basereg), (disp));	\
	} while (0)

#define amd64_lea4_membase(inst,reg,basereg,disp) \
	amd64_lea_membase_body((inst), (reg), (basereg), (disp), 4)

#define amd64_lea_membase(inst,reg,basereg,disp) \
	amd64_lea_membase_body((inst), (reg), (basereg), (disp), 8)

/* Instruction are implicitly 64-bits so don't generate REX for just the size. */
#define amd64_push_reg(inst,reg)	\
	do {	\
		amd64_codegen_pre(inst); \
		amd64_emit_rex(inst, 0, 0, 0, (reg)); \
		*(inst)++ = (unsigned char)0x50 + ((reg) & 0x7);	\
		amd64_codegen_post(inst); \
	} while (0)

/* Instruction is implicitly 64-bits so don't generate REX for just the size. */
#define amd64_push_membase(inst,basereg,disp)	\
	do {	\
		amd64_codegen_pre(inst); \
		amd64_emit_rex(inst, 0, 0, 0, (basereg)); \
		*(inst)++ = (unsigned char)0xff;	\
		x86_membase_emit ((inst), 6, (basereg) & 0x7, (disp));	\
		amd64_codegen_post(inst); \
	} while (0)

#define amd64_pop_reg_body(inst,reg)	\
	do {	\
		amd64_codegen_pre(inst);  \
		amd64_emit_rex(inst, 0, 0, 0, (reg)); \
		*(inst)++ = (unsigned char)0x58 + ((reg) & 0x7);	\
		amd64_codegen_post(inst);  \
	} while (0)

#define amd64_call_reg(inst,reg)	\
	do {	\
		amd64_emit_rex(inst, 0, 0, 0, (reg)); \
		*(inst)++ = (unsigned char)0xff;	\
		x86_reg_emit ((inst), 2, ((reg) & 0x7));	\
	} while (0)


#define amd64_ret(inst) do { *(inst)++ = (unsigned char)0xc3; } while (0)
#define amd64_leave(inst) do { *(inst)++ = (unsigned char)0xc9; } while (0)

#define amd64_pop_reg(inst,reg) amd64_pop_reg_body((inst), (reg))

#define amd64_movsd_reg_regp(inst,reg,regp)	\
	do {	\
		amd64_codegen_pre(inst); \
		x86_prefix((inst), 0xf2); \
		amd64_emit_rex(inst, 0, (reg), 0, (regp)); \
		*(inst)++ = (unsigned char)0x0f;	\
		*(inst)++ = (unsigned char)0x10;	\
		x86_regp_emit ((inst), (reg) & 0x7, (regp) & 0x7);	\
		amd64_codegen_post(inst); \
	} while (0)

#define amd64_movsd_regp_reg(inst,regp,reg)	\
	do {	\
		amd64_codegen_pre(inst); \
		x86_prefix((inst), 0xf2); \
		amd64_emit_rex(inst, 0, (reg), 0, (regp)); \
		*(inst)++ = (unsigned char)0x0f;	\
		*(inst)++ = (unsigned char)0x11;	\
		x86_regp_emit ((inst), (reg) & 0x7, (regp) & 0x7);	\
		amd64_codegen_post(inst); \
	} while (0)

#define amd64_movss_reg_regp(inst,reg,regp)	\
	do {	\
		amd64_codegen_pre(inst); \
		x86_prefix((inst), 0xf3); \
		amd64_emit_rex(inst, 0, (reg), 0, (regp)); \
		*(inst)++ = (unsigned char)0x0f;	\
		*(inst)++ = (unsigned char)0x10;	\
		x86_regp_emit ((inst), (reg) & 0x7, (regp) & 0x7);	\
		amd64_codegen_post(inst); \
	} while (0)

#define amd64_movss_regp_reg(inst,regp,reg)	\
	do {	\
		amd64_codegen_pre(inst); \
		x86_prefix((inst), 0xf3); \
		amd64_emit_rex(inst, 0, (reg), 0, (regp)); \
		*(inst)++ = (unsigned char)0x0f;	\
		*(inst)++ = (unsigned char)0x11;	\
		x86_regp_emit ((inst), (reg) & 0x7, (regp) & 0x7);	\
		amd64_codegen_post(inst); \
	} while (0)

#define amd64_movdqu_reg_membase(inst,reg,basereg,disp)	\
	do {	\
		amd64_codegen_pre(inst); \
		x86_prefix((inst), 0xf3); \
		amd64_emit_rex(inst, 0, (reg), 0, (basereg)); \
		*(inst)++ = (unsigned char)0x0f;	\
		*(inst)++ = (unsigned char)0x6f;	\
		x86_membase_emit ((inst), (reg) & 0x7, (basereg) & 0x7, (disp));	\
		amd64_codegen_post(inst); \
	} while (0)

#define amd64_movsd_reg_membase(inst,reg,basereg,disp)	\
	do {	\
		amd64_codegen_pre(inst); \
		x86_prefix((inst), 0xf2); \
		amd64_emit_rex(inst, 0, (reg), 0, (basereg)); \
		*(inst)++ = (unsigned char)0x0f;	\
		*(inst)++ = (unsigned char)0x10;	\
		x86_membase_emit ((inst), (reg) & 0x7, (basereg) & 0x7, (disp));	\
		amd64_codegen_post(inst); \
	} while (0)

#define amd64_movss_reg_membase(inst,reg,basereg,disp)	\
	do {	\
		amd64_codegen_pre(inst); \
		x86_prefix((inst), 0xf3); \
		amd64_emit_rex(inst, 0, (reg), 0, (basereg)); \
		*(inst)++ = (unsigned char)0x0f;	\
		*(inst)++ = (unsigned char)0x10;	\
		x86_membase_emit ((inst), (reg) & 0x7, (basereg) & 0x7, (disp));	\
		amd64_codegen_post(inst); \
	} while (0)

#define amd64_movdqu_membase_reg(inst,basereg,disp,reg)	\
	do {	\
		amd64_codegen_pre(inst); \
		x86_prefix((inst), 0xf3); \
		amd64_emit_rex(inst, 0, (reg), 0, (basereg)); \
		*(inst)++ = (unsigned char)0x0f;	\
		*(inst)++ = (unsigned char)0x7f;	\
		x86_membase_emit ((inst), (reg) & 0x7, (basereg) & 0x7, (disp));	\
		amd64_codegen_post(inst); \
	} while (0)

#define amd64_movsd_membase_reg(inst,basereg,disp,reg)	\
	do {	\
		amd64_codegen_pre(inst); \
		x86_prefix((inst), 0xf2); \
		amd64_emit_rex(inst, 0, (reg), 0, (basereg)); \
		*(inst)++ = (unsigned char)0x0f;	\
		*(inst)++ = (unsigned char)0x11;	\
		x86_membase_emit ((inst), (reg) & 0x7, (basereg) & 0x7, (disp));	\
		amd64_codegen_post(inst); \
	} while (0)

#define amd64_movss_membase_reg(inst,basereg,disp,reg)	\
	do {	\
		amd64_codegen_pre(inst); \
		x86_prefix((inst), 0xf3); \
		amd64_emit_rex(inst, 0, (reg), 0, (basereg)); \
		*(inst)++ = (unsigned char)0x0f;	\
		*(inst)++ = (unsigned char)0x11;	\
		x86_membase_emit ((inst), (reg) & 0x7, (basereg) & 0x7, (disp));	\
		amd64_codegen_post(inst); \
	} while (0)

/* The original inc_reg opcode is used as the REX prefix */
#define amd64_inc_reg_size(inst,reg,size) \
	do { \
		amd64_codegen_pre(inst); \
		amd64_emit_rex ((inst),(size),0,0,(reg)); \
		*(inst)++ = (unsigned char)0xff; \
		x86_reg_emit ((inst),0,(reg) & 0x7); \
		amd64_codegen_post(inst); \
	} while (0)

#define amd64_dec_reg_size(inst,reg,size) \
	do { \
		amd64_codegen_pre(inst); \
		amd64_emit_rex ((inst),(size),0,0,(reg)); \
		*(inst)++ = (unsigned char)0xff; \
		x86_reg_emit ((inst),1,(reg) & 0x7); \
		amd64_codegen_post(inst); \
	} while (0)

#define amd64_fld_membase_size(inst,basereg,disp,is_double,size) do { \
	amd64_codegen_pre(inst); \
	amd64_emit_rex ((inst),0,0,0,(basereg)); \
	*(inst)++ = (is_double) ? (unsigned char)0xdd : (unsigned char)0xd9;	\
	amd64_membase_emit ((inst), 0, (basereg), (disp));	\
	amd64_codegen_post(inst); \
} while (0)

/* From the AMD64 Software Optimization Manual */
#define amd64_padding_size(inst,size) \
    do { \
	    switch ((size)) {								  \
        case 1: *(inst)++ = 0x90; break;						  \
        case 2: *(inst)++ = 0x66; *(inst)++ = 0x90; break;			  \
        case 3: *(inst)++ = 0x66; *(inst)++ = 0x66; *(inst)++ = 0x90; break; \
		default: amd64_emit_rex ((inst),8,0,0,0); x86_padding ((inst), (size) - 1); \
		}; \
		} while (0)

#define amd64_call_membase_size(inst,basereg,disp,size) do { amd64_emit_rex ((inst),0,0,0,(basereg)); *(inst)++ = (unsigned char)0xff; amd64_membase_emit ((inst),2, (basereg),(disp)); } while (0)
#define amd64_jump_membase_size(inst,basereg,disp,size) do { amd64_emit_rex ((inst),0,0,0,(basereg)); *(inst)++ = (unsigned char)0xff; amd64_membase_emit ((inst), 4, (basereg), (disp)); } while (0)
    
#define amd64_jump_code_size(inst,target,size) do { \
	if (amd64_is_imm32 ((gint64)(target) - (gint64)(inst))) {		\
		x86_jump_code((inst),(target));									\
	} else {															\
	    amd64_jump_membase ((inst), AMD64_RIP, 0);							\
		*(guint64*)(inst) = (guint64)(target);							\
		(inst) += 8; \
	} \
} while (0)

/*
 * SSE
 */

//TODO Reorganize SSE opcode defines.

/* Two opcode SSE defines */

#define emit_sse_reg_reg_op2_size(inst,dreg,reg,op1,op2,size) do { \
    amd64_codegen_pre(inst); \
    amd64_emit_rex ((inst), size, (dreg), 0, (reg)); \
    *(inst)++ = (unsigned char)(op1); \
    *(inst)++ = (unsigned char)(op2); \
    x86_reg_emit ((inst), (dreg), (reg)); \
    amd64_codegen_post(inst); \
} while (0)

#define emit_sse_reg_reg_op2(inst,dreg,reg,op1,op2) emit_sse_reg_reg_op2_size ((inst), (dreg), (reg), (op1), (op2), 0)

#define emit_sse_reg_reg_op2_imm(inst,dreg,reg,op1,op2,imm) do { \
   amd64_codegen_pre(inst); \
   emit_sse_reg_reg_op2 ((inst), (dreg), (reg), (op1), (op2)); \
   x86_imm_emit8 ((inst), (imm)); \
   amd64_codegen_post(inst); \
} while (0)

#define emit_sse_membase_reg_op2(inst,basereg,disp,reg,op1,op2) do { \
    amd64_codegen_pre(inst); \
    amd64_emit_rex ((inst), 0, (reg), 0, (basereg)); \
    *(inst)++ = (unsigned char)(op1); \
    *(inst)++ = (unsigned char)(op2); \
    amd64_membase_emit ((inst), (reg), (basereg), (disp)); \
    amd64_codegen_post(inst); \
} while (0)

#define emit_sse_reg_membase_op2(inst,dreg,basereg,disp,op1,op2) do { \
    amd64_codegen_pre(inst); \
    amd64_emit_rex ((inst), 0, (dreg), 0, (basereg) == AMD64_RIP ? 0 : (basereg)); \
    *(inst)++ = (unsigned char)(op1); \
    *(inst)++ = (unsigned char)(op2); \
    amd64_membase_emit ((inst), (dreg), (basereg), (disp)); \
    amd64_codegen_post(inst); \
} while (0)

/* Three opcode SSE defines */

#define emit_opcode3(inst,op1,op2,op3) do { \
   *(inst)++ = (unsigned char)(op1); \
   *(inst)++ = (unsigned char)(op2); \
   *(inst)++ = (unsigned char)(op3); \
} while (0)

#define emit_sse_reg_reg_size(inst,dreg,reg,op1,op2,op3,size) do { \
    amd64_codegen_pre(inst); \
    *(inst)++ = (unsigned char)(op1); \
	amd64_emit_rex ((inst), size, (dreg), 0, (reg)); \
    *(inst)++ = (unsigned char)(op2); \
    *(inst)++ = (unsigned char)(op3); \
    x86_reg_emit ((inst), (dreg), (reg)); \
    amd64_codegen_post(inst); \
} while (0)

#define emit_sse_reg_reg(inst,dreg,reg,op1,op2,op3) emit_sse_reg_reg_size ((inst), (dreg), (reg), (op1), (op2), (op3), 0)

#define emit_sse_reg_reg_imm(inst,dreg,reg,op1,op2,op3,imm) do { \
   amd64_codegen_pre(inst); \
   emit_sse_reg_reg ((inst), (dreg), (reg), (op1), (op2), (op3)); \
   x86_imm_emit8 ((inst), (imm)); \
   amd64_codegen_post(inst); \
} while (0)

#define emit_sse_membase_reg(inst,basereg,disp,reg,op1,op2,op3) do { \
    amd64_codegen_pre(inst); \
    x86_prefix((inst), (unsigned char)(op1)); \
    amd64_emit_rex ((inst), 0, (reg), 0, (basereg)); \
    *(inst)++ = (unsigned char)(op2); \
    *(inst)++ = (unsigned char)(op3); \
    amd64_membase_emit ((inst), (reg), (basereg), (disp)); \
    amd64_codegen_post(inst); \
} while (0)

#define emit_sse_reg_membase(inst,dreg,basereg,disp,op1,op2,op3) do { \
    amd64_codegen_pre(inst); \
    x86_prefix((inst), (unsigned char)(op1)); \
    amd64_emit_rex ((inst), 0, (dreg), 0, (basereg) == AMD64_RIP ? 0 : (basereg)); \
    *(inst)++ = (unsigned char)(op2); \
    *(inst)++ = (unsigned char)(op3); \
    amd64_membase_emit ((inst), (dreg), (basereg), (disp)); \
    amd64_codegen_post(inst); \
} while (0)

/* Four opcode SSE defines */

#define emit_sse_reg_reg_op4_size(inst,dreg,reg,op1,op2,op3,op4,size) do { \
    amd64_codegen_pre(inst); \
    x86_prefix((inst), (unsigned char)(op1)); \
    amd64_emit_rex ((inst), size, (dreg), 0, (reg)); \
    *(inst)++ = (unsigned char)(op2); \
    *(inst)++ = (unsigned char)(op3); \
    *(inst)++ = (unsigned char)(op4); \
    x86_reg_emit ((inst), (dreg), (reg)); \
    amd64_codegen_post(inst); \
} while (0)

#define emit_sse_reg_reg_op4(inst,dreg,reg,op1,op2,op3,op4) emit_sse_reg_reg_op4_size ((inst), (dreg), (reg), (op1), (op2), (op3), (op4), 0)

#define emit_sse_reg_reg_op4_imm(inst,dreg,reg,op1,op2,op3,op4,imm) do { \
    amd64_codegen_pre(inst); \
    emit_sse_reg_reg_op4 ((inst), (dreg), (reg), (op1), (op2), (op3), (op4)); \
    x86_imm_emit8 ((inst), (imm)); \
    amd64_codegen_post(inst); \
} while (0)

/* specific SSE opcode defines */
 
#define amd64_sse_xorpd_reg_reg(inst,dreg,reg) emit_sse_reg_reg ((inst),(dreg),(reg), 0x66, 0x0f, 0x57)

#define amd64_sse_xorpd_reg_membase(inst,dreg,basereg,disp) emit_sse_reg_membase ((inst),(dreg),(basereg), (disp), 0x66, 0x0f, 0x57)

#define amd64_sse_andpd_reg_membase(inst,dreg,basereg,disp) emit_sse_reg_membase ((inst),(dreg),(basereg), (disp), 0x66, 0x0f, 0x54)

#define amd64_sse_movsd_reg_reg(inst,dreg,reg) emit_sse_reg_reg ((inst), (dreg), (reg), 0xf2, 0x0f, 0x10)
#define amd64_sse_movss_reg_reg(inst,dreg,reg) emit_sse_reg_reg ((inst), (dreg), (reg), 0xf3, 0x0f, 0x10)

#define amd64_sse_movsd_reg_membase(inst,dreg,basereg,disp) emit_sse_reg_membase ((inst), (dreg), (basereg), (disp), 0xf2, 0x0f, 0x10)

#define amd64_sse_movsd_membase_reg(inst,basereg,disp,reg) emit_sse_membase_reg ((inst), (basereg), (disp), (reg), 0xf2, 0x0f, 0x11)

#define amd64_sse_movss_membase_reg(inst,basereg,disp,reg) emit_sse_membase_reg ((inst), (basereg), (disp), (reg), 0xf3, 0x0f, 0x11)

#define amd64_sse_movss_reg_membase(inst,dreg,basereg,disp) emit_sse_reg_membase ((inst), (dreg), (basereg), (disp), 0xf3, 0x0f, 0x10)

#define amd64_sse_comisd_reg_reg(inst,dreg,reg) emit_sse_reg_reg ((inst),(dreg),(reg),0x66,0x0f,0x2f)
#define amd64_sse_comiss_reg_reg(inst,dreg,reg) emit_sse_reg_reg ((inst),(dreg),(reg),0x67,0x0f,0x2f)

#define amd64_sse_comisd_reg_membase(inst,dreg,basereg,disp) emit_sse_reg_membase ((inst), (dreg), (basereg), (disp), 0x66, 0x0f, 0x2f)

#define amd64_sse_ucomisd_reg_reg(inst,dreg,reg) emit_sse_reg_reg ((inst),(dreg),(reg),0x66,0x0f,0x2e)

#define amd64_sse_cvtsd2si_reg_reg(inst,dreg,reg) emit_sse_reg_reg_size ((inst), (dreg), (reg), 0xf2, 0x0f, 0x2d, 8)
#define amd64_sse_cvtss2si_reg_reg(inst,dreg,reg) emit_sse_reg_reg_size ((inst), (dreg), (reg), 0xf3, 0x0f, 0x2d, 8)

#define amd64_sse_cvttsd2si_reg_reg_size(inst,dreg,reg,size) emit_sse_reg_reg_size ((inst), (dreg), (reg), 0xf2, 0x0f, 0x2c, (size))
#define amd64_sse_cvtss2si_reg_reg_size(inst,dreg,reg,size) emit_sse_reg_reg_size ((inst), (dreg), (reg), 0xf3, 0x0f, 0x2c, (size))

#define amd64_sse_cvttsd2si_reg_reg(inst,dreg,reg) amd64_sse_cvttsd2si_reg_reg_size ((inst), (dreg), (reg), 8)

#define amd64_sse_cvtsi2sd_reg_reg_size(inst,dreg,reg,size) emit_sse_reg_reg_size ((inst), (dreg), (reg), 0xf2, 0x0f, 0x2a, (size))

#define amd64_sse_cvtsi2sd_reg_reg(inst,dreg,reg) amd64_sse_cvtsi2sd_reg_reg_size ((inst), (dreg), (reg), 8)

#define amd64_sse_cvtsi2ss_reg_reg_size(inst,dreg,reg,size) emit_sse_reg_reg_size ((inst), (dreg), (reg), 0xf3, 0x0f, 0x2a, (size))

#define amd64_sse_cvtsi2ss_reg_reg(inst,dreg,reg) amd64_sse_cvtsi2ss_reg_reg_size ((inst), (dreg), (reg), 8)

#define amd64_sse_cvtsd2ss_reg_reg(inst,dreg,reg) emit_sse_reg_reg ((inst), (dreg), (reg), 0xf2, 0x0f, 0x5a)

#define amd64_sse_cvtss2sd_reg_reg(inst,dreg,reg) emit_sse_reg_reg ((inst), (dreg), (reg), 0xf3, 0x0f, 0x5a)

#define amd64_sse_addsd_reg_reg(inst,dreg,reg) emit_sse_reg_reg ((inst), (dreg), (reg), 0xf2, 0x0f, 0x58)
#define amd64_sse_addss_reg_reg(inst,dreg,reg) emit_sse_reg_reg ((inst), (dreg), (reg), 0xf3, 0x0f, 0x58)

#define amd64_sse_subsd_reg_reg(inst,dreg,reg) emit_sse_reg_reg ((inst), (dreg), (reg), 0xf2, 0x0f, 0x5c)
#define amd64_sse_subss_reg_reg(inst,dreg,reg) emit_sse_reg_reg ((inst), (dreg), (reg), 0xf3, 0x0f, 0x5c)

#define amd64_sse_mulsd_reg_reg(inst,dreg,reg) emit_sse_reg_reg ((inst), (dreg), (reg), 0xf2, 0x0f, 0x59)
#define amd64_sse_mulss_reg_reg(inst,dreg,reg) emit_sse_reg_reg ((inst), (dreg), (reg), 0xf3, 0x0f, 0x59)

#define amd64_sse_divsd_reg_reg(inst,dreg,reg) emit_sse_reg_reg ((inst), (dreg), (reg), 0xf2, 0x0f, 0x5e)
#define amd64_sse_divss_reg_reg(inst,dreg,reg) emit_sse_reg_reg ((inst), (dreg), (reg), 0xf3, 0x0f, 0x5e)

#define amd64_sse_sqrtsd_reg_reg(inst,dreg,reg) emit_sse_reg_reg((inst), (dreg), (reg), 0xf2, 0x0f, 0x51)


#define amd64_sse_pinsrw_reg_reg_imm(inst,dreg,reg,imm) emit_sse_reg_reg_imm ((inst), (dreg), (reg), 0x66, 0x0f, 0xc4, (imm))

#define amd64_sse_pextrw_reg_reg_imm(inst,dreg,reg,imm) emit_sse_reg_reg_imm ((inst), (dreg), (reg), 0x66, 0x0f, 0xc5, (imm))


#define amd64_sse_cvttsd2si_reg_xreg_size(inst,reg,xreg,size) emit_sse_reg_reg_size ((inst), (reg), (xreg), 0xf2, 0x0f, 0x2c, (size))


#define amd64_sse_addps_reg_reg(inst,dreg,reg) emit_sse_reg_reg_op2((inst), (dreg), (reg), 0x0f, 0x58)

#define amd64_sse_divps_reg_reg(inst,dreg,reg) emit_sse_reg_reg_op2((inst), (dreg), (reg), 0x0f, 0x5e)

#define amd64_sse_mulps_reg_reg(inst,dreg,reg) emit_sse_reg_reg_op2((inst), (dreg), (reg), 0x0f, 0x59)

#define amd64_sse_subps_reg_reg(inst,dreg,reg) emit_sse_reg_reg_op2((inst), (dreg), (reg), 0x0f, 0x5c)

#define amd64_sse_maxps_reg_reg(inst,dreg,reg) emit_sse_reg_reg_op2((inst), (dreg), (reg), 0x0f, 0x5f)

#define amd64_sse_minps_reg_reg(inst,dreg,reg) emit_sse_reg_reg_op2((inst), (dreg), (reg), 0x0f, 0x5d)

#define amd64_sse_cmpps_reg_reg_imm(inst,dreg,reg,imm) emit_sse_reg_reg_op2_imm((inst), (dreg), (reg), 0x0f, 0xc2, (imm))

#define amd64_sse_andps_reg_reg(inst,dreg,reg) emit_sse_reg_reg_op2((inst), (dreg), (reg), 0x0f, 0x54)

#define amd64_sse_andnps_reg_reg(inst,dreg,reg) emit_sse_reg_reg_op2((inst), (dreg), (reg), 0x0f, 0x55)

#define amd64_sse_orps_reg_reg(inst,dreg,reg) emit_sse_reg_reg_op2((inst), (dreg), (reg), 0x0f, 0x56)

#define amd64_sse_xorps_reg_reg(inst,dreg,reg) emit_sse_reg_reg_op2((inst), (dreg), (reg), 0x0f, 0x57)

#define amd64_sse_sqrtps_reg_reg(inst,dreg,reg) emit_sse_reg_reg_op2((inst), (dreg), (reg), 0x0f, 0x51)

#define amd64_sse_rsqrtps_reg_reg(inst,dreg,reg) emit_sse_reg_reg_op2((inst), (dreg), (reg), 0x0f, 0x52)

#define amd64_sse_rcpps_reg_reg(inst,dreg,reg) emit_sse_reg_reg_op2((inst), (dreg), (reg), 0x0f, 0x53)

#define amd64_sse_addsubps_reg_reg(inst,dreg,reg) emit_sse_reg_reg((inst), (dreg), (reg), 0xf2, 0x0f, 0xd0)

#define amd64_sse_haddps_reg_reg(inst,dreg,reg) emit_sse_reg_reg((inst), (dreg), (reg), 0xf2, 0x0f, 0x7c)

#define amd64_sse_hsubps_reg_reg(inst,dreg,reg) emit_sse_reg_reg((inst), (dreg), (reg), 0xf2, 0x0f, 0x7d)

#define amd64_sse_movshdup_reg_reg(inst,dreg,reg) emit_sse_reg_reg((inst), (dreg), (reg), 0xf3, 0x0f, 0x16)

#define amd64_sse_movsldup_reg_reg(inst,dreg,reg) emit_sse_reg_reg((inst), (dreg), (reg), 0xf3, 0x0f, 0x12)


#define amd64_sse_pshufhw_reg_reg_imm(inst,dreg,reg,imm) emit_sse_reg_reg_imm((inst), (dreg), (reg), 0xf3, 0x0f, 0x70, (imm))

#define amd64_sse_pshuflw_reg_reg_imm(inst,dreg,reg,imm) emit_sse_reg_reg_imm((inst), (dreg), (reg), 0xf2, 0x0f, 0x70, (imm))

#define amd64_sse_pshufd_reg_reg_imm(inst,dreg,reg,imm) emit_sse_reg_reg_imm((inst), (dreg), (reg), 0x66, 0x0f, 0x70, (imm))

#define amd64_sse_shufps_reg_reg_imm(inst,dreg,reg,imm) emit_sse_reg_reg_op2_imm((inst), (dreg), (reg), 0x0f, 0xC6, (imm))

#define amd64_sse_shufpd_reg_reg_imm(inst,dreg,reg,imm) emit_sse_reg_reg_imm((inst), (dreg), (reg), 0x66, 0x0f, 0xC6, (imm))

#define amd64_sse_roundpd_reg_reg_imm(inst, dreg, reg, imm) emit_sse_reg_reg_op4_imm((inst), (dreg), (reg), 0x66, 0x0f, 0x3a, 0x09, (imm))

#define amd64_sse_addpd_reg_reg(inst,dreg,reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0x58)

#define amd64_sse_divpd_reg_reg(inst,dreg,reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0x5e)

#define amd64_sse_mulpd_reg_reg(inst,dreg,reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0x59)

#define amd64_sse_subpd_reg_reg(inst,dreg,reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0x5c)

#define amd64_sse_maxpd_reg_reg(inst,dreg,reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0x5f)

#define amd64_sse_minpd_reg_reg(inst,dreg,reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0x5d)

#define amd64_sse_cmppd_reg_reg_imm(inst,dreg,reg,imm) emit_sse_reg_reg_imm((inst), (dreg), (reg), 0x66, 0x0f, 0xc2, (imm))

#define amd64_sse_andpd_reg_reg(inst,dreg,reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0x54)

#define amd64_sse_andnpd_reg_reg(inst,dreg,reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0x55)

#define amd64_sse_orpd_reg_reg(inst,dreg,reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0x56)

#define amd64_sse_sqrtpd_reg_reg(inst,dreg,reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0x51)

#define amd64_sse_rsqrtpd_reg_reg(inst,dreg,reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0x52)

#define amd64_sse_rcppd_reg_reg(inst,dreg,reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0x53)

#define amd64_sse_addsubpd_reg_reg(inst,dreg,reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0xd0)

#define amd64_sse_haddpd_reg_reg(inst,dreg,reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0x7c)

#define amd64_sse_hsubpd_reg_reg(inst,dreg,reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0x7d)

#define amd64_sse_movddup_reg_reg(inst,dreg,reg) emit_sse_reg_reg((inst), (dreg), (reg), 0xf2, 0x0f, 0x12)


#define amd64_sse_pmovmskb_reg_reg(inst,dreg,reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0xd7)


#define amd64_sse_pand_reg_reg(inst, dreg, reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0xdb)

#define amd64_sse_pandn_reg_reg(inst, dreg, reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0xdf)

#define amd64_sse_por_reg_reg(inst, dreg, reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0xeb)

#define amd64_sse_pxor_reg_reg(inst, dreg, reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0xef)


#define amd64_sse_paddb_reg_reg(inst, dreg, reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0xfc)

#define amd64_sse_paddw_reg_reg(inst, dreg, reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0xfd)

#define amd64_sse_paddd_reg_reg(inst, dreg, reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0xfe)

#define amd64_sse_paddq_reg_reg(inst, dreg, reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0xd4)


#define amd64_sse_psubb_reg_reg(inst, dreg, reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0xf8)

#define amd64_sse_psubw_reg_reg(inst, dreg, reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0xf9)

#define amd64_sse_psubd_reg_reg(inst, dreg, reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0xfa)

#define amd64_sse_psubq_reg_reg(inst, dreg, reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0xfb)


#define amd64_sse_pmaxub_reg_reg(inst, dreg, reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0xde)

#define amd64_sse_pmaxuw_reg_reg(inst, dreg, reg) emit_sse_reg_reg_op4((inst), (dreg), (reg), 0x66, 0x0f, 0x38, 0x3e)

#define amd64_sse_pmaxud_reg_reg(inst, dreg, reg) emit_sse_reg_reg_op4((inst), (dreg), (reg), 0x66, 0x0f, 0x38, 0x3f)


#define amd64_sse_pmaxsb_reg_reg(inst, dreg, reg) emit_sse_reg_reg_op4((inst), (dreg), (reg), 0x66, 0x0f, 0x38, 0x3c)

#define amd64_sse_pmaxsw_reg_reg(inst, dreg, reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0xee)

#define amd64_sse_pmaxsd_reg_reg(inst, dreg, reg) emit_sse_reg_reg_op4((inst), (dreg), (reg), 0x66, 0x0f, 0x38, 0x3d)


#define amd64_sse_pavgb_reg_reg(inst, dreg, reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0xe0)

#define amd64_sse_pavgw_reg_reg(inst, dreg, reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0xe3)


#define amd64_sse_pminub_reg_reg(inst, dreg, reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0xda)

#define amd64_sse_pminuw_reg_reg(inst, dreg, reg) emit_sse_reg_reg_op4((inst), (dreg), (reg), 0x66, 0x0f, 0x38, 0x3a)

#define amd64_sse_pminud_reg_reg(inst, dreg, reg) emit_sse_reg_reg_op4((inst), (dreg), (reg), 0x66, 0x0f, 0x38, 0x3b)


#define amd64_sse_pminsb_reg_reg(inst, dreg, reg) emit_sse_reg_reg_op4((inst), (dreg), (reg), 0x66, 0x0f, 0x38, 0x38)

#define amd64_sse_pminsw_reg_reg(inst, dreg, reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0xea)

#define amd64_sse_pminsd_reg_reg(inst, dreg, reg) emit_sse_reg_reg_op4((inst), (dreg), (reg), 0x66, 0x0f, 0x38, 0x39)


#define amd64_sse_pcmpeqb_reg_reg(inst, dreg, reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0x74)

#define amd64_sse_pcmpeqw_reg_reg(inst, dreg, reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0x75)

#define amd64_sse_pcmpeqd_reg_reg(inst, dreg, reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0x76)

#define amd64_sse_pcmpeqq_reg_reg(inst, dreg, reg) emit_sse_reg_reg_op4((inst), (dreg), (reg), 0x66, 0x0f, 0x38, 0x29)


#define amd64_sse_pcmpgtb_reg_reg(inst, dreg, reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0x64)

#define amd64_sse_pcmpgtw_reg_reg(inst, dreg, reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0x65)

#define amd64_sse_pcmpgtd_reg_reg(inst, dreg, reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0x66)

#define amd64_sse_pcmpgtq_reg_reg(inst, dreg, reg) emit_sse_reg_reg_op4((inst), (dreg), (reg), 0x66, 0x0f, 0x38, 0x37)


#define amd64_sse_psadbw_reg_reg(inst, dreg, reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0xf6)


#define amd64_sse_punpcklbw_reg_reg(inst, dreg, reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0x60)

#define amd64_sse_punpcklwd_reg_reg(inst, dreg, reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0x61)

#define amd64_sse_punpckldq_reg_reg(inst, dreg, reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0x62)

#define amd64_sse_punpcklqdq_reg_reg(inst, dreg, reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0x6c)

#define amd64_sse_unpcklpd_reg_reg(inst, dreg, reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0x14)

#define amd64_sse_unpcklps_reg_reg(inst, dreg, reg) emit_sse_reg_reg_op2((inst), (dreg), (reg), 0x0f, 0x14)


#define amd64_sse_punpckhbw_reg_reg(inst, dreg, reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0x68)

#define amd64_sse_punpckhwd_reg_reg(inst, dreg, reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0x69)

#define amd64_sse_punpckhdq_reg_reg(inst, dreg, reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0x6a)

#define amd64_sse_punpckhqdq_reg_reg(inst, dreg, reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0x6d)

#define amd64_sse_unpckhpd_reg_reg(inst, dreg, reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0x15)

#define amd64_sse_unpckhps_reg_reg(inst, dreg, reg) emit_sse_reg_reg_op2((inst), (dreg), (reg), 0x0f, 0x15)


#define amd64_sse_packsswb_reg_reg(inst, dreg, reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0x63)

#define amd64_sse_packssdw_reg_reg(inst, dreg, reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0x6b)

#define amd64_sse_packuswb_reg_reg(inst, dreg, reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0x67)

#define amd64_sse_packusdw_reg_reg(inst, dreg, reg) emit_sse_reg_reg_op4((inst), (dreg), (reg), 0x66, 0x0f, 0x38, 0x2b)


#define amd64_sse_paddusb_reg_reg(inst, dreg, reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0xdc)

#define amd64_sse_psubusb_reg_reg(inst, dreg, reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0xd8)

#define amd64_sse_paddusw_reg_reg(inst, dreg, reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0xdd)

#define amd64_sse_psubusw_reg_reg(inst, dreg, reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0xd8)


#define amd64_sse_paddsb_reg_reg(inst, dreg, reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0xec)

#define amd64_sse_psubsb_reg_reg(inst, dreg, reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0xe8)

#define amd64_sse_paddsw_reg_reg(inst, dreg, reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0xed)

#define amd64_sse_psubsw_reg_reg(inst, dreg, reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0xe9)


#define amd64_sse_pmullw_reg_reg(inst, dreg, reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0xd5)

#define amd64_sse_pmulld_reg_reg(inst, dreg, reg) emit_sse_reg_reg_op4((inst), (dreg), (reg), 0x66, 0x0f, 0x38, 0x40)

#define amd64_sse_pmuludq_reg_reg(inst, dreg, reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0xf4)

#define amd64_sse_pmulhuw_reg_reg(inst, dreg, reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0xe4)

#define amd64_sse_pmulhw_reg_reg(inst, dreg, reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0xe5)


#define amd64_sse_psrlw_reg_imm(inst, reg, imm) emit_sse_reg_reg_imm((inst), X86_SSE_SHR, (reg), 0x66, 0x0f, 0x71, (imm))

#define amd64_sse_psrlw_reg_reg(inst, dreg, reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0xd1)


#define amd64_sse_psraw_reg_imm(inst, reg, imm) emit_sse_reg_reg_imm((inst), X86_SSE_SAR, (reg), 0x66, 0x0f, 0x71, (imm))

#define amd64_sse_psraw_reg_reg(inst, dreg, reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0xe1)


#define amd64_sse_psllw_reg_imm(inst, reg, imm) emit_sse_reg_reg_imm((inst), X86_SSE_SHL, (reg), 0x66, 0x0f, 0x71, (imm))

#define amd64_sse_psllw_reg_reg(inst, dreg, reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0xf1)


#define amd64_sse_psrld_reg_imm(inst, reg, imm) emit_sse_reg_reg_imm((inst), X86_SSE_SHR, (reg), 0x66, 0x0f, 0x72, (imm))

#define amd64_sse_psrld_reg_reg(inst, dreg, reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0xd2)


#define amd64_sse_psrad_reg_imm(inst, reg, imm) emit_sse_reg_reg_imm((inst), X86_SSE_SAR, (reg), 0x66, 0x0f, 0x72, (imm))

#define amd64_sse_psrad_reg_reg(inst, dreg, reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0xe2)


#define amd64_sse_pslld_reg_imm(inst, reg, imm) emit_sse_reg_reg_imm((inst), X86_SSE_SHL, (reg), 0x66, 0x0f, 0x72, (imm))

#define amd64_sse_pslld_reg_reg(inst, dreg, reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0xf2)


#define amd64_sse_psrlq_reg_imm(inst, reg, imm) emit_sse_reg_reg_imm((inst), X86_SSE_SHR, (reg), 0x66, 0x0f, 0x73, (imm))

#define amd64_sse_psrlq_reg_reg(inst, dreg, reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0xd3)


#define amd64_sse_psraq_reg_imm(inst, reg, imm) emit_sse_reg_reg_imm((inst), X86_SSE_SAR, (reg), 0x66, 0x0f, 0x73, (imm))

#define amd64_sse_psraq_reg_reg(inst, dreg, reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0xe3)


#define amd64_sse_psllq_reg_imm(inst, reg, imm) emit_sse_reg_reg_imm((inst), X86_SSE_SHL, (reg), 0x66, 0x0f, 0x73, (imm))

#define amd64_sse_psllq_reg_reg(inst, dreg, reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0f, 0xf3)


#define amd64_sse_cvtdq2pd_reg_reg(inst, dreg, reg) emit_sse_reg_reg((inst), (dreg), (reg), 0xF3, 0x0F, 0xE6)

#define amd64_sse_cvtdq2ps_reg_reg(inst, dreg, reg) emit_sse_reg_reg_op2((inst), (dreg), (reg), 0x0F, 0x5B)

#define amd64_sse_cvtpd2dq_reg_reg(inst, dreg, reg) emit_sse_reg_reg((inst), (dreg), (reg), 0xF2, 0x0F, 0xE6)

#define amd64_sse_cvtpd2ps_reg_reg(inst, dreg, reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0F, 0x5A)

#define amd64_sse_cvtps2dq_reg_reg(inst, dreg, reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0F, 0x5B)

#define amd64_sse_cvtps2pd_reg_reg(inst, dreg, reg) emit_sse_reg_reg_op2((inst), (dreg), (reg), 0x0F, 0x5A)

#define amd64_sse_cvttpd2dq_reg_reg(inst, dreg, reg) emit_sse_reg_reg((inst), (dreg), (reg), 0x66, 0x0F, 0xE6)

#define amd64_sse_cvttps2dq_reg_reg(inst, dreg, reg) emit_sse_reg_reg((inst), (dreg), (reg), 0xF3, 0x0F, 0x5B)


#define amd64_movd_xreg_reg_size(inst,dreg,sreg,size) emit_sse_reg_reg_size((inst), (dreg), (sreg), 0x66, 0x0f, 0x6e, (size))

#define amd64_movd_reg_xreg_size(inst,dreg,sreg,size) emit_sse_reg_reg_size((inst), (sreg), (dreg), 0x66, 0x0f, 0x7e, (size))

#define amd64_movd_xreg_membase(inst,dreg,basereg,disp) emit_sse_reg_membase((inst), (dreg), (basereg), (disp), 0x66, 0x0f, 0x6e)


#define amd64_movlhps_reg_reg(inst,dreg,sreg) emit_sse_reg_reg_op2((inst), (dreg), (sreg), 0x0f, 0x16)

#define amd64_movhlps_reg_reg(inst,dreg,sreg) emit_sse_reg_reg_op2((inst), (dreg), (sreg), 0x0f, 0x12)

#define amd64_sse_movups_membase_reg(inst, basereg, disp, reg) emit_sse_membase_reg_op2((inst), (basereg), (disp), (reg), 0x0f, 0x11)

#define amd64_sse_movups_reg_membase(inst, dreg, basereg, disp) emit_sse_reg_membase_op2((inst), (dreg), (basereg), (disp), 0x0f, 0x10)

#define amd64_sse_movaps_membase_reg(inst, basereg, disp, reg) emit_sse_membase_reg_op2((inst), (basereg), (disp), (reg), 0x0f, 0x29)

#define amd64_sse_movaps_reg_membase(inst, dreg, basereg, disp) emit_sse_reg_membase_op2((inst), (dreg), (basereg), (disp), 0x0f, 0x28)

#define amd64_sse_movaps_reg_reg(inst, dreg, reg) emit_sse_reg_reg_op2((inst), (dreg), (reg), 0x0f, 0x28)

#define amd64_sse_movntps_reg_membase(inst, dreg, basereg, disp) emit_sse_reg_membase_op2((inst), (dreg), (basereg), (disp), 0x0f, 0x2b)

#define amd64_sse_prefetch_reg_membase(inst, arg, basereg, disp) emit_sse_reg_membase_op2((inst), (arg), (basereg), (disp), 0x0f, 0x18)

#define amd64_sse_lzcnt_reg_reg_size(inst, dreg, reg, size) emit_sse_reg_reg_size((inst), (dreg), (reg), 0xf3, 0x0f, 0xbd, (size))
#define amd64_sse_popcnt_reg_reg_size(inst, dreg, reg, size) emit_sse_reg_reg_size((inst), (dreg), (reg), 0xf3, 0x0f, 0xb8, (size))

/* Generated from x86-codegen.h */

#define amd64_breakpoint_size(inst,size) do { x86_breakpoint(inst); } while (0)
#define amd64_cld_size(inst,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),0,0,0,0); x86_cld(inst); amd64_codegen_post(inst); } while (0)
#define amd64_stosb_size(inst,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,0); x86_stosb(inst); amd64_codegen_post(inst); } while (0)
#define amd64_stosl_size(inst,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,0); x86_stosl(inst); amd64_codegen_post(inst); } while (0)
#define amd64_stosd_size(inst,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,0); x86_stosd(inst); amd64_codegen_post(inst); } while (0)
#define amd64_movsb_size(inst,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,0); x86_movsb(inst); amd64_codegen_post(inst); } while (0)
#define amd64_movsl_size(inst,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,0); x86_movsl(inst); amd64_codegen_post(inst); } while (0)
#define amd64_movsd_size(inst,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,0); x86_movsd(inst); amd64_codegen_post(inst); } while (0)
#define amd64_prefix_size(inst,p,size) do { x86_prefix((inst), p); } while (0)
#define amd64_rdtsc_size(inst,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,0); x86_rdtsc(inst); amd64_codegen_post(inst); } while (0)
#define amd64_cmpxchg_reg_reg_size(inst,dreg,reg,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),(dreg),0,(reg)); x86_cmpxchg_reg_reg((inst),((dreg)&0x7),((reg)&0x7)); amd64_codegen_post(inst); } while (0)
#define amd64_cmpxchg_mem_reg_size(inst,mem,reg,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,(reg)); x86_cmpxchg_mem_reg((inst),(mem),((reg)&0x7)); amd64_codegen_post(inst); } while (0)
#define amd64_cmpxchg_membase_reg_size(inst,basereg,disp,reg,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),(reg),0,(basereg)); x86_cmpxchg_membase_reg((inst),((basereg)&0x7),(disp),((reg)&0x7)); amd64_codegen_post(inst); } while (0)
#define amd64_xchg_reg_reg_size(inst,dreg,reg,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),(dreg),0,(reg)); x86_xchg_reg_reg((inst),((dreg)&0x7),((reg)&0x7),(size) == 8 ? 4 : (size)); amd64_codegen_post(inst); } while (0)
#define amd64_xchg_mem_reg_size(inst,mem,reg,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,(reg)); x86_xchg_mem_reg((inst),(mem),((reg)&0x7),(size) == 8 ? 4 : (size)); amd64_codegen_post(inst); } while (0)
#define amd64_xchg_membase_reg_size(inst,basereg,disp,reg,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),(reg),0,(basereg)); x86_xchg_membase_reg((inst),((basereg)&0x7),(disp),((reg)&0x7),(size) == 8 ? 4 : (size)); amd64_codegen_post(inst); } while (0)
#define amd64_inc_mem_size(inst,mem,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,0); x86_inc_mem((inst),(mem)); amd64_codegen_post(inst); } while (0)
#define amd64_inc_membase_size(inst,basereg,disp,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,(basereg)); x86_inc_membase((inst),((basereg)&0x7),(disp)); amd64_codegen_post(inst); } while (0)
//#define amd64_inc_reg_size(inst,reg,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,(reg)); x86_inc_reg((inst),((reg)&0x7)); amd64_codegen_post(inst); } while (0)
#define amd64_dec_mem_size(inst,mem,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,0); x86_dec_mem((inst),(mem)); amd64_codegen_post(inst); } while (0)
#define amd64_dec_membase_size(inst,basereg,disp,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,(basereg)); x86_dec_membase((inst),((basereg)&0x7),(disp)); amd64_codegen_post(inst); } while (0)
//#define amd64_dec_reg_size(inst,reg,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,(reg)); x86_dec_reg((inst),((reg)&0x7)); amd64_codegen_post(inst); } while (0)
#define amd64_not_mem_size(inst,mem,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,0); x86_not_mem((inst),(mem)); amd64_codegen_post(inst); } while (0)
#define amd64_not_membase_size(inst,basereg,disp,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,(basereg)); x86_not_membase((inst),((basereg)&0x7),(disp)); amd64_codegen_post(inst); } while (0)
#define amd64_not_reg_size(inst,reg,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,(reg)); x86_not_reg((inst),((reg)&0x7)); amd64_codegen_post(inst); } while (0)
#define amd64_neg_mem_size(inst,mem,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,0); x86_neg_mem((inst),(mem)); amd64_codegen_post(inst); } while (0)
#define amd64_neg_membase_size(inst,basereg,disp,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,(basereg)); x86_neg_membase((inst),((basereg)&0x7),(disp)); amd64_codegen_post(inst); } while (0)
#define amd64_neg_reg_size(inst,reg,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,(reg)); x86_neg_reg((inst),((reg)&0x7)); amd64_codegen_post(inst); } while (0)
#define amd64_nop_size(inst,size) do { amd64_codegen_pre(inst); x86_nop(inst); amd64_codegen_post(inst); } while (0)
//#define amd64_alu_reg_imm_size(inst,opc,reg,imm,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,(reg)); x86_alu_reg_imm((inst),(opc),((reg)&0x7),(imm)); amd64_codegen_post(inst); } while (0)
#define amd64_alu_mem_imm_size(inst,opc,mem,imm,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,0); x86_alu_mem_imm((inst),(opc),(mem),(imm)); amd64_codegen_post(inst); } while (0)
#define amd64_alu_membase_imm_size(inst,opc,basereg,disp,imm,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,(basereg)); x86_alu_membase_imm((inst),(opc),((basereg)&0x7),(disp),(imm)); amd64_codegen_post(inst); } while (0)
#define amd64_alu_membase8_imm_size(inst,opc,basereg,disp,imm,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,(basereg)); x86_alu_membase8_imm((inst),(opc),((basereg)&0x7),(disp),(imm)); amd64_codegen_post(inst); } while (0)	
#define amd64_alu_mem_reg_size(inst,opc,mem,reg,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,(reg)); x86_alu_mem_reg((inst),(opc),(mem),((reg)&0x7)); amd64_codegen_post(inst); } while (0)
#define amd64_alu_membase_reg_size(inst,opc,basereg,disp,reg,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),(reg),0,(basereg)); x86_alu_membase_reg((inst),(opc),((basereg)&0x7),(disp),((reg)&0x7)); amd64_codegen_post(inst); } while (0)
//#define amd64_alu_reg_reg_size(inst,opc,dreg,reg,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),(dreg),0,(reg)); x86_alu_reg_reg((inst),(opc),((dreg)&0x7),((reg)&0x7)); amd64_codegen_post(inst); } while (0)
#define amd64_alu_reg8_reg8_size(inst,opc,dreg,reg,is_dreg_h,is_reg_h,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),(dreg),0,(reg)); x86_alu_reg8_reg8((inst),(opc),((dreg)&0x7),((reg)&0x7),(is_dreg_h),(is_reg_h)); amd64_codegen_post(inst); } while (0)
#define amd64_alu_reg_mem_size(inst,opc,reg,mem,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,(reg)); x86_alu_reg_mem((inst),(opc),((reg)&0x7),(mem)); amd64_codegen_post(inst); } while (0)
//#define amd64_alu_reg_membase_size(inst,opc,reg,basereg,disp,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),(reg),0,(basereg)); x86_alu_reg_membase((inst),(opc),((reg)&0x7),((basereg)&0x7),(disp)); amd64_codegen_post(inst); } while (0)
//#define amd64_test_reg_imm_size(inst,reg,imm,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,(reg)); x86_test_reg_imm((inst),((reg)&0x7),(imm)); amd64_codegen_post(inst); } while (0)
#define amd64_test_mem_imm_size(inst,mem,imm,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,0); x86_test_mem_imm((inst),(mem),(imm)); amd64_codegen_post(inst); } while (0)
#define amd64_test_membase_imm_size(inst,basereg,disp,imm,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,(basereg)); x86_test_membase_imm((inst),((basereg)&0x7),(disp),(imm)); amd64_codegen_post(inst); } while (0)
#define amd64_test_reg_reg_size(inst,dreg,reg,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),(dreg),0,(reg)); x86_test_reg_reg((inst),((dreg)&0x7),((reg)&0x7)); amd64_codegen_post(inst); } while (0)
#define amd64_test_mem_reg_size(inst,mem,reg,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,(reg)); x86_test_mem_reg((inst),(mem),((reg)&0x7)); amd64_codegen_post(inst); } while (0)
#define amd64_test_membase_reg_size(inst,basereg,disp,reg,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),(reg),0,(basereg)); x86_test_membase_reg((inst),((basereg)&0x7),(disp),((reg)&0x7)); amd64_codegen_post(inst); } while (0)
#define amd64_shift_reg_imm_size(inst,opc,reg,imm,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,(reg)); x86_shift_reg_imm((inst),(opc),((reg)&0x7),(imm)); amd64_codegen_post(inst); } while (0)
#define amd64_shift_mem_imm_size(inst,opc,mem,imm,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,(reg)); x86_shift_mem_imm((inst),(opc),(mem),(imm)); amd64_codegen_post(inst); } while (0)
#define amd64_shift_membase_imm_size(inst,opc,basereg,disp,imm,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),(reg),0,(basereg)); x86_shift_membase_imm((inst),(opc),((basereg)&0x7),(disp),(imm)); amd64_codegen_post(inst); } while (0)
#define amd64_shift_reg_size(inst,opc,reg,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,(reg)); x86_shift_reg((inst),(opc),((reg)&0x7)); amd64_codegen_post(inst); } while (0)
#define amd64_shift_mem_size(inst,opc,mem,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,(reg)); x86_shift_mem((inst),(opc),(mem)); amd64_codegen_post(inst); } while (0)
#define amd64_shift_membase_size(inst,opc,basereg,disp,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,(basereg)); x86_shift_membase((inst),(opc),((basereg)&0x7),(disp)); amd64_codegen_post(inst); } while (0)
#define amd64_shrd_reg_size(inst,dreg,reg,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),(dreg),0,(reg)); x86_shrd_reg((inst),((dreg)&0x7),((reg)&0x7)); amd64_codegen_post(inst); } while (0)
#define amd64_shrd_reg_imm_size(inst,dreg,reg,shamt,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),(dreg),0,(reg)); x86_shrd_reg_imm((inst),((dreg)&0x7),((reg)&0x7),(shamt)); amd64_codegen_post(inst); } while (0)
#define amd64_shld_reg_size(inst,dreg,reg,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),(dreg),0,(reg)); x86_shld_reg((inst),((dreg)&0x7),((reg)&0x7)); amd64_codegen_post(inst); } while (0)
#define amd64_shld_reg_imm_size(inst,dreg,reg,shamt,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),(dreg),0,(reg)); x86_shld_reg_imm((inst),((dreg)&0x7),((reg)&0x7),(shamt)); amd64_codegen_post(inst); } while (0)
#define amd64_mul_reg_size(inst,reg,is_signed,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,(reg)); x86_mul_reg((inst),((reg)&0x7),(is_signed)); amd64_codegen_post(inst); } while (0)
#define amd64_mul_mem_size(inst,mem,is_signed,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,0); x86_mul_mem((inst),(mem),(is_signed)); amd64_codegen_post(inst); } while (0)
#define amd64_mul_membase_size(inst,basereg,disp,is_signed,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,(basereg)); x86_mul_membase((inst),((basereg)&0x7),(disp),(is_signed)); amd64_codegen_post(inst); } while (0)
#define amd64_imul_reg_reg_size(inst,dreg,reg,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),(dreg),0,(reg)); x86_imul_reg_reg((inst),((dreg)&0x7),((reg)&0x7)); amd64_codegen_post(inst); } while (0)
#define amd64_imul_reg_mem_size(inst,reg,mem,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,(reg)); x86_imul_reg_mem((inst),((reg)&0x7),(mem)); amd64_codegen_post(inst); } while (0)
#define amd64_imul_reg_membase_size(inst,reg,basereg,disp,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),(reg),0,(basereg)); x86_imul_reg_membase((inst),((reg)&0x7),((basereg)&0x7),(disp)); amd64_codegen_post(inst); } while (0)
#define amd64_imul_reg_reg_imm_size(inst,dreg,reg,imm,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),(dreg),0,(reg)); x86_imul_reg_reg_imm((inst),((dreg)&0x7),((reg)&0x7),(imm)); amd64_codegen_post(inst); } while (0)
#define amd64_imul_reg_mem_imm_size(inst,reg,mem,imm,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,(reg)); x86_imul_reg_mem_imm((inst),((reg)&0x7),(mem),(imm)); amd64_codegen_post(inst); } while (0)
#define amd64_imul_reg_membase_imm_size(inst,reg,basereg,disp,imm,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),(reg),0,(basereg)); x86_imul_reg_membase_imm((inst),((reg)&0x7),((basereg)&0x7),(disp),(imm)); amd64_codegen_post(inst); } while (0)
#define amd64_div_reg_size(inst,reg,is_signed,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,(reg)); x86_div_reg((inst),((reg)&0x7),(is_signed)); amd64_codegen_post(inst); } while (0)
#define amd64_div_mem_size(inst,mem,is_signed,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,0); x86_div_mem((inst),(mem),(is_signed)); amd64_codegen_post(inst); } while (0)
#define amd64_div_membase_size(inst,basereg,disp,is_signed,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,(basereg)); x86_div_membase((inst),((basereg)&0x7),(disp),(is_signed)); amd64_codegen_post(inst); } while (0)
#define amd64_mov_mem_reg_size(inst,mem,reg,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,(reg)); x86_mov_mem_reg((inst),(mem),((reg)&0x7),(size) == 8 ? 4 : (size)); amd64_codegen_post(inst); } while (0)
//#define amd64_mov_regp_reg_size(inst,regp,reg,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),(regp),0,(reg)); x86_mov_regp_reg((inst),(regp),((reg)&0x7),(size) == 8 ? 4 : (size)); amd64_codegen_post(inst); } while (0)
//#define amd64_mov_membase_reg_size(inst,basereg,disp,reg,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),(reg),0,(basereg)); x86_mov_membase_reg((inst),((basereg)&0x7),(disp),((reg)&0x7),(size) == 8 ? 4 : (size)); amd64_codegen_post(inst); } while (0)
#define amd64_mov_memindex_reg_size(inst,basereg,disp,indexreg,shift,reg,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),(reg),(indexreg),(basereg)); x86_mov_memindex_reg((inst),((basereg)&0x7),(disp),((indexreg)&0x7),(shift),((reg)&0x7),(size) == 8 ? 4 : (size)); amd64_codegen_post(inst); } while (0)
//#define amd64_mov_reg_mem_size(inst,reg,mem,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,(reg)); x86_mov_reg_mem((inst),((reg)&0x7),(mem),(size) == 8 ? 4 : (size)); amd64_codegen_post(inst); } while (0)
//#define amd64_mov_reg_membase_size(inst,reg,basereg,disp,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),(reg),0,(basereg)); x86_mov_reg_membase((inst),((reg)&0x7),((basereg)&0x7),(disp),(size) == 8 ? 4 : (size)); amd64_codegen_post(inst); } while (0)
//#define amd64_mov_reg_memindex_size(inst,reg,basereg,disp,indexreg,shift,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),(reg),(indexreg),(basereg)); x86_mov_reg_memindex((inst),((reg)&0x7),((basereg)&0x7),(disp),((indexreg)&0x7),(shift),(size) == 8 ? 4 : (size)); amd64_codegen_post(inst); } while (0)
#define amd64_clear_reg_size(inst,reg,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,(reg)); x86_clear_reg((inst),((reg)&0x7)); amd64_codegen_post(inst); } while (0)
//#define amd64_mov_reg_imm_size(inst,reg,imm,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,(reg)); x86_mov_reg_imm((inst),((reg)&0x7),(imm)); amd64_codegen_post(inst); } while (0)
#define amd64_mov_mem_imm_size(inst,mem,imm,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,0); x86_mov_mem_imm((inst),(mem),(imm),(size) == 8 ? 4 : (size)); amd64_codegen_post(inst); } while (0)
//#define amd64_mov_membase_imm_size(inst,basereg,disp,imm,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,(basereg)); x86_mov_membase_imm((inst),((basereg)&0x7),(disp),(imm),(size) == 8 ? 4 : (size)); amd64_codegen_post(inst); } while (0)
#define amd64_mov_memindex_imm_size(inst,basereg,disp,indexreg,shift,imm,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,(indexreg),(basereg)); x86_mov_memindex_imm((inst),((basereg)&0x7),(disp),((indexreg)&0x7),(shift),(imm),(size) == 8 ? 4 : (size)); amd64_codegen_post(inst); } while (0)
#define amd64_lea_mem_size(inst,reg,mem,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,(reg)); x86_lea_mem((inst),((reg)&0x7),(mem)); amd64_codegen_post(inst); } while (0)
//#define amd64_lea_membase_size(inst,reg,basereg,disp,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,(basereg)); x86_lea_membase((inst),((reg)&0x7),((basereg)&0x7),(disp)); amd64_codegen_post(inst); } while (0)
#define amd64_lea_memindex_size(inst,reg,basereg,disp,indexreg,shift,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),(reg),(indexreg),(basereg)); x86_lea_memindex((inst),((reg)&0x7),((basereg)&0x7),(disp),((indexreg)&0x7),(shift)); amd64_codegen_post(inst); } while (0)
#define amd64_widen_reg_size(inst,dreg,reg,is_signed,is_half,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),(dreg),0,(reg)); x86_widen_reg((inst),((dreg)&0x7),((reg)&0x7),(is_signed),(is_half)); amd64_codegen_post(inst); } while (0)
#define amd64_widen_mem_size(inst,dreg,mem,is_signed,is_half,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),(dreg),0,0); x86_widen_mem((inst),((dreg)&0x7),(mem),(is_signed),(is_half)); amd64_codegen_post(inst); } while (0)
#define amd64_widen_membase_size(inst,dreg,basereg,disp,is_signed,is_half,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),(dreg),0,(basereg)); x86_widen_membase((inst),((dreg)&0x7),((basereg)&0x7),(disp),(is_signed),(is_half)); amd64_codegen_post(inst); } while (0)
#define amd64_widen_memindex_size(inst,dreg,basereg,disp,indexreg,shift,is_signed,is_half,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),(dreg),(indexreg),(basereg)); x86_widen_memindex((inst),((dreg)&0x7),((basereg)&0x7),(disp),((indexreg)&0x7),(shift),(is_signed),(is_half)); amd64_codegen_post(inst); } while (0)
#define amd64_cdq_size(inst,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,0); x86_cdq(inst); amd64_codegen_post(inst); } while (0)
#define amd64_wait_size(inst,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,0); x86_wait(inst); amd64_codegen_post(inst); } while (0)
#define amd64_fp_op_mem_size(inst,opc,mem,is_double,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),0,0,0,0); x86_fp_op_mem((inst),(opc),(mem),(is_double)); amd64_codegen_post(inst); } while (0)
#define amd64_fp_op_membase_size(inst,opc,basereg,disp,is_double,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),0,0,0,(basereg)); x86_fp_op_membase((inst),(opc),((basereg)&0x7),(disp),(is_double)); amd64_codegen_post(inst); } while (0)
#define amd64_fp_op_size(inst,opc,index,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),0,0,0,0); x86_fp_op((inst),(opc),(index)); amd64_codegen_post(inst); } while (0)
#define amd64_fp_op_reg_size(inst,opc,index,pop_stack,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),0,0,0,0); x86_fp_op_reg((inst),(opc),(index),(pop_stack)); amd64_codegen_post(inst); } while (0)
#define amd64_fp_int_op_membase_size(inst,opc,basereg,disp,is_int,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),0,0,0,(basereg)); x86_fp_int_op_membase((inst),(opc),((basereg)&0x7),(disp),(is_int)); amd64_codegen_post(inst); } while (0)
#define amd64_fstp_size(inst,index,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),0,0,0,0); x86_fstp((inst),(index)); amd64_codegen_post(inst); } while (0)
#define amd64_fcompp_size(inst,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),0,0,0,0); x86_fcompp(inst); amd64_codegen_post(inst); } while (0)
#define amd64_fucompp_size(inst,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),0,0,0,0); x86_fucompp(inst); amd64_codegen_post(inst); } while (0)
#define amd64_fnstsw_size(inst,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),0,0,0,0); x86_fnstsw(inst); amd64_codegen_post(inst); } while (0)
#define amd64_fnstcw_size(inst,mem,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),0,0,0,0); x86_fnstcw((inst),(mem)); amd64_codegen_post(inst); } while (0)
#define amd64_fnstcw_membase_size(inst,basereg,disp,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,(basereg)); x86_fnstcw_membase((inst),((basereg)&0x7),(disp)); amd64_codegen_post(inst); } while (0)
#define amd64_fldcw_size(inst,mem,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),0,0,0,0); x86_fldcw((inst),(mem)); amd64_codegen_post(inst); } while (0)
#define amd64_fldcw_membase_size(inst,basereg,disp,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),0,0,0,(basereg)); x86_fldcw_membase((inst),((basereg)&0x7),(disp)); amd64_codegen_post(inst); } while (0)
#define amd64_fchs_size(inst,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),0,0,0,0); x86_fchs(inst); amd64_codegen_post(inst); } while (0)
#define amd64_frem_size(inst,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),0,0,0,0); x86_frem(inst); amd64_codegen_post(inst); } while (0)
#define amd64_fxch_size(inst,index,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),0,0,0,0); x86_fxch((inst),(index)); amd64_codegen_post(inst); } while (0)
#define amd64_fcomi_size(inst,index,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),0,0,0,0); x86_fcomi((inst),(index)); amd64_codegen_post(inst); } while (0)
#define amd64_fcomip_size(inst,index,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),0,0,0,0); x86_fcomip((inst),(index)); amd64_codegen_post(inst); } while (0)
#define amd64_fucomi_size(inst,index,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),0,0,0,0); x86_fucomi((inst),(index)); amd64_codegen_post(inst); } while (0)
#define amd64_fucomip_size(inst,index,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),0,0,0,0); x86_fucomip((inst),(index)); amd64_codegen_post(inst); } while (0)
#define amd64_fld_size(inst,mem,is_double,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),0,0,0,0); x86_fld((inst),(mem),(is_double)); amd64_codegen_post(inst); } while (0)
//#define amd64_fld_membase_size(inst,basereg,disp,is_double,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),0,0,0,(basereg)); x86_fld_membase((inst),((basereg)&0x7),(disp),(is_double)); amd64_codegen_post(inst); } while (0)
#define amd64_fld80_mem_size(inst,mem,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),0,0,0,0); x86_fld80_mem((inst),(mem)); amd64_codegen_post(inst); } while (0)
#define amd64_fld80_membase_size(inst,basereg,disp,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,(basereg)); x86_fld80_membase((inst),((basereg)&0x7),(disp)); amd64_codegen_post(inst); } while (0)
#define amd64_fild_size(inst,mem,is_long,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),0,0,0,0); x86_fild((inst),(mem),(is_long)); amd64_codegen_post(inst); } while (0)
#define amd64_fild_membase_size(inst,basereg,disp,is_long,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),0,0,0,(basereg)); x86_fild_membase((inst),((basereg)&0x7),(disp),(is_long)); amd64_codegen_post(inst); } while (0)
#define amd64_fld_reg_size(inst,index,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),0,0,0,0); x86_fld_reg((inst),(index)); amd64_codegen_post(inst); } while (0)
#define amd64_fldz_size(inst,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),0,0,0,0); x86_fldz(inst); amd64_codegen_post(inst); } while (0)
#define amd64_fld1_size(inst,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),0,0,0,0); x86_fld1(inst); amd64_codegen_post(inst); } while (0)
#define amd64_fldpi_size(inst,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),0,0,0,0); x86_fldpi(inst); amd64_codegen_post(inst); } while (0)
#define amd64_fst_size(inst,mem,is_double,pop_stack,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),0,0,0,0); x86_fst((inst),(mem),(is_double),(pop_stack)); amd64_codegen_post(inst); } while (0)
#define amd64_fst_membase_size(inst,basereg,disp,is_double,pop_stack,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),0,0,0,(basereg)); x86_fst_membase((inst),((basereg)&0x7),(disp),(is_double),(pop_stack)); amd64_codegen_post(inst); } while (0)
#define amd64_fst80_mem_size(inst,mem,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),0,0,0,0); x86_fst80_mem((inst),(mem)); amd64_codegen_post(inst); } while (0)
#define amd64_fst80_membase_size(inst,basereg,disp,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),0,0,0,(basereg)); x86_fst80_membase((inst),((basereg)&0x7),(disp)); amd64_codegen_post(inst); } while (0)
#define amd64_fist_pop_size(inst,mem,is_long,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),0,0,0,0); x86_fist_pop((inst),(mem),(is_long)); amd64_codegen_post(inst); } while (0)
#define amd64_fist_pop_membase_size(inst,basereg,disp,is_long,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),0,0,0,(basereg)); x86_fist_pop_membase((inst),((basereg)&0x7),(disp),(is_long)); amd64_codegen_post(inst); } while (0)
#define amd64_fstsw_size(inst,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,0); x86_fstsw(inst); amd64_codegen_post(inst); } while (0)
#define amd64_fist_membase_size(inst,basereg,disp,is_int,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),0,0,0,(basereg)); x86_fist_membase((inst),((basereg)&0x7),(disp),(is_int)); amd64_codegen_post(inst); } while (0)
//#define amd64_push_reg_size(inst,reg,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,(reg)); x86_push_reg((inst),((reg)&0x7)); amd64_codegen_post(inst); } while (0)
#define amd64_push_regp_size(inst,reg,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,(reg)); x86_push_regp((inst),((reg)&0x7)); amd64_codegen_post(inst); } while (0)
#define amd64_push_mem_size(inst,mem,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,0); x86_push_mem((inst),(mem)); amd64_codegen_post(inst); } while (0)
//#define amd64_push_membase_size(inst,basereg,disp,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,(basereg)); x86_push_membase((inst),((basereg)&0x7),(disp)); amd64_codegen_post(inst); } while (0)
#define amd64_push_memindex_size(inst,basereg,disp,indexreg,shift,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,(indexreg),(basereg)); x86_push_memindex((inst),((basereg)&0x7),(disp),((indexreg)&0x7),(shift)); amd64_codegen_post(inst); } while (0)
#define amd64_push_imm_size(inst,imm,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),0,0,0,0); x86_push_imm((inst),(imm)); amd64_codegen_post(inst); } while (0)
//#define amd64_pop_reg_size(inst,reg,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,(reg)); x86_pop_reg((inst),((reg)&0x7)); amd64_codegen_post(inst); } while (0)
#define amd64_pop_mem_size(inst,mem,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,0); x86_pop_mem((inst),(mem)); amd64_codegen_post(inst); } while (0)
#define amd64_pop_membase_size(inst,basereg,disp,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,(basereg)); x86_pop_membase((inst),((basereg)&0x7),(disp)); amd64_codegen_post(inst); } while (0)
#define amd64_pushad_size(inst,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,0); x86_pushad(inst); amd64_codegen_post(inst); } while (0)
#define amd64_pushfd_size(inst,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,0); x86_pushfd(inst); amd64_codegen_post(inst); } while (0)
#define amd64_popad_size(inst,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,0); x86_popad(inst); amd64_codegen_post(inst); } while (0)
#define amd64_popfd_size(inst,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,0); x86_popfd(inst); amd64_codegen_post(inst); } while (0)
#define amd64_loop_size(inst,imm,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,0); x86_loop((inst),(imm)); amd64_codegen_post(inst); } while (0)
#define amd64_loope_size(inst,imm,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,0); x86_loope((inst),(imm)); amd64_codegen_post(inst); } while (0)
#define amd64_loopne_size(inst,imm,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,0); x86_loopne((inst),(imm)); amd64_codegen_post(inst); } while (0)
#define amd64_jump32_size(inst,imm,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,0); x86_jump32((inst),(imm)); amd64_codegen_post(inst); } while (0)
#define amd64_jump8_size(inst,imm,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,0); x86_jump8((inst),(imm)); amd64_codegen_post(inst); } while (0)
/* Defined above for Native Client, so they can be used in other macros */
#define amd64_jump_reg_size(inst,reg,size) do { amd64_emit_rex ((inst),0,0,0,(reg)); x86_jump_reg((inst),((reg)&0x7)); } while (0)
#define amd64_jump_mem_size(inst,mem,size) do { amd64_emit_rex ((inst),(size),0,0,0); x86_jump_mem((inst),(mem)); } while (0)
#define amd64_jump_disp_size(inst,disp,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),0,0,0,0); x86_jump_disp((inst),(disp)); amd64_codegen_post(inst); } while (0)
#define amd64_branch8_size(inst,cond,imm,is_signed,size) do { x86_branch8((inst),(cond),(imm),(is_signed)); } while (0)
#define amd64_branch32_size(inst,cond,imm,is_signed,size) do { x86_branch32((inst),(cond),(imm),(is_signed)); } while (0)
#define amd64_branch_size_body(inst,cond,target,is_signed,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,0); x86_branch((inst),(cond),(target),(is_signed)); amd64_codegen_post(inst); } while (0)
#define amd64_branch_size(inst,cond,target,is_signed,size) do { amd64_branch_size_body((inst),(cond),(target),(is_signed),(size)); } while (0)

#define amd64_branch_disp_size(inst,cond,disp,is_signed,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,0); x86_branch_disp((inst),(cond),(disp),(is_signed)); amd64_codegen_post(inst); } while (0)
#define amd64_set_reg_size(inst,cond,reg,is_signed,size) do { amd64_codegen_pre(inst); amd64_emit_rex((inst),1,0,0,(reg)); x86_set_reg((inst),(cond),((reg)&0x7),(is_signed)); amd64_codegen_post(inst); } while (0)
#define amd64_set_mem_size(inst,cond,mem,is_signed,size) do { amd64_codegen_pre(inst); x86_set_mem((inst),(cond),(mem),(is_signed)); amd64_codegen_post(inst); } while (0)
#define amd64_set_membase_size(inst,cond,basereg,disp,is_signed,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),0,0,0,(basereg)); x86_set_membase((inst),(cond),((basereg)&0x7),(disp),(is_signed)); amd64_codegen_post(inst); } while (0)
//#define amd64_call_reg_size(inst,reg,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,(reg)); x86_call_reg((inst),((reg)&0x7)); amd64_codegen_post(inst); } while (0)
#define amd64_call_mem_size(inst,mem,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,0); x86_call_mem((inst),(mem)); amd64_codegen_post(inst); } while (0)

#define amd64_call_imm_size(inst,disp,size) do { x86_call_imm((inst),(disp)); } while (0)
#define amd64_call_code_size(inst,target,size) do { x86_call_code((inst),(target)); } while (0)

//#define amd64_ret_size(inst,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,0); x86_ret(inst); amd64_codegen_post(inst); } while (0)
#define amd64_ret_imm_size(inst,imm,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,0); x86_ret_imm((inst),(imm)); amd64_codegen_post(inst); } while (0)
#define amd64_cmov_reg_size(inst,cond,is_signed,dreg,reg,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),(dreg),0,(reg)); x86_cmov_reg((inst),(cond),(is_signed),((dreg)&0x7),((reg)&0x7)); amd64_codegen_post(inst); } while (0)
#define amd64_cmov_mem_size(inst,cond,is_signed,reg,mem,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,(reg)); x86_cmov_mem((inst),(cond),(is_signed),((reg)&0x7),(mem)); amd64_codegen_post(inst); } while (0)
#define amd64_cmov_membase_size(inst,cond,is_signed,reg,basereg,disp,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,(basereg)); x86_cmov_membase((inst),(cond),(is_signed),((reg)&0x7),((basereg)&0x7),(disp)); amd64_codegen_post(inst); } while (0)
#define amd64_enter_size(inst,framesize) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,0); x86_enter((inst),(framesize)); amd64_codegen_post(inst); } while (0)
//#define amd64_leave_size(inst,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,0); x86_leave(inst); amd64_codegen_post(inst); } while (0)
#define amd64_sahf_size(inst,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,0); x86_sahf(inst); amd64_codegen_post(inst); } while (0)
#define amd64_fsin_size(inst,size) do { amd64_codegen_pre(inst); x86_fsin(inst); amd64_codegen_post(inst); } while (0)
#define amd64_fcos_size(inst,size) do { amd64_codegen_pre(inst); x86_fcos(inst); amd64_codegen_post(inst); } while (0)
#define amd64_fabs_size(inst,size) do { amd64_codegen_pre(inst); x86_fabs(inst); amd64_codegen_post(inst); } while (0)
#define amd64_ftst_size(inst,size) do { amd64_codegen_pre(inst); x86_ftst(inst); amd64_codegen_post(inst); } while (0)
#define amd64_fxam_size(inst,size) do { amd64_codegen_pre(inst); x86_fxam(inst); amd64_codegen_post(inst); } while (0)
#define amd64_fpatan_size(inst,size) do { amd64_codegen_pre(inst); x86_fpatan(inst); amd64_codegen_post(inst); } while (0)
#define amd64_fprem_size(inst,size) do { amd64_codegen_pre(inst); x86_fprem(inst); amd64_codegen_post(inst); } while (0)
#define amd64_fprem1_size(inst,size) do { amd64_codegen_pre(inst); x86_fprem1(inst); amd64_codegen_post(inst); } while (0)
#define amd64_frndint_size(inst,size) do { amd64_codegen_pre(inst); x86_frndint(inst); amd64_codegen_post(inst); } while (0)
#define amd64_fsqrt_size(inst,size) do { amd64_codegen_pre(inst); x86_fsqrt(inst); amd64_codegen_post(inst); } while (0)
#define amd64_fptan_size(inst,size) do { amd64_codegen_pre(inst); x86_fptan(inst); amd64_codegen_post(inst); } while (0)
//#define amd64_padding_size(inst,size) do { amd64_codegen_pre(inst); x86_padding((inst),(size)); amd64_codegen_post(inst); } while (0)
#define amd64_prolog_size(inst,frame_size,reg_mask,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,0); x86_prolog((inst),(frame_size),(reg_mask)); amd64_codegen_post(inst); } while (0)
#define amd64_epilog_size(inst,reg_mask,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,0); x86_epilog((inst),(reg_mask)); amd64_codegen_post(inst); } while (0)
#define amd64_xadd_reg_reg_size(inst,dreg,reg,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),(dreg),0,(reg)); x86_xadd_reg_reg ((inst), (dreg), (reg), (size)); amd64_codegen_post(inst); } while (0)
#define amd64_xadd_mem_reg_size(inst,mem,reg,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),0,0,(reg)); x86_xadd_mem_reg((inst),(mem),((reg)&0x7), (size)); amd64_codegen_post(inst); } while (0)
#define amd64_xadd_membase_reg_size(inst,basereg,disp,reg,size) do { amd64_codegen_pre(inst); amd64_emit_rex ((inst),(size),(reg),0,(basereg)); x86_xadd_membase_reg((inst),((basereg)&0x7),(disp),((reg)&0x7),(size)); amd64_codegen_post(inst); } while (0)




#define amd64_breakpoint(inst) amd64_breakpoint_size(inst,8)
#define amd64_cld(inst) amd64_cld_size(inst,8)
#define amd64_stosb(inst) amd64_stosb_size(inst,8)
#define amd64_stosl(inst) amd64_stosl_size(inst,8)
#define amd64_stosd(inst) amd64_stosd_size(inst,8)
#define amd64_movsb(inst) amd64_movsb_size(inst,8)
#define amd64_movsl(inst) amd64_movsl_size(inst,8)
#define amd64_movsd(inst) amd64_movsd_size(inst,8)
#define amd64_prefix(inst,p) amd64_prefix_size(inst,p,8)
#define amd64_rdtsc(inst) amd64_rdtsc_size(inst,8)
#define amd64_cmpxchg_reg_reg(inst,dreg,reg) amd64_cmpxchg_reg_reg_size(inst,dreg,reg,8)
#define amd64_cmpxchg_mem_reg(inst,mem,reg) amd64_cmpxchg_mem_reg_size(inst,mem,reg,8)
#define amd64_cmpxchg_membase_reg(inst,basereg,disp,reg) amd64_cmpxchg_membase_reg_size(inst,basereg,disp,reg,8)
#define amd64_xchg_reg_reg(inst,dreg,reg,size) amd64_xchg_reg_reg_size(inst,dreg,reg,size)
#define amd64_xchg_mem_reg(inst,mem,reg,size) amd64_xchg_mem_reg_size(inst,mem,reg,size)
#define amd64_xchg_membase_reg(inst,basereg,disp,reg,size) amd64_xchg_membase_reg_size(inst,basereg,disp,reg,size)
#define amd64_xadd_reg_reg(inst,dreg,reg,size) amd64_xadd_reg_reg_size(inst,dreg,reg,size)
#define amd64_xadd_mem_reg(inst,mem,reg,size) amd64_xadd_mem_reg_size(inst,mem,reg,size)
#define amd64_xadd_membase_reg(inst,basereg,disp,reg,size) amd64_xadd_membase_reg_size(inst,basereg,disp,reg,size)
#define amd64_inc_mem(inst,mem) amd64_inc_mem_size(inst,mem,8)
#define amd64_inc_membase(inst,basereg,disp) amd64_inc_membase_size(inst,basereg,disp,8)
#define amd64_inc_reg(inst,reg) amd64_inc_reg_size(inst,reg,8)
#define amd64_dec_mem(inst,mem) amd64_dec_mem_size(inst,mem,8)
#define amd64_dec_membase(inst,basereg,disp) amd64_dec_membase_size(inst,basereg,disp,8)
#define amd64_dec_reg(inst,reg) amd64_dec_reg_size(inst,reg,8)
#define amd64_not_mem(inst,mem) amd64_not_mem_size(inst,mem,8)
#define amd64_not_membase(inst,basereg,disp) amd64_not_membase_size(inst,basereg,disp,8)
#define amd64_not_reg(inst,reg) amd64_not_reg_size(inst,reg,8)
#define amd64_neg_mem(inst,mem) amd64_neg_mem_size(inst,mem,8)
#define amd64_neg_membase(inst,basereg,disp) amd64_neg_membase_size(inst,basereg,disp,8)
#define amd64_neg_reg(inst,reg) amd64_neg_reg_size(inst,reg,8)
#define amd64_nop(inst) amd64_nop_size(inst,8)
//#define amd64_alu_reg_imm(inst,opc,reg,imm) amd64_alu_reg_imm_size(inst,opc,reg,imm,8)
#define amd64_alu_mem_imm(inst,opc,mem,imm) amd64_alu_mem_imm_size(inst,opc,mem,imm,8)
#define amd64_alu_membase_imm(inst,opc,basereg,disp,imm) amd64_alu_membase_imm_size(inst,opc,basereg,disp,imm,8)
#define amd64_alu_mem_reg(inst,opc,mem,reg) amd64_alu_mem_reg_size(inst,opc,mem,reg,8)
#define amd64_alu_membase_reg(inst,opc,basereg,disp,reg) amd64_alu_membase_reg_size(inst,opc,basereg,disp,reg,8)
//#define amd64_alu_reg_reg(inst,opc,dreg,reg) amd64_alu_reg_reg_size(inst,opc,dreg,reg,8)
#define amd64_alu_reg8_reg8(inst,opc,dreg,reg,is_dreg_h,is_reg_h) amd64_alu_reg8_reg8_size(inst,opc,dreg,reg,is_dreg_h,is_reg_h,8)
#define amd64_alu_reg_mem(inst,opc,reg,mem) amd64_alu_reg_mem_size(inst,opc,reg,mem,8)
#define amd64_alu_reg_membase(inst,opc,reg,basereg,disp) amd64_alu_reg_membase_size(inst,opc,reg,basereg,disp,8)
//#define amd64_test_reg_imm(inst,reg,imm) amd64_test_reg_imm_size(inst,reg,imm,8)
#define amd64_test_mem_imm(inst,mem,imm) amd64_test_mem_imm_size(inst,mem,imm,8)
#define amd64_test_membase_imm(inst,basereg,disp,imm) amd64_test_membase_imm_size(inst,basereg,disp,imm,8)
#define amd64_test_reg_reg(inst,dreg,reg) amd64_test_reg_reg_size(inst,dreg,reg,8)
#define amd64_test_mem_reg(inst,mem,reg) amd64_test_mem_reg_size(inst,mem,reg,8)
#define amd64_test_membase_reg(inst,basereg,disp,reg) amd64_test_membase_reg_size(inst,basereg,disp,reg,8)
#define amd64_shift_reg_imm(inst,opc,reg,imm) amd64_shift_reg_imm_size(inst,opc,reg,imm,8)
#define amd64_shift_mem_imm(inst,opc,mem,imm) amd64_shift_mem_imm_size(inst,opc,mem,imm,8)
#define amd64_shift_membase_imm(inst,opc,basereg,disp,imm) amd64_shift_membase_imm_size(inst,opc,basereg,disp,imm,8)
#define amd64_shift_reg(inst,opc,reg) amd64_shift_reg_size(inst,opc,reg,8)
#define amd64_shift_mem(inst,opc,mem) amd64_shift_mem_size(inst,opc,mem,8)
#define amd64_shift_membase(inst,opc,basereg,disp) amd64_shift_membase_size(inst,opc,basereg,disp,8)
#define amd64_shrd_reg(inst,dreg,reg) amd64_shrd_reg_size(inst,dreg,reg,8)
#define amd64_shrd_reg_imm(inst,dreg,reg,shamt) amd64_shrd_reg_imm_size(inst,dreg,reg,shamt,8)
#define amd64_shld_reg(inst,dreg,reg) amd64_shld_reg_size(inst,dreg,reg,8)
#define amd64_shld_reg_imm(inst,dreg,reg,shamt) amd64_shld_reg_imm_size(inst,dreg,reg,shamt,8)
#define amd64_mul_reg(inst,reg,is_signed) amd64_mul_reg_size(inst,reg,is_signed,8)
#define amd64_mul_mem(inst,mem,is_signed) amd64_mul_mem_size(inst,mem,is_signed,8)
#define amd64_mul_membase(inst,basereg,disp,is_signed) amd64_mul_membase_size(inst,basereg,disp,is_signed,8)
#define amd64_imul_reg_reg(inst,dreg,reg) amd64_imul_reg_reg_size(inst,dreg,reg,8)
#define amd64_imul_reg_mem(inst,reg,mem) amd64_imul_reg_mem_size(inst,reg,mem,8)
#define amd64_imul_reg_membase(inst,reg,basereg,disp) amd64_imul_reg_membase_size(inst,reg,basereg,disp,8)
#define amd64_imul_reg_reg_imm(inst,dreg,reg,imm) amd64_imul_reg_reg_imm_size(inst,dreg,reg,imm,8)
#define amd64_imul_reg_mem_imm(inst,reg,mem,imm) amd64_imul_reg_mem_imm_size(inst,reg,mem,imm,8)
#define amd64_imul_reg_membase_imm(inst,reg,basereg,disp,imm) amd64_imul_reg_membase_imm_size(inst,reg,basereg,disp,imm,8)
#define amd64_div_reg(inst,reg,is_signed) amd64_div_reg_size(inst,reg,is_signed,8)
#define amd64_div_mem(inst,mem,is_signed) amd64_div_mem_size(inst,mem,is_signed,8)
#define amd64_div_membase(inst,basereg,disp,is_signed) amd64_div_membase_size(inst,basereg,disp,is_signed,8)
//#define amd64_mov_mem_reg(inst,mem,reg,size) amd64_mov_mem_reg_size(inst,mem,reg,size)
//#define amd64_mov_regp_reg(inst,regp,reg,size) amd64_mov_regp_reg_size(inst,regp,reg,size)
//#define amd64_mov_membase_reg(inst,basereg,disp,reg,size) amd64_mov_membase_reg_size(inst,basereg,disp,reg,size)
#define amd64_mov_memindex_reg(inst,basereg,disp,indexreg,shift,reg,size) amd64_mov_memindex_reg_size(inst,basereg,disp,indexreg,shift,reg,size)
//#define amd64_mov_reg_reg(inst,dreg,reg,size) amd64_mov_reg_reg_size(inst,dreg,reg,size)
//#define amd64_mov_reg_mem(inst,reg,mem,size) amd64_mov_reg_mem_size(inst,reg,mem,size)
//#define amd64_mov_reg_membase(inst,reg,basereg,disp,size) amd64_mov_reg_membase_size(inst,reg,basereg,disp,size)
#define amd64_mov_reg_memindex(inst,reg,basereg,disp,indexreg,shift,size) amd64_mov_reg_memindex_size(inst,reg,basereg,disp,indexreg,shift,size)
#define amd64_clear_reg(inst,reg) amd64_clear_reg_size(inst,reg,8)
//#define amd64_mov_reg_imm(inst,reg,imm) amd64_mov_reg_imm_size(inst,reg,imm,8)
#define amd64_mov_mem_imm(inst,mem,imm,size) amd64_mov_mem_imm_size(inst,mem,imm,size)
//#define amd64_mov_membase_imm(inst,basereg,disp,imm,size) amd64_mov_membase_imm_size(inst,basereg,disp,imm,size)
#define amd64_mov_memindex_imm(inst,basereg,disp,indexreg,shift,imm,size) amd64_mov_memindex_imm_size(inst,basereg,disp,indexreg,shift,imm,size)
#define amd64_lea_mem(inst,reg,mem) amd64_lea_mem_size(inst,reg,mem,8)
//#define amd64_lea_membase(inst,reg,basereg,disp) amd64_lea_membase_size(inst,reg,basereg,disp,8)
#define amd64_lea_memindex(inst,reg,basereg,disp,indexreg,shift) amd64_lea_memindex_size(inst,reg,basereg,disp,indexreg,shift,8)
#define amd64_widen_reg(inst,dreg,reg,is_signed,is_half) amd64_widen_reg_size(inst,dreg,reg,is_signed,is_half,8)
#define amd64_widen_mem(inst,dreg,mem,is_signed,is_half) amd64_widen_mem_size(inst,dreg,mem,is_signed,is_half,8)
#define amd64_widen_membase(inst,dreg,basereg,disp,is_signed,is_half) amd64_widen_membase_size(inst,dreg,basereg,disp,is_signed,is_half,8)
#define amd64_widen_memindex(inst,dreg,basereg,disp,indexreg,shift,is_signed,is_half) amd64_widen_memindex_size(inst,dreg,basereg,disp,indexreg,shift,is_signed,is_half,8)
#define amd64_cdq(inst) amd64_cdq_size(inst,8)
#define amd64_wait(inst) amd64_wait_size(inst,8)
#define amd64_fp_op_mem(inst,opc,mem,is_double) amd64_fp_op_mem_size(inst,opc,mem,is_double,8)
#define amd64_fp_op_membase(inst,opc,basereg,disp,is_double) amd64_fp_op_membase_size(inst,opc,basereg,disp,is_double,8)
#define amd64_fp_op(inst,opc,index) amd64_fp_op_size(inst,opc,index,8)
#define amd64_fp_op_reg(inst,opc,index,pop_stack) amd64_fp_op_reg_size(inst,opc,index,pop_stack,8)
#define amd64_fp_int_op_membase(inst,opc,basereg,disp,is_int) amd64_fp_int_op_membase_size(inst,opc,basereg,disp,is_int,8)
#define amd64_fstp(inst,index) amd64_fstp_size(inst,index,8)
#define amd64_fcompp(inst) amd64_fcompp_size(inst,8)
#define amd64_fucompp(inst) amd64_fucompp_size(inst,8)
#define amd64_fnstsw(inst) amd64_fnstsw_size(inst,8)
#define amd64_fnstcw(inst,mem) amd64_fnstcw_size(inst,mem,8)
#define amd64_fnstcw_membase(inst,basereg,disp) amd64_fnstcw_membase_size(inst,basereg,disp,8)
#define amd64_fldcw(inst,mem) amd64_fldcw_size(inst,mem,8)
#define amd64_fldcw_membase(inst,basereg,disp) amd64_fldcw_membase_size(inst,basereg,disp,8)
#define amd64_fchs(inst) amd64_fchs_size(inst,8)
#define amd64_frem(inst) amd64_frem_size(inst,8)
#define amd64_fxch(inst,index) amd64_fxch_size(inst,index,8)
#define amd64_fcomi(inst,index) amd64_fcomi_size(inst,index,8)
#define amd64_fcomip(inst,index) amd64_fcomip_size(inst,index,8)
#define amd64_fucomi(inst,index) amd64_fucomi_size(inst,index,8)
#define amd64_fucomip(inst,index) amd64_fucomip_size(inst,index,8)
#define amd64_fld(inst,mem,is_double) amd64_fld_size(inst,mem,is_double,8)
#define amd64_fld_membase(inst,basereg,disp,is_double)  amd64_fld_membase_size(inst,basereg,disp,is_double,8) 
#define amd64_fld80_mem(inst,mem) amd64_fld80_mem_size(inst,mem,8)
#define amd64_fld80_membase(inst,basereg,disp) amd64_fld80_membase_size(inst,basereg,disp,8)
#define amd64_fild(inst,mem,is_long) amd64_fild_size(inst,mem,is_long,8)
#define amd64_fild_membase(inst,basereg,disp,is_long) amd64_fild_membase_size(inst,basereg,disp,is_long,8)
#define amd64_fld_reg(inst,index) amd64_fld_reg_size(inst,index,8)
#define amd64_fldz(inst) amd64_fldz_size(inst,8)
#define amd64_fld1(inst) amd64_fld1_size(inst,8)
#define amd64_fldpi(inst) amd64_fldpi_size(inst,8)
#define amd64_fst(inst,mem,is_double,pop_stack) amd64_fst_size(inst,mem,is_double,pop_stack,8)
#define amd64_fst_membase(inst,basereg,disp,is_double,pop_stack) amd64_fst_membase_size(inst,basereg,disp,is_double,pop_stack,8)
#define amd64_fst80_mem(inst,mem) amd64_fst80_mem_size(inst,mem,8)
#define amd64_fst80_membase(inst,basereg,disp) amd64_fst80_membase_size(inst,basereg,disp,8)
#define amd64_fist_pop(inst,mem,is_long) amd64_fist_pop_size(inst,mem,is_long,8)
#define amd64_fist_pop_membase(inst,basereg,disp,is_long) amd64_fist_pop_membase_size(inst,basereg,disp,is_long,8)
#define amd64_fstsw(inst) amd64_fstsw_size(inst,8)
#define amd64_fist_membase(inst,basereg,disp,is_int) amd64_fist_membase_size(inst,basereg,disp,is_int,8)
//#define amd64_push_reg(inst,reg) amd64_push_reg_size(inst,reg,8)
#define amd64_push_regp(inst,reg) amd64_push_regp_size(inst,reg,8)
#define amd64_push_mem(inst,mem) amd64_push_mem_size(inst,mem,8)
//#define amd64_push_membase(inst,basereg,disp) amd64_push_membase_size(inst,basereg,disp,8)
#define amd64_push_memindex(inst,basereg,disp,indexreg,shift) amd64_push_memindex_size(inst,basereg,disp,indexreg,shift,8)
#define amd64_push_imm(inst,imm) amd64_push_imm_size(inst,imm,8)
//#define amd64_pop_reg(inst,reg) amd64_pop_reg_size(inst,reg,8)
#define amd64_pop_mem(inst,mem) amd64_pop_mem_size(inst,mem,8)
#define amd64_pop_membase(inst,basereg,disp) amd64_pop_membase_size(inst,basereg,disp,8)
#define amd64_pushad(inst) amd64_pushad_size(inst,8)
#define amd64_pushfd(inst) amd64_pushfd_size(inst,8)
#define amd64_popad(inst) amd64_popad_size(inst,8)
#define amd64_popfd(inst) amd64_popfd_size(inst,8)
#define amd64_loop(inst,imm) amd64_loop_size(inst,imm,8)
#define amd64_loope(inst,imm) amd64_loope_size(inst,imm,8)
#define amd64_loopne(inst,imm) amd64_loopne_size(inst,imm,8)
#define amd64_jump32(inst,imm) amd64_jump32_size(inst,imm,8)
#define amd64_jump8(inst,imm) amd64_jump8_size(inst,imm,8)
#define amd64_jump_reg(inst,reg) amd64_jump_reg_size(inst,reg,8)
#define amd64_jump_mem(inst,mem) amd64_jump_mem_size(inst,mem,8)
#define amd64_jump_membase(inst,basereg,disp) amd64_jump_membase_size(inst,basereg,disp,8)
#define amd64_jump_code(inst,target) amd64_jump_code_size(inst,target,8)
#define amd64_jump_disp(inst,disp) amd64_jump_disp_size(inst,disp,8)
#define amd64_branch8(inst,cond,imm,is_signed) amd64_branch8_size(inst,cond,imm,is_signed,8)
#define amd64_branch32(inst,cond,imm,is_signed) amd64_branch32_size(inst,cond,imm,is_signed,8)
#define amd64_branch(inst,cond,target,is_signed) amd64_branch_size(inst,cond,target,is_signed,8)
#define amd64_branch_disp(inst,cond,disp,is_signed) amd64_branch_disp_size(inst,cond,disp,is_signed,8)
#define amd64_set_reg(inst,cond,reg,is_signed) amd64_set_reg_size(inst,cond,reg,is_signed,8)
#define amd64_set_mem(inst,cond,mem,is_signed) amd64_set_mem_size(inst,cond,mem,is_signed,8)
#define amd64_set_membase(inst,cond,basereg,disp,is_signed) amd64_set_membase_size(inst,cond,basereg,disp,is_signed,8)
#define amd64_call_imm(inst,disp) amd64_call_imm_size(inst,disp,8)
//#define amd64_call_reg(inst,reg) amd64_call_reg_size(inst,reg,8)
#define amd64_call_mem(inst,mem) amd64_call_mem_size(inst,mem,8)
#define amd64_call_membase(inst,basereg,disp) amd64_call_membase_size(inst,basereg,disp,8)
#define amd64_call_code(inst,target) amd64_call_code_size(inst,target,8)
//#define amd64_ret(inst) amd64_ret_size(inst,8)
#define amd64_ret_imm(inst,imm) amd64_ret_imm_size(inst,imm,8)
#define amd64_cmov_reg(inst,cond,is_signed,dreg,reg) amd64_cmov_reg_size(inst,cond,is_signed,dreg,reg,8)
#define amd64_cmov_mem(inst,cond,is_signed,reg,mem) amd64_cmov_mem_size(inst,cond,is_signed,reg,mem,8)
#define amd64_cmov_membase(inst,cond,is_signed,reg,basereg,disp) amd64_cmov_membase_size(inst,cond,is_signed,reg,basereg,disp,8)
#define amd64_enter(inst,framesize) amd64_enter_size(inst,framesize)
//#define amd64_leave(inst) amd64_leave_size(inst,8)
#define amd64_sahf(inst) amd64_sahf_size(inst,8)
#define amd64_fsin(inst) amd64_fsin_size(inst,8)
#define amd64_fcos(inst) amd64_fcos_size(inst,8)
#define amd64_fabs(inst) amd64_fabs_size(inst,8)
#define amd64_ftst(inst) amd64_ftst_size(inst,8)
#define amd64_fxam(inst) amd64_fxam_size(inst,8)
#define amd64_fpatan(inst) amd64_fpatan_size(inst,8)
#define amd64_fprem(inst) amd64_fprem_size(inst,8)
#define amd64_fprem1(inst) amd64_fprem1_size(inst,8)
#define amd64_frndint(inst) amd64_frndint_size(inst,8)
#define amd64_fsqrt(inst) amd64_fsqrt_size(inst,8)
#define amd64_fptan(inst) amd64_fptan_size(inst,8)
#define amd64_padding(inst,size) amd64_padding_size(inst,size)
#define amd64_prolog(inst,frame,reg_mask) amd64_prolog_size(inst,frame,reg_mask,8)
#define amd64_epilog(inst,reg_mask) amd64_epilog_size(inst,reg_mask,8)

#endif // AMD64_H
