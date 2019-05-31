/**
 * \file
 * System.Diagnostics.Process support
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * Copyright 2002 Ximian, Inc.
 * Copyright 2002-2006 Novell, Inc.
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>
#include <glib.h>

#include <stdio.h>
#include <string.h>
#include <pthread.h>
#include <sched.h>
#include <sys/time.h>
#include <errno.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <unistd.h>
#ifdef HAVE_SIGNAL_H
#include <signal.h>
#endif
#include <sys/time.h>
#include <fcntl.h>
#ifdef HAVE_SYS_PARAM_H
#include <sys/param.h>
#endif
#include <ctype.h>

#ifdef HAVE_SYS_WAIT_H
#include <sys/wait.h>
#endif
#ifdef HAVE_SYS_RESOURCE_H
#include <sys/resource.h>
#endif

#ifdef HAVE_SYS_MKDEV_H
#include <sys/mkdev.h>
#endif

#ifdef HAVE_UTIME_H
#include <utime.h>
#endif

// For close_my_fds
#if defined (_AIX)
#include <procinfo.h>
#elif defined (__FreeBSD__)
#include <sys/sysctl.h>
#include <sys/user.h>
#include <libutil.h>
#elif defined(__linux__)
#include <dirent.h>
#endif

#include <mono/metadata/object-internals.h>
#include <mono/metadata/w32process.h>
#include <mono/metadata/w32process-internals.h>
#include <mono/metadata/w32process-unix-internals.h>
#include <mono/metadata/w32error.h>
#include <mono/metadata/class.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/object.h>
#include <mono/metadata/metadata.h>
#include <mono/metadata/metadata-internals.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/w32handle.h>
#include <mono/metadata/w32file.h>
#include <mono/utils/mono-membar.h>
#include <mono/utils/mono-logger-internals.h>
#include <mono/utils/strenc.h>
#include <mono/utils/mono-proclib.h>
#include <mono/utils/mono-path.h>
#include <mono/utils/mono-lazy-init.h>
#include <mono/utils/mono-signal-handler.h>
#include <mono/utils/mono-time.h>
#include <mono/utils/mono-mmap.h>
#include <mono/utils/strenc.h>
#include <mono/utils/mono-io-portability.h>
#include <mono/utils/w32api.h>
#include <mono/utils/mono-errno.h>
#include "object-internals.h"
#include "icall-decl.h"

#ifndef MAXPATHLEN
#define MAXPATHLEN 242
#endif

#define STILL_ACTIVE ((int) 0x00000103)

#define LOGDEBUG(...)
/* define LOGDEBUG(...) g_message(__VA_ARGS__)  */

/* The process' environment strings */
#if defined(__APPLE__)
#if defined (TARGET_OSX)
/* Apple defines this in crt_externs.h but doesn't provide that header for 
 * arm-apple-darwin9.  We'll manually define the symbol on Apple as it does
 * in fact exist on all implementations (so far) 
 */
G_BEGIN_DECLS
gchar ***_NSGetEnviron(void);
G_END_DECLS
#define environ (*_NSGetEnviron())
#else
static char *mono_environ[1] = { NULL };
#define environ mono_environ
#endif /* defined (TARGET_OSX) */
#else
G_BEGIN_DECLS
extern char **environ;
G_END_DECLS
#endif

typedef enum {
	STARTF_USESHOWWINDOW=0x001,
	STARTF_USESIZE=0x002,
	STARTF_USEPOSITION=0x004,
	STARTF_USECOUNTCHARS=0x008,
	STARTF_USEFILLATTRIBUTE=0x010,
	STARTF_RUNFULLSCREEN=0x020,
	STARTF_FORCEONFEEDBACK=0x040,
	STARTF_FORCEOFFFEEDBACK=0x080,
	STARTF_USESTDHANDLES=0x100
} StartupFlags;

typedef struct {
	gpointer input;
	gpointer output;
	gpointer error;
} StartupHandles;

typedef struct {
#if G_BYTE_ORDER == G_BIG_ENDIAN
	guint32 highDateTime;
	guint32 lowDateTime;
#else
	guint32 lowDateTime;
	guint32 highDateTime;
#endif
} ProcessTime;

/*
 * Process describes processes we create.
 * It contains a semaphore that can be waited on in order to wait
 * for process termination.
 */
typedef struct _Process {
	pid_t pid; /* the pid of the process. This value is only valid until the process has exited. */
	MonoCoopSem exit_sem; /* this semaphore will be released when the process exits */
	int status; /* the exit status */
	gint32 handle_count; /* the number of handles to this process instance */
	/* we keep a ref to the creating _WapiHandle_process handle until
	 * the process has exited, so that the information there isn't lost.
	 */
	gpointer handle;
	gboolean signalled;
	struct _Process *next;
} Process;

/* MonoW32HandleProcess is a structure containing all the required information for process handling. */
typedef struct {
	pid_t pid;
	gboolean child;
	guint32 exitstatus;
	gpointer main_thread;
	guint64 create_time;
	guint64 exit_time;
	char *pname;
	size_t min_working_set;
	size_t max_working_set;
	gboolean exited;
	Process *process;
} MonoW32HandleProcess;

/*
 * VS_VERSIONINFO:
 *
 * 2 bytes: Length in bytes (this block, and all child blocks. does _not_ include alignment padding between blocks)
 * 2 bytes: Length in bytes of VS_FIXEDFILEINFO struct
 * 2 bytes: Type (contains 1 if version resource contains text data and 0 if version resource contains binary data)
 * Variable length unicode string (null terminated): Key (currently "VS_VERSION_INFO")
 * Variable length padding to align VS_FIXEDFILEINFO on a 32-bit boundary
 * VS_FIXEDFILEINFO struct
 * Variable length padding to align Child struct on a 32-bit boundary
 * Child struct (zero or one StringFileInfo structs, zero or one VarFileInfo structs)
 */

/*
 * StringFileInfo:
 *
 * 2 bytes: Length in bytes (includes this block, as well as all Child blocks)
 * 2 bytes: Value length (always zero)
 * 2 bytes: Type (contains 1 if version resource contains text data and 0 if version resource contains binary data)
 * Variable length unicode string: Key (currently "StringFileInfo")
 * Variable length padding to align Child struct on a 32-bit boundary
 * Child structs ( one or more StringTable structs.  Each StringTable struct's Key member indicates the appropriate language and code page for displaying the text in that StringTable struct.)
 */

/*
 * StringTable:
 *
 * 2 bytes: Length in bytes (includes this block as well as all Child blocks, but excludes any padding between String blocks)
 * 2 bytes: Value length (always zero)
 * 2 bytes: Type (contains 1 if version resource contains text data and 0 if version resource contains binary data)
 * Variable length unicode string: Key. An 8-digit hex number stored as a unicode string.  The four most significant digits represent the language identifier.  The four least significant digits represent the code page for which the data is formatted.
 * Variable length padding to align Child struct on a 32-bit boundary
 * Child structs (an array of one or more String structs (each aligned on a 32-bit boundary)
 */

/*
 * String:
 *
 * 2 bytes: Length in bytes (of this block)
 * 2 bytes: Value length (the length in words of the Value member)
 * 2 bytes: Type (contains 1 if version resource contains text data and 0 if version resource contains binary data)
 * Variable length unicode string: Key. arbitrary string, identifies data.
 * Variable length padding to align Value on a 32-bit boundary
 * Value: Variable length unicode string, holding data.
 */

/*
 * VarFileInfo:
 *
 * 2 bytes: Length in bytes (includes this block, as well as all Child blocks)
 * 2 bytes: Value length (always zero)
 * 2 bytes: Type (contains 1 if version resource contains text data and 0 if version resource contains binary data)
 * Variable length unicode string: Key (currently "VarFileInfo")
 * Variable length padding to align Child struct on a 32-bit boundary
 * Child structs (a Var struct)
 */

/*
 * Var:
 *
 * 2 bytes: Length in bytes of this block
 * 2 bytes: Value length in bytes of the Value
 * 2 bytes: Type (contains 1 if version resource contains text data and 0 if version resource contains binary data)
 * Variable length unicode string: Key ("Translation")
 * Variable length padding to align Value on a 32-bit boundary
 * Value: an array of one or more 4 byte values that are language and code page identifier pairs, low-order word containing a language identifier, and the high-order word containing a code page number.  Either word can be zero, indicating that the file is language or code page independent.
 */

#if G_BYTE_ORDER == G_BIG_ENDIAN
#define VS_FFI_SIGNATURE	0xbd04effe
#define VS_FFI_STRUCVERSION	0x00000100
#else
#define VS_FFI_SIGNATURE	0xfeef04bd
#define VS_FFI_STRUCVERSION	0x00010000
#endif

#define VOS_UNKNOWN		0x00000000
#define VOS_DOS			0x00010000
#define VOS_OS216		0x00020000
#define VOS_OS232		0x00030000
#define VOS_NT			0x00040000
#define VOS__BASE		0x00000000
#define VOS__WINDOWS16		0x00000001
#define VOS__PM16		0x00000002
#define VOS__PM32		0x00000003
#define VOS__WINDOWS32		0x00000004
/* Should "embrace and extend" here with some entries for linux etc */

#define VOS_DOS_WINDOWS16	0x00010001
#define VOS_DOS_WINDOWS32	0x00010004
#define VOS_OS216_PM16		0x00020002
#define VOS_OS232_PM32		0x00030003
#define VOS_NT_WINDOWS32	0x00040004

#define VFT_UNKNOWN		0x0000
#define VFT_APP			0x0001
#define VFT_DLL			0x0002
#define VFT_DRV			0x0003
#define VFT_FONT		0x0004
#define VFT_VXD			0x0005
#define VFT_STATIC_LIB		0x0007

#define VFT2_UNKNOWN		0x0000
#define VFT2_DRV_PRINTER	0x0001
#define VFT2_DRV_KEYBOARD	0x0002
#define VFT2_DRV_LANGUAGE	0x0003
#define VFT2_DRV_DISPLAY	0x0004
#define VFT2_DRV_MOUSE		0x0005
#define VFT2_DRV_NETWORK	0x0006
#define VFT2_DRV_SYSTEM		0x0007
#define VFT2_DRV_INSTALLABLE	0x0008
#define VFT2_DRV_SOUND		0x0009
#define VFT2_DRV_COMM		0x000a
#define VFT2_DRV_INPUTMETHOD	0x000b
#define VFT2_FONT_RASTER	0x0001
#define VFT2_FONT_VECTOR	0x0002
#define VFT2_FONT_TRUETYPE	0x0003

#define MAKELANGID(primary,secondary) ((guint16)((secondary << 10) | (primary)))

#define ALIGN32(ptr) ptr = (gpointer)((char *)ptr + 3); ptr = (gpointer)((char *)ptr - ((gsize)ptr & 3));

#if HAVE_SIGACTION
static mono_lazy_init_t process_sig_chld_once = MONO_LAZY_INIT_STATUS_NOT_INITIALIZED;
#endif

static gchar *cli_launcher;

static Process *processes;
static MonoCoopMutex processes_mutex;

static pid_t current_pid;
static gpointer current_process;

static const gunichar2 utf16_space [2] = { 0x20, 0 };
static const gunichar2 utf16_quote [2] = { 0x22, 0 };

static MonoBoolean
mono_get_exit_code_process (gpointer handle, gint32 *exitcode);

/* Check if a pid is valid - i.e. if a process exists with this pid. */
static gboolean
process_is_alive (pid_t pid)
{
#if defined(HOST_WATCHOS)
	return TRUE; // TODO: Rewrite using sysctl
#elif defined(HOST_DARWIN) || defined(__OpenBSD__) || defined(__FreeBSD__) || defined(_AIX)
	if (pid == 0)
		return FALSE;
	if (kill (pid, 0) == 0)
		return TRUE;
	if (errno == EPERM)
		return TRUE;
	return FALSE;
#elif defined(__HAIKU__)
	team_info teamInfo;
	if (get_team_info ((team_id)pid, &teamInfo) == B_OK)
		return TRUE;
	return FALSE;
#else
	gchar *dir = g_strdup_printf ("/proc/%d", pid);
	gboolean result = access (dir, F_OK) == 0;
	g_free (dir);
	return result;
#endif
}

static void
process_details (MonoW32Handle *handle_data)
{
	MonoW32HandleProcess *process_handle = (MonoW32HandleProcess *) handle_data->specific;
	g_print ("pid: %d, exited: %s, exitstatus: %d",
		process_handle->pid, process_handle->exited ? "true" : "false", process_handle->exitstatus);
}

static const gchar*
process_typename (void)
{
	return "Process";
}

static gsize
process_typesize (void)
{
	return sizeof (MonoW32HandleProcess);
}

static MonoW32HandleWaitRet
process_wait (MonoW32Handle *handle_data, guint32 timeout, gboolean *alerted)
{
	MonoW32HandleProcess *process_handle;
	pid_t pid G_GNUC_UNUSED, ret;
	int status;
	gint64 start, now;
	Process *process;

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s (%p, %" G_GUINT32_FORMAT ")", __func__, handle_data, timeout);

	if (alerted)
		*alerted = FALSE;

	process_handle = (MonoW32HandleProcess*) handle_data->specific;

	if (process_handle->exited) {
		/* We've already done this one */
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s (%p, %" G_GUINT32_FORMAT "): Process already exited", __func__, handle_data, timeout);
		return MONO_W32HANDLE_WAIT_RET_SUCCESS_0;
	}

	pid = process_handle->pid;

	if (pid == mono_process_current_pid ()) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s (%p, %" G_GUINT32_FORMAT "): waiting on current process", __func__, handle_data, timeout);
		return MONO_W32HANDLE_WAIT_RET_TIMEOUT;
	}

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s (%p, %" G_GUINT32_FORMAT "): PID: %d", __func__, handle_data, timeout, pid);

	if (!process_handle->child) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s (%p, %" G_GUINT32_FORMAT "): waiting on non-child process", __func__, handle_data, timeout);

		if (!process_is_alive (pid)) {
			/* assume the process has exited */
			process_handle->exited = TRUE;
			process_handle->exitstatus = -1;
			mono_w32handle_set_signal_state (handle_data, TRUE, TRUE);

			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s (%p, %" G_GUINT32_FORMAT "): non-child process is not alive anymore (2)", __func__, handle_data, timeout);
			return MONO_W32HANDLE_WAIT_RET_SUCCESS_0;
		}

		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s (%p, %" G_GUINT32_FORMAT "): non-child process wait failed, error : %s (%d))", __func__, handle_data, timeout, g_strerror (errno), errno);
		return MONO_W32HANDLE_WAIT_RET_FAILED;
	}

	/* We don't need to lock processes here, the entry
	 * has a handle_count > 0 which means it will not be freed. */
	process = process_handle->process;
	g_assert (process);

	start = mono_msec_ticks ();
	now = start;

	while (1) {
		if (timeout != MONO_INFINITE_WAIT) {
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s (%p, %" G_GUINT32_FORMAT "): waiting on semaphore for %" G_GINT64_FORMAT " ms...",
				__func__, handle_data, timeout, timeout - (now - start));
			ret = mono_coop_sem_timedwait (&process->exit_sem, (timeout - (now - start)), alerted ? MONO_SEM_FLAGS_ALERTABLE : MONO_SEM_FLAGS_NONE);
		} else {
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s (%p, %" G_GUINT32_FORMAT "): waiting on semaphore forever...",
				__func__, handle_data, timeout);
			ret = mono_coop_sem_wait (&process->exit_sem, alerted ? MONO_SEM_FLAGS_ALERTABLE : MONO_SEM_FLAGS_NONE);
		}

		if (ret == MONO_SEM_TIMEDWAIT_RET_SUCCESS) {
			/* Success, process has exited */
			mono_coop_sem_post (&process->exit_sem);
			break;
		}

		if (ret == MONO_SEM_TIMEDWAIT_RET_TIMEDOUT) {
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s (%p, %" G_GUINT32_FORMAT "): wait timeout (timeout = 0)", __func__, handle_data, timeout);
			return MONO_W32HANDLE_WAIT_RET_TIMEOUT;
		}

		now = mono_msec_ticks ();
		if (now - start >= timeout) {
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s (%p, %" G_GUINT32_FORMAT "): wait timeout", __func__, handle_data, timeout);
			return MONO_W32HANDLE_WAIT_RET_TIMEOUT;
		}

		if (alerted && ret == MONO_SEM_TIMEDWAIT_RET_ALERTED) {
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s (%p, %" G_GUINT32_FORMAT "): wait alerted", __func__, handle_data, timeout);
			*alerted = TRUE;
			return MONO_W32HANDLE_WAIT_RET_ALERTED;
		}
	}

	/* Process must have exited */
	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s (%p, %" G_GUINT32_FORMAT "): Waited successfully", __func__, handle_data, timeout);

	status = process->status;
	if (WIFSIGNALED (status))
		process_handle->exitstatus = 128 + WTERMSIG (status);
	else
		process_handle->exitstatus = WEXITSTATUS (status);

	process_handle->exit_time = mono_100ns_datetime ();

	process_handle->exited = TRUE;

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s (%p, %" G_GUINT32_FORMAT "): Setting pid %d signalled, exit status %d",
		   __func__, handle_data, timeout, process_handle->pid, process_handle->exitstatus);

	mono_w32handle_set_signal_state (handle_data, TRUE, TRUE);

	return MONO_W32HANDLE_WAIT_RET_SUCCESS_0;
}

