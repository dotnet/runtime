#ifndef __MONO_JIT_DEBUG_PRIVATE_H__
#define __MONO_JIT_DEBUG_PRIVATE_H__

#include <mono/metadata/debug-symfile.h>

#include "debug.h"

typedef struct {
	gpointer address;
	guint32 line;
	int is_basic_block;
	int source_file;
} DebugLineNumberInfo;

typedef struct _AssemblyDebugInfo AssemblyDebugInfo;

typedef enum {
	MONO_DEBUG_FLAGS_NONE			= 0,
	// Don't run the assembler.
	MONO_DEBUG_FLAGS_DONT_ASSEMBLE		= (1 << 1),
	// Install the generated *.il files in the assembly dir.
	MONO_DEBUG_FLAGS_INSTALL_IL_FILES	= (1 << 2),
	// Don't update the *.il files.
	MONO_DEBUG_FLAGS_DONT_UPDATE_IL_FILES	= (1 << 3),
	// Don't create any new *.il files.
	MONO_DEBUG_FLAGS_DONT_CREATE_IL_FILES	= (1 << 4),
	// Don't fallback to normal dwarf2.
	MONO_DEBUG_FLAGS_DONT_FALLBACK		= (1 << 5),
	// Don't precompile image.
	MONO_DEBUG_FLAGS_DONT_PRECOMPILE	= (1 << 6),
	// Update symbol file on exit.
	MONO_DEBUG_FLAGS_UPDATE_ON_EXIT		= (1 << 7)
} MonoDebugFlags;

typedef struct {
	MonoDebugMethodInfo method_info;
	AssemblyDebugInfo *info;
	gchar *name;
	int source_file;
	guint32 method_number;
	guint32 start_line;
	guint32 first_line;
	guint32 last_line;
	GPtrArray *line_numbers;
} DebugMethodInfo;

struct _AssemblyDebugInfo {
	MonoDebugFormat format;
	MonoDebugHandle *handle;
	MonoDebugSymbolFile *symfile;
	char *name;
	char *ilfile;
	char *filename;
	char *objfile;
	int source_file;
	int total_lines;
	int *mlines;
	int *moffsets;
	int nmethods;
	MonoImage *image;
	gpointer _priv;
};

struct _MonoDebugHandle {
	MonoDebugHandle *next;
	MonoDebugFormat format;
	MonoDebugFlags flags;
	char *name;
	char *filename;
	char *objfile;
	char *producer_name;
	GHashTable *type_hash;
	GHashTable *methods;
	GPtrArray *source_files;
	int next_idx;
	int next_klass_idx;
	GList *info;
	FILE *f;
};

guint32        mono_debug_get_type                   (MonoDebugHandle* debug, MonoClass *klass);

void           mono_debug_open_assembly_dwarf2_plus  (AssemblyDebugInfo *info);

void           mono_debug_write_assembly_dwarf2_plus (AssemblyDebugInfo *info);

void           mono_debug_close_assembly_dwarf2_plus (AssemblyDebugInfo *info);

void           mono_debug_write_stabs                (MonoDebugHandle *debug);

void           mono_debug_write_dwarf2               (MonoDebugHandle *debug);

#endif /* __MONO_JIT_DEBUG_PRIVATE_H__ */
