#include <stdlib.h>
#include <string.h>
#include <errno.h>
#include <sys/stat.h>
#include <mono/metadata/class.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/tokentype.h>
#include <mono/jit/codegen.h>
#include <mono/jit/debug.h>

#include "debug-private.h"

MonoDebugHandle*
mono_debug_open_file (const char *filename, MonoDebugFormat format)
{
	MonoDebugHandle *debug;
	
	debug = g_new0 (MonoDebugHandle, 1);
	debug->name = g_strdup (filename);
	debug->default_format = format;
	return debug;
}

static void
debug_load_method_lines (AssemblyDebugInfo* info)
{
	FILE *f;
	char buf [1024];
	int i, mnum;
	char *name = g_strdup_printf ("%s.il", info->name);
	char *command = g_strdup_printf ("monodis --output=%s.il %s", info->name, info->image->name);
	struct stat stata, statb;
	int offset = -1;

	if (stat (info->image->name, &stata)) {
		g_warning ("cannot access assembly file (%s): %s", info->image->name, g_strerror (errno));
		g_free (command);
		g_free (name);
		return;
	}

	/* If the stat() failed or the file is older. */
	if (stat (name, &statb) || (statb.st_mtime < stata.st_mtime)) {
		g_print ("Recreating %s from %s.\n", name, info->image->name);
		if (system (command)) {
			g_warning ("cannot create IL assembly file (%s): %s", command, g_strerror (errno));
			g_free (command);
			g_free (name);
			return;
		}
	}

	/* use an env var with directories for searching. */
	if (!(f = fopen (name, "r"))) {
		g_warning ("cannot open IL assembly file %s", name);
		g_free (command);
		g_free (name);
		return;
	}

	info->total_lines = 100;
	info->moffsets = g_malloc (info->total_lines * sizeof (int));

	g_free (name);
	g_free (command);
	i = 0;
	while (fgets (buf, sizeof (buf), f)) {
		int pos = i;

		info->moffsets [i++] = offset;
		if (i + 2 >= info->total_lines) {
			info->total_lines += 100;
			info->moffsets = g_realloc (info->moffsets, info->total_lines * sizeof (int));
			g_assert (info->moffsets);
		}

		if (!sscanf (buf, " // method line %d", &mnum))
			continue;

		offset = 0;

		if (mnum >= info->nmethods)
			break;

		while (fgets (buf, sizeof (buf), f)) {
			int newoffset;

			++i;
			if (i + 2 >= info->total_lines) {
				info->total_lines += 100;
				info->moffsets = g_realloc (info->moffsets, info->total_lines * sizeof (int));
				g_assert (info->moffsets);
			}

			if (strstr (buf, "}")) {
				offset = -1;
				break;
			}

			if (sscanf (buf, " IL_%x:", &newoffset)) {
				offset = newoffset;
				if (!offset)
					pos = i;
			}

			info->moffsets [i] = offset;
		}
		/* g_print ("method %d found at %d\n", mnum, pos); */
		info->mlines [mnum] = pos;
	}
	fclose (f);
}

static void
record_line_number (DebugMethodInfo *minfo, gpointer address, guint32 line, int is_basic_block)
{
	DebugLineNumberInfo *lni = g_new0 (DebugLineNumberInfo, 1);

	lni->address = address;
	lni->line = line;
	lni->is_basic_block = is_basic_block;
	lni->source_file = 0;

	g_ptr_array_add (minfo->line_numbers, lni);
}

static void
record_il_offset (GPtrArray *array, guint32 offset, guint32 address)
{
	MonoDebugILOffsetInfo *info = g_new0 (MonoDebugILOffsetInfo, 1);

	info->offset = offset;
	info->address = address;

	g_ptr_array_add (array, info);
}