static void
processes_cleanup (void)
{
	static gint32 cleaning_up;
	Process *process;
	Process *prev = NULL;

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s", __func__);

	/* Ensure we're not in here in multiple threads at once, nor recursive. */
	if (mono_atomic_cas_i32 (&cleaning_up, 1, 0) != 0)
		return;

	/*
	 * This needs to be done outside the lock but atomically, hence the CAS above.
	 */
	for (process = processes; process; process = process->next) {
		if (process->signalled && process->handle) {
			/* This process has exited and we need to remove the artifical ref
			 * on the handle */
			mono_w32handle_close (process->handle);
			process->handle = NULL;
		}
	}

	mono_coop_mutex_lock (&processes_mutex);

	for (process = processes; process;) {
		Process *next = process->next;
		if (process->handle_count == 0 && process->signalled) {
			/*
			 * Unlink the entry.
			 */
			if (process == processes)
				processes = process->next;
			else
				prev->next = process->next;

			mono_coop_sem_destroy (&process->exit_sem);
			g_free (process);
		} else {
			prev = process;
		}
		process = next;
	}

	mono_coop_mutex_unlock (&processes_mutex);

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s done", __func__);

	mono_atomic_xchg_i32 (&cleaning_up, 0);
}

static void
process_close (gpointer data)
{
	MonoW32HandleProcess *process_handle;

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s", __func__);

	process_handle = (MonoW32HandleProcess *) data;
	g_free (process_handle->pname);
	process_handle->pname = NULL;
	if (process_handle->process)
		mono_atomic_dec_i32 (&process_handle->process->handle_count);
	processes_cleanup ();
}

static const MonoW32HandleOps process_ops = {
	process_close,		/* close_shared */
	NULL,				/* signal */
	NULL,				/* own */
	NULL,				/* is_owned */
	process_wait,			/* special_wait */
	NULL,				/* prewait */
	process_details,	/* details */
	process_typename,	/* typename */
	process_typesize,	/* typesize */
};

static void
process_set_defaults (MonoW32HandleProcess *process_handle)
{
	/* These seem to be the defaults on w2k */
	process_handle->min_working_set = 204800;
	process_handle->max_working_set = 1413120;

	process_handle->create_time = mono_100ns_datetime ();
}

static void
process_set_name (MonoW32HandleProcess *process_handle)
{
	char *progname, *utf8_progname, *slash;

	progname = g_get_prgname ();
	utf8_progname = mono_utf8_from_external (progname);

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: using [%s] as prog name", __func__, progname);

	if (utf8_progname) {
		slash = strrchr (utf8_progname, '/');
		if (slash)
			process_handle->pname = g_strdup (slash+1);
		else
			process_handle->pname = g_strdup (utf8_progname);
		g_free (utf8_progname);
	}
}

void
mono_w32process_init (void)
{
	MonoW32HandleProcess process_handle;

	mono_w32handle_register_ops (MONO_W32TYPE_PROCESS, &process_ops);

	mono_w32handle_register_capabilities (MONO_W32TYPE_PROCESS,
		(MonoW32HandleCapability)(MONO_W32HANDLE_CAP_WAIT | MONO_W32HANDLE_CAP_SPECIAL_WAIT));

	current_pid = getpid ();

	memset (&process_handle, 0, sizeof (process_handle));
	process_handle.pid = current_pid;
	process_set_defaults (&process_handle);
	process_set_name (&process_handle);

	current_process = mono_w32handle_new (MONO_W32TYPE_PROCESS, &process_handle);
	g_assert (current_process != INVALID_HANDLE_VALUE);

	mono_coop_mutex_init (&processes_mutex);
}

void
mono_w32process_cleanup (void)
{
	g_free (cli_launcher);
}

static int
len16 (const gunichar2 *str)
{
	int len = 0;

	while (*str++ != 0)
		len++;

	return len;
}

static gunichar2 *
utf16_concat (const gunichar2 *first, ...)
{
	va_list args;
	int total = 0, i;
	const gunichar2 *s;
	const gunichar2 *p;
	gunichar2 *ret;

	va_start (args, first);
	total += len16 (first);
	for (s = va_arg (args, gunichar2 *); s != NULL; s = va_arg(args, gunichar2 *))
		total += len16 (s);
	va_end (args);

	ret = g_new (gunichar2, total + 1);
	if (ret == NULL)
		return NULL;

	ret [total] = 0;
	i = 0;
	for (s = first; *s != 0; s++)
		ret [i++] = *s;
	va_start (args, first);
	for (s = va_arg (args, gunichar2 *); s != NULL; s = va_arg (args, gunichar2 *)){
		for (p = s; *p != 0; p++)
			ret [i++] = *p;
	}
	va_end (args);

	return ret;
}

guint32
mono_w32process_get_pid (gpointer handle)
{
	MonoW32Handle *handle_data;
	guint32 ret;

	if (!mono_w32handle_lookup_and_ref (handle, &handle_data)) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: unknown handle %p", __func__, handle);
		mono_w32error_set_last (ERROR_INVALID_HANDLE);
		return 0;
	}

	if (handle_data->type != MONO_W32TYPE_PROCESS) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: unknown process handle %p", __func__, handle);
		mono_w32error_set_last (ERROR_INVALID_HANDLE);
		mono_w32handle_unref (handle_data);
		return 0;
	}

	ret = ((MonoW32HandleProcess*) handle_data->specific)->pid;

	mono_w32handle_unref (handle_data);

	return ret;
}

typedef struct {
	guint32 pid;
	gpointer handle;
} GetProcessForeachData;

static gboolean
get_process_foreach_callback (MonoW32Handle *handle_data, gpointer user_data)
{
	GetProcessForeachData *foreach_data;
	MonoW32HandleProcess *process_handle;
	pid_t pid;

	if (handle_data->type != MONO_W32TYPE_PROCESS)
		return FALSE;

	process_handle = (MonoW32HandleProcess*) handle_data->specific;

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: looking at process %d", __func__, process_handle->pid);

	pid = process_handle->pid;
	if (pid == 0)
		return FALSE;

	foreach_data = (GetProcessForeachData*) user_data;

	/* It's possible to have more than one process handle with the
	 * same pid, but only the one running process can be
	 * unsignalled. */
	if (foreach_data->pid != pid)
		return FALSE;
	if (mono_w32handle_issignalled (handle_data))
		return FALSE;

	foreach_data->handle = mono_w32handle_duplicate (handle_data);
	return TRUE;
}

HANDLE
ves_icall_System_Diagnostics_Process_GetProcess_internal (guint32 pid)
{
	GetProcessForeachData foreach_data;
	gpointer handle;

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: looking for process %d", __func__, pid);

	memset (&foreach_data, 0, sizeof (foreach_data));
	foreach_data.pid = pid;
	mono_w32handle_foreach (get_process_foreach_callback, &foreach_data);
	handle = foreach_data.handle;
	if (handle) {
		/* get_process_foreach_callback already added a ref */
		return handle;
	}

	if (process_is_alive (pid)) {
		/* non-child process */
		MonoW32HandleProcess process_handle;

		memset (&process_handle, 0, sizeof (process_handle));
		process_handle.pid = pid;
		process_handle.pname = mono_w32process_get_name (pid);

		handle = mono_w32handle_new (MONO_W32TYPE_PROCESS, &process_handle);
		if (handle == INVALID_HANDLE_VALUE) {
			g_warning ("%s: error creating process handle", __func__);

			mono_w32error_set_last (ERROR_OUTOFMEMORY);
			return NULL;
		}

		return handle;
	}

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: Can't find pid %d", __func__, pid);

	mono_w32error_set_last (ERROR_PROC_NOT_FOUND);
	return NULL;
}

static gboolean
match_procname_to_modulename (char *procname, char *modulename)
{
	char* lastsep = NULL;
	char* lastsep2 = NULL;
	char* pname = NULL;
	char* mname = NULL;
	gboolean result = FALSE;

	if (procname == NULL || modulename == NULL)
		return (FALSE);

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: procname=\"%s\", modulename=\"%s\"", __func__, procname, modulename);
	pname = mono_path_resolve_symlinks (procname);
	mname = mono_path_resolve_symlinks (modulename);

	if (!strcmp (pname, mname))
		result = TRUE;

	if (!result) {
		lastsep = strrchr (mname, '/');
		if (lastsep)
			if (!strcmp (lastsep+1, pname))
				result = TRUE;
		if (!result) {
			lastsep2 = strrchr (pname, '/');
			if (lastsep2){
				if (lastsep) {
					if (!strcmp (lastsep+1, lastsep2+1))
						result = TRUE;
				} else {
					if (!strcmp (mname, lastsep2+1))
						result = TRUE;
				}
			}
		}
	}

	g_free (pname);
	g_free (mname);

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: result is %" G_GINT32_FORMAT, __func__, result);
	return result;
}

gboolean
mono_w32process_try_get_modules (gpointer handle, gpointer *modules, guint32 size, guint32 *needed)
{
	MonoW32Handle *handle_data;
	MonoW32HandleProcess *process_handle;
	GSList *mods = NULL, *mods_iter;
	MonoW32ProcessModule *module;
	guint32 count, avail = size / sizeof(gpointer);
	int i;
	pid_t pid;
	char *pname = NULL;

	/* Store modules in an array of pointers (main module as
	 * modules[0]), using the load address for each module as a
	 * token.  (Use 'NULL' as an alternative for the main module
	 * so that the simple implementation can just return one item
	 * for now.)  Get the info from /proc/<pid>/maps on linux,
	 * /proc/<pid>/map on FreeBSD, other systems will have to
	 * implement /dev/kmem reading or whatever other horrid
	 * technique is needed.
	 */
	if (size < sizeof(gpointer))
		return FALSE;

	if (!mono_w32handle_lookup_and_ref (handle, &handle_data)) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: unknown handle %p", __func__, handle);
		mono_w32error_set_last (ERROR_INVALID_HANDLE);
		return FALSE;
	}

	if (handle_data->type != MONO_W32TYPE_PROCESS) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: unknown process handle %p", __func__, handle);
		mono_w32error_set_last (ERROR_INVALID_HANDLE);
		mono_w32handle_unref (handle_data);
		return FALSE;
	}

	process_handle = (MonoW32HandleProcess*) handle_data->specific;

	pid = process_handle->pid;
	pname = g_strdup (process_handle->pname);

	if (!pname) {
		modules[0] = NULL;
		*needed = sizeof(gpointer);
		mono_w32handle_unref (handle_data);
		return TRUE;
	}

	mods = mono_w32process_get_modules (pid);
	if (!mods) {
		modules[0] = NULL;
		*needed = sizeof(gpointer);
		g_free (pname);
		mono_w32handle_unref (handle_data);
		return TRUE;
	}

	count = 0;

	/*
	 * Use the NULL shortcut, as the first line in
	 * /proc/<pid>/maps isn't the executable, and we need
	 * that first in the returned list. Check the module name
	 * to see if it ends with the proc name and substitute
	 * the first entry with it.  FIXME if this turns out to
	 * be a problem.
	 */
	modules[0] = NULL;
	mods_iter = mods;
	for (i = 0; mods_iter; i++) {
		if (i < avail - 1) {
			module = (MonoW32ProcessModule *)mods_iter->data;
			if (modules[0] != NULL)
				modules[i] = module->address_start;
			else if (match_procname_to_modulename (pname, module->filename))
				modules[0] = module->address_start;
			else
				modules[i + 1] = module->address_start;
		}
		mono_w32process_module_free ((MonoW32ProcessModule *)mods_iter->data);
		mods_iter = g_slist_next (mods_iter);
		count++;
	}

	/* count + 1 to leave slot 0 for the main module */
	*needed = sizeof(gpointer) * (count + 1);

	g_slist_free (mods);
	g_free (pname);
	mono_w32handle_unref (handle_data);
	return TRUE;
}

guint32
mono_w32process_module_get_filename (gpointer handle, gpointer module, gunichar2 *basename, guint32 size)
{
	gint pid, len;
	gsize bytes;
	gchar *path;
	gunichar2 *proc_path;

	size *= sizeof (gunichar2); /* adjust for unicode characters */

	if (basename == NULL || size == 0)
		return 0;

	pid = mono_w32process_get_pid (handle);

	path = mono_w32process_get_path (pid);
	if (path == NULL)
		return 0;

	proc_path = mono_unicode_from_external (path, &bytes);
	g_free (path);

	if (proc_path == NULL)
		return 0;

	len = (bytes / 2);

	/* Add the terminator */
	bytes += 2;

	if (size < bytes) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: Size %" G_GUINT32_FORMAT " smaller than needed (%zd); truncating", __func__, size, bytes);
		memcpy (basename, proc_path, size);
	} else {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: Size %" G_GUINT32_FORMAT " larger than needed (%zd)", __func__, size, bytes);
		memcpy (basename, proc_path, bytes);
	}

	g_free (proc_path);

	return len;
}

guint32
mono_w32process_module_get_name (gpointer handle, gpointer module, gunichar2 *basename, guint32 size)
{
	MonoW32Handle *handle_data;
	MonoW32HandleProcess *process_handle;
	pid_t pid;
	gunichar2 *procname;
	char *procname_ext = NULL;
	glong len;
	gsize bytes;
	GSList *mods = NULL, *mods_iter;
	MonoW32ProcessModule *found_module;
	char *pname = NULL;

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: Getting module base name, process handle %p module %p basename %p size %" G_GUINT32_FORMAT,
		   __func__, handle, module, basename, size);

	size = size * sizeof (gunichar2); /* adjust for unicode characters */

	if (basename == NULL || size == 0)
		return 0;

	if (!mono_w32handle_lookup_and_ref (handle, &handle_data)) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: unknown handle %p", __func__, handle);
		mono_w32error_set_last (ERROR_INVALID_HANDLE);
		return 0;
	}

	if (handle_data->type != MONO_W32TYPE_PROCESS) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: unknown process handle %p", __func__, handle);
		mono_w32error_set_last (ERROR_INVALID_HANDLE);
		mono_w32handle_unref (handle_data);
		return 0;
	}

	process_handle = (MonoW32HandleProcess*) handle_data->specific;

	pid = process_handle->pid;
	pname = g_strdup (process_handle->pname);

	mods = mono_w32process_get_modules (pid);
	if (!mods && module != NULL) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: Can't get modules %p", __func__, handle);
		g_free (pname);
		mono_w32handle_unref (handle_data);
		return 0;
	}

	/* If module != NULL compare the address.
	 * If module == NULL we are looking for the main module.
	 * The best we can do for now check it the module name end with the process name.
	 */
	for (mods_iter = mods; mods_iter; mods_iter = g_slist_next (mods_iter)) {
		found_module = (MonoW32ProcessModule *)mods_iter->data;
		if (procname_ext == NULL &&
			((module == NULL && match_procname_to_modulename (pname, found_module->filename)) ||
			 (module != NULL && found_module->address_start == module))) {
			procname_ext = g_path_get_basename (found_module->filename);
		}

		mono_w32process_module_free (found_module);
	}

	if (procname_ext == NULL) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: Can't find procname_ext from procmods %p", __func__, handle);
		/* If it's *still* null, we might have hit the
		 * case where reading /proc/$pid/maps gives an
		 * empty file for this user.
		 */
		procname_ext = mono_w32process_get_name (pid);
		if (!procname_ext)
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: Can't find procname_ext from proc_get_name %p pid %d", __func__, handle, pid);
	}

	g_slist_free (mods);
	g_free (pname);

	if (procname_ext) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: Process name is [%s]", __func__,
			   procname_ext);

		procname = mono_unicode_from_external (procname_ext, &bytes);
		if (procname == NULL) {
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: Can't get procname %p", __func__, handle);
			/* bugger */
			g_free (procname_ext);
			mono_w32handle_unref (handle_data);
			return 0;
		}

		len = (bytes / 2);

		/* Add the terminator */
		bytes += 2;

		if (size < bytes) {
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: Size %" G_GUINT32_FORMAT " smaller than needed (%zd); truncating", __func__, size, bytes);

			memcpy (basename, procname, size);
		} else {
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: Size %" G_GUINT32_FORMAT " larger than needed (%zd)",
				   __func__, size, bytes);

			memcpy (basename, procname, bytes);
		}

		g_free (procname);
		g_free (procname_ext);

		mono_w32handle_unref (handle_data);
		return len;
	}

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: Can't find procname_ext %p", __func__, handle);
	mono_w32handle_unref (handle_data);
	return 0;
}

