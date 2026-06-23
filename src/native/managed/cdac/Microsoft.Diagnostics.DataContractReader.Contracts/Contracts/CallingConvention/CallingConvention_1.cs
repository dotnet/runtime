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

using ArgIterator = Internal.CallingConvention.ArgIterator;
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

    public byte[]? TryComputeArgGCRefMapBlob(MethodDescHandle methodDesc)
    {
        try
        {
            return CallingConventionGCRefMapBuilder.TryBuild(_target, methodDesc);
        }
        catch
        {
            // Any thrown exception from EnumerateArguments / signature decode
            // makes the result unusable; treat as "cDAC can't encode this MD".
            return null;
        }
    }

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
    }

    public IEnumerable<ArgumentLocation> EnumerateArguments(MethodDescHandle methodDesc)
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

        if (methodSig.Header.CallingConvention is SignatureCallingConvention.VarArgs)
        {
            throw new NotImplementedException("VarArgs calling convention is not yet supported by the cDAC.");
        }

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

        ITypeHandle[] parameterTypes = new ITypeHandle[methodSig.ParameterTypes.Length];
        for (int i = 0; i < parameterTypes.Length; i++)
        {
            parameterTypes[i] = new CdacTypeHandle(methodSig.ParameterTypes[i], _target);
        }

        ITypeHandle returnType = new CdacTypeHandle(methodSig.ReturnType, _target);

        TransitionBlock transitionBlock = BuildTransitionBlock(runtimeInfo);

        CallingConventions callingConventions = hasThis
            ? CallingConventions.ManagedInstance
            : CallingConventions.ManagedStatic;

        ArgIteratorData argIteratorData = new ArgIteratorData(
            hasThis, isVarArg: false, parameterTypes, returnType);

        bool isWindows = runtimeInfo.GetTargetOperatingSystem() == RuntimeInfoOperatingSystem.Windows;

        ArgIterator argit = new ArgIterator(
            transitionBlock,
            argIteratorData,
            callingConventions,
            hasParamType: requiresInstArg,
            hasAsyncContinuation: isAsync,
            extraFunctionPointerArg: false,
            forcedByRefParams: new bool[parameterTypes.Length],
            skipFirstArg: false,
            extraObjectFirstArg: false,
            isWindows: isWindows);

        if (hasThis)
        {
            TargetPointer methodTablePtr = rts.GetMethodTable(methodDesc);
            TypeHandle owningType = rts.GetTypeHandle(methodTablePtr);
            bool isValueTypeThis = rts.IsValueType(owningType) && !rts.IsUnboxingStub(methodDesc);

            yield return new ArgumentLocation
            {
                Offset = transitionBlock.ThisOffset,
                ElementType = isValueTypeThis ? CdacCorElementType.ValueType : CdacCorElementType.Class,
                TypeHandle = owningType,
                IsThis = true,
                IsValueTypeThis = isValueTypeThis,
            };
        }

        if (argit.HasParamType)
        {
            yield return new ArgumentLocation
            {
                Offset = argit.GetParamTypeArgOffset(),
                ElementType = CdacCorElementType.I,
                IsParamType = true,
            };
        }

        if (argit.HasAsyncContinuation)
        {
            yield return new ArgumentLocation
            {
                Offset = argit.GetAsyncContinuationArgOffset(),
                ElementType = CdacCorElementType.Object,
            };
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
                    throw new NotImplementedException("SystemV AMD64 struct-in-registers is not yet supported by the cDAC.");

                bool passedByRef = elemType == CdacCorElementType.ValueType
                    && transitionBlock.IsArgPassedByRef(parameterTypes[argIndex]);

                yield return new ArgumentLocation
                {
                    Offset = argOffset,
                    ElementType = elemType,
                    TypeHandle = methodSig.ParameterTypes[argIndex],
                    IsPassedByRef = passedByRef,
                };
            }
            argIndex++;
        }
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

        MethodSignature<TrackedType> sig;
        if (rts.IsStoredSigMethodDesc(methodDesc, out ReadOnlySpan<byte> storedSig))
        {
            unsafe
            {
                fixed (byte* pStoredSig = storedSig)
                {
                    BlobReader blobReader = new(pStoredSig, storedSig.Length);
                    sig = decoder.DecodeMethodSignature(ref blobReader);
                }
            }
        }
        else
        {
            uint methodToken = rts.GetMethodToken(methodDesc);
            if (methodToken == (uint)EcmaMetadataUtils.TokenType.mdtMethodDef)
                return new ParamTypeInfo[paramCount];

            MethodDefinitionHandle methodDefHandle = MetadataTokens.MethodDefinitionHandle(
                (int)EcmaMetadataUtils.GetRowId(methodToken));
            MethodDefinition methodDef = mdReader.GetMethodDefinition(methodDefHandle);
            BlobReader sigReader = mdReader.GetBlobReader(methodDef.Signature);
            sig = decoder.DecodeMethodSignature(ref sigReader);
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

        // Helpers: Wrap stamps Underlying but leaves OutermostKind == End so
        // callers know to fall back to GetSignatureCorElementType on Underlying.
        // The constructed-type overrides (ByRef/Ptr/SzArray/Array) override
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
            return new TrackedType { Underlying = constructed, OutermostKind = kind };
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
    internal sealed class MethodAndTypeContextProvider : SignatureTypeProvider<MethodSigContext>
    {
        private readonly IRuntimeTypeSystem _rts;

        public MethodAndTypeContextProvider(Target target, ModuleHandle moduleHandle, IRuntimeTypeSystem rts)
            : base(target, moduleHandle)
        {
            _rts = rts;
        }

        public override TypeHandle GetGenericMethodParameter(MethodSigContext context, int index)
            => _rts.GetGenericMethodInstantiation(context.Method)[index];

        public override TypeHandle GetGenericTypeParameter(MethodSigContext context, int index)
            => _rts.GetInstantiation(context.OwningType)[index];
    }
}
