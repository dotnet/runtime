using System;

namespace Mono.Linker.Tests.Cases.Reflection
{
	public class TypeUsedViaReflectionLdstrValidButChanged
	{
		public static void Main ()
		{
			var replace = "Mono.Linker";
			var with = "Blah.Blah";
			var typeName = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflectionLdstrValidButChanged+Full, test";
			var typeKept = Type.GetType (typeName.Replace (replace, with), false);
		}

		public class Full { }
	}
}