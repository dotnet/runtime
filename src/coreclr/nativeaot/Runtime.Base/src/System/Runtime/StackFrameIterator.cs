// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
#if !NATIVEAOT
using System.Runtime.ExceptionServices;
#endif

namespace System.Runtime
{
    [StructLayout(LayoutKind.Explicit, Size = AsmOffsets.SIZEOF__REGDISPLAY)]
    internal unsafe struct REGDISPLAY
    {
        [FieldOffset(AsmOffsets.OFFSETOF__REGDISPLAY__SP)]
        internal UIntPtr SP;
#if !NATIVEAOT
        [FieldOffset(AsmOffsets.OFFSETOF__REGDISPLAY__ControlPC)]
        internal IntPtr ControlPC;
        [FieldOffset(AsmOffsets.OFFSETOF__REGDISPLAY__m_pCurrentContext)]
        internal EH.PAL_LIMITED_CONTEXT* m_pCurrentContext;
#endif
    }

    [StructLayout(LayoutKind.Explicit, Size = AsmOffsets.SIZEOF__StackFrameIterator)]
    internal unsafe struct StackFrameIterator
    {
#if !NATIVEAOT
        [FieldOffset(AsmOffsets.OFFSETOF__StackFrameIterator__m_pRegDisplay)]
        private REGDISPLAY* _pRegDisplay;

        [FieldOffset(AsmOffsets.OFFSETOF__StackFrameIterator__m_AdjustedControlPC)]
        internal byte* ControlPC;
        internal byte* OriginalControlPC { get { return (byte*)_pRegDisplay->ControlPC; } }
        internal void* RegisterSet { get { return _pRegDisplay; } }
        internal UIntPtr SP { get { return _pRegDisplay->SP; } }
        internal UIntPtr FramePointer { get { return _pRegDisplay->m_pCurrentContext->FP; } }
        [FieldOffset(AsmOffsets.OFFSETOF__StackFrameIterator__m_isRuntimeWrappedExceptions)]
        private byte _IsRuntimeWrappedExceptions;
        internal bool IsRuntimeWrappedExceptions { get { return _IsRuntimeWrappedExceptions != 0; } }
#else // NATIVEAOT
        [FieldOffset(AsmOffsets.OFFSETOF__StackFrameIterator__m_FramePointer)]
        private UIntPtr _framePointer;
        [FieldOffset(AsmOffsets.OFFSETOF__StackFrameIterator__m_ControlPC)]
        private IntPtr _controlPC;
        [FieldOffset(AsmOffsets.OFFSETOF__StackFrameIterator__m_RegDisplay)]
        private REGDISPLAY _regDisplay;
        [FieldOffset(AsmOffsets.OFFSETOF__StackFrameIterator__m_OriginalControlPC)]
        private IntPtr _originalControlPC;
        [FieldOffset(AsmOffsets.OFFSETOF__StackFrameIterator__m_pPreviousTransitionFrame)]
        private IntPtr _pPreviousTransitionFrame;

        internal byte* ControlPC { get { return (byte*)_controlPC; } }
        internal byte* OriginalControlPC { get { return (byte*)_originalControlPC; } }
        internal void* RegisterSet { get { fixed (void* pRegDisplay = &_regDisplay) { return pRegDisplay; } } }
        internal UIntPtr SP { get { return _regDisplay.SP; } }
        internal UIntPtr FramePointer { get { return _framePointer; } }
        internal IntPtr PreviousTransitionFrame { get { return _pPreviousTransitionFrame; } }
#pragma warning disable CA1822
        internal bool IsRuntimeWrappedExceptions { get { return false; } }
#pragma warning restore CA1822
#endif // NATIVEAOT

        internal bool Init(EH.PAL_LIMITED_CONTEXT* pStackwalkCtx, bool instructionFault = false, bool* fIsExceptionIntercepted = null)
        {
            return InternalCalls.RhpSfiInit(ref this, pStackwalkCtx, instructionFault, fIsExceptionIntercepted);
        }

        internal bool Next()
        {
            return Next(null, null, null);
        }

        internal bool Next(uint* uExCollideClauseIdx, bool* fIsExceptionIntercepted)
        {
            return Next(uExCollideClauseIdx, null, fIsExceptionIntercepted);
        }

        internal bool Next(uint* uExCollideClauseIdx, bool* fUnwoundReversePInvoke, bool* fIsExceptionIntercepted)
        {
            return InternalCalls.RhpSfiNext(ref this, uExCollideClauseIdx, fUnwoundReversePInvoke, fIsExceptionIntercepted);
        }
    }
}
