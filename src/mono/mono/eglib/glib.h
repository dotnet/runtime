#ifndef __GLIB_H
#define __GLIB_H

// Ask stdint.h and inttypes.h for the full C99 features for CentOS 6 g++ 4.4, Android, etc.
// See for example:
// $HOME/android-toolchain/toolchains/armeabi-v7a-clang/sysroot/usr/include/inttypes.h
// $HOME/android-toolchain/toolchains/armeabi-v7a-clang/sysroot/usr/include/stdint.h
#ifdef __cplusplus
#ifndef __STDC_LIMIT_MACROS
#define __STDC_LIMIT_MACROS
#endif
#ifndef __STDC_CONSTANT_MACROS
#define __STDC_CONSTANT_MACROS
#endif
#ifndef __STDC_FORMAT_MACROS
#define __STDC_FORMAT_MACROS
#endif
#endif // __cplusplus

#include <stdarg.h>
#include <stdlib.h>
#include <string.h>
#include <stdio.h>
#include <stddef.h>
#include <ctype.h>
#include <limits.h>
#include <stdint.h>
#include <inttypes.h>
#include <eglib-config.h>
#include <minipal/utils.h>
#include <time.h>

#ifndef EGLIB_NO_REMAP
#include <eglib-remap.h>
#endif

#ifdef G_HAVE_ALLOCA_H
#include <alloca.h>
#endif

#ifdef WIN32
/* For alloca */
#include <malloc.h>
#endif

#ifdef G_HAVE_UNISTD_H
#include <unistd.h>
#endif

#ifndef offsetof
#   define offsetof(s_name,n_name) (size_t)(char *)&(((s_name*)0)->m_name)
#endif

#ifdef  __cplusplus
#define G_BEGIN_DECLS  extern "C" {
#define G_END_DECLS    }
#define G_EXTERN_C     extern "C"
#else
#define G_BEGIN_DECLS  /* nothing */
#define G_END_DECLS    /* nothing */
#define G_EXTERN_C     /* nothing */
#endif

#ifdef __cplusplus

#define g_cast monoeg_g_cast // in case not inlined (see eglib-remap.h)

// g_cast converts void* to T*.
// e.g. #define malloc(x) (g_cast (malloc (x)))
// FIXME It used to do more. Rename?
struct g_cast
{
private:
	void * const x;
public:
	explicit g_cast (void volatile *y) : x((void*)y) { }
	// Lack of rvalue constructor inhibits ternary operator.
	// Either don't use ternary, or cast each side.
	// sa = (salen <= 128) ? g_alloca (salen) : g_malloc (salen);
	// w32socket.c:1045:24: error: call to deleted constructor of 'monoeg_g_cast'
	//g_cast (g_cast&& y) : x(y.x) { }
	g_cast (g_cast&&) = delete;
	g_cast () = delete;
	g_cast (const g_cast&) = delete;

	template <typename TTo>
	operator TTo* () const
	{
		return (TTo*)x;
	}
};

#else

// FIXME? Parens are omitted to preserve prior meaning.
#define g_cast(x) x

#endif

// G++4.4 breaks opeq below without this.
#if defined  (__GNUC__) || defined  (__clang__)
#define G_MAY_ALIAS  __attribute__((__may_alias__))
#else
#define G_MAY_ALIAS /* nothing */
#endif

#ifdef __cplusplus

// Provide for bit operations on enums, but not all integer operations.
// This alleviates a fair number of casts in porting C to C++.

// Forward declare template with no generic implementation.
template <size_t> struct g_size_to_int;

// Template specializations.
template <> struct g_size_to_int<1> { typedef int8_t type; };
template <> struct g_size_to_int<2> { typedef int16_t type; };
template <> struct g_size_to_int<4> { typedef int32_t type; };
template <> struct g_size_to_int<8> { typedef int64_t type; };

// g++4.4 does not accept:
//template <typename T>
//using g_size_to_int_t = typename g_size_to_int <sizeof (T)>::type;
#define g_size_to_int_t(x) g_size_to_int <sizeof (x)>::type

#define G_ENUM_BINOP(Enum, op, opeq) 		\
inline Enum					\
operator op (Enum a, Enum b)			\
{						\
	typedef g_size_to_int_t (Enum) type; 	\
	return static_cast<Enum>(static_cast<type>(a) op b); \
}						\
						\
inline Enum&					\
operator opeq (Enum& a, Enum b)			\
{						\
	typedef g_size_to_int_t (Enum) G_MAY_ALIAS type; \
	return (Enum&)((type&)a opeq b); 	\
}						\

#define G_ENUM_FUNCTIONS(Enum)			\
extern "C++" { /* in case within extern "C" */	\
inline Enum					\
operator~ (Enum a)				\
{						\
	typedef g_size_to_int_t (Enum) type; 	\
	return static_cast<Enum>(~static_cast<type>(a)); \
}						\
						\
G_ENUM_BINOP (Enum, |, |=) 			\
G_ENUM_BINOP (Enum, &, &=) 			\
G_ENUM_BINOP (Enum, ^, ^=) 			\
						\
} /* extern "C++" */

#else
#define G_ENUM_FUNCTIONS(Enum) /* nothing */
#endif

G_BEGIN_DECLS

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
typedef ptrdiff_t      gptrdiff;
typedef intptr_t       gintptr;
typedef uintptr_t      guintptr;

#if defined (HOST_WIN32) || defined (_WIN32)
G_END_DECLS
#include <wchar.h>
typedef wchar_t gunichar2;
G_BEGIN_DECLS
#else
typedef guint16 gunichar2;
#endif
typedef guint32 gunichar;

/*
 * Macros
 */
#define G_N_ELEMENTS(s)      ARRAY_SIZE(s)

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

#define G_USEC_PER_SEC  1000000

#ifndef ABS
#define ABS(a)         ((a) > 0 ? (a) : -(a))
#endif

#define ALIGN_TO(val,align) ((((gssize)val) + (gssize)((align) - 1)) & (~((gssize)(align - 1))))

#define ALIGN_DOWN_TO(val,align) (((gssize)val) & (~((gssize)(align - 1))))

#define ALIGN_PTR_TO(ptr,align) (gpointer)((((gssize)(ptr)) + (gssize)(align - 1)) & (~((gssize)(align - 1))))

#define G_STRUCT_OFFSET(p_type,field) offsetof(p_type,field)

#define EGLIB_STRINGIFY(x) #x
#define EGLIB_TOSTRING(x) EGLIB_STRINGIFY(x)
#define G_STRLOC __FILE__ ":" EGLIB_TOSTRING(__LINE__) ":"

#define G_CONST_RETURN const

#define G_GUINT64_FORMAT PRIu64
#define G_GINT64_FORMAT PRIi64
#define G_GUINT32_FORMAT PRIu32
#define G_GINT32_FORMAT PRIi32

#ifdef __GNUC__
#define G_ATTR_FORMAT_PRINTF(fmt_pos,arg_pos) __attribute__((__format__(__printf__,fmt_pos,arg_pos)))
#else
#define G_ATTR_FORMAT_PRINTF(fmt_pos,arg_pos)
#endif

/*
 * Allocation
 */
G_EXTERN_C // Used by MonoPosixHelper or MonoSupportW, at least.
void g_free (void *ptr);
G_EXTERN_C // Used by MonoPosixHelper or MonoSupportW, at least.
gpointer g_realloc (gpointer obj, gsize size);
G_EXTERN_C // Used by MonoPosixHelper or MonoSupportW, at least.
gpointer g_malloc (gsize x);
G_EXTERN_C // Used by MonoPosixHelper or MonoSupportW, at least.
gpointer g_malloc0 (gsize x);
G_EXTERN_C // Used by profilers, at least.
gpointer g_calloc (gsize n, gsize x);
gpointer g_try_malloc (gsize x);
gpointer g_try_realloc (gpointer obj, gsize size);

#define g_new(type,size)        ((type *) g_malloc (sizeof (type) * (size)))
#define g_new0(type,size)       ((type *) g_malloc0 (sizeof (type)* (size)))
#define g_newa(type,size)       ((type *) alloca (sizeof (type) * (size)))
#define g_newa0(type,size)      ((type *) memset (alloca (sizeof (type) * (size)), 0, sizeof (type) * (size)))

#define g_memmove(dest,src,len) memmove (dest, src, len)
#define g_renew(struct_type, mem, n_structs) ((struct_type*)g_realloc (mem, sizeof (struct_type) * n_structs))
#define g_alloca(size)		(g_cast (alloca (size)))

G_EXTERN_C // Used by libtest, at least.
gpointer g_memdup (gconstpointer mem, guint byte_size);
static inline gchar   *g_strdup (const gchar *str) { if (str) { return (gchar*) g_memdup (str, (guint)strlen (str) + 1); } return NULL; }
gchar **g_strdupv (gchar **str_array);

typedef struct {
	gpointer (*malloc)      (gsize    n_bytes);
	gpointer (*realloc)     (gpointer mem, gsize n_bytes);
	void     (*free)        (gpointer mem);
	gpointer (*calloc)      (gsize    n_blocks, gsize n_block_bytes);
} GMemVTable;

void g_mem_set_vtable (GMemVTable* vtable);
void g_mem_get_vtable (GMemVTable* vtable);

struct _GMemChunk {
	guint alloc_size;
};

typedef struct _GMemChunk GMemChunk;
/*
 * Misc.
 */

gboolean         g_hasenv(const gchar *variable);
gchar *          g_getenv(const gchar *variable);

G_EXTERN_C // sdks/wasm/driver.c is C and uses this
gboolean         g_setenv(const gchar *variable, const gchar *value, gboolean overwrite);

/*
 * Precondition macros
 */
#define g_warn_if_fail(x)  G_STMT_START { if (!(x)) { g_warning ("%s:%d: assertion '%s' failed\n", __FILE__, __LINE__, #x); } } G_STMT_END
#define g_return_if_fail(x)  G_STMT_START { if (!(x)) { g_critical ("%s:%d: assertion '%s' failed\n", __FILE__, __LINE__, #x); return; } } G_STMT_END
#define g_return_val_if_fail(x,e)  G_STMT_START { if (!(x)) { g_critical ("%s:%d: assertion '%s' failed\n", __FILE__, __LINE__, #x); return (e); } } G_STMT_END

