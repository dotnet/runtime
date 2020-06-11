// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.RequiresCapability
{
	[SkipKeptItemsValidation]
	public class RequiresUnreferencedCodeCapability
	{
		public static void Main ()
		{
			TestRequiresWithMessageOnlyOnMethod ();
			TestRequiresWithMessageAndUrlOnMethod ();
			TestRequiresOnConstructor ();
			TestRequiresOnPropertyGetterAndSetter ();
			TestRequiresSuppressesReflectionAnalysis ();
		}

		[LogContains (
			"warning IL2026: Mono.Linker.Tests.Cases.RequiresCapability.RequiresUnreferencedCodeCapability::TestRequiresWithMessageOnlyOnMethod(): " +
			"Calling 'System.Void Mono.Linker.Tests.Cases.RequiresCapability.RequiresUnreferencedCodeCapability::RequiresWithMessageOnly()' " +
			"which has `RequiresUnreferencedCodeAttribute` can break functionality when trimming application code. " +
			"Message for --RequiresWithMessageOnly--.")]
		static void TestRequiresWithMessageOnlyOnMethod ()
		{
			RequiresWithMessageOnly ();
		}

		[RequiresUnreferencedCode ("Message for --RequiresWithMessageOnly--")]
		static void RequiresWithMessageOnly ()
		{
		}

		[LogContains (
			"warning IL2026: Mono.Linker.Tests.Cases.RequiresCapability.RequiresUnreferencedCodeCapability::TestRequiresWithMessageAndUrlOnMethod(): " +
			"Calling 'System.Void Mono.Linker.Tests.Cases.RequiresCapability.RequiresUnreferencedCodeCapability::RequiresWithMessageAndUrl()' " +
			"which has `RequiresUnreferencedCodeAttribute` can break functionality when trimming application code. " +
			"Message for --RequiresWithMessageAndUrl--. " +
			"https://helpurl")]
		static void TestRequiresWithMessageAndUrlOnMethod ()
		{
			RequiresWithMessageAndUrl ();
		}

		[RequiresUnreferencedCode ("Message for --RequiresWithMessageAndUrl--", Url = "https://helpurl")]
		static void RequiresWithMessageAndUrl ()
		{
		}

		[LogContains (
			"warning IL2026: Mono.Linker.Tests.Cases.RequiresCapability.RequiresUnreferencedCodeCapability::TestRequiresOnConstructor(): " +
			"Calling 'System.Void Mono.Linker.Tests.Cases.RequiresCapability.RequiresUnreferencedCodeCapability/ConstructorRequires::.ctor()' " +
			"which has `RequiresUnreferencedCodeAttribute` can break functionality when trimming application code. " +
			"Message for --ConstructorRequires--.")]
		static void TestRequiresOnConstructor ()
		{
			new ConstructorRequires ();
		}

		class ConstructorRequires
		{
			[RequiresUnreferencedCode ("Message for --ConstructorRequires--")]
			public ConstructorRequires ()
			{
			}
		}

		[LogContains (
			"warning IL2026: Mono.Linker.Tests.Cases.RequiresCapability.RequiresUnreferencedCodeCapability::TestRequiresOnPropertyGetterAndSetter(): " +
			"Calling 'System.Int32 Mono.Linker.Tests.Cases.RequiresCapability.RequiresUnreferencedCodeCapability::get_PropertyRequires()' " +
			"which has `RequiresUnreferencedCodeAttribute` can break functionality when trimming application code. " +
			"Message for --getter PropertyRequires--.")]
		[LogContains (
			"warning IL2026: Mono.Linker.Tests.Cases.RequiresCapability.RequiresUnreferencedCodeCapability::TestRequiresOnPropertyGetterAndSetter(): " +
			"Calling 'System.Void Mono.Linker.Tests.Cases.RequiresCapability.RequiresUnreferencedCodeCapability::set_PropertyRequires(System.Int32)' " +
			"which has `RequiresUnreferencedCodeAttribute` can break functionality when trimming application code. " +
			"Message for --setter PropertyRequires--.")]
		static void TestRequiresOnPropertyGetterAndSetter ()
		{
			_ = PropertyRequires;
			PropertyRequires = 0;
		}

		static int PropertyRequires {
			[RequiresUnreferencedCode ("Message for --getter PropertyRequires--")]
			get { return 42; }

			[RequiresUnreferencedCode ("Message for --setter PropertyRequires--")]
			set { }
		}

		[LogContains (
			"warning IL2026: Mono.Linker.Tests.Cases.RequiresCapability.RequiresUnreferencedCodeCapability::TestRequiresSuppressesReflectionAnalysis(): " +
			"Calling 'System.Void Mono.Linker.Tests.Cases.RequiresCapability.RequiresUnreferencedCodeCapability::RequiresAndCallsOtherRequiresMethods()' " +
			"which has `RequiresUnreferencedCodeAttribute` can break functionality when trimming application code. " +
			"Message for --RequiresAndCallsOtherRequiresMethods--.")]
		static void TestRequiresSuppressesReflectionAnalysis ()
		{
			RequiresAndCallsOtherRequiresMethods ();
		}

		[RequiresUnreferencedCode ("Message for --RequiresAndCallsOtherRequiresMethods--")]
		[LogDoesNotContain ("Message for --RequiresUnreferencedCodeMethod--")]
		[RecognizedReflectionAccessPattern]
		static void RequiresAndCallsOtherRequiresMethods ()
		{
			// Normally this would warn, but with the attribute on this method it should be auto-suppressed
			RequiresUnreferencedCodeMethod ();

			// Normally this would warn due to incompatible annotations, but with the attribute on this method it should be auto-suppressed
			RequiresPublicFields (GetTypeWithPublicMethods ());
		}

		[RequiresUnreferencedCode ("Message for --RequiresUnreferencedCodeMethod--")]
		static void RequiresUnreferencedCodeMethod ()
		{
		}

		static void RequiresPublicFields ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] Type type)
		{
		}

		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
		static Type GetTypeWithPublicMethods ()
		{
			return null;
		}
	}
}