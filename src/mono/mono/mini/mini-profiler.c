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
#include "trace.h"

#ifndef DISABLE_JIT

static MonoInst *
emit_fill_call_ctx (MonoCompile *cfg, MonoInst *method, MonoInst *ret)
{
	cfg->flags |= MONO_CFG_HAS_ALLOCA;

	MonoInst *alloc, *size;

	EMIT_NEW_ICONST (cfg, size, sizeof (MonoProfilerCallContext));
	MONO_INST_NEW (cfg, alloc, OP_LOCALLOC);
	alloc->dreg = alloc_preg (cfg);
	alloc->sreg1 = size->dreg;
	alloc->flags |= MONO_INST_INIT;
	MONO_ADD_INS (cfg->cbb, alloc);

	MonoInst *args_alloc, *ins;
	MonoMethodSignature *sig;

	sig = mono_method_signature_internal (cfg->method);

	MONO_INST_NEW (cfg, args_alloc, OP_LOCALLOC_IMM);
	args_alloc->dreg = alloc_preg (cfg);
	args_alloc->inst_imm = (sig->param_count + sig->hasthis) * TARGET_SIZEOF_VOID_P;
	args_alloc->flags |= MONO_INST_INIT;
	MONO_ADD_INS (cfg->cbb, args_alloc);
	MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORE_MEMBASE_REG, alloc->dreg, MONO_STRUCT_OFFSET (MonoProfilerCallContext, args), args_alloc->dreg);

	for (int i = 0; i < sig->hasthis + sig->param_count; ++i) {
		NEW_VARLOADA (cfg, ins, cfg->args [i], cfg->args [i]->inst_vtype);
		MONO_ADD_INS (cfg->cbb, ins);

		MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORE_MEMBASE_REG, args_alloc->dreg, i * TARGET_SIZEOF_VOID_P, ins->dreg);
	}

	MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORE_MEMBASE_REG, alloc->dreg, MONO_STRUCT_OFFSET (MonoProfilerCallContext, method), method->dreg);

	if (ret) {
		MonoInst *var = mono_compile_create_var (cfg, mono_method_signature_internal (cfg->method)->ret, OP_LOCAL);

		MonoInst *store, *addr;

		EMIT_NEW_TEMPSTORE (cfg, store, var->inst_c0, ret);
		EMIT_NEW_VARLOADA (cfg, addr, var, NULL);
		MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORE_MEMBASE_REG, alloc->dreg, MONO_STRUCT_OFFSET (MonoProfilerCallContext, return_value), addr->dreg);
	}

	return alloc;
}

static gboolean
can_encode_method_ref (MonoMethod *method)
{
	/* Return whenever the AOT compiler can encode references to this method */
	if (!method->wrapper_type)
		return TRUE;
	return (method->wrapper_type == MONO_WRAPPER_DYNAMIC_METHOD);
}

void
mini_profiler_emit_enter (MonoCompile *cfg)
{
	gboolean trace = mono_jit_trace_calls != NULL && mono_trace_eval (cfg->method);

	if ((!MONO_CFG_PROFILE (cfg, ENTER) || cfg->current_method != cfg->method || (cfg->compile_aot && !can_encode_method_ref (cfg->method))) && !trace)
		return;

	if (cfg->current_method != cfg->method)
		return;

	MonoInst *iargs [2];

	EMIT_NEW_METHODCONST (cfg, iargs [0], cfg->method);

	if (MONO_CFG_PROFILE (cfg, ENTER_CONTEXT))
		iargs [1] = emit_fill_call_ctx (cfg, iargs [0], NULL);
	else
		EMIT_NEW_PCONST (cfg, iargs [1], NULL);

	/* void mono_profiler_raise_method_enter (MonoMethod *method, MonoProfilerCallContext *ctx) */
	if (trace)
		mono_emit_jit_icall (cfg, mono_trace_enter_method, iargs);
	else
		mono_emit_jit_icall (cfg, mono_profiler_raise_method_enter, iargs);
}

void
mini_profiler_emit_leave (MonoCompile *cfg, MonoInst *ret)
{
	gboolean trace = mono_jit_trace_calls != NULL && mono_trace_eval (cfg->method);

	if (!MONO_CFG_PROFILE (cfg, LEAVE) || cfg->current_method != cfg->method || (cfg->compile_aot && !can_encode_method_ref (cfg->method)))
		return;

	MonoInst *iargs [2];

	EMIT_NEW_METHODCONST (cfg, iargs [0], cfg->method);

	if (MONO_CFG_PROFILE (cfg, LEAVE_CONTEXT))
		iargs [1] = emit_fill_call_ctx (cfg, iargs [0], ret);
	else
		EMIT_NEW_PCONST (cfg, iargs [1], NULL);

	/* void mono_profiler_raise_method_leave (MonoMethod *method, MonoProfilerCallContext *ctx) */
	if (trace)
		mono_emit_jit_icall (cfg, mono_trace_leave_method, iargs);
	else
		mono_emit_jit_icall (cfg, mono_profiler_raise_method_leave, iargs);
}

