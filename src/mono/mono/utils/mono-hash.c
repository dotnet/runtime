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

/* 
 * MT safe
 */

/*
 * Imported in mono cvs from version 1.32 of gnome cvs.
 */

#ifdef HAVE_CONFIG_H
#include <config.h>
#endif

#include <glib.h>
#include <mono/os/gc_wrapper.h>
#include "mono-hash.h"

#define HASH_TABLE_MIN_SIZE 11
#define HASH_TABLE_MAX_SIZE 13845163


typedef struct _MonoGHashNode      MonoGHashNode;

struct _MonoGHashNode
{
  gpointer   key;
  gpointer   value;
  MonoGHashNode *next;
};

struct _MonoGHashTable
{
  gint             size;
  gint             nnodes;
  MonoGHashNode      **nodes;
  GHashFunc        hash_func;
  GEqualFunc       key_equal_func;
  GDestroyNotify   key_destroy_func;
  GDestroyNotify   value_destroy_func;
};

#define G_HASH_TABLE_RESIZE(hash_table)				\
   G_STMT_START {						\
     if ((hash_table->size >= 3 * hash_table->nnodes &&	        \
	  hash_table->size > HASH_TABLE_MIN_SIZE) ||		\
	 (3 * hash_table->size <= hash_table->nnodes &&	        \
	  hash_table->size < HASH_TABLE_MAX_SIZE))		\
	   g_hash_table_resize (hash_table);			\
   } G_STMT_END

static void		g_hash_table_resize	  (MonoGHashTable	  *hash_table);
static MonoGHashNode**	g_hash_table_lookup_node  (MonoGHashTable     *hash_table,
                                                   gconstpointer   key);
static MonoGHashNode*	g_hash_node_new		  (gpointer	   key,
                                                   gpointer        value);
static void		g_hash_node_destroy	  (MonoGHashNode	  *hash_node,
                                                   GDestroyNotify  key_destroy_func,
                                                   GDestroyNotify  value_destroy_func);
static void		g_hash_nodes_destroy	  (MonoGHashNode	  *hash_node,
						  GDestroyNotify   key_destroy_func,
						  GDestroyNotify   value_destroy_func);
static guint g_hash_table_foreach_remove_or_steal (MonoGHashTable     *hash_table,
                                                   GHRFunc	   func,
                                                   gpointer	   user_data,
                                                   gboolean        notify);


G_LOCK_DEFINE_STATIC (g_hash_global);

#if !HAVE_BOEHM_GC
static GMemChunk *node_mem_chunk = NULL;
#endif
static MonoGHashNode *node_free_list = NULL;

/**
 * g_hash_table_new:
 * @hash_func: a function to create a hash value from a key.
 *   Hash values are used to determine where keys are stored within the
 *   #GHashTable data structure. The g_direct_hash(), g_int_hash() and 
 *   g_str_hash() functions are provided for some common types of keys. 
 *   If hash_func is %NULL, g_direct_hash() is used.
 * @key_equal_func: a function to check two keys for equality.  This is
 *   used when looking up keys in the #GHashTable.  The g_direct_equal(),
 *   g_int_equal() and g_str_equal() functions are provided for the most
 *   common types of keys. If @key_equal_func is %NULL, keys are compared
 *   directly in a similar fashion to g_direct_equal(), but without the
 *   overhead of a function call.
 *
 * Creates a new #GHashTable.
 * 
 * Return value: a new #GHashTable.
 **/
MonoGHashTable*
mono_g_hash_table_new (GHashFunc    hash_func,
		  GEqualFunc   key_equal_func)
{
  return mono_g_hash_table_new_full (hash_func, key_equal_func, NULL, NULL);
}


/**
 * g_hash_table_new_full:
 * @hash_func: a function to create a hash value from a key.
 * @key_equal_func: a function to check two keys for equality.
 * @key_destroy_func: a function to free the memory allocated for the key 
 *   used when removing the entry from the #GHashTable or %NULL if you 
 *   don't want to supply such a function.
 * @value_destroy_func: a function to free the memory allocated for the 
 *   value used when removing the entry from the #GHashTable or %NULL if 
 *   you don't want to supply such a function.
 * 
 * Creates a new #GHashTable like g_hash_table_new() and allows to specify
 * functions to free the memory allocated for the key and value that get 
 * called when removing the entry from the #GHashTable.
 * 
 * Return value: a new #GHashTable.
 **/