gboolean
mono_w32process_module_get_information (gpointer handle, gpointer module, MODULEINFO *modinfo, guint32 size)
{
	MonoW32Handle *handle_data;
	MonoW32HandleProcess *process_handle;
	pid_t pid;
	GSList *mods = NULL, *mods_iter;
	MonoW32ProcessModule *found_module;
	gboolean ret = FALSE;
	char *pname = NULL;

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: Getting module info, process handle %p module %p",
		   __func__, handle, module);

	if (modinfo == NULL || size < sizeof (MODULEINFO))
		return FALSE;

	if (!mono_w32handle_lookup_and_ref (handle, &handle_data)) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: unknown handle %p", __func__, handle);
		mono_w32error_set_last (ERROR_INVALID_HANDLE);
		return FALSE;
	}

	if (handle_data->type != MONO_W32TYPE_PROCESS) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: unknown process handle %p", __func__, handle);
		mono_w32error_set_last (ERROR_INVALID_HANDLE);
		mono_w32handle_unref (handle_data);
		return FALSE;
	}

	process_handle = (MonoW32HandleProcess*) handle_data->specific;

	pid = process_handle->pid;
	pname = g_strdup (process_handle->pname);

	mods = mono_w32process_get_modules (pid);
	if (!mods) {
		g_free (pname);
		mono_w32handle_unref (handle_data);
		return FALSE;
	}

	/* If module != NULL compare the address.
	 * If module == NULL we are looking for the main module.
	 * The best we can do for now check it the module name end with the process name.
	 */
	for (mods_iter = mods; mods_iter; mods_iter = g_slist_next (mods_iter)) {
			found_module = (MonoW32ProcessModule *)mods_iter->data;
			if (ret == FALSE &&
				((module == NULL && match_procname_to_modulename (pname, found_module->filename)) ||
				 (module != NULL && found_module->address_start == module))) {
				modinfo->lpBaseOfDll = found_module->address_start;
				modinfo->SizeOfImage = (gsize)(found_module->address_end) - (gsize)(found_module->address_start);
				modinfo->EntryPoint = found_module->address_offset;
				ret = TRUE;
			}

			mono_w32process_module_free (found_module);
	}

	g_slist_free (mods);
	g_free (pname);
	mono_w32handle_unref (handle_data);
	return ret;
}

static void
switch_dir_separators (char *path)
{
	size_t i, pathLength = strlen(path);
	
	/* Turn all the slashes round the right way, except for \' */
	/* There are probably other characters that need to be excluded as well. */
	for (i = 0; i < pathLength; i++) {
		if (path[i] == '\\' && i < pathLength - 1 && path[i+1] != '\'' )
			path[i] = '/';
	}
}

#if HAVE_SIGACTION

MONO_SIGNAL_HANDLER_FUNC (static, mono_sigchld_signal_handler, (int _dummy, siginfo_t *info, void *context))
{
	/*
	 * Don't want to do any complicated processing here so just wake up the finalizer thread which will call
	 * mono_w32process_signal_finished ().
	 */
	int old_errno = errno;

	mono_gc_finalize_notify ();

	mono_set_errno (old_errno);
}

static void
process_add_sigchld_handler (void)
{
	struct sigaction sa;

	sa.sa_sigaction = mono_sigchld_signal_handler;
	sigemptyset (&sa.sa_mask);
	sa.sa_flags = SA_NOCLDSTOP | SA_SIGINFO | SA_RESTART;
	g_assert (sigaction (SIGCHLD, &sa, NULL) != -1);
	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "Added SIGCHLD handler");
}

#endif

/*
 * mono_w32process_signal_finished:
 *
 *   Signal the exit semaphore for processes which have finished.
 */
void
mono_w32process_signal_finished (void)
{
	mono_coop_mutex_lock (&processes_mutex);

	for (Process* process = processes; process; process = process->next) {
		int status = -1;
		int pid;

		do {
			pid = waitpid (process->pid, &status, WNOHANG);
		} while (pid == -1 && errno == EINTR);

		// possible values of 'pid':
		//  process->pid : the status changed for this child
		//  0            : status unchanged for this PID
		//  ECHILD       : process has been reaped elsewhere (or never existed)
		//  EINVAL       : invalid PID or other argument

		// Therefore, we ignore status unchanged (nothing to do) and error
		// events (process is cleaned up later).
		if (pid <= 0)
			continue;
		if (process->signalled)
			continue;

		process->signalled = TRUE;
		process->status = status;
		mono_coop_sem_post (&process->exit_sem);
	}

	mono_coop_mutex_unlock (&processes_mutex);
}

static gboolean
is_readable_or_executable (const char *prog)
{
	struct stat buf;
	int a = access (prog, R_OK);
	int b = access (prog, X_OK);
	if (a != 0 && b != 0)
		return FALSE;
	if (stat (prog, &buf))
		return FALSE;
	if (S_ISREG (buf.st_mode))
		return TRUE;
	return FALSE;
}

static gboolean
is_executable (const char *prog)
{
	struct stat buf;
	if (access (prog, X_OK) != 0)
		return FALSE;
	if (stat (prog, &buf))
		return FALSE;
	if (S_ISREG (buf.st_mode))
		return TRUE;
	return FALSE;
}

static gboolean
is_managed_binary (const char *filename)
{
	int original_errno = errno;
#if defined(HAVE_LARGE_FILE_SUPPORT) && defined(O_LARGEFILE)
	int file = open (filename, O_RDONLY | O_LARGEFILE);
#else
	int file = open (filename, O_RDONLY);
#endif
	off_t new_offset;
	unsigned char buffer[8];
	off_t file_size, optional_header_offset;
	off_t pe_header_offset, clr_header_offset;
	gboolean managed = FALSE;
	int num_read;
	guint32 first_word, second_word, magic_number;
	
	/* If we are unable to open the file, then we definitely
	 * can't say that it is managed. The child mono process
	 * probably wouldn't be able to open it anyway.
	 */
	if (file < 0) {
		mono_set_errno (original_errno);
		return FALSE;
	}

	/* Retrieve the length of the file for future sanity checks. */
	file_size = lseek (file, 0, SEEK_END);
	lseek (file, 0, SEEK_SET);

	/* We know we need to read a header field at offset 60. */
	if (file_size < 64)
		goto leave;

	num_read = read (file, buffer, 2);

	if ((num_read != 2) || (buffer[0] != 'M') || (buffer[1] != 'Z'))
		goto leave;

	new_offset = lseek (file, 60, SEEK_SET);

	if (new_offset != 60)
		goto leave;
	
	num_read = read (file, buffer, 4);

	if (num_read != 4)
		goto leave;
	pe_header_offset =  buffer[0]
		| (buffer[1] <<  8)
		| (buffer[2] << 16)
		| (buffer[3] << 24);
	
	if (pe_header_offset + 24 > file_size)
		goto leave;

	new_offset = lseek (file, pe_header_offset, SEEK_SET);

	if (new_offset != pe_header_offset)
		goto leave;

	num_read = read (file, buffer, 4);

	if ((num_read != 4) || (buffer[0] != 'P') || (buffer[1] != 'E') || (buffer[2] != 0) || (buffer[3] != 0))
		goto leave;

	/*
	 * Verify that the header we want in the optional header data
	 * is present in this binary.
	 */
	new_offset = lseek (file, pe_header_offset + 20, SEEK_SET);

	if (new_offset != pe_header_offset + 20)
		goto leave;

	num_read = read (file, buffer, 2);

	if ((num_read != 2) || ((buffer[0] | (buffer[1] << 8)) < 216))
		goto leave;

	optional_header_offset = pe_header_offset + 24;

	/* Read the PE magic number */
	new_offset = lseek (file, optional_header_offset, SEEK_SET);
	
	if (new_offset != optional_header_offset)
		goto leave;

	num_read = read (file, buffer, 2);

	if (num_read != 2)
		goto leave;

	magic_number = (buffer[0] | (buffer[1] << 8));
	
	if (magic_number == 0x10B)  // PE32
		clr_header_offset = 208;
	else if (magic_number == 0x20B)  // PE32+
		clr_header_offset = 224;
	else
		goto leave;

	/* Read the CLR header address and size fields. These will be
	 * zero if the binary is not managed.
	 */
	new_offset = lseek (file, optional_header_offset + clr_header_offset, SEEK_SET);

	if (new_offset != optional_header_offset + clr_header_offset)
		goto leave;

	num_read = read (file, buffer, 8);
	
	/* We are not concerned with endianness, only with
	 * whether it is zero or not.
	 */
	first_word = *(guint32 *)&buffer[0];
	second_word = *(guint32 *)&buffer[4];
	
	if ((num_read != 8) || (first_word == 0) || (second_word == 0))
		goto leave;
	
	managed = TRUE;

leave:
	close (file);
	mono_set_errno (original_errno);
	return managed;
}

/**
 * Gets the biggest numbered file descriptor for the current process; failing
 * that, the system's file descriptor limit. This is called by the fork child
 * in close_my_fds.
 */
static inline guint32
max_fd_count (void)
{
#if defined (_AIX)
	struct procentry64 pe;
	pid_t p;
	p = getpid ();
	if (getprocs64 (&pe, sizeof (pe), NULL, 0, &p, 1) != -1) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS,
			   "%s: maximum returned fd in child is %u",
			   __func__, pe.pi_maxofile);
		return pe.pi_maxofile; // biggest + 1
	}
#endif
	// fallback to user/system limit if unsupported/error
	return eg_getdtablesize ();
}

/**
 * Closes all of the process' opened file descriptors, applying a strategy
 * appropriate for the target system. This is called by the fork child in
 * process_create.
 */
static void
close_my_fds (void)
{
// TODO: Other platforms.
//       * On macOS, use proc_pidinfo + PROC_PIDLISTFDS? See:
//         http://blog.palominolabs.com/2012/06/19/getting-the-files-being-used-by-a-process-on-mac-os-x/
//         (I have no idea how this plays out on i/watch/tvOS.)
//       * On the other BSDs, there's likely a sysctl for this.
//       * On Solaris, there exists posix_spawn_file_actions_addclosefrom_np,
//         but that assumes we're using posix_spawn; we aren't, as we do some
//         complex stuff between fork and exec. There's likely a way to get
//         the FD list/count though (maybe look at addclosefrom source in
//         illumos?) or just walk /proc/pid/fd like Linux?
#if defined (__linux__)
	/* Walk the file descriptors in /proc/self/fd/. Linux has no other API,
	 * as far as I'm aware. Opening a directory won't create an FD. */
	struct dirent *dp;
	DIR *d;
	int fd;
	d = opendir ("/proc/self/fd/");
	if (d) {
		while ((dp = readdir (d)) != NULL) {
			if (dp->d_name [0] == '.')
				continue;
			fd = atoi (dp->d_name);
			if (fd > 2)
				close (fd);
		}
		closedir (d);
		return;
	} else {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS,
			   "%s: opening fd dir failed, using fallback",
			   __func__);
	}
#elif defined (__FreeBSD__)
	/* FreeBSD lets us get a list of FDs. There's a MIB to access them
	 * directly, but it uses a lot of nasty variable length structures. The
	 * system library libutil provides a nicer way to get a fixed length
	 * version instead. */
	struct kinfo_file *kif;
	int count, i;
	/* this is malloced but we won't need to free once we exec/exit */
	kif = kinfo_getfile (getpid (), &count);
	if (kif) {
		for (i = 0; i < count; i++) {
			/* negative FDs look to be used by the OS */
			if (kif [i].kf_fd > 2) /* no neg + no stdio */
				close (kif [i].kf_fd);
		}
		return;
	} else {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS,
			   "%s: kinfo_getfile failed, using fallback",
			   __func__);
	}
#elif defined (_AIX)
	struct procentry64 pe;
	/* this array struct is 1 MB, we're NOT putting it on the stack.
	 * likewise no need to free; getprocs will fail if we use the smalller
	 * versions if we have a lot of FDs (is it worth it?)
	 */
	struct fdsinfo_100K *fds;
	pid_t p;
	p = getpid ();
	fds = (struct fdsinfo_100K *) g_malloc0 (sizeof (struct fdsinfo_100K));
	if (!fds) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS,
			   "%s: fdsinfo alloc failed, using fallback",
			   __func__);
		goto fallback;
	}

	if (getprocs64 (&pe, sizeof (pe), fds, sizeof (struct fdsinfo_100K), &p, 1) != -1) {
		for (int i = 3; i < pe.pi_maxofile; i++) {
			if (fds->pi_ufd [i].fp != 0)
				close (fds->pi_ufd [i].fp);
		}
		return;
	} else {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS,
			   "%s: getprocs64 failed, using fallback",
			   __func__);
	}
fallback:
#endif
	/* Fallback: Close FDs blindly, according to an FD limit */
	for (guint32 i = max_fd_count () - 1; i > 2; i--)
		close (i);
}

