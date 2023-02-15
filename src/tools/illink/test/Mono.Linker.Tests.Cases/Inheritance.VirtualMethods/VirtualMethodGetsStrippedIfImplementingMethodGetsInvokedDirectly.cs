using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.VirtualMethods
{
	class VirtualMethodGetsStrippedIfImplementingMethodGetsInvokedDirectly
	{
		public static void Main ()
		{
			new A ().Foo ();
		}

		[KeptMember (".ctor()")]
		class B
		{
			// TODO: Would be nice to be removed
			// https://github.com/dotnet/linker/issues/3097
			[KeptBy (typeof (A), nameof (A.Foo), "BaseMethod")]
			public virtual void Foo ()
			{
			}
		}

		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (B))]
		class A : B
		{
			// Bug: https://github.com/dotnet/linker/issues/3078
			// Linker should mark for DirectCall as well as OverrideOnInstantiatedType, not just OverrideOnInstantiatedType
			//[KeptBy (typeof(A), nameof(Foo), DependencyKind.DirectCall)]
			[KeptBy (typeof (A), "OverrideOnInstantiatedType")]
			public override void Foo ()
			{
			}
		}
	}
}
