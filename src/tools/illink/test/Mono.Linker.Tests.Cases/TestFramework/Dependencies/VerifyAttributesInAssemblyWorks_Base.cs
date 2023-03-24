using System;

namespace Mono.Linker.Tests.Cases.TestFramework.Dependencies
{
	public class VerifyAttributesInAssemblyWorks_Base
	{
		public class ForAssertingKeptAttribute : Attribute
		{
		}

		public class ForAssertingRemoveAttribute : Attribute
		{
		}
	}
}