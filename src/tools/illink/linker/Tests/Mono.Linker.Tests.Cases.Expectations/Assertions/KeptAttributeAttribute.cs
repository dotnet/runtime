﻿using System;

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
	}
}
