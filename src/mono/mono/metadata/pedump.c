/*
 * pedump.c: Dumps the contents of an extended PE/COFF file
 *
 * Author:
 *   Miguel de Icaza (miguel@ximian.com)
 *
 * Copyright 2001-2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
 */
#include <config.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include "image.h"
#include <glib.h>
#include "cil-coff.h"
#include "mono-endian.h"
#include "verify.h"
#include <mono/metadata/class.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/metadata-internals.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/verify-internals.h>
#include <mono/metadata/marshal.h>
#include "mono/utils/mono-digest.h"
#include <mono/utils/mono-mmap.h>
#include <sys/types.h>
#include <sys/stat.h>
#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif

gboolean dump_data = TRUE;
gboolean verify_pe = FALSE;
gboolean verify_metadata = FALSE;
gboolean verify_code = FALSE;
gboolean verify_partial_md = FALSE;

static MonoAssembly *pedump_preload (MonoAssemblyName *aname, gchar **assemblies_path, gpointer user_data);
static void pedump_assembly_load_hook (MonoAssembly *assembly, gpointer user_data);
static MonoAssembly *pedump_assembly_search_hook (MonoAssemblyName *aname, gpointer user_data);

/* unused
static void
hex_dump (const char *buffer, int base, int count)
{
	int i;
	
	for (i = 0; i < count; i++){
		if ((i % 16) == 0)
			printf ("\n0x%08x: ", (unsigned char) base + i);

		printf ("%02x ", (unsigned char) (buffer [i]));
	}
}
*/

static void
hex8 (const char *label, unsigned char x)
{
	printf ("\t%s: 0x%02x\n", label, (unsigned char) x);
}

static void
hex16 (const char *label, guint16 x)
{
	printf ("\t%s: 0x%04x\n", label, x);
}

static void
hex32 (const char *label, guint32 x)
{
	printf ("\t%s: 0x%08x\n", label, x);
}

static void
dump_coff_header (MonoCOFFHeader *coff)
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
dump_pe_header (MonoPEHeader *pe)
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
dump_nt_header (MonoPEHeaderNT *nt)
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
dent (const char *label, MonoPEDirEntry de)
{
	printf ("\t%s: 0x%08x [0x%08x]\n", label, de.rva, de.size);
}

static void
dump_blob (const char *desc, const char* p, guint32 size)
{
	int i;

	printf ("%s", desc);
	if (!p) {
		printf (" none\n");
		return;
	}

	for (i = 0; i < size; ++i) {
		if (!(i % 16))
			printf ("\n\t");
		printf (" %02X", p [i] & 0xFF);
	}
	printf ("\n");
}

static void
dump_public_key (MonoImage *m)
{
	guint32 size;
	const char *p;

	p = mono_image_get_public_key (m, &size);
	dump_blob ("\nPublic key:", p, size);
}

static void
dump_strong_name (MonoImage *m)
{
	guint32 size;
	const char *p;

	p = mono_image_get_strong_name (m, &size);
	dump_blob ("\nStrong name:", p, size);
}

static void
dump_datadir (MonoPEDatadir *dd)
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
dump_dotnet_header (MonoDotNetHeader *header)
{
	dump_coff_header (&header->coff);
	dump_pe_header (&header->pe);
	dump_nt_header (&header->nt);
	dump_datadir (&header->datadir);
}

static void
dump_section_table (MonoSectionTable *st)
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
dump_sections (MonoCLIImageInfo *iinfo)
{
	const int top = iinfo->cli_header.coff.coff_sections;
	int i;
	
	for (i = 0; i < top; i++)
		dump_section_table (&iinfo->cli_section_tables [i]);
}

