// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Mono.Linker.Tests.Extensions;
using Mono.Linker.Tests.TestCases;

namespace Mono.Linker.Tests.TestCasesRunner
{
	public class ILCompilerTestCaseResult
	{
		public readonly TestCase TestCase;
		public readonly NPath InputAssemblyPath;
		public readonly NPath ExpectationsAssemblyPath;
		public readonly TestCaseSandbox Sandbox;
		public readonly TestCaseMetadataProvider MetadataProvider;
		public readonly ManagedCompilationResult CompilationResult;
		public readonly TestLogWriter LogWriter;

		public ILCompilerTestCaseResult (TestCase testCase, NPath inputAssemblyPath, NPath expectationsAssemblyPath, TestCaseSandbox sandbox, TestCaseMetadataProvider metadataProvider, ManagedCompilationResult compilationResult, TestLogWriter logWriter)
		{
			TestCase = testCase;
			InputAssemblyPath = inputAssemblyPath;
			ExpectationsAssemblyPath = expectationsAssemblyPath;
			Sandbox = sandbox;
			MetadataProvider = metadataProvider;
			CompilationResult = compilationResult;
			LogWriter = logWriter;
		}
	}
}
