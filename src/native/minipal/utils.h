// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HAVE_MINIPAL_UTILS_H
#define HAVE_MINIPAL_UTILS_H

#define ARRAY_SIZE(arr) (sizeof(arr)/sizeof(arr[0]))

// Number of characters in a string literal. Excludes terminating NULL.
#define STRING_LENGTH(str) (ARRAY_SIZE(str) - 1)

#endif // HAVE_MINIPAL_UTILS_H
