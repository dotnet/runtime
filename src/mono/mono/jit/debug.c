#include <stdlib.h>
#include <string.h>
#include <errno.h>
#include <sys/stat.h>
#include <mono/metadata/class.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/debug-mono-symfile.h>
#include <mono/jit/codegen.h>
#include <mono/jit/debug.h>

#include "debug-private.h"

/* See debug.h for documentation. */
guint32 mono_debugger_symbol_file_table_generation = 0;
guint8 *mono_debugger_symbol_file_table = NULL;

/* Caution: This function MUST be called before touching the symbol table! */
static void release_symbol_file_table (void);

static MonoDebugHandle *mono_debug_handles = NULL;
static MonoDebugHandle *mono_default_debug_handle = NULL;

/*
 * This is a global data symbol which is read by the debugger.
 */
MonoDebuggerInfo MONO_DEBUGGER__debugger_info = {
	MONO_SYMBOL_FILE_MAGIC,
	MONO_SYMBOL_FILE_VERSION,
	sizeof (MonoDebuggerInfo),
	&mono_generic_trampoline_code,
	&mono_debugger_symbol_file_table_generation,
	&mono_debugger_symbol_file_table,
	&mono_debugger_update_symbol_file_table,
	&mono_compile_method
};

static void
free_method_info (MonoDebugMethodInfo *minfo)
{
	DebugMethodInfo *priv = minfo->user_data;

	if (priv) {
		if (priv->line_numbers)
			g_ptr_array_free (priv->line_numbers, TRUE);

		g_free (priv->name);
		g_free (priv);
	}

	if (minfo->jit) {
		g_free (minfo->jit->il_addresses);
		g_free (minfo->jit->this_var);
		g_free (minfo->jit->params);
		g_free (minfo->jit->locals);
		g_free (minfo->jit);
	}

	g_free (minfo->il_offsets);
	g_free (minfo);
}

static void
debug_arg_warning (const char *message)
{
	g_warning ("Error while processing --debug-args arguments: %s", message);
}

static gchar *
replace_suffix (const char *filename, const char *new_suffix)
{
	const char *pos = strrchr (filename, '.');

	if (!pos)
		return g_strdup_printf ("%s.%s", filename, new_suffix);
	else {
		int len = pos - filename;
		gchar *retval = g_malloc0 (len + strlen (new_suffix) + 2);
		memcpy (retval, filename, len);
		retval [len] = '.';
		memcpy (retval + len + 1, new_suffix, strlen (new_suffix) + 1);
		return retval;
	}
}

MonoDebugHandle*
mono_debug_open (const char *name, MonoDebugFormat format, const char **args)
{
	MonoDebugHandle *debug;
	const char **ptr;

	release_symbol_file_table ();
	
	debug = g_new0 (MonoDebugHandle, 1);
	debug->name = g_strdup (name);
	debug->format = format;
	debug->producer_name = g_strdup_printf ("Mono JIT compiler version %s", VERSION);
	debug->next_idx = 100;
	debug->dirty = TRUE;

	debug->type_hash = g_hash_table_new (NULL, NULL);
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
			}
			break;
		case MONO_DEBUG_FORMAT_MONO:
			debug->flags |= MONO_DEBUG_FLAGS_DONT_UPDATE_IL_FILES |
				MONO_DEBUG_FLAGS_DONT_CREATE_IL_FILES;
			break;
		default:
			break;
		}

		if (debug->format != MONO_DEBUG_FORMAT_MONO) {
			if (!strcmp (arg, "dont_assemble")) {
				debug->flags |= MONO_DEBUG_FLAGS_DONT_ASSEMBLE;
				continue;
			} else if (!strcmp (arg, "update_on_exit")) {
				debug->flags |= MONO_DEBUG_FLAGS_UPDATE_ON_EXIT;
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
		} else {
			if (!strcmp (arg, "internal_mono_debugger")) {
				debug->flags |= MONO_DEBUG_FLAGS_MONO_DEBUGGER;
				continue;
			}
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
	case MONO_DEBUG_FORMAT_MONO:
		break;
	default:
		g_assert_not_reached ();
	}

	debug->next = mono_debug_handles;
	mono_debug_handles = debug;

	if (!mono_default_debug_handle)
		mono_default_debug_handle = debug;

	return debug;
}

