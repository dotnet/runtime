using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NoKeptCtor.OverrideRemoval
{
	[SetupLinkerDescriptorFile ("PreservesOverriddenMethodOverrideOfUsedVirtualStillRemoved.xml")]
	public class PreservesOverriddenMethodOverrideOfUsedVirtualStillRemoved
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
			public virtual void One ()
			{
			}
		}

		[Kept]
		[KeptBaseType (typeof (Base))]
		class Bar : Base
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
		[KeptBaseType (typeof (Base))]
		class Jar : Base
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