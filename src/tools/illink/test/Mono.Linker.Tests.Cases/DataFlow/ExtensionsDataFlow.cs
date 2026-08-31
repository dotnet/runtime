// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;

namespace Mono.Linker.Tests.Cases.DataFlow
{
    [SkipKeptItemsValidation]
    [ExpectedNoWarnings]
    public class ExtensionsDataFlow
    {
        public static void Main()
        {
            TestExtensionMethod();
            TestExtensionMethodMismatch();
            TestExtensionMethodRequires();
        }

        [ExpectedWarning("IL2072", "GetWithMethods", nameof(Extensions.ExtensionMethod))]
        static void TestExtensionMethod()
        {
            GetWithFields().ExtensionMethod();
            GetWithMethods().ExtensionMethod();
        }

        static void TestExtensionMethodMismatch()
        {
            GetWithFields().ExtensionMethodMismatch();
        }

        [ExpectedWarning("IL2026", nameof(Extensions.ExtensionMethodRequires))]
        static void TestExtensionMethodRequires()
        {
            GetWithFields().ExtensionMethodRequires();
        }

        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
        static Type GetWithFields() => null;

        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
        static Type GetWithMethods() => null;
    }

    [ExpectedNoWarnings]
    public static class Extensions
    {
        public static void ExtensionMethod([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] this Type type)
        {
            type.RequiresPublicFields();
        }

        [ExpectedWarning("IL2067", "RequiresPublicMethods")]
        public static void ExtensionMethodMismatch([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] this Type type)
        {
            type.RequiresPublicMethods();
        }

        [RequiresUnreferencedCode(nameof(ExtensionMethodRequires))]
        public static void ExtensionMethodRequires([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] this Type type)
        {
        }
    }
}
