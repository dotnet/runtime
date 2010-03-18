#ifndef __MONO_METADATA_MONO_MLIST_H__
#define __MONO_METADATA_MONO_MLIST_H__

/*
 * mono-mlist.h: Managed object list implementation
 */

#include <mono/metadata/object.h>

typedef struct _MonoMList MonoMList;
MonoMList*  mono_mlist_alloc       (MonoObject *data);
MonoObject* mono_mlist_get_data    (MonoMList* list);
void        mono_mlist_set_data    (MonoMList* list, MonoObject *data);
MonoMList*  mono_mlist_set_next    (MonoMList* list, MonoMList *next);
int         mono_mlist_length      (MonoMList* list);
MonoMList*  mono_mlist_next        (MonoMList* list);
MonoMList*  mono_mlist_last        (MonoMList* list);
MonoMList*  mono_mlist_prepend     (MonoMList* list, MonoObject *data);
MonoMList*  mono_mlist_append      (MonoMList* list, MonoObject *data);
MonoMList*  mono_mlist_remove_item (MonoMList* list, MonoMList *item);

#endif /* __MONO_METADATA_MONO_MLIST_H__ */

