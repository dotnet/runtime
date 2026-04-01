// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

[GeneratedComClass]
public sealed unsafe partial class ClrDataFrame : IXCLRDataFrame, IXCLRDataFrame2
{
    private readonly Target _target;
    private readonly IXCLRDataFrame? _legacyImpl;
    private readonly IXCLRDataFrame2? _legacyImpl2;

    private readonly IStackDataFrameHandle _dataFrame;

    public ClrDataFrame(Target target, IStackDataFrameHandle dataFrame, IXCLRDataFrame? legacyImpl)
    {
        _target = target;
        _legacyImpl = legacyImpl;
        _legacyImpl2 = legacyImpl as IXCLRDataFrame2;

        _dataFrame = dataFrame;
    }

    // IXCLRDataFrame implementation
    int IXCLRDataFrame.GetFrameType(uint* simpleType, uint* detailedType)
        => _legacyImpl is not null ? _legacyImpl.GetFrameType(simpleType, detailedType) : HResults.E_NOTIMPL;

    int IXCLRDataFrame.GetContext(
        uint contextFlags,
        uint contextBufSize,
        uint* contextSize,
        [Out, MarshalUsing(CountElementName = nameof(contextBufSize))] byte[] contextBuf)
    {
        int hr = HResults.S_OK;
        try
        {
            IStackWalk stackWalk = _target.Contracts.StackWalk;
            byte[] context = stackWalk.GetRawContext(_dataFrame);

            // TODO(https://github.com/dotnet/runtime/issues/125791):
            // Use contextFlags to compute the required size via ContextSizeForFlags
            // (see native ClrDataFrame::GetContext in stack.cpp). Currently we always
            // return the full platform context regardless of the requested flags.
            if (contextSize is not null)
                *contextSize = (uint)context.Length;

            // Match native DAC behavior: fail when the buffer is too small,
            // and on success copy the full context.
            if (contextBufSize < (uint)context.Length)
                throw new ArgumentException();

            if (contextBufSize > 0 && context.Length > 0)
                Array.Copy(context, 0, contextBuf, 0, context.Length);
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            byte[] localContextBuf = new byte[contextBufSize];
            int hrLocal = _legacyImpl.GetContext(contextFlags, contextBufSize, null, localContextBuf);
            Debug.ValidateHResult(hr, hrLocal);

            if (hr == HResults.S_OK)
            {
                IPlatformAgnosticContext contextStruct = IPlatformAgnosticContext.GetContextForPlatform(_target);
                IPlatformAgnosticContext localContextStruct = IPlatformAgnosticContext.GetContextForPlatform(_target);
                contextStruct.FillFromBuffer(contextBuf);
                localContextStruct.FillFromBuffer(localContextBuf);

                Debug.Assert(contextStruct.Equals(localContextStruct));
            }
        }
#endif

        return hr;
    }

