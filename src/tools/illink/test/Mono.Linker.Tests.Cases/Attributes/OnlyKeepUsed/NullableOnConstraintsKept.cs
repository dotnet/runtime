#nullable enable

using System.Reflection;
using System.Runtime.CompilerServices;

using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Attributes.OnlyKeepUsed
{
	[SetupCSharpCompilerToUse ("csc")]
	[SetupLinkerTrimMode ("link")]
	public class NullableOnConstraintsKept
	{
		public static void Main ()
		{
			Test.Run ();
		}

		[Kept]
		[KeptInterface (typeof (I))]
		class Test : I
		{
			[Kept]
			public static void Run ()
			{
				new C<Test> ();
				Method<Test> ();
			}

			[Kept]
			[KeptAttributeAttribute (typeof (NullableContextAttribute))]
			static T? Method<
				[KeptGenericParamAttributes (GenericParameterAttributes.ReferenceTypeConstraint)] 
				[KeptAttributeAttribute (typeof (NullableAttribute))]
				T
			> ()
				where T : class, I?
			{
				return default;
			}
		}

		[Kept]
		interface I
		{
		}

		[Kept]
		[KeptMember (".ctor()")]
		class C<
			[KeptAttributeOnConstraint (typeof (I), typeof (NullableAttribute))]
			T
		>
			where T : I?
		{
		}
	}
}
