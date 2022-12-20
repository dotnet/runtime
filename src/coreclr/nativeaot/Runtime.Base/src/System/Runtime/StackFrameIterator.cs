// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Runtime
{
    [StructLayout(LayoutKind.Explicit, Size = AsmOffsets.SIZEOF__REGDISPLAY)]
    internal unsafe struct REGDISPLAY
    {
        [FieldOffset(AsmOffsets.OFFSETOF__REGDISPLAY__SP)]
        internal UIntPtr SP;
    }

    [StructLayout(LayoutKind.Explicit, Size = AsmOffsets.SIZEOF__StackFrameIterator)]
    internal unsafe struct StackFrameIterator
    {
        [FieldOffset(AsmOffsets.OFFSETOF__StackFrameIterator__m_FramePointer)]
        private UIntPtr _framePointer;
        [FieldOffset(AsmOffsets.OFFSETOF__StackFrameIterator__m_ControlPC)]
        private IntPtr _controlPC;
        [FieldOffset(AsmOffsets.OFFSETOF__StackFrameIterator__m_RegDisplay)]
        private REGDISPLAY _regDisplay;
        [FieldOffset(AsmOffsets.OFFSETOF__StackFrameIterator__m_OriginalControlPC)]
        private IntPtr _originalControlPC;

        internal byte* ControlPC { get { return (byte*)_controlPC; } }
        internal byte* OriginalControlPC { get { return (byte*)_originalControlPC; } }
        internal void* RegisterSet { get { fixed (void* pRegDisplay = &_regDisplay) { return pRegDisplay; } } }
        internal UIntPtr SP { get { return _regDisplay.SP; } }
        internal UIntPtr FramePointer { get { return _framePointer; } }

        internal bool Init(EH.PAL_LIMITED_CONTEXT* pStackwalkCtx, bool instructionFault = false)
        {
            return InternalCalls.RhpSfiInit(ref this, pStackwalkCtx, instructionFault);
        }

        internal bool Next()
        {
            return Next(null, null);
        }

        internal bool Next(uint* uExCollideClauseIdx)
        {
            return Next(uExCollideClauseIdx, null);
        }

        internal bool Next(uint* uExCollideClauseIdx, bool* fUnwoundReversePInvoke)
        {
            return InternalCalls.RhpSfiNext(ref this, uExCollideClauseIdx, fUnwoundReversePInvoke);
        }
    }
}
