// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.Marshalling;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.Interop;
using Microsoft.Interop.UnitTests;
using Xunit;
using StringMarshalling = System.Runtime.InteropServices.StringMarshalling;
using VerifyComInterfaceGenerator = Microsoft.Interop.UnitTests.Verifiers.CSharpSourceGeneratorVerifier<Microsoft.Interop.ComInterfaceGenerator>;

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
                GeneratorKind.ComInterfaceGeneratorManagedObjectWrapper => new GeneratedComInterfaceAttributeProvider(System.Runtime.InteropServices.Marshalling.ComInterfaceOptions.ManagedObjectWrapper),
                GeneratorKind.ComInterfaceGeneratorComObjectWrapper => new GeneratedComInterfaceAttributeProvider(System.Runtime.InteropServices.Marshalling.ComInterfaceOptions.ComObjectWrapper),
                GeneratorKind.ComInterfaceGenerator => new GeneratedComInterfaceAttributeProvider(),
                _ => throw new UnreachableException(),
            };

        public static IEnumerable<object[]> InvalidUnmanagedToManagedCodeSnippetsToCompile(GeneratorKind generator)
        {
            CodeSnippets codeSnippets = new(GetAttributeProvider(generator));

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
            string CustomStringMarshallingWithNoCustomTypeMessage = SR.InvalidStringMarshallingConfigurationMissingCustomType;
            string CustomTypeSpecifiedWithNoStringMarshallingCustom = SR.InvalidStringMarshallingConfigurationNotCustom;
            string StringMarshallingMustMatchBase = SR.GeneratedComInterfaceStringMarshallingMustMatchBase;

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
                .WithArguments(string.Format(SR.ManagedToUnmanagedMissingRequiredMarshaller, "global::Marshaller"), "value");
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

        [Theory]
        [MemberData(nameof(StringMarshallingCustomTypeVisibilities))]
        public async Task VerifyStringMarshallingCustomTypeWithLessVisibilityThanInterfaceWarns(string id, string source, DiagnosticResult[] diagnostics)
        {
            _ = id;
            await VerifyComInterfaceGenerator.VerifySourceGeneratorAsync(source, diagnostics);
        }

        [Theory]
        [MemberData(nameof(InterfaceVisibilities))]
        public async Task VerifyInterfaceWithLessVisibilityThanInterfaceWarns(string id, string source, DiagnosticResult[] diagnostics)
        {
            _ = id;
            await VerifyComInterfaceGenerator.VerifySourceGeneratorAsync(source, diagnostics);
        }

        public static IEnumerable<object[]> ByValueMarshalAttributeOnValueTypes()
        {
            var codeSnippets = new CodeSnippets(GetAttributeProvider(GeneratorKind.ComInterfaceGenerator));
            const string inAttribute = "[{|#1:InAttribute|}]";
            const string outAttribute = "[{|#2:OutAttribute|}]";
            const string paramName = "p";
            string paramNameWithLocation = $$"""{|#0:{{paramName}}|}""";
            var inAttributeIsDefaultDiagnostic = new DiagnosticResult(GeneratorDiagnostics.UnnecessaryParameterMarshallingInfo)
                    .WithLocation(0)
                    .WithLocation(1)
                    .WithArguments(SR.InOutAttributes, paramName, SR.InAttributeOnlyIsDefault);


            // [In] is default for all non-pinned marshalled types
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(inAttribute, "int", paramNameWithLocation), new DiagnosticResult[] {
                inAttributeIsDefaultDiagnostic,
                //https://github.com/dotnet/runtime/issues/88540
                inAttributeIsDefaultDiagnostic } };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(inAttribute, "byte", paramNameWithLocation), new DiagnosticResult[] {
                inAttributeIsDefaultDiagnostic,
                //https://github.com/dotnet/runtime/issues/88540
                inAttributeIsDefaultDiagnostic } };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(inAttribute + "[MarshalAs(UnmanagedType.U4)]", "bool", paramNameWithLocation), new DiagnosticResult[] {
                inAttributeIsDefaultDiagnostic,
                //https://github.com/dotnet/runtime/issues/88540
                inAttributeIsDefaultDiagnostic } };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(inAttribute + "[MarshalAs(UnmanagedType.U2)]", "char", paramNameWithLocation), new DiagnosticResult[] {
                inAttributeIsDefaultDiagnostic,
                //https://github.com/dotnet/runtime/issues/88540
                inAttributeIsDefaultDiagnostic } };

            // [Out] is not allowed on value types passed by value - there is no indirection for the callee to make visible modifications.
            var outAttributeNotSupportedOnValueParameters = new DiagnosticResult(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(0)
                    .WithArguments(SR.OutAttributeNotSupportedOnByValueParameters, paramName);
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(outAttribute, "int", paramNameWithLocation), new DiagnosticResult[] {
                outAttributeNotSupportedOnValueParameters,
                //https://github.com/dotnet/runtime/issues/88540
                outAttributeNotSupportedOnValueParameters } };
            yield return new object[] {
                ID(),
                codeSnippets.ByValueMarshallingOfType(outAttribute, "IntStruct", paramNameWithLocation) + CodeSnippets.IntStructAndMarshaller,
                new DiagnosticResult[] {
                    outAttributeNotSupportedOnValueParameters,
                    //https://github.com/dotnet/runtime/issues/88540
                    outAttributeNotSupportedOnValueParameters,
                } };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(outAttribute + "[MarshalAs(UnmanagedType.U4)]", "bool", paramNameWithLocation), new DiagnosticResult[] {
                outAttributeNotSupportedOnValueParameters,
                //https://github.com/dotnet/runtime/issues/88540
                outAttributeNotSupportedOnValueParameters
            } };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(outAttribute, "[MarshalAs(UnmanagedType.U2)] char", paramNameWithLocation), new DiagnosticResult[] {
                outAttributeNotSupportedOnValueParameters,
                //https://github.com/dotnet/runtime/issues/88540
                outAttributeNotSupportedOnValueParameters
            } };
            // [In,Out] should only warn for Out attribute
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(inAttribute+outAttribute, "int", paramNameWithLocation), new DiagnosticResult[] {
                outAttributeNotSupportedOnValueParameters,
                //https://github.com/dotnet/runtime/issues/88540
                outAttributeNotSupportedOnValueParameters } };
            yield return new object[] {
                ID(),
                codeSnippets.ByValueMarshallingOfType(inAttribute+outAttribute, "IntStruct", paramNameWithLocation) + CodeSnippets.IntStructAndMarshaller,
                new DiagnosticResult[] {
                    outAttributeNotSupportedOnValueParameters,
                    //https://github.com/dotnet/runtime/issues/88540
                    outAttributeNotSupportedOnValueParameters,
                } };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(inAttribute + outAttribute + "[MarshalAs(UnmanagedType.U4)]", "bool", paramNameWithLocation), new DiagnosticResult[] {
                outAttributeNotSupportedOnValueParameters,
                //https://github.com/dotnet/runtime/issues/88540
                outAttributeNotSupportedOnValueParameters
            } };
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(inAttribute + outAttribute, "[MarshalAs(UnmanagedType.U2)] char", paramNameWithLocation), new DiagnosticResult[] {
                outAttributeNotSupportedOnValueParameters,
                //https://github.com/dotnet/runtime/issues/88540
                outAttributeNotSupportedOnValueParameters
            } };

            // Any ref keyword is okay for value types
            yield return new object[] {
                ID(),
                codeSnippets.ByValueMarshallingOfType("out", "IntStruct", paramNameWithLocation) + CodeSnippets.IntStructAndMarshaller,
                new DiagnosticResult[] {}
            };
            yield return new object[] {
                ID(),
                codeSnippets.ByValueMarshallingOfType("out", "byte", paramNameWithLocation),
                new DiagnosticResult[] {}
            };
            yield return new object[] {
                ID(),
                codeSnippets.ByValueMarshallingOfType("", "[MarshalAs(UnmanagedType.U2)] out char", paramNameWithLocation),
                new DiagnosticResult[] {}
            };
            yield return new object[] {
                ID(),
                codeSnippets.ByValueMarshallingOfType("in", "IntStruct", paramNameWithLocation) + CodeSnippets.IntStructAndMarshaller,
                new DiagnosticResult[] {}
            };
            yield return new object[] {
                ID(),
                codeSnippets.ByValueMarshallingOfType("in", "byte", paramNameWithLocation),
                new DiagnosticResult[] {}
            };
            yield return new object[] {
                ID(),
                codeSnippets.ByValueMarshallingOfType("", "[MarshalAs(UnmanagedType.U2)] in char", paramNameWithLocation),
                new DiagnosticResult[] {}
            };
            yield return new object[] {
                ID(),
                codeSnippets.ByValueMarshallingOfType("ref", "IntStruct", paramNameWithLocation) + CodeSnippets.IntStructAndMarshaller,
                new DiagnosticResult[] {}
            };
            yield return new object[] {
                ID(),
                codeSnippets.ByValueMarshallingOfType("ref", "byte", paramNameWithLocation),
                new DiagnosticResult[] {}
            };
            yield return new object[] {
                ID(),
                codeSnippets.ByValueMarshallingOfType("", "[MarshalAs(UnmanagedType.U2)] ref char", paramNameWithLocation),
                new DiagnosticResult[] {}
            };
        }

        public static IEnumerable<object[]> ByValueMarshalAttributeOnReferenceTypes()
        {
            var codeSnippets = new CodeSnippets(GetAttributeProvider(GeneratorKind.ComInterfaceGenerator));
            const string inAttribute = "[{|#1:InAttribute|}]";
            const string outAttribute = "[{|#2:OutAttribute|}]";
            const string paramName = "p";
            string paramNameWithLocation = $$"""{|#0:{{paramName}}|}""";
            var inAttributeIsDefaultDiagnostic = new DiagnosticResult(GeneratorDiagnostics.UnnecessaryParameterMarshallingInfo)
                    .WithLocation(0)
                    .WithLocation(1)
                    .WithArguments(SR.InOutAttributes, paramName, SR.InAttributeOnlyIsDefault);

            // [In] is default
            yield return new object[] {
                ID(),
                codeSnippets.ByValueMarshallingOfType(inAttribute, "string", paramNameWithLocation, (StringMarshalling.Utf8, null)),
                new DiagnosticResult[] { inAttributeIsDefaultDiagnostic, inAttributeIsDefaultDiagnostic }
            };
            yield return new object[] {
                ID(),
                codeSnippets.ByValueMarshallingOfType(inAttribute, "IntClass", paramNameWithLocation) + CodeSnippets.IntClassAndMarshaller,
                new DiagnosticResult[] { inAttributeIsDefaultDiagnostic, inAttributeIsDefaultDiagnostic }
            };

            var outNotAllowedOnRefTypes = new DiagnosticResult(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                        .WithLocation(0)
                        .WithArguments(SR.OutAttributeNotSupportedOnByValueParameters, paramName);

            // [Out] is not allowed on strings
            yield return new object[] {
                ID(),
                codeSnippets.ByValueMarshallingOfType(outAttribute, "string", paramNameWithLocation, (StringMarshalling.Utf8, null)),
                new DiagnosticResult[] { outNotAllowedOnRefTypes, outNotAllowedOnRefTypes }
            };

            // [Out] warns on by value reference types
            yield return new object[] {
                ID(),
                codeSnippets.ByValueMarshallingOfType(outAttribute, "IntClass", paramNameWithLocation) + CodeSnippets.IntClassAndMarshaller,
                new DiagnosticResult[] { outNotAllowedOnRefTypes, outNotAllowedOnRefTypes }
            };

            // [In,Out] is fine on classes
            yield return new object[] {
                ID(),
                codeSnippets.ByValueMarshallingOfType(inAttribute + outAttribute, "IntClass", paramNameWithLocation) + CodeSnippets.IntClassAndMarshaller,
                new DiagnosticResult[] { outNotAllowedOnRefTypes, outNotAllowedOnRefTypes }
            };

            // All refkinds are okay on classes and strings
            yield return new object[] {
                ID(),
                codeSnippets.ByValueMarshallingOfType("in", "string", paramNameWithLocation, (StringMarshalling.Utf8, null)),
                new DiagnosticResult[] { }
            };
            yield return new object[] {
                ID(),
                codeSnippets.ByValueMarshallingOfType("in", "IntClass", paramNameWithLocation) + CodeSnippets.IntClassAndMarshaller,
                new DiagnosticResult[] { }
            };
            yield return new object[] {
                ID(),
                codeSnippets.ByValueMarshallingOfType("out", "string", paramNameWithLocation, (StringMarshalling.Utf8, null)),
                new DiagnosticResult[] { }
            };
            yield return new object[] {
                ID(),
                codeSnippets.ByValueMarshallingOfType("out", "IntClass", paramNameWithLocation) + CodeSnippets.IntClassAndMarshaller,
                new DiagnosticResult[] { }
            };
            yield return new object[] {
                ID(),
                codeSnippets.ByValueMarshallingOfType("ref", "string", paramNameWithLocation, (StringMarshalling.Utf8, null)),
                new DiagnosticResult[] { }
            };
            yield return new object[] {
                ID(),
                codeSnippets.ByValueMarshallingOfType("ref", "IntClass", paramNameWithLocation) + CodeSnippets.IntClassAndMarshaller,
                new DiagnosticResult[] { }
            };
        }

        public static IEnumerable<object[]> ByValueMarshalAttributeOnPinnedMarshalledTypes()
        {
            var codeSnippets = new CodeSnippets(GetAttributeProvider(GeneratorKind.ComInterfaceGenerator));
            const string inAttribute = "[{|#1:InAttribute|}]";
            const string outAttribute = "[{|#2:OutAttribute|}]";
            const string paramName = "p";
            string paramNameWithLocation = $$"""{|#0:{{paramName}}|}""";
            const string constElementCount = @"[MarshalUsing(ConstantElementCount = 10)]";
            var inAttributeIsDefaultDiagnostic = new DiagnosticResult(GeneratorDiagnostics.UnnecessaryParameterMarshallingInfo)
                    .WithLocation(0)
                    .WithLocation(1)
                    .WithArguments(SR.InOutAttributes, paramName, SR.InAttributeOnlyIsDefault);
            // Pinned arrays cannot be [In]
            var inAttributeNotSupportedOnBlittableArray = new DiagnosticResult(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(0)
                    .WithArguments(SR.InAttributeNotSupportedWithoutOutBlittableArray, paramName);
            var inAttributeNotSupportedOnPinnedParameter = new DiagnosticResult(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(0)
                    .WithArguments(SR.InAttributeOnlyNotSupportedOnPinnedParameters, paramName);
            yield return new object[] { ID(), codeSnippets.ByValueMarshallingOfType(inAttribute + constElementCount, "int[]", paramNameWithLocation), new DiagnosticResult[] {
                inAttributeNotSupportedOnPinnedParameter,
                //https://github.com/dotnet/runtime/issues/88540
                inAttributeNotSupportedOnPinnedParameter
            }};
            // new issue before merge: char generated code doesn't seem to work well with [In, Out]
            yield return new object[] {
                ID(),
                codeSnippets.ByValueMarshallingOfType(inAttribute + constElementCount, "char[]", paramNameWithLocation, (StringMarshalling.Utf16, null)),
                new DiagnosticResult[] { inAttributeNotSupportedOnPinnedParameter, inAttributeIsDefaultDiagnostic }
            };

            // bools that are marshalled into a new array are in by default
            yield return new object[] {
                ID(),
                codeSnippets.ByValueMarshallingOfType(
                    inAttribute + "[MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U1, SizeConst = 10)]",
                    "bool[]",
                    paramNameWithLocation,
                    (StringMarshalling.Utf16, null)),
                new DiagnosticResult[] { inAttributeIsDefaultDiagnostic, inAttributeIsDefaultDiagnostic}
            };
            // Overriding marshalling with a custom marshaller makes it not pinned
            yield return new object[] {
                ID(),
                codeSnippets.ByValueMarshallingOfType(inAttribute, "[MarshalUsing(typeof(IntMarshaller), ElementIndirectionDepth = 1), MarshalUsing(ConstantElementCount = 10)]int[]", paramNameWithLocation) + CodeSnippets.IntMarshaller,
                new DiagnosticResult[] { inAttributeIsDefaultDiagnostic, inAttributeIsDefaultDiagnostic}
            };

            // [In, Out] is default
            var inOutAttributeIsDefaultDiagnostic = new DiagnosticResult(GeneratorDiagnostics.UnnecessaryParameterMarshallingInfo)
                    .WithLocation(0)
                    .WithLocation(1)
                    .WithLocation(2)
                    .WithArguments(SR.InOutAttributes, paramName, SR.PinnedMarshallingIsInOutByDefault);
            yield return new object[] {
                ID(),
                codeSnippets.ByValueMarshallingOfType(inAttribute + outAttribute + constElementCount, "int[]", paramNameWithLocation),
                new DiagnosticResult[] { inOutAttributeIsDefaultDiagnostic, inOutAttributeIsDefaultDiagnostic}
            };
            yield return new object[] {
                ID(),
                codeSnippets.ByValueMarshallingOfType(inAttribute + outAttribute + constElementCount, "char[]", paramNameWithLocation, (StringMarshalling.Utf16, null)),
                //https://github.com/dotnet/runtime/issues/88540
                new DiagnosticResult[] { inOutAttributeIsDefaultDiagnostic }
            };

            // [Out] Should not warn
            // https://github.com/dotnet/runtime/issues/88708
            //yield return new object[] {
            //    ID(),
            //    codeSnippets.ByValueMarshallingOfType(outAttribute + constElementCount, "int[]", paramNameWithLocation),
            //    new DiagnosticResult[] { }
            //};

            // https://github.com/dotnet/runtime/issues/88708
            //yield return new object[] {
            //    ID(),
            //    codeSnippets.ByValueMarshallingOfType(outAttribute + constElementCount, "char[]", paramNameWithLocation, (StringMarshalling.Utf16, null)),
            //    new DiagnosticResult[] { }
            //};
        }

        [Theory]
        [MemberData(nameof(ByValueMarshalAttributeOnValueTypes))]
        [MemberData(nameof(ByValueMarshalAttributeOnReferenceTypes))]
        [MemberData(nameof(ByValueMarshalAttributeOnPinnedMarshalledTypes))]
        public async Task VerifyByValueMarshallingAttributeUsage(string id, string source, DiagnosticResult[] diagnostics)
        {
            _ = id;
            VerifyComInterfaceGenerator.Test test = new(referenceAncillaryInterop: false)
            {
                TestCode = source,
                TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
                // Our fallback mechanism for invalid code for unmanaged->managed stubs sometimes generates invalid code.
                CompilerDiagnostics = diagnostics.Length != 0 ? CompilerDiagnostics.None : CompilerDiagnostics.Errors,
            };
            test.ExpectedDiagnostics.AddRange(diagnostics);
            await test.RunAsync();
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
    }
}
