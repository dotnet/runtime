/**
 * \file
 *
 * Author:
 *	Mono Project (http://www.mono-project.com)
 *
 * Copyright 2001-2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
 * Copyright 2011 Xamarin Inc (http://www.xamarin.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/assembly-internals.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/mono-debug.h>
#include <mono/metadata/debug-internals.h>
#include <mono/metadata/mono-endian.h>
#include <mono/metadata/gc-internals.h>
#include <mono/metadata/mempool.h>
#include <mono/metadata/debug-mono-symfile.h>
#include <mono/metadata/debug-mono-ppdb.h>
#include <mono/metadata/exception-internals.h>
#include <mono/metadata/runtime.h>
#include <string.h>

#if NO_UNALIGNED_ACCESS
#define WRITE_UNALIGNED(type, addr, val) \
	memcpy(addr, &val, sizeof(type))
#define READ_UNALIGNED(type, addr, val) \
	memcpy(&val, addr, sizeof(type))
#else
#define WRITE_UNALIGNED(type, addr, val) \
	(*(type *)(addr) = (val))
#define READ_UNALIGNED(type, addr, val) \
	val = (*(type *)(addr))
#endif

/* Many functions have 'domain' parameters, those are ignored */

/*
 * This contains per-memory manager info.
 */
typedef struct {
	MonoMemPool *mp;
	/* Maps MonoMethod->MonoDebugMethodAddress */
	GHashTable *method_hash;
} DebugMemoryManager;

/* This contains JIT debugging information about a method in serialized format */
struct _MonoDebugMethodAddress {
	const guint8 *code_start;
	guint32 code_size;
	guint8 data [MONO_ZERO_LEN_ARRAY];
};

static MonoDebugFormat mono_debug_format = MONO_DEBUG_FORMAT_NONE;

static gboolean mono_debug_initialized = FALSE;
/* Maps MonoImage -> MonoMonoDebugHandle */
static GHashTable *mono_debug_handles;

static mono_mutex_t debugger_lock_mutex;

static gboolean is_attached = FALSE;

static MonoDebugHandle     *mono_debug_open_image      (MonoImage *image, const guint8 *raw_contents, int size);

static MonoDebugHandle     *mono_debug_get_image      (MonoImage *image);
static void                 add_assembly    (MonoAssemblyLoadContext *alc, MonoAssembly *assembly, gpointer user_data, MonoError *error);

static MonoDebugHandle     *open_symfile_from_bundle   (MonoImage *image);

static DebugMemoryManager*
get_mem_manager (MonoMethod *method)
{
	MonoMemoryManager *mem_manager = m_method_get_mem_manager (method);

	if (!mono_debug_initialized)
		return NULL;

	if (!mem_manager->debug_info) {
		DebugMemoryManager *info;

		info = g_new0 (DebugMemoryManager, 1);
		info->mp = mono_mempool_new ();
		info->method_hash = g_hash_table_new (NULL, NULL);
		mono_memory_barrier ();

		mono_debugger_lock ();
		if (!mem_manager->debug_info)
			mem_manager->debug_info = info;
		// FIXME: Free otherwise
		mono_debugger_unlock ();
	}

	return (DebugMemoryManager*)mem_manager->debug_info;
}

/*
 * mono_mem_manager_free_debug_info:
 *
 *   Free the information maintained by this module for MEM_MANAGER.
 */
void
mono_mem_manager_free_debug_info (MonoMemoryManager *memory_manager)
{
	DebugMemoryManager *info = (DebugMemoryManager*)memory_manager->debug_info;
	if (!info)
		return;

	mono_mempool_destroy (info->mp);
	g_hash_table_destroy (info->method_hash);

	g_free (info);
}

