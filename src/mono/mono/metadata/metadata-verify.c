/*
 * metadata-verify.c: Metadata verfication support
 *
 * Author:
 *	Mono Project (http://www.mono-project.com)
 *
 * Copyright (C) 2005-2008 Novell, Inc. (http://www.novell.com)
 */

#include <mono/metadata/object-internals.h>
#include <mono/metadata/verify.h>
#include <mono/metadata/verify-internals.h>
#include <mono/metadata/opcodes.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/reflection.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/mono-endian.h>
#include <mono/metadata/metadata.h>
#include <mono/metadata/metadata-internals.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/security-manager.h>
#include <mono/metadata/security-core-clr.h>
#include <mono/metadata/cil-coff.h>
#include <mono/utils/strenc.h>
#include <string.h>
#include <signal.h>
#include <ctype.h>

/*
 TODO add fail fast mode
 TODO add PE32+ support
 TODO verify the entry point RVA and content.
 TODO load_section_table and load_data_directories must take PE32+ into account
 TODO add section relocation support
 TODO verify the relocation table, since we really don't use, no need so far.
 TODO do full PECOFF resources verification 
 TODO verify in the CLI header entry point and resources
 TODO implement null token typeref validation  
 TODO verify table wide invariants for typedef (sorting and uniqueness)
 TODO implement proper authenticode data directory validation
 TODO verify properties that require multiple tables to be valid 
 FIXME use subtraction based bounds checking to avoid overflows
 FIXME get rid of metadata_streams and other fields from VerifyContext
*/

#ifdef MONO_VERIFIER_DEBUG
#define VERIFIER_DEBUG(code) do { code; } while (0)
#else
#define VERIFIER_DEBUG(code)
#endif

#define INVALID_OFFSET ((guint32)-1)
#define INVALID_ADDRESS 0xffffffff

enum {
	STAGE_PE,
	STAGE_CLI,
	STAGE_TABLES
};

enum {
	IMPORT_TABLE_IDX = 1, 
	RESOURCE_TABLE_IDX = 2,
	CERTIFICATE_TABLE_IDX = 4,
	RELOCATION_TABLE_IDX = 5,
	IAT_IDX = 12,
	CLI_HEADER_IDX = 14,
};

enum {
	STRINGS_STREAM,
	USER_STRINGS_STREAM,
	BLOB_STREAM,
	GUID_STREAM,
	TILDE_STREAM
};


#define INVALID_TABLE (0xFF)
/*format: number of bits, number of tables, tables{n. tables} */
const static unsigned char coded_index_desc[] = {
#define TYPEDEF_OR_REF_DESC (0)
	2, /*bits*/
	3, /*tables*/
	MONO_TABLE_TYPEDEF,
	MONO_TABLE_TYPEREF,
	MONO_TABLE_TYPESPEC,

#define HAS_CONSTANT_DESC (TYPEDEF_OR_REF_DESC + 5)
	2, /*bits*/
	3, /*tables*/
	MONO_TABLE_FIELD,
	MONO_TABLE_PARAM,
	MONO_TABLE_PROPERTY,

#define HAS_CATTR_DESC (HAS_CONSTANT_DESC + 5)
	5, /*bits*/
	19, /*tables*/
	MONO_TABLE_METHOD,
	MONO_TABLE_FIELD,
	MONO_TABLE_TYPEREF,
	MONO_TABLE_TYPEDEF,
	MONO_TABLE_PARAM,
	MONO_TABLE_INTERFACEIMPL,
	MONO_TABLE_MEMBERREF,
	MONO_TABLE_MODULE,
	MONO_TABLE_DECLSECURITY,
	MONO_TABLE_PROPERTY, 
	MONO_TABLE_EVENT,
	MONO_TABLE_STANDALONESIG,
	MONO_TABLE_MODULEREF,
	MONO_TABLE_TYPESPEC,
	MONO_TABLE_ASSEMBLY,
	MONO_TABLE_ASSEMBLYREF,
	MONO_TABLE_FILE,
	MONO_TABLE_EXPORTEDTYPE,
	MONO_TABLE_MANIFESTRESOURCE,

#define HAS_FIELD_MARSHAL_DESC (HAS_CATTR_DESC + 21)
	1, /*bits*/
	2, /*tables*/
	MONO_TABLE_FIELD,
	MONO_TABLE_PARAM,

#define HAS_DECL_SECURITY_DESC (HAS_FIELD_MARSHAL_DESC + 4)
	2, /*bits*/
	3, /*tables*/
	MONO_TABLE_TYPEDEF,
	MONO_TABLE_METHOD,
	MONO_TABLE_ASSEMBLY,

#define MEMBERREF_PARENT_DESC (HAS_DECL_SECURITY_DESC + 5)
	3, /*bits*/
	5, /*tables*/
	MONO_TABLE_TYPEDEF,
	MONO_TABLE_TYPEREF,
	MONO_TABLE_MODULE,
	MONO_TABLE_METHOD,
	MONO_TABLE_TYPESPEC,

#define HAS_SEMANTICS_DESC (MEMBERREF_PARENT_DESC + 7)
	1, /*bits*/
	2, /*tables*/
	MONO_TABLE_EVENT,
	MONO_TABLE_PROPERTY,

#define METHODDEF_OR_REF_DESC (HAS_SEMANTICS_DESC + 4)
	1, /*bits*/
	2, /*tables*/
	MONO_TABLE_METHOD,
	MONO_TABLE_MEMBERREF,

#define MEMBER_FORWARDED_DESC (METHODDEF_OR_REF_DESC + 4)
	1, /*bits*/
	2, /*tables*/
	MONO_TABLE_FIELD,
	MONO_TABLE_METHOD,

#define IMPLEMENTATION_DESC (MEMBER_FORWARDED_DESC + 4)
	2, /*bits*/
	3, /*tables*/
	MONO_TABLE_FILE,
	MONO_TABLE_ASSEMBLYREF,
	MONO_TABLE_EXPORTEDTYPE,

#define CATTR_TYPE_DESC (IMPLEMENTATION_DESC + 5)
	3, /*bits*/
	5, /*tables*/
	INVALID_TABLE,
	INVALID_TABLE,
	MONO_TABLE_METHOD,
	MONO_TABLE_MEMBERREF,
	INVALID_TABLE,

#define RES_SCOPE_DESC (CATTR_TYPE_DESC + 7)
	2, /*bits*/
	4, /*tables*/
	MONO_TABLE_MODULE,
	MONO_TABLE_MODULEREF,
	MONO_TABLE_ASSEMBLYREF,
	MONO_TABLE_TYPEREF,

#define TYPE_OR_METHODDEF_DESC (RES_SCOPE_DESC + 6)
	1, /*bits*/
	2, /*tables*/
	MONO_TABLE_TYPEDEF,
	MONO_TABLE_METHOD
};

typedef struct {
	guint32 rva;
	guint32 size;
	guint32 translated_offset;
} DataDirectory;

typedef struct {
	guint32 offset;
	guint32 size;
} OffsetAndSize;

typedef struct {
	guint32 baseRVA;
	guint32 baseOffset;
	guint32 size;
	guint32 rellocationsRVA;
	guint16 numberOfRelocations;
} SectionHeader;

typedef struct {
	guint32 row_count;
	guint32 row_size;
	guint32 offset;
} TableInfo;

typedef struct {
	const char *data;
	guint32 size;
	GSList *errors;
	int valid;
	gboolean is_corlib;
	MonoImage *image;
	gboolean report_error;
	int stage;

	DataDirectory data_directories [16];
	guint32 section_count;
	SectionHeader *sections;

	OffsetAndSize metadata_streams [5]; //offset from begin of the image
} VerifyContext;

#define ADD_VERIFY_INFO(__ctx, __msg, __status, __exception)	\
	do {	\
		MonoVerifyInfoExtended *vinfo = g_new (MonoVerifyInfoExtended, 1);	\
		vinfo->info.status = __status;	\
		vinfo->info.message = ( __msg);	\
		vinfo->exception_type = (__exception);	\
		(__ctx)->errors = g_slist_prepend ((__ctx)->errors, vinfo);	\
	} while (0)


