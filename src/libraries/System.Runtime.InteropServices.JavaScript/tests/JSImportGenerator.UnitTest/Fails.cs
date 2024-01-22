// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Interop.UnitTests;
using System.Linq;

namespace JSImportGenerator.Unit.Tests
{
    public class Fails
    {
        public static IEnumerable<object?[]> CodeSnippetsToFail()
        {
            yield return new object?[] { CodeSnippets.DefaultReturnMarshaler<long>(), new string[] {
                "Please annotate the argument with 'JSMarshalAsAttribute' to specify marshaling of long. The generated source will not handle marshalling of the return value of method 'Import1'. For more information see https://aka.ms/dotnet-wasm-jsinterop",
                "Please annotate the argument with 'JSMarshalAsAttribute' to specify marshaling of long. The generated source will not handle marshalling of the return value of method 'Export1'. For more information see https://aka.ms/dotnet-wasm-jsinterop",
            },null };
            yield return new object?[] { CodeSnippets.DefaultReturnMarshaler<object>(), null, null };
            yield return new object?[] { CodeSnippets.DefaultReturnMarshaler("System.Func<string>"), null, null };
            yield return new object?[] { CodeSnippets.DefaultReturnMarshaler("System.Action"), new string[] {
                "Please annotate the argument with 'JSMarshalAsAttribute' to specify marshaling of global::System.Action. The generated source will not handle marshalling of the return value of method 'Import1'. For more information see https://aka.ms/dotnet-wasm-jsinterop",
                "Please annotate the argument with 'JSMarshalAsAttribute' to specify marshaling of global::System.Action. The generated source will not handle marshalling of the return value of method 'Export1'. For more information see https://aka.ms/dotnet-wasm-jsinterop",
            },null };
            yield return new object?[] { CodeSnippets.DefaultReturnMarshaler("System.Span<byte>"), null, null };
            yield return new object?[] { CodeSnippets.DefaultReturnMarshaler("System.Span<long>"), null, null };
            yield return new object?[] { CodeSnippets.DefaultReturnMarshaler("System.ArraySegment<byte>"), null, null };
            yield return new object?[] { CodeSnippets.AllMissing, new string[] {
                "Please annotate the argument with 'JSMarshalAsAttribute' to specify marshaling of object. The generated source will not handle marshalling of parameter 'a1'. For more information see https://aka.ms/dotnet-wasm-jsinterop",
                "Please annotate the argument with 'JSMarshalAsAttribute' to specify marshaling of long. The generated source will not handle marshalling of parameter 'a2'. For more information see https://aka.ms/dotnet-wasm-jsinterop",
                "Please annotate the argument with 'JSMarshalAsAttribute' to specify marshaling of long. The generated source will not handle marshalling of parameter 'a3'. For more information see https://aka.ms/dotnet-wasm-jsinterop",
                "Please annotate the argument with 'JSMarshalAsAttribute' to specify marshaling of global::System.Action. The generated source will not handle marshalling of parameter 'a4'. For more information see https://aka.ms/dotnet-wasm-jsinterop",
                "Please annotate the argument with 'JSMarshalAsAttribute' to specify marshaling of global::System.Func<int>. The generated source will not handle marshalling of parameter 'a5'. For more information see https://aka.ms/dotnet-wasm-jsinterop",
                "Please annotate the argument with 'JSMarshalAsAttribute' to specify marshaling of global::System.Span<byte>. The generated source will not handle marshalling of parameter 'a6'. For more information see https://aka.ms/dotnet-wasm-jsinterop",
                "Please annotate the argument with 'JSMarshalAsAttribute' to specify marshaling of global::System.ArraySegment<byte>. The generated source will not handle marshalling of parameter 'a7'. For more information see https://aka.ms/dotnet-wasm-jsinterop",
                "Please annotate the argument with 'JSMarshalAsAttribute' to specify marshaling of global::System.Threading.Tasks.Task<object>. The generated source will not handle marshalling of parameter 'a8'. For more information see https://aka.ms/dotnet-wasm-jsinterop",
                "Please annotate the argument with 'JSMarshalAsAttribute' to specify marshaling of object[]. The generated source will not handle marshalling of parameter 'a9'. For more information see https://aka.ms/dotnet-wasm-jsinterop",
                "Please annotate the argument with 'JSMarshalAsAttribute' to specify marshaling of global::System.DateTime. The generated source will not handle marshalling of parameter 'a10'. For more information see https://aka.ms/dotnet-wasm-jsinterop",
                "Please annotate the argument with 'JSMarshalAsAttribute' to specify marshaling of global::System.DateTimeOffset. The generated source will not handle marshalling of parameter 'a11'. For more information see https://aka.ms/dotnet-wasm-jsinterop",
                "Please annotate the argument with 'JSMarshalAsAttribute' to specify marshaling of global::System.Threading.Tasks.Task<global::System.DateTime>. The generated source will not handle marshalling of parameter 'a12'. For more information see https://aka.ms/dotnet-wasm-jsinterop",
                "Please annotate the argument with 'JSMarshalAsAttribute' to specify marshaling of global::System.Threading.Tasks.Task<global::System.DateTimeOffset>. The generated source will not handle marshalling of parameter 'a13'. For more information see https://aka.ms/dotnet-wasm-jsinterop",
                "Please annotate the argument with 'JSMarshalAsAttribute' to specify marshaling of global::System.Threading.Tasks.Task<long>. The generated source will not handle marshalling of parameter 'a14'. For more information see https://aka.ms/dotnet-wasm-jsinterop",
                "Please annotate the argument with 'JSMarshalAsAttribute' to specify marshaling of global::System.Threading.Tasks.Task<long>. The generated source will not handle marshalling of parameter 'a15'. For more information see https://aka.ms/dotnet-wasm-jsinterop",
            },null };
            yield return new object?[] { CodeSnippets.InOutRef, new string[] {
                "Parameters with 'in', 'out' and 'ref' modifiers are not supported by source-generated JavaScript interop. The generated source will not handle marshalling of parameter 'a1'. For more information see https://aka.ms/dotnet-wasm-jsinterop",
                "Parameters with 'in', 'out' and 'ref' modifiers are not supported by source-generated JavaScript interop. The generated source will not handle marshalling of parameter 'a2'. For more information see https://aka.ms/dotnet-wasm-jsinterop",
                "Parameters with 'in', 'out' and 'ref' modifiers are not supported by source-generated JavaScript interop. The generated source will not handle marshalling of parameter 'a3'. For more information see https://aka.ms/dotnet-wasm-jsinterop",
            }, null };
        }

