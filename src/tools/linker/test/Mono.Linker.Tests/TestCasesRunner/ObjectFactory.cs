﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Mono.Cecil;
using Mono.Linker.Tests.TestCases;

namespace Mono.Linker.Tests.TestCasesRunner
{
    public class ObjectFactory
    {
        public virtual TestCaseSandbox CreateSandbox(TestCase testCase)
        {
            return new TestCaseSandbox(testCase);
        }

        public virtual TestCaseCompiler CreateCompiler(TestCaseSandbox sandbox, TestCaseCompilationMetadataProvider metadataProvider)
        {
            return new TestCaseCompiler(sandbox, metadataProvider);
        }

        public virtual LinkerDriver CreateLinker()
        {
            return new LinkerDriver();
        }

        public virtual TestCaseMetadataProvider CreateMetadataProvider(TestCase testCase, AssemblyDefinition expectationsAssemblyDefinition)
        {
            return new TestCaseMetadataProvider(testCase, expectationsAssemblyDefinition);
        }

        public virtual TestCaseCompilationMetadataProvider CreateCompilationMetadataProvider(TestCase testCase, AssemblyDefinition fullTestCaseAssemblyDefinition)
        {
            return new TestCaseCompilationMetadataProvider(testCase, fullTestCaseAssemblyDefinition);
        }

        public virtual LinkerArgumentBuilder CreateLinkerArgumentBuilder(TestCaseMetadataProvider metadataProvider)
        {
            return new LinkerArgumentBuilder(metadataProvider);
        }
    }
}
