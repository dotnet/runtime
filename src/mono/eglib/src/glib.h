#ifndef __GLIB_H
#define __GLIB_H

#include <stdarg.h>
#include <stdlib.h>
#include <string.h>
#include <stdio.h>
#include <stdint.h>
#include <stddef.h>
#include <ctype.h>
#include <eglib-config.h>

#ifndef offsetof
#   define offsetof(s_name,n_name) (size_t)(char *)&(((s_name*)0)->m_name)
#endif

#define __EGLIB_X11 1
/*
 * Basic data types
 */
typedef int            gboolean;
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
typedef uint16_t       gunichar2;
/*
 * Macros
 */
#define G_N_ELEMENTS(s)      (sizeof(s) / sizeof ((s) [0]))

#define FALSE                0
#define TRUE                 1

#define G_MAXINT32           INT32_MAX
#define G_MININT32           INT32_MIN
#define G_MININT64           INT64_MIN
#define G_MAXINT64	     INT64_MAX

#define G_LITTLE_ENDIAN 1234
#define G_BIG_ENDIAN    4321
#define G_STMT_START    do 
#define G_STMT_END      while (0)

#define G_USEC_PER_SEC  1000000

#define ABS(a,b)        (((a)>(b)) ? ((a)-(b)) : ((b)-(a)))

#define G_STRUCT_OFFSET(p_type,field) offsetof(p_type,field)

#define EGLIB_STRINGIFY(x) #x
#define EGLIB_TOSTRING(x) EGLIB_STRINGIFY(x)
#define G_STRLOC __FILE__ ":" EGLIB_TOSTRING(__LINE__) ":"

#define G_BEGIN_DECLS
#define G_END_DECLS
 
#define G_CONST_RETURN const

/*
 * Allocation
 */
#define g_new(type,size)        ((type *) malloc (sizeof (type) * (size)))
#define g_new0(type,size)       ((type *) calloc (sizeof (type), (size)))
#define g_newa(type,size)       ((type *) alloca (sizeof (type) * (size)))
#define g_realloc(obj,size)     realloc((obj), (size))
#define g_malloc(x)             malloc(x)
#define g_try_malloc(x)         malloc(x)
#define g_try_realloc(obj,size) realloc((obj),(size))
#define g_malloc0(x)            calloc(1,x)
#define g_memmove(dest,src,len) memmove (dest, src, len)
#define g_renew(struct_type, mem, n_structs) realloc (mem, sizeof (struct_type) * n_structs)
#define g_alloca(size)		alloca (size)
#define g_free                  free

gpointer g_memdup (gconstpointer mem, guint byte_size);
gchar   *g_strdup (const gchar *str);

typedef struct {
	gpointer (*malloc)      (gsize    n_bytes);
	gpointer (*realloc)     (gpointer mem, gsize n_bytes);
	void     (*free)        (gpointer mem);
	gpointer (*calloc)      (gsize    n_blocks, gsize n_block_bytes);
	gpointer (*try_malloc)  (gsize    n_bytes);
	gpointer (*try_realloc) (gpointer mem, gsize n_bytes);
} GMemVTable;

#define g_mem_set_vtable(x)
/*
 * Misc.
 */
#define g_atexit(func)	((void) atexit (func))

const gchar *    g_getenv(const gchar *variable);
gboolean         g_setenv(const gchar *variable, const gchar *value, gboolean overwrite);
void             g_unsetenv(const gchar *variable);

/*
 * Precondition macros
 */
#define g_return_if_fail(x)  G_STMT_START { if (!(x)) { printf ("%s:%d: assertion '%s' failed", __FILE__, __LINE__, #x); return; } } G_STMT_END
#define g_return_val_if_fail(x,e)  G_STMT_START { if (!(x)) { printf ("%s:%d: assertion '%s' failed", __FILE__, __LINE__, #x); return (e); } } G_STMT_END

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
GHashTable     *g_hash_table_new_full        (GHashFunc hash_func, GEqualFunc key_equal_func,
					      GDestroyNotify key_destroy_func, GDestroyNotify value_destroy_func);
