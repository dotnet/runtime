
/*
 * reflection.c: Routines for creating an image at runtime.
 * 
 * Author:
 *   Paolo Molaro (lupus@ximian.com)
 *
 * (C) 2001 Ximian, Inc.  http://www.ximian.com
 *
 */
#include <config.h>
#include "mono/metadata/reflection.h"
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


static guint32
string_heap_insert (MonoStringHeap *sh, char *str)
{
	guint32 idx = GPOINTER_TO_UINT(g_hash_table_lookup (sh, str));
	guint32 len;
	if (idx)
		return idx;
	len = strlen (str) + 1;
	idx = sh->index;
	if (idx + len > sh->alloc_size) {
		sh->alloc_size += len + 4096;
		sh->data = g_realloc (sh->data, sh->alloc_size);
	}
	/*
	 * We strdup the string even if we already copy them in sh->data
	 * so that the string pointers in the hash remain valid even if
	 * we need to realloc sh->data. We may want to avoid that later.
	 */
	g_hash_table_insert (sh->hash, g_strdup (str), GUINT_TO_POINTER (idx));
	memcpy (sh->data + idx, str, len);
	sh->index += len;
	return idx;
}

static void
string_heap_init (MonoStringHeap *sh)
{
	sh->index = 0;
	sh->alloc_size = 4096;
	sh->data = g_malloc (4096);
	sh->hash = g_hash_table_new (g_str_hash, g_str_equal);
	string_heap_insert (sh, "");
}

static void
string_heap_free (MonoStringHeap *sh)
{
	g_free (sh->data);
	g_hash_table_foreach (sh->hash, g_free, NULL);
	g_hash_table_destroy (sh->hash);
}

static void
mono_image_add_blob_data (MonoDynamicAssembly *assembly, char *data, guint32 len)
{
	if (assembly->blob.alloc_size < assembly->blob.index + len) {
		assembly->blob.alloc_size += len + 4096;
		assembly->blob.data = g_realloc (assembly->blob.data, assembly->blob.alloc_size);
	}
	memcpy (assembly->blob.data + assembly->blob.index, data, len);
	assembly->blob.index += len;
}

static void
mono_image_get_type_info (MonoTypeBuilder *tb, MonoDynamicAssembly *assembly)
{
	MonoDynamicTable *table;

	assembly->tables [MONO_TABLE_METHOD].rows += g_list_length (tb->methods);
	if (!tb->has_default_ctor)
		assembly->tables [MONO_TABLE_METHOD].rows++;
	/* Do the same with fields, properties etc.. */
}

static void
mono_image_fill_module_table (MonoModuleBuilder *mb, MonoDynamicAssembly *assembly)
{
	MonoDynamicTable *table;

	table = &assembly->tables [MONO_TABLE_MODULE];
	/* FIXME: handle multiple modules */
	table->values [MONO_MODULE_NAME] = string_heap_insert (&assembly->sheap, mb->name);
	/* need to set mvid? */

	/*
	 * fill-in info in other tables as well.
	 */
	assembly->tables [MONO_TABLE_TYPEDEF].rows += g_list_length (mb->types);
}

static void
mono_image_build_metadata (MonoDynamicAssembly *assembly)
{
	char *meta;
	MonoDynamicTable *table;
	GList *type;
	guint32 len;
	
	/*
	 * FIXME: check if metadata was already built. 
	 */
	string_heap_init (&assembly->sheap);
	
	table = &assembly->tables [MONO_TABLE_ASSEMBLY];
	table->rows = 1;
	table->values = g_malloc (table->rows * MONO_ASSEMBLY_SIZE);
	table->values [MONO_ASSEMBLY_HASH_ALG] = 0x8004;
	table->values [MONO_ASSEMBLY_NAME] = string_heap_insert (&assembly->sheap, assembly->name);

	assembly->tables [MONO_TABLE_TYPEDEF].rows = 1; /* .<Module>*/
	len = g_list_length (assembly->modules);
	table = &assembly->tables [MONO_TABLE_MODULE];
	table->rows = len;
	table->values = g_malloc (table->rows * MONO_MODULE_SIZE);
	g_list_foreach (assembly->modules, mono_image_fill_module_table, assembly);

	table = &assembly->tables [MONO_TABLE_TYPEDEF];
	/* 
	 * table->rows is already set above and in mono_image_fill_module_table.
	 */
	table->values = g_malloc (table->rows * MONO_TYPEDEF_SIZE);
	/*
	 * Set the first entry.
	 */
	table->values [MONO_TYPEDEF_FLAGS] = 0;
	table->values [MONO_TYPEDEF_NAME] = string_heap_insert (&assembly->sheap, "<Module>") ;
	table->values [MONO_TYPEDEF_NAMESPACE] = string_heap_insert (&assembly->sheap, "") ;
	table->values [MONO_TYPEDEF_EXTENDS] = 0;
	table->values [MONO_TYPEDEF_FIELD_LIST] = 1;
	table->values [MONO_TYPEDEF_METHOD_LIST] = 1;

}

