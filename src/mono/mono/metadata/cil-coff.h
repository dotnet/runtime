
#ifndef __MONO_CIL_COFF_H__
#define __MONO_CIL_COFF_H__

#include <mono/metadata/metadata.h>

/*
 * 25.2.1: Method header type values
 */
#define METHOD_HEADER_FORMAT_MASK   7
#define METHOD_HEADER_TINY_FORMAT   2
#define METHOD_HEADER_TINY_FORMAT1  6
#define METHOD_HEADER_FAT_FORMAT    3

/*
 * 25.2.3.1: Flags for method headers
 */
#define METHOD_HEADER_INIT_LOCALS   0x10
#define METHOD_HEADER_MORE_SECTS    0x08

/*
 * For section data (25.3)
 */
#define METHOD_HEADER_SECTION_RESERVED    0
#define METHOD_HEADER_SECTION_EHTABLE     1
#define METHOD_HEADER_SECTION_OPTIL_TABLE 2
#define METHOD_HEADER_SECTION_FAT_FORMAT  0x40
#define METHOD_HEADER_SECTION_MORE_SECTS  0x80

/* 128 bytes */
typedef struct {
	char    msdos_header [60];
	guint32 pe_offset;
	char    msdos_header2 [64];
} MonoMSDOSHeader;

/* 20 bytes */
typedef struct {
	guint16  coff_machine;
	guint16  coff_sections;
	guint32  coff_time;
	guint32  coff_symptr;
	guint32  coff_symcount;
	guint16  coff_opt_header_size;
	guint16  coff_attributes;
} MonoCOFFHeader;

#define COFF_ATTRIBUTE_EXECUTABLE_IMAGE 0x0002
#define COFF_ATTRIBUTE_LIBRARY_IMAGE    0x2000

/* 28 bytes */
typedef struct {
	guint16 pe_magic;
	guchar  pe_major;
	guchar  pe_minor;
	guint32 pe_code_size;
	guint32 pe_data_size;
	guint32 pe_uninit_data_size;
	guint32 pe_rva_entry_point;
	guint32 pe_rva_code_base;
	guint32 pe_rva_data_base;
} MonoPEHeader;

/* 68 bytes */
typedef struct {
	guint32 pe_image_base;		/* must be 0x400000 */
	guint32 pe_section_align;       /* must be 8192 */
	guint32 pe_file_alignment;      /* must be 512 or 4096 */
	guint16 pe_os_major;            /* must be 4 */
	guint16 pe_os_minor;            /* must be 0 */
	guint16 pe_user_major;
	guint16 pe_user_minor;
	guint16 pe_subsys_major;
	guint16 pe_subsys_minor;
	guint32 pe_reserved_1;
	guint32 pe_image_size;
	guint32 pe_header_size;
	guint32 pe_checksum;
	guint16 pe_subsys_required;
	guint16 pe_dll_flags;
	guint32 pe_stack_reserve;
	guint32 pe_stack_commit;
	guint32 pe_heap_reserve;
	guint32 pe_heap_commit;
	guint32 pe_loader_flags;
	guint32 pe_data_dir_count;
} MonoPEHeaderNT;

typedef struct {
	guint32 rva;
	guint32 size;
} MonoPEDirEntry;

/* 128 bytes */
typedef struct {
	MonoPEDirEntry pe_export_table;
	MonoPEDirEntry pe_import_table;
	MonoPEDirEntry pe_resource_table;
	MonoPEDirEntry pe_exception_table;
	MonoPEDirEntry pe_certificate_table;
	MonoPEDirEntry pe_reloc_table;
	MonoPEDirEntry pe_debug;
	MonoPEDirEntry pe_copyright;
	MonoPEDirEntry pe_global_ptr;
	MonoPEDirEntry pe_tls_table;
	MonoPEDirEntry pe_load_config_table;
	MonoPEDirEntry pe_bound_import;
	MonoPEDirEntry pe_iat;
	MonoPEDirEntry pe_delay_import_desc;
	MonoPEDirEntry pe_cli_header;
	MonoPEDirEntry pe_reserved;
} MonoPEDatadir;

/* 248 bytes */
typedef struct {
	char            pesig [4];
	MonoCOFFHeader  coff;
	MonoPEHeader    pe;
	MonoPEHeaderNT  nt;
	MonoPEDatadir   datadir;
} MonoDotNetHeader;

typedef struct {
	char    st_name [8];
	guint32 st_virtual_size;
	guint32 st_virtual_address;
	guint32 st_raw_data_size;
	guint32 st_raw_data_ptr;
	guint32 st_reloc_ptr;
	guint32 st_lineno_ptr;
	guint16 st_reloc_count;
	guint16 st_line_count;

#define SECT_FLAGS_HAS_CODE               0x20
#define SECT_FLAGS_HAS_INITIALIZED_DATA   0x40
#define SECT_FLAGS_HAS_UNINITIALIZED_DATA 0x80
#define SECT_FLAGS_MEM_DISCARDABLE        0x02000000
#define SECT_FLAGS_MEM_NOT_CACHED         0x04000000
#define SECT_FLAGS_MEM_NOT_PAGED          0x08000000
#define SECT_FLAGS_MEM_SHARED             0x10000000
#define SECT_FLAGS_MEM_EXECUTE            0x20000000
#define SECT_FLAGS_MEM_READ               0x40000000
#define SECT_FLAGS_MEM_WRITE              0x80000000
	guint32 st_flags;

} MonoSectionTable;

typedef struct {
	guint32        ch_size;
	guint16        ch_runtime_major;
	guint16        ch_runtime_minor;
	MonoPEDirEntry ch_metadata;

#define CLI_FLAGS_ILONLY         0x01
#define CLI_FLAGS_32BITREQUIRED  0x02
#define CLI_FLAGS_TRACKDEBUGDATA 0x00010000
	guint32        ch_flags;

	guint32        ch_entry_point;
	MonoPEDirEntry ch_resources;
	MonoPEDirEntry ch_strong_name;
	MonoPEDirEntry ch_code_manager_table;
	MonoPEDirEntry ch_vtable_fixups;
	MonoPEDirEntry ch_export_address_table_jumps;

	/* The following are zero in the current docs */
	MonoPEDirEntry ch_eeinfo_table;
	MonoPEDirEntry ch_helper_table;
	MonoPEDirEntry ch_dynamic_info;
	MonoPEDirEntry ch_delay_load_info;
	MonoPEDirEntry ch_module_image;
	MonoPEDirEntry ch_external_fixups;
	MonoPEDirEntry ch_ridmap;
	MonoPEDirEntry ch_debug_map;
	MonoPEDirEntry ch_ip_map;
} MonoCLIHeader;

/* This is not an on-disk structure */
typedef struct {
	MonoDotNetHeader  cli_header;
	int               cli_section_count;
	MonoSectionTable  *cli_section_tables;
	void            **cli_sections;
	MonoCLIHeader     cli_cli_header;
} MonoCLIImageInfo;

guint32       mono_cli_rva_image_map (MonoCLIImageInfo *iinfo, guint32 rva);
char         *mono_cli_rva_map       (MonoCLIImageInfo *iinfo, guint32 rva);

#endif /* __MONO_CIL_COFF_H__ */
