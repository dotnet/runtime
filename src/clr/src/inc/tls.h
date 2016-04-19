// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// TLS.H -
//

//
// Encapsulates TLS access for maximum performance. 
//



#ifndef __tls_h__
#define __tls_h__

#ifdef FEATURE_IMPLICIT_TLS
#ifdef _WIN64
#ifndef _DEBUG
#define OFFSETOF__TLS__tls_ThreadLocalInfo 0x10
#else // _DEBUG
#define OFFSETOF__TLS__tls_ThreadLocalInfo 0x08
#endif // _DEBUG
#else // _WIN64
#define OFFSETOF__TLS__tls_ThreadLocalInfo 0x04
#endif // _WIN64

#define OFFSETOF__TLS__tls_CurrentThread         (OFFSETOF__TLS__tls_ThreadLocalInfo+0x0)
#define OFFSETOF__TLS__tls_EETlsData             (OFFSETOF__TLS__tls_CurrentThread+2*sizeof(void*))


#ifdef _TARGET_WIN64_
#define WINNT_OFFSETOF__TEB__ThreadLocalStoragePointer  0x58
#else
#define WINNT_OFFSETOF__TEB__ThreadLocalStoragePointer  0x2c
#endif

#endif // FEATURE_IMPLICIT_TLS

// Pointer to a function that retrieves the TLS data for a specific index.
typedef LPVOID (*POPTIMIZEDTLSGETTER)();

//---------------------------------------------------------------------------
// Creates a platform-optimized version of TlsGetValue compiled
// for a particular index. Can return NULL - the caller should substitute
// a non-optimized getter in this case.
//---------------------------------------------------------------------------
POPTIMIZEDTLSGETTER MakeOptimizedTlsGetter(DWORD tlsIndex, LPVOID pBuffer = NULL, SIZE_T cbBuffer = 0, POPTIMIZEDTLSGETTER pGenericImpl = NULL, BOOL fForceGeneric = FALSE);


//---------------------------------------------------------------------------
// Frees a function created by MakeOptimizedTlsGetter().
//---------------------------------------------------------------------------
VOID FreeOptimizedTlsGetter(POPTIMIZEDTLSGETTER pOptimizedTlsGetter);



//---------------------------------------------------------------------------
// For ASM stub generators that want to inline Thread access for efficiency,
// the Thread manager uses these constants to define how to access the Thread.
//---------------------------------------------------------------------------
enum TLSACCESSMODE {
   TLSACCESS_GENERIC    = 1,   // Make no platform assumptions: use the API
   // TLS
   TLSACCESS_WNT        = 2,   // WinNT-style TLS
   TLSACCESS_WNT_HIGH   = 3,   // WinNT5-style TLS, slot > TLS_MINIMUM_AVAILABLE
};


//---------------------------------------------------------------------------
// WinNT store the TLS in different places relative to the
// fs:[0]. This api reveals which. Can also return TLSACCESS_GENERIC if
// no info is available about the Thread location (you have to use the TlsGetValue
// api.) This is intended for use by stub generators that want to inline TLS
// access.
//---------------------------------------------------------------------------
TLSACCESSMODE GetTLSAccessMode(DWORD tlsIndex);   

#endif // __tls_h__
