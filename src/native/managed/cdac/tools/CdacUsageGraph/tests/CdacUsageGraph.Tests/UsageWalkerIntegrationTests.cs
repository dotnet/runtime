// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using CdacUsageGraph.Analysis;
using CdacUsageGraph.Compilation;
using CdacUsageGraph.Discovery;
using CdacUsageGraph.Model;
using CdacUsageGraph.Reporting;
using CdacUsageGraph.Semantic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace CdacUsageGraph.Tests;
/// <summary>
/// End-to-end test over the real in-repo cDAC source. Guards the whole pipeline against known
/// invariants (a form of the "verify against known facts" baseline pattern). Skipped when the
/// cDAC source cannot be located (e.g. running outside the repo).
/// </summary>
public sealed class UsageWalkerIntegrationTests
{
    private static readonly Lazy<(UsageGraph Graph, string Root)?> s_realGraph =
        new(BuildRealGraphCore);

    private static (UsageGraph Graph, string Root)? BuildRealGraph() => s_realGraph.Value;

    private static (UsageGraph Graph, string Root)? BuildRealGraphCore()
    {
        DirectoryInfo? root = Locator.FindCdacRoot();
        if (root is null)
            return null;

        return (AnalysisPipeline.BuildGraph(root.FullName), root.FullName);
    }

    [Fact]
    public void DefaultOutputDirectoryIsIgnoredToolOutput()
    {
        DirectoryInfo? root = Locator.FindCdacRoot();
        if (root is null) return; // cDAC source not found (running outside the repo)

        string expected = Path.Combine(
            root.FullName,
            "tools",
            "CdacUsageGraph",
            "output");
        Assert.Equal(
            Path.GetFullPath(expected),
            Locator.DefaultOutputDirectory().FullName);
    }

    [Fact]
    public void MSBuildWorkspaceLoadsGeneratedContractsCompilation()
    {
        DirectoryInfo? root = Locator.FindCdacRoot();
        if (root is null) return; // cDAC source not found (running outside the repo)

        CSharpCompilation compilation = CdacCompilationLoader.Load(root.FullName);
        INamedTypeSymbol jitNotification = compilation.GetTypeByMetadataName(
            "Microsoft.Diagnostics.DataContractReader.Data.JITNotification")!;

        Assert.Empty(compilation.GetDiagnostics().Where(
            d => d.Severity == DiagnosticSeverity.Error));
        Assert.Single(jitNotification.GetMembers("WriteState").OfType<IMethodSymbol>());
        Assert.Contains(jitNotification.InstanceConstructors, c => c.Parameters.Length == 2);
        Assert.Contains(compilation.SyntaxTrees, tree =>
            tree.FilePath.EndsWith(".g.cs", StringComparison.Ordinal));

        INamedTypeSymbol thread = compilation.GetTypeByMetadataName(
            "Microsoft.Diagnostics.DataContractReader.Data.Thread")!;
        IPropertySymbol threadHandle = thread.GetMembers("ThreadHandle")
            .OfType<IPropertySymbol>()
            .Single();
        Assert.True(new CdacAttributeMatcher(compilation).TryGetDescriptorDependencies(
            threadHandle,
            out _));
    }

    [Theory]
    [InlineData("EETypeHashTable", "Buckets", "pointer")]
    [InlineData("EETypeHashTable", "Count", "uint32")]
    [InlineData("EETypeHashTable", "VolatileEntryNextEntry", "pointer")]
    [InlineData("EETypeHashTable", "VolatileEntryValue", "pointer")]
    [InlineData("InstMethodHashTable", "Buckets", "pointer")]
    [InlineData("InstMethodHashTable", "Count", "uint32")]
    [InlineData("InstMethodHashTable", "VolatileEntryNextEntry", "pointer")]
    [InlineData("InstMethodHashTable", "VolatileEntryValue", "pointer")]
    public void ResolvesNativeTypesForFieldsReadThroughHelpers(
        string dataType,
        string field,
        string expectedType)
    {
        (UsageGraph Graph, string Root)? built = BuildRealGraph();
        if (built is null) return; // cDAC source not found (running outside the repo)

        Assert.Equal(expectedType, Field(
            built.Value.Graph,
            new ContractVersion(new ContractInterface("ILoader"), "c1"),
            $"Data.{dataType}",
            field).Type);
    }

