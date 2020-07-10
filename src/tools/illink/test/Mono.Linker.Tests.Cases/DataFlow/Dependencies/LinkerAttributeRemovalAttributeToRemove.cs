using System;
using System.Collections.Generic;
using System.Text;

namespace Mono.Linker.Tests.Cases.DataFlow.Dependencies
{
	public enum TestAttributeUsedFromCopyAssemblyEnum
	{
		None
	}

	public class TestAttributeUsedFromCopyAssemblyAttribute : Attribute
	{
		public TestAttributeUsedFromCopyAssemblyAttribute (TestAttributeUsedFromCopyAssemblyEnum n)
		{
		}
	}

	public class TestAnotherAttributeUsedFromCopyAssemblyAttribute : Attribute
	{
		public TestAnotherAttributeUsedFromCopyAssemblyAttribute ()
		{
		}
	}
}
