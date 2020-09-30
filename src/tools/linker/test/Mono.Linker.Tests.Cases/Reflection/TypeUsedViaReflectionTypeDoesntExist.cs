using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Reflection
{
	[SetupCSharpCompilerToUse ("csc")]
	[VerifyAllReflectionAccessPatternsAreValidated]
	public class TypeUsedViaReflectionTypeDoesntExist
	{
		public static void Main ()
		{
			var typeName = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflectionTypeDoesntExist+Full, DoesntExist";
			var typeKept = Type.GetType (typeName, false);
		}

		public class Full { }
	}
}