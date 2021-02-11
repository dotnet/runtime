/**
 * \file
 * JIT trampoline code for MIPS
 *
 * Authors:
 *    Mark Mason (mason@broadcom.com)
 *
 * Based on tramp-ppc.c by:
 *   Dietmar Maurer (dietmar@ximian.com)
 *   Paolo Molaro (lupus@ximian.com)
 *   Carlos Valiente <yo@virutass.net>
 *
 * (C) 2006 Broadcom
 * (C) 2001 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>

#include <mono/metadata/abi-details.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/marshal.h>
#include <mono/metadata/tabledefs.h>
#include <mono/arch/mips/mips-codegen.h>

#include "mini.h"
#include "mini-mips.h"
#include "mini-runtime.h"
#include "mono/utils/mono-tls-inline.h"

/*
 * get_unbox_trampoline:
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
	MonoDomain *domain = mono_domain_get ();
	MonoMemoryManager *mem_manager = m_method_get_mem_manager (domain, m);
	    
	start = code = mono_mem_manager_code_reserve (mem_manager, 20);

	mips_load (code, mips_t9, addr);
	/* The this pointer is kept in a0 */
	mips_addiu (code, mips_a0, mips_a0, MONO_ABI_SIZEOF (MonoObject));
	mips_jr (code, mips_t9);
	mips_nop (code);

	mono_arch_flush_icache (start, code - start);
	MONO_PROFILER_RAISE (jit_code_buffer, (start, code - start, MONO_PROFILER_CODE_BUFFER_UNBOX_TRAMPOLINE, m));

	g_assert ((code - start) <= 20);
	/*g_print ("unbox trampoline at %d for %s:%s\n", this_pos, m->klass->name, m->name);
	g_print ("unbox code is at %p for method at %p\n", start, addr);*/

	mono_tramp_info_register (mono_tramp_info_create (NULL, start, code - start, NULL, NULL), domain);

	return start;
}

void
mono_arch_patch_callsite (guint8 *method_start, guint8 *orig_code, guint8 *addr)
{
	guint32 *code = (guint32*)orig_code;

	/* Locate the address of the method-specific trampoline.
	The call using the vtable slot that took the processing flow to
	'arch_create_jit_trampoline' looks something like one of these:

		jal	XXXXYYYY
		nop

		lui	t9, XXXX
		addiu	t9, YYYY
		jalr	t9
		nop

	On entry, 'code' points just after one of the above sequences.
	*/
	
	/* The jal case */
	if ((code[-2] >> 26) == 0x03) {
		//g_print ("direct patching\n");
		mips_patch ((code-2), (gsize)addr);
		return;
	}
	/* Look for the jalr */
	if ((code[-2] & 0xfc1f003f) == 0x00000009) {
		/* The lui / addiu / jalr case */
		if ((code [-4] >> 26) == 0x0f && (code [-3] >> 26) == 0x09
		    && (code [-2] >> 26) == 0) {
			mips_patch ((code-4), (gsize)addr);
			return;
		}
	}
	g_print("error: bad patch at 0x%08x\n", code);
	g_assert_not_reached ();
}

void
mono_arch_patch_plt_entry (guint8 *code, gpointer *got, host_mgreg_t *regs, guint8 *addr)
{
	g_assert_not_reached ();
}

/* Stack size for trampoline function 
 * MIPS_MINIMAL_STACK_SIZE + 16 (args + alignment to mips_magic_trampoline)
 * + MonoLMF + 14 fp regs + 13 gregs + alignment
 * #define STACK (MIPS_MINIMAL_STACK_SIZE + 4 * sizeof (gulong) + sizeof (MonoLMF) + 14 * sizeof (double) + 13 * (sizeof (gulong)))
 * STACK would be 444 for 32 bit darwin
 */

#define STACK (int)(ALIGN_TO(4*IREG_SIZE + 8 + sizeof(MonoLMF) + 32, 8))

/*
 * Stack frame description when the generic trampoline is called.
 * caller frame
 * --------------------
 *  MonoLMF
 *  -------------------
 *  Saved FP registers 0-13
 *  -------------------
 *  Saved general registers 0-12
 *  -------------------
 *  param area for 3 args to mips_magic_trampoline
 *  -------------------
 *  linkage area
 *  -------------------
 */
