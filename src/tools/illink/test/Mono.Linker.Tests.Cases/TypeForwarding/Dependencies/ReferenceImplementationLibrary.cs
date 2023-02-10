using System;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.TypeForwarding.Dependencies
{
	[NotATestCase]
	public class ReferenceImplementationLibrary
	{
	}

#if INCLUDE_REFERENCE_IMPL
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

	public class ImplementationLibrary {
		public class ImplementationLibraryNestedType
		{
			public static int PropertyOnNestedType { get; set; }
		}

		public class ForwardedNestedType
		{
		}

		public static int someField = 0;

		public string GetSomeValue ()
		{
			return null;
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
#endif
}
