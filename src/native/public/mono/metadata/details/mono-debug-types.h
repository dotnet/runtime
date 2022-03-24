// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
#ifndef _MONO_MONO_DEBUG_TYPES_H
#define _MONO_MONO_DEBUG_TYPES_H

#include <mono/utils/details/mono-publib-types.h>
#include <mono/metadata/details/image-types.h>
#include <mono/metadata/details/appdomain-types.h>

MONO_BEGIN_DECLS

typedef struct _MonoSymbolTable			MonoSymbolTable;
typedef struct _MonoDebugDataTable		MonoDebugDataTable;

typedef struct _MonoSymbolFile			MonoSymbolFile;
typedef struct _MonoPPDBFile			MonoPPDBFile;

typedef struct _MonoDebugHandle			MonoDebugHandle;

typedef struct _MonoDebugLineNumberEntry	MonoDebugLineNumberEntry;

typedef struct _MonoDebugVarInfo		MonoDebugVarInfo;
typedef struct _MonoDebugMethodJitInfo		MonoDebugMethodJitInfo;
typedef struct _MonoDebugMethodAddress		MonoDebugMethodAddress;
typedef struct _MonoDebugMethodAddressList	MonoDebugMethodAddressList;
typedef struct _MonoDebugClassEntry		MonoDebugClassEntry;

typedef struct _MonoDebugMethodInfo		MonoDebugMethodInfo;
typedef struct _MonoDebugLocalsInfo		MonoDebugLocalsInfo;
typedef struct _MonoDebugMethodAsyncInfo	MonoDebugMethodAsyncInfo;
typedef struct _MonoDebugSourceLocation		MonoDebugSourceLocation;

typedef struct _MonoDebugList			MonoDebugList;

typedef enum {
	MONO_DEBUG_FORMAT_NONE,
	MONO_DEBUG_FORMAT_MONO,
	/* Deprecated, the mdb debugger is not longer supported. */
	MONO_DEBUG_FORMAT_DEBUGGER
} MonoDebugFormat;

/*
 * NOTE:
 * We intentionally do not use GList here since the debugger needs to know about
 * the layout of the fields.
*/
struct _MonoDebugList {
	MonoDebugList *next;
	const void* data;
};

struct _MonoSymbolTable {
	uint64_t magic;
	uint32_t version;
	uint32_t total_size;

	/*
	 * Corlib and metadata info.
	 */
	MonoDebugHandle *corlib;
	MonoDebugDataTable *global_data_table;
	MonoDebugList *data_tables;

	/*
	 * The symbol files.
	 */
	MonoDebugList *symbol_files;
};

struct _MonoDebugHandle {
	uint32_t index;
	char *image_file;
	MonoImage *image;
	MonoDebugDataTable *type_table;
	MonoSymbolFile *symfile;
	MonoPPDBFile *ppdb;
};

struct _MonoDebugMethodJitInfo {
	const mono_byte *code_start;
	uint32_t code_size;
	uint32_t prologue_end;
	uint32_t epilogue_begin;
	const mono_byte *wrapper_addr;
	uint32_t num_line_numbers;
	MonoDebugLineNumberEntry *line_numbers;
	uint32_t has_var_info;
	uint32_t num_params;
	MonoDebugVarInfo *this_var;
	MonoDebugVarInfo *params;
	uint32_t num_locals;
	MonoDebugVarInfo *locals;
	MonoDebugVarInfo *gsharedvt_info_var;
	MonoDebugVarInfo *gsharedvt_locals_var;
};

struct _MonoDebugMethodAddressList {
	uint32_t size;
	uint32_t count;
	mono_byte data [MONO_ZERO_LEN_ARRAY];
};

struct _MonoDebugSourceLocation {
	char *source_file;
	uint32_t row, column;
	uint32_t il_offset;
};

/*
 * These bits of the MonoDebugLocalInfo's "index" field are flags specifying
 * where the variable is actually stored.
 *
 * See relocate_variable() in debug-symfile.c for more info.
 */
#define MONO_DEBUG_VAR_ADDRESS_MODE_FLAGS		0xf0000000

/* The variable is in register "index". */
#define MONO_DEBUG_VAR_ADDRESS_MODE_REGISTER		0

/* The variable is at offset "offset" from register "index". */
#define MONO_DEBUG_VAR_ADDRESS_MODE_REGOFFSET		0x10000000

/* The variable is in the two registers "offset" and "index". */
#define MONO_DEBUG_VAR_ADDRESS_MODE_TWO_REGISTERS	0x20000000

/* The variable is dead. */
#define MONO_DEBUG_VAR_ADDRESS_MODE_DEAD		0x30000000

/* Same as REGOFFSET, but do an indirection */
#define MONO_DEBUG_VAR_ADDRESS_MODE_REGOFFSET_INDIR		0x40000000

/* gsharedvt local */
#define MONO_DEBUG_VAR_ADDRESS_MODE_GSHAREDVT_LOCAL		0x50000000

/* variable is a vt address */
#define MONO_DEBUG_VAR_ADDRESS_MODE_VTADDR		0x60000000

struct _MonoDebugVarInfo {
	uint32_t index;
	uint32_t offset;
	uint32_t size;
	uint32_t begin_scope;
	uint32_t end_scope;
	MonoType *type;
};

#define MONO_DEBUGGER_MAJOR_VERSION			81
#define MONO_DEBUGGER_MINOR_VERSION			6
#define MONO_DEBUGGER_MAGIC				0x7aff65af4253d427ULL

MONO_END_DECLS

#endif /* _MONO_MONO_DEBUG_TYPES_H */
