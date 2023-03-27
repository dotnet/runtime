using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Extensibility
{
	[SetupCompileBefore ("MarkHandler.dll", new[] { "Dependencies/CustomMarkHandler.cs" }, new[] { "illink.dll", "Mono.Cecil.dll", "netstandard.dll" })]
	[SetupLinkerArgument ("--custom-step", "CustomMarkHandler,MarkHandler.dll")]
	public class MarkHandlerUsage
	{
		public static void Main ()
		{
			UsedType.UsedMethod ();
		}

		[Kept]
		public class DiscoveredTypeForAssembly
		{
			[Kept]
			public static void DiscoveredMethodForType_DiscoveredTypeForAssembly ()
			{
			}

			[Kept]
			public static void DiscoveredMethodForMethod_DiscoveredMethodForType_DiscoveredTypeForAssembly ()
			{
			}

			public static void UnusedMethod ()
			{
			}

			public static void DiscoveredMethodForMethod_UnusedMethod ()
			{
			}
		}

		[Kept]
		public class UsedType
		{
			[Kept]
			public static void DiscoveredMethodForType_UsedType ()
			{
			}

			[Kept]
			public static void DiscoveredMethodForMethod_DiscoveredMethodForType_UsedType ()
			{
			}

			[Kept]
			public static void UsedMethod ()
			{
			}

			[Kept]
			public static void DiscoveredMethodForMethod_UsedMethod ()
			{
			}

			[Kept]
			public static void DiscoveredMethodForMethod_DiscoveredMethodForMethod_UsedMethod ()
			{
			}

			public static void UnusedMethod ()
			{
			}

			public static void DiscoveredMethodForMethod_UnusedMethod ()
			{
			}
		}

		public class UnusedType
		{
			public static void DiscoveredMethodForType_UnusedType ()
			{
			}

			public static void DiscoveredMethodForMethod_DiscoveredMethodForType_UnusedType ()
			{
			}

			public static void UnusedMethod ()
			{
			}
		}
	}
}