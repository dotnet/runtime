/**
 * \file
 */

#ifndef __MONO_METADATA_MONO_MLIST_H__
#define __MONO_METADATA_MONO_MLIST_H__

/*
 * mono-mlist.h: Managed object list implementation
 */

#include <mono/metadata/object.h>

typedef struct _MonoMList MonoMList;
MONO_RT_EXTERNAL_ONLY
MONO_API MonoMList*  mono_mlist_alloc       (MonoObject *data);
MONO_API MonoObject* mono_mlist_get_data    (MonoMList* list);
MONO_API void        mono_mlist_set_data    (MonoMList* list, MonoObject *data);
MONO_API MonoMList*  mono_mlist_set_next    (MonoMList* list, MonoMList *next);
MONO_API int         mono_mlist_length      (MonoMList* list);
MONO_API MonoMList*  mono_mlist_next        (MonoMList* list);
MONO_API MonoMList*  mono_mlist_last        (MonoMList* list);
MONO_RT_EXTERNAL_ONLY
MONO_API MonoMList*  mono_mlist_prepend     (MonoMList* list, MonoObject *data);
MONO_RT_EXTERNAL_ONLY
MONO_API MonoMList*  mono_mlist_append      (MonoMList* list, MonoObject *data);

MonoMList*  mono_mlist_prepend_checked      (MonoMList* list, MonoObject *data, MonoError *error);
MonoMList*  mono_mlist_append_checked       (MonoMList* list, MonoObject *data, MonoError *error);

MONO_API MonoMList*  mono_mlist_remove_item (MonoMList* list, MonoMList *item);

#endif /* __MONO_METADATA_MONO_MLIST_H__ */

