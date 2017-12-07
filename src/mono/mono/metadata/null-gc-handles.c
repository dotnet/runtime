/* Borrowed from ./boehm-gc.c */

/* GC Handles */

#include "config.h"

#include <glib.h>
#include <mono/metadata/mono-gc.h>
#include <mono/metadata/gc-internals.h>
#include <mono/metadata/profiler-private.h>
#include <mono/utils/mono-compiler.h>
#include <mono/metadata/null-gc-handles.h>


#ifdef HAVE_NULL_GC

#define HIDE_POINTER(obj) (obj)
#define REVEAL_POINTER(obj) (obj)

#define GC_call_with_alloc_lock(fnptr,arg) ((fnptr)((arg)))

static mono_mutex_t handle_section;
#define lock_handles(handles) mono_os_mutex_lock (&handle_section)
#define unlock_handles(handles) mono_os_mutex_unlock (&handle_section)

typedef struct {
	guint32  *bitmap;
	gpointer *entries;
	guint32   size;
	guint8    type;
	guint     slot_hint : 24; /* starting slot for search in bitmap */
	/* 2^16 appdomains should be enough for everyone (though I know I'll regret this in 20 years) */
	/* we alloc this only for weak refs, since we can get the domain directly in the other cases */
	guint16  *domain_ids;
} HandleData;

#define EMPTY_HANDLE_DATA(type) {NULL, NULL, 0, (type), 0, NULL}

/* weak and weak-track arrays will be allocated in malloc memory 
 */
static HandleData gc_handles [] = {
	EMPTY_HANDLE_DATA (HANDLE_WEAK),
	EMPTY_HANDLE_DATA (HANDLE_WEAK_TRACK),
	EMPTY_HANDLE_DATA (HANDLE_NORMAL),
	EMPTY_HANDLE_DATA (HANDLE_PINNED)
};

#define BITMAP_SIZE (sizeof (*((HandleData *)NULL)->bitmap) * CHAR_BIT)

void
null_gc_handles_init (void)
{
	mono_os_mutex_init_recursive (&handle_section);
}

static void
mono_gc_weak_link_add (void **link_addr, MonoObject *obj, gboolean track)
{
	/* libgc requires that we use HIDE_POINTER... */
	*link_addr = (void*)HIDE_POINTER (obj);
}

static void
mono_gc_weak_link_remove (void **link_addr, gboolean track)
{
	*link_addr = NULL;
}

static gpointer
reveal_link (gpointer link_addr)
{
	void **link_a = (void **)link_addr;
	return REVEAL_POINTER (*link_a);
}

static MonoObject *
mono_gc_weak_link_get (void **link_addr)
{
	MonoObject *obj = (MonoObject *)GC_call_with_alloc_lock (reveal_link, link_addr);
	if (obj == (MonoObject *) -1)
		return NULL;
	return obj;
}

static inline gboolean
slot_occupied (HandleData *handles, guint slot) {
	return handles->bitmap [slot / BITMAP_SIZE] & (1 << (slot % BITMAP_SIZE));
}

static inline void
vacate_slot (HandleData *handles, guint slot) {
	handles->bitmap [slot / BITMAP_SIZE] &= ~(1 << (slot % BITMAP_SIZE));
}

static inline void
occupy_slot (HandleData *handles, guint slot) {
	handles->bitmap [slot / BITMAP_SIZE] |= 1 << (slot % BITMAP_SIZE);
}

static int
find_first_unset (guint32 bitmap)
{
	int i;
	for (i = 0; i < 32; ++i) {
		if (!(bitmap & (1 << i)))
			return i;
	}
	return -1;
}

static void
handle_data_alloc_entries (HandleData *handles)
{
	handles->size = 32;
	if (MONO_GC_HANDLE_TYPE_IS_WEAK (handles->type)) {
		handles->entries = (void **)g_malloc0 (sizeof (*handles->entries) * handles->size);
		handles->domain_ids = (guint16 *)g_malloc0 (sizeof (*handles->domain_ids) * handles->size);
	} else {
		handles->entries = (void **)mono_gc_alloc_fixed (sizeof (*handles->entries) * handles->size, NULL, MONO_ROOT_SOURCE_GC_HANDLE, "GC Handle Table (Null)");
	}
	handles->bitmap = (guint32 *)g_malloc0 (handles->size / CHAR_BIT);
}

