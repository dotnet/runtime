#ifndef __EGLIB_CONFIG_H
#define __EGLIB_CONFIG_H

/*
 * System-dependent settings
 */
#define G_GNUC_PRETTY_FUNCTION   
#define G_GNUC_UNUSED            
#define G_BYTE_ORDER             G_LITTLE_ENDIAN
#define G_GNUC_NORETURN          
#define G_SEARCHPATH_SEPARATOR_S ";"
#define G_SEARCHPATH_SEPARATOR   ';'
#define G_DIR_SEPARATOR          '\'
#define G_DIR_SEPARATOR_S        "\"
#define G_BREAKPOINT()           G_STMT_START { raise(SIGTRAP); } G_STMT_END
#define G_OS_WIN32

#if  == 1
#define G_HAVE_ALLOCA_H
#endif

#if  == 1
#define G_HAVE_UNISTD_H
#endif

typedef size_t gsize;
typedef ptrdiff_t gssize;

#define G_GSIZE_FORMAT   "lu"

#if defined (HOST_WATCHOS)
#undef G_BREAKPOINT
#define G_BREAKPOINT()
#endif

#if defined (HOST_WASM)
#undef G_BREAKPOINT
#define G_BREAKPOINT() do { printf ("MONO: BREAKPOINT\n"); abort (); } while (0)
#endif

typedef void * GPid;

#endif
