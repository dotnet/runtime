/*
 * threads-dummy.h: System-specific thread support dummy routines
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */

#ifndef _MONO_METADATA_THREADS_DUMMY_H_
#define _MONO_METADATA_THREADS_DUMMY_H_

#include <mono/metadata/object.h>

extern void ves_icall_System_Threading_Thread_Start_internal(MonoObject *this, MonoObject *start);
extern int ves_icall_System_Threading_Thread_Sleep_internal(int ms);
extern void ves_icall_System_Threading_Thread_Schedule_internal(void);
extern MonoObject *ves_icall_System_Threading_Thread_CurrentThread_internal(void);

#endif /* _MONO_METADATA_THREADS_DUMMY_H_ */