guchar*
mono_arch_create_generic_trampoline (MonoTrampolineType tramp_type, MonoTrampInfo **info, gboolean aot)
{
	const char *tramp_name;
	guint8 *buf, *tramp, *code = NULL;
	int i, lmf;
	GSList *unwind_ops = NULL;
	MonoJumpInfo *ji = NULL;
	int max_code_len = 768;

	/* AOT not supported on MIPS yet */
	g_assert (!aot);

	/* Now we'll create in 'buf' the MIPS trampoline code. This
	   is the trampoline code common to all methods  */

	code = buf = mono_global_codeman_reserve (max_code_len);

	/* Allocate the stack frame, and save the return address */
	mips_addiu (code, mips_sp, mips_sp, -STACK);
	mips_sw (code, mips_ra, mips_sp, STACK + MIPS_RET_ADDR_OFFSET);

	/* we build the MonoLMF structure on the stack - see mini-mips.h */
	/* offset of MonoLMF from sp */
	lmf = STACK - sizeof (MonoLMF) - 8;

	for (i = 0; i < MONO_MAX_IREGS; i++)
		MIPS_SW (code, i, mips_sp, lmf + G_STRUCT_OFFSET (MonoLMF, iregs[i]));
	for (i = 0; i < MONO_MAX_FREGS; i++)
		MIPS_SWC1 (code, i, mips_sp, lmf + G_STRUCT_OFFSET (MonoLMF, fregs[i]));

	/* Set the magic number */
	mips_load_const (code, mips_at, MIPS_LMF_MAGIC2);
	mips_sw (code, mips_at, mips_sp, lmf + G_STRUCT_OFFSET(MonoLMF, magic));

	/* Save caller sp */
	mips_addiu (code, mips_at, mips_sp, STACK);
	MIPS_SW (code, mips_at, mips_sp, lmf + G_STRUCT_OFFSET (MonoLMF, iregs[mips_sp]));

	/* save method info (it was in t8) */
	mips_sw (code, mips_t8, mips_sp, lmf + G_STRUCT_OFFSET(MonoLMF, method));

	/* save the IP (caller ip) */
	if (tramp_type == MONO_TRAMPOLINE_JUMP) {
		mips_sw (code, mips_zero, mips_sp, lmf + G_STRUCT_OFFSET(MonoLMF, eip));
	} else {
		mips_sw (code, mips_ra, mips_sp, lmf + G_STRUCT_OFFSET(MonoLMF, eip));
	}

	/* jump to mono_get_lmf_addr here */
	mips_load (code, mips_t9, mono_get_lmf_addr);
	mips_jalr (code, mips_t9, mips_ra);
	mips_nop (code);

	/* v0 now points at the (MonoLMF **) for the current thread */

	/* new_lmf->lmf_addr = lmf_addr -- useful when unwinding */
	mips_sw (code, mips_v0, mips_sp, lmf + G_STRUCT_OFFSET(MonoLMF, lmf_addr));

	/* new_lmf->previous_lmf = *lmf_addr */
	mips_lw (code, mips_at, mips_v0, 0);
	mips_sw (code, mips_at, mips_sp, lmf + G_STRUCT_OFFSET(MonoLMF, previous_lmf));

	/* *(lmf_addr) = new_lmf */
	mips_addiu (code, mips_at, mips_sp, lmf);
	mips_sw (code, mips_at, mips_v0, 0);

	/*
	 * Now we're ready to call mips_magic_trampoline ().
	 */

	/* Arg 1: pointer to registers so that the magic trampoline can
	 * access what we saved above
	 */
	mips_addiu (code, mips_a0, mips_sp, lmf + G_STRUCT_OFFSET (MonoLMF, iregs[0]));

	/* Arg 2: code (next address to the instruction that called us) */
	if (tramp_type == MONO_TRAMPOLINE_JUMP) {
		mips_move (code, mips_a1, mips_zero);
	} else {
		mips_lw (code, mips_a1, mips_sp, STACK + MIPS_RET_ADDR_OFFSET);
	}

	/* Arg 3: MonoMethod *method. */
	mips_lw (code, mips_a2, mips_sp, lmf + G_STRUCT_OFFSET (MonoLMF, method));

	/* Arg 4: Trampoline */
	mips_move (code, mips_a3, mips_zero);
		
	/* Now go to the trampoline */
	tramp = (guint8*)mono_get_trampoline_func (tramp_type);
	mips_load (code, mips_t9, (guint32)tramp);
	mips_jalr (code, mips_t9, mips_ra);
	mips_nop (code);
		
	/* Code address is now in v0, move it to at */
	mips_move (code, mips_at, mips_v0);

	/*
	 * Now unwind the MonoLMF
	 */

	/* t0 = current_lmf->previous_lmf */
	mips_lw (code, mips_t0, mips_sp, lmf + G_STRUCT_OFFSET(MonoLMF, previous_lmf));
	/* t1 = lmf_addr */
	mips_lw (code, mips_t1, mips_sp, lmf + G_STRUCT_OFFSET(MonoLMF, lmf_addr));
	/* (*lmf_addr) = previous_lmf */
	mips_sw (code, mips_t0, mips_t1, 0);

	/* Restore the callee-saved & argument registers */
	for (i = 0; i < MONO_MAX_IREGS; i++) {
		if ((MONO_ARCH_CALLEE_SAVED_REGS | MONO_ARCH_CALLEE_REGS | MIPS_ARG_REGS) & (1 << i))
		    MIPS_LW (code, i, mips_sp, lmf + G_STRUCT_OFFSET (MonoLMF, iregs[i]));
	}
	for (i = 0; i < MONO_MAX_FREGS; i++)
		MIPS_LWC1 (code, i, mips_sp, lmf + G_STRUCT_OFFSET (MonoLMF, fregs[i]));

	/* Non-standard function epilogue. Instead of doing a proper
	 * return, we just jump to the compiled code.
	 */
	/* Restore ra & stack pointer, and jump to the code */

	if (tramp_type == MONO_TRAMPOLINE_RGCTX_LAZY_FETCH)
		mips_move (code, mips_v0, mips_at);
	mips_lw (code, mips_ra, mips_sp, STACK + MIPS_RET_ADDR_OFFSET);
	mips_addiu (code, mips_sp, mips_sp, STACK);
	if (MONO_TRAMPOLINE_TYPE_MUST_RETURN (tramp_type))
		mips_jr (code, mips_ra);
	else
		mips_jr (code, mips_at);
	mips_nop (code);

	/* Flush instruction cache, since we've generated code */
	mono_arch_flush_icache (buf, code - buf);
	MONO_PROFILER_RAISE (jit_code_buffer, (buf, code - buf, MONO_PROFILER_CODE_BUFFER_HELPER, NULL));

	/* Sanity check */
	g_assert ((code - buf) <= max_code_len);

	g_assert (info);
	tramp_name = mono_get_generic_trampoline_name (tramp_type);
	*info = mono_tramp_info_create (tramp_name, buf, code - buf, ji, unwind_ops);

	return buf;
}