MonoGHashTable*
mono_g_hash_table_new_full (GHashFunc       hash_func,
		       GEqualFunc      key_equal_func,
		       GDestroyNotify  key_destroy_func,
		       GDestroyNotify  value_destroy_func)
{
  MonoGHashTable *hash_table;
  
#if HAVE_BOEHM_GC
  hash_table = GC_malloc (sizeof (MonoGHashTable));
#else
  hash_table = g_new (MonoGHashTable, 1);
#endif
  hash_table->size               = HASH_TABLE_MIN_SIZE;
  hash_table->nnodes             = 0;
  hash_table->hash_func          = hash_func ? hash_func : g_direct_hash;
  hash_table->key_equal_func     = key_equal_func;
  hash_table->key_destroy_func   = key_destroy_func;
  hash_table->value_destroy_func = value_destroy_func;
#if HAVE_BOEHM_GC
  hash_table->nodes              = GC_malloc (sizeof (MonoGHashNode*) * hash_table->size);
#else
  hash_table->nodes              = g_new0 (MonoGHashNode*, hash_table->size);
#endif
  
  return hash_table;
}

/**
 * g_hash_table_destroy:
 * @hash_table: a #GHashTable.
 * 
 * Destroys the #GHashTable. If keys and/or values are dynamically 
 * allocated, you should either free them first or create the #GHashTable
 * using g_hash_table_new_full(). In the latter case the destroy functions 
 * you supplied will be called on all keys and values before destroying 
 * the #GHashTable.
 **/
void
mono_g_hash_table_destroy (MonoGHashTable *hash_table)
{
  guint i;
  
  g_return_if_fail (hash_table != NULL);
  
  for (i = 0; i < hash_table->size; i++)
    g_hash_nodes_destroy (hash_table->nodes[i], 
			  hash_table->key_destroy_func,
			  hash_table->value_destroy_func);

#if HAVE_BOEHM_GC
#else
  g_free (hash_table->nodes);
  g_free (hash_table);
#endif
}

static inline MonoGHashNode**
g_hash_table_lookup_node (MonoGHashTable	*hash_table,
			  gconstpointer	 key)
{
  MonoGHashNode **node;
  
  node = &hash_table->nodes
    [(* hash_table->hash_func) (key) % hash_table->size];
  
  /* Hash table lookup needs to be fast.
   *  We therefore remove the extra conditional of testing
   *  whether to call the key_equal_func or not from
   *  the inner loop.
   */
  if (hash_table->key_equal_func)
    while (*node && !(*hash_table->key_equal_func) ((*node)->key, key))
      node = &(*node)->next;
  else
    while (*node && (*node)->key != key)
      node = &(*node)->next;
  
  return node;
}

/**
 * g_hash_table_lookup:
 * @hash_table: a #GHashTable.
 * @key: the key to look up.
 * 
 * Looks up a key in a #GHashTable.
 * 
 * Return value: the associated value, or %NULL if the key is not found.
 **/
gpointer
mono_g_hash_table_lookup (MonoGHashTable	  *hash_table,
		     gconstpointer key)
{
  MonoGHashNode *node;
  
  g_return_val_if_fail (hash_table != NULL, NULL);
  
  node = *g_hash_table_lookup_node (hash_table, key);
  
  return node ? node->value : NULL;
}

/**
 * g_hash_table_lookup_extended:
 * @hash_table: a #GHashTable.
 * @lookup_key: the key to look up.
 * @orig_key: returns the original key.
 * @value: returns the value associated with the key.
 * 
 * Looks up a key in the #GHashTable, returning the original key and the
 * associated value and a #gboolean which is %TRUE if the key was found. This 
 * is useful if you need to free the memory allocated for the original key, 
 * for example before calling g_hash_table_remove().
 * 
 * Return value: %TRUE if the key was found in the #GHashTable.
 **/
gboolean
mono_g_hash_table_lookup_extended (MonoGHashTable    *hash_table,
			      gconstpointer  lookup_key,
			      gpointer	    *orig_key,
			      gpointer	    *value)
{
  MonoGHashNode *node;
  
  g_return_val_if_fail (hash_table != NULL, FALSE);
  
  node = *g_hash_table_lookup_node (hash_table, lookup_key);
  
  if (node)
    {
      if (orig_key)
	*orig_key = node->key;
      if (value)
	*value = node->value;
      return TRUE;
    }
  else
    return FALSE;
}

