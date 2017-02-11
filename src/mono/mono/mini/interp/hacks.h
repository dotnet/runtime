/**
 * \file
 */

/* we need some special math function */
#ifndef _ISOC99_SOURCE
#define _ISOC99_SOURCE
#endif
#include <math.h>

/* which are not defined on FreeBSD */
#ifdef __GNUC__

#ifndef isunordered
#   define isunordered(u, v)                              \
    (__extension__                                        \
     ({ __typeof__(u) __u = (u); __typeof__(v) __v = (v); \
        isnan(__u) || isnan(__v); }))
#endif

#ifndef islessgreater
#   define islessgreater(x, u)                                    \
    (__extension__                                                \
     ({ __typeof__(x) __x = (x); __typeof__(y) __y = (y);         \
        !isunordered (__x, __y) && (__x < __y) || (__y < __x); }))
#endif

#ifndef islessequal
#   define islessequal(x, y)                              \
    (__extension__                                        \
     ({ __typeof__(x) __x = (x); __typeof__(y) __y = (y); \
        !isunordered(__x, __y) && __x <= __y; })) 
#endif

#ifndef isless
#   define isless(x, y)                                   \
    (__extension__                                        \
     ({ __typeof__(x) __x = (x); __typeof__(y) __y = (y); \
        !isunordered(__x, __y) && __x < __y; })) 
#endif

#ifndef isgreater
#   define isgreater(x, y)                                \
    (__extension__                                        \
     ({ __typeof__(x) __x = (x); __typeof__(y) __y = (y); \
        !isunordered(__x, __y) && __x > __y; }))
#endif

#else

/*  isunordered seems to crash on HPUX when built 64 bits
    so use generic implementation.
*/
#if defined(__hpux) && SIZEOF_VOID_P == 8
#undef isunordered
#undef islessgreater
#undef islessequal
#undef isless
#undef isgreater
#endif

#ifndef isunordered
#   define isunordered(u, v) (isnan(u) || isnan(v))
#endif

#ifndef islessgreater
#   define islessgreater(x, u) (!isunordered (x, y) && (x < y) || (y < x))
#endif

#ifndef islessequal
#   define islessequal(x, y) (!isunordered(x, y) && x <= y)
#endif

#ifndef isless
#   define isless(x, y) (!isunordered(x, y) && x < y) 
#endif

#ifndef isgreater
#   define isgreater(x, y) (!isunordered(x, y) && x > y)
#endif

#endif

/*
 * Attempt at using the goto label construct of GNU GCC:
 * it turns out this does give some benefit: 5-15% speedup.
 * Don't look at these macros, it hurts...
 */
#define GOTO_LABEL
#undef GOTO_LABEL
#ifdef GOTO_LABEL

#define SWITCH(a) goto *goto_map [(a)];
#define BREAK SWITCH(*ip)
#define CASE(l)	l ## _LABEL:
#define DEFAULT	\
	CEE_ILLEGAL_LABEL:	\
	CEE_ENDMAC_LABEL:
#define SUB_SWITCH \
	CEE_PREFIX1_LABEL: \
	CEE_ARGLIST_LABEL: \
	CEE_CEQ_LABEL: \
	CEE_CGT_LABEL: \
	CEE_CGT_UN_LABEL: \
	CEE_CLT_LABEL: \
	CEE_CLT_UN_LABEL: \
	CEE_LDFTN_LABEL: \
	CEE_LDVIRTFTN_LABEL: \
	CEE_UNUSED56_LABEL: \
	CEE_LDARG_LABEL: \
	CEE_LDARGA_LABEL: \
	CEE_STARG_LABEL: \
	CEE_LDLOC_LABEL: \
	CEE_LDLOCA_LABEL: \
	CEE_STLOC_LABEL: \
	CEE_LOCALLOC_LABEL: \
	CEE_UNUSED57_LABEL: \
	CEE_ENDFILTER_LABEL: \
	CEE_UNALIGNED__LABEL: \
	CEE_VOLATILE__LABEL: \
	CEE_TAIL__LABEL: \
	CEE_INITOBJ_LABEL: \
	CEE_UNUSED68_LABEL: \
	CEE_CPBLK_LABEL: \
	CEE_INITBLK_LABEL: \
	CEE_UNUSED69_LABEL: \
	CEE_RETHROW_LABEL: \
	CEE_UNUSED_LABEL: \
	CEE_SIZEOF_LABEL: \
	CEE_REFANYTYPE_LABEL: \
	CEE_UNUSED52_LABEL: \
	CEE_UNUSED53_LABEL: \
	CEE_UNUSED54_LABEL: \
	CEE_UNUSED55_LABEL: \
	CEE_UNUSED70_LABEL:
#define GOTO_LABEL_VARS \
	const static void * const goto_map [] = {\
#define OPDEF(a,b,c,d,e,f,g,h,i,j) \	\
	&& a ## _LABEL,	\
#include "mono/cil/opcode.def"	\
#undef OPDEF	\
	&&DUMMY_LABEL	\
	};	\
	DUMMY_LABEL:

#else
	
#define SWITCH(a) switch(a)
#define BREAK	break
#define CASE(l)	case l:
#define DEFAULT	\
		default:	\
			g_error ("Unimplemented opcode: %x at 0x%x\n", *ip, ip-header->code);
#define SUB_SWITCH case 0xFE:
#define GOTO_LABEL_VARS

#endif
