// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef _WINDOWSRUNTIMEBUFFERHELPER_H_
#define _WINDOWSRUNTIMEBUFFERHELPER_H_

#ifdef FEATURE_COMINTEROP

#include "nativeoverlapped.h"
#include "qcall.h"

class WindowsRuntimeBufferHelper {

private:
    
    
public:
    
    static void QCALLTYPE StoreOverlappedPtrInCCW(QCall::ObjectHandleOnStack winRtBuffer, LPOVERLAPPED lpOverlapped);
    static void ReleaseOverlapped(LPOVERLAPPED lpOverlapped);
    
};

#endif  // ifdef FEATURE_COMINTEROP

#endif  // _WINDOWSRUNTIMEBUFFERHELPER_H_