static void
dump_cli_header (MonoCLIHeader *ch)
{
	printf ("\n");
	printf ("          CLI header size: %d\n", ch->ch_size);
	printf ("         Runtime required: %d.%d\n", ch->ch_runtime_major, ch->ch_runtime_minor);
	printf ("                    Flags: %s, %s, %s, %s\n",
		(ch->ch_flags & CLI_FLAGS_ILONLY ? "ilonly" : "contains native"),
		(ch->ch_flags & CLI_FLAGS_32BITREQUIRED ? "32bits" : "32/64"),
		(ch->ch_flags & CLI_FLAGS_TRACKDEBUGDATA ? "trackdebug" : "no-trackdebug"),
		(ch->ch_flags & CLI_FLAGS_STRONGNAMESIGNED ? "strongnamesigned" : "notsigned"));
	dent   ("         Metadata", ch->ch_metadata);
	hex32  ("Entry Point Token", ch->ch_entry_point);
	dent   ("     Resources at", ch->ch_resources);
	dent   ("   Strong Name at", ch->ch_strong_name);
	dent   ("  Code Manager at", ch->ch_code_manager_table);
	dent   ("  VTableFixups at", ch->ch_vtable_fixups);
	dent   ("     EAT jumps at", ch->ch_export_address_table_jumps);
}	

static void
dsh (const char *label, MonoImage *meta, MonoStreamHeader *sh)
{
	printf ("%s: 0x%08x - 0x%08x [%d == 0x%08x]\n",
		label,
		(int)(sh->data - meta->raw_metadata), (int)(sh->data + sh->size - meta->raw_metadata),
		sh->size, sh->size);
}

static void
dump_metadata_header (MonoImage *meta)
{
	printf ("\nMetadata header:\n");
	printf ("           Version: %d.%d\n", meta->md_version_major, meta->md_version_minor);
	printf ("    Version string: %s\n", meta->version);
}

static void
dump_metadata_ptrs (MonoImage *meta)
{
	printf ("\nMetadata pointers:\n");
	dsh ("\tTables (#~)", meta, &meta->heap_tables);
	dsh ("\t    Strings", meta, &meta->heap_strings);
	dsh ("\t       Blob", meta, &meta->heap_blob);
	dsh ("\tUser string", meta, &meta->heap_us);
	dsh ("\t       GUID", meta, &meta->heap_guid);
}

static void
dump_metadata (MonoImage *meta)
{
	int table;

	dump_metadata_header (meta);

	dump_metadata_ptrs (meta);

	printf ("Rows:\n");
	for (table = 0; table < MONO_TABLE_NUM; table++){
		if (meta->tables [table].rows == 0)
			continue;
		printf ("Table %s: %d records (%d bytes, at %x)\n",
			mono_meta_table_name (table),
			meta->tables [table].rows,
			meta->tables [table].row_size,
			(unsigned int)(meta->tables [table].base - meta->raw_data)
			);
	}
}

static void
dump_methoddef (MonoImage *metadata, guint32 token)
{
	const char *loc;

	if (!token)
		return;
	loc = mono_metadata_locate_token (metadata, token);

	printf ("RVA for Entry Point: 0x%08x\n", read32 (loc));
}

static void
dump_dotnet_iinfo (MonoImage *image)
{
	MonoCLIImageInfo *iinfo = image->image_info;

	dump_dotnet_header (&iinfo->cli_header);
	dump_sections (iinfo);
	dump_cli_header (&iinfo->cli_cli_header);
	dump_strong_name (image);
	dump_public_key (image);
	dump_metadata (image);

	dump_methoddef (image, iinfo->cli_cli_header.ch_entry_point);
}

