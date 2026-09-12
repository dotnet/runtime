// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Interop;
using Microsoft.Interop.UnitTests;
using SourceGenerators;
using Xunit;

namespace LibraryImportGenerator.UnitTests;

public class CodeWritingTests
{
    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    [InlineData("\r")]
    public void WriterIndentsMultilineTextWithoutIndentingEmptyLines(string newline)
    {
        var writer = new IndentedTextWriter();
        writer.WriteLine("class C");
        using (writer.WriteBlock())
        {
            writer.Write($"first{newline}{newline}second{newline}");
            using (writer.WriteBlock())
            {
                writer.Write("thi");
                writer.WriteLine("rd");
            }
        }

        string expected = """
            class C
            {
                first

                second
                {
                    third
                }
            }

            """.ReplaceLineEndings("\r\n");
        Assert.Equal(expected, writer.ToString());
        Assert.Equal(writer.ToString().Length, writer.Length);
        Assert.Equal(0, writer.Indent);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WriterHandlesLineEndingsSplitAcrossWrites(bool writeCharacters)
    {
        var writer = new IndentedTextWriter();
        using (writer.WriteBlock())
        {
            writer.Write("first\r");
            if (writeCharacters)
            {
                writer.Write('\n');
            }
            else
            {
                writer.Write("\n");
            }
            writer.WriteLine("second");
        }

        string expected = """
            {
                first
                second
            }

            """.ReplaceLineEndings("\r\n");
        Assert.Equal(expected, writer.ToString());
    }

    [Fact]
    public void WriterCanBeReusedAfterClosingBlocks()
    {
        var writer = new IndentedTextWriter();
        IndentedTextWriter.Block block = writer.WriteBlock();
        writer.WriteLine("first");
        block.Dispose();
        block.Dispose();
        string expected = """
            {
                first
            }

            """.ReplaceLineEndings("\r\n");
        Assert.Equal(expected, writer.ToString());
        Assert.Equal(0, writer.Indent);

        writer.Write('\r');
        writer.Clear();
        Assert.Equal(0, writer.Length);
        writer.Write("\nsecond");
        string expectedAfterClear = """

            second
            """.ReplaceLineEndings("\r\n");
        Assert.Equal(expectedAfterClear, writer.ToString());
    }

    [Fact]
    public void WriterFormatsInterpolatedValuesInvariantly()
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        culture.NumberFormat.NegativeSign = "~";
        culture.NumberFormat.NumberDecimalSeparator = ",";
        try
        {
            CultureInfo.CurrentCulture = culture;
            var writer = new IndentedTextWriter();
            writer.Write($"({-42}, {12.5}, {255:X})");
            writer.WriteLine($", {12.5:F2}");
            string expected = """
                (-42, 12.5, FF), 12.50

                """.ReplaceLineEndings("\r\n");
            Assert.Equal(expected, writer.ToString());
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("ordinary")]
    [InlineData("quotes\"slash\\new\nline\r\t")]
    [InlineData("\0")]
    [InlineData("\u2028\u2029")]
    [InlineData("\ud800")]
    [InlineData("\udc00")]
    public void StringLiteralsPreserveTheirValues(string value)
    {
        string source = CodeWriterHelpers.StringLiteral(value);
        LiteralExpressionSyntax literal = Assert.IsType<LiteralExpressionSyntax>(SyntaxFactory.ParseExpression(source));
        Assert.False(literal.ContainsDiagnostics);
        Assert.Equal(value, literal.Token.ValueText);
    }

    [Theory]
    [InlineData("event", "@event")]
    [InlineData("record", "@record")]
    [InlineData("@class", "@class")]
    [InlineData("ordinary", "ordinary")]
    public void IdentifiersAreEscaped(string identifier, string expected)
    {
        Assert.Equal(expected, CodeWriterHelpers.EscapeIdentifier(identifier));
    }

    [Theory]
    [InlineData("value")]
    [InlineData("record")]
    [InlineData("scoped")]
    [InlineData("field")]
    [InlineData("args")]
    public void ContextualKeywordParametersCompileInForwardersAndPinnedWrappers(string identifier)
    {
        string source = $$"""
            using System.Runtime.InteropServices;

            partial class Native
            {
                [LibraryImport("test")]
                private static partial int Forward(int {{identifier}});

                [LibraryImport("test", StringMarshalling = StringMarshalling.Utf16)]
                private static partial void String(string {{identifier}});

                [LibraryImport("test")]
                private static partial void Character([MarshalAs(UnmanagedType.U2)] ref char {{identifier}});
            }
            """;

        Compilation compilation = TestUtils.CreateCompilation(source);
        GeneratorDriver driver = TestUtils.CreateDriver(compilation, null, [new Microsoft.Interop.LibraryImportGenerator()]);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation outputCompilation, out ImmutableArray<Diagnostic> diagnostics);

        Assert.Single(driver.GetRunResult().GeneratedTrees);
        Assert.DoesNotContain(diagnostics, static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(outputCompilation.GetDiagnostics(), static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void SignaturesSeparateDeclarationsFromFunctionPointerTypes()
    {
        var signature = new GeneratedMethodSignature(
            [
                new GeneratedParameter("int", "@event", "this scoped ref", "A(1)"),
                new GeneratedParameter("void*", "__this")
            ],
            "int",
            "A(2)");

        Assert.Equal("([A(1)] this scoped ref int @event, void* __this)", signature.ParameterList);
        Assert.Equal("delegate* unmanaged[Cdecl]<ref int, void*, int>", signature.GetFunctionPointerType(["Cdecl"]));
        Assert.Equal("delegate* unmanaged<ref int, void*, int>", signature.GetFunctionPointerType([]));
        Assert.Equal("A(2)", signature.ReturnTypeAttributes);
        var equivalent = new GeneratedMethodSignature(
            [
                new GeneratedParameter("int", "@event", "this scoped ref", "A(1)"),
                new GeneratedParameter("void*", "__this")
            ],
            "int",
            "A(2)");
        Assert.Equal(signature, equivalent);
        Assert.Equal(signature.GetHashCode(), equivalent.GetHashCode());
    }

    [Theory]
    [InlineData("class")]
    [InlineData("struct")]
    [InlineData("interface")]
    [InlineData("record")]
    [InlineData("record struct")]
    public void ContainingDeclarationsCanBeWrittenFromValues(string keyword)
    {
        var declaration = new DeclarationHeader(["partial"], keyword, "@event", "@event<T>");
        var context = new ContainingSyntaxContext([declaration], "Outer.@namespace");
        string expected = $$"""
            namespace Outer.@namespace
            {
                partial {{keyword}} @event<T>
                {
                    private static int M() => 42;
                }
            }

            """.ReplaceLineEndings("\r\n");

        Assert.Equal(expected, context.WrapMemberInContainingSyntax("private static int M() => 42;"));
    }

    [Theory]
    [InlineData("class", "class")]
    [InlineData("struct", "struct")]
    [InlineData("interface", "interface")]
    [InlineData("record", "record")]
    [InlineData("record class", "record")]
    [InlineData("record struct", "record struct")]
    public void DeclarationExtractionPreservesInputDetails(string keyword, string expectedKeyword)
    {
        string source = $$"""
            namespace Outer
            {
                namespace @namespace /* comment */ . Inner
                {
                    public partial {{keyword}} @event<T>
                    {
                        partial void M();
                    }
                }
            }
            """;
        CompilationUnitSyntax input = SyntaxFactory.ParseCompilationUnit(source);
        Assert.False(input.ContainsDiagnostics);
        MethodDeclarationSyntax method = input.DescendantNodes().OfType<MethodDeclarationSyntax>().Single();
        ContainingSyntaxContext context = method.GetContainingSyntaxContext();
        DeclarationHeader declaration = Assert.Single(context.ContainingSyntax);

        Assert.Equal("Outer.@namespace.Inner", context.ContainingNamespace);
        Assert.Equal(["public", "partial"], declaration.Modifiers);
        Assert.Equal("@event", declaration.Identifier);
        Assert.Equal("@event<T>", declaration.Name);
        Assert.Equal(expectedKeyword, declaration.Keyword);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ContainingDeclarationsPreserveNestingAndModifierOrder(bool addUnsafe)
    {
        var context = new ContainingSyntaxContext(
            [
                new DeclarationHeader(["readonly", "ref", "partial"], "struct", "Nested", "Nested<U>"),
                new DeclarationHeader(["public", "partial"], "class", "Outer", "Outer<T>")
            ],
            ContainingNamespace: null);
        string expected = $$"""
            public {{(addUnsafe ? "unsafe " : "")}}partial class Outer<T>
            {
                readonly {{(addUnsafe ? "unsafe " : "")}}ref partial struct Nested<U>
                {
                    private static int M() => 42;
                }
            }

            """.ReplaceLineEndings("\r\n");
        const string Member = "private static int M() => 42;";

        string actual = addUnsafe
            ? context.WrapMembersInContainingSyntaxWithUnsafeModifier(Member)
            : context.WrapMemberInContainingSyntax(Member);

        Assert.Equal(expected, actual);
        Assert.False(SyntaxFactory.ParseCompilationUnit(actual).ContainsDiagnostics);
    }

    [Fact]
    public void DeclarationHeadersUseValueEquality()
    {
        var first = new DeclarationHeader(["public", "partial"], "class", "@class", "@class<T>");
        var second = new DeclarationHeader(["public", "partial"], "class", "@class", "@class<T>");

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.Equal("public partial class @class<T>", first.ToString());
        Assert.NotEqual(first, second with { Name = "@class<U>" });
    }

    [Fact]
    public void ContainingDeclarationsPreserveInputLiteralContents()
    {
        const string Source = """"
            partial class C<[A("""
                first
                second
                """)] T>
            {
                partial void M();
            }
            """";

        CompilationUnitSyntax input = SyntaxFactory.ParseCompilationUnit(Source);
        MethodDeclarationSyntax method = input.DescendantNodes().OfType<MethodDeclarationSyntax>().Single();
        ContainingSyntaxContext context = method.GetContainingSyntaxContext();
        CompilationUnitSyntax output = SyntaxFactory.ParseCompilationUnit(context.WrapMemberInContainingSyntax("partial void M() { }\r\n"));

        Assert.False(output.ContainsDiagnostics);
        Assert.Single(output.DescendantNodes().OfType<TypeParameterSyntax>().Single().AttributeLists);
        Assert.Equal(
            input.DescendantNodes().OfType<LiteralExpressionSyntax>().Single().Token.ValueText,
            output.DescendantNodes().OfType<LiteralExpressionSyntax>().Single().Token.ValueText);
    }

    [Theory]
    [InlineData("byte", false)]
    [InlineData("nint", false)]
    [InlineData("global::System.Guid", false)]
    [InlineData("byte", true)]
    [InlineData("nint", true)]
    [InlineData("global::System.Guid", true)]
    public void CollectionSpecializationReplacesSymbolsRatherThanMatchingNames(string elementType, bool nestedMarshaller)
    {
        string marshallerName = nestedMarshaller ? "Marshaller<,>.Nested" : "Marshaller<,>";
        string source = $$"""
            using System;
            using System.Runtime.InteropServices.Marshalling;

            namespace TUnmanagedElement
            {
                [ContiguousCollectionMarshaller]
                [CustomMarshaller(typeof(CustomMarshallerAttribute.GenericPlaceholder[]), MarshalMode.ManagedToUnmanagedIn, typeof({{marshallerName}}))]
                public static unsafe class Marshaller<T, TUnmanagedElement> where TUnmanagedElement : unmanaged
                {
                    {{(nestedMarshaller ? "public static class Nested {" : "")}}
                    public static int BufferSize => 16;
                    public static TUnmanagedElement* AllocateContainerForUnmanagedElements(T[] managed, Span<TUnmanagedElement> buffer, out int numElements)
                    {
                        numElements = 0;
                        return null;
                    }
                    public static ReadOnlySpan<T> GetManagedValuesSource(T[] managed) => default;
                    public static Span<TUnmanagedElement> GetUnmanagedValuesDestination(TUnmanagedElement* unmanaged, int numElements) => default;
                    {{(nestedMarshaller ? "}" : "")}}
                }
            }

            partial class Native
            {
                [global::System.Runtime.InteropServices.LibraryImport("test")]
                static partial void M([MarshalUsing(typeof(global::TUnmanagedElement.Marshaller<,>))] int[] values);
            }
            """;

        Compilation compilation = TestUtils.CreateCompilation(source);
        Assert.DoesNotContain(compilation.GetDiagnostics(), static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        INamedTypeSymbol definition = compilation.GetTypeByMetadataName("TUnmanagedElement.Marshaller`2")!;
        ITypeSymbol managedElement = compilation.GetSpecialType(SpecialType.System_Int32);
        INamedTypeSymbol entryPoint = definition.Construct(managedElement, definition.TypeParameters[1]);
        Assert.True(ManualTypeMarshallingHelper.TryGetLinearCollectionMarshallersFromEntryType(
            entryPoint,
            compilation.CreateArrayTypeSymbol(managedElement),
            compilation,
            static _ => NoMarshallingInfo.Instance,
            out CustomTypeMarshallers? marshallers));

        CustomTypeMarshallerData data = Assert.IsType<CustomTypeMarshallers>(marshallers)
            .GetModeOrDefault(MarshalMode.ManagedToUnmanagedIn)
            .WithUnmanagedElementType(elementType);
        Assert.Equal($"global::TUnmanagedElement.Marshaller<int, {elementType}>{(nestedMarshaller ? ".Nested" : "")}", data.MarshallerType.FullTypeName);
        Assert.Equal(elementType + "*", data.NativeType.FullTypeName);
        Assert.Equal(elementType, data.BufferElementType!.FullTypeName);

        GeneratorDriver driver = TestUtils.CreateDriver(compilation, null, [new Microsoft.Interop.LibraryImportGenerator()]);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation outputCompilation, out ImmutableArray<Diagnostic> diagnostics);
        Assert.Single(driver.GetRunResult().GeneratedTrees);
        Assert.DoesNotContain(diagnostics, static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(outputCompilation.GetDiagnostics(), static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }
}
