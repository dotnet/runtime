using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.LinqExpressions
{
	[SetupLinkerArgument ("--disable-operator-discovery")]
	public class CanDisableOperatorDiscovery
	{
		public static void Main ()
		{
			var c = new CustomOperators ();
			var expression = typeof (System.Linq.Expressions.Expression);
			c = -c;
			var t = typeof (TargetType);
		}

		[KeptMember (".ctor()")]
		class CustomOperators
		{
			[Kept]
			public static CustomOperators operator - (CustomOperators c) => null;

			public static CustomOperators operator + (CustomOperators c) => null;
			public static CustomOperators operator + (CustomOperators left, CustomOperators right) => null;
			public static explicit operator TargetType (CustomOperators self) => null;
		}

		[Kept]
		class TargetType { }
	}
}
