

/* this ALWAYS GENERATED file contains the IIDs and CLSIDs */

/* link this file in with the server and any clients */


 /* File created by MIDL compiler version 8.01.0628 */
/* Compiler settings for metahost.idl:
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

MIDL_DEFINE_GUID(IID, IID_ICLRMetaHost,0xD332DB9E,0xB9B3,0x4125,0x82,0x07,0xA1,0x48,0x84,0xF5,0x32,0x16);


MIDL_DEFINE_GUID(IID, IID_ICLRDebuggingLibraryProvider,0x3151C08D,0x4D09,0x4f9b,0x88,0x38,0x28,0x80,0xBF,0x18,0xFE,0x51);


MIDL_DEFINE_GUID(IID, IID_ICLRDebuggingLibraryProvider2,0xE04E2FF1,0xDCFD,0x45D5,0xBC,0xD1,0x16,0xFF,0xF2,0xFA,0xF7,0xBA);


MIDL_DEFINE_GUID(IID, IID_ICLRDebuggingLibraryProvider3,0xDE3AAB18,0x46A0,0x48B4,0xBF,0x0D,0x2C,0x33,0x6E,0x69,0xEA,0x1B);


MIDL_DEFINE_GUID(IID, IID_ICLRDebugging,0xD28F3C5A,0x9634,0x4206,0xA5,0x09,0x47,0x75,0x52,0xEE,0xFB,0x10);


MIDL_DEFINE_GUID(IID, IID_ICLRRuntimeInfo,0xBD39D1D2,0xBA2F,0x486a,0x89,0xB0,0xB4,0xB0,0xCB,0x46,0x68,0x91);

#undef MIDL_DEFINE_GUID

#ifdef __cplusplus
}
#endif



