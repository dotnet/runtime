/*
 * image.c: Routines for manipulating an image stored in an
 * extended PE/COFF file.
 * 
 * Authors:
 *   Miguel de Icaza (miguel@ximian.com)
 *   Paolo Molaro (lupus@ximian.com)
 *
 * (C) 2001-2003 Ximian, Inc.  http://www.ximian.com
 *
 */
#include <config.h>
#include <stdio.h>
#include <glib.h>
#include <errno.h>
#include <time.h>
#include <string.h>
#include "image.h"
#include "cil-coff.h"
#include "rawbuffer.h"
#include "mono-endian.h"
#include "private.h"
#include "tabledefs.h"
#include "tokentype.h"
#include "metadata-internals.h"
#include <mono/io-layer/io-layer.h>

#define INVALID_ADDRESS 0xffffffff

/*
 * Keeps track of the various assemblies loaded
 */
static GHashTable *loaded_images_hash;
static GHashTable *loaded_images_guid_hash;

static CRITICAL_SECTION images_mutex;

guint32
mono_cli_rva_image_map (MonoCLIImageInfo *iinfo, guint32 addr)
{
	const int top = iinfo->cli_section_count;
	MonoSectionTable *tables = iinfo->cli_section_tables;
	int i;
	
	for (i = 0; i < top; i++){
		if ((addr >= tables->st_virtual_address) &&
		    (addr < tables->st_virtual_address + tables->st_raw_data_size)){
			return addr - tables->st_virtual_address + tables->st_raw_data_ptr;
		}
		tables++;
	}
	return INVALID_ADDRESS;
}

