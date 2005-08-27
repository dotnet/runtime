/*
 * codegen.c: Tests for the IA64 code generation macros
 */

#include <glib.h>
#include <stdio.h>
#include <ctype.h>

#define IA64_SIMPLE_EMIT_BUNDLE

#include <mono/arch/ia64/ia64-codegen.h>

void
mono_disassemble_code (guint8 *code, int size, char *id)
{
	int i;
	FILE *ofd;
	const char *tmp = g_get_tmp_dir ();
	const char *objdump_args = g_getenv ("MONO_OBJDUMP_ARGS");
	char *as_file;
	char *o_file;
	char *cmd;
	
	as_file = g_strdup_printf ("%s/test.s", tmp);    

	if (!(ofd = fopen (as_file, "w")))
		g_assert_not_reached ();

	for (i = 0; id [i]; ++i) {
		if (!isalnum (id [i]))
			fprintf (ofd, "_");
		else
			fprintf (ofd, "%c", id [i]);
	}
	fprintf (ofd, ":\n");

	for (i = 0; i < size; ++i) 
		fprintf (ofd, ".byte %d\n", (unsigned int) code [i]);

	fclose (ofd);

#ifdef __ia64__
#define DIS_CMD "objdump -d"
#define AS_CMD "as"
#else
#define DIS_CMD "ia64-linux-gnu-objdump -d"
#define AS_CMD "ia64-linux-gnu-as"
#endif

	o_file = g_strdup_printf ("%s/test.o", tmp);    
	cmd = g_strdup_printf (AS_CMD " %s -o %s", as_file, o_file);
	system (cmd); 
	g_free (cmd);
	if (!objdump_args)
		objdump_args = "";
	
	cmd = g_strdup_printf (DIS_CMD " %s %s", objdump_args, o_file);
	system (cmd);
	g_free (cmd);
	
	g_free (o_file);
	g_free (as_file);
}