#define ADD_ERROR(__ctx, __msg)	\
	do {	\
		if ((__ctx)->report_error) \
			ADD_VERIFY_INFO(__ctx, __msg, MONO_VERIFY_ERROR, MONO_EXCEPTION_INVALID_PROGRAM); \
		(__ctx)->valid = 0; \
		return; \
	} while (0)

#define CHECK_STATE() do { if (!ctx.valid) goto cleanup; } while (0)

#define CHECK_ERROR() do { if (!ctx->valid) return; } while (0)

static guint32
pe_signature_offset (VerifyContext *ctx)
{
	return read32 (ctx->data + 0x3c);
}

static guint32
pe_header_offset (VerifyContext *ctx)
{
	return read32 (ctx->data + 0x3c) + 4;
}

static gboolean
bounds_check_virtual_address (VerifyContext *ctx, guint32 rva, guint32 size)
{
	int i;

	if (rva + size < rva) //overflow
		return FALSE;

	if (ctx->stage > STAGE_PE) {
		MonoCLIImageInfo *iinfo = ctx->image->image_info;
		const int top = iinfo->cli_section_count;
		MonoSectionTable *tables = iinfo->cli_section_tables;
		int i;
		
		for (i = 0; i < top; i++) {
			guint32 base = tables->st_virtual_address;
			guint32 end = base + tables->st_raw_data_size;

			if (rva >= base && rva + size <= end)
				return TRUE;

			/*if ((addr >= tables->st_virtual_address) &&
			    (addr < tables->st_virtual_address + tables->st_raw_data_size)){

				return addr - tables->st_virtual_address + tables->st_raw_data_ptr;
			}*/
			tables++;
		}
		return FALSE;
	}

	if (!ctx->sections)
		return FALSE;

	for (i = 0; i < ctx->section_count; ++i) {
		guint32 base = ctx->sections [i].baseRVA;
		guint32 end = ctx->sections [i].baseRVA + ctx->sections [i].size;
		if (rva >= base && rva + size <= end)
			return TRUE;
	}
	return FALSE;
}

static gboolean
bounds_check_datadir (DataDirectory *dir, guint32 offset, guint32 size)
{
	if (dir->translated_offset > offset)
		return FALSE;
	if (dir->size < size)
		return FALSE;
	return offset + size <= dir->translated_offset + dir->size;
}

static gboolean
bounds_check_offset (OffsetAndSize *off, guint32 offset, guint32 size)
{
	if (off->offset > offset)
		return FALSE;
	
	if (off->size < size)
		return FALSE;

	return offset + size <= off->offset + off->size;
}

static guint32
translate_rva (VerifyContext *ctx, guint32 rva)
{
	int i;

	if (ctx->stage > STAGE_PE)
		return mono_cli_rva_image_map (ctx->image, rva);
		
	if (!ctx->sections)
		return FALSE;

	for (i = 0; i < ctx->section_count; ++i) {
		guint32 base = ctx->sections [i].baseRVA;
		guint32 end = ctx->sections [i].baseRVA + ctx->sections [i].size;
		if (rva >= base && rva <= end) {
			guint32 res = (rva - base) + ctx->sections [i].baseOffset;
			/* double check */
			return res >= ctx->size ? INVALID_OFFSET : res;
		}
	}

	return INVALID_OFFSET;
}

static void
verify_msdos_header (VerifyContext *ctx)
{
	guint32 lfanew;
	if (ctx->size < 128)
		ADD_ERROR (ctx, g_strdup ("Not enough space for the MS-DOS header"));
	if (ctx->data [0] != 0x4d || ctx->data [1] != 0x5a)
		ADD_ERROR (ctx,  g_strdup ("Invalid MS-DOS watermark"));
	lfanew = pe_signature_offset (ctx);
	if (lfanew > ctx->size - 4)
		ADD_ERROR (ctx, g_strdup ("MS-DOS lfanew offset points to outside of the file"));
}

static void
verify_pe_header (VerifyContext *ctx)
{
	guint32 offset = pe_signature_offset (ctx);
	const char *pe_header = ctx->data + offset;
	if (pe_header [0] != 'P' || pe_header [1] != 'E' ||pe_header [2] != 0 ||pe_header [3] != 0)
		ADD_ERROR (ctx,  g_strdup ("Invalid PE header watermark"));
	pe_header += 4;
	offset += 4;

	if (offset > ctx->size - 20)
		ADD_ERROR (ctx, g_strdup ("File with truncated pe header"));
	if (read16 (pe_header) != 0x14c)
		ADD_ERROR (ctx, g_strdup ("Invalid PE header Machine value"));
}

static void
verify_pe_optional_header (VerifyContext *ctx)
{
	guint32 offset = pe_header_offset (ctx);
	guint32 header_size, file_alignment;
	const char *pe_header = ctx->data + offset;
	const char *pe_optional_header = pe_header + 20;

	header_size = read16 (pe_header + 16);
	offset += 20;

	if (header_size < 2) /*must be at least 2 or we won't be able to read magic*/
		ADD_ERROR (ctx, g_strdup ("Invalid PE optional header size"));

	if (offset > ctx->size - header_size || header_size > ctx->size)
		ADD_ERROR (ctx, g_strdup ("Invalid PE optional header size"));

	if (read16 (pe_optional_header) == 0x10b) {
		if (header_size != 224)
			ADD_ERROR (ctx, g_strdup_printf ("Invalid optional header size %d", header_size));

		/* LAMESPEC MS plays around this value and ignore it during validation
		if (read32 (pe_optional_header + 28) != 0x400000)
			ADD_ERROR (ctx, g_strdup_printf ("Invalid Image base %x", read32 (pe_optional_header + 28)));*/
		if (read32 (pe_optional_header + 32) != 0x2000)
			ADD_ERROR (ctx, g_strdup_printf ("Invalid Section Aligmnent %x", read32 (pe_optional_header + 32)));
		file_alignment = read32 (pe_optional_header + 36);
		if (file_alignment != 0x200 && file_alignment != 0x1000)
			ADD_ERROR (ctx, g_strdup_printf ("Invalid file Aligmnent %x", file_alignment));
		/* All the junk in the middle is irrelevant, specially for mono. */
		if (read32 (pe_optional_header + 92) > 0x10)
			ADD_ERROR (ctx, g_strdup_printf ("Too many data directories %x", read32 (pe_optional_header + 92)));
	} else {
		if (read16 (pe_optional_header) == 0x20B)
			ADD_ERROR (ctx, g_strdup ("Metadata verifier doesn't handle PE32+"));
		else
			ADD_ERROR (ctx, g_strdup_printf ("Invalid optional header magic %d", read16 (pe_optional_header)));
	}
}

