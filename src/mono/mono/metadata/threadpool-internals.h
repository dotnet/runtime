#ifndef _MONO_THREADPOOL_INTERNALS_H_
#define _MONO_THREADPOOL_INTERNALS_H_

#include <glib.h>
#include <mono/metadata/object.h>
#include <mono/metadata/mono-hash.h>
#include <mono/metadata/mono-mlist.h>
#include <mono/utils/mono-compiler.h>
#include <mono/utils/mono-mutex.h>

typedef struct {
	mono_mutex_t io_lock; /* access to sock_to_state */
	int inited; // 0 -> not initialized , 1->initializing, 2->initialized, 3->cleaned up
	MonoGHashTable *sock_to_state;

	gint event_system;
	gpointer event_data;
	void (*modify) (gpointer p, int fd, int operation, int events, gboolean is_new);
	void (*wait) (gpointer sock_data);
	void (*shutdown) (gpointer event_data);
} SocketIOData;

void mono_thread_pool_remove_socket (int sock);
gboolean mono_thread_pool_is_queue_array (MonoArray *o);
void mono_internal_thread_unhandled_exception (MonoObject* exc);

//TP implementations
gpointer tp_poll_init (SocketIOData *data);

//TP internals the impls use
void check_for_interruption_critical (void);
void socket_io_cleanup (SocketIOData *data);
MonoObject *get_io_event (MonoMList **list, gint event);
int get_events_from_list (MonoMList *list);
void threadpool_append_async_io_jobs (MonoObject **jobs, gint njobs);

#endif
