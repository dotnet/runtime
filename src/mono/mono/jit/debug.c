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

#include "debug-private.h"

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
 * copy of the symbol table, it must call mono_debug_update_symbol_file_table().
 */
guint32 mono_debugger_symbol_file_table_generation = 0;
guint32 mono_debugger_symbol_file_table_modified = 0;

/* Caution: This variable may be accessed at any time from the debugger;
 *          it is very important not to modify the memory it is pointing to
 *          without previously setting this pointer back to NULL.
 */
MonoDebuggerSymbolFileTable *mono_debugger_symbol_file_table = NULL;

/* Caution: This function MUST be called before touching the symbol table! */
static void release_symbol_file_table (void);

MonoDebugHandle *mono_debug_handle = NULL;
gboolean mono_debug_initialized = FALSE;

static CRITICAL_SECTION debugger_lock_mutex;

extern void (*mono_debugger_class_init_func) (MonoClass *klass);

static void mono_debug_add_assembly (MonoAssembly *assembly, gpointer user_data);
static void mono_debug_close_assembly (AssemblyDebugInfo* info);
static AssemblyDebugInfo *mono_debug_open_image (MonoDebugHandle* debug, MonoImage *image);

static int running_in_the_mono_debugger = FALSE;
void (*mono_debugger_event_handler) (MonoDebuggerEvent event, gpointer data, gpointer data2) = NULL;

#ifndef PLATFORM_WIN32

MonoDebuggerIOLayer mono_debugger_io_layer = {
	InitializeCriticalSection, DeleteCriticalSection, TryEnterCriticalSection,
	EnterCriticalSection, LeaveCriticalSection, WaitForSingleObject, SignalObjectAndWait,
	WaitForMultipleObjects, CreateSemaphore, ReleaseSemaphore, CreateThread
};

#endif

void
mono_debugger_event (MonoDebuggerEvent event, gpointer data, gpointer data2)
{
	if (mono_debugger_event_handler)
		(* mono_debugger_event_handler) (event, data, data2);
}

void
mono_debug_init (int in_the_debugger)
{
	if (mono_debug_initialized)
		return;

	InitializeCriticalSection (&debugger_lock_mutex);
	mono_debug_initialized = TRUE;
	running_in_the_mono_debugger = in_the_debugger;
}

gpointer
mono_debug_create_notification_function (gpointer *notification_address)
{
	guint8 *ptr, *buf;

	ptr = buf = g_malloc0 (16);
	mono_debug_codegen_breakpoint (&buf);
	if (notification_address)
		*notification_address = buf;
	mono_debug_codegen_ret (&buf);

	return ptr;
}

void
mono_debug_lock (void)
{
	if (mono_debug_initialized)
		EnterCriticalSection (&debugger_lock_mutex);
}

