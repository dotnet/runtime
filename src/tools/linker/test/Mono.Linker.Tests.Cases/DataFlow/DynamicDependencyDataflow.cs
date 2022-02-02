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

		// Intrinsic is disabled https://github.com/dotnet/linker/issues/2559
		[Kept]
		[ExpectedWarning ("IL2080", nameof (Type.GetField), ProducedBy = ProducedBy.Trimmer)]
		[DynamicDependency ("DynamicDependencyTo")]
		static void DynamicDependencyFrom ()
		{
			_ = TypeWithPublicMethods.GetField ("f");
		}

		// Intrinsic is disabled https://github.com/dotnet/linker/issues/2559
		[Kept]
		[ExpectedWarning ("IL2080", nameof (Type.GetProperty), ProducedBy = ProducedBy.Trimmer)]
		static void DynamicDependencyTo ()
		{
			_ = TypeWithPublicMethods.GetProperty ("p");
		}
	}
}
