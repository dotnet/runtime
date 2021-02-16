/*
 * Licensed to the .NET Foundation under one or more agreements.
 * The .NET Foundation licenses this file to you under the MIT license.
 */

#include "mini-runtime.h"

#include <mono/metadata/abi-details.h>

void
mono_arch_patch_callsite (guint8 *method_start, guint8 *code_ptr, guint8 *addr)
{
	NOT_IMPLEMENTED;
}

void
mono_arch_patch_plt_entry (guint8 *code, gpointer *got, host_mgreg_t *regs, guint8 *addr)
{
	NOT_IMPLEMENTED;
}

guint8 *
mono_arch_get_call_target (guint8 *code)
{
	NOT_IMPLEMENTED;
	return NULL;
}

guint32
mono_arch_get_plt_info_offset (guint8 *plt_entry, host_mgreg_t *regs, guint8 *code)
{
	NOT_IMPLEMENTED;
	return 0;
}

GSList *
mono_arch_get_delegate_invoke_impls (void)
{
	NOT_IMPLEMENTED;
	return NULL;
}

gpointer
mono_arch_get_delegate_invoke_impl (MonoMethodSignature *sig, gboolean has_target)
{
	NOT_IMPLEMENTED;
	return NULL;
}

gpointer
mono_arch_get_delegate_virtual_invoke_impl (MonoMethodSignature *sig,
                                            MonoMethod *method, int offset,
                                            gboolean load_imt_reg)
{
	NOT_IMPLEMENTED;
	return NULL;
}

#ifndef DISABLE_JIT

guchar *
mono_arch_create_generic_trampoline (MonoTrampolineType tramp_type, MonoTrampInfo **info,
                                     gboolean aot)
{
	if (aot)
		NOT_IMPLEMENTED;

	guint8 *buf = mono_global_codeman_reserve (1024), *code = buf;

	if (info) {
		const char *name = mono_get_generic_trampoline_name (tramp_type);
		*info = mono_tramp_info_create (name, buf, code - buf, NULL, NULL);
	}

	return buf;
}

gpointer
mono_arch_create_specific_trampoline (gpointer arg1, MonoTrampolineType tramp_type,
                                      MonoMemoryManager *mem_manager, guint32 *code_len)
{
	guint8 *buf = mono_mem_manager_code_reserve (mem_manager, 64), *code = buf;
	guint8 *tramp = mono_get_trampoline_code (tramp_type);

	// Pass the argument in scratch t0.
	code = mono_riscv_emit_imm (code, RISCV_T0, (gsize) arg1);
	code = mono_riscv_emit_imm (code, RISCV_T1, (gsize) tramp);
	riscv_jalr (code, RISCV_ZERO, RISCV_T1, 0);

	mono_arch_flush_icache (buf, code - buf);

	if (code_len)
		*code_len = code - buf;

	return buf;
}

gpointer
mono_arch_get_unbox_trampoline (MonoMethod *m, gpointer addr)
{
	MonoMemoryManager *mem_manager = m_method_get_mem_manager (m);
	guint8 *buf = mono_mem_manager_code_reserve (mem_manager, 64), *code = buf;

	// Pass the argument in a0.
	code = mono_riscv_emit_imm (code, RISCV_A0, sizeof (MonoObject));
	code = mono_riscv_emit_imm (code, RISCV_T0, (gsize) addr);
	riscv_jalr (code, RISCV_ZERO, RISCV_T0, 0);

	mono_arch_flush_icache (buf, code - buf);

	return buf;
}

gpointer
mono_arch_build_imt_trampoline (MonoVTable *vtable, MonoIMTCheckItem **imt_entries, int count,
                                gpointer fail_tramp)
{
	NOT_IMPLEMENTED;
	return NULL;
}

gpointer
mono_arch_get_static_rgctx_trampoline (MonoMemoryManager *mem_manager, gpointer arg, gpointer addr)
{
	guint8 *buf = mono_mem_manager_code_reserve (mem_manager, 64), *code = buf;

	// Pass the argument in the RGCTX register.
	code = mono_riscv_emit_imm (code, MONO_ARCH_RGCTX_REG, (gsize) arg);
	code = mono_riscv_emit_imm (code, RISCV_T0, (gsize) addr);
	riscv_jalr (code, RISCV_ZERO, RISCV_T0, 0);

	mono_arch_flush_icache (buf, code - buf);

	return buf;
}

