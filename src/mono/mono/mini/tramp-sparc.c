/*
 * tramp-sparc.c: JIT trampoline code for Sparc
 *
 * Authors:
 *   Mark Crichton (crichton@gimp.org)
 *   Dietmar Maurer (dietmar@ximian.com)
 *
 * (C) 2003 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>

#include <mono/arch/sparc/sparc-codegen.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/marshal.h>
#include <mono/metadata/tabledefs.h>

#include "mini.h"
#include "mini-sparc.h"

/*
 * mono_arch_get_unbox_trampoline:
 * @m: method pointer
 * @addr: pointer to native code for @m
 *
 * when value type methods are called through the vtable we need to unbox the
 * this argument. This method returns a pointer to a trampoline which does
 * unboxing before calling the method
 */
gpointer
mono_arch_get_unbox_trampoline (MonoMethod *m, gpointer addr)
{
	guint8 *code, *start;
	int this_pos = 4, reg;

	if (MONO_TYPE_ISSTRUCT (mono_method_signature (m)->ret))
		this_pos = 8;
	    
	start = code = mono_global_codeman_reserve (36);

	/* This executes in the context of the caller, hence o0 */
	sparc_add_imm (code, 0, sparc_o0, sizeof (MonoObject), sparc_o0);
#ifdef SPARCV9
	reg = sparc_g4;
#else
	reg = sparc_g1;
#endif
	sparc_set (code, addr, reg);
	sparc_jmpl (code, reg, sparc_g0, sparc_g0);
	sparc_nop (code);

	g_assert ((code - start) <= 36);

	mono_arch_flush_icache (start, code - start);

	return start;
}

void
mono_arch_patch_callsite (guint8 *method_start, guint8 *code, guint8 *addr)
{
	if (sparc_inst_op (*(guint32*)code) == 0x1) {
		sparc_call_simple (code, (guint8*)addr - (guint8*)code);
	}
}

void
mono_arch_patch_plt_entry (guint8 *code, guint8 *addr)
{
	g_assert_not_reached ();
}

void
mono_arch_nullify_class_init_trampoline (guint8 *code, gssize *regs)
{
	/* Patch calling code */
	sparc_nop (code);
}

void
mono_arch_nullify_plt_entry (guint8 *code)
{
	g_assert_not_reached ();
}

#define ALIGN_TO(val,align) (((val) + ((align) - 1)) & ~((align) - 1))

