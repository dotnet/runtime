/*
 * assembly.c: Routines for manipulating and assembly stored in an
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
#include <string.h>
#include "assembly.h"
#include "cil-coff.h"
#include "rawbuffer.h"
#include "endian.h"

#define INVALID_ADDRESS 0xffffffff

guint32
cli_rva_image_map (cli_image_info_t *iinfo, guint32 addr)
{
	const int top = iinfo->cli_section_count;
	section_table_t *tables = iinfo->cli_section_tables;
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
cli_rva_map (cli_image_info_t *iinfo, guint32 addr)
{
	const int top = iinfo->cli_section_count;
	section_table_t *tables = iinfo->cli_section_tables;
	int i;
	
	for (i = 0; i < top; i++){
		if ((addr >= tables->st_virtual_address) &&
		    (addr < tables->st_virtual_address + tables->st_raw_data_size)){
			return iinfo->cli_sections [i] +
				(addr - tables->st_virtual_address);
		}
		tables++;
	}
	return NULL;
}

/**
 * mono_assembly_ensure_section_idx:
 * @assembly: The image we are operating on
 * @section: section number that we will load/map into memory
 *
 * This routine makes sure that we have an in-memory copy of
 * an image section (.text, .rsrc, .data).
 *
 * Returns: TRUE on success
 */
int
mono_assembly_ensure_section_idx (MonoAssembly *assembly, int section)
{
	cli_image_info_t *iinfo = assembly->image_info;
	section_table_t *sect;
	gboolean writable;
	
	g_return_val_if_fail (section < iinfo->cli_section_count, FALSE);

	if (iinfo->cli_sections [section] != NULL)
		return TRUE;

	sect = &iinfo->cli_section_tables [section];
	
	writable = sect->st_flags & SECT_FLAGS_MEM_WRITE;

	iinfo->cli_sections [section] = raw_buffer_load (
		fileno (assembly->f), writable,
		sect->st_raw_data_ptr, sect->st_raw_data_size);

	if (iinfo->cli_sections [section] == NULL)
		return FALSE;

	return TRUE;
}

/**
 * mono_assembly_ensure_section:
 * @assembly: The image we are operating on
 * @section: section name that we will load/map into memory
 *
 * This routine makes sure that we have an in-memory copy of
 * an image section (.text, .rsrc, .data).
 *
 * Returns: TRUE on success
 */
int
mono_assembly_ensure_section (MonoAssembly *assembly, const char *section)
{
	cli_image_info_t *ii = assembly->image_info;
	int i;
	
	for (i = 0; i < ii->cli_section_count; i++){
		if (strncmp (ii->cli_section_tables [i].st_name, section, 8) != 0)
			continue;
		
		return mono_assembly_ensure_section_idx (assembly, i);
	}
	return FALSE;
}

static int
load_section_tables (MonoAssembly *assembly, cli_image_info_t *iinfo)
{
	const int top = iinfo->cli_header.coff.coff_sections;
	int i;

	iinfo->cli_section_count = top;
	iinfo->cli_section_tables = g_new (section_table_t, top);
	iinfo->cli_sections = g_new0 (void *, top);
	
	for (i = 0; i < top; i++){
		section_table_t *t = &iinfo->cli_section_tables [i];
		
		if (fread (t, sizeof (section_table_t), 1, assembly->f) != 1)
			return FALSE;

		t->st_virtual_size = le32_to_cpu (t->st_virtual_size);
		t->st_virtual_address = le32_to_cpu (t->st_virtual_address);
		t->st_raw_data_size = le32_to_cpu (t->st_raw_data_size);
		t->st_raw_data_ptr = le32_to_cpu (t->st_raw_data_ptr);
		t->st_reloc_ptr = le32_to_cpu (t->st_reloc_ptr);
		t->st_lineno_ptr = le32_to_cpu (t->st_lineno_ptr);
		t->st_reloc_count = le16_to_cpu (t->st_reloc_count);
		t->st_line_count = le16_to_cpu (t->st_line_count);
	}

	for (i = 0; i < top; i++)
		if (!mono_assembly_ensure_section_idx (assembly, i))
			return FALSE;
	
	return TRUE;
}

