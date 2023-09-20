// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Mono.Linker.Tests.Extensions;

namespace Mono.Linker.Tests.TestCasesRunner
{
	partial class TestRunner
	{
		partial void IgnoreTest (string reason)
		{
			Assert.Ignore (reason);
		}

		private partial IEnumerable<string>? GetAdditionalDefines() => null;

		private static T GetResultOfTaskThatMakesAssertions<T> (Task<T> task)
		{
			try {
				return task.Result;
			} catch (AggregateException e) {
				if (e.InnerException != null) {
					if (e.InnerException is AssertionException
					|| e.InnerException is SuccessException
					|| e.InnerException is IgnoreException
					|| e.InnerException is InconclusiveException)
						throw e.InnerException;
				}

				throw;
			}
		}

		protected partial TrimmingCustomizations? CustomizeTrimming (TrimmingDriver linker, TestCaseMetadataProvider metadataProvider)
		{
			TrimmingCustomizations customizations = new TrimmingCustomizations ();

			metadataProvider.CustomizeTrimming (linker, customizations);

			return customizations;
		}

		static partial void AddOutputDirectory (TestCaseSandbox sandbox, ManagedCompilationResult compilationResult, TrimmingArgumentBuilder builder)
		{
			builder.AddOutputDirectory (sandbox.OutputDirectory);
		}

		static partial void AddInputReference (NPath inputReference, TrimmingArgumentBuilder builder)
		{
			builder.AddReference (inputReference);
		}
	}
}