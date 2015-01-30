//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//*****************************************************************************
// File: CorPermE.H
//
// Defines the Memory and Error routines defined in the secuirty libraries. 
// These routines are mainly for the security dll and the runtime.
//
//*****************************************************************************

#ifndef _CORPERME_H_
#define _CORPERME_H_

//=============================================================================
// Error macros so we do not have to see goto's in the code
// Adds structure to where error handling and clean up code goes. Be careful
// when rethrowing EE exceptions, the routine must be cleaned up first.
//=============================================================================
typedef struct _CorError {
    HRESULT corError;
} CorError;

#define CORTRY       HRESULT _tcorError = 0;
#define CORTHROW(x)  {_tcorError = x; goto CORERROR;} //
#define CORCATCH(x)  goto CORCONT; \
                     CORERROR: \
                     { CorError x; x.corError = _tcorError;
#define COREND       } CORCONT: //                                        

#ifdef __cplusplus
extern "C" {
#endif
    
#define WIDEN_CP CP_UTF8

// Helper macros for security logging
#define WIDEN(psz, pwsz) \
    LPCSTR _##psz = (LPCSTR) psz; \
    int _cc##psz = _##psz ? strlen(_##psz) + 1 : 0; \
    LPWSTR pwsz = (LPWSTR) (_cc##psz ? _alloca((_cc##psz) * sizeof(WCHAR)) : NULL); \
    if(pwsz) WszMultiByteToWideChar(WIDEN_CP, 0, _##psz, _cc##psz, pwsz, _cc##psz);


#define NARROW(pwsz, psz) \
    LPCWSTR _##pwsz = (LPCWSTR) pwsz; \
    int _cc##psz =  _##pwsz ? WszWideCharToMultiByte(WIDEN_CP, 0, _##pwsz, -1, NULL, 0, NULL, NULL) : 0; \
    LPSTR psz = (LPSTR) (_cc##psz ? _alloca(_cc##psz) : NULL); \
    if(psz) WszWideCharToMultiByte(WIDEN_CP, 0, _##pwsz, -1, psz, _cc##psz, NULL, NULL);


#ifdef __cplusplus
}
#endif

#endif