static void
free_debug_handle (MonoDebugHandle *handle)
{
	if (handle->ppdb)
		mono_ppdb_close (handle);
	if (handle->symfile)
		mono_debug_close_mono_symbol_file (handle->symfile);
	/* decrease the refcount added with mono_image_addref () */
	mono_image_close (handle->image);
	g_free (handle);
}

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
	g_assert (!mono_debug_initialized);
	if (format == MONO_DEBUG_FORMAT_DEBUGGER)
		g_error ("The mdb debugger is no longer supported.");

	mono_debug_initialized = TRUE;
	mono_debug_format = format;

	mono_os_mutex_init_recursive (&debugger_lock_mutex);

	mono_debugger_lock ();

	mono_debug_handles = g_hash_table_new_full
		(NULL, NULL, NULL, (GDestroyNotify) free_debug_handle);

	mono_install_assembly_load_hook_v2 (add_assembly, NULL, FALSE);

	mono_debugger_unlock ();
}

void
mono_debug_open_image_from_memory (MonoImage *image, const guint8 *raw_contents, int size)
{
	MONO_ENTER_GC_UNSAFE;
	if (!mono_debug_initialized)
		goto leave;

	mono_debug_open_image (image, raw_contents, size);
leave:
	MONO_EXIT_GC_UNSAFE;
}

void
mono_debug_cleanup (void)
{
	if (mono_debug_handles)
		g_hash_table_destroy (mono_debug_handles);
	mono_debug_handles = NULL;
}

/**
 * mono_debug_domain_create:
 */
void
mono_debug_domain_create (MonoDomain *domain)
{
	g_assert_not_reached ();
}

void
mono_debug_domain_unload (MonoDomain *domain)
{
	g_assert_not_reached ();
}

/*
 * LOCKING: Assumes the debug lock is held.
 */
static MonoDebugHandle *
mono_debug_get_image (MonoImage *image)
{
	return (MonoDebugHandle *)g_hash_table_lookup (mono_debug_handles, image);
}

/**
 * mono_debug_close_image:
 */
void
mono_debug_close_image (MonoImage *image)
{
	MonoDebugHandle *handle;

	if (!mono_debug_initialized)
		return;

	mono_debugger_lock ();

	handle = mono_debug_get_image (image);
	if (!handle) {
		mono_debugger_unlock ();
		return;
	}

	g_hash_table_remove (mono_debug_handles, image);

	mono_debugger_unlock ();
}

MonoDebugHandle *
mono_debug_get_handle (MonoImage *image)
{
    return mono_debug_open_image (image, NULL, 0);
}

static MonoDebugHandle *
mono_debug_open_image (MonoImage *image, const guint8 *raw_contents, int size)
{
	MonoDebugHandle *handle;

	if (mono_image_is_dynamic (image))
		return NULL;

	mono_debugger_lock ();

	handle = mono_debug_get_image (image);
	if (handle != NULL) {
		mono_debugger_unlock ();
		return handle;
	}

	handle = g_new0 (MonoDebugHandle, 1);

	handle->image = image;
	mono_image_addref (image);

	/* Try a ppdb file first */
	handle->ppdb = mono_ppdb_load_file (handle->image, raw_contents, size);

	if (!handle->ppdb)
		handle->symfile = mono_debug_open_mono_symbols (handle, raw_contents, size, FALSE);

	g_hash_table_insert (mono_debug_handles, image, handle);

	mono_debugger_unlock ();

	return handle;
}

static void
add_assembly (MonoAssemblyLoadContext *alc, MonoAssembly *assembly, gpointer user_data, MonoError *error)
{
	MonoDebugHandle *handle;
	MonoImage *image;

	mono_debugger_lock ();
	image = mono_assembly_get_image_internal (assembly);
	handle = open_symfile_from_bundle (image);
	if (!handle)
		mono_debug_open_image (image, NULL, 0);
	mono_debugger_unlock ();
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

	if (handle->ppdb)
		data->minfo = mono_ppdb_lookup_method (handle, data->method);
	else if (handle->symfile)
		data->minfo = mono_debug_symfile_lookup_method (handle, data->method);
}

static MonoDebugMethodInfo *
lookup_method (MonoMethod *method)
{
	struct LookupMethodData data;

	data.minfo = NULL;
	data.method = method;

	if (!mono_debug_handles)
		return NULL;

	g_hash_table_foreach (mono_debug_handles, lookup_method_func, &data);
	return data.minfo;
}