static gint
handle_data_next_unset (HandleData *handles)
{
	gint slot;
	for (slot = handles->slot_hint; slot < handles->size / BITMAP_SIZE; ++slot) {
		if (handles->bitmap [slot] == 0xffffffff)
			continue;
		handles->slot_hint = slot;
		return find_first_unset (handles->bitmap [slot]);
	}
	return -1;
}

static gint
handle_data_first_unset (HandleData *handles)
{
	gint slot;
	for (slot = 0; slot < handles->slot_hint; ++slot) {
		if (handles->bitmap [slot] == 0xffffffff)
			continue;
		handles->slot_hint = slot;
		return find_first_unset (handles->bitmap [slot]);
	}
	return -1;
}

/* Returns the index of the current slot in the bitmap. */
static void
handle_data_grow (HandleData *handles, gboolean track)
{
	guint32 *new_bitmap;
	guint32 new_size = handles->size * 2; /* always double: we memset to 0 based on this below */

	/* resize and copy the bitmap */
	new_bitmap = (guint32 *)g_malloc0 (new_size / CHAR_BIT);
	memcpy (new_bitmap, handles->bitmap, handles->size / CHAR_BIT);
	g_free (handles->bitmap);
	handles->bitmap = new_bitmap;

	/* resize and copy the entries */
	if (MONO_GC_HANDLE_TYPE_IS_WEAK (handles->type)) {
		gpointer *entries;
		guint16 *domain_ids;
		gint i;
		domain_ids = (guint16 *)g_malloc0 (sizeof (*handles->domain_ids) * new_size);
		entries = (void **)g_malloc0 (sizeof (*handles->entries) * new_size);
		memcpy (domain_ids, handles->domain_ids, sizeof (*handles->domain_ids) * handles->size);
		for (i = 0; i < handles->size; ++i) {
			MonoObject *obj = mono_gc_weak_link_get (&(handles->entries [i]));
			if (obj) {
				mono_gc_weak_link_add (&(entries [i]), obj, track);
				mono_gc_weak_link_remove (&(handles->entries [i]), track);
			} else {
				g_assert (!handles->entries [i]);
			}
		}
		g_free (handles->entries);
		g_free (handles->domain_ids);
		handles->entries = entries;
		handles->domain_ids = domain_ids;
	} else {
		gpointer *entries;
		entries = (void **)mono_gc_alloc_fixed (sizeof (*handles->entries) * new_size, NULL, MONO_ROOT_SOURCE_GC_HANDLE, "GC Handle Table (Null)");
		mono_gc_memmove_aligned (entries, handles->entries, sizeof (*handles->entries) * handles->size);
		mono_gc_free_fixed (handles->entries);
		handles->entries = entries;
	}
	handles->slot_hint = handles->size / BITMAP_SIZE;
	handles->size = new_size;
}

static guint32
alloc_handle (HandleData *handles, MonoObject *obj, gboolean track)
{
	gint slot, i;
	guint32 res;
	lock_handles (handles);
	if (!handles->size)
		handle_data_alloc_entries (handles);
	i = handle_data_next_unset (handles);
	if (i == -1 && handles->slot_hint != 0)
		i = handle_data_first_unset (handles);
	if (i == -1) {
		handle_data_grow (handles, track);
		i = 0;
	}
	slot = handles->slot_hint * BITMAP_SIZE + i;
	occupy_slot (handles, slot);
	handles->entries [slot] = NULL;
	if (MONO_GC_HANDLE_TYPE_IS_WEAK (handles->type)) {
		/*FIXME, what to use when obj == null?*/
		handles->domain_ids [slot] = (obj ? mono_object_get_domain (obj) : mono_domain_get ())->domain_id;
		if (obj)
			mono_gc_weak_link_add (&(handles->entries [slot]), obj, track);
	} else {
		handles->entries [slot] = obj;
	}

#ifndef DISABLE_PERFCOUNTERS
	mono_atomic_inc_i32 (&mono_perfcounters->gc_num_handles);
#endif
	unlock_handles (handles);
	res = MONO_GC_HANDLE (slot, handles->type);
	mono_profiler_gc_handle (MONO_PROFILER_GC_HANDLE_CREATED, handles->type, res, obj);
	return res;
}

