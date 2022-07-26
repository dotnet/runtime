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
    public class CustomMarshallerAttributeAnalyzerTests_StatelessValueShapeValidation
    {
        [Fact]
        public async Task ModeThatUsesManagedToUnmanagedShape_Missing_ConvertToUnmanagedMethod_ReportsDiagnostic()
        {
            string source = """
                using System.Runtime.InteropServices.Marshalling;
                
                class ManagedType {}
                
                [CustomMarshaller(typeof(ManagedType), MarshalMode.ManagedToUnmanagedIn, typeof({|#0:MarshallerType|}))]
                [CustomMarshaller(typeof(ManagedType), MarshalMode.UnmanagedToManagedOut, typeof({|#1:MarshallerType|}))]
                [CustomMarshaller(typeof(ManagedType), MarshalMode.ElementIn, typeof({|#2:MarshallerType|}))]
                static class MarshallerType
                {
                }
                """;

            await VerifyCS.VerifyAnalyzerAsync(
                source,
                VerifyCS.Diagnostic(StatelessValueInRequiresConvertToUnmanagedRule).WithLocation(0).WithArguments("MarshallerType", MarshalMode.ManagedToUnmanagedIn, "ManagedType"),
                VerifyCS.Diagnostic(StatelessValueInRequiresConvertToUnmanagedRule).WithLocation(1).WithArguments("MarshallerType", MarshalMode.UnmanagedToManagedOut, "ManagedType"),
                VerifyCS.Diagnostic(StatelessValueInRequiresConvertToUnmanagedRule).WithLocation(2).WithArguments("MarshallerType", MarshalMode.ElementIn, "ManagedType"));
        }

        [Fact]
        public async Task ModeThatUsesUnmanagedToManagedShape_Missing_ConvertToManagedMethod_ReportsDiagnostic()
        {
            string source = """
                using System.Runtime.InteropServices.Marshalling;
                
                class ManagedType {}
                
                [CustomMarshaller(typeof(ManagedType), MarshalMode.ManagedToUnmanagedOut, typeof({|#0:MarshallerType|}))]
                [CustomMarshaller(typeof(ManagedType), MarshalMode.UnmanagedToManagedIn, typeof({|#1:MarshallerType|}))]
                [CustomMarshaller(typeof(ManagedType), MarshalMode.ElementOut, typeof({|#2:MarshallerType|}))]
                static class MarshallerType
                {
                }
                """;

            await VerifyCS.VerifyAnalyzerAsync(
                source,
                VerifyCS.Diagnostic(StatelessRequiresConvertToManagedRule).WithLocation(0).WithArguments("MarshallerType", MarshalMode.ManagedToUnmanagedOut, "ManagedType"),
                VerifyCS.Diagnostic(StatelessRequiresConvertToManagedRule).WithLocation(1).WithArguments("MarshallerType", MarshalMode.UnmanagedToManagedIn, "ManagedType"),
                VerifyCS.Diagnostic(StatelessRequiresConvertToManagedRule).WithLocation(2).WithArguments("MarshallerType", MarshalMode.ElementOut, "ManagedType"));
        }

        [Fact]
        public async Task ModeThatUsesBidirectionalShape_Missing_BothConvertMethods_ReportsDiagnostic()
        {
            string source = """
                using System.Runtime.InteropServices.Marshalling;
                
                class ManagedType {}
                
                [CustomMarshaller(typeof(ManagedType), MarshalMode.ManagedToUnmanagedRef, typeof({|#0:MarshallerType|}))]
                [CustomMarshaller(typeof(ManagedType), MarshalMode.UnmanagedToManagedRef, typeof({|#1:MarshallerType|}))]
                [CustomMarshaller(typeof(ManagedType), MarshalMode.ElementRef, typeof({|#2:MarshallerType|}))]
                static class MarshallerType
                {
                }
                """;

            await VerifyCS.VerifyAnalyzerAsync(
                source,
                VerifyCS.Diagnostic(StatelessValueInRequiresConvertToUnmanagedRule).WithLocation(0).WithArguments("MarshallerType", MarshalMode.ManagedToUnmanagedRef, "ManagedType"),
                VerifyCS.Diagnostic(StatelessValueInRequiresConvertToUnmanagedRule).WithLocation(1).WithArguments("MarshallerType", MarshalMode.UnmanagedToManagedRef, "ManagedType"),
                VerifyCS.Diagnostic(StatelessValueInRequiresConvertToUnmanagedRule).WithLocation(2).WithArguments("MarshallerType", MarshalMode.ElementRef, "ManagedType"),
                VerifyCS.Diagnostic(StatelessRequiresConvertToManagedRule).WithLocation(0).WithArguments("MarshallerType", MarshalMode.ManagedToUnmanagedRef, "ManagedType"),
                VerifyCS.Diagnostic(StatelessRequiresConvertToManagedRule).WithLocation(1).WithArguments("MarshallerType", MarshalMode.UnmanagedToManagedRef, "ManagedType"),
                VerifyCS.Diagnostic(StatelessRequiresConvertToManagedRule).WithLocation(2).WithArguments("MarshallerType", MarshalMode.ElementRef, "ManagedType"));
        }

        [Fact]
        public async Task ModeThatUsesBidirectionalShape_MismatchedUnmanagedTypes_ReportsDiagnostic()
        {
            string source = """
                using System.Runtime.InteropServices.Marshalling;
                
                class ManagedType {}
                
                [CustomMarshaller(typeof(ManagedType), MarshalMode.ManagedToUnmanagedRef, typeof({|#0:MarshallerType|}))]
                [CustomMarshaller(typeof(ManagedType), MarshalMode.UnmanagedToManagedRef, typeof({|#1:MarshallerType|}))]
                [CustomMarshaller(typeof(ManagedType), MarshalMode.ElementRef, typeof({|#2:MarshallerType|}))]
                static class MarshallerType
                {
                    public static int ConvertToUnmanaged(ManagedType t) => default;
                    public static ManagedType ConvertToManaged(float f) => default;
                }
                """;

            await VerifyCS.VerifyAnalyzerAsync(
                source,
                VerifyCS.Diagnostic(FirstParameterMustMatchReturnTypeRule).WithLocation(0).WithArguments("MarshallerType.ConvertToManaged(float)", "MarshallerType.ConvertToUnmanaged(ManagedType)"),
                VerifyCS.Diagnostic(FirstParameterMustMatchReturnTypeRule).WithLocation(1).WithArguments("MarshallerType.ConvertToManaged(float)", "MarshallerType.ConvertToUnmanaged(ManagedType)"),
                VerifyCS.Diagnostic(FirstParameterMustMatchReturnTypeRule).WithLocation(2).WithArguments("MarshallerType.ConvertToManaged(float)", "MarshallerType.ConvertToUnmanaged(ManagedType)"));
        }

        [Fact]
        public async Task DefaultMode_Missing_BothConvertMethods_ReportsDiagnostic()
        {
            string source = """
                using System.Runtime.InteropServices.Marshalling;
                
                class ManagedType {}
                
                [CustomMarshaller(typeof(ManagedType), MarshalMode.Default, typeof({|#0:MarshallerType|}))]
                static class MarshallerType
                {
                }
                """;

            await VerifyCS.VerifyAnalyzerAsync(
                source,
                VerifyCS.Diagnostic(StatelessValueInRequiresConvertToUnmanagedRule).WithSeverity(DiagnosticSeverity.Info).WithLocation(0).WithArguments("MarshallerType", MarshalMode.Default, "ManagedType"),
                VerifyCS.Diagnostic(StatelessRequiresConvertToManagedRule).WithSeverity(DiagnosticSeverity.Info).WithLocation(0).WithArguments("MarshallerType", MarshalMode.Default, "ManagedType"));
        }

        [Fact]
        public async Task CallerAllocatedBuffer_NoBufferSize_ReportsDiagnostic()
        {
            string source = """
                using System;
                using System.Runtime.InteropServices.Marshalling;
                
                class ManagedType {}
                
                [CustomMarshaller(typeof(ManagedType), MarshalMode.ManagedToUnmanagedIn, typeof({|#0:MarshallerType|}))]
                static class MarshallerType
                {
                    public static nint ConvertToUnmanaged(ManagedType m, Span<byte> b) => default;
                }
                """;

            await VerifyCS.VerifyAnalyzerAsync(
                source,
                VerifyCS.Diagnostic(CallerAllocConstructorMustHaveBufferSizeRule).WithLocation(0).WithArguments("MarshallerType", "byte"));
        }
    }
}