static void
load_section_table (VerifyContext *ctx)
{
	int i;
	SectionHeader *sections;
	guint32 offset =  pe_header_offset (ctx);
	const char *ptr = ctx->data + offset;
	guint16 num_sections = ctx->section_count = read16 (ptr + 2);

	offset += 244;/*FIXME, this constant is different under PE32+*/
	ptr += 244;

	if (num_sections * 40 > ctx->size - offset)
		ADD_ERROR (ctx, g_strdup ("Invalid PE optional header size"));

	sections = ctx->sections = g_new0 (SectionHeader, num_sections);
	for (i = 0; i < num_sections; ++i) {
		sections [i].size = read32 (ptr + 8);
		sections [i].baseRVA = read32 (ptr + 12);
		sections [i].baseOffset = read32 (ptr + 20);
		sections [i].rellocationsRVA = read32 (ptr + 24);
		sections [i].numberOfRelocations = read16 (ptr + 32);
		ptr += 40;
	}

	ptr = ctx->data + offset; /*reset it to the beggining*/
	for (i = 0; i < num_sections; ++i) {
		guint32 raw_size, flags;
		if (sections [i].baseOffset == 0)
			ADD_ERROR (ctx, g_strdup ("Metadata verifier doesn't handle sections with intialized data only"));
		if (sections [i].baseOffset >= ctx->size)
			ADD_ERROR (ctx, g_strdup_printf ("Invalid PointerToRawData %x points beyond EOF", sections [i].baseOffset));
		if (sections [i].size > ctx->size - sections [i].baseOffset)
			ADD_ERROR (ctx, g_strdup ("Invalid VirtualSize points beyond EOF"));

		raw_size = read32 (ptr + 16);
		if (raw_size < sections [i].size)
			ADD_ERROR (ctx, g_strdup ("Metadata verifier doesn't handle sections with SizeOfRawData < VirtualSize"));

		if (raw_size > ctx->size - sections [i].baseOffset)
			ADD_ERROR (ctx, g_strdup_printf ("Invalid SizeOfRawData %x points beyond EOF", raw_size));

		if (sections [i].rellocationsRVA || sections [i].numberOfRelocations)
			ADD_ERROR (ctx, g_strdup_printf ("Metadata verifier doesn't handle section relocation"));

		flags = read32 (ptr + 36);
		/*TODO 0xFE0000E0 is all flags from cil-coff.h OR'd. Make it a less magical number*/
		if (flags == 0 || (flags & ~0xFE0000E0) != 0)
			ADD_ERROR (ctx, g_strdup_printf ("Invalid section flags %x", flags));

		ptr += 40;
	}
}

static gboolean
is_valid_data_directory (int i)
{
	/*LAMESPEC 4 == certificate 6 == debug, MS uses both*/
	return i == 1 || i == 2 || i == 5 || i == 12 || i == 14 || i == 4 || i == 6; 
}

static void
load_data_directories (VerifyContext *ctx)
{
	guint32 offset =  pe_header_offset (ctx) + 116; /*FIXME, this constant is different under PE32+*/
	const char *ptr = ctx->data + offset;
	int i;

	for (i = 0; i < 16; ++i) {
		guint32 rva = read32 (ptr);
		guint32 size = read32 (ptr + 4);

		/*LAMESPEC the authenticode data directory format is different. We don't support CAS, so lets ignore for now.*/
		if (i == CERTIFICATE_TABLE_IDX) {
			ptr += 8;
			continue;
		}
		if ((rva != 0 || size != 0) && !is_valid_data_directory (i))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid data directory %d", i));

		if (rva != 0 && !bounds_check_virtual_address (ctx, rva, size))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid data directory %d rva/size pair %x/%x", i, rva, size));

		ctx->data_directories [i].rva = rva;
		ctx->data_directories [i].size = size;
		ctx->data_directories [i].translated_offset = translate_rva (ctx, rva);

		ptr += 8;
	}
}

#define SIZE_OF_MSCOREE (sizeof ("mscoree.dll"))

#define SIZE_OF_CORMAIN (sizeof ("_CorExeMain"))

static void
verify_hint_name_table (VerifyContext *ctx, guint32 import_rva, const char *table_name)
{
	const char *ptr;
	guint32 hint_table_rva;

	import_rva = translate_rva (ctx, import_rva);
	g_assert (import_rva != INVALID_OFFSET);

	hint_table_rva = read32 (ctx->data + import_rva);
	if (!bounds_check_virtual_address (ctx, hint_table_rva, SIZE_OF_CORMAIN + 2))
		ADD_ERROR (ctx, g_strdup_printf ("Invalid Hint/Name rva %d for %s", hint_table_rva, table_name));

	hint_table_rva = translate_rva (ctx, hint_table_rva);
	g_assert (hint_table_rva != INVALID_OFFSET);
	ptr = ctx->data + hint_table_rva + 2;

	if (memcmp ("_CorExeMain", ptr, SIZE_OF_CORMAIN) && memcmp ("_CorDllMain", ptr, SIZE_OF_CORMAIN)) {
		char name[SIZE_OF_CORMAIN];
		memcpy (name, ptr, SIZE_OF_CORMAIN);
		name [SIZE_OF_CORMAIN - 1] = 0;
		ADD_ERROR (ctx, g_strdup_printf ("Invalid Hint / Name: '%s'", name));
	}
}

static void
verify_import_table (VerifyContext *ctx)
{
	DataDirectory it = ctx->data_directories [IMPORT_TABLE_IDX];
	guint32 offset = it.translated_offset;
	const char *ptr = ctx->data + offset;
	guint32 name_rva, ilt_rva, iat_rva;

	g_assert (offset != INVALID_OFFSET);

	if (it.size < 40)
		ADD_ERROR (ctx, g_strdup_printf ("Import table size %d is smaller than 40", it.size));

	ilt_rva = read32 (ptr);
	if (!bounds_check_virtual_address (ctx, ilt_rva, 8))
		ADD_ERROR (ctx, g_strdup_printf ("Invalid Import Lookup Table rva %x", ilt_rva));

	name_rva = read32 (ptr + 12);
	if (!bounds_check_virtual_address (ctx, name_rva, SIZE_OF_MSCOREE))
		ADD_ERROR (ctx, g_strdup_printf ("Invalid Import Table Name rva %x", name_rva));

	iat_rva = read32 (ptr + 16);
	if (!bounds_check_virtual_address (ctx, iat_rva, 8))
		ADD_ERROR (ctx, g_strdup_printf ("Invalid Import Address Table rva %x", iat_rva));

	if (iat_rva != ctx->data_directories [IAT_IDX].rva)
		ADD_ERROR (ctx, g_strdup_printf ("Import Address Table rva %x different from data directory entry %x", read32 (ptr + 16), ctx->data_directories [IAT_IDX].rva));

	name_rva = translate_rva (ctx, name_rva);
	g_assert (name_rva != INVALID_OFFSET);
	ptr = ctx->data + name_rva;

	if (memcmp ("mscoree.dll", ptr, SIZE_OF_MSCOREE)) {
		char name[SIZE_OF_MSCOREE];
		memcpy (name, ptr, SIZE_OF_MSCOREE);
		name [SIZE_OF_MSCOREE - 1] = 0;
		ADD_ERROR (ctx, g_strdup_printf ("Invalid Import Table Name: '%s'", name));
	}
	
	verify_hint_name_table (ctx, ilt_rva, "Import Lookup Table");
	CHECK_ERROR ();
	verify_hint_name_table (ctx, iat_rva, "Import Address Table");
}

static void
verify_resources_table (VerifyContext *ctx)
{
	DataDirectory it = ctx->data_directories [RESOURCE_TABLE_IDX];
	guint32 offset;
	guint16 named_entries, id_entries;
	const char *ptr, *root, *end;

	if (it.rva == 0)
		return;

	if (it.size < 16)
		ADD_ERROR (ctx, g_strdup_printf ("Resource section is too small, must be at least 16 bytes long but it's %d long", it.size));

	offset = it.translated_offset;
	root = ptr = ctx->data + offset;
	end = root + it.size;

	g_assert (offset != INVALID_OFFSET);

	named_entries = read16 (ptr + 12);
	id_entries = read16 (ptr + 14);

	if ((named_entries + id_entries) * 8 + 16 > it.size)
		ADD_ERROR (ctx, g_strdup_printf ("Resource section is too small, the number of entries (%d) doesn't fit on it's size %d", named_entries + id_entries, it.size));

	/* XXX at least one unmanaged resource is added due to a call to AssemblyBuilder::DefineVersionInfoResource () 
	if (named_entries || id_entries)
		ADD_ERROR (ctx, g_strdup_printf ("The metadata verifier doesn't support full verification of PECOFF resources"));
	*/
}

/*----------nothing from here on can use data_directory---*/

