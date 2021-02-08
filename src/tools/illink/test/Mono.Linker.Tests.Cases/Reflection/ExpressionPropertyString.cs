using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Reflection
{
	// Explicitly use roslyn to try and get a compiler that supports defining a static property without a setter
	[SetupCSharpCompilerToUse ("csc")]
	[Reference ("System.Core.dll")]
	public class ExpressionPropertyString
	{
		[UnrecognizedReflectionAccessPattern (typeof (Expression), nameof (Expression.Property),
			new Type[] { typeof (Expression), typeof (Type), typeof (string) }, messageCode: "IL2072")]
		public static void Main ()
		{
			Expression.Property (Expression.Parameter (typeof (int), ""), typeof (ExpressionPropertyString), "Property");
			Expression.Property (null, typeof (ExpressionPropertyString), "StaticProperty");
			Expression.Property (null, typeof (Derived), "ProtectedPropertyOnBase");
			Expression.Property (null, typeof (Derived), "PublicPropertyOnBase");
			UnknownType.Test ();
			UnknownString.Test ();
			Expression.Property (null, GetType (), "This string will not be reached"); // UnrecognizedReflectionAccessPattern
		}

		[Kept]
		private int Property {
			[Kept]
			[ExpectBodyModified]
			get;

			[Kept]
			[ExpectBodyModified]
			set;
		}

		[Kept]
		[KeptBackingField]
		static private int StaticProperty {
			[Kept]
			get;
		}

		private int UnusedProperty {
			get;
		}

		[Kept]
		static Type GetType ()
		{
			return typeof (int);
		}

		[Kept]
		class UnknownType
		{
			[Kept]
			[KeptBackingField]
			public static int Property1 {
				[Kept]
				get;
			}

			[Kept]
			private int Property2 {
				[Kept]
				[ExpectBodyModified]
				get;
			}

			[Kept]
			public static void Test ()
			{
				Expression.Property (null, GetType (), "This string will not be reached");
			}

			[Kept]
			[return: KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
			static Type GetType ()
			{
				return typeof (UnknownType);
			}
		}

		[Kept]
		class UnknownString
		{
			[Kept]
			[KeptBackingField]
			private static int Property1 {
				[Kept]
				get;
			}

			[Kept]
			public int Property2 {
				[Kept]
				[ExpectBodyModified]
				get;
			}

			[Kept]
			public static void Test ()
			{
				Expression.Property (null, typeof (UnknownString), GetString ());
			}

			[Kept]
			static string GetString ()
			{
				return "UnknownString";
			}
		}

		[Kept]
		class Base
		{
			[Kept]
			[KeptBackingField]
			protected static bool ProtectedPropertyOnBase { [Kept] get; }

			[Kept]
			[KeptBackingField]
			public static bool PublicPropertyOnBase { [Kept] get; }
		}

		[Kept]
		[KeptBaseType (typeof (Base))]
		class Derived : Base
		{
		}
	}
}