// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



/* this ALWAYS GENERATED file contains the IIDs and CLSIDs */

/* link this file in with the server and any clients */


 /* File created by MIDL compiler version 8.00.0603 */
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

MIDL_DEFINE_GUID(IID, LIBID_CorpubProcessLib,0xe97ca460,0x657d,0x11d3,0x8d,0x5b,0x00,0x10,0x4b,0x35,0xe7,0xef);


MIDL_DEFINE_GUID(CLSID, CLSID_CorpubPublish,0x047a9a40,0x657e,0x11d3,0x8d,0x5b,0x00,0x10,0x4b,0x35,0xe7,0xef);


MIDL_DEFINE_GUID(IID, IID_ICorPublish,0x9613A0E7,0x5A68,0x11d3,0x8F,0x84,0x00,0xA0,0xC9,0xB4,0xD5,0x0C);


MIDL_DEFINE_GUID(IID, IID_ICorPublishEnum,0xC0B22967,0x5A69,0x11d3,0x8F,0x84,0x00,0xA0,0xC9,0xB4,0xD5,0x0C);


MIDL_DEFINE_GUID(IID, IID_ICorPublishProcess,0x18D87AF1,0x5A6A,0x11d3,0x8F,0x84,0x00,0xA0,0xC9,0xB4,0xD5,0x0C);


MIDL_DEFINE_GUID(IID, IID_ICorPublishAppDomain,0xD6315C8F,0x5A6A,0x11d3,0x8F,0x84,0x00,0xA0,0xC9,0xB4,0xD5,0x0C);


MIDL_DEFINE_GUID(IID, IID_ICorPublishProcessEnum,0xA37FBD41,0x5A69,0x11d3,0x8F,0x84,0x00,0xA0,0xC9,0xB4,0xD5,0x0C);


MIDL_DEFINE_GUID(IID, IID_ICorPublishAppDomainEnum,0x9F0C98F5,0x5A6A,0x11d3,0x8F,0x84,0x00,0xA0,0xC9,0xB4,0xD5,0x0C);

#undef MIDL_DEFINE_GUID

#ifdef __cplusplus
}
#endif



