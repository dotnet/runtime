
#include <config.h>
#include <glib.h>

#include <stdlib.h>
#include <string.h>
#include <time.h>

#if !defined(HOST_WIN32)
#include <pthread.h>
#include <errno.h>
#else
#include <winsock2.h>
#include <windows.h>
#endif

#ifdef HAVE_SYS_TIME_H
#include <sys/time.h>
#endif

#include "mono-os-mutex.h"

#if !defined(HOST_WIN32)

void
mono_os_mutex_init (mono_mutex_t *mutex)
{
	gint res;

	res = pthread_mutex_init (mutex, NULL);
	if (G_UNLIKELY (res != 0))
		g_error ("%s: pthread_mutex_init failed with \"%s\" (%d)", __func__, g_strerror (res), res);
}

void
mono_os_mutex_init_recursive (mono_mutex_t *mutex)
{
	gint res;
	pthread_mutexattr_t attr;

	res = pthread_mutexattr_init (&attr);
	if (G_UNLIKELY (res != 0))
		g_error ("%s: pthread_mutexattr_init failed with \"%s\" (%d)", __func__, g_strerror (res), res);

	res = pthread_mutexattr_settype (&attr, PTHREAD_MUTEX_RECURSIVE);
	if (G_UNLIKELY (res != 0))
		g_error ("%s: pthread_mutexattr_settype failed with \"%s\" (%d)", __func__, g_strerror (res), res);

	res = pthread_mutex_init (mutex, &attr);
	if (G_UNLIKELY (res != 0))
		g_error ("%s: pthread_mutex_init failed with \"%s\" (%d)", __func__, g_strerror (res), res);

	res = pthread_mutexattr_destroy (&attr);
	if (G_UNLIKELY (res != 0))
		g_error ("%s: pthread_mutexattr_destroy failed with \"%s\" (%d)", __func__, g_strerror (res), res);
}

gint
mono_os_mutex_destroy (mono_mutex_t *mutex)
{
	gint res;

	res = pthread_mutex_destroy (mutex);
	if (G_UNLIKELY (res != 0 && res != EBUSY))
		g_error ("%s: pthread_mutex_destroy failed with \"%s\" (%d)", __func__, g_strerror (res), res);

	return res != 0 ? -1 : 0;
}

void
mono_os_mutex_lock (mono_mutex_t *mutex)
{
	gint res;

	res = pthread_mutex_lock (mutex);
	if (G_UNLIKELY (res != 0))
		g_error ("%s: pthread_mutex_lock failed with \"%s\" (%d)", __func__, g_strerror (res), res);
}

gint
mono_os_mutex_trylock (mono_mutex_t *mutex)
{
	gint res;

	res = pthread_mutex_trylock (mutex);
	if (G_UNLIKELY (res != 0 && res != EBUSY))
		g_error ("%s: pthread_mutex_trylock failed with \"%s\" (%d)", __func__, g_strerror (res), res);

	return res != 0 ? -1 : 0;
}

void
mono_os_mutex_unlock (mono_mutex_t *mutex)
{
	gint res;

	res = pthread_mutex_unlock (mutex);
	if (G_UNLIKELY (res != 0))
		g_error ("%s: pthread_mutex_unlock failed with \"%s\" (%d)", __func__, g_strerror (res), res);
}

void
mono_os_cond_init (mono_cond_t *cond)
{
	gint res;

	res = pthread_cond_init (cond, NULL);
	if (G_UNLIKELY (res != 0))
		g_error ("%s: pthread_cond_init failed with \"%s\" (%d)", __func__, g_strerror (res), res);
}

gint
mono_os_cond_destroy (mono_cond_t *cond)
{
	gint res;

	res = pthread_cond_destroy (cond);
	if (G_UNLIKELY (res != 0 && res != EBUSY))
		g_error ("%s: pthread_cond_destroy failed with \"%s\" (%d)", __func__, g_strerror (res), res);

	return res != 0 ? -1 : 0;
}

void
mono_os_cond_wait (mono_cond_t *cond, mono_mutex_t *mutex)
{
	gint res;

	res = pthread_cond_wait (cond, mutex);
	if (G_UNLIKELY (res != 0))
		g_error ("%s: pthread_cond_wait failed with \"%s\" (%d)", __func__, g_strerror (res), res);
}

gint
mono_os_cond_timedwait (mono_cond_t *cond, mono_mutex_t *mutex, guint32 timeout_ms)
{
	struct timeval tv;
	struct timespec ts;
	gint64 usecs;
	gint res;

	if (timeout_ms == MONO_INFINITE_WAIT) {
		mono_os_cond_wait (cond, mutex);
		return 0;
	}

	/* ms = 10^-3, us = 10^-6, ns = 10^-9 */

	res = gettimeofday (&tv, NULL);
	if (G_UNLIKELY (res != 0))
		g_error ("%s: pthread_cond_timedwait failed with \"%s\" (%d)", __func__, g_strerror (errno), errno);

	tv.tv_sec += timeout_ms / 1000;
	usecs = tv.tv_usec + ((timeout_ms % 1000) * 1000);
	if (usecs >= 1000000) {
		usecs -= 1000000;
		tv.tv_sec ++;
	}
	ts.tv_sec = tv.tv_sec;
	ts.tv_nsec = usecs * 1000;

	res = pthread_cond_timedwait (cond, mutex, &ts);
	if (G_UNLIKELY (res != 0 && res != ETIMEDOUT))
		g_error ("%s: pthread_cond_timedwait failed with \"%s\" (%d)", __func__, g_strerror (res), res);

	return res != 0 ? -1 : 0;
}

