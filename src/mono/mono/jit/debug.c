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

static MonoDebugHandle *mono_debug_handles = NULL;
static MonoDebugHandle *mono_default_debug_handle = NULL;

static void
free_method_info (DebugMethodInfo *minfo)
{
	if (minfo->line_numbers)
		g_ptr_array_free (minfo->line_numbers, TRUE);
	g_free (minfo->method_info.params);
	g_free (minfo->method_info.locals);
	g_free (minfo);
}

static void
debug_arg_warning (const char *message)
{
	g_warning ("Error while processing --debug-args arguments: %s", message);
}

MonoDebugHandle*
mono_debug_open (MonoAssembly *assembly, MonoDebugFormat format, const char **args)
{
	MonoDebugHandle *debug;
	const char **ptr;
	
	debug = g_new0 (MonoDebugHandle, 1);
	debug->name = g_strdup (assembly->image->name);
	debug->format = format;
	debug->producer_name = g_strdup_printf ("Mono JIT compiler version %s", VERSION);
	debug->next_idx = 100;

	debug->type_hash = g_hash_table_new (NULL, NULL);
	debug->methods = g_hash_table_new_full (g_direct_hash, g_direct_equal,
						NULL, (GDestroyNotify) free_method_info);
	debug->source_files = g_ptr_array_new ();

	for (ptr = args; ptr && *ptr; ptr++) {
		const char *arg = *ptr;
		gchar *message;

		switch (debug->format) {
		case MONO_DEBUG_FORMAT_STABS:
		case MONO_DEBUG_FORMAT_DWARF2:
			if (!strncmp (arg, "filename=", 9)) {
				if (debug->filename)
					debug_arg_warning ("The `filename' argument can be given only once.");
				debug->filename = g_strdup (arg + 9);
				continue;
			} else if (!strncmp (arg, "objfile=", 8)) {
				if (debug->objfile)
					debug_arg_warning ("The `objfile' argument can be given only once.");
				debug->objfile = g_strdup (arg + 8);
				continue;
			} else if (!strcmp (arg, "dont_assemble")) {
				debug->flags |= MONO_DEBUG_FLAGS_DONT_ASSEMBLE;
				continue;
			} else if (!strcmp (arg, "install_il_files")) {
				debug->flags |= MONO_DEBUG_FLAGS_INSTALL_IL_FILES;
				continue;
			} else if (!strcmp (arg, "dont_update_il_files")) {
				debug->flags |= MONO_DEBUG_FLAGS_DONT_UPDATE_IL_FILES;
				continue;
			} else if (!strcmp (arg, "dont_create_il_files")) {
				debug->flags |= MONO_DEBUG_FLAGS_DONT_CREATE_IL_FILES;
				continue;
			}
			break;
		case MONO_DEBUG_FORMAT_DWARF2_PLUS:
			if (!strcmp (arg, "dont_fallback")) {
				debug->flags |= MONO_DEBUG_FLAGS_DONT_FALLBACK;
				continue;
			}
			break;
		default:
			break;
		}

		message = g_strdup_printf ("Unknown argument `%s'.", arg);
		debug_arg_warning (message);
		g_free (message);
	}

	switch (debug->format) {
	case MONO_DEBUG_FORMAT_STABS:
		if (!debug->filename)
			debug->filename = g_strdup_printf ("%s-stabs.s", g_basename (debug->name));
		if (!debug->objfile)
			debug->objfile = g_strdup_printf ("%s.o", g_basename (debug->name));
		break;
	case MONO_DEBUG_FORMAT_DWARF2:
		if (!debug->filename)
			debug->filename = g_strdup_printf ("%s-dwarf.s", g_basename (debug->name));
		if (!debug->objfile)
			debug->objfile = g_strdup_printf ("%s.o", g_basename (debug->name));
		break;
	case MONO_DEBUG_FORMAT_DWARF2_PLUS:
		if (!mono_default_debug_handle && !(debug->flags & MONO_DEBUG_FLAGS_DONT_FALLBACK))
			mono_debug_open (assembly, MONO_DEBUG_FORMAT_DWARF2, NULL);
		break;
	default:
		g_assert_not_reached ();
	}

	debug->next = mono_debug_handles;
	mono_debug_handles = debug;

	if (!mono_default_debug_handle && (format != MONO_DEBUG_FORMAT_DWARF2_PLUS)) {
		mono_default_debug_handle = debug;
		mono_debug_add_image (debug, assembly->image);
	}

	return debug;
}

