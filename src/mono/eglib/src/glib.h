#ifndef __GLIB_H
#define __GLIB_H

#include <stdarg.h>
#include <stdlib.h>
#include <string.h>
#include <stdio.h>
#include <stdint.h>
#include <eglib-config.h>

/*
 * Basic data types
 */
typedef int            gboolean;
typedef int            gint;
typedef unsigned int   gsize;
typedef signed int     gssize;
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
/*
 * Macros
 */
#define G_N_ELEMENTS(s)      (sizeof(s) / sizeof ((s) [0]))

#define FALSE                0
#define TRUE                 1

#define G_MAXINT32           INT32_MAX
#define G_MININT32           INT32_MIN

#define GPOINTER_TO_INT(ptr)   ((int)(ptr))
#define GPOINTER_TO_UINT(ptr)  ((uint)(ptr))
#define GINT_TO_POINTER(v)     ((gpointer) (v))
#define GUINT_TO_POINTER(v)    ((gpointer) (v))

#define G_GSIZE_FORMAT "u"
#define G_LITTLE_ENDIAN 1234
#define G_BIG_ENDIAN    4321
#define G_STMT_START    do 
#define G_STMT_END      while (0)

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

gpointer g_memdup (gconstpointer mem, guint byte_size);

/*
 * Misc.
 */
#define g_atexit(func)	((void) atexit (func))
/*
 * Precondition macros
 */
#define g_return_if_fail(x)  G_STMT_START { if (!(x)) { printf ("%s:%d: assertion %s failed", __FILE__, __LINE__, #x); return; } } G_STMT_END
#define g_return_val_if_fail(x,e)  G_STMT_START { if (!(x)) { printf ("%s:%d: assertion %s failed", __FILE__, __LINE__, #x); return (e); } } G_STMT_END

/*
 * Hashtables
 */
typedef struct _GHashTable GHashTable;
typedef void     (*GFunc)          (gpointer data, gpointer user_data);
typedef gint     (*GCompareFunc)   (gconstpointer a, gconstpointer b);
typedef gint     (*GCompareDataFunc) (gconstpointer a, gconstpointer b, gpointer user_data);
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

#define  g_assert(x)     G_STMT_START { if (!(x)) g_error ("* Assertion at %s:%d, condition `%s' not met\n", __FILE__, __LINE__, #x);  } G_STMT_END
#define  g_assert_not_reached() G_STMT_START { g_error ("* Assertion: should not be reached at %s:%d\n", __FILE__, __LINE__); } G_STMT_END

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
gboolean     g_str_has_prefix (const gchar *str, const gchar *prefix);
gboolean     g_str_has_suffix (const gchar *str, const gchar *suffix);
gchar       *g_strjoin        (const gchar *separator, ...);
gchar       *g_strchug        (gchar *str);
gchar       *g_strchomp       (gchar *str);

#define g_ascii_isspace(c) (isspace (c) != 0)
#define g_ascii_isalpha(c) (isalpha (c) != 0)
#define g_ascii_isprint(c) (isprint (c) != 0)

/* FIXME: g_strcasecmp supports utf8 unicode stuff */
#define g_strcasecmp strcasecmp
#define g_ascii_strcasecmp strcasecmp
#define g_strncasecmp strncasecmp
#define g_strstrip(a) g_strchug (g_strchomp (a))

/*
 * String type
 */
typedef struct {
	char *str;
	gsize len;
	gsize allocated_len;
} GString;

GString     *g_string_new           (const gchar *init);
GString     *g_string_new_len       (const gchar *init, gssize len);
GString     *g_string_sized_new     (gsize default_size);
gchar       *g_string_free          (GString *string, gboolean free_segment);
GString     *g_string_append        (GString *string, const gchar *val);
void         g_string_printf        (GString *string, const gchar *format, ...);
void         g_string_append_printf (GString *string, const gchar *format, ...);
GString     *g_string_append_c      (GString *string, gchar c);
GString     *g_string_append        (GString *string, const gchar *val);
GString     *g_string_append_len    (GString *string, const gchar *val, gssize len);
GString     *g_string_truncate      (GString *string, gsize len);
GString     *g_string_prepend       (GString *string, const gchar *val);

#define g_string_sprintfa g_string_append_printf

/*
 * Lists
 */
typedef struct _GSList GSList;
struct _GSList {
	gpointer data;
	GSList *next;
};

GSList *g_slist_alloc         (void);
GSList *g_slist_append        (GSList        *list,
			       gpointer       data);
GSList *g_slist_prepend       (GSList        *list,
			       gpointer       data);
void    g_slist_free          (GSList        *list);
void    g_slist_free_1        (GSList        *list);
GSList *g_slist_copy          (GSList        *list);
GSList *g_slist_concat        (GSList        *list1,
			       GSList        *list2);
