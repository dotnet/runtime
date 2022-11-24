// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Mono.Linker.Tests.TestCasesRunner;
using Xunit;

namespace Mono.Linker.Tests.TestCases
{
	public class All
	{

		[Theory]
		[MemberData(nameof(TestDatabase.DataFlow), MemberType = typeof(TestDatabase))]
		public void DataFlow(string t)
		{
			Run(t);
		}

		[Theory]
		[MemberData (nameof (TestDatabase.Repro), MemberType = typeof (TestDatabase))]
		public void Repro (string t)
		{
			Run (t);
		}

		[Theory]
		[MemberData(nameof(TestDatabase.RequiresCapability), MemberType = typeof(TestDatabase))]
		public void RequiresCapability(string t)
		{
			Run(t);
		}

		protected virtual void Run(string testName)
		{
			TestCase testCase = TestDatabase.GetTestCaseFromName(testName) ?? throw new InvalidOperationException($"Unknown test {testName}");
			var runner = new TestRunner(new ObjectFactory());
			var linkedResult = runner.Run(testCase);
			if (linkedResult != null)
			{
				new ResultChecker().Check(linkedResult);
			}
		}
	}
}
