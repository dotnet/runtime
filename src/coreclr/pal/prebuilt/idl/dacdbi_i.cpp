// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/* this ALWAYS GENERATED file contains the IIDs and CLSIDs */

/* link this file in with the server and any clients */


 /* File created by MIDL compiler version 8.01.0628 */
/* Compiler settings for dacdbi.idl:
    Oicf, W1, Zp8, env=Win64 (32b run), target_arch=AMD64 8.01.0628
    protocol : dce , ms_ext, c_ext, robust
    error checks: allocation ref bounds_check enum stub_data
    VC __declspec() decoration level:
         __declspec(uuid()), __declspec(selectany), __declspec(novtable)
         DECLSPEC_UUID(), MIDL_INTERFACE()
*/
/* @@MIDL_FILE_HEADING(  ) */

#pragma warning( disable: 4049 )  /* more than 64k source lines */


#ifdef __cplusplus
extern "C"{
#endif


#include <rpc.h>
#include <rpcndr.h>

#ifdef _MIDL_USE_GUIDDEF_

#ifndef INITGUID
#define INITGUID
#include <guiddef.h>
#undef INITGUID
#else
#include <guiddef.h>
#endif

#define MIDL_DEFINE_GUID(type,name,l,w1,w2,b1,b2,b3,b4,b5,b6,b7,b8) \
        DEFINE_GUID(name,l,w1,w2,b1,b2,b3,b4,b5,b6,b7,b8)

#else // !_MIDL_USE_GUIDDEF_

#ifndef __IID_DEFINED__
#define __IID_DEFINED__

typedef struct _IID
{
    unsigned long x;
    unsigned short s1;
    unsigned short s2;
    unsigned char  c[8];
} IID;

#endif // __IID_DEFINED__

#ifndef CLSID_DEFINED
#define CLSID_DEFINED
typedef IID CLSID;
#endif // CLSID_DEFINED

#define MIDL_DEFINE_GUID(type,name,l,w1,w2,b1,b2,b3,b4,b5,b6,b7,b8) \
        EXTERN_C __declspec(selectany) const type name = {l,w1,w2,{b1,b2,b3,b4,b5,b6,b7,b8}}

#endif // !_MIDL_USE_GUIDDEF_


// IDacDbiAllocator: {97441D33-C82F-4106-B025-A912CB507526}
MIDL_DEFINE_GUID(IID, IID_IDacDbiAllocator,0x97441d33,0xc82f,0x4106,0xb0,0x25,0xa9,0x12,0xcb,0x50,0x75,0x26);


// IDacDbiMetaDataLookup: {EF037312-925C-4A13-A9B5-3C1BB07B56FD}
MIDL_DEFINE_GUID(IID, IID_IDacDbiMetaDataLookup,0xef037312,0x925c,0x4a13,0xa9,0xb5,0x3c,0x1b,0xb0,0x7b,0x56,0xfd);


// IDacDbiInterface: {DB505C1B-A327-4A46-8C32-AF55A56F8E09}
MIDL_DEFINE_GUID(IID, IID_IDacDbiInterface,0xdb505c1b,0xa327,0x4a46,0x8c,0x32,0xaf,0x55,0xa5,0x6f,0x8e,0x09);

#undef MIDL_DEFINE_GUID

#ifdef __cplusplus
}
#endif


