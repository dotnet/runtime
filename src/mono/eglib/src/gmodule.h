#ifndef __GLIB_GMODULE_H
#define __GLIB_GMODULE_H

#include <glib.h>

/*
 * Modules
 */
typedef enum {
	G_MODULE_BIND_LAZY = 0x01,
	G_MODULE_BIND_LOCAL = 0x02,
	G_MODULE_BIND_MASK = 0x03
} GModuleFlags;
typedef struct _GModule GModule;

GModule *g_module_open (const gchar *file, GModuleFlags flags);
gboolean g_module_symbol (GModule *module, const gchar *symbol_name,
			  gpointer *symbol);
const gchar *g_module_error (void);
gboolean g_module_close (GModule *module);
gchar *  g_module_build_path (const gchar *directory, const gchar *module_name);

#endif
