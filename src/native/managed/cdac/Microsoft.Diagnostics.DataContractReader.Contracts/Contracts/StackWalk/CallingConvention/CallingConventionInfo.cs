// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Provides an abstraction over platform-specific calling conventions
// (specifically, the managed calling convention utilized by the JIT).
// Ported from crossgen2's TransitionBlock.cs.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers.CallingConvention;

/// <summary>
/// Architecture-specific calling convention constants and methods for
/// mapping method arguments to register and stack locations.
/// </summary>
/// <remarks>
/// Layout-dependent values (offsets, sizes) come from the data descriptor
/// (<see cref="Data.TransitionBlock"/>). ABI-invariant values (register counts,
/// alignment rules) are hardcoded per architecture since they are defined by
/// the hardware/OS ABI and never change.
/// </remarks>
internal sealed class CallingConventionInfo
{
    // Layout values from the data descriptor
    private readonly uint _sizeOfTransitionBlock;
    private readonly uint _argumentRegistersOffset;
    private readonly uint _firstGCRefMapSlot;
    private readonly uint _offsetOfArgs;
    private readonly int _offsetOfFloatArgumentRegisters;

    // ABI invariants
    public int PointerSize { get; }
    public int NumArgumentRegisters { get; }
    public int NumFloatArgumentRegisters { get; }
    public int FloatRegisterSize { get; }
    public int EnregisteredParamTypeMaxSize { get; }
    public int StackSlotSize { get; }
    public bool IsRetBuffPassedAsFirstArg { get; }
    public bool IsX64UnixABI { get; }
    public bool IsAppleArm64ABI { get; }
    public bool IsArmhfABI { get; }

    public RuntimeInfoArchitecture Architecture { get; }

    // Convenience accessors
    public uint SizeOfTransitionBlock => _sizeOfTransitionBlock;
    public uint ArgumentRegistersOffset => _argumentRegistersOffset;
    public uint FirstGCRefMapSlot => _firstGCRefMapSlot;
    public uint OffsetOfArgs => _offsetOfArgs;
    public int OffsetOfFloatArgumentRegisters => _offsetOfFloatArgumentRegisters;
    public int SizeOfArgumentRegisters => NumArgumentRegisters * PointerSize;

    public const int InvalidOffset = -1;
    public const int StructInRegsOffset = -2;

    /// <summary>
    /// Creates a <see cref="CallingConventionInfo"/> for the given target, reading
    /// layout data from the data descriptor and filling in ABI constants from
    /// the target's architecture and OS.
    /// </summary>
    public CallingConventionInfo(Target target)
    {
        IRuntimeInfo runtimeInfo = target.Contracts.RuntimeInfo;
        Architecture = runtimeInfo.GetTargetArchitecture();
        RuntimeInfoOperatingSystem os = runtimeInfo.GetTargetOperatingSystem();
        PointerSize = target.PointerSize;

        // Read layout values from the data descriptor
        Target.TypeInfo tbType = target.GetTypeInfo(DataType.TransitionBlock);
        _sizeOfTransitionBlock = (uint)tbType.Size!;
        _argumentRegistersOffset = (uint)tbType.Fields["ArgumentRegistersOffset"].Offset;
        _firstGCRefMapSlot = (uint)tbType.Fields["FirstGCRefMapSlot"].Offset;
        _offsetOfArgs = (uint)tbType.Fields["OffsetOfArgs"].Offset;
        _offsetOfFloatArgumentRegisters = tbType.Fields["OffsetOfFloatArgumentRegisters"].Offset;

        // Fill in ABI invariants based on architecture
        switch (Architecture)
        {
            case RuntimeInfoArchitecture.X86:
                NumArgumentRegisters = 2;       // ECX, EDX
                NumFloatArgumentRegisters = 0;
                FloatRegisterSize = 0;
                EnregisteredParamTypeMaxSize = 0;
                StackSlotSize = 4;
                IsRetBuffPassedAsFirstArg = true;
                break;

            case RuntimeInfoArchitecture.X64:
                if (os == RuntimeInfoOperatingSystem.Unix)
                {
                    // Unix AMD64 ABI
                    NumArgumentRegisters = 6;   // RDI, RSI, RDX, RCX, R8, R9
                    NumFloatArgumentRegisters = 8; // XMM0-XMM7
                    FloatRegisterSize = 16;     // M128A
                    EnregisteredParamTypeMaxSize = 16;
                    IsX64UnixABI = true;
                }
                else
                {
                    // Windows AMD64 ABI
                    NumArgumentRegisters = 4;   // RCX, RDX, R8, R9
                    NumFloatArgumentRegisters = 0; // Shared with GP regs on Windows
                    FloatRegisterSize = 16;
                    EnregisteredParamTypeMaxSize = 8;
                }
                StackSlotSize = 8;
                IsRetBuffPassedAsFirstArg = true;
                break;

            case RuntimeInfoArchitecture.Arm:
                NumArgumentRegisters = 4;       // R0-R3
                NumFloatArgumentRegisters = 16; // 16 single-precision slots (D0-D7 / S0-S15)
                FloatRegisterSize = 4;
                EnregisteredParamTypeMaxSize = 0;
                StackSlotSize = 4;
                IsRetBuffPassedAsFirstArg = true;
                IsArmhfABI = true; // TODO: detect armel
                break;

            case RuntimeInfoArchitecture.Arm64:
                NumArgumentRegisters = 8;       // X0-X7
                NumFloatArgumentRegisters = 8;  // V0-V7
                FloatRegisterSize = 16;
                EnregisteredParamTypeMaxSize = 16;
                StackSlotSize = 8;
                IsRetBuffPassedAsFirstArg = false; // ARM64 uses X8 for retbuf
                // Apple ARM64 has different stack alignment rules.
                // Unix OS covers macOS/iOS in the runtime's classification.
                IsAppleArm64ABI = os == RuntimeInfoOperatingSystem.Unix; // TODO: refine Apple vs Linux detection
                break;

            case RuntimeInfoArchitecture.LoongArch64:
                NumArgumentRegisters = 8;       // A0-A7
                NumFloatArgumentRegisters = 8;  // FA0-FA7
                FloatRegisterSize = 8;
                EnregisteredParamTypeMaxSize = 16;
                StackSlotSize = 8;
                IsRetBuffPassedAsFirstArg = true;
                break;

            case RuntimeInfoArchitecture.RiscV64:
                NumArgumentRegisters = 8;       // a0-a7
                NumFloatArgumentRegisters = 8;  // fa0-fa7
                FloatRegisterSize = 8;
                EnregisteredParamTypeMaxSize = 16;
                StackSlotSize = 8;
                IsRetBuffPassedAsFirstArg = true;
                break;

            default:
                throw new NotSupportedException($"Architecture {Architecture} is not supported for calling convention analysis.");
        }
    }

