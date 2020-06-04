// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



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

MIDL_DEFINE_GUID(IID, IID_ICLRPrivBinder,0x2601F621,0xE462,0x404C,0xB2,0x99,0x3E,0x1D,0xE7,0x2F,0x85,0x42);


MIDL_DEFINE_GUID(IID, IID_ICLRPrivAssembly,0x2601F621,0xE462,0x404C,0xB2,0x99,0x3E,0x1D,0xE7,0x2F,0x85,0x43);


MIDL_DEFINE_GUID(IID, IID_ICLRPrivResource,0x2601F621,0xE462,0x404C,0xB2,0x99,0x3E,0x1D,0xE7,0x2F,0x85,0x47);


MIDL_DEFINE_GUID(IID, IID_ICLRPrivResourcePath,0x2601F621,0xE462,0x404C,0xB2,0x99,0x3E,0x1D,0xE7,0x2F,0x85,0x44);


MIDL_DEFINE_GUID(IID, IID_ICLRPrivResourceAssembly,0x8d2d3cc9,0x1249,0x4ad4,0x97,0x7d,0xb7,0x72,0xbd,0x4e,0x8a,0x94);


MIDL_DEFINE_GUID(IID, IID_ICLRPrivAssemblyInfo,0x5653946E,0x800B,0x48B7,0x8B,0x09,0xB1,0xB8,0x79,0xB5,0x4F,0x68);


MIDL_DEFINE_GUID(IID, IID_ICLRPrivAssemblyID_WinRT,0x4372D277,0x9906,0x4FED,0xBF,0x53,0x30,0xC0,0xB4,0x01,0x08,0x96);

#undef MIDL_DEFINE_GUID

#ifdef __cplusplus
}
#endif



