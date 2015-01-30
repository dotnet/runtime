//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

// The VM statically links StrongName APIs so we can suppress the warning.

#define USE_DEPRECATED_CLR_API_WITHOUT_WARNING
#include "../strongname/inc/strongname.h"
#undef USE_DEPRECATED_CLR_API_WITHOUT_WARNING 
