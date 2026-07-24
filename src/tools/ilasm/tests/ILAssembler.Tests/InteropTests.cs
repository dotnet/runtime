// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using Internal.IL;
using Xunit;
using DocumentCompilerTestHelpers = ILAssembler.Tests.DocumentCompilerTestHelpers;

namespace ILAssembler.Tests
{
    public class InteropTests
    {
        [Fact]
        public void Diagnostic_InvalidPInvokeSignature()
        {
            // P/Invoke with no module name triggers InvalidPInvokeSignature
            string source = """
                .assembly test { }
                .class public auto ansi Test
                {
                    .method public static pinvokeimpl() void TestMethod() cil managed
                    {
                    }
                }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            var error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.InvalidPInvokeSignature, error.Id);
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        }

        [Fact]
        public void Diagnostic_DeprecatedNativeType_Variant()
        {
            // Using deprecated VARIANT native type triggers warning
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void TestMethod(object marshal(variant) arg) cil managed
                    {
                        ret
                    }
                }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            var warning = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.DeprecatedNativeType, warning.Id);
            Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
        }

        [Fact]
        public void Diagnostic_DeprecatedCustomMarshaller()
        {
            // Using 4-string custom marshaller syntax triggers warning
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void TestMethod(object marshal(custom("guid", "nativeType", "marshallerType", "cookie")) arg) cil managed
                    {
                        ret
                    }
                }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            var warning = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.DeprecatedCustomMarshaller, warning.Id);
            Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
        }

        [Fact]
        public void PinvokeMethod_SetsPinvokeImplFlag()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .module test.dll
                .class public auto ansi beforefieldinit MyClass extends [mscorlib]System.Object
                {
                    .method public static pinvokeimpl("kernel32.dll" winapi)
                        int32 GetCurrentProcessId() cil managed preservesig
                    {
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var method = reader.MethodDefinitions
                .Select(h => reader.GetMethodDefinition(h))
                .First(m => reader.GetString(m.Name) == "GetCurrentProcessId");

            Assert.True(method.Attributes.HasFlag(MethodAttributes.PinvokeImpl));
            var import = method.GetImport();
            Assert.False(import.Module.IsNil);
        }
    }
}
