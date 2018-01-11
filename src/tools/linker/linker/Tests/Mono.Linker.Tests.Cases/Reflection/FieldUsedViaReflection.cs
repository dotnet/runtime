using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Reflection {
	public class FieldUsedViaReflection {
		public static void Main ()
		{
			var field = typeof (FieldUsedViaReflection).GetField ("field");
			field.GetValue (null);
		}

		[Kept]
		static int field;
	}
}