void            g_hash_table_insert_replace  (GHashTable *hash, gpointer key, gpointer value, gboolean replace);
guint           g_hash_table_size            (GHashTable *hash);
gpointer        g_hash_table_lookup          (GHashTable *hash, gconstpointer key);
gboolean        g_hash_table_lookup_extended (GHashTable *hash, gconstpointer key, gpointer *orig_key, gpointer *value);
void            g_hash_table_foreach         (GHashTable *hash, GHFunc func, gpointer user_data);
gpointer        g_hash_table_find            (GHashTable *hash, GHRFunc predicate, gpointer user_data);
gboolean        g_hash_table_remove          (GHashTable *hash, gconstpointer key);
guint           g_hash_table_foreach_remove  (GHashTable *hash, GHRFunc func, gpointer user_data);
void            g_hash_table_destroy         (GHashTable *hash);

guint           g_spaced_primes_closest      (guint x);

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
 * Errors
 */
typedef struct {
	/* In the real glib, this is a GQuark, but we dont use/need that */
	gpointer domain;
	gint     code;
	gchar   *message;
} GError;

void    g_error_free (GError *error);
GError *g_error_new  (gpointer domain, gint code, const char *format, ...);
void    g_set_error  (GError **err, gpointer domain, gint code, const gchar *format, ...);

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
guint        g_strv_length    (gchar **str_array);
gchar       *g_strjoin        (const gchar *separator, ...);
gchar       *g_strchug        (gchar *str);
gchar       *g_strchomp       (gchar *str);
gchar       *g_strdelimit     (gchar *string, const gchar *delimiters, gchar new_delimiter);
gchar       *g_strescape      (const gchar *source, const gchar *exceptions);

gchar       *g_filename_to_uri   (const gchar *filename, const gchar *hostname, GError **error);
gchar       *g_filename_from_uri (const gchar *uri, gchar **hostname, GError **error);

gint         g_printf          (gchar const *format, ...);
gint         g_fprintf         (FILE *file, gchar const *format, ...);
gint         g_sprintf         (gchar *string, gchar const *format, ...);
gint         g_snprintf        (gchar *string, gulong n, gchar const *format, ...);
#define g_vprintf vprintf
#define g_vfprintf vfprintf
#define g_vsprintf vsprintf
#define g_vsnprintf vsnprintf
#define g_vasprintf vasprintf

#ifdef HAVE_STRLCPY
#define g_strlcpy	strlcpy
#else
gsize       g_strlcpy          (gchar *dest, const gchar *src, gsize dest_size);
#endif

gchar  *g_ascii_strdown      (const gchar *str, gssize len);
gint    g_ascii_strncasecmp  (const gchar *s1, const gchar *s2, gsize n);
gint    g_ascii_xdigit_value (gchar c);
#define g_ascii_isspace(c)   (isspace (c) != 0)
#define g_ascii_isalpha(c)   (isalpha (c) != 0)
#define g_ascii_isprint(c)   (isprint (c) != 0)
#define g_ascii_isxdigit(c)  (isxdigit (c) != 0)

/* FIXME: g_strcasecmp supports utf8 unicode stuff */
#define g_strcasecmp strcasecmp
#define g_ascii_strcasecmp strcasecmp
#define g_strncasecmp strncasecmp
#define g_strstrip(a) g_strchug (g_strchomp (a))

