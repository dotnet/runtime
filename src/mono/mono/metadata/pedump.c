/*
 * pedump.c: Dumps the contents of an extended PE/COFF file
 *
 * Author:
 *   Miguel de Icaza (miguel@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */
#include <config.h>
#include <stdio.h>
#include <string.h>
#include "image.h"
#include <glib.h>
#include "cil-coff.h"

gboolean dump_data = TRUE;
gboolean dump_tables = FALSE;

static void
hex_dump (char *buffer, int base, int count)
{
	int i;
	
	for (i = 0; i < count; i++){
		if ((i % 16) == 0)
			printf ("\n0x%08x: ", (unsigned char) base + i);

		printf ("%02x ", (unsigned char) (buffer [i]));
	}
}

static void
hex8 (char *label, unsigned char x)
{
	printf ("\t%s: 0x%02x\n", label, (unsigned char) x);
}

static void
hex16 (char *label, guint16 x)
{
	printf ("\t%s: 0x%04x\n", label, x);
}

static void
hex32 (char *label, guint32 x)
{
	printf ("\t%s: 0x%08x\n", label, x);
}

static void
dump_coff_header (coff_header_t *coff)
{
	printf ("\nCOFF Header:\n");
	hex16 ("                Machine", coff->coff_machine);
	hex16 ("               Sections", coff->coff_sections);
	hex32 ("             Time stamp", coff->coff_time);
	hex32 ("Pointer to Symbol Table", coff->coff_symptr);
	hex32 ("   	   Symbol Count", coff->coff_symcount);
	hex16 ("   Optional Header Size", coff->coff_opt_header_size);
	hex16 ("   	Characteristics", coff->coff_attributes);

}

static void
dump_pe_header (pe_header_t *pe)
{
	printf ("\nPE Header:\n");
	hex16 ("         Magic (0x010b)", pe->pe_magic);
	hex8  ("             LMajor (6)", pe->pe_major);
	hex8  ("             LMinor (0)", pe->pe_minor);
	hex32 ("              Code Size", pe->pe_code_size);
	hex32 ("  Initialized Data Size", pe->pe_data_size);
	hex32 ("Uninitialized Data Size", pe->pe_uninit_data_size);
	hex32 ("        Entry Point RVA", pe->pe_rva_entry_point);
	hex32 (" 	  Code Base RVA", pe->pe_rva_code_base);
	hex32 ("	  Data Base RVA", pe->pe_rva_data_base);
	printf ("\n");
}

static void
dump_nt_header (pe_header_nt_t *nt)
{
	printf ("\nNT Header:\n");

	hex32 ("   Image Base (0x400000)", nt->pe_image_base);
	hex32 ("Section Alignment (8192)", nt->pe_section_align);
	hex32 ("   File Align (512/4096)", nt->pe_file_alignment);
	hex16 ("            OS Major (4)", nt->pe_os_major);
	hex16 ("            OS Minor (0)", nt->pe_os_minor);
	hex16 ("  	  User Major (0)", nt->pe_user_major);
	hex16 ("  	  User Minor (0)", nt->pe_user_minor);
	hex16 ("  	Subsys major (4)", nt->pe_subsys_major);
	hex16 ("  	Subsys minor (0)", nt->pe_subsys_minor);
	hex32 (" 	       Reserverd", nt->pe_reserved_1);
	hex32 (" 	      Image Size", nt->pe_image_size);
	hex32 (" 	     Header Size", nt->pe_header_size);
	hex32 ("            Checksum (0)", nt->pe_checksum);
	hex16 ("               Subsystem", nt->pe_subsys_required);
	hex16 ("           DLL Flags (0)", nt->pe_dll_flags);
	hex32 (" Stack Reserve Size (1M)", nt->pe_stack_reserve);
	hex32 ("Stack commit Size (4096)", nt->pe_stack_commit);
	hex32 ("  Heap Reserve Size (1M)", nt->pe_heap_reserve);
	hex32 (" Heap Commit Size (4096)", nt->pe_heap_commit);
	hex32 ("      Loader flags (0x1)", nt->pe_loader_flags);
	hex32 ("   Data Directories (16)", nt->pe_data_dir_count);
}