static gboolean
process_create (const gunichar2 *appname, const gunichar2 *cmdline,
	const gunichar2 *cwd, StartupHandles *startup_handles, MonoW32ProcessInfo *process_info)
{
#if defined (HAVE_FORK) && defined (HAVE_EXECVE)
	char *cmd = NULL, *prog = NULL, *full_prog = NULL, *args = NULL, *args_after_prog = NULL;
	char *dir = NULL, **env_strings = NULL, **argv = NULL;
	guint32 i;
	gboolean ret = FALSE;
	gpointer handle = NULL;
	GError *gerr = NULL;
	int in_fd, out_fd, err_fd;
	pid_t pid = 0;
	int startup_pipe [2] = {-1, -1};
	int dummy;
	Process *process;

#if HAVE_SIGACTION
	mono_lazy_initialize (&process_sig_chld_once, process_add_sigchld_handler);
#endif

	/* appname and cmdline specify the executable and its args:
	 *
	 * If appname is not NULL, it is the name of the executable.
	 * Otherwise the executable is the first token in cmdline.
	 *
	 * Executable searching:
	 *
	 * If appname is not NULL, it can specify the full path and
	 * file name, or else a partial name and the current directory
	 * will be used.  There is no additional searching.
	 *
	 * If appname is NULL, the first whitespace-delimited token in
	 * cmdline is used.  If the name does not contain a full
	 * directory path, the search sequence is:
	 *
	 * 1) The directory containing the current process
	 * 2) The current working directory
	 * 3) The windows system directory  (Ignored)
	 * 4) The windows directory (Ignored)
	 * 5) $PATH
	 *
	 * Just to make things more interesting, tokens can contain
	 * white space if they are surrounded by quotation marks.  I'm
	 * beginning to understand just why windows apps are generally
	 * so crap, with an API like this :-(
	 */
	if (appname != NULL) {
		cmd = mono_unicode_to_external (appname);
		if (cmd == NULL) {
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: unicode conversion returned NULL",
				   __func__);

			mono_w32error_set_last (ERROR_PATH_NOT_FOUND);
			goto free_strings;
		}

		switch_dir_separators(cmd);
	}

	if (cmdline != NULL) {
		args = mono_unicode_to_external (cmdline);
		if (args == NULL) {
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: unicode conversion returned NULL", __func__);

			mono_w32error_set_last (ERROR_PATH_NOT_FOUND);
			goto free_strings;
		}
	}

	if (cwd != NULL) {
		dir = mono_unicode_to_external (cwd);
		if (dir == NULL) {
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: unicode conversion returned NULL", __func__);

			mono_w32error_set_last (ERROR_PATH_NOT_FOUND);
			goto free_strings;
		}

		/* Turn all the slashes round the right way */
		switch_dir_separators(dir);
	}


	/* We can't put off locating the executable any longer :-( */
	if (cmd != NULL) {
		char *unquoted;
		if (g_ascii_isalpha (cmd[0]) && (cmd[1] == ':')) {
			/* Strip off the drive letter.  I can't
			 * believe that CP/M holdover is still
			 * visible...
			 */
			g_memmove (cmd, cmd+2, strlen (cmd)-2);
			cmd[strlen (cmd)-2] = '\0';
		}

		unquoted = g_shell_unquote (cmd, NULL);
		if (unquoted[0] == '/') {
			/* Assume full path given */
			prog = g_strdup (unquoted);

			/* Executable existing ? */
			if (!is_readable_or_executable (prog)) {
				mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: Couldn't find executable %s",
					   __func__, prog);
				g_free (unquoted);
				mono_w32error_set_last (ERROR_FILE_NOT_FOUND);
				goto free_strings;
			}
		} else {
			/* Search for file named by cmd in the current
			 * directory
			 */
			char *curdir = g_get_current_dir ();

			prog = g_strdup_printf ("%s/%s", curdir, unquoted);
			g_free (curdir);

			/* And make sure it's readable */
			if (!is_readable_or_executable (prog)) {
				mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: Couldn't find executable %s",
					   __func__, prog);
				g_free (unquoted);
				mono_w32error_set_last (ERROR_FILE_NOT_FOUND);
				goto free_strings;
			}
		}
		g_free (unquoted);

		args_after_prog = args;
	} else {
		char *token = NULL;
		char quote;

		/* Dig out the first token from args, taking quotation
		 * marks into account
		 */

		/* First, strip off all leading whitespace */
		args = g_strchug (args);

		/* args_after_prog points to the contents of args
		 * after token has been set (otherwise argv[0] is
		 * duplicated)
		 */
		args_after_prog = args;

		/* Assume the opening quote will always be the first
		 * character
		 */
		if (args[0] == '\"' || args [0] == '\'') {
			quote = args [0];
			for (i = 1; args[i] != '\0' && args[i] != quote; i++);
			if (args [i + 1] == '\0' || g_ascii_isspace (args[i+1])) {
				/* We found the first token */
				token = g_strndup (args+1, i-1);
				args_after_prog = g_strchug (args + i + 1);
			} else {
				/* Quotation mark appeared in the
				 * middle of the token.  Just give the
				 * whole first token, quotes and all,
				 * to exec.
				 */
			}
		}

		if (token == NULL) {
			/* No quote mark, or malformed */
			for (i = 0; args[i] != '\0'; i++) {
				if (g_ascii_isspace (args[i])) {
					token = g_strndup (args, i);
					args_after_prog = args + i + 1;
					break;
				}
			}
		}

		if (token == NULL && args[0] != '\0') {
			/* Must be just one token in the string */
			token = g_strdup (args);
			args_after_prog = NULL;
		}

		if (token == NULL) {
			/* Give up */
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: Couldn't find what to exec", __func__);

			mono_w32error_set_last (ERROR_PATH_NOT_FOUND);
			goto free_strings;
		}

		/* Turn all the slashes round the right way. Only for
		 * the prg. name
		 */
		switch_dir_separators(token);

		if (g_ascii_isalpha (token[0]) && (token[1] == ':')) {
			/* Strip off the drive letter.  I can't
			 * believe that CP/M holdover is still
			 * visible...
			 */
			g_memmove (token, token+2, strlen (token)-2);
			token[strlen (token)-2] = '\0';
		}

		if (token[0] == '/') {
			/* Assume full path given */
			prog = g_strdup (token);

			/* Executable existing ? */
			if (!is_readable_or_executable (prog)) {
				mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: Couldn't find executable %s",
					   __func__, token);
				g_free (token);
				mono_w32error_set_last (ERROR_FILE_NOT_FOUND);
				goto free_strings;
			}
		} else {
			char *curdir = g_get_current_dir ();

			/* FIXME: Need to record the directory
			 * containing the current process, and check
			 * that for the new executable as the first
			 * place to look
			 */

			prog = g_strdup_printf ("%s/%s", curdir, token);
			g_free (curdir);

			/* I assume X_OK is the criterion to use,
			 * rather than F_OK
			 *
			 * X_OK is too strict *if* the target is a CLR binary
			 */
			if (!is_readable_or_executable (prog)) {
				g_free (prog);
				prog = g_find_program_in_path (token);
				if (prog == NULL) {
					mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: Couldn't find executable %s", __func__, token);

					g_free (token);
					mono_w32error_set_last (ERROR_FILE_NOT_FOUND);
					goto free_strings;
				}
			}
		}

		g_free (token);
	}

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: Exec prog [%s] args [%s]",
		__func__, prog, args_after_prog);

	/* Check for CLR binaries; if found, we will try to invoke
	 * them using the same mono binary that started us.
	 */
	if (is_managed_binary (prog)) {
		gunichar2 *newapp, *newcmd;
		gsize bytes_ignored;

		newapp = mono_unicode_from_external (cli_launcher ? cli_launcher : "mono", &bytes_ignored);
		if (newapp) {
			if (appname)
				newcmd = utf16_concat (utf16_quote, newapp, utf16_quote, utf16_space, appname, utf16_space, cmdline, (const gunichar2 *)NULL);
			else
				newcmd = utf16_concat (utf16_quote, newapp, utf16_quote, utf16_space, cmdline, (const gunichar2 *)NULL);

			g_free (newapp);

			if (newcmd) {
				ret = process_create (NULL, newcmd, cwd, startup_handles, process_info);

				g_free (newcmd);

				goto free_strings;
			}
		}
	} else {
		if (!is_executable (prog)) {
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: Executable permisson not set on %s", __func__, prog);
			mono_w32error_set_last (ERROR_ACCESS_DENIED);
			goto free_strings;
		}
	}

	if (args_after_prog != NULL && *args_after_prog) {
		char *qprog;

		qprog = g_shell_quote (prog);
		full_prog = g_strconcat (qprog, " ", args_after_prog, NULL);
		g_free (qprog);
	} else {
		full_prog = g_shell_quote (prog);
	}

	ret = g_shell_parse_argv (full_prog, NULL, &argv, &gerr);
	if (ret == FALSE) {
		g_message ("process_create: %s\n", gerr->message);
		g_error_free (gerr);
		gerr = NULL;
		goto free_strings;
	}

	if (startup_handles) {
		in_fd = GPOINTER_TO_UINT (startup_handles->input);
		out_fd = GPOINTER_TO_UINT (startup_handles->output);
		err_fd = GPOINTER_TO_UINT (startup_handles->error);
	} else {
		in_fd = GPOINTER_TO_UINT (mono_w32file_get_console_input ());
		out_fd = GPOINTER_TO_UINT (mono_w32file_get_console_output ());
		err_fd = GPOINTER_TO_UINT (mono_w32file_get_console_error ());
	}

	/*
	 * process->env_variables is a an array of MonoString*
	 *
	 * If new_environ is not NULL it specifies the entire set of
	 * environment variables in the new process.  Otherwise the
	 * new process inherits the same environment.
	 */
	if (process_info->env_variables) {
		MonoArrayHandle array = MONO_HANDLE_NEW (MonoArray, process_info->env_variables);
		MonoStringHandle var = MONO_HANDLE_NEW (MonoString, NULL);
		gsize const array_length = mono_array_handle_length (array);

		/* +2: one for the process handle value, and the last one is NULL */
		// What "process handle value"?
		env_strings = g_new0 (gchar*, array_length + 2);

		/* Copy each environ string into 'strings' turning it into utf8 (or the requested encoding) at the same time */
		for (gsize i = 0; i < array_length; ++i) {
			MONO_HANDLE_ARRAY_GETREF (var, array, i);
			gchandle_t gchandle = 0;
			env_strings [i] = mono_unicode_to_external (mono_string_handle_pin_chars (var, &gchandle));
			mono_gchandle_free_internal (gchandle);
		}
	} else {
		gsize env_count = 0;
		for (i = 0; environ[i] != NULL; i++)
			env_count++;

		/* +2: one for the process handle value, and the last one is NULL */
		// What "process handle value"?
		env_strings = g_new0 (gchar*, env_count + 2);

		/* Copy each environ string into 'strings' turning it into utf8 (or the requested encoding) at the same time */
		for (i = 0; i < env_count; i++)
			env_strings [i] = g_strdup (environ[i]);
	}

	/* Create a pipe to make sure the child doesn't exit before
	 * we can add the process to the linked list of processes */
	if (pipe (startup_pipe) == -1) {
		/* Could not create the pipe to synchroniz process startup. We'll just not synchronize.
		 * This is just for a very hard to hit race condition in the first place */
		startup_pipe [0] = startup_pipe [1] = -1;
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: new process startup not synchronized. We may not notice if the newly created process exits immediately.", __func__);
	}

	switch (pid = fork ()) {
	case -1: /* Error */ {
		mono_w32error_set_last (ERROR_OUTOFMEMORY);
		ret = FALSE;
		break;
	}
	case 0: /* Child */ {
		if (startup_pipe [0] != -1) {
			/* Wait until the parent has updated it's internal data */
			ssize_t _i G_GNUC_UNUSED = read (startup_pipe [0], &dummy, 1);
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: child: parent has completed its setup", __func__);
			close (startup_pipe [0]);
			close (startup_pipe [1]);
		}

		/* should we detach from the process group? */

		/* Connect stdin, stdout and stderr */
		dup2 (in_fd, 0);
		dup2 (out_fd, 1);
		dup2 (err_fd, 2);

		/* Close this child's file handles. */
		close_my_fds ();

#ifdef DEBUG_ENABLED
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: exec()ing [%s] in dir [%s]", __func__, cmd,
			   dir == NULL?".":dir);
		for (i = 0; argv[i] != NULL; i++)
			g_message ("arg %" G_GUINT32_FORMAT ": [%s]", i, argv[i]);

		for (i = 0; env_strings[i] != NULL; i++)
			g_message ("env %" G_GUINT32_FORMAT ": [%s]", i, env_strings[i]);
#endif

		/* set cwd */
		if (dir != NULL && chdir (dir) == -1) {
			/* set error */
			_exit (-1);
		}

		/* exec */
		execve (argv[0], argv, env_strings);

		/* set error */
		_exit (-1);

		break;
	}
	default: /* Parent */ {
		MonoW32Handle *handle_data;
		MonoW32HandleProcess process_handle;

		memset (&process_handle, 0, sizeof (process_handle));
		process_handle.pid = pid;
		process_handle.child = TRUE;
		process_handle.pname = g_strdup (prog);
		process_set_defaults (&process_handle);

		/* Add our process into the linked list of processes */
		process = (Process *) g_malloc0 (sizeof (Process));
		process->pid = pid;
		process->handle_count = 1;
		mono_coop_sem_init (&process->exit_sem, 0);

		process_handle.process = process;

		handle = mono_w32handle_new (MONO_W32TYPE_PROCESS, &process_handle);
		if (handle == INVALID_HANDLE_VALUE) {
			g_warning ("%s: error creating process handle", __func__);

			mono_coop_sem_destroy (&process->exit_sem);
			g_free (process);

			mono_w32error_set_last (ERROR_OUTOFMEMORY);
			ret = FALSE;
			break;
		}

		if (!mono_w32handle_lookup_and_ref (handle, &handle_data))
			g_error ("%s: unknown handle %p", __func__, handle);

		if (handle_data->type != MONO_W32TYPE_PROCESS)
			g_error ("%s: unknown process handle %p", __func__, handle);

		/* Keep the process handle artificially alive until the process
		 * exits so that the information in the handle isn't lost. */
		process->handle = mono_w32handle_duplicate (handle_data);

		mono_coop_mutex_lock (&processes_mutex);
		process->next = processes;
		mono_memory_barrier ();
		processes = process;
		mono_coop_mutex_unlock (&processes_mutex);

		if (process_info != NULL) {
			process_info->process_handle = handle;
			process_info->pid = pid;
		}

		mono_w32handle_unref (handle_data);

		break;
	}
	}

	if (startup_pipe [1] != -1) {
		/* Write 1 byte, doesn't matter what */
		ssize_t _i G_GNUC_UNUSED = write (startup_pipe [1], startup_pipe, 1);
		close (startup_pipe [0]);
		close (startup_pipe [1]);
	}

free_strings:
	g_free (cmd);
	g_free (full_prog);
	g_free (prog);
	g_free (args);
	g_free (dir);
	g_strfreev (env_strings);
	g_strfreev (argv);

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: returning handle %p for pid %d", __func__, handle, pid);

	/* Check if something needs to be cleaned up. */
	processes_cleanup ();

	return ret;
#else
	mono_w32error_set_last (ERROR_NOT_SUPPORTED);
	return FALSE;
#endif // defined (HAVE_FORK) && defined (HAVE_EXECVE)
}

MonoBoolean
ves_icall_System_Diagnostics_Process_ShellExecuteEx_internal (MonoW32ProcessStartInfoHandle proc_start_info, MonoW32ProcessInfo *process_info, MonoError *error)
{
	MonoCreateProcessCoop coop;
	mono_createprocess_coop_init (&coop, proc_start_info, process_info);

	gboolean ret;
	gboolean handler_needswait = FALSE;

	if (!coop.filename) {
		/* w2k returns TRUE for this, for some reason. */
		ret = TRUE;
		goto done;
	}

	const gunichar2 *lpFile;
	lpFile = coop.filename;
	const gunichar2 *lpParameters;
	lpParameters = coop.arguments;
	const gunichar2 *lpDirectory;
	lpDirectory = coop.length.working_directory ? coop.working_directory : NULL;

	/* Put both executable and parameters into the second argument
	 * to process_create (), so it searches $PATH.  The conversion
	 * into and back out of utf8 is because there is no
	 * g_strdup_printf () equivalent for gunichar2 :-(
	 */
	gunichar2 *args;
	args = utf16_concat (utf16_quote, lpFile, utf16_quote, lpParameters ? utf16_space : NULL, lpParameters, (const gunichar2 *)NULL);
	if (args == NULL) {
		mono_w32error_set_last (ERROR_INVALID_DATA);
		ret = FALSE;
		goto done;
	}
	ret = process_create (NULL, args, lpDirectory, NULL, process_info);
	g_free (args);

	if (!ret && mono_w32error_get_last () == ERROR_OUTOFMEMORY)
		goto done;

	if (!ret) {

#if defined(TARGET_IOS) || defined(TARGET_ANDROID)
		// don't try the "open" handlers on iOS/Android, they don't exist there anyway
		goto done;
#endif

		static char *handler;
		static gunichar2 *handler_utf16;

		if (handler_utf16 == (gunichar2 *)-1) {
			ret = FALSE;
			goto done;
		}

#ifdef HOST_DARWIN
		handler = g_strdup ("/usr/bin/open");
		handler_needswait = TRUE;
#else
		/*
		 * On Linux, try: xdg-open, the FreeDesktop standard way of doing it,
		 * if that fails, try to use gnome-open, then kfmclient
		 */
		MONO_ENTER_GC_SAFE;
		handler = g_find_program_in_path ("xdg-open");
		if (handler != NULL)
			handler_needswait = TRUE;
		else {
			handler = g_find_program_in_path ("gnome-open");
			if (handler == NULL){
				handler = g_find_program_in_path ("kfmclient");
				if (handler == NULL){
					handler_utf16 = (gunichar2 *) -1;
					ret = FALSE;
				} else {
					/* kfmclient needs exec argument */
					char *old = handler;
					handler = g_strconcat (old, " exec",
							       NULL);
					g_free (old);
				}
			}
		}
		MONO_EXIT_GC_SAFE;
		if (ret == FALSE){
			goto done;
		}
#endif
		handler_utf16 = g_utf8_to_utf16 (handler, -1, NULL, NULL, NULL);
		g_free (handler);

		/* Put quotes around the filename, in case it's a url
		 * that contains #'s (process_create() calls
		 * g_shell_parse_argv(), which deliberately throws
		 * away anything after an unquoted #).  Fixes bug
		 * 371567.
		 */
		args = utf16_concat (handler_utf16, utf16_space, utf16_quote, lpFile, utf16_quote,
			lpParameters ? utf16_space : NULL, lpParameters, (const gunichar2 *)NULL);
		if (args == NULL) {
			mono_w32error_set_last (ERROR_INVALID_DATA);
			ret = FALSE;
			goto done;
		}
		ret = process_create (NULL, args, lpDirectory, NULL, process_info);
		g_free (args);
		if (!ret) {
			if (mono_w32error_get_last () != ERROR_OUTOFMEMORY)
				mono_w32error_set_last (ERROR_INVALID_DATA);
			ret = FALSE;
			goto done;
		}

		if (handler_needswait) {
			gint32 exitcode;
			MonoW32HandleWaitRet waitret;
			waitret = process_wait ((MonoW32Handle*)process_info->process_handle, MONO_INFINITE_WAIT, NULL);
			mono_get_exit_code_process (process_info->process_handle, &exitcode);
			if (exitcode != 0)
				ret = FALSE;
		}
		/* Shell exec should not return a process handle when it spawned a GUI thing, like a browser. */
		mono_w32handle_close (process_info->process_handle);
		process_info->process_handle = INVALID_HANDLE_VALUE;
	}

done:
	if (ret == FALSE) {
		process_info->pid = -mono_w32error_get_last ();
	} else {
#if !defined(MONO_CROSS_COMPILE)
		process_info->pid = mono_w32process_get_pid (process_info->process_handle);
#else
		process_info->pid = 0;
#endif
	}

	mono_createprocess_coop_cleanup (&coop);

	return ret;
}

/* Only used when UseShellExecute is false */
static gboolean
process_get_complete_path (const gunichar2 *appname, gchar **completed)
{
	char *found = NULL;
	gboolean result = FALSE;

	char *utf8app = g_utf16_to_utf8 (appname, -1, NULL, NULL, NULL);

	if (g_path_is_absolute (utf8app)) {
		*completed = g_shell_quote (utf8app);
		result = TRUE;
		goto exit;
	}

	if (g_file_test (utf8app, G_FILE_TEST_IS_EXECUTABLE) && !g_file_test (utf8app, G_FILE_TEST_IS_DIR)) {
		*completed = g_shell_quote (utf8app);
		result = TRUE;
		goto exit;
	}
	
	found = g_find_program_in_path (utf8app);
	if (found == NULL) {
		*completed = NULL;
		result = FALSE;
		goto exit;
	}

	*completed = g_shell_quote (found);
	result = TRUE;
exit:
	g_free (found);
	g_free (utf8app);
	return result;
}

static gboolean
process_get_shell_arguments (MonoCreateProcessCoop *coop, gunichar2 **shell_path)
{
	gchar *complete_path = NULL;

	*shell_path = NULL;

	if (process_get_complete_path (coop->filename, &complete_path)) {
		*shell_path = g_utf8_to_utf16 (complete_path, -1, NULL, NULL, NULL);
		g_free (complete_path);
	}

	return *shell_path != NULL;
}

