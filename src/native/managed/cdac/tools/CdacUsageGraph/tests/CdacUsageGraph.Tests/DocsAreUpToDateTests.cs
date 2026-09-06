// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using CdacUsageGraph;
using CdacUsageGraph.Docs;
using CdacUsageGraph.Model;
using Xunit;

namespace CdacUsageGraph.Tests;

/// <summary>
/// Unit tests for generated usage-doc marker validation and formatting.
/// </summary>
public sealed class DocsAreUpToDateTests
{
    [Theory]
    [InlineData("<!-- BEGIN GENERATED: usage contract=Thread version=c1 -->")]
    [InlineData(
        "<!-- BEGIN GENERATED: unknown contract=Thread version=c1 -->\n" +
        "<!-- END GENERATED: unknown contract=Thread version=c1 -->")]
    [InlineData(
        "<!-- BEGIN GENERATED: data-descriptors contract=Thread version=c1 -->\n" +
        "<!-- END GENERATED: data-descriptors contract=Thread version=c1 -->")]
    [InlineData(
        "<!-- BEGIN GENERATED: contracts-used contract=Thread version=c1 -->\n" +
        "<!-- END GENERATED: contracts-used contract=Thread version=c1 -->")]
    [InlineData(
        "<!-- BEGIN GENERATED: usage contract=Thread version=c1 -->\n" +
        "<!-- END GENERATED: usage contract=Thread version=c1 -->\n" +
        "<!-- BEGIN GENERATED: usage contract=Thread version=c1 -->\n" +
        "<!-- END GENERATED: usage contract=Thread version=c1 -->")]
    [InlineData(
        "<!-- BEGIN GENERATED: usage contract=Thread version=c1 mode=compact -->\n" +
        "<!-- END GENERATED: usage contract=Thread version=c1 mode=compact -->")]
    public void RejectsInvalidGeneratedMarkers(string content)
    {
        using TempDirectory temp = new();
        File.WriteAllText(Path.Combine(temp.Path, "Thread.md"), content);
        DocGenerator generator = new(EmptyGraph(), DocDescriptorMeanings.Empty);

        Assert.Throws<InvalidOperationException>(() => generator.Check(temp.Path));
    }

    [Fact]
    public void GeneratesUsageDiffFromRegisteredVersion()
    {
        using TempDirectory temp = new();
        string path = Path.Combine(temp.Path, "Widget.md");
        File.WriteAllText(path,
            "<!-- BEGIN GENERATED: usage contract=Widget version=c2 diff-from=c1 -->\n" +
            "<!-- END GENERATED: usage contract=Widget version=c2 diff-from=c1 -->");
        UsageGraph graph = new(
            "",
            1,
            [
                new ContractVersionUsage(
                    new ContractVersion(new ContractInterface("IWidget"), "c1"),
                    [
                        new DataTypeUsage(
                            "Data.Widget",
                            false,
                            [
                                new FieldUsage("RemovedField", "uint32"),
                                new FieldUsage("SharedField", "uint64"),
                            ]),
                    ],
                    [new GlobalUsage("RemovedGlobal", "uint32", false)],
                    [new ContractInterface("IRemovedContract"), new ContractInterface("ISharedContract")]),
                new ContractVersionUsage(
                    new ContractVersion(new ContractInterface("IWidget"), "c2"),
                    [
                        new DataTypeUsage(
                            "Data.Widget",
                            false,
                            [
                                new FieldUsage("AddedField", "pointer"),
                                new FieldUsage("SharedField", "uint64"),
                            ]),
                    ],
                    [new GlobalUsage("AddedGlobal", "pointer", false)],
                    [new ContractInterface("IAddedContract"), new ContractInterface("ISharedContract")]),
            ]);

        new DocGenerator(graph, DocDescriptorMeanings.Empty).Emit(temp.Path);

        string generated = File.ReadAllText(path);
        Assert.Contains(
            "| Added | `Widget` | `AddedField` | `pointer` | _TODO: describe_ |",
            generated);
        Assert.Contains(
            "| Removed | `Widget` | `RemovedField` | `uint32` | _TODO: describe_ |",
            generated);
        Assert.DoesNotContain("SharedField", generated);
        Assert.Contains(
            "| Added | `AddedGlobal` | `pointer` | _TODO: describe_ |",
            generated);
        Assert.Contains(
            "| Removed | `RemovedGlobal` | `uint32` | _TODO: describe_ |",
            generated);
        Assert.Contains("| Added | `AddedContract` |", generated);
        Assert.Contains("| Removed | `RemovedContract` |", generated);
        Assert.DoesNotContain("SharedContract", generated);
    }