/**
 * g_hash_table_insert:
 * @hash_table: a #GHashTable.
 * @key: a key to insert.
 * @value: the value to associate with the key.
 * 
 * Inserts a new key and value into a #GHashTable.
 * 
 * If the key already exists in the #GHashTable its current value is replaced
 * with the new value. If you supplied a @value_destroy_func when creating the 
 * #GHashTable, the old value is freed using that function. If you supplied
 * a @key_destroy_func when creating the #GHashTable, the passed key is freed 
 * using that function.
 **/
void
mono_g_hash_table_insert (MonoGHashTable *hash_table,
		     gpointer	 key,
		     gpointer	 value)
{
  MonoGHashNode **node;
  
  g_return_if_fail (hash_table != NULL);
  
  node = g_hash_table_lookup_node (hash_table, key);
  
  if (*node)
    {
      /* do not reset node->key in this place, keeping
       * the old key is the intended behaviour. 
       * g_hash_table_replace() can be used instead.
       */

      /* free the passed key */
      if (hash_table->key_destroy_func)
	hash_table->key_destroy_func (key);
      
      if (hash_table->value_destroy_func)
	hash_table->value_destroy_func ((*node)->value);

      (*node)->value = value;
    }
  else
    {
      *node = g_hash_node_new (key, value);
      hash_table->nnodes++;
      G_HASH_TABLE_RESIZE (hash_table);
    }
}

/**
 * g_hash_table_replace:
 * @hash_table: a #GHashTable.
 * @key: a key to insert.
 * @value: the value to associate with the key.
 * 
 * Inserts a new key and value into a #GHashTable similar to 
 * g_hash_table_insert(). The difference is that if the key already exists 
 * in the #GHashTable, it gets replaced by the new key. If you supplied a 
 * @value_destroy_func when creating the #GHashTable, the old value is freed 
 * using that function. If you supplied a @key_destroy_func when creating the 
 * #GHashTable, the old key is freed using that function. 
 **/
void
mono_g_hash_table_replace (MonoGHashTable *hash_table,
		      gpointer	  key,
		      gpointer	  value)
{
  MonoGHashNode **node;
  
  g_return_if_fail (hash_table != NULL);
  
  node = g_hash_table_lookup_node (hash_table, key);
  
  if (*node)
    {
      if (hash_table->key_destroy_func)
	hash_table->key_destroy_func ((*node)->key);
      
      if (hash_table->value_destroy_func)
	hash_table->value_destroy_func ((*node)->value);

      (*node)->key   = key;
      (*node)->value = value;
    }
  else
    {
      *node = g_hash_node_new (key, value);
      hash_table->nnodes++;
      G_HASH_TABLE_RESIZE (hash_table);
    }
}

/**
 * g_hash_table_remove:
 * @hash_table: a #GHashTable.
 * @key: the key to remove.
 * 
 * Removes a key and its associated value from a #GHashTable.
 *
 * If the #GHashTable was created using g_hash_table_new_full(), the
 * key and value are freed using the supplied destroy functions, otherwise
 * you have to make sure that any dynamically allocated values are freed 
 * yourself.
 * 
 * Return value: %TRUE if the key was found and removed from the #GHashTable.
 **/
gboolean
mono_g_hash_table_remove (MonoGHashTable	   *hash_table,
		     gconstpointer  key)
{
  MonoGHashNode **node, *dest;
  
  g_return_val_if_fail (hash_table != NULL, FALSE);
  
  node = g_hash_table_lookup_node (hash_table, key);
  if (*node)
    {
      dest = *node;
      (*node) = dest->next;
      g_hash_node_destroy (dest, 
			   hash_table->key_destroy_func,
			   hash_table->value_destroy_func);
      hash_table->nnodes--;
  
      G_HASH_TABLE_RESIZE (hash_table);

      return TRUE;
    }

  return FALSE;
}

/**
 * g_hash_table_steal:
 * @hash_table: a #GHashTable.
 * @key: the key to remove.
 * 
 * Removes a key and its associated value from a #GHashTable without
 * calling the key and value destroy functions.
 *
 * Return value: %TRUE if the key was found and removed from the #GHashTable.
 **/
