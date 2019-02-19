using System;

namespace Mono.Linker.Tests.Cases.Reflection {
	public class TypeUsedViaReflectionAssemblyDoesntExist {
		public static void Main ()
		{
			var typeName = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflectionAssemblyDoesntExist+DoesntExist, test";
			var typeKept = Type.GetType (typeName, false);
		}

		public class Full { }
	}
}