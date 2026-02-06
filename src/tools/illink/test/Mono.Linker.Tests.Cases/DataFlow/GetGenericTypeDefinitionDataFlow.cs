// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;

namespace Mono.Linker.Tests.Cases.DataFlow
{
    [SkipKeptItemsValidation]
    [ExpectedNoWarnings]
    public class GetGenericTypeDefinitionDataFlow
    {
        public static void Main()
        {
            TestAllPropagated();
            TestPublicConstructors();
            TestPublicMethods();
            TestPublicFields();
            TestPublicNestedTypes();
            TestPublicProperties();
            TestPublicEvents();
            TestInterfaces();
            TestAll();
            TestNullValue();
            TestNoValue();
            TestUnknownValue();
            TestCombinations();
        }

        static void TestAllPropagated()
        {
            Type type = typeof(List<int>);
            // GetGenericTypeDefinition on a known type should track the result as the specific generic type definition
            // List<> doesn't have members that violate trimming rules, so this shouldn't warn
            type.GetGenericTypeDefinition().RequiresAll();
        }

        static void TestPublicConstructors()
        {
            Type type = GetTypeWithPublicConstructors();
            // GetGenericTypeDefinition propagates all annotations
            type.GetGenericTypeDefinition().RequiresPublicConstructors();
        }

        static void TestPublicMethods()
        {
            Type type = GetTypeWithPublicMethods();
            type.GetGenericTypeDefinition().RequiresPublicMethods();
        }

        static void TestPublicFields()
        {
            Type type = GetTypeWithPublicFields();
            type.GetGenericTypeDefinition().RequiresPublicFields();
        }

        static void TestPublicNestedTypes()
        {
            Type type = GetTypeWithPublicNestedTypes();
            type.GetGenericTypeDefinition().RequiresPublicNestedTypes();
        }

        static void TestPublicProperties()
        {
            Type type = GetTypeWithPublicProperties();
            type.GetGenericTypeDefinition().RequiresPublicProperties();
        }

        static void TestPublicEvents()
        {
            Type type = GetTypeWithPublicEvents();
            type.GetGenericTypeDefinition().RequiresPublicEvents();
        }

        static void TestInterfaces()
        {
            Type type = GetTypeWithInterfaces();
            type.GetGenericTypeDefinition().RequiresInterfaces();
        }

        static void TestAll()
        {
            Type type = GetTypeWithAll();
            // All annotations should be propagated
            type.GetGenericTypeDefinition().RequiresAll();
        }

        static void TestNullValue()
        {
            Type type = null;
            // Should not warn - null.GetGenericTypeDefinition() will throw at runtime
            type.GetGenericTypeDefinition();
        }

        static void TestNoValue()
        {
            Type type;
            if (Random.Shared.NextDouble() == 0)
                type = typeof(List<int>);
            else
                return;

            type.GetGenericTypeDefinition();
        }

        [ExpectedWarning("IL2072", nameof(DataFlowTypeExtensions.RequiresAll))]
        static void TestUnknownValue()
        {
            Type type = GetUnknownType();
            // Should warn - unknown type
            type.GetGenericTypeDefinition().RequiresAll();
        }

        static void TestCombinations()
        {
            TestCombinationPublicMethodsAndFields(typeof(List<int>));
        }

        [ExpectedWarning("IL2072", nameof(DataFlowTypeExtensions.RequiresPublicProperties))]
        static void TestCombinationPublicMethodsAndFields(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicFields)] Type type)
        {
            // Should propagate PublicMethods and PublicFields
            type.GetGenericTypeDefinition().RequiresPublicMethods();
            type.GetGenericTypeDefinition().RequiresPublicFields();

            // Should warn - PublicProperties not included
            type.GetGenericTypeDefinition().RequiresPublicProperties();
        }

        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        static Type GetTypeWithAll() => typeof(List<int>);

        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        static Type GetTypeWithPublicConstructors() => typeof(List<int>);

        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
        static Type GetTypeWithPublicMethods() => typeof(List<int>);

        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
        static Type GetTypeWithPublicFields() => typeof(List<int>);

        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes)]
        static Type GetTypeWithPublicNestedTypes() => typeof(List<int>);

        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
        static Type GetTypeWithPublicProperties() => typeof(List<int>);

        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents)]
        static Type GetTypeWithPublicEvents() => typeof(List<int>);

        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        static Type GetTypeWithInterfaces() => typeof(List<int>);

        static Type GetUnknownType() => null;
    }
}
