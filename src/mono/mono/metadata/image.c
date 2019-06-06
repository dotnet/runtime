/**
 * \file
 * Routines for manipulating an image stored in an
 * extended PE/COFF file.
 * 
 * Authors:
 *   Miguel de Icaza (miguel@ximian.com)
 *   Paolo Molaro (lupus@ximian.com)
 *
 * Copyright 2001-2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include <config.h>
#include <stdio.h>
#include <glib.h>
#include <errno.h>
#include <time.h>
#include <string.h>
#include "image.h"
#include "cil-coff.h"
#include "mono-endian.h"
#include "tabledefs.h"
#include "tokentype.h"
#include "metadata-internals.h"
#include "profiler-private.h"
#include "loader.h"
#include "marshal.h"
#include "coree.h"
#include <mono/metadata/exception-internals.h>
#include <mono/utils/checked-build.h>
#include <mono/utils/mono-logger-internals.h>
#include <mono/utils/mono-errno.h>
#include <mono/utils/mono-path.h>
#include <mono/utils/mono-mmap.h>
#include <mono/utils/mono-io-portability.h>
#include <mono/utils/atomic.h>
#include <mono/utils/mono-proclib.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/object-internals.h>
#include <mono/metadata/security-core-clr.h>
#include <mono/metadata/verify-internals.h>
#include <mono/metadata/verify.h>
#include <mono/metadata/image-internals.h>
#include <mono/metadata/w32process-internals.h>
#include <sys/types.h>
#include <sys/stat.h>
#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif
#include <mono/metadata/w32error.h>

#define INVALID_ADDRESS 0xffffffff

// Amount initially reserved in each image's mempool.
// FIXME: This number is arbitrary, a more practical number should be found
#define INITIAL_IMAGE_SIZE    512

/*
 * The "loaded images" hashes keep track of the various assemblies and netmodules loaded
 * There are four, for all combinations of [look up by path or assembly name?]
 * and [normal or reflection-only load?, as in Assembly.ReflectionOnlyLoad]
 */
enum {
	IMAGES_HASH_PATH = 0,
	IMAGES_HASH_PATH_REFONLY = 1,
	IMAGES_HASH_NAME = 2,
	IMAGES_HASH_NAME_REFONLY = 3,
	IMAGES_HASH_COUNT = 4
};
static GHashTable *loaded_images_hashes [4] = {NULL, NULL, NULL, NULL};

static GHashTable *
get_loaded_images_hash (gboolean refonly)
{
	int idx = refonly ? IMAGES_HASH_PATH_REFONLY : IMAGES_HASH_PATH;
	return loaded_images_hashes [idx];
}

static GHashTable *
get_loaded_images_by_name_hash (gboolean refonly)
{
	int idx = refonly ? IMAGES_HASH_NAME_REFONLY : IMAGES_HASH_NAME;
	return loaded_images_hashes [idx];
}

// Change the assembly set in `image` to the assembly set in `assemblyImage`. Halt if overwriting is attempted.
// Can be used on modules loaded through either the "file" or "module" mechanism
static gboolean
assign_assembly_parent_for_netmodule (MonoImage *image, MonoImage *assemblyImage, MonoError *error)
{
	// Assembly to assign
	MonoAssembly *assembly = assemblyImage->assembly;

	while (1) {
		// Assembly currently assigned
		MonoAssembly *assemblyOld = image->assembly;
		if (assemblyOld) {
			if (assemblyOld == assembly)
				return TRUE;
			mono_error_set_bad_image (error, assemblyImage, "Attempted to load module %s which has already been loaded by assembly %s. This is not supported in Mono.", image->name, assemblyOld->image->name);
			return FALSE;
		}
		gpointer result = mono_atomic_xchg_ptr((gpointer *)&image->assembly, assembly);
		if (result == assembly)
			return TRUE;
	}
}

static gboolean debug_assembly_unload = FALSE;

#define mono_images_lock() if (mutex_inited) mono_os_mutex_lock (&images_mutex)
#define mono_images_unlock() if (mutex_inited) mono_os_mutex_unlock (&images_mutex)
#define mono_images_storage_lock() do { if (mutex_inited) mono_os_mutex_lock (&images_storage_mutex); } while (0)
#define mono_images_storage_unlock() do { if (mutex_inited) mono_os_mutex_unlock (&images_storage_mutex); } while (0)
static gboolean mutex_inited;
static mono_mutex_t images_mutex;
static mono_mutex_t images_storage_mutex;

/* Maps string keys to MonoImageStorage values.
 *
 * The MonoImageStorage in the hash owns the key.
 */
static GHashTable *images_storage_hash;

static void install_pe_loader (void);

typedef struct ImageUnloadHook ImageUnloadHook;
struct ImageUnloadHook {
	MonoImageUnloadFunc func;
	gpointer user_data;
};

static GSList *image_unload_hooks;

void
mono_install_image_unload_hook (MonoImageUnloadFunc func, gpointer user_data)
{
	ImageUnloadHook *hook;
	
	g_return_if_fail (func != NULL);

	hook = g_new0 (ImageUnloadHook, 1);
	hook->func = func;
	hook->user_data = user_data;
	image_unload_hooks = g_slist_prepend (image_unload_hooks, hook);
}

void
mono_remove_image_unload_hook (MonoImageUnloadFunc func, gpointer user_data)
{
	GSList *l;
	ImageUnloadHook *hook;

	for (l = image_unload_hooks; l; l = l->next) {
		hook = (ImageUnloadHook *)l->data;

		if (hook->func == func && hook->user_data == user_data) {
			g_free (hook);
			image_unload_hooks = g_slist_delete_link (image_unload_hooks, l);
			break;
		}
	}
}

static void
mono_image_invoke_unload_hook (MonoImage *image)
{
	GSList *l;
	ImageUnloadHook *hook;

	for (l = image_unload_hooks; l; l = l->next) {
		hook = (ImageUnloadHook *)l->data;

		hook->func (image, hook->user_data);
	}
}

static GSList *image_loaders;

void
mono_install_image_loader (const MonoImageLoader *loader)
{
	image_loaders = g_slist_prepend (image_loaders, (MonoImageLoader*)loader);
}

/* returns offset relative to image->raw_data */
guint32
mono_cli_rva_image_map (MonoImage *image, guint32 addr)
{
	MonoCLIImageInfo *iinfo = image->image_info;
	const int top = iinfo->cli_section_count;
	MonoSectionTable *tables = iinfo->cli_section_tables;
	int i;

	if (image->metadata_only)
		return addr;

	for (i = 0; i < top; i++){
		if ((addr >= tables->st_virtual_address) &&
		    (addr < tables->st_virtual_address + tables->st_raw_data_size)){
#ifdef HOST_WIN32
			if (m_image_is_module_handle (image))
				return addr;
#endif
			return addr - tables->st_virtual_address + tables->st_raw_data_ptr;
		}
		tables++;
	}
	return INVALID_ADDRESS;
}

/**
 * mono_image_rva_map:
 * \param image a \c MonoImage
 * \param addr relative virtual address (RVA)
 *
 * This is a low-level routine used by the runtime to map relative
 * virtual address (RVA) into their location in memory. 
 *
 * \returns the address in memory for the given RVA, or NULL if the
 * RVA is not valid for this image. 
 */
char *
mono_image_rva_map (MonoImage *image, guint32 addr)
{
	MonoCLIImageInfo *iinfo = image->image_info;
	const int top = iinfo->cli_section_count;
	MonoSectionTable *tables = iinfo->cli_section_tables;
	int i;

#ifdef HOST_WIN32
	if (m_image_is_module_handle (image)) {
		if (addr && addr < image->raw_data_len)
			return image->raw_data + addr;
		else
			return NULL;
	}
#endif

	for (i = 0; i < top; i++){
		if ((addr >= tables->st_virtual_address) &&
		    (addr < tables->st_virtual_address + tables->st_raw_data_size)){
			if (!iinfo->cli_sections [i]) {
				if (!mono_image_ensure_section_idx (image, i))
					return NULL;
			}
			return (char*)iinfo->cli_sections [i] +
				(addr - tables->st_virtual_address);
		}
		tables++;
	}
	return NULL;
}

/**
 * mono_images_init:
 *
 *  Initialize the global variables used by this module.
 */
void
mono_images_init (void)
{
	mono_os_mutex_init (&images_storage_mutex);
	mono_os_mutex_init_recursive (&images_mutex);

	images_storage_hash = g_hash_table_new (g_str_hash, g_str_equal);

	int hash_idx;
	for(hash_idx = 0; hash_idx < IMAGES_HASH_COUNT; hash_idx++)
		loaded_images_hashes [hash_idx] = g_hash_table_new (g_str_hash, g_str_equal);

	debug_assembly_unload = g_hasenv ("MONO_DEBUG_ASSEMBLY_UNLOAD");

	install_pe_loader ();

	mutex_inited = TRUE;
}

/**
 * mono_images_cleanup:
 *
 *  Free all resources used by this module.
 */
void
mono_images_cleanup (void)
{
	GHashTableIter iter;
	MonoImage *image;

	mono_os_mutex_destroy (&images_mutex);

	// If an assembly image is still loaded at shutdown, this could indicate managed code is still running.
	// Reflection-only images being still loaded doesn't indicate anything as harmful, so we don't check for it.
	g_hash_table_iter_init (&iter, get_loaded_images_hash (FALSE));
	while (g_hash_table_iter_next (&iter, NULL, (void**)&image))
		mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_ASSEMBLY, "Assembly image '%s' still loaded at shutdown.", image->name);

	int hash_idx;
	for(hash_idx = 0; hash_idx < IMAGES_HASH_COUNT; hash_idx++)
		g_hash_table_destroy (loaded_images_hashes [hash_idx]);

	g_hash_table_destroy (images_storage_hash);

	mono_os_mutex_destroy (&images_storage_mutex);

	mutex_inited = FALSE;
}

/**
 * mono_image_ensure_section_idx:
 * \param image The image we are operating on
 * \param section section number that we will load/map into memory
 *
 * This routine makes sure that we have an in-memory copy of
 * an image section (<code>.text</code>, <code>.rsrc</code>, <code>.data</code>).
 *
 * \returns TRUE on success
 */
int
mono_image_ensure_section_idx (MonoImage *image, int section)
{
	MonoCLIImageInfo *iinfo = image->image_info;
	MonoSectionTable *sect;
	
	g_return_val_if_fail (section < iinfo->cli_section_count, FALSE);

	if (iinfo->cli_sections [section] != NULL)
		return TRUE;

	sect = &iinfo->cli_section_tables [section];
	
	if (sect->st_raw_data_ptr + sect->st_raw_data_size > image->raw_data_len)
		return FALSE;
#ifdef HOST_WIN32
	if (m_image_is_module_handle (image))
		iinfo->cli_sections [section] = image->raw_data + sect->st_virtual_address;
	else
#endif
	/* FIXME: we ignore the writable flag since we don't patch the binary */
	iinfo->cli_sections [section] = image->raw_data + sect->st_raw_data_ptr;
	return TRUE;
}

/**
 * mono_image_ensure_section:
 * \param image The image we are operating on
 * \param section section name that we will load/map into memory
 *
 * This routine makes sure that we have an in-memory copy of
 * an image section (.text, .rsrc, .data).
 *
 * \returns TRUE on success
 */
int
mono_image_ensure_section (MonoImage *image, const char *section)
{
	MonoCLIImageInfo *ii = image->image_info;
	int i;
	
	for (i = 0; i < ii->cli_section_count; i++){
		if (strncmp (ii->cli_section_tables [i].st_name, section, 8) != 0)
			continue;
		
		return mono_image_ensure_section_idx (image, i);
	}
	return FALSE;
}

static int
load_section_tables (MonoImage *image, MonoCLIImageInfo *iinfo, guint32 offset)
{
	const int top = iinfo->cli_header.coff.coff_sections;
	int i;

	iinfo->cli_section_count = top;
	iinfo->cli_section_tables = g_new0 (MonoSectionTable, top);
	iinfo->cli_sections = g_new0 (void *, top);
	
	for (i = 0; i < top; i++){
		MonoSectionTable *t = &iinfo->cli_section_tables [i];

		if (offset + sizeof (MonoSectionTable) > image->raw_data_len)
			return FALSE;
		memcpy (t, image->raw_data + offset, sizeof (MonoSectionTable));
		offset += sizeof (MonoSectionTable);

#if G_BYTE_ORDER != G_LITTLE_ENDIAN
		t->st_virtual_size = GUINT32_FROM_LE (t->st_virtual_size);
		t->st_virtual_address = GUINT32_FROM_LE (t->st_virtual_address);
		t->st_raw_data_size = GUINT32_FROM_LE (t->st_raw_data_size);
		t->st_raw_data_ptr = GUINT32_FROM_LE (t->st_raw_data_ptr);
		t->st_reloc_ptr = GUINT32_FROM_LE (t->st_reloc_ptr);
		t->st_lineno_ptr = GUINT32_FROM_LE (t->st_lineno_ptr);
		t->st_reloc_count = GUINT16_FROM_LE (t->st_reloc_count);
		t->st_line_count = GUINT16_FROM_LE (t->st_line_count);
		t->st_flags = GUINT32_FROM_LE (t->st_flags);
#endif
		/* consistency checks here */
	}

	return TRUE;
}

