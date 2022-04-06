using System;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Reflection
{

	[SetupCSharpCompilerToUse ("csc")]
	[ExpectedNoWarnings]
	[SetupLinkerArgument ("--disable-opt", "unreachablebodies")]
	public class MethodUsedViaReflection
	{
		public static void Main ()
		{
			GetMethod_Name.TestName ();
			GetMethod_Name.TestNamePrivate ();
			GetMethod_Name_Types.TestNameAndType ();
			GetMethod_Name_BindingAttr.TestExplicitBindingFlags ();
			GetMethod_Name_BindingAttr.TestUnknownBindingFlags (BindingFlags.Public);
			GetMethod_Name_BindingAttr.TestUnknownBindingFlagsAndName (BindingFlags.Public, "DoesntMatter");
			GetMethod_Name_BindingAttr.TestUnknownNullBindingFlags (BindingFlags.Public);
			GetMethod_Name_BindingAttr.TestWrongBindingFlags ();
			GetMethod_Name_BindingAttr.TestNullName ();
			GetMethod_Name_BindingAttr.TestUnknownName ("Unknown");
			GetMethod_Name_BindingAttr.TestUnknownNameAndWrongBindingFlags ("Unknown");
			GetMethod_Name_BindingAttr_Binder_Types_Modifiers.TestNameBindingFlagsAndParameterModifier ();
			GetMethod_Name_BindingAttr_Binder_CallConvention_Types_Modifiers.TestNameBindingFlagsCallingConventionParameterModifier ();
#if NETCOREAPP
			GetMethod_Name_BindingAttr_Types.TestNameBindingFlagsAndTypes ();
			GetMethod_Name_GenericParameterCount_Types.TestNameWithIntAndType ();
			GetMethod_Name_GenericParameterCount_Types_Modifiers.TestNameWithIntAndTypeAndModifiers ();
			GetMethod_Name_GenericParameterCount_BindingAttr_Binder_Types_Modifiers.TestNameWithIntAndBindingFlags ();
			GetMethod_Name_GenericParameterCount_BindingAttr_Binder_Types_Modifiers_PrivateBinding.TestNameWithIntAndPrivateBindingFlags ();
			GetMethod_Name_GenericParameterCount_BindingAttr_Binder_CallConvention_Types_Modifiers.TestNameWithIntBindingFlagsCallingConventionParameter ();
#endif
			TestNullName ();
			TestEmptyName ();
			TestNoValueName ();
			TestNonExistingName ();
			TestNullType ();
			TestNoValue ();
			TestDataFlowType ();
			IfElse.TestIfElse (1);
			DerivedAndBase.TestMethodInBaseType ();
			IgnoreCaseBindingFlags.TestIgnoreCaseBindingFlags ();
			FailIgnoreCaseBindingFlags.TestFailIgnoreCaseBindingFlags ();
			IgnorableBindingFlags.TestIgnorableBindingFlags ();
			UnsupportedBindingFlags.TestUnsupportedBindingFlags ();
		}

		// GetMethod(string name)
		[Kept]
		class GetMethod_Name
		{
			[Kept]
			public static int OnlyCalledViaReflection ()
			{
				return 42;
			}

			[Kept]
			public int OnlyCalledViaReflection (int _, int __)
			{
				return 42;
			}

			private int OnlyCalledViaReflection (int _)
			{
				return 42;
			}

			private static int PrivateMethod ()
			{
				return 42;
			}

			[Kept]
			public static void TestName ()
			{
				var method = typeof (GetMethod_Name).GetMethod ("OnlyCalledViaReflection");
				method.Invoke (null, new object[] { });
			}

			[Kept]
			public static void TestNamePrivate ()
			{
				// This should fail at runtime, since GetMethod(name) only works on public methods
				// also means linker should not mark the PrivateMethod in this case
				var method = typeof (GetMethod_Name).GetMethod ("PrivateMethod");
				method.Invoke (null, new object[] { });
			}
		}

		// GetMethod(string name, Type[] types)
		[Kept]
		class GetMethod_Name_Types
		{
			[Kept]
			public static int OnlyCalledViaReflection ()
			{
				return 42;
			}

			private int OnlyCalledViaReflection (int foo)
			{
				return 43;
			}

			[Kept]
			public int OnlyCalledViaReflection (int foo, int bar)
			{
				return 44;
			}

			[Kept]
			public static int OnlyCalledViaReflection (int foo, int bar, int baz)
			{
				return 45;
			}

			[Kept]
			public static void TestNameAndType ()
			{
				// Currently linker doesn't analyze the Type[] parameter and thus it marks all methods with the name and matching binding flags (public in this case)
				var method = typeof (GetMethod_Name_Types).GetMethod ("OnlyCalledViaReflection", new Type[] { });
				method.Invoke (null, new object[] { });
			}
		}

		// GetMethod(string name, BindingFlags bindingAttr)
		[Kept]
		class GetMethod_Name_BindingAttr
		{
			[Kept]
			public static int OnlyCalledViaReflection ()
			{
				return 42;
			}

			private int OnlyCalledViaReflection (int foo)
			{
				return 43;
			}

			public int OnlyCalledViaReflection (int foo, int bar)
			{
				return 44;
			}

			[Kept]
			public static int OnlyCalledViaReflection (int foo, int bar, int baz)
			{
				return 45;
			}

			[Kept]
			public static void TestExplicitBindingFlags ()
			{
				var method = typeof (GetMethod_Name_BindingAttr).GetMethod ("OnlyCalledViaReflection", BindingFlags.Static | BindingFlags.Public);
				method.Invoke (null, new object[] { });
			}

			[Kept]
			class UnknownBindingFlags
			{
				[Kept]
				private static int OnlyCalledViaReflection ()
				{
					return 42;
				}

				[Kept]
				private int OnlyCalledViaReflection (int foo)
				{
					return 43;
				}

				[Kept]
				public int OnlyCalledViaReflection (int foo, int bar)
				{
					return 44;
				}

				[Kept]
				public static int OnlyCalledViaReflection (int foo, int bar, int baz)
				{
					return 45;
				}
			}

			[Kept]
			public static void TestUnknownBindingFlags (BindingFlags bindingFlags)
			{
				// Since the binding flags are not known linker should mark all methods on the type
				var method = typeof (UnknownBindingFlags).GetMethod ("OnlyCalledViaReflection", bindingFlags);
				method.Invoke (null, new object[] { });
			}

			[Kept]
			class UnknownBindingFlagsAndName
			{
				[Kept]
				private static int OnlyCalledViaReflection ()
				{
					return 42;
				}
			}

			[Kept]
			public static void TestUnknownBindingFlagsAndName (BindingFlags bindingFlags, string name)
			{
				// Since the binding flags and name are not known linker should mark all methods on the type
				var method = typeof (UnknownBindingFlagsAndName).GetMethod (name, bindingFlags);
				method.Invoke (null, new object[] { });
			}

			[Kept]
			private class NullBindingFlags
			{
				[Kept]
				private static int OnlyCalledViaReflection ()
				{
					return 42;
				}

				[Kept]
				private int OnlyCalledViaReflection (int foo)
				{
					return 43;
				}

				[Kept]
				public int OnlyCalledViaReflection (int foo, int bar)
				{
					return 44;
				}

				[Kept]
				public static int OnlyCalledViaReflection (int foo, int bar, int baz)
				{
					return 45;
				}
			}

			[Kept]
			public static void TestUnknownNullBindingFlags (BindingFlags bindingFlags)
			{
				// The case here is a pattern which linker doesn't recognize (unlike the test case above, which passes a recognized
				// method parameter with unknown value). Unrecognized patterns are internally represented as unknown values which are passed
				// around as nulls in some cases. So there's a potential risk of hitting a nullref. The test here is to validate that
				// linker can accept such value for binding flags.
				// The semantic is exactly the same as above, that is unknown value and thus all methods should be marked.

				// One way to produce unrecognized pattern is to use some bitfield arithmetics - linker currently doesn't do constexpr evaluation
				// and then store it in a local.
				var bf = bindingFlags | BindingFlags.Static;

				var method = typeof (NullBindingFlags).GetMethod ("OnlyCalledViaReflection", bf);
				method.Invoke (null, new object[] { });
			}

			[Kept]
			private class WrongBindingFlags
			{
				// Unnecessarily kept: https://github.com/dotnet/linker/issues/2432
				[Kept]
				private static void One () { }

				// Unnecessarily kept: https://github.com/dotnet/linker/issues/2432
				[Kept]
				public static void Two () { }
			}

			[Kept]
			public static void TestWrongBindingFlags ()
			{
				// Specifying just Static will never return anything (Public or NonPublic is required on top)
				// So this doesn't need to mark anything.
				typeof (WrongBindingFlags).GetMethod ("One", BindingFlags.Static);
				typeof (WrongBindingFlags).GetMethod ("Two", BindingFlags.Static);
			}

			[Kept]
			private class NullName
			{
				private static void Known () { }

				public void AlsoKnown () { }
			}

			[Kept]
			public static void TestNullName ()
			{
				// null will actually throw exception at runtime, so there's no need to mark anything
				typeof (NullName).GetMethod (null, BindingFlags.Public);
			}

			[Kept]
			private class UnknownName
			{
				private static void Known () { }

				[Kept]
				public void AlsoKnown () { }
			}

			[Kept]
			public static void TestUnknownName (string name)
			{
				typeof (UnknownName).GetMethod (name, BindingFlags.Public);
			}

			[Kept]
			private class UnknownNameAndWrongBindingFlags
			{
				private static void Known () { }

				public void AlsoKnown () { }
			}

			[Kept]
			public static void TestUnknownNameAndWrongBindingFlags (string name)
			{
				// The binding flags like this will not return any methods (it's a valid call, but never returns anything)
				// So it's OK to not mark any method due to this.
				typeof (UnknownNameAndWrongBindingFlags).GetMethod (name, BindingFlags.Static);
			}
		}

		// GetMethod(string name, BindingFlags bindingAttr, Binder binder, Type[] types, ParameterModifier[] modifiers)
		[Kept]
		class GetMethod_Name_BindingAttr_Binder_Types_Modifiers
		{
			private static int OnlyCalledViaReflection ()
			{
				return 42;
			}

			private int OnlyCalledViaReflection (int foo)
			{
				return 43;
			}
			[Kept]
			public int OnlyCalledViaReflection (int foo, int bar)
			{
				return 44;
			}
			[Kept]
			public static int OnlyCalledViaReflection (int foo, int bar, int baz)
			{
				return 45;
			}

			[Kept]
			public static void TestNameBindingFlagsAndParameterModifier ()
			{
				var method = typeof (GetMethod_Name_BindingAttr_Binder_Types_Modifiers).GetMethod ("OnlyCalledViaReflection", BindingFlags.Public, null, new Type[] { }, null);
				method.Invoke (null, new object[] { });
			}
		}

		// GetMethod(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
		[Kept]
		class GetMethod_Name_BindingAttr_Binder_CallConvention_Types_Modifiers
		{
			[Kept]
			private static int OnlyCalledViaReflection ()
			{
				return 42;
			}
			[Kept]
			private int OnlyCalledViaReflection (int foo)
			{
				return 43;
			}
			public int OnlyCalledViaReflection (int foo, int bar)
			{
				return 44;
			}
			public static int OnlyCalledViaReflection (int foo, int bar, int baz)
			{
				return 45;
			}

			[Kept]
			public static void TestNameBindingFlagsCallingConventionParameterModifier ()
			{
				var method = typeof (GetMethod_Name_BindingAttr_Binder_CallConvention_Types_Modifiers).GetMethod ("OnlyCalledViaReflection", BindingFlags.NonPublic, null, CallingConventions.Standard, new Type[] { }, null);
				method.Invoke (null, new object[] { });
			}
		}

#if NETCOREAPP
		// GetMethod(string name, BindingFlags bindingAttr, Type[] types)
		[Kept]
		class GetMethod_Name_BindingAttr_Types
		{
			private static int OnlyCalledViaReflection ()
			{
				return 42;
			}

			private int OnlyCalledViaReflection (int foo)
			{
				return 43;
			}
			[Kept]
			public int OnlyCalledViaReflection (int foo, int bar)
			{
				return 44;
			}
			[Kept]
			public static int OnlyCalledViaReflection (int foo, int bar, int baz)
			{
				return 45;
			}

			[Kept]
			public static void TestNameBindingFlagsAndTypes ()
			{
				var method = typeof (GetMethod_Name_BindingAttr_Types).GetMethod ("OnlyCalledViaReflection", BindingFlags.Public, new Type[] { });
				method.Invoke (null, new object[] { });
			}
		}

		// GetMethod(string name, int genericParameterCount, Type[] types)
		[Kept]
		class GetMethod_Name_GenericParameterCount_Types
		{
			private static int OnlyCalledViaReflection ()
			{
				return 42;
			}
			private int OnlyCalledViaReflection (int foo)
			{
				return 43;
			}
			[Kept]
			public int OnlyCalledViaReflection (int foo, int bar)
			{
				return 44;
			}
			[Kept]
			public static int OnlyCalledViaReflection (int foo, int bar, int baz)
			{
				return 45;
			}

			[Kept]
			public static void TestNameWithIntAndType ()
			{
				var method = typeof (GetMethod_Name_GenericParameterCount_Types).GetMethod ("OnlyCalledViaReflection", 1, new Type[] { typeof (int) });
				method.Invoke (null, new object[] { });
			}
		}

		// GetMethod(string name, int genericParameterCount, Type[] types, ParameterModifier[] modifiers)
		[Kept]
		class GetMethod_Name_GenericParameterCount_Types_Modifiers
		{
			private static int OnlyCalledViaReflection ()
			{
				return 42;
			}
			private int OnlyCalledViaReflection (int foo)
			{
				return 43;
			}
			[Kept]
			public int OnlyCalledViaReflection (int foo, int bar)
			{
				return 44;
			}
			[Kept]
			public static int OnlyCalledViaReflection (int foo, int bar, int baz)
			{
				return 45;
			}

			[Kept]
			public static void TestNameWithIntAndTypeAndModifiers ()
			{
				var method = typeof (GetMethod_Name_GenericParameterCount_Types_Modifiers).GetMethod ("OnlyCalledViaReflection", 1, new Type[] { typeof (int) }, null);
				method.Invoke (null, new object[] { });
			}
		}

		// GetMethod(string name, int genericParameterCount, BindingFlags bindingAttr, Binder binder, Type[] types, ParameterModifier[] modifiers)
		[Kept]
		class GetMethod_Name_GenericParameterCount_BindingAttr_Binder_Types_Modifiers
		{
			private static int OnlyCalledViaReflection ()
			{
				return 42;
			}
			private int OnlyCalledViaReflection (int foo)
			{
				return 43;
			}
			[Kept]
			public int OnlyCalledViaReflection (int foo, int bar)
			{
				return 44;
			}
			[Kept]
			public static int OnlyCalledViaReflection (int foo, int bar, int baz)
			{
				return 45;
			}

			[Kept]
			public static void TestNameWithIntAndBindingFlags ()
			{
				var method = typeof (GetMethod_Name_GenericParameterCount_BindingAttr_Binder_Types_Modifiers)
					.GetMethod ("OnlyCalledViaReflection", 1, BindingFlags.Public, null, new Type[] { }, null);
				method.Invoke (null, new object[] { });
			}
		}

		[Kept]
		class GetMethod_Name_GenericParameterCount_BindingAttr_Binder_Types_Modifiers_PrivateBinding
		{
			[Kept]
			private static int OnlyCalledViaReflection ()
			{
				return 42;
			}

			[Kept]
			private int OnlyCalledViaReflection (int foo)
			{
				return 43;
			}

			public int OnlyCalledViaReflection (int foo, int bar)
			{
				return 44;
			}
			public static int OnlyCalledViaReflection (int foo, int bar, int baz)
			{
				return 45;
			}

			[Kept]
			public static void TestNameWithIntAndPrivateBindingFlags ()
			{
				var method = typeof (GetMethod_Name_GenericParameterCount_BindingAttr_Binder_Types_Modifiers_PrivateBinding)
					.GetMethod ("OnlyCalledViaReflection", 1, BindingFlags.NonPublic, null, new Type[] { typeof (int) }, null);
				method.Invoke (null, new object[] { 42 });
			}
		}

		// GetMethod(string name, int genericParameterCount, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
		[Kept]
		class GetMethod_Name_GenericParameterCount_BindingAttr_Binder_CallConvention_Types_Modifiers
		{
			[Kept]
			private static int OnlyCalledViaReflection ()
			{
				return 42;
			}
			private int OnlyCalledViaReflection (int foo)
			{
				return 43;
			}
			public int OnlyCalledViaReflection (int foo, int bar)
			{
				return 44;
			}
			public static int OnlyCalledViaReflection (int foo, int bar, int baz)
			{
				return 45;
			}

			[Kept]
			public static void TestNameWithIntBindingFlagsCallingConventionParameter ()
			{
				var method = typeof (GetMethod_Name_GenericParameterCount_BindingAttr_Binder_CallConvention_Types_Modifiers).GetMethod ("OnlyCalledViaReflection", 1, BindingFlags.Static | BindingFlags.NonPublic, null, CallingConventions.Any, new Type[] { }, null);
				method.Invoke (null, new object[] { });
			}
		}
#endif

		[Kept]
		static void TestNullName ()
		{
			var method = typeof (MethodUsedViaReflection).GetMethod (null);
		}

		[Kept]
		static void TestEmptyName ()
		{
			var method = typeof (MethodUsedViaReflection).GetMethod (string.Empty);
		}

		[Kept]
		static void TestNoValueName ()
		{
			Type t = null;
			string noValue = t.AssemblyQualifiedName;
			var method = typeof (MethodUsedViaReflection).GetMethod (noValue);
		}

		[Kept]
		static void TestNonExistingName ()
		{
			var method = typeof (MethodUsedViaReflection).GetMethod ("NonExisting");
		}

		[Kept]
		static void TestNullType ()
		{
			Type type = null;
			var method = type.GetMethod ("OnlyCalledViaReflection", BindingFlags.Static | BindingFlags.Public);
		}

		[Kept]
		static void TestNoValue ()
		{
			Type t = null;
			Type noValue = Type.GetTypeFromHandle (t.TypeHandle);
			var method = noValue.GetMethod ("OnlyCalledViaReflection", BindingFlags.Static | BindingFlags.Public);
		}

		[Kept]
		static Type FindType ()
		{
			return typeof (MethodUsedViaReflection);
		}

		[Kept]
		[ExpectedWarning ("IL2075", "FindType", "GetMethod")]
		static void TestDataFlowType ()
		{
			Type type = FindType ();
			var method = type.GetMethod ("OnlyCalledViaReflection", BindingFlags.Static | BindingFlags.Public);
		}

		[Kept]
		class IfElse
		{
			[Kept]
			private class IfClass
			{
				[Kept]
				public static int OnlyCalledViaReflection ()
				{
					return 42;
				}

				private int OnlyCalledViaReflection (int foo)
				{
					return 43;
				}
				[Kept]
				public static int ElseIfCall ()
				{
					return 44;
				}
			}

			[Kept]
			private class ElseIfClass
			{
				[Kept]
				public static int OnlyCalledViaReflection ()
				{
					return 45;
				}
				[Kept]
				private static int OnlyCalledViaReflection (int foo)
				{
					return 46;
				}
				[Kept]
				public static int ElseIfCall ()
				{
					return 47;
				}
			}

			[Kept]
			private class ElseClass
			{
				[Kept]
				public static int OnlyCalledViaReflection ()
				{
					return 48;
				}
				[Kept]
				private static int OnlyCalledViaReflection (int foo)
				{
					return 49;
				}
				private int ElseIfCall ()
				{
					return 50;
				}
			}

			[Kept]
			public static void TestIfElse (int i)
			{
				Type myType;
				if (i == 1) {
					myType = typeof (IfClass);
				} else if (i == 2) {
					myType = typeof (ElseIfClass);
				} else {
					myType = typeof (ElseClass);
				}
				string mystring;
				if (i == 1) {
					mystring = "OnlyCalledViaReflection";
				} else if (i == 2) {
					mystring = "ElseIfCall";
				} else {
					mystring = null;
				}
				var method = myType.GetMethod (mystring, BindingFlags.Static, null, new Type[] { typeof (int) }, null);
				method.Invoke (null, new object[] { });
			}
		}

		[Kept]
		class DerivedAndBase
		{
			[Kept]
			class BaseClass
			{
				[Kept]
				public int OnlyCalledViaReflection ()
				{
					return 51;
				}
			}

			[Kept]
			[KeptBaseType (typeof (BaseClass))]
			class DerivedClass : BaseClass
			{ }

			[Kept]
			public static void TestMethodInBaseType ()
			{
				var method = typeof (DerivedClass).GetMethod ("OnlyCalledViaReflection");
				method.Invoke (null, new object[] { });
			}
		}

		[Kept]
		class IgnoreCaseBindingFlags
		{
			[Kept]
			public int OnlyCalledViaReflection ()
			{
				return 52;
			}
			[Kept]
			public string MarkedDueToIgnoreCase ()
			{
				return "52";
			}

			[Kept]
			public static void TestIgnoreCaseBindingFlags ()
			{
				var method = typeof (IgnoreCaseBindingFlags).GetMethod ("onlycalledviareflection", BindingFlags.IgnoreCase | BindingFlags.Public);
				method.Invoke (null, new object[] { });
			}
		}

		[Kept]
		class FailIgnoreCaseBindingFlags
		{
			public int OnlyCalledViaReflection ()
			{
				return 53;
			}

			[Kept]
			public static void TestFailIgnoreCaseBindingFlags ()
			{
				var method = typeof (FailIgnoreCaseBindingFlags).GetMethod ("onlycalledviareflection", BindingFlags.Public);
				method.Invoke (null, new object[] { });
			}
		}

		[Kept]
		class IgnorableBindingFlags
		{
			[Kept]
			public int OnlyCalledViaReflection ()
			{
				return 54;
			}

			private bool Unmarked ()
			{
				return true;
			}

			[Kept]
			public static void TestIgnorableBindingFlags ()
			{
				var method = typeof (IgnorableBindingFlags).GetMethod ("OnlyCalledViaReflection", BindingFlags.Public | BindingFlags.InvokeMethod);
				method.Invoke (null, new object[] { });
			}
		}

		[Kept]
		class UnsupportedBindingFlags
		{
			[Kept]
			public int OnlyCalledViaReflection ()
			{
				return 54;
			}

			[Kept]
			private bool MarkedDueToChangeType ()
			{
				return true;
			}

			[Kept]
			public static void TestUnsupportedBindingFlags ()
			{
				var method = typeof (UnsupportedBindingFlags).GetMethod ("OnlyCalledViaReflection", BindingFlags.Public | BindingFlags.SuppressChangeType);
				method.Invoke (null, new object[] { });
			}
		}

		public static int Unused ()
		{
			return 42;
		}
	}
}
