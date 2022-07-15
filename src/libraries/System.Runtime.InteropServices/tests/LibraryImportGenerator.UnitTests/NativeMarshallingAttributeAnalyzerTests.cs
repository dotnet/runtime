// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using static Microsoft.Interop.Analyzers.NativeMarshallingAttributeAnalyzer;

using VerifyCS = LibraryImportGenerator.UnitTests.Verifiers.CSharpAnalyzerVerifier<
    Microsoft.Interop.Analyzers.NativeMarshallingAttributeAnalyzer>;

namespace LibraryImportGenerator.UnitTests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/60650", TestRuntimes.Mono)]
    public class NativeMarshallingAttributeAnalyzerTests
    {
        [Fact]
        public async Task NativeMarshallingWithNullMarshallerType_ReportsDiagnostic()
        {
            string source = """
                using System.Runtime.InteropServices.Marshalling;

                [NativeMarshalling({|#0:null|})]
                class ManagedType {}
                """;

            await VerifyCS.VerifyAnalyzerAsync(source,
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

            await VerifyCS.VerifyAnalyzerAsync(source,
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

            await VerifyCS.VerifyAnalyzerAsync(source,
                VerifyCS.Diagnostic(MarshallerEntryPointTypeMustHaveCustomMarshallerAttributeWithMatchingManagedTypeRule).WithLocation(0).WithArguments("MarshallerType", "ManagedType"));
        }

        [Fact]
        public async Task NativeMarshallingWithMarshallerWithEntryPointAttributeForType_DoesNotReportDiagnostic()
        {
            string source = """
                using System.Runtime.InteropServices.Marshalling;
                
                [NativeMarshalling(typeof(MarshallerType))]
                class ManagedType {}
                
                [CustomMarshaller(typeof(ManagedType), MarshalMode.ManagedToUnmanagedIn, typeof(MarshallerType))]
                static class MarshallerType
                {
                    public static int ConvertToUnmanaged(ManagedType m) => default;
                }
                """;

            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [Fact]
        public async Task GenericTypeNativeMarshallingWithMarshallerWithEntryPointAttributeForType_DoesNotReportDiagnostic()
        {
            string source = """
                using System.Runtime.InteropServices.Marshalling;
                
                [NativeMarshalling(typeof(MarshallerType<>))]
                class ManagedType<T> {}
                
                [CustomMarshaller(typeof(ManagedType<>), MarshalMode.ManagedToUnmanagedIn, typeof(MarshallerType<>))]
                static class MarshallerType<T>
                {
                    public static int ConvertToUnmanaged(ManagedType<T> m) => default;
                }
                """;

            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [Fact]
        public async Task NestedGenericTypeNativeMarshallingWithMarshallerWithEntryPointAttributeForType_DoesNotReportDiagnostic()
        {
            string source = """
                using System.Runtime.InteropServices.Marshalling;

                class Container<T>
                {
                    [NativeMarshalling(typeof(MarshallerType<>))]
                    public class ManagedType {}
                }
                
                [CustomMarshaller(typeof(Container<>.ManagedType), MarshalMode.ManagedToUnmanagedIn, typeof(MarshallerType<>))]
                static class MarshallerType<T>
                {
                    public static int ConvertToUnmanaged(Container<T>.ManagedType m) => default;
                }
                """;

            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [Fact]
        public async Task NativeMarshallingWithMarshallerWithEntryPointAttributeForTypeAndOtherType_DoesNotReportDiagnostic()
        {
            string source = """
                using System.Runtime.InteropServices.Marshalling;
                
                [NativeMarshalling(typeof(MarshallerType))]
                class ManagedType {}
                
                [CustomMarshaller(typeof(int), MarshalMode.ManagedToUnmanagedIn, typeof(MarshallerType))]
                [CustomMarshaller(typeof(ManagedType), MarshalMode.ManagedToUnmanagedIn, typeof(MarshallerType))]
                static class MarshallerType
                {
                    public static int ConvertToUnmanaged(ManagedType m) => default;
                    public static int ConvertToUnmanaged(int m) => default;
                }
                """;

            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [Fact]
        public async Task NativeMarshallingWithMarshallerWithHigherArity_ReportsDiagnostic()
        {
            string source = """
                using System.Runtime.InteropServices.Marshalling;

                [NativeMarshalling(typeof({|#0:MarshallerType<,>|}))]
                class ManagedType<T> {}

                [CustomMarshaller(typeof(ManagedType<>), MarshalMode.ManagedToUnmanagedIn, typeof(MarshallerType<,>))]
                static class MarshallerType<U, V>
                {
                    public static int ConvertToUnmanaged(int i) => i;
                }
                """;

            await VerifyCS.VerifyAnalyzerAsync(source,
                VerifyCS.Diagnostic(GenericEntryPointMarshallerTypeMustBeClosedOrMatchArityRule).WithLocation(0).WithArguments("MarshallerType<U, V>", "ManagedType<T>"));
        }

        [Fact]
        public async Task NativeMarshallingWithMarshallerWithLowerArity_ReportsDiagnostic()
        {
            string source = """
                using System.Runtime.InteropServices.Marshalling;

                [NativeMarshalling(typeof({|#0:MarshallerType<>|}))]
                class ManagedType<T, U> {}

                [CustomMarshaller(typeof(ManagedType<,>), MarshalMode.ManagedToUnmanagedIn, typeof(MarshallerType<>))]
                static class MarshallerType<V>
                {
                    public static int ConvertToUnmanaged(int i) => i;
                }
                """;

            await VerifyCS.VerifyAnalyzerAsync(source,
                VerifyCS.Diagnostic(GenericEntryPointMarshallerTypeMustBeClosedOrMatchArityRule).WithLocation(0).WithArguments("MarshallerType<V>", "ManagedType<T, U>"));
        }

        [Fact]
        public async Task NativeMarshallingWithCollectionMarshallerWithOneHigherArity_DoesNotReportDiagnostic()
        {
            string source = """
                using System.Runtime.InteropServices.Marshalling;

                [NativeMarshalling(typeof(MarshallerType<,>))]
                class ManagedType<T> {}

                [CustomMarshaller(typeof(ManagedType<>), MarshalMode.ManagedToUnmanagedIn, typeof(MarshallerType<,>))]
                [ContiguousCollectionMarshaller]
                static unsafe class MarshallerType<U, V> where V : unmanaged
                {
                    public static V* AllocateContainerForUnmanagedElements(ManagedType<U> managed, out int numElements) { throw null; }
                    public static System.ReadOnlySpan<int> GetManagedValuesSource(ManagedType<U> managed) { throw null; }
                    public static System.Span<V> GetUnmanagedValuesDestination(V* unmanaged, int numElements) { throw null; }
                }
                """;

            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [Fact]
        public async Task NativeMarshallingWithCollectionMarshallerWithSameArity_ReportsDiagnostic()
        {
            string source = """
                using System.Runtime.InteropServices.Marshalling;

                [NativeMarshalling(typeof({|#0:MarshallerType<>|}))]
                class ManagedType<T> {}

                [CustomMarshaller(typeof(ManagedType<>), MarshalMode.ManagedToUnmanagedIn, typeof(MarshallerType<>))]
                [ContiguousCollectionMarshaller]
                static unsafe class MarshallerType<U>
                {
                    public static int* AllocateContainerForUnmanagedElements(ManagedType<U> managed, out int numElements) { throw null; }
                    public static System.ReadOnlySpan<int> GetManagedValuesSource(ManagedType<U> managed) { throw null; }
                    public static System.Span<int> GetUnmanagedValuesDestination(int* unmanaged, int numElements) { throw null; }
                }
                """;

            await VerifyCS.VerifyAnalyzerAsync(source,
                VerifyCS.Diagnostic(GenericEntryPointMarshallerTypeMustBeClosedOrMatchArityRule).WithLocation(0).WithArguments("MarshallerType<U>", "ManagedType<T>"));
        }

        [Fact]
        public async Task NativeMarshallingWithCollectionMarshallerWithMoreArity_ReportsDiagnostic()
        {
            string source = """
                using System.Runtime.InteropServices.Marshalling;

                [NativeMarshalling(typeof({|#0:MarshallerType<,,>|}))]
                class ManagedType<T> {}

                [CustomMarshaller(typeof(ManagedType<>), MarshalMode.ManagedToUnmanagedIn, typeof(MarshallerType<,,>))]
                [ContiguousCollectionMarshaller]
                static unsafe class MarshallerType<U, V, W> where W : unmanaged
                {
                    public static W* AllocateContainerForUnmanagedElements(ManagedType<U> managed, out int numElements) { throw null; }
                    public static System.ReadOnlySpan<int> GetManagedValuesSource(ManagedType<U> managed) { throw null; }
                    public static System.Span<W> GetUnmanagedValuesDestination(W* unmanaged, int numElements) { throw null; }
                }
                """;

            await VerifyCS.VerifyAnalyzerAsync(source,
                VerifyCS.Diagnostic(GenericEntryPointMarshallerTypeMustBeClosedOrMatchArityRule).WithLocation(0).WithArguments("MarshallerType<U, V, W>", "ManagedType<T>"));
        }
    }
}