int
mono_image_get_header (MonoDynamicAssembly *assembly, char *buffer, int maxsize)
{
	MonoMSDOSHeader *msdos;
	MonoDotNetHeader *header;
	MonoSectionTable *section;
	static const unsigned char msheader[] = {
		0x4d, 0x5a, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00,  0x04, 0x00, 0x00, 0x00, 0xff, 0xff, 0x00, 0x00,
		0xb8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,  0x40, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,  0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,  0x00, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00,
		0x0e, 0x1f, 0xba, 0x0e, 0x00, 0xb4, 0x09, 0xcd,  0x21, 0xb8, 0x01, 0x4c, 0xcd, 0x21, 0x54, 0x68,
		0x69, 0x73, 0x20, 0x70, 0x72, 0x6f, 0x67, 0x72,  0x61, 0x6d, 0x20, 0x63, 0x61, 0x6e, 0x6e, 0x6f,
		0x74, 0x20, 0x62, 0x65, 0x20, 0x72, 0x75, 0x6e,  0x20, 0x69, 0x6e, 0x20, 0x44, 0x4f, 0x53, 0x20,
		0x6d, 0x6f, 0x64, 0x65, 0x2e, 0x0d, 0x0d, 0x0a,  0x24, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
	};

	if (maxsize < sizeof (MonoMSDOSHeader) + sizeof (MonoDotNetHeader) + sizeof (MonoSectionTable))
		return -1;

	memcpy (buffer, msheader, sizeof (MonoMSDOSHeader));
	memset (buffer + sizeof (MonoMSDOSHeader), 0, sizeof (MonoDotNetHeader) + sizeof (MonoSectionTable));

	msdos = (MonoMSDOSHeader *)buffer;
	header = (MonoDotNetHeader *)(buffer + sizeof (MonoMSDOSHeader));
	section = (MonoSectionTable*) (buffer + sizeof (MonoMSDOSHeader) + sizeof (MonoDotNetHeader));

	/* FIXME: ENDIAN problem: byteswap as needed */
	msdos->pe_offset = sizeof (MonoMSDOSHeader);

	header->pesig [0] = 'P';
	header->pesig [1] = 'E';
	header->pesig [2] = header->pesig [3] = 0;

	header->coff.coff_machine = 0x14c;
	header->coff.coff_sections = 1; /* only text supported now */
	header->coff.coff_time = time (NULL);
	header->coff.coff_opt_header_size = sizeof (MonoDotNetHeader) - sizeof (MonoCOFFHeader) - 4;
	/* it's an exe */
	header->coff.coff_attributes = 0x010e;
	/* it's a dll */
	//header->coff.coff_attributes = 0x210e;
	header->pe.pe_magic = 0x10B;
	header->pe.pe_major = 6;
	header->pe.pe_minor = 0;
	/* need to set: pe_code_size pe_data_size pe_rva_entry_point pe_rva_code_base pe_rva_data_base */

	header->nt.pe_image_base = 0x400000;
	header->nt.pe_section_align = 8192;
	header->nt.pe_file_alignment = 512;
	header->nt.pe_os_major = 4;
	header->nt.pe_os_minor = 0;
	header->nt.pe_subsys_major = 4;
	/* need to set pe_image_size, pe_header_size */
	header->nt.pe_subsys_required = 3; /* 3 -> cmdline app, 2 -> GUI app */
	header->nt.pe_stack_reserve = 0x00100000;
	header->nt.pe_stack_commit = 0x00001000;
	header->nt.pe_heap_reserve = 0x00100000;
	header->nt.pe_heap_commit = 0x00001000;
	header->nt.pe_loader_flags = 1;
	header->nt.pe_data_dir_count = 16;

#if 0
	/* set: */
	header->datadir.pe_import_table
	pe_resource_table
	pe_reloc_table
	pe_iat	
#endif
	header->datadir.pe_cli_header.size = 0x48;
	header->datadir.pe_cli_header.rva = 0x00002008;

	/* Write section tables */
	strcpy (section->st_name, ".text");
	section->st_virtual_size = 1024;
	section->st_virtual_address = 0x00002000;
	section->st_raw_data_size = 1024;
	section->st_raw_data_ptr = 0x00000100;
	section->st_flags = SECT_FLAGS_HAS_CODE | SECT_FLAGS_MEM_EXECUTE | SECT_FLAGS_MEM_READ;

	return sizeof (MonoMSDOSHeader) + sizeof (MonoDotNetHeader) + sizeof (MonoSectionTable);;
}

