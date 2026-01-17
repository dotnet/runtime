// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.Versioning;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Substitutions
{
	/// <summary>
	/// Test that methods with [UnsupportedOSPlatform] attribute matching --target-os
	/// are stubbed with PlatformNotSupportedException.
	/// </summary>
	[SetupLinkerArgument ("--target-os", "browser")]
	public class UnsupportedOSPlatformSubstitution
	{
		public static void Main ()
		{
			// This method should be kept and stubbed with PNSE
			UnsupportedOnBrowser ();

			// This method should be kept intact - different platform
			UnsupportedOnWindows ();

			// This method should be kept intact - no attribute
			SupportedEverywhere ();

			// Test property with attribute
			var x = PropertyUnsupportedOnBrowser;
			PropertyUnsupportedOnBrowser = x;

			// Test property without attribute (should be kept intact)
			var y = PropertySupportedEverywhere;
			PropertySupportedEverywhere = y;
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"newobj System.Void System.PlatformNotSupportedException::.ctor()",
			"throw",
		})]
		[KeptAttributeAttribute (typeof (UnsupportedOSPlatformAttribute))]
		[UnsupportedOSPlatform ("browser")]
		static void UnsupportedOnBrowser ()
		{
			// This body should be replaced with throw new PlatformNotSupportedException()
			Console.WriteLine ("This should not appear");
		}

		[Kept]
		[KeptAttributeAttribute (typeof (UnsupportedOSPlatformAttribute))]
		[UnsupportedOSPlatform ("windows")]
		static void UnsupportedOnWindows ()
		{
			// This method targets windows, not browser, so should be kept intact
			Console.WriteLine ("This should be kept");
		}

		[Kept]
		static void SupportedEverywhere ()
		{
			// No UnsupportedOSPlatform attribute, should be kept intact
			Console.WriteLine ("This should be kept");
		}

		[Kept]
		[KeptAttributeAttribute (typeof (UnsupportedOSPlatformAttribute))]
		[UnsupportedOSPlatform ("browser")]
		static int PropertyUnsupportedOnBrowser
		{
			[Kept]
			[ExpectedInstructionSequence (new[] {
				"newobj System.Void System.PlatformNotSupportedException::.ctor()",
				"throw",
			})]
			get => 42;

			[Kept]
			[ExpectedInstructionSequence (new[] {
				"newobj System.Void System.PlatformNotSupportedException::.ctor()",
				"throw",
			})]
			set { }
		}

		[Kept]
		static int PropertySupportedEverywhere
		{
			[Kept]
			get => 42;

			[Kept]
			set { }
		}
	}
}
