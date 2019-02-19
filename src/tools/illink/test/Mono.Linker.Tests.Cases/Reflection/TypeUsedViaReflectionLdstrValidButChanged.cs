using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Reflection {
	/// <summary>
	/// We don't know if `typeName` will be changed or not.  If we error on the side of caution and preserve something
	/// that we found, I don't think that's a big deal
	/// </summary>
	public class TypeUsedViaReflectionLdstrValidButChanged {
		public static void Main ()
		{
			var replace = "Mono.Linker";
			var with = "Blah.Blah";
			var typeName = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflectionLdstrValidButChanged+Full, test";
			var typeKept = Type.GetType (typeName.Replace (replace, with), false);
		}

		[Kept]
		public class Full { }
	}
}