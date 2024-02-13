// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// This is where we group together all the internal calls.
//

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Runtime.ExceptionServices
{
    internal static partial class InternalCalls
    {
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "SfiInit")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool RhpSfiInit(ref StackFrameIterator pThis, void* pStackwalkCtx, [MarshalAs(UnmanagedType.Bool)] bool instructionFault, bool* fIsExceptionIntercepted);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "SfiNext")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool RhpSfiNext(ref StackFrameIterator pThis, uint* uExCollideClauseIdx, bool* fUnwoundReversePInvoke, bool* fIsExceptionIntercepted);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ResumeAtInterceptionLocation")]
        internal static unsafe partial void ResumeAtInterceptionLocation(void* pvRegDisplay);

#pragma warning disable CS8500
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "CallCatchFunclet")]
        internal static unsafe partial IntPtr RhpCallCatchFunclet(
            ObjectHandleOnStack exceptionObj, byte* pHandlerIP, void* pvRegDisplay, EH.ExInfo* exInfo);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "CallFinallyFunclet")]
        internal static unsafe partial void RhpCallFinallyFunclet(byte* pHandlerIP, void* pvRegDisplay, EH.ExInfo* exInfo);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "CallFilterFunclet")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool RhpCallFilterFunclet(
            ObjectHandleOnStack exceptionObj, byte* pFilterIP, void* pvRegDisplay);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "AppendExceptionStackFrame")]
        internal static unsafe partial void RhpAppendExceptionStackFrame(ObjectHandleOnStack exceptionObj, IntPtr ip, UIntPtr sp, int flags, EH.ExInfo* exInfo);
#pragma warning restore CS8500

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "EHEnumInitFromStackFrameIterator")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool RhpEHEnumInitFromStackFrameIterator(ref StackFrameIterator pFrameIter, byte** pMethodStartAddress, void* pEHEnum);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "EHEnumNext")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool RhpEHEnumNext(void* pEHEnum, void* pEHClause);
    }
}
