#include <config.h>
#include <mono/metadata/mono-debug.h>
#include <mono/metadata/mono-debug-debugger.h>
#include <mono/jit/debug-jit.h>
#include "codegen.h"

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

static void
debug_update_il_offsets (MonoMethod *method, MonoDebugMethodJitInfo *jit, MonoFlowGraph* cfg)
{
	MonoMethodHeader *header;
	guint32 address, offset;
	int i;

	jit->line_numbers = g_array_new (FALSE, TRUE, sizeof (MonoDebugLineNumberEntry));

	address = jit->prologue_end;
	offset = 0;

	g_assert (((MonoMethodNormal*)method)->header);
	header = ((MonoMethodNormal*)method)->header;

	record_line_number (jit, address, offset);

	/* start lines of basic blocks */
	for (i = 0; i < cfg->block_count; ++i) {
		int j;

		for (j = 0; cfg->bblocks [i].forest && (j < cfg->bblocks [i].forest->len); ++j) {
			MBTree *t = (MBTree *) g_ptr_array_index (cfg->bblocks [i].forest, j);

			if ((t->cli_addr == -1) || (t->cli_addr == offset) || (t->addr == address))
				continue;

			offset = t->cli_addr;
			address = t->addr;

			record_line_number (jit, address, offset);
		}
	}

	record_line_number (jit, jit->epilogue_begin, header->code_size);
}

static gint32
il_offset_from_position (MonoFlowGraph *cfg, MonoPosition *pos)
{
	MonoBBlock *bblock;
	MBTree *tree;

	if (pos->abs_pos == 0)
		return -1;

	if (pos->pos.bid >= cfg->block_count)
		return -1;

	bblock = &cfg->bblocks [pos->pos.bid];
	if (pos->pos.tid >= bblock->forest->len)
		return -1;

	tree = (MBTree *) g_ptr_array_index (bblock->forest, pos->pos.tid);

	return tree->cli_addr;
}

static gint32
address_from_il_offset (MonoDebugMethodJitInfo *jit, guint32 il_offset)
{
	int i;

	for (i = jit->line_numbers->len - 1; i >= 0; i--) {
		MonoDebugLineNumberEntry lne = g_array_index (
			jit->line_numbers, MonoDebugLineNumberEntry, i);

		if (lne.offset <= il_offset)
			return lne.address;
	}

	return -1;
}

void
mono_debug_jit_add_method (MonoFlowGraph *cfg)
{
	MonoMethod *method = cfg->method;
	MonoClass *klass = method->klass;
	MonoDebugMethodJitInfo *jit;
	int i;

	mono_class_init (klass);

	if ((method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) ||
	    (method->iflags & METHOD_IMPL_ATTRIBUTE_RUNTIME) ||
	    (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) ||
	    (method->flags & METHOD_ATTRIBUTE_ABSTRACT))
		return;

	jit = g_new0 (MonoDebugMethodJitInfo, 1);
	jit->code_start = cfg->start;
	jit->code_size = cfg->epilogue_end;
	jit->prologue_end = cfg->prologue_end;
	jit->epilogue_begin = cfg->epilog;
	jit->num_params = method->signature->param_count;
	jit->params = g_new0 (MonoDebugVarInfo, jit->num_params);

	if (method->signature->hasthis) {
		MonoVarInfo *ptr = ((MonoVarInfo *) cfg->varinfo->data) + cfg->args_start_index;

		jit->this_var = g_new0 (MonoDebugVarInfo, 1);
		jit->this_var->offset = ptr->offset;
		jit->this_var->size = ptr->size;
	}

	for (i = 0; i < jit->num_params; i++) {
		MonoVarInfo *ptr = ((MonoVarInfo *) cfg->varinfo->data) + cfg->args_start_index +
			method->signature->hasthis;

		jit->params [i].offset = ptr [i].offset;
		jit->params [i].size = ptr [i].size;
	}

	debug_update_il_offsets (method, jit, cfg);

	if (!method->iflags & (METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL | METHOD_IMPL_ATTRIBUTE_RUNTIME)) {
		MonoMethodHeader *header = ((MonoMethodNormal*)method)->header;
		MonoVarInfo *ptr = ((MonoVarInfo *) cfg->varinfo->data) + cfg->locals_start_index;
		MonoDebugVarInfo *locals;

		locals = g_new0 (MonoDebugVarInfo, header->num_locals);
		for (i = 0; i < header->num_locals; i++) {
			gint32 begin_offset, end_offset;
			gint32 begin_scope, end_scope;

			if (ptr [i].reg >= 0) {
				locals [i].index = ptr [i].reg | MONO_DEBUG_VAR_ADDRESS_MODE_REGISTER;
				locals [i].offset = 0;
			} else
				locals [i].offset = ptr [i].offset;

			locals [i].size = ptr [i].size;

			begin_offset = il_offset_from_position (cfg, &ptr [i].range.first_use);
			end_offset = il_offset_from_position (cfg, &ptr [i].range.last_use);
			if (end_offset >= 0)
				end_offset++;

			if (begin_offset >= 0)
				begin_scope = address_from_il_offset (jit, begin_offset);
			else
				begin_scope = -1;
			if (end_offset >= 0)
				end_scope = address_from_il_offset (jit, end_offset);
			else
				end_scope = -1;

			if (begin_scope > 0)
				locals [i].begin_scope = begin_scope;
			else
				locals [i].begin_scope = jit->prologue_end;
			if (end_scope > 0)
				locals [i].end_scope = end_scope;
			else
				locals [i].end_scope = jit->epilogue_begin;
		}

		jit->num_locals = header->num_locals;
		jit->locals = locals;
	}

	mono_debug_add_method (method, jit);
}