#define	G_STR_DELIMITERS "_-|> <."

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
GSList *g_slist_remove_all    (GSList        *list,
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
GSList *g_slist_insert_before (GSList        *list,
			       GSList        *sibling,
			       gpointer       data);
GSList *g_slist_sort          (GSList        *list,
			       GCompareFunc   func);
#define g_slist_next(slist) ((slist) ? (((GSList *) (slist))->next) : NULL)

typedef struct _GList GList;
struct _GList {
  gpointer data;
  GList *next;
  GList *prev;
};

#define g_list_next(list) ((list) ? (((GList *) (list))->next) : NULL)

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
GList *g_list_insert_before (GList         *list,
			     GList         *sibling,
			     gpointer       data);
GList *g_list_sort          (GList         *sort,
			     GCompareFunc   func);

/*
 * Array
 */

typedef struct _GArray GArray;
struct _GArray {
	gchar *data;
	gint len;
};

GArray *g_array_new               (gboolean zero_terminated, gboolean clear_, guint element_size);
gchar*  g_array_free              (GArray *array, gboolean free_segment);
GArray *g_array_append_vals       (GArray *array, gconstpointer data, guint len);
GArray* g_array_insert_vals       (GArray *array, guint index_, gconstpointer data, guint len);
GArray* g_array_remove_index      (GArray *array, guint index_);

#define g_array_append_val(a,v)   (g_array_append_vals((a),&(v),1))
#define g_array_insert_val(a,i,v) (g_array_insert_vals((a),(i),&(v),1))
#define g_array_index(a,t,i)      *(t*)(((a)->data) + sizeof(t) * (i))

/*
 * Pointer Array
 */

typedef struct _GPtrArray GPtrArray;
struct _GPtrArray {
	gpointer *pdata;
	guint len;
};

GPtrArray *g_ptr_array_new                (void);
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
 * Queues
 */
typedef struct {
	GList *head;
	GList *tail;
	guint length;
} GQueue;

gpointer g_queue_pop_head  (GQueue   *queue);
void     g_queue_push_head (GQueue   *queue,
			    gpointer  data);
gboolean g_queue_is_empty  (GQueue   *queue);
GQueue  *g_queue_new       (void);
void     g_queue_free      (GQueue   *queue);

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

#define g_log_set_handler(a,b,c,d)
#define g_printerr(format...) fprintf (stderr, format)
/*
 * Conversions
 */

gpointer g_convert_error_quark(void);


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

#ifndef MIN
#define MIN(a,b) (((a)<(b)) ? (a) : (b))
#endif

/* FIXME: Implement these two for gcc */
#define G_LIKELY(x) (x)
#define G_UNLIKELY(x) (x)

/*
 * Unicode conversion
 */

#define G_CONVERT_ERROR g_convert_error_quark()

typedef enum {
	G_CONVERT_ERROR_NO_CONVERSION,
	G_CONVERT_ERROR_ILLEGAL_SEQUENCE,
	G_CONVERT_ERROR_FAILED,
	G_CONVERT_ERROR_PARTIAL_INPUT,
	G_CONVERT_ERROR_BAD_URI,
	G_CONVERT_ERROR_NOT_ABSOLUTE_PATH
} GConvertError;

gunichar2 *g_utf8_to_utf16 (const gchar *str, glong len, glong *items_read, glong     *items_written, GError **error);
gchar     *g_utf16_to_utf8 (const gunichar2 *str, glong len, glong *items_read, glong     *items_written, GError **error);

/*
 * Path
 */
gchar  *g_build_path           (const gchar *separator, const gchar *first_element, ...);
#define g_build_filename(x...) g_build_path(G_DIR_SEPARATOR_S, x)
gchar  *g_path_get_dirname     (const gchar *filename);
gchar  *g_path_get_basename    (const char *filename);
gchar  *g_find_program_in_path (const gchar *program);
gchar  *g_get_current_dir      (void);
gboolean g_path_is_absolute    (const char *filename);

const gchar *g_get_home_dir    (void);
const gchar *g_get_tmp_dir     (void);
const gchar *g_get_user_name   (void);
gchar *g_get_prgname           (void);
void  g_set_prgname            (const gchar *prgname);

/*
 * Shell
 */

gboolean  g_shell_parse_argv (const gchar *command_line, gint *argcp, gchar ***argvp, GError **error);
gchar    *g_shell_unquote    (const gchar *quoted_string, GError **error);
gchar    *g_shell_quote      (const gchar *unquoted_string);

/*
 * Spawn
 */
typedef enum {
	G_SPAWN_LEAVE_DESCRIPTORS_OPEN = 1,
	G_SPAWN_DO_NOT_REAP_CHILD      = 1 << 1,
	G_SPAWN_SEARCH_PATH            = 1 << 2,
	G_SPAWN_STDOUT_TO_DEV_NULL     = 1 << 3,
	G_SPAWN_STDERR_TO_DEV_NULL     = 1 << 4,
	G_SPAWN_CHILD_INHERITS_STDIN   = 1 << 5,
	G_SPAWN_FILE_AND_ARGV_ZERO     = 1 << 6
} GSpawnFlags;

typedef pid_t GPid;

typedef void (*GSpawnChildSetupFunc) (gpointer user_data);

gboolean g_spawn_command_line_sync (const gchar *command_line, gchar **standard_output, gchar **standard_error, gint *exit_status, GError **error);
gboolean g_spawn_async_with_pipes  (const gchar *working_directory, gchar **argv, gchar **envp, GSpawnFlags flags, GSpawnChildSetupFunc child_setup,
				gpointer user_data, GPid *child_pid, gint *standard_input, gint *standard_output, gint *standard_error, GError **error);


/*
 * Timer
 */
typedef struct _GTimer GTimer;

GTimer *g_timer_new (void);
void g_timer_destroy (GTimer *timer);
gdouble g_timer_elapsed (GTimer *timer, gulong *microseconds);
void g_timer_stop (GTimer *timer);
void g_timer_start (GTimer *timer);

/*
 * Date and time
 */
typedef struct {
	glong tv_sec;
	glong tv_usec;
} GTimeVal;

void g_get_current_time (GTimeVal *result);

/*
 * File
 */

typedef enum {
	G_FILE_ERROR_EXIST,
	G_FILE_ERROR_ISDIR,
	G_FILE_ERROR_ACCES,
	G_FILE_ERROR_NAMETOOLONG,
	G_FILE_ERROR_NOENT,
	G_FILE_ERROR_NOTDIR,
	G_FILE_ERROR_NXIO,
	G_FILE_ERROR_NODEV,
	G_FILE_ERROR_ROFS,
	G_FILE_ERROR_TXTBSY,
	G_FILE_ERROR_FAULT,
	G_FILE_ERROR_LOOP,
	G_FILE_ERROR_NOSPC,
	G_FILE_ERROR_NOMEM,
	G_FILE_ERROR_MFILE,
	G_FILE_ERROR_NFILE,
	G_FILE_ERROR_BADF,
	G_FILE_ERROR_INVAL,
	G_FILE_ERROR_PIPE,
	G_FILE_ERROR_AGAIN,
	G_FILE_ERROR_INTR,
	G_FILE_ERROR_IO,
	G_FILE_ERROR_PERM,
	G_FILE_ERROR_NOSYS,
	G_FILE_ERROR_FAILED
} GFileError;

typedef enum {
	G_FILE_TEST_IS_REGULAR = 1 << 0,
	G_FILE_TEST_IS_SYMLINK = 1 << 1,
	G_FILE_TEST_IS_DIR = 1 << 2,
	G_FILE_TEST_IS_EXECUTABLE = 1 << 3,
	G_FILE_TEST_EXISTS = 1 << 4
} GFileTest;


gboolean   g_file_get_contents (const gchar *filename, gchar **contents, gsize *length, GError **error);
GFileError g_file_error_from_errno (gint err_no);
gint       g_file_open_tmp (const gchar *tmpl, gchar **name_used, GError **error);
gboolean   g_file_test (const gchar *filename, GFileTest test);

/*
 * Pattern matching
 */
typedef struct _GPatternSpec GPatternSpec;
GPatternSpec * g_pattern_spec_new (const gchar *pattern);
void           g_pattern_spec_free (GPatternSpec *pspec);
gboolean       g_pattern_match_string (GPatternSpec *pspec, const gchar *string);

/*
 * Directory
 */
typedef struct _GDir GDir;
GDir        *g_dir_open (const gchar *path, guint flags, GError **error);
const gchar *g_dir_read_name (GDir *dir);
void         g_dir_rewind (GDir *dir);
void         g_dir_close (GDir *dir);

/*
 * GMarkup
 */
typedef struct _GMarkupParseContext GMarkupParseContext;

typedef enum
{
	G_MARKUP_DO_NOT_USE_THIS_UNSUPPORTED_FLAG = 1 << 0,
	G_MARKUP_TREAT_CDATA_AS_TEXT              = 1 << 1
} GMarkupParseFlags;

typedef struct {
	void (*start_element)  (GMarkupParseContext *context,
				const gchar *element_name,
				const gchar **attribute_names,
				const gchar **attribute_values,
				gpointer user_data,
				GError **error);

	void (*end_element)    (GMarkupParseContext *context,
				const gchar         *element_name,
				gpointer             user_data,
				GError             **error);
	
	void (*text)           (GMarkupParseContext *context,
				const gchar         *text,
				gsize                text_len,  
				gpointer             user_data,
				GError             **error);
	
	void (*passthrough)    (GMarkupParseContext *context,
				const gchar         *passthrough_text,
				gsize                text_len,  
				gpointer             user_data,
				GError             **error);
	void (*error)          (GMarkupParseContext *context,
				GError              *error,
				gpointer             user_data);
} GMarkupParser;

GMarkupParseContext *g_markup_parse_context_new   (const GMarkupParser *parser,
						   GMarkupParseFlags flags,
						   gpointer user_data,
						   GDestroyNotify user_data_dnotify);
void                 g_markup_parse_context_free  (GMarkupParseContext *context);
gboolean             g_markup_parse_context_parse (GMarkupParseContext *context,
						   const gchar *text, gssize text_len,
						   GError **error);
gboolean         g_markup_parse_context_end_parse (GMarkupParseContext *context,
						   GError **error);

/*
 * Character set conversion
 */
gboolean  g_get_charset        (char **charset);
gchar    *g_locale_to_utf8     (const gchar *opsysstring, gssize len,
				gsize *bytes_read, gsize *bytes_written,
				GError **error);
gchar    *g_locale_from_utf8   (const gchar *utf8string, gssize len, gsize *bytes_read,
				gsize *bytes_written, GError **error);
gchar    *g_filename_from_utf8 (const gchar *utf8string, gssize len, gsize *bytes_read,
				gsize *bytes_written, GError **error);
gchar    *g_convert            (const gchar *str, gssize len,
				const gchar *to_codeset, const gchar *from_codeset,
				gsize *bytes_read, gsize *bytes_written, GError **error);
gboolean  g_utf8_validate      (const gchar *str, gssize max_len, const gchar **end);

/*
 * Empty thread functions, not used by eglib
 */
#define g_thread_supported()   TRUE
#define g_thread_init(x)       G_STMT_START { if (x != NULL) { g_error ("No vtable supported in g_thread_init"); } } G_STMT_END

#define GUINT16_SWAP_LE_BE(x) ((guint16) (((guint16) x) >> 8) | ((((guint16)(x)) & 0xff) << 8))
#define GUINT32_SWAP_LE_BE(x) ((guint32) \
			       ( (((guint32) (x)) << 24)| \
				 ((((guint32) (x)) & 0xff0000) >> 8) | \
		                 ((((guint32) (x)) & 0xff00) << 8) | \
			         (((guint32) (x)) >> 24)) )
 
