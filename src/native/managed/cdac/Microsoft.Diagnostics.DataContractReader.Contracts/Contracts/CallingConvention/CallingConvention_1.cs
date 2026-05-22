// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Microsoft.Diagnostics.DataContractReader.Contracts.CallingConventionHelpers;
using Microsoft.Diagnostics.DataContractReader.RuntimeTypeSystemHelpers;
using Microsoft.Diagnostics.DataContractReader.SignatureHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

/// <summary>
/// CoreCLR implementation of <see cref="ICallingConvention"/>. Decodes method
/// signatures and drives a per-arch <see cref="ArgIteratorBase"/> subclass to
/// compute per-argument offsets and pass-style for a call site.
/// </summary>
internal sealed class CallingConvention_1 : ICallingConvention
{
    private static readonly IReadOnlyList<ArgLayout> EmptyArgs = Array.Empty<ArgLayout>();
    private static readonly CallSiteLayout EmptyLayout = new(null, false, null, null, EmptyArgs);

    private readonly Target _target;
    private readonly TransitionBlockLayout _layout;

    public CallingConvention_1(Target target)
    {
        _target = target;
        _layout = new TransitionBlockLayout(_target);
    }

    CallSiteLayout ICallingConvention.ComputeCallSiteLayout(MethodDescHandle method)
    {
        if (!TryDecodeSignature(method, out MethodSignature<ArgTypeInfo> methodSig))
            return EmptyLayout;

        bool isVarArg = methodSig.Header.CallingConvention is SignatureCallingConvention.VarArgs;

        bool hasThis = methodSig.Header.IsInstance;
        bool requiresInstArg = false;
        bool isAsync = false;
        bool isValueTypeThis = false;

        try
        {
            IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
            requiresInstArg = rts.GetGenericContextLoc(method) == GenericContextLoc.InstArg;
            isAsync = rts.IsAsyncMethod(method);
            if (hasThis)
            {
                TargetPointer methodTablePtr = rts.GetMethodTable(method);
                TypeHandle enclosingType = rts.GetTypeHandle(methodTablePtr);
                isValueTypeThis = rts.IsValueType(enclosingType);
            }
        }
        catch (System.Exception ex)
        {
            Debug.Fail(ex.ToString());
        }

        ArgTypeInfo[] paramTypes = new ArgTypeInfo[methodSig.ParameterTypes.Length];
        for (int i = 0; i < paramTypes.Length; i++)
            paramTypes[i] = methodSig.ParameterTypes[i];

        ArgIteratorData argData = new(
            hasThis,
            isVarArg,
            paramTypes,
            methodSig.ReturnType);

        ArgIteratorBase argit = ArgIteratorFactory.Create(
            _layout,
            argData,
            hasParamType: requiresInstArg,
            hasAsyncContinuation: isAsync);

        int? thisOffset = argit.HasThis ? argit.GetThisOffset() : null;
        int? asyncOffset = argit.HasAsyncContinuation ? argit.GetAsyncContinuationArgOffset() : null;
        int? varArgCookieOffset = isVarArg ? argit.GetVASigCookieOffset() : null;

        List<ArgLayout> args = new(paramTypes.Length);
        int argIndex = 0;
        foreach (ArgLocDesc loc in argit.EnumerateArgs())
        {
            if (argIndex >= paramTypes.Length)
                break;

            var slots = new List<ArgSlot>(loc.Locations.Count);
            foreach (ArgLocation l in loc.Locations)
                slots.Add(new ArgSlot(l.TransitionBlockOffset, l.ElementType));

            args.Add(new ArgLayout(loc.IsByRef, slots, ComputeValueTypeHandle(loc, slots)));
            argIndex++;
        }

        return new CallSiteLayout(thisOffset, isValueTypeThis, asyncOffset, varArgCookieOffset, args);
    }

