// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// File: Console.cpp
//

//
// Purpose: Native methods on System.Console
//
//

#ifndef FEATURE_CORECLR

#include "common.h"
#include "sbuffer.h"
#include <windows.h>
#include "console.h"

// GetConsoleTitle sometimes interprets the second parameter (nSize) as number of bytes and sometimes as the number of chars.
// Instead of doing complicated and dangerous logic to determine if this may or may not occur,
// we simply assume the worst and reserve a bigger buffer. This way we may use a bit more memory,
// but we will always be safe. This macro helps us doing that:
#define ADJUST_NUM_CHARS(numChars) ((numChars) * 2)

#define BUFF_SIZE(numChars)         ( ((numChars) + 1) * sizeof(TCHAR) )

// A buffer of size ConsoleNative::MaxConsoleTitleLength is quite big.
// First, we try allocating a smaller buffer because most often, the console title is short.
// If it turns out that the short buffer size is insufficient, we try again using a larger buffer.
INT32 QCALLTYPE ConsoleNative::GetTitle(QCall::StringHandleOnStack outTitle, INT32& outTitleLen) {

    QCALL_CONTRACT;
    
    INT32 result = 0;
    
    BEGIN_QCALL;

    // Reserve buffer:   
    InlineSBuffer< ADJUST_NUM_CHARS(BUFF_SIZE(ShortConsoleTitleLength)) > titleBuff;
        
    // Hold last error:
    DWORD lastError;

    // Read console title, get length of the title:    
    
    BYTE *buffPtr = titleBuff.OpenRawBuffer( ADJUST_NUM_CHARS(BUFF_SIZE(ShortConsoleTitleLength)) );
    
    SetLastError(0);
    DWORD len = GetConsoleTitle((TCHAR *) buffPtr, ADJUST_NUM_CHARS(ShortConsoleTitleLength + 1));
    lastError = GetLastError();

    titleBuff.CloseRawBuffer();
    
    // If the title length is larger than supported maximum, do not bother reading it, just return the length:
    if (len > MaxConsoleTitleLength) {
    
        outTitleLen = len;
        outTitle.Set(W(""));    
        result = 0;  
    
    // If title length is within valid range:
    } else {
    
        // If the title is longer than the short buffer, but can fit in the max supported length,
        // read it again with the long buffer:
        if (len > ShortConsoleTitleLength) {
        
            COUNT_T buffSize = ADJUST_NUM_CHARS(BUFF_SIZE(len));
            titleBuff.SetSize(buffSize);
            
            BYTE *buffPtr = titleBuff.OpenRawBuffer(buffSize);
            
            SetLastError(0);
            len = GetConsoleTitle((TCHAR *) buffPtr, ADJUST_NUM_CHARS(len + 1));
            lastError = GetLastError();

            titleBuff.CloseRawBuffer();
        }
        
        // Zero may indicate error or empty title. Check for error:
        result = (INT32) (0 == len ? lastError : 0);
        
        // If no error, set title and length:
        if (0 == result) {
            const BYTE *cBuffPtr = (const BYTE *) titleBuff;
            outTitle.Set((TCHAR *) cBuffPtr);
            outTitleLen = (INT32) len;
            
        // If error, set to empty:
        } else {            
            outTitleLen = (INT32) -1;
            // No need to set the title string if we have an error anyway.
        }
    }  // if title length is within valid range.
        
    END_QCALL;

    return result;
}

// Block waiting for data to become available on the console stream indicated by the safe file handle passed.
// Ensure that the thread remains abortable in the process.
FCIMPL2(void, ConsoleStreamHelper::WaitForAvailableConsoleInput, SafeHandle* refThisUNSAFE, CLR_BOOL bIsPipe)
{
    FCALL_CONTRACT;

    SAFEHANDLEREF refConsoleHandle(refThisUNSAFE);

    HELPER_METHOD_FRAME_BEGIN_1(refConsoleHandle);

    // Prevent the console handle being closed under our feet.
    SafeHandleHolder shh(&refConsoleHandle);

    // Don't pass the address of the native handle within the safe handle to DoAppropriateWait since the safe
    // handle is on the GC heap and could be moved. Instead copy the native handle out into a stack location
    // (this is safe because we've ref-counted the safe handle to prevent it being disposed on us).
    HANDLE hNativeConsoleHandle = refConsoleHandle->GetHandle();

    bool skipWait = false;

    // If we are reading from a pipe and the other end of the pipe was closed, then do not block.  No one can write to it.
    // Also we can skip blocking if we do have data available.  We should block if nothing is available, with the assumption 
    // that Windows is smart enough to handle pipes where the other end is closed.
    if (bIsPipe)
    {
        DWORD cBytesRead, cTotalBytesAvailable, cBytesLeftThisMessage;
        int r = PeekNamedPipe(hNativeConsoleHandle, NULL, 0, &cBytesRead, &cTotalBytesAvailable, &cBytesLeftThisMessage);
        if (r != 0)
        {
            skipWait = cTotalBytesAvailable > 0;
        }
        else
        {
            // Windows returns ERROR_BROKEN_PIPE if the other side of a pipe is closed.  However, we've seen
            // pipes return ERROR_NO_DATA and ERROR_PIPE_NOT_CONNECTED.  Check for those too.
            int errorCode = GetLastError();
            skipWait = errorCode == ERROR_BROKEN_PIPE || errorCode == ERROR_NO_DATA || errorCode == ERROR_PIPE_NOT_CONNECTED;
        }
    }

    // Perform the wait (DoAppropriateWait automatically handles thread aborts).
    if (!skipWait)
    {
        GetThread()->DoAppropriateWait(1, &hNativeConsoleHandle, TRUE, INFINITE, WaitMode_Alertable);
    }
  
    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

#endif  // ifndef FEATURE_CORECLR
