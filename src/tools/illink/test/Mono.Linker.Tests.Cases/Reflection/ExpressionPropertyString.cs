using Mono.Linker.Tests.Cases.Expectations.Assertions;
using System;
using System.Linq.Expressions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Reflection
{
	// Explicitly use roslyn to try and get a compiler that supports defining `TestOnlyStatic2` without a setter
	[SetupCSharpCompilerToUse ("csc")]
	[Reference ("System.Core.dll")]
	public class ExpressionPropertyString
	{
		public static void Main ()
		{
			// So that this test works with or without unreachable bodies
			var Foo = new Foo ();

			Foo.Branch_SystemTypeValueNode_KnownStringValue_NonStatic ();
			Foo.Branch_SystemTypeValueNode_KnownStringValue_SaticOnly ();
			Foo.Branch_SystemTypeValueNode_UnknownStringValue ();
			Foo.Branch_NullValueNode ();
			Foo.Branch_MethodParameterValueNode (typeof (Foo), "Foo");
			Foo.Branch_UnrecognizedPatterns ();
		}

		[KeptMember (".ctor()")]
		class Foo
		{
			[Kept]
			[KeptBackingField]
			private static int TestOnlyStatic1 { [Kept] get; [Kept] set; }

			private int TestOnlyStatic2 { get; }

			[Kept]
			[KeptBackingField]
			private int TestName1 { [Kept] get; }

			[Kept]
			[KeptBackingField]
			private int TestName2 { [Kept] get; }

			private int TestName3 { get; }

			private int TestName4 { get; }

			private int TestName5 { get; }

			[Kept]
			class A
			{
				[Kept]
				[KeptBackingField]
				static int Foo { [Kept] get; }
			}

			[Kept]
			class B
			{
				[Kept]
				[KeptBackingField]
				static int Foo { [Kept] get; }
			}

			[Kept]
			private string UnknownString ()
			{
				return "unknownstring";
			}

			[Kept]
			public void Branch_SystemTypeValueNode_KnownStringValue_NonStatic ()
			{
				TestPropertyName (0);
				TestPropertyName (1);
				TestType (0);
				TestType (1);
			}

			[Kept]
			static void TestPropertyName (int i)
			{
				string PropertyName = string.Empty;
				switch (i) {
					case 0:
						PropertyName = "TestName1";
						break;
					case 1:
						PropertyName = "TestName2";
						break;
					default:
						break;
				}

				Expression.Property (Expression.Parameter (typeof (int), "somename"), typeof (Foo), PropertyName);
			}

			[Kept]
			static void TestType (int i)
			{
				Type T = (Type)null;
				switch (i) {
					case 0:
						T = typeof (A);
						break;
					case 1:
						T = typeof (B);
						break;
					default:
						break;
				}

				Expression.Property (null, T, "Foo");
			}

			[Kept]
			public void Branch_SystemTypeValueNode_KnownStringValue_SaticOnly ()
			{
				var expr = Expression.Property (null, typeof (Foo), "TestOnlyStatic1");
				expr = Expression.Property (null, typeof (Foo), "TestOnlyStatic2");
			}

			[UnrecognizedReflectionAccessPattern (
				typeof (Expression), nameof (Expression.Property), new Type [] { typeof (Expression), typeof (Type), typeof (string) })]
			[Kept]
			public void Branch_SystemTypeValueNode_UnknownStringValue ()
			{
				var expr = Expression.Property (null, typeof (Foo), UnknownString ());
			}

			[UnrecognizedReflectionAccessPattern (
				typeof (Expression), nameof (Expression.Property), new Type [] { typeof (Expression), typeof (Type), typeof (string) })]
			[Kept]
			public void Branch_NullValueNode ()
			{
				var expr = Expression.Property (null, (Type)null, "TestName3");
			}

			[UnrecognizedReflectionAccessPattern (
				typeof (Expression), nameof (Expression.Property), new Type [] { typeof (Expression), typeof (Type), typeof (string) })]
			[Kept]
			public void Branch_MethodParameterValueNode (Type T, string s)
			{
				var expr = Expression.Property (null, T, "TestName4");
				expr = Expression.Property (null, typeof (Foo), s);
			}

			[UnrecognizedReflectionAccessPattern (
				typeof (Expression), nameof (Expression.Property), new Type [] { typeof (Expression), typeof (Type), typeof (string) })]
			[Kept]
			public void Branch_UnrecognizedPatterns ()
			{
				var expr = Expression.Property (null, Type.GetType ("Foo"), "TestName5");
			}
		}
	}
}