#define GUINT64_SWAP_LE_BE(x) ((guint64) (((guint64)(GUINT32_SWAP_LE_BE(((guint64)x) & 0xffffffff))) << 32) | \
	      	               GUINT32_SWAP_LE_BE(((guint64)x) >> 32))

				  
 
#if G_BYTE_ORDER == G_LITTLE_ENDIAN
#   define GUINT32_TO_LE(x) (x)
#   define GUINT64_TO_LE(x) (x)
#   define GUINT16_TO_LE(x) (x)
#else
#   define GUINT32_TO_LE(x) GUINT32_SWAP_LE_BE(x)
#   define GUINT64_TO_LE(x) GUINT64_SWAP_LE_BE(x)
#   define GUINT16_TO_LE(x) GUINT16_SWAP_LE_BE(x)
#endif

#define GUINT32_FROM_LE(x)  (GUINT32_TO_LE (x))
#define GUINT64_FROM_LE(x)  (GUINT64_TO_LE (x))
#define GUINT16_FROM_LE(x)  (GUINT16_TO_LE (x))

#define _EGLIB_MAJOR  2
#define _EGLIB_MIDDLE 4
#define _EGLIB_MINOR  0
 
#define GLIB_CHECK_VERSION(a,b,c) ((a < _EGLIB_MAJOR) || (a == _EGLIB_MAJOR && (b < _EGLIB_MIDDLE || (b == _EGLIB_MIDDLE && c <= _EGLIB_MINOR))))
 
#endif