    [Fact]
    public void GeneratesUnknownTypeForEmptyFieldTypeSet()
    {
        using TempDirectory temp = new();
        string path = Path.Combine(temp.Path, "Widget.md");
        File.WriteAllText(path,
            "<!-- BEGIN GENERATED: usage contract=Widget version=c1 -->\n" +
            "<!-- END GENERATED: usage contract=Widget version=c1 -->");
        UsageGraph graph = new(
            "",
            1,
            [
                new ContractVersionUsage(
                    new ContractVersion(new ContractInterface("IWidget"), "c1"),
                    [
                        new DataTypeUsage(
                            "Data.Widget",
                            false,
                            [new FieldUsage("Value", "unknown")]),
                    ],
                    [],
                    []),
            ]);

        new DocGenerator(graph, DocDescriptorMeanings.Empty).Emit(temp.Path);

        Assert.Contains(
            "| `Widget` | `Value` | `unknown` | _TODO: describe_ |",
            File.ReadAllText(path));
    }

    [Fact]
    public void GeneratesNoneForEmptyUsageSections()
    {
        using TempDirectory temp = new();
        string path = Path.Combine(temp.Path, "Widget.md");
        File.WriteAllText(path,
            "<!-- BEGIN GENERATED: usage contract=Widget version=c1 -->\n" +
            "<!-- END GENERATED: usage contract=Widget version=c1 -->");

        UsageGraph graph = new(
            "",
            0,
            [new ContractVersionUsage(new ContractVersion(new ContractInterface("IWidget"), "c1"), [], [], [])]);
        new DocGenerator(graph, DocDescriptorMeanings.Empty).Emit(temp.Path);

        Assert.Equal(
            3,
            System.Text.RegularExpressions.Regex.Matches(
                File.ReadAllText(path),
                "_None\\._").Count);
    }

    [Fact]
    public void IdentifiesSymbolicGlobalNamesAsPatterns()
    {
        using TempDirectory temp = new();
        string path = Path.Combine(temp.Path, "Widget.md");
        File.WriteAllText(path,
                "<!-- BEGIN GENERATED: usage contract=Widget version=c1 -->\n" +
                "<!-- END GENERATED: usage contract=Widget version=c1 -->");
        UsageGraph graph = new(
                "",
                0,
                [
                    new ContractVersionUsage(
                        new ContractVersion(new ContractInterface("IWidget"), "c1"),
                        [],
                        [new GlobalUsage("<type>.<field>", "pointer", false)],
                        []),
                ]);

        new DocGenerator(graph, DocDescriptorMeanings.Empty).Emit(temp.Path);

        Assert.Contains(
                "| `<type>.<field>` *(name pattern)* | `pointer` |",
                File.ReadAllText(path));
    }

    [Fact]
    public void RejectsUsageDiffFromUnregisteredVersion()
    {
        using TempDirectory temp = new();
        string path = Path.Combine(temp.Path, "Widget.md");
        File.WriteAllText(path,
            "<!-- BEGIN GENERATED: usage contract=Widget version=c2 diff-from=c1 -->\n" +
            "<!-- END GENERATED: usage contract=Widget version=c2 diff-from=c1 -->");
        UsageGraph graph = new(
            "",
            0,
            [new ContractVersionUsage(new ContractVersion(new ContractInterface("IWidget"), "c2"), [], [], [])]);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => new DocGenerator(graph, DocDescriptorMeanings.Empty).Check(temp.Path));