static void
dent (const char *label, pe_dir_entry_t de)
{
	printf ("\t%s: 0x%08x [0x%08x]\n", label, de.rva, de.size);
}

static void
dump_datadir (pe_datadir_t *dd)
{
	printf ("\nData directories:\n");
	dent ("     Export Table", dd->pe_export_table);
	dent ("     Import Table", dd->pe_import_table);
	dent ("   Resource Table", dd->pe_resource_table);
	dent ("  Exception Table", dd->pe_exception_table);
	dent ("Certificate Table", dd->pe_certificate_table);
	dent ("      Reloc Table", dd->pe_reloc_table);
	dent ("            Debug", dd->pe_debug);
	dent ("        Copyright", dd->pe_copyright);
	dent ("       Global Ptr", dd->pe_global_ptr);
	dent ("        TLS Table", dd->pe_tls_table);
	dent ("Load Config Table", dd->pe_load_config_table);
	dent ("     Bound Import", dd->pe_bound_import);
	dent ("              IAT", dd->pe_iat);
	dent ("Delay Import Desc", dd->pe_delay_import_desc);
	dent ("       CLI Header", dd->pe_cli_header);
}

static void
dump_dotnet_header (dotnet_header_t *header)
{
	dump_coff_header (&header->coff);
	dump_pe_header (&header->pe);
	dump_nt_header (&header->nt);
	dump_datadir (&header->datadir);
}

static void
dump_section_table (section_table_t *st)
{
	guint32 flags = st->st_flags;
		
	printf ("\n\tName: %s\n", st->st_name);
	hex32 ("   Virtual Size", st->st_virtual_size);
	hex32 ("Virtual Address", st->st_virtual_address);
	hex32 ("  Raw Data Size", st->st_raw_data_size);
	hex32 ("   Raw Data Ptr", st->st_raw_data_ptr);
	hex32 ("      Reloc Ptr", st->st_reloc_ptr);
	hex32 ("     LineNo Ptr", st->st_lineno_ptr);
	hex16 ("    Reloc Count", st->st_reloc_count);
	hex16 ("     Line Count", st->st_line_count);

	printf ("\tFlags: %s%s%s%s%s%s%s%s%s%s\n",
		(flags & SECT_FLAGS_HAS_CODE) ? "code, " : "",
		(flags & SECT_FLAGS_HAS_INITIALIZED_DATA) ? "data, " : "",
		(flags & SECT_FLAGS_HAS_UNINITIALIZED_DATA) ? "bss, " : "",
		(flags & SECT_FLAGS_MEM_DISCARDABLE) ? "discard, " : "",
		(flags & SECT_FLAGS_MEM_NOT_CACHED) ? "nocache, " : "",
		(flags & SECT_FLAGS_MEM_NOT_PAGED) ? "nopage, " : "",
		(flags & SECT_FLAGS_MEM_SHARED) ? "shared, " : "",
		(flags & SECT_FLAGS_MEM_EXECUTE) ? "exec, " : "",
		(flags & SECT_FLAGS_MEM_READ) ? "read, " : "",
		(flags & SECT_FLAGS_MEM_WRITE) ? "write" : "");
}

static void
dump_sections (cli_image_info_t *iinfo)
{
	const int top = iinfo->cli_header.coff.coff_sections;
	int i;
	
	for (i = 0; i < top; i++)
		dump_section_table (&iinfo->cli_section_tables [i]);
}

