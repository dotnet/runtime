/**
 * \file
 * System.Diagnostics.Process support
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#ifndef _MONO_METADATA_W32PROCESS_H_
#define _MONO_METADATA_W32PROCESS_H_

#include <config.h>
#include <glib.h>

#if HAVE_SYS_TYPES_H
#include <sys/types.h>
#endif

#include <mono/metadata/object.h>
#include "object-internals.h"
#include "marshal.h"

G_BEGIN_DECLS

typedef enum {
	MONO_W32PROCESS_PRIORITY_CLASS_NORMAL       = 0x0020,
	MONO_W32PROCESS_PRIORITY_CLASS_IDLE         = 0x0040,
	MONO_W32PROCESS_PRIORITY_CLASS_HIGH         = 0x0080,
	MONO_W32PROCESS_PRIORITY_CLASS_REALTIME     = 0x0100,
	MONO_W32PROCESS_PRIORITY_CLASS_BELOW_NORMAL = 0x4000,
	MONO_W32PROCESS_PRIORITY_CLASS_ABOVE_NORMAL = 0x8000,
} MonoW32ProcessPriorityClass;

typedef struct 
{
	gpointer process_handle;
	guint32 pid; /* Contains mono_w32error_get_last () on failure */
	MonoArray *env_variables;
	MonoString *username;
	MonoString *domain;
	mono_bstr password; /* BSTR from SecureString in 2.0 profile */
	MonoBoolean load_user_profile;
} MonoW32ProcessInfo;

typedef struct
{
	MonoObject object;
	MonoString *filename;
	MonoString *arguments;
	MonoString *working_directory;
	MonoString *verb;
	guint32 window_style;
	MonoBoolean error_dialog;
	gpointer error_dialog_parent_handle;
	MonoBoolean use_shell_execute;

	MonoString *unused_username;
	MonoString *unused_domain;
	MonoObject *unused_password; /* SecureString in 2.0 profile, dummy in 1.x */
	MonoString *unused_password_in_clear_text;
	MonoBoolean unused_load_user_profile;
	MonoBoolean unused_redirect_standard_input;
	MonoBoolean unused_redirect_standard_output;
	MonoBoolean unused_redirect_standard_error;
	MonoObject *unused_encoding_stdout;
	MonoObject *unused_encoding_stderr;

	MonoBoolean create_no_window;

	MonoObject *unused_weak_parent_process;
	MonoObject *unused_envVars;

} MonoW32ProcessStartInfo;

TYPED_HANDLE_DECL (MonoW32ProcessStartInfo);

typedef uint32_t gchandle_t; // FIXME use this more, make it typesafe.

typedef struct _MonoCreateProcessCoop {
	gunichar2 *filename;
	gunichar2 *arguments;
	gunichar2 *working_directory;
	gunichar2 *verb;
	gunichar2 *username;
	gunichar2 *domain;
	struct {
		MonoStringHandle filename;
		MonoStringHandle arguments;
		MonoStringHandle working_directory;
		MonoStringHandle verb;
		MonoStringHandle username;
		MonoStringHandle domain;
	} coophandle;
	struct {
		gchandle_t filename;
		gchandle_t arguments;
		gchandle_t working_directory;
		gchandle_t verb;
		gchandle_t username;
		gchandle_t domain;
	} gchandle;
	struct {
		gsize filename;
		gsize arguments;
		gsize working_directory;
		gsize verb;
		gsize username;
		gsize domain;
	} length;
} MonoCreateProcessCoop;

void
mono_createprocess_coop_init (MonoCreateProcessCoop *coop, MonoW32ProcessStartInfoHandle proc_start_info, MonoW32ProcessInfo *process_info);

void
mono_createprocess_coop_cleanup (MonoCreateProcessCoop *coop);

void
mono_w32process_init (void);

void
mono_w32process_cleanup (void);

void
mono_w32process_signal_finished (void);

#ifndef HOST_WIN32

void
mono_w32process_set_cli_launcher (gchar *path);

gchar*
mono_w32process_get_path (pid_t pid);

#endif

gpointer
ves_icall_System_Diagnostics_Process_GetProcess_internal (guint32 pid);

MonoArray*
ves_icall_System_Diagnostics_Process_GetProcesses_internal (void);

MonoArray*
ves_icall_System_Diagnostics_Process_GetModules_internal (MonoObject *this_obj, gpointer process);

void
ves_icall_System_Diagnostics_FileVersionInfo_GetVersionInfo_internal (MonoObject *this_obj, MonoString *filename);

MonoBoolean
ves_icall_System_Diagnostics_Process_ShellExecuteEx_internal (MonoW32ProcessStartInfoHandle proc_start_info, MonoW32ProcessInfo *process_info, MonoError *error);

MonoBoolean
ves_icall_System_Diagnostics_Process_CreateProcess_internal (MonoW32ProcessStartInfoHandle proc_start_info, gpointer stdin_handle,
	gpointer stdout_handle, gpointer stderr_handle, MonoW32ProcessInfo *process_handle, MonoError *error);

MonoString*
ves_icall_System_Diagnostics_Process_ProcessName_internal (gpointer process);

gint64
ves_icall_System_Diagnostics_Process_GetProcessData (int pid, gint32 data_type, gint32 *error);

gpointer
ves_icall_Microsoft_Win32_NativeMethods_GetCurrentProcess (MonoError *error);

MonoBoolean
ves_icall_Microsoft_Win32_NativeMethods_GetExitCodeProcess (gpointer handle, gint32 *exitcode, MonoError *error);

MonoBoolean
ves_icall_Microsoft_Win32_NativeMethods_CloseProcess (gpointer handle, MonoError *error);

MonoBoolean
ves_icall_Microsoft_Win32_NativeMethods_TerminateProcess (gpointer handle, gint32 exitcode, MonoError *error);

MonoBoolean
ves_icall_Microsoft_Win32_NativeMethods_GetProcessWorkingSetSize (gpointer handle, gsize *min, gsize *max, MonoError *error);

MonoBoolean
ves_icall_Microsoft_Win32_NativeMethods_SetProcessWorkingSetSize (gpointer handle, gsize min, gsize max, MonoError *error);

gint32
ves_icall_Microsoft_Win32_NativeMethods_GetPriorityClass (gpointer handle, MonoError *error);

MonoBoolean
ves_icall_Microsoft_Win32_NativeMethods_SetPriorityClass (gpointer handle, gint32 priorityClass, MonoError *error);

MonoBoolean
ves_icall_Microsoft_Win32_NativeMethods_GetProcessTimes (gpointer handle, gint64 *creationtime, gint64 *exittime, gint64 *kerneltime, gint64 *usertime, MonoError *error);

G_END_DECLS

#endif /* _MONO_METADATA_W32PROCESS_H_ */

