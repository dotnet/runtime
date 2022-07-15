// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using static Microsoft.Interop.Analyzers.CustomTypeMarshallerAnalyzer;

using VerifyCS = LibraryImportGenerator.UnitTests.Verifiers.CSharpCodeFixVerifier<
    Microsoft.Interop.Analyzers.CustomTypeMarshallerAnalyzer,
    Microsoft.Interop.Analyzers.CustomTypeMarshallerFixer>;

namespace LibraryImportGenerator.UnitTests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/60650", TestRuntimes.Mono)]
    public class CustomTypeMarshallerFixerTests
    {
        [Fact]
        public async Task NativeMarshallingWithNullMarshallerType_ReportsDiagnostic()
        {
            string source = """
                using System.Runtime.InteropServices.Marshalling;

                [NativeMarshalling({|#0:null|})]
                class ManagedType {}
                """;

            await VerifyCS.VerifyCodeFixAsync(source, source,
                VerifyCS.Diagnostic(MarshallerEntryPointTypeMustBeNonNullRule).WithLocation(0).WithArguments("ManagedType"));
        }

        [Fact]
        public async Task NativeMarshallingWithMarshallerWithNoEntryPointAttributes_ReportsDiagnostic()
        {
            string source = """
                using System.Runtime.InteropServices.Marshalling;

                [NativeMarshalling(typeof({|#0:MarshallerType|}))]
                class ManagedType {}

                static class MarshallerType
                {
                }
                """;

            await VerifyCS.VerifyCodeFixAsync(source, source,
                VerifyCS.Diagnostic(MarshallerEntryPointTypeMustHaveCustomMarshallerAttributeWithMatchingManagedTypeRule).WithLocation(0).WithArguments("MarshallerType", "ManagedType"));
        }

        [Fact]
        public async Task NativeMarshallingWithMarshallerWithOnlyEntryPointAttributesForAnotherType_ReportsDiagnostic()
        {
            string source = """
                using System.Runtime.InteropServices.Marshalling;

                [NativeMarshalling(typeof({|#0:MarshallerType|}))]
                class ManagedType {}

                [CustomMarshaller(typeof(int), MarshalMode.ManagedToUnmanagedIn, typeof(MarshallerType))]
                static class MarshallerType
                {
                    public static int ConvertToUnmanaged(int i) => i;
                }
                """;

            await VerifyCS.VerifyCodeFixAsync(source, source,
                VerifyCS.Diagnostic(MarshallerEntryPointTypeMustHaveCustomMarshallerAttributeWithMatchingManagedTypeRule).WithLocation(0).WithArguments("MarshallerType", "ManagedType"));
        }

        [Fact]
        public async Task NativeMarshallingWithMarshallerWithEntryPointAttributeForType_DoesNotReportDiagnostic()
        {
            string source = """
                using System.Runtime.InteropServices.Marshalling;
                
                [NativeMarshalling(typeof({|#0:MarshallerType|}))]
                class ManagedType {}
                
                [CustomMarshaller(typeof(ManagedType), MarshalMode.ManagedToUnmanagedIn, typeof(MarshallerType))]
                static class MarshallerType
                {
                    public static int ConvertToUnmanaged(ManagedType m) => default;
                }
                """;


            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact]
        public async Task GenericTypeNativeMarshallingWithMarshallerWithEntryPointAttributeForType_DoesNotReportDiagnostic()
        {
            string source = """
                using System.Runtime.InteropServices.Marshalling;
                
                [NativeMarshalling(typeof({|#0:MarshallerType<>|}))]
                class ManagedType<T> {}
                
                [CustomMarshaller(typeof(ManagedType<>), MarshalMode.ManagedToUnmanagedIn, typeof(MarshallerType<>))]
                static class MarshallerType<T>
                {
                    public static int ConvertToUnmanaged(ManagedType<T> m) => default;
                }
                """;

            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact]
        public async Task NestedGenericTypeNativeMarshallingWithMarshallerWithEntryPointAttributeForType_DoesNotReportDiagnostic()
        {
            string source = """
                using System.Runtime.InteropServices.Marshalling;

                class Container<T>
                {
                    [NativeMarshalling(typeof({|#0:MarshallerType<>|}))]
                    public class ManagedType {}
                }
                
                [CustomMarshaller(typeof(Container<>.ManagedType), MarshalMode.ManagedToUnmanagedIn, typeof(MarshallerType<>))]
                static class MarshallerType<T>
                {
                    public static int ConvertToUnmanaged(Container<T>.ManagedType m) => default;
                }
                """;


            await VerifyCS.VerifyCodeFixAsync(source, source);
        }

        [Fact]
        public async Task NativeMarshallingWithMarshallerWithEntryPointAttributeForTypeAndOtherType_DoesNotReportDiagnostic()
        {
            string source = """
                using System.Runtime.InteropServices.Marshalling;
                
                [NativeMarshalling(typeof({|#0:MarshallerType|}))]
                class ManagedType {}
                
                [CustomMarshaller(typeof(int), MarshalMode.ManagedToUnmanagedIn, typeof(MarshallerType))]
                [CustomMarshaller(typeof(ManagedType), MarshalMode.ManagedToUnmanagedIn, typeof(MarshallerType))]
                static class MarshallerType
                {
                    public static int ConvertToUnmanaged(ManagedType m) => default;
                    public static int ConvertToUnmanaged(int m) => default;
                }
                """;


            await VerifyCS.VerifyCodeFixAsync(source, source);
        }
    }
}
