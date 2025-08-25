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
            TestExtensionMethodWithParamsMismatch();
            TestExtensionStaticMethodRequires();
            TestExtensionProperty();
            TestExtensionPropertyMismatch();
            TestExtensionPropertyAnnotatedAccessor();
            TestExtensionPropertyAnnotatedAccessorMismatch();
            TestExtensionPropertyRequires();
            TestExtensionPropertyConflict();
        }

        static void TestExtensionMethod()
        {
            GetWithFields().ExtensionMembersMethod();
        }

        [ExpectedWarning("IL2072", "GetWithMethods", nameof(ExtensionMembers.ExtensionMembersMethod))]
        static void TestExtensionMethodMismatch()
        {
            GetWithMethods().ExtensionMembersMethodMismatch();
        }

        [ExpectedWarning("IL2026", nameof(ExtensionMembers.ExtensionMembersMethodRequires))]
        static void TestExtensionMethodRequires()
        {
            GetWithFields().ExtensionMembersMethodRequires();
        }

        static void TestExtensionMethodWithParams()
        {
            GetWithFields().ExtensionMembersMethodWithParams(GetWithMethods());
        }

        [ExpectedWarning("IL2072", "GetWithMethods", nameof(ExtensionMembers.ExtensionMembersMethodWithParams))]
        [ExpectedWarning("IL2072", "GetWithFields", nameof(ExtensionMembers.ExtensionMembersMethodWithParams))]
        static void TestExtensionMethodWithParamsMismatch()
        {
            GetWithMethods().ExtensionMembersMethodWithParamsMismatch(GetWithFields());
        }

        [ExpectedWarning("IL2026", nameof(ExtensionMembers.ExtensionMembersStaticMethodRequires))]
        static void TestExtensionStaticMethodRequires()
        {
            ExtensionMembers.ExtensionMembersStaticMethodRequires();
        }

        [ExpectedWarning("IL2072", "ExtensionMembersProperty", "RequiresPublicMethods")]
        static void TestExtensionProperty()
        {
            var instance = GetWithFields();
            instance.ExtensionMembersProperty.RequiresPublicMethods();
            instance.ExtensionMembersProperty = GetWithMethods();
        }

        [ExpectedWarning("IL2072", "GetWithMethods", "ExtensionMembersPropertyMismatch")]
        [ExpectedWarning("IL2072", "GetWithMethods", "ExtensionMembersPropertyMismatch")]
        [ExpectedWarning("IL2072", "ExtensionMembersPropertyMismatch", "RequiresPublicFields")]
        static void TestExtensionPropertyMismatch()
        {
            var instance = GetWithMethods();
            instance.ExtensionMembersPropertyMismatch.RequiresPublicFields();
            instance.ExtensionMembersPropertyMismatch = GetWithFields();
        }

        [UnexpectedWarning("IL2072", "ExtensionMembersPropertyAnnotatedAccessor", "RequiresPublicMethods", Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/roslyn/issues/80017")]
        static void TestExtensionPropertyAnnotatedAccessor()
        {
            var instance = GetWithFields();
            instance.ExtensionMembersPropertyAnnotatedAccessor.RequiresPublicMethods();
            instance.ExtensionMembersPropertyAnnotatedAccessor = GetWithMethods();
        }

        [ExpectedWarning("IL2072", "GetWithMethods", "ExtensionMembersPropertyAnnotatedAccessorMismatch")]
        [ExpectedWarning("IL2072", "GetWithMethods", "ExtensionMembersPropertyAnnotatedAccessorMismatch")]
        [ExpectedWarning("IL2072", "ExtensionMembersPropertyAnnotatedAccessorMismatch", "RequiresPublicFields")]
        [ExpectedWarning("IL2072", "ExtensionMembersPropertyAnnotatedAccessorMismatch", "GetWithFields")]
        static void TestExtensionPropertyAnnotatedAccessorMismatch()
        {
            var instance = GetWithMethods();
            instance.ExtensionMembersPropertyAnnotatedAccessorMismatch.RequiresPublicFields();
            instance.ExtensionMembersPropertyAnnotatedAccessorMismatch = GetWithFields();
        }

        [ExpectedWarning("IL2026", "ExtensionMembersPropertyRequires")]
        static void TestExtensionPropertyRequires()
        {
            _ = GetWithFields().ExtensionMembersPropertyRequires;
        }

        [UnexpectedWarning("IL2072", "ExtensionMembersPropertyConflict", "RequiresPublicFields", Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/roslyn/issues/80017")]
        static void TestExtensionPropertyConflict()
        {
            var instance = GetWithFields();
            instance.ExtensionMembersPropertyConflict.RequiresPublicFields();
            instance.ExtensionMembersPropertyConflict = GetWithFields();
        }

        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
        public static Type GetWithFields() => null;

        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
        public static Type GetWithMethods() => null;
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

            public void ExtensionMembersMethodWithParams([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type typeParam)
            {
                type.RequiresPublicFields();
                typeParam.RequiresPublicMethods();
            }

            [ExpectedWarning("IL2067", "type", "RequiresPublicMethods")]
            [ExpectedWarning("IL2067", "typeParam", "RequiresPublicFields")]
            public void ExtensionMembersMethodWithParamsMismatch([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type typeParam)
            {
                type.RequiresPublicMethods();
                typeParam.RequiresPublicFields();
            }

            [RequiresUnreferencedCode(nameof(ExtensionMembersStaticMethodRequires))]
            public static void ExtensionMembersStaticMethodRequires() { }

            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
            public Type ExtensionMembersProperty
            {
                get => ExtensionMembersDataFlow.GetWithMethods();

                [ExpectedWarning("IL2067", "value", "RequiresPublicMethods")]
                set => value.RequiresPublicMethods();
            }

            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
            public Type ExtensionMembersPropertyMismatch
            {
                get => ExtensionMembersDataFlow.GetWithFields();

                [ExpectedWarning("IL2067", "value", "RequiresPublicFields")]
                set => value.RequiresPublicFields();
            }

            public Type ExtensionMembersPropertyAnnotatedAccessor
            {
                [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
                get => ExtensionMembersDataFlow.GetWithMethods();
                [param: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
                set => value.RequiresPublicMethods();
            }

            public Type ExtensionMembersPropertyAnnotatedAccessorMismatch
            {
                [ExpectedWarning("IL2073", "GetWithFields", Tool.Analyzer, "https://github.com/dotnet/roslyn/issues/80017")]
                [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
                get => ExtensionMembersDataFlow.GetWithFields();

                [ExpectedWarning("IL2067", "value", "RequiresPublicFields")]
                [param: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
                set => value.RequiresPublicFields();
            }

            public Type ExtensionMembersPropertyRequires
            {
                [RequiresUnreferencedCode("ExtensionMembersPropertyRequires")]
                get => null;
            }

            // Conflict scenario for extension property: both property-level and accessor-level DAM
            // Analyzer behavior: skip IL2043 conflicts for extension properties (property metadata doesn't apply to accessors).
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
            public Type ExtensionMembersPropertyConflict
            {
                [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
                get => ExtensionMembersDataFlow.GetWithFields();

                [param: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
                set => value.RequiresPublicFields();
            }
        }
    }
}
