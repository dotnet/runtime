// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.SingleFile
{
	[IgnoreTestCase ("Ignore in illink since it doesn't implement any single-file related functionality", IgnoredBy = Tool.Trimmer)]
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	public class SingleFileIntrinsics
	{
		// Some of the test methods have RAF on them, it's not the point of this test to verify that behavior
		[UnconditionalSuppressMessage("test", "IL3002")]
		public static void Main ()
		{
			TestAssemblyLocation ();
			TestAssemblyLocationSuppressedByRAF ();
			TestAssemblyNameCodeBase ();
			TestAssemblyNameCodeBaseSuppressedByRAF ();
			TestAssemblyNameEscapedCodeBase ();
			TestAssemblyNameEscapedCodeBaseSuppressedByRAF ();
		}

		[ExpectedWarning("IL3000", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
		static void TestAssemblyLocation()
		{
			var a = typeof (SingleFileIntrinsics).Assembly.Location;
		}

		[RequiresAssemblyFiles("test")]
		static void TestAssemblyLocationSuppressedByRAF()
		{
			var a = typeof (SingleFileIntrinsics).Assembly.Location;
		}

		[ExpectedWarning ("IL3000", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
		// https://github.com/dotnet/runtime/issues/83088 - analyzer also produces IL3002 - unnecessarily
		[ExpectedWarning ("IL3002", ProducedBy = Tool.Analyzer)]
		static void TestAssemblyNameCodeBase()
		{
			var a = typeof (SingleFileIntrinsics).Assembly.GetName ().CodeBase;
		}

		[RequiresAssemblyFiles ("test")]
		static void TestAssemblyNameCodeBaseSuppressedByRAF ()
		{
			var a = typeof (SingleFileIntrinsics).Assembly.GetName ().CodeBase;
		}

		[ExpectedWarning ("IL3000", ProducedBy = Tool.Analyzer | Tool.NativeAot)]
		// https://github.com/dotnet/runtime/issues/83088 - analyzer also produces IL3002 - unnecessarily
		[ExpectedWarning ("IL3002", ProducedBy = Tool.Analyzer)]
		static void TestAssemblyNameEscapedCodeBase ()
		{
			var a = typeof (SingleFileIntrinsics).Assembly.GetName ().EscapedCodeBase;
		}

		[RequiresAssemblyFiles ("test")]
		static void TestAssemblyNameEscapedCodeBaseSuppressedByRAF ()
		{
			var a = typeof (SingleFileIntrinsics).Assembly.GetName ().EscapedCodeBase;
		}
	}
}