char *
mono_image_rva_map (MonoImage *image, guint32 addr)
{
	MonoCLIImageInfo *iinfo = image->image_info;
	const int top = iinfo->cli_section_count;
	MonoSectionTable *tables = iinfo->cli_section_tables;
	int i;
	
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

static gchar *
canonicalize_path (const char *path)
{
	gchar *abspath, *pos, *lastpos, *dest;
	int backc;

	if (g_path_is_absolute (path)) {
		abspath = g_strdup (path);
	} else {
		gchar *tmpdir = g_get_current_dir ();
		abspath = g_build_filename (tmpdir, path, NULL);
		g_free (tmpdir);
	}

	abspath = g_strreverse (abspath);

	backc = 0;
	dest = lastpos = abspath;
	pos = strchr (lastpos, G_DIR_SEPARATOR);

	while (pos != NULL) {
		int len = pos - lastpos;
		if (len == 1 && lastpos [0] == '.') {
			// nop
		} else if (len == 2 && lastpos [0] == '.' && lastpos [1] == '.') {
			backc++;
		} else if (len > 0) {
			if (backc > 0) {
				backc--;
			} else {
				if (dest != lastpos) strncpy (dest, lastpos, len + 1);
				dest += len + 1;
			}
		}
		lastpos = pos + 1;
		pos = strchr (lastpos, G_DIR_SEPARATOR);
	}
	
	if (dest != lastpos) strcpy (dest, lastpos);
	return g_strreverse (abspath);
}

/**
 * mono_images_init:
 *
 *  Initialize the global variables used by this module.
 */
void
mono_images_init (void)
{
	InitializeCriticalSection (&images_mutex);

	loaded_images_hash = g_hash_table_new (g_str_hash, g_str_equal);
	loaded_images_guid_hash = g_hash_table_new (g_str_hash, g_str_equal);
}

/**
 * mono_image_ensure_section_idx:
 * @image: The image we are operating on
 * @section: section number that we will load/map into memory
 *
 * This routine makes sure that we have an in-memory copy of
 * an image section (.text, .rsrc, .data).
 *
 * Returns: TRUE on success
 */
int
mono_image_ensure_section_idx (MonoImage *image, int section)
{
	MonoCLIImageInfo *iinfo = image->image_info;
	MonoSectionTable *sect;
	gboolean writable;
	
	g_return_val_if_fail (section < iinfo->cli_section_count, FALSE);

	if (iinfo->cli_sections [section] != NULL)
		return TRUE;

	sect = &iinfo->cli_section_tables [section];
	
	writable = sect->st_flags & SECT_FLAGS_MEM_WRITE;

	if (!image->f) {
		if (sect->st_raw_data_ptr + sect->st_raw_data_size > image->raw_data_len)
			return FALSE;
		/* FIXME: we ignore the writable flag since we don't patch the binary */
		iinfo->cli_sections [section] = image->raw_data + sect->st_raw_data_ptr;
		return TRUE;
	}
	iinfo->cli_sections [section] = mono_raw_buffer_load (
		fileno (image->f), writable,
		sect->st_raw_data_ptr, sect->st_raw_data_size);

	if (iinfo->cli_sections [section] == NULL)
		return FALSE;

	return TRUE;
}

/**
 * mono_image_ensure_section:
 * @image: The image we are operating on
 * @section: section name that we will load/map into memory
 *
 * This routine makes sure that we have an in-memory copy of
 * an image section (.text, .rsrc, .data).
 *
 * Returns: TRUE on success
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

		if (!image->f) {
			if (offset + sizeof (MonoSectionTable) > image->raw_data_len)
				return FALSE;
			memcpy (t, image->raw_data + offset, sizeof (MonoSectionTable));
			offset += sizeof (MonoSectionTable);
		} else {
			if (fread (t, sizeof (MonoSectionTable), 1, image->f) != 1)
				return FALSE;
		}

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

static gboolean
load_cli_header (MonoImage *image, MonoCLIImageInfo *iinfo)
{
	guint32 offset;
	int n;
	
	offset = mono_cli_rva_image_map (iinfo, iinfo->cli_header.datadir.pe_cli_header.rva);
	if (offset == INVALID_ADDRESS)
		return FALSE;

	if (!image->f) {
		if (offset + sizeof (MonoCLIHeader) > image->raw_data_len)
			return FALSE;
		memcpy (&iinfo->cli_cli_header, image->raw_data + offset, sizeof (MonoCLIHeader));
	} else {
		if (fseek (image->f, offset, SEEK_SET) != 0)
			return FALSE;
	
		if ((n = fread (&iinfo->cli_cli_header, sizeof (MonoCLIHeader), 1, image->f)) != 1)
			return FALSE;
	}

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
	
	offset = mono_cli_rva_image_map (iinfo, iinfo->cli_cli_header.ch_metadata.rva);
	if (offset == INVALID_ADDRESS)
		return FALSE;

	size = iinfo->cli_cli_header.ch_metadata.size;

	if (!image->f) {
		if (offset + size > image->raw_data_len)
			return FALSE;
		image->raw_metadata = image->raw_data + offset;
	} else {
		image->raw_metadata = mono_raw_buffer_load (fileno (image->f), FALSE, offset, size);
		if (image->raw_metadata == NULL)
			return FALSE;
	}

	ptr = image->raw_metadata;

	if (strncmp (ptr, "BSJB", 4) == 0){
		guint32 version_string_len;

		ptr += 12;
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
			g_print ("Assembly '%s' has the non-standard metadata heap #-.\nRecompile it correctly (without the /incremental switch or in Release mode).", image->name);
			return FALSE;
		} else {
			g_message ("Unknown heap type: %s\n", ptr + 8);
			ptr += 8 + strlen (ptr + 8) + 1;
		}
		pad = ptr - image->raw_metadata;
		if (pad % 4)
			ptr += 4 - (pad % 4);
	}

	g_assert (image->heap_guid.data);
	g_assert (image->heap_guid.size >= 16);

	image->guid = mono_guid_to_string (image->heap_guid.data);

	return TRUE;
}

/*
 * Load representation of logical metadata tables, from the "#~" stream
 */
static gboolean
load_tables (MonoImage *image)
{
	const char *heap_tables = image->heap_tables.data;
	const guint32 *rows;
	guint64 valid_mask, sorted_mask;
	int valid = 0, table;
	int heap_sizes;
	
	heap_sizes = heap_tables [6];
	image->idx_string_wide = ((heap_sizes & 0x01) == 1);
	image->idx_guid_wide   = ((heap_sizes & 0x02) == 2);
	image->idx_blob_wide   = ((heap_sizes & 0x04) == 4);
	
	valid_mask = read64 (heap_tables + 8);
	sorted_mask = read64 (heap_tables + 16);
	rows = (const guint32 *) (heap_tables + 24);
	
	for (table = 0; table < 64; table++){
		if ((valid_mask & ((guint64) 1 << table)) == 0){
			image->tables [table].rows = 0;
			continue;
		}
		if (table > MONO_TABLE_LAST) {
			g_warning("bits in valid must be zero above 0x2d (II - 23.1.6)");
		}
		/*if ((sorted_mask & ((guint64) 1 << table)) == 0){
			g_print ("table %s (0x%02x) is sorted\n", mono_meta_table_name (table), table);
		}*/
		image->tables [table].rows = read32 (rows);
		rows++;
		valid++;
	}

	image->tables_base = (heap_tables + 24) + (4 * valid);

	/* They must be the same */
	g_assert ((const void *) image->tables_base == (const void *) rows);

	mono_metadata_compute_table_bases (image);
	return TRUE;
}

static gboolean
load_metadata (MonoImage *image, MonoCLIImageInfo *iinfo)
{
	if (!load_metadata_ptrs (image, iinfo))
		return FALSE;

	return load_tables (image);
}

void
mono_image_add_to_name_cache (MonoImage *image, const char *nspace, 
							  const char *name, guint32 index)
{
	GHashTable *nspace_table;
	GHashTable *name_cache = image->name_cache;

	if (!(nspace_table = g_hash_table_lookup (name_cache, nspace))) {
		nspace_table = g_hash_table_new (g_str_hash, g_str_equal);
		g_hash_table_insert (name_cache, (char *)nspace, (char *)nspace_table);
	}
	g_hash_table_insert (nspace_table, (char *) name, GUINT_TO_POINTER (index));
}

static void
load_modules (MonoImage *image, MonoImageOpenStatus *status)
{
	MonoTableInfo *t;
	int i;
	char *base_dir;

	if (image->modules)
		return;

	t = &image->tables [MONO_TABLE_MODULEREF];
	image->modules = g_new0 (MonoImage *, t->rows);
	image->module_count = t->rows;
	base_dir = g_path_get_dirname (image->name);
	for (i = 0; i < t->rows; i++){
		char *module_ref;
		const char *name;
		guint32 cols [MONO_MODULEREF_SIZE];

		mono_metadata_decode_row (t, i, cols, MONO_MODULEREF_SIZE);
		name = mono_metadata_string_heap (image, cols [MONO_MODULEREF_NAME]);
		module_ref = g_build_filename (base_dir, name, NULL);
		image->modules [i] = mono_image_open (module_ref, status);
		if (image->modules [i]) {
			/* g_print ("loaded module %s from %s (%p)\n", module_ref, image->name, image->assembly); */
		}
		/* 
		 * FIXME: what do we do here? it could be a native dll...
		 * We should probably do lazy-loading of modules.
		 */
		if (status)
			*status = MONO_IMAGE_OK;
		g_free (module_ref);
	}

	g_free (base_dir);
}

static void
load_class_names (MonoImage *image)
{
	MonoTableInfo  *t = &image->tables [MONO_TABLE_TYPEDEF];
	guint32 cols [MONO_TYPEDEF_SIZE];
	const char *name;
	const char *nspace;
	guint32 i, visib, nspace_index;
	GHashTable *name_cache2, *nspace_table;

	/* Temporary hash table to avoid lookups in the nspace_table */
	name_cache2 = g_hash_table_new (NULL, NULL);

	for (i = 1; i <= t->rows; ++i) {
		mono_metadata_decode_row (t, i - 1, cols, MONO_TYPEDEF_SIZE);
		/* nested types are accessed from the nesting name */
		visib = cols [MONO_TYPEDEF_FLAGS] & TYPE_ATTRIBUTE_VISIBILITY_MASK;
		if (visib > TYPE_ATTRIBUTE_PUBLIC && visib <= TYPE_ATTRIBUTE_NESTED_ASSEMBLY)
			continue;
		name = mono_metadata_string_heap (image, cols [MONO_TYPEDEF_NAME]);
		nspace = mono_metadata_string_heap (image, cols [MONO_TYPEDEF_NAMESPACE]);

		nspace_index = cols [MONO_TYPEDEF_NAMESPACE];
		nspace_table = g_hash_table_lookup (name_cache2, GUINT_TO_POINTER (nspace_index));
		if (!nspace_table) {
			nspace_table = g_hash_table_new (g_str_hash, g_str_equal);
			g_hash_table_insert (image->name_cache, (char*)nspace, nspace_table);
			g_hash_table_insert (name_cache2, GUINT_TO_POINTER (nspace_index),
								 nspace_table);
		}
		g_hash_table_insert (nspace_table, (char *) name, GUINT_TO_POINTER (i));
	}

	/* Load type names from EXPORTEDTYPES table */
	{
		MonoTableInfo  *t = &image->tables [MONO_TABLE_EXPORTEDTYPE];
		guint32 cols [MONO_EXP_TYPE_SIZE];
		int i;

		for (i = 0; i < t->rows; ++i) {
			mono_metadata_decode_row (t, i, cols, MONO_EXP_TYPE_SIZE);
			name = mono_metadata_string_heap (image, cols [MONO_EXP_TYPE_NAME]);
			nspace = mono_metadata_string_heap (image, cols [MONO_EXP_TYPE_NAMESPACE]);

			nspace_index = cols [MONO_EXP_TYPE_NAMESPACE];
			nspace_table = g_hash_table_lookup (name_cache2, GUINT_TO_POINTER (nspace_index));
			if (!nspace_table) {
				nspace_table = g_hash_table_new (g_str_hash, g_str_equal);
				g_hash_table_insert (image->name_cache, (char*)nspace, nspace_table);
				g_hash_table_insert (name_cache2, GUINT_TO_POINTER (nspace_index),
									 nspace_table);
			}
			g_hash_table_insert (nspace_table, (char *) name, GUINT_TO_POINTER (mono_metadata_make_token (MONO_TABLE_EXPORTEDTYPE, i + 1)));
		}
	}

	g_hash_table_destroy (name_cache2);
}

static void
register_guid (gpointer key, gpointer value, gpointer user_data)
{
	MonoImage *image = (MonoImage*)value;

	if (!g_hash_table_lookup (loaded_images_guid_hash, image))
		g_hash_table_insert (loaded_images_guid_hash, image->guid, image);
}

static void
build_guid_table (void)
{
	g_hash_table_foreach (loaded_images_hash, register_guid, NULL);
}

void
mono_image_init (MonoImage *image)
{
	image->method_cache = g_hash_table_new (NULL, NULL);
	image->class_cache = g_hash_table_new (NULL, NULL);
	image->field_cache = g_hash_table_new (NULL, NULL);
	image->name_cache = g_hash_table_new (g_str_hash, g_str_equal);
	image->array_cache = g_hash_table_new (NULL, NULL);

	image->delegate_begin_invoke_cache = 
		g_hash_table_new ((GHashFunc)mono_signature_hash, 
				  (GCompareFunc)mono_metadata_signature_equal);
	image->delegate_end_invoke_cache = 
		g_hash_table_new ((GHashFunc)mono_signature_hash, 
				  (GCompareFunc)mono_metadata_signature_equal);
	image->delegate_invoke_cache = 
		g_hash_table_new ((GHashFunc)mono_signature_hash, 
				  (GCompareFunc)mono_metadata_signature_equal);
	image->runtime_invoke_cache  = 
		g_hash_table_new ((GHashFunc)mono_signature_hash, 
				  (GCompareFunc)mono_metadata_signature_equal);
	
	image->managed_wrapper_cache = g_hash_table_new (NULL, NULL);
	image->native_wrapper_cache = g_hash_table_new (NULL, NULL);
	image->remoting_invoke_cache = g_hash_table_new (NULL, NULL);
	image->synchronized_cache = g_hash_table_new (NULL, NULL);

	image->typespec_cache = g_hash_table_new (NULL, NULL);
	image->memberref_signatures = g_hash_table_new (NULL, NULL);
	image->helper_signatures = g_hash_table_new (g_str_hash, g_str_equal);

	image->generic_class_cache =
		g_hash_table_new ((GHashFunc)mono_metadata_generic_class_hash,
				  (GCompareFunc)mono_metadata_generic_class_equal);
}

static MonoImage *
do_mono_image_load (MonoImage *image, MonoImageOpenStatus *status)
{
	MonoCLIImageInfo *iinfo;
	MonoDotNetHeader *header;
	MonoMSDOSHeader msdos;
	int n;
	guint32 offset = 0;

	mono_image_init (image);

	iinfo = image->image_info;
	header = &iinfo->cli_header;
		
	if (status)
		*status = MONO_IMAGE_IMAGE_INVALID;

	if (!image->f) {
		if (offset + sizeof (msdos) > image->raw_data_len)
			goto invalid_image;
		memcpy (&msdos, image->raw_data + offset, sizeof (msdos));
	} else {
		if (fread (&msdos, sizeof (msdos), 1, image->f) != 1)
			goto invalid_image;
	}
	
	if (!(msdos.msdos_sig [0] == 'M' && msdos.msdos_sig [1] == 'Z'))
		goto invalid_image;
	
	msdos.pe_offset = GUINT32_FROM_LE (msdos.pe_offset);

	if (msdos.pe_offset != sizeof (msdos)) {
		if (image->f)
			fseek (image->f, msdos.pe_offset, SEEK_SET);
	}
	offset = msdos.pe_offset;

	if (!image->f) {
		if (offset + sizeof (MonoDotNetHeader) > image->raw_data_len)
			goto invalid_image;
		memcpy (header, image->raw_data + offset, sizeof (MonoDotNetHeader));
		offset += sizeof (MonoDotNetHeader);
	} else {
		if ((n = fread (header, sizeof (MonoDotNetHeader), 1, image->f)) != 1)
			goto invalid_image;
	}

#if G_BYTE_ORDER != G_LITTLE_ENDIAN
#define SWAP32(x) (x) = GUINT32_FROM_LE ((x))
#define SWAP16(x) (x) = GUINT16_FROM_LE ((x))
#define SWAPPDE(x) do { (x).rva = GUINT32_FROM_LE ((x).rva); (x).size = GUINT32_FROM_LE ((x).size);} while (0)
	SWAP32 (header->coff.coff_time);
	SWAP32 (header->coff.coff_symptr);
	SWAP32 (header->coff.coff_symcount);
	SWAP16 (header->coff.coff_machine);
	SWAP16 (header->coff.coff_sections);
	SWAP16 (header->coff.coff_opt_header_size);
	SWAP16 (header->coff.coff_attributes);
	/* MonoPEHeader */
	SWAP32 (header->pe.pe_code_size);
	SWAP32 (header->pe.pe_data_size);
	SWAP32 (header->pe.pe_uninit_data_size);
	SWAP32 (header->pe.pe_rva_entry_point);
	SWAP32 (header->pe.pe_rva_code_base);
	SWAP32 (header->pe.pe_rva_data_base);
	SWAP16 (header->pe.pe_magic);

	/* MonoPEHeaderNT: not used yet */
	SWAP32	(header->nt.pe_image_base); 	/* must be 0x400000 */
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
	SWAP32	(header->nt.pe_stack_reserve);
	SWAP32	(header->nt.pe_stack_commit);
	SWAP32	(header->nt.pe_heap_reserve);
	SWAP32	(header->nt.pe_heap_commit);
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

#undef SWAP32
#undef SWAP16
#undef SWAPPDE
#endif

	if (header->coff.coff_machine != 0x14c)
		goto invalid_image;

	if (header->coff.coff_opt_header_size != (sizeof (MonoDotNetHeader) - sizeof (MonoCOFFHeader) - 4))
		goto invalid_image;

	if (header->pesig[0] != 'P' || header->pesig[1] != 'E' || header->pe.pe_magic != 0x10B)
		goto invalid_image;

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
	
	/* Load the CLI header */
	if (!load_cli_header (image, iinfo))
		goto invalid_image;

	if (!load_metadata (image, iinfo))
		goto invalid_image;

	load_class_names (image);

	/* modules don't have an assembly table row */
	if (image->tables [MONO_TABLE_ASSEMBLY].rows)
		image->assembly_name = mono_metadata_string_heap (image, 
			mono_metadata_decode_row_col (&image->tables [MONO_TABLE_ASSEMBLY],
					0, MONO_ASSEMBLY_NAME));

	image->module_name = mono_metadata_string_heap (image, 
			mono_metadata_decode_row_col (&image->tables [MONO_TABLE_MODULE],
					0, MONO_MODULE_NAME));

	load_modules (image, status);

	if (status)
		*status = MONO_IMAGE_OK;

	image->ref_count=1;

	return image;

invalid_image:
	mono_image_close (image);
		return NULL;
}

static MonoImage *
do_mono_image_open (const char *fname, MonoImageOpenStatus *status)
{
	MonoCLIImageInfo *iinfo;
	MonoImage *image;
	FILE *filed;

	if ((filed = fopen (fname, "rb")) == NULL){
		if (status)
			*status = MONO_IMAGE_ERROR_ERRNO;
		return NULL;
	}

	image = g_new0 (MonoImage, 1);
	image->ref_count = 1;
	image->f = filed;
	iinfo = g_new0 (MonoCLIImageInfo, 1);
	image->image_info = iinfo;
	image->name = canonicalize_path (fname);

	return do_mono_image_load (image, status);
}

MonoImage *
mono_image_loaded (const char *name)
{
	MonoImage *res;
        
	EnterCriticalSection (&images_mutex);
	res = g_hash_table_lookup (loaded_images_hash, name);
	LeaveCriticalSection (&images_mutex);
	return res;
}

MonoImage *
mono_image_loaded_by_guid (const char *guid)
{
	MonoImage *res;

	EnterCriticalSection (&images_mutex);
	res = g_hash_table_lookup (loaded_images_guid_hash, guid);
	LeaveCriticalSection (&images_mutex);
	return res;
}

MonoImage *
mono_image_open_from_data (char *data, guint32 data_len, gboolean need_copy, MonoImageOpenStatus *status)
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
		datac = g_try_malloc (data_len);
		if (!datac) {
			if (status)
				*status = MONO_IMAGE_ERROR_ERRNO;
			return NULL;
		}
		memcpy (datac, data, data_len);
	}

	image = g_new0 (MonoImage, 1);
	image->ref_count = 1;
	image->raw_data = datac;
	image->raw_data_len = data_len;
	image->raw_data_allocated = need_copy;
	image->name = g_strdup_printf ("data-%p", datac);
	iinfo = g_new0 (MonoCLIImageInfo, 1);
	image->image_info = iinfo;

	return do_mono_image_load (image, status);
}

