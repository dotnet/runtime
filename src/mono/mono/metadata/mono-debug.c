#include <config.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/mono-debug.h>
#include <mono/metadata/mono-debug-debugger.h>

struct _MonoDebugHandlePriv
{
	MonoDebuggerSymbolFile *debugger_info;
	MonoDebugDomainData *domain_table;
};

struct _MonoDebugDomainDataPriv
{
	GHashTable *wrapper_info;
	MonoDebugDomainData *next;
};

MonoDebugFormat mono_debug_format = MONO_DEBUG_FORMAT_NONE;

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
mono_debug_init (MonoDomain *domain, MonoDebugFormat format)
{
	MonoAssembly **ass;

	g_assert (!mono_debug_initialized);

	mono_debug_initialized = TRUE;
	mono_debug_format = format;
	in_the_mono_debugger = format == MONO_DEBUG_FORMAT_DEBUGGER;

	if (in_the_mono_debugger)
		mono_debugger_initialize (domain);

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
	MonoDebugHandle *handle;

	mono_debug_open_image (assembly->image);

	handle = _mono_debug_get_image (mono_defaults.corlib);
	g_assert (handle);

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
	handle->image_file = g_strdup (image->name);
	handle->_priv = g_new0 (MonoDebugHandlePriv, 1);

	g_hash_table_insert (mono_debug_handles, image, handle);

	if (image->assembly->dynamic)
		return handle;

	handle->symfile = mono_debug_open_mono_symbol_file (handle, in_the_mono_debugger);
	if (in_the_mono_debugger) {
		handle->_priv->debugger_info = mono_debugger_add_symbol_file (handle);
		if (image == mono_defaults.corlib)
			mono_debugger_add_builtin_types (handle->_priv->debugger_info);
	}

	return handle;
}

