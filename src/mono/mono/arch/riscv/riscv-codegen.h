/*
 * Licensed to the .NET Foundation under one or more agreements.
 * The .NET Foundation licenses this file to you under the MIT license.
 */

#ifndef __MONO_RISCV_CODEGEN_H__
#define __MONO_RISCV_CODEGEN_H__

#include "config.h"
#include <glib.h>

#ifdef MONO_RISCV_CODEGEN_TEST

#include <stdio.h>
#include <stdlib.h>

/*
 * Avoid having to link with eglib in the codegen test program so that it can
 * be built with a native toolchain even if we're building Mono with a cross
 * toolchain.
 */

#undef g_assert
#define g_assert(expr) \
	do { \
		if (G_UNLIKELY (!(expr))) { \
			fprintf (stderr, "* Assertion at %s:%d, condition `%s' not met\n", __FILE__, __LINE__, #expr); \
			abort (); \
		} \
	} while (0)

#endif

enum {
	RISCV_X0   = 0,
	RISCV_X1   = 1,
	RISCV_X2   = 2,
	RISCV_X3   = 3,
	RISCV_X4   = 4,
	RISCV_X5   = 5,
	RISCV_X6   = 6,
	RISCV_X7   = 7,
	RISCV_X8   = 8,
	RISCV_X9   = 9,
	RISCV_X10  = 10,
	RISCV_X11  = 11,
	RISCV_X12  = 12,
	RISCV_X13  = 13,
	RISCV_X14  = 14,
	RISCV_X15  = 15,
	RISCV_X16  = 16,
	RISCV_X17  = 17,
	RISCV_X18  = 18,
	RISCV_X19  = 19,
	RISCV_X20  = 20,
	RISCV_X21  = 21,
	RISCV_X22  = 22,
	RISCV_X23  = 23,
	RISCV_X24  = 24,
	RISCV_X25  = 25,
	RISCV_X26  = 26,
	RISCV_X27  = 27,
	RISCV_X28  = 28,
	RISCV_X29  = 29,
	RISCV_X30  = 30,
	RISCV_X31  = 31,

	RISCV_ZERO = RISCV_X0,

	// Argument and return registers.

	RISCV_A0   = RISCV_X10,
	RISCV_A1   = RISCV_X11,
	RISCV_A2   = RISCV_X12,
	RISCV_A3   = RISCV_X13,
	RISCV_A4   = RISCV_X14,
	RISCV_A5   = RISCV_X15,
	RISCV_A6   = RISCV_X16,
	RISCV_A7   = RISCV_X17,

	// Callee-saved registers.

	RISCV_S0   = RISCV_X8,
	RISCV_S1   = RISCV_X9,
	RISCV_S2   = RISCV_X18,
	RISCV_S3   = RISCV_X19,
	RISCV_S4   = RISCV_X20,
	RISCV_S5   = RISCV_X21,
	RISCV_S6   = RISCV_X22,
	RISCV_S7   = RISCV_X23,
	RISCV_S8   = RISCV_X24,
	RISCV_S9   = RISCV_X25,
	RISCV_S10  = RISCV_X26,
	RISCV_S11  = RISCV_X27,

	// Temporary registers.

	RISCV_T0   = RISCV_X5,
	RISCV_T1   = RISCV_X6,
	RISCV_T2   = RISCV_X7,
	RISCV_T3   = RISCV_X28,
	RISCV_T4   = RISCV_X29,
	RISCV_T5   = RISCV_X30,
	RISCV_T6   = RISCV_X31,

	// Call stack registers.

	RISCV_SP   = RISCV_X2, // Stack pointer.
	RISCV_RA   = RISCV_X1, // Return address (AKA link register).
	RISCV_FP   = RISCV_S0, // Frame pointer (AKA base pointer).

	// ABI implementation registers.

	RISCV_GP   = RISCV_X3,
	RISCV_TP   = RISCV_X4,
};

#define RISCV_N_GREGS  (32)
#define RISCV_N_GAREGS (8)
#define RISCV_N_GSREGS (12)
#define RISCV_N_GTREGS (7)

enum {
	RISCV_F0   = 0,
	RISCV_F1   = 1,
	RISCV_F2   = 2,
	RISCV_F3   = 3,
	RISCV_F4   = 4,
	RISCV_F5   = 5,
	RISCV_F6   = 6,
	RISCV_F7   = 7,
	RISCV_F8   = 8,
	RISCV_F9   = 9,
	RISCV_F10  = 10,
	RISCV_F11  = 11,
	RISCV_F12  = 12,
	RISCV_F13  = 13,
	RISCV_F14  = 14,
	RISCV_F15  = 15,
	RISCV_F16  = 16,
	RISCV_F17  = 17,
	RISCV_F18  = 18,
	RISCV_F19  = 19,
	RISCV_F20  = 20,
	RISCV_F21  = 21,
	RISCV_F22  = 22,
	RISCV_F23  = 23,
	RISCV_F24  = 24,
	RISCV_F25  = 25,
	RISCV_F26  = 26,
	RISCV_F27  = 27,
	RISCV_F28  = 28,
	RISCV_F29  = 29,
	RISCV_F30  = 30,
	RISCV_F31  = 31,

	// Argument and return registers.

	RISCV_FA0  = RISCV_F10,
	RISCV_FA1  = RISCV_F11,
	RISCV_FA2  = RISCV_F12,
	RISCV_FA3  = RISCV_F13,
	RISCV_FA4  = RISCV_F14,
	RISCV_FA5  = RISCV_F15,
	RISCV_FA6  = RISCV_F16,
	RISCV_FA7  = RISCV_F17,

	// Callee-saved registers.

	RISCV_FS0  = RISCV_F8,
	RISCV_FS1  = RISCV_F9,
	RISCV_FS2  = RISCV_F18,
	RISCV_FS3  = RISCV_F19,
	RISCV_FS4  = RISCV_F20,
	RISCV_FS5  = RISCV_F21,
	RISCV_FS6  = RISCV_F22,
	RISCV_FS7  = RISCV_F23,
	RISCV_FS8  = RISCV_F24,
	RISCV_FS9  = RISCV_F25,
	RISCV_FS10 = RISCV_F26,
	RISCV_FS11 = RISCV_F27,

	// Temporary registers.

	RISCV_FT0  = RISCV_F0,
	RISCV_FT1  = RISCV_F1,
	RISCV_FT2  = RISCV_F2,
	RISCV_FT3  = RISCV_F3,
	RISCV_FT4  = RISCV_F4,
	RISCV_FT5  = RISCV_F5,
	RISCV_FT6  = RISCV_F6,
	RISCV_FT7  = RISCV_F7,
	RISCV_FT8  = RISCV_F28,
	RISCV_FT9  = RISCV_F29,
	RISCV_FT10 = RISCV_F30,
	RISCV_FT11 = RISCV_F31,
};

#define RISCV_N_FREGS  (32)
#define RISCV_N_FAREGS (8)
#define RISCV_N_FSREGS (12)
#define RISCV_N_FTREGS (12)

enum {
	// Floating point.

	RISCV_CSR_FFLAGS   = 0x001, // Accrued exceptions.
	RISCV_CSR_FRM      = 0x002, // Rounding mode.
	RISCV_CSR_FCSR     = 0x003, // Combination of FFLAGS and FRM.

	// Counters and timers.

	RISCV_CSR_CYCLE    = 0xc00, // Cycle counter.
	RISCV_CSR_TIME     = 0xc01, // Wall clock time.
	RISCV_CSR_INSTRET  = 0xc02, // Instruction counter.

#ifdef TARGET_RISCV32
	RISCV_CSR_CYCLEH   = 0xc80, // Upper 32 bits of CYCLE.
	RISCV_CSR_TIMEH    = 0xc81, // Upper 32 bits of TIME.
	RISCV_CSR_INSTRETH = 0xc82, // Upper 32 bits of INSTRET.
#endif
};

enum {
	RISCV_FENCE_NONE = 0b0000,

	RISCV_FENCE_W    = 0b0001, // Memory writes.
	RISCV_FENCE_R    = 0b0010, // Memory reads.
	RISCV_FENCE_O    = 0b0100, // Device outputs.
	RISCV_FENCE_I    = 0b1000, // Device inputs.

	RISCV_FENCE_MEM  = RISCV_FENCE_W | RISCV_FENCE_R,
	RISCV_FENCE_DEV  = RISCV_FENCE_O | RISCV_FENCE_I,
	RISCV_FENCE_ALL  = RISCV_FENCE_DEV | RISCV_FENCE_MEM,
};

enum {
	RISCV_ORDER_NONE = 0b00,

	RISCV_ORDER_RL   = 0b01, // Release semantics.
	RISCV_ORDER_AQ   = 0b10, // Acquire semantics.

	RISCV_ORDER_ALL  = RISCV_ORDER_RL | RISCV_ORDER_AQ,
};

enum {
	RISCV_ROUND_NE = 0b000, // Round to nearest (ties to even).
	RISCV_ROUND_TZ = 0b001, // Round towards zero.
	RISCV_ROUND_DN = 0b010, // Round down (towards negative infinity).
	RISCV_ROUND_UP = 0b011, // Round up (towards positive infinity).
	RISCV_ROUND_MM = 0b100, // Round to nearest (ties to max magnitude).
	RISCV_ROUND_DY = 0b111, // Use current rounding mode in the FRM CSR.
};

#define _riscv_emit(p, insn) \
	do { \
		*(guint32 *) (p) = (insn); \
		(p) += sizeof (guint32); \
	} while (0)

#define RISCV_BITS(value, start, count) (((value) >> (start)) & ((1 << (count)) - 1))
#define RISCV_SIGN(value) (-(((value) >> (sizeof (guint32) * 8 - 1)) & 1))

// Encode an imemdiate for use in an instruction.

#define RISCV_ENCODE_I_IMM(imm) \
	(RISCV_BITS ((imm), 0, 12) << 20)
#define RISCV_ENCODE_S_IMM(imm) \
	((RISCV_BITS ((imm), 0, 5) << 7) | (RISCV_BITS ((imm), 5, 7) << 25))
#define RISCV_ENCODE_B_IMM(imm) \
	((RISCV_BITS ((imm), 11, 1) << 7) | (RISCV_BITS ((imm), 1, 4) << 8) | \
	 (RISCV_BITS ((imm), 5, 6) << 25) | (RISCV_BITS ((imm), 12, 1) << 31))
#define RISCV_ENCODE_U_IMM(imm) \
	(RISCV_BITS ((imm), 0, 20) << 12)
#define RISCV_ENCODE_J_IMM(imm) \
	((RISCV_BITS ((imm), 1, 10) << 21) | (RISCV_BITS ((imm), 11, 1) << 20) | \
	 (RISCV_BITS ((imm), 12, 8) << 12) | (RISCV_BITS ((imm), 20, 1) << 31))

// Decode an immediate from an instruction.

#define RISCV_DECODE_I_IMM(ins) \
	((RISCV_BITS ((ins), 20, 12) << 0) | (RISCV_SIGN ((ins)) << 12))
#define RISCV_DECODE_S_IMM(ins) \
	((RISCV_BITS ((ins), 7, 5) << 0) | (RISCV_BITS ((ins), 25, 7) << 5) | \
	 (RISCV_SIGN ((ins)) << 12))
#define RISCV_DECODE_B_IMM(ins) \
	((RISCV_BITS ((ins), 8, 4) << 1) | (RISCV_BITS ((ins), 25, 6) << 5) | \
	 (RISCV_BITS ((ins), 7, 1) << 11) | (RISCV_SIGN((ins)) << 12))
#define RISCV_DECODE_U_IMM(ins) \
	(RISCV_BITS ((ins), 12, 20) << 0)
#define RISCV_DECODE_J_IMM(ins) \
	((RISCV_BITS ((ins), 21, 10) << 1) | (RISCV_BITS ((ins), 20, 1) << 11) | \
	 (RISCV_BITS ((ins), 12, 8) << 12) | (RISCV_SIGN ((ins)) << 20))

// Check a value for validity as an immediate.

#define RISCV_VALID_I_IMM(value) \
	(RISCV_DECODE_I_IMM (RISCV_ENCODE_I_IMM ((value))) == (value))
#define RISCV_VALID_S_IMM(value) \
	(RISCV_DECODE_S_IMM (RISCV_ENCODE_S_IMM ((value))) == (value))
#define RISCV_VALID_B_IMM(value) \
	(RISCV_DECODE_B_IMM (RISCV_ENCODE_B_IMM ((value))) == (value))
#define RISCV_VALID_U_IMM(value) \
	(RISCV_DECODE_U_IMM (RISCV_ENCODE_U_IMM ((value))) == (value))
#define RISCV_VALID_J_IMM(value) \
	(RISCV_DECODE_J_IMM (RISCV_ENCODE_J_IMM ((value))) == (value))

// Check various values for validity in an instruction.

#define RISCV_VALID_REG(value) \
	(RISCV_BITS ((value), 0, 5) == (value))
#define RISCV_VALID_CSR(value) \
	(RISCV_BITS ((value), 0, 12) == (value))
#define RISCV_VALID_IS_AMOUNT(value) \
	(RISCV_BITS ((value), 0, 5) == (value))
#define RISCV_VALID_LS_AMOUNT(value) \
	(RISCV_BITS ((value), 0, 6) == (value))
#define RISCV_VALID_FENCE(value) \
	(RISCV_BITS ((value), 0, 4) == (value))
#define RISCV_VALID_ORDERING(value) \
	(RISCV_BITS ((value), 0, 2) == (value))

/*
 * The R-type encoding is used for a variety of instructions that operate on
 * registers only, such as most integer instructions, atomic instructions, and
 * some floating point instructions.
 *
 * [0....6] opcode
 * [7...11] rd
 * [12..14] funct3
 * [15..19] rs1
 * [20..24] rs2
 * [25..31] funct7
 */

#define _riscv_r_op(p, opcode, funct3, funct7, rd, rs1, rs2) \
	do { \
		g_assert (RISCV_VALID_REG ((rd))); \
		g_assert (RISCV_VALID_REG ((rs1))); \
		g_assert (RISCV_VALID_REG ((rs2))); \
		_riscv_emit ((p), ((opcode) << 0) | \
		                  ((rd) << 7) | \
		                  ((funct3) << 12) | \
		                  ((rs1) << 15) | \
		                  ((rs2) << 20) | \
		                  ((funct7) << 25)); \
	} while (0)

/*
 * The R4-type encoding is used for floating point fused multiply-add
 * instructions.
 *
 * [0....6] opcode
 * [7...11] rd
 * [12..14] funct3
 * [15..19] rs1
 * [20..24] rs2
 * [25..26] funct2
 * [27..31] rs3
 */

#define _riscv_r4_op(p, opcode, funct3, funct2, rd, rs1, rs2, rs3) \
	do { \
		g_assert (RISCV_VALID_REG ((rd))); \
		g_assert (RISCV_VALID_REG ((rs1))); \
		g_assert (RISCV_VALID_REG ((rs2))); \
		g_assert (RISCV_VALID_REG ((rs3))); \
		_riscv_emit ((p), ((opcode) << 0) | \
		                  ((rd) << 7) | \
		                  ((funct3) << 12) | \
		                  ((rs1) << 15) | \
		                  ((rs2) << 20) | \
		                  ((funct2) << 25) | \
		                  ((rs3) << 27)); \
	} while (0)

/*
 * The I-type encoding is used for a variety of instructions, such as JALR,
 * loads, and most integer instructions that operate on immediates.
 *
 * [0....6] opcode
 * [7...11] rd
 * [12..14] funct3
 * [15..19] rs1
 * [20..31] imm[0..11]
 */

#define _riscv_i_op(p, opcode, funct3, rd, rs1, imm) \
	do { \
		g_assert (RISCV_VALID_REG ((rd))); \
		g_assert (RISCV_VALID_REG ((rs1))); \
		g_assert (RISCV_VALID_I_IMM ((gint32) (gssize) (imm))); \
		_riscv_emit ((p), ((opcode) << 0) | \
		                  ((rd) << 7) | \
		                  ((funct3) << 12) | \
		                  ((rs1) << 15) | \
		                  (RISCV_ENCODE_I_IMM ((gint32) (gssize) (imm)))); \
	} while (0)

/*
 * This is a specialization of the I-type encoding used for shifts by immediate
 * values. The shift amount and right shift type are encoded into separate
 * parts of the imm field.
 *
 * [0....6] opcode
 * [7...11] rd
 * [12..14] funct3
 * [15..19] rs1
 * [20..24] shamt
 * [25..31] rstype
 */

#define _riscv_is_op(p, opcode, funct3, rstype, rd, rs1, shamt) \
	do { \
		g_assert (RISCV_VALID_REG ((rd))); \
		g_assert (RISCV_VALID_REG ((rs1))); \
		g_assert (RISCV_VALID_IS_AMOUNT ((shamt))); \
		_riscv_emit ((p), ((opcode) << 0) | \
		                  ((rd) << 7) | \
		                  ((funct3) << 12) | \
		                  ((rs1) << 15) | \
		                  (RISCV_BITS ((shamt), 0, 5) << 20) | \
		                  ((rstype) << 25)); \
	} while (0)

/*
 * A further specialization of the I-type encoding used for shifts by immediate
 * values in RV64I. The shift amount field is larger.
 *
 * [0....6] opcode
 * [7...11] rd
 * [12..14] funct3
 * [15..19] rs1
 * [20..25] shamt
 * [26..31] rstype
 */

#define _riscv_ls_op(p, opcode, funct3, rstype, rd, rs1, shamt) \
	do { \
		g_assert (RISCV_VALID_REG ((rd))); \
		g_assert (RISCV_VALID_REG ((rs1))); \
		g_assert (RISCV_VALID_LS_AMOUNT ((shamt))); \
		_riscv_emit ((p), ((opcode) << 0) | \
		                  ((rd) << 7) | \
		                  ((funct3) << 12) | \
		                  ((rs1) << 15) | \
		                  (RISCV_BITS ((shamt), 0, 6) << 20) | \
		                  ((rstype) << 26)); \
	} while (0)

/*
 * This is a specialization of the I-type encoding used for accessing control
 * and status registers.
 *
 * [0....6] opcode
 * [7...11] rd
 * [12..14] funct3
 * [15..19] rs1/zimm
 * [20..31] csr
 */

#define _riscv_ic_op(p, opcode, funct3, rd, csr, rs1) \
	do { \
		g_assert (RISCV_VALID_REG ((rd))); \
		g_assert (RISCV_VALID_REG ((rs1))); \
		g_assert (RISCV_VALID_CSR ((csr))); \
		_riscv_emit ((p), ((opcode) << 0) | \
		                  ((rd) << 7) | \
		                  ((funct3) << 12) | \
		                  ((rs1) << 15) | \
		                  (RISCV_BITS ((csr), 0, 12) << 20)); \
	} while (0)

/*
 * The S-type encoding is used for stores with signed offsets.
 *
 * [0....6] opcode
 * [7...11] imm[0..4]
 * [12..14] funct3
 * [15..19] rs1
 * [20..24] rs2
 * [25..31] imm[5..11]
 */

#define _riscv_s_op(p, opcode, funct3, rs2, rs1, imm) \
	do { \
		g_assert (RISCV_VALID_REG ((rs1))); \
		g_assert (RISCV_VALID_REG ((rs2))); \
		g_assert (RISCV_VALID_S_IMM ((gint32) (gssize) (imm))); \
		_riscv_emit ((p), ((opcode) << 0) | \
		                  ((funct3) << 12) | \
		                  ((rs1) << 15) | \
		                  ((rs2) << 20) | \
		                  (RISCV_ENCODE_S_IMM ((gint32) (gssize) (imm)))); \
	} while (0)

/*
 * The B-type encoding is used for conditional branches with signed offsets.
 *
 * [0....6] opcode
 * [7....7] imm[11]
 * [8...11] imm[1..4]
 * [12..14] funct3
 * [15..19] rs1
 * [20..24] rs2
 * [25..30] imm[5..10]
 * [31..31] imm[12]
 */

#define _riscv_b_op(p, opcode, funct3, rs1, rs2, imm) \
	do { \
		g_assert (RISCV_VALID_REG ((rs1))); \
		g_assert (RISCV_VALID_REG ((rs2))); \
		g_assert (RISCV_VALID_B_IMM ((gint32) (gssize) (imm))); \
		_riscv_emit ((p), ((opcode) << 0) | \
		                  ((funct3) << 12) | \
		                  ((rs1) << 15) | \
		                  ((rs2) << 20) | \
		                  (RISCV_ENCODE_B_IMM ((gint32) (gssize) (imm)))); \
	} while (0)

/*
 * The U-type encoding is used for LUI and AUIPC only, i.e. for instructions
 * that create 32-bit values from 20-bit immediates.
 *
 * [0....6] opcode
 * [7...11] rd
 * [12..31] imm[12..31]
 */

#define _riscv_u_op(p, opcode, rd, imm) \
	do { \
		g_assert (RISCV_VALID_REG ((rd))); \
		g_assert (RISCV_VALID_U_IMM ((guint32) (gsize) (imm))); \
		_riscv_emit ((p), ((opcode) << 0) | \
		                  ((rd) << 7) | \
		                  (RISCV_ENCODE_U_IMM ((guint32) (gsize) (imm)))); \
	} while (0)

/*
 * The J-type encoding is used exclusively for JAL.
 *
 * [0....6] opcode
 * [7...11] rd
 * [12..19] imm[12..19]
 * [20..20] imm[11]
 * [21..30] imm[1..10]
 * [31..31] imm[20]
 */

#define _riscv_j_op(p, opcode, rd, imm) \
	do { \
		g_assert (RISCV_VALID_REG ((rd))); \
		g_assert (RISCV_VALID_J_IMM ((gint32) (gssize) (imm))); \
		_riscv_emit ((p), ((opcode) << 0) | \
		                  ((rd) << 7) | \
		                  (RISCV_ENCODE_J_IMM ((gint32) (gssize) (imm)))); \
	} while (0)

/*
 * Fence instructions have a peculiar encoding that isn't quite like any of the
 * other formal encoding categories.
 *
 * [0....6] opcode
 * [7...11] rd
 * [12..14] funct3
 * [15..19] rs1
 * [20..23] succ
 * [24..27] pred
 * [28..31] imm[0..3]
 */

#define _riscv_f_op(p, opcode, funct3, rd, rs1, pred, succ, imm) \
	do { \
		g_assert (RISCV_VALID_REG ((rd))); \
		g_assert (RISCV_VALID_REG ((rs1))); \
		g_assert (RISCV_VALID_FENCE ((pred))); \
		g_assert (RISCV_VALID_FENCE ((succ))); \
		_riscv_emit ((p), ((opcode) << 0) | \
		                  ((rd) << 7) | \
		                  ((funct3) << 12) | \
		                  ((rs1) << 15) | \
		                  (RISCV_BITS ((succ), 0, 4) << 20) | \
		                  (RISCV_BITS ((pred), 0, 4) << 24) | \
		                  (RISCV_BITS ((guint32) (gsize) (imm), 0, 4) << 28)); \
	} while (0)

/*
 * Atomic instructions have a peculiar encoding that isn't quite like any of
 * the other formal encoding categories.
 *
 * [0....6] opcode
 * [7...11] rd
 * [12..14] funct3
 * [15..19] rs1
 * [20..24] rs2
 * [25..26] ordering
 * [27..31] funct5
 */

#define _riscv_a_op(p, opcode, funct3, funct5, ordering, rd, rs2, rs1) \
	do { \
		g_assert (RISCV_VALID_REG ((rd))); \
		g_assert (RISCV_VALID_REG ((rs1))); \
		g_assert (RISCV_VALID_REG ((rs2))); \
		g_assert (RISCV_VALID_ORDERING ((ordering))); \
		_riscv_emit ((p), ((opcode) << 0) | \
		                  ((rd) << 7) | \
		                  ((funct3) << 12) | \
		                  ((rs1) << 15) | \
		                  ((rs2) << 20) | \
		                  (RISCV_BITS ((ordering), 0, 2) << 25) | \
		                  ((funct5) << 27)); \
	} while (0)

/*
 * NOTE: When you add new codegen macros or change existing ones, you must
 * expand riscv-codegen-test.c to cover them, and update riscv-codegen.exp32
 * and riscv-codegen.exp64 as needed.
 */

// RV32I

#define riscv_lui(p, rd, imm)                      _riscv_u_op  ((p), 0b0110111, (rd), (imm))
#define riscv_auipc(p, rd, imm)                    _riscv_u_op  ((p), 0b0010111, (rd), (imm))
#define riscv_jal(p, rd, imm)                      _riscv_j_op  ((p), 0b1101111, (rd), (imm))
#define riscv_jalr(p, rd, rs1, imm)                _riscv_i_op  ((p), 0b1100111, 0b000, (rd), (rs1), (imm))
#define riscv_beq(p, rs1, rs2, imm)                _riscv_b_op  ((p), 0b1100011, 0b000, (rs1), (rs2), (imm))
#define riscv_bne(p, rs1, rs2, imm)                _riscv_b_op  ((p), 0b1100011, 0b001, (rs1), (rs2), (imm))
#define riscv_blt(p, rs1, rs2, imm)                _riscv_b_op  ((p), 0b1100011, 0b100, (rs1), (rs2), (imm))
#define riscv_bge(p, rs1, rs2, imm)                _riscv_b_op  ((p), 0b1100011, 0b101, (rs1), (rs2), (imm))
#define riscv_bltu(p, rs1, rs2, imm)               _riscv_b_op  ((p), 0b1100011, 0b110, (rs1), (rs2), (imm))
#define riscv_bgeu(p, rs1, rs2, imm)               _riscv_b_op  ((p), 0b1100011, 0b111, (rs1), (rs2), (imm))
#define riscv_lb(p, rd, rs1, imm)                  _riscv_i_op  ((p), 0b0000011, 0b000, (rd), (rs1), (imm))
#define riscv_lh(p, rd, rs1, imm)                  _riscv_i_op  ((p), 0b0000011, 0b001, (rd), (rs1), (imm))
#define riscv_lw(p, rd, rs1, imm)                  _riscv_i_op  ((p), 0b0000011, 0b010, (rd), (rs1), (imm))
#define riscv_lbu(p, rd, rs1, imm)                 _riscv_i_op  ((p), 0b0000011, 0b100, (rd), (rs1), (imm))
#define riscv_lhu(p, rd, rs1, imm)                 _riscv_i_op  ((p), 0b0000011, 0b101, (rd), (rs1), (imm))
#define riscv_sb(p, rs2, rs1, imm)                 _riscv_s_op  ((p), 0b0100011, 0b000, (rs2), (rs1), (imm))
#define riscv_sh(p, rs2, rs1, imm)                 _riscv_s_op  ((p), 0b0100011, 0b001, (rs2), (rs1), (imm))
#define riscv_sw(p, rs2, rs1, imm)                 _riscv_s_op  ((p), 0b0100011, 0b010, (rs2), (rs1), (imm))
#define riscv_addi(p, rd, rs1, imm)                _riscv_i_op  ((p), 0b0010011, 0b000, (rd), (rs1), (imm))
#define riscv_slti(p, rd, rs1, imm)                _riscv_i_op  ((p), 0b0010011, 0b010, (rd), (rs1), (imm))
#define riscv_sltiu(p, rd, rs1, imm)               _riscv_i_op  ((p), 0b0010011, 0b011, (rd), (rs1), (imm))
#define riscv_xori(p, rd, rs1, imm)                _riscv_i_op  ((p), 0b0010011, 0b100, (rd), (rs1), (imm))
#define riscv_ori(p, rd, rs1, imm)                 _riscv_i_op  ((p), 0b0010011, 0b110, (rd), (rs1), (imm))
#define riscv_andi(p, rd, rs1, imm)                _riscv_i_op  ((p), 0b0010011, 0b111, (rd), (rs1), (imm))
#ifdef TARGET_RISCV32
#define riscv_slli(p, rd, rs1, shamt)              _riscv_is_op ((p), 0b0010011, 0b001, 0b0000000, (rd), (rs1), (shamt))
#define riscv_srli(p, rd, rs1, shamt)              _riscv_is_op ((p), 0b0010011, 0b101, 0b0000000, (rd), (rs1), (shamt))
#define riscv_srai(p, rd, rs1, shamt)              _riscv_is_op ((p), 0b0010011, 0b101, 0b0100000, (rd), (rs1), (shamt))
#endif
#define riscv_add(p, rd, rs1, rs2)                 _riscv_r_op  ((p), 0b0110011, 0b000, 0b0000000, (rd), (rs1), (rs2))
#define riscv_sub(p, rd, rs1, rs2)                 _riscv_r_op  ((p), 0b0110011, 0b000, 0b0100000, (rd), (rs1), (rs2))
#define riscv_sll(p, rd, rs1, rs2)                 _riscv_r_op  ((p), 0b0110011, 0b001, 0b0000000, (rd), (rs1), (rs2))
#define riscv_slt(p, rd, rs1, rs2)                 _riscv_r_op  ((p), 0b0110011, 0b010, 0b0000000, (rd), (rs1), (rs2))
#define riscv_sltu(p, rd, rs1, rs2)                _riscv_r_op  ((p), 0b0110011, 0b011, 0b0000000, (rd), (rs1), (rs2))
#define riscv_xor(p, rd, rs1, rs2)                 _riscv_r_op  ((p), 0b0110011, 0b100, 0b0000000, (rd), (rs1), (rs2))
#define riscv_srl(p, rd, rs1, rs2)                 _riscv_r_op  ((p), 0b0110011, 0b101, 0b0000000, (rd), (rs1), (rs2))
#define riscv_sra(p, rd, rs1, rs2)                 _riscv_r_op  ((p), 0b0110011, 0b101, 0b0100000, (rd), (rs1), (rs2))
#define riscv_or(p, rd, rs1, rs2)                  _riscv_r_op  ((p), 0b0110011, 0b110, 0b0000000, (rd), (rs1), (rs2))
#define riscv_and(p, rd, rs1, rs2)                 _riscv_r_op  ((p), 0b0110011, 0b111, 0b0000000, (rd), (rs1), (rs2))
#define riscv_fence(p, pred, succ)                 _riscv_f_op  ((p), 0b0001111, 0b000, 0b00000, 0b00000, (pred), (succ), 0b0000)
#define riscv_fence_i(p)                           _riscv_f_op  ((p), 0b0001111, 0b001, 0b00000, 0b00000, 0b0000, 0b0000, 0b0000)
#define riscv_ecall(p)                             _riscv_i_op  ((p), 0b1110011, 0b000, 0b00000, 0b00000, 0b000000000000)
#define riscv_ebreak(p)                            _riscv_i_op  ((p), 0b1110011, 0b000, 0b00000, 0b00000, 0b000000000001)
#define riscv_csrrw(p, rd, csr, rs1)               _riscv_ic_op ((p), 0b1110011, 0b001, (rd), (csr), (rs1))
#define riscv_csrrs(p, rd, csr, rs1)               _riscv_ic_op ((p), 0b1110011, 0b010, (rd), (csr), (rs1))
#define riscv_csrrc(p, rd, csr, rs1)               _riscv_ic_op ((p), 0b1110011, 0b011, (rd), (csr), (rs1))
#define riscv_csrrwi(p, rd, csr, imm)              _riscv_ic_op ((p), 0b1110011, 0b101, (rd), (csr), (imm))
#define riscv_csrrsi(p, rd, csr, imm)              _riscv_ic_op ((p), 0b1110011, 0b110, (rd), (csr), (imm))
#define riscv_csrrci(p, rd, csr, imm)              _riscv_ic_op ((p), 0b1110011, 0b111, (rd), (csr), (imm))

// RV64I

#ifdef TARGET_RISCV64
#define riscv_lwu(p, rd, rs1, imm)                 _riscv_i_op  ((p), 0b0000011, 0b110, (rd), (rs1), (imm))
#define riscv_ld(p, rd, rs1, imm)                  _riscv_i_op  ((p), 0b0000011, 0b011, (rd), (rs1), (imm))
#define riscv_sd(p, rs2, rs1, imm)                 _riscv_s_op  ((p), 0b0100011, 0b011, (rs2), (rs1), (imm))
#define riscv_slli(p, rd, rs1, shamt)              _riscv_ls_op ((p), 0b0010011, 0b001, 0b000000, (rd), (rs1), (shamt))
#define riscv_srli(p, rd, rs1, shamt)              _riscv_ls_op ((p), 0b0010011, 0b101, 0b000000, (rd), (rs1), (shamt))
#define riscv_srai(p, rd, rs1, shamt)              _riscv_ls_op ((p), 0b0010011, 0b101, 0b010000, (rd), (rs1), (shamt))
#define riscv_addiw(p, rd, rs1, imm)               _riscv_i_op  ((p), 0b0011011, 0b000, (rd), (rs1), (imm))
#define riscv_slliw(p, rd, rs1, shamt)             _riscv_is_op ((p), 0b0011011, 0b001, 0b0000000, (rd), (rs1), (shamt))
#define riscv_srliw(p, rd, rs1, shamt)             _riscv_is_op ((p), 0b0011011, 0b101, 0b0000000, (rd), (rs1), (shamt))
#define riscv_sraiw(p, rd, rs1, shamt)             _riscv_is_op ((p), 0b0011011, 0b101, 0b0100000, (rd), (rs1), (shamt))
#define riscv_addw(p, rd, rs1, rs2)                _riscv_r_op  ((p), 0b0111011, 0b000, 0b0000000, (rd), (rs1), (rs2))
#define riscv_subw(p, rd, rs1, rs2)                _riscv_r_op  ((p), 0b0111011, 0b000, 0b0100000, (rd), (rs1), (rs2))
#define riscv_sllw(p, rd, rs1, rs2)                _riscv_r_op  ((p), 0b0111011, 0b001, 0b0000000, (rd), (rs1), (rs2))
#define riscv_srlw(p, rd, rs1, rs2)                _riscv_r_op  ((p), 0b0111011, 0b101, 0b0000000, (rd), (rs1), (rs2))
#define riscv_sraw(p, rd, rs1, rs2)                _riscv_r_op  ((p), 0b0111011, 0b101, 0b0100000, (rd), (rs1), (rs2))
#endif

// RV32M

#define riscv_mul(p, rd, rs1, rs2)                 _riscv_r_op  ((p), 0b0110011, 0b000, 0b0000001, (rd), (rs1), (rs2))
#define riscv_mulh(p, rd, rs1, rs2)                _riscv_r_op  ((p), 0b0110011, 0b001, 0b0000001, (rd), (rs1), (rs2))
#define riscv_mulhsu(p, rd, rs1, rs2)              _riscv_r_op  ((p), 0b0110011, 0b010, 0b0000001, (rd), (rs1), (rs2))
#define riscv_mulhu(p, rd, rs1, rs2)               _riscv_r_op  ((p), 0b0110011, 0b011, 0b0000001, (rd), (rs1), (rs2))
#define riscv_div(p, rd, rs1, rs2)                 _riscv_r_op  ((p), 0b0110011, 0b100, 0b0000001, (rd), (rs1), (rs2))
#define riscv_divu(p, rd, rs1, rs2)                _riscv_r_op  ((p), 0b0110011, 0b101, 0b0000001, (rd), (rs1), (rs2))
#define riscv_rem(p, rd, rs1, rs2)                 _riscv_r_op  ((p), 0b0110011, 0b110, 0b0000001, (rd), (rs1), (rs2))
#define riscv_remu(p, rd, rs1, rs2)                _riscv_r_op  ((p), 0b0110011, 0b111, 0b0000001, (rd), (rs1), (rs2))

// RV64M

#ifdef TARGET_RISCV64
#define riscv_mulw(p, rd, rs1, rs2)                _riscv_r_op  ((p), 0b0111011, 0b000, 0b0000001, (rd), (rs1), (rs2))
#define riscv_divw(p, rd, rs1, rs2)                _riscv_r_op  ((p), 0b0111011, 0b100, 0b0000001, (rd), (rs1), (rs2))
#define riscv_divuw(p, rd, rs1, rs2)               _riscv_r_op  ((p), 0b0111011, 0b101, 0b0000001, (rd), (rs1), (rs2))
#define riscv_remw(p, rd, rs1, rs2)                _riscv_r_op  ((p), 0b0111011, 0b110, 0b0000001, (rd), (rs1), (rs2))
#define riscv_remuw(p, rd, rs1, rs2)               _riscv_r_op  ((p), 0b0111011, 0b111, 0b0000001, (rd), (rs1), (rs2))
#endif

// RV32A

#define riscv_lr_w(p, ordering, rd, rs1)           _riscv_a_op  ((p), 0b0101111, 0b010, 0b00010, (ordering), (rd), 0b00000, (rs1))
#define riscv_sc_w(p, ordering, rd, rs2, rs1)      _riscv_a_op  ((p), 0b0101111, 0b010, 0b00011, (ordering), (rd), (rs2), (rs1))
#define riscv_amoswap_w(p, ordering, rd, rs2, rs1) _riscv_a_op  ((p), 0b0101111, 0b010, 0b00001, (ordering), (rd), (rs2), (rs1))
#define riscv_amoadd_w(p, ordering, rd, rs2, rs1)  _riscv_a_op  ((p), 0b0101111, 0b010, 0b00000, (ordering), (rd), (rs2), (rs1))
#define riscv_amoxor_w(p, ordering, rd, rs2, rs1)  _riscv_a_op  ((p), 0b0101111, 0b010, 0b00100, (ordering), (rd), (rs2), (rs1))
#define riscv_amoand_w(p, ordering, rd, rs2, rs1)  _riscv_a_op  ((p), 0b0101111, 0b010, 0b01100, (ordering), (rd), (rs2), (rs1))
#define riscv_amoor_w(p, ordering, rd, rs2, rs1)   _riscv_a_op  ((p), 0b0101111, 0b010, 0b01000, (ordering), (rd), (rs2), (rs1))
#define riscv_amomin_w(p, ordering, rd, rs2, rs1)  _riscv_a_op  ((p), 0b0101111, 0b010, 0b10000, (ordering), (rd), (rs2), (rs1))
#define riscv_amomax_w(p, ordering, rd, rs2, rs1)  _riscv_a_op  ((p), 0b0101111, 0b010, 0b10100, (ordering), (rd), (rs2), (rs1))
#define riscv_amominu_w(p, ordering, rd, rs2, rs1) _riscv_a_op  ((p), 0b0101111, 0b010, 0b11000, (ordering), (rd), (rs2), (rs1))
#define riscv_amomaxu_w(p, ordering, rd, rs2, rs1) _riscv_a_op  ((p), 0b0101111, 0b010, 0b11100, (ordering), (rd), (rs2), (rs1))

// RV64A

#ifdef TARGET_RISCV64
#define riscv_lr_d(p, ordering, rd, rs1)           _riscv_a_op  ((p), 0b0101111, 0b011, 0b00010, (ordering), (rd), 0b00000, (rs1))
#define riscv_sc_d(p, ordering, rd, rs2, rs1)      _riscv_a_op  ((p), 0b0101111, 0b011, 0b00011, (ordering), (rd), (rs2), (rs1))
#define riscv_amoswap_d(p, ordering, rd, rs2, rs1) _riscv_a_op  ((p), 0b0101111, 0b011, 0b00001, (ordering), (rd), (rs2), (rs1))
#define riscv_amoadd_d(p, ordering, rd, rs2, rs1)  _riscv_a_op  ((p), 0b0101111, 0b011, 0b00000, (ordering), (rd), (rs2), (rs1))
#define riscv_amoxor_d(p, ordering, rd, rs2, rs1)  _riscv_a_op  ((p), 0b0101111, 0b011, 0b00100, (ordering), (rd), (rs2), (rs1))
#define riscv_amoand_d(p, ordering, rd, rs2, rs1)  _riscv_a_op  ((p), 0b0101111, 0b011, 0b01100, (ordering), (rd), (rs2), (rs1))
#define riscv_amoor_d(p, ordering, rd, rs2, rs1)   _riscv_a_op  ((p), 0b0101111, 0b011, 0b01000, (ordering), (rd), (rs2), (rs1))
#define riscv_amomin_d(p, ordering, rd, rs2, rs1)  _riscv_a_op  ((p), 0b0101111, 0b011, 0b10000, (ordering), (rd), (rs2), (rs1))
#define riscv_amomax_d(p, ordering, rd, rs2, rs1)  _riscv_a_op  ((p), 0b0101111, 0b011, 0b10100, (ordering), (rd), (rs2), (rs1))
#define riscv_amominu_d(p, ordering, rd, rs2, rs1) _riscv_a_op  ((p), 0b0101111, 0b011, 0b11000, (ordering), (rd), (rs2), (rs1))
#define riscv_amomaxu_d(p, ordering, rd, rs2, rs1) _riscv_a_op  ((p), 0b0101111, 0b011, 0b11100, (ordering), (rd), (rs2), (rs1))
#endif

// RV32F

#define riscv_flw(p, rd, rs1, imm)                 _riscv_i_op  ((p), 0b0000111, 0b010, (rd), (rs1), (imm))
#define riscv_fsw(p, rs2, rs1, imm)                _riscv_s_op  ((p), 0b0100111, 0b010, (rs2), (rs1), (imm))
#define riscv_fmadd_s(p, rm, rd, rs1, rs2, rs3)    _riscv_r4_op ((p), 0b1000011, (rm), 0b00, (rd), (rs1), (rs2), (rs3))
#define riscv_fmsub_s(p, rm, rd, rs1, rs2, rs3)    _riscv_r4_op ((p), 0b1000111, (rm), 0b00, (rd), (rs1), (rs2), (rs3))
#define riscv_fnmadd_s(p, rm, rd, rs1, rs2, rs3)   _riscv_r4_op ((p), 0b1001011, (rm), 0b00, (rd), (rs1), (rs2), (rs3))
#define riscv_fnmsub_s(p, rm, rd, rs1, rs2, rs3)   _riscv_r4_op ((p), 0b1001111, (rm), 0b00, (rd), (rs1), (rs2), (rs3))
#define riscv_fadd_s(p, rm, rd, rs1, rs2)          _riscv_r_op  ((p), 0b1010011, (rm), 0b0000000, (rd), (rs1), (rs2))
#define riscv_fsub_s(p, rm, rd, rs1, rs2)          _riscv_r_op  ((p), 0b1010011, (rm), 0b0000100, (rd), (rs1), (rs2))
#define riscv_fmul_s(p, rm, rd, rs1, rs2)          _riscv_r_op  ((p), 0b1010011, (rm), 0b0001000, (rd), (rs1), (rs2))
#define riscv_fdiv_s(p, rm, rd, rs1, rs2)          _riscv_r_op  ((p), 0b1010011, (rm), 0b0001100, (rd), (rs1), (rs2))
#define riscv_fsqrt_s(p, rm, rd, rs1)              _riscv_r_op  ((p), 0b1010011, (rm), 0b0101100, (rd), (rs1), 0b00000)
#define riscv_fsgnj_s(p, rd, rs1, rs2)             _riscv_r_op  ((p), 0b1010011, 0b000, 0b0010000, (rd), (rs1), (rs2))
#define riscv_fsgnjn_s(p, rd, rs1, rs2)            _riscv_r_op  ((p), 0b1010011, 0b001, 0b0010000, (rd), (rs1), (rs2))
#define riscv_fsgnjx_s(p, rd, rs1, rs2)            _riscv_r_op  ((p), 0b1010011, 0b010, 0b0010000, (rd), (rs1), (rs2))
#define riscv_fmin_s(p, rd, rs1, rs2)              _riscv_r_op  ((p), 0b1010011, 0b000, 0b0010100, (rd), (rs1), (rs2))
#define riscv_fmax_s(p, rd, rs1, rs2)              _riscv_r_op  ((p), 0b1010011, 0b001, 0b0010100, (rd), (rs1), (rs2))
#define riscv_fcvt_w_s(p, rm, rd, rs1)             _riscv_r_op  ((p), 0b1010011, (rm), 0b1100000, (rd), (rs1), 0b00000)
#define riscv_fcvt_wu_s(p, rm, rd, rs1)            _riscv_r_op  ((p), 0b1010011, (rm), 0b1100000, (rd), (rs1), 0b00001)
#define riscv_fmv_x_w(p, rd, rs1)                  _riscv_r_op  ((p), 0b1010011, 0b000, 0b1110000, (rd), (rs1), 0b00000)
#define riscv_feq_s(p, rd, rs1, rs2)               _riscv_r_op  ((p), 0b1010011, 0b010, 0b1010000, (rd), (rs1), (rs2))
#define riscv_flt_s(p, rd, rs1, rs2)               _riscv_r_op  ((p), 0b1010011, 0b001, 0b1010000, (rd), (rs1), (rs2))
#define riscv_fle_s(p, rd, rs1, rs2)               _riscv_r_op  ((p), 0b1010011, 0b000, 0b1010000, (rd), (rs1), (rs2))
#define riscv_fclass_s(p, rd, rs1)                 _riscv_r_op  ((p), 0b1010011, 0b001, 0b1110000, (rd), (rs1), 0b00000)
#define riscv_fcvt_s_w(p, rm, rd, rs1)             _riscv_r_op  ((p), 0b1010011, (rm), 0b1101000, (rd), (rs1), 0b00000)
#define riscv_fcvt_s_wu(p, rm, rd, rs1)            _riscv_r_op  ((p), 0b1010011, (rm), 0b1101000, (rd), (rs1), 0b00001)
#define riscv_fmv_w_x(p, rd, rs1)                  _riscv_r_op  ((p), 0b1010011, 0b000, 0b1111000, (rd), (rs1), 0b00000)

// RV64F

#ifdef TARGET_RISCV64
#define riscv_fcvt_l_s(p, rm, rd, rs1)             _riscv_r_op ((p), 0b1010011, (rm), 0b1100000, (rd), (rs1), 0b00010)
#define riscv_fcvt_lu_s(p, rm, rd, rs1)            _riscv_r_op ((p), 0b1010011, (rm), 0b1100000, (rd), (rs1), 0b00011)
#define riscv_fcvt_s_l(p, rm, rd, rs1)             _riscv_r_op ((p), 0b1010011, (rm), 0b1101000, (rd), (rs1), 0b00010)
#define riscv_fcvt_s_lu(p, rm, rd, rs1)            _riscv_r_op ((p), 0b1010011, (rm), 0b1101000, (rd), (rs1), 0b00011)
#endif

// RV32D

#define riscv_fld(p, rd, rs1, imm)                 _riscv_i_op  ((p), 0b0000111, 0b011, (rd), (rs1), (imm))
#define riscv_fsd(p, rs2, rs1, imm)                _riscv_s_op  ((p), 0b0100111, 0b011, (rs2), (rs1), (imm))
#define riscv_fmadd_d(p, rm, rd, rs1, rs2, rs3)    _riscv_r4_op ((p), 0b1000011, (rm), 0b01, (rd), (rs1), (rs2), (rs3))
#define riscv_fmsub_d(p, rm, rd, rs1, rs2, rs3)    _riscv_r4_op ((p), 0b1000111, (rm), 0b01, (rd), (rs1), (rs2), (rs3))
#define riscv_fnmadd_d(p, rm, rd, rs1, rs2, rs3)   _riscv_r4_op ((p), 0b1001011, (rm), 0b01, (rd), (rs1), (rs2), (rs3))
#define riscv_fnmsub_d(p, rm, rd, rs1, rs2, rs3)   _riscv_r4_op ((p), 0b1001111, (rm), 0b01, (rd), (rs1), (rs2), (rs3))
#define riscv_fadd_d(p, rm, rd, rs1, rs2)          _riscv_r_op  ((p), 0b1010011, (rm), 0b0000001, (rd), (rs1), (rs2))
#define riscv_fsub_d(p, rm, rd, rs1, rs2)          _riscv_r_op  ((p), 0b1010011, (rm), 0b0000101, (rd), (rs1), (rs2))
#define riscv_fmul_d(p, rm, rd, rs1, rs2)          _riscv_r_op  ((p), 0b1010011, (rm), 0b0001001, (rd), (rs1), (rs2))
#define riscv_fdiv_d(p, rm, rd, rs1, rs2)          _riscv_r_op  ((p), 0b1010011, (rm), 0b0001101, (rd), (rs1), (rs2))
#define riscv_fsqrt_d(p, rm, rd, rs1)              _riscv_r_op  ((p), 0b1010011, (rm), 0b0101101, (rd), (rs1), 0b00000)
#define riscv_fsgnj_d(p, rd, rs1, rs2)             _riscv_r_op  ((p), 0b1010011, 0b000, 0b0010001, (rd), (rs1), (rs2))
#define riscv_fsgnjn_d(p, rd, rs1, rs2)            _riscv_r_op  ((p), 0b1010011, 0b001, 0b0010001, (rd), (rs1), (rs2))
#define riscv_fsgnjx_d(p, rd, rs1, rs2)            _riscv_r_op  ((p), 0b1010011, 0b010, 0b0010001, (rd), (rs1), (rs2))
#define riscv_fmin_d(p, rd, rs1, rs2)              _riscv_r_op  ((p), 0b1010011, 0b000, 0b0010101, (rd), (rs1), (rs2))
#define riscv_fmax_d(p, rd, rs1, rs2)              _riscv_r_op  ((p), 0b1010011, 0b001, 0b0010101, (rd), (rs1), (rs2))
#define riscv_fcvt_s_d(p, rm, rd, rs1)             _riscv_r_op  ((p), 0b1010011, (rm), 0b0100000, (rd), (rs1), 0b00001)
#define riscv_fcvt_d_s(p, rd, rs1)                 _riscv_r_op  ((p), 0b1010011, 0b000, 0b0100001, (rd), (rs1), 0b00000)
#define riscv_feq_d(p, rd, rs1, rs2)               _riscv_r_op  ((p), 0b1010011, 0b010, 0b1010001, (rd), (rs1), (rs2))
#define riscv_flt_d(p, rd, rs1, rs2)               _riscv_r_op  ((p), 0b1010011, 0b001, 0b1010001, (rd), (rs1), (rs2))
#define riscv_fle_d(p, rd, rs1, rs2)               _riscv_r_op  ((p), 0b1010011, 0b000, 0b1010001, (rd), (rs1), (rs2))
#define riscv_fclass_d(p, rd, rs1)                 _riscv_r_op  ((p), 0b1010011, 0b001, 0b1110001, (rd), (rs1), 0b00000)
#define riscv_fcvt_w_d(p, rm, rd, rs1)             _riscv_r_op  ((p), 0b1010011, (rm), 0b1100001, (rd), (rs1), 0b00000)
#define riscv_fcvt_wu_d(p, rm, rd, rs1)            _riscv_r_op  ((p), 0b1010011, (rm), 0b1100001, (rd), (rs1), 0b00001)
#define riscv_fcvt_d_w(p, rd, rs1)                 _riscv_r_op  ((p), 0b1010011, 0b000, 0b1101001, (rd), (rs1), 0b00000)
#define riscv_fcvt_d_wu(p, rd, rs1)                _riscv_r_op  ((p), 0b1010011, 0b000, 0b1101001, (rd), (rs1), 0b00001)

// RV64D

#ifdef TARGET_RISCV64
#define riscv_fcvt_l_d(p, rm, rd, rs1)             _riscv_r_op  ((p), 0b1010011, (rm), 0b1100001, (rd), (rs1), 0b00010)
#define riscv_fcvt_lu_d(p, rm, rd, rs1)            _riscv_r_op  ((p), 0b1010011, (rm), 0b1100001, (rd), (rs1), 0b00011)
#define riscv_fmv_x_d(p, rd, rs1)                  _riscv_r_op  ((p), 0b1010011, 0b000, 0b1110001, (rd), (rs1), 0b00000)
#define riscv_fcvt_d_l(p, rm, rd, rs1)             _riscv_r_op  ((p), 0b1010011, (rm), 0b1101001, (rd), (rs1), 0b00010)
#define riscv_fcvt_d_lu(p, rm, rd, rs1)            _riscv_r_op  ((p), 0b1010011, (rm), 0b1101001, (rd), (rs1), 0b00011)
#define riscv_fmv_d_x(p, rd, rs1)                  _riscv_r_op  ((p), 0b1010011, 0b000, 0b1111001, (rd), (rs1), 0b00000)
#endif

#endif
