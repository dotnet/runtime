/**
 * \file
 */

#ifndef __MONO_MINI_INTERPRETER_H__
#define __MONO_MINI_INTERPRETER_H__
#include <mono/mini/mini.h>

int
mono_interp_regression_list (int verbose, int count, char *images []);

void
mono_interp_init (void);

gpointer
mono_interp_create_method_pointer (MonoMethod *method, MonoError *error);

MonoObject*
mono_interp_runtime_invoke (MonoMethod *method, void *obj, void **params, MonoObject **exc, MonoError *error);

void
mono_interp_init_delegate (MonoDelegate *del);

gpointer
mono_interp_create_trampoline (MonoDomain *domain, MonoMethod *method, MonoError *error);

void
mono_interp_parse_options (const char *options);

void
interp_walk_stack_with_ctx (MonoInternalStackWalk func, MonoContext *ctx, MonoUnwindOptions options, void *user_data);
#endif /* __MONO_MINI_INTERPRETER_H__ */
