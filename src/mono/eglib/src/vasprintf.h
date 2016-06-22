#ifndef __VASPRINTF_H
#define __VASPRINTF_H

#include <stdarg.h>

#if !defined (HAVE_VASPRINTF) || defined (G_OVERRIDABLE_ALLOCATORS)
int g_vasprintf(char **ret, const char *fmt, va_list ap);
#endif

#endif /* __VASPRINTF_H */
