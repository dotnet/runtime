/*
 * debug-mini.c: Mini-specific debugging stuff.
 *
 * Author:
 *   Martin Baulig (martin@ximian.com)
 *
 * (C) 2003 Ximian, Inc.
 */

#include "mini.h"
#include "mini-x86.h"
#include "debug-private.h"

void
mono_debug_codegen_breakpoint (guint8 **buf)
{
	x86_breakpoint (*buf);
}

void
mono_debug_codegen_ret (guint8 **buf)
{
	x86_ret (*buf);
}

typedef struct
{
	MonoDebugMethodInfo *minfo;
	guint32 has_line_numbers;
} MiniDebugMethodInfo;

void
mono_debug_init_method (MonoCompile *cfg, MonoBasicBlock *start_block)
{
	MonoMethod *method = cfg->method;
	MiniDebugMethodInfo *info;

	if (!mono_debug_handle)
		return;

	if ((method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) ||
	    (method->iflags & METHOD_IMPL_ATTRIBUTE_RUNTIME) ||
	    (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) ||
	    (method->flags & METHOD_ATTRIBUTE_ABSTRACT) ||
	    (method->wrapper_type != MONO_WRAPPER_NONE))
		return;

	info = g_new0 (MiniDebugMethodInfo, 1);

	cfg->debug_info = info;
}

void
mono_debug_open_method (MonoCompile *cfg)
{
	MiniDebugMethodInfo *info;
	MonoDebugMethodJitInfo *jit;
	MonoMethodHeader *header;

	info = (MiniDebugMethodInfo *) cfg->debug_info;
	if (!info)
		return;

	mono_class_init (cfg->method->klass);

	info->minfo = _mono_debug_lookup_method (cfg->method);
	if (!info->minfo || info->minfo->jit)
		return;

	mono_debug_handle->dirty = TRUE;

	g_assert (((MonoMethodNormal*)info->minfo->method)->header);
	header = ((MonoMethodNormal*)info->minfo->method)->header;

	info->minfo->jit = jit = g_new0 (MonoDebugMethodJitInfo, 1);
	jit->line_numbers = g_array_new (FALSE, TRUE, sizeof (MonoDebugLineNumberEntry));
	jit->num_locals = header->num_locals;
	jit->locals = g_new0 (MonoDebugVarInfo, jit->num_locals);
}

static void
write_variable (MonoInst *inst, MonoDebugVarInfo *var)
{
	if (inst->opcode == OP_REGVAR)
		var->index = inst->dreg | MONO_DEBUG_VAR_ADDRESS_MODE_REGISTER;
	else if (inst->inst_basereg != X86_EBP) {
		g_message (G_STRLOC ": %d - %d", inst->inst_basereg, inst->inst_offset);
		var->index = inst->inst_basereg | MONO_DEBUG_VAR_ADDRESS_MODE_REGISTER;
		var->offset = inst->inst_offset;
	} else
		var->offset = inst->inst_offset;
}

void
mono_debug_close_method (MonoCompile *cfg)
{
	MiniDebugMethodInfo *info;
	MonoDebugMethodInfo *minfo;
	MonoDebugMethodJitInfo *jit;
	MonoMethodHeader *header;
	MonoMethod *method;
	int i;

	info = (MiniDebugMethodInfo *) cfg->debug_info;
	if (!info || !info->minfo)
		return;

	minfo = info->minfo;
	method = minfo->method;
	header = ((MonoMethodNormal*)method)->header;

	jit = minfo->jit;
	jit->code_start = cfg->native_code;
	jit->epilogue_begin = cfg->epilog_begin;
	jit->code_size = cfg->code_len;

	_mono_debug_generate_line_number (minfo, jit->epilogue_begin, header->code_size, 0);

	jit->num_params = method->signature->param_count;
	jit->params = g_new0 (MonoDebugVarInfo, jit->num_params);

	for (i = 0; i < jit->num_locals; i++)
		write_variable (cfg->varinfo [cfg->locals_start + i], &jit->locals [i]);

	if (method->signature->hasthis) {
		jit->this_var = g_new0 (MonoDebugVarInfo, 1);
		write_variable (cfg->varinfo [0], jit->this_var);
	}

	for (i = 0; i < jit->num_params; i++)
		write_variable (cfg->varinfo [i + method->signature->hasthis], &jit->params [i]);

	if (minfo->symfile) {
		mono_debug_symfile_add_method (minfo->symfile, minfo->method);
		mono_debugger_event (MONO_DEBUGGER_EVENT_METHOD_ADDED, minfo->symfile, minfo->method);
	}
}

void
mono_debug_record_line_number (MonoCompile *cfg, MonoInst *ins, guint32 address)
{
	MiniDebugMethodInfo *info;
	MonoMethodHeader *header;
	guint32 offset;

	info = (MiniDebugMethodInfo *) cfg->debug_info;
	if (!info || !info->minfo || !ins->cil_code)
		return;

	g_assert (((MonoMethodNormal*)info->minfo->method)->header);
	header = ((MonoMethodNormal*)info->minfo->method)->header;

	if ((ins->cil_code < header->code) ||
	    (ins->cil_code > header->code + header->code_size))
		return;

	offset = ins->cil_code - header->code;
	if (!info->has_line_numbers) {
		info->minfo->jit->prologue_end = address;
		info->has_line_numbers = TRUE;
	}

	_mono_debug_generate_line_number (info->minfo, address, offset, 0);
}
