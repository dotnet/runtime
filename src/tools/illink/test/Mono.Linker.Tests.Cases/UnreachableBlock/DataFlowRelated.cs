// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.UnreachableBlock
{
	[SetupLinkerArgument ("--enable-opt", "ipconstprop")]
	[SkipKeptItemsValidation]
	public class DataFlowRelated
	{
		public static void Main ()
		{
			RemovedLambdaUsage ();
		}

		static bool AlwaysFalse => false;

		// This test cases a Debug.Assert - which has been temporarily disabled (search for the issue number in the codebase)
		// https://github.com/dotnet/linker/issues/2845
		static Func<int, int> RemovedLambdaUsage (int param = 0)
		{
			// Trigger data flow in this method
			typeof (TestType).GetProperties ();

			if (param == 0) {
				return (a) => a + 1;
			}

			if (AlwaysFalse) {
				return (a) => a + 2;
			} else {
				return (a) => a + 3;
			}
		}

		class TestType { }
	}
}