static void
mono_debug_close_image (MonoDebugHandle *handle)
{
	if (handle->symfile)
		mono_debug_close_mono_symbol_file (handle->symfile);
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
	if (!handle)
		return;

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
	struct LookupMethodData data;

	data.minfo = NULL;
	data.method = method;

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
mono_debug_add_wrapper (MonoMethod *method, MonoMethod *wrapper_method, MonoDomain *domain)
{
	MonoClass *klass = method->klass;
	MonoDebugHandle *handle;
	MonoDebugMethodInfo *minfo;
	MonoDebugMethodJitInfo *jit;
	MonoDebugDomainData *domain_data;

	if (!(method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL))
		return;

	mono_class_init (klass);

	handle = _mono_debug_get_image (klass->image);
	g_assert (handle);

	minfo = _mono_debug_lookup_method (method);
	if (!minfo)
		return;

	domain_data = mono_debug_get_domain_data (handle, domain);
	if (domain_data->jit [minfo->index]) {
		// FIXME FIXME FIXME
		// This is bug #48591.
		return;
	}

	jit = g_hash_table_lookup (domain_data->_priv->wrapper_info, wrapper_method);
	g_assert (jit);

	mono_debugger_lock ();

	domain_data->jit [minfo->index] = jit;
	jit->wrapper_addr = method->addr;

	if (handle->_priv->debugger_info && (domain == mono_root_domain))
		mono_debugger_add_method (handle->_priv->debugger_info, minfo, jit);

	mono_debugger_unlock ();
}

/*
 * This is called by the JIT to tell the debugging code about a newly
 * compiled method.
 */
void
mono_debug_add_method (MonoMethod *method, MonoDebugMethodJitInfo *jit, MonoDomain *domain)
{
	MonoClass *klass = method->klass;
	MonoDebugDomainData *domain_data;
	MonoDebugHandle *handle;
	MonoDebugMethodInfo *minfo;

	mono_class_init (klass);

	if ((method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) ||
	    (method->iflags & METHOD_IMPL_ATTRIBUTE_RUNTIME) ||
	    (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) ||
	    (method->flags & METHOD_ATTRIBUTE_ABSTRACT))
		return;

	handle = _mono_debug_get_image (klass->image);
	if (!handle)
		return;

	minfo = _mono_debug_lookup_method (method);
	if (!minfo)
		return;

	mono_debugger_lock ();

	domain_data = mono_debug_get_domain_data (handle, domain);
	if (domain_data->jit [minfo->index]) {
		// FIXME FIXME FIXME
		// This is bug #48591.
		return;
	}

	if (method->wrapper_type != MONO_WRAPPER_NONE) {
		g_hash_table_insert (domain_data->_priv->wrapper_info, method, jit);
		mono_debugger_unlock ();
		return;
	}

	domain_data->jit [minfo->index] = jit;

	if (handle->_priv->debugger_info && (domain == mono_root_domain))
		mono_debugger_add_method (handle->_priv->debugger_info, minfo, jit);

	mono_debugger_unlock ();
}

static gint32
il_offset_from_address (MonoDebugMethodJitInfo *jit, guint32 address)
{
	int i;

	if (!jit || !jit->line_numbers)
		return -1;

	for (i = jit->line_numbers->len - 1; i >= 0; i--) {
		MonoDebugLineNumberEntry lne = g_array_index (
			jit->line_numbers, MonoDebugLineNumberEntry, i);

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
mono_debug_source_location_from_address (MonoMethod *method, guint32 address, guint32 *line_number,
					 MonoDomain *domain)
{
	MonoDebugMethodInfo *minfo = _mono_debug_lookup_method (method);
	MonoDebugDomainData *domain_data;

	if (!minfo)
		return NULL;

	domain_data = mono_debug_get_domain_data (minfo->handle, domain);
	if (!domain_data->jit [minfo->index])
		return NULL;

	if (minfo->handle) {
		gint32 offset = il_offset_from_address (domain_data->jit [minfo->index], address);
		
		if (offset < 0)
			return NULL;

		return mono_debug_find_source_location (minfo->handle->symfile, method, offset, line_number);
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

	if (!minfo || !minfo->handle)
		return NULL;

	return mono_debug_find_source_location (minfo->handle->symfile, method, offset, line_number);
}

/*
 * Returns the IL offset corresponding to machine address @address which is an offset
 * relative to the beginning of the method @method.
 */
gint32
mono_debug_il_offset_from_address (MonoMethod *method, gint32 address, MonoDomain *domain)
{
	MonoDebugMethodInfo *minfo;
	MonoDebugDomainData *domain_data;

	if (address < 0)
		return -1;

	minfo = _mono_debug_lookup_method (method);
	if (!minfo || !minfo->il_offsets)
		return -1;

	domain_data = mono_debug_get_domain_data (minfo->handle, domain);

	return il_offset_from_address (domain_data->jit [minfo->index], address);
}

/*
 * Returns the machine address corresponding to IL offset @il_offset.
 * The returned value is an offset relative to the beginning of the method @method.
 */
gint32
mono_debug_address_from_il_offset (MonoMethod *method, gint32 il_offset, MonoDomain *domain)
{
	MonoDebugMethodInfo *minfo;
	MonoDebugDomainData *domain_data;

	if (il_offset < 0)
		return -1;

	minfo = _mono_debug_lookup_method (method);
	if (!minfo || !minfo->il_offsets)
		return -1;

	domain_data = mono_debug_get_domain_data (minfo->handle, domain);

	return _mono_debug_address_from_il_offset (domain_data->jit [minfo->index], il_offset);
}

MonoDebugDomainData *
mono_debug_get_domain_data (MonoDebugHandle *handle, MonoDomain *domain)
{
	MonoDebugDomainData *data;

	for (data = handle->_priv->domain_table; data; data = data->_priv->next)
		if (data->domain_id == domain->domain_id)
			return data;

	data = g_new0 (MonoDebugDomainData, 1);
	data->domain_id = domain->domain_id;
	data->jit = g_new0 (MonoDebugMethodJitInfo *, handle->symfile->offset_table->method_count + 1);

	data->_priv = g_new0 (MonoDebugDomainDataPriv, 1);
	data->_priv->next = handle->_priv->domain_table;
	data->_priv->wrapper_info = g_hash_table_new (g_direct_hash, g_direct_equal);
	handle->_priv->domain_table = data;

	return data;
}
