// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure.ContractDescriptor;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests.ContractDescriptor;

public unsafe partial class TargetTests
{
    private const int PageCachePageSize = 4096;
    private const int PageCachePageCount = 32;
    private const string PageCacheEnabledSetting = "Microsoft.Diagnostics.DataContractReader.PageCache.Enabled";
    private const string PageCachePageSizeSetting = "Microsoft.Diagnostics.DataContractReader.PageCache.PageSize";
    private const string PageCachePageCountSetting = "Microsoft.Diagnostics.DataContractReader.PageCache.PageCount";

    private static readonly Dictionary<DataType, Target.TypeInfo> TestTypes = new()
    {
        // Size and fields
        [DataType.Thread] = new()
        {
            Size = 56,
            Fields = new Dictionary<string, Target.FieldInfo> {
                { "Field1", new(){ Offset = 8, TypeName = DataType.uint16.ToString() }},
                { "Field2", new(){ Offset = 16, TypeName = DataType.ObjectHandle.ToString() }},
                { "Field3", new(){ Offset = 32 }}
            }
        },
        // Fields only
        [DataType.ThreadStore] = new()
        {
            Fields = new Dictionary<string, Target.FieldInfo> {
                { "Field1", new(){ Offset = 0, TypeName = "FieldType" }},
                { "Field2", new(){ Offset = 8 }}
            }
        },
        // Size only
        [DataType.ObjectHandle] = new()
        {
            Size = 8
        }
    };

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetTypeInfo(MockTarget.Architecture arch)
    {
        TargetTestHelpers targetTestHelpers = new(arch);
        ContractDescriptorBuilder builder = new(targetTestHelpers);
        ContractDescriptorBuilder.DescriptorBuilder descriptorBuilder = new(builder);
        descriptorBuilder.SetTypes(TestTypes)
            .SetGlobals(Array.Empty<(string, ulong, string?)>())
            .SetContracts(Array.Empty<string>());

        bool success = builder.TryCreateTarget(descriptorBuilder, out ContractDescriptorTarget? target);
        Assert.True(success);

        ValidateTypes(target, TestTypes);
    }

