#include <config.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/mono-debug.h>
#include <mono/metadata/mono-debug-debugger.h>

struct _MonoDebugHandlePriv
{
	GHashTable *wrapper_info;
	MonoDebuggerSymbolFile *debugger_info;
};

MonoDebugFormat mono_debug_format;

static gboolean in_the_mono_debugger = FALSE;
static gboolean mono_debug_initialized = FALSE;
GHashTable *mono_debug_handles = NULL;

static MonoDebugHandle *mono_debug_open_image    (MonoImage *image);
static void             mono_debug_close_image   (MonoDebugHandle *debug);

static MonoDebugHandle *_mono_debug_get_image    (MonoImage *image);
static void             mono_debug_add_assembly  (MonoAssembly *assembly, gpointer user_data);
static void             mono_debug_add_type      (MonoClass *klass);

extern void (*mono_debugger_class_init_func) (MonoClass *klass);

/*
 * Initialize debugging support.
 *
 * This method must be called after loading corlib,
 * but before opening the application's main assembly because we need to set some
 * callbacks here.
 */
void
mono_debug_init (MonoDebugFormat format)
{
	MonoAssembly **ass;

	g_assert (!mono_debug_initialized);

	mono_debug_initialized = TRUE;
	mono_debug_format = format;
	in_the_mono_debugger = format == MONO_DEBUG_FORMAT_DEBUGGER;

	if (in_the_mono_debugger)
		mono_debugger_initialize ();

	mono_debugger_lock ();

	mono_debug_handles = g_hash_table_new_full
		(NULL, NULL, NULL, (GDestroyNotify) mono_debug_close_image);

	mono_debugger_class_init_func = mono_debug_add_type;
	mono_install_assembly_load_hook (mono_debug_add_assembly, NULL);

	mono_debug_open_image (mono_defaults.corlib);
	for (ass = mono_defaults.corlib->references; ass && *ass; ass++)
		mono_debug_open_image ((*ass)->image);
}

/*
 * Initialize debugging support - part 2.
 *
 * This method must be called after loading the application's main assembly.
 */
void
mono_debug_init_2 (MonoAssembly *assembly)
{
	mono_debug_open_image (assembly->image);

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

	mono_debugger_unlock ();
}

void
mono_debug_cleanup (void)
{
	mono_debugger_cleanup ();

	if (mono_debug_handles)
		g_hash_table_destroy (mono_debug_handles);
	mono_debug_handles = NULL;
}

static MonoDebugHandle *
_mono_debug_get_image (MonoImage *image)
{
	return g_hash_table_lookup (mono_debug_handles, image);
}

static MonoDebugHandle *
mono_debug_open_image (MonoImage *image)
{
	MonoDebugHandle *handle;

	handle = _mono_debug_get_image (image);
	if (handle != NULL)
		return handle;

	handle = g_new0 (MonoDebugHandle, 1);
	handle->image = image;
	handle->image->ref_count++;
	handle->_priv = g_new0 (MonoDebugHandlePriv, 1);
	handle->_priv->wrapper_info = g_hash_table_new (g_direct_hash, g_direct_equal);

	g_hash_table_insert (mono_debug_handles, image, handle);

	if (image->assembly->dynamic)
		return handle;

	handle->symfile = mono_debug_open_mono_symbol_file (handle->image, in_the_mono_debugger);
	if (in_the_mono_debugger)
		handle->_priv->debugger_info = mono_debugger_add_symbol_file (handle->symfile);

	return handle;
}

static void
mono_debug_close_image (MonoDebugHandle *handle)
{
	if (handle->symfile)
		mono_debug_close_mono_symbol_file (handle->symfile);
	g_hash_table_destroy (handle->_priv->wrapper_info);
	handle->image->ref_count--;
	g_free (handle->_priv);
	g_free (handle);
}

static void
mono_debug_add_assembly (MonoAssembly *assembly, gpointer user_data)
{
	mono_debugger_lock ();
	mono_debug_open_image (assembly->image);
	mono_debugger_unlock ();
}

/*
 * This is called via the `mono_debugger_class_init_func' from mono_class_init() each time
 * a new class is initialized.
 */
static void
mono_debug_add_type (MonoClass *klass)
{
	MonoDebugHandle *handle;

	handle = _mono_debug_get_image (klass->image);
	g_assert (handle);

	if (handle->_priv->debugger_info)
		mono_debugger_add_type (handle->_priv->debugger_info, klass);
}

struct LookupMethodData
{
	MonoDebugMethodInfo *minfo;
	MonoMethod *method;
};