gboolean
mono_image_load_cli_header (MonoImage *image, MonoCLIImageInfo *iinfo)
{
	guint32 offset;
	
	offset = mono_cli_rva_image_map (image, iinfo->cli_header.datadir.pe_cli_header.rva);
	if (offset == INVALID_ADDRESS)
		return FALSE;

	if (offset + sizeof (MonoCLIHeader) > image->raw_data_len)
		return FALSE;
	memcpy (&iinfo->cli_cli_header, image->raw_data + offset, sizeof (MonoCLIHeader));

#if G_BYTE_ORDER != G_LITTLE_ENDIAN
#define SWAP32(x) (x) = GUINT32_FROM_LE ((x))
#define SWAP16(x) (x) = GUINT16_FROM_LE ((x))
#define SWAPPDE(x) do { (x).rva = GUINT32_FROM_LE ((x).rva); (x).size = GUINT32_FROM_LE ((x).size);} while (0)
	SWAP32 (iinfo->cli_cli_header.ch_size);
	SWAP32 (iinfo->cli_cli_header.ch_flags);
	SWAP32 (iinfo->cli_cli_header.ch_entry_point);
	SWAP16 (iinfo->cli_cli_header.ch_runtime_major);
	SWAP16 (iinfo->cli_cli_header.ch_runtime_minor);
	SWAPPDE (iinfo->cli_cli_header.ch_metadata);
	SWAPPDE (iinfo->cli_cli_header.ch_resources);
	SWAPPDE (iinfo->cli_cli_header.ch_strong_name);
	SWAPPDE (iinfo->cli_cli_header.ch_code_manager_table);
	SWAPPDE (iinfo->cli_cli_header.ch_vtable_fixups);
	SWAPPDE (iinfo->cli_cli_header.ch_export_address_table_jumps);
	SWAPPDE (iinfo->cli_cli_header.ch_eeinfo_table);
	SWAPPDE (iinfo->cli_cli_header.ch_helper_table);
	SWAPPDE (iinfo->cli_cli_header.ch_dynamic_info);
	SWAPPDE (iinfo->cli_cli_header.ch_delay_load_info);
	SWAPPDE (iinfo->cli_cli_header.ch_module_image);
	SWAPPDE (iinfo->cli_cli_header.ch_external_fixups);
	SWAPPDE (iinfo->cli_cli_header.ch_ridmap);
	SWAPPDE (iinfo->cli_cli_header.ch_debug_map);
	SWAPPDE (iinfo->cli_cli_header.ch_ip_map);
#undef SWAP32
#undef SWAP16
#undef SWAPPDE
#endif
	/* Catch new uses of the fields that are supposed to be zero */

	if ((iinfo->cli_cli_header.ch_eeinfo_table.rva != 0) ||
	    (iinfo->cli_cli_header.ch_helper_table.rva != 0) ||
	    (iinfo->cli_cli_header.ch_dynamic_info.rva != 0) ||
	    (iinfo->cli_cli_header.ch_delay_load_info.rva != 0) ||
	    (iinfo->cli_cli_header.ch_module_image.rva != 0) ||
	    (iinfo->cli_cli_header.ch_external_fixups.rva != 0) ||
	    (iinfo->cli_cli_header.ch_ridmap.rva != 0) ||
	    (iinfo->cli_cli_header.ch_debug_map.rva != 0) ||
	    (iinfo->cli_cli_header.ch_ip_map.rva != 0)){

		/*
		 * No need to scare people who are testing this, I am just
		 * labelling this as a LAMESPEC
		 */
		/* g_warning ("Some fields in the CLI header which should have been zero are not zero"); */

	}
	    
	return TRUE;
}

static gboolean
load_metadata_ptrs (MonoImage *image, MonoCLIImageInfo *iinfo)
{
	guint32 offset, size;
	guint16 streams;
	int i;
	guint32 pad;
	char *ptr;
	
	offset = mono_cli_rva_image_map (image, iinfo->cli_cli_header.ch_metadata.rva);
	if (offset == INVALID_ADDRESS)
		return FALSE;

	size = iinfo->cli_cli_header.ch_metadata.size;

	if (offset + size > image->raw_data_len)
		return FALSE;
	image->raw_metadata = image->raw_data + offset;

	/* 24.2.1: Metadata root starts here */
	ptr = image->raw_metadata;

	if (strncmp (ptr, "BSJB", 4) == 0){
		guint32 version_string_len;

		ptr += 4;
		image->md_version_major = read16 (ptr);
		ptr += 2;
		image->md_version_minor = read16 (ptr);
		ptr += 6;

		version_string_len = read32 (ptr);
		ptr += 4;
		image->version = g_strndup (ptr, version_string_len);
		ptr += version_string_len;
		pad = ptr - image->raw_metadata;
		if (pad % 4)
			ptr += 4 - (pad % 4);
	} else
		return FALSE;

	/* skip over flags */
	ptr += 2;
	
	streams = read16 (ptr);
	ptr += 2;

	for (i = 0; i < streams; i++){
		if (strncmp (ptr + 8, "#~", 3) == 0){
			image->heap_tables.data = image->raw_metadata + read32 (ptr);
			image->heap_tables.size = read32 (ptr + 4);
			ptr += 8 + 3;
		} else if (strncmp (ptr + 8, "#Strings", 9) == 0){
			image->heap_strings.data = image->raw_metadata + read32 (ptr);
			image->heap_strings.size = read32 (ptr + 4);
			ptr += 8 + 9;
		} else if (strncmp (ptr + 8, "#US", 4) == 0){
			image->heap_us.data = image->raw_metadata + read32 (ptr);
			image->heap_us.size = read32 (ptr + 4);
			ptr += 8 + 4;
		} else if (strncmp (ptr + 8, "#Blob", 6) == 0){
			image->heap_blob.data = image->raw_metadata + read32 (ptr);
			image->heap_blob.size = read32 (ptr + 4);
			ptr += 8 + 6;
		} else if (strncmp (ptr + 8, "#GUID", 6) == 0){
			image->heap_guid.data = image->raw_metadata + read32 (ptr);
			image->heap_guid.size = read32 (ptr + 4);
			ptr += 8 + 6;
		} else if (strncmp (ptr + 8, "#-", 3) == 0) {
			image->heap_tables.data = image->raw_metadata + read32 (ptr);
			image->heap_tables.size = read32 (ptr + 4);
			ptr += 8 + 3;
			image->uncompressed_metadata = TRUE;
			mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_ASSEMBLY, "Assembly '%s' has the non-standard metadata heap #-.\nRecompile it correctly (without the /incremental switch or in Release mode).", image->name);
		} else if (strncmp (ptr + 8, "#Pdb", 5) == 0) {
			image->heap_pdb.data = image->raw_metadata + read32 (ptr);
			image->heap_pdb.size = read32 (ptr + 4);
			ptr += 8 + 5;
		} else {
			g_message ("Unknown heap type: %s\n", ptr + 8);
			ptr += 8 + strlen (ptr + 8) + 1;
		}
		pad = ptr - image->raw_metadata;
		if (pad % 4)
			ptr += 4 - (pad % 4);
	}

	i = ((MonoImageLoader*)image->loader)->load_tables (image);

	if (!image->metadata_only) {
		g_assert (image->heap_guid.data);
		g_assert (image->heap_guid.size >= 16);

		image->guid = mono_guid_to_string ((guint8*)image->heap_guid.data);
	} else {
		/* PPDB files have no guid */
		guint8 empty_guid [16];

		memset (empty_guid, 0, sizeof (empty_guid));

		image->guid = mono_guid_to_string (empty_guid);
	}

	return i;
}

/*
 * Load representation of logical metadata tables, from the "#~" stream
 */
static gboolean
load_tables (MonoImage *image)
{
	const char *heap_tables = image->heap_tables.data;
	const guint32 *rows;
	guint64 valid_mask;
	int valid = 0, table;
	int heap_sizes;
	
	heap_sizes = heap_tables [6];
	image->idx_string_wide = ((heap_sizes & 0x01) == 1);
	image->idx_guid_wide   = ((heap_sizes & 0x02) == 2);
	image->idx_blob_wide   = ((heap_sizes & 0x04) == 4);
	
	valid_mask = read64 (heap_tables + 8);
	rows = (const guint32 *) (heap_tables + 24);
	
	for (table = 0; table < 64; table++){
		if ((valid_mask & ((guint64) 1 << table)) == 0){
			if (table > MONO_TABLE_LAST)
				continue;
			image->tables [table].rows = 0;
			continue;
		}
		if (table > MONO_TABLE_LAST) {
			g_warning("bits in valid must be zero above 0x37 (II - 23.1.6)");
		} else {
			image->tables [table].rows = read32 (rows);
		}
		rows++;
		valid++;
	}

	image->tables_base = (heap_tables + 24) + (4 * valid);

	/* They must be the same */
	g_assert ((const void *) image->tables_base == (const void *) rows);

	if (image->heap_pdb.size) {
		/*
		 * Obtain token sizes from the pdb stream.
		 */
		/* 24 = guid + entry point */
		int pos = 24;
		image->referenced_tables = read64 (image->heap_pdb.data + pos);
		pos += 8;
		image->referenced_table_rows = g_new0 (int, 64);
		for (int i = 0; i < 64; ++i) {
			if (image->referenced_tables & ((guint64)1 << i)) {
				image->referenced_table_rows [i] = read32 (image->heap_pdb.data + pos);
				pos += 4;
			}
		}
	}

	mono_metadata_compute_table_bases (image);
	return TRUE;
}

gboolean
mono_image_load_metadata (MonoImage *image, MonoCLIImageInfo *iinfo)
{
	if (!load_metadata_ptrs (image, iinfo))
		return FALSE;

	return load_tables (image);
}

void
mono_image_check_for_module_cctor (MonoImage *image)
{
	MonoTableInfo *t, *mt;
	t = &image->tables [MONO_TABLE_TYPEDEF];
	mt = &image->tables [MONO_TABLE_METHOD];
	if (image_is_dynamic (image)) {
		/* FIXME: */
		image->checked_module_cctor = TRUE;
		return;
	}
	if (t->rows >= 1) {
		guint32 nameidx = mono_metadata_decode_row_col (t, 0, MONO_TYPEDEF_NAME);
		const char *name = mono_metadata_string_heap (image, nameidx);
		if (strcmp (name, "<Module>") == 0) {
			guint32 first_method = mono_metadata_decode_row_col (t, 0, MONO_TYPEDEF_METHOD_LIST) - 1;
			guint32 last_method;
			if (t->rows > 1)
				last_method = mono_metadata_decode_row_col (t, 1, MONO_TYPEDEF_METHOD_LIST) - 1;
			else 
				last_method = mt->rows;
			for (; first_method < last_method; first_method++) {
				nameidx = mono_metadata_decode_row_col (mt, first_method, MONO_METHOD_NAME);
				name = mono_metadata_string_heap (image, nameidx);
				if (strcmp (name, ".cctor") == 0) {
					image->has_module_cctor = TRUE;
					image->checked_module_cctor = TRUE;
					return;
				}
			}
		}
	}
	image->has_module_cctor = FALSE;
	image->checked_module_cctor = TRUE;
}

static void
load_modules (MonoImage *image)
{
	MonoTableInfo *t;

	if (image->modules)
		return;

	t = &image->tables [MONO_TABLE_MODULEREF];
	image->modules = g_new0 (MonoImage *, t->rows);
	image->modules_loaded = g_new0 (gboolean, t->rows);
	image->module_count = t->rows;
}

/**
 * mono_image_load_module_checked:
 *
 *   Load the module with the one-based index IDX from IMAGE and return it. Return NULL if
 * it cannot be loaded. NULL without MonoError being set will be interpreted as "not found".
 */
MonoImage*
mono_image_load_module_checked (MonoImage *image, int idx, MonoError *error)
{
	MonoTableInfo *t;
	MonoTableInfo *file_table;
	int i;
	char *base_dir;
	gboolean refonly = image->ref_only;
	GList *list_iter, *valid_modules = NULL;
	MonoImageOpenStatus status;

	error_init (error);

	if ((image->module_count == 0) || (idx > image->module_count || idx <= 0))
		return NULL;
	if (image->modules_loaded [idx - 1])
		return image->modules [idx - 1];

	file_table = &image->tables [MONO_TABLE_FILE];
	for (i = 0; i < file_table->rows; i++) {
		guint32 cols [MONO_FILE_SIZE];
		mono_metadata_decode_row (file_table, i, cols, MONO_FILE_SIZE);
		if (cols [MONO_FILE_FLAGS] == FILE_CONTAINS_NO_METADATA)
			continue;
		valid_modules = g_list_prepend (valid_modules, (char*)mono_metadata_string_heap (image, cols [MONO_FILE_NAME]));
	}

	t = &image->tables [MONO_TABLE_MODULEREF];
	base_dir = g_path_get_dirname (image->name);

	{
		char *module_ref;
		const char *name;
		guint32 cols [MONO_MODULEREF_SIZE];
		/* if there is no file table, we try to load the module... */
		int valid = file_table->rows == 0;

		mono_metadata_decode_row (t, idx - 1, cols, MONO_MODULEREF_SIZE);
		name = mono_metadata_string_heap (image, cols [MONO_MODULEREF_NAME]);
		for (list_iter = valid_modules; list_iter; list_iter = list_iter->next) {
			/* be safe with string dups, but we could just compare string indexes  */
			if (strcmp ((const char*)list_iter->data, name) == 0) {
				valid = TRUE;
				break;
			}
		}
		if (valid) {
			module_ref = g_build_filename (base_dir, name, NULL);
			MonoImage *moduleImage = mono_image_open_full (module_ref, &status, refonly);
			if (moduleImage) {
				if (!assign_assembly_parent_for_netmodule (moduleImage, image, error)) {
					mono_image_close (moduleImage);
					g_free (module_ref);
					g_free (base_dir);
					g_list_free (valid_modules);
					return NULL;
				}

				image->modules [idx - 1] = moduleImage;

#ifdef HOST_WIN32
				if (m_image_is_module_handle (image->modules [idx - 1]))
					mono_image_fixup_vtable (image->modules [idx - 1]);
#endif
				/* g_print ("loaded module %s from %s (%p)\n", module_ref, image->name, image->assembly); */
			}
			g_free (module_ref);
		}
	}

	image->modules_loaded [idx - 1] = TRUE;

	g_free (base_dir);
	g_list_free (valid_modules);

	return image->modules [idx - 1];
}

/**
 * mono_image_load_module:
 */
MonoImage*
mono_image_load_module (MonoImage *image, int idx)
{
	ERROR_DECL (error);
	MonoImage *result = mono_image_load_module_checked (image, idx, error);
	mono_error_assert_ok (error);
	return result;
}

static gpointer
class_key_extract (gpointer value)
{
	MonoClass *klass = (MonoClass *)value;

	return GUINT_TO_POINTER (m_class_get_type_token (klass));
}

static gpointer*
class_next_value (gpointer value)
{
	MonoClassDef *klass = (MonoClassDef *)value;

	return (gpointer*)m_classdef_get_next_class_cache (klass);
}

/**
 * mono_image_init:
 */
void
mono_image_init (MonoImage *image)
{
	mono_os_mutex_init_recursive (&image->lock);
	mono_os_mutex_init_recursive (&image->szarray_cache_lock);

	image->mempool = mono_mempool_new_size (INITIAL_IMAGE_SIZE);
	mono_internal_hash_table_init (&image->class_cache,
				       g_direct_hash,
				       class_key_extract,
				       class_next_value);
	image->field_cache = mono_conc_hashtable_new (NULL, NULL);

	image->typespec_cache = mono_conc_hashtable_new (NULL, NULL);
	image->memberref_signatures = g_hash_table_new (NULL, NULL);
	image->method_signatures = g_hash_table_new (NULL, NULL);

	image->property_hash = mono_property_hash_new ();
}

#if G_BYTE_ORDER != G_LITTLE_ENDIAN
#define SWAP64(x) (x) = GUINT64_FROM_LE ((x))
#define SWAP32(x) (x) = GUINT32_FROM_LE ((x))
#define SWAP16(x) (x) = GUINT16_FROM_LE ((x))
#define SWAPPDE(x) do { (x).rva = GUINT32_FROM_LE ((x).rva); (x).size = GUINT32_FROM_LE ((x).size);} while (0)
#else
#define SWAP64(x)
#define SWAP32(x)
#define SWAP16(x)
#define SWAPPDE(x)
#endif

/*
 * Returns < 0 to indicate an error.
 */
