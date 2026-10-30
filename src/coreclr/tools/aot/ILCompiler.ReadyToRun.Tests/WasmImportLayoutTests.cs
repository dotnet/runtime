// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

extern alias crossgen2;

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using crossgen2::ILCompiler;
using crossgen2::ILCompiler.DependencyAnalysis;
using crossgen2::ILCompiler.DependencyAnalysis.ReadyToRun;
using crossgen2::ILCompiler.Win32Resources;
using crossgen2::Internal.JitInterface;
using crossgen2::Internal.ReadyToRunConstants;
using crossgen2::Internal.Text;
using ILCompiler.DependencyAnalysisFramework;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;
using Xunit;

namespace ILCompiler.ReadyToRun.Tests;

public class WasmImportLayoutTests
{
    private static readonly SortableDependencyNode.EmbeddedObjectNodeComparer s_comparer =
        new(CompilerComparer.Instance);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MixedImportsHaveOneColdRunInBothArrays(bool reverse)
    {
        ImportSectionNode table = Table();
        Import[] imports = Enumerable.Range(1, 8).Select(id =>
            new Import(table, new ReadyToRunHelperSignature((ReadyToRunHelper)id))).ToArray();
        Import[] original = imports.OrderBy(import => import, s_comparer).ToArray();
        HashSet<Import> cold = new(original.Where((_, index) => index % 2 == 0));
        Assert.Equal(4, Runs(original, cold));

        WasmImportLayout layout = new();
        foreach (Import import in reverse ? imports.Reverse() : imports)
        {
            layout.AddImport(import);
            layout.AddUse(import, cold.Contains(import));
        }
        layout.Complete(symbol => symbol);

        Import[] ordered = imports.OrderBy(import => import, s_comparer).ToArray();
        Assert.Equal(original.Where(import => !cold.Contains(import)).Concat(original.Where(cold.Contains)), ordered);
        Assert.Equal(1, Runs(ordered, layout.ColdImports.ToHashSet()));
        Assert.Equal(ordered.Select(import => import.ImportSignature),
            imports.Select(import => import.ImportSignature).OrderBy(signature => signature, s_comparer));
        Assert.True(cold.Select(import => (ISymbolNode)import.Signature).ToHashSet().SetEquals(layout.ColdSignatures));
    }

