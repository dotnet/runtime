// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Mono.Linker.Tests.Extensions;
using Mono.Linker.Tests.TestCases;

namespace Mono.Linker.Tests.TestCasesRunner
{
    partial class TrimmedTestCaseResult
    {
        public readonly NPath OutputAssemblyPath;
        public readonly TrimmingCustomizations Customizations;
        public readonly int ExitCode;

        public TrimmedTestCaseResult(
            TestCase testCase,
            NPath inputAssemblyPath,
            NPath outputAssemblyPath,
            NPath expectationsAssemblyPath,
            TestCaseSandbox sandbox,
            TestCaseMetadataProvider metadataProvider,
            ManagedCompilationResult compilationResult,
            TrimmingTestLogger logger,
            TrimmingCustomizations? customizations,
            TrimmingResults trimmingResults)
        {
            TestCase = testCase;
            InputAssemblyPath = inputAssemblyPath;
            OutputAssemblyPath = outputAssemblyPath;
            ExpectationsAssemblyPath = expectationsAssemblyPath;
            Sandbox = sandbox;
            MetadataProvider = metadataProvider;
            CompilationResult = compilationResult;
            Logger = logger;
            Customizations = customizations ?? new TrimmingCustomizations();
            ExitCode = trimmingResults.ExitCode;
        }
    }
}