        Assert.Contains("Widget c1", exception.Message);
    }

    [Fact]
    public void RejectsMalformedDescriptorOverrideKey()
    {
        using TempDirectory temp = new();
        string path = Path.Combine(temp.Path, "overrides.json");
        File.WriteAllText(path, """{ "_supplement": { "Thread": ["MissingDot"] } }""");

        Assert.Throws<System.Text.Json.JsonException>(() => DocDescriptorOverrides.Load(path));
    }

    [Fact]
    public void AppliesVersionSpecificDescriptorOverrides()
    {
        using TempDirectory temp = new();
        string docPath = Path.Combine(temp.Path, "Widget.md");
        string overridesPath = Path.Combine(temp.Path, "overrides.json");
        File.WriteAllText(docPath,
            "<!-- BEGIN GENERATED: usage contract=Widget version=c1 -->\n" +
            "<!-- END GENERATED: usage contract=Widget version=c1 -->\n" +
            "<!-- BEGIN GENERATED: usage contract=Widget version=c2 -->\n" +
            "<!-- END GENERATED: usage contract=Widget version=c2 -->");
        File.WriteAllText(
            overridesPath,
            """
            {
              "_supplement": { "Widget@c2": { "Widget.Added": "int32" } },
              "_suppress": {
                "Widget": ["Widget.Common"],
                "Widget@c1": ["Widget.VersionSpecific"]
              }
            }
            """);
        UsageGraph graph = new(
            "",
            0,
            [
                new ContractVersionUsage(
                    new ContractVersion(new ContractInterface("IWidget"), "c1"),
                    [new DataTypeUsage("Data.Widget", false, [new FieldUsage("Common", "int32"), new FieldUsage("VersionSpecific", "int32")])],
                    [],
                    []),
                new ContractVersionUsage(
                    new ContractVersion(new ContractInterface("IWidget"), "c2"),
                    [new DataTypeUsage("Data.Widget", false, [new FieldUsage("Common", "int32"), new FieldUsage("VersionSpecific", "int32")])],
                    [],
                    []),
            ]);

        new DocGenerator(
            graph,
            DocDescriptorMeanings.Empty,
            DocDescriptorOverrides.Load(overridesPath)).Emit(temp.Path);

        string generated = File.ReadAllText(docPath);
        Assert.DoesNotContain("| `Widget` | `Common` |", generated);
        Assert.Contains("| `Widget` | `VersionSpecific` |", generated);
        Assert.Contains("| `Widget` | `Added` | `int32` |", generated);
        Assert.Equal(1, generated.Split("| `Widget` | `VersionSpecific` |").Length - 1);
    }

    [Fact]
    public void UsesSourceVersionTypesForSupplementedDescriptorDiffs()
    {
        using TempDirectory temp = new();
        string docPath = Path.Combine(temp.Path, "Widget.md");
        string overridesPath = Path.Combine(temp.Path, "overrides.json");
        File.WriteAllText(docPath,
            "<!-- BEGIN GENERATED: usage contract=Widget version=c1 -->\n" +
            "<!-- END GENERATED: usage contract=Widget version=c1 -->\n" +
            "<!-- BEGIN GENERATED: usage contract=Widget version=c2 diff-from=c1 -->\n" +
            "<!-- END GENERATED: usage contract=Widget version=c2 diff-from=c1 -->");
        File.WriteAllText(
            overridesPath,
            """
            {
              "_supplement": {
                "Widget@c1": { "Widget.Removed": "pointer" },
                "Widget@c2": { "Widget.Added": "int32" }
              }
            }
            """);
        UsageGraph graph = new(
            "",
            0,
            [
                new ContractVersionUsage(new ContractVersion(new ContractInterface("IWidget"), "c1"), [], [], []),
                new ContractVersionUsage(new ContractVersion(new ContractInterface("IWidget"), "c2"), [], [], []),
            ]);

        new DocGenerator(
            graph,
            DocDescriptorMeanings.Empty,
            DocDescriptorOverrides.Load(overridesPath)).Emit(temp.Path);

        string generated = File.ReadAllText(docPath);
        Assert.Contains("| Added | `Widget` | `Added` | `int32` |", generated);
        Assert.Contains("| Removed | `Widget` | `Removed` | `pointer` |", generated);
    }

    [Fact]
    public void LoadsCanonicalFieldAndGlobalMeanings()
    {
        using TempDirectory temp = new();
        string path = Path.Combine(temp.Path, "meanings.json");
        File.WriteAllText(
            path,
            """
            {
              "_fields": { "Widget.Value": "Widget value" },
              "_globals": { "WidgetStore": "Pointer to the widget store" }
            }
            """);

        DocDescriptorMeanings meanings = DocDescriptorMeanings.Load(path);

        Assert.Equal("Widget value", meanings.Meaning("Widget.Value"));
        Assert.Equal("Pointer to the widget store", meanings.GlobalMeaning("WidgetStore"));
    }

    [Fact]
    public void RejectsContractScopedMeanings()
    {
        using TempDirectory temp = new();
        string path = Path.Combine(temp.Path, "meanings.json");
        File.WriteAllText(path, """{ "Widget": { "Widget.Value": "Widget value" } }""");

        Assert.Throws<System.Text.Json.JsonException>(() => DocDescriptorMeanings.Load(path));
    }

    [Fact]
    public void FormatsManagedCdacNamesAsMarkdownCodeSpans()
    {
        using TempDirectory temp = new();
        string path = Path.Combine(temp.Path, "Thread.md");
        File.WriteAllText(path,
            "<!-- BEGIN GENERATED: usage contract=Thread version=c1 -->\n" +
            "<!-- END GENERATED: usage contract=Thread version=c1 -->");
        UsageGraph graph = new(
            "",
            1,
            [
                new ContractVersionUsage(
                    new ContractVersion(new ContractInterface("IThread"), "c1"),
                    [
                        new DataTypeUsage(
                            "Data.System.Threading.Lock",
                            false,
                            [new FieldUsage("_state", "int32")]),
                        new DataTypeUsage(
                            "Data.System.Collections.Generic.List`1",
                            false,
                            [new FieldUsage("_items", "pointer")]),
                    ],
                    [],
                    []),
            ]);

        new DocGenerator(graph, DocDescriptorMeanings.Empty).Emit(temp.Path);

        string generated = File.ReadAllText(path);
        Assert.Contains(
            "| `System.Threading.Lock` | `_state` | `int32` | _TODO: describe_ |",
            generated);
        Assert.Contains(
            "| ``System.Collections.Generic.List`1`` | `_items` | `pointer` | _TODO: describe_ |",
            generated);
    }

    private static UsageGraph EmptyGraph() => new(
        "",
        0,
        []);

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory() => Path = Directory.CreateTempSubdirectory("CdacUsageGraphTests").FullName;

        public string Path { get; }

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
