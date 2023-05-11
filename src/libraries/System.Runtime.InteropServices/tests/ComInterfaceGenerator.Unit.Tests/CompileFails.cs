// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.Interop.UnitTests;
using Xunit;

using System.Diagnostics;


using VerifyComInterfaceGenerator = Microsoft.Interop.UnitTests.Verifiers.CSharpSourceGeneratorVerifier<Microsoft.Interop.ComInterfaceGenerator>;
using Microsoft.Interop;

namespace ComInterfaceGenerator.Unit.Tests
{
    public class CompileFails
    {
        private static string ID(
            [CallerLineNumber] int lineNumber = 0,
            [CallerFilePath] string? filePath = null)
            => TestUtils.GetFileLineName(lineNumber, filePath);

        public static IEnumerable<object[]> ComInterfaceGeneratorSnippetsToCompile()
        {
            CodeSnippets codeSnippets = new(new GeneratedComInterfaceAttributeProvider());
            // Inheriting from multiple GeneratedComInterface-marked interfaces.
            yield return new object[] { ID(), codeSnippets.DerivedComInterfaceTypeMultipleComInterfaceBases, new[] {
                VerifyComInterfaceGenerator.Diagnostic(GeneratorDiagnostics.MultipleComInterfaceBaseTypes)
                    .WithLocation(0)
                    .WithArguments("IComInterface2")
            } };
        }

        [Theory]
        [MemberData(nameof(ComInterfaceGeneratorSnippetsToCompile))]
        public async Task ValidateComInterfaceGeneratorSnippets(string id, string source, DiagnosticResult[] expectedDiagnostics)
        {
            TestUtils.Use(id);
            await VerifyComInterfaceGenerator.VerifySourceGeneratorAsync(source, expectedDiagnostics);
        }

        private static IComInterfaceAttributeProvider GetAttributeProvider(GeneratorKind generator)
            => generator switch
            {
                GeneratorKind.VTableIndexStubGenerator => new VirtualMethodIndexAttributeProvider(),
                GeneratorKind.ComInterfaceGenerator => new GeneratedComInterfaceAttributeProvider(),
                _ => throw new UnreachableException(),
            };

