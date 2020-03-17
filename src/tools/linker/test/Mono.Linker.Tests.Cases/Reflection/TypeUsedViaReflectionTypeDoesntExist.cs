using Mono.Linker.Tests.Cases.Expectations.Assertions;
using System;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Reflection {
	[SetupCSharpCompilerToUse ("csc")]
	[VerifyAllReflectionAccessPatternsAreValidated]
	public class TypeUsedViaReflectionTypeDoesntExist {
		[UnrecognizedReflectionAccessPatternAttribute (
			typeof (Type), nameof (Type.GetType), new Type [] { typeof (string), typeof (bool) })]
		public static void Main ()
		{
			var typeName = "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflectionTypeDoesntExist+Full, DoesntExist";
			var typeKept = Type.GetType (typeName, false);
		}

		public class Full { }
	}
}