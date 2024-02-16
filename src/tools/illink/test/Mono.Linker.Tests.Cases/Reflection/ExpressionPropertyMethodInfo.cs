// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Reflection
{
	[ExpectedNoWarnings]
	public class ExpressionPropertyMethodInfo
	{
		public static void Main ()
		{
			PropertyGetter.Test ();
			PropertySetter.Test ();
			TestNull ();
			TestNonPropertyMethod ();
			TestNonExistentMethod ();
			MultipleMethods.Test (0);
			TestUnknownMethod (null);
		}

		[Kept]
		[KeptMember (".ctor()")]
		class PropertyGetter
		{
			[Kept]
			[KeptBackingField]
			public static int StaticPropertyExpressionAccess {
				[Kept]
				get;
				[Kept]
				[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
				[RequiresUnreferencedCode (nameof (StaticPropertyExpressionAccess))]
				set;
			}

			[Kept]
			[KeptBackingField]
			public static int StaticPropertyViaReflection {
				[Kept]
				get;
				[Kept]
				[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
				[RequiresUnreferencedCode (nameof (StaticPropertyViaReflection))]
				set;
			}

			[Kept]
			[KeptBackingField]
			public static int StaticPropertyViaRuntimeMethod {
				[Kept]
				get;
				[Kept]
				[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
				[RequiresUnreferencedCode (nameof (StaticPropertyViaRuntimeMethod))]
				set;
			}

			[Kept]
			[KeptBackingField]
			public int InstancePropertyExpressionAccess {
				[Kept]
				get;
				[Kept]
				[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
				[RequiresUnreferencedCode (nameof (InstancePropertyExpressionAccess))]
				set;
			}

			[Kept]
			[KeptBackingField]
			public int InstancePropertyViaReflection {
				[Kept]
				get;
				[Kept]
				[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
				[RequiresUnreferencedCode (nameof (InstancePropertyViaReflection))]
				set;
			}

			[Kept]
			// https://github.com/dotnet/linker/issues/2669
			[ExpectedWarning ("IL2026", nameof (StaticPropertyExpressionAccess), ProducedBy = Tool.Trimmer)]
			[ExpectedWarning ("IL2026", nameof (StaticPropertyViaReflection))]
			[ExpectedWarning ("IL2026", nameof (StaticPropertyViaRuntimeMethod))]
			// https://github.com/dotnet/linker/issues/2669
			[ExpectedWarning ("IL2026", nameof (InstancePropertyExpressionAccess), ProducedBy = Tool.Trimmer)]
			[ExpectedWarning ("IL2026", nameof (InstancePropertyViaReflection))]
			public static void Test ()
			{
				Expression<Func<int>> staticGetter = () => StaticPropertyExpressionAccess;

				Expression.Property (null, typeof (PropertyGetter).GetMethod ("get_StaticPropertyViaReflection"));

				PropertyGetter instance = new PropertyGetter ();
				Expression<Func<PropertyGetter, int>> instanceGetter = i => i.InstancePropertyExpressionAccess;

				Expression.Property (Expression.New (typeof (PropertyGetter)), typeof (PropertyGetter).GetMethod ("get_InstancePropertyViaReflection"));

				Expression.Property (null, typeof (PropertyGetter).GetRuntimeMethod ("get_StaticPropertyViaRuntimeMethod", Type.EmptyTypes));
			}
		}

		[Kept]
		[KeptMember (".ctor()")]
		class PropertySetter
		{
			[Kept]
			[KeptBackingField]
			public static int StaticPropertyReflectionAccess {
				[Kept]
				[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
				[RequiresUnreferencedCode (nameof (StaticPropertyReflectionAccess))]
				get;
				[Kept]
				set;
			}

			[Kept]
			[KeptBackingField]
			public static int StaticPropertyViaRuntimeMethod {
				[Kept]
				[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
				[RequiresUnreferencedCode (nameof (StaticPropertyViaRuntimeMethod))]
				get;
				[Kept]
				set;
			}

			[Kept]
			[KeptBackingField]
			public int InstancePropertyReflectionAccess {
				[Kept]
				[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
				[RequiresUnreferencedCode (nameof (InstancePropertyReflectionAccess))]
				get;
				[Kept]
				set;
			}

			[Kept]
			[ExpectedWarning ("IL2026", nameof (StaticPropertyReflectionAccess))]
			[ExpectedWarning ("IL2026", nameof (StaticPropertyViaRuntimeMethod))]
			[ExpectedWarning ("IL2026", nameof (InstancePropertyReflectionAccess))]
			public static void Test ()
			{
				Expression.Property (null, typeof (PropertySetter).GetMethod ("set_StaticPropertyReflectionAccess"));

				Expression.Property (null, typeof (PropertySetter).GetRuntimeMethod ("set_StaticPropertyViaRuntimeMethod", Type.EmptyTypes));

				Expression.Property (Expression.New (typeof (PropertySetter)), typeof (PropertySetter).GetMethod ("set_InstancePropertyReflectionAccess"));
			}
		}

		[Kept]
		static void TestNull ()
		{
			MethodInfo mi = null;
			Expression.Property (null, mi);
		}

		[Kept]
		[ExpectedWarning ("IL2103", nameof (Expression) + "." + nameof (Expression.Property))]
		static void TestNonPropertyMethod ()
		{
			Expression.Property (null, typeof (ExpressionPropertyMethodInfo).GetMethod (nameof (TestNonPropertyMethod), BindingFlags.NonPublic | BindingFlags.Static));
		}

		[Kept]
		static void TestNonExistentMethod ()
		{
			Expression.Property (null, typeof (ExpressionPropertyMethodInfo).GetMethod ("NonExistent"));
		}

		[Kept]
		class MultipleMethods
		{
			[Kept]
			[KeptBackingField]
			public static int FirstStaticProperty {
				[Kept]
				get;
				[Kept]
				[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
				[RequiresUnreferencedCode (nameof (FirstStaticProperty))]
				set;
			}

			[Kept]
			[KeptBackingField]
			public static int SecondStaticProperty {
				[Kept]
				get;
				[Kept]
				[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
				[RequiresUnreferencedCode (nameof (SecondStaticProperty))]
				set;
			}

			[Kept]
			[ExpectedWarning ("IL2026", nameof (FirstStaticProperty))]
			[ExpectedWarning ("IL2026", nameof (SecondStaticProperty))]
			public static void Test (int p)
			{
				MethodInfo mi;
				switch (p) {
				case 0:
					mi = typeof (MultipleMethods).GetMethod ("get_FirstStaticProperty");
					break;
				case 1:
					mi = typeof (MultipleMethods).GetMethod ("get_SecondStaticProperty");
					break;
				default:
					mi = null;
					break;
				}

				Expression.Property (null, mi);
			}
		}

		[Kept]
		[ExpectedWarning ("IL2103", nameof (Expression) + "." + nameof (Expression.Property))]
		static void TestUnknownMethod (MethodInfo mi)
		{
			Expression.Property (null, mi);
		}
	}
}
