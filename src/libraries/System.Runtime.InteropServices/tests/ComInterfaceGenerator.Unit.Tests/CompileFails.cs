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
using Microsoft.Interop;
using Microsoft.Interop.UnitTests;
using Xunit;
using System.Diagnostics;

using VerifyComInterfaceGenerator = Microsoft.Interop.UnitTests.Verifiers.CSharpSourceGeneratorVerifier<Microsoft.Interop.ComInterfaceGenerator>;
using StringMarshalling = System.Runtime.InteropServices.StringMarshalling;
using System.Runtime.InteropServices.Marshalling;
using Microsoft.CodeAnalysis.CSharp;

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

        public static IEnumerable<object[]> StringMarshallingCodeSnippets(GeneratorKind generator)
        {
            const string CustomStringMarshallingWithNoCustomTypeMessage = @"'StringMarshallingCustomType' must be specified when 'StringMarshalling' is set to 'StringMarshalling.Custom'.";
            const string CustomTypeSpecifiedWithNoStringMarshallingCustom = @"'StringMarshalling' should be set to 'StringMarshalling.Custom' when 'StringMarshallingCustomType' is specified.";
            const string StringMarshallingMustMatchBase = "The configuration of 'StringMarshalling' and 'StringMarshallingCustomType' must match the base COM interface.";

            CodeSnippets codeSnippets = new(GetAttributeProvider(generator));
            (StringMarshalling, Type?) utf8Marshalling = (StringMarshalling.Utf8, null);
            (StringMarshalling, Type?) utf16Marshalling = (StringMarshalling.Utf16, null);
            (StringMarshalling, Type?) customUtf16Marshalling = (StringMarshalling.Custom, typeof(Utf16StringMarshaller));
            (StringMarshalling, Type?) customWithNoType = (StringMarshalling.Custom, null);
            (StringMarshalling, Type?) utf8WithType = (StringMarshalling.Utf8, typeof(Utf16StringMarshaller));
            DiagnosticResult[] emptyDiagnostics = new DiagnosticResult[] { };

            // StringMarshalling.Custom / StringMarshallingCustomType invalid
            yield return new object[]
            {
                ID(),
                codeSnippets.DerivedWithStringMarshalling(customWithNoType),
                new DiagnosticResult[]
                {
                    VerifyComInterfaceGenerator.Diagnostic(GeneratorDiagnostics.InvalidStringMarshallingConfigurationOnInterface)
                        .WithLocation(0)
                        .WithArguments("Test.IStringMarshalling0", CustomStringMarshallingWithNoCustomTypeMessage)
                }
            };
            yield return new object[]
            {
                ID(),
                codeSnippets.DerivedWithStringMarshalling(utf8WithType),
                new DiagnosticResult[]
                {
                    VerifyComInterfaceGenerator.Diagnostic(GeneratorDiagnostics.InvalidStringMarshallingConfigurationOnInterface)
                        .WithLocation(0)
                        .WithArguments("Test.IStringMarshalling0", CustomTypeSpecifiedWithNoStringMarshallingCustom)
                }
            };

            // Inheritance no diagnostic
            yield return new object[]
            {
                ID(),
                codeSnippets.DerivedWithStringMarshalling(utf16Marshalling, utf16Marshalling),
                emptyDiagnostics
            };
            yield return new object[]
            {
                ID(),
                codeSnippets.DerivedWithStringMarshalling(utf8Marshalling, utf8Marshalling),
                emptyDiagnostics
            };
            yield return new object[]
            {
                ID(),
                codeSnippets.DerivedWithStringMarshalling(customUtf16Marshalling, customUtf16Marshalling),
                emptyDiagnostics
            };

            // mismatches
            DiagnosticResult[] mismatchAt1 = MismatchesWithLocations(1);
            yield return new object[]
            {
                ID(),
                codeSnippets.DerivedWithStringMarshalling(utf8Marshalling, utf16Marshalling),
                mismatchAt1
            };
            yield return new object[]
            {
                ID(),
                codeSnippets.DerivedWithStringMarshalling(utf16Marshalling, utf8Marshalling),
                mismatchAt1
            };
            yield return new object[]
            {
                ID(),
                codeSnippets.DerivedWithStringMarshalling(utf16Marshalling, customUtf16Marshalling),
                mismatchAt1
            };
            yield return new object[]
            {
                ID(),
                codeSnippets.DerivedWithStringMarshalling(customUtf16Marshalling, utf16Marshalling),
                mismatchAt1
            };
            yield return new object[]
            {
                ID(),
                codeSnippets.DerivedWithStringMarshalling(utf8Marshalling, customUtf16Marshalling),
                mismatchAt1
            };
            yield return new object[]
            {
                ID(),
                codeSnippets.DerivedWithStringMarshalling(customUtf16Marshalling, utf8Marshalling),
                mismatchAt1
            };

            // Three levels inheritance
            // Matching
            yield return new object[]
            {
                ID(),
                codeSnippets.DerivedWithStringMarshalling(utf8Marshalling, utf8Marshalling, utf8Marshalling),
                emptyDiagnostics
            };
            yield return new object[]
            {
                ID(),
                codeSnippets.DerivedWithStringMarshalling(utf16Marshalling, utf16Marshalling, utf16Marshalling),
                emptyDiagnostics
            };
            yield return new object[]
            {
                ID(),
                codeSnippets.DerivedWithStringMarshalling(customUtf16Marshalling, customUtf16Marshalling, customUtf16Marshalling),
                emptyDiagnostics
            };

            //Mismatches
            yield return new object[]
            {
                ID(),
                codeSnippets.DerivedWithStringMarshalling(utf8Marshalling, utf8Marshalling, utf16Marshalling),
                MismatchesWithLocations(2)
            };
            yield return new object[]
            {
                ID(),
                codeSnippets.DerivedWithStringMarshalling(utf8Marshalling, utf16Marshalling, utf16Marshalling),
                MismatchesWithLocations(1).Concat(BaseCannotBeGeneratedWithLocations(2)).ToArray()
            };
            yield return new object[]
            {
                ID(),
                codeSnippets.DerivedWithStringMarshalling(utf8Marshalling, utf16Marshalling, utf8Marshalling),
                MismatchesWithLocations(1, 2)
            };
            yield return new object[]
            {
                ID(),
                codeSnippets.DerivedWithStringMarshalling(utf8Marshalling, utf16Marshalling, customUtf16Marshalling),
                MismatchesWithLocations(1, 2)
            };

            // Base has no StringMarshalling and Derived does is okay
            string source = $$"""
                using System;
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;
                namespace Test
                {
                    [GeneratedComInterface]
                    [Guid("0E7204B5-4B61-4E06-B872-82BA652F2ECA")]
                    internal partial interface INoStringMarshalling
                    {
                        public int GetInt();
                        public void SetInt(int value);
                    }
                    [GeneratedComInterface(StringMarshalling = StringMarshalling.Utf8)]
                    [Guid("0E7204B5-4B61-5E06-B872-82BA652F2ECA")]
                    internal partial interface IStringMarshalling : INoStringMarshalling
                    {
                        public string GetString();
                        public void SetString(string value);
                    }
                }
                """;
            yield return new object[] { ID(), source, emptyDiagnostics };

            source = $$"""
                using System;
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;
                namespace Test
                {
                    [GeneratedComInterface]
                    [Guid("0E7204B5-4B61-4E06-B872-82BA652F2ECA")]
                    internal partial interface INoStringMarshalling
                    {
                        [return: MarshalUsing(typeof(Utf8StringMarshaller))]
                        public string GetString();
                        public void SetString([MarshalUsing(typeof(Utf8StringMarshaller))] string value);
                    }
                    [GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16)]
                    [Guid("0E7204B5-4B61-5E06-B872-82BA652F2ECA")]
                    internal partial interface IStringMarshalling : INoStringMarshalling
                    {
                        public string GetString2();
                        public void SetString2(string value);
                    }
                }
                """;
            yield return new object[] { ID(), source, emptyDiagnostics };

            // Base many levels up fails, all inheriting fail
            yield return new object[]
            {
                ID(),
                codeSnippets.DerivedWithStringMarshalling(customWithNoType, customUtf16Marshalling, customUtf16Marshalling, customUtf16Marshalling, customUtf16Marshalling, customUtf16Marshalling),
                new DiagnosticResult[]
                {
                    VerifyComInterfaceGenerator.Diagnostic(GeneratorDiagnostics.InvalidStringMarshallingConfigurationOnInterface)
                        .WithLocation(0)
                        .WithArguments("Test.IStringMarshalling0", CustomStringMarshallingWithNoCustomTypeMessage)
                }
                   .Concat(MismatchesWithLocations(1))
                   .Concat(BaseCannotBeGeneratedWithLocations(2, 3, 4, 5))
                   .ToArray()
            };

            DiagnosticResult[] MismatchesWithLocations(params int[] locations)
            {
                return locations
                    .Select(i =>
                        new DiagnosticResult(GeneratorDiagnostics.InvalidStringMarshallingMismatchBetweenBaseAndDerived)
                            .WithLocation(i)
                            .WithArguments($"Test.IStringMarshalling{i}", StringMarshallingMustMatchBase))
                    .ToArray();
            }

            DiagnosticResult[] BaseCannotBeGeneratedWithLocations(params int[] locations)
            {
                return locations
                    .Select(i =>
                        new DiagnosticResult(GeneratorDiagnostics.BaseInterfaceIsNotGenerated)
                            .WithLocation(i)
                            .WithArguments($"Test.IStringMarshalling{i}", $"Test.IStringMarshalling{i - 1}"))
                   .ToArray();
            }
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

        [Theory]
        [MemberData(nameof(StringMarshallingCodeSnippets), GeneratorKind.ComInterfaceGenerator)]
        public async Task ValidateStringMarshallingDiagnostics(string id, string source, DiagnosticResult[] expectedDiagnostics)
        {
            _ = id;
            await VerifyComInterfaceGenerator.VerifySourceGeneratorAsync(source, expectedDiagnostics);
        }

        [Fact]
        public async Task ValidateInterfaceWithoutGuidWarns()
        {
            var source = $$"""

                [System.Runtime.InteropServices.Marshalling.GeneratedComInterface]
                partial interface {|#0:IFoo|}
                {
                    void Method();
                }

            """;
            DiagnosticResult expectedDiagnostic = VerifyComInterfaceGenerator.Diagnostic(GeneratorDiagnostics.InvalidAttributedInterfaceMissingGuidAttribute)
                .WithLocation(0).WithArguments("IFoo");

            await VerifyComInterfaceGenerator.VerifySourceGeneratorAsync(source, expectedDiagnostic);
        }

        [Fact]
        public async Task VerifyGenericInterfaceCreatesDiagnostic()
        {
            var source = $$"""

                namespace Tests
                {
                    public interface IFoo1<T>
                    {
                        void Method();
                    }

                    [System.Runtime.InteropServices.Marshalling.GeneratedComInterface]
                    [System.Runtime.InteropServices.Guid("36722BA8-A03B-406E-AFE6-27AA2F7AC032")]
                    partial interface {|#0:IFoo2|}<T>
                    {
                        void Method();
                    }
                }
                """;

            DiagnosticResult expectedDiagnostic = VerifyComInterfaceGenerator.Diagnostic(GeneratorDiagnostics.InvalidAttributedInterfaceGenericNotSupported)
                .WithLocation(0).WithArguments("IFoo2");

            await VerifyComInterfaceGenerator.VerifySourceGeneratorAsync(source, expectedDiagnostic);
        }

        [Fact]
        public async Task VerifyComInterfaceInheritingFromComInterfaceInOtherAssemblyReportsDiagnostic()
        {
            string additionalSource = $$"""
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;
       
                [GeneratedComInterface]
                [Guid("9D3FD745-3C90-4C10-B140-FAFB01E3541D")]
                public partial interface I
                {
                    void Method();
                }
                """;

            string source = $$"""
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;

                [GeneratedComInterface]
                [Guid("0DB41042-0255-4CDD-B73A-9C5D5F31303D")]
                partial interface {|#0:J|} : I
                {
                    void MethodA();
                }
                """;

            var test = new VerifyComInterfaceGenerator.Test(referenceAncillaryInterop: false)
            {
                TestState =
                {
                    Sources =
                    {
                        ("Source.cs", source)
                    },
                    AdditionalProjects =
                    {
                        ["Other"] =
                        {
                            Sources =
                            {
                                ("Other.cs", additionalSource)
                            },
                        },
                    },
                    AdditionalProjectReferences =
                    {
                        "Other"
                    }
                },
                TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck | TestBehaviors.SkipGeneratedCodeCheck,
            };
            test.TestState.AdditionalProjects["Other"].AdditionalReferences.AddRange(test.TestState.AdditionalReferences);

            test.ExpectedDiagnostics.Add(
                VerifyComInterfaceGenerator
                    .Diagnostic(GeneratorDiagnostics.BaseInterfaceIsNotGenerated)
                    .WithLocation(0)
                    .WithArguments("J", "I"));

            // The Roslyn SDK doesn't apply the compilation options from CreateCompilationOptions to AdditionalProjects-based projects.
            test.SolutionTransforms.Add((sln, _) =>
            {
                var additionalProject = sln.Projects.First(proj => proj.Name == "Other");
                return additionalProject.WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true)).Solution;
            });

            await test.RunAsync();
        }
    }
}