/*
 * Errors
 */
typedef struct {
	/* In the real glib, this is a GQuark, but we dont use/need that */
	gpointer domain;
	gint     code;
	gchar   *message;
} GError;

void    g_clear_error (GError **gerror);
void    g_error_free (GError *gerror);
GError *g_error_new  (gpointer domain, gint code, const char *format, ...);
void    g_set_error  (GError **err, gpointer domain, gint code, const gchar *format, ...);
void    g_propagate_error (GError **dest, GError *src);

/*
 * Strings utility
 */
G_EXTERN_C // Used by libtest, at least.
gchar       *g_strdup_printf  (const gchar *format, ...) G_ATTR_FORMAT_PRINTF(1, 2);
gchar       *g_strdup_vprintf (const gchar *format, va_list args);
gchar       *g_strndup        (const gchar *str, gsize n);
const gchar *g_strerror       (gint errnum);
gchar       *g_strndup        (const gchar *str, gsize n);
void         g_strfreev       (gchar **str_array);
gchar       *g_strconcat      (const gchar *first, ...);
gchar      **g_strsplit       (const gchar *string, const gchar *delimiter, gint max_tokens);
gchar      **g_strsplit_set   (const gchar *string, const gchar *delimiter, gint max_tokens);
gchar       *g_strreverse     (gchar *str);
gboolean     g_str_has_prefix (const gchar *str, const gchar *prefix);
gboolean     g_str_has_suffix (const gchar *str, const gchar *suffix);
guint        g_strv_length    (gchar **str_array);
gchar       *g_strjoin        (const gchar *separator, ...);
gchar       *g_strjoinv       (const gchar *separator, gchar **str_array);
gchar       *g_strchug        (gchar *str);
gchar       *g_strchomp       (gchar *str);
gchar       *g_strnfill       (gsize length, gchar fill_char);
gsize        g_strnlen        (const char*, gsize);
char        *g_str_from_file_region (int fd, guint64 offset, gsize size);

void	     g_strdelimit     (char *string, char delimiter, char new_delimiter);

gint         g_printf          (gchar const *format, ...) G_ATTR_FORMAT_PRINTF(1, 2);
gint         g_fprintf         (FILE *file, gchar const *format, ...) G_ATTR_FORMAT_PRINTF(2, 3);
gint         g_sprintf         (gchar *string, gchar const *format, ...) G_ATTR_FORMAT_PRINTF(2, 3);
gint         g_snprintf        (gchar *string, gulong n, gchar const *format, ...) G_ATTR_FORMAT_PRINTF(3, 4);
gint         g_vasprintf       (gchar **ret, const gchar *fmt, va_list ap);
#define g_vprintf vprintf
#define g_vfprintf vfprintf
#define g_vsprintf vsprintf
#define g_vsnprintf vsnprintf

gsize   g_strlcpy            (gchar *dest, const gchar *src, gsize dest_size);
gchar  *g_stpcpy             (gchar *dest, const char *src);


gchar   g_ascii_tolower      (gchar c);
gchar   g_ascii_toupper      (gchar c);
gchar  *g_ascii_strdown      (const gchar *str, gssize len);
void    g_ascii_strdown_no_alloc (char* dst, const char* src, gsize len);
gchar  *g_ascii_strup        (const gchar *str, gssize len);
gint    g_ascii_strncasecmp  (const gchar *s1, const gchar *s2, gsize n);
gint    g_ascii_strcasecmp   (const gchar *s1, const gchar *s2);
gint    g_ascii_xdigit_value (gchar c);
#define g_ascii_isspace(c)   (isspace (c) != 0)
#define g_ascii_isalpha(c)   (isalpha (c) != 0)
#define g_ascii_isprint(c)   (isprint (c) != 0)
#define g_ascii_isxdigit(c)  (isxdigit (c) != 0)
gboolean g_utf16_ascii_equal (const gunichar2 *utf16, size_t ulen, const char *ascii, size_t alen);
gboolean g_utf16_asciiz_equal (const gunichar2 *utf16, const char *ascii);

static inline
gboolean g_ascii_equal (const char *s1, gsize len1, const char *s2, gsize len2)
{
    return len1 == len2 && (s1 == s2 || memcmp (s1, s2, len1) == 0);
}

static inline
gboolean g_asciiz_equal (const char *s1, const char *s2)
{
    return s1 == s2 || strcmp (s1, s2) == 0;
}

static inline
gboolean
g_ascii_equal_caseinsensitive (const char *s1, gsize len1, const char *s2, gsize len2)
{
    return len1 == len2 && (s1 == s2 || g_ascii_strncasecmp (s1, s2, len1) == 0);
}

static inline
gboolean
g_asciiz_equal_caseinsensitive (const char *s1, const char *s2)
{
    return s1 == s2 || g_ascii_strcasecmp (s1, s2) == 0;
}

/* FIXME: g_strcasecmp supports utf8 unicode stuff */
#ifdef _MSC_VER
#define g_strcasecmp _stricmp
#define g_strncasecmp _strnicmp
#define g_strstrip(a) g_strchug (g_strchomp (a))
#else
#define g_strcasecmp strcasecmp
#define g_ascii_strtoull strtoull
#define g_strncasecmp strncasecmp
#define g_strstrip(a) g_strchug (g_strchomp (a))
#endif
#define g_ascii_strdup strdup

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

void         g_string_printf        (GString *string, const gchar *format, ...) G_ATTR_FORMAT_PRINTF(2, 3);
void         g_string_append_printf (GString *string, const gchar *format, ...) G_ATTR_FORMAT_PRINTF(2, 3);
void         g_string_append_vprintf (GString *string, const gchar *format, va_list args);
GString     *g_string_append_unichar (GString *string, gunichar c);
GString     *g_string_append_c      (GString *string, gchar c);
GString     *g_string_append        (GString *string, const gchar *val);
GString     *g_string_append_len    (GString *string, const gchar *val, gssize len);
GString     *g_string_truncate      (GString *string, gsize len);
GString     *g_string_set_size      (GString *string, gsize len);

#define g_string_sprintfa g_string_append_printf

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
GSList *g_slist_find_custom   (GSList	     *list,
			       gconstpointer  data,
			       GCompareFunc   func);
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
gint    g_slist_index	      (GSList        *list,
			       gconstpointer  data);
GSList *g_slist_nth	      (GSList	     *list,
			       guint	      n);
gpointer g_slist_nth_data     (GSList	     *list,
			       guint	      n);

#define g_slist_next(slist) ((slist) ? (((GSList *) (slist))->next) : NULL)


typedef struct _GList GList;
struct _GList {
  gpointer data;
  GList *next;
  GList *prev;
};

#define g_list_next(list) ((list) ? (((GList *) (list))->next) : NULL)
#define g_list_previous(list) ((list) ? (((GList *) (list))->prev) : NULL)

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
GList *g_list_find_custom   (GList	   *list,
			     gconstpointer  data,
			     GCompareFunc   func);
GList *g_list_remove        (GList         *list,
			     gconstpointer  data);