gpointer
mono_arch_create_rgctx_lazy_fetch_trampoline (guint32 slot, MonoTrampInfo **info,
                                              gboolean aot)
{
	if (aot)
		NOT_IMPLEMENTED;

	gboolean is_mrgctx = MONO_RGCTX_SLOT_IS_MRGCTX (slot);
	int index = MONO_RGCTX_SLOT_INDEX (slot);

	if (is_mrgctx)
		index += MONO_SIZEOF_METHOD_RUNTIME_GENERIC_CONTEXT / sizeof (target_mgreg_t);

	int depth;

	for (depth = 0; ; depth++) {
		int size = mono_class_rgctx_get_array_size (depth, is_mrgctx);

		if (index < size - 1)
			break;

		index -= size - 1;
	}

	guint8 *buf = mono_global_codeman_reserve (128 * depth), *code = buf;
	guint8 **null_jumps = g_malloc0 (sizeof (guint8 *) * (depth + 2));

	if (!is_mrgctx) {
	} else
		riscv_addi (code, RISCV_T1, RISCV_A0, 0);

	mono_arch_flush_icache (buf, code - buf);

	if (info) {
		char *name = mono_get_rgctx_fetch_trampoline_name (slot);
		*info = mono_tramp_info_create (name, buf, code - buf, NULL, NULL);
		g_free (name);
	}

	return buf;
}

gpointer
mono_arch_create_general_rgctx_lazy_fetch_trampoline (MonoTrampInfo **info, gboolean aot)
{
	if (aot)
		NOT_IMPLEMENTED;

	guint8 *buf = mono_global_codeman_reserve (64), *code = buf;

	/*
	 * The RGCTX register holds a pointer to a <slot, trampoline address> pair.
	 * Load the trampoline address and branch to it. a0 holds the actual
	 * (M)RGCTX or VTable.
	 */
	code = mono_riscv_emit_load (code, RISCV_T0, MONO_ARCH_RGCTX_REG, sizeof (target_mgreg_t));
	riscv_jalr (code, RISCV_ZERO, RISCV_T0, 0);

	mono_arch_flush_icache (buf, code - buf);

	if (info)
		*info = mono_tramp_info_create ("rgctx_fetch_trampoline_general", buf, code - buf, NULL, NULL);

	return buf;
}

guint8 *
mono_arch_create_sdb_trampoline (gboolean single_step, MonoTrampInfo **info, gboolean aot)
{
	NOT_IMPLEMENTED;
	return NULL;
}

gpointer
mono_arch_get_interp_to_native_trampoline (MonoTrampInfo **info)
{
	NOT_IMPLEMENTED;
	return NULL;
}

gpointer
mono_arch_get_native_to_interp_trampoline (MonoTrampInfo **info)
{
	NOT_IMPLEMENTED;
	return NULL;
}

#else

guchar *
mono_arch_create_generic_trampoline (MonoTrampolineType tramp_type, MonoTrampInfo **info,
                                     gboolean aot)
{
	g_assert_not_reached ();
	return NULL;
}

gpointer
mono_arch_create_specific_trampoline (gpointer arg1, MonoTrampolineType tramp_type,
                                      MonoMemoryManager *mem_manager, guint32 *code_len)
{
	g_assert_not_reached ();
	return NULL;
}

gpointer
mono_arch_get_unbox_trampoline (MonoMethod *m, gpointer addr)
{
	g_assert_not_reached ();
	return NULL;
}

gpointer
mono_arch_build_imt_trampoline (MonoVTable *vtable, MonoIMTCheckItem **imt_entries, int count,
                                gpointer fail_tramp)
{
	g_assert_not_reached ();
	return NULL;
}

gpointer
mono_arch_get_static_rgctx_trampoline (MonoMemoryManager *mem_manager, gpointer arg, gpointer addr)
{
	g_assert_not_reached ();
	return NULL;
}

gpointer
mono_arch_create_rgctx_lazy_fetch_trampoline (guint32 slot, MonoTrampInfo **info,
                                              gboolean aot)
{
	g_assert_not_reached ();
	return NULL;
}

gpointer
mono_arch_create_general_rgctx_lazy_fetch_trampoline (MonoTrampInfo **info, gboolean aot)
{
	g_assert_not_reached ();
	return NULL;
}

guint8 *
mono_arch_create_sdb_trampoline (gboolean single_step, MonoTrampInfo **info, gboolean aot)
{
	g_assert_not_reached ();
	return NULL;
}

gpointer
mono_arch_get_interp_to_native_trampoline (MonoTrampInfo **info)
{
	g_assert_not_reached ();
	return NULL;
}

gpointer
mono_arch_get_native_to_interp_trampoline (MonoTrampInfo **info)
{
	g_assert_not_reached ();
	return NULL;
}

#endif
