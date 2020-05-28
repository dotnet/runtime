/**
 * \file
 *
 * Header for jemalloc registration code
 */

#ifndef __MONO_JEMALLOC_H__
#define __MONO_JEMALLOC_H__

#if defined(MONO_JEMALLOC_ENABLED)

#include <jemalloc/jemalloc.h>

/* Jemalloc can be configured in three ways.
 * 1. You can use it with library loading hacks at run-time
 * 2. You can use it as a global malloc replacement
 * 3. You can use it with a prefix. If you use it with a prefix, you have to explicitly name the malloc function.
 *
 * In order to make this feature able to be toggled at run-time, I chose to use a prefix of mono_je. 
 * This mapping is captured below in the header, in the spirit of "no magic constants".
 *
 * The place that configures jemalloc and sets this prefix is in the Makefile in
 * mono/jemalloc/Makefile.am 
 *
 */
#define MONO_JEMALLOC_MALLOC mono_jemalloc
#define MONO_JEMALLOC_REALLOC mono_jerealloc
#define MONO_JEMALLOC_FREE mono_jefree
#define MONO_JEMALLOC_CALLOC mono_jecalloc

void mono_init_jemalloc (void);

#endif

#endif

