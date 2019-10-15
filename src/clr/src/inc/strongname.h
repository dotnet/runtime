// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// The VM statically links StrongName APIs so we can suppress the warning.

#define USE_DEPRECATED_CLR_API_WITHOUT_WARNING
#include "../strongname/inc/strongname.h"
#undef USE_DEPRECATED_CLR_API_WITHOUT_WARNING 
