#ifndef __MONO_JIT_DEBUG_PRIVATE_H__
#define __MONO_JIT_DEBUG_PRIVATE_H__

#include <mono/metadata/debug-mono-symfile.h>

#include "debug.h"

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
	AssemblyDebugInfo *info;
	gchar *name;
	int source_file;
	guint32 method_number;
	guint32 start_line;
	guint32 first_line;
	guint32 last_line;
} DebugMethodInfo;

struct _AssemblyDebugInfo {
	MonoDebugFormat format;
	MonoDebugHandle *handle;
	MonoSymbolFile *symfile;
	char *name;
	char *ilfile;
	char *filename;
	char *objfile;
	int always_create_il;
	int source_file;
	int total_lines;
	int *mlines;
	int *moffsets;
	int nmethods;
	GHashTable *methods;
	MonoImage *image;
	gpointer _priv;
};

struct _MonoDebugHandle {
	MonoDebugFormat format;
	MonoDebugFlags flags;
	char *name;
	char *filename;
	char *objfile;
	char *producer_name;
	GHashTable *type_hash;
	GPtrArray *source_files;
	int next_idx;
	int next_klass_idx;
	int dirty;
	GHashTable *images;
	FILE *f;
};

guint32        mono_debug_get_type                   (MonoDebugHandle* debug, MonoClass *klass);

void           mono_debug_write_stabs                (MonoDebugHandle *debug);

void           mono_debug_write_dwarf2               (MonoDebugHandle *debug);

#endif /* __MONO_JIT_DEBUG_PRIVATE_H__ */
