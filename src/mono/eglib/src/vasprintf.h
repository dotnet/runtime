#ifndef __VASPRINTF_H
#define __VASPRINTF_H

#include <stdarg.h>
#include <config.h>

#ifndef HAVE_VASPRINTF
int vasprintf(char **ret, const char *fmt, va_list ap);
#endif

#endif /* __VASPRINTF_H */