    [Theory]
    [InlineData("IExecutionManager", "c1", "Data.UnwindInfo", "FunctionLength")]
    [InlineData("IPrecodeStubs", "c1", "Data.PrecodeMachineDescriptor", "OffsetOfPrecodeType")]
    [InlineData("IStackWalk", "c1", "Data.ReadyToRunInfo", "ImportSections")]
    [InlineData("IThread", "c1", "Data.Thread", "ThreadHandle")]
    [InlineData("IThread", "c1", "Data.Thread", "DebuggerControlledThreadState")]
    public void UsageWalkerEffectsAreIntegratedIntoUsageGraph(
        string contract,
        string version,
        string dataType,
        string field)
    {
        (UsageGraph Graph, string Root)? built = BuildRealGraph();
        if (built is null) return; // cDAC source not found (running outside the repo)

        FieldUsage usage = Field(
            built.Value.Graph,
            new ContractVersion(new ContractInterface(contract), version),
            dataType,
            field);
        Assert.Equal(field, usage.Name);
    }

    [Fact]
    public void UsageWalkerTypeSizeEffectsAreIntegratedIntoUsageGraph()
    {
        (UsageGraph Graph, string Root)? built = BuildRealGraph();
        if (built is null) return; // cDAC source not found (running outside the repo)

        Assert.True(DataType(
            built.Value.Graph,
            new ContractVersion(new ContractInterface("IExecutionManager"), "c1"),
            "Data.R2RExceptionClause").UsesTypeSize);
    }

    [Fact]
    public void TypeSizeAndFieldNamedSizeAreTrackedSeparately()
    {
        (UsageGraph Graph, string Root)? built = BuildRealGraph();
        if (built is null) return; // cDAC source not found (running outside the repo)

        DataTypeUsage usage = DataType(
            built.Value.Graph,
            new ContractVersion(new ContractInterface("IDebugger"), "c1"),
            "Data.MemoryRange");
        Assert.True(usage.UsesTypeSize);
        Assert.Equal("nuint", usage.Fields.Single(field => field.Name == "Size").Type);
    }

    [Fact]
    public void ThreadContractUsesThreadDataWithoutObjectContract()
    {
        (UsageGraph Graph, string Root)? built = BuildRealGraph();
        if (built is null) return; // cDAC source not found (running outside the repo)
        UsageGraph graph = built!.Value.Graph;

        Assert.True(graph.DataTypeCount > 100, $"expected >100 Data types, got {graph.DataTypeCount}");

        // IThread c1 directly reads Data.Thread ...
        HashSet<string> threadTypes = DataTypesUsed(graph, new ContractVersion(new ContractInterface("IThread"), "c1"));
        Assert.Contains("Data.Thread", threadTypes);

        // ObjectHandle exposes its already-resolved target pointer without calling IObject.
        Assert.DoesNotContain(
            new ContractInterface("IObject"),
            Contract(graph, new ContractVersion(new ContractInterface("IThread"), "c1")).ContractsUsed);
    }

    [Fact]
    public void ContractsDoNotDependOnThemselves()
    {
        (UsageGraph Graph, string Root)? built = BuildRealGraph();
        if (built is null) return; // cDAC source not found (running outside the repo)

        foreach (ContractVersionUsage contract in built.Value.Graph.Contracts)
        {
            Assert.DoesNotContain(contract.Label.Interface, contract.ContractsUsed);
        }
    }

    [Fact]
    public void RecordsContractDependencies()
    {
        (UsageGraph Graph, string Root)? built = BuildRealGraph();
        if (built is null) return; // cDAC source not found (running outside the repo)

        Assert.Contains(
            new ContractInterface("ILoader"),
            Contract(
                built.Value.Graph,
                new ContractVersion(new ContractInterface("IEcmaMetadata"), "c1")).ContractsUsed);
    }

