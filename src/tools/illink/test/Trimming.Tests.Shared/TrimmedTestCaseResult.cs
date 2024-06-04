// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable
using System;
using Mono.Linker.Tests.Extensions;
using Mono.Linker.Tests.TestCases;

namespace Mono.Linker.Tests.TestCasesRunner
{
	public partial class TrimmedTestCaseResult
	{
		public readonly TestCase TestCase;
		public readonly NPath InputAssemblyPath;
		public readonly NPath ExpectationsAssemblyPath;
		public readonly TestCaseSandbox Sandbox;
		public readonly TestCaseMetadataProvider MetadataProvider;
		public readonly ManagedCompilationResult CompilationResult;
		public readonly TrimmingTestLogger Logger;
	}
}