static DataDirectory
get_data_dir (VerifyContext *ctx, int idx)
{
	MonoCLIImageInfo *iinfo = ctx->image->image_info;
	MonoPEDirEntry *entry= &iinfo->cli_header.datadir.pe_export_table;
	DataDirectory res;

	entry += idx;
	res.rva = entry->rva;
	res.size = entry->size;
	res.translated_offset = translate_rva (ctx, res.rva);
	return res;

}
static void
verify_cli_header (VerifyContext *ctx)
{
	DataDirectory it = get_data_dir (ctx, CLI_HEADER_IDX);
	guint32 offset;
	const char *ptr;
	int i;

	if (it.rva == 0)
		ADD_ERROR (ctx, g_strdup_printf ("CLI header missing"));

	if (it.size != 72)
		ADD_ERROR (ctx, g_strdup_printf ("Invalid cli header size in data directory %d must be 72", it.size));

	offset = it.translated_offset;
	ptr = ctx->data + offset;

	g_assert (offset != INVALID_OFFSET);

	if (read16 (ptr) != 72)
		ADD_ERROR (ctx, g_strdup_printf ("Invalid cli header size %d must be 72", read16 (ptr)));

	if (!bounds_check_virtual_address (ctx, read32 (ptr + 8), read32 (ptr + 12)))
		ADD_ERROR (ctx, g_strdup_printf ("Invalid medatata section rva/size pair %x/%x", read32 (ptr + 8), read32 (ptr + 12)));


	if (!read32 (ptr + 8) || !read32 (ptr + 12))
		ADD_ERROR (ctx, g_strdup_printf ("Missing medatata section in the CLI header"));

	if ((read32 (ptr + 16) & ~0x0001000B) != 0)
		ADD_ERROR (ctx, g_strdup_printf ("Invalid CLI header flags"));

	ptr += 24;
	for (i = 0; i < 6; ++i) {
		guint32 rva = read32 (ptr);
		guint32 size = read32 (ptr + 4);

		if (rva != 0 && !bounds_check_virtual_address (ctx, rva, size))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid cli section %i rva/size pair %x/%x", i, rva, size));

		ptr += 8;

		if (rva && i > 1)
			ADD_ERROR (ctx, g_strdup_printf ("Metadata verifier doesn't support cli header section %d", i));
	}
}

static guint32
pad4 (guint32 offset)
{
	if (offset & 0x3) //pad to the next 4 byte boundary
		offset = (offset & ~0x3) + 4;
	return offset;
}

static void
verify_metadata_header (VerifyContext *ctx)
{
	int i;
	DataDirectory it = get_data_dir (ctx, CLI_HEADER_IDX);
	guint32 offset;
	const char *ptr;

	offset = it.translated_offset;
	ptr = ctx->data + offset;
	g_assert (offset != INVALID_OFFSET);

	//build a directory entry for the metadata root
	ptr += 8;
	it.rva = read32 (ptr);
	ptr += 4;
	it.size = read32 (ptr);
	it.translated_offset = offset = translate_rva (ctx, it.rva);

	ptr = ctx->data + offset;
	g_assert (offset != INVALID_OFFSET);

	if (it.size < 20)
		ADD_ERROR (ctx, g_strdup_printf ("Metadata root section is too small %d (at least 20 bytes required for initial decoding)", it.size));

	if (read32 (ptr) != 0x424A5342)
		ADD_ERROR (ctx, g_strdup_printf ("Invalid metadata signature, expected 0x424A5342 but got %08x", read32 (ptr)));

	offset = pad4 (offset + 16 + read32 (ptr + 12));

	if (!bounds_check_datadir (&it, offset, 4))
		ADD_ERROR (ctx, g_strdup_printf ("Metadata root section is too small %d (at least %d bytes required for flags decoding)", it.size, offset + 4 - it.translated_offset));

	ptr = ctx->data + offset; //move to streams header 

	if (read16 (ptr + 2) != 5)
		ADD_ERROR (ctx, g_strdup_printf ("Metadata root section have %d streams (it must have exactly 5)", read16 (ptr + 2)));

	ptr += 4;
	offset += 4;

	for (i = 0; i < 5; ++i) {
		guint32 stream_off, stream_size;
		int string_size, stream_idx;

		if (!bounds_check_datadir (&it, offset, 8))
			ADD_ERROR (ctx, g_strdup_printf ("Metadata root section is too small for initial decode of stream header %d, missing %d bytes", i, offset + 9 - it.translated_offset));

		stream_off = it.translated_offset + read32 (ptr);
		stream_size = read32 (ptr + 4);

		if (!bounds_check_datadir (&it,  stream_off, stream_size))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid stream header %d offset/size pair %x/%x", 0, stream_off, stream_size));

		ptr += 8;
		offset += 8;

		for (string_size = 0; string_size < 32; ++string_size) {
			if (!bounds_check_datadir (&it, offset++, 1))
				ADD_ERROR (ctx, g_strdup_printf ("Metadata root section is too small to decode stream header %d name", i));
			if (!ptr [string_size])
				break;
		}

		if (ptr [string_size])
			ADD_ERROR (ctx, g_strdup_printf ("Metadata stream header %d name larger than 32 bytes", i));

		if (!strncmp ("#Strings", ptr, 9))
			stream_idx = STRINGS_STREAM;
		else if (!strncmp ("#US", ptr, 4))
			stream_idx = USER_STRINGS_STREAM;
		else if (!strncmp ("#Blob", ptr, 6))
			stream_idx = BLOB_STREAM;
		else if (!strncmp ("#GUID", ptr, 6))
			stream_idx = GUID_STREAM;
		else if (!strncmp ("#~", ptr, 3))
			stream_idx = TILDE_STREAM;
		else
			ADD_ERROR (ctx, g_strdup_printf ("Metadata stream header %d invalid name %s", i, ptr));

		if (ctx->metadata_streams [stream_idx].offset != 0)
			ADD_ERROR (ctx, g_strdup_printf ("Duplicated metadata stream header %s", ptr));

		ctx->metadata_streams [stream_idx].offset = stream_off;
		ctx->metadata_streams [stream_idx].size = stream_size;

		offset = pad4 (offset);
		ptr = ctx->data + offset;
	}
}

static void
verify_tables_schema (VerifyContext *ctx)
{
	OffsetAndSize tables_area = ctx->metadata_streams [TILDE_STREAM];
	unsigned offset = tables_area.offset;
	const char *ptr = ctx->data + offset;
	guint64 valid_tables;
	guint32 count;
	int i;

	//printf ("tables_area size %d offset %x %s\n", tables_area.size, tables_area.offset, ctx->image->name);
	if (tables_area.size < 24)
		ADD_ERROR (ctx, g_strdup_printf ("Table schemata size (%d) too small to for initial decoding (requires 24 bytes)", tables_area.size));

	//printf ("ptr %x %x\n", ptr[4], ptr[5]);
	if (ptr [4] != 2 && ptr [4] != 1)
		ADD_ERROR (ctx, g_strdup_printf ("Invalid table schemata major version %d, expected 2", ptr [4]));
	if (ptr [5] != 0)
		ADD_ERROR (ctx, g_strdup_printf ("Invalid table schemata minor version %d, expected 0", ptr [5]));

	if ((ptr [6] & ~0x7) != 0)
		ADD_ERROR (ctx, g_strdup_printf ("Invalid table schemata heap sizes 0x%02x, only bits 0, 1 and 2 can be set", ((unsigned char *) ptr) [6]));

	valid_tables = read64 (ptr + 8);
	count = 0;
	for (i = 0; i < 64; ++i) {
		if (!(valid_tables & ((guint64)1 << i)))
			continue;

		/*MS Extensions: 0x3 0x5 0x7 0x13 0x16
 		  Unused: 0x1E 0x1F 0x2D-0x3F
 		  We don't care about the MS extensions.*/
		if (i == 0x3 || i == 0x5 || i == 0x7 || i == 0x13 || i == 0x16)
			ADD_ERROR (ctx, g_strdup_printf ("The metadata verifies doesn't support MS specific table %x", i));
		if (i == 0x1E || i == 0x1F || i >= 0x2D)
			ADD_ERROR (ctx, g_strdup_printf ("Invalid table %x", i));
		++count;
	}

	if (tables_area.size < 24 + count * 4)
		ADD_ERROR (ctx, g_strdup_printf ("Table schemata size (%d) too small to for decoding row counts (requires %d bytes)", tables_area.size, 24 + count * 4));
	ptr += 24;

	for (i = 0; i < 64; ++i) {
		if (valid_tables & ((guint64)1 << i)) {
			guint32 row_count = read32 (ptr);
			if (row_count > (1 << 25) - 1)
				ADD_ERROR (ctx, g_strdup_printf ("Invalid Table %d row count, mono only supports ", i));
			ptr += 4;
		}
	}
}

