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
    [SetupLinkerArgument("--feature", "Mono.Linker.Tests.Cases.Libraries.LibraryModeFeatureSwitchConstants.ExplicitlySetSwitch", "false")]
    [SetupCompileBefore("impl.dll", new[] { "Dependencies/LibraryModeFeatureSwitchConstants_Impl.cs" })]
    [SetupLinkerAction("link", "impl")]
    // Feature switches without a --feature value can still be substituted when the app is published, so library
    // mode must not fold them, even when their getters have constant bodies or come from a referenced implementation.
    [KeptAssembly("impl.dll")]
    [KeptMemberInAssembly("impl.dll", typeof(LibraryModeFeatureSwitchConstants_Impl),
        "get_IsMultithreadingSupported()", "GetProcessorCount()", "GetProcessorCountIfSupported()")]
    public class LibraryModeFeatureSwitchConstants
    {
        [Kept]
        public static void Main()
        {
            TestConstantSwitchRetains();
            TestReferencedConstantSwitchRetains();
            TestNonConstantSwitchRetains();
            TestExplicitlySetSwitchFolds();
        }

        [Kept]
        public LibraryModeFeatureSwitchConstants()
        {
        }

        [Kept]
        [KeptAttributeAttribute(typeof(FeatureSwitchDefinitionAttribute))]
        [FeatureSwitchDefinition("Mono.Linker.Tests.Cases.Libraries.LibraryModeFeatureSwitchConstants.IsMultithreadingSupported")]
        static bool IsMultithreadingSupported
        {
            [Kept]
            get => false;
        }

        [Kept]
        static int GetProcessorCount() => Environment.ProcessorCount;

        [Kept]
        public static void TestConstantSwitchRetains()
        {
            if (IsMultithreadingSupported)
                GetProcessorCount();
        }

        [Kept]
        public static void TestReferencedConstantSwitchRetains()
        {
            Use(LibraryModeFeatureSwitchConstants_Impl.GetProcessorCountIfSupported());
        }

        [Kept]
        [KeptAttributeAttribute(typeof(FeatureSwitchDefinitionAttribute))]
        [FeatureSwitchDefinition("Mono.Linker.Tests.Cases.Libraries.LibraryModeFeatureSwitchConstants.AppConfigurableSwitch")]
        static bool AppConfigurableSwitch
        {
            [Kept]
            get => AppContext.TryGetSwitch("Mono.Linker.Tests.Cases.Libraries.LibraryModeFeatureSwitchConstants.AppConfigurableSwitch", out bool isEnabled) && isEnabled;
        }

        [Kept]
        public static void TestNonConstantSwitchRetains()
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

        [FeatureSwitchDefinition("Mono.Linker.Tests.Cases.Libraries.LibraryModeFeatureSwitchConstants.ExplicitlySetSwitch")]
        static bool ExplicitlySetSwitch => true;

        static void ExplicitlyDisabledPath()
        {
        }

        [Kept]
        [ExpectedInstructionSequence(new[] {
            "nop",
            "ldc.i4.0",
            "stloc.0",
            "ldloc.0",
            "brfalse.s il_6",
            "ret"
        })]
        public static void TestExplicitlySetSwitchFolds()
        {
            if (ExplicitlySetSwitch)
                ExplicitlyDisabledPath();
        }

        [Kept]
        static void Use(int value)
        {
        }
    }
}