gpointer
mono_arch_create_specific_trampoline (gpointer arg1, MonoTrampolineType tramp_type, MonoMemoryManager *mem_manager, guint32 *code_len)
{
	guint8 *code, *buf, *tramp;

	tramp = mono_get_trampoline_code (tramp_type);

	code = buf = mono_mem_manager_code_reserve (domain, 32);

	/* Prepare the jump to the generic trampoline code
	 * mono_arch_create_trampoline_code() knows we're putting this in t8
	 */
	mips_load (code, mips_t8, arg1);
	
	/* Now jump to the generic trampoline code */
	mips_load (code, mips_at, tramp);
	mips_jr (code, mips_at);
	mips_nop (code);

	/* Flush instruction cache, since we've generated code */
	mono_arch_flush_icache (buf, code - buf);
	MONO_PROFILER_RAISE (jit_code_buffer, (buf, code - buf, MONO_PROFILER_CODE_BUFFER_SPECIFIC_TRAMPOLINE, mono_get_generic_trampoline_simple_name (tramp_type)));

	g_assert ((code - buf) <= 32);

	if (code_len)
		*code_len = code - buf;

	return buf;
}

gpointer
mono_arch_get_static_rgctx_trampoline (MonoMemoryManager *mem_manager, gpointer arg, gpointer addr)
{
	guint8 *code, *start;
	int buf_len;
	MonoDomain *domain = mono_domain_get ();

	buf_len = 24;

	start = code = mono_mem_manager_code_reserve (mem_manager, buf_len);

	mips_load (code, MONO_ARCH_RGCTX_REG, arg);
	mips_load (code, mips_at, addr);
	mips_jr (code, mips_at);
	mips_nop (code);

	g_assert ((code - start) <= buf_len);

	mono_arch_flush_icache (start, code - start);
	MONO_PROFILER_RAISE (jit_code_buffer, (start, code - start, MONO_PROFILER_CODE_BUFFER_GENERICS_TRAMPOLINE, NULL));

	mono_tramp_info_register (mono_tramp_info_create (NULL, start, code - start, NULL, NULL), domain);

	return start;
}

