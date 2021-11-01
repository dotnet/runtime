// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Warnings.Dependencies;


namespace Mono.Linker.Tests.Cases.Warnings.Individual
{
	[SkipRemainingErrorsValidation]
	[SetupLinkerTrimMode ("skip")]
#if !NETCOREAPP
	[SetupCompileBefore ("library.dll", new[] { typeof (TriggerWarnings_Lib) }, new[] { "System.Core.dll" })]
#else
	[SetupCompileBefore ("library.dll", new[] { typeof (TriggerWarnings_Lib) })]
#endif
	[KeptAssembly ("library.dll")]
	[SetupLinkerAction ("link", "library.dll")]
	[SetupLinkerArgument ("--verbose")]
	public class WarningsAreSorted
	{
		public static void Main ()
		{
			TriggerWarnings_Lib.Main ();
			B.Z ();
			B.Y ();
			B.X ();
			A.Y ();
			A.X ();
		}

		public static class Warn
		{
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)]
			public static string _;
		}

		public class A
		{
			public static void X ()
			{
				_ = Type.GetType (Warn._).GetMethod ("None");
			}

			public static void Y ()
			{
				_ = Type.GetType (Warn._).GetMethod ("None");
			}
		}

		public class B
		{
			public static void X ()
			{
				_ = Type.GetType (Warn._).GetMethod ("None");
			}

			public static void Y ()
			{
				_ = Type.GetType (Warn._).GetMethod ("None");
			}

			public static void Z ()
			{
				_ = Type.GetType (Warn._).GetMethod ("None");
			}
		}
	}
}
