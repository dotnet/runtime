
#ifndef __MONO_CIL_COFF_H__
#define __MONO_CIL_COFF_H__

#include <mono/metadata/metadata.h>

/* A metadata token */
typedef guint32 mtoken_t;

typedef struct {
	char msdos_header [128];
} msdos_header_t;

typedef struct {
	guint16  coff_machine;
	guint16  coff_sections;
	guint32  coff_time;
	guint32  coff_symptr;
	guint32  coff_symcount;
	guint16  coff_opt_header_size;
	guint16  coff_attributes;
} coff_header_t;

#define COFF_ATTRIBUTE_EXECUTABLE_IMAGE 0x0002
#define COFF_ATTRIBUTE_LIBRARY_IMAGE    0x2000

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
} pe_header_t;

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
} pe_header_nt_t;

typedef struct {
	guint32 rva;
	guint32 size;
} pe_dir_entry_t;

typedef struct {
	pe_dir_entry_t pe_export_table;
	pe_dir_entry_t pe_import_table;
	pe_dir_entry_t pe_resource_table;
	pe_dir_entry_t pe_exception_table;
	pe_dir_entry_t pe_certificate_table;
	pe_dir_entry_t pe_reloc_table;
	pe_dir_entry_t pe_debug;
	pe_dir_entry_t pe_copyright;
	pe_dir_entry_t pe_global_ptr;
	pe_dir_entry_t pe_tls_table;
	pe_dir_entry_t pe_load_config_table;
	pe_dir_entry_t pe_bound_import;
	pe_dir_entry_t pe_iat;
	pe_dir_entry_t pe_delay_import_desc;
	pe_dir_entry_t pe_cli_header;
	pe_dir_entry_t pe_reserved;
} pe_datadir_t;

typedef struct {
	char            pesig [4];
	coff_header_t   coff;
	pe_header_t     pe;
	pe_header_nt_t  nt;
	pe_datadir_t    datadir;
} dotnet_header_t;

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

} section_table_t;

typedef struct {
	guint32        ch_size;
	guint16        ch_runtime_major;
	guint16        ch_runtime_minor;
	pe_dir_entry_t ch_metadata;

#define CLI_FLAGS_ILONLY         0x01
#define CLI_FLAGS_32BITREQUIRED  0x02
#define CLI_FLAGS_TRACKDEBUGDATA 0x00010000
	guint32        ch_flags;

	mtoken_t       ch_entry_point;
	pe_dir_entry_t ch_resources;
	pe_dir_entry_t ch_strong_name;
	pe_dir_entry_t ch_code_manager_table;
	pe_dir_entry_t ch_vtable_fixups;
	pe_dir_entry_t ch_export_address_table_jumps;

	/* The following are zero in the current docs */
	pe_dir_entry_t ch_eeinfo_table;
	pe_dir_entry_t ch_helper_table;
	pe_dir_entry_t ch_dynamic_info;
	pe_dir_entry_t ch_delay_load_info;
	pe_dir_entry_t ch_module_image;
	pe_dir_entry_t ch_external_fixups;
	pe_dir_entry_t ch_ridmap;
	pe_dir_entry_t ch_debug_map;
	pe_dir_entry_t ch_ip_map;
} cli_header_t;

/* This is not an on-disk structure */
typedef struct {
	dotnet_header_t   cli_header;
	int               cli_section_count;
	section_table_t  *cli_section_tables;
	void            **cli_sections;
	cli_header_t      cli_cli_header;

	metadata_t        cli_metadata;
} cli_image_info_t;

guint32       cli_rva_image_map (cli_image_info_t *iinfo, guint32 rva);
char         *cli_rva_map       (cli_image_info_t *iinfo, guint32 rva);

#endif /* __MONO_CIL_COFF_H__ */
