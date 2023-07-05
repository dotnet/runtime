using System;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

//[assembly: AssemblyVersion ("2.0")]

namespace Mono.Linker.Tests.Cases.TypeForwarding.Dependencies
{
	public interface ImplementationLibraryInterface
	{
		public int GetDefaultImplementation ()
		{
			return 42;
		}
	}

	public class ImplementationLibraryImp : ImplementationLibraryInterface
	{
	}

	public class ImplementationLibrary
	{
		public class ImplementationLibraryNestedType
		{
			public static int PropertyOnNestedType { get; set; }
		}

		public class ForwardedNestedType
		{
		}

		public static int someField = 42;

		public string GetSomeValue ()
		{
			return "Hello";
		}
	}

	public class AnotherImplementationClass
	{
		public class ForwardedNestedType
		{
		}
	}

	[AttributeUsage (AttributeTargets.All)]
	public class ImplementationLibraryAttribute : Attribute
	{
	}

	public struct ImplementationStruct
	{
		public int Field;
	}
}
