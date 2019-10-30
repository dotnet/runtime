//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#include "standardpch.h"
#include "coreclrcallbacks.h"
#include "iexecutionengine.h"

CoreClrCallbacks*           original_CoreClrCallbacks         = nullptr;

IExecutionEngine* STDMETHODCALLTYPE IEE_t()
{
    interceptor_IEE* iee = new interceptor_IEE();
    iee->original_IEE    = original_CoreClrCallbacks->m_pfnIEE();
    return iee;
}