/**
 * mono_debug_lookup_method:
 *
 * Lookup symbol file information for the method \p method.  The returned
 * \c MonoDebugMethodInfo is a private structure, but it can be passed to
 * \c mono_debug_symfile_lookup_location.
 */
MonoDebugMethodInfo *
mono_debug_lookup_method (MonoMethod *method)
{
	MonoDebugMethodInfo *minfo;

	if (mono_debug_format == MONO_DEBUG_FORMAT_NONE)
		return NULL;

	mono_debugger_lock ();
	minfo = lookup_method (method);
	mono_debugger_unlock ();
	return minfo;
}

typedef struct
{
	gboolean found;
	MonoImage *image;
} LookupImageData;

static void
lookup_image_func (gpointer key, gpointer value, gpointer user_data)
{
	MonoDebugHandle *handle = (MonoDebugHandle *) value;
	LookupImageData *data = (LookupImageData *) user_data;

	if (data->found)
		return;

	if (handle->image == data->image && (handle->symfile || handle->ppdb))
		data->found = TRUE;
}

gboolean
mono_debug_image_has_debug_info (MonoImage *image)
{
	LookupImageData data;

	if (!mono_debug_handles)
		return FALSE;

	memset (&data, 0, sizeof (data));
	data.image = image;

	mono_debugger_lock ();
	g_hash_table_foreach (mono_debug_handles, lookup_image_func, &data);
	mono_debugger_unlock ();
	return data.found;
}

static void
write_leb128 (guint32 value, guint8 *ptr, guint8 **rptr)
{
	do {
		guint8 byte = value & 0x7f;
		value >>= 7;
		if (value)
			byte |= 0x80;
		*ptr++ = byte;
	} while (value);

	*rptr = ptr;
}

static void
write_sleb128 (gint32 value, guint8 *ptr, guint8 **rptr)
{
	gboolean more = 1;

	while (more) {
		guint8 byte = value & 0x7f;
		value >>= 7;

		if (((value == 0) && ((byte & 0x40) == 0)) || ((value == -1) && (byte & 0x40)))
			more = 0;
		else
			byte |= 0x80;
		*ptr++ = byte;
	}

	*rptr = ptr;
}

/* leb128 uses a maximum of 5 bytes for a 32 bit value */
#define LEB128_MAX_SIZE 5
#define WRITE_VARIABLE_MAX_SIZE (5 * LEB128_MAX_SIZE + sizeof (gpointer))

static void
write_variable (MonoDebugVarInfo *var, guint8 *ptr, guint8 **rptr)
{
	write_leb128 (var->index, ptr, &ptr);
	write_sleb128 (var->offset, ptr, &ptr);
	write_leb128 (var->size, ptr, &ptr);
	write_leb128 (var->begin_scope, ptr, &ptr);
	write_leb128 (var->end_scope, ptr, &ptr);
	WRITE_UNALIGNED (gpointer, ptr, var->type);
	ptr += sizeof (gpointer);
	*rptr = ptr;
}

/**
 * mono_debug_add_method:
 */
