// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using CdacUsageGraph.Analysis;
using CdacUsageGraph.Discovery;
using CdacUsageGraph.Model;
using CdacUsageGraph.Semantic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace CdacUsageGraph.Tests;

/// <summary>
/// Standalone-compilation unit tests: build a tiny in-memory cDAC-shaped source and assert the
/// discovery phase resolves Data types and native descriptor field names.
/// </summary>
public sealed class DataTypeIndexTests
{
    private const string UsageWalkerApiSource = """
        global using Target = Microsoft.Diagnostics.DataContractReader.Target;
        global using TypeInfo = Microsoft.Diagnostics.DataContractReader.Target.TypeInfo;

        namespace Microsoft.Diagnostics.DataContractReader
        {
            public abstract class Target
            {
                public readonly struct FieldInfo
                {
                    public int Offset { get; }
                }

                public readonly struct TypeInfo
                {
                    public System.Collections.Generic.IReadOnlyDictionary<string, FieldInfo> Fields { get; }
                    public uint? Size { get; }
                }

                public abstract TypeInfo GetTypeInfo(string name);
                public abstract bool TryGetTypeInfo(string name, out TypeInfo type);
                public abstract ulong ReadGlobalPointer(string name);
                public abstract bool TryReadGlobalPointer(string name, out ulong? value);
                public abstract string ReadGlobalString(string name);
                public abstract bool TryReadGlobalString(string name, out string? value);
                public abstract T ReadGlobal<T>(string name) where T : struct;
                public abstract bool TryReadGlobal<T>(string name, out T? value) where T : struct;
                public abstract T Read<T>(ulong address);
                public abstract void Write<T>(ulong address, T value);
            }
        }

        namespace Microsoft.Diagnostics.DataContractReader.Contracts
        {
            public interface IManagedTypeSource
            {
                Target.TypeInfo GetTypeInfo(string name);
                bool TryGetTypeInfo(string name, out Target.TypeInfo type);
            }
        }
        """;

    private const string Source = """
        namespace Microsoft.Diagnostics.DataContractReader
        {
            public sealed class CdacTypeAttribute : System.Attribute
            {
                public CdacTypeAttribute(params string[] names) { }
            }
            public sealed class FieldAttribute : System.Attribute
            {
                public FieldAttribute() { }
                public FieldAttribute(params string[] names) { }
                public string[]? Names { get; set; }
            }
            [System.AttributeUsage(
                System.AttributeTargets.Property | System.AttributeTargets.Method,
                AllowMultiple = true)]
            public sealed class DataDescriptorDependencyAttribute : System.Attribute
            {
                public DataDescriptorDependencyAttribute(string fieldName, string nativeType, string? typeName = null) { }
            }
            [System.AttributeUsage(
                System.AttributeTargets.Property | System.AttributeTargets.Method,
                AllowMultiple = true)]
            public sealed class UsesDataDescriptorTypeSizeAttribute : System.Attribute
            {
                public UsesDataDescriptorTypeSizeAttribute(string? typeName = null) { }
            }
        }
        namespace Microsoft.Diagnostics.DataContractReader.Data
        {
            public interface IData<T> { }

            [Microsoft.Diagnostics.DataContractReader.CdacType("Widget")]
            public sealed partial class Widget : IData<Widget>
            {
                [Microsoft.Diagnostics.DataContractReader.Field("m_value")]
                [Microsoft.Diagnostics.DataContractReader.DataDescriptorDependency("m_value", "int32")]
                public int Value { get; }

                [Microsoft.Diagnostics.DataContractReader.Field]
                [Microsoft.Diagnostics.DataContractReader.DataDescriptorDependency("Count", "int32")]
                public int Count { get; }

                [Microsoft.Diagnostics.DataContractReader.DataDescriptorDependency("m_value", "int32")]
                [Microsoft.Diagnostics.DataContractReader.DataDescriptorDependency("Count", "int32", "DescriptorOnly")]
                [Microsoft.Diagnostics.DataContractReader.UsesDataDescriptorTypeSize]
                [Microsoft.Diagnostics.DataContractReader.UsesDataDescriptorTypeSize("DescriptorOnly")]
                public void WriteValue(int value) { }
            }

            [Microsoft.Diagnostics.DataContractReader.CdacType("NotData")]
            public sealed class NotData { }
        }
        namespace Unrelated
        {
            public interface IData<T> { }

            public sealed class Lookalike : IData<Lookalike> { }
        }
        """;

    private static (CSharpCompilation Compilation, INamedTypeSymbol Widget) BuildWidget()
    {
        CSharpCompilation compilation = CSharpCompilation.Create(
            "DataTypeIndexTest",
            [CSharpSyntaxTree.ParseText(Source)],
            RuntimeReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        INamedTypeSymbol? widget = compilation.GetTypeByMetadataName("Microsoft.Diagnostics.DataContractReader.Data.Widget");
        Assert.NotNull(widget);
        return (compilation, widget!);
    }

    private static IEnumerable<MetadataReference> RuntimeReferences()
    {
        string tpa = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
        foreach (string path in tpa.Split(Path.PathSeparator))
        {
            if (path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) && File.Exists(path))
                yield return MetadataReference.CreateFromFile(path);
        }
    }

