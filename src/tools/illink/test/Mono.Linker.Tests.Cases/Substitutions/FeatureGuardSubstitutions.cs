 // Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using ILLink.RoslynAnalyzer;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Substitutions
{
	[ExpectedNoWarnings]
	[SetupCompileBefore ("TestFeatures.dll", new[] { "Dependencies/TestFeatures.cs" })]
	[SetupCompileResource ("FeatureGuardSubstitutions.xml", "ILLink.Substitutions.xml")]
	[IgnoreSubstitutions (false)] 
#if NATIVEAOT
	// ILC has different constant propagation behavior than ILLink, and we don't have
	// the test infrastructure to check for different IL sequences between ILLink/ILC.
	// Just validate the warning behavior instead.
	[SkipKeptItemsValidation]
#else
	// Tell linker to treat RequiresDynamicCodeAttribute as a disabled feature:
	[SetupLinkerArgument ("--feature", "System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported", "false")]
#endif
	[SetupLinkerArgument ("--feature", "Mono.Linker.Tests.Cases.Substitutions.FeatureGuardSubstitutions.DefineFeatureGuard.FeatureSwitch", "false")]
	[SetupLinkerArgument ("--feature", "Mono.Linker.Tests.Cases.Substitutions.FeatureGuardSubstitutions.DefineFeatureGuard.FeatureSwitchAndGuard", "false")]
	[SetupLinkerArgument ("--feature", "Mono.Linker.Tests.Cases.Substitutions.FeatureGuardSubstitutions.FeatureGuardPrecedence.GuardAndSwitch", "true")]
	[SetupLinkerArgument ("--feature", "Mono.Linker.Tests.Cases.Substitutions.FeatureGuardSubstitutions.FeatureGuardPrecedence.SwitchWithXml", "false")]
	[SetupLinkerArgument ("--feature", "Mono.Linker.Tests.Cases.Substitutions.FeatureGuardSubstitutions.FeatureGuardPrecedence.GuardAndSwitchWithXml", "false")]
	public class FeatureGuardSubstitutions
	{
		public static void Main ()
		{
			DefineFeatureGuard.Test ();
			FeatureGuardPrecedence.Test ();
		}

		[Kept]
		class DefineFeatureGuard {
			[FeatureGuard (typeof(RequiresDynamicCodeAttribute))]
			static bool GuardDynamicCode => RuntimeFeature.IsDynamicCodeSupported;

			[Kept]
			[ExpectedInstructionSequence (new[] {
				"nop",
				"ldc.i4.0",
				"stloc.0",
				"ldloc.0",
				"brfalse.s il_6",
				"ret"
			})]
			static void TestGuardDynamicCode ()
			{
				if (GuardDynamicCode)
					RequiresDynamicCode ();
			}

			[FeatureGuard (typeof(RequiresUnreferencedCodeAttribute))]
			static bool GuardUnreferencedCode => TestFeatures.IsUnreferencedCodeSupported;

			[Kept]
			[ExpectedInstructionSequence (new[] {
				"nop",
				"ldc.i4.0",
				"stloc.0",
				"ldloc.0",
				"brfalse.s il_6",
				"ret"
			})]

			static void TestGuardUnreferencedCode ()
			{
				if (GuardUnreferencedCode)
					RequiresUnreferencedCode ();
			}

			[Kept]
			[KeptAttributeAttribute (typeof (FeatureGuardAttribute))]
			[FeatureGuard (typeof(RequiresAssemblyFilesAttribute))]
			static bool GuardAssemblyFiles {
				[Kept]
				get => TestFeatures.IsAssemblyFilesSupported;
			}

			[Kept]
			// Linker doesn't treat RequiresAssemblyFilesAttribute as a disabled feature, so it's not removed.
			static void TestGuardAssemblyFiles ()
			{
				if (GuardAssemblyFiles)
					RequiresAssemblyFiles ();
			}

			[ExpectedWarning ("IL4000", nameof (RequiresDynamicCodeAttribute), Tool.Analyzer, "")]
			[ExpectedWarning ("IL4000", nameof (RequiresUnreferencedCodeAttribute), Tool.Analyzer, "")]
			[FeatureGuard (typeof (RequiresDynamicCodeAttribute))]
			[FeatureGuard (typeof (RequiresUnreferencedCodeAttribute))]
			static bool GuardDynamicCodeAndUnreferencedCode => RuntimeFeature.IsDynamicCodeSupported && TestFeatures.IsUnreferencedCodeSupported;

			[Kept]
			[ExpectedInstructionSequence (new[] {
				"nop",
				"ldc.i4.0",
				"stloc.0",
				"ldloc.0",
				"brfalse.s il_6",
				"ret"
			})]

			static void TestMultipleGuards ()
			{
				if (GuardDynamicCodeAndUnreferencedCode) {
					RequiresDynamicCode ();
					RequiresUnreferencedCode ();
				}
			}

			static class UnreferencedCode {
				[FeatureGuard (typeof (RequiresUnreferencedCodeAttribute))]
				public static bool GuardUnreferencedCode => TestFeatures.IsUnreferencedCodeSupported;
			}

			static class UnreferencedCodeIndirect {
				[FeatureGuard (typeof (RequiresUnreferencedCodeAttribute))]
				public static bool GuardUnreferencedCode => UnreferencedCode.GuardUnreferencedCode;
			}

			// Currently there is no way to annotate a feature type as depending on another feature,
			// so indirect guards are expressed the same way as direct guards, by using
			// FeatureGuardAttribute that references the underlying feature type.
			[FeatureGuard (typeof (RequiresUnreferencedCodeAttribute))]
			static bool GuardUnreferencedCodeIndirect => UnreferencedCodeIndirect.GuardUnreferencedCode;

			[Kept]
			[ExpectedInstructionSequence (new[] {
				"nop",
				"ldc.i4.0",
				"stloc.0",
				"ldloc.0",
				"brfalse.s il_6",
				"ret"
			})]

			static void TestIndirectGuard ()
			{
				if (GuardUnreferencedCodeIndirect)
					RequiresUnreferencedCode ();
			}

			[FeatureSwitchDefinition ("Mono.Linker.Tests.Cases.Substitutions.FeatureGuardSubstitutions.DefineFeatureGuard.FeatureSwitch")]
			static bool FeatureSwitch => AppContext.TryGetSwitch ("Mono.Linker.Tests.Cases.Substitutions.FeatureGuardSubstitutions.DefineFeatureGuard.FeatureSwitch", out bool isEnabled) && isEnabled;

			[ExpectedWarning ("IL2026", Tool.Analyzer, "")] // Analyzer doesn't respect FeatureSwitchDefinition or feature settings
			[ExpectedInstructionSequence (new[] {
				"nop",
				"ldc.i4.0",
				"stloc.0",
				"ldloc.0",
				"brfalse.s il_6",
				"ret"
			})]

			[Kept]
			static void TestFeatureSwitch ()
			{
				if (FeatureSwitch)
					RequiresUnreferencedCode ();
			}

			[ExpectedWarning ("IL4000", nameof (RequiresUnreferencedCodeAttribute), Tool.Analyzer, "")]
			[FeatureSwitchDefinition ("Mono.Linker.Tests.Cases.Substitutions.FeatureGuardSubstitutions.DefineFeatureGuard.FeatureSwitchAndGuard")]
			[FeatureGuard (typeof (RequiresUnreferencedCodeAttribute))]
			static bool FeatureSwitchAndGuard => AppContext.TryGetSwitch ("Mono.Linker.Tests.Cases.Substitutions.FeatureGuardSubstitutions.DefineFeatureGuard.FeatureSwitchAndGuard", out bool isEnabled) && isEnabled;

			[Kept]
			[ExpectedInstructionSequence (new[] {
				"nop",
				"ldc.i4.0",
				"stloc.0",
				"ldloc.0",
				"brfalse.s il_6",
				"ret"
			})]

			static void TestFeatureSwitchAndGuard ()
			{
				if (FeatureSwitchAndGuard)
					RequiresUnreferencedCode ();
			}

			static class UnreferencedCodeCycle {
				[FeatureGuard (typeof (RequiresUnreferencedCodeAttribute))]
				public static bool IsSupported => UnreferencedCodeCycle.IsSupported;
			}

			[FeatureGuard (typeof (RequiresUnreferencedCodeAttribute))]
			static bool GuardUnreferencedCodeCycle => TestFeatures.IsUnreferencedCodeSupported;

			[Kept]
			[ExpectedInstructionSequence (new[] {
				"nop",
				"ldc.i4.0",
				"stloc.0",
				"ldloc.0",
				"brfalse.s il_6",
				"ret"
			})]

			static void TestFeatureDependencyCycle1 ()
			{
				if (GuardUnreferencedCodeCycle)
					RequiresUnreferencedCode ();
			}

			static class UnreferencedCodeCycle2_A {
				[FeatureGuard (typeof (RequiresUnreferencedCodeAttribute))]
				public static bool IsSupported => UnreferencedCodeCycle2_A.IsSupported;
			}

			static class UnreferencedCodeCycle2_B {
				[FeatureGuard (typeof (RequiresUnreferencedCodeAttribute))]
				public static bool IsSupported => UnreferencedCodeCycle2_B.IsSupported;
			}

			static class UnreferencedCodeCycle2 {
				[FeatureGuard (typeof (RequiresUnreferencedCodeAttribute))]
				public static bool IsSupported => UnreferencedCodeCycle2_A.IsSupported;
			}

			[FeatureGuard (typeof (RequiresUnreferencedCodeAttribute))]
			static bool GuardUnreferencedCodeCycle2 => TestFeatures.IsUnreferencedCodeSupported;

			[Kept]
			[ExpectedInstructionSequence (new[] {
				"nop",
				"ldc.i4.0",
				"stloc.0",
				"ldloc.0",
				"brfalse.s il_6",
				"ret"
			})]
			static void TestFeatureDependencyCycle2 ()
			{
				if (GuardUnreferencedCodeCycle2)
					RequiresUnreferencedCode ();
			}

			[Kept]
			public static void Test ()
			{
				TestGuardDynamicCode ();
				TestGuardUnreferencedCode ();
				TestGuardAssemblyFiles ();
				TestMultipleGuards ();
				TestIndirectGuard ();
				TestFeatureDependencyCycle1 ();
				TestFeatureDependencyCycle2 ();
				TestFeatureSwitch ();
				TestFeatureSwitchAndGuard ();
			}
		}

		[Kept]
		class FeatureGuardPrecedence {
			[ExpectedWarning ("IL4000", nameof (RequiresUnreferencedCodeAttribute), Tool.Analyzer, "")]
			[FeatureSwitchDefinition ("Mono.Linker.Tests.Cases.Substitutions.FeatureGuardSubstitutions.FeatureGuardPrecedence.GuardAndSwitch")]
			[FeatureGuard (typeof (RequiresUnreferencedCodeAttribute))]
			static bool GuardAndSwitch => AppContext.TryGetSwitch ("Mono.Linker.Tests.Cases.Substitutions.FeatureGuardSubstitutions.FeatureGuardPrecedence.GuardAndSwitch", out bool isEnabled) && isEnabled;

			[Kept]
			[ExpectedInstructionSequence (new[] {
				"nop",
				"ldc.i4.1",
				"stloc.0",
				"ldloc.0",
				"pop",
				"call System.Void Mono.Linker.Tests.Cases.Substitutions.FeatureGuardSubstitutions::RequiresUnreferencedCode()",
				"nop",
				"ret"
			})]
			// ILLink/ILCompiler ignore FeatureGuard on properties that also have FeatureSwitchDefinition
			[ExpectedWarning ("IL2026", Tool.Trimmer | Tool.NativeAot, "")]
			static void TestSwitchWinsOverGuard ()
			{
				if (GuardAndSwitch)
					RequiresUnreferencedCode ();
			}

			[Kept]
			[KeptAttributeAttribute (typeof (FeatureSwitchDefinitionAttribute))]
			[KeptAttributeAttribute (typeof (FeatureGuardAttribute))]
			[ExpectedWarning ("IL4000", nameof (RequiresUnreferencedCodeAttribute), Tool.Analyzer, "")]
			[FeatureSwitchDefinition ("Mono.Linker.Tests.Cases.Substitutions.FeatureGuardSubstitutions.FeatureGuardPrecedence.GuardAndSwitchNotSet")]
			[FeatureGuard (typeof (RequiresUnreferencedCodeAttribute))]
			static bool GuardAndSwitchNotSet {
				[Kept]
				get => AppContext.TryGetSwitch ("Mono.Linker.Tests.Cases.Substitutions.FeatureGuardSubstitutions.FeatureGuardPrecedence.GuardAndSwitchNotSet", out bool isEnabled) && isEnabled;
			}

			[Kept]
			// No IL modifications because feature is not set, and FeatureGuard is ignored due to FeatureSwitchDefinition.
			[ExpectedWarning ("IL2026", Tool.Trimmer | Tool.NativeAot, "")]
			static void TestSwitchNotSetWinsOverGuard ()
			{
				if (GuardAndSwitchNotSet)
					RequiresUnreferencedCode ();
			}

			[FeatureGuard (typeof (RequiresUnreferencedCodeAttribute))]
			static bool GuardWithXml => TestFeatures.IsUnreferencedCodeSupported;

			[Kept]
			[ExpectedInstructionSequence (new[] {
				"nop",
				"ldc.i4.1",
				"stloc.0",
				"ldloc.0",
				"pop",
				"call System.Void Mono.Linker.Tests.Cases.Substitutions.FeatureGuardSubstitutions::RequiresUnreferencedCode()",
				"nop",
				"ret"
			})]
			[ExpectedWarning ("IL2026", Tool.Trimmer | Tool.NativeAot, "")]
			static void TestXmlWinsOverGuard ()
			{
				if (GuardWithXml)
					RequiresUnreferencedCode ();
			}

			[KeptAttributeAttribute (typeof (FeatureSwitchDefinitionAttribute))]
			[KeptAttributeAttribute (typeof (FeatureGuardAttribute))]
			[ExpectedWarning ("IL4000", nameof (RequiresUnreferencedCodeAttribute), Tool.Analyzer, "")]
			[FeatureSwitchDefinition ("Mono.Linker.Tests.Cases.Substitutions.FeatureGuardSubstitutions.FeatureGuardPrecedence.SwitchWithXml")]
			[FeatureGuard (typeof (RequiresUnreferencedCodeAttribute))]
			static bool SwitchWithXml => AppContext.TryGetSwitch ("Mono.Linker.Tests.Cases.Substitutions.FeatureGuardSubstitutions.FeatureGuardPrecedence.SwitchWithXml", out bool isEnabled) && isEnabled;

			[Kept]
			// XML substitutions win despite FeatureSwitchDefinition and feature settings.
			[ExpectedInstructionSequence (new[] {
				"nop",
				"ldc.i4.1",
				"stloc.0",
				"ldloc.0",
				"pop",
				"call System.Void Mono.Linker.Tests.Cases.Substitutions.FeatureGuardSubstitutions::RequiresUnreferencedCode()",
				"nop",
				"ret"
			})]
			[ExpectedWarning ("IL2026", Tool.Trimmer | Tool.NativeAot, "")]
			static void TestXmlWinsOverSwitch () {
				if (SwitchWithXml)
					RequiresUnreferencedCode ();
			}

			[KeptAttributeAttribute (typeof (FeatureSwitchDefinitionAttribute))]
			[KeptAttributeAttribute (typeof (FeatureGuardAttribute))]
			[ExpectedWarning ("IL4000", nameof (RequiresUnreferencedCodeAttribute), Tool.Analyzer, "")]
			[FeatureSwitchDefinition ("Mono.Linker.Tests.Cases.Substitutions.FeatureGuardPrecedence.GuardAndSwitchWithXml")]
			[FeatureGuard (typeof (RequiresUnreferencedCodeAttribute))]
			static bool GuardAndSwitchWithXml => AppContext.TryGetSwitch ("Mono.Linker.Tests.Cases.Substitutions.FeatureGuardSubstitutions.FeatureGuardPrecedence.GuardAndSwitchWithXml", out bool isEnabled) && isEnabled;

			[Kept]
			// XML substitutions win despite FeatureSwitchDefinition and feature settings.
			[ExpectedInstructionSequence (new[] {
				"nop",
				"ldc.i4.1",
				"stloc.0",
				"ldloc.0",
				"pop",
				"call System.Void Mono.Linker.Tests.Cases.Substitutions.FeatureGuardSubstitutions::RequiresUnreferencedCode()",
				"nop",
				"ret"
			})]
			[ExpectedWarning ("IL2026", Tool.Trimmer | Tool.NativeAot, "")]
			static void TestXmlWinsOverGuardAndSwitch () {
				if (GuardAndSwitchWithXml)
					RequiresUnreferencedCode ();
			}

			[Kept]
			public static void Test () {
				TestSwitchWinsOverGuard ();
				TestSwitchNotSetWinsOverGuard ();
				TestXmlWinsOverGuard ();
				TestXmlWinsOverSwitch ();
				TestXmlWinsOverGuardAndSwitch ();
			}
		}

		[RequiresDynamicCode (nameof (RequiresDynamicCode))]
		static void RequiresDynamicCode () { }

		[Kept]
		[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
		[RequiresUnreferencedCode (nameof (RequiresUnreferencedCode))]
		static void RequiresUnreferencedCode () { }

		[Kept]
		[KeptAttributeAttribute (typeof (RequiresAssemblyFilesAttribute))]
		[RequiresAssemblyFiles (nameof (RequiresAssemblyFiles))]
		static void RequiresAssemblyFiles () { }
	}
}
