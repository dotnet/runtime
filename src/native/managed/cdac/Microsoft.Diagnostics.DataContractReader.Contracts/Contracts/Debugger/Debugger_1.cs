// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly struct Debugger_1 : IDebugger
{
    private enum DebuggerControlFlag_1 : uint
    {
        PendingAttach = 0x0100,
        Attached = 0x0200,
    }
    private const uint UnhandledExceptionHijackIndex = 0;

    private readonly Target _target;

    internal Debugger_1(Target target)
    {
        _target = target;
    }

    private bool TryGetDebuggerAddress(out TargetPointer debuggerAddress)
    {
        debuggerAddress = TargetPointer.Null;

        TargetPointer debuggerPtrPtr = _target.ReadGlobalPointer(Constants.Globals.Debugger);
        if (debuggerPtrPtr == TargetPointer.Null)
            return false;

        debuggerAddress = _target.ReadPointer(debuggerPtrPtr);
        return debuggerAddress != TargetPointer.Null;
    }

    bool IDebugger.TryGetDebuggerData(out DebuggerData data)
    {
        data = default;
        if (!TryGetDebuggerAddress(out TargetPointer debuggerAddress))
            return false;

        Data.Debugger debugger = _target.ProcessedData.GetOrAdd<Data.Debugger>(debuggerAddress);
        data = new DebuggerData(debugger.LeftSideInitialized != 0, debugger.Defines, debugger.MDStructuresVersion);
        return true;
    }

    int IDebugger.GetAttachStateFlags()
    {
        TargetPointer addr = _target.ReadGlobalPointer(Constants.Globals.CLRJitAttachState);
        return (int)_target.Read<uint>(addr.Value);
    }

    void IDebugger.MarkDebuggerAttachPending()
    {
        TargetPointer addr = _target.ReadGlobalPointer(Constants.Globals.CORDebuggerControlFlags);
        uint currentFlags = _target.Read<uint>(addr.Value);
        _target.Write<uint>(addr.Value, currentFlags | (uint)DebuggerControlFlag_1.PendingAttach);
    }

    void IDebugger.MarkDebuggerAttached(bool fAttached)
    {
        TargetPointer addr = _target.ReadGlobalPointer(Constants.Globals.CORDebuggerControlFlags);
        uint currentFlags = _target.Read<uint>(addr.Value);
        if (fAttached)
        {
            _target.Write<uint>(addr.Value, currentFlags | (uint)DebuggerControlFlag_1.Attached);
        }
        else
        {
            _target.Write<uint>(addr.Value, currentFlags & ~((uint)DebuggerControlFlag_1.Attached | (uint)DebuggerControlFlag_1.PendingAttach));
        }
    }

    bool IDebugger.MetadataUpdatesApplied()
    {
        if (_target.TryReadGlobalPointer(Constants.Globals.MetadataUpdatesApplied, out TargetPointer? addr))
        {
            return _target.Read<byte>(addr.Value.Value) != 0;
        }
        return false;
    }

    void IDebugger.RequestSyncAtEvent()
    {
        if (!TryGetDebuggerAddress(out TargetPointer debuggerAddress))
            return;

        Data.Debugger debugger = _target.ProcessedData.GetOrAdd<Data.Debugger>(debuggerAddress);
        debugger.WriteRSRequestedSync(1);
    }

    void IDebugger.SetSendExceptionsOutsideOfJMC(bool sendExceptionsOutsideOfJMC)
    {
        if (!TryGetDebuggerAddress(out TargetPointer debuggerAddress))
            return;

        Data.Debugger debugger = _target.ProcessedData.GetOrAdd<Data.Debugger>(debuggerAddress);
        debugger.WriteSendExceptionsOutsideOfJMC(sendExceptionsOutsideOfJMC ? 1 : 0);
    }

    TargetPointer IDebugger.GetDebuggerControlBlockAddress()
    {
        if (!TryGetDebuggerAddress(out TargetPointer debuggerAddress))
            return TargetPointer.Null;

        Data.Debugger debugger = _target.ProcessedData.GetOrAdd<Data.Debugger>(debuggerAddress);
        TargetPointer rcThread = debugger.RCThread;
        if (rcThread == TargetPointer.Null)
            return TargetPointer.Null;

        Data.DebuggerRCThread debuggerRcThread = _target.ProcessedData.GetOrAdd<Data.DebuggerRCThread>(rcThread);
        return debuggerRcThread.DCB;
    }

    void IDebugger.EnableGCNotificationEvents(bool fEnable)
    {
        if (!TryGetDebuggerAddress(out TargetPointer debuggerAddress))
            return;

        Data.Debugger debugger = _target.ProcessedData.GetOrAdd<Data.Debugger>(debuggerAddress);
        debugger.WriteGCNotificationEventsEnabled(fEnable ? 1 : 0);
    }

    HijackKind IDebugger.GetHijackKind(TargetCodePointer controlPC)
    {
        if (!TryGetDebuggerAddress(out TargetPointer debuggerAddress))
            return HijackKind.None;

        Data.Debugger debugger = _target.ProcessedData.GetOrAdd<Data.Debugger>(debuggerAddress);
        if (debugger.RgHijackFunction == TargetPointer.Null)
            return HijackKind.None;

        uint maxHijackFunctions = _target.ReadGlobal<uint>(Constants.Globals.MaxHijackFunctions);
        if (maxHijackFunctions == 0)
            return HijackKind.None;

        Target.TypeInfo memoryRangeTypeInfo = _target.GetTypeInfo(DataType.MemoryRange);
        uint stride = memoryRangeTypeInfo.Size!.Value;

        for (uint i = 0; i < maxHijackFunctions; i++)
        {
            TargetPointer entryAddress = debugger.RgHijackFunction + (ulong)(i * stride);
            Data.MemoryRange entry = _target.ProcessedData.GetOrAdd<Data.MemoryRange>(entryAddress);

            ulong start = entry.StartAddress.Value;
            ulong end = start + entry.Size.Value;
            if (controlPC.Value >= start && controlPC.Value < end)
            {
                return i == UnhandledExceptionHijackIndex ? HijackKind.UnhandledException : HijackKind.Other;
            }
        }
        return HijackKind.None;
    }

    private TargetPointer GetHijackAddress()
    {
        return TryGetHijackFunctionRange(UnhandledExceptionHijackIndex, out Data.MemoryRange? range)
            ? range.StartAddress
            : TargetPointer.Null;
    }

    private bool TryGetHijackFunctionRange(uint index, [NotNullWhen(true)] out Data.MemoryRange? range)
    {
        range = null;
        if (!TryGetDebuggerAddress(out TargetPointer debuggerAddress))
            return false;

        Data.Debugger debugger = _target.ProcessedData.GetOrAdd<Data.Debugger>(debuggerAddress);
        if (debugger.RgHijackFunction == TargetPointer.Null)
            return false;

        uint maxHijackFunctions = _target.ReadGlobal<uint>(Constants.Globals.MaxHijackFunctions);
        if (index >= maxHijackFunctions)
            return false;

        uint stride = _target.GetTypeInfo(DataType.MemoryRange).Size!.Value;
        TargetPointer entryAddress = debugger.RgHijackFunction + (ulong)(index * stride);
        range = _target.ProcessedData.GetOrAdd<Data.MemoryRange>(entryAddress);
        return true;
    }

    // offsetof(EXCEPTION_RECORD, ExceptionInformation) for the target's pointer size.
    // Layout: ExceptionCode (DWORD) + ExceptionFlags (DWORD) + ExceptionRecord (ptr) +
    //         ExceptionAddress (ptr) + NumberParameters (DWORD), then ExceptionInformation[]
    //         aligned up to the pointer size.
    private int ExceptionRecordHeaderSize()
    {
        int ptrSize = _target.PointerSize;
        int unaligned = sizeof(uint) + sizeof(uint) + ptrSize + ptrSize + sizeof(uint);
        return (unaligned + (ptrSize - 1)) & ~(ptrSize - 1);
    }

    // EXCEPTION_RECORD::NumberParameters lives after the two leading DWORDs and the two pointers.
    private uint ReadExceptionRecordNumberParameters(ReadOnlySpan<byte> record)
    {
        int numberParametersOffset = sizeof(uint) + sizeof(uint) + (2 * _target.PointerSize);
        ReadOnlySpan<byte> slice = record.Slice(numberParametersOffset, sizeof(uint));
        return _target.IsLittleEndian
            ? BinaryPrimitives.ReadUInt32LittleEndian(slice)
            : BinaryPrimitives.ReadUInt32BigEndian(slice);
    }

    private void WriteExceptionRecordHelper(TargetPointer remotePtr, byte[] record)
    {
        uint numberParameters = ReadExceptionRecordNumberParameters(record);
        int cbSize = ExceptionRecordHeaderSize() + ((int)numberParameters * _target.PointerSize);
        _target.WriteBuffer(remotePtr.Value, record.AsSpan(0, cbSize));
    }

    TargetPointer IDebugger.PrepareExceptionHijack(byte[] context, TargetPointer vmThread, byte[]? exceptionRecord, int reason, TargetPointer userData)
    {
        TargetPointer pfnHijackFunction = GetHijackAddress();
        if (pfnHijackFunction == TargetPointer.Null)
            throw Marshal.GetExceptionForHR(CorDbgHResults.CORDBG_E_NOTREADY)!;

        IPlatformAgnosticContext ctx = IPlatformAgnosticContext.GetContextForPlatform(_target);
        ctx.FillFromBuffer(context);

        ctx.UnsetSingleStepFlag();

        TargetPointer sp = ctx.StackPointer;
        TargetPointer espContext = TargetPointer.Null;
        TargetPointer espRecord = TargetPointer.Null;

        if (vmThread != TargetPointer.Null)
        {
            ThreadData threadData = _target.Contracts.Thread.GetThreadData(vmThread);
            if (threadData.IsExceptionInProgress)
            {
                TargetPointer espOSContext = threadData.OSExceptionContextRecord;
                TargetPointer espOSRecord = threadData.OSExceptionRecord;
                if (espOSContext < sp)
                {
                    _target.WriteBuffer(espOSContext.Value, ctx.GetBytes());
                    espContext = espOSContext;

                    // We should have an EXCEPTION_RECORD if we're hijacked at an exception.
                    WriteExceptionRecordHelper(espOSRecord, exceptionRecord!);
                    espRecord = espOSRecord;

                    sp = espOSContext < espOSRecord ? espOSContext : espOSRecord;
                }
            }
        }

        // If we didn't reuse the OS stack space, push fresh structures at the leaf of the stack.
        if (espContext == TargetPointer.Null)
        {
            Debug.Assert(espRecord == TargetPointer.Null);

            espContext = StackPusher.Push(_target, ref sp, ctx.GetBytes(), align: true);

            // If the caller didn't pass an exception record, we're not hijacking at an
            // exception and pass null for the record argument.
            if (exceptionRecord is not null)
            {
                espRecord = StackPusher.Push(_target, ref sp, exceptionRecord, align: true);
            }
        }

        // Set up the arguments for the hijack worker:
        //   void ExceptionHijackWorker(CONTEXT* pContext, EXCEPTION_RECORD* pRecord, EHijackReason reason, void* pData)
        ReadOnlySpan<TargetNUInt> args =
        [
            new TargetNUInt(espContext.Value),
            new TargetNUInt(espRecord.Value),
            new TargetNUInt((uint)reason),
            new TargetNUInt(userData.Value),
        ];
        IntegerArgPlacer.PlaceArgs(_target, ctx, ref sp, args);

        ctx.StackPointer = sp;
        ctx.InstructionPointer = new TargetCodePointer(pfnHijackFunction.Value);

        ctx.GetBytes().AsSpan().CopyTo(context);

        return espContext;
    }

    private static class IntegerArgPlacer
    {
        // Places integer arguments into the appropriate registers and stack slots for the native ABI.
        public static void PlaceArgs(Target target, IPlatformAgnosticContext ctx, ref TargetPointer sp, ReadOnlySpan<TargetNUInt> args)
        {
            RuntimeInfoArchitecture arch = target.Contracts.RuntimeInfo.GetTargetArchitecture();
            RuntimeInfoOperatingSystem os = target.Contracts.RuntimeInfo.GetTargetOperatingSystem();
            CallingConvention cc = GetCallingConvention(arch, os);
            int regCount = Math.Min(args.Length, cc.IntegerArgRegisters.Length);

            // Place register args.
            for (int i = 0; i < regCount; i++)
            {
                SetRegisterChecked(ctx, cc.IntegerArgRegisters[i], args[i], target.PointerSize);
            }

            // Push stack-passed args (those beyond the register slots) right-to-left
            for (int i = args.Length - 1; i >= regCount; i--)
            {
                StackPusher.PushSlot(target, ref sp, args[i], align: false);
            }

            // Reserve home / shadow space (Windows x64 only).
            if (cc.HomeSpaceSlotCount > 0)
            {
                sp = new TargetPointer(sp.Value - (ulong)(cc.HomeSpaceSlotCount * target.PointerSize));
            }
        }

        private readonly record struct CallingConvention(
            string[] IntegerArgRegisters,
            int HomeSpaceSlotCount);

        private static CallingConvention GetCallingConvention(RuntimeInfoArchitecture arch, RuntimeInfoOperatingSystem os)
        {
            switch (arch)
            {
                case RuntimeInfoArchitecture.X86:
                    // cdecl / stdcall: all args on the stack, no register args, no home space.
                    return new CallingConvention([], HomeSpaceSlotCount: 0);

                case RuntimeInfoArchitecture.X64 when os == RuntimeInfoOperatingSystem.Windows:
                    // Microsoft x64 calling convention: 4 integer arg regs + always 32 bytes
                    // of caller-allocated home / shadow space.
                    return new CallingConvention(["rcx", "rdx", "r8", "r9"], HomeSpaceSlotCount: 4);

                case RuntimeInfoArchitecture.X64:
                    // System V AMD64 ABI (Linux, macOS, *BSD, ...): 6 integer arg regs, no home space.
                    return new CallingConvention(["rdi", "rsi", "rdx", "rcx", "r8", "r9"], HomeSpaceSlotCount: 0);

                case RuntimeInfoArchitecture.Arm:
                    // AAPCS32: r0..r3.
                    return new CallingConvention(["r0", "r1", "r2", "r3"], HomeSpaceSlotCount: 0);

                case RuntimeInfoArchitecture.Arm64:
                    // AAPCS64: x0..x7.
                    return new CallingConvention(["x0", "x1", "x2", "x3", "x4", "x5", "x6", "x7"], HomeSpaceSlotCount: 0);

                case RuntimeInfoArchitecture.LoongArch64:
                case RuntimeInfoArchitecture.RiscV64:
                    // LoongArch and RISC-V calling conventions: a0..a7.
                    return new CallingConvention(["a0", "a1", "a2", "a3", "a4", "a5", "a6", "a7"], HomeSpaceSlotCount: 0);

                default:
                    throw new NotSupportedException($"IntegerArgPlacer.PlaceArgs does not support architecture '{arch}'.");
            }
        }

        private static void SetRegisterChecked(IPlatformAgnosticContext ctx, string register, TargetNUInt value, int pointerSize)
        {
            if (pointerSize == 4 && value.Value > uint.MaxValue)
            {
                throw new InvalidOperationException($"Cannot set register '{register}' to value {value.Value}: value exceeds 32-bit range.");
            }
            if (!ctx.TrySetRegister(register, value))
            {
                throw new InvalidOperationException($"Failed to set register '{register}' on context.");
            }
        }
    }
}
