using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[ExpectedNoWarnings]
	public class DynamicDependencyDataflow
	{
		public static void Main ()
		{
			DynamicDependencyFrom ();
		}

		[Kept]
		[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
		static Type TypeWithPublicMethods;

		[Kept]
		[ExpectedWarning ("IL2080", nameof (Type.GetField))]
		[DynamicDependency ("DynamicDependencyTo")]
		static void DynamicDependencyFrom ()
		{
			_ = TypeWithPublicMethods.GetField ("f");
		}

		[Kept]
		[ExpectedWarning ("IL2080", nameof (Type.GetProperty))]
		static void DynamicDependencyTo ()
		{
			_ = TypeWithPublicMethods.GetProperty ("p");
		}
	}
}