static gboolean
load_cli_header (MonoAssembly *assembly, cli_image_info_t *iinfo)
{
	guint32 offset;
	int n;
	
	offset = cli_rva_image_map (iinfo, iinfo->cli_header.datadir.pe_cli_header.rva);
	if (offset == INVALID_ADDRESS)
		return FALSE;

	if (fseek (assembly->f, offset, 0) != 0)
		return FALSE;
	
	if ((n = fread (&iinfo->cli_cli_header, sizeof (cli_header_t), 1, assembly->f)) != 1)
		return FALSE;

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
		g_message ("Some fields in the CLI header which should have been zero are not zero");
	}
	    
	return TRUE;
}

static gboolean
load_metadata_ptrs (MonoAssembly *assembly, cli_image_info_t *iinfo)
{
	metadata_t *metadata = &iinfo->cli_metadata;
	guint32 offset, size;
	guint16 streams;
	int i;
	char *ptr;
	
	offset = cli_rva_image_map (iinfo, iinfo->cli_cli_header.ch_metadata.rva);
	size = iinfo->cli_cli_header.ch_metadata.size;
	
	metadata->raw_metadata = raw_buffer_load (fileno (assembly->f), FALSE, offset, size);
	if (metadata->raw_metadata == NULL)
		return FALSE;

	ptr = metadata->raw_metadata;

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
			metadata->heap_tables.sh_offset = read32 (ptr);
			metadata->heap_tables.sh_size = read32 (ptr + 4);
			ptr += 8 + 3;
		} else if (strncmp (ptr + 8, "#Strings", 9) == 0){
			metadata->heap_strings.sh_offset = read32 (ptr);
			metadata->heap_strings.sh_size = read32 (ptr + 4);
			ptr += 8 + 9;
		} else if (strncmp (ptr + 8, "#US", 4) == 0){
			metadata->heap_us.sh_offset = read32 (ptr);
			metadata->heap_us.sh_size = read32 (ptr + 4);
			ptr += 8 + 4;
		} else if (strncmp (ptr + 8, "#Blob", 6) == 0){
			metadata->heap_blob.sh_offset = read32 (ptr);
			metadata->heap_blob.sh_size = read32 (ptr + 4);
			ptr += 8 + 6;
		} else if (strncmp (ptr + 8, "#GUID", 6) == 0){
			metadata->heap_guid.sh_offset = read32 (ptr);
			metadata->heap_guid.sh_size = read32 (ptr + 4);
			ptr += 8 + 6;
		} else
			g_message ("Unknown heap type: %s\n", ptr + 8);
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
load_tables (MonoAssembly *assembly, metadata_t *meta)
{
	char *heap_tables = meta->raw_metadata + meta->heap_tables.sh_offset;
	guint32 *rows;
	guint64 valid_mask;
	int valid = 0, table;
	int heap_sizes;
	
	heap_sizes = heap_tables [6];
	meta->idx_string_wide = ((heap_sizes & 0x01) == 1);
	meta->idx_guid_wide   = ((heap_sizes & 0x02) == 2);
	meta->idx_blob_wide   = ((heap_sizes & 0x04) == 4);
	
	valid_mask = read64 (heap_tables + 8);
	rows = (guint32 *) (heap_tables + 24);
	
	for (table = 0; table < 64; table++){
		if ((valid_mask & ((guint64) 1 << table)) == 0){
			meta->tables [table].rows = 0;
			continue;
		}
		meta->tables [table].rows = read32 (rows);
		rows++;
		valid++;
	}

	meta->tables_base = (heap_tables + 24) + (4 * valid);

	/* They must be the same */
	g_assert ((void *) meta->tables_base == (void *) rows);

	mono_metadata_compute_table_bases (meta);
	return TRUE;
}

