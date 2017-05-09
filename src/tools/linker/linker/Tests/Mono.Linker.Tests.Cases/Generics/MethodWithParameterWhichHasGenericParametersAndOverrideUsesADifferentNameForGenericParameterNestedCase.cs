using System.Collections.Generic;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Generics {
	class MethodWithParameterWhichHasGenericParametersAndOverrideUsesADifferentNameForGenericParameterNestedCase {
		public static void Main (string [] args)
		{
			Derived<int, int> tmp = new Derived<int, int> ();
			tmp.Method<int> (null);
		}

		[Kept]
		public class Base<TSource> {

			[KeptMember (".ctor()")]
			public abstract class Nested {
				[Kept]
				public abstract TResult1 Method<TResult1> (IDictionary<TSource, TResult1> arg);
			}
		}

		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (Base<>.Nested), "TResult1")]
		public class Derived<TSource, TResult1> : Base<TResult1>.Nested {
			[Kept]
			public override TResult2 Method<TResult2> (IDictionary<TResult1, TResult2> arg)
			{
				return default (TResult2);
			}
		}
	}
}