GList *g_list_remove_all    (GList         *list,
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
 * Hashtables
 */
typedef struct _GHashTable GHashTable;
typedef struct _GHashTableIter GHashTableIter;

/* Private, but needed for stack allocation */
struct _GHashTableIter
{
	gpointer dummy [8];
};

G_EXTERN_C // Used by MonoPosixHelper or MonoSupportW, at least.
GHashTable     *g_hash_table_new             (GHashFunc hash_func, GEqualFunc key_equal_func);
GHashTable     *g_hash_table_new_full        (GHashFunc hash_func, GEqualFunc key_equal_func,
					      GDestroyNotify key_destroy_func, GDestroyNotify value_destroy_func);
G_EXTERN_C // Used by MonoPosixHelper or MonoSupportW, at least.
void            g_hash_table_insert_replace  (GHashTable *hash, gpointer key, gpointer value, gboolean replace);
guint           g_hash_table_size            (GHashTable *hash);
GList          *g_hash_table_get_keys        (GHashTable *hash);
GList          *g_hash_table_get_values      (GHashTable *hash);
gboolean        g_hash_table_contains (GHashTable *hash, gconstpointer key);
G_EXTERN_C // Used by MonoPosixHelper or MonoSupportW, at least.
gpointer        g_hash_table_lookup          (GHashTable *hash, gconstpointer key);
gboolean        g_hash_table_lookup_extended (GHashTable *hash, gconstpointer key, gpointer *orig_key, gpointer *value);
G_EXTERN_C // Used by MonoPosixHelper or MonoSupportW, at least.
void            g_hash_table_foreach         (GHashTable *hash, GHFunc func, gpointer user_data);
gpointer        g_hash_table_find            (GHashTable *hash, GHRFunc predicate, gpointer user_data);
G_EXTERN_C // Used by MonoPosixHelper or MonoSupportW, at least.
gboolean        g_hash_table_remove          (GHashTable *hash, gconstpointer key);
gboolean        g_hash_table_steal           (GHashTable *hash, gconstpointer key);
void            g_hash_table_remove_all      (GHashTable *hash);
guint           g_hash_table_foreach_remove  (GHashTable *hash, GHRFunc func, gpointer user_data);
guint           g_hash_table_foreach_steal   (GHashTable *hash, GHRFunc func, gpointer user_data);
G_EXTERN_C // Used by MonoPosixHelper or MonoSupportW, at least.
void            g_hash_table_destroy         (GHashTable *hash);
void            g_hash_table_print_stats     (GHashTable *table);

void            g_hash_table_iter_init       (GHashTableIter *iter, GHashTable *hash_table);
gboolean        g_hash_table_iter_next       (GHashTableIter *iter, gpointer *key, gpointer *value);

guint           g_spaced_primes_closest      (guint x);

#define g_hash_table_insert(h,k,v)    g_hash_table_insert_replace ((h),(k),(v),FALSE)
#define g_hash_table_replace(h,k,v)   g_hash_table_insert_replace ((h),(k),(v),TRUE)
#define g_hash_table_add(h,k)         g_hash_table_insert_replace ((h),(k),(k),TRUE)

G_EXTERN_C // Used by MonoPosixHelper or MonoSupportW, at least.
gboolean g_direct_equal (gconstpointer v1, gconstpointer v2);
G_EXTERN_C // Used by MonoPosixHelper or MonoSupportW, at least.
guint    g_direct_hash  (gconstpointer v1);
gboolean g_int_equal    (gconstpointer v1, gconstpointer v2);
guint    g_int_hash     (gconstpointer v1);
gboolean g_str_equal    (gconstpointer v1, gconstpointer v2);
guint    g_str_hash     (gconstpointer v1);

/*
 * ByteArray
 */

typedef struct _GByteArray GByteArray;
struct _GByteArray {
	guint8 *data;
	gint len;
};

GByteArray *g_byte_array_new    (void);
GByteArray* g_byte_array_append (GByteArray *array, const guint8 *data, guint len);
guint8*  g_byte_array_free      (GByteArray *array, gboolean free_segment);
void     g_byte_array_set_size  (GByteArray *array, gint length);

/*
 * Array
 */

typedef struct _GArray GArray;
struct _GArray {
	gchar *data;
	gint len;
};

GArray *g_array_new               (gboolean zero_terminated, gboolean clear_, guint element_size);
GArray *g_array_sized_new         (gboolean zero_terminated, gboolean clear_, guint element_size, guint reserved_size);
gchar*  g_array_free              (GArray *array, gboolean free_segment);
GArray *g_array_append_vals       (GArray *array, gconstpointer data, guint len);
GArray* g_array_insert_vals       (GArray *array, guint index_, gconstpointer data, guint len);
GArray* g_array_remove_index      (GArray *array, guint index_);
GArray* g_array_remove_index_fast (GArray *array, guint index_);
void    g_array_set_size          (GArray *array, gint length);

#define g_array_append_val(a,v)   (g_array_append_vals((a),&(v),1))
#define g_array_insert_val(a,i,v) (g_array_insert_vals((a),(i),&(v),1))
#define g_array_index(a,t,i)      *(t*)(((a)->data) + sizeof(t) * (i))
//FIXME previous missing parens

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
gpointer   g_ptr_array_remove_index_fast  (GPtrArray *array, guint index);
void       g_ptr_array_sort               (GPtrArray *array, GCompareFunc compare_func);
void       g_ptr_array_set_size           (GPtrArray *array, gint length);
gpointer  *g_ptr_array_free               (GPtrArray *array, gboolean free_seg);
void       g_ptr_array_foreach            (GPtrArray *array, GFunc func, gpointer user_data);
guint      g_ptr_array_capacity           (GPtrArray *array);
gboolean   g_ptr_array_find               (GPtrArray *array, gconstpointer needle, guint *index);
#define    g_ptr_array_index(array,index) (array)->pdata[(index)]
//FIXME previous missing parens

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
void     g_queue_push_tail (GQueue   *queue,
			    gpointer  data);
gboolean g_queue_is_empty  (GQueue   *queue);
GQueue  *g_queue_new       (void);
void     g_queue_free      (GQueue   *queue);
void     g_queue_foreach   (GQueue   *queue, GFunc func, gpointer user_data);

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

G_ENUM_FUNCTIONS (GLogLevelFlags)

gint           g_printv               (const gchar *format, va_list args);
void           g_print                (const gchar *format, ...);
void           g_printerr             (const gchar *format, ...);
GLogLevelFlags g_log_set_always_fatal (GLogLevelFlags fatal_mask);
GLogLevelFlags g_log_set_fatal_mask   (const gchar *log_domain, GLogLevelFlags fatal_mask);
void           g_logv                 (const gchar *log_domain, GLogLevelFlags log_level, const gchar *format, va_list args);
G_EXTERN_C // Used by MonoPosixHelper or MonoSupportW, at least.
void           g_log                  (const gchar *log_domain, GLogLevelFlags log_level, const gchar *format, ...);
void           g_log_disabled         (const gchar *log_domain, GLogLevelFlags log_level, const char *file, int line);
G_EXTERN_C // Used by MonoPosixHelper or MonoSupportW, at least.
void           g_assertion_message    (const gchar *format, ...) G_GNUC_NORETURN;
void           mono_assertion_message_disabled  (const char *file, int line) G_GNUC_NORETURN;
void           mono_assertion_message  (const char *file, int line, const char *condition) G_GNUC_NORETURN;
void           mono_assertion_message_unreachable (const char *file, int line) G_GNUC_NORETURN;
const char *   g_get_assertion_message (void);

#ifndef DISABLE_ASSERT_MESSAGES
/* The for (;;) tells gc thats g_error () doesn't return, avoiding warnings */
#define g_error(...) do { g_log (G_LOG_DOMAIN, G_LOG_LEVEL_ERROR, __VA_ARGS__); for (;;); } while (0)
#define g_critical(...) g_log (G_LOG_DOMAIN, G_LOG_LEVEL_CRITICAL, __VA_ARGS__)
#define g_warning(...)  g_log (G_LOG_DOMAIN, G_LOG_LEVEL_WARNING, __VA_ARGS__)
#define g_message(...)  g_log (G_LOG_DOMAIN, G_LOG_LEVEL_MESSAGE, __VA_ARGS__)
#define g_debug(...)    g_log (G_LOG_DOMAIN, G_LOG_LEVEL_DEBUG, __VA_ARGS__)
#else
#define g_error(...)    do { g_log_disabled (G_LOG_DOMAIN, G_LOG_LEVEL_ERROR, __FILE__, __LINE__); for (;;); } while (0)
#define g_critical(...) g_log_disabled (G_LOG_DOMAIN, G_LOG_LEVEL_CRITICAL, __FILE__, __LINE__)
#define g_warning(...)  g_log_disabled (G_LOG_DOMAIN, G_LOG_LEVEL_WARNING, __FILE__, __LINE__)
#define g_message(...)  g_log_disabled (G_LOG_DOMAIN, G_LOG_LEVEL_MESSAGE, __FILE__, __LINE__)
#define g_debug(...)    g_log_disabled (G_LOG_DOMAIN, G_LOG_LEVEL_DEBUG, __FILE__, __LINE__)
#endif

typedef void (*GLogFunc) (const gchar *log_domain, GLogLevelFlags log_level, const gchar *message, gpointer user_data);
typedef void (*GPrintFunc) (const gchar *string);
typedef void (*GAbortFunc) (void);

void       g_assertion_disable_global   (GAbortFunc func);
void       g_assert_abort               (void);
void       g_log_default_handler     (const gchar *log_domain, GLogLevelFlags log_level, const gchar *message, gpointer unused_data);
GLogFunc   g_log_set_default_handler (GLogFunc log_func, gpointer user_data);
GPrintFunc g_set_print_handler       (GPrintFunc func);
GPrintFunc g_set_printerr_handler    (GPrintFunc func);
/*
 * Conversions
 */

gpointer g_convert_error_quark(void);

#ifndef MAX
#define MAX(a,b) (((a)>(b)) ? (a) : (b))
#endif

#ifndef MIN
#define MIN(a,b) (((a)<(b)) ? (a) : (b))
#endif

#ifndef CLAMP
#define CLAMP(a,low,high) (((a) < (low)) ? (low) : (((a) > (high)) ? (high) : (a)))
#endif

#if defined(__GNUC__) && (__GNUC__ > 2)
#define G_LIKELY(expr) (__builtin_expect ((expr) != 0, 1))
#define G_UNLIKELY(expr) (__builtin_expect ((expr) != 0, 0))
#else
#define G_LIKELY(x) (x)
#define G_UNLIKELY(x) (x)
#endif

#if defined(_MSC_VER)
#define  eg_unreachable() __assume(0)
#elif defined(__GNUC__) && ((__GNUC__ > 4) || (__GNUC__ == 4 && (__GNUC_MINOR__ >= 5)))
#define  eg_unreachable() __builtin_unreachable()
#else
#define  eg_unreachable()
#endif

/* g_assert is a boolean expression; the precise value is not preserved, just true or false. */
#ifdef DISABLE_ASSERT_MESSAGES
// This is smaller than the equivalent mono_assertion_message (..."disabled");
#define g_assert(x) (G_LIKELY((x)) ? 1 : (mono_assertion_message_disabled (__FILE__, __LINE__), 0))
#else
#define g_assert(x) (G_LIKELY((x)) ? 1 : (mono_assertion_message (__FILE__, __LINE__, #x), 0))
#endif

#ifdef __cplusplus
#define g_static_assert(x) static_assert (x, "")
#else
#define g_static_assert(x) g_assert (x)
#endif

#define  g_assert_not_reached() G_STMT_START { mono_assertion_message_unreachable (__FILE__, __LINE__); eg_unreachable(); } G_STMT_END

/* f is format -- like printf and scanf
 * Where you might have said:
 * 	if (!(expr))
 * 		g_error("%s invalid bar:%d", __func__, bar)
 *
 * You can say:
 * 	g_assertf(expr, "bar:%d", bar);
 *
 * The usual assertion text of file/line/expr/newline are builtin, and __func__.
 *
 * g_assertf is a boolean expression -- the precise value is not preserved, just true or false.
 *
 * Other than expr, the parameters are not evaluated unless expr is false.
 *
 * format must be a string literal, in order to be concatenated.
 * If this is too restrictive, g_error remains.
 */
#ifdef DISABLE_ASSERT_MESSAGES
#define g_assertf(x, format, ...) (G_LIKELY((x)) ? 1 : (mono_assertion_message_disabled (__FILE__, __LINE__), 0))
#elif defined(_MSC_VER) && (_MSC_VER < 1910)
#define g_assertf(x, format, ...) (G_LIKELY((x)) ? 1 : (g_assertion_message ("* Assertion at %s:%d, condition `%s' not met, function:%s, " format "\n", __FILE__, __LINE__, #x, __func__, __VA_ARGS__), 0))
#else
#define g_assertf(x, format, ...) (G_LIKELY((x)) ? 1 : (g_assertion_message ("* Assertion at %s:%d, condition `%s' not met, function:%s, " format "\n", __FILE__, __LINE__, #x, __func__, ##__VA_ARGS__), 0))
#endif

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
	G_CONVERT_ERROR_NOT_ABSOLUTE_PATH,
	G_CONVERT_ERROR_NO_MEMORY
} GConvertError;