static void
debug_generate_method_lines (AssemblyDebugInfo *info, DebugMethodInfo *minfo, MonoFlowGraph* cfg)
{
	guint32 st_address, st_line;
	GPtrArray *il_offsets;
	int i;

	il_offsets = g_ptr_array_new ();
	minfo->line_numbers = g_ptr_array_new ();

	st_line = minfo->first_line;
	st_address = minfo->method_info.prologue_end;

	/* record_line_number takes absolute memory addresses. */
	record_line_number (minfo, minfo->method_info.code_start, minfo->start_line, FALSE);
	/* record_il_offsets uses offsets relative to minfo->method_info.code_start. */
	record_il_offset (il_offsets, 0, st_address);

	/* This is the first actual code line of the method. */
	record_line_number (minfo, minfo->method_info.code_start + st_address, st_line, TRUE);

	/* start lines of basic blocks */
	for (i = 0; i < cfg->block_count; ++i) {
		int j;

		for (j = 0; cfg->bblocks [i].forest && (j < cfg->bblocks [i].forest->len); ++j) {
			MBTree *t = (MBTree *) g_ptr_array_index (cfg->bblocks [i].forest, j);
			gint32 line_inc = 0, addr_inc;

			if (!i && !j) {
				st_line = minfo->first_line;
				st_address = t->addr - 1;

				record_line_number (minfo, cfg->start + st_address, st_line, TRUE);
			}

			addr_inc = t->addr - st_address - 1;
			st_address += addr_inc;

			if (t->cli_addr != -1)
				record_il_offset (il_offsets, t->cli_addr, st_address);

			if (!info->moffsets)
				continue;


			if (t->cli_addr != -1) {
				int *lines = info->moffsets + st_line;
				int *k = lines;

				while ((*k != -1) && (*k < t->cli_addr))
					k++;

				line_inc = k - lines;
			}

			st_line += line_inc;

			record_line_number (minfo, minfo->method_info.code_start + st_address,
					    st_line, j == 0);
		}
	}

	minfo->method_info.num_il_offsets = il_offsets->len;
	minfo->method_info.il_offsets = g_new0 (MonoDebugILOffsetInfo, il_offsets->len);
	for (i = 0; i < il_offsets->len; i++) {
		MonoDebugILOffsetInfo *il = (MonoDebugILOffsetInfo *) g_ptr_array_index (il_offsets, i);

		minfo->method_info.il_offsets [i] = *il;
	}

	g_ptr_array_free (il_offsets, TRUE);
}

static void
free_method_info (DebugMethodInfo *minfo)
{
	if (minfo->line_numbers)
		g_ptr_array_free (minfo->line_numbers, TRUE);
	g_free (minfo->method_info.params);
	g_free (minfo->method_info.locals);
	g_free (minfo);
}

static AssemblyDebugInfo*
mono_debug_open_assembly (MonoDebugHandle* handle, MonoImage *image)
{
	GList *tmp;
	AssemblyDebugInfo* info;

	for (tmp = handle->info; tmp; tmp = tmp->next) {
		info = (AssemblyDebugInfo*)tmp->data;
		if (strcmp (info->name, image->assembly_name) == 0)
			return info;
	}
	info = g_new0 (AssemblyDebugInfo, 1);
	info->format = handle->default_format;
	switch (handle->default_format) {
	case MONO_DEBUG_FORMAT_STABS:
		info->filename = g_strdup_printf ("%s-stabs.s", image->assembly_name);
		break;
	case MONO_DEBUG_FORMAT_DWARF2:
		info->filename = g_strdup_printf ("%s-dwarf.s", image->assembly_name);
		break;
	case MONO_DEBUG_FORMAT_DWARF2_PLUS:
		info->filename = g_strdup_printf ("%s-debug.o", image->assembly_name);

		g_message (G_STRLOC ": %s - %s", image->assembly_name, image->name);

		/* Fall back to dwarf if we can't find the symbol file. */
		if (!g_file_test (info->filename, G_FILE_TEST_EXISTS)) {
			gchar *fname = g_strdup_printf ("%s-dwarf.s", image->assembly_name);

			if (g_file_test (fname, G_FILE_TEST_EXISTS)) {
				g_warning ("Can't open symbol file `%s', falling back to DWARF 2.",
					   info->filename);
				g_free (info->filename);
				info->filename = fname;
				info->format = MONO_DEBUG_FORMAT_DWARF2;
			} else
				g_free (fname);
		}

		break;
	}
	info->image = image;
	info->image->ref_count++;
	info->name = g_strdup (image->assembly_name);
	info->methods = g_hash_table_new_full (g_direct_hash, g_direct_equal,
					       NULL, (GDestroyNotify) free_method_info);
	info->source_files = g_ptr_array_new ();
	info->type_hash = g_hash_table_new (NULL, NULL);

	g_ptr_array_add (info->source_files, g_strdup_printf ("%s.il", image->assembly_name));
	info->producer_name = g_strdup_printf ("Mono JIT compiler version %s", VERSION);

	mono_debug_get_type (info, mono_defaults.void_class);
	mono_debug_get_type (info, mono_defaults.object_class);
	mono_debug_get_type (info, mono_defaults.void_class);
	mono_debug_get_type (info, mono_defaults.boolean_class);
	mono_debug_get_type (info, mono_defaults.char_class);
	mono_debug_get_type (info, mono_defaults.sbyte_class);
	mono_debug_get_type (info, mono_defaults.byte_class);
	mono_debug_get_type (info, mono_defaults.int16_class);
	mono_debug_get_type (info, mono_defaults.uint16_class);
	mono_debug_get_type (info, mono_defaults.int32_class);
	mono_debug_get_type (info, mono_defaults.uint32_class);
	mono_debug_get_type (info, mono_defaults.int_class);
	mono_debug_get_type (info, mono_defaults.uint_class);
	mono_debug_get_type (info, mono_defaults.int64_class);
	mono_debug_get_type (info, mono_defaults.uint64_class);
	mono_debug_get_type (info, mono_defaults.single_class);
	mono_debug_get_type (info, mono_defaults.double_class);
	mono_debug_get_type (info, mono_defaults.string_class);

	switch (info->format) {
	case MONO_DEBUG_FORMAT_STABS:
		mono_debug_open_assembly_stabs (info);
		break;
	case MONO_DEBUG_FORMAT_DWARF2:
		mono_debug_open_assembly_dwarf2 (info);
		break;
	case MONO_DEBUG_FORMAT_DWARF2_PLUS:
		mono_debug_open_assembly_dwarf2_plus (info);
		break;
	}

	info->next_idx = 100;
	handle->info = g_list_prepend (handle->info, info);

	info->nmethods = image->tables [MONO_TABLE_METHOD].rows + 1;
	info->mlines = g_new0 (int, info->nmethods);

	if (info->format != MONO_DEBUG_FORMAT_DWARF2_PLUS)
		debug_load_method_lines (info);

	return info;
}