gpointer
mono_arch_create_rgctx_lazy_fetch_trampoline (guint32 slot, MonoTrampInfo **info, gboolean aot)
{
	guint8 *tramp;
	guint8 *code, *buf;
	int tramp_size;
	guint32 code_len;
	guint8 **rgctx_null_jumps;
	int depth, index;
	int i, njumps;
	gboolean mrgctx;
	MonoJumpInfo *ji = NULL;
	GSList *unwind_ops = NULL;

	mrgctx = MONO_RGCTX_SLOT_IS_MRGCTX (slot);
	index = MONO_RGCTX_SLOT_INDEX (slot);
	if (mrgctx)
		index += MONO_SIZEOF_METHOD_RUNTIME_GENERIC_CONTEXT / sizeof (target_mgreg_t);
	for (depth = 0; ; ++depth) {
		int size = mono_class_rgctx_get_array_size (depth, mrgctx);

		if (index < size - 1)
			break;
		index -= size - 1;
	}

	tramp_size = 64 + 16 * depth;

	code = buf = mono_global_codeman_reserve (tramp_size);

	mono_add_unwind_op_def_cfa (unwind_ops, code, buf, mips_sp, 0);

	rgctx_null_jumps = g_malloc (sizeof (guint8*) * (depth + 2));
	njumps = 0;

	/* The vtable/mrgctx is in a0 */
	g_assert (MONO_ARCH_VTABLE_REG == mips_a0);
	if (mrgctx) {
		/* get mrgctx ptr */
		mips_move (code, mips_a1, mips_a0);
 	} else {
		/* load rgctx ptr from vtable */
		g_assert (mips_is_imm16 (MONO_STRUCT_OFFSET (MonoVTable, runtime_generic_context)));
		mips_lw (code, mips_a1, mips_a0, MONO_STRUCT_OFFSET (MonoVTable, runtime_generic_context));
		/* is the rgctx ptr null? */
		/* if yes, jump to actual trampoline */
		rgctx_null_jumps [njumps ++] = code;
		mips_beq (code, mips_a1, mips_zero, 0);
		mips_nop (code);
	}

	for (i = 0; i < depth; ++i) {
		/* load ptr to next array */
		if (mrgctx && i == 0) {
			g_assert (mips_is_imm16 (MONO_SIZEOF_METHOD_RUNTIME_GENERIC_CONTEXT));
			mips_lw (code, mips_a1, mips_a1, MONO_SIZEOF_METHOD_RUNTIME_GENERIC_CONTEXT);
		} else {
			mips_lw (code, mips_a1, mips_a1, 0);
		}
		/* is the ptr null? */
		/* if yes, jump to actual trampoline */
		rgctx_null_jumps [njumps ++] = code;
		mips_beq (code, mips_a1, mips_zero, 0);
		mips_nop (code);
	}

	/* fetch slot */
	g_assert (mips_is_imm16 (sizeof (target_mgreg_t) * (index + 1)));
	mips_lw (code, mips_a1, mips_a1, sizeof (target_mgreg_t) * (index + 1));
	/* is the slot null? */
	/* if yes, jump to actual trampoline */
	rgctx_null_jumps [njumps ++] = code;
	mips_beq (code, mips_a1, mips_zero, 0);
	mips_nop (code);
	/* otherwise return, result is in R1 */
	mips_move (code, mips_v0, mips_a1);
	mips_jr (code, mips_ra);
	mips_nop (code);

	g_assert (njumps <= depth + 2);
	for (i = 0; i < njumps; ++i)
		mips_patch ((guint32*)rgctx_null_jumps [i], (guint32)code);

	g_free (rgctx_null_jumps);

	/* Slowpath */

	/* The vtable/mrgctx is still in a0 */

	if (aot) {
		ji = mono_patch_info_list_prepend (ji, code - buf, MONO_PATCH_INFO_SPECIFIC_TRAMPOLINE_LAZY_FETCH_ADDR, GUINT_TO_POINTER (slot));
		mips_load (code, mips_at, 0);
		mips_jr (code, mips_at);
		mips_nop (code);
	} else {
		MonoMemoryManager *mem_manager = mono_domain_ambient_memory_manager (mono_get_root_domain ());
		tramp = (guint8*)mono_arch_create_specific_trampoline (GUINT_TO_POINTER (slot), MONO_TRAMPOLINE_RGCTX_LAZY_FETCH, mem_manager, &code_len);
		mips_load (code, mips_at, tramp);
		mips_jr (code, mips_at);
		mips_nop (code);
	}

	mono_arch_flush_icache (buf, code - buf);
	MONO_PROFILER_RAISE (jit_code_buffer, (buf, code - buf, MONO_PROFILER_CODE_BUFFER_GENERICS_TRAMPOLINE, NULL));

	g_assert (code - buf <= tramp_size);

	if (info) {
		char *name = mono_get_rgctx_fetch_trampoline_name (slot);
		*info = mono_tramp_info_create (name, buf, code - buf, ji, unwind_ops);
		g_free (name);
	}

	return buf;
}