gint       g_unichar_to_utf8 (gunichar c, gchar *outbuf);
gunichar  *g_utf8_to_ucs4_fast (const gchar *str, glong len, glong *items_written);
gunichar  *g_utf8_to_ucs4 (const gchar *str, glong len, glong *items_read, glong *items_written, GError **err);
G_EXTERN_C // Used by libtest, at least.
gunichar2 *g_utf8_to_utf16 (const gchar *str, glong len, glong *items_read, glong *items_written, GError **err);
gunichar2 *g_utf8_to_utf16be (const gchar *str, glong len, glong *items_read, glong *items_written, GError **err);
gunichar2 *g_utf8_to_utf16le (const gchar *str, glong len, glong *items_read, glong *items_written, GError **err);
gunichar2 *eg_utf8_to_utf16_with_nuls (const gchar *str, glong len, glong *items_read, glong *items_written, GError **err);
gunichar2 *eg_wtf8_to_utf16 (const gchar *str, glong len, glong *items_read, glong *items_written, GError **err);
G_EXTERN_C // Used by libtest, at least.
gchar     *g_utf16_to_utf8 (const gunichar2 *str, glong len, glong *items_read, glong *items_written, GError **err);
gchar     *g_utf16le_to_utf8 (const gunichar2 *str, glong len, glong *items_read, glong *items_written, GError **err);
gchar     *g_utf16be_to_utf8 (const gunichar2 *str, glong len, glong *items_read, glong *items_written, GError **err);
gunichar  *g_utf16_to_ucs4 (const gunichar2 *str, glong len, glong *items_read, glong *items_written, GError **err);
gchar     *g_ucs4_to_utf8  (const gunichar *str, glong len, glong *items_read, glong *items_written, GError **err);
gunichar2 *g_ucs4_to_utf16 (const gunichar *str, glong len, glong *items_read, glong *items_written, GError **err);
size_t     g_utf16_len     (const gunichar2 *);

#define u8to16(str) g_utf8_to_utf16(str, (glong)strlen(str), NULL, NULL, NULL)

#ifdef G_OS_WIN32
#define u16to8(str) g_utf16_to_utf8((gunichar2 *) (str), (glong)wcslen((wchar_t *) (str)), NULL, NULL, NULL)
#else
#define u16to8(str) g_utf16_to_utf8(str, (glong)strlen(str), NULL, NULL, NULL)
#endif

typedef gpointer (*GCustomAllocator) (gsize req_size, gpointer custom_alloc_data);

typedef struct {
	gpointer buffer;
	gsize buffer_size;
	gsize req_buffer_size;
} GFixedBufferCustomAllocatorData;

gpointer
g_fixed_buffer_custom_allocator (gsize req_size, gpointer custom_alloc_data);

gunichar2 *g_utf8_to_utf16_custom_alloc (const gchar *str, glong len, glong *items_read, glong *items_written, GCustomAllocator custom_alloc_func, gpointer custom_alloc_data, GError **err);
gunichar2 *g_utf8_to_utf16be_custom_alloc (const gchar *str, glong len, glong *items_read, glong *items_written, GCustomAllocator custom_alloc_func, gpointer custom_alloc_data, GError **err);
gunichar2 *g_utf8_to_utf16le_custom_alloc (const gchar *str, glong len, glong *items_read, glong *items_written, GCustomAllocator custom_alloc_func, gpointer custom_alloc_data, GError **err);
gchar *g_utf16_to_utf8_custom_alloc (const gunichar2 *str, glong len, glong *items_read, glong *items_written, GCustomAllocator custom_alloc_func, gpointer custom_alloc_data, GError **err);

/*
 * Path
 */
gchar  *g_build_path           (const gchar *separator, const gchar *first_element, ...);
#define g_build_filename(x, ...) g_build_path(G_DIR_SEPARATOR_S, x, __VA_ARGS__)
gchar  *g_path_get_dirname     (const gchar *filename);
gchar  *g_path_get_basename    (const char *filename);
gchar  *g_find_program_in_path (const gchar *program);
gchar  *g_get_current_dir      (void);
gboolean g_path_is_absolute    (const char *filename);

const gchar *g_get_tmp_dir     (void);

gboolean g_ensure_directory_exists (const gchar *filename);

#ifndef G_OS_WIN32 // Spawn could be implemented but is not.

int eg_getdtablesize (void);

#if !defined (HAVE_FORK) || !defined (HAVE_EXECVE)

#define HAVE_G_SPAWN 0

#else

#define HAVE_G_SPAWN 1


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

typedef void (*GSpawnChildSetupFunc) (gpointer user_data);

gboolean g_spawn_async_with_pipes  (const gchar *working_directory, gchar **argv, gchar **envp, GSpawnFlags flags, GSpawnChildSetupFunc child_setup,
				gpointer user_data, GPid *child_pid, gint *standard_input, gint *standard_output, gint *standard_error, GError **gerror);

#endif
#endif

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
void g_usleep (gulong microseconds);

/*
 * File
 */

gpointer g_file_error_quark (void);

#define G_FILE_ERROR g_file_error_quark ()

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

G_ENUM_FUNCTIONS (GFileTest)

gboolean   g_file_set_contents (const gchar *filename, const gchar *contents, gssize length, GError **gerror);
gboolean   g_file_get_contents (const gchar *filename, gchar **contents, gsize *length, GError **gerror);
GFileError g_file_error_from_errno (gint err_no);
gint       g_file_open_tmp (const gchar *tmpl, gchar **name_used, GError **gerror);
gboolean   g_file_test (const gchar *filename, GFileTest test);

#ifdef G_OS_WIN32
#define g_open _open
#else
#define g_open open
#endif
#define g_rename rename
#define g_stat stat
#ifdef G_OS_WIN32
#define g_access _access
#else
#define g_access access
#endif
#ifdef G_OS_WIN32
#define g_mktemp _mktemp
#else
#define g_mktemp mktemp
#endif
#ifdef G_OS_WIN32
#define g_unlink _unlink
#else
#define g_unlink unlink
#endif
#ifdef G_OS_WIN32
#define g_write _write
#else
#define g_write write
#endif
#ifdef G_OS_WIN32
#define g_read(fd, buffer, buffer_size) _read(fd, buffer, (unsigned)buffer_size)
#else
#define g_read(fd, buffer, buffer_size) (int)read(fd, buffer, buffer_size)
#endif

#define g_fopen fopen
#define g_lstat lstat
#define g_rmdir rmdir
#define g_mkstemp mkstemp
#define g_ascii_isdigit isdigit
#define g_ascii_strtod strtod
#define g_ascii_isalnum isalnum

gchar *g_mkdtemp (gchar *tmpl);

/*
 * Low-level write-based printing functions
 */

static inline int
g_async_safe_fgets (char *str, int num, int handle, gboolean *newline)
{
	memset (str, 0, num);
	// Make sure we don't overwrite the last index so that we are
	// guaranteed to be NULL-terminated
	int without_padding = num - 1;
	int i=0;
	while (i < without_padding && g_read (handle, &str [i], sizeof(char))) {
		if (str [i] == '\n') {
			str [i] = '\0';
			*newline = TRUE;
		}

		if (!isprint (str [i]))
			str [i] = '\0';

		if (str [i] == '\0')
			break;

		i++;
	}

	return i;
}

static inline gint
g_async_safe_vfprintf (int handle, gchar const *format, va_list args)
{
	char print_buff [1024];
	print_buff [0] = '\0';
	g_vsnprintf (print_buff, sizeof(print_buff), format, args);
	int ret = g_write (handle, print_buff, (guint32) strlen (print_buff));

	return ret;
}

static inline gint
g_async_safe_fprintf (int handle, gchar const *format, ...)
{
	va_list args;
	va_start (args, format);
	int ret = g_async_safe_vfprintf (handle, format, args);
	va_end (args);
	return ret;
}

static inline gint
g_async_safe_vprintf (gchar const *format, va_list args)
{
	return g_async_safe_vfprintf (1, format, args);
}

static inline gint
g_async_safe_printf (gchar const *format, ...)
{
	va_list args;
	va_start (args, format);
	int ret = g_async_safe_vfprintf (1, format, args);
	va_end (args);

	return ret;
}

/*
 * Directory
 */
typedef struct _GDir GDir;
GDir        *g_dir_open (const gchar *path, guint flags, GError **gerror);
const gchar *g_dir_read_name (GDir *dir);
void         g_dir_rewind (GDir *dir);
void         g_dir_close (GDir *dir);

int          g_mkdir_with_parents (const gchar *pathname, int mode);
#define g_mkdir mkdir

/*
 * Unicode manipulation
 */
extern const guchar g_utf8_jump_table[256];

gboolean  g_utf8_validate      (const gchar *str, gssize max_len, const gchar **end);
gunichar  g_utf8_get_char_validated (const gchar *str, gssize max_len);
#define   g_utf8_next_char(p)  ((p) + g_utf8_jump_table[(guchar)(*p)])
gunichar  g_utf8_get_char      (const gchar *src);
glong     g_utf8_strlen        (const gchar *str, gssize max);
gchar    *g_utf8_offset_to_pointer (const gchar *str, glong offset);
glong     g_utf8_pointer_to_offset (const gchar *str, const gchar *pos);

/*
 * priorities
 */
#define G_PRIORITY_DEFAULT 0
#define G_PRIORITY_DEFAULT_IDLE 200

#define GUINT16_SWAP_LE_BE_CONSTANT(x) ((((guint16) x) >> 8) | ((((guint16) x) << 8)))

#define GUINT16_SWAP_LE_BE(x) ((guint16) (((guint16) x) >> 8) | ((((guint16)(x)) & 0xff) << 8))
#define GUINT32_SWAP_LE_BE(x) ((guint32) \
			       ( (((guint32) (x)) << 24)| \
				 ((((guint32) (x)) & 0xff0000) >> 8) | \
		                 ((((guint32) (x)) & 0xff00) << 8) | \
			         (((guint32) (x)) >> 24)) )

