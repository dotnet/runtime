/*
 * metadata/gc.h: GC icalls.
 *
 * Author: Paolo Molaro <lupus@ximian.com>
 *
 * (C) 2002 Ximian, Inc.
 */

#ifndef __MONO_METADATA_GC_H__
#define __MONO_METADATA_GC_H__

#include <glib.h>
#include <mono/metadata/object.h>

void   mono_object_register_finalizer               (MonoObject  *obj);
void   ves_icall_System_GC_InternalCollect          (int          generation);
gint64 ves_icall_System_GC_GetTotalMemory           (MonoBoolean  forceCollection);
void   ves_icall_System_GC_KeepAlive                (MonoObject  *obj);
void   ves_icall_System_GC_ReRegisterForFinalize    (MonoObject  *obj);
void   ves_icall_System_GC_SuppressFinalize         (MonoObject  *obj);
void   ves_icall_System_GC_WaitForPendingFinalizers (void);

#endif /* __MONO_METADATA_GC_H__ */

