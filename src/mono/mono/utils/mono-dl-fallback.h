#ifndef __MONO_UTILS_DL_FALLBACK_H__
#define __MONO_UTILS_DL_FALLBACK_H__

#include <mono/utils/mono-publib.h>

MONO_BEGIN_DECLS

enum {
	MONO_DL_LAZY  = 1,
	MONO_DL_LOCAL = 2,
	MONO_DL_MASK  = 3
};

/*
 * This is the dynamic loader fallback API
 */
typedef struct MonoDlFallbackHandler MonoDlFallbackHandler;

/*
 * The "err" variable contents must be allocated using g_malloc or g_strdup
 */
typedef void* (*MonoDlFallbackLoad) (const char *name, int flags, char **err, void *user_data);
typedef void* (*MonoDlFallbackSymbol) (void *handle, const char *name, char **err, void *user_data);
typedef void* (*MonoDlFallbackClose) (void *handle, void *user_data);

MONO_API MonoDlFallbackHandler *mono_dl_fallback_register (MonoDlFallbackLoad load_func, MonoDlFallbackSymbol symbol_func,
						  MonoDlFallbackClose close_func, void *user_data);

MONO_API void                   mono_dl_fallback_unregister (MonoDlFallbackHandler *handler);

MONO_END_DECLS

#endif /* __MONO_UTILS_DL_FALLBACK_H__ */

