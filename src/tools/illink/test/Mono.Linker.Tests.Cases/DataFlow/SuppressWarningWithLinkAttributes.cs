// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[SkipKeptItemsValidation]
	[SetupLinkAttributesFile ("SuppressWarningWithLinkAttributes.xml")]
	[LogDoesNotContain ("Trim analysis warning IL2067: Mono.Linker.Tests.Cases.DataFlow.SuppressWarningWithLinkAttributes::ReadFromInstanceField()")]
	[ExpectedWarning ("IL2031", "System.DoesNotExistAttribute", FileName = "SuppressWarningWithLinkAttributes.xml")]
	[ExpectedNoWarnings]
	class SuppressWarningWithLinkAttributes
	{
		public static void Main ()
		{
			var instance = new SuppressWarningWithLinkAttributes ();

			instance.ReadFromInstanceField ();
		}

		Type _typeWithPublicParameterlessConstructor;

		Type PropertyWithPublicParameterlessConstructor { get; set; }

		[ExpectedWarning ("IL2077", nameof (SuppressWarningWithLinkAttributes) + "." + nameof (RequirePublicConstructors))]
		[ExpectedWarning ("IL2077", nameof (SuppressWarningWithLinkAttributes) + "." + nameof (RequireNonPublicConstructors))]
		private void ReadFromInstanceField ()
		{
			RequirePublicParameterlessConstructor (_typeWithPublicParameterlessConstructor);
			RequirePublicConstructors (_typeWithPublicParameterlessConstructor);
			RequireNonPublicConstructors (_typeWithPublicParameterlessConstructor);
		}

		private static void RequirePublicParameterlessConstructor (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
			Type type)
		{
		}

		private static void RequirePublicConstructors (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
			Type type)
		{
		}

		private static void RequireNonPublicConstructors (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicConstructors)]
			Type type)
		{
		}
	}
}
