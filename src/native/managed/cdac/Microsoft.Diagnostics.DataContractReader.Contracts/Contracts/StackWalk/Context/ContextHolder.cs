// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

public sealed class ContextHolder<T> : IPlatformAgnosticContext, IEquatable<ContextHolder<T>>
    where T : unmanaged, IPlatformContext
{
    public T Context;

    public uint Size => Context.Size;
    public uint GetSize() => Context.GetSize();
    public uint ContextControlFlags => Context.ContextControlFlags;
    public uint FullContextFlags => Context.FullContextFlags;
    public uint AllContextFlags => Context.AllContextFlags;

    public int StackPointerRegister => Context.StackPointerRegister;

    public TargetPointer StackPointer { get => Context.StackPointer; set => Context.StackPointer = value; }
    public TargetCodePointer InstructionPointer { get => Context.InstructionPointer; set => Context.InstructionPointer = value; }
    public TargetPointer FramePointer { get => Context.FramePointer; set => Context.FramePointer = value; }

    public uint RawContextFlags { get => Context.RawContextFlags; set => Context.RawContextFlags = value; }

    public unsafe void ReadFromAddress(Target target, TargetPointer address)
    {
        Span<byte> buffer = new byte[Size];
        target.ReadBuffer(address, buffer);
        FillFromBuffer(buffer);
    }
    public unsafe void FillFromBuffer(Span<byte> buffer)
    {
        Span<T> structSpan = new(ref Context);
        Span<byte> byteSpan = MemoryMarshal.Cast<T, byte>(structSpan);
        if (buffer.Length > sizeof(T))
        {
            buffer.Slice(0, sizeof(T)).CopyTo(byteSpan);
        }
        else
        {
            buffer.CopyTo(byteSpan);
        }
    }
    public ReadOnlySpan<byte> GetBytes() => MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref Context, 1));
    public IPlatformAgnosticContext Clone() => new ContextHolder<T>() { Context = Context };
    public void Clear() => Context = default;
    public void Unwind(Target target) => Context.Unwind(target);
    public void UnsetSingleStepFlag() => Context.UnsetSingleStepFlag();

    public bool TrySetRegister(string fieldName, TargetNUInt value) => Context.TrySetRegister(fieldName, value);
    public bool TryReadRegister(string fieldName, out TargetNUInt value) => Context.TryReadRegister(fieldName, out value);
    public bool TrySetRegister(int number, TargetNUInt value) => Context.TrySetRegister(number, value);
    public bool TryReadRegister(int number, out TargetNUInt value) => Context.TryReadRegister(number, out value);
    public bool TryReadFloatingPointRegister(int index, out double value) => Context.TryReadFloatingPointRegister(index, out value);
    public bool TryWriteFloatingPointRegister(int index, double value) => Context.TryWriteFloatingPointRegister(index, value);

    public void CopyContext(IPlatformAgnosticContext source)
    {
        ContextHolder<T> sourceHolder = (ContextHolder<T>)source;
        Context.CopyContext(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref sourceHolder.Context, 1)));
    }

    public bool Equals(ContextHolder<T>? other)
    {
        if (GetType() != other?.GetType())
        {
            return false;
        }

        return Context.Equals(other.Context);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as ContextHolder<T>);
    }

    public override int GetHashCode() => Context.GetHashCode();

    public override string ToString() => Context.ToString() ?? string.Empty;
}
