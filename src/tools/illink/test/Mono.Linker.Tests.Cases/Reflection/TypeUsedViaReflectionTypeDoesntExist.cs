using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Reflection
{
	[SetupCSharpCompilerToUse ("csc")]
	[ExpectedNoWarnings]
	public class TypeUsedViaReflectionTypeDoesntExist
	{
		public static void Main ()
		{
			var typeName = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflectionTypeDoesntExist+DoesntExist, test";
			var typeKept = Type.GetType (typeName, false);
		}

		public class Full { }
	}
}