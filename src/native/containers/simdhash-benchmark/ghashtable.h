// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef GHASHTABLE_H
#define GHASHTABLE_H

#include <stdlib.h>
#include <stdint.h>
#include <string.h>
#include <math.h>

/*
 * Basic data types
 */
typedef int            gint;
typedef unsigned int   guint;
typedef short          gshort;
typedef unsigned short gushort;
typedef long           glong;
typedef unsigned long  gulong;
typedef void *         gpointer;
typedef const void *   gconstpointer;
typedef char           gchar;
typedef unsigned char  guchar;

/* Types defined in terms of the stdint.h */
typedef int8_t         gint8;
typedef uint8_t        guint8;
typedef int16_t        gint16;
typedef uint16_t       guint16;
typedef int32_t        gint32;
typedef uint32_t       guint32;
typedef int64_t        gint64;
typedef uint64_t       guint64;
typedef float          gfloat;
typedef double         gdouble;
typedef int32_t        gboolean;
// typedef ptrdiff_t      gptrdiff;
typedef intptr_t       gintptr;
typedef uintptr_t      guintptr;

#define G_N_ELEMENTS(arr)      (sizeof(arr)/sizeof(arr[0]))

#define FALSE                0
#define TRUE                 1

#define G_MINSHORT           SHRT_MIN
#define G_MAXSHORT           SHRT_MAX
#define G_MAXUSHORT          USHRT_MAX
#define G_MAXINT             INT_MAX
#define G_MININT             INT_MIN
#define G_MAXINT8            INT8_MAX
#define G_MAXUINT8           UINT8_MAX
#define G_MININT8            INT8_MIN
#define G_MAXINT16           INT16_MAX
#define G_MAXUINT16          UINT16_MAX
#define G_MININT16           INT16_MIN
#define G_MAXINT32           INT32_MAX
#define G_MAXUINT32          UINT32_MAX
#define G_MININT32           INT32_MIN
#define G_MININT64           INT64_MIN
#define G_MAXINT64           INT64_MAX
#define G_MAXUINT64          UINT64_MAX

#define G_LITTLE_ENDIAN 1234
#define G_BIG_ENDIAN    4321
#define G_STMT_START    do
#define G_STMT_END      while (0)

typedef void     (*GFunc)          (gpointer data, gpointer user_data);
typedef gint     (*GCompareFunc)   (gconstpointer a, gconstpointer b);
typedef gint     (*GCompareDataFunc) (gconstpointer a, gconstpointer b, gpointer user_data);
typedef void     (*GHFunc)         (gpointer key, gpointer value, gpointer user_data);
typedef gboolean (*GHRFunc)        (gpointer key, gpointer value, gpointer user_data);
typedef void     (*GDestroyNotify) (gpointer data);
typedef guint    (*GHashFunc)      (gconstpointer key);
typedef gboolean (*GEqualFunc)     (gconstpointer a, gconstpointer b);
typedef void     (*GFreeFunc)      (gpointer       data);

/*
 * Hashtables
 */
typedef struct _GHashTable GHashTable;
typedef struct _GHashTableIter GHashTableIter;

/* Private, but needed for stack allocation */
struct _GHashTableIter
{
	gpointer dummy [8];
};

GHashTable     *g_hash_table_new             (GHashFunc hash_func, GEqualFunc key_equal_func);
GHashTable     *g_hash_table_new_full        (GHashFunc hash_func, GEqualFunc key_equal_func,
					      GDestroyNotify key_destroy_func, GDestroyNotify value_destroy_func);
void            g_hash_table_insert_replace  (GHashTable *hash, gpointer key, gpointer value, gboolean replace);
guint           g_hash_table_size            (GHashTable *hash);
gboolean        g_hash_table_contains (GHashTable *hash, gconstpointer key);
gpointer        g_hash_table_lookup          (GHashTable *hash, gconstpointer key);
gboolean        g_hash_table_lookup_extended (GHashTable *hash, gconstpointer key, gpointer *orig_key, gpointer *value);
void            g_hash_table_foreach         (GHashTable *hash, GHFunc func, gpointer user_data);
gpointer        g_hash_table_find            (GHashTable *hash, GHRFunc predicate, gpointer user_data);
gboolean        g_hash_table_remove          (GHashTable *hash, gconstpointer key);
gboolean        g_hash_table_steal           (GHashTable *hash, gconstpointer key);
void            g_hash_table_remove_all      (GHashTable *hash);
guint           g_hash_table_foreach_remove  (GHashTable *hash, GHRFunc func, gpointer user_data);
guint           g_hash_table_foreach_steal   (GHashTable *hash, GHRFunc func, gpointer user_data);
void            g_hash_table_destroy         (GHashTable *hash);
void            g_hash_table_print_stats     (GHashTable *table);

void            g_hash_table_iter_init       (GHashTableIter *iter, GHashTable *hash_table);
gboolean        g_hash_table_iter_next       (GHashTableIter *iter, gpointer *key, gpointer *value);

guint           g_spaced_primes_closest      (guint x);

#define g_hash_table_insert(h,k,v)    g_hash_table_insert_replace ((h),(k),(v),FALSE)
#define g_hash_table_replace(h,k,v)   g_hash_table_insert_replace ((h),(k),(v),TRUE)
#define g_hash_table_add(h,k)         g_hash_table_insert_replace ((h),(k),(k),TRUE)

gboolean g_direct_equal (gconstpointer v1, gconstpointer v2);
guint    g_direct_hash  (gconstpointer v1);
gboolean g_int_equal    (gconstpointer v1, gconstpointer v2);
guint    g_int_hash     (gconstpointer v1);
gboolean g_str_equal    (gconstpointer v1, gconstpointer v2);
guint    g_str_hash     (gconstpointer v1);

#define GCONSTPOINTER_TO_INT(v)  (gint)((ssize_t)v)
#define GCONSTPOINTER_TO_UINT(v) (gint)((size_t)v)

// FIXME
#define g_assert(expr) (void)(expr)

#define g_malloc malloc
#define g_free free
#define g_realloc realloc

#define g_new(type,size)        ((type *) g_malloc (sizeof (type) * (size)))
#define g_new0(type,size)       ((type *) g_malloc0 (sizeof (type)* (size)))
#define g_newa(type,size)       ((type *) alloca (sizeof (type) * (size)))
#define g_newa0(type,size)      ((type *) memset (alloca (sizeof (type) * (size)), 0, sizeof (type) * (size)))

#define g_memmove(dest,src,len) memmove (dest, src, len)
#define g_renew(struct_type, mem, n_structs) ((struct_type*)g_realloc (mem, sizeof (struct_type) * n_structs))
#define g_alloca(size)		(g_cast (alloca (size)))

#define g_return_if_fail(x)  if (!(x)) { return; }
#define g_return_val_if_fail(x,e)  if (!(x)) { return (e); }

static inline void *
g_malloc0 (size_t size) {
    void * result = malloc(size);
    memset(result, 0, size);
    return result;
}

#define ABS(x) abs(x)

#endif // GHASHTABLE_H