void
mono_debug_make_symbols (void)
{
	GList *tmp;
	AssemblyDebugInfo* info;

	if (!mono_debug_handle)
		return;

	for (tmp = mono_debug_handle->info; tmp; tmp = tmp->next) {
		info = (AssemblyDebugInfo*)tmp->data;

		switch (info->format) {
		case MONO_DEBUG_FORMAT_STABS:
			mono_debug_write_assembly_stabs (info);
			break;
		case MONO_DEBUG_FORMAT_DWARF2:
			mono_debug_write_assembly_dwarf2 (info);
			break;
		case MONO_DEBUG_FORMAT_DWARF2_PLUS:
			mono_debug_write_assembly_dwarf2_plus (info);
			break;
		}
	}
}

static void
mono_debug_close_assembly (AssemblyDebugInfo* info)
{
	g_free (info->mlines);
	g_free (info->moffsets);
	g_free (info->name);
	g_free (info->filename);
	g_ptr_array_free (info->source_files, TRUE);
	g_hash_table_destroy (info->type_hash);
	g_hash_table_destroy (info->methods);
	g_free (info->producer_name);
	g_free (info);
}

void
mono_debug_close (MonoDebugHandle* debug)
{
	GList *tmp;
	AssemblyDebugInfo* info;

	mono_debug_make_symbols ();

	for (tmp = debug->info; tmp; tmp = tmp->next) {
		info = (AssemblyDebugInfo*)tmp->data;

		switch (info->format) {
		case MONO_DEBUG_FORMAT_STABS:
			mono_debug_close_assembly_stabs (info);
			break;
		case MONO_DEBUG_FORMAT_DWARF2:
			mono_debug_close_assembly_dwarf2 (info);
			break;
		case MONO_DEBUG_FORMAT_DWARF2_PLUS:
			mono_debug_close_assembly_dwarf2_plus (info);
			break;
		}

		mono_debug_close_assembly (info);
	}

	g_free (debug->name);
	g_free (debug);
}

guint32
mono_debug_get_type (AssemblyDebugInfo* info, MonoClass *klass)
{
	guint index, i;

	mono_class_init (klass);

	index = GPOINTER_TO_INT (g_hash_table_lookup (info->type_hash, klass));
	if (index)
		return index;

	index = ++info->next_klass_idx;
	g_hash_table_insert (info->type_hash, klass, GINT_TO_POINTER (index));

	if (klass->enumtype)
		return index;

	switch (klass->byval_arg.type) {
	case MONO_TYPE_CLASS:
		if (klass->parent)
			mono_debug_get_type (info, klass->parent);

		for (i = 0; i < klass->method.count; i++) {
			MonoMethod *method = klass->methods [i];
			MonoType *ret_type = NULL;
			int j;

			if (method->signature->ret->type != MONO_TYPE_VOID)
				ret_type = method->signature->ret;

			if (ret_type) {
				MonoClass *ret_klass = mono_class_from_mono_type (ret_type);
				mono_debug_get_type (info, ret_klass);
			}

			for (j = 0; j < method->signature->param_count; j++) {
				MonoType *sub_type = method->signature->params [j];
				MonoClass *sub_klass = mono_class_from_mono_type (sub_type);
				mono_debug_get_type (info, sub_klass);
			}
		}
		// fall through
	case MONO_TYPE_VALUETYPE:
		for (i = 0; i < klass->field.count; i++) {
			MonoClass *subclass = mono_class_from_mono_type (klass->fields [i].type);
			mono_debug_get_type (info, subclass);
		}
		break;
	case MONO_TYPE_ARRAY:
	case MONO_TYPE_SZARRAY:
		mono_debug_get_type (info, klass->element_class);
		break;
	default:
		break;
	}

	return index;
}