    /// <summary>
    /// Mirrors native <c>MetaSig::GcScanRoots</c>'s value-type branch
    /// (see <c>src/coreclr/vm/siginfo.cpp</c>): when an argument is a value type
    /// passed by value in storage that the per-arch iterator did <em>not</em>
    /// GC-decompose, the GC scanner walks the type's layout to report embedded
    /// refs. Surface the value-type's <see cref="TypeHandle"/> on
    /// <see cref="ArgLayout"/> so the scanner can do that walk. The scanner
    /// dispatches on <see cref="IRuntimeTypeSystem.IsByRefLike"/> to choose
    /// between a CGCDesc walk (ordinary value types) and a field walk
    /// (ByRefLike types: <c>Span&lt;T&gt;</c>, ref structs).
    /// </summary>
    /// <returns>
    /// The value type's <see cref="TypeHandle"/> when the layout describes a
    /// contiguous by-value buffer that requires a layout-driven walk to report
    /// refs (including ByRefLike types); <see langword="null"/> otherwise
    /// (primitives, references, byref-passed value types, iterator-decomposed
    /// slots).
    /// </returns>
    private static TypeHandle? ComputeValueTypeHandle(ArgLocDesc loc, List<ArgSlot> slots)
    {
        // Pass-by-implicit-reference: the slot holds an interior pointer and the
        // GC scanner reports it via the byref path; no value-type walk needed.
        if (loc.IsByRef)
            return null;

        // Native dispatches by gElementTypeInfo[etype].m_gc; only ValueType /
        // TypedByRef route through the GCDesc walk. Other types are handled by
        // the per-slot ElementType already.
        if (loc.ArgType is not (CorElementType.ValueType or CorElementType.TypedByRef))
            return null;

        // If the per-arch iterator pre-decomposed the storage (e.g. SysV split
        // structs on AMD64-Unix emit per-eightbyte ElementTypes like Class/Byref/R8;
        // ARM64 HFAs emit per-FP-reg R4/R8 slots), the GC scanner already has
        // sufficient information via ArgSlot.ElementType. Walking GCDesc would
        // duplicate or contradict the iterator's classification. Discriminator:
        // every slot has ElementType == ValueType iff the iterator did NOT
        // decompose this argument.
        foreach (ArgSlot s in slots)
        {
            if (s.ElementType != CorElementType.ValueType)
                return null;
        }

        TypeHandle th = loc.ArgTypeInfo.RuntimeTypeHandle;
        if (!th.IsMethodTable())
            return null;

        // Both ordinary value types and ByRefLike types (Span<T>, ref structs) are
        // reported through ValueTypeHandle. The GC scanner dispatches on
        // IRuntimeTypeSystem.IsByRefLike to choose the walk strategy: a CGCDesc
        // series walk for ordinary value types, a field-by-field walk (parallel to
        // native ByRefPointerOffsetsReporter in siginfo.cpp) for ByRefLike types.
        return th;
    }

    /// <summary>
    /// Decodes the signature for <paramref name="method"/> into a
    /// <see cref="MethodSignature{ArgTypeInfo}"/>. Matches native
    /// <c>MethodDesc::GetSig</c>: prefers a stored signature (dynamic, EEImpl, and
    /// array method descs) before falling back to a metadata token lookup.
    /// </summary>
    private bool TryDecodeSignature(MethodDescHandle method, out MethodSignature<ArgTypeInfo> methodSig)
    {
        methodSig = default;
        try
        {
            IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;

            TargetPointer methodTablePtr = rts.GetMethodTable(method);
            TypeHandle typeHandle = rts.GetTypeHandle(methodTablePtr);
            TargetPointer modulePtr = rts.GetModule(typeHandle);

            ModuleHandle moduleHandle = _target.Contracts.Loader.GetModuleHandleFromModulePtr(modulePtr);
            MetadataReader? mdReader = _target.Contracts.EcmaMetadata.GetMetadata(moduleHandle);

            ArgTypeInfoSignatureProvider provider = new(_target, moduleHandle);
            ArgTypeInfoSignatureContext genericContext = new(typeHandle, method);

            if (rts.IsStoredSigMethodDesc(method, out ReadOnlySpan<byte> storedSig))
            {
                // Stored sigs (dynamic, EEImpl, array method descs) decode without needing
                // a metadata reader for primitive or ELEMENT_TYPE_INTERNAL element types.
                // A null reader is only a problem if the stored sig references a TypeDef /
                // TypeRef / TypeSpec, which is unusual for these method-desc kinds; the
                // outer catch handles that case gracefully.
                RuntimeSignatureDecoder<ArgTypeInfo, ArgTypeInfoSignatureContext> storedDecoder = new(
                    provider, _target, mdReader!, genericContext);
                unsafe
                {
                    fixed (byte* pStoredSig = storedSig)
                    {
                        BlobReader blobReader = new BlobReader(pStoredSig, storedSig.Length);
                        methodSig = storedDecoder.DecodeMethodSignature(ref blobReader);
                    }
                }
                return true;
            }

            // Non-stored-sig path: needs a real metadata reader to look up the method def.
            if (mdReader is null)
                return false;

            RuntimeSignatureDecoder<ArgTypeInfo, ArgTypeInfoSignatureContext> decoder = new(
                provider, _target, mdReader, genericContext);

            uint methodToken = rts.GetMethodToken(method);
            if (methodToken == (uint)EcmaMetadataUtils.TokenType.mdtMethodDef)
                return false;

            MethodDefinitionHandle methodDefHandle = MetadataTokens.MethodDefinitionHandle(
                (int)EcmaMetadataUtils.GetRowId(methodToken));
            MethodDefinition methodDef = mdReader.GetMethodDefinition(methodDefHandle);

            BlobReader bodyReader = mdReader.GetBlobReader(methodDef.Signature);
            methodSig = decoder.DecodeMethodSignature(ref bodyReader);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
