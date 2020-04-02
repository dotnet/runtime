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
			public void Branch_SystemTypeValueNode_KnownStringValue_NonStatic ()
			{
				TestPropertyName (0);
				TestPropertyName (1);
				TestByType (0);
				TestByType (1);
				StaticPropertyExpected ();
			}

			[Kept]
			public void Branch_NullValueNode ()
			{
				var expr = Expression.Property (null, (Type)null, "TestName1");
			}

			#region RecognizedReflectionAccessPattern
			[RecognizedReflectionAccessPattern (
				typeof (Expression), nameof (Expression.Property), new Type [] { typeof (Expression), typeof (Type), typeof (string) },
				typeof (Foo), nameof (TestOnlyStatic1))]
			[Kept]
			public void Branch_SystemTypeValueNode_KnownStringValue_SaticOnly ()
			{
				var expr = Expression.Property (null, typeof (Foo), "TestOnlyStatic1");
			}

			[RecognizedReflectionAccessPattern (
				typeof (Expression), nameof (Expression.Property), new Type [] { typeof (Expression), typeof (Type), typeof (string) },
				typeof (Foo), nameof (TestName2))]
			[RecognizedReflectionAccessPattern (
				typeof (Expression), nameof (Expression.Property), new Type [] { typeof (Expression), typeof (Type), typeof (string) },
				typeof (Foo), nameof (TestName3))]
			[Kept]
			static void TestPropertyName (int i)
			{
				string PropertyName = null;
				switch (i) {
					case 0:
						PropertyName = "TestName2";
						break;
					case 1:
						PropertyName = "TestName3";
						break;
					default:
						break;
				}

				Expression.Property (Expression.Parameter (typeof (int), "somename"), typeof (Foo), PropertyName);
			}

			[RecognizedReflectionAccessPattern (
				typeof (Expression), nameof (Expression.Property), new Type [] { typeof (Expression), typeof (Type), typeof (string) },
				typeof (A), "Foo")]
			[RecognizedReflectionAccessPattern (
				typeof (Expression), nameof (Expression.Property), new Type [] { typeof (Expression), typeof (Type), typeof (string) },
				typeof (B), "Foo")]
			[Kept]
			static void TestByType (int i)
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
			#endregion

			#region UnrecognizedReflectionAccessPatterns
			[UnrecognizedReflectionAccessPattern (
				typeof (Expression), nameof (Expression.Property), new Type [] { typeof (Expression), typeof (Type), typeof (string) })]
			[Kept]
			public void StaticPropertyExpected ()
			{
				var expr = Expression.Property (null, typeof (Foo), "TestOnlyStatic2");
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
			[UnrecognizedReflectionAccessPattern (
				typeof (Expression), nameof (Expression.Property), new Type [] { typeof (Expression), typeof (Type), typeof (string) })]
			[Kept]
			public void Branch_MethodParameterValueNode (Type T, string s)
			{
				var expr = Expression.Property (null, T, "TestName4");
				expr = Expression.Property (null, typeof (Foo), s);
			}

			[UnrecognizedReflectionAccessPattern (
				typeof (Type), nameof (Type.GetType), new Type [] { typeof (string) })]
			[UnrecognizedReflectionAccessPattern (
				typeof (Expression), nameof (Expression.Property), new Type [] { typeof (Expression), typeof (Type), typeof (string) })]
			[Kept]
			public void Branch_UnrecognizedPatterns ()
			{
				var expr = Expression.Property (null, Type.GetType ("Foo"), "TestName5");
			}
			#endregion

			#region Helpers
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
			[KeptBackingField]
			private static int TestOnlyStatic1 { [Kept] get; [Kept] set; }

			private int TestOnlyStatic2 { get; }

			private int TestName1 { get; }

			[Kept]
			[KeptBackingField]
			private int TestName2 { [Kept] get; }

			[Kept]
			[KeptBackingField]
			private int TestName3 { [Kept] get; }

			private int TestName4 { get; }

			private int TestName5 { get; }

			[Kept]
			private string UnknownString ()
			{
				return "unknownstring";
			}
			#endregion
		}
	}
}