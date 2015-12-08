//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

#include "stdafx.h"
#include <utilcode.h>

EXTERN_C void __stdcall InitializeSxS(CoreClrCallbacks const & callbacks)
{
    InitUtilcode(callbacks);
}