MonoDebugMethodAddress *
mono_debug_add_method (MonoMethod *method, MonoDebugMethodJitInfo *jit, MonoDomain *domain)
{
	DebugMemoryManager *info;
	MonoDebugMethodAddress *address;
	guint8 buffer [BUFSIZ];
	guint8 *ptr, *oldptr;
	guint32 i, size, total_size, max_size;

	info = get_mem_manager (method);

	max_size = (5 * LEB128_MAX_SIZE) + 1 + (2 * LEB128_MAX_SIZE * jit->num_line_numbers);
	if (jit->has_var_info) {
		/* this */
		max_size += 1;
		if (jit->this_var)
			max_size += WRITE_VARIABLE_MAX_SIZE;
		/* params */
		max_size += LEB128_MAX_SIZE;
		max_size += jit->num_params * WRITE_VARIABLE_MAX_SIZE;
		/* locals */
		max_size += LEB128_MAX_SIZE;
		max_size += jit->num_locals * WRITE_VARIABLE_MAX_SIZE;
		/* gsharedvt */
		max_size += 1;
		if (jit->gsharedvt_info_var)
			max_size += 2 * WRITE_VARIABLE_MAX_SIZE;
	}

	if (max_size > BUFSIZ)
		ptr = oldptr = (guint8 *)g_malloc (max_size);
	else
		ptr = oldptr = buffer;

	write_leb128 (jit->prologue_end, ptr, &ptr);
	write_leb128 (jit->epilogue_begin, ptr, &ptr);

	write_leb128 (jit->num_line_numbers, ptr, &ptr);
	for (i = 0; i < jit->num_line_numbers; i++) {
		MonoDebugLineNumberEntry *lne = &jit->line_numbers [i];

		write_sleb128 (lne->il_offset, ptr, &ptr);
		write_sleb128 (lne->native_offset, ptr, &ptr);
	}
	write_leb128 (jit->has_var_info, ptr, &ptr);
	if (jit->has_var_info) {
		*ptr++ = jit->this_var ? 1 : 0;
		if (jit->this_var)
			write_variable (jit->this_var, ptr, &ptr);

		write_leb128 (jit->num_params, ptr, &ptr);
		for (i = 0; i < jit->num_params; i++)
			write_variable (&jit->params [i], ptr, &ptr);

		write_leb128 (jit->num_locals, ptr, &ptr);
		for (i = 0; i < jit->num_locals; i++)
			write_variable (&jit->locals [i], ptr, &ptr);

		*ptr++ = jit->gsharedvt_info_var ? 1 : 0;
		if (jit->gsharedvt_info_var) {
			write_variable (jit->gsharedvt_info_var, ptr, &ptr);
			write_variable (jit->gsharedvt_locals_var, ptr, &ptr);
		}
	}

	size = ptr - oldptr;
	g_assert (size < max_size);
	total_size = size + sizeof (MonoDebugMethodAddress);

	mono_debugger_lock ();

	if (method_is_dynamic (method)) {
		address = (MonoDebugMethodAddress *)g_malloc0 (total_size);
	} else {
		address = (MonoDebugMethodAddress *)mono_mempool_alloc (info->mp, total_size);
	}

	address->code_start = jit->code_start;
	address->code_size = jit->code_size;

	memcpy (&address->data, oldptr, size);
	if (max_size > BUFSIZ)
		g_free (oldptr);

	g_hash_table_insert (info->method_hash, method, address);

	mono_debugger_unlock ();
	return address;
}

void
mono_debug_remove_method (MonoMethod *method, MonoDomain *domain)
{
	DebugMemoryManager *info;
	MonoDebugMethodAddress *address;

	if (!mono_debug_initialized)
		return;

	g_assert (method_is_dynamic (method));

	info = get_mem_manager (method);

	mono_debugger_lock ();

	address = (MonoDebugMethodAddress *)g_hash_table_lookup (info->method_hash, method);
	if (address)
		g_free (address);

	g_hash_table_remove (info->method_hash, method);

	mono_debugger_unlock ();
}

/**
 * mono_debug_add_delegate_trampoline:
 */
void
mono_debug_add_delegate_trampoline (gpointer code, int size)
{
}

static guint32
read_leb128 (guint8 *ptr, guint8 **rptr)
{
	guint32 result = 0, shift = 0;

	while (TRUE) {
		guint8 byte = *ptr++;

		result |= (byte & 0x7f) << shift;
		if ((byte & 0x80) == 0)
			break;
		shift += 7;
	}

	*rptr = ptr;
	return result;
}

static gint32
read_sleb128 (guint8 *ptr, guint8 **rptr)
{
	gint32 result = 0;
	guint32 shift = 0;

	while (TRUE) {
		guint8 byte = *ptr++;

		result |= (byte & 0x7f) << shift;
		shift += 7;

		if (byte & 0x80)
			continue;

		if ((shift < 32) && (byte & 0x40))
			result |= - (1 << shift);
		break;
	}

	*rptr = ptr;
	return result;
}

