// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.RequiresCapability
{
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	class RequiresViaDataflow
	{
		public static void Main ()
		{
			AnnotatedParameter.Test ();
			DynamicDependency.Test ();
		}

		class AnnotatedParameter
		{
			static void MethodWithAnnotatedParameter (
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
			{
			}

			public class DynamicallyAccessedTypeWithRequires
			{
				[RequiresUnreferencedCode ("Message for --DynamicallyAccessedTypeWithRequires.MethodWithRequires--")]
				public void MethodWithRequires ()
				{
				}
			}

			[ExpectedWarning ("IL2026", "--DynamicallyAccessedTypeWithRequires.MethodWithRequires--")]
			static void TestNonVirtualMethod ()
			{
				MethodWithAnnotatedParameter (typeof (DynamicallyAccessedTypeWithRequires));
			}

			class BaseType
			{
				[RequiresUnreferencedCode ("Message for --BaseType.VirtualMethodRequires--")]
				[RequiresAssemblyFiles ("Message for --BaseType.VirtualMethodRequires--")]
				[RequiresDynamicCode ("Message for --BaseType.VirtualMethodRequires--")]
				public virtual void VirtualMethodRequires ()
				{
				}
			}

			class TypeWhichOverridesMethod : BaseType
			{
				[RequiresUnreferencedCode ("Message for --TypeWhichOverridesMethod.VirtualMethodRequires--")]
				[RequiresAssemblyFiles ("Message for --TypeWhichOverridesMethod.VirtualMethodRequires--")]
				[RequiresDynamicCode ("Message for --TypeWhichOverridesMethod.VirtualMethodRequires--")]
				public override void VirtualMethodRequires ()
				{
				}
			}

			[ExpectedWarning ("IL2026", "TypeWhichOverridesMethod.VirtualMethodRequires()", "--TypeWhichOverridesMethod.VirtualMethodRequires--")]
			[ExpectedWarning ("IL3002", "TypeWhichOverridesMethod.VirtualMethodRequires()", "--TypeWhichOverridesMethod.VirtualMethodRequires--", ProducedBy = Tool.NativeAot)]
			[ExpectedWarning ("IL3050", "TypeWhichOverridesMethod.VirtualMethodRequires()", "--TypeWhichOverridesMethod.VirtualMethodRequires--", ProducedBy = Tool.NativeAot)]
			[ExpectedWarning ("IL2026", "BaseType.VirtualMethodRequires()", "--BaseType.VirtualMethodRequires--")]
			[ExpectedWarning ("IL3002", "BaseType.VirtualMethodRequires()", "--BaseType.VirtualMethodRequires--", ProducedBy = Tool.NativeAot)]
			[ExpectedWarning ("IL3050", "BaseType.VirtualMethodRequires()", "--BaseType.VirtualMethodRequires--", ProducedBy = Tool.NativeAot)]
			static void TestOverriddenVirtualMethod ()
			{
				MethodWithAnnotatedParameter (typeof (TypeWhichOverridesMethod));
			}

			public static void Test ()
			{
				TestNonVirtualMethod ();
				TestOverriddenVirtualMethod ();
			}
		}

		class DynamicDependency
		{
			[RequiresUnreferencedCode ("Message for --RequiresInDynamicDependency--")]
			[RequiresAssemblyFiles ("Message for --RequiresInDynamicDependency--")]
			[RequiresDynamicCode ("Message for --RequiresInDynamicDependency--")]
			static void RequiresInDynamicDependency ()
			{
			}

			// https://github.com/dotnet/runtime/issues/83080 - Analyzer doesn't recognize DynamicDependency in any way
			[ExpectedWarning ("IL2026", "--RequiresInDynamicDependency--", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			[ExpectedWarning ("IL3002", "--RequiresInDynamicDependency--", ProducedBy = Tool.NativeAot)]
			[ExpectedWarning ("IL3050", "--RequiresInDynamicDependency--", ProducedBy = Tool.NativeAot)]
			[DynamicDependency ("RequiresInDynamicDependency")]
			public static void Test ()
			{
			}
		}
	}
}
