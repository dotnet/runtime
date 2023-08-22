
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.Interop;
using Microsoft.Interop.UnitTests;
using Xunit;
using VerifyComInterfaceGenerator = Microsoft.Interop.UnitTests.Verifiers.CSharpSourceGeneratorVerifier<Microsoft.Interop.ComInterfaceGenerator>;



namespace ComInterfaceGenerator.Unit.Tests
{
    public class BestPracticesMessagesTests
    {
        [Fact]
        public async Task ArrayParameterWithNoAttributesGivesMessage()
        {
            var source = $$"""
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;

                [GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16)]
                [Guid("a4a20815-f1f2-4fa6-ada7-ecb05ac60d30")]
                internal partial interface IFoo
                {
                    void BlittableArray([MarshalUsing(CountElementName = nameof(size))] int[] {|#0:param|}, int size);
                    void BlittableOutArray([MarshalUsing(CountElementName = nameof(size))] out int[] param, int size);
                    void MaybeBlittableArray([MarshalUsing(CountElementName = nameof(size))] char[] {|#1:param|}, int size);
                    void NonBlittableArray([MarshalUsing(CountElementName = nameof(size))] IntStruct[] {|#2:param|}, int size);
                }
            """ + CodeSnippets.IntStructAndMarshaller;
            var diagnostic = new DiagnosticResult(GeneratorDiagnostics.GeneratedComInterfaceUsageDoesNotFollowBestPractices)
                .WithArguments(SR.PreferExplicitInOutAttributesOnArrays);

            VerifyComInterfaceGenerator.Test test = new(referenceAncillaryInterop: false)
            {
                TestCode = source,
                TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
                CompilerDiagnostics = CompilerDiagnostics.Suggestions,
            };
            test.ExpectedDiagnostics.AddRange([diagnostic.WithLocation(0), diagnostic.WithLocation(1), diagnostic.WithLocation(2)]);
            await test.RunAsync();
        }
    }
}
