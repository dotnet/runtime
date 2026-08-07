// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <minipal/thread.h>

#if !defined(__wasm) || defined(_REENTRANT)
MINIPAL_THREAD_LOCAL size_t minipal_cached_thread_id;
#endif
