// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mono.Cecil;
using Mono.Linker.Tests.Extensions;
using Mono.Linker.Tests.TestCases;
using Xunit.Sdk;

namespace Mono.Linker.Tests.TestCasesRunner
{
	public partial class TestRunner
	{
		partial void IgnoreTest (string reason)
		{
			throw new IgnoreTestException (reason);
		}

		static IEnumerable<string> additionalDefines = new string[] { "NATIVEAOT" };
		private partial IEnumerable<string>? GetAdditionalDefines () => additionalDefines;

		private static T GetResultOfTaskThatMakesAssertions<T> (Task<T> task)
		{
			try {
				return task.Result;
			} catch (AggregateException e) {
				if (e.InnerException != null) {
					if (e.InnerException is XunitException)
						throw e.InnerException;
				}

				throw;
			}
		}

		protected partial TrimmingCustomizations? CustomizeTrimming (TrimmingDriver linker, TestCaseMetadataProvider metadataProvider)
			=> null;

		protected partial void AddDumpDependenciesOptions (TestCaseLinkerOptions caseDefinedOptions, ManagedCompilationResult compilationResult, TrimmingArgumentBuilder builder, TestCaseMetadataProvider metadataProvider)
		{
		}

		static partial void AddOutputDirectory (TestCaseSandbox sandbox, ManagedCompilationResult compilationResult, TrimmingArgumentBuilder builder)
		{
			builder.AddOutputDirectory (sandbox.OutputDirectory.Combine (compilationResult.InputAssemblyPath.FileNameWithoutExtension + ".obj"));
		}

		static partial void AddInputReference (NPath inputReference, TrimmingArgumentBuilder builder)
		{
			// It's important to add all assemblies as "link" assemblies since the default configuration
			// is to run the compiler in multi-file mode which will not process anything which is just in the reference set.
			builder.AddLinkAssembly (inputReference);
			builder.AddReference (inputReference);
		}
	}
}
