// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	[AttributeUsage (AttributeTargets.All, AllowMultiple = true, Inherited = false)]
	public class KeptAttributeAttribute : KeptAttribute
	{

		public KeptAttributeAttribute (string attributeName)
		{
			if (string.IsNullOrEmpty (attributeName))
				throw new ArgumentException ("Value cannot be null or empty.", nameof (attributeName));
		}

		public KeptAttributeAttribute (Type type)
		{
			if (type == null)
				throw new ArgumentNullException (nameof (type));
		}
	}
}
