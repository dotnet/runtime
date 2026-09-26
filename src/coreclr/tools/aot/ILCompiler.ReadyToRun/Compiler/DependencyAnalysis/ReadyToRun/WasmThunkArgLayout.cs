// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using ILCompiler.DependencyAnalysis.Wasm;
using ILCompiler.ObjectWriter.WasmInstructions;
using Internal.CallingConvention;
using Internal.JitInterface;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis.ReadyToRun;

internal enum WasmThunkArgKind
{
    This,
    RetBuf,
    GenericContext,
    AsyncContinuation,
    Argument,
}

/// <summary>
/// An argument of a Wasm thunk: the Wasm parameters it arrives in and its slot in the argument area.
/// </summary>
internal readonly struct WasmThunkArg
{
    public WasmThunkArg(WasmThunkArgKind kind, int offset, int wasmParamIndex, int wasmParamCount, WasmValueType wasmType, int indirectStructSize, TypeDesc type)
    {
        Kind = kind;
        Offset = offset;
        WasmParamIndex = wasmParamIndex;
        WasmParamCount = wasmParamCount;
        WasmType = wasmType;
        IndirectStructSize = indirectStructSize;
        Type = type;
    }

    public WasmThunkArgKind Kind { get; }

    /// <summary>Offset from the TransitionBlock base, or <see cref="TransitionBlock.InvalidOffset"/> for the return buffer.</summary>
    public int Offset { get; }

    /// <summary>Index of the first Wasm parameter in the thunk's function type.</summary>
    public int WasmParamIndex { get; }

    /// <summary>Number of Wasm parameters; 0 for an empty struct, more than 1 for a multi-slot value.</summary>
    public int WasmParamCount { get; }

    /// <summary>Type of each Wasm parameter.</summary>
    public WasmValueType WasmType { get; }

    /// <summary>Size of a struct passed by reference, otherwise 0.</summary>
    public int IndirectStructSize { get; }

    /// <summary>The raised argument type, or null for the hidden arguments.</summary>
    public TypeDesc Type { get; }

    public bool IsIndirectStruct => IndirectStructSize != 0;
    public bool IsEmptyStruct => WasmParamCount == 0;
    public bool IsMultiSlot => WasmParamCount > 1;
}

/// <summary>
/// Maps each element of a managed Wasm signature, in Wasm parameter order, to its location in the
/// ArgIterator argument area.
/// </summary>
internal sealed class WasmThunkArgLayout
{
    public MethodSignature Signature { get; }
    public TransitionBlock TransitionBlock { get; }
    public int SizeOfFrameArgumentArray { get; }
    public WasmThunkArg[] Args { get; }
    public int? RetBufParamIndex { get; }
    public int PortableEntrypointParamIndex { get; }