gboolean
mono_g_hash_table_steal (MonoGHashTable    *hash_table,
                    gconstpointer  key)
{
  MonoGHashNode **node, *dest;
  
  g_return_val_if_fail (hash_table != NULL, FALSE);
  
  node = g_hash_table_lookup_node (hash_table, key);
  if (*node)
    {
      dest = *node;
      (*node) = dest->next;
      g_hash_node_destroy (dest, NULL, NULL);
      hash_table->nnodes--;
  
      G_HASH_TABLE_RESIZE (hash_table);

      return TRUE;
    }

  return FALSE;
}

/**
 * g_hash_table_foreach_remove:
 * @hash_table: a #GHashTable.
 * @func: the function to call for each key/value pair.
 * @user_data: user data to pass to the function.
 * 
 * Calls the given function for each key/value pair in the #GHashTable.
 * If the function returns %TRUE, then the key/value pair is removed from the
 * #GHashTable. If you supplied key or value destroy functions when creating
 * the #GHashTable, they are used to free the memory allocated for the removed
 * keys and values.
 * 
 * Return value: the number of key/value pairs removed.
 **/
guint
mono_g_hash_table_foreach_remove (MonoGHashTable	*hash_table,
			     GHRFunc	 func,
			     gpointer	 user_data)
{
  g_return_val_if_fail (hash_table != NULL, 0);
  g_return_val_if_fail (func != NULL, 0);
  
  return g_hash_table_foreach_remove_or_steal (hash_table, func, user_data, TRUE);
}

/**
 * g_hash_table_foreach_steal:
 * @hash_table: a #GHashTable.
 * @func: the function to call for each key/value pair.
 * @user_data: user data to pass to the function.
 * 
 * Calls the given function for each key/value pair in the #GHashTable.
 * If the function returns %TRUE, then the key/value pair is removed from the
 * #GHashTable, but no key or value destroy functions are called.
 * 
 * Return value: the number of key/value pairs removed.
 **/
guint
mono_g_hash_table_foreach_steal (MonoGHashTable *hash_table,
                            GHRFunc	func,
                            gpointer	user_data)
{
  g_return_val_if_fail (hash_table != NULL, 0);
  g_return_val_if_fail (func != NULL, 0);
  
  return g_hash_table_foreach_remove_or_steal (hash_table, func, user_data, FALSE);
}

static guint
g_hash_table_foreach_remove_or_steal (MonoGHashTable *hash_table,
                                      GHRFunc	  func,
                                      gpointer	  user_data,
                                      gboolean    notify)
{
  MonoGHashNode *node, *prev;
  guint i;
  guint deleted = 0;
  
  for (i = 0; i < hash_table->size; i++)
    {
    restart:
      
      prev = NULL;
      
      for (node = hash_table->nodes[i]; node; prev = node, node = node->next)
	{
	  if ((* func) (node->key, node->value, user_data))
	    {
	      deleted += 1;
	      
	      hash_table->nnodes -= 1;
	      
	      if (prev)
		{
		  prev->next = node->next;
		  g_hash_node_destroy (node,
				       notify ? hash_table->key_destroy_func : NULL,
				       notify ? hash_table->value_destroy_func : NULL);
		  node = prev;
		}
	      else
		{
		  hash_table->nodes[i] = node->next;
		  g_hash_node_destroy (node,
				       notify ? hash_table->key_destroy_func : NULL,
				       notify ? hash_table->value_destroy_func : NULL);
		  goto restart;
		}
	    }
	}
    }
  
  G_HASH_TABLE_RESIZE (hash_table);
  
  return deleted;
}

/**
 * g_hash_table_foreach:
 * @hash_table: a #GHashTable.
 * @func: the function to call for each key/value pair.
 * @user_data: user data to pass to the function.
 * 
 * Calls the given function for each of the key/value pairs in the
 * #GHashTable.  The function is passed the key and value of each
 * pair, and the given @user_data parameter.  The hash table may not
 * be modified while iterating over it (you can't add/remove
 * items). To remove all items matching a predicate, use
 * g_hash_table_remove().
 **/
