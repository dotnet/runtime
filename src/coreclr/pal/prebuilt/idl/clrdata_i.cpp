// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/* this ALWAYS GENERATED file contains the IIDs and CLSIDs */

/* link this file in with the server and any clients */


 /* File created by MIDL compiler version 8.01.0626 */
/* Compiler settings for clrdata.idl:
    Oicf, W1, Zp8, env=Win64 (32b run), target_arch=AMD64 8.01.0626 
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

MIDL_DEFINE_GUID(IID, IID_ICLRDataTarget,0x3E11CCEE,0xD08B,0x43e5,0xAF,0x01,0x32,0x71,0x7A,0x64,0xDA,0x03);


MIDL_DEFINE_GUID(IID, IID_ICLRDataTarget2,0x6d05fae3,0x189c,0x4630,0xa6,0xdc,0x1c,0x25,0x1e,0x1c,0x01,0xab);


MIDL_DEFINE_GUID(IID, IID_ICLRDataTarget3,0xa5664f95,0x0af4,0x4a1b,0x96,0x0e,0x2f,0x33,0x46,0xb4,0x21,0x4c);


MIDL_DEFINE_GUID(IID, IID_ICLRRuntimeLocator,0xb760bf44,0x9377,0x4597,0x8b,0xe7,0x58,0x08,0x3b,0xdc,0x51,0x46);


MIDL_DEFINE_GUID(IID, IID_ICLRMetadataLocator,0xaa8fa804,0xbc05,0x4642,0xb2,0xc5,0xc3,0x53,0xed,0x22,0xfc,0x63);


MIDL_DEFINE_GUID(IID, IID_ICLRDataEnumMemoryRegionsCallback,0xBCDD6908,0xBA2D,0x4ec5,0x96,0xCF,0xDF,0x4D,0x5C,0xDC,0xB4,0xA4);


MIDL_DEFINE_GUID(IID, IID_ICLRDataEnumMemoryRegionsCallback2,0x3721A26F,0x8B91,0x4D98,0xA3,0x88,0xDB,0x17,0xB3,0x56,0xFA,0xDB);


MIDL_DEFINE_GUID(IID, IID_ICLRDataLoggingCallback,0xF315248D,0x8B79,0x49DB,0xB1,0x84,0x37,0x42,0x65,0x59,0xF7,0x03);


MIDL_DEFINE_GUID(IID, IID_ICLRDataEnumMemoryRegions,0x471c35b4,0x7c2f,0x4ef0,0xa9,0x45,0x00,0xf8,0xc3,0x80,0x56,0xf1);

#undef MIDL_DEFINE_GUID

#ifdef __cplusplus
}
#endif