    public WasmThunkArgLayout(WasmSignature wasmSignature, TypeSystemContext context)
    {
        (MethodSignature signature, ArgIterator<TypeHandle> argit, TransitionBlock transitionBlock) = BuildArgIterator(wasmSignature, context);
        Signature = signature;
        TransitionBlock = transitionBlock;
        SizeOfFrameArgumentArray = argit.SizeOfFrameArgumentArray();

        string sig = wasmSignature.SignatureString;
        WasmResultType wasmParams = wasmSignature.FuncType.Params;
        List<WasmThunkArg> args = new List<WasmThunkArg>();
        int wasmParamIndex = 1; // $sp

        void AddHiddenArg(WasmThunkArgKind kind, int offset)
        {
            args.Add(new WasmThunkArg(kind, offset, wasmParamIndex, 1, wasmParams.Types[wasmParamIndex], 0, null));
            wasmParamIndex++;
        }

        int pos = 0;
        bool hasRetBuf = sig[pos] == 'S';
        if (hasRetBuf)
        {
            WasmLowering.ParseStructSize(sig, ref pos);
        }
        else
        {
            pos++;
        }

        if (sig[pos] == 'T')
        {
            Debug.Assert(argit.HasThis);
            AddHiddenArg(WasmThunkArgKind.This, transitionBlock.ThisOffset);
            pos++;
        }

        if (hasRetBuf)
        {
            RetBufParamIndex = wasmParamIndex;
            AddHiddenArg(WasmThunkArgKind.RetBuf, TransitionBlock.InvalidOffset);
        }

        if (argit.HasParamType)
        {
            Debug.Assert(sig[pos] == ((context.Target.PointerSize == 4) ? 'i' : 'l'));
            AddHiddenArg(WasmThunkArgKind.GenericContext, argit.GetParamTypeArgOffset());
            pos++;
        }

        if (sig[pos] == 'a')
        {
            Debug.Assert(argit.HasAsyncContinuation);
            AddHiddenArg(WasmThunkArgKind.AsyncContinuation, argit.GetAsyncContinuationArgOffset());
            pos++;
        }

        for (int i = 0; sig[pos] != 'p'; i++)
        {
            int offset = argit.GetNextOffset();
            TypeDesc type = signature[i];
            bool isIndirectStruct = WasmLowering.CurrentArgLowersValueTypeToPassAsByref(argit);
            char c = sig[pos];

            int indirectStructSize = 0;
            int wasmParamCount = 1;
            if (c == 'e')
            {
                Debug.Assert(WasmLowering.IsEmptyStruct(type));
                wasmParamCount = 0;
                pos++;
            }
            else if (c is 'S' or 'A')
            {
                indirectStructSize = WasmLowering.ParseStructSize(sig, ref pos);
                Debug.Assert(indirectStructSize == type.GetElementSize().AsInt);
            }
            else if ((c is 'l' or 'V') && char.IsDigit(sig[pos + 1]))
            {
                wasmParamCount = sig[pos + 1] - '0';
                Debug.Assert(WasmLowering.TryGetMultiSegmentLayout(type, out WasmValueType slotType, out int slotCount) &&
                    (slotType == wasmParams.Types[wasmParamIndex]) && (slotCount == wasmParamCount));
                pos += 2;
            }
            else
            {
                pos++;
            }

            Debug.Assert(isIndirectStruct == (indirectStructSize != 0));
            WasmValueType wasmType = (wasmParamCount != 0) ? wasmParams.Types[wasmParamIndex] : default;
            args.Add(new WasmThunkArg(WasmThunkArgKind.Argument, offset, wasmParamIndex, wasmParamCount, wasmType, indirectStructSize, type));
            wasmParamIndex += wasmParamCount;
        }

        Debug.Assert(argit.GetNextOffset() == TransitionBlock.InvalidOffset);
        Debug.Assert((pos == sig.Length - 1) && (wasmParamIndex == wasmParams.Types.Length - 1),
            $"Wasm thunk argument layout does not cover the parameters of '{sig}'");

        Args = args.ToArray();
        PortableEntrypointParamIndex = wasmParamIndex;
    }

    /// <summary>
    /// Builds the ArgIterator for a Wasm thunk from its Wasm signature.
    /// </summary>
    internal static (MethodSignature, ArgIterator<TypeHandle>, TransitionBlock) BuildArgIterator(WasmSignature wasmSignature, TypeSystemContext context)
    {
        MethodSignature signature = WasmLowering.RaiseSignature(wasmSignature, context);
        bool isAsyncCall = wasmSignature.SignatureString.Contains('a');
        bool hasGenericContext = WasmLowering.HasGenericContextBeforeAsync(wasmSignature, context);
        if (hasGenericContext)
        {
            // RaiseSignature returns the generic context as the first parameter; lay it out as the hidden
            // instantiation argument instead, so it precedes the async continuation.
            TypeDesc[] parameters = new TypeDesc[signature.Length - 1];
            for (int i = 0; i < parameters.Length; i++)
            {
                parameters[i] = signature[i + 1];
            }

            signature = new MethodSignature(signature.Flags, signature.GenericParameterCount, signature.ReturnType, parameters);
        }

        (ArgIterator<TypeHandle> argit, TransitionBlock transitionBlock) = GCRefMapBuilder.BuildArgIterator(signature, context,
            methodRequiresInstArg: hasGenericContext,
            methodIsAsyncCall: isAsyncCall);

        return (signature, argit, transitionBlock);
    }

    internal static WasmExpr Load(WasmValueType type, int offset) => type switch
    {
        WasmValueType.I32 => I32.Load((ulong)offset),
        WasmValueType.I64 => I64.Load((ulong)offset),
        WasmValueType.F32 => F32.Load((ulong)offset),
        WasmValueType.F64 => F64.Load((ulong)offset),
        WasmValueType.V128 => V128.Load((ulong)offset),
        _ => throw new NotSupportedException($"Unexpected wasm type arg: {type}"),
    };

    internal static WasmExpr Store(WasmValueType type, int offset) => type switch
    {
        WasmValueType.I32 => I32.Store((ulong)offset),
        WasmValueType.I64 => I64.Store((ulong)offset),
        WasmValueType.F32 => F32.Store((ulong)offset),
        WasmValueType.F64 => F64.Store((ulong)offset),
        WasmValueType.V128 => V128.Store((ulong)offset),
        _ => throw new NotSupportedException($"Unexpected wasm type arg: {type}"),
    };
}
