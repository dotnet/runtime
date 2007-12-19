#ifndef __MONO_DBG_H__
#define __MONO_DBG_H__

#include <glib.h>

G_BEGIN_DECLS

typedef gboolean (*MonoDbgMemoryAccess) (gconstpointer address, void *buffer, guint32 size);

guint32
mono_dbg_get_version (void);

typedef struct {
	gconstpointer container_class;
	gconstpointer generic_inst;
	gconstpointer klass;
} MonoDbgGenericClass;

typedef struct {
	guint32 id;
	guint32 type_argc;
	gconstpointer *type_argv;
} MonoDbgGenericInst;

gboolean
mono_dbg_read_generic_class (MonoDbgMemoryAccess memory, gconstpointer address,
			     MonoDbgGenericClass *result);

gboolean
mono_dbg_read_generic_inst (MonoDbgMemoryAccess memory, gconstpointer address,
			    MonoDbgGenericInst *result);

G_END_DECLS

#endif