MonoBoolean
ves_icall_System_Diagnostics_Process_CreateProcess_internal (MonoW32ProcessStartInfoHandle proc_start_info,
	HANDLE stdin_handle, HANDLE stdout_handle, HANDLE stderr_handle, MonoW32ProcessInfo *process_info, MonoError *error)
{
	MonoCreateProcessCoop coop;
	mono_createprocess_coop_init (&coop, proc_start_info, process_info);

	gboolean ret;
	StartupHandles startup_handles;
	gunichar2 *shell_path = NULL;

	memset (&startup_handles, 0, sizeof (startup_handles));
	startup_handles.input = stdin_handle;
	startup_handles.output = stdout_handle;
	startup_handles.error = stderr_handle;

	if (!process_get_shell_arguments (&coop, &shell_path)) {
		process_info->pid = -ERROR_FILE_NOT_FOUND;
		ret = FALSE;
		goto exit;
	}

	gunichar2 *args;
	args = coop.length.arguments ? coop.arguments : NULL;

	/* The default dir name is "".  Turn that into NULL to mean "current directory" */
	gunichar2 *dir;
	dir = coop.length.working_directory ? coop.working_directory : NULL;

	ret = process_create (shell_path, args, dir, &startup_handles, process_info);

	if (!ret)
		process_info->pid = -mono_w32error_get_last ();

exit:
	g_free (shell_path);
	mono_createprocess_coop_cleanup (&coop);
	return ret;
}

/* Returns an array of pids */
MonoArray *
ves_icall_System_Diagnostics_Process_GetProcesses_internal (void)
{
	ERROR_DECL (error);
	MonoArray *procs;
	gpointer *pidarray;
	int i, count;

	MONO_ENTER_GC_SAFE;
	pidarray = mono_process_list (&count);
	MONO_EXIT_GC_SAFE;
	if (!pidarray) {
		mono_error_set_not_supported (error, "This system does not support EnumProcesses");
		mono_error_set_pending_exception (error);
		return NULL;
	}
	procs = mono_array_new_checked (mono_domain_get (), mono_get_int32_class (), count, error);
	if (mono_error_set_pending_exception (error)) {
		g_free (pidarray);
		return NULL;
	}
	if (sizeof (guint32) == sizeof (gpointer)) {
		memcpy (mono_array_addr_internal (procs, guint32, 0), pidarray, count * sizeof (gint32));
	} else {
		for (i = 0; i < count; ++i)
			*(mono_array_addr_internal (procs, guint32, i)) = GPOINTER_TO_UINT (pidarray [i]);
	}
	g_free (pidarray);

	return procs;
}

void
mono_w32process_set_cli_launcher (gchar *path)
{
	g_free (cli_launcher);
	cli_launcher = g_strdup (path);
}

gpointer
ves_icall_Microsoft_Win32_NativeMethods_GetCurrentProcess (MonoError *error)
{
	return current_process;
}

MonoBoolean
ves_icall_Microsoft_Win32_NativeMethods_GetExitCodeProcess (gpointer handle, gint32 *exitcode, MonoError *error)
{
	return mono_get_exit_code_process (handle, exitcode);
}

static MonoBoolean
mono_get_exit_code_process (gpointer handle, gint32 *exitcode)
{
	MonoW32Handle *handle_data;
	MonoW32HandleProcess *process_handle;

	if (!exitcode)
		return FALSE;

	if (!mono_w32handle_lookup_and_ref (handle, &handle_data)) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: unknown handle %p", __func__, handle);
		mono_w32error_set_last (ERROR_INVALID_HANDLE);
		return FALSE;
	}

	if (handle_data->type != MONO_W32TYPE_PROCESS) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: unknown process handle %p", __func__, handle);
		mono_w32error_set_last (ERROR_INVALID_HANDLE);
		mono_w32handle_unref (handle_data);
		return FALSE;
	}

	process_handle = (MonoW32HandleProcess*) handle_data->specific;

	if (process_handle->pid == current_pid) {
		*exitcode = STILL_ACTIVE;
		mono_w32handle_unref (handle_data);
		return TRUE;
	}

	/* A process handle is only signalled if the process has exited
	 * and has been waited for. Make sure any process exit has been
	 * noticed before checking if the process is signalled.
	 * Fixes bug 325463. */
	mono_w32handle_wait_one (handle, 0, TRUE);

	*exitcode = mono_w32handle_issignalled (handle_data) ? process_handle->exitstatus : STILL_ACTIVE;

	mono_w32handle_unref (handle_data);

	return TRUE;
}

MonoBoolean
ves_icall_Microsoft_Win32_NativeMethods_CloseProcess (gpointer handle, MonoError *error)
{
	return mono_w32handle_close (handle);
}

MonoBoolean
ves_icall_Microsoft_Win32_NativeMethods_TerminateProcess (gpointer handle, gint32 exitcode, MonoError *error)
{
#ifdef HAVE_KILL
	MonoW32Handle *handle_data;
	int ret;
	pid_t pid;

	if (!mono_w32handle_lookup_and_ref (handle, &handle_data)) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: unknown handle %p", __func__, handle);
		mono_w32error_set_last (ERROR_INVALID_HANDLE);
		return FALSE;
	}

	if (handle_data->type != MONO_W32TYPE_PROCESS) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: unknown process handle %p", __func__, handle);
		mono_w32error_set_last (ERROR_INVALID_HANDLE);
		mono_w32handle_unref (handle_data);
		return FALSE;
	}

	pid = ((MonoW32HandleProcess*) handle_data->specific)->pid;

	ret = kill (pid, exitcode == -1 ? SIGKILL : SIGTERM);
	if (ret == 0) {
		mono_w32handle_unref (handle_data);
		return TRUE;
	}

	switch (errno) {
	case EINVAL: mono_w32error_set_last (ERROR_INVALID_PARAMETER); break;
	case EPERM:  mono_w32error_set_last (ERROR_ACCESS_DENIED);     break;
	case ESRCH:  mono_w32error_set_last (ERROR_PROC_NOT_FOUND);    break;
	default:     mono_w32error_set_last (ERROR_GEN_FAILURE);       break;
	}

	mono_w32handle_unref (handle_data);
	return FALSE;
#else
	g_error ("kill() is not supported by this platform");
#endif
}

MonoBoolean
ves_icall_Microsoft_Win32_NativeMethods_GetProcessWorkingSetSize (gpointer handle, gsize *min, gsize *max, MonoError *error)
{
	MonoW32Handle *handle_data;
	MonoW32HandleProcess *process_handle;

	if (!min || !max)
		return FALSE;

	if (!mono_w32handle_lookup_and_ref (handle, &handle_data)) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: unknown handle %p", __func__, handle);
		mono_w32error_set_last (ERROR_INVALID_HANDLE);
		return FALSE;
	}

	if (handle_data->type != MONO_W32TYPE_PROCESS) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: unknown process handle %p", __func__, handle);
		mono_w32error_set_last (ERROR_INVALID_HANDLE);
		mono_w32handle_unref (handle_data);
		return FALSE;
	}

	process_handle = (MonoW32HandleProcess*) handle_data->specific;

	if (!process_handle->child) {
		mono_w32handle_unref (handle_data);
		return FALSE;
	}

	*min = process_handle->min_working_set;
	*max = process_handle->max_working_set;

	mono_w32handle_unref (handle_data);
	return TRUE;
}

MonoBoolean
ves_icall_Microsoft_Win32_NativeMethods_SetProcessWorkingSetSize (gpointer handle, gsize min, gsize max, MonoError *error)
{
	MonoW32Handle *handle_data;
	MonoW32HandleProcess *process_handle;

	if (!mono_w32handle_lookup_and_ref (handle, &handle_data)) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: unknown handle %p", __func__, handle);
		mono_w32error_set_last (ERROR_INVALID_HANDLE);
		return FALSE;
	}

	if (handle_data->type != MONO_W32TYPE_PROCESS) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: unknown process handle %p", __func__, handle);
		mono_w32error_set_last (ERROR_INVALID_HANDLE);
		mono_w32handle_unref (handle_data);
		return FALSE;
	}

	process_handle = (MonoW32HandleProcess*) handle_data->specific;

	if (!process_handle->child) {
		mono_w32handle_unref (handle_data);
		return FALSE;
	}

	process_handle->min_working_set = min;
	process_handle->max_working_set = max;

	mono_w32handle_unref (handle_data);
	return TRUE;
}

gint32
ves_icall_Microsoft_Win32_NativeMethods_GetPriorityClass (gpointer handle, MonoError *error)
{
#ifdef HAVE_GETPRIORITY
	MonoW32Handle *handle_data;
	gint res;
	gint32 ret;
	pid_t pid;

	if (!mono_w32handle_lookup_and_ref (handle, &handle_data)) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: unknown handle %p", __func__, handle);
		mono_w32error_set_last (ERROR_INVALID_HANDLE);
		return 0;
	}

	if (handle_data->type != MONO_W32TYPE_PROCESS) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: unknown process handle %p", __func__, handle);
		mono_w32error_set_last (ERROR_INVALID_HANDLE);
		mono_w32handle_unref (handle_data);
		return 0;
	}

	pid = ((MonoW32HandleProcess*) handle_data->specific)->pid;

	mono_set_errno (0);
	res = getpriority (PRIO_PROCESS, pid);
	if (res == -1 && errno != 0) {
		switch (errno) {
		case EPERM:
		case EACCES:
			mono_w32error_set_last (ERROR_ACCESS_DENIED);
			break;
		case ESRCH:
			mono_w32error_set_last (ERROR_PROC_NOT_FOUND);
			break;
		default:
			mono_w32error_set_last (ERROR_GEN_FAILURE);
		}

		mono_w32handle_unref (handle_data);
		return 0;
	}

	if (res == 0)
		ret = MONO_W32PROCESS_PRIORITY_CLASS_NORMAL;
	else if (res < -15)
		ret = MONO_W32PROCESS_PRIORITY_CLASS_REALTIME;
	else if (res < -10)
		ret = MONO_W32PROCESS_PRIORITY_CLASS_HIGH;
	else if (res < 0)
		ret = MONO_W32PROCESS_PRIORITY_CLASS_ABOVE_NORMAL;
	else if (res > 10)
		ret = MONO_W32PROCESS_PRIORITY_CLASS_IDLE;
	else if (res > 0)
		ret = MONO_W32PROCESS_PRIORITY_CLASS_BELOW_NORMAL;
	else
		ret = MONO_W32PROCESS_PRIORITY_CLASS_NORMAL;

	mono_w32handle_unref (handle_data);
	return ret;
#else
	mono_w32error_set_last (ERROR_NOT_SUPPORTED);
	return 0;
#endif
}

MonoBoolean
ves_icall_Microsoft_Win32_NativeMethods_SetPriorityClass (gpointer handle, gint32 priorityClass, MonoError *error)
{
#ifdef HAVE_SETPRIORITY
	MonoW32Handle *handle_data;
	int ret;
	int prio;
	pid_t pid;

	if (!mono_w32handle_lookup_and_ref (handle, &handle_data)) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: unknown handle %p", __func__, handle);
		mono_w32error_set_last (ERROR_INVALID_HANDLE);
		return FALSE;
	}

	if (handle_data->type != MONO_W32TYPE_PROCESS) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: unknown process handle %p", __func__, handle);
		mono_w32error_set_last (ERROR_INVALID_HANDLE);
		mono_w32handle_unref (handle_data);
		return FALSE;
	}

	pid = ((MonoW32HandleProcess*) handle_data->specific)->pid;

	switch (priorityClass) {
	case MONO_W32PROCESS_PRIORITY_CLASS_IDLE:
		prio = 19;
		break;
	case MONO_W32PROCESS_PRIORITY_CLASS_BELOW_NORMAL:
		prio = 10;
		break;
	case MONO_W32PROCESS_PRIORITY_CLASS_NORMAL:
		prio = 0;
		break;
	case MONO_W32PROCESS_PRIORITY_CLASS_ABOVE_NORMAL:
		prio = -5;
		break;
	case MONO_W32PROCESS_PRIORITY_CLASS_HIGH:
		prio = -11;
		break;
	case MONO_W32PROCESS_PRIORITY_CLASS_REALTIME:
		prio = -20;
		break;
	default:
		mono_w32error_set_last (ERROR_INVALID_PARAMETER);
		mono_w32handle_unref (handle_data);
		return FALSE;
	}

	ret = setpriority (PRIO_PROCESS, pid, prio);
	if (ret == -1) {
		switch (errno) {
		case EPERM:
		case EACCES:
			mono_w32error_set_last (ERROR_ACCESS_DENIED);
			break;
		case ESRCH:
			mono_w32error_set_last (ERROR_PROC_NOT_FOUND);
			break;
		default:
			mono_w32error_set_last (ERROR_GEN_FAILURE);
		}
	}

	mono_w32handle_unref (handle_data);
	return ret == 0;
#else
	mono_w32error_set_last (ERROR_NOT_SUPPORTED);
	return FALSE;
#endif
}

static void
ticks_to_processtime (guint64 ticks, ProcessTime *processtime)
{
	processtime->lowDateTime = ticks & 0xFFFFFFFF;
	processtime->highDateTime = ticks >> 32;
}

MonoBoolean
ves_icall_Microsoft_Win32_NativeMethods_GetProcessTimes (gpointer handle, gint64 *creation_time, gint64 *exit_time, gint64 *kernel_time, gint64 *user_time, MonoError *error)
{
	MonoW32Handle *handle_data;
	MonoW32HandleProcess *process_handle;
	ProcessTime *creation_processtime, *exit_processtime, *kernel_processtime, *user_processtime;

	if (!creation_time || !exit_time || !kernel_time || !user_time) {
		/* Not sure if w32 allows NULLs here or not */
		return FALSE;
	}

	creation_processtime = (ProcessTime*) creation_time;
	exit_processtime = (ProcessTime*) exit_time;
	kernel_processtime = (ProcessTime*) kernel_time;
	user_processtime = (ProcessTime*) user_time;

	memset (creation_processtime, 0, sizeof (ProcessTime));
	memset (exit_processtime, 0, sizeof (ProcessTime));
	memset (kernel_processtime, 0, sizeof (ProcessTime));
	memset (user_processtime, 0, sizeof (ProcessTime));

	if (!mono_w32handle_lookup_and_ref (handle, &handle_data)) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: unknown handle %p", __func__, handle);
		mono_w32error_set_last (ERROR_INVALID_HANDLE);
		return FALSE;
	}

	if (handle_data->type != MONO_W32TYPE_PROCESS) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: unknown process handle %p", __func__, handle);
		mono_w32error_set_last (ERROR_INVALID_HANDLE);
		mono_w32handle_unref (handle_data);
		return FALSE;
	}

	process_handle = (MonoW32HandleProcess*) handle_data->specific;

	if (!process_handle->child) {
		gint64 start_ticks, user_ticks, kernel_ticks;

		mono_process_get_times (GINT_TO_POINTER (process_handle->pid),
			&start_ticks, &user_ticks, &kernel_ticks);

		ticks_to_processtime (start_ticks, creation_processtime);
		ticks_to_processtime (kernel_ticks, kernel_processtime);
		ticks_to_processtime (user_ticks, user_processtime);

		mono_w32handle_unref (handle_data);
		return TRUE;
	}

	ticks_to_processtime (process_handle->create_time, creation_processtime);

	/* A process handle is only signalled if the process has
	 * exited, otherwise exit_processtime isn't set */
	if (mono_w32handle_issignalled (handle_data))
		ticks_to_processtime (process_handle->exit_time, exit_processtime);

#ifdef HAVE_GETRUSAGE
	if (process_handle->pid == getpid ()) {
		struct rusage time_data;
		if (getrusage (RUSAGE_SELF, &time_data) == 0) {
			ticks_to_processtime ((guint64)time_data.ru_utime.tv_sec * 10000000 + (guint64)time_data.ru_utime.tv_usec * 10, user_processtime);
			ticks_to_processtime ((guint64)time_data.ru_stime.tv_sec * 10000000 + (guint64)time_data.ru_stime.tv_usec * 10, kernel_processtime);
		}
	}
#endif

	mono_w32handle_unref (handle_data);
	return TRUE;
}

static IMAGE_SECTION_HEADER *
get_enclosing_section_header (guint32 rva, IMAGE_NT_HEADERS32 *nt_headers)
{
	IMAGE_SECTION_HEADER *section = IMAGE_FIRST_SECTION32 (nt_headers);
	guint32 i;

	for (i = 0; i < GUINT16_FROM_LE (nt_headers->FileHeader.NumberOfSections); i++, section++) {
		guint32 size = GUINT32_FROM_LE (section->Misc.VirtualSize);
		if (size == 0) {
			size = GUINT32_FROM_LE (section->SizeOfRawData);
		}

		if ((rva >= GUINT32_FROM_LE (section->VirtualAddress)) &&
		    (rva < (GUINT32_FROM_LE (section->VirtualAddress) + size))) {
			return(section);
		}
	}

	return(NULL);
}

/* This works for both 32bit and 64bit files, as the differences are
 * all after the section header block
 */
static gpointer
get_ptr_from_rva (guint32 rva, IMAGE_NT_HEADERS32 *ntheaders, gpointer file_map)
{
	IMAGE_SECTION_HEADER *section_header;
	guint32 delta;

	section_header = get_enclosing_section_header (rva, ntheaders);
	if (section_header == NULL) {
		return(NULL);
	}

	delta = (guint32)(GUINT32_FROM_LE (section_header->VirtualAddress) -
			  GUINT32_FROM_LE (section_header->PointerToRawData));

	return((guint8 *)file_map + rva - delta);
}

