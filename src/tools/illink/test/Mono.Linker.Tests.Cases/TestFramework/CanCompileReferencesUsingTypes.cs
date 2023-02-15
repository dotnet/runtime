using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.TestFramework.Dependencies;

namespace Mono.Linker.Tests.Cases.TestFramework
{
	[SetupCompileBefore ("library1.dll", new[] { typeof (CanCompileReferencesUsingTypes_LibSource1), typeof (CanCompileReferencesUsingTypes_LibSource2.Nested1.Nested2) })]
	public class CanCompileReferencesUsingTypes
	{
		public static void Main ()
		{
			// Only the compile before assembly types are used because we wouldn't have access to the after types
			CanCompileReferencesUsingTypes_LibSource1.MethodFromParentType ();
			CanCompileReferencesUsingTypes_LibSource2.Nested1.Nested2.MethodFromNestedNested ();
		}
	}
}