#include <mono/arch/arm64/arm64-codegen.h>
#include "glib.h"

int
main (int argc, char *argv [])
{
	guint8 buf [4096];
	guint8 *code;
	int i;

	code = buf;

	arm_nop (code);
	arm_brx (code, ARMREG_R1);
	arm_blrx (code, ARMREG_R1);
	arm_retx (code, ARMREG_R1);

	arm_nop (code);
	arm_b (code, code + 4);
	arm_b (code, code);
	arm_b (code, code - 4);
	arm_bl (code, code + 4);
	arm_bl (code, code);
	arm_bl (code, code - 4);

	arm_nop (code);
	arm_bcc (code, ARMCOND_NE, code + 4);
	arm_bcc (code, ARMCOND_NE, code);
	arm_bcc (code, ARMCOND_NE, code - 4);
	arm_cbzx (code, ARMREG_R1, code + 4);
	arm_cbzx (code, ARMREG_R1, code);
	arm_cbzx (code, ARMREG_R1, code - 4);
	arm_cbzw (code, ARMREG_R1, code + 4);
	arm_cbzw (code, ARMREG_R1, code);
	arm_cbzw (code, ARMREG_R1, code - 4);
	arm_cbnzx (code, ARMREG_R1, code + 4);
	arm_cbnzx (code, ARMREG_R1, code);
	arm_cbnzx (code, ARMREG_R1, code - 4);
	arm_cbnzw (code, ARMREG_R1, code + 4);
	arm_cbnzw (code, ARMREG_R1, code);
	arm_cbnzw (code, ARMREG_R1, code - 4);
	arm_tbz (code, ARMREG_R1, 1, code + 4);
	arm_tbz (code, ARMREG_R1, 1, code);
	arm_tbz (code, ARMREG_R1, 1, code - 4);
	arm_tbz (code, ARMREG_R1, 33, code + 4);
	arm_tbz (code, ARMREG_R1, 33, code);
	arm_tbz (code, ARMREG_R1, 33, code - 4);
	arm_tbnz (code, ARMREG_R1, 1, code + 4);
	arm_tbnz (code, ARMREG_R1, 1, code);
	arm_tbnz (code, ARMREG_R1, 1, code - 4);
	arm_tbnz (code, ARMREG_R1, 33, code + 4);
	arm_tbnz (code, ARMREG_R1, 33, code);
	arm_tbnz (code, ARMREG_R1, 33, code - 4);

	arm_nop (code);
	arm_ldrx (code, ARMREG_R1, ARMREG_R2, 0);
	arm_ldrx (code, ARMREG_R1, ARMREG_R2, 16);
	arm_ldrw (code, ARMREG_R1, ARMREG_R2, 0);
	arm_ldrw (code, ARMREG_R1, ARMREG_R2, 16);
	arm_ldrb (code, ARMREG_R1, ARMREG_R2, 0);
	arm_ldrb (code, ARMREG_R1, ARMREG_R2, 16);
	arm_ldrh (code, ARMREG_R1, ARMREG_R2, 0);
	arm_ldrh (code, ARMREG_R1, ARMREG_R2, 16);
	arm_ldrsbx (code, ARMREG_R1, ARMREG_R2, 0);
	arm_ldrsbx (code, ARMREG_R1, ARMREG_R2, 16);
	arm_ldrsbw (code, ARMREG_R1, ARMREG_R2, 0);
	arm_ldrsbw (code, ARMREG_R1, ARMREG_R2, 16);
	arm_ldrshx (code, ARMREG_R1, ARMREG_R2, 0);
	arm_ldrshx (code, ARMREG_R1, ARMREG_R2, 16);
	arm_ldrshw (code, ARMREG_R1, ARMREG_R2, 0);
	arm_ldrshw (code, ARMREG_R1, ARMREG_R2, 16);
	arm_ldrswx (code, ARMREG_R1, ARMREG_R2, 0);
	arm_ldrswx (code, ARMREG_R1, ARMREG_R2, 16);
	arm_strx (code, ARMREG_R1, ARMREG_R2, 0);
	arm_strx (code, ARMREG_R1, ARMREG_R2, 16);
	arm_strw (code, ARMREG_R1, ARMREG_R2, 0);
	arm_strw (code, ARMREG_R1, ARMREG_R2, 16);
	arm_strh (code, ARMREG_R1, ARMREG_R2, 0);
	arm_strh (code, ARMREG_R1, ARMREG_R2, 16);
	arm_strb (code, ARMREG_R1, ARMREG_R2, 0);
	arm_strb (code, ARMREG_R1, ARMREG_R2, 16);

	arm_nop (code);
	arm_ldrx_post (code, ARMREG_R1, ARMREG_R2, 0);
	arm_ldrx_post (code, ARMREG_R1, ARMREG_R2, 16);
	arm_ldrx_post (code, ARMREG_R1, ARMREG_R2, -16);
	arm_ldrw_post (code, ARMREG_R1, ARMREG_R2, 0);
	arm_ldrw_post (code, ARMREG_R1, ARMREG_R2, 16);
	arm_ldrw_post (code, ARMREG_R1, ARMREG_R2, -16);
	arm_strx_post (code, ARMREG_R1, ARMREG_R2, 0);
	arm_strx_post (code, ARMREG_R1, ARMREG_R2, 16);
	arm_strx_post (code, ARMREG_R1, ARMREG_R2, -16);
	arm_strw_post (code, ARMREG_R1, ARMREG_R2, 0);
	arm_strw_post (code, ARMREG_R1, ARMREG_R2, 16);
	arm_strw_post (code, ARMREG_R1, ARMREG_R2, -16);

	arm_nop (code);
	arm_ldrx_pre (code, ARMREG_R1, ARMREG_R2, 0);
	arm_ldrx_pre (code, ARMREG_R1, ARMREG_R2, 16);
	arm_ldrx_pre (code, ARMREG_R1, ARMREG_R2, -16);
	arm_ldrw_pre (code, ARMREG_R1, ARMREG_R2, 0);
	arm_ldrw_pre (code, ARMREG_R1, ARMREG_R2, 16);
	arm_ldrw_pre (code, ARMREG_R1, ARMREG_R2, -16);
	arm_strx_pre (code, ARMREG_R1, ARMREG_R2, 0);
	arm_strx_pre (code, ARMREG_R1, ARMREG_R2, 16);
	arm_strx_pre (code, ARMREG_R1, ARMREG_R2, -16);
	arm_strw_pre (code, ARMREG_R1, ARMREG_R2, 0);
	arm_strw_pre (code, ARMREG_R1, ARMREG_R2, 16);
	arm_strw_pre (code, ARMREG_R1, ARMREG_R2, -16);

	arm_nop (code);
	arm_ldrx_reg (code, ARMREG_R1, ARMREG_R2, ARMREG_R3);
	arm_ldrw_reg (code, ARMREG_R1, ARMREG_R2, ARMREG_R3);
	arm_ldrb_reg (code, ARMREG_R1, ARMREG_R2, ARMREG_R3);
	arm_ldrh_reg (code, ARMREG_R1, ARMREG_R2, ARMREG_R3);
	arm_ldrsbx_reg (code, ARMREG_R1, ARMREG_R2, ARMREG_R3);
	arm_ldrsbw_reg (code, ARMREG_R1, ARMREG_R2, ARMREG_R3);
	arm_ldrshx_reg (code, ARMREG_R1, ARMREG_R2, ARMREG_R3);
	arm_ldrshw_reg (code, ARMREG_R1, ARMREG_R2, ARMREG_R3);
	arm_ldrswx_reg (code, ARMREG_R1, ARMREG_R2, ARMREG_R3);
	arm_strx_reg (code, ARMREG_R1, ARMREG_R2, ARMREG_R3);
	arm_strw_reg (code, ARMREG_R1, ARMREG_R2, ARMREG_R3);
	arm_strh_reg (code, ARMREG_R1, ARMREG_R2, ARMREG_R3);
	arm_strb_reg (code, ARMREG_R1, ARMREG_R2, ARMREG_R3);

	arm_nop (code);
	arm_ldrx_lit (code, ARMREG_R1, code + 4);
	arm_ldrx_lit (code, ARMREG_R1, code);
	arm_ldrx_lit (code, ARMREG_R1, code - 4);
	arm_ldrw_lit (code, ARMREG_R1, code + 4);
	arm_ldrw_lit (code, ARMREG_R1, code);
	arm_ldrw_lit (code, ARMREG_R1, code - 4);
	arm_ldrswx_lit (code, ARMREG_R1, code + 4);
	arm_ldrswx_lit (code, ARMREG_R1, code);
	arm_ldrswx_lit (code, ARMREG_R1, code - 4);

	arm_nop (code);
	arm_ldpx (code, ARMREG_R1, ARMREG_R2, ARMREG_R3, 0);
	arm_ldpx (code, ARMREG_R1, ARMREG_R2, ARMREG_R3, 16);
	arm_ldpx (code, ARMREG_R1, ARMREG_R2, ARMREG_R3, -16);
	arm_ldpw (code, ARMREG_R1, ARMREG_R2, ARMREG_R3, 0);
	arm_ldpw (code, ARMREG_R1, ARMREG_R2, ARMREG_R3, 16);
	arm_ldpw (code, ARMREG_R1, ARMREG_R2, ARMREG_R3, -16);
	arm_ldpsw (code, ARMREG_R1, ARMREG_R2, ARMREG_R3, 0);
	arm_ldpsw (code, ARMREG_R1, ARMREG_R2, ARMREG_R3, 16);
	arm_ldpsw (code, ARMREG_R1, ARMREG_R2, ARMREG_R3, -16);
	arm_stpx (code, ARMREG_R1, ARMREG_R2, ARMREG_R3, 0);
	arm_stpx (code, ARMREG_R1, ARMREG_R2, ARMREG_R3, 16);
	arm_stpx (code, ARMREG_R1, ARMREG_R2, ARMREG_R3, -16);
	arm_stpw (code, ARMREG_R1, ARMREG_R2, ARMREG_R3, 0);
	arm_stpw (code, ARMREG_R1, ARMREG_R2, ARMREG_R3, 16);
	arm_stpw (code, ARMREG_R1, ARMREG_R2, ARMREG_R3, -16);

	arm_nop (code);
	arm_ldpx_pre (code, ARMREG_R1, ARMREG_R2, ARMREG_R3, 0);
	arm_ldpx_pre (code, ARMREG_R1, ARMREG_R2, ARMREG_R3, 16);
	arm_ldpx_pre (code, ARMREG_R1, ARMREG_R2, ARMREG_R3, -16);
	arm_ldpw_pre (code, ARMREG_R1, ARMREG_R2, ARMREG_R3, 0);
	arm_ldpw_pre (code, ARMREG_R1, ARMREG_R2, ARMREG_R3, 16);
	arm_ldpw_pre (code, ARMREG_R1, ARMREG_R2, ARMREG_R3, -16);
	arm_ldpsw_pre (code, ARMREG_R1, ARMREG_R2, ARMREG_R3, 0);
	arm_ldpsw_pre (code, ARMREG_R1, ARMREG_R2, ARMREG_R3, 16);
	arm_ldpsw_pre (code, ARMREG_R1, ARMREG_R2, ARMREG_R3, -16);

	arm_nop (code);
	arm_ldpx_post (code, ARMREG_R1, ARMREG_R2, ARMREG_R3, 0);
	arm_ldpx_post (code, ARMREG_R1, ARMREG_R2, ARMREG_R3, 16);
	arm_ldpx_post (code, ARMREG_R1, ARMREG_R2, ARMREG_R3, -16);
	arm_ldpw_post (code, ARMREG_R1, ARMREG_R2, ARMREG_R3, 0);
	arm_ldpw_post (code, ARMREG_R1, ARMREG_R2, ARMREG_R3, 16);
	arm_ldpw_post (code, ARMREG_R1, ARMREG_R2, ARMREG_R3, -16);
	arm_ldpsw_post (code, ARMREG_R1, ARMREG_R2, ARMREG_R3, 0);
	arm_ldpsw_post (code, ARMREG_R1, ARMREG_R2, ARMREG_R3, 16);
	arm_ldpsw_post (code, ARMREG_R1, ARMREG_R2, ARMREG_R3, -16);

	arm_nop (code);
	arm_ldxrx (code, ARMREG_R1, ARMREG_R2);
	arm_ldxrw (code, ARMREG_R1, ARMREG_R2);
	arm_ldxrh (code, ARMREG_R1, ARMREG_R2);
	arm_ldxrb (code, ARMREG_R1, ARMREG_R2);
	arm_ldxpx (code, ARMREG_R1, ARMREG_R2, ARMREG_R3);
	arm_ldxpw (code, ARMREG_R1, ARMREG_R2, ARMREG_R3);
	arm_stxrx (code, ARMREG_R1, ARMREG_R2, ARMREG_R3);
	arm_stxrw (code, ARMREG_R1, ARMREG_R2, ARMREG_R3);
	arm_stxrh (code, ARMREG_R1, ARMREG_R2, ARMREG_R3);
	arm_stxrb (code, ARMREG_R1, ARMREG_R2, ARMREG_R3);
	arm_stxpx (code, ARMREG_R1, ARMREG_R2, ARMREG_R3, ARMREG_R4);
	arm_stxpw (code, ARMREG_R1, ARMREG_R2, ARMREG_R3, ARMREG_R4);

	// fixme: immeditate tests
	arm_nop (code);
	arm_addx_imm (code, ARMREG_R1, ARMREG_R2, 16);
	arm_addw_imm (code, ARMREG_R1, ARMREG_R2, 16);
	arm_addsx_imm (code, ARMREG_R1, ARMREG_R2, 16);
	arm_addsw_imm (code, ARMREG_R1, ARMREG_R2, 16);
	arm_subx_imm (code, ARMREG_R1, ARMREG_R2, 16);
	arm_subw_imm (code, ARMREG_R1, ARMREG_R2, 16);
	arm_subsx_imm (code, ARMREG_R1, ARMREG_R2, 16);
	arm_subsw_imm (code, ARMREG_R1, ARMREG_R2, 16);
	arm_cmpx_imm (code, ARMREG_R1, 16);
	arm_cmpw_imm (code, ARMREG_R1, 16);
	arm_cmnx_imm (code, ARMREG_R1, 16);
	arm_cmnw_imm (code, ARMREG_R1, 16);

#if 0
	// fixme: bitmasks
	// fixme: bitmask tests
	arm_nop (code);
	arm_andx_imm (code, ARMREG_R1, ARMREG_R2, 1);
	arm_andw_imm (code, ARMREG_R1, ARMREG_R2, 1);
	arm_andsx_imm (code, ARMREG_R1, ARMREG_R2, 1);
	arm_andsw_imm (code, ARMREG_R1, ARMREG_R2, 1);
	arm_eorx_imm (code, ARMREG_R1, ARMREG_R2, 1);
	arm_eorw_imm (code, ARMREG_R1, ARMREG_R2, 1);
	arm_orrx_imm (code, ARMREG_R1, ARMREG_R2, 1);
	arm_orrw_imm (code, ARMREG_R1, ARMREG_R2, 1);
	arm_tstx_imm (code, ARMREG_R1, 1);
	arm_tstw_imm (code, ARMREG_R1, 1);
#endif

	arm_nop (code);
	arm_movzx (code, ARMREG_R1, 16, 0);
	arm_movzx (code, ARMREG_R1, 16, 16);
	arm_movzx (code, ARMREG_R1, 16, 32);
	arm_movzx (code, ARMREG_R1, 16, 48);
	arm_movzw (code, ARMREG_R1, 16, 0);
	arm_movzw (code, ARMREG_R1, 16, 16);
	arm_movzw (code, ARMREG_R1, 16, 32);
	arm_movzw (code, ARMREG_R1, 16, 48);
	arm_movnx (code, ARMREG_R1, 16, 0);
	arm_movnx (code, ARMREG_R1, 16, 16);
	arm_movnx (code, ARMREG_R1, 16, 32);
	arm_movnx (code, ARMREG_R1, 16, 48);
	arm_movnw (code, ARMREG_R1, 16, 0);
	arm_movnw (code, ARMREG_R1, 16, 16);
	arm_movnw (code, ARMREG_R1, 16, 32);
	arm_movnw (code, ARMREG_R1, 16, 48);
	arm_movkx (code, ARMREG_R1, 16, 0);
	arm_movkx (code, ARMREG_R1, 16, 16);
	arm_movkx (code, ARMREG_R1, 16, 32);
	arm_movkx (code, ARMREG_R1, 16, 48);
	arm_movkw (code, ARMREG_R1, 16, 0);
	arm_movkw (code, ARMREG_R1, 16, 16);
	arm_movkw (code, ARMREG_R1, 16, 32);
	arm_movkw (code, ARMREG_R1, 16, 48);

	arm_nop (code);
	arm_adrpx (code, ARMREG_R1, code);
	arm_adrx (code, ARMREG_R1, code + 4);
	arm_adrx (code, ARMREG_R1, code + 16);
	arm_adrx (code, ARMREG_R1, code);
	arm_adrx (code, ARMREG_R1, code - 4);

	// fixme: bitfield encodings
	arm_nop (code);
	arm_bfmx (code, ARMREG_R1, ARMREG_R2, 0, 5);
	arm_bfmw (code, ARMREG_R1, ARMREG_R2, 0, 5);
	arm_sbfmx (code, ARMREG_R1, ARMREG_R2, 0, 5);
	arm_sbfmw (code, ARMREG_R1, ARMREG_R2, 0, 5);
	arm_ubfmx (code, ARMREG_R1, ARMREG_R2, 0, 5);
	arm_ubfmw (code, ARMREG_R1, ARMREG_R2, 0, 5);
	arm_asrx (code, ARMREG_R1, ARMREG_R2, 11);
	arm_asrw (code, ARMREG_R1, ARMREG_R2, 11);
	arm_sxtbx (code, ARMREG_R1, ARMREG_R2);
	arm_sxtbw (code, ARMREG_R1, ARMREG_R2);
	arm_sxthx (code, ARMREG_R1, ARMREG_R2);
	arm_sxthw (code, ARMREG_R1, ARMREG_R2);
	arm_sxtwx (code, ARMREG_R1, ARMREG_R2);
	arm_uxtbw (code, ARMREG_R1, ARMREG_R2);
	arm_uxthw (code, ARMREG_R1, ARMREG_R2);
	arm_lslx (code, ARMREG_R1, ARMREG_R2, 16);
	arm_lslw (code, ARMREG_R1, ARMREG_R2, 16);
	arm_lsrx (code, ARMREG_R1, ARMREG_R2, 16);
	arm_lsrw (code, ARMREG_R1, ARMREG_R2, 16);
	arm_extrx (code, ARMREG_R1, ARMREG_R2, ARMREG_R3, 16);
	arm_extrw (code, ARMREG_R1, ARMREG_R2, ARMREG_R3, 16);
	arm_rorx (code, ARMREG_R1, ARMREG_R2, 16);
	arm_rorw (code, ARMREG_R1, ARMREG_R2, 16);

	arm_nop (code);
	arm_adcx (code, ARMREG_R1, ARMREG_R2, ARMREG_R3);
	arm_adcw (code, ARMREG_R1, ARMREG_R2, ARMREG_R3);
	arm_adcsx (code, ARMREG_R1, ARMREG_R2, ARMREG_R3);
	arm_adcsw (code, ARMREG_R1, ARMREG_R2, ARMREG_R3);
	arm_sbcx (code, ARMREG_R1, ARMREG_R2, ARMREG_R3);
	arm_sbcw (code, ARMREG_R1, ARMREG_R2, ARMREG_R3);
	arm_sbcsx (code, ARMREG_R1, ARMREG_R2, ARMREG_R3);
	arm_sbcsw (code, ARMREG_R1, ARMREG_R2, ARMREG_R3);
	arm_ngcx (code, ARMREG_R1, ARMREG_R2);
	arm_ngcw (code, ARMREG_R1, ARMREG_R2);
	arm_ngcsx (code, ARMREG_R1, ARMREG_R2);
	arm_ngcsw (code, ARMREG_R1, ARMREG_R2);

	arm_nop (code);
	arm_addx_shift (code, ARMREG_R1, ARMREG_R2, ARMREG_R3, ARMSHIFT_LSL, 4);
	arm_addx_shift (code, ARMREG_R1, ARMREG_R2, ARMREG_R3, ARMSHIFT_LSR, 4);
	arm_addx_shift (code, ARMREG_R1, ARMREG_R2, ARMREG_R3, ARMSHIFT_ASR, 4);
	arm_addw_shift (code, ARMREG_R1, ARMREG_R2, ARMREG_R3, ARMSHIFT_ASR, 4);
	arm_addsx_shift (code, ARMREG_R1, ARMREG_R2, ARMREG_R3, ARMSHIFT_ASR, 4);
	arm_addsw_shift (code, ARMREG_R1, ARMREG_R2, ARMREG_R3, ARMSHIFT_ASR, 4);
	arm_subx_shift (code, ARMREG_R1, ARMREG_R2, ARMREG_R3, ARMSHIFT_ASR, 4);
	arm_subw_shift (code, ARMREG_R1, ARMREG_R2, ARMREG_R3, ARMSHIFT_ASR, 4);
	arm_subsx_shift (code, ARMREG_R1, ARMREG_R2, ARMREG_R3, ARMSHIFT_ASR, 4);
	arm_subsw_shift (code, ARMREG_R1, ARMREG_R2, ARMREG_R3, ARMSHIFT_ASR, 4);
	arm_cmpx_shift (code, ARMREG_R1, ARMREG_R2, ARMSHIFT_ASR, 4);
	arm_cmpw_shift (code, ARMREG_R1, ARMREG_R2, ARMSHIFT_ASR, 4);
	arm_cmnx_shift (code, ARMREG_R1, ARMREG_R2, ARMSHIFT_ASR, 4);
	arm_cmnw_shift (code, ARMREG_R1, ARMREG_R2, ARMSHIFT_ASR, 4);
	arm_negx_shift (code, ARMREG_R1, ARMREG_R2, ARMSHIFT_ASR, 4);
	arm_negw_shift (code, ARMREG_R1, ARMREG_R2, ARMSHIFT_ASR, 4);
	arm_negsx_shift (code, ARMREG_R1, ARMREG_R2, ARMSHIFT_ASR, 4);
	arm_negsw_shift (code, ARMREG_R1, ARMREG_R2, ARMSHIFT_ASR, 4);

	arm_nop (code);
	arm_andx_shift (code, ARMREG_R1, ARMREG_R2, ARMREG_R3, ARMSHIFT_LSL, 4);
	arm_andw_shift (code, ARMREG_R1, ARMREG_R2, ARMREG_R3, ARMSHIFT_LSL, 4);
	arm_andsx_shift (code, ARMREG_R1, ARMREG_R2, ARMREG_R3, ARMSHIFT_LSL, 4);
	arm_andsw_shift (code, ARMREG_R1, ARMREG_R2, ARMREG_R3, ARMSHIFT_LSL, 4);
	arm_bicx_shift (code, ARMREG_R1, ARMREG_R2, ARMREG_R3, ARMSHIFT_LSL, 4);
	arm_bicw_shift (code, ARMREG_R1, ARMREG_R2, ARMREG_R3, ARMSHIFT_LSL, 4);
	arm_bicsx_shift (code, ARMREG_R1, ARMREG_R2, ARMREG_R3, ARMSHIFT_LSL, 4);
	arm_bicsw_shift (code, ARMREG_R1, ARMREG_R2, ARMREG_R3, ARMSHIFT_LSL, 4);
	arm_eonx_shift (code, ARMREG_R1, ARMREG_R2, ARMREG_R3, ARMSHIFT_LSL, 4);
	arm_eonw_shift (code, ARMREG_R1, ARMREG_R2, ARMREG_R3, ARMSHIFT_LSL, 4);
	arm_eorx_shift (code, ARMREG_R1, ARMREG_R2, ARMREG_R3, ARMSHIFT_LSL, 4);
	arm_eorw_shift (code, ARMREG_R1, ARMREG_R2, ARMREG_R3, ARMSHIFT_LSL, 4);
	arm_orrx_shift (code, ARMREG_R1, ARMREG_R2, ARMREG_R3, ARMSHIFT_LSL, 4);
	arm_orrw_shift (code, ARMREG_R1, ARMREG_R2, ARMREG_R3, ARMSHIFT_LSL, 4);
	arm_ornx_shift (code, ARMREG_R1, ARMREG_R2, ARMREG_R3, ARMSHIFT_LSL, 4);
	arm_ornw_shift (code, ARMREG_R1, ARMREG_R2, ARMREG_R3, ARMSHIFT_LSL, 4);
	arm_mvnx_shift (code, ARMREG_R1, ARMREG_R2, ARMSHIFT_LSL, 4);
	arm_mvnw_shift (code, ARMREG_R1, ARMREG_R2, ARMSHIFT_LSL, 4);
	arm_tstx_shift (code, ARMREG_R1, ARMREG_R2, ARMSHIFT_LSL, 4);
	arm_tstw_shift (code, ARMREG_R1, ARMREG_R2, ARMSHIFT_LSL, 4);

	arm_nop (code);
	arm_movx (code, ARMREG_R1, ARMREG_R2);
	arm_movw (code, ARMREG_R1, ARMREG_R2);

	arm_nop (code);
	arm_asrvx (code, ARMREG_R1, ARMREG_R2, ARMREG_R3);
	arm_asrvw (code, ARMREG_R1, ARMREG_R2, ARMREG_R3);
	arm_lslvx (code, ARMREG_R1, ARMREG_R2, ARMREG_R3);
	arm_lslvw (code, ARMREG_R1, ARMREG_R2, ARMREG_R3);
	arm_lsrvx (code, ARMREG_R1, ARMREG_R2, ARMREG_R3);
	arm_lsrvw (code, ARMREG_R1, ARMREG_R2, ARMREG_R3);
	arm_rorvx (code, ARMREG_R1, ARMREG_R2, ARMREG_R3);
	arm_rorvw (code, ARMREG_R1, ARMREG_R2, ARMREG_R3);

	arm_nop (code);
	arm_sdivx (code, ARMREG_R1, ARMREG_R2, ARMREG_R3);
	arm_sdivw (code, ARMREG_R1, ARMREG_R2, ARMREG_R3);
	arm_udivx (code, ARMREG_R1, ARMREG_R2, ARMREG_R3);
	arm_udivw (code, ARMREG_R1, ARMREG_R2, ARMREG_R3);

	arm_nop (code);
	arm_cselx (code, ARMCOND_NE, ARMREG_R1, ARMREG_R2, ARMREG_R3);
	arm_cselw (code, ARMCOND_NE, ARMREG_R1, ARMREG_R2, ARMREG_R3);
	arm_csincx (code, ARMCOND_NE, ARMREG_R1, ARMREG_R2, ARMREG_R3);
	arm_csincw (code, ARMCOND_NE, ARMREG_R1, ARMREG_R2, ARMREG_R3);
	arm_csinvx (code, ARMCOND_NE, ARMREG_R1, ARMREG_R2, ARMREG_R3);
	arm_csinvw (code, ARMCOND_NE, ARMREG_R1, ARMREG_R2, ARMREG_R3);
	arm_csnegx (code, ARMCOND_NE, ARMREG_R1, ARMREG_R2, ARMREG_R3);
	arm_csnegw (code, ARMCOND_NE, ARMREG_R1, ARMREG_R2, ARMREG_R3);

	arm_brk (code, 0x1);

	arm_maddx (code, ARMREG_R0, ARMREG_R1, ARMREG_R2, ARMREG_R3);
	arm_maddw (code, ARMREG_R0, ARMREG_R1, ARMREG_R2, ARMREG_R3);
	arm_msubx (code, ARMREG_R0, ARMREG_R1, ARMREG_R2, ARMREG_R3);
	arm_msubw (code, ARMREG_R0, ARMREG_R1, ARMREG_R2, ARMREG_R3);
	arm_mnegx (code, ARMREG_R0, ARMREG_R1, ARMREG_R2);
	arm_mnegw (code, ARMREG_R0, ARMREG_R1, ARMREG_R2);


	arm_nop (code);
	arm_fmovd (code, ARMREG_D1, ARMREG_D2);
	arm_fmov_rx_to_double (code, ARMREG_D1, ARMREG_R2);
	arm_strfpx (code, ARMREG_D1, ARMREG_R2, 16);
	arm_ldrfpx (code, ARMREG_D1, ARMREG_R2, 16);
	arm_strfpw (code, ARMREG_D1, ARMREG_R2, 16);
	arm_ldrfpw (code, ARMREG_D1, ARMREG_R2, 16);
	arm_fcmpd (code, ARMREG_D1, ARMREG_D2);
	arm_fcvtzs_dx (code, ARMREG_R1, ARMREG_D2);
	arm_fcvtzs_dw (code, ARMREG_R1, ARMREG_D2);
	arm_fcvtzu_dx (code, ARMREG_R1, ARMREG_D2);
	arm_fcvtzu_dw (code, ARMREG_R1, ARMREG_D2);
	arm_fcvt_sd (code, ARMREG_D1, ARMREG_D2);
	arm_fcvt_ds (code, ARMREG_D1, ARMREG_D2);
	arm_scvtf_d (code, ARMREG_D1, ARMREG_D2);
	arm_scvtf_s (code, ARMREG_D1, ARMREG_D2);
	arm_scvtf_rx_to_d (code, ARMREG_D1, ARMREG_R2);
	arm_scvtf_rw_to_d (code, ARMREG_D1, ARMREG_R2);
	arm_ucvtf_d (code, ARMREG_D1, ARMREG_D2);
	arm_ucvtf_s (code, ARMREG_D1, ARMREG_D2);
	arm_ucvtf_rx_to_d (code, ARMREG_D1, ARMREG_R2);
	arm_ucvtf_rw_to_d (code, ARMREG_D1, ARMREG_R2);
	arm_fadd_d (code, ARMREG_D1, ARMREG_D2, ARMREG_D3);
	arm_fsub_d (code, ARMREG_D1, ARMREG_D2, ARMREG_D3);
	arm_fmul_d (code, ARMREG_D1, ARMREG_D2, ARMREG_D3);
	arm_fdiv_d (code, ARMREG_D1, ARMREG_D2, ARMREG_D3);
	arm_fmsub_d (code, ARMREG_D1, ARMREG_D2, ARMREG_D3, ARMREG_D4);
	arm_fneg_d (code, ARMREG_D1, ARMREG_D2);
	arm_fabs_d (code, ARMREG_D1, ARMREG_D2);

	arm_nop (code);
	arm_dmb (code, 0x0);
	arm_dmb (code, 0xd);

	arm_nop (code);
	arm_mrs (code, ARMREG_R1, ARM_MRS_REG_TPIDR_EL0);

	arm_nop (code);
	arm_ldaxrx (code, ARMREG_R0, ARMREG_R1);
	arm_ldaxrw (code, ARMREG_R0, ARMREG_R1);
	arm_stlxrx (code, ARMREG_R0, ARMREG_R1, ARMREG_R2);
	arm_stlxrw (code, ARMREG_R0, ARMREG_R1, ARMREG_R2);

	arm_paciasp (code);
	arm_pacibsp (code);
	arm_retaa (code);
	arm_retab (code);
	arm_braaz (code, ARMREG_R1);
	arm_brabz (code, ARMREG_R1);
	arm_braa (code, ARMREG_R1, ARMREG_R2);
	arm_brab (code, ARMREG_R1, ARMREG_R2);
	arm_blraaz (code, ARMREG_R1);
	arm_blraa (code, ARMREG_R1, ARMREG_R2);
	arm_blrabz (code, ARMREG_R1);
	arm_blrab (code, ARMREG_R1, ARMREG_R2);

	// neon int 3-reg same type
	arm_neon_add (code, VREG_FULL, TYPE_I8, ARMREG_R0, ARMREG_R1, ARMREG_R2);
	arm_neon_sub (code, VREG_FULL, TYPE_I8, ARMREG_R0, ARMREG_R1, ARMREG_R2);
	arm_neon_mul (code, VREG_FULL, TYPE_I8, ARMREG_R0, ARMREG_R1, ARMREG_R2);
	arm_neon_smax (code, VREG_FULL, TYPE_I8, ARMREG_R0, ARMREG_R1, ARMREG_R2);
	arm_neon_smin (code, VREG_FULL, TYPE_I8, ARMREG_R0, ARMREG_R1, ARMREG_R2);
	arm_neon_umax (code, VREG_FULL, TYPE_I8, ARMREG_R0, ARMREG_R1, ARMREG_R2);
	arm_neon_umin (code, VREG_FULL, TYPE_I8, ARMREG_R0, ARMREG_R1, ARMREG_R2);
	arm_neon_cmgt (code, VREG_FULL, TYPE_I8, ARMREG_R0, ARMREG_R1, ARMREG_R2);
	arm_neon_cmge (code, VREG_FULL, TYPE_I8, ARMREG_R0, ARMREG_R1, ARMREG_R2);
	arm_neon_cmeq (code, VREG_FULL, TYPE_I8, ARMREG_R0, ARMREG_R1, ARMREG_R2);
	arm_neon_cmhi (code, VREG_FULL, TYPE_I8, ARMREG_R0, ARMREG_R1, ARMREG_R2);
	arm_neon_cmhs (code, VREG_FULL, TYPE_I8, ARMREG_R0, ARMREG_R1, ARMREG_R2);

	// neon float 3-reg same type
	arm_neon_fadd (code, VREG_FULL, TYPE_F32, ARMREG_R0, ARMREG_R1, ARMREG_R2);
	arm_neon_fsub (code, VREG_FULL, TYPE_F32, ARMREG_R0, ARMREG_R1, ARMREG_R2);
	arm_neon_fmax (code, VREG_FULL, TYPE_F32, ARMREG_R0, ARMREG_R1, ARMREG_R2);
	arm_neon_fmin (code, VREG_FULL, TYPE_F32, ARMREG_R0, ARMREG_R1, ARMREG_R2);
	arm_neon_fmul (code, VREG_FULL, TYPE_F32, ARMREG_R0, ARMREG_R1, ARMREG_R2);
	arm_neon_fdiv (code, VREG_FULL, TYPE_F32, ARMREG_R0, ARMREG_R1, ARMREG_R2);
	arm_neon_fcmeq (code, VREG_FULL, TYPE_F32, ARMREG_R0, ARMREG_R1, ARMREG_R2);
	arm_neon_fcmge (code, VREG_FULL, TYPE_F32, ARMREG_R0, ARMREG_R1, ARMREG_R2);
	arm_neon_fcmgt (code, VREG_FULL, TYPE_F32, ARMREG_R0, ARMREG_R1, ARMREG_R2);

	// neon bitwise 3-reg
	arm_neon_and (code, VREG_FULL, ARMREG_R0, ARMREG_R1, ARMREG_R2);
	arm_neon_orr (code, VREG_FULL, ARMREG_R0, ARMREG_R1, ARMREG_R2);
	arm_neon_eor (code, VREG_FULL, ARMREG_R0, ARMREG_R1, ARMREG_R2);

	// neon int 2-reg
	arm_neon_abs (code, VREG_FULL, TYPE_I8, ARMREG_R0, ARMREG_R1);
	arm_neon_neg (code, VREG_FULL, TYPE_I8, ARMREG_R0, ARMREG_R1);

	// neon float 2-reg
	arm_neon_fabs (code, VREG_FULL, TYPE_F32, ARMREG_R0, ARMREG_R1);
	arm_neon_fneg (code, VREG_FULL, TYPE_F32, ARMREG_R0, ARMREG_R1);
	arm_neon_fsqrt (code, VREG_FULL, TYPE_F32, ARMREG_R0, ARMREG_R1);

	// neon bitwise 2-reg
	arm_neon_not (code, VREG_FULL, ARMREG_R0, ARMREG_R1); // aliased to mvn

	// neon copy
	arm_neon_ins_g (code, TYPE_I8, ARMREG_R0, ARMREG_R1, 3); // insert w1 into v0.b[3]
	arm_neon_ins_g (code, TYPE_I32, ARMREG_R0, ARMREG_R1, 1);
	arm_neon_ins_e (code, TYPE_I8, ARMREG_R0, ARMREG_R1, 1, 5); // insert v1.b[5] into v0.b[1]
	arm_neon_ins_e (code, TYPE_I32, ARMREG_R0, ARMREG_R1, 1, 2); // insert v1.s[2] into v0.s[1]

	// pairwise and horizontal adds
	arm_neon_addv (code, VREG_FULL, TYPE_I8, ARMREG_R0, ARMREG_R1);
	arm_neon_addp (code, VREG_FULL, TYPE_I8, ARMREG_R0, ARMREG_R1, ARMREG_R2);
	arm_neon_faddp (code, VREG_FULL, TYPE_F32, ARMREG_R0, ARMREG_R1, ARMREG_R2);

	for (i = 0; i < code - buf; ++i)
		printf (".byte %d\n", buf [i]);
	printf ("\n");

	return 0;
}