    [Fact]
    public void ParsesRepeatedTypedDescriptorDependenciesAndTypeSizes()
    {
        (CSharpCompilation compilation, INamedTypeSymbol widget) = BuildWidget();
        IMethodSymbol writeValue = (IMethodSymbol)widget.GetMembers("WriteValue").Single();
        CdacAttributeMatcher attributes = new(compilation);

        Assert.True(attributes.TryGetDescriptorDependencies(
            writeValue,
            out DataDescriptorDependencies dependencies));
        Assert.Equal(
            [
                new DataDescriptorFieldDependency("m_value", "int32"),
                new DataDescriptorFieldDependency("Count", "int32", "DescriptorOnly"),
            ],
            dependencies.Fields);
        Assert.Equal([null, "DescriptorOnly"], dependencies.TypeSizeTypeNames);
    }

    [Fact]
    public void RecordsDependenciesUnderExplicitDescriptorOnlyType()
    {
        const string contractSource = """
            namespace Microsoft.Diagnostics.DataContractReader.Contracts
            {
                public interface ITest
                {
                    void Read(Microsoft.Diagnostics.DataContractReader.Data.Widget value);
                }

                public sealed class TestContract : ITest
                {
                    public void Read(Microsoft.Diagnostics.DataContractReader.Data.Widget value)
                        => value.WriteValue(0);
                }
            }
            """;
        CSharpCompilation compilation = CSharpCompilation.Create(
            "ExplicitDescriptorTypeTest",
            [
                CSharpSyntaxTree.ParseText(UsageWalkerApiSource),
                CSharpSyntaxTree.ParseText(Source),
                CSharpSyntaxTree.ParseText(contractSource),
            ],
            RuntimeReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        INamedTypeSymbol implementation = compilation.GetTypeByMetadataName(
            "Microsoft.Diagnostics.DataContractReader.Contracts.TestContract")!;
        ContractVersion label = new(new ContractInterface("ITest"), "c1");
        UsageGraph graph = new UsageWalker(compilation, DataTypeDiscovery.BuildIndex(compilation)).Walk(
            [Registration(label, implementation)],
            "");

        Assert.Equal(
            "int32",
            Field(graph, label, "Data.DescriptorOnly", "Count").Type);
        Assert.True(DataType(graph, label, "Data.DescriptorOnly").UsesTypeSize);
    }

    [Fact]
    public void UsageCollectorRecordsTypedField()
    {
        (CSharpCompilation compilation, INamedTypeSymbol widget) = BuildWidget();
        DataTypeIndex index = DataTypeDiscovery.BuildIndex(compilation);
        Assert.True(index.TryGetDataType(widget, out DataDescriptorType dataType));
        UsageCollector collector = new(index);
        ContractVersion label = new(new ContractInterface("ITest"), "c1");

        collector.RecordField(
            label,
            dataType,
            new DataDescriptorFieldDependency("Value", "pointer"));

        FieldUsage usage = Field(
            collector.Build("", 1),
            label,
            "Data.Widget",
            "Value");
        Assert.Equal("pointer", usage.Type);
    }

    [Fact]
    public void UsageCollectorRejectsConflictingFieldTypes()
    {
        (CSharpCompilation compilation, INamedTypeSymbol widget) = BuildWidget();
        DataTypeIndex index = DataTypeDiscovery.BuildIndex(compilation);
        Assert.True(index.TryGetDataType(widget, out DataDescriptorType dataType));
        UsageCollector collector = new(index);
        ContractVersion label = new(new ContractInterface("ITest"), "c1");
        collector.RecordField(
            label,
            dataType,
            new DataDescriptorFieldDependency("Value", "pointer"));

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => collector.RecordField(
                label,
                dataType,
                new DataDescriptorFieldDependency("Value", "uint32")));

        Assert.Contains("Data.Widget.Value", exception.Message);
    }

    [Fact]
    public void DiscoversCdacTypeDataTypes()
    {
        (CSharpCompilation compilation, INamedTypeSymbol widget) = BuildWidget();

        DataTypeIndex index = DataTypeDiscovery.BuildIndex(compilation);

        Assert.True(index.IsDataType(widget));
        Assert.False(index.IsDataType(compilation.GetTypeByMetadataName(
            "Microsoft.Diagnostics.DataContractReader.Data.NotData")));
        Assert.False(index.IsDataType(compilation.GetTypeByMetadataName(
            "Unrelated.Lookalike")));
        Assert.Equal(1, index.Count);
    }

    [Theory]
    [InlineData("Value", "m_value", "int32")]
    [InlineData("Count", "Count", "int32")]
    public void ResolvesDescriptorDependencies(
        string property,
        string expectedField,
        string expectedType)
    {
        (CSharpCompilation compilation, INamedTypeSymbol widget) = BuildWidget();
        IPropertySymbol symbol = (IPropertySymbol)widget.GetMembers(property).Single();
        CdacAttributeMatcher attributes = new(compilation);

        Assert.True(attributes.TryGetDescriptorDependencies(
            symbol,
            out DataDescriptorDependencies dependencies));
        Assert.Equal(
            [new DataDescriptorFieldDependency(expectedField, expectedType)],
            dependencies.Fields);
        Assert.Empty(dependencies.TypeSizeTypeNames);
    }