/*----------nothing from here on can use data_directory or metadata_streams ---*/

static guint32
get_col_offset (VerifyContext *ctx, int table, int column)
{
	guint32 bitfield = ctx->image->tables [table].size_bitfield;
	guint32 offset = 0;

	while (column-- > 0)
		offset += mono_metadata_table_size (bitfield, column);

	return offset;
}

static guint32
get_col_size (VerifyContext *ctx, int table, int column)
{
	return mono_metadata_table_size (ctx->image->tables [table].size_bitfield, column);
}

static OffsetAndSize
get_metadata_stream (VerifyContext *ctx, MonoStreamHeader *header)
{
	OffsetAndSize res;
	res.offset = header->data - ctx->data;
	res.size = header->size;

	return res;
}

static gboolean
is_valid_non_empty_string (VerifyContext *ctx, guint32 offset)
{
	OffsetAndSize strings = get_metadata_stream (ctx, &ctx->image->heap_strings);
	glong length;
	const char *data = ctx->data + strings.offset;

	if (offset >= strings.size)
		return FALSE;
	if (data + offset < data) //FIXME, use a generalized and smart unsigned add with overflow check and fix the whole thing  
		return FALSE;

	if (!mono_utf8_validate_and_len_with_bounds (data + offset, strings.size - offset, &length, NULL))
		return FALSE;
	return length > 0;
}

static gboolean
is_valid_guid (VerifyContext *ctx, guint32 offset)
{
	OffsetAndSize guids = get_metadata_stream (ctx, &ctx->image->heap_guid);
	return guids.size >= 8 && guids.size - 8 >= offset;
}

static guint32
get_coded_index_token (VerifyContext *ctx, int token_kind, guint32 coded_token)
{
	guint32 bits = coded_index_desc [token_kind];
	return coded_token >> bits;
}

static guint32
make_coded_token (int kind, guint32 table, guint32 table_idx)
{
	guint32 bits = coded_index_desc [kind++];
	guint32 tables = coded_index_desc [kind++];
	guint32 i;
	for (i = 0; i < tables; ++i) {
		if (coded_index_desc [kind++] == table)
			return ((table_idx + 1) << bits) | i; 
	}
	g_assert_not_reached ();
	return -1;
}

static gboolean
is_valid_coded_index (VerifyContext *ctx, int token_kind, guint32 coded_token)
{
	guint32 bits = coded_index_desc [token_kind++];
	guint32 table_count = coded_index_desc [token_kind++];
	guint32 table = coded_token & ((1 << bits) - 1);
	guint32 token = coded_token >> bits;

	if (table >= table_count)
		return FALSE;

	/*token_kind points to the first table idx*/
	table = coded_index_desc [token_kind + table];

	if (table == INVALID_TABLE)
		return FALSE;
	return token <= ctx->image->tables [table].rows;
}

typedef struct {
	guint32 token;
	guint32 col_size;
	guint32 col_offset;
} RowLocator;

static int
token_locator (const void *a, const void *b)
{
	RowLocator *loc = (RowLocator *)a;
	unsigned const char *row = (unsigned const char *)b;
	guint32 token = loc->col_size == 2 ? read16 (row + loc->col_offset) : read32 (row + loc->col_offset);

	VERIFIER_DEBUG ( printf ("\tfound token %x\n", token) );
	return (int)loc->token - (int)token;
}

static int
search_sorted_table (VerifyContext *ctx, int table, int column, guint32 coded_token)
{
	MonoTableInfo *tinfo = &ctx->image->tables [table];
	RowLocator locator;
	const char *res, *base;
	locator.token = coded_token;
	locator.col_offset = get_col_offset (ctx, table, column);
	locator.col_size = get_col_size (ctx, table, column);
	base = tinfo->base;

	VERIFIER_DEBUG ( printf ("looking token %x table %d col %d rsize %d roff %d\n", coded_token, table, column, locator.col_size, locator.col_offset) );
	res = bsearch (&locator, base, tinfo->rows, tinfo->row_size, token_locator);
	if (!res)
		return -1;

	return (res - base) / tinfo->rows;
}

/*WARNING: This function doesn't verify if the strings @offset points to a valid string*/
static const char*
get_string_ptr (VerifyContext *ctx, guint offset)
{
	return ctx->image->heap_strings.data + offset;
}

/*WARNING: This function doesn't verify if the strings @offset points to a valid string*/
static int
string_cmp (VerifyContext *ctx, const char *str, guint offset)
{
	if (offset == 0)
		return strcmp (str, "");

	return strcmp (str, get_string_ptr (ctx, offset));
}

static gboolean
typedef_is_system_object (VerifyContext *ctx, guint32 *data)
{
	return ctx->is_corlib && !string_cmp (ctx, "System", data [MONO_TYPEDEF_NAME]) && !string_cmp (ctx, "Object", data [MONO_TYPEDEF_NAMESPACE]);
}

static gboolean
is_valid_field_signature (VerifyContext *ctx, guint32 offset)
{
	OffsetAndSize blob = get_metadata_stream (ctx, &ctx->image->heap_blob);
	//TODO do proper verification
	return blob.size >= 2 && blob.size - 2 >= offset;
}

static gboolean
is_valid_method_signature (VerifyContext *ctx, guint32 offset)
{
	OffsetAndSize blob = get_metadata_stream (ctx, &ctx->image->heap_blob);
	//TODO do proper verification
	return blob.size >= 2 && blob.size - 2 >= offset;
}

static gboolean
is_valid_method_header (VerifyContext *ctx, guint32 rva)
{
	//TODO do proper method header validation
	return mono_cli_rva_image_map (ctx->image, rva) != INVALID_ADDRESS;
}

static void
verify_module_table (VerifyContext *ctx)
{
	MonoTableInfo *table = &ctx->image->tables [MONO_TABLE_MODULE];
	guint32 data [MONO_MODULE_SIZE];

	if (table->rows != 1)
		ADD_ERROR (ctx, g_strdup_printf ("Module table must have exactly one row, but have %d", table->rows));

	mono_metadata_decode_row (table, 0, data, MONO_MODULE_SIZE);

	if (!is_valid_non_empty_string (ctx, data [MONO_MODULE_NAME]))
		ADD_ERROR (ctx, g_strdup_printf ("Module has an invalid name, string index 0x%08x", data [MONO_MODULE_NAME]));

	if (!is_valid_guid (ctx, data [MONO_MODULE_MVID]))
		ADD_ERROR (ctx, g_strdup_printf ("Module has an invalid Mvid, guid index %x", data [MONO_MODULE_MVID]));

	if (data [MONO_MODULE_ENC] != 0)
		ADD_ERROR (ctx, g_strdup_printf ("Module has a non zero Enc field %x", data [MONO_MODULE_ENC]));

	if (data [MONO_MODULE_ENCBASE] != 0)
		ADD_ERROR (ctx, g_strdup_printf ("Module has a non zero EncBase field %x", data [MONO_MODULE_ENCBASE]));
}

