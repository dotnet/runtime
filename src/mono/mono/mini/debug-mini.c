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
#include <mono/metadata/mono-debug.h>
/* mono-debug-debugger.h nneds config.h to work... */
#include "config.h"
#include <mono/metadata/mono-debug-debugger.h>

/*
 * This method is only called when running in the Mono Debugger.
 */
gpointer
mono_debugger_create_notification_function (gpointer *notification_address)
{
	guint8 *ptr, *buf;

	ptr = buf = g_malloc0 (16);
	x86_breakpoint (buf);
	if (notification_address)
		*notification_address = buf;
	x86_ret (buf);

	return ptr;
}

static void
record_line_number (MonoDebugMethodJitInfo *jit, guint32 address, guint32 offset)
{
	MonoDebugLineNumberEntry *lne = g_new0 (MonoDebugLineNumberEntry, 1);

	lne->address = address;
	lne->offset = offset;

	g_array_append_val (jit->line_numbers, *lne);
}

typedef struct
{
	MonoDebugMethodJitInfo *jit;
	guint32 has_line_numbers;
	guint32 breakpoint_id;
} MiniDebugMethodInfo;

void
mono_debug_init_method (MonoCompile *cfg, MonoBasicBlock *start_block, guint32 breakpoint_id)
{
	MonoMethod *method = cfg->method;
	MiniDebugMethodInfo *info;

	if (mono_debug_format == MONO_DEBUG_FORMAT_NONE)
		return;

	if ((method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) ||
	    (method->iflags & METHOD_IMPL_ATTRIBUTE_RUNTIME) ||
	    (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) ||
	    (method->flags & METHOD_ATTRIBUTE_ABSTRACT) ||
	    (method->wrapper_type != MONO_WRAPPER_NONE))
		return;

	info = g_new0 (MiniDebugMethodInfo, 1);
	info->breakpoint_id = breakpoint_id;

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

	g_assert (((MonoMethodNormal*)cfg->method)->header);
	header = ((MonoMethodNormal*)cfg->method)->header;

	info->jit = jit = g_new0 (MonoDebugMethodJitInfo, 1);
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
	MonoDebugMethodJitInfo *jit;
	MonoMethodHeader *header;
	MonoMethod *method;
	int i;

	info = (MiniDebugMethodInfo *) cfg->debug_info;
	if (!info || !info->jit)
		return;

	method = cfg->method;
	header = ((MonoMethodNormal*)method)->header;

	jit = info->jit;
	jit->code_start = cfg->native_code;
	jit->epilogue_begin = cfg->epilog_begin;
	jit->code_size = cfg->code_len;

	record_line_number (jit, jit->epilogue_begin, header->code_size);

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

	mono_debug_add_method (method, jit, cfg->domain);

	if (info->breakpoint_id)
		mono_debugger_breakpoint_callback (method, info->breakpoint_id);
}

void
mono_debug_record_line_number (MonoCompile *cfg, MonoInst *ins, guint32 address)
{
	MiniDebugMethodInfo *info;
	MonoMethodHeader *header;
	guint32 offset;

	info = (MiniDebugMethodInfo *) cfg->debug_info;
	if (!info || !info->jit || !ins->cil_code)
		return;

	g_assert (((MonoMethodNormal*)cfg->method)->header);
	header = ((MonoMethodNormal*)cfg->method)->header;

	if ((ins->cil_code < header->code) ||
	    (ins->cil_code > header->code + header->code_size))
		return;

	offset = ins->cil_code - header->code;
	if (!info->has_line_numbers) {
		info->jit->prologue_end = address;
		info->has_line_numbers = TRUE;
	}

	record_line_number (info->jit, address, offset);
}

MonoDomain *
mono_init_debugger (const char *file, const char *opt_flags)
{
	MonoDomain *domain;
	const char *file, *error;
	int opt;

	g_set_prgname (file);

	opt = mono_parse_default_optimizations (opt_flags);
	opt |= MONO_OPT_SHARED;

	mono_set_defaults (0, opt);

	domain = mono_jit_init (file);

	mono_config_parse (NULL);

	error = mono_verify_corlib ();
	if (error) {
		fprintf (stderr, "Corlib not in sync with this runtime: %s\n", error);
		exit (1);
	}

	return domain;
}