guchar*
mono_arch_create_trampoline_code (MonoTrampolineType tramp_type)
{
	guint8 *buf, *code, *tramp_addr;
	guint32 lmf_offset, regs_offset, method_reg, i;
	gboolean has_caller;

	if (tramp_type == MONO_TRAMPOLINE_JUMP)
		has_caller = FALSE;
	else
		has_caller = TRUE;

	code = buf = mono_global_codeman_reserve (1024);

	sparc_save_imm (code, sparc_sp, -1608, sparc_sp);

#ifdef SPARCV9
	method_reg = sparc_g4;
#else
	method_reg = sparc_g1;
#endif

	regs_offset = MONO_SPARC_STACK_BIAS + 1000;

	/* Save r1 needed by the IMT code */
	sparc_sti_imm (code, sparc_g1, sparc_sp, regs_offset + (sparc_g1 * sizeof (gpointer)));

	/* 
	 * sparc_g5 contains the return address, the trampoline argument is stored in the
	 * instruction stream after the call.
	 */
	sparc_ld_imm (code, sparc_g5, 8, method_reg);

#ifdef SPARCV9
	/* Save fp regs since they are not preserved by calls */
	for (i = 0; i < 16; i ++)
		sparc_stdf_imm (code, sparc_f0 + (i * 2), sparc_sp, MONO_SPARC_STACK_BIAS + 320 + (i * 8));
#endif	

	/* We receive the method address in %r1, so save it here */
	sparc_sti_imm (code, method_reg, sparc_sp, MONO_SPARC_STACK_BIAS + 200);

	/* Save lmf since compilation can raise exceptions */
	lmf_offset = MONO_SPARC_STACK_BIAS - sizeof (MonoLMF);

	/* Save the data for the parent (managed) frame */

	/* Save ip */
	sparc_sti_imm (code, sparc_i7, sparc_fp, lmf_offset + G_STRUCT_OFFSET (MonoLMF, ip));
	/* Save sp */
	sparc_sti_imm (code, sparc_fp, sparc_fp, lmf_offset + G_STRUCT_OFFSET (MonoLMF, sp));
	/* Save fp */
	/* Load previous fp from the saved register window */
	sparc_flushw (code);
	sparc_ldi_imm (code, sparc_fp, MONO_SPARC_STACK_BIAS + (sparc_i6 - 16) * sizeof (gpointer), sparc_o7);
	sparc_sti_imm (code, sparc_o7, sparc_fp, lmf_offset + G_STRUCT_OFFSET (MonoLMF, ebp));
	/* Save method */
	sparc_sti_imm (code, method_reg, sparc_fp, lmf_offset + G_STRUCT_OFFSET (MonoLMF, method));

	sparc_set (code, mono_get_lmf_addr, sparc_o7);
	sparc_jmpl (code, sparc_o7, sparc_g0, sparc_o7);
	sparc_nop (code);

	code = mono_sparc_emit_save_lmf (code, lmf_offset);

	if (has_caller) {
		/* Load all registers of the caller into a table inside this frame */
		/* first the out registers */
		for (i = 0; i < 8; ++i)
			sparc_sti_imm (code, sparc_i0 + i, sparc_sp, regs_offset + ((sparc_o0 + i) * sizeof (gpointer)));
		/* then the in+local registers */
		for (i = 0; i < 16; i ++) {
			sparc_ldi_imm (code, sparc_fp, MONO_SPARC_STACK_BIAS + (i * sizeof (gpointer)), sparc_o7);
			sparc_sti_imm (code, sparc_o7, sparc_sp, regs_offset + ((sparc_l0 + i) * sizeof (gpointer)));
		}
	}

	tramp_addr = mono_get_trampoline_func (tramp_type);
	sparc_ldi_imm (code, sparc_sp, MONO_SPARC_STACK_BIAS + 200, sparc_o2);
	/* pass address of register table as third argument */
	sparc_add_imm (code, FALSE, sparc_sp, regs_offset, sparc_o0);
	sparc_set (code, tramp_addr, sparc_o7);
	/* set %o1 to caller address */
	if (has_caller)
		sparc_mov_reg_reg (code, sparc_i7, sparc_o1);
	else
		sparc_set (code, 0, sparc_o1);
	sparc_set (code, 0, sparc_o3);
	sparc_jmpl (code, sparc_o7, sparc_g0, sparc_o7);
	sparc_nop (code);

	/* Save result */
	sparc_sti_imm (code, sparc_o0, sparc_sp, MONO_SPARC_STACK_BIAS + 304);

	/* Check for thread interruption */
	sparc_set (code, (guint8*)mono_thread_force_interruption_checkpoint, sparc_o7);
	sparc_jmpl (code, sparc_o7, sparc_g0, sparc_o7);
	sparc_nop (code);

	/* Restore lmf */
	code = mono_sparc_emit_restore_lmf (code, lmf_offset);

	/* Reload result */
	sparc_ldi_imm (code, sparc_sp, MONO_SPARC_STACK_BIAS + 304, sparc_o0);

#ifdef SPARCV9
	/* Reload fp regs */
	for (i = 0; i < 16; i ++)
		sparc_lddf_imm (code, sparc_sp, MONO_SPARC_STACK_BIAS + 320 + (i * 8), sparc_f0 + (i * 2));
#endif	

	if (tramp_type == MONO_TRAMPOLINE_CLASS_INIT)
		sparc_ret (code);
	else
		sparc_jmpl (code, sparc_o0, sparc_g0, sparc_g0);

	/* restore previous frame in delay slot */
	sparc_restore_simple (code);

/*
{
	gpointer addr;

	sparc_save_imm (code, sparc_sp, -608, sparc_sp);
	addr = code;
	sparc_call_simple (code, 16);
	sparc_nop (code);
	sparc_rett_simple (code);
	sparc_nop (code);

	sparc_save_imm (code, sparc_sp, -608, sparc_sp);
	sparc_ta (code, 1);
	tramp_addr = &sparc_magic_trampoline;
	sparc_call_simple (code, tramp_addr - code);
	sparc_nop (code);
	sparc_rett_simple (code);
	sparc_nop (code);
}
*/

	g_assert ((code - buf) <= 512);

	mono_arch_flush_icache (buf, code - buf);

	return buf;
}

#define TRAMPOLINE_SIZE (SPARC_SET_MAX_SIZE + 3)

gpointer
mono_arch_create_specific_trampoline (gpointer arg1, MonoTrampolineType tramp_type, MonoDomain *domain, guint32 *code_len)
{
	guint32 *code, *buf, *tramp;

	tramp = mono_get_trampoline_code (tramp_type);

	mono_domain_lock (domain);
	code = buf = mono_code_manager_reserve (domain->code_mp, TRAMPOLINE_SIZE * 4);
	mono_domain_unlock (domain);

	/* We have to use g5 here because there is no other free register */
	sparc_set (code, tramp, sparc_g5);
	sparc_jmpl (code, sparc_g5, sparc_g0, sparc_g5);
	sparc_nop (code);
#ifdef SPARCV9
	g_assert_not_reached ();
#else
	*code = (guint32)arg1;
	code ++;
#endif

	g_assert ((code - buf) <= TRAMPOLINE_SIZE);

	if (code_len)
		*code_len = (code - buf) * 4;

	mono_jit_stats.method_trampolines++;

	mono_arch_flush_icache ((guint8*)buf, (code - buf) * 4);

	return buf;
}	

gpointer
mono_arch_create_rgctx_lazy_fetch_trampoline (guint32 encoded_offset)
{
	/* FIXME: implement! */
	g_assert_not_reached ();
	return NULL;
}

guint32
mono_arch_get_rgctx_lazy_fetch_offset (gpointer *regs)
{
	/* FIXME: implement! */
	g_assert_not_reached ();
	return 0;
}
