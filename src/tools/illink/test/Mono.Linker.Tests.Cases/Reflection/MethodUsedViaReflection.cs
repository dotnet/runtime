using System;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Reflection {

	[SetupCSharpCompilerToUse ("csc")]
	/*[VerifyAllReflectionAccessPatternsAreValidated]*/
	public class MethodUsedViaReflection {
		public static void Main ()
		{
			TestName ();
			TestNameAndExplicitBindingFlags ();
			TestNameAndType ();
			TestNameWithIntAndType ();
			TestNameWithIntAndBindingFlags ();
			TestNullName ();
			TestEmptyName ();
			TestNonExistingName ();
			TestNullType ();
			TestDataFlowType ();
		}

		[Kept]
		[RecognizedReflectionAccessPattern (
			typeof(Type), nameof(Type.GetMethod), new Type [] { typeof (string) },
			typeof(MethodUsedViaReflection), nameof(MethodUsedViaReflection.OnlyCalledViaReflection), new Type[0])]
		static void TestName()
		{
			var method = typeof (MethodUsedViaReflection).GetMethod ("OnlyCalledViaReflection");
			method.Invoke (null, new object [] { });
		}
		[Kept]
		[RecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetMethod), new Type [] { typeof (string), typeof (BindingFlags) },
			typeof (TestNameAndExplicitBindingClass), nameof (TestNameAndExplicitBindingClass.OnlyCalledViaReflection), new Type [0])]
		static void TestNameAndExplicitBindingFlags ()
		{
			var method = typeof (TestNameAndExplicitBindingClass).GetMethod ("OnlyCalledViaReflection", BindingFlags.Static | BindingFlags.NonPublic);
			method.Invoke (null, new object [] { });
		}
		
		[Kept]
		static void TestNameAndType ()
		{
			var method = typeof (TestNameAndTypeClass).GetMethod ("OnlyCalledViaReflection", new Type [] { typeof (int) });
			method.Invoke (null, new object [] { });
		}

		[Kept]
		static void TestNameWithIntAndType ()
		{
			var method = typeof (TestNameWithIntAndTypeClass).GetMethod ("OnlyCalledViaReflection", 1, new Type [] { typeof(int) });
			method.Invoke (null, new object [] { });
		}

		[Kept]
		static void TestNameWithIntAndBindingFlags ()
		{
			var method = typeof (TestNameWithIntAndBindingClass).GetMethod ("OnlyCalledViaReflection", 1, BindingFlags.Static | BindingFlags.NonPublic, null, new Type [] { typeof (int) }, null);
			method.Invoke (null, new object [] { });
		}

		[Kept]
		[UnrecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetMethod), new Type [] { typeof (string) })]
		static void TestNullName ()
		{
			var method = typeof (MethodUsedViaReflection).GetMethod (null);
		}

		[Kept]
		[UnrecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetMethod), new Type [] { typeof (string) })]
		static void TestEmptyName ()
		{
			var method = typeof (MethodUsedViaReflection).GetMethod (string.Empty);
		}

		[Kept]
		[UnrecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetMethod), new Type [] { typeof (string) })]
		static void TestNonExistingName ()
		{
			var method = typeof (MethodUsedViaReflection).GetMethod ("NonExisting");
		}

		[Kept]
		[UnrecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetMethod), new Type [] { typeof (string), typeof (BindingFlags) })]
		static void TestNullType ()
		{
			Type type = null;
			var method = type.GetMethod ("OnlyCalledViaReflection", BindingFlags.Static | BindingFlags.NonPublic);
		}

		[Kept]
		static Type FindType ()
		{
			return typeof (MethodUsedViaReflection);
		}

		[Kept]
		[UnrecognizedReflectionAccessPattern (
			typeof (Type), nameof (Type.GetMethod), new Type [] { typeof (string), typeof (BindingFlags) })]
		static void TestDataFlowType ()
		{
			Type type = FindType ();
			var method = type.GetMethod ("OnlyCalledViaReflection", BindingFlags.Static | BindingFlags.NonPublic);
		}

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
		[Kept]
		private class TestNameAndExplicitBindingClass
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
		}

		[Kept]
		private class TestNameAndTypeClass
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
		private class TestNameWithIntAndTypeClass
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
		private class TestNameWithIntAndBindingClass
		{
			/*At this moment due to int parameter everything will be kept*/
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
	}
}
