// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public class EnCContractTests
{
    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetLatestEnCVersion_ReturnsLatestVersion_DefaultsForUnregisteredToken(MockTarget.Architecture arch)
    {
        const ulong defaultVersion = 1;
        const uint methodToken = 0x06000042;
        const uint missingToken = 0x06000043;
        const ulong latestVersion = 7;

        var (enc, module) = CreateEnCContract(
            arch,
            defaultVersion,
            [
                (methodToken, 0x1000ul, latestVersion),
                (methodToken, 0x2000ul, 6ul),
                (missingToken, 0x3000ul, 9ul),
            ]);

        Assert.Equal(latestVersion, enc.GetLatestEnCVersion(module, methodToken).Value);
        Assert.Equal(defaultVersion, enc.GetLatestEnCVersion(module, 0x06000044).Value);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetEnCVersion_ReturnsMatchingVersion_DefaultsForMismatchOrNull(MockTarget.Architecture arch)
    {
        const ulong defaultVersion = 1;
        const uint methodToken = 0x06000042;
        const ulong matchingAddress = 0x2000;
        const ulong expectedVersion = 6;

        var (enc, module) = CreateEnCContract(
            arch,
            defaultVersion,
            [
                (methodToken, 0x1000ul, 7ul),
                (methodToken, matchingAddress, expectedVersion),
                (0x06000043u, 0x3000ul, 9ul),
            ]);

        Assert.Equal(expectedVersion, enc.GetEnCVersion(module, methodToken, new TargetCodePointer(matchingAddress)).Value);
        Assert.Equal(defaultVersion, enc.GetEnCVersion(module, methodToken, new TargetCodePointer(0x4000)).Value);
        Assert.Equal(defaultVersion, enc.GetEnCVersion(module, methodToken, TargetCodePointer.Null).Value);
    }

    private static (IEnC Contract, TargetPointer ModuleAddress) CreateEnCContract(
        MockTarget.Architecture arch,
        ulong defaultVersion,
        IReadOnlyList<(uint Token, ulong AddrOfCode, ulong EnCVersion)> entries)
    {
        var targetBuilder = new TestPlaceholderTarget.Builder(arch);
        MockMemorySpace.BumpAllocator allocator = targetBuilder.MemoryBuilder.CreateAllocator(0x0010_0000, 0x0011_0000);

        Layout<MockLoaderModuleWithEnCDataList> moduleLayout = MockLoaderModuleWithEnCDataList.CreateLayout(arch);
        Layout<MockEnCDataNode> encDataLayout = MockEnCDataNode.CreateLayout(arch);

        MockLoaderModuleWithEnCDataList moduleInstance = moduleLayout.Create(allocator.Allocate((ulong)moduleLayout.Size, "Module"));

        ulong headAddress = TargetPointer.Null.Value;
        for (int i = entries.Count - 1; i >= 0; i--)
        {
            var entry = entries[i];
            MockEnCDataNode node = encDataLayout.Create(allocator.Allocate((ulong)encDataLayout.Size, "EnCData"));
            node.AddrOfCode = entry.AddrOfCode;
            node.Token = entry.Token;
            node.EnCVersion = entry.EnCVersion;
            node.Next = headAddress;
            headAddress = node.Address;
        }

        moduleInstance.EnCDataList = headAddress;

        targetBuilder
            .AddGlobals((Constants.Globals.CorDBDefaultEnCFunctionVersion, defaultVersion))
            .AddTypes(new Dictionary<DataType, Target.TypeInfo>
            {
                [DataType.Module] = TargetTestHelpers.CreateTypeInfo(moduleLayout),
                [DataType.EnCData] = TargetTestHelpers.CreateTypeInfo(encDataLayout),
            })
            .AddContract<IEnC>("c1");

        TestPlaceholderTarget target = targetBuilder.Build();
        return (target.Contracts.EnC, new TargetPointer(moduleInstance.Address));
    }

    private sealed class MockLoaderModuleWithEnCDataList : TypedView
    {
        public static Layout<MockLoaderModuleWithEnCDataList> CreateLayout(MockTarget.Architecture architecture)
            => new SequentialLayoutBuilder("Module", architecture)
                .AddPointerField("Assembly")
                .AddPointerField("PEAssembly")
                .AddPointerField("Base")
                .AddUInt32Field("Flags")
                .AddPointerField("LoaderAllocator")
                .AddPointerField("DynamicMetadata")
                .AddPointerField("SimpleName")
                .AddPointerField("Path")
                .AddPointerField("FileName")
                .AddPointerField("ReadyToRunInfo")
                .AddPointerField("GrowableSymbolStream")
                .AddPointerField("AvailableTypeParams")
                .AddPointerField("InstMethodHashTable")
                .AddPointerField("FieldDefToDescMap")
                .AddPointerField("ManifestModuleReferencesMap")
                .AddPointerField("MemberRefToDescMap")
                .AddPointerField("MethodDefToDescMap")
                .AddPointerField("TypeDefToMethodTableMap")
                .AddPointerField("TypeRefToMethodTableMap")
                .AddPointerField("MethodDefToILCodeVersioningStateMap")
                .AddPointerField("DynamicILBlobTable")
                .AddPointerField("EnCDataList")
                .Build<MockLoaderModuleWithEnCDataList>();

        public ulong EnCDataList
        {
            set => WritePointerField("EnCDataList", value);
        }
    }

    private sealed class MockEnCDataNode : TypedView
    {
        public static Layout<MockEnCDataNode> CreateLayout(MockTarget.Architecture architecture)
            => new SequentialLayoutBuilder("EnCData", architecture)
                .AddPointerField("AddrOfCode")
                .AddUInt32Field("Token")
                .AddPointerField("EnCVersion")
                .AddPointerField("Next")
                .Build<MockEnCDataNode>();

        public ulong AddrOfCode
        {
            set => WritePointerField("AddrOfCode", value);
        }

        public uint Token
        {
            set => WriteUInt32Field("Token", value);
        }

        public ulong EnCVersion
        {
            set => WritePointerField("EnCVersion", value);
        }

        public ulong Next
        {
            set => WritePointerField("Next", value);
        }
    }
}