/**
 * mono_image_open:
 * @fname: filename that points to the module we want to open
 * @status: An error condition is returned in this field
 *
 * Retuns: An open image of type %MonoImage or NULL on error.
 * if NULL, then check the value of @status for details on the error
 */
MonoImage *
mono_image_open (const char *fname, MonoImageOpenStatus *status)
{
	MonoImage *image, *image2;
	char *absfname;
	
	g_return_val_if_fail (fname != NULL, NULL);
	
	absfname = canonicalize_path (fname);

	/*
	 * The easiest solution would be to do all the loading inside the mutex,
	 * but that would lead to scalability problems. So we let the loading
	 * happen outside the mutex, and if multiple threads happen to load
	 * the same image, we discard all but the first copy.
	 */
	EnterCriticalSection (&images_mutex);
	image = g_hash_table_lookup (loaded_images_hash, absfname);
	g_free (absfname);
	
	if (image){
		image->ref_count++;
		LeaveCriticalSection (&images_mutex);
		return image;
	}
	LeaveCriticalSection (&images_mutex);

	image = do_mono_image_open (fname, status);
	if (image == NULL)
		return NULL;

	EnterCriticalSection (&images_mutex);
	image2 = g_hash_table_lookup (loaded_images_hash, image->name);

	if (image2) {
		/* Somebody else beat us to it */
		image2->ref_count ++;
		LeaveCriticalSection (&images_mutex);
		mono_image_close (image);
		return image2;
	}
	g_hash_table_insert (loaded_images_hash, image->name, image);
	if (image->assembly_name && (g_hash_table_lookup (loaded_images_hash, image->assembly_name) == NULL))
		g_hash_table_insert (loaded_images_hash, (char *) image->assembly_name, image);	
	g_hash_table_insert (loaded_images_guid_hash, image->guid, image);
	LeaveCriticalSection (&images_mutex);

	return image;
}