static void
read_variable (MonoDebugVarInfo *var, guint8 *ptr, guint8 **rptr)
{
	var->index = read_leb128 (ptr, &ptr);
	var->offset = read_sleb128 (ptr, &ptr);
	var->size = read_leb128 (ptr, &ptr);
	var->begin_scope = read_leb128 (ptr, &ptr);
	var->end_scope = read_leb128 (ptr, &ptr);
	READ_UNALIGNED (MonoType *, ptr, var->type);
	ptr += sizeof (gpointer);
	*rptr = ptr;
}

static void
free_method_jit_info (MonoDebugMethodJitInfo *jit, gboolean stack)
{
	if (!jit)
		return;
	g_free (jit->line_numbers);
	g_free (jit->this_var);
	g_free (jit->params);
	g_free (jit->locals);
	g_free (jit->gsharedvt_info_var);
	g_free (jit->gsharedvt_locals_var);
	if (!stack)
		g_free (jit);
}

void
mono_debug_free_method_jit_info (MonoDebugMethodJitInfo *jit)
{
	free_method_jit_info (jit, FALSE);
}

static MonoDebugMethodJitInfo *
mono_debug_read_method (MonoDebugMethodAddress *address, MonoDebugMethodJitInfo *jit)
{
	guint32 i;
	guint8 *ptr;

	memset (jit, 0, sizeof (*jit));
	jit->code_start = address->code_start;
	jit->code_size = address->code_size;

	ptr = (guint8 *) &address->data;

	jit->prologue_end = read_leb128 (ptr, &ptr);
	jit->epilogue_begin = read_leb128 (ptr, &ptr);

	jit->num_line_numbers = read_leb128 (ptr, &ptr);
	jit->line_numbers = g_new0 (MonoDebugLineNumberEntry, jit->num_line_numbers);
	for (i = 0; i < jit->num_line_numbers; i++) {
		MonoDebugLineNumberEntry *lne = &jit->line_numbers [i];

		lne->il_offset = read_sleb128 (ptr, &ptr);
		lne->native_offset = read_sleb128 (ptr, &ptr);
	}
	jit->has_var_info = read_leb128 (ptr, &ptr);
	if (jit->has_var_info) {
		if (*ptr++) {
			jit->this_var = g_new0 (MonoDebugVarInfo, 1);
			read_variable (jit->this_var, ptr, &ptr);
		}

		jit->num_params = read_leb128 (ptr, &ptr);
		jit->params = g_new0 (MonoDebugVarInfo, jit->num_params);
		for (i = 0; i < jit->num_params; i++)
			read_variable (&jit->params [i], ptr, &ptr);

		jit->num_locals = read_leb128 (ptr, &ptr);
		jit->locals = g_new0 (MonoDebugVarInfo, jit->num_locals);
		for (i = 0; i < jit->num_locals; i++)
			read_variable (&jit->locals [i], ptr, &ptr);

		if (*ptr++) {
			jit->gsharedvt_info_var = g_new0 (MonoDebugVarInfo, 1);
			jit->gsharedvt_locals_var = g_new0 (MonoDebugVarInfo, 1);
			read_variable (jit->gsharedvt_info_var, ptr, &ptr);
			read_variable (jit->gsharedvt_locals_var, ptr, &ptr);
		}
	}

	return jit;
}

static MonoDebugMethodJitInfo *
find_method (MonoMethod *method, MonoDebugMethodJitInfo *jit)
{
	DebugMemoryManager *info;
	MonoDebugMethodAddress *address;

	info = get_mem_manager (method);
	address = (MonoDebugMethodAddress *)g_hash_table_lookup (info->method_hash, method);

	if (!address)
		return NULL;

	return mono_debug_read_method (address, jit);
}

MonoDebugMethodJitInfo *
mono_debug_find_method (MonoMethod *method, MonoDomain *domain)
{
	MonoDebugMethodJitInfo *res = g_new0 (MonoDebugMethodJitInfo, 1);

	if (mono_debug_format == MONO_DEBUG_FORMAT_NONE)
		return NULL;

	mono_debugger_lock ();
	find_method (method, res);
	mono_debugger_unlock ();
	return res;
}

