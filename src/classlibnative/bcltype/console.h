// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// File: Console.h
//

//
// Purpose: Native methods on System.Console
//

//
#ifndef _CONSOLE_H_
#define _CONSOLE_H_

#ifndef FEATURE_CORECLR

#include "qcall.h"

class ConsoleNative {

private:

    // Short buffer len to try using first:
    static const INT32 ShortConsoleTitleLength = 200;
    
public:

    // This value is copied from Console.cs. There is said:
    // MSDN says console titles can be up to 64KB in length.
    // But there is an exception if the buffer lengths longer than
    // ~24500 Unicode characters are used. Oh well.
    static const INT32 MaxConsoleTitleLength = 24500;
        
    static INT32 QCALLTYPE GetTitle(QCall::StringHandleOnStack outTitle, INT32& outTitleLen);  
};

class ConsoleStreamHelper {
public:
    static FCDECL2(void, WaitForAvailableConsoleInput, SafeHandle* refThisUNSAFE, CLR_BOOL bIsPipe);
};

#endif  // ifndef FEATURE_CORECLR

#endif  // _CONSOLE_H_