static int
dump_verify_info (MonoImage *image, int flags)
{
	GSList *errors, *tmp;
	int count = 0, verifiable = 0;
	const char* desc [] = {
		"Ok", "Error", "Warning", NULL, "CLS", NULL, NULL, NULL, "Not Verifiable"
	};

	if (verify_code) { /* verify code */
		int i;
		MonoTableInfo *m = &image->tables [MONO_TABLE_METHOD];

		for (i = 0; i < m->rows; ++i) {
			MonoMethod *method;
			mono_loader_clear_error ();

			method = mono_get_method (image, MONO_TOKEN_METHOD_DEF | (i+1), NULL);
			if (!method) {
				g_print ("Warning: Cannot lookup method with token 0x%08x\n", i + 1);
				continue;
			}
			errors = mono_method_verify (method, flags);
			if (errors) {
				MonoClass *klass = mono_method_get_class (method);
				char *name = mono_type_full_name (&klass->byval_arg);
				if (mono_method_signature (method) == NULL) {
					g_print ("In method: %s::%s(ERROR)\n", name, mono_method_get_name (method));
				} else {
					char *sig;
					sig = mono_signature_get_desc (mono_method_signature (method), FALSE);	
					g_print ("In method: %s::%s(%s)\n", name, mono_method_get_name (method), sig);
					g_free (sig);
				}
				g_free (name);
			}

			for (tmp = errors; tmp; tmp = tmp->next) {
				MonoVerifyInfo *info = tmp->data;
				g_print ("%s: %s\n", desc [info->status], info->message);
				if (info->status == MONO_VERIFY_ERROR) {
					count++;
					verifiable = 3;
				}
				if(info->status == MONO_VERIFY_NOT_VERIFIABLE) {
					if (verifiable < 2)
						verifiable = 2;	
				}
			}
			mono_free_verify_list (errors);
		}
	}

	if (count)
		g_print ("Error count: %d\n", count);
	return verifiable;
}

static void
usage (void)
{
	printf ("Usage is: pedump [--verify error,warn,cls,all,code,fail-on-verifiable,non-strict,valid-only,metadata] file.exe\n");
	exit (1);
}

static int
verify_image_file (const char *fname)
{
	GSList *errors = NULL, *tmp;
	MonoImage *image;
	MonoTableInfo *table;
	MonoAssembly *assembly;
	MonoImageOpenStatus status;
	int i, count = 0;
	const char* desc [] = {
		"Ok", "Error", "Warning", NULL, "CLS", NULL, NULL, NULL, "Not Verifiable"
	};

	image = mono_image_open_raw (fname, &status);
	if (!image) {
		printf ("Could not open %s\n", fname);
		return 1;
	}

	if (!mono_verifier_verify_pe_data (image, &errors))
		goto invalid_image;

	if (!mono_image_load_pe_data (image)) {
		printf ("Could not load pe data for assembly %s\n", fname);
		return 1;
	}

	if (!mono_verifier_verify_cli_data (image, &errors))
		goto invalid_image;

	if (!mono_image_load_cli_data (image)) {
		printf ("Could not load cli data for assembly %s\n", fname);
		return 1;
	}

	if (!mono_verifier_verify_table_data (image, &errors))
		goto invalid_image;

	mono_image_load_names (image);

	/*fake an assembly for class loading to work*/
	assembly = g_new0 (MonoAssembly, 1);
	assembly->in_gac = FALSE;
	assembly->image = image;
	image->assembly = assembly;
	mono_assembly_fill_assembly_name (image, &assembly->aname);

	/*Finish initializing the runtime*/
	mono_install_assembly_load_hook (pedump_assembly_load_hook, NULL);
	mono_install_assembly_search_hook (pedump_assembly_search_hook, NULL);

	mono_init_version ("pedump", image->version);

	mono_install_assembly_preload_hook (pedump_preload, GUINT_TO_POINTER (FALSE));

	mono_marshal_init ();


	if (!verify_partial_md && !mono_verifier_verify_full_table_data (image, &errors))
		goto invalid_image;


	table = &image->tables [MONO_TABLE_TYPEDEF];
	for (i = 1; i <= table->rows; ++i) {
		guint32 token = i | MONO_TOKEN_TYPE_DEF;
		MonoClass *class = mono_class_get (image, token);
		if (!class) {
			printf ("Could not load class with token %x\n", token);
			continue;
		}
		mono_class_init (class);
		if (class->exception_type != MONO_EXCEPTION_NONE || mono_loader_get_last_error ()) {
			printf ("Error verifying class(0x%08x) %s.%s a type load error happened\n", token, class->name_space, class->name);
			mono_loader_clear_error ();
			++count;
		}

		mono_class_setup_vtable (class);
		if (class->exception_type != MONO_EXCEPTION_NONE || mono_loader_get_last_error ()) {
			printf ("Error verifying class(0x%08x) %s.%s a type load error happened\n", token, class->name_space, class->name);
			mono_loader_clear_error ();
			++count;
		}
	}
	if (count)
		return 5;
	return 0;

invalid_image:
	for (tmp = errors; tmp; tmp = tmp->next) {
		MonoVerifyInfo *info = tmp->data;
		g_print ("%s: %s\n", desc [info->status], info->message);
		if (info->status == MONO_VERIFY_ERROR)
			count++;
	}
	mono_free_verify_list (errors);
	if (count)
		g_print ("Error count: %d\n", count);
	return 1;
}

