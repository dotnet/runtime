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

typedef struct {
	MonoDebugMethodInfo method_info;
	gchar *name;
	int source_file;
	guint32 method_number;
	guint32 start_line;
	guint32 first_line;
	guint32 frame_start_offset;
	GPtrArray *line_numbers;
} DebugMethodInfo;

struct _AssemblyDebugInfo {
	FILE *f;
	char *filename;
	char *name;
	char *producer_name;
	int total_lines;
	int *mlines;
	int *moffsets;
	int nmethods;
	int next_idx;
	MonoImage *image;
	GHashTable *methods;
	GHashTable *type_hash;
	int next_klass_idx;
	GPtrArray *source_files;
};

struct _MonoDebugHandle {
	char *name;
	MonoDebugFormat format;
	GList *info;
};

guint32        mono_debug_get_type                   (AssemblyDebugInfo* info, MonoClass *klass);

void           mono_debug_open_assembly_stabs        (AssemblyDebugInfo *info);

void           mono_debug_open_assembly_dwarf2       (AssemblyDebugInfo *info);

void           mono_debug_open_assembly_dwarf2_plus  (AssemblyDebugInfo *info);

void           mono_debug_write_assembly_stabs       (AssemblyDebugInfo *info);

void           mono_debug_write_assembly_dwarf2      (AssemblyDebugInfo *info);

void           mono_debug_write_assembly_dwarf2_plus (AssemblyDebugInfo *info);

void           mono_debug_close_assembly_stabs       (AssemblyDebugInfo *info);

void           mono_debug_close_assembly_dwarf2      (AssemblyDebugInfo *info);

void           mono_debug_close_assembly_dwarf2_plus (AssemblyDebugInfo *info);

#endif /* __MONO_JIT_DEBUG_PRIVATE_H__ */