static gpointer
scan_resource_dir (IMAGE_RESOURCE_DIRECTORY *root, IMAGE_NT_HEADERS32 *nt_headers, gpointer file_map,
	IMAGE_RESOURCE_DIRECTORY_ENTRY *entry, int level, guint32 res_id, guint32 lang_id, gsize *size)
{
	IMAGE_RESOURCE_DIRECTORY_ENTRY swapped_entry;
	gboolean is_string, is_dir;
	guint32 name_offset, dir_offset, data_offset;

	swapped_entry.Name = GUINT32_FROM_LE (entry->Name);
	swapped_entry.OffsetToData = GUINT32_FROM_LE (entry->OffsetToData);

	is_string = swapped_entry.NameIsString;
	is_dir = swapped_entry.DataIsDirectory;
	name_offset = swapped_entry.NameOffset;
	dir_offset = swapped_entry.OffsetToDirectory;
	data_offset = swapped_entry.OffsetToData;

	if (level == 0) {
		/* Normally holds a directory entry for each type of
		 * resource
		 */
		if ((is_string == FALSE &&
		     name_offset != res_id) ||
		    (is_string == TRUE)) {
			return(NULL);
		}
	} else if (level == 1) {
		/* Normally holds a directory entry for each resource
		 * item
		 */
	} else if (level == 2) {
		/* Normally holds a directory entry for each language
		 */
		if ((is_string == FALSE &&
		     name_offset != lang_id &&
		     lang_id != 0) ||
		    (is_string == TRUE)) {
			return(NULL);
		}
	} else {
		g_assert_not_reached ();
	}

	if (is_dir == TRUE) {
		IMAGE_RESOURCE_DIRECTORY *res_dir = (IMAGE_RESOURCE_DIRECTORY *)((guint8 *)root + dir_offset);
		IMAGE_RESOURCE_DIRECTORY_ENTRY *sub_entries = (IMAGE_RESOURCE_DIRECTORY_ENTRY *)(res_dir + 1);
		guint32 entries, i;

		entries = GUINT16_FROM_LE (res_dir->NumberOfNamedEntries) + GUINT16_FROM_LE (res_dir->NumberOfIdEntries);

		for (i = 0; i < entries; i++) {
			IMAGE_RESOURCE_DIRECTORY_ENTRY *sub_entry = &sub_entries[i];
			gpointer ret;

			ret = scan_resource_dir (root, nt_headers, file_map,
						 sub_entry, level + 1, res_id,
						 lang_id, size);
			if (ret != NULL) {
				return(ret);
			}
		}

		return(NULL);
	} else {
		IMAGE_RESOURCE_DATA_ENTRY *data_entry = (IMAGE_RESOURCE_DATA_ENTRY *)((guint8 *)root + data_offset);
		*size = GUINT32_FROM_LE (data_entry->Size);

		return(get_ptr_from_rva (GUINT32_FROM_LE (data_entry->OffsetToData), nt_headers, file_map));
	}
}

static gpointer
find_pe_file_resources32 (gpointer file_map, guint32 map_size, guint32 res_id, guint32 lang_id, gsize *size)
{
	IMAGE_DOS_HEADER *dos_header;
	IMAGE_NT_HEADERS32 *nt_headers;
	IMAGE_RESOURCE_DIRECTORY *resource_dir;
	IMAGE_RESOURCE_DIRECTORY_ENTRY *resource_dir_entry;
	guint32 resource_rva, entries, i;
	gpointer ret = NULL;

	dos_header = (IMAGE_DOS_HEADER *)file_map;
	if (dos_header->e_magic != IMAGE_DOS_SIGNATURE) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: Bad dos signature 0x%x", __func__, dos_header->e_magic);

		mono_w32error_set_last (ERROR_INVALID_DATA);
		return(NULL);
	}

	if (map_size < sizeof(IMAGE_NT_HEADERS32) + GUINT32_FROM_LE (dos_header->e_lfanew)) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: File is too small: %" G_GUINT32_FORMAT, __func__, map_size);

		mono_w32error_set_last (ERROR_BAD_LENGTH);
		return(NULL);
	}

	nt_headers = (IMAGE_NT_HEADERS32 *)((guint8 *)file_map + GUINT32_FROM_LE (dos_header->e_lfanew));
	if (nt_headers->Signature != IMAGE_NT_SIGNATURE) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: Bad NT signature 0x%x", __func__, nt_headers->Signature);

		mono_w32error_set_last (ERROR_INVALID_DATA);
		return(NULL);
	}

	if (nt_headers->OptionalHeader.Magic == IMAGE_NT_OPTIONAL_HDR64_MAGIC) {
		/* Do 64-bit stuff */
		resource_rva = GUINT32_FROM_LE (((IMAGE_NT_HEADERS64 *)nt_headers)->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_RESOURCE].VirtualAddress);
	} else {
		resource_rva = GUINT32_FROM_LE (nt_headers->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_RESOURCE].VirtualAddress);
	}

	if (resource_rva == 0) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: No resources in file!", __func__);

		mono_w32error_set_last (ERROR_INVALID_DATA);
		return(NULL);
	}

	resource_dir = (IMAGE_RESOURCE_DIRECTORY *)get_ptr_from_rva (resource_rva, (IMAGE_NT_HEADERS32 *)nt_headers, file_map);
	if (resource_dir == NULL) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: Can't find resource directory", __func__);

		mono_w32error_set_last (ERROR_INVALID_DATA);
		return(NULL);
	}

	entries = GUINT16_FROM_LE (resource_dir->NumberOfNamedEntries) + GUINT16_FROM_LE (resource_dir->NumberOfIdEntries);
	resource_dir_entry = (IMAGE_RESOURCE_DIRECTORY_ENTRY *)(resource_dir + 1);

	for (i = 0; i < entries; i++) {
		IMAGE_RESOURCE_DIRECTORY_ENTRY *direntry = &resource_dir_entry[i];
		ret = scan_resource_dir (resource_dir,
					 (IMAGE_NT_HEADERS32 *)nt_headers,
					 file_map, direntry, 0, res_id,
					 lang_id, size);
		if (ret != NULL) {
			return(ret);
		}
	}

	return(NULL);
}

static gpointer
find_pe_file_resources64 (gpointer file_map, guint32 map_size, guint32 res_id, guint32 lang_id, gsize *size)
{
	IMAGE_DOS_HEADER *dos_header;
	IMAGE_NT_HEADERS64 *nt_headers;
	IMAGE_RESOURCE_DIRECTORY *resource_dir;
	IMAGE_RESOURCE_DIRECTORY_ENTRY *resource_dir_entry;
	guint32 resource_rva, entries, i;
	gpointer ret = NULL;

	dos_header = (IMAGE_DOS_HEADER *)file_map;
	if (dos_header->e_magic != IMAGE_DOS_SIGNATURE) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: Bad dos signature 0x%x", __func__, dos_header->e_magic);

		mono_w32error_set_last (ERROR_INVALID_DATA);
		return(NULL);
	}

	if (map_size < sizeof(IMAGE_NT_HEADERS64) + GUINT32_FROM_LE (dos_header->e_lfanew)) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: File is too small: %" G_GUINT32_FORMAT, __func__, map_size);

		mono_w32error_set_last (ERROR_BAD_LENGTH);
		return(NULL);
	}

	nt_headers = (IMAGE_NT_HEADERS64 *)((guint8 *)file_map + GUINT32_FROM_LE (dos_header->e_lfanew));
	if (nt_headers->Signature != IMAGE_NT_SIGNATURE) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: Bad NT signature 0x%x", __func__,
			   nt_headers->Signature);

		mono_w32error_set_last (ERROR_INVALID_DATA);
		return(NULL);
	}

	if (nt_headers->OptionalHeader.Magic == IMAGE_NT_OPTIONAL_HDR64_MAGIC) {
		/* Do 64-bit stuff */
		resource_rva = GUINT32_FROM_LE (((IMAGE_NT_HEADERS64 *)nt_headers)->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_RESOURCE].VirtualAddress);
	} else {
		resource_rva = GUINT32_FROM_LE (nt_headers->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_RESOURCE].VirtualAddress);
	}

	if (resource_rva == 0) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: No resources in file!", __func__);

		mono_w32error_set_last (ERROR_INVALID_DATA);
		return(NULL);
	}

	resource_dir = (IMAGE_RESOURCE_DIRECTORY *)get_ptr_from_rva (resource_rva, (IMAGE_NT_HEADERS32 *)nt_headers, file_map);
	if (resource_dir == NULL) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: Can't find resource directory", __func__);

		mono_w32error_set_last (ERROR_INVALID_DATA);
		return(NULL);
	}

	entries = GUINT16_FROM_LE (resource_dir->NumberOfNamedEntries) + GUINT16_FROM_LE (resource_dir->NumberOfIdEntries);
	resource_dir_entry = (IMAGE_RESOURCE_DIRECTORY_ENTRY *)(resource_dir + 1);

	for (i = 0; i < entries; i++) {
		IMAGE_RESOURCE_DIRECTORY_ENTRY *direntry = &resource_dir_entry[i];
		ret = scan_resource_dir (resource_dir,
					 (IMAGE_NT_HEADERS32 *)nt_headers,
					 file_map, direntry, 0, res_id,
					 lang_id, size);
		if (ret != NULL) {
			return(ret);
		}
	}

	return(NULL);
}

static gpointer
find_pe_file_resources (gpointer file_map, guint32 map_size, guint32 res_id, guint32 lang_id, gsize *size)
{
	/* Figure this out when we support 64bit PE files */
	if (1) {
		return find_pe_file_resources32 (file_map, map_size, res_id,
						 lang_id, size);
	} else {
		return find_pe_file_resources64 (file_map, map_size, res_id,
						 lang_id, size);
	}
}

static guint32
unicode_chars (const gunichar2 *str)
{
	guint32 len = 0;

	do {
		if (str[len] == '\0') {
			return(len);
		}
		len++;
	} while(1);
}

static gboolean
unicode_compare (const gunichar2 *str1, const gunichar2 *str2)
{
	while (*str1 && *str2) {
		if (*str1 != *str2) {
			return(FALSE);
		}
		++str1;
		++str2;
	}

	return(*str1 == *str2);
}

/* compare a little-endian null-terminated utf16 string and a normal string.
 * Can be used only for ascii or latin1 chars.
 */
static gboolean
unicode_string_equals (const gunichar2 *str1, const gchar *str2)
{
	while (*str1 && *str2) {
		if (GUINT16_TO_LE (*str1) != *str2) {
			return(FALSE);
		}
		++str1;
		++str2;
	}

	return(*str1 == *str2);
}

typedef struct {
	guint16 data_len;
	guint16 value_len;
	guint16 type;
	gunichar2 *key;
} version_data;

/* Returns a pointer to the value data, because there's no way to know
 * how big that data is (value_len is set to zero for most blocks :-( )
 */
static gconstpointer
get_versioninfo_block (gconstpointer data, version_data *block)
{
	block->data_len = GUINT16_FROM_LE (*((guint16 *)data));
	data = (char *)data + sizeof(guint16);
	block->value_len = GUINT16_FROM_LE (*((guint16 *)data));
	data = (char *)data + sizeof(guint16);

	/* No idea what the type is supposed to indicate */
	block->type = GUINT16_FROM_LE (*((guint16 *)data));
	data = (char *)data + sizeof(guint16);
	block->key = ((gunichar2 *)data);

	/* Skip over the key (including the terminator) */
	data = ((gunichar2 *)data) + (unicode_chars (block->key) + 1);

	/* align on a 32-bit boundary */
	ALIGN32 (data);

	return(data);
}

static gconstpointer
get_fixedfileinfo_block (gconstpointer data, version_data *block)
{
	gconstpointer data_ptr;
	VS_FIXEDFILEINFO *ffi;

	data_ptr = get_versioninfo_block (data, block);

	if (block->value_len != sizeof(VS_FIXEDFILEINFO)) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: FIXEDFILEINFO size mismatch", __func__);
		return(NULL);
	}

	if (!unicode_string_equals (block->key, "VS_VERSION_INFO")) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: VS_VERSION_INFO mismatch", __func__);

		return(NULL);
	}

	ffi = ((VS_FIXEDFILEINFO *)data_ptr);
	if ((ffi->dwSignature != VS_FFI_SIGNATURE) ||
	    (ffi->dwStrucVersion != VS_FFI_STRUCVERSION)) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: FIXEDFILEINFO bad signature", __func__);

		return(NULL);
	}

	return(data_ptr);
}

static gconstpointer
get_varfileinfo_block (gconstpointer data_ptr, version_data *block)
{
	/* data is pointing at a Var block
	 */
	data_ptr = get_versioninfo_block (data_ptr, block);

	return(data_ptr);
}

static gconstpointer
get_string_block (gconstpointer data_ptr, const gunichar2 *string_key, gpointer *string_value,
	guint32 *string_value_len, version_data *block)
{
	guint16 data_len = block->data_len;
	guint16 string_len = 28; /* Length of the StringTable block */
	char *orig_data_ptr = (char *)data_ptr - 28;

	/* data_ptr is pointing at an array of one or more String blocks
	 * with total length (not including alignment padding) of
	 * data_len
	 */
	while (((char *)data_ptr - (char *)orig_data_ptr) < data_len) {
		/* align on a 32-bit boundary */
		ALIGN32 (data_ptr);

		data_ptr = get_versioninfo_block (data_ptr, block);
		if (block->data_len == 0) {
			/* We must have hit padding, so give up
			 * processing now
			 */
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: Hit 0-length block, giving up", __func__);

			return(NULL);
		}

		string_len = string_len + block->data_len;

		if (string_key != NULL &&
		    string_value != NULL &&
		    string_value_len != NULL &&
		    unicode_compare (string_key, block->key) == TRUE) {
			*string_value = (gpointer)data_ptr;
			*string_value_len = block->value_len;
		}

		/* Skip over the value */
		data_ptr = ((gunichar2 *)data_ptr) + block->value_len;
	}

	return(data_ptr);
}

/* Returns a pointer to the byte following the Stringtable block, or
 * NULL if the data read hits padding.  We can't recover from this
 * because the data length does not include padding bytes, so it's not
 * possible to just return the start position + length
 *
 * If lang == NULL it means we're just stepping through this block
 */
static gconstpointer
get_stringtable_block (gconstpointer data_ptr, gchar *lang, const gunichar2 *string_key, gpointer *string_value,
	guint32 *string_value_len, version_data *block)
{
	guint16 data_len = block->data_len;
	guint16 string_len = 36; /* length of the StringFileInfo block */
	gchar *found_lang;
	gchar *lowercase_lang;

	/* data_ptr is pointing at an array of StringTable blocks,
	 * with total length (not including alignment padding) of
	 * data_len
	 */

	while(string_len < data_len) {
		/* align on a 32-bit boundary */
		ALIGN32 (data_ptr);

		data_ptr = get_versioninfo_block (data_ptr, block);
		if (block->data_len == 0) {
			/* We must have hit padding, so give up
			 * processing now
			 */
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: Hit 0-length block, giving up", __func__);
			return(NULL);
		}

		string_len = string_len + block->data_len;

		found_lang = g_utf16_to_utf8 (block->key, 8, NULL, NULL, NULL);
		if (found_lang == NULL) {
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: Didn't find a valid language key, giving up", __func__);
			return(NULL);
		}

		lowercase_lang = g_utf8_strdown (found_lang, -1);
		g_free (found_lang);
		found_lang = lowercase_lang;
		lowercase_lang = NULL;

		if (lang != NULL && !strcmp (found_lang, lang)) {
			/* Got the one we're interested in */
			data_ptr = get_string_block (data_ptr, string_key,
						     string_value,
						     string_value_len, block);
		} else {
			data_ptr = get_string_block (data_ptr, NULL, NULL,
						     NULL, block);
		}

		g_free (found_lang);

		if (data_ptr == NULL) {
			/* Child block hit padding */
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: Child block hit 0-length block, giving up", __func__);
			return(NULL);
		}
	}

	return(data_ptr);
}

#if G_BYTE_ORDER == G_BIG_ENDIAN
static gconstpointer
big_up_string_block (gconstpointer data_ptr, version_data *block)
{
	guint16 data_len = block->data_len;
	guint16 string_len = 28; /* Length of the StringTable block */
	gchar *big_value;
	char *orig_data_ptr = (char *)data_ptr - 28;

	/* data_ptr is pointing at an array of one or more String
	 * blocks with total length (not including alignment padding)
	 * of data_len
	 */
	while (((char *)data_ptr - (char *)orig_data_ptr) < data_len) {
		/* align on a 32-bit boundary */
		ALIGN32 (data_ptr);

		data_ptr = get_versioninfo_block (data_ptr, block);
		if (block->data_len == 0) {
			/* We must have hit padding, so give up
			 * processing now
			 */
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: Hit 0-length block, giving up", __func__);
			return(NULL);
		}

		string_len = string_len + block->data_len;

		big_value = g_convert ((gchar *)block->key,
				       unicode_chars (block->key) * 2,
				       "UTF-16BE", "UTF-16LE", NULL, NULL,
				       NULL);
		if (big_value == NULL) {
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: Didn't find a valid string, giving up", __func__);
			return(NULL);
		}

		/* The swapped string should be exactly the same
		 * length as the original little-endian one, but only
		 * copy the number of original chars just to be on the
		 * safe side
		 */
		memcpy (block->key, big_value, unicode_chars (block->key) * 2);
		g_free (big_value);

		big_value = g_convert ((gchar *)data_ptr,
				       unicode_chars (data_ptr) * 2,
				       "UTF-16BE", "UTF-16LE", NULL, NULL,
				       NULL);
		if (big_value == NULL) {
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: Didn't find a valid data string, giving up", __func__);
			return(NULL);
		}
		memcpy ((gpointer)data_ptr, big_value,
			unicode_chars (data_ptr) * 2);
		g_free (big_value);

		data_ptr = ((gunichar2 *)data_ptr) + block->value_len;
	}

	return(data_ptr);
}

