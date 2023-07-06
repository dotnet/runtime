// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using System.Collections.Generic;
using System.Runtime.InteropServices.Marshalling;
using System.Threading.Tasks;
using Xunit;
using static Microsoft.Interop.Analyzers.CustomMarshallerAttributeAnalyzer;

using VerifyCS = Microsoft.Interop.UnitTests.Verifiers.CSharpCodeFixVerifier<
    Microsoft.Interop.Analyzers.CustomMarshallerAttributeAnalyzer,
    Microsoft.Interop.Analyzers.CustomMarshallerAttributeFixer>;

namespace LibraryImportGenerator.UnitTests
{
    public class CustomMarshallerAttributeAnalyzerTests_AttributeUsage
    {
        [Fact]
        public async Task NullManagedType_ReportsDiagnostic()
        {
            string source = """
                using System.Runtime.InteropServices.Marshalling;

                [CustomMarshaller({|#0:null|}, MarshalMode.Default, typeof(MarshallerType))]
                static class MarshallerType {}
                """;

            await VerifyCS.VerifyAnalyzerAsync(source,
                VerifyCS.Diagnostic(ManagedTypeMustBeNonNullRule).WithLocation(0).WithArguments("MarshallerType"));
        }
        [Fact]
        public async Task UsingMarshalModeAsFlags_ReportsDiagnostic()
        {
            string source = """
                    using System.Collections.Generic;
                    using System.Runtime.InteropServices;
                    using System.Runtime.InteropServices.Marshalling;
                    [CustomMarshaller(typeof(int), {|#0:MarshalMode.ElementIn | MarshalMode.ElementOut | MarshalMode.Default|}, typeof(MyMarshaller))]
                    public static class MyMarshaller
                    {

                    }
            """;

            await VerifyCS.VerifyAnalyzerAsync(source,
                VerifyCS.Diagnostic(MarshalModeMustBeValidValue).WithLocation(0));
        }

        [Fact]
        public async Task UsingInvalidMarshalMode_ReportsDiagnostic()
        {
            string source = """
                    using System.Collections.Generic;
                    using System.Runtime.InteropServices;
                    using System.Runtime.InteropServices.Marshalling;
                    [CustomMarshaller(typeof(int), {|#0:(MarshalMode)10|}, typeof(MyMarshaller))]
                    public static class MyMarshaller
                    {

                    }
            """;

            await VerifyCS.VerifyAnalyzerAsync(source,
                VerifyCS.Diagnostic(MarshalModeMustBeValidValue).WithLocation(0));
        }

