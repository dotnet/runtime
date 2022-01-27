// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
		[ExpectedWarning ("IL2026", "--DynamicallyAccessedTypeWithRequires.MethodWithRequires--", ProducedBy = ProducedBy.Trimmer)]
		[ExpectedWarning ("IL2026", "--BaseType.VirtualMethodRequires--", ProducedBy = ProducedBy.Trimmer)]
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
		[ExpectedWarning ("IL3002", "--RequiresInDynamicDependency--", ProducedBy = ProducedBy.Analyzer)]
		[ExpectedWarning ("IL3050", "--RequiresInDynamicDependency--", ProducedBy = ProducedBy.Analyzer)]
		[DynamicDependency ("RequiresInDynamicDependency")]
		static void TestRequiresInDynamicDependency ()
		{
			RequiresInDynamicDependency ();
		}
	}
}
