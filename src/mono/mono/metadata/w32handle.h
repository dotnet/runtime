/**
 * \file
 */

#ifndef _MONO_METADATA_W32HANDLE_H_
#define _MONO_METADATA_W32HANDLE_H_

#include <config.h>
#include <glib.h>

#ifdef HOST_WIN32
#include <windows.h>
#endif

#ifndef INVALID_HANDLE_VALUE
#define INVALID_HANDLE_VALUE (gpointer)-1
#endif

#define MONO_W32HANDLE_MAXIMUM_WAIT_OBJECTS 64

#ifndef MONO_INFINITE_WAIT
#define MONO_INFINITE_WAIT ((guint32) 0xFFFFFFFF)
#endif

typedef enum {
	MONO_W32HANDLE_UNUSED = 0,
	MONO_W32HANDLE_FILE,
	MONO_W32HANDLE_CONSOLE,
	MONO_W32HANDLE_THREAD,
	MONO_W32HANDLE_SEM,
	MONO_W32HANDLE_MUTEX,
	MONO_W32HANDLE_EVENT,
	MONO_W32HANDLE_SOCKET,
	MONO_W32HANDLE_FIND,
	MONO_W32HANDLE_PROCESS,
	MONO_W32HANDLE_PIPE,
	MONO_W32HANDLE_NAMEDMUTEX,
	MONO_W32HANDLE_NAMEDSEM,
	MONO_W32HANDLE_NAMEDEVENT,
	MONO_W32HANDLE_COUNT
} MonoW32HandleType;

typedef enum {
	MONO_W32HANDLE_WAIT_RET_SUCCESS_0   =  0,
	MONO_W32HANDLE_WAIT_RET_ABANDONED_0 =  MONO_W32HANDLE_WAIT_RET_SUCCESS_0 + MONO_W32HANDLE_MAXIMUM_WAIT_OBJECTS,
	MONO_W32HANDLE_WAIT_RET_ALERTED     = -1,
	MONO_W32HANDLE_WAIT_RET_TIMEOUT     = -2,
	MONO_W32HANDLE_WAIT_RET_FAILED      = -3,
} MonoW32HandleWaitRet;

typedef struct 
{
	void (*close)(gpointer handle, gpointer data);

	/* mono_w32handle_signal_and_wait */
	void (*signal)(gpointer signal, gpointer data);

	/* Called by mono_w32handle_wait_one and mono_w32handle_wait_multiple,
	 * with the handle locked (shared handles aren't locked.)
	 * Returns TRUE if ownership was established, false otherwise.
	 * If TRUE, *abandoned contains a status code such as
	 * WAIT_OBJECT_0 or WAIT_ABANDONED_0.
	 */
	gboolean (*own_handle)(gpointer handle, gboolean *abandoned);

	/* Called by mono_w32handle_wait_one and mono_w32handle_wait_multiple, if the
	 * handle in question is "ownable" (ie mutexes), to see if the current
	 * thread already owns this handle
	 */
	gboolean (*is_owned)(gpointer handle);

	/* Called by mono_w32handle_wait_one and mono_w32handle_wait_multiple,
	 * if the handle in question needs a special wait function
	 * instead of using the normal handle signal mechanism.
	 * Returns the mono_w32handle_wait_one return code.
	 */
	MonoW32HandleWaitRet (*special_wait)(gpointer handle, guint32 timeout, gboolean *alerted);

	/* Called by mono_w32handle_wait_one and mono_w32handle_wait_multiple,
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
} MonoW32HandleOps;

typedef enum {
	MONO_W32HANDLE_CAP_WAIT         = 0x01,
	MONO_W32HANDLE_CAP_SIGNAL       = 0x02,
	MONO_W32HANDLE_CAP_OWN          = 0x04,
	MONO_W32HANDLE_CAP_SPECIAL_WAIT = 0x08,
} MonoW32HandleCapability;

extern guint32 mono_w32handle_fd_reserve;

void
mono_w32handle_init (void);

void
mono_w32handle_cleanup (void);

void
mono_w32handle_register_ops (MonoW32HandleType type, MonoW32HandleOps *ops);

gpointer
mono_w32handle_new (MonoW32HandleType type, gpointer handle_specific);

gpointer
mono_w32handle_new_fd (MonoW32HandleType type, int fd, gpointer handle_specific);

gpointer
mono_w32handle_duplicate (gpointer handle);

gboolean
mono_w32handle_close (gpointer handle);

MonoW32HandleType
mono_w32handle_get_type (gpointer handle);

const gchar*
mono_w32handle_get_typename (MonoW32HandleType type);

gboolean
mono_w32handle_lookup (gpointer handle, MonoW32HandleType type, gpointer *handle_specific);

void
mono_w32handle_foreach (gboolean (*on_each)(gpointer handle, gpointer data, gpointer user_data), gpointer user_data);

void
mono_w32handle_dump (void);

void
mono_w32handle_register_capabilities (MonoW32HandleType type, MonoW32HandleCapability caps);

gboolean
mono_w32handle_test_capabilities (gpointer handle, MonoW32HandleCapability caps);

void
mono_w32handle_force_close (gpointer handle, gpointer data);

void
mono_w32handle_set_signal_state (gpointer handle, gboolean state, gboolean broadcast);

gboolean
mono_w32handle_issignalled (gpointer handle);

void
mono_w32handle_lock_handle (gpointer handle);

gboolean
mono_w32handle_trylock_handle (gpointer handle);

void
mono_w32handle_unlock_handle (gpointer handle);

MonoW32HandleWaitRet
mono_w32handle_wait_one (gpointer handle, guint32 timeout, gboolean alertable);

MonoW32HandleWaitRet
mono_w32handle_wait_multiple (gpointer *handles, gsize nhandles, gboolean waitall, guint32 timeout, gboolean alertable);

MonoW32HandleWaitRet
mono_w32handle_signal_and_wait (gpointer signal_handle, gpointer wait_handle, guint32 timeout, gboolean alertable);

#ifdef HOST_WIN32
static inline MonoW32HandleWaitRet
mono_w32handle_convert_wait_ret (guint32 res, guint32 numobjects)
{
	if (res >= WAIT_OBJECT_0 && res <= WAIT_OBJECT_0 + numobjects - 1)
		return MONO_W32HANDLE_WAIT_RET_SUCCESS_0 + (res - WAIT_OBJECT_0);
	else if (res >= WAIT_ABANDONED_0 && res <= WAIT_ABANDONED_0 + numobjects - 1)
		return MONO_W32HANDLE_WAIT_RET_ABANDONED_0 + (res - WAIT_ABANDONED_0);
	else if (res == WAIT_IO_COMPLETION)
		return MONO_W32HANDLE_WAIT_RET_ALERTED;
	else if (res == WAIT_TIMEOUT)
		return MONO_W32HANDLE_WAIT_RET_TIMEOUT;
	else if (res == WAIT_FAILED)
		return MONO_W32HANDLE_WAIT_RET_FAILED;
	else
		g_error ("%s: unknown res value %d", __func__, res);
}
#endif


#endif /* _MONO_METADATA_W32HANDLE_H_ */
