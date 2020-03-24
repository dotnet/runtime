using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Reflection
{
	[SetupCSharpCompilerToUse ("csc")]
	[KeptMember(".ctor()")]
	public class ActivatorCreateInstance
	{
		[UnrecognizedReflectionAccessPattern(
			typeof(Activator), nameof(Activator.CreateInstance) + "<T>", new Type[0])]
		public static void Main ()
		{
			Activator.CreateInstance (typeof (Test1));
			Activator.CreateInstance (typeof (Test2), true);
			Activator.CreateInstance (typeof (Test3), BindingFlags.NonPublic | BindingFlags.Instance, null, null, null);
			Activator.CreateInstance (typeof (Test4), new object [] { 1, "ss" });
			Activator.CreateInstance ("test", "Mono.Linker.Tests.Cases.Reflection.ActivatorCreateInstance+Test5");
			Activator.CreateInstance<Test1> ();

			var p = new ActivatorCreateInstance ();

			// Direct call to static method with a parameter
			FromParameterOnStaticMethod (typeof (FromParameterOnStaticMethodType));

			// Value comes from multiple sources - each must be marked appropriately
			Type fromParameterOnStaticMethodType = p == null ?
				typeof (FromParameterOnStaticMethodTypeA) :
				typeof (FromParameterOnStaticMethodTypeB);
			FromParameterOnStaticMethod (fromParameterOnStaticMethodType);

			// Direct call to instance method with a parameter
			p.FromParameterOnInstanceMethod (typeof (FromParameterOnInstanceMethodType));

			// All constructors required
			FromParameterWithConstructors (typeof (FromParameterWithConstructorsType));

			// Public contructors required
			FromParameterWithPublicConstructors (typeof (FromParameterWithPublicConstructorsType));
		}

		[Kept]
		class Test1
		{
			[Kept]
			public Test1 ()
			{
			}

			public Test1 (int arg)
			{
			}
		}

		[Kept]
		class Test2
		{
			[Kept]
			private Test2 ()
			{
			}

			public Test2 (int arg)
			{
			}
		}

		[Kept]
		class Test3
		{
			[Kept]
			private Test3 ()
			{
			}

			public Test3 (int arg)
			{
			}
		}

		[Kept]
		class Test4
		{
			[Kept] // TODO: Should not be kept
			public Test4 ()
			{
			}

			[Kept]
			public Test4 (int i, object o)
			{
			}
		}


		[Kept]
		class Test5
		{
			[Kept]
			public Test5 ()
			{
			}

			public Test5 (int i, object o)
			{
			}
		}

		[Kept]
		private static void FromParameterOnStaticMethod (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.DefaultConstructor)]
			[KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))]
			Type type)
		{
			Activator.CreateInstance (type);
		}

		[Kept]
		class FromParameterOnStaticMethodType
		{
			[Kept]
			public FromParameterOnStaticMethodType () { }
			public FromParameterOnStaticMethodType (int arg) { }
		}

		[Kept]
		class FromParameterOnStaticMethodTypeA
		{
			[Kept]
			public FromParameterOnStaticMethodTypeA () { }
			public FromParameterOnStaticMethodTypeA (int arg) { }
		}

		[Kept]
		class FromParameterOnStaticMethodTypeB
		{
			[Kept]
			public FromParameterOnStaticMethodTypeB () { }
			public FromParameterOnStaticMethodTypeB (int arg) { }
		}

		[Kept]
		private void FromParameterOnInstanceMethod (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.DefaultConstructor)]
			[KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))]
			Type type)
		{
			Activator.CreateInstance (type);
		}

		[Kept]
		class FromParameterOnInstanceMethodType
		{
			[Kept]
			public FromParameterOnInstanceMethodType () { }
			public FromParameterOnInstanceMethodType (int arg) { }
		}

		[Kept]
		private static void FromParameterWithConstructors (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.Constructors)]
			[KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))]
			Type type)

		{
			// All ctors are required by the parameter, so all cases should work
			Activator.CreateInstance (type);
			Activator.CreateInstance (type, new object [] { 1 });
			Activator.CreateInstance (type, BindingFlags.NonPublic, new object [] { 1, 2 });
		}

		[Kept]
		class FromParameterWithConstructorsType
		{
			[Kept]
			public FromParameterWithConstructorsType () { }
			[Kept]
			public FromParameterWithConstructorsType (int arg) { }
			[Kept]
			private FromParameterWithConstructorsType (int arg, int arg2) { }
		}

		[Kept]
		private static void FromParameterWithPublicConstructors (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.PublicConstructors)]
			[KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))]
			Type type)
		{
			// Only public ctors and default ctor are required, so only those cases should work
			Activator.CreateInstance (type);
			Activator.CreateInstance (type, new object [] { 1 });
		}

		[Kept]
		class FromParameterWithPublicConstructorsType
		{
			[Kept]
			public FromParameterWithPublicConstructorsType () { }
			[Kept]
			public FromParameterWithPublicConstructorsType (int arg) { }
			private FromParameterWithPublicConstructorsType (int arg, int arg2) { }
		}
	}
}
