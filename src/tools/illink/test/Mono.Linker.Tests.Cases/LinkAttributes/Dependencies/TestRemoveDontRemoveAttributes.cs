using System;

namespace Mono.Linker.Tests.Cases.LinkAttributes.Dependencies
{
	public class TestRemoveAttribute : Attribute
	{
		public TestRemoveAttribute ()
		{
		}
	}

	public class TestDontRemoveAttribute : Attribute
	{
		public TestDontRemoveAttribute ()
		{
		}
	}
}