static void
generate_il_offsets (AssemblyDebugInfo *info, MonoMethod *method)
{
	GPtrArray *il_offsets = g_ptr_array_new ();
	MonoClass *klass = method->klass;
	MonoDebugMethodInfo *minfo;
	DebugMethodInfo *priv;
	int i;

	g_assert (klass->image == info->image);

	/* FIXME: doesn't work yet. */
	if (!strcmp (klass->name_space, "System.Runtime.Remoting.Proxies"))
		return;

	mono_class_init (klass);

	minfo = g_new0 (MonoDebugMethodInfo, 1);
	minfo->method = method;
	minfo->user_data = priv = g_new0 (DebugMethodInfo, 1);

	priv->name = g_strdup_printf ("%s%s%s.%s", klass->name_space, klass->name_space [0]? ".": "",
				      klass->name, method->name);
	priv->source_file = info->source_file;
	priv->info = info;

	/*
	 * Find the method index in the image.
	 */
	for (i = 0; klass->methods && i < klass->method.count; ++i) {
		if (klass->methods [i] == minfo->method) {
			priv->method_number = klass->method.first + i + 1;
			priv->first_line = info->mlines [priv->method_number];
			break;
		}
	}

	g_assert (priv->method_number);

	/* info->moffsets contains -1 "outside" of functions. */
	for (i = priv->first_line; (i > 0) && (info->moffsets [i] == 0); i--)
		;
	priv->start_line = i + 1;

	for (i = priv->start_line; info->moffsets [i] != -1; i++) {
		MonoSymbolFileLineNumberEntry *lne = g_new0 (MonoSymbolFileLineNumberEntry, 1);

		if (!info->moffsets [i] && (i > priv->start_line))
			continue;

		lne->offset = info->moffsets [i];
		lne->row = i;

		g_ptr_array_add (il_offsets, lne);
	}

	priv->last_line = i;

	minfo->num_il_offsets = il_offsets->len;
	minfo->il_offsets = g_new0 (MonoSymbolFileLineNumberEntry, il_offsets->len);
	for (i = 0; i < il_offsets->len; i++) {
		MonoSymbolFileLineNumberEntry *il = g_ptr_array_index (il_offsets, i);

		minfo->il_offsets [i] = *il;
	}

	g_ptr_array_free (il_offsets, TRUE);

	g_hash_table_insert (info->methods, method, minfo);
}

