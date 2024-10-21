using System;
using System.Reflection;
using System.Runtime.CompilerServices;

using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Generics
{
	[IgnoreTestCase ("Ignore in NativeAOT, see https://github.com/dotnet/runtime/issues/82447", IgnoredBy = Tool.NativeAot)]
	[KeptAttributeAttribute (typeof (IgnoreTestCaseAttribute), By = Tool.Trimmer)]
	class ByRefLike
	{
		static void Main ()
		{
			Test ();
		}

		[Kept]
		static void Test ()
		{
			G<RefStruct> g = new ();
		}

		[Kept]
		[KeptAttributeAttribute (typeof (IsByRefLikeAttribute))]
		[KeptAttributeAttribute (typeof (ObsoleteAttribute))] // Signals this is unsupported to older compilers
		[KeptAttributeAttribute (typeof (CompilerFeatureRequiredAttribute))]
		ref struct RefStruct {
		}

		[Kept]
		[KeptAttributeAttribute (typeof (IsByRefLikeAttribute))]
		[KeptAttributeAttribute (typeof (ObsoleteAttribute))] // Signals this is unsupported to older compilers
		[KeptAttributeAttribute (typeof (CompilerFeatureRequiredAttribute))]
		ref struct G<
			[KeptGenericParamAttributes (GenericParameterAttributes.AllowByRefLike)]
			T
		> where T : allows ref struct {
			[Kept]
			public T t;
		}
	}
}
