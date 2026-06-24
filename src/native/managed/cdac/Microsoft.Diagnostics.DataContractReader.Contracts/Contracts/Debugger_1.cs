// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
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

    TargetPointer IDebugger.GetHijackAddress()
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

    void IDebugger.PlaceExceptionHijackWorkerArguments(byte[] context, ref TargetPointer sp, ReadOnlySpan<TargetNUInt> args)
    {
        IPlatformAgnosticContext ctx = IPlatformAgnosticContext.GetContextForPlatform(_target);
        ctx.FillFromBuffer(context);
        IntegerArgPlacer.PlaceArgs(_target, ctx, ref sp, args);
        ctx.GetBytes().AsSpan().CopyTo(context);
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