void
mono_debug_add_type (MonoDebugHandle* debug, MonoClass *klass)
{
	AssemblyDebugInfo* info = mono_debug_open_assembly (debug, klass->image);

	mono_debug_get_type (info, klass);
}

void
mono_debug_add_method (MonoDebugHandle* debug, MonoFlowGraph *cfg)
{
	MonoMethod *method = cfg->method;
	MonoClass *klass = method->klass;
	AssemblyDebugInfo* info = mono_debug_open_assembly (debug, klass->image);
	int method_number = 0, line = 0, start_line = 0, end_line = 0, i;
	DebugMethodInfo *minfo;
	char *name;

	mono_class_init (klass);
	/*
	 * Find the method index in the image.
	 */
	for (i = 0; klass->methods && i < klass->method.count; ++i) {
		if (klass->methods [i] == method) {
			method_number = klass->method.first + i + 1;
			line = info->mlines [method_number];
			break;
		}
	}

	if (g_hash_table_lookup (info->methods, method))
		return;

	if (info->moffsets) {
		/* info->moffsets contains -1 "outside" of functions. */
		for (i = line; (i > 0) && (info->moffsets [i] == 0); i--)
			;
		start_line = i + 1;

		for (i = start_line; info->moffsets [i] != -1; i++)
			;
		end_line = i;
	}

	name = g_strdup_printf ("%s%s%s.%s", klass->name_space, klass->name_space [0]? ".": "",
				klass->name, method->name);

	minfo = g_new0 (DebugMethodInfo, 1);
	minfo->name = name;
	minfo->start_line = start_line;
	minfo->first_line = line;
	minfo->last_line = end_line;
	minfo->method_info.code_start = cfg->start + 1;
	minfo->method_info.code_size = cfg->epilogue_end - 1;
	minfo->method_number = method_number;
	minfo->method_info.method = method;
	minfo->method_info.num_params = method->signature->param_count;
	minfo->method_info.params = g_new0 (MonoDebugVarInfo, minfo->method_info.num_params);
	minfo->method_info.prologue_end = cfg->prologue_end - 1;
	minfo->method_info.epilogue_begin = cfg->epilog - 1;

	if (method->signature->hasthis) {
		MonoVarInfo *ptr = ((MonoVarInfo *) cfg->varinfo->data) + cfg->args_start_index;

		minfo->method_info.this_var = g_new0 (MonoDebugVarInfo, 1);
		minfo->method_info.this_var->offset = ptr->offset;
	}

	for (i = 0; i < minfo->method_info.num_params; i++) {
		MonoVarInfo *ptr = ((MonoVarInfo *) cfg->varinfo->data) + cfg->args_start_index +
			method->signature->hasthis;

		minfo->method_info.params [i].offset = ptr [i].offset;
	}

	if (!method->iflags & (METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL | METHOD_IMPL_ATTRIBUTE_RUNTIME)) {
		MonoMethodHeader *header = ((MonoMethodNormal*)method)->header;
		MonoVarInfo *ptr = ((MonoVarInfo *) cfg->varinfo->data) + cfg->locals_start_index;

		minfo->method_info.num_locals = header->num_locals;
		minfo->method_info.locals = g_new0 (MonoDebugVarInfo, header->num_locals);
		for (i = 0; i < minfo->method_info.num_locals; i++) {
			minfo->method_info.locals [i].offset = ptr [i].offset;
			minfo->method_info.locals [i].begin_scope = minfo->method_info.prologue_end;
			minfo->method_info.locals [i].end_scope = minfo->method_info.epilogue_begin;
		}
	}

	debug_generate_method_lines (info, minfo, cfg);

	g_hash_table_insert (info->methods, method, minfo);
}
