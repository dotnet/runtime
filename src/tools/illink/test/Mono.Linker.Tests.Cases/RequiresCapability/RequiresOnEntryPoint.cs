// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.RequiresCapability
{
    [SkipKeptItemsValidation]
    [ExpectedNoWarnings]
    [SetupCompileArgument("/unsafe")]
    class RequiresOnEntryPoint
    {
        [ExpectedWarning("IL2123", "RequiresOnEntryPoint.Main()")]
        [ExpectedWarning("IL3005", "RequiresOnEntryPoint.Main()", Tool.NativeAot | Tool.Analyzer, "ILLink only generates trimming warnings")]
        [ExpectedWarning("IL3057", "RequiresOnEntryPoint.Main()", Tool.NativeAot | Tool.Analyzer, "ILLink only generates trimming warnings")]
        [RequiresUnreferencedCode("")]
        [RequiresDynamicCode("")]
        [RequiresAssemblyFiles("")]
        static unsafe void Main()
        {
            delegate* unmanaged<void> del1 = &RequireUnreferencedCode;
            delegate* unmanaged<void> del2 = &RequireDynamicCode;
            delegate* unmanaged<void> del3 = &RequireAssemblyFiles;
        }

        [ExpectedWarning("IL2123", "RequiresOnEntryPoint.RequireUnreferencedCode()", Tool.NativeAot | Tool.Analyzer, "ILLink doesn't consider EntryPoint meaningful")]
        [RequiresUnreferencedCode("")]
        [UnmanagedCallersOnly(EntryPoint = nameof(RequireUnreferencedCode))]
        static void RequireUnreferencedCode()
        {
        }

        [ExpectedWarning("IL3057", "RequiresOnEntryPoint.RequireDynamicCode()", Tool.NativeAot | Tool.Analyzer, "ILLink only generates trimming warnings")]
        [RequiresDynamicCode("")]
        [UnmanagedCallersOnly(EntryPoint = nameof(RequireDynamicCode))]
        static void RequireDynamicCode()
        {
        }

        [ExpectedWarning("IL3005", "RequiresOnEntryPoint.RequireAssemblyFiles()", Tool.NativeAot | Tool.Analyzer, "ILLink only generates trimming warnings")]
        [RequiresAssemblyFiles("")]
        [UnmanagedCallersOnly(EntryPoint = nameof(RequireAssemblyFiles))]
        static void RequireAssemblyFiles()
        {
        }
    }
}
