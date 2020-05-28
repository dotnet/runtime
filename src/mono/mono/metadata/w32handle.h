/**
 * \file
 */

#ifndef _MONO_METADATA_W32HANDLE_H_
#define _MONO_METADATA_W32HANDLE_H_

#include <config.h>
#include <glib.h>

#ifdef HOST_WIN32
#include <windows.h>
#else
#define INVALID_HANDLE_VALUE ((gpointer)-1)
#endif

#include "mono/utils/mono-coop-mutex.h"
#include "mono/utils/mono-error.h"

#define MONO_W32HANDLE_MAXIMUM_WAIT_OBJECTS 64
#define MONO_INFINITE_WAIT ((guint32) 0xFFFFFFFF)

typedef enum {
	MONO_W32TYPE_UNUSED = 0,
	MONO_W32TYPE_SEM,
	MONO_W32TYPE_MUTEX,
	MONO_W32TYPE_EVENT,
	MONO_W32TYPE_PROCESS,
	MONO_W32TYPE_NAMEDMUTEX,
	MONO_W32TYPE_NAMEDSEM,
	MONO_W32TYPE_NAMEDEVENT,
	MONO_W32TYPE_COUNT
} MonoW32Type;

typedef struct {
	MonoW32Type type;
	guint ref;
	gboolean signalled;
	gboolean in_use;
	MonoCoopMutex signal_mutex;
	MonoCoopCond signal_cond;
	gpointer specific;
} MonoW32Handle;

typedef enum {
	MONO_W32HANDLE_WAIT_RET_SUCCESS_0   =  0,
	MONO_W32HANDLE_WAIT_RET_ABANDONED_0 =  MONO_W32HANDLE_WAIT_RET_SUCCESS_0 + MONO_W32HANDLE_MAXIMUM_WAIT_OBJECTS,
	MONO_W32HANDLE_WAIT_RET_ALERTED     = -1,
	MONO_W32HANDLE_WAIT_RET_TIMEOUT     = -2,
	MONO_W32HANDLE_WAIT_RET_FAILED      = -3,
	MONO_W32HANDLE_WAIT_RET_TOO_MANY_POSTS = -4,
	MONO_W32HANDLE_WAIT_RET_NOT_OWNED_BY_CALLER = -5
} MonoW32HandleWaitRet;

typedef struct 
{
	void (*close)(gpointer data);

	/* mono_w32handle_signal_and_wait */
	gint32 (*signal)(MonoW32Handle *handle_data);

	/* Called by mono_w32handle_wait_one and mono_w32handle_wait_multiple,
	 * with the handle locked (shared handles aren't locked.)
	 * Returns TRUE if ownership was established, false otherwise.
	 * If TRUE, *abandoned contains a status code such as
	 * WAIT_OBJECT_0 or WAIT_ABANDONED_0.
	 */
	gboolean (*own_handle)(MonoW32Handle *handle_data, gboolean *abandoned);

	/* Called by mono_w32handle_wait_one and mono_w32handle_wait_multiple, if the
	 * handle in question is "ownable" (ie mutexes), to see if the current
	 * thread already owns this handle
	 */
	gboolean (*is_owned)(MonoW32Handle *handle_data);

	/* Called by mono_w32handle_wait_one and mono_w32handle_wait_multiple,
	 * if the handle in question needs a special wait function
	 * instead of using the normal handle signal mechanism.
	 * Returns the mono_w32handle_wait_one return code.
	 */
	MonoW32HandleWaitRet (*special_wait)(MonoW32Handle *handle_data, guint32 timeout, gboolean *alerted);

	/* Called by mono_w32handle_wait_one and mono_w32handle_wait_multiple,
	 * if the handle in question needs some preprocessing before the
	 * signal wait.
	 */
	void (*prewait)(MonoW32Handle *handle_data);

	/* Called when dumping the handles */
	void (*details)(MonoW32Handle *handle_data);

	/* Called to get the name of the handle type */
	const char* (*type_name) (void);

	/* Called to get the size of the handle type */
	gsize (*typesize) (void);
} MonoW32HandleOps;

typedef enum {
	MONO_W32HANDLE_CAP_WAIT         = 0x01,
	MONO_W32HANDLE_CAP_SIGNAL       = 0x02,
	MONO_W32HANDLE_CAP_OWN          = 0x04,
	MONO_W32HANDLE_CAP_SPECIAL_WAIT = 0x08,
} MonoW32HandleCapability;

void
mono_w32handle_init (void);

void
mono_w32handle_cleanup (void);

void
mono_w32handle_register_ops (MonoW32Type type, const MonoW32HandleOps *ops);

gpointer
mono_w32handle_new (MonoW32Type type, gpointer handle_specific);

gpointer
mono_w32handle_duplicate (MonoW32Handle *handle_data);

gboolean
mono_w32handle_close (gpointer handle);

const gchar*
mono_w32handle_get_typename (MonoW32Type type);

gboolean
mono_w32handle_lookup_and_ref (gpointer handle, MonoW32Handle **handle_data);

void
mono_w32handle_unref (MonoW32Handle *handle_data);

void
mono_w32handle_foreach (gboolean (*on_each)(MonoW32Handle *handle_data, gpointer user_data), gpointer user_data);

void
mono_w32handle_dump (void);

void
mono_w32handle_register_capabilities (MonoW32Type type, MonoW32HandleCapability caps);

void
mono_w32handle_set_signal_state (MonoW32Handle *handle_data, gboolean state, gboolean broadcast);

gboolean
mono_w32handle_issignalled (MonoW32Handle *handle_data);

void
mono_w32handle_lock (MonoW32Handle *handle_data);

gboolean
mono_w32handle_trylock (MonoW32Handle *handle_data);

void
mono_w32handle_unlock (MonoW32Handle *handle_data);

gboolean
mono_w32handle_handle_is_signalled (gpointer handle);

gboolean
mono_w32handle_handle_is_owned (gpointer handle);

MonoW32HandleWaitRet
mono_w32handle_wait_one (gpointer handle, guint32 timeout, gboolean alertable);

MonoW32HandleWaitRet
mono_w32handle_wait_multiple (gpointer *handles, gsize nhandles, gboolean waitall, guint32 timeout, gboolean alertable, MonoError *error);

MonoW32HandleWaitRet
mono_w32handle_signal_and_wait (gpointer signal_handle, gpointer wait_handle, guint32 timeout, gboolean alertable);

#ifdef HOST_WIN32
static inline MonoW32HandleWaitRet
mono_w32handle_convert_wait_ret (guint32 res, guint32 numobjects)
{
	if (res >= WAIT_OBJECT_0 && res <= WAIT_OBJECT_0 + numobjects - 1)
		return (MonoW32HandleWaitRet)(MONO_W32HANDLE_WAIT_RET_SUCCESS_0 + (res - WAIT_OBJECT_0));
	else if (res >= WAIT_ABANDONED_0 && res <= WAIT_ABANDONED_0 + numobjects - 1)
		return (MonoW32HandleWaitRet)(MONO_W32HANDLE_WAIT_RET_ABANDONED_0 + (res - WAIT_ABANDONED_0));
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