static void
dump_cli_header (cli_header_t *ch)
{
	printf ("\n");
	printf ("          CLI header size: %d\n", ch->ch_size);
	printf ("         Runtime required: %d.%d\n", ch->ch_runtime_major, ch->ch_runtime_minor);
	printf ("                    Flags: %s, %s, %s\n",
		(ch->ch_flags & CLI_FLAGS_ILONLY ? "ilonly" : "contains native"),
		(ch->ch_flags & CLI_FLAGS_32BITREQUIRED ? "32bits" : "32/64"),
		(ch->ch_flags & CLI_FLAGS_ILONLY ? "trackdebug" : "no-trackdebug"));
	dent   ("         Metadata", ch->ch_metadata);
	hex32  ("Entry Point Token", ch->ch_entry_point);
	dent   ("     Resources at", ch->ch_resources);
	dent   ("   Strong Name at", ch->ch_strong_name);
	dent   ("  Code Manager at", ch->ch_code_manager_table);
	dent   ("  VTableFixups at", ch->ch_vtable_fixups);
	dent   ("     EAT jumps at", ch->ch_export_address_table_jumps);
}	

static void
dsh (char *label, cli_image_info_t *iinfo, stream_header_t *sh)
{
	printf ("%s: 0x%08x - 0x%08x [%d == 0x%08x]\n",
		label,
		sh->sh_offset, sh->sh_offset + sh->sh_size,
		sh->sh_size, sh->sh_size);
}

static void
dump_metadata_ptrs (cli_image_info_t *iinfo)
{
	metadata_t *meta = &iinfo->cli_metadata;
	
	printf ("\nMetadata pointers:\n");
	dsh ("\tTables (#~)", iinfo, &meta->heap_tables);
	dsh ("\t    Strings", iinfo, &meta->heap_strings);
	dsh ("\t       Blob", iinfo, &meta->heap_blob);
	dsh ("\tUser string", iinfo, &meta->heap_us);
	dsh ("\t       GUID", iinfo, &meta->heap_guid);
}

static void
dump_table (metadata_t *meta, int table)
{
	
}

static void
dump_metadata (cli_image_info_t *iinfo)
{
	metadata_t *meta = &iinfo->cli_metadata;
	int table;
	
	dump_metadata_ptrs (iinfo);

	printf ("Rows:\n");
	for (table = 0; table < 64; table++){
		if (meta->tables [table].rows == 0)
			continue;
		printf ("Table %s: %d records (%d bytes, at %p)\n",
			mono_meta_table_name (table),
			meta->tables [table].rows,
			meta->tables [table].row_size,
			meta->tables [table].base
			);
		if (dump_tables)
			dump_table (meta, table);
	}
}

static void
dump_methoddef (cli_image_info_t *iinfo, guint32 token)
{
	char *loc;

	loc = mono_metadata_locate_token (&iinfo->cli_metadata, token);

	printf ("RVA for Entry Point: 0x%08x\n", (*(guint32 *)loc));
}

static void
dump_dotnet_iinfo (cli_image_info_t *iinfo)
{
	dump_dotnet_header (&iinfo->cli_header);
	dump_sections (iinfo);
	dump_cli_header (&iinfo->cli_cli_header);
	dump_metadata (iinfo);

	dump_methoddef (iinfo, iinfo->cli_cli_header.ch_entry_point);
}

static void
usage (void)
{
	printf ("Usage is: pedump [--tables] file.exe\n");
	exit (1);
}

int
main (int argc, char *argv [])
{
	cli_image_info_t *iinfo;
	MonoImage *image;
	char *file = NULL;
	int i;
	
	for (i = 1; i < argc; i++){
		if (argv [i][0] != '-'){
			file = argv [1];
			continue;
		}

		if (strcmp (argv [i], "--help") == 0)
			usage ();
		if (strcmp (argv [i], "--tables") == 0)
			dump_tables = 1;
	}
	
	if (!file)
		usage ();

	image = mono_image_open (file, NULL);
	if (!image){
		fprintf (stderr, "Can not open image %s\n", file);
		exit (1);
	}
	iinfo = image->image_info;

	if (dump_data)
		dump_dotnet_iinfo (iinfo);
	
	mono_image_close (image);
	
	return 0;
}

