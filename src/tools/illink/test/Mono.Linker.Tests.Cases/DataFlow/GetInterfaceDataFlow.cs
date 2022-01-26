// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	public class GetInterfaceDataFlow
	{
		public static void Main ()
		{
			GetInterface_Name.Test ();
			GetInterface_Name_IgnoreCase.Test ();
		}

		class GetInterface_Name
		{
			[ExpectedWarning ("IL2070", nameof (Type.GetInterface), nameof (DynamicallyAccessedMemberTypes) + "." + nameof (DynamicallyAccessedMemberTypes.Interfaces))]
			static void TestNoAnnotation (Type type)
			{
				type.GetInterface ("ITestInterface");
			}

			[ExpectedWarning ("IL2072", nameof (DataFlowTypeExtensions.RequiresAll), nameof (DynamicallyAccessedMemberTypes) + "." + nameof (DynamicallyAccessedMemberTypes.All))]
			static void TestWithAnnotation ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.Interfaces)] Type type)
			{
				type.GetInterface ("ITestInterface").RequiresInterfaces ();
				type.GetInterface ("ITestInterface").RequiresAll (); // Warns
			}

			static void TestWithAll ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type type)
			{
				type.GetInterface ("ITestInterface").RequiresInterfaces ();
				type.GetInterface ("ITestInterface").RequiresAll ();
			}

			[ExpectedWarning ("IL2072", nameof (DataFlowTypeExtensions.RequiresAll), nameof (DynamicallyAccessedMemberTypes) + "." + nameof (DynamicallyAccessedMemberTypes.All))]
			static void TestKnownType ()
			{
				// Interfaces marking is transitive - meaning that it marks all interfaces in the entire hierarchy
				// so the return value of GetInterface always has all interfaces on it already.
				typeof (TestType).GetInterface ("ITestInterface").RequiresInterfaces ();
				typeof (TestType).GetInterface ("ITestInterface").RequiresAll (); // Warns
			}

			[ExpectedWarning ("IL2072", nameof (DataFlowTypeExtensions.RequiresAll), nameof (DynamicallyAccessedMemberTypes) + "." + nameof (DynamicallyAccessedMemberTypes.All))]
			static void TestMultipleValues (int p, [DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type typeWithAll)
			{
				Type type;
				if (p == 0)
					type = typeof (TestType);
				else
					type = typeWithAll;

				type.GetInterface ("ITestInterface").RequiresInterfaces ();
				type.GetInterface ("ITestInterface").RequiresAll (); // Warns since only one of the values is guaranteed All
			}

			[ExpectedWarning ("IL2075", nameof (DynamicallyAccessedMemberTypes) + "." + nameof (DynamicallyAccessedMemberTypes.Interfaces))]
			static void TestMergedValues (int p, [DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type typeWithAll)
			{
				Type type = new TestType ().GetType ();
				if (p == 0) {
					type = typeWithAll;
				}

				type.GetInterface ("ITestInterface").RequiresInterfaces ();
			}

			public static void Test ()
			{
				TestNoAnnotation (typeof (TestType));
				TestWithAnnotation (typeof (TestType));
				TestWithAnnotation (typeof (ITestInterface));
				TestWithAll (typeof (TestType));
				TestKnownType ();
				TestMultipleValues (0, typeof (TestType));
				TestMergedValues (0, typeof (TestType));
			}
		}

		class GetInterface_Name_IgnoreCase
		{
			[ExpectedWarning ("IL2070", nameof (Type.GetInterface), nameof (DynamicallyAccessedMemberTypes) + "." + nameof (DynamicallyAccessedMemberTypes.Interfaces))]
			static void TestNoAnnotation (Type type)
			{
				type.GetInterface ("ITestInterface", false);
			}

			[ExpectedWarning ("IL2072", nameof (DataFlowTypeExtensions.RequiresAll), nameof (DynamicallyAccessedMemberTypes) + "." + nameof (DynamicallyAccessedMemberTypes.All))]
			static void TestWithAnnotation ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.Interfaces)] Type type)
			{
				type.GetInterface ("ITestInterface", false).RequiresInterfaces ();
				type.GetInterface ("ITestInterface", false).RequiresAll (); // Warns
			}

			static void TestWithAll ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type type)
			{
				type.GetInterface ("ITestInterface", false).RequiresInterfaces ();
				type.GetInterface ("ITestInterface", false).RequiresAll ();
			}

			[ExpectedWarning ("IL2072", nameof (DataFlowTypeExtensions.RequiresAll), nameof (DynamicallyAccessedMemberTypes) + "." + nameof (DynamicallyAccessedMemberTypes.All))]
			static void TestKnownType ()
			{
				// Interfaces marking is transitive - meaning that it marks all interfaces in the entire hierarchy
				// so the return value of GetInterface always has all interfaces on it already.
				typeof (TestType).GetInterface ("ITestInterface", false).RequiresInterfaces ();
				typeof (TestType).GetInterface ("ITestInterface", false).RequiresAll ();
			}

			[ExpectedWarning ("IL2072", nameof (DataFlowTypeExtensions.RequiresAll), nameof (DynamicallyAccessedMemberTypes) + "." + nameof (DynamicallyAccessedMemberTypes.All))]
			static void TestMultipleValues (int p, [DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type typeWithAll)
			{
				Type type;
				if (p == 0)
					type = typeof (TestType);
				else
					type = typeWithAll;

				type.GetInterface ("ITestInterface", false).RequiresInterfaces ();
				type.GetInterface ("ITestInterface", false).RequiresAll (); // Warns since only one of the values is guaranteed All
			}

			public static void Test ()
			{
				TestNoAnnotation (typeof (TestType));
				TestWithAnnotation (typeof (TestType));
				TestWithAnnotation (typeof (ITestInterface));
				TestWithAll (typeof (TestType));
				TestKnownType ();
				TestMultipleValues (0, typeof (TestType));
			}
		}

		interface ITestInterface
		{
		}

		class TestType : ITestInterface
		{
		}
	}
}
