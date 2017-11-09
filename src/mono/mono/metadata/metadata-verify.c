/**
 * \file
 * Metadata verfication support
 *
 * Author:
 *	Mono Project (http://www.mono-project.com)
 *
 * Copyright (C) 2005-2008 Novell, Inc. (http://www.novell.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include <config.h>
#include <mono/metadata/object-internals.h>
#include <mono/metadata/verify.h>
#include <mono/metadata/verify-internals.h>
#include <mono/metadata/opcodes.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/reflection.h>
#include <mono/metadata/reflection-internals.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/mono-endian.h>
#include <mono/metadata/metadata.h>
#include <mono/metadata/metadata-internals.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/security-manager.h>
#include <mono/metadata/security-core-clr.h>
#include <mono/metadata/cil-coff.h>
#include <mono/metadata/attrdefs.h>
#include <mono/utils/strenc.h>
#include <mono/utils/mono-error-internals.h>
#include <mono/utils/bsearch.h>
#include <string.h>
//#include <signal.h>
#include <ctype.h>

#ifndef DISABLE_VERIFIER
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
	20, /*tables*/
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
	MONO_TABLE_GENERICPARAM,

#define HAS_FIELD_MARSHAL_DESC (HAS_CATTR_DESC + 22)
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
	MONO_TABLE_MODULEREF,
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
	guint32 size, token;
	GSList *errors;
	int valid;
	MonoImage *image;
	gboolean report_error;
	gboolean report_warning;
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

#define ADD_WARNING(__ctx, __msg)	\
	do {	\
		if ((__ctx)->report_warning) { \
			ADD_VERIFY_INFO(__ctx, __msg, MONO_VERIFY_WARNING, MONO_EXCEPTION_INVALID_PROGRAM); \
			(__ctx)->valid = 0; \
			return; \
		} \
	} while (0)

#define ADD_ERROR_NO_RETURN(__ctx, __msg)	\
	do {	\
		if ((__ctx)->report_error) \
			ADD_VERIFY_INFO(__ctx, __msg, MONO_VERIFY_ERROR, MONO_EXCEPTION_INVALID_PROGRAM); \
		(__ctx)->valid = 0; \
	} while (0)

#define ADD_ERROR(__ctx, __msg)	\
	do {	\
		if ((__ctx)->report_error) \
			ADD_VERIFY_INFO(__ctx, __msg, MONO_VERIFY_ERROR, MONO_EXCEPTION_INVALID_PROGRAM); \
		(__ctx)->valid = 0; \
		return; \
	} while (0)

#define FAIL(__ctx, __msg)	\
	do {	\
		if ((__ctx)->report_error) \
			ADD_VERIFY_INFO(__ctx, __msg, MONO_VERIFY_ERROR, MONO_EXCEPTION_INVALID_PROGRAM); \
		(__ctx)->valid = 0; \
		return FALSE; \
	} while (0)

#define CHECK_STATE() do { if (!ctx.valid) goto cleanup; } while (0)

#define CHECK_ERROR() do { if (!ctx->valid) return; } while (0)

#define CHECK_ADD4_OVERFLOW_UN(a, b) ((guint32)(0xFFFFFFFFU) - (guint32)(b) < (guint32)(a))
#define CHECK_ADD8_OVERFLOW_UN(a, b) ((guint64)(0xFFFFFFFFFFFFFFFFUL) - (guint64)(b) < (guint64)(a))

#if SIZEOF_VOID_P == 4
#define CHECK_ADDP_OVERFLOW_UN(a,b) CHECK_ADD4_OVERFLOW_UN(a, b)
#else
#define CHECK_ADDP_OVERFLOW_UN(a,b) CHECK_ADD8_OVERFLOW_UN(a, b)
#endif

#define ADDP_IS_GREATER_OR_OVF(a, b, c) (((a) + (b) > (c)) || CHECK_ADDP_OVERFLOW_UN (a, b))
#define ADD_IS_GREATER_OR_OVF(a, b, c) (((a) + (b) > (c)) || CHECK_ADD4_OVERFLOW_UN (a, b))

static const char *
dword_align (const char *ptr)
{
#if SIZEOF_VOID_P == 8
	return (const char *) (((guint64) (ptr + 3)) & ~3);
#else
	return (const char *) (((guint32) (ptr + 3)) & ~3);
#endif
}

static void
add_from_mono_error (VerifyContext *ctx, MonoError *error)
{
	if (mono_error_ok (error))
		return;

	ADD_ERROR (ctx, g_strdup (mono_error_get_message (error)));
	mono_error_cleanup (error);
}

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
		MonoCLIImageInfo *iinfo = (MonoCLIImageInfo *)ctx->image->image_info;
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
	if (ilt_rva && !bounds_check_virtual_address (ctx, ilt_rva, 8))
		ADD_ERROR (ctx, g_strdup_printf ("Invalid Import Lookup Table rva %x", ilt_rva));

	name_rva = read32 (ptr + 12);
	if (name_rva && !bounds_check_virtual_address (ctx, name_rva, SIZE_OF_MSCOREE))
		ADD_ERROR (ctx, g_strdup_printf ("Invalid Import Table Name rva %x", name_rva));

	iat_rva = read32 (ptr + 16);
	if (iat_rva) {
		if (!bounds_check_virtual_address (ctx, iat_rva, 8))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid Import Address Table rva %x", iat_rva));

		if (iat_rva != ctx->data_directories [IAT_IDX].rva)
			ADD_ERROR (ctx, g_strdup_printf ("Import Address Table rva %x different from data directory entry %x", read32 (ptr + 16), ctx->data_directories [IAT_IDX].rva));
	}

	if (name_rva) {
		name_rva = translate_rva (ctx, name_rva);
		g_assert (name_rva != INVALID_OFFSET);
		ptr = ctx->data + name_rva;
	
		if (memcmp ("mscoree.dll", ptr, SIZE_OF_MSCOREE)) {
			char name[SIZE_OF_MSCOREE];
			memcpy (name, ptr, SIZE_OF_MSCOREE);
			name [SIZE_OF_MSCOREE - 1] = 0;
			ADD_ERROR (ctx, g_strdup_printf ("Invalid Import Table Name: '%s'", name));
		}
	}
	
	if (ilt_rva) {
		verify_hint_name_table (ctx, ilt_rva, "Import Lookup Table");
		CHECK_ERROR ();
	}

	if (iat_rva)
		verify_hint_name_table (ctx, iat_rva, "Import Address Table");
}

