/*
 * Licensed to the .NET Foundation under one or more agreements.
 * The .NET Foundation licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
 */

#include <mono/metadata/abi-details.h>
#include <mono/metadata/mono-debug.h>

#include "interp/interp.h"
#include "ir-emit.h"
#include "mini.h"

void
mini_profiler_emit_instrumentation_call (MonoCompile *cfg, void *func, gboolean entry, MonoInst **ret, MonoType *rtype)
{
	gboolean instrument, capture;

	if (entry) {
		instrument = cfg->prof_flags & MONO_PROFILER_CALL_INSTRUMENTATION_PROLOGUE;
		capture = cfg->prof_flags & MONO_PROFILER_CALL_INSTRUMENTATION_PROLOGUE_CONTEXT;
	} else {
		instrument = cfg->prof_flags & MONO_PROFILER_CALL_INSTRUMENTATION_EPILOGUE;
		capture = cfg->prof_flags & MONO_PROFILER_CALL_INSTRUMENTATION_EPILOGUE_CONTEXT;
	}

	if (!instrument)
		return;

	g_assert (cfg->current_method == cfg->method);

	MonoInst *iargs [2];

	EMIT_NEW_METHODCONST (cfg, iargs [0], cfg->method);

	if (capture && !cfg->llvm_only) {
		cfg->flags |= MONO_CFG_HAS_ALLOCA;

		MonoInst *size, *fill_ctx;

		EMIT_NEW_ICONST (cfg, size, sizeof (MonoProfilerCallContext));
		MONO_INST_NEW (cfg, iargs [1], OP_LOCALLOC);
		iargs [1]->dreg = alloc_preg (cfg);
		iargs [1]->sreg1 = size->dreg;
		iargs [1]->flags |= MONO_INST_INIT;
		MONO_ADD_INS (cfg->cbb, iargs [1]);
		MONO_INST_NEW (cfg, fill_ctx, OP_FILL_PROF_CALL_CTX);
		fill_ctx->sreg1 = iargs [1]->dreg;
		MONO_ADD_INS (cfg->cbb, fill_ctx);
		MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORE_MEMBASE_REG, iargs [1]->dreg, MONO_STRUCT_OFFSET (MonoProfilerCallContext, method), iargs [0]->dreg);

		if (rtype && rtype->type != MONO_TYPE_VOID) {
			MonoInst *var = mono_compile_create_var (cfg, rtype, OP_LOCAL);

			MonoInst *store, *addr;

			EMIT_NEW_TEMPSTORE (cfg, store, var->inst_c0, *ret);
			EMIT_NEW_VARLOADA (cfg, addr, var, NULL);
			MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORE_MEMBASE_REG, iargs [1]->dreg, MONO_STRUCT_OFFSET (MonoProfilerCallContext, return_value), addr->dreg);
		}
	} else
		EMIT_NEW_PCONST (cfg, iargs [1], NULL);

	mono_emit_jit_icall (cfg, func, iargs);
}

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
		return memdup_with_type (mono_interp_frame_get_this (ctx->interp_frame), &ctx->method->klass->this_arg);

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
		return memdup_with_type (mono_interp_frame_get_arg (ctx->interp_frame, pos), sig->params [pos]);

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
		return memdup_with_type (mono_interp_frame_get_local (ctx->interp_frame, pos), t);

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