static gboolean
try_load_from (MonoAssembly **assembly, const gchar *path1, const gchar *path2,
					const gchar *path3, const gchar *path4, gboolean refonly)
{
	gchar *fullpath;

	*assembly = NULL;
	fullpath = g_build_filename (path1, path2, path3, path4, NULL);
	if (g_file_test (fullpath, G_FILE_TEST_IS_REGULAR))
		*assembly = mono_assembly_open_full (fullpath, NULL, refonly);

	g_free (fullpath);
	return (*assembly != NULL);
}

static MonoAssembly *
real_load (gchar **search_path, const gchar *culture, const gchar *name, gboolean refonly)
{
	MonoAssembly *result = NULL;
	gchar **path;
	gchar *filename;
	const gchar *local_culture;
	gint len;

	if (!culture || *culture == '\0') {
		local_culture = "";
	} else {
		local_culture = culture;
	}

	filename =  g_strconcat (name, ".dll", NULL);
	len = strlen (filename);

	for (path = search_path; *path; path++) {
		if (**path == '\0')
			continue; /* Ignore empty ApplicationBase */

		/* See test cases in bug #58992 and bug #57710 */
		/* 1st try: [culture]/[name].dll (culture may be empty) */
		strcpy (filename + len - 4, ".dll");
		if (try_load_from (&result, *path, local_culture, "", filename, refonly))
			break;

		/* 2nd try: [culture]/[name].exe (culture may be empty) */
		strcpy (filename + len - 4, ".exe");
		if (try_load_from (&result, *path, local_culture, "", filename, refonly))
			break;

		/* 3rd try: [culture]/[name]/[name].dll (culture may be empty) */
		strcpy (filename + len - 4, ".dll");
		if (try_load_from (&result, *path, local_culture, name, filename, refonly))
			break;

		/* 4th try: [culture]/[name]/[name].exe (culture may be empty) */
		strcpy (filename + len - 4, ".exe");
		if (try_load_from (&result, *path, local_culture, name, filename, refonly))
			break;
	}

	g_free (filename);
	return result;
}

/*
 * Try to load referenced assemblies from assemblies_path.
 */
static MonoAssembly *
pedump_preload (MonoAssemblyName *aname,
				 gchar **assemblies_path,
				 gpointer user_data)
{
	MonoAssembly *result = NULL;
	gboolean refonly = GPOINTER_TO_UINT (user_data);

	if (assemblies_path && assemblies_path [0] != NULL) {
		result = real_load (assemblies_path, aname->culture, aname->name, refonly);
	}

	return result;
}

static GList *loaded_assemblies = NULL;

static void
pedump_assembly_load_hook (MonoAssembly *assembly, gpointer user_data)
{
	loaded_assemblies = g_list_prepend (loaded_assemblies, assembly);
}

static MonoAssembly *
pedump_assembly_search_hook (MonoAssemblyName *aname, gpointer user_data)
{
        GList *tmp;

       for (tmp = loaded_assemblies; tmp; tmp = tmp->next) {
               MonoAssembly *ass = tmp->data;
               if (mono_assembly_names_equal (aname, &ass->aname))
		       return ass;
       }
       return NULL;
}

#define VALID_ONLY_FLAG 0x08000000
#define VERIFY_CODE_ONLY MONO_VERIFY_ALL + 1 
#define VERIFY_METADATA_ONLY VERIFY_CODE_ONLY + 1
#define VERIFY_PARTIAL_METADATA VERIFY_CODE_ONLY + 2

