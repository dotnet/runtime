// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.Marshalling;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.DotNet.XUnitExtensions.Attributes;
using Microsoft.Interop;
using Microsoft.Interop.UnitTests;
using Xunit;
using static Microsoft.Interop.UnitTests.TestUtils;
using StringMarshalling = System.Runtime.InteropServices.StringMarshalling;
using VerifyComInterfaceGenerator = Microsoft.Interop.UnitTests.Verifiers.CSharpSourceGeneratorVerifier<Microsoft.Interop.ComInterfaceGenerator>;

namespace ComInterfaceGenerator.Unit.Tests
{
    public class CompileFails
    {
        public static IEnumerable<object[]> ComInterfaceGeneratorSnippetsToCompile()
        {
            CodeSnippets codeSnippets = new(new GeneratedComInterfaceAttributeProvider());
            // Inheriting from multiple GeneratedComInterface-marked interfaces.
            yield return new object[] { ID(), codeSnippets.DerivedComInterfaceTypeMultipleComInterfaceBases, new[] {
                VerifyComInterfaceGenerator.Diagnostic(GeneratorDiagnostics.MultipleComInterfaceBaseTypes)
                    .WithLocation(0)
                    .WithArguments("IComInterface2")
            } };
            yield return new object[] { ID(), codeSnippets.InterfaceWithPropertiesAndEvents, new[]
            {
               VerifyComInterfaceGenerator.Diagnostic(GeneratorDiagnostics.InstancePropertyDeclaredInInterface)
                   .WithLocation(0)
                   .WithArguments("Property", "INativeAPI"),
               VerifyComInterfaceGenerator.Diagnostic(GeneratorDiagnostics.InstanceEventDeclaredInInterface)
                   .WithLocation(1)
                   .WithArguments("Event", "INativeAPI"),
            } };
        }

        [ParallelTheory]
        [MemberData(nameof(ComInterfaceGeneratorSnippetsToCompile))]
        public async Task ValidateComInterfaceGeneratorSnippets(string id, string source, DiagnosticResult[] expectedDiagnostics)
        {
            TestUtils.Use(id);
            await VerifyComInterfaceGenerator.VerifySourceGeneratorAsync(source, expectedDiagnostics);
        }

