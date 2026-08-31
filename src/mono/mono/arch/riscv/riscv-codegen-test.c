/*
 * Licensed to the .NET Foundation under one or more agreements.
 * The .NET Foundation licenses this file to you under the MIT license.
 */

#define MONO_RISCV_CODEGEN_TEST

#include "riscv-codegen.h"
#include <stdio.h>

static guint8 code [4096 * 16];

int
main (void)
{
	guint8 *p = code;

	{
		// R

		riscv_add (p, RISCV_X0, RISCV_X1, RISCV_X2);
		riscv_sub (p, RISCV_X0, RISCV_X1, RISCV_X2);
		riscv_sll (p, RISCV_X0, RISCV_X1, RISCV_X2);
		riscv_slt (p, RISCV_X0, RISCV_X1, RISCV_X2);
		riscv_sltu (p, RISCV_X0, RISCV_X1, RISCV_X2);
		riscv_xor (p, RISCV_X0, RISCV_X1, RISCV_X2);
		riscv_srl (p, RISCV_X0, RISCV_X1, RISCV_X2);
		riscv_sra (p, RISCV_X0, RISCV_X1, RISCV_X2);
		riscv_or (p, RISCV_X0, RISCV_X1, RISCV_X2);
		riscv_and (p, RISCV_X0, RISCV_X1, RISCV_X2);
#ifdef TARGET_RISCV64
		riscv_addw (p, RISCV_X0, RISCV_X1, RISCV_X2);
		riscv_subw (p, RISCV_X0, RISCV_X1, RISCV_X2);
		riscv_sllw (p, RISCV_X0, RISCV_X1, RISCV_X2);
		riscv_srlw (p, RISCV_X0, RISCV_X1, RISCV_X2);
		riscv_sraw (p, RISCV_X0, RISCV_X1, RISCV_X2);
#endif
		riscv_mul (p, RISCV_X0, RISCV_X1, RISCV_X2);
		riscv_mulh (p, RISCV_X0, RISCV_X1, RISCV_X2);
		riscv_mulhsu (p, RISCV_X0, RISCV_X1, RISCV_X2);
		riscv_mulhu (p, RISCV_X0, RISCV_X1, RISCV_X2);
		riscv_div (p, RISCV_X0, RISCV_X1, RISCV_X2);
		riscv_divu (p, RISCV_X0, RISCV_X1, RISCV_X2);
		riscv_rem (p, RISCV_X0, RISCV_X1, RISCV_X2);
		riscv_remu (p, RISCV_X0, RISCV_X1, RISCV_X2);
#ifdef TARGET_RISCV64
		riscv_mulw (p, RISCV_X0, RISCV_X1, RISCV_X2);
		riscv_divw (p, RISCV_X0, RISCV_X1, RISCV_X2);
		riscv_divuw (p, RISCV_X0, RISCV_X1, RISCV_X2);
		riscv_remw (p, RISCV_X0, RISCV_X1, RISCV_X2);
		riscv_remuw (p, RISCV_X0, RISCV_X1, RISCV_X2);
#endif
		riscv_fadd_s (p, RISCV_ROUND_NE, RISCV_F0, RISCV_F1, RISCV_F2);
		riscv_fsub_s (p, RISCV_ROUND_TZ, RISCV_F0, RISCV_F1, RISCV_F2);
		riscv_fmul_s (p, RISCV_ROUND_DN, RISCV_F0, RISCV_F1, RISCV_F2);
		riscv_fdiv_s (p, RISCV_ROUND_UP, RISCV_F0, RISCV_F1, RISCV_F2);
		riscv_fsqrt_s (p, RISCV_ROUND_MM, RISCV_F0, RISCV_F1);
		riscv_fsgnj_s (p, RISCV_F0, RISCV_F1, RISCV_F2);
		riscv_fsgnjn_s (p, RISCV_F0, RISCV_F1, RISCV_F2);
		riscv_fsgnjx_s (p, RISCV_F0, RISCV_F1, RISCV_F2);
		riscv_fmin_s (p, RISCV_F0, RISCV_F1, RISCV_F2);
		riscv_fmax_s (p, RISCV_F0, RISCV_F1, RISCV_F2);
		riscv_fcvt_w_s (p, RISCV_ROUND_DY, RISCV_X0, RISCV_F1);
		riscv_fcvt_wu_s (p, RISCV_ROUND_NE, RISCV_X0, RISCV_F1);
		riscv_fmv_x_w (p, RISCV_X0, RISCV_F1);
		riscv_feq_s (p, RISCV_X0, RISCV_F1, RISCV_F2);
		riscv_flt_s (p, RISCV_X0, RISCV_F1, RISCV_F2);
		riscv_fle_s (p, RISCV_X0, RISCV_F1, RISCV_F2);
		riscv_fclass_s (p, RISCV_X0, RISCV_F1);
		riscv_fcvt_s_w (p, RISCV_ROUND_TZ, RISCV_F0, RISCV_X1);
		riscv_fcvt_s_wu (p, RISCV_ROUND_DN, RISCV_F0, RISCV_X1);
		riscv_fmv_w_x (p, RISCV_F0, RISCV_X1);
#ifdef TARGET_RISCV64
		riscv_fcvt_l_s (p, RISCV_ROUND_UP, RISCV_X0, RISCV_F1);
		riscv_fcvt_lu_s (p, RISCV_ROUND_MM, RISCV_X0, RISCV_F1);
		riscv_fcvt_s_l (p, RISCV_ROUND_DY, RISCV_F0, RISCV_X1);
		riscv_fcvt_s_lu (p, RISCV_ROUND_NE, RISCV_F0, RISCV_X1);
#endif
		riscv_fadd_d (p, RISCV_ROUND_TZ, RISCV_F0, RISCV_F1, RISCV_F2);
		riscv_fsub_d (p, RISCV_ROUND_DN, RISCV_F0, RISCV_F1, RISCV_F2);
		riscv_fmul_d (p, RISCV_ROUND_UP, RISCV_F0, RISCV_F1, RISCV_F2);
		riscv_fdiv_d (p, RISCV_ROUND_MM, RISCV_F0, RISCV_F1, RISCV_F2);
		riscv_fsqrt_d (p, RISCV_ROUND_DY, RISCV_F0, RISCV_F1);
		riscv_fsgnj_d (p, RISCV_F0, RISCV_F1, RISCV_F2);
		riscv_fsgnjn_d (p, RISCV_F0, RISCV_F1, RISCV_F2);
		riscv_fsgnjx_d (p, RISCV_F0, RISCV_F1, RISCV_F2);
		riscv_fmin_d (p, RISCV_F0, RISCV_F1, RISCV_F2);
		riscv_fmax_d (p, RISCV_F0, RISCV_F1, RISCV_F2);
		riscv_fcvt_s_d (p, RISCV_ROUND_NE, RISCV_F0, RISCV_F1);
		riscv_fcvt_d_s (p, RISCV_F0, RISCV_F1);
		riscv_feq_d (p, RISCV_X0, RISCV_F1, RISCV_F2);
		riscv_flt_d (p, RISCV_X0, RISCV_F1, RISCV_F2);
		riscv_fle_d (p, RISCV_X0, RISCV_F1, RISCV_F2);
		riscv_fclass_d (p, RISCV_X0, RISCV_F1);
		riscv_fcvt_w_d (p, RISCV_ROUND_TZ, RISCV_X0, RISCV_F1);
		riscv_fcvt_wu_d (p, RISCV_ROUND_DN, RISCV_X0, RISCV_F1);
		riscv_fcvt_d_w (p, RISCV_F0, RISCV_X1);
		riscv_fcvt_d_wu (p, RISCV_F0, RISCV_X1);
#ifdef TARGET_RISCV64
		riscv_fcvt_l_d (p, RISCV_ROUND_UP, RISCV_X0, RISCV_F1);
		riscv_fcvt_lu_d (p, RISCV_ROUND_MM, RISCV_X0, RISCV_F1);
		riscv_fmv_x_d (p, RISCV_X0, RISCV_F1);
		riscv_fcvt_d_l (p, RISCV_ROUND_DY, RISCV_F0, RISCV_X1);
		riscv_fcvt_d_lu (p, RISCV_ROUND_NE, RISCV_F0, RISCV_X1);
		riscv_fmv_d_x (p, RISCV_F0, RISCV_X1);
#endif

		// R4

		riscv_fmadd_s (p, RISCV_ROUND_NE, RISCV_F0, RISCV_F1, RISCV_F2, RISCV_F3);
		riscv_fmsub_s (p, RISCV_ROUND_TZ, RISCV_F0, RISCV_F1, RISCV_F2, RISCV_F3);
		riscv_fnmadd_s (p, RISCV_ROUND_DN, RISCV_F0, RISCV_F1, RISCV_F2, RISCV_F3);
		riscv_fnmadd_s (p, RISCV_ROUND_UP, RISCV_F0, RISCV_F1, RISCV_F2, RISCV_F3);
		riscv_fmadd_d (p, RISCV_ROUND_MM, RISCV_F0, RISCV_F1, RISCV_F2, RISCV_F3);
		riscv_fmsub_d (p, RISCV_ROUND_DY, RISCV_F0, RISCV_F1, RISCV_F2, RISCV_F3);
		riscv_fnmadd_d (p, RISCV_ROUND_NE, RISCV_F0, RISCV_F1, RISCV_F2, RISCV_F3);
		riscv_fnmadd_d (p, RISCV_ROUND_TZ, RISCV_F0, RISCV_F1, RISCV_F2, RISCV_F3);

		// I

		riscv_jalr (p, RISCV_X0, RISCV_X1, 123);
		riscv_jalr (p, RISCV_X0, RISCV_X1, -123);
		riscv_lb (p, RISCV_X0, RISCV_X1, 321);
		riscv_lh (p, RISCV_X0, RISCV_X1, -321);
		riscv_lw (p, RISCV_X0, RISCV_X1, 123);
		riscv_lbu (p, RISCV_X0, RISCV_X1, -123);
		riscv_lhu (p, RISCV_X0, RISCV_X1, 321);
		riscv_addi (p, RISCV_X0, RISCV_X1, -321);
		riscv_slti (p, RISCV_X0, RISCV_X1, 123);
		riscv_sltiu (p, RISCV_X0, RISCV_X1, -123);
		riscv_xori (p, RISCV_X0, RISCV_X1, 321);
		riscv_ori (p, RISCV_X0, RISCV_X1, -321);
		riscv_andi (p, RISCV_X0, RISCV_X1, 123);
		riscv_ecall (p);
		riscv_ebreak (p);
#ifdef TARGET_RISCV64
		riscv_lwu (p, RISCV_X0, RISCV_X1, -123);
		riscv_ld (p, RISCV_X0, RISCV_X1, 321);
		riscv_addiw (p, RISCV_X0, RISCV_X1, -321);
#endif
		riscv_flw (p, RISCV_F0, RISCV_X1, 123);
		riscv_fld (p, RISCV_F0, RISCV_X1, -123);

		// IS/LS

		riscv_slli (p, RISCV_X0, RISCV_X1, 1);
		riscv_srli (p, RISCV_X0, RISCV_X1, 2);
		riscv_srai (p, RISCV_X0, RISCV_X1, 3);
#ifdef TARGET_RISCV64
		riscv_slliw (p, RISCV_X0, RISCV_X1, 1);
		riscv_srliw (p, RISCV_X0, RISCV_X1, 2);
		riscv_sraiw (p, RISCV_X0, RISCV_X1, 3);
#endif

		// IC

		riscv_csrrw (p, RISCV_X0, RISCV_CSR_FFLAGS, RISCV_X1);
		riscv_csrrs (p, RISCV_X0, RISCV_CSR_FRM, RISCV_X1);
		riscv_csrrc (p, RISCV_X0, RISCV_CSR_FCSR, RISCV_X1);
		riscv_csrrwi (p, RISCV_X0, RISCV_CSR_CYCLE, 1);
		riscv_csrrsi (p, RISCV_X0, RISCV_CSR_TIME, 2);
		riscv_csrrci (p, RISCV_X0, RISCV_CSR_INSTRET, 3);

		// S

		riscv_sb (p, RISCV_X0, RISCV_X1, 123);
		riscv_sh (p, RISCV_X0, RISCV_X1, -123);
		riscv_sw (p, RISCV_X0, RISCV_X1, 321);
#ifdef TARGET_RISCV64
		riscv_sd (p, RISCV_X0, RISCV_X1, -321);
#endif
		riscv_fsw (p, RISCV_F0, RISCV_X1, 123);
		riscv_fsd (p, RISCV_F0, RISCV_X1, -123);

		// B

		riscv_beq (p, RISCV_X0, RISCV_X1, 128);
		riscv_bne (p, RISCV_X0, RISCV_X1, -128);
		riscv_blt (p, RISCV_X0, RISCV_X1, 256);
		riscv_bge (p, RISCV_X0, RISCV_X1, -256);
		riscv_bltu (p, RISCV_X0, RISCV_X1, 512);
		riscv_bgeu (p, RISCV_X0, RISCV_X1, -512);

		// U

		riscv_lui (p, RISCV_X0, 123);
		riscv_auipc (p, RISCV_X0, 321);

		// J

		riscv_jal (p, RISCV_X0, 128);
		riscv_jal (p, RISCV_X0, -128);

		// F

		riscv_fence (p, RISCV_FENCE_W, RISCV_FENCE_R);
		riscv_fence (p, RISCV_FENCE_O, RISCV_FENCE_I);
		riscv_fence (p, RISCV_FENCE_MEM, RISCV_FENCE_DEV);
		riscv_fence (p, RISCV_FENCE_NONE, RISCV_FENCE_ALL);
		riscv_fence_i (p);

		// A

		riscv_lr_w (p, RISCV_ORDER_RL, RISCV_X0, RISCV_X1);
		riscv_sc_w (p, RISCV_ORDER_AQ, RISCV_X0, RISCV_X1, RISCV_X2);
		riscv_amoswap_w (p, RISCV_ORDER_ALL, RISCV_X0, RISCV_X1, RISCV_X2);
		riscv_amoadd_w (p, RISCV_ORDER_RL, RISCV_X0, RISCV_X1, RISCV_X2);
		riscv_amoxor_w (p, RISCV_ORDER_AQ, RISCV_X0, RISCV_X1, RISCV_X2);
		riscv_amoand_w (p, RISCV_ORDER_ALL, RISCV_X0, RISCV_X1, RISCV_X2);
		riscv_amoor_w (p, RISCV_ORDER_RL, RISCV_X0, RISCV_X1, RISCV_X2);
		riscv_amomin_w (p, RISCV_ORDER_AQ, RISCV_X0, RISCV_X1, RISCV_X2);
		riscv_amomax_w (p, RISCV_ORDER_ALL, RISCV_X0, RISCV_X1, RISCV_X2);
		riscv_amominu_w (p, RISCV_ORDER_RL, RISCV_X0, RISCV_X1, RISCV_X2);
		riscv_amomaxu_w (p, RISCV_ORDER_AQ, RISCV_X0, RISCV_X1, RISCV_X2);
#ifdef TARGET_RISCV64
		riscv_lr_d (p, RISCV_ORDER_RL, RISCV_X0, RISCV_X1);
		riscv_sc_d (p, RISCV_ORDER_AQ, RISCV_X0, RISCV_X1, RISCV_X2);
		riscv_amoswap_d (p, RISCV_ORDER_ALL, RISCV_X0, RISCV_X1, RISCV_X2);
		riscv_amoadd_d (p, RISCV_ORDER_RL, RISCV_X0, RISCV_X1, RISCV_X2);
		riscv_amoxor_d (p, RISCV_ORDER_AQ, RISCV_X0, RISCV_X1, RISCV_X2);
		riscv_amoand_d (p, RISCV_ORDER_ALL, RISCV_X0, RISCV_X1, RISCV_X2);
		riscv_amoor_d (p, RISCV_ORDER_RL, RISCV_X0, RISCV_X1, RISCV_X2);
		riscv_amomin_d (p, RISCV_ORDER_AQ, RISCV_X0, RISCV_X1, RISCV_X2);
		riscv_amomax_d (p, RISCV_ORDER_ALL, RISCV_X0, RISCV_X1, RISCV_X2);
		riscv_amominu_d (p, RISCV_ORDER_RL, RISCV_X0, RISCV_X1, RISCV_X2);
		riscv_amomaxu_d (p, RISCV_ORDER_AQ, RISCV_X0, RISCV_X1, RISCV_X2);
#endif
	}

	guint8 *p2 = code;

	do {
		printf (".byte %d\n", *p2);
	} while (++p2 != p);

	return 0;
}
