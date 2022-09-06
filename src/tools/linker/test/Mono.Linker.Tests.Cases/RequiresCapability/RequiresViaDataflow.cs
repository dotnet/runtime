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
		// Base/Derived and Implementation/Interface differs between linker and analyzer https://github.com/dotnet/linker/issues/2533
		[ExpectedWarning ("IL2026", "--DynamicallyAccessedTypeWithRequires.MethodWithRequires--")]
		[ExpectedWarning ("IL2026", "TypeWhichOverridesMethod.VirtualMethodRequires()", "--TypeWhichOverridesMethod.VirtualMethodRequires--", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
		[ExpectedWarning ("IL2026", "BaseType.VirtualMethodRequires()", "--BaseType.VirtualMethodRequires--")]
		public static void Main ()
		{
			TestDynamicallyAccessedMembersWithRequires (typeof (DynamicallyAccessedTypeWithRequires));
			TestDynamicallyAccessedMembersWithRequires (typeof (TypeWhichOverridesMethod));
			TestRequiresInDynamicDependency ();
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

		public class DynamicallyAccessedTypeWithRequires
		{
			[RequiresUnreferencedCode ("Message for --DynamicallyAccessedTypeWithRequires.MethodWithRequires--")]
			public void MethodWithRequires ()
			{
			}
		}

		static void TestDynamicallyAccessedMembersWithRequires (
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
		{
		}

		[RequiresUnreferencedCode ("Message for --RequiresInDynamicDependency--")]
		[RequiresAssemblyFiles ("Message for --RequiresInDynamicDependency--")]
		[RequiresDynamicCode ("Message for --RequiresInDynamicDependency--")]
		static void RequiresInDynamicDependency ()
		{
		}

		[ExpectedWarning ("IL2026", "--RequiresInDynamicDependency--")]
		[ExpectedWarning ("IL2026", "--RequiresInDynamicDependency--", ProducedBy = ProducedBy.Trimmer)]
		[ExpectedWarning ("IL3002", "--RequiresInDynamicDependency--", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
		[ExpectedWarning ("IL3050", "--RequiresInDynamicDependency--", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
		[DynamicDependency ("RequiresInDynamicDependency")]
		static void TestRequiresInDynamicDependency ()
		{
			RequiresInDynamicDependency ();
		}
	}
}
