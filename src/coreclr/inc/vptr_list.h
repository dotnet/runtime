// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Any class with a vtable that needs to be instantiated
// during debugging data access must be listed here.

VPTR_CLASS(EEJitManager)

#ifdef FEATURE_READYTORUN
VPTR_CLASS(ReadyToRunJitManager)
#endif
VPTR_CLASS(EECodeManager)

VPTR_CLASS(RangeList)
VPTR_CLASS(LockedRangeList)
VPTR_CLASS(CodeRangeMapRangeList)

#ifdef FEATURE_METADATA_UPDATER
VPTR_CLASS(EditAndContinueModule)
#endif
VPTR_CLASS(Module)
VPTR_CLASS(ReflectionModule)

VPTR_CLASS(PrecodeStubManager)
VPTR_CLASS(StubLinkStubManager)
VPTR_CLASS(ThePreStubManager)
VPTR_CLASS(VirtualCallStubManager)
VPTR_CLASS(VirtualCallStubManagerManager)
VPTR_CLASS(JumpStubStubManager)
VPTR_CLASS(RangeSectionStubManager)
VPTR_CLASS(ILStubManager)
VPTR_CLASS(InteropDispatchStubManager)
#if defined(TARGET_X86) && !defined(UNIX_X86_ABI)
VPTR_CLASS(TailCallStubManager)
#endif
VPTR_CLASS(CallCountingStubManager)

VPTR_CLASS(PEImageLayout)
VPTR_CLASS(ConvertedImageLayout)
VPTR_CLASS(LoadedImageLayout)
VPTR_CLASS(FlatImageLayout)

#ifdef DEBUGGING_SUPPORTED
VPTR_CLASS(Debugger)
VPTR_CLASS(EEDbgInterfaceImpl)
#endif // DEBUGGING_SUPPORTED

VPTR_CLASS(DebuggerController)
VPTR_CLASS(DebuggerMethodInfoTable)
VPTR_CLASS(DebuggerPatchTable)

VPTR_CLASS(LoaderCodeHeap)
VPTR_CLASS(HostCodeHeap)

VPTR_CLASS(GlobalLoaderAllocator)
VPTR_CLASS(AssemblyLoaderAllocator)
