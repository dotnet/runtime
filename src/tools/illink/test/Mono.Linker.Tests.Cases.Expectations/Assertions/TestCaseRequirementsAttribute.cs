// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	[AttributeUsage (AttributeTargets.Class)]
	public class TestCaseRequirementsAttribute : BaseExpectedLinkedBehaviorAttribute
	{
		public TestCaseRequirementsAttribute (TestRunCharacteristics targetFrameworkCharacteristics, string reason)
		{
			if (reason == null)
				throw new ArgumentNullException (nameof (reason));
		}
	}
}