static void
free_hash_table (gpointer key, gpointer val, gpointer user_data)
{
	g_hash_table_destroy ((GHashTable*)val);
}

static void
free_mr_signatures (gpointer key, gpointer val, gpointer user_data)
{
	mono_metadata_free_method_signature ((MonoMethodSignature*)val);
}

static void
free_remoting_wrappers (gpointer key, gpointer val, gpointer user_data)
{
	g_free (val);
}

/**
 * mono_image_addref:
 * @image: The image file we wish to add a reference to
 *
 *  Increases the reference count of an image.
 */
void
mono_image_addref (MonoImage *image)
{
	InterlockedIncrement (&image->ref_count);
}	

/**
 * mono_image_close:
 * @image: The image file we wish to close
 *
 * Closes an image file, deallocates all memory consumed and
 * unmaps all possible sections of the file
 */
void
mono_image_close (MonoImage *image)
{
	MonoImage *image2;

	g_return_if_fail (image != NULL);

	EnterCriticalSection (&images_mutex);
	if (--image->ref_count) {
		LeaveCriticalSection (&images_mutex);
		return;
	}
	image2 = g_hash_table_lookup (loaded_images_hash, image->name);
	if (image == image2) {
		/* This is not true if we are called from mono_image_open () */
		g_hash_table_remove (loaded_images_hash, image->name);
		g_hash_table_remove (loaded_images_guid_hash, image->guid);
		/* Multiple images might have the same guid */
		build_guid_table ();
	}
	if (image->assembly_name && (g_hash_table_lookup (loaded_images_hash, image->assembly_name) == image))
		g_hash_table_remove (loaded_images_hash, (char *) image->assembly_name);	
	LeaveCriticalSection (&images_mutex);

	if (image->f)
		fclose (image->f);
	if (image->raw_data_allocated) {
		/* image->raw_metadata and cli_sections might lie inside image->raw_data */
		MonoCLIImageInfo *ii = image->image_info;
		int i;

		if ((image->raw_metadata > image->raw_data) &&
			(image->raw_metadata <= (image->raw_data + image->raw_data_len)))
			image->raw_metadata = NULL;

		for (i = 0; i < ii->cli_section_count; i++)
			if (((char*)(ii->cli_sections [i]) > image->raw_data) &&
				((char*)(ii->cli_sections [i]) <= ((char*)image->raw_data + image->raw_data_len)))
				ii->cli_sections [i] = NULL;

		g_free (image->raw_data);
	}
	g_free (image->name);
	g_free (image->guid);
	g_free (image->files);

	g_hash_table_destroy (image->method_cache);
	g_hash_table_destroy (image->class_cache);
	g_hash_table_destroy (image->field_cache);
	g_hash_table_destroy (image->array_cache);
	g_hash_table_foreach (image->name_cache, free_hash_table, NULL);
	g_hash_table_destroy (image->name_cache);
	g_hash_table_destroy (image->native_wrapper_cache);
	g_hash_table_destroy (image->managed_wrapper_cache);
	g_hash_table_destroy (image->delegate_begin_invoke_cache);
	g_hash_table_destroy (image->delegate_end_invoke_cache);
	g_hash_table_destroy (image->delegate_invoke_cache);
	g_hash_table_foreach (image->remoting_invoke_cache, free_remoting_wrappers, NULL);
	g_hash_table_destroy (image->remoting_invoke_cache);
	g_hash_table_destroy (image->runtime_invoke_cache);
	g_hash_table_destroy (image->typespec_cache);
	g_hash_table_destroy (image->generic_class_cache);
	g_hash_table_foreach (image->memberref_signatures, free_mr_signatures, NULL);
	g_hash_table_destroy (image->memberref_signatures);
	g_hash_table_foreach (image->helper_signatures, free_mr_signatures, NULL);
	g_hash_table_destroy (image->helper_signatures);
	
	if (image->raw_metadata != NULL)
		mono_raw_buffer_free (image->raw_metadata);
	
	if (image->image_info){
		MonoCLIImageInfo *ii = image->image_info;
		int i;

		for (i = 0; i < ii->cli_section_count; i++){
			if (!ii->cli_sections [i])
				continue;
			mono_raw_buffer_free (ii->cli_sections [i]);
		}
		if (ii->cli_section_tables)
			g_free (ii->cli_section_tables);
		if (ii->cli_sections)
			g_free (ii->cli_sections);
		g_free (image->image_info);
	}

	if (!image->dynamic)
		/* Dynamic images are GC_MALLOCed */
		g_free (image);
}

