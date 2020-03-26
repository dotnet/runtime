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
			private string UnknownString ()
			{
				return "unknownstring";
			}

			[Kept]
			public void Branch_SystemTypeValueNode_KnownStringValue_NonStatic ()
			{
				var expr = Expression.Property (Expression.Parameter (typeof (int), "somename"), typeof (Foo), "TestName1");
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
				var expr = Expression.Property (null, UnknownString () == "unknownstring" ? null : typeof (Foo), "TestName1");
			}

			[UnrecognizedReflectionAccessPattern (
				typeof (Expression), nameof (Expression.Property), new Type [] { typeof (Expression), typeof (Type), typeof (string) })]
			[Kept]
			public void Branch_MethodParameterValueNode (Type T, string S)
			{
				var expr = Expression.Property (null, T, "TestName1");
				expr = Expression.Property (null, typeof (Foo), S);
			}

			[UnrecognizedReflectionAccessPattern (
				typeof (Expression), nameof (Expression.Property), new Type [] { typeof (Expression), typeof (Type), typeof (string) })]
			[Kept]
			public void Branch_UnrecognizedPatterns ()
			{
				var expr = Expression.Property (null, Type.GetType ("Foo"), "TestName1");
			}
		}
	}
}