int
main (int argc, char *argv [])
{
	int image_result = 0;
	MonoImage *image;
	char *file = NULL;
	char *flags = NULL;
	MiniVerifierMode verifier_mode = MONO_VERIFIER_MODE_VERIFIABLE;
	const char *flag_desc [] = {"error", "warn", "cls", "all", "code", "fail-on-verifiable", "non-strict", "valid-only", "metadata", "partial-md", NULL};
	guint flag_vals [] = {MONO_VERIFY_ERROR, MONO_VERIFY_WARNING, MONO_VERIFY_CLS, MONO_VERIFY_ALL, VERIFY_CODE_ONLY, MONO_VERIFY_FAIL_FAST, MONO_VERIFY_NON_STRICT, VALID_ONLY_FLAG, VERIFY_METADATA_ONLY, VERIFY_PARTIAL_METADATA, 0};
	int i, verify_flags = MONO_VERIFY_REPORT_ALL_ERRORS, run_new_metadata_verifier = 0;
	
	for (i = 1; i < argc; i++){
		if (argv [i][0] != '-'){
			file = argv [i];
			continue;
		}

		if (strcmp (argv [i], "--help") == 0)
			usage ();
		else if (strcmp (argv [i], "--verify") == 0) {
			verify_pe = 1;
			dump_data = 0;
			++i;
			flags = argv [i];
		} else {
			usage ();
		}
	}
	
	if (!file)
		usage ();

#ifndef DISABLE_PERFCOUNTERS
	mono_perfcounters_init ();
#endif
	mono_metadata_init ();
	mono_images_init ();
	mono_assemblies_init ();
	mono_loader_init ();
 
	if (verify_pe) {
		char *tok = strtok (flags, ",");

		verify_metadata = 1;
		verify_code = 0;
		while (tok) {
			for (i = 0; flag_desc [i]; ++i) {
				if (strcmp (tok, flag_desc [i]) == 0) {
					if (flag_vals [i] == VERIFY_CODE_ONLY) {
						verify_metadata = 0;
						verify_code = 1;
					} else if(flag_vals [i] == MONO_VERIFY_ALL) {
						verify_code = 1;
					} else if(flag_vals [i] == VERIFY_METADATA_ONLY) {
						verify_metadata = 0;
						run_new_metadata_verifier = 1;
					} else if(flag_vals [i] == VERIFY_PARTIAL_METADATA) {
						verify_partial_md = 1;
					}
					if (flag_vals [i] == VALID_ONLY_FLAG)
						verifier_mode = MONO_VERIFIER_MODE_VALID;
					else
						verify_flags |= flag_vals [i];
					break;
				}
			}
			if (!flag_desc [i])
				g_print ("Unknown verify flag %s\n", tok);
			tok = strtok (NULL, ",");
		}

		mono_verifier_set_mode (verifier_mode);
		/**/
	}

	if (verify_pe || run_new_metadata_verifier) {
		run_new_metadata_verifier = 1;
	}
	
	if (run_new_metadata_verifier) {
		mono_verifier_set_mode (verifier_mode);

		image_result = verify_image_file (file);
		if (image_result == 1 || !verify_code)
			return image_result;
	}

	image = mono_image_open (file, NULL);
	if (!image){
		fprintf (stderr, "Cannot open image %s\n", file);
		exit (1);
	}

	if (dump_data)
		dump_dotnet_iinfo (image);
	if (verify_pe) {
		MonoAssembly *assembly;
		MonoImage *image;
		MonoImageOpenStatus status;
		int code_result;

		mono_verifier_set_mode (verifier_mode);

		assembly = mono_assembly_open (file, NULL);
		/*fake an assembly for netmodules so the verifier works*/
		if (!assembly && (image = mono_image_open (file, &status)) && image->tables [MONO_TABLE_ASSEMBLY].rows == 0) {
			assembly = g_new0 (MonoAssembly, 1);
			assembly->in_gac = FALSE;
			assembly->image = image;
			image->assembly = assembly;
		}

		if (!assembly) {
			g_print ("Could not open assembly %s\n", file);
			return 4;
		}

		code_result = dump_verify_info (assembly->image, verify_flags);
		return code_result ? code_result : image_result;
	} else
		mono_image_close (image);
	
	return 0;
}