static void
verify_resources_table (VerifyContext *ctx)
{
	DataDirectory it = ctx->data_directories [RESOURCE_TABLE_IDX];
	guint32 offset;
	guint16 named_entries, id_entries;
	const char *ptr;

	if (it.rva == 0)
		return;

	if (it.size < 16)
		ADD_ERROR (ctx, g_strdup_printf ("Resource section is too small, must be at least 16 bytes long but it's %d long", it.size));

	offset = it.translated_offset;
	ptr = ctx->data + offset;

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
	MonoCLIImageInfo *iinfo = (MonoCLIImageInfo *)ctx->image->image_info;
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

	if ((read32 (ptr + 16) & ~0x0003000B) != 0)
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
	guint32 offset, section_count;
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

	section_count = read16 (ptr + 2);
	if (section_count < 2)
		ADD_ERROR (ctx, g_strdup_printf ("Metadata root section must have at least 2 streams (#~ and #GUID)"));

	ptr += 4;
	offset += 4;

	for (i = 0; i < section_count; ++i) {
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
		else {
			ADD_WARNING (ctx, g_strdup_printf ("Metadata stream header %d invalid name %s", i, ptr));
			offset = pad4 (offset);
			ptr = ctx->data + offset;
			continue;
		}

		if (ctx->metadata_streams [stream_idx].offset != 0)
			ADD_ERROR (ctx, g_strdup_printf ("Duplicated metadata stream header %s", ptr));

		ctx->metadata_streams [stream_idx].offset = stream_off;
		ctx->metadata_streams [stream_idx].size = stream_size;

		offset = pad4 (offset);
		ptr = ctx->data + offset;
	}

	if (!ctx->metadata_streams [TILDE_STREAM].size)
		ADD_ERROR (ctx, g_strdup_printf ("Metadata #~ stream missing"));
	if (!ctx->metadata_streams [GUID_STREAM].size)
		ADD_ERROR (ctx, g_strdup_printf ("Metadata guid stream missing"));
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

	if (tables_area.size < 24)
		ADD_ERROR (ctx, g_strdup_printf ("Table schemata size (%d) too small to for initial decoding (requires 24 bytes)", tables_area.size));

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
			ADD_ERROR (ctx, g_strdup_printf ("The metadata verifier doesn't support MS specific table %x", i));
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
			if (row_count > (1 << 24) - 1)
				ADD_ERROR (ctx, g_strdup_printf ("Invalid Table %d row count: %d. Mono only supports 16777215 rows", i, row_count));
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
is_valid_string_full_with_image (MonoImage *image, guint32 offset, gboolean allow_empty)
{
	guint32 heap_offset = (char*)image->heap_strings.data - image->raw_data;
	guint32 heap_size = image->heap_strings.size;

	glong length;
	const char *data = image->raw_data + heap_offset;

	if (offset >= heap_size)
		return FALSE;
	if (CHECK_ADDP_OVERFLOW_UN (data, offset))
		return FALSE;

	if (!mono_utf8_validate_and_len_with_bounds (data + offset, heap_size - offset, &length, NULL))
		return FALSE;
	return allow_empty || length > 0;
}


static gboolean
is_valid_string_full (VerifyContext *ctx, guint32 offset, gboolean allow_empty)
{
	return is_valid_string_full_with_image (ctx->image, offset, allow_empty);
}

static gboolean
is_valid_string (VerifyContext *ctx, guint32 offset)
{
	return is_valid_string_full (ctx, offset, TRUE);
}

static gboolean
is_valid_non_empty_string (VerifyContext *ctx, guint32 offset)
{
	return is_valid_string_full (ctx, offset, FALSE);
}

static gboolean
is_valid_guid (VerifyContext *ctx, guint32 offset)
{
	OffsetAndSize guids = get_metadata_stream (ctx, &ctx->image->heap_guid);
	return guids.size >= 8 && guids.size - 8 >= offset;
}

static guint32
get_coded_index_token (int token_kind, guint32 coded_token)
{
	guint32 bits = coded_index_desc [token_kind];
	return coded_token >> bits;
}

static guint32
get_coded_index_table (int kind, guint32 coded_token)
{
	guint32 idx, bits = coded_index_desc [kind];
	kind += 2;
	idx = coded_token & ((1 << bits) - 1);
	return coded_index_desc [kind + idx];
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
is_valid_coded_index_with_image (MonoImage *image, int token_kind, guint32 coded_token)
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
	return token <= image->tables [table].rows;
}

static gboolean
is_valid_coded_index (VerifyContext *ctx, int token_kind, guint32 coded_token)
{
	return is_valid_coded_index_with_image (ctx->image, token_kind, coded_token);
}

typedef struct {
	guint32 token;
	guint32 col_size;
	guint32 col_offset;
	MonoTableInfo *table;
} RowLocator;

static int
token_locator (const void *a, const void *b)
{
	RowLocator *loc = (RowLocator *)a;
	unsigned const char *row = (unsigned const char *)b;
	guint32 token = loc->col_size == 2 ? read16 (row + loc->col_offset) : read32 (row + loc->col_offset);

	VERIFIER_DEBUG ( printf ("\tfound token %x at idx %d\n", token, ((const char*)row - loc->table->base) / loc->table->row_size) );
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
	locator.table = tinfo;

	base = tinfo->base;

	VERIFIER_DEBUG ( printf ("looking token %x table %d col %d rsize %d roff %d\n", coded_token, table, column, locator.col_size, locator.col_offset) );
	res = (const char *)mono_binary_search (&locator, base, tinfo->rows, tinfo->row_size, token_locator);
	if (!res)
		return -1;

	return (res - base) / tinfo->row_size;
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
mono_verifier_is_corlib (MonoImage *image)
{
	gboolean trusted_location = !mono_security_core_clr_enabled () ?
			TRUE : mono_security_core_clr_is_platform_image (image);

	return trusted_location && image->module_name && !strcmp ("mscorlib.dll", image->module_name);
}

static gboolean
typedef_is_system_object (VerifyContext *ctx, guint32 *data)
{
	return mono_verifier_is_corlib (ctx->image) && !string_cmp (ctx, "System", data [MONO_TYPEDEF_NAMESPACE]) && !string_cmp (ctx, "Object", data [MONO_TYPEDEF_NAME]);
}

static gboolean
decode_value (const char *_ptr, unsigned available, unsigned *value, unsigned *size)
{
	unsigned char b;
	const unsigned char *ptr = (const unsigned char *)_ptr;

	if (!available)
		return FALSE;

	b = *ptr;
	*value = *size = 0;
	
	if ((b & 0x80) == 0) {
		*size = 1;
		*value = b;
	} else if ((b & 0x40) == 0) {
		if (available < 2)
			return FALSE;
		*size = 2;
		*value = ((b & 0x3f) << 8 | ptr [1]);
	} else {
		if (available < 4)
			return FALSE;
		*size = 4;
		*value  = ((b & 0x1f) << 24) |
			(ptr [1] << 16) |
			(ptr [2] << 8) |
			ptr [3];
	}

	return TRUE;
}

static gboolean
decode_signature_header (VerifyContext *ctx, guint32 offset, guint32 *size, const char **first_byte)
{
	MonoStreamHeader blob = ctx->image->heap_blob;
	guint32 value, enc_size;

	if (offset >= blob.size)
		return FALSE;

	if (!decode_value (blob.data + offset, blob.size - offset, &value, &enc_size))
		return FALSE;

	if (CHECK_ADD4_OVERFLOW_UN (offset, enc_size))
		return FALSE;

	offset += enc_size;

	if (ADD_IS_GREATER_OR_OVF (offset, value, blob.size))
		return FALSE;

	*size = value;
	*first_byte = blob.data + offset;
	return TRUE;
}

static gboolean
safe_read (const char **_ptr, const char *limit, unsigned *dest, int size)
{
	const char *ptr = *_ptr;
	if (ptr + size > limit)
		return FALSE;
	switch (size) {
	case 1:
		*dest = *((guint8*)ptr);
		++ptr;
		break;
	case 2:
		*dest = read16 (ptr);
		ptr += 2;
		break;
	case 4:
		*dest = read32 (ptr);
		ptr += 4;
		break;
	}
	*_ptr = ptr;
	return TRUE;
}

static gboolean
safe_read_compressed_int (const char **_ptr, const char *limit, unsigned *dest)
{
	unsigned size = 0;
	const char *ptr = *_ptr;
	gboolean res = decode_value (ptr, limit - ptr, dest, &size);
	*_ptr = ptr + size;
	return res;
}

#define safe_read8(VAR, PTR, LIMIT) safe_read (&PTR, LIMIT, &VAR, 1)
#define safe_read_cint(VAR, PTR, LIMIT) safe_read_compressed_int (&PTR, LIMIT, &VAR)
#define safe_read16(VAR, PTR, LIMIT) safe_read (&PTR, LIMIT, &VAR, 2)
#define safe_read32(VAR, PTR, LIMIT) safe_read (&PTR, LIMIT, &VAR, 4)

static gboolean
parse_type (VerifyContext *ctx, const char **_ptr, const char *end);

static gboolean
parse_method_signature (VerifyContext *ctx, const char **_ptr, const char *end, gboolean allow_sentinel, gboolean allow_unmanaged);

static gboolean
parse_custom_mods (VerifyContext *ctx, const char **_ptr, const char *end)
{
	const char *ptr = *_ptr;
	unsigned type = 0;
	unsigned token = 0;

	while (TRUE) {
		if (!safe_read8 (type, ptr, end))
			FAIL (ctx, g_strdup ("CustomMod: Not enough room for the type"));
	
		if (type != MONO_TYPE_CMOD_REQD && type != MONO_TYPE_CMOD_OPT) {
			--ptr;
			break;
		}
	
		if (!safe_read_cint (token, ptr, end))
			FAIL (ctx, g_strdup ("CustomMod: Not enough room for the token"));
	
		if (!is_valid_coded_index (ctx, TYPEDEF_OR_REF_DESC, token) || !get_coded_index_token (TYPEDEF_OR_REF_DESC, token))
			FAIL (ctx, g_strdup_printf ("CustomMod: invalid TypeDefOrRef token %x", token));
	}

	*_ptr = ptr;
	return TRUE;
}

static gboolean
parse_array_shape (VerifyContext *ctx, const char **_ptr, const char *end)
{
	const char *ptr = *_ptr;
	unsigned val = 0;
	unsigned size, num, i;

	if (!safe_read8 (val, ptr, end))
		FAIL (ctx, g_strdup ("ArrayShape: Not enough room for Rank"));

	if (val == 0)
		FAIL (ctx, g_strdup ("ArrayShape: Invalid shape with zero Rank"));

	if (!safe_read_cint (size, ptr, end))
		FAIL (ctx, g_strdup ("ArrayShape: Not enough room for NumSizes"));

	for (i = 0; i < size; ++i) {
		if (!safe_read_cint (num, ptr, end))
			FAIL (ctx, g_strdup_printf ("ArrayShape: Not enough room for Size of rank %d", i + 1));
	}

	if (!safe_read_cint (size, ptr, end))
		FAIL (ctx, g_strdup ("ArrayShape: Not enough room for NumLoBounds"));

	for (i = 0; i < size; ++i) {
		if (!safe_read_cint (num, ptr, end))
			FAIL (ctx, g_strdup_printf ("ArrayShape: Not enough room for LoBound of rank %d", i + 1));
	}

	*_ptr = ptr;
	return TRUE;
}

static gboolean
parse_generic_inst (VerifyContext *ctx, const char **_ptr, const char *end)
{
	const char *ptr = *_ptr;
	unsigned type;
	unsigned count, token, i;

	if (!safe_read8 (type, ptr, end))
		FAIL (ctx, g_strdup ("GenericInst: Not enough room for kind"));

	if (type != MONO_TYPE_CLASS && type != MONO_TYPE_VALUETYPE)
		FAIL (ctx, g_strdup_printf ("GenericInst: Invalid GenericInst kind %x\n", type));

	if (!safe_read_cint (token, ptr, end))
		FAIL (ctx, g_strdup ("GenericInst: Not enough room for type token"));

	if (!is_valid_coded_index (ctx, TYPEDEF_OR_REF_DESC, token) || !get_coded_index_token (TYPEDEF_OR_REF_DESC, token))
		FAIL (ctx, g_strdup_printf ("GenericInst: invalid TypeDefOrRef token %x", token));

	if (ctx->token) {
		if (mono_metadata_token_index (ctx->token) == get_coded_index_token (TYPEDEF_OR_REF_DESC, token) &&
			mono_metadata_token_table (ctx->token) == get_coded_index_table (TYPEDEF_OR_REF_DESC, token))
			FAIL (ctx, g_strdup_printf ("Type: Recurside generic instance specification (%x). A type signature can't reference itself", ctx->token));
	}

	if (!safe_read_cint (count, ptr, end))
		FAIL (ctx, g_strdup ("GenericInst: Not enough room for argument count"));

	if (count == 0)
		FAIL (ctx, g_strdup ("GenericInst: Zero arguments generic instance"));

	for (i = 0; i < count; ++i) {
		if (!parse_custom_mods (ctx, &ptr, end))
			FAIL (ctx, g_strdup ("Type: Failed to parse pointer custom attr"));

		if (!parse_type (ctx, &ptr, end))
			FAIL (ctx, g_strdup_printf ("GenericInst: invalid generic argument %d", i + 1));
	}
	*_ptr = ptr;
	return TRUE;
}

static gboolean
parse_type (VerifyContext *ctx, const char **_ptr, const char *end)
{
	const char *ptr = *_ptr;
	unsigned type;
	unsigned token = 0;

	if (!safe_read8 (type, ptr, end))
		FAIL (ctx, g_strdup ("Type: Not enough room for the type"));

	if (!((type >= MONO_TYPE_BOOLEAN && type <= MONO_TYPE_PTR) ||
		(type >= MONO_TYPE_VALUETYPE && type <= MONO_TYPE_GENERICINST) ||
		(type >= MONO_TYPE_I && type <= MONO_TYPE_U) ||
		(type >= MONO_TYPE_FNPTR && type <= MONO_TYPE_MVAR)))
		FAIL (ctx, g_strdup_printf ("Type: Invalid type kind %x\n", type));

	switch (type) {
	case MONO_TYPE_PTR:
		if (!parse_custom_mods (ctx, &ptr, end))
			FAIL (ctx, g_strdup ("Type: Failed to parse pointer custom attr"));

		if (!safe_read8 (type, ptr, end))
			FAIL (ctx, g_strdup ("Type: Not enough room to parse the pointer type"));

		if (type != MONO_TYPE_VOID) {
			--ptr;
			if (!parse_type (ctx, &ptr, end))
				FAIL (ctx, g_strdup ("Type: Could not parse pointer type"));
		}
		break;

	case MONO_TYPE_VALUETYPE:
	case MONO_TYPE_CLASS:
		if (!safe_read_cint (token, ptr, end))
			FAIL (ctx, g_strdup ("Type: Not enough room for the type token"));
	
		if (!is_valid_coded_index (ctx, TYPEDEF_OR_REF_DESC, token) || !get_coded_index_token (TYPEDEF_OR_REF_DESC, token))
			FAIL (ctx, g_strdup_printf ("Type: invalid TypeDefOrRef token %x", token));

		if (!get_coded_index_token (TYPEDEF_OR_REF_DESC, token))
			FAIL (ctx, g_strdup_printf ("Type: zero TypeDefOrRef token %x", token));
		if (ctx->token) {
			if (mono_metadata_token_index (ctx->token) == get_coded_index_token (TYPEDEF_OR_REF_DESC, token) &&
				mono_metadata_token_table (ctx->token) == get_coded_index_table (TYPEDEF_OR_REF_DESC, token))
				FAIL (ctx, g_strdup_printf ("Type: Recursive type specification (%x). A type signature can't reference itself", ctx->token));
		}
		break;

	case MONO_TYPE_VAR:
	case MONO_TYPE_MVAR:
		if (!safe_read_cint (token, ptr, end))
			FAIL (ctx, g_strdup ("Type: Not enough room for to decode generic argument number"));
		break;

	case MONO_TYPE_ARRAY:
		if (!parse_type (ctx, &ptr, end))
			FAIL (ctx, g_strdup ("Type: Could not parse array type"));
		if (!parse_array_shape (ctx, &ptr, end))
			FAIL (ctx, g_strdup ("Type: Could not parse array shape"));
		break;

	case MONO_TYPE_GENERICINST:
		if (!parse_generic_inst (ctx, &ptr, end))
			FAIL (ctx, g_strdup ("Type: Could not parse generic inst"));
		break;

	case MONO_TYPE_FNPTR:
		if (!parse_method_signature (ctx, &ptr, end, TRUE, TRUE))
			FAIL (ctx, g_strdup ("Type: Could not parse method pointer signature"));
		break;

	case MONO_TYPE_SZARRAY:
		if (!parse_custom_mods (ctx, &ptr, end))
			FAIL (ctx, g_strdup ("Type: Failed to parse array element custom attr"));
		if (!parse_type (ctx, &ptr, end))
			FAIL (ctx, g_strdup ("Type: Could not parse array type"));
		break;
	}
	*_ptr = ptr;
	return TRUE;
}

static gboolean
parse_return_type (VerifyContext *ctx, const char **_ptr, const char *end)
{
	const char *ptr;
	unsigned type = 0;

	if (!parse_custom_mods (ctx, _ptr, end))
		return FALSE;

	ptr = *_ptr;
	if (!safe_read8 (type, ptr, end))
		FAIL (ctx, g_strdup ("ReturnType: Not enough room for the type"));

	if (type == MONO_TYPE_VOID || type == MONO_TYPE_TYPEDBYREF) {
		*_ptr = ptr;
		return TRUE;
	}

	//it's a byref, update the cursor ptr
	if (type == MONO_TYPE_BYREF)
		*_ptr = ptr;

	return parse_type (ctx, _ptr, end);
}

static gboolean
parse_param (VerifyContext *ctx, const char **_ptr, const char *end)
{
	const char *ptr;
	unsigned type = 0;

	if (!parse_custom_mods (ctx, _ptr, end))
		return FALSE;

	ptr = *_ptr;
	if (!safe_read8 (type, ptr, end))
		FAIL (ctx, g_strdup ("Param: Not enough room for the type"));

	if (type == MONO_TYPE_TYPEDBYREF) {
		*_ptr = ptr;
		return TRUE;
	}

	//it's a byref, update the cursor ptr
	if (type == MONO_TYPE_BYREF) {
		*_ptr = ptr;
		if (!parse_custom_mods (ctx, _ptr, end))
			return FALSE;
	}

	return parse_type (ctx, _ptr, end);
}

static gboolean
parse_method_signature (VerifyContext *ctx, const char **_ptr, const char *end, gboolean allow_sentinel, gboolean allow_unmanaged)
{
	unsigned cconv = 0;
	unsigned param_count = 0, gparam_count = 0, type = 0, i;
	const char *ptr = *_ptr;
	gboolean saw_sentinel = FALSE;

	if (!safe_read8 (cconv, ptr, end))
		FAIL (ctx, g_strdup ("MethodSig: Not enough room for the call conv"));

	if (cconv & 0x80)
		FAIL (ctx, g_strdup ("MethodSig: CallConv has 0x80 set"));

	if (allow_unmanaged) {
		if ((cconv & 0x0F) > MONO_CALL_VARARG)
			FAIL (ctx, g_strdup_printf ("MethodSig: CallConv is not valid, it's %x", cconv & 0x0F));
	} else if ((cconv & 0x0F) != MONO_CALL_DEFAULT && (cconv & 0x0F) != MONO_CALL_VARARG)
		FAIL (ctx, g_strdup_printf ("MethodSig: CallConv is not Default or Vararg, it's %x", cconv & 0x0F));

	if ((cconv & 0x10) && !safe_read_cint (gparam_count, ptr, end))
		FAIL (ctx, g_strdup ("MethodSig: Not enough room for the generic param count"));

	if ((cconv & 0x10) && gparam_count == 0)
		FAIL (ctx, g_strdup ("MethodSig: Signature with generics but zero arity"));

	if (allow_unmanaged && (cconv & 0x10))
		FAIL (ctx, g_strdup ("MethodSig: Standalone signature with generic params"));

	if (!safe_read_cint (param_count, ptr, end))
		FAIL (ctx, g_strdup ("MethodSig: Not enough room for the param count"));

	if (!parse_return_type (ctx, &ptr, end))
		FAIL (ctx, g_strdup ("MethodSig: Error parsing return type"));

	for (i = 0; i < param_count; ++i) {
		if (allow_sentinel) {
			if (!safe_read8 (type, ptr, end))
				FAIL (ctx, g_strdup_printf ("MethodSig: Not enough room for param %d type", i));

			if (type == MONO_TYPE_SENTINEL) {
				if ((cconv & 0x0F) != MONO_CALL_VARARG)
					FAIL (ctx, g_strdup ("MethodSig: Found sentinel but signature is not vararg"));

				if (saw_sentinel)
					FAIL (ctx, g_strdup ("MethodSig: More than one sentinel type"));

				saw_sentinel = TRUE;
			} else {
				--ptr;
			}
		}

		if (!parse_param (ctx, &ptr, end))
			FAIL (ctx, g_strdup_printf ("MethodSig: Error parsing arg %d", i));
	}

	*_ptr = ptr;
	return TRUE;
}

static gboolean
parse_property_signature (VerifyContext *ctx, const char **_ptr, const char *end)
{
	unsigned type = 0;
	unsigned sig = 0;
	unsigned param_count = 0, i;
	const char *ptr = *_ptr;

	if (!safe_read8 (sig, ptr, end))
		FAIL (ctx, g_strdup ("PropertySig: Not enough room for signature"));

	if (sig != 0x08 && sig != 0x28)
		FAIL (ctx, g_strdup_printf ("PropertySig: Signature is not 0x28 or 0x08: %x", sig));

	if (!safe_read_cint (param_count, ptr, end))
		FAIL (ctx, g_strdup ("PropertySig: Not enough room for the param count"));

	if (!parse_custom_mods (ctx, &ptr, end))
		return FALSE;

	if (!safe_read8 (type, ptr, end))
		FAIL (ctx, g_strdup ("PropertySig: Not enough room for the type"));

	//check if it's a byref. safe_read8 did update ptr, so we rollback if it's not a byref
	if (type != MONO_TYPE_BYREF)
		--ptr;

	if (!parse_type (ctx, &ptr, end))
		FAIL (ctx, g_strdup ("PropertySig: Could not parse property type"));

	for (i = 0; i < param_count; ++i) {
		if (!parse_custom_mods (ctx, &ptr, end))
			FAIL (ctx, g_strdup ("Type: Failed to parse pointer custom attr"));
		if (!parse_type (ctx, &ptr, end))
			FAIL (ctx, g_strdup_printf ("PropertySig: Error parsing arg %d", i));
	}

	*_ptr = ptr;
	return TRUE;
}

static gboolean
parse_field (VerifyContext *ctx, const char **_ptr, const char *end)
{
	const char *ptr = *_ptr;
	unsigned signature = 0;

	if (!safe_read8 (signature, ptr, end))
		FAIL (ctx, g_strdup ("Field: Not enough room for field signature"));

	if (signature != 0x06)
		FAIL (ctx, g_strdup_printf ("Field: Invalid signature 0x%x, must be 6", signature));

	if (!parse_custom_mods (ctx, &ptr, end))
		return FALSE;

	if (safe_read8 (signature, ptr, end)) {
		if (signature != MONO_TYPE_BYREF)
			--ptr;
	}
	*_ptr = ptr;

	return parse_type (ctx, _ptr, end);
}

static gboolean
parse_locals_signature (VerifyContext *ctx, const char **_ptr, const char *end)
{
	unsigned sig = 0;
	unsigned locals_count = 0, i;
	const char *ptr = *_ptr;	

	if (!safe_read8 (sig, ptr, end))
		FAIL (ctx, g_strdup ("LocalsSig: Not enough room for signature"));

	if (sig != 0x07)
		FAIL (ctx, g_strdup_printf ("LocalsSig: Signature is not 0x28 or 0x08: %x", sig));

	if (!safe_read_cint (locals_count, ptr, end))
		FAIL (ctx, g_strdup ("LocalsSig: Not enough room for the param count"));

	/* LAMEIMPL: MS sometimes generates empty local signatures and its verifier is ok with.
	if (locals_count == 0)
		FAIL (ctx, g_strdup ("LocalsSig: Signature with zero locals"));
	*/

	for (i = 0; i < locals_count; ++i) {
		if (!safe_read8 (sig, ptr, end))
			FAIL (ctx, g_strdup ("LocalsSig: Not enough room for type"));

		while (sig == MONO_TYPE_CMOD_REQD || sig == MONO_TYPE_CMOD_OPT || sig == MONO_TYPE_PINNED) {
			if (sig != MONO_TYPE_PINNED && !parse_custom_mods (ctx, &ptr, end))
				FAIL (ctx, g_strdup_printf ("LocalsSig: Error parsing local %d", i));
			if (!safe_read8 (sig, ptr, end))
				FAIL (ctx, g_strdup ("LocalsSig: Not enough room for type"));
		}

		if (sig == MONO_TYPE_BYREF) {
			if (!safe_read8 (sig, ptr, end))
				FAIL (ctx, g_strdup_printf ("Type: Not enough room for byref type for local %d", i));
			if (sig == MONO_TYPE_TYPEDBYREF)
				FAIL (ctx, g_strdup_printf ("Type: Invalid type typedref& for local %d", i));
		}

		if (sig == MONO_TYPE_TYPEDBYREF)
			continue;

		--ptr;

		if (!parse_type (ctx, &ptr, end))
			FAIL (ctx, g_strdup_printf ("LocalsSig: Error parsing local %d", i));
	}

	*_ptr = ptr;
	return TRUE;
}

static gboolean
is_valid_field_signature (VerifyContext *ctx, guint32 offset)
{
	guint32 size = 0;
	unsigned signature = 0;
	const char *ptr = NULL, *end;

	if (!decode_signature_header (ctx, offset, &size, &ptr))
		FAIL (ctx, g_strdup ("FieldSig: Could not decode signature header"));
	end = ptr + size;

	if (!safe_read8 (signature, ptr, end))
		FAIL (ctx, g_strdup ("FieldSig: Not enough room for the signature"));

	if (signature != 6)
		FAIL (ctx, g_strdup_printf ("FieldSig: Invalid signature %x", signature));
	--ptr;

	return parse_field (ctx, &ptr, end);
}

static gboolean
is_valid_method_signature (VerifyContext *ctx, guint32 offset)
{
	guint32 size = 0;
	const char *ptr = NULL, *end;

	if (!decode_signature_header (ctx, offset, &size, &ptr))
		FAIL (ctx, g_strdup ("MethodSig: Could not decode signature header"));
	end = ptr + size;

	return parse_method_signature (ctx, &ptr, end, FALSE, FALSE);
}

static gboolean
is_valid_memberref_method_signature (VerifyContext *ctx, guint32 offset)
{
	guint32 size = 0;
	const char *ptr = NULL, *end;

	if (!decode_signature_header (ctx, offset, &size, &ptr))
		FAIL (ctx, g_strdup ("MemberRefSig: Could not decode signature header"));
	end = ptr + size;

	return parse_method_signature (ctx, &ptr, end, TRUE, FALSE);
}


static gboolean
is_valid_method_or_field_signature (VerifyContext *ctx, guint32 offset)
{
	guint32 size = 0;
	unsigned signature = 0;
	const char *ptr = NULL, *end;

	if (!decode_signature_header (ctx, offset, &size, &ptr))
		FAIL (ctx, g_strdup ("MemberRefSig: Could not decode signature header"));
	end = ptr + size;

	if (!safe_read8 (signature, ptr, end))
		FAIL (ctx, g_strdup ("MemberRefSig: Not enough room for the call conv"));
	--ptr;

	if (signature == 0x06)
		return parse_field (ctx, &ptr, end);

	return parse_method_signature (ctx, &ptr, end, TRUE, FALSE);
}

static gboolean
is_valid_cattr_blob (VerifyContext *ctx, guint32 offset)
{
	guint32 size = 0;
	unsigned prolog = 0;
	const char *ptr = NULL, *end;

	if (!offset)
		return TRUE;

	if (!decode_signature_header (ctx, offset, &size, &ptr))
		FAIL (ctx, g_strdup ("CustomAttribute: Could not decode signature header"));
	end = ptr + size;

	if (!safe_read16 (prolog, ptr, end))
		FAIL (ctx, g_strdup ("CustomAttribute: Not enough room for prolog"));

	if (prolog != 1)
		FAIL (ctx, g_strdup_printf ("CustomAttribute: Prolog is 0x%x, expected 0x1", prolog));

	return TRUE;
}

static gboolean
is_valid_cattr_type (MonoType *type)
{
	MonoClass *klass;

	if (type->type == MONO_TYPE_OBJECT || (type->type >= MONO_TYPE_BOOLEAN && type->type <= MONO_TYPE_STRING))
		return TRUE;

	if (type->type == MONO_TYPE_VALUETYPE) {
		klass = mono_class_from_mono_type (type);
		return klass && klass->enumtype;
	}

	if (type->type == MONO_TYPE_CLASS)
		return mono_class_from_mono_type (type) == mono_defaults.systemtype_class;

	return FALSE;
}

static gboolean
is_valid_ser_string_full (VerifyContext *ctx, const char **str_start, guint32 *str_len, const char **_ptr, const char *end)
{
	guint32 size = 0;
	const char *ptr = *_ptr;

	*str_start = NULL;
	*str_len = 0;

	if (ptr >= end)
		FAIL (ctx, g_strdup ("CustomAttribute: Not enough room for string size"));

	/*NULL string*/
	if (*ptr == (char)0xFF) {
		*_ptr = ptr + 1;
		return TRUE;
	}

	if (!safe_read_cint (size, ptr, end))
		FAIL (ctx, g_strdup ("CustomAttribute: Not enough room for string size"));

	if (ADDP_IS_GREATER_OR_OVF (ptr, size, end))
		FAIL (ctx, g_strdup ("CustomAttribute: Not enough room for string"));

	*str_start = ptr;
	*str_len = size;

	*_ptr = ptr + size;
	return TRUE;
}

static gboolean
is_valid_ser_string (VerifyContext *ctx, const char **_ptr, const char *end)
{
	const char *dummy_str;
	guint32 dummy_int;
	return is_valid_ser_string_full (ctx, &dummy_str, &dummy_int, _ptr, end);
}

static MonoClass*
get_enum_by_encoded_name (VerifyContext *ctx, const char **_ptr, const char *end)
{
	MonoError error;
	MonoType *type;
	MonoClass *klass;
	const char *str_start = NULL;
	const char *ptr = *_ptr;
	char *enum_name;
	guint32 str_len = 0;

	if (!is_valid_ser_string_full (ctx, &str_start, &str_len, &ptr, end))
		return NULL;

	/*NULL or empty string*/
	if (str_start == NULL || str_len == 0) {
		ADD_ERROR_NO_RETURN (ctx, g_strdup ("CustomAttribute: Null or empty enum name"));
		return NULL;
	}

	enum_name = (char *)g_memdup (str_start, str_len + 1);
	enum_name [str_len] = 0;
	type = mono_reflection_type_from_name_checked (enum_name, ctx->image, &error);
	if (!type || !is_ok (&error)) {
		ADD_ERROR_NO_RETURN (ctx, g_strdup_printf ("CustomAttribute: Invalid enum class %s, due to %s", enum_name, mono_error_get_message (&error)));
		g_free (enum_name);
		mono_error_cleanup (&error);
		return NULL;
	}
	g_free (enum_name);

	klass = mono_class_from_mono_type (type);
	if (!klass || !klass->enumtype) {
		ADD_ERROR_NO_RETURN (ctx, g_strdup_printf ("CustomAttribute:Class %s::%s is not an enum", klass->name_space, klass->name));
		return NULL;
	}

	*_ptr = ptr;
	return klass;
}

static gboolean
is_valid_fixed_param (VerifyContext *ctx, MonoType *mono_type, const char **_ptr, const char *end)
{
	MonoClass *klass;
	const char *ptr = *_ptr;
	int elem_size = 0;
	guint32 element_count, i;
	int type;

	klass = mono_type->data.klass;
	type = mono_type->type;

handle_enum:
	switch (type) {
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
		elem_size = 1;
		break;
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_CHAR:
		elem_size = 2;
		break;
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
	case MONO_TYPE_R4:
		elem_size = 4;
		break;
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
	case MONO_TYPE_R8:
		elem_size = 8;
		break;

	case MONO_TYPE_STRING:
		*_ptr = ptr;
		return is_valid_ser_string (ctx, _ptr, end);

	case MONO_TYPE_OBJECT: {
		unsigned sub_type = 0;
		if (!safe_read8 (sub_type, ptr, end))
			FAIL (ctx, g_strdup ("CustomAttribute: Not enough room for array type"));

		if (sub_type >= MONO_TYPE_BOOLEAN && sub_type <= MONO_TYPE_STRING) {
			type = sub_type;
			goto handle_enum;
		}
		if (sub_type == MONO_TYPE_ENUM) {
			klass = get_enum_by_encoded_name (ctx, &ptr, end);
			if (!klass)
				return FALSE;

			klass = klass->element_class;
			type = klass->byval_arg.type;
			goto handle_enum;
		}
		if (sub_type == 0x50) { /*Type*/
			*_ptr = ptr;
			return is_valid_ser_string (ctx, _ptr, end);
		}
		if (sub_type == MONO_TYPE_SZARRAY) {
			MonoType simple_type = {{0}};
			unsigned etype = 0;
			if (!safe_read8 (etype, ptr, end))
				FAIL (ctx, g_strdup ("CustomAttribute: Not enough room for array element type"));

			if (etype == MONO_TYPE_ENUM) {
				klass = get_enum_by_encoded_name (ctx, &ptr, end);
				if (!klass)
					return FALSE;
			} else if (etype == 0x50 || etype == MONO_TYPE_CLASS) {
				klass = mono_defaults.systemtype_class;
			} else if ((etype >= MONO_TYPE_BOOLEAN && etype <= MONO_TYPE_STRING) || etype == 0x51) {
				simple_type.type = etype == 0x51 ? MONO_TYPE_OBJECT : (MonoTypeEnum)etype;
				klass = mono_class_from_mono_type (&simple_type);
			} else
				FAIL (ctx, g_strdup_printf ("CustomAttribute: Invalid array element type %x", etype));

			type = MONO_TYPE_SZARRAY;
			goto handle_enum;
		}
		FAIL (ctx, g_strdup_printf ("CustomAttribute: Invalid boxed object type %x", sub_type));
	}

	case MONO_TYPE_CLASS:
		if (klass && klass->enumtype) {
			klass = klass->element_class;
			type = klass->byval_arg.type;
			goto handle_enum;
		}

		if (klass != mono_defaults.systemtype_class)
			FAIL (ctx, g_strdup_printf ("CustomAttribute: Invalid class parameter type %s:%s ",klass->name_space, klass->name));
		*_ptr = ptr;
		return is_valid_ser_string (ctx, _ptr, end);

	case MONO_TYPE_VALUETYPE:
		if (!klass || !klass->enumtype)
			FAIL (ctx, g_strdup_printf ("CustomAttribute: Invalid valuetype parameter expected enum %s:%s ",klass->name_space, klass->name));

		klass = klass->element_class;
		type = klass->byval_arg.type;
		goto handle_enum;

	case MONO_TYPE_SZARRAY:
		mono_type = &klass->byval_arg;
		if (!is_valid_cattr_type (mono_type))
			FAIL (ctx, g_strdup_printf ("CustomAttribute: Invalid array element type %s:%s ",klass->name_space, klass->name));
		if (!safe_read32 (element_count, ptr, end))
			FAIL (ctx, g_strdup_printf ("CustomAttribute: Invalid class parameter type %s:%s ",klass->name_space, klass->name));
		if (element_count == 0xFFFFFFFFu) {
			*_ptr = ptr;
			return TRUE;
		}
		for (i = 0; i < element_count; ++i) {
			if (!is_valid_fixed_param (ctx, mono_type, &ptr, end))
				return FALSE;
		}
		*_ptr = ptr;
		return TRUE;
	default:
		FAIL (ctx, g_strdup_printf ("CustomAttribute: Invalid parameter type %x ", type));
	}

	if (ADDP_IS_GREATER_OR_OVF (ptr, elem_size, end))
		FAIL (ctx, g_strdup ("CustomAttribute: Not enough space for element"));
	*_ptr = ptr + elem_size;
	return TRUE;
}

static gboolean
is_valid_cattr_content (VerifyContext *ctx, MonoMethod *ctor, const char *ptr, guint32 size)
{
	MonoError error;
	unsigned prolog = 0;
	const char *end;
	MonoMethodSignature *sig;
	int args, i;
	unsigned num_named;

	if (!ctor)
		FAIL (ctx, g_strdup ("CustomAttribute: Invalid constructor"));

	sig = mono_method_signature_checked (ctor, &error);
	if (!mono_error_ok (&error)) {
		ADD_ERROR_NO_RETURN (ctx, g_strdup_printf ("CustomAttribute: Invalid constructor signature %s", mono_error_get_message (&error)));
		mono_error_cleanup (&error);
		return FALSE;
	}

	if (sig->sentinelpos != -1 || sig->call_convention == MONO_CALL_VARARG)
		FAIL (ctx, g_strdup ("CustomAttribute: Constructor cannot have VARAG signature"));

	end = ptr + size;

	if (!safe_read16 (prolog, ptr, end))
		FAIL (ctx, g_strdup ("CustomAttribute: Not enough room for prolog"));

	if (prolog != 1)
		FAIL (ctx, g_strdup_printf ("CustomAttribute: Prolog is 0x%x, expected 0x1", prolog));

	args = sig->param_count;
	for (i = 0; i < args; ++i) {
		MonoType *arg_type = sig->params [i];
		if (!is_valid_fixed_param (ctx, arg_type, &ptr, end))
			return FALSE;
	}

	if (!safe_read16 (num_named, ptr, end))
		FAIL (ctx, g_strdup ("CustomAttribute: Not enough space for num_named field"));

	for (i = 0; i < num_named; ++i) {
		MonoType *type, simple_type = {{0}};
		unsigned kind;

		if (!safe_read8 (kind, ptr, end))
			FAIL (ctx, g_strdup_printf ("CustomAttribute: Not enough space for named parameter %d kind", i));
		if (kind != 0x53 && kind != 0x54)
			FAIL (ctx, g_strdup_printf ("CustomAttribute: Invalid named parameter %d kind %x", i, kind));
		if (!safe_read8 (kind, ptr, end))
			FAIL (ctx, g_strdup_printf ("CustomAttribute: Not enough space for named parameter %d type", i));

		if (kind >= MONO_TYPE_BOOLEAN && kind <= MONO_TYPE_STRING) {
			simple_type.type = (MonoTypeEnum)kind;
			type = &simple_type;
		} else if (kind == MONO_TYPE_ENUM) {
			MonoClass *klass = get_enum_by_encoded_name (ctx, &ptr, end);
			if (!klass)
				return FALSE;
			type = &klass->byval_arg;
		} else if (kind == 0x50) {
			type = &mono_defaults.systemtype_class->byval_arg;
		} else if (kind == 0x51) {
			type = &mono_defaults.object_class->byval_arg;
		} else if (kind == MONO_TYPE_SZARRAY) {
			MonoClass *klass;
			unsigned etype = 0;
			if (!safe_read8 (etype, ptr, end))
				FAIL (ctx, g_strdup ("CustomAttribute: Not enough room for array element type"));

			if (etype == MONO_TYPE_ENUM) {
				klass = get_enum_by_encoded_name (ctx, &ptr, end);
				if (!klass)
					return FALSE;
			} else if (etype == 0x50 || etype == MONO_TYPE_CLASS) {
				klass = mono_defaults.systemtype_class;
			} else if ((etype >= MONO_TYPE_BOOLEAN && etype <= MONO_TYPE_STRING) || etype == 0x51) {
				simple_type.type = etype == 0x51 ? MONO_TYPE_OBJECT : (MonoTypeEnum)etype;
				klass = mono_class_from_mono_type (&simple_type);
			} else
				FAIL (ctx, g_strdup_printf ("CustomAttribute: Invalid array element type %x", etype));

			type = &mono_array_class_get (klass, 1)->byval_arg;
		} else {
			FAIL (ctx, g_strdup_printf ("CustomAttribute: Invalid named parameter type %x", kind));
		}

		if (!is_valid_ser_string (ctx, &ptr, end))
			return FALSE;

		if (!is_valid_fixed_param (ctx, type, &ptr, end))
			return FALSE;

	}

	return TRUE;
}

static gboolean
is_valid_marshal_spec (VerifyContext *ctx, guint32 offset)
{
	OffsetAndSize blob = get_metadata_stream (ctx, &ctx->image->heap_blob);
	//TODO do proper verification
	return blob.size >= 1 && blob.size - 1 >= offset;
}

static gboolean
is_valid_permission_set (VerifyContext *ctx, guint32 offset)
{
	OffsetAndSize blob = get_metadata_stream (ctx, &ctx->image->heap_blob);
	//TODO do proper verification
	return blob.size >= 1 && blob.size - 1 >= offset;
}

static gboolean
is_valid_standalonesig_blob (VerifyContext *ctx, guint32 offset)
{
	guint32 size = 0;
	unsigned signature = 0;
	const char *ptr = NULL, *end;

	if (!decode_signature_header (ctx, offset, &size, &ptr))
		FAIL (ctx, g_strdup ("StandAloneSig: Could not decode signature header"));
	end = ptr + size;

	if (!safe_read8 (signature, ptr, end))
		FAIL (ctx, g_strdup ("StandAloneSig: Not enough room for the call conv"));

	--ptr;
	if (signature == 0x07)
		return parse_locals_signature (ctx, &ptr, end);

	/*F# and managed C++ produce standalonesig for fields even thou the spec doesn't mention it.*/
	if (signature == 0x06)
		return parse_field (ctx, &ptr, end);

	return parse_method_signature (ctx, &ptr, end, TRUE, TRUE);
}

static gboolean
is_valid_property_sig_blob (VerifyContext *ctx, guint32 offset)
{
	guint32 size = 0;
	const char *ptr = NULL, *end;

	if (!decode_signature_header (ctx, offset, &size, &ptr))
		FAIL (ctx, g_strdup ("PropertySig: Could not decode signature header"));
	end = ptr + size;

	return parse_property_signature (ctx, &ptr, end);
}

static gboolean
is_valid_typespec_blob (VerifyContext *ctx, guint32 offset)
{
	guint32 size = 0;
	const char *ptr = NULL, *end;
	unsigned type = 0;
	
	if (!decode_signature_header (ctx, offset, &size, &ptr))
		FAIL (ctx, g_strdup ("TypeSpec: Could not decode signature header"));
	end = ptr + size;

	if (!parse_custom_mods (ctx, &ptr, end))
		return FALSE;

	if (!safe_read8 (type, ptr, end))
		FAIL (ctx, g_strdup ("TypeSpec: Not enough room for type"));

	if (type == MONO_TYPE_BYREF) {
		if (!safe_read8 (type, ptr, end)) 
			FAIL (ctx, g_strdup ("TypeSpec: Not enough room for byref type"));
		if (type == MONO_TYPE_TYPEDBYREF)
			FAIL (ctx, g_strdup ("TypeSpec: Invalid type typedref&"));
	}
	
	if (type == MONO_TYPE_TYPEDBYREF)
		return TRUE;

	--ptr;
	return parse_type (ctx, &ptr, end);
}

static gboolean
is_valid_methodspec_blob (VerifyContext *ctx, guint32 offset)
{
	guint32 size = 0;
	const char *ptr = NULL, *end;
	unsigned type = 0;
	unsigned count = 0, i;

	if (!decode_signature_header (ctx, offset, &size, &ptr))
		FAIL (ctx, g_strdup ("MethodSpec: Could not decode signature header"));
	end = ptr + size;

	if (!safe_read8 (type, ptr, end))
		FAIL (ctx, g_strdup ("MethodSpec: Not enough room for call convention"));

	if (type != 0x0A)
		FAIL (ctx, g_strdup_printf ("MethodSpec: Invalid call convention 0x%x, expected 0x0A", type));

	if (!safe_read_cint (count, ptr, end))
		FAIL (ctx, g_strdup ("MethodSpec: Not enough room for parameter count"));

	if (!count)
		FAIL (ctx, g_strdup ("MethodSpec: Zero generic argument count"));

	for (i = 0; i < count; ++i) {
		if (!parse_custom_mods (ctx, &ptr, end))
			return FALSE;
		if (!parse_type (ctx, &ptr, end))
			FAIL (ctx, g_strdup_printf ("MethodSpec: Could not parse parameter %d", i + 1));
	}
	return TRUE;
}

static gboolean
is_valid_blob_object (VerifyContext *ctx, guint32 offset, guint32 minsize)
{
	OffsetAndSize blob = get_metadata_stream (ctx, &ctx->image->heap_blob);
	guint32 entry_size, bytes;

	if (blob.size < offset)
		return FALSE;

	if (!decode_value (ctx->data + offset + blob.offset, blob.size - blob.offset, &entry_size, &bytes))
		return FALSE;

	if (entry_size < minsize)
		return FALSE;

	if (CHECK_ADD4_OVERFLOW_UN (entry_size, bytes))
		return FALSE;
	entry_size += bytes;

	return !ADD_IS_GREATER_OR_OVF (offset, entry_size, blob.size);
}

static gboolean
is_valid_constant (VerifyContext *ctx, guint32 type, guint32 offset)
{
	OffsetAndSize blob = get_metadata_stream (ctx, &ctx->image->heap_blob);
	guint32 size, entry_size, bytes;

	if (blob.size < offset)
		FAIL (ctx, g_strdup ("ContantValue: invalid offset"));
	
	if (!decode_value (ctx->data + offset + blob.offset, blob.size - blob.offset, &entry_size, &bytes))
		FAIL (ctx, g_strdup ("ContantValue: not enough space to decode size"));

	if (type == MONO_TYPE_STRING) {
		//String is encoded as: compressed_int:len len *bytes
		offset += bytes;

		if (ADD_IS_GREATER_OR_OVF (offset, entry_size, blob.size))
			FAIL (ctx, g_strdup_printf ("ContantValue: not enough space for string, required %d but got %d", entry_size * 2, blob.size - offset));	

		return TRUE;
	}

	switch (type) {
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
		size = 1;
		break;
	case MONO_TYPE_CHAR:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
		size = 2;
		break;
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
	case MONO_TYPE_R4:
	case MONO_TYPE_CLASS:
		size = 4;
		break;

	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
	case MONO_TYPE_R8:
		size = 8;
		break;
	default:
		g_assert_not_reached ();
	}

	if (size != entry_size)
		FAIL (ctx, g_strdup_printf ("ContantValue: Expected size %d but got %d", size, entry_size));

	offset += bytes;

	if (ADD_IS_GREATER_OR_OVF (offset, size, blob.size))
		FAIL (ctx, g_strdup_printf ("ContantValue: Not enough room for constant, required %d but have %d", size, blob.size - offset));

	if (type == MONO_TYPE_CLASS && read32 (ctx->data + blob.offset + offset))
		FAIL (ctx, g_strdup_printf ("ContantValue: Type is class but value is not null"));
	return TRUE;
}

#define FAT_HEADER_INVALID_FLAGS ~(0x3 | 0x8 | 0x10 | 0xF000)
//only 0x01, 0x40 and 0x80 are allowed
#define SECTION_HEADER_INVALID_FLAGS 0x3E

static gboolean
is_valid_method_header (VerifyContext *ctx, guint32 rva, guint32 *locals_token)
{
	unsigned local_vars_tok, code_size, offset = mono_cli_rva_image_map (ctx->image, rva);
	unsigned header = 0;
	unsigned fat_header = 0, size = 0, max_stack;
	const char *ptr = NULL, *end;

	*locals_token = 0;

	if (offset == INVALID_ADDRESS)
		FAIL (ctx, g_strdup ("MethodHeader: Invalid RVA"));

	ptr = ctx->data + offset;
	end = ctx->data + ctx->size; /*no worries if it spawns multiple sections*/

	if (!safe_read8 (header, ptr, end))
		FAIL (ctx, g_strdup ("MethodHeader: Not enough room for header"));

	switch (header & 0x3) {
	case 0:
	case 1:
		FAIL (ctx, g_strdup_printf ("MethodHeader: Invalid header type 0x%x", header & 0x3));
	case 2:
		header >>= 2;
		if (ADDP_IS_GREATER_OR_OVF (ptr, header, end)) 
			FAIL (ctx, g_strdup_printf ("MethodHeader: Not enough room for method body. Required %d, but only %d is available", header, (int)(end - ptr)));
		return TRUE;
	}
	//FAT HEADER
	--ptr;
	if (!safe_read16 (fat_header, ptr, end))
		FAIL (ctx, g_strdup ("MethodHeader: Not enough room for fat header"));

	size = (fat_header >> 12) & 0xF;
	if (size != 3)
		FAIL (ctx, g_strdup ("MethodHeader: header size must be 3"));

	if (!safe_read16 (max_stack, ptr, end))
		FAIL (ctx, g_strdup ("MethodHeader: Not enough room for max stack"));

	if (!safe_read32 (code_size, ptr, end))
		FAIL (ctx, g_strdup ("MethodHeader: Not enough room for code size"));

	if (!safe_read32 (local_vars_tok, ptr, end))
		FAIL (ctx, g_strdup ("MethodHeader: Not enough room for local vars tok"));

	if (local_vars_tok) {
		if (((local_vars_tok >> 24) & 0xFF) != 0x11)
			FAIL (ctx, g_strdup_printf ("MethodHeader: Invalid local vars signature table 0x%x", ((local_vars_tok >> 24) & 0xFF)));
		if ((local_vars_tok & 0xFFFFFF) > ctx->image->tables [MONO_TABLE_STANDALONESIG].rows)	
			FAIL (ctx, g_strdup_printf ("MethodHeader: Invalid local vars signature points to invalid row 0x%x", local_vars_tok & 0xFFFFFF));
		if (!(local_vars_tok & 0xFFFFFF))
			FAIL (ctx, g_strdup_printf ("MethodHeader: Invalid local vars signature with zero index"));
		*locals_token = local_vars_tok & 0xFFFFFF;
	}

	if (fat_header & FAT_HEADER_INVALID_FLAGS)
		FAIL (ctx, g_strdup_printf ("MethodHeader: Invalid fat signature flags %x", fat_header & FAT_HEADER_INVALID_FLAGS));

	if (ADDP_IS_GREATER_OR_OVF (ptr, code_size, end))
		FAIL (ctx, g_strdup_printf ("MethodHeader: Not enough room for code %d", code_size));

	if (!(fat_header & 0x08))
		return TRUE;

	ptr += code_size;

	do {
		unsigned section_header = 0, section_size = 0;
		gboolean is_fat;

		ptr = dword_align (ptr);
		if (!safe_read32 (section_header, ptr, end))
			FAIL (ctx, g_strdup ("MethodHeader: Not enough room for data section header"));

		if (section_header & SECTION_HEADER_INVALID_FLAGS)
			FAIL (ctx, g_strdup_printf ("MethodHeader: Invalid section header flags 0x%x", section_header & SECTION_HEADER_INVALID_FLAGS));
			
		is_fat = (section_header & METHOD_HEADER_SECTION_FAT_FORMAT) != 0;
		section_size = (section_header >> 8) & (is_fat ? 0xFFFFFF : 0xFF);

		if (section_size < 4)
			FAIL (ctx, g_strdup_printf ("MethodHeader: Section size too small"));

		if (ADDP_IS_GREATER_OR_OVF (ptr, section_size - 4, end)) /*must be section_size -4 as ptr was incremented by safe_read32*/
			FAIL (ctx, g_strdup_printf ("MethodHeader: Not enough room for section content %d", section_size));

		if (section_header & METHOD_HEADER_SECTION_EHTABLE) {
			guint32 i, clauses = section_size / (is_fat ? 24 : 12);
			/*
				LAMEIMPL: MS emits section_size without accounting for header size.
				Mono does as the spec says. section_size is header + section
				MS's peverify happily accepts both. 
			*/
			if ((clauses * (is_fat ? 24 : 12) != section_size) && (clauses * (is_fat ? 24 : 12) + 4 != section_size))
				FAIL (ctx, g_strdup_printf ("MethodHeader: Invalid EH section size %d, it's not of the expected size %d", section_size, clauses * (is_fat ? 24 : 12)));

			/* only verify the class token is verified as the rest is done by the IL verifier*/
			for (i = 0; i < clauses; ++i) {
				unsigned flags = *(unsigned char*)ptr;
				unsigned class_token = 0;
				ptr += (is_fat ? 20 : 8);
				if (!safe_read32 (class_token, ptr, end))
					FAIL (ctx, g_strdup_printf ("MethodHeader: Not enough room for section %d", i));
				if (flags == MONO_EXCEPTION_CLAUSE_NONE && class_token) {
					guint table = mono_metadata_token_table (class_token);
					if (table != MONO_TABLE_TYPEREF && table != MONO_TABLE_TYPEDEF && table != MONO_TABLE_TYPESPEC)
						FAIL (ctx, g_strdup_printf ("MethodHeader: Invalid section %d class token table %x", i, table));
					if (mono_metadata_token_index (class_token) > ctx->image->tables [table].rows)
						FAIL (ctx, g_strdup_printf ("MethodHeader: Invalid section %d class token index %x", i, mono_metadata_token_index (class_token)));
				}
			}
		}

		if (!(section_header & METHOD_HEADER_SECTION_MORE_SECTS))
			break;
	} while (1);
	return TRUE;
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
	MonoError error;
	guint32 i;

	for (i = 0; i < table->rows; ++i) {
		mono_verifier_verify_typeref_row (ctx->image, i, &error);
		add_from_mono_error (ctx, &error);
	}
}

/*bits 9,11,14,15,19,21,24-31 */
#define INVALID_TYPEDEF_FLAG_BITS ((1 << 6) | (1 << 9) | (1 << 15) | (1 << 19) | (1 << 21) | 0xFF000000)
static void
verify_typedef_table (VerifyContext *ctx)
{
	MonoTableInfo *table = &ctx->image->tables [MONO_TABLE_TYPEDEF];
	guint32 data [MONO_TYPEDEF_SIZE];
	guint32 fieldlist = 1, methodlist = 1, visibility;
	int i;

	if (table->rows == 0)
		ADD_ERROR (ctx, g_strdup_printf ("Typedef table must have exactly at least one row"));

	for (i = 0; i < table->rows; ++i) {
		mono_metadata_decode_row (table, i, data, MONO_TYPEDEF_SIZE);
		if (data [MONO_TYPEDEF_FLAGS] & INVALID_TYPEDEF_FLAG_BITS)
			ADD_ERROR (ctx, g_strdup_printf ("Invalid typedef row %d invalid flags field 0x%08x rejected bits: 0x%08x", i, data [MONO_TYPEDEF_FLAGS], data [MONO_TYPEDEF_FLAGS] & INVALID_TYPEDEF_FLAG_BITS));

		if ((data [MONO_TYPEDEF_FLAGS] & TYPE_ATTRIBUTE_LAYOUT_MASK) == 0x18)
			ADD_ERROR (ctx, g_strdup_printf ("Invalid typedef row %d invalid class layout 0x18", i));

		if ((data [MONO_TYPEDEF_FLAGS] & TYPE_ATTRIBUTE_STRING_FORMAT_MASK) == 0x30000)
			ADD_ERROR (ctx, g_strdup_printf ("Invalid typedef row %d mono doesn't support custom string format", i));

		if ((data [MONO_TYPEDEF_FLAGS] & 0xC00000) != 0)
			ADD_ERROR (ctx, g_strdup_printf ("Invalid typedef row %d mono doesn't support custom string format", i));

		if ((data [MONO_TYPEDEF_FLAGS] & TYPE_ATTRIBUTE_INTERFACE) && (data [MONO_TYPEDEF_FLAGS] & TYPE_ATTRIBUTE_ABSTRACT) == 0)
			ADD_ERROR (ctx, g_strdup_printf ("Invalid typedef row %d for interface type must be abstract", i));

		if (!data [MONO_TYPEDEF_NAME] || !is_valid_non_empty_string (ctx, data [MONO_TYPEDEF_NAME]))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid typedef row %d invalid name token %08x", i, data [MONO_TYPEDEF_NAME]));

		if (data [MONO_TYPEREF_NAMESPACE] && !is_valid_non_empty_string (ctx, data [MONO_TYPEREF_NAMESPACE]))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid typedef row %d invalid namespace token %08x", i, data [MONO_TYPEREF_NAMESPACE]));

		if (data [MONO_TYPEDEF_EXTENDS] && !is_valid_coded_index (ctx, TYPEDEF_OR_REF_DESC, data [MONO_TYPEDEF_EXTENDS]))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid typedef row %d extend field coded index 0x%08x", i, data [MONO_TYPEDEF_EXTENDS]));

		if (data [MONO_TYPEDEF_EXTENDS] && !get_coded_index_token (TYPEDEF_OR_REF_DESC, data [MONO_TYPEDEF_EXTENDS]))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid typedef row %d zero coded extend field coded index 0x%08x", i, data [MONO_TYPEDEF_EXTENDS]));

		visibility = data [MONO_TYPEDEF_FLAGS] & TYPE_ATTRIBUTE_VISIBILITY_MASK;
		if ((visibility >= TYPE_ATTRIBUTE_NESTED_PUBLIC && visibility <= TYPE_ATTRIBUTE_NESTED_FAM_OR_ASSEM) &&
			search_sorted_table (ctx, MONO_TABLE_NESTEDCLASS, MONO_NESTED_CLASS_NESTED, i + 1) == -1)
			ADD_ERROR (ctx, g_strdup_printf ("Invalid typedef row %d has nested visibility but no rows in the NestedClass table", i));

		if (data [MONO_TYPEDEF_FIELD_LIST] == 0)
			ADD_ERROR (ctx, g_strdup_printf ("Invalid typedef row %d FieldList be be >= 1", i));

		if (data [MONO_TYPEDEF_FIELD_LIST] > ctx->image->tables [MONO_TABLE_FIELD].rows + 1)
			ADD_ERROR (ctx, g_strdup_printf ("Invalid typedef row %d FieldList rowid 0x%08x is out of range", i, data [MONO_TYPEDEF_FIELD_LIST]));

		if (data [MONO_TYPEDEF_FIELD_LIST] < fieldlist)
			ADD_ERROR (ctx, g_strdup_printf ("Invalid typedef row %d FieldList rowid 0x%08x can't be smaller than of previous row 0x%08x", i, data [MONO_TYPEDEF_FIELD_LIST], fieldlist));

		if (data [MONO_TYPEDEF_METHOD_LIST] == 0)
			ADD_ERROR (ctx, g_strdup_printf ("Invalid typedef row %d MethodList be be >= 1", i));

		if (data [MONO_TYPEDEF_METHOD_LIST] > ctx->image->tables [MONO_TABLE_METHOD].rows + 1)
			ADD_ERROR (ctx, g_strdup_printf ("Invalid typedef row %d MethodList rowid 0x%08x is out of range", i, data [MONO_TYPEDEF_METHOD_LIST]));

		if (data [MONO_TYPEDEF_METHOD_LIST] < methodlist)
			ADD_ERROR (ctx, g_strdup_printf ("Invalid typedef row %d MethodList rowid 0x%08x can't be smaller than of previous row 0x%08x", i, data [MONO_TYPEDEF_METHOD_LIST], methodlist));

		fieldlist = data [MONO_TYPEDEF_FIELD_LIST];
		methodlist = data [MONO_TYPEDEF_METHOD_LIST];
	}
}

static void
verify_typedef_table_full (VerifyContext *ctx)
{
	MonoTableInfo *table = &ctx->image->tables [MONO_TABLE_TYPEDEF];
	guint32 data [MONO_TYPEDEF_SIZE];
	int i;

	if (table->rows == 0)
		ADD_ERROR (ctx, g_strdup_printf ("Typedef table must have exactly at least one row"));

	for (i = 0; i < table->rows; ++i) {
		mono_metadata_decode_row (table, i, data, MONO_TYPEDEF_SIZE);

		if (i == 0) {
			/*XXX it's ok if <module> extends object, or anything at all, actually. */
			/*if (data [MONO_TYPEDEF_EXTENDS] != 0)
				ADD_ERROR (ctx, g_strdup_printf ("Invalid typedef row 0 for the special <module> type must have a null extend field"));
			*/
			continue;
		}

		if (data [MONO_TYPEDEF_FLAGS] & TYPE_ATTRIBUTE_INTERFACE) {
			if (data [MONO_TYPEDEF_EXTENDS])
				ADD_ERROR (ctx, g_strdup_printf ("Invalid typedef row %d for interface type must have a null extend field", i));
		} else {
			gboolean is_sys_obj = typedef_is_system_object (ctx, data);
			gboolean has_parent = get_coded_index_token (TYPEDEF_OR_REF_DESC, data [MONO_TYPEDEF_EXTENDS]) != 0;

			if (is_sys_obj) {
				if (has_parent)
					ADD_ERROR (ctx, g_strdup_printf ("Invalid typedef row %d for System.Object must have a null extend field", i));
			} else {
				if (!has_parent) {
					ADD_ERROR (ctx, g_strdup_printf ("Invalid typedef row %d for non-interface type must have a non-null extend field", i));
				}
			}
		}
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

		if (data [MONO_FIELD_SIGNATURE] && !is_valid_blob_object (ctx, data [MONO_FIELD_SIGNATURE], 1))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid field row %d invalid signature blob token 0x%x", i, data [MONO_FIELD_SIGNATURE]));

		//TODO verify contant flag

		if (i + 1 < module_field_list) {
			guint32 access = flags & FIELD_ATTRIBUTE_FIELD_ACCESS_MASK;
			if (!(flags & FIELD_ATTRIBUTE_STATIC))
				ADD_ERROR (ctx, g_strdup_printf ("Invalid field row %d is a global variable but is not static", i));
			if (access != FIELD_ATTRIBUTE_COMPILER_CONTROLLED && access != FIELD_ATTRIBUTE_PRIVATE && access != FIELD_ATTRIBUTE_PUBLIC)
				ADD_ERROR (ctx, g_strdup_printf ("Invalid field row %d is a global variable but have wrong visibility %x", i, access));
		}
	}
}

static void
verify_field_table_full (VerifyContext *ctx)
{
	MonoTableInfo *table = &ctx->image->tables [MONO_TABLE_FIELD];
	guint32 data [MONO_FIELD_SIZE];
	int i;
	
	for (i = 0; i < table->rows; ++i) {
		mono_metadata_decode_row (table, i, data, MONO_FIELD_SIZE);

		if (!data [MONO_FIELD_SIGNATURE] || !is_valid_field_signature (ctx, data [MONO_FIELD_SIGNATURE]))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid field row %d invalid signature token %08x", i, data [MONO_FIELD_SIGNATURE]));
	}
}

/*bits 8,9,10,11,13,14,15*/
#define INVALID_METHOD_IMPLFLAG_BITS ((1 << 9) | (1 << 10) | (1 << 11) | (1 << 13) | (1 << 14) | (1 << 15))
static void
verify_method_table (VerifyContext *ctx)
{
	MonoTableInfo *table = &ctx->image->tables [MONO_TABLE_METHOD];
	guint32 data [MONO_METHOD_SIZE], flags, implflags, rva, module_method_list, access, code_type;
	guint32 paramlist = 1;
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
			if (flags & METHOD_ATTRIBUTE_FINAL)
				ADD_ERROR (ctx, g_strdup_printf ("Invalid method row %d is Abstract and Final", i));
			if (!(flags & METHOD_ATTRIBUTE_VIRTUAL))
				ADD_ERROR (ctx, g_strdup_printf ("Invalid method row %d is Abstract but not Virtual", i));
		}

		if (access == METHOD_ATTRIBUTE_COMPILER_CONTROLLED && (flags & (METHOD_ATTRIBUTE_RT_SPECIAL_NAME | METHOD_ATTRIBUTE_SPECIAL_NAME)))
			ADD_WARNING (ctx, g_strdup_printf ("Invalid method row %d is CompileControlled and SpecialName or RtSpecialName", i));

		if ((flags & METHOD_ATTRIBUTE_RT_SPECIAL_NAME) && !(flags & METHOD_ATTRIBUTE_SPECIAL_NAME))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid method row %d is RTSpecialName but not SpecialName", i));

		//XXX no checks against cas stuff 10,11,12,13)

		//TODO check iface with .ctor (15,16)

		if (i + 1 < module_method_list) {
			if (!(flags & METHOD_ATTRIBUTE_STATIC))
				ADD_ERROR (ctx, g_strdup_printf ("Invalid method row %d is a global method but not Static", i));
			if (flags & (METHOD_ATTRIBUTE_ABSTRACT | METHOD_ATTRIBUTE_VIRTUAL))
				ADD_ERROR (ctx, g_strdup_printf ("Invalid method row %d is a global method but is Abstract or Virtual", i));
			if (access == METHOD_ATTRIBUTE_FAMILY || access == METHOD_ATTRIBUTE_FAM_AND_ASSEM || access == METHOD_ATTRIBUTE_FAM_OR_ASSEM)
				ADD_ERROR (ctx, g_strdup_printf ("Invalid method row %d is a global method but not CompilerControled, Public, Private or Assembly", i));
		}

		//TODO check valuetype for synchronized

		if ((flags & (METHOD_ATTRIBUTE_FINAL | METHOD_ATTRIBUTE_NEW_SLOT | METHOD_ATTRIBUTE_STRICT)) && !(flags & METHOD_ATTRIBUTE_VIRTUAL))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid method row %d is (Final, NewSlot or Strict) but not Virtual", i));

		if (flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) {
			if (flags & METHOD_ATTRIBUTE_VIRTUAL)
				ADD_ERROR (ctx, g_strdup_printf ("Invalid method row %d is PinvokeImpl and Virtual", i));
			if (!(flags & METHOD_ATTRIBUTE_STATIC))
				ADD_ERROR (ctx, g_strdup_printf ("Invalid method row %d is PinvokeImpl but not Static", i));
		}

		if (!(flags & METHOD_ATTRIBUTE_ABSTRACT) && !rva && !(flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) && 
				!(implflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) && code_type != METHOD_IMPL_ATTRIBUTE_RUNTIME)
			ADD_ERROR (ctx, g_strdup_printf ("Invalid method row %d is not Abstract and neither PinvokeImpl, Runtime, InternalCall or with RVA != 0", i));

		if (access == METHOD_ATTRIBUTE_COMPILER_CONTROLLED && !(rva || (flags & METHOD_ATTRIBUTE_PINVOKE_IMPL)))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid method row %d is CompilerControlled but neither RVA != 0 or PinvokeImpl", i));

		//TODO check signature contents

		if (rva) {
			if ((flags & (METHOD_ATTRIBUTE_ABSTRACT | METHOD_ATTRIBUTE_PINVOKE_IMPL)) || (implflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL))
				ADD_ERROR (ctx, g_strdup_printf ("Invalid method row %d has RVA != 0 but is either Abstract, InternalCall or PinvokeImpl", i));
			if (code_type == METHOD_IMPL_ATTRIBUTE_OPTIL)
				ADD_ERROR (ctx, g_strdup_printf ("Invalid method row %d has RVA != 0 but is CodeTypeMask is neither Native, CIL or Runtime", i));
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

		if (data [MONO_METHOD_SIGNATURE] && !is_valid_blob_object (ctx, data [MONO_METHOD_SIGNATURE], 1))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid method row %d invalid signature blob token 0x%x", i, data [MONO_METHOD_SIGNATURE]));

		if (data [MONO_METHOD_PARAMLIST] == 0)
			ADD_ERROR (ctx, g_strdup_printf ("Invalid method row %d ParamList be be >= 1", i));

		if (data [MONO_METHOD_PARAMLIST] < paramlist)
			ADD_ERROR (ctx, g_strdup_printf ("Invalid method row %d ParamList rowid 0x%08x can't be smaller than of previous row 0x%08x", i, data [MONO_METHOD_PARAMLIST], paramlist));

		if (data [MONO_METHOD_PARAMLIST] > ctx->image->tables [MONO_TABLE_PARAM].rows + 1)
			ADD_ERROR (ctx, g_strdup_printf ("Invalid method row %d ParamList rowid 0x%08x is out of range", i, data [MONO_METHOD_PARAMLIST]));

		paramlist = data [MONO_METHOD_PARAMLIST];

	}
}

static void
verify_method_table_full (VerifyContext *ctx)
{
	MonoTableInfo *table = &ctx->image->tables [MONO_TABLE_METHOD];
	guint32 data [MONO_METHOD_SIZE], rva, locals_token;
	int i;

	for (i = 0; i < table->rows; ++i) {
		mono_metadata_decode_row (table, i, data, MONO_METHOD_SIZE);
		rva = data [MONO_METHOD_RVA];

		if (!data [MONO_METHOD_SIGNATURE] || !is_valid_method_signature (ctx, data [MONO_METHOD_SIGNATURE]))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid method row %d invalid signature token 0x%08x", i, data [MONO_METHOD_SIGNATURE]));

		if (rva && !is_valid_method_header (ctx, rva, &locals_token))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid method row %d RVA points to an invalid method header", i));
	}
}

static guint32
get_next_param_count (VerifyContext *ctx, guint32 *current_method)
{
	MonoTableInfo *table = &ctx->image->tables [MONO_TABLE_METHOD];
	guint32 row = *current_method;
	guint32 paramlist, tmp;


	paramlist = mono_metadata_decode_row_col (table, row++, MONO_METHOD_PARAMLIST);
	while (row < table->rows) {
		tmp = mono_metadata_decode_row_col (table, row, MONO_METHOD_PARAMLIST);
		if (tmp > paramlist) {
			*current_method = row;
			return tmp - paramlist;
		}
		++row;
	}

	/*no more methods, all params apply to the last one*/
	*current_method = table->rows;
	return (guint32)-1;
}


#define INVALID_PARAM_FLAGS_BITS ((1 << 2) | (1 << 3) | (1 << 5) | (1 << 6) | (1 << 7) | (1 << 8) | (1 << 9) | (1 << 10) | (1 << 11) | (1 << 14) | (1 << 15))
static void
verify_param_table (VerifyContext *ctx)
{
	MonoTableInfo *table = &ctx->image->tables [MONO_TABLE_PARAM];
	guint32 data [MONO_PARAM_SIZE], flags, sequence = 0, remaining_params, current_method = 0;
	gboolean first_param = TRUE;
	int i;

	if (ctx->image->tables [MONO_TABLE_METHOD].rows == 0) {
		if (table->rows > 0)
			ADD_ERROR (ctx, g_strdup ("Param table has rows while the method table has zero"));
		return;
	}
	
	remaining_params = get_next_param_count (ctx, &current_method);

	for (i = 0; i < table->rows; ++i) {
		mono_metadata_decode_row (table, i, data, MONO_PARAM_SIZE);
		flags = data [MONO_PARAM_FLAGS];

		if (flags & INVALID_PARAM_FLAGS_BITS)
			ADD_ERROR (ctx, g_strdup_printf ("Invalid param row %d bad Flags value 0x%08x", i, flags));

		if (search_sorted_table (ctx, MONO_TABLE_CONSTANT, MONO_CONSTANT_PARENT, make_coded_token (HAS_CONSTANT_DESC, MONO_TABLE_PARAM, i)) == -1) {
			if (flags & PARAM_ATTRIBUTE_HAS_DEFAULT)
				ADD_ERROR (ctx, g_strdup_printf ("Invalid param row %d HasDefault = 1 but no owned row in Contant table", i));
		} else {
			if (!(flags & PARAM_ATTRIBUTE_HAS_DEFAULT))
				ADD_ERROR (ctx, g_strdup_printf ("Invalid param row %d HasDefault = 0 but has owned row in Contant table", i));
		}

		if ((flags & PARAM_ATTRIBUTE_HAS_FIELD_MARSHAL) && search_sorted_table (ctx, MONO_TABLE_FIELDMARSHAL, MONO_FIELD_MARSHAL_PARENT, make_coded_token (HAS_FIELD_MARSHAL_DESC, MONO_TABLE_PARAM, i)) == -1)
			ADD_ERROR (ctx, g_strdup_printf ("Invalid param row %d HasFieldMarshal = 1 but no owned row in FieldMarshal table", i));

		if (!is_valid_string (ctx, data [MONO_PARAM_NAME]))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid param row %d Name = 1 bad token 0x%08x", i, data [MONO_PARAM_NAME]));

		if (!first_param && data [MONO_PARAM_SEQUENCE] <= sequence)
				ADD_ERROR (ctx, g_strdup_printf ("Invalid param row %d sequece = %d previus param has %d", i, data [MONO_PARAM_SEQUENCE], sequence));

		first_param = FALSE;
		sequence = data [MONO_PARAM_SEQUENCE];
		if (--remaining_params == 0) {
			remaining_params = get_next_param_count (ctx, &current_method);
			first_param = TRUE;
		}
	}
}

static void
verify_interfaceimpl_table (VerifyContext *ctx)
{
	MonoTableInfo *table = &ctx->image->tables [MONO_TABLE_INTERFACEIMPL];
	guint32 data [MONO_INTERFACEIMPL_SIZE];
	int i;

	for (i = 0; i < table->rows; ++i) {
		mono_metadata_decode_row (table, i, data, MONO_INTERFACEIMPL_SIZE);
		if (data [MONO_INTERFACEIMPL_CLASS] && data [MONO_INTERFACEIMPL_CLASS] > ctx->image->tables [MONO_TABLE_TYPEDEF].rows)
			ADD_ERROR (ctx, g_strdup_printf ("Invalid InterfaceImpl row %d Class field 0x%08x", i, data [MONO_INTERFACEIMPL_CLASS]));

		if (!is_valid_coded_index (ctx, TYPEDEF_OR_REF_DESC, data [MONO_INTERFACEIMPL_INTERFACE]))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid InterfaceImpl row %d Inteface field coded index 0x%08x", i, data [MONO_INTERFACEIMPL_INTERFACE]));

		if (!get_coded_index_token (TYPEDEF_OR_REF_DESC, data [MONO_INTERFACEIMPL_INTERFACE]))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid InterfaceImpl row %d Inteface field is null", i));
	}
}

static void
verify_memberref_table (VerifyContext *ctx)
{
	MonoTableInfo *table = &ctx->image->tables [MONO_TABLE_MEMBERREF];
	guint32 data [MONO_MEMBERREF_SIZE];
	int i;

	for (i = 0; i < table->rows; ++i) {
		mono_metadata_decode_row (table, i, data, MONO_MEMBERREF_SIZE);

		if (!is_valid_coded_index (ctx, MEMBERREF_PARENT_DESC, data [MONO_MEMBERREF_CLASS]))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid MemberRef row %d Class field coded index 0x%08x", i, data [MONO_MEMBERREF_CLASS]));

		if (!get_coded_index_token (MEMBERREF_PARENT_DESC, data [MONO_MEMBERREF_CLASS]))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid MemberRef row %d Class field coded is null", i));

		if (!is_valid_non_empty_string (ctx, data [MONO_MEMBERREF_NAME]))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid MemberRef row %d Name field coded is invalid or empty 0x%08x", i, data [MONO_MEMBERREF_NAME]));

		if (data [MONO_MEMBERREF_SIGNATURE] && !is_valid_blob_object (ctx, data [MONO_MEMBERREF_SIGNATURE], 1))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid MemberRef row %d invalid signature blob token 0x%x", i, data [MONO_MEMBERREF_SIGNATURE]));
	}
}


static void
verify_memberref_table_full (VerifyContext *ctx)
{
	MonoTableInfo *table = &ctx->image->tables [MONO_TABLE_MEMBERREF];
	guint32 data [MONO_MEMBERREF_SIZE];
	int i;

	for (i = 0; i < table->rows; ++i) {
		mono_metadata_decode_row (table, i, data, MONO_MEMBERREF_SIZE);

		if (!is_valid_method_or_field_signature (ctx, data [MONO_MEMBERREF_SIGNATURE]))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid MemberRef row %d Signature field  0x%08x", i, data [MONO_MEMBERREF_SIGNATURE]));
	}
}

static void
verify_constant_table (VerifyContext *ctx)
{
	MonoTableInfo *table = &ctx->image->tables [MONO_TABLE_CONSTANT];
	guint32 data [MONO_CONSTANT_SIZE], type;
	int i;

	for (i = 0; i < table->rows; ++i) {
		mono_metadata_decode_row (table, i, data, MONO_CONSTANT_SIZE);
		type = data [MONO_CONSTANT_TYPE];

		if (!((type >= MONO_TYPE_BOOLEAN && type <= MONO_TYPE_STRING) || type == MONO_TYPE_CLASS))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid Constant row %d Type field 0x%08x", i, type));

		if (!is_valid_coded_index (ctx, HAS_CONSTANT_DESC, data [MONO_CONSTANT_PARENT]))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid Constant row %d Parent field coded index 0x%08x", i, data [MONO_CONSTANT_PARENT]));

		if (!get_coded_index_token (HAS_CONSTANT_DESC, data [MONO_CONSTANT_PARENT]))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid Constant row %d Parent field coded is null", i));

		if (!is_valid_constant (ctx, type, data [MONO_CONSTANT_VALUE]))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid Constant row %d Value field 0x%08x", i, data [MONO_CONSTANT_VALUE]));
	}
}

static void
verify_cattr_table (VerifyContext *ctx)
{
	MonoTableInfo *table = &ctx->image->tables [MONO_TABLE_CUSTOMATTRIBUTE];
	guint32 data [MONO_CUSTOM_ATTR_SIZE];
	int i;

	for (i = 0; i < table->rows; ++i) {
		mono_metadata_decode_row (table, i, data, MONO_CUSTOM_ATTR_SIZE);

		if (!is_valid_coded_index (ctx, HAS_CATTR_DESC, data [MONO_CUSTOM_ATTR_PARENT]))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid CustomAttribute row %d Parent field 0x%08x", i, data [MONO_CUSTOM_ATTR_PARENT]));

		if (!is_valid_coded_index (ctx, CATTR_TYPE_DESC, data [MONO_CUSTOM_ATTR_TYPE]) || !get_coded_index_token (CATTR_TYPE_DESC, data [MONO_CUSTOM_ATTR_TYPE]))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid CustomAttribute row %d Type field 0x%08x", i, data [MONO_CUSTOM_ATTR_TYPE]));

		if (data [MONO_CUSTOM_ATTR_VALUE] && !is_valid_blob_object (ctx, data [MONO_CUSTOM_ATTR_VALUE], 0))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid CustomAttribute row %d invalid value blob 0x%x", i, data [MONO_CUSTOM_ATTR_VALUE]));
	}
}

static void
verify_cattr_table_full (VerifyContext *ctx)
{
	MonoError error;
	MonoTableInfo *table = &ctx->image->tables [MONO_TABLE_CUSTOMATTRIBUTE];
	MonoMethod *ctor;
	const char *ptr;
	guint32 data [MONO_CUSTOM_ATTR_SIZE], mtoken, size;
	int i;

	for (i = 0; i < table->rows; ++i) {
		mono_metadata_decode_row (table, i, data, MONO_CUSTOM_ATTR_SIZE);

		if (!is_valid_cattr_blob (ctx, data [MONO_CUSTOM_ATTR_VALUE]))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid CustomAttribute row %d Value field 0x%08x", i, data [MONO_CUSTOM_ATTR_VALUE]));

		mtoken = data [MONO_CUSTOM_ATTR_TYPE] >> MONO_CUSTOM_ATTR_TYPE_BITS;
		switch (data [MONO_CUSTOM_ATTR_TYPE] & MONO_CUSTOM_ATTR_TYPE_MASK) {
		case MONO_CUSTOM_ATTR_TYPE_METHODDEF:
			mtoken |= MONO_TOKEN_METHOD_DEF;
			break;
		case MONO_CUSTOM_ATTR_TYPE_MEMBERREF:
			mtoken |= MONO_TOKEN_MEMBER_REF;
			break;
		default:
			ADD_ERROR (ctx, g_strdup_printf ("Invalid CustomAttribute constructor row %d Token 0x%08x", i, data [MONO_CUSTOM_ATTR_TYPE]));
		}

		ctor = mono_get_method_checked (ctx->image, mtoken, NULL, NULL, &error);

		if (!ctor) {
			ADD_ERROR (ctx, g_strdup_printf ("Invalid CustomAttribute content row %d Could not load ctor due to %s", i, mono_error_get_message (&error)));
			mono_error_cleanup (&error);
		}

		/*This can't fail since this is checked in is_valid_cattr_blob*/
		g_assert (decode_signature_header (ctx, data [MONO_CUSTOM_ATTR_VALUE], &size, &ptr));

		if (!is_valid_cattr_content (ctx, ctor, ptr, size)) {
			char *ctor_name =  mono_method_full_name (ctor, TRUE);
			ADD_ERROR (ctx, g_strdup_printf ("Invalid CustomAttribute content row %d Value field 0x%08x ctor: %s", i, data [MONO_CUSTOM_ATTR_VALUE], ctor_name));
			g_free (ctor_name);
		}
	}
}

static void
verify_field_marshal_table (VerifyContext *ctx)
{
	MonoTableInfo *table = &ctx->image->tables [MONO_TABLE_FIELDMARSHAL];
	guint32 data [MONO_FIELD_MARSHAL_SIZE];
	int i;

	for (i = 0; i < table->rows; ++i) {
		mono_metadata_decode_row (table, i, data, MONO_FIELD_MARSHAL_SIZE);

		if (!is_valid_coded_index (ctx, HAS_FIELD_MARSHAL_DESC, data [MONO_FIELD_MARSHAL_PARENT]))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid FieldMarshal row %d Parent field 0x%08x", i, data [MONO_FIELD_MARSHAL_PARENT]));

		if (!get_coded_index_token (HAS_FIELD_MARSHAL_DESC, data [MONO_FIELD_MARSHAL_PARENT]))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid FieldMarshal row %d Parent field is null", i));

		if (!data [MONO_FIELD_MARSHAL_NATIVE_TYPE])
			ADD_ERROR (ctx, g_strdup_printf ("Invalid FieldMarshal row %d NativeType field is null", i));

		if (!is_valid_blob_object (ctx, data [MONO_FIELD_MARSHAL_NATIVE_TYPE], 1))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid FieldMarshal row %d invalid NativeType blob 0x%x", i, data [MONO_FIELD_MARSHAL_NATIVE_TYPE]));
	}
}

static void
verify_field_marshal_table_full (VerifyContext *ctx)
{
	MonoTableInfo *table = &ctx->image->tables [MONO_TABLE_FIELDMARSHAL];
	guint32 data [MONO_FIELD_MARSHAL_SIZE];
	int i;

	for (i = 0; i < table->rows; ++i) {
		mono_metadata_decode_row (table, i, data, MONO_FIELD_MARSHAL_SIZE);

		if (!is_valid_marshal_spec (ctx, data [MONO_FIELD_MARSHAL_NATIVE_TYPE]))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid FieldMarshal row %d NativeType field 0x%08x", i, data [MONO_FIELD_MARSHAL_NATIVE_TYPE]));
	}
}

static void
verify_decl_security_table (VerifyContext *ctx)
{
	MonoTableInfo *table = &ctx->image->tables [MONO_TABLE_DECLSECURITY];
	guint32 data [MONO_DECL_SECURITY_SIZE];
	int i;

	for (i = 0; i < table->rows; ++i) {
		mono_metadata_decode_row (table, i, data, MONO_DECL_SECURITY_SIZE);

		if (!is_valid_coded_index (ctx, HAS_DECL_SECURITY_DESC, data [MONO_DECL_SECURITY_PARENT]))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid DeclSecurity row %d Parent field 0x%08x", i, data [MONO_DECL_SECURITY_PARENT]));

		if (!get_coded_index_token (HAS_DECL_SECURITY_DESC, data [MONO_DECL_SECURITY_PARENT]))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid DeclSecurity row %d Parent field is null", i));

		if (!data [MONO_DECL_SECURITY_PERMISSIONSET])
			ADD_ERROR (ctx, g_strdup_printf ("Invalid DeclSecurity row %d PermissionSet field is null", i));
	}
}

static void
verify_decl_security_table_full (VerifyContext *ctx)
{
	MonoTableInfo *table = &ctx->image->tables [MONO_TABLE_DECLSECURITY];
	guint32 data [MONO_DECL_SECURITY_SIZE];
	int i;

	for (i = 0; i < table->rows; ++i) {
		mono_metadata_decode_row (table, i, data, MONO_DECL_SECURITY_SIZE);

		if (!is_valid_permission_set (ctx, data [MONO_DECL_SECURITY_PERMISSIONSET]))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid DeclSecurity row %d PermissionSet field 0x%08x", i, data [MONO_DECL_SECURITY_PERMISSIONSET]));
	}
}

static void
verify_class_layout_table (VerifyContext *ctx)
{
	MonoTableInfo *table = &ctx->image->tables [MONO_TABLE_CLASSLAYOUT];
	guint32 data [MONO_CLASS_LAYOUT_SIZE];
	int i;

	for (i = 0; i < table->rows; ++i) {
		mono_metadata_decode_row (table, i, data, MONO_CLASS_LAYOUT_SIZE);

		if (!data [MONO_CLASS_LAYOUT_PARENT] || data[MONO_CLASS_LAYOUT_PARENT] > ctx->image->tables [MONO_TABLE_TYPEDEF].rows + 1)
			ADD_ERROR (ctx, g_strdup_printf ("Invalid ClassLayout row %d Parent field 0x%08x", i, data [MONO_TABLE_TYPEDEF]));

		switch (data [MONO_CLASS_LAYOUT_PACKING_SIZE]) {
		case 0:
		case 1:
		case 2:
		case 4:
		case 8:
		case 16:
		case 32:
		case 64:
		case 128:
			break;
		default:
			ADD_ERROR (ctx, g_strdup_printf ("Invalid ClassLayout row %d Packing field %d", i, data [MONO_CLASS_LAYOUT_PACKING_SIZE]));
		}
	}
}

static void
verify_field_layout_table (VerifyContext *ctx)
{
	MonoTableInfo *table = &ctx->image->tables [MONO_TABLE_FIELDLAYOUT];
	guint32 data [MONO_FIELD_LAYOUT_SIZE];
	int i;

	for (i = 0; i < table->rows; ++i) {
		mono_metadata_decode_row (table, i, data, MONO_FIELD_LAYOUT_SIZE);

		if (!data [MONO_FIELD_LAYOUT_FIELD] || data[MONO_FIELD_LAYOUT_FIELD] > ctx->image->tables [MONO_TABLE_FIELD].rows + 1)
			ADD_ERROR (ctx, g_strdup_printf ("Invalid FieldLayout row %d Field field 0x%08x", i, data [MONO_FIELD_LAYOUT_FIELD]));
	}
}

static void
verify_standalonesig_table (VerifyContext *ctx)
{
	MonoTableInfo *table = &ctx->image->tables [MONO_TABLE_STANDALONESIG];
	guint32 data [MONO_STAND_ALONE_SIGNATURE_SIZE];
	int i;

	for (i = 0; i < table->rows; ++i) {
		mono_metadata_decode_row (table, i, data, MONO_STAND_ALONE_SIGNATURE_SIZE);

		if (data [MONO_STAND_ALONE_SIGNATURE] && !is_valid_blob_object (ctx, data [MONO_STAND_ALONE_SIGNATURE], 1))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid StandAloneSig row %d invalid signature 0x%x", i, data [MONO_STAND_ALONE_SIGNATURE]));
	}
}

static void
verify_standalonesig_table_full (VerifyContext *ctx)
{
	MonoTableInfo *table = &ctx->image->tables [MONO_TABLE_STANDALONESIG];
	guint32 data [MONO_STAND_ALONE_SIGNATURE_SIZE];
	int i;

	for (i = 0; i < table->rows; ++i) {
		mono_metadata_decode_row (table, i, data, MONO_STAND_ALONE_SIGNATURE_SIZE);

		if (!is_valid_standalonesig_blob (ctx, data [MONO_STAND_ALONE_SIGNATURE]))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid StandAloneSig row %d Signature field 0x%08x", i, data [MONO_STAND_ALONE_SIGNATURE]));
	}
}

static void
verify_eventmap_table (VerifyContext *ctx)
{
	MonoTableInfo *table = &ctx->image->tables [MONO_TABLE_EVENTMAP];
	guint32 data [MONO_EVENT_MAP_SIZE], eventlist = 0;
	int i;

	for (i = 0; i < table->rows; ++i) {
		mono_metadata_decode_row (table, i, data, MONO_EVENT_MAP_SIZE);

		if (!data [MONO_EVENT_MAP_PARENT] || data [MONO_EVENT_MAP_PARENT] > ctx->image->tables [MONO_TABLE_TYPEDEF].rows + 1)
			ADD_ERROR (ctx, g_strdup_printf ("Invalid EventMap row %d Parent field 0x%08x", i, data [MONO_EVENT_MAP_PARENT]));

		if (!data [MONO_EVENT_MAP_EVENTLIST] || data [MONO_EVENT_MAP_EVENTLIST] <= eventlist)
			ADD_ERROR (ctx, g_strdup_printf ("Invalid EventMap row %d EventList field %d", i, data [MONO_EVENT_MAP_EVENTLIST]));

		eventlist = data [MONO_EVENT_MAP_EVENTLIST];
	}
}

#define INVALID_EVENT_FLAGS_BITS ~((1 << 9) | (1 << 10))
static void
verify_event_table (VerifyContext *ctx)
{
	MonoTableInfo *table = &ctx->image->tables [MONO_TABLE_EVENT];
	guint32 data [MONO_EVENT_SIZE];
	int i;

	for (i = 0; i < table->rows; ++i) {
		mono_metadata_decode_row (table, i, data, MONO_EVENT_SIZE);

		if (data [MONO_EVENT_FLAGS] & INVALID_EVENT_FLAGS_BITS)
			ADD_ERROR (ctx, g_strdup_printf ("Invalid Event row %d EventFlags field %08x", i, data [MONO_EVENT_FLAGS]));

		if (!is_valid_non_empty_string (ctx, data [MONO_EVENT_NAME]))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid Event row %d Name field %08x", i, data [MONO_EVENT_NAME]));

		if (!is_valid_coded_index (ctx, TYPEDEF_OR_REF_DESC, data [MONO_EVENT_TYPE]))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid Event row %d EventType field %08x", i, data [MONO_EVENT_TYPE]));
	}
}

static void
verify_event_table_full (VerifyContext *ctx)
{
	MonoTableInfo *table = &ctx->image->tables [MONO_TABLE_EVENT];
	MonoTableInfo *sema_table = &ctx->image->tables [MONO_TABLE_METHODSEMANTICS];
	guint32 data [MONO_EVENT_SIZE], sema_data [MONO_METHOD_SEMA_SIZE], token;
	gboolean found_add, found_remove;
	int i, idx;

	for (i = 0; i < table->rows; ++i) {
		mono_metadata_decode_row (table, i, data, MONO_EVENT_SIZE);

		token = make_coded_token (HAS_SEMANTICS_DESC, MONO_TABLE_EVENT, i);
		idx = search_sorted_table (ctx, MONO_TABLE_METHODSEMANTICS, MONO_METHOD_SEMA_ASSOCIATION, token);
		if (idx == -1)
			ADD_ERROR (ctx, g_strdup_printf ("Invalid Event row %d has no AddOn or RemoveOn associated methods", i));

		//first we move to the first row for this event
		while (idx > 0) {
			if (mono_metadata_decode_row_col (sema_table, idx - 1, MONO_METHOD_SEMA_ASSOCIATION) != token)
				break;
			--idx;
		}
		//now move forward looking for AddOn and RemoveOn rows
		found_add = found_remove = FALSE;
		while (idx < sema_table->rows) {
			mono_metadata_decode_row (sema_table, idx, sema_data, MONO_METHOD_SEMA_SIZE);
			if (sema_data [MONO_METHOD_SEMA_ASSOCIATION] != token)
				break;
			if (sema_data [MONO_METHOD_SEMA_SEMANTICS] & METHOD_SEMANTIC_ADD_ON)
				found_add = TRUE;
			if (sema_data [MONO_METHOD_SEMA_SEMANTICS] & METHOD_SEMANTIC_REMOVE_ON)
				found_remove = TRUE;
			if (found_add && found_remove)
				break;
			++idx;
		}

		if (!found_add)
			ADD_ERROR (ctx, g_strdup_printf ("Invalid Event row %d has no AddOn associated method", i));
		if (!found_remove)
			ADD_ERROR (ctx, g_strdup_printf ("Invalid Event row %d has no RemoveOn associated method", i));
	}
}

static void
verify_propertymap_table (VerifyContext *ctx)
{
	MonoTableInfo *table = &ctx->image->tables [MONO_TABLE_PROPERTYMAP];
	guint32 data [MONO_PROPERTY_MAP_SIZE], propertylist = 0;
	int i;

	for (i = 0; i < table->rows; ++i) {
		mono_metadata_decode_row (table, i, data, MONO_PROPERTY_MAP_SIZE);

		if (!data [MONO_PROPERTY_MAP_PARENT] || data [MONO_PROPERTY_MAP_PARENT] > ctx->image->tables [MONO_TABLE_TYPEDEF].rows + 1)
			ADD_ERROR (ctx, g_strdup_printf ("Invalid PropertyMap row %d Parent field 0x%08x", i, data [MONO_PROPERTY_MAP_PARENT]));

		if (!data [MONO_PROPERTY_MAP_PROPERTY_LIST] || data [MONO_PROPERTY_MAP_PROPERTY_LIST] <= propertylist)
			ADD_ERROR (ctx, g_strdup_printf ("Invalid PropertyMap row %d PropertyList field %d", i, data [MONO_PROPERTY_MAP_PROPERTY_LIST]));

		propertylist = data [MONO_PROPERTY_MAP_PROPERTY_LIST];
	}
}

#define INVALID_PROPERTY_FLAGS_BITS ~((1 << 9) | (1 << 10) | (1 << 12))
static void
verify_property_table (VerifyContext *ctx)
{
	MonoTableInfo *table = &ctx->image->tables [MONO_TABLE_PROPERTY];
	guint32 data [MONO_PROPERTY_SIZE];
	int i;

	for (i = 0; i < table->rows; ++i) {
		mono_metadata_decode_row (table, i, data, MONO_PROPERTY_SIZE);

		if (data [MONO_PROPERTY_FLAGS] & INVALID_PROPERTY_FLAGS_BITS)
			ADD_ERROR (ctx, g_strdup_printf ("Invalid Property row %d PropertyFlags field %08x", i, data [MONO_PROPERTY_FLAGS]));

		if (!is_valid_non_empty_string (ctx, data [MONO_PROPERTY_NAME]))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid Property row %d Name field %08x", i, data [MONO_PROPERTY_NAME]));

		if (!is_valid_property_sig_blob (ctx, data [MONO_PROPERTY_TYPE]))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid Property row %d Type field %08x", i, data [MONO_PROPERTY_TYPE]));

		if ((data [MONO_PROPERTY_FLAGS] & PROPERTY_ATTRIBUTE_HAS_DEFAULT) &&
				search_sorted_table (ctx, MONO_TABLE_CONSTANT, MONO_CONSTANT_PARENT, make_coded_token (HAS_CONSTANT_DESC, MONO_TABLE_PROPERTY, i)) == -1)
			ADD_ERROR (ctx, g_strdup_printf ("Invalid Property row %d has HasDefault but there is no corresponding row in the Constant table", i));

	}
}

static void
verify_methodimpl_table (VerifyContext *ctx)
{
	MonoTableInfo *table = &ctx->image->tables [MONO_TABLE_METHODIMPL];
	guint32 data [MONO_METHODIMPL_SIZE];
	int i;

	for (i = 0; i < table->rows; ++i) {
		mono_metadata_decode_row (table, i, data, MONO_METHODIMPL_SIZE);

		if (!data [MONO_METHODIMPL_CLASS] || data [MONO_METHODIMPL_CLASS] > ctx->image->tables [MONO_TABLE_TYPEDEF].rows + 1)
			ADD_ERROR (ctx, g_strdup_printf ("Invalid MethodImpl row %d Class field %08x", i, data [MONO_TABLE_TYPEDEF]));
			
		if (!get_coded_index_token (METHODDEF_OR_REF_DESC, data [MONO_METHODIMPL_BODY]))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid MethodImpl row %d MethodBody field %08x", i, data [MONO_METHODIMPL_BODY]));
		
		if (!is_valid_coded_index (ctx, METHODDEF_OR_REF_DESC, data [MONO_METHODIMPL_BODY]))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid MethodImpl row %d MethodBody field %08x", i, data [MONO_METHODIMPL_BODY]));

		if (!get_coded_index_token (METHODDEF_OR_REF_DESC, data [MONO_METHODIMPL_DECLARATION]))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid MethodImpl row %d MethodDeclaration field %08x", i, data [MONO_METHODIMPL_DECLARATION]));
		
		if (!is_valid_coded_index (ctx, METHODDEF_OR_REF_DESC, data [MONO_METHODIMPL_DECLARATION]))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid MethodImpl row %d MethodDeclaration field %08x", i, data [MONO_METHODIMPL_DECLARATION]));
	}
}

static void
verify_moduleref_table (VerifyContext *ctx)
{
	MonoTableInfo *table = &ctx->image->tables [MONO_TABLE_MODULEREF];
	guint32 data [MONO_MODULEREF_SIZE];
	int i;

	for (i = 0; i < table->rows; ++i) {
		mono_metadata_decode_row (table, i, data, MONO_MODULEREF_SIZE);

		if (!is_valid_non_empty_string (ctx, data[MONO_MODULEREF_NAME]))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid ModuleRef row %d name field %08x", i, data [MONO_MODULEREF_NAME]));
	}
}

static void
verify_typespec_table (VerifyContext *ctx)
{
	MonoTableInfo *table = &ctx->image->tables [MONO_TABLE_TYPESPEC];
	guint32 data [MONO_TYPESPEC_SIZE];
	int i;

	for (i = 0; i < table->rows; ++i) {
		mono_metadata_decode_row (table, i, data, MONO_TYPESPEC_SIZE);

		if (data [MONO_TYPESPEC_SIGNATURE] && !is_valid_blob_object (ctx, data [MONO_TYPESPEC_SIGNATURE], 1))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid TypeSpec row %d Signature field %08x", i, data [MONO_TYPESPEC_SIGNATURE]));
	}
}

static void
verify_typespec_table_full (VerifyContext *ctx)
{
	MonoTableInfo *table = &ctx->image->tables [MONO_TABLE_TYPESPEC];
	guint32 data [MONO_TYPESPEC_SIZE];
	int i;

	for (i = 0; i < table->rows; ++i) {
		mono_metadata_decode_row (table, i, data, MONO_TYPESPEC_SIZE);
		ctx->token = (i + 1) | MONO_TOKEN_TYPE_SPEC;
		if (!is_valid_typespec_blob (ctx, data [MONO_TYPESPEC_SIGNATURE]))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid TypeSpec row %d Signature field %08x", i, data [MONO_TYPESPEC_SIGNATURE]));
	}
	ctx->token = 0;
}

#define INVALID_IMPLMAP_FLAGS_BITS ~((1 << 0) | (1 << 1) | (1 << 2) | (1 << 4) | (1 << 5) | (1 << 6) | (1 << 8) | (1 << 9) | (1 << 10) | (1 << 12) | (1 << 13))
static void
verify_implmap_table (VerifyContext *ctx)
{
	MonoTableInfo *table = &ctx->image->tables [MONO_TABLE_IMPLMAP];
	guint32 data [MONO_IMPLMAP_SIZE], cconv;
	int i;

	for (i = 0; i < table->rows; ++i) {
		mono_metadata_decode_row (table, i, data, MONO_IMPLMAP_SIZE);

		if (data [MONO_IMPLMAP_FLAGS] & INVALID_IMPLMAP_FLAGS_BITS)
			ADD_ERROR (ctx, g_strdup_printf ("Invalid ImplMap row %d Flags field %08x", i, data [MONO_IMPLMAP_FLAGS]));

		cconv = data [MONO_IMPLMAP_FLAGS] & PINVOKE_ATTRIBUTE_CALL_CONV_MASK;
		if (cconv == 0 || cconv == 0x0600 || cconv == 0x0700)
			ADD_ERROR (ctx, g_strdup_printf ("Invalid ImplMap row %d Invalid call conv field %x", i, cconv));

		if (!is_valid_coded_index (ctx, MEMBER_FORWARDED_DESC, data [MONO_IMPLMAP_MEMBER]))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid ImplMap row %d Invalid MemberForward token %x", i, data [MONO_IMPLMAP_MEMBER]));

		if (get_coded_index_table (MEMBER_FORWARDED_DESC, data [MONO_IMPLMAP_MEMBER]) != MONO_TABLE_METHOD)
			ADD_ERROR (ctx, g_strdup_printf ("Invalid ImplMap row %d only methods are supported token %x", i, data [MONO_IMPLMAP_MEMBER]));

		if (!get_coded_index_token (MEMBER_FORWARDED_DESC, data [MONO_IMPLMAP_MEMBER]))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid ImplMap row %d null token", i));

		if (!is_valid_non_empty_string (ctx, data [MONO_IMPLMAP_NAME]))
			ADD_ERROR (ctx, g_strdup_printf ("Invalid ImplMap row %d ImportName Token %x", i, data [MONO_IMPLMAP_NAME]));

		if (!data [MONO_IMPLMAP_SCOPE] || data [MONO_IMPLMAP_SCOPE] > ctx->image->tables [MONO_TABLE_MODULEREF].rows)
			ADD_ERROR (ctx, g_strdup_printf ("Invalid ImplMap row %d Invalid ImportScope token %x", i, data [MONO_IMPLMAP_SCOPE]));
	}
}

static void
verify_fieldrva_table (VerifyContext *ctx)
{
	MonoTableInfo *table = &ctx->image->tables [MONO_TABLE_FIELDRVA];
	guint32 data [MONO_FIELD_RVA_SIZE];
	int i;

	for (i = 0; i < table->rows; ++i) {
		mono_metadata_decode_row (table, i, data, MONO_FIELD_RVA_SIZE);

		if (!data [MONO_FIELD_RVA_RVA] || mono_cli_rva_image_map (ctx->image, data [MONO_FIELD_RVA_RVA]) == INVALID_ADDRESS)
			ADD_ERROR (ctx, g_strdup_printf ("Invalid FieldRVA row %d RVA %08x", i, data [MONO_FIELD_RVA_RVA]));

		if (!data [MONO_FIELD_RVA_FIELD] || data [MONO_FIELD_RVA_FIELD] > ctx->image->tables [MONO_TABLE_FIELD].rows + 1)
			ADD_ERROR (ctx, g_strdup_printf ("Invalid FieldRVA row %d Field %08x", i, data [MONO_FIELD_RVA_FIELD]));
	}
}

#define INVALID_ASSEMBLY_FLAGS_BITS ~((1 << 0) | (1 << 4) | (1 << 5) | (1 << 6) | (1 << 7) | (1 << 8) | (1 << 14) | (1 << 15))
static void
verify_assembly_table (VerifyContext *ctx)
{
	MonoTableInfo *table = &ctx->image->tables [MONO_TABLE_ASSEMBLY];
	guint32 data [MONO_ASSEMBLY_SIZE], hash;
	int i;

	if (table->rows > 1)
		ADD_ERROR (ctx, g_strdup_printf ("Assembly table can have zero or one rows, but now %d", table->rows));

	for (i = 0; i < table->rows; ++i) {
		mono_metadata_decode_row (table, i, data, MONO_ASSEMBLY_SIZE);

		hash = data [MONO_ASSEMBLY_HASH_ALG];
		if (!(hash == 0 || hash == 0x8003 || hash == 0x8004))
			ADD_ERROR (ctx, g_strdup_printf ("Assembly table row %d has invalid HashAlgId %x", i, hash));

		if (data [MONO_ASSEMBLY_FLAGS] & INVALID_ASSEMBLY_FLAGS_BITS)
			ADD_ERROR (ctx, g_strdup_printf ("Assembly table row %d has invalid Flags %08x", i, data [MONO_ASSEMBLY_FLAGS]));

		if (data [MONO_ASSEMBLY_PUBLIC_KEY] && !is_valid_blob_object (ctx, data [MONO_ASSEMBLY_PUBLIC_KEY], 1))
			ADD_ERROR (ctx, g_strdup_printf ("Assembly table row %d has invalid PublicKey %08x", i, data [MONO_ASSEMBLY_FLAGS]));

		if (!is_valid_non_empty_string (ctx, data [MONO_ASSEMBLY_NAME]))
			ADD_ERROR (ctx, g_strdup_printf ("Assembly table row %d has invalid Name %08x", i, data [MONO_ASSEMBLY_NAME]));

		if (data [MONO_ASSEMBLY_CULTURE] && !is_valid_string (ctx, data [MONO_ASSEMBLY_CULTURE]))
			ADD_ERROR (ctx, g_strdup_printf ("Assembly table row %d has invalid Culture %08x", i, data [MONO_ASSEMBLY_CULTURE]));
	}
}

#define INVALID_ASSEMBLYREF_FLAGS_BITS ~((1 << 0) | (1 << 8) | (1 << 14) | (1 << 15))
static void
verify_assemblyref_table (VerifyContext *ctx)
{
	MonoTableInfo *table = &ctx->image->tables [MONO_TABLE_ASSEMBLYREF];
	guint32 data [MONO_ASSEMBLYREF_SIZE];
	int i;

	for (i = 0; i < table->rows; ++i) {
		mono_metadata_decode_row (table, i, data, MONO_ASSEMBLYREF_SIZE);

		if (data [MONO_ASSEMBLYREF_FLAGS] & INVALID_ASSEMBLYREF_FLAGS_BITS)
			ADD_ERROR (ctx, g_strdup_printf ("AssemblyRef table row %d has invalid Flags %08x", i, data [MONO_ASSEMBLYREF_FLAGS]));

		if (data [MONO_ASSEMBLYREF_PUBLIC_KEY] && !is_valid_blob_object (ctx, data [MONO_ASSEMBLYREF_PUBLIC_KEY], 1))
			ADD_ERROR (ctx, g_strdup_printf ("AssemblyRef table row %d has invalid PublicKeyOrToken %08x", i, data [MONO_ASSEMBLYREF_PUBLIC_KEY]));

		if (!is_valid_non_empty_string (ctx, data [MONO_ASSEMBLYREF_NAME]))
			ADD_ERROR (ctx, g_strdup_printf ("AssemblyRef table row %d has invalid Name %08x", i, data [MONO_ASSEMBLYREF_NAME]));

		if (data [MONO_ASSEMBLYREF_CULTURE] && !is_valid_string (ctx, data [MONO_ASSEMBLYREF_CULTURE]))
			ADD_ERROR (ctx, g_strdup_printf ("AssemblyRef table row %d has invalid Culture %08x", i, data [MONO_ASSEMBLYREF_CULTURE]));

		if (data [MONO_ASSEMBLYREF_HASH_VALUE] && !is_valid_blob_object (ctx, data [MONO_ASSEMBLYREF_HASH_VALUE], 1))
			ADD_ERROR (ctx, g_strdup_printf ("AssemblyRef table row %d has invalid HashValue %08x", i, data [MONO_ASSEMBLYREF_HASH_VALUE]));
	}
}

#define INVALID_FILE_FLAGS_BITS ~(1)
static void
verify_file_table (VerifyContext *ctx)
{
	MonoTableInfo *table = &ctx->image->tables [MONO_TABLE_FILE];
	guint32 data [MONO_FILE_SIZE];
	int i;

	for (i = 0; i < table->rows; ++i) {
		mono_metadata_decode_row (table, i, data, MONO_FILE_SIZE);
		
		if (data [MONO_FILE_FLAGS] & INVALID_FILE_FLAGS_BITS)
			ADD_ERROR (ctx, g_strdup_printf ("File table row %d has invalid Flags %08x", i, data [MONO_FILE_FLAGS]));

		if (!is_valid_non_empty_string (ctx, data [MONO_FILE_NAME]))
			ADD_ERROR (ctx, g_strdup_printf ("File table row %d has invalid Name %08x", i, data [MONO_FILE_NAME]));

		if (!data [MONO_FILE_HASH_VALUE] || !is_valid_blob_object (ctx, data [MONO_FILE_HASH_VALUE], 1))
			ADD_ERROR (ctx, g_strdup_printf ("File table row %d has invalid HashValue %08x", i, data [MONO_FILE_HASH_VALUE]));
	}
}

#define INVALID_EXPORTED_TYPE_FLAGS_BITS (INVALID_TYPEDEF_FLAG_BITS & ~TYPE_ATTRIBUTE_FORWARDER)
static void
verify_exportedtype_table (VerifyContext *ctx)
{
	MonoTableInfo *table = &ctx->image->tables [MONO_TABLE_EXPORTEDTYPE];
	guint32 data [MONO_EXP_TYPE_SIZE];
	int i;

	for (i = 0; i < table->rows; ++i) {
		mono_metadata_decode_row (table, i, data, MONO_EXP_TYPE_SIZE);
		
		if (data [MONO_EXP_TYPE_FLAGS] & INVALID_EXPORTED_TYPE_FLAGS_BITS)
			ADD_ERROR (ctx, g_strdup_printf ("ExportedType table row %d has invalid Flags %08x", i, data [MONO_EXP_TYPE_FLAGS]));

		if (!is_valid_non_empty_string (ctx, data [MONO_EXP_TYPE_NAME]))
			ADD_ERROR (ctx, g_strdup_printf ("ExportedType table row %d has invalid TypeName %08x", i, data [MONO_FILE_NAME]));

		if (data [MONO_EXP_TYPE_NAMESPACE] && !is_valid_string (ctx, data [MONO_EXP_TYPE_NAMESPACE]))
			ADD_ERROR (ctx, g_strdup_printf ("ExportedType table row %d has invalid TypeNamespace %08x", i, data [MONO_EXP_TYPE_NAMESPACE]));

		if (!is_valid_coded_index (ctx, IMPLEMENTATION_DESC, data [MONO_EXP_TYPE_IMPLEMENTATION]))
			ADD_ERROR (ctx, g_strdup_printf ("ExportedType table row %d has invalid Implementation token %08x", i, data [MONO_EXP_TYPE_IMPLEMENTATION]));

		if (!get_coded_index_token (IMPLEMENTATION_DESC, data [MONO_EXP_TYPE_IMPLEMENTATION]))
			ADD_ERROR (ctx, g_strdup_printf ("ExportedType table row %d has null Implementation token", i));

		/*nested type can't have a namespace*/
		if (get_coded_index_table (IMPLEMENTATION_DESC, data [MONO_EXP_TYPE_IMPLEMENTATION]) == MONO_TABLE_EXPORTEDTYPE && data [MONO_EXP_TYPE_NAMESPACE])
			ADD_ERROR (ctx, g_strdup_printf ("ExportedType table row %d has denotes a nested type but has a non null TypeNamespace", i));
	}
}

#define INVALID_MANIFEST_RESOURCE_FLAGS_BITS ~((1 << 0) | (1 << 1) | (1 << 2))
static void
verify_manifest_resource_table (VerifyContext *ctx)
{
	MonoCLIImageInfo *iinfo = (MonoCLIImageInfo *)ctx->image->image_info;
	MonoCLIHeader *ch = &iinfo->cli_cli_header;
	MonoTableInfo *table = &ctx->image->tables [MONO_TABLE_MANIFESTRESOURCE];
	guint32 data [MONO_MANIFEST_SIZE], impl_table, token, resources_size;
	int i;

	resources_size = ch->ch_resources.size;

	for (i = 0; i < table->rows; ++i) {
		mono_metadata_decode_row (table, i, data, MONO_MANIFEST_SIZE);

		if (data [MONO_MANIFEST_FLAGS] & INVALID_MANIFEST_RESOURCE_FLAGS_BITS)
			ADD_ERROR (ctx, g_strdup_printf ("ManifestResource table row %d has invalid Flags %08x", i, data [MONO_MANIFEST_FLAGS]));

		if (data [MONO_MANIFEST_FLAGS] != 1 && data [MONO_MANIFEST_FLAGS] != 2)
			ADD_ERROR (ctx, g_strdup_printf ("ManifestResource table row %d has invalid Flags VisibilityMask %08x", i, data [MONO_MANIFEST_FLAGS]));

		if (!is_valid_non_empty_string (ctx, data [MONO_MANIFEST_NAME]))
			ADD_ERROR (ctx, g_strdup_printf ("ManifestResource table row %d has invalid Name %08x", i, data [MONO_MANIFEST_NAME]));

		if (!is_valid_coded_index (ctx, IMPLEMENTATION_DESC, data [MONO_MANIFEST_IMPLEMENTATION]))
			ADD_ERROR (ctx, g_strdup_printf ("ManifestResource table row %d has invalid Implementation token %08x", i, data [MONO_MANIFEST_IMPLEMENTATION]));

		impl_table = get_coded_index_table (IMPLEMENTATION_DESC, data [MONO_MANIFEST_IMPLEMENTATION]);
		token = get_coded_index_token (IMPLEMENTATION_DESC, data [MONO_MANIFEST_IMPLEMENTATION]);

		if (impl_table == MONO_TABLE_EXPORTEDTYPE)
			ADD_ERROR (ctx, g_strdup_printf ("ManifestResource table row %d has invalid Implementation token table %08x", i, get_coded_index_table (IMPLEMENTATION_DESC, data [MONO_MANIFEST_IMPLEMENTATION])));

		if (impl_table == MONO_TABLE_FILE && token && data [MONO_MANIFEST_OFFSET])
			ADD_ERROR (ctx, g_strdup_printf ("ManifestResource table row %d points to a file but has non-zero offset", i));

		if (!token && data [MONO_MANIFEST_OFFSET] >= resources_size)
			ADD_ERROR (ctx, g_strdup_printf ("ManifestResource table row %d invalid Offset field %08x ", i, data [MONO_MANIFEST_OFFSET]));
	}
}

static void
verify_nested_class_table (VerifyContext *ctx)
{
	MonoTableInfo *table = &ctx->image->tables [MONO_TABLE_NESTEDCLASS];
	guint32 data [MONO_NESTED_CLASS_SIZE];
	int i;

	for (i = 0; i < table->rows; ++i) {
		mono_metadata_decode_row (table, i, data, MONO_NESTED_CLASS_SIZE);

		if (!data [MONO_NESTED_CLASS_NESTED] || data [MONO_NESTED_CLASS_NESTED] > ctx->image->tables [MONO_TABLE_TYPEDEF].rows)
			ADD_ERROR (ctx, g_strdup_printf ("NestedClass table row %d has invalid NestedClass token %08x", i, data [MONO_NESTED_CLASS_NESTED]));
		if (!data [MONO_NESTED_CLASS_ENCLOSING] || data [MONO_NESTED_CLASS_ENCLOSING] > ctx->image->tables [MONO_TABLE_TYPEDEF].rows)
			ADD_ERROR (ctx, g_strdup_printf ("NestedClass table row %d has invalid EnclosingClass token %08x", i, data [MONO_NESTED_CLASS_ENCLOSING]));
		if (data [MONO_NESTED_CLASS_ENCLOSING] == data [MONO_NESTED_CLASS_NESTED])
			ADD_ERROR (ctx, g_strdup_printf ("NestedClass table row %d has same token for NestedClass  and EnclosingClass %08x", i, data [MONO_NESTED_CLASS_ENCLOSING]));
	}
}

#define INVALID_GENERIC_PARAM_FLAGS_BITS ~((1 << 0) | (1 << 1) | (1 << 2) | (1 << 3) | (1 << 4))
static void
verify_generic_param_table (VerifyContext *ctx)
{
	MonoTableInfo *table = &ctx->image->tables [MONO_TABLE_GENERICPARAM];
	guint32 data [MONO_GENERICPARAM_SIZE], token, last_token = 0;
	int i, param_number = 0;

	for (i = 0; i < table->rows; ++i) {
		mono_metadata_decode_row (table, i, data, MONO_GENERICPARAM_SIZE);

		if (data [MONO_GENERICPARAM_FLAGS] & INVALID_GENERIC_PARAM_FLAGS_BITS)
			ADD_ERROR (ctx, g_strdup_printf ("GenericParam table row %d has invalid Flags token %08x", i, data [MONO_GENERICPARAM_FLAGS]));

		if ((data [MONO_GENERICPARAM_FLAGS] & MONO_GEN_PARAM_VARIANCE_MASK) == 0x3)
			ADD_ERROR (ctx, g_strdup_printf ("GenericParam table row %d has invalid VarianceMask 0x3", i));

		if (!is_valid_non_empty_string (ctx, data [MONO_GENERICPARAM_NAME]))
			ADD_ERROR (ctx, g_strdup_printf ("GenericParam table row %d has invalid Name token %08x", i, data [MONO_GENERICPARAM_NAME]));

		token = data [MONO_GENERICPARAM_OWNER];

		if (!is_valid_coded_index (ctx, TYPE_OR_METHODDEF_DESC, token))
			ADD_ERROR (ctx, g_strdup_printf ("GenericParam table row %d has invalid Owner token %08x", i, token));

		if (!get_coded_index_token (TYPE_OR_METHODDEF_DESC, token))
			ADD_ERROR (ctx, g_strdup_printf ("GenericParam table row %d has null Owner token", i));

		if (token != last_token) {
			param_number = 0;
			last_token = token;
		}

		if (data [MONO_GENERICPARAM_NUMBER] != param_number)
			ADD_ERROR (ctx, g_strdup_printf ("GenericParam table row %d Number is out of order %d expected %d", i, data [MONO_GENERICPARAM_NUMBER], param_number));

		++param_number;
	}
}

static void
verify_method_spec_table (VerifyContext *ctx)
{
	MonoTableInfo *table = &ctx->image->tables [MONO_TABLE_METHODSPEC];
	guint32 data [MONO_METHODSPEC_SIZE];
	int i;

	for (i = 0; i < table->rows; ++i) {
		mono_metadata_decode_row (table, i, data, MONO_METHODSPEC_SIZE);

		if (!is_valid_coded_index (ctx, METHODDEF_OR_REF_DESC, data [MONO_METHODSPEC_METHOD]))
			ADD_ERROR (ctx, g_strdup_printf ("MethodSpec table row %d has invalid Method token %08x", i, data [MONO_METHODSPEC_METHOD]));

		if (!get_coded_index_token (METHODDEF_OR_REF_DESC, data [MONO_METHODSPEC_METHOD]))
			ADD_ERROR (ctx, g_strdup_printf ("MethodSpec table row %d has null Method token", i));

		if (data [MONO_METHODSPEC_SIGNATURE] && !is_valid_blob_object (ctx, data [MONO_METHODSPEC_SIGNATURE], 1))
			ADD_ERROR (ctx, g_strdup_printf ("MethodSpec table row %d has invalid signature token %08x", i, data [MONO_METHODSPEC_SIGNATURE]));
	}
}

static void
verify_method_spec_table_full (VerifyContext *ctx)
{
	MonoTableInfo *table = &ctx->image->tables [MONO_TABLE_METHODSPEC];
	guint32 data [MONO_METHODSPEC_SIZE];
	int i;

	for (i = 0; i < table->rows; ++i) {
		mono_metadata_decode_row (table, i, data, MONO_METHODSPEC_SIZE);

		if (!is_valid_methodspec_blob (ctx, data [MONO_METHODSPEC_SIGNATURE]))
			ADD_ERROR (ctx, g_strdup_printf ("MethodSpec table row %d has invalid Instantiation token %08x", i, data [MONO_METHODSPEC_SIGNATURE]));
	}
}

static void
verify_generic_param_constraint_table (VerifyContext *ctx)
{
	MonoTableInfo *table = &ctx->image->tables [MONO_TABLE_GENERICPARAMCONSTRAINT];
	guint32 data [MONO_GENPARCONSTRAINT_SIZE];
	int i;
	guint32 last_owner = 0, last_constraint = 0;

	for (i = 0; i < table->rows; ++i) {
		mono_metadata_decode_row (table, i, data, MONO_GENPARCONSTRAINT_SIZE);

		if (!data [MONO_GENPARCONSTRAINT_GENERICPAR] || data [MONO_GENPARCONSTRAINT_GENERICPAR] > ctx->image->tables [MONO_TABLE_GENERICPARAM].rows)
			ADD_ERROR (ctx, g_strdup_printf ("GenericParamConstraint table row %d has invalid Owner token %08x", i, data [MONO_GENPARCONSTRAINT_GENERICPAR]));

		if (!is_valid_coded_index (ctx, TYPEDEF_OR_REF_DESC, data [MONO_GENPARCONSTRAINT_CONSTRAINT]))
			ADD_ERROR (ctx, g_strdup_printf ("GenericParamConstraint table row %d has invalid Constraint token %08x", i, data [MONO_GENPARCONSTRAINT_CONSTRAINT]));

		if (!get_coded_index_token (TYPEDEF_OR_REF_DESC, data [MONO_GENPARCONSTRAINT_CONSTRAINT]))
			ADD_ERROR (ctx, g_strdup_printf ("GenericParamConstraint table row %d has null Constraint token", i));

		if (last_owner > data [MONO_GENPARCONSTRAINT_GENERICPAR])
			ADD_ERROR (ctx, g_strdup_printf ("GenericParamConstraint table row %d is not properly sorted. Previous value of the owner column is 0x%08x current value is 0x%08x", i, last_owner, data [MONO_GENPARCONSTRAINT_GENERICPAR]));

		if (last_owner == data [MONO_GENPARCONSTRAINT_GENERICPAR]) {
			if (last_constraint == data [MONO_GENPARCONSTRAINT_CONSTRAINT])
				ADD_ERROR (ctx, g_strdup_printf ("GenericParamConstraint table row %d has duplicate constraint 0x%08x", i, last_constraint));
		} else {
			last_owner = data [MONO_GENPARCONSTRAINT_GENERICPAR];
		}
		last_constraint = data [MONO_GENPARCONSTRAINT_CONSTRAINT];
	}
}


typedef struct {
	const char *name;
	const char *name_space;
	guint32 resolution_scope;
} TypeDefUniqueId;

static guint
typedef_hash (gconstpointer _key)
{
	const TypeDefUniqueId *key = (const TypeDefUniqueId *)_key;
	return g_str_hash (key->name) ^ g_str_hash (key->name_space) ^ key->resolution_scope; /*XXX better salt the int key*/
}

static gboolean
typedef_equals (gconstpointer _a, gconstpointer _b)
{
	const TypeDefUniqueId *a = (const TypeDefUniqueId *)_a;
	const TypeDefUniqueId *b = (const TypeDefUniqueId *)_b;
	return !strcmp (a->name, b->name) && !strcmp (a->name_space, b->name_space) && a->resolution_scope == b->resolution_scope;
}

static void
verify_typedef_table_global_constraints (VerifyContext *ctx)
{
	int i;
	guint32 data [MONO_TYPEDEF_SIZE];
	guint32 nested_data [MONO_NESTED_CLASS_SIZE];
	MonoTableInfo *table = &ctx->image->tables [MONO_TABLE_TYPEDEF];
	MonoTableInfo *nested_table = &ctx->image->tables [MONO_TABLE_NESTEDCLASS];
	GHashTable *unique_types = g_hash_table_new_full (&typedef_hash, &typedef_equals, g_free, NULL);

	for (i = 0; i < table->rows; ++i) {
		guint visibility;
		TypeDefUniqueId *type = g_new (TypeDefUniqueId, 1);
		mono_metadata_decode_row (table, i, data, MONO_TYPEDEF_SIZE);

		type->name = mono_metadata_string_heap (ctx->image, data [MONO_TYPEDEF_NAME]);
		type->name_space = mono_metadata_string_heap (ctx->image, data [MONO_TYPEDEF_NAMESPACE]);
		type->resolution_scope = 0;

		visibility = data [MONO_TYPEDEF_FLAGS] & TYPE_ATTRIBUTE_VISIBILITY_MASK;
		if (visibility >= TYPE_ATTRIBUTE_NESTED_PUBLIC && visibility <= TYPE_ATTRIBUTE_NESTED_FAM_OR_ASSEM) {
			int res = search_sorted_table (ctx, MONO_TABLE_NESTEDCLASS, MONO_NESTED_CLASS_NESTED, i + 1);
			g_assert (res >= 0);

			mono_metadata_decode_row (nested_table, res, nested_data, MONO_NESTED_CLASS_SIZE);
			type->resolution_scope = nested_data [MONO_NESTED_CLASS_ENCLOSING];
		}

		if (g_hash_table_lookup (unique_types, type)) {
			ADD_ERROR_NO_RETURN (ctx, g_strdup_printf ("TypeDef table row %d has duplicate for tuple (%s,%s,%x)", i, type->name, type->name_space, type->resolution_scope));
			g_hash_table_destroy (unique_types);
			g_free (type);
			return;
		}
		g_hash_table_insert (unique_types, type, GUINT_TO_POINTER (1));
	}

	g_hash_table_destroy (unique_types);
}

static void
verify_typeref_table_global_constraints (VerifyContext *ctx)
{
	int i;
	guint32 data [MONO_TYPEREF_SIZE];
	MonoTableInfo *table = &ctx->image->tables [MONO_TABLE_TYPEREF];
	GHashTable *unique_types = g_hash_table_new_full (&typedef_hash, &typedef_equals, g_free, NULL);

	for (i = 0; i < table->rows; ++i) {
		TypeDefUniqueId *type = g_new (TypeDefUniqueId, 1);
		mono_metadata_decode_row (table, i, data, MONO_TYPEREF_SIZE);

		type->resolution_scope = data [MONO_TYPEREF_SCOPE];
		type->name = mono_metadata_string_heap (ctx->image, data [MONO_TYPEREF_NAME]);
		type->name_space = mono_metadata_string_heap (ctx->image, data [MONO_TYPEREF_NAMESPACE]);

		if (g_hash_table_lookup (unique_types, type)) {
			ADD_ERROR_NO_RETURN (ctx, g_strdup_printf ("TypeRef table row %d has duplicate for tuple (%s,%s,%x)", i, type->name, type->name_space, type->resolution_scope));
			g_hash_table_destroy (unique_types);
			g_free (type);
			return;
		}
		g_hash_table_insert (unique_types, type, GUINT_TO_POINTER (1));
	}

	g_hash_table_destroy (unique_types);
}

typedef struct {
	guint32 klass;
	guint32 method_declaration;
} MethodImplUniqueId;

static guint
methodimpl_hash (gconstpointer _key)
{
	const MethodImplUniqueId *key = (const MethodImplUniqueId *)_key;
	return key->klass ^ key->method_declaration;
}

static gboolean
methodimpl_equals (gconstpointer _a, gconstpointer _b)
{
	const MethodImplUniqueId *a = (const MethodImplUniqueId *)_a;
	const MethodImplUniqueId *b = (const MethodImplUniqueId *)_b;
	return a->klass == b->klass && a->method_declaration == b->method_declaration;
}

static void
verify_methodimpl_table_global_constraints (VerifyContext *ctx)
{
	int i;
	guint32 data [MONO_METHODIMPL_SIZE];
	MonoTableInfo *table = &ctx->image->tables [MONO_TABLE_METHODIMPL];
	GHashTable *unique_impls = g_hash_table_new_full (&methodimpl_hash, &methodimpl_equals, g_free, NULL);

	for (i = 0; i < table->rows; ++i) {
		MethodImplUniqueId *impl = g_new (MethodImplUniqueId, 1);
		mono_metadata_decode_row (table, i, data, MONO_METHODIMPL_SIZE);

		impl->klass = data [MONO_METHODIMPL_CLASS];
		impl->method_declaration = data [MONO_METHODIMPL_DECLARATION];

		if (g_hash_table_lookup (unique_impls, impl)) {
			ADD_ERROR_NO_RETURN (ctx, g_strdup_printf ("MethodImpl table row %d has duplicate for tuple (0x%x, 0x%x)", impl->klass, impl->method_declaration));
			g_hash_table_destroy (unique_impls);
			g_free (impl);
			return;
		}
		g_hash_table_insert (unique_impls, impl, GUINT_TO_POINTER (1));
	}

	g_hash_table_destroy (unique_impls);
}


static void
verify_tables_data_global_constraints (VerifyContext *ctx)
{
	verify_typedef_table_global_constraints (ctx);
}

static void
verify_tables_data_global_constraints_full (VerifyContext *ctx)
{
	verify_typeref_table (ctx);
	verify_typeref_table_global_constraints (ctx);
	verify_methodimpl_table_global_constraints (ctx);
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
	/*Obfuscators love to place broken stuff in the typeref table
	verify_typeref_table (ctx);
	CHECK_ERROR ();*/
	verify_typedef_table (ctx);
	CHECK_ERROR ();
	verify_field_table (ctx);
	CHECK_ERROR ();
	verify_method_table (ctx);
	CHECK_ERROR ();
	verify_param_table (ctx);
	CHECK_ERROR ();
	verify_interfaceimpl_table (ctx);
	CHECK_ERROR ();
	verify_memberref_table (ctx);
	CHECK_ERROR ();
	verify_constant_table (ctx);
	CHECK_ERROR ();
	verify_cattr_table (ctx);
	CHECK_ERROR ();
	verify_field_marshal_table (ctx);
	CHECK_ERROR ();
	verify_decl_security_table (ctx);
	CHECK_ERROR ();
	verify_class_layout_table (ctx);
	CHECK_ERROR ();
	verify_field_layout_table (ctx);
	CHECK_ERROR ();
	verify_standalonesig_table (ctx);
	CHECK_ERROR ();
	verify_eventmap_table (ctx);
	CHECK_ERROR ();
	verify_event_table (ctx);
	CHECK_ERROR ();
	verify_propertymap_table (ctx);
	CHECK_ERROR ();
	verify_property_table (ctx);
	CHECK_ERROR ();
	verify_methodimpl_table (ctx);
	CHECK_ERROR ();
	verify_moduleref_table (ctx);
	CHECK_ERROR ();
	verify_typespec_table (ctx);
	CHECK_ERROR ();
	verify_implmap_table (ctx);
	CHECK_ERROR ();
	verify_fieldrva_table (ctx);
	CHECK_ERROR ();
	verify_assembly_table (ctx);
	CHECK_ERROR ();
	verify_assemblyref_table (ctx);
	CHECK_ERROR ();
	verify_file_table (ctx);
	CHECK_ERROR ();
	verify_exportedtype_table (ctx);
	CHECK_ERROR ();
	verify_manifest_resource_table (ctx);
	CHECK_ERROR ();
	verify_nested_class_table (ctx);
	CHECK_ERROR ();
	verify_generic_param_table (ctx);
	CHECK_ERROR ();
	verify_method_spec_table (ctx);
	CHECK_ERROR ();
	verify_generic_param_constraint_table (ctx);
	CHECK_ERROR ();
	verify_tables_data_global_constraints (ctx);
}

static void
init_verify_context (VerifyContext *ctx, MonoImage *image, gboolean report_error)
{
	memset (ctx, 0, sizeof (VerifyContext));
	ctx->image = image;
	ctx->report_error = report_error;
	ctx->report_warning = FALSE; //export this setting in the API
	ctx->valid = 1;
	ctx->size = image->raw_data_len;
	ctx->data = image->raw_data;
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

static gboolean
cleanup_context_checked (VerifyContext *ctx, MonoError *error)
{
	g_free (ctx->sections);
	if (ctx->errors) {
		MonoVerifyInfo *info = (MonoVerifyInfo *)ctx->errors->data;
		mono_error_set_bad_image (error, ctx->image, "%s", info->message);
		mono_free_verify_list (ctx->errors);
	}
	return ctx->valid;
}

gboolean
mono_verifier_verify_pe_data (MonoImage *image, GSList **error_list)
{
	VerifyContext ctx;

	if (!mono_verifier_is_enabled_for_image (image))
		return TRUE;

	init_verify_context (&ctx, image, error_list != NULL);
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

	init_verify_context (&ctx, image, error_list != NULL);
	ctx.stage = STAGE_CLI;

	verify_cli_header (&ctx);
	CHECK_STATE();
	verify_metadata_header (&ctx);
	CHECK_STATE();
	verify_tables_schema (&ctx);

cleanup:
	return cleanup_context (&ctx, error_list);
}


/*
 * Verifies basic table constraints such as global table invariants (sorting, field monotonicity, etc).
 * Other verification checks are meant to be done lazily by the runtime. Those include:
 * 	blob items (signatures, method headers, custom attributes, etc)
 *  type semantics related
 *  vtable related
 *  stuff that should not block other pieces from running such as bad types/methods/fields/etc.
 * 
 * The whole idea is that if this succeed the runtime is free to play around safely but any complex
 * operation still need more checking.
 */
gboolean
mono_verifier_verify_table_data (MonoImage *image, GSList **error_list)
{
	VerifyContext ctx;

	if (!mono_verifier_is_enabled_for_image (image))
		return TRUE;

	init_verify_context (&ctx, image, error_list != NULL);
	ctx.stage = STAGE_TABLES;

	verify_tables_data (&ctx);

	return cleanup_context (&ctx, error_list);
}


/*
 * Verifies all other constraints.
 */
gboolean
mono_verifier_verify_full_table_data (MonoImage *image, GSList **error_list)
{
	VerifyContext ctx;

	if (!mono_verifier_is_enabled_for_image (image))
		return TRUE;

	init_verify_context (&ctx, image, error_list != NULL);
	ctx.stage = STAGE_TABLES;

	verify_typedef_table_full (&ctx);
	CHECK_STATE ();
	verify_field_table_full (&ctx);
	CHECK_STATE ();
	verify_method_table_full (&ctx);
	CHECK_STATE ();
	verify_memberref_table_full (&ctx);
	CHECK_STATE ();
	verify_cattr_table_full (&ctx);
	CHECK_STATE ();
	verify_field_marshal_table_full (&ctx);
	CHECK_STATE ();
	verify_decl_security_table_full (&ctx);
	CHECK_STATE ();
	verify_standalonesig_table_full (&ctx);
	CHECK_STATE ();
	verify_event_table_full (&ctx);
	CHECK_STATE ();
	verify_typespec_table_full (&ctx);
	CHECK_STATE ();
	verify_method_spec_table_full (&ctx);
	CHECK_STATE ();
	verify_tables_data_global_constraints_full (&ctx);

cleanup:
	return cleanup_context (&ctx, error_list);
}

gboolean
mono_verifier_verify_field_signature (MonoImage *image, guint32 offset, GSList **error_list)
{
	VerifyContext ctx;

	if (!mono_verifier_is_enabled_for_image (image))
		return TRUE;

	init_verify_context (&ctx, image, error_list != NULL);
	ctx.stage = STAGE_TABLES;

	is_valid_field_signature (&ctx, offset);
	return cleanup_context (&ctx, error_list);
}

gboolean
mono_verifier_verify_method_header (MonoImage *image, guint32 offset, GSList **error_list)
{
	VerifyContext ctx;
	guint32 locals_token;

	if (!mono_verifier_is_enabled_for_image (image))
		return TRUE;

	init_verify_context (&ctx, image, error_list != NULL);
	ctx.stage = STAGE_TABLES;

	is_valid_method_header (&ctx, offset, &locals_token);
	if (locals_token) {
		guint32 sig_offset = mono_metadata_decode_row_col (&image->tables [MONO_TABLE_STANDALONESIG], locals_token - 1, MONO_STAND_ALONE_SIGNATURE);
		is_valid_standalonesig_blob (&ctx, sig_offset);
	}

	return cleanup_context (&ctx, error_list);
}

gboolean
mono_verifier_verify_method_signature (MonoImage *image, guint32 offset, MonoError *error)
{
	VerifyContext ctx;

	error_init (error);

	if (!mono_verifier_is_enabled_for_image (image))
		return TRUE;

	init_verify_context (&ctx, image, TRUE);
	ctx.stage = STAGE_TABLES;

	is_valid_method_signature (&ctx, offset);
	/*XXX This returns a bad image exception, it might be the case that the right exception is method load.*/
	return cleanup_context_checked (&ctx, error);
}

gboolean
mono_verifier_verify_memberref_method_signature (MonoImage *image, guint32 offset, GSList **error_list)
{
	VerifyContext ctx;

	if (!mono_verifier_is_enabled_for_image (image))
		return TRUE;

	init_verify_context (&ctx, image, error_list != NULL);
	ctx.stage = STAGE_TABLES;

	is_valid_memberref_method_signature (&ctx, offset);
	return cleanup_context (&ctx, error_list);
}

gboolean
mono_verifier_verify_memberref_field_signature (MonoImage *image, guint32 offset, GSList **error_list)
{
	VerifyContext ctx;

	if (!mono_verifier_is_enabled_for_image (image))
		return TRUE;

	init_verify_context (&ctx, image, error_list != NULL);
	ctx.stage = STAGE_TABLES;

	is_valid_field_signature (&ctx, offset);
	return cleanup_context (&ctx, error_list);
}

gboolean
mono_verifier_verify_standalone_signature (MonoImage *image, guint32 offset, GSList **error_list)
{
	VerifyContext ctx;

	if (!mono_verifier_is_enabled_for_image (image))
		return TRUE;

	init_verify_context (&ctx, image, error_list != NULL);
	ctx.stage = STAGE_TABLES;

	is_valid_standalonesig_blob (&ctx, offset);
	return cleanup_context (&ctx, error_list);
}

gboolean
mono_verifier_verify_typespec_signature (MonoImage *image, guint32 offset, guint32 token, GSList **error_list)
{
	VerifyContext ctx;

	if (!mono_verifier_is_enabled_for_image (image))
		return TRUE;

	init_verify_context (&ctx, image, error_list != NULL);
	ctx.stage = STAGE_TABLES;
	ctx.token = token;

	is_valid_typespec_blob (&ctx, offset);
	return cleanup_context (&ctx, error_list);
}

gboolean
mono_verifier_verify_methodspec_signature (MonoImage *image, guint32 offset, GSList **error_list)
{
	VerifyContext ctx;

	if (!mono_verifier_is_enabled_for_image (image))
		return TRUE;

	init_verify_context (&ctx, image, error_list != NULL);
	ctx.stage = STAGE_TABLES;

	is_valid_methodspec_blob (&ctx, offset);
	return cleanup_context (&ctx, error_list);
}

static void
verify_user_string (VerifyContext *ctx, guint32 offset)
{
	OffsetAndSize heap_us = get_metadata_stream (ctx, &ctx->image->heap_us);
	guint32 entry_size, bytes;

	if (heap_us.size < offset)
		ADD_ERROR (ctx, g_strdup ("User string offset beyond heap_us size"));

	if (!decode_value (ctx->data + offset + heap_us.offset, heap_us.size - heap_us.offset, &entry_size, &bytes))
		ADD_ERROR (ctx, g_strdup ("Could not decode user string blob size"));

	if (CHECK_ADD4_OVERFLOW_UN (entry_size, bytes))
		ADD_ERROR (ctx, g_strdup ("User string size overflow"));

	entry_size += bytes;

	if (ADD_IS_GREATER_OR_OVF (offset, entry_size, heap_us.size))
		ADD_ERROR (ctx, g_strdup ("User string oveflow heap_us"));
}

gboolean
mono_verifier_verify_string_signature (MonoImage *image, guint32 offset, GSList **error_list)
{
	VerifyContext ctx;

	if (!mono_verifier_is_enabled_for_image (image))
		return TRUE;

	init_verify_context (&ctx, image, error_list != NULL);
	ctx.stage = STAGE_TABLES;

	verify_user_string (&ctx, offset);

	return cleanup_context (&ctx, error_list);
}

gboolean
mono_verifier_verify_cattr_blob (MonoImage *image, guint32 offset, GSList **error_list)
{
	VerifyContext ctx;

	if (!mono_verifier_is_enabled_for_image (image))
		return TRUE;

	init_verify_context (&ctx, image, error_list != NULL);
	ctx.stage = STAGE_TABLES;

	is_valid_cattr_blob (&ctx, offset);

	return cleanup_context (&ctx, error_list);
}

gboolean
mono_verifier_verify_cattr_content (MonoImage *image, MonoMethod *ctor, const guchar *data, guint32 size, GSList **error_list)
{
	VerifyContext ctx;

	if (!mono_verifier_is_enabled_for_image (image))
		return TRUE;

	init_verify_context (&ctx, image, error_list != NULL);
	ctx.stage = STAGE_TABLES;

	is_valid_cattr_content (&ctx, ctor, (const char*)data, size);

	return cleanup_context (&ctx, error_list);
}

gboolean
mono_verifier_is_sig_compatible (MonoImage *image, MonoMethod *method, MonoMethodSignature *signature)
{
	MonoMethodSignature *original_sig;
	if (!mono_verifier_is_enabled_for_image (image))
		return TRUE;

	original_sig = mono_method_signature (method);
	if (original_sig->call_convention == MONO_CALL_VARARG) {
		if (original_sig->hasthis != signature->hasthis)
			return FALSE;
		if (original_sig->call_convention != signature->call_convention)
			return FALSE;
		if (original_sig->explicit_this != signature->explicit_this)
			return FALSE;
		if (original_sig->pinvoke != signature->pinvoke)
			return FALSE;
		if (original_sig->sentinelpos != signature->sentinelpos)
			return FALSE;
	} else if (!mono_metadata_signature_equal (signature, original_sig)) {
		return FALSE;
	}

	return TRUE;
}

gboolean
mono_verifier_verify_typeref_row (MonoImage *image, guint32 row, MonoError *error)
{
	MonoTableInfo *table = &image->tables [MONO_TABLE_TYPEREF];
	guint32 data [MONO_TYPEREF_SIZE];

	error_init (error);

	if (!mono_verifier_is_enabled_for_image (image))
		return TRUE;

	if (row >= table->rows) {
		mono_error_set_bad_image (error, image, "Invalid typeref row %d - table has %d rows", row, table->rows);
		return FALSE;
	}

	mono_metadata_decode_row (table, row, data, MONO_TYPEREF_SIZE);
	if (!is_valid_coded_index_with_image (image, RES_SCOPE_DESC, data [MONO_TYPEREF_SCOPE])) {
		mono_error_set_bad_image (error, image, "Invalid typeref row %d coded index 0x%08x", row, data [MONO_TYPEREF_SCOPE]);
		return FALSE;
	}

	if (!get_coded_index_token (RES_SCOPE_DESC, data [MONO_TYPEREF_SCOPE])) {
		mono_error_set_bad_image (error, image, "The metadata verifier doesn't support null ResolutionScope tokens for typeref row %d", row);
		return FALSE;
	}

	if (!data [MONO_TYPEREF_NAME] || !is_valid_string_full_with_image (image, data [MONO_TYPEREF_NAME], FALSE)) {
		mono_error_set_bad_image (error, image, "Invalid typeref row %d name token 0x%08x", row, data [MONO_TYPEREF_NAME]);
		return FALSE;
	}

	if (data [MONO_TYPEREF_NAMESPACE] && !is_valid_string_full_with_image (image, data [MONO_TYPEREF_NAMESPACE], FALSE)) {
		mono_error_set_bad_image (error, image, "Invalid typeref row %d namespace token 0x%08x", row, data [MONO_TYPEREF_NAMESPACE]);
		return FALSE;
	}

	return TRUE;
}

/*Perform additional verification including metadata ones*/
gboolean
mono_verifier_verify_methodimpl_row (MonoImage *image, guint32 row, MonoError *error)
{
	MonoMethod *declaration, *body;
	MonoMethodSignature *body_sig, *decl_sig;
	MonoTableInfo *table = &image->tables [MONO_TABLE_METHODIMPL];
	guint32 data [MONO_METHODIMPL_SIZE];

	error_init (error);

	if (!mono_verifier_is_enabled_for_image (image))
		return TRUE;

	if (row >= table->rows) {
		mono_error_set_bad_image (error, image, "Invalid methodimpl row %d - table has %d rows", row, table->rows);
		return FALSE;
	}

	mono_metadata_decode_row (table, row, data, MONO_METHODIMPL_SIZE);

	body = method_from_method_def_or_ref (image, data [MONO_METHODIMPL_BODY], NULL, error);
	if (!body)
		return FALSE;

	declaration = method_from_method_def_or_ref (image, data [MONO_METHODIMPL_DECLARATION], NULL, error);
	if (!declaration)
		return FALSE;

	/* FIXME
	mono_class_setup_supertypes (class);
	if (!mono_class_has_parent (class, body->klass)) {
		mono_error_set_bad_image (error, image, "Invalid methodimpl body doesn't belong to parent for row %x", row);
		return FALSE;
	}*/

	if (!(body_sig = mono_method_signature_checked (body, error))) {
		return FALSE;
	}

	if (!(decl_sig = mono_method_signature_checked (declaration, error))) {
		return FALSE;
	}

	if (!mono_verifier_is_signature_compatible (decl_sig, body_sig)) {
		mono_error_set_bad_image (error, image, "Invalid methodimpl body signature not compatible with declaration row %x", row);
		return FALSE;
	}

	return TRUE;
}

#else
gboolean
mono_verifier_verify_table_data (MonoImage *image, GSList **error_list)
{
	return TRUE;
}

gboolean
mono_verifier_verify_cli_data (MonoImage *image, GSList **error_list)
{
	return TRUE;
}

gboolean
mono_verifier_verify_pe_data (MonoImage *image, GSList **error_list)
{
	return TRUE;
}

gboolean
mono_verifier_verify_full_table_data (MonoImage *image, GSList **error_list)
{
	return TRUE;
}

gboolean
mono_verifier_verify_field_signature (MonoImage *image, guint32 offset, GSList **error_list)
{
	return TRUE;
}

gboolean
mono_verifier_verify_method_header (MonoImage *image, guint32 offset, GSList **error_list)
{
	return TRUE;
}

gboolean
mono_verifier_verify_method_signature (MonoImage *image, guint32 offset, MonoError *error)
{
	error_init (error);
	return TRUE;
}

gboolean
mono_verifier_verify_standalone_signature (MonoImage *image, guint32 offset, GSList **error_list)
{
	return TRUE;
}

gboolean
mono_verifier_verify_typespec_signature (MonoImage *image, guint32 offset, guint32 token, GSList **error_list)
{
	return TRUE;
}

gboolean
mono_verifier_verify_methodspec_signature (MonoImage *image, guint32 offset, GSList **error_list)
{
	return TRUE;
}

gboolean
mono_verifier_verify_string_signature (MonoImage *image, guint32 offset, GSList **error_list)
{
	return TRUE;
}

gboolean
mono_verifier_verify_cattr_blob (MonoImage *image, guint32 offset, GSList **error_list)
{
	return TRUE;
}

gboolean
mono_verifier_verify_cattr_content (MonoImage *image, MonoMethod *ctor, const guchar *data, guint32 size, GSList **error_list)
{
	return TRUE;
}

gboolean
mono_verifier_is_sig_compatible (MonoImage *image, MonoMethod *method, MonoMethodSignature *signature)
{
	return TRUE;
}


gboolean
mono_verifier_verify_typeref_row (MonoImage *image, guint32 row, MonoError *error)
{
	error_init (error);
	return TRUE;
}

gboolean
mono_verifier_verify_methodimpl_row (MonoImage *image, guint32 row, MonoError *error)
{
	error_init (error);
	return TRUE;
}

gboolean
mono_verifier_verify_memberref_method_signature (MonoImage *image, guint32 offset, GSList **error_list)
{
	return TRUE;
}

gboolean
mono_verifier_verify_memberref_field_signature (MonoImage *image, guint32 offset, GSList **error_list)
{
	return TRUE;
}

#endif /* DISABLE_VERIFIER */
