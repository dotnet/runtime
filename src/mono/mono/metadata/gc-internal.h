/*
 * metadata/gc-internal.h: GC icalls.
 *
 * Author: Paolo Molaro <lupus@ximian.com>
 *
 * (C) 2002 Ximian, Inc.
 */

#ifndef __MONO_METADATA_GC_H__
#define __MONO_METADATA_GC_H__

#include <glib.h>
#include <mono/metadata/object-internals.h>

void   mono_object_register_finalizer               (MonoObject  *obj);
void   ves_icall_System_GC_InternalCollect          (int          generation);
gint64 ves_icall_System_GC_GetTotalMemory           (MonoBoolean  forceCollection);
void   ves_icall_System_GC_KeepAlive                (MonoObject  *obj);
void   ves_icall_System_GC_ReRegisterForFinalize    (MonoObject  *obj);
void   ves_icall_System_GC_SuppressFinalize         (MonoObject  *obj);
void   ves_icall_System_GC_WaitForPendingFinalizers (void);

MonoObject *ves_icall_System_GCHandle_GetTarget (guint32 handle);
guint32     ves_icall_System_GCHandle_GetTargetHandle (MonoObject *obj, guint32 handle, gint32 type);
void        ves_icall_System_GCHandle_FreeHandle (guint32 handle);
gpointer    ves_icall_System_GCHandle_GetAddrOfPinnedObject (guint32 handle);

extern void mono_gc_init (void);
extern void mono_gc_cleanup (void);
extern void mono_gc_enable (void);
extern void mono_gc_disable (void);
extern gboolean mono_gc_is_finalizer_thread (MonoThread *thread);
extern gpointer mono_gc_out_of_memory (size_t size);

#endif /* __MONO_METADATA_GC_H__ */

