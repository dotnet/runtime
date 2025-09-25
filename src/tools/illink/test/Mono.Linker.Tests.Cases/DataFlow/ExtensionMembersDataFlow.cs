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
    // [IgnoreTestCase("NativeAOT sometimes emits duplicate IL2041: https://github.com/dotnet/runtime/issues/119155", IgnoredBy = Tool.NativeAot)]
    // Root the entire assembly to ensure that ILLink/ILC analyze extension properties which are otherwise unused in IL.
    [SetupRootEntireAssembly("test")]
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
            TestExtensionMethodAnnotation();
            TestExtensionStaticMethodAnnotation();
            TestExtensionProperty();
            TestExtensionPropertyMismatch();
            TestExtensionPropertyAnnotatedAccessor();
            TestExtensionPropertyAnnotatedAccessorMismatch();
            TestExtensionPropertyRequires();
            TestExtensionPropertyConflict();
            TestExtensionOperators();
            TestExtensionOperatorsMismatch();
        }

        static void TestExtensionMethod()
        {
            GetWithFields().ExtensionMembersMethod();
        }

        [ExpectedWarning("IL2072", nameof(GetWithMethods), nameof(ExtensionMembers.ExtensionMembersMethod))]
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

        [ExpectedWarning("IL2072", nameof(GetWithMethods), nameof(ExtensionMembers.ExtensionMembersMethodWithParams))]
        [ExpectedWarning("IL2072", nameof(GetWithFields), nameof(ExtensionMembers.ExtensionMembersMethodWithParams))]
        static void TestExtensionMethodWithParamsMismatch()
        {
            GetWithMethods().ExtensionMembersMethodWithParamsMismatch(GetWithFields());
        }

        [ExpectedWarning("IL2026", nameof(ExtensionMembers.ExtensionMembersStaticMethodRequires))]
        static void TestExtensionStaticMethodRequires()
        {
            ExtensionMembers.ExtensionMembersStaticMethodRequires();
        }

        static void TestExtensionMethodAnnotation()
        {
            GetWithFields().ExtensionMembersMethodAnnotation();
        }

        static void TestExtensionStaticMethodAnnotation()
        {
            ExtensionMembers.ExtensionMembersStaticMethodAnnotation();
        }

        [ExpectedWarning("IL2072", "ExtensionMembersProperty", nameof(DataFlowTypeExtensions.RequiresPublicMethods))]
        static void TestExtensionProperty()
        {
            var instance = GetWithFields();
            instance.ExtensionMembersProperty.RequiresPublicMethods();
            instance.ExtensionMembersProperty = GetWithMethods();
        }

        [ExpectedWarning("IL2072", nameof(GetWithMethods), "ExtensionMembersPropertyMismatch")]
        [ExpectedWarning("IL2072", nameof(GetWithMethods), "ExtensionMembersPropertyMismatch")]
        [ExpectedWarning("IL2072", "ExtensionMembersPropertyMismatch", nameof(DataFlowTypeExtensions.RequiresPublicFields))]
        static void TestExtensionPropertyMismatch()
        {
            var instance = GetWithMethods();
            instance.ExtensionMembersPropertyMismatch.RequiresPublicFields();
            instance.ExtensionMembersPropertyMismatch = GetWithFields();
        }

        static void TestExtensionPropertyAnnotatedAccessor()
        {
            var instance = GetWithFields();
            instance.ExtensionMembersPropertyAnnotatedAccessor.RequiresPublicMethods();
            instance.ExtensionMembersPropertyAnnotatedAccessor = GetWithMethods();
        }

        [ExpectedWarning("IL2072", nameof(GetWithMethods), "ExtensionMembersPropertyAnnotatedAccessorMismatch")]
        [ExpectedWarning("IL2072", nameof(GetWithMethods), "ExtensionMembersPropertyAnnotatedAccessorMismatch")]
        [ExpectedWarning("IL2072", "ExtensionMembersPropertyAnnotatedAccessorMismatch", nameof(DataFlowTypeExtensions.RequiresPublicFields))]
        [ExpectedWarning("IL2072", "ExtensionMembersPropertyAnnotatedAccessorMismatch", nameof(GetWithFields))]
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

        static void TestExtensionPropertyConflict()
        {
            var instance = GetWithFields();
            instance.ExtensionMembersPropertyConflict.RequiresPublicFields();
            instance.ExtensionMembersPropertyConflict = GetWithFields();
        }

        [UnexpectedWarning("IL2062", nameof(DataFlowTypeExtensions.RequiresPublicFields), Tool.Analyzer, "https://github.com/dotnet/runtime/issues/119110")]
        static void TestExtensionOperators()
        {
            var a = GetWithFields();
            var b = GetWithFields();
            var c = a + b;
            c.RequiresPublicFields();
        }

        [ExpectedWarning("IL2072", nameof(GetWithMethods), nameof(ExtensionMembers.op_Subtraction), Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/runtime/issues/119110")]
        [ExpectedWarning("IL2072", nameof(GetWithMethods), nameof(ExtensionMembers.op_Subtraction), Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/runtime/issues/119110")]
        [ExpectedWarning("IL2072", nameof(ExtensionMembers.op_Subtraction), nameof(DataFlowTypeExtensions.RequiresPublicMethods), Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/runtime/issues/119110")]
        [UnexpectedWarning("IL2062", nameof(DataFlowTypeExtensions.RequiresPublicMethods), Tool.Analyzer, "https://github.com/dotnet/runtime/issues/119110")]
        static void TestExtensionOperatorsMismatch()
        {
            var a = GetWithMethods();
            var b = GetWithMethods();
            var c = a - b;
            c.RequiresPublicMethods();
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

            [ExpectedWarning("IL2067", nameof(DataFlowTypeExtensions.RequiresPublicMethods))]
            public void ExtensionMembersMethodMismatch() => type.RequiresPublicMethods();

            [RequiresUnreferencedCode(nameof(ExtensionMembersMethodRequires))]
            public void ExtensionMembersMethodRequires() { }

            public void ExtensionMembersMethodWithParams([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type typeParam)
            {
                type.RequiresPublicFields();
                typeParam.RequiresPublicMethods();
            }

            [ExpectedWarning("IL2067", "type", nameof(DataFlowTypeExtensions.RequiresPublicMethods))]
            [ExpectedWarning("IL2067", "typeParam", nameof(DataFlowTypeExtensions.RequiresPublicFields))]
            public void ExtensionMembersMethodWithParamsMismatch([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type typeParam)
            {
                type.RequiresPublicMethods();
                typeParam.RequiresPublicFields();
            }

            [RequiresUnreferencedCode(nameof(ExtensionMembersStaticMethodRequires))]
            public static void ExtensionMembersStaticMethodRequires() { }

            [ExpectedWarning("IL2041")]
            [ExpectedWarning("IL2041", Tool.Trimmer | Tool.NativeAot, "Analyzer doesn't see generated extension metadata type", CompilerGeneratedCode = true)]
            [ExpectedWarning("IL2067", nameof(DataFlowTypeExtensions.RequiresPublicMethods))]
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
            public void ExtensionMembersMethodAnnotation()
            {
                type.RequiresPublicFields();
                type.RequiresPublicMethods();
            }

            [ExpectedWarning("IL2041")]
            [ExpectedWarning("IL2041", Tool.Trimmer | Tool.NativeAot, "Analyzer doesn't see generated extension metadata type", CompilerGeneratedCode = true)]
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
            public static void ExtensionMembersStaticMethodAnnotation()
            {
            }

            [ExpectedWarning("IL2127", CompilerGeneratedCode = true)]
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
            public Type ExtensionMembersProperty
            {
                get => ExtensionMembersDataFlow.GetWithMethods();

                [ExpectedWarning("IL2067", "value", nameof(DataFlowTypeExtensions.RequiresPublicMethods))]
                set => value.RequiresPublicMethods();
            }

            [ExpectedWarning("IL2127", CompilerGeneratedCode = true)]
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
            public Type ExtensionMembersPropertyMismatch
            {
                get => ExtensionMembersDataFlow.GetWithFields();

                [ExpectedWarning("IL2067", "value", nameof(DataFlowTypeExtensions.RequiresPublicFields))]
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
                [ExpectedWarning("IL2073", nameof(ExtensionMembersDataFlow.GetWithFields))]
                [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
                get => ExtensionMembersDataFlow.GetWithFields();

                [ExpectedWarning("IL2067", "value", nameof(DataFlowTypeExtensions.RequiresPublicFields))]
                [param: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
                set => value.RequiresPublicFields();
            }

            public Type ExtensionMembersPropertyRequires
            {
                [RequiresUnreferencedCode("ExtensionMembersPropertyRequires")]
                get => null;
            }

            [ExpectedWarning("IL2127", CompilerGeneratedCode = true)]
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
            public Type ExtensionMembersPropertyConflict
            {
                [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
                get => ExtensionMembersDataFlow.GetWithFields();

                [param: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
                set => value.RequiresPublicFields();
            }

            [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
            public static Type operator +(
                [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] Type left,
                [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] Type right)
            {
                left.RequiresPublicFields();
                right.RequiresPublicFields();
                return ExtensionMembersDataFlow.GetWithFields();
            }

            [ExpectedWarning("IL2067", "left", nameof(DataFlowTypeExtensions.RequiresPublicMethods))]
            [ExpectedWarning("IL2067", "right", nameof(DataFlowTypeExtensions.RequiresPublicMethods))]
            [ExpectedWarning("IL2073", nameof(ExtensionMembersDataFlow.GetWithMethods))]
            [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
            public static Type operator -(
                [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] Type left,
                [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] Type right)
            {
                left.RequiresPublicMethods();
                right.RequiresPublicMethods();
                return ExtensionMembersDataFlow.GetWithMethods();
            }
        }
    }
}
