/**
 * \file
 */

#ifndef __MONO_CODEMAN_H__
#define __MONO_CODEMAN_H__

#include <mono/utils/mono-publib.h>

#if defined(__APPLE__) && defined(__arm64__)

#include <pthread.h>

typedef enum {
	JPM_NONE,
	JPM_ENABLED,
	JPM_DISABLED,
} jit_protect_mode;

extern __thread jit_protect_mode arm_current_jit_protect_mode;

static void mono_arm_jit_write_protect_enable()
{
	if (__builtin_available(macOS 11, *)) {
		if (arm_current_jit_protect_mode != JPM_ENABLED) {
			pthread_jit_write_protect_np(1);
			arm_current_jit_protect_mode = JPM_ENABLED;
		}
	}
}

static void mono_arm_jit_write_protect_disable()
{
	if (__builtin_available(macOS 11, *)) {
		if (arm_current_jit_protect_mode != JPM_DISABLED) {
			pthread_jit_write_protect_np(0);
			arm_current_jit_protect_mode = JPM_DISABLED;
		}
	}
}

#define MONO_SCOPE_ENABLE_JIT_WRITE()					\
	__attribute__((unused, cleanup(mono_arm_restore_jit_protect_mode))) \
	jit_protect_mode scope_restrict_mode = arm_current_jit_protect_mode; \
        mono_arm_jit_write_protect_disable();					\

#define MONO_SCOPE_ENABLE_JIT_EXEC()					\
	__attribute__((unused, cleanup(mono_arm_restore_jit_protect_mode))) \
	jit_protect_mode scope_restrict_mode = arm_current_jit_protect_mode; \
	mono_arm_jit_write_protect_enable();				\

static void mono_arm_restore_jit_protect_mode(jit_protect_mode* previous_jit_protect_mode)
{
	if (*previous_jit_protect_mode == arm_current_jit_protect_mode)
		return;

	switch (*previous_jit_protect_mode)
	{
	case JPM_ENABLED:
		mono_arm_jit_write_protect_enable();
		break;
	case JPM_DISABLED:
	case JPM_NONE:
	default:
		mono_arm_jit_write_protect_disable();
	}
}

#else
#define MONO_SCOPE_ENABLE_JIT_WRITE()
#define MONO_SCOPE_ENABLE_JIT_EXEC()

static void mono_arm_jit_write_protect_enable() {}
static void mono_arm_jit_write_protect_disable() {}
#endif

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

#endif /* __MONO_CODEMAN_H__ */

