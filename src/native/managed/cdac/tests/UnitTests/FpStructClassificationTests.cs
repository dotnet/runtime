// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Moq;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

using MockRTS = MockDescriptors.RuntimeTypeSystem;

// Tests for IRuntimeTypeSystem.TryGetFpStructInRegistersClassification, which mirrors
// MethodTable::GetFpStructInRegistersInfo (src/coreclr/vm/methodtable.cpp) for RISC-V and LoongArch64.
public class FpStructClassificationTests
{
    private static readonly MockTarget.Architecture Arch64 = new() { IsLittleEndian = true, Is64Bit = true };
    private const ulong ModuleAddress = 0x0002_0000;

    // A field of a mock value type. Nested names another type of the same test; its FieldDesc type must be ValueType.
    private readonly record struct FieldSpec(CorElementType Type, uint Offset, string? Nested = null);

    private sealed record TypeSpec(string Name, uint Size, FieldSpec[] Fields, bool HasLayout = false, bool IsInlineArray = false);

    private static FieldSpec F(CorElementType type, uint offset) => new(type, offset);
    private static FieldSpec Nested(string type, uint offset) => new(CorElementType.ValueType, offset, type);

    public static IEnumerable<object[]> FpArchitectures()
    {
        yield return [RuntimeInfoArchitecture.RiscV64];
        yield return [RuntimeInfoArchitecture.LoongArch64];
    }

