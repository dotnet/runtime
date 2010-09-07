/* GLIB - Library of useful routines for C programming
 * Copyright (C) 1995-1997  Peter Mattis, Spencer Kimball and Josh MacDonald
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2 of the License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.	 See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the
 * Free Software Foundation, Inc., 59 Temple Place - Suite 330,
 * Boston, MA 02111-1307, USA.
 */

/*
 * Modified by the GLib Team and others 1997-2000.  See the AUTHORS
 * file for a list of people on the GLib Team.  See the ChangeLog
 * files for a list of changes.  These files are distributed with
 * GLib at ftp://ftp.gtk.org/pub/gtk/. 
 */

#ifndef __MONO_G_HASH_H__
#define __MONO_G_HASH_H__

/*
 * Imported in mono cvs from version 1.7 of gnome cvs.
 * This hash table is GC friendly and the pointers stored in it
 * are tracked by the garbage collector.
 */

#include <glib.h>

G_BEGIN_DECLS

typedef struct _MonoGHashTable  MonoGHashTable;

typedef gpointer (*MonoGRemapperFunc) (gpointer key, gpointer value, 
									   gpointer user_data);

/* do not change the values of this enum */
typedef enum {
	MONO_HASH_CONSERVATIVE_GC,
	MONO_HASH_KEY_GC,
	MONO_HASH_VALUE_GC,
	MONO_HASH_KEY_VALUE_GC /* note this is the OR of the other two values */
} MonoGHashGCType;

/* Hash tables
 */
MonoGHashTable* mono_g_hash_table_new		   (GHashFunc	    hash_func,
					    GEqualFunc	    key_equal_func);
MonoGHashTable* mono_g_hash_table_new_type		   (GHashFunc	    hash_func,
					    GEqualFunc	    key_equal_func,
					    MonoGHashGCType type);
MonoGHashTable* mono_g_hash_table_new_full      	   (GHashFunc	    hash_func,
					    GEqualFunc	    key_equal_func,
					    GDestroyNotify  key_destroy_func,
					    GDestroyNotify  value_destroy_func);
void	    mono_g_hash_table_destroy	   (MonoGHashTable	   *hash_table);
void	    mono_g_hash_table_insert		   (MonoGHashTable	   *hash_table,
					    gpointer	    key,
					    gpointer	    value);
void        mono_g_hash_table_replace           (MonoGHashTable     *hash_table,
					    gpointer	    key,
					    gpointer	    value);
gboolean    mono_g_hash_table_remove		   (MonoGHashTable	   *hash_table,
					    gconstpointer   key);
gboolean    mono_g_hash_table_steal             (MonoGHashTable     *hash_table,
					    gconstpointer   key);
gpointer    mono_g_hash_table_lookup		   (MonoGHashTable	   *hash_table,
					    gconstpointer   key);
gboolean    mono_g_hash_table_lookup_extended   (MonoGHashTable	   *hash_table,
					    gconstpointer   lookup_key,
					    gpointer	   *orig_key,
					    gpointer	   *value);
void	    mono_g_hash_table_foreach	   (MonoGHashTable	   *hash_table,
					    GHFunc	    func,
					    gpointer	    user_data);
guint	    mono_g_hash_table_foreach_remove	   (MonoGHashTable	   *hash_table,
					    GHRFunc	    func,
					    gpointer	    user_data);
guint	    mono_g_hash_table_foreach_steal	   (MonoGHashTable	   *hash_table,
					    GHRFunc	    func,
					    gpointer	    user_data);
gpointer    mono_g_hash_table_find (MonoGHashTable *hash_table,
									GHRFunc predicate,
									gpointer user_data);
guint	    mono_g_hash_table_size		   (MonoGHashTable	   *hash_table);

void        mono_g_hash_table_remap (MonoGHashTable *hash_table,
									 MonoGRemapperFunc func,
									 gpointer user_data);

void        mono_g_hash_table_print_stats (MonoGHashTable *table);

G_END_DECLS

#endif /* __MONO_G_HASH_H__ */

