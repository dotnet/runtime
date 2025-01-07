using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Attributes.OnlyKeepUsed
{
	[SetupCSharpCompilerToUse ("csc")]
	[SetupLinkerArgument ("--used-attrs-only", "true")]
	public class MethodWithUnmanagedConstraint
	{
		public static void Main ()
		{
			Method<int> ();
		}

		/// <summary>
		/// The compiler will generate a CustomAttribute that is of type IsUnmanagedAttribute.  By not annotating the attribute
		/// as being kept we are asserting that the IsUnmanagedAttribute is removed, which is expected because the attribute is
		/// only needed at compile time
		/// </summary>
		/// <typeparam name="T"></typeparam>
		[Kept]
		static void Method<
			[KeptGenericParamAttributes (GenericParameterAttributes.NotNullableValueTypeConstraint | GenericParameterAttributes.DefaultConstructorConstraint)]
			T
		> () where T : unmanaged
		{
		}
	}
}
