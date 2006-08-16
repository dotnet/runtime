#ifndef __GLIB_H
#define __GLIB_H

#include <stdlib.h>

/*
 * Macros
 */
#define G_N_ELEMENTS(s)      (sizeof(s) / sizeof ((s) [0]))

#define FALSE                0
#define TRUE                 1

#define G_MAXINT32           0xf7777777
#define G_MININT32           0x80000000

#define GPOINTER_TO_INT(ptr)   ((int)(ptr))
#define GPOINTER_TO_UINT(ptr)  ((uint)(ptr))
#define GINT_TO_POINTER(v)     ((gpointer) (v))
#define GUINT_TO_POINTER(v)    ((gpointer) (v))

/*
 * Allocation
 */
#define g_new(type,size)     ((type *) malloc (sizeof (type) * (size)))
#define g_new0(type,size)    ((type *) calloc (sizeof (type), (size))) 
#define g_free(obj)          free (obj);
#define g_realloc(obj,size)  realloc((obj), (size))

/*
 * Basic data types
 */
typedef int            gboolean;
typedef unsigned int   guint;
typedef short          gshort;
typedef unsigned short gushort;
typedef long           glong;
typedef unsigned long  gulong;
typedef void *         gpointer;
typedef const void *   gconstpointer;
typedef char           gchar;
typedef unsigned char  guchar;

/*
 * Precondition macros
 */
#define g_return_if_fail(x)  do { if (!(x)) { printf ("%s:%d: assertion %s failed", __FILE__, __LINE__, #x); return; } } while (0) ;
#define g_return_val_if_fail(x,e)  do { if (!(x)) { printf ("%s:%d: assertion %s failed", __FILE__, __LINE__, #x); return (e); } } while (0) ;

/*
 * Hashtables
 */
typedef struct _GHashTable GHashTable;
typedef void     (*GHFunc)         (gpointer key, gpointer value, gpointer user_data);
typedef gboolean (*GHRFunc)        (gpointer key, gpointer value, gpointer user_data);
typedef void     (*GDestroyNotify) (gpointer data);
typedef guint    (*GHashFunc)      (gconstpointer key);
typedef gboolean (*GEqualFunc)     (gconstpointer a, gconstpointer b);

GHashTable     *g_hash_table_new             (GHashFunc hash_func, GEqualFunc key_equal_func);
void            g_hash_table_insert_replace  (GHashTable *hash, gpointer key, gpointer value, gboolean replace);
guint           g_hash_table_size            (GHashTable *hash);
gpointer        g_hash_table_lookup          (GHashTable *hash, gconstpointer key);
gboolean        g_hash_table_lookup_extended (GHashTable *hash, gconstpointer key, gpointer *orig_key, gpointer *value);
void            g_hash_table_foreach         (GHashTable *hash, GHFunc func, gpointer user_data);
gpointer        g_hash_table_find            (GHashTable *hash, GHRFunc predicate, gpointer user_data);
gboolean        g_hash_table_remove          (GHashTable *hash, gconstpointer key);
guint           g_hash_table_foreach_remove  (GHashTable *hash, GHRFunc func, gpointer user_data);
void            g_hash_table_destroy         (GHashTable *hash);

#define g_hash_table_insert(h,k,v)    g_hash_table_insert_replace ((h),(k),(v),FALSE)
#define g_hash_table_replace(h,k,v)   g_hash_table_insert_replace ((h),(k),(v),TRUE)

gboolean g_direct_equal (gconstpointer v1, gconstpointer v2);
guint    g_direct_hash  (gconstpointer v1);
gboolean g_int_equal    (gconstpointer v1, gconstpointer v2);
guint    g_int_hash     (gconstpointer v1);
gboolean g_str_equal    (gconstpointer v1, gconstpointer v2);
guint    g_str_hash     (gconstpointer v1);

#endif