static int
do_load_header (MonoImage *image, MonoDotNetHeader *header, int offset)
{
	MonoDotNetHeader64 header64;

#ifdef HOST_WIN32
	if (!m_image_is_module_handle (image))
#endif
	if (offset + sizeof (MonoDotNetHeader32) > image->raw_data_len)
		return -1;

	memcpy (header, image->raw_data + offset, sizeof (MonoDotNetHeader));

	if (header->pesig [0] != 'P' || header->pesig [1] != 'E' || header->pesig [2] || header->pesig [3])
		return -1;

	/* endian swap the fields common between PE and PE+ */
	SWAP32 (header->coff.coff_time);
	SWAP32 (header->coff.coff_symptr);
	SWAP32 (header->coff.coff_symcount);
	SWAP16 (header->coff.coff_machine);
	SWAP16 (header->coff.coff_sections);
	SWAP16 (header->coff.coff_opt_header_size);
	SWAP16 (header->coff.coff_attributes);
	/* MonoPEHeader */
	SWAP32 (header->pe.pe_code_size);
	SWAP32 (header->pe.pe_uninit_data_size);
	SWAP32 (header->pe.pe_rva_entry_point);
	SWAP32 (header->pe.pe_rva_code_base);
	SWAP32 (header->pe.pe_rva_data_base);
	SWAP16 (header->pe.pe_magic);

	/* now we are ready for the basic tests */

	if (header->pe.pe_magic == 0x10B) {
		offset += sizeof (MonoDotNetHeader);
		SWAP32 (header->pe.pe_data_size);
		if (header->coff.coff_opt_header_size != (sizeof (MonoDotNetHeader) - sizeof (MonoCOFFHeader) - 4))
			return -1;

		SWAP32	(header->nt.pe_image_base); 	/* must be 0x400000 */
		SWAP32	(header->nt.pe_stack_reserve);
		SWAP32	(header->nt.pe_stack_commit);
		SWAP32	(header->nt.pe_heap_reserve);
		SWAP32	(header->nt.pe_heap_commit);
	} else if (header->pe.pe_magic == 0x20B) {
		/* PE32+ file format */
		if (header->coff.coff_opt_header_size != (sizeof (MonoDotNetHeader64) - sizeof (MonoCOFFHeader) - 4))
			return -1;
		memcpy (&header64, image->raw_data + offset, sizeof (MonoDotNetHeader64));
		offset += sizeof (MonoDotNetHeader64);
		/* copy the fields already swapped. the last field, pe_data_size, is missing */
		memcpy (&header64, header, sizeof (MonoDotNetHeader) - 4);
		/* FIXME: we lose bits here, but we don't use this stuff internally, so we don't care much.
		 * will be fixed when we change MonoDotNetHeader to not match the 32 bit variant
		 */
		SWAP64	(header64.nt.pe_image_base);
		header->nt.pe_image_base = header64.nt.pe_image_base;
		SWAP64	(header64.nt.pe_stack_reserve);
		header->nt.pe_stack_reserve = header64.nt.pe_stack_reserve;
		SWAP64	(header64.nt.pe_stack_commit);
		header->nt.pe_stack_commit = header64.nt.pe_stack_commit;
		SWAP64	(header64.nt.pe_heap_reserve);
		header->nt.pe_heap_reserve = header64.nt.pe_heap_reserve;
		SWAP64	(header64.nt.pe_heap_commit);
		header->nt.pe_heap_commit = header64.nt.pe_heap_commit;

		header->nt.pe_section_align = header64.nt.pe_section_align;
		header->nt.pe_file_alignment = header64.nt.pe_file_alignment;
		header->nt.pe_os_major = header64.nt.pe_os_major;
		header->nt.pe_os_minor = header64.nt.pe_os_minor;
		header->nt.pe_user_major = header64.nt.pe_user_major;
		header->nt.pe_user_minor = header64.nt.pe_user_minor;
		header->nt.pe_subsys_major = header64.nt.pe_subsys_major;
		header->nt.pe_subsys_minor = header64.nt.pe_subsys_minor;
		header->nt.pe_reserved_1 = header64.nt.pe_reserved_1;
		header->nt.pe_image_size = header64.nt.pe_image_size;
		header->nt.pe_header_size = header64.nt.pe_header_size;
		header->nt.pe_checksum = header64.nt.pe_checksum;
		header->nt.pe_subsys_required = header64.nt.pe_subsys_required;
		header->nt.pe_dll_flags = header64.nt.pe_dll_flags;
		header->nt.pe_loader_flags = header64.nt.pe_loader_flags;
		header->nt.pe_data_dir_count = header64.nt.pe_data_dir_count;

		/* copy the datadir */
		memcpy (&header->datadir, &header64.datadir, sizeof (MonoPEDatadir));
	} else {
		return -1;
	}

	/* MonoPEHeaderNT: not used yet */
	SWAP32	(header->nt.pe_section_align);       /* must be 8192 */
	SWAP32	(header->nt.pe_file_alignment);      /* must be 512 or 4096 */
	SWAP16	(header->nt.pe_os_major);            /* must be 4 */
	SWAP16	(header->nt.pe_os_minor);            /* must be 0 */
	SWAP16	(header->nt.pe_user_major);
	SWAP16	(header->nt.pe_user_minor);
	SWAP16	(header->nt.pe_subsys_major);
	SWAP16	(header->nt.pe_subsys_minor);
	SWAP32	(header->nt.pe_reserved_1);
	SWAP32	(header->nt.pe_image_size);
	SWAP32	(header->nt.pe_header_size);
	SWAP32	(header->nt.pe_checksum);
	SWAP16	(header->nt.pe_subsys_required);
	SWAP16	(header->nt.pe_dll_flags);
	SWAP32	(header->nt.pe_loader_flags);
	SWAP32	(header->nt.pe_data_dir_count);

	/* MonoDotNetHeader: mostly unused */
	SWAPPDE (header->datadir.pe_export_table);
	SWAPPDE (header->datadir.pe_import_table);
	SWAPPDE (header->datadir.pe_resource_table);
	SWAPPDE (header->datadir.pe_exception_table);
	SWAPPDE (header->datadir.pe_certificate_table);
	SWAPPDE (header->datadir.pe_reloc_table);
	SWAPPDE (header->datadir.pe_debug);
	SWAPPDE (header->datadir.pe_copyright);
	SWAPPDE (header->datadir.pe_global_ptr);
	SWAPPDE (header->datadir.pe_tls_table);
	SWAPPDE (header->datadir.pe_load_config_table);
	SWAPPDE (header->datadir.pe_bound_import);
	SWAPPDE (header->datadir.pe_iat);
	SWAPPDE (header->datadir.pe_delay_import_desc);
 	SWAPPDE (header->datadir.pe_cli_header);
	SWAPPDE (header->datadir.pe_reserved);

#ifdef HOST_WIN32
	if (m_image_is_module_handle (image))
		image->storage->raw_data_len = header->nt.pe_image_size;
#endif

	return offset;
}

gboolean
mono_image_load_pe_data (MonoImage *image)
{
	return ((MonoImageLoader*)image->loader)->load_pe_data (image);
}

static gboolean
pe_image_load_pe_data (MonoImage *image)
{
	MonoCLIImageInfo *iinfo;
	MonoDotNetHeader *header;
	MonoMSDOSHeader msdos;
	gint32 offset = 0;

	iinfo = image->image_info;
	header = &iinfo->cli_header;

#ifdef HOST_WIN32
	if (!m_image_is_module_handle (image))
#endif
	if (offset + sizeof (msdos) > image->raw_data_len)
		goto invalid_image;
	memcpy (&msdos, image->raw_data + offset, sizeof (msdos));
	
	if (!(msdos.msdos_sig [0] == 'M' && msdos.msdos_sig [1] == 'Z'))
		goto invalid_image;
	
	msdos.pe_offset = GUINT32_FROM_LE (msdos.pe_offset);

	offset = msdos.pe_offset;

	offset = do_load_header (image, header, offset);
	if (offset < 0)
		goto invalid_image;

	/*
	 * this tests for a x86 machine type, but itanium, amd64 and others could be used, too.
	 * we skip this test.
	if (header->coff.coff_machine != 0x14c)
		goto invalid_image;
	*/

#if 0
	/*
	 * The spec says that this field should contain 6.0, but Visual Studio includes a new compiler,
	 * which produces binaries with 7.0.  From Sergey:
	 *
	 * The reason is that MSVC7 uses traditional compile/link
	 * sequence for CIL executables, and VS.NET (and Framework
	 * SDK) includes linker version 7, that puts 7.0 in this
	 * field.  That's why it's currently not possible to load VC
	 * binaries with Mono.  This field is pretty much meaningless
	 * anyway (what linker?).
	 */
	if (header->pe.pe_major != 6 || header->pe.pe_minor != 0)
		goto invalid_image;
#endif

	/*
	 * FIXME: byte swap all addresses here for header.
	 */
	
	if (!load_section_tables (image, iinfo, offset))
		goto invalid_image;

	return TRUE;

invalid_image:
	return FALSE;
}

gboolean
mono_image_load_cli_data (MonoImage *image)
{
	return ((MonoImageLoader*)image->loader)->load_cli_data (image);
}

static gboolean
pe_image_load_cli_data (MonoImage *image)
{
	MonoCLIImageInfo *iinfo;
	MonoDotNetHeader *header;

	iinfo = image->image_info;
	header = &iinfo->cli_header;

	/* Load the CLI header */
	if (!mono_image_load_cli_header (image, iinfo))
		return FALSE;

	if (!mono_image_load_metadata (image, iinfo))
		return FALSE;

	return TRUE;
}

static void
mono_image_load_time_date_stamp (MonoImage *image)
{
	image->time_date_stamp = 0;
#ifndef HOST_WIN32
	if (!image->filename)
		return;

	gunichar2 *uni_name = g_utf8_to_utf16 (image->filename, -1, NULL, NULL, NULL);
	mono_pe_file_time_date_stamp (uni_name, &image->time_date_stamp);
	g_free (uni_name);
#endif
}

void
mono_image_load_names (MonoImage *image)
{
	/* modules don't have an assembly table row */
	if (image->tables [MONO_TABLE_ASSEMBLY].rows) {
		image->assembly_name = mono_metadata_string_heap (image, 
			mono_metadata_decode_row_col (&image->tables [MONO_TABLE_ASSEMBLY],
					0, MONO_ASSEMBLY_NAME));
	}

	/* Portable pdb images don't have a MODULE row */
	if (image->tables [MONO_TABLE_MODULE].rows) {
		image->module_name = mono_metadata_string_heap (image,
			mono_metadata_decode_row_col (&image->tables [MONO_TABLE_MODULE],
					0, MONO_MODULE_NAME));
	}
}

static gboolean
pe_image_load_tables (MonoImage *image)
{
	return TRUE;
}

static gboolean
pe_image_match (MonoImage *image)
{
	if (image->raw_data [0] == 'M' && image->raw_data [1] == 'Z')
		return TRUE;
	return FALSE;
}

static const MonoImageLoader pe_loader = {
	pe_image_match,
	pe_image_load_pe_data,
	pe_image_load_cli_data,
	pe_image_load_tables,
};

static void
install_pe_loader (void)
{
	mono_install_image_loader (&pe_loader);
}

/*
Ignored assemblies.

There are some assemblies we need to ignore because they include an implementation that doesn't work under mono.
Mono provides its own implementation of those assemblies so it's safe to do so.

The ignored_assemblies list is generated using tools/nuget-hash-extractor and feeding the problematic nugets to it.

Right now the list of nugets are the ones that provide the assemblies in $ignored_assemblies_file_names.

This is to be removed once a proper fix is shipped through nuget.

Please keep this in sync with mcs/tools/xbuild/data/deniedAssembliesList.txt
If any assemblies are added/removed, then this should be regenerated with:

  $ mono tools/nuget-hash-extractor/nuget-hash-extractor.exe nugets guids_for_msbuild > mcs/tools/xbuild/data/deniedAssembliesList.txt

*/

typedef enum {
	SYS_RT_INTEROP_RUNTIME_INFO = 0, //System.Runtime.InteropServices.RuntimeInformation
	SYS_GLOBALIZATION_EXT = 1, //System.Globalization.Extensions
	SYS_IO_COMPRESSION = 2, //System.IO.Compression
	SYS_NET_HTTP = 3, //System.Net.Http
	SYS_TEXT_ENC_CODEPAGES = 4, //System.Text.Encoding.CodePages
	SYS_THREADING_OVERLAPPED = 5, //System.Threading.Overlapped
} IgnoredAssemblyNames;

typedef struct {
	int hash;
	int assembly_name;
	const char guid [40];
} IgnoredAssembly;

typedef struct {
	int assembly_name;
	guint16 major, minor, build, revision;
} IgnoredAssemblyVersion;

static const char *ignored_assemblies_file_names[] = {
	"System.Runtime.InteropServices.RuntimeInformation.dll",
	"System.Globalization.Extensions.dll",
	"System.IO.Compression.dll",
	"System.Net.Http.dll",
	"System.Text.Encoding.CodePages.dll",
	"System.Threading.Overlapped.dll"
};

#define IGNORED_ASSEMBLY(HASH, NAME, GUID, VER_STR) { (int)HASH, NAME, GUID }