void    g_slist_foreach       (GSList        *list,
			       GFunc          func,
			       gpointer       user_data);
GSList *g_slist_last          (GSList        *list);
GSList *g_slist_find          (GSList        *list,
			       gconstpointer  data);
GSList *g_slist_remove        (GSList        *list,
			       gconstpointer  data);
GSList *g_slist_reverse       (GSList        *list);
guint   g_slist_length        (GSList        *list);
GSList *g_slist_remove_link   (GSList        *list,
			       GSList        *link);
GSList *g_slist_delete_link   (GSList        *list,
			       GSList        *link);
GSList *g_slist_insert_sorted (GSList        *list,
			       gpointer       data,
			       GCompareFunc   func);

#define g_slist_next(slist) ((slist) ? (((GSList *) (slist))->next) : NULL)

typedef struct _GList GList;
struct _GList {
  gpointer data;
  GList *next;
  GList *prev;
};

#define g_list_next(list) ((list) ? (((GList *) (list))->next) : NULL);

GList *g_list_alloc         (void);
GList *g_list_append        (GList         *list,
			     gpointer       data);
GList *g_list_prepend       (GList         *list,
			     gpointer       data);
void   g_list_free          (GList         *list);
void   g_list_free_1        (GList         *list);
GList *g_list_copy          (GList         *list);
guint  g_list_length        (GList         *list);
gint   g_list_index         (GList         *list,
			     gconstpointer  data);
GList *g_list_nth           (GList         *list,
			     guint          n);
gpointer g_list_nth_data      (GList         *list,
			     guint          n);
GList *g_list_last          (GList         *list);
GList *g_list_concat        (GList         *list1,
			     GList         *list2);
void   g_list_foreach       (GList         *list,
			     GFunc          func,
			     gpointer       user_data);
GList *g_list_first         (GList         *list);
GList *g_list_find          (GList         *list,
			     gconstpointer  data);
GList *g_list_remove        (GList         *list,
			     gconstpointer  data);
GList *g_list_reverse       (GList         *list);
GList *g_list_remove_link   (GList         *list,
			     GList         *link);
GList *g_list_delete_link   (GList         *list,
			     GList         *link);
GList *g_list_insert_sorted (GList         *list,
			     gpointer       data,
			     GCompareFunc   func);


/*
 * Pointer Array
 */

typedef struct _GPtrArray GPtrArray;
struct _GPtrArray {
	gpointer *pdata;
	guint len;
};

GPtrArray *g_ptr_array_new                ();
GPtrArray *g_ptr_array_sized_new          (guint reserved_size);
void       g_ptr_array_add                (GPtrArray *array, gpointer data);
gboolean   g_ptr_array_remove             (GPtrArray *array, gpointer data);
gpointer   g_ptr_array_remove_index       (GPtrArray *array, guint index);
gboolean   g_ptr_array_remove_fast        (GPtrArray *array, gpointer data);
gpointer   g_ptr_array_remove_index_fast  (GPtrArray *array, gpointer data);
void       g_ptr_array_sort               (GPtrArray *array, GCompareFunc compare_func);
void       g_ptr_array_sort_with_data     (GPtrArray *array, GCompareDataFunc compare_func, gpointer user_data);
void       g_ptr_array_set_size           (GPtrArray *array, gint length);
gpointer  *g_ptr_array_free               (GPtrArray *array, gboolean free_seg);
void       g_ptr_array_foreach            (GPtrArray *array, GFunc func, gpointer user_data);
#define    g_ptr_array_index(array,index) (array)->pdata[(index)]


/*
 * Modules
 */
typedef enum {
	G_MODULE_BIND_LAZY = 0x01,
	G_MODULE_BIND_LOCAL = 0x02,
	G_MODULE_BIND_MASK = 0x03
} GModuleFlags;
typedef struct _GModule GModule;

GModule *g_module_open (const gchar *file, GModuleFlags flags);
gboolean g_module_symbol (GModule *module, const gchar *symbol_name,
			  gpointer *symbol);
const gchar *g_module_error (void);
gboolean g_module_close (GModule *module);
gchar *  g_module_build_path (const gchar *directory, const gchar *module_name);
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

/*
 * Unicode Manipulation: most of this is not used by Mono by default, it is
 * only used if the old collation code is activated, so this is only the
 * bare minimum to build.
 */
typedef guint32 gunichar;

typedef enum {
	G_UNICODE_LOWERCASE_LETTER,
} GUnicodeType;

gunichar       g_unichar_tolower (gunichar c);
GUnicodeType   g_unichar_type    (gunichar c);

#ifndef MAX
#define MAX(a,b) (((a)>(b)) ? (a) : (b))
#endif

#endif
