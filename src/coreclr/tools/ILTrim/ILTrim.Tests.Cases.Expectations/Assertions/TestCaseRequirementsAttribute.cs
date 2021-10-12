// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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