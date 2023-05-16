// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.Interop;
using Xunit;
using VerifyComInterfaceGenerator = Microsoft.Interop.UnitTests.Verifiers.CSharpSourceGeneratorVerifier<Microsoft.Interop.ComInterfaceGenerator>;

namespace ComInterfaceGenerator.Unit.Tests
{
    public class InvalidInterfaceDiagnostics
    {
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


    }
}