        [Fact]
        public async Task MarshallerWithEntryPointAttributeForType_DoesNotReportDiagnostic()
        {
            string source = """
                using System.Runtime.InteropServices.Marshalling;

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
        public async Task GenericTypeWithMarshallerWithEntryPointAttributeForType_DoesNotReportDiagnostic()
        {
            string source = """
                using System.Runtime.InteropServices.Marshalling;

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
        public async Task NestedGenericTypeWithMarshallerWithEntryPointAttributeForType_DoesNotReportDiagnostic()
        {
            string source = """
                using System.Runtime.InteropServices.Marshalling;

                class Container<T>
                {
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
        public async Task ManagedTypeWithMarshallerWithHigherArity_ReportsDiagnostic()
        {
            string source = """
                using System.Runtime.InteropServices.Marshalling;

                class ManagedType<T> {}

                [CustomMarshaller(typeof({|#0:ManagedType<>|}), MarshalMode.ManagedToUnmanagedIn, typeof(MarshallerType<,>))]
                static class MarshallerType<U, V>
                {
                    public static int ConvertToUnmanaged(int i) => i;
                }
                """;

            await VerifyCS.VerifyAnalyzerAsync(source,
                VerifyCS.Diagnostic(ManagedTypeMustBeClosedOrMatchArityRule).WithLocation(0).WithArguments("ManagedType<T>", "MarshallerType<U, V>"));
        }

        [Fact]
        public async Task ManagedTypeWithMarshallerWithLowerArity_ReportsDiagnostic()
        {
            string source = """
                using System.Runtime.InteropServices.Marshalling;

                class ManagedType<T, U> {}

                [CustomMarshaller(typeof({|#0:ManagedType<,>|}), MarshalMode.ManagedToUnmanagedIn, typeof(MarshallerType<>))]
                static class MarshallerType<V>
                {
                    public static int ConvertToUnmanaged(int i) => i;
                }
                """;

            await VerifyCS.VerifyAnalyzerAsync(source,
                VerifyCS.Diagnostic(ManagedTypeMustBeClosedOrMatchArityRule).WithLocation(0).WithArguments("ManagedType<T, U>", "MarshallerType<V>"));
        }

        [Fact]
        public async Task ManagedTypeWithCollectionMarshallerWithOneHigherArity_DoesNotReportDiagnostic()
        {
            string source = """
                using System.Runtime.InteropServices.Marshalling;

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
        public async Task ManagedTypeWithCollectionMarshallerWithSameArity_ReportsDiagnostic()
        {
            string source = """
                using System.Runtime.InteropServices.Marshalling;

                class ManagedType<T> {}

                [CustomMarshaller(typeof({|#0:ManagedType<>|}), MarshalMode.ManagedToUnmanagedIn, typeof(MarshallerType<>))]
                [ContiguousCollectionMarshaller]
                static unsafe class MarshallerType<U>
                {
                    public static int* AllocateContainerForUnmanagedElements(ManagedType<U> managed, out int numElements) { throw null; }
                    public static System.ReadOnlySpan<int> GetManagedValuesSource(ManagedType<U> managed) { throw null; }
                    public static System.Span<int> GetUnmanagedValuesDestination(int* unmanaged, int numElements) { throw null; }
                }
                """;

            await VerifyCS.VerifyAnalyzerAsync(source,
                VerifyCS.Diagnostic(ManagedTypeMustBeClosedOrMatchArityRule).WithLocation(0).WithArguments("ManagedType<T>", "MarshallerType<U>"));
        }

        [Fact]
        public async Task ManagedTypeWithCollectionMarshallerWithMoreArity_ReportsDiagnostic()
        {
            string source = """
                using System.Runtime.InteropServices.Marshalling;

                class ManagedType<T> {}

                [CustomMarshaller(typeof({|#0:ManagedType<>|}), MarshalMode.ManagedToUnmanagedIn, typeof(MarshallerType<,,>))]
                [ContiguousCollectionMarshaller]
                static unsafe class MarshallerType<U, V, W> where W : unmanaged
                {
                    public static W* AllocateContainerForUnmanagedElements(ManagedType<U> managed, out int numElements) { throw null; }
                    public static System.ReadOnlySpan<int> GetManagedValuesSource(ManagedType<U> managed) { throw null; }
                    public static System.Span<W> GetUnmanagedValuesDestination(W* unmanaged, int numElements) { throw null; }
                }
                """;

            await VerifyCS.VerifyAnalyzerAsync(source,
                VerifyCS.Diagnostic(ManagedTypeMustBeClosedOrMatchArityRule).WithLocation(0).WithArguments("ManagedType<T>", "MarshallerType<U, V, W>"));
        }

        [Fact]
        public async Task NonGenericManagedTypeWithMarshallerEntryPointMatchingArity_DoesNotReportDiagnostic()
        {
            string source = """
                using System.Runtime.InteropServices.Marshalling;

                class ManagedType {}

                [CustomMarshaller(typeof(ManagedType), MarshalMode.ManagedToUnmanagedIn, typeof(MarshallerType<>))]
                static class MarshallerType<V>
                {
                    public static int ConvertToUnmanaged(ManagedType i) => 0;
                }
                """;

            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [Fact]
        public async Task NonGenericManagedTypeNestedInGenericWithMarshallerEntryPointMatchingArity_ReportsDiagnostic()
        {
            string source = """
                using System.Runtime.InteropServices.Marshalling;

                class Container<T, U>
                {
                    public class ManagedType {}
                }

                [CustomMarshaller(typeof({|#0:Container<,>.ManagedType|}), MarshalMode.ManagedToUnmanagedIn, typeof(MarshallerType<>))]
                static class MarshallerType<V>
                {
                    public static int ConvertToUnmanaged(int i) => 0;
                }
                """;

            await VerifyCS.VerifyAnalyzerAsync(source,
                VerifyCS.Diagnostic(ManagedTypeMustBeClosedOrMatchArityRule).WithLocation(0).WithArguments("Container<T, U>.ManagedType", "MarshallerType<V>"));
        }

        [Fact]
        public async Task PlaceholderManagedTypeWithMarshallerEntryPointMatchingArity_DoesNotReportDiagnostic()
        {
            string source = """
                using System.Runtime.InteropServices.Marshalling;

                class ManagedType {}

                [CustomMarshaller(typeof(CustomMarshallerAttribute.GenericPlaceholder), MarshalMode.ManagedToUnmanagedIn, typeof(MarshallerType<>))]
                static class MarshallerType<V>
                {
                    public static int ConvertToUnmanaged(V i) => 0;
                }
                """;

            await VerifyCS.VerifyAnalyzerAsync(source);
        }
    }
}