#define GUINT64_SWAP_LE_BE(x) ((guint64) (((guint64)(GUINT32_SWAP_LE_BE(((guint64)x) & 0xffffffff))) << 32) | \
	      	               GUINT32_SWAP_LE_BE(((guint64)x) >> 32))



#if G_BYTE_ORDER == G_LITTLE_ENDIAN
#   define GUINT64_FROM_BE(x) GUINT64_SWAP_LE_BE(x)
#   define GUINT32_FROM_BE(x) GUINT32_SWAP_LE_BE(x)
#   define GUINT16_FROM_BE(x) GUINT16_SWAP_LE_BE(x)
#   define GUINT_FROM_BE(x)   GUINT32_SWAP_LE_BE(x)
#   define GUINT64_FROM_LE(x) (x)
#   define GUINT32_FROM_LE(x) (x)
#   define GUINT16_FROM_LE(x) (x)
#   define GUINT_FROM_LE(x)   (x)
#   define GUINT64_TO_BE(x)   GUINT64_SWAP_LE_BE(x)
#   define GUINT32_TO_BE(x)   GUINT32_SWAP_LE_BE(x)
#   define GUINT16_TO_BE(x)   GUINT16_SWAP_LE_BE(x)
#   define GUINT_TO_BE(x)     GUINT32_SWAP_LE_BE(x)
#   define GUINT64_TO_LE(x)   (x)
#   define GUINT32_TO_LE(x)   (x)
#   define GUINT16_TO_LE(x)   (x)
#   define GUINT_TO_LE(x)     (x)
#else
#   define GUINT64_FROM_BE(x) (x)
#   define GUINT32_FROM_BE(x) (x)
#   define GUINT16_FROM_BE(x) (x)
#   define GUINT_FROM_BE(x)   (x)
#   define GUINT64_FROM_LE(x) GUINT64_SWAP_LE_BE(x)
#   define GUINT32_FROM_LE(x) GUINT32_SWAP_LE_BE(x)
#   define GUINT16_FROM_LE(x) GUINT16_SWAP_LE_BE(x)
#   define GUINT_FROM_LE(x)   GUINT32_SWAP_LE_BE(x)
#   define GUINT64_TO_BE(x)   (x)
#   define GUINT32_TO_BE(x)   (x)
#   define GUINT16_TO_BE(x)   (x)
#   define GUINT_TO_BE(x)     (x)
#   define GUINT64_TO_LE(x)   GUINT64_SWAP_LE_BE(x)
#   define GUINT32_TO_LE(x)   GUINT32_SWAP_LE_BE(x)
#   define GUINT16_TO_LE(x)   GUINT16_SWAP_LE_BE(x)
#   define GUINT_TO_LE(x)     GUINT32_SWAP_LE_BE(x)
#endif

#define GINT64_FROM_BE(x)   (GUINT64_TO_BE (x))
#define GINT32_FROM_BE(x)   (GUINT32_TO_BE (x))
#define GINT16_FROM_BE(x)   (GUINT16_TO_BE (x))
#define GINT64_FROM_LE(x)   (GUINT64_TO_LE (x))
#define GINT32_FROM_LE(x)   (GUINT32_TO_LE (x))
#define GINT16_FROM_LE(x)   (GUINT16_TO_LE (x))

#define _EGLIB_MAJOR  2
#define _EGLIB_MIDDLE 4
#define _EGLIB_MINOR  0

#define GLIB_CHECK_VERSION(a,b,c) ((a < _EGLIB_MAJOR) || (a == _EGLIB_MAJOR && (b < _EGLIB_MIDDLE || (b == _EGLIB_MIDDLE && c <= _EGLIB_MINOR))))

#define G_HAVE_API_SUPPORT(x) (x)
#define G_UNSUPPORTED_API "%s:%d: '%s' not supported.", __FILE__, __LINE__
#define g_unsupported_api(name) G_STMT_START { g_debug (G_UNSUPPORTED_API, name); } G_STMT_END

#if _WIN32
// g_free the result
// No MAX_PATH limit.
gboolean
mono_get_module_filename (gpointer mod, gunichar2 **pstr, guint32 *plength);

// g_free the result
// No MAX_PATH limit.
gboolean
mono_get_module_filename_ex (gpointer process, gpointer mod, gunichar2 **pstr, guint32 *plength);

// g_free the result
// No MAX_PATH limit.
gboolean
mono_get_module_basename (gpointer process, gpointer mod, gunichar2 **pstr, guint32 *plength);

// g_free the result
// No MAX_PATH limit.
gboolean
mono_get_current_directory (gunichar2 **pstr, guint32 *plength);
#endif

G_END_DECLS // FIXME: There is more extern C than there should be.

static inline
void
mono_qsort (void* base, size_t num, size_t size, int (*compare)(const void*, const void*))
{
	g_assert (compare);
	g_assert (size);
	if (num < 2 || !size || !base)
		return;
	qsort (base, num, size, compare);
}

#define MONO_DECL_CALLBACK(prefix, ret, name, sig) ret (*name) sig;
#define MONO_INIT_CALLBACK(prefix, ret, name, sig) prefix ## _ ## name,

// For each allocator; i.e. returning gpointer that needs to be cast.
// Macros do not recurse, so naming function and macro the same is ok.
// However these are also already macros.
#undef g_malloc
#undef g_realloc
#undef g_malloc0
#undef g_calloc
#undef g_try_malloc
#undef g_try_realloc
#undef g_memdup
#define g_malloc(x) (g_cast (monoeg_malloc (x)))
#define g_realloc(obj, size) (g_cast (monoeg_realloc ((obj), (size))))
#define g_malloc0(x) (g_cast (monoeg_malloc0 (x)))
#define g_calloc(x, y) (g_cast (monoeg_g_calloc ((x), (y))))
#define g_try_malloc(x) (g_cast (monoeg_try_malloc (x)))
#define g_try_realloc(obj, size) (g_cast (monoeg_try_realloc ((obj), (size))))
#define g_memdup(mem, size) (g_cast (monoeg_g_memdup ((mem), (size))))

/*
 * Clock Nanosleep
 */

#ifdef HAVE_CLOCK_NANOSLEEP
gint
g_clock_nanosleep (clockid_t clockid, gint flags, const struct timespec *request, struct timespec *remain);
#endif

//#define ENABLE_CHECKED_CASTS
#ifdef ENABLE_CHECKED_CASTS

#define __CAST_PTRTYPE_TO_STYPE(src,dest,min_v,max_v) \
static inline dest \
__cast_ptr_##src##_to_stype_##dest (src v) \
{ \
	if ((gssize)(v) < min_v) \
		/* underflow */ \
		; \
	if ((gssize)(v) > max_v) \
		/* overflow */ \
		; \
	return ((dest)(gssize)(v)); \
}

#define __CAST_PTRTYPE_TO_UTYPE(src,dest,max_v) \
static inline dest \
__cast_ptr_##src##_to_utype_##dest (src v) \
{ \
	if ((gsize)(v) > max_v) \
		/* overflow */ \
		; \
	return ((dest)(gsize)(v)); \
}

#define __CAST_STYPE_TO_STYPE(src,dest,min_v,max_v) \
static inline dest \
__cast_##src##_to_##dest (src v) \
{ \
	if (v < min_v) \
		/* underflow */ \
		; \
	if (v > max_v) \
		/* overflow */ \
		; \
	return (dest)(v); \
}

#define __CAST_UTYPE_TO_UTYPE(src,dest,max_v) \
static inline dest \
__cast_##src##_to_##dest (src v) \
{ \
	if (v > max_v) \
		/* overflow */ \
		; \
	return (dest)(v); \
}

#define __CAST_STYPE_TO_UTYPE(src,dest,max_v) \
static inline dest \
__cast_##src##_to_##dest (src v) \
{ \
	if (v < 0) \
		/* underflow */ \
		; \
	if (v > max_v) \
		/* overflow */ \
		; \
	return (dest)(v); \
}

#define __CAST_UTYPE_TO_STYPE(src,dest,min_v,max_v) \
static inline dest \
__cast_##src##_to_##dest (src v) \
{ \
	if (v > max_v) \
		/* overflow */ \
		; \
	return (dest)(v); \
}

__CAST_PTRTYPE_TO_STYPE(gpointer, gint64, INT64_MIN, INT64_MAX)
__CAST_PTRTYPE_TO_UTYPE(gpointer, guint64, UINT64_MAX)
__CAST_PTRTYPE_TO_STYPE(gpointer, gint32, INT32_MIN, INT32_MAX)
__CAST_PTRTYPE_TO_UTYPE(gpointer, guint32, UINT32_MAX)
__CAST_PTRTYPE_TO_STYPE(gpointer, gint16, INT16_MIN, INT16_MAX)
__CAST_PTRTYPE_TO_UTYPE(gpointer, guint16, UINT16_MAX)
__CAST_PTRTYPE_TO_STYPE(gpointer, gint8, INT8_MIN, INT8_MAX)
__CAST_PTRTYPE_TO_UTYPE(gpointer, guint8, UINT8_MAX)

__CAST_PTRTYPE_TO_STYPE(gpointer, glong, LONG_MIN, LONG_MAX)
__CAST_PTRTYPE_TO_UTYPE(gpointer, gulong, ULONG_MAX)
__CAST_PTRTYPE_TO_STYPE(gpointer, gint, INT_MIN, INT_MAX)
__CAST_PTRTYPE_TO_UTYPE(gpointer, guint, UINT_MAX)

__CAST_PTRTYPE_TO_STYPE(gintptr, gint32, INT32_MIN, INT32_MAX)
__CAST_PTRTYPE_TO_UTYPE(gintptr, guint32, UINT32_MAX)
__CAST_PTRTYPE_TO_STYPE(gintptr, gint16, INT16_MIN, INT16_MAX)
__CAST_PTRTYPE_TO_UTYPE(gintptr, guint16, UINT16_MAX)

__CAST_PTRTYPE_TO_STYPE(gintptr, glong, LONG_MIN, LONG_MAX)
__CAST_PTRTYPE_TO_UTYPE(gintptr, gulong, ULONG_MAX)
__CAST_PTRTYPE_TO_STYPE(gintptr, gint, INT_MIN, INT_MAX)
__CAST_PTRTYPE_TO_UTYPE(gintptr, guint, UINT_MAX)

