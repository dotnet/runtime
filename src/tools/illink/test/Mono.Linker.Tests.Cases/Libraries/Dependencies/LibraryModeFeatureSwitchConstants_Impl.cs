// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Mono.Linker.Tests.Cases.Libraries.Dependencies
{
    public static class LibraryModeFeatureSwitchConstants_Impl
    {
        [FeatureSwitchDefinition("Mono.Linker.Tests.Cases.Libraries.Dependencies.LibraryModeFeatureSwitchConstants_Impl.IsMultithreadingSupported")]
        public static bool IsMultithreadingSupported => false;

        public static int GetProcessorCount() => System.Environment.ProcessorCount;

        public static int GetProcessorCountIfSupported()
        {
            return IsMultithreadingSupported ? GetProcessorCount() : 1;
        }
    }
}
