#ifndef __MONO_DEBUG_SYMFILE_H__
#define __MONO_DEBUG_SYMFILE_H__

#include <glib.h>
#include <mono/metadata/class.h>
#include <mono/metadata/reflection.h>

typedef struct MonoDebugSymbolFile		MonoDebugSymbolFile;
typedef struct MonoDebugSymbolFileSection	MonoDebugSymbolFileSection;
typedef struct MonoDebugMethodInfo		MonoDebugMethodInfo;
typedef struct MonoDebugVarInfo			MonoDebugVarInfo;
typedef struct MonoDebugILOffsetInfo		MonoDebugILOffsetInfo;
typedef struct MonoDebugLineNumberBlock		MonoDebugLineNumberBlock;

/* Machine dependent information about a method.
 *
 * This structure is created by the MonoDebugMethodInfoFunc callback of
 * mono_debug_update_symbol_file (). */
struct MonoDebugMethodInfo {
	MonoMethod *method;
	char *code_start;
	guint32 code_size;
	guint32 num_params;
	MonoDebugVarInfo *this_var;
	MonoDebugVarInfo *params;
	guint32 num_locals;
	MonoDebugVarInfo *locals;
	guint32 num_il_offsets;
	MonoDebugILOffsetInfo *il_offsets;
	guint32 prologue_end;
	guint32 epilogue_begin;
	gpointer _priv;
};

/*
 * These bits of the MonoDebugLocalInfo's "index" field are flags specifying
 * where the variable is actually stored.
 *
 * See relocate_variable() in debug-symfile.c for more info.
 */
#define MONO_DEBUG_VAR_ADDRESS_MODE_FLAGS		0xf0000000

/* If "index" is zero, the variable is at stack offset "offset". */
#define MONO_DEBUG_VAR_ADDRESS_MODE_STACK		0

/* The variable is in the register whose number is contained in bits 0..4 of the
 * "index" field plus an offset of "offset" (which can be zero).
 */
#define MONO_DEBUG_VAR_ADDRESS_MODE_REGISTER		0x10000000

/* The variables in in the two registers whose numbers are contained in bits 0..4
 * and 5..9 of the "index" field plus an offset of "offset" (which can be zero).
 */
#define MONO_DEBUG_VAR_ADDRESS_MODE_TWO_REGISTERS	0x20000000

struct MonoDebugVarInfo {
	guint32 index;
	guint32 offset;
	guint32 begin_scope;
	guint32 end_scope;
};

struct MonoDebugILOffsetInfo {
	guint32 offset;
	guint32 address;
};

struct MonoDebugLineNumberBlock {
	guint32 token;
	const char *source_file;
	guint32 start_line;
	guint32 file_offset;
};

struct MonoDebugSymbolFile {
	int fd;
	char *file_name;
	MonoImage *image;
	/* Pointer to the mmap()ed contents of the file. */
	char *raw_contents;
	size_t raw_contents_size;
	/* Array of MONO_DEBUG_SYMBOL_SECTION_MAX elements. */
	MonoDebugSymbolFileSection *section_offsets;
	GHashTable *line_number_table;
	guint32 num_types;
	MonoClass **type_table;
	gpointer user_data;
};

struct MonoDebugSymbolFileSection {
	int type;
	gulong file_offset;
	gulong size;
};

#define MONO_DEBUG_SYMBOL_FILE_VERSION			15

/* Keep in sync with Mono.CSharp.Debugger.MonoDwarfFileWriter.Section */
#define MONO_DEBUG_SYMBOL_SECTION_DEBUG_INFO		0x01
#define MONO_DEBUG_SYMBOL_SECTION_DEBUG_ABBREV		0x02
#define MONO_DEBUG_SYMBOL_SECTION_DEBUG_LINE		0x03
#define MONO_DEBUG_SYMBOL_SECTION_MONO_RELOC_TABLE	0x04
#define MONO_DEBUG_SYMBOL_SECTION_MONO_LINE_NUMBERS	0x05
#define MONO_DEBUG_SYMBOL_SECTION_MONO_SYMBOL_TABLE	0x06

#define MONO_DEBUG_SYMBOL_SECTION_MAX			0x07

/* Tries to load `filename' as a debugging information file, if `emit_warnings" is set,
 * use g_warning() to signal error messages on failure, otherwise silently return NULL. */
MonoDebugSymbolFile *mono_debug_open_symbol_file   (MonoImage                 *image,
						    const char                *filename,
						    gboolean                   emit_warnings);

/* This callback function needs to return a MonoDebugMethodInfo structure containing the
 * machine-dependent information about method associated with the metadata_token.
 * It's highly recommended to cache the generated data since this function can be called
 * several times per method. */
typedef MonoDebugMethodInfo * (*MonoDebugMethodInfoFunc) (MonoDebugSymbolFile *symbol_file,
							  guint32              metadata_token,
							  gpointer             user_data);

void    mono_debug_update_symbol_file (MonoDebugSymbolFile      *symbol_file,
				       MonoDebugMethodInfoFunc   method_info_func,
				       gpointer                  user_data);

void    mono_debug_close_symbol_file  (MonoDebugSymbolFile      *symbol_file);

gchar *
mono_debug_find_source_location (MonoDebugSymbolFile *symfile, MonoMethod *method, guint32 offset,
				 guint32 *line_number);


#endif /* __MONO_DEBUG_SYMFILE_H__ */

