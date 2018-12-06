/**
 * \file
 */

#ifndef __MONO_UTILS_MONO_THREADS_DEBUG_H__
#define __MONO_UTILS_MONO_THREADS_DEBUG_H__

/* Logging - enable them below if you need specific logging for the category you need */
#define MOSTLY_ASYNC_SAFE_FPRINTF(handle, ...) do { \
	char __buff[1024];	\
	__buff [0] = '\0';	\
	g_snprintf (__buff, sizeof(__buff), __VA_ARGS__);	\
	g_write (handle, __buff, (guint32)strlen (__buff));	\
} while (0)

#define MOSTLY_ASYNC_SAFE_PRINTF(...) MOSTLY_ASYNC_SAFE_FPRINTF(1, __VA_ARGS__);

#if 1
#define THREADS_DEBUG(...)
#else
#define THREADS_DEBUG MOSTLY_ASYNC_SAFE_PRINTF
#endif

#if 1
#define THREADS_STW_DEBUG(...)
#else
#define THREADS_STW_DEBUG MOSTLY_ASYNC_SAFE_PRINTF
#endif

#if 1
#define THREADS_SUSPEND_DEBUG(...)
#else
#define THREADS_SUSPEND_DEBUG MOSTLY_ASYNC_SAFE_PRINTF
#endif

#if 1
#define THREADS_STATE_MACHINE_DEBUG(...)
#else
#define THREADS_STATE_MACHINE_DEBUG MOSTLY_ASYNC_SAFE_PRINTF
#endif

#if 1
#define THREADS_INTERRUPT_DEBUG(...)
#else
#define THREADS_INTERRUPT_DEBUG MOSTLY_ASYNC_SAFE_PRINTF
#endif

#endif /* __MONO_UTILS_MONO_THREADS_DEBUG_H__ */
