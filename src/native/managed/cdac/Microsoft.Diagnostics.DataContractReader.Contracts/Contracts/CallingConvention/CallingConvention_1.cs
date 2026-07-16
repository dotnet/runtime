// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using Internal.CallingConvention;
using Internal.CorConstants;
using Internal.JitInterface;
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

    // Per-parameter metadata captured at signature-decode time. We track this
    // out-of-band because the standard SignatureTypeProvider collapses
    // ELEMENT_TYPE_BYREF, _PTR, _SZARRAY, and _ARRAY into the underlying type
    // (or a null TypeHandle when the runtime hasn't cached the constructed
    // form), making the top-level element type unrecoverable from
    // methodSig.ParameterTypes alone.
    private readonly struct ParamTypeInfo
    {
        // Set if the parameter is wrapped in ELEMENT_TYPE_BYREF.
        public bool IsByRef { get; init; }

        // Outermost element type of the parameter signature, if known
        // (Byref / Ptr / SzArray / Array). The enum's zero value (default)
        // means "no constructed-type wrapper -- caller should fall back to
        // GetSignatureCorElementType on the underlying TypeHandle".
        public CdacCorElementType OutermostKind { get; init; }

        // For generic-instantiation parameters, the open generic type
        // (e.g. Span<T> for a Span<int> arg). Used by the encoder when the
        // constructed TypeHandle is null (uncached) to fall back to
        // attributes of the open type (IsByRefLike, etc.).
        public TypeHandle OpenGenericType { get; init; }
    }

    private ArgumentLayout GetArgumentLayout(MethodDescHandle methodDesc)
    {
        IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
        IRuntimeInfo runtimeInfo = _target.Contracts.RuntimeInfo;

        MethodSignature<TypeHandle> methodSig = DecodeMethodSignature(rts, methodDesc);

        // Re-decode the same signature with a wrapper provider to learn each
        // parameter's outermost element type (Byref / Ptr / SzArray / Array)
        // and whether it's wrapped in ELEMENT_TYPE_BYREF. The standard
        // SignatureTypeProvider hides these wrappers (returning a null
        // TypeHandle when GetConstructedType isn't cached), so without this
        // out-of-band metadata the encoder would silently drop any arg whose
        // outermost wrapper isn't in the loader's available-type-params list.
        ParamTypeInfo[] paramInfo = DecodeParamTypeInfo(rts, methodDesc, methodSig.ParameterTypes.Length);

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
            parameterTypes[i] = new CdacTypeHandle(methodSig.ParameterTypes[i], _target, paramInfo[i].OutermostKind);
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
            TypeHandle owningType = rts.GetTypeHandle(methodTablePtr);
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
                CdacCorElementType elemType;
                if (paramInfo[argIndex].IsByRef)
                {
                    // ELEMENT_TYPE_BYREF wrapper: pass-by-reference (managed pointer).
                    elemType = CdacCorElementType.Byref;
                }
                else if (paramInfo[argIndex].OutermostKind != default(CdacCorElementType))
                {
                    // Outermost wrapper was something the standard signature
                    // provider may have dropped (SzArray / Array / Ptr). Use
                    // the kind we recorded during the wrapper-provider walk.
                    elemType = paramInfo[argIndex].OutermostKind;
                }
                else
                {
                    elemType = rts.GetSignatureCorElementType(methodSig.ParameterTypes[argIndex]);
                }

                if (argOffset == TransitionBlock.StructInRegsOffset)
                {
                    // SystemV-AMD64 struct-in-registers.
                    SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR sysvDesc;
                    parameterTypes[argIndex].GetSystemVAmd64PassStructInRegisterDescriptor(out sysvDesc);
                    ArgLocDesc loc = argit.GetArgLoc(argOffset) ?? throw new InvalidOperationException("ArgIterator returned null ArgLocDesc for struct-in-registers argument");

                    arguments.Add(new ArgumentLocation
                    {
                        Offset = argOffset,
                        ElementType = elemType,
                        TypeHandle = methodSig.ParameterTypes[argIndex],
                        IsStructPassedInRegs = true,
                        SysVEightByteDescriptor = sysvDesc,
                        SysVIdxGenReg = loc.m_idxGenReg,
                        OpenGenericType = paramInfo[argIndex].OpenGenericType,
                    });
                    argIndex++;
                    continue;
                }

                bool passedByRef = elemType == CdacCorElementType.ValueType
                    && transitionBlock.IsArgPassedByRef(parameterTypes[argIndex]);

                // Detect ByRefLike value types (Span<T>, ReadOnlySpan<T>,
                // ref structs in general). The runtime emits one INTERIOR
                // token per managed-pointer field inside the unboxed struct
                // via ByRefPointerOffsetsReporter, in addition to any REF
                // tokens from GCDesc. For constructed generic instantiations
                // (Span<int>) the closed TypeHandle may be uncached/null, so
                // we fall back to the open generic type captured during
                // signature decoding.
                bool isByRefLikeStruct = false;
                if (elemType == CdacCorElementType.ValueType && !passedByRef)
                {
                    TypeHandle probe = methodSig.ParameterTypes[argIndex];
                    if (probe.Address == TargetPointer.Null)
                        probe = paramInfo[argIndex].OpenGenericType;
                    if (probe.Address != TargetPointer.Null)
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
                    OpenGenericType = paramInfo[argIndex].OpenGenericType,
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

    private MethodSignature<TypeHandle> DecodeMethodSignature(
        IRuntimeTypeSystem rts, MethodDescHandle methodDesc)
    {
        TargetPointer methodTablePtr = rts.GetMethodTable(methodDesc);
        TypeHandle typeHandle = rts.GetTypeHandle(methodTablePtr);
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
        RuntimeSignatureDecoder<TypeHandle, MethodSigContext> decoder = new(
            provider, _target, mdReader, context);

        if (!rts.TryGetMethodSignature(methodDesc, out ReadOnlySpan<byte> methodSig))
            throw new InvalidOperationException("Method has no signature");

        unsafe
        {
            fixed (byte* pSig = methodSig)
            {
                BlobReader blobReader = new(pSig, methodSig.Length);
                return decoder.DecodeMethodSignature(ref blobReader);
            }
        }
    }

    // Re-decode the method signature using a wrapper provider that records
    // per-parameter metadata the standard signature provider would discard:
    //   - whether the parameter is wrapped in ELEMENT_TYPE_BYREF, and
    //   - the outermost element type (SzArray / Array / Ptr / Byref) so
    //     constructed-type wrappers the runtime hasn't cached don't get
    //     silently dropped via null TypeHandles.
    private ParamTypeInfo[] DecodeParamTypeInfo(IRuntimeTypeSystem rts, MethodDescHandle methodDesc, int paramCount)
    {
        if (paramCount == 0)
            return Array.Empty<ParamTypeInfo>();

        TargetPointer methodTablePtr = rts.GetMethodTable(methodDesc);
        TypeHandle typeHandle = rts.GetTypeHandle(methodTablePtr);
        TargetPointer modulePtr = rts.GetModule(typeHandle);

        ModuleHandle moduleHandle = _target.Contracts.Loader.GetModuleHandleFromModulePtr(modulePtr);
        MetadataReader? mdReader = _target.Contracts.EcmaMetadata.GetMetadata(moduleHandle);
        if (mdReader is null)
            return new ParamTypeInfo[paramCount];

        MethodSigContext context = new(methodDesc, typeHandle);
        ParamMetadataProvider provider = new(new MethodAndTypeContextProvider(_target, moduleHandle, rts), rts);
        RuntimeSignatureDecoder<TrackedType, MethodSigContext> decoder = new(
            provider, _target, mdReader, context);

        if (!rts.TryGetMethodSignature(methodDesc, out ReadOnlySpan<byte> methodSig))
            return new ParamTypeInfo[paramCount];

        MethodSignature<TrackedType> sig;
        unsafe
        {
            fixed (byte* pSig = methodSig)
            {
                BlobReader blobReader = new(pSig, methodSig.Length);
                sig = decoder.DecodeMethodSignature(ref blobReader);
            }
        }

        ParamTypeInfo[] result = new ParamTypeInfo[paramCount];
        int count = Math.Min(paramCount, sig.ParameterTypes.Length);
        for (int i = 0; i < count; i++)
        {
            TrackedType t = sig.ParameterTypes[i];
            result[i] = new ParamTypeInfo
            {
                IsByRef = t.IsByRef,
                OutermostKind = t.OutermostKind,
                OpenGenericType = t.OpenGeneric,
            };
        }
        return result;
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

    // Well-known type handles passed to ArgIterator. The shared iterator only
    // dereferences them when extraObjectFirstArg / extraFunctionPointerArg are
    // set; this contract never sets either, so the lookups are cheap insurance
    // against a future cDAC change tripping a NullReferenceException deep in
    // GetArgumentType.
    private CdacTypeHandle GetObjectTypeHandle(IRuntimeTypeSystem rts)
    {
        TargetPointer objectMt = rts.GetWellKnownMethodTable(WellKnownMethodTable.Object);
        return new CdacTypeHandle(rts.GetTypeHandle(objectMt), _target);
    }

    private CdacTypeHandle GetIntPtrTypeHandle(IRuntimeTypeSystem rts)
    {
        return new CdacTypeHandle(rts.GetPrimitiveType(CdacCorElementType.I), _target);
    }

    // Result type produced by ParamMetadataProvider. Carries the underlying
    // TypeHandle (resolved by the inner provider when possible) plus the
    // outermost element type and an IsByRef flag, both of which the standard
    // SignatureTypeProvider would otherwise drop on the floor when the runtime
    // hasn't cached the constructed-type instantiation.
    private readonly struct TrackedType
    {
        public TypeHandle Underlying { get; init; }
        public bool IsByRef { get; init; }
        // The outermost ELEMENT_TYPE_* wrapper applied to this signature.
        // The enum's zero value (default) means "no constructed-type wrapper;
        // use GetSignatureCorElementType on Underlying instead".
        public CdacCorElementType OutermostKind { get; init; }
        // For generic instantiations: the open generic type before
        // GetConstructedType collapsed it. Lets the encoder inspect
        // attributes (IsByRefLike, etc.) even when the constructed
        // TypeHandle isn't cached.
        public TypeHandle OpenGeneric { get; init; }
    }

    // ISignatureTypeProvider wrapper that records the outermost
    // ELEMENT_TYPE_* wrapper (BYREF / PTR / SZARRAY / ARRAY) on each parameter
    // so the caller can recover that information even when the standard
    // SignatureTypeProvider would have returned a null TypeHandle from
    // GetConstructedType. Used only by DecodeParamTypeInfo. The generic
    // context is a MethodDescHandle so both ELEMENT_TYPE_VAR and _MVAR can be
    // resolved by the inner MethodGenericContextProvider.
    private sealed class ParamMetadataProvider : IRuntimeSignatureTypeProvider<TrackedType, MethodSigContext>
    {
        private readonly MethodAndTypeContextProvider _inner;
        private readonly IRuntimeTypeSystem _rts;

        public ParamMetadataProvider(MethodAndTypeContextProvider inner, IRuntimeTypeSystem rts)
        {
            _inner = inner;
            _rts = rts;
        }

        // Helpers: Wrap stamps Underlying but leaves OutermostKind at default
        // (the enum's 0 value, which CdacCorElementType doesn't name) so callers
        // know to fall back to GetSignatureCorElementType on Underlying. The
        // constructed-type overrides (ByRef/Ptr/SzArray/Array) set
        // OutermostKind explicitly.
        private static TrackedType Wrap(TypeHandle th)
            => new() { Underlying = th };

        public TrackedType GetByReferenceType(TrackedType elementType)
            => new() { Underlying = elementType.Underlying, IsByRef = true,
                       OutermostKind = CdacCorElementType.Byref };

        public TrackedType GetPointerType(TrackedType elementType)
            => new() { Underlying = elementType.Underlying,
                       OutermostKind = CdacCorElementType.Ptr };

        public TrackedType GetArrayType(TrackedType elementType, ArrayShape shape)
            => new() { Underlying = _inner.GetArrayType(elementType.Underlying, shape),
                       OutermostKind = CdacCorElementType.Array };

        public TrackedType GetSZArrayType(TrackedType elementType)
            => new() { Underlying = _inner.GetSZArrayType(elementType.Underlying),
                       OutermostKind = CdacCorElementType.SzArray };

        public TrackedType GetFunctionPointerType(MethodSignature<TrackedType> signature)
            => Wrap(_inner.GetPrimitiveType(PrimitiveTypeCode.IntPtr));

        public TrackedType GetGenericInstantiation(TrackedType genericType, ImmutableArray<TrackedType> typeArguments)
        {
            ImmutableArray<TypeHandle>.Builder builder = ImmutableArray.CreateBuilder<TypeHandle>(typeArguments.Length);
            for (int i = 0; i < typeArguments.Length; i++)
                builder.Add(typeArguments[i].Underlying);
            TypeHandle constructed = _inner.GetGenericInstantiation(genericType.Underlying, builder.ToImmutable());

            // GetConstructedType returns null when the runtime hasn't cached
            // this exact instantiation. Recover the would-be top-level kind
            // (Class / ValueType / ...) from the open generic type so the
            // encoder still sees the right token (REF for class, etc.).
            CdacCorElementType kind = default;
            if (constructed.Address == TargetPointer.Null && genericType.Underlying.Address != TargetPointer.Null)
            {
                try { kind = _rts.GetSignatureCorElementType(genericType.Underlying); }
                catch { /* leave default */ }
            }
            return new TrackedType
            {
                Underlying = constructed,
                OutermostKind = kind,
                OpenGeneric = genericType.Underlying,
            };
        }

        public TrackedType GetGenericMethodParameter(MethodSigContext context, int index)
            => Wrap(_inner.GetGenericMethodParameter(context, index));

        public TrackedType GetGenericTypeParameter(MethodSigContext context, int index)
            => Wrap(_inner.GetGenericTypeParameter(context, index));

        public TrackedType GetModifiedType(TrackedType modifier, TrackedType unmodifiedType, bool isRequired)
            => unmodifiedType;

        public TrackedType GetPinnedType(TrackedType elementType)
            => elementType;

        public TrackedType GetPrimitiveType(PrimitiveTypeCode typeCode)
            => Wrap(_inner.GetPrimitiveType(typeCode));

        public TrackedType GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
            => Wrap(_inner.GetTypeFromDefinition(reader, handle, rawTypeKind));

        public TrackedType GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
            => Wrap(_inner.GetTypeFromReference(reader, handle, rawTypeKind));

        public TrackedType GetTypeFromSpecification(MetadataReader reader, MethodSigContext context, TypeSpecificationHandle handle, byte rawTypeKind)
            => Wrap(_inner.GetTypeFromSpecification(reader, context, handle, rawTypeKind));

        public TrackedType GetInternalType(TargetPointer typeHandlePointer)
            => Wrap(_inner.GetInternalType(typeHandlePointer));

        public TrackedType GetInternalModifiedType(TargetPointer typeHandlePointer, TrackedType unmodifiedType, bool isRequired)
            => unmodifiedType;
    }

    // Generic context for signature decoding that carries both the method
    // (for ELEMENT_TYPE_MVAR resolution) and its owning type (for
    // ELEMENT_TYPE_VAR resolution). The existing SignatureTypeProvider<T>
    // only resolves one or the other depending on T -- since a method
    // signature can reference both kinds of type parameters, we need both.
    internal readonly record struct MethodSigContext(MethodDescHandle Method, TypeHandle OwningType);

    // SignatureTypeProvider variant that resolves both VAR (owning type's
    // type parameters) and MVAR (method's type parameters) by pulling the
    // appropriate field out of the MethodSigContext. Overrides the base
    // implementations, which only handle one direction.
    // Specialization that resolves generic parameters via the
    // MethodSigContext (open generic MD + owning TypeHandle) instead of
    // requiring the context to be exactly a MethodDescHandle or TypeHandle.
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
          IRuntimeSignatureTypeProvider<TypeHandle, MethodSigContext>
    {
        private readonly IRuntimeTypeSystem _rts;

        public MethodAndTypeContextProvider(Target target, ModuleHandle moduleHandle, IRuntimeTypeSystem rts)
            : base(target, moduleHandle)
        {
            _rts = rts;
        }

        public new TypeHandle GetGenericMethodParameter(MethodSigContext context, int index)
            => _rts.GetGenericMethodInstantiation(context.Method)[index];

        public new TypeHandle GetGenericTypeParameter(MethodSigContext context, int index)
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
        TransitionBlock tb = BuildTransitionBlock(runtimeInfo);

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
            else if (arg.IsStructPassedInRegs)
            {
                // Mirrors ArgDestination::ReportPointersFromStructInRegisters
                // in src/coreclr/vm/argdestination.h.
                int genRegOffset = tb.OffsetOfArgumentRegisters + arg.SysVIdxGenReg * pointerSize;
                for (int i = 0; i < arg.SysVEightByteDescriptor.eightByteCount; i++)
                {
                    SystemVClassificationType cls = (i == 0)
                        ? arg.SysVEightByteDescriptor.eightByteClassifications0
                        : arg.SysVEightByteDescriptor.eightByteClassifications1;
                    int size = (i == 0)
                        ? arg.SysVEightByteDescriptor.eightByteSizes0
                        : arg.SysVEightByteDescriptor.eightByteSizes1;

                    // SSE eightbytes go to XMM regs; don't advance genRegOffset.
                    if (cls == SystemVClassificationType.SystemVClassificationTypeSSE)
                        continue;

                    if (cls == SystemVClassificationType.SystemVClassificationTypeIntegerReference)
                        tokens[genRegOffset] = GCRefMapToken.Ref;
                    else if (cls == SystemVClassificationType.SystemVClassificationTypeIntegerByRef)
                        tokens[genRegOffset] = GCRefMapToken.Interior;

                    genRegOffset += size;
                }
                continue;
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
                                TypeHandle probe = arg.TypeHandle;
                                if (probe.Address == TargetPointer.Null)
                                    probe = arg.OpenGenericType;
                                if (probe.Address != TargetPointer.Null)
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
        TypeHandle byRefLikeType,
        int baseOffset,
        SortedDictionary<int, GCRefMapToken> tokens)
    {
        // Bound recursion just in case the data is corrupt / cycles in a dump.
        EmitByRefLikeInteriorRecursive(rts, byRefLikeType, baseOffset, tokens, depth: 0);
    }

    private static void EmitByRefLikeInteriorRecursive(
        IRuntimeTypeSystem rts,
        TypeHandle byRefLikeType,
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
                TypeHandle nested = rts.GetFieldDescApproxTypeHandle(fdPtr);
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