__CAST_PTRTYPE_TO_UTYPE(gintptr, gsize, SIZE_MAX)

__CAST_PTRTYPE_TO_STYPE(guintptr, gint32, INT32_MIN, INT32_MAX)
__CAST_PTRTYPE_TO_UTYPE(guintptr, guint32, UINT32_MAX)
__CAST_PTRTYPE_TO_STYPE(guintptr, gint16, INT16_MIN, INT16_MAX)
__CAST_PTRTYPE_TO_UTYPE(guintptr, guint16, UINT16_MAX)

__CAST_PTRTYPE_TO_STYPE(guintptr, glong, LONG_MIN, LONG_MAX)
__CAST_PTRTYPE_TO_UTYPE(guintptr, gulong, ULONG_MAX)
__CAST_PTRTYPE_TO_STYPE(guintptr, gint, INT_MIN, INT_MAX)
__CAST_PTRTYPE_TO_UTYPE(guintptr, guint, UINT_MAX)

__CAST_PTRTYPE_TO_STYPE(gptrdiff, gint32, INT32_MIN, INT32_MAX)
__CAST_PTRTYPE_TO_UTYPE(gptrdiff, guint32, UINT32_MAX)
__CAST_PTRTYPE_TO_STYPE(gptrdiff, gint16, INT16_MIN, INT16_MAX)
__CAST_PTRTYPE_TO_UTYPE(gptrdiff, guint16, UINT16_MAX)
__CAST_PTRTYPE_TO_STYPE(gptrdiff, gint8, INT8_MIN, INT8_MAX)
__CAST_PTRTYPE_TO_UTYPE(gptrdiff, guint8, UINT8_MAX)

__CAST_PTRTYPE_TO_STYPE(gptrdiff, glong, LONG_MIN, LONG_MAX)
__CAST_PTRTYPE_TO_UTYPE(gptrdiff, gulong, ULONG_MAX)
__CAST_PTRTYPE_TO_STYPE(gptrdiff, gint, INT_MIN, INT_MAX)
__CAST_PTRTYPE_TO_UTYPE(gptrdiff, guint, UINT_MAX)

__CAST_UTYPE_TO_STYPE(gsize, gint32, INT32_MIN, INT32_MAX)
__CAST_UTYPE_TO_UTYPE(gsize, guint32, UINT32_MAX)

__CAST_UTYPE_TO_STYPE(gsize, gint, INT_MIN, INT_MAX)
__CAST_UTYPE_TO_UTYPE(gsize, guint, UINT_MAX)

__CAST_STYPE_TO_STYPE(gssize, gint32, INT32_MIN, INT32_MAX)
__CAST_STYPE_TO_UTYPE(gssize, guint32, UINT32_MAX)

__CAST_STYPE_TO_STYPE(gssize, gint, INT_MIN, INT_MAX)
__CAST_STYPE_TO_UTYPE(gssize, guint, UINT_MAX)

__CAST_STYPE_TO_UTYPE(gssize, gsize, SIZE_MAX)
__CAST_UTYPE_TO_STYPE(gsize, gssize, PTRDIFF_MIN, PTRDIFF_MAX)

__CAST_STYPE_TO_UTYPE(glong, gulong, ULONG_MAX)
__CAST_UTYPE_TO_STYPE(gulong, glong, LONG_MIN, LONG_MAX)

__CAST_STYPE_TO_STYPE(gdouble, gint64, INT64_MIN, INT64_MAX)
__CAST_STYPE_TO_UTYPE(gdouble, guint64, UINT64_MAX)
__CAST_STYPE_TO_STYPE(gdouble, gint32, INT32_MIN, INT32_MAX)
__CAST_STYPE_TO_UTYPE(gdouble, guint32, UINT32_MAX)

__CAST_STYPE_TO_UTYPE(gdouble, gsize, SIZE_MAX)
__CAST_STYPE_TO_STYPE(gdouble, gint, INT_MIN, INT_MAX)
__CAST_STYPE_TO_UTYPE(gdouble, guint, UINT_MAX)

__CAST_STYPE_TO_UTYPE(gint64, guint64, UINT64_MAX)
__CAST_STYPE_TO_STYPE(gint64, gint32, INT32_MIN, INT32_MAX)
__CAST_STYPE_TO_UTYPE(gint64, guint32, UINT32_MAX)
__CAST_STYPE_TO_STYPE(gint64, gint16, INT16_MIN, INT16_MAX)
__CAST_STYPE_TO_UTYPE(gint64, guint16, UINT16_MAX)
__CAST_STYPE_TO_STYPE(gint64, gint8, INT8_MIN, INT8_MAX)
__CAST_STYPE_TO_UTYPE(gint64, guint8, UINT8_MAX)

__CAST_STYPE_TO_STYPE(gint64, gssize, PTRDIFF_MIN, PTRDIFF_MAX)
__CAST_STYPE_TO_UTYPE(gint64, gsize, SIZE_MAX)
__CAST_STYPE_TO_STYPE(gint64, glong, LONG_MIN, LONG_MAX)
__CAST_STYPE_TO_UTYPE(gint64, gulong, ULONG_MAX)
__CAST_STYPE_TO_STYPE(gint64, gint, INT_MIN, INT_MAX)
__CAST_STYPE_TO_UTYPE(gint64, guint, UINT_MAX)

__CAST_UTYPE_TO_STYPE(guint64, gint64, INT64_MIN, INT64_MAX)
__CAST_UTYPE_TO_STYPE(guint64, gint32, INT32_MIN, INT32_MAX)
__CAST_UTYPE_TO_UTYPE(guint64, guint32, UINT32_MAX)
__CAST_UTYPE_TO_STYPE(guint64, gint16, INT16_MIN, INT16_MAX)
__CAST_UTYPE_TO_UTYPE(guint64, guint16, UINT16_MAX)
__CAST_UTYPE_TO_STYPE(guint64, gint8, INT8_MIN, INT8_MAX)
__CAST_UTYPE_TO_UTYPE(guint64, guint8, UINT8_MAX)

__CAST_UTYPE_TO_STYPE(guint64, gssize, PTRDIFF_MIN, PTRDIFF_MAX)
__CAST_UTYPE_TO_UTYPE(guint64, gsize, SIZE_MAX)
__CAST_UTYPE_TO_STYPE(guint64, glong, LONG_MIN, LONG_MAX)
__CAST_UTYPE_TO_UTYPE(guint64, gulong, ULONG_MAX)
__CAST_UTYPE_TO_STYPE(guint64, gint, INT_MIN, INT_MAX)
__CAST_UTYPE_TO_UTYPE(guint64, guint, UINT_MAX)

__CAST_STYPE_TO_UTYPE(gint32, guint32, UINT32_MAX)
__CAST_STYPE_TO_STYPE(gint32, gint16, INT16_MIN, INT16_MAX)
__CAST_STYPE_TO_UTYPE(gint32, guint16, UINT16_MAX)
__CAST_STYPE_TO_STYPE(gint32, gint8, INT8_MIN, INT8_MAX)
__CAST_STYPE_TO_UTYPE(gint32, guint8, UINT8_MAX)

__CAST_STYPE_TO_UTYPE(gint32, guint, UINT_MAX)

__CAST_UTYPE_TO_UTYPE(guint32, guint, UINT_MAX)
__CAST_UTYPE_TO_STYPE(guint32, gint32, INT32_MIN, INT32_MAX)
__CAST_UTYPE_TO_STYPE(guint32, gint16, INT16_MIN, INT16_MAX)
__CAST_UTYPE_TO_UTYPE(guint32, guint16, UINT16_MAX)
__CAST_UTYPE_TO_STYPE(guint32, gint8, INT8_MIN, INT8_MAX)
__CAST_UTYPE_TO_UTYPE(guint32, guint8, UINT8_MAX)

__CAST_UTYPE_TO_STYPE(guint32, gint, INT_MIN, INT_MAX)
__CAST_UTYPE_TO_STYPE(guint32, gchar, CHAR_MIN, CHAR_MAX)

__CAST_STYPE_TO_STYPE(gint, gint32, INT32_MIN, INT32_MAX)
__CAST_STYPE_TO_UTYPE(gint, guint32, UINT32_MAX)
__CAST_STYPE_TO_STYPE(gint, gint16, INT16_MIN, INT16_MAX)
__CAST_STYPE_TO_UTYPE(gint, guint16, UINT16_MAX)
__CAST_STYPE_TO_STYPE(gint, gint8, INT8_MIN, INT8_MAX)
__CAST_STYPE_TO_UTYPE(gint, guint8, UINT8_MAX)
__CAST_STYPE_TO_UTYPE(gint, gsize, SIZE_MAX)

__CAST_STYPE_TO_UTYPE(gint, guint, UINT_MAX)
__CAST_STYPE_TO_UTYPE(gint, gunichar2, UINT16_MAX)
__CAST_STYPE_TO_STYPE(gint, gshort, SHRT_MIN, SHRT_MAX)
__CAST_STYPE_TO_STYPE(gint, gchar, CHAR_MIN, CHAR_MAX)

__CAST_UTYPE_TO_STYPE(guint, gint32, INT32_MIN, INT32_MAX)
__CAST_UTYPE_TO_UTYPE(guint, guint32, UINT32_MAX)
__CAST_UTYPE_TO_STYPE(guint, gint16, INT16_MIN, INT16_MAX)
__CAST_UTYPE_TO_UTYPE(guint, guint16, UINT16_MAX)
__CAST_UTYPE_TO_STYPE(guint, gint8, INT8_MIN, INT8_MAX)
__CAST_UTYPE_TO_UTYPE(guint, guint8, UINT8_MAX)

__CAST_UTYPE_TO_STYPE(guint, gint, INT_MIN, INT_MAX)
__CAST_UTYPE_TO_UTYPE(guint, gunichar2, UINT16_MAX)
__CAST_UTYPE_TO_STYPE(guint, gshort, SHRT_MIN, SHRT_MAX)
__CAST_UTYPE_TO_STYPE(guint, gchar, CHAR_MIN, CHAR_MAX)

