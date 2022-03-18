using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Reflection
{

	[SetupCSharpCompilerToUse ("csc")]
	[ExpectedNoWarnings]
	[SetupLinkerArgument ("--disable-opt", "unreachablebodies")]
	public class MethodsUsedViaReflection
	{
		public static void Main ()
		{
			TestGetMethods ();
			TestBindingFlags ();
			TestUnknownBindingFlags (BindingFlags.Public);
			TestNullType ();
			TestNoValue ();
			TestDataFlowType ();
			TestDataFlowWithAnnotation (typeof (MyType));
			TestIfElse (1);
			TestIgnoreCaseBindingFlags ();
			TestIgnorableBindingFlags ();
			TestUnsupportedBindingFlags ();

			HandlingOfComplexExpressionForBindingFlags.Test ();
			HandlingOfBindingFlagsAsNumbers.Test ();
			HandlingOfBindingFlagsFromConstants.Test ();
		}

		[Kept]
		static void TestGetMethods ()
		{
			var methods = typeof (MethodsUsedViaReflection).GetMethods ();
		}

		[Kept]
		static void TestBindingFlags ()
		{
			var methods = typeof (TestBindingClass).GetMethods (BindingFlags.Static | BindingFlags.Public);
		}

		[Kept]
		static void TestUnknownBindingFlags (BindingFlags bindingFlags)
		{
			// Since the binding flags are not known linker should mark all methods on the type
			var methods = typeof (TestUnknownBindingClass).GetMethods (bindingFlags);
		}

		[Kept]
		static void TestNullType ()
		{
			Type type = null;
			var methods = type.GetMethods (BindingFlags.Static | BindingFlags.Public);
		}

		[Kept]
		static void TestNoValue ()
		{
			Type t = null;
			Type noValue = Type.GetTypeFromHandle (t.TypeHandle);
			var methods = noValue.GetMethods (BindingFlags.Static | BindingFlags.Public);
		}

		[Kept]
		static Type FindType ()
		{
			return typeof (MethodsUsedViaReflection);
		}

		[Kept]
		[ExpectedWarning ("IL2075", "FindType", "GetMethods")]
		static void TestDataFlowType ()
		{
			Type type = FindType ();
			var methods = type.GetMethods (BindingFlags.Static | BindingFlags.Public);
		}

		[Kept]
		private static void TestDataFlowWithAnnotation ([KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))][DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
		{
			var methods = type.GetMethods (BindingFlags.Public | BindingFlags.Static);
		}

		[Kept]
		static void TestIfElse (int i)
		{
			Type myType;
			if (i == 1) {
				myType = typeof (IfClass);
			} else if (i == 2) {
				myType = typeof (ElseIfClass);
			} else {
				myType = typeof (ElseClass);
			}
			var methods = myType.GetMethods (BindingFlags.Public);
		}

		[Kept]
		static void TestIgnoreCaseBindingFlags ()
		{
			var methods = typeof (IgnoreCaseClass).GetMethods (BindingFlags.IgnoreCase | BindingFlags.Public);
		}

		[Kept]
		static void TestIgnorableBindingFlags ()
		{
			var methods = typeof (InvokeMethodClass).GetMethods (BindingFlags.Public | BindingFlags.InvokeMethod);
		}

		[Kept]
		static void TestUnsupportedBindingFlags ()
		{
			var methods = typeof (SuppressChangeTypeClass).GetMethods (BindingFlags.Public | BindingFlags.SuppressChangeType);
		}

		[Kept]
		public static int OnlyCalledViaReflection ()
		{
			return 42;
		}

		[Kept]
		public int OnlyCalledViaReflection (int foo)
		{
			return 43;
		}

		// This one will not be kept as we're only ever ask for public methods of this name
		int OnlyCalledViaReflection (int foo, int bar)
		{
			return 44;
		}

		[Kept]
		public static int OnlyCalledViaReflection (int foo, int bar, int baz)
		{
			return 45;
		}

		[Kept]
		private class TestBindingClass
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
		}

		[Kept]
		private class TestUnknownBindingClass
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
		private class MyType
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
			public static int SomeotherFunc ()
			{
				return 44;
			}
		}

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
			public int OnlyCalledViaReflection ()
			{
				return 45;
			}
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
		private class IgnoreCaseClass
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
		}

		[Kept]
		private class InvokeMethodClass
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
		}

		[Kept]
		private class SuppressChangeTypeClass
		{
			[Kept]
			public int OnlyCalledViaReflection ()
			{
				return 54;
			}

			[Kept]
			private bool MarkedDueToSuppressChangeType ()
			{
				return true;
			}
		}

		[Kept]
		class HandlingOfComplexExpressionForBindingFlags
		{
			[Kept]
			class TestClassWithRUCMethods
			{
				[Kept]
				public void Method () { }

				[Kept]
				[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
				[RequiresUnreferencedCode (nameof (HandlingOfComplexExpressionForBindingFlags) + "--" + nameof (TestClassWithRUCMethods))]
				private void PrivateMethodWithRUC () { }
			}

			[Kept]
			// https://github.com/dotnet/linker/issues/2638
			[ExpectedWarning ("IL2026", ProducedBy = ProducedBy.Trimmer)]
			public static void Test ()
			{
				BindingFlags left = BindingFlags.Instance | BindingFlags.Static;
				BindingFlags right = BindingFlags.Public;
				int result = (int) left | (int) right;
				typeof (TestClassWithRUCMethods).GetMethods ((BindingFlags) result);
			}
		}

		[Kept]
		class HandlingOfBindingFlagsAsNumbers
		{
			[Kept]
			class TestClassWithRUCMethods
			{
				[Kept]
				public static void Method () { }

				[RequiresUnreferencedCode (nameof (HandlingOfBindingFlagsAsNumbers) + "--" + nameof (TestClassWithRUCMethods))]
				private static void PrivateMethodWithRUC () { }
			}

			[Kept]
			public static void Test ()
			{
				typeof (TestClassWithRUCMethods).GetMethods ((BindingFlags) 24);

				// Analyzer currently can't figure this out
				int bindingFlagsNumber = 24;
				typeof (TestClassWithRUCMethods).GetMethods ((BindingFlags) bindingFlagsNumber);
			}
		}

		[Kept]
		class HandlingOfBindingFlagsFromConstants
		{
			[Kept]
			class TestClassWithRUCMethods
			{
				[Kept]
				public static void Method () { }

				[RequiresUnreferencedCode (nameof (HandlingOfBindingFlagsAsNumbers) + "--" + nameof (TestClassWithRUCMethods))]
				private static void PrivateMethodWithRUC () { }
			}

			const BindingFlags PublicStaticFlags = BindingFlags.Public | BindingFlags.Static;
			const BindingFlags PublicOnlyFlags = BindingFlags.Public;

			[Kept]
			public static void Test ()
			{
				typeof (TestClassWithRUCMethods).GetMethods (PublicStaticFlags);
				typeof (TestClassWithRUCMethods).GetMethods (PublicOnlyFlags | BindingFlags.Static);
			}
		}
	}
}