void
mono_os_cond_signal (mono_cond_t *cond)
{
	gint res;

	res = pthread_cond_signal (cond);
	if (G_UNLIKELY (res != 0))
		g_error ("%s: pthread_cond_signal failed with \"%s\" (%d)", __func__, g_strerror (res), res);
}

void
mono_os_cond_broadcast (mono_cond_t *cond)
{
	gint res;

	res = pthread_cond_broadcast (cond);
	if (G_UNLIKELY (res != 0))
		g_error ("%s: pthread_cond_broadcast failed with \"%s\" (%d)", __func__, g_strerror (res), res);
}

#else

/* Vanilla MinGW is missing some defs, load them from MinGW-w64. */
#if defined __MINGW32__ && !defined __MINGW64_VERSION_MAJOR && (_WIN32_WINNT >= 0x0600)

/* Fixme: Opaque structs */
typedef PVOID RTL_CONDITION_VARIABLE;

#define RTL_SRWLOCK_INIT 0
#define RTL_CONDITION_VARIABLE_INIT 0
#define RTL_CONDITION_VARIABLE_LOCKMODE_SHARED 1

/*Condition Variables http://msdn.microsoft.com/en-us/library/ms682052%28VS.85%29.aspx*/
typedef RTL_CONDITION_VARIABLE CONDITION_VARIABLE, *PCONDITION_VARIABLE;

WINBASEAPI VOID WINAPI InitializeConditionVariable(PCONDITION_VARIABLE ConditionVariable);
WINBASEAPI WINBOOL WINAPI SleepConditionVariableCS(PCONDITION_VARIABLE ConditionVariable, PCRITICAL_SECTION CriticalSection, DWORD dwMilliseconds);
WINBASEAPI VOID WINAPI WakeAllConditionVariable(PCONDITION_VARIABLE ConditionVariable);
WINBASEAPI VOID WINAPI WakeConditionVariable(PCONDITION_VARIABLE ConditionVariable);

/* https://msdn.microsoft.com/en-us/library/windows/desktop/ms683477(v=vs.85).aspx */
WINBASEAPI BOOL WINAPI InitializeCriticalSectionEx(LPCRITICAL_SECTION lpCriticalSection, DWORD dwSpinCount, DWORD Flags);

#define CRITICAL_SECTION_NO_DEBUG_INFO 0x01000000

#endif /* defined __MINGW32__ && !defined __MINGW64_VERSION_MAJOR && (_WIN32_WINNT >= 0x0600) */

void
mono_os_mutex_init (mono_mutex_t *mutex)
{
	BOOL res;

	res = InitializeCriticalSectionEx (mutex, 0, CRITICAL_SECTION_NO_DEBUG_INFO);
	if (G_UNLIKELY (res == 0))
		g_error ("%s: InitializeCriticalSectionEx failed with error %d", __func__, GetLastError ());
}

void
mono_os_mutex_init_recursive (mono_mutex_t *mutex)
{
	BOOL res;

	res = InitializeCriticalSectionEx (mutex, 0, CRITICAL_SECTION_NO_DEBUG_INFO);
	if (G_UNLIKELY (res == 0))
		g_error ("%s: InitializeCriticalSectionEx failed with error %d", __func__, GetLastError ());
}

gint
mono_os_mutex_destroy (mono_mutex_t *mutex)
{
	DeleteCriticalSection (mutex);
	return 0;
}

void
mono_os_mutex_lock (mono_mutex_t *mutex)
{
	EnterCriticalSection (mutex);
}

gint
mono_os_mutex_trylock (mono_mutex_t *mutex)
{
	return TryEnterCriticalSection (mutex) == 0 ? -1 : 0;
}

void
mono_os_mutex_unlock (mono_mutex_t *mutex)
{
	LeaveCriticalSection (mutex);
}

void
mono_os_cond_init (mono_cond_t *cond)
{
	InitializeConditionVariable (cond);
}

gint
mono_os_cond_destroy (mono_cond_t *cond)
{
	/* Beauty of win32 API: do not destroy it */
	return 0;
}

void
mono_os_cond_wait (mono_cond_t *cond, mono_mutex_t *mutex)
{
	BOOL res;

	res = SleepConditionVariableCS (cond, mutex, INFINITE);
	if (G_UNLIKELY (res == 0))
		g_error ("%s: SleepConditionVariableCS failed with error %d", __func__, GetLastError ());
}

gint
mono_os_cond_timedwait (mono_cond_t *cond, mono_mutex_t *mutex, guint32 timeout_ms)
{
	BOOL res;

	res = SleepConditionVariableCS (cond, mutex, timeout_ms);
	if (G_UNLIKELY (res == 0 && GetLastError () != ERROR_TIMEOUT))
		g_error ("%s: SleepConditionVariableCS failed with error %d", __func__, GetLastError ());

	return res == 0 ? -1 : 0;
}

void
mono_os_cond_signal (mono_cond_t *cond)
{
	WakeConditionVariable (cond);
}

void
mono_os_cond_broadcast (mono_cond_t *cond)
{
	WakeAllConditionVariable (cond);
}

#endif