/**
 * mono_gchandle_new:
 * \param obj managed object to get a handle for
 * \param pinned whether the object should be pinned
 *
 * This returns a handle that wraps the object, this is used to keep a
 * reference to a managed object from the unmanaged world and preventing the
 * object from being disposed.
 * 
 * If \p pinned is false the address of the object can not be obtained, if it is
 * true the address of the object can be obtained.  This will also pin the
 * object so it will not be possible by a moving garbage collector to move the
 * object. 
 * 
 * \returns a handle that can be used to access the object from
 * unmanaged code.
 */
guint32
mono_gchandle_new (MonoObject *obj, gboolean pinned)
{
	return alloc_handle (&gc_handles [pinned? HANDLE_PINNED: HANDLE_NORMAL], obj, FALSE);
}

/**
 * mono_gchandle_new_weakref:
 * \param obj managed object to get a handle for
 * \param track_resurrection Determines how long to track the object, if this is set to TRUE, the object is tracked after finalization, if FALSE, the object is only tracked up until the point of finalization.
 *
 * This returns a weak handle that wraps the object, this is used to
 * keep a reference to a managed object from the unmanaged world.
 * Unlike the \c mono_gchandle_new the object can be reclaimed by the
 * garbage collector.  In this case the value of the GCHandle will be
 * set to zero.
 * 
 * If \p track_resurrection is TRUE the object will be tracked through
 * finalization and if the object is resurrected during the execution
 * of the finalizer, then the returned weakref will continue to hold
 * a reference to the object.   If \p track_resurrection is FALSE, then
 * the weak reference's target will become NULL as soon as the object
 * is passed on to the finalizer.
 * 
 * \returns a handle that can be used to access the object from
 * unmanaged code.
 */
guint32
mono_gchandle_new_weakref (MonoObject *obj, gboolean track_resurrection)
{
	return alloc_handle (&gc_handles [track_resurrection? HANDLE_WEAK_TRACK: HANDLE_WEAK], obj, track_resurrection);
}

/**
 * mono_gchandle_get_target:
 * \param gchandle a GCHandle's handle.
 *
 * The handle was previously created by calling \c mono_gchandle_new or
 * \c mono_gchandle_new_weakref.
 *
 * \returns A pointer to the \c MonoObject* represented by the handle or
 * NULL for a collected object if using a weakref handle.
 */
MonoObject*
mono_gchandle_get_target (guint32 gchandle)
{
	guint slot = MONO_GC_HANDLE_SLOT (gchandle);
	guint type = MONO_GC_HANDLE_TYPE (gchandle);
	HandleData *handles = &gc_handles [type];
	MonoObject *obj = NULL;
	if (type >= HANDLE_TYPE_MAX)
		return NULL;

	lock_handles (handles);
	if (slot < handles->size && slot_occupied (handles, slot)) {
		if (MONO_GC_HANDLE_TYPE_IS_WEAK (handles->type)) {
			obj = mono_gc_weak_link_get (&handles->entries [slot]);
		} else {
			obj = (MonoObject *)handles->entries [slot];
		}
	} else {
		/* print a warning? */
	}
	unlock_handles (handles);
	/*g_print ("get target of entry %d of type %d: %p\n", slot, handles->type, obj);*/
	return obj;
}

void
mono_gchandle_set_target (guint32 gchandle, MonoObject *obj)
{
	guint slot = MONO_GC_HANDLE_SLOT (gchandle);
	guint type = MONO_GC_HANDLE_TYPE (gchandle);
	HandleData *handles = &gc_handles [type];
	MonoObject *old_obj = NULL;

	g_assert (type < HANDLE_TYPE_MAX);
	lock_handles (handles);
	if (slot < handles->size && slot_occupied (handles, slot)) {
		if (MONO_GC_HANDLE_TYPE_IS_WEAK (handles->type)) {
			old_obj = (MonoObject *)handles->entries [slot];
			if (handles->entries [slot])
				mono_gc_weak_link_remove (&handles->entries [slot], handles->type == HANDLE_WEAK_TRACK);
			if (obj)
				mono_gc_weak_link_add (&handles->entries [slot], obj, handles->type == HANDLE_WEAK_TRACK);
			/*FIXME, what to use when obj == null?*/
			handles->domain_ids [slot] = (obj ? mono_object_get_domain (obj) : mono_domain_get ())->domain_id;
		} else {
			handles->entries [slot] = obj;
		}
	} else {
		/* print a warning? */
	}
	/*g_print ("changed entry %d of type %d to object %p (in slot: %p)\n", slot, handles->type, obj, handles->entries [slot]);*/
	unlock_handles (handles);
}

