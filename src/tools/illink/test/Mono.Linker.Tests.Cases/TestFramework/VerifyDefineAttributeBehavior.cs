using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.TestFramework
{

	/// <summary>
	/// The purpose of this test is to verify that the testing framework's define attribute is working correctly
	/// </summary>
	[Define ("SOME_DEFINE")]
	public class VerifyDefineAttributeBehavior
	{
		static void Main ()
		{
#if SOME_DEFINE
			MethodThatIsUsedIfDefineIsWorkingProperly ();
#endif
		}


		[Kept]
		static void MethodThatIsUsedIfDefineIsWorkingProperly ()
		{
			Console.WriteLine ("Foo");
		}
	}
}
