using Mono.Linker.Tests.Cases.Expectations.Assertions;
using System.Linq.Expressions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Reflection
{
	// Explicitly use roslyn to try and get a compiler that supports defining `TestOnlyStatic2` without a setter
	[SetupCSharpCompilerToUse ("csc")]
	[Reference ("System.Core.dll")]
	public class ExpressionPropertyString
	{
		public static void Main ()
		{
			// So that this test works with or without unreachable bodies
			new Foo ();
			
			var e1 = Expression.Property (null, typeof (Foo), "TestOnlyStatic1");
			var e2 = Expression.Property (null, typeof (Foo), "TestOnlyStatic2");

			var e3 = Expression.Property (Expression.Parameter (typeof(int), "somename"), typeof (Foo), "TestName1");
		}

		[KeptMember (".ctor()")]
		class Foo
		{
			[Kept]
			[KeptBackingField]
			private static int TestOnlyStatic1 { [Kept] get; [Kept] set; }

			private int TestOnlyStatic2 { get; }

			[Kept]
			[KeptBackingField]
			private int TestName1 { [Kept] get; }
		}
	}
}
