// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#if NATIVEAOT
using Internal.Runtime;
#else
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
#endif

// Disable: Filter expression is a constant. We know. We just can't do an unfiltered catch.
#pragma warning disable 7095

namespace System.Runtime
{
    internal static unsafe partial class EH
    {
        internal static UIntPtr MaxSP
        {
            get
            {
                return (UIntPtr)(void*)(-1);
            }
        }

        private enum RhEHClauseKind
        {
            RH_EH_CLAUSE_TYPED = 0,
            RH_EH_CLAUSE_FAULT = 1,
            RH_EH_CLAUSE_FILTER = 2,
            RH_EH_CLAUSE_UNUSED = 3,
        }

        private struct RhEHClause
        {
            internal RhEHClauseKind _clauseKind;
            internal uint _tryStartOffset;
            internal uint _tryEndOffset;
            internal byte* _filterAddress;
            internal byte* _handlerAddress;
            internal void* _pTargetType;
#if !NATIVEAOT
            internal bool _isSameTry;
#endif

            ///<summary>
            /// We expect the stackwalker to adjust return addresses to point at 'return address - 1' so that we
            /// can use an interval here that is closed at the start and open at the end.  When a hardware fault
            /// occurs, the IP is pointing at the start of the instruction and will not be adjusted by the
            /// stackwalker.  Therefore, it will naturally work with an interval that has a closed start and open
            /// end.
            ///</summary>
            public bool ContainsCodeOffset(uint codeOffset)
            {
                return ((codeOffset >= _tryStartOffset) &&
                        (codeOffset < _tryEndOffset));
            }
        }

        [StructLayout(LayoutKind.Explicit, Size = AsmOffsets.SIZEOF__EHEnum)]
        private struct EHEnum
        {
            [FieldOffset(0)]
            private IntPtr _dummy; // For alignment
        }

#pragma warning disable IDE0060
        // This is a fail-fast function used by the runtime as a last resort that will terminate the process with
        // as little effort as possible. No guarantee is made about the semantics of this fail-fast.
#if !NATIVEAOT
        [DoesNotReturn]
#endif
        internal static void FallbackFailFast(RhFailFastReason reason, object? unhandledException)
        {
#if NATIVEAOT
            InternalCalls.RhpFallbackFailFast();
#else
            // TODO-NewEH: Debugging the FailFasts is difficult, debugger doesn't stop here, but
            // only after the process exits. The Debugger.Break() is a temporary solution to
            // help during development. Figure out how to do it properly.
            System.Diagnostics.Debugger.Break();
            Environment.FailFast(reason.ToString());
#endif
        }
#pragma warning restore IDE0060

        // Given an address pointing somewhere into a managed module, get the classlib-defined fail-fast
        // function and invoke it.  Any failure to find and invoke the function, or if it returns, results in
        // MRT-defined fail-fast behavior.
#if !NATIVEAOT
        [DoesNotReturn]
#endif
        internal static void FailFastViaClasslib(RhFailFastReason reason, object? unhandledException,
            IntPtr classlibAddress)
        {
#if NATIVEAOT
            // Find the classlib function that will fail fast. This is a RuntimeExport function from the
            // classlib module, and is therefore managed-callable.
            IntPtr pFailFastFunction = (IntPtr)InternalCalls.RhpGetClasslibFunctionFromCodeAddress(classlibAddress,
                                                                           ClassLibFunctionId.FailFast);

            if (pFailFastFunction == IntPtr.Zero)
            {
                // The classlib didn't provide a function, so we fail our way...
                FallbackFailFast(reason, unhandledException);
            }

            try
            {
                // Invoke the classlib fail fast function.
                ((delegate*<RhFailFastReason, object, IntPtr, IntPtr, void>)pFailFastFunction)
                    (reason, unhandledException, IntPtr.Zero, IntPtr.Zero);
            }
            catch when (true)
            {
                // disallow all exceptions leaking out of callbacks
            }

            // The classlib's function should never return and should not throw. If it does, then we fail our way...
#endif
            FallbackFailFast(reason, unhandledException);
        }

#if TARGET_AMD64
        [StructLayout(LayoutKind.Explicit, Size = 0x4d0)]
#elif TARGET_ARM
        [StructLayout(LayoutKind.Explicit, Size = 0x1a0)]
#elif TARGET_X86
        [StructLayout(LayoutKind.Explicit, Size = 0x2cc)]
#elif TARGET_ARM64
        [StructLayout(LayoutKind.Explicit, Size = 0x390)]
#else
        [StructLayout(LayoutKind.Explicit, Size = 0x10)] // this is small enough that it should trip an assert in RhpCopyContextFromExInfo
#endif
        private struct OSCONTEXT
        {
        }

        internal static unsafe void* PointerAlign(void* ptr, int alignmentInBytes)
        {
            int alignMask = alignmentInBytes - 1;
#if TARGET_64BIT
            return (void*)((((long)ptr) + alignMask) & ~alignMask);
#else
            return (void*)((((int)ptr) + alignMask) & ~alignMask);
#endif
        }

#if NATIVEAOT
        private static void OnFirstChanceExceptionViaClassLib(object exception)
        {
            IntPtr pOnFirstChanceFunction =
                (IntPtr)InternalCalls.RhpGetClasslibFunctionFromEEType(exception.GetMethodTable(), ClassLibFunctionId.OnFirstChance);

            if (pOnFirstChanceFunction == IntPtr.Zero)
            {
                return;
            }

            try
            {
                ((delegate*<object, void>)pOnFirstChanceFunction)(exception);
            }
            catch when (true)
            {
                // disallow all exceptions leaking out of callbacks
            }
        }
#endif // NATIVEAOT

