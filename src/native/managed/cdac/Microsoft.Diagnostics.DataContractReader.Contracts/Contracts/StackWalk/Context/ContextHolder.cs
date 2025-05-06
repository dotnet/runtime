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

    public bool TrySetRegister(Target target, string fieldName, TargetNUInt value)
    {
        if (typeof(T).GetField(fieldName) is not FieldInfo field) return false;
        switch (field.FieldType)
        {
            case Type t when t == typeof(ulong) && target.PointerSize == sizeof(ulong):
                field.SetValueDirect(__makeref(Context), value.Value);
                return true;
            case Type t when t == typeof(uint) && target.PointerSize == sizeof(uint):
                field.SetValueDirect(__makeref(Context), (uint)value.Value);
                return true;
            default:
                return false;
        }
    }

    public bool TryReadRegister(Target target, string fieldName, out TargetNUInt value)
    {
        value = default;
        if (typeof(T).GetField(fieldName) is not FieldInfo field) return false;
        object? fieldValue = field.GetValue(Context);
        if (fieldValue is ulong ul && target.PointerSize == sizeof(ulong))
        {
            value = new(ul);
            return true;
        }
        if (fieldValue is uint ui && target.PointerSize == sizeof(uint))
        {
            value = new(ui);
            return true;
        }
        return false;
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
}