static const IgnoredAssembly ignored_assemblies [] = {
	IGNORED_ASSEMBLY (0x10CADA75, SYS_NET_HTTP, "EA2EC6DC-51DD-479C-BFC2-E713FB9E7E47", "4.1.1 net46"),
	IGNORED_ASSEMBLY (0x1136045D, SYS_GLOBALIZATION_EXT, "475DBF02-9F68-44F1-8FB5-C9F69F1BD2B1", "4.0.0 net46"),
	IGNORED_ASSEMBLY (0x1438EAE0, SYS_RT_INTEROP_RUNTIME_INFO, "F580BAAC-12BD-4716-B486-C0A5E3EE6EEA", "15.5.0-preview-20171027-2 net461"),
	IGNORED_ASSEMBLY (0x16BCA997, SYS_RT_INTEROP_RUNTIME_INFO, "CA2D23DE-55E1-45D8-9720-0EBE3EEC1DF2", "2.0.0-preview3-20170622-1 net462"),
	IGNORED_ASSEMBLY (0x17A113, SYS_RT_INTEROP_RUNTIME_INFO, "D87389D8-6E9C-48CF-B128-3637018577AF", "2.1.0-preview1-62414-02 net47"),
	IGNORED_ASSEMBLY (0x1CBD59A2, SYS_IO_COMPRESSION, "44FCA06C-A510-4B3E-BDBF-D08D697EF65A", "4.1.0 net46"),
	IGNORED_ASSEMBLY (0x1EA951BB, SYS_IO_COMPRESSION, "05C07BD4-AFF1-4B12-900B-F0A5EB88DDB4", "2.1.0-preview1-62414-02 net461"),
	IGNORED_ASSEMBLY (0x1FB3F8E8, SYS_GLOBALIZATION_EXT, "B9BA8638-25D2-4A3B-B91F-16B3D3799861", "2.1.100-preview-62617-01 net461"),
	IGNORED_ASSEMBLY (0x2706B80, SYS_NET_HTTP, "8E2F55F3-D010-417B-A742-21386EDDD388", "2.0.0-preview3-20170622-1 net461"),
	IGNORED_ASSEMBLY (0x27726A90, SYS_NET_HTTP, "269B562C-CC15-4736-B1B1-68D4A43CAA98", "4.1.0 net46"),
	IGNORED_ASSEMBLY (0x284ECF63, SYS_THREADING_OVERLAPPED, "E933407E-C846-4413-82C5-09F4BCFA67F1", "2.1.0-preview1-62414-02 net461"),
	IGNORED_ASSEMBLY (0x358C9723, SYS_GLOBALIZATION_EXT, "5FCD54F0-4B97-4259-875D-30E481F02EA2", "4.0.1 net46"),
	IGNORED_ASSEMBLY (0x39C3575D, SYS_GLOBALIZATION_EXT, "3B30D67C-B16B-47BC-B949-9500B5AAAAFB", "2.1.0-preview1-62414-02 net471"),
	IGNORED_ASSEMBLY (0x3E21E75A, SYS_TEXT_ENC_CODEPAGES, "67D3A14A-8F55-4C9F-9699-EDD0876369DA", "4.5.0 net461"),
	IGNORED_ASSEMBLY (0x420963C4, SYS_NET_HTTP, "084B071E-1637-4B3F-B7CD-6CEF28A6E4AE", "4.3.4 net46"),
	IGNORED_ASSEMBLY (0x450A096A, SYS_GLOBALIZATION_EXT, "E9FCFF5B-4DE1-4BDC-9CE8-08C640FC78CC", "4.3.0 net46"),
	IGNORED_ASSEMBLY (0x46A4A1C5, SYS_RT_INTEROP_RUNTIME_INFO, "F13660F8-9D0D-419F-BA4E-315693DD26EA", "4.0.0 net45"),
	IGNORED_ASSEMBLY (0x472FA630, SYS_NET_HTTP, "09D4A140-061C-4884-9B63-22067E841931", "4.3.2 net46"),
	IGNORED_ASSEMBLY (0x4CC79B26, SYS_GLOBALIZATION_EXT, "28006E9A-74DB-45AC-8A8D-030CEBAA272A", "2.0.0-preview3-20170622-1 net461"),
	IGNORED_ASSEMBLY (0x4E906129, SYS_NET_HTTP, "27BBDD4C-EAF0-4A95-B172-EE502D76A725", "2.1.100-preview-62617-01 net461"),
	IGNORED_ASSEMBLY (0x5E393C29, SYS_IO_COMPRESSION, "3A58A219-266B-47C3-8BE8-4E4F394147AB", "4.3.0 net46"),
	IGNORED_ASSEMBLY (0x662BC58F, SYS_IO_COMPRESSION, "C786B28D-0850-4D4C-AED9-FE6B86EE7C31", "2.1.0-preview1-62414-02 net471"),
	IGNORED_ASSEMBLY (0x66CEDA9, SYS_THREADING_OVERLAPPED, "A0439CB6-A5E6-4813-A76C-13F92ADDDED5", "2.1.100-preview-62617-01 net461"),
	IGNORED_ASSEMBLY (0x6AE7C015, SYS_IO_COMPRESSION, "35DD20B5-8766-476B-B5D2-0EA16EF0A946", "2.1.100-preview-62617-01 net461"),
	IGNORED_ASSEMBLY (0x6FE03EE2, SYS_THREADING_OVERLAPPED, "87697E71-D192-4F0B-BAD4-02BBC7793005", "4.3.0 net46"),
	IGNORED_ASSEMBLY (0x74EA304F, SYS_RT_INTEROP_RUNTIME_INFO, "E5AE3324-2100-4F77-9E41-AEEF226C9649", "15.5.0-preview-20171027-2 net47"),
	IGNORED_ASSEMBLY (0x765A8E04, SYS_RT_INTEROP_RUNTIME_INFO, "E46BA45E-6A63-47CD-AF70-2C3016AFF75A", "2.1.100-preview-62617-01 net461"),
	IGNORED_ASSEMBLY (0x786145B9, SYS_RT_INTEROP_RUNTIME_INFO, "0C4BCFB3-F609-4631-93A6-17B19C69D9B6", "2.0.0-preview3-20170622-1 net47"),
	IGNORED_ASSEMBLY (0x79F6E37F, SYS_THREADING_OVERLAPPED, "212BEDF2-E3F5-4D59-8C1A-F4D1C58B46CD", "15.5.0-preview-20171027-2 net461"),
	IGNORED_ASSEMBLY (0x7D927C2A, SYS_THREADING_OVERLAPPED, "FCBD003B-2BB4-4940-BAEF-63AF520C2336", "4.0.1 net46"),
	IGNORED_ASSEMBLY (0x8437178B, SYS_NET_HTTP, "C0E04D9C-70CF-48A6-A179-FBFD8CE69FD0", "4.3.0 net46"),
	IGNORED_ASSEMBLY (0x8BFCB05D, SYS_THREADING_OVERLAPPED, "82D565AC-E41C-4E29-9939-C031C88EDBDD", "2.1.100-preview-62617-01 net471"),
	IGNORED_ASSEMBLY (0x8CCF2469, SYS_RT_INTEROP_RUNTIME_INFO, "9A3724BF-DF8F-4955-8CFA-41D45F11B586", "2.1.0-preview1-62414-02 net462"),
	IGNORED_ASSEMBLY (0x90772EB6, SYS_THREADING_OVERLAPPED, "F4FFC4A6-E694-49D9-81B2-12F2C9A29652", "2.0.0-preview3-20170622-1 net461"),
	IGNORED_ASSEMBLY (0x911D9EC3, SYS_TEXT_ENC_CODEPAGES, "C142254F-DEB5-46A7-AE43-6F10320D1D1F", "4.0.1 net46"),
	IGNORED_ASSEMBLY (0x96B5F0BA, SYS_NET_HTTP, "DB06A592-E332-44A1-8B85-20CAB3C3C147", "2.1.0-preview1-62414-02 net461"),
	IGNORED_ASSEMBLY (0x9DBB28A2, SYS_NET_HTTP, "903A137B-BB3F-464A-94D4-780B89EE5580", "2.1.0-preview1-62414-02 net471"),
	IGNORED_ASSEMBLY (0xA2E8EC53, SYS_RT_INTEROP_RUNTIME_INFO, "6D334D4D-0149-4D07-9DEF-CC52213145CE", "2.0.0-preview3-20170622-1 net461"),
	IGNORED_ASSEMBLY (0xA3BFE786, SYS_RT_INTEROP_RUNTIME_INFO, "33D296D9-EE6D-404E-BF9F-432A429FF5DA", "15.5.0-preview-20171027-2 net462"),
	IGNORED_ASSEMBLY (0xA99E866F, SYS_NET_HTTP, "41ACE450-8F44-455A-97AC-0679E5462071", "2.1.100-preview-62617-01 net471"),
	IGNORED_ASSEMBLY (0xAA21986B, SYS_THREADING_OVERLAPPED, "9F5D4F09-787A-458A-BA08-553AA71470F1", "4.0.0 net46"),
	IGNORED_ASSEMBLY (0xAF2093B8, SYS_TEXT_ENC_CODEPAGES, "D2B4F262-31A4-4E80-9CFB-26A2249A735E", "4.5.1 net461"),
	IGNORED_ASSEMBLY (0xC69BED92, SYS_IO_COMPRESSION, "33AD8174-7781-46FA-A110-33821CCBE810", "15.5.0-preview-20171027-2 net461"),
	IGNORED_ASSEMBLY (0xC8D00759, SYS_IO_COMPRESSION, "1332CE2F-1517-4BD7-93FD-7D4BCFBAED66", "2.0.0-preview3-20170622-1 net461"),
	IGNORED_ASSEMBLY (0xCA951D5B, SYS_RT_INTEROP_RUNTIME_INFO, "1F37581E-4589-4C71-A465-05C6B9AE966E", "2.1.0-preview1-62414-02 net461"),
	IGNORED_ASSEMBLY (0xD00F7419, SYS_THREADING_OVERLAPPED, "3336A2A3-1772-4EF9-A74B-AFDC80A8B21E", "2.1.0-preview1-62414-02 net471"),
	IGNORED_ASSEMBLY (0xD07383BB, SYS_RT_INTEROP_RUNTIME_INFO, "DD91439F-3167-478E-BD2C-BF9C036A1395", "4.3.0 net45"),
	IGNORED_ASSEMBLY (0xD08A991A, SYS_NET_HTTP, "82C79759-CB3C-4EB6-A17C-BDE85AF00A9B", "15.5.0-preview-20171027-2 net461"),
	IGNORED_ASSEMBLY (0xD3ABE53A, SYS_RT_INTEROP_RUNTIME_INFO, "488CE209-4E5D-40E7-BE8C-F81F2B99F13A", "2.1.100-preview-62617-01 net462"),
	IGNORED_ASSEMBLY (0xDB9397A9, SYS_NET_HTTP, "56203551-6937-47C1-9246-346A733913EE", "4.3.3 net46"),
	IGNORED_ASSEMBLY (0xE16ECCCD, SYS_GLOBALIZATION_EXT, "1A2B9B2A-02F5-4C78-AB0C-7C6D2795CE2B", "2.1.100-preview-62617-01 net471"),
	IGNORED_ASSEMBLY (0xE4016B17, SYS_GLOBALIZATION_EXT, "50F4163A-D692-452F-90ED-2F8024BB5319", "15.5.0-preview-20171027-2 net461"),
	IGNORED_ASSEMBLY (0xE758DAD4, SYS_IO_COMPRESSION, "8DBD1669-97BC-4190-9BD8-738561193741", "2.1.100-preview-62617-01 net471"),
	IGNORED_ASSEMBLY (0xF6D18A2E, SYS_TEXT_ENC_CODEPAGES, "F5CCCBEC-E1AD-4DBB-9B44-9B42C86B94B8", "4.4.0 net461"),
	IGNORED_ASSEMBLY (0xF9D06E1E, SYS_GLOBALIZATION_EXT, "FC1439FC-C1B8-4DB1-914D-165CCFA77002", "2.1.0-preview1-62414-02 net461"),
	IGNORED_ASSEMBLY (0xFA686A38, SYS_TEXT_ENC_CODEPAGES, "FD178CD4-EF4F-44D5-9C3F-812B1E25126B", "4.3.0 net46"),
	IGNORED_ASSEMBLY (0xFAFDA422, SYS_NET_HTTP, "817F01C3-4011-477D-890A-98232B85553D", "4.3.1 net46"),
	IGNORED_ASSEMBLY (0xFC67D3A7, SYS_RT_INTEROP_RUNTIME_INFO, "FD6C8616-C1D8-43F9-AC17-A1C48A45FDA2", "2.1.100-preview-62617-01 net47")
};


static const char *ignored_assemblies_names[] = {
	"System.Runtime.InteropServices.RuntimeInformation",
	"System.Globalization.Extensions",
	"System.IO.Compression",
	"System.Net.Http",
	"System.Text.Encoding.CodePages",
	"System.Threading.Overlapped"
};

#define IGNORED_ASM_VER(NAME, MAJOR, MINOR, BUILD, REVISION) { NAME, MAJOR, MINOR, BUILD, REVISION }

static const IgnoredAssemblyVersion ignored_assembly_versions [] = {
	IGNORED_ASM_VER (SYS_GLOBALIZATION_EXT, 4, 0, 0, 0),
	IGNORED_ASM_VER (SYS_GLOBALIZATION_EXT, 4, 0, 1, 0),
	IGNORED_ASM_VER (SYS_GLOBALIZATION_EXT, 4, 0, 2, 0),
	IGNORED_ASM_VER (SYS_GLOBALIZATION_EXT, 4, 1, 0, 0),
	IGNORED_ASM_VER (SYS_IO_COMPRESSION, 4, 1, 0, 0),
	IGNORED_ASM_VER (SYS_IO_COMPRESSION, 4, 1, 2, 0),
	IGNORED_ASM_VER (SYS_IO_COMPRESSION, 4, 2, 0, 0),
	IGNORED_ASM_VER (SYS_NET_HTTP, 4, 1, 0, 0),
	IGNORED_ASM_VER (SYS_NET_HTTP, 4, 1, 0, 1),
	IGNORED_ASM_VER (SYS_NET_HTTP, 4, 1, 1, 0),
	IGNORED_ASM_VER (SYS_NET_HTTP, 4, 1, 1, 1),
	IGNORED_ASM_VER (SYS_NET_HTTP, 4, 1, 1, 2),
	IGNORED_ASM_VER (SYS_NET_HTTP, 4, 1, 1, 3),
	IGNORED_ASM_VER (SYS_NET_HTTP, 4, 2, 0, 0),
	IGNORED_ASM_VER (SYS_RT_INTEROP_RUNTIME_INFO, 4, 0, 0, 0),
	IGNORED_ASM_VER (SYS_RT_INTEROP_RUNTIME_INFO, 4, 0, 1, 0),
	IGNORED_ASM_VER (SYS_RT_INTEROP_RUNTIME_INFO, 4, 0, 2, 0),
	IGNORED_ASM_VER (SYS_TEXT_ENC_CODEPAGES, 4, 0, 1, 0),
	IGNORED_ASM_VER (SYS_TEXT_ENC_CODEPAGES, 4, 0, 2, 0),
	IGNORED_ASM_VER (SYS_TEXT_ENC_CODEPAGES, 4, 1, 0, 0),
	IGNORED_ASM_VER (SYS_TEXT_ENC_CODEPAGES, 4, 1, 1, 0),
	IGNORED_ASM_VER (SYS_THREADING_OVERLAPPED, 4, 0, 0, 0),
	IGNORED_ASM_VER (SYS_THREADING_OVERLAPPED, 4, 0, 1, 0),
	IGNORED_ASM_VER (SYS_THREADING_OVERLAPPED, 4, 0, 2, 0),
	IGNORED_ASM_VER (SYS_THREADING_OVERLAPPED, 4, 1, 0, 0)
};

gboolean
mono_assembly_is_problematic_version (const char *name, guint16 major, guint16 minor, guint16 build, guint16 revision)
{
	for (int i = 0; i < G_N_ELEMENTS (ignored_assembly_versions); ++i) {
		if (ignored_assembly_versions [i].major != major ||
			ignored_assembly_versions [i].minor != minor ||
			ignored_assembly_versions [i].build != build ||
			ignored_assembly_versions [i].revision != revision)
				continue;
		if (!strcmp (ignored_assemblies_names [ignored_assembly_versions [i].assembly_name], name))
			return TRUE;
	}
	return FALSE;
}

/*
Equivalent C# code:
	static void Main  () {
		string str = "...";
		int h = 5381;
        for (int i = 0;  i < str.Length; ++i)
            h = ((h << 5) + h) ^ str[i];

		Console.WriteLine ("{0:X}", h);
	}
*/
static int
hash_guid (const char *str)
{
	int h = 5381;
    while (*str) {
        h = ((h << 5) + h) ^ *str;
		++str;
	}

	return h;
}

