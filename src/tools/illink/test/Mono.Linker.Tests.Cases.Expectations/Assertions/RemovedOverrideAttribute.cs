// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	/// <Summary>
	/// Used to ensure that a method should remove an 'override' annotation for a method in the supplied base type.
	/// Fails in tests if the method has the override method in the linked assembly,
	///		or if the override is not found in the original assembly
	/// </Summary>
	/// <seealso cref="KeptOverrideAttribute" />
	[AttributeUsage (AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
	public class RemovedOverrideAttribute : BaseInAssemblyAttribute
	{
		public RemovedOverrideAttribute (Type typeWithOverriddenMethod)
		{
			if (typeWithOverriddenMethod == null)
				throw new ArgumentException ("Value cannot be null or empty.", nameof (typeWithOverriddenMethod));
		}

		public RemovedOverrideAttribute (string nameOfOverriddenMethod)
		{
		}
	}
}