    [Fact]
    public void ResolvesCdacNameToDataClass()
    {
        (CSharpCompilation compilation, INamedTypeSymbol widget) = BuildWidget();
        DataTypeIndex index = DataTypeDiscovery.BuildIndex(compilation);

        Assert.True(index.TryGetType("Widget", out DataDescriptorType resolved));
        Assert.Equal(widget, resolved.Symbol, SymbolEqualityComparer.Default);
    }

    [Fact]
    public void UsesFirstCdacNameWhenItDiffersFromClassName()
    {
        const string source = """
            namespace Microsoft.Diagnostics.DataContractReader
            {
                public enum DataType { WidgetTable }
                public sealed class CdacTypeAttribute : System.Attribute
                {
                    public CdacTypeAttribute(params string[] names) { }
                }
            }
            namespace Microsoft.Diagnostics.DataContractReader.Data
            {
                public interface IData<T> { }

                [Microsoft.Diagnostics.DataContractReader.CdacType(
                    nameof(Microsoft.Diagnostics.DataContractReader.DataType.WidgetTable))]
                public sealed class WidgetEntry : IData<WidgetEntry> { }

                [Microsoft.Diagnostics.DataContractReader.CdacType("Managed.Layout")]
                public sealed class ManagedLayout : IData<ManagedLayout> { }
            }
            """;
        CSharpCompilation compilation = CSharpCompilation.Create(
            "CdacNameTest",
            [CSharpSyntaxTree.ParseText(source)],
            RuntimeReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        INamedTypeSymbol entry = compilation.GetTypeByMetadataName(
            "Microsoft.Diagnostics.DataContractReader.Data.WidgetEntry")!;

        DataTypeIndex index = DataTypeDiscovery.BuildIndex(compilation);

        Assert.True(index.TryGetDataType(entry, out DataDescriptorType dataType));
        Assert.Equal("WidgetTable", dataType.Name);

        INamedTypeSymbol managedLayout = compilation.GetTypeByMetadataName(
            "Microsoft.Diagnostics.DataContractReader.Data.ManagedLayout")!;
        Assert.True(index.TryGetDataType(managedLayout, out DataDescriptorType managedDataType));
        Assert.Equal("Managed.Layout", managedDataType.Name);
        Assert.True(index.TryGetType("Managed.Layout", out DataDescriptorType resolvedManagedType));
        Assert.Equal(managedLayout, resolvedManagedType.Symbol, SymbolEqualityComparer.Default);
    }

    [Fact]
    public void ResolvesDependenciesOnInheritedProperties()
    {
        const string source = """
            namespace Microsoft.Diagnostics.DataContractReader
            {
                public sealed class FieldAttribute : System.Attribute { }
                public sealed class DataDescriptorDependencyAttribute : System.Attribute
                {
                    public DataDescriptorDependencyAttribute(string fieldName, string nativeType) { }
                }
            }
            namespace Microsoft.Diagnostics.DataContractReader.Data
            {
                public interface IData<T> { }

                public class Base
                {
                    [Microsoft.Diagnostics.DataContractReader.Field]
                    [Microsoft.Diagnostics.DataContractReader.DataDescriptorDependency("Value", "int32")]
                    public int Value { get; }
                }

                public sealed class Derived : Base, IData<Derived> { }
            }
            """;
        CSharpCompilation compilation = CSharpCompilation.Create(
            "InheritedDataPropertyTest",
            [CSharpSyntaxTree.ParseText(source)],
            RuntimeReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        INamedTypeSymbol derived = compilation.GetTypeByMetadataName(
            "Microsoft.Diagnostics.DataContractReader.Data.Derived")!;
        IPropertySymbol baseValue = (IPropertySymbol)compilation.GetTypeByMetadataName(
            "Microsoft.Diagnostics.DataContractReader.Data.Base")!
            .GetMembers("Value").Single();

        DataTypeIndex index = DataTypeDiscovery.BuildIndex(compilation);
        CdacAttributeMatcher attributes = new(compilation);

        Assert.True(index.TryGetDataType(derived, out _));
        Assert.True(attributes.TryGetDescriptorDependencies(
            baseValue,
            out DataDescriptorDependencies dependencies));
        Assert.Equal(
            [new DataDescriptorFieldDependency("Value", "int32")],
            dependencies.Fields);
    }

    [Fact]
    public void PropertiesWithoutAttributesDoNotDefineDependencies()
    {
        const string source = """
            namespace Microsoft.Diagnostics.DataContractReader
            {
                public sealed class FieldAttribute : System.Attribute { }
            }
            namespace Microsoft.Diagnostics.DataContractReader.Data
            {
                public interface IData<T> { }

                public sealed class SetterOnly : IData<SetterOnly>
                {
                    [Microsoft.Diagnostics.DataContractReader.Field]
                    public int Raw { get; private set; }

                    public int Value
                    {
                        get;
                        set { Raw = value; }
                    }
                }
            }
            """;
        CSharpCompilation compilation = CSharpCompilation.Create(
            "SetterBodyPropertyTest",
            [CSharpSyntaxTree.ParseText(source)],
            RuntimeReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        INamedTypeSymbol setterOnly = compilation.GetTypeByMetadataName(
            "Microsoft.Diagnostics.DataContractReader.Data.SetterOnly")!;
        IPropertySymbol value = (IPropertySymbol)setterOnly.GetMembers("Value").Single();

        CdacAttributeMatcher attributes = new(compilation);
        Assert.False(attributes.TryGetDescriptorDependencies(value, out _));
    }

    [Fact]
    public void ConstructorsDoNotDefinePropertyDependencies()
    {
        const string handwrittenSource = """
            namespace Microsoft.Diagnostics.DataContractReader.Data
            {
                public interface IData<T> { }

                public sealed partial class Widget : IData<Widget>
                {
                    public int Derived { get; private set; }
                    public int GeneratedOnly { get; private set; }

                    public Widget(int value)
                    {
                        Derived = value;
                    }
                }
            }
            """;
        const string generatedSource = """
            namespace Microsoft.Diagnostics.DataContractReader.Data
            {
                public sealed partial class Widget
                {
                    public Widget()
                    {
                        Derived = 1;
                        GeneratedOnly = 2;
                    }
                }
            }
            """;
        CSharpCompilation compilation = CSharpCompilation.Create(
            "GeneratedConstructorPropertyTest",
            [
                CSharpSyntaxTree.ParseText(handwrittenSource, path: "Widget.cs"),
                CSharpSyntaxTree.ParseText(generatedSource, path: "Widget.g.cs"),
            ],
            RuntimeReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        INamedTypeSymbol widget = compilation.GetTypeByMetadataName(
            "Microsoft.Diagnostics.DataContractReader.Data.Widget")!;
        CdacAttributeMatcher attributes = new(compilation);

        IPropertySymbol derived = (IPropertySymbol)widget.GetMembers("Derived").Single();
        Assert.False(attributes.TryGetDescriptorDependencies(derived, out _));

        IPropertySymbol generatedOnly = (IPropertySymbol)widget.GetMembers("GeneratedOnly").Single();
        Assert.False(attributes.TryGetDescriptorDependencies(generatedOnly, out _));
    }

    [Fact]
    public void DependencyAttributesReplaceOnInitInference()
    {
        const string source = """
            namespace Microsoft.Diagnostics.DataContractReader
            {
                public sealed class CdacTypeAttribute : System.Attribute
                {
                    public CdacTypeAttribute(params string[] names) { }
                }
                public sealed class FieldAttribute : System.Attribute { }
                [System.AttributeUsage(
                    System.AttributeTargets.Property | System.AttributeTargets.Method,
                    AllowMultiple = true)]
                public sealed class DataDescriptorDependencyAttribute : System.Attribute
                {
                    public DataDescriptorDependencyAttribute(string fieldName, string nativeType) { }
                }
            }
            namespace Microsoft.Diagnostics.DataContractReader.Data
            {
                public interface IData<T> { }

                [Microsoft.Diagnostics.DataContractReader.CdacType("Widget")]
                public sealed partial class Widget : IData<Widget>
                {
                    [Microsoft.Diagnostics.DataContractReader.Field]
                    [Microsoft.Diagnostics.DataContractReader.DataDescriptorDependency("Raw", "int32")]
                    public int Raw { get; }

                    [Microsoft.Diagnostics.DataContractReader.DataDescriptorDependency("Raw", "int32")]
                    public int Derived { get; private set; }

                    [Microsoft.Diagnostics.DataContractReader.DataDescriptorDependency("Raw", "int32")]
                    public int CompoundDerived { get; private set; }

                    partial void OnInit();
                }

                public sealed partial class Widget
                {
                    partial void OnInit()
                    {
                        Derived = Raw;
                        CompoundDerived += Raw;
                    }
                }
            }
            namespace Microsoft.Diagnostics.DataContractReader.Contracts
            {
                public interface ITest
                {
                    int Read(Microsoft.Diagnostics.DataContractReader.Data.Widget widget);
                }

                public sealed class TestContract : ITest
                {
                    public int Read(Microsoft.Diagnostics.DataContractReader.Data.Widget widget)
                        => widget.Derived + widget.CompoundDerived;
                }
            }
            """;
        CSharpCompilation compilation = CSharpCompilation.Create(
            "OnInitPropertyTest",
            [CSharpSyntaxTree.ParseText(source)],
            RuntimeReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        INamedTypeSymbol impl = compilation.GetTypeByMetadataName(
            "Microsoft.Diagnostics.DataContractReader.Contracts.TestContract")!;
        DataTypeIndex index = DataTypeDiscovery.BuildIndex(compilation);
        UsageGraph graph = new UsageWalker(compilation, index).Walk(
            [Registration(
                new ContractVersion(new ContractInterface("ITest"), "c1"),
                impl)], "");
        IReadOnlyCollection<FieldUsage> fields = DataType(
            graph,
            new ContractVersion(new ContractInterface("ITest"), "c1"),
            "Data.Widget").Fields;

        Assert.Contains("Raw", fields.Select(field => field.Name));
        Assert.DoesNotContain("Derived", fields.Select(field => field.Name));
        Assert.DoesNotContain("CompoundDerived", fields.Select(field => field.Name));
    }

    [Fact]
    public void CompoundAssignmentRightOperandIsReadOnly()
    {
        const string source = """
            namespace Microsoft.Diagnostics.DataContractReader
            {
                public sealed class CdacTypeAttribute : System.Attribute
                {
                    public CdacTypeAttribute(params string[] names) { }
                }
                public sealed class FieldAttribute : System.Attribute { }
                public sealed class DataDescriptorDependencyAttribute : System.Attribute
                {
                    public DataDescriptorDependencyAttribute(string fieldName, string nativeType) { }
                }
            }
            namespace Microsoft.Diagnostics.DataContractReader.Data
            {
                public interface IData<T> { }

                [Microsoft.Diagnostics.DataContractReader.CdacType("Widget")]
                public sealed class Widget : IData<Widget>
                {
                    [Microsoft.Diagnostics.DataContractReader.Field]
                    [Microsoft.Diagnostics.DataContractReader.DataDescriptorDependency("Value", "int32")]
                    public int Value { get; }
                }
            }
            namespace Microsoft.Diagnostics.DataContractReader.Contracts
            {
                public interface ITest
                {
                    int Read(Microsoft.Diagnostics.DataContractReader.Data.Widget widget);
                }

                public sealed class TestContract : ITest
                {
                    public int Read(Microsoft.Diagnostics.DataContractReader.Data.Widget widget)
                    {
                        int value = 1;
                        value += widget.Value;
                        return value;
                    }
                }
            }
            """;
        CSharpCompilation compilation = CSharpCompilation.Create(
            "CompoundAssignmentReadTest",
            [CSharpSyntaxTree.ParseText(source)],
            RuntimeReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        INamedTypeSymbol impl = compilation.GetTypeByMetadataName(
            "Microsoft.Diagnostics.DataContractReader.Contracts.TestContract")!;
        DataTypeIndex index = DataTypeDiscovery.BuildIndex(compilation);
        UsageGraph graph = new UsageWalker(compilation, index).Walk(
            [Registration(
                new ContractVersion(new ContractInterface("ITest"), "c1"),
                impl)], "");
        FieldUsage usage = Field(
            graph,
            new ContractVersion(new ContractInterface("ITest"), "c1"),
            "Data.Widget",
            "Value");

        Assert.Equal("Value", usage.Name);
    }

    [Fact]
    public void UsageGraphMergesGlobalAccessRequirements()
    {
        const string source = """
            namespace Microsoft.Diagnostics.DataContractReader.Data
            {
                public interface IData<T> { }
            }

            namespace Example
            {
                public interface ITest
                {
                    void Read(Target target);
                }

                public sealed class TestContract : ITest
                {
                    public void Read(Target target)
                    {
                        _ = target.TryReadGlobalPointer("SharedGlobal", out _);
                        _ = target.ReadGlobalPointer("SharedGlobal");
                    }
                }
            }
            """;
        CSharpCompilation compilation = CreateAnalysisCompilation(source);
        INamedTypeSymbol implementation = compilation.GetTypeByMetadataName(
            "Example.TestContract")!;
        UsageGraph graph = new UsageWalker(
            compilation,
            DataTypeDiscovery.BuildIndex(compilation)).Walk(
                [Registration(
                    new ContractVersion(new ContractInterface("ITest"), "c1"),
                    implementation)],
                "");
        GlobalUsage usage = Contract(
            graph,
            new ContractVersion(new ContractInterface("ITest"), "c1")).Globals.Single(
                global => global.Name == "SharedGlobal");

        Assert.Equal("pointer", usage.Type);
        Assert.False(usage.IsOptional);
    }

    private static CSharpCompilation CreateAnalysisCompilation(string source) =>
        CSharpCompilation.Create(
            "UsageWalkerTest",
            [
                CSharpSyntaxTree.ParseText(UsageWalkerApiSource, path: "UsageWalkerApi.cs"),
                CSharpSyntaxTree.ParseText(source, path: "Input.cs"),
            ],
            RuntimeReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    [Fact]
    public void InterfaceComputedPropertyUsesSameProvenanceAsDirectRead()
    {
        const string source = """
            namespace Microsoft.Diagnostics.DataContractReader
            {
                public sealed class CdacTypeAttribute : System.Attribute
                {
                    public CdacTypeAttribute(params string[] names) { }
                }
                public sealed class FieldAttribute : System.Attribute { }
                [System.AttributeUsage(
                    System.AttributeTargets.Property | System.AttributeTargets.Method,
                    AllowMultiple = true)]
                public sealed class DataDescriptorDependencyAttribute : System.Attribute
                {
                    public DataDescriptorDependencyAttribute(string fieldName, string nativeType) { }
                }
            }
            namespace Microsoft.Diagnostics.DataContractReader.Data
            {
                public interface IData<T> { }
                public interface IValue { int Value { get; } }

                [Microsoft.Diagnostics.DataContractReader.CdacType("Computed")]
                public sealed class Computed : IData<Computed>, IValue
                {
                    [Microsoft.Diagnostics.DataContractReader.Field]
                    [Microsoft.Diagnostics.DataContractReader.DataDescriptorDependency("Raw", "int32")]
                    public int Raw { get; }

                    public int Value => Raw;
                }

                [Microsoft.Diagnostics.DataContractReader.CdacType("Direct")]
                public sealed class Direct : IData<Direct>, IValue
                {
                    [Microsoft.Diagnostics.DataContractReader.Field]
                    [Microsoft.Diagnostics.DataContractReader.DataDescriptorDependency("Value", "int32")]
                    public int Value { get; }
                }
            }
            namespace Microsoft.Diagnostics.DataContractReader.Contracts
            {
                public interface ITest
                {
                    int Read(Microsoft.Diagnostics.DataContractReader.Data.IValue value);
                }

                public sealed class TestContract : ITest
                {
                    public int Read(Microsoft.Diagnostics.DataContractReader.Data.IValue value)
                        => value.Value;
                }
            }
            """;
        CSharpCompilation compilation = CSharpCompilation.Create(
            "InterfaceComputedPropertyTest",
            [CSharpSyntaxTree.ParseText(source)],
            RuntimeReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        INamedTypeSymbol impl = compilation.GetTypeByMetadataName(
            "Microsoft.Diagnostics.DataContractReader.Contracts.TestContract")!;
        DataTypeIndex index = DataTypeDiscovery.BuildIndex(compilation);
        UsageGraph graph = new UsageWalker(compilation, index).Walk(
            [Registration(
                new ContractVersion(new ContractInterface("ITest"), "c1"),
                impl)], "");

        Assert.Contains(
            "Raw",
            DataType(graph, new ContractVersion(new ContractInterface("ITest"), "c1"), "Data.Computed")
                .Fields.Select(field => field.Name));
        Assert.DoesNotContain(
            "Value",
            DataType(graph, new ContractVersion(new ContractInterface("ITest"), "c1"), "Data.Computed")
                .Fields.Select(field => field.Name));
        Assert.Contains(
            "Value",
            DataType(graph, new ContractVersion(new ContractInterface("ITest"), "c1"), "Data.Direct")
                .Fields.Select(field => field.Name));
    }

    [Fact]
    public void WalksGenericMemberForEachTypeSubstitution()
    {
        const string source = """
            namespace Microsoft.Diagnostics.DataContractReader
            {
                public sealed class CdacTypeAttribute : System.Attribute
                {
                    public CdacTypeAttribute(params string[] names) { }
                }
            }
            namespace Microsoft.Diagnostics.DataContractReader.Data
            {
                public interface IData<T> { }
                [Microsoft.Diagnostics.DataContractReader.CdacType("First")]
                public sealed class First : IData<First> { }
                [Microsoft.Diagnostics.DataContractReader.CdacType("Second")]
                public sealed class Second : IData<Second> { }
            }
            namespace Microsoft.Diagnostics.DataContractReader.Contracts
            {
                using Microsoft.Diagnostics.DataContractReader.Data;

                public sealed class ProcessedData
                {
                    public T GetOrAdd<T>(int address) => default!;
                }

                public sealed class Helper<T>
                {
                    private readonly ProcessedData _data;
                    public Helper(ProcessedData data) => _data = data;
                    public T Load() => _data.GetOrAdd<T>(0);
                }

                public interface ITest
                {
                    void ReadBoth();
                }

                public sealed class TestContract : ITest
                {
                    private readonly ProcessedData _data = new();
                    public void ReadBoth()
                    {
                        _ = new Helper<First>(_data).Load();
                        _ = new Helper<Second>(_data).Load();
                    }
                }
            }
            """;
        CSharpCompilation compilation = CSharpCompilation.Create(
            "GenericSubstitutionTest",
            [CSharpSyntaxTree.ParseText(source)],
            RuntimeReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        INamedTypeSymbol impl = compilation.GetTypeByMetadataName(
            "Microsoft.Diagnostics.DataContractReader.Contracts.TestContract")!;
        DataTypeIndex index = DataTypeDiscovery.BuildIndex(compilation);
        UsageGraph graph = new UsageWalker(
            compilation, index).Walk(
                [Registration(
                    new ContractVersion(new ContractInterface("ITest"), "c1"),
                    impl)], "");
        ContractVersion label = new(new ContractInterface("ITest"), "c1");
        HashSet<string> used = Contract(graph, label).DataTypes
            .Select(dataType => dataType.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("Data.First", used);
        Assert.Contains("Data.Second", used);
    }

    [Fact]
    public void DataMethodDependenciesStayInCallingContractContext()
    {
        const string source = """
            namespace Microsoft.Diagnostics.DataContractReader
            {
                public sealed class CdacTypeAttribute : System.Attribute
                {
                    public CdacTypeAttribute(params string[] names) { }
                }
                public sealed class DataDescriptorDependencyAttribute : System.Attribute
                {
                    public DataDescriptorDependencyAttribute(string fieldName, string nativeType) { }
                }
            }
            namespace Microsoft.Diagnostics.DataContractReader.Data
            {
                public interface IData<T> { }

                [Microsoft.Diagnostics.DataContractReader.CdacType("First")]
                public sealed class First : IData<First>
                {
                    [Microsoft.Diagnostics.DataContractReader.DataDescriptorDependency("Value", "int32")]
                    public static int Read(Target target) => 0;
                }

                [Microsoft.Diagnostics.DataContractReader.CdacType("Second")]
                public sealed class Second : IData<Second>
                {
                    [Microsoft.Diagnostics.DataContractReader.DataDescriptorDependency("Value", "int32")]
                    public static int Read(Target target) => 0;
                }
            }
            namespace Example
            {
                public interface IFirst
                {
                    int Read(Target target);
                }

                public interface ISecond
                {
                    int Read(Target target);
                }

                public sealed class FirstContract : IFirst
                {
                    public int Read(Target target)
                        => Microsoft.Diagnostics.DataContractReader.Data.First.Read(target);
                }

                public sealed class SecondContract : ISecond
                {
                    public int Read(Target target)
                        => Microsoft.Diagnostics.DataContractReader.Data.Second.Read(target);
                }
            }
            """;
        CSharpCompilation compilation = CreateAnalysisCompilation(source);
        DataTypeIndex index = DataTypeDiscovery.BuildIndex(compilation);
        UsageGraph graph = new UsageWalker(
            compilation,
            index).Walk(
                [
                    Registration(
                        new ContractVersion(new ContractInterface("IFirst"), "c1"),
                        compilation.GetTypeByMetadataName("Example.FirstContract")!),
                    Registration(
                        new ContractVersion(new ContractInterface("ISecond"), "c1"),
                        compilation.GetTypeByMetadataName("Example.SecondContract")!),
                ],
                "");

        Assert.Contains(
            Contract(graph, new ContractVersion(new ContractInterface("IFirst"), "c1")).DataTypes,
            dataType => dataType.Name == "Data.First");
        Assert.DoesNotContain(
            Contract(graph, new ContractVersion(new ContractInterface("IFirst"), "c1")).DataTypes,
            dataType => dataType.Name == "Data.Second");
        Assert.Contains(
            Contract(graph, new ContractVersion(new ContractInterface("ISecond"), "c1")).DataTypes,
            dataType => dataType.Name == "Data.Second");
        Assert.DoesNotContain(
            Contract(graph, new ContractVersion(new ContractInterface("ISecond"), "c1")).DataTypes,
            dataType => dataType.Name == "Data.First");
    }

    [Fact]
    public void ContractEntryPointUsesMostDerivedInterfaceOverride()
    {
        const string source = """
            namespace Microsoft.Diagnostics.DataContractReader
            {
                public sealed class CdacTypeAttribute : System.Attribute
                {
                    public CdacTypeAttribute(params string[] names) { }
                }
                public sealed class FieldAttribute : System.Attribute { }
                public sealed class DataDescriptorDependencyAttribute : System.Attribute
                {
                    public DataDescriptorDependencyAttribute(string fieldName, string nativeType) { }
                }
            }
            namespace Microsoft.Diagnostics.DataContractReader.Data
            {
                public interface IData<T> { }

                [Microsoft.Diagnostics.DataContractReader.CdacType("BaseData")]
                public sealed class BaseData : IData<BaseData>
                {
                    [Microsoft.Diagnostics.DataContractReader.Field]
                    [Microsoft.Diagnostics.DataContractReader.DataDescriptorDependency("Value", "int32")]
                    public int Value { get; }
                }

                [Microsoft.Diagnostics.DataContractReader.CdacType("DerivedData")]
                public sealed class DerivedData : IData<DerivedData>
                {
                    [Microsoft.Diagnostics.DataContractReader.Field]
                    [Microsoft.Diagnostics.DataContractReader.DataDescriptorDependency("Value", "int32")]
                    public int Value { get; }
                }
            }
            namespace Example
            {
                public interface ITest
                {
                    int Read(
                        Microsoft.Diagnostics.DataContractReader.Data.BaseData baseData,
                        Microsoft.Diagnostics.DataContractReader.Data.DerivedData derivedData);
                }

                public class Base : ITest
                {
                    public virtual int Read(
                        Microsoft.Diagnostics.DataContractReader.Data.BaseData baseData,
                        Microsoft.Diagnostics.DataContractReader.Data.DerivedData derivedData)
                        => baseData.Value;
                }

                public sealed class Derived : Base
                {
                    public override int Read(
                        Microsoft.Diagnostics.DataContractReader.Data.BaseData baseData,
                        Microsoft.Diagnostics.DataContractReader.Data.DerivedData derivedData)
                        => derivedData.Value;
                }
            }
            """;
        CSharpCompilation compilation = CSharpCompilation.Create(
            "InterfaceOverrideReachabilityTest",
            [CSharpSyntaxTree.ParseText(source)],
            RuntimeReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        INamedTypeSymbol implementation = compilation.GetTypeByMetadataName("Example.Derived")!;
        UsageGraph graph = new UsageWalker(
            compilation,
            DataTypeDiscovery.BuildIndex(compilation)).Walk(
                [Registration(
                    new ContractVersion(new ContractInterface("ITest"), "c1"),
                    implementation)],
                "");
        ContractVersion label = new(new ContractInterface("ITest"), "c1");

        Assert.Contains(
            "Value",
            DataType(graph, label, "Data.DerivedData").Fields.Select(field => field.Name));
        Assert.DoesNotContain(
            Contract(graph, label).DataTypes,
            dataType => dataType.Name == "Data.BaseData");
    }

    [Fact]
    public void DiscoversRegistrationsOutsideMethodNamedRegisterAndIgnoresNestedHelpers()
    {
        const string source = """
            namespace Microsoft.Diagnostics.DataContractReader
            {
                public sealed class ContractRegistry
                {
                    public void Register<T>(string version, System.Func<object, T> factory) { }
                }
            }
            namespace Microsoft.Diagnostics.DataContractReader.Contracts
            {
                public interface IContract { }
                public interface ITest : IContract { }
                public sealed class Helper { }
                public sealed class Impl : ITest
                {
                    public Impl(Helper helper) { }
                }
                public static class CoreCLRContracts
                {
                    public static void Configure(
                        Microsoft.Diagnostics.DataContractReader.ContractRegistry registry)
                    {
                        registry.Register<ITest>("c1", _ => new Impl(new Helper()));
                        new UnrelatedRegistry().Register<ITest>("c2", _ => new Impl(new Helper()));
                        registry.Register<ITest>("c3", _ => new NonContractImplementation());
                    }
                }

                public sealed class NonContractImplementation { }
                public sealed class UnrelatedRegistry
                {
                    public void Register<T>(string version, System.Func<object, T> factory) { }
                }
            }
            """;
        CSharpCompilation compilation = CSharpCompilation.Create(
            "RegistrationDiscoveryTest",
            [CSharpSyntaxTree.ParseText(source)],
            RuntimeReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        IReadOnlyList<ContractRegistration> registrations =
            ContractRegistrationParser.Parse(compilation);

        ContractRegistration registration = Assert.Single(registrations);
        Assert.Equal(new ContractVersion(new ContractInterface("ITest"), "c1"), registration.Label);
        Assert.Equal("ITest", registration.Interface.Name);
        Assert.Equal("Impl", registration.Impl.Name);
        Assert.Equal("Impl", registration.Constructor.ContainingType.Name);
    }

    [Fact]
    public void ContractEntryPointsFindsInterfacesReturnedThroughOutParameters()
    {
        CSharpCompilation compilation = CSharpCompilation.Create(
            "ReturnedInterfaceTest",
            [CSharpSyntaxTree.ParseText("""
                namespace Example
                {
                    public interface IReturnHandle { }
                    public interface IOutHandle { }

                    public interface ITest
                    {
                        System.Collections.Generic.IEnumerable<IReturnHandle> GetHandles();
                        void GetHandle(out IOutHandle handle);
                    }

                    public sealed class Impl : ITest
                    {
                        System.Collections.Generic.IEnumerable<IReturnHandle> ITest.GetHandles()
                            => System.Array.Empty<IReturnHandle>();

                        void ITest.GetHandle(out IOutHandle handle) => handle = null;
                    }
                }
                """)],
            RuntimeReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        ContractRegistration registration = Registration(
            new ContractVersion(new ContractInterface("ITest"), "c1"),
            compilation.GetTypeByMetadataName("Example.Impl")!);

        IReadOnlySet<INamedTypeSymbol> interfaces =
            ContractEntryPoints.GetReturnedInterfaces(registration);

        Assert.Contains(interfaces, @interface => @interface.Name == "IReturnHandle");
        Assert.Contains(interfaces, @interface => @interface.Name == "IOutHandle");
    }

    private static ContractRegistration Registration(
        ContractVersion label,
        INamedTypeSymbol implementation) =>
        new(
            label,
            implementation.AllInterfaces.Single(
                @interface => @interface.Name == label.Interface.Name),
            implementation,
            implementation.InstanceConstructors.Single(
                constructor => constructor.Parameters.Length == 0));

    private static ContractVersionUsage Contract(UsageGraph graph, ContractVersion label) =>
        graph.Contracts.Single(contract => contract.Label == label);

    private static DataTypeUsage DataType(
        UsageGraph graph,
        ContractVersion label,
        string name) =>
        Contract(graph, label).DataTypes.Single(dataType => dataType.Name == name);

    private static FieldUsage Field(
        UsageGraph graph,
        ContractVersion label,
        string dataType,
        string field) =>
        DataType(graph, label, dataType).Fields.Single(usage => usage.Name == field);
}
