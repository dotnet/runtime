/*
 * This header is only installed for use by the debugger:
 * the structures and the API declared here are not supported.
 */

#ifndef __MONO_DEBUG_H__
#define __MONO_DEBUG_H__

#include <glib.h>
#include <mono/metadata/image.h>
#include <mono/metadata/appdomain.h>

typedef struct _MonoSymbolFile			MonoSymbolFile;
typedef struct _MonoSymbolFilePriv		MonoSymbolFilePriv;

typedef struct _MonoDebugHandle			MonoDebugHandle;
typedef struct _MonoDebugHandlePriv		MonoDebugHandlePriv;
typedef struct _MonoDebugDomainData		MonoDebugDomainData;
typedef struct _MonoDebugDomainDataPriv		MonoDebugDomainDataPriv;

typedef struct _MonoDebugVarInfo		MonoDebugVarInfo;
typedef struct _MonoDebugMethodJitInfo		MonoDebugMethodJitInfo;

typedef enum {
	MONO_DEBUG_FORMAT_NONE,
	MONO_DEBUG_FORMAT_MONO,
	MONO_DEBUG_FORMAT_DEBUGGER
} MonoDebugFormat;

struct _MonoDebugHandle {
	const char *image_file;
	MonoImage *image;
	MonoSymbolFile *symfile;
	MonoDebugHandlePriv *_priv;
};

struct _MonoDebugMethodJitInfo {
	const guint8 *code_start;
	guint32 code_size;
	guint32 prologue_end;
	guint32 epilogue_begin;
	const guint8 *wrapper_addr;
	/* Array of MonoDebugLineNumberEntry */
	GArray *line_numbers;
	guint32 num_params;
	MonoDebugVarInfo *this_var;
	MonoDebugVarInfo *params;
	guint32 num_locals;
	MonoDebugVarInfo *locals;
};

struct _MonoDebugDomainData {
	guint32 domain_id;
	MonoDebugMethodJitInfo **jit;
	MonoDebugDomainDataPriv *_priv;
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

struct _MonoDebugVarInfo {
	guint32 index;
	guint32 offset;
	guint32 size;
	guint32 begin_scope;
	guint32 end_scope;
};

#define MONO_DEBUGGER_VERSION				47
#define MONO_DEBUGGER_MAGIC				0x7aff65af4253d427ULL

extern MonoDebugFormat mono_debug_format;
extern GHashTable *mono_debug_handles;

void mono_debug_init (MonoDebugFormat format);
void mono_debug_init_1 (MonoDomain *domain);
void mono_debug_init_2 (MonoAssembly *assembly);
void mono_debug_cleanup (void);
void mono_debug_add_wrapper (MonoMethod *method, gpointer wrapper, MonoDomain *domain);

void mono_debug_add_method (MonoMethod *method, MonoDebugMethodJitInfo *jit, MonoDomain *domain);
gchar *mono_debug_source_location_from_address (MonoMethod *method, guint32 address,
						guint32 *line_number, MonoDomain *domain);
gchar *mono_debug_source_location_from_il_offset (MonoMethod *method, guint32 offset,
						  guint32 *line_number);
gint32 mono_debug_il_offset_from_address (MonoMethod *method, gint32 address, MonoDomain *domain);
gint32 mono_debug_address_from_il_offset (MonoMethod *method, gint32 il_offset, MonoDomain *domain);

#endif /* __MONO_DEBUG_H__ */
