#ifndef __MONO_DEBUG_MONO_SYMFILE_H__
#define __MONO_DEBUG_MONO_SYMFILE_H__

#include <glib.h>
#include <mono/metadata/class.h>
#include <mono/metadata/reflection.h>

typedef struct MonoSymbolFile			MonoSymbolFile;
typedef struct MonoSymbolFileOffsetTable	MonoSymbolFileOffsetTable;
typedef struct MonoSymbolFileLineNumberEntry	MonoSymbolFileLineNumberEntry;
typedef struct MonoSymbolFileMethodEntry	MonoSymbolFileMethodEntry;

/* Keep in sync with OffsetTable in mcs/class/Mono.CSharp.Debugger/MonoSymbolTable.cs */
struct MonoSymbolFileOffsetTable {
	guint32 source_table_offset;
	guint32 source_table_size;
	guint32 method_table_offset;
	guint32 method_table_size;
	guint32 line_number_table_offset;
	guint32 line_number_table_size;
};

struct MonoSymbolFileMethodEntry {
	guint32 token;
	guint32 source_file_offset;
	guint32 line_number_table_offset;
	guint32 start_row;
	guint64 address;
};

struct MonoSymbolFileLineNumberEntry {
	guint32 row;
	guint32 offset;
	guint32 address;
};

struct MonoSymbolFile {
	int fd;
	char *file_name;
	MonoImage *image;
	/* Pointer to the mmap()ed contents of the file. */
	char *raw_contents;
	size_t raw_contents_size;
	MonoSymbolFileOffsetTable *offset_table;
	GHashTable *method_table;
};

#define MONO_SYMBOL_FILE_VERSION		16
#define MONO_SYMBOL_FILE_MAGIC			0x45e82623fd7fa614

/* Tries to load `filename' as a debugging information file, if `emit_warnings" is set,
 * use g_warning() to signal error messages on failure, otherwise silently return NULL. */
MonoSymbolFile *mono_debug_open_mono_symbol_file   (MonoImage                 *image,
						    const char                *filename,
						    gboolean                   emit_warnings);

void mono_debug_close_mono_symbol_file (MonoSymbolFile *symfile);

gchar *
mono_debug_find_source_location (MonoSymbolFile *symfile, MonoMethod *method, guint32 offset,
				 guint32 *line_number);

#endif /* __MONO_SYMFILE_H__ */

