using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Reflection
{
	[SetupCSharpCompilerToUse ("csc")]
	[ExpectedNoWarnings]
	public class ConstructorsUsedViaReflection
	{
		public static void Main ()
		{
			TestGetConstructors ();
			TestWithBindingFlags ();
			TestWithUnknownBindingFlags (BindingFlags.Public);
			TestNullType ();
			TestDataFlowType ();
			TestDataFlowWithAnnotation (typeof (MyType));
			TestIfElse (true);
		}

		[RecognizedReflectionAccessPattern]
		[Kept]
		static void TestGetConstructors ()
		{
			var constructors = typeof (SimpleGetConstructors).GetConstructors ();
		}

		[RecognizedReflectionAccessPattern]
		[Kept]
		static void TestWithBindingFlags ()
		{
			var constructors = typeof (ConstructorsBindingFlags).GetConstructors (BindingFlags.Public | BindingFlags.Static);
		}

		[RecognizedReflectionAccessPattern]
		[Kept]
		static void TestWithUnknownBindingFlags (BindingFlags bindingFlags)
		{
			// Since the binding flags are not known linker should mark all constructors on the type
			var constructors = typeof (UnknownBindingFlags).GetConstructors (bindingFlags);
		}

		[Kept]
		[RecognizedReflectionAccessPattern]
		static void TestNullType ()
		{
			Type type = null;
			var constructors = type.GetConstructors ();
		}

		[Kept]
		static Type FindType ()
		{
			return null;
		}

		[UnrecognizedReflectionAccessPattern (typeof (Type), nameof (Type.GetConstructors), new Type[] { typeof (BindingFlags) },
			messageCode: "IL2075", message: new string[] { "FindType", "GetConstructors" })]
		[Kept]
		static void TestDataFlowType ()
		{
			Type type = FindType ();
			var constructors = type.GetConstructors (BindingFlags.Public);
		}

		[Kept]
		[RecognizedReflectionAccessPattern]
		private static void TestDataFlowWithAnnotation ([KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))][DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)] Type type)
		{
			var constructors = type.GetConstructors (BindingFlags.Public | BindingFlags.Static);
		}

		[Kept]
		[RecognizedReflectionAccessPattern]
		static void TestIfElse (bool decision)
		{
			Type myType;
			if (decision) {
				myType = typeof (IfConstructor);
			} else {
				myType = typeof (ElseConstructor);
			}
			var constructors = myType.GetConstructors (BindingFlags.Public);
		}

		[Kept]
		private class SimpleGetConstructors
		{
			[Kept]
			public SimpleGetConstructors ()
			{ }

			[Kept]
			public SimpleGetConstructors (int i)
			{ }

			private SimpleGetConstructors (string foo)
			{ }

			protected SimpleGetConstructors (string foo, string bar)
			{ }
		}

		[Kept]
		private class ConstructorsBindingFlags
		{
			[Kept]
			public ConstructorsBindingFlags ()
			{ }

			[Kept]
			public ConstructorsBindingFlags (string bar)
			{ }

			private ConstructorsBindingFlags (int foo)
			{ }

			protected ConstructorsBindingFlags (int foo, int bar)
			{ }

			internal ConstructorsBindingFlags (int foo, int bar, int baz)
			{ }
		}

		[Kept]
		private class UnknownBindingFlags
		{
			[Kept]
			public UnknownBindingFlags ()
			{ }

			[Kept]
			public UnknownBindingFlags (string bar)
			{ }

			[Kept]
			private UnknownBindingFlags (int foo)
			{ }

			[Kept]
			protected UnknownBindingFlags (int foo, int bar)
			{ }

			[Kept]
			internal UnknownBindingFlags (int foo, int bar, int baz)
			{ }
		}

		[Kept]
		private class MyType
		{
			[Kept]
			public MyType ()
			{ }

			[Kept]
			public MyType (string bar)
			{ }

			private MyType (int foo)
			{ }

			protected MyType (int foo, int bar)
			{ }

			internal MyType (int foo, int bar, int baz)
			{ }
		}

		[Kept]
		private class IfConstructor
		{
			[Kept]
			public IfConstructor ()
			{ }

			[Kept]
			public IfConstructor (int foo)
			{ }

			private IfConstructor (string foo)
			{ }

			protected IfConstructor (int foo, int bar)
			{ }

			internal IfConstructor (int foo, string bar)
			{ }
		}

		[Kept]
		private class ElseConstructor
		{
			[Kept]
			public ElseConstructor ()
			{ }

			[Kept]
			public ElseConstructor (int foo)
			{ }

			private ElseConstructor (string foo)
			{ }

			protected ElseConstructor (int foo, int bar)
			{ }

			internal ElseConstructor (int foo, string bar)
			{ }
		}
	}
}