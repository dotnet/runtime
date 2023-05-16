// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.Interop.UnitTests;
using VerifyCS = Microsoft.Interop.UnitTests.Verifiers.CSharpSourceGeneratorVerifier<Microsoft.Interop.ComClassGenerator>;

namespace ComInterfaceGenerator.Unit.Tests
{
    internal class VerifyCompilationTest : VerifyCS.Test
    {
        public Action<Compilation>? CompilationVerifier { get; init; }

        public VerifyCompilationTest(TestTargetFramework targetFramework) : base(targetFramework)
        {
        }

        public VerifyCompilationTest(bool referenceAncillaryInterop) : base(referenceAncillaryInterop)
        {
        }

        protected override void VerifyFinalCompilation(Compilation compilation)
        {
            if(CompilationVerifier != null)
                CompilationVerifier(compilation);
        }
    }
}
