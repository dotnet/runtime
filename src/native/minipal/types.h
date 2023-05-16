// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HAVE_MINIPAL_TYPES_H
#define HAVE_MINIPAL_TYPES_H

#if defined(TARGET_32BIT) || defined(TARGET_OSX) || defined(TARGET_WINDOWS)
#define FORMAT_PREFIX "l"
#else
#define FORMAT_PREFIX ""
#endif

#ifndef PRIX64
#define PRIX64 FORMAT_PREFIX "lX"
#endif

#ifndef PRIx64
#define PRIx64 FORMAT_PREFIX "lx"
#endif

#ifndef PRIu64
#define PRIu64 FORMAT_PREFIX "lu"
#endif

#endif // HAVE_MINIPAL_TYPES_H
