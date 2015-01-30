//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

#ifdef FEATURE_COMINTEROP

#include "common.h"
#include "ComCallableWrapper.h"
#include "WindowsRuntimeBufferHelper.h"

void QCALLTYPE WindowsRuntimeBufferHelper::StoreOverlappedPtrInCCW(QCall::ObjectHandleOnStack winRtBuffer, LPOVERLAPPED lpOverlapped) {

    QCALL_CONTRACT;
        
    BEGIN_QCALL;
    
    GCX_COOP();
    OBJECTREF buffer = ObjectToOBJECTREF(*winRtBuffer.m_ppObject);
        
    ComCallWrapper *ccw = ComCallWrapper::GetWrapperForObject(buffer);
    SimpleComCallWrapper *simpleCCW = ccw->GetSimpleWrapper();
    
    simpleCCW->StoreOverlappedPointer(lpOverlapped);
       
    END_QCALL;    
}


void WindowsRuntimeBufferHelper::ReleaseOverlapped(LPOVERLAPPED lpOverlapped) {
        
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    GCX_COOP();
    OverlappedDataObject::GetOverlapped(lpOverlapped)->FreeAsyncPinHandles();
}

#endif  // ifdef FEATURE_COMINTEROP