static gboolean
load_metadata (MonoAssembly *assembly, cli_image_info_t *iinfo)
{
	if (!load_metadata_ptrs (assembly, iinfo))
		return FALSE;

	return load_tables (assembly, &iinfo->cli_metadata);
}

/**
 * mono_assembly_open:
 * @fname: filename that points to the module we want to open
 * @status: An error condition is returned in this field
 *
 * Retuns: An open assembly of type %MonoAssembly or NULL on error.
 * if NULL, then check the value of @status for details on the error
 */
MonoAssembly *
mono_assembly_open (const char *fname, enum MonoAssemblyOpenStatus *status)
{
	cli_image_info_t *iinfo;
	dotnet_header_t *header;
	msdos_header_t msdos;
	MonoAssembly *assembly;
	int n;

	assembly = g_new (MonoAssembly, 1);
	assembly->f = fopen (fname, "r");
	iinfo = g_new (cli_image_info_t, 1);
	assembly->image_info = iinfo;

	header = &iinfo->cli_header;
		
	if (assembly->f == NULL){
		if (status)
			*status = MONO_ASSEMBLY_ERROR_ERRNO;
		mono_assembly_close (assembly);
		return NULL;
	}

	if (status)
		*status = MONO_ASSEMBLY_IMAGE_INVALID;
	
	if (fread (&msdos, sizeof (msdos), 1, assembly->f) != 1)
		goto invalid_image;
	
	if (!(msdos.msdos_header [0] == 0x4d && msdos.msdos_header [1] == 0x5a))
		goto invalid_image;
	
	if ((n = fread (header, sizeof (dotnet_header_t), 1, assembly->f)) != 1)
		goto invalid_image;

	/*
	 * FIXME: byte swap all addresses here for header.
	 */
	
	if (!load_section_tables (assembly, iinfo))
		goto invalid_image;
	
	/* Load the CLI header */
	if (!load_cli_header (assembly, iinfo))
		goto invalid_image;

	if (!load_metadata (assembly, iinfo))
		goto invalid_image;
	
	if (status)
		*status = MONO_ASSEMBLY_OK;

	return assembly;

invalid_image:
	mono_assembly_close (assembly);
		return NULL;
}

/**
 * mono_assembly_close:
 * @assembly: The image file we wish to close
 *
 * Closes an image file, deallocates all memory consumed and
 * unmaps all possible sections of the file
 */
void
mono_assembly_close (MonoAssembly *assembly)
{
	g_return_if_fail (assembly != NULL);

	if (assembly->f)
		fclose (assembly->f);

	if (assembly->image_info){
		cli_image_info_t *ii = assembly->image_info;
		int i;

		if (ii->cli_metadata.raw_metadata != NULL)
			raw_buffer_free (ii->cli_metadata.raw_metadata);
	
		for (i = 0; i < ii->cli_section_count; i++){
			if (!ii->cli_sections [i])
				continue;
			raw_buffer_free (ii->cli_sections [i]);
		}
		if (ii->cli_section_tables)
			g_free (ii->cli_section_tables);
		if (ii->cli_sections)
			g_free (ii->cli_section_tables);
		g_free (assembly->image_info);
	}
	
	g_free (assembly);
}

/** 
 * mono_assembly_strerror:
 * @status: an code indicating the result from a recent operation
 *
 * Returns: a string describing the error
 */
const char *
mono_assembly_strerror (enum MonoAssemblyOpenStatus status)
{
	switch (status){
	case MONO_ASSEMBLY_OK:
		return "success";
	case MONO_ASSEMBLY_ERROR_ERRNO:
		return strerror (errno);
	case MONO_ASSEMBLY_IMAGE_INVALID:
		return "File does not contain a valid CIL image";
	}
	return "Internal error";
}

