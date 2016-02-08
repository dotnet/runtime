/*
 * process.h: System.Diagnostics.Process support
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#ifndef _MONO_METADATA_PROCESS_H_
#define _MONO_METADATA_PROCESS_H_

#include <config.h>
#include <glib.h>

#include <mono/metadata/object.h>
#include <mono/io-layer/io-layer.h>
#include "mono/utils/mono-compiler.h"

typedef struct 
{
	HANDLE process_handle;
	HANDLE thread_handle;
	guint32 pid; /* Contains GetLastError () on failure */
	guint32 tid;
	MonoArray *env_keys;
	MonoArray *env_values;
	MonoString *username;
	MonoString *domain;
	gpointer password; /* BSTR from SecureString in 2.0 profile */
	MonoBoolean load_user_profile;
} MonoProcInfo;

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
	MonoString *username;
	MonoString *domain;
	MonoObject *password; /* SecureString in 2.0 profile, dummy in 1.x */
	MonoString *password_in_clear_text;
	MonoBoolean load_user_profile;
	MonoBoolean redirect_standard_input;
	MonoBoolean redirect_standard_output;
	MonoBoolean redirect_standard_error;
	MonoObject *encoding_stdout;
	MonoObject *encoding_stderr;
	MonoBoolean create_no_window;
	MonoObject *weak_parent_process;
	MonoObject *envVars;
} MonoProcessStartInfo;

G_BEGIN_DECLS

HANDLE ves_icall_System_Diagnostics_Process_GetProcess_internal (guint32 pid);
MonoArray *ves_icall_System_Diagnostics_Process_GetProcesses_internal (void);
MonoArray *ves_icall_System_Diagnostics_Process_GetModules_internal (MonoObject *this_obj, HANDLE process);
void ves_icall_System_Diagnostics_FileVersionInfo_GetVersionInfo_internal (MonoObject *this_obj, MonoString *filename);
MonoBoolean ves_icall_System_Diagnostics_Process_ShellExecuteEx_internal (MonoProcessStartInfo *proc_start_info, MonoProcInfo *process_handle);
MonoBoolean ves_icall_System_Diagnostics_Process_CreateProcess_internal (MonoProcessStartInfo *proc_start_info, HANDLE stdin_handle, HANDLE stdout_handle, HANDLE stderr_handle, MonoProcInfo *process_handle);
MonoString *ves_icall_System_Diagnostics_Process_ProcessName_internal (HANDLE process);
gint64 ves_icall_System_Diagnostics_Process_GetProcessData (int pid, gint32 data_type, gint32 *error);

G_END_DECLS

#endif /* _MONO_METADATA_PROCESS_H_ */