    [Fact]
    public void SharedEagerUnusedAndResidentDataImportsStayPrimary()
    {
        ImportSectionNode table = Table();
        Signature sharedSignature = new ReadyToRunHelperSignature((ReadyToRunHelper)1);
        Import hot = new(table, sharedSignature);
        Import shared = new(table, new ReadyToRunHelperSignature((ReadyToRunHelper)2));
        Import coldWithSharedSignature = new(table, sharedSignature);
        Import residentData = new(table, new ReadyToRunHelperSignature((ReadyToRunHelper)3));
        Import eager = new(Table(eager: true), new ReadyToRunHelperSignature((ReadyToRunHelper)4));
        Import unused = new(table, new ReadyToRunHelperSignature((ReadyToRunHelper)5));
        Import exclusive = new(table, new ReadyToRunHelperSignature((ReadyToRunHelper)6));
        WasmImportLayout layout = new();
        foreach (Import import in new[] { hot, shared, coldWithSharedSignature, residentData, eager, unused, exclusive })
            layout.AddImport(import);
        layout.AddUse(hot, cold: false);
        layout.AddUse(shared, cold: false);
        layout.AddUse(shared, cold: true);
        layout.AddUse(coldWithSharedSignature, cold: true);
        layout.AddUse(residentData, cold: true);
        layout.AddUse(residentData, cold: false);
        layout.AddUse(eager, cold: true);
        layout.AddUse(exclusive, cold: true);
        layout.Complete(symbol => symbol);

        Assert.True(new HashSet<Import> { coldWithSharedSignature, exclusive }.SetEquals(layout.ColdImports));
        Assert.Same(exclusive.Signature, Assert.Single(layout.ColdSignatures));
        Assert.DoesNotContain(sharedSignature, layout.ColdSignatures);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SharedDeduplicatedAndDirectlyReferencedSignaturesStayPrimary(bool directReference)
    {
        Signature first = new ReadyToRunHelperSignature((ReadyToRunHelper)1);
        Signature alias = new ReadyToRunHelperSignature((ReadyToRunHelper)2);
        Import cold = new(Table(), first);
        WasmImportLayout layout = new();
        layout.AddImport(cold);
        layout.AddUse(cold, cold: true);
        if (directReference)
            layout.AddUse(alias, cold: false);
        else
            layout.AddImport(new Import(Table(), alias));
        layout.Complete(symbol => symbol == alias ? first : symbol);
        Assert.Same(cold, Assert.Single(layout.ColdImports));
        Assert.Empty(layout.ColdSignatures);
    }

    [Fact]
    public void EagerValidationFixupsStillPrecedeReadyToRunCommitFixups()
    {
        ImportSectionNode table = Table(eager: true);
        Import commit = new(table, new TestSignature(1), sortLast: true);
        Import validation = new(table, new TestSignature(2));
        WasmImportLayout layout = new();
        layout.AddImport(commit);
        layout.AddImport(validation);
        layout.AddUse(commit, cold: true);
        layout.AddUse(validation, cold: true);
        layout.Complete(symbol => symbol);
        Assert.Empty(layout.ColdImports);
        Assert.True(s_comparer.Compare(validation, commit) < 0);
        Assert.True(s_comparer.Compare(validation.ImportSignature, commit.ImportSignature) < 0);
    }

    [Fact]
    public void SignatureBlobsAreStablyGroupedWithoutMovingOrderedTables()
    {
        ImportSectionNode table = Table();
        Signature[] signatures = Enumerable.Range(1, 4)
            .Select(id => (Signature)new ReadyToRunHelperSignature((ReadyToRunHelper)id)).ToArray();
        WasmImportLayout layout = new();
        for (int index = 0; index < signatures.Length; index++)
        {
            Import import = new(table, signatures[index]);
            layout.AddImport(import);
            layout.AddUse(import, cold: index % 2 == 0);
        }
        layout.Complete(symbol => symbol);
        DependencyNode[] original = [table, signatures[0], signatures[1], signatures[2], signatures[3]];
        Assert.Equal<DependencyNode>([table, signatures[1], signatures[3], signatures[0], signatures[2]],
            layout.OrderSignatureBlobs(original));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(4, false)]
    [InlineData(4, true)]
    public void EmptyAndSingleOwnerPartitions(int count, bool cold)
    {
        WasmImportLayout layout = new();
        Import[] imports = Enumerable.Range(1, count).Select(id =>
            new Import(Table(), new ReadyToRunHelperSignature((ReadyToRunHelper)id))).ToArray();
        foreach (Import import in imports)
        {
            layout.AddImport(import);
            layout.AddUse(import, cold);
        }
        layout.Complete(symbol => symbol);
        Assert.Equal(cold ? count : 0, layout.ColdImports.Count());
        Assert.Equal(cold ? count : 0, layout.ColdSignatures.Count());
        Assert.Equal(imports.Cast<DependencyNode>(), layout.OrderSignatureBlobs(imports));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    [InlineData(1025)]
    public void EmittedTablesUsePartitionedOffsetsAndPreserveAlignment(int count)
    {
        NodeFactory factory = CreateFactory();
        ImportSectionNode table = new("EmittedImports", ReadyToRunImportSectionType.Unknown,
            0, 4, emitPrecode: true, emitGCRefMap: false);
        Import[] imports = Enumerable.Range(1, count).Select(id =>
            new Import(table, new TestSignature(id))).ToArray();
        HashSet<Import> coldImports = imports.Where((_, index) => index % 2 == 0).ToHashSet();
        int originalRuns = Runs(imports.OrderBy(import => import, s_comparer), coldImports);
        WasmImportLayout layout = new();
        for (int index = 0; index < count; index++)
        {
            table.AddImport(factory, imports[index]);
            layout.AddImport(imports[index]);
            layout.AddUse(imports[index], cold: index % 2 == 0);
        }
        layout.Complete(symbol => symbol);
        SortableDependencyNode.DependencySink sink = new();
        table.AddStaticDependencies(sink, factory);
        using var dependencies = sink.Drain();
        List<ObjectNode> tables = new();
        while (dependencies.MoveNext())
            tables.Add(Assert.IsAssignableFrom<ObjectNode>(dependencies.Dependency));
        ObjectNode cells = Assert.Single(tables, node => node != table.SignatureArray);
        ObjectNode.ObjectData cellData = cells.GetData(factory);
        ObjectNode.ObjectData signatureData = ((ObjectNode)table.SignatureArray).GetData(factory);

        Assert.Equal(4, cellData.Alignment);
        Assert.Equal(count * 4, cellData.Data.Length);
        Assert.Equal(count * 4, signatureData.Data.Length);
        Import[] ordered = imports.OrderBy(import => import, s_comparer).ToArray();
        for (int index = 0; index < count; index++)
        {
            Import import = ordered[index];
            Assert.Equal(index, import.IndexFromBeginningOfArray);
            Assert.Equal(index * 4, import.OffsetFromBeginningOfArray);
            Assert.Equal(index, import.ImportSignature.IndexFromBeginningOfArray);
            Assert.Equal(index * 4, import.ImportSignature.OffsetFromBeginningOfArray);
            Assert.Same(import.Signature, signatureData.Relocs[index].Target);
            Assert.Equal(index * 4, signatureData.Relocs[index].Offset);
        }
        Import[] physicalCells = cellData.DefinedSymbols.OfType<Import>()
            .OrderBy(import => ((ISymbolDefinitionNode)import).Offset).ToArray();
        int emittedRuns = Runs(physicalCells, layout.ColdImports.ToHashSet());
        Assert.Equal(ordered, physicalCells);
        Assert.Equal(count == 0 ? 0 : 1, emittedRuns);
        if (count > 2)
            Assert.True(emittedRuns < originalRuns,
                "The emitted table must have fewer cold-owned ranges than the unpartitioned order.");
        table.InitializeIndexFromBeginningOfArray(3);
        EcmaModule module = factory.TypeSystemContext.GetModuleFromPath(typeof(WasmImportLayoutTests).Assembly.Location);
        MethodWithGCInfo method = new(module.GetType("ILCompiler.ReadyToRun.Tests"u8, "WasmImportLayoutTests"u8)
            .GetMethod("MixedImportsHaveOneColdRunInBothArrays"u8, null));
        method.SetCode(new ObjectNode.ObjectData([], [], 1, []));
        foreach (Import import in ordered.Where((_, index) => index % 2 == 0).Reverse())
            method.Fixups.Add(import);
        if (count > 0)
        {
            byte[] blob = method.GetFixupBlob(factory);
            NibbleWriter expected = new();
            expected.WriteUInt(3);
            expected.WriteUInt(0);
            for (int index = 2; index < count; index += 2)
                expected.WriteUInt(2);
            expected.WriteUInt(0);
            expected.WriteUInt(0);
            Assert.Equal(expected.ToArray(), blob);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GCMapsFollowSignaturesAcrossPartitionsAndSharing(bool instantiatingStub)
    {
        NodeFactory factory = CreateFactory(hostTarget: true);
        CompilerTypeSystemContext context = factory.TypeSystemContext;
        EcmaModule module = context.GetModuleFromPath(typeof(WasmImportLayoutTests).Assembly.Location);
        MetadataType type = module.GetType("ILCompiler.ReadyToRun.Tests"u8, "WasmImportLayoutTests"u8);
        MethodDesc generic = type.GetMethod("GenericArgument"u8, null).MakeInstantiatedMethod(
            new Instantiation(context.GetWellKnownType(WellKnownType.Object)));
        ImportSectionNode table = new("GCImports", ReadyToRunImportSectionType.StubDispatch, 0, 8,
            emitPrecode: false, emitGCRefMap: true);
        MethodDesc[] methods = [type.GetMethod("ObjectArgument"u8, null),
            type.GetMethod("IntegerArgument"u8, null), generic];
        MethodFixupSignature[] signatures = methods.Select(method =>
            new MethodFixupSignature(ReadyToRunFixupKind.MethodEntry,
                new MethodWithToken(method,
                    new ModuleToken(module, ((EcmaMethod)method.GetTypicalMethodDefinition()).Handle),
                    constrainedType: null, unboxing: false, genericContextObject: null),
                instantiatingStub)).ToArray();
        Import[] imports = [new(table, signatures[0]), new(table, signatures[1]),
            new(table, signatures[2]), new(table, signatures[0])];
        WasmImportLayout layout = new();
        for (int index = 0; index < imports.Length; index++)
        {
            table.AddImport(factory, imports[index]);
            layout.AddImport(imports[index]);
            layout.AddUse(imports[index], cold: index >= 2);
        }
        layout.Complete(symbol => symbol);
        Assert.Same(signatures[2], Assert.Single(layout.ColdSignatures));
        ObjectNode.ObjectData signatureData = ((ObjectNode)table.SignatureArray).GetData(factory);
        int indexInArray = 0;
        foreach (Import import in imports.OrderBy(import => import, s_comparer))
        {
            MethodFixupSignature signature = Assert.IsType<MethodFixupSignature>(import.Signature);
            Assert.True(signature.EmitGCRefMap);
            GCRefMapBuilder expected = new(factory.Target, relocsOnly: false);
            expected.GetCallRefMap(signature.Method, instantiatingStub);
            byte[] map = signature.GetGCRefMap(factory);
            Assert.Equal(expected.Builder.ToObjectData().Data, map);
            Assert.Equal(map.Length, signature.Offset);
            Assert.Same(signature, signatureData.Relocs[indexInArray].Target);
            Assert.Equal(-map.Length,
                BinaryPrimitives.ReadInt32LittleEndian(signatureData.Data.AsSpan(indexInArray * sizeof(int))));
            indexInArray++;
        }
    }

    private static void ObjectArgument(object value) { }
    private static void IntegerArgument(int value) { }
    private static void GenericArgument<T>(T value) { }

    private static NodeFactory CreateFactory(bool hostTarget = false)
    {
        TargetArchitecture architecture = hostTarget ? TargetArchitecture.X64 : TargetArchitecture.Wasm32;
        TargetDetails target = new(architecture,
            hostTarget ? TargetOS.Linux : TargetOS.Browser,
            TargetAbi.NativeAot);
        ReadyToRunCompilerContext context = new(target, SharedGenericsMode.CanonicalReferenceTypes,
            bubbleIncludesCoreModule: true, targetAllowsRuntimeCodeGeneration: hostTarget,
            targetIsSingleThreaded: !hostTarget, new InstructionSetSupport(default, default, architecture),
            oldTypeSystemContext: null);
        if (hostTarget)
        {
            context.InputFilePaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            context.ReferenceFilePaths = Directory.GetFiles(Path.GetDirectoryName(typeof(object).Assembly.Location)!, "*.dll")
                .ToDictionary(path => Path.GetFileNameWithoutExtension(path), StringComparer.OrdinalIgnoreCase);
            context.SetSystemModule(context.GetModuleFromPath(typeof(object).Assembly.Location));
        }
        EcmaModule module = context.GetModuleFromPath(typeof(WasmImportLayoutTests).Assembly.Location);
        NoMethodsCompilationModuleGroup group = new(new ReadyToRunCompilationModuleGroupConfig
        {
            Context = context,
            IsCompositeBuildMode = true,
            CompilationModuleSet = [module],
            VersionBubbleModuleSet = [],
            CrossModuleInlineable = [],
        });
        context.SetCompilationGroup(group);
        return new NodeFactory(context, group, null!, null!, null!, null!, new ResourceData(module),
            0, default, ReadyToRunContainerFormat.Wasm, 0, module, -1, -1);
    }

    private sealed class TestSignature(int id) : Signature
    {
        public override int ClassCode => 173201194;

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false) =>
            new(BitConverter.GetBytes(id), [], 1, [this]);

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb) =>
            sb.Append("TestSignature" + id);

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer) =>
            id.CompareTo(((TestSignature)other).Id);

        private int Id => id;
    }

    private static ImportSectionNode Table(bool eager = false) =>
        new("TestImports", ReadyToRunImportSectionType.Unknown,
            eager ? ReadyToRunImportSectionFlags.Eager : 0, 4, emitPrecode: false, emitGCRefMap: false);

    private static int Runs(IEnumerable<Import> imports, HashSet<Import> cold)
    {
        int runs = 0;
        bool previous = false;
        foreach (Import import in imports)
        {
            bool current = cold.Contains(import);
            if (current && !previous)
                runs++;
            previous = current;
        }
        return runs;
    }
}
