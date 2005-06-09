/*
 * environment.h: System.Environment support internal calls
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc
 */

#ifndef _MONO_METADATA_ENVIRONMENT_H_
#define _MONO_METADATA_ENVIRONMENT_H_

G_BEGIN_DECLS

extern gint32 mono_environment_exitcode_get (void);
extern void mono_environment_exitcode_set (gint32 value);

extern MonoString* ves_icall_System_Environment_GetOSVersionString (void);

G_END_DECLS

#endif /* _MONO_METADATA_ENVIRONMENT_H_ */
