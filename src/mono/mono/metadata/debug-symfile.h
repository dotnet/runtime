#ifndef __MONO_DEBUG_SYMFILE_H__
#define __MONO_DEBUG_SYMFILE_H__

#include <glib.h>
#include <mono/metadata/class.h>

typedef struct MonoDebugSymbolFile		MonoDebugSymbolFile;
typedef struct MonoDebugSymbolFileSection	MonoDebugSymbolFileSection;
typedef struct MonoDebugMethodInfo		MonoDebugMethodInfo;
typedef struct MonoDebugILOffsetInfo		MonoDebugILOffsetInfo;

/* Machine dependent information about a method.
 *
 * This structure is created by the MonoDebugMethodInfoFunc callback of
 * mono_debug_update_symbol_file (). */
struct MonoDebugMethodInfo {
	MonoMethod *method;
	gpointer code_start;
	guint32 code_size;
	guint32 num_params;
	guint32 *param_offsets;
	guint32 num_locals;
	guint32 *local_offsets;
	guint32 num_il_offsets;
	MonoDebugILOffsetInfo *il_offsets;
	gpointer _priv;
};

struct MonoDebugILOffsetInfo {
	guint32 offset;
	guint32 address;
};

struct MonoDebugSymbolFile {
	int fd;
	char *file_name;
	MonoImage *image;
	/* Pointer to the mmap()ed contents of the file. */
	void *raw_contents;
	size_t raw_contents_size;
	/* Array of MONO_DEBUG_SYMBOL_SECTION_MAX elements. */
	MonoDebugSymbolFileSection *section_offsets;
	gpointer user_data;
};

struct MonoDebugSymbolFileSection {
	int type;
	gulong file_offset;
	gulong size;
};

#define MONO_DEBUG_SYMBOL_FILE_VERSION			5

/* Keep in sync with Mono.CSharp.Debugger.MonoDwarfFileWriter.Section */
#define MONO_DEBUG_SYMBOL_SECTION_DEBUG_INFO		0x01
#define MONO_DEBUG_SYMBOL_SECTION_DEBUG_ABBREV		0x02
#define MONO_DEBUG_SYMBOL_SECTION_DEBUG_LINE		0x03
#define MONO_DEBUG_SYMBOL_SECTION_MONO_RELOC_TABLE	0x04

#define MONO_DEBUG_SYMBOL_SECTION_MAX			0x05

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

#endif /* __MONO_DEBUG_SYMFILE_H__ */

