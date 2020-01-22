#ifndef __GLIB_GMODULE_H
#define __GLIB_GMODULE_H

#include <glib.h>

#define G_MODULE_IMPORT extern
#ifdef G_OS_WIN32
#define G_MODULE_EXPORT __declspec(dllexport)
#else
#define G_MODULE_EXPORT
#endif

G_BEGIN_DECLS

/*
 * Modules
 */
typedef enum {
	G_MODULE_BIND_LAZY = 0x01,
	G_MODULE_BIND_LOCAL = 0x02,
	G_MODULE_BIND_MASK = 0x03
} GModuleFlags;

G_ENUM_FUNCTIONS (GModuleFlags)

typedef struct _GModule GModule;

G_EXTERN_C // Used by libtest, at least.
GModule *g_module_open (const gchar *file, GModuleFlags flags);
G_EXTERN_C // Used by libtest, at least.
gboolean g_module_symbol (GModule *module, const gchar *symbol_name,
			  gpointer *symbol);
/* Caller must provide a suitable buffer. */
gboolean g_module_address (void *addr, char *file_name, size_t file_name_len,
                           void **file_base, char *sym_name,
                           size_t sym_name_len, void **sym_addr);
const gchar *g_module_error (void);
gboolean g_module_close (GModule *module);
gchar *  g_module_build_path (const gchar *directory, const gchar *module_name);

extern char *gmodule_libprefix;
extern char *gmodule_libsuffix;

G_END_DECLS

#endif
