// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HAVE_MINIPAL_TYPES_H
#define HAVE_MINIPAL_TYPES_H

#include <stdlib.h>

// Format attribute for printf-style functions
#ifdef __GNUC__
#define MINIPAL_ATTR_FORMAT_PRINTF(fmt_pos, arg_pos) __attribute__ ((__format__(__printf__, fmt_pos, arg_pos)))
#else
#define MINIPAL_ATTR_FORMAT_PRINTF(fmt_pos, arg_pos)
#endif

#ifdef TARGET_WINDOWS
typedef wchar_t CHAR16_T;
#else
typedef unsigned short CHAR16_T;
#endif

#endif // HAVE_MINIPAL_TYPES_H
