#include <config.h>
#include <stdlib.h>
#include <string.h>
#include <errno.h>
#include <signal.h>
#include <sys/stat.h>
#include <unistd.h>
#include <mono/metadata/class.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/debug-mono-symfile.h>
#include <mono/jit/codegen.h>
#include <mono/jit/debug.h>

#include "debug-private.h"
#include "helpers.h"

#ifndef PLATFORM_WIN32
/*
 * NOTE:  Functions and variables starting with `mono_debug_' and `debug_' are
 *        part of the general debugging code.
 *
 *        Functions and variables starting with `mono_debugger_' and `debugger_'
 *        are only used when the JIT is running inside the Mono Debugger.
 *
 * FIXME: This file needs some API loving.
 */

/* This is incremented each time the symbol table is modified.
 * The debugger looks at this variable and if it has a higher value than its current
 * copy of the symbol table, it must call debugger_update_symbol_file_table().
 */
static guint32 debugger_symbol_file_table_generation = 0;
static guint32 debugger_symbol_file_table_modified = 0;

/* Caution: This variable may be accessed at any time from the debugger;
 *          it is very important not to modify the memory it is pointing to
 *          without previously setting this pointer back to NULL.
 */
static MonoDebuggerSymbolFileTable *debugger_symbol_file_table = NULL;

/* Caution: This function MUST be called before touching the symbol table! */
static void release_symbol_file_table (void);

static void initialize_debugger_support (void);

static gboolean running_in_the_mono_debugger = FALSE;

static MonoDebugHandle *mono_debug_handle = NULL;
static gboolean mono_debug_initialized = FALSE;
static gconstpointer debugger_notification_address = NULL;

static CRITICAL_SECTION debugger_lock_mutex;

static gpointer debugger_thread_cond;
static gpointer debugger_start_cond;

static gpointer debugger_finished_cond;
static CRITICAL_SECTION debugger_finished_mutex;

static gboolean debugger_signalled = FALSE;
static gboolean must_send_finished = FALSE;

extern void (*mono_debugger_class_init_func) (MonoClass *klass);

static guint64 debugger_insert_breakpoint (guint64 method_argument, const gchar *string_argument);
static guint64 debugger_remove_breakpoint (guint64 breakpoint);
static int debugger_update_symbol_file_table (void);
static int debugger_update_symbol_file_table_internal (void);
static gpointer debugger_compile_method (MonoMethod *method);

static void mono_debug_add_assembly (MonoAssembly *assembly, gpointer user_data);
static void mono_debug_close_assembly (AssemblyDebugInfo* info);
static AssemblyDebugInfo *mono_debug_open_image (MonoDebugHandle* debug, MonoImage *image);

/*
 * This is a global data symbol which is read by the debugger.
 */
MonoDebuggerInfo MONO_DEBUGGER__debugger_info = {
	MONO_SYMBOL_FILE_DYNAMIC_MAGIC,
	MONO_SYMBOL_FILE_DYNAMIC_VERSION,
	sizeof (MonoDebuggerInfo),
	&mono_generic_trampoline_code,
	&mono_breakpoint_trampoline_code,
	&debugger_symbol_file_table_generation,
	&debugger_symbol_file_table_modified,
	&debugger_notification_address,
	&debugger_symbol_file_table,
	&debugger_compile_method,
	&debugger_insert_breakpoint,
	&debugger_remove_breakpoint,
	&mono_runtime_invoke
};

static void
debugger_init_threads (void)
{
	debugger_thread_cond = CreateSemaphore (NULL, 0, 1, NULL);
	debugger_start_cond = CreateSemaphore (NULL, 0, 1, NULL);

	InitializeCriticalSection (&debugger_finished_mutex);
	debugger_finished_cond = CreateSemaphore (NULL, 0, 1, NULL);
}

static void
mono_debug_init (void)
{
	if (!mono_debug_initialized) {
		InitializeCriticalSection (&debugger_lock_mutex);
		mono_debug_initialized = TRUE;
	}
}

static void
mono_debug_lock (void)
{
	if (mono_debug_initialized)
		EnterCriticalSection (&debugger_lock_mutex);
}

