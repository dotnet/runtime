// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.Interop;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using static Microsoft.Interop.Analyzers.CustomMarshallerAttributeAnalyzer;

using VerifyCS = LibraryImportGenerator.UnitTests.Verifiers.CSharpCodeFixVerifier<
    Microsoft.Interop.Analyzers.CustomMarshallerAttributeAnalyzer,
    Microsoft.Interop.Analyzers.CustomMarshallerAttributeFixer>;

namespace LibraryImportGenerator.UnitTests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/60650", TestRuntimes.Mono)]
    public class CustomMarshallerAttributeAnalyzerTests_StatefulValueShapeValidation
    {
        [Fact]
        public async Task ModeThatUsesManagedToUnmanagedShape_Missing_AllMethods_ReportsDiagnostic()
        {
            string source = """
                using System.Runtime.InteropServices.Marshalling;
                
                class ManagedType {}
                
                [CustomMarshaller(typeof(ManagedType), MarshalMode.ManagedToUnmanagedIn, typeof({|#0:MarshallerType|}))]
                [CustomMarshaller(typeof(ManagedType), MarshalMode.UnmanagedToManagedOut, typeof({|#1:MarshallerType|}))]
                struct MarshallerType
                {
                }
                """;

            await VerifyCS.VerifyAnalyzerAsync(
                source,
                VerifyCS.Diagnostic(StatefulMarshallerRequiresFromManagedRule).WithLocation(0).WithArguments("MarshallerType", MarshalMode.ManagedToUnmanagedIn, "ManagedType"),
                VerifyCS.Diagnostic(StatefulMarshallerRequiresFromManagedRule).WithLocation(1).WithArguments("MarshallerType", MarshalMode.UnmanagedToManagedOut, "ManagedType"),
                VerifyCS.Diagnostic(StatefulMarshallerRequiresToUnmanagedRule).WithLocation(0).WithArguments("MarshallerType", MarshalMode.ManagedToUnmanagedIn, "ManagedType"),
                VerifyCS.Diagnostic(StatefulMarshallerRequiresToUnmanagedRule).WithLocation(1).WithArguments("MarshallerType", MarshalMode.UnmanagedToManagedOut, "ManagedType"),
                VerifyCS.Diagnostic(StatefulMarshallerRequiresFreeRule).WithLocation(0).WithArguments("MarshallerType"),
                VerifyCS.Diagnostic(StatefulMarshallerRequiresFreeRule).WithLocation(1).WithArguments("MarshallerType"));
        }

        [Fact]
        public async Task ModeThatUsesUnmanagedToManagedShape_Missing_AllMethods_ReportsDiagnostic()
        {
            string source = """
                using System.Runtime.InteropServices.Marshalling;
                
                class ManagedType {}
                
                [CustomMarshaller(typeof(ManagedType), MarshalMode.ManagedToUnmanagedOut, typeof({|#0:MarshallerType|}))]
                [CustomMarshaller(typeof(ManagedType), MarshalMode.UnmanagedToManagedIn, typeof({|#1:MarshallerType|}))]
                struct MarshallerType
                {
                }
                """;

            await VerifyCS.VerifyAnalyzerAsync(
                source,
                VerifyCS.Diagnostic(StatefulMarshallerRequiresFromUnmanagedRule).WithLocation(0).WithArguments("MarshallerType", MarshalMode.ManagedToUnmanagedOut, "ManagedType"),
                VerifyCS.Diagnostic(StatefulMarshallerRequiresFromUnmanagedRule).WithLocation(1).WithArguments("MarshallerType", MarshalMode.UnmanagedToManagedIn, "ManagedType"),
                VerifyCS.Diagnostic(StatefulMarshallerRequiresToManagedRule).WithLocation(0).WithArguments("MarshallerType", MarshalMode.ManagedToUnmanagedOut, "ManagedType"),
                VerifyCS.Diagnostic(StatefulMarshallerRequiresToManagedRule).WithLocation(1).WithArguments("MarshallerType", MarshalMode.UnmanagedToManagedIn, "ManagedType"),
                VerifyCS.Diagnostic(StatefulMarshallerRequiresFreeRule).WithLocation(0).WithArguments("MarshallerType"),
                VerifyCS.Diagnostic(StatefulMarshallerRequiresFreeRule).WithLocation(1).WithArguments("MarshallerType"));
        }

        [Fact]
        public async Task Overloaded_FromUnmanaged_ReportsDiagnostic()
        {
            string source = """
                using System.Runtime.InteropServices.Marshalling;
                
                class ManagedType {}
                
                [CustomMarshaller(typeof(ManagedType), MarshalMode.ManagedToUnmanagedOut, typeof({|#0:MarshallerType|}))]
                struct MarshallerType
                {
                    public ManagedType ToManaged() => default;
                    public void FromUnmanaged(int i) {}
                    public void FromUnmanaged(float f) {}
                    public void Free() {}
                }
                """;

            await VerifyCS.VerifyAnalyzerAsync(
                source,
                VerifyCS.Diagnostic(FromUnmanagedOverloadsNotSupportedRule).WithLocation(0).WithArguments("MarshallerType"));
        }

        [Fact]
        public async Task ModeThatUsesBidirectionalShape_Missing_AllMethods_ReportsDiagnostic()
        {
            string source = """
                using System.Runtime.InteropServices.Marshalling;
                
                class ManagedType {}
                
                [CustomMarshaller(typeof(ManagedType), MarshalMode.ManagedToUnmanagedRef, typeof({|#0:MarshallerType|}))]
                [CustomMarshaller(typeof(ManagedType), MarshalMode.UnmanagedToManagedRef, typeof({|#1:MarshallerType|}))]
                struct MarshallerType
                {
                }
                """;

            await VerifyCS.VerifyAnalyzerAsync(
                source,
                VerifyCS.Diagnostic(StatefulMarshallerRequiresFreeRule).WithLocation(0).WithArguments("MarshallerType"),
                VerifyCS.Diagnostic(StatefulMarshallerRequiresFromManagedRule).WithLocation(0).WithArguments("MarshallerType", MarshalMode.ManagedToUnmanagedRef, "ManagedType"),
                VerifyCS.Diagnostic(StatefulMarshallerRequiresFromManagedRule).WithLocation(1).WithArguments("MarshallerType", MarshalMode.UnmanagedToManagedRef, "ManagedType"),
                VerifyCS.Diagnostic(StatefulMarshallerRequiresToUnmanagedRule).WithLocation(0).WithArguments("MarshallerType", MarshalMode.ManagedToUnmanagedRef, "ManagedType"),
                VerifyCS.Diagnostic(StatefulMarshallerRequiresToUnmanagedRule).WithLocation(1).WithArguments("MarshallerType", MarshalMode.UnmanagedToManagedRef, "ManagedType"),
                VerifyCS.Diagnostic(StatefulMarshallerRequiresFromUnmanagedRule).WithLocation(0).WithArguments("MarshallerType", MarshalMode.ManagedToUnmanagedRef, "ManagedType"),
                VerifyCS.Diagnostic(StatefulMarshallerRequiresFromUnmanagedRule).WithLocation(1).WithArguments("MarshallerType", MarshalMode.UnmanagedToManagedRef, "ManagedType"),
                VerifyCS.Diagnostic(StatefulMarshallerRequiresToManagedRule).WithLocation(0).WithArguments("MarshallerType", MarshalMode.ManagedToUnmanagedRef, "ManagedType"),
                VerifyCS.Diagnostic(StatefulMarshallerRequiresToManagedRule).WithLocation(1).WithArguments("MarshallerType", MarshalMode.UnmanagedToManagedRef, "ManagedType"),
                VerifyCS.Diagnostic(StatefulMarshallerRequiresFreeRule).WithLocation(1).WithArguments("MarshallerType"));
        }

        [Fact]
        public async Task ModeThatUsesBidirectionalShape_MismatchedUnmanagedTypes_ReportsDiagnostic()
        {
            string source = """
                using System.Runtime.InteropServices.Marshalling;
                
                class ManagedType {}
                
                [CustomMarshaller(typeof(ManagedType), MarshalMode.ManagedToUnmanagedRef, typeof({|#0:MarshallerType|}))]
                [CustomMarshaller(typeof(ManagedType), MarshalMode.UnmanagedToManagedRef, typeof({|#1:MarshallerType|}))]
                struct MarshallerType
                {
                    public void FromManaged(ManagedType t) {}
                    public int ToUnmanaged() => default;
                    public void FromUnmanaged(float f) {}
                    public ManagedType ToManaged() => default;
                    public void Free() {}
                }
                """;

            await VerifyCS.VerifyAnalyzerAsync(
                source,
                VerifyCS.Diagnostic(FirstParameterMustMatchReturnTypeRule).WithLocation(0).WithArguments("MarshallerType.FromUnmanaged(float)", "MarshallerType.ToUnmanaged()"),
                VerifyCS.Diagnostic(FirstParameterMustMatchReturnTypeRule).WithLocation(1).WithArguments("MarshallerType.FromUnmanaged(float)", "MarshallerType.ToUnmanaged()"));
        }

        [Fact]
        public async Task ModeThatUsesBidirectionalShape_DoesNotReportDiagnostic()
        {
            string source = """
                using System.Runtime.InteropServices.Marshalling;
                
                class ManagedType {}
                
                [CustomMarshaller(typeof(ManagedType), MarshalMode.ManagedToUnmanagedRef, typeof(MarshallerType))]
                [CustomMarshaller(typeof(ManagedType), MarshalMode.UnmanagedToManagedRef, typeof(MarshallerType))]
                struct MarshallerType
                {
                    public void FromManaged(ManagedType t) {}
                    public int ToUnmanaged() => default;
                    public void FromUnmanaged(int f) {}
                    public ManagedType ToManaged() => default;
                    public void Free() {}
                }
                """;

            await VerifyCS.VerifyAnalyzerAsync(
                source);
        }

        [Fact]
        public async Task ModeThatUsesElementMode_ReportsDiagnostic()
        {
            string source = """
                using System.Runtime.InteropServices.Marshalling;
                
                class ManagedType {}
                
                [CustomMarshaller(typeof(ManagedType), MarshalMode.ElementIn, typeof({|#0:MarshallerType|}))]
                [CustomMarshaller(typeof(ManagedType), MarshalMode.ElementRef, typeof({|#1:MarshallerType|}))]
                [CustomMarshaller(typeof(ManagedType), MarshalMode.ElementOut, typeof({|#2:MarshallerType|}))]
                struct MarshallerType
                {
                    public void FromManaged(ManagedType t) {}
                    public int ToUnmanaged() => default;
                    public void FromUnmanaged(int f) {}
                    public ManagedType ToManaged() => default;
                    public void Free() {}
                }
                """;

            await VerifyCS.VerifyAnalyzerAsync(
                source,
                VerifyCS.Diagnostic(ElementMarshallerCannotBeStatefulRule).WithLocation(0).WithArguments("MarshallerType", MarshalMode.ElementIn),
                VerifyCS.Diagnostic(ElementMarshallerCannotBeStatefulRule).WithLocation(1).WithArguments("MarshallerType", MarshalMode.ElementRef),
                VerifyCS.Diagnostic(ElementMarshallerCannotBeStatefulRule).WithLocation(2).WithArguments("MarshallerType", MarshalMode.ElementOut));
        }

        [Fact]
        public async Task DefaultMode_Missing_AllMethods_ReportsDiagnostic()
        {
            string source = """
                using System.Runtime.InteropServices.Marshalling;
                
                class ManagedType {}
                
                [CustomMarshaller(typeof(ManagedType), MarshalMode.Default, typeof({|#0:MarshallerType|}))]
                struct MarshallerType
                {
                }
                """;

            await VerifyCS.VerifyAnalyzerAsync(
                source,
                VerifyCS.Diagnostic(StatefulMarshallerRequiresFreeRule).WithLocation(0).WithArguments("MarshallerType"),
                VerifyCS.Diagnostic(StatefulMarshallerRequiresFromManagedRule).WithSeverity(DiagnosticSeverity.Info).WithLocation(0).WithArguments("MarshallerType", MarshalMode.Default, "ManagedType"),
                VerifyCS.Diagnostic(StatefulMarshallerRequiresToUnmanagedRule).WithSeverity(DiagnosticSeverity.Info).WithLocation(0).WithArguments("MarshallerType", MarshalMode.Default, "ManagedType"),
                VerifyCS.Diagnostic(StatefulMarshallerRequiresFromUnmanagedRule).WithSeverity(DiagnosticSeverity.Info).WithLocation(0).WithArguments("MarshallerType", MarshalMode.Default, "ManagedType"),
                VerifyCS.Diagnostic(StatefulMarshallerRequiresToManagedRule).WithSeverity(DiagnosticSeverity.Info).WithLocation(0).WithArguments("MarshallerType", MarshalMode.Default, "ManagedType"));
        }

        [Fact]
        public async Task CallerAllocatedBuffer_NoBufferSize_ReportsDiagnostic()
        {
            string source = """
                using System;
                using System.Runtime.InteropServices.Marshalling;
                
                class ManagedType {}
                
                [CustomMarshaller(typeof(ManagedType), MarshalMode.ManagedToUnmanagedIn, typeof({|#0:MarshallerType|}))]
                struct MarshallerType
                {
                    public void FromManaged(ManagedType m, Span<byte> b) {}

                    public int ToUnmanaged() => default;

                    public void Free() {}
                }
                """;

            await VerifyCS.VerifyAnalyzerAsync(
                source,
                VerifyCS.Diagnostic(CallerAllocFromManagedMustHaveBufferSizeRule).WithLocation(0).WithArguments("MarshallerType", "byte"));
        }
    }
}
