using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Generics
{
	class CorrectOverloadedMethodGetsStrippedInGenericClass
	{
		public static void Main ()
		{
			// Call overloaded method through the abstract base class
			GenericClassWithTwoOverloadedAbstractMethods<float> item = new SpecializedClassWithTwoOverloadedVirtualMethods ();
			item.OverloadedMethod (5);
		}

		[KeptMember (".ctor()")]
		public abstract class GenericClassWithTwoOverloadedAbstractMethods<T>
		{
			public abstract string OverloadedMethod (T thing); // Don't call this one, it should be stripped

			[Kept]
			public abstract string OverloadedMethod (int thing); // Call to this should preserve the overriden one
		}

		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (GenericClassWithTwoOverloadedAbstractMethods<System.Single>))]
		public class SpecializedClassWithTwoOverloadedVirtualMethods : GenericClassWithTwoOverloadedAbstractMethods<float>
		{
			// Don't call this one, it should be stripped
			public override string OverloadedMethod (float thing)
			{
				return "first";
			}

			// Don't call this one, but it shouldn't be stripped because the method it overrides is invoked
			[Kept]
			public override string OverloadedMethod (int thing)
			{
				return "second";
			}
		}
	}
}
