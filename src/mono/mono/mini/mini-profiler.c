/*
 * Licensed to the .NET Foundation under one or more agreements.
 * The .NET Foundation licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
 */

#include <config.h>

#include <mono/metadata/abi-details.h>
#include <mono/metadata/mono-debug.h>

#include "interp/interp.h"
#include "ir-emit.h"
#include "mini.h"

#ifndef DISABLE_JIT

static MonoInst *
emit_fill_call_ctx (MonoCompile *cfg, MonoInst *method, MonoInst *ret)
{
	cfg->flags |= MONO_CFG_HAS_ALLOCA;

	MonoInst *alloc, *size, *fill_ctx;

	EMIT_NEW_ICONST (cfg, size, sizeof (MonoProfilerCallContext));
	MONO_INST_NEW (cfg, alloc, OP_LOCALLOC);
	alloc->dreg = alloc_preg (cfg);
	alloc->sreg1 = size->dreg;
	alloc->flags |= MONO_INST_INIT;
	MONO_ADD_INS (cfg->cbb, alloc);
	MONO_INST_NEW (cfg, fill_ctx, OP_FILL_PROF_CALL_CTX);
	fill_ctx->sreg1 = alloc->dreg;
	MONO_ADD_INS (cfg->cbb, fill_ctx);
	MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORE_MEMBASE_REG, alloc->dreg, MONO_STRUCT_OFFSET (MonoProfilerCallContext, method), method->dreg);

	if (ret) {
		MonoInst *var = mono_compile_create_var (cfg, mono_method_signature (cfg->method)->ret, OP_LOCAL);

		MonoInst *store, *addr;

		EMIT_NEW_TEMPSTORE (cfg, store, var->inst_c0, ret);
		EMIT_NEW_VARLOADA (cfg, addr, var, NULL);
		MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORE_MEMBASE_REG, alloc->dreg, MONO_STRUCT_OFFSET (MonoProfilerCallContext, return_value), addr->dreg);
	}

	return alloc;
}

void
mini_profiler_emit_enter (MonoCompile *cfg)
{
	if (!MONO_CFG_PROFILE (cfg, ENTER) || cfg->current_method != cfg->method)
		return;

	MonoInst *iargs [2];

	EMIT_NEW_METHODCONST (cfg, iargs [0], cfg->method);

	if (MONO_CFG_PROFILE (cfg, ENTER_CONTEXT) && !cfg->llvm_only)
		iargs [1] = emit_fill_call_ctx (cfg, iargs [0], NULL);
	else
		EMIT_NEW_PCONST (cfg, iargs [1], NULL);

	/* void mono_profiler_raise_method_enter (MonoMethod *method, MonoProfilerCallContext *ctx) */
	mono_emit_jit_icall (cfg, mono_profiler_raise_method_enter, iargs);
}

void
mini_profiler_emit_leave (MonoCompile *cfg, MonoInst *ret)
{
	if (!MONO_CFG_PROFILE (cfg, LEAVE) || cfg->current_method != cfg->method)
		return;

	MonoInst *iargs [2];

	EMIT_NEW_METHODCONST (cfg, iargs [0], cfg->method);

	if (MONO_CFG_PROFILE (cfg, LEAVE_CONTEXT) && !cfg->llvm_only)
		iargs [1] = emit_fill_call_ctx (cfg, iargs [0], ret);
	else
		EMIT_NEW_PCONST (cfg, iargs [1], NULL);

	/* void mono_profiler_raise_method_leave (MonoMethod *method, MonoProfilerCallContext *ctx) */
	mono_emit_jit_icall (cfg, mono_profiler_raise_method_leave, iargs);
}

void
mini_profiler_emit_tail_call (MonoCompile *cfg, MonoMethod *target)
{
	if (!MONO_CFG_PROFILE (cfg, TAIL_CALL) || cfg->current_method != cfg->method)
		return;

	g_assert (cfg->current_method == cfg->method);

	MonoInst *iargs [2];

	EMIT_NEW_METHODCONST (cfg, iargs [0], cfg->method);

	if (target)
		EMIT_NEW_METHODCONST (cfg, iargs [1], target);
	else
		EMIT_NEW_PCONST (cfg, iargs [1], NULL);

	/* void mono_profiler_raise_method_tail_call (MonoMethod *method, MonoMethod *target) */
	mono_emit_jit_icall (cfg, mono_profiler_raise_method_tail_call, iargs);
}

#endif

void
mini_profiler_context_enable (void)
{
	if (!mono_debug_enabled ())
		mono_debug_init (MONO_DEBUG_FORMAT_MONO);
}

static gpointer
memdup_with_type (gpointer data, MonoType *t)
{
	int dummy;

	return g_memdup (data, mono_type_size (t, &dummy));
}

static guint8 *
get_int_reg (MonoContext *ctx, guint32 reg)
{
	return (guint8 *) mono_arch_context_get_int_reg (ctx, reg);
}

