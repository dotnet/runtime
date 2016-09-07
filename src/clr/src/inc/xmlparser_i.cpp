// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/* This was a created file from a newer version of xmlparser.idl than 
   what made xmlparser.h in ndp/clr/src/inc, and then with extra
   GUIDs expurgated. */

/* this ALWAYS GENERATED file contains the IIDs and CLSIDs */

/* link this file in with the server and any clients */


 /* File created by MIDL compiler version 8.00.0571 */
/* @@MIDL_FILE_HEADING(  ) */

#ifdef _MSC_VER
#pragma warning( disable: 4049 )  /* more than 64k source lines */
#endif


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

#endif // !_MIDL_USE_GUIDDEF_

MIDL_DEFINE_GUID(IID, LIBID_XMLPSR,0xd242361c,0x51a0,0x11d2,0x9c,0xaf,0x00,0x60,0xb0,0xec,0x3d,0x39);


MIDL_DEFINE_GUID(IID, IID_IXMLNodeSource,0xd242361d,0x51a0,0x11d2,0x9c,0xaf,0x00,0x60,0xb0,0xec,0x3d,0x39);


MIDL_DEFINE_GUID(IID, IID_IXMLParser,0xd242361e,0x51a0,0x11d2,0x9c,0xaf,0x00,0x60,0xb0,0xec,0x3d,0x39);


MIDL_DEFINE_GUID(IID, IID_IXMLNodeFactory,0xd242361f,0x51a0,0x11d2,0x9c,0xaf,0x00,0x60,0xb0,0xec,0x3d,0x39);

#undef MIDL_DEFINE_GUID

#ifdef __cplusplus
}
#endif