static void
debug_load_method_lines (AssemblyDebugInfo* info)
{
	MonoTableInfo *table = &info->image->tables [MONO_TABLE_METHOD];
	FILE *f;
	char buf [1024];
	int i, mnum, idx;
	int offset = -1;

	if (info->always_create_il || !(info->handle->flags & MONO_DEBUG_FLAGS_DONT_UPDATE_IL_FILES)) {
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

	for (idx = 1; idx <= table->rows; idx++) {
		guint32 token = mono_metadata_make_token (MONO_TABLE_METHOD, idx);
		MonoMethod *method = mono_get_method (info->image, token, NULL);

		if ((method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) ||
		    (method->iflags & METHOD_IMPL_ATTRIBUTE_RUNTIME) ||
		    (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL))
			continue;

		if (method->wrapper_type != MONO_WRAPPER_NONE)
			continue;

		generate_il_offsets (info, method);
	}
}

static void
record_line_number (DebugMethodInfo *priv, gconstpointer address, guint32 line, int is_basic_block)
{
	DebugLineNumberInfo *lni = g_new0 (DebugLineNumberInfo, 1);

	lni->address = address;
	lni->line = line;
	lni->is_basic_block = is_basic_block;
	lni->source_file = priv->source_file;

	g_ptr_array_add (priv->line_numbers, lni);
}

static void
debug_generate_method_lines (AssemblyDebugInfo *info, MonoDebugMethodInfo *minfo, MonoFlowGraph* cfg)
{
	guint32 st_address, st_line;
	DebugMethodInfo *priv = minfo->user_data;
	int i;

	if (!priv)
		return;

	priv->line_numbers = g_ptr_array_new ();

	st_line = priv->first_line;
	st_address = minfo->jit->prologue_end;

	/* record_line_number takes absolute memory addresses. */
	record_line_number (priv, minfo->jit->code_start, priv->start_line, FALSE);

	/* This is the first actual code line of the method. */
	record_line_number (priv, minfo->jit->code_start + st_address, st_line, TRUE);

	/* start lines of basic blocks */
	for (i = 0; i < cfg->block_count; ++i) {
		int j;

		for (j = 0; cfg->bblocks [i].forest && (j < cfg->bblocks [i].forest->len); ++j) {
			MBTree *t = (MBTree *) g_ptr_array_index (cfg->bblocks [i].forest, j);
			gint32 line_inc = 0, addr_inc;

			if (!i && !j) {
				st_line = priv->first_line;
				st_address = t->addr;

				record_line_number (priv, cfg->start + st_address, st_line, TRUE);
			}

			addr_inc = t->addr - st_address;
			st_address += addr_inc;

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

			record_line_number (priv, minfo->jit->code_start + st_address,
					    st_line, j == 0);
		}
	}
}

static void
debug_update_il_offsets (AssemblyDebugInfo *info, MonoDebugMethodInfo *minfo, MonoFlowGraph* cfg)
{
	guint32 old_address, st_address;
	int index, i;

	minfo->jit->il_addresses = g_new0 (guint32, minfo->num_il_offsets);
	if (minfo->num_il_offsets < 2)
		return;

	st_address = old_address = minfo->jit->prologue_end;

	minfo->jit->il_addresses [0] = 0;
	minfo->jit->il_addresses [1] = st_address;
	index = 2;

	/* start lines of basic blocks */
	for (i = 0; i < cfg->block_count; ++i) {
		int j;

		for (j = 0; cfg->bblocks [i].forest && (j < cfg->bblocks [i].forest->len); ++j) {
			MBTree *t = (MBTree *) g_ptr_array_index (cfg->bblocks [i].forest, j);
			gint32 addr_inc;

			if (!i && !j)
				st_address = t->addr;

			addr_inc = t->addr - st_address;
			st_address += addr_inc;

			if (t->cli_addr == -1)
				continue;

			while (minfo->il_offsets [index].offset < t->cli_addr) {
				minfo->jit->il_addresses [index] = old_address;
				if (++index >= minfo->num_il_offsets)
					return;
			}

			minfo->jit->il_addresses [index] = st_address;
			old_address = st_address;
		}
	}

	while (index < minfo->num_il_offsets)
		minfo->jit->il_addresses [index++] = minfo->jit->epilogue_begin;
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

#if 0
	if (!strcmp (image->assembly_name, "corlib"))
		return NULL;
#endif

	debug->dirty = TRUE;

	info = g_new0 (AssemblyDebugInfo, 1);
	info->image = image;
	info->image->ref_count++;
	info->name = g_strdup (image->assembly_name);
	info->format = debug->format;
	info->handle = debug;
	info->methods = g_hash_table_new_full (g_direct_hash, g_direct_equal,
					       NULL, (GDestroyNotify) free_method_info);

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
	case MONO_DEBUG_FORMAT_MONO:
		info->filename = replace_suffix (image->name, "dbg");
		if (g_file_test (info->filename, G_FILE_TEST_EXISTS))
			info->symfile = mono_debug_open_mono_symbol_file (info->image, info->filename, TRUE);
		else if (debug->flags & MONO_DEBUG_FLAGS_MONO_DEBUGGER)
			info->symfile = mono_debug_create_mono_symbol_file (info->image);
		mono_debugger_symbol_file_table_generation++;
		break;

	default:
		break;
	}

	if (debug->format != MONO_DEBUG_FORMAT_MONO)
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

	if (!debug || !debug->dirty)
		return;

	release_symbol_file_table ();
	
	switch (debug->format) {
	case MONO_DEBUG_FORMAT_STABS:
		mono_debug_write_stabs (debug);
		break;
	case MONO_DEBUG_FORMAT_DWARF2:
		mono_debug_write_dwarf2 (debug);
		break;
	case MONO_DEBUG_FORMAT_MONO:
		for (tmp = debug->info; tmp; tmp = tmp->next) {
			AssemblyDebugInfo *info = (AssemblyDebugInfo*)tmp->data;

			if (!info->symfile)
				continue;

			mono_debug_update_mono_symbol_file (info->symfile);
		}
		break;
	default:
		g_assert_not_reached ();
	}

	debug->dirty = FALSE;
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
	case MONO_DEBUG_FORMAT_MONO:
		if (info->symfile != NULL)
			mono_debug_close_mono_symbol_file (info->symfile);
		break;
	default:
		break;
	}
	g_hash_table_destroy (info->methods);
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

	release_symbol_file_table ();
	
	for (debug = mono_debug_handles; debug; debug = temp) {
		GList *tmp;

		if (debug->flags & MONO_DEBUG_FLAGS_UPDATE_ON_EXIT)
			mono_debug_write_symbols (debug);


		for (tmp = debug->info; tmp; tmp = tmp->next) {
			AssemblyDebugInfo* info = (AssemblyDebugInfo*)tmp->data;

			mono_debug_close_assembly (info);
		}

		g_ptr_array_free (debug->source_files, TRUE);
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

	debug->dirty = TRUE;

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

static gint32
il_offset_from_address (MonoDebugMethodInfo *minfo, guint32 address)
{
	int i;

	if (!minfo->jit)
		return -1;

	for (i = 0; i < minfo->num_il_offsets; i++)
		if (minfo->jit->il_addresses [i] > address)
			return minfo->il_offsets [i].offset;

	return -1;
}

static gint32
address_from_il_offset (MonoDebugMethodInfo *minfo, guint32 il_offset)
{
	int i;

	if (!minfo->jit)
		return -1;

	for (i = 0; i < minfo->num_il_offsets; i++)
		if (minfo->il_offsets [i].offset > il_offset)
			return minfo->jit->il_addresses [i];

	return -1;
}

void
mono_debug_add_type (MonoClass *klass)
{
	MonoDebugHandle *debug = mono_debug_handle_from_class (klass);

	g_assert (debug != NULL);

	mono_debug_get_type (debug, klass);
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

static MonoDebugMethodInfo *
lookup_method (MonoMethod *method)
{
	MonoDebugHandle *debug;

	for (debug = mono_debug_handles; debug; debug = debug->next) {
		GList *tmp;

		for (tmp = debug->info; tmp; tmp = tmp->next) {
			AssemblyDebugInfo *info = (AssemblyDebugInfo*)tmp->data;
			MonoDebugMethodInfo *minfo;

			if (info->symfile)
				minfo = mono_debug_find_method (info->symfile, method);
			else
				minfo = g_hash_table_lookup (info->methods, method);

			if (minfo)
				return minfo;
		}
	}

	return NULL;
}

void
mono_debug_add_method (MonoFlowGraph *cfg)
{
	MonoMethod *method = cfg->method;
	MonoClass *klass = method->klass;
	MonoDebugHandle* debug;
	AssemblyDebugInfo* info;
	MonoDebugMethodJitInfo *jit;
	MonoDebugMethodInfo *minfo;
	int i;

	mono_class_init (klass);

	if ((method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) ||
	    (method->iflags & METHOD_IMPL_ATTRIBUTE_RUNTIME) ||
	    (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL))
		return;

	if (method->wrapper_type != MONO_WRAPPER_NONE)
		return;

	debug = mono_debug_handle_from_class (klass);
	if (!debug) {
		if (mono_default_debug_handle)
			debug = mono_default_debug_handle;
		else
			return;
	}

	release_symbol_file_table ();

	info = mono_debug_open_image (debug, klass->image);

	minfo = lookup_method (method);
	if (!minfo || minfo->jit)
		return;

	debug->dirty = TRUE;

	mono_debugger_symbol_file_table_generation++;

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

			locals [i].offset = ptr [i].size;

			begin_offset = il_offset_from_position (cfg, &ptr [i].range.first_use);
			end_offset = il_offset_from_position (cfg, &ptr [i].range.last_use);
			if (end_offset >= 0)
				end_offset++;

			begin_scope = address_from_il_offset (minfo, begin_offset);
			end_scope = address_from_il_offset (minfo, end_offset);

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
}

gchar *
mono_debug_source_location_from_address (MonoMethod *method, guint32 address, guint32 *line_number)
{
	MonoDebugMethodInfo *minfo = lookup_method (method);

	if (!minfo)
		return NULL;

	if (minfo->symfile) {
		gint32 offset = il_offset_from_address (minfo, address);
		
		if (offset < 0)
			return NULL;

		return mono_debug_find_source_location (minfo->symfile, method, offset, line_number);
	}

	return NULL;
}

gint32
mono_debug_il_offset_from_address (MonoMethod *method, gint32 address)
{
	MonoDebugMethodInfo *minfo;

	if (address < 0)
		return -1;

	minfo = lookup_method (method);
	if (!minfo || !minfo->il_offsets)
		return -1;

	return il_offset_from_address (minfo, address);
}

gint32
mono_debug_address_from_il_offset (MonoMethod *method, gint32 il_offset)
{
	MonoDebugMethodInfo *minfo;

	if (il_offset < 0)
		return -1;

	minfo = lookup_method (method);
	if (!minfo || !minfo->il_offsets)
		return -1;

	return address_from_il_offset (minfo, il_offset);
}

static void
release_symbol_file_table ()
{
	guint8 *temp;

	if (!mono_debugger_symbol_file_table)
		return;

	/*
	 * Caution: The debugger may access the memory pointed to by this variable
	 *          at any time.  It is very important to set the pointer to NULL
	 *          before freeing the area.
	 */

	temp = mono_debugger_symbol_file_table;
	mono_debugger_symbol_file_table = NULL;
	g_free (mono_debugger_symbol_file_table);
}

int
mono_debugger_update_symbol_file_table (void)
{
	MonoDebugHandle *debug;
	int dirty = 0, count = 0;
	guint8 *ptr, *symfiles;
	guint32 size;

	for (debug = mono_debug_handles; debug; debug = debug->next) {
		GList *tmp;

		if (debug->format != MONO_DEBUG_FORMAT_MONO)
			continue;

		if (debug->dirty)
			dirty = TRUE;

		for (tmp = debug->info; tmp; tmp = tmp->next) {
			AssemblyDebugInfo *info = (AssemblyDebugInfo*)tmp->data;
			MonoSymbolFile *symfile = info->symfile;

			if (!symfile)
				continue;

			count++;
		}
	}

	if (!dirty)
		return FALSE;

	release_symbol_file_table ();

	size = 2 * sizeof (guint32) + count * sizeof (MonoSymbolFile);
	symfiles = ptr = g_malloc0 (size);
	*((guint32 *) ptr)++ = size;
	*((guint32 *) ptr)++ = count;
	*((guint32 *) ptr)++ = mono_debugger_symbol_file_table_generation;

	for (debug = mono_debug_handles; debug; debug = debug->next) {
		GList *tmp;

		if (debug->format != MONO_DEBUG_FORMAT_MONO)
			continue;

		for (tmp = debug->info; tmp; tmp = tmp->next) {
			AssemblyDebugInfo *info = (AssemblyDebugInfo*)tmp->data;
			MonoSymbolFile *symfile = info->symfile;

			if (!symfile)
				continue;

			if (debug->dirty)
				mono_debug_update_mono_symbol_file (info->symfile);

			*((MonoSymbolFile *) ptr)++ = *symfile;
		}
	}
	
	mono_debugger_symbol_file_table = symfiles;
	return TRUE;
}
