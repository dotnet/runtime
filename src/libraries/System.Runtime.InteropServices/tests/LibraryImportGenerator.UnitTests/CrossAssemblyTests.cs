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