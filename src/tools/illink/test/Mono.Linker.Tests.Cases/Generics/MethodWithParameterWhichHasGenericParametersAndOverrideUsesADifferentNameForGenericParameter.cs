using System.Collections.Generic;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Generics
{
	[IgnoreTestCase ("Ignore in NativeAOT, see https://github.com/dotnet/runtime/issues/82447", IgnoredBy = Tool.NativeAot)]
	[KeptAttributeAttribute (typeof (IgnoreTestCaseAttribute), By = Tool.Trimmer)]
	class MethodWithParameterWhichHasGenericParametersAndOverrideUsesADifferentNameForGenericParameter
	{
		public static void Main ()
		{
			Derived<int, int> tmp = new Derived<int, int> ();
			tmp.Method<int> (null);
		}

		[KeptMember (".ctor()")]
		public abstract class Base<TSource>
		{
			[Kept]
			public abstract TResult1 Method<TResult1> (IDictionary<TSource, TResult1> arg);
		}

		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (Base<>), "TResult1")]
		public class Derived<TSource, TResult1> : Base<TResult1>
		{
			[Kept]
			public override TResult2 Method<TResult2> (IDictionary<TResult1, TResult2> arg)
			{
				return default (TResult2);
			}
		}
	}
}
