// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using LibraryImportGenerator.UnitTests;
using System.Linq;

namespace JSImportGenerator.Unit.Tests
{
    public class Fails
    {
        public static IEnumerable<object?[]> CodeSnippetsToFail()
        {
            yield return new object?[] { CodeSnippets.DefaultReturnMarshaler<long>(), new string[] {
                "TODO Please use JSMarshalAsAttribute to specify marshaling of long. The generated source will not handle marshalling of the return value of method 'Import1'.",
                "TODO Please use JSMarshalAsAttribute to specify marshaling of long. The generated source will not handle marshalling of the return value of method 'Export1'.",
            },null };
            yield return new object?[] { CodeSnippets.DefaultReturnMarshaler<object>(), null, null };
            yield return new object?[] { CodeSnippets.DefaultReturnMarshaler("System.Func<string>"), null, null };
            yield return new object?[] { CodeSnippets.DefaultReturnMarshaler("System.Action"), new string[] {
                "TODO Please use JSMarshalAsAttribute to specify marshaling of global::System.Action. The generated source will not handle marshalling of the return value of method 'Import1'.",
                "TODO Please use JSMarshalAsAttribute to specify marshaling of global::System.Action. The generated source will not handle marshalling of the return value of method 'Export1'.",
            },null };
            yield return new object?[] { CodeSnippets.DefaultReturnMarshaler("System.Span<byte>"), null, null };
            yield return new object?[] { CodeSnippets.DefaultReturnMarshaler("System.Span<long>"), null, null };
            yield return new object?[] { CodeSnippets.DefaultReturnMarshaler("System.ArraySegment<byte>"), null, null };
            yield return new object?[] { CodeSnippets.AllMissing, new string[] {
                "TODO Please use JSMarshalAsAttribute to specify marshaling of object. The generated source will not handle marshalling of parameter 'a1'.",
                "TODO Please use JSMarshalAsAttribute to specify marshaling of long. The generated source will not handle marshalling of parameter 'a2'.",
                "TODO Please use JSMarshalAsAttribute to specify marshaling of long. The generated source will not handle marshalling of parameter 'a3'.",
                "TODO Please use JSMarshalAsAttribute to specify marshaling of global::System.Action. The generated source will not handle marshalling of parameter 'a4'.",
                "TODO Please use JSMarshalAsAttribute to specify marshaling of global::System.Func<int>. The generated source will not handle marshalling of parameter 'a5'.",
                "TODO Please use JSMarshalAsAttribute to specify marshaling of global::System.Span<byte>. The generated source will not handle marshalling of parameter 'a6'.",
                "TODO Please use JSMarshalAsAttribute to specify marshaling of global::System.ArraySegment<byte>. The generated source will not handle marshalling of parameter 'a7'.",
                "TODO Please use JSMarshalAsAttribute to specify marshaling of global::System.Threading.Tasks.Task<object>. The generated source will not handle marshalling of parameter 'a8'.",
                "TODO Please use JSMarshalAsAttribute to specify marshaling of object[]. The generated source will not handle marshalling of parameter 'a9'.",
                "TODO Please use JSMarshalAsAttribute to specify marshaling of global::System.DateTime. The generated source will not handle marshalling of parameter 'a10'.",
                "TODO Please use JSMarshalAsAttribute to specify marshaling of global::System.DateTimeOffset. The generated source will not handle marshalling of parameter 'a11'.",
                "TODO Please use JSMarshalAsAttribute to specify marshaling of global::System.Threading.Tasks.Task<global::System.DateTime>. The generated source will not handle marshalling of parameter 'a12'.",
                "TODO Please use JSMarshalAsAttribute to specify marshaling of global::System.Threading.Tasks.Task<global::System.DateTimeOffset>. The generated source will not handle marshalling of parameter 'a13'.",
                "TODO Please use JSMarshalAsAttribute to specify marshaling of global::System.Threading.Tasks.Task<long>. The generated source will not handle marshalling of parameter 'a14'.",
                "TODO Please use JSMarshalAsAttribute to specify marshaling of global::System.Threading.Tasks.Task<long>. The generated source will not handle marshalling of parameter 'a15'.",
            },null };
            yield return new object?[] { CodeSnippets.InOutRef, new string[] {
                "TODO Resources.InOutRefNotSupported The generated source will not handle marshalling of parameter 'a1'.",
                "TODO Resources.InOutRefNotSupported The generated source will not handle marshalling of parameter 'a2'.",
                "TODO Resources.InOutRefNotSupported The generated source will not handle marshalling of parameter 'a3'.",
            }, null };
        }

        [Theory]
        [MemberData(nameof(CodeSnippetsToFail))]
        public async Task ValidateFailSnippets(string source, string[]? generatorMessages, string[]? compilerMessages)
        {
            Compilation comp = await TestUtils.CreateCompilation(source);
            TestUtils.AssertPreSourceGeneratorCompilation(comp);

            var newComp = TestUtils.RunGenerators(comp, out var generatorDiags,
                new Microsoft.Interop.JavaScript.JSImportGenerator(),
                new Microsoft.Interop.JavaScript.JSExportGenerator());
            JSTestUtils.DumpCode(source, newComp, generatorDiags);

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
    }
}