void
mini_profiler_emit_tail_call (MonoCompile *cfg, MonoMethod *target)
{
	gboolean trace = mono_jit_trace_calls != NULL && mono_trace_eval (cfg->method);

	if ((!MONO_CFG_PROFILE (cfg, TAIL_CALL) || cfg->current_method != cfg->method) && !trace)
		return;

	g_assert (cfg->current_method == cfg->method);

	MonoInst *iargs [2];

	EMIT_NEW_METHODCONST (cfg, iargs [0], cfg->method);

	if (target)
		EMIT_NEW_METHODCONST (cfg, iargs [1], target);
	else
		EMIT_NEW_PCONST (cfg, iargs [1], NULL);

	/* void mono_profiler_raise_method_tail_call (MonoMethod *method, MonoMethod *target) */
	if (trace)
		mono_emit_jit_icall (cfg, mono_trace_leave_method, iargs);
	else
		mono_emit_jit_icall (cfg, mono_profiler_raise_method_tail_call, iargs);
}

void
mini_profiler_emit_call_finally (MonoCompile *cfg, MonoMethodHeader *header, unsigned char *ip,
                                 guint32 index, MonoExceptionClause *clause)
{
	if (!G_UNLIKELY (mono_profiler_clauses_enabled ()))
		return;

	MonoBasicBlock *ebb;

	NEW_BBLOCK (cfg, ebb);

	MonoInst *ins = mini_emit_runtime_constant (cfg, MONO_PATCH_INFO_PROFILER_CLAUSE_COUNT, NULL);
	MonoInst *count_ins = mini_emit_memory_load (cfg, m_class_get_byval_arg (mono_defaults.uint32_class), ins, 0, 0);
	EMIT_NEW_BIALU_IMM (cfg, ins, OP_ICOMPARE_IMM, -1, count_ins->dreg, 0);
	ins->flags |= MONO_INST_LIKELY;
	MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_IBEQ, ebb);

	MonoInst *iargs [4];

	EMIT_NEW_METHODCONST (cfg, iargs [0], cfg->current_method);
	EMIT_NEW_ICONST (cfg, iargs [1], index);
	EMIT_NEW_ICONST (cfg, iargs [2], clause->flags);

	MonoExceptionClause *cclause = NULL;

	// Are we leaving a catch clause?
	for (guint32 i = 0; i < header->num_clauses; i++) {
		MonoExceptionClause *hclause = &header->clauses [i];
		guint32 offset = ip - header->code;

		if (hclause->flags != MONO_EXCEPTION_CLAUSE_NONE && hclause->flags != MONO_EXCEPTION_CLAUSE_FILTER)
			continue;

		if (!MONO_OFFSET_IN_HANDLER (hclause, offset))
			continue;

		if (offset + (*ip == CEE_LEAVE ? 5 : 2) <= hclause->handler_offset + hclause->handler_len) {
			cclause = hclause;
			break;
		}
	}

	// If so, find the exception object and pass it along.
	if (cclause)
		EMIT_NEW_TEMPLOAD (cfg, iargs [3], mono_find_exvar_for_offset (cfg, cclause->handler_offset)->inst_c0);
	else
		EMIT_NEW_PCONST (cfg, iargs [3], NULL);

	/* void mono_profiler_raise_exception_clause (MonoMethod *method, uint32_t index, MonoExceptionEnum type, MonoObject *exception) */
	mono_emit_jit_icall (cfg, mono_profiler_raise_exception_clause, iargs);

	MONO_START_BB (cfg, ebb);
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
	return (guint8 *)(gsize)mono_arch_context_get_int_reg (ctx, reg);
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
		 * address mode gets us the value itself as an host_mgreg_t value.
		 */
		host_mgreg_t value = (host_mgreg_t) get_int_reg (ctx, reg);

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
	if (!mono_method_signature_internal (ctx->method)->hasthis)
		return NULL;

	if (ctx->interp_frame)
		return memdup_with_type (mini_get_interp_callbacks ()->frame_get_this (ctx->interp_frame), m_class_get_this_arg (ctx->method->klass));
	else
		return memdup_with_type (ctx->args [0], m_class_get_this_arg (ctx->method->klass));
}

gpointer
mini_profiler_context_get_argument (MonoProfilerCallContext *ctx, guint32 pos)
{
	MonoMethodSignature *sig = mono_method_signature_internal (ctx->method);

	if (pos >= sig->param_count)
		return NULL;

	if (ctx->interp_frame)
		return memdup_with_type (mini_get_interp_callbacks ()->frame_get_arg (ctx->interp_frame, pos), sig->params [pos]);

	return memdup_with_type (ctx->args [sig->hasthis + pos], sig->params [pos]);
}

gpointer
mini_profiler_context_get_local (MonoProfilerCallContext *ctx, guint32 pos)
{
	ERROR_DECL (error);
	MonoMethodHeader *header = mono_method_get_header_checked (ctx->method, error);
	mono_error_assert_ok (error); // Must be a valid method at this point.

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
	MonoType *ret = mono_method_signature_internal (ctx->method)->ret;

	if (ctx->interp_frame) {
		int dummy;
		// FIXME:
		return g_malloc0 (mono_type_size (ret, &dummy));
	}

	if (!ctx->return_value)
		return NULL;

	return memdup_with_type (ctx->return_value, ret);
}

void
mini_profiler_context_free_buffer (void *buffer)
{
	g_free (buffer);
}
