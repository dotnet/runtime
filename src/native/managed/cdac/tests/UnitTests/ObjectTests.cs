// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Moq;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public unsafe class ObjectTests
{
    private static IObject CreateObjectContract(MockTarget.Architecture arch, Action<MockDescriptors.MockObjectBuilder> configure)
        => CreateObjectTarget(arch, configure).Contracts.Object;

    private static ISOSDacInterface CreateSOSDacInterface(
        MockTarget.Architecture arch,
        Action<MockDescriptors.MockObjectBuilder> configure,
        Action<TestPlaceholderTarget.Builder>? configureMocks = null)
        => new SOSDacImpl(CreateObjectTarget(arch, configure, configureMocks), legacyObj: null);

    private static TestPlaceholderTarget CreateObjectTarget(
        MockTarget.Architecture arch,
        Action<MockDescriptors.MockObjectBuilder> configure,
        Action<TestPlaceholderTarget.Builder>? configureMocks = null)
    {
        var targetBuilder = new TestPlaceholderTarget.Builder(arch);
        MockDescriptors.RuntimeTypeSystem rtsBuilder = new(targetBuilder.MemoryBuilder);
        MockDescriptors.MockObjectBuilder objectBuilder = new(rtsBuilder);

        configure?.Invoke(objectBuilder);

        targetBuilder
            .AddTypes(CreateContractTypes(objectBuilder))
            .AddGlobals(CreateContractGlobals(objectBuilder));

        if (configureMocks is not null)
        {
            configureMocks(targetBuilder);
        }
        else
        {
            targetBuilder
                .AddContract<IObject>(version: "c1")
                .AddContract<IRuntimeTypeSystem>(version: "c1")
                .AddContract<ISyncBlock>(version: "c1");
        }

        return targetBuilder.Build();
    }

    private static Dictionary<DataType, Target.TypeInfo> CreateContractTypes(MockDescriptors.MockObjectBuilder objectBuilder)
    {
        Dictionary<DataType, Target.TypeInfo> types = MethodTableTests.CreateContractTypes(objectBuilder.RTSBuilder);

        // Object-specific types take precedence; the full ContinuationObject layout (with field
        // offsets) overrides the size-only entry provided by MethodTableTests.CreateContractTypes.
        types[DataType.Object] = TargetTestHelpers.CreateTypeInfo(objectBuilder.ObjectLayout);
        types[DataType.ObjectHeader] = TargetTestHelpers.CreateTypeInfo(objectBuilder.ObjectHeaderLayout);
        types[DataType.String] = TargetTestHelpers.CreateTypeInfo(objectBuilder.StringLayout);
        types[DataType.Array] = TargetTestHelpers.CreateTypeInfo(objectBuilder.ArrayLayout);
        types[DataType.Delegate] = TargetTestHelpers.CreateTypeInfo(objectBuilder.DelegateLayout);
        types[DataType.ContinuationObject] = TargetTestHelpers.CreateTypeInfo(objectBuilder.ContinuationLayout);
        types[DataType.AsyncResumeInfo] = TargetTestHelpers.CreateTypeInfo(objectBuilder.AsyncResumeInfoLayout);
        types[DataType.SyncTableEntry] = TargetTestHelpers.CreateTypeInfo(objectBuilder.SyncTableEntryLayout);
        types[DataType.SyncBlock] = TargetTestHelpers.CreateTypeInfo(objectBuilder.SyncBlockLayout);
        types[DataType.InteropSyncBlockInfo] = TargetTestHelpers.CreateTypeInfo(objectBuilder.InteropSyncBlockInfoLayout);

        return types;
    }

    private static (string Name, ulong Value)[] CreateContractGlobals(MockDescriptors.MockObjectBuilder objectBuilder)
        => MethodTableTests.CreateContractGlobals(objectBuilder.RTSBuilder).Concat(
        [
            (nameof(Constants.Globals.ObjectToMethodTableUnmask), MockDescriptors.MockObjectBuilder.TestObjectToMethodTableUnmask),
            (nameof(Constants.Globals.StringMethodTable), MockDescriptors.MockObjectBuilder.TestStringMethodTableGlobalAddress),
            (nameof(Constants.Globals.ArrayBoundsZero), MockDescriptors.MockObjectBuilder.TestArrayBoundsZeroGlobalAddress),
            (nameof(Constants.Globals.SyncTableEntries), MockDescriptors.MockObjectBuilder.TestSyncTableEntriesGlobalAddress),
            (nameof(Constants.Globals.SyncBlockValueToObjectOffset), MockDescriptors.MockObjectBuilder.TestSyncBlockValueToObjectOffset),
            (nameof(Constants.Globals.SyncBlockIsHashOrSyncBlockIndex), 0x08000000u),
            (nameof(Constants.Globals.SyncBlockIsHashCode), 0x04000000u),
            (nameof(Constants.Globals.SyncBlockIndexMask), (1u << 26) - 1),
            (nameof(Constants.Globals.SyncBlockHashCodeMask), (1u << 26) - 1),
        ]).ToArray();

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void UnmaskMethodTableAddress(MockTarget.Architecture arch)
    {
        TargetPointer TestObjectAddress = default;
        const ulong TestMethodTableAddress = 0x00000000_10000027;
        IObject contract = CreateObjectContract(
            arch,
            objectBuilder =>
            {
                TestObjectAddress = objectBuilder.AddObject(TestMethodTableAddress);
            });

        TargetPointer mt = contract.GetMethodTableAddress(TestObjectAddress);

        Assert.Equal(TestMethodTableAddress & ~MockDescriptors.MockObjectBuilder.TestObjectToMethodTableUnmask, mt.Value);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void StringValue(MockTarget.Architecture arch)
    {
        TargetPointer TestStringAddress = default;
        string expected = "test_string_value";
        IObject contract = CreateObjectContract(
            arch,
            objectBuilder =>
            {
                TestStringAddress = objectBuilder.AddStringObject(expected);
            });

        string actual = contract.GetStringValue(TestStringAddress);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void StringData(MockTarget.Architecture arch)
    {
        TargetPointer TestStringAddress = default;
        int offsetToFirstChar = 0;
        string expected = "test_string_value";
        IObject contract = CreateObjectContract(
            arch,
            objectBuilder =>
            {
                offsetToFirstChar = objectBuilder.StringLayout.GetField("m_FirstChar").Offset;
                TestStringAddress = objectBuilder.AddStringObject(expected);
            });

        contract.GetStringData(TestStringAddress, out uint length, out uint actualOffsetToFirstChar);

        Assert.Equal((uint)expected.Length, length);
        Assert.Equal((uint)offsetToFirstChar, actualOffsetToFirstChar);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void Size(MockTarget.Architecture arch)
    {
        const int TestArrayLength = 10;
        TargetPointer TestObjectAddress = default;
        TargetPointer TestArrayAddress = default;
        Array testArray = new int[TestArrayLength];
        TargetTestHelpers targetTestHelpers = new(arch);
        IObject contract = CreateObjectContract(
            arch,
            objectBuilder =>
            {
                MockEEClass eeClass = objectBuilder.RTSBuilder.AddEEClass("TestClass");
                MockMethodTable methodTable = objectBuilder.RTSBuilder.AddMethodTable("TestClass");
                methodTable.BaseSize = objectBuilder.Builder.TargetTestHelpers.ObjectBaseSize;
                eeClass.MethodTable = methodTable.Address;
                methodTable.EEClassOrCanonMT = eeClass.Address;
                TestObjectAddress = objectBuilder.AddObject(methodTable.Address);

                TestArrayAddress = objectBuilder.AddArrayObject(testArray);
            });

        // Fixed-size object: size is just the base size, no component data.
        Assert.Equal(targetTestHelpers.ObjectBaseSize, contract.GetSize(TestObjectAddress));

        // Variable-size object (array): base size plus component count times component size.
        // AddArrayObject encodes the component size as the array length in the method table flags,
        // so the expected component size here matches TestArrayLength rather than sizeof(int).
        ulong componentSize = TestArrayLength;
        ulong expectedArraySize = targetTestHelpers.ArrayBaseBaseSize + TestArrayLength * componentSize;
        Assert.Equal(expectedArraySize, contract.GetSize(TestArrayAddress));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ArrayData(MockTarget.Architecture arch)
    {
        TargetPointer SingleDimensionArrayAddress = default;
        TargetPointer MultiDimensionArrayAddress = default;
        TargetPointer NonZeroLowerBoundArrayAddress = default;
        int numComponentsOffset = 0;

        Array singleDimension = new int[10];
        Array multiDimension = new int[1, 2, 3, 4];
        Array nonZeroLowerBound = Array.CreateInstance(typeof(int), [10], [5]);
        TargetTestHelpers targetTestHelpers = new(arch);
        IObject contract = CreateObjectContract(
            arch,
            objectBuilder =>
            {
                numComponentsOffset = objectBuilder.ArrayLayout.GetField("m_NumComponents").Offset;
                SingleDimensionArrayAddress = objectBuilder.AddArrayObject(singleDimension);
                MultiDimensionArrayAddress = objectBuilder.AddArrayObject(multiDimension);
                NonZeroLowerBoundArrayAddress = objectBuilder.AddArrayObject(nonZeroLowerBound);
            });

        {
            TargetPointer data = contract.GetArrayData(SingleDimensionArrayAddress, out uint count, out TargetPointer boundsStart, out TargetPointer lowerBounds);
            Assert.Equal(SingleDimensionArrayAddress + targetTestHelpers.ArrayBaseBaseSize - targetTestHelpers.ObjHeaderSize, data.Value);
            Assert.Equal((uint)singleDimension.Length, count);
            Assert.Equal(SingleDimensionArrayAddress + (ulong)numComponentsOffset, boundsStart.Value);
            Assert.Equal(MockDescriptors.MockObjectBuilder.TestArrayBoundsZeroGlobalAddress, lowerBounds.Value);
        }
        {
            TargetPointer data = contract.GetArrayData(MultiDimensionArrayAddress, out uint count, out TargetPointer boundsStart, out TargetPointer lowerBounds);
            Assert.Equal(MultiDimensionArrayAddress + targetTestHelpers.ArrayBaseSize + (ulong)(multiDimension.Rank * sizeof(int) * 2), data.Value);
            Assert.Equal((uint)multiDimension.Length, count);
            Assert.Equal(MultiDimensionArrayAddress + targetTestHelpers.ArrayBaseSize, boundsStart.Value);
            Assert.Equal(boundsStart.Value + (ulong)(multiDimension.Rank * sizeof(int)), lowerBounds.Value);
        }
        {
            TargetPointer data = contract.GetArrayData(NonZeroLowerBoundArrayAddress, out uint count, out TargetPointer boundsStart, out TargetPointer lowerBounds);
            Assert.Equal(NonZeroLowerBoundArrayAddress + targetTestHelpers.ArrayBaseSize + (ulong)(nonZeroLowerBound.Rank * sizeof(int) * 2), data.Value);
            Assert.Equal((uint)nonZeroLowerBound.Length, count);
            Assert.Equal(NonZeroLowerBoundArrayAddress + targetTestHelpers.ArrayBaseSize, boundsStart.Value);
            Assert.Equal(boundsStart.Value + (ulong)(nonZeroLowerBound.Rank * sizeof(int)), lowerBounds.Value);
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ComData(MockTarget.Architecture arch)
    {
        TargetPointer TestComObjectAddress = default;
        TargetPointer TestNonComObjectAddress = default;

        TargetPointer expectedRCW = 0xaaaa;
        TargetPointer expectedCCW = 0xbbbb;
        TargetPointer expectedCCF = 0xcccc;

        IObject contract = CreateObjectContract(
            arch,
            objectBuilder =>
            {
                uint syncBlockIndex = 0;
                TestComObjectAddress = objectBuilder.AddObjectWithSyncBlock(0, syncBlockIndex++, expectedRCW, expectedCCW, expectedCCF);
                TestNonComObjectAddress = objectBuilder.AddObjectWithSyncBlock(0, syncBlockIndex++, TargetPointer.Null, TargetPointer.Null, TargetPointer.Null);
            });

        {
            bool res = contract.GetBuiltInComData(TestComObjectAddress, out TargetPointer rcw, out TargetPointer ccw, out TargetPointer ccf);
            Assert.True(res);
            Assert.Equal(expectedRCW.Value, rcw.Value);
            Assert.Equal(expectedCCW.Value, ccw.Value);
            Assert.Equal(expectedCCF.Value, ccf.Value);
        }
        {
            bool res = contract.GetBuiltInComData(TestNonComObjectAddress, out TargetPointer rcw, out TargetPointer ccw, out TargetPointer ccf);
            Assert.False(res);
            Assert.Equal(TargetPointer.Null.Value, rcw.Value);
            Assert.Equal(TargetPointer.Null.Value, ccw.Value);
            Assert.Equal(TargetPointer.Null.Value, ccf.Value);
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetObjectClassName_ZeroAddress(MockTarget.Architecture arch)
    {
        ISOSDacInterface sosDac = CreateSOSDacInterface(arch, objectBuilder => { });
        char[] buffer = new char[256];
        uint needed;
        int hr;
        fixed (char* ptr = buffer)
        {
            hr = sosDac.GetObjectClassName(default, (uint)buffer.Length, ptr, &needed);
        }
        Assert.NotEqual(HResults.S_OK, hr);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetObjectClassName_UnloadedModule(MockTarget.Architecture arch)
    {
        TargetPointer TestObjectAddress = default;
        TargetPointer TestMethodTableAddress = default;
        ISOSDacInterface sosDac = CreateSOSDacInterface(
            arch,
            objectBuilder =>
            {
                MockEEClass eeClass = objectBuilder.RTSBuilder.AddEEClass("TestClass");
                MockMethodTable methodTable = objectBuilder.RTSBuilder.AddMethodTable("TestClass");
                methodTable.BaseSize = objectBuilder.Builder.TargetTestHelpers.ObjectBaseSize;
                TestMethodTableAddress = methodTable.Address;
                eeClass.MethodTable = methodTable.Address;
                methodTable.EEClassOrCanonMT = eeClass.Address;
                TestObjectAddress = objectBuilder.AddObject(TestMethodTableAddress);
            },
            builder => {
                var mockRts = new Mock<IRuntimeTypeSystem>();
                TypeHandle handle = new TypeHandle(TestMethodTableAddress);
                mockRts.Setup(r => r.GetTypeHandle(TestMethodTableAddress)).Returns(handle);
                mockRts.Setup(r => r.GetModule(handle)).Returns(TargetPointer.Null);

                var mockLoader = new Mock<ILoader>();
                mockLoader.Setup(l => l.GetModuleHandleFromModulePtr(It.IsAny<TargetPointer>())).Returns(default(Contracts.ModuleHandle));
                mockLoader.Setup(l => l.TryGetLoadedImageContents(It.IsAny<Contracts.ModuleHandle>(), out It.Ref<TargetPointer>.IsAny, out It.Ref<uint>.IsAny, out It.Ref<uint>.IsAny)).Returns(false);

                var mockObject = new Mock<IObject>();
                mockObject.Setup(o => o.GetMethodTableAddress(It.IsAny<TargetPointer>())).Returns(TestMethodTableAddress);

                builder
                    .AddMockContract(mockObject)
                    .AddMockContract(mockRts)
                    .AddMockContract(mockLoader);
            });
        char[] buffer = new char[256];
        uint needed;
        int hr;
        fixed (char* ptr = buffer)
        {
            hr = sosDac.GetObjectClassName(new ClrDataAddress(TestObjectAddress.Value), (uint)buffer.Length, ptr, &needed);
        }
        Assert.Equal(HResults.S_OK, hr);
        Assert.Equal((uint)"<Unloaded Type>".Length + 1, needed);
        Assert.Equal("<Unloaded Type>", new string(buffer, 0, (int)needed - 1));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetObjectClassName_NullBufferReturnsNeededSize(MockTarget.Architecture arch)
    {
        TargetPointer TestObjectAddress = default;
        TargetPointer TestMethodTableAddress = default;
        ISOSDacInterface sosDac = CreateSOSDacInterface(
            arch,
            objectBuilder =>
            {
                MockEEClass eeClass = objectBuilder.RTSBuilder.AddEEClass("TestClass");
                MockMethodTable methodTable = objectBuilder.RTSBuilder.AddMethodTable("TestClass");
                methodTable.BaseSize = objectBuilder.Builder.TargetTestHelpers.ObjectBaseSize;
                TestMethodTableAddress = methodTable.Address;
                eeClass.MethodTable = methodTable.Address;
                methodTable.EEClassOrCanonMT = eeClass.Address;
                TestObjectAddress = objectBuilder.AddObject(TestMethodTableAddress);
            },
            builder => {
                var mockRts = new Mock<IRuntimeTypeSystem>();
                TypeHandle handle = new TypeHandle(TestMethodTableAddress);
                mockRts.Setup(r => r.GetTypeHandle(TestMethodTableAddress)).Returns(handle);
                mockRts.Setup(r => r.GetModule(handle)).Returns(TargetPointer.Null);

                var mockLoader = new Mock<ILoader>();
                mockLoader.Setup(l => l.GetModuleHandleFromModulePtr(It.IsAny<TargetPointer>())).Returns(default(Contracts.ModuleHandle));
                mockLoader.Setup(l => l.TryGetLoadedImageContents(It.IsAny<Contracts.ModuleHandle>(), out It.Ref<TargetPointer>.IsAny, out It.Ref<uint>.IsAny, out It.Ref<uint>.IsAny)).Returns(false);

                var mockObject = new Mock<IObject>();
                mockObject.Setup(o => o.GetMethodTableAddress(It.IsAny<TargetPointer>())).Returns(TestMethodTableAddress);

                builder
                    .AddMockContract(mockObject)
                    .AddMockContract(mockRts)
                    .AddMockContract(mockLoader);
            });
        uint needed;
        int hr = sosDac.GetObjectClassName(new ClrDataAddress(TestObjectAddress.Value), 0, null, &needed);
        Assert.Equal(HResults.S_OK, hr);
        Assert.Equal((uint)"<Unloaded Type>".Length + 1, needed);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetDelegateInfo_Closed(MockTarget.Architecture arch)
    {
        const ulong TestMethodTable = 0x00000000_10000200;
        const ulong TestTarget = 0x00000000_10000400;
        const ulong TestMethodPtr = 0x00000000_aaaa0000;
        TargetPointer delegateAddress = default;

        IObject contract = CreateObjectContract(
            arch,
            objectBuilder =>
            {
                delegateAddress = objectBuilder.AddDelegateObject(
                    methodTable: TestMethodTable,
                    target: TestTarget,
                    methodPtr: TestMethodPtr,
                    methodPtrAux: 0,
                    extraData: 0);
            });

        DelegateInfo info = contract.GetDelegateInfo(delegateAddress);

        Assert.Equal(DelegateType.Closed, info.DelegateType);
        Assert.Equal(TestTarget, info.TargetObject.Value);
        Assert.Equal(TestMethodPtr, info.TargetMethodPtr.Value);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetDelegateInfo_Open(MockTarget.Architecture arch)
    {
        const ulong TestMethodTable = 0x00000000_10000200;
        const ulong TestMethodPtr = 0x00000000_aaaa0000;
        const ulong TestMethodPtrAux = 0x00000000_bbbb0000;
        TargetPointer delegateAddress = default;

        IObject contract = CreateObjectContract(
            arch,
            objectBuilder =>
            {
                delegateAddress = objectBuilder.AddDelegateObject(
                    methodTable: TestMethodTable,
                    target: 0,
                    methodPtr: TestMethodPtr,
                    methodPtrAux: TestMethodPtrAux,
                    extraData: 0);
            });

        DelegateInfo info = contract.GetDelegateInfo(delegateAddress);

        Assert.Equal(DelegateType.Open, info.DelegateType);
        Assert.Equal(0ul, info.TargetObject.Value);
        Assert.Equal(TestMethodPtrAux, info.TargetMethodPtr.Value);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetDelegateInfo_Unmanaged(MockTarget.Architecture arch)
    {
        const ulong TestMethodTable = 0x00000000_10000200;
        const ulong TestMethodPtr = 0x00000000_aaaa0000;
        const long TestExtraData = -1;
        TargetPointer delegateAddress = default;

        IObject contract = CreateObjectContract(
            arch,
            objectBuilder =>
            {
                delegateAddress = objectBuilder.AddDelegateObject(
                    methodTable: TestMethodTable,
                    target: 0,
                    methodPtr: TestMethodPtr,
                    methodPtrAux: 0,
                    extraData: TestExtraData);
            });

        DelegateInfo info = contract.GetDelegateInfo(delegateAddress);

        Assert.Equal(DelegateType.Unknown, info.DelegateType);
        Assert.Equal(0ul, info.TargetObject.Value);
        Assert.Equal(0ul, info.TargetMethodPtr.Value);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetContinuationInfo_WithResumeInfo(MockTarget.Architecture arch)
    {
        const ulong TestMethodTable = 0x00000000_10000200;
        const ulong TestNext = 0x00000000_10000600;
        const ulong TestDiagnosticIP = 0x00000000_aaaa1000;
        const int TestState = 0x1234_5678;
        TargetPointer continuationAddress = default;

        IObject contract = CreateObjectContract(
            arch,
            objectBuilder =>
            {
                ulong resumeInfoAddress = objectBuilder.AddAsyncResumeInfo(diagnosticIP: TestDiagnosticIP);
                continuationAddress = objectBuilder.AddContinuationObject(
                    methodTable: TestMethodTable,
                    next: TestNext,
                    resumeInfo: resumeInfoAddress,
                    state: TestState);
            });

        ContinuationInfo info = contract.GetContinuationInfo(continuationAddress);

        Assert.Equal(TestNext, info.Next.Value);
        Assert.Equal(TestDiagnosticIP, info.DiagnosticIP.Value);
        Assert.Equal(unchecked((uint)TestState), info.State);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetContinuationInfo_NullResumeInfo(MockTarget.Architecture arch)
    {
        const ulong TestMethodTable = 0x00000000_10000200;
        const ulong TestNext = 0x00000000_10000600;
        const int TestState = 42;
        TargetPointer continuationAddress = default;

        IObject contract = CreateObjectContract(
            arch,
            objectBuilder =>
            {
                continuationAddress = objectBuilder.AddContinuationObject(
                    methodTable: TestMethodTable,
                    next: TestNext,
                    resumeInfo: 0,
                    state: TestState);
            });

        ContinuationInfo info = contract.GetContinuationInfo(continuationAddress);

        Assert.Equal(TestNext, info.Next.Value);
        Assert.Equal(TargetPointer.Null, info.DiagnosticIP);
        Assert.Equal((uint)TestState, info.State);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetContinuationInfo_NullDiagnosticIP(MockTarget.Architecture arch)
    {
        const ulong TestMethodTable = 0x00000000_10000200;
        const int TestState = 0;
        TargetPointer continuationAddress = default;

        IObject contract = CreateObjectContract(
            arch,
            objectBuilder =>
            {
                ulong resumeInfoAddress = objectBuilder.AddAsyncResumeInfo(diagnosticIP: 0);
                continuationAddress = objectBuilder.AddContinuationObject(
                    methodTable: TestMethodTable,
                    next: 0,
                    resumeInfo: resumeInfoAddress,
                    state: TestState);
            });

        ContinuationInfo info = contract.GetContinuationInfo(continuationAddress);

        Assert.Equal(TargetPointer.Null, info.Next);
        Assert.Equal(TargetPointer.Null, info.DiagnosticIP);
        Assert.Equal((uint)TestState, info.State);
    }
}
