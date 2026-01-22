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
            type.RequiresAll();
            type.GetGenericTypeDefinition().RequiresAll();
        }

        [ExpectedWarning("IL2072", nameof(DataFlowTypeExtensions.RequiresPublicConstructors))]
        static void TestPublicConstructors()
        {
            Type type = typeof(List<int>);
            // GetGenericTypeDefinition propagates all annotations
            type.GetGenericTypeDefinition().RequiresPublicConstructors();
        }

        [ExpectedWarning("IL2072", nameof(DataFlowTypeExtensions.RequiresPublicMethods))]
        static void TestPublicMethods()
        {
            Type type = typeof(List<int>);
            type.GetGenericTypeDefinition().RequiresPublicMethods();
        }

        [ExpectedWarning("IL2072", nameof(DataFlowTypeExtensions.RequiresPublicFields))]
        static void TestPublicFields()
        {
            Type type = typeof(List<int>);
            type.GetGenericTypeDefinition().RequiresPublicFields();
        }

        [ExpectedWarning("IL2072", nameof(DataFlowTypeExtensions.RequiresPublicNestedTypes))]
        static void TestPublicNestedTypes()
        {
            Type type = typeof(List<int>);
            type.GetGenericTypeDefinition().RequiresPublicNestedTypes();
        }

        [ExpectedWarning("IL2072", nameof(DataFlowTypeExtensions.RequiresPublicProperties))]
        static void TestPublicProperties()
        {
            Type type = typeof(List<int>);
            type.GetGenericTypeDefinition().RequiresPublicProperties();
        }

        [ExpectedWarning("IL2072", nameof(DataFlowTypeExtensions.RequiresPublicEvents))]
        static void TestPublicEvents()
        {
            Type type = typeof(List<int>);
            type.GetGenericTypeDefinition().RequiresPublicEvents();
        }

        [ExpectedWarning("IL2072", nameof(DataFlowTypeExtensions.RequiresInterfaces))]
        static void TestInterfaces()
        {
            Type type = typeof(List<int>);
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
            TestCombinationAll(typeof(List<int>));
            TestCombinationPublicMethodsAndFields(typeof(List<int>));
        }

        static void TestCombinationAll([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type)
        {
            // All annotations propagate
            type.GetGenericTypeDefinition().RequiresAll();
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

        static Type GetUnknownType() => null;

        class TestType
        {
            public TestType() { }
            public void Method() { }
            public int Field;
            public int Property { get; set; }
            public event EventHandler Event;
            public class NestedType { }
        }
    }
}