    [Theory]
    [InlineData("IThread", "c1", "ThreadStore", "pointer", false)]
    [InlineData("IRuntimeInfo", "c1", "Architecture", "string", true)]
    [InlineData("IRuntimeInfo", "c1", "RecommendedReaderVersion", "uint32", true)]
    [InlineData("IStackWalk", "c1", "<FrameType>Identifier", "pointer", true)]
    [InlineData("IDacStreams", "c1", "MiniMetaDataBuffAddress", "pointer", false)]
    [InlineData("IDacStreams", "c1", "MiniMetaDataBuffMaxSize", "pointer", false)]
    [InlineData("IComWrappers", "c1", "System.Runtime.InteropServices.ComWrappers.s_allManagedObjectWrapperTable", "pointer", true)]
    [InlineData("IComWrappers", "c1", "System.Runtime.InteropServices.ComWrappers.s_nativeObjectWrapperTable", "pointer", true)]
    [InlineData("IObjectiveCMarshal", "c1", "System.Runtime.InteropServices.ObjectiveC.ObjectiveCMarshal.s_objects", "pointer", true)]
    public void RecordsGlobalUsage(
        string contract,
        string version,
        string global,
        string type,
        bool optional)
    {
        (UsageGraph Graph, string Root)? built = BuildRealGraph();
        if (built is null) return; // cDAC source not found (running outside the repo)

        GlobalUsage usage = Global(
            built.Value.Graph,
            new ContractVersion(new ContractInterface(contract), version),
            global);
        Assert.Equal(type, usage.Type);
        Assert.Equal(optional, usage.IsOptional);
    }

    [Fact]
    public void ResolvesNativeDescriptorFieldNames()
    {
        (UsageGraph Graph, string Root)? built = BuildRealGraph();
        if (built is null) return; // cDAC source not found (running outside the repo)
        UsageGraph graph = built!.Value.Graph;

        // Exception.Message is [Field("_message")] -> the native descriptor name is emitted.
        Assert.Contains(
            "_message",
            DataType(graph, new ContractVersion(new ContractInterface("IException"), "c1"), "Data.Exception")
                .Fields.Select(field => field.Name));
    }

    [Fact]
    public void ResolvesGenericBaseAndStaticAbstractDispatch()
    {
        (UsageGraph Graph, string Root)? built = BuildRealGraph();
        if (built is null) return; // cDAC source not found (running outside the repo)
        UsageGraph graph = built!.Value.Graph;

        // PrecodeStubs c3 reaches Data types only via a generic base + static-abstract dispatch.
        HashSet<string> precodeTypes = DataTypesUsed(
            graph,
            new ContractVersion(new ContractInterface("IPrecodeStubs"), "c3"));
        Assert.Contains("Data.InterpMethod", precodeTypes);
    }

    [Theory]
    [InlineData("c1", false)]
    [InlineData("c2", false)]
    [InlineData("c3", true)]
    public void ReportsInterpreterPrecodeUsageOnlyForSupportingVersion(
        string version,
        bool expected)
    {
        (UsageGraph Graph, string Root)? built = BuildRealGraph();
        if (built is null) return; // cDAC source not found (running outside the repo)

        HashSet<string> dataTypes = DataTypesUsed(
            built.Value.Graph,
            new ContractVersion(new ContractInterface("IPrecodeStubs"), version));
        Assert.Equal(expected, dataTypes.Contains("Data.InterpreterPrecodeData"));
    }