gboolean
mono_is_problematic_image (MonoImage *image)
{
	int h = hash_guid (image->guid);

	//TODO make this more cache effiecient.
	// Either sort by hash and bseach or use SoA and make the linear search more cache efficient.
	for (int i = 0; i < G_N_ELEMENTS (ignored_assemblies); ++i) {
		if (ignored_assemblies [i].hash == h && !strcmp (image->guid, ignored_assemblies [i].guid)) {
			const char *needle = ignored_assemblies_file_names [ignored_assemblies [i].assembly_name];
			size_t needle_len = strlen (needle);
			size_t asm_len = strlen (image->name);
			if (asm_len > needle_len && !g_ascii_strcasecmp (image->name + (asm_len - needle_len), needle))
				return TRUE;
		}
	}
	return FALSE;
}

static MonoImage *
do_mono_image_load (MonoImage *image, MonoImageOpenStatus *status,
		    gboolean care_about_cli, gboolean care_about_pecoff)
{
	ERROR_DECL (error);
	MonoCLIImageInfo *iinfo;
	MonoDotNetHeader *header;
	GSList *l;

	MONO_PROFILER_RAISE (image_loading, (image));

	mono_image_init (image);

	iinfo = image->image_info;
	header = &iinfo->cli_header;

	if (!image->metadata_only) {
		for (l = image_loaders; l; l = l->next) {
			MonoImageLoader *loader = (MonoImageLoader *)l->data;
			if (loader->match (image)) {
				image->loader = loader;
				break;
			}
		}
		if (!image->loader) {
			if (status)
				*status = MONO_IMAGE_IMAGE_INVALID;
			goto invalid_image;
		}

		if (status)
			*status = MONO_IMAGE_IMAGE_INVALID;

		if (care_about_pecoff == FALSE)
			goto done;

		if (image->loader == &pe_loader && !mono_verifier_verify_pe_data (image, error))
			goto invalid_image;

		if (!mono_image_load_pe_data (image))
			goto invalid_image;
	} else {
		image->loader = (MonoImageLoader*)&pe_loader;
	}

	if (care_about_cli == FALSE) {
		goto done;
	}

	if (image->loader == &pe_loader && !image->metadata_only && !mono_verifier_verify_cli_data (image, error))
		goto invalid_image;

	if (!mono_image_load_cli_data (image))
		goto invalid_image;

	if (!image->ref_only && mono_is_problematic_image (image)) {
		if (image->load_from_context) {
			mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_ASSEMBLY, "Loading problematic image %s", image->name);
		} else {
			mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_ASSEMBLY, "Denying load of problematic image %s", image->name);
			if (status)
				*status = MONO_IMAGE_IMAGE_INVALID;
			goto invalid_image;
		}
	}

	if (image->loader == &pe_loader && !image->metadata_only && !mono_verifier_verify_table_data (image, error))
		goto invalid_image;

	mono_image_load_names (image);

	mono_image_load_time_date_stamp (image);

	load_modules (image);

done:
	MONO_PROFILER_RAISE (image_loaded, (image));
	if (status)
		*status = MONO_IMAGE_OK;

	return image;

invalid_image:
	if (!is_ok (error)) {
		mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_ASSEMBLY, "Could not load image %s due to %s", image->name, mono_error_get_message (error));
		mono_error_cleanup (error);
	}
	MONO_PROFILER_RAISE (image_failed, (image));
	mono_image_close (image);
	return NULL;
}

static gboolean
mono_image_storage_trypublish (MonoImageStorage *candidate, MonoImageStorage **out_storage)
{
	gboolean result;
	mono_images_storage_lock ();
	MonoImageStorage *val = (MonoImageStorage *)g_hash_table_lookup (images_storage_hash, candidate->key);
	if (val) {
		mono_refcount_inc (val);
		*out_storage = val;
		result = FALSE;
	} else {
		g_hash_table_insert (images_storage_hash, candidate->key, candidate);
		result = TRUE;
	}
	mono_images_storage_unlock ();
	return result;
}

static void
mono_image_storage_unpublish (MonoImageStorage *storage)
{
	mono_images_storage_lock ();
	g_assert (storage->ref.ref == 0);

	MonoImageStorage *published = (MonoImageStorage *)g_hash_table_lookup (images_storage_hash, storage->key);
	if (published == storage) {
		g_hash_table_remove (images_storage_hash, storage->key);
	}

	mono_images_storage_unlock ();
}

static gboolean
mono_image_storage_tryaddref (const char *key, MonoImageStorage **found)
{
	gboolean result = FALSE;
	mono_images_storage_lock ();
	MonoImageStorage *val = (MonoImageStorage *)g_hash_table_lookup (images_storage_hash, key);
	if (val) {
		mono_refcount_inc (val);
		*found = val;
		result = TRUE;
	}
	mono_images_storage_unlock ();
	return result;
}

static void
mono_image_storage_dtor (gpointer self)
{
	MonoImageStorage *storage = (MonoImageStorage *)self;

	mono_image_storage_unpublish (storage);
	
#ifdef HOST_WIN32
	if (storage->is_module_handle && !storage->has_entry_point) {
		mono_images_lock ();
		FreeLibrary ((HMODULE) storage->raw_data);
		mono_images_unlock ();
	}
#endif

	if (storage->raw_buffer_used) {
		if (storage->raw_data != NULL) {
#ifndef HOST_WIN32
			if (storage->fileio_used)
				mono_file_unmap_fileio (storage->raw_data, storage->raw_data_handle);
			else
#endif
				mono_file_unmap (storage->raw_data, storage->raw_data_handle);
		}
	}
	if (storage->raw_data_allocated) {
		g_free (storage->raw_data);
	}

	g_free (storage->key);

	g_free (storage);
}

static void
mono_image_storage_close (MonoImageStorage *storage)
{
	mono_refcount_dec (storage);
}

static gboolean
mono_image_init_raw_data (MonoImage *image, const MonoImageStorage *storage)
{
	if (!storage)
		return FALSE;
	image->raw_data = storage->raw_data;
	image->raw_data_len = storage->raw_data_len;
	return TRUE;
}

static MonoImageStorage *
mono_image_storage_open (const char *fname)
{
	char *key = NULL;

	key = mono_path_resolve_symlinks (fname);
	MonoImageStorage *published_storage = NULL;
	if (mono_image_storage_tryaddref (key, &published_storage)) {
		g_free (key);
		return published_storage;
	}
	
	MonoFileMap *filed;
	if ((filed = mono_file_map_open (fname)) == NULL){
		if (IS_PORTABILITY_SET) {
			gchar *ffname = mono_portability_find_file (fname, TRUE);
			if (ffname) {
				filed = mono_file_map_open (ffname);
				g_free (ffname);
			}
		}

		if (filed == NULL) {
			g_free (key);
			return NULL;
		}
	}

	MonoImageStorage *storage = g_new0 (MonoImageStorage, 1);
	mono_refcount_init (storage, mono_image_storage_dtor);
	storage->raw_buffer_used = TRUE;
	storage->raw_data_len = mono_file_map_size (filed);
	storage->raw_data = (char*)mono_file_map (storage->raw_data_len, MONO_MMAP_READ|MONO_MMAP_PRIVATE, mono_file_map_fd (filed), 0, &storage->raw_data_handle);
#if defined(HAVE_MMAP) && !defined (HOST_WIN32)
	if (!storage->raw_data) {
		storage->fileio_used = TRUE;
		storage->raw_data = (char *)mono_file_map_fileio (storage->raw_data_len, MONO_MMAP_READ|MONO_MMAP_PRIVATE, mono_file_map_fd (filed), 0, &storage->raw_data_handle);
	}
#endif
	mono_file_map_close (filed);

	storage->key = key;
	
	MonoImageStorage *other_storage = NULL;
	if (!mono_image_storage_trypublish (storage, &other_storage)) {
		mono_image_storage_close (storage);
		storage = other_storage;
	}
	return storage;
}

static MonoImageStorage *
mono_image_storage_new_raw_data (char *datac, guint32 data_len, gboolean raw_data_allocated, const char *name)
{
	char *key = (name == NULL) ? g_strdup_printf ("data-%p", datac) : g_strdup (name);
	MonoImageStorage *published_storage = NULL;
	if (mono_image_storage_tryaddref (key, &published_storage)) {
		g_free (key);
		return published_storage;
	}

	MonoImageStorage *storage = g_new0 (MonoImageStorage, 1);
	mono_refcount_init (storage, mono_image_storage_dtor);

	storage->raw_data = datac;
	storage->raw_data_len = data_len;
	storage->raw_data_allocated = raw_data_allocated;

	storage->key = key;
	MonoImageStorage *other_storage = NULL;
	if (!mono_image_storage_trypublish (storage, &other_storage)) {
		mono_image_storage_close (storage);
		storage = other_storage;
	}
	return storage;
}

static MonoImage *
do_mono_image_open (const char *fname, MonoImageOpenStatus *status,
					gboolean care_about_cli, gboolean care_about_pecoff, gboolean refonly, gboolean metadata_only, gboolean load_from_context)
{
	MonoCLIImageInfo *iinfo;
	MonoImage *image;

	MonoImageStorage *storage = mono_image_storage_open (fname);

	if (!storage) {
		if (status)
			*status = MONO_IMAGE_ERROR_ERRNO;
		return NULL;
	}

	image = g_new0 (MonoImage, 1);
	image->storage = storage;
	mono_image_init_raw_data (image, storage);
	if (!image->raw_data) {
		mono_image_storage_close (image->storage);
		g_free (image);
		if (status)
			*status = MONO_IMAGE_IMAGE_INVALID;
		return NULL;
	}
	iinfo = g_new0 (MonoCLIImageInfo, 1);
	image->image_info = iinfo;
	image->name = mono_path_resolve_symlinks (fname);
	image->filename = g_strdup (image->name);
	image->ref_only = refonly;
	image->metadata_only = metadata_only;
	image->load_from_context = load_from_context;
	image->ref_count = 1;
	/* if MONO_SECURITY_MODE_CORE_CLR is set then determine if this image is platform code */
	image->core_clr_platform_code = mono_security_core_clr_determine_platform_image (image);

	return do_mono_image_load (image, status, care_about_cli, care_about_pecoff);
}

/**
 * mono_image_loaded_full:
 * \param name path or assembly name of the image to load
 * \param refonly Check with respect to reflection-only loads?
 *
 * This routine verifies that the given image is loaded.
 * It checks either reflection-only loads only, or normal loads only, as specified by parameter.
 *
 * \returns the loaded \c MonoImage, or NULL on failure.
 */
MonoImage *
mono_image_loaded_full (const char *name, gboolean refonly)
{
	MonoImage *result;
	MONO_ENTER_GC_UNSAFE;
	result = mono_image_loaded_internal (name, refonly);
	MONO_EXIT_GC_UNSAFE;
	return result;
}

/**
 * mono_image_loaded_internal:
 * \param name path or assembly name of the image to load
 * \param refonly Check with respect to reflection-only loads?
 *
 * This routine verifies that the given image is loaded.
 * It checks either reflection-only loads only, or normal loads only, as specified by parameter.
 *
 * \returns the loaded \c MonoImage, or NULL on failure.
 */
MonoImage *
mono_image_loaded_internal (const char *name, gboolean refonly)
{
	MonoImage *res;

	mono_images_lock ();
	res = (MonoImage *)g_hash_table_lookup (get_loaded_images_hash (refonly), name);
	if (!res)
		res = (MonoImage *)g_hash_table_lookup (get_loaded_images_by_name_hash (refonly), name);
	mono_images_unlock ();

	return res;
}

/**
 * mono_image_loaded:
 * \param name path or assembly name of the image to load
 * This routine verifies that the given image is loaded. Reflection-only loads do not count.
 * \returns the loaded \c MonoImage, or NULL on failure.
 */
MonoImage *
mono_image_loaded (const char *name)
{
	MonoImage *result;
	MONO_ENTER_GC_UNSAFE;
	result = mono_image_loaded_internal (name, FALSE);
	MONO_EXIT_GC_UNSAFE;
	return result;
}

typedef struct {
	MonoImage *res;
	const char* guid;
} GuidData;

static void
find_by_guid (gpointer key, gpointer val, gpointer user_data)
{
	GuidData *data = (GuidData *)user_data;
	MonoImage *image;

	if (data->res)
		return;
	image = (MonoImage *)val;
	if (strcmp (data->guid, mono_image_get_guid (image)) == 0)
		data->res = image;
}

/**
 * mono_image_loaded_by_guid_full:
 */
MonoImage *
mono_image_loaded_by_guid_full (const char *guid, gboolean refonly)
{
	GuidData data;
	GHashTable *loaded_images = get_loaded_images_hash (refonly);
	data.res = NULL;
	data.guid = guid;

	mono_images_lock ();
	g_hash_table_foreach (loaded_images, find_by_guid, &data);
	mono_images_unlock ();
	return data.res;
}

/**
 * mono_image_loaded_by_guid:
 */
MonoImage *
mono_image_loaded_by_guid (const char *guid)
{
	return mono_image_loaded_by_guid_full (guid, FALSE);
}

static MonoImage *
register_image (MonoImage *image)
{
	MonoImage *image2;
	GHashTable *loaded_images = get_loaded_images_hash (image->ref_only);

	mono_images_lock ();
	image2 = (MonoImage *)g_hash_table_lookup (loaded_images, image->name);

	if (image2) {
		/* Somebody else beat us to it */
		mono_image_addref (image2);
		mono_images_unlock ();
		mono_image_close (image);
		return image2;
	}

	GHashTable *loaded_images_by_name = get_loaded_images_by_name_hash (image->ref_only);
	g_hash_table_insert (loaded_images, image->name, image);
	if (image->assembly_name && (g_hash_table_lookup (loaded_images_by_name, image->assembly_name) == NULL))
		g_hash_table_insert (loaded_images_by_name, (char *) image->assembly_name, image);
	mono_images_unlock ();

	if (mono_is_problematic_image (image)) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_ASSEMBLY, "Registering %s, problematic image '%s'", image->ref_only ? "REFONLY" : "default", image->name);
	}
	return image;
}

MonoImage *
mono_image_open_from_data_internal (char *data, guint32 data_len, gboolean need_copy, MonoImageOpenStatus *status, gboolean refonly, gboolean metadata_only, const char *name)
{
	MonoCLIImageInfo *iinfo;
	MonoImage *image;
	char *datac;

	if (!data || !data_len) {
		if (status)
			*status = MONO_IMAGE_IMAGE_INVALID;
		return NULL;
	}
	datac = data;
	if (need_copy) {
		datac = (char *)g_try_malloc (data_len);
		if (!datac) {
			if (status)
				*status = MONO_IMAGE_ERROR_ERRNO;
			return NULL;
		}
		memcpy (datac, data, data_len);
	}

	MonoImageStorage *storage = mono_image_storage_new_raw_data (datac, data_len, need_copy, name);
	image = g_new0 (MonoImage, 1);
	image->storage = storage;
	mono_image_init_raw_data (image, storage);
	image->name = (name == NULL) ? g_strdup_printf ("data-%p", datac) : g_strdup(name);
	iinfo = g_new0 (MonoCLIImageInfo, 1);
	image->image_info = iinfo;
	image->ref_only = refonly;
	image->metadata_only = metadata_only;
	image->ref_count = 1;

	image = do_mono_image_load (image, status, TRUE, TRUE);
	if (image == NULL)
		return NULL;

	return register_image (image);
}