/* Returns a pointer to the byte following the Stringtable block, or
 * NULL if the data read hits padding.  We can't recover from this
 * because the data length does not include padding bytes, so it's not
 * possible to just return the start position + length
 */
static gconstpointer
big_up_stringtable_block (gconstpointer data_ptr, version_data *block)
{
	guint16 data_len = block->data_len;
	guint16 string_len = 36; /* length of the StringFileInfo block */
	gchar *big_value;

	/* data_ptr is pointing at an array of StringTable blocks,
	 * with total length (not including alignment padding) of
	 * data_len
	 */

	while(string_len < data_len) {
		/* align on a 32-bit boundary */
		ALIGN32 (data_ptr);

		data_ptr = get_versioninfo_block (data_ptr, block);
		if (block->data_len == 0) {
			/* We must have hit padding, so give up
			 * processing now
			 */
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: Hit 0-length block, giving up", __func__);
			return(NULL);
		}

		string_len = string_len + block->data_len;

		big_value = g_convert ((gchar *)block->key, 16, "UTF-16BE",
				       "UTF-16LE", NULL, NULL, NULL);
		if (big_value == NULL) {
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: Didn't find a valid string, giving up", __func__);
			return(NULL);
		}

		memcpy (block->key, big_value, 16);
		g_free (big_value);

		data_ptr = big_up_string_block (data_ptr, block);

		if (data_ptr == NULL) {
			/* Child block hit padding */
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: Child block hit 0-length block, giving up", __func__);
			return(NULL);
		}
	}

	return(data_ptr);
}

/* Follows the data structures and turns all UTF-16 strings from the
 * LE found in the resource section into UTF-16BE
 */
static void
big_up (gconstpointer datablock, guint32 size)
{
	gconstpointer data_ptr;
	gint32 data_len; /* signed to guard against underflow */
	version_data block;

	data_ptr = get_fixedfileinfo_block (datablock, &block);
	if (data_ptr != NULL) {
		VS_FIXEDFILEINFO *ffi = (VS_FIXEDFILEINFO *)data_ptr;

		/* Byteswap all the fields */
		ffi->dwFileVersionMS = GUINT32_SWAP_LE_BE (ffi->dwFileVersionMS);
		ffi->dwFileVersionLS = GUINT32_SWAP_LE_BE (ffi->dwFileVersionLS);
		ffi->dwProductVersionMS = GUINT32_SWAP_LE_BE (ffi->dwProductVersionMS);
		ffi->dwProductVersionLS = GUINT32_SWAP_LE_BE (ffi->dwProductVersionLS);
		ffi->dwFileFlagsMask = GUINT32_SWAP_LE_BE (ffi->dwFileFlagsMask);
		ffi->dwFileFlags = GUINT32_SWAP_LE_BE (ffi->dwFileFlags);
		ffi->dwFileOS = GUINT32_SWAP_LE_BE (ffi->dwFileOS);
		ffi->dwFileType = GUINT32_SWAP_LE_BE (ffi->dwFileType);
		ffi->dwFileSubtype = GUINT32_SWAP_LE_BE (ffi->dwFileSubtype);
		ffi->dwFileDateMS = GUINT32_SWAP_LE_BE (ffi->dwFileDateMS);
		ffi->dwFileDateLS = GUINT32_SWAP_LE_BE (ffi->dwFileDateLS);

		/* The FFI and header occupies the first 92 bytes
		 */
		data_ptr = (char *)data_ptr + sizeof(VS_FIXEDFILEINFO);
		data_len = block.data_len - 92;

		/* There now follow zero or one StringFileInfo blocks
		 * and zero or one VarFileInfo blocks
		 */
		while (data_len > 0) {
			/* align on a 32-bit boundary */
			ALIGN32 (data_ptr);

			data_ptr = get_versioninfo_block (data_ptr, &block);
			if (block.data_len == 0) {
				/* We must have hit padding, so give
				 * up processing now
				 */
				mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: Hit 0-length block, giving up", __func__);
				return;
			}

			data_len = data_len - block.data_len;

			if (unicode_string_equals (block.key, "VarFileInfo")) {
				data_ptr = get_varfileinfo_block (data_ptr,
								  &block);
				data_ptr = ((guchar *)data_ptr) + block.value_len;
			} else if (unicode_string_equals (block.key,
							  "StringFileInfo")) {
				data_ptr = big_up_stringtable_block (data_ptr,
								     &block);
			} else {
				/* Bogus data */
				mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: Not a valid VERSIONINFO child block", __func__);
				return;
			}

			if (data_ptr == NULL) {
				/* Child block hit padding */
				mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: Child block hit 0-length block, giving up", __func__);
				return;
			}
		}
	}
}
#endif

gboolean
mono_w32process_get_fileversion_info (gunichar2 *filename, gpointer *data)
{
	gpointer file_map;
	gpointer versioninfo;
	void *map_handle;
	gint32 map_size;
	gsize datasize;

	g_assert (data);
	*data = NULL;

	file_map = mono_pe_file_map (filename, &map_size, &map_handle);
	if (!file_map)
		return FALSE;

	versioninfo = find_pe_file_resources (file_map, map_size, RT_VERSION, 0, &datasize);
	if (!versioninfo) {
		mono_pe_file_unmap (file_map, map_handle);
		return FALSE;
	}

	*data = g_malloc0 (datasize);

	/* This could probably process the data so that mono_w32process_ver_query_value() doesn't have to follow the
	 * data blocks every time. But hey, these functions aren't likely to appear in many profiles. */
	memcpy (*data, versioninfo, datasize);

#if G_BYTE_ORDER == G_BIG_ENDIAN
	big_up (*data, datasize);
#endif

	mono_pe_file_unmap (file_map, map_handle);

	return TRUE;
}

gboolean
mono_w32process_ver_query_value (gconstpointer datablock, const gunichar2 *subblock, gpointer *buffer, guint32 *len)
{
	gchar *subblock_utf8, *lang_utf8 = NULL;
	gboolean ret = FALSE;
	version_data block;
	gconstpointer data_ptr;
	gint32 data_len; /* signed to guard against underflow */
	gboolean want_var = FALSE;
	gboolean want_string = FALSE;
	gunichar2 lang[8];
	const gunichar2 *string_key = NULL;
	gpointer string_value = NULL;
	guint32 string_value_len = 0;
	gchar *lowercase_lang;

	subblock_utf8 = g_utf16_to_utf8 (subblock, -1, NULL, NULL, NULL);
	if (subblock_utf8 == NULL) {
		return(FALSE);
	}

	if (!strcmp (subblock_utf8, "\\VarFileInfo\\Translation")) {
		want_var = TRUE;
	} else if (!strncmp (subblock_utf8, "\\StringFileInfo\\", 16)) {
		want_string = TRUE;
		memcpy (lang, subblock + 16, 8 * sizeof(gunichar2));
		lang_utf8 = g_utf16_to_utf8 (lang, 8, NULL, NULL, NULL);
		lowercase_lang = g_utf8_strdown (lang_utf8, -1);
		g_free (lang_utf8);
		lang_utf8 = lowercase_lang;
		lowercase_lang = NULL;
		string_key = subblock + 25;
	}

	if (!strcmp (subblock_utf8, "\\")) {
		data_ptr = get_fixedfileinfo_block (datablock, &block);
		if (data_ptr != NULL) {
			*buffer = (gpointer)data_ptr;
			*len = block.value_len;

			ret = TRUE;
		}
	} else if (want_var || want_string) {
		data_ptr = get_fixedfileinfo_block (datablock, &block);
		if (data_ptr != NULL) {
			/* The FFI and header occupies the first 92
			 * bytes
			 */
			data_ptr = (char *)data_ptr + sizeof(VS_FIXEDFILEINFO);
			data_len = block.data_len - 92;

			/* There now follow zero or one StringFileInfo
			 * blocks and zero or one VarFileInfo blocks
			 */
			while (data_len > 0) {
				/* align on a 32-bit boundary */
				ALIGN32 (data_ptr);

				data_ptr = get_versioninfo_block (data_ptr,
								  &block);
				if (block.data_len == 0) {
					/* We must have hit padding,
					 * so give up processing now
					 */
					mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: Hit 0-length block, giving up", __func__);
					goto done;
				}

				data_len = data_len - block.data_len;

				if (unicode_string_equals (block.key, "VarFileInfo")) {
					data_ptr = get_varfileinfo_block (data_ptr, &block);
					if (want_var) {
						*buffer = (gpointer)data_ptr;
						*len = block.value_len;
						ret = TRUE;
						goto done;
					} else {
						/* Skip over the Var block */
						data_ptr = ((guchar *)data_ptr) + block.value_len;
					}
				} else if (unicode_string_equals (block.key, "StringFileInfo")) {
					data_ptr = get_stringtable_block (data_ptr, lang_utf8, string_key, &string_value, &string_value_len, &block);
					if (want_string &&
					    string_value != NULL &&
					    string_value_len != 0) {
						*buffer = string_value;
						*len = unicode_chars ((const gunichar2 *)string_value) + 1; /* Include trailing null */
						ret = TRUE;
						goto done;
					}
				} else {
					/* Bogus data */
					mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: Not a valid VERSIONINFO child block", __func__);
					goto done;
				}

				if (data_ptr == NULL) {
					/* Child block hit padding */
					mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_IO_LAYER_PROCESS, "%s: Child block hit 0-length block, giving up", __func__);
					goto done;
				}
			}
		}
	}

  done:
	if (lang_utf8) {
		g_free (lang_utf8);
	}

	g_free (subblock_utf8);
	return(ret);
}

static guint32
copy_lang (gunichar2 *lang_out, guint32 lang_len, const gchar *text)
{
	gunichar2 *unitext;
	int chars = strlen (text);
	int ret;

	unitext = g_utf8_to_utf16 (text, -1, NULL, NULL, NULL);
	g_assert (unitext != NULL);

	if (chars < (lang_len - 1)) {
		memcpy (lang_out, (gpointer)unitext, chars * 2);
		lang_out[chars] = '\0';
		ret = chars;
	} else {
		memcpy (lang_out, (gpointer)unitext, (lang_len - 1) * 2);
		lang_out[lang_len] = '\0';
		ret = lang_len;
	}

	g_free (unitext);

	return(ret);
}