void
mono_g_hash_table_foreach (MonoGHashTable *hash_table,
		      GHFunc	  func,
		      gpointer	  user_data)
{
  MonoGHashNode *node;
  gint i;
  
  g_return_if_fail (hash_table != NULL);
  g_return_if_fail (func != NULL);
  
  for (i = 0; i < hash_table->size; i++)
    for (node = hash_table->nodes[i]; node; node = node->next)
      (* func) (node->key, node->value, user_data);
}

/**
 * g_hash_table_size:
 * @hash_table: a #GHashTable.
 * 
 * Returns the number of elements contained in the #GHashTable.
 * 
 * Return value: the number of key/value pairs in the #GHashTable.
 **/
guint
mono_g_hash_table_size (MonoGHashTable *hash_table)
{
  g_return_val_if_fail (hash_table != NULL, 0);
  
  return hash_table->nnodes;
}

static void
g_hash_table_resize (MonoGHashTable *hash_table)
{
  MonoGHashNode **new_nodes;
  MonoGHashNode *node;
  MonoGHashNode *next;
  guint hash_val;
  gint new_size;
  gint i;

  new_size = g_spaced_primes_closest (hash_table->nnodes);
  new_size = CLAMP (new_size, HASH_TABLE_MIN_SIZE, HASH_TABLE_MAX_SIZE);
 
#if HAVE_BOEHM_GC
  new_nodes              = GC_malloc (sizeof (MonoGHashNode*) * new_size);
#else
  new_nodes              = g_new0 (MonoGHashNode*, new_size);
#endif
  
  for (i = 0; i < hash_table->size; i++)
    for (node = hash_table->nodes[i]; node; node = next)
      {
	next = node->next;

	hash_val = (* hash_table->hash_func) (node->key) % new_size;

	node->next = new_nodes[hash_val];
	new_nodes[hash_val] = node;
      }
  
#if HAVE_BOEHM_GC
#else
  g_free (hash_table->nodes);
#endif
  hash_table->nodes = new_nodes;
  hash_table->size = new_size;
}

static MonoGHashNode*
g_hash_node_new (gpointer key,
		 gpointer value)
{
  MonoGHashNode *hash_node;
  
  G_LOCK (g_hash_global);
  if (node_free_list)
    {
      hash_node = node_free_list;
      node_free_list = node_free_list->next;
    }
  else
    {
#if HAVE_BOEHM_GC
      hash_node = GC_malloc (sizeof (MonoGHashNode));
#else
      if (!node_mem_chunk)
	node_mem_chunk = g_mem_chunk_new ("hash node mem chunk",
					  sizeof (MonoGHashNode),
					  1024, G_ALLOC_ONLY);
      
      hash_node = g_chunk_new (MonoGHashNode, node_mem_chunk);
#endif
    }
  G_UNLOCK (g_hash_global);
  
  hash_node->key = key;
  hash_node->value = value;
  hash_node->next = NULL;
  
  return hash_node;
}

static void
g_hash_node_destroy (MonoGHashNode      *hash_node,
		     GDestroyNotify  key_destroy_func,
		     GDestroyNotify  value_destroy_func)
{
  if (key_destroy_func)
    key_destroy_func (hash_node->key);
  if (value_destroy_func)
    value_destroy_func (hash_node->value);
  
  hash_node->key = NULL;
  hash_node->value = NULL;

  G_LOCK (g_hash_global);
  hash_node->next = node_free_list;
  node_free_list = hash_node;
  G_UNLOCK (g_hash_global);
}

static void
g_hash_nodes_destroy (MonoGHashNode *hash_node,
		      GFreeFunc  key_destroy_func,
		      GFreeFunc  value_destroy_func)
{
  if (hash_node)
    {
      MonoGHashNode *node = hash_node;
  
      while (node->next)
	{
	  if (key_destroy_func)
	    key_destroy_func (node->key);
	  if (value_destroy_func)
	    value_destroy_func (node->value);

	  node->key = NULL;
	  node->value = NULL;

	  node = node->next;
	}

  if (key_destroy_func)
    key_destroy_func (node->key);
  if (value_destroy_func)
    value_destroy_func (node->value);

      node->key = NULL;
      node->value = NULL;
 
      G_LOCK (g_hash_global);
      node->next = node_free_list;
      node_free_list = hash_node;
      G_UNLOCK (g_hash_global);
    }
}
