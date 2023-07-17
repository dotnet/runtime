// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.Interop.UnitTests;

namespace ComInterfaceGenerator.Unit.Tests
{
    internal class VerifyCompilationTest<T> : Microsoft.Interop.UnitTests.Verifiers.CSharpSourceGeneratorVerifier<T>.Test
        where T : new()
    {
        public required Action<Compilation> CompilationVerifier { get; init; }

        public VerifyCompilationTest(TestTargetFramework targetFramework) : base(targetFramework)
        {
        }

        public VerifyCompilationTest(bool referenceAncillaryInterop) : base(referenceAncillaryInterop)
        {
        }

        protected override void VerifyFinalCompilation(Compilation compilation) => CompilationVerifier(compilation);
    }
}
