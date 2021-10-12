using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	[AttributeUsage (AttributeTargets.Class)]
	public class NoLinkedOutputAttribute : Attribute
	{
		public NoLinkedOutputAttribute () { }
	}
}
