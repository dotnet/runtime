#ifndef __MONO_PROC_LIB_H__
#define __MONO_PROC_LIB_H__
/*
 * Utility functions to access processes information and other info about the system.
 */

#include <glib.h>
#include <mono/utils/mono-compiler.h>

/* never remove or reorder these enums values: they are used in corlib/System */

typedef enum {
	MONO_PROCESS_NUM_THREADS,
	MONO_PROCESS_USER_TIME, /* milliseconds */
	MONO_PROCESS_SYSTEM_TIME, /* milliseconds */
	MONO_PROCESS_TOTAL_TIME, /* milliseconds */
	MONO_PROCESS_WORKING_SET,
	MONO_PROCESS_WORKING_SET_PEAK, /* 5 */
	MONO_PROCESS_PRIVATE_BYTES,
	MONO_PROCESS_VIRTUAL_BYTES,
	MONO_PROCESS_VIRTUAL_BYTES_PEAK,
	MONO_PROCESS_FAULTS,
	MONO_PROCESS_ELAPSED, /* 10 */
	MONO_PROCESS_PPID,
	MONO_PROCESS_PAGED_BYTES,
	MONO_PROCESS_END
} MonoProcessData;

typedef enum {
	MONO_CPU_USER_TIME,
	MONO_CPU_PRIV_TIME,
	MONO_CPU_INTR_TIME,
	MONO_CPU_DCP_TIME,
	MONO_CPU_IDLE_TIME,
	MONO_CPU_END
} MonoCpuData;

typedef enum {
	MONO_PROCESS_ERROR_NONE, /* no error happened */
	MONO_PROCESS_ERROR_NOT_FOUND, /* process not found */
	MONO_PROCESS_ERROR_OTHER
} MonoProcessError;

gpointer* mono_process_list     (int *size) MONO_INTERNAL;

char*     mono_process_get_name (gpointer pid, char *buf, int len) MONO_INTERNAL;

gint64    mono_process_get_data (gpointer pid, MonoProcessData data) MONO_INTERNAL;
gint64    mono_process_get_data_with_error (gpointer pid, MonoProcessData data, MonoProcessError *error) MONO_INTERNAL;

int       mono_process_current_pid (void) MONO_INTERNAL;

int       mono_cpu_count    (void) MONO_INTERNAL;
gint64    mono_cpu_get_data (int cpu_id, MonoCpuData data, MonoProcessError *error) MONO_INTERNAL;

int       mono_atexit (void (*func)(void)) MONO_INTERNAL;

#endif /* __MONO_PROC_LIB_H__ */

