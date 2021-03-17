using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Reflection
{
	[SetupCSharpCompilerToUse ("csc")]
	public class ConstructorUsedViaReflection
	{
		public static void Main ()
		{
			GetConstructor_Types.Test ();
			GetConstructor_BindingAttr_Binder_Types_Modifiers.TestWithBindingFlags ();
			GetConstructor_BindingAttr_Binder_Types_Modifiers.TestWithUnknownBindingFlags (BindingFlags.Public);
			GetConstructor_BindingAttr_Binder_CallConvention_Types_Modifiers.TestWithCallingConvention ();
#if NETCOREAPP
			GetConstructor_BindingAttr_Types.Test ();
#endif
			TestNullType ();
			TestDataFlowType ();
			IfElse.TestIfElse (true);
		}

		[Kept]
		private class GetConstructor_Types
		{
			[Kept]
			public GetConstructor_Types ()
			{ }

			[Kept]
			public GetConstructor_Types (int i)
			{ }

			private GetConstructor_Types (string foo)
			{ }

			protected GetConstructor_Types (string foo, string bar)
			{ }

			[Kept]
			class EmptyTypes
			{
				[Kept]
				public EmptyTypes () { }
				public EmptyTypes (int i) { }
			}

			[Kept]
			public static void Test ()
			{
				TestConstructorWithTypes_EmptyTypes ();
				TestConstructorWithTypes_NonEmptyTypes ();
				TestConstructorWithTypes_EmptyTypes_DataFlow (typeof (TestType));
				TestConstructorWithTypes_NonEmptyTypes_DataFlow (typeof (TestType));
			}

			[Kept]
			[RecognizedReflectionAccessPattern (
				typeof (Type), nameof (Type.GetConstructor), new Type[] { typeof (Type[]) },
				typeof (EmptyTypes), ".ctor", new Type[0])]
			static void TestConstructorWithTypes_EmptyTypes ()
			{
				var constructor = typeof (EmptyTypes).GetConstructor (new Type[] { });
				constructor.Invoke (null, new object[] { });
			}

			[Kept]
			[RecognizedReflectionAccessPattern]
			static void TestConstructorWithTypes_NonEmptyTypes ()
			{
				var constructor = typeof (GetConstructor_Types).GetConstructor (new Type[] { typeof (int) });
				constructor.Invoke (null, new object[] { null });
			}

			[Kept]
			[RecognizedReflectionAccessPattern]
			static void TestConstructorWithTypes_EmptyTypes_DataFlow (
				[KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))]
				[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
				Type type)
			{
				var constructor = type.GetConstructor (new Type[] { });
				constructor.Invoke (null, new object[] { });
			}

			[Kept]
			[UnrecognizedReflectionAccessPattern (
				typeof (Type), nameof (Type.GetConstructor), new Type[] { typeof (Type[]) }, messageCode: "IL2070")]
			static void TestConstructorWithTypes_NonEmptyTypes_DataFlow (
				[KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))]
				[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
				Type type)
			{
				var constructor = type.GetConstructor (new Type[] { typeof (int) });
				constructor.Invoke (null, new object[] { null });
			}
		}

		[Kept]
		class GetConstructor_BindingAttr_Binder_Types_Modifiers
		{
			[Kept]
			class KnownBindingFlags
			{
				[Kept]
				public KnownBindingFlags ()
				{ }

				public KnownBindingFlags (string bar)
				{ }

				private KnownBindingFlags (int foo)
				{ }

				protected KnownBindingFlags (int foo, int bar)
				{ }

				internal KnownBindingFlags (int foo, int bar, int baz)
				{ }
			}

			[Kept]
			[RecognizedReflectionAccessPattern (
				typeof (Type), nameof (Type.GetConstructor), new Type[] { typeof (BindingFlags), typeof (Binder), typeof (Type[]), typeof (ParameterModifier[]) },
				typeof (KnownBindingFlags), ".ctor", new Type[0])]
			public static void TestWithBindingFlags ()
			{
				var constructor = typeof (KnownBindingFlags).GetConstructor (BindingFlags.Public, GetNullValue ("some argument", 2, 3), new Type[] { }, new ParameterModifier[] { });
				constructor.Invoke (null, new object[] { });
			}

			[Kept]
			class UnknownBindingFlags
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
			[RecognizedReflectionAccessPattern (typeof (Type), nameof (Type.GetConstructor), new Type[] { typeof (BindingFlags), typeof (Binder), typeof (Type[]), typeof (ParameterModifier[]) },
					typeof (UnknownBindingFlags), ".ctor", new Type[0])]
			public static void TestWithUnknownBindingFlags (BindingFlags bindingFlags)
			{
				// Since the binding flags are not known linker should mark all constructors on the type
				var constructor = typeof (UnknownBindingFlags).GetConstructor (bindingFlags, GetNullValue ("some argument", 2, 3), new Type[] { }, new ParameterModifier[] { });
				constructor.Invoke (null, new object[] { });
			}
		}

		[Kept]
		class GetConstructor_BindingAttr_Binder_CallConvention_Types_Modifiers
		{
			[Kept]
			public GetConstructor_BindingAttr_Binder_CallConvention_Types_Modifiers ()
			{ }

			public GetConstructor_BindingAttr_Binder_CallConvention_Types_Modifiers (string bar)
			{ }

			private GetConstructor_BindingAttr_Binder_CallConvention_Types_Modifiers (int foo)
			{ }

			protected GetConstructor_BindingAttr_Binder_CallConvention_Types_Modifiers (int foo, int bar)
			{ }

			internal GetConstructor_BindingAttr_Binder_CallConvention_Types_Modifiers (int foo, int bar, int baz)
			{ }

			[Kept]
			[RecognizedReflectionAccessPattern (
				typeof (Type), nameof (Type.GetConstructor), new Type[] { typeof (BindingFlags), typeof (Binder), typeof (CallingConventions), typeof (Type[]), typeof (ParameterModifier[]) },
				typeof (GetConstructor_BindingAttr_Binder_CallConvention_Types_Modifiers), ".ctor", new Type[0])]
			public static void TestWithCallingConvention ()
			{
				var constructor = typeof (GetConstructor_BindingAttr_Binder_CallConvention_Types_Modifiers).GetConstructor (BindingFlags.Public, GetNullValue ("some argument", 2, 3), CallingConventions.HasThis, new Type[] { }, new ParameterModifier[] { });
				constructor.Invoke (null, new object[] { });
			}
		}

