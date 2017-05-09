﻿using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	[AttributeUsage (AttributeTargets.All, AllowMultiple = true, Inherited = false)]
	public class KeptAttributeAttribute : KeptAttribute
	{
		public readonly string AttributeName;

		public KeptAttributeAttribute (string attributeName)
		{
			AttributeName = attributeName;
		}
	}
}
