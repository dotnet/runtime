// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
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

    public IEnumerable<ArgumentLocation> EnumerateArguments(MethodDescHandle methodDesc)
    {
        IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
        IRuntimeInfo runtimeInfo = _target.Contracts.RuntimeInfo;

        MethodSignature<TypeHandle> methodSig = DecodeMethodSignature(rts, methodDesc);

        if (methodSig.Header.CallingConvention is SignatureCallingConvention.VarArgs)
        {
            throw new NotImplementedException("VarArgs support not implemented");
        }

        bool hasThis = methodSig.Header.IsInstance;
        bool requiresInstArg = false;
        bool isAsync = false;
        try
        {
            requiresInstArg = rts.GetGenericContextLoc(methodDesc) == GenericContextLoc.InstArg;
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
                CdacCorElementType elemType = rts.GetSignatureCorElementType(
                    methodSig.ParameterTypes[argIndex]);

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

        SignatureTypeProvider<TypeHandle> provider = new(_target, moduleHandle);
        RuntimeSignatureDecoder<TypeHandle, TypeHandle> decoder = new(
            provider, _target, mdReader, typeHandle);

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
}
