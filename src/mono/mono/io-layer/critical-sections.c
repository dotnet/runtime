#include <config.h>
#include <glib.h>
#include <pthread.h>

#include "mono/io-layer/wapi.h"

#undef DEBUG

/* A critical section is really just like a lightweight mutex. It
 * can't be waited for, and doesn't have a handle.
 */

/* According to the MSDN docs, the Microsoft implementation spins a
 * number of times then waits for a semaphore.  I could implement that
 * here but I'd need a mutex around the critical section structure
 * anyway.  So I may as well just use a pthread mutex.
 */
static pthread_once_t attr_key_once=PTHREAD_ONCE_INIT;
static pthread_mutexattr_t attr;

static void attr_init(void)
{
	pthread_mutexattr_init(&attr);
	pthread_mutexattr_settype(&attr, PTHREAD_MUTEX_RECURSIVE);
}

/**
 * InitializeCriticalSection:
 * @section: The critical section to initialise
 *
 * Initialises a critical section.
 */
void InitializeCriticalSection(WapiCriticalSection *section)
{
	pthread_once(&attr_key_once, attr_init);
	pthread_mutex_init(&section->mutex, &attr);
}

/**
 * InitializeCriticalSectionAndSpinCount:
 * @section: The critical section to initialise.
 * @spincount: The spin count for this critical section.  Not
 * currently used.
 *
 * Initialises a critical section and sets the spin count.  This
 * implementation just calls InitializeCriticalSection().
 *
 * Return value: %TRUE on success, %FALSE otherwise.  (%FALSE never
 * happens).
 */
gboolean InitializeCriticalSectionAndSpinCount(WapiCriticalSection *section,
					       guint32 spincount G_GNUC_UNUSED)
{
	InitializeCriticalSection(section);
	
	return(TRUE);
}

/**
 * DeleteCriticalSection:
 * @section: The critical section to delete.
 *
 * Releases all resources owned by critical section @section.
 */
void DeleteCriticalSection(WapiCriticalSection *section)
{
	pthread_mutex_destroy(&section->mutex);
}

/**
 * SetCriticalSectionSpinCount:
 * @section: The critical section to set
 * @spincount: The new spin count for this critical section.  Not
 * currently used.
 *
 * Sets the spin count for the critical section @section.  The spin
 * count is currently ignored, and set to zero.
 *
 * Return value: The previous spin count.  (Currently always zero).
 */
guint32 SetCriticalSectionSpinCount(WapiCriticalSection *section G_GNUC_UNUSED, guint32 spincount G_GNUC_UNUSED)
{
	return(0);
}

/**
 * TryEnterCriticalSection:
 * @section: The critical section to try and enter
 *
 * Attempts to enter a critical section without blocking.  If
 * successful the calling thread takes ownership of the critical
 * section.
 *
 * A thread can recursively call EnterCriticalSection() and
 * TryEnterCriticalSection(), but must call LeaveCriticalSection() an
 * equal number of times.
 *
 * Return value: %TRUE if the thread successfully locked the critical
 * section, %FALSE otherwise.
 */
gboolean TryEnterCriticalSection(WapiCriticalSection *section)
{
	int ret;
	
	ret=pthread_mutex_trylock(&section->mutex);
	if(ret==0) {
		return(TRUE);
	} else {
		return(FALSE);
	}
}

/**
 * EnterCriticalSection:
 * @section: The critical section to enter
 *
 * Enters critical section @section, blocking while other threads own
 * it.  This function doesn't return until the calling thread assumes
 * ownership of @section.
 *
 * A thread can recursively call EnterCriticalSection() and
 * TryEnterCriticalSection(), but must call LeaveCriticalSection() an
 * equal number of times.
 */
void EnterCriticalSection(WapiCriticalSection *section)
{
	pthread_mutex_lock(&section->mutex);
}

/**
 * LeaveCriticalSection:
 * @section: The critical section to leave
 *
 * Leaves critical section @section, relinquishing ownership.
 *
 * A thread can recursively call EnterCriticalSection() and
 * TryEnterCriticalSection(), but must call LeaveCriticalSection() an
 * equal number of times.
 */
void LeaveCriticalSection(WapiCriticalSection *section)
{
	pthread_mutex_unlock(&section->mutex);
}

