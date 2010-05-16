/*
 * This header is only installed for use by the debugger:
 * the structures and the API declared here are not supported.
 */

#ifndef __MONO_DEBUG_MONO_SYMFILE_H__
#define __MONO_DEBUG_MONO_SYMFILE_H__

#include <glib.h>
#include <mono/metadata/class.h>
#include <mono/metadata/reflection.h>
#include <mono/metadata/mono-debug.h>

typedef struct MonoSymbolFileOffsetTable	MonoSymbolFileOffsetTable;
typedef struct MonoSymbolFileLineNumberEntry	MonoSymbolFileLineNumberEntry;
typedef struct MonoSymbolFileMethodAddress	MonoSymbolFileMethodAddress;
typedef struct MonoSymbolFileDynamicTable	MonoSymbolFileDynamicTable;
typedef struct MonoSymbolFileSourceEntry	MonoSymbolFileSourceEntry;
typedef struct MonoSymbolFileMethodEntry	MonoSymbolFileMethodEntry;

/* Keep in sync with OffsetTable in mcs/class/Mono.CSharp.Debugger/MonoSymbolTable.cs */
struct MonoSymbolFileOffsetTable {
	guint32 _total_file_size;
	guint32 _data_section_offset;
	guint32 _data_section_size;
	guint32 _compile_unit_count;
	guint32 _compile_unit_table_offset;
	guint32 _compile_unit_table_size;
	guint32 _source_count;
	guint32 _source_table_offset;
	guint32 _source_table_size;
	guint32 _method_count;
	guint32 _method_table_offset;
	guint32 _method_table_size;
	guint32 _type_count;
	guint32 _anonymous_scope_count;
	guint32 _anonymous_scope_table_offset;
	guint32 _anonymous_scope_table_size;
	guint32 _line_number_table_line_base;
	guint32 _line_number_table_line_range;
	guint32 _line_number_table_opcode_base;
	guint32 _is_aspx_source;
};

struct MonoSymbolFileSourceEntry {
	guint32 _index;
	guint32 _data_offset;
};

struct MonoSymbolFileMethodEntry {
	guint32 _token;
	guint32 _data_offset;
	guint32 _line_number_table;
};

struct MonoSymbolFileMethodAddress {
	guint32 size;
	const guint8 *start_address;
	const guint8 *end_address;
	const guint8 *method_start_address;
	const guint8 *method_end_address;
	const guint8 *wrapper_address;
	guint32 has_this;
	guint32 num_params;
	guint32 variable_table_offset;
	guint32 type_table_offset;
	guint32 num_line_numbers;
	guint32 line_number_offset;
	guint8 data [MONO_ZERO_LEN_ARRAY];
};

struct _MonoDebugMethodInfo {
	MonoMethod *method;
	MonoDebugHandle *handle;
	guint32 index;
	guint32 data_offset;
	guint32 lnt_offset;
};

struct _MonoDebugLineNumberEntry {
	guint32 il_offset;
	guint32 native_offset;
};

struct _MonoSymbolFile {
	const guint8 *raw_contents;
	int raw_contents_size;
	void *raw_contents_handle;
	int major_version;
	int minor_version;
	gchar *filename;
	GHashTable *method_hash;
	MonoSymbolFileOffsetTable *offset_table;
};

#define MONO_SYMBOL_FILE_MAJOR_VERSION		50
#define MONO_SYMBOL_FILE_MINOR_VERSION		0
#define MONO_SYMBOL_FILE_MAGIC			0x45e82623fd7fa614ULL

G_BEGIN_DECLS

MonoSymbolFile *
mono_debug_open_mono_symbols       (MonoDebugHandle          *handle,
				    const guint8             *raw_contents,
				    int                       size,
				    gboolean                  in_the_debugger);

void
mono_debug_close_mono_symbol_file  (MonoSymbolFile           *symfile);

MonoDebugSourceLocation *
mono_debug_symfile_lookup_location (MonoDebugMethodInfo      *minfo,
				    guint32                   offset);

void
mono_debug_symfile_free_location   (MonoDebugSourceLocation  *location);

gint32
_mono_debug_address_from_il_offset (MonoDebugMethodJitInfo   *jit,
				    guint32                   il_offset);

MonoDebugMethodInfo *
mono_debug_symfile_lookup_method   (MonoDebugHandle          *handle,
				    MonoMethod               *method);

int
mono_debug_symfile_lookup_locals (MonoDebugMethodInfo *minfo, char ***names, 
								  int **indexes);

void
mono_debug_symfile_get_line_numbers (MonoDebugMethodInfo *minfo, char **source_file, int *n_il_offsets, int **il_offsets, int **line_numbers);

G_END_DECLS

#endif /* __MONO_SYMFILE_H__ */

