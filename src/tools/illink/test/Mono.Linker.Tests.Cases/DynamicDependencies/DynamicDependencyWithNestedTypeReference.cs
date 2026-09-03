// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.DynamicDependencies.Dependencies;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DynamicDependencies
{
    [SetupCompileBefore("library.dll", new[] { "Dependencies/DynamicDependencyWithNestedTypeReferenceLibrary.cs" })]
    public class DynamicDependencyWithNestedTypeReference
    {
        public static void Main()
        {
            Dependency();
        }

        [Kept]
        [DynamicDependency("MethodWithParameter(Mono.Linker.Tests.Cases.DynamicDependencies.Dependencies.Outer.Nested)", typeof(Target))]
        [DynamicDependency("MethodWithGenericParameter(Mono.Linker.Tests.Cases.DynamicDependencies.Dependencies.GenericOuter{System.Int32}.GenericMiddle{System.String}.GenericNested{System.Boolean})", typeof(Target))]
        [DynamicDependency("MethodWithNonGenericParameter(Mono.Linker.Tests.Cases.DynamicDependencies.Dependencies.GenericOuter{System.Int32}.NonGenericNested)", typeof(Target))]
        [DynamicDependency("MethodWithLocalNonGenericParameter(Mono.Linker.Tests.Cases.DynamicDependencies.DynamicDependencyWithNestedTypeReference.LocalGenericOuter{System.Int32}.NonGenericNested)", typeof(Target))]
        private static void Dependency()
        {
        }

        private static class Target
        {
            [Kept]
            private static void MethodWithParameter(Outer.Nested value)
            {
            }

            [Kept]
            private static void MethodWithGenericParameter(GenericOuter<int>.GenericMiddle<string>.GenericNested<bool> value)
            {
            }

            [Kept]
            private static void MethodWithNonGenericParameter(GenericOuter<int>.NonGenericNested value)
            {
            }

            [Kept]
            private static void MethodWithLocalNonGenericParameter(LocalGenericOuter<int>.NonGenericNested value)
            {
            }
        }

        [Kept]
        private class LocalGenericOuter<T>
        {
            [Kept]
            public class NonGenericNested
            {
            }
        }
    }
}
