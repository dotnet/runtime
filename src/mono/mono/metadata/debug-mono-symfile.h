#ifndef __MONO_DEBUG_MONO_SYMFILE_H__
#define __MONO_DEBUG_MONO_SYMFILE_H__

#include <glib.h>
#include <mono/metadata/class.h>
#include <mono/metadata/reflection.h>
#include <mono/metadata/debug-symfile.h>

typedef struct MonoSymbolFile			MonoSymbolFile;
typedef struct MonoSymbolFilePriv		MonoSymbolFilePriv;
typedef struct MonoSymbolFileOffsetTable	MonoSymbolFileOffsetTable;
typedef struct MonoSymbolFileLineNumberEntry	MonoSymbolFileLineNumberEntry;
typedef struct MonoSymbolFileMethodEntry	MonoSymbolFileMethodEntry;
typedef struct MonoSymbolFileMethodAddress	MonoSymbolFileMethodAddress;

/* Keep in sync with OffsetTable in mcs/class/Mono.CSharp.Debugger/MonoSymbolTable.cs */
struct MonoSymbolFileOffsetTable {
	guint32 total_file_size;
	guint32 source_table_offset;
	guint32 source_table_size;
	guint32 method_count;
	guint32 method_table_offset;
	guint32 method_table_size;
	guint32 line_number_table_offset;
	guint32 line_number_table_size;
	guint32 address_table_size;
};

struct MonoSymbolFileMethodEntry {
	guint32 token;
	guint32 start_row;
	guint32 end_row;
	guint32 num_line_numbers;
	guint32 source_file_offset;
	guint32 line_number_table_offset;
	guint32 address_table_offset;
	guint32 address_table_size;
};

struct MonoSymbolFileMethodAddress {
	guint32 is_valid;
	guint64 start_address;
	guint64 end_address;
	guint64 trampoline_address;
	guint32 line_addresses [MONO_ZERO_LEN_ARRAY];
};

struct MonoSymbolFileLineNumberEntry {
	guint32 row;
	guint32 offset;
};

struct MonoSymbolFile {
	guint64 magic;
	guint32 version;
	guint32 is_dynamic;
	/* Pointer to the mmap()ed contents of the file. */
	char *raw_contents;
	guint32 raw_contents_size;
	/* Pointer to the malloced address table. */
	char *address_table;
	guint32 address_table_size;
	/* Private. */
	MonoSymbolFilePriv *_priv;
};

#define MONO_SYMBOL_FILE_VERSION		17
#define MONO_SYMBOL_FILE_MAGIC			0x45e82623fd7fa614

/* Tries to load `filename' as a debugging information file, if `emit_warnings" is set,
 * use g_warning() to signal error messages on failure, otherwise silently return NULL. */
typedef MonoDebugMethodInfo * (*MonoDebugGetMethodFunc) (MonoSymbolFile      *symbol_file,
							 MonoMethod          *method,
							 gpointer             user_data);

MonoSymbolFile *
mono_debug_open_mono_symbol_file   (MonoImage                 *image,
				    const char                *filename,
				    gboolean                   emit_warnings);

void
mono_debug_update_mono_symbol_file (MonoSymbolFile           *symbol_file,
				    GHashTable               *method_hash);

void
mono_debug_close_mono_symbol_file  (MonoSymbolFile           *symfile);

MonoSymbolFile *
mono_debug_create_mono_symbol_file (MonoImage                *image,
				    const char               *source_file);

gchar *
mono_debug_find_source_location    (MonoSymbolFile           *symfile,
				    MonoMethod               *method,
				    guint32                   offset,
				    guint32                  *line_number);

#endif /* __MONO_SYMFILE_H__ */