    // Builds the value types (the last one is classified), wiring nested value-type fields through
    // metadata and the Signature contract the way GetFieldDescApproxTypeHandle resolves them.
    private static bool Classify(RuntimeInfoArchitecture architecture, TypeSpec[] types, out FpStructInRegistersClassification classification)
    {
        TestPlaceholderTarget.Builder targetBuilder = new(Arch64);
        MockRTS rtsBuilder = new(targetBuilder.MemoryBuilder);
        TargetTestHelpers helpers = rtsBuilder.Builder.TargetTestHelpers;

        MetadataBuilder mdBuilder = new();
        mdBuilder.AddModule(0, mdBuilder.GetOrAddString("TestModule"), mdBuilder.GetOrAddGuid(Guid.Empty), default, default);
        Dictionary<BlobHandle, string> nestedTypeBySignature = [];
        FieldDefinitionHandle firstField = default;
        int nextRid = 1;

        Dictionary<string, ulong> methodTables = [];
        foreach (TypeSpec type in types)
        {
            MockEEClass eeClass = type.HasLayout
                ? rtsBuilder.AddLayoutEEClass(type.Name, layoutType: 0, alignmentRequirement: 8, flags: 0)
                : rtsBuilder.AddEEClass(type.Name);
            if (type.IsInlineArray)
                eeClass.VMFlags |= 0x00010000; // VMFLAG_INLINE_ARRAY
            eeClass.CorTypeAttr = (uint)(TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.SequentialLayout);
            eeClass.NumInstanceFields = (ushort)type.Fields.Length;

            // A boxed value type's BaseSize is pointer-aligned and includes the object header and
            // MethodTable pointer; BaseSizePadding is what NumInstanceFieldBytes subtracts back out.
            uint baseSize = (helpers.ObjectBaseSize + type.Size + (uint)helpers.PointerSize - 1) & ~((uint)helpers.PointerSize - 1);
            eeClass.BaseSizePadding = (byte)(baseSize - type.Size);

            MockMethodTable methodTable = rtsBuilder.AddMethodTable(type.Name);
            methodTable.BaseSize = baseSize;
            methodTable.ParentMethodTable = rtsBuilder.SystemObjectMethodTable.Address;
            methodTable.NumVirtuals = 3;
            methodTable.Module = ModuleAddress;
            methodTable.EEClassOrCanonMT = eeClass.Address;
            eeClass.MethodTable = methodTable.Address;
            methodTables[type.Name] = methodTable.Address;

            var fieldDescs = new (CorElementType, uint, uint)[type.Fields.Length];
            for (int i = 0; i < type.Fields.Length; i++)
            {
                FieldSpec field = type.Fields[i];
                uint memberDef = 0;
                if (field.Nested is not null)
                {
                    // Each nested field gets a distinct signature blob so the Signature mock can resolve it.
                    BlobBuilder signature = new();
                    new BlobEncoder(signature).FieldSignature().Type(MetadataTokens.TypeDefinitionHandle(1000 + nextRid), isValueType: true);
                    BlobHandle signatureHandle = mdBuilder.GetOrAddBlob(signature);
                    FieldDefinitionHandle fieldHandle = mdBuilder.AddFieldDefinition(
                        FieldAttributes.Public, mdBuilder.GetOrAddString($"f{nextRid}"), signatureHandle);
                    if (firstField.IsNil)
                        firstField = fieldHandle;
                    nestedTypeBySignature[signatureHandle] = field.Nested;
                    memberDef = (uint)nextRid++;
                }
                fieldDescs[i] = (field.Type, field.Offset, memberDef);
            }
            eeClass.FieldDescList = rtsBuilder.AddFieldDescList(methodTable.Address, fieldDescs).Value;
        }

        mdBuilder.AddTypeDefinition(
            default, default, mdBuilder.GetOrAddString("<Module>"), default,
            firstField.IsNil ? MetadataTokens.FieldDefinitionHandle(1) : firstField,
            MetadataTokens.MethodDefinitionHandle(1));
        BlobBuilder metadataBlob = new();
        new MetadataRootBuilder(mdBuilder).Serialize(metadataBlob, 0, 0);
        using MetadataReaderProvider provider = MetadataReaderProvider.FromMetadataImage(ImmutableArray.Create(metadataBlob.ToArray()));
        MetadataReader reader = provider.GetMetadataReader();

        Contracts.ModuleHandle moduleHandle = new(new TargetPointer(ModuleAddress));
        Mock<IRuntimeInfo> runtimeInfo = new();
        runtimeInfo.Setup(r => r.GetTargetArchitecture()).Returns(architecture);
        Mock<ILoader> loader = new();
        loader.Setup(l => l.GetModuleHandleFromModulePtr(new TargetPointer(ModuleAddress))).Returns(moduleHandle);
        Mock<IEcmaMetadata> ecmaMetadata = new();
        ecmaMetadata.Setup(m => m.GetMetadata(moduleHandle)).Returns(reader);

        IRuntimeTypeSystem? rts = null;
        Mock<ISignature> signatureContract = new();
        signatureContract
            .Setup(s => s.DecodeFieldSignature(It.IsAny<BlobHandle>(), It.IsAny<Contracts.ModuleHandle>(), It.IsAny<ITypeHandle?>()))
            .Returns((BlobHandle blob, Contracts.ModuleHandle _, ITypeHandle? _) => rts!.GetTypeHandle(new TargetPointer(methodTables[nestedTypeBySignature[blob]])));

        TestPlaceholderTarget target = targetBuilder
            .AddTypes(MethodTableTests.CreateContractTypes(rtsBuilder))
            .AddGlobals(MethodTableTests.CreateContractGlobals(rtsBuilder))
            .AddContract<IRuntimeTypeSystem>(version: "c1")
            .AddMockContract(runtimeInfo)
            .AddMockContract(loader)
            .AddMockContract(ecmaMetadata)
            .AddMockContract(signatureContract)
            .Build();
        rts = target.Contracts.RuntimeTypeSystem;

        ITypeHandle handle = rts.GetTypeHandle(new TargetPointer(methodTables[types[^1].Name]));
        return rts.TryGetFpStructInRegistersClassification(handle, out classification);
    }

    private static void AssertClassification(
        RuntimeInfoArchitecture architecture,
        TypeSpec[] types,
        FpStructField first,
        FpStructField? second)
    {
        Assert.True(Classify(architecture, types, out FpStructInRegistersClassification classification));
        Assert.Equal(first, classification.First);
        Assert.Equal(second, classification.Second);
    }

    private static void AssertIntegerCallingConvention(RuntimeInfoArchitecture architecture, TypeSpec[] types)
    {
        Assert.False(Classify(architecture, types, out FpStructInRegistersClassification classification));
        Assert.Equal(default, classification);
    }

