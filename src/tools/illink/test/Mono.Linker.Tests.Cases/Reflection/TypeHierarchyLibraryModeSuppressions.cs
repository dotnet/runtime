// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Reflection
{
	[SetupLinkerArgument ("-a", "test.exe", "library")]
	[ExpectedNoWarnings]
	[KeptMember (".ctor()")]
	public class TypeHierarchyLibraryModeSuppressions
	{
		public static void Main ()
		{
			var t1 = typeof (Unsuppressed);
			var t2 = typeof (Suppressed);
		}

		[Kept]
		[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
		class Unsuppressed
		{
			[Kept]
			[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
			[ExpectedWarning ("IL2112", "--RUC on Unsuppressed--")]
			[RequiresUnreferencedCode ("--RUC on Unsuppressed--")]
			public void RUCMethod () { }
		}

		[Kept]
		[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
		class Suppressed
		{
			[Kept]
			[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
			[KeptAttributeAttribute (typeof (UnconditionalSuppressMessageAttribute))]
			[UnconditionalSuppressMessage ("TrimAnalysis", "IL2112")]
			[RequiresUnreferencedCode ("--RUC on Suppressed--")]
			public void RUCMethod () { }
		}
	}
}