static void
mono_debug_unlock (void)
{
	if (mono_debug_initialized)
		LeaveCriticalSection (&debugger_lock_mutex);
}

static void
mono_debugger_signal (gboolean modified)
{
	g_assert (running_in_the_mono_debugger);
	if (modified)
		debugger_symbol_file_table_modified = TRUE;
	mono_debug_lock ();
	if (!debugger_signalled) {
		debugger_signalled = TRUE;
		ReleaseSemaphore (debugger_thread_cond, 1, NULL);
	}
	mono_debug_unlock ();
}

static void
mono_debugger_wait (void)
{
	g_assert (running_in_the_mono_debugger);
	LeaveCriticalSection (&debugger_finished_mutex);
	g_assert (WaitForSingleObject (debugger_finished_cond, INFINITE) == WAIT_OBJECT_0);
	EnterCriticalSection (&debugger_finished_mutex);
}

static void
free_method_info (MonoDebugMethodInfo *minfo)
{
	DebugMethodInfo *priv = minfo->user_data;

	if (priv) {
		g_free (priv->name);
		g_free (priv);
	}

	if (minfo->jit) {
		g_array_free (minfo->jit->line_numbers, TRUE);
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
mono_debug_open (MonoAssembly *assembly, MonoDebugFormat format, const char **args)
{
	MonoDebugHandle *debug;
	const char **ptr;

	g_assert (!mono_debug_handle);

	mono_debug_init ();

	debug = g_new0 (MonoDebugHandle, 1);
	debug->name = g_strdup (assembly->image->name);
	debug->format = format;
	debug->producer_name = g_strdup_printf ("Mono JIT compiler version %s", VERSION);
	debug->next_idx = 100;
	debug->dirty = TRUE;

	debug->type_hash = g_hash_table_new (NULL, NULL);
	debug->source_files = g_ptr_array_new ();

	debug->images = g_hash_table_new_full (NULL, NULL, NULL,
					       (GDestroyNotify) mono_debug_close_assembly);

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
		case MONO_DEBUG_FORMAT_MONO_DEBUGGER:
			debug->flags |= MONO_DEBUG_FLAGS_DONT_UPDATE_IL_FILES |
				MONO_DEBUG_FLAGS_DONT_CREATE_IL_FILES;
			break;
		default:
			break;
		}

		if ((debug->format != MONO_DEBUG_FORMAT_MONO) &&
		    (debug->format != MONO_DEBUG_FORMAT_MONO_DEBUGGER)) {
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
		mono_debugger_class_init_func = mono_debug_add_type;
		break;
	case MONO_DEBUG_FORMAT_MONO_DEBUGGER:
		mono_debugger_class_init_func = mono_debug_add_type;
		initialize_debugger_support ();
		break;
	default:
		g_assert_not_reached ();
	}

	mono_debug_lock ();
	release_symbol_file_table ();

	mono_debug_handle = debug;
	mono_install_assembly_load_hook (mono_debug_add_assembly, NULL);

	mono_debug_open_image (mono_debug_handle, assembly->image);
	mono_debug_open_image (mono_debug_handle, mono_defaults.corlib);

	mono_debug_add_type (mono_defaults.object_class);
	mono_debug_add_type (mono_defaults.object_class);
	mono_debug_add_type (mono_defaults.byte_class);
	mono_debug_add_type (mono_defaults.void_class);
	mono_debug_add_type (mono_defaults.boolean_class);
	mono_debug_add_type (mono_defaults.sbyte_class);
	mono_debug_add_type (mono_defaults.int16_class);
	mono_debug_add_type (mono_defaults.uint16_class);
	mono_debug_add_type (mono_defaults.int32_class);
	mono_debug_add_type (mono_defaults.uint32_class);
	mono_debug_add_type (mono_defaults.int_class);
	mono_debug_add_type (mono_defaults.uint_class);
	mono_debug_add_type (mono_defaults.int64_class);
	mono_debug_add_type (mono_defaults.uint64_class);
	mono_debug_add_type (mono_defaults.single_class);
	mono_debug_add_type (mono_defaults.double_class);
	mono_debug_add_type (mono_defaults.char_class);
	mono_debug_add_type (mono_defaults.string_class);
	mono_debug_add_type (mono_defaults.enum_class);
	mono_debug_add_type (mono_defaults.array_class);
	mono_debug_add_type (mono_defaults.multicastdelegate_class);
	mono_debug_add_type (mono_defaults.asyncresult_class);
	mono_debug_add_type (mono_defaults.waithandle_class);
	mono_debug_add_type (mono_defaults.typehandle_class);
	mono_debug_add_type (mono_defaults.fieldhandle_class);
	mono_debug_add_type (mono_defaults.methodhandle_class);
	mono_debug_add_type (mono_defaults.monotype_class);
	mono_debug_add_type (mono_defaults.exception_class);
	mono_debug_add_type (mono_defaults.threadabortexception_class);
	mono_debug_add_type (mono_defaults.thread_class);
	mono_debug_add_type (mono_defaults.transparent_proxy_class);
	mono_debug_add_type (mono_defaults.real_proxy_class);
	mono_debug_add_type (mono_defaults.mono_method_message_class);
	mono_debug_add_type (mono_defaults.appdomain_class);
	mono_debug_add_type (mono_defaults.field_info_class);
	mono_debug_add_type (mono_defaults.stringbuilder_class);
	mono_debug_add_type (mono_defaults.math_class);
	mono_debug_add_type (mono_defaults.stack_frame_class);
	mono_debug_add_type (mono_defaults.stack_trace_class);
	mono_debug_add_type (mono_defaults.marshal_class);
	mono_debug_add_type (mono_defaults.iserializeable_class);
	mono_debug_add_type (mono_defaults.serializationinfo_class);
	mono_debug_add_type (mono_defaults.streamingcontext_class);

	debugger_update_symbol_file_table ();

	mono_debug_unlock ();

	return debug;
}

static void
mono_debug_add_assembly (MonoAssembly *assembly, gpointer user_data)
{
	if (!mono_debug_handle)
		return;

	mono_debug_lock ();
	mono_debug_open_image (mono_debug_handle, assembly->image);
	mono_debug_unlock ();
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

	minfo->start_line = priv->first_line;
	minfo->end_line = priv->last_line;

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
			need_update = TRUE;
		} else if (statb.st_mtime < stata.st_mtime)
			need_update = TRUE;

		if (need_update) {
#ifndef PLATFORM_WIN32
			struct sigaction act, oldact;
			sigset_t old_set;
#endif
			int ret;

#ifndef PLATFORM_WIN32
			act.sa_handler = SIG_IGN;
			act.sa_flags = SA_NOCLDSTOP | SA_RESTART;
			sigemptyset (&act.sa_mask);
			sigaddset (&act.sa_mask, SIGCHLD);
			sigprocmask (SIG_BLOCK, &act.sa_mask, &old_set);
			sigaction (SIGCHLD, &act, &oldact);
#endif
			
			g_print ("Recreating %s from %s.\n", info->ilfile, info->image->name);

			ret = system (command);

#ifndef PLATFORM_WIN32
			sigaction (SIGCHLD, &oldact, NULL);
			sigprocmask (SIG_SETMASK, &old_set, NULL);
#endif

			if (ret) {
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
generate_line_number (MonoDebugMethodInfo *minfo, guint32 address, guint32 offset, int debug)
{
	int i;

	if (debug)
		g_message (G_STRLOC ": searching IL offset %x", offset);

	for (i = minfo->num_il_offsets - 1; i >= 0; i--) {
		MonoDebugLineNumberEntry *lne;

		if (minfo->il_offsets [i].offset > offset)
			continue;

		if (debug)
			g_message (G_STRLOC ": found entry %d: offset = %x, row = %d",
				   i, minfo->il_offsets [i].offset, minfo->il_offsets [i].row);

		if (minfo->jit->line_numbers->len) {
			MonoDebugLineNumberEntry last = g_array_index (
				minfo->jit->line_numbers, MonoDebugLineNumberEntry,
				minfo->jit->line_numbers->len - 1);

			/* Avoid writing more than one entry for the same line. */
			if (minfo->il_offsets [i].row == last.line) {
				if (debug)
					g_message (G_STRLOC ": skipping line: line = %d, last line = %d, "
						   "last address = %x, address = %x, "
						   "last offset = %x, offset = %x",
						   last.line, minfo->il_offsets [i].row,
						   last.address, address, last.offset, offset);

				return;
			}
		}

		if (debug)
			g_message (G_STRLOC ": writing entry: line = %d, offfset = %x, address = %x",
				   minfo->il_offsets [i].row, offset, address);

		lne = g_new0 (MonoDebugLineNumberEntry, 1);
		lne->address = address;
		lne->offset = offset;
		lne->line = minfo->il_offsets [i].row;

		g_array_append_val (minfo->jit->line_numbers, *lne);
		return;
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
	if (info->symfile->is_dynamic)
		return;

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

	generate_line_number (minfo, address, offset, debug);

	/* start lines of basic blocks */
	for (i = 0; i < cfg->block_count; ++i) {
		int j;

		for (j = 0; cfg->bblocks [i].forest && (j < cfg->bblocks [i].forest->len); ++j) {
			MBTree *t = (MBTree *) g_ptr_array_index (cfg->bblocks [i].forest, j);

			if ((t->cli_addr == -1) || (t->cli_addr == offset) || (t->addr == address))
				continue;

			offset = t->cli_addr;
			address = t->addr;

			generate_line_number (minfo, address, offset, debug);
		}
	}

	generate_line_number (minfo, minfo->jit->epilogue_begin, header->code_size, debug);

	if (debug) {
		for (i = 0; i < minfo->jit->line_numbers->len; i++) {
			MonoDebugLineNumberEntry lne = g_array_index (
				minfo->jit->line_numbers, MonoDebugLineNumberEntry, i);

			g_message (G_STRLOC ": %x,%x,%d", lne.address, lne.offset, lne.line);
		}
	}
}

static AssemblyDebugInfo *
mono_debug_get_image (MonoDebugHandle* debug, MonoImage *image)
{
	return g_hash_table_lookup (debug->images, image);
}

static AssemblyDebugInfo *
mono_debug_open_image (MonoDebugHandle* debug, MonoImage *image)
{
	AssemblyDebugInfo *info;
	MonoAssembly **ptr;

	info = mono_debug_get_image (debug, image);
	if (info != NULL)
		return info;

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

	g_hash_table_insert (debug->images, image, info);

	info->nmethods = image->tables [MONO_TABLE_METHOD].rows + 1;
	info->mlines = g_new0 (int, info->nmethods);

	for (ptr = image->references; ptr && *ptr; ptr++)
		mono_debug_add_assembly (*ptr, NULL);

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
	case MONO_DEBUG_FORMAT_MONO_DEBUGGER:
		info->filename = replace_suffix (image->name, "dbg");
		if (g_file_test (info->filename, G_FILE_TEST_EXISTS))
			info->symfile = mono_debug_open_mono_symbol_file (info->image, info->filename, TRUE);
		else if (info->format == MONO_DEBUG_FORMAT_MONO_DEBUGGER)
			info->symfile = mono_debug_create_mono_symbol_file (info->image);
		debugger_symbol_file_table_generation++;
		break;

	default:
		break;
	}

	if ((debug->format != MONO_DEBUG_FORMAT_MONO) && (debug->format != MONO_DEBUG_FORMAT_MONO_DEBUGGER))
		debug_load_method_lines (info);

	return info;
}

void
mono_debug_write_symbols (MonoDebugHandle *debug)
{
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
	case MONO_DEBUG_FORMAT_MONO_DEBUGGER:
		break;
	default:
		g_assert_not_reached ();
	}

	debug->dirty = FALSE;
}

void
mono_debug_make_symbols (void)
{
	if (!mono_debug_handle || !mono_debug_handle->dirty)
		return;
	
	switch (mono_debug_handle->format) {
	case MONO_DEBUG_FORMAT_STABS:
		mono_debug_write_stabs (mono_debug_handle);
		break;
	case MONO_DEBUG_FORMAT_DWARF2:
		mono_debug_write_dwarf2 (mono_debug_handle);
		break;
	case MONO_DEBUG_FORMAT_MONO:
	case MONO_DEBUG_FORMAT_MONO_DEBUGGER:
		debugger_update_symbol_file_table ();
		break;
	default:
		g_assert_not_reached ();
	}

	mono_debug_handle->dirty = FALSE;
}

static void
mono_debug_close_assembly (AssemblyDebugInfo* info)
{
	switch (info->format) {
	case MONO_DEBUG_FORMAT_MONO:
	case MONO_DEBUG_FORMAT_MONO_DEBUGGER:
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
	release_symbol_file_table ();

	if (!mono_debug_handle)
		return;

	if (mono_debug_handle->flags & MONO_DEBUG_FLAGS_UPDATE_ON_EXIT)
		mono_debug_write_symbols (mono_debug_handle);

	g_hash_table_destroy (mono_debug_handle->images);
	g_ptr_array_free (mono_debug_handle->source_files, TRUE);
	g_hash_table_destroy (mono_debug_handle->type_hash);
	g_free (mono_debug_handle->producer_name);
	g_free (mono_debug_handle->name);
	g_free (mono_debug_handle);

	mono_debug_handle = NULL;
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

static gint32
il_offset_from_address (MonoDebugMethodInfo *minfo, guint32 address)
{
	int i;

	if (!minfo->jit || !minfo->jit->line_numbers)
		return -1;

	for (i = minfo->jit->line_numbers->len - 1; i >= 0; i--) {
		MonoDebugLineNumberEntry lne = g_array_index (
			minfo->jit->line_numbers, MonoDebugLineNumberEntry, i);

		if (lne.address <= address)
			return lne.offset;
	}

	return -1;
}

static gint32
address_from_il_offset (MonoDebugMethodInfo *minfo, guint32 il_offset)
{
	int i;

	if (!minfo->jit || !minfo->jit->line_numbers)
		return -1;

	for (i = minfo->jit->line_numbers->len - 1; i >= 0; i--) {
		MonoDebugLineNumberEntry lne = g_array_index (
			minfo->jit->line_numbers, MonoDebugLineNumberEntry, i);

		if (lne.offset <= il_offset)
			return lne.address;
	}

	return -1;
}

void
mono_debug_add_type (MonoClass *klass)
{
	AssemblyDebugInfo* info;

	if (!mono_debug_handle)
		return;

	info = mono_debug_get_image (mono_debug_handle, klass->image);
	g_assert (info);

	if ((mono_debug_handle->format != MONO_DEBUG_FORMAT_MONO) &&
	    (mono_debug_handle->format != MONO_DEBUG_FORMAT_MONO_DEBUGGER))
		return;

	if (info->symfile) {
		mono_debug_lock ();
		mono_debug_symfile_add_type (info->symfile, klass);
		if (running_in_the_mono_debugger)
			mono_debugger_signal (TRUE);
		mono_debug_unlock ();
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

struct LookupMethodData
{
	MonoDebugMethodInfo *minfo;
	MonoMethod *method;
};

static void
lookup_method_func (gpointer key, gpointer value, gpointer user_data)
{
	AssemblyDebugInfo *info = (AssemblyDebugInfo *) value;
	struct LookupMethodData *data = (struct LookupMethodData *) user_data;

	if (data->minfo)
		return;

	if (info->symfile)
		data->minfo = mono_debug_find_method (info->symfile, data->method);
	else
		data->minfo = g_hash_table_lookup (info->methods, data->method);
}

static MonoDebugMethodInfo *
lookup_method (MonoMethod *method)
{
	struct LookupMethodData data = { NULL, method };

	if (!mono_debug_handle)
		return NULL;

	g_hash_table_foreach (mono_debug_handle->images, lookup_method_func, &data);
	return data.minfo;
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

	if (method->wrapper_type != MONO_WRAPPER_NONE)
		return;

	info = mono_debug_get_image (mono_debug_handle, klass->image);
	g_assert (info);

	minfo = lookup_method (method);
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
	if ((info->format == MONO_DEBUG_FORMAT_MONO) || (info->format == MONO_DEBUG_FORMAT_MONO_DEBUGGER))
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
				begin_scope = address_from_il_offset (minfo, begin_offset);
			else
				begin_scope = -1;
			if (end_offset >= 0)
				end_scope = address_from_il_offset (minfo, end_offset);
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
		if (running_in_the_mono_debugger)
			mono_debugger_signal (TRUE);
	}

	mono_debug_unlock ();
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
	MonoDebuggerSymbolFileTable *temp;

	if (!debugger_symbol_file_table)
		return;

	/*
	 * Caution: The debugger may access the memory pointed to by this variable
	 *          at any time.  It is very important to set the pointer to NULL
	 *          before freeing the area.
	 */

	temp = debugger_symbol_file_table;
	debugger_symbol_file_table = NULL;
	g_free (debugger_symbol_file_table);
}

static void
update_symbol_file_table_count_func (gpointer key, gpointer value, gpointer user_data)
{
	AssemblyDebugInfo *info = (AssemblyDebugInfo *) value;

	if (!info->symfile)
		return;
	if ((info->format != MONO_DEBUG_FORMAT_MONO) && (info->format != MONO_DEBUG_FORMAT_MONO_DEBUGGER))
		return;

	++ (* (int *) user_data);
}

struct SymfileTableData
{
	MonoDebuggerSymbolFileTable *symfile_table;
	int index;
};

static void
update_symbol_file_table_func (gpointer key, gpointer value, gpointer user_data)
{
	AssemblyDebugInfo *info = (AssemblyDebugInfo *) value;
	struct SymfileTableData *data = (struct SymfileTableData *) user_data;

	if (!info->symfile)
		return;
	if ((info->format != MONO_DEBUG_FORMAT_MONO) && (info->format != MONO_DEBUG_FORMAT_MONO_DEBUGGER))
		return;

	data->symfile_table->symfiles [data->index++] = info->symfile;
}

static int
debugger_update_symbol_file_table_internal (void)
{
	int count = 0;
	MonoDebuggerSymbolFileTable *symfile_table;
	struct SymfileTableData data;
	guint32 size;

	if (!mono_debug_handle)
		return FALSE;

	mono_debug_lock ();

	g_hash_table_foreach (mono_debug_handle->images, update_symbol_file_table_count_func, &count);

	release_symbol_file_table ();

	size = sizeof (MonoDebuggerSymbolFileTable) + count * sizeof (MonoSymbolFile *);
	symfile_table = g_malloc0 (size);
	symfile_table->magic = MONO_SYMBOL_FILE_DYNAMIC_MAGIC;
	symfile_table->version = MONO_SYMBOL_FILE_DYNAMIC_VERSION;
	symfile_table->total_size = size;
	symfile_table->count = count;
	symfile_table->generation = debugger_symbol_file_table_generation;
	symfile_table->global_symfile = mono_debugger_global_symbol_file;

	data.symfile_table = symfile_table;
	data.index = 0;

	g_hash_table_foreach (mono_debug_handle->images, update_symbol_file_table_func, &data);

	debugger_symbol_file_table = symfile_table;

	mono_debug_unlock ();

	return TRUE;
}

static int
debugger_update_symbol_file_table (void)
{
	int retval;

	if (!mono_debug_handle)
		return FALSE;

	mono_debug_lock ();
	retval = debugger_update_symbol_file_table_internal ();
	mono_debug_unlock ();

	return retval;
}

static pid_t debugger_background_thread;
extern void mono_debugger_init_thread_debug (pid_t);

/*
 * NOTE: We must not call any functions here which we ever may want to debug !
 */
static guint32
debugger_thread_func (gpointer ptr)
{
	void (*notification_code) (void) = ptr;
	int last_generation = 0;

	mono_new_thread_init (NULL, &last_generation);
	debugger_background_thread = getpid ();

	/*
	 * The parent thread waits on this condition because it needs our pid.
	 */
	ReleaseSemaphore (debugger_start_cond, 1, NULL);

	/*
	 * This mutex is locked by the parent thread until the debugger actually
	 * attached to us, so we don't need a SIGSTOP here anymore.
	 */
	mono_debug_lock ();

	while (TRUE) {
		/* Wait for an event. */
		mono_debug_unlock ();
		g_assert (WaitForSingleObject (debugger_thread_cond, INFINITE) == WAIT_OBJECT_0);
		mono_debug_lock ();

		/* Reload the symbol file table if necessary. */
		if (debugger_symbol_file_table_generation > last_generation) {
			debugger_update_symbol_file_table_internal ();
			last_generation = debugger_symbol_file_table_generation;
		}

		/*
		 * Send notification - we'll stop on a breakpoint instruction at a special
		 * address.  The debugger will reload the symbol tables while we're stopped -
		 * and owning the `debugger_thread_mutex' so that no other thread can touch
		 * them in the meantime.
		 */
		notification_code ();

		/* Clear modified and signalled flag. */
		debugger_symbol_file_table_modified = FALSE;
		debugger_signalled = FALSE;

		if (must_send_finished) {
			EnterCriticalSection (&debugger_finished_mutex);
			ReleaseSemaphore (debugger_finished_cond, 1, NULL);
			must_send_finished = FALSE;
			LeaveCriticalSection (&debugger_finished_mutex);
		}
	}

	return 0;
}

static void
initialize_debugger_support ()
{
	guint8 *buf, *ptr;
	HANDLE thread;

	if (running_in_the_mono_debugger)
		return;
	debugger_init_threads ();
	running_in_the_mono_debugger = TRUE;

	ptr = buf = g_malloc0 (16);
	x86_breakpoint (buf);
	debugger_notification_address = buf;
	x86_ret (buf);

	/*
	 * We keep this mutex until mono_debugger_jit_exec().
	 */
	mono_debug_lock ();

	/*
	 * This mutex is only unlocked in mono_debugger_wait().
	 */
	EnterCriticalSection (&debugger_finished_mutex);

	thread = CreateThread (NULL, 0, debugger_thread_func, ptr, FALSE, NULL);
	g_assert (thread);

	/*
	 * Wait until the background thread set its pid.
	 */
	g_assert (WaitForSingleObject (debugger_start_cond, INFINITE) == WAIT_OBJECT_0);

	/*
	 * Wait until the debugger thread has actually been started and we
	 * have its pid, then actually start the background thread.
	 */
#ifndef PLATFORM_WIN32
	mono_debugger_init_thread_debug (debugger_background_thread);
#endif
}

static GPtrArray *breakpoints = NULL;

int
mono_insert_breakpoint_full (MonoMethodDesc *desc, gboolean use_trampoline)
{
	static int last_breakpoint_id = 0;
	MonoDebuggerBreakpointInfo *info;

	info = g_new0 (MonoDebuggerBreakpointInfo, 1);
	info->desc = desc;
	info->use_trampoline = use_trampoline;
	info->index = ++last_breakpoint_id;

	if (!breakpoints)
		breakpoints = g_ptr_array_new ();

	g_ptr_array_add (breakpoints, info);

	return info->index;
}

static guint64
debugger_insert_breakpoint (guint64 method_argument, const gchar *string_argument)
{
	MonoMethodDesc *desc;

	desc = mono_method_desc_new (string_argument, FALSE);
	if (!desc)
		return 0;

	return mono_insert_breakpoint_full (desc, TRUE);
}

static guint64
debugger_remove_breakpoint (guint64 breakpoint)
{
	int i;

	if (!breakpoints)
		return 0;

	for (i = 0; i < breakpoints->len; i++) {
		MonoDebuggerBreakpointInfo *info = g_ptr_array_index (breakpoints, i);

		if (info->index != breakpoint)
			continue;

		mono_method_desc_free (info->desc);
		g_ptr_array_remove (breakpoints, info);
		g_free (info);
		return 1;
	}

	return 0;
}

static gpointer
debugger_compile_method (MonoMethod *method)
{
	gpointer retval;

	mono_debug_lock ();
	retval = mono_compile_method (method);
	mono_debugger_signal (FALSE);
	mono_debug_unlock ();
	return retval;
}

int
mono_remove_breakpoint (int breakpoint_id)
{
	return debugger_remove_breakpoint (breakpoint_id);
}

int
mono_insert_breakpoint (const gchar *method_name, gboolean include_namespace)
{
	MonoMethodDesc *desc;

	desc = mono_method_desc_new (method_name, include_namespace);
	if (!desc)
		return 0;

	return mono_insert_breakpoint_full (desc, running_in_the_mono_debugger);
}

int
mono_method_has_breakpoint (MonoMethod* method, gboolean use_trampoline)
{
	int i;

	if (!breakpoints || (method->wrapper_type != MONO_WRAPPER_NONE))
		return 0;

	for (i = 0; i < breakpoints->len; i++) {
		MonoDebuggerBreakpointInfo *info = g_ptr_array_index (breakpoints, i);

		if (info->use_trampoline != use_trampoline)
			continue;

		if (!mono_method_desc_full_match (info->desc, method))
			continue;

		return info->index;
	}

	return 0;
}

void
mono_debugger_trampoline_breakpoint_callback (void)
{
	mono_debug_lock ();
	must_send_finished = TRUE;
	mono_debugger_signal (FALSE);
	mono_debug_unlock ();

	mono_debugger_wait ();
}

/*
 * This is a custom version of mono_jit_exec() which is used when we're being run inside
 * the Mono Debugger.
 */
int 
mono_debugger_jit_exec (MonoDomain *domain, MonoAssembly *assembly, int argc, char *argv[])
{
	MonoImage *image = assembly->image;
	MonoMethod *method;
	gpointer addr;
	int retval;

	method = mono_get_method (image, mono_image_get_entry_point (image), NULL);

	addr = mono_compile_method (method);

	/*
	 * The mutex has been locked in initialize_debugger_support(); we keep it locked
	 * until we compiled the main method and signalled the debugger.
	 */
	must_send_finished = TRUE;
	mono_debugger_signal (TRUE);
	mono_debug_unlock ();

	/*
	 * Wait until the debugger has loaded the initial symbol tables.
	 */
	mono_debugger_wait ();

	retval = mono_runtime_run_main (method, argc, argv, NULL);

	/*
	 * Kill the background thread.
	 */
#ifndef PLATFORM_WIN32
	/* Someone should look at this too */
	kill (debugger_background_thread, SIGKILL);
#endif

	return retval;
}
#else

MonoDebugHandle*
mono_debug_open (MonoAssembly *assembly, MonoDebugFormat format, const char **args)
{
	return NULL;
}

void
mono_debug_write_symbols (MonoDebugHandle *debug)
{
}

void
mono_debug_make_symbols (void)
{
}

void
mono_debug_cleanup (void)
{
}

guint32
mono_debug_get_type (MonoDebugHandle *debug, MonoClass *klass)
{
	return 0;
}

void
mono_debug_add_type (MonoClass *klass)
{
}

void
mono_debug_add_method (MonoFlowGraph *cfg)
{
}

gchar *
mono_debug_source_location_from_address (MonoMethod *method, guint32 address, guint32 *line_number)
{
	return NULL;
}

gint32
mono_debug_il_offset_from_address (MonoMethod *method, gint32 address)
{
	return -1;
}

gint32
mono_debug_address_from_il_offset (MonoMethod *method, gint32 il_offset)
{
	return 0;
}

int
mono_insert_breakpoint_full (MonoMethodDesc *desc, gboolean use_trampoline)
{
	return 0;
}

int
mono_remove_breakpoint (int breakpoint_id)
{
	return 0;
}

int
mono_insert_breakpoint (const gchar *method_name, gboolean include_namespace)
{
	return 0;
}

int
mono_method_has_breakpoint (MonoMethod* method, gboolean use_trampoline)
{
	return 0;
}

void
mono_debugger_trampoline_breakpoint_callback (void)
{
}

int 
mono_debugger_jit_exec (MonoDomain *domain, MonoAssembly *assembly, int argc, char *argv[])
{
	return -1;
}
#endif /* PLATFORM_WIN32 */
