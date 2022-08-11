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

            string fixedSource = """
                using System.Runtime.InteropServices.Marshalling;

                class ManagedType {}
                
                [CustomMarshaller(typeof(ManagedType), MarshalMode.ManagedToUnmanagedIn, typeof(MarshallerType))]
                [CustomMarshaller(typeof(ManagedType), MarshalMode.UnmanagedToManagedOut, typeof(MarshallerType))]
                [CustomMarshaller(typeof(ManagedType), MarshalMode.ElementIn, typeof(MarshallerType))]
                static class MarshallerType
                {
                    public static nint ConvertToUnmanaged(ManagedType managed)
                    {
                        throw new System.NotImplementedException();
                    }
                }
                """;

            await CustomMarshallerAttributeFixerTest.VerifyCodeFixAsync(
                source,
                fixedSource,
                VerifyCS.Diagnostic(StatelessValueInRequiresConvertToUnmanagedRule).WithLocation(0).WithArguments("MarshallerType", MarshalMode.ManagedToUnmanagedIn, "ManagedType"),
                VerifyCS.Diagnostic(StatelessValueInRequiresConvertToUnmanagedRule).WithLocation(1).WithArguments("MarshallerType", MarshalMode.UnmanagedToManagedOut, "ManagedType"),
                VerifyCS.Diagnostic(StatelessValueInRequiresConvertToUnmanagedRule).WithLocation(2).WithArguments("MarshallerType", MarshalMode.ElementIn, "ManagedType"));
        }

        [Fact]
        public async Task ModeThatUsesManagedToUnmanagedIn_OnlyCallerAllocatedBuffer_DoesNotReportDiagnostic()
        {
            string source = """
                using System;
                using System.Runtime.InteropServices.Marshalling;
                
                class ManagedType {}
                
                [CustomMarshaller(typeof(ManagedType), MarshalMode.ManagedToUnmanagedIn, typeof(MarshallerType))]
                static class MarshallerType
                {
                    public static int BufferSize => 1;

                    public static nint ConvertToUnmanaged(ManagedType managed, Span<byte> buffer) => default;
                }
                """;

            await VerifyCS.VerifyAnalyzerAsync(source);
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

            string fixedSource = """
                using System.Runtime.InteropServices.Marshalling;

                class ManagedType {}
                
                [CustomMarshaller(typeof(ManagedType), MarshalMode.ManagedToUnmanagedOut, typeof(MarshallerType))]
                [CustomMarshaller(typeof(ManagedType), MarshalMode.UnmanagedToManagedIn, typeof(MarshallerType))]
                [CustomMarshaller(typeof(ManagedType), MarshalMode.ElementOut, typeof(MarshallerType))]
                static class MarshallerType
                {
                    public static ManagedType ConvertToManaged(nint unmanaged)
                    {
                        throw new System.NotImplementedException();
                    }
                }
                """;

            await CustomMarshallerAttributeFixerTest.VerifyCodeFixAsync(
                source,
                fixedSource,
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

            string fixedSource = """
                using System.Runtime.InteropServices.Marshalling;

                class ManagedType {}
                
                [CustomMarshaller(typeof(ManagedType), MarshalMode.ManagedToUnmanagedRef, typeof(MarshallerType))]
                [CustomMarshaller(typeof(ManagedType), MarshalMode.UnmanagedToManagedRef, typeof(MarshallerType))]
                [CustomMarshaller(typeof(ManagedType), MarshalMode.ElementRef, typeof(MarshallerType))]
                static class MarshallerType
                {
                    public static nint ConvertToUnmanaged(ManagedType managed)
                    {
                        throw new System.NotImplementedException();
                    }
                
                    public static ManagedType ConvertToManaged(nint unmanaged)
                    {
                        throw new System.NotImplementedException();
                    }
                }
                """;

            await CustomMarshallerAttributeFixerTest.VerifyCodeFixAsync(
                source,
                fixedSource,
                VerifyCS.DiagnosticWithArguments(StatelessValueInRequiresConvertToUnmanagedRule, "MarshallerType", MarshalMode.ManagedToUnmanagedRef, "ManagedType").WithLocation(0),
                VerifyCS.DiagnosticWithArguments(StatelessValueInRequiresConvertToUnmanagedRule, "MarshallerType", MarshalMode.UnmanagedToManagedRef, "ManagedType").WithLocation(1),
                VerifyCS.DiagnosticWithArguments(StatelessValueInRequiresConvertToUnmanagedRule, "MarshallerType", MarshalMode.ElementRef, "ManagedType").WithLocation(2),
                VerifyCS.DiagnosticWithArguments(StatelessRequiresConvertToManagedRule, "MarshallerType", MarshalMode.ManagedToUnmanagedRef, "ManagedType").WithLocation(0),
                VerifyCS.DiagnosticWithArguments(StatelessRequiresConvertToManagedRule, "MarshallerType", MarshalMode.UnmanagedToManagedRef, "ManagedType").WithLocation(1),
                VerifyCS.DiagnosticWithArguments(StatelessRequiresConvertToManagedRule, "MarshallerType", MarshalMode.ElementRef, "ManagedType").WithLocation(2));
        }

        [Fact]
        public async Task ModeThatUsesBidirectionalShape_Missing_ConvertToUnmanaged_AddsMethod_WithMatchingUnmanagedType()
        {
            string source = """
                using System.Runtime.InteropServices.Marshalling;
                
                class ManagedType {}
                
                [CustomMarshaller(typeof(ManagedType), MarshalMode.ManagedToUnmanagedRef, typeof({|#0:MarshallerType|}))]
                [CustomMarshaller(typeof(ManagedType), MarshalMode.UnmanagedToManagedRef, typeof({|#1:MarshallerType|}))]
                [CustomMarshaller(typeof(ManagedType), MarshalMode.ElementRef, typeof({|#2:MarshallerType|}))]
                static class MarshallerType
                {
                    public static ManagedType ConvertToManaged(float unmanaged)
                    {
                        throw new System.NotImplementedException();
                    }
                }
                """;

            string fixedSource = """
                using System.Runtime.InteropServices.Marshalling;

                class ManagedType {}
                
                [CustomMarshaller(typeof(ManagedType), MarshalMode.ManagedToUnmanagedRef, typeof(MarshallerType))]
                [CustomMarshaller(typeof(ManagedType), MarshalMode.UnmanagedToManagedRef, typeof(MarshallerType))]
                [CustomMarshaller(typeof(ManagedType), MarshalMode.ElementRef, typeof(MarshallerType))]
                static class MarshallerType
                {
                    public static ManagedType ConvertToManaged(float unmanaged)
                    {
                        throw new System.NotImplementedException();
                    }

                    public static float ConvertToUnmanaged(ManagedType managed)
                    {
                        throw new System.NotImplementedException();
                    }
                }
                """;

            await CustomMarshallerAttributeFixerTest.VerifyCodeFixAsync(
                source,
                fixedSource,
                VerifyCS.Diagnostic(StatelessValueInRequiresConvertToUnmanagedRule).WithLocation(0).WithArguments("MarshallerType", MarshalMode.ManagedToUnmanagedRef, "ManagedType"),
                VerifyCS.Diagnostic(StatelessValueInRequiresConvertToUnmanagedRule).WithLocation(1).WithArguments("MarshallerType", MarshalMode.UnmanagedToManagedRef, "ManagedType"),
                VerifyCS.Diagnostic(StatelessValueInRequiresConvertToUnmanagedRule).WithLocation(2).WithArguments("MarshallerType", MarshalMode.ElementRef, "ManagedType"));
        }

        [Fact]
        public async Task ModeThatUsesBidirectionalShape_Missing_ConvertToManaged_AddsMethod_WithMatchingUnmanagedType()
        {
            string source = """
                using System.Runtime.InteropServices.Marshalling;
                
                class ManagedType {}
                
                [CustomMarshaller(typeof(ManagedType), MarshalMode.ManagedToUnmanagedRef, typeof({|#0:MarshallerType|}))]
                [CustomMarshaller(typeof(ManagedType), MarshalMode.UnmanagedToManagedRef, typeof({|#1:MarshallerType|}))]
                [CustomMarshaller(typeof(ManagedType), MarshalMode.ElementRef, typeof({|#2:MarshallerType|}))]
                static class MarshallerType
                {
                    public static float ConvertToUnmanaged(ManagedType managed)
                    {
                        throw new System.NotImplementedException();
                    }
                }
                """;

            string fixedSource = """
                using System.Runtime.InteropServices.Marshalling;

                class ManagedType {}
                
                [CustomMarshaller(typeof(ManagedType), MarshalMode.ManagedToUnmanagedRef, typeof(MarshallerType))]
                [CustomMarshaller(typeof(ManagedType), MarshalMode.UnmanagedToManagedRef, typeof(MarshallerType))]
                [CustomMarshaller(typeof(ManagedType), MarshalMode.ElementRef, typeof(MarshallerType))]
                static class MarshallerType
                {
                    public static float ConvertToUnmanaged(ManagedType managed)
                    {
                        throw new System.NotImplementedException();
                    }

                    public static ManagedType ConvertToManaged(float unmanaged)
                    {
                        throw new System.NotImplementedException();
                    }
                }
                """;

            await CustomMarshallerAttributeFixerTest.VerifyCodeFixAsync(
                source,
                fixedSource,
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

            string fixedSource = """
                using System.Runtime.InteropServices.Marshalling;

                class ManagedType {}
                
                [CustomMarshaller(typeof(ManagedType), MarshalMode.Default, typeof(MarshallerType))]
                static class MarshallerType
                {
                    public static nint ConvertToUnmanaged(ManagedType managed)
                    {
                        throw new System.NotImplementedException();
                    }
                
                    public static ManagedType ConvertToManaged(nint unmanaged)
                    {
                        throw new System.NotImplementedException();
                    }
                }
                """;

            await CustomMarshallerAttributeFixerTest.VerifyCodeFixAsync(
                source,
                fixedSource,
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

            string fixedSource = """
                using System;
                using System.Runtime.InteropServices.Marshalling;
                
                class ManagedType {}
                
                [CustomMarshaller(typeof(ManagedType), MarshalMode.ManagedToUnmanagedIn, typeof(MarshallerType))]
                static class MarshallerType
                {
                    public static nint ConvertToUnmanaged(ManagedType m, Span<byte> b) => default;

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

        [Fact]
        public async Task ModeThatUsesManagedToUnmanagedShape_Missing_ConvertToUnmanagedMethod_Marshaller_DifferentDocument_ReportsDiagnostic()
        {
            string entryPointTypeSource = """
                using System.Runtime.InteropServices.Marshalling;

                class ManagedType {}
                
                [CustomMarshaller(typeof(ManagedType), MarshalMode.ManagedToUnmanagedIn, typeof({|SYSLIB1057:OtherMarshallerType|}))]
                [CustomMarshaller(typeof(ManagedType), MarshalMode.UnmanagedToManagedOut, typeof({|SYSLIB1057:OtherMarshallerType|}))]
                [CustomMarshaller(typeof(ManagedType), MarshalMode.ElementIn, typeof({|SYSLIB1057:OtherMarshallerType|}))]
                static class MarshallerType
                {
                }
                """;

            string otherMarshallerTypeOriginalSource = """
                static class OtherMarshallerType
                {
                }
                """;

            string otherMarshallerTypeFixedSource = """
                static class OtherMarshallerType
                {
                    public static nint ConvertToUnmanaged(ManagedType managed)
                    {
                        throw new System.NotImplementedException();
                    }
                }
                """;

            var test = new VerifyCS.Test();
            test.TestState.Sources.Add(entryPointTypeSource);
            test.TestState.Sources.Add(("OtherMarshaller.cs", otherMarshallerTypeOriginalSource));
            test.FixedState.Sources.Add(entryPointTypeSource);
            test.FixedState.Sources.Add(("OtherMarshaller.cs", otherMarshallerTypeFixedSource));
            test.MarkupOptions = MarkupOptions.UseFirstDescriptor;
            test.FixedState.MarkupHandling = MarkupMode.IgnoreFixable;
            await test.RunAsync();
        }

        [Fact]
        public async Task ModeThatUsesManagedToUnmanagedShape_Missing_ConvertToUnmanagedMethod_Marshaller_DifferentProject_ReportsDiagnostic()
        {
            string entryPointTypeSource = """
                using System.Runtime.InteropServices.Marshalling;

                [CustomMarshaller(typeof(ManagedType), MarshalMode.ManagedToUnmanagedIn, typeof({|SYSLIB1057:OtherMarshallerType|}))]
                [CustomMarshaller(typeof(ManagedType), MarshalMode.UnmanagedToManagedOut, typeof({|SYSLIB1057:OtherMarshallerType|}))]
                [CustomMarshaller(typeof(ManagedType), MarshalMode.ElementIn, typeof({|SYSLIB1057:OtherMarshallerType|}))]
                static class MarshallerType
                {
                }
                """;

            string otherMarshallerTypeOriginalSource = """
                public class ManagedType {}
                public static class OtherMarshallerType
                {
                }
                """;

            string otherMarshallerTypeFixedSource = """
                public class ManagedType {}
                public static class OtherMarshallerType
                {
                    public static nint ConvertToUnmanaged(ManagedType managed)
                    {
                        throw new System.NotImplementedException();
                    }
                }
                """;

            var test = new VerifyCS.Test();

            string otherProjectName = "OtherMarshallerProject";
            ProjectState otherProjectOriginalState = new ProjectState(otherProjectName, LanguageNames.CSharp, "/1/Other", "cs");
            otherProjectOriginalState.Sources.Add(otherMarshallerTypeOriginalSource);
            otherProjectOriginalState.AdditionalReferences.AddRange(test.TestState.AdditionalReferences);

            ProjectState otherProjectFixedState = new ProjectState(otherProjectName, LanguageNames.CSharp, "/1/Other", "cs");
            otherProjectFixedState.Sources.Add(otherMarshallerTypeFixedSource);
            otherProjectFixedState.AdditionalReferences.AddRange(test.TestState.AdditionalReferences);

            test.TestState.Sources.Add(entryPointTypeSource);
            test.TestState.AdditionalProjects.Add(otherProjectName, otherProjectOriginalState);
            test.TestState.AdditionalProjectReferences.Add(otherProjectName);

            test.FixedState.Sources.Add(entryPointTypeSource);
            test.FixedState.AdditionalProjects.Add(otherProjectName, otherProjectFixedState);
            test.FixedState.AdditionalProjectReferences.Add(otherProjectName);
            test.FixedState.MarkupHandling = MarkupMode.IgnoreFixable;

            test.NumberOfFixAllIterations = 1;
            test.MarkupOptions = MarkupOptions.UseFirstDescriptor;
            await test.RunAsync();
        }

        [Fact]
        public async Task ModeThatUsesManagedToUnmanagedShape_Missing_ConvertToUnmanagedMethod_TwoManagedTypes_ReportsDiagnostic()
        {
            string source = """
                using System.Runtime.InteropServices.Marshalling;

                class ManagedType {}
                class ManagedType2 {}

                [CustomMarshaller(typeof(ManagedType), MarshalMode.ManagedToUnmanagedIn, typeof({|#0:MarshallerType|}))]
                [CustomMarshaller(typeof(ManagedType2), MarshalMode.ManagedToUnmanagedIn, typeof({|#1:MarshallerType|}))]
                static class MarshallerType
                {
                }
                """;

            string fixedSource = """
                using System.Runtime.InteropServices.Marshalling;

                class ManagedType {}
                class ManagedType2 {}

                [CustomMarshaller(typeof(ManagedType), MarshalMode.ManagedToUnmanagedIn, typeof(MarshallerType))]
                [CustomMarshaller(typeof(ManagedType2), MarshalMode.ManagedToUnmanagedIn, typeof(MarshallerType))]
                static class MarshallerType
                {
                    public static nint ConvertToUnmanaged(ManagedType managed)
                    {
                        throw new System.NotImplementedException();
                    }

                    public static nint ConvertToUnmanaged(ManagedType2 managed)
                    {
                        throw new System.NotImplementedException();
                    }
                }
                """;

            await CustomMarshallerAttributeFixerTest.VerifyCodeFixAsync(
                source,
                fixedSource,
                VerifyCS.Diagnostic(StatelessValueInRequiresConvertToUnmanagedRule).WithLocation(0).WithArguments("MarshallerType", MarshalMode.ManagedToUnmanagedIn, "ManagedType"),
                VerifyCS.Diagnostic(StatelessValueInRequiresConvertToUnmanagedRule).WithLocation(1).WithArguments("MarshallerType", MarshalMode.ManagedToUnmanagedIn, "ManagedType2"));
        }
    }
}