static void
verify_typeref_table (VerifyContext *ctx)
{
	MonoTableInfo *table = &ctx->image->tables [MONO_TABLE_TYPEREF];
	guint32 data [MONO_TYPEREF_SIZE];
	int i;

	for (i = 0; i < table->rows; ++i) {
		mono_metadata_decode_row (table, i, data, MONO_TYPEREF_SIZE);
		if (!is_valid_coded_index (ctx, RES_SCOPE_DESC, data [MONO_TYPEREF_SCOPE]))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid typeref row %d coded index 0x%08x", i, data [MONO_TYPEREF_SCOPE]));
		
		if (!get_coded_index_token (ctx, RES_SCOPE_DESC, data [MONO_TYPEREF_SCOPE]))
			ADD_ERROR (ctx, g_strdup_printf ("The metadata verifier doesn't support null ResolutionScope tokens for typeref row %d", i));

		if (!data [MONO_TYPEREF_NAME] || !is_valid_non_empty_string (ctx, data [MONO_TYPEREF_NAME]))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid typeref row %d name token 0x%08x", i, data [MONO_TYPEREF_NAME]));

		if (data [MONO_TYPEREF_NAMESPACE] && !is_valid_non_empty_string (ctx, data [MONO_TYPEREF_NAMESPACE]))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid typeref row %d namespace token 0x%08x", i, data [MONO_TYPEREF_NAMESPACE]));
	}
}

/*bits 9,11,14,15,19,21,24-31 */
#define INVALID_TYPEDEF_FLAG_BITS ((1 << 9) | (1 << 11) | (1 << 14) | (1 << 15) | (1 << 19) | (1 << 21) | 0xFF000000)
static void
verify_typedef_table (VerifyContext *ctx)
{
	MonoTableInfo *table = &ctx->image->tables [MONO_TABLE_TYPEDEF];
	guint32 data [MONO_TYPEDEF_SIZE];
	guint32 fieldlist = 1, methodlist = 1;
	int i;

	if (table->rows == 0)
		ADD_ERROR (ctx, g_strdup_printf ("Typedef table must have exactly at least one row"));

	for (i = 0; i < table->rows; ++i) {
		mono_metadata_decode_row (table, i, data, MONO_TYPEDEF_SIZE);
		if (data [MONO_TYPEDEF_FLAGS] & INVALID_TYPEDEF_FLAG_BITS)
			ADD_ERROR (ctx, g_strdup_printf ("Invalid typedef row %d invalid flags field 0x%08x", i, data [MONO_TYPEDEF_FLAGS]));

		if ((data [MONO_TYPEDEF_FLAGS] & TYPE_ATTRIBUTE_LAYOUT_MASK) == 0x18)
			ADD_ERROR (ctx, g_strdup_printf ("Invalid typedef row %d invalid class layout 0x18", i));

		if ((data [MONO_TYPEDEF_FLAGS] & TYPE_ATTRIBUTE_STRING_FORMAT_MASK) == 0x30000)
			ADD_ERROR (ctx, g_strdup_printf ("Invalid typedef row %d mono doesn't support custom string format", i));

		if ((data [MONO_TYPEDEF_FLAGS] & 0xC00000) != 0)
			ADD_ERROR (ctx, g_strdup_printf ("Invalid typedef row %d mono doesn't support custom string format", i));

		if (!data [MONO_TYPEDEF_NAME] || !is_valid_non_empty_string (ctx, data [MONO_TYPEDEF_NAME]))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid typedef row %d invalid name token %08x", i, data [MONO_TYPEDEF_NAME]));

		if (data [MONO_TYPEREF_NAMESPACE] && !is_valid_non_empty_string (ctx, data [MONO_TYPEREF_NAMESPACE]))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid typedef row %d invalid namespace token %08x", i, data [MONO_TYPEREF_NAMESPACE]));

		if (i == 0) {
			if (data [MONO_TYPEDEF_EXTENDS] != 0)
				ADD_ERROR (ctx, g_strdup_printf ("Invalid typedef row 0 for the special <module> type must have a null extend field"));
		} else {
			if (typedef_is_system_object (ctx, data) && data [MONO_TYPEDEF_EXTENDS] != 0)
				ADD_ERROR (ctx, g_strdup_printf ("Invalid typedef row %d for System.Object must have a null extend field", i));
	
			if (data [MONO_TYPEDEF_FLAGS] & TYPE_ATTRIBUTE_INTERFACE) {
				if (data [MONO_TYPEDEF_EXTENDS])
					ADD_ERROR (ctx, g_strdup_printf ("Invalid typedef row %d for interface type must have a null extend field", i));
				if ((data [MONO_TYPEDEF_FLAGS] & TYPE_ATTRIBUTE_ABSTRACT) == 0)
					ADD_ERROR (ctx, g_strdup_printf ("Invalid typedef row %d for interface type must be abstract", i));
			} else {
				if (!is_valid_coded_index (ctx, TYPEDEF_OR_REF_DESC, data [MONO_TYPEDEF_EXTENDS]))
					ADD_ERROR (ctx, g_strdup_printf ("Invalid typedef row %d extend field coded index 0x%08x", i, data [MONO_TYPEDEF_EXTENDS]));
	
				if (!get_coded_index_token (ctx, TYPEDEF_OR_REF_DESC, data [MONO_TYPEDEF_EXTENDS])) 
					ADD_ERROR (ctx, g_strdup_printf ("Invalid typedef row %d for non-interface type must have a non-null extend field", i));
			}
		}

		if (data [MONO_TYPEDEF_FIELD_LIST] == 0)
			ADD_ERROR (ctx, g_strdup_printf ("Invalid typedef row %d FieldList be be >= 1", i));

		if (data [MONO_TYPEDEF_FIELD_LIST] < fieldlist)
			ADD_ERROR (ctx, g_strdup_printf ("Invalid typedef row %d FieldList rowid 0x%08x can't be smaller than of previous row 0x%08x", i, data [MONO_TYPEDEF_FIELD_LIST], fieldlist));

		if (data [MONO_TYPEDEF_METHOD_LIST] == 0)
			ADD_ERROR (ctx, g_strdup_printf ("Invalid typedef row %d MethodList be be >= 1", i));

		if (data [MONO_TYPEDEF_METHOD_LIST] < methodlist)
			ADD_ERROR (ctx, g_strdup_printf ("Invalid typedef row %d MethodList rowid 0x%08x can't be smaller than of previous row 0x%08x", i, data [MONO_TYPEDEF_METHOD_LIST], methodlist));

		fieldlist = data [MONO_TYPEDEF_FIELD_LIST];
		methodlist = data [MONO_TYPEDEF_METHOD_LIST];
	}
}

