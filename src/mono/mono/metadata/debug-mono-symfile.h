/**
 * \file
 * This header is only installed for use by the debugger:
 * the structures and the API declared here are not supported.
 * Copyright 2012 Xamarin Inc (http://www.xamarin.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#ifndef __MONO_DEBUG_MONO_SYMFILE_H__
#define __MONO_DEBUG_MONO_SYMFILE_H__

#include <glib.h>
#include <mono/metadata/class.h>
#include <mono/metadata/reflection.h>
#include <mono/metadata/mono-debug.h>
#include <mono/metadata/debug-internals.h>

typedef struct MonoSymbolFileOffsetTable	MonoSymbolFileOffsetTable;
typedef struct MonoSymbolFileLineNumberEntry	MonoSymbolFileLineNumberEntry;
typedef struct MonoSymbolFileMethodAddress	MonoSymbolFileMethodAddress;
typedef struct MonoSymbolFileDynamicTable	MonoSymbolFileDynamicTable;
typedef struct MonoSymbolFileSourceEntry	MonoSymbolFileSourceEntry;
typedef struct MonoSymbolFileMethodEntry	MonoSymbolFileMethodEntry;

/* Keep in sync with OffsetTable in mcs/class/Mono.CSharp.Debugger/MonoSymbolTable.cs */
struct MonoSymbolFileOffsetTable {
	uint32_t _total_file_size;
	uint32_t _data_section_offset;
	uint32_t _data_section_size;
	uint32_t _compile_unit_count;
	uint32_t _compile_unit_table_offset;
	uint32_t _compile_unit_table_size;
	uint32_t _source_count;
	uint32_t _source_table_offset;
	uint32_t _source_table_size;
	uint32_t _method_count;
	uint32_t _method_table_offset;
	uint32_t _method_table_size;
	uint32_t _type_count;
	uint32_t _anonymous_scope_count;
	uint32_t _anonymous_scope_table_offset;
	uint32_t _anonymous_scope_table_size;
	uint32_t _line_number_table_line_base;
	uint32_t _line_number_table_line_range;
	uint32_t _line_number_table_opcode_base;
	uint32_t _is_aspx_source;
};

struct MonoSymbolFileSourceEntry {
	uint32_t _index;
	uint32_t _data_offset;
};

struct MonoSymbolFileMethodEntry {
	uint32_t _token;
	uint32_t _data_offset;
	uint32_t _line_number_table;
};

struct MonoSymbolFileMethodAddress {
	uint32_t size;
	const uint8_t *start_address;
	const uint8_t *end_address;
	const uint8_t *method_start_address;
	const uint8_t *method_end_address;
	const uint8_t *wrapper_address;
	uint32_t has_this;
	uint32_t num_params;
	uint32_t variable_table_offset;
	uint32_t type_table_offset;
	uint32_t num_line_numbers;
	uint32_t line_number_offset;
	uint8_t data [MONO_ZERO_LEN_ARRAY];
};

#define MONO_SYMBOL_FILE_MAJOR_VERSION		50
#define MONO_SYMBOL_FILE_MINOR_VERSION		0
#define MONO_SYMBOL_FILE_MAGIC			0x45e82623fd7fa614ULL

MONO_BEGIN_DECLS

MONO_API MonoSymbolFile *
mono_debug_open_mono_symbols       (MonoDebugHandle          *handle,
				    const uint8_t            *raw_contents,
				    int                       size,
				    mono_bool                 in_the_debugger);

MONO_API void
mono_debug_close_mono_symbol_file  (MonoSymbolFile           *symfile);

MONO_API mono_bool
mono_debug_symfile_is_loaded       (MonoSymbolFile           *symfile);

MONO_API MonoDebugSourceLocation *
mono_debug_symfile_lookup_location (MonoDebugMethodInfo      *minfo,
				    uint32_t                  offset);

MONO_API void
mono_debug_symfile_free_location   (MonoDebugSourceLocation  *location);

MONO_API MonoDebugMethodInfo *
mono_debug_symfile_lookup_method   (MonoDebugHandle          *handle,
				    MonoMethod               *method);

MONO_API MonoDebugLocalsInfo*
mono_debug_symfile_lookup_locals (MonoDebugMethodInfo *minfo);

void
mono_debug_symfile_get_seq_points (MonoDebugMethodInfo *minfo, char **source_file, GPtrArray **source_file_list, int **source_files, MonoSymSeqPoint **seq_points, int *n_seq_points);

MONO_END_DECLS

#endif /* __MONO_SYMFILE_H__ */