/**
 * mono_image_open_from_data_with_name:
 */
MonoImage *
mono_image_open_from_data_with_name (char *data, guint32 data_len, gboolean need_copy, MonoImageOpenStatus *status, gboolean refonly, const char *name)
{
	MonoImage *result;
	MONO_ENTER_GC_UNSAFE;
	result = mono_image_open_from_data_internal (data, data_len, need_copy, status, refonly, FALSE, name);
	MONO_EXIT_GC_UNSAFE;
	return result;
}

/**
 * mono_image_open_from_data_full:
 */
MonoImage *
mono_image_open_from_data_full (char *data, guint32 data_len, gboolean need_copy, MonoImageOpenStatus *status, gboolean refonly)
{
	MonoImage *result;
	MONO_ENTER_GC_UNSAFE;
	result = mono_image_open_from_data_internal (data, data_len, need_copy, status, refonly, FALSE, NULL);
	MONO_EXIT_GC_UNSAFE;
	return result;
}

/**
 * mono_image_open_from_data:
 */
MonoImage *
mono_image_open_from_data (char *data, guint32 data_len, gboolean need_copy, MonoImageOpenStatus *status)
{
	MonoImage *result;
	MONO_ENTER_GC_UNSAFE;
	result = mono_image_open_from_data_internal (data, data_len, need_copy, status, FALSE, FALSE, NULL);
	MONO_EXIT_GC_UNSAFE;
	return result;
}

#ifdef HOST_WIN32
static MonoImageStorage *
mono_image_storage_open_from_module_handle (HMODULE module_handle, const char *fname, gboolean has_entry_point)
{
	char *key = g_strdup (fname);
	MonoImageStorage *published_storage = NULL;
	if (mono_image_storage_tryaddref (key, &published_storage)) {
		g_free (key);
		return published_storage;
	}

	MonoImageStorage *storage = g_new0 (MonoImageStorage, 1);
	mono_refcount_init (storage, mono_image_storage_dtor);
	storage->raw_data = (char*) module_handle;
	storage->is_module_handle = TRUE;
	storage->has_entry_point = has_entry_point;

	storage->key = key;

	MonoImageStorage *other_storage = NULL;
	if (!mono_image_storage_trypublish (storage, &other_storage)) {
		mono_image_storage_close (storage);
		storage = other_storage;
	}
	return storage;
}

/* fname is not duplicated. */
MonoImage*
mono_image_open_from_module_handle (HMODULE module_handle, char* fname, gboolean has_entry_point, MonoImageOpenStatus* status)
{
	MonoImage* image;
	MonoCLIImageInfo* iinfo;

	MonoImageStorage *storage = mono_image_storage_open_from_module_handle (module_handle, fname, has_entry_point);
	image = g_new0 (MonoImage, 1);
	image->storage = storage;
	mono_image_init_raw_data (image, storage);
	iinfo = g_new0 (MonoCLIImageInfo, 1);
	image->image_info = iinfo;
	image->name = fname;
	image->ref_count = has_entry_point ? 0 : 1;

	image = do_mono_image_load (image, status, TRUE, TRUE);
	if (image == NULL)
		return NULL;

	return register_image (image);
}
#endif

/**
 * mono_image_open_full:
 */
MonoImage *
mono_image_open_full (const char *fname, MonoImageOpenStatus *status, gboolean refonly)
{
	return mono_image_open_a_lot (fname, status, refonly, FALSE);
}

static MonoImage *
mono_image_open_a_lot_parameterized (const char *fname, MonoImageOpenStatus *status, gboolean refonly, gboolean load_from_context, gboolean *problematic)
{
	MonoImage *image;
	GHashTable *loaded_images = get_loaded_images_hash (refonly);
	char *absfname;
	
	g_return_val_if_fail (fname != NULL, NULL);
	
#ifdef HOST_WIN32
	// Win32 path: If we are running with mixed-mode assemblies enabled (ie have loaded mscoree.dll),
	// then assemblies need to be loaded with LoadLibrary:
	if (!refonly && coree_module_handle) {
		HMODULE module_handle;
		gunichar2 *fname_utf16;
		DWORD last_error;

		absfname = mono_path_resolve_symlinks (fname);
		fname_utf16 = NULL;

		/* There is little overhead because the OS loader lock is held by LoadLibrary. */
		mono_images_lock ();
		image = (MonoImage*)g_hash_table_lookup (loaded_images, absfname);
		if (image) { // Image already loaded
			if (!load_from_context && mono_is_problematic_image (image)) {
				// If we previously loaded a problematic image, don't
				// return it if we're not in LoadFrom context.
				//
				// Note: this has an interaction with
				//  mono_problematic_image_reprobe - at that point we
				//  have a problematic image opened, but we don't want
				//  to see it again when we go searching for an image
				//  to load.
				mono_images_unlock ();
				return NULL;
			}
			g_assert (m_image_is_module_handle (image));
			if (m_image_has_entry_point (image) && image->ref_count == 0) {
				/* Increment reference count on images loaded outside of the runtime. */
				fname_utf16 = g_utf8_to_utf16 (absfname, -1, NULL, NULL, NULL);
				/* The image is already loaded because _CorDllMain removes images from the hash. */
				module_handle = LoadLibrary (fname_utf16);
				g_assert (module_handle == (HMODULE) image->raw_data);
			}
			mono_image_addref (image);
			mono_images_unlock ();
			if (fname_utf16)
				g_free (fname_utf16);
			g_free (absfname);
			return image;
		}

		// Image not loaded, load it now
		fname_utf16 = g_utf8_to_utf16 (absfname, -1, NULL, NULL, NULL);
		module_handle = MonoLoadImage (fname_utf16);
		if (status && module_handle == NULL)
			last_error = mono_w32error_get_last ();

		/* mono_image_open_from_module_handle is called by _CorDllMain. */
		image = (MonoImage*)g_hash_table_lookup (loaded_images, absfname);
		if (image)
			mono_image_addref (image);
		mono_images_unlock ();

		g_free (fname_utf16);

		if (module_handle == NULL) {
			g_assert (!image);
			g_free (absfname);
			if (status) {
				if (last_error == ERROR_BAD_EXE_FORMAT || last_error == STATUS_INVALID_IMAGE_FORMAT) {
					if (status)
						*status = MONO_IMAGE_IMAGE_INVALID;
				} else {
					if (last_error == ERROR_FILE_NOT_FOUND || last_error == ERROR_PATH_NOT_FOUND)
						mono_set_errno (ENOENT);
					else
						mono_set_errno (0);
				}
			}
			return NULL;
		}

		if (image) {
			g_assert (m_image_is_module_handle (image));
			g_assert (m_image_has_entry_point (image));
			g_free (absfname);
			return image;
		}

		return mono_image_open_from_module_handle (module_handle, absfname, FALSE, status);
	}
#endif

	absfname = mono_path_resolve_symlinks (fname);

	/*
	 * The easiest solution would be to do all the loading inside the mutex,
	 * but that would lead to scalability problems. So we let the loading
	 * happen outside the mutex, and if multiple threads happen to load
	 * the same image, we discard all but the first copy.
	 */
	mono_images_lock ();
	image = (MonoImage *)g_hash_table_lookup (loaded_images, absfname);
	g_free (absfname);

	if (image) { // Image already loaded
		if (!refonly && !load_from_context && mono_is_problematic_image (image)) {
			// If we previously loaded a problematic image, don't
			// return it if we're not in LoadFrom context.
			//
			// Note: this has an interaction with
			//  mono_problematic_image_reprobe - at that point we
			//  have a problematic image opened, but we don't want
			//  to see it again when we go searching for an image
			//  to load.
			mono_images_unlock ();
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_ASSEMBLY, "Not returning problematic image '%s' refcount=%d", image->name, image->ref_count);
			if (problematic)
				*problematic = TRUE;
			return NULL;
		}
		mono_image_addref (image);
		mono_images_unlock ();
		return image;
	}
	mono_images_unlock ();

	// Image not loaded, load it now
	image = do_mono_image_open (fname, status, TRUE, TRUE, refonly, FALSE, load_from_context);
	if (problematic)
		*problematic = TRUE;
	if (image == NULL)
		return NULL;

	return register_image (image);
}

MonoImage *
mono_image_open_a_lot (const char *fname, MonoImageOpenStatus *status, gboolean refonly, gboolean load_from_context)
{
	return mono_image_open_a_lot_parameterized (fname, status, refonly, load_from_context, NULL);
}

gboolean
mono_is_problematic_file (const char *fname)
{
	MonoImageOpenStatus status;
	gboolean problematic = FALSE;

	MonoImage *opened = mono_image_open_a_lot_parameterized (fname, &status, FALSE, FALSE, &problematic);
	if (opened)
		mono_image_close (opened);

	return problematic;
}


/**
 * mono_image_open:
 * \param fname filename that points to the module we want to open
 * \param status An error condition is returned in this field
 * \returns An open image of type \c MonoImage or NULL on error. 
 * The caller holds a temporary reference to the returned image which should be cleared 
 * when no longer needed by calling \c mono_image_close.
 * if NULL, then check the value of \p status for details on the error
 */
MonoImage *
mono_image_open (const char *fname, MonoImageOpenStatus *status)
{
	return mono_image_open_full (fname, status, FALSE);
}

/**
 * mono_pe_file_open:
 * \param fname filename that points to the module we want to open
 * \param status An error condition is returned in this field
 * \returns An open image of type \c MonoImage or NULL on error.  if
 * NULL, then check the value of \p status for details on the error.
 * This variant for \c mono_image_open DOES NOT SET UP CLI METADATA.
 * It's just a PE file loader, used for \c FileVersionInfo.  It also does
 * not use the image cache.
 */
MonoImage *
mono_pe_file_open (const char *fname, MonoImageOpenStatus *status)
{
	g_return_val_if_fail (fname != NULL, NULL);
	
	return do_mono_image_open (fname, status, FALSE, TRUE, FALSE, FALSE, FALSE);
}

/**
 * mono_image_open_raw
 * \param fname filename that points to the module we want to open
 * \param status An error condition is returned in this field
 * \returns an image without loading neither pe or cli data.
 * Use mono_image_load_pe_data and mono_image_load_cli_data to load them.  
 */
MonoImage *
mono_image_open_raw (const char *fname, MonoImageOpenStatus *status)
{
	g_return_val_if_fail (fname != NULL, NULL);
	
	return do_mono_image_open (fname, status, FALSE, FALSE, FALSE, FALSE, FALSE);
}

/*
 * mono_image_open_metadata_only:
 *
 *   Open an image which contains metadata only without a PE header.
 */
MonoImage *
mono_image_open_metadata_only (const char *fname, MonoImageOpenStatus *status)
{
	return do_mono_image_open (fname, status, TRUE, TRUE, FALSE, TRUE, FALSE);
}

/**
 * mono_image_fixup_vtable:
 */
void
mono_image_fixup_vtable (MonoImage *image)
{
#ifdef HOST_WIN32
	MonoCLIImageInfo *iinfo;
	MonoPEDirEntry *de;
	MonoVTableFixup *vtfixup;
	int count;
	gpointer slot;
	guint16 slot_type;
	int slot_count;

	g_assert (m_image_is_module_handle (image));

	iinfo = image->image_info;
	de = &iinfo->cli_cli_header.ch_vtable_fixups;
	if (!de->rva || !de->size)
		return;
	vtfixup = (MonoVTableFixup*) mono_image_rva_map (image, de->rva);
	if (!vtfixup)
		return;
	
	count = de->size / sizeof (MonoVTableFixup);
	while (count--) {
		if (!vtfixup->rva || !vtfixup->count)
			continue;

		slot = mono_image_rva_map (image, vtfixup->rva);
		g_assert (slot);
		slot_type = vtfixup->type;
		slot_count = vtfixup->count;
		if (slot_type & VTFIXUP_TYPE_32BIT)
			while (slot_count--) {
				*((guint32*) slot) = (guint32)(gsize)mono_marshal_get_vtfixup_ftnptr (image, *((guint32*) slot), slot_type);
				slot = ((guint32*) slot) + 1;
			}
		else if (slot_type & VTFIXUP_TYPE_64BIT)
			while (slot_count--) {
				*((guint64*) slot) = (guint64) mono_marshal_get_vtfixup_ftnptr (image, *((guint64*) slot), slot_type);
				slot = ((guint32*) slot) + 1;
			}
		else
			g_assert_not_reached();

		vtfixup++;
	}
#else
	g_assert_not_reached();
#endif
}

static void
free_hash_table (gpointer key, gpointer val, gpointer user_data)
{
	g_hash_table_destroy ((GHashTable*)val);
}

/*
static void
free_mr_signatures (gpointer key, gpointer val, gpointer user_data)
{
	mono_metadata_free_method_signature ((MonoMethodSignature*)val);
}
*/

static void
free_array_cache_entry (gpointer key, gpointer val, gpointer user_data)
{
	g_slist_free ((GSList*)val);
}

/**
 * mono_image_addref:
 * \param image The image file we wish to add a reference to
 * Increases the reference count of an image.
 */
void
mono_image_addref (MonoImage *image)
{
	mono_atomic_inc_i32 (&image->ref_count);
}	

void
mono_dynamic_stream_reset (MonoDynamicStream* stream)
{
	stream->alloc_size = stream->index = stream->offset = 0;
	g_free (stream->data);
	stream->data = NULL;
	if (stream->hash) {
		g_hash_table_destroy (stream->hash);
		stream->hash = NULL;
	}
}

static inline void
free_hash (GHashTable *hash)
{
	if (hash)
		g_hash_table_destroy (hash);
}

void
mono_wrapper_caches_free (MonoWrapperCaches *cache)
{
	free_hash (cache->delegate_invoke_cache);
	free_hash (cache->delegate_begin_invoke_cache);
	free_hash (cache->delegate_end_invoke_cache);
	free_hash (cache->runtime_invoke_signature_cache);
	
	free_hash (cache->delegate_abstract_invoke_cache);

	free_hash (cache->runtime_invoke_method_cache);
	free_hash (cache->managed_wrapper_cache);

	free_hash (cache->native_wrapper_cache);
	free_hash (cache->native_wrapper_aot_cache);
	free_hash (cache->native_wrapper_check_cache);
	free_hash (cache->native_wrapper_aot_check_cache);

	free_hash (cache->native_func_wrapper_aot_cache);
	free_hash (cache->remoting_invoke_cache);
	free_hash (cache->synchronized_cache);
	free_hash (cache->unbox_wrapper_cache);
	free_hash (cache->cominterop_invoke_cache);
	free_hash (cache->cominterop_wrapper_cache);
	free_hash (cache->thunk_invoke_cache);
}