__CAST_STYPE_TO_STYPE(gfloat, gint32, INT32_MIN, INT32_MAX)
__CAST_STYPE_TO_UTYPE(gfloat, guint32, UINT32_MAX)

__CAST_STYPE_TO_STYPE(gfloat, gint, INT_MIN, INT_MAX)
__CAST_STYPE_TO_UTYPE(gfloat, guint, UINT_MAX)

__CAST_STYPE_TO_STYPE(gint16, gint8, INT8_MIN, INT8_MAX)
__CAST_STYPE_TO_UTYPE(gint16, guint8, UINT8_MAX)

__CAST_UTYPE_TO_STYPE(guint16, gint8, INT8_MIN, INT8_MAX)
__CAST_UTYPE_TO_UTYPE(guint16, guint8, UINT8_MAX)

__CAST_UTYPE_TO_STYPE(gunichar, gint16, INT16_MIN, INT16_MAX)
__CAST_UTYPE_TO_UTYPE(gunichar, guint16, UINT16_MAX)
__CAST_UTYPE_TO_STYPE(gunichar, gint8, INT8_MIN, INT8_MAX)
__CAST_UTYPE_TO_UTYPE(gunichar, guint8, UINT8_MAX)
__CAST_UTYPE_TO_STYPE(gunichar, gchar, CHAR_MIN, CHAR_MAX)

#define G_CAST_PTRTYPE_TO_STYPE(src,dest,v) __cast_ptr_##src##_to_stype_##dest ((v))
#define G_CAST_PTRTYPE_TO_UTYPE(src,dest,v) __cast_ptr_##src##_to_utype_##dest ((v))
#define G_CAST_TYPE_TO_TYPE(src,dest,v) __cast_##src##_to_##dest ((v))

#endif

#if !defined(ENABLE_CHECKED_CASTS)

#define G_CAST_PTRTYPE_TO_STYPE(src,dest,v) ((dest)(gssize)(v))
#define G_CAST_PTRTYPE_TO_UTYPE(src,dest,v) ((dest)(gsize)(v))
#define G_CAST_TYPE_TO_TYPE(src,dest,v) ((dest)(v))

#endif

// - Pointers should only be converted to or from pointer-sized integers.
// - Any size integer can be converted to any other size integer.
// - Therefore a pointer-sized integer is the intermediary between
//   a pointer and any integer type.

#define G_CAST_STYPE_TO_PTRTYPE(src,dest,v) ((dest)(gssize)(v))
#define G_CAST_UTYPE_TO_PTRTYPE(src,dest,v) ((dest)(gsize)(v))

#define GPOINTER_TO_INT64(v)     G_CAST_PTRTYPE_TO_STYPE(gpointer, gint64, v)
#define GPOINTER_TO_UINT64(v)    G_CAST_PTRTYPE_TO_UTYPE(gpointer, guint64, v)
#define GPOINTER_TO_INT32(v)     G_CAST_PTRTYPE_TO_STYPE(gpointer, gint32, v)
#define GPOINTER_TO_UINT32(v)    G_CAST_PTRTYPE_TO_UTYPE(gpointer, guint32, v)
#define GPOINTER_TO_INT16(v)     G_CAST_PTRTYPE_TO_STYPE(gpointer, gint16, v)
#define GPOINTER_TO_UINT16(v)    G_CAST_PTRTYPE_TO_UTYPE(gpointer, guint16, v)
#define GPOINTER_TO_INT8(v)      G_CAST_PTRTYPE_TO_STYPE(gpointer, gint8, v)
#define GPOINTER_TO_UINT8(v)     G_CAST_PTRTYPE_TO_UTYPE(gpointer, guint8, v)

#define GPOINTER_TO_LONG(v)      G_CAST_PTRTYPE_TO_STYPE(gpointer, glong, v)
#define GPOINTER_TO_ULONG(v)     G_CAST_PTRTYPE_TO_UTYPE(gpointer, gulong, v)
#define GPOINTER_TO_INT(v)       G_CAST_PTRTYPE_TO_STYPE(gpointer, gint, v)
#define GPOINTER_TO_UINT(v)      G_CAST_PTRTYPE_TO_UTYPE(gpointer, guint, v)

#define GINTPTR_TO_INT32(v)      G_CAST_PTRTYPE_TO_STYPE(gintptr, gint32, v)
#define GINTPTR_TO_UINT32(v)     G_CAST_PTRTYPE_TO_UTYPE(gintptr, guint32, v)

#define GINTPTR_TO_INT16(v)      G_CAST_PTRTYPE_TO_STYPE(gintptr, gint16, v)
#define GINTPTR_TO_UINT16(v)     G_CAST_PTRTYPE_TO_UTYPE(gintptr, guint16, v)

#define GINTPTR_TO_LONG(v)       G_CAST_PTRTYPE_TO_STYPE(gintptr, glong, v)
#define GINTPTR_TO_INT(v)        G_CAST_PTRTYPE_TO_STYPE(gintptr, gint, v)
#define GINTPTR_TO_UINT(v)       G_CAST_PTRTYPE_TO_UTYPE(gintptr, guint, v)

#define GINTPTR_TO_SIZE(v)       G_CAST_PTRTYPE_TO_UTYPE(gintptr, gsize, v)

#define GUINTPTR_TO_INT32(v)     G_CAST_PTRTYPE_TO_STYPE(guintptr, gint32, v)
#define GUINTPTR_TO_UINT32(v)    G_CAST_PTRTYPE_TO_UTYPE(guintptr, guint32, v)

#define GUINTPTR_TO_INT16(v)     G_CAST_PTRTYPE_TO_STYPE(guintptr, gint16, v)
#define GUINTPTR_TO_UINT16(v)    G_CAST_PTRTYPE_TO_UTYPE(guintptr, guint16, v)

#define GUINTPTR_TO_LONG(v)      G_CAST_PTRTYPE_TO_STYPE(guintptr, glong, v)
#define GUINTPTR_TO_INT(v)       G_CAST_PTRTYPE_TO_STYPE(guintptr, gint, v)
#define GUINTPTR_TO_UINT(v)      G_CAST_PTRTYPE_TO_UTYPE(guintptr, guint, v)

#define GPTRDIFF_TO_INT32(v)     G_CAST_PTRTYPE_TO_STYPE(gptrdiff, gint32, v)
#define GPTRDIFF_TO_UINT32(v)    G_CAST_PTRTYPE_TO_UTYPE(gptrdiff, guint32, v)
#define GPTRDIFF_TO_INT16(v)     G_CAST_PTRTYPE_TO_STYPE(gptrdiff, gint16, v)
#define GPTRDIFF_TO_UINT16(v)    G_CAST_PTRTYPE_TO_UTYPE(gptrdiff, guint16, v)
#define GPTRDIFF_TO_INT8(v)      G_CAST_PTRTYPE_TO_STYPE(gptrdiff, gint8, v)
#define GPTRDIFF_TO_UINT8(v)     G_CAST_PTRTYPE_TO_UTYPE(gptrdiff, guint8, v)

#define GPTRDIFF_TO_LONG(v)      G_CAST_PTRTYPE_TO_STYPE(gptrdiff, glong, v)
#define GPTRDIFF_TO_INT(v)       G_CAST_PTRTYPE_TO_STYPE(gptrdiff, gint, v)
#define GPTRDIFF_TO_UINT(v)      G_CAST_PTRTYPE_TO_UTYPE(gptrdiff, guint, v)

#define GSIZE_TO_INT32(v)        G_CAST_TYPE_TO_TYPE(gsize, gint32, v)
#define GSIZE_TO_UINT32(v)       G_CAST_TYPE_TO_TYPE(gsize, guint32, v)

#define GSIZE_TO_INT(v)          G_CAST_TYPE_TO_TYPE(gsize, gint, v)
#define GSIZE_TO_UINT(v)         G_CAST_TYPE_TO_TYPE(gsize, guint, v)

#define GSSIZE_TO_INT32(v)       G_CAST_TYPE_TO_TYPE(gssize, gint32, v)
#define GSSIZE_TO_UINT32(v)      G_CAST_TYPE_TO_TYPE(gssize, guint32, v)

#define GSSIZE_TO_INT(v)         G_CAST_TYPE_TO_TYPE(gssize, gint, v)
#define GSSIZE_TO_UINT(v)        G_CAST_TYPE_TO_TYPE(gssize, guint, v)

#define GSSIZE_TO_SIZE(v)        G_CAST_TYPE_TO_TYPE(gssize, gsize, v)
#define GSIZE_TO_SSIZE(v)        G_CAST_TYPE_TO_TYPE(gsize, gssize, v)

#define GLONG_TO_ULONG(v)        G_CAST_TYPE_TO_TYPE(glong, gulong, v)
#define GULONG_TO_LONG(v)        G_CAST_TYPE_TO_TYPE(gulong, glong, v)

#define GDOUBLE_TO_INT64(v)      G_CAST_TYPE_TO_TYPE(gdouble, gint64, v)
#define GDOUBLE_TO_UINT64(v)     G_CAST_TYPE_TO_TYPE(gdouble, guint64, v)
#define GDOUBLE_TO_INT32(v)      G_CAST_TYPE_TO_TYPE(gdouble, gint32, v)
#define GDOUBLE_TO_UINT32(v)     G_CAST_TYPE_TO_TYPE(gdouble, guint32, v)

#define GDOUBLE_TO_SIZE(v)       G_CAST_TYPE_TO_TYPE(gdouble, gsize, v)
#define GDOUBLE_TO_INT(v)        G_CAST_TYPE_TO_TYPE(gdouble, gint, v)
#define GDOUBLE_TO_UINT(v)       G_CAST_TYPE_TO_TYPE(gdouble, guint, v)

#define GINT64_TO_UINT64(v)      G_CAST_TYPE_TO_TYPE(gint64, guint64, v)
#define GINT64_TO_INT32(v)       G_CAST_TYPE_TO_TYPE(gint64, gint32, v)
#define GINT64_TO_UINT32(v)      G_CAST_TYPE_TO_TYPE(gint64, guint32,v)
#define GINT64_TO_INT16(v)       G_CAST_TYPE_TO_TYPE(gint64, gint16, v)
#define GINT64_TO_UINT16(v)      G_CAST_TYPE_TO_TYPE(gint64, guint16, v)
#define GINT64_TO_INT8(v)        G_CAST_TYPE_TO_TYPE(gint64, gint8, v)
#define GINT64_TO_UINT8(v)       G_CAST_TYPE_TO_TYPE(gint64, guint8, v)

