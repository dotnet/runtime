using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.BaseProvidesInterfaceMember
{
	// mcs silently generates an explicit interface `Method` on `FooWithBase` that calls `BaseFoo.Method`, this leads to a failure
	// because the explicit interface `Method` needs a `[Kept]` on it.
	// To work around this, use csc so that the IL that is produced matches the test assertions we define
	[SetupCSharpCompilerToUse ("csc")]
	public class GenericInterfaceWithGenericBaseMethod
	{
		public static void Main ()
		{
			IFoo<object, int> f = new FooWithBase<object, int> ();
			f.Method (null, 0);
		}

		[Kept]
		interface IFoo<T1, T2>
		{
			[Kept]
			void Method (T1 arg, T2 arg2);
		}

		[Kept]
		[KeptMember (".ctor()")]
		class BaseFoo<T1, T2>
		{
			[Kept]
			public void Method (T1 arg, T2 arg2)
			{
			}
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (BaseFoo<,>), "T1", "T2")]
		[KeptInterface (typeof (IFoo<,>), "T1", "T2")]
		class FooWithBase<T1, T2> : BaseFoo<T1, T2>, IFoo<T1, T2>
		{
		}
	}
}