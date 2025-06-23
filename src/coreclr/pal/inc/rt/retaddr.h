// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#ifndef __PALRETADDR_H__
#define __PALRETADDR_H__

#ifndef TARGET_WASM
#define _ReturnAddress() __builtin_return_address(0)
#else
#define _ReturnAddress() 0
#endif

#endif // __PALRETADDR_H__
