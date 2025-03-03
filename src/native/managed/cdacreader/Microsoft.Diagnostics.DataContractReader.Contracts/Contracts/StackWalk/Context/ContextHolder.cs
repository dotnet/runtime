// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

public class ContextHolder<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] T> : IPlatformAgnosticContext, IEquatable<ContextHolder<T>>
    where T : unmanaged, IPlatformContext
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
    public IPlatformAgnosticContext Clone() => new ContextHolder<T>() { Context = Context };
    public void Clear() => Context = default;
    public void Unwind(Target target) => Context.Unwind(target);

    public bool TrySetField(string fieldName, TargetNUInt value)
    {
        FieldInfo? field = typeof(T).GetField(fieldName);
        if (field is null) return false;
        field.SetValueDirect(__makeref(Context), value.Value);
        return true;
    }

    public bool TryReadField(string fieldName, out TargetNUInt value)
    {
        FieldInfo? field = typeof(T).GetField(fieldName);
        if (field is null)
        {
            value = default;
            return false;
        }
        value = new((ulong)field.GetValue(Context)!);
        return true;
    }

    public override string? ToString() => Context.ToString();
    public bool Equals(ContextHolder<T>? other)
    {
        if (other is null)
        {
            return false;
        }

        if (GetType() != other.GetType())
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
}
