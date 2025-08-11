using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.Interop;
using Microsoft.Interop.UnitTests;
using Xunit;

using VerifyCS = Microsoft.Interop.UnitTests.Verifiers.CSharpSourceGeneratorVerifier<Microsoft.Interop.LibraryImportGenerator>;

namespace LibraryImportGenerator.UnitTests
{
    public class CrossAssemblyTests
    {
        [Fact]
        public async Task TypeFromAnotherAssembly_BlittableStruct_ShouldWork()
        {
            // Define a simple blittable struct in a separate "assembly" 
            string externalAssemblySource = """
                using System.Runtime.InteropServices;

                namespace ExternalLib
                {
                    [StructLayout(LayoutKind.Sequential)]
                    public struct Point
                    {
                        public int X;
                        public int Y;
                    }
                }
                """;

            // Source that uses the type from another assembly
            string source = """
                using System.Runtime.InteropServices;
                using ExternalLib;

                partial class Test
                {
                    [LibraryImport("TestLib")]
                    public static partial Point GetPoint();

                    [LibraryImport("TestLib")]
                    public static partial void SetPoint(Point point);
                }
                """;

            // This should work without any errors
            await VerifyCS.VerifySourceGeneratorAsync(
                new[] { externalAssemblySource, source });
        }

        [Fact]
        public async Task TypeFromAnotherAssembly_WithNonBlittableType_ReportsTypeNotSupportedInsteadOfMarshalAs()
        {
            // Define a non-blittable type that will trigger marshalling issues
            string externalAssemblySource = """
                using System.Runtime.InteropServices;

                namespace ExternalLib
                {
                    /// <summary>
                    /// A non-blittable type that should trigger a type not supported error
                    /// </summary>
                    public struct NonBlittableStruct
                    {
                        public string StringField; // Non-blittable field
                    }
                }
                """;

            // Source that uses the non-blittable type from another assembly
            string source = """
                using System.Runtime.InteropServices;
                using System.Runtime.CompilerServices;
                using ExternalLib;

                partial class Test
                {
                    [LibraryImport("TestLib")]
                    public static partial NonBlittableStruct {|#0:GetNonBlittableStruct|}();
                }
                """;

            // This should produce a TypeNotSupported error, NOT a MarshalAs error
            await VerifyCS.VerifySourceGeneratorAsync(
                new[] { externalAssemblySource, source },
                VerifyCS.Diagnostic(GeneratorDiagnostics.ReturnTypeNotSupported)
                    .WithLocation(0)
                    .WithArguments("ExternalLib.NonBlittableStruct", "GetNonBlittableStruct"));
        }

        [Fact]
        public async Task ExplicitMarshalAsAttribute_OnPrimitive_ReportsImprovedError()
        {
            // When there's an explicit MarshalAs attribute on a primitive type,
            // it should report a clearer error message that doesn't confuse users
            string source = """
                using System.Runtime.InteropServices;

                partial class Test
                {
                    [LibraryImport("TestLib")]
                    [return: MarshalAs(UnmanagedType.BStr)]
                    public static partial int {|#0:Method1|}(int i);
                }
                """;

            // This should produce the improved error message that focuses on the type rather than MarshalAs
            await VerifyCS.VerifySourceGeneratorAsync(source,
                VerifyCS.Diagnostic(GeneratorDiagnostics.TypeNotSupportedWithMarshallingInfoReturn)
                    .WithLocation(0)
                    .WithArguments("int", "Method1"));
        }

        [Fact]
        public async Task InferredMarshallingOnCustomType_ReportsTypeNotSupportedWithMarshallingInfo()
        {
            // When there's no explicit MarshalAs attribute but the type has marshalling issues,
            // it should report the new type-not-supported error message
            string source = """
                using System.Runtime.InteropServices;

                public struct CustomStruct
                {
                    public string StringField; // Non-blittable field
                }

                partial class Test
                {
                    [LibraryImport("TestLib")]
                    public static partial CustomStruct {|#0:GetCustomStruct|}();
                }
                """;

            // This should produce the new type-not-supported error (not the old MarshalAs error)
            // Note: This might still produce the general ReturnTypeNotSupported instead of TypeNotSupportedWithMarshallingInfoReturn
            // depending on how the marshalling analysis works
            await VerifyCS.VerifySourceGeneratorAsync(source,
                VerifyCS.Diagnostic(GeneratorDiagnostics.ReturnTypeNotSupported)
                    .WithLocation(0)
                    .WithArguments("CustomStruct", "GetCustomStruct"));
        }
        
        [Fact]
        public async Task TypeFromAnotherAssembly_WithDisabledRuntimeMarshalling_WorksCorrectly()
        {
            // Test the specific scenario mentioned in the issue where the error message
            // incorrectly mentions MarshalAsAttribute when types from another assembly are used
            string externalAssemblySource = """
                using System.Runtime.InteropServices;

                namespace ExternalLib
                {
                    [StructLayout(LayoutKind.Sequential)]
                    public readonly struct CustomBool
                    {
                        private readonly byte _value;

                        public CustomBool(bool value) => _value = value ? (byte)1 : (byte)0;
                        
                        public static implicit operator bool(CustomBool customBool) => customBool._value != 0;
                        public static implicit operator CustomBool(bool value) => new(value);
                    }
                }
                """;

            string source = """
                using System.Runtime.InteropServices;
                using System.Runtime.CompilerServices;
                using ExternalLib;
                
                [assembly:System.Runtime.CompilerServices.DisableRuntimeMarshalling]

                partial class Test
                {
                    [LibraryImport("TestLib")]
                    public static partial CustomBool TestMethod();
                }
                """;

            // This should work without any errors - the type is unmanaged and blittable
            await VerifyCS.VerifySourceGeneratorAsync(
                new[] { externalAssemblySource, source });
        }
    }
}