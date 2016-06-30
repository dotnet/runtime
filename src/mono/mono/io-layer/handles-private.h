/*
 * handles-private.h:  Internal operations on handles
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002-2006 Novell, Inc.
 */

#ifndef _WAPI_HANDLES_PRIVATE_H_
#define _WAPI_HANDLES_PRIVATE_H_

#include <config.h>
#include <glib.h>
#include <errno.h>
#include <string.h>
#include <sys/types.h>
#include <time.h>

#include <mono/utils/atomic.h>

#undef DEBUG

typedef enum {
	WAPI_HANDLE_UNUSED=0,
	WAPI_HANDLE_FILE,
	WAPI_HANDLE_CONSOLE,
	WAPI_HANDLE_THREAD,
	WAPI_HANDLE_SEM,
	WAPI_HANDLE_MUTEX,
	WAPI_HANDLE_EVENT,
	WAPI_HANDLE_SOCKET,
	WAPI_HANDLE_FIND,
	WAPI_HANDLE_PROCESS,
	WAPI_HANDLE_PIPE,
	WAPI_HANDLE_NAMEDMUTEX,
	WAPI_HANDLE_NAMEDSEM,
	WAPI_HANDLE_NAMEDEVENT,
	WAPI_HANDLE_COUNT
} WapiHandleType;

typedef struct 
{
	void (*close)(gpointer handle, gpointer data);

	/* SignalObjectAndWait */
	void (*signal)(gpointer signal);

	/* Called by WaitForSingleObject and WaitForMultipleObjects,
	 * with the handle locked (shared handles aren't locked.)
	 * Returns TRUE if ownership was established, false otherwise.
	 */
	gboolean (*own_handle)(gpointer handle);

	/* Called by WaitForSingleObject and WaitForMultipleObjects, if the
	 * handle in question is "ownable" (ie mutexes), to see if the current
	 * thread already owns this handle
	 */
	gboolean (*is_owned)(gpointer handle);

	/* Called by WaitForSingleObject and WaitForMultipleObjects,
	 * if the handle in question needs a special wait function
	 * instead of using the normal handle signal mechanism.
	 * Returns the WaitForSingleObject return code.
	 */
	guint32 (*special_wait)(gpointer handle, guint32 timeout, gboolean alertable);

	/* Called by WaitForSingleObject and WaitForMultipleObjects,
	 * if the handle in question needs some preprocessing before the
	 * signal wait.
	 */
	void (*prewait)(gpointer handle);

	/* Called when dumping the handles */
	void (*details)(gpointer data);

	/* Called to get the name of the handle type */
	const gchar* (*typename) (void);

	/* Called to get the size of the handle type */
	gsize (*typesize) (void);
} WapiHandleOps;

typedef enum {
	WAPI_HANDLE_CAP_WAIT         = 0x01,
	WAPI_HANDLE_CAP_SIGNAL       = 0x02,
	WAPI_HANDLE_CAP_OWN          = 0x04,
	WAPI_HANDLE_CAP_SPECIAL_WAIT = 0x08,
} WapiHandleCapability;

extern guint32 _wapi_fd_reserve;

void
_wapi_handle_init (void);

void
_wapi_handle_cleanup (void);

extern gpointer _wapi_handle_new (WapiHandleType type,
				  gpointer handle_specific);
extern gpointer _wapi_handle_new_fd (WapiHandleType type, int fd,
				     gpointer handle_specific);
extern gboolean _wapi_lookup_handle (gpointer handle, WapiHandleType type,
				     gpointer *handle_specific);
extern gpointer _wapi_search_handle (WapiHandleType type,
				     gboolean (*check)(gpointer, gpointer),
				     gpointer user_data,
				     gpointer *handle_specific,
				     gboolean search_shared);
extern void _wapi_handle_ref (gpointer handle);
extern void _wapi_handle_unref (gpointer handle);
extern void _wapi_handle_register_capabilities (WapiHandleType type,
						WapiHandleCapability caps);
extern gboolean _wapi_handle_test_capabilities (gpointer handle,
						WapiHandleCapability caps);
extern void _wapi_handle_ops_close (gpointer handle, gpointer data);
extern void _wapi_handle_ops_signal (gpointer handle);
extern gboolean _wapi_handle_ops_own (gpointer handle);
extern gboolean _wapi_handle_ops_isowned (gpointer handle);
extern guint32 _wapi_handle_ops_special_wait (gpointer handle,
					      guint32 timeout,
					      gboolean alertable);
extern void _wapi_handle_ops_prewait (gpointer handle);
extern void _wapi_handle_ops_details (WapiHandleType type, gpointer data);
extern const gchar* _wapi_handle_ops_typename (WapiHandleType type);
extern gsize _wapi_handle_ops_typesize (WapiHandleType type);

extern gboolean _wapi_handle_count_signalled_handles (guint32 numhandles,
						      gpointer *handles,
						      gboolean waitall,
						      guint32 *retcount,
						      guint32 *lowest);
extern void _wapi_handle_unlock_handles (guint32 numhandles,
					 gpointer *handles);
extern int _wapi_handle_timedwait_signal_handle (gpointer handle, guint32 timeout, gboolean poll, gboolean *alerted);
extern int _wapi_handle_timedwait_signal (guint32 timeout, gboolean poll, gboolean *alerted);
extern void _wapi_handle_dump (void);
extern void _wapi_handle_foreach (gboolean (*on_each)(gpointer handle, gpointer data, gpointer user_data), gpointer user_data);

WapiHandleType
_wapi_handle_type (gpointer handle);

void
_wapi_handle_set_signal_state (gpointer handle, gboolean state, gboolean broadcast);

gboolean
_wapi_handle_issignalled (gpointer handle);

int
_wapi_handle_lock_signal_mutex (void);

int
_wapi_handle_unlock_signal_mutex (void);

int
_wapi_handle_lock_handle (gpointer handle);

int
_wapi_handle_trylock_handle (gpointer handle);

int
_wapi_handle_unlock_handle (gpointer handle);

static inline void _wapi_handle_spin (guint32 ms)
{
	struct timespec sleepytime;
	
	g_assert (ms < 1000);
	
	sleepytime.tv_sec = 0;
	sleepytime.tv_nsec = ms * 1000000;
	
	nanosleep (&sleepytime, NULL);
}

#endif /* _WAPI_HANDLES_PRIVATE_H_ */
