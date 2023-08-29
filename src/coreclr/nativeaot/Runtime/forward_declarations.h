// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This file may be included by header files to forward declare common
// public types. The intent here is that .CPP files should need to
// include fewer header files.

#define FWD_DECL(x)             \
    class x;                    \
    typedef DPTR(x) PTR_##x;

// rtu
FWD_DECL(AllocHeap)
FWD_DECL(CObjectHeader)
FWD_DECL(CLREventStatic)
FWD_DECL(CrstHolder)
FWD_DECL(CrstStatic)
FWD_DECL(EEMethodInfo)
FWD_DECL(EECodeManager)
FWD_DECL(EEThreadId)
FWD_DECL(MethodInfo)
FWD_DECL(Module)
FWD_DECL(Object)
FWD_DECL(OBJECTHANDLEHolder)
FWD_DECL(PageEntry)
FWD_DECL(PAL_EnterHolder)
FWD_DECL(PAL_LeaveHolder)
FWD_DECL(SpinLock)
FWD_DECL(RCOBJECTHANDLEHolder)
FWD_DECL(RedhawkGCInterface)
FWD_DECL(RtuObjectRef)
FWD_DECL(RuntimeInstance)
FWD_DECL(StackFrameIterator)
FWD_DECL(SyncClean)
FWD_DECL(SyncState)
FWD_DECL(Thread)
FWD_DECL(ThreadStore)

#ifdef FEATURE_RWX_MEMORY
namespace rh {
    namespace util {
        FWD_DECL(MemRange)
        FWD_DECL(MemAccessMgr)
        FWD_DECL(WriteAccessHolder)
    }
}
#endif // FEATURE_RWX_MEMORY

// inc
FWD_DECL(MethodTable)

