// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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

#define __uuidof(type)      IID_##type

#define DECLSPEC_UUID(x)

#define MIDL_INTERFACE(x)   struct DECLSPEC_UUID(x) DECLSPEC_NOVTABLE

#define EXTERN_GUID(itf,l1,s1,s2,c1,c2,c3,c4,c5,c6,c7,c8) \
    constexpr IID itf = {l1,s1,s2,{c1,c2,c3,c4,c5,c6,c7,c8}}

interface IRpcStubBuffer;
interface IRpcChannelBuffer;

typedef void* PRPC_MESSAGE;
typedef void* RPC_IF_HANDLE;

#endif // __RPC_H__