    [Fact]
    public void ResolvesFieldReadsReachedThroughFieldInitializerHelper()
    {
        (UsageGraph Graph, string Root)? built = BuildRealGraph();
        if (built is null) return; // cDAC source not found (running outside the repo)
        UsageGraph graph = built!.Value.Graph;

        // StressLog_1's SmallStressMessageReader is constructed in a field initializer and reads
        // Data.StressMsg fields; walking initializers is what surfaces these.
        Assert.Contains(
            "Header",
            DataType(graph, new ContractVersion(new ContractInterface("IStressLog"), "c1"), "Data.StressMsg")
                .Fields.Select(field => field.Name));

        // StressMsgHeader is used only via Data.StressMsgHeader.GetSize.
        Assert.True(DataType(
            graph,
            new ContractVersion(new ContractInterface("IStressLog"), "c1"),
            "Data.StressMsgHeader").UsesTypeSize);
    }

    [Fact]
    public void ResolvesFieldReadsThroughSharedDataInterface()
    {
        (UsageGraph Graph, string Root)? built = BuildRealGraph();
        if (built is null) return; // cDAC source not found (running outside the repo)
        UsageGraph graph = built!.Value.Graph;

        // ExecutionManager reads exception-clause fields through the IExceptionClauseData interface
        // on a local that may hold either R2RExceptionClause or EEExceptionClause. Both Data types'
        // [Field] members should be attributed -- in particular R2RExceptionClause, which is never
        // referenced by a concrete-typed read.
        DataTypeUsage r2rUsage = DataType(
            graph,
            new ContractVersion(new ContractInterface("IExecutionManager"), "c1"),
            "Data.R2RExceptionClause");
        string[] r2rFields = r2rUsage.Fields.Select(field => field.Name).ToArray();
        Assert.Contains("Flags", r2rFields);
        Assert.Contains("ClassToken", r2rFields); // a [Field] on R2R (computed on EE, so EE is not credited it)

        string[] eeFields = DataType(
            graph,
            new ContractVersion(new ContractInterface("IExecutionManager"), "c1"),
            "Data.EEExceptionClause").Fields.Select(field => field.Name).ToArray();
        Assert.Contains("Flags", eeFields);

        // Computed (non-[Field]) interface members map to no descriptor field on either type.
        Assert.DoesNotContain("FilterOffset", r2rFields);
        Assert.DoesNotContain("FilterOffset", eeFields);
    }

    [Fact]
    public void ComputedConveniencePropertiesResolveToUnderlyingFields()
    {
        (UsageGraph Graph, string Root)? built = BuildRealGraph();
        if (built is null) return; // cDAC source not found (running outside the repo)
        UsageGraph graph = built!.Value.Graph;

        // IThread reads TLSIndex.IndexOffset/IsAllocated, which are computed (=> TLSIndexRawIndex & ...).
        // The tool records the actual underlying [Field] (TLSIndexRawIndex), not the derived names.
        string[] tlsFields = DataType(
            graph,
            new ContractVersion(new ContractInterface("IThread"), "c1"),
            "Data.TLSIndex").Fields.Select(field => field.Name).ToArray();
        Assert.Contains("TLSIndexRawIndex", tlsFields);
        Assert.DoesNotContain("IndexOffset", tlsFields);
        Assert.DoesNotContain("IsAllocated", tlsFields);

        // Handwritten dependency metadata preserves derived properties such as
        // Thread.ThreadHandle / Thread.RuntimeThreadLocals.
        DataTypeUsage thread = DataType(
            graph,
            new ContractVersion(new ContractInterface("IThread"), "c1"),
            "Data.Thread");
        Assert.Contains("ThreadHandle", thread.Fields.Select(field => field.Name));
        Assert.Equal(
            "pointer",
            thread.Fields.Single(field => field.Name == "RuntimeThreadLocals").Type);

        // ObjectHandle.Handle/Object are parsed from raw target pointers, not named descriptor
        // fields, so reading the convenience properties must not invent descriptor rows.
        if (FindDataType(
            graph,
            new ContractVersion(new ContractInterface("IThread"), "c1"),
            "Data.ObjectHandle") is DataTypeUsage objectHandle)
        {
            Assert.DoesNotContain("Handle", objectHandle.Fields.Select(field => field.Name));
            Assert.DoesNotContain("Object", objectHandle.Fields.Select(field => field.Name));
        }
    }

