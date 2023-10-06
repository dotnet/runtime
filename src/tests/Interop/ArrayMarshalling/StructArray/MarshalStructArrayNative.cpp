// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#include <xplatform.h>
#include <platformdefines.h>
#include <stdio.h>
#include <stdlib.h>

extern "C" DLL_EXPORT void STDMETHODCALLTYPE ForwardToCallback(void* value, void(STDMETHODCALLTYPE* callback)(void*))
{
	callback(value);
}