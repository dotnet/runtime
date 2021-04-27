/**
 * \file
 */

#ifndef __MONO_CODEMAN_H__
#define __MONO_CODEMAN_H__

#include <mono/utils/mono-publib.h>

typedef struct _MonoCodeManager MonoCodeManager;

#define MONO_CODE_MANAGER_CALLBACKS \
	MONO_CODE_MANAGER_CALLBACK (void, chunk_new, (void *chunk, int size)) \
	MONO_CODE_MANAGER_CALLBACK (void, chunk_destroy, (void *chunk)) \

typedef struct {

#undef MONO_CODE_MANAGER_CALLBACK
#define MONO_CODE_MANAGER_CALLBACK(ret, name, sig) ret (*name) sig;

    MONO_CODE_MANAGER_CALLBACKS

} MonoCodeManagerCallbacks;

MonoCodeManager* mono_code_manager_new     (void);
MonoCodeManager* mono_code_manager_new_dynamic (void);
void             mono_code_manager_destroy (MonoCodeManager *cman);
void             mono_code_manager_invalidate (MonoCodeManager *cman);
void             mono_code_manager_set_read_only (MonoCodeManager *cman);

void*            mono_code_manager_reserve_align (MonoCodeManager *cman, int size, int alignment);

void*            mono_code_manager_reserve (MonoCodeManager *cman, int size);
void             mono_code_manager_commit  (MonoCodeManager *cman, void *data, int size, int newsize);
int              mono_code_manager_size    (MonoCodeManager *cman, int *used_size);
void             mono_code_manager_init (void);
void             mono_code_manager_cleanup (void);
void             mono_code_manager_install_callbacks (const MonoCodeManagerCallbacks* callbacks);

/* find the extra block allocated to resolve branches close to code */
typedef int    (*MonoCodeManagerFunc)      (void *data, int csize, int size, void *user_data);
void            mono_code_manager_foreach  (MonoCodeManager *cman, MonoCodeManagerFunc func, void *user_data);

void mono_codeman_enable_write (void);
void mono_codeman_disable_write (void);

#endif /* __MONO_CODEMAN_H__ */

