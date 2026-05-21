// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.RuntimeTypeSystemHelpers;
using Moq;
using ModuleHandle = Microsoft.Diagnostics.DataContractReader.Contracts.ModuleHandle;
using TypeHandle = Microsoft.Diagnostics.DataContractReader.Contracts.TypeHandle;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

/// <summary>
/// End-to-end test harness for the <see cref="ICallingConvention"/> contract.
/// Builds a mock target containing a single stored-sig EEImpl method whose
/// signature is encoded as a raw blob — so the harness bypasses the
/// metadata reader entirely.
/// </summary>
internal static class CallingConventionTestHelpers
{
    public static (Target Target, MethodDescHandle Handle) CreateTargetWithStaticMethod(
        CallConvTestCase testCase,
        Action<SignatureBlobBuilder> buildSignature,
        SyntheticVectorMetadata? syntheticMetadata = null)
        => CreateTargetWithMethod(testCase, hasThis: false, (_, sig) => buildSignature(sig), syntheticMetadata: syntheticMetadata);

    public static (Target Target, MethodDescHandle Handle) CreateTargetWithMethod(
        CallConvTestCase testCase,
        bool hasThis,
        Action<SignatureBlobBuilder> buildSignature,
        bool hasParamType = false,
        bool hasAsyncContinuation = false,
        SyntheticVectorMetadata? syntheticMetadata = null)
        => CreateTargetWithMethod(testCase, hasThis, (_, sig) => buildSignature(sig), hasParamType, hasAsyncContinuation, syntheticMetadata: syntheticMetadata);

