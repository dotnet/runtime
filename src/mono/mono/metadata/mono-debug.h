#ifndef __MONO_DEBUG_H__
#define __MONO_DEBUG_H__

#include <glib.h>
#include <mono/metadata/debug-mono-symfile.h>

typedef struct _MonoDebugHandle			MonoDebugHandle;
typedef struct _MonoDebugHandlePriv		MonoDebugHandlePriv;

typedef enum {
	MONO_DEBUG_FORMAT_NONE,
	MONO_DEBUG_FORMAT_MONO,
	MONO_DEBUG_FORMAT_DEBUGGER
} MonoDebugFormat;

struct _MonoDebugHandle {
	MonoImage *image;
	MonoSymbolFile *symfile;
	MonoDebugHandlePriv *_priv;
};

extern MonoDebugFormat mono_debug_format;
extern GHashTable *mono_debug_handles;

void mono_debug_init (MonoDebugFormat format);
void mono_debug_init_2 (MonoAssembly *assembly);
void mono_debug_cleanup (void);
void mono_debug_add_wrapper (MonoMethod *method, MonoMethod *wrapper_method);
void mono_debug_add_method (MonoMethod *method, MonoDebugMethodJitInfo *jit);
gchar *mono_debug_source_location_from_address (MonoMethod *method, guint32 address,
						guint32 *line_number);
gchar *mono_debug_source_location_from_il_offset (MonoMethod *method, guint32 offset,
						  guint32 *line_number);
gint32 mono_debug_il_offset_from_address (MonoMethod *method, gint32 address);
gint32 mono_debug_address_from_il_offset (MonoMethod *method, gint32 il_offset);

#endif /* __MONO_DEBUG_H__ */
