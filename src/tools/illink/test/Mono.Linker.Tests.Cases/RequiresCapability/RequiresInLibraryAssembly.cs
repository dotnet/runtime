// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.RequiresCapability
{
	[SetupLinkerArgument ("-a", "test.exe", "library")]

	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	public class RequiresInLibraryAssembly
	{
		public static void Main ()
		{
		}

		[RequiresDynamicCode ("--MethodWhichRequires--")]
		public static void MethodWhichRequires () { }

		[RequiresDynamicCode ("--InstanceMethodWhichRequires--")]
		public void InstanceMethodWhichRequires () { }
	}

	[ExpectedNoWarnings]
	public sealed class ClassWithDAMAnnotatedMembers
	{
		public static void Method ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type type) { }

		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
		public static Type Field;
	}

	[ExpectedNoWarnings]
	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
	public sealed class ClassWithDAMAnnotation
	{
		public void Method () { }
	}

	[ExpectedNoWarnings]
	[RequiresUnreferencedCode ("--ClassWithRequires--")]
	public sealed class ClassWithRequires
	{
		public static int Field;

		internal static int InternalField;

		private static int PrivateField;

		public static void Method () { }

		public void InstanceMethod () { }

		public static int Property { get; set; }

		public static event EventHandler PropertyChanged;
	}
}
