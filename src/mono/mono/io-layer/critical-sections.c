/*
 * critical-sections.c:  Critical sections
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>
#include <pthread.h>

#include <mono/io-layer/wapi.h>
#include <mono/io-layer/critical-section-private.h>

#include "mono-mutex.h"

#undef DEBUG

/* A critical section is really just like a lightweight mutex. It
 * can't be waited for, and doesn't have a handle.
 */

/* According to the MSDN docs, the Microsoft implementation spins a
 * number of times then waits for a semaphore.  I could implement that
 * here but I'd need a mutex around the critical section structure
 * anyway.  So I may as well just use a pthread mutex.
 */
static mono_once_t attr_key_once=MONO_ONCE_INIT;
static mono_mutexattr_t attr;

static void attr_init(void)
{
	int ret;
	
	ret = mono_mutexattr_init(&attr);
	g_assert (ret == 0);
	
	ret = mono_mutexattr_settype(&attr, MONO_MUTEX_RECURSIVE);
	g_assert (ret == 0);
}

void _wapi_critical_section_cleanup (void)
{
	mono_mutexattr_destroy (&attr);
}

/**
 * InitializeCriticalSection:
 * @section: The critical section to initialise
 *
 * Initialises a critical section.
 */
void InitializeCriticalSection(WapiCriticalSection *section)
{
	int ret;
	
	mono_once(&attr_key_once, attr_init);
	ret = mono_mutex_init(&section->mutex, &attr);
	g_assert (ret == 0);
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
	int ret;
	
	ret = mono_mutex_destroy(&section->mutex);
	g_assert (ret == 0);
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
	
	ret=mono_mutex_trylock(&section->mutex);
	if(ret==0) {
		return(TRUE);
	} else {
		return(FALSE);
	}
}