/** 
 * mono_image_strerror:
 * @status: an code indicating the result from a recent operation
 *
 * Returns: a string describing the error
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
	gboolean is_string=entry->name_is_string;

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

	if(level==0) {
		if((is_string==FALSE && entry->name_offset!=res_id) ||
		   (is_string==TRUE)) {
			return(NULL);
		}
	} else if (level==1) {
#if 0
		if(name!=NULL &&
		   is_string==TRUE && name!=lookup (entry->name_offset)) {
			return(NULL);
		}
#endif
	} else if (level==2) {
		if((is_string==FALSE && entry->name_offset!=lang_id) ||
		   (is_string==TRUE)) {
			return(NULL);
		}
	} else {
		g_assert_not_reached ();
	}

	if(entry->is_dir==TRUE) {
		MonoPEResourceDir *res_dir=(MonoPEResourceDir *)(((char *)root)+entry->dir_offset);
		MonoPEResourceDirEntry *sub_entries=(MonoPEResourceDirEntry *)(res_dir+1);
		guint32 entries, i;
		
		entries=res_dir->res_named_entries + res_dir->res_id_entries;

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
		MonoPEResourceDataEntry *data_entry=(MonoPEResourceDataEntry *)((char *)(root)+entry->dir_offset);
		
		return(data_entry);
	}
}

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

	info=image->image_info;
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
	
	entries=resource_dir->res_named_entries + resource_dir->res_id_entries;
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

guint32
mono_image_get_entry_point (MonoImage *image)
{
	return ((MonoCLIImageInfo*)image->image_info)->cli_cli_header.ch_entry_point;
}

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

MonoImage*
mono_image_load_file_for_image (MonoImage *image, int fileidx)
{
	char *base_dir, *name;
	MonoImage *res;
	MonoTableInfo  *t = &image->tables [MONO_TABLE_FILE];
	const char *fname;
	guint32 fname_id;

	if (fileidx < 1 || fileidx > t->rows)
		return NULL;

	if (image->files && image->files [fileidx - 1])
		return image->files [fileidx - 1];

	if (!image->files)
		image->files = g_new0 (MonoImage*, t->rows);

	fname_id = mono_metadata_decode_row_col (t, fileidx - 1, MONO_FILE_NAME);
	fname = mono_metadata_string_heap (image, fname_id);
	base_dir = g_path_get_dirname (image->name);
	name = g_build_filename (base_dir, fname, NULL);
	res = mono_image_open (name, NULL);
	if (res) {
		int i;
		/* g_print ("loaded file %s from %s (%p)\n", name, image->name, image->assembly); */
		res->assembly = image->assembly;
		for (i = 0; i < res->module_count; ++i) {
			if (res->modules [i] && !res->modules [i]->assembly)
				res->modules [i]->assembly = image->assembly;
		}

		image->files [fileidx - 1] = res;
	}
	g_free (name);
	g_free (base_dir);
	return res;
}

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