/*bits 3,11,14 */
#define INVALID_FIELD_FLAG_BITS ((1 << 3) | (1 << 11) | (1 << 14))
static void
verify_field_table (VerifyContext *ctx)
{
	MonoTableInfo *table = &ctx->image->tables [MONO_TABLE_FIELD];
	guint32 data [MONO_FIELD_SIZE], flags, module_field_list;
	int i;

	module_field_list = (guint32)-1;
	if (ctx->image->tables [MONO_TABLE_TYPEDEF].rows > 1) {
		MonoTableInfo *type = &ctx->image->tables [MONO_TABLE_TYPEDEF];
		module_field_list = mono_metadata_decode_row_col (type, 1, MONO_TYPEDEF_FIELD_LIST);
	}
	
	for (i = 0; i < table->rows; ++i) {
		mono_metadata_decode_row (table, i, data, MONO_FIELD_SIZE);
		flags = data [MONO_FIELD_FLAGS];

		if (flags & INVALID_FIELD_FLAG_BITS)
			ADD_ERROR (ctx, g_strdup_printf ("Invalid field row %d invalid flags field 0x%08x", i, flags));

		if ((flags & FIELD_ATTRIBUTE_FIELD_ACCESS_MASK) == 0x7)		
			ADD_ERROR (ctx, g_strdup_printf ("Invalid field row %d invalid field visibility 0x7", i));

		if ((flags & (FIELD_ATTRIBUTE_LITERAL | FIELD_ATTRIBUTE_INIT_ONLY)) == (FIELD_ATTRIBUTE_LITERAL | FIELD_ATTRIBUTE_INIT_ONLY))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid field row %d cannot be InitOnly and Literal at the same time", i));

		if ((flags & FIELD_ATTRIBUTE_RT_SPECIAL_NAME) && !(flags & FIELD_ATTRIBUTE_SPECIAL_NAME))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid field row %d is RTSpecialName but not SpecialName", i));

		if ((flags & FIELD_ATTRIBUTE_LITERAL) && !(flags & FIELD_ATTRIBUTE_STATIC))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid field row %d is Literal but not Static", i));

		if ((flags & FIELD_ATTRIBUTE_HAS_FIELD_MARSHAL) &&
				search_sorted_table (ctx, MONO_TABLE_FIELDMARSHAL, MONO_FIELD_MARSHAL_PARENT, make_coded_token (HAS_FIELD_MARSHAL_DESC, MONO_TABLE_FIELD, i)) == -1)
			ADD_ERROR (ctx, g_strdup_printf ("Invalid field row %d has FieldMarshal but there is no corresponding row in the FieldMarshal table", i));

		if ((flags & FIELD_ATTRIBUTE_HAS_DEFAULT) &&
				search_sorted_table (ctx, MONO_TABLE_CONSTANT, MONO_CONSTANT_PARENT, make_coded_token (HAS_CONSTANT_DESC, MONO_TABLE_FIELD, i)) == -1)
			ADD_ERROR (ctx, g_strdup_printf ("Invalid field row %d has Default but there is no corresponding row in the Constant table", i));

		if ((flags & FIELD_ATTRIBUTE_LITERAL) &&
				search_sorted_table (ctx, MONO_TABLE_CONSTANT, MONO_CONSTANT_PARENT, make_coded_token (HAS_CONSTANT_DESC, MONO_TABLE_FIELD, i)) == -1)
			ADD_ERROR (ctx, g_strdup_printf ("Invalid field row %d is Literal but there is no corresponding row in the Constant table", i));

		if ((flags & FIELD_ATTRIBUTE_HAS_FIELD_RVA) &&
				search_sorted_table (ctx, MONO_TABLE_FIELDRVA, MONO_FIELD_RVA_FIELD, i + 1) == -1)
			ADD_ERROR (ctx, g_strdup_printf ("Invalid field row %d has Default but there is no corresponding row in the Constant table", i));

		if (!data [MONO_FIELD_NAME] || !is_valid_non_empty_string (ctx, data [MONO_FIELD_NAME]))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid field row %d invalid name token %08x", i, data [MONO_FIELD_NAME]));

		//TODO verify contant flag
		if (!data [MONO_FIELD_SIGNATURE] || !is_valid_field_signature (ctx, data [MONO_FIELD_SIGNATURE]))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid field row %d invalid signature token %08x", i, data [MONO_FIELD_SIGNATURE]));

		if (i + 1 < module_field_list) {
			guint32 access = flags & FIELD_ATTRIBUTE_FIELD_ACCESS_MASK;
			if (!(flags & FIELD_ATTRIBUTE_STATIC))
				ADD_ERROR (ctx, g_strdup_printf ("Invalid field row %d is a global variable but is not static", i));
			if (access != FIELD_ATTRIBUTE_COMPILER_CONTROLLED && access != FIELD_ATTRIBUTE_PRIVATE && access != FIELD_ATTRIBUTE_PUBLIC)
				ADD_ERROR (ctx, g_strdup_printf ("Invalid field row %d is a global variable but have wrong visibility %x", i, access));
		}
	}
}

/*bits 6,8,9,10,11,13,14,15*/
#define INVALID_METHOD_IMPLFLAG_BITS ((1 << 6) | (1 << 8) | (1 << 9) | (1 << 10) | (1 << 11) | (1 << 13) | (1 << 14) | (1 << 15))
static void
verify_method_table (VerifyContext *ctx)
{
	MonoTableInfo *table = &ctx->image->tables [MONO_TABLE_METHOD];
	guint32 data [MONO_METHOD_SIZE], flags, implflags, rva, module_method_list, access, code_type;
	gboolean is_ctor, is_cctor;
	const char *name;
	int i;

	module_method_list = (guint32)-1;
	if (ctx->image->tables [MONO_TABLE_TYPEDEF].rows > 1) {
		MonoTableInfo *type = &ctx->image->tables [MONO_TABLE_TYPEDEF];
		module_method_list = mono_metadata_decode_row_col (type, 1, MONO_TYPEDEF_METHOD_LIST);
	}

	for (i = 0; i < table->rows; ++i) {
		mono_metadata_decode_row (table, i, data, MONO_METHOD_SIZE);
		rva = data [MONO_METHOD_RVA];
		implflags = data [MONO_METHOD_IMPLFLAGS];
		flags = data [MONO_METHOD_FLAGS];
		access = flags & METHOD_ATTRIBUTE_MEMBER_ACCESS_MASK;
		code_type = implflags & METHOD_IMPL_ATTRIBUTE_CODE_TYPE_MASK;
		

		if (implflags & INVALID_METHOD_IMPLFLAG_BITS)
			ADD_ERROR (ctx, g_strdup_printf ("Invalid method row %d invalid implflags field 0x%08x", i, implflags));

		if (access == 0x7)
			ADD_ERROR (ctx, g_strdup_printf ("Invalid method row %d invalid MemberAccessMask 0x7", i));

		if (!data [MONO_METHOD_NAME] || !is_valid_non_empty_string (ctx, data [MONO_METHOD_NAME]))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid method row %d invalid name field 0x%08x", i, data [MONO_METHOD_NAME]));

		name = get_string_ptr (ctx, data [MONO_METHOD_NAME]);
		is_ctor = !strcmp (".ctor", name);
		is_cctor = !strcmp (".cctor", name);

		if ((is_ctor || is_cctor) &&
			search_sorted_table (ctx, MONO_TABLE_GENERICPARAM, MONO_GENERICPARAM_OWNER, make_coded_token (TYPE_OR_METHODDEF_DESC, MONO_TABLE_METHOD, i)) != -1)
			ADD_ERROR (ctx, g_strdup_printf ("Invalid method row %d .ctor or .cctor has generic param", i));

		if ((flags & METHOD_ATTRIBUTE_STATIC) && (flags & (METHOD_ATTRIBUTE_FINAL | METHOD_ATTRIBUTE_VIRTUAL | METHOD_ATTRIBUTE_NEW_SLOT)))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid method row %d is static and (final, virtual or new slot)", i));
		
		if (flags & METHOD_ATTRIBUTE_ABSTRACT) {
			if (flags & METHOD_ATTRIBUTE_PINVOKE_IMPL)
				ADD_ERROR (ctx, g_strdup_printf ("Invalid method row %d is Abstract and PinvokeImpl", i));
			if (!(flags & METHOD_ATTRIBUTE_VIRTUAL))
				ADD_ERROR (ctx, g_strdup_printf ("Invalid method row %d is Abstract but not Virtual", i));
		}

		if (access == METHOD_ATTRIBUTE_COMPILER_CONTROLLED && (flags & (METHOD_ATTRIBUTE_RT_SPECIAL_NAME | METHOD_ATTRIBUTE_SPECIAL_NAME)))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid method row %d is CompileControlled and SpecialName or RtSpecialName", i));

		if ((flags & METHOD_ATTRIBUTE_RT_SPECIAL_NAME) && !(flags & METHOD_ATTRIBUTE_SPECIAL_NAME))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid method row %d is RTSpecialName but not SpecialName", i));

		//XXX no checks against cas stuff 10,11,12,13)

		//TODO check iface with .ctor (15,16)

		if (!data [MONO_METHOD_SIGNATURE] || !is_valid_method_signature (ctx, data [MONO_METHOD_SIGNATURE]))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid method row %d invalid signature token %08x", i, data [MONO_METHOD_SIGNATURE]));

		if (i + 1 < module_method_list) {
			if (!(flags & METHOD_ATTRIBUTE_STATIC))
				ADD_ERROR (ctx, g_strdup_printf ("Invalid method row %d is a global method but not Static", i));
			if (flags & (METHOD_ATTRIBUTE_ABSTRACT | METHOD_ATTRIBUTE_VIRTUAL))
				ADD_ERROR (ctx, g_strdup_printf ("Invalid method row %d is a global method but is Abstract or Virtual", i));
			if (!(access == METHOD_ATTRIBUTE_COMPILER_CONTROLLED || access == METHOD_ATTRIBUTE_PUBLIC || access == METHOD_ATTRIBUTE_PRIVATE))
				ADD_ERROR (ctx, g_strdup_printf ("Invalid method row %d is a global method but not CompilerControled, Public or Private", i));
		}

		//TODO check valuetype for synchronized

		if ((flags & (METHOD_ATTRIBUTE_FINAL | METHOD_ATTRIBUTE_NEW_SLOT | METHOD_ATTRIBUTE_STRICT)) && !(flags & METHOD_ATTRIBUTE_VIRTUAL))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid method row %d is (Final, NewSlot or Strict) but not Virtual", i));

		if ((flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) && (flags & METHOD_ATTRIBUTE_VIRTUAL))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid method row %d is PinvokeImpl and Virtual", i));

		if (!(flags & METHOD_ATTRIBUTE_ABSTRACT) && !rva && !(flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) && 
				!(implflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) && code_type != METHOD_IMPL_ATTRIBUTE_RUNTIME)
			ADD_ERROR (ctx, g_strdup_printf ("Invalid method row %d is not Abstract and neither PinvokeImpl, Runtime, InternalCall or with RVA != 0", i));

		if (access == METHOD_ATTRIBUTE_COMPILER_CONTROLLED && !(rva || (flags & METHOD_ATTRIBUTE_PINVOKE_IMPL)))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid method row %d is CompilerControlled but neither RVA != 0 or PinvokeImpl", i));

		//TODO check signature contents

		if (rva) {
			if (flags & METHOD_ATTRIBUTE_ABSTRACT)
				ADD_ERROR (ctx, g_strdup_printf ("Invalid method row %d has RVA != 0 but is Abstract", i));
			if (code_type == METHOD_IMPL_ATTRIBUTE_OPTIL)
				ADD_ERROR (ctx, g_strdup_printf ("Invalid method row %d has RVA != 0 but is CodeTypeMask is neither Native, CIL or Runtime", i));
			if (!is_valid_method_header (ctx, rva))
				ADD_ERROR (ctx, g_strdup_printf ("Invalid method row %d RVA points to an invalid method header", i));
		} else {
			if (!(flags & (METHOD_ATTRIBUTE_ABSTRACT | METHOD_ATTRIBUTE_PINVOKE_IMPL)) && !(implflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) && code_type != METHOD_IMPL_ATTRIBUTE_RUNTIME)
				ADD_ERROR (ctx, g_strdup_printf ("Invalid method row %d has RVA = 0 but neither Abstract, InternalCall, Runtime or PinvokeImpl", i));
		}

		if ((flags & METHOD_ATTRIBUTE_PINVOKE_IMPL)) {
			if (rva)
				ADD_ERROR (ctx, g_strdup_printf ("Invalid method row %d is PinvokeImpl but has RVA != 0", i));
			if (search_sorted_table (ctx, MONO_TABLE_IMPLMAP, MONO_IMPLMAP_MEMBER, make_coded_token (MEMBER_FORWARDED_DESC, MONO_TABLE_METHOD, i)) == -1)
				ADD_ERROR (ctx, g_strdup_printf ("Invalid method row %d is PinvokeImpl but has no row in the ImplMap table", i));
		}
		if (flags & METHOD_ATTRIBUTE_RT_SPECIAL_NAME && !is_ctor && !is_cctor)
			ADD_ERROR (ctx, g_strdup_printf ("Invalid method row %d is RtSpecialName but not named .ctor or .cctor", i));

		if ((is_ctor || is_cctor) && !(flags & METHOD_ATTRIBUTE_RT_SPECIAL_NAME))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid method row %d is named .ctor or .cctor but is not RtSpecialName", i));
	}
}

