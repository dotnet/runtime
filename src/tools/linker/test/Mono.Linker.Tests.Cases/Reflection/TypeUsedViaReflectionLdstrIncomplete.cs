using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Reflection
{
	/// <summary>
	/// This case we can't detect and need to gracefully do nothing
	/// </summary>
	public class TypeUsedViaReflectionLdstrIncomplete
	{
		public static void Main ()
		{
			var typePart = GetTypePart ();
			var assemblyPart = ",test";
			var typeKept = Type.GetType (string.Concat (typePart, assemblyPart), false);
		}

		public class Full { }

		[Kept]
		static string GetTypePart ()
		{
			return "Mono.Linker.Tests.Cases.Reflection.TypeUsedViaReflectionLdstrIncomplete+Full";
		}
	}
}