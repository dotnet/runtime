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

typedef struct 
{
	HANDLE process_handle;
	HANDLE thread_handle;
	guint32 pid; /* Contains GetLastError () on failure */
	guint32 tid;
	MonoArray *env_keys;
	MonoArray *env_values;
	MonoBoolean use_shell;
} MonoProcInfo;

G_BEGIN_DECLS

HANDLE ves_icall_System_Diagnostics_Process_GetProcess_internal (guint32 pid);
MonoArray *ves_icall_System_Diagnostics_Process_GetProcesses_internal (void);
guint32 ves_icall_System_Diagnostics_Process_GetPid_internal (void);
void ves_icall_System_Diagnostics_Process_Process_free_internal (MonoObject *this, HANDLE process);
MonoArray *ves_icall_System_Diagnostics_Process_GetModules_internal (MonoObject *this);
void ves_icall_System_Diagnostics_FileVersionInfo_GetVersionInfo_internal (MonoObject *this, MonoString *filename);
MonoBoolean ves_icall_System_Diagnostics_Process_Start_internal (MonoString *cmd, MonoString *dirname, HANDLE stdin_handle, HANDLE stdout_handle, HANDLE stderr_handle, MonoProcInfo *process_handle);
MonoBoolean ves_icall_System_Diagnostics_Process_WaitForExit_internal (MonoObject *this, HANDLE process, gint32 ms);
gint64 ves_icall_System_Diagnostics_Process_ExitTime_internal (HANDLE process);
gint64 ves_icall_System_Diagnostics_Process_StartTime_internal (HANDLE process);
gint32 ves_icall_System_Diagnostics_Process_ExitCode_internal (HANDLE process);
MonoString *ves_icall_System_Diagnostics_Process_ProcessName_internal (HANDLE process);
MonoBoolean ves_icall_System_Diagnostics_Process_GetWorkingSet_internal (HANDLE process, guint32 *min, guint32 *max);
MonoBoolean ves_icall_System_Diagnostics_Process_SetWorkingSet_internal (HANDLE process, guint32 min, guint32 max, MonoBoolean use_min);
MonoBoolean ves_icall_System_Diagnostics_Process_Kill_internal (HANDLE process, gint32 sig);

G_END_DECLS

#endif /* _MONO_METADATA_PROCESS_H_ */