static void
verify_tables_data (VerifyContext *ctx)
{
	OffsetAndSize tables_area = get_metadata_stream (ctx, &ctx->image->heap_tables);
	guint32 size = 0, tables_offset;
	int i;

	for (i = 0; i < 0x2D; ++i) {
		MonoTableInfo *table = &ctx->image->tables [i];
		guint32 tmp_size;
		tmp_size = size + (guint32)table->row_size * (guint32)table->rows;
		if (tmp_size < size) {
			size = 0;
			break;
		}
		size = tmp_size;			
	}

	if (size == 0)
		ADD_ERROR (ctx, g_strdup_printf ("table space is either empty or overflowed"));

	tables_offset = ctx->image->tables_base - ctx->data;
	if (!bounds_check_offset (&tables_area, tables_offset, size))
		ADD_ERROR (ctx, g_strdup_printf ("Tables data require %d bytes but the only %d are available in the #~ stream", size, tables_area.size - (tables_offset - tables_area.offset)));

	verify_module_table (ctx);
	CHECK_ERROR ();
	verify_typeref_table (ctx);
	CHECK_ERROR ();
	verify_typedef_table (ctx);
	CHECK_ERROR ();
	verify_field_table (ctx);
	CHECK_ERROR ();
	verify_method_table (ctx);
}

static gboolean
mono_verifier_is_corlib (MonoImage *image)
{
	gboolean trusted_location = (mono_security_get_mode () != MONO_SECURITY_MODE_CORE_CLR) ? 
			TRUE : mono_security_core_clr_is_platform_image (image);

	return trusted_location && !strcmp ("mscorlib.dll", image->name);
}

static void
init_verify_context (VerifyContext *ctx, MonoImage *image, GSList **error_list)
{
	memset (ctx, 0, sizeof (VerifyContext));
	ctx->image = image;
	ctx->report_error = error_list != NULL;
	ctx->valid = 1;
	ctx->size = image->raw_data_len;
	ctx->data = image->raw_data;
	ctx->is_corlib = mono_verifier_is_corlib (image);	
}

static gboolean
cleanup_context (VerifyContext *ctx, GSList **error_list)
{
	g_free (ctx->sections);
	if (error_list)
		*error_list = ctx->errors;
	else
		mono_free_verify_list (ctx->errors);
	return ctx->valid;	
}

gboolean
mono_verifier_verify_pe_data (MonoImage *image, GSList **error_list)
{
	VerifyContext ctx;

	if (!mono_verifier_is_enabled_for_image (image))
		return TRUE;

	init_verify_context (&ctx, image, error_list);
	ctx.stage = STAGE_PE;

	verify_msdos_header (&ctx);
	CHECK_STATE();
	verify_pe_header (&ctx);
	CHECK_STATE();
	verify_pe_optional_header (&ctx);
	CHECK_STATE();
	load_section_table (&ctx);
	CHECK_STATE();
	load_data_directories (&ctx);
	CHECK_STATE();
	verify_import_table (&ctx);
	CHECK_STATE();
	/*No need to check the IAT directory entry, it's content is indirectly verified by verify_import_table*/
	verify_resources_table (&ctx);

cleanup:
	return cleanup_context (&ctx, error_list);
}

gboolean
mono_verifier_verify_cli_data (MonoImage *image, GSList **error_list)
{
	VerifyContext ctx;

	if (!mono_verifier_is_enabled_for_image (image))
		return TRUE;

	init_verify_context (&ctx, image, error_list);
	ctx.stage = STAGE_CLI;

	verify_cli_header (&ctx);
	CHECK_STATE();
	verify_metadata_header (&ctx);
	CHECK_STATE();
	verify_tables_schema (&ctx);

cleanup:
	return cleanup_context (&ctx, error_list);
}

gboolean
mono_verifier_verify_table_data (MonoImage *image, GSList **error_list)
{
	VerifyContext ctx;

	if (!mono_verifier_is_enabled_for_image (image))
		return TRUE;

	init_verify_context (&ctx, image, error_list);
	ctx.stage = STAGE_TABLES;

	verify_tables_data (&ctx);

	return cleanup_context (&ctx, error_list);
}