MonoDebugMethodAddressList *
mono_debug_lookup_method_addresses (MonoMethod *method)
{
	g_assert_not_reached ();
	return NULL;
}

static gint32
il_offset_from_address (MonoMethod *method, guint32 native_offset)
{
	MonoDebugMethodJitInfo mem;
	int i;

	MonoDebugMethodJitInfo *jit = find_method (method, &mem);
	if (!jit || !jit->line_numbers)
		goto cleanup_and_fail;

	for (i = jit->num_line_numbers - 1; i >= 0; i--) {
		MonoDebugLineNumberEntry lne = jit->line_numbers [i];

		if (lne.native_offset <= native_offset) {
			free_method_jit_info (jit, TRUE);
			return lne.il_offset;
		}
	}

cleanup_and_fail:
	free_method_jit_info (jit, TRUE);
	return -1;
}

/**
 * mono_debug_il_offset_from_address:
 *
 * Compute the IL offset corresponding to \p native_offset inside the native
 * code of \p method in \p domain.
 */
gint32
mono_debug_il_offset_from_address (MonoMethod *method, MonoDomain *domain, guint32 native_offset)
{
	gint32 res;

	mono_debugger_lock ();

	res = il_offset_from_address (method, native_offset);

	mono_debugger_unlock ();

	return res;
}

/**
 * mono_debug_lookup_source_location:
 * \param address Native offset within the \p method's machine code.
 * Lookup the source code corresponding to the machine instruction located at
 * native offset \p address within \p method.
 * The returned \c MonoDebugSourceLocation contains both file / line number
 * information and the corresponding IL offset.  It must be freed by
 * \c mono_debug_free_source_location.
 */
MonoDebugSourceLocation *
mono_debug_lookup_source_location (MonoMethod *method, guint32 address, MonoDomain *domain)
{
	MonoDebugMethodInfo *minfo;
	MonoDebugSourceLocation *location;
	gint32 offset;

	if (mono_debug_format == MONO_DEBUG_FORMAT_NONE)
		return NULL;

	mono_debugger_lock ();
	minfo = lookup_method (method);
	if (!minfo || !minfo->handle) {
		mono_debugger_unlock ();
		return NULL;
	}

	if (!minfo->handle->ppdb && (!minfo->handle->symfile || !mono_debug_symfile_is_loaded (minfo->handle->symfile))) {
		mono_debugger_unlock ();
		return NULL;
	}

	offset = il_offset_from_address (method, address);
	if (offset < 0) {
		mono_debugger_unlock ();
		return NULL;
	}

	if (minfo->handle->ppdb)
		location = mono_ppdb_lookup_location (minfo, offset);
	else
		location = mono_debug_symfile_lookup_location (minfo, offset);
	mono_debugger_unlock ();
	return location;
}

/**
 * mono_debug_lookup_source_location_by_il:
 *
 *   Same as mono_debug_lookup_source_location but take an IL_OFFSET argument.
 */
MonoDebugSourceLocation *
mono_debug_lookup_source_location_by_il (MonoMethod *method, guint32 il_offset, MonoDomain *domain)
{
	MonoDebugMethodInfo *minfo;
	MonoDebugSourceLocation *location;

	if (mono_debug_format == MONO_DEBUG_FORMAT_NONE)
		return NULL;

	mono_debugger_lock ();
	minfo = lookup_method (method);
	if (!minfo || !minfo->handle) {
		mono_debugger_unlock ();
		return NULL;
	}

	if (!minfo->handle->ppdb && (!minfo->handle->symfile || !mono_debug_symfile_is_loaded (minfo->handle->symfile))) {
		mono_debugger_unlock ();
		return NULL;
	}

	if (minfo->handle->ppdb)
		location = mono_ppdb_lookup_location (minfo, il_offset);
	else
		location = mono_debug_symfile_lookup_location (minfo, il_offset);
	mono_debugger_unlock ();
	return location;
}