    [Theory]
    [MemberData(nameof(FpArchitectures))]
    public void OneFloat(RuntimeInfoArchitecture arch)
        => AssertClassification(arch,
            [new("S", 4, [F(CorElementType.R4, 0)])],
            new(true, 4, 0), null);

    [Theory]
    [MemberData(nameof(FpArchitectures))]
    public void OneDouble(RuntimeInfoArchitecture arch)
        => AssertClassification(arch,
            [new("S", 8, [F(CorElementType.R8, 0)])],
            new(true, 8, 0), null);

    [Theory]
    [MemberData(nameof(FpArchitectures))]
    public void TwoFloats(RuntimeInfoArchitecture arch)
        => AssertClassification(arch,
            [new("S", 8, [F(CorElementType.R4, 0), F(CorElementType.R4, 4)])],
            new(true, 4, 0), new(true, 4, 4));

    [Theory]
    [MemberData(nameof(FpArchitectures))]
    public void TwoDoubles(RuntimeInfoArchitecture arch)
        => AssertClassification(arch,
            [new("S", 16, [F(CorElementType.R8, 0), F(CorElementType.R8, 8)])],
            new(true, 8, 0), new(true, 8, 8));

    [Theory]
    [MemberData(nameof(FpArchitectures))]
    public void FloatThenInt(RuntimeInfoArchitecture arch)
        => AssertClassification(arch,
            [new("S", 8, [F(CorElementType.R4, 0), F(CorElementType.I4, 4)])],
            new(true, 4, 0), new(false, 4, 4));

    [Theory]
    [MemberData(nameof(FpArchitectures))]
    public void ByteThenDouble(RuntimeInfoArchitecture arch)
        => AssertClassification(arch,
            [new("S", 16, [F(CorElementType.U1, 0), F(CorElementType.R8, 8)])],
            new(false, 1, 0), new(true, 8, 8));

    [Theory]
    [MemberData(nameof(FpArchitectures))]
    public void LongThenFloat(RuntimeInfoArchitecture arch)
        => AssertClassification(arch,
            [new("S", 16, [F(CorElementType.I8, 0), F(CorElementType.R4, 8)])],
            new(false, 8, 0), new(true, 4, 8));

    [Theory]
    [MemberData(nameof(FpArchitectures))]
    public void ReferenceThenDouble(RuntimeInfoArchitecture arch)
        => AssertClassification(arch,
            [new("S", 16, [F(CorElementType.Class, 0), F(CorElementType.R8, 8)])],
            new(false, 8, 0), new(true, 8, 8));

    [Theory]
    [MemberData(nameof(FpArchitectures))]
    public void DoubleThenReference(RuntimeInfoArchitecture arch)
        => AssertClassification(arch,
            [new("S", 16, [F(CorElementType.R8, 0), F(CorElementType.Class, 8)])],
            new(true, 8, 0), new(false, 8, 8));

    [Theory]
    [MemberData(nameof(FpArchitectures))]
    public void FieldsAreOrderedByOffset(RuntimeInfoArchitecture arch)
        => AssertClassification(arch,
            [new("S", 8, [F(CorElementType.R4, 4), F(CorElementType.I4, 0)])],
            new(false, 4, 0), new(true, 4, 4));

    [Theory]
    [MemberData(nameof(FpArchitectures))]
    public void NestedStructs(RuntimeInfoArchitecture arch)
        => AssertClassification(arch,
            [
                new("Inner", 4, [F(CorElementType.R4, 0)]),
                new("Middle", 4, [Nested("Inner", 0)]),
                new("Outer", 16, [F(CorElementType.I8, 0), Nested("Middle", 8)]),
            ],
            new(false, 8, 0), new(true, 4, 8));

    [Theory]
    [MemberData(nameof(FpArchitectures))]
    public void NestedStructWithTwoFields(RuntimeInfoArchitecture arch)
        => AssertClassification(arch,
            [
                new("Inner", 8, [F(CorElementType.I4, 0), F(CorElementType.R4, 4)]),
                new("Outer", 8, [Nested("Inner", 0)]),
            ],
            new(false, 4, 0), new(true, 4, 4));