guint32
mono_image_strong_name_position (MonoImage *image, guint32 *size)
{
	MonoCLIImageInfo *iinfo = image->image_info;
	MonoPEDirEntry *de = &iinfo->cli_cli_header.ch_strong_name;
	const int top = iinfo->cli_section_count;
	MonoSectionTable *tables = iinfo->cli_section_tables;
	int i;
	guint32 addr = de->rva;
	
	if (size)
		*size = de->size;
	if (!de->size || !de->rva)
		return 0;
	for (i = 0; i < top; i++){
		if ((addr >= tables->st_virtual_address) &&
		    (addr < tables->st_virtual_address + tables->st_raw_data_size)){
			return tables->st_raw_data_ptr +
				(addr - tables->st_virtual_address);
		}
		tables++;
	}

	return 0;
}

const char*
mono_image_get_public_key (MonoImage *image, guint32 *size)
{
	const char *pubkey;
	guint32 len, tok;
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

const char*
mono_image_get_name (MonoImage *image)
{
	return image->assembly_name;
}

const char*
mono_image_get_filename (MonoImage *image)
{
	return image->name;
}

const MonoTableInfo*
mono_image_get_table_info (MonoImage *image, int table_id)
{
	if (table_id < 0 || table_id >= 64)
		return NULL;
	return &image->tables [table_id];
}

int
mono_image_get_table_rows (MonoImage *image, int table_id)
{
	if (table_id < 0 || table_id >= 64)
		return 0;
	return image->tables [table_id].rows;
}

int
mono_table_info_get_rows (const MonoTableInfo *table)
{
	return table->rows;
}

MonoAssembly* 
mono_image_get_assembly (MonoImage *image)
{
	return image->assembly;
}

gboolean
mono_image_is_dynamic (MonoImage *image)
{
	return image->dynamic;
}

gboolean
mono_image_has_authenticode_entry (MonoImage *image)
{
	MonoCLIImageInfo *iinfo = image->image_info;
	MonoDotNetHeader *header = &iinfo->cli_header;
	MonoPEDirEntry *de = &header->datadir.pe_certificate_table;
	// the Authenticode "pre" (non ASN.1) header is 8 bytes long
	return ((de->rva != 0) && (de->size > 8));
}
