// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Runtime.CompilerServices;

[StackTraceHidden]
[DebuggerStepThrough]
internal static unsafe partial class GenericsHelpers
{
    [LibraryImport(RuntimeHelpers.QCall)]
    private static partial IntPtr GenericHandleWorker(IntPtr pMD, IntPtr pMT, IntPtr signature, uint dictionaryIndexAndSlot, IntPtr pModule);

    public struct GenericHandleArgs
    {
        public IntPtr signature;
        public IntPtr module;
        public uint dictionaryIndexAndSlot;
    };

    [DebuggerHidden]
    public static IntPtr Method(IntPtr methodHnd, IntPtr signature)
    {
        return GenericHandleWorker(methodHnd, IntPtr.Zero, signature, 0xFFFFFFFF, IntPtr.Zero);
    }

    [DebuggerHidden]
    public static IntPtr MethodWithSlotAndModule(IntPtr methodHnd, GenericHandleArgs * pArgs)
    {
        return GenericHandleWorker(methodHnd, IntPtr.Zero, pArgs->signature, pArgs->dictionaryIndexAndSlot, pArgs->module);
    }

    [DebuggerHidden]
    public static IntPtr Class(IntPtr classHnd, IntPtr signature)
    {
        return GenericHandleWorker(IntPtr.Zero, classHnd, signature, 0xFFFFFFFF, IntPtr.Zero);
    }

    [DebuggerHidden]
    public static IntPtr ClassWithSlotAndModule(IntPtr classHnd, GenericHandleArgs * pArgs)
    {
        return GenericHandleWorker(IntPtr.Zero, classHnd, pArgs->signature, pArgs->dictionaryIndexAndSlot, pArgs->module);
    }
}
