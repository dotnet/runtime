using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DynamicDependencies
{
	public class DynamicDependencyField
	{
		public static void Main ()
		{
			DirectReference.Test ();
			ReferenceViaReflection.Test ();
		}

		[KeptMember (".ctor()")]
		class DirectReference
		{
			[Kept]
			[DynamicDependency ("ExtraMethod1")]
			public int field;

			[Kept]
			static void ExtraMethod1 ()
			{
			}

			[Kept]
			public static void Test ()
			{
				var b = new DirectReference ();
				b.field = 3;
			}
		}

		class ReferenceViaReflection
		{
			[Kept]
			[DynamicDependency ("TargetMethod")]
			public static int source;

			[Kept]
			static void TargetMethod ()
			{
			}

			[Kept]
			public static void Test ()
			{
				typeof (ReferenceViaReflection).RequiresPublicFields ();
			}
		}
	}
}
