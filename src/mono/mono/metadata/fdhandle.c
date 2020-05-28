
#include "fdhandle.h"
#include "utils/mono-lazy-init.h"
#include "utils/mono-coop-mutex.h"

static GHashTable *fds;
static MonoCoopMutex fds_mutex;
static MonoFDHandleCallback fds_callback[MONO_FDTYPE_COUNT];
static mono_lazy_init_t fds_init = MONO_LAZY_INIT_STATUS_NOT_INITIALIZED;

static const gchar *types_str[] = {
	"File",
	"Console",
	"Pipe",
	"Socket",
	NULL
};

static void
fds_remove (gpointer data)
{
	MonoFDHandle* fdhandle;

	fdhandle = (MonoFDHandle*) data;
	g_assert (fdhandle);

	g_assert (fds_callback [fdhandle->type].close);
	fds_callback [fdhandle->type].close (fdhandle);

	mono_refcount_dec (fdhandle);
}

static void
initialize (void)
{
	fds = g_hash_table_new_full (g_direct_hash, g_direct_equal, NULL, fds_remove);
	mono_coop_mutex_init (&fds_mutex);
}

void
mono_fdhandle_register (MonoFDType type, MonoFDHandleCallback *callback)
{
	mono_lazy_initialize (&fds_init, initialize);
	memcpy (&fds_callback [type], callback, sizeof (MonoFDHandleCallback));
}

static void
fdhandle_destroy (gpointer data)
{
	MonoFDHandle* fdhandle;

	fdhandle = (MonoFDHandle*) data;
	g_assert (fdhandle);

	g_assert (fds_callback [fdhandle->type].destroy);
	fds_callback [fdhandle->type].destroy (fdhandle);
}

void
mono_fdhandle_init (MonoFDHandle *fdhandle, MonoFDType type, gint fd)
{
	mono_refcount_init (fdhandle, fdhandle_destroy);
	fdhandle->type = type;
	fdhandle->fd = fd;
}

void
mono_fdhandle_insert (MonoFDHandle *fdhandle)
{
	mono_coop_mutex_lock (&fds_mutex);

	if (g_hash_table_lookup_extended (fds, GINT_TO_POINTER(fdhandle->fd), NULL, NULL))
		g_error("%s: duplicate %s fd %d", __func__, types_str [fdhandle->type], fdhandle->fd);

	g_hash_table_insert (fds, GINT_TO_POINTER(fdhandle->fd), fdhandle);

	mono_coop_mutex_unlock (&fds_mutex);
}

gboolean
mono_fdhandle_try_insert (MonoFDHandle *fdhandle)
{
	mono_coop_mutex_lock (&fds_mutex);

	if (g_hash_table_lookup_extended (fds, GINT_TO_POINTER(fdhandle->fd), NULL, NULL)) {
		/* we raced between 2 invocations of mono_fdhandle_try_insert */
		mono_coop_mutex_unlock (&fds_mutex);

		return FALSE;
	}

	g_hash_table_insert (fds, GINT_TO_POINTER(fdhandle->fd), fdhandle);

	mono_coop_mutex_unlock (&fds_mutex);

	return TRUE;
}

gboolean
mono_fdhandle_lookup_and_ref (gint fd, MonoFDHandle **fdhandle)
{
	mono_coop_mutex_lock (&fds_mutex);

	if (!g_hash_table_lookup_extended (fds, GINT_TO_POINTER(fd), NULL, (gpointer*) fdhandle)) {
		mono_coop_mutex_unlock (&fds_mutex);
		return FALSE;
	}

	mono_refcount_inc (*fdhandle);

	mono_coop_mutex_unlock (&fds_mutex);

	return TRUE;
}

void
mono_fdhandle_unref (MonoFDHandle *fdhandle)
{
	mono_refcount_dec (fdhandle);
}

gboolean
mono_fdhandle_close (gint fd)
{
	MonoFDHandle *fdhandle;
	gboolean removed;

	mono_coop_mutex_lock (&fds_mutex);

	if (!g_hash_table_lookup_extended (fds, GINT_TO_POINTER(fd), NULL, (gpointer*) &fdhandle)) {
		mono_coop_mutex_unlock (&fds_mutex);

		return FALSE;
	}

	removed = g_hash_table_remove (fds, GINT_TO_POINTER(fdhandle->fd));
	g_assert (removed);

	mono_coop_mutex_unlock (&fds_mutex);

	return TRUE;
}