static void
mono_image_close_except_pools_all (MonoImage**images, int image_count)
{
	for (int i = 0; i < image_count; ++i) {
		if (images [i]) {
			if (!mono_image_close_except_pools (images [i]))
				images [i] = NULL;
		}
	}
}

/*
 * Returns whether mono_image_close_finish() must be called as well.
 * We must unload images in two steps because clearing the domain in
 * SGen requires the class metadata to be intact, but we need to free
 * the mono_g_hash_tables in case a collection occurs during domain
 * unloading and the roots would trip up the GC.
 */
gboolean
mono_image_close_except_pools (MonoImage *image)
{
	MonoImage *image2;
	GHashTable *loaded_images, *loaded_images_by_name;
	int i;

	g_return_val_if_fail (image != NULL, FALSE);

	/*
	 * Atomically decrement the refcount and remove ourselves from the hash tables, so
	 * register_image () can't grab an image which is being closed.
	 */
	mono_images_lock ();

	if (mono_atomic_dec_i32 (&image->ref_count) > 0) {
		mono_images_unlock ();
		return FALSE;
	}

	loaded_images         = get_loaded_images_hash (image->ref_only);
	loaded_images_by_name = get_loaded_images_by_name_hash (image->ref_only);
	image2 = (MonoImage *)g_hash_table_lookup (loaded_images, image->name);
	if (image == image2) {
		/* This is not true if we are called from mono_image_open () */
		g_hash_table_remove (loaded_images, image->name);
	}
	if (image->assembly_name && (g_hash_table_lookup (loaded_images_by_name, image->assembly_name) == image))
		g_hash_table_remove (loaded_images_by_name, (char *) image->assembly_name);	

	mono_images_unlock ();

#ifdef HOST_WIN32
	if (m_image_is_module_handle (image) && m_image_has_entry_point (image)) {
		mono_images_lock ();
		if (image->ref_count == 0) {
			/* Image will be closed by _CorDllMain. */
			FreeLibrary ((HMODULE) image->raw_data);
			mono_images_unlock ();
			return FALSE;
		}
		mono_images_unlock ();
	}
#endif

	MONO_PROFILER_RAISE (image_unloading, (image));

	mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_ASSEMBLY, "Unloading image %s [%p].", image->name, image);

	mono_image_invoke_unload_hook (image);

	mono_metadata_clean_for_image (image);

	/*
	 * The caches inside a MonoImage might refer to metadata which is stored in referenced 
	 * assemblies, so we can't release these references in mono_assembly_close () since the
	 * MonoImage might outlive its associated MonoAssembly.
	 */
	if (image->references && !image_is_dynamic (image)) {
		for (i = 0; i < image->nreferences; i++) {
			if (image->references [i] && image->references [i] != REFERENCE_MISSING) {
				if (!mono_assembly_close_except_image_pools (image->references [i]))
					image->references [i] = NULL;
			}
		}
	} else {
		if (image->references) {
			g_free (image->references);
			image->references = NULL;
		}
	}

	/* a MonoDynamicImage doesn't have any storage */
	g_assert (image_is_dynamic (image) || image->storage != NULL);

	if (image->storage && m_image_is_raw_data_allocated (image)) {
		/* FIXME: do we need this? (image is disposed anyway) */
		/* image->raw_metadata and cli_sections might lie inside image->raw_data */
		MonoCLIImageInfo *ii = image->image_info;

		if ((image->raw_metadata > image->raw_data) &&
			(image->raw_metadata <= (image->raw_data + image->raw_data_len)))
			image->raw_metadata = NULL;

		for (i = 0; i < ii->cli_section_count; i++)
			if (((char*)(ii->cli_sections [i]) > image->raw_data) &&
				((char*)(ii->cli_sections [i]) <= ((char*)image->raw_data + image->raw_data_len)))
				ii->cli_sections [i] = NULL;

	}

	if (image->storage)
		mono_image_storage_close (image->storage);

	if (debug_assembly_unload) {
		char *old_name = image->name;
		image->name = g_strdup_printf ("%s - UNLOADED", old_name);
		g_free (old_name);
	} else {
		g_free (image->name);
		g_free (image->filename);
		g_free (image->guid);
		g_free (image->version);
	}

	if (image->method_cache)
		g_hash_table_destroy (image->method_cache);
	if (image->methodref_cache)
		g_hash_table_destroy (image->methodref_cache);
	mono_internal_hash_table_destroy (&image->class_cache);
	mono_conc_hashtable_destroy (image->field_cache);
	if (image->array_cache) {
		g_hash_table_foreach (image->array_cache, free_array_cache_entry, NULL);
		g_hash_table_destroy (image->array_cache);
	}
	if (image->szarray_cache)
		g_hash_table_destroy (image->szarray_cache);
	if (image->ptr_cache)
		g_hash_table_destroy (image->ptr_cache);
	if (image->name_cache) {
		g_hash_table_foreach (image->name_cache, free_hash_table, NULL);
		g_hash_table_destroy (image->name_cache);
	}

	free_hash (image->delegate_bound_static_invoke_cache);
	free_hash (image->ldfld_wrapper_cache);
	free_hash (image->ldflda_wrapper_cache);
	free_hash (image->stfld_wrapper_cache);
	free_hash (image->isinst_cache);
	free_hash (image->castclass_cache);
	free_hash (image->icall_wrapper_cache);
	free_hash (image->proxy_isinst_cache);
	if (image->var_gparam_cache)
		mono_conc_hashtable_destroy (image->var_gparam_cache);
	if (image->mvar_gparam_cache)
		mono_conc_hashtable_destroy (image->mvar_gparam_cache);
	free_hash (image->wrapper_param_names);
	free_hash (image->pinvoke_scopes);
	free_hash (image->pinvoke_scope_filenames);
	free_hash (image->native_func_wrapper_cache);
	mono_conc_hashtable_destroy (image->typespec_cache);
	free_hash (image->weak_field_indexes);

	mono_wrapper_caches_free (&image->wrapper_caches);

	for (i = 0; i < image->gshared_types_len; ++i)
		free_hash (image->gshared_types [i]);
	g_free (image->gshared_types);

	/* The ownership of signatures is not well defined */
	g_hash_table_destroy (image->memberref_signatures);
	g_hash_table_destroy (image->method_signatures);

	if (image->rgctx_template_hash)
		g_hash_table_destroy (image->rgctx_template_hash);

	if (image->property_hash)
		mono_property_hash_destroy (image->property_hash);

	/*
	reflection_info_unregister_classes is only required by dynamic images, which will not be properly
	cleared during shutdown as we don't perform regular appdomain unload for the root one.
	*/
	g_assert (!image->reflection_info_unregister_classes || mono_runtime_is_shutting_down ());
	image->reflection_info_unregister_classes = NULL;

	if (image->interface_bitset) {
		mono_unload_interface_ids (image->interface_bitset);
		mono_bitset_free (image->interface_bitset);
	}
	if (image->image_info){
		MonoCLIImageInfo *ii = image->image_info;

		g_free (ii->cli_section_tables);
		g_free (ii->cli_sections);
		g_free (image->image_info);
	}

	mono_image_close_except_pools_all (image->files, image->file_count);
	mono_image_close_except_pools_all (image->modules, image->module_count);
	g_free (image->modules_loaded);

	mono_os_mutex_destroy (&image->szarray_cache_lock);
	mono_os_mutex_destroy (&image->lock);

	/*g_print ("destroy image %p (dynamic: %d)\n", image, image->dynamic);*/
	if (image_is_dynamic (image)) {
		/* Dynamic images are GC_MALLOCed */
		g_free ((char*)image->module_name);
		mono_dynamic_image_free ((MonoDynamicImage*)image);
	}

	MONO_PROFILER_RAISE (image_unloaded, (image));

	return TRUE;
}

static void
mono_image_close_all (MonoImage**images, int image_count)
{
	for (int i = 0; i < image_count; ++i) {
		if (images [i])
			mono_image_close_finish (images [i]);
	}
	if (images)
		g_free (images);
}

void
mono_image_close_finish (MonoImage *image)
{
	int i;

	if (image->references && !image_is_dynamic (image)) {
		for (i = 0; i < image->nreferences; i++) {
			if (image->references [i] && image->references [i] != REFERENCE_MISSING)
				mono_assembly_close_finish (image->references [i]);
		}

		g_free (image->references);
		image->references = NULL;
	}

	mono_image_close_all (image->files, image->file_count);
	mono_image_close_all (image->modules, image->module_count);

#ifndef DISABLE_PERFCOUNTERS
	/* FIXME: use an explicit subtraction method as soon as it's available */
	mono_atomic_fetch_add_i32 (&mono_perfcounters->loader_bytes, -1 * mono_mempool_get_allocated (image->mempool));
#endif

	if (!image_is_dynamic (image)) {
		if (debug_assembly_unload)
			mono_mempool_invalidate (image->mempool);
		else {
			mono_mempool_destroy (image->mempool);
			g_free (image);
		}
	} else {
		if (debug_assembly_unload)
			mono_mempool_invalidate (image->mempool);
		else {
			mono_mempool_destroy (image->mempool);
			mono_dynamic_image_free_image ((MonoDynamicImage*)image);
		}
	}
}

/**
 * mono_image_close:
 * \param image The image file we wish to close
 * Closes an image file, deallocates all memory consumed and
 * unmaps all possible sections of the file
 */
void
mono_image_close (MonoImage *image)
{
	if (mono_image_close_except_pools (image))
		mono_image_close_finish (image);
}

/** 
 * mono_image_strerror:
 * \param status an code indicating the result from a recent operation
 * \returns a string describing the error
 */
const char *
mono_image_strerror (MonoImageOpenStatus status)
{
	switch (status){
	case MONO_IMAGE_OK:
		return "success";
	case MONO_IMAGE_ERROR_ERRNO:
		return strerror (errno);
	case MONO_IMAGE_IMAGE_INVALID:
		return "File does not contain a valid CIL image";
	case MONO_IMAGE_MISSING_ASSEMBLYREF:
		return "An assembly was referenced, but could not be found";
	}
	return "Internal error";
}

static gpointer
mono_image_walk_resource_tree (MonoCLIImageInfo *info, guint32 res_id,
			       guint32 lang_id, gunichar2 *name,
			       MonoPEResourceDirEntry *entry,
			       MonoPEResourceDir *root, guint32 level)
{
	gboolean is_string, is_dir;
	guint32 name_offset, dir_offset;

	/* Level 0 holds a directory entry for each type of resource
	 * (identified by ID or name).
	 *
	 * Level 1 holds a directory entry for each named resource
	 * item, and each "anonymous" item of a particular type of
	 * resource.
	 *
	 * Level 2 holds a directory entry for each language pointing to
	 * the actual data.
	 */
	is_string = MONO_PE_RES_DIR_ENTRY_NAME_IS_STRING (*entry);
	name_offset = MONO_PE_RES_DIR_ENTRY_NAME_OFFSET (*entry);

	is_dir = MONO_PE_RES_DIR_ENTRY_IS_DIR (*entry);
	dir_offset = MONO_PE_RES_DIR_ENTRY_DIR_OFFSET (*entry);

	if(level==0) {
		if (is_string)
			return NULL;
	} else if (level==1) {
		if (res_id != name_offset)
			return NULL;
#if 0
		if(name!=NULL &&
		   is_string==TRUE && name!=lookup (name_offset)) {
			return(NULL);
		}
#endif
	} else if (level==2) {
		if (is_string || (lang_id != 0 && name_offset != lang_id))
			return NULL;
	} else {
		g_assert_not_reached ();
	}

	if (is_dir) {
		MonoPEResourceDir *res_dir=(MonoPEResourceDir *)(((char *)root)+dir_offset);
		MonoPEResourceDirEntry *sub_entries=(MonoPEResourceDirEntry *)(res_dir+1);
		guint32 entries, i;

		entries = GUINT16_FROM_LE (res_dir->res_named_entries) + GUINT16_FROM_LE (res_dir->res_id_entries);

		for(i=0; i<entries; i++) {
			MonoPEResourceDirEntry *sub_entry=&sub_entries[i];
			gpointer ret;
			
			ret=mono_image_walk_resource_tree (info, res_id,
							   lang_id, name,
							   sub_entry, root,
							   level+1);
			if(ret!=NULL) {
				return(ret);
			}
		}

		return(NULL);
	} else {
		MonoPEResourceDataEntry *data_entry=(MonoPEResourceDataEntry *)((char *)(root)+dir_offset);
		MonoPEResourceDataEntry *res;

		res = g_new0 (MonoPEResourceDataEntry, 1);

		res->rde_data_offset = GUINT32_TO_LE (data_entry->rde_data_offset);
		res->rde_size = GUINT32_TO_LE (data_entry->rde_size);
		res->rde_codepage = GUINT32_TO_LE (data_entry->rde_codepage);
		res->rde_reserved = GUINT32_TO_LE (data_entry->rde_reserved);

		return (res);
	}
}

/**
 * mono_image_lookup_resource:
 * \param image the image to look up the resource in
 * \param res_id A \c MONO_PE_RESOURCE_ID_ that represents the resource ID to lookup.
 * \param lang_id The language id.
 * \param name the resource name to lookup.
 * \returns NULL if not found, otherwise a pointer to the in-memory representation
 * of the given resource. The caller should free it using \c g_free when no longer
 * needed.
 */
gpointer
mono_image_lookup_resource (MonoImage *image, guint32 res_id, guint32 lang_id, gunichar2 *name)
{
	MonoCLIImageInfo *info;
	MonoDotNetHeader *header;
	MonoPEDatadir *datadir;
	MonoPEDirEntry *rsrc;
	MonoPEResourceDir *resource_dir;
	MonoPEResourceDirEntry *res_entries;
	guint32 entries, i;

	if(image==NULL) {
		return(NULL);
	}

	mono_image_ensure_section_idx (image, MONO_SECTION_RSRC);

	info = (MonoCLIImageInfo *)image->image_info;
	if(info==NULL) {
		return(NULL);
	}

	header=&info->cli_header;
	if(header==NULL) {
		return(NULL);
	}

	datadir=&header->datadir;
	if(datadir==NULL) {
		return(NULL);
	}

	rsrc=&datadir->pe_resource_table;
	if(rsrc==NULL) {
		return(NULL);
	}

	resource_dir=(MonoPEResourceDir *)mono_image_rva_map (image, rsrc->rva);
	if(resource_dir==NULL) {
		return(NULL);
	}

	entries = GUINT16_FROM_LE (resource_dir->res_named_entries) + GUINT16_FROM_LE (resource_dir->res_id_entries);
	res_entries=(MonoPEResourceDirEntry *)(resource_dir+1);
	
	for(i=0; i<entries; i++) {
		MonoPEResourceDirEntry *entry=&res_entries[i];
		gpointer ret;
		
		ret=mono_image_walk_resource_tree (info, res_id, lang_id,
						   name, entry, resource_dir,
						   0);
		if(ret!=NULL) {
			return(ret);
		}
	}

	return(NULL);
}