        public static IEnumerable<object[]> InvalidUnmanagedToManagedCodeSnippetsToCompile(GeneratorKind generator)
        {
            CodeSnippets codeSnippets = new(generator);

            string safeHandleMarshallerDoesNotSupportManagedToUnmanaged = string.Format(SR.ManagedToUnmanagedMissingRequiredMarshaller, "global::System.Runtime.InteropServices.Marshalling.SafeHandleMarshaller<global::Microsoft.Win32.SafeHandles.SafeFileHandle>");
            string safeHandleMarshallerDoesNotSupportUnmanagedToManaged = string.Format(SR.UnmanagedToManagedMissingRequiredMarshaller, "global::System.Runtime.InteropServices.Marshalling.SafeHandleMarshaller<global::Microsoft.Win32.SafeHandles.SafeFileHandle>");
            string safeHandleMarshallerDoesNotSupportBidirectional = string.Format(SR.BidirectionalMissingRequiredMarshaller, "global::System.Runtime.InteropServices.Marshalling.SafeHandleMarshaller<global::Microsoft.Win32.SafeHandles.SafeFileHandle>");
            // SafeHandles
            yield return new object[] { ID(), codeSnippets.BasicParametersAndModifiers("Microsoft.Win32.SafeHandles.SafeFileHandle"), new[]
            {
                VerifyComInterfaceGenerator.Diagnostic(GeneratorDiagnostics.ReturnTypeNotSupportedWithDetails).WithLocation(0).WithArguments(safeHandleMarshallerDoesNotSupportManagedToUnmanaged, "Method"),
                VerifyComInterfaceGenerator.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails).WithLocation(1).WithArguments(safeHandleMarshallerDoesNotSupportUnmanagedToManaged, "value"),
                // /0/Test0.cs(13,151): error SYSLIB1051: The type 'Microsoft.Win32.SafeHandles.SafeFileHandle' is not supported by source-generated P/Invokes. The generated source will not handle marshalling of parameter 'inValue'.
                VerifyComInterfaceGenerator.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails).WithLocation(2).WithArguments(safeHandleMarshallerDoesNotSupportUnmanagedToManaged, "inValue"),
                // /0/Test0.cs(13,207): error SYSLIB1051: The type 'Microsoft.Win32.SafeHandles.SafeFileHandle' is not supported by source-generated P/Invokes. The generated source will not handle marshalling of parameter 'refValue'.
                VerifyComInterfaceGenerator.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails).WithLocation(3).WithArguments(safeHandleMarshallerDoesNotSupportBidirectional, "refValue"),
                // /0/Test0.cs(13,264): error SYSLIB1051: The type 'Microsoft.Win32.SafeHandles.SafeFileHandle' is not supported by source-generated P/Invokes. The generated source will not handle marshalling of parameter 'outValue'.
                VerifyComInterfaceGenerator.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails).WithLocation(4).WithArguments(safeHandleMarshallerDoesNotSupportManagedToUnmanaged, "outValue"),
            } };


            // Marshallers with only support for their expected places in the signatures in
            // ManagedToUnmanaged marshal modes.
            string marshallerDoesNotSupportManagedToUnmanaged = string.Format(SR.ManagedToUnmanagedMissingRequiredMarshaller, "global::Marshaller");
            string marshallerDoesNotSupportUnmanagedToManaged = string.Format(SR.UnmanagedToManagedMissingRequiredMarshaller, "global::Marshaller");
            DiagnosticResult invalidManagedToUnmanagedParameterDiagnostic = VerifyComInterfaceGenerator.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                .WithLocation(0)
                .WithArguments(marshallerDoesNotSupportManagedToUnmanaged, "value");
            DiagnosticResult invalidUnmanagedToManagedParameterDiagnostic = VerifyComInterfaceGenerator.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                .WithLocation(0)
                .WithArguments(marshallerDoesNotSupportUnmanagedToManaged, "value");
            DiagnosticResult invalidReturnTypeDiagnostic = VerifyComInterfaceGenerator.Diagnostic(GeneratorDiagnostics.ReturnTypeNotSupportedWithDetails)
                .WithLocation(0)
                .WithArguments(marshallerDoesNotSupportManagedToUnmanaged, "Method");
            CustomStructMarshallingCodeSnippets customStructMarshallingCodeSnippets = new(new CodeSnippets.Bidirectional(CodeSnippets.GetAttributeProvider(generator)));
            yield return new object[] { ID(), customStructMarshallingCodeSnippets.Stateless.NativeToManagedOnlyOutParameter, new[] { invalidManagedToUnmanagedParameterDiagnostic } };
            yield return new object[] { ID(), customStructMarshallingCodeSnippets.Stateless.NativeToManagedOnlyReturnValue, new[] { invalidReturnTypeDiagnostic } };
            yield return new object[] { ID(), customStructMarshallingCodeSnippets.Stateless.ByValueInParameter, new[] { invalidUnmanagedToManagedParameterDiagnostic } };
            yield return new object[] { ID(), customStructMarshallingCodeSnippets.Stateful.NativeToManagedOnlyOutParameter, new[] { invalidManagedToUnmanagedParameterDiagnostic } };
            yield return new object[] { ID(), customStructMarshallingCodeSnippets.Stateful.NativeToManagedOnlyReturnValue, new[] { invalidReturnTypeDiagnostic } };
            yield return new object[] { ID(), customStructMarshallingCodeSnippets.Stateful.ByValueInParameter, new[] { invalidUnmanagedToManagedParameterDiagnostic } };
        }

        public static IEnumerable<object[]> StringMarshallingCodeSnippets(GeneratorKind generator)
        {
            string CustomStringMarshallingWithNoCustomTypeMessage = SR.InvalidStringMarshallingConfigurationMissingCustomType;
            string CustomTypeSpecifiedWithNoStringMarshallingCustom = SR.InvalidStringMarshallingConfigurationNotCustom;
            string StringMarshallingMustMatchBase = SR.GeneratedComInterfaceStringMarshallingMustMatchBase;

            CodeSnippets codeSnippets = new(generator);
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
            CustomStructMarshallingCodeSnippets customStructMarshallingCodeSnippets = new(new CodeSnippets.Bidirectional(CodeSnippets.GetAttributeProvider(generator)));

            yield return new[] { ID(), customStructMarshallingCodeSnippets.Stateless.NativeToManagedOnlyInParameter };
            yield return new[] { ID(), customStructMarshallingCodeSnippets.Stateless.ByValueOutParameter };
            yield return new[] { ID(), customStructMarshallingCodeSnippets.Stateful.NativeToManagedOnlyInParameter };
            yield return new[] { ID(), customStructMarshallingCodeSnippets.Stateful.ByValueOutParameter };
        }

        [ParallelTheory]
        [MemberData(nameof(InvalidUnmanagedToManagedCodeSnippetsToCompile), GeneratorKind.ComInterfaceGenerator)]
        public async Task ValidateInvalidUnmanagedToManagedCodeSnippets(string id, string source, DiagnosticResult[] expectedDiagnostics)
        {
            _ = id;
            VerifyComInterfaceGenerator.Test test = new(referenceAncillaryInterop: false)
            {
                TestCode = source,
                TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
            };
            test.ExpectedDiagnostics.AddRange(expectedDiagnostics);
            await test.RunAsync();
        }

        [ParallelTheory]
        [MemberData(nameof(InvalidManagedToUnmanagedCodeSnippetsToCompile), GeneratorKind.ComInterfaceGenerator)]
        public async Task ValidateInvalidManagedToUnmanagedCodeSnippets(string id, string source)
        {
            _ = id;

            DiagnosticResult expectedDiagnostic = VerifyComInterfaceGenerator.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                .WithLocation(0)
                .WithArguments(string.Format(SR.ManagedToUnmanagedMissingRequiredMarshaller, "global::Marshaller"), "value");
            await VerifyComInterfaceGenerator.VerifySourceGeneratorAsync(source, expectedDiagnostic);
        }

        [ParallelTheory]
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

        public static IEnumerable<object[]> InterfaceVisibilities()
        {
            var emptyDiagnostics = new DiagnosticResult[] { };
            var privateDiagnostic = new DiagnosticResult[]{
                    new DiagnosticResult(GeneratorDiagnostics.InvalidAttributedInterfaceNotAccessible)
                    .WithLocation(0)
                    .WithArguments("Test.IComInterface", "'Test.IComInterface' has accessibility 'private'.")
            };
            var protectedDiagnostic = new DiagnosticResult[]{
                    new DiagnosticResult(GeneratorDiagnostics.InvalidAttributedInterfaceNotAccessible)
                    .WithLocation(0)
                    .WithArguments("Test.IComInterface", "'Test.IComInterface' has accessibility 'protected'.")
            };

            var group = new List<(string, DiagnosticResult[], string)>()
            {
                ("public", emptyDiagnostics, ID()),
                ("internal", emptyDiagnostics, ID()),
                ("protected", protectedDiagnostic, ID()),
                ("private", privateDiagnostic, ID()),
            };
            foreach (var (interfaceVisibility, diagnostics, id) in group)
            {
                var source = $$"""
                using System;
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;

                public static unsafe partial class Test {
                    [GeneratedComInterface]
                    [Guid("B585EEFE-85B2-45BA-935E-C993C81D038C")]
                    {{interfaceVisibility}} partial interface {|#{{0}}:IComInterface|}
                    {
                        public int Get();
                        public void Set(int value);
                    }
                }
                """;
                yield return new object[] { id, source, diagnostics };
            }
        }

        public static IEnumerable<object[]> StringMarshallingCustomTypeVisibilities()
        {
            var emptyDiagnostics = new DiagnosticResult[] { };
            var privateDiagnostic = new DiagnosticResult[]{
                    new DiagnosticResult(GeneratorDiagnostics.StringMarshallingCustomTypeNotAccessibleByGeneratedCode)
                    .WithLocation(0)
                    .WithArguments("Test.CustomStringMarshallingType", "'Test.CustomStringMarshallingType' has accessibility 'private'.")
                };
            var protectedDiagnostic = new DiagnosticResult[]{
                    new DiagnosticResult(GeneratorDiagnostics.StringMarshallingCustomTypeNotAccessibleByGeneratedCode)
                    .WithLocation(0)
                    .WithArguments("Test.CustomStringMarshallingType", "'Test.CustomStringMarshallingType' has accessibility 'protected'.")
                };

            var group = new List<(string, string, DiagnosticResult[], string)>()
            {
                ("public", "public", emptyDiagnostics, ID()),
                // Technically we don't support inheriting from a GeneratedComInterface from another assembly, so this should be okay
                ("public", "internal", emptyDiagnostics, ID()),
                ("public", "protected", protectedDiagnostic, ID()),
                ("public", "private", privateDiagnostic, ID()),
                ("internal", "public", emptyDiagnostics, ID()),
                ("internal", "internal", emptyDiagnostics, ID()),
                ("internal", "protected", protectedDiagnostic, ID()),
                ("internal", "private", privateDiagnostic, ID()),
            };
            foreach (var (interfaceVisibility, customTypeVisibility, diagnostics, id) in group)
            {
                var source = $$"""
                using System;
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;

                public static unsafe partial class Test {
                    [CustomMarshaller(typeof(string), MarshalMode.Default, typeof(CustomStringMarshallingType))]
                    {{customTypeVisibility}} static class CustomStringMarshallingType
                    {
                        public static string ConvertToManaged(ushort* unmanaged) => throw new NotImplementedException();
                        public static ushort* ConvertToUnmanaged(string managed) => throw new NotImplementedException();
                        public static void Free(ushort* unmanaged) => throw new NotImplementedException();
                        public static ref readonly char GetPinnableReference(string str) => throw new NotImplementedException();
                    }

                    [GeneratedComInterface(StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(CustomStringMarshallingType))]
                    [Guid("B585EEFE-85B2-45BA-935E-C993C81D038C")]
                    {{interfaceVisibility}} partial interface {|#{{0}}:IStringMarshalling|}
                    {
                        public string GetString();
                        public void SetString(string value);
                    }
                }
                """;
                yield return new object[] { id, source, diagnostics };
            }
        }

        [ParallelTheory]
        [MemberData(nameof(StringMarshallingCustomTypeVisibilities))]
        public async Task VerifyStringMarshallingCustomTypeWithLessVisibilityThanInterfaceWarns(string id, string source, DiagnosticResult[] diagnostics)
        {
            _ = id;
            await VerifyComInterfaceGenerator.VerifySourceGeneratorAsync(source, diagnostics);
        }

        [ParallelTheory]
        [MemberData(nameof(InterfaceVisibilities))]
        public async Task VerifyInterfaceWithLessVisibilityThanInterfaceWarns(string id, string source, DiagnosticResult[] diagnostics)
        {
            _ = id;
            await VerifyComInterfaceGenerator.VerifySourceGeneratorAsync(source, diagnostics);
        }

        [Fact]
        public async Task VerifyNonPartialInterfaceWarns()
        {
            string basic = $$"""
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;

                [GeneratedComInterface]
                [Guid("9D3FD745-3C90-4C10-B140-FAFB01E3541D")]
                public interface {|#0:I|}
                {
                    void Method();
                }
                """;
            string containingTypeIsNotPartial = $$"""
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;

                public static class Test
                {
                    [GeneratedComInterface]
                    [Guid("9D3FD745-3C90-4C10-B140-FAFB01E3541D")]
                    public partial interface {|#0:I|}
                    {
                        void Method();
                    }
                }
                """;
            await VerifyComInterfaceGenerator.VerifySourceGeneratorAsync(basic, new DiagnosticResult(GeneratorDiagnostics.InvalidAttributedInterfaceMissingPartialModifiers).WithLocation(0).WithArguments("I"));
            await VerifyComInterfaceGenerator.VerifySourceGeneratorAsync(containingTypeIsNotPartial, new DiagnosticResult(GeneratorDiagnostics.InvalidAttributedInterfaceMissingPartialModifiers).WithLocation(0).WithArguments("I"));
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

        [Fact]
        public async Task VerifyDiagnosticIsOnAttributedSyntax()
        {
            string source = $$"""
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;

                partial interface J
                {
                }

                [GeneratedComInterface]
                partial interface {|#0:J|}
                {
                    void Method();
                }

                partial interface J
                {
                }
                """;
            DiagnosticResult expectedDiagnostic = VerifyComInterfaceGenerator.Diagnostic(GeneratorDiagnostics.InvalidAttributedInterfaceMissingGuidAttribute)
                .WithLocation(0).WithArguments("J");
            await VerifyComInterfaceGenerator.VerifySourceGeneratorAsync(source, expectedDiagnostic);
        }

        internal class UnsafeBlocksNotAllowedTest : VerifyComInterfaceGenerator.Test
        {
            internal UnsafeBlocksNotAllowedTest(bool referenceAncillaryInterop) : base(referenceAncillaryInterop) { }
            protected override CompilationOptions CreateCompilationOptions()
                => new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: false);
        }

        [Fact]
        public async Task VerifyGeneratedComInterfaceWithoutAllowUnsafeBlocksWarns()
        {
            string source = $$"""
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;

                [GeneratedComInterface]
                partial interface {|#0:J|}
                {
                    void Method();
                }
                """;
            DiagnosticResult expectedDiagnostic = VerifyComInterfaceGenerator.Diagnostic(GeneratorDiagnostics.RequiresAllowUnsafeBlocks)
                .WithLocation(0);
            var test = new UnsafeBlocksNotAllowedTest(false);
            test.TestState.Sources.Add(source);
            test.ExpectedDiagnostics.Add(expectedDiagnostic);
            await test.RunAsync();
        }

        public static IEnumerable<object[]> CountParameterIsOutSnippets()
        {
            var g = CodeSnippets.GetAttributeProvider(GeneratorKind.ComInterfaceGenerator);
            CodeSnippets a = new(g);
            DiagnosticResult returnValueDiag = new DiagnosticResult(GeneratorDiagnostics.SizeOfInCollectionMustBeDefinedAtCallReturnValue)
                .WithLocation(1)
                .WithArguments("arr");
            DiagnosticResult outParamDiag = new DiagnosticResult(GeneratorDiagnostics.SizeOfInCollectionMustBeDefinedAtCallOutParam)
                .WithLocation(1)
                .WithArguments("arr", "size");

            var voidReturn = ("void", "", Array.Empty<string>());

            var size = ("int", "", "size", Array.Empty<string>());
            var outSize = ("int", "out", "size", Array.Empty<string>());
            var inSize = ("int", "in", "size", Array.Empty<string>());
            var refSize = ("int", "ref", "size", Array.Empty<string>());

            var arr = ("IntStruct[]", "", "arr", new[] { "nameof(size)" });
            var outArr = ("IntStruct[]", "out", "arr", new[] { "nameof(size)" });
            var inArr = ("IntStruct[]", "in", "arr", new[] { "nameof(size)" });
            var refArr = ("IntStruct[]", "ref", "arr", new[] { "nameof(size)" });
            var contentsOutArr = ("IntStruct[]", "[OutAttribute]", "arr", new[] { "nameof(size)" });
            var contentsInOutArr = ("IntStruct[]", "[InAttribute, OutAttribute]", "arr", new[] { "nameof(size)" });

            yield return new object[] { ID(), Source(voidReturn, arr, size) };
            yield return new object[] { ID(), Source(voidReturn, arr, inSize) };
            yield return new object[] { ID(), Source(voidReturn, arr, outSize), outParamDiag };
            yield return new object[] { ID(), Source(voidReturn, arr, refSize) };

            yield return new object[] { ID(), Source(voidReturn, inArr, size) };
            yield return new object[] { ID(), Source(voidReturn, inArr, inSize) };
            yield return new object[] { ID(), Source(voidReturn, inArr, outSize), outParamDiag };
            yield return new object[] { ID(), Source(voidReturn, inArr, refSize) };

            yield return new object[] { ID(), Source(voidReturn, outArr, size) };
            yield return new object[] { ID(), Source(voidReturn, outArr, inSize) };
            yield return new object[] { ID(), Source(voidReturn, outArr, outSize) };
            yield return new object[] { ID(), Source(voidReturn, outArr, refSize) };

            yield return new object[] { ID(), Source(voidReturn, refArr, size) };
            yield return new object[] { ID(), Source(voidReturn, refArr, inSize) };
            yield return new object[] { ID(), Source(voidReturn, refArr, outSize), outParamDiag };
            yield return new object[] { ID(), Source(voidReturn, refArr, refSize) };

            yield return new object[] { ID(), Source(voidReturn, contentsOutArr, size) };
            yield return new object[] { ID(), Source(voidReturn, contentsOutArr, inSize) };
            yield return new object[] { ID(), Source(voidReturn, contentsOutArr, outSize), outParamDiag };
            yield return new object[] { ID(), Source(voidReturn, contentsOutArr, refSize) };

            yield return new object[] { ID(), Source(voidReturn, contentsInOutArr, size) };
            yield return new object[] { ID(), Source(voidReturn, contentsInOutArr, inSize) };
            yield return new object[] { ID(), Source(voidReturn, contentsInOutArr, outSize), outParamDiag };
            yield return new object[] { ID(), Source(voidReturn, contentsInOutArr, refSize) };

            var sizeReturn = ("int", "", Array.Empty<string>());

            var arrReturnSize = ("IntStruct[]", "", "arr", new[] { "MarshalUsingAttribute.ReturnsCountValue" });
            var outArrReturnSize = ("IntStruct[]", "out", "arr", new[] { "MarshalUsingAttribute.ReturnsCountValue" });
            var inArrReturnSize = ("IntStruct[]", "in", "arr", new[] { "MarshalUsingAttribute.ReturnsCountValue" });
            var refArrReturnSize = ("IntStruct[]", "ref", "arr", new[] { "MarshalUsingAttribute.ReturnsCountValue" });
            var contentsOutArrReturnSize = ("IntStruct[]", "[OutAttribute]", "arr", new[] { "MarshalUsingAttribute.ReturnsCountValue" });
            var contentsInOutArrReturnSize = ("IntStruct[]", "[InAttribute, OutAttribute]", "arr", new[] { "MarshalUsingAttribute.ReturnsCountValue" });

            yield return new object[] { ID(), Source(sizeReturn, arrReturnSize), returnValueDiag };
            yield return new object[] { ID(), Source(sizeReturn, outArrReturnSize) };
            yield return new object[] { ID(), Source(sizeReturn, inArrReturnSize), returnValueDiag };
            yield return new object[] { ID(), Source(sizeReturn, refArrReturnSize), returnValueDiag };
            yield return new object[] { ID(), Source(sizeReturn, contentsOutArrReturnSize), returnValueDiag };
            yield return new object[] { ID(), Source(sizeReturn, contentsInOutArrReturnSize), returnValueDiag };

            var returnArr = ("IntStruct[]", "", new[] { "size" });

            yield return new object[] { ID(), Source(returnArr, size) };
            yield return new object[] { ID(), Source(returnArr, inSize) };
            yield return new object[] { ID(), Source(returnArr, outSize) };
            yield return new object[] { ID(), Source(returnArr, refSize) };

            string Source(
                (string type, string modifiers, string[] counts) returnValue,
                params (string type, string modifiers, string name, string[] counts)[] parameters)
            {
                return a.CollectionMarshallingWithCountRefKinds(returnValue, parameters)
                    + "[NativeMarshalling(typeof(IntStructMarshaller))]"
                    + CodeSnippets.IntStructAndMarshaller;
            }
        }

        [ParallelTheory]
        [MemberData(nameof(CountParameterIsOutSnippets))]
        public async Task ValidateSizeParameterRefKindDiagnostics(string ID, string source, params DiagnosticResult[] diagnostics)
        {
            _ = ID;
            await VerifyComInterfaceGenerator.VerifySourceGeneratorAsync(source, diagnostics);
        }

        public static IEnumerable<object[]> IntAndEnumReturnTypeSnippets()
        {
            var managedReturnWillBeOutDiagnostic = VerifyComInterfaceGenerator.Diagnostic(GeneratorDiagnostics.ComMethodManagedReturnWillBeOutVariable).WithLocation(0);
            var hresultReturnStructWillBeStructDiagnostic = VerifyComInterfaceGenerator.Diagnostic(GeneratorDiagnostics.HResultTypeWillBeTreatedAsStruct).WithLocation(0);
            var enumDecl = $$"""
                internal enum Err
                {
                    Val1, Val2
                }
                """;
            var structDeclHR = $$"""
                internal struct HR
                {
                    int hr;
                }
                """;
            var structDeclHResult = $$"""
                internal struct HResult
                {
                    int hr;
                }
                """;
            var enumReturn = Template("Err", enumDecl, "");
            var intReturn = Template("int", "", "");
            var floatReturn = Template("float", "", "");
            var structHrReturn = Template("HR", structDeclHR, "");
            var structHResultReturn = Template("HResult", structDeclHResult, "");
            yield return new object[] {
                ID(),
                enumReturn,
                new DiagnosticResult[] { managedReturnWillBeOutDiagnostic }
            };
            yield return new object[] {
                ID(),
                intReturn,
                new DiagnosticResult[] { managedReturnWillBeOutDiagnostic }
            };
            yield return new object[] {
                ID(),
                structHrReturn,
                new DiagnosticResult[] { managedReturnWillBeOutDiagnostic }
            };
            yield return new object[] {
                ID(),
                structHResultReturn,
                new DiagnosticResult[] { managedReturnWillBeOutDiagnostic }
            };
            yield return new object[] {
                ID(),
                floatReturn,
                new DiagnosticResult[] {  }
            };

            var enumReturnPreserveSig = Template("Err", enumDecl, "[PreserveSig]");
            var intReturnPreserveSig = Template("int", "", "[PreserveSig]");
            var structHrPreserveSig = Template("HR", structDeclHR, "[PreserveSig]");
            var structHResultPreserveSig = Template("HResult", structDeclHResult, "[PreserveSig]");
            yield return new object[] {
                ID(),
                enumReturnPreserveSig,
                new DiagnosticResult[] {  }
            };
            yield return new object[] {
                ID(),
                intReturnPreserveSig,
                new DiagnosticResult[] {  }
            };
            yield return new object[] {
                ID(),
                structHrPreserveSig,
                new DiagnosticResult[] { hresultReturnStructWillBeStructDiagnostic.WithArguments("HR") }
            };
            yield return new object[] {
                ID(),
                structHResultPreserveSig,
                new DiagnosticResult[] { hresultReturnStructWillBeStructDiagnostic.WithArguments("HResult") }
            };

            var structHResultPreserveSigWithMarshalAs = Template("HResult", structDeclHResult, "[PreserveSig][return:MarshalAs(UnmanagedType.Error)]");
            yield return new object[] {
                ID(),
                structHResultPreserveSigWithMarshalAs,
                new DiagnosticResult[] { }
            };

            var intReturnMarshalAs = Template("int", "", "[return: MarshalAs(UnmanagedType.I4)]");
            yield return new object[] {
                ID(),
                intReturnMarshalAs,
                new DiagnosticResult[] {  }
            };

            string Template(string type, string typedef, string attribute)
            {
                return $$"""
                    using System.Runtime.InteropServices;
                    using System.Runtime.InteropServices.Marshalling;
                    [GeneratedComInterface]
                    [Guid("0DB41042-0255-4CDD-B73A-9C5D5F31303D")]
                    partial interface I
                    {
                        {{attribute}}
                        {{type}} {|#0:Method|}();
                    }
                    {{typedef}}
                    """;
            }
        }

        [ParallelTheory]
        [MemberData(nameof(IntAndEnumReturnTypeSnippets))]
        public async Task ValidateReturnTypeInfoDiagnostics(string id, string source, DiagnosticResult[] diagnostics)
        {
            _ = id;

            var test = new VerifyComInterfaceGenerator.Test(referenceAncillaryInterop: false)
            {
                TestState =
                {
                    Sources =
                    {
                        ("Source.cs", source)
                    },
                },
                TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck | TestBehaviors.SkipGeneratedCodeCheck,
            };
            test.ExpectedDiagnostics.AddRange(diagnostics);
            test.DisabledDiagnostics.Remove(GeneratorDiagnostics.Ids.NotRecommendedGeneratedComInterfaceUsage);
            await test.RunAsync();
        }

        [Fact]
        public async Task ByRefInVariant_ReportsNotRecommendedDiagnostic()
        {
            CodeSnippets codeSnippets = new CodeSnippets(GeneratorKind.ComInterfaceGeneratorManagedObjectWrapper);

            var test = new VerifyComInterfaceGenerator.Test(referenceAncillaryInterop: false)
            {
                TestCode = codeSnippets.MarshalAsParameterAndModifiers("object", System.Runtime.InteropServices.UnmanagedType.Struct),
                TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck | TestBehaviors.SkipGeneratedCodeCheck,
            };
            test.ExpectedDiagnostics.Add(
                VerifyComInterfaceGenerator
                    .Diagnostic(GeneratorDiagnostics.GeneratedComInterfaceUsageDoesNotFollowBestPractices)
                    .WithLocation(2)
                    .WithArguments(SR.InVariantShouldBeRef));
            test.DisabledDiagnostics.Remove(GeneratorDiagnostics.Ids.NotRecommendedGeneratedComInterfaceUsage);
            await test.RunAsync();
        }
    }
}
