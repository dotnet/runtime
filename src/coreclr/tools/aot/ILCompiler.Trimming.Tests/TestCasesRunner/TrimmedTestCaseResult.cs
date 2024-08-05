// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ILCompiler;
using Mono.Linker.Tests.Extensions;
using Mono.Linker.Tests.TestCases;

namespace Mono.Linker.Tests.TestCasesRunner
{
	partial class TrimmedTestCaseResult
	{
		public readonly ILScanResults TrimmingResults;

		public TrimmedTestCaseResult (
			TestCase testCase,
			NPath inputAssemblyPath,
			NPath outputAssemblyPath,
			NPath expectationsAssemblyPath,
			TestCaseSandbox sandbox,
			TestCaseMetadataProvider metadataProvider,
			ManagedCompilationResult compilationResult,
			TrimmingTestLogger logger,
			TrimmingCustomizations? customizations,
			ILScanResults trimmingResults)
		{
			// Ignore outputAssemblyPath because ILCompiler trimming tests don't write output assemblies.
			// Ignore TrimmingCustomizatoins which are not used by ILCompiler trimming tests.
			TestCase = testCase;
			InputAssemblyPath = inputAssemblyPath;
			ExpectationsAssemblyPath = expectationsAssemblyPath;
			Sandbox = sandbox;
			MetadataProvider = metadataProvider;
			CompilationResult = compilationResult;			
			Logger = logger;
			TrimmingResults = trimmingResults;
		}
	}
}
