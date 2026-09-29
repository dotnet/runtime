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
    [InlineData("\0\a\b\f\n\r\t\v'\"\\")]
    [InlineData("\u00011234\u001fabcdef")]
    [InlineData("\u007f\u0080\u0085\u009f")]
    [InlineData("\u00a0\u1680\u2000\u2028\u2029\u202f\u205f\u3000")]
    [InlineData("\u00ad\u061c\u200b\u200c\u200d\u200e\u200f\u202a\u202b\u202c\u202d\u202e\u2060\u2066\u2067\u2068\u2069\ufeff")]
    [InlineData("\u0378\ue000\uf8ff\ufffe\uffff")]
    [InlineData("\U00010000\U0001f600\U000e0001\U000f0000\U000fffff\U0010ffff")]
    public void StringLiteralsPreserveTheirValues(string value)
    {
        string source = CodeWriterHelpers.StringLiteral(value);
        Assert.Equal(Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(value, quote: true), source);
        LiteralExpressionSyntax literal = Assert.IsType<LiteralExpressionSyntax>(SyntaxFactory.ParseExpression(source));
        Assert.False(literal.ContainsDiagnostics);
        Assert.Equal(value, literal.Token.ValueText);
    }

    [Theory]
    [InlineData(0x0000, 0xffff)]
    [InlineData(0x10000, 0x10ffff)]
    public void StringLiteralsMatchRoslynForAllUtf16CodeUnitsAndSurrogatePairs(int first, int last)
    {
        for (int codePoint = first; codePoint <= last; codePoint++)
        {
            string value = codePoint <= char.MaxValue
                ? ((char)codePoint).ToString()
                : char.ConvertFromUtf32(codePoint);

            Assert.Equal(
                Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(value, quote: true),
                CodeWriterHelpers.StringLiteral(value));
        }
    }

    [Theory]
    [InlineData(0xd800)]
    [InlineData(0xdc00)]
    [InlineData(0xdbff)]
    [InlineData(0xdfff)]
    [InlineData(0xd800, 0xdc00)]
    [InlineData(0xdbff, 0xdfff)]
    [InlineData(0xdc00, 0xd800)]
    [InlineData(0xd800, 0xd800, 0xdc00, 0xdc00)]
    [InlineData(0xdbff, 0xdfff, 0xdbff)]
    [InlineData(0xd800, (int)'"', 0xdc00, (int)'\\', 0xdbff, (int)'\n', 0xdfff)]
    public void StringLiteralsPreserveMixedAndUnpairedSurrogates(params int[] codeUnits)
    {
        // Construct at execution time: attribute strings cannot preserve unpaired surrogates.
        string value = new string(codeUnits.Select(static codeUnit => (char)codeUnit).ToArray());
        StringLiteralsPreserveTheirValues(value);
        StringLiteralsPreserveTheirValues("prefix\"\\0123456789abcdef" + value + "fedcba9876543210\\\"suffix");
    }

    [Fact]
    public void StringLiteralsRejectNull()
    {
        Assert.Throws<ArgumentNullException>("value", static () => CodeWriterHelpers.StringLiteral(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("event")]
    [InlineData("record")]
    [InlineData("extension")]
    [InlineData("var")]
    [InlineData("_")]
    [InlineData("@class")]
    [InlineData("Class")]
    [InlineData("CLASS")]
    [InlineData("className")]
    [InlineData("myclass")]
    [InlineData("cl\u200bass")]
    [InlineData("\\u0063lass")]
    [InlineData("dynamic")]
    [InlineData("nint")]
    [InlineData("nuint")]
    [InlineData("notnull")]
    [InlineData("value")]
    [InlineData("args")]
    [InlineData("closed")]
    [InlineData("safe")]
    [InlineData("union")]
    [InlineData("ordinary")]
    public void IdentifiersAreEscaped(string identifier)
    {
        bool isKeyword = SyntaxFacts.GetKeywordKind(identifier) != SyntaxKind.None
            || SyntaxFacts.GetContextualKeywordKind(identifier) != SyntaxKind.None;
        Assert.Equal(isKeyword ? "@" + identifier : identifier, CodeWriterHelpers.EscapeIdentifier(identifier));
    }

    [Theory]
    [InlineData("")]
    [InlineData("ordinary")]
    [InlineData("event")]
    [InlineData("record")]
    [InlineData("closed")]
    [InlineData("safe")]
    [InlineData("union")]
    [InlineData("@event")]
    public void IdentifierEscapingUsesTheSuppliedKeywordClassification(string identifier)
    {
        Assert.Equal(identifier, CodeWriterHelpers.EscapeIdentifier(identifier, isKeyword: false));
        Assert.Equal("@" + identifier, CodeWriterHelpers.EscapeIdentifier(identifier, isKeyword: true));
    }

    [Fact]
    public void IdentifiersMatchRoslynKeywordAndContextualKeywordSets()
    {
        // Enumerate all token spellings: the keyword-kind enumerators omit var and underscore.
        foreach (SyntaxKind kind in Enum.GetValues<SyntaxKind>())
        {
            string text = SyntaxFacts.GetText(kind);
            if (string.IsNullOrEmpty(text))
            {
                continue;
            }

            foreach (string identifier in new[] { text, "@" + text, text.ToUpperInvariant(), text + "Suffix", "Prefix" + text })
            {
                IdentifiersAreEscaped(identifier);
            }
        }
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
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void ContainingDeclarationsPreserveNestingAndModifierOrder(bool addUnsafe, bool useUpdatedMemorySafetyRules)
    {
        var context = new ContainingSyntaxContext(
            [
                new DeclarationHeader(["readonly", "ref", "partial"], "struct", "Nested", "Nested<U>"),
                new DeclarationHeader(["public", "partial"], "class", "Outer", "Outer<T>")
            ],
            ContainingNamespace: null);
        string expected = $$"""
            public {{(addUnsafe && !useUpdatedMemorySafetyRules ? "unsafe " : "")}}partial class Outer<T>
            {
                readonly {{(addUnsafe && !useUpdatedMemorySafetyRules ? "unsafe " : "")}}ref partial struct Nested<U>
                {
                    private static int M() => 42;
                }
            }

            """.ReplaceLineEndings("\r\n");
        const string Member = "private static int M() => 42;";

        string actual = addUnsafe
            ? context.WrapMembersInContainingSyntaxWithUnsafeModifier(useUpdatedMemorySafetyRules, Member)
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

    [Theory]
    [InlineData("Marker", false)]
    [InlineData("Marker", true)]
    [InlineData("Alias", false)]
    [InlineData("Alias", true)]
    [InlineData("global::Attributes.Marker", false)]
    [InlineData("global::Attributes.Marker", true)]
    public void ContainingDeclarationsDoNotRepeatGenericParameterAttributes(string attributeName, bool allowMultiple)
    {
        string source = $$""""
            using Attributes;
            using Alias = Attributes.MarkerAttribute;

            namespace Attributes
            {
                [System.AttributeUsage(System.AttributeTargets.GenericParameter, AllowMultiple = {{(allowMultiple ? "true" : "false")}})]
                public sealed class MarkerAttribute : System.Attribute
                {
                    public MarkerAttribute(string value) { }
                }
            }

            namespace Example
            {
                partial class Outer<[{{attributeName}}("""
                    first
                    second
                    """)] T>
                {
                    partial class Inner<[{{attributeName}}("inner")] U>
                    {
                        partial void M();
                    }
                }
            }
            """".ReplaceLineEndings("\n");

        Compilation compilation = TestUtils.CreateCompilation(source);
        SyntaxTree tree = Assert.Single(compilation.SyntaxTrees);
        CompilationUnitSyntax input = tree.GetCompilationUnitRoot();
        MethodDeclarationSyntax method = input.DescendantNodes().OfType<MethodDeclarationSyntax>().Single();
        ContainingSyntaxContext context = method.GetContainingSyntaxContext();
        SyntaxTree generatedTree = CSharpSyntaxTree.ParseText(
            context.WrapMemberInContainingSyntax("partial void M() { }\r\n"),
            (CSharpParseOptions)tree.Options);
        Compilation output = compilation.AddSyntaxTrees(generatedTree);

        Assert.DoesNotContain(output.GetDiagnostics(), static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        TypeParameterSyntax[] generatedParameters = generatedTree.GetRoot().DescendantNodes().OfType<TypeParameterSyntax>().ToArray();
        Assert.Equal(2, generatedParameters.Length);
        Assert.All(generatedParameters, static parameter => Assert.Empty(parameter.AttributeLists));

        INamedTypeSymbol outer = output.GetTypeByMetadataName("Example.Outer`1")!;
        INamedTypeSymbol inner = Assert.Single(outer.GetTypeMembers("Inner"));
        string expectedValue = """
            first
            second
            """.ReplaceLineEndings("\n");
        Assert.Equal(expectedValue, Assert.Single(outer.TypeParameters[0].GetAttributes()).ConstructorArguments[0].Value);
        Assert.Equal("inner", Assert.Single(inner.TypeParameters[0].GetAttributes()).ConstructorArguments[0].Value);
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
        ITypeSymbol managedElement = compilation.GetSpecialType(Microsoft.CodeAnalysis.SpecialType.System_Int32);
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
