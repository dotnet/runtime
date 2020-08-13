// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.RequiresCapability
{
	[SetupLinkAttributesFile ("RequiresUnreferencedCodeCapability.attributes.xml")]
	[SkipKeptItemsValidation]
	public class RequiresUnreferencedCodeCapability
	{
		public static void Main ()
		{
			TestRequiresWithMessageOnlyOnMethod ();
			TestRequiresWithMessageAndUrlOnMethod ();
			TestRequiresOnConstructor ();
			TestRequiresOnPropertyGetterAndSetter ();
			TestRequiresSuppressesWarningsFromReflectionAnalysis ();
			TestDuplicateRequiresAttribute ();
		}

		[ExpectedWarning ("IL2026",
			"Calling 'Mono.Linker.Tests.Cases.RequiresCapability.RequiresUnreferencedCodeCapability.RequiresWithMessageOnly()' " +
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

		[ExpectedWarning ("IL2026",
			"Calling 'Mono.Linker.Tests.Cases.RequiresCapability.RequiresUnreferencedCodeCapability.RequiresWithMessageAndUrl()' " +
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
			"warning IL2026: Mono.Linker.Tests.Cases.RequiresCapability.RequiresUnreferencedCodeCapability.TestRequiresOnConstructor(): " +
			"Calling 'Mono.Linker.Tests.Cases.RequiresCapability.RequiresUnreferencedCodeCapability.ConstructorRequires.ConstructorRequires()' " +
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

		[ExpectedWarning ("IL2026",
			"Calling 'Mono.Linker.Tests.Cases.RequiresCapability.RequiresUnreferencedCodeCapability.get_PropertyRequires()' " +
			"which has `RequiresUnreferencedCodeAttribute` can break functionality when trimming application code. " +
			"Message for --getter PropertyRequires--.")]
		[ExpectedWarning ("IL2026",
			"Calling 'Mono.Linker.Tests.Cases.RequiresCapability.RequiresUnreferencedCodeCapability.set_PropertyRequires(Int32)' " +
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

		[ExpectedWarning ("IL2026",
			"Calling 'Mono.Linker.Tests.Cases.RequiresCapability.RequiresUnreferencedCodeCapability.RequiresAndCallsOtherRequiresMethods<TPublicMethods>()' " +
			"which has `RequiresUnreferencedCodeAttribute` can break functionality when trimming application code. " +
			"Message for --RequiresAndCallsOtherRequiresMethods--.")]
		static void TestRequiresSuppressesWarningsFromReflectionAnalysis ()
		{
			RequiresAndCallsOtherRequiresMethods<TestType> ();
		}

		[RequiresUnreferencedCode ("Message for --RequiresAndCallsOtherRequiresMethods--")]
		[LogDoesNotContain ("Message for --RequiresUnreferencedCodeMethod--")]
		[RecognizedReflectionAccessPattern]
		static void RequiresAndCallsOtherRequiresMethods<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods> ()
		{
			// Normally this would warn, but with the attribute on this method it should be auto-suppressed
			RequiresUnreferencedCodeMethod ();

			// Normally this would warn due to incompatible annotations, but with the attribute on this method it should be auto-suppressed
			RequiresPublicFields (GetTypeWithPublicMethods ());

			TypeRequiresPublicFields<TPublicMethods>.Method ();

			MethodRequiresPublicFields<TPublicMethods> ();
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

		class TypeRequiresPublicFields<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T>
		{
			public static void Method () { }
		}

		static void MethodRequiresPublicFields<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T> () { }

		class TestType { }

		[ExpectedWarning ("IL2026", "--MethodWithDuplicateRequiresAttribute--")]
		[LogDoesNotContain ("Message for MethodWithDuplicateRequiresAttribute from link attributes XML")]
		static void TestDuplicateRequiresAttribute ()
		{
			MethodWithDuplicateRequiresAttribute ();
		}

		// The second attribute is added through link attribute XML
		[RequiresUnreferencedCode ("Message for --MethodWithDuplicateRequiresAttribute--")]
		[ExpectedWarning ("IL2027", "RequiresUnreferencedCodeAttribute", nameof (MethodWithDuplicateRequiresAttribute))]
		static void MethodWithDuplicateRequiresAttribute ()
		{
		}
	}
}