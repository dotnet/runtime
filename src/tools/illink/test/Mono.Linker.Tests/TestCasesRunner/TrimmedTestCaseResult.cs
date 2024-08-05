// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable
using System;
using Mono.Linker.Tests.Extensions;
using Mono.Linker.Tests.TestCases;

namespace Mono.Linker.Tests.TestCasesRunner
{
	partial class TrimmedTestCaseResult
	{
		public readonly NPath OutputAssemblyPath;
		public readonly TrimmingCustomizations Customizations;
		public readonly int ExitCode;

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
			Customizations = customizations ?? throw new InvalidOperationException ("Customizations must be provided");
			ExitCode = trimmingResults.ExitCode;
		}
	}
}
