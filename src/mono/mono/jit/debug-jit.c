#include <mono/jit/debug.h>
#include <mono/jit/debug-jit.h>
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

static void
record_line_number (MonoDebugMethodInfo *minfo, guint32 address, guint32 offset, guint32 line)
{
	MonoDebugLineNumberEntry *lne = g_new0 (MonoDebugLineNumberEntry, 1);

	lne->address = address;
	lne->offset = offset;
	lne->line = line;

	g_array_append_val (minfo->jit->line_numbers, *lne);
}

static void
debug_generate_method_lines (AssemblyDebugInfo *info, MonoDebugMethodInfo *minfo, MonoFlowGraph* cfg)
{
	guint32 st_address, st_line;
	DebugMethodInfo *priv = minfo->user_data;
	int i;

	if (!priv || !info->moffsets)
		return;

	minfo->jit->line_numbers = g_array_new (FALSE, TRUE, sizeof (MonoDebugLineNumberEntry));

	st_line = priv->first_line;
	st_address = minfo->jit->prologue_end;

	/* This is the first actual code line of the method. */
	record_line_number (minfo, st_address, 0, st_line);

	/* start lines of basic blocks */
	for (i = 0; i < cfg->block_count; ++i) {
		int j;

		for (j = 0; cfg->bblocks [i].forest && (j < cfg->bblocks [i].forest->len); ++j) {
			MBTree *t = (MBTree *) g_ptr_array_index (cfg->bblocks [i].forest, j);
			gint32 line_inc = 0, addr_inc;

			if (!i && !j) {
				st_line = priv->first_line;
				st_address = t->addr;
			}

			addr_inc = t->addr - st_address;
			st_address += addr_inc;

			if (t->cli_addr != -1) {
				int *lines = info->moffsets + st_line;
				int *k = lines;

				while ((*k != -1) && (*k < t->cli_addr))
					k++;

				line_inc = k - lines;
			}

			st_line += line_inc;

			if (t->cli_addr != -1)
				record_line_number (minfo, st_address, t->cli_addr, st_line);
		}
	}
}

static void
debug_update_il_offsets (AssemblyDebugInfo *info, MonoDebugMethodInfo *minfo, MonoFlowGraph* cfg)
{
	MonoMethodHeader *header;
	guint32 address, offset;
	int debug = 0;
	int i;

	g_assert (info->symfile);
	g_assert (!minfo->jit->line_numbers);
	minfo->jit->line_numbers = g_array_new (FALSE, TRUE, sizeof (MonoDebugLineNumberEntry));

	address = minfo->jit->prologue_end;
	offset = 0;

	g_assert (((MonoMethodNormal*)minfo->method)->header);
	header = ((MonoMethodNormal*)minfo->method)->header;

#if 0
	if (!strcmp (minfo->method->name, "Test") || !strcmp (minfo->method->name, "Main")) {
		MonoMethodHeader *header = ((MonoMethodNormal*)minfo->method)->header;

		debug = 1;
		mono_disassemble_code (minfo->jit->code_start, minfo->jit->code_size,
				       minfo->method->name);

		printf ("\nDisassembly:\n%s\n", mono_disasm_code (
			NULL, minfo->method, header->code, header->code + header->code_size));
		g_message (G_STRLOC ": %x - %x", minfo->jit->prologue_end, minfo->jit->epilogue_begin);
	}
#endif

	_mono_debug_generate_line_number (minfo, address, offset, debug);

	/* start lines of basic blocks */
	for (i = 0; i < cfg->block_count; ++i) {
		int j;

		for (j = 0; cfg->bblocks [i].forest && (j < cfg->bblocks [i].forest->len); ++j) {
			MBTree *t = (MBTree *) g_ptr_array_index (cfg->bblocks [i].forest, j);

			if ((t->cli_addr == -1) || (t->cli_addr == offset) || (t->addr == address))
				continue;

			offset = t->cli_addr;
			address = t->addr;

			_mono_debug_generate_line_number (minfo, address, offset, debug);
		}
	}

	_mono_debug_generate_line_number (minfo, minfo->jit->epilogue_begin, header->code_size, debug);

	if (debug) {
		for (i = 0; i < minfo->jit->line_numbers->len; i++) {
			MonoDebugLineNumberEntry lne = g_array_index (
				minfo->jit->line_numbers, MonoDebugLineNumberEntry, i);

			g_message (G_STRLOC ": %x,%x,%d", lne.address, lne.offset, lne.line);
		}
	}

	if (minfo->jit->line_numbers->len) {
		MonoDebugLineNumberEntry lne = g_array_index (
			minfo->jit->line_numbers, MonoDebugLineNumberEntry, 0);

		minfo->jit->prologue_end = lne.address;
	}
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

void
mono_debug_add_method (MonoFlowGraph *cfg)
{
	MonoMethod *method = cfg->method;
	MonoClass *klass = method->klass;
	AssemblyDebugInfo* info;
	MonoDebugMethodJitInfo *jit;
	MonoDebugMethodInfo *minfo;
	int i;

	if (!mono_debug_handle)
		return;

	mono_class_init (klass);

	if ((method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) ||
	    (method->iflags & METHOD_IMPL_ATTRIBUTE_RUNTIME) ||
	    (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) ||
	    (method->flags & METHOD_ATTRIBUTE_ABSTRACT))
		return;

	info = _mono_debug_get_image (mono_debug_handle, klass->image);
	g_assert (info);

	if (method->wrapper_type != MONO_WRAPPER_NONE) {
		DebugWrapperInfo *winfo = g_new0 (DebugWrapperInfo, 1);

		winfo->method = method;
		winfo->code_start = cfg->start;
		winfo->code_size = cfg->epilogue_end;

		g_hash_table_insert (info->wrapper_methods, method, winfo);
		return;
	}

	minfo = _mono_debug_lookup_method (method);
	if (!minfo || minfo->jit)
		return;

	mono_debug_lock ();

	mono_debug_handle->dirty = TRUE;

	minfo->jit = jit = g_new0 (MonoDebugMethodJitInfo, 1);
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

	debug_generate_method_lines (info, minfo, cfg);
	if (info->format == MONO_DEBUG_FORMAT_MONO)
		debug_update_il_offsets (info, minfo, cfg);

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
				begin_scope = _mono_debug_address_from_il_offset (minfo, begin_offset);
			else
				begin_scope = -1;
			if (end_offset >= 0)
				end_scope = _mono_debug_address_from_il_offset (minfo, end_offset);
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

	if (info->symfile) {
		mono_debug_symfile_add_method (info->symfile, method);
		mono_debugger_event (MONO_DEBUGGER_EVENT_METHOD_ADDED, info->symfile, method);
	}

	mono_debug_unlock ();
}

