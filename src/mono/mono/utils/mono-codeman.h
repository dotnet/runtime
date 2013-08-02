#ifndef __MONO_CODEMAN_H__
#define __MONO_CODEMAN_H__

#include <mono/utils/mono-publib.h>

typedef struct _MonoCodeManager MonoCodeManager;

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

/* find the extra block allocated to resolve branches close to code */
typedef int    (*MonoCodeManagerFunc)      (void *data, int csize, int size, void *user_data);
void            mono_code_manager_foreach  (MonoCodeManager *cman, MonoCodeManagerFunc func, void *user_data);

#if defined( __native_client_codegen__ ) && defined( __native_client__ )

#ifdef __arm__
#define kNaClBundleSize 16
#else
#define kNaClBundleSize 32
#endif
#define kNaClBundleMask (kNaClBundleSize-1)

#ifndef USE_JUMP_TABLES
extern __thread unsigned char **patch_source_base;
extern __thread unsigned char **patch_dest_base;
extern __thread int patch_current_depth;
#endif

int              nacl_is_code_address             (void *target);
void*            nacl_code_manager_get_code_dest  (MonoCodeManager *cman, void *data);
void             nacl_allow_target_modification   (int val);
void*            nacl_modify_patch_target         (unsigned char *target);
void*            nacl_inverse_modify_patch_target (unsigned char *target);
#endif /* __native_client__ */

#endif /* __MONO_CODEMAN_H__ */