        public static IEnumerable<object[]> InvalidUnmanagedToManagedCodeSnippetsToCompile(GeneratorKind generator)
        {
            CodeSnippets codeSnippets = new(GetAttributeProvider(generator));

            // SafeHandles
            yield return new object[] { ID(), codeSnippets.BasicParametersAndModifiers("Microsoft.Win32.SafeHandles.SafeFileHandle"), new[]
            {
                VerifyComInterfaceGenerator.Diagnostic(GeneratorDiagnostics.ReturnTypeNotSupportedWithDetails).WithLocation(0).WithArguments("The specified parameter needs to be marshalled from managed to unmanaged, but the marshaller type 'global::System.Runtime.InteropServices.Marshalling.SafeHandleMarshaller<global::Microsoft.Win32.SafeHandles.SafeFileHandle>' does not support it.", "Method"),
                VerifyComInterfaceGenerator.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails).WithLocation(1).WithArguments("The specified parameter needs to be marshalled from unmanaged to managed, but the marshaller type 'global::System.Runtime.InteropServices.Marshalling.SafeHandleMarshaller<global::Microsoft.Win32.SafeHandles.SafeFileHandle>' does not support it.", "value"),
                // /0/Test0.cs(13,151): error SYSLIB1051: The type 'Microsoft.Win32.SafeHandles.SafeFileHandle' is not supported by source-generated P/Invokes. The generated source will not handle marshalling of parameter 'inValue'.
                VerifyComInterfaceGenerator.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails).WithLocation(2).WithArguments("The specified parameter needs to be marshalled from unmanaged to managed, but the marshaller type 'global::System.Runtime.InteropServices.Marshalling.SafeHandleMarshaller<global::Microsoft.Win32.SafeHandles.SafeFileHandle>' does not support it.", "inValue"),
                // /0/Test0.cs(13,207): error SYSLIB1051: The type 'Microsoft.Win32.SafeHandles.SafeFileHandle' is not supported by source-generated P/Invokes. The generated source will not handle marshalling of parameter 'refValue'.
                VerifyComInterfaceGenerator.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails).WithLocation(3).WithArguments("The specified parameter needs to be marshalled from managed to unmanaged and unmanaged to managed, but the marshaller type 'global::System.Runtime.InteropServices.Marshalling.SafeHandleMarshaller<global::Microsoft.Win32.SafeHandles.SafeFileHandle>' does not support it.", "refValue"),
                // /0/Test0.cs(13,264): error SYSLIB1051: The type 'Microsoft.Win32.SafeHandles.SafeFileHandle' is not supported by source-generated P/Invokes. The generated source will not handle marshalling of parameter 'outValue'.
                VerifyComInterfaceGenerator.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails).WithLocation(4).WithArguments("The specified parameter needs to be marshalled from managed to unmanaged, but the marshaller type 'global::System.Runtime.InteropServices.Marshalling.SafeHandleMarshaller<global::Microsoft.Win32.SafeHandles.SafeFileHandle>' does not support it.", "outValue"),
            } };


            // Marshallers with only support for their expected places in the signatures in
            // ManagedToUnmanaged marshal modes.

            DiagnosticResult invalidManagedToUnmanagedParameterDiagnostic = VerifyComInterfaceGenerator.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                .WithLocation(0)
                .WithArguments("The specified parameter needs to be marshalled from managed to unmanaged, but the marshaller type 'global::Marshaller' does not support it.", "value");
            DiagnosticResult invalidUnmanagedToManagedParameterDiagnostic = VerifyComInterfaceGenerator.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                .WithLocation(0)
                .WithArguments("The specified parameter needs to be marshalled from unmanaged to managed, but the marshaller type 'global::Marshaller' does not support it.", "value");
            DiagnosticResult invalidReturnTypeDiagnostic = VerifyComInterfaceGenerator.Diagnostic(GeneratorDiagnostics.ReturnTypeNotSupportedWithDetails)
                .WithLocation(0)
                .WithArguments("The specified parameter needs to be marshalled from managed to unmanaged, but the marshaller type 'global::Marshaller' does not support it.", "Method");
            CustomStructMarshallingCodeSnippets customStructMarshallingCodeSnippets = new(new CodeSnippets.Bidirectional(GetAttributeProvider(generator)));
            yield return new object[] { ID(), customStructMarshallingCodeSnippets.Stateless.NativeToManagedOnlyOutParameter, new[] { invalidManagedToUnmanagedParameterDiagnostic } };
            yield return new object[] { ID(), customStructMarshallingCodeSnippets.Stateless.NativeToManagedOnlyReturnValue, new[] { invalidReturnTypeDiagnostic } };
            yield return new object[] { ID(), customStructMarshallingCodeSnippets.Stateless.ByValueInParameter, new[] { invalidUnmanagedToManagedParameterDiagnostic } };
            yield return new object[] { ID(), customStructMarshallingCodeSnippets.Stateful.NativeToManagedOnlyOutParameter, new[] { invalidManagedToUnmanagedParameterDiagnostic } };
            yield return new object[] { ID(), customStructMarshallingCodeSnippets.Stateful.NativeToManagedOnlyReturnValue, new[] { invalidReturnTypeDiagnostic } };
            yield return new object[] { ID(), customStructMarshallingCodeSnippets.Stateful.ByValueInParameter, new[] { invalidUnmanagedToManagedParameterDiagnostic } };
        }

        public static IEnumerable<object[]> InvalidManagedToUnmanagedCodeSnippetsToCompile(GeneratorKind generator)
        {
            // Marshallers with only support for their expected places in the signatures in
            // UnmanagedToManaged marshal modes.
            CustomStructMarshallingCodeSnippets customStructMarshallingCodeSnippets = new(new CodeSnippets.Bidirectional(GetAttributeProvider(generator)));

            yield return new[] { ID(), customStructMarshallingCodeSnippets.Stateless.NativeToManagedOnlyInParameter };
            yield return new[] { ID(), customStructMarshallingCodeSnippets.Stateless.ByValueOutParameter };
            yield return new[] { ID(), customStructMarshallingCodeSnippets.Stateful.NativeToManagedOnlyInParameter };
            yield return new[] { ID(), customStructMarshallingCodeSnippets.Stateful.ByValueOutParameter };
        }

        [Theory]
        [MemberData(nameof(InvalidUnmanagedToManagedCodeSnippetsToCompile), GeneratorKind.ComInterfaceGenerator)]
        public async Task ValidateInvalidUnmanagedToManagedCodeSnippets(string id, string source, DiagnosticResult[] expectedDiagnostics)
        {
            _ = id;
            VerifyComInterfaceGenerator.Test test = new(referenceAncillaryInterop: false)
            {
                TestCode = source,
                TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
                // Our fallback mechanism for invalid code for unmanaged->managed stubs sometimes generates invalid code.
                CompilerDiagnostics = CompilerDiagnostics.None,
            };
            test.ExpectedDiagnostics.AddRange(expectedDiagnostics);
            await test.RunAsync();
        }

        [Theory]
        [MemberData(nameof(InvalidManagedToUnmanagedCodeSnippetsToCompile), GeneratorKind.ComInterfaceGenerator)]
        public async Task ValidateInvalidManagedToUnmanagedCodeSnippets(string id, string source)
        {
            _ = id;

            DiagnosticResult expectedDiagnostic = VerifyComInterfaceGenerator.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                .WithLocation(0)
                .WithArguments("The specified parameter needs to be marshalled from managed to unmanaged, but the marshaller type 'global::Marshaller' does not support it.", "value");
            await VerifyComInterfaceGenerator.VerifySourceGeneratorAsync(source, expectedDiagnostic);
        }
    }
}