static void
lookup_method_func (gpointer key, gpointer value, gpointer user_data)
{
	MonoDebugHandle *handle = (MonoDebugHandle *) value;
	struct LookupMethodData *data = (struct LookupMethodData *) user_data;

	if (data->minfo)
		return;

	if (handle->symfile)
		data->minfo = mono_debug_find_method (handle->symfile, data->method);
}

static MonoDebugMethodInfo *
_mono_debug_lookup_method (MonoMethod *method)
{
	struct LookupMethodData data = { NULL, method };

	if (!mono_debug_handles)
		return NULL;

	g_hash_table_foreach (mono_debug_handles, lookup_method_func, &data);
	return data.minfo;
}

/*
 * This is called by the JIT to tell the debugging code about a newly compiled
 * wrapper method.
 */
void
mono_debug_add_wrapper (MonoMethod *method, MonoMethod *wrapper_method)
{
	MonoClass *klass = method->klass;
	MonoDebugHandle *handle;
	MonoDebugMethodInfo *minfo;
	MonoDebugMethodJitInfo *jit;

	if (!(method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL))
		return;

	mono_class_init (klass);

	handle = _mono_debug_get_image (klass->image);
	g_assert (handle);

	minfo = _mono_debug_lookup_method (method);
	if (!minfo || minfo->jit)
		return;

	jit = g_hash_table_lookup (handle->_priv->wrapper_info, wrapper_method);
	g_assert (jit);

	mono_debugger_lock ();

	minfo->jit = jit;
	minfo->jit->wrapper_addr = method->addr;

	if (handle->_priv->debugger_info)
		mono_debugger_add_method (handle->_priv->debugger_info, method);

	mono_debugger_unlock ();
}

/*
 * This is called by the JIT to tell the debugging code about a newly
 * compiled method.
 */
void
mono_debug_add_method (MonoMethod *method, MonoDebugMethodJitInfo *jit)
{
	MonoClass *klass = method->klass;
	MonoDebugHandle *handle;
	MonoDebugMethodInfo *minfo;

	mono_class_init (klass);

	if ((method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) ||
	    (method->iflags & METHOD_IMPL_ATTRIBUTE_RUNTIME) ||
	    (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) ||
	    (method->flags & METHOD_ATTRIBUTE_ABSTRACT))
		return;

	handle = _mono_debug_get_image (klass->image);
	g_assert (handle);

	if (method->wrapper_type != MONO_WRAPPER_NONE) {
		g_hash_table_insert (handle->_priv->wrapper_info, method, jit);
		return;
	}

	minfo = _mono_debug_lookup_method (method);
	if (!minfo)
		return;

	mono_debugger_lock ();

	g_assert (!minfo->jit);
	minfo->jit = jit;

	if (handle->_priv->debugger_info)
		mono_debugger_add_method (handle->_priv->debugger_info, method);

	mono_debugger_unlock ();
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

/*
 * Used by the exception code to get a source location from a machine address.
 *
 * Returns a textual representation of the specified address which is suitable to be displayed to
 * the user (for instance "/home/martin/monocvs/debugger/test/Y.cs:8").
 *
 * If the optional @line_number argument is not NULL, the line number is stored there and just the
 * source file is returned (ie. it'd return "/home/martin/monocvs/debugger/test/Y.cs" and store the
 * line number 8 in the variable pointed to by @line_number).
 */
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

/*
 * Used by the exception code to get a source location from an IL offset.
 *
 * Returns a textual representation of the specified address which is suitable to be displayed to
 * the user (for instance "/home/martin/monocvs/debugger/test/Y.cs:8").
 *
 * If the optional @line_number argument is not NULL, the line number is stored there and just the
 * source file is returned (ie. it'd return "/home/martin/monocvs/debugger/test/Y.cs" and store the
 * line number 8 in the variable pointed to by @line_number).
 */
gchar *
mono_debug_source_location_from_il_offset (MonoMethod *method, guint32 offset, guint32 *line_number)
{
	MonoDebugMethodInfo *minfo = _mono_debug_lookup_method (method);

	if (!minfo || !minfo->symfile)
		return NULL;

	return mono_debug_find_source_location (minfo->symfile, method, offset, line_number);
}

/*
 * Returns the IL offset corresponding to machine address @address which is an offset
 * relative to the beginning of the method @method.
 */
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

/*
 * Returns the machine address corresponding to IL offset @il_offset.
 * The returned value is an offset relative to the beginning of the method @method.
 */
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