    /// <summary>
    /// Richer overload that gives the test callback access to the
    /// <see cref="MockDescriptors.RuntimeTypeSystem"/> builder so it can
    /// allocate auxiliary mock types (e.g. value-type MTs for
    /// <c>ELEMENT_TYPE_INTERNAL</c> sig references) before building the
    /// method signature.
    /// </summary>
    /// <param name="enclosingMTOverride">
    /// Optional callback to choose a different MethodTable as the enclosing
    /// class of the test method (default is <c>System.Object</c>). The callback
    /// runs after the <c>configure</c> callback so it can refer to MTs the
    /// configure callback allocated.
    /// </param>
    public static (Target Target, MethodDescHandle Handle) CreateTargetWithMethod(
        CallConvTestCase testCase,
        bool hasThis,
        Action<MockDescriptors.RuntimeTypeSystem, SignatureBlobBuilder> configure,
        bool hasParamType = false,
        bool hasAsyncContinuation = false,
        Func<MockDescriptors.RuntimeTypeSystem, MockMethodTable>? enclosingMTOverride = null,
        SyntheticVectorMetadata? syntheticMetadata = null,
        bool isArmSoftFP = false)
    {
        var targetBuilder = new TestPlaceholderTarget.Builder(testCase.MockArch);
        MockDescriptors.RuntimeTypeSystem rtsBuilder = new(targetBuilder.MemoryBuilder);
        MockLoaderBuilder loaderBuilder = new(targetBuilder.MemoryBuilder);
        MockDescriptors.MockMethodDescriptorsBuilder mdsBuilder = new(rtsBuilder, loaderBuilder);

        MockLoaderModule mockModule = loaderBuilder.AddModule(simpleName: "TestModule");
        rtsBuilder.SystemObjectMethodTable.Module = mockModule.Address;
        rtsBuilder.ContinuationMethodTable.Module = mockModule.Address;

        int pointerSize = targetBuilder.MemoryBuilder.TargetTestHelpers.PointerSize;
        SignatureBlobBuilder sigBuilder = new(pointerSize, hasThis);
        configure(rtsBuilder, sigBuilder);
        byte[] sigBytes = sigBuilder.Build();

        MockMethodTable enclosingMT = enclosingMTOverride?.Invoke(rtsBuilder)
            ?? (hasParamType ? CreateSharedGenericMethodTable(rtsBuilder, hasThis) : rtsBuilder.SystemObjectMethodTable);
        enclosingMT.Module = mockModule.Address;
        MockMethodTableAuxiliaryData aux = rtsBuilder.AddMethodTableAuxiliaryData();
        aux.LoaderModule = mockModule.Address;
        enclosingMT.AuxiliaryData = aux.Address;

        MockMemorySpace.HeapFragment sigFragment = rtsBuilder.TypeSystemAllocator.Allocate(
            (ulong)sigBytes.Length, "TestMethodSig");
        Array.Copy(sigBytes, sigFragment.Data, sigBytes.Length);

        uint methodDescTotalSize = (uint)mdsBuilder.StoredSigMethodDescLayout.Size;
        if (hasAsyncContinuation)
        {
            methodDescTotalSize += mdsBuilder.AsyncMethodDataSize;
        }

        byte methodDescSize = (byte)(methodDescTotalSize / mdsBuilder.MethodDescAlignment);
        byte chunkCount = 1;
        byte chunkSize = (byte)(chunkCount * methodDescSize);
        MockMethodDescChunk chunk = mdsBuilder.AddMethodDescChunk("TestMethod", chunkSize);
        chunk.MethodTable = enclosingMT.Address;
        chunk.Size = chunkSize;
        chunk.Count = chunkCount;

        MockStoredSigMethodDesc md = chunk.GetMethodDescAtChunkIndex(0, mdsBuilder.StoredSigMethodDescLayout);
        md.ChunkIndex = 0;
        md.Slot = 0;

        ushort methodFlags = (ushort)MethodClassification.EEImpl;
        if (!hasThis)
        {
            methodFlags |= (ushort)MethodDescFlags_1.MethodDescFlags.Static;
        }

        if (hasAsyncContinuation)
        {
            methodFlags |= (ushort)MethodDescFlags_1.MethodDescFlags.HasAsyncMethodData;
        }

        md.Flags = methodFlags;
        md.Sig = sigFragment.Address;
        md.CSig = (uint)sigBytes.Length;

        if (hasAsyncContinuation)
        {
            int asyncDataOffset = (int)(md.Address - chunk.Address) + mdsBuilder.StoredSigMethodDescLayout.Size;
            targetBuilder.MemoryBuilder.TargetTestHelpers.Write(
                chunk.Memory.Span.Slice(asyncDataOffset, sizeof(uint)),
                (uint)RuntimeTypeSystem_1.AsyncMethodFlags.AsyncCall);
        }

        Dictionary<DataType, Target.TypeInfo> types = new Dictionary<DataType, Target.TypeInfo>()
        {
            [DataType.TransitionBlock] = MockDescriptors.CallingConvention.CreateTransitionBlockTypeInfo(testCase),
            [DataType.FieldDesc] = MockDescriptors.CallingConvention.CreateFieldDescTypeInfo(testCase.MockArch),
            [DataType.MethodDesc] = TargetTestHelpers.CreateTypeInfo(mdsBuilder.MethodDescLayout),
            [DataType.MethodDescChunk] = TargetTestHelpers.CreateTypeInfo(mdsBuilder.MethodDescChunkLayout),
            [DataType.StoredSigMethodDesc] = TargetTestHelpers.CreateTypeInfo(mdsBuilder.StoredSigMethodDescLayout),
            [DataType.EEImplMethodDesc] = new Target.TypeInfo { Size = mdsBuilder.EEImplMethodDescSize },
            [DataType.InstantiatedMethodDesc] = TargetTestHelpers.CreateTypeInfo(mdsBuilder.InstantiatedMethodDescLayout),
            [DataType.DynamicMethodDesc] = TargetTestHelpers.CreateTypeInfo(mdsBuilder.DynamicMethodDescLayout),
            [DataType.NonVtableSlot] = new Target.TypeInfo { Size = mdsBuilder.NonVtableSlotSize },
            [DataType.MethodImpl] = new Target.TypeInfo { Size = mdsBuilder.MethodImplSize },
            [DataType.NativeCodeSlot] = new Target.TypeInfo { Size = mdsBuilder.NativeCodeSlotSize },
            [DataType.AsyncMethodData] = new Target.TypeInfo
            {
                Size = mdsBuilder.AsyncMethodDataSize,
                Fields = new Dictionary<string, Target.FieldInfo>
                {
                    [nameof(Data.AsyncMethodData.Flags)] = new Target.FieldInfo { Offset = 0 },
                },
            },
            [DataType.ArrayMethodDesc] = new Target.TypeInfo { Size = mdsBuilder.ArrayMethodDescSize },
            [DataType.FCallMethodDesc] = new Target.TypeInfo { Size = mdsBuilder.FCallMethodDescSize },
            [DataType.PInvokeMethodDesc] = new Target.TypeInfo { Size = mdsBuilder.PInvokeMethodDescSize },
            [DataType.CLRToCOMCallMethodDesc] = new Target.TypeInfo { Size = mdsBuilder.CLRToCOMCallMethodDescSize },
        }
        .Concat(MethodTableTests.CreateContractTypes(rtsBuilder))
        .Concat(LoaderTests.CreateContractTypes(loaderBuilder))
        .ToDictionary();

        var globals = MethodTableTests.CreateContractGlobals(rtsBuilder).Concat(
        [
            (nameof(Constants.Globals.MethodDescTokenRemainderBitCount),
                (ulong)MockDescriptors.MockMethodDescriptorsBuilder.TokenRemainderBitCount),
            (nameof(Constants.Globals.FieldOffsetBigRVA), 0xFFFFFFFFUL),
        ]);

        // ARM32 and ARM64 enable FEATURE_HFA (needed for HFA detection in IsHFA).
        if (testCase.Architecture is RuntimeInfoArchitecture.Arm or RuntimeInfoArchitecture.Arm64)
            globals = globals.Append((nameof(Constants.Globals.FeatureHFA), 1UL));

        // ARM32 soft-float (armel): the presence of FeatureArmSoftFP tells the
        // iterator to skip the FP register path. Only add it when explicitly requested.
        if (isArmSoftFP)
            globals = globals.Append((nameof(Constants.Globals.FeatureArmSoftFP), 1UL));

        var globalsArray = globals.ToArray();

        Mock<IExecutionManager> execMgr = new();
        Mock<IPrecodeStubs> precode = new();
        Mock<IPlatformMetadata> platMetadata = new();

        Mock<IEcmaMetadata> ecmaMd = new();
        ecmaMd.Setup(e => e.GetMetadata(It.IsAny<ModuleHandle>())).Returns(
            (ModuleHandle moduleHandle) => syntheticMetadata is not null && moduleHandle.Address == mockModule.Address
                ? syntheticMetadata.Reader
                : (MetadataReader?)null);

        Mock<ISignature> sig = new();
        sig.Setup(s => s.DecodeFieldSignature(It.IsAny<BlobHandle>(), It.IsAny<ModuleHandle>(), It.IsAny<TypeHandle>()))
           .Returns(default(TypeHandle));

        var target = targetBuilder
            .AddTypes(types)
            .AddGlobals(globalsArray)
            .AddGlobalStrings(
                (Constants.Globals.Architecture, testCase.Architecture.ToString().ToLowerInvariant()),
                (Constants.Globals.OperatingSystem, testCase.OperatingSystem.ToString().ToLowerInvariant()))
            .AddContract<IRuntimeTypeSystem>(version: "c1")
            .AddContract<ILoader>(version: "c1")
            .AddContract<IRuntimeInfo>(version: "c1")
            .AddContract<ICallingConvention>(version: "c1")
            .AddMockContract(ecmaMd)
            .AddMockContract(sig)
            .AddMockContract(execMgr)
            .AddMockContract(precode)
            .AddMockContract(platMetadata)
            .Build();

        MethodDescHandle mdh = target.Contracts.RuntimeTypeSystem.GetMethodDescHandle(new TargetPointer(md.Address));
        return (target, mdh);
    }

    private static MockMethodTable CreateSharedGenericMethodTable(MockDescriptors.RuntimeTypeSystem rtsBuilder, bool hasThis)
    {
        MockEEClass eeClass = rtsBuilder.AddEEClass(hasThis ? "SharedGenericInterfaceEEClass" : "SharedGenericClassEEClass");
        MockMethodTable methodTable = rtsBuilder.AddMethodTable(hasThis ? "SharedGenericInterface" : "SharedGenericClass");

        uint flags = (uint)MethodTableFlags_1.WFLAGS_LOW.GenericsMask_TypicalInstantiation;
        if (hasThis)
        {
            flags |= (uint)MethodTableFlags_1.WFLAGS_HIGH.Category_Interface;
        }

        methodTable.MTFlags = flags;
        methodTable.BaseSize = rtsBuilder.Builder.TargetTestHelpers.ObjectBaseSize;
        methodTable.ParentMethodTable = rtsBuilder.SystemObjectMethodTable.Address;
        methodTable.NumVirtuals = 1;
        eeClass.MethodTable = methodTable.Address;
        methodTable.EEClassOrCanonMT = eeClass.Address;
        return methodTable;
    }
}