    private static readonly (string Name, ulong Value, string? Type)[] TestGlobals =
    [
        ("value", (ulong)sbyte.MaxValue, null),
        ("int8Value", 0x12, "int8"),
        ("uint8Value", 0x12, "uint8"),
        ("int16Value", 0x1234, "int16"),
        ("uint16Value", 0x1234, "uint16"),
        ("int32Value", 0x12345678, "int32"),
        ("uint32Value", 0x12345678, "uint32"),
        ("int64Value", 0x123456789abcdef0, "int64"),
        ("uint64Value", 0x123456789abcdef0, "uint64"),
        ("nintValue", 0xabcdef0, "nint"),
        ("nuintValue", 0xabcdef0, "nuint"),
        ("pointerValue", 0xabcdef0, "pointer"),
    ];

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ReadGlobalValue(MockTarget.Architecture arch)
    {
        TargetTestHelpers targetTestHelpers = new(arch);
        ContractDescriptorBuilder builder = new(targetTestHelpers);
        ContractDescriptorBuilder.DescriptorBuilder descriptorBuilder = new(builder);
        descriptorBuilder.SetTypes(new Dictionary<DataType, Target.TypeInfo>())
            .SetGlobals(TestGlobals)
            .SetContracts([]);

        bool success = builder.TryCreateTarget(descriptorBuilder, out ContractDescriptorTarget? target);
        Assert.True(success);

        ValidateGlobals(target, TestGlobals);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ReadIndirectGlobalValue(MockTarget.Architecture arch)
    {
        TargetTestHelpers targetTestHelpers = new(arch);
        ContractDescriptorBuilder builder = new(targetTestHelpers);
        ContractDescriptorBuilder.DescriptorBuilder descriptorBuilder = new(builder);
        descriptorBuilder.SetTypes(new Dictionary<DataType, Target.TypeInfo>())
            .SetContracts([])
            .SetGlobals(TestGlobals.Select(MakeGlobalToIndirect).ToArray(),
                        TestGlobals.Select((g) => g.Value).ToArray());

        bool success = builder.TryCreateTarget(descriptorBuilder, out ContractDescriptorTarget? target);
        Assert.True(success);

        // Indirect values are pointer-sized, so max 32-bits for a 32-bit target
        var expected = arch.Is64Bit
            ? TestGlobals
            : TestGlobals.Select(g => (g.Name, g.Value & 0xffffffff, g.Type)).ToArray();

        ValidateGlobals(target, expected);

        static (string Name, ulong? Value, uint? IndirectIndex, string? StringValue, string? Type) MakeGlobalToIndirect((string Name, ulong Value, string? Type) global, int index)
        {
            return (global.Name, null, (uint?)index, null, global.Type);
        }
    }

    private static readonly (string Name, string Value, ulong? NumericValue)[] GlobalStringyValues =
    [
        ("value", "testString", null),
        ("emptyString", "", null),
        ("specialChars", "string with spaces & special chars ✓", null),
        ("longString", new string('a', 1024), null),
        ("hexString", "0x1234", 0x1234),
        ("decimalString", "123456", 123456),
        ("negativeString", "-1234", unchecked((ulong)-1234)),
        ("negativeHexString", "-0x1234", unchecked((ulong)-0x1234)),
    ];

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ReadGlobalStringyValues(MockTarget.Architecture arch)
    {
        TargetTestHelpers targetTestHelpers = new(arch);
        ContractDescriptorBuilder builder = new(targetTestHelpers);
        ContractDescriptorBuilder.DescriptorBuilder descriptorBuilder = new(builder);
        descriptorBuilder.SetTypes(new Dictionary<DataType, Target.TypeInfo>())
            .SetContracts([])
            .SetGlobals(GlobalStringyValues.Select(MakeGlobalsToStrings).ToArray());

        bool success = builder.TryCreateTarget(descriptorBuilder, out ContractDescriptorTarget? target);
        Assert.True(success);

        ValidateGlobalStrings(target, GlobalStringyValues);

        static (string Name, ulong? Value, string? StringValue, string? Type) MakeGlobalsToStrings((string Name, string Value, ulong? NumericValue) global)
        {
            return (global.Name, null, global.Value, global.NumericValue is null ? "string" : null);
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ReadUtf8String(MockTarget.Architecture arch)
    {
        TargetTestHelpers targetTestHelpers = new(arch);
        ContractDescriptorBuilder builder = new(targetTestHelpers);

        string expected = "UTF-8 string ✓";
        ulong addr = 0x1000;

        MockMemorySpace.HeapFragment fragment = new() { Address = addr, Data = new byte[Encoding.UTF8.GetByteCount(expected) + 1] };
        Encoding.UTF8.GetBytes(expected).AsSpan().CopyTo(fragment.Data);
        fragment.Data[^1] = 0;
        builder.AddHeapFragment(fragment);

        bool success = builder.TryCreateTarget(new(builder), out ContractDescriptorTarget? target);
        Assert.True(success);

        string actual = target.ReadUtf8String(addr);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void WriteValue(MockTarget.Architecture arch)
    {
        TargetTestHelpers targetTestHelpers = new(arch);
        ContractDescriptorBuilder builder = new(targetTestHelpers);
        uint expected = 0xdeadbeef;
        ulong addr = 0x1000;

        MockMemorySpace.HeapFragment fragment = new() { Address = addr, Data = new byte[4] };
        builder.AddHeapFragment(fragment);

        bool success = builder.TryCreateTarget(new(builder), out ContractDescriptorTarget? target);
        Assert.True(success);
        target.Write<uint>(addr, expected);
        Assert.Equal(expected, target.Read<uint>(addr));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void WriteBuffer(MockTarget.Architecture arch)
    {
        TargetTestHelpers targetTestHelpers = new(arch);
        ContractDescriptorBuilder builder = new(targetTestHelpers);
        byte[] expected = new byte[] { 0xde, 0xad, 0xbe, 0xef };
        ulong addr = 0x1000;

        MockMemorySpace.HeapFragment fragment = new() { Address = addr, Data = new byte[4] };
        builder.AddHeapFragment(fragment);

        bool success = builder.TryCreateTarget(new(builder), out ContractDescriptorTarget? target);
        Assert.True(success);
        target.WriteBuffer(addr, expected);
        Span<byte> data = stackalloc byte[4];
        target.ReadBuffer(addr, data);
        Assert.Equal(expected, data);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ReadUtf16String(MockTarget.Architecture arch)
    {
        TargetTestHelpers targetTestHelpers = new(arch);
        ContractDescriptorBuilder builder = new(targetTestHelpers);

        string expected = "UTF-16 string ✓";
        ulong addr = 0x1000;

        Encoding encoding = arch.IsLittleEndian ? Encoding.Unicode : Encoding.BigEndianUnicode;
        MockMemorySpace.HeapFragment fragment = new() { Address = addr, Data = new byte[encoding.GetByteCount(expected) + sizeof(char)] };
        targetTestHelpers.WriteUtf16String(fragment.Data, expected);
        builder.AddHeapFragment(fragment);

        bool success = builder.TryCreateTarget(new(builder), out ContractDescriptorTarget? target);
        Assert.True(success);

        string actual = target.ReadUtf16String(addr);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void Create_InvalidDescriptorJson_ThrowsFormatException(MockTarget.Architecture arch)
    {
        TargetTestHelpers targetTestHelpers = new(arch);
        ContractDescriptorBuilder builder = new(targetTestHelpers);
        byte[] descriptor = new byte[ContractDescriptorHelpers.Size(targetTestHelpers.Arch.Is64Bit)];
        byte[] descriptorJson = "{ invalid json"u8.ToArray();
        ContractDescriptorHelpers.Fill(descriptor, targetTestHelpers.Arch, descriptorJson.Length, 0xdddddddd, 0, 0xeeeeeeee);

        FormatException ex = Assert.Throws<FormatException>(() => builder.CreateTargetFromRawDescriptor(descriptor, descriptorJson, []));
        Assert.IsAssignableFrom<System.Text.Json.JsonException>(ex.InnerException);
        Assert.Equal(CdacHResults.CDAC_E_DESCRIPTOR_MALFORMED, ex.HResult);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void Create_UnsupportedDataDescriptorVersion_ThrowsFormatException(MockTarget.Architecture arch)
    {
        int?[] unsupportedVersions = [null, 0, 1, 3];

        foreach (int? version in unsupportedVersions)
        {
            TargetTestHelpers targetTestHelpers = new(arch);
            ContractDescriptorBuilder builder = new(targetTestHelpers);
            ContractDescriptorBuilder.DescriptorBuilder descriptorBuilder = new(builder);
            descriptorBuilder.SetVersion(version);

            FormatException ex = Assert.Throws<FormatException>(() => builder.CreateTarget(descriptorBuilder));
            Assert.Equal(CdacHResults.CDAC_E_DESCRIPTOR_MALFORMED, ex.HResult);
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void Create_InvalidMagic_ThrowsDescriptorNotFound(MockTarget.Architecture arch)
    {
        TargetTestHelpers targetTestHelpers = new(arch);
        ContractDescriptorBuilder builder = new(targetTestHelpers);
        byte[] descriptor = new byte[ContractDescriptorHelpers.Size(targetTestHelpers.Arch.Is64Bit)];
        byte[] descriptorJson = "{}"u8.ToArray();
        ContractDescriptorHelpers.Fill(descriptor, targetTestHelpers.Arch, descriptorJson.Length, 0xdddddddd, 0, 0xeeeeeeee);
        // Corrupt the magic so the bytes at the descriptor address are not a recognized contract descriptor.
        descriptor[0] ^= 0xFF;

        FormatException ex = Assert.Throws<FormatException>(() => builder.CreateTargetFromRawDescriptor(descriptor, descriptorJson, []));
        Assert.Equal(CdacHResults.CDAC_E_DESCRIPTOR_NOT_FOUND, ex.HResult);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void Create_InvalidPointerDataIndex_ThrowsFormatException(MockTarget.Architecture arch)
    {
        TargetTestHelpers targetTestHelpers = new(arch);
        ContractDescriptorBuilder builder = new(targetTestHelpers);
        ContractDescriptorBuilder.DescriptorBuilder descriptorBuilder = new(builder);
        descriptorBuilder.SetGlobals([("invalid", null, (uint?)1, null, "pointer")])
            .SetIndirectValues([0]);

        FormatException ex = Assert.Throws<FormatException>(() => builder.CreateTarget(descriptorBuilder));
        Assert.Equal(CdacHResults.CDAC_E_DESCRIPTOR_MALFORMED, ex.HResult);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void TryCreateTarget_InvalidPointerDataIndex_ReturnsFalse(MockTarget.Architecture arch)
    {
        TargetTestHelpers targetTestHelpers = new(arch);
        ContractDescriptorBuilder builder = new(targetTestHelpers);
        ContractDescriptorBuilder.DescriptorBuilder descriptorBuilder = new(builder);
        descriptorBuilder.SetGlobals([("invalid", null, (uint?)1, null, "pointer")])
            .SetIndirectValues([0]);

        Assert.False(builder.TryCreateTarget(descriptorBuilder, out _));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetContract_MissingContract_ThrowsContractMissingException(MockTarget.Architecture arch)
    {
        TargetTestHelpers targetTestHelpers = new(arch);
        ContractDescriptorBuilder builder = new(targetTestHelpers);
        ContractDescriptorBuilder.DescriptorBuilder descriptorBuilder = new(builder);
        descriptorBuilder.SetContracts([]);

        Assert.True(builder.TryCreateTarget(descriptorBuilder, out ContractDescriptorTarget? target));

        ContractMissingException ex = Assert.Throws<ContractMissingException>(() => target.Contracts.RuntimeInfo);
        Assert.Equal("RuntimeInfo", ex.ContractName);
        Assert.Null(ex.ContractVersion);
        Assert.Equal(CdacHResults.CDAC_E_CONTRACT_NOT_ADVERTISED, ex.HResult);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetContract_UnrecognizedVersion_ThrowsContractUnrecognizedException(MockTarget.Architecture arch)
    {
        TargetTestHelpers targetTestHelpers = new(arch);
        ContractDescriptorBuilder builder = new(targetTestHelpers);
        ContractDescriptorBuilder.DescriptorBuilder descriptorBuilder = new(builder);
        descriptorBuilder.SetContracts(new Dictionary<string, string> { ["RuntimeInfo"] = "unsupported-version" });

        Assert.True(builder.TryCreateTarget(
            descriptorBuilder,
            out ContractDescriptorTarget? target));

        ContractUnrecognizedException ex = Assert.Throws<ContractUnrecognizedException>(() => target.Contracts.RuntimeInfo);
        Assert.Equal("RuntimeInfo", ex.ContractName);
        Assert.Equal("unsupported-version", ex.ContractVersion);
        Assert.Equal(CdacHResults.CDAC_E_CONTRACT_UNRECOGNIZED, ex.HResult);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetContract_ObsoleteVersion_ThrowsContractObsoleteException(MockTarget.Architecture arch)
    {
        TargetTestHelpers targetTestHelpers = new(arch);
        ContractDescriptorBuilder builder = new(targetTestHelpers);
        ContractDescriptorBuilder.DescriptorBuilder descriptorBuilder = new(builder);
        descriptorBuilder.SetContracts(new Dictionary<string, string> { ["RuntimeInfo"] = "obsolete-version" });

        Assert.True(builder.TryCreateTarget(
            descriptorBuilder,
            out ContractDescriptorTarget? target,
            registry => registry.RegisterUnsupported<Contracts.IRuntimeInfo>("obsolete-version")));

        ContractObsoleteException ex = Assert.Throws<ContractObsoleteException>(() => target.Contracts.RuntimeInfo);
        Assert.Equal("RuntimeInfo", ex.ContractName);
        Assert.Equal("obsolete-version", ex.ContractVersion);
        Assert.Equal(CdacHResults.CDAC_E_CONTRACT_UNSUPPORTED, ex.HResult);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void TryGetContract_UnrecognizedVersion_ReturnsContractUnrecognizedException(MockTarget.Architecture arch)
    {
        TargetTestHelpers targetTestHelpers = new(arch);
        ContractDescriptorBuilder builder = new(targetTestHelpers);
        ContractDescriptorBuilder.DescriptorBuilder descriptorBuilder = new(builder);
        descriptorBuilder.SetContracts(new Dictionary<string, string> { ["RuntimeInfo"] = "unsupported-version" });

        Assert.True(builder.TryCreateTarget(descriptorBuilder, out ContractDescriptorTarget? target));

        Assert.False(target.Contracts.TryGetContract<Contracts.IRuntimeInfo>(out _, out System.Exception? failureException));
        ContractUnrecognizedException ex = Assert.IsType<ContractUnrecognizedException>(failureException);
        Assert.Equal("RuntimeInfo", ex.ContractName);
        Assert.Equal("unsupported-version", ex.ContractVersion);
    }

    // The contracts required by the data-access interfaces, advertised at the versions
    // CoreCLRContracts registers. Mirrors CoreCLRContracts.ValidateForDataAccess.
    internal static readonly IReadOnlyDictionary<string, string> s_requiredDataAccessContracts =
        new Dictionary<string, string>
        {
            ["AuxiliarySymbols"] = "c1",
            ["BuiltInCOM"] = "c1",
            ["CodeNotifications"] = "c1",
            ["CodeVersions"] = "c1",
            ["ComWrappers"] = "c1",
            ["ConditionalWeakTable"] = "c1",
            ["DacStreams"] = "c1",
            ["Debugger"] = "c1",
            ["DebugInfo"] = "c1",
            ["EcmaMetadata"] = "c1",
            ["Exception"] = "c1",
            ["ExecutionManager"] = "c1",
            ["ExternalMemoryHandles"] = "c1",
            ["FeatureFlags"] = "c1",
            ["GC"] = "c1",
            ["GCInfo"] = "c1",
            ["Loader"] = "c1",
            ["Notifications"] = "c1",
            ["Object"] = "c1",
            ["PlatformMetadata"] = "c1",
            ["PrecodeStubs"] = "c1",
            ["ReJIT"] = "c1",
            ["RuntimeInfo"] = "c1",
            ["RuntimeMutableTypeSystem"] = "c1",
            ["RuntimeTypeSystem"] = "c1",
            ["SHash"] = "c1",
            ["Signature"] = "c1",
            ["StackWalk"] = "c1",
            ["StressLog"] = "c2",
            ["SyncBlock"] = "c1",
            ["Thread"] = "c1",
        };

    // A string-valued "OperatingSystem" contract-descriptor global, used to drive the target
    // platform that ValidateForDataAccess reads when deciding which OS-specific contracts to require.
    private static readonly (string Name, ulong? Value, string? StringValue, string? TypeName)[] s_windowsOperatingSystemGlobal =
        [("OperatingSystem", null, "windows", "string")];
    private static readonly (string Name, ulong? Value, string? StringValue, string? TypeName)[] s_unixOperatingSystemGlobal =
        [("OperatingSystem", null, "unix", "string")];

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void TryGetContractVersion_DoesNotInstantiateOrValidateContract(MockTarget.Architecture arch)
    {
        TargetTestHelpers targetTestHelpers = new(arch);
        ContractDescriptorBuilder builder = new(targetTestHelpers);
        ContractDescriptorBuilder.DescriptorBuilder descriptorBuilder = new(builder);
        descriptorBuilder.SetContracts(new Dictionary<string, string>
        {
            ["GCInfo"] = "c1",
            ["StackWalk"] = "c99",
        });

        Assert.True(builder.TryCreateTarget(descriptorBuilder, out ContractDescriptorTarget? target));

        Assert.True(target.Contracts.TryGetContractVersion<Contracts.IGCInfo>(out string? version));
        Assert.Equal("c1", version);
        Assert.True(target.Contracts.TryGetContractVersion<Contracts.IStackWalk>(out version));
        Assert.Equal("c99", version);
        Assert.False(target.Contracts.TryGetContractVersion<Contracts.IRuntimeInfo>(out version));
        Assert.Null(version);
        Assert.Throws<ContractMissingException>(() => target.Contracts.GCInfo);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void TryValidate_RegisteredVersion_ReturnsTrue(MockTarget.Architecture arch)
    {
        TargetTestHelpers targetTestHelpers = new(arch);
        ContractDescriptorBuilder builder = new(targetTestHelpers);
        ContractDescriptorBuilder.DescriptorBuilder descriptorBuilder = new(builder);
        descriptorBuilder.SetContracts(new Dictionary<string, string> { ["RuntimeInfo"] = "c1" });

        Assert.True(builder.TryCreateTarget(descriptorBuilder, out ContractDescriptorTarget? target));

        Assert.True(target.Contracts.TryValidate<Contracts.IRuntimeInfo>(out System.Exception? failure));
        Assert.Null(failure);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void TryValidate_DoesNotInstantiateContract(MockTarget.Architecture arch)
    {
        // GCInfo's creator reads RuntimeInfo from target memory. Advertise GCInfo but omit RuntimeInfo:
        // TryValidate must succeed (a creator is registered) without invoking it, whereas a real
        // GetContract would chain into RuntimeInfo and fail.
        TargetTestHelpers targetTestHelpers = new(arch);
        ContractDescriptorBuilder builder = new(targetTestHelpers);
        ContractDescriptorBuilder.DescriptorBuilder descriptorBuilder = new(builder);
        descriptorBuilder.SetContracts(new Dictionary<string, string> { ["GCInfo"] = "c1" });

        Assert.True(builder.TryCreateTarget(descriptorBuilder, out ContractDescriptorTarget? target));

        Assert.True(target.Contracts.TryValidate<Contracts.IGCInfo>(out System.Exception? failure));
        Assert.Null(failure);
        Assert.Throws<ContractMissingException>(() => target.Contracts.GCInfo);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void TryValidate_MissingContract_ReturnsContractMissingException(MockTarget.Architecture arch)
    {
        TargetTestHelpers targetTestHelpers = new(arch);
        ContractDescriptorBuilder builder = new(targetTestHelpers);
        ContractDescriptorBuilder.DescriptorBuilder descriptorBuilder = new(builder);
        descriptorBuilder.SetContracts([]);

        Assert.True(builder.TryCreateTarget(descriptorBuilder, out ContractDescriptorTarget? target));

        Assert.False(target.Contracts.TryValidate<Contracts.IRuntimeInfo>(out System.Exception? failure));
        Assert.IsType<ContractMissingException>(failure);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ValidateForDataAccess_AllRequiredPresent_DoesNotThrow(MockTarget.Architecture arch)
    {
        foreach (bool portableEntrypoints in new[] { false, true })
        {
            TargetTestHelpers targetTestHelpers = new(arch);
            ContractDescriptorBuilder builder = new(targetTestHelpers);
            ContractDescriptorBuilder.DescriptorBuilder descriptorBuilder = new(builder);
            descriptorBuilder
                .SetContracts(
                    s_requiredDataAccessContracts
                        .Where(pair => !portableEntrypoints || pair.Key != "PrecodeStubs")
                        .ToDictionary(static pair => pair.Key, static pair => pair.Value))
                .SetGlobals(
                [
                    (Constants.Globals.FeaturePortableEntrypoints, portableEntrypoints ? 1ul : 0ul, null, "uint8"),
                ]);

            Assert.True(builder.TryCreateTarget(descriptorBuilder, out ContractDescriptorTarget? target));

            Contracts.CoreCLRContracts.ValidateForDataAccess(target);
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ValidateForDataAccess_Net11Target_DoesNotRequireExternalMemoryHandles(MockTarget.Architecture arch)
    {
        TargetTestHelpers targetTestHelpers = new(arch);
        ContractDescriptorBuilder builder = new(targetTestHelpers);
        ContractDescriptorBuilder.DescriptorBuilder descriptorBuilder = new(builder);
        descriptorBuilder
            .SetContracts(
                s_requiredDataAccessContracts
                    .Where(static pair => pair.Key != "ExternalMemoryHandles")
                    .ToDictionary(static pair => pair.Key, static pair => pair.Value))
            .SetGlobals(
            [
                (Constants.Globals.RuntimeProductVersionString, null, "11.0.0", "string"),
            ]);

        Assert.True(builder.TryCreateTarget(descriptorBuilder, out ContractDescriptorTarget? target));

        Contracts.CoreCLRContracts.ValidateForDataAccess(target);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ValidateForDataAccess_Net12Target_RequiresExternalMemoryHandles(MockTarget.Architecture arch)
    {
        TargetTestHelpers targetTestHelpers = new(arch);
        ContractDescriptorBuilder builder = new(targetTestHelpers);
        ContractDescriptorBuilder.DescriptorBuilder descriptorBuilder = new(builder);
        descriptorBuilder
            .SetContracts(
                s_requiredDataAccessContracts
                    .Where(static pair => pair.Key != "ExternalMemoryHandles")
                    .ToDictionary(static pair => pair.Key, static pair => pair.Value))
            .SetGlobals(
            [
                (Constants.Globals.RuntimeProductVersionString, null, "12.0.0", "string"),
            ]);

        Assert.True(builder.TryCreateTarget(descriptorBuilder, out ContractDescriptorTarget? target));

        ContractMissingException ex = Assert.Throws<ContractMissingException>(
            () => Contracts.CoreCLRContracts.ValidateForDataAccess(target));
        Assert.Equal("ExternalMemoryHandles", ex.ContractName);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ValidateForDataAccess_MissingRequiredContract_ThrowsNotAdvertised(MockTarget.Architecture arch)
    {
        TargetTestHelpers targetTestHelpers = new(arch);
        ContractDescriptorBuilder builder = new(targetTestHelpers);
        ContractDescriptorBuilder.DescriptorBuilder descriptorBuilder = new(builder);
        descriptorBuilder.SetContracts(
            s_requiredDataAccessContracts
                .Where(static pair => pair.Key != "RuntimeInfo")
                .ToDictionary(static pair => pair.Key, static pair => pair.Value));

        Assert.True(builder.TryCreateTarget(descriptorBuilder, out ContractDescriptorTarget? target));

        ContractMissingException ex = Assert.Throws<ContractMissingException>(
            () => Contracts.CoreCLRContracts.ValidateForDataAccess(target));
        Assert.Equal(CdacHResults.CDAC_E_CONTRACT_NOT_ADVERTISED, ex.HResult);
        Assert.Equal("RuntimeInfo", ex.ContractName);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ValidateForDataAccess_MissingTransitiveContract_ThrowsNotAdvertised(MockTarget.Architecture arch)
    {
        string[] transitiveDependencies = ["ConditionalWeakTable", "Debugger", "PrecodeStubs", "SHash"];

        foreach (string missingContract in transitiveDependencies)
        {
            TargetTestHelpers targetTestHelpers = new(arch);
            ContractDescriptorBuilder builder = new(targetTestHelpers);
            ContractDescriptorBuilder.DescriptorBuilder descriptorBuilder = new(builder);
            descriptorBuilder.SetContracts(
                s_requiredDataAccessContracts
                    .Where(pair => pair.Key != missingContract)
                    .ToDictionary(static pair => pair.Key, static pair => pair.Value));

            Assert.True(builder.TryCreateTarget(descriptorBuilder, out ContractDescriptorTarget? target));

            ContractMissingException ex = Assert.Throws<ContractMissingException>(
                () => Contracts.CoreCLRContracts.ValidateForDataAccess(target));
            Assert.Equal(CdacHResults.CDAC_E_CONTRACT_NOT_ADVERTISED, ex.HResult);
            Assert.Equal(missingContract, ex.ContractName);
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ValidateForDataAccess_UnrecognizedRequiredVersion_ThrowsUnrecognized(MockTarget.Architecture arch)
    {
        TargetTestHelpers targetTestHelpers = new(arch);
        ContractDescriptorBuilder builder = new(targetTestHelpers);
        ContractDescriptorBuilder.DescriptorBuilder descriptorBuilder = new(builder);
        Dictionary<string, string> contracts = new(s_requiredDataAccessContracts);
        contracts["RuntimeInfo"] = "version-from-the-future";
        descriptorBuilder.SetContracts(contracts);

        Assert.True(builder.TryCreateTarget(descriptorBuilder, out ContractDescriptorTarget? target));

        ContractUnrecognizedException ex = Assert.Throws<ContractUnrecognizedException>(
            () => Contracts.CoreCLRContracts.ValidateForDataAccess(target));
        Assert.Equal(CdacHResults.CDAC_E_CONTRACT_UNRECOGNIZED, ex.HResult);
        Assert.Equal("RuntimeInfo", ex.ContractName);
        Assert.Equal("version-from-the-future", ex.ContractVersion);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ValidateForDataAccess_DeprecatedContractReference_ThrowsUnsupported(MockTarget.Architecture arch)
    {
        TargetTestHelpers targetTestHelpers = new(arch);
        ContractDescriptorBuilder builder = new(targetTestHelpers);
        ContractDescriptorBuilder.DescriptorBuilder descriptorBuilder = new(builder);
        Dictionary<string, string> contracts = new(s_requiredDataAccessContracts);
        contracts["RuntimeInfo"] = "deprecated-version";
        descriptorBuilder.SetContracts(contracts);

        // Model a target descriptor that still references a contract version this cDAC recognizes
        // as deprecated. Production registrations can use the same mechanism when a version retires.
        Assert.True(builder.TryCreateTarget(
            descriptorBuilder,
            out ContractDescriptorTarget? target,
            registry => registry.RegisterUnsupported<Contracts.IRuntimeInfo>("deprecated-version")));

        ContractObsoleteException ex = Assert.Throws<ContractObsoleteException>(
            () => Contracts.CoreCLRContracts.ValidateForDataAccess(target));
        Assert.Equal(CdacHResults.CDAC_E_CONTRACT_UNSUPPORTED, ex.HResult);
        Assert.Equal("RuntimeInfo", ex.ContractName);
        Assert.Equal("deprecated-version", ex.ContractVersion);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ValidateForDataAccess_WindowsTarget_RequiresWindowsErrorReporting(MockTarget.Architecture arch)
    {
        TargetTestHelpers targetTestHelpers = new(arch);
        ContractDescriptorBuilder builder = new(targetTestHelpers);
        ContractDescriptorBuilder.DescriptorBuilder descriptorBuilder = new(builder);

        // A Windows target advertising every cross-platform contract but not the Windows-only
        // WindowsErrorReporting must still fail validation, since SOS reaches it unconditionally
        // (GetClrWatsonBuckets) on Windows.
        descriptorBuilder
            .SetContracts(s_requiredDataAccessContracts)
            .SetGlobals(s_windowsOperatingSystemGlobal);

        Assert.True(builder.TryCreateTarget(descriptorBuilder, out ContractDescriptorTarget? target));

        ContractMissingException ex = Assert.Throws<ContractMissingException>(
            () => Contracts.CoreCLRContracts.ValidateForDataAccess(target));
        Assert.Equal(CdacHResults.CDAC_E_CONTRACT_NOT_ADVERTISED, ex.HResult);
        Assert.Equal("WindowsErrorReporting", ex.ContractName);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ValidateForDataAccess_WindowsTargetWithWindowsErrorReporting_DoesNotThrow(MockTarget.Architecture arch)
    {
        TargetTestHelpers targetTestHelpers = new(arch);
        ContractDescriptorBuilder builder = new(targetTestHelpers);
        ContractDescriptorBuilder.DescriptorBuilder descriptorBuilder = new(builder);

        Dictionary<string, string> contracts = new(s_requiredDataAccessContracts)
        {
            ["WindowsErrorReporting"] = "c1",
        };
        descriptorBuilder
            .SetContracts(contracts)
            .SetGlobals(s_windowsOperatingSystemGlobal);

        Assert.True(builder.TryCreateTarget(descriptorBuilder, out ContractDescriptorTarget? target));

        Contracts.CoreCLRContracts.ValidateForDataAccess(target);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void ValidateForDataAccess_NonWindowsTarget_DoesNotRequireWindowsErrorReporting(MockTarget.Architecture arch)
    {
        TargetTestHelpers targetTestHelpers = new(arch);
        ContractDescriptorBuilder builder = new(targetTestHelpers);
        ContractDescriptorBuilder.DescriptorBuilder descriptorBuilder = new(builder);

        // A non-Windows target that omits WindowsErrorReporting must validate cleanly: the guard
        // must not require a contract the target platform never uses.
        descriptorBuilder
            .SetContracts(s_requiredDataAccessContracts)
            .SetGlobals(s_unixOperatingSystemGlobal);

        Assert.True(builder.TryCreateTarget(descriptorBuilder, out ContractDescriptorTarget? target));

        Contracts.CoreCLRContracts.ValidateForDataAccess(target);
    }

    private static void ValidateGlobals(
        ContractDescriptorTarget target,
        (string Name, ulong Value, string? Type)[] globals,
        [CallerMemberName] string caller = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        foreach (var (name, value, type) in globals)
        {
            // Validate that each global can be read successfully based on its type
            // and that it matches the expected value
            if (type is null || type == "int8")
            {
                sbyte actual = target.ReadGlobal<sbyte>(name, out string? actualType);
                AssertEqualsWithCallerInfo(actualType, type);
                AssertEqualsWithCallerInfo((sbyte)value, actual);
            }

            if (type is null || type == "uint8")
            {
                byte actual = target.ReadGlobal<byte>(name, out string? actualType);
                AssertEqualsWithCallerInfo(actualType, type);
                AssertEqualsWithCallerInfo(value, actual);
            }

            if (type is null || type == "int16")
            {
                short actual = target.ReadGlobal<short>(name, out string? actualType);
                AssertEqualsWithCallerInfo(actualType, type);
                AssertEqualsWithCallerInfo((short)value, actual);
            }

            if (type is null || type == "uint16")
            {
                ushort actual = target.ReadGlobal<ushort>(name, out string? actualType);
                AssertEqualsWithCallerInfo(actualType, type);
                AssertEqualsWithCallerInfo(value, actual);
            }

            if (type is null || type == "int32")
            {
                int actual = target.ReadGlobal<int>(name, out string? actualType);
                AssertEqualsWithCallerInfo(actualType, type);
                AssertEqualsWithCallerInfo((int)value, actual);
            }

            if (type is null || type == "uint32")
            {
                uint actual = target.ReadGlobal<uint>(name, out string? actualType);
                AssertEqualsWithCallerInfo(actualType, type);
                AssertEqualsWithCallerInfo((uint)value, actual);
            }

            if (type is null || type == "int64")
            {
                long actual = target.ReadGlobal<long>(name, out string? actualType);
                AssertEqualsWithCallerInfo(actualType, type);
                AssertEqualsWithCallerInfo((long)value, actual);
            }

            if (type is null || type == "uint64")
            {
                ulong actual = target.ReadGlobal<ulong>(name, out string? actualType);
                AssertEqualsWithCallerInfo(actualType, type);
                AssertEqualsWithCallerInfo(value, actual);
            }

            if (type is null || type == "pointer" || type == "nint" || type == "nuint")
            {
                TargetPointer actual = target.ReadGlobalPointer(name, out string? actualType);
                AssertEqualsWithCallerInfo(actualType, type);
                AssertEqualsWithCallerInfo(value, actual.Value);
            }
        }

        void AssertEqualsWithCallerInfo<T>(T expected, T actual)
        {
            Assert.True((expected is null && actual is null) || expected.Equals(actual), $"Expected: {expected}. Actual: {actual}. [test case: {caller} in {filePath}:{lineNumber}]");
        }
    }

    private static void ValidateGlobalStrings(
        ContractDescriptorTarget target,
        (string Name, string Value, ulong? NumericValue)[] globals,
        [CallerMemberName] string caller = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        foreach (var (name, value, numericValue) in globals)
        {
            string actualString;

            Assert.True(target.TryReadGlobalString(name, out actualString));
            AssertEqualsWithCallerInfo(value, actualString);

            actualString = target.ReadGlobalString(name);
            AssertEqualsWithCallerInfo(value, actualString);

            if (numericValue != null)
            {
                ulong? actualNumericValue;

                Assert.True(target.TryReadGlobal(name, out actualNumericValue));
                AssertEqualsWithCallerInfo(numericValue.Value, actualNumericValue.Value);

                actualNumericValue = target.ReadGlobal<ulong>(name);
                AssertEqualsWithCallerInfo(numericValue.Value, actualNumericValue.Value);

                TargetPointer? actualPointer;

                Assert.True(target.TryReadGlobalPointer(name, out actualPointer));
                AssertEqualsWithCallerInfo(numericValue.Value, actualPointer.Value.Value);

                actualPointer = target.ReadGlobalPointer(name);
                AssertEqualsWithCallerInfo(numericValue.Value, actualPointer.Value.Value);
            }
            else
            {
                // if there is no numeric value, assert that reading as numeric fails
                Assert.False(target.TryReadGlobal(name, out ulong? _));
                Assert.ThrowsAny<Exception>(() => target.ReadGlobal<ulong>(name));
                Assert.False(target.TryReadGlobalPointer(name, out TargetPointer? _));
                Assert.ThrowsAny<Exception>(() => target.ReadGlobalPointer(name));
            }
        }

        void AssertEqualsWithCallerInfo<T>(T expected, T actual)
        {
            Assert.True((expected is null && actual is null) || expected.Equals(actual), $"Expected: {expected}. Actual: {actual}. [test case: {caller} in {filePath}:{lineNumber}]");
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void PageCache_ReadsSharePagesAndPreserveEndianness(MockTarget.Architecture arch)
    {
        List<(ulong Address, int Size)> reads = [];
        byte[] memory = Enumerable.Range(0, 8192).Select(i => (byte)(i % 251)).ToArray();
        ContractDescriptorTarget target = CreatePageCacheTarget((address, buffer) =>
        {
            reads.Add((address, buffer.Length));
            memory.AsSpan(checked((int)address), buffer.Length).CopyTo(buffer);
            return 0;
        }, arch: arch);

        TargetTestHelpers helpers = new(arch);
        Span<byte> expected = stackalloc byte[8];
        helpers.Write(expected, 0x12345678ul);
        expected.CopyTo(memory.AsSpan(64));
        Assert.Equal(0x12345678ul, target.Read<ulong>(64));
        Assert.Equal(BinaryPrimitives.ReadUInt64LittleEndian(memory.AsSpan(64)), target.ReadLittleEndian<ulong>(64));
        Assert.True(target.TryRead<byte>(71, out byte value));
        Assert.Equal(memory[71], value);
        Assert.Equal(helpers.ReadPointer(memory.AsSpan(64)).Value, target.ReadNUInt(64).Value);
        Assert.Equal(helpers.ReadPointer(memory.AsSpan(64)), target.ReadPointer(64));
        Assert.True(target.TryReadPointer(64, out TargetPointer pointer));
        Assert.Equal(target.ReadPointer(64), pointer);
        byte[] actual = new byte[16];
        target.ReadBuffer(96, actual);
        Assert.Equal(memory.AsSpan(96, 16).ToArray(), actual);
        Assert.Equal(new (ulong, int)[] { (0, PageCachePageSize) }, reads);

        Assert.Equal(memory[PageCachePageSize], target.Read<byte>(PageCachePageSize));
        Assert.Equal(new (ulong, int)[] { (0, PageCachePageSize), (PageCachePageSize, PageCachePageSize) }, reads);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(PageCachePageSize - 2, 4)]
    [InlineData(PageCachePageSize, PageCachePageSize * 2)]
    public void PageCache_BypassesEmptyLargeAndCrossingReads(int address, int size)
    {
        List<(ulong Address, int Size)> reads = [];
        ContractDescriptorTarget target = CreatePageCacheTarget((location, buffer) =>
        {
            reads.Add((location, buffer.Length));
            buffer.Fill(0x5a);
            return 0;
        });
        byte[] buffer = new byte[size];
        target.ReadBuffer((ulong)address, buffer);
        target.ReadBuffer((ulong)address, buffer);
        Assert.All(buffer, value => Assert.Equal((byte)0x5a, value));
        Assert.Equal(new (ulong, int)[] { ((ulong)address, size), ((ulong)address, size) }, reads);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PageCache_FailedFillFallsBackWithoutCachingPartialBytes(bool failExactRead)
    {
        const ulong Address = 0x1234;
        List<(ulong Address, int Size)> reads = [];
        ContractDescriptorTarget target = CreatePageCacheTarget((address, buffer) =>
        {
            reads.Add((address, buffer.Length));
            buffer.Fill(0xcc);
            if (buffer.Length == PageCachePageSize || failExactRead)
                return HResults.E_FAIL;

            buffer[0] = 0x5a;
            return 0;
        });
        List<(ulong Address, ulong Size)> reported = [];
        using IDisposable scope = target.RegisterReadCallback((address, size) => reported.Add((address, size)));
        for (int i = 0; i < 2; i++)
        {
            Assert.Equal(!failExactRead, target.TryRead<byte>(Address, out byte value));
            Assert.Equal(failExactRead ? (byte)0 : (byte)0x5a, value);
        }
        Assert.Equal(new (ulong, int)[] { (0x1000, PageCachePageSize), (Address, 1), (Address, 1) }, reads);
        Assert.Equal(failExactRead ? [] : new (ulong, ulong)[] { (Address, 1), (Address, 1) }, reported);
        if (failExactRead)
            Assert.Throws<VirtualReadException>(() => target.Read<byte>(Address));
    }

    [Theory]
    [InlineData(FlushScope.All)]
    [InlineData(FlushScope.ForwardExecution)]
    public void PageCache_FlushRetriesPreviouslyUnavailablePages(FlushScope scope)
    {
        int pageReads = 0;
        int directReads = 0;
        bool available = false;
        ContractDescriptorTarget target = CreatePageCacheTarget((_, buffer) =>
        {
            buffer.Fill(0x5a);
            if (buffer.Length == PageCachePageSize)
            {
                pageReads++;
                return available ? 0 : HResults.E_FAIL;
            }
            directReads++;
            return 0;
        });
        Assert.Equal((byte)0x5a, target.Read<byte>(0x1001));
        available = true;
        Assert.Equal((byte)0x5a, target.Read<byte>(0x1002));
        Assert.Equal(1, pageReads);
        Assert.Equal(2, directReads);

        target.Flush(scope);
        Assert.Equal((byte)0x5a, target.Read<byte>(0x1001));
        Assert.Equal((byte)0x5a, target.Read<byte>(0x1002));
        Assert.Equal(2, pageReads);
        Assert.Equal(2, directReads);
    }

    [Fact]
    public void PageCache_WriteRetriesPreviouslyUnavailablePage()
    {
        int pageReads = 0;
        bool available = false;
        ContractDescriptorTarget target = CreatePageCacheTarget((_, buffer) =>
        {
            buffer.Fill(0x5a);
            if (buffer.Length != PageCachePageSize)
                return 0;

            pageReads++;
            return available ? 0 : HResults.E_FAIL;
        }, (_, _) => { available = true; return 0; });
        Assert.Equal((byte)0x5a, target.Read<byte>(0x1001));
        Assert.Equal((byte)0x5a, target.Read<byte>(0x1002));
        Assert.Equal(1, pageReads);

        target.Write<byte>(0x1001, 0x5a);
        Assert.Equal((byte)0x5a, target.Read<byte>(0x1001));
        Assert.Equal((byte)0x5a, target.Read<byte>(0x1002));
        Assert.Equal(2, pageReads);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PageCache_EvictionDoesNotReusePartiallyOverwrittenData(bool failReplacement)
    {
        const ulong First = 0x1000;
        const ulong Conflict = First + PageCachePageCount * PageCachePageSize;
        List<ulong> reads = [];
        ContractDescriptorTarget target = CreatePageCacheTarget((address, buffer) =>
        {
            reads.Add(address);
            buffer.Fill(address == First ? (byte)0x11 : (byte)0x22);
            return address == Conflict && buffer.Length == PageCachePageSize && failReplacement ? HResults.E_FAIL : 0;
        });
        Assert.Equal((byte)0x11, target.Read<byte>(First));
        for (int i = 1; i < PageCachePageCount; i++)
            Assert.Equal((byte)0x22, target.Read<byte>(First + (ulong)i * PageCachePageSize));

        Assert.Equal((byte)0x22, target.Read<byte>(Conflict));
        Assert.Equal((byte)0x11, target.Read<byte>(First));
        List<ulong> expected = Enumerable.Range(0, PageCachePageCount + 1).Select(i => First + (ulong)i * PageCachePageSize).ToList();
        if (failReplacement)
            expected.Add(Conflict);
        expected.Add(First);
        Assert.Equal(expected, reads);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(PageCachePageCount)]
    public void PageCache_DoesNotAliasPowerOfTwoStridedPages(int count)
    {
        List<ulong> reads = [];
        ContractDescriptorTarget target = CreatePageCacheTarget((address, buffer) =>
        {
            reads.Add(address);
            buffer.Fill((byte)(address / PageCachePageSize % 251));
            return 0;
        });
        ulong[] addresses = Enumerable.Range(0, count).Select(i => 0x1000ul + (ulong)i * PageCachePageCount * PageCachePageSize).ToArray();
        foreach (ulong address in addresses)
            Assert.Equal((byte)(address / PageCachePageSize % 251), target.Read<byte>(address));
        foreach (ulong address in addresses.Reverse())
            Assert.Equal((byte)(address / PageCachePageSize % 251), target.Read<byte>(address));
        Assert.Equal(addresses, reads);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(PageCachePageCount)]
    public void PageCache_DoesNotSuppressUnexpectedReaderExceptions(int cachedPages)
    {
        InvalidOperationException expected = new("Reader failed unexpectedly.");
        bool fail = false;
        ContractDescriptorTarget target = CreatePageCacheTarget((_, buffer) =>
        {
            buffer.Fill(fail ? (byte)0xcc : (byte)0x11);
            return fail ? throw expected : 0;
        });
        for (int i = 1; i <= cachedPages; i++)
            Assert.Equal((byte)0x11, target.Read<byte>((ulong)i * PageCachePageSize));
        fail = true;
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => target.Read<byte>((ulong)(PageCachePageCount + 1) * PageCachePageSize)));
        fail = false;
        Assert.Equal((byte)0x11, target.Read<byte>(0x1000));
    }

    [Theory]
    [InlineData(FlushScope.All)]
    [InlineData(FlushScope.ForwardExecution)]
    public void PageCache_FlushRefreshesMemoryAndSurvivesReadScopeDisposal(FlushScope scope)
    {
        byte value = 0x11;
        int reads = 0;
        ContractDescriptorTarget target = CreatePageCacheTarget((_, buffer) =>
        {
            reads++;
            buffer.Fill(value);
            return 0;
        });
        Assert.Equal(value, target.Read<byte>(0x1000));
        using (target.RegisterReadCallback((_, _) => { }))
        {
            value = 0x22;
            target.Flush(scope);
            Assert.Equal(value, target.Read<byte>(0x1000));
        }
        Assert.Equal(value, target.Read<byte>(0x1000));
        Assert.Equal(2, reads);
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(false, 1)]
    [InlineData(false, 2)]
    [InlineData(true, 0)]
    [InlineData(true, 1)]
    [InlineData(true, 2)]
    public void PageCache_WritesInvalidateEvenWhenTheyFail(bool scalar, int failureMode)
    {
        byte value = 0x11;
        int reads = 0;
        ContractDescriptorTarget target = CreatePageCacheTarget((_, buffer) =>
        {
            reads++;
            buffer.Fill(value);
            return 0;
        }, (_, buffer) =>
        {
            value = buffer[0];
            return failureMode switch
            {
                0 => 0,
                1 => HResults.E_FAIL,
                _ => throw new InvalidOperationException("Write failed after modifying memory."),
            };
        });
        Assert.Equal((byte)0x11, target.Read<byte>(0x1000));
        Action write = scalar ? () => target.Write<byte>(0x1000, 0x22) : () => target.WriteBuffer(0x1000, [0x22]);
        if (failureMode != 0)
            Assert.Throws<InvalidOperationException>(write);
        else
            write();

        Assert.Equal((byte)0x22, target.Read<byte>(0x1000));
        Assert.Equal(2, reads);
    }

    [Fact]
    public void PageCache_ReadCallbacksReportLogicalReadsNotPrefetch()
    {
        int physicalReads = 0;
        ContractDescriptorTarget target = CreatePageCacheTarget((_, buffer) =>
        {
            physicalReads++;
            buffer.Fill(0x11);
            return 0;
        });
        List<(ulong Address, ulong Size)> outer = [];
        List<(ulong Address, ulong Size)> inner = [];
        Assert.Equal((byte)0x11, target.Read<byte>(0x1000));
        using (target.RegisterReadCallback((address, size) => outer.Add((address, size))))
        {
            Assert.Equal((byte)0x11, target.Read<byte>(0x1001));
            using (target.RegisterReadCallback((address, size) => inner.Add((address, size))))
                Assert.Equal(0x11111111u, target.Read<uint>(0x1002));

            Assert.Equal((byte)0x11, target.Read<byte>(0x2001));
        }
        Assert.Equal((byte)0x11, target.Read<byte>(0x2002));
        Assert.Equal(new (ulong, ulong)[] { (0x1001, 1), (0x1002, 4), (0x2001, 1) }, outer);
        Assert.Equal(new (ulong, ulong)[] { (0x1002, 4) }, inner);
        Assert.Equal(2, physicalReads);
    }

    [Theory]
    [InlineData(0ul)]
    [InlineData(ulong.MaxValue)]
    public void PageCache_ExtremeAddressesAreNotConfusedWithEmptyEntries(ulong address)
    {
        int reads = 0;
        ContractDescriptorTarget target = CreatePageCacheTarget((_, buffer) =>
        {
            reads++;
            buffer.Fill(0x5a);
            return 0;
        });
        Assert.Equal((byte)0x5a, target.Read<byte>(address));
        Assert.Equal((byte)0x5a, target.Read<byte>(address));
        target.Flush(FlushScope.All);
        Assert.Equal((byte)0x5a, target.Read<byte>(address));
        Assert.Equal(2, reads);
    }

    [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
    [InlineData(true, 4096, 32)]
    [InlineData(true, 2048, 1)]
    [InlineData(true, 8192, 3)]
    [InlineData(true, 1, 1)]
    [InlineData(false, 4096, 32)]
    [InlineData(false, 0, 0)]
    public void PageCache_ConfigurationControlsReads(bool enabled, int pageSize, int pageCount)
    {
        RemoteInvokeOptions options = new();
        options.RuntimeConfigurationOptions[PageCacheEnabledSetting] = enabled;
        options.RuntimeConfigurationOptions[PageCachePageSizeSetting] = pageSize;
        options.RuntimeConfigurationOptions[PageCachePageCountSetting] = pageCount;
        using RemoteInvokeHandle handle = RemoteExecutor.Invoke(static (enabledText, sizeText, countText) =>
        {
            bool enabled = bool.Parse(enabledText);
            int pageSize = int.Parse(sizeText, CultureInfo.InvariantCulture);
            int pageCount = int.Parse(countText, CultureInfo.InvariantCulture);
            byte value = 0x11;
            List<(ulong, int)> reads = [];
            ContractDescriptorTarget target = CreatePageCacheTarget((address, buffer) =>
            {
                reads.Add((address, buffer.Length));
                buffer.Fill(value);
                return 0;
            }, (_, _) => { value = 0x33; return 0; });

            Assert.Equal(value, target.Read<byte>(0));
            Assert.Equal(value, target.Read<byte>(0));
            Assert.Equal(enabled ? new (ulong, int)[] { (0, pageSize) } : [(0, 1), (0, 1)], reads);
            if (enabled)
            {
                for (int i = 1; i <= pageCount; i++)
                    Assert.Equal(value, target.Read<byte>((ulong)i * (ulong)pageSize));
                Assert.Equal(value, target.Read<byte>(0));
                Assert.Equal(pageCount + 2, reads.Count);
                Assert.All(reads, read => Assert.Equal(pageSize, read.Item2));
            }

            value = 0x22;
            Assert.Equal(enabled ? (byte)0x11 : value, target.Read<byte>(0));
            target.Flush(FlushScope.All);
            Assert.Equal(value, target.Read<byte>(0));
            target.Write<byte>(0, 0x33);
            Assert.Equal((byte)0x33, target.Read<byte>(0));
            List<(ulong, ulong)> reported = [];
            using (target.RegisterReadCallback((address, size) => reported.Add((address, size))))
                Assert.Equal((byte)0x33, target.Read<byte>(0));
            Assert.Equal(new (ulong, ulong)[] { (0, 1) }, reported);
        }, enabled.ToString(), pageSize.ToString(CultureInfo.InvariantCulture), pageCount.ToString(CultureInfo.InvariantCulture), options);
    }

    [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
    [InlineData(PageCachePageSizeSetting, 0)]
    [InlineData(PageCachePageSizeSetting, -1)]
    [InlineData(PageCachePageSizeSetting, 4095)]
    [InlineData(PageCachePageSizeSetting, "not-an-integer")]
    [InlineData(PageCachePageSizeSetting, "2147483648")]
    [InlineData(PageCachePageSizeSetting, true)]
    [InlineData(PageCachePageCountSetting, 0)]
    [InlineData(PageCachePageCountSetting, -1)]
    [InlineData(PageCachePageCountSetting, int.MaxValue)]
    [InlineData(PageCachePageCountSetting, "not-an-integer")]
    public void PageCache_InvalidConfigurationThrows(string name, object value)
    {
        RemoteInvokeOptions options = new();
        options.RuntimeConfigurationOptions[name] = value;
        using RemoteInvokeHandle handle = RemoteExecutor.Invoke(static setting =>
        {
            ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
                () => CreatePageCacheTarget((_, _) => 0));
            Assert.Equal(setting, exception.ParamName);
        }, name, options);
    }

    [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
    public void PageCache_StringConfigurationIsCapturedAtConstruction()
    {
        RemoteInvokeOptions options = new();
        options.RuntimeConfigurationOptions[PageCachePageSizeSetting] = "1024";
        options.RuntimeConfigurationOptions[PageCachePageCountSetting] = "2";
        using RemoteInvokeHandle handle = RemoteExecutor.Invoke(static () =>
        {
            List<(ulong, int)> reads = [];
            ContractDescriptorTarget target = CreatePageCacheTarget((address, buffer) =>
            {
                reads.Add((address, buffer.Length));
                buffer.Fill(0x11);
                return 0;
            });
            AppContext.SetData(PageCachePageSizeSetting, 0);
            AppContext.SetData(PageCachePageCountSetting, 0);
            Assert.Equal((byte)0x11, target.Read<byte>(1023));
            Assert.Equal((byte)0x11, target.Read<byte>(1024));
            byte[] crossing = new byte[2];
            target.ReadBuffer(1023, crossing);
            Assert.Equal(new byte[] { 0x11, 0x11 }, crossing);
            Assert.Equal(new (ulong, int)[] { (0, 1024), (1024, 1024), (1023, 2) }, reads);
        }, options);
    }

    private static ContractDescriptorTarget CreatePageCacheTarget(
        ContractDescriptorTarget.ReadFromTargetDelegate read,
        ContractDescriptorTarget.WriteToTargetDelegate? write = null,
        MockTarget.Architecture? arch = null)
        => ContractDescriptorTarget.Create(
            new ContractDescriptorParser.ContractDescriptor { Version = 2 }, [], read,
            write ?? ((_, _) => HResults.E_NOTIMPL),
            (_, _, _) => HResults.E_NOTIMPL,
            (_, _) => HResults.E_NOTIMPL,
            (ulong _, out ulong address) => { address = 0; return HResults.E_NOTIMPL; },
            arch?.IsLittleEndian ?? true, arch?.Is64Bit == false ? 4 : 8);

    private static void ValidateTypes(
        ContractDescriptorTarget target,
        Dictionary<DataType, Target.TypeInfo> types,
        [CallerMemberName] string caller = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        foreach ((DataType type, Target.TypeInfo info) in types)
        {
            {
                // By known type
                Target.TypeInfo actual = target.GetTypeInfo(type);
                Assert.Equal(info.Size, actual.Size);
                Assert.Equal(info.Fields, actual.Fields);
            }
            {
                // By name
                Target.TypeInfo actual = target.GetTypeInfo(type.ToString());
                Assert.Equal(info.Size, actual.Size);
                Assert.Equal(info.Fields, actual.Fields);
            }
        }
    }
}