        [Theory]
        [MemberData(nameof(CodeSnippetsToFail))]
        public async Task ValidateFailSnippets(string source, string[]? generatorMessages, string[]? compilerMessages)
        {
            Compilation comp = await TestUtils.CreateCompilation(source, allowUnsafe: true);
            TestUtils.AssertPreSourceGeneratorCompilation(comp);

            var newComp = TestUtils.RunGenerators(comp, out var generatorDiags,
                new Microsoft.Interop.JavaScript.JSImportGenerator(),
                new Microsoft.Interop.JavaScript.JSExportGenerator());
            // uncomment for debugging JSTestUtils.DumpCode(source, newComp, generatorDiags);

            if (generatorMessages != null)
            {
                JSTestUtils.AssertMessages(generatorDiags, generatorMessages);
            }
            var compilationDiags = newComp.GetDiagnostics();
            if (compilerMessages != null)
            {
                JSTestUtils.AssertMessages(compilationDiags, compilerMessages);
            }
        }

        [Fact]
        public async Task ValidateRequireAllowUnsafeBlocksDiagnostic()
        {
            string source = CodeSnippets.TrivialClassDeclarations;
            Compilation comp = await TestUtils.CreateCompilation(new[] { source }, allowUnsafe: false);
            TestUtils.AssertPreSourceGeneratorCompilation(comp);

            var newComp = TestUtils.RunGenerators(comp, out var generatorDiags,
                new Microsoft.Interop.JavaScript.JSImportGenerator(),
                new Microsoft.Interop.JavaScript.JSExportGenerator());

            // The errors should indicate the AllowUnsafeBlocks is required.
            Assert.True(generatorDiags.Single(d => d.Id == "SYSLIB1074") != null);
            Assert.True(generatorDiags.Single(d => d.Id == "SYSLIB1075") != null);
        }
    }
}
