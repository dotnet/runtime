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
	[ExpectedNoWarnings]
	public class ExpressionPropertyString
	{
		[ExpectedWarning ("IL2026", nameof (BasicProperty))]
		[ExpectedWarning ("IL2111", "ProtectedPropertyOnBase")]
		[ExpectedWarning ("IL2026", "PublicPropertyOnBase")]
		[ExpectedWarning ("IL2072", nameof (Expression) + "." + nameof (Expression.Property))]
		public static void Main ()
		{
			Expression.Property (Expression.Parameter (typeof (int), ""), typeof (ExpressionPropertyString), nameof (BasicProperty));
			Expression.Property (null, typeof (ExpressionPropertyString), nameof (StaticProperty));
			Expression.Property (null, typeof (Derived), "ProtectedPropertyOnBase");
			Expression.Property (null, typeof (Derived), "PublicPropertyOnBase");
			UnknownType.Test ();
			TestNull ();
			TestNoValue ();
			TestNullString ();
			TestEmptyString ();
			TestNoValueString ();
			UnknownString.Test ();
			Expression.Property (null, GetType (), "This string will not be reached"); // IL2072
		}

		[Kept]
		private int BasicProperty {
			[Kept]
			[ExpectBodyModified]
			get;

			[Kept]
			[ExpectBodyModified]
			[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
			[RequiresUnreferencedCode (nameof (BasicProperty))]
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
		static void TestNull ()
		{
			Type t = null;
			Expression.Property (null, t, "This string will not be reached");
		}

		[Kept]
		static void TestNoValue ()
		{
			Type t = null;
			Type noValue = Type.GetTypeFromHandle (t.TypeHandle);
			Expression.Property (null, noValue, "This string will not be reached");
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
		static void TestNullString ()
		{
			Expression.Property (null, typeof (Base), null);
		}

		[Kept]
		static void TestEmptyString ()
		{
			Expression.Property (null, typeof (Base), string.Empty);
		}

		[Kept]
		static void TestNoValueString ()
		{
			Type t = null;
			string noValue = t.AssemblyQualifiedName;
			Expression.Property (null, typeof (Base), noValue);
		}

		[Kept]
		class Base
		{
			[Kept]
			[KeptBackingField]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			protected static Type ProtectedPropertyOnBase {
				[Kept]
				get;

				[Kept]
				set;
			}

			[Kept]
			[KeptBackingField]
			public static bool PublicPropertyOnBase {
				[Kept]
				get;

				[Kept]
				[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
				[RequiresUnreferencedCode (nameof (PublicPropertyOnBase))]
				set;
			}
		}

		[Kept]
		[KeptBaseType (typeof (Base))]
		class Derived : Base
		{
		}
	}
}