static void
debug_load_method_lines (AssemblyDebugInfo* info)
{
	FILE *f;
	char buf [1024];
	int i, mnum;
	int offset = -1;

	if (!(info->handle->flags & MONO_DEBUG_FLAGS_DONT_UPDATE_IL_FILES)) {
		char *command = g_strdup_printf ("monodis --output=%s %s",
						 info->ilfile, info->image->name);
		struct stat stata, statb;
		int need_update = FALSE;

		if (stat (info->image->name, &stata)) {
			g_warning ("cannot access assembly file (%s): %s",
				   info->image->name, g_strerror (errno));
			g_free (command);
			return;
		}

		/* If the stat() failed or the file is older. */
		if (stat (info->ilfile, &statb)) {
			/* Don't create any new *.il files if the user told us not to do so. */
			if (!(info->handle->flags & MONO_DEBUG_FLAGS_DONT_CREATE_IL_FILES))
				need_update = TRUE;
		} else if (statb.st_mtime < stata.st_mtime)
			need_update = TRUE;

		if (need_update) {
			g_print ("Recreating %s from %s.\n", info->ilfile, info->image->name);
			if (system (command)) {
				g_warning ("cannot create IL assembly file (%s): %s",
					   command, g_strerror (errno));
				g_free (command);
				return;
			}
		}
	}

	/* use an env var with directories for searching. */
	if (!(f = fopen (info->ilfile, "r"))) {
		g_warning ("cannot open IL assembly file %s", info->ilfile);
		return;
	}

	info->total_lines = 100;
	info->moffsets = g_malloc (info->total_lines * sizeof (int));

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
	lni->source_file = minfo->source_file;

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

static AssemblyDebugInfo *
mono_debug_get_image (MonoDebugHandle* debug, MonoImage *image)
{
	GList *tmp;
	AssemblyDebugInfo *info;

	if (debug->format == MONO_DEBUG_FORMAT_NONE)
		return NULL;

	for (tmp = debug->info; tmp; tmp = tmp->next) {
		info = (AssemblyDebugInfo*)tmp->data;

		if (info->image == image)
			return info;
	}

	return NULL;
}

static AssemblyDebugInfo *
mono_debug_open_image (MonoDebugHandle* debug, MonoImage *image)
{
	AssemblyDebugInfo *info;

	info = mono_debug_get_image (debug, image);
	if (info != NULL)
		return info;

	info = g_new0 (AssemblyDebugInfo, 1);
	info->image = image;
	info->image->ref_count++;
	info->name = g_strdup (image->assembly_name);
	info->format = debug->format;
	info->handle = debug;

	info->source_file = debug->source_files->len;
	g_ptr_array_add (debug->source_files, g_strdup_printf ("%s.il", image->assembly_name));

	debug->info = g_list_prepend (debug->info, info);

	info->nmethods = image->tables [MONO_TABLE_METHOD].rows + 1;
	info->mlines = g_new0 (int, info->nmethods);

	switch (info->format) {
	case MONO_DEBUG_FORMAT_STABS:
	case MONO_DEBUG_FORMAT_DWARF2:
		if (debug->flags & MONO_DEBUG_FLAGS_INSTALL_IL_FILES) {
			gchar *dirname = g_path_get_dirname (image->name);
			info->ilfile = g_strdup_printf ("%s/%s.il", dirname, info->name);
			g_free (dirname);
		} else
			info->ilfile = g_strdup_printf ("%s.il", info->name);
		break;
	case MONO_DEBUG_FORMAT_DWARF2_PLUS:
		info->filename = g_strdup_printf ("%s-debug.s", info->name);
		info->objfile = g_strdup_printf ("%s-debug.o", info->name);
		mono_debug_open_assembly_dwarf2_plus (info);
		break;
	default:
		break;
	}

	if (debug->format != MONO_DEBUG_FORMAT_DWARF2_PLUS)
		debug_load_method_lines (info);

	return info;
}

void
mono_debug_add_image (MonoDebugHandle* debug, MonoImage *image)
{
	mono_debug_open_image (debug, image);
}

void
mono_debug_write_symbols (MonoDebugHandle *debug)
{
	GList *tmp;

	if (!debug)
		return;

	switch (debug->format) {
	case MONO_DEBUG_FORMAT_STABS:
		mono_debug_write_stabs (debug);
		break;
	case MONO_DEBUG_FORMAT_DWARF2:
		mono_debug_write_dwarf2 (debug);
		break;
	case MONO_DEBUG_FORMAT_DWARF2_PLUS:
		for (tmp = debug->info; tmp; tmp = tmp->next) {
			AssemblyDebugInfo *info = (AssemblyDebugInfo*)tmp->data;

			mono_debug_write_assembly_dwarf2_plus (info);
		}
		break;
	default:
		g_assert_not_reached ();
	}
}

void
mono_debug_make_symbols (void)
{
	MonoDebugHandle *debug;

	for (debug = mono_debug_handles; debug; debug = debug->next)
		mono_debug_write_symbols (debug);
}

static void
mono_debug_close_assembly (AssemblyDebugInfo* info)
{
	switch (info->format) {
	case MONO_DEBUG_FORMAT_DWARF2_PLUS:
		mono_debug_close_assembly_dwarf2_plus (info);
		break;
	default:
		break;
	}
	g_free (info->mlines);
	g_free (info->moffsets);
	g_free (info->name);
	g_free (info->ilfile);
	g_free (info->filename);
	g_free (info->objfile);
	g_free (info);
}

void
mono_debug_cleanup (void)
{
	MonoDebugHandle *debug, *temp;

	mono_debug_make_symbols ();

	for (debug = mono_debug_handles; debug; debug = temp) {
		GList *tmp;

		for (tmp = debug->info; tmp; tmp = tmp->next) {
			AssemblyDebugInfo* info = (AssemblyDebugInfo*)tmp->data;

			mono_debug_close_assembly (info);
		}

		g_ptr_array_free (debug->source_files, TRUE);
		g_hash_table_destroy (debug->methods);
		g_hash_table_destroy (debug->type_hash);
		g_free (debug->producer_name);
		g_free (debug->name);

		temp = debug->next;
		g_free (debug);
	}

	mono_debug_handles = NULL;
	mono_default_debug_handle = NULL;
}

guint32
mono_debug_get_type (MonoDebugHandle *debug, MonoClass *klass)
{
	guint index, i;

	mono_class_init (klass);

	index = GPOINTER_TO_INT (g_hash_table_lookup (debug->type_hash, klass));
	if (index)
		return index;

	index = ++debug->next_klass_idx;
	g_hash_table_insert (debug->type_hash, klass, GINT_TO_POINTER (index));

	if (klass->enumtype)
		return index;

	switch (klass->byval_arg.type) {
	case MONO_TYPE_CLASS:
		if (klass->parent)
			mono_debug_get_type (debug, klass->parent);

		for (i = 0; i < klass->method.count; i++) {
			MonoMethod *method = klass->methods [i];
			MonoType *ret_type = NULL;
			int j;

			if (method->signature->ret->type != MONO_TYPE_VOID)
				ret_type = method->signature->ret;

			if (ret_type) {
				MonoClass *ret_klass = mono_class_from_mono_type (ret_type);
				mono_debug_get_type (debug, ret_klass);
			}

			for (j = 0; j < method->signature->param_count; j++) {
				MonoType *sub_type = method->signature->params [j];
				MonoClass *sub_klass = mono_class_from_mono_type (sub_type);
				mono_debug_get_type (debug, sub_klass);
			}
		}
		// fall through
	case MONO_TYPE_VALUETYPE:
		for (i = 0; i < klass->field.count; i++) {
			MonoClass *subclass = mono_class_from_mono_type (klass->fields [i].type);
			mono_debug_get_type (debug, subclass);
		}
		break;
	case MONO_TYPE_ARRAY:
	case MONO_TYPE_SZARRAY:
		mono_debug_get_type (debug, klass->element_class);
		break;
	default:
		break;
	}

	return index;
}

MonoDebugHandle *
mono_debug_handle_from_class (MonoClass *klass)
{
	MonoDebugHandle *debug;

	mono_class_init (klass);

	for (debug = mono_debug_handles; debug; debug = debug->next) {
		GList *tmp;

		for (tmp = debug->info; tmp; tmp = tmp->next) {
			AssemblyDebugInfo *info = (AssemblyDebugInfo*)tmp->data;

			if (info->image == klass->image)
				return debug;
		}
	}

	return NULL;
}

void
mono_debug_add_type (MonoClass *klass)
{
	MonoDebugHandle *debug = mono_debug_handle_from_class (klass);

	g_assert (debug != NULL);

	mono_debug_get_type (debug, klass);
}

void
mono_debug_add_method (MonoFlowGraph *cfg)
{
	MonoMethod *method = cfg->method;
	MonoClass *klass = method->klass;
	int method_number = 0, line = 0, start_line = 0, end_line = 0, i;
	MonoDebugHandle* debug;
	AssemblyDebugInfo* info;
	DebugMethodInfo *minfo;
	char *name;

	mono_class_init (klass);

	debug = mono_debug_handle_from_class (klass);
	if (!debug) {
		if (mono_default_debug_handle)
			debug = mono_default_debug_handle;
		else
			return;
	}

	info = mono_debug_open_image (debug, klass->image);

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

	if (g_hash_table_lookup (debug->methods, method))
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
	minfo->source_file = info->source_file;
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

	g_hash_table_insert (debug->methods, method, minfo);
}

gchar *
mono_debug_source_location_from_address (MonoMethod *method, guint32 address)
{
	MonoDebugHandle *debug;
	DebugMethodInfo *minfo = NULL;
	int i;

	for (debug = mono_debug_handles; debug; debug = debug->next) {
		minfo = g_hash_table_lookup (debug->methods, method);

		if (minfo)
			break;
	}

	if (!minfo || !minfo->line_numbers)
		return NULL;

	for (i = 0; i < minfo->line_numbers->len; i++) {
		DebugLineNumberInfo *lni = g_ptr_array_index (minfo->line_numbers, i);

		if ((gchar *)lni->address > minfo->method_info.code_start + address) {
			gchar *source_file = g_ptr_array_index (debug->source_files, lni->source_file);

			return g_strdup_printf ("%s:%d", source_file, lni->line);
		}
	}

	return NULL;
}

gint32
mono_debug_il_offset_from_address (MonoMethod *method, guint32 address)
{
	MonoDebugHandle *debug;
	MonoDebugMethodInfo *minfo = NULL;
	int i;

	for (debug = mono_debug_handles; debug; debug = debug->next) {
		minfo = g_hash_table_lookup (debug->methods, method);

		if (minfo)
			break;
	}

	if (!minfo || !minfo->il_offsets)
		return -1;

	for (i = 0; i < minfo->num_il_offsets; i++) {
		MonoDebugILOffsetInfo *ilo = &minfo->il_offsets [i];

		if (ilo->address > address)
			return ilo->offset;
	}

	return -1;
}
