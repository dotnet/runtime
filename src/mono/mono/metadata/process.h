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

extern HANDLE ves_icall_System_Diagnostics_Process_GetCurrentProcess_internal (void);
extern guint32 ves_icall_System_Diagnostics_Process_GetPid_internal (void);
extern void ves_icall_System_Diagnostics_Process_Process_free_internal (MonoObject *this, HANDLE process);
extern MonoArray *ves_icall_System_Diagnostics_Process_GetModules_internal (MonoObject *this);
extern void ves_icall_System_Diagnostics_FileVersionInfo_GetVersionInfo_internal (MonoObject *this, MonoString *filename);
extern MonoBoolean ves_icall_System_Diagnostics_Process_Start_internal (MonoString *filename, MonoString *args, HANDLE stdin_handle, HANDLE stdout_handle, HANDLE stderr_handle, MonoProcInfo *process_handle);
extern MonoBoolean ves_icall_System_Diagnostics_Process_WaitForExit_internal (MonoObject *this, HANDLE process, gint32 ms);
extern gint64 ves_icall_System_Diagnostics_Process_ExitTime_internal (HANDLE process);
extern gint64 ves_icall_System_Diagnostics_Process_StartTime_internal (HANDLE process);
extern gint32 ves_icall_System_Diagnostics_Process_ExitCode_internal (HANDLE process);

#endif /* _MONO_METADATA_PROCESS_H_ */
