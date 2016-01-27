// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***
*snprintf.c - "Count" version of sprintf
*

*
*Purpose:
*       The _snprintf() flavor takes a count argument that is
*       the max number of bytes that should be written to the
*       user's buffer.
*
*******************************************************************************/

#define _COUNT_ 1
#include "sprintf.c"
