// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.Interop;
using System.Collections.Generic;
using System.Threading;
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

            string fixedSource = """
                using System.Runtime.InteropServices.Marshalling;
                
                class ManagedType {}
                
                [CustomMarshaller(typeof(ManagedType), MarshalMode.ManagedToUnmanagedIn, typeof(MarshallerType))]
                [CustomMarshaller(typeof(ManagedType), MarshalMode.UnmanagedToManagedOut, typeof(MarshallerType))]
                struct MarshallerType
                {
                    public void FromManaged(ManagedType managed)
                    {
                        throw new System.NotImplementedException();
                    }

                    public nint ToUnmanaged()
                    {
                        throw new System.NotImplementedException();
                    }

                    public void Free()
                    {
                        throw new System.NotImplementedException();
                    }
                }
                """;

            await CustomMarshallerAttributeFixerTest.VerifyCodeFixAsync(
                source,
                fixedSource,
                VerifyCS.DiagnosticWithArguments(StatefulMarshallerRequiresFromManagedRule, "MarshallerType", MarshalMode.ManagedToUnmanagedIn, "ManagedType").WithLocation(0),
                VerifyCS.DiagnosticWithArguments(StatefulMarshallerRequiresFromManagedRule, "MarshallerType", MarshalMode.UnmanagedToManagedOut, "ManagedType").WithLocation(1),
                VerifyCS.DiagnosticWithArguments(StatefulMarshallerRequiresToUnmanagedRule, "MarshallerType", MarshalMode.ManagedToUnmanagedIn, "ManagedType").WithLocation(0),
                VerifyCS.DiagnosticWithArguments(StatefulMarshallerRequiresToUnmanagedRule, "MarshallerType", MarshalMode.UnmanagedToManagedOut, "ManagedType").WithLocation(1),
                VerifyCS.DiagnosticWithArguments(StatefulMarshallerRequiresFreeRule, "MarshallerType").WithLocation(0),
                VerifyCS.DiagnosticWithArguments(StatefulMarshallerRequiresFreeRule, "MarshallerType").WithLocation(1));
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

            string fixedSource = """
                using System.Runtime.InteropServices.Marshalling;
                
                class ManagedType {}
                
                [CustomMarshaller(typeof(ManagedType), MarshalMode.ManagedToUnmanagedOut, typeof(MarshallerType))]
                [CustomMarshaller(typeof(ManagedType), MarshalMode.UnmanagedToManagedIn, typeof(MarshallerType))]
                struct MarshallerType
                {
                    public void FromUnmanaged(nint unmanaged)
                    {
                        throw new System.NotImplementedException();
                    }

                    public ManagedType ToManaged()
                    {
                        throw new System.NotImplementedException();
                    }

                    public void Free()
                    {
                        throw new System.NotImplementedException();
                    }
                }
                """;

            await CustomMarshallerAttributeFixerTest.VerifyCodeFixAsync(
                source,
                fixedSource,
                VerifyCS.DiagnosticWithArguments(StatefulMarshallerRequiresFromUnmanagedRule, "MarshallerType", MarshalMode.ManagedToUnmanagedOut, "ManagedType").WithLocation(0),
                VerifyCS.DiagnosticWithArguments(StatefulMarshallerRequiresFromUnmanagedRule, "MarshallerType", MarshalMode.UnmanagedToManagedIn, "ManagedType").WithLocation(1),
                VerifyCS.DiagnosticWithArguments(StatefulMarshallerRequiresToManagedRule, "MarshallerType", MarshalMode.ManagedToUnmanagedOut, "ManagedType").WithLocation(0),
                VerifyCS.DiagnosticWithArguments(StatefulMarshallerRequiresToManagedRule, "MarshallerType", MarshalMode.UnmanagedToManagedIn, "ManagedType").WithLocation(1),
                VerifyCS.DiagnosticWithArguments(StatefulMarshallerRequiresFreeRule, "MarshallerType").WithLocation(0),
                VerifyCS.DiagnosticWithArguments(StatefulMarshallerRequiresFreeRule, "MarshallerType").WithLocation(1));
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

            string fixedSource = """
                using System.Runtime.InteropServices.Marshalling;
                
                class ManagedType {}
                
                [CustomMarshaller(typeof(ManagedType), MarshalMode.ManagedToUnmanagedRef, typeof(MarshallerType))]
                [CustomMarshaller(typeof(ManagedType), MarshalMode.UnmanagedToManagedRef, typeof(MarshallerType))]
                struct MarshallerType
                {
                    public void FromManaged(ManagedType managed)
                    {
                        throw new System.NotImplementedException();
                    }

                    public nint ToUnmanaged()
                    {
                        throw new System.NotImplementedException();
                    }

                    public void FromUnmanaged(nint unmanaged)
                    {
                        throw new System.NotImplementedException();
                    }
                
                    public ManagedType ToManaged()
                    {
                        throw new System.NotImplementedException();
                    }
                
                    public void Free()
                    {
                        throw new System.NotImplementedException();
                    }
                }
                """;

            await CustomMarshallerAttributeFixerTest.VerifyCodeFixAsync(
                source,
                fixedSource,
                VerifyCS.DiagnosticWithArguments(StatefulMarshallerRequiresFreeRule, "MarshallerType").WithLocation(0),
                VerifyCS.DiagnosticWithArguments(StatefulMarshallerRequiresFromManagedRule, "MarshallerType", MarshalMode.ManagedToUnmanagedRef, "ManagedType").WithLocation(0),
                VerifyCS.DiagnosticWithArguments(StatefulMarshallerRequiresFromManagedRule, "MarshallerType", MarshalMode.UnmanagedToManagedRef, "ManagedType").WithLocation(1),
                VerifyCS.DiagnosticWithArguments(StatefulMarshallerRequiresToUnmanagedRule, "MarshallerType", MarshalMode.ManagedToUnmanagedRef, "ManagedType").WithLocation(0),
                VerifyCS.DiagnosticWithArguments(StatefulMarshallerRequiresToUnmanagedRule, "MarshallerType", MarshalMode.UnmanagedToManagedRef, "ManagedType").WithLocation(1),
                VerifyCS.DiagnosticWithArguments(StatefulMarshallerRequiresFromUnmanagedRule, "MarshallerType", MarshalMode.ManagedToUnmanagedRef, "ManagedType").WithLocation(0),
                VerifyCS.DiagnosticWithArguments(StatefulMarshallerRequiresFromUnmanagedRule, "MarshallerType", MarshalMode.UnmanagedToManagedRef, "ManagedType").WithLocation(1),
                VerifyCS.DiagnosticWithArguments(StatefulMarshallerRequiresToManagedRule, "MarshallerType", MarshalMode.ManagedToUnmanagedRef, "ManagedType").WithLocation(0),
                VerifyCS.DiagnosticWithArguments(StatefulMarshallerRequiresToManagedRule, "MarshallerType", MarshalMode.UnmanagedToManagedRef, "ManagedType").WithLocation(1),
                VerifyCS.DiagnosticWithArguments(StatefulMarshallerRequiresFreeRule, "MarshallerType").WithLocation(1));
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

            string fixedSource = """
                using System.Runtime.InteropServices.Marshalling;
                
                class ManagedType {}
                
                [CustomMarshaller(typeof(ManagedType), MarshalMode.Default, typeof(MarshallerType))]
                struct MarshallerType
                {
                    public void FromManaged(ManagedType managed)
                    {
                        throw new System.NotImplementedException();
                    }
                
                    public nint ToUnmanaged()
                    {
                        throw new System.NotImplementedException();
                    }

                    public void FromUnmanaged(nint unmanaged)
                    {
                        throw new System.NotImplementedException();
                    }
                
                    public ManagedType ToManaged()
                    {
                        throw new System.NotImplementedException();
                    }
                
                    public void Free()
                    {
                        throw new System.NotImplementedException();
                    }
                }
                """;

            await CustomMarshallerAttributeFixerTest.VerifyCodeFixAsync(
                source,
                fixedSource,
                VerifyCS.Diagnostic(StatefulMarshallerRequiresFreeRule).WithLocation(0).WithArguments("MarshallerType"),
                VerifyCS.DiagnosticWithArguments(StatefulMarshallerRequiresFromManagedRule, "MarshallerType", MarshalMode.Default, "ManagedType").WithSeverity(DiagnosticSeverity.Info).WithLocation(0),
                VerifyCS.DiagnosticWithArguments(StatefulMarshallerRequiresToUnmanagedRule, "MarshallerType", MarshalMode.Default, "ManagedType").WithSeverity(DiagnosticSeverity.Info).WithLocation(0),
                VerifyCS.DiagnosticWithArguments(StatefulMarshallerRequiresFromUnmanagedRule, "MarshallerType", MarshalMode.Default, "ManagedType").WithSeverity(DiagnosticSeverity.Info).WithLocation(0),
                VerifyCS.DiagnosticWithArguments(StatefulMarshallerRequiresToManagedRule, "MarshallerType", MarshalMode.Default, "ManagedType").WithSeverity(DiagnosticSeverity.Info).WithLocation(0));
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

            string fixedSource = """
                using System;
                using System.Runtime.InteropServices.Marshalling;
                
                class ManagedType {}
                
                [CustomMarshaller(typeof(ManagedType), MarshalMode.ManagedToUnmanagedIn, typeof({|#0:MarshallerType|}))]
                struct MarshallerType
                {
                    public void FromManaged(ManagedType m, Span<byte> b) {}

                    public int ToUnmanaged() => default;

                    public void Free() {}

                    public static int BufferSize
                    {
                        get
                        {
                            throw new NotImplementedException();
                        }
                    }
                }
                """;

            await CustomMarshallerAttributeFixerTest.VerifyCodeFixAsync(
                source,
                fixedSource,
                VerifyCS.Diagnostic(CallerAllocFromManagedMustHaveBufferSizeRule).WithLocation(0).WithArguments("MarshallerType", "byte"));
        }
    }
}
