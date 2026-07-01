// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Internal.CallingConvention;
using Internal.CorConstants;
using Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;
using Microsoft.Diagnostics.DataContractReader.SignatureHelpers;

using CallingConventions = Internal.CallingConvention.CallingConventions;
using CdacCorElementType = Microsoft.Diagnostics.DataContractReader.Contracts.CorElementType;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal sealed class CallingConvention_1 : ICallingConvention
{
    private readonly Target _target;

    internal CallingConvention_1(Target target)
    {
        _target = target;
    }

    public bool TryComputeArgGCRefMapBlob(MethodDescHandle methodDesc, out byte[] blob)
    {
        try
        {
            byte[]? result = ComputeArgGCRefMapBlobCore(methodDesc);
            if (result is null)
            {
                blob = [];
                return false;
            }
            blob = result;
            return true;
        }
        catch (NotImplementedException)
        {
            // Any unported ABI path, including NIEs from GetArgumentLayout,
            // maps to a clean decline (false).
            blob = [];
            return false;
        }
    }

    // Result of GetArgumentLayout: a single ArgIterator walk produces the
    // per-argument locations the encoder iterates plus the x86 callee-pop
    // stack-byte count it needs for the WriteStackPop prefix. Bundled so the
    // implementation builds ArgIterator once per method instead of twice.
    private readonly record struct ArgumentLayout(
        IReadOnlyList<ArgumentLocation> Arguments,
        uint CbStackPop);

    private ArgumentLayout GetArgumentLayout(MethodDescHandle methodDesc)
    {
        IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
        IRuntimeInfo runtimeInfo = _target.Contracts.RuntimeInfo;

        MethodSignature<ITypeHandle> methodSig = DecodeMethodSignature(rts, methodDesc);

        bool isVarArg = methodSig.Header.CallingConvention is SignatureCallingConvention.VarArgs;

        bool hasThis = methodSig.Header.IsInstance;
        bool requiresInstArg = false;
        bool isAsync = false;
        try
        {
            GenericContextLoc ctxLoc = rts.GetGenericContextLoc(methodDesc);
            requiresInstArg = ctxLoc is GenericContextLoc.InstArgMethodDesc or GenericContextLoc.InstArgMethodTable;
            isAsync = rts.IsAsyncMethod(methodDesc);
        }
        catch
        {
        }

        CdacTypeHandle[] parameterTypes = new CdacTypeHandle[methodSig.ParameterTypes.Length];
        for (int i = 0; i < parameterTypes.Length; i++)
        {
            parameterTypes[i] = new CdacTypeHandle(methodSig.ParameterTypes[i], _target);
        }

        CdacTypeHandle returnType = new CdacTypeHandle(methodSig.ReturnType, _target);

        TransitionBlock transitionBlock = BuildTransitionBlock(runtimeInfo);

        CallingConventions callingConventions = hasThis
            ? CallingConventions.ManagedInstance
            : CallingConventions.ManagedStatic;

        ArgIteratorData<CdacTypeHandle> argIteratorData = new ArgIteratorData<CdacTypeHandle>(
            hasThis, isVarArg: isVarArg, parameterTypes, returnType);

        bool isWindows = runtimeInfo.GetTargetOperatingSystem() == RuntimeInfoOperatingSystem.Windows;

        ArgIterator<CdacTypeHandle> argit = new ArgIterator<CdacTypeHandle>(
            transitionBlock,
            argIteratorData,
            callingConventions,
            hasParamType: requiresInstArg,
            hasAsyncContinuation: isAsync,
            extraFunctionPointerArg: false,
            forcedByRefParams: new bool[parameterTypes.Length],
            skipFirstArg: false,
            extraObjectFirstArg: false,
            isWindows: isWindows,
            objectTypeHandle: GetObjectTypeHandle(rts),
            intPtrTypeHandle: GetIntPtrTypeHandle(rts));

        List<ArgumentLocation> arguments = new();

        if (hasThis)
        {
            TargetPointer methodTablePtr = rts.GetMethodTable(methodDesc);
            ITypeHandle owningType = rts.GetTypeHandle(methodTablePtr);
            bool isValueTypeThis = rts.IsValueType(owningType) && !rts.IsUnboxingStub(methodDesc);

            arguments.Add(new ArgumentLocation
            {
                Offset = transitionBlock.ThisOffset,
                ElementType = isValueTypeThis ? CdacCorElementType.ValueType : CdacCorElementType.Class,
                TypeHandle = owningType,
                IsThis = true,
                IsValueTypeThis = isValueTypeThis,
            });
        }

        if (argit.HasParamType)
        {
            arguments.Add(new ArgumentLocation
            {
                Offset = argit.GetParamTypeArgOffset(),
                ElementType = CdacCorElementType.I,
                IsParamType = true,
            });
        }

        if (argit.HasAsyncContinuation)
        {
            arguments.Add(new ArgumentLocation
            {
                Offset = argit.GetAsyncContinuationArgOffset(),
                ElementType = CdacCorElementType.Object,
            });
        }

        // VarArgs: mirror the runtime's FakeGcScanRoots short-circuit -- emit
        // the VASigCookie slot and stop. The variadic tail is reported via
        // the cookie's signature at GC scan time, not via this contract.
        // CbStackPop is 0 for VarArgs on x86 (caller cleans up), and
        // argit.CbStackPop() is unsafe to call on the VarArgs-configured
        // iterator -- short-circuit both here.
        if (isVarArg)
        {
            arguments.Add(new ArgumentLocation
            {
                Offset = argit.GetVASigCookieOffset(),
                ElementType = CdacCorElementType.I,
                IsVASigCookie = true,
            });
            return new ArgumentLayout(arguments, CbStackPop: 0);
        }

        int argIndex = 0;
        int argOffset;
        while ((argOffset = argit.GetNextOffset()) != TransitionBlock.InvalidOffset)
        {
            if (argIndex < parameterTypes.Length)
            {
                CdacCorElementType elemType = rts.GetSignatureCorElementType(methodSig.ParameterTypes[argIndex]);

                if (argOffset == TransitionBlock.StructInRegsOffset)
                    throw new NotImplementedException("SystemV AMD64 struct-in-registers is not yet supported by the cDAC.");

                bool passedByRef = elemType == CdacCorElementType.ValueType
                    && transitionBlock.IsArgPassedByRef(parameterTypes[argIndex]);

                // Detect ByRefLike value types (Span<T>, ReadOnlySpan<T>,
                // ref structs in general). The runtime emits one INTERIOR
                // token per managed-pointer field inside the unboxed struct
                // via ByRefPointerOffsetsReporter, in addition to any REF
                // tokens from GCDesc. For constructed generic instantiations
                // (Span<int>) the closed ITypeHandle may be uncached/null, so
                // we fall back to the open generic type captured during
                // signature decoding.
                bool isByRefLikeStruct = false;
                if (elemType == CdacCorElementType.ValueType && !passedByRef)
                {
                    ITypeHandle probe = methodSig.ParameterTypes[argIndex];
                    if (!probe.IsNull)
                    {
                        try { isByRefLikeStruct = rts.IsByRefLike(probe); }
                        catch { /* leave false on partial-state failures */ }
                    }
                }

                arguments.Add(new ArgumentLocation
                {
                    Offset = argOffset,
                    ElementType = elemType,
                    TypeHandle = methodSig.ParameterTypes[argIndex],
                    IsPassedByRef = passedByRef,
                    IsByRefLikeStruct = isByRefLikeStruct,
                });
            }
            argIndex++;
        }

        // CbStackPop is only consumed on x86; skip the call elsewhere.
        uint cbStackPop = runtimeInfo.GetTargetArchitecture() == RuntimeInfoArchitecture.X86
            ? argit.CbStackPop()
            : 0;

        return new ArgumentLayout(arguments, cbStackPop);
    }

    private MethodSignature<ITypeHandle> DecodeMethodSignature(
        IRuntimeTypeSystem rts, MethodDescHandle methodDesc)
    {
        TargetPointer methodTablePtr = rts.GetMethodTable(methodDesc);
        ITypeHandle typeHandle = rts.GetTypeHandle(methodTablePtr);
        TargetPointer modulePtr = rts.GetModule(typeHandle);

        ModuleHandle moduleHandle = _target.Contracts.Loader.GetModuleHandleFromModulePtr(modulePtr);
        MetadataReader? mdReader = _target.Contracts.EcmaMetadata.GetMetadata(moduleHandle);
        if (mdReader is null)
            throw new InvalidOperationException("Cannot read metadata for method");

        // Carry both the method and its owning type as the generic context so
        // ELEMENT_TYPE_VAR and _MVAR each resolve through the right
        // instantiation. The standard one-handle SignatureTypeProvider throws
        // NotSupportedException for whichever side it wasn't parameterized on.
        MethodSigContext context = new(methodDesc, typeHandle);
        MethodAndTypeContextProvider provider = new(_target, moduleHandle, rts);
        RuntimeSignatureDecoder<ITypeHandle, MethodSigContext> decoder = new(
            provider, _target, mdReader, context);

        if (rts.IsStoredSigMethodDesc(methodDesc, out ReadOnlySpan<byte> storedSig))
        {
            unsafe
            {
                fixed (byte* pStoredSig = storedSig)
                {
                    BlobReader blobReader = new(pStoredSig, storedSig.Length);
                    return decoder.DecodeMethodSignature(ref blobReader);
                }
            }
        }

        uint methodToken = rts.GetMethodToken(methodDesc);
        if (methodToken == (uint)EcmaMetadataUtils.TokenType.mdtMethodDef)
            throw new InvalidOperationException("Method has no token");

        MethodDefinitionHandle methodDefHandle = MetadataTokens.MethodDefinitionHandle(
            (int)EcmaMetadataUtils.GetRowId(methodToken));
        MethodDefinition methodDef = mdReader.GetMethodDefinition(methodDefHandle);
        BlobReader sigReader = mdReader.GetBlobReader(methodDef.Signature);
        return decoder.DecodeMethodSignature(ref sigReader);
    }

    private static TransitionBlock BuildTransitionBlock(IRuntimeInfo runtimeInfo)
    {
        RuntimeInfoArchitecture arch = runtimeInfo.GetTargetArchitecture();
        RuntimeInfoOperatingSystem os = runtimeInfo.GetTargetOperatingSystem();

        Internal.TypeSystem.TargetArchitecture targetArch = arch switch
        {
            RuntimeInfoArchitecture.X86 => Internal.TypeSystem.TargetArchitecture.X86,
            RuntimeInfoArchitecture.X64 => Internal.TypeSystem.TargetArchitecture.X64,
            RuntimeInfoArchitecture.Arm => Internal.TypeSystem.TargetArchitecture.ARM,
            RuntimeInfoArchitecture.Arm64 => Internal.TypeSystem.TargetArchitecture.ARM64,
            RuntimeInfoArchitecture.LoongArch64 => Internal.TypeSystem.TargetArchitecture.LoongArch64,
            RuntimeInfoArchitecture.RiscV64 => Internal.TypeSystem.TargetArchitecture.RiscV64,
            RuntimeInfoArchitecture.Wasm => Internal.TypeSystem.TargetArchitecture.Wasm32,
            _ => throw new NotSupportedException($"Unsupported architecture: {arch}"),
        };

        bool isWindows = os == RuntimeInfoOperatingSystem.Windows;
        bool isApplePlatform = os == RuntimeInfoOperatingSystem.Apple;

        return TransitionBlock.FromTarget(targetArch, isWindows, isApplePlatform, isArmel: false);
    }

    private CdacTypeHandle GetObjectTypeHandle(IRuntimeTypeSystem rts)
    {
        TargetPointer objectMt = rts.GetWellKnownMethodTable(WellKnownMethodTable.Object);
        return new CdacTypeHandle(rts.GetTypeHandle(objectMt), _target);
    }

    private CdacTypeHandle GetIntPtrTypeHandle(IRuntimeTypeSystem rts)
    {
        return new CdacTypeHandle(rts.GetPrimitiveType(CdacCorElementType.I), _target);
    }

    // Generic context for signature decoding that carries both the method
    // (for ELEMENT_TYPE_MVAR resolution) and its owning type (for
    // ELEMENT_TYPE_VAR resolution). The existing SignatureTypeProvider<T>
    // only resolves one or the other depending on T -- since a method
    // signature can reference both kinds of type parameters, we need both.
    internal readonly record struct MethodSigContext(MethodDescHandle Method, ITypeHandle OwningType);

    // SignatureTypeProvider variant that resolves both VAR (owning type's
    // type parameters) and MVAR (method's type parameters) by pulling the
    // appropriate field out of the MethodSigContext. Overrides the base
    // implementations, which only handle one direction.
    // Specialization that resolves generic parameters via the
    // MethodSigContext (open generic MD + owning ITypeHandle) instead of
    // requiring the context to be exactly a MethodDescHandle or ITypeHandle.
    //
    // The base SignatureTypeProvider<T> deliberately keeps its
    // GetGenericMethodParameter / GetGenericTypeParameter non-virtual to
    // avoid breaking downstream derived types (an override would change
    // the dispatch shape they shipped against). To still route the
    // signature decoder through this class's specialized lookups, we
    // re-implement the IRuntimeSignatureTypeProvider interface here:
    // hiding the base's methods with `new` and explicitly re-declaring
    // the interface in the type's base list causes the C# compiler to
    // emit a MethodImpl that rewires the interface slots to the
    // derived members. Result: through-interface dispatch (which is
    // how RuntimeSignatureDecoder calls them) lands on this class's
    // methods without making the base virtual.
    internal sealed class MethodAndTypeContextProvider
        : SignatureTypeProvider<MethodSigContext>,
          IRuntimeSignatureTypeProvider<ITypeHandle, MethodSigContext>
    {
        private readonly IRuntimeTypeSystem _rts;

        public MethodAndTypeContextProvider(Target target, ModuleHandle moduleHandle, IRuntimeTypeSystem rts)
            : base(target, moduleHandle)
        {
            _rts = rts;
        }

        public new ITypeHandle GetGenericMethodParameter(MethodSigContext context, int index)
            => _rts.GetGenericMethodInstantiation(context.Method)[index];

        public new ITypeHandle GetGenericTypeParameter(MethodSigContext context, int index)
            => _rts.GetInstantiation(context.OwningType)[index];
    }

    // =====================================================================
    // GCRefMap blob encoder. Produces byte-for-byte the same output as the
    // runtime's ComputeCallRefMap (frames.cpp) via the shared ArgIterator
    // walk above. Used by the cdacstress ArgIterator sub-check.
    // =====================================================================

    private const int MaxGCRefMapBlobLength = 252;
    private const int MaxByRefLikeRecursionDepth = 16;

    private byte[]? ComputeArgGCRefMapBlobCore(MethodDescHandle methodDesc)
    {
        IRuntimeInfo runtimeInfo = _target.Contracts.RuntimeInfo;
        IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;

        RuntimeInfoArchitecture arch = runtimeInfo.GetTargetArchitecture();
        bool isX86 = arch is RuntimeInfoArchitecture.X86;

        int pointerSize = _target.PointerSize;

        SortedDictionary<int, GCRefMapToken> tokens = new();
        ArgumentLayout enumeration = GetArgumentLayout(methodDesc);

        GenericContextLoc ctxLoc = GenericContextLoc.None;

        foreach (ArgumentLocation arg in enumeration.Arguments)
        {
            GCRefMapToken token;
            if (arg.IsThis)
            {
                token = arg.IsValueTypeThis ? GCRefMapToken.Interior : GCRefMapToken.Ref;
            }
            else if (arg.IsVASigCookie)
            {
                token = GCRefMapToken.VASigCookie;
            }
            else if (arg.IsParamType)
            {
                // Resolve InstArgMethodDesc vs InstArgMethodTable on demand
                // (cheaper than caching when most methods aren't generic).
                if (ctxLoc == GenericContextLoc.None)
                    ctxLoc = SafeGetGenericContextLoc(rts, methodDesc);

                token = ctxLoc switch
                {
                    GenericContextLoc.InstArgMethodDesc => GCRefMapToken.MethodParam,
                    GenericContextLoc.InstArgMethodTable => GCRefMapToken.TypeParam,
                    _ => GCRefMapToken.Skip,
                };
                if (token == GCRefMapToken.Skip)
                    continue;
            }
            else
            {
                switch ((CorElementType)arg.ElementType)
                {
                    case CorElementType.Class:
                    case CorElementType.String:
                    case CorElementType.Object:
                    case CorElementType.Array:
                    case CorElementType.SzArray:
                        token = GCRefMapToken.Ref;
                        break;

                    case CorElementType.Byref:
                        token = GCRefMapToken.Interior;
                        break;

                    case CorElementType.ValueType:
                        if (arg.IsPassedByRef)
                        {
                            token = GCRefMapToken.Interior;
                        }
                        else
                        {
                            bool emitted = false;

                            if (arg.IsByRefLikeStruct)
                            {
                                // ByRefLike value type (Span<T>, ReadOnlySpan<T>,
                                // ByteRef, any ref struct). Mirrors the runtime's
                                // ByRefPointerOffsetsReporter (siginfo.cpp): walk
                                // the type's instance fields and emit INTERIOR
                                // for each ELEMENT_TYPE_BYREF field at its
                                // in-struct offset. ELEMENT_TYPE_PTR / IntPtr /
                                // void* fields are explicitly NOT reported
                                // (so QCallTypeHandle, ObjectHandleOnStack,
                                // StringHandleOnStack contribute nothing).
                                //
                                // For uncached generic instantiations (Span<int>
                                // whose closed MT isn't loaded), the field
                                // layout lives on the open generic (Span<T>).
                                // The byref/ptr distinction is preserved at the
                                // FieldDesc level regardless of which T closes
                                // the type.
                                ITypeHandle probe = arg.TypeHandle;
                                if (!probe.IsNull)
                                {
                                    EmitByRefLikeInterior(rts, probe, arg.Offset, tokens);
                                }
                                emitted = true;
                            }

                            if (rts.ContainsGCPointers(arg.TypeHandle))
                            {
                                // By-value struct with embedded GC pointers: emit one
                                // Ref token per pointer slot inside the struct. Mirrors
                                // the runtime's ReportPointersFromValueTypeArg
                                // (siginfo.cpp). The GCDesc series Offset is relative
                                // to a boxed object's start (including the leading MT
                                // pointer); subtract pointerSize to translate to the
                                // unboxed in-frame layout.
                                int structFieldStart = arg.Offset - pointerSize;
                                foreach ((uint seriesOffset, uint seriesSize) in rts.GetGCDescSeries(arg.TypeHandle))
                                {
                                    int seriesBase = structFieldStart + (int)seriesOffset;
                                    for (int subOff = 0; subOff < (int)seriesSize; subOff += pointerSize)
                                    {
                                        tokens[seriesBase + subOff] = GCRefMapToken.Ref;
                                    }
                                }
                                emitted = true;
                            }

                            if (!emitted)
                                continue;
                            continue;
                        }
                        break;

                    default:
                        continue;
                }
            }

            tokens[arg.Offset] = token;
        }

        // No GC-significant arguments. On non-x86 the empty blob is just the
        // pending byte flush. On x86 it still carries the WriteStackPop prefix,
        // so emit that first.
        if (tokens.Count == 0)
        {
            if (!isX86)
                return EmptyGCRefMapBlob();
            GCRefMapEncoder enc0 = default;
            enc0.WriteStackPop(enumeration.CbStackPop / (uint)pointerSize);
            return enc0.Flush();
        }

        // Walk positions 0..maxPos and look up each one's offset in the token
        // map. This is necessary on x86 because pos-order and offset-order
        // diverge there (argument registers occupy the highest offsets but
        // the lowest positions). On non-x86 the mapping is monotonic so we
        // could iterate the offset map directly, but using OffsetFromGCRefMapPos
        // for both keeps the code path uniform.
        TransitionBlock tb = BuildTransitionBlock(runtimeInfo);

        // For x86 we need to know how many slot positions exist (we'd otherwise
        // miss high-pos register slots when the offset map's max is on the
        // stack). Walk every recorded offset and compute its position; for x86
        // OffsetFromGCRefMapPos is bijective so the inverse is well-defined.
        int maxPos = -1;
        foreach (int offset in tokens.Keys)
        {
            int pos = GCRefMapPosFromOffset(tb, offset, isX86, pointerSize);
            if (pos < 0)
                return null;  // alignment / out-of-range -- conservative skip
            if (pos > maxPos) maxPos = pos;
        }

        GCRefMapEncoder enc = default;
        if (isX86)
            enc.WriteStackPop(enumeration.CbStackPop / (uint)pointerSize);

        for (int pos = 0; pos <= maxPos; pos++)
        {
            int offset = tb.OffsetFromGCRefMapPos(pos);
            if (tokens.TryGetValue(offset, out GCRefMapToken token) && token != GCRefMapToken.Skip)
            {
                enc.WriteToken((uint)pos, (byte)token);
                if (enc.Length > MaxGCRefMapBlobLength)
                    return null;
            }
        }
        return enc.Flush();
    }

    // Inverse of TransitionBlock.OffsetFromGCRefMapPos. On non-x86 the mapping
    // is offset = first + pos*ptr, so pos = (offset - first) / ptr. On x86 the
    // first NumArgumentRegisters positions are argument registers laid out at
    // OffsetOfArgumentRegisters + ARGUMENTREGISTERS_SIZE - (pos+1)*ptr; the
    // remaining positions are stack args at OffsetOfArgs + (pos - n)*ptr.
    // Returns -1 on misalignment.
    private static int GCRefMapPosFromOffset(TransitionBlock tb, int offset, bool isX86, int pointerSize)
    {
        if (!isX86)
        {
            int delta = offset - tb.OffsetOfFirstGCRefMapSlot;
            if (delta < 0 || delta % pointerSize != 0) return -1;
            return delta / pointerSize;
        }

        // x86: arg registers come first in pos order, then stack args.
        int argRegBase = tb.OffsetOfArgumentRegisters;
        int argRegEnd = argRegBase + tb.NumArgumentRegisters * pointerSize;
        if (offset >= argRegBase && offset < argRegEnd)
        {
            int delta = offset - argRegBase;
            if (delta % pointerSize != 0) return -1;
            // Reverse: pos = NumArgumentRegisters - 1 - (delta / ptr)
            return tb.NumArgumentRegisters - 1 - (delta / pointerSize);
        }
        if (offset >= tb.OffsetOfArgs)
        {
            int delta = offset - tb.OffsetOfArgs;
            if (delta % pointerSize != 0) return -1;
            return tb.NumArgumentRegisters + (delta / pointerSize);
        }
        return -1;
    }

    private static GenericContextLoc SafeGetGenericContextLoc(IRuntimeTypeSystem rts, MethodDescHandle md)
    {
        try
        {
            return rts.GetGenericContextLoc(md);
        }
        catch
        {
            return GenericContextLoc.None;
        }
    }

    // Mirror of runtime ByRefPointerOffsetsReporter (siginfo.cpp): walk the
    // instance fields of a ByRefLike value type and emit one INTERIOR token
    // per ELEMENT_TYPE_BYREF field at its offset within the unboxed struct
    // (so absolute offset is baseOffset + fieldOffset). Recurses into nested
    // ByRefLike value-type fields. ELEMENT_TYPE_PTR / IntPtr / void* fields
    // are deliberately skipped to match runtime behavior for QCall-style
    // handle wrappers.
    private static void EmitByRefLikeInterior(
        IRuntimeTypeSystem rts,
        ITypeHandle byRefLikeType,
        int baseOffset,
        SortedDictionary<int, GCRefMapToken> tokens)
    {
        // Bound recursion just in case the data is corrupt / cycles in a dump.
        EmitByRefLikeInteriorRecursive(rts, byRefLikeType, baseOffset, tokens, depth: 0);
    }

    private static void EmitByRefLikeInteriorRecursive(
        IRuntimeTypeSystem rts,
        ITypeHandle byRefLikeType,
        int baseOffset,
        SortedDictionary<int, GCRefMapToken> tokens,
        int depth)
    {
        if (depth > MaxByRefLikeRecursionDepth)
            return;
        if (byRefLikeType.Address == TargetPointer.Null)
            return;

        IEnumerable<TargetPointer> fieldDescs;
        try
        {
            fieldDescs = rts.GetFieldDescList(byRefLikeType);
        }
        catch
        {
            return;
        }

        foreach (TargetPointer fdPtr in fieldDescs)
        {
            bool isStatic;
            CorElementType fieldType;
            uint fieldOffset;
            try
            {
                isStatic = rts.IsFieldDescStatic(fdPtr);
                if (isStatic)
                    continue;
                fieldType = rts.GetFieldDescType(fdPtr);
                fieldOffset = rts.GetFieldDescOffset(fdPtr, fieldDef: null);
            }
            catch
            {
                continue;
            }

            int absOffset = baseOffset + (int)fieldOffset;

            if (fieldType == CorElementType.Byref)
            {
                tokens[absOffset] = GCRefMapToken.Interior;
            }
            else if (fieldType == CorElementType.ValueType)
            {
                // Nested value-type field. Recurse only if the field's own
                // MethodTable is ByRefLike (matches runtime Find(FieldDesc*)
                // in ByRefPointerOffsetsReporter).
                ITypeHandle nested = rts.GetFieldDescApproxTypeHandle(fdPtr);
                if (nested.Address == TargetPointer.Null)
                    continue;
                bool nestedByRefLike;
                try { nestedByRefLike = rts.IsByRefLike(nested); }
                catch { continue; }
                if (!nestedByRefLike)
                    continue;
                EmitByRefLikeInteriorRecursive(rts, nested, absOffset, tokens, depth + 1);
            }
        }
    }

    private static byte[] EmptyGCRefMapBlob()
    {
        GCRefMapEncoder enc = default;
        return enc.Flush();
    }

    // Bit-stream encoder mirroring native GCRefMapBuilder (inc/gcrefmap.h).
    // Every encoding rule -- AppendBit's 7-bit chunks with high-bit
    // continuation, WriteToken's delta encoding, Flush's final byte --
    // matches byte-for-byte.
    private struct GCRefMapEncoder
    {
        private int _pendingByte;
        private int _bits;
        private uint _pos;
        private List<byte> _bytes;

        public int Length => _bytes?.Count ?? 0;

        private void AppendBit(uint bit)
        {
            _bytes ??= new List<byte>(8);
            if (bit != 0)
            {
                while (_bits >= 7)
                {
                    _bytes.Add((byte)(_pendingByte | 0x80));
                    _pendingByte = 0;
                    _bits -= 7;
                }
                _pendingByte |= 1 << _bits;
            }
            _bits++;
        }

        private void AppendTwoBit(uint bits)
        {
            AppendBit(bits & 1);
            AppendBit(bits >> 1);
        }

        private void AppendInt(uint val)
        {
            do
            {
                AppendBit(val & 1);
                AppendBit((val >> 1) & 1);
                AppendBit((val >> 2) & 1);
                val >>= 3;
                AppendBit(val != 0 ? 1u : 0u);
            }
            while (val != 0);
        }

        // x86-only prefix: encode the callee-popped stack-byte count in
        // pointer-size units before any tokens. Mirrors native
        // GCRefMapBuilder::WriteStackPop (inc/gcrefmap.h). Must be called
        // before the first WriteToken.
        public void WriteStackPop(uint stackPop)
        {
            if (stackPop < 3)
            {
                AppendTwoBit(stackPop);
            }
            else
            {
                AppendTwoBit(3);
                AppendInt(stackPop - 3);
            }
        }

        public void WriteToken(uint pos, uint token)
        {
            uint posDelta = pos - _pos;
            _pos = pos + 1;

            if (posDelta != 0)
            {
                if (posDelta < 4)
                {
                    while (posDelta > 0)
                    {
                        AppendTwoBit(0);
                        posDelta--;
                    }
                }
                else
                {
                    AppendTwoBit(3);
                    AppendInt((posDelta - 4) << 1);
                }
            }

            if (token < 3)
            {
                AppendTwoBit(token);
            }
            else
            {
                AppendTwoBit(3);
                AppendInt(((token - 3) << 1) | 1);
            }
        }

        public byte[] Flush()
        {
            _bytes ??= new List<byte>(1);
            if ((_pendingByte & 0x7F) != 0 || _pos == 0)
                _bytes.Add((byte)(_pendingByte & 0x7F));

            return _bytes.ToArray();
        }
    }
}