/** 
 * mono_image_get_entry_point:
 * \param image the image where the entry point will be looked up.
 * Use this routine to determine the metadata token for method that
 * has been flagged as the entry point.
 * \returns the token for the entry point method in the image
 */
guint32
mono_image_get_entry_point (MonoImage *image)
{
	return image->image_info->cli_cli_header.ch_entry_point;
}

/**
 * mono_image_get_resource:
 * \param image the image where the resource will be looked up.
 * \param offset The offset to add to the resource
 * \param size a pointer to an int where the size of the resource will be stored
 *
 * This is a low-level routine that fetches a resource from the
 * metadata that starts at a given \p offset.  The \p size parameter is
 * filled with the data field as encoded in the metadata.
 *
 * \returns the pointer to the resource whose offset is \p offset.
 */
const char*
mono_image_get_resource (MonoImage *image, guint32 offset, guint32 *size)
{
	MonoCLIImageInfo *iinfo = image->image_info;
	MonoCLIHeader *ch = &iinfo->cli_cli_header;
	const char* data;

	if (!ch->ch_resources.rva || offset + 4 > ch->ch_resources.size)
		return NULL;
	
	data = mono_image_rva_map (image, ch->ch_resources.rva);
	if (!data)
		return NULL;
	data += offset;
	if (size)
		*size = read32 (data);
	data += 4;
	return data;
}

// Returning NULL with no error set will be interpeted as "not found"
MonoImage*
mono_image_load_file_for_image_checked (MonoImage *image, int fileidx, MonoError *error)
{
	char *base_dir, *name;
	MonoImage *res;
	MonoTableInfo  *t = &image->tables [MONO_TABLE_FILE];
	const char *fname;
	guint32 fname_id;

	error_init (error);

	if (fileidx < 1 || fileidx > t->rows)
		return NULL;

	mono_image_lock (image);
	if (image->files && image->files [fileidx - 1]) {
		mono_image_unlock (image);
		return image->files [fileidx - 1];
	}
	mono_image_unlock (image);

	fname_id = mono_metadata_decode_row_col (t, fileidx - 1, MONO_FILE_NAME);
	fname = mono_metadata_string_heap (image, fname_id);
	base_dir = g_path_get_dirname (image->name);
	name = g_build_filename (base_dir, fname, NULL);
	res = mono_image_open (name, NULL);
	if (!res)
		goto done;

	mono_image_lock (image);
	if (image->files && image->files [fileidx - 1]) {
		MonoImage *old = res;
		res = image->files [fileidx - 1];
		mono_image_unlock (image);
		mono_image_close (old);
	} else {
		int i;
		/* g_print ("loaded file %s from %s (%p)\n", name, image->name, image->assembly); */
		if (!assign_assembly_parent_for_netmodule (res, image, error)) {
			mono_image_unlock (image);
			mono_image_close (res);
			return NULL;
		}

		for (i = 0; i < res->module_count; ++i) {
			if (res->modules [i] && !res->modules [i]->assembly)
				res->modules [i]->assembly = image->assembly;
		}

		if (!image->files) {
			image->files = g_new0 (MonoImage*, t->rows);
			image->file_count = t->rows;
		}
		image->files [fileidx - 1] = res;
		mono_image_unlock (image);
		/* vtable fixup can't happen with the image lock held */
#ifdef HOST_WIN32
		if (m_image_is_module_handle (res))
			mono_image_fixup_vtable (res);
#endif
	}

done:
	g_free (name);
	g_free (base_dir);
	return res;
}

/**
 * mono_image_load_file_for_image:
 */
MonoImage*
mono_image_load_file_for_image (MonoImage *image, int fileidx)
{
	ERROR_DECL (error);
	MonoImage *result = mono_image_load_file_for_image_checked (image, fileidx, error);
	mono_error_assert_ok (error);
	return result;
}

/**
 * mono_image_get_strong_name:
 * \param image a MonoImage
 * \param size a \c guint32 pointer, or NULL.
 *
 * If the image has a strong name, and \p size is not NULL, the value
 * pointed to by size will have the size of the strong name.
 *
 * \returns NULL if the image does not have a strong name, or a
 * pointer to the public key.
 */
const char*
mono_image_get_strong_name (MonoImage *image, guint32 *size)
{
	MonoCLIImageInfo *iinfo = image->image_info;
	MonoPEDirEntry *de = &iinfo->cli_cli_header.ch_strong_name;
	const char* data;

	if (!de->size || !de->rva)
		return NULL;
	data = mono_image_rva_map (image, de->rva);
	if (!data)
		return NULL;
	if (size)
		*size = de->size;
	return data;
}

/**
 * mono_image_strong_name_position:
 * \param image a \c MonoImage
 * \param size a \c guint32 pointer, or NULL.
 *
 * If the image has a strong name, and \p size is not NULL, the value
 * pointed to by size will have the size of the strong name.
 *
 * \returns the position within the image file where the strong name
 * is stored.
 */
guint32
mono_image_strong_name_position (MonoImage *image, guint32 *size)
{
	MonoCLIImageInfo *iinfo = image->image_info;
	MonoPEDirEntry *de = &iinfo->cli_cli_header.ch_strong_name;
	guint32 pos;

	if (size)
		*size = de->size;
	if (!de->size || !de->rva)
		return 0;
	pos = mono_cli_rva_image_map (image, de->rva);
	return pos == INVALID_ADDRESS ? 0 : pos;
}

/**
 * mono_image_get_public_key:
 * \param image a \c MonoImage
 * \param size a \c guint32 pointer, or NULL.
 *
 * This is used to obtain the public key in the \p image.
 * 
 * If the image has a public key, and \p size is not NULL, the value
 * pointed to by size will have the size of the public key.
 * 
 * \returns NULL if the image does not have a public key, or a pointer
 * to the public key.
 */
const char*
mono_image_get_public_key (MonoImage *image, guint32 *size)
{
	const char *pubkey;
	guint32 len, tok;

	if (image_is_dynamic (image)) {
		if (size)
			*size = ((MonoDynamicImage*)image)->public_key_len;
		return (char*)((MonoDynamicImage*)image)->public_key;
	}
	if (image->tables [MONO_TABLE_ASSEMBLY].rows != 1)
		return NULL;
	tok = mono_metadata_decode_row_col (&image->tables [MONO_TABLE_ASSEMBLY], 0, MONO_ASSEMBLY_PUBLIC_KEY);
	if (!tok)
		return NULL;
	pubkey = mono_metadata_blob_heap (image, tok);
	len = mono_metadata_decode_blob_size (pubkey, &pubkey);
	if (size)
		*size = len;
	return pubkey;
}

/**
 * mono_image_get_name:
 * \param name a \c MonoImage
 * \returns the name of the assembly.
 */
const char*
mono_image_get_name (MonoImage *image)
{
	return image->assembly_name;
}

/**
 * mono_image_get_filename:
 * \param image a \c MonoImage
 * Used to get the filename that hold the actual \c MonoImage
 * \returns the filename.
 */
const char*
mono_image_get_filename (MonoImage *image)
{
	return image->name;
}

/**
 * mono_image_get_guid:
 */
const char*
mono_image_get_guid (MonoImage *image)
{
	return image->guid;
}

/**
 * mono_image_get_table_info:
 */
const MonoTableInfo*
mono_image_get_table_info (MonoImage *image, int table_id)
{
	if (table_id < 0 || table_id >= MONO_TABLE_NUM)
		return NULL;
	return &image->tables [table_id];
}

/**
 * mono_image_get_table_rows:
 */
int
mono_image_get_table_rows (MonoImage *image, int table_id)
{
	if (table_id < 0 || table_id >= MONO_TABLE_NUM)
		return 0;
	return image->tables [table_id].rows;
}

/**
 * mono_table_info_get_rows:
 */
int
mono_table_info_get_rows (const MonoTableInfo *table)
{
	return table->rows;
}

/**
 * mono_image_get_assembly:
 * \param image the \c MonoImage .
 * Use this routine to get the assembly that owns this image.
 * \returns the assembly that holds this image.
 */
MonoAssembly* 
mono_image_get_assembly (MonoImage *image)
{
	return image->assembly;
}

/**
 * mono_image_is_dynamic:
 * \param image the \c MonoImage
 *
 * Determines if the given image was created dynamically through the
 * \c System.Reflection.Emit API
 * \returns TRUE if the image was created dynamically, FALSE if not.
 */
gboolean
mono_image_is_dynamic (MonoImage *image)
{
	return image_is_dynamic (image);
}

/**
 * mono_image_has_authenticode_entry:
 * \param image the \c MonoImage
 * Use this routine to determine if the image has a Authenticode
 * Certificate Table.
 * \returns TRUE if the image contains an authenticode entry in the PE
 * directory.
 */
gboolean
mono_image_has_authenticode_entry (MonoImage *image)
{
	MonoCLIImageInfo *iinfo = image->image_info;
	MonoDotNetHeader *header = &iinfo->cli_header;
	if (!header)
		return FALSE;
	MonoPEDirEntry *de = &header->datadir.pe_certificate_table;
	// the Authenticode "pre" (non ASN.1) header is 8 bytes long
	return ((de->rva != 0) && (de->size > 8));
}

gpointer
mono_image_alloc (MonoImage *image, guint size)
{
	gpointer res;

#ifndef DISABLE_PERFCOUNTERS
	mono_atomic_fetch_add_i32 (&mono_perfcounters->loader_bytes, size);
#endif
	mono_image_lock (image);
	res = mono_mempool_alloc (image->mempool, size);
	mono_image_unlock (image);

	return res;
}

gpointer
mono_image_alloc0 (MonoImage *image, guint size)
{
	gpointer res;

#ifndef DISABLE_PERFCOUNTERS
	mono_atomic_fetch_add_i32 (&mono_perfcounters->loader_bytes, size);
#endif
	mono_image_lock (image);
	res = mono_mempool_alloc0 (image->mempool, size);
	mono_image_unlock (image);

	return res;
}

char*
mono_image_strdup (MonoImage *image, const char *s)
{
	char *res;

#ifndef DISABLE_PERFCOUNTERS
	mono_atomic_fetch_add_i32 (&mono_perfcounters->loader_bytes, (gint32)strlen (s));
#endif
	mono_image_lock (image);
	res = mono_mempool_strdup (image->mempool, s);
	mono_image_unlock (image);

	return res;
}

char*
mono_image_strdup_vprintf (MonoImage *image, const char *format, va_list args)
{
	char *buf;
	mono_image_lock (image);
	buf = mono_mempool_strdup_vprintf (image->mempool, format, args);
	mono_image_unlock (image);
#ifndef DISABLE_PERFCOUNTERS
	mono_atomic_fetch_add_i32 (&mono_perfcounters->loader_bytes, (gint32)strlen (buf));
#endif
	return buf;
}

char*
mono_image_strdup_printf (MonoImage *image, const char *format, ...)
{
	char *buf;
	va_list args;

	va_start (args, format);
	buf = mono_image_strdup_vprintf (image, format, args);
	va_end (args);
	return buf;
}

GList*
mono_g_list_prepend_image (MonoImage *image, GList *list, gpointer data)
{
	GList *new_list;
	
	new_list = (GList *)mono_image_alloc (image, sizeof (GList));
	new_list->data = data;
	new_list->prev = list ? list->prev : NULL;
    new_list->next = list;

    if (new_list->prev)
            new_list->prev->next = new_list;
    if (list)
            list->prev = new_list;

	return new_list;
}

GSList*
mono_g_slist_append_image (MonoImage *image, GSList *list, gpointer data)
{
	GSList *new_list;

	new_list = (GSList *)mono_image_alloc (image, sizeof (GSList));
	new_list->data = data;
	new_list->next = NULL;

	return g_slist_concat (list, new_list);
}

void
mono_image_lock (MonoImage *image)
{
	mono_locks_os_acquire (&image->lock, ImageDataLock);
}

void
mono_image_unlock (MonoImage *image)
{
	mono_locks_os_release (&image->lock, ImageDataLock);
}


/**
 * mono_image_property_lookup:
 * Lookup a property on \p image . Used to store very rare fields of \c MonoClass and \c MonoMethod .
 *
 * LOCKING: Takes the image lock
 */
gpointer 
mono_image_property_lookup (MonoImage *image, gpointer subject, guint32 property)
{
	gpointer res;

	mono_image_lock (image);
	res = mono_property_hash_lookup (image->property_hash, subject, property);
 	mono_image_unlock (image);

	return res;
}

/**
 * mono_image_property_insert:
 * Insert a new property \p property with value \p value on \p subject in \p
 * image. Used to store very rare fields of \c MonoClass and \c MonoMethod.
 *
 * LOCKING: Takes the image lock
 */
void
mono_image_property_insert (MonoImage *image, gpointer subject, guint32 property, gpointer value)
{
	CHECKED_METADATA_STORE_LOCAL (image->mempool, value);
	mono_image_lock (image);
	mono_property_hash_insert (image->property_hash, subject, property, value);
 	mono_image_unlock (image);
}

/**
 * mono_image_property_remove:
 * Remove all properties associated with \p subject in \p image. Used to store very rare fields of \c MonoClass and \c MonoMethod .
 *
 * LOCKING: Takes the image lock
 */
void
mono_image_property_remove (MonoImage *image, gpointer subject)
{
	mono_image_lock (image);
	mono_property_hash_remove_object (image->property_hash, subject);
 	mono_image_unlock (image);
}

void
mono_image_append_class_to_reflection_info_set (MonoClass *klass)
{
	MonoImage *image = m_class_get_image (klass);
	g_assert (image_is_dynamic (image));
	mono_image_lock (image);
	image->reflection_info_unregister_classes = g_slist_prepend_mempool (image->mempool, image->reflection_info_unregister_classes, klass);
	mono_image_unlock (image);
}

// This is support for the mempool reference tracking feature in checked-build, but lives in image.c due to use of static variables of this file.

/**
 * mono_find_image_owner:
 *
 * Find the image, if any, which a given pointer is located in the memory of.
 */
MonoImage *
mono_find_image_owner (void *ptr)
{
	mono_images_lock ();

	MonoImage *owner = NULL;

	// Iterate over both by-path image hashes
	const int hash_candidates[] = {IMAGES_HASH_PATH, IMAGES_HASH_PATH_REFONLY};
	int hash_idx;
	for (hash_idx = 0; !owner && hash_idx < G_N_ELEMENTS (hash_candidates); hash_idx++)
	{
		GHashTable *target = loaded_images_hashes [hash_candidates [hash_idx]];
		GHashTableIter iter;
		MonoImage *image;

		// Iterate over images within a hash
		g_hash_table_iter_init (&iter, target);
		while (!owner && g_hash_table_iter_next(&iter, NULL, (gpointer *)&image))
		{
			mono_image_lock (image);
			if (mono_mempool_contains_addr (image->mempool, ptr))
				owner = image;
			mono_image_unlock (image);
		}
	}

	mono_images_unlock ();

	return owner;
}
