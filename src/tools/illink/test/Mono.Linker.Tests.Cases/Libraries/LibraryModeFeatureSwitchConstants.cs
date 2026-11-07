// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Libraries.Dependencies;

namespace Mono.Linker.Tests.Cases.Libraries
{
    [IgnoreTestCase("NativeAOT doesn't implement library trimming the same way", IgnoredBy = Tool.NativeAot)]
    [KeptAttributeAttribute(typeof(IgnoreTestCaseAttribute), By = Tool.Trimmer)]
    [SetupLinkerArgument("-a", "test", "library")]
    [SetupLinkerArgument("--enable-opt", "ipconstprop")]
    [SetupCompileBefore("impl.dll", new[] { "Dependencies/LibraryModeFeatureSwitchConstants_Impl.cs" })]
    [SetupLinkerAction("link", "impl")]
    // Trivial constant FeatureSwitchDefinition getters must fold in library mode (including when
    // they come from a referenced implementation), so the whole implementation assembly becomes unused.
    [RemovedAssembly("impl.dll")]
    [RemovedAssemblyReference("test", "impl")]
    public class LibraryModeFeatureSwitchConstants
    {
        [Kept]
        public static void Main()
        {
            TestTrivialConstantFolds();
            TestReferencedTrivialConstantFolds();
            TestNonTrivialUnsetRetains();
        }

        [Kept]
        public LibraryModeFeatureSwitchConstants()
        {
        }

        // Mirrors CoreLib's IsMultithreadingSupported under #if FEATURE_MULTITHREADING:
        // a FeatureSwitchDefinition getter compiled as a platform constant must still fold in
        // library mode so dependent helpers (QCalls) can be trimmed.
        [FeatureSwitchDefinition("Mono.Linker.Tests.Cases.Libraries.LibraryModeFeatureSwitchConstants.IsMultithreadingSupported")]
        static bool IsMultithreadingSupported => false;

        // Must not be kept once the trivial feature switch folds to false.
        static int GetProcessorCount() => 42;

        [Kept]
        [ExpectedInstructionSequence(new[] {
            "nop",
            "ldc.i4.0",
            "stloc.0",
            "ldloc.0",
            "brfalse.s il_6",
            "ret"
        })]
        public static void TestTrivialConstantFolds()
        {
            if (IsMultithreadingSupported)
                GetProcessorCount();
        }

        [Kept]
        [ExpectedInstructionSequence(new[] {
            "nop",
            "ldc.i4.1",
            "call System.Void Mono.Linker.Tests.Cases.Libraries.LibraryModeFeatureSwitchConstants::Use(System.Int32)",
            "nop",
            "ret"
        })]
        public static void TestReferencedTrivialConstantFolds()
        {
            Use(LibraryModeFeatureSwitchConstants_Impl.GetProcessorCountIfSupported());
        }

        // Non-trivial AppContext-based switches without an explicit --feature must not be decided
        // for the app: both paths stay.
        [Kept]
        [KeptAttributeAttribute(typeof(FeatureSwitchDefinitionAttribute))]
        [FeatureSwitchDefinition("Mono.Linker.Tests.Cases.Libraries.LibraryModeFeatureSwitchConstants.AppConfigurableSwitch")]
        static bool AppConfigurableSwitch
        {
            [Kept]
            get => AppContext.TryGetSwitch("Mono.Linker.Tests.Cases.Libraries.LibraryModeFeatureSwitchConstants.AppConfigurableSwitch", out bool isEnabled) && isEnabled;
        }

        [Kept]
        public static void TestNonTrivialUnsetRetains()
        {
            if (AppConfigurableSwitch)
                EnabledPath();
            else
                DisabledPath();
        }

        [Kept]
        static void EnabledPath()
        {
        }

        [Kept]
        static void DisabledPath()
        {
        }

        [Kept]
        static void Use(int value)
        {
        }
    }
}
