using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType
{
	public class UnusedGenericInterfaceIsRemoved
	{
		public static void Main ()
		{
			var fb = new Foo ();
			IFoo<object> fo = fb;
			fo.Method (null);

			IFoo<int> fi = fb;
			fi.Method (0);
		}

		[Kept]
		interface IFoo<T>
		{
			[Kept]
			void Method (T arg);
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptInterface (typeof (IFoo<object>))]
		[KeptInterface (typeof (IFoo<int>))]
		[KeptInterface (typeof (IFoo<string>))] // FIXME : Should be removed
		[KeptInterface (typeof (IFoo<Bar>))] // FIXME : Should be removed
		class Foo : IFoo<object>, IFoo<int>, IFoo<string>, IFoo<Bar>
		{
			[Kept]
			public void Method (object arg)
			{
			}

			[Kept]
			public void Method (int arg)
			{
			}

			// FIXME : This should be removed.  The issue is caused by a combination of
			// * ShouldMarkInterfaceImplementation only checking if the resolved interface impl type is marked
			// * We don't track which generic instance types are marked 
			[Kept]
			public void Method (string arg)
			{
			}

			// FIXME : This should be removed.  The issue is caused by a combination of
			// * ShouldMarkInterfaceImplementation only checking if the resolved interface impl type is marked
			// * We don't track which generic instance types are marked 
			[Kept]
			public void Method (Bar arg)
			{
			}
		}

		[Kept] // FIXME : Should be removed
		class Bar
		{
		}
	}
}