    [Theory]
    [InlineData("IExecutionManager", "c1")]
    [InlineData("IExecutionManager", "c2")]
    public void ExplicitDependenciesIncludeCompositeInfoWhereUsed(string contract, string version)
    {
        (UsageGraph Graph, string Root)? built = BuildRealGraph();
        if (built is null) return; // cDAC source not found (running outside the repo)
        UsageGraph graph = built.Value.Graph;

        Assert.Equal("CompositeInfo", Field(
            graph,
            new ContractVersion(new ContractInterface(contract), version),
            "Data.ReadyToRunInfo",
            "CompositeInfo").Name);
    }

    [Fact]
    public void SyncBlockUsesExplicitPropertyDependencies()
    {
        (UsageGraph Graph, string Root)? built = BuildRealGraph();
        if (built is null) return; // cDAC source not found (running outside the repo)
        UsageGraph graph = built.Value.Graph;
        ContractVersion label = new(new ContractInterface("ISyncBlock"), "c1");

        DataTypeUsage syncBlock = DataType(graph, label, "Data.SyncBlock");
        Assert.Equal(
            ["EnCInfo", "InteropInfo", "LinkNext", "Lock", "ThinLock"],
            syncBlock.Fields.Select(field => field.Name).Order().ToArray());
        Assert.DoesNotContain(syncBlock.Fields, field => field.Name == "Address");

        DataTypeUsage syncTableEntry = DataType(graph, label, "Data.SyncTableEntry");
        Assert.Equal(
            ["Object", "SyncBlock"],
            syncTableEntry.Fields.Select(field => field.Name).Order().ToArray());
        Assert.True(syncTableEntry.UsesTypeSize);
        Assert.DoesNotContain(syncTableEntry.Fields, field => field.Name == "Address");
    }

    [Theory]
    [InlineData("ILoader", "c1", "Data.ModuleLookupMap", "Count")]
    [InlineData("IStackWalk", "c1", "Data.VASigCookie", "SizeOfArgs")]
    public void CompoundAssignmentOperandsRemainDependencies(
        string contract,
        string version,
        string dataType,
        string field)
    {
        (UsageGraph Graph, string Root)? built = BuildRealGraph();
        if (built is null) return; // cDAC source not found (running outside the repo)
        UsageGraph graph = built.Value.Graph;

        Assert.Equal(
            field,
            Field(graph, new ContractVersion(new ContractInterface(contract), version), dataType, field).Name);
    }

    [Theory]
    [InlineData("Data.EETypeHashTable")]
    [InlineData("Data.InstMethodHashTable")]
    public void ResolvesFieldsReadThroughReusableTypeInfoHelper(string dataType)
    {
        (UsageGraph Graph, string Root)? built = BuildRealGraph();
        if (built is null) return; // cDAC source not found (running outside the repo)
        UsageGraph graph = built!.Value.Graph;

        // Both hash Data types declare the descriptor dependencies of their derived Entries
        // collection explicitly, so helper implementation details do not affect attribution.
        string[] fields = DataType(
            graph,
            new ContractVersion(new ContractInterface("ILoader"), "c1"),
            dataType).Fields.Select(field => field.Name).ToArray();
        Assert.Contains("Buckets", fields);
        Assert.Contains("Count", fields);
        Assert.Contains("VolatileEntryNextEntry", fields);
        Assert.Contains("VolatileEntryValue", fields);
        Assert.DoesNotContain("Entries", fields);
    }

    [Fact]
    public void AttributesDynamicILFieldsBelongToLoader()
    {
        (UsageGraph Graph, string Root)? built = BuildRealGraph();
        if (built is null) return; // cDAC source not found (running outside the repo)
        UsageGraph graph = built!.Value.Graph;
        const string dataType = "Data.DynamicILBlobTable";

        // Loader owns the entry traits/direct result read. DynamicILBlobEntry is an adapter C#
        // class for the native DynamicILBlobTable descriptor.
        string[] loaderFields = DataType(
            graph,
            new ContractVersion(new ContractInterface("ILoader"), "c1"),
            dataType).Fields.Select(field => field.Name).ToArray();
        Assert.Contains("EntryIL", loaderFields);
        Assert.Contains("EntryMethodToken", loaderFields);
        Assert.DoesNotContain("HashTable", loaderFields);

        // HashTable's metadata belongs to the property, so Loader records the fields and
        // layout-size usage when it reads that property.
        Assert.Contains("Table", loaderFields);
        Assert.Contains("TableSize", loaderFields);

        // The call still establishes the contract dependency, but the descriptor dependencies
        // are declared by Loader's property rather than inferred from the TypeInfo argument.
        Assert.Contains(
            new ContractInterface("ISHash"),
            Contract(graph, new ContractVersion(new ContractInterface("ILoader"), "c1")).ContractsUsed);
    }

    [Fact]
    public void CapturesGeneratedWritableFieldMethods()
    {
        (UsageGraph Graph, string Root)? built = BuildRealGraph();
        if (built is null) return; // cDAC source not found (running outside the repo)
        UsageGraph graph = built!.Value.Graph;

        DataTypeUsage usage = DataType(
            graph,
            new ContractVersion(new ContractInterface("ICodeNotifications"), "c1"),
            "Data.JITNotification");
        foreach (string field in new[] { "ClrModule", "MethodToken", "State" })
        {
            Assert.Contains(usage.Fields, candidate => candidate.Name == field);
        }
    }

    [Fact]
    public void JsonReportPreservesGlobalUsageProperties()
    {
        (UsageGraph Graph, string Root)? built = BuildRealGraph();
        if (built is null) return; // cDAC source not found (running outside the repo)
        string output = Directory.CreateTempSubdirectory("CdacUsageGraphJson").FullName;
        try
        {
            new JsonReportWriter().Write(built.Value.Graph, output);
            using JsonDocument document = JsonDocument.Parse(
                File.ReadAllText(Path.Combine(output, "contract-usage.json")));
            JsonElement runtimeInfo = document.RootElement.EnumerateArray().Single(e =>
                e.GetProperty("contract").GetString() == "IRuntimeInfo" &&
                e.GetProperty("version").GetString() == "c1");
            JsonElement architecture =
                runtimeInfo.GetProperty("globalsUsed").GetProperty("Architecture");
            Assert.Equal("string", architecture.GetProperty("type").GetString());
            Assert.True(architecture.GetProperty("optional").GetBoolean());
            JsonElement debugger = document.RootElement.EnumerateArray().Single(e =>
                e.GetProperty("contract").GetString() == "IDebugger" &&
                e.GetProperty("version").GetString() == "c1");
            Assert.Contains(
                "Data.MemoryRange",
                debugger.GetProperty("dataTypeSizesUsed")
                    .EnumerateArray()
                    .Select(element => element.GetString()));
        }
        finally
        {
            Directory.Delete(output, recursive: true);
        }
    }

    private static ContractVersionUsage Contract(UsageGraph graph, ContractVersion label) =>
        graph.Contracts.Single(contract => contract.Label == label);

    private static DataTypeUsage DataType(
        UsageGraph graph,
        ContractVersion label,
        string name) =>
        Contract(graph, label).DataTypes.Single(dataType => dataType.Name == name);

    private static DataTypeUsage? FindDataType(
        UsageGraph graph,
        ContractVersion label,
        string name) =>
        Contract(graph, label).DataTypes.SingleOrDefault(dataType => dataType.Name == name);

    private static FieldUsage Field(
        UsageGraph graph,
        ContractVersion label,
        string dataType,
        string field) =>
        DataType(graph, label, dataType).Fields.Single(usage => usage.Name == field);

    private static GlobalUsage Global(
        UsageGraph graph,
        ContractVersion label,
        string name) =>
        Contract(graph, label).Globals.Single(global => global.Name == name);

    private static HashSet<string> DataTypesUsed(UsageGraph graph, ContractVersion label) =>
        Contract(graph, label).DataTypes.Select(dataType => dataType.Name).ToHashSet();
}