#define GINT64_TO_INTPTR(v)      G_CAST_STYPE_TO_PTRTYPE(gint64, gintptr, v)
#define GINT64_TO_UINTPTR(v)     G_CAST_STYPE_TO_PTRTYPE(gint64, guintptr, v)
#define GINT64_TO_POINTER(v)     G_CAST_STYPE_TO_PTRTYPE(gint64, gpointer, v)
#define GINT64_TO_SSIZE(v)       G_CAST_TYPE_TO_TYPE(gint64, gssize, v)
#define GINT64_TO_SIZE(v)        G_CAST_TYPE_TO_TYPE(gint64, gsize, v)
#define GINT64_TO_LONG(v)        G_CAST_TYPE_TO_TYPE(gint64, glong, v)
#define GINT64_TO_ULONG(v)       G_CAST_TYPE_TO_TYPE(gint64, gulong, v)
#define GINT64_TO_INT(v)         G_CAST_TYPE_TO_TYPE(gint64, gint, v)
#define GINT64_TO_UINT(v)        G_CAST_TYPE_TO_TYPE(gint64, guint, v)

#define GUINT64_TO_INT64(v)      G_CAST_TYPE_TO_TYPE(guint64, gint64, v)
#define GUINT64_TO_INT32(v)      G_CAST_TYPE_TO_TYPE(guint64, gint32, v)
#define GUINT64_TO_UINT32(v)     G_CAST_TYPE_TO_TYPE(guint64, guint32, v)
#define GUINT64_TO_INT16(v)      G_CAST_TYPE_TO_TYPE(guint64, gint16, v)
#define GUINT64_TO_UINT16(v)     G_CAST_TYPE_TO_TYPE(guint64, guint16, v)
#define GUINT64_TO_INT8(v)       G_CAST_TYPE_TO_TYPE(guint64, gint8, v)
#define GUINT64_TO_UINT8(v)      G_CAST_TYPE_TO_TYPE(guint64, guint8, v)

#define GUINT64_TO_INTPTR(v)     G_CAST_UTYPE_TO_PTRTYPE(guint64, gintptr, v)
#define GUINT64_TO_UINTPTR(v)    G_CAST_UTYPE_TO_PTRTYPE(guint64, guintptr, v)
#define GUINT64_TO_POINTER(v)    G_CAST_UTYPE_TO_PTRTYPE(guint64, gpointer, v)
#define GUINT64_TO_SSIZE(v)      G_CAST_TYPE_TO_TYPE(guint64, gssize, v)
#define GUINT64_TO_SIZE(v)       G_CAST_TYPE_TO_TYPE(guint64, gsize, v)
#define GUINT64_TO_LONG(v)       G_CAST_TYPE_TO_TYPE(guint64, glong, v)
#define GUINT64_TO_ULONG(v)      G_CAST_TYPE_TO_TYPE(guint64, gulong, v)
#define GUINT64_TO_INT(v)        G_CAST_TYPE_TO_TYPE(guint64, gint, v)
#define GUINT64_TO_UINT(v)       G_CAST_TYPE_TO_TYPE(guint64, guint, v)

#define GINT32_TO_UINT32(v)      G_CAST_TYPE_TO_TYPE(gint32, guint32, v)
#define GINT32_TO_INT16(v)       G_CAST_TYPE_TO_TYPE(gint32, gint16, v)
#define GINT32_TO_UINT16(v)      G_CAST_TYPE_TO_TYPE(gint32, guint16, v)
#define GINT32_TO_INT8(v)        G_CAST_TYPE_TO_TYPE(gint32, gint8, v)
#define GINT32_TO_UINT8(v)       G_CAST_TYPE_TO_TYPE(gint32, guint8, v)

#define GINT32_TO_UINT(v)        G_CAST_TYPE_TO_TYPE(gint32, guint, v)

#define GUINT32_TO_UINT(v)       G_CAST_TYPE_TO_TYPE(guint32, guint, v)
#define GUINT32_TO_INT32(v)      G_CAST_TYPE_TO_TYPE(guint32, gint32, v)
#define GUINT32_TO_INT16(v)      G_CAST_TYPE_TO_TYPE(guint32, gint16, v)
#define GUINT32_TO_UINT16(v)     G_CAST_TYPE_TO_TYPE(guint32, guint16, v)
#define GUINT32_TO_INT8(v)       G_CAST_TYPE_TO_TYPE(guint32, gint8, v)
#define GUINT32_TO_UINT8(v)      G_CAST_TYPE_TO_TYPE(guint32, guint8, v)

#define GUINT32_TO_INT(v)        G_CAST_TYPE_TO_TYPE(guint32, gint, v)
#define GUINT32_TO_OPCODE(v)     G_CAST_TYPE_TO_TYPE(guint32, guint16, v)
#define GUINT32_TO_CHAR(v)       G_CAST_TYPE_TO_TYPE(guint32, gchar, v)

#define GINT_TO_INT32(v)         G_CAST_TYPE_TO_TYPE(gint, gint32, v)
#define GINT_TO_UINT32(v)        G_CAST_TYPE_TO_TYPE(gint, guint32, v)
#define GINT_TO_INT16(v)         G_CAST_TYPE_TO_TYPE(gint, gint16, v)
#define GINT_TO_UINT16(v)        G_CAST_TYPE_TO_TYPE(gint, guint16, v)
#define GINT_TO_INT8(v)          G_CAST_TYPE_TO_TYPE(gint, gint8, v)
#define GINT_TO_UINT8(v)         G_CAST_TYPE_TO_TYPE(gint, guint8, v)
#define GINT_TO_SIZE(v)          G_CAST_TYPE_TO_TYPE(gint, gsize, v)

#define GINT_TO_INTPTR(v)        G_CAST_STYPE_TO_PTRTYPE(gint, gintptr, v)
#define GINT_TO_UINTPTR(v)       G_CAST_STYPE_TO_PTRTYPE(gint, guintptr, v)
#define GINT_TO_POINTER(v)       G_CAST_STYPE_TO_PTRTYPE(gint, gpointer, v)
#define GINT_TO_UINT(v)          G_CAST_TYPE_TO_TYPE(gint, guint, v)
#define GINT_TO_OPCODE(v)        G_CAST_TYPE_TO_TYPE(gint, guint16, v)
#define GINT_TO_UNICHAR2(v)      G_CAST_TYPE_TO_TYPE(gint, gunichar2, v)
#define GINT_TO_SHORT(v)         G_CAST_TYPE_TO_TYPE(gint, gshort, v)
#define GINT_TO_CHAR(v)          G_CAST_TYPE_TO_TYPE(gint, gchar, v)

#define GUINT_TO_INT32(v)        G_CAST_TYPE_TO_TYPE(guint, gint32, v)
#define GUINT_TO_UINT32(v)       G_CAST_TYPE_TO_TYPE(guint, guint32, v)
#define GUINT_TO_INT16(v)        G_CAST_TYPE_TO_TYPE(guint, gint16, v)
#define GUINT_TO_UINT16(v)       G_CAST_TYPE_TO_TYPE(guint, guint16, v)
#define GUINT_TO_INT8(v)         G_CAST_TYPE_TO_TYPE(guint, gint8, v)
#define GUINT_TO_UINT8(v)        G_CAST_TYPE_TO_TYPE(guint, guint8, v)

#define GUINT_TO_INTPTR(v)       G_CAST_UTYPE_TO_PTRTYPE(guint, gintptr, v)
#define GUINT_TO_UINTPTR(v)      G_CAST_UTYPE_TO_PTRTYPE(guint, guintptr, v)
#define GUINT_TO_POINTER(v)      G_CAST_UTYPE_TO_PTRTYPE(guint, gpointer, v)
#define GUINT_TO_INT(v)          G_CAST_TYPE_TO_TYPE(guint, gint, v)
#define GUINT_TO_OPCODE(v)       G_CAST_TYPE_TO_TYPE(guint, guint16, v)
#define GUINT_TO_UNICHAR2(v)     G_CAST_TYPE_TO_TYPE(guint, gunichar2, v)
#define GUINT_TO_SHORT(v)        G_CAST_TYPE_TO_TYPE(guint, gshort, v)
#define GUINT_TO_CHAR(v)         G_CAST_TYPE_TO_TYPE(guint, gchar, v)

#define GFLOAT_TO_INT32(v)       G_CAST_TYPE_TO_TYPE(gfloat, gint32, v)
#define GFLOAT_TO_UINT32(v)      G_CAST_TYPE_TO_TYPE(gfloat, guint32, v)

#define GFLOAT_TO_INT(v)         G_CAST_TYPE_TO_TYPE(gfloat, gint, v)
#define GFLOAT_TO_UINT(v)        G_CAST_TYPE_TO_TYPE(gfloat, guint, v)

#define GINT16_TO_INT8(v)        G_CAST_TYPE_TO_TYPE(gint16, gint8, v)
#define GINT16_TO_UINT8(v)       G_CAST_TYPE_TO_TYPE(gint16, guint8, v)

#define GUINT16_TO_INT8(v)       G_CAST_TYPE_TO_TYPE(guint16, gint8, v)
#define GUINT16_TO_UINT8(v)      G_CAST_TYPE_TO_TYPE(guint16, guint8, v)

#define GUNICHAR_TO_INT16(v)     G_CAST_TYPE_TO_TYPE(gunichar, gint16, v)
#define GUNICHAR_TO_UINT16(v)    G_CAST_TYPE_TO_TYPE(gunichar, guint16, v)
#define GUNICHAR_TO_INT8(v)      G_CAST_TYPE_TO_TYPE(gunichar, gint8, v)
#define GUNICHAR_TO_UINT8(v)     G_CAST_TYPE_TO_TYPE(gunichar, guint8, v)
#define GUNICHAR_TO_CHAR(v)      G_CAST_TYPE_TO_TYPE(gunichar, gchar, v)

#endif // __GLIB_H
