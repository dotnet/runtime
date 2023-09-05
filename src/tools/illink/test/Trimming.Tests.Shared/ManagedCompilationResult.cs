// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Mono.Linker.Tests.Extensions;

namespace Mono.Linker.Tests.TestCasesRunner
{
	public class ManagedCompilationResult
	{
		public ManagedCompilationResult (NPath inputAssemblyPath, NPath expectationsAssemblyPath)
		{
			InputAssemblyPath = inputAssemblyPath;
			ExpectationsAssemblyPath = expectationsAssemblyPath;
		}

		public NPath InputAssemblyPath { get; }

		public NPath ExpectationsAssemblyPath { get; }
	}
}
