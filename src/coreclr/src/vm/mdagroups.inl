// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
// Groups
//


// These are the MDAs that are on by-default when a debugger is attached.
// These ABSOLUTELY MUST NOT CHANGE BEHAVIOR. They must be purely checks
// with absolutely no sideeffects.
// Violating this will cause an app to behave differently under a debugger
// vs. not under a debugger, and that will really confuse end-users.
// (eg, "My app only does XYZ when running under a debugger."
// If you have any questions about this, please follow up with the
// managed debugger team for further guidance.
MDA_GROUP_DEFINITION(managedDebugger) 
    MDA_GROUP_MEMBER(AsynchronousThreadAbort)
    MDA_GROUP_MEMBER(BindingFailure)
    MDA_GROUP_MEMBER(CallbackOnCollectedDelegate)
    MDA_GROUP_MEMBER(ContextSwitchDeadlock)
    MDA_GROUP_MEMBER(DangerousThreadingAPI)
    MDA_GROUP_MEMBER(DateTimeInvalidLocalFormat)
    MDA_GROUP_MEMBER(DisconnectedContext)
    MDA_GROUP_MEMBER(DllMainReturnsFalse)
    MDA_GROUP_MEMBER(ExceptionSwallowedOnCallFromCom)
    MDA_GROUP_MEMBER(FailedQI)
    MDA_GROUP_MEMBER(FatalExecutionEngineError)    
    MDA_GROUP_MEMBER(InvalidApartmentStateChange)
    MDA_GROUP_MEMBER(InvalidFunctionPointerInDelegate)
    MDA_GROUP_MEMBER(InvalidMemberDeclaration)
    MDA_GROUP_MEMBER(InvalidOverlappedToPinvoke)
    MDA_GROUP_MEMBER(InvalidVariant)
    MDA_GROUP_MEMBER(LoaderLock)
    MDA_GROUP_MEMBER(LoadFromContext)
    MDA_GROUP_MEMBER(MarshalCleanupError)
    MDA_GROUP_MEMBER(NonComVisibleBaseClass)
    MDA_GROUP_MEMBER(NotMarshalable)
#ifdef _X86_ 
    MDA_GROUP_MEMBER(PInvokeStackImbalance)
#endif
    MDA_GROUP_MEMBER(RaceOnRCWCleanup)
    MDA_GROUP_MEMBER(Reentrancy)
    MDA_GROUP_MEMBER(ReleaseHandleFailed)
    MDA_GROUP_MEMBER(ReportAvOnComRelease)
    MDA_GROUP_MEMBER(StreamWriterBufferedDataLost)   
MDA_GROUP_DEFINITION_END(managedDebugger) 

MDA_GROUP_DEFINITION(unmanagedDebugger) 
    MDA_GROUP_MEMBER(Reentrancy)
    MDA_GROUP_MEMBER(LoaderLock)
MDA_GROUP_DEFINITION_END(unmanagedDebugger) 

MDA_GROUP_DEFINITION(halting) 
    MDA_GROUP_MEMBER(CallbackOnCollectedDelegate)
    MDA_GROUP_MEMBER(ContextSwitchDeadlock)
    MDA_GROUP_MEMBER(DateTimeInvalidLocalFormat)
    MDA_GROUP_MEMBER(DisconnectedContext)
    MDA_GROUP_MEMBER(FatalExecutionEngineError)    
    MDA_GROUP_MEMBER(InvalidFunctionPointerInDelegate)
    MDA_GROUP_MEMBER(InvalidMemberDeclaration)
    MDA_GROUP_MEMBER(InvalidVariant)
    MDA_GROUP_MEMBER(LoaderLock)
    MDA_GROUP_MEMBER(NonComVisibleBaseClass)
#ifdef _X86_ 
    MDA_GROUP_MEMBER(PInvokeStackImbalance)
#endif
    MDA_GROUP_MEMBER(RaceOnRCWCleanup)
    MDA_GROUP_MEMBER(Reentrancy)
MDA_GROUP_DEFINITION_END(halting) 

