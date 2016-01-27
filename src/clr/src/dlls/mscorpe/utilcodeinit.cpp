// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "stdafx.h"
#include <utilcode.h>

EXTERN_C void __stdcall InitializeSxS(CoreClrCallbacks const & callbacks)
{
    InitUtilcode(callbacks);
}
