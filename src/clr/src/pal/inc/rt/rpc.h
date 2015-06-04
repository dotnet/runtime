//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//

//
// ===========================================================================
// File: rpc.h
// 
// =========================================================================== 
// dummy rpc.h for PAL

#ifndef __RPC_H__
#define __RPC_H__

#include "palrt.h"

#define __RPC_STUB
#define __RPC_USER
#define __RPC_FAR

#define DECLSPEC_UUID(x) __declspec(uuid(x))
#define MIDL_INTERFACE(x)   struct DECLSPEC_UUID(x) DECLSPEC_NOVTABLE

#define EXTERN_GUID(itf,l1,s1,s2,c1,c2,c3,c4,c5,c6,c7,c8) \
    EXTERN_C const IID DECLSPEC_SELECTANY itf = {l1,s1,s2,{c1,c2,c3,c4,c5,c6,c7,c8}}

interface IRpcStubBuffer;
interface IRpcChannelBuffer;

typedef void* PRPC_MESSAGE;
typedef void* RPC_IF_HANDLE;

#endif // __RPC_H__
