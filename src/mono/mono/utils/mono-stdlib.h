/**
 * \file
 */

#ifndef __MONO_FILE_H
#define __MONO_FILE_H

#ifdef HAVE_MKSTEMP
#include <stdlib.h>
#define mono_mkstemp(a) mkstemp(a)
#else
int mono_mkstemp (char *templ);
#endif

#endif /* __MONO_FILE_H */