    // ---- Derived methods ----

    /// <summary>
    /// Returns the byte offset of the 'this' pointer in the transition block.
    /// </summary>
    public int ThisOffset
    {
        get
        {
            if (Architecture == RuntimeInfoArchitecture.X86)
            {
                // ECX offset within ArgumentRegisters: ECX is at offset PointerSize (after EDX at 0)
                return (int)ArgumentRegistersOffset + PointerSize;
            }
            return (int)ArgumentRegistersOffset;
        }
    }

    /// <summary>
    /// Rounds up a parameter size to the stack slot size for the platform.
    /// </summary>
    public int StackElemSize(int parmSize, bool isValueType = false, bool isFloatHfa = false)
    {
        if (IsAppleArm64ABI)
        {
            if (!isValueType)
            {
                // Primitives use their natural size, no padding
                return parmSize;
            }
            if (isFloatHfa)
            {
                // Float HFA: 4-byte alignment
                return parmSize;
            }
        }
        return AlignUp(parmSize, StackSlotSize);
    }

    /// <summary>
    /// Maps a GCRefMap position index to the byte offset in the transition block.
    /// </summary>
    public int OffsetFromGCRefMapPos(int pos)
    {
        if (Architecture == RuntimeInfoArchitecture.X86)
        {
            if (pos < NumArgumentRegisters)
            {
                return (int)ArgumentRegistersOffset + SizeOfArgumentRegisters - (pos + 1) * PointerSize;
            }
            return (int)OffsetOfArgs + (pos - NumArgumentRegisters) * PointerSize;
        }
        return (int)FirstGCRefMapSlot + pos * PointerSize;
    }

    /// <summary>
    /// Returns true if the argument at the given offset is a float register.
    /// Float register offsets are negative.
    /// </summary>
    public static bool IsFloatArgumentRegisterOffset(int offset) => offset < 0;

    /// <summary>
    /// Returns true if the argument at the given offset is in a general-purpose register.
    /// </summary>
    public bool IsArgumentRegisterOffset(int offset)
    {
        return offset >= (int)ArgumentRegistersOffset
            && offset < (int)ArgumentRegistersOffset + SizeOfArgumentRegisters;
    }

    /// <summary>
    /// Returns true if the argument at the given offset is on the stack.
    /// </summary>
    public bool IsStackArgumentOffset(int offset)
    {
        return offset >= (int)ArgumentRegistersOffset + SizeOfArgumentRegisters;
    }

    /// <summary>
    /// Checks if a value type of the given size should be passed by reference
    /// (applies to x64 and ARM64).
    /// </summary>
    public bool IsArgPassedByRef(int size)
    {
        if (EnregisteredParamTypeMaxSize == 0)
            return false;

        if (Architecture == RuntimeInfoArchitecture.X64)
        {
            // On x64, also check power-of-2 rule
            return size > EnregisteredParamTypeMaxSize || (size & (size - 1)) != 0;
        }

        return size > EnregisteredParamTypeMaxSize;
    }

    /// <summary>
    /// Returns the byte offset of the return buffer argument.
    /// </summary>
    public int GetRetBuffArgOffset(bool hasThis)
    {
        if (Architecture == RuntimeInfoArchitecture.X86)
        {
            // x86: retbuf goes in EDX if hasThis (this in ECX), else ECX
            return hasThis
                ? (int)ArgumentRegistersOffset  // EDX offset = 0
                : (int)ArgumentRegistersOffset + PointerSize; // ECX offset
        }
        if (Architecture == RuntimeInfoArchitecture.Arm64)
        {
            // ARM64: retbuf is in X8, which is at FirstGCRefMapSlot
            return (int)FirstGCRefMapSlot;
        }
        // Default: retbuf is after 'this' in the argument registers
        return (int)ArgumentRegistersOffset + (hasThis ? PointerSize : 0);
    }

    internal static int AlignUp(int value, int alignment)
    {
        return (value + (alignment - 1)) & ~(alignment - 1);
    }
}
