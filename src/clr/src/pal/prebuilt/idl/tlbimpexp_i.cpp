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
        const type name = {l,w1,w2,{b1,b2,b3,b4,b5,b6,b7,b8}}

#endif !_MIDL_USE_GUIDDEF_

MIDL_DEFINE_GUID(IID, LIBID_TlbImpLib,0x20BC1825,0x06F0,0x11d2,0x8C,0xF4,0x00,0xA0,0xC9,0xB0,0xA0,0x63);


MIDL_DEFINE_GUID(IID, IID_ITypeLibImporterNotifySink,0xF1C3BF76,0xC3E4,0x11D3,0x88,0xE7,0x00,0x90,0x27,0x54,0xC4,0x3A);


MIDL_DEFINE_GUID(IID, IID_ITypeLibExporterNotifySink,0xF1C3BF77,0xC3E4,0x11D3,0x88,0xE7,0x00,0x90,0x27,0x54,0xC4,0x3A);


MIDL_DEFINE_GUID(IID, IID_ITypeLibExporterNameProvider,0xFA1F3615,0xACB9,0x486d,0x9E,0xAC,0x1B,0xEF,0x87,0xE3,0x6B,0x09);

#undef MIDL_DEFINE_GUID

#ifdef __cplusplus
}
#endif