int
main ()
{
	Ia64CodegenState code;

	guint8 *buf = g_malloc0 (40960);

	ia64_codegen_init (code, buf);

	ia64_add (code, 1, 2, 3);
	ia64_add1 (code, 1, 2, 3);
	ia64_sub (code, 1, 2, 3);
	ia64_sub1 (code, 1, 2, 3);
	ia64_addp4 (code, 1, 2, 3);
	ia64_and (code, 1, 2, 3);
	ia64_andcm (code, 1, 2, 3);
	ia64_or (code, 1, 2, 3);
	ia64_xor (code, 1, 2, 3);
	ia64_shladd (code, 1, 2, 3, 4);
	ia64_shladdp4 (code, 1, 2, 3, 4);
	ia64_sub_imm (code, 1, 0x7f, 2);
	ia64_sub_imm (code, 1, -1, 2);
	ia64_and_imm (code, 1, -128, 2);
	ia64_andcm_imm (code, 1, -128, 2);
	ia64_or_imm (code, 1, -128, 2);
	ia64_xor_imm (code, 1, -128, 2);
	ia64_adds_imm (code, 1, 8191, 2);
	ia64_adds_imm (code, 1, -8192, 2);
	ia64_adds_imm (code, 1, 1234, 2);
	ia64_adds_imm (code, 1, -1234, 2);
	ia64_addp4_imm (code, 1, -1234, 2);
	ia64_addl_imm (code, 1, 1234, 2);
	ia64_addl_imm (code, 1, -1234, 2);
	ia64_addl_imm (code, 1, 2097151, 2);
	ia64_addl_imm (code, 1, -2097152, 2);

	ia64_cmp_lt (code, 1, 2, 1, 2);
	ia64_cmp_ltu (code, 1, 2, 1, 2);
	ia64_cmp_eq (code, 1, 2, 1, 2);
	ia64_cmp_lt_unc (code, 1, 2, 1, 2);
	ia64_cmp_ltu_unc (code, 1, 2, 1, 2);
	ia64_cmp_eq_unc (code, 1, 2, 1, 2);
	ia64_cmp_eq_and (code, 1, 2, 1, 2);
	ia64_cmp_eq_or (code, 1, 2, 1, 2);
	ia64_cmp_eq_or_andcm (code, 1, 2, 1, 2);
	ia64_cmp_ne_and (code, 1, 2, 1, 2);
	ia64_cmp_ne_or (code, 1, 2, 1, 2);
	ia64_cmp_ne_or_andcm (code, 1, 2, 1, 2);

	ia64_cmp4_lt (code, 1, 2, 1, 2);
	ia64_cmp4_ltu (code, 1, 2, 1, 2);
	ia64_cmp4_eq (code, 1, 2, 1, 2);
	ia64_cmp4_lt_unc (code, 1, 2, 1, 2);
	ia64_cmp4_ltu_unc (code, 1, 2, 1, 2);
	ia64_cmp4_eq_unc (code, 1, 2, 1, 2);
	ia64_cmp4_eq_and (code, 1, 2, 1, 2);
	ia64_cmp4_eq_or (code, 1, 2, 1, 2);
	ia64_cmp4_eq_or_andcm (code, 1, 2, 1, 2);
	ia64_cmp4_ne_and (code, 1, 2, 1, 2);
	ia64_cmp4_ne_or (code, 1, 2, 1, 2);
	ia64_cmp4_ne_or_andcm (code, 1, 2, 1, 2);

	ia64_cmp_gt_and (code, 1, 2, 0, 2);
	ia64_cmp_gt_or (code, 1, 2, 0, 2);
	ia64_cmp_gt_or_andcm (code, 1, 2, 0, 2);
	ia64_cmp_le_and (code, 1, 2, 0, 2);
	ia64_cmp_le_or (code, 1, 2, 0, 2);
	ia64_cmp_le_or_andcm (code, 1, 2, 0, 2);
	ia64_cmp_ge_and (code, 1, 2, 0, 2);
	ia64_cmp_ge_or (code, 1, 2, 0, 2);
	ia64_cmp_ge_or_andcm (code, 1, 2, 0, 2);
	ia64_cmp_lt_and (code, 1, 2, 0, 2);
	ia64_cmp_lt_or (code, 1, 2, 0, 2);
	ia64_cmp_lt_or_andcm (code, 1, 2, 0, 2);

	ia64_cmp4_gt_and (code, 1, 2, 0, 2);
	ia64_cmp4_gt_or (code, 1, 2, 0, 2);
	ia64_cmp4_gt_or_andcm (code, 1, 2, 0, 2);
	ia64_cmp4_le_and (code, 1, 2, 0, 2);
	ia64_cmp4_le_or (code, 1, 2, 0, 2);
	ia64_cmp4_le_or_andcm (code, 1, 2, 0, 2);
	ia64_cmp4_ge_and (code, 1, 2, 0, 2);
	ia64_cmp4_ge_or (code, 1, 2, 0, 2);
	ia64_cmp4_ge_or_andcm (code, 1, 2, 0, 2);
	ia64_cmp4_lt_and (code, 1, 2, 0, 2);
	ia64_cmp4_lt_or (code, 1, 2, 0, 2);
	ia64_cmp4_lt_or_andcm (code, 1, 2, 0, 2);

	ia64_cmp_lt_imm (code, 1, 2, 127, 2);
	ia64_cmp_lt_imm (code, 1, 2, -128, 2);

	ia64_cmp_lt_imm (code, 1, 2, -128, 2);
	ia64_cmp_ltu_imm (code, 1, 2, -128, 2);
	ia64_cmp_eq_imm (code, 1, 2, -128, 2);
	ia64_cmp_lt_unc_imm (code, 1, 2, -128, 2);
	ia64_cmp_ltu_unc_imm (code, 1, 2, -128, 2);
	ia64_cmp_eq_unc_imm (code, 1, 2, -128, 2);
	ia64_cmp_eq_and_imm (code, 1, 2, -128, 2);
	ia64_cmp_eq_or_imm (code, 1, 2, -128, 2);
	ia64_cmp_eq_unc_imm (code, 1, 2, -128, 2);
	ia64_cmp_ne_and_imm (code, 1, 2, -128, 2);
	ia64_cmp_ne_or_imm (code, 1, 2, -128, 2);
	ia64_cmp_ne_or_andcm_imm (code, 1, 2, -128, 2);

	ia64_cmp4_lt_imm (code, 1, 2, -128, 2);
	ia64_cmp4_ltu_imm (code, 1, 2, -128, 2);
	ia64_cmp4_eq_imm (code, 1, 2, -128, 2);
	ia64_cmp4_lt_unc_imm (code, 1, 2, -128, 2);
	ia64_cmp4_ltu_unc_imm (code, 1, 2, -128, 2);
	ia64_cmp4_eq_unc_imm (code, 1, 2, -128, 2);
	ia64_cmp4_eq_and_imm (code, 1, 2, -128, 2);
	ia64_cmp4_eq_or_imm (code, 1, 2, -128, 2);
	ia64_cmp4_eq_unc_imm (code, 1, 2, -128, 2);
	ia64_cmp4_ne_and_imm (code, 1, 2, -128, 2);
	ia64_cmp4_ne_or_imm (code, 1, 2, -128, 2);
	ia64_cmp4_ne_or_andcm_imm (code, 1, 2, -128, 2);

	ia64_padd1 (code, 1, 2, 3);
	ia64_padd2 (code, 1, 2, 3);
	ia64_padd4 (code, 1, 2, 3);
	ia64_padd1_sss (code, 1, 2, 3);
	ia64_padd2_sss (code, 1, 2, 3);
	ia64_padd1_uuu (code, 1, 2, 3);
	ia64_padd2_uuu (code, 1, 2, 3);
	ia64_padd1_uus (code, 1, 2, 3);
	ia64_padd2_uus (code, 1, 2, 3);

	ia64_psub1 (code, 1, 2, 3);
	ia64_psub2 (code, 1, 2, 3);
	ia64_psub4 (code, 1, 2, 3);
	ia64_psub1_sss (code, 1, 2, 3);
	ia64_psub2_sss (code, 1, 2, 3);
	ia64_psub1_uuu (code, 1, 2, 3);
	ia64_psub2_uuu (code, 1, 2, 3);
	ia64_psub1_uus (code, 1, 2, 3);
	ia64_psub2_uus (code, 1, 2, 3);

	ia64_pavg1 (code, 1, 2, 3);
	ia64_pavg2 (code, 1, 2, 3);
	ia64_pavg1_raz (code, 1, 2, 3);
	ia64_pavg2_raz (code, 1, 2, 3);
	ia64_pavgsub1 (code, 1, 2, 3);
	ia64_pavgsub2 (code, 1, 2, 3);
	ia64_pcmp1_eq (code, 1, 2, 3);
	ia64_pcmp2_eq (code, 1, 2, 3);
	ia64_pcmp4_eq (code, 1, 2, 3);
	ia64_pcmp1_gt (code, 1, 2, 3);
	ia64_pcmp2_gt (code, 1, 2, 3);
	ia64_pcmp4_gt (code, 1, 2, 3);
	
	ia64_pshladd2 (code, 1, 2, 3, 4);
	ia64_pshradd2 (code, 1, 2, 3, 4);

	ia64_pmpyshr2 (code, 1, 2, 3, 0);
	ia64_pmpyshr2_u (code, 1, 2, 3, 0);
	ia64_pmpyshr2 (code, 1, 2, 3, 7);
	ia64_pmpyshr2_u (code, 1, 2, 3, 7);
	ia64_pmpyshr2 (code, 1, 2, 3, 15);
	ia64_pmpyshr2_u (code, 1, 2, 3, 15);
	ia64_pmpyshr2 (code, 1, 2, 3, 16);
	ia64_pmpyshr2_u (code, 1, 2, 3, 16);

	ia64_pmpy2_r (code, 1, 2, 3);
	ia64_pmpy2_l (code, 1, 2, 3);
	ia64_mix1_r (code, 1, 2, 3);
	ia64_mix2_r (code, 1, 2, 3);
	ia64_mix4_r (code, 1, 2, 3);
	ia64_mix1_l (code, 1, 2, 3);
	ia64_mix2_l (code, 1, 2, 3);
	ia64_mix4_l (code, 1, 2, 3);
	ia64_pack2_uss (code, 1, 2, 3);
	ia64_pack2_sss (code, 1, 2, 3);
	ia64_pack4_sss (code, 1, 2, 3);
	ia64_unpack1_h (code, 1, 2, 3);
	ia64_unpack2_h (code, 1, 2, 3);
	ia64_unpack4_h (code, 1, 2, 3);
	ia64_unpack1_l (code, 1, 2, 3);
	ia64_unpack2_l (code, 1, 2, 3);
	ia64_unpack4_l (code, 1, 2, 3);
	ia64_pmin1_u (code, 1, 2, 3);
	ia64_pmax1_u (code, 1, 2, 3);
	ia64_pmin2 (code, 1, 2, 3);
	ia64_pmax2 (code, 1, 2, 3);
	ia64_psad1 (code, 1, 2, 3);

	ia64_mux1 (code, 1, 2, IA64_MUX1_BRCST);
	ia64_mux1 (code, 1, 2, IA64_MUX1_MIX);
	ia64_mux1 (code, 1, 2, IA64_MUX1_SHUF);
	ia64_mux1 (code, 1, 2, IA64_MUX1_ALT);
	ia64_mux1 (code, 1, 2, IA64_MUX1_REV);

	ia64_mux2 (code, 1, 2, 0x8d);

	ia64_pshr2 (code, 1, 2, 3);
	ia64_pshr4 (code, 1, 2, 3);
	ia64_shr (code, 1, 2, 3);
	ia64_pshr2_u (code, 1, 2, 3);
	ia64_pshr4_u (code, 1, 2, 3);
	ia64_shr_u (code, 1, 2, 3);

	ia64_pshr2_imm (code, 1, 2, 20);
	ia64_pshr4_imm (code, 1, 2, 20);
	ia64_pshr2_u_imm (code, 1, 2, 20);
	ia64_pshr4_u_imm (code, 1, 2, 20);

	ia64_pshl2 (code, 1, 2, 3);
	ia64_pshl4 (code, 1, 2, 3);
	ia64_shl (code, 1, 2, 3);

	ia64_pshl2_imm (code, 1, 2, 20);
	ia64_pshl4_imm (code, 1, 2, 20);

	ia64_popcnt (code, 1, 2);

	ia64_shrp (code, 1, 2, 3, 62);

	ia64_extr_u (code, 1, 2, 62, 61);
	ia64_extr (code, 1, 2, 62, 61);

	ia64_dep_z (code, 1, 2, 62, 61);

	ia64_dep_z_imm (code, 1, 127, 62, 61);
	ia64_dep_z_imm (code, 1, -128, 62, 61);
	ia64_dep_imm (code, 1, 0, 2, 62, 61);
	ia64_dep_imm (code, 1, -1, 2, 62, 61);
	ia64_dep (code, 1, 2, 3, 10, 15);

	ia64_tbit_z (code, 1, 2, 3, 0);

	ia64_tbit_z (code, 1, 2, 3, 63);
	ia64_tbit_z_unc (code, 1, 2, 3, 63);
	ia64_tbit_z_and (code, 1, 2, 3, 63);
	ia64_tbit_nz_and (code, 1, 2, 3, 63);
	ia64_tbit_z_or (code, 1, 2, 3, 63);
	ia64_tbit_nz_or (code, 1, 2, 3, 63);
	ia64_tbit_z_or_andcm (code, 1, 2, 3, 63);
	ia64_tbit_nz_or_andcm (code, 1, 2, 3, 63);

	ia64_tnat_z (code, 1, 2, 3);
	ia64_tnat_z_unc (code, 1, 2, 3);
	ia64_tnat_z_and (code, 1, 2, 3);
	ia64_tnat_nz_and (code, 1, 2, 3);
	ia64_tnat_z_or (code, 1, 2, 3);
	ia64_tnat_nz_or (code, 1, 2, 3);
	ia64_tnat_z_or_andcm (code, 1, 2, 3);
	ia64_tnat_nz_or_andcm (code, 1, 2, 3);

	ia64_nop_i (code, 0x1234);
	ia64_hint_i (code, 0x1234);

	ia64_break_i (code, 0x1234);

	ia64_chk_s_i (code, 1, 0);
	ia64_chk_s_i (code, 1, -1);
	ia64_chk_s_i (code, 1, 1);

	ia64_mov_to_br_hint (code, 1, 1, -1, IA64_MOV_TO_BR_WH_NONE, 0);
	ia64_mov_to_br_hint (code, 1, 1, -1, IA64_MOV_TO_BR_WH_SPTK, 0);
	ia64_mov_to_br_hint (code, 1, 1, -1, IA64_MOV_TO_BR_WH_DPTK, 0);
	ia64_mov_to_br_hint (code, 1, 1, -1, IA64_MOV_TO_BR_WH_DPTK, IA64_BR_IH_IMP);
	ia64_mov_ret_to_br_hint (code, 1, 1, -1, IA64_MOV_TO_BR_WH_NONE, 0);

	ia64_mov_from_br (code, 1, 1);

	ia64_mov_to_pred (code, 1, 0xfe);

	ia64_mov_to_pred_rot_imm (code, 0xff0000);

	ia64_mov_from_ip (code, 1);
	ia64_mov_from_pred (code, 1);

	ia64_mov_to_ar_i (code, 1, 1);

	ia64_mov_to_ar_imm_i (code, 1, 127);

	ia64_mov_from_ar_i (code, 1, 1);

	ia64_zxt1 (code, 1, 2);
	ia64_zxt2 (code, 1, 2);
	ia64_zxt4 (code, 1, 2);
	ia64_sxt1 (code, 1, 2);
	ia64_sxt2 (code, 1, 2);
	ia64_sxt4 (code, 1, 2);

	ia64_czx1_l (code, 1, 2);
	ia64_czx2_l (code, 1, 2);
	ia64_czx1_r (code, 1, 2);
	ia64_czx2_r (code, 1, 2);

	ia64_ld1_hint (code, 1, 2, IA64_LD_HINT_NONE);
	ia64_ld1_hint (code, 1, 2, IA64_LD_HINT_NT1);
	ia64_ld1_hint (code, 1, 2, IA64_LD_HINT_NTA);

	ia64_ld1_hint (code, 1, 2, 0);
	ia64_ld2_hint (code, 1, 2, 0);
	ia64_ld4_hint (code, 1, 2, 0);
	ia64_ld8_hint (code, 1, 2, 0);

	ia64_ld1_s_hint (code, 1, 2, 0);
	ia64_ld2_s_hint (code, 1, 2, 0);
	ia64_ld4_s_hint (code, 1, 2, 0);
	ia64_ld8_s_hint (code, 1, 2, 0);

	ia64_ld1_a_hint (code, 1, 2, 0);
	ia64_ld2_a_hint (code, 1, 2, 0);
	ia64_ld4_a_hint (code, 1, 2, 0);
	ia64_ld8_a_hint (code, 1, 2, 0);

	ia64_ld1_sa_hint (code, 1, 2, 0);
	ia64_ld2_sa_hint (code, 1, 2, 0);
	ia64_ld4_sa_hint (code, 1, 2, 0);
	ia64_ld8_sa_hint (code, 1, 2, 0);

	ia64_ld1_bias_hint (code, 1, 2, 0);
	ia64_ld2_bias_hint (code, 1, 2, 0);
	ia64_ld4_bias_hint (code, 1, 2, 0);
	ia64_ld8_bias_hint (code, 1, 2, 0);

	ia64_ld1_inc_hint (code, 1, 2, 3, IA64_LD_HINT_NONE);

	ia64_ld1_inc_imm_hint (code, 1, 2, 255, IA64_LD_HINT_NONE);
	ia64_ld1_inc_imm_hint (code, 1, 2, -256, IA64_LD_HINT_NONE);

	ia64_st1_hint (code, 1, 2, IA64_ST_HINT_NTA);

	ia64_st1_hint (code, 1, 2, IA64_ST_HINT_NONE);
	ia64_st2_hint (code, 1, 2, IA64_ST_HINT_NONE);
	ia64_st4_hint (code, 1, 2, IA64_ST_HINT_NONE);
	ia64_st8_hint (code, 1, 2, IA64_ST_HINT_NONE);

	ia64_st1_rel_hint (code, 1, 2, IA64_ST_HINT_NONE);
	ia64_st2_rel_hint (code, 1, 2, IA64_ST_HINT_NONE);
	ia64_st4_rel_hint (code, 1, 2, IA64_ST_HINT_NONE);
	ia64_st8_rel_hint (code, 1, 2, IA64_ST_HINT_NONE);

	ia64_st8_spill_hint (code, 1, 2, IA64_ST_HINT_NONE);

	ia64_st16_hint (code, 1, 2, IA64_ST_HINT_NONE);
	ia64_st16_rel_hint (code, 1, 2, IA64_ST_HINT_NONE);

	ia64_st1_inc_imm_hint (code, 1, 2, 255, IA64_ST_HINT_NONE);
	ia64_st2_inc_imm_hint (code, 1, 2, 255, IA64_ST_HINT_NONE);
	ia64_st4_inc_imm_hint (code, 1, 2, 255, IA64_ST_HINT_NONE);
	ia64_st8_inc_imm_hint (code, 1, 2, 255, IA64_ST_HINT_NONE);

	ia64_st1_rel_inc_imm_hint (code, 1, 2, 255, IA64_ST_HINT_NONE);
	ia64_st2_rel_inc_imm_hint (code, 1, 2, 255, IA64_ST_HINT_NONE);
	ia64_st4_rel_inc_imm_hint (code, 1, 2, 255, IA64_ST_HINT_NONE);
	ia64_st8_rel_inc_imm_hint (code, 1, 2, 255, IA64_ST_HINT_NONE);

	ia64_st8_spill_inc_imm_hint (code, 1, 2, 255, IA64_ST_HINT_NONE);

	ia64_ldfs_hint (code, 1, 2, 0);
	ia64_ldfd_hint (code, 1, 2, 0);
	ia64_ldf8_hint (code, 1, 2, 0);
	ia64_ldfe_hint (code, 1, 2, 0);

	ia64_ldfs_s_hint (code, 1, 2, 0);
	ia64_ldfd_s_hint (code, 1, 2, 0);
	ia64_ldf8_s_hint (code, 1, 2, 0);
	ia64_ldfe_s_hint (code, 1, 2, 0);

	ia64_ldfs_a_hint (code, 1, 2, 0);
	ia64_ldfd_a_hint (code, 1, 2, 0);
	ia64_ldf8_a_hint (code, 1, 2, 0);
	ia64_ldfe_a_hint (code, 1, 2, 0);

	ia64_ldfs_sa_hint (code, 1, 2, 0);
	ia64_ldfd_sa_hint (code, 1, 2, 0);
	ia64_ldf8_sa_hint (code, 1, 2, 0);
	ia64_ldfe_sa_hint (code, 1, 2, 0);

	ia64_ldfs_c_clr_hint (code, 1, 2, 0);
	ia64_ldfd_c_clr_hint (code, 1, 2, 0);
	ia64_ldf8_c_clr_hint (code, 1, 2, 0);
	ia64_ldfe_c_clr_hint (code, 1, 2, 0);

	ia64_ldfs_c_nc_hint (code, 1, 2, 0);
	ia64_ldfd_c_nc_hint (code, 1, 2, 0);
	ia64_ldf8_c_nc_hint (code, 1, 2, 0);
	ia64_ldfe_c_nc_hint (code, 1, 2, 0);

	ia64_ldf_fill_hint (code, 1, 2, 0);

	ia64_ldfs_inc_hint (code, 1, 2, 3, 0);
	ia64_ldfd_inc_hint (code, 1, 2, 3, 0);
	ia64_ldf8_inc_hint (code, 1, 2, 3, 0);
	ia64_ldfe_inc_hint (code, 1, 2, 3, 0);

	ia64_ldfs_s_inc_hint (code, 1, 2, 3, 0);
	ia64_ldfd_s_inc_hint (code, 1, 2, 3, 0);
	ia64_ldf8_s_inc_hint (code, 1, 2, 3, 0);
	ia64_ldfe_s_inc_hint (code, 1, 2, 3, 0);

	ia64_ldfs_a_inc_hint (code, 1, 2, 3, 0);
	ia64_ldfd_a_inc_hint (code, 1, 2, 3, 0);
	ia64_ldf8_a_inc_hint (code, 1, 2, 3, 0);
	ia64_ldfe_a_inc_hint (code, 1, 2, 3, 0);

	ia64_ldfs_sa_inc_hint (code, 1, 2, 3, 0);
	ia64_ldfd_sa_inc_hint (code, 1, 2, 3, 0);
	ia64_ldf8_sa_inc_hint (code, 1, 2, 3, 0);
	ia64_ldfe_sa_inc_hint (code, 1, 2, 3, 0);

	ia64_ldfs_c_clr_inc_hint (code, 1, 2, 3, 0);
	ia64_ldfd_c_clr_inc_hint (code, 1, 2, 3, 0);
	ia64_ldf8_c_clr_inc_hint (code, 1, 2, 3, 0);
	ia64_ldfe_c_clr_inc_hint (code, 1, 2, 3, 0);

	ia64_ldfs_c_nc_inc_hint (code, 1, 2, 3, 0);
	ia64_ldfd_c_nc_inc_hint (code, 1, 2, 3, 0);
	ia64_ldf8_c_nc_inc_hint (code, 1, 2, 3, 0);
	ia64_ldfe_c_nc_inc_hint (code, 1, 2, 3, 0);

	ia64_ldf_fill_inc_hint (code, 1, 2, 3, 0);

	ia64_ldfs_inc_imm_hint (code, 1, 2, 255, 0);
	ia64_ldfd_inc_imm_hint (code, 1, 2, 255, 0);
	ia64_ldf8_inc_imm_hint (code, 1, 2, 255, 0);
	ia64_ldfe_inc_imm_hint (code, 1, 2, 255, 0);

	ia64_ldfs_s_inc_imm_hint (code, 1, 2, 255, 0);
	ia64_ldfd_s_inc_imm_hint (code, 1, 2, 255, 0);
	ia64_ldf8_s_inc_imm_hint (code, 1, 2, 255, 0);
	ia64_ldfe_s_inc_imm_hint (code, 1, 2, 255, 0);

	ia64_ldfs_a_inc_imm_hint (code, 1, 2, 255, 0);
	ia64_ldfd_a_inc_imm_hint (code, 1, 2, 255, 0);
	ia64_ldf8_a_inc_imm_hint (code, 1, 2, 255, 0);
	ia64_ldfe_a_inc_imm_hint (code, 1, 2, 255, 0);

	ia64_ldfs_sa_inc_imm_hint (code, 1, 2, 255, 0);
	ia64_ldfd_sa_inc_imm_hint (code, 1, 2, 255, 0);
	ia64_ldf8_sa_inc_imm_hint (code, 1, 2, 255, 0);
	ia64_ldfe_sa_inc_imm_hint (code, 1, 2, 255, 0);

	ia64_ldfs_c_clr_inc_imm_hint (code, 1, 2, 255, 0);
	ia64_ldfd_c_clr_inc_imm_hint (code, 1, 2, 255, 0);
	ia64_ldf8_c_clr_inc_imm_hint (code, 1, 2, 255, 0);
	ia64_ldfe_c_clr_inc_imm_hint (code, 1, 2, 255, 0);

	ia64_ldfs_c_nc_inc_imm_hint (code, 1, 2, 255, 0);
	ia64_ldfd_c_nc_inc_imm_hint (code, 1, 2, 255, 0);
	ia64_ldf8_c_nc_inc_imm_hint (code, 1, 2, 255, 0);
	ia64_ldfe_c_nc_inc_imm_hint (code, 1, 2, 255, 0);

	ia64_ldf_fill_inc_imm_hint (code, 1, 2, 255, 0);

	ia64_stfs_hint (code, 1, 2, 0);
	ia64_stfd_hint (code, 1, 2, 0);
	ia64_stf8_hint (code, 1, 2, 0);
	ia64_stfe_hint (code, 1, 2, 0);

	ia64_stf_spill_hint (code, 1, 2, 0);

	ia64_stfs_inc_imm_hint (code, 1, 2, 255, 0);
	ia64_stfd_inc_imm_hint (code, 1, 2, 255, 0);
	ia64_stf8_inc_imm_hint (code, 1, 2, 255, 0);
	ia64_stfe_inc_imm_hint (code, 1, 2, 255, 0);

	ia64_stf_spill_inc_imm_hint (code, 1, 2, 255, 0);

	ia64_ldfps_hint (code, 1, 2, 3, 0);
	ia64_ldfpd_hint (code, 1, 2, 3, 0);
	ia64_ldfp8_hint (code, 1, 2, 3, 0);

	ia64_ldfps_s_hint (code, 1, 2, 3, 0);
	ia64_ldfpd_s_hint (code, 1, 2, 3, 0);
	ia64_ldfp8_s_hint (code, 1, 2, 3, 0);

	ia64_ldfps_a_hint (code, 1, 2, 3, 0);
	ia64_ldfpd_a_hint (code, 1, 2, 3, 0);
	ia64_ldfp8_a_hint (code, 1, 2, 3, 0);

	ia64_ldfps_sa_hint (code, 1, 2, 3, 0);
	ia64_ldfpd_sa_hint (code, 1, 2, 3, 0);
	ia64_ldfp8_sa_hint (code, 1, 2, 3, 0);

	ia64_ldfps_c_clr_hint (code, 1, 2, 3, 0);
	ia64_ldfpd_c_clr_hint (code, 1, 2, 3, 0);
	ia64_ldfp8_c_clr_hint (code, 1, 2, 3, 0);

	ia64_ldfps_c_nc_hint (code, 1, 2, 3, 0);
	ia64_ldfpd_c_nc_hint (code, 1, 2, 3, 0);
	ia64_ldfp8_c_nc_hint (code, 1, 2, 3, 0);

	ia64_ldfps_inc_hint (code, 1, 2, 3, 0);
	ia64_ldfpd_inc_hint (code, 1, 2, 3, 0);
	ia64_ldfp8_inc_hint (code, 1, 2, 3, 0);

	ia64_ldfps_s_inc_hint (code, 1, 2, 3, 0);
	ia64_ldfpd_s_inc_hint (code, 1, 2, 3, 0);
	ia64_ldfp8_s_inc_hint (code, 1, 2, 3, 0);

	ia64_ldfps_a_inc_hint (code, 1, 2, 3, 0);
	ia64_ldfpd_a_inc_hint (code, 1, 2, 3, 0);
	ia64_ldfp8_a_inc_hint (code, 1, 2, 3, 0);

	ia64_ldfps_sa_inc_hint (code, 1, 2, 3, 0);
	ia64_ldfpd_sa_inc_hint (code, 1, 2, 3, 0);
	ia64_ldfp8_sa_inc_hint (code, 1, 2, 3, 0);

	ia64_ldfps_c_clr_inc_hint (code, 1, 2, 3, 0);
	ia64_ldfpd_c_clr_inc_hint (code, 1, 2, 3, 0);
	ia64_ldfp8_c_clr_inc_hint (code, 1, 2, 3, 0);

	ia64_ldfps_c_nc_inc_hint (code, 1, 2, 3, 0);
	ia64_ldfpd_c_nc_inc_hint (code, 1, 2, 3, 0);
	ia64_ldfp8_c_nc_inc_hint (code, 1, 2, 3, 0);

	ia64_lfetch_hint (code, 1, 0);
	ia64_lfetch_excl_hint (code, 1, 0);
	ia64_lfetch_fault_hint (code, 1, 0);
	ia64_lfetch_fault_excl_hint (code, 1, 0);

	ia64_lfetch_hint (code, 1, IA64_LFHINT_NT1);
	ia64_lfetch_hint (code, 1, IA64_LFHINT_NT2);
	ia64_lfetch_hint (code, 1, IA64_LFHINT_NTA);

	ia64_lfetch_inc_hint (code, 1, 2, 0);
	ia64_lfetch_excl_inc_hint (code, 1, 2, 0);
	ia64_lfetch_fault_inc_hint (code, 1, 2, 0);
	ia64_lfetch_fault_excl_inc_hint (code, 1, 2, 0);

	ia64_lfetch_inc_imm_hint (code, 1, 255, 0);
	ia64_lfetch_excl_inc_imm_hint (code, 1, 255, 0);
	ia64_lfetch_fault_inc_imm_hint (code, 1, 255, 0);
	ia64_lfetch_fault_excl_inc_imm_hint (code, 1, 255, 0);

	ia64_cmpxchg1_acq_hint (code, 1, 2, 3, 0);
	ia64_cmpxchg2_acq_hint (code, 1, 2, 3, 0);
	ia64_cmpxchg4_acq_hint (code, 1, 2, 3, 0);
	ia64_cmpxchg8_acq_hint (code, 1, 2, 3, 0);
	ia64_cmpxchg1_rel_hint (code, 1, 2, 3, 0);
	ia64_cmpxchg2_rel_hint (code, 1, 2, 3, 0);
	ia64_cmpxchg4_rel_hint (code, 1, 2, 3, 0);
	ia64_cmpxchg8_rel_hint (code, 1, 2, 3, 0);
	ia64_cmpxchg16_acq_hint (code, 1, 2, 3, 0);
	ia64_cmpxchg16_rel_hint (code, 1, 2, 3, 0);
	ia64_xchg1_hint (code, 1, 2, 3, 0);
	ia64_xchg2_hint (code, 1, 2, 3, 0);
	ia64_xchg4_hint (code, 1, 2, 3, 0);
	ia64_xchg8_hint (code, 1, 2, 3, 0);

	ia64_fetchadd4_acq_hint (code, 1, 2, -16, 0);
	ia64_fetchadd4_acq_hint (code, 1, 2, -8, 0);
	ia64_fetchadd4_acq_hint (code, 1, 2, -4, 0);
	ia64_fetchadd4_acq_hint (code, 1, 2, -1, 0);
	ia64_fetchadd4_acq_hint (code, 1, 2, 1, 0);
	ia64_fetchadd4_acq_hint (code, 1, 2, 4, 0);
	ia64_fetchadd4_acq_hint (code, 1, 2, 8, 0);
	ia64_fetchadd4_acq_hint (code, 1, 2, 16, 0);

	ia64_fetchadd4_acq_hint (code, 1, 2, 16, 0);
	ia64_fetchadd8_acq_hint (code, 1, 2, 16, 0);
	ia64_fetchadd4_rel_hint (code, 1, 2, 16, 0);
	ia64_fetchadd8_rel_hint (code, 1, 2, 16, 0);

	ia64_setf_sig (code, 1, 2);
	ia64_setf_exp (code, 1, 2);
	ia64_setf_s (code, 1, 2);
	ia64_setf_d (code, 1, 2);

	ia64_getf_sig (code, 1, 2);
	ia64_getf_exp (code, 1, 2);
	ia64_getf_s (code, 1, 2);
	ia64_getf_d (code, 1, 2);

	ia64_chk_s_m (code, 1, 0);
	ia64_chk_s_m (code, 1, 1);
	ia64_chk_s_m (code, 1, -1);

	ia64_chk_s_float_m (code, 1, 0);

	ia64_chk_a_nc (code, 1, 0);
	ia64_chk_a_nc (code, 1, 1);
	ia64_chk_a_nc (code, 1, -1);

	ia64_chk_a_nc (code, 1, 0);
	ia64_chk_a_clr (code, 1, 0);

	ia64_chk_a_nc_float (code, 1, 0);
	ia64_chk_a_clr_float (code, 1, 0);

	ia64_invala (code);
	ia64_fwb (code);
	ia64_mf (code);
	ia64_mf_a (code);
	ia64_srlz_d (code);
	ia64_stlz_i (code);
	ia64_sync_i (code);

	ia64_flushrs (code);
	ia64_loadrs (code);

	ia64_invala_e (code, 1);
	ia64_invala_e_float (code, 1);

	ia64_fc (code, 1);
	ia64_fc_i (code, 1);

	ia64_mov_to_ar_m (code, 1, 1);

	ia64_mov_to_ar_imm_m (code, 1, 127);

	ia64_mov_from_ar_m (code, 1, 1);

	ia64_mov_to_cr (code, 1, 2);

	ia64_mov_from_cr (code, 1, 2);

	ia64_alloc (code, 1, 3, 4, 5, 0);
	ia64_alloc (code, 1, 3, 4, 5, 8);

	ia64_mov_to_psr_l (code, 1);
	ia64_mov_to_psr_um (code, 1);

	ia64_mov_from_psr (code, 1);
	ia64_mov_from_psr_um (code, 1);

	ia64_break_m (code, 0x1234);
	ia64_nop_m (code, 0x1234);
	ia64_hint_m (code, 0x1234);

	ia64_br_cond_hint (code, 0, 0, 0, 0);
	ia64_br_wexit_hint (code, 0, 0, 0, 0);
	ia64_br_wtop_hint (code, 0, 0, 0, 0);

	ia64_br_cloop_hint (code, 0, 0, 0, 0);
	ia64_br_cexit_hint (code, 0, 0, 0, 0);
	ia64_br_ctop_hint (code, 0, 0, 0, 0);

	ia64_br_call_hint (code, 1, 0, 0, 0, 0);

	ia64_br_cond_reg_hint (code, 1, 0, 0, 0);
	ia64_br_ia_reg_hint (code, 1, 0, 0, 0);
	ia64_br_ret_reg_hint (code, 1, 0, 0, 0);

	ia64_br_call_reg_hint (code, 1, 2, 0, 0, 0);

	ia64_cover (code);
	ia64_clrrrb (code);
	ia64_clrrrb_pr (code);
	ia64_rfi (code);
	ia64_bsw_0 (code);
	ia64_bsw_1 (code);
	ia64_epc (code);

	ia64_break_b (code, 0x1234);
	ia64_nop_b (code, 0x1234);
	ia64_hint_b (code, 0x1234);

	ia64_break_x (code, 0x2123456789ABCDEFULL);

	ia64_movl (code, 1, 0x123456789ABCDEF0LL);

	ia64_brl_cond_hint (code, 0, 0, 0, 0);
	ia64_brl_cond_hint (code, -1, 0, 0, 0);

	ia64_brl_call_hint (code, 1, 0, 0, 0, 0);
	ia64_brl_call_hint (code, 1, -1, 0, 0, 0);

	ia64_nop_x (code, 0x2123456789ABCDEFULL);
	ia64_hint_x (code, 0x2123456789ABCDEFULL);

	ia64_movl_pred (code, 1, 1, 0x123456789ABCDEF0LL);

	/* FLOATING-POINT */
	ia64_fma_sf_pred (code, 1, 1, 2, 3, 4, 2);
	ia64_fma_s_sf_pred (code, 1, 1, 2, 3, 4, 2);
	ia64_fma_d_sf_pred (code, 1, 1, 2, 3, 4, 2);
	ia64_fpma_sf_pred (code, 1, 1, 2, 3, 4, 2);
	ia64_fms_sf_pred (code, 1, 1, 2, 3, 4, 2);
	ia64_fms_s_sf_pred (code, 1, 1, 2, 3, 4, 2);
	ia64_fms_d_sf_pred (code, 1, 1, 2, 3, 4, 2);
	ia64_fpms_sf_pred (code, 1, 1, 2, 3, 4, 2);
	ia64_fnma_sf_pred (code, 1, 1, 2, 3, 4, 2);
	ia64_fnma_s_sf_pred (code, 1, 1, 2, 3, 4, 2);
	ia64_fnma_d_sf_pred (code, 1, 1, 2, 3, 4, 2);
	ia64_fpnma_sf_pred (code, 1, 1, 2, 3, 4, 2);

	ia64_xma_l_pred (code, 1, 1, 2, 3, 4);
	ia64_xma_h_pred (code, 1, 1, 2, 3, 4);
	ia64_xma_hu_pred (code, 1, 1, 2, 3, 4);

	ia64_fselect_pred (code, 1, 1, 2, 3, 4);

	ia64_fcmp_eq_sf_pred (code, 1, 1, 2, 3, 4, 0);
	ia64_fcmp_lt_sf_pred (code, 1, 1, 2, 3, 4, 0);
	ia64_fcmp_le_sf_pred (code, 1, 1, 2, 3, 4, 0);
	ia64_fcmp_unord_sf_pred (code, 1, 1, 2, 3, 4, 0);
	ia64_fcmp_eq_unc_sf_pred (code, 1, 1, 2, 3, 4, 0);
	ia64_fcmp_lt_unc_sf_pred (code, 1, 1, 2, 3, 4, 0);
	ia64_fcmp_le_unc_sf_pred (code, 1, 1, 2, 3, 4, 0);
	ia64_fcmp_unord_unc_sf_pred (code, 1, 1, 2, 3, 4, 0);

	ia64_fclass_m_pred (code, 1, 1, 2, 3, 0x1ff);
	ia64_fclass_m_unc_pred (code, 1, 1, 2, 3, 0x1ff);

	ia64_frcpa_sf_pred (code, 1, 1, 2, 3, 4, 0);
	ia64_fprcpa_sf_pred (code, 1, 1, 2, 3, 4, 0);

	ia64_frsqrta_sf_pred (code, 1, 1, 2, 4, 0);
	ia64_fprsqrta_sf_pred (code, 1, 1, 2, 4, 0);

	ia64_fmin_sf_pred (code, 1, 2, 3, 4, 0);
	ia64_fman_sf_pred (code, 1, 2, 3, 4, 0);
	ia64_famin_sf_pred (code, 1, 2, 3, 4, 0);
	ia64_famax_sf_pred (code, 1, 2, 3, 4, 0);
	ia64_fpmin_sf_pred (code, 1, 2, 3, 4, 0);
	ia64_fpman_sf_pred (code, 1, 2, 3, 4, 0);
	ia64_fpamin_sf_pred (code, 1, 2, 3, 4, 0);
	ia64_fpamax_sf_pred (code, 1, 2, 3, 4, 0);
	ia64_fpcmp_eq_sf_pred (code, 1, 2, 3, 4, 0);
	ia64_fpcmp_lt_sf_pred (code, 1, 2, 3, 4, 0);
	ia64_fpcmp_le_sf_pred (code, 1, 2, 3, 4, 0);
	ia64_fpcmp_unord_sf_pred (code, 1, 2, 3, 4, 0);
	ia64_fpcmp_neq_sf_pred (code, 1, 2, 3, 4, 0);
	ia64_fpcmp_nlt_sf_pred (code, 1, 2, 3, 4, 0);
	ia64_fpcmp_nle_sf_pred (code, 1, 2, 3, 4, 0);
	ia64_fpcmp_ord_sf_pred (code, 1, 2, 3, 4, 0);

	ia64_fmerge_s_pred (code, 1, 2, 3, 4);
	ia64_fmerge_ns_pred (code, 1, 2, 3, 4);
	ia64_fmerge_se_pred (code, 1, 2, 3, 4);
	ia64_fmix_lr_pred (code, 1, 2, 3, 4);
	ia64_fmix_r_pred (code, 1, 2, 3, 4);
	ia64_fmix_l_pred (code, 1, 2, 3, 4);
	ia64_fsxt_r_pred (code, 1, 2, 3, 4);
	ia64_fsxt_l_pred (code, 1, 2, 3, 4);
	ia64_fpack_pred (code, 1, 2, 3, 4);
	ia64_fswap_pred (code, 1, 2, 3, 4);
	ia64_fswap_nl_pred (code, 1, 2, 3, 4);
	ia64_fswap_nr_pred (code, 1, 2, 3, 4);
	ia64_fand_pred (code, 1, 2, 3, 4);
	ia64_fandcm_pred (code, 1, 2, 3, 4);
	ia64_for_pred (code, 1, 2, 3, 4);
	ia64_fxor_pred (code, 1, 2, 3, 4);
	ia64_fpmerge_s_pred (code, 1, 2, 3, 4);
	ia64_fpmerge_ns_pred (code, 1, 2, 3, 4);
	ia64_fpmerge_se_pred (code, 1, 2, 3, 4);
	
	ia64_fcvt_fx_sf_pred ((code), 1, 2, 3, 0);
	ia64_fcvt_fxu_sf_pred ((code), 1, 2, 3, 0);
	ia64_fcvt_fx_trunc_sf_pred ((code), 1, 2, 3, 0);
	ia64_fcvt_fxu_trunc_sf_pred ((code), 1, 2, 3, 0);
	ia64_fpcvt_fx_sf_pred ((code), 1, 2, 3, 0);
	ia64_fpcvt_fxu_sf_pred ((code), 1, 2, 3, 0);
	ia64_fpcvt_fx_trunc_sf_pred ((code), 1, 2, 3, 0);
	ia64_fpcvt_fxu_trunc_sf_pred ((code), 1, 2, 3, 0);

	ia64_fcvt_xf_pred ((code), 1, 2, 3);

	ia64_fsetc_sf_pred ((code), 1, 0x33, 0x33, 3);

	ia64_fclrf_sf_pred ((code), 1, 3);

	ia64_fchkf_sf_pred ((code), 1, -1, 3);

	ia64_break_f_pred ((code), 1, 0x1234);

	ia64_movl (code, 31, -123456);

	ia64_codegen_close (code);

#if 0
	/* disassembly */
	{
		guint8 *buf = code.buf;
		int template;
		guint64 dw1, dw2;
		guint64 ins1, ins2, ins3;

		ia64_break_i (code, 0x1234);

		ia64_codegen_close (code);

		dw1 = ((guint64*)buf) [0];
		dw2 = ((guint64*)buf) [1];

		template = ia64_bundle_template (buf);
		ins1 = ia64_bundle_ins1 (buf);
		ins2 = ia64_bundle_ins2 (buf);
		ins3 = ia64_bundle_ins3 (buf);

		code.buf = buf;
		ia64_emit_bundle_template (&code, template, ins1, ins2, ins3);

		g_assert (dw1 == ((guint64*)buf) [0]);
		g_assert (dw2 == ((guint64*)buf) [1]);
	}
#endif

	mono_disassemble_code (buf, 40960, "code");

	return 0;
}
