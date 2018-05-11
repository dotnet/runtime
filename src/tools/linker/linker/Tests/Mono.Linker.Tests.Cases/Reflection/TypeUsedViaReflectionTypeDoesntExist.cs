using System;

namespace Mono.Linker.Tests.Cases.Reflection {
	public class TypeUsedViaReflectionTypeDoesntExist {
		public static void Main ()
		{
			var typeName = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflectionTypeDoesntExist+Full, DoesntExist";
			var typeKept = Type.GetType (typeName, false);
		}

		public class Full { }
	}
}