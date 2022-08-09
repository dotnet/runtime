using System;
using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Basic
{
	public class UnusedFieldsOfStructsAreKept
	{
		public static void Main ()
		{
			A a = new A ();
			PreventCompilerOptimization (a);
			R r = new R ();
		}

		[Kept]
		static void PreventCompilerOptimization (A a)
		{
		}

		[Kept]
		struct A
		{
			[Kept]
			private int UnusedField1;
			[Kept]
			private int UnusedField2;

			public void UnusedMethod ()
			{
			}
		}

		[KeptAttributeAttribute (typeof (IsByRefLikeAttribute))]
		[KeptAttributeAttribute (typeof (CompilerFeatureRequiredAttribute))]
		[KeptAttributeAttribute (typeof (ObsoleteAttribute))]
		ref struct R
		{
			[Kept]
			public ref int UnusedRefField;

			[Kept]
			int UnusedField;

			[Kept]
			int UsedField;
		}
	}
}