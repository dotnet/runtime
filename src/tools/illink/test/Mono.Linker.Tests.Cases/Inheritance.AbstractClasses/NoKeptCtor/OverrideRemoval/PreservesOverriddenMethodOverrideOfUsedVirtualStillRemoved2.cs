using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NoKeptCtor.OverrideRemoval
{
	[SetupLinkerDescriptorFile ("PreservesOverriddenMethodOverrideOfUsedVirtualStillRemoved2.xml")]
	public class PreservesOverriddenMethodOverrideOfUsedVirtualStillRemoved2
	{
		public static void Main ()
		{
			Base j = new Jar ();
			j.One ();
		}

		[Kept]
		[KeptMember (".ctor()")]
		abstract class Base
		{
			[Kept]
			public abstract void Foo ();

			[Kept]
			public abstract void One ();
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (Base))]
		abstract class Base2 : Base
		{
			[Kept]
			public override void One ()
			{
			}
		}

		[Kept]
		[KeptBaseType (typeof (Base2))]
		class Bar : Base2
		{
			[Kept]
			public override void Foo ()
			{
			}

			public override void One ()
			{
			}
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (Base2))]
		class Jar : Base2
		{
			[Kept]
			public override void Foo ()
			{
			}

			[Kept]
			public override void One ()
			{
			}
		}
	}
}