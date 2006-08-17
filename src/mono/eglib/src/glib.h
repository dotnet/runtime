#ifndef __GLIB_H
#define __GLIB_H

#include <stdarg.h>
#include <stdlib.h>
#include <string.h>

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

#define G_STRUCT_OFFSET(p_type,field) \
        ((long) (((char *) (&(((p_type)NULL)->field))) - ((char *) NULL)))

/*
 * Allocation
 */
#define g_new(type,size)        ((type *) malloc (sizeof (type) * (size)))
#define g_new0(type,size)       ((type *) calloc (sizeof (type), (size))) 
#define g_free(obj)             free (obj);
#define g_realloc(obj,size)     realloc((obj), (size))
#define g_strdup(x)             strdup(x)
#define g_malloc(x)             malloc(x)
#define g_try_malloc(x)         malloc(x)
#define g_try_realloc(obj,size) realloc((obj),(size))
#define g_malloc0(x)            calloc(1,x)
#define g_memmove(dest,src,len) memmove (dest, src, len)
#define g_renew(struct_type, mem, n_structs) realloc (mem, sizeof (struct_type) * n_structs)
#define g_alloca(size)		alloca (size)

/*
 * Misc.
 */
#define g_atexit(func)	((void) atexit (func))
/*
 * Basic data types
 */
typedef int            gboolean;
typedef int            gint;
typedef unsigned int   gsize;
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
typedef void     (* GFunc)         (gpointer data, gpointer user_data);
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

#define  g_assert(x)     do { if (!(x)) g_error ("* Assertion at %s:%d, condition `%s' not met\n", __FILE__, __LINE__, #x); } while (0)
#define  g_assert_not_reached() do { g_error ("* Assertion: should not be reached at %s:%d\n", __FILE__, __LINE__); } while (0)

/*
 * Strings utility
 */
gchar       *g_strdup_printf  (const gchar *format, ...);
gchar       *g_strdup_vprintf (const gchar *format, va_list args);
gchar       *g_strndup        (const gchar *str, gsize n);
const gchar *g_strerror       (gint errnum);
gchar       *g_strndup        (const gchar *str, gsize n);
void         g_strfreev       (gchar **str_array);
gchar       *g_strconcat      (const gchar *first, ...);
gchar      **g_strsplit       (const gchar *string, const gchar *delimiter, gint max_tokens);
gchar       *g_strreverse     (gchar *str);

/*
 * String type
 */
typedef struct {
	char *str;
	gsize len;
	gsize allocated_len;
} GString;

GString     *g_string_new           (const gchar *init);
GString     *g_string_new_len       (const gchar *init, gsize len);
GString     *g_string_sized_new     (gsize default_size);
gchar       *g_string_free          (GString *string, gboolean free_segment);
GString     *g_string_append        (GString *string, const gchar *val);
void         g_string_printf        (GString *string, const gchar *format, ...);
void         g_string_append_printf (GString *string, const gchar *format, ...);
GString     *g_string_append_c      (GString *string, gchar c);
GString     *g_string_append        (GString *string, const gchar *val);

#define g_string_sprintfa g_string_append_printf

/*
 * Lists
 */
typedef struct _GSList GSList;
struct _GSList {
	gpointer data;
	GSList *next;
};

GSList *g_slist_alloc     (void);
GSList *g_slist_append    (GSList* list, gpointer data);
GSList *g_slist_prepend   (GSList* list, gpointer data);
void    g_slist_free      (GSList* list);
void    g_slist_free_1    (GSList* list);
GSList *g_slist_copy      (GSList* list);
GSList *g_slist_concat    (GSList* list1, GSList* list2);
void    g_slist_foreach   (GSList* list, GFunc func, gpointer user_data);
GSList *g_slist_last      (GSList *list);

#define g_slist_next (slist) ((slist) ? (((GSList *) slist)->next) : NULL)

/*
 * Messages
 */
#ifndef G_LOG_DOMAIN
#define G_LOG_DOMAIN ((gchar*) 0)
#endif

typedef enum {
	G_LOG_FLAG_RECURSION          = 1 << 0,
	G_LOG_FLAG_FATAL              = 1 << 1,
	
	G_LOG_LEVEL_ERROR             = 1 << 2,
	G_LOG_LEVEL_CRITICAL          = 1 << 3,
	G_LOG_LEVEL_WARNING           = 1 << 4,
	G_LOG_LEVEL_MESSAGE           = 1 << 5,
	G_LOG_LEVEL_INFO              = 1 << 6,
	G_LOG_LEVEL_DEBUG             = 1 << 7,
	
	G_LOG_LEVEL_MASK              = ~(G_LOG_FLAG_RECURSION | G_LOG_FLAG_FATAL)
} GLogLevelFlags;

void           g_print                (const gchar *format, ...);
GLogLevelFlags g_log_set_always_fatal (GLogLevelFlags fatal_mask);
GLogLevelFlags g_log_set_fatal_mask   (const gchar *log_domain, GLogLevelFlags fatal_mask);
void           g_logv                 (const gchar *log_domain, GLogLevelFlags log_level, const gchar *format, va_list args);
void           g_log                  (const gchar *log_domain, GLogLevelFlags log_level, const gchar *format, ...);

#define g_error(format...)    g_log (G_LOG_DOMAIN, G_LOG_LEVEL_ERROR, format)
#define g_critical(format...) g_log (G_LOG_DOMAIN, G_LOG_LEVEL_CRITICAL, format)
#define g_warning(format...)  g_log (G_LOG_DOMAIN, G_LOG_LEVEL_WARNING, format)
#define g_message(format...)  g_log (G_LOG_DOMAIN, G_LOG_LEVEL_MESSAGE, format)
#define g_debug(format...)    g_log (G_LOG_DOMAIN, G_LOG_LEVEL_DEBUG, format)

#endif
