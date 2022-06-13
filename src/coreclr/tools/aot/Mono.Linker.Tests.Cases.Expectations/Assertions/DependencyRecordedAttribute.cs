// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	[AttributeUsage (AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
	public class DependencyRecordedAttribute : BaseExpectedLinkedBehaviorAttribute
	{
		public DependencyRecordedAttribute (string source, string target, string marked = null)
		{
			if (string.IsNullOrEmpty (source))
				throw new ArgumentException ("Value cannot be null or empty.", nameof (source));

			if (string.IsNullOrEmpty (target))
				throw new ArgumentException ("Value cannot be null or empty.", nameof (target));
		}
	}
}
