// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DataFlow
{
    [SkipKeptItemsValidation]
    [ExpectedNoWarnings]
    public class ExtensionMembersDataFlow
    {
        public static void Main()
        {
            TestExtensionMethod();
            TestExtensionMethodMismatch();
            TestExtensionMethodRequires();
            TestExtensionMethodWithParams();
            TestExtensionStaticMethodRequires();
            TestExtensionProperty();
            TestExtensionPropertyAnnotatedReturn();
            TestExtensionPropertyMismatch();
            TestExtensionPropertyRequires();
        }

        [ExpectedWarning("IL2072", "GetWithMethods", nameof(ExtensionMembers.ExtensionMembersMethod))]
        static void TestExtensionMethod()
        {
            GetWithFields().ExtensionMembersMethod();
            GetWithMethods().ExtensionMembersMethod();
        }

        static void TestExtensionMethodMismatch()
        {
            GetWithFields().ExtensionMembersMethodMismatch();
        }

        [ExpectedWarning("IL2026", nameof(ExtensionMembers.ExtensionMembersMethodRequires))]
        static void TestExtensionMethodRequires()
        {
            GetWithFields().ExtensionMembersMethodRequires();
        }

        [ExpectedWarning("IL2072", "GetWithMethods", nameof(ExtensionMembers.ExtensionMembersMethodWithParams))]
        [ExpectedWarning("IL2072", "GetWithMethods", nameof(ExtensionMembers.ExtensionMembersMethodWithParams))]
        static void TestExtensionMethodWithParams()
        {
            GetWithFields().ExtensionMembersMethodWithParams(GetWithFields());
            GetWithMethods().ExtensionMembersMethodWithParams(GetWithMethods());
        }

        [ExpectedWarning("IL2026", nameof(ExtensionMembers.ExtensionMembersStaticMethodRequires))]
        static void TestExtensionStaticMethodRequires()
        {
            ExtensionMembers.ExtensionMembersStaticMethodRequires();
        }

        [UnexpectedWarning("IL2072", "ExtensionMembersProperty", "RequiresPublicMethods", Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/roslyn/issues/80017")]
        static void TestExtensionProperty()
        {
            GetWithFields().ExtensionMembersProperty.RequiresPublicMethods();
        }

        [UnexpectedWarning("IL2072", "ExtensionMembersPropertyAnnotatedReturn", "RequiresPublicMethods", Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/roslyn/issues/80017")]
        static void TestExtensionPropertyAnnotatedReturn()
        {
            GetWithFields().ExtensionMembersPropertyAnnotatedReturn.RequiresPublicMethods();
        }


        [ExpectedWarning("IL2072", "GetWithMethods", "ExtensionMembersProperty")]
        [ExpectedWarning("IL2072", "ExtensionMembersProperty", "RequiresPublicFields")]
        static void TestExtensionPropertyMismatch()
        {
            GetWithMethods().ExtensionMembersProperty.RequiresPublicFields();
        }

        [ExpectedWarning("IL2026", "ExtensionMembersPropertyRequires")]
        static void TestExtensionPropertyRequires()
        {
            _ = GetWithFields().ExtensionMembersPropertyRequires;
        }

        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
        static Type GetWithFields() => null;

        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
        static Type GetWithMethods() => null;
    }

    [ExpectedNoWarnings]
    public static class ExtensionMembers
    {
        extension([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] Type type)
        {
            public void ExtensionMembersMethod() => type.RequiresPublicFields();

            [ExpectedWarning("IL2067", "RequiresPublicMethods")] // The attribute gets copied to the internal class too, but that one is unused. Then it complains.
            public void ExtensionMembersMethodMismatch() => type.RequiresPublicMethods();

            [RequiresUnreferencedCode(nameof(ExtensionMembersMethodRequires))]
            public void ExtensionMembersMethodRequires() { }

            [ExpectedWarning("IL2067", "type", "RequiresPublicConstructors")]
            [ExpectedWarning("IL2067", "typeParam", "RequiresPublicMethods")]
            public void ExtensionMembersMethodWithParams([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] Type typeParam)
            {
                type.RequiresPublicConstructors();
                typeParam.RequiresPublicMethods();
            }

            [RequiresUnreferencedCode(nameof(ExtensionMembersStaticMethodRequires))]
            public static void ExtensionMembersStaticMethodRequires() { }

            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
            public Type ExtensionMembersProperty => null;

            public Type ExtensionMembersPropertyAnnotatedReturn
            {
                [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
                get => null;
            }

            public Type ExtensionMembersPropertyRequires
            {
                [RequiresUnreferencedCode("ExtensionMembersPropertyRequires")]
                get => null;
            }
        }
    }
}
