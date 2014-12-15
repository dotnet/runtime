/*
 * sgen-client.h: SGen client interface.
 *
 * Copyright (C) 2014 Xamarin Inc
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Library General Public
 * License 2.0 as published by the Free Software Foundation;
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Library General Public License for more details.
 *
 * You should have received a copy of the GNU Library General Public
 * License 2.0 along with this library; if not, write to the Free
 * Software Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.
 */

void sgen_client_init (void);

/*
 * Returns the vtable used for dummy objects to fill the nursery for ease and speed of
 * walking.  Must be a valid vtable that is not used by any actual objects.  Must be
 * idempotent.
 */
MonoVTable* sgen_client_get_array_fill_vtable (void);

/*
 * Fill the given range with a dummy object.  Its vtable must be the one returned by
 * `sgen_client_get_array_fill_vtable()`.  If the range is too short to be filled with an
 * object, null it.  Return `TRUE` if the range was filled with an object, `FALSE` if it was
 * nulled.
 */
gboolean sgen_client_array_fill_range (char *start, size_t size);

/*
 * Called after an object is enqueued for finalization.  This is a very low-level callback.
 * It should almost certainly be a NOP.
 */
void sgen_client_object_queued_for_finalization (MonoObject *obj);

gboolean sgen_client_mark_ephemerons (ScanCopyContext ctx);

/*
 * Clear ephemeron pairs with unreachable keys.
 * We pass the copy func so we can figure out if an array was promoted or not.
 */
void sgen_client_clear_unreachable_ephemerons (ScanCopyContext ctx);

/*
 * If the client has registered any internal memory types, this must return a string
 * describing the given type.  Only used for debugging.
 */
const char* sgen_client_description_for_internal_mem_type (int type);

void sgen_client_pre_collection_checks (void);

gboolean sgen_client_handle_gc_debug (const char *opt);
void sgen_client_print_gc_debug_usage (void);