    [Theory]
    [MemberData(nameof(FpArchitectures))]
    public void FixedBufferOfTwoFloats(RuntimeInfoArchitecture arch)
        => AssertClassification(arch,
            [new("S", 8, [F(CorElementType.R4, 0)], HasLayout: true)],
            new(true, 4, 0), new(true, 4, 4));

    [Theory]
    [MemberData(nameof(FpArchitectures))]
    public void InlineArrayOfTwoDoubles(RuntimeInfoArchitecture arch)
        => AssertClassification(arch,
            [new("S", 16, [F(CorElementType.R8, 0)], IsInlineArray: true)],
            new(true, 8, 0), new(true, 8, 8));

    [Theory]
    [MemberData(nameof(FpArchitectures))]
    public void InlineArrayOfOneStructElement(RuntimeInfoArchitecture arch)
        => AssertClassification(arch,
            [
                new("Element", 8, [F(CorElementType.R4, 0), F(CorElementType.I4, 4)]),
                new("S", 8, [Nested("Element", 0)], IsInlineArray: true),
            ],
            new(true, 4, 0), new(false, 4, 4));

    [Theory]
    [MemberData(nameof(FpArchitectures))]
    public void OnlyIntegerFields_UsesIntegerConvention(RuntimeInfoArchitecture arch)
        => AssertIntegerCallingConvention(arch, [new("S", 8, [F(CorElementType.I4, 0), F(CorElementType.I4, 4)])]);

    [Theory]
    [MemberData(nameof(FpArchitectures))]
    public void ThreeFields_UsesIntegerConvention(RuntimeInfoArchitecture arch)
        => AssertIntegerCallingConvention(arch,
            [new("S", 12, [F(CorElementType.R4, 0), F(CorElementType.R4, 4), F(CorElementType.R4, 8)])]);

    [Theory]
    [MemberData(nameof(FpArchitectures))]
    public void LargerThanSixteenBytes_UsesIntegerConvention(RuntimeInfoArchitecture arch)
        => AssertIntegerCallingConvention(arch,
            [new("S", 24, [F(CorElementType.R8, 0), F(CorElementType.R8, 8), F(CorElementType.R8, 16)])]);

    [Theory]
    [MemberData(nameof(FpArchitectures))]
    public void OverlappingFields_UsesIntegerConvention(RuntimeInfoArchitecture arch)
        => AssertIntegerCallingConvention(arch, [new("S", 8, [F(CorElementType.R8, 0), F(CorElementType.R4, 0)])]);

    [Theory]
    [MemberData(nameof(FpArchitectures))]
    public void NestedStructWithTooManyFields_UsesIntegerConvention(RuntimeInfoArchitecture arch)
        => AssertIntegerCallingConvention(arch,
            [
                new("Inner", 8, [F(CorElementType.R4, 0), F(CorElementType.R4, 4)]),
                new("Outer", 12, [Nested("Inner", 0), F(CorElementType.R4, 8)]),
            ]);

    [Theory]
    [MemberData(nameof(FpArchitectures))]
    public void FixedBufferOfThreeFloats_UsesIntegerConvention(RuntimeInfoArchitecture arch)
        => AssertIntegerCallingConvention(arch, [new("S", 12, [F(CorElementType.R4, 0)], HasLayout: true)]);

    [Theory]
    [MemberData(nameof(FpArchitectures))]
    public void InlineArrayOfTwoStructsWithTwoFields_UsesIntegerConvention(RuntimeInfoArchitecture arch)
        => AssertIntegerCallingConvention(arch,
            [
                new("Element", 8, [F(CorElementType.R4, 0), F(CorElementType.I4, 4)]),
                new("S", 16, [Nested("Element", 0)], IsInlineArray: true),
            ]);

    [Theory]
    [InlineData(RuntimeInfoArchitecture.X64)]
    [InlineData(RuntimeInfoArchitecture.Arm64)]
    public void OtherArchitectures_UseIntegerConvention(RuntimeInfoArchitecture arch)
        => AssertIntegerCallingConvention(arch, [new("S", 8, [F(CorElementType.R4, 0), F(CorElementType.R4, 4)])]);
}
