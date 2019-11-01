// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//*****************************************************************************
// UtilCode.h
//
// Utility functions implemented in UtilCode.lib.
//

//*****************************************************************************

#ifndef __PostError_h__
#define __PostError_h__

#include "switches.h"

//*****************************************************************************
// This function will post an error for the client.  If the LOWORD(hrRpt) can
// be found as a valid error message, then it is loaded and formatted with
// the arguments passed in.  If it cannot be found, then the error is checked
// against FormatMessage to see if it is a system error.  System errors are
// not formatted so no add'l parameters are required.  If any errors in this
// process occur, hrRpt is returned for the client with no error posted.
//*****************************************************************************
extern "C"
HRESULT __cdecl PostError(             // Returned error.
    HRESULT     hrRpt,                  // Reported error.
    ...);                               // Error arguments.

extern "C"
HRESULT __cdecl PostErrorVA(            // Returned error.
    HRESULT     hrRpt,                  // Reported error.
    va_list     marker);                // Error arguments.

//*****************************************************************************
// This function formats an error message, but doesn't fill the IErrorInfo.
//*****************************************************************************
HRESULT __cdecl FormatRuntimeErrorVa(        
    __out_ecount(cchMsg) WCHAR *rcMsg,  // Buffer into which to format.         
    ULONG       cchMsg,                 // Size of buffer, characters.          
    HRESULT     hrRpt,                  // The HR to report.                    
    va_list     marker);                // Optional args.                       

HRESULT __cdecl FormatRuntimeError(
    __out_ecount(cchMsg) WCHAR *rcMsg,  // Buffer into which to format.
    ULONG       cchMsg,                 // Size of buffer, characters.
    HRESULT     hrRpt,                  // The HR to report.
    ...);                               // Optional args.

#endif // __PostError_h__