#if NETCOREAPP
		[Kept]
		class GetConstructor_BindingAttr_Types
		{
			[Kept]
			public GetConstructor_BindingAttr_Types ()
			{ }

			public GetConstructor_BindingAttr_Types (string bar)
			{ }

			private GetConstructor_BindingAttr_Types (int foo)
			{ }

			protected GetConstructor_BindingAttr_Types (int foo, int bar)
			{ }

			internal GetConstructor_BindingAttr_Types (int foo, int bar, int baz)
			{ }

			[Kept]
			class NonEmptyTypes
			{
				[Kept]
				public NonEmptyTypes () { }
				[Kept]
				public NonEmptyTypes (int i) { }
			}

			[Kept]
			public static void Test ()
			{
				TestWithBindingFlagsAndTypes_EmptyTypes ();
				TestWithBindingFlagsAndTypes_NonEmptyTypes ();
				TestWithBindingFlagsAndTypes_EmptyTypes_DataFlow (null);
				TestWithBindingFlagsAndTypes_NonEmptyTypes_DataFlow (null);
			}

			[Kept]
			[RecognizedReflectionAccessPattern (
				typeof (Type), nameof (Type.GetConstructor), new Type[] { typeof (BindingFlags), typeof (Type[]) },
				typeof (GetConstructor_BindingAttr_Types), ".ctor", new Type[0])]
			static void TestWithBindingFlagsAndTypes_EmptyTypes ()
			{
				var constructor = typeof (GetConstructor_BindingAttr_Types).GetConstructor (BindingFlags.Public, new Type[] { });
				constructor.Invoke (null, new object[] { });
			}

			[Kept]
			[RecognizedReflectionAccessPattern]
			static void TestWithBindingFlagsAndTypes_NonEmptyTypes ()
			{
				var constructor = typeof (NonEmptyTypes).GetConstructor (BindingFlags.Public, new Type[] { typeof (TestType) });
				constructor.Invoke (null, new object[] { null });
			}

			[Kept]
			[RecognizedReflectionAccessPattern]
			static void TestWithBindingFlagsAndTypes_EmptyTypes_DataFlow (
				[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
				[KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))]
				Type type)
			{
				var constructor = type.GetConstructor (BindingFlags.Public, new Type[] { });
			}

			[Kept]
			[UnrecognizedReflectionAccessPattern (
				typeof (Type), nameof (Type.GetConstructor), new Type[] { typeof (BindingFlags), typeof (Type[]) }, messageCode: "IL2070")]
			static void TestWithBindingFlagsAndTypes_NonEmptyTypes_DataFlow (
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
				[KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))]
				Type type)
			{
				var constructor = type.GetConstructor (BindingFlags.Public, new Type[] { typeof (TestType) });
			}
		}
#endif

		[Kept]
		static void TestNullType ()
		{
			Type type = null;
			var constructor = type.GetConstructor (new Type[] { });
		}

		[Kept]
		static Type FindType ()
		{
			return null;
		}

		[UnrecognizedReflectionAccessPattern (typeof (Type), nameof (Type.GetConstructor), new Type[] { typeof (Type[]) },
			messageCode: "IL2075", message: new string[] { "FindType", "GetConstructor" })]
		[Kept]
		static void TestDataFlowType ()
		{
			Type type = FindType ();
			var constructor = type.GetConstructor (new Type[] { });
		}

		[Kept]
		class IfElse
		{
			[Kept]
			private class IfConstructor
			{
				[Kept]
				public IfConstructor ()
				{ }

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

				public ElseConstructor (int foo)
				{ }

				private ElseConstructor (string foo)
				{ }

				protected ElseConstructor (int foo, int bar)
				{ }

				internal ElseConstructor (int foo, string bar)
				{ }
			}

			[Kept]
			[RecognizedReflectionAccessPattern (
				typeof (Type), nameof (Type.GetConstructor), new Type[] { typeof (BindingFlags), typeof (Binder), typeof (Type[]), typeof (ParameterModifier[]) },
				typeof (IfConstructor), ".ctor", new Type[0])]
			[RecognizedReflectionAccessPattern (
				typeof (Type), nameof (Type.GetConstructor), new Type[] { typeof (BindingFlags), typeof (Binder), typeof (Type[]), typeof (ParameterModifier[]) },
				typeof (ElseConstructor), ".ctor", new Type[0])]
			public static void TestIfElse (bool decision)
			{
				Type myType;
				if (decision) {
					myType = typeof (IfConstructor);
				} else {
					myType = typeof (ElseConstructor);
				}
				var constructor = myType.GetConstructor (BindingFlags.Public, null, new Type[] { }, null);
				constructor.Invoke (null, new object[] { });
			}
		}

		[Kept]
		static Binder GetNullValue (string str, int i, long g)
		{
			return null;
		}

		[Kept]
		[KeptMember (".ctor()")]
		class TestType
		{
		}
	}
}