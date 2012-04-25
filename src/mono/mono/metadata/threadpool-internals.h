#ifndef _MONO_THREADPOOL_INTERNALS_H_
#define _MONO_THREADPOOL_INTERNALS_H_

void mono_thread_pool_remove_socket (int sock) MONO_INTERNAL;
gboolean mono_thread_pool_is_queue_array (MonoArray *o) MONO_INTERNAL;
void mono_internal_thread_unhandled_exception (MonoObject* exc) MONO_INTERNAL;

#endif
