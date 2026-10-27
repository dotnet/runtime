// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Moq;
using Xunit;
using FrameType = Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers.FrameType;
using ModuleHandle = Microsoft.Diagnostics.DataContractReader.Contracts.ModuleHandle;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public unsafe class StackWalkTests
{
    private const uint X86TransitionBlockSize = 7 * sizeof(uint);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FindGCRefMap_ResolvesImportRecord(bool inlineGCRefMap)
    {
        (TestPlaceholderTarget target, _) = CreateX86TransitionFrameTarget(
            nameof(Data.ExternalMethodFrame), gcRefMap: [0x83, 0x05], hasMethodDesc: false,
            inlineGCRefMap: inlineGCRefMap);

        TargetPointer map = new FrameHelpers(target).FindGCRefMap(new TargetPointer(0x100108));

        Assert.Equal(inlineGCRefMap ? 0x100200UL : 0x100206UL, map.Value);
    }

    [Theory]
    [InlineData(0x00, 0)]
    [InlineData(0x01, 1)]
    [InlineData(0x02, 2)]
    [InlineData(0x07, 3)]
    [InlineData(0x0f, 4)]
    [InlineData(0x17, 5)]
    [InlineData(0x1f, 6)]
    [InlineData(0x67, 7)]
    public void GCRefMapDecoder_ReadsSingleTokens(byte encoded, int expected)
    {
        GCRefMapDecoder decoder = new([encoded]);
        Assert.Equal((GCRefMapToken)expected, decoder.ReadToken());
    }

    [Theory]
    [InlineData(new byte[] { 0x57 }, false, 5)]
    [InlineData(new byte[] { 0x5f }, false, 6)]
    [InlineData(new byte[] { 0xe7, 0x08 }, false, 7)]
    [InlineData(new byte[] { 0x57 }, true, 7)]
    public void GCRefMapDecoder_ExtendedTokensDoNotConsumeExtraBits(byte[] blob, bool legacyTokens, int expected)
    {
        GCRefMapDecoder decoder = new(blob, legacyTokens: legacyTokens);
        Assert.Equal((GCRefMapToken)expected, decoder.ReadToken());
        Assert.Equal(1, decoder.CurrentPos);
        Assert.Equal(GCRefMapToken.Ref, decoder.ReadToken());
        Assert.Equal(2, decoder.CurrentPos);
        Assert.True(decoder.AtEnd);
    }

    [Theory]
    [InlineData("c1", 7)]
    [InlineData("c2", 5)]
    public void GCRefMapDecoder_TargetTokensUseContractVersion(string version, int expected)
    {
        (TestPlaceholderTarget target, _) = CreateX86TransitionFrameTarget(
            nameof(FrameType.PrestubMethodFrame), gcRefMap: [0x57], hasMethodDesc: true);
        ((TestPlaceholderTarget.TestContractRegistry)target.Contracts).SetVersion<IStackWalk>(version);

        GCRefMapDecoder decoder = new(target, new TargetPointer(0x6200));
        Assert.Equal((GCRefMapToken)expected, decoder.ReadToken());
        Assert.Equal(GCRefMapToken.Ref, decoder.ReadToken());
        Assert.Equal(2, decoder.CurrentPos);
        Assert.True(decoder.AtEnd);
    }

    [Theory]
    [InlineData(0u)]
    [InlineData(1u)]
    [InlineData(2u)]
    [InlineData(3u)]
    [InlineData(10u)]
    [InlineData(11u)]
    [InlineData(138u)]
    [InlineData(139u)]
    [InlineData(32906u)]
    [InlineData(32907u)]
    [InlineData(1000000u)]
    public void DispatchMapDecoder_ReadsIntegerBoundaries(uint value)
    {
        TestPlaceholderTarget.Builder builder = new(new MockTarget.Architecture { IsLittleEndian = true, Is64Bit = false });
        builder.MemoryBuilder.AddHeapFragment(new() { Address = 0x1000, Data = EncodeDispatchMap([value, 2, 139, 0]), Name = "Dispatch integers" });
        DispatchMapDecoder decoder = new(builder.Build(), new TargetPointer(0x1000));
        Assert.Equal(value, decoder.Read());
        Assert.Equal(2u, decoder.Read());
        Assert.Equal(139u, decoder.Read());
        Assert.Equal(0u, decoder.Read());
    }

    [Theory]
    [InlineData(new byte[] { 0x00 }, 0u)]
    [InlineData(new byte[] { 0x80 }, 1u)]
    [InlineData(new byte[] { 0xa0 }, 2u)]
    [InlineData(new byte[] { 0xc0 }, 3u)]
    [InlineData(new byte[] { 0xdc }, 10u)]
    [InlineData(new byte[] { 0xe0, 0x00 }, 11u)]
    [InlineData(new byte[] { 0xef, 0xe0 }, 138u)]
    [InlineData(new byte[] { 0xf0, 0x00, 0x00 }, 139u)]
    [InlineData(new byte[] { 0xf7, 0xff, 0xf0 }, 32906u)]
    [InlineData(new byte[] { 0xf8, 0x00, 0x00, 0x00, 0x00 }, 32907u)]
    public void DispatchMapDecoder_MatchesNativeIntegerEncoding(byte[] encoded, uint expected)
    {
        TestPlaceholderTarget.Builder builder = new(new MockTarget.Architecture { IsLittleEndian = true, Is64Bit = false });
        builder.MemoryBuilder.AddHeapFragment(new() { Address = 0x1000, Data = encoded, Name = "Native DispatchMap integer" });
        DispatchMapDecoder decoder = new(builder.Build(), new TargetPointer(0x1000));
        Assert.Equal(expected, decoder.Read());
    }

    [Fact]
    public void DispatchMapDecoder_RejectsInvalidPrefix()
    {
        TestPlaceholderTarget.Builder builder = new(new MockTarget.Architecture { IsLittleEndian = true, Is64Bit = false });
        builder.MemoryBuilder.AddHeapFragment(new() { Address = 0x1000, Data = [0xfc], Name = "Invalid DispatchMap" });
        DispatchMapDecoder decoder = new(builder.Build(), new TargetPointer(0x1000));
        Assert.Throws<InvalidOperationException>(() => decoder.Read());
    }

    private static byte[] EncodeDispatchMap(uint[] values)
    {
        uint[] bases = [0, 1, 3, 11, 139, 32907];
        List<int> bits = [];
        foreach (uint value in values)
        {
            int ones = 0;
            while (ones + 1 < bases.Length && value >= bases[ones + 1])
                ones++;
            for (int i = 0; i < ones; i++)
                bits.Add(1);
            bits.Add(0);
            uint payload = value - bases[ones];
            for (int i = (1 << ones) - 2; i >= 0; i--)
                bits.Add((int)((payload >> i) & 1));
        }
        byte[] result = new byte[(bits.Count + 7) / 8];
        for (int i = 0; i < bits.Count; i++)
            result[i / 8] |= (byte)(bits[i] << (7 - i % 8));
        return result;
    }

    [Theory]
    [InlineData("declaration")]
    [InlineData("overriding-declaration")]
    [InlineData("same-type-declaration")]
    [InlineData("alias-overriding")]
    [InlineData("alias-target-overriding")]
    [InlineData("alias-chain")]
    [InlineData("alias-target")]
    [InlineData("alias-split")]
    [InlineData("interface")]
    [InlineData("interface-negative")]
    [InlineData("canonical-interface")]
    [InlineData("interface-owner")]
    [InlineData("interface-owner-implementation")]
    [InlineData("interface-owner-ancestor")]
    public void MethodGCRefMapLookup_FindsVirtualDeclaration(string scenario)
    {
        (TestPlaceholderTarget target, VirtualMapTypeSystem rts) = CreateVirtualMapTarget(scenario);
        TargetPointer map = new MethodGCRefMapLookup(target, new FrameHelpers(target)).Find(
            rts.GetMethodDescHandle(new TargetPointer(0x6000)), new TargetPointer(0x7200), dispatch: false);
        Assert.Equal(0xa000UL, map.Value);
    }

    [Theory]
    [InlineData(false, GenericContextLoc.None, 0xb200UL)]
    [InlineData(true, GenericContextLoc.None, 0xa000UL)]
    [InlineData(false, GenericContextLoc.InstArgMethodDesc, 0xa000UL)]
    [InlineData(false, GenericContextLoc.InstArgMethodTable, 0xa000UL)]
    public void MethodGCRefMapLookup_FiltersOverrideMapsByMetadata(bool genericMethod, GenericContextLoc context, ulong expectedMap)
    {
        (TestPlaceholderTarget target, VirtualMapTypeSystem rts) =
            CreateVirtualMapTarget("overriding-fallback", overrideMap: [0xdd, 0x02]);
        if (genericMethod)
            rts.GenericMethods.Add(0x6200);
        rts.MethodContexts[0x6200] = context;
        Assert.Equal(expectedMap, new MethodGCRefMapLookup(target, new FrameHelpers(target)).Find(
            rts.GetMethodDescHandle(new TargetPointer(0x6000)), new TargetPointer(0x7200), dispatch: false).Value);
    }

    [Theory]
    [InlineData(false, 0xb200UL)]
    [InlineData(true, 0xa000UL)]
    public void MethodGCRefMapLookup_PrefersNearestNonUnboxingDeclaration(bool unboxing, ulong expectedMap)
    {
        (TestPlaceholderTarget target, VirtualMapTypeSystem rts) =
            CreateVirtualMapTarget("overriding-fallback", overrideMap: [0xdd, 0x02]);
        if (unboxing)
            rts.UnboxingStubs.Add(0x6200);
        Assert.Equal(expectedMap, new MethodGCRefMapLookup(target, new FrameHelpers(target)).Find(
            rts.GetMethodDescHandle(new TargetPointer(0x6000)), new TargetPointer(0x7200), dispatch: false).Value);
    }

    [Theory]
    [InlineData("nonvirtual-no-inherit")]
    [InlineData("virtual-boundary")]
    [InlineData("interface-parent")]
    public void MethodGCRefMapLookup_DoesNotCrossSlotOrInterfaceBoundary(string scenario)
    {
        (TestPlaceholderTarget target, VirtualMapTypeSystem rts) = CreateVirtualMapTarget(scenario);
        Assert.Equal(TargetPointer.Null, new MethodGCRefMapLookup(target, new FrameHelpers(target)).Find(
            rts.GetMethodDescHandle(new TargetPointer(0x6000)), new TargetPointer(0x7200), dispatch: false));
    }

    [Theory]
    [InlineData("nonvirtual-interface-collision")]
    [InlineData("nonvirtual-alias-collision")]
    public void MethodGCRefMapLookup_DoesNotBorrowForUnrelatedDerivedSlot(string scenario)
    {
        (TestPlaceholderTarget target, VirtualMapTypeSystem rts) = CreateVirtualMapTarget(scenario);
        Assert.Equal(TargetPointer.Null, new MethodGCRefMapLookup(target, new FrameHelpers(target)).Find(
            rts.GetMethodDescHandle(new TargetPointer(0x6000)), new TargetPointer(0x7200), dispatch: false));
    }

    [Theory]
    [InlineData("same", true)]
    [InlineData("thunk", true)]
    [InlineData("token", false)]
    [InlineData("module", false)]
    [InlineData("canonical-type", false)]
    [InlineData("async-variant", false)]
    [InlineData("return-dropping", false)]
    public void MethodGCRefMapLookup_MatchesMethodDefinitionAndCanonicalOwner(string difference, bool matches)
    {
        (TestPlaceholderTarget target, VirtualMapTypeSystem rts) = CreateVirtualMapTarget("interface");
        rts.Methods[0x6500] = (0x7100, 0);
        rts.Vtable[(0x7200, 1)] = 0x6500;
        rts.MethodTokens[0x6000] = 0x06000001;
        rts.MethodTokens[0x6500] = difference == "token" ? 0x06000002u : 0x06000001u;
        if (difference is "module" or "canonical-type")
            rts.Methods[0x6500] = (0x7200, 0);
        if (difference == "module")
        {
            rts.Canonical[0x7200] = 0x7100;
            rts.Modules[0x7200] = 0xc100;
        }
        rts.AsyncFlags[0x6500] = difference switch
        {
            "thunk" => AsyncMethodFlags.Thunk,
            "async-variant" => AsyncMethodFlags.IsAsyncVariant,
            "return-dropping" => AsyncMethodFlags.ReturnDroppingThunk,
            _ => AsyncMethodFlags.None,
        };
        Assert.Equal(matches ? 0xa000UL : 0UL, new MethodGCRefMapLookup(target, new FrameHelpers(target)).Find(
            rts.GetMethodDescHandle(new TargetPointer(0x6000)), new TargetPointer(0x7200), dispatch: false).Value);
    }

    [Theory]
    [InlineData(nameof(FrameType.PrestubMethodFrame))]
    [InlineData(nameof(FrameType.CallCountingHelperFrame))]
    [InlineData(nameof(FrameType.StubDispatchFrame))]
    public void TransitionFrame_ConsumesNativeMemoizedMapWithoutReceiverLookup(string frameType)
    {
        (TestPlaceholderTarget target, VirtualMapTypeSystem rts) = CreateVirtualMapTarget(
            "overriding-declaration", frameType: frameType, unreadableThis: true, ownMap: true);
        rts.Introduced.Clear();
        Assert.Equal(0xa000UL, new FrameHelpers(target).GetGCRefMapForFrame(new TargetPointer(0x1000)).Value);
        Assert.Equal(0xa000UL, target.ReadPointer(new TargetPointer(0x6008)).Value);
    }

    [Theory]
    [InlineData(4u)]
    [InlineData(12u)]
    [InlineData(32u)]
    public void MethodGCRefMapLookup_UsesDescribedAuxiliaryDataSize(uint auxiliarySize)
    {
        (TestPlaceholderTarget target, VirtualMapTypeSystem rts) =
            CreateVirtualMapTarget("canonical-interface", auxiliarySize: auxiliarySize);
        Assert.Equal(0xa000UL, new MethodGCRefMapLookup(target, new FrameHelpers(target)).Find(
            rts.GetMethodDescHandle(new TargetPointer(0x6000)), new TargetPointer(0x7200), dispatch: false).Value);
    }

    [Theory]
    [InlineData(false, GenericContextLoc.None, true)]
    [InlineData(true, GenericContextLoc.None, false)]
    [InlineData(false, GenericContextLoc.InstArgMethodDesc, false)]
    [InlineData(false, GenericContextLoc.InstArgMethodTable, false)]
    public void MethodGCRefMapLookup_FiltersBorrowedMapsByMetadata(bool genericMethod, GenericContextLoc context, bool expected)
    {
        (TestPlaceholderTarget target, VirtualMapTypeSystem rts) = CreateVirtualMapTarget("alias-chain");
        if (genericMethod)
            rts.GenericMethods.Add(0x6100);
        rts.MethodContexts[0x6100] = context;
        Assert.Equal(expected ? 0xa000UL : 0UL, new MethodGCRefMapLookup(target, new FrameHelpers(target)).Find(
            rts.GetMethodDescHandle(new TargetPointer(0x6000)), new TargetPointer(0x7200), dispatch: false).Value);
    }

    [Theory]
    [InlineData("alias-chain", 0xa000UL)]
    [InlineData("interface", 0xa000UL)]
    [InlineData("overriding-fallback", 0xb200UL)]
    public void MethodGCRefMapLookup_DoesNotReadMapContents(string scenario, ulong expectedMap)
    {
        // Only the first byte is readable: lookup must not decode the map to validate its format.
        (TestPlaceholderTarget target, VirtualMapTypeSystem rts) =
            CreateVirtualMapTarget(scenario, map: [0xfd], overrideMap: [0xfd]);
        Assert.Equal(expectedMap, new MethodGCRefMapLookup(target, new FrameHelpers(target)).Find(
            rts.GetMethodDescHandle(new TargetPointer(0x6000)), new TargetPointer(0x7200), dispatch: false).Value);
    }

    [Theory]
    [InlineData(false, false, GenericContextLoc.None, true)]
    [InlineData(false, false, GenericContextLoc.InstArgMethodDesc, true)]
    [InlineData(false, false, GenericContextLoc.InstArgMethodTable, true)]
    [InlineData(false, true, GenericContextLoc.None, true)]
    [InlineData(false, true, GenericContextLoc.InstArgMethodDesc, true)]
    [InlineData(false, true, GenericContextLoc.InstArgMethodTable, true)]
    [InlineData(true, false, GenericContextLoc.None, true)]
    [InlineData(true, false, GenericContextLoc.InstArgMethodDesc, false)]
    [InlineData(true, false, GenericContextLoc.InstArgMethodTable, false)]
    [InlineData(true, true, GenericContextLoc.None, false)]
    [InlineData(true, true, GenericContextLoc.InstArgMethodDesc, false)]
    [InlineData(true, true, GenericContextLoc.InstArgMethodTable, false)]
    public void MethodGCRefMapLookup_UsesExactEntryMapOnlyForCompatibleFrames(
        bool dispatch, bool genericMethod, GenericContextLoc context, bool expected)
    {
        (TestPlaceholderTarget target, VirtualMapTypeSystem rts) = CreateVirtualMapTarget("declaration", ownMap: true);
        rts.Introduced.Clear();
        if (genericMethod)
            rts.GenericMethods.Add(0x6000);
        rts.MethodContexts[0x6000] = context;
        TargetPointer result = new MethodGCRefMapLookup(target, new FrameHelpers(target)).Find(
            rts.GetMethodDescHandle(new TargetPointer(0x6000)), TargetPointer.Null, dispatch);
        Assert.Equal(expected ? 0xa000UL : 0UL, result.Value);
    }

    [Theory]
    [InlineData(GenericContextLoc.InstArgMethodDesc)]
    [InlineData(GenericContextLoc.InstArgMethodTable)]
    public void MethodGCRefMapLookup_DispatchBorrowsInsteadOfUsingExactContextBodyMap(GenericContextLoc context)
    {
        (TestPlaceholderTarget target, VirtualMapTypeSystem rts) =
            CreateVirtualMapTarget("overriding-fallback", ownMap: true, overrideMap: [0xdd, 0x02]);
        rts.MethodContexts[0x6000] = context;
        TargetPointer result = new MethodGCRefMapLookup(target, new FrameHelpers(target)).Find(
            rts.GetMethodDescHandle(new TargetPointer(0x6000)), TargetPointer.Null, dispatch: true);
        Assert.Equal(0xb200UL, result.Value);
    }

    [Theory]
    [InlineData(false, GenericContextLoc.None, true)]
    [InlineData(false, GenericContextLoc.InstArgMethodDesc, false)]
    [InlineData(false, GenericContextLoc.InstArgMethodTable, false)]
    [InlineData(true, GenericContextLoc.InstArgMethodTable, true)]
    public void MethodGCRefMapLookup_ExcludesHiddenBodyArguments(bool dispatch, GenericContextLoc context, bool expected)
    {
        (TestPlaceholderTarget target, VirtualMapTypeSystem rts) = CreateVirtualMapTarget("declaration");
        rts.MethodContexts[0x6000] = context;
        MethodGCRefMapLookup lookup = new(target, new FrameHelpers(target));
        TargetPointer result = lookup.Find(rts.GetMethodDescHandle(new TargetPointer(0x6000)), new TargetPointer(0x7200), dispatch);
        Assert.Equal(expected ? 0xa000UL : 0UL, result.Value);
        rts.GenericMethods.Add(0x6000);
        Assert.Equal(TargetPointer.Null, lookup.Find(
            rts.GetMethodDescHandle(new TargetPointer(0x6000)), new TargetPointer(0x7200), dispatch));
    }

    [Theory]
    [InlineData("alias-chain", nameof(FrameType.PrestubMethodFrame))]
    [InlineData("overriding-declaration", nameof(FrameType.PrestubMethodFrame))]
    [InlineData("alias-overriding", nameof(FrameType.CallCountingHelperFrame))]
    [InlineData("alias-target-overriding", nameof(FrameType.StubDispatchFrame))]
    [InlineData("interface", nameof(FrameType.CallCountingHelperFrame))]
    [InlineData("interface-owner", nameof(FrameType.StubDispatchFrame))]
    [InlineData("interface-owner-implementation", nameof(FrameType.StubDispatchFrame))]
    [InlineData("interface-owner-ancestor", nameof(FrameType.StubDispatchFrame))]
    public void TransitionFrame_VirtualMapScanningAndX86Unwind(string scenario, string frameType)
    {
        (TestPlaceholderTarget target, _) = CreateVirtualMapTarget(scenario, frameType: frameType);
        GcScanContext scan = new(target, resolveInteriorPointers: false);
        scan.UpdateScanContext(new TargetPointer(0x2000), TargetCodePointer.Null, new TargetPointer(0x1000));
        new GcScanner(target).GcScanRoots(new TargetPointer(0x1000), scan);
        Assert.Equal(new ulong[] { 0x2014, 0x2010 }, scan.StackRefs.Select(reference => reference.Address.Value));
        Assert.All(scan.StackRefs, reference => Assert.Equal(GcScanFlags.None, reference.Flags));

        ContextHolder<X86Context> context = new() { Context = new X86Context { ContextFlags = (uint)X86Context.ContextFlagsValues.CONTEXT_FULL } };
        new X86FrameHandler(target, context).HandleTransitionFrame(
            target.ProcessedData.GetOrAdd<Data.FramedMethodFrame>(new TargetPointer(0x1000)));
        AssertX86TransitionFrameContext(context, stackPopSlots: 1);
    }

    [Theory]
    [InlineData(".ctor", 0, false, true)]
    [InlineData(".ctor", 0, true, false)]
    [InlineData(".ctor", 4, false, false)]
    [InlineData("Method", 0, true, true)]
    [InlineData("Method", 0, false, true)]
    [InlineData("FastAllocateString", 0x80, true, false)]
    public void TransitionFrame_ReceiverEligibilityDistinguishesStringAndArrayConstructors(
        string name, ushort methodFlags, bool stringOwner, bool hasReceiver)
    {
        MetadataBuilder metadata = new();
        metadata.AddModule(0, metadata.GetOrAddString("M"), default, default, default);
        metadata.AddTypeDefinition(default, default, metadata.GetOrAddString("<Module>"), default,
            MetadataTokens.FieldDefinitionHandle(1), MetadataTokens.MethodDefinitionHandle(1));
        metadata.AddMethodDefinition(MethodAttributes.Public | MethodAttributes.RTSpecialName, default,
            metadata.GetOrAddString(name), default, 0, MetadataTokens.ParameterHandle(1));
        BlobBuilder image = new();
        new MetadataRootBuilder(metadata).Serialize(image, 0, 0);
        using MetadataReaderProvider provider = MetadataReaderProvider.FromMetadataImage(ImmutableArray.Create(image.ToArray()));
        (TestPlaceholderTarget target, VirtualMapTypeSystem rts) = CreateVirtualMapTarget(
            "interface", metadata: provider.GetMetadataReader(), unreadableThis: !hasReceiver, methodFlags: methodFlags);
        if (stringOwner)
            rts.StringTypes.Add(rts.Methods[0x6000].Owner);
        Assert.Equal(hasReceiver ? 0xa000UL : 0UL,
            new FrameHelpers(target).GetGCRefMapForFrame(new TargetPointer(0x1000)).Value);
    }

    [Fact]
    public void TransitionFrame_DeclarationMapDoesNotReadReceiver()
    {
        (TestPlaceholderTarget target, _) = CreateVirtualMapTarget("declaration", unreadableThis: true);
        Assert.Equal(0xa000UL, new FrameHelpers(target).GetGCRefMapForFrame(new TargetPointer(0x1000)).Value);
    }

    [Theory]
    [InlineData("declaration", nameof(FrameType.PrestubMethodFrame))]
    [InlineData("declaration", nameof(FrameType.CallCountingHelperFrame))]
    [InlineData("declaration", nameof(FrameType.StubDispatchFrame))]
    [InlineData("interface", nameof(FrameType.PrestubMethodFrame))]
    [InlineData("interface", nameof(FrameType.CallCountingHelperFrame))]
    [InlineData("interface", nameof(FrameType.StubDispatchFrame))]
    public void TransitionFrame_X86UnwindUsesMetadataOrCachedMiss(string scenario, string frameType)
    {
        byte[] signatureMap = [0xdd, 0x02];
        (TestPlaceholderTarget target, _) = CreateVirtualMapTarget(
            scenario, frameType: frameType, unreadableThis: true, signatureMap: signatureMap,
            cachedFrameMap: scenario == "interface" ? uint.MaxValue : 0);
        ContextHolder<X86Context> context = new() { Context = new X86Context { ContextFlags = (uint)X86Context.ContextFlagsValues.CONTEXT_FULL } };

        new X86FrameHandler(target, context).HandleTransitionFrame(
            target.ProcessedData.GetOrAdd<Data.FramedMethodFrame>(new TargetPointer(0x1000)));

        AssertX86TransitionFrameContext(context, stackPopSlots: 1);
        Mock.Get(target.Contracts.CallingConvention).Verify(
            callingConvention => callingConvention.TryComputeArgGCRefMapBlob(It.IsAny<MethodDescHandle>(), out signatureMap),
            Times.Exactly(scenario == "interface" ? 1 : 0));
    }

    [Theory]
    [InlineData(nameof(FrameType.PrestubMethodFrame))]
    [InlineData(nameof(FrameType.CallCountingHelperFrame))]
    [InlineData(nameof(FrameType.StubDispatchFrame))]
    public void TransitionFrame_X86UnwindFindsReceiverMapBeforeFirstGC(string frameType)
    {
        byte[] signatureMap = [];
        (TestPlaceholderTarget target, _) = CreateVirtualMapTarget(
            "interface", frameType: frameType, signatureMap: signatureMap);
        Mock<ICallingConvention> callingConvention = Mock.Get(target.Contracts.CallingConvention);
        callingConvention.Setup(c => c.TryComputeArgGCRefMapBlob(It.IsAny<MethodDescHandle>(), out signatureMap)).Returns(false);
        ContextHolder<X86Context> context = new() { Context = new X86Context { ContextFlags = (uint)X86Context.ContextFlagsValues.CONTEXT_FULL } };

        new X86FrameHandler(target, context).HandleTransitionFrame(
            target.ProcessedData.GetOrAdd<Data.FramedMethodFrame>(new TargetPointer(0x1000)));

        AssertX86TransitionFrameContext(context, stackPopSlots: 1);
        callingConvention.Verify(
            c => c.TryComputeArgGCRefMapBlob(It.IsAny<MethodDescHandle>(), out signatureMap), Times.Never);
    }

    [Theory]
    [InlineData(nameof(FrameType.PrestubMethodFrame), true)]
    [InlineData(nameof(FrameType.CallCountingHelperFrame), true)]
    [InlineData(nameof(FrameType.StubDispatchFrame), true)]
    [InlineData(nameof(FrameType.PrestubMethodFrame), false)]
    [InlineData(nameof(FrameType.CallCountingHelperFrame), false)]
    [InlineData(nameof(FrameType.StubDispatchFrame), false)]
    public void TransitionFrame_CachedLookupDoesNotReadRelocatedReceiver(string frameType, bool hasMap)
    {
        byte[] signatureMap = [0xdd, 0x02];
        ulong cachedMap = hasMap ? 0xa000UL : uint.MaxValue;
        (TestPlaceholderTarget target, _) = CreateVirtualMapTarget(
            "interface", frameType: frameType, unreadableThis: true, signatureMap: signatureMap,
            cachedFrameMap: cachedMap);
        FrameHelpers helpers = new(target);

        Assert.Equal(hasMap ? cachedMap : 0, helpers.GetGCRefMapForFrame(new TargetPointer(0x1000)).Value);

        ContextHolder<X86Context> context = new() { Context = new X86Context { ContextFlags = (uint)X86Context.ContextFlagsValues.CONTEXT_FULL } };
        new X86FrameHandler(target, context).HandleTransitionFrame(
            target.ProcessedData.GetOrAdd<Data.FramedMethodFrame>(new TargetPointer(0x1000)));
        AssertX86TransitionFrameContext(context, stackPopSlots: 1);

        if (hasMap)
        {
            GcScanContext scan = new(target, resolveInteriorPointers: false);
            scan.UpdateScanContext(new TargetPointer(0x2000), TargetCodePointer.Null, new TargetPointer(0x1000));
            new GcScanner(target).GcScanRoots(new TargetPointer(0x1000), scan);
            Assert.Equal(new ulong[] { 0x2014, 0x2010 }, scan.StackRefs.Select(reference => reference.Address.Value));
        }

        Mock.Get(target.Contracts.CallingConvention).Verify(
            callingConvention => callingConvention.TryComputeArgGCRefMapBlob(It.IsAny<MethodDescHandle>(), out signatureMap),
            Times.Exactly(hasMap ? 0 : 1));
    }

    [Theory]
    [InlineData(nameof(FrameType.PrestubMethodFrame))]
    [InlineData(nameof(FrameType.CallCountingHelperFrame))]
    [InlineData(nameof(FrameType.StubDispatchFrame))]
    public void TransitionFrame_UninitializedCacheDoesNotHideLookup(string frameType)
    {
        (TestPlaceholderTarget target, _) = CreateVirtualMapTarget(
            "interface", frameType: frameType);

        Assert.Equal(0xa000UL,
            new FrameHelpers(target).GetGCRefMapForFrame(new TargetPointer(0x1000)).Value);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void TransitionFrame_CachedLookupUsesTargetPointerSize(MockTarget.Architecture arch)
    {
        ulong hit = arch.Is64Bit ? 0x1_0000_a000UL : 0xa000UL;
        ulong miss = arch.Is64Bit ? ulong.MaxValue : uint.MaxValue;
        foreach (ulong cachedMap in new[] { hit, miss })
        {
            TestPlaceholderTarget.Builder builder = new(arch);
            TargetTestHelpers helpers = builder.MemoryBuilder.TargetTestHelpers;
            int pointerSize = arch.Is64Bit ? sizeof(ulong) : sizeof(uint);
            byte[] frame = new byte[4 * pointerSize];
            helpers.WritePointer(frame, 0x100);
            helpers.WritePointer(frame.AsSpan(2 * pointerSize), 0x6000);
            helpers.WritePointer(frame.AsSpan(3 * pointerSize), cachedMap);
            builder.MemoryBuilder.AddHeapFragment(new() { Address = 0x1000, Data = frame, Name = "Cached frame map" });
            TestPlaceholderTarget target = builder.AddTypes(new Dictionary<DataType, Target.TypeInfo>
                {
                    [DataType.Frame] = new()
                    {
                        Fields = new Dictionary<string, Target.FieldInfo>
                        {
                            [nameof(Data.Frame.Identifier)] = new() { Offset = 0 },
                        },
                    },
                    [DataType.FramedMethodFrame] = new()
                    {
                        Fields = new Dictionary<string, Target.FieldInfo>
                        {
                            [nameof(Data.FramedMethodFrame.TransitionBlockPtr)] = new() { Offset = pointerSize },
                            [nameof(Data.FramedMethodFrame.MethodDescPtr)] = new() { Offset = 2 * pointerSize },
                            [nameof(Data.FramedMethodFrame.CachedGCRefMap)] = new() { Offset = 3 * pointerSize },
                        },
                    },
                    [DataType.MethodDesc] = new(),
                })
                .AddGlobals((nameof(FrameType.PrestubMethodFrame) + "Identifier", 0x100))
                .AddMockContract(Mock.Of<IGC>())
                .AddMockContract(Mock.Of<IGCInfo>())
                .AddMockContract(Mock.Of<IExecutionManager>())
                .Build();

            Assert.Equal(cachedMap == miss ? 0 : cachedMap,
                new FrameHelpers(target).GetGCRefMapForFrame(new TargetPointer(0x1000)).Value);
        }
    }

    [Theory]
    [InlineData("c1", false)]
    [InlineData("c2", true)]
    public void TransitionFrame_VirtualLookupUsesContractVersion(string version, bool virtualLookup)
    {
        (TestPlaceholderTarget target, _) = CreateVirtualMapTarget("declaration", unreadableThis: true);
        ((TestPlaceholderTarget.TestContractRegistry)target.Contracts).SetVersion<IStackWalk>(version);
        Assert.Equal(virtualLookup ? 0xa000UL : 0UL,
            new FrameHelpers(target).GetGCRefMapForFrame(new TargetPointer(0x1000)).Value);
    }

    [Theory]
    [InlineData("c1", false)]
    [InlineData("c2", true)]
    public void StubDispatchFrame_CachedMethodMapUsesContractVersion(string version, bool methodMap)
    {
        (TestPlaceholderTarget target, _) = CreateVirtualMapTarget(
            "interface", map: [0xdd, 0x02],
            frameType: nameof(FrameType.StubDispatchFrame), unreadableThis: true, ownMap: true);
        ((TestPlaceholderTarget.TestContractRegistry)target.Contracts).SetVersion<IStackWalk>(version);
        Assert.Equal(methodMap ? 0xa000UL : 0UL,
            new FrameHelpers(target).GetGCRefMapForFrame(new TargetPointer(0x1000)).Value);
    }

    [Theory]
    [InlineData("alias-chain")]
    [InlineData("interface-owner")]
    public void MethodGCRefMapLookup_IgnoresMissingVtableMethods(string scenario)
    {
        (TestPlaceholderTarget target, VirtualMapTypeSystem rts) = CreateVirtualMapTarget(scenario);
        rts.Vtable.Clear();
        Assert.Equal(TargetPointer.Null, new MethodGCRefMapLookup(target, new FrameHelpers(target)).Find(
            rts.GetMethodDescHandle(new TargetPointer(0x6000)), new TargetPointer(0x7200), dispatch: false));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void TransitionFrame_MissingMetadataOnlyPreventsStringReceiverLookup(bool stringOwner, bool ownMap)
    {
        (TestPlaceholderTarget target, VirtualMapTypeSystem rts) = CreateVirtualMapTarget(
            "interface", metadataMissing: true, unreadableThis: stringOwner, ownMap: ownMap);
        if (stringOwner)
            rts.StringTypes.Add(rts.Methods[0x6000].Owner);
        Assert.Equal(!stringOwner || ownMap ? 0xa000UL : 0UL,
            new FrameHelpers(target).GetGCRefMapForFrame(new TargetPointer(0x1000)).Value);
    }

    [Theory]
    [InlineData(ArrayFunctionType.Get)]
    [InlineData(ArrayFunctionType.Set)]
    [InlineData(ArrayFunctionType.Address)]
    public void TransitionFrame_ArrayAccessorsHaveReceiver(ArrayFunctionType function)
    {
        (TestPlaceholderTarget target, VirtualMapTypeSystem rts) = CreateVirtualMapTarget("interface", methodFlags: 4);
        rts.ArrayFunction = function;
        Assert.Equal(0xa000UL, new FrameHelpers(target).GetGCRefMapForFrame(new TargetPointer(0x1000)).Value);
    }

    [Fact]
    public void TransitionFrame_MasksGCMarkedReceiverMethodTable()
    {
        (TestPlaceholderTarget target, _) = CreateVirtualMapTarget("interface", markedReceiver: true);
        Assert.Equal(0xa000UL, new FrameHelpers(target).GetGCRefMapForFrame(new TargetPointer(0x1000)).Value);
    }

    [Theory]
    [InlineData(0x80, false, false)]
    [InlineData(4, false, false)]
    [InlineData(0, true, false)]
    [InlineData(0, false, true)]
    public void TransitionFrame_ExcludedBodiesDoNotReadReceiver(ushort flags, bool generic, bool instantiationArgument)
    {
        (TestPlaceholderTarget target, VirtualMapTypeSystem rts) =
            CreateVirtualMapTarget("interface", unreadableThis: true, methodFlags: flags);
        if (generic)
            rts.GenericMethods.Add(0x6000);
        rts.MethodContexts[0x6000] = instantiationArgument ? GenericContextLoc.InstArgMethodTable : GenericContextLoc.None;
        Assert.Equal(TargetPointer.Null, new FrameHelpers(target).GetGCRefMapForFrame(new TargetPointer(0x1000)));
    }

    private sealed record VirtualMapTypeHandle(TargetPointer Address) : ITypeHandle;

    private sealed class VirtualMapTypeSystem : IRuntimeTypeSystem
    {
        internal readonly Dictionary<ulong, (ulong Owner, ushort Slot)> Methods = [];
        internal readonly Dictionary<ulong, TargetPointer[]> Introduced = [];
        internal readonly Dictionary<(ulong Owner, ushort Slot), ulong> Vtable = [];
        internal readonly Dictionary<ulong, ulong> Canonical = [];
        internal readonly HashSet<ulong> UnboxingStubs = [];
        internal readonly HashSet<ulong> GenericMethods = [];
        internal readonly HashSet<ulong> StringTypes = [];
        internal readonly Dictionary<ulong, GenericContextLoc> MethodContexts = [];
        internal readonly Dictionary<ulong, uint> MethodTokens = [];
        internal readonly Dictionary<ulong, ulong> Modules = [];
        internal readonly Dictionary<ulong, AsyncMethodFlags> AsyncFlags = [];
        internal uint MethodToken = 0x06000000;
        internal ArrayFunctionType ArrayFunction = ArrayFunctionType.Constructor;
        public ITypeHandle GetTypeHandle(TargetPointer address) => new VirtualMapTypeHandle(address);
        public MethodDescHandle GetMethodDescHandle(TargetPointer address) => new(address);
        public TargetPointer GetMethodTable(MethodDescHandle method) => new(Methods[method.Address.Value].Owner);
        public ushort GetSlotNumber(MethodDescHandle method) => Methods[method.Address.Value].Slot;
        public bool IsUnboxingStub(MethodDescHandle method) => UnboxingStubs.Contains(method.Address.Value);
        public bool IsValueType(ITypeHandle type) => false;
        public bool IsString(ITypeHandle type) => StringTypes.Contains(type.Address.Value);
        public uint GetMethodToken(MethodDescHandle method) => MethodTokens.GetValueOrDefault(method.Address.Value, MethodToken);
        public TargetPointer GetModule(ITypeHandle type) => new(Modules.GetValueOrDefault(type.Address.Value, 0xc000UL));
        public AsyncMethodFlags GetAsyncMethodFlags(MethodDescHandle method) => AsyncFlags.GetValueOrDefault(method.Address.Value);
        public bool IsArrayMethod(MethodDescHandle method, out ArrayFunctionType function)
        {
            function = ArrayFunction;
            return true;
        }
        public GenericContextLoc GetGenericContextLoc(MethodDescHandle method) => MethodContexts.GetValueOrDefault(method.Address.Value);
        public ReadOnlySpan<ITypeHandle> GetGenericMethodInstantiation(MethodDescHandle method)
            => GenericMethods.Contains(method.Address.Value) ? new ITypeHandle[] { new VirtualMapTypeHandle(new TargetPointer(0x7000)) } : [];
        public IEnumerable<TargetPointer> GetIntroducedMethodDescs(ITypeHandle type)
            => Introduced.GetValueOrDefault(type.Address.Value) ?? [];
        public TargetPointer GetMethodDescForSlot(ITypeHandle type, ushort slot) => new(Vtable.GetValueOrDefault((type.Address.Value, slot)));
        public TargetPointer GetCanonicalMethodTable(ITypeHandle type) => new(Canonical.GetValueOrDefault(type.Address.Value, type.Address.Value));
    }

    private static (TestPlaceholderTarget Target, VirtualMapTypeSystem RuntimeTypeSystem) CreateVirtualMapTarget(
        string scenario, byte[]? map = null, string frameType = nameof(FrameType.PrestubMethodFrame),
        MetadataReader? metadata = null, bool unreadableThis = false, bool metadataMissing = false, ushort methodFlags = 0,
        bool markedReceiver = false, bool ownMap = false, uint auxiliarySize = 4, byte[]? overrideMap = null,
        byte[]? signatureMap = null, ulong cachedFrameMap = 0)
    {
        TestPlaceholderTarget.Builder builder = new(new MockTarget.Architecture { IsLittleEndian = true, Is64Bit = false });
        TargetTestHelpers helpers = builder.MemoryBuilder.TargetTestHelpers;
        VirtualMapTypeSystem rts = new();
        if (signatureMap is not null)
        {
            Mock<ICallingConvention> callingConvention = new(MockBehavior.Strict);
            callingConvention.Setup(c => c.TryComputeArgGCRefMapBlob(It.IsAny<MethodDescHandle>(), out signatureMap)).Returns(true);
            builder.AddMockContract(callingConvention);
        }
        if (metadata is not null || metadataMissing)
        {
            rts.MethodToken = 0x06000001;
            ModuleHandle module = new(new TargetPointer(0xc000));
            Mock<ILoader> loader = new(MockBehavior.Strict);
            loader.Setup(l => l.GetModuleHandleFromModulePtr(module.Address)).Returns(module);
            Mock<IEcmaMetadata> ecma = new(MockBehavior.Strict);
            ecma.Setup(e => e.GetMetadata(module)).Returns(metadata);
            builder.AddMockContract(loader).AddMockContract(ecma);
        }
        Dictionary<DataType, Target.TypeInfo> types = new()
        {
            [DataType.Array] = TypeInfo((nameof(Data.Array.NumComponents), 4)),
            [DataType.String] = TypeInfo((nameof(Data.String.StringLength), 4)),
            [DataType.MethodDesc] = TypeInfo((nameof(Data.MethodDesc.CodeData), 0), (nameof(Data.MethodDesc.Flags), 4)),
            [DataType.MethodDescCodeData] = TypeInfo((nameof(Data.MethodDescCodeData.GCRefMap), 0)),
            [DataType.MethodTable] = TypeInfo(
                (nameof(Data.MethodTable.MTFlags), 0), (nameof(Data.MethodTable.MTFlags2), 4),
                (nameof(Data.MethodTable.ParentMethodTable), 8), (nameof(Data.MethodTable.NumVirtuals), 12),
                (nameof(Data.MethodTable.NumInterfaces), 14), (nameof(Data.MethodTable.AuxiliaryData), 16),
                (nameof(Data.MethodTable.InterfaceMap), 20)),
            [DataType.MethodTableAuxiliaryData] = new() { Size = auxiliarySize },
            [DataType.Frame] = TypeInfo((nameof(Data.Frame.Identifier), 0)),
            [DataType.FramedMethodFrame] = TypeInfo(
                (nameof(Data.FramedMethodFrame.TransitionBlockPtr), 8), (nameof(Data.FramedMethodFrame.MethodDescPtr), 12),
                (nameof(Data.FramedMethodFrame.CachedGCRefMap), 20)),
            [DataType.StubDispatchFrame] = TypeInfo(
                (nameof(Data.StubDispatchFrame.MethodDescPtr), 12), (nameof(Data.StubDispatchFrame.Indirection), 16)),
            [DataType.TransitionBlock] = TypeInfo(
                (nameof(Data.TransitionBlock.CalleeSavedRegisters), 0), (nameof(Data.TransitionBlock.ReturnAddress), 24),
                (nameof(Data.TransitionBlock.ArgumentRegisters), 16)) with { Size = X86TransitionBlockSize },
            [DataType.CalleeSavedRegisters] = TypeInfo(
                (nameof(X86Context.Ebp), 0), (nameof(X86Context.Ebx), 4), (nameof(X86Context.Edi), 8), (nameof(X86Context.Esi), 12)),
            [DataType.Object] = TypeInfo((nameof(Data.Object.MethodTable), 0)),
        };
        AddMemory(0xa000, map ?? [0xdd, 0x02]);
        if (overrideMap is not null)
            AddMemory(0xb200, overrideMap);
        byte[] frame = new byte[24];
        helpers.Write(frame, 0x100u);
        helpers.Write(frame.AsSpan(8), 0x2000u);
        helpers.Write(frame.AsSpan(12), 0x6000u);
        helpers.Write(frame.AsSpan(20), checked((uint)cachedFrameMap));
        AddMemory(0x1000, frame);
        byte[] transition = new byte[X86TransitionBlockSize];
        uint[] saved = [0x1111_1111, 0x2222_2222, 0x3333_3333, 0x4444_4444, 0xb100, 0xb000, 0x1234_5678];
        if (unreadableThis)
            saved[5] = 0xdeadbeef;
        for (int i = 0; i < saved.Length; i++)
            helpers.Write(transition.AsSpan(i * sizeof(uint)), saved[i]);
        AddMemory(0x2000, transition);
        byte[] receiver = new byte[4];
        helpers.Write(receiver, markedReceiver ? 0x7201u : 0u);
        AddMemory(0xb000, receiver);
        AddMemory(0xb100, new byte[4]);

        ushort bodySlot = scenario == "nonvirtual-no-inherit" ? (ushort)4 :
            scenario is "virtual-boundary" or "nonvirtual-interface-collision" or "nonvirtual-alias-collision" ||
                scenario.StartsWith("alias", StringComparison.Ordinal) ? (ushort)2 : (ushort)0;
        bool interfaceOwner = scenario.StartsWith("interface-owner", StringComparison.Ordinal);
        bool resolvedInterface = scenario is "interface-owner-implementation" or "interface-owner-ancestor";
        bool overriding = scenario.Contains("overriding", StringComparison.Ordinal);
        AddMethod(0x6000, interfaceOwner ? 0x8000u :
            scenario.Contains("interface", StringComparison.Ordinal) || scenario == "nonvirtual-alias-collision" ? 0x7100u : 0x7200u, bodySlot, cached: ownMap);
        AddMethod(0x6100, 0x7000, 0, cached: !scenario.Contains("interface", StringComparison.Ordinal) &&
            scenario != "same-type-declaration" && (!overriding || scenario == "overriding-fallback"));
        ushort parentSlot = scenario == "nonvirtual-no-inherit" ? (ushort)4 :
            scenario == "virtual-boundary" ? (ushort)2 : overriding || scenario == "interface-owner-ancestor" ? (ushort)0 : (ushort)1;
        AddMethod(0x6200, 0x7100, parentSlot,
            cached: overriding || scenario is "nonvirtual-no-inherit" or "virtual-boundary" or "interface-owner-ancestor");
        AddMethod(0x6300, 0x8000, 0, cached: !interfaceOwner && scenario != "interface-parent");
        AddMethod(0x6400, 0x8100, scenario == "interface-negative" ? (ushort)1 : (ushort)0, cached: !resolvedInterface);
        ushort implementationSlot = scenario is "nonvirtual-interface-collision" or "nonvirtual-alias-collision" ? (ushort)2 :
            scenario == "interface-owner-ancestor" ? (ushort)0 : (ushort)3;
        AddMethod(0x6500, 0x7200, implementationSlot, cached: scenario == "interface-owner-implementation");
        if (scenario == "same-type-declaration")
            AddMethod(0x6600, 0x7200, 0, cached: true);
        AddType(0x7000, 0, 1);
        AddType(0x7100, 0x7000, 2,
            scenario is "alias-chain" or "alias-overriding" ? [1, 1, 0, 1] : null);
        uint[]? derivedMap = scenario switch
        {
            "alias-chain" or "alias-overriding" => [1, 1, 1, 2],
            "alias-target" or "alias-target-overriding" => [1, 1, 1, 0],
            "alias-split" => [1, 1, 0, 2],
            "nonvirtual-alias-collision" => [1, 1, 0, 2],
            "interface" or "canonical-interface" or "interface-parent" => [2, 1, 1, 0, 0, 1],
            "nonvirtual-interface-collision" or "interface-owner-implementation" or "interface-owner-ancestor" => [2, 1, 1, 0, 0, 2],
            "interface-negative" => [2, 1, 2, 1, 0, 3, 0, 6],
            "interface-owner" => [4, 1, 1, 0, 0, 2, 0, 1, 0, 0, 1],
            _ => null,
        };
        AddType(0x7200, 0x7100, 4, scenario == "canonical-interface" ? null : derivedMap, interfaces: 2);
        if (scenario == "canonical-interface")
        {
            AddType(0x7400, 0x7100, 4, derivedMap, interfaces: 2);
            rts.Canonical[0x7200] = 0x7400;
        }
        AddType(0x8000, scenario == "interface-parent" ? 0x8100u : 0u, 1, isInterface: true);
        AddType(0x8100, 0, 2, isInterface: true);
        byte[] interfaces = new byte[8];
        helpers.Write(interfaces, scenario == "interface-negative" ? 0x8100u : 0x8000u);
        helpers.Write(interfaces.AsSpan(4), 0x8100u);
        AddMemory(0x9000, interfaces);

        for (ushort slot = 0; slot < 4; slot++)
            rts.Vtable[(0x7200, slot)] = 0x6000;
        if (scenario == "interface-negative")
            rts.Vtable[(0x7200, 2)] = 0x6200;
        if (scenario is "nonvirtual-interface-collision" or "nonvirtual-alias-collision")
        {
            rts.Vtable[(0x7200, 0)] = 0x6500;
            rts.Vtable[(0x7200, 2)] = 0x6500;
        }
        if (scenario == "alias-split")
            rts.Vtable[(0x7200, 0)] = 0x6100;
        if (interfaceOwner)
        {
            rts.Vtable[(0x7200, 1)] = 0x6500;
            rts.Vtable[(0x7200, 2)] = 0x6500;
        }
        Mock<IObject> objects = new(MockBehavior.Strict);
        objects.Setup(o => o.GetMethodTableAddress(new TargetPointer(0xb000))).Returns(new TargetPointer(0x7200));
        if (markedReceiver)
        {
            builder.AddContract<IObject>("c1").AddGlobals(
                (Constants.Globals.SyncBlockIsHashOrSyncBlockIndex, 0), (Constants.Globals.SyncBlockIsHashCode, 0),
                (Constants.Globals.SyncBlockHashCodeMask, 0), (Constants.Globals.SyncBlockIndexMask, 0));
        }
        else
        {
            builder.AddMockContract(objects);
        }
        Mock<IRuntimeInfo> runtimeInfo = new(MockBehavior.Strict);
        runtimeInfo.Setup(r => r.GetTargetArchitecture()).Returns(RuntimeInfoArchitecture.X86);
        return (builder.AddTypes(types).AddGlobals((frameType + "Identifier", 0x100), (Constants.Globals.ObjectToMethodTableUnmask, markedReceiver ? 3UL : 0UL))
            .AddMockContract<IRuntimeTypeSystem>(rts).AddMockContract(runtimeInfo)
            .AddMockContract(Mock.Of<IGC>()).AddMockContract(Mock.Of<IGCInfo>())
            .AddMockContract(Mock.Of<IExecutionManager>()).Build(), rts);

        void AddMemory(uint address, byte[] data)
            => builder.MemoryBuilder.AddHeapFragment(new() { Address = address, Data = data, Name = $"Virtual map data {address:x}" });

        void AddMethod(uint address, uint owner, ushort slot, bool cached)
        {
            byte[] data = new byte[8];
            helpers.Write(data, cached ? address + 8 : 0u);
            if (address == 0x6000)
                helpers.Write(data.AsSpan(4), methodFlags);
            AddMemory(address, data);
            if (cached)
            {
                byte[] codeData = new byte[4];
                helpers.Write(codeData, address == 0x6200 && overrideMap is not null ? 0xb200u : 0xa000u);
                AddMemory(address + 8, codeData);
            }
            rts.Methods[address] = (owner, slot);
            rts.Introduced[owner] = [.. rts.Introduced.GetValueOrDefault(owner) ?? [], new TargetPointer(address)];
        }

        void AddType(uint address, uint parent, ushort virtuals, uint[]? dispatchMap = null, ushort interfaces = 0, bool isInterface = false)
        {
            byte[] data = new byte[24];
            helpers.Write(data, isInterface ? 0x000c0000u : 0u);
            helpers.Write(data.AsSpan(4), dispatchMap is null ? 0u : 4u);
            helpers.Write(data.AsSpan(8), parent);
            helpers.Write(data.AsSpan(12), virtuals);
            helpers.Write(data.AsSpan(14), interfaces);
            helpers.Write(data.AsSpan(16), address + 0x10000u);
            helpers.Write(data.AsSpan(20), 0x9000u);
            AddMemory(address, data);
            if (dispatchMap is not null)
                AddMemory(address + 0x10000 + auxiliarySize, EncodeDispatchMap(dispatchMap));
        }

        static Target.TypeInfo TypeInfo(params (string Name, int Offset)[] fields)
            => new() { Fields = fields.ToDictionary(f => f.Name, f => new Target.FieldInfo { Offset = f.Offset }) };
    }

    [Theory]
    [InlineData(nameof(Data.ExternalMethodFrame), false, false, false, false)]
    [InlineData(nameof(Data.ExternalMethodFrame), true, false, false, true)]
    [InlineData(nameof(Data.StubDispatchFrame), false, false, false, false)]
    [InlineData(nameof(Data.StubDispatchFrame), true, false, false, false)]
    [InlineData(nameof(FrameType.PrestubMethodFrame), false, true, false, true)]
    [InlineData(nameof(FrameType.PrestubMethodFrame), true, false, false, false)]
    [InlineData(nameof(FrameType.PrestubMethodFrame), true, true, true, false)]
    [InlineData(nameof(FrameType.CallCountingHelperFrame), false, true, false, true)]
    [InlineData(nameof(FrameType.CallCountingHelperFrame), true, false, false, false)]
    [InlineData(nameof(FrameType.CallCountingHelperFrame), true, true, true, false)]
    public void TransitionFrame_ThisUsesFrameReceiverKind(
        string frameType, bool encodedInterior, bool valueType, bool unboxing, bool expectedInterior)
    {
        byte[] map = encodedInterior ? [0xfd, 0x02] : [0xdd, 0x02];
        (TestPlaceholderTarget target, ContextHolder<X86Context> context) = CreateX86TransitionFrameTarget(
            frameType, map, hasMethodDesc: true, valueType: valueType, unboxing: unboxing);
        GcScanContext scanContext = new(target, resolveInteriorPointers: false);
        scanContext.UpdateScanContext(new TargetPointer(0x2000), TargetCodePointer.Null, new TargetPointer(0x1000));
        new GcScanner(target).GcScanRoots(new TargetPointer(0x1000), scanContext);

        Assert.Collection(scanContext.StackRefs,
            reference =>
            {
                Assert.Equal(0x2014UL, reference.Address.Value);
                Assert.Equal(expectedInterior ? GcScanFlags.GC_CALL_INTERIOR : GcScanFlags.None, reference.Flags);
            },
            reference =>
            {
                Assert.Equal(0x2010UL, reference.Address.Value);
                Assert.Equal(GcScanFlags.None, reference.Flags);
            });
        new X86FrameHandler(target, context).HandleTransitionFrame(
            target.ProcessedData.GetOrAdd<Data.FramedMethodFrame>(new TargetPointer(0x1000)));
        AssertX86TransitionFrameContext(context, stackPopSlots: 1);
    }

    [Theory]
    [InlineData(false, new byte[] { 0x04, 0x05, 0x21, 0x02 }, uint.MaxValue)]
    [InlineData(false, new byte[] { 0x81, 0x08, 0x0a, 0x30, 0x42, 0x04 }, 8u)]
    [InlineData(true, new byte[] { 0x04, 0x4a, 0x04 }, uint.MaxValue)]
    [InlineData(true, new byte[] { 0x81, 0x08, 0x84, 0x11, 0x22, 0x00 }, 8u)]
    public void GCInfo_Wasm_DecodesPlatformAndInterpreterFormats(
        bool interpreter, byte[] encoded, uint stackBaseRegister)
    {
        MockTarget.Architecture arch = new() { IsLittleEndian = true, Is64Bit = false };
        TestPlaceholderTarget.Builder builder = new(arch);
        // Length 129 and safe point 17 distinguish the platform's six-bit encoding
        // from the interpreter's eight-bit encoding and exercise header alignment.
        byte[] gcInfo = new byte[32];
        encoded.CopyTo(gcInfo, 0);
        builder.MemoryBuilder.AddHeapFragment(new MockMemorySpace.HeapFragment
        {
            Address = 0x1000,
            Data = gcInfo,
            Name = "Wasm GC info"
        });
        Mock<IRuntimeInfo> runtimeInfo = new();
        runtimeInfo.Setup(r => r.GetTargetArchitecture()).Returns(RuntimeInfoArchitecture.Wasm);
        IGCInfo contract = builder
            .AddMockContract(runtimeInfo)
            .AddContract<IGCInfo>(version: "c1")
            .Build().Contracts.GCInfo;
        IGCInfoHandle handle = interpreter
            ? contract.DecodeInterpreterGCInfo(new TargetPointer(0x1000), 4)
            : contract.DecodePlatformSpecificGCInfo(new TargetPointer(0x1000), 4);

        GCInfoHeader header = contract.GetHeader(handle);
        Assert.Equal(129u, contract.GetCodeLength(handle));
        Assert.Equal(129u, header.CodeSize);
        Assert.Equal(stackBaseRegister, header.StackBaseRegister);
        Assert.Equal(0u, header.SizeOfStackParameterArea);
        Assert.Equal(new uint[] { 17 }, contract.GetSafePoints(handle));
        Assert.Empty(contract.GetInterruptibleRanges(handle));
        Assert.Equal(interpreter, contract.TryGetGenericContextStorage(
            handle, GenericContextLoc.ThisPtr, 0, out GenericContextStorage storage));
        if (interpreter)
        {
            Assert.Equal(GenericContextStorageKind.InterpreterArgumentRelative, storage.Kind);
            Assert.Equal(0, storage.Offset);
        }
    }

    [Fact]
    public void LoongArch64Unwind_EpilogReturn_DoesNotRepeatStackAdjustment()
    {
        MockTarget.Architecture arch = new() { IsLittleEndian = true, Is64Bit = true };
        TestPlaceholderTarget.Builder targetBuilder = new(arch);
        TargetTestHelpers helpers = targetBuilder.MemoryBuilder.TargetTestHelpers;

        const ulong ImageBase = 0x1000_0000;
        const uint FunctionStart = 0x1000;
        const uint XdataRva = 0x2000;
        const ulong RuntimeFunctionAddress = 0x1800;
        const ulong CallerSp = 0x3000;
        const ulong ReturnAddress = 0x1234_5678_9abc_def0;

        Layout<MockRuntimeFunction> runtimeFunctionLayout = MockRuntimeFunction.CreateLayout(arch, includeEndAddress: false);
        byte[] runtimeFunctionData = new byte[runtimeFunctionLayout.Size];
        MockRuntimeFunction runtimeFunction = runtimeFunctionLayout.Create(runtimeFunctionData, RuntimeFunctionAddress);
        runtimeFunction.BeginAddress = FunctionStart;
        runtimeFunction.UnwindData = XdataRva;
        targetBuilder.MemoryBuilder.AddHeapFragment(new MockMemorySpace.HeapFragment
        {
            Address = RuntimeFunctionAddress,
            Data = runtimeFunctionData,
            Name = "Runtime function",
        });

        byte[] unwindData = new byte[2 * sizeof(uint)];
        helpers.Write(unwindData.AsSpan(0, sizeof(uint)), 16u | (1u << 21) | (1u << 27));
        unwindData[4] = 0x01; // alloc_s 16
        unwindData[5] = 0xe4; // end
        targetBuilder.MemoryBuilder.AddHeapFragment(new MockMemorySpace.HeapFragment
        {
            Address = ImageBase + XdataRva,
            Data = unwindData,
            Name = "Unwind data",
        });

        TargetCodePointer controlPc = new(ImageBase + FunctionStart + (15 * sizeof(uint)));
        CodeBlockHandle codeBlock = new(new TargetPointer(controlPc.Value));
        Mock<IExecutionManager> executionManager = new();
        executionManager.Setup(e => e.GetCodeBlockHandle(controlPc)).Returns(codeBlock);
        executionManager.Setup(e => e.GetUnwindInfoBaseAddress(codeBlock)).Returns(new TargetPointer(ImageBase));
        executionManager.Setup(e => e.GetUnwindInfo(codeBlock)).Returns(new TargetPointer(RuntimeFunctionAddress));

        TestPlaceholderTarget target = targetBuilder
            .AddTypes(new Dictionary<DataType, Target.TypeInfo>
            {
                [DataType.RuntimeFunction] = TargetTestHelpers.CreateTypeInfo(runtimeFunctionLayout),
            })
            .AddMockContract(executionManager.Object)
            .Build();

        LoongArch64Context context = new()
        {
            ContextFlags = (uint)LoongArch64Context.ContextFlagsValues.CONTEXT_FULL,
            Sp = CallerSp,
            Ra = ReturnAddress,
            Pc = controlPc.Value,
        };

        Assert.True(new Contracts.StackWalkHelpers.LoongArch64.LoongArch64Unwinder(target).Unwind(ref context));
        Assert.Equal(CallerSp, context.Sp);
        Assert.Equal(ReturnAddress, context.Pc);
        Assert.NotEqual(0u, context.ContextFlags & (uint)LoongArch64Context.ContextFlagsValues.CONTEXT_UNWOUND_TO_CALL);
    }

    [Theory]
    [InlineData(nameof(Data.ExternalMethodFrame), new byte[] { 0x00 }, 0u, false)]
    [InlineData(nameof(Data.ExternalMethodFrame), new byte[] { 0x01 }, 1u, false)]
    [InlineData(nameof(Data.ExternalMethodFrame), new byte[] { 0x02 }, 2u, false)]
    [InlineData(nameof(Data.ExternalMethodFrame), new byte[] { 0x03 }, 3u, false)]
    [InlineData(nameof(Data.ExternalMethodFrame), new byte[] { 0x0f }, 6u, false)]
    [InlineData(nameof(Data.ExternalMethodFrame), new byte[] { 0x01 }, 1u, true)]
    [InlineData(nameof(Data.StubDispatchFrame), new byte[] { 0x00 }, 0u, false)]
    [InlineData(nameof(Data.StubDispatchFrame), new byte[] { 0x01 }, 1u, false)]
    [InlineData(nameof(Data.StubDispatchFrame), new byte[] { 0x02 }, 2u, false)]
    [InlineData(nameof(Data.StubDispatchFrame), new byte[] { 0x03 }, 3u, false)]
    [InlineData(nameof(Data.StubDispatchFrame), new byte[] { 0x0f }, 6u, false)]
    [InlineData(nameof(Data.StubDispatchFrame), new byte[] { 0x01 }, 1u, true)]
    [InlineData(nameof(FrameType.PrestubMethodFrame), new byte[] { 0x00 }, 0u, true)]
    [InlineData(nameof(FrameType.PrestubMethodFrame), new byte[] { 0x01 }, 1u, true)]
    [InlineData(nameof(FrameType.PrestubMethodFrame), new byte[] { 0x0f }, 6u, true)]
    [InlineData(nameof(FrameType.CallCountingHelperFrame), new byte[] { 0x00 }, 0u, true)]
    [InlineData(nameof(FrameType.CallCountingHelperFrame), new byte[] { 0x01 }, 1u, true)]
    [InlineData(nameof(FrameType.CallCountingHelperFrame), new byte[] { 0x0f }, 6u, true)]
    public void X86TransitionFrame_UsesGCRefMapForStackPop(string frameType, byte[] gcRefMap, uint stackPopSlots, bool hasMethodDesc)
    {
        (TestPlaceholderTarget target, ContextHolder<X86Context> context) =
            CreateX86TransitionFrameTarget(frameType, gcRefMap, hasMethodDesc);

        new X86FrameHandler(target, context).HandleTransitionFrame(
            target.ProcessedData.GetOrAdd<Data.FramedMethodFrame>(new TargetPointer(0x1000)));

        AssertX86TransitionFrameContext(context, stackPopSlots);
        Assert.Equal(0x1234_5678UL, new FrameHelpers(target).GetReturnAddress(
            target.ProcessedData.GetOrAdd<Data.Frame>(new TargetPointer(0x1000))).Value);
    }

    [Theory]
    [InlineData(nameof(Data.FramedMethodFrame))]
    [InlineData(nameof(Data.ExternalMethodFrame))]
    [InlineData(nameof(Data.StubDispatchFrame))]
    [InlineData(nameof(FrameType.PrestubMethodFrame))]
    [InlineData(nameof(FrameType.CallCountingHelperFrame))]
    public void X86TransitionFrame_UsesSignatureWhenGCRefMapUnavailable(string frameType)
    {
        (TestPlaceholderTarget target, ContextHolder<X86Context> context) =
            CreateX86TransitionFrameTarget(frameType, gcRefMap: null, hasMethodDesc: true, signatureBlob: [0x02]);

        new X86FrameHandler(target, context).HandleTransitionFrame(
            target.ProcessedData.GetOrAdd<Data.FramedMethodFrame>(new TargetPointer(0x1000)));

        AssertX86TransitionFrameContext(context, stackPopSlots: 2);
        Assert.Equal(0x1234_5678UL, new FrameHelpers(target).GetReturnAddress(
            target.ProcessedData.GetOrAdd<Data.Frame>(new TargetPointer(0x1000))).Value);
    }

    [Theory]
    [InlineData(nameof(FrameType.PrestubMethodFrame), true, true, true)]
    [InlineData(nameof(FrameType.PrestubMethodFrame), false, true, true)]
    [InlineData(nameof(FrameType.PrestubMethodFrame), false, false, true)]
    [InlineData(nameof(FrameType.PrestubMethodFrame), false, true, false)]
    [InlineData(nameof(FrameType.CallCountingHelperFrame), true, true, true)]
    [InlineData(nameof(FrameType.CallCountingHelperFrame), false, true, true)]
    [InlineData(nameof(FrameType.CallCountingHelperFrame), false, false, true)]
    [InlineData(nameof(FrameType.CallCountingHelperFrame), false, true, false)]
    public void MethodDescFrame_ScansRootsWithCachedMapOrSignature(
        string frameType, bool cachedMap, bool hasCodeData, bool hasGCRefMapField)
    {
        byte[] map = [0x24];
        (TestPlaceholderTarget target, _) = CreateX86TransitionFrameTarget(
            frameType, gcRefMap: cachedMap ? map : null, hasMethodDesc: true,
            signatureBlob: cachedMap ? null : map, hasCodeData: hasCodeData, hasGCRefMapField: hasGCRefMapField);
        GcScanContext scanContext = new(target, resolveInteriorPointers: false);
        scanContext.UpdateScanContext(new TargetPointer(0x2000), TargetCodePointer.Null, new TargetPointer(0x1000));

        new GcScanner(target).GcScanRoots(new TargetPointer(0x1000), scanContext);

        Assert.Collection(scanContext.StackRefs,
            reference =>
            {
                Assert.Equal(0x2014UL, reference.Address.Value);
                Assert.Equal(0x2222_0000UL, reference.Object.Value);
                Assert.Equal(GcScanFlags.None, reference.Flags);
            },
            reference =>
            {
                Assert.Equal(0x2010UL, reference.Address.Value);
                Assert.Equal(0x1111_0000UL, reference.Object.Value);
                Assert.Equal(GcScanFlags.GC_CALL_INTERIOR, reference.Flags);
            });
    }

    [Theory]
    [InlineData(nameof(FrameType.PrestubMethodFrame), new byte[] { 0 }, new ulong[] { })]
    [InlineData(nameof(FrameType.CallCountingHelperFrame), new byte[] { 0 }, new ulong[] { })]
    [InlineData(nameof(FrameType.PrestubMethodFrame), new byte[] { 0xe4, 0 }, new ulong[] { 0x2014, 0x2010, 0x201c })]
    [InlineData(nameof(FrameType.CallCountingHelperFrame), new byte[] { 0xe4, 0 }, new ulong[] { 0x2014, 0x2010, 0x201c })]
    public void MethodDescFrame_CachedMapsDoNotInspectSignature(string frameType, byte[] gcRefMap, ulong[] expectedSlots)
    {
        (TestPlaceholderTarget target, _) = CreateX86TransitionFrameTarget(
            frameType, gcRefMap, hasMethodDesc: true);
        GcScanContext scanContext = new(target, resolveInteriorPointers: false);
        scanContext.UpdateScanContext(new TargetPointer(0x2000), TargetCodePointer.Null, new TargetPointer(0x1000));

        new GcScanner(target).GcScanRoots(new TargetPointer(0x1000), scanContext);

        Assert.Equal(expectedSlots, scanContext.StackRefs.Select(reference => reference.Address.Value));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void X86TransitionFrame_StubDispatchWithoutMapOrMethod_AdjustsInstructionPointer(
        bool hasRepresentativeMethodTable, bool hasUnresolvedIndirection)
    {
        (TestPlaceholderTarget target, ContextHolder<X86Context> context) = CreateX86TransitionFrameTarget(
            nameof(Data.StubDispatchFrame), gcRefMap: null, hasMethodDesc: false,
            representativeMethodDescAddress: hasRepresentativeMethodTable ? 0u : null,
            hasUnresolvedIndirection: hasUnresolvedIndirection);

        new X86FrameHandler(target, context).HandleTransitionFrame(
            target.ProcessedData.GetOrAdd<Data.FramedMethodFrame>(new TargetPointer(0x1000)));

        AssertX86TransitionFrameContext(context, stackPopSlots: 0, instructionPointer: 0x1234_5673);
        Assert.Equal(0x1234_5673UL, new FrameHelpers(target).GetReturnAddress(
            target.ProcessedData.GetOrAdd<Data.Frame>(new TargetPointer(0x1000))).Value);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void X86TransitionFrame_StubDispatchUsesRepresentativeMethod(bool hasUnresolvedIndirection)
    {
        (TestPlaceholderTarget target, ContextHolder<X86Context> context) = CreateX86TransitionFrameTarget(
            nameof(Data.StubDispatchFrame), gcRefMap: null, hasMethodDesc: false, signatureBlob: [0x02],
            representativeMethodDescAddress: 0x6000, hasUnresolvedIndirection: hasUnresolvedIndirection);

        new X86FrameHandler(target, context).HandleTransitionFrame(
            target.ProcessedData.GetOrAdd<Data.FramedMethodFrame>(new TargetPointer(0x1000)));

        AssertX86TransitionFrameContext(context, stackPopSlots: 2);
        Assert.Equal(0x1234_5678UL, new FrameHelpers(target).GetReturnAddress(
            target.ProcessedData.GetOrAdd<Data.Frame>(new TargetPointer(0x1000))).Value);
    }

    [Theory]
    [InlineData(RuntimeInfoArchitecture.X86, 0x1234_5673UL)]
    [InlineData(RuntimeInfoArchitecture.Arm, 0x1234_5678UL)]
    public void StubDispatchFrame_ReturnAddressWithoutMapOrMethod(
        RuntimeInfoArchitecture architecture, ulong expectedReturnAddress)
    {
        (TestPlaceholderTarget target, _) = CreateX86TransitionFrameTarget(
            nameof(Data.StubDispatchFrame), gcRefMap: null, hasMethodDesc: false, architecture: architecture);

        Assert.Equal(expectedReturnAddress, new FrameHelpers(target).GetReturnAddress(
            target.ProcessedData.GetOrAdd<Data.Frame>(new TargetPointer(0x1000))).Value);
    }

    private static void AssertX86TransitionFrameContext(
        ContextHolder<X86Context> context, uint stackPopSlots, uint instructionPointer = 0x1234_5678)
    {
        Assert.Equal(0x2000u + X86TransitionBlockSize + stackPopSlots * sizeof(uint), context.Context.Esp);
        Assert.Equal(instructionPointer, context.Context.Eip);
        Assert.Equal(0x1111_1111u, context.Context.Ebp);
        Assert.Equal(0x2222_2222u, context.Context.Ebx);
        Assert.Equal(0x3333_3333u, context.Context.Edi);
        Assert.Equal(0x4444_4444u, context.Context.Esi);
        Assert.Equal((uint)X86Context.ContextFlagsValues.CONTEXT_FULL, context.Context.ContextFlags);
    }

    private static (TestPlaceholderTarget Target, ContextHolder<X86Context> Context) CreateX86TransitionFrameTarget(
        string frameType, byte[]? gcRefMap, bool hasMethodDesc, byte[]? signatureBlob = null,
        uint? representativeMethodDescAddress = null, bool hasUnresolvedIndirection = false,
        RuntimeInfoArchitecture architecture = RuntimeInfoArchitecture.X86, bool hasCodeData = true,
        bool hasGCRefMapField = true, bool? valueType = null, bool unboxing = false, bool inlineGCRefMap = true)
    {
        MockTarget.Architecture arch = new() { IsLittleEndian = true, Is64Bit = false };
        TestPlaceholderTarget.Builder builder = new(arch);
        TargetTestHelpers helpers = builder.MemoryBuilder.TargetTestHelpers;
        const uint FrameIdentifier = 0x100;
        const uint MethodDescAddress = 0x6000;
        const uint ImageBase = 0x100000;
        const uint ImportSectionRva = 0x100;
        const uint GCRefMapRva = 0x200;
        bool methodDescMap = frameType is nameof(FrameType.PrestubMethodFrame) or nameof(FrameType.CallCountingHelperFrame);
        TargetPointer indirection = new(ImageBase + ImportSectionRva + 2 * sizeof(uint));

        byte[] frameData = new byte[28];
        helpers.Write(frameData.AsSpan(0), FrameIdentifier);
        helpers.Write(frameData.AsSpan(8), 0x2000u);
        helpers.Write(frameData.AsSpan(12), hasMethodDesc ? MethodDescAddress : 0);
        helpers.Write(frameData.AsSpan(16), gcRefMap is not null || hasUnresolvedIndirection ? (uint)indirection.Value : 0);
        helpers.Write(frameData.AsSpan(20), representativeMethodDescAddress.HasValue ? 0x7000u : 0);
        helpers.Write(frameData.AsSpan(24), 3u);
        builder.MemoryBuilder.AddHeapFragment(new() { Address = 0x1000, Data = frameData, Name = "Transition frame" });

        byte[] transitionBlock = new byte[X86TransitionBlockSize];
        helpers.Write(transitionBlock.AsSpan(0), 0x1111_1111u);
        helpers.Write(transitionBlock.AsSpan(4), 0x2222_2222u);
        helpers.Write(transitionBlock.AsSpan(8), 0x3333_3333u);
        helpers.Write(transitionBlock.AsSpan(12), 0x4444_4444u);
        helpers.Write(transitionBlock.AsSpan(16), 0x1111_0000u);
        helpers.Write(transitionBlock.AsSpan(20), 0x2222_0000u);
        helpers.Write(transitionBlock.AsSpan(24), 0x1234_5678u);
        builder.MemoryBuilder.AddHeapFragment(new() { Address = 0x2000, Data = transitionBlock, Name = "Transition block" });
        byte[] stackArgument = new byte[sizeof(uint)];
        helpers.Write(stackArgument, 0x3333_0000u);
        builder.MemoryBuilder.AddHeapFragment(new() { Address = 0x201c, Data = stackArgument, Name = "Stack argument" });

        Mock<IExecutionManager> executionManager = new(MockBehavior.Strict);
        if (methodDescMap)
        {
            byte[] methodDesc = new byte[sizeof(uint)];
            helpers.Write(methodDesc, hasCodeData ? 0x6100u : 0u);
            builder.MemoryBuilder.AddHeapFragment(new() { Address = MethodDescAddress, Data = methodDesc, Name = "MethodDesc" });
            byte[] codeData = new byte[sizeof(uint)];
            helpers.Write(codeData, gcRefMap is not null ? 0x6200u : 0u);
            builder.MemoryBuilder.AddHeapFragment(new() { Address = 0x6100, Data = codeData, Name = "MethodDesc code data" });
            if (gcRefMap is not null)
                builder.MemoryBuilder.AddHeapFragment(new() { Address = 0x6200, Data = gcRefMap, Name = "Cached GCRefMap" });
        }
        else if (gcRefMap is not null)
        {
            byte[] module = new byte[4];
            helpers.Write(module, 0x4000u);
            builder.MemoryBuilder.AddHeapFragment(new() { Address = 0x3000, Data = module, Name = "Module" });
            byte[] r2rInfo = new byte[12];
            helpers.Write(r2rInfo.AsSpan(0), ImageBase);
            helpers.Write(r2rInfo.AsSpan(4), 1u);
            helpers.Write(r2rInfo.AsSpan(8), 0x5000u);
            builder.MemoryBuilder.AddHeapFragment(new() { Address = 0x4000, Data = r2rInfo, Name = "ReadyToRun info" });
            byte[] importSection = new byte[20];
            helpers.Write(importSection.AsSpan(0), ImportSectionRva);
            helpers.Write(importSection.AsSpan(4), 3u * sizeof(uint));
            importSection[11] = sizeof(uint);
            if (inlineGCRefMap)
            {
                const uint SignaturesRva = 0x180;
                helpers.Write(importSection.AsSpan(8), (ushort)0x0008);
                helpers.Write(importSection.AsSpan(12), SignaturesRva);
                byte[] signatures = new byte[3 * sizeof(uint)];
                helpers.Write(signatures.AsSpan(2 * sizeof(uint)), GCRefMapRva);
                builder.MemoryBuilder.AddHeapFragment(new() { Address = ImageBase + SignaturesRva, Data = signatures, Name = "Import signatures" });
            }
            else
            {
                helpers.Write(importSection.AsSpan(16), GCRefMapRva);
            }
            builder.MemoryBuilder.AddHeapFragment(new() { Address = 0x5000, Data = importSection, Name = "Import section" });
            int mapOffset = inlineGCRefMap ? 0 : 6;
            byte[] maps = new byte[mapOffset + gcRefMap.Length];
            if (!inlineGCRefMap)
            {
                // Use the third import to exercise skipping the two preceding empty maps.
                helpers.Write(maps.AsSpan(0), 4u);
            }
            gcRefMap.CopyTo(maps, mapOffset);
            builder.MemoryBuilder.AddHeapFragment(new() { Address = ImageBase + GCRefMapRva, Data = maps, Name = "GCRefMaps" });
            executionManager.Setup(e => e.FindReadyToRunModule(indirection)).Returns(new TargetPointer(0x3000));
        }
        else if (hasUnresolvedIndirection)
        {
            executionManager.Setup(e => e.FindReadyToRunModule(indirection)).Returns(TargetPointer.Null);
        }

        Mock<IRuntimeTypeSystem> runtimeTypeSystem = new(MockBehavior.Strict);
        if (valueType.HasValue)
        {
            MethodDescHandle method = new(new TargetPointer(MethodDescAddress));
            Mock<ITypeHandle> type = new(MockBehavior.Strict);
            runtimeTypeSystem.Setup(r => r.GetMethodDescHandle(new TargetPointer(MethodDescAddress))).Returns(method);
            runtimeTypeSystem.Setup(r => r.GetMethodTable(method)).Returns(new TargetPointer(0x7000));
            runtimeTypeSystem.Setup(r => r.GetTypeHandle(new TargetPointer(0x7000))).Returns(type.Object);
            runtimeTypeSystem.Setup(r => r.IsValueType(type.Object)).Returns(valueType.Value);
            runtimeTypeSystem.Setup(r => r.IsUnboxingStub(method)).Returns(unboxing);
        }
        if (representativeMethodDescAddress.HasValue)
        {
            Mock<ITypeHandle> representativeType = new(MockBehavior.Strict);
            runtimeTypeSystem.Setup(r => r.GetTypeHandle(new TargetPointer(0x7000))).Returns(representativeType.Object);
            runtimeTypeSystem.Setup(r => r.GetMethodDescForSlot(representativeType.Object, 3))
                .Returns(new TargetPointer(representativeMethodDescAddress.Value));
        }
        Mock<ICallingConvention> callingConvention = new(MockBehavior.Strict);
        if (signatureBlob is not null)
        {
            MethodDescHandle method = new(new TargetPointer(MethodDescAddress));
            runtimeTypeSystem.Setup(r => r.GetMethodDescHandle(new TargetPointer(MethodDescAddress))).Returns(method);
            callingConvention.Setup(c => c.TryComputeArgGCRefMapBlob(method, out signatureBlob)).Returns(true);
        }

        Dictionary<DataType, Target.TypeInfo> types = new()
        {
            [DataType.Array] = CreateTypeInfo((nameof(Data.Array.NumComponents), 4)),
            [DataType.String] = CreateTypeInfo((nameof(Data.String.StringLength), 4)),
            [DataType.Object] = CreateTypeInfo((nameof(Data.Object.MethodTable), 0)),
            [DataType.Frame] = CreateTypeInfo((nameof(Data.Frame.Identifier), 0)),
            [DataType.FramedMethodFrame] = CreateTypeInfo(
                (nameof(Data.FramedMethodFrame.TransitionBlockPtr), 8), (nameof(Data.FramedMethodFrame.MethodDescPtr), 12)),
            [DataType.TransitionBlock] = CreateTypeInfo(
                (nameof(Data.TransitionBlock.CalleeSavedRegisters), 0), (nameof(Data.TransitionBlock.ReturnAddress), 24),
                (nameof(Data.TransitionBlock.ArgumentRegisters), 16)) with { Size = X86TransitionBlockSize },
            [DataType.CalleeSavedRegisters] = CreateTypeInfo(
                (nameof(X86Context.Ebp), 0), (nameof(X86Context.Ebx), 4), (nameof(X86Context.Edi), 8), (nameof(X86Context.Esi), 12)),
            [DataType.Module] = CreateTypeInfo((nameof(Data.Module.ReadyToRunInfo), 0)),
            [DataType.ReadyToRunInfo] = CreateTypeInfo(
                (nameof(Data.ReadyToRunInfo.LoadedImageBase), 0), (nameof(Data.ReadyToRunInfo.NumImportSections), 4), (nameof(Data.ReadyToRunInfo.ImportSections), 8)),
        };
        if (methodDescMap)
        {
            types[DataType.MethodDesc] = CreateTypeInfo((nameof(Data.MethodDesc.CodeData), 0));
            types[DataType.MethodDescCodeData] = hasGCRefMapField
                ? CreateTypeInfo((nameof(Data.MethodDescCodeData.GCRefMap), 0))
                : CreateTypeInfo();
        }
        if (frameType != nameof(Data.FramedMethodFrame) && !methodDescMap)
        {
            types[Enum.Parse<DataType>(frameType)] = CreateTypeInfo(
                (nameof(Data.FramedMethodFrame.MethodDescPtr), 12), (nameof(Data.ExternalMethodFrame.Indirection), 16));
        }
        if (frameType == nameof(Data.StubDispatchFrame))
        {
            types[DataType.StubDispatchFrame] = CreateTypeInfo(
                (nameof(Data.StubDispatchFrame.MethodDescPtr), 12), (nameof(Data.StubDispatchFrame.Indirection), 16),
                (nameof(Data.StubDispatchFrame.RepresentativeMTPtr), 20), (nameof(Data.StubDispatchFrame.RepresentativeSlot), 24));
        }

        Mock<IRuntimeInfo> runtimeInfo = new(MockBehavior.Strict);
        runtimeInfo.Setup(r => r.GetTargetArchitecture()).Returns(architecture);
        TestPlaceholderTarget target = builder
            .AddTypes(types)
            .AddGlobals((frameType + "Identifier", FrameIdentifier), (Constants.Globals.ObjectToMethodTableUnmask, 0))
            .AddMockContract(executionManager)
            .AddMockContract(runtimeTypeSystem)
            .AddMockContract(callingConvention)
            .AddMockContract(Mock.Of<IGC>())
            .AddMockContract(Mock.Of<IGCInfo>())
            .AddMockContract(runtimeInfo)
            .Build();
        ContextHolder<X86Context> context = new()
        {
            Context = new X86Context { ContextFlags = (uint)X86Context.ContextFlagsValues.CONTEXT_FULL, Esp = uint.MaxValue },
        };

        return (target, context);

        static Target.TypeInfo CreateTypeInfo(params (string Name, int Offset)[] fields)
            => new() { Fields = fields.ToDictionary(f => f.Name, f => new Target.FieldInfo { Offset = f.Offset }) };
    }

    [Fact]
    public void X86Unwind_EbpProlog_RestoresCallerContext()
    {
        MockTarget.Architecture arch = new() { IsLittleEndian = true, Is64Bit = false };
        TestPlaceholderTarget.Builder targetBuilder = new(arch);
        TargetTestHelpers helpers = targetBuilder.MemoryBuilder.TargetTestHelpers;

        const uint MethodStart = 0x1000;
        const uint GcInfoAddress = 0x1800;
        const uint CurrentEbp = 0x2100;
        const uint CallerEbp = 0x3100;
        const uint ReturnAddress = 0x1234_5678;
        const uint SavedEdi = 0x1111_1111;
        const uint SavedEsi = 0x2222_2222;
        const uint CurrentEbx = 0x3333_3333;

        targetBuilder.MemoryBuilder.AddHeapFragment(new MockMemorySpace.HeapFragment
        {
            Address = MethodStart,
            Data = [0x55, 0x8b, 0xec, 0x57, 0x56, 0x53],
            Name = "Method code",
        });

        targetBuilder.MemoryBuilder.AddHeapFragment(new MockMemorySpace.HeapFragment
        {
            Address = GcInfoAddress,
            Data = [0x20, 0x4f],
            Name = "GC info",
        });

        byte[] stack = new byte[0x20];
        helpers.Write(stack.AsSpan(0x04, sizeof(uint)), CurrentEbx);
        helpers.Write(stack.AsSpan(0x08, sizeof(uint)), SavedEsi);
        helpers.Write(stack.AsSpan(0x0c, sizeof(uint)), SavedEdi);
        helpers.Write(stack.AsSpan(0x10, sizeof(uint)), CallerEbp);
        helpers.Write(stack.AsSpan(0x14, sizeof(uint)), ReturnAddress);
        targetBuilder.MemoryBuilder.AddHeapFragment(new MockMemorySpace.HeapFragment
        {
            Address = CurrentEbp - 0x10,
            Data = stack,
            Name = "Stack",
        });

        CodeBlockHandle codeBlock = new(new TargetPointer(MethodStart));
        TargetPointer gcInfo = new(GcInfoAddress);
        uint gcInfoVersion = 4;
        Mock<IExecutionManager> executionManager = new();
        executionManager.Setup(e => e.GetCodeBlockHandle(new TargetCodePointer(MethodStart + 5))).Returns(codeBlock);
        executionManager.Setup(e => e.GetGCInfo(codeBlock, out gcInfo, out gcInfoVersion));
        executionManager.Setup(e => e.GetRelativeOffset(codeBlock)).Returns(new TargetNUInt(5));
        executionManager.Setup(e => e.GetStartAddress(codeBlock)).Returns(new TargetPointer(MethodStart));
        executionManager.Setup(e => e.GetFuncletStartAddress(codeBlock)).Returns(new TargetPointer(MethodStart));
        executionManager.Setup(e => e.IsFunclet(codeBlock)).Returns(false);

        Mock<IRuntimeInfo> runtimeInfo = new();
        runtimeInfo.Setup(r => r.GetTargetOperatingSystem()).Returns(RuntimeInfoOperatingSystem.Windows);

        TestPlaceholderTarget target = targetBuilder
            .AddMockContract(executionManager.Object)
            .AddMockContract(runtimeInfo.Object)
            .Build();

        X86Context context = new()
        {
            ContextFlags = (uint)X86Context.ContextFlagsValues.CONTEXT_FULL,
            Eip = MethodStart + 5,
            Esp = CurrentEbp - 8,
            Ebp = CurrentEbp,
            Edi = uint.MaxValue,
            Esi = uint.MaxValue,
            Ebx = CurrentEbx,
        };

        Assert.True(new Contracts.StackWalkHelpers.X86.X86Unwinder(target).Unwind(ref context));
        Assert.Equal(SavedEdi, context.Edi);
        Assert.Equal(SavedEsi, context.Esi);
        Assert.Equal(CurrentEbx, context.Ebx);
        Assert.Equal(CallerEbp, context.Ebp);
        Assert.Equal(CurrentEbp + (2 * sizeof(uint)), context.Esp);
        Assert.Equal(ReturnAddress, context.Eip);
        Assert.NotEqual(0u, context.ContextFlags & (uint)X86Context.ContextFlagsValues.CONTEXT_UNWOUND_TO_CALL);
    }

    [Fact]
    public void ARMUnwind_CompactEpilog_RestoresCallerContext()
    {
        MockTarget.Architecture arch = new() { IsLittleEndian = true, Is64Bit = false };
        TestPlaceholderTarget.Builder targetBuilder = new(arch);
        TargetTestHelpers helpers = targetBuilder.MemoryBuilder.TargetTestHelpers;

        const uint ImageBase = 0x1000_0000;
        const uint FunctionStart = 0x1000;
        const uint RuntimeFunctionAddress = 0x1800;
        const uint CurrentSp = 0x2000;
        const uint SavedR4 = 0x4444_4444;
        const uint ReturnAddress = 0x1234_5679;

        Layout<MockRuntimeFunction> runtimeFunctionLayout = MockRuntimeFunction.CreateLayout(arch, includeEndAddress: false);
        byte[] runtimeFunctionData = new byte[runtimeFunctionLayout.Size];
        MockRuntimeFunction runtimeFunction = runtimeFunctionLayout.Create(runtimeFunctionData, RuntimeFunctionAddress);
        runtimeFunction.BeginAddress = FunctionStart;
        runtimeFunction.UnwindData =
            1u |           // Flag = packed unwind data
            (16u << 2) |   // FunctionLength = 16 halfwords
            (16u << 16) |  // L = 1, Reg = 0: save r4 and lr
            (1u << 22);    // StackAdjust = 1 word
        targetBuilder.MemoryBuilder.AddHeapFragment(new MockMemorySpace.HeapFragment
        {
            Address = RuntimeFunctionAddress,
            Data = runtimeFunctionData,
            Name = "Runtime function",
        });

        byte[] stack = new byte[2 * sizeof(uint)];
        helpers.Write(stack.AsSpan(0, sizeof(uint)), SavedR4);
        helpers.Write(stack.AsSpan(sizeof(uint), sizeof(uint)), ReturnAddress);
        targetBuilder.MemoryBuilder.AddHeapFragment(new MockMemorySpace.HeapFragment
        {
            Address = CurrentSp,
            Data = stack,
            Name = "Stack",
        });

        TargetCodePointer controlPc = new(ImageBase + FunctionStart + (15 * 2));
        CodeBlockHandle codeBlock = new(new TargetPointer(controlPc.Value));
        Mock<IExecutionManager> executionManager = new();
        executionManager.Setup(e => e.GetCodeBlockHandle(controlPc)).Returns(codeBlock);
        executionManager.Setup(e => e.GetUnwindInfoBaseAddress(codeBlock)).Returns(new TargetPointer(ImageBase));
        executionManager.Setup(e => e.GetUnwindInfo(codeBlock)).Returns(new TargetPointer(RuntimeFunctionAddress));

        TestPlaceholderTarget target = targetBuilder
            .AddTypes(new Dictionary<DataType, Target.TypeInfo>
            {
                [DataType.RuntimeFunction] = TargetTestHelpers.CreateTypeInfo(runtimeFunctionLayout),
            })
            .AddMockContract(executionManager.Object)
            .Build();

        ARMContext context = new()
        {
            ContextFlags = (uint)ARMContext.ContextFlagsValues.CONTEXT_FULL,
            R4 = uint.MaxValue,
            Sp = CurrentSp,
            Lr = uint.MaxValue,
            Pc = (uint)controlPc.Value,
        };

        Assert.True(new Contracts.StackWalkHelpers.ARM.ARMUnwinder(target).Unwind(ref context));
        Assert.Equal(SavedR4, context.R4);
        Assert.Equal(CurrentSp + (2 * sizeof(uint)), context.Sp);
        Assert.Equal(ReturnAddress, context.Lr);
        Assert.Equal(ReturnAddress, context.Pc);
        Assert.NotEqual(0u, context.ContextFlags & (uint)ARMContext.ContextFlagsValues.CONTEXT_UNWOUND_TO_CALL);
    }

    [Fact]
    public void RISCV64Unwind_SingleEpilogAtReturn_DoesNotReapplyStackAdjustment()
    {
        MockTarget.Architecture arch = new() { IsLittleEndian = true, Is64Bit = true };
        TestPlaceholderTarget.Builder targetBuilder = new(arch);
        TargetTestHelpers helpers = targetBuilder.MemoryBuilder.TargetTestHelpers;

        const uint ImageBase = 0x1000_0000;
        const uint FunctionStart = 0x1000;
        const uint RuntimeFunctionAddress = 0x1800;
        const uint UnwindDataRva = 0x2000;
        const ulong CurrentSp = 0x3000;
        const ulong ReturnAddress = 0x1234_5678_9abc_def0;

        Layout<MockRuntimeFunction> runtimeFunctionLayout = MockRuntimeFunction.CreateLayout(arch, includeEndAddress: false);
        byte[] runtimeFunctionData = new byte[runtimeFunctionLayout.Size];
        MockRuntimeFunction runtimeFunction = runtimeFunctionLayout.Create(runtimeFunctionData, RuntimeFunctionAddress);
        runtimeFunction.BeginAddress = FunctionStart;
        runtimeFunction.UnwindData = UnwindDataRva;
        targetBuilder.MemoryBuilder.AddHeapFragment(new MockMemorySpace.HeapFragment
        {
            Address = RuntimeFunctionAddress,
            Data = runtimeFunctionData,
            Name = "Runtime function",
        });

        byte[] unwindData = new byte[2 * sizeof(uint)];
        uint unwindHeader =
            8u |          // FunctionLength = 8 halfwords
            (1u << 21) |  // E = 1: single epilog at the end of the function
            (1u << 27);   // CodeWords = 1
        helpers.Write(unwindData.AsSpan(0, sizeof(uint)), unwindHeader);
        unwindData[sizeof(uint)] = 0x01;     // alloc_s 1: restore 16 bytes of stack
        unwindData[sizeof(uint) + 1] = 0xe4; // end
        targetBuilder.MemoryBuilder.AddHeapFragment(new MockMemorySpace.HeapFragment
        {
            Address = ImageBase + UnwindDataRva,
            Data = unwindData,
            Name = "Unwind data",
        });

        TargetCodePointer controlPc = new(ImageBase + FunctionStart + 12);
        CodeBlockHandle codeBlock = new(new TargetPointer(controlPc.Value));
        Mock<IExecutionManager> executionManager = new();
        executionManager.Setup(e => e.GetCodeBlockHandle(controlPc)).Returns(codeBlock);
        executionManager.Setup(e => e.GetUnwindInfoBaseAddress(codeBlock)).Returns(new TargetPointer(ImageBase));
        executionManager.Setup(e => e.GetUnwindInfo(codeBlock)).Returns(new TargetPointer(RuntimeFunctionAddress));

        TestPlaceholderTarget target = targetBuilder
            .AddTypes(new Dictionary<DataType, Target.TypeInfo>
            {
                [DataType.RuntimeFunction] = TargetTestHelpers.CreateTypeInfo(runtimeFunctionLayout),
            })
            .AddMockContract(executionManager.Object)
            .Build();

        RISCV64Context context = new()
        {
            ContextFlags = (uint)RISCV64Context.ContextFlagsValues.CONTEXT_FULL,
            Sp = CurrentSp,
            Ra = ReturnAddress,
            Pc = controlPc.Value,
        };

        Assert.True(new Contracts.StackWalkHelpers.RISCV64.RISCV64Unwinder(target).Unwind(ref context));
        Assert.Equal(CurrentSp, context.Sp);
        Assert.Equal(ReturnAddress, context.Pc);
        Assert.NotEqual(0u, context.ContextFlags & (uint)RISCV64Context.ContextFlagsValues.CONTEXT_UNWOUND_TO_CALL);
    }

    [Fact]
    public void ARM64Unwind_SaveAnyPair_RestoresCallerContext()
    {
        MockTarget.Architecture arch = new() { IsLittleEndian = true, Is64Bit = true };
        TestPlaceholderTarget.Builder targetBuilder = new(arch);
        TargetTestHelpers helpers = targetBuilder.MemoryBuilder.TargetTestHelpers;

        const ulong ImageBase = 0x1000_0000;
        const uint FunctionStart = 0x1000;
        const uint XdataRva = 0x2000;
        const ulong RuntimeFunctionAddress = 0x1800;
        const ulong CurrentSp = 0x3000;
        const ulong SavedX19 = 0x1919_1919_1919_1919;
        const ulong SavedX20 = 0x2020_2020_2020_2020;
        const ulong ReturnAddress = 0x1234_5678_9abc_def0;

        Layout<MockRuntimeFunction> runtimeFunctionLayout = MockRuntimeFunction.CreateLayout(arch, includeEndAddress: false);
        byte[] runtimeFunctionData = new byte[runtimeFunctionLayout.Size];
        MockRuntimeFunction runtimeFunction = runtimeFunctionLayout.Create(runtimeFunctionData, RuntimeFunctionAddress);
        runtimeFunction.BeginAddress = FunctionStart;
        runtimeFunction.UnwindData = XdataRva;
        targetBuilder.MemoryBuilder.AddHeapFragment(new MockMemorySpace.HeapFragment
        {
            Address = RuntimeFunctionAddress,
            Data = runtimeFunctionData,
            Name = "Runtime function",
        });

        byte[] unwindData = new byte[2 * sizeof(uint)];
        helpers.Write(unwindData.AsSpan(0, sizeof(uint)), 16u | (1u << 27));
        unwindData[4] = 0xe7; // save_any
        unwindData[5] = 0x53; // p=1, x=0, r=19
        unwindData[6] = 0x00; // f=0, o=0
        unwindData[7] = 0xe4; // end
        targetBuilder.MemoryBuilder.AddHeapFragment(new MockMemorySpace.HeapFragment
        {
            Address = ImageBase + XdataRva,
            Data = unwindData,
            Name = "Unwind data",
        });

        byte[] stack = new byte[2 * sizeof(ulong)];
        helpers.Write(stack.AsSpan(0, sizeof(ulong)), SavedX19);
        helpers.Write(stack.AsSpan(sizeof(ulong), sizeof(ulong)), SavedX20);
        targetBuilder.MemoryBuilder.AddHeapFragment(new MockMemorySpace.HeapFragment
        {
            Address = CurrentSp,
            Data = stack,
            Name = "Stack",
        });

        TargetCodePointer controlPc = new(ImageBase + FunctionStart + (8 * sizeof(uint)));
        CodeBlockHandle codeBlock = new(new TargetPointer(controlPc.Value));
        Mock<IExecutionManager> executionManager = new();
        executionManager.Setup(e => e.GetCodeBlockHandle(controlPc)).Returns(codeBlock);
        executionManager.Setup(e => e.GetUnwindInfoBaseAddress(codeBlock)).Returns(new TargetPointer(ImageBase));
        executionManager.Setup(e => e.GetUnwindInfo(codeBlock)).Returns(new TargetPointer(RuntimeFunctionAddress));

        TestPlaceholderTarget target = targetBuilder
            .AddTypes(new Dictionary<DataType, Target.TypeInfo>
            {
                [DataType.RuntimeFunction] = TargetTestHelpers.CreateTypeInfo(runtimeFunctionLayout),
            })
            .AddMockContract(executionManager.Object)
            .Build();

        ARM64Context context = new()
        {
            ContextFlags = (uint)ARM64Context.ContextFlagsValues.CONTEXT_FULL,
            X19 = ulong.MaxValue,
            X20 = ulong.MaxValue,
            Sp = CurrentSp,
            Lr = ReturnAddress,
            Pc = controlPc.Value,
        };

        Assert.True(new Contracts.StackWalkHelpers.ARM64.ARM64Unwinder(target).Unwind(ref context));
        Assert.Equal(SavedX19, context.X19);
        Assert.Equal(SavedX20, context.X20);
        Assert.Equal(CurrentSp, context.Sp);
        Assert.Equal(ReturnAddress, context.Pc);
        Assert.NotEqual(0u, context.ContextFlags & (uint)ARM64Context.ContextFlagsValues.CONTEXT_UNWOUND_TO_CALL);
    }

    [Fact]
    public void GenericContextStorage_PreservesRegisterRepresentation()
    {
        Assert.Equal(string.Empty, default(GenericContextStorage).RegisterName);

        GenericContextStorage named = new(GenericContextStorageKind.RegisterRelative, "ebp", -4);
        Assert.Equal("ebp", named.RegisterName);
        Assert.Equal(0u, named.RegisterNumber);

        GenericContextStorage numbered = new(GenericContextStorageKind.Register, 5u, 0);
        Assert.Equal(string.Empty, numbered.RegisterName);
        Assert.Equal(5u, numbered.RegisterNumber);
    }

    [Theory]
    [InlineData(0u, false)]
    [InlineData(0x08000000u, true)]
    [InlineData(0x08000001u, true)]
    public void HasFaultedContext_UsesExceptionActiveFlag(uint contextFlags, bool expected)
    {
        var context = new Mock<IPlatformAgnosticContext>();
        context.SetupGet(c => c.RawContextFlags).Returns(contextFlags);

        Assert.Equal(expected, StackWalk_1.HasFaultedContext(context.Object));
    }

    [Fact]
    public void GetStackSizeSkipped_ReturnsBytesSkippedByFiltering()
    {
        IXCLRDataStackWalk stackWalk = CreateClrDataStackWalk(
            new TestStackDataFrameHandle(StackWalkState.Frameless, 0x1000),
            new TestStackDataFrameHandle(StackWalkState.InitialNativeContext, 0x1100),
            new TestStackDataFrameHandle(StackWalkState.NativeMarker, 0x1200),
            new TestStackDataFrameHandle(StackWalkState.Frameless, 0x1500));

        ulong stackSizeSkipped = ulong.MaxValue;
        Assert.Equal(HResults.S_OK, stackWalk.GetStackSizeSkipped(&stackSizeSkipped));
        Assert.Equal(0ul, stackSizeSkipped);

        Assert.Equal(HResults.S_OK, stackWalk.Next());
        Assert.Equal(HResults.S_OK, stackWalk.GetStackSizeSkipped(&stackSizeSkipped));
        Assert.Equal(0x400ul, stackSizeSkipped);
    }

    private static IXCLRDataStackWalk CreateClrDataStackWalk(params TestStackDataFrameHandle[] frames)
    {
        TargetPointer threadAddress = new(0x1000);
        ThreadData threadData = new()
        {
            ThreadAddress = threadAddress,
        };

        var thread = new Mock<IThread>();
        thread.Setup(t => t.GetThreadData(threadAddress)).Returns(threadData);

        var stackWalk = new Mock<IStackWalk>();
        stackWalk.Setup(s => s.CreateStackWalk(threadData)).Returns(frames);
        stackWalk
            .Setup(s => s.GetStackPointer(It.IsAny<IStackDataFrameHandle>()))
            .Returns((IStackDataFrameHandle frame) => ((TestStackDataFrameHandle)frame).StackPointer);

        MockTarget.Architecture arch = new() { IsLittleEndian = true, Is64Bit = true };
        TestPlaceholderTarget target = new TestPlaceholderTarget.Builder(arch)
            .AddMockContract(thread)
            .AddMockContract(stackWalk)
            .Build();

        return new ClrDataStackWalk(threadAddress, CLRDataStackWalkFlag.CLRDATA_SIMPFRAME_RUNTIME_UNMANAGED_CODE, target, legacyImpl: null, new());
    }

    private sealed record TestStackDataFrameHandle(StackWalkState State, ulong StackPointerValue) : IStackDataFrameHandle
    {
        public TargetPointer StackPointer => new(StackPointerValue);
        public bool IsInterrupted => false;
        public bool HasFaulted => false;
        public bool IsExceptionFrame => false;
        public bool IsActiveFrame => false;
    }

    private static TestPlaceholderTarget CreateTarget(
        MockTarget.Architecture arch,
        Action<MockThreadBuilder> configure,
        Action<MockFrameBuilder>? configureFrames = null,
        RuntimeInfoArchitecture? runtimeArchitecture = null,
        Action<TestPlaceholderTarget.Builder>? configureTarget = null,
        IExecutionManager? executionManager = null)
    {
        TestPlaceholderTarget.Builder targetBuilder = new(arch);
        MockThreadBuilder threadBuilder = new(targetBuilder.MemoryBuilder);
        configure(threadBuilder);

        MockFrameBuilder? frameBuilder = null;
        if (configureFrames is not null)
        {
            frameBuilder = new MockFrameBuilder(targetBuilder.MemoryBuilder);
            configureFrames(frameBuilder);
        }

        targetBuilder
            .AddTypes(CreateThreadTypes(threadBuilder))
            .AddGlobals(
                (nameof(Constants.Globals.ThreadStore), threadBuilder.ThreadStoreGlobalAddress),
                (nameof(Constants.Globals.FinalizerThread), threadBuilder.FinalizerThreadGlobalAddress),
                (nameof(Constants.Globals.GCThread), threadBuilder.GCThreadGlobalAddress));

        if (frameBuilder is not null)
        {
            targetBuilder
                .AddTypes(CreateFrameTypes(frameBuilder))
                .AddGlobals(
                    ("InlinedCallFrameIdentifier", MockFrameBuilder.InlinedCallFrameIdentifierValue),
                    ("FramedMethodFrameIdentifier", MockFrameBuilder.FramedMethodFrameIdentifierValue),
                    ("FuncEvalFrameIdentifier", MockFrameBuilder.FuncEvalFrameIdentifierValue),
                    ("DebuggerExitFrameIdentifier", MockFrameBuilder.DebuggerExitFrameIdentifierValue),
                    ("PrestubMethodFrameIdentifier", MockFrameBuilder.PrestubMethodFrameIdentifierValue),
                    ("DebuggerClassInitMarkFrameIdentifier", MockFrameBuilder.DebuggerClassInitMarkFrameIdentifierValue),
                    ("SoftwareExceptionFrameIdentifier", MockFrameBuilder.SoftwareExceptionFrameIdentifierValue),
                    ("DebuggerU2MCatchHandlerFrameIdentifier", MockFrameBuilder.DebuggerU2MCatchHandlerFrameIdentifierValue),
                    ("InterpreterFrameIdentifier", MockFrameBuilder.InterpreterFrameIdentifierValue),
                    ("HijackFrameIdentifier", MockFrameBuilder.HijackFrameIdentifierValue));
        }

        // Some paths (e.g. the interpreter virtual unwind's first-argument-register lookup)
        // consult IRuntimeInfo for the target architecture. Register a mock when the test needs it.
        if (runtimeArchitecture is RuntimeInfoArchitecture rtArch)
        {
            targetBuilder.AddGlobalStrings((Constants.Globals.Architecture, rtArch.ToString().ToLowerInvariant()));

            Mock<IRuntimeInfo> runtimeInfo = new();
            runtimeInfo.Setup(r => r.GetTargetArchitecture()).Returns(rtArch);
            targetBuilder.AddMockContract(runtimeInfo.Object);
        }

        configureTarget?.Invoke(targetBuilder);

        return targetBuilder
            .AddContract<IThread>(version: "c1")
            .AddContract<IStackWalk>(version: "c1")
            // StackWalk_1's constructor reads these contracts via target.Contracts.{ExecutionManager,GCInfo}
            // when constructing its GcScanner. Our tests only exercise GetFrames /
            // IsExceptionHandlingHelperInlinedCallFrame / GetDebuggerEvalData, none of which
            // invoke ExecutionManager or GCInfo, so empty mocks satisfy construction.
            .AddMockContract(executionManager ?? Mock.Of<IExecutionManager>())
            .AddMockContract(Mock.Of<IGCInfo>())
            .Build();
    }

    private static Dictionary<DataType, Target.TypeInfo> CreateThreadTypes(MockThreadBuilder threadBuilder)
        => new()
        {
            [DataType.ExceptionInfo] = TargetTestHelpers.CreateTypeInfo(threadBuilder.ExceptionInfoLayout),
            [DataType.Thread] = TargetTestHelpers.CreateTypeInfo(threadBuilder.ThreadLayout),
            [DataType.ThreadStore] = TargetTestHelpers.CreateTypeInfo(threadBuilder.ThreadStoreLayout),
            [DataType.GCAllocContext] = TargetTestHelpers.CreateTypeInfo(threadBuilder.GCAllocContextLayout),
            [DataType.EEAllocContext] = TargetTestHelpers.CreateTypeInfo(threadBuilder.EEAllocContextLayout),
            [DataType.RuntimeThreadLocals] = TargetTestHelpers.CreateTypeInfo(threadBuilder.RuntimeThreadLocalsLayout),
        };

    private static Dictionary<DataType, Target.TypeInfo> CreateFrameTypes(MockFrameBuilder frameBuilder)
        => new()
        {
            [DataType.Frame] = TargetTestHelpers.CreateTypeInfo(frameBuilder.FrameLayout),
            [DataType.InlinedCallFrame] = TargetTestHelpers.CreateTypeInfo(frameBuilder.InlinedCallFrameLayout),
            [DataType.FramedMethodFrame] = TargetTestHelpers.CreateTypeInfo(frameBuilder.FramedMethodFrameLayout),
            [DataType.FuncEvalFrame] = TargetTestHelpers.CreateTypeInfo(frameBuilder.FuncEvalFrameLayout),
            [DataType.DebuggerEval] = TargetTestHelpers.CreateTypeInfo(frameBuilder.DebuggerEvalLayout),
            [DataType.InterpMethodContextFrame] = TargetTestHelpers.CreateTypeInfo(frameBuilder.InterpMethodContextFrameLayout),
        };

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetFrames_EmptyChain_ReturnsNothing(MockTarget.Architecture arch)
    {
        MockThread? thread = null;
        ulong terminator = arch.Is64Bit ? ulong.MaxValue : uint.MaxValue;

        TestPlaceholderTarget target = CreateTarget(
            arch,
            threadBuilder =>
            {
                thread = threadBuilder.AddThread(1, 1234);
                thread.Frame = terminator;
            },
            frameBuilder => { /* register layouts and identifiers, no frames */ });

        IStackWalk contract = target.Contracts.StackWalk;
        StackFrameData[] frames = contract.GetFrames(new TargetPointer(thread!.Address)).ToArray();
        Assert.Empty(frames);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetFrames_ClassifiesInternalFrameTypes(MockTarget.Architecture arch)
    {
        // Builds a chain whose frames exercise every InternalFrameType branch reachable
        // without the StubDispatchFrame layout. PrestubMethodFrame is a subclass of
        // FramedMethodFrame so it is allocated with the FramedMethodFrame layout but
        // overridden with the PrestubMethodFrameIdentifier.
        MockThread? thread = null;

        ulong framedMethodAddr = 0;
        ulong prestubAddr = 0;
        ulong funcEvalAddr = 0;
        ulong debuggerExitAddr = 0;
        ulong classInitAddr = 0;
        ulong softwareExAddr = 0;
        ulong u2mAddr = 0;
        ulong interpAddr = 0;
        ulong hijackAddr = 0;

        TestPlaceholderTarget target = CreateTarget(
            arch,
            threadBuilder => thread = threadBuilder.AddThread(1, 1234),
            frameBuilder =>
            {
                framedMethodAddr = frameBuilder.AddFramedMethodFrame(0x12345000).Address;

                MockFramedMethodFrame prestubFmf = frameBuilder.AddFramedMethodFrame(0);
                prestubFmf.Identifier = MockFrameBuilder.PrestubMethodFrameIdentifierValue;
                prestubAddr = prestubFmf.Address;

                funcEvalAddr = frameBuilder.AddFrame(MockFrameBuilder.FuncEvalFrameIdentifierValue, "FuncEvalFrame").Address;
                debuggerExitAddr = frameBuilder.AddFrame(MockFrameBuilder.DebuggerExitFrameIdentifierValue, "DebuggerExitFrame").Address;
                classInitAddr = frameBuilder.AddFrame(MockFrameBuilder.DebuggerClassInitMarkFrameIdentifierValue, "DebuggerClassInitMarkFrame").Address;
                softwareExAddr = frameBuilder.AddFrame(MockFrameBuilder.SoftwareExceptionFrameIdentifierValue, "SoftwareExceptionFrame").Address;
                u2mAddr = frameBuilder.AddFrame(MockFrameBuilder.DebuggerU2MCatchHandlerFrameIdentifierValue, "DebuggerU2MCatchHandlerFrame").Address;
                interpAddr = frameBuilder.AddFrame(MockFrameBuilder.InterpreterFrameIdentifierValue, "InterpreterFrame").Address;
                hijackAddr = frameBuilder.AddFrame(MockFrameBuilder.HijackFrameIdentifierValue, "HijackFrame").Address;

                thread!.Frame = frameBuilder.LinkChain(
                    framedMethodAddr, prestubAddr, funcEvalAddr, debuggerExitAddr,
                    classInitAddr, softwareExAddr, u2mAddr, interpAddr, hijackAddr);
            });

        IStackWalk contract = target.Contracts.StackWalk;
        StackFrameData[] frames = contract.GetFrames(new TargetPointer(thread!.Address)).ToArray();
        Assert.Equal(9, frames.Length);

        Assert.Equal(framedMethodAddr, frames[0].FrameAddress.Value);
        Assert.Equal(InternalFrameType.M2U, frames[0].InternalFrameType);

        Assert.Equal(prestubAddr, frames[1].FrameAddress.Value);
        Assert.Equal(InternalFrameType.JitCompilation, frames[1].InternalFrameType);

        Assert.Equal(funcEvalAddr, frames[2].FrameAddress.Value);
        Assert.Equal(InternalFrameType.FuncEval, frames[2].InternalFrameType);

        Assert.Equal(debuggerExitAddr, frames[3].FrameAddress.Value);
        Assert.Equal(InternalFrameType.M2U, frames[3].InternalFrameType);

        Assert.Equal(classInitAddr, frames[4].FrameAddress.Value);
        Assert.Equal(InternalFrameType.ClassInit, frames[4].InternalFrameType);

        Assert.Equal(softwareExAddr, frames[5].FrameAddress.Value);
        Assert.Equal(InternalFrameType.Exception, frames[5].InternalFrameType);

        Assert.Equal(u2mAddr, frames[6].FrameAddress.Value);
        Assert.Equal(InternalFrameType.U2M, frames[6].InternalFrameType);

        // InterpreterFrame classifies as M2U at the StackWalk layer; the
        // debugger-internal-frames consumer filters it out separately.
        Assert.Equal(interpAddr, frames[7].FrameAddress.Value);
        Assert.Equal(InternalFrameType.M2U, frames[7].InternalFrameType);

        Assert.Equal(hijackAddr, frames[8].FrameAddress.Value);
        Assert.Equal(InternalFrameType.None, frames[8].InternalFrameType);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void IsExceptionHandlingHelperInlinedCallFrame_DetectsMarkedActiveIcf(MockTarget.Architecture arch)
    {
        // Match enum class InlinedCallFrameMarker in src/coreclr/vm/exceptionhandling.h.
        const ulong ehMarker = 1;
        ulong activeReturnAddr = 0xCAFE_BABE;

        ulong ehHelperAddr = 0;
        TestPlaceholderTarget target = CreateTarget(
            arch,
            threadBuilder => threadBuilder.AddThread(1, 1234),
            frameBuilder =>
            {
                ehHelperAddr = frameBuilder.AddInlinedCallFrame(callerReturnAddress: activeReturnAddr, datum: ehMarker).Address;
            });

        IStackWalk contract = target.Contracts.StackWalk;
        Assert.True(contract.IsExceptionHandlingHelperInlinedCallFrame(new TargetPointer(ehHelperAddr)));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void IsExceptionHandlingHelperInlinedCallFrame_ReturnsFalseForPlainActiveIcf(MockTarget.Architecture arch)
    {
        ulong activeReturnAddr = 0xCAFE_BABE;

        ulong plainIcfAddr = 0;
        TestPlaceholderTarget target = CreateTarget(
            arch,
            threadBuilder => threadBuilder.AddThread(1, 1234),
            frameBuilder =>
            {
                plainIcfAddr = frameBuilder.AddInlinedCallFrame(callerReturnAddress: activeReturnAddr, datum: 0).Address;
            });

        IStackWalk contract = target.Contracts.StackWalk;
        Assert.False(contract.IsExceptionHandlingHelperInlinedCallFrame(new TargetPointer(plainIcfAddr)));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void IsExceptionHandlingHelperInlinedCallFrame_ReturnsFalseForInactiveIcf(MockTarget.Architecture arch)
    {
        const ulong ehMarker = 1;

        ulong inactiveAddr = 0;
        TestPlaceholderTarget target = CreateTarget(
            arch,
            threadBuilder => threadBuilder.AddThread(1, 1234),
            frameBuilder =>
            {
                // The marker is set but CallerReturnAddress == 0, so the frame is not active.
                inactiveAddr = frameBuilder.AddInlinedCallFrame(callerReturnAddress: 0, datum: ehMarker).Address;
            });

        IStackWalk contract = target.Contracts.StackWalk;
        Assert.False(contract.IsExceptionHandlingHelperInlinedCallFrame(new TargetPointer(inactiveAddr)));
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void IsExceptionHandlingHelperInlinedCallFrame_ReturnsFalseForNonIcf(MockTarget.Architecture arch)
    {
        ulong framedAddr = 0;
        TestPlaceholderTarget target = CreateTarget(
            arch,
            threadBuilder => threadBuilder.AddThread(1, 1234),
            frameBuilder =>
            {
                framedAddr = frameBuilder.AddFramedMethodFrame(0x9000).Address;
            });

        IStackWalk contract = target.Contracts.StackWalk;
        Assert.False(contract.IsExceptionHandlingHelperInlinedCallFrame(new TargetPointer(framedAddr)));
    }

    [Theory]
    [InlineData(RuntimeInfoArchitecture.X86, 0ul)]
    [InlineData(RuntimeInfoArchitecture.Arm, 0x1000ul)]
    public void GetMethodDescPtr_InlinedCallFrame_UsesX86StackSizeSentinel(RuntimeInfoArchitecture runtimeArchitecture, ulong expectedMethodDesc)
    {
        MockTarget.Architecture arch = new() { IsLittleEndian = true, Is64Bit = false };

        ulong inlinedCallFrameAddr = 0;
        TestPlaceholderTarget target = CreateTarget(
            arch,
            threadBuilder => threadBuilder.AddThread(1, 1234),
            frameBuilder =>
            {
                inlinedCallFrameAddr = frameBuilder.AddInlinedCallFrame(callerReturnAddress: 0xCAFE_BABE, datum: 0x1000).Address;
            },
            runtimeArchitecture: runtimeArchitecture);

        IStackWalk contract = target.Contracts.StackWalk;
        Assert.Equal(expectedMethodDesc, contract.GetMethodDescPtr(new TargetPointer(inlinedCallFrameAddr)).Value);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetDebuggerEvalData_ReturnsTokenAndAssemblyFromDebuggerEval(MockTarget.Architecture arch)
    {
        const uint expectedToken = 0x0600_0042;
        // Use a pointer-sized-safe value: the mock allocator writes _helpers.PointerSize
        // bytes for AssemblyPtr, so values must fit in 32 bits to remain consistent
        // across 32- and 64-bit architectures.
        const ulong expectedAssembly = 0x5678_9000;

        ulong funcEvalFrameAddr = 0;
        TestPlaceholderTarget target = CreateTarget(
            arch,
            threadBuilder => threadBuilder.AddThread(1, 1234),
            frameBuilder =>
            {
                MockDebuggerEval eval = frameBuilder.AddDebuggerEval(expectedToken, expectedAssembly);
                funcEvalFrameAddr = frameBuilder.AddFuncEvalFrame(eval.Address).Address;
            });

        IStackWalk contract = target.Contracts.StackWalk;
        DebuggerEvalData data = contract.GetDebuggerEvalData(new TargetPointer(funcEvalFrameAddr));

        Assert.Equal(expectedToken, data.MethodToken);
        Assert.Equal(expectedAssembly, data.AssemblyPtr.Value);
    }

    // WASM is a 32-bit little-endian target with no native register context; the initial
    // stack walk context is seeded from the Frame chain. This verifies that the degenerate
    // WasmContext is routed through WasmFrameHandler and that an active InlinedCallFrame at a
    // P/Invoke transition seeds the synthetic IP/SP/FP slots from CallSiteSP / CallerReturnAddress
    // / CalleeSavedFP -- the common context-seeding path on WASM.
    [Fact]
    public void UpdateContextFromFrame_WasmInlinedCallFrame_SeedsContextFromCallSiteSP()
    {
        MockTarget.Architecture wasmArch = new() { IsLittleEndian = true, Is64Bit = false };

        const ulong callSiteSP = 0x0004_1000;
        const ulong callerReturnAddress = 0x0004_2000;
        const ulong calleeSavedFP = 0x0004_3000;

        ulong icfAddr = 0;
        TestPlaceholderTarget target = CreateTarget(
            wasmArch,
            threadBuilder => threadBuilder.AddThread(1, 1234),
            frameBuilder =>
            {
                icfAddr = frameBuilder.AddInlinedCallFrame(callerReturnAddress, datum: 0, callSiteSP, calleeSavedFP).Address;
            });

        ContextHolder<WasmContext> context = new();
        FrameHelpers frameHelpers = new(target);
        Data.Frame frame = target.ProcessedData.GetOrAdd<Data.Frame>(icfAddr);
        frameHelpers.UpdateContextFromFrame(frame, context);

        Assert.Equal(callSiteSP, context.StackPointer.Value);
        Assert.Equal(callerReturnAddress, context.InstructionPointer.Value);
        Assert.Equal(calleeSavedFP, context.FramePointer.Value);
    }

    // The WasmContext mirrors the native wasm T_CONTEXT (src/coreclr/pal/inc/pal.h): five
    // 32-bit slots (ContextFlags, InterpreterWalkFramePointer, InterpreterSP/FP/IP). Verify the
    // serialized size and that the synthetic first-argument register (InterpreterWalkFramePointer)
    // and context flags round-trip.
    [Fact]
    public void WasmContext_MirrorsNativeLayoutAndRoundTripsRegisters()
    {
        WasmContext context = default;

        Assert.Equal(5u * sizeof(uint), context.Size);

        Assert.True(context.TrySetRegister(WasmContext.InterpreterWalkFramePointerRegister, new TargetNUInt(0x0004_9000)));
        Assert.True(context.TryReadRegister(WasmContext.InterpreterWalkFramePointerRegister, out TargetNUInt walkFp));
        Assert.Equal(0x0004_9000ul, walkFp.Value);

        context.StackPointer = new TargetPointer(0x0004_1000);
        context.InstructionPointer = new TargetCodePointer(0x0004_2000);
        context.FramePointer = new TargetPointer(0x0004_3000);
        context.RawContextFlags = 0x8000000; // CONTEXT_EXCEPTION_ACTIVE

        Assert.Equal(0x0004_1000ul, context.StackPointer.Value);
        Assert.Equal(0x0004_2000ul, context.InstructionPointer.Value);
        Assert.Equal(0x0004_3000ul, context.FramePointer.Value);
        Assert.Equal(0x8000000u, context.RawContextFlags);
    }

    // An InlinedCallFrame pushed by R2R code on WASM stores INLINED_PINVOKE_FROM_R2R (1) instead of
    // a return address. Like native InlinedCallFrame::UpdateRegDisplay_Impl, the handler takes SP
    // from CallSiteSP and derives the virtual IP from the R2R shadow frame at that SP.
    [Fact]
    public void UpdateContextFromFrame_WasmR2RInlinedCallFrame_DerivesVirtualIPFromCallSiteSP()
    {
        MockTarget.Architecture wasmArch = new() { IsLittleEndian = true, Is64Bit = false };
        const uint FunctionTableIndex = 5;
        const ulong MinVirtualIP = 0x0005_0000;
        const uint FunctionBeginAddress = 0x100;
        const uint LocalVirtualIPHalf = 3;

        ulong shadowFrameAddr = 0;
        MockInlinedCallFrame? icf = null;
        TestPlaceholderTarget target = CreateTarget(
            wasmArch,
            threadBuilder => threadBuilder.AddThread(1, 1234),
            frameBuilder => icf = frameBuilder.AddInlinedCallFrame(callerReturnAddress: 1, datum: 0, callSiteSP: 0, calleeSavedFP: 0xBAD0),
            configureTarget: targetBuilder =>
            {
                MockMemorySpace.BumpAllocator allocator = targetBuilder.MemoryBuilder.CreateAllocator(0x0010_0000, 0x0010_4000);
                AddWasmR2RFunction(targetBuilder, allocator, FunctionTableIndex, MinVirtualIP, FunctionBeginAddress);
                uint frameIdentity = AddWasmPortableEntryPoint(targetBuilder, allocator, FunctionTableIndex);

                MockMemorySpace.HeapFragment shadowFrame = allocator.Allocate(8, "R2RShadowFrame");
                targetBuilder.MemoryBuilder.TargetTestHelpers.Write(shadowFrame.Data.AsSpan(0, sizeof(uint)), frameIdentity);
                targetBuilder.MemoryBuilder.TargetTestHelpers.Write(shadowFrame.Data.AsSpan(4, sizeof(uint)), LocalVirtualIPHalf);
                shadowFrameAddr = shadowFrame.Address;
                icf!.CallSiteSP = shadowFrameAddr;
            });

        ContextHolder<WasmContext> context = new();
        FrameHelpers frameHelpers = new(target);
        Data.Frame frame = target.ProcessedData.GetOrAdd<Data.Frame>(icf!.Address);
        frameHelpers.UpdateContextFromFrame(frame, context);

        Assert.Equal(shadowFrameAddr, context.StackPointer.Value);
        Assert.Equal(MinVirtualIP + FunctionBeginAddress + LocalVirtualIPHalf * 2, context.InstructionPointer.Value);
        Assert.Equal(shadowFrameAddr, context.FramePointer.Value);
    }

    // A WASM walk seeded from the Frame chain must terminate when an active InlinedCallFrame does
    // not lead to managed code: either an R2R marker whose shadow frame yields no virtual IP, or a
    // return address outside any code range. WASM advertises the Debugger contract with a null
    // g_pDebugger (no in-process debugger), which reports no hijacks.
    [Theory]
    [InlineData(1ul)]
    [InlineData(0x0004_2000ul)]
    public void CreateStackWalk_WasmActiveInlinedCallFrameWithoutManagedCaller_Terminates(ulong callerReturnAddress)
    {
        MockTarget.Architecture wasmArch = new() { IsLittleEndian = true, Is64Bit = false };

        MockThread? thread = null;
        ulong icfAddr = 0;
        TestPlaceholderTarget target = CreateTarget(
            wasmArch,
            threadBuilder => thread = threadBuilder.AddThread(1, 1234),
            frameBuilder =>
            {
                // CallSiteSP below the linear-stack floor: no R2R shadow frame to resolve.
                icfAddr = frameBuilder.AddInlinedCallFrame(callerReturnAddress, datum: 0, callSiteSP: 0x800).Address;
            },
            runtimeArchitecture: RuntimeInfoArchitecture.Wasm,
            configureTarget: targetBuilder =>
            {
                MockMemorySpace.BumpAllocator allocator = targetBuilder.MemoryBuilder.CreateAllocator(0x0010_0000, 0x0010_4000);
                AddWasmR2RFunction(targetBuilder, allocator, functionTableIndex: 5, minVirtualIP: 0x0005_0000, functionBeginAddress: 0x100);
                AddWasmNullDebugger(targetBuilder, allocator);
            });
        thread!.Frame = icfAddr;

        IStackWalk stackWalk = target.Contracts.StackWalk;
        ThreadData threadData = target.Contracts.Thread.GetThreadData(new TargetPointer(thread.Address));
        IStackDataFrameHandle[] frames = stackWalk.CreateStackWalk(threadData).Take(16).ToArray();

        Assert.InRange(frames.Length, 1, 2);
        Assert.Equal(icfAddr, stackWalk.GetFrameAddress(frames[^1]).Value);
    }

    // An interpreted P/Invoke pushes an active InlinedCallFrame whose CallSiteSP is the top
    // InterpMethodContextFrame of the owning InterpreterFrame that follows it. Like native
    // InlinedCallFrame::IsInInterpreter handling, the walk must move from that ICF to the
    // InterpreterFrame and walk its interpreted chain exactly once, whether it is seeded from a
    // native context below the ICF or from the Frame chain (which yields the ICF's context).
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CreateStackWalk_WasmInterpretedPInvoke_WalksInterpretedChainOnce(bool seedFromNativeContext)
    {
        MockTarget.Architecture wasmArch = new() { IsLittleEndian = true, Is64Bit = false };
        const ulong InterpIp1 = 0x0005_1000;
        const ulong InterpIp2 = 0x0005_2000;
        const ulong NativeCallerIp = 0x0009_0000;

        MockThread? thread = null;
        MockFrameBuilder? frames = null;
        ulong imcfLeaf = 0;
        ulong icfAddr = 0;
        ulong interpreterFrameAddr = 0;
        TestPlaceholderTarget target = CreateTarget(
            wasmArch,
            threadBuilder => thread = threadBuilder.AddThread(1, 1234),
            frameBuilder =>
            {
                frames = frameBuilder;
                ulong imcfRoot = frameBuilder.AddInterpMethodContextFrame(parentPtr: 0, ip: InterpIp2, stack: 0x0006_2000).Address;
                imcfLeaf = frameBuilder.AddInterpMethodContextFrame(parentPtr: imcfRoot, ip: InterpIp1, stack: 0x0006_1000).Address;
            },
            runtimeArchitecture: RuntimeInfoArchitecture.Wasm,
            configureTarget: targetBuilder =>
            {
                TargetTestHelpers helpers = targetBuilder.MemoryBuilder.TargetTestHelpers;
                int pointerSize = helpers.PointerSize;
                MockMemorySpace.BumpAllocator allocator = targetBuilder.MemoryBuilder.CreateAllocator(0x0020_0000, 0x0020_4000);
                ulong terminator = uint.MaxValue;
                AddWasmR2RFunction(targetBuilder, allocator, functionTableIndex: 5, minVirtualIP: 0x0005_0000, functionBeginAddress: 0x100);
                AddWasmNullDebugger(targetBuilder, allocator);

                ulong transitionBlock = AddWasmTransitionBlock(helpers, allocator, NativeCallerIp, stackPointer: 0);

                // InterpreterFrame derives from FramedMethodFrame.
                Layout<MockFramedMethodFrame> fmfLayout = frames!.FramedMethodFrameLayout;
                int topOffset = fmfLayout.Size;
                int isFaultingOffset = topOffset + pointerSize;
                Dictionary<string, Target.FieldInfo> interpreterFrameFields = new(TargetTestHelpers.CreateTypeInfo(fmfLayout).Fields)
                {
                    [nameof(Data.InterpreterFrame.TopInterpMethodContextFrame)] = new() { Offset = topOffset },
                    [nameof(Data.InterpreterFrame.IsFaulting)] = new() { Offset = isFaultingOffset },
                };
                MockMemorySpace.HeapFragment interpreterFrame = allocator.Allocate((ulong)(isFaultingOffset + pointerSize), "InterpreterFrame");
                MockFramedMethodFrame fmf = fmfLayout.Create(interpreterFrame);
                fmf.Identifier = MockFrameBuilder.InterpreterFrameIdentifierValue;
                fmf.Next = terminator;
                helpers.WritePointer(interpreterFrame.Data.AsSpan(fmfLayout.Fields.Single(f => f.Name == nameof(Data.FramedMethodFrame.TransitionBlockPtr)).Offset, pointerSize), transitionBlock);
                helpers.WritePointer(interpreterFrame.Data.AsSpan(topOffset, pointerSize), imcfLeaf);
                interpreterFrameAddr = interpreterFrame.Address;

                // The ICF is pushed below (at a lower address than) the owning InterpreterFrame.
                Layout<MockInlinedCallFrame> icfLayout = frames.InlinedCallFrameLayout;
                MockMemorySpace.HeapFragment icfFragment = targetBuilder.MemoryBuilder.CreateAllocator(0x001F_0000, 0x001F_1000).Allocate((ulong)icfLayout.Size, "InlinedCallFrame");
                MockInlinedCallFrame icf = icfLayout.Create(icfFragment);
                icf.Identifier = MockFrameBuilder.InlinedCallFrameIdentifierValue;
                icf.Next = interpreterFrameAddr;
                icf.CallerReturnAddress = InterpIp1;
                icf.CallSiteSP = imcfLeaf;
                icfAddr = icf.Address;
                thread!.Frame = icfAddr;

                targetBuilder
                    .AddTypes(CreateWasmTransitionBlockTypes(pointerSize))
                    .AddTypes(new Dictionary<DataType, Target.TypeInfo>
                    {
                        [DataType.InterpreterFrame] = new() { Fields = interpreterFrameFields, Size = (uint)(isFaultingOffset + pointerSize) },
                    });
            },
            executionManager: CreateInterpreterExecutionManager([InterpIp1, InterpIp2]));

        IStackWalk stackWalk = target.Contracts.StackWalk;
        ThreadData threadData = target.Contracts.Thread.GetThreadData(new TargetPointer(thread!.Address));
        IEnumerable<IStackDataFrameHandle> walk;
        if (seedFromNativeContext)
        {
            ContextHolder<WasmContext> nativeContext = new() { InstructionPointer = new TargetCodePointer(0x0009_9000) };
            walk = stackWalk.CreateStackWalk(threadData, nativeContext.GetBytes());
        }
        else
        {
            walk = stackWalk.CreateStackWalk(threadData);
        }

        IStackDataFrameHandle[] walked = walk.Take(32).ToArray();
        ulong[] interpretedIps = walked
            .Select(f => stackWalk.GetInstructionPointer(f).Value)
            .Where(ip => ip is InterpIp1 or InterpIp2)
            .ToArray();
        ulong[] explicitFrames = walked
            .Select(f => stackWalk.GetFrameAddress(f).Value)
            .Where(a => a != 0)
            .ToArray();

        Assert.Equal([InterpIp1, InterpIp2], interpretedIps);
        Assert.Equal(NativeCallerIp, stackWalk.GetInstructionPointer(walked[^1]).Value);
        if (seedFromNativeContext)
        {
            Assert.Equal([icfAddr, interpreterFrameAddr], explicitFrames.Distinct());
        }
        Assert.True(walked.Length <= 8, $"Walk did not terminate: {walked.Length} frames");
    }

    // A WASM TransitionFrame mirrors native TransitionFrame::GetSP / UpdateRegDisplay_Impl: when the
    // helper recorded the caller's R2R linear-stack pointer and a return address is known (stored, or
    // derived lazily from that stack pointer as in FramedMethodFrame::GetTransitionBlock_Impl), the
    // caller is the R2R frame at that stack pointer; otherwise it is the end of the TransitionBlock.
    [Theory]
    [InlineData(0ul, true, true)]
    [InlineData(0x0009_0000ul, true, false)]
    [InlineData(0x0009_0000ul, false, false)]
    [InlineData(0ul, false, false)]
    public void UpdateContextFromFrame_WasmTransitionFrame_MirrorsNativeTransitionBlock(ulong returnAddress, bool hasStackPointer, bool expectDerivedIP)
    {
        MockTarget.Architecture wasmArch = new() { IsLittleEndian = true, Is64Bit = false };
        const uint FunctionTableIndex = 5;
        const ulong MinVirtualIP = 0x8001_0000;
        const uint FunctionBeginAddress = 0x100;
        const uint LocalVirtualIPHalf = 3;

        MockFramedMethodFrame? framedMethodFrame = null;
        ulong r2rFrame = 0;
        ulong transitionBlock = 0;
        TestPlaceholderTarget target = CreateTarget(
            wasmArch,
            threadBuilder => threadBuilder.AddThread(1, 1234),
            frameBuilder => framedMethodFrame = frameBuilder.AddFramedMethodFrame(methodDescPtr: 0),
            runtimeArchitecture: RuntimeInfoArchitecture.Wasm,
            configureTarget: targetBuilder =>
            {
                TargetTestHelpers helpers = targetBuilder.MemoryBuilder.TargetTestHelpers;
                MockMemorySpace.BumpAllocator allocator = targetBuilder.MemoryBuilder.CreateAllocator(0x0020_0000, 0x0020_4000);
                AddWasmR2RFunction(targetBuilder, allocator, FunctionTableIndex, MinVirtualIP, FunctionBeginAddress);
                uint frameIdentity = AddWasmPortableEntryPoint(targetBuilder, allocator, FunctionTableIndex);
                r2rFrame = AddWasmR2RFrameRecord(helpers, allocator, frameIdentity, LocalVirtualIPHalf);
                transitionBlock = AddWasmTransitionBlock(helpers, allocator, returnAddress, hasStackPointer ? r2rFrame : 0);
                helpers.WritePointer(
                    framedMethodFrame!.Memory.Span.Slice(framedMethodFrame.Layout.GetField("TransitionBlockPtr").Offset, helpers.PointerSize),
                    transitionBlock);
                targetBuilder.AddTypes(CreateWasmTransitionBlockTypes(helpers.PointerSize));
            });

        ContextHolder<WasmContext> context = new();
        FrameHelpers frameHelpers = new(target);
        Data.Frame frame = target.ProcessedData.GetOrAdd<Data.Frame>(framedMethodFrame!.Address);
        frameHelpers.UpdateContextFromFrame(frame, context);

        ulong expectedIP = expectDerivedIP ? MinVirtualIP + FunctionBeginAddress + LocalVirtualIPHalf * 2 : returnAddress;
        bool callerIsR2R = hasStackPointer && expectedIP != 0;
        Assert.Equal(expectedIP, context.InstructionPointer.Value);
        Assert.Equal(expectedIP, frameHelpers.GetReturnAddress(frame).Value);
        Assert.Equal(callerIsR2R ? r2rFrame : transitionBlock + 2 * sizeof(uint), context.StackPointer.Value);
        Assert.Equal(callerIsR2R ? r2rFrame : 0ul, context.FramePointer.Value);
    }

    // A funclet's frame pointer is its establishing method's frame, not its own record (native
    // GetWasmFramePointerFromStackPointer). The funclet identity contains that frame pointer
    // tagged with +2. Both frame kinds that seed from an R2R stack pointer (a transition helper's
    // TransitionBlock and an R2R inlined P/Invoke) must report the establishing pointer.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void UpdateContextFromFrame_WasmFuncletCaller_ReportsEstablishingFramePointer(bool transitionFrame)
    {
        MockTarget.Architecture wasmArch = new() { IsLittleEndian = true, Is64Bit = false };
        const uint ParentIndex = 5;
        const ulong MinVirtualIP = 0x8001_0000;
        const uint ParentBegin = 0x100;
        const uint FuncletVipHalf = 0x21;
        const byte FuncletFrameSize = 16; // AlignUp(2 * TARGET_POINTER_SIZE, STACK_ALIGN) on wasm32
        const ulong EstablishingFp = 0x0021_0F00;

        MockFramedMethodFrame? framedMethodFrame = null;
        MockInlinedCallFrame? inlinedCallFrame = null;
        ulong funcletFrame = 0;
        TestPlaceholderTarget target = CreateTarget(
            wasmArch,
            threadBuilder => threadBuilder.AddThread(1, 1234),
            frameBuilder =>
            {
                if (transitionFrame)
                    framedMethodFrame = frameBuilder.AddFramedMethodFrame(methodDescPtr: 0);
                else
                    inlinedCallFrame = frameBuilder.AddInlinedCallFrame(callerReturnAddress: 1, datum: 0);
            },
            runtimeArchitecture: RuntimeInfoArchitecture.Wasm,
            configureTarget: targetBuilder =>
            {
                TargetTestHelpers helpers = targetBuilder.MemoryBuilder.TargetTestHelpers;
                MockMemorySpace.BumpAllocator allocator = targetBuilder.MemoryBuilder.CreateAllocator(0x0020_0000, 0x0020_4000);
                AddWasmR2RFunctions(targetBuilder, allocator, ParentIndex, MinVirtualIP,
                    [(ParentBegin, 0x30), (0x8000_0000 | (ParentBegin + 0x40), FuncletFrameSize)]);
                uint parentFrameIdentity = AddWasmPortableEntryPoint(targetBuilder, allocator, ParentIndex);

                byte[] establishingFrame = new byte[8];
                helpers.Write(establishingFrame.AsSpan(0, sizeof(uint)), parentFrameIdentity);
                targetBuilder.MemoryBuilder.AddHeapFragment(new MockMemorySpace.HeapFragment
                {
                    Address = EstablishingFp,
                    Data = establishingFrame,
                    Name = "EstablishingFrame",
                });

                // The funclet's identity directly references its establishing frame.
                MockMemorySpace.HeapFragment stack = allocator.Allocate(FuncletFrameSize + 2 * sizeof(uint), "LinearStack");
                helpers.Write(stack.Data.AsSpan(0, sizeof(uint)), checked((uint)EstablishingFp + 2));
                helpers.Write(stack.Data.AsSpan(4, sizeof(uint)), FuncletVipHalf);
                helpers.Write(stack.Data.AsSpan(FuncletFrameSize, sizeof(uint)), 1u);
                funcletFrame = stack.Address;

                if (transitionFrame)
                {
                    ulong transitionBlock = AddWasmTransitionBlock(helpers, allocator, returnAddress: 0, stackPointer: funcletFrame);
                    helpers.WritePointer(
                        framedMethodFrame!.Memory.Span.Slice(framedMethodFrame.Layout.GetField("TransitionBlockPtr").Offset, helpers.PointerSize),
                        transitionBlock);
                    targetBuilder.AddTypes(CreateWasmTransitionBlockTypes(helpers.PointerSize));
                }
                else
                {
                    inlinedCallFrame!.CallSiteSP = funcletFrame;
                }
            });

        ContextHolder<WasmContext> context = new();
        FrameHelpers frameHelpers = new(target);
        ulong frameAddress = transitionFrame ? framedMethodFrame!.Address : inlinedCallFrame!.Address;
        frameHelpers.UpdateContextFromFrame(target.ProcessedData.GetOrAdd<Data.Frame>(frameAddress), context);

        // Funclet virtual IPs are relative to the controlling method's base.
        Assert.Equal(MinVirtualIP + ParentBegin + FuncletVipHalf * 2, context.InstructionPointer.Value);
        Assert.Equal(funcletFrame, context.StackPointer.Value);
        Assert.Equal(EstablishingFp, context.FramePointer.Value);
    }

    // R2R code that enters an interpreted method through a portable-entry-point thunk leaves an
    // InterpreterFrame whose TransitionBlock records only the R2R caller's linear-stack pointer.
    // When the interpreted chain is exhausted, the walk must continue into that R2R caller rather
    // than reporting a native marker at the end of the TransitionBlock and skipping the R2R frames.
    // Each R2R frame's frame pointer is recomputed from its own stack pointer as the walk unwinds.
    [Fact]
    public void CreateStackWalk_WasmInterpreterFrameEnteredFromR2R_ContinuesIntoR2RCaller()
    {
        MockTarget.Architecture wasmArch = new() { IsLittleEndian = true, Is64Bit = false };
        const ulong InterpIp1 = 0x0005_1000;
        const ulong InterpIp2 = 0x0005_2000;
        const uint FunctionTableIndex = 5;
        const ulong MinVirtualIP = 0x8001_0000;
        const uint FunctionBeginAddress = 0x100;
        const uint LocalVirtualIPHalf = 3;
        const uint OuterLocalVirtualIPHalf = 5;
        const byte R2RFrameSize = 16;
        const ulong R2RCallerIp = MinVirtualIP + FunctionBeginAddress + LocalVirtualIPHalf * 2;
        const ulong OuterR2RCallerIp = MinVirtualIP + FunctionBeginAddress + OuterLocalVirtualIPHalf * 2;

        MockThread? thread = null;
        MockFrameBuilder? frames = null;
        ulong imcfLeaf = 0;
        ulong r2rFrame = 0;
        TargetPointer runtimeFunctionAddress = TargetPointer.Null;
        IExecutionManager executionManager = CreateInterpreterExecutionManager(
            [InterpIp1, InterpIp2],
            managedIps: [R2RCallerIp, OuterR2RCallerIp],
            unwindInfo: () => (runtimeFunctionAddress, TargetPointer.Null));
        TestPlaceholderTarget target = CreateTarget(
            wasmArch,
            threadBuilder => thread = threadBuilder.AddThread(1, 1234),
            frameBuilder =>
            {
                frames = frameBuilder;
                ulong imcfRoot = frameBuilder.AddInterpMethodContextFrame(parentPtr: 0, ip: InterpIp2, stack: 0x0006_2000).Address;
                imcfLeaf = frameBuilder.AddInterpMethodContextFrame(parentPtr: imcfRoot, ip: InterpIp1, stack: 0x0006_1000).Address;
            },
            runtimeArchitecture: RuntimeInfoArchitecture.Wasm,
            configureTarget: targetBuilder =>
            {
                TargetTestHelpers helpers = targetBuilder.MemoryBuilder.TargetTestHelpers;
                int pointerSize = helpers.PointerSize;
                MockMemorySpace.BumpAllocator allocator = targetBuilder.MemoryBuilder.CreateAllocator(0x0020_0000, 0x0020_4000);
                runtimeFunctionAddress = AddWasmR2RFunction(
                    targetBuilder,
                    allocator,
                    FunctionTableIndex,
                    MinVirtualIP,
                    FunctionBeginAddress,
                    R2RFrameSize);
                uint frameIdentity = AddWasmPortableEntryPoint(targetBuilder, allocator, FunctionTableIndex);
                AddWasmNullDebugger(targetBuilder, allocator);

                // Two R2R frame records, each frame size apart, then a TERMINATE_R2R_STACK_WALK
                // marker, so the walk ends after the outer caller.
                MockMemorySpace.HeapFragment r2rStack = allocator.Allocate(2 * R2RFrameSize + 4, "R2RLinearStack");
                helpers.Write(r2rStack.Data.AsSpan(0, sizeof(uint)), frameIdentity);
                helpers.Write(r2rStack.Data.AsSpan(4, sizeof(uint)), LocalVirtualIPHalf);
                helpers.Write(r2rStack.Data.AsSpan(R2RFrameSize, sizeof(uint)), frameIdentity);
                helpers.Write(r2rStack.Data.AsSpan(R2RFrameSize + 4, sizeof(uint)), OuterLocalVirtualIPHalf);
                helpers.Write(r2rStack.Data.AsSpan(2 * R2RFrameSize, sizeof(uint)), 1u);
                r2rFrame = r2rStack.Address;

                ulong transitionBlock = AddWasmTransitionBlock(helpers, allocator, returnAddress: 0, stackPointer: r2rFrame);

                Layout<MockFramedMethodFrame> fmfLayout = frames!.FramedMethodFrameLayout;
                int topOffset = fmfLayout.Size;
                int isFaultingOffset = topOffset + pointerSize;
                Dictionary<string, Target.FieldInfo> interpreterFrameFields = new(TargetTestHelpers.CreateTypeInfo(fmfLayout).Fields)
                {
                    [nameof(Data.InterpreterFrame.TopInterpMethodContextFrame)] = new() { Offset = topOffset },
                    [nameof(Data.InterpreterFrame.IsFaulting)] = new() { Offset = isFaultingOffset },
                };
                MockMemorySpace.HeapFragment interpreterFrame = allocator.Allocate((ulong)(isFaultingOffset + pointerSize), "InterpreterFrame");
                MockFramedMethodFrame fmf = fmfLayout.Create(interpreterFrame);
                fmf.Identifier = MockFrameBuilder.InterpreterFrameIdentifierValue;
                fmf.Next = uint.MaxValue;
                helpers.WritePointer(interpreterFrame.Data.AsSpan(fmfLayout.Fields.Single(f => f.Name == nameof(Data.FramedMethodFrame.TransitionBlockPtr)).Offset, pointerSize), transitionBlock);
                helpers.WritePointer(interpreterFrame.Data.AsSpan(topOffset, pointerSize), imcfLeaf);
                thread!.Frame = interpreterFrame.Address;

                targetBuilder
                    .AddTypes(CreateWasmTransitionBlockTypes(pointerSize))
                    .AddTypes(new Dictionary<DataType, Target.TypeInfo>
                    {
                        [DataType.InterpreterFrame] = new() { Fields = interpreterFrameFields, Size = (uint)(isFaultingOffset + pointerSize) },
                    });
            },
            executionManager: executionManager);

        IStackWalk stackWalk = target.Contracts.StackWalk;
        ThreadData threadData = target.Contracts.Thread.GetThreadData(new TargetPointer(thread!.Address));
        IStackDataFrameHandle[] walked = stackWalk.CreateStackWalk(threadData).Take(16).ToArray();

        (ulong Ip, ulong Sp, ulong Fp)[] frameless = walked
            .Where(f => f.State == StackWalkState.Frameless)
            .Select(f => (stackWalk.GetInstructionPointer(f).Value, stackWalk.GetStackPointer(f).Value, stackWalk.GetContextFramePointer(f).Value))
            .ToArray();

        Assert.Equal([InterpIp1, InterpIp2, R2RCallerIp, OuterR2RCallerIp], frameless.Select(f => f.Ip));
        Assert.Equal((r2rFrame, r2rFrame), (frameless[2].Sp, frameless[2].Fp));
        Assert.Equal((r2rFrame + R2RFrameSize, r2rFrame + R2RFrameSize), (frameless[3].Sp, frameless[3].Fp));
        Assert.True(walked.Length <= 7, $"Walk did not terminate: {walked.Length} frames");
    }

    // Unwinding keeps the caller's SP when the caller is not R2R, so the walk passes the
    // InlinedCallFrame in the method's own frame and reports the method once.
    [Fact]
    public void CreateStackWalk_WasmR2RMethodWithNonR2RCaller_PassesInlinedCallFrameInItsFrame()
    {
        MockTarget.Architecture wasmArch = new() { IsLittleEndian = true, Is64Bit = false };
        const uint FunctionTableIndex = 5;
        const ulong MinVirtualIP = 0x8001_0000;
        const uint FunctionBeginAddress = 0x100;
        const uint LocalVirtualIPHalf = 3;
        const byte R2RFrameSize = 0x70;
        const ulong R2RIp = MinVirtualIP + FunctionBeginAddress + LocalVirtualIPHalf * 2;
        const ulong InlinedPInvokeFromR2R = 1;
        const uint TerminateR2RStackWalk = 1;

        MockThread? thread = null;
        MockInlinedCallFrame? icf = null;
        int icfSize = 0;
        ulong r2rFrame = 0;
        TargetPointer runtimeFunctionAddress = TargetPointer.Null;
        TestPlaceholderTarget target = CreateTarget(
            wasmArch,
            threadBuilder => thread = threadBuilder.AddThread(1, 1234),
            frameBuilder =>
            {
                icf = frameBuilder.AddInlinedCallFrame(InlinedPInvokeFromR2R, datum: 0);
                icfSize = frameBuilder.InlinedCallFrameLayout.Size;
            },
            runtimeArchitecture: RuntimeInfoArchitecture.Wasm,
            configureTarget: targetBuilder =>
            {
                TargetTestHelpers helpers = targetBuilder.MemoryBuilder.TargetTestHelpers;
                MockMemorySpace.BumpAllocator allocator = targetBuilder.MemoryBuilder.CreateAllocator(0x0020_0000, 0x0020_4000);
                runtimeFunctionAddress = AddWasmR2RFunction(targetBuilder, allocator, FunctionTableIndex, MinVirtualIP, FunctionBeginAddress, R2RFrameSize);
                uint frameIdentity = AddWasmPortableEntryPoint(targetBuilder, allocator, FunctionTableIndex);
                AddWasmNullDebugger(targetBuilder, allocator);

                // The method's frame record sits just below the InlinedCallFrame, and its caller's
                // stack pointer is above it. The caller slot holds TERMINATE_R2R_STACK_WALK, so the
                // caller has no R2R virtual IP.
                r2rFrame = icf!.Address - 0x10;
                ulong callerSp = r2rFrame + R2RFrameSize;
                Assert.True(icf.Address + (ulong)icfSize <= callerSp);

                byte[] record = new byte[8];
                helpers.Write(record.AsSpan(0, sizeof(uint)), frameIdentity);
                helpers.Write(record.AsSpan(4, sizeof(uint)), LocalVirtualIPHalf);
                targetBuilder.MemoryBuilder.AddHeapFragment(new MockMemorySpace.HeapFragment { Address = r2rFrame, Data = record, Name = "R2RFrame" });

                byte[] callerRecord = new byte[8];
                helpers.Write(callerRecord.AsSpan(0, sizeof(uint)), TerminateR2RStackWalk);
                targetBuilder.MemoryBuilder.AddHeapFragment(new MockMemorySpace.HeapFragment { Address = callerSp, Data = callerRecord, Name = "NonR2RCaller" });

                icf.CallSiteSP = r2rFrame;
                thread!.Frame = icf.Address;
            },
            executionManager: CreateInterpreterExecutionManager(
                [],
                managedIps: [R2RIp],
                unwindInfo: () => (runtimeFunctionAddress, TargetPointer.Null)));

        IStackWalk stackWalk = target.Contracts.StackWalk;
        ThreadData threadData = target.Contracts.Thread.GetThreadData(new TargetPointer(thread!.Address));
        IStackDataFrameHandle[] walked = stackWalk.CreateStackWalk(threadData).Take(16).ToArray();

        (ulong Ip, ulong Sp)[] frameless = walked
            .Where(f => f.State == StackWalkState.Frameless)
            .Select(f => (stackWalk.GetInstructionPointer(f).Value, stackWalk.GetStackPointer(f).Value))
            .ToArray();

        Assert.Equal([(R2RIp, r2rFrame)], frameless);
        Assert.True(walked.Length <= 3, $"Walk did not terminate: {walked.Length} frames");
    }

    private sealed class InterpreterExecutionManager : IExecutionManager
    {
        private readonly ulong[] _interpreterIps;
        private readonly ulong[]? _managedIps;
        private readonly Func<(TargetPointer RuntimeFunction, TargetPointer BaseAddress)>? _unwindInfo;

        public InterpreterExecutionManager(
            ulong[] interpreterIps,
            ulong[]? managedIps,
            Func<(TargetPointer RuntimeFunction, TargetPointer BaseAddress)>? unwindInfo)
        {
            _interpreterIps = interpreterIps;
            _managedIps = managedIps;
            _unwindInfo = unwindInfo;
        }

        public CodeBlockHandle? GetCodeBlockHandle(TargetCodePointer ip)
            => _interpreterIps.Contains(ip.Value) || (_managedIps?.Contains(ip.Value) ?? false)
                ? new CodeBlockHandle(new TargetPointer(ip.Value))
                : null;

        public CodeKind GetCodeKind(TargetCodePointer codeAddress)
            => _interpreterIps.Contains(codeAddress.Value) ? CodeKind.Interpreter : default;

        public TargetPointer GetUnwindInfo(CodeBlockHandle codeInfoHandle)
            => (_managedIps?.Contains(codeInfoHandle.Address.Value) ?? false) && _unwindInfo is not null
                ? _unwindInfo().RuntimeFunction
                : TargetPointer.Null;

        public TargetPointer GetUnwindInfoBaseAddress(CodeBlockHandle codeInfoHandle)
            => _unwindInfo?.Invoke().BaseAddress ?? TargetPointer.Null;
    }

    private static IExecutionManager CreateInterpreterExecutionManager(
        ulong[] interpreterIps,
        ulong[]? managedIps = null,
        Func<(TargetPointer RuntimeFunction, TargetPointer BaseAddress)>? unwindInfo = null)
        => new InterpreterExecutionManager(interpreterIps, managedIps, unwindInfo);

    // WASM TransitionBlock: m_ReturnAddress, then m_StackPointer. Matches the browser-wasm data
    // descriptor, where ArgumentRegisters and FirstGCRefMapSlot are sizeof(TransitionBlock).
    private static Dictionary<DataType, Target.TypeInfo> CreateWasmTransitionBlockTypes(int pointerSize)
        => new()
        {
            [DataType.TransitionBlock] = new()
            {
                Fields = new Dictionary<string, Target.FieldInfo>
                {
                    [nameof(Data.TransitionBlock.ReturnAddress)] = new() { Offset = 0 },
                    [nameof(Data.TransitionBlock.StackPointer)] = new() { Offset = pointerSize },
                    [nameof(Data.TransitionBlock.CalleeSavedRegisters)] = new() { Offset = 0 },
                    [nameof(Data.TransitionBlock.ArgumentRegisters)] = new() { Offset = 2 * pointerSize },
                    [nameof(Data.TransitionBlock.FirstGCRefMapSlot)] = new() { Offset = 2 * pointerSize },
                },
                Size = (uint)(2 * pointerSize),
            },
            [DataType.CalleeSavedRegisters] = new() { Fields = new Dictionary<string, Target.FieldInfo>(), Size = 0 },
        };

    private static ulong AddWasmTransitionBlock(TargetTestHelpers helpers, MockMemorySpace.BumpAllocator allocator, ulong returnAddress, ulong stackPointer)
    {
        int pointerSize = helpers.PointerSize;
        MockMemorySpace.HeapFragment transitionBlock = allocator.Allocate((ulong)(2 * pointerSize), "TransitionBlock");
        helpers.WritePointer(transitionBlock.Data.AsSpan(0, pointerSize), returnAddress);
        helpers.WritePointer(transitionBlock.Data.AsSpan(pointerSize, pointerSize), stackPointer);
        return transitionBlock.Address;
    }

    // An R2R linear-stack frame record: the encoded identity, then the function-local virtual IP / 2.
    private static ulong AddWasmR2RFrameRecord(TargetTestHelpers helpers, MockMemorySpace.BumpAllocator allocator, uint frameIdentity, uint localVirtualIPHalf)
    {
        MockMemorySpace.HeapFragment frame = allocator.Allocate(8, "R2RShadowFrame");
        helpers.Write(frame.Data.AsSpan(0, sizeof(uint)), frameIdentity);
        helpers.Write(frame.Data.AsSpan(4, sizeof(uint)), localVirtualIPHalf);
        return frame.Address;
    }

    private static uint AddWasmPortableEntryPoint(
        TestPlaceholderTarget.Builder targetBuilder,
        MockMemorySpace.BumpAllocator allocator,
        uint functionTableIndex)
    {
        TargetTestHelpers helpers = targetBuilder.MemoryBuilder.TargetTestHelpers;
        TargetTestHelpers.LayoutResult layout = helpers.LayoutFields([
            new(nameof(Data.PortableEntryPoint.ActualCode), DataType.pointer),
            new(nameof(Data.PortableEntryPoint.MethodDesc), DataType.pointer),
            new(nameof(Data.PortableEntryPoint.Flags), DataType.int32),
        ]);
        MockMemorySpace.HeapFragment portableEntryPoint = allocator.Allocate(layout.Stride, "PortableEntryPoint");
        helpers.WritePointer(
            portableEntryPoint.Data.AsSpan(
                layout.Fields[nameof(Data.PortableEntryPoint.ActualCode)].Offset,
                helpers.PointerSize),
            functionTableIndex);
        targetBuilder.AddTypes(new Dictionary<DataType, Target.TypeInfo>
        {
            [DataType.PortableEntryPoint] = new() { Fields = layout.Fields, Size = layout.Stride },
        });
        return checked((uint)portableEntryPoint.Address);
    }

    // WASM advertises the Debugger contract, but the in-process debugger is not built there, so
    // g_pDebugger stays null.
    private static void AddWasmNullDebugger(TestPlaceholderTarget.Builder targetBuilder, MockMemorySpace.BumpAllocator allocator)
    {
        MockMemorySpace.HeapFragment debuggerSlot = allocator.Allocate(4, "g_pDebugger");
        targetBuilder.AddGlobals((Constants.Globals.Debugger, debuggerSlot.Address));
        targetBuilder.AddContract<IDebugger>(version: "c1");
    }

    private static TargetPointer AddWasmR2RFunction(
        TestPlaceholderTarget.Builder targetBuilder,
        MockMemorySpace.BumpAllocator allocator,
        uint functionTableIndex,
        ulong minVirtualIP,
        uint functionBeginAddress,
        byte frameSize = 0)
        => AddWasmR2RFunctions(targetBuilder, allocator, functionTableIndex, minVirtualIP, [(functionBeginAddress, frameSize)]);

    // Registers one R2R module whose RUNTIME_FUNCTIONs occupy consecutive function-table indices
    // starting at minFunctionTableIndex. BeginAddress bit 31 marks a funclet; a frame size of 0
    // leaves the unwind data unset.
    private static TargetPointer AddWasmR2RFunctions(
        TestPlaceholderTarget.Builder targetBuilder,
        MockMemorySpace.BumpAllocator allocator,
        uint minFunctionTableIndex,
        ulong minVirtualIP,
        (uint BeginAddress, byte FrameSize)[] functions)
    {
        MockTarget.Architecture arch = targetBuilder.MemoryBuilder.TargetTestHelpers.Arch;
        TargetTestHelpers helpers = targetBuilder.MemoryBuilder.TargetTestHelpers;

        int hashMapStride = MockHashMap.CreateLayout(arch).Size;
        var moduleLayout = MockLoaderModule.CreateLayout(arch);
        var r2rInfoLayout = MockReadyToRunInfo.CreateLayout(arch, hashMapStride, isWasm: true);
        TargetTestHelpers.LayoutResult runtimeFunctionLayout = helpers.LayoutFields([
            new(nameof(Data.RuntimeFunction.BeginAddress), DataType.uint32),
            new(nameof(Data.RuntimeFunction.UnwindData), DataType.uint32),
        ]);
        TargetTestHelpers.LayoutResult rangeSectionLayout = helpers.LayoutFields([
            new(nameof(Data.FunctionTableIndexRangeSection.MinFunctionTableIndex), DataType.uint32),
            new(nameof(Data.FunctionTableIndexRangeSection.NumRuntimeFunctions), DataType.uint32),
            new(nameof(Data.FunctionTableIndexRangeSection.R2RModule), DataType.pointer),
            new(nameof(Data.FunctionTableIndexRangeSection.Next), DataType.pointer),
        ]);

        MockMemorySpace.HeapFragment runtimeFunctions = allocator.Allocate(runtimeFunctionLayout.Stride * (ulong)functions.Length, "RuntimeFunctions");
        for (int i = 0; i < functions.Length; i++)
        {
            Span<byte> entry = runtimeFunctions.Data.AsSpan(i * (int)runtimeFunctionLayout.Stride, (int)runtimeFunctionLayout.Stride);
            helpers.Write(entry.Slice(runtimeFunctionLayout.Fields[nameof(Data.RuntimeFunction.BeginAddress)].Offset, sizeof(uint)), functions[i].BeginAddress);
            if (functions[i].FrameSize != 0)
            {
                // Unwind data is the ULEB128 frame size, addressed as LoadedImageBase (0 here) + UnwindData.
                Assert.True(functions[i].FrameSize < 0x80);
                MockMemorySpace.HeapFragment unwindData = allocator.Allocate(1, "UnwindData");
                unwindData.Data[0] = functions[i].FrameSize;
                helpers.Write(entry.Slice(runtimeFunctionLayout.Fields[nameof(Data.RuntimeFunction.UnwindData)].Offset, sizeof(uint)), (uint)unwindData.Address);
            }
        }

        MockReadyToRunInfo r2rInfo = r2rInfoLayout.Create(allocator.Allocate((ulong)r2rInfoLayout.Size, "ReadyToRunInfo"));
        r2rInfo.CompositeInfo = r2rInfo.Address;
        r2rInfo.NumRuntimeFunctions = (uint)functions.Length;
        r2rInfo.RuntimeFunctions = runtimeFunctions.Address;
        r2rInfo.MinVirtualIP = minVirtualIP;

        MockLoaderModule module = moduleLayout.Create(allocator.Allocate((ulong)moduleLayout.Size, "Module"));
        module.ReadyToRunInfo = r2rInfo.Address;

        MockMemorySpace.HeapFragment section = allocator.Allocate(rangeSectionLayout.Stride, "FunctionTableIndexRangeSection");
        helpers.Write(section.Data.AsSpan(rangeSectionLayout.Fields[nameof(Data.FunctionTableIndexRangeSection.MinFunctionTableIndex)].Offset, sizeof(uint)), minFunctionTableIndex);
        helpers.Write(section.Data.AsSpan(rangeSectionLayout.Fields[nameof(Data.FunctionTableIndexRangeSection.NumRuntimeFunctions)].Offset, sizeof(uint)), (uint)functions.Length);
        helpers.WritePointer(section.Data.AsSpan(rangeSectionLayout.Fields[nameof(Data.FunctionTableIndexRangeSection.R2RModule)].Offset, helpers.PointerSize), module.Address);

        MockMemorySpace.HeapFragment listSlot = allocator.Allocate((ulong)helpers.PointerSize, "FunctionTableIndexRangeListSlot");
        helpers.WritePointer(listSlot.Data.AsSpan(), section.Address);

        targetBuilder
            .AddTypes(new Dictionary<DataType, Target.TypeInfo>
            {
                [DataType.RuntimeFunction] = new() { Fields = runtimeFunctionLayout.Fields, Size = runtimeFunctionLayout.Stride },
                [DataType.ReadyToRunInfo] = TargetTestHelpers.CreateTypeInfo(r2rInfoLayout),
                [DataType.Module] = TargetTestHelpers.CreateTypeInfo(moduleLayout),
                [DataType.FunctionTableIndexRangeSection] = new() { Fields = rangeSectionLayout.Fields, Size = rangeSectionLayout.Stride },
            })
            .AddGlobals((Constants.Globals.FunctionTableIndexRangeList, listSlot.Address));

        return runtimeFunctions.Address;
    }

    /// <summary>
    /// Native InlinedCallFrame::UpdateRegDisplay_Impl returns before recording the owning
    /// InterpreterFrame when the frame has no active call.
    /// </summary>
    [Fact]
    public void UpdateContextFromFrame_WasmInactiveInlinedCallFrameOverInterpreterFrame_LeavesContextUnchanged()
    {
        MockTarget.Architecture wasmArch = new() { IsLittleEndian = true, Is64Bit = false };

        ulong icfAddr = 0;
        TestPlaceholderTarget target = CreateTarget(
            wasmArch,
            threadBuilder => threadBuilder.AddThread(1, 1234),
            frameBuilder =>
            {
                ulong interpAddr = frameBuilder.AddFrame(MockFrameBuilder.InterpreterFrameIdentifierValue, "InterpreterFrame").Address;
                MockInlinedCallFrame icf = frameBuilder.AddInlinedCallFrame(callerReturnAddress: 0, datum: 0, callSiteSP: 0x0004_1000);
                icf.Next = interpAddr;
                icfAddr = icf.Address;
            });

        ContextHolder<WasmContext> context = new();
        context.Context.StackPointer = new TargetPointer(0x0005_0000);
        context.Context.InstructionPointer = new TargetCodePointer(0x0005_1000);
        context.Context.FramePointer = new TargetPointer(0x0005_2000);
        FrameHelpers frameHelpers = new(target);
        Data.Frame frame = target.ProcessedData.GetOrAdd<Data.Frame>(icfAddr);
        frameHelpers.UpdateContextFromFrame(frame, context);

        Assert.True(context.TryReadRegister(WasmContext.InterpreterWalkFramePointerRegister, out TargetNUInt stashed));
        Assert.Equal(0ul, stashed.Value);
        Assert.Equal(0x0005_0000ul, context.StackPointer.Value);
        Assert.Equal(0x0005_1000ul, context.InstructionPointer.Value);
        Assert.Equal(0x0005_2000ul, context.FramePointer.Value);
    }

    /// <summary>
    /// On WASM the callee-saved register set is InterpreterFP, so a SoftwareExceptionFrame restores
    /// the frame pointer from its saved context along with IP and SP (native
    /// SoftwareExceptionFrame::UpdateRegDisplay_Impl). WASM's CalleeSavedRegisters descriptor is
    /// empty, so the frame pointer must not be left from the previous frame.
    /// </summary>
    [Fact]
    public void UpdateContextFromFrame_WasmSoftwareExceptionFrame_RestoresFramePointer()
    {
        MockTarget.Architecture wasmArch = new() { IsLittleEndian = true, Is64Bit = false };
        TargetTestHelpers helpers = new(wasmArch);
        const ulong FrameAddress = 0x0006_0000;
        const int ReturnAddressOffset = 8;
        const int TargetContextOffset = 12;

        // WasmContext layout: ContextFlags, InterpreterWalkFramePointer, InterpreterSP, InterpreterFP, InterpreterIP.
        byte[] data = new byte[TargetContextOffset + 5 * sizeof(uint)];
        helpers.Write(data.AsSpan(0, sizeof(uint)), (uint)MockFrameBuilder.SoftwareExceptionFrameIdentifierValue);
        helpers.Write(data.AsSpan(TargetContextOffset + 8, sizeof(uint)), 0x0004_1000u);
        helpers.Write(data.AsSpan(TargetContextOffset + 12, sizeof(uint)), 0x0004_1040u);
        helpers.Write(data.AsSpan(TargetContextOffset + 16, sizeof(uint)), 0x8001_0106u);

        TestPlaceholderTarget target = CreateTarget(
            wasmArch,
            threadBuilder => threadBuilder.AddThread(1, 1234),
            frameBuilder => { },
            runtimeArchitecture: RuntimeInfoArchitecture.Wasm,
            configureTarget: builder =>
            {
                builder.MemoryBuilder.AddHeapFragment(new MockMemorySpace.HeapFragment { Address = FrameAddress, Data = data, Name = "SoftwareExceptionFrame" });
                builder.AddTypes(new Dictionary<DataType, Target.TypeInfo>
                {
                    [DataType.SoftwareExceptionFrame] = new()
                    {
                        Fields = new Dictionary<string, Target.FieldInfo>
                        {
                            [nameof(Data.SoftwareExceptionFrame.ReturnAddress)] = new() { Offset = ReturnAddressOffset, TypeName = "CodePointer" },
                            [nameof(Data.SoftwareExceptionFrame.TargetContext)] = new() { Offset = TargetContextOffset },
                        },
                    },
                    [DataType.CalleeSavedRegisters] = new() { Fields = new Dictionary<string, Target.FieldInfo>() },
                });
            });

        ContextHolder<WasmContext> context = new();
        context.Context.FramePointer = new TargetPointer(0x0007_0000);
        FrameHelpers frameHelpers = new(target);
        Data.Frame frame = target.ProcessedData.GetOrAdd<Data.Frame>(new TargetPointer(FrameAddress));
        frameHelpers.UpdateContextFromFrame(frame, context);

        Assert.Equal(0x0004_1000ul, context.StackPointer.Value);
        Assert.Equal(0x8001_0106ul, context.InstructionPointer.Value);
        Assert.Equal(0x0004_1040ul, context.FramePointer.Value);
    }

    // When an active InlinedCallFrame is directly followed by an InterpreterFrame, WasmFrameHandler
    // stashes the InterpreterFrame address into the synthetic first-argument register
    // (InterpreterWalkFramePointer) so the subsequent interpreter virtual unwind can recover the
    // owning frame -- mirroring native SetFirstArgReg on the P/Invoke-into-interpreter transition.
    [Fact]
    public void UpdateContextFromFrame_WasmInlinedCallFrameOverInterpreterFrame_StashesInterpreterFrame()
    {
        MockTarget.Architecture wasmArch = new() { IsLittleEndian = true, Is64Bit = false };

        ulong icfAddr = 0;
        ulong interpAddr = 0;
        TestPlaceholderTarget target = CreateTarget(
            wasmArch,
            threadBuilder => threadBuilder.AddThread(1, 1234),
            frameBuilder =>
            {
                interpAddr = frameBuilder.AddFrame(MockFrameBuilder.InterpreterFrameIdentifierValue, "InterpreterFrame").Address;
                MockInlinedCallFrame icf = frameBuilder.AddInlinedCallFrame(callerReturnAddress: 0x0004_2000, datum: 0, callSiteSP: 0x0004_1000);
                icf.Next = interpAddr;
                icfAddr = icf.Address;
            });

        ContextHolder<WasmContext> context = new();
        FrameHelpers frameHelpers = new(target);
        Data.Frame frame = target.ProcessedData.GetOrAdd<Data.Frame>(icfAddr);
        frameHelpers.UpdateContextFromFrame(frame, context);

        Assert.True(context.TryReadRegister(WasmContext.InterpreterWalkFramePointerRegister, out TargetNUInt stashed));
        Assert.Equal(interpAddr, stashed.Value);
    }

    // Interpreter virtual unwind on WASM: with the WasmContext SP pointing at an
    // InterpMethodContextFrame, each InterpreterVirtualUnwind step follows pParent to the next
    // interpreted method, setting IP/SP/FP from the parent frame (matching native
    // VirtualUnwindInterpreterCallFrame). Walks a three-node chain to the point of exhaustion.
    [Fact]
    public void InterpreterVirtualUnwind_WasmChain_StepsThroughInterpMethodContextFrames()
    {
        MockTarget.Architecture wasmArch = new() { IsLittleEndian = true, Is64Bit = false };

        const ulong ip1 = 0x0005_1000, fp1 = 0x0006_1000;
        const ulong ip2 = 0x0005_2000, fp2 = 0x0006_2000;

        ulong frame0 = 0, frame1 = 0, frame2 = 0;
        TestPlaceholderTarget target = CreateTarget(
            wasmArch,
            threadBuilder => threadBuilder.AddThread(1, 1234),
            frameBuilder =>
            {
                // Build leaf-to-root so parent addresses are known when linking children.
                frame2 = frameBuilder.AddInterpMethodContextFrame(parentPtr: 0, ip: ip2, stack: fp2).Address;
                frame1 = frameBuilder.AddInterpMethodContextFrame(parentPtr: frame2, ip: ip1, stack: fp1).Address;
                frame0 = frameBuilder.AddInterpMethodContextFrame(parentPtr: frame1, ip: 0, stack: 0).Address;
            });

        ContextHolder<WasmContext> context = new();
        context.StackPointer = new TargetPointer(frame0);
        FrameHelpers frameHelpers = new(target);

        // Step 1: frame0 -> parent frame1; context takes frame1's IP/SP/FP.
        frameHelpers.InterpreterVirtualUnwind(context);
        Assert.Equal(ip1, context.InstructionPointer.Value);
        Assert.Equal(frame1, context.StackPointer.Value);
        Assert.Equal(fp1, context.FramePointer.Value);

        // Step 2: frame1 -> parent frame2.
        frameHelpers.InterpreterVirtualUnwind(context);
        Assert.Equal(ip2, context.InstructionPointer.Value);
        Assert.Equal(frame2, context.StackPointer.Value);
        Assert.Equal(fp2, context.FramePointer.Value);
    }

    // When the InterpMethodContextFrame chain is exhausted (pParent == null) and no owning
    // InterpreterFrame is stashed in the synthetic first-argument register, the WASM interpreter
    // virtual unwind terminates gracefully without applying a transition. This also guards the
    // WASM first-argument-register wiring: before it was mapped to InterpreterWalkFramePointer,
    // this path threw NotSupportedException from GetFirstArgRegisterName.
    [Fact]
    public void InterpreterVirtualUnwind_WasmExhaustedChainNoOwningFrame_TerminatesGracefully()
    {
        MockTarget.Architecture wasmArch = new() { IsLittleEndian = true, Is64Bit = false };

        ulong frame0 = 0;
        TestPlaceholderTarget target = CreateTarget(
            wasmArch,
            threadBuilder => threadBuilder.AddThread(1, 1234),
            frameBuilder =>
            {
                frame0 = frameBuilder.AddInterpMethodContextFrame(parentPtr: 0, ip: 0x0005_1000, stack: 0x0006_1000).Address;
            },
            runtimeArchitecture: RuntimeInfoArchitecture.Wasm);

        ContextHolder<WasmContext> context = new();
        context.StackPointer = new TargetPointer(frame0);
        FrameHelpers frameHelpers = new(target);

        frameHelpers.InterpreterVirtualUnwind(context);

        // Chain exhausted with a null owning frame: context SP is left unchanged, no throw.
        Assert.Equal(frame0, context.StackPointer.Value);
    }

    // GetRuntimeFramePointer on ARM64 is the caller SP. For an interpreted frame the caller must come from the
    // interpreter virtual unwind (parent InterpMethodContextFrame), as native InterpreterCodeManager does;
    // the OS unwinder has no unwind info for bytecode and would leave SP unchanged.
    [Fact]
    public void CallerStackPointer_InterpretedFrame_IsParentInterpMethodContextFrame()
    {
        MockTarget.Architecture arm64 = new() { IsLittleEndian = true, Is64Bit = true };
        const ulong bytecodeIp = 0x0005_1020, parentIp = 0x0005_2040, parentStack = 0x0006_2000;

        ulong frame = 0, parent = 0;
        Mock<IExecutionManager> executionManager = new();
        executionManager.Setup(e => e.GetCodeKind(new TargetCodePointer(bytecodeIp))).Returns(CodeKind.Interpreter);
        TestPlaceholderTarget target = CreateTarget(
            arm64,
            threadBuilder => threadBuilder.AddThread(1, 1234),
            frameBuilder =>
            {
                parent = frameBuilder.AddInterpMethodContextFrame(parentPtr: 0, ip: parentIp, stack: parentStack).Address;
                frame = frameBuilder.AddInterpMethodContextFrame(parentPtr: parent, ip: bytecodeIp, stack: 0x0006_1000).Address;
            },
            executionManager: executionManager.Object);

        ContextHolder<ARM64Context> context = new();
        context.InstructionPointer = new TargetCodePointer(bytecodeIp);
        context.StackPointer = new TargetPointer(frame);

        StackWalk_1 stackWalk = new(target);
        Assert.Equal(parent, stackWalk.CallerStackPointer(context).Value);
        Assert.Equal(frame, context.StackPointer.Value);
        Assert.Equal(bytecodeIp, context.InstructionPointer.Value);
    }

    // The first interpreted method of an interpreter activation has the zeroed root InterpMethodContextFrame
    // (ip == NULL) as its parent. Native VirtualUnwindInterpreterCallFrame still sets the caller SP to that
    // parent, so the debugger frame pointer is the root frame's address.
    [Fact]
    public void CallerStackPointer_InterpretedFrameWithRootParent_IsRootFrameAddress()
    {
        MockTarget.Architecture arm64 = new() { IsLittleEndian = true, Is64Bit = true };
        const ulong bytecodeIp = 0x0005_1020;

        ulong frame = 0, root = 0;
        Mock<IExecutionManager> executionManager = new();
        executionManager.Setup(e => e.GetCodeKind(new TargetCodePointer(bytecodeIp))).Returns(CodeKind.Interpreter);
        TestPlaceholderTarget target = CreateTarget(
            arm64,
            threadBuilder => threadBuilder.AddThread(1, 1234),
            frameBuilder =>
            {
                root = frameBuilder.AddInterpMethodContextFrame(parentPtr: 0, ip: 0, stack: 0).Address;
                frame = frameBuilder.AddInterpMethodContextFrame(parentPtr: root, ip: bytecodeIp, stack: 0x0006_1000).Address;
            },
            executionManager: executionManager.Object);

        ContextHolder<ARM64Context> context = new();
        context.InstructionPointer = new TargetCodePointer(bytecodeIp);
        context.StackPointer = new TargetPointer(frame);

        StackWalk_1 stackWalk = new(target);
        Assert.Equal(root, stackWalk.CallerStackPointer(context).Value);
    }
}
