/**
 * \file
 */

#ifndef __MONO_CODEMAN_H__
#define __MONO_CODEMAN_H__

#include <mono/utils/mono-publib.h>

typedef struct _MonoCodeManager MonoCodeManager;

typedef struct {
	void (*chunk_new) (void *chunk, int size);
	void (*chunk_destroy) (void *chunk);
} MonoCodeManagerCallbacks;

MONO_API MonoCodeManager* mono_code_manager_new     (void);
MONO_API MonoCodeManager* mono_code_manager_new_dynamic (void);
MONO_API void             mono_code_manager_destroy (MonoCodeManager *cman);
MONO_API void             mono_code_manager_invalidate (MonoCodeManager *cman);
MONO_API void             mono_code_manager_set_read_only (MonoCodeManager *cman);

MONO_API void*            mono_code_manager_reserve_align (MonoCodeManager *cman, int size, int alignment);

MONO_API void*            mono_code_manager_reserve (MonoCodeManager *cman, int size);
MONO_API void             mono_code_manager_commit  (MonoCodeManager *cman, void *data, int size, int newsize);
MONO_API int              mono_code_manager_size    (MonoCodeManager *cman, int *used_size);
MONO_API void             mono_code_manager_init (void);
MONO_API void             mono_code_manager_cleanup (void);
MONO_API void             mono_code_manager_install_callbacks (MonoCodeManagerCallbacks* callbacks);

/* find the extra block allocated to resolve branches close to code */
typedef int    (*MonoCodeManagerFunc)      (void *data, int csize, int size, void *user_data);
void            mono_code_manager_foreach  (MonoCodeManager *cman, MonoCodeManagerFunc func, void *user_data);

#endif /* __MONO_CODEMAN_H__ */

