// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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
HRESULT PostError(             // Returned error.
    HRESULT     hrRpt,                  // Reported error.
    ...);                               // Error arguments.

#endif // __PostError_h__
