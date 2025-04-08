// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

public class CotnextHolder<T> : IPlatformAgnosticContext where T : unmanaged, IPlatformContext
{
    public T Context;

    public uint Size => Context.Size;
    public uint DefaultContextFlags => Context.DefaultContextFlags;

    public TargetPointer StackPointer { get => Context.StackPointer; set => Context.StackPointer = value; }
    public TargetPointer InstructionPointer { get => Context.InstructionPointer; set => Context.InstructionPointer = value; }
    public TargetPointer FramePointer { get => Context.FramePointer; set => Context.FramePointer = value; }

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
    public unsafe byte[] GetBytes()
    {
        Span<T> structSpan = MemoryMarshal.CreateSpan(ref Context, 1);
        Span<byte> byteSpan = MemoryMarshal.AsBytes(structSpan);
        return byteSpan.ToArray();
    }
    public IPlatformAgnosticContext Clone() => new CotnextHolder<T>() { Context = Context };
    public void Clear() => Context = default;
    public void Unwind(Target target) => Context.Unwind(target);
}
