using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.BaseProvidesInterfaceMember
{
	// mcs silently generates an explicit interface `Method` on `FooWithBase` that calls `BaseFoo.Method`, this leads to a failure
	// because the explicit interface `Method` needs a `[Kept]` on it.
	// To work around this, use csc so that the IL that is produced matches the test assertions we define
	[SetupCSharpCompilerToUse ("csc")]
	public class GenericInterfaceWithMethodComplex
	{
		public static void Main ()
		{
			IFoo<object> f = new FooWithBase ();
			f.Method (null, 0);
		}

		[Kept]
		interface IFoo<T>
		{
			[Kept]
			void Method (T arg, int arg2);
		}

		[Kept]
		[KeptMember (".ctor()")]
		class BaseFoo<T1>
		{
			[Kept]
			public void Method (object arg, T1 arg2)
			{
			}
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (BaseFoo<>), "T1")]
		class BaseFoo2<T1> : BaseFoo<T1>
		{
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (BaseFoo2<>), "T1")]
		class BaseFoo3<T1> : BaseFoo2<T1>
		{
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (BaseFoo3<>), "T1")]
		class BaseFoo4<T1> : BaseFoo3<T1>
		{
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (BaseFoo4<int>))]
		[KeptInterface (typeof (IFoo<object>))]
		class FooWithBase : BaseFoo4<int>, IFoo<object>
		{
		}
	}
}