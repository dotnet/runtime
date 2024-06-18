#nullable enable

using System.Reflection;
using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Attributes.OnlyKeepUsed
{
	[SetupCSharpCompilerToUse ("csc")]
	[SetupLinkerArgument ("--used-attrs-only", "true")]
	[SetupLinkerTrimMode ("link")]
	[IgnoreDescriptors (false)]
	public class NullableOnConstraintsRemoved
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
			static T? Method<
				[KeptGenericParamAttributes (GenericParameterAttributes.ReferenceTypeConstraint)] 
				T
			> () where T : class, I?
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
		class C<T> where T : I?
		{
		}
	}
}