/**
 * mono_gchandle_is_in_domain:
 * \param gchandle a GCHandle's handle.
 * \param domain An application domain.
 *
 * Use this function to determine if the \p gchandle points to an
 * object allocated in the specified \p domain.
 *
 * \returns TRUE if the object wrapped by the \p gchandle belongs to the specific \p domain.
 */
gboolean
mono_gchandle_is_in_domain (guint32 gchandle, MonoDomain *domain)
{
	guint slot = MONO_GC_HANDLE_SLOT (gchandle);
	guint type = MONO_GC_HANDLE_TYPE (gchandle);
	HandleData *handles = &gc_handles [type];
	gboolean result = FALSE;

	if (type >= HANDLE_TYPE_MAX)
		return FALSE;

	lock_handles (handles);
	if (slot < handles->size && slot_occupied (handles, slot)) {
		if (MONO_GC_HANDLE_TYPE_IS_WEAK (handles->type)) {
			result = domain->domain_id == handles->domain_ids [slot];
		} else {
			MonoObject *obj;
			obj = (MonoObject *)handles->entries [slot];
			if (obj == NULL)
				result = TRUE;
			else
				result = domain == mono_object_domain (obj);
		}
	} else {
		/* print a warning? */
	}
	unlock_handles (handles);
	return result;
}

/**
 * mono_gchandle_free:
 * \param gchandle a GCHandle's handle.
 *
 * Frees the \p gchandle handle.  If there are no outstanding
 * references, the garbage collector can reclaim the memory of the
 * object wrapped. 
 */
void
mono_gchandle_free (guint32 gchandle)
{
	guint slot = MONO_GC_HANDLE_SLOT (gchandle);
	guint type = MONO_GC_HANDLE_TYPE (gchandle);
	HandleData *handles = &gc_handles [type];
	if (type >= HANDLE_TYPE_MAX)
		return;

	lock_handles (handles);
	if (slot < handles->size && slot_occupied (handles, slot)) {
		if (MONO_GC_HANDLE_TYPE_IS_WEAK (handles->type)) {
			if (handles->entries [slot])
				mono_gc_weak_link_remove (&handles->entries [slot], handles->type == HANDLE_WEAK_TRACK);
		} else {
			handles->entries [slot] = NULL;
		}
		vacate_slot (handles, slot);
	} else {
		/* print a warning? */
	}
#ifndef DISABLE_PERFCOUNTERS
	mono_atomic_dec_i32 (&mono_perfcounters->gc_num_handles);
#endif
	/*g_print ("freed entry %d of type %d\n", slot, handles->type);*/
	unlock_handles (handles);
	mono_profiler_gc_handle (MONO_PROFILER_GC_HANDLE_DESTROYED, handles->type, gchandle, NULL);
}

/**
 * mono_gchandle_free_domain:
 * \param domain domain that is unloading
 *
 * Function used internally to cleanup any GC handle for objects belonging
 * to the specified domain during appdomain unload.
 */
void
mono_gchandle_free_domain (MonoDomain *domain)
{
	guint type;

	for (type = HANDLE_TYPE_MIN; type < HANDLE_PINNED; ++type) {
		guint slot;
		HandleData *handles = &gc_handles [type];
		lock_handles (handles);
		for (slot = 0; slot < handles->size; ++slot) {
			if (!slot_occupied (handles, slot))
				continue;
			if (MONO_GC_HANDLE_TYPE_IS_WEAK (type)) {
				if (domain->domain_id == handles->domain_ids [slot]) {
					vacate_slot (handles, slot);
					if (handles->entries [slot])
						mono_gc_weak_link_remove (&handles->entries [slot], handles->type == HANDLE_WEAK_TRACK);
				}
			} else {
				if (handles->entries [slot] && mono_object_domain (handles->entries [slot]) == domain) {
					vacate_slot (handles, slot);
					handles->entries [slot] = NULL;
				}
			}
		}
		unlock_handles (handles);
	}

}
#else

MONO_EMPTY_SOURCE_FILE (null_gc_handles);
#endif /* HAVE_NULL_GC */
