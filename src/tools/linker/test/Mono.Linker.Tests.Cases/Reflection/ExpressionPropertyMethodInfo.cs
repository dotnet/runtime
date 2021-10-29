// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
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
			public static int StaticProperty {
				[Kept]
				get;
				[Kept]
				set;
			}

			[Kept]
			[KeptBackingField]
			public static int StaticPropertyViaReflection {
				[Kept]
				get;
				[Kept]
				set;
			}

			[Kept]
			[KeptBackingField]
			public static int StaticPropertyViaRuntimeMethod {
				[Kept]
				get;
				[Kept]
				set;
			}

			[Kept]
			[KeptBackingField]
			public int InstanceProperty {
				[Kept]
				get;
				[Kept]
				set;
			}

			[Kept]
			[KeptBackingField]
			public int InstancePropertyViaReflection {
				[Kept]
				get;
				[Kept]
				set;
			}

			[Kept]
			[RecognizedReflectionAccessPattern]
			public static void Test ()
			{
				Expression<Func<int>> staticGetter = () => StaticProperty;

				Expression.Property (null, typeof (PropertyGetter).GetMethod ("get_StaticPropertyViaReflection"));

				PropertyGetter instance = new PropertyGetter ();
				Expression<Func<PropertyGetter, int>> instanceGetter = i => i.InstanceProperty;

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
			public static int StaticProperty {
				[Kept]
				get;
				[Kept]
				set;
			}

			[Kept]
			[KeptBackingField]
			public static int StaticPropertyViaRuntimeMethod {
				[Kept]
				get;
				[Kept]
				set;
			}

			[Kept]
			[KeptBackingField]
			public int InstanceProperty {
				[Kept]
				get;
				[Kept]
				set;
			}

			[Kept]
			[RecognizedReflectionAccessPattern]
			public static void Test ()
			{
				Expression.Property (null, typeof (PropertySetter).GetMethod ("set_StaticProperty"));

				Expression.Property (null, typeof (PropertySetter).GetRuntimeMethod ("set_StaticPropertyViaRuntimeMethod", Type.EmptyTypes));

				Expression.Property (Expression.New (typeof (PropertySetter)), typeof (PropertySetter).GetMethod ("set_InstanceProperty"));
			}
		}

		[Kept]
		[RecognizedReflectionAccessPattern]
		static void TestNull ()
		{
			MethodInfo mi = null;
			Expression.Property (null, mi);
		}

		[Kept]
		[UnrecognizedReflectionAccessPattern (
			typeof (Expression), nameof (Expression.Property), new Type[] { typeof (Expression), typeof (MethodInfo) },
			messageCode: "IL2103")]
		static void TestNonPropertyMethod ()
		{
			Expression.Property (null, typeof (ExpressionPropertyMethodInfo).GetMethod (nameof (TestNonPropertyMethod), BindingFlags.NonPublic | BindingFlags.Static));
		}

		[Kept]
		[RecognizedReflectionAccessPattern]
		static void TestNonExistentMethod ()
		{
			Expression.Property (null, typeof (ExpressionPropertyMethodInfo).GetMethod ("NonExistent"));
		}

		[Kept]
		class MultipleMethods
		{
			[Kept]
			[KeptBackingField]
			public static int StaticProperty {
				[Kept]
				get;
				[Kept]
				set;
			}

			[Kept]
			[KeptBackingField]
			public static int SecondStaticProperty {
				[Kept]
				get;
				[Kept]
				set;
			}

			[Kept]
			[RecognizedReflectionAccessPattern]
			public static void Test (int p)
			{
				MethodInfo mi;
				switch (p) {
				case 0:
					mi = typeof (MultipleMethods).GetMethod ("get_StaticProperty");
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
		[UnrecognizedReflectionAccessPattern (
			typeof (Expression), nameof (Expression.Property), new Type[] { typeof (Expression), typeof (MethodInfo) },
			messageCode: "IL2103")]
		static void TestUnknownMethod (MethodInfo mi)
		{
			Expression.Property (null, mi);
		}
	}
}