guint32
mono_w32process_ver_language_name (guint32 lang, gunichar2 *lang_out, guint32 lang_len)
{
	int primary, secondary;
	const char *name = NULL;

	primary = lang & 0x3FF;
	secondary = (lang >> 10) & 0x3F;

	switch(primary) {
	case 0x00:
		switch (secondary) {
		case 0x01: name = "Process Default Language"; break;
		}
		break;
	case 0x01:
		switch (secondary) {
		case 0x00:
		case 0x01: name = "Arabic (Saudi Arabia)"; break;
		case 0x02: name = "Arabic (Iraq)"; break;
		case 0x03: name = "Arabic (Egypt)"; break;
		case 0x04: name = "Arabic (Libya)"; break;
		case 0x05: name = "Arabic (Algeria)"; break;
		case 0x06: name = "Arabic (Morocco)"; break;
		case 0x07: name = "Arabic (Tunisia)"; break;
		case 0x08: name = "Arabic (Oman)"; break;
		case 0x09: name = "Arabic (Yemen)"; break;
		case 0x0a: name = "Arabic (Syria)"; break;
		case 0x0b: name = "Arabic (Jordan)"; break;
		case 0x0c: name = "Arabic (Lebanon)"; break;
		case 0x0d: name = "Arabic (Kuwait)"; break;
		case 0x0e: name = "Arabic (U.A.E.)"; break;
		case 0x0f: name = "Arabic (Bahrain)"; break;
		case 0x10: name = "Arabic (Qatar)"; break;
		}
		break;
	case 0x02:
		switch (secondary) {
		case 0x00: name = "Bulgarian (Bulgaria)"; break;
		case 0x01: name = "Bulgarian"; break;
		}
		break;
	case 0x03:
		switch (secondary) {
		case 0x00: name = "Catalan (Spain)"; break;
		case 0x01: name = "Catalan"; break;
		}
		break;
	case 0x04:
		switch (secondary) {
		case 0x00:
		case 0x01: name = "Chinese (Taiwan)"; break;
		case 0x02: name = "Chinese (PRC)"; break;
		case 0x03: name = "Chinese (Hong Kong S.A.R.)"; break;
		case 0x04: name = "Chinese (Singapore)"; break;
		case 0x05: name = "Chinese (Macau S.A.R.)"; break;
		}
		break;
	case 0x05:
		switch (secondary) {
		case 0x00: name = "Czech (Czech Republic)"; break;
		case 0x01: name = "Czech"; break;
		}
		break;
	case 0x06:
		switch (secondary) {
		case 0x00: name = "Danish (Denmark)"; break;
		case 0x01: name = "Danish"; break;
		}
		break;
	case 0x07:
		switch (secondary) {
		case 0x00:
		case 0x01: name = "German (Germany)"; break;
		case 0x02: name = "German (Switzerland)"; break;
		case 0x03: name = "German (Austria)"; break;
		case 0x04: name = "German (Luxembourg)"; break;
		case 0x05: name = "German (Liechtenstein)"; break;
		}
		break;
	case 0x08:
		switch (secondary) {
		case 0x00: name = "Greek (Greece)"; break;
		case 0x01: name = "Greek"; break;
		}
		break;
	case 0x09:
		switch (secondary) {
		case 0x00:
		case 0x01: name = "English (United States)"; break;
		case 0x02: name = "English (United Kingdom)"; break;
		case 0x03: name = "English (Australia)"; break;
		case 0x04: name = "English (Canada)"; break;
		case 0x05: name = "English (New Zealand)"; break;
		case 0x06: name = "English (Ireland)"; break;
		case 0x07: name = "English (South Africa)"; break;
		case 0x08: name = "English (Jamaica)"; break;
		case 0x09: name = "English (Caribbean)"; break;
		case 0x0a: name = "English (Belize)"; break;
		case 0x0b: name = "English (Trinidad and Tobago)"; break;
		case 0x0c: name = "English (Zimbabwe)"; break;
		case 0x0d: name = "English (Philippines)"; break;
		case 0x10: name = "English (India)"; break;
		case 0x11: name = "English (Malaysia)"; break;
		case 0x12: name = "English (Singapore)"; break;
		}
		break;
	case 0x0a:
		switch (secondary) {
		case 0x00: name = "Spanish (Spain)"; break;
		case 0x01: name = "Spanish (Traditional Sort)"; break;
		case 0x02: name = "Spanish (Mexico)"; break;
		case 0x03: name = "Spanish (International Sort)"; break;
		case 0x04: name = "Spanish (Guatemala)"; break;
		case 0x05: name = "Spanish (Costa Rica)"; break;
		case 0x06: name = "Spanish (Panama)"; break;
		case 0x07: name = "Spanish (Dominican Republic)"; break;
		case 0x08: name = "Spanish (Venezuela)"; break;
		case 0x09: name = "Spanish (Colombia)"; break;
		case 0x0a: name = "Spanish (Peru)"; break;
		case 0x0b: name = "Spanish (Argentina)"; break;
		case 0x0c: name = "Spanish (Ecuador)"; break;
		case 0x0d: name = "Spanish (Chile)"; break;
		case 0x0e: name = "Spanish (Uruguay)"; break;
		case 0x0f: name = "Spanish (Paraguay)"; break;
		case 0x10: name = "Spanish (Bolivia)"; break;
		case 0x11: name = "Spanish (El Salvador)"; break;
		case 0x12: name = "Spanish (Honduras)"; break;
		case 0x13: name = "Spanish (Nicaragua)"; break;
		case 0x14: name = "Spanish (Puerto Rico)"; break;
		case 0x15: name = "Spanish (United States)"; break;
		}
		break;
	case 0x0b:
		switch (secondary) {
		case 0x00: name = "Finnish (Finland)"; break;
		case 0x01: name = "Finnish"; break;
		}
		break;
	case 0x0c:
		switch (secondary) {
		case 0x00:
		case 0x01: name = "French (France)"; break;
		case 0x02: name = "French (Belgium)"; break;
		case 0x03: name = "French (Canada)"; break;
		case 0x04: name = "French (Switzerland)"; break;
		case 0x05: name = "French (Luxembourg)"; break;
		case 0x06: name = "French (Monaco)"; break;
		}
		break;
	case 0x0d:
		switch (secondary) {
		case 0x00: name = "Hebrew (Israel)"; break;
		case 0x01: name = "Hebrew"; break;
		}
		break;
	case 0x0e:
		switch (secondary) {
		case 0x00: name = "Hungarian (Hungary)"; break;
		case 0x01: name = "Hungarian"; break;
		}
		break;
	case 0x0f:
		switch (secondary) {
		case 0x00: name = "Icelandic (Iceland)"; break;
		case 0x01: name = "Icelandic"; break;
		}
		break;
	case 0x10:
		switch (secondary) {
		case 0x00:
		case 0x01: name = "Italian (Italy)"; break;
		case 0x02: name = "Italian (Switzerland)"; break;
		}
		break;
	case 0x11:
		switch (secondary) {
		case 0x00: name = "Japanese (Japan)"; break;
		case 0x01: name = "Japanese"; break;
		}
		break;
	case 0x12:
		switch (secondary) {
		case 0x00: name = "Korean (Korea)"; break;
		case 0x01: name = "Korean"; break;
		}
		break;
	case 0x13:
		switch (secondary) {
		case 0x00:
		case 0x01: name = "Dutch (Netherlands)"; break;
		case 0x02: name = "Dutch (Belgium)"; break;
		}
		break;
	case 0x14:
		switch (secondary) {
		case 0x00:
		case 0x01: name = "Norwegian (Bokmal)"; break;
		case 0x02: name = "Norwegian (Nynorsk)"; break;
		}
		break;
	case 0x15:
		switch (secondary) {
		case 0x00: name = "Polish (Poland)"; break;
		case 0x01: name = "Polish"; break;
		}
		break;
	case 0x16:
		switch (secondary) {
		case 0x00:
		case 0x01: name = "Portuguese (Brazil)"; break;
		case 0x02: name = "Portuguese (Portugal)"; break;
		}
		break;
	case 0x17:
		switch (secondary) {
		case 0x01: name = "Romansh (Switzerland)"; break;
		}
		break;
	case 0x18:
		switch (secondary) {
		case 0x00: name = "Romanian (Romania)"; break;
		case 0x01: name = "Romanian"; break;
		}
		break;
	case 0x19:
		switch (secondary) {
		case 0x00: name = "Russian (Russia)"; break;
		case 0x01: name = "Russian"; break;
		}
		break;
	case 0x1a:
		switch (secondary) {
		case 0x00: name = "Croatian (Croatia)"; break;
		case 0x01: name = "Croatian"; break;
		case 0x02: name = "Serbian (Latin)"; break;
		case 0x03: name = "Serbian (Cyrillic)"; break;
		case 0x04: name = "Croatian (Bosnia and Herzegovina)"; break;
		case 0x05: name = "Bosnian (Latin, Bosnia and Herzegovina)"; break;
		case 0x06: name = "Serbian (Latin, Bosnia and Herzegovina)"; break;
		case 0x07: name = "Serbian (Cyrillic, Bosnia and Herzegovina)"; break;
		case 0x08: name = "Bosnian (Cyrillic, Bosnia and Herzegovina)"; break;
		}
		break;
	case 0x1b:
		switch (secondary) {
		case 0x00: name = "Slovak (Slovakia)"; break;
		case 0x01: name = "Slovak"; break;
		}
		break;
	case 0x1c:
		switch (secondary) {
		case 0x00: name = "Albanian (Albania)"; break;
		case 0x01: name = "Albanian"; break;
		}
		break;
	case 0x1d:
		switch (secondary) {
		case 0x00: name = "Swedish (Sweden)"; break;
		case 0x01: name = "Swedish"; break;
		case 0x02: name = "Swedish (Finland)"; break;
		}
		break;
	case 0x1e:
		switch (secondary) {
		case 0x00: name = "Thai (Thailand)"; break;
		case 0x01: name = "Thai"; break;
		}
		break;
	case 0x1f:
		switch (secondary) {
		case 0x00: name = "Turkish (Turkey)"; break;
		case 0x01: name = "Turkish"; break;
		}
		break;
	case 0x20:
		switch (secondary) {
		case 0x00: name = "Urdu (Islamic Republic of Pakistan)"; break;
		case 0x01: name = "Urdu"; break;
		}
		break;
	case 0x21:
		switch (secondary) {
		case 0x00: name = "Indonesian (Indonesia)"; break;
		case 0x01: name = "Indonesian"; break;
		}
		break;
	case 0x22:
		switch (secondary) {
		case 0x00: name = "Ukrainian (Ukraine)"; break;
		case 0x01: name = "Ukrainian"; break;
		}
		break;
	case 0x23:
		switch (secondary) {
		case 0x00: name = "Belarusian (Belarus)"; break;
		case 0x01: name = "Belarusian"; break;
		}
		break;
	case 0x24:
		switch (secondary) {
		case 0x00: name = "Slovenian (Slovenia)"; break;
		case 0x01: name = "Slovenian"; break;
		}
		break;
	case 0x25:
		switch (secondary) {
		case 0x00: name = "Estonian (Estonia)"; break;
		case 0x01: name = "Estonian"; break;
		}
		break;
	case 0x26:
		switch (secondary) {
		case 0x00: name = "Latvian (Latvia)"; break;
		case 0x01: name = "Latvian"; break;
		}
		break;
	case 0x27:
		switch (secondary) {
		case 0x00: name = "Lithuanian (Lithuania)"; break;
		case 0x01: name = "Lithuanian"; break;
		}
		break;
	case 0x28:
		switch (secondary) {
		case 0x01: name = "Tajik (Tajikistan)"; break;
		}
		break;
	case 0x29:
		switch (secondary) {
		case 0x00: name = "Farsi (Iran)"; break;
		case 0x01: name = "Farsi"; break;
		}
		break;
	case 0x2a:
		switch (secondary) {
		case 0x00: name = "Vietnamese (Viet Nam)"; break;
		case 0x01: name = "Vietnamese"; break;
		}
		break;
	case 0x2b:
		switch (secondary) {
		case 0x00: name = "Armenian (Armenia)"; break;
		case 0x01: name = "Armenian"; break;
		}
		break;
	case 0x2c:
		switch (secondary) {
		case 0x00: name = "Azeri (Latin) (Azerbaijan)"; break;
		case 0x01: name = "Azeri (Latin)"; break;
		case 0x02: name = "Azeri (Cyrillic)"; break;
		}
		break;
	case 0x2d:
		switch (secondary) {
		case 0x00: name = "Basque (Spain)"; break;
		case 0x01: name = "Basque"; break;
		}
		break;
	case 0x2e:
		switch (secondary) {
		case 0x01: name = "Upper Sorbian (Germany)"; break;
		case 0x02: name = "Lower Sorbian (Germany)"; break;
		}
		break;
	case 0x2f:
		switch (secondary) {
		case 0x00: name = "FYRO Macedonian (Former Yugoslav Republic of Macedonia)"; break;
		case 0x01: name = "FYRO Macedonian"; break;
		}
		break;
	case 0x32:
		switch (secondary) {
		case 0x00: name = "Tswana (South Africa)"; break;
		case 0x01: name = "Tswana"; break;
		}
		break;
	case 0x34:
		switch (secondary) {
		case 0x00: name = "Xhosa (South Africa)"; break;
		case 0x01: name = "Xhosa"; break;
		}
		break;
	case 0x35:
		switch (secondary) {
		case 0x00: name = "Zulu (South Africa)"; break;
		case 0x01: name = "Zulu"; break;
		}
		break;
	case 0x36:
		switch (secondary) {
		case 0x00: name = "Afrikaans (South Africa)"; break;
		case 0x01: name = "Afrikaans"; break;
		}
		break;
	case 0x37:
		switch (secondary) {
		case 0x00: name = "Georgian (Georgia)"; break;
		case 0x01: name = "Georgian"; break;
		}
		break;
	case 0x38:
		switch (secondary) {
		case 0x00: name = "Faroese (Faroe Islands)"; break;
		case 0x01: name = "Faroese"; break;
		}
		break;
	case 0x39:
		switch (secondary) {
		case 0x00: name = "Hindi (India)"; break;
		case 0x01: name = "Hindi"; break;
		}
		break;
	case 0x3a:
		switch (secondary) {
		case 0x00: name = "Maltese (Malta)"; break;
		case 0x01: name = "Maltese"; break;
		}
		break;
	case 0x3b:
		switch (secondary) {
		case 0x00: name = "Sami (Northern) (Norway)"; break;
		case 0x01: name = "Sami, Northern (Norway)"; break;
		case 0x02: name = "Sami, Northern (Sweden)"; break;
		case 0x03: name = "Sami, Northern (Finland)"; break;
		case 0x04: name = "Sami, Lule (Norway)"; break;
		case 0x05: name = "Sami, Lule (Sweden)"; break;
		case 0x06: name = "Sami, Southern (Norway)"; break;
		case 0x07: name = "Sami, Southern (Sweden)"; break;
		case 0x08: name = "Sami, Skolt (Finland)"; break;
		case 0x09: name = "Sami, Inari (Finland)"; break;
		}
		break;
	case 0x3c:
		switch (secondary) {
		case 0x02: name = "Irish (Ireland)"; break;
		}
		break;
	case 0x3e:
		switch (secondary) {
		case 0x00:
		case 0x01: name = "Malay (Malaysia)"; break;
		case 0x02: name = "Malay (Brunei Darussalam)"; break;
		}
		break;
	case 0x3f:
		switch (secondary) {
		case 0x00: name = "Kazakh (Kazakhstan)"; break;
		case 0x01: name = "Kazakh"; break;
		}
		break;
	case 0x40:
		switch (secondary) {
		case 0x00: name = "Kyrgyz (Kyrgyzstan)"; break;
		case 0x01: name = "Kyrgyz (Cyrillic)"; break;
		}
		break;
	case 0x41:
		switch (secondary) {
		case 0x00: name = "Swahili (Kenya)"; break;
		case 0x01: name = "Swahili"; break;
		}
		break;
	case 0x42:
		switch (secondary) {
		case 0x01: name = "Turkmen (Turkmenistan)"; break;
		}
		break;
	case 0x43:
		switch (secondary) {
		case 0x00: name = "Uzbek (Latin) (Uzbekistan)"; break;
		case 0x01: name = "Uzbek (Latin)"; break;
		case 0x02: name = "Uzbek (Cyrillic)"; break;
		}
		break;
	case 0x44:
		switch (secondary) {
		case 0x00: name = "Tatar (Russia)"; break;
		case 0x01: name = "Tatar"; break;
		}
		break;
	case 0x45:
		switch (secondary) {
		case 0x00:
		case 0x01: name = "Bengali (India)"; break;
		}
		break;
	case 0x46:
		switch (secondary) {
		case 0x00: name = "Punjabi (India)"; break;
		case 0x01: name = "Punjabi"; break;
		}
		break;
	case 0x47:
		switch (secondary) {
		case 0x00: name = "Gujarati (India)"; break;
		case 0x01: name = "Gujarati"; break;
		}
		break;
	case 0x49:
		switch (secondary) {
		case 0x00: name = "Tamil (India)"; break;
		case 0x01: name = "Tamil"; break;
		}
		break;
	case 0x4a:
		switch (secondary) {
		case 0x00: name = "Telugu (India)"; break;
		case 0x01: name = "Telugu"; break;
		}
		break;
	case 0x4b:
		switch (secondary) {
		case 0x00: name = "Kannada (India)"; break;
		case 0x01: name = "Kannada"; break;
		}
		break;
	case 0x4c:
		switch (secondary) {
		case 0x00:
		case 0x01: name = "Malayalam (India)"; break;
		}
		break;
	case 0x4d:
		switch (secondary) {
		case 0x01: name = "Assamese (India)"; break;
		}
		break;
	case 0x4e:
		switch (secondary) {
		case 0x00: name = "Marathi (India)"; break;
		case 0x01: name = "Marathi"; break;
		}
		break;
	case 0x4f:
		switch (secondary) {
		case 0x00: name = "Sanskrit (India)"; break;
		case 0x01: name = "Sanskrit"; break;
		}
		break;
	case 0x50:
		switch (secondary) {
		case 0x00: name = "Mongolian (Mongolia)"; break;
		case 0x01: name = "Mongolian (Cyrillic)"; break;
		case 0x02: name = "Mongolian (PRC)"; break;
		}
		break;
	case 0x51:
		switch (secondary) {
		case 0x01: name = "Tibetan (PRC)"; break;
		case 0x02: name = "Tibetan (Bhutan)"; break;
		}
		break;
	case 0x52:
		switch (secondary) {
		case 0x00: name = "Welsh (United Kingdom)"; break;
		case 0x01: name = "Welsh"; break;
		}
		break;
	case 0x53:
		switch (secondary) {
		case 0x01: name = "Khmer (Cambodia)"; break;
		}
		break;
	case 0x54:
		switch (secondary) {
		case 0x01: name = "Lao (Lao PDR)"; break;
		}
		break;
	case 0x56:
		switch (secondary) {
		case 0x00: name = "Galician (Spain)"; break;
		case 0x01: name = "Galician"; break;
		}
		break;
	case 0x57:
		switch (secondary) {
		case 0x00: name = "Konkani (India)"; break;
		case 0x01: name = "Konkani"; break;
		}
		break;
	case 0x5a:
		switch (secondary) {
		case 0x00: name = "Syriac (Syria)"; break;
		case 0x01: name = "Syriac"; break;
		}
		break;
	case 0x5b:
		switch (secondary) {
		case 0x01: name = "Sinhala (Sri Lanka)"; break;
		}
		break;
	case 0x5d:
		switch (secondary) {
		case 0x01: name = "Inuktitut (Syllabics, Canada)"; break;
		case 0x02: name = "Inuktitut (Latin, Canada)"; break;
		}
		break;
	case 0x5e:
		switch (secondary) {
		case 0x01: name = "Amharic (Ethiopia)"; break;
		}
		break;
	case 0x5f:
		switch (secondary) {
		case 0x02: name = "Tamazight (Algeria, Latin)"; break;
		}
		break;
	case 0x61:
		switch (secondary) {
		case 0x01: name = "Nepali (Nepal)"; break;
		}
		break;
	case 0x62:
		switch (secondary) {
		case 0x01: name = "Frisian (Netherlands)"; break;
		}
		break;
	case 0x63:
		switch (secondary) {
		case 0x01: name = "Pashto (Afghanistan)"; break;
		}
		break;
	case 0x64:
		switch (secondary) {
		case 0x01: name = "Filipino (Philippines)"; break;
		}
		break;
	case 0x65:
		switch (secondary) {
		case 0x00: name = "Divehi (Maldives)"; break;
		case 0x01: name = "Divehi"; break;
		}
		break;
	case 0x68:
		switch (secondary) {
		case 0x01: name = "Hausa (Nigeria, Latin)"; break;
		}
		break;
	case 0x6a:
		switch (secondary) {
		case 0x01: name = "Yoruba (Nigeria)"; break;
		}
		break;
	case 0x6b:
		switch (secondary) {
		case 0x00:
		case 0x01: name = "Quechua (Bolivia)"; break;
		case 0x02: name = "Quechua (Ecuador)"; break;
		case 0x03: name = "Quechua (Peru)"; break;
		}
		break;
	case 0x6c:
		switch (secondary) {
		case 0x00: name = "Northern Sotho (South Africa)"; break;
		case 0x01: name = "Northern Sotho"; break;
		}
		break;
	case 0x6d:
		switch (secondary) {
		case 0x01: name = "Bashkir (Russia)"; break;
		}
		break;
	case 0x6e:
		switch (secondary) {
		case 0x01: name = "Luxembourgish (Luxembourg)"; break;
		}
		break;
	case 0x6f:
		switch (secondary) {
		case 0x01: name = "Greenlandic (Greenland)"; break;
		}
		break;
	case 0x78:
		switch (secondary) {
		case 0x01: name = "Yi (PRC)"; break;
		}
		break;
	case 0x7a:
		switch (secondary) {
		case 0x01: name = "Mapudungun (Chile)"; break;
		}
		break;
	case 0x7c:
		switch (secondary) {
		case 0x01: name = "Mohawk (Mohawk)"; break;
		}
		break;
	case 0x7e:
		switch (secondary) {
		case 0x01: name = "Breton (France)"; break;
		}
		break;
	case 0x7f:
		switch (secondary) {
		case 0x00: name = "Invariant Language (Invariant Country)"; break;
		}
		break;
	case 0x80:
		switch (secondary) {
		case 0x01: name = "Uighur (PRC)"; break;
		}
		break;
	case 0x81:
		switch (secondary) {
		case 0x00: name = "Maori (New Zealand)"; break;
		case 0x01: name = "Maori"; break;
		}
		break;
	case 0x83:
		switch (secondary) {
		case 0x01: name = "Corsican (France)"; break;
		}
		break;
	case 0x84:
		switch (secondary) {
		case 0x01: name = "Alsatian (France)"; break;
		}
		break;
	case 0x85:
		switch (secondary) {
		case 0x01: name = "Yakut (Russia)"; break;
		}
		break;
	case 0x86:
		switch (secondary) {
		case 0x01: name = "K'iche (Guatemala)"; break;
		}
		break;
	case 0x87:
		switch (secondary) {
		case 0x01: name = "Kinyarwanda (Rwanda)"; break;
		}
		break;
	case 0x88:
		switch (secondary) {
		case 0x01: name = "Wolof (Senegal)"; break;
		}
		break;
	case 0x8c:
		switch (secondary) {
		case 0x01: name = "Dari (Afghanistan)"; break;
		}
		break;

	default:
		name = "Language Neutral";

	}

	if (!name)
		name = "Language Neutral";

	return copy_lang (lang_out, lang_len, name);
}
