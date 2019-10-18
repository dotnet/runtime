using Mono.Linker.Tests.Cases.Expectations.Assertions;
using System.Linq.Expressions;
using System;
using System.Linq;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Reflection
{
	[Reference ("System.Core.dll")]
	public class ExpressionNewType
	{
		public static void Main ()
		{
			var expr = Expression.New (typeof (T1));
		}

		[Kept]
		class T1
		{
			[Kept]
			internal T1 ()
			{

			}
		}
	}
}