    int IXCLRDataFrame.GetAppDomain(DacComNullableByRef<IXCLRDataAppDomain> appDomain)
    {
        int hr = HResults.S_OK;

        int hrLegacy = HResults.S_OK;
        IXCLRDataAppDomain? legacyAppDomain = null;
        if (_legacyImpl is not null)
        {
            DacComNullableByRef<IXCLRDataAppDomain> legacyAppDomainOut = new(isNullRef: false);
            hrLegacy = _legacyImpl.GetAppDomain(legacyAppDomainOut);
            if (hrLegacy >= 0)
            {
                legacyAppDomain = legacyAppDomainOut.Interface;
            }
        }

        try
        {
            TargetPointer appDomainPointer = _target.ReadGlobalPointer(Constants.Globals.AppDomain);
            TargetPointer appDomainAddr = _target.ReadPointer(appDomainPointer);

            if (appDomainAddr != TargetPointer.Null)
            {
                appDomain.Interface = new ClrDataAppDomain(_target, appDomainAddr, legacyAppDomain);
            }
            else
            {
                hr = HResults.S_FALSE;
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            Debug.ValidateHResult(hr, hrLegacy);
        }
#endif

        return hr;
    }

    int IXCLRDataFrame.GetNumArguments(uint* numArgs)
    {
        int hr = HResults.S_OK;
        try
        {
            *numArgs = 0;
            GetMethodInfo(out _, out MetadataReader mdReader, out MethodDefinition methodDef, out _, out _);
            GetMethodSignatureInfo(mdReader, methodDef, out _, out uint numArgsResult);
            *numArgs = numArgsResult;
            if (*numArgs == 0)
                hr = HResults.S_FALSE;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacyImpl is not null)
        {
            uint numArgsLocal;
            int hrLocal = _legacyImpl.GetNumArguments(&numArgsLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*numArgs == numArgsLocal, $"cDAC: {*numArgs}, DAC: {numArgsLocal}");
        }
#endif
        return hr;
    }

    int IXCLRDataFrame.GetArgumentByIndex(
        uint index,
        DacComNullableByRef<IXCLRDataValue> arg,
        uint bufLen,
        uint* nameLen,
        char* name)
    {
        int hr = HResults.S_OK;

        int hrLegacy = HResults.S_OK;
        IXCLRDataValue? legacyValue = null;
        if (_legacyImpl is not null)
        {
            DacComNullableByRef<IXCLRDataValue> legacyArgOut = new(isNullRef: false);
            hrLegacy = _legacyImpl.GetArgumentByIndex(index, legacyArgOut, bufLen, null, null);
            if (hrLegacy >= 0)
            {
                legacyValue = legacyArgOut.Interface;
            }
        }

        try
        {
            if (nameLen is not null)
                *nameLen = 0;

            GetMethodInfo(out MethodDescHandle mdh, out MetadataReader mdReader, out MethodDefinition methodDef, out Contracts.ModuleHandle moduleHandle, out _);
            GetMethodSignatureInfo(mdReader, methodDef, out SignatureHeader header, out uint numArgs);

            if (index >= numArgs)
                throw Marshal.GetExceptionForHR(HResults.E_INVALIDARG)!;

            // Resolve parameter name
            if ((bufLen > 0 && name is not null) || nameLen is not null)
            {
                IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
                if (index == 0 && header.IsInstance)
                {
                    OutputBufferHelpers.CopyStringToBuffer(name, bufLen, nameLen, "this");
                }
                else if (!rts.IsNoMetadataMethod(mdh, out _))
                {
                    // Param indexing is 1-based in metadata. 'this' isn't in the
                    // signature, so for instance methods adjust the index down.
                    int mdIndex = (int)(header.IsInstance ? index : index + 1);
                    string? paramName = GetParameterName(mdReader, methodDef, mdIndex);
                    OutputBufferHelpers.CopyStringToBuffer(name, bufLen, nameLen, paramName ?? string.Empty);
                }
                else
                {
                    OutputBufferHelpers.CopyStringToBuffer(name, bufLen, nameLen, string.Empty);
                }
            }

            if (!arg.IsNullRef)
            {
                arg.Interface = CreateValueFromDebugInfo(
                    header, isArg: true, sigIndex: index, varInfoSlot: index,
                    legacyValue, mdh, moduleHandle);
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacyImpl is not null)
        {
            // See AllowCdacSuccess in DebugExtensions.cs — the native DAC's MetaSig
            // constructor can fail on certain frames (e.g., EH dispatch) where the cDAC
            // succeeds via contract-based metadata access.
            Debug.ValidateHResult(hr, hrLegacy, HResultValidationMode.AllowCdacSuccess);
        }
#endif
        return hr;
    }

    int IXCLRDataFrame.GetNumLocalVariables(uint* numLocals)
    {
        int hr = HResults.S_OK;
        try
        {
            *numLocals = 0;
            GetMethodInfo(out MethodDescHandle mdh, out _, out _, out Contracts.ModuleHandle moduleHandle, out _);
            *numLocals = GetLocalVariableCount(mdh, moduleHandle);
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacyImpl is not null)
        {
            uint numLocalsLocal;
            int hrLocal = _legacyImpl.GetNumLocalVariables(&numLocalsLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*numLocals == numLocalsLocal, $"cDAC: {*numLocals}, DAC: {numLocalsLocal}");
        }
#endif
        return hr;
    }

    int IXCLRDataFrame.GetLocalVariableByIndex(
        uint index,
        DacComNullableByRef<IXCLRDataValue> localVariable,
        uint bufLen,
        uint* nameLen,
        char* name)
    {
        int hr = HResults.S_OK;

        int hrLegacy = HResults.S_OK;
        IXCLRDataValue? legacyValue = null;
        if (_legacyImpl is not null)
        {
            DacComNullableByRef<IXCLRDataValue> legacyLocalOut = new(isNullRef: false);
            hrLegacy = _legacyImpl.GetLocalVariableByIndex(index, legacyLocalOut, bufLen, null, null);
            if (hrLegacy >= 0)
            {
                legacyValue = legacyLocalOut.Interface;
            }
        }

        try
        {
            if (nameLen is not null)
                *nameLen = 0;

            GetMethodInfo(out MethodDescHandle mdh, out MetadataReader mdReader, out MethodDefinition methodDef, out Contracts.ModuleHandle moduleHandle, out _);
            GetMethodSignatureInfo(mdReader, methodDef, out SignatureHeader argHeader, out uint numArgs);

            uint numLocals = GetLocalVariableCount(mdh, moduleHandle);

            if (index >= numLocals)
                throw Marshal.GetExceptionForHR(HResults.E_INVALIDARG)!;

            // Local variable names are not available
            OutputBufferHelpers.CopyStringToBuffer(name, bufLen, nameLen, string.Empty);

            if (!localVariable.IsNullRef)
            {
                // The locals are indexed immediately following the arguments in the NativeVarInfos.
                // varInfoSlot = index + numArgs
                localVariable.Interface = CreateValueFromDebugInfo(
                    argHeader, isArg: false, sigIndex: index, varInfoSlot: index + numArgs,
                    legacyValue, mdh, moduleHandle);
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacyImpl is not null)
        {
            // See comment in GetArgumentByIndex.
            Debug.ValidateHResult(hr, hrLegacy, HResultValidationMode.AllowCdacSuccess);
        }
#endif
        return hr;
    }

    int IXCLRDataFrame.GetCodeName(
        uint flags,
        uint bufLen,
        uint* nameLen,
        char* nameBuf)
        => _legacyImpl is not null ? _legacyImpl.GetCodeName(flags, bufLen, nameLen, nameBuf) : HResults.E_NOTIMPL;

    int IXCLRDataFrame.GetMethodInstance(DacComNullableByRef<IXCLRDataMethodInstance> method)
    {
        int hr = HResults.S_OK;

        int hrLocal = HResults.S_OK;
        IXCLRDataMethodInstance? legacyMethod = null;
        if (_legacyImpl is not null)
        {
            DacComNullableByRef<IXCLRDataMethodInstance> legacyMethodOut = new(isNullRef: false);
            hrLocal = _legacyImpl.GetMethodInstance(legacyMethodOut);
            legacyMethod = legacyMethodOut.Interface;
        }

        try
        {
            IStackWalk stackWalk = _target.Contracts.StackWalk;
            IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;

            TargetPointer methodDesc = stackWalk.GetMethodDescPtr(_dataFrame);

            if (methodDesc == TargetPointer.Null)
                throw new InvalidCastException(); // E_NOINTERFACE

            MethodDescHandle mdh = rts.GetMethodDescHandle(methodDesc);
            TargetPointer appDomain = _target.ReadPointer(
                _target.ReadGlobalPointer(Constants.Globals.AppDomain));

            method.Interface = new ClrDataMethodInstance(_target, mdh, appDomain, legacyMethod);
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif

        return hr;
    }

    int IXCLRDataFrame.Request(
        uint reqCode,
        uint inBufferSize,
        byte* inBuffer,
        uint outBufferSize,
        byte* outBuffer)
        => _legacyImpl is not null ? _legacyImpl.Request(reqCode, inBufferSize, inBuffer, outBufferSize, outBuffer) : HResults.E_NOTIMPL;

    int IXCLRDataFrame.GetNumTypeArguments(uint* numTypeArgs)
        => _legacyImpl is not null ? _legacyImpl.GetNumTypeArguments(numTypeArgs) : HResults.E_NOTIMPL;

    int IXCLRDataFrame.GetTypeArgumentByIndex(uint index, DacComNullableByRef<IXCLRDataTypeInstance> typeArg)
        => _legacyImpl is not null ? _legacyImpl.GetTypeArgumentByIndex(index, typeArg) : HResults.E_NOTIMPL;

    // IXCLRDataFrame2 implementation
    int IXCLRDataFrame2.GetExactGenericArgsToken(DacComNullableByRef<IXCLRDataValue> genericToken)
        => _legacyImpl2 is not null ? _legacyImpl2.GetExactGenericArgsToken(genericToken) : HResults.E_NOTIMPL;

    // ========== Metadata resolution helpers ==========

    /// <summary>
    /// Resolves the frame's MethodDesc into its module-level metadata objects.
    /// Throws on failure (no MethodDesc, no metadata, etc.).
    /// </summary>
    private void GetMethodInfo(out MethodDescHandle mdh, out MetadataReader mdReader, out MethodDefinition methodDef, out Contracts.ModuleHandle moduleHandle, out uint token)
    {
        IStackWalk stackWalk = _target.Contracts.StackWalk;
        TargetPointer methodDescPtr = stackWalk.GetMethodDescPtr(_dataFrame);
        if (methodDescPtr == TargetPointer.Null)
            throw new InvalidCastException(); // E_NOINTERFACE

        IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
        mdh = rts.GetMethodDescHandle(methodDescPtr);
        TargetPointer mtAddr = rts.GetMethodTable(mdh);
        TypeHandle typeHandle = rts.GetTypeHandle(mtAddr);
        TargetPointer modulePtr = rts.GetModule(typeHandle);
        ILoader loader = _target.Contracts.Loader;
        moduleHandle = loader.GetModuleHandleFromModulePtr(modulePtr);
        token = rts.GetMethodToken(mdh);

        IEcmaMetadata ecmaMetadataContract = _target.Contracts.EcmaMetadata;
        MetadataReader? reader = ecmaMetadataContract.GetMetadata(moduleHandle);
        if (reader is null)
            throw new NotImplementedException();
        mdReader = reader;

        MethodDefinitionHandle methodDefHandle = MetadataTokens.MethodDefinitionHandle((int)token);
        methodDef = mdReader.GetMethodDefinition(methodDefHandle);
    }

    /// <summary>
    /// Parses the method signature to determine argument count and signature header.
    /// </summary>
    private static void GetMethodSignatureInfo(MetadataReader mdReader, MethodDefinition methodDef, out SignatureHeader header, out uint numArgs)
    {
        BlobReader blobReader = mdReader.GetBlobReader(methodDef.Signature);
        header = blobReader.ReadSignatureHeader();
        if (header.Kind != SignatureKind.Method)
            throw new BadImageFormatException();
        if (header.IsGeneric)
            blobReader.ReadCompressedInteger(); // skip generic arity
        uint paramCount = (uint)blobReader.ReadCompressedInteger();
        numArgs = paramCount + (header.IsInstance ? 1u : 0u);
    }

    /// <summary>
    /// Creates a ClrDataValue by resolving variable locations for the current frame
    /// via the DebugInfo contract. Mirrors the native ValueFromDebugInfo from stack.cpp.
    /// </summary>
    private ClrDataValue CreateValueFromDebugInfo(
        SignatureHeader methodHeader,
        bool isArg,
        uint sigIndex,
        uint varInfoSlot,
        IXCLRDataValue? legacyImpl,
        MethodDescHandle mdh,
        Contracts.ModuleHandle moduleHandle)
    {
        IStackWalk stackWalk = _target.Contracts.StackWalk;
        IDebugInfo debugInfo = _target.Contracts.DebugInfo;

        TargetPointer ip = stackWalk.GetInstructionPointer(_dataFrame);
        TargetCodePointer codePointer = new TargetCodePointer(ip.Value);
        byte[] context = stackWalk.GetRawContext(_dataFrame);
        IEnumerable<DebugVarInfo> varInfos = debugInfo.GetMethodVarInfo(codePointer, out uint codeOffset);
        NativeVarLocation[] locations = FindAndResolveVarLocation(varInfos, codeOffset, varInfoSlot, context, _target);

        // Determine value flags and adjust size for primitives.
        // Read the raw method/local signature to determine type flags without requiring
        // types to be loaded by the runtime (see https://github.com/dotnet/runtime/issues/125792).
        // Only VAR/MVAR (generic parameters) require runtime type system resolution.
        uint valueFlags;
        int typeSize = -1;
        if (isArg && sigIndex == 0 && methodHeader.IsInstance)
        {
            // 'this' parameter is always a reference
            valueFlags = (uint)ClrDataValueFlag.IS_REFERENCE;
        }
        else
        {
            (valueFlags, typeSize) = ComputeFlagsFromSignature(isArg, sigIndex, methodHeader, mdh, moduleHandle);
        }

        // Match native DAC (ValueFromDebugInfo in stack.cpp): for primitives with a
        // single location, shrink the location size to the actual type size. This is
        // necessary because NativeVarLocations always sets size = sizeof(SIZE_T)
        // (pointer-sized) for every location, regardless of the actual variable type.
        //
        // For value types (structs), no size adjustment is made — GetSize will always
        // return the JIT storage size (pointer-sized per location), not the logical
        // struct size. This matches the native DAC's behavior.
        if ((valueFlags & (uint)ClrDataValueFlag.IS_PRIMITIVE) != 0
            && typeSize > 0
            && locations.Length == 1
            && (ulong)typeSize < locations[0].Size)
        {
            locations =
            [
                new NativeVarLocation
                {
                    AddressOrValue = locations[0].AddressOrValue,
                    Size = (ulong)typeSize,
                    IsRegisterValue = locations[0].IsRegisterValue,
                },
            ];
        }

        return new ClrDataValue(_target, valueFlags, locations, legacyImpl);
    }

    // ========== Signature-based flag computation ==========

    /// <summary>
    /// Returns a BlobReader for the local variable signature of the given method,
    /// or null if the method has no locals (tiny header or no local sig token).
    /// </summary>
    private BlobReader? GetLocalSignatureReader(MethodDescHandle mdh, Contracts.ModuleHandle moduleHandle, out MetadataReader mdReader)
    {
        IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
        uint token = rts.GetMethodToken(mdh);
        ILoader loader = _target.Contracts.Loader;
        TargetPointer ilHeader = loader.GetILHeader(moduleHandle, token);
        if (ilHeader == TargetPointer.Null)
        {
            mdReader = null!;
            return null;
        }

        const int FatFormatFlag = 0x0003;
        const int FormatMask = 0x0007;
        ushort sizeAndFlags = _target.Read<ushort>(ilHeader);
        if ((sizeAndFlags & FormatMask) != FatFormatFlag)
        {
            mdReader = null!;
            return null;
        }

        int localToken = _target.Read<int>(ilHeader + 8);
        if (localToken == 0)
        {
            mdReader = null!;
            return null;
        }

        mdReader = _target.Contracts.EcmaMetadata.GetMetadata(moduleHandle)!;
        StandaloneSignatureHandle localSigHandle = MetadataTokens.StandaloneSignatureHandle(localToken);
        BlobHandle localSigBlob = mdReader.GetStandaloneSignature(localSigHandle).Signature;
        return mdReader.GetBlobReader(localSigBlob);
    }

    /// <summary>
    /// Returns the count of local variables by reading the local signature header.
    /// Throws E_FAIL if the method has no local signature (matches native DAC behavior
    /// for dynamic methods, IL stubs, and methods with no declared locals).
    /// </summary>
    private uint GetLocalVariableCount(MethodDescHandle mdh, Contracts.ModuleHandle moduleHandle)
    {
        BlobReader? reader = GetLocalSignatureReader(mdh, moduleHandle, out _);
        if (reader is null)
            throw Marshal.GetExceptionForHR(HResults.E_FAIL)!;

        BlobReader r = reader.Value;
        r.ReadSignatureHeader();
        return (uint)r.ReadCompressedInteger();
    }

    /// <summary>
    /// Computes value flags by decoding the method or local signature using a custom
    /// ISignatureTypeProvider that maps types directly to (Flags, Size) tuples.
    /// Avoids requiring types to be loaded by the runtime
    /// (see https://github.com/dotnet/runtime/issues/125792).
    /// Only VAR/MVAR (generic parameters) require runtime type system resolution.
    /// </summary>
    private (uint Flags, int Size) ComputeFlagsFromSignature(
        bool isArg, uint sigIndex, SignatureHeader methodHeader,
        MethodDescHandle mdh, Contracts.ModuleHandle moduleHandle)
    {
        try
        {
            GetMethodInfo(out _, out MetadataReader mdReader, out MethodDefinition methodDef, out _, out _);
            FlagSignatureTypeProvider provider = new(_target, moduleHandle);
            SignatureDecoder<(uint Flags, int Size), MethodDescHandle> decoder = new(provider, mdReader, mdh);

            if (isArg)
            {
                BlobReader sigReader = mdReader.GetBlobReader(methodDef.Signature);
                MethodSignature<(uint Flags, int Size)> methodSig = decoder.DecodeMethodSignature(ref sigReader);
                int paramIndex = methodHeader.IsInstance ? (int)sigIndex - 1 : (int)sigIndex;
                return methodSig.ParameterTypes[paramIndex];
            }
            else
            {
                BlobReader? localReader = GetLocalSignatureReader(mdh, moduleHandle, out _);
                if (localReader is null)
                    return ((uint)ClrDataValueFlag.DEFAULT, -1);

                BlobReader sigReader = localReader.Value;
                ImmutableArray<(uint Flags, int Size)> localFlags = decoder.DecodeLocalSignature(ref sigReader);
                return localFlags[(int)sigIndex];
            }
        }
        catch (System.Exception)
        {
            return ((uint)ClrDataValueFlag.DEFAULT, -1);
        }
    }

    /// <summary>
    /// Maps a CorElementType to ClrDataValueFlag and primitive type size.
    /// Used for generic parameter resolution (VAR/MVAR) where we get the
    /// concrete type's CorElementType from the runtime type system.
    /// </summary>
    private static (uint Flags, int Size) MapCorElementTypeToFlags(CorElementType elementType)
    {
        return elementType switch
        {
            CorElementType.Boolean => ((uint)ClrDataValueFlag.IS_PRIMITIVE, 1),
            CorElementType.I1 or CorElementType.U1 => ((uint)ClrDataValueFlag.IS_PRIMITIVE, 1),
            CorElementType.Char or CorElementType.I2 or CorElementType.U2 => ((uint)ClrDataValueFlag.IS_PRIMITIVE, 2),
            CorElementType.I4 or CorElementType.U4 or CorElementType.R4 => ((uint)ClrDataValueFlag.IS_PRIMITIVE, 4),
            CorElementType.I8 or CorElementType.U8 or CorElementType.R8 => ((uint)ClrDataValueFlag.IS_PRIMITIVE, 8),
            CorElementType.I or CorElementType.U => ((uint)ClrDataValueFlag.IS_PRIMITIVE, -1),
            CorElementType.String or CorElementType.Object or CorElementType.Class
                or CorElementType.SzArray or CorElementType.Array => ((uint)ClrDataValueFlag.IS_REFERENCE, -1),
            CorElementType.Ptr => ((uint)ClrDataValueFlag.IS_POINTER, -1),
            CorElementType.ValueType => ((uint)ClrDataValueFlag.IS_VALUE_TYPE, -1),
            _ => ((uint)ClrDataValueFlag.DEFAULT, -1),
        };
    }

    /// <summary>
    /// Checks if a type's base type refers to System.Enum.
    /// </summary>
    private static bool IsEnumBaseType(MetadataReader mdReader, EntityHandle baseType)
    {
        if (baseType.IsNil)
            return false;

        string? baseTypeName = null;
        string? baseTypeNamespace = null;

        if (baseType.Kind == HandleKind.TypeReference)
        {
            TypeReference baseRef = mdReader.GetTypeReference((TypeReferenceHandle)baseType);
            baseTypeName = mdReader.GetString(baseRef.Name);
            baseTypeNamespace = mdReader.GetString(baseRef.Namespace);
        }
        else if (baseType.Kind == HandleKind.TypeDefinition)
        {
            TypeDefinition baseDef = mdReader.GetTypeDefinition((TypeDefinitionHandle)baseType);
            baseTypeName = mdReader.GetString(baseDef.Name);
            baseTypeNamespace = mdReader.GetString(baseDef.Namespace);
        }

        return baseTypeNamespace == "System" && baseTypeName == "Enum";
    }

    /// <summary>
    /// ISignatureTypeProvider that maps signature types directly to ClrDataValue flags
    /// and primitive type sizes. Avoids constructing TypeHandles (and the runtime type
    /// loading that implies) for everything except generic parameters (VAR/MVAR).
    /// </summary>
    private sealed class FlagSignatureTypeProvider : ISignatureTypeProvider<(uint Flags, int Size), MethodDescHandle>
    {
        // ECMA-335 II.23.2.8 rawTypeKind values passed by SignatureDecoder
        private const byte RawTypeKind_ValueType = 0x11; // ELEMENT_TYPE_VALUETYPE
        private const byte RawTypeKind_Class = 0x12;     // ELEMENT_TYPE_CLASS

        private readonly Target _target;
        private readonly Contracts.ModuleHandle _moduleHandle;

        public FlagSignatureTypeProvider(Target target, Contracts.ModuleHandle moduleHandle)
        {
            _target = target;
            _moduleHandle = moduleHandle;
        }

        // --- ISimpleTypeProvider ---

        public (uint Flags, int Size) GetPrimitiveType(PrimitiveTypeCode typeCode) => typeCode switch
        {
            PrimitiveTypeCode.Boolean => ((uint)ClrDataValueFlag.IS_PRIMITIVE, 1),
            PrimitiveTypeCode.SByte or PrimitiveTypeCode.Byte => ((uint)ClrDataValueFlag.IS_PRIMITIVE, 1),
            PrimitiveTypeCode.Char or PrimitiveTypeCode.Int16 or PrimitiveTypeCode.UInt16 => ((uint)ClrDataValueFlag.IS_PRIMITIVE, 2),
            PrimitiveTypeCode.Int32 or PrimitiveTypeCode.UInt32 or PrimitiveTypeCode.Single => ((uint)ClrDataValueFlag.IS_PRIMITIVE, 4),
            PrimitiveTypeCode.Int64 or PrimitiveTypeCode.UInt64 or PrimitiveTypeCode.Double => ((uint)ClrDataValueFlag.IS_PRIMITIVE, 8),
            PrimitiveTypeCode.IntPtr or PrimitiveTypeCode.UIntPtr => ((uint)ClrDataValueFlag.IS_PRIMITIVE, -1),
            // String and Object are PrimitiveTypeCodes but are GC references
            PrimitiveTypeCode.String or PrimitiveTypeCode.Object => ((uint)ClrDataValueFlag.IS_REFERENCE, -1),
            // Void, TypedReference — no meaningful flag
            _ => ((uint)ClrDataValueFlag.DEFAULT, -1),
        };

        public (uint Flags, int Size) GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) =>
            rawTypeKind switch
            {
                RawTypeKind_Class => ((uint)ClrDataValueFlag.IS_REFERENCE, -1),
                RawTypeKind_ValueType => CheckEnumFromTypeDef(reader, handle),
                _ => ((uint)ClrDataValueFlag.DEFAULT, -1),
            };

        public (uint Flags, int Size) GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) =>
            rawTypeKind switch
            {
                RawTypeKind_Class => ((uint)ClrDataValueFlag.IS_REFERENCE, -1),
                RawTypeKind_ValueType => CheckEnumFromTypeRef(reader, handle),
                _ => ((uint)ClrDataValueFlag.DEFAULT, -1),
            };

        // --- IConstructedTypeProvider ---

        public (uint Flags, int Size) GetPointerType((uint Flags, int Size) elementType)
            => ((uint)ClrDataValueFlag.IS_POINTER, -1);

        // ByRef — native DAC's GetTypeFieldValueFlags returns DEFAULT for ELEMENT_TYPE_BYREF
        public (uint Flags, int Size) GetByReferenceType((uint Flags, int Size) elementType)
            => ((uint)ClrDataValueFlag.DEFAULT, -1);

        public (uint Flags, int Size) GetSZArrayType((uint Flags, int Size) elementType)
            => ((uint)ClrDataValueFlag.IS_REFERENCE, -1);

        public (uint Flags, int Size) GetArrayType((uint Flags, int Size) elementType, ArrayShape shape)
            => ((uint)ClrDataValueFlag.IS_REFERENCE, -1);

        // GenericInstantiation — the base type carries the CLASS/VALUETYPE distinction
        public (uint Flags, int Size) GetGenericInstantiation((uint Flags, int Size) genericType, ImmutableArray<(uint Flags, int Size)> typeArguments)
            => genericType;

        // --- ISignatureTypeProvider ---

        // Function pointers are mapped to IntPtr in the native DAC
        public (uint Flags, int Size) GetFunctionPointerType(MethodSignature<(uint Flags, int Size)> signature)
            => ((uint)ClrDataValueFlag.IS_PRIMITIVE, -1);

        public (uint Flags, int Size) GetModifiedType((uint Flags, int Size) modifier, (uint Flags, int Size) unmodifiedType, bool isRequired)
            => unmodifiedType;

        public (uint Flags, int Size) GetPinnedType((uint Flags, int Size) elementType)
            => elementType;

        public (uint Flags, int Size) GetTypeFromSpecification(MetadataReader reader, MethodDescHandle genericContext, TypeSpecificationHandle handle, byte rawTypeKind)
            => ((uint)ClrDataValueFlag.DEFAULT, -1);

        // --- Generic parameter resolution (needs runtime type system) ---

        public (uint Flags, int Size) GetGenericMethodParameter(MethodDescHandle mdh, int index)
        {
            try
            {
                IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
                ReadOnlySpan<TypeHandle> methodInst = rts.GetGenericMethodInstantiation(mdh);
                return ResolveGenericParam(rts, methodInst[index]);
            }
            catch (System.Exception) { return ((uint)ClrDataValueFlag.DEFAULT, -1); }
        }

        public (uint Flags, int Size) GetGenericTypeParameter(MethodDescHandle mdh, int index)
        {
            try
            {
                IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
                TargetPointer mtAddr = rts.GetMethodTable(mdh);
                TypeHandle declaringType = rts.GetTypeHandle(mtAddr);
                ReadOnlySpan<TypeHandle> typeInst = rts.GetInstantiation(declaringType);
                return ResolveGenericParam(rts, typeInst[index]);
            }
            catch (System.Exception) { return ((uint)ClrDataValueFlag.DEFAULT, -1); }
        }

        private static (uint Flags, int Size) ResolveGenericParam(IRuntimeTypeSystem rts, TypeHandle resolvedType)
        {
            CorElementType elementType = rts.GetSignatureCorElementType(resolvedType);
            (uint flags, int size) = MapCorElementTypeToFlags(elementType);

            if (flags == (uint)ClrDataValueFlag.IS_VALUE_TYPE && rts.IsEnum(resolvedType))
                flags = (uint)ClrDataValueFlag.IS_ENUM;

            return (flags, size);
        }

        // --- Enum detection helpers ---

        private static (uint Flags, int Size) CheckEnumFromTypeDef(MetadataReader reader, TypeDefinitionHandle handle)
        {
            TypeDefinition typeDef = reader.GetTypeDefinition(handle);
            if (IsEnumBaseType(reader, typeDef.BaseType))
                return ((uint)ClrDataValueFlag.IS_ENUM, -1);

            return ((uint)ClrDataValueFlag.IS_VALUE_TYPE, -1);
        }

        private (uint Flags, int Size) CheckEnumFromTypeRef(MetadataReader reader, TypeReferenceHandle handle)
        {
            // For TypeRefs, try to resolve in the same module's TypeDef table.
            TypeReference typeRef = reader.GetTypeReference(handle);
            MetadataReader moduleReader = _target.Contracts.EcmaMetadata.GetMetadata(_moduleHandle)!;
            foreach (TypeDefinitionHandle tdh in moduleReader.TypeDefinitions)
            {
                TypeDefinition td = moduleReader.GetTypeDefinition(tdh);
                if (moduleReader.StringComparer.Equals(td.Name, reader.GetString(typeRef.Name))
                    && moduleReader.StringComparer.Equals(td.Namespace, reader.GetString(typeRef.Namespace)))
                {
                    if (IsEnumBaseType(moduleReader, td.BaseType))
                        return ((uint)ClrDataValueFlag.IS_ENUM, -1);
                    break;
                }
            }

            return ((uint)ClrDataValueFlag.IS_VALUE_TYPE, -1);
        }
    }

    private static string? GetParameterName(MetadataReader mdReader, MethodDefinition methodDef, int sequenceNumber)
    {
        foreach (ParameterHandle paramHandle in methodDef.GetParameters())
        {
            Parameter param = mdReader.GetParameter(paramHandle);
            if (param.SequenceNumber == sequenceNumber)
            {
                return mdReader.GetString(param.Name);
            }
        }

        return null;
    }

    #region Variable Location Resolution

    /// <summary>
    /// Finds the matching DebugVarInfo entry for a given variable number and code offset,
    /// then resolves it to physical locations using the provided CPU context bytes.
    /// </summary>
    private static NativeVarLocation[] FindAndResolveVarLocation(
        IEnumerable<DebugVarInfo> varInfos,
        uint codeOffset,
        uint varNumber,
        byte[] context,
        Target target)
    {
        foreach (DebugVarInfo varInfo in varInfos)
        {
            if (varInfo.StartOffset <= codeOffset &&
                varInfo.EndOffset >= codeOffset &&
                varInfo.VarNumber == varNumber)
            {
                IPlatformAgnosticContext platformContext = IPlatformAgnosticContext.GetContextForPlatform(target);
                platformContext.FillFromBuffer(context);

                return ResolveVarLocation(varInfo, platformContext, target);
            }
        }

        return [];
    }

    /// <summary>
    /// Resolves a DebugVarInfo entry to physical NativeVarLocation(s)
    /// using the given CPU context. Mirrors the native NativeVarLocations() from util.cpp.
    /// </summary>
    private static NativeVarLocation[] ResolveVarLocation(
        DebugVarInfo varInfo,
        IPlatformAgnosticContext context,
        Target target)
    {
        int pointerSize = target.PointerSize;

        return (varInfo.Kind, varInfo.IsByRef) switch
        {
            (DebugVarLocKind.Register, false) =>
            [
                new NativeVarLocation { AddressOrValue = ReadRegister(context, target, varInfo.Register), Size = (ulong)pointerSize, IsRegisterValue = true },
            ],

            (DebugVarLocKind.Register, true) => ResolveRegByRef(context, target, varInfo.Register, pointerSize),

            (DebugVarLocKind.Stack, false) => ResolveStack(context, target, varInfo.BaseRegister, varInfo.StackOffset, pointerSize, deref: false),
            (DebugVarLocKind.Stack, true) => ResolveStack(context, target, varInfo.BaseRegister, varInfo.StackOffset, pointerSize, deref: true),

            (DebugVarLocKind.RegisterRegister, _) =>
            [
                new NativeVarLocation { AddressOrValue = ReadRegister(context, target, varInfo.Register), Size = (ulong)pointerSize, IsRegisterValue = true },
                new NativeVarLocation { AddressOrValue = ReadRegister(context, target, varInfo.Register2), Size = (ulong)pointerSize, IsRegisterValue = true },
            ],

            (DebugVarLocKind.RegisterStack, _) =>
            [
                new NativeVarLocation { AddressOrValue = ReadRegister(context, target, varInfo.Register), Size = (ulong)pointerSize, IsRegisterValue = true },
                new NativeVarLocation { AddressOrValue = ComputeStackAddress(context, target, varInfo.BaseRegister2, varInfo.StackOffset2), Size = (ulong)pointerSize, IsRegisterValue = false },
            ],

            (DebugVarLocKind.StackRegister, _) =>
            [
                new NativeVarLocation { AddressOrValue = ComputeStackAddress(context, target, varInfo.BaseRegister, varInfo.StackOffset), Size = (ulong)pointerSize, IsRegisterValue = false },
                new NativeVarLocation { AddressOrValue = ReadRegister(context, target, varInfo.Register), Size = (ulong)pointerSize, IsRegisterValue = true },
            ],

            (DebugVarLocKind.DoubleStack, _) =>
            [
                new NativeVarLocation { AddressOrValue = ComputeStackAddress(context, target, varInfo.BaseRegister, varInfo.StackOffset), Size = 2 * (ulong)pointerSize, IsRegisterValue = false },
            ],

            _ => [],
        };
    }

    private static NativeVarLocation[] ResolveRegByRef(IPlatformAgnosticContext context, Target target, uint register, int pointerSize)
    {
        // The register holds a pointer to the variable in target memory.
        // Mark as IsRegisterValue=true to match the native DAC, which sets contextReg=true for
        // VLT_REG_BYREF and copies the register bytes directly via memcpy in IntGetBytes.
        // This means GetBytes returns the raw pointer value (matching the DAC), and
        // GetLocationByIndex returns CLRDATA_VLOC_REGISTER (matching the DAC).
        ulong regValue = ReadRegister(context, target, register);

        return [new NativeVarLocation { AddressOrValue = regValue, Size = (ulong)pointerSize, IsRegisterValue = true }];
    }

    private static NativeVarLocation[] ResolveStack(IPlatformAgnosticContext context, Target target, uint baseRegister, int offset, int pointerSize, bool deref)
    {
        ulong addr = ComputeStackAddress(context, target, baseRegister, offset);
        if (deref)
            addr = DereferenceOrZero(target, addr);

        return [new NativeVarLocation { AddressOrValue = addr, Size = (ulong)pointerSize, IsRegisterValue = false }];
    }

    private static ulong ComputeStackAddress(IPlatformAgnosticContext context, Target target, uint baseRegister, int offset)
    {
        ulong baseReg = ReadRegister(context, target, baseRegister);

        return (ulong)((long)baseReg + offset);
    }

    /// <summary>
    /// Reads a pointer from target memory, returning 0 on failure.
    /// Matches native DereferenceByRefVar (util.cpp) which returns 0 when DacReadAll fails.
    /// </summary>
    private static ulong DereferenceOrZero(Target target, ulong addr)
    {
        try
        {
            return target.ReadPointer(addr);
        }
        catch (System.Exception)
        {
            return 0;
        }
    }

    private static ulong ReadRegister(IPlatformAgnosticContext context, Target target, uint registerNumber)
    {
        if (context.TryReadRegister((int)registerNumber, out TargetNUInt value))
            return value.Value;

        // REGNUM_AMBIENT_SP is beyond the normal register range on every architecture.
        // It represents the entry-time SP, not necessarily the current SP.
        // Map it to the stack pointer as a best-effort approximation (see util.cpp).
        int spRegisterNumber = GetStackPointerRegisterNumber(target);
        if (spRegisterNumber >= 0 && context.TryReadRegister(spRegisterNumber, out value))
            return value.Value;

        return 0;
    }

    private static int GetStackPointerRegisterNumber(Target target)
    {
        RuntimeInfoArchitecture arch = target.Contracts.RuntimeInfo.GetTargetArchitecture();
        return arch switch
        {
            RuntimeInfoArchitecture.X64 => 4,   // RSP
            RuntimeInfoArchitecture.X86 => 4,   // ESP
            RuntimeInfoArchitecture.Arm64 => 31, // SP
            RuntimeInfoArchitecture.Arm => 13,   // SP
            _ => -1,
        };
    }

    #endregion
}
