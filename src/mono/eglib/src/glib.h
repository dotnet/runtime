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

/*
 * Hashtables
 */
typedef struct _GHashTable GHashTable;

typedef guint    (*GHashFunc)  (gconstpointer key);
typedef gboolean (*GEqualFunc) (gconstpointer a, gconstpointer b);

GHashTable     *g_hash_table_new (GHashFunc hash_func, GEqualFunc key_equal_func);

#endif
