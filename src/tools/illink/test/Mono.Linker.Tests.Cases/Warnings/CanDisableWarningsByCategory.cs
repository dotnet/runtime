using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Warnings
{
	[ExpectedNoWarnings]
	[SkipKeptItemsValidation]
	[SetupLinkerArgument ("--notrimwarn")]
#if NATIVEAOT
	[SetupLinkerArgument ("--noaotwarn")]
#endif
	public class CanDisableWarningsByCategory
	{
		public static void Main ()
		{
			TestNonQualifiedType ();
			RequiresDynamicCodeOnStaticConstructor.Test ();
		}

		static void TestNonQualifiedType ()
		{
			RequireAll ("Mono.Linker.Tests.Cases.Warnings.TypeDoesntExist");
		}

		class RequiresDynamicCodeOnStaticConstructor
		{
			class StaticConstructor
			{
				[RequiresDynamicCode (nameof (StaticConstructor))]
				static StaticConstructor () { }
			}

			public static void Test () {
				new StaticConstructor ();
			}
		}

		static void RequireAll ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] string s) {}
	}
}
