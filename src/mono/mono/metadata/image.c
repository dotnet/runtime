/*
 * image.c: Routines for manipulating an image stored in an
 * extended PE/COFF file.
 * 
 * Author:
 *   Miguel de Icaza (miguel@ximian.com)
 *
 * (C) 2001 Ximian, Inc.  http://www.ximian.com
 *
 * TODO:
 *   Implement big-endian versions of the reading routines.
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

#define INVALID_ADDRESS 0xffffffff

/*
 * Keeps track of the various assemblies loaded
 */
static GHashTable *loaded_images_hash;

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
mono_cli_rva_map (MonoCLIImageInfo *iinfo, guint32 addr)
{
	const int top = iinfo->cli_section_count;
	MonoSectionTable *tables = iinfo->cli_section_tables;
	int i;
	
	for (i = 0; i < top; i++){
		if ((addr >= tables->st_virtual_address) &&
		    (addr < tables->st_virtual_address + tables->st_raw_data_size)){
			return (char*)iinfo->cli_sections [i] +
				(addr - tables->st_virtual_address);
		}
		tables++;
	}
	return NULL;
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
load_section_tables (MonoImage *image, MonoCLIImageInfo *iinfo)
{
	const int top = iinfo->cli_header.coff.coff_sections;
	int i;

	iinfo->cli_section_count = top;
	iinfo->cli_section_tables = g_new0 (MonoSectionTable, top);
	iinfo->cli_sections = g_new0 (void *, top);
	
	for (i = 0; i < top; i++){
		MonoSectionTable *t = &iinfo->cli_section_tables [i];
		
		if (fread (t, sizeof (MonoSectionTable), 1, image->f) != 1)
			return FALSE;

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

	for (i = 0; i < top; i++)
		if (!mono_image_ensure_section_idx (image, i))
			return FALSE;
	
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

	if (fseek (image->f, offset, SEEK_SET) != 0)
		return FALSE;
	
	if ((n = fread (&iinfo->cli_cli_header, sizeof (MonoCLIHeader), 1, image->f)) != 1)
		return FALSE;

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
	char *ptr;
	
	offset = mono_cli_rva_image_map (iinfo, iinfo->cli_cli_header.ch_metadata.rva);
	size = iinfo->cli_cli_header.ch_metadata.size;
	
	image->raw_metadata = mono_raw_buffer_load (fileno (image->f), FALSE, offset, size);
	if (image->raw_metadata == NULL)
		return FALSE;

	ptr = image->raw_metadata;

	if (strncmp (ptr, "BSJB", 4) == 0){
		guint32 version_string_len;

		ptr += 12;
		version_string_len = read32 (ptr);
		ptr += 4;
		ptr += version_string_len;
		if (((guint32) ptr) % 4)
			ptr += 4 - (((guint32) ptr) %4);
	} else
		return FALSE;

	/* skip over flags */
	ptr += 2;
	
	streams = read16 (ptr);
	ptr += 2;

	for (i = 0; i < streams; i++){
		if (strncmp (ptr + 8, "#~", 3) == 0){
			image->heap_tables.offset = read32 (ptr);
			image->heap_tables.size = read32 (ptr + 4);
			ptr += 8 + 3;
		} else if (strncmp (ptr + 8, "#Strings", 9) == 0){
			image->heap_strings.offset = read32 (ptr);
			image->heap_strings.size = read32 (ptr + 4);
			ptr += 8 + 9;
		} else if (strncmp (ptr + 8, "#US", 4) == 0){
			image->heap_us.offset = read32 (ptr);
			image->heap_us.size = read32 (ptr + 4);
			ptr += 8 + 4;
		} else if (strncmp (ptr + 8, "#Blob", 6) == 0){
			image->heap_blob.offset = read32 (ptr);
			image->heap_blob.size = read32 (ptr + 4);
			ptr += 8 + 6;
		} else if (strncmp (ptr + 8, "#GUID", 6) == 0){
			image->heap_guid.offset = read32 (ptr);
			image->heap_guid.size = read32 (ptr + 4);
			ptr += 8 + 6;
		} else {
			g_message ("Unknown heap type: %s\n", ptr + 8);
			ptr += 8 + strlen (ptr) + 1;
		}
		if (((guint32)ptr) % 4){
			ptr += 4 - (((guint32)ptr) % 4);
		}
	}
	return TRUE;
}

/*
 * Load representation of logical metadata tables, from the "#~" stream
 */
static gboolean
load_tables (MonoImage *image)
{
	char *heap_tables = image->raw_metadata + image->heap_tables.offset;
	guint32 *rows;
	guint64 valid_mask;
	int valid = 0, table;
	int heap_sizes;
	
	heap_sizes = heap_tables [6];
	image->idx_string_wide = ((heap_sizes & 0x01) == 1);
	image->idx_guid_wide   = ((heap_sizes & 0x02) == 2);
	image->idx_blob_wide   = ((heap_sizes & 0x04) == 4);
	
	valid_mask = read64 (heap_tables + 8);
	rows = (guint32 *) (heap_tables + 24);
	
	for (table = 0; table < 64; table++){
		if ((valid_mask & ((guint64) 1 << table)) == 0){
			image->tables [table].rows = 0;
			continue;
		}
		if (table > 0x2b) {
			g_warning("bits in valid must be zero above 0x2b (II - 23.1.6)");
		}
		image->tables [table].rows = read32 (rows);
		rows++;
		valid++;
	}

	image->tables_base = (heap_tables + 24) + (4 * valid);

	/* They must be the same */
	g_assert ((void *) image->tables_base == (void *) rows);

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

static void
load_class_names (MonoImage *image) {
	MonoTableInfo  *t = &image->tables [MONO_TABLE_TYPEDEF];
	guint32 cols [MONO_TYPEDEF_SIZE];
	const char* name;
	const char *nspace;
	GHashTable *nspace_table;
	GHashTable *name_cache = image->name_cache;
	guint32 i;

	for (i = 1; i <= t->rows; ++i) {
		mono_metadata_decode_row (t, i - 1, cols, MONO_TYPEDEF_SIZE);
		name = mono_metadata_string_heap (image, cols [MONO_TYPEDEF_NAME]);
		nspace = mono_metadata_string_heap (image, cols [MONO_TYPEDEF_NAMESPACE]);
		if (!(nspace_table = g_hash_table_lookup (name_cache, nspace))) {
			nspace_table = g_hash_table_new (g_str_hash, g_str_equal);
			g_hash_table_insert (name_cache, nspace, nspace_table);
		}
		g_hash_table_insert (nspace_table, name, GUINT_TO_POINTER (i));
	}
}

static MonoImage *
do_mono_image_open (const char *fname, enum MonoImageOpenStatus *status)
{
	MonoCLIImageInfo *iinfo;
	MonoDotNetHeader *header;
	MonoMSDOSHeader msdos;
	MonoImage *image;
	int n;

	image = g_new0 (MonoImage, 1);
	image->f = fopen (fname, "rb");
	image->name = g_strdup (fname);
	iinfo = g_new0 (MonoCLIImageInfo, 1);
	image->image_info = iinfo;

	image->method_cache = g_hash_table_new (g_direct_hash, g_direct_equal);
	image->class_cache = g_hash_table_new (g_direct_hash, g_direct_equal);
	image->name_cache = g_hash_table_new (g_str_hash, g_str_equal);
	image->array_cache = g_hash_table_new (g_direct_hash, g_direct_equal);

	header = &iinfo->cli_header;
		
	if (image->f == NULL){
		if (status)
			*status = MONO_IMAGE_ERROR_ERRNO;
		mono_image_close (image);
		return NULL;
	}

	if (status)
		*status = MONO_IMAGE_IMAGE_INVALID;
	
	if (fread (&msdos, sizeof (msdos), 1, image->f) != 1)
		goto invalid_image;
	
	if (!(msdos.msdos_header [0] == 'M' && msdos.msdos_header [1] == 'Z'))
		goto invalid_image;
	
	msdos.pe_offset = GUINT32_FROM_LE (msdos.pe_offset);

	if (msdos.pe_offset != sizeof (msdos))
		fseek (image->f, msdos.pe_offset, SEEK_SET);
	
	if ((n = fread (header, sizeof (MonoDotNetHeader), 1, image->f)) != 1)
		goto invalid_image;

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

	if (header->pe.pe_major != 6 || header->pe.pe_minor != 0)
		goto invalid_image;

	/*
	 * FIXME: byte swap all addresses here for header.
	 */
	
	if (!load_section_tables (image, iinfo))
		goto invalid_image;
	
	/* Load the CLI header */
	if (!load_cli_header (image, iinfo))
		goto invalid_image;

	if (!load_metadata (image, iinfo))
		goto invalid_image;

	load_class_names (image);

	image->assembly_name = mono_metadata_string_heap (image, 
			mono_metadata_decode_row_col (&image->tables [MONO_TABLE_ASSEMBLY],
					0, MONO_ASSEMBLY_NAME));

	if (status)
		*status = MONO_IMAGE_OK;

	return image;

invalid_image:
	mono_image_close (image);
		return NULL;
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
mono_image_open (const char *fname, enum MonoImageOpenStatus *status)
{
	MonoImage *image;
	
	g_return_val_if_fail (fname != NULL, NULL);

	if (loaded_images_hash){
		image = g_hash_table_lookup (loaded_images_hash, fname);
		if (image){
			image->ref_count++;
			return image;
		}
	}

	image = do_mono_image_open (fname, status);
	if (image == NULL)
		return NULL;

	if (!loaded_images_hash)
		loaded_images_hash = g_hash_table_new (g_str_hash, g_str_equal);
	g_hash_table_insert (loaded_images_hash, image->name, image);

	return image;
}

static void
free_hash_table(gpointer key, gpointer val, gpointer user_data)
{
	g_hash_table_destroy ((GHashTable*)val);
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
	g_return_if_fail (image != NULL);

	if (--image->ref_count)
		return;

	g_hash_table_remove (loaded_images_hash, image->name);
	
	if (image->f)
		fclose (image->f);

	g_free (image->name);

	g_hash_table_destroy (image->method_cache);
	g_hash_table_destroy (image->class_cache);
	g_hash_table_destroy (image->array_cache);
	g_hash_table_foreach (image->name_cache, free_hash_table, NULL);
	g_hash_table_destroy (image->name_cache);
	
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
	
	g_free (image);
}

/** 
 * mono_image_strerror:
 * @status: an code indicating the result from a recent operation
 *
 * Returns: a string describing the error
 */
const char *
mono_image_strerror (enum MonoImageOpenStatus status)
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