MonoDebugSourceLocation *
mono_debug_method_lookup_location (MonoDebugMethodInfo *minfo, int il_offset)
{
	MonoDebugSourceLocation *location;

	mono_debugger_lock ();
	if (minfo->handle->ppdb)
		location = mono_ppdb_lookup_location (minfo, il_offset);
	else
		location = mono_debug_symfile_lookup_location (minfo, il_offset);
	mono_debugger_unlock ();
	return location;
}

/*
 * mono_debug_lookup_locals:
 *
 *   Return information about the local variables of MINFO.
 * The result should be freed using mono_debug_free_locals ().
 */
MonoDebugLocalsInfo*
mono_debug_lookup_locals (MonoMethod *method)
{
	MonoDebugMethodInfo *minfo;
	MonoDebugLocalsInfo *res;

	if (mono_debug_format == MONO_DEBUG_FORMAT_NONE)
		return NULL;

	mono_debugger_lock ();
	minfo = lookup_method (method);
	if (!minfo || !minfo->handle) {
		mono_debugger_unlock ();
		return NULL;
	}

	if (minfo->handle->ppdb) {
		res = mono_ppdb_lookup_locals (minfo);
	} else {
		if (!minfo->handle->symfile || !mono_debug_symfile_is_loaded (minfo->handle->symfile))
			res = NULL;
		else
			res = mono_debug_symfile_lookup_locals (minfo);
	}
	mono_debugger_unlock ();

	return res;
}

/*
 * mono_debug_free_locals:
 *
 *   Free all the data allocated by mono_debug_lookup_locals ().
 */
void
mono_debug_free_locals (MonoDebugLocalsInfo *info)
{
	int i;

	for (i = 0; i < info->num_locals; ++i)
		g_free (info->locals [i].name);
	g_free (info->locals);
	g_free (info->code_blocks);
	g_free (info);
}

/*
* mono_debug_lookup_method_async_debug_info:
*
*   Return information about the async stepping information of method.
* The result should be freed using mono_debug_free_async_debug_info ().
*/
MonoDebugMethodAsyncInfo*
mono_debug_lookup_method_async_debug_info (MonoMethod *method)
{
	MonoDebugMethodInfo *minfo;
	MonoDebugMethodAsyncInfo *res = NULL;

	if (mono_debug_format == MONO_DEBUG_FORMAT_NONE)
		return NULL;

	mono_debugger_lock ();
	minfo = lookup_method (method);
	if (!minfo || !minfo->handle) {
		mono_debugger_unlock ();
		return NULL;
	}

	if (minfo->handle->ppdb)
		res = mono_ppdb_lookup_method_async_debug_info (minfo);

	mono_debugger_unlock ();

	return res;
}

/*
 * mono_debug_free_method_async_debug_info:
 *
 *   Free all the data allocated by mono_debug_lookup_method_async_debug_info ().
 */
void
mono_debug_free_method_async_debug_info (MonoDebugMethodAsyncInfo *info)
{
	if (info->num_awaits) {
		g_free (info->yield_offsets);
		g_free (info->resume_offsets);
		g_free (info->move_next_method_token);
	}
	g_free (info);
}

/**
 * mono_debug_free_source_location:
 * \param location A \c MonoDebugSourceLocation
 * Frees the \p location.
 */
void
mono_debug_free_source_location (MonoDebugSourceLocation *location)
{
	if (location) {
		g_free (location->source_file);
		g_free (location);
	}
}

static int (*get_seq_point) (MonoMethod *method, gint32 native_offset);

void
mono_install_get_seq_point (MonoGetSeqPointFunc func)
{
	get_seq_point = func;
}

/**
 * mono_debug_print_stack_frame:
 * \param native_offset Native offset within the \p method's machine code.
 * Conventient wrapper around \c mono_debug_lookup_source_location which can be
 * used if you only want to use the location to print a stack frame.
 */