static gpointer
get_variable_buffer (MonoDebugMethodJitInfo *jit, MonoDebugVarInfo *var, MonoContext *ctx)
{
	guint32 flags = var->index & MONO_DEBUG_VAR_ADDRESS_MODE_FLAGS;
	guint32 reg = var->index & ~MONO_DEBUG_VAR_ADDRESS_MODE_FLAGS;

	switch (flags) {
	case MONO_DEBUG_VAR_ADDRESS_MODE_REGISTER: {
		/*
		 * This is kind of a special case: All other address modes ultimately
		 * produce an address to where the actual value is located, but this
		 * address mode gets us the value itself as an mgreg_t value.
		 */
		mgreg_t value = (mgreg_t) get_int_reg (ctx, reg);

		return memdup_with_type (&value, var->type);
	}
	case MONO_DEBUG_VAR_ADDRESS_MODE_REGOFFSET:
		return memdup_with_type (get_int_reg (ctx, reg) + (gint32) var->offset, var->type);
	case MONO_DEBUG_VAR_ADDRESS_MODE_REGOFFSET_INDIR:
	case MONO_DEBUG_VAR_ADDRESS_MODE_VTADDR:
		return memdup_with_type (*(guint8 **) (get_int_reg (ctx, reg) + (gint32) var->offset), var->type);
	case MONO_DEBUG_VAR_ADDRESS_MODE_GSHAREDVT_LOCAL: {
		guint32 idx = reg;

		MonoDebugVarInfo *info_var = jit->gsharedvt_info_var;

		flags = info_var->index & MONO_DEBUG_VAR_ADDRESS_MODE_FLAGS;
		reg = info_var->index & ~MONO_DEBUG_VAR_ADDRESS_MODE_FLAGS;

		MonoGSharedVtMethodRuntimeInfo *info;

		switch (flags) {
		case MONO_DEBUG_VAR_ADDRESS_MODE_REGISTER:
			info = (MonoGSharedVtMethodRuntimeInfo *) get_int_reg (ctx, reg);
			break;
		case MONO_DEBUG_VAR_ADDRESS_MODE_REGOFFSET:
			info = *(MonoGSharedVtMethodRuntimeInfo **) (get_int_reg (ctx, reg) + (gint32) info_var->offset);
			break;
		default:
			g_assert_not_reached ();
		}

		MonoDebugVarInfo *locals_var = jit->gsharedvt_locals_var;

		flags = locals_var->index & MONO_DEBUG_VAR_ADDRESS_MODE_FLAGS;
		reg = locals_var->index & ~MONO_DEBUG_VAR_ADDRESS_MODE_FLAGS;

		guint8 *locals;

		switch (flags) {
		case MONO_DEBUG_VAR_ADDRESS_MODE_REGISTER:
			locals = get_int_reg (ctx, reg);
			break;
		case MONO_DEBUG_VAR_ADDRESS_MODE_REGOFFSET:
			locals = *(guint8 **) (get_int_reg (ctx, reg) + (gint32) info_var->offset);
			break;
		default:
			g_assert_not_reached ();
		}

		return memdup_with_type (locals + (gsize) info->entries [idx], var->type);
	}
	default:
		g_assert_not_reached ();
		return NULL;
	}
}

gpointer
mini_profiler_context_get_this (MonoProfilerCallContext *ctx)
{
	if (!mono_method_signature (ctx->method)->hasthis)
		return NULL;

	if (ctx->interp_frame)
		return memdup_with_type (mini_get_interp_callbacks ()->frame_get_this (ctx->interp_frame), &ctx->method->klass->this_arg);

	MonoDebugMethodJitInfo *info = mono_debug_find_method (ctx->method, mono_domain_get ());

	if (!info)
		return NULL;

	return get_variable_buffer (info, info->this_var, &ctx->context);
}

gpointer
mini_profiler_context_get_argument (MonoProfilerCallContext *ctx, guint32 pos)
{
	MonoMethodSignature *sig = mono_method_signature (ctx->method);

	if (pos >= sig->param_count)
		return NULL;

	if (ctx->interp_frame)
		return memdup_with_type (mini_get_interp_callbacks ()->frame_get_arg (ctx->interp_frame, pos), sig->params [pos]);

	MonoDebugMethodJitInfo *info = mono_debug_find_method (ctx->method, mono_domain_get ());

	if (!info)
		return NULL;

	return get_variable_buffer (info, &info->params [pos], &ctx->context);
}

gpointer
mini_profiler_context_get_local (MonoProfilerCallContext *ctx, guint32 pos)
{
	MonoError error;
	MonoMethodHeader *header = mono_method_get_header_checked (ctx->method, &error);
	mono_error_assert_ok (&error); // Must be a valid method at this point.

	if (pos >= header->num_locals) {
		mono_metadata_free_mh (header);
		return NULL;
	}

	MonoType *t = header->locals [pos];

	mono_metadata_free_mh (header);

	if (ctx->interp_frame)
		return memdup_with_type (mini_get_interp_callbacks ()->frame_get_local (ctx->interp_frame, pos), t);

	MonoDebugMethodJitInfo *info = mono_debug_find_method (ctx->method, mono_domain_get ());

	if (!info)
		return NULL;

	return get_variable_buffer (info, &info->locals [pos], &ctx->context);
}

gpointer
mini_profiler_context_get_result (MonoProfilerCallContext *ctx)
{
	if (!ctx->return_value)
		return NULL;

	return memdup_with_type (ctx->return_value, mono_method_signature (ctx->method)->ret);
}

void
mini_profiler_context_free_buffer (void *buffer)
{
	g_free (buffer);
}
