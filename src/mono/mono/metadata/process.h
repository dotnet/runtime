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
	guint32 pid;
	guint32 tid;
} MonoProcInfo;

extern HANDLE ves_icall_System_Diagnostics_Process_GetProcess_internal (guint32 pid);
extern MonoArray *ves_icall_System_Diagnostics_Process_GetProcesses_internal (void);
extern guint32 ves_icall_System_Diagnostics_Process_GetPid_internal (void);
extern void ves_icall_System_Diagnostics_Process_Process_free_internal (MonoObject *this, HANDLE process);
extern MonoArray *ves_icall_System_Diagnostics_Process_GetModules_internal (MonoObject *this);
extern void ves_icall_System_Diagnostics_FileVersionInfo_GetVersionInfo_internal (MonoObject *this, MonoString *filename);
extern MonoBoolean ves_icall_System_Diagnostics_Process_Start_internal (MonoString *cmd, MonoString *dirname, HANDLE stdin_handle, HANDLE stdout_handle, HANDLE stderr_handle, MonoProcInfo *process_handle);
extern MonoBoolean ves_icall_System_Diagnostics_Process_WaitForExit_internal (MonoObject *this, HANDLE process, gint32 ms);
extern gint64 ves_icall_System_Diagnostics_Process_ExitTime_internal (HANDLE process);
extern gint64 ves_icall_System_Diagnostics_Process_StartTime_internal (HANDLE process);
extern gint32 ves_icall_System_Diagnostics_Process_ExitCode_internal (HANDLE process);
extern MonoString *ves_icall_System_Diagnostics_Process_ProcessName_internal (HANDLE process);
extern MonoBoolean ves_icall_System_Diagnostics_Process_GetWorkingSet_internal (HANDLE process, guint32 *min, guint32 *max);
extern MonoBoolean ves_icall_System_Diagnostics_Process_SetWorkingSet_internal (HANDLE process, guint32 min, guint32 max, MonoBoolean use_min);

#endif /* _MONO_METADATA_PROCESS_H_ */