gchar *
mono_debug_print_stack_frame (MonoMethod *method, guint32 native_offset, MonoDomain *domain)
{
	MonoDebugSourceLocation *location;
	gchar *fname, *ptr, *res;
	int offset;

	fname = mono_method_full_name (method, TRUE);
	for (ptr = fname; *ptr; ptr++) {
		if (*ptr == ':') *ptr = '.';
	}

	location = mono_debug_lookup_source_location (method, native_offset, NULL);

	if (!location) {
		if (mono_debug_initialized) {
			mono_debugger_lock ();
			offset = il_offset_from_address (method, native_offset);
			mono_debugger_unlock ();
		} else {
			offset = -1;
		}

		if (offset < 0 && get_seq_point)
			offset = get_seq_point (method, native_offset);

		if (offset < 0)
			res = g_strdup_printf ("at %s <0x%05x>", fname, native_offset);
		else {
			char *mvid = mono_guid_to_string_minimal ((uint8_t*)m_class_get_image (method->klass)->heap_guid.data);
			char *aotid = mono_runtime_get_aotid ();
			if (aotid)
				res = g_strdup_printf ("at %s [0x%05x] in <%s#%s>:0" , fname, offset, mvid, aotid);
			else
				res = g_strdup_printf ("at %s [0x%05x] in <%s>:0" , fname, offset, mvid);

			g_free (aotid);
			g_free (mvid);
		}
		g_free (fname);
		return res;
	}

	res = g_strdup_printf ("at %s [0x%05x] in %s:%d", fname, location->il_offset,
			       location->source_file, location->row);

	g_free (fname);
	mono_debug_free_source_location (location);
	return res;
}

void
mono_set_is_debugger_attached (gboolean attached)
{
	is_attached = attached;
}

gboolean
mono_is_debugger_attached (void)
{
	return is_attached;
}

/*
 * Bundles
 */

typedef struct _BundledSymfile BundledSymfile;

struct _BundledSymfile {
	BundledSymfile *next;
	const char *aname;
	const mono_byte *raw_contents;
	int size;
};

static BundledSymfile *bundled_symfiles = NULL;

/**
 * mono_register_symfile_for_assembly:
 */
void
mono_register_symfile_for_assembly (const char *assembly_name, const mono_byte *raw_contents, int size)
{
	BundledSymfile *bsymfile;

	bsymfile = g_new0 (BundledSymfile, 1);
	bsymfile->aname = assembly_name;
	bsymfile->raw_contents = raw_contents;
	bsymfile->size = size;
	bsymfile->next = bundled_symfiles;
	bundled_symfiles = bsymfile;
}

static MonoDebugHandle *
open_symfile_from_bundle (MonoImage *image)
{
	BundledSymfile *bsymfile;

	for (bsymfile = bundled_symfiles; bsymfile; bsymfile = bsymfile->next) {
		if (strcmp (bsymfile->aname, image->module_name))
			continue;

		return mono_debug_open_image (image, bsymfile->raw_contents, bsymfile->size);
	}

	return NULL;
}

void
mono_debugger_lock (void)
{
	g_assert (mono_debug_initialized);
	mono_os_mutex_lock (&debugger_lock_mutex);
}

void
mono_debugger_unlock (void)
{
	g_assert (mono_debug_initialized);
	mono_os_mutex_unlock (&debugger_lock_mutex);
}

/**
 * mono_debug_enabled:
 *
 * Returns true is debug information is enabled. This doesn't relate if a debugger is present or not.
 */
mono_bool
mono_debug_enabled (void)
{
	return mono_debug_format != MONO_DEBUG_FORMAT_NONE;
}

void
mono_debug_get_seq_points (MonoDebugMethodInfo *minfo, char **source_file, GPtrArray **source_file_list, int **source_files, MonoSymSeqPoint **seq_points, int *n_seq_points)
{
	if (minfo->handle->ppdb)
		mono_ppdb_get_seq_points (minfo, source_file, source_file_list, source_files, seq_points, n_seq_points);
	else
		mono_debug_symfile_get_seq_points (minfo, source_file, source_file_list, source_files, seq_points, n_seq_points);
}

char*
mono_debug_image_get_sourcelink (MonoImage *image)
{
	MonoDebugHandle *handle = mono_debug_get_handle (image);

	if (handle && handle->ppdb)
		return mono_ppdb_get_sourcelink (handle);
	else
		return NULL;
}
