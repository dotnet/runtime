// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Mono.Linker.Tests.Extensions;
using Mono.Linker.Tests.TestCases;

namespace Mono.Linker.Tests.TestCasesRunner
{
    public class TrimmedTestCaseResult
    {
        public readonly TestCase TestCase;
        public readonly NPath InputAssemblyPath;
        public readonly NPath OutputAssemblyPath;
        public readonly NPath ExpectationsAssemblyPath;
        public readonly TestCaseSandbox Sandbox;
        public readonly TestCaseMetadataProvider MetadataProvider;
        public readonly ManagedCompilationResult CompilationResult;

        public TrimmedTestCaseResult(TestCase testCase, NPath inputAssemblyPath, NPath outputAssemblyPath, NPath expectationsAssemblyPath, TestCaseSandbox sandbox, TestCaseMetadataProvider metadataProvider, ManagedCompilationResult compilationResult)
        {
            TestCase = testCase;
            InputAssemblyPath = inputAssemblyPath;
            OutputAssemblyPath = outputAssemblyPath;
            ExpectationsAssemblyPath = expectationsAssemblyPath;
            Sandbox = sandbox;
            MetadataProvider = metadataProvider;
            CompilationResult = compilationResult;
        }
    }
}