void
mono_debug_unlock (void)
{
	if (mono_debug_initialized)
		LeaveCriticalSection (&debugger_lock_mutex);
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
free_wrapper_info (DebugWrapperInfo *winfo)
{
	g_free (winfo);
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

	g_assert (!mono_debug_handle);

	debug = g_new0 (MonoDebugHandle, 1);
	debug->name = g_strdup (assembly->image->name);
	debug->format = format;
	debug->source_files = g_ptr_array_new ();
	debug->producer_name = g_strdup_printf ("Mono JIT compiler version %s", VERSION);
	debug->next_idx = 100;
	debug->dirty = TRUE;

	debug->type_hash = g_hash_table_new (NULL, NULL);

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

	mono_debug_update_symbol_file_table ();

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

void
_mono_debug_generate_line_number (MonoDebugMethodInfo *minfo, guint32 address, guint32 offset, int debug)
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

AssemblyDebugInfo *
_mono_debug_get_image (MonoDebugHandle* debug, MonoImage *image)
{
	return g_hash_table_lookup (debug->images, image);
}

static AssemblyDebugInfo *
mono_debug_open_image (MonoDebugHandle* debug, MonoImage *image)
{
	AssemblyDebugInfo *info;
	MonoAssembly **ptr;

	info = _mono_debug_get_image (debug, image);
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
	info->wrapper_methods = g_hash_table_new_full (g_direct_hash, g_direct_equal,
						       NULL, (GDestroyNotify) free_wrapper_info);

	g_hash_table_insert (debug->images, image, info);

	info->nmethods = image->tables [MONO_TABLE_METHOD].rows + 1;
	info->mlines = g_new0 (int, info->nmethods);

	for (ptr = image->references; ptr && *ptr; ptr++)
		mono_debug_add_assembly (*ptr, NULL);

	if (image->assembly->dynamic)
		return info;

	switch (info->format) {
	case MONO_DEBUG_FORMAT_STABS:
	case MONO_DEBUG_FORMAT_DWARF2:
		if (debug->flags & MONO_DEBUG_FLAGS_INSTALL_IL_FILES) {
			gchar *dirname = g_path_get_dirname (image->name);
			info->ilfile = g_strdup_printf ("%s/%s.il", dirname, info->name);
			g_free (dirname);
		} else
			info->ilfile = g_strdup_printf ("%s.il", info->name);
		info->source_file = debug->source_files->len;
		g_ptr_array_add (debug->source_files, info->ilfile);
		break;
	case MONO_DEBUG_FORMAT_MONO:
		info->symfile = mono_debug_open_mono_symbol_file (info->image, running_in_the_mono_debugger);
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
		mono_debug_update_symbol_file_table ();
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
		if (info->symfile != NULL)
			mono_debug_close_mono_symbol_file (info->symfile);
		break;
	default:
		break;
	}
	g_hash_table_destroy (info->methods);
	g_hash_table_destroy (info->wrapper_methods);
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
	g_ptr_array_free (mono_debug_handle->source_files, FALSE);
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

gint32
_mono_debug_address_from_il_offset (MonoDebugMethodInfo *minfo, guint32 il_offset)
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

	info = _mono_debug_get_image (mono_debug_handle, klass->image);
	g_assert (info);

	if (mono_debug_handle->format != MONO_DEBUG_FORMAT_MONO)
		return;

	if (info->symfile) {
		mono_debug_lock ();
		mono_debug_symfile_add_type (info->symfile, klass);
		mono_debugger_event (MONO_DEBUGGER_EVENT_TYPE_ADDED, info->symfile, klass);
		mono_debug_unlock ();
	}
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

MonoDebugMethodInfo *
_mono_debug_lookup_method (MonoMethod *method)
{
	struct LookupMethodData data = { NULL, method };

	if (!mono_debug_handle)
		return NULL;

	g_hash_table_foreach (mono_debug_handle->images, lookup_method_func, &data);
	return data.minfo;
}

void
mono_debug_add_wrapper (MonoMethod *method, MonoMethod *wrapper_method)
{
	MonoClass *klass = method->klass;
	AssemblyDebugInfo* info;
	MonoDebugMethodInfo *minfo;
	DebugWrapperInfo *winfo;
	MonoDebugMethodJitInfo *jit;

	if (!mono_debug_handle)
		return;

	if (!(method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL))
		return;

	mono_class_init (klass);

	info = _mono_debug_get_image (mono_debug_handle, klass->image);
	g_assert (info);

	minfo = _mono_debug_lookup_method (method);
	if (!minfo || minfo->jit)
		return;

	winfo = g_hash_table_lookup (info->wrapper_methods, wrapper_method);
	g_assert (winfo);

	mono_debug_lock ();

	mono_debug_handle->dirty = TRUE;

	minfo->jit = jit = g_new0 (MonoDebugMethodJitInfo, 1);
	jit->code_start = winfo->code_start;
	jit->code_size = winfo->code_size;
	jit->prologue_end = 0;
	jit->epilogue_begin = winfo->code_size;
	jit->num_params = 0;
	jit->wrapper_addr = method->addr;

	if (info->symfile) {
		mono_debug_symfile_add_method (info->symfile, method);
		mono_debugger_event (MONO_DEBUGGER_EVENT_METHOD_ADDED, info->symfile, method);
	}

	mono_debug_unlock ();
}

gchar *
mono_debug_source_location_from_address (MonoMethod *method, guint32 address, guint32 *line_number)
{
	MonoDebugMethodInfo *minfo = _mono_debug_lookup_method (method);

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

	minfo = _mono_debug_lookup_method (method);
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

	minfo = _mono_debug_lookup_method (method);
	if (!minfo || !minfo->il_offsets)
		return -1;

	return _mono_debug_address_from_il_offset (minfo, il_offset);
}

static void
release_symbol_file_table ()
{
	MonoDebuggerSymbolFileTable *temp;

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

static void
update_symbol_file_table_count_func (gpointer key, gpointer value, gpointer user_data)
{
	AssemblyDebugInfo *info = (AssemblyDebugInfo *) value;

	if (!info->symfile)
		return;
	if (info->format != MONO_DEBUG_FORMAT_MONO)
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
	if (info->format != MONO_DEBUG_FORMAT_MONO)
		return;

	data->symfile_table->symfiles [data->index++] = info->symfile;
}

int
mono_debug_update_symbol_file_table (void)
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
	symfile_table->generation = mono_debugger_symbol_file_table_generation;
	symfile_table->global_symfile = mono_debugger_global_symbol_file;

	data.symfile_table = symfile_table;
	data.index = 0;

	g_hash_table_foreach (mono_debug_handle->images, update_symbol_file_table_func, &data);

	mono_debugger_symbol_file_table = symfile_table;

	mono_debug_unlock ();

	return TRUE;
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

int
mono_remove_breakpoint (int breakpoint_id)
{
	int i;

	if (!breakpoints)
		return 0;

	for (i = 0; i < breakpoints->len; i++) {
		MonoDebuggerBreakpointInfo *info = g_ptr_array_index (breakpoints, i);

		if (info->index != breakpoint_id)
			continue;

		mono_method_desc_free (info->desc);
		g_ptr_array_remove (breakpoints, info);
		g_free (info);
		return 1;
	}

	return 0;
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
	mono_debugger_event (MONO_DEBUGGER_EVENT_BREAKPOINT_TRAMPOLINE, NULL, NULL);
}