        private static void OnUnhandledExceptionViaClassLib(object exception)
        {
#if NATIVEAOT
            IntPtr pOnUnhandledExceptionFunction =
                (IntPtr)InternalCalls.RhpGetClasslibFunctionFromEEType(exception.GetMethodTable(), ClassLibFunctionId.OnUnhandledException);

            if (pOnUnhandledExceptionFunction == IntPtr.Zero)
            {
                return;
            }

            try
            {
                ((delegate*<object, void>)pOnUnhandledExceptionFunction)(exception);
            }
            catch when (true)
            {
                // disallow all exceptions leaking out of callbacks
            }
#else
            Debug.Assert(false, "Unhandled exceptions should be processed by the native runtime only");
#endif
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static unsafe void UnhandledExceptionFailFastViaClasslib(
            RhFailFastReason reason, object unhandledException, IntPtr classlibAddress, ref ExInfo exInfo)
        {
#if NATIVEAOT
            IntPtr pFailFastFunction =
                (IntPtr)InternalCalls.RhpGetClasslibFunctionFromCodeAddress(classlibAddress, ClassLibFunctionId.FailFast);

            if (pFailFastFunction == IntPtr.Zero)
            {
                FailFastViaClasslib(
                    reason,
                    unhandledException,
                    classlibAddress);
            }

            // 16-byte align the context.  This is overkill on x86 and ARM, but simplifies things slightly.
            const int contextAlignment = 16;
            byte* pbBuffer = stackalloc byte[sizeof(OSCONTEXT) + contextAlignment];
            void* pContext = PointerAlign(pbBuffer, contextAlignment);

            InternalCalls.RhpCopyContextFromExInfo(pContext, sizeof(OSCONTEXT), exInfo._pExContext);

            try
            {
                ((delegate*<RhFailFastReason, object, IntPtr, void*, void>)pFailFastFunction)
                    (reason, unhandledException, exInfo._pExContext->IP, pContext);
            }
            catch when (true)
            {
                // disallow all exceptions leaking out of callbacks
            }

            // The classlib's function should never return and should not throw. If it does, then we fail our way...
            FallbackFailFast(reason, unhandledException);
#else
            FailFastViaClasslib(
                reason,
                unhandledException,
                classlibAddress);
#endif
        }

        private enum RhEHFrameType
        {
            RH_EH_FIRST_FRAME = 1,
            RH_EH_FIRST_RETHROW_FRAME = 2,
        }

        private static void AppendExceptionStackFrameViaClasslib(object exception, IntPtr ip,
            UIntPtr sp, ref ExInfo exInfo,
            ref bool isFirstRethrowFrame, ref bool isFirstFrame)
        {
            int flags = (isFirstFrame ? (int)RhEHFrameType.RH_EH_FIRST_FRAME : 0) |
                        (isFirstRethrowFrame ? (int)RhEHFrameType.RH_EH_FIRST_RETHROW_FRAME : 0);
#if NATIVEAOT
            IntPtr pAppendStackFrame = (IntPtr)InternalCalls.RhpGetClasslibFunctionFromCodeAddress(ip,
                ClassLibFunctionId.AppendExceptionStackFrame);

            if (pAppendStackFrame != IntPtr.Zero)
            {
                try
                {
                    ((delegate*<object, IntPtr, int, void>)pAppendStackFrame)(exception, ip, flags);
                }
                catch when (true)
                {
                    // disallow all exceptions leaking out of callbacks
                }

                // Clear flags only if we called the function
                isFirstRethrowFrame = false;
                isFirstFrame = false;
            }
#else
#pragma warning disable CS8500
            fixed (EH.ExInfo* pExInfo = &exInfo)
            {
                InternalCalls.RhpAppendExceptionStackFrame(ObjectHandleOnStack.Create(ref exception), ip, sp, flags, pExInfo);
            }
#pragma warning restore CS8500
            // Clear flags only if we called the function
            isFirstRethrowFrame = false;
            isFirstFrame = false;
#endif
        }

        // Given an ExceptionID and an address pointing somewhere into a managed module, get
        // an exception object of a type that the module containing the given address will understand.
        // This finds the classlib-defined GetRuntimeException function and asks it for the exception object.
        internal static Exception GetClasslibException(ExceptionIDs id, IntPtr address)
        {
#if NATIVEAOT
            // Find the classlib function that will give us the exception object we want to throw. This
            // is a RuntimeExport function from the classlib module, and is therefore managed-callable.
            IntPtr pGetRuntimeExceptionFunction =
                (IntPtr)InternalCalls.RhpGetClasslibFunctionFromCodeAddress(address, ClassLibFunctionId.GetRuntimeException);

            // Return the exception object we get from the classlib.
            Exception? e = null;
            try
            {
                e = ((delegate*<ExceptionIDs, Exception>)pGetRuntimeExceptionFunction)(id);
            }
            catch when (true)
            {
                // disallow all exceptions leaking out of callbacks
            }
#else
            Exception? e = id switch
            {
                ExceptionIDs.AccessViolation => new AccessViolationException(),
                ExceptionIDs.Arithmetic => new ArithmeticException(),
                ExceptionIDs.AmbiguousImplementation => new AmbiguousImplementationException(),
                ExceptionIDs.ArrayTypeMismatch => new ArrayTypeMismatchException(),
                ExceptionIDs.DataMisaligned => new DataMisalignedException(),
                ExceptionIDs.DivideByZero => new DivideByZeroException(),
                ExceptionIDs.EntrypointNotFound => new EntryPointNotFoundException(),
                ExceptionIDs.IndexOutOfRange => new IndexOutOfRangeException(),
                ExceptionIDs.InvalidCast => new InvalidCastException(),
                ExceptionIDs.NullReference => new NullReferenceException(),
                ExceptionIDs.OutOfMemory => new OutOfMemoryException(),
                ExceptionIDs.Overflow => new OverflowException(),
                _ => null
            };
#endif
            // If the helper fails to yield an object, then we fail-fast.
            if (e == null)
            {
                FailFastViaClasslib(RhFailFastReason.InternalError, null, address);
            }

            return e;
        }
#if NATIVEAOT
        // Given an ExceptionID and an MethodTable address, get an exception object of a type that the module containing
        // the given address will understand. This finds the classlib-defined GetRuntimeException function and asks
        // it for the exception object.
        internal static Exception GetClasslibExceptionFromEEType(ExceptionIDs id, MethodTable* pEEType)
        {
            // Find the classlib function that will give us the exception object we want to throw. This
            // is a RuntimeExport function from the classlib module, and is therefore managed-callable.
            IntPtr pGetRuntimeExceptionFunction = IntPtr.Zero;
            if (pEEType != null)
            {
                pGetRuntimeExceptionFunction = (IntPtr)InternalCalls.RhpGetClasslibFunctionFromEEType(pEEType, ClassLibFunctionId.GetRuntimeException);
            }

            // Return the exception object we get from the classlib.
            Exception? e = null;
            try
            {
                e = ((delegate*<ExceptionIDs, Exception>)pGetRuntimeExceptionFunction)(id);
            }
            catch when (true)
            {
                // disallow all exceptions leaking out of callbacks
            }

            // If the helper fails to yield an object, then we fail-fast.
            if (e == null)
            {
                FailFastViaClasslib(RhFailFastReason.InternalError, null, (IntPtr)pEEType);
            }

            return e;
        }

        // RhExceptionHandling_ functions are used to throw exceptions out of our asm helpers. We tail-call from
        // the asm helpers to these functions, which performs the throw. The tail-call is important: it ensures that
        // the stack is crawlable from within these functions.
        [RuntimeExport("RhExceptionHandling_ThrowClasslibOverflowException")]
        public static void ThrowClasslibOverflowException(IntPtr address)
        {
            // Throw the overflow exception defined by the classlib, using the return address of the asm helper
            // to find the correct classlib.

            throw GetClasslibException(ExceptionIDs.Overflow, address);
        }

        [RuntimeExport("RhExceptionHandling_ThrowClasslibDivideByZeroException")]
        public static void ThrowClasslibDivideByZeroException(IntPtr address)
        {
            // Throw the divide by zero exception defined by the classlib, using the return address of the asm helper
            // to find the correct classlib.

            throw GetClasslibException(ExceptionIDs.DivideByZero, address);
        }

        [RuntimeExport("RhExceptionHandling_FailedAllocation")]
        public static void FailedAllocation(EETypePtr pEEType, bool fIsOverflow)
        {
            ExceptionIDs exID = fIsOverflow ? ExceptionIDs.Overflow : ExceptionIDs.OutOfMemory;

            // Throw the out of memory exception defined by the classlib, using the input MethodTable*
            // to find the correct classlib.

            throw pEEType.ToPointer()->GetClasslibException(exID);
        }

#if !INPLACE_RUNTIME
        private static OutOfMemoryException s_theOOMException = new OutOfMemoryException();

        // MRT exports GetRuntimeException for the few cases where we have a helper that throws an exception
        // and may be called by either MRT or other classlibs and that helper needs to throw an exception.
        // There are only a few cases where this happens now (the fast allocation helpers), so we limit the
        // exception types that MRT will return.
        [RuntimeExport("GetRuntimeException")]
        public static Exception GetRuntimeException(ExceptionIDs id)
        {
            switch (id)
            {
                case ExceptionIDs.OutOfMemory:
                    // Throw a preallocated exception to avoid infinite recursion.
                    return s_theOOMException;

                case ExceptionIDs.Overflow:
                    return new OverflowException();

                case ExceptionIDs.InvalidCast:
                    return new InvalidCastException();

                default:
                    Debug.Assert(false, "unexpected ExceptionID");
                    FallbackFailFast(RhFailFastReason.InternalError, null);
                    return null;
            }
        }
#endif
#endif // NATIVEAOT

        private enum HwExceptionCode : uint
        {
            STATUS_REDHAWK_NULL_REFERENCE = 0x00000000u,
            STATUS_REDHAWK_UNMANAGED_HELPER_NULL_REFERENCE = 0x00000042u,
            STATUS_REDHAWK_THREAD_ABORT = 0x00000043u,

            STATUS_DATATYPE_MISALIGNMENT = 0x80000002u,
            STATUS_ACCESS_VIOLATION = 0xC0000005u,
            STATUS_INTEGER_DIVIDE_BY_ZERO = 0xC0000094u,
            STATUS_INTEGER_OVERFLOW = 0xC0000095u,
        }
        [StructLayout(LayoutKind.Explicit, Size = AsmOffsets.SIZEOF__PAL_LIMITED_CONTEXT)]
        public struct PAL_LIMITED_CONTEXT
        {
            [FieldOffset(AsmOffsets.OFFSETOF__PAL_LIMITED_CONTEXT__IP)]
            internal IntPtr IP;
#if !NATIVEAOT
            [FieldOffset(AsmOffsets.OFFSETOF__PAL_LIMITED_CONTEXT__FP)]
            internal UIntPtr FP;
#endif
            // the rest of the struct is left unspecified.
        }

        // N.B. -- These values are burned into the throw helper assembly code and are also known the the
        //         StackFrameIterator code.
        [Flags]
        internal enum ExKind : byte
        {
            None = 0,
            Throw = 1,
            HardwareFault = 2,
            KindMask = 3,

            RethrowFlag = 4,

            SupersededFlag = 8,

            InstructionFaultFlag = 0x10
        }

        [StructLayout(LayoutKind.Explicit)]
        public ref struct ExInfo
        {
            internal void Init(object exceptionObj, bool instructionFault = false)
            {
                // _pPrevExInfo    -- set by asm helper
                // _pExContext     -- set by asm helper
                // _passNumber     -- set by asm helper
                // _kind           -- set by asm helper
                // _idxCurClause   -- set by asm helper
                // _frameIter      -- initialized explicitly during dispatch

                _exception = exceptionObj;
                if (instructionFault)
                    _kind |= ExKind.InstructionFaultFlag;
                _notifyDebuggerSP = UIntPtr.Zero;
            }

            internal void Init(object exceptionObj, ref ExInfo rethrownExInfo)
            {
                // _pPrevExInfo    -- set by asm helper
                // _pExContext     -- set by asm helper
                // _passNumber     -- set by asm helper
                // _idxCurClause   -- set by asm helper
                // _frameIter      -- initialized explicitly during dispatch

                _exception = exceptionObj;
                _kind = rethrownExInfo._kind | ExKind.RethrowFlag;
                _notifyDebuggerSP = UIntPtr.Zero;
            }

            internal object ThrownException
            {
                get
                {
                    return _exception;
                }
            }

            [FieldOffset(AsmOffsets.OFFSETOF__ExInfo__m_pPrevExInfo)]
            internal void* _pPrevExInfo;

            [FieldOffset(AsmOffsets.OFFSETOF__ExInfo__m_pExContext)]
            internal PAL_LIMITED_CONTEXT* _pExContext;

            [FieldOffset(AsmOffsets.OFFSETOF__ExInfo__m_exception)]
            private object _exception;  // actual object reference, specially reported by GcScanRootsWorker

            [FieldOffset(AsmOffsets.OFFSETOF__ExInfo__m_kind)]
            internal ExKind _kind;

            [FieldOffset(AsmOffsets.OFFSETOF__ExInfo__m_passNumber)]
            internal byte _passNumber;

            // BEWARE: This field is used by the stackwalker to know if the dispatch code has reached the
            //         point at which a handler is called.  In other words, it serves as an "is a handler
            //         active" state where '_idxCurClause == MaxTryRegionIdx' means 'no'.
            [FieldOffset(AsmOffsets.OFFSETOF__ExInfo__m_idxCurClause)]
            internal uint _idxCurClause;

            [FieldOffset(AsmOffsets.OFFSETOF__ExInfo__m_frameIter)]
            internal StackFrameIterator _frameIter;

            [FieldOffset(AsmOffsets.OFFSETOF__ExInfo__m_notifyDebuggerSP)]
            internal volatile UIntPtr _notifyDebuggerSP;
        }

        //
        // Called by RhpThrowHwEx
        //
#if NATIVEAOT
        [RuntimeExport("RhThrowHwEx")]
#endif
        public static void RhThrowHwEx(uint exceptionCode, ref ExInfo exInfo)
        {
#if NATIVEAOT
            // trigger a GC (only if gcstress) to ensure we can stackwalk at this point
            GCStress.TriggerGC();

            InternalCalls.RhpValidateExInfoStack();
#endif
            IntPtr faultingCodeAddress = exInfo._pExContext->IP;
            bool instructionFault = true;
            ExceptionIDs exceptionId = default(ExceptionIDs);
            Exception? exceptionToThrow = null;

            switch (exceptionCode)
            {
                case (uint)HwExceptionCode.STATUS_REDHAWK_NULL_REFERENCE:
                    exceptionId = ExceptionIDs.NullReference;
                    break;

                case (uint)HwExceptionCode.STATUS_REDHAWK_UNMANAGED_HELPER_NULL_REFERENCE:
                    // The write barrier where the actual fault happened has been unwound already.
                    // The IP of this fault needs to be treated as return address, not as IP of
                    // faulting instruction.
                    instructionFault = false;
                    exceptionId = ExceptionIDs.NullReference;
                    break;

#if NATIVEAOT
                case (uint)HwExceptionCode.STATUS_REDHAWK_THREAD_ABORT:
                    exceptionToThrow = InternalCalls.RhpGetThreadAbortException();
                    break;
#endif

                case (uint)HwExceptionCode.STATUS_DATATYPE_MISALIGNMENT:
                    exceptionId = ExceptionIDs.DataMisaligned;
                    break;

                // N.B. -- AVs that have a read/write address lower than 64k are already transformed to
                //         HwExceptionCode.REDHAWK_NULL_REFERENCE prior to calling this routine.
                case (uint)HwExceptionCode.STATUS_ACCESS_VIOLATION:
                    exceptionId = ExceptionIDs.AccessViolation;
                    break;

                case (uint)HwExceptionCode.STATUS_INTEGER_DIVIDE_BY_ZERO:
                    exceptionId = ExceptionIDs.DivideByZero;
                    break;

                case (uint)HwExceptionCode.STATUS_INTEGER_OVERFLOW:
                    exceptionId = ExceptionIDs.Overflow;
                    break;

                default:
                    // We don't wrap SEH exceptions from foreign code like CLR does, so we believe that we
                    // know the complete set of HW faults generated by managed code and do not need to handle
                    // this case.
                    FailFastViaClasslib(RhFailFastReason.InternalError, null, faultingCodeAddress);
                    break;
            }

            if (exceptionId != default(ExceptionIDs))
            {
                exceptionToThrow = GetClasslibException(exceptionId, faultingCodeAddress);
            }

            exInfo.Init(exceptionToThrow!, instructionFault);
            DispatchEx(ref exInfo._frameIter, ref exInfo);
            FallbackFailFast(RhFailFastReason.InternalError, null);
        }

        private const uint MaxTryRegionIdx = 0xFFFFFFFFu;

#if NATIVEAOT
        [RuntimeExport("RhThrowEx")]
#endif
        public static void RhThrowEx(object exceptionObj, ref ExInfo exInfo)
        {
#if NATIVEAOT
            // trigger a GC (only if gcstress) to ensure we can stackwalk at this point
            GCStress.TriggerGC();

            InternalCalls.RhpValidateExInfoStack();
#endif
            // Transform attempted throws of null to a throw of NullReferenceException.
            if (exceptionObj == null)
            {
                IntPtr faultingCodeAddress = exInfo._pExContext->IP;
                exceptionObj = GetClasslibException(ExceptionIDs.NullReference, faultingCodeAddress);
            }

            exInfo.Init(exceptionObj);
            DispatchEx(ref exInfo._frameIter, ref exInfo);
            FallbackFailFast(RhFailFastReason.InternalError, null);
        }
#if !NATIVEAOT
        public static void RhUnwindAndIntercept(ref ExInfo exInfo, UIntPtr interceptStackFrameSP)
        {
            exInfo._passNumber = 2;
            exInfo._idxCurClause = MaxTryRegionIdx;
            uint startIdx = MaxTryRegionIdx;
            bool unwoundReversePInvoke = false;
            bool isExceptionIntercepted = false;
            bool isValid = exInfo._frameIter.Init(exInfo._pExContext, (exInfo._kind & ExKind.InstructionFaultFlag) != 0, &isExceptionIntercepted);
            for (; isValid && !isExceptionIntercepted && ((byte*)exInfo._frameIter.SP <= (byte*)interceptStackFrameSP); isValid = exInfo._frameIter.Next(&startIdx, &unwoundReversePInvoke, &isExceptionIntercepted))
            {
                Debug.Assert(isValid, "Unwind and intercept failed unexpectedly");
                DebugScanCallFrame(exInfo._passNumber, exInfo._frameIter.ControlPC, exInfo._frameIter.SP);

                if (unwoundReversePInvoke)
                {
                    // Found the native frame that called the reverse P/invoke.
                    // It is not possible to run managed second pass handlers on a native frame.
                    break;
                }

                if (exInfo._frameIter.SP == interceptStackFrameSP)
                {
                    break;
                }

                InvokeSecondPass(ref exInfo, startIdx);
                if (isExceptionIntercepted)
                {
                    Debug.Assert(false);
                    break;
                }
            }

            // ------------------------------------------------
            //
            // Call the interception code
            //
            // ------------------------------------------------
            if (unwoundReversePInvoke)
            {
                object exceptionObj = exInfo.ThrownException;
#pragma warning disable CS8500
                fixed (EH.ExInfo* pExInfo = &exInfo)
                {
                    InternalCalls.RhpCallCatchFunclet(
                        ObjectHandleOnStack.Create(ref exceptionObj), null, exInfo._frameIter.RegisterSet, pExInfo);
                }
#pragma warning restore CS8500
            }
            else
            {
                InternalCalls.ResumeAtInterceptionLocation(exInfo._frameIter.RegisterSet);
            }

            Debug.Assert(false, "unreachable");
            FallbackFailFast(RhFailFastReason.InternalError, null);
        }
#endif // !NATIVEAOT


#if NATIVEAOT
        [RuntimeExport("RhRethrow")]
#endif
        public static void RhRethrow(ref ExInfo activeExInfo, ref ExInfo exInfo)
        {
#if NATIVEAOT
            // trigger a GC (only if gcstress) to ensure we can stackwalk at this point
            GCStress.TriggerGC();

            InternalCalls.RhpValidateExInfoStack();
#endif
            // We need to copy the exception object to this stack location because collided unwinds
            // will cause the original stack location to go dead.
            object rethrownException = activeExInfo.ThrownException;

            exInfo.Init(rethrownException, ref activeExInfo);
            DispatchEx(ref exInfo._frameIter, ref exInfo);
            FallbackFailFast(RhFailFastReason.InternalError, null);
        }

        private static void DispatchEx(scoped ref StackFrameIterator frameIter, ref ExInfo exInfo)
        {
            Debug.Assert(exInfo._passNumber == 1, "expected asm throw routine to set the pass");
            object exceptionObj = exInfo.ThrownException;

            // ------------------------------------------------
            //
            // First pass
            //
            // ------------------------------------------------
            UIntPtr handlingFrameSP = MaxSP;
            byte* pCatchHandler = null;
            uint catchingTryRegionIdx = MaxTryRegionIdx;

            bool isFirstRethrowFrame = (exInfo._kind & ExKind.RethrowFlag) != 0;
            bool isFirstFrame = true;
            bool isExceptionIntercepted = false;

            byte* prevControlPC = null;
            byte* prevOriginalPC = null;
            UIntPtr prevFramePtr = UIntPtr.Zero;
            bool unwoundReversePInvoke = false;
            IntPtr pReversePInvokePropagationCallback = IntPtr.Zero;
            IntPtr pReversePInvokePropagationContext = IntPtr.Zero;

            bool isValid = frameIter.Init(exInfo._pExContext, (exInfo._kind & ExKind.InstructionFaultFlag) != 0, &isExceptionIntercepted);
            Debug.Assert(isValid, "RhThrowEx called with an unexpected context");

#if NATIVEAOT
            OnFirstChanceExceptionViaClassLib(exceptionObj);
#endif
            uint startIdx = MaxTryRegionIdx;
            for (; isValid; isValid = frameIter.Next(&startIdx, &unwoundReversePInvoke, &isExceptionIntercepted))
            {
                // For GC stackwalking, we'll happily walk across native code blocks, but for EH dispatch, we
                // disallow dispatching exceptions across native code.
                if (unwoundReversePInvoke)
                    break;

                if (isExceptionIntercepted)
                {
                    break;
                }

                prevControlPC = frameIter.ControlPC;
                prevOriginalPC = frameIter.OriginalControlPC;

                DebugScanCallFrame(exInfo._passNumber, frameIter.ControlPC, frameIter.SP);

                UpdateStackTrace(exceptionObj, exInfo._frameIter.FramePointer, (IntPtr)frameIter.OriginalControlPC, frameIter.SP, ref isFirstRethrowFrame, ref prevFramePtr, ref isFirstFrame, ref exInfo);

                byte* pHandler;
                if (FindFirstPassHandler(exceptionObj, startIdx, ref frameIter,
                                         out catchingTryRegionIdx, out pHandler))
                {
                    handlingFrameSP = frameIter.SP;
                    pCatchHandler = pHandler;

                    DebugVerifyHandlingFrame(handlingFrameSP);
                    break;
                }
            }

            if (unwoundReversePInvoke)
            {
#if NATIVEAOT
#if FEATURE_OBJCMARSHAL
                // We did not find any managed handlers before hitting a reverse P/Invoke boundary.
                // See if the classlib has a handler to propagate the exception to native code.
                IntPtr pGetHandlerClasslibFunction = (IntPtr)InternalCalls.RhpGetClasslibFunctionFromCodeAddress((IntPtr)prevControlPC,
                    ClassLibFunctionId.ObjectiveCMarshalGetUnhandledExceptionPropagationHandler);
                if (pGetHandlerClasslibFunction != IntPtr.Zero)
                {
                    var pGetHandler = (delegate*<object, IntPtr, out IntPtr, IntPtr>)pGetHandlerClasslibFunction;
                    pReversePInvokePropagationCallback = pGetHandler(
                        exceptionObj, (IntPtr)prevControlPC, out pReversePInvokePropagationContext);
                    if (pReversePInvokePropagationCallback != IntPtr.Zero)
                    {
                        // Tell the second pass to unwind to this frame.
                        handlingFrameSP = frameIter.SP;
                        catchingTryRegionIdx = MaxTryRegionIdx;
                    }
                }
#endif // FEATURE_OBJCMARSHAL
#else // !NATIVEAOT
                handlingFrameSP = frameIter.SP;
                catchingTryRegionIdx = MaxTryRegionIdx;
#endif // !NATIVEAOT
            }

            if (pCatchHandler == null && pReversePInvokePropagationCallback == IntPtr.Zero && !isExceptionIntercepted
#if !NATIVEAOT
                && !unwoundReversePInvoke
#endif
            )
            {
                OnUnhandledExceptionViaClassLib(exceptionObj);

                UnhandledExceptionFailFastViaClasslib(
                    RhFailFastReason.UnhandledException,
                    exceptionObj,
                    (IntPtr)prevOriginalPC, // IP of the last frame that did not handle the exception
                    ref exInfo);
            }

            // We FailFast above if the exception goes unhandled.  Therefore, we cannot run the second pass
            // without a catch handler or propagation callback.
            Debug.Assert(pCatchHandler != null || pReversePInvokePropagationCallback != IntPtr.Zero || unwoundReversePInvoke || isExceptionIntercepted, "We should have a handler if we're starting the second pass");
            Debug.Assert(!isExceptionIntercepted || (pCatchHandler == null), "No catch handler should be returned for intercepted exceptions in the first pass");

            // ------------------------------------------------
            //
            // Second pass
            //
            // ------------------------------------------------

            // Due to the stackwalker logic, we cannot tolerate triggering a GC from the dispatch code once we
            // are in the 2nd pass.  This is because the stackwalker applies a particular unwind semantic to
            // 'collapse' funclets which gets confused when we walk out of the dispatch code and encounter the
            // 'main body' without first encountering the funclet.  The thunks used to invoke 2nd-pass
            // funclets will always toggle this mode off before invoking them.
#if NATIVEAOT
            InternalCalls.RhpSetThreadDoNotTriggerGC();
#endif
            exInfo._passNumber = 2;
            exInfo._idxCurClause = catchingTryRegionIdx;
            startIdx = MaxTryRegionIdx;
            unwoundReversePInvoke = false;
            isExceptionIntercepted = false;
            isValid = frameIter.Init(exInfo._pExContext, (exInfo._kind & ExKind.InstructionFaultFlag) != 0, &isExceptionIntercepted);
            for (; isValid && ((byte*)frameIter.SP <= (byte*)handlingFrameSP); isValid = frameIter.Next(&startIdx, &unwoundReversePInvoke, &isExceptionIntercepted))
            {
                Debug.Assert(isValid, "second-pass EH unwind failed unexpectedly");
                DebugScanCallFrame(exInfo._passNumber, frameIter.ControlPC, frameIter.SP);

                if (isExceptionIntercepted)
                {
                    pCatchHandler = null;
                    break;
                }

                if (unwoundReversePInvoke)
                {
#if NATIVEAOT
                    Debug.Assert(pReversePInvokePropagationCallback != IntPtr.Zero, "Unwound to a reverse P/Invoke in the second pass. We should have a propagation handler.");
                    Debug.Assert(frameIter.PreviousTransitionFrame != IntPtr.Zero, "Should have a transition frame for reverse P/Invoke.");
#endif
                    Debug.Assert(frameIter.SP == handlingFrameSP, "Encountered a different reverse P/Invoke frame in the second pass.");
                    // Found the native frame that called the reverse P/invoke.
                    // It is not possible to run managed second pass handlers on a native frame.
                    break;
                }

                if ((frameIter.SP == handlingFrameSP)
#if TARGET_ARM64
                    && (frameIter.ControlPC == prevControlPC)
#endif
                    )
                {
                    // invoke only a partial second-pass here...
                    InvokeSecondPass(ref exInfo, startIdx, catchingTryRegionIdx);
                    break;
                }

                InvokeSecondPass(ref exInfo, startIdx);
            }

#if FEATURE_OBJCMARSHAL
            if (pReversePInvokePropagationCallback != IntPtr.Zero)
            {
#if NATIVEAOT
                InternalCalls.RhpCallPropagateExceptionCallback(
                    pReversePInvokePropagationContext, pReversePInvokePropagationCallback, frameIter.RegisterSet, ref exInfo, frameIter.PreviousTransitionFrame);
                // the helper should jump to propagation handler and not return
#endif
                Debug.Assert(false, "unreachable");
                FallbackFailFast(RhFailFastReason.InternalError, null);
            }
#endif // FEATURE_OBJCMARSHAL


            // ------------------------------------------------
            //
            // Call the handler and resume execution
            //
            // ------------------------------------------------
            exInfo._idxCurClause = catchingTryRegionIdx;
#if NATIVEAOT
            InternalCalls.RhpCallCatchFunclet(
                exceptionObj, pCatchHandler, frameIter.RegisterSet, ref exInfo);
#else // NATIVEAOT
#pragma warning disable CS8500
            fixed (EH.ExInfo* pExInfo = &exInfo)
            {
                InternalCalls.RhpCallCatchFunclet(
                    ObjectHandleOnStack.Create(ref exceptionObj), pCatchHandler, frameIter.RegisterSet, pExInfo);
            }
#pragma warning restore CS8500
#endif // NATIVEAOT
            // currently, RhpCallCatchFunclet will resume after the catch
            Debug.Assert(false, "unreachable");
            FallbackFailFast(RhFailFastReason.InternalError, null);
        }

        [System.Diagnostics.Conditional("DEBUG")]
        private static void DebugScanCallFrame(int passNumber, byte* ip, UIntPtr sp)
        {
            Debug.Assert(ip != null, "IP address must not be null");
        }

        [System.Diagnostics.Conditional("DEBUG")]
        private static void DebugVerifyHandlingFrame(UIntPtr handlingFrameSP)
        {
            Debug.Assert(handlingFrameSP != MaxSP, "Handling frame must have an SP value");
            Debug.Assert(((UIntPtr*)handlingFrameSP) > &handlingFrameSP,
                "Handling frame must have a valid stack frame pointer");
        }

        private static void UpdateStackTrace(object exceptionObj, UIntPtr curFramePtr, IntPtr ip, UIntPtr sp,
            ref bool isFirstRethrowFrame, ref UIntPtr prevFramePtr, ref bool isFirstFrame, ref ExInfo exInfo)
        {
            // We use the fact that all funclet stack frames belonging to the same logical method activation
            // will have the same FramePointer value.  Additionally, the stackwalker will return a sequence of
            // callbacks for all the funclet stack frames, one right after the other.  The classlib doesn't
            // want to know about funclets, so we strip them out by only reporting the first frame of a
            // sequence of funclets.  This is correct because the leafmost funclet is first in the sequence
            // and corresponds to the current 'IP state' of the method.
#if NATIVEAOT
            if ((prevFramePtr == UIntPtr.Zero) || (curFramePtr != prevFramePtr))
#endif
            {
                AppendExceptionStackFrameViaClasslib(exceptionObj, ip, sp, ref exInfo,
                    ref isFirstRethrowFrame, ref isFirstFrame);
            }
            prevFramePtr = curFramePtr;
        }

        private static bool FindFirstPassHandler(object exception, uint idxStart,
            ref StackFrameIterator frameIter, out uint tryRegionIdx, out byte* pHandler)
        {
            pHandler = null;
            tryRegionIdx = MaxTryRegionIdx;

            EHEnum ehEnum;
            byte* pbMethodStartAddress;
            if (!InternalCalls.RhpEHEnumInitFromStackFrameIterator(ref frameIter, &pbMethodStartAddress, &ehEnum))
                return false;

            byte* pbControlPC = frameIter.ControlPC;

            uint codeOffset = (uint)(pbControlPC - pbMethodStartAddress);

            uint lastTryStart = 0, lastTryEnd = 0;

            // Search the clauses for one that contains the current offset.
            RhEHClause ehClause;
            for (uint curIdx = 0; InternalCalls.RhpEHEnumNext(&ehEnum, &ehClause); curIdx++)
            {
                //
                // Skip to the starting try region.  This is used by collided unwinds and rethrows to pickup where
                // the previous dispatch left off.
                //
                if (idxStart != MaxTryRegionIdx)
                {
                    if (curIdx <= idxStart)
                    {
                        lastTryStart = ehClause._tryStartOffset; lastTryEnd = ehClause._tryEndOffset;
                        continue;
                    }

                    // Now, we continue skipping while the try region is identical to the one that invoked the
                    // previous dispatch.
                    if ((ehClause._tryStartOffset == lastTryStart) && (ehClause._tryEndOffset == lastTryEnd)
#if !NATIVEAOT
                        && (ehClause._isSameTry)
#endif
                    )
                        continue;

                    // We are done skipping. This is required to handle empty finally block markers that are used
                    // to separate runs of different try blocks with same native code offsets.
                    idxStart = MaxTryRegionIdx;
                }

                RhEHClauseKind clauseKind = ehClause._clauseKind;

                if (((clauseKind != RhEHClauseKind.RH_EH_CLAUSE_TYPED) &&
                     (clauseKind != RhEHClauseKind.RH_EH_CLAUSE_FILTER))
                    || !ehClause.ContainsCodeOffset(codeOffset))
                {
                    continue;
                }

                // Found a containing clause. Because of the order of the clauses, we know this is the
                // most containing.
                if (clauseKind == RhEHClauseKind.RH_EH_CLAUSE_TYPED)
                {
                    if (ShouldTypedClauseCatchThisException(exception, (MethodTable*)ehClause._pTargetType, !frameIter.IsRuntimeWrappedExceptions))
                    {
                        pHandler = ehClause._handlerAddress;
                        tryRegionIdx = curIdx;
                        return true;
                    }
                }
                else
                {
                    byte* pFilterFunclet = ehClause._filterAddress;

                    bool shouldInvokeHandler = false;
#if NATIVEAOT
                    try
                    {
                        shouldInvokeHandler =
                            InternalCalls.RhpCallFilterFunclet(exception, pFilterFunclet, frameIter.RegisterSet);
                    }
                    catch when (true)
                    {
                        // Prevent leaking any exception from the filter funclet
                    }
#else // NATIVEAOT
                    shouldInvokeHandler =
                        InternalCalls.RhpCallFilterFunclet(ObjectHandleOnStack.Create(ref exception), pFilterFunclet, frameIter.RegisterSet);
#endif // NATIVEAOT

                    if (shouldInvokeHandler)
                    {
                        pHandler = ehClause._handlerAddress;
                        tryRegionIdx = curIdx;
                        return true;
                    }
                }
            }

            return false;
        }

#if DEBUG && !INPLACE_RUNTIME && NATIVEAOT
        private static MethodTable* s_pLowLevelObjectType;
        private static void AssertNotRuntimeObject(MethodTable* pClauseType)
        {
            //
            // The C# try { } catch { } clause expands into a typed catch of System.Object.
            // Since runtime has its own definition of System.Object, try { } catch { } might not do what
            // was intended (catch all exceptions).
            //
            // This assertion is making sure we don't use try { } catch { } within the runtime.
            // The runtime codebase should either use try { } catch (Exception) { } for exception types
            // from the runtime or a try { } catch when (true) { } to catch all exceptions.
            //

            if (s_pLowLevelObjectType == null)
            {
                // Allocating might fail, but since this is just a debug assert, it's probably fine.
                s_pLowLevelObjectType = new System.Object().MethodTable;
            }

            Debug.Assert(!pClauseType->IsEquivalentTo(s_pLowLevelObjectType));
        }
#endif // DEBUG && !INPLACE_RUNTIME && NATIVEAOT


        private static bool ShouldTypedClauseCatchThisException(object exception, MethodTable* pClauseType, bool tryUnwrapException)
        {
#if NATIVEAOT
#if DEBUG && !INPLACE_RUNTIME
            AssertNotRuntimeObject(pClauseType);
#endif

            return TypeCast.IsInstanceOfException(pClauseType, exception);
#else
            bool retry = false;
            do
            {
                MethodTable* mt = RuntimeHelpers.GetMethodTable(exception);
                while (mt != null)
                {
                    if (pClauseType == mt)
                    {
                        return true;
                    }

                    mt = mt->ParentMethodTable;
                }

                if (tryUnwrapException && exception is RuntimeWrappedException ex)
                {
                    exception = ex.WrappedException;
                    retry = true;
                }
            }
            while (retry);

            return false;
#endif
        }

        private static void InvokeSecondPass(ref ExInfo exInfo, uint idxStart)
        {
            InvokeSecondPass(ref exInfo, idxStart, MaxTryRegionIdx);
        }
        private static void InvokeSecondPass(ref ExInfo exInfo, uint idxStart, uint idxLimit)
        {
            EHEnum ehEnum;
            byte* pbMethodStartAddress;
            if (!InternalCalls.RhpEHEnumInitFromStackFrameIterator(ref exInfo._frameIter, &pbMethodStartAddress, &ehEnum))
                return;

            byte* pbControlPC = exInfo._frameIter.ControlPC;

            uint codeOffset = (uint)(pbControlPC - pbMethodStartAddress);

            uint lastTryStart = 0, lastTryEnd = 0;

            // Search the clauses for one that contains the current offset.
            RhEHClause ehClause;
            for (uint curIdx = 0; InternalCalls.RhpEHEnumNext(&ehEnum, &ehClause) && curIdx < idxLimit; curIdx++)
            {
                //
                // Skip to the starting try region.  This is used by collided unwinds and rethrows to pickup where
                // the previous dispatch left off.
                //
                if (idxStart != MaxTryRegionIdx)
                {
                    if (curIdx <= idxStart)
                    {
                        lastTryStart = ehClause._tryStartOffset; lastTryEnd = ehClause._tryEndOffset;
                        continue;
                    }

                    // Now, we continue skipping while the try region is identical to the one that invoked the
                    // previous dispatch.
                    if ((ehClause._tryStartOffset == lastTryStart) && (ehClause._tryEndOffset == lastTryEnd)
#if !NATIVEAOT
                        && (ehClause._isSameTry)
#endif
                    )
                        continue;

                    // We are done skipping. This is required to handle empty finally block markers that are used
                    // to separate runs of different try blocks with same native code offsets.
                    idxStart = MaxTryRegionIdx;
                }

                RhEHClauseKind clauseKind = ehClause._clauseKind;

                if ((clauseKind != RhEHClauseKind.RH_EH_CLAUSE_FAULT)
                    || !ehClause.ContainsCodeOffset(codeOffset))
                {
                    continue;
                }

                // Found a containing clause. Because of the order of the clauses, we know this is the
                // most containing.

                // N.B. -- We need to suppress GC "in-between" calls to finallys in this loop because we do
                // not have the correct next-execution point live on the stack and, therefore, may cause a GC
                // hole if we allow a GC between invocation of finally funclets (i.e. after one has returned
                // here to the dispatcher, but before the next one is invoked).  Once they are running, it's
                // fine for them to trigger a GC, obviously.
                //
                // As a result, RhpCallFinallyFunclet will set this state in the runtime upon return from the
                // funclet, and we need to reset it if/when we fall out of the loop and we know that the
                // method will no longer get any more GC callbacks.

                byte* pFinallyHandler = ehClause._handlerAddress;
                exInfo._idxCurClause = curIdx;
#if NATIVEAOT
                InternalCalls.RhpCallFinallyFunclet(pFinallyHandler, exInfo._frameIter.RegisterSet);
#else // NATIVEAOT
#pragma warning disable CS8500
                fixed (EH.ExInfo* pExInfo = &exInfo)
                {
                    InternalCalls.RhpCallFinallyFunclet(pFinallyHandler, exInfo._frameIter.RegisterSet, pExInfo);
                }
#pragma warning restore CS8500
#endif // NATIVEAOT
                exInfo._idxCurClause = MaxTryRegionIdx;
            }
        }

#if NATIVEAOT
#pragma warning disable IDE0060
        [UnmanagedCallersOnly(EntryPoint = "RhpFailFastForPInvokeExceptionPreemp", CallConvs = new Type[] { typeof(CallConvCdecl) })]
        public static void RhpFailFastForPInvokeExceptionPreemp(IntPtr PInvokeCallsiteReturnAddr, void* pExceptionRecord, void* pContextRecord)
        {
            FailFastViaClasslib(RhFailFastReason.UnhandledExceptionFromPInvoke, null, PInvokeCallsiteReturnAddr);
        }
        [RuntimeExport("RhpFailFastForPInvokeExceptionCoop")]
        public static void RhpFailFastForPInvokeExceptionCoop(IntPtr classlibBreadcrumb, void* pExceptionRecord, void* pContextRecord)
        {
            FailFastViaClasslib(RhFailFastReason.UnhandledExceptionFromPInvoke, null, classlibBreadcrumb);
        }
#pragma warning restore IDE0060
#endif
    } // static class